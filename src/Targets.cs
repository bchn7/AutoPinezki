using System.Collections.Generic;

namespace AutoPinezki
{
    public enum Cat { Zbieractwo, Rudy, Lochy, Inne }

    public class Target
    {
        public readonly string Key;     // memory/grouping id; keep stable or places get marked again
        public readonly string Label;   // normal name shown in the config menu
        public readonly Cat Cat;
        public readonly string[] Names; // pin names on the map, one picked at random
        public bool DefaultOff;         // config toggle starts disabled

        public Target(Cat cat, string key, string label, params string[] names) { Cat = cat; Key = key; Label = label; Names = names; }
    }

    public static class Targets
    {
        // Exact prefab name -> target (pickables and ore deposits).
        public static readonly Dictionary<string, Target> Prefabs = new Dictionary<string, Target>();

        // Location name prefix -> target (dungeons and other locations). Turn on Debug in config to log unknown names.
        public static readonly List<KeyValuePair<string, Target>> Locations = new List<KeyValuePair<string, Target>>();

        static void P(Target t, params string[] prefabs) { foreach (var p in prefabs) Prefabs[p] = t; }
        static void L(Target t, params string[] prefixes) { foreach (var p in prefixes) Locations.Add(new KeyValuePair<string, Target>(p, t)); }

        // Random notes: appended to auto pins now and then, and used by F7 for unknown objects.
        public static readonly string[] Notes =
        {
            "go tu", "tu go", "come tutaj", "wracamy later", "back later", "return here",
            "check this", "check cave", "check later", "loot tutaj", "loot tam", "free loot", "darmowy loot",
            "need iron", "need silver", "need loot", "duzo iron", "duzo srebra", "big iron", "big silver",
            "bad idea", "zly pomysl", "dont go", "dont go tutaj", "nie isc", "maybe boss", "maybe loot",
            "probably silver", "probably iron", "unknown", "unknown location", "todo", "todo boss",
            "todo explore", "todo loot", "wip", "wip baza", "fixme", "fixme troll", "bug", "bug jaskinia",
        };

        public static Target FindLocation(string name)
        {
            foreach (var kv in Locations)
                if (name.StartsWith(kv.Key)) return kv.Value;
            return null;
        }

