using System.Collections.Generic;
using System.IO;

namespace Classic
{
    public enum ObjectType
    {
        None = -1,
        Wall = 0,
        Fireball,
        Robot,
        Hostage,
        Player,
        Weapon,
        Camera,
        Powerup,
        Debris,
        ControlCenter,
        Flare,
        Clutter,
        Ghost,
        Light,
        Coop,
        Marker,
    }

    // Only the ids that carry extra data in the level file are named here.
    public enum MoveTypeID
    {
        None = 0,
        Physics = 1,
        Spinning = 3,
    }

    public enum ControlTypeID
    {
        None = 0,
        AI = 1,
        Explosion = 2,
        Weapon = 9,
        Morph = 11, // shares the AI payload
        Powerup = 13,
        Light = 14,
    }

    public enum RenderTypeID
    {
        None = 0,
        Polyobj = 1,
        Fireball = 2,
        Laser = 3,
        Hostage = 4,     // these three share the fireball (vclip) payload
        Powerup = 5,
        Morph = 6,       // shares the polyobj payload
        WeaponVClip = 7,
    }

    public class LevelObj
    {
        public ObjectType type;
        public byte id;
        public ControlTypeID controlType;
        public MoveTypeID moveType;
        public RenderTypeID renderType;
        public short segnum;
        public vms_vector pos;
        public Fix size;
    }

    public static class ClassicObjectReader
    {
        private const int NUM_AI_FLAGS = 11;
        private const int MAX_SUBMODELS = 10;

        private static void SkipVector(BinaryReader r)
        {
            r.BaseStream.Position += 12;
        }

        // Mirrors the object record layout used by Descent 1 and 2. Every field is read even
        // when unused, because the record is variable length and the next object starts right
        // after it.
        public static LevelObj Read(BinaryReader r, int gameDataVersion)
        {
            var o = new LevelObj();
            o.type = (ObjectType)r.ReadSByte();
            o.id = r.ReadByte();
            o.controlType = (ControlTypeID)r.ReadByte();
            o.moveType = (MoveTypeID)r.ReadByte();
            o.renderType = (RenderTypeID)r.ReadByte();
            r.ReadByte(); // flags
            if (gameDataVersion > 37)
                r.ReadByte(); // multiplayer only (D2X-XL)
            o.segnum = r.ReadInt16();
            o.pos.Read(r);
            r.BaseStream.Position += 9 * 4; // orientation matrix
            o.size.Read(r);
            r.ReadInt32(); // shields
            SkipVector(r); // last pos
            r.ReadByte(); // contains type
            r.ReadByte(); // contains id
            r.ReadByte(); // contains count

            switch (o.moveType)
            {
                case MoveTypeID.Physics:
                    SkipVector(r); // velocity
                    SkipVector(r); // thrust
                    r.ReadInt32(); // mass
                    r.ReadInt32(); // drag
                    r.ReadInt32(); // brakes
                    SkipVector(r); // angular velocity
                    SkipVector(r); // rotational thrust
                    r.ReadInt16(); // turnroll
                    r.ReadInt16(); // flags
                    break;
                case MoveTypeID.Spinning:
                    SkipVector(r); // spin rate
                    break;
            }

            switch (o.controlType)
            {
                case ControlTypeID.AI:
                case ControlTypeID.Morph:
                    r.ReadByte(); // behavior
                    r.BaseStream.Position += NUM_AI_FLAGS;
                    r.ReadInt16(); // hide segment
                    r.ReadInt16(); // hide index
                    r.ReadInt16(); // path length
                    r.ReadInt16(); // cur path index
                    if (gameDataVersion <= 25)
                        r.ReadInt32(); // unused follow path start/end (D1)
                    break;
                case ControlTypeID.Explosion:
                    r.ReadInt32(); // spawn time
                    r.ReadInt32(); // delete time
                    r.ReadInt16(); // delete object
                    break;
                case ControlTypeID.Powerup:
                    if (gameDataVersion >= 25)
                        r.ReadInt32(); // count
                    break;
                case ControlTypeID.Weapon:
                    r.ReadInt16(); // parent type
                    r.ReadInt16(); // parent num
                    r.ReadInt32(); // parent sig
                    break;
                case ControlTypeID.Light:
                    r.ReadInt32(); // intensity
                    break;
            }

            switch (o.renderType)
            {
                case RenderTypeID.Polyobj:
                case RenderTypeID.Morph:
                    r.ReadInt32(); // model num
                    r.BaseStream.Position += MAX_SUBMODELS * 3 * 2; // body angles
                    r.ReadInt32(); // flags
                    r.ReadInt32(); // texture override
                    break;
                case RenderTypeID.Fireball:
                case RenderTypeID.Hostage:
                case RenderTypeID.Powerup:
                case RenderTypeID.WeaponVClip:
                    r.ReadInt32(); // vclip num
                    r.ReadInt32(); // frame time
                    r.ReadByte(); // frame number
                    break;
            }

            return o;
        }
    }

