// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
    public class PhysicalFilesWatcherTests
    {
        private const int WaitTimeForTokenToFire = 500;

        [Fact]
        public void CreateFileChangeToken_DoesNotAllowPathsAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.RootPath, "..")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken(Path.GetFullPath(Path.Combine(root.RootPath, "../")));
                Assert.IsType<NullChangeToken>(token);

                token = physicalFilesWatcher.CreateFileChangeToken("..");
                Assert.IsType<NullChangeToken>(token);
            }
        }

        [Fact]
        public async Task HandlesOnRenamedEventsThatMatchRootPath()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var token = physicalFilesWatcher.CreateFileChangeToken("**");
                var called = false;
                token.RegisterChangeCallback(o => called = true, null);

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, string.Empty, string.Empty));
                await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);
                Assert.False(called, "Callback should not have been triggered");

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, "old.txt", "new.txt"));
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
        public void GetOrAddFilePathChangeToken_AddsPollingChangeTokenWithCancellationToken_WhenActiveCallbackIsTrue()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
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
        public void GetOrAddFilePathChangeToken_AddsPollingChangeTokenWhenPollingIsEnabled()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
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
        public void GetOrAddFilePathChangeToken_DoesNotAddsPollingChangeTokenWhenCallbackIsDisabled()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            {
                var changeToken = physicalFilesWatcher.GetOrAddFilePathChangeToken("some-path");

                Assert.IsType<CancellationChangeToken>(changeToken);
                Assert.Empty(physicalFilesWatcher.PollingChangeTokens);
            }
        }

        [Fact]
        public void GetOrAddWildcardChangeToken_AddsPollingChangeTokenWithCancellationToken_WhenActiveCallbackIsTrue()
        {
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
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
