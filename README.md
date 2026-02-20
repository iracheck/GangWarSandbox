# Dynamic Simulation Mod
**[Download "GangWarSandbox" on NexusMods](https://www.nexusmods.com/gta5/mods/1430)** (200+ downloads!)

Create your battlefield anywhere on the map and watch the AI fight for control of the map, then join in yourself for extra chaos.. <br>

While this started as a learning project to explore AI, state management, and modular game architectures, GTA5 was used as a foundation so I could focus on experimenting with AI systems and game logic rather than reinventing core game mechanics. It also has ended up surpassing that original goal by quite a bit, so certain elements (e.g. the 'master' class which serves as the entrypoint) are due a rewrite, especially since implementing 

### State of the Project
This project has turned into quite a monolith, and thus there are some changes I plan to make over the next few months. Mainly-- getting rid of the god class that is the project's entrypoint. I'm quite proud of the things I developed more recently, like the Gamemode API and the data parser, and those two have taught me a lot about C# and also programming in general that I want to backport onto the older elements of the project. 

### Tech Stack
**Language:** C#, .NET 4.8 <br>
**Frameworks:** ScriptHookV, ScriptHookVDotNet, Native Library, LemonUI

### Technical Features
- Modular gamemode architecture with abstract base class `Gamemode.cs`
- Customizable rulesets via overrideable hooks (e.g., `OnStart`, `OnTickGameRunning`, `OnEnd`) The "gamemode" system
- Dynamic state management (threat levels, squad spawning, scoring systems)
- Extensible squad AI
- Integration with LemonUI for dynamic in-game configuration menus
- Minimal system performance impact-- mod's update loop (30Hz) runs significantly faster than the game-- costing only ~1.5 ms at 60 frames per second

### Dependencies
- ScriptHookV
- ScriptHookVDotNet 3.7-nightly
- LemonUI-SHVDN3 2.2 (or newer) 
  
### How to Use
- Ensure all dependencies are installed
- Unpack mod file and drag everything within into your [gta5-path]/scripts folder.
- Run the game and use F10 to begin

### Future Plans
This project is due a refactor, as its grown significantly over time (well beyond what I ever expected it to, especially once I started implementing data parsing & the API)

## Gamemode API
This repo also features an extensible API in the form of the "Gamemode" system. Gamemodes can selectively control AI behavior, squad targets, spawning rules, and user-configurable settings while reusing the core simulation engine, and without overwriting any code outside of the gamemode structure. It's still a WIP, but its totally functional.

Here's an example of how to create a gamemode:
### Creating a Custom Gamemode

GangWarSandbox exposes a plugin-style `Gamemode` API that allows developers to define custom rules, spawn logic, and AI behavior without modifying core systems. This is currently an Internal API only-- in the future it will work for multiple 

### Create a Gamemode Class
The constructor for the gamemode class takes the following:
arg0 = name, what appears in the selector
arg1 = description, what appears in the menu
arg2 = max teams, this is not necessary but it allows you to force a greater number of teams than are supported. Recommended to be less than 4 but nothing can stop you :)

```csharp
public class ExampleGamemode : Gamemode
{
    public ExampleGamemode()
        : base("Example", "Endless waves of enemies.", 4)
    {
        CaptureProgressMultiplier = 0.5f;
        PedHealthMultiplier = 1.2f;
    }
}
```

### Overrides
Beyond that, you must override the existing functions within the `Gamemode` class in order to implement new functionalities. The base class already includes a few important helpers, for example setting starting relationships or helping the AI recieve targets. However, in order to create something that is actually unique, there are a couple of very important methods that you might want to touch:
- `OnTick` is called 30 times per second.
- `OnTickBattleRunning` is the same as above, but it only happens when the game is started.
- `OnPlayerDeath` occurs when the player dies. Note that this occurs the INSTANT the player dies-- not after they respawn. See "InfiniteBattle.cs" for an example of how to block this death-respawn process.
- `InitializeGamemode` is called as soon as the gamemode is first selected by the user.
- `TerminateGamemode` is called as soon as a different gamemode is selected by the user.
- `OnStart` is called as soon as the start button is pressed.
- `OnEnd` is called as soon as the end button is pressed.
There are a plenty more of these, but pending proper documentation you can discover those yourself! 

### Parameters
These are the parameters that the user may (or may not, if thats what you want) modify. By default, all parameters are avaliable to be modified, but you can disable them with the "EnableParameter_[x]" field. 
```csharp
public GamemodeSpawnMethod SpawnMethod = GamemodeSpawnMethod.Spawnpoint; // options: "Spawnpoint", "Random"

public bool SpawnVehicles { get; set; } = true;
public bool SpawnWeaponizedVehicles { get; set; } = false;
public bool SpawnHelicopters { get; set; } = false;
public bool FogOfWar { get; set; } = true;
public float UnitCountMultiplier = 1;

// code-facing only
public float CaptureProgressMultiplier { get; set; } = 1.0f;
public float PedHealthMultiplier { get; set; } = 1.0f;
public bool HasTier4Ped = true;
```

## In Game Customization
Currently, you can create your own custom Factions and Vehicle Sets for those faction. The mod tries to parse everything within a given folder (e.g. ...scripts/GangWarSandbox/Factions) as what it expects to be in that folder, so be sure you are using the correct folders. To create a new Faction or VehicleSet, create a new .ini file and follow the instructions given in the Creation Guide provided.<br>

File Paths:
> scripts/GangWarSandbox/Factions  
> scripts/GangWarSandbox/VehicleSets

You can use the following links to find ped or vehicle models: <br>
https://docs.fivem.net/docs/game-references/vehicle-references/vehicle-models/  <br>
https://wiki.rage.mp/wiki/Peds <br>
https://docs.fivem.net/docs/game-references/weapon-models/


__Note__ <br>
Use the name of the model for peds and vehicles, e.g. "hc_driver" or "issi2" <br>
Use the name of the hash for weapons: e.g. "WEAPON_PISTOL" "WEAPON_MINIGUN"
