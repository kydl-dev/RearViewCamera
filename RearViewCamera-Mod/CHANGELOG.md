# Changelog

Newest version first. The top `## x.y.z` heading = `Plugin.ModVersion` = `manifest.json` `version_number` (integration tests check it).
Write "not tested in game" for anything that was not run in game.

## 0.1.0
- First version (project created 2026-10-24).
- Small rear-view camera under the minimap: key N (Toggle by default, or Hold), 16:9, height 360-540 px (default 800x450), offsets in the config. Not tested in game.
- Full-screen rear view while J is held; the character keeps facing forward. Not tested in game.
- Keys are ignored while chat, the console, Configuration Manager or a text field is open. Not tested in game.
- Camera distance / height, field of view, update rate and mirror image in the config. Not tested in game.
- The rear camera uses the game camera's render settings (HDR, view distance, depth) and the game's colour grading. Tested in game.
- Full-screen view renders at up to 1080p and is scaled to the screen. Not tested in game.
- Hidden while the inventory, the escape menu, a loading screen or the hidden HUD (Ctrl+F3) is up; comes back afterwards. On the large map it can only be hidden. Not tested in game.
- New config StatusEffects: Under / Behind / OnTop the status-effect icons, following where they really are. Not tested in game.
- Camera rises over an animal that is right behind you. Not tested in game.
- Keeps the last picture while the game is paused; renders nothing while hidden. Not tested in game.
- Config and game graphics changes apply at once. Not tested in game.
- If a rear-view key is used by the game or another mod, Home / End are used for the session and a message says so. Not tested in game.
- After an error the rear view drops to simpler rendering instead of failing every frame. Not tested in game.
