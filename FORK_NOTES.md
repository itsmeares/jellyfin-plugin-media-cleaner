# Homelab active-playback fork

Based on upstream Media Cleaner 3.7.0, targeting the Jellyfin 12 ABI.

## Behavior

- A nonzero `PlaybackPositionTicks` means the user has a saved resume position. It does **not** mean a stream is currently playing. This fork uses Jellyfin's `ISessionManager.Sessions` instead.
- The Leaving Soon collection is populated even if someone has a current playback session for a tagged item. The user's watch progress and played state are never modified to publish a notice.
- The cleanup planner protects movies and episodes with live playback sessions, including their containing season or series. Paused live sessions remain protected.
- Immediately before each file deletion, active sessions are queried again. If a matching playback session exists, deletion of that unit is skipped. The next daily cleanup can retry after playback ends.
- Protection rules and explicit Keep-until-watched user protections still apply. The global notice window and tag filtering remain unchanged.

## Build and testing

Pushes to `feature/active-playback-leaving-soon` run `.github/workflows/fork-active-playback.yml` to test the Jellyfin 12 ABI and build a package containing `MediaCleaner.dll`, `MediaCleaner.Core.dll`, and `meta.json`.

## Deployment

Back up the old plugin directory, its configuration, and the Leaving Soon state DB before replacing the installed files. Keep the original plugin GUID so the existing Media Cleaner settings are reused. Avoid running the destructive scheduled cleanup until a dry run confirms only intended `leaving-soon` candidates.

This fork is currently **not** an upstream pull request.
