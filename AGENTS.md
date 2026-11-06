# Project workflow

- Canonical project: /home/zlhrs/zlhrs-helper in Ubuntu WSL, user zlhrs.
- On a Windows execution host, use `wsl -d Ubuntu -u zlhrs --cd /home/zlhrs/zlhrs-helper` or the equivalent UNC path explicitly; do not use the deleted AnswerLens workspace.
- Keep one working source tree and one replaceable build/ directory.
- Build with ./build.sh; verify with ./build.sh --test. The application is a Windows Forms application; WSL invokes the Windows compiler and Windows self-tests.
- Do not create release-* directories, numbered copies, ZIP archives, backup project folders, or automatic version bumps unless the user explicitly requests them.
- Keep assembly metadata version stable for routine edits. Update build/NVIDIAHelper.exe in place.
- Preserve the existing features, theme choices, character backgrounds and cursors. Settings remain in the current Windows user's LocalAppData/ZlhrsHelper/settings.json; do not print keys or copy user settings into source control.
- For Word/DOCX work, follow the user's word-lab-style skill by default; preserve existing formatting on targeted text edits.
- Keep build/NVIDIAHelper.exe and build/NVIDIAHelper.exe.config committed and synchronized with source so a clone can run immediately. Ignore other build outputs. After code changes, rebuild the same EXE in place before pushing. Windows launcher: Запустить.cmd; WSL launcher: ./run.sh.
