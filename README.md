Underdawn is a top-down roguelike action video game made for PC players who want fast-paced and dynamic combat styles. Unlike other roguelike games that
have rigid character builds, Underdawn challenges players to explore dungeons and steal defeated enemies' elemental abilities to alter their effectiveness against
ever-changing threats. The player will have to constantly adapt on the go, making quick decisions to strategically survive dangerous dungeons.

## Controls (can be changed in Options)
Move WASD, attack J / K (hold for a charged hit, J J K = spin), skill L, element burst I,
absorb element F, jump Space, dash Shift, pause Esc

## Tests
Automated tests for the game mechanics are in `tests/`. Run them from the project folder:

    Godot_v4.7.2-stable_mono_win64_console.exe --headless --path . res://tests/test_runner.tscn

Every check prints PASS / FAIL and the exit code is the number of failed checks.
The tests use their own save file, so they never touch your save.

## Logs
Errors and crashes are written to `user://logs/error_log.txt`
(on Windows: `%APPDATA%\Godot\app_userdata\UnderDawn\logs\error_log.txt`).
