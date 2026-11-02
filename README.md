# Goblin Horde (portfolio)

**Survivor-style Unity game.** Move a hero through a goblin village, draft a build, and hold out until the boss at 10:00.

> This is a **portfolio showcase**, not the full game. The production Unity project stays private while I prepare a Steam release. Sample scripts below show how key systems are built. They will not compile as a standalone project.

**Status:** In development · PC (landscape) · Unity 6 · Planned Steam release  
**Designer / builder:** [Joseph Abraham](https://github.com/thejoebear) · YouTube: [@thebearjoe5304](https://www.youtube.com/@thebearjoe5304)

![Holding the village objective as a red goblin horde closes in](media/gameplay.gif)

---

## Pitch

Goblin Horde is a single-player survivor run set inside a living village, not an empty arena. You pick a hero, draft weapons and upgrades as you level, and the horde pathfinds around forests, water, and buildings instead of walking through them. Survive to 10:00, then face one of three climax bosses.

---

## What I designed and built

Solo project: systems design, content design, Unity implementation, and UI.

| Area | What shipped |
| --- | --- |
| Core loop | 10-minute timed run, auto-firing weapons, dash, XP gems, level-up draft, chest drafts, end-of-run gold |
| Heroes | 21 heroes with health/stat blocks, starting weapons, bag/stash sizes, traits, and signatures (Warden free; others unlock via feats or gold) |
| Draft / buildcraft | Card picks on level-up and chests; weapon fusion into Supers; inventory and stash pressure |
| Combat space | Off-screen spawns into the village; melee swarm, camped ranged, brutes/elites; early-run move-speed ramp so new players are not wiped in the first minute |
| Objectives | Optional map objectives that pay a boon if completed |
| Meta | Roster, meta shop, permanent upgrades that stack into the next run |
| Climax | At 10:00: Goblin Warlord, Goblin Captain, or Hex Warchief; win and leave, or keep fighting unranked |

---

## Design pillars

1. **The village is the arena.** Terrain blocks and shapes pathing, so positioning matters more than pure DPS.
2. **Draft tension, not just power.** Every level-up is a choice; chests are bigger bets; fusions reward committing to a build.
3. **Readable pressure curve.** Half-speed then 70% speed early; health scales so a finished build still has something to chew on.
4. **Hero fantasy first.** Signatures and traits make each run feel like a different character, not a reskin.

---

## Sample scripts

These are **excerpts from the private production repo**, chosen to show design systems without shipping the full game, assets, or data dumps.

| File | Why it's here |
| --- | --- |
| [`samples/DraftMath.cs`](samples/DraftMath.cs) | How draft offers are weighted and filtered |
| [`samples/XPManager.cs`](samples/XPManager.cs) | XP, leveling, and the pause-into-draft handoff |
| [`samples/WeaponBase.cs`](samples/WeaponBase.cs) | Shared weapon behaviour for the auto-fire loadout |
| [`samples/KillCounter.cs`](samples/KillCounter.cs) | Small run metric used by UI and end-of-run scoring |

Not included on purpose: full weapon/upgrade databases, scenes, prefabs, art, audio, and anything that would recreate a playable build.

---

## Role fit

Useful for recruiters looking for someone who can **own a game loop end-to-end**: draft/economy systems, enemy pacing, hero differentiation, and shipping that design in Unity C#.

---

## Links

- YouTube shorts / gameplay: [youtube.com/@thebearjoe5304](https://www.youtube.com/@thebearjoe5304)
- Full production repo: private (Steam-bound)
- Other public Unity work: [Celestial Solitaire](https://github.com/thejoebear/puzzlecardgame) (rename / README polish in progress)

---

*Questions about the design? Open an issue on this repo or reach me via LinkedIn / the contact on my applications.*
