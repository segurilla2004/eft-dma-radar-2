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
        private Game _game;
        private uint _pid;
        public ulong BaseModule { get; private set; }
        public bool InGame
        {
            get
            {
                return _game?.InGame ?? false;
            }
        }
        public ConcurrentDictionary<string, Player> Players
        {
            get
            {
                return _game.Players;
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
                    )
                    {
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find game, trying again in 15 seconds...");
                        Thread.Sleep(15000);
                    }
                }
                while (Heartbeat())
                {
                    _game = new Game(this);
                    _game.WaitForGame();
                    while (Heartbeat() && _game.InGame)
                    {
                        _game.GameLoop();
                    }
                }
                Console.WriteLine("Game is no longer running... Attempting to restart...");
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
                BaseModule = vmm.ProcessGetModuleBase(_pid, "UnityPlayer.dll");
                if (BaseModule == 0) throw new DMAException("Unable to obtain Base Module Address. Game may not be running");
                else
                {
                    Debug.WriteLine($"Found UnityPlayer.dll at 0x{BaseModule.ToString("x")}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR getting module base: {ex}");
                return false;
            }
        }



 

        /// <summary>
        /// Copy 'n' bytes to unmanaged memory. Caller is responsible for freeing memory.
        /// </summary>
        public unsafe void ReadMemoryToBuffer(ulong addr, IntPtr bufPtr, int size)
        {
            Marshal.Copy(vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE)
                , 0, bufPtr, size);
        }

        /// <summary>
        /// Read a chain of pointers.
        /// </summary>
        public ulong AddressOf(ulong ptr, ulong[] offsets, uint flag = 0x0001) // Cache = 0x00 NoCache = 0x0001
        {
            ulong addr = AddressOf(ptr + offsets[0], flag);
            for (int i = 1; i < offsets.Length; i++)
            {
                addr = AddressOf(addr + offsets[i], flag);
            }
            return addr;
        }
        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        public ulong AddressOf(ulong ptr, uint flag = 0x0001) => ReadMemoryUlong(ptr, flag);
        public ulong ReadMemoryUlong(ulong addr, uint flag) // Cache = 0x00 NoCache = 0x0001
        {
            try
            {
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, flag), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        public long ReadMemoryLong(ulong addr) // read 8 bytes (int64)
        {
            try
            {
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        public int ReadMemoryInt(ulong addr) // read 4 bytes (int32)
        {
            try
            {
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        public uint ReadMemoryUint(ulong addr) // read 4 bytes (uint32)
        {
            try
            {
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        public float ReadMemoryFloat(ulong addr) // read 4 bytes (float)
        {
            try
            {
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        public double ReadMemoryDouble(ulong addr) // read 8 bytes (double)
        {
            try
            {
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }
        public bool ReadMemoryBool(ulong addr) // read 1 byte (bool)
        {
            try
            {
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, vmm.FLAG_NOCACHE), 0);
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        public T ReadMemoryStruct<T>(ulong addr) // Read structure from memory location
        {
            int size = Marshal.SizeOf(typeof(T));
            var mem = Marshal.AllocHGlobal(size); // alloc mem
            try
            {
                Marshal.Copy(
                    vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE), 
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
        public string ReadMemoryString(ulong addr, uint size) // read n bytes (string)
        {
            try
            {
                return Encoding.Default.GetString(
                    vmm.MemRead(_pid, addr, size, vmm.FLAG_NOCACHE));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// Read UnityEngineString structure
        /// </summary>
        public string ReadMemoryUnityString(ulong addr, uint flag = 0x0001)
        {
            try
            {
                var length = (uint)ReadMemoryInt(addr + 0x10);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + 0x14, length * 2, flag));
            }
            catch (Exception ex)
            {
                throw new DMAException($"ERROR reading memory at 0x{addr.ToString("X")}", ex);
            }
        }

        /// <summary>
        /// ToDo - Not sure if this is a good way to keep track if the process is still open
        /// </summary>
        public bool Heartbeat() // Make sure game is still there
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
