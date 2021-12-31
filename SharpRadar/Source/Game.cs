using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace SharpRadar
{

    /// <summary>
    /// Class containing Game (Raid) instance.
    /// </summary>
    public class Game
    {
        private readonly Memory _mem;
        private GameObjectManager _gom;
        private ulong _localGameWorld;
        private ulong _rgtPlayers;
        public volatile bool InGame = false;
        public ConcurrentDictionary<string, Player> Players { get; private set; }

        public Game(Memory mem)
        {
            _mem = mem;
            Players = new ConcurrentDictionary<string, Player>();
        }

        public void WaitForGame()
        {
            while (_mem.Heartbeat())
            {
                if (GetGOM() && GetLGW()) break;
                else Thread.Sleep(500);
            }
            Console.WriteLine("Preparing game...");
            while (GetRegPlayers() == 0) Thread.Sleep(100);
            Console.WriteLine("Raid has started!");
            InGame = true;
        }

        private bool GetGOM()
        {
            try
            {
                var addr = _mem.AddressOf(_mem.BaseModule + 0x17F8D28);
                _gom = _mem.ReadMemoryStruct<GameObjectManager>(addr);
                Debug.WriteLine($"Found Game Object Manager at 0x{addr.ToString("x")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Game Object Manager: {ex}");
                return false;
            }
        }
        private ulong GetObjectFromList(ulong listPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = _mem.ReadMemoryStruct<BaseObject>(_mem.AddressOf(listPtr));
            var lastObject = _mem.ReadMemoryStruct<BaseObject>(_mem.AddressOf(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    var classNamePtr = activeObject.obj + 0x60;

                    var memStr = _mem.ReadMemoryString(_mem.AddressOf(classNamePtr), 64);

                    if (memStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Found object {memStr}");
                        return activeObject.obj;
                    }

                    activeObject = _mem.ReadMemoryStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
                Debug.WriteLine($"Couldn't find object {objectName}");
            }

            return 0;
        }

        private bool GetLGW()
        {
            try
            {
                var gameWorld = GetObjectFromList(
                    _mem.AddressOf(_gom.ActiveNodes),
                    _mem.AddressOf(_gom.LastActiveNode),
                    "GameWorld");
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld Object, likely not in raid.");
                Debug.WriteLine($"Found Game World at 0x{gameWorld.ToString("x")}");
                _localGameWorld = _mem.AddressOf(gameWorld + 0x30);
                _localGameWorld = _mem.AddressOf(_localGameWorld + 0x18);
                _localGameWorld = _mem.AddressOf(_localGameWorld + 0x28);
                Debug.WriteLine($"Found Local Game World at 0x{_localGameWorld.ToString("X")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }
        private int GetRegPlayers()
        {
            try
            {
                _rgtPlayers = _mem.AddressOf(_localGameWorld + 0x80, 0);
                int playerCnt = _mem.ReadMemoryInt(_rgtPlayers + 0x18);
                return playerCnt;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Registered Players: {ex}");
                return 0;
            }
        }

        /// <summary>
        /// Main Game Loop executed by Memory Worker Thread.
        /// </summary>
        public void GameLoop()
        {
            int playerCnt = GetRegPlayers();
            if (playerCnt == 0)
            {
                Console.WriteLine("Raid has ended!");
                InGame = false;
                return;
            }
            var listBase = _mem.AddressOf(_rgtPlayers + 0x0010, 0);
            for (uint i = 0; i < playerCnt; i++) // Add new players
            {
                try
                {
                    var playerBase = _mem.AddressOf(listBase + 0x20 + (i * 0x8), 0);
                    var playerProfile = _mem.AddressOf(playerBase + 0x4B8, 0);
                    var playerId = _mem.AddressOf(playerProfile + 0x10, 0);
                    var playerIdString = _mem.ReadMemoryUnityString(playerId, 0); // Player's Personal ID ToDo Testing
                    if (!this.Players.ContainsKey(playerIdString)) // See if already exists
                    {
                        this.Players.TryAdd(playerIdString, new Player(_mem, playerBase, playerProfile));
                    }
                }
                catch { }
            }
            foreach (var player in this.Players) // Update all players
            {
                lock (player.Value)
                {
                    player.Value.Update();
                }
            }
        }
    }
}
