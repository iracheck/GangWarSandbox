# Gang War Sandbox
A mod for Grand Theft Auto V that allows players to immerse themselves into a battlefield.

# Installation
[A copy of these dependencies are included with the mod file. Only use them if you don't already have those installed.]
### Dependencies:
- LemonUI 2.2+
- ScriptHookVDotNet-Nightly 3.7+
### Recommended Mods:
- Menyoo (to create battlefields with Object Spooner)

### Instructions:
1. Install all above dependencies
2. Drag GangWarSandbox.dll and GangWarSandbox.ini into your *scripts* folder.
3. Run GTAV.exe, and press F10 to open the mod menu. 

# Features
All of these features are __planned__. When or in what state they arrive in is unknown. Anything marked in bold is a priority.
- [x] Customizable Factions
- [x] Player Optional Neutrality / Participation
- [x] Player Spawns with Team
- [x] Up to ~~6~~ 4 Factions on a battlefield at once (for powerful devices only) --> will try to increase later, if performance permits
- [x] Squad System
- [ ] Ped Target Caching
- [x] Peds use line of sight targetting
- [x] Waypoint Movement System (Allow peds to move >150m distances)
- [x] Custom AI-- Squad system where one "squad leader" represents multiple AIs --> larger battlefield
- [ ] Advanced Custom AI Behaviors
- [ ] Strategy AI-- Order squads to go to places around the map to achieve
- [ ] Radio System (call for reinforcements, air strikes, etc)
- [ ] __Battle Type: Conquest / KOTH__
- [ ] Battle Type: Defend the Point
- [ ] Battle Type: Skirmish
- [ ] Battle Type: Gun Game
- [x] Capture Points
- [ ] Vehicle Support
- [ ] Air Vehicle Support
- [x] Decrease performance overhead of large squad/ped counts
- [ ] Rework file structure, with two INIs-- one for faction setup, one for mod options

### "Beyond 1.0" Features
Features that may not fit into Version 1, but I want to include.
- [ ] Load Custom Battles from XML + Menyoo XMLs
- [ ] Performance Mode (alternatively, limit # of peds in mod INI)
- [ ] Squads Retreat


## Known Bugs / Issues to Squash
- [x] NPCs don't spawn in spawnpoints near the player --> two options: simulate battle, or find a workaround
- [x] NPCs take cover when engaging the enemy, but aren't capable of shooting them
- [x] Peds are far apart when fighting
- [x] Ped limits per faction is ~25 default, but only 13-16 peds spawn (why?)
- [x] NPCs far from the player don't have their AI update






#### Regarding the Codebase
Parts of this mod are below my standard of code. In the beginning, it was just something quick to mess around with, a fun thing to learn. So in the time from then to making this a real thing, the code wasn't written very well. As I update the mod, I will rewrite parts of it. Anybody who would make a submod of this (I'm not sure if anyone would be interested in that) should expect some changes, but everything will remain mostly the same.
