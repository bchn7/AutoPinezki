using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace AutoPinezki
{
    // Remembered marks (key + XZ position). Pure logic, no Unity, so it can be tested.
    public class Memory
    {
        private readonly List<(string key, float x, float z)> _marks = new List<(string, float, float)>();

        public int Count => _marks.Count;

        public bool IsNear(string key, float x, float z, float radius)
        {
            float r2 = radius * radius;
            foreach (var m in _marks)
            {
                if (m.key != key) continue;
                float dx = m.x - x, dz = m.z - z;
                if (dx * dx + dz * dz < r2) return true;
            }
            return false;
        }

        public void Add(string key, float x, float z) => _marks.Add((key, x, z));

        public static string Line(string key, float x, float z) =>
            key + ";" + x.ToString(CultureInfo.InvariantCulture) + ";" + z.ToString(CultureInfo.InvariantCulture);

        public IEnumerable<string> ToLines() => _marks.Select(m => Line(m.key, m.x, m.z));

        public static Memory FromLines(IEnumerable<string> lines)
        {
            var mem = new Memory();
            foreach (var line in lines)
            {
                var p = line.Split(';');
                if (p.Length == 3 && p[0].Length > 0
                    && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                    && float.TryParse(p[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
                    mem.Add(p[0], x, z);
            }
            return mem;
        }

        public List<KeyValuePair<string, int>> Stats() =>
            _marks.GroupBy(m => m.key)
                  .Select(g => new KeyValuePair<string, int>(g.Key, g.Count()))
                  .OrderByDescending(kv => kv.Value)
                  .ToList();
    }
}
