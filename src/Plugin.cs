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
    [BepInPlugin("bchn.autopinezki", "AutoPinezki", "1.3.0")]
    public class Plugin : BaseUnityPlugin
    {
        static Plugin I;

        ConfigEntry<float> Range, GroupRadius;
        ConfigEntry<bool> Messages, DebugLog, NotesOnF7;
        ConfigEntry<float> NoteChance;
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

        // Menu texts per entry; swapped when the game language is known (Polish vs everything else).
        readonly List<(ConfigurationManagerAttributes a, string secPl, string secEn, string namePl, string nameEn, string descPl, string descEn)> texts =
            new List<(ConfigurationManagerAttributes, string, string, string, string, string, string)>();
        static readonly FieldInfo LocalizationInstance = AccessTools.Field(typeof(Localization), "m_instance");
        static bool pl;
        bool langKnown;
        float nextLangCheck;

        static readonly Dictionary<Cat, (string pl, string en)> CatNames = new Dictionary<Cat, (string, string)>
        {
            { Cat.Zbieractwo, ("Zbieractwo", "Gathering") },
            { Cat.Rudy, ("Rudy", "Ores") },
            { Cat.Lochy, ("Lochy", "Dungeons") },
            { Cat.Inne, ("Inne", "Other") },
        };

        // File keys are English and never change, so switching game language keeps your settings.
        ConfigEntry<T> Bind<T>(string secEn, string secPl, string key, string namePl, T value,
            string descEn, string descPl, AcceptableValueBase range = null)
        {
            var a = new ConfigurationManagerAttributes();
            texts.Add((a, secPl, secEn, namePl, key, descPl, descEn));
            return Config.Bind(secEn, key, value, new ConfigDescription(descEn, range, a));
        }

        void ApplyLanguage()
        {
            foreach (var t in texts)
            {
                t.a.Category = pl ? t.secPl : t.secEn;
                t.a.DispName = pl ? t.namePl : t.nameEn;
                t.a.Description = pl ? t.descPl : t.descEn;
            }
        }

        static string T(string polish, string english) => pl ? polish : english;

        void Awake()
        {
            I = this;
            Range = Bind("General", "Ogólne", "Range", "Zasięg", 50f,
                "Detection range in meters.", "Zasięg wykrywania w metrach.");
            GroupRadius = Bind("General", "Ogólne", "Group radius", "Promień grupowania", 40f,
                "Don't place a second pin of the same kind closer than this (meters).",
                "Nie stawiaj drugiej pinezki tego samego typu bliżej niż tyle metrów.");
            Messages = Bind("General", "Ogólne", "Messages", "Komunikaty", true,
                "Show a message on screen when a pin is placed.", "Komunikat na ekranie przy nowej pinezce.");
            DebugLog = Bind("General", "Ogólne", "Debug", "Debug", false,
                "Log names of unknown nearby locations to BepInEx/LogOutput.log.",
                "Loguj nazwy nieznanych lokacji w zasięgu (BepInEx/LogOutput.log).");
            NoteChance = Bind("Notes", "Notatki", "Note chance", "Szansa notatki", 0.1f,
                "Chance (0-1) that an automatic pin gets a random note, e.g. \"miedz ruda - check later\".",
                "Szansa (0-1), że do automatycznej pinezki dojdzie losowa notatka, np. \"miedz ruda - check later\".",
                new AcceptableValueRange<float>(0f, 1f));
            NotesOnF7 = Bind("Notes", "Notatki", "Notes on mark key", "Notatki pod klawiszem oznacz", true,
                "Marking an unknown object places a random note instead of its in-game name.",
                "Oznaczenie obiektu spoza listy stawia losową notatkę zamiast nazwy z gry.");
            KeyToggle = Bind("Keys", "Klawisze", "Toggle key", "Klawisz automatu", KeyCode.F8,
                "Turn automatic marking on/off.", "Włącz/wyłącz automatyczne oznaczanie.");
            KeyMark = Bind("Keys", "Klawisze", "Mark key", "Klawisz oznacz", KeyCode.F7,
                "Mark the object you are aiming at (up to 100 m).", "Oznacz obiekt, w który celujesz (do 100 m).");

            var defaultIcons = new Dictionary<Cat, Minimap.PinType>
            {
                { Cat.Zbieractwo, Minimap.PinType.Icon3 }, // dot
                { Cat.Rudy, Minimap.PinType.Icon2 },       // hammer
                { Cat.Lochy, Minimap.PinType.Icon4 },      // portal
                { Cat.Inne, Minimap.PinType.Icon0 },       // fire
            };
            foreach (var kv in defaultIcons)
            {
                var (cpl, cen) = CatNames[kv.Key];
                catEnabled[kv.Key] = Bind("Categories", "Kategorie", cen, cpl, true,
                    "Mark this whole category.", "Oznaczaj całą kategorię.");
                icons[kv.Key] = Bind("Icons", "Ikony", cen, cpl, kv.Value,
                    "Icon0=fire, Icon1=house, Icon2=hammer, Icon3=dot, Icon4=portal.",
                    "Icon0=ognisko, Icon1=dom, Icon2=młotek, Icon3=kropka, Icon4=portal.");
            }

            // One on/off per thing, e.g. "Items: Ores" -> "Copper" (menu: "Rzeczy: Rudy" -> "Miedź").
            var sources = Targets.Prefabs.Select(kv => new KeyValuePair<Target, string>(kv.Value, kv.Key))
                .Concat(Targets.Locations.Select(kv => new KeyValuePair<Target, string>(kv.Value, kv.Key + "*")));
            foreach (var g in sources.GroupBy(kv => kv.Key))
            {
                var t = g.Key;
                var (cpl, cen) = CatNames[t.Cat];
                string names = string.Join(", ", t.Names), objects = string.Join(", ", g.Select(kv => kv.Value));
                targetEnabled[t] = Bind("Items: " + cen, "Rzeczy: " + cpl, t.LabelEn, t.Label, !t.DefaultOff,
                    "Pin names: " + names + ". Game objects: " + objects,
                    "Na mapie np.: " + names + ". Obiekty w grze: " + objects);
            }
            ApplyLanguage();

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
            new Terminal.ConsoleCommand("pinezki", "AutoPinezki stats", (Terminal.ConsoleEvent)(args =>
            {
                if (I.mem == null) { args.Context.AddString("AutoPinezki: " + T("najpierw wejdź do świata.", "enter a world first.")); return; }
                var stats = I.mem.Stats();
                args.Context.AddString(stats.Count == 0
                    ? "AutoPinezki: " + T("jeszcze nic nie znalazłeś.", "nothing found yet.")
                    : "AutoPinezki: " + string.Join(", ", stats.Select(kv => kv.Key + ": " + kv.Value)));
            }));
        }

        void Update()
        {
            // Game language is only known after the game's Localization exists; recheck in case it changes.
            if (Time.unscaledTime >= nextLangCheck)
            {
                nextLangCheck = Time.unscaledTime + 2f;
                if (LocalizationInstance.GetValue(null) != null)
                {
                    bool isPl = Localization.instance.GetSelectedLanguage() == "Polish";
                    if (!langKnown || isPl != pl) { pl = isPl; langKnown = true; ApplyLanguage(); }
                }
            }

            var player = Player.m_localPlayer;
            var map = Minimap.instance;
            if (player == null || map == null || ZNet.instance == null) return;
            EnsureMemory(player);

            if ((bool)TakeInputMethod.Invoke(player, null))
            {
                if (ZInput.GetKeyDown(KeyToggle.Value))
                {
                    auto = !auto;
                    Say(T("Automat pinezek: ", "Auto pins: ") + (auto ? T("WŁ", "ON") : T("WYŁ", "OFF")));
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
            string name = Pick(t.Names);
            if (Random.value < NoteChance.Value) name += NoteSep + Pick(Targets.Notes);
            AddPin(map, name, icons[t.Cat].Value, pos);
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
                // Key stays the game name (memory); the pin shows a random note if enabled.
                MarkManual(map, name, NotesOnF7.Value ? Targets.Notes : new[] { name }, Minimap.PinType.Icon3, pos);
                return;
            }
            Say(T("Nic tu nie ma do oznaczenia", "Nothing here to mark"));
        }

        void MarkManual(Minimap map, string key, string[] names, Minimap.PinType icon, Vector3 pos)
        {
            // Same object (2 m) already remembered and its pin still on the map -> skip.
            if (mem.IsNear(key, pos.x, pos.z, 2f) && FindPin(map, names, pos) != null) { Say(T("Już oznaczone: ", "Already marked: ") + key); return; }
            if (!mem.IsNear(key, pos.x, pos.z, 2f)) Remember(key, pos);
            AddPin(map, Pick(names), icon, pos);
        }

        const string NoteSep = " - ";

        static string Pick(string[] arr) => arr[Random.Range(0, arr.Length)];

        void AddPin(Minimap map, string name, Minimap.PinType icon, Vector3 pos)
        {
            map.AddPin(pos, icon, name, true, false);
            if (Messages.Value) Say(T("Znalazłeś: ", "Found: ") + name + "!");
        }

        void Remember(string key, Vector3 pos)
        {
            mem.Add(key, pos.x, pos.z);
            File.AppendAllText(memPath, Memory.Line(key, pos.x, pos.z) + "\n");
        }

        // Key included so pins placed under older names still match.
        Minimap.PinData FindPin(Minimap map, Target t, Vector3 pos) => FindPin(map, t.Names.Append(t.Key).ToArray(), pos);

        Minimap.PinData FindPin(Minimap map, string[] names, Vector3 pos)
        {
            var pins = (List<Minimap.PinData>)PinsField.GetValue(map);
            float r = GroupRadius.Value;
            return pins.FirstOrDefault(p => p.m_save && Utils.DistanceXZ(p.m_pos, pos) < r
                && names.Any(n => p.m_name == n || p.m_name.StartsWith(n + NoteSep)));
        }

        static void Say(string text)
        {
            if (MessageHud.instance != null) MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, text);
        }
    }

    // Read by ConfigurationManager (F1 menu) via reflection, matched by class and field names.
    internal sealed class ConfigurationManagerAttributes
    {
        public string Category;
        public string DispName;
        public string Description;
    }
}
