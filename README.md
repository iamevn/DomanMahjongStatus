# Doman Mahjong Status

I'd like to make a plugin that logs mahjong game info/stats.

## Commands

The following chat commands are currently implemented:

### `/mj`

Looks for active mahjong UI and tries reads gamestate out of it.

Example output:
```
 yup, looks like you're playing mahjong!
[East 1] Zawa Zawa: 25000, Spriggan: 25000, Mandragora: 25000, Moogle: 25000 (dealer)
```

### `/mjstat`

Read game stats from UIState.

Example output:
```
Reading UIState...
read [1E, 00, 96, 05, 9A, 06, 3C, 00, 09, 01] from UIState+0x14F78
Matches Played: 30
Current Rating: 1430
Highest Rating: 1690
Rank: ??? (0x09 0x01) - 60 points
```

### `/dumpUIState`

Debug command to dump the `UIState` struct to be inspected with a hex editor (see `DumpUIState.cs`).

## Resources

- [Dalamud plugin framework](https://github.com/goatcorp/Dalamud)
	- [Development FAQ](https://goatcorp.github.io/faq/development)
		- [hot reload](https://goatcorp.github.io/faq/development#q-how-do-i-hot-reload-my-plugin)
		- [how do services work](https://goatcorp.github.io/faq/development#q-how-do-the-services-in-dalamud-work)
		- [what services are available](https://goatcorp.github.io/faq/development#q-what-are-the-currently-available-dalamud-services)
	- [API reference](https://goatcorp.github.io/Dalamud/api/index.html)
	- [Sample plugin](https://github.com/goatcorp/SamplePlugin)
	- [karashiiro/DalamudPluginProjectTemplate](https://github.com/karashiiro/DalamudPluginProjectTemplate)
	- [lmcintyre/PluginTemplate](https://github.com/lmcintyre/PluginTemplate)
	- Plugin examples ([more here](https://goatcorp.github.io/DalamudPlugins/plugins)):
		- [MiniCactpotSolver](https://github.com/daemitus/MiniCactpotSolver)
		- [TriadBuddy](https://github.com/MgAl2O4/FFTriadBuddyDalamud)
- Resource files (as csv): https://github.com/xivapi/ffxiv-datamining/
- https://github.com/aers/FFXIVClientStructs/
	- [how to use with Dalamud plugin](https://goatcorp.github.io/faq/development#q-how-do-i-use-ffxivclientstructs-in-my-own-code)