        static Targets()
        {
            const Cat Z = Cat.Zbieractwo, R = Cat.Rudy, D = Cat.Lochy, I = Cat.Inne;

            // Off by default: too many of them. Enable per item in config.
            P(new Target(Z, "grzyp", "Grzyby", "grzyp") { DefaultOff = true }, "Pickable_Mushroom");
            P(new Target(Z, "zułty grzyp", "Żółte grzyby", "zułty grzyp") { DefaultOff = true }, "Pickable_Mushroom_yellow");
            P(new Target(Z, "niebieski grzyp", "Niebieskie grzyby", "niebieski grzyp") { DefaultOff = true }, "Pickable_Mushroom_blue");

            P(new Target(Z, "malinki", "Maliny", "maliny"), "RaspberryBush");
            P(new Target(Z, "boruwki", "Borówki", "borowki"), "BlueberryBush");
            P(new Target(Z, "morozki", "Moroszki", "morozki"), "CloudberryBush");
            P(new Target(Z, "linkgoberki", "Borówki brusznice", "linkgoberki", "brusznica czy cos"), "LingonberryBush");
            P(new Target(Z, "ostek", "Oset", "ostek"), "Pickable_Thistle");
            P(new Target(Z, "marhewka", "Dzika marchew", "marchewka"), "Pickable_SeedCarrot");
            P(new Target(Z, "rzepka", "Dzika rzepa", "rzepa"), "Pickable_SeedTurnip");
            P(new Target(Z, "cebulka", "Dzika cebula", "cebulka"), "Pickable_SeedOnion", "OnionSeeds");
            P(new Target(Z, "jarmusz", "Dziki jarmuż", "jarmuz"), "Pickable_SeedKale");
            P(new Target(Z, "len dziki", "Dziki len", "len"), "Pickable_Flax_Wild");
            P(new Target(Z, "jenczmień", "Dziki jęczmień", "jenczmien"), "Pickable_Barley_Wild");
            P(new Target(Z, "jotun pafy", "Jotun Puffs", "jotun pafy", "pufy jotuna"), "Pickable_Mushroom_JotunPuffs");
            P(new Target(Z, "magekapelusz", "Magecap", "magekapelusz", "czapka maga"), "Pickable_Mushroom_Magecap");
            P(new Target(Z, "dymek", "Smoke Puff", "dymek", "smołk pufek"), "Pickable_SmokePuff");
            P(new Target(Z, "paprotka", "Paprocie (Fiddlehead)", "paprotka"), "Pickable_Fiddlehead");
            P(new Target(Z, "winogronka", "Vineberry", "winogronka"), "VineAsh");
            P(new Target(Z, "jajo sempa", "Jaja voltur", "jajo sempa", "jajko ptaszora"), "Pickable_VoltureEgg");
            P(new Target(Z, "galaretka krulewska", "Royal Jelly", "galaretka krolewska"), "Pickable_RoyalJelly");
            P(new Target(Z, "jajo smoka", "Smocze jaja", "jajo smoka"), "Pickable_DragonEgg");
            P(new Target(Z, "ul dziki", "Dzikie ule", "ul"), "Beehive");

            P(new Target(R, "copper", "Miedź", "miedz", "miedz ruda", "copper miedz", "copper ruda"), "rock4_copper", "MineRock_Copper");
            P(new Target(R, "tin cyna", "Cyna", "tin cyna", "tin ruda"), "MineRock_Tin", "Pickable_Tin");
            P(new Target(R, "silwer", "Srebro", "srebro", "silver srebro", "silver ruda", "silwer ruda"), "silvervein", "rock3_silver");
            P(new Target(R, "ajron w błocie", "Żelazo (złom w błocie)", "zelazo", "iron zelazo", "iron ruda", "iron bagno", "ajron w błocie"),
              "mudpile", "mudpile2", "Pickable_BogIronOre", "MineRock_Iron");
            P(new Target(R, "obsydjan", "Obsydian", "obsydian", "obsydjan"), "MineRock_Obsidian", "Pickable_Obsidian");
            P(new Target(R, "meteoryt", "Meteoryty", "meteoryt", "meteor kamien", "kamien z nieba"), "MineRock_Meteorite", "Pickable_Meteorite");
            P(new Target(R, "flametal", "Flametal", "flametal", "flametal ruda"), "FlametalRockstand", "LeviathanLava");
            P(new Target(R, "czacha giganta", "Szczątki gigantów", "czacha giganta", "gigantyczne kości"), "giant_skull", "giant_ribs", "giant_brain");
            P(new Target(R, "złoto gold", "Złoto", "zloto", "gold zloto", "gold ruda"), "goldvein");
            P(new Target(R, "siarka", "Siarka", "siarka", "sulfur siarka"), "Pickable_SulfurRock");
            P(new Target(R, "smoła tar", "Smoła", "smola", "tar smoła"), "Pickable_Tar", "Pickable_TarBig");
            P(new Target(R, "kryształ", "Kryształy", "krysztal", "crystal krysztal", "crystal jaskinia"), "Pickable_MountainCaveCrystal");

            // Order matters: first matching prefix wins.
            L(new Target(D, "dung bagno", "Zatopiona krypta (Sunken Crypt)", "dung bagno"), "SunkenCrypt");
            L(new Target(D, "dung krypta", "Krypta (Burial Chamber)", "dung krypta"), "Crypt");
            L(new Target(D, "dung zimny", "Lodowa jaskinia (Frost Cave)", "dung zimny", "frost jaskinia", "frost cave", "zimna jaskinia", "jaskinia", "jaskina"), "MountainCave");
            L(new Target(D, "dung dwergi", "Zainfekowana kopalnia (Infested Mine)", "dung dwergi", "infested kopalnia", "infested mine", "kopalnia robalow", "dvergr kopalnia", "dvergr mine"),
              "Mistlands_DvergrTownEntrance");
            L(new Target(D, "trol jaskinia", "Jaskinia trolla", "trol jaskinia", "troll jaskinia", "troll cave"), "TrollCave");
            L(new Target(D, "spalony zamek", "Zwęglona forteca (Charred Fortress)", "charred zamek", "charred fortress"), "CharredFortress");
            L(new Target(D, "śmierdząca dziura", "Gnijąca dziura (Putrid Hole)", "smierdzaca dziura", "putrid dziura", "putrid hole", "dziura smrodu"), "MorgenHole");

            L(new Target(I, "boss tu", "Ołtarze bossów", "boss tu", "boss tutaj", "tu boss", "boss altar", "boss oltarz", "boss ołtarz",
                "boss spawn", "boss location", "boss chyba", "boss napewno", "duzy boss", "big boss"),
              "Eikthyrnir", "GDKing", "Bonemass", "Dragonqueen", "GoblinKing", "Mistlands_DvergrBossEntrance", "FaderLocation");
            L(new Target(I, "sklepik haldora", "Haldor (kupiec)", "haldor sklep", "haldor shop", "haldor sklepik", "haldora"), "Vendor_BlackForest");
            L(new Target(I, "hildirka", "Hildir (kupiec)", "hildr oboz", "hildir oboz", "hildir camp"), "Hildir_camp");
            L(new Target(I, "wiedzma", "Wiedźma z bagien", "witch wiedzma", "witch wiedźma", "witch camp", "witch oboz", "wiedzma camp"), "BogWitch_Camp");
            L(new Target(I, "kamień z runamy", "Kamienie runiczne", "kamień z runamy", "runy jakieś"), "Runestone_");
            L(new Target(I, "obuz goblinuw", "Obozy goblinów (fulingów)", "oboz goblinow", "goblin oboz", "goblin camp", "goblin obuz", "fuling oboz", "fuling camp"), "GoblinCamp");
            L(new Target(I, "obuz grejdwarfuw", "Obozy greydwarfów", "oboz grejdwarfow", "greydwarf oboz", "greydwarf camp"), "Greydwarf_camp");
            L(new Target(I, "gniazdo smoka", "Gniazda smoków", "gniazdo smoka", "drake gniazdo", "drake nest", "dragon gniazdo"), "DrakeNest");
            L(new Target(I, "wrak", "Wraki", "wrak", "ship wrak", "shipwreck"), "ShipWreck");
        }
    }
}
