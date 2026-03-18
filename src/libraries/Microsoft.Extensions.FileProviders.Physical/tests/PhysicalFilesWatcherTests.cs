// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Microsoft.Extensions.FileProviders.Physical.Tests
{
    public class PhysicalFilesWatcherTests : FileCleanupTestBase
    {
        private const int WaitTimeForTokenToFire = 500;

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void CreateFileChangeToken_DoesNotAllowPathsAboveRoot()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.Path, "..")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.Path, "../")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken("..");
                Assert.IsType<NullChangeToken>(token);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task HandlesOnRenamedEventsThatMatchRootPath()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var token = physicalFilesWatcher.CreateFileChangeToken("**");
                var called = false;
                token.RegisterChangeCallback(o => called = true, null);

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.Path, string.Empty, string.Empty));
                await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);
                Assert.False(called, "Callback should not have been triggered");

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.Path, "old.txt", "new.txt"));
                await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);
                Assert.True(called, "Callback should have been triggered");
            }
        }

        [Fact]
        public void RaiseChangeEvents_CancelsCancellationTokenSourceForExpiredTokens()
        {
            // Arrange
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var cts3 = new CancellationTokenSource();

            var token1 = new TestPollingChangeToken { Id = 1, CancellationTokenSource = cts1 };
            var token2 = new TestPollingChangeToken { Id = 2, HasChanged = true, CancellationTokenSource = cts2 };
            var token3 = new TestPollingChangeToken { Id = 3, CancellationTokenSource = cts3 };

            var tokens = new ConcurrentDictionary<IPollingChangeToken, IPollingChangeToken>
            {
                [token1] = token1,
                [token2] = token2,
                [token3] = token3,
            };

            // Act
            PhysicalFilesWatcher.RaiseChangeEvents(tokens);

            // Assert
            Assert.False(cts1.IsCancellationRequested);
            Assert.False(cts3.IsCancellationRequested);
            Assert.True(cts2.IsCancellationRequested);

            // Ensure token2 is removed from the collection.
            Assert.Equal(new[] { token1, token3, }, tokens.Keys.OfType<TestPollingChangeToken>().OrderBy(t => t.Id));
        }

        [Fact]
        public void RaiseChangeEvents_CancelsAndRemovesMultipleChangedTokens()
        {
            // Arrange
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var cts3 = new CancellationTokenSource();
            var cts4 = new CancellationTokenSource();
            var cts5 = new CancellationTokenSource();

            var token1 = new TestPollingChangeToken { Id = 1, HasChanged = true, CancellationTokenSource = cts1 };
            var token2 = new TestPollingChangeToken { Id = 2, CancellationTokenSource = cts2 };
            var token3 = new TestPollingChangeToken { Id = 3, CancellationTokenSource = cts3 };
            var token4 = new TestPollingChangeToken { Id = 4, HasChanged = true, CancellationTokenSource = cts4 };
            var token5 = new TestPollingChangeToken { Id = 5, HasChanged = true, CancellationTokenSource = cts5 };

            var tokens = new ConcurrentDictionary<IPollingChangeToken, IPollingChangeToken>
            {
                [token1] = token1,
                [token2] = token2,
                [token3] = token3,
                [token4] = token4,
                [token5] = token5,
            };

            // Act
            PhysicalFilesWatcher.RaiseChangeEvents(tokens);

            // Assert
            Assert.False(cts2.IsCancellationRequested);
            Assert.False(cts3.IsCancellationRequested);

            Assert.True(cts1.IsCancellationRequested);
            Assert.True(cts4.IsCancellationRequested);
            Assert.True(cts5.IsCancellationRequested);

            // Ensure changed tokens are removed
            Assert.Equal(new[] { token2, token3, }, tokens.Keys.OfType<TestPollingChangeToken>().OrderBy(t => t.Id));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void GetOrAddFilePathChangeToken_AddsPollingChangeTokenWithCancellationToken_WhenActiveCallbackIsTrue()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
            {
                physicalFilesWatcher.UseActivePolling = true;

                var changeToken = physicalFilesWatcher.GetOrAddFilePathChangeToken("some-path");

                var compositeChangeToken = Assert.IsType<CompositeChangeToken>(changeToken);
                Assert.Collection(
                    compositeChangeToken.ChangeTokens,
                    token => Assert.IsType<CancellationChangeToken>(token),
                    token =>
                    {
                        var pollingChangeToken = Assert.IsType<PollingFileChangeToken>(token);
                        Assert.NotNull(pollingChangeToken.CancellationTokenSource);
                        Assert.True(pollingChangeToken.ActiveChangeCallbacks);
                    });

                Assert.NotEmpty(physicalFilesWatcher.PollingChangeTokens);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void GetOrAddFilePathChangeToken_AddsPollingChangeTokenWhenPollingIsEnabled()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
            {
                var changeToken = physicalFilesWatcher.GetOrAddFilePathChangeToken("some-path");

                var compositeChangeToken = Assert.IsType<CompositeChangeToken>(changeToken);
                Assert.Collection(
                    compositeChangeToken.ChangeTokens,
                    token => Assert.IsType<CancellationChangeToken>(token),
                    token =>
                    {
                        var pollingChangeToken = Assert.IsType<PollingFileChangeToken>(token);
                        Assert.Null(pollingChangeToken.CancellationTokenSource);
                        Assert.False(pollingChangeToken.ActiveChangeCallbacks);
                    });

                Assert.Empty(physicalFilesWatcher.PollingChangeTokens);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void GetOrAddFilePathChangeToken_DoesNotAddsPollingChangeTokenWhenCallbackIsDisabled()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var changeToken = physicalFilesWatcher.GetOrAddFilePathChangeToken("some-path");

                Assert.IsType<CancellationChangeToken>(changeToken);
                Assert.Empty(physicalFilesWatcher.PollingChangeTokens);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void GetOrAddWildcardChangeToken_AddsPollingChangeTokenWithCancellationToken_WhenActiveCallbackIsTrue()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
            {
                physicalFilesWatcher.UseActivePolling = true;

                var changeToken = physicalFilesWatcher.GetOrAddWildcardChangeToken("*.cshtml");

                var compositeChangeToken = Assert.IsType<CompositeChangeToken>(changeToken);
                Assert.Collection(
                    compositeChangeToken.ChangeTokens,
                    token => Assert.IsType<CancellationChangeToken>(token),
                    token =>
                    {
                        var pollingChangeToken = Assert.IsType<PollingWildCardChangeToken>(token);
                        Assert.NotNull(pollingChangeToken.CancellationTokenSource);
                        Assert.True(pollingChangeToken.ActiveChangeCallbacks);
                    });

                Assert.NotEmpty(physicalFilesWatcher.PollingChangeTokens);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task GetOrAddFilePathChangeToken_FiresWhenMissingDirectoryIsCreated()
        {
            using var root = new TempDirectory(GetTestFilePath());
            string missingDir = Path.Combine(root.Path, "missingdir");

            using var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path + Path.DirectorySeparatorChar,
                new FileSystemWatcher(root.Path),
                pollForChanges: false);

            // filePath's parent directory doesn't exist yet – should get a pending creation token
            IChangeToken token = physicalFilesWatcher.GetOrAddFilePathChangeToken("missingdir/file.txt");

            var tcs = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            cts.Token.Register(() => tcs.TrySetCanceled());
            token.RegisterChangeCallback(_ => tcs.TrySetResult(true), null);

            // Now create the missing directory – the token must fire
            Directory.CreateDirectory(missingDir);

            Assert.True(await tcs.Task);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task GetOrAddFilePathChangeToken_FiresWhenMultipleMissingDirectoryLevelsAreCreated()
        {
            // Verifies the cascading behavior: each time a missing directory is created the token fires,
            // the caller re-registers, and a new pending watcher is placed one level deeper.
            using var root = new TempDirectory(GetTestFilePath());
            string level1 = Path.Combine(root.Path, "level1");
            string level2 = Path.Combine(level1, "level2");

            using var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path + Path.DirectorySeparatorChar,
                new FileSystemWatcher(root.Path),
                pollForChanges: false);

            var allLevelsDone = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => allLevelsDone.TrySetCanceled());

            void Register()
            {
                IChangeToken t = physicalFilesWatcher.GetOrAddFilePathChangeToken("level1/level2/file.txt");
                t.RegisterChangeCallback(_ =>
                {
                    if (!Directory.Exists(level1) || !Directory.Exists(level2))
                    {
                        Register();
                    }
                    else
                    {
                        allLevelsDone.TrySetResult(true);
                    }
                }, null);
            }

            Register();

            // Create level1 – first token fires
            Directory.CreateDirectory(level1);
            await Task.Delay(300);

            // Create level2 – second token fires and allLevelsDone completes
            Directory.CreateDirectory(level2);

            Assert.True(await allLevelsDone.Task);
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void CreateFileChangeToken_DoesNotEnableMainFswWhenParentMissing()
        {
            // Verifies that the main FileSystemWatcher is NOT enabled (EnableRaisingEvents stays false)
            // when only pending-creation tokens are registered, preventing the recursive-watch explosion.
            using var root = new TempDirectory(GetTestFilePath());

            var fileSystemWatcher = new MockFileSystemWatcher(root.Path);
            using var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: false);

            // Register a token for a path with a missing parent directory
            _ = physicalFilesWatcher.CreateFileChangeToken("missingdir/file.txt");

            // The main FSW should NOT be enabled because only a pending-creation token exists
            Assert.False(fileSystemWatcher.EnableRaisingEvents,
                "Main FileSystemWatcher should not be enabled when parent directories are missing");
        }

        private class TestPollingChangeToken : IPollingChangeToken
        {
            public int Id { get; set; }

            public CancellationTokenSource CancellationTokenSource { get; set; }

            public bool HasChanged { get; set; }

            public bool ActiveChangeCallbacks => throw new NotImplementedException();

            public IDisposable RegisterChangeCallback(Action<object> callback, object state)
            {
                throw new NotImplementedException();
            }
        }
    }
}
