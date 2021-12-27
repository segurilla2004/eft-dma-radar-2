using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
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
        public ConcurrentDictionary<string, Player> Players { get; private set; }

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
                var addr = AddressOf(_baseModule + 0x17F8D28);
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
            string currentPlayerGroupID = null; // Cache value to compare possible teammates
            for (uint i = 0; i < playerCnt; i++)
            {
                ulong playerBase = AddressOf(listBase + 0x20 + (i * 0x8));
                //var playerBones = AddressOf(playerBase + 0x530);
                var playerProfile = AddressOf(playerBase + 0x4b0);
                var playerId = AddressOf(playerProfile + 0x10);
                var playerIdString = ReadMemoryUnityString(playerId); // Player's Personal ID ToDo Testing
                var playerInfo = AddressOf(playerProfile + 0x28);
                var playerGroupId = AddressOf(playerInfo + 0x18);
                var playerGroupIdStr = ReadMemoryString(playerGroupId, 32); // Player's Group Affiliation ID ToDo testing

                /// ToDo - Get Player Location Transform -> Position
                /// 
                // 0x598 : x
                // 0x59C : y 
                // 0x5A0 : z
                // player + 0xA8] + 0x28] + 0x28] + 
                // PlayerBody >> SkeletonRootJoint >> List<Transform> >> BoneMatrix >> HumanBase
                //var playerTransform = AddressOf(new ulong[] { playerBase + 0xA8, 0x28, 0x28, 0x10, 0x20 });
                var playerTransform = AddressOf(playerBase, new ulong[] { 0xA8, 0x28, 0x28, 0x10, 0x20 });

                var playerIsAlive = true; // ToDo get value if player is alive or not
                var playerPos = GetPosition(new UIntPtr(playerTransform));
                Debug.WriteLine($"{playerPos.X}, {playerPos.Y}, {playerPos.Z}");
                if (i == 0) // Current player is always first
                {
                    currentPlayerGroupID = playerGroupIdStr;
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
                    var playerNickname = AddressOf(playerInfo + 0x10);
                    var nicknameStr = ReadMemoryUnityString(playerNickname); // Now working!
                    var playerSide = ReadMemoryInt(playerInfo + 0x58); // Scav, PMC, etc.
                    var playerType = PlayerType.Default; // ToDo parse player type and assign proper value
                    if (i == 0) // Current player is always first
                    {
                        playerType = PlayerType.CurrentPlayer;
                    }
                    else
                    {
                        if (playerGroupIdStr == currentPlayerGroupID) playerType = PlayerType.Teammate;
                        else
                        {
                            if (playerSide == 0x1 || playerSide == 0x2) playerType = PlayerType.PMC;
                            else if (playerSide == 0x4)
                            {
                                var regDate = ReadMemoryInt(playerInfo + 0x5C); // Bots wont have 'reg date'
                                if (regDate == 0) playerType = PlayerType.AIScav;
                                else playerType = PlayerType.PlayerScav;
                            }
                        }
                    }
                    Debug.WriteLine($"{nicknameStr} {playerIdString} ({playerType}) (grp: {playerGroupIdStr}) entered the game world.");
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

        private unsafe Vector3 GetPosition(UIntPtr transform)
        {
            ulong pMatricesBuf = 0;
            ulong pIndicesBuf = 0;
            int index = 0;

            var offsets = GetPositionOffset(transform);
            pMatricesBuf = offsets.Item1;
            pIndicesBuf = offsets.Item2;
            index = offsets.Item3;

            Vector4 result = *(Vector4*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index);
            int index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index);

            Vector4 xmmword_1410D1340 = new Vector4(-2.0f, 2.0f, -2.0f, 0.0f);
            Vector4 xmmword_1410D1350 = new Vector4(2.0f, -2.0f, -2.0f, 0.0f);
            Vector4 xmmword_1410D1360 = new Vector4(-2.0f, -2.0f, 2.0f, 0.0f);

            while (index_relation >= 0)
            {
                Matrix34 matrix34 = *(Matrix34*)((UInt64)pMatricesBuf + 0x30 * (UInt64)index_relation);

                Vector4 v10 = matrix34.vec2 * result;
                Vector4 v11 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(0)));
                Vector4 v12 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(85)));
                Vector4 v13 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-114)));
                Vector4 v14 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-37)));
                Vector4 v15 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(-86)));
                Vector4 v16 = (Vector4)(Shuffle(matrix34.vec1, (ShuffleSel)(113)));
                result = (((((((v11 * xmmword_1410D1350) * v13) - ((v12 * xmmword_1410D1360) * v14)) * Shuffle(v10, (ShuffleSel)(-86))) +
                    ((((v15 * xmmword_1410D1360) * v14) - ((v11 * xmmword_1410D1340) * v16)) * Shuffle(v10, (ShuffleSel)(85)))) +
                    (((((v12 * xmmword_1410D1340) * v16) - ((v15 * xmmword_1410D1350) * v13)) * Shuffle(v10, (ShuffleSel)(0))) + v10)) + matrix34.vec0);
                index_relation = *(int*)((UInt64)pIndicesBuf + 0x4 * (UInt64)index_relation);
            }

            //Marshal.FreeHGlobal(new IntPtr((long)pMatricesBufPtr.ToUInt64()));
            //Marshal.FreeHGlobal(new IntPtr((long)pIndicesBufPtr.ToUInt64()));

            return new Vector3(result.X, result.Z, result.Y);
        }
        private unsafe Tuple<ulong, ulong, int> GetPositionOffset(UIntPtr transform)
        {
            UIntPtr transform_internal = GetUintPtr(transform, new int[] { 0x10 });

            UIntPtr pMatrix = GetUintPtr(transform_internal, new int[] { 0x38 });
            int index = ReadMemoryInt((ulong)transform_internal + 0x40);

            UIntPtr matrix_list_base = GetUintPtr(pMatrix, new int[] { 0x18 });

            UIntPtr dependency_index_table_base = GetUintPtr(pMatrix, new int[] { 0x20 });

            UIntPtr pMatricesBufPtr = new UIntPtr((ulong)Marshal.AllocHGlobal(sizeof(Matrix34) * index + sizeof(Matrix34)).ToInt64()); // sizeof(Matrix34) == 48
            void* pMatricesBuf = pMatricesBufPtr.ToPointer();
            ReadMemoryToBuffer(matrix_list_base.ToUInt64(), pMatricesBufPtr, sizeof(Matrix34) * index + sizeof(Matrix34));

            UIntPtr pIndicesBufPtr = new UIntPtr((ulong)Marshal.AllocHGlobal(sizeof(int) * index + sizeof(int)).ToInt64());
            void* pIndicesBuf = pIndicesBufPtr.ToPointer();
            ReadMemoryToBuffer(dependency_index_table_base.ToUInt64(), pIndicesBufPtr, sizeof(int) * index + sizeof(int));

            return Tuple.Create((UInt64)pMatricesBuf, (UInt64)pIndicesBuf, index);
        }

        private static unsafe Vector4 Shuffle(Vector4 v1, ShuffleSel sel)
        {
            var ptr = (float*)&v1;
            var idx = (int)sel;
            return new Vector4(*(ptr + ((idx >> 0) & 0x3)), *(ptr + ((idx >> 2) & 0x3)), *(ptr + ((idx >> 4) & 0x3)),
                *(ptr + ((idx >> 6) & 0x3)));
        }

        /// <summary>
        /// Copy 'n' bytes to unmanaged memory. Caller is responsible for freeing memory.
        /// </summary>
        private unsafe void ReadMemoryToBuffer(ulong addr, UIntPtr bufPtr, int size)
        {
            var ptr = new IntPtr(bufPtr.ToPointer());
            Marshal.Copy(vmm.MemRead(_pid, addr, (uint)size, vmm.FLAG_NOCACHE)
                , 0, ptr, size);
        }

        /// <summary>
        /// Return a managed UIntPtr Address.
        /// </summary>
        private UIntPtr GetUintPtr(UIntPtr addr, int[] offsets)
        {
            var result = AddressOf(addr.ToUInt64() + (uint)offsets[0]);
            for (int i = 1; i < offsets.Length; i++)
            {
                result = AddressOf(result + (uint)offsets[i]);
            }
            return new UIntPtr(result);
        }

        /// <summary>
        /// Read a chain of pointers.
        /// </summary>
        private ulong AddressOf(ulong ptr, ulong[] offsets)
        {
            ulong addr = AddressOf(ptr + offsets[0]);
            for (int i = 1; i < offsets.Length; i++)
            {
                addr = AddressOf(addr + offsets[i]);
            }
            return addr;
        }
        /// <summary>
        /// Resolves a pointer and returns the memory address it points to.
        /// </summary>
        private ulong AddressOf(ulong ptr) => ReadMemoryUlong(ptr);
        private ulong ReadMemoryUlong(ulong addr)
        {
            try
            {
                return BitConverter.ToUInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToInt64(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToUInt32(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToSingle(vmm.MemRead(_pid, addr, 4, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToDouble(vmm.MemRead(_pid, addr, 8, vmm.FLAG_NOCACHE), 0);
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
                return BitConverter.ToBoolean(vmm.MemRead(_pid, addr, 1, vmm.FLAG_NOCACHE), 0);
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
        private string ReadMemoryString(ulong addr, uint size) // read n bytes (string)
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
        private string ReadMemoryUnityString(ulong addr)
        {
            try
            {
                var length = (uint)ReadMemoryInt(addr + 0x10);
                return Encoding.Unicode.GetString(
                    vmm.MemRead(_pid, addr + 0x14, length * 2, vmm.FLAG_NOCACHE));
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
