using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;

namespace MediaCleaner.Adapters;

/// <summary>
/// PlaybackPositionTicks represents a saved resume point, not a live stream.
/// Use Jellyfin's currently playing sessions to protect active media instead.
/// Paused sessions remain protected until Jellyfin reports playback stopped.
/// </summary>
internal static class JellyfinActivePlaybackGuard
{
    public static Dictionary<Guid, HashSet<Guid>> Capture(ISessionManager sessionManager, ILibraryManager libraryManager)
    {
        var active = new Dictionary<Guid, HashSet<Guid>>();
        foreach (var session in sessionManager.Sessions)
        {
            if (session.NowPlayingItem is not { } playing)
            {
                continue;
            }

            var ids = new HashSet<Guid> { playing.Id };
            var item = session.FullNowPlayingItem ?? libraryManager.GetItemById(playing.Id);
            if (item is not null)
            {
                ids.Add(item.Id);
                if (item is Video video)
                {
                    foreach (var version in video.GetAllVersions())
                    {
                        ids.Add(version.Id);
                    }
                }

                if (item is Episode episode)
                {
                    if (episode.SeasonId != Guid.Empty) ids.Add(episode.SeasonId);
                    if (episode.SeriesId != Guid.Empty) ids.Add(episode.SeriesId);
                }
            }

            AddSessionUser(active, session.UserId, ids);
            foreach (var additionalUser in session.AdditionalUsers)
            {
                AddSessionUser(active, additionalUser.UserId, ids);
            }
        }

        return active;
    }

    public static bool IsWatching(
        IReadOnlyDictionary<Guid, HashSet<Guid>> active,
        Guid userId,
        Guid itemId) =>
        active.TryGetValue(userId, out var ids) && ids.Contains(itemId);

    public static bool BlocksDeletion(
        BaseItem deletionItem,
        ISessionManager sessionManager,
        ILibraryManager libraryManager)
    {
        // Always re-read sessions directly before every filesystem delete:
        // playback could start after the cleanup plan was built.
        var ids = Capture(sessionManager, libraryManager)
            .Values.SelectMany(x => x).ToHashSet();

        if (ids.Contains(deletionItem.Id))
        {
            return true;
        }

        return deletionItem is Video video
            && video.GetAllVersions().Any(version => ids.Contains(version.Id));
    }

    private static void AddSessionUser(
        Dictionary<Guid, HashSet<Guid>> active,
        Guid userId,
        IReadOnlyCollection<Guid> itemIds)
    {
        if (userId == Guid.Empty) return;
        if (!active.TryGetValue(userId, out var ids))
        {
            ids = [];
            active.Add(userId, ids);
        }

        ids.UnionWith(itemIds);
    }
}
