# Gang War Sandbox

A multi-gamemode combat utility for Grand Theft Auto V.<br>
Simulate massive AI battles with 100+ peds, survive waves of enemies, or build your own custom battlefield.

# Installation

A copy of these dependencies are included with the mod file. Only use them if you don't already have those installed.

### Dependencies:
- LemonUI 2.2+
- ScriptHookVDotNet-Nightly 3.7+
- ScriptHookV

### Recommended Mods:
- Menyoo (Create battlefields)

### Instructions:
1. Install all above dependencies
2. Drag GangWarSandbox.dll and the GangWarSandbox folder into your *scripts* folder.
3. Run GTAV.exe, and press F10 (or optionally configured key) to open the mod menu.

## Customization Instructions
Currently, you can create your own custom Factions and Vehicle Sets for those faction. The mod tries to parse everything within a given folder (e.g. ...scripts/GangWarSandbox/Factions), so be sure you are using the correctly provided folders. To create a new Faction or VehicleSet, create a new .ini file and follow the instructions given in the Creation Guide given in each folder.<br>

File Paths:
> scripts/GangWarSandbox/Factions  
> scripts/GangWarSandbox/VehicleSets

You can use the following links to find ped or vehicle models: <br>
https://docs.fivem.net/docs/game-references/vehicle-references/vehicle-models/  <br>
https://wiki.rage.mp/wiki/Peds
https://docs.fivem.net/docs/game-references/weapon-models/


__Note__ <br>
Use the name of the model for peds and vehicles, e.g. "hc_driver" or "issi2"
Use the name of the hash for weapons: e.g. "WEAPON_PISTOL" "WEAPON_MINIGUN"

# Features
All of these features are __planned__. When or in what state they arrive in is unknown. Anything marked in bold is a priority.
- [x] Customizable Factions
- [x] Player Optional Neutrality / Participation
- [x] Player Spawns with Team
- [x] Up to ~~6~~ 4 Factions on a battlefield at once (for powerful devices only) --> will try to increase later, if performance permits
- [x] Squad System
- [x] Ped Target Caching
- [x] Peds use line of sight targetting
- [x] Waypoint Movement System (Allow peds to move >150m distances)
- [x] Custom AI-- Squad system where one "squad leader" represents multiple AIs --> larger battlefield
- [x] Gamemode: Survival
- [x] Gamemode: Endless
- [x] Capture Points
- [x] Vehicle Support
- [x] Helicopter Support
- [x] Decrease performance overhead of large squad/ped counts
- [x] Rework file structure, with two INIs-- one for faction setup, one for mod options


## Known Bugs / Issues to Squash
- [ ] Placing victory points/spawn points in an unloaded area causes them to be placed underneath the map
- [x] NPCs don't spawn in spawnpoints near the player --> two options: simulate battle, or find a workaround
- [x] NPCs take cover when engaging the enemy, but aren't capable of shooting them
- [x] Peds are far apart when fighting
- [x] Ped limits per faction is ~25 default, but only 13-16 peds spawn (why?)
- [x] NPCs far from the player don't have their AI update
- [x] Incomplete .ini files cause a crash
