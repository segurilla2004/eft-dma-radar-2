using System;
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
        private volatile bool _inGame = false;
        public bool InGame
        {
            get
            {
                return _inGame;
            }
        }

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
                        Console.WriteLine("Trying again in 15 seconds...");
                        Thread.Sleep(15000);
                    }
                }
                while (Heartbeat()) // Game is running, wait for raid entry
                {
                    if (GetLGW()) // Try find raid
                    {
                        _inGame = true;
                        ulong rgtPlayers = AddressOf(_localGameWorld + 0x80);
                        while (Heartbeat()) // Main loop
                        {
                            try
                            {
                                int playerCnt = ReadMemoryInt(rgtPlayers + 0x18);
                                Console.WriteLine("Online Raid Player Count is: " + playerCnt);
                                ulong listBase = AddressOf(rgtPlayers + 0x0010);
                                for (uint i = 0; i < playerCnt; i++)
                                {
                                    ulong playerBase = AddressOf(listBase + 0x20 + (i * 0x8));
                                    /// ToDo - Get Player Location Transform
                                    var playerProfile = AddressOf(playerBase + 0x4b0);
                                    var playerInfo = AddressOf(playerProfile + 0x28);
                                    var playerNickname = AddressOf(playerInfo + 0x10);
                                    var name = ReadMemoryString(playerNickname, 64);
                                    Console.WriteLine($"Player {i + 1}: {name}"); // For testing purposes
                                }
                                Thread.Sleep(2200); // Tick
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Game ended? " + ex.ToString()); // for debug purposes
                                break;
                            }
                        }
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
                    Console.WriteLine($"EscapeFromTarkov.exe is running at PID {_pid}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting PID: {ex}");
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
                    Console.WriteLine($"Found UnityPlayer.dll at 0x{_baseModule.ToString("x")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }

        private bool GetGOM()
        {
            try
            {
                var addr = AddressOf(_baseModule + (ulong)Offsets.Startup.GameObjectManager);
                _gom = ReadMemoryStruct<GameObjectManager>(addr);
                Console.WriteLine($"Found Game Object Manager at 0x{addr.ToString("x")}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting Game Object Manager: {ex}");
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
                if (gameWorld == 0) throw new DMAException("Unable to find GameWorld, not in raid.");
                Console.WriteLine($"Found Game World at 0x{gameWorld.ToString("x")}");
                _localGameWorld = AddressOf(gameWorld + 0x30);
                _localGameWorld = AddressOf(_localGameWorld + 0x18);
                _localGameWorld = AddressOf(_localGameWorld + 0x28);
                Console.WriteLine($"Found Local Game World at 0x{_localGameWorld.ToString("X")}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR getting Local Game World: {ex}");
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
                        Console.WriteLine($"Found object {memStr}");
                        return activeObject.obj;
                    }

                    activeObject = ReadMemoryStruct<BaseObject>(activeObject.nextObjectLink); // Read next object
                }
                Console.WriteLine($"Couldn't find object {objectName}");
            }

            return 0;
        }

        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        private ulong AddressOf(ulong ptr)
        {
            return BitConverter.ToUInt64(vmm.MemRead(_pid, ptr, 8, 0), 0);
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
        /// ToDo - Not sure if this implementation is correct
        /// </summary>
        private string ReadMemoryString(ulong addr, uint size) // read n bytes (string)
        {
            try
            {
                var buffer = vmm.MemRead(_pid, addr, size, 0);
                return Encoding.Default.GetString(buffer);
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
