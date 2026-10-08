using System.Collections.Generic;

namespace AutoPinezki
{
    public enum Cat { Zbieractwo, Rudy, Lochy, Inne }

    public class Target
    {
        public readonly string Key;   // = Names[0]; used by memory/grouping
        public readonly Cat Cat;
        public readonly string[] Names; // funny variants, one picked at random

        public Target(Cat cat, params string[] names) { Cat = cat; Names = names; Key = names[0]; }
    }

    public static class Targets
    {
        // Exact prefab name -> target (pickables and ore deposits).
        public static readonly Dictionary<string, Target> Prefabs = new Dictionary<string, Target>();

        // Location name prefix -> target (dungeons and other locations). Turn on Debug in config to log unknown names.
        public static readonly List<KeyValuePair<string, Target>> Locations = new List<KeyValuePair<string, Target>>();

        static void P(Target t, params string[] prefabs) { foreach (var p in prefabs) Prefabs[p] = t; }
        static void L(Target t, params string[] prefixes) { foreach (var p in prefixes) Locations.Add(new KeyValuePair<string, Target>(p, t)); }

        public static Target FindLocation(string name)
        {
            foreach (var kv in Locations)
                if (name.StartsWith(kv.Key)) return kv.Value;
            return null;
        }

        static Targets()
        {
            const Cat Z = Cat.Zbieractwo, R = Cat.Rudy, D = Cat.Lochy, I = Cat.Inne;

            P(new Target(Z, "malinki", "malinka"), "RaspberryBush");
            P(new Target(Z, "boruwki", "jagudki", "borówy niebieskie"), "BlueberryBush");
            P(new Target(Z, "morozki", "żułte jagudy"), "CloudberryBush");
            P(new Target(Z, "linkgoberki", "brusznica czy cos"), "LingonberryBush");
            P(new Target(Z, "ostek", "kłujące"), "Pickable_Thistle");
            P(new Target(Z, "marhewka", "karotka"), "Pickable_SeedCarrot");
            P(new Target(Z, "rzepka", "żepka"), "Pickable_SeedTurnip");
            P(new Target(Z, "cebulka", "cebula płacz"), "Pickable_SeedOnion", "OnionSeeds");
            P(new Target(Z, "jarmusz", "kapusta fit"), "Pickable_SeedKale");
            P(new Target(Z, "len dziki", "lenik"), "Pickable_Flax_Wild");
            P(new Target(Z, "jenczmień", "na piwko"), "Pickable_Barley_Wild");
            P(new Target(Z, "jotun pafy", "pufy jotuna"), "Pickable_Mushroom_JotunPuffs");
            P(new Target(Z, "magekapelusz", "czapka maga"), "Pickable_Mushroom_Magecap");
            P(new Target(Z, "dymek", "smołk pufek"), "Pickable_SmokePuff");
            P(new Target(Z, "paprotka", "fidelhed"), "Pickable_Fiddlehead");
            P(new Target(Z, "winogronka", "wino z popiołu"), "VineAsh");
            P(new Target(Z, "jajo sempa", "jajko ptaszora"), "Pickable_VoltureEgg");
            P(new Target(Z, "galaretka krulewska", "żelka krula"), "Pickable_RoyalJelly");
            P(new Target(Z, "jajo smoka", "omlet smoczy"), "Pickable_DragonEgg");
            P(new Target(Z, "ul dziki", "bzzz", "miodek"), "Beehive");

            P(new Target(R, "copper", "miedziak", "kopper"), "rock4_copper", "MineRock_Copper");
            P(new Target(R, "tin cyna", "cyna tin"), "MineRock_Tin", "Pickable_Tin");
            P(new Target(R, "silwer", "srebrzysko"), "silvervein", "rock3_silver");
            P(new Target(R, "ajron w błocie", "żelastwo"), "mudpile", "mudpile2", "Pickable_BogIronOre", "MineRock_Iron");
            P(new Target(R, "obsydjan", "czarne szkło"), "MineRock_Obsidian", "Pickable_Obsidian");
            P(new Target(R, "meteoryt", "kamień z kosmosu"), "MineRock_Meteorite", "Pickable_Meteorite");
            P(new Target(R, "flametal", "gorący metal"), "FlametalRockstand", "LeviathanLava");
            P(new Target(R, "czacha giganta", "gigantyczne kości"), "giant_skull", "giant_ribs", "giant_brain");
            P(new Target(R, "złoto gold", "golda"), "goldvein");
            P(new Target(R, "siarka", "śmierdzi jajem"), "Pickable_SulfurRock");
            P(new Target(R, "smoła tar", "tar smoła"), "Pickable_Tar", "Pickable_TarBig");
            P(new Target(R, "kryształ", "kryształek"), "Pickable_MountainCaveCrystal");

            // Order matters: first matching prefix wins.
            L(new Target(D, "dung bagno", "krypta mokra"), "SunkenCrypt");
            L(new Target(D, "dung krypta", "dung kości"), "Crypt");
            L(new Target(D, "dung zimny", "jaskinia brr"), "MountainCave");
            L(new Target(D, "dung dwergi", "kopalnia robali"), "Mistlands_DvergrTownEntrance");
            L(new Target(D, "trol jaskinia", "chata trola"), "TrollCave");
            L(new Target(D, "spalony zamek", "zamek na grilu"), "CharredFortress");
            L(new Target(D, "śmierdząca dziura", "dziura smrodu"), "MorgenHole");

            L(new Target(I, "boss tu", "tu bije boss", "boss!!"),
              "Eikthyrnir", "GDKing", "Bonemass", "Dragonqueen", "GoblinKing", "Mistlands_DvergrBossEntrance", "FaderLocation");
            L(new Target(I, "sklepik haldora", "haldor biznes"), "Vendor_BlackForest");
            L(new Target(I, "hildirka", "sklep hildir"), "Hildir_camp");
            L(new Target(I, "wiedzma", "babcia z bagna"), "BogWitch_Camp");
            L(new Target(I, "kamień z runamy", "runy jakieś"), "Runestone_");
            L(new Target(I, "obuz goblinuw", "fulingi tu"), "GoblinCamp");
            L(new Target(I, "obuz grejdwarfuw", "szyszkowe ludki"), "Greydwarf_camp");
            L(new Target(I, "gniazdo smoka", "jajka smoka"), "DrakeNest");
            L(new Target(I, "wrak", "statek się utopił"), "ShipWreck");
        }
    }
}
