using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;

namespace SharpRadar
{

    /// <summary>
    /// GUI Testing Structures, may change
    /// </summary>

    public class Player
    {
        public volatile bool IsPlayer = false;
        public volatile bool IsPMC = false;
        public volatile bool IsAlly = false;
        public volatile bool IsScav = false;
        public volatile bool IsScavBoss = false;
        public volatile bool IsPlayerScav = false;
        public volatile bool IsAlive = true;
        private volatile string _groupID;
        public string GroupID
        {
            get { return _groupID; }
        }
        private UnityEngine.Vector3 _pos = new UnityEngine.Vector3();
        public UnityEngine.Vector3 Position
        {
            set
            {
                Interlocked.Exchange(ref _pos.x, value.x);
                Interlocked.Exchange(ref _pos.y, value.y);
                Interlocked.Exchange(ref _pos.z, value.z);
            }
            get
                /* MSDN RE: Interlocked
                 * The Read method is unnecessary on 64-bit systems, because 64-bit read operations are already atomic. 
                 * On 32-bit systems, 64-bit read operations are not atomic unless performed using Read. 
                 */
            {
                return new UnityEngine.Vector3()
                {
                    x = _pos.x,
                    y = _pos.y,
                    z = _pos.z
                };
            }
        }

        public Player(string groupId)
        {
            _groupID = groupId;
        }
    }

    public struct MapPosition // Integer map coordinates for GUI
    {
        public int X;
        public int Y;
        public int Z;
    }

    /// <summary>
    /// EFT/Unity Structures (WIP)
    /// </summary>
    public struct GameObjectManager
    {
        public ulong LastTaggedNode; // 0x0

        public ulong TaggedNodes; // 0x8

        public ulong LastMainCameraTaggedNode; // 0x10

        public ulong MainCameraTaggedNodes; // 0x18

        public ulong LastActiveNode; // 0x20

        public ulong ActiveNodes; // 0x28

    }

    public struct BaseObject
    {
        public ulong previousObjectLink; //0x0000
        public ulong nextObjectLink; //0x0008
        public ulong obj; //0x0010
	};

    public struct ListInternal // Not sure if this is correct
    {
		public unsafe fixed byte pad_0x0000[0x20]; //0x0000
        public ulong firstEntry; //0x0020 
    }; //Size=0x0028

    public struct List // Not sure if this is correct
    {
		public unsafe fixed byte pad_0x0000[0x10]; //0x0000
        public ulong listBase; //0x0010    to ListInternal
        public int itemCount; //0x0018 
    }; //Size=0x001C

    public class Map
    {
        public readonly Bitmap MapFile;
        public readonly MapConfig ConfigFile;

        public Map(Bitmap map, MapConfig config)
        {
            MapFile = map;
            ConfigFile = config;
        }
    }

    public class MapConfig
    {
        [JsonProperty("x")]
        public float X;
        [JsonProperty("y")]
        public float Y;
        [JsonProperty("z")]
        public float Z;
        [JsonProperty("scale")]
        public float Scale;


        public static MapConfig LoadFromFile(string file)
        {
            using (var stream = File.OpenText(file))
            {
                var json = new JsonSerializer();
                return (MapConfig)json.Deserialize(stream, typeof(MapConfig));
            }
        }
    }

}
