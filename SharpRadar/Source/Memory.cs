using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using vmmsharp;

namespace SharpRadar
{
    public class Memory : IDisposable
    {
        private readonly Thread _worker;
        private uint _pid; // Stores EscapeFromTarkov.exe PID
        private ulong _baseModule; // Stores UnityPlayer.DLL Module Base Entry
        private GameObjectManager _gom;
        private ulong _localGameWorld;
        private ulong _rgtPlayers;
        private volatile bool _inGame = false;
        public bool InGame
        {
            get
            {
                return _inGame;
            }
        }
        public ConcurrentDictionary<string, Player> Players;

        public Memory()
        {
            Console.WriteLine("Loading memory module...");
            vmm.Initialize("-printf", "-v", "-device", "FPGA"); // Initialize DMA device
            Console.WriteLine("Starting Memory worker thread...");
            _worker = new Thread(() => Worker()) { IsBackground = true };
            _worker.Start(); // Start new background thread to do memory operations on
        }

        private void Worker()
        {
            while (true)
            {
                while (true) // Startup loop
                {
                    if (GetPid() 
                    && GetModuleBase() 
                    && GetGOM()
                    )
                    {
                        break;
                    }
                    else
                    {
                        Debug.WriteLine("Trying again in 15 seconds...");
                        Thread.Sleep(15000);
                    }
                }
                while (Heartbeat()) // Game is running, wait for raid entry
                {
                    if (GetLGW()) // Try find raid
                    {
                        this.Players = new ConcurrentDictionary<string, Player>();
                        Console.WriteLine("Raid started!");
                        _inGame = true;
                        while (GetRegPlayers()) // Main game loop
                        {
                            try
                            {
                                GameLoop();
                                Thread.Sleep(2200); // Tick
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine(ex.ToString()); // for debug purposes
                            }
                        }
                        Console.WriteLine("Raid ended!");
                        _inGame = false;
                    }
                    else Thread.Sleep(5000);
                }
                Console.WriteLine("EscapeFromTarkov.exe is no longer running... Attempting to restart...");
            }
        }

