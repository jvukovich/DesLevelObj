using Classic;
using System.Globalization;
using System.IO;

namespace DesLevelObj
{
    // Writes one .obj per level object, each a named placeholder cube, so every pickup and spawn
    // imports as its own asset that can be found by name and swapped for something else.
    public static class ObjectsToObj
    {
        private const float DefaultHalfSize = 2.0f;

        // Files are prefixed so the placeholder assets sort and search apart from level geometry.
        private const string FilePrefix = "__";

        // Corner offsets of a unit cube, as multipliers of the half size.
        private static readonly int[,] CubeCorners = new int[8, 3] {
            {-1,-1,-1}, {1,-1,-1}, {1,1,-1}, {-1,1,-1},
            {-1,-1,1},  {1,-1,1},  {1,1,1},  {-1,1,1},
        };

        // Wound counter-clockwise as seen from outside, so normals face out.
        private static readonly int[,] CubeTris = new int[12, 3] {
            {0,3,2}, {0,2,1}, // -z
            {4,5,6}, {4,6,7}, // +z
            {0,4,7}, {0,7,3}, // -x
            {1,2,6}, {1,6,5}, // +x
            {0,1,5}, {0,5,4}, // -y
            {3,7,6}, {3,6,2}, // +y
        };

        private static string F(float v)
        {
            return v.ToString(CultureInfo.InvariantCulture);
        }

        private static float HalfSize(LevelObj o)
        {
            var half = o.size.ToFloat();
            return half > 0 ? half : DefaultHalfSize;
        }

        public static void Convert(MainForm mainForm, ClassicLevel lvl, string outDir)
        {
            if (lvl.ObjectReadError != null)
                mainForm.Log("Could not read level objects: " + lvl.ObjectReadError);
            if (lvl.Objects.Count == 0)
            {
                mainForm.Log("No level objects to export");
                return;
            }

            var names = ObjectNames.AssignUniqueNames(lvl.Objects);
            WritePerObjectFiles(lvl, names, outDir);
            mainForm.Log("Wrote " + lvl.Objects.Count + " object .obj files to " + outDir);
        }

        // One file per object. OBJ has no notion of a transform, so the cube keeps its world
        // position - otherwise the location would be lost entirely.
        public static void WritePerObjectFiles(ClassicLevel lvl, string[] names, string dir)
        {
            Directory.CreateDirectory(dir);
            for (int i = 0; i < lvl.Objects.Count; i++)
            {
                var o = lvl.Objects[i];
                float cx, cy, cz;
                // Shares LevelToObj's transform, with no recentering, so the placeholders land
                // inside the mine geometry and need no rotating after import.
                LevelToObj.ToExportSpace(o.pos, out cx, out cy, out cz);
                var path = Path.Combine(dir, FilePrefix + names[i] + ".obj");
                using (var f = new StreamWriter(path))
                    WriteCube(f, names[i], cx, cy, cz, HalfSize(o));
            }
        }

        private static void WriteCube(TextWriter f, string name, float cx, float cy, float cz,
            float half)
        {
            // "o" alone is not enough for some importers, which split on "g" instead.
            f.WriteLine("o " + name);
            f.WriteLine("g " + name);
            for (int i = 0; i < 8; i++)
                f.WriteLine("v " + F(cx + CubeCorners[i, 0] * half) + " " +
                    F(cy + CubeCorners[i, 1] * half) + " " +
                    F(cz + CubeCorners[i, 2] * half));
            f.WriteLine("s off");
            for (int i = 0; i < 12; i++)
                f.WriteLine("f " + (1 + CubeTris[i, 0]) + " " +
                    (1 + CubeTris[i, 1]) + " " +
                    (1 + CubeTris[i, 2]));
        }
    }
}