    public static class ObjectNames
    {
        // Ids 0-27 are identical in Descent 1 and 2; 28+ are Descent 2 only, so one table
        // serves both games.
        private static readonly Dictionary<int, string> Powerups = new Dictionary<int, string>
        {
            { 0, "ExtraLife" },
            { 1, "Energy" },
            { 2, "ShieldBoost" },
            { 3, "Laser" },
            { 4, "BlueKey" },
            { 5, "RedKey" },
            { 6, "GoldKey" },
            { 10, "Concussion" },
            { 11, "ConcussionPack" },
            { 12, "QuadLaser" },
            { 13, "Vulcan" },
            { 14, "Spreadfire" },
            { 15, "Plasma" },
            { 16, "Fusion" },
            { 17, "ProximityBomb" },
            { 18, "HomingMissile" },
            { 19, "HomingMissilePack" },
            { 20, "SmartMissile" },
            { 21, "MegaMissile" },
            { 22, "VulcanAmmo" },
            { 23, "Cloak" },
            { 24, "Turbo" },
            { 25, "Invulnerability" },
            { 27, "Megawow" },
            { 28, "Gauss" },
            { 29, "Helix" },
            { 30, "Phoenix" },
            { 31, "Omega" },
            { 32, "SuperLaser" },
            { 33, "FullMap" },
            { 34, "Converter" },
            { 35, "AmmoRack" },
            { 36, "Afterburner" },
            { 37, "Headlight" },
            { 38, "FlashMissile" },
            { 39, "FlashMissilePack" },
            { 40, "GuidedMissile" },
            { 41, "GuidedMissilePack" },
            { 42, "SmartMine" },
            { 43, "MercuryMissile" },
            { 44, "MercuryMissilePack" },
            { 45, "EarthshakerMissile" },
            { 46, "BlueFlag" },
            { 47, "RedFlag" },
            { 48, "HoardOrb" },
        };

        // Numbers each base name from 1, so every writer refers to an object by the same name.
        // Returns names parallel to the object list.
        public static string[] AssignUniqueNames(IList<LevelObj> objects)
        {
            var counts = new Dictionary<string, int>();
            var names = new string[objects.Count];
            for (int i = 0; i < objects.Count; i++)
            {
                var baseName = GetBaseName(objects[i]);
                int n;
                counts.TryGetValue(baseName, out n);
                counts[baseName] = ++n;
                // Keep the ordinal readable when the base name already ends in a digit
                // (e.g. Robot17 -> Robot17_1 rather than Robot171).
                var sep = char.IsDigit(baseName[baseName.Length - 1]) ? "_" : "";
                names[i] = baseName + sep + n;
            }
            return names;
        }

        public static string GetBaseName(LevelObj o)
        {
            switch (o.type)
            {
                case ObjectType.Powerup:
                    string name;
                    return Powerups.TryGetValue(o.id, out name) ? name : "Powerup" + o.id;
                case ObjectType.Robot:
                    return "Robot" + o.id;
                case ObjectType.Hostage:
                    return "Hostage";
                case ObjectType.Player:
                    return "PlayerStart";
                case ObjectType.Coop:
                    return "CoopStart";
                case ObjectType.ControlCenter:
                    return "Reactor";
                case ObjectType.Weapon:
                    return "Weapon" + o.id;
                case ObjectType.Clutter:
                    return "Clutter";
                case ObjectType.Light:
                    return "Light";
                case ObjectType.Marker:
                    return "Marker";
                case ObjectType.Camera:
                    return "Camera";
                default:
                    return "Object" + (int)o.type;
            }
        }
    }
}