        private bool GetPid()
        {
            try
            {
                vmm.PidGetFromName("EscapeFromTarkov.exe", out _pid);
                if (_pid == 0) throw new DMAException("Unable to obtain PID. Game may not be running.");
                else
                {
                    Debug.WriteLine($"EscapeFromTarkov.exe is running at PID {_pid}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting PID: {ex}");
                return false;
            }
        }

        private bool GetModuleBase()
        {
            try
            {
                _baseModule = vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
                if (_baseModule == 0) throw new DMAException("Unable to obtain Base Module Address. Game may not be running");
                else
                {
                    Debug.WriteLine($"Found UnityPlayer.dll at 0x{_baseModule.ToString("x")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }

        private bool GetGOM()
        {
            try
            {
                var addr = AddressOf(_baseModule + 0x17F1CE8);
                _gom = ReadMemoryStruct<GameObjectManager>(addr);
                Debug.WriteLine($"Found Game Object Manager at 0x{addr.ToString("x")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Game Object Manager: {ex}");
                return false;
            }
        }

        private bool GetLGW()
        {
            try
            {
                var gameWorld = GetObjectFromList(
                    AddressOf(_gom.ActiveNodes), 
                    AddressOf(_gom.LastActiveNode), 
                    "GameWorld");
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld Object, likely not in raid.");
                Debug.WriteLine($"Found Game World at 0x{gameWorld.ToString("x")}");
                _localGameWorld = AddressOf(gameWorld + 0x30);
                _localGameWorld = AddressOf(_localGameWorld + 0x18);
                _localGameWorld = AddressOf(_localGameWorld + 0x28);
                Debug.WriteLine($"Found Local Game World at 0x{_localGameWorld.ToString("X")}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Local Game World: {ex}");
                return false;
            }
        }
        private bool GetRegPlayers()
        {
            try
            {
                _rgtPlayers = AddressOf(_localGameWorld + 0x80);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting Registered Players: {ex}");
                return false;
            }
        }

        private ulong GetObjectFromList(ulong listPtr, ulong lastObjectPtr, string objectName)
        {
            var activeObject = ReadMemoryStruct<BaseObject>(AddressOf(listPtr));
            var lastObject = ReadMemoryStruct<BaseObject>(AddressOf(lastObjectPtr));

            if (activeObject.obj != 0x0)
            {
                while (activeObject.obj != 0x0 && activeObject.obj != lastObject.obj)
                {
                    var classNamePtr = activeObject.obj + 0x60;

                    var memStr = ReadMemoryString(AddressOf(classNamePtr), 64);

                    if (memStr.Contains(objectName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.WriteLine($"Found object {memStr}");
                        return activeObject.obj;
                    }

                    activeObject = ReadMemoryStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
                Debug.WriteLine($"Couldn't find object {objectName}");
            }

            return 0;
        }

        private void GameLoop()
        {
            int playerCnt = ReadMemoryInt(_rgtPlayers + 0x18);
            Debug.WriteLine("Online Raid Player Count is: " + playerCnt);
            ulong listBase = AddressOf(_rgtPlayers + 0x0010);
            string currentPlayerGroupID = null;
            for (uint i = 0; i < playerCnt; i++)
            {
                ulong playerBase = AddressOf(listBase + 0x20 + (i * 0x8));
                /// ToDo - Get Player Location Transform
                var playerProfile = AddressOf(playerBase + 0x4b0);
                var playerId = AddressOf(playerProfile + 0x10);
                var playerIdString = ReadMemoryUnityString(playerId); // Player's Personal ID ToDo Testing
                var playerInfo = AddressOf(playerProfile + 0x28);
                var playerGroupId = AddressOf(playerInfo + 0x20);
                var playerGroupIdStr = ReadMemoryUnityString(playerGroupId); // Player's Group Affiliation ID ToDo testing
                var playerSide = ReadMemoryInt(playerInfo + 0x58); // Scav, PMC, etc.
                var playerNickname = AddressOf(playerInfo + 0x10);
                var nicknameStr = ReadMemoryUnityString(playerNickname); // Now working!
                Debug.WriteLine($"Player {i + 1}: {nicknameStr}"); // For testing purposes

                var playerIsAlive = true; // ToDo get value if player is alive or not
                var playerPos = new UnityEngine.Vector3(0,0,0); // ToDo parse vectors from transform
                var playerType = PlayerType.Default; // ToDo parse player type and assign proper value
                if (i == 0) // Current player is always first
                {
                    currentPlayerGroupID = playerGroupIdStr;
                    playerType = PlayerType.CurrentPlayer;
                }
                else
                {
                    if (playerGroupIdStr == currentPlayerGroupID) playerType = PlayerType.Teammate;
                    else
                    {
                        if (playerSide == 0x1 || playerSide == 0x2) playerType = PlayerType.PMC;
                        else if (playerSide == 0x4) playerType = PlayerType.AIScav; // ToDo determine player scav
                    }
                }
                if (this.Players.TryGetValue(playerIdString, out var player)) // Update existing object
                {
                    lock (player) // obtain lock
                    {
                        if (player.IsAlive) // Don't update already dead player
                        {
                            player.Position = playerPos;
                            player.IsAlive = playerIsAlive;
                        }
                    }
                }
                else // Create new object
                {
                    this.Players.TryAdd(playerIdString, new Player(
                        nicknameStr, // Player's name
                        playerGroupIdStr, // Player's Group ID
                        playerType) // Player's Type
                    {
                        Position = playerPos
                    });
                }
            }
        }

        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        private ulong AddressOf(ulong ptr) => ReadMemoryUlong(ptr);
        private ulong ReadMemoryUlong(ulong addr)
        {
            try
            {
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        private long ReadMemoryLong(ulong addr) // read 8 bytes (int64)
        {
            try
            {
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        private int ReadMemoryInt(ulong addr) // read 4 bytes (int32)
        {
            try
            {
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        private uint ReadMemoryUint(ulong addr) // read 4 bytes (uint32)
        {
            try
            {
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        private float ReadMemoryFloat(ulong addr) // read 4 bytes (float)
        {
            try
            {
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        private double ReadMemoryDouble(ulong addr) // read 8 bytes (double)
        {
            try
            {
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        private bool ReadMemoryBool(ulong addr) // read 1 byte (bool)
        {
            try
            {
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, 0), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        private T ReadMemoryStruct<T>(ulong addr) // Read structure from memory location
        {
            int size = Marshal.SizeOf(typeof(T));
            var mem = Marshal.AllocHGlobal(size); // alloc mem
            try
            {
                Marshal.Copy(
                    vmm.MemRead(_pid, addr, (uint)size, 0), 
                    0, mem, size); // Read to pointer location

                return (T)Marshal.PtrToStructure(mem, typeof(T)); // Convert bytes to struct
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
            finally
            {
                Marshal.FreeHGlobal(mem); // free mem
            }
        }
        /// <summary>
        /// Read 'n' bytes at specified address and convert directly to a string.
        /// </summary>
        private string ReadMemoryString(ulong addr, uint size) // read n bytes (string)
        {
            try
            {
                return Encoding.Default.GetString(
                    vmm.MemRead(_pid, addr, size, 0));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        private string ReadMemoryUnityString(ulong addr)
        {
            try
            {
                var length = (uint)ReadMemoryInt(addr + 0x10);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + 0x14, length * 2, 0));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// ToDo - Not sure if this is a good way to keep track if the process is still open
        /// </summary>
        private bool Heartbeat() // Make sure game is still there
        {
            vmm.PidGetFromName("EscapeFromTarkov.exe", out uint pid);
            if (pid == 0) return false;
            else return true;
        }

        // Public implementation of Dispose pattern callable by consumers.
        private bool _disposed = false;
        /// <summary>
        /// Calls vmm.Close() and cleans up DMA Unmanaged Resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).
                vmm.Close(); // Cleanup vmmsharp resources
            }

            _disposed = true;
        }

    }

    public class DMAException : Exception
    {
        public DMAException()
        {
        }

        public DMAException(string message)
            : base(message)
        {
        }

        public DMAException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
