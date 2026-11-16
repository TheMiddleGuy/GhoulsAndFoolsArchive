# Ghouls'n'Fools — project archive

A haunted hotel, an upgradeable vacuum, and four friends trying to make it out together.
**Ghouls'n'Fools** is a local co-op game for up to four players on gamepads, made in Unity by a team of six for the Group Projects course at Howest Digital Arts and Entertainment (2026).

▶ **Play it:** [jeremiahvb.itch.io/gp33ghouls-and-fools](https://jeremiahvb.itch.io/gp33ghouls-and-fools)
🌐 **Project page:** [Ghouls'n'Fools on my portfolio](https://themiddleguy.github.io/ghouls-and-fools.html)

## About this repo

During development we used Perforce. That server is gone, so this repository is a snapshot of the final version of the project, imported in a single commit. The original history isn't preserved, so the sections below list who worked on what.

## Team

| Name | Role |
|---|---|
| Dan (Me) | Main developer: game flow, lobby, UI, items, flashlights, audio |
| Larz | Game design, with work on items, doors, the vacuum and upgrade logic, and UI design |
| Lucas | Enemy AI and interactions |
| Luiz | Environment design and shaders |
| Jeremiah | Character art (players and ghosts), prop refinement and art direction |
| Tsimafei | Environment props |

## My part (Danila)

I was the main developer and also led the team. My work was mostly the code that holds the game together, plus turning our designers' standalone prototypes into one game.

**Scripts I wrote**
- [`Assets/Scripts/General/`](Assets/Scripts/General) — game flow between scenes with fades, the game session (player selections, win/loss state), scene music with crossfades, and a sound limiter so the same sound can't stack up more than a few times at once.
- [`Assets/Scripts/FlashLight/`](Assets/Scripts/FlashLight) — the flashlights: items sparkling when they're in the light and not behind a wall, plus the custom flashlight shader experiment (below).
- [`Assets/Scripts/Items/`](Assets/Scripts/Items) — items and where they spawn, including special items that must spawn inside locked rooms.
- [`Assets/Scripts/UI/`](Assets/Scripts/UI) — main menu, the player selection lobby (join, pick a colour, ready up, countdown), player icons, tutorial prompts, end screen and the vacuum upgrade overlay.
- [`Assets/Scripts/Player/`](Assets/Scripts/Player) — a joint effort with the rest of the team.

**Audio:** I found, edited and tweaked all the sounds used in the game.

**The shader that didn't ship:** Unity's spotlights didn't give us the flashlight look we wanted, so I wrote one from scratch: [`FlashlightManager.cs`](Assets/Scripts/FlashLight/FlashlightManager.cs) sends every flashlight's position, direction and beam shape to [`FlashlightURP.shader`](Assets/Shaders/FlashlightURP.shader). It worked, but it was complex, so we went with stronger spotlights instead. It's left in for reference.

## Opening the project

- Unity **6000.3.9f1** (Unity 6.3) with URP
- Open the folder in Unity Hub and start from `Assets/Scenes/Bootstrap.unity`
- Up to four gamepads; each player joins in the lobby by pressing A

## Credits

- Music by [Nikita Kondrashev](https://pixabay.com/users/leberch-42823964/) from [Pixabay](https://pixabay.com/)
- Sound effects from [Pixabay](https://pixabay.com/)
- Font: Nightcore

This archive is shared for portfolio purposes. All art, audio and code belong to their respective authors on the team.
