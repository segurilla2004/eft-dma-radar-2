using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;
using System.Threading;

namespace SharpRadar
{

    // GUI Testing Structures, may change

    /// <summary>
    /// Class containing Game Player Data. Use lock() when accessing instances of this class.
    /// </summary>
    public class Player
    {
        public readonly string Name;
        public readonly PlayerType Type;
        public readonly string GroupID;
        public bool IsAlive = true;
        public UnityEngine.Vector3 Position = new UnityEngine.Vector3(0, 0, 0);

        public Player(string name, string groupId, PlayerType type = PlayerType.Default)
        {
            Name = name;
            GroupID = groupId;
            Type = type;
        }
    }

    /// <summary>
    /// Defines Player Unit Type (Player,PMC,Scav,etc.)
    /// </summary>
    public enum PlayerType
    {
        Default,
        CurrentPlayer,
        Teammate,
        PMC,
        AIScav,
        AIBoss,
        PlayerScav
    }

    /// <summary>
    /// Defines map position for the UI Map.
    /// </summary>
    public struct MapPosition
    {
        public int X;
        public int Y;
        public int Height; // Z
    }

    /// <summary>
    /// Defines a .PNG Map File and corresponding .JSON config file from \\Maps folder.
    /// </summary>
    public class Map
    {
        public readonly string Name;
        public readonly Bitmap MapFile;
        public readonly MapConfig ConfigFile;

        public Map(string name, Bitmap map, MapConfig config)
        {
            Name = name;
            MapFile = map;
            ConfigFile = config;
        }
    }

    /// <summary>
    /// Defines a .JSON Map Config File
    /// </summary>
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

    // EFT/Unity Structures (WIP)

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

    // ToDo UnityEngineString

}
