using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace eft_dma_radar
{

    /// <summary>
    /// Class containing Game (Raid) instance.
    /// </summary>
    public class Game
    {
        private readonly Memory _mem;
        private GameObjectManager _gom;
        private ulong _localGameWorld;
        private RegisteredPlayers _rgtPlayers;
        public volatile bool InGame = false;
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _rgtPlayers?.Players;
            }
        }

        public Game(Memory mem)
        {
            _mem = mem;
        }

        /// <summary>
        /// Waits until Raid has started before returning to caller.
        /// </summary>
        public void WaitForGame()
        {
            while (true)
            {
                if (GetGOM()) break;
                else Thread.Sleep(500);
            }
            while (true)
            {
                if (GetLGW()) break;
                else Thread.Sleep(500);
            }
            if (GetRegPlayers())
            {
                Console.WriteLine("Raid has started!");
                InGame = true;
                return;
            }
            else throw new Exception("ERROR starting game.");
        }

        /// <summary>
        /// Gets Game Object Manager structure.
        /// </summary>
        private bool GetGOM()
        {
            try
            {
                var addr = _mem.ReadPtr(_mem.BaseModule + 0x17F8D28);
                _gom = _mem.ReadStruct<GameObjectManager>(addr);
                Debug.WriteLine($"Found Game Object Manager at 0x{addr.ToString("X")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Game Object Manager: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Helper method to locate GOM.
        /// </summary>
        private ulong GetObjectFromList(ulong listPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = _mem.ReadStruct<BaseObject>(_mem.ReadPtr(listPtr));
            var lastObject = _mem.ReadStruct<BaseObject>(_mem.ReadPtr(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    var classNamePtr = activeObject.obj + 0x60;

                    var memStr = _mem.ReadString(_mem.ReadPtr(classNamePtr), 64);

                    if (memStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Found object {memStr}");
                        return activeObject.obj;
                    }

                    activeObject = _mem.ReadStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
                Debug.WriteLine($"Couldn't find object {objectName}");
            }

            return 0;
        }

        /// <summary>
        /// Gets Local Game World address.
        /// </summary>
        private bool GetLGW()
        {
            try
            {
                var gameWorld = GetObjectFromList(
                    _mem.ReadPtr(_gom.ActiveNodes),
                    _mem.ReadPtr(_gom.LastActiveNode),
                    "GameWorld");
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld Object, likely not in raid.");
                _localGameWorld = _mem.ReadPtrChain(gameWorld, new uint[] { 0x30, 0x18, 0x28 });
                var regPlayers = _mem.ReadPtr(_localGameWorld + 0x80);
                int playerCount = _mem.ReadInt(regPlayers + 0x18);
                if (playerCount > 1) return true;
                else return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Gets Registered Players list
        /// </summary>
        private bool GetRegPlayers()
        {
            try
            {
                var baseAddr = _mem.ReadPtr(_localGameWorld + 0x80);
                var rgtPlayers = new RegisteredPlayers(_mem, baseAddr);
                _rgtPlayers = rgtPlayers;
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Registered Players: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread. Updates player list, and updates all player values.
        /// </summary>
        public void GameLoop()
        {
            int playerCount = _rgtPlayers.PlayerCount;
            if (playerCount < 1 || playerCount > 1024)
            {
                Console.WriteLine("Raid has ended!");
                InGame = false;
                return;
            }
            _rgtPlayers.UpdateList(); // Check for new players, add to list
            _rgtPlayers.UpdateAllPlayers(); // Update all player locations,etc.
        }
    }
}
