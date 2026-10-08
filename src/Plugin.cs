using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace AutoPinezki
{
    [BepInPlugin("bchn.autopinezki", "AutoPinezki", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        static Plugin I;

        ConfigEntry<float> Range, GroupRadius;
        ConfigEntry<bool> Messages, DebugLog;
        ConfigEntry<KeyCode> KeyToggle, KeyMark;
        readonly Dictionary<Cat, ConfigEntry<bool>> catEnabled = new Dictionary<Cat, ConfigEntry<bool>>();
        readonly Dictionary<Target, ConfigEntry<bool>> targetEnabled = new Dictionary<Target, ConfigEntry<bool>>();
        readonly Dictionary<Cat, ConfigEntry<Minimap.PinType>> icons = new Dictionary<Cat, ConfigEntry<Minimap.PinType>>();

        static readonly List<KeyValuePair<ZNetView, Target>> Tracked = new List<KeyValuePair<ZNetView, Target>>();
        static readonly FieldInfo LocationsField = AccessTools.Field(typeof(Location), "s_allLocations");
        static readonly FieldInfo PinsField = AccessTools.Field(typeof(Minimap), "m_pins");
        static readonly MethodInfo TakeInputMethod = AccessTools.Method(typeof(Player), "TakeInput");

        Memory mem;
        string memPath;
        float nextTick;
        bool auto = true;
        readonly HashSet<string> debugSeen = new HashSet<string>();

        void Awake()
        {
            I = this;
            Range = Config.Bind("Ogólne", "Range", 50f, "Zasięg wykrywania w metrach.");
            GroupRadius = Config.Bind("Ogólne", "GroupRadius", 40f, "Nie stawiaj drugiej pinezki tego samego typu bliżej niż tyle metrów.");
            Messages = Config.Bind("Ogólne", "Komunikaty", true, "Komunikat na ekranie przy nowej pinezce.");
            KeyToggle = Config.Bind("Klawisze", "KlawiszAutomat", KeyCode.F8, "Włącz/wyłącz automatyczne oznaczanie.");
            KeyMark = Config.Bind("Klawisze", "KlawiszOznacz", KeyCode.F7, "Oznacz obiekt, w który celujesz (do 100 m).");
            DebugLog = Config.Bind("Ogólne", "Debug", false, "Loguj nazwy nieznanych lokacji w zasięgu (BepInEx/LogOutput.log).");

            var defaultIcons = new Dictionary<Cat, Minimap.PinType>
            {
                { Cat.Zbieractwo, Minimap.PinType.Icon3 }, // dot
                { Cat.Rudy, Minimap.PinType.Icon2 },       // hammer
                { Cat.Lochy, Minimap.PinType.Icon4 },      // portal
                { Cat.Inne, Minimap.PinType.Icon0 },       // fire
            };
            foreach (var kv in defaultIcons)
            {
                catEnabled[kv.Key] = Config.Bind("Kategorie", kv.Key.ToString(), true, "Oznaczaj kategorię " + kv.Key + ".");
                icons[kv.Key] = Config.Bind("Ikony", kv.Key.ToString(), kv.Value, "Icon0=ognisko, Icon1=dom, Icon2=młotek, Icon3=kropka, Icon4=portal.");
            }

            // One on/off per thing, e.g. "Rzeczy: Zbieractwo" -> "boruwki".
            var sources = Targets.Prefabs.Select(kv => new KeyValuePair<Target, string>(kv.Value, kv.Key))
                .Concat(Targets.Locations.Select(kv => new KeyValuePair<Target, string>(kv.Value, kv.Key + "*")));
            foreach (var g in sources.GroupBy(kv => kv.Key))
                targetEnabled[g.Key] = Config.Bind("Rzeczy: " + g.Key.Cat, g.Key.Key, true,
                    "Oznaczaj: " + string.Join(", ", g.Select(kv => kv.Value)));

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Logger.LogInfo("AutoPinezki loaded");
        }

        [HarmonyPostfix, HarmonyPatch(typeof(ZNetView), "Awake")]
        static void OnViewAwake(ZNetView __instance)
        {
            if (Targets.Prefabs.TryGetValue(Utils.GetPrefabName(__instance.gameObject), out var t))
                Tracked.Add(new KeyValuePair<ZNetView, Target>(__instance, t));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(Terminal), "InitTerminal")]
        static void OnInitTerminal()
        {
            new Terminal.ConsoleCommand("pinezki", "statystyki AutoPinezki", (Terminal.ConsoleEvent)(args =>
            {
                if (I.mem == null) { args.Context.AddString("AutoPinezki: najpierw wejdź do świata."); return; }
                var stats = I.mem.Stats();
                args.Context.AddString(stats.Count == 0
                    ? "AutoPinezki: jeszcze nic nie znalazłeś."
                    : "AutoPinezki: " + string.Join(", ", stats.Select(kv => kv.Key + ": " + kv.Value)));
            }));
        }

        void Update()
        {
            var player = Player.m_localPlayer;
            var map = Minimap.instance;
            if (player == null || map == null || ZNet.instance == null) return;
            EnsureMemory(player);

            if ((bool)TakeInputMethod.Invoke(player, null))
            {
                if (ZInput.GetKeyDown(KeyToggle.Value))
                {
                    auto = !auto;
                    Say("Automat pinezek: " + (auto ? "WŁ" : "WYŁ"));
                }
                if (ZInput.GetKeyDown(KeyMark.Value)) MarkLookedAt(player, map);
            }

            if (Time.time < nextTick) return;
            nextTick = Time.time + 2f;
            Scan(player, map);
        }

        void EnsureMemory(Player player)
        {
            string dir = Path.Combine(Paths.ConfigPath, "AutoPinezki");
            string path = Path.Combine(dir, Safe(ZNet.instance.GetWorldName()) + "_" + Safe(player.GetPlayerName()) + ".txt");
            if (path == memPath) return;
            Directory.CreateDirectory(dir);
            memPath = path;
            mem = File.Exists(path) ? Memory.FromLines(File.ReadAllLines(path)) : new Memory();
            Logger.LogInfo("AutoPinezki: pamięć " + path + " (" + mem.Count + " wpisów)");
        }

        static string Safe(string s) =>
            new string((s ?? "").Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c).ToArray());

        void Scan(Player player, Minimap map)
        {
            Vector3 pp = player.transform.position;
            float range = Range.Value;
            var groupReady = new Dictionary<Minimap.PinData, bool>();

            Tracked.RemoveAll(e => e.Key == null);
            foreach (var e in Tracked)
            {
                Vector3 pos = e.Key.transform.position;
                if (Utils.DistanceXZ(pos, pp) > range) continue;
                var t = e.Value;
                var pick = t.Cat == Cat.Zbieractwo ? e.Key.GetComponent<Pickable>() : null;
                if (pick != null) // non-pickables (e.g. wild Beehive) skip the respawn filter
                {
                    if (pick.m_respawnTimeMinutes <= 0f) continue;
                    var pin = FindPin(map, t, pos);
                    if (pin != null && targetEnabled[t].Value)
                    {
                        groupReady.TryGetValue(pin, out bool ready);
                        groupReady[pin] = ready || !pick.GetPicked();
                    }
                }
                if (auto) TryMark(map, t, pos);
            }

            if (catEnabled[Cat.Zbieractwo].Value)
                foreach (var kv in groupReady) kv.Key.m_checked = !kv.Value;

            if (!(LocationsField.GetValue(null) is List<Location> locations)) return;
            foreach (var loc in locations)
            {
                if (loc == null) continue;
                Vector3 pos = loc.transform.position;
                if (Utils.DistanceXZ(pos, pp) > range) continue;
                string name = Utils.GetPrefabName(loc.gameObject);
                var t = Targets.FindLocation(name);
                if (t == null)
                {
                    if (DebugLog.Value && debugSeen.Add(name)) Logger.LogInfo("AutoPinezki debug: nieznana lokacja " + name);
                    continue;
                }
                if (auto) TryMark(map, t, pos);
            }
        }

        void TryMark(Minimap map, Target t, Vector3 pos)
        {
            if (!catEnabled[t.Cat].Value || !targetEnabled[t].Value) return;
            if (mem.IsNear(t.Key, pos.x, pos.z, GroupRadius.Value)) return;
            Remember(t.Key, pos);
            if (FindPin(map, t, pos) != null) return; // e.g. shared via cartography table
            AddPin(map, t.Names[Random.Range(0, t.Names.Length)], icons[t.Cat].Value, pos);
        }

        void MarkLookedAt(Player player, Minimap map)
        {
            var cam = GameCamera.instance;
            if (cam == null) return;
            var hits = Physics.RaycastAll(cam.transform.position, cam.transform.forward, 100f, ~0, QueryTriggerInteraction.Ignore)
                              .OrderBy(h => h.distance);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Player>() == player) continue;

                var view = hit.collider.GetComponentInParent<ZNetView>();
                if (view != null && Targets.Prefabs.TryGetValue(Utils.GetPrefabName(view.gameObject), out var t))
                {
                    MarkManual(map, t.Key, t.Names, icons[t.Cat].Value, view.transform.position);
                    return;
                }

                var hover = hit.collider.GetComponentInParent<Hoverable>();
                string name = hover != null ? Localization.instance.Localize(hover.GetHoverName()) : null;
                if (string.IsNullOrEmpty(name) && view != null) name = Utils.GetPrefabName(view.gameObject);
                if (string.IsNullOrEmpty(name)) break; // terrain, rocks, etc.
                Vector3 pos = view != null ? view.transform.position : hit.point;
                MarkManual(map, name, new[] { name }, Minimap.PinType.Icon3, pos);
                return;
            }
            Say("Nic tu nie ma do oznaczenia");
        }

        void MarkManual(Minimap map, string key, string[] names, Minimap.PinType icon, Vector3 pos)
        {
            if (FindPin(map, names, pos) != null) { Say("Już oznaczone: " + key); return; }
            if (!mem.IsNear(key, pos.x, pos.z, GroupRadius.Value)) Remember(key, pos);
            AddPin(map, names[Random.Range(0, names.Length)], icon, pos);
        }

        void AddPin(Minimap map, string name, Minimap.PinType icon, Vector3 pos)
        {
            map.AddPin(pos, icon, name, true, false);
            if (Messages.Value) Say("Znalazłeś: " + name + "!");
        }

        void Remember(string key, Vector3 pos)
        {
            mem.Add(key, pos.x, pos.z);
            File.AppendAllText(memPath, Memory.Line(key, pos.x, pos.z) + "\n");
        }

        Minimap.PinData FindPin(Minimap map, Target t, Vector3 pos) => FindPin(map, t.Names, pos);

        Minimap.PinData FindPin(Minimap map, string[] names, Vector3 pos)
        {
            var pins = (List<Minimap.PinData>)PinsField.GetValue(map);
            float r = GroupRadius.Value;
            return pins.FirstOrDefault(p => p.m_save && names.Contains(p.m_name) && Utils.DistanceXZ(p.m_pos, pos) < r);
        }

        static void Say(string text)
        {
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }
    }
}
