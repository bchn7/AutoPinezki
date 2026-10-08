# AutoPinezki

A Valheim mod that drops pins on your map whenever you walk past something useful:
berry bushes, ore deposits, dungeon entrances, camps, traders, boss altars.

Pin names on the map are in broken Polish/English on purpose (`miedz ruda`, `dung krypta`, `goblin camp`).
The config menu uses normal names, in English or Polish depending on your game language.

Client-side only. The server and other players don't need it.

## How it works

- Every 2 seconds the mod checks what's within 50 m of you and puts a pin on the regular game map.
- Several of the same thing close together (40 m by default) share one pin instead of five.
- The mod remembers what it already marked. If you delete a pin, it won't come back next time you pass by.
- It never removes pins on its own. That's your job (right-click on the map).
- Picked bush = crossed-out pin. Once it grows back and you're nearby, the cross goes away.
- Only plants the game actually respawns get pinned.
- Now and then a pin gets a random note, like `miedz ruda - check later`.

## Keys

| Key | What it does |
|---|---|
| `F7` | Mark whatever you're aiming at (up to 100 m), including things not on the list |
| `F8` | Turn automatic marking on/off |

In the console (`F5`), `pinezki` shows how many of each thing you've found.

## What gets marked

**Gathering:** raspberries, blueberries, cloudberries, lingonberries, thistle, wild carrot / turnip / onion / kale seeds,
wild flax and barley, Jotun Puffs, Magecap, Smoke Puff, Fiddlehead, Vineberries, volture eggs, royal jelly,
dragon eggs, wild beehives. Mushrooms (regular, yellow, blue) too, but they're off by default.

**Ores:** copper, tin, silver, iron (muddy scrap piles), obsidian, meteorites, flametal, giant remains,
gold, sulfur, tar, crystals.

**Dungeons:** Burial Chamber, Sunken Crypt, Frost Cave, Infested Mine, Troll Cave, Charred Fortress, Putrid Hole.

**Other:** boss altars, Haldor, Hildir, Bog Witch, runestones, fuling and greydwarf camps, drake nests, shipwrecks.

Every item can be turned off on its own, e.g. once you're past the copper age.

## Install

1. You need [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Grab `AutoPinezki-x.y.z.zip` from [Releases](../../releases).
3. In r2modman: **Settings → Import local mod** and pick the zip.

Without a mod manager: drop `AutoPinezki.dll` into `BepInEx/plugins/AutoPinezki/`.

## Config

`BepInEx/config/bchn.autopinezki.cfg` is created the first time you start the game.
The easiest way to change it is in-game with [ConfigurationManager](https://thunderstore.io/c/valheim/p/cjayride/ConfigurationManager/) (`F1`),
changes apply right away.

- `General`: detection range (50 m), group radius (40 m), on-screen messages, debug log of unknown locations
- `Notes`: chance of a random note on a pin, and whether `F7` on an unknown object places a note instead of its name
- `Keys`: toggle and mark keys
- `Categories` and `Items: …`: on/off for whole categories and single items
- `Icons`: icon per category (Icon0 fire, Icon1 house, Icon2 hammer, Icon3 dot, Icon4 portal)

Marked spots are stored in `BepInEx/config/AutoPinezki/<world>_<character>.txt`.
Delete the file and the mod forgets what it already marked.

## Cartography table

Pins from this mod are normal game pins, so the cartography table shares them.
Your friends will see them even without the mod.

## Building

You need .NET SDK 8+, Valheim installed and an r2modman profile with BepInEx.

```
cp src/Local.props.example src/Local.props   # fix the paths
dotnet build -c Release
```

The package ends up in `dist/`. If the mod is already imported into r2modman, the DLL is replaced right away.
Pin names and the object list live in `src/Targets.cs`.
