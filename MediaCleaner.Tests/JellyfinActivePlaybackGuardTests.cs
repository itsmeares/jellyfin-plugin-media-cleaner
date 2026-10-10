using System;
using System.Linq;
using FluentAssertions;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;
using MediaCleaner.Adapters;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace MediaCleaner.Tests;

public sealed class JellyfinActivePlaybackGuardTests
{
    [Fact]
    public void LiveSessionProtectsItemUntilPlaybackStops()
    {
        var userId = Guid.NewGuid();
        var item = new Audio { Id = Guid.NewGuid() };
        var sessions = new Mock<ISessionManager>();
        var nowPlaying = new SessionInfo(sessions.Object, NullLogger.Instance)
        {
            UserId = userId,
            NowPlayingItem = new BaseItemDto { Id = item.Id },
            FullNowPlayingItem = item,
        };
        var active = true;
        sessions.SetupGet(manager => manager.Sessions)
            .Returns(() => active ? [nowPlaying] : []);

        var library = Mock.Of<ILibraryManager>();
        var snapshot = JellyfinActivePlaybackGuard.Capture(sessions.Object, library);
        JellyfinActivePlaybackGuard.IsWatching(snapshot, userId, item.Id).Should().BeTrue();
        JellyfinActivePlaybackGuard.BlocksDeletion(item, sessions.Object, library).Should().BeTrue();

        active = false;
        JellyfinActivePlaybackGuard.Capture(sessions.Object, library).Should().BeEmpty();
        JellyfinActivePlaybackGuard.BlocksDeletion(item, sessions.Object, library).Should().BeFalse();
    }

    [Fact]
    public void SavedResumeWithoutLiveSessionDoesNotProtectMedia()
    {
        var sessions = new Mock<ISessionManager>();
        sessions.SetupGet(manager => manager.Sessions).Returns([]);
        var active = JellyfinActivePlaybackGuard.Capture(sessions.Object, Mock.Of<ILibraryManager>());

        active.Should().BeEmpty();
        JellyfinActivePlaybackGuard.IsWatching(active, Guid.NewGuid(), Guid.NewGuid()).Should().BeFalse();
    }
}
