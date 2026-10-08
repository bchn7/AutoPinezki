# AutoPinezki

Mod do Valheim, który sam stawia pinezki na mapie, kiedy przechodzisz obok czegoś przydatnego.
Krzaki z jagodami, złoża rudy, wejścia do lochów, obozy, kupcy, ołtarze bossów.

Nazwy pinezek są celowo głupie i pisane z błędami: `boruwki`, `copper`, `dung krypta`,
`obuz goblinuw`, `śmierdząca dziura`. Każda rzecz ma kilka wersji nazwy losowanych przy stawianiu.

Mod działa tylko po stronie gracza. Serwer i inni gracze nie muszą go mieć.

## Jak to działa

- Co 2 sekundy mod sprawdza, co jest w promieniu 50 m od Ciebie, i stawia pinezkę na zwykłej mapie gry.
- Kilka takich samych rzeczy blisko siebie (domyślnie 40 m) dostaje jedną pinezkę, a nie pięć.
- Mod zapamiętuje, co już oznaczył. Jak usuniesz pinezkę, nie wróci przy następnym przejściu.
- Pinezek nigdy sam nie usuwa, to Twoja robota (PPM na mapie).
- Zebrany krzak = przekreślona pinezka. Jak odrośnie i będziesz w okolicy, przekreślenie znika.
- Z krzaków oznacza tylko to, co gra faktycznie odnawia.

## Klawisze

| Klawisz | Co robi |
|---|---|
| `F7` | Oznacza to, w co celujesz (do 100 m), także rzeczy spoza listy |
| `F8` | Włącza / wyłącza automatyczne oznaczanie |

W konsoli (`F5`) komenda `pinezki` pokazuje, czego ile znalazłeś.

## Co jest oznaczane

**Zbieractwo:** maliny, borówki, moroszki, borówki brusznice, oset, dzika marchew / rzepa / cebula / jarmuż,
dziki len i jęczmień, Jotun Puffs, Magecap, Smoke Puff, Fiddlehead, Vineberry, jaja voltur, royal jelly,
smocze jaja, dzikie ule.

**Rudy:** miedź, cyna, srebro, żelazo z błota, obsydian, meteoryty, flametal, szczątki gigantów,
złoto, siarka, smoła, kryształy.

**Lochy:** Burial Chamber, Sunken Crypt, Frost Cave, Infested Mine, Troll Cave, Charred Fortress, Putrid Hole.

**Inne:** ołtarze bossów, Haldor, Hildir, wiedźma z bagien, runestones, obozy goblinów i greydwarfów,
gniazda smoków, wraki.

Każdą rzecz można osobno wyłączyć w configu, np. jak w późniejszej fazie gry nie potrzebujesz już miedzi.

## Instalacja

1. Potrzebny jest [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/).
2. Pobierz `AutoPinezki-x.y.z.zip` z [Releases](../../releases).
3. W r2modman: **Settings → Import local mod** i wskaż zip.

Bez menedżera modów: wrzuć `AutoPinezki.dll` do `BepInEx/plugins/AutoPinezki/`.

## Config

Plik `BepInEx/config/bchn.autopinezki.cfg` tworzy się przy pierwszym uruchomieniu gry.
Najwygodniej zmieniać go w grze przez [ConfigurationManager](https://thunderstore.io/c/valheim/p/cjayride/ConfigurationManager/) (`F1`),
wtedy zmiany działają od razu.

- `Range`: zasięg wykrywania (50 m)
- `GroupRadius`: odległość, poniżej której nie stawia drugiej takiej samej pinezki (40 m)
- `Komunikaty`: napis „Znalazłeś: …!” na ekranie
- `Kategorie` i `Rzeczy: …`: włączniki całych kategorii i pojedynczych rzeczy
- `Ikony`: ikona dla każdej kategorii (Icon0 ognisko, Icon1 dom, Icon2 młotek, Icon3 kropka, Icon4 portal)
- `Debug`: zapisuje w logu nazwy lokacji, których mod nie zna

Lista oznaczonych miejsc leży w `BepInEx/config/AutoPinezki/<świat>_<postać>.txt`.
Skasowanie pliku = mod zapomina, co już oznaczył.

## Stół kartograficzny

Pinezki z moda to zwykłe pinezki gry, więc stół kartograficzny przekazuje je dalej.
Kumple zobaczą je u siebie, nawet jeśli nie mają moda.

## Budowanie

Potrzebny .NET SDK 8+, zainstalowany Valheim i profil r2modman z BepInExem.

```
cp src/Local.props.example src/Local.props   # popraw ścieżki
dotnet build -c Release
```

Gotowa paczka ląduje w `dist/`. Jeśli mod jest już zaimportowany do r2modman, DLL podmienia się od razu.
Nazwy pinezek i listę obiektów znajdziesz w `src/Targets.cs`.
