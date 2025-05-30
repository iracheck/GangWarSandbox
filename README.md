# Gang War Sandbox
A mod for Grand Theft Auto V that allows players to immerse themselves into a battlefield.

### Regarding the Codebase
Parts of this mod are below my standard of code. In the beginning, it was just something quick to mess around with, a fun thing to learn. So in the time from then to making this a real thing, the code wasn't written very well. As I update the mod, I will rewrite parts of it. Anybody who would make a submod of this (I'm not sure if anyone would be interested in that) should expect some changes, but everything will remain mostly the same.



# Features
- [x] Customizable Factions
- [x] Player Optional Neutrality / Participation
- [x] Player Spawns with Team
- [x] Up to ~~6~~ 4 Factions on a battlefield at once (for powerful devices only)
- [ ] Light Faction Diplomacy (A and B are weak, C is strong --> focus on C)
- [x] Custom AI-- Squad system where one "squad leader" represents multiple AIs --> larger battlefield
- [ ] Strategy AI-- Order squads to go to places around the map to achieve
- [ ] Battle Type: Conquest / KOTH
- [ ] Battle Type: Defend the Point
- [ ] Battle Type: Skirmish
- [ ] Capture Points
- [ ] Vehicle Support
- [ ] Air Vehicle Support
- [ ] Decrease performance overhead of large squad/ped counts
- [ ] Stealth mechanics (peds can only see in a 270* angle around them)

## Current Focus
Currently, I am focusing on the Squad AI, as well as advanced AI states such as throwing grenades. This is almost complete (in its barebones state until Capture Points and Strategy AI).
Following that, I'm going to fully implement player neutrality, greater numbers of factions

## Known Bugs / Issues to Squash
- [ ] NPCs don't spawn in spawnpoints near the player --> two options: simulate battle, or find a workaround
- [x] NPCs take cover when engaging the enemy, but aren't capable of shooting them
- [x] Peds are far apart when fighting
- [ ] Ped limits per faction is ~25 default, but only 13-16 peds spawn (why?)
