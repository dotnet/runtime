// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.Extensions.FileProviders.Physical.Tests
{
    public class PhysicalFilesWatcherTests : FileCleanupTestBase
    {
        private const int WaitTimeForTokenToFire = 500;

        public static TheoryData<bool> WatcherModeData
        {
            get
            {
                var data = new TheoryData<bool>();

                if (!PlatformDetection.IsBrowser && !PlatformDetection.IsiOS && !PlatformDetection.IstvOS)
                {
                    data.Add(false); // useActivePolling = false: real FileSystemWatcher
                }

                data.Add(true); // useActivePolling = true: polling

                return data;
            }
        }

        private static PhysicalFilesWatcher CreateWatcher(string rootPath, bool useActivePolling)
        {
            FileSystemWatcher? fsw = useActivePolling ? null : new FileSystemWatcher();
            var watcher = new PhysicalFilesWatcher(rootPath, fsw, pollForChanges: useActivePolling);

            if (useActivePolling)
            {
                watcher.UseActivePolling = true;
            }

            return watcher;
        }

        private static async Task WhenChanged(IChangeToken token, bool withTimeout = true)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = token.RegisterChangeCallback(_ => tcs.TrySetResult(true), null);

            try
            {
                if (withTimeout)
                {
                    await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }
                else
                {
                    await tcs.Task;
                }
            }
            catch (TimeoutException ex)
            {
                throw new XunitException("Change token did not fire within 30 seconds.", ex);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void CreateFileChangeToken_DoesNotAllowPathsAboveRoot()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: false))
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
        public async Task Constructor_AcceptsFswWithPathAboveRoot()
        {
            using var root = new TempDirectory(GetTestFilePath());
            string subDir = Path.Combine(root.Path, "sub");
            Directory.CreateDirectory(subDir);

            // FSW watches root.Path which is above subDir
            using var fsw = new FileSystemWatcher(root.Path);
            using var physicalFilesWatcher = new PhysicalFilesWatcher(subDir, fsw, pollForChanges: false);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("file.txt");
            Task changed = WhenChanged(token);

            File.WriteAllText(Path.Combine(subDir, "file.txt"), string.Empty);

            await changed;
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task Constructor_AcceptsFswWithPathBelowRoot()
        {
            using var root = new TempDirectory(GetTestFilePath());
            string subDir = Path.Combine(root.Path, "sub");
            Directory.CreateDirectory(subDir);

            // FSW watches subDir which is below root.Path
            using var fsw = new FileSystemWatcher(subDir);
            using var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fsw, pollForChanges: false);

            // A file directly under root (but not under subDir) should not trigger the token,
            // because the FSW only watches subDir.
            IChangeToken rootFileToken = physicalFilesWatcher.CreateFileChangeToken("rootfile.txt");
            Task rootFileChanged = WhenChanged(rootFileToken, withTimeout: false);

            File.WriteAllText(Path.Combine(root.Path, "rootfile.txt"), string.Empty);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(rootFileChanged.IsCompleted, "Token must not fire for a file outside the FSW's watched path");

            // A file under subDir should trigger its token.
            IChangeToken subFileToken = physicalFilesWatcher.CreateFileChangeToken("sub/file.txt");
            Task subFileChanged = WhenChanged(subFileToken);

            File.WriteAllText(Path.Combine(subDir, "file.txt"), string.Empty);

            await subFileChanged;
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void Constructor_RejectsFswWithUnrelatedPath()
        {
            using var root = new TempDirectory(GetTestFilePath());

            string dir = Path.Combine(root.Path, "dir");
            string siblingDir = Path.Combine(root.Path, "dir-sibling");

            Directory.CreateDirectory(dir);
            Directory.CreateDirectory(siblingDir);

            using var fsw = new FileSystemWatcher(siblingDir);
            Assert.Throws<ArgumentException>(() =>
                new PhysicalFilesWatcher(dir, fsw, pollForChanges: false));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task HandlesOnRenamedEventsThatMatchRootPath()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: false))
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
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: true))
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
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: true))
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
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: false))
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
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path, fileSystemWatcher, pollForChanges: true))
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

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_FiresWhenFileIsCreated_WithMissingParentDirectory(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string missingDir = Path.Combine(root.Path, "missingdir");
            string targetFile = Path.Combine(missingDir, "file.txt");

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("missingdir/file.txt");

            Task changed = WhenChanged(token);

            // Create the missing directory - the token must NOT fire yet
            Directory.CreateDirectory(missingDir);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when only the parent directory is created");

            // Create the actual file - now the token must fire
            File.WriteAllText(targetFile, string.Empty);

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_IgnoresFileCreatedWithExpectedDirectoryName(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string missingDir = Path.Combine(root.Path, "missingdir");
            string targetFile = Path.Combine(missingDir, "file.txt");

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("missingdir/file.txt");
            Task changed = WhenChanged(token);

            File.WriteAllText(missingDir, string.Empty);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when a file is created with the expected directory name.");

            File.Delete(missingDir);
            Directory.CreateDirectory(missingDir);
            // On Linux, the recursive FSW needs time to add an inotify watch on
            // the new subdirectory before it can detect file changes inside it,
            // see e.g. https://github.com/dotnet/runtime/issues/116351.
            await Task.Delay(WaitTimeForTokenToFire);
            File.WriteAllText(targetFile, string.Empty);

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_FiresOnlyWhenFileIsCreated_WithMultipleMissingDirectories(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string level1 = Path.Combine(root.Path, "level1");
            string level2 = Path.Combine(level1, "level2");
            string targetFile = Path.Combine(level2, "file.txt");

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("level1/level2/file.txt");

            Task changed = WhenChanged(token);

            // Create level1 - token must NOT fire
            Directory.CreateDirectory(level1);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when level1 is created");

            // Create level2 - token must NOT fire
            Directory.CreateDirectory(level2);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when level2 is created");

            // Create the target file - now the token must fire exactly once
            File.WriteAllText(targetFile, string.Empty);

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_FiresAfterSubdirectoryDeletedAndRecreated(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string dir = Path.Combine(root.Path, "dir");

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("dir/file.txt");

            Task changed = WhenChanged(token);

            // Create the directory — token must NOT fire
            Directory.CreateDirectory(dir);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when dir is created");

            // Delete the directory
            Directory.Delete(dir);
            await Task.Delay(WaitTimeForTokenToFire);

            // Recreate the directory — token must NOT fire
            Directory.CreateDirectory(dir);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when dir is recreated without the file");

            // Create the target file — now the token must fire
            File.WriteAllText(Path.Combine(dir, "file.txt"), string.Empty);

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public void CreateFileChangeToken_DoesNotThrow_WhenRootDeletedBeforeFirstWatch(bool useActivePolling)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/107700:
            // Root exists at construction time but is deleted before the first
            // CreateFileChangeToken call. Previously this threw FileNotFoundException.
            using var root = new TempDirectory(GetTestFilePath());

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            Directory.Delete(root.Path, recursive: true);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("test.txt");
            Assert.NotNull(token);
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_RootDeletedAndRecreated_TokenFiresWhenFileCreated(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string rootPath = root.Path;

            using var physicalFilesWatcher = CreateWatcher(rootPath, useActivePolling);

            // On some platforms (e.g., Linux) the FSW does not fire OnError when the watched directory
            // is deleted (see https://github.com/dotnet/runtime/issues/126295), so we cannot wait
            // for the token to fire. Instead, wait briefly and then re-register after deleting the directory.
            physicalFilesWatcher.CreateFileChangeToken("file.txt");
            Directory.Delete(rootPath, recursive: true);
            await Task.Delay(WaitTimeForTokenToFire);

            // Re-watch the same file — root is now missing, so this goes through PendingCreationWatcher where available
            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("file.txt");

            Task changed = WhenChanged(token);

            // Recreate the root — token must not fire yet
            Directory.CreateDirectory(rootPath);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when only the root directory is recreated");

            // Create the target file — now the token must fire
            File.WriteAllText(Path.Combine(rootPath, "file.txt"), string.Empty);

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task WildcardToken_DoesNotThrow_WhenRootIsMissing(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string rootPath = root.Path;

            using var physicalFilesWatcher = CreateWatcher(rootPath, useActivePolling);

            // Delete the root so it no longer exists
            Directory.Delete(rootPath, recursive: true);

            // Watching a wildcard pattern when root is missing must not throw
            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("**/*.json");
            Assert.NotNull(token);
            Assert.False(token.HasChanged);

            Task changed = WhenChanged(token);

            // Recreate the root — token must not fire yet (no matching file)
            Directory.CreateDirectory(rootPath);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when only the root directory is recreated");

            // Create a matching file — now the token must fire
            File.WriteAllText(Path.Combine(rootPath, "config.json"), "{}");

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task WildcardToken_FiresWhenFileCreatedInMissingPrefixDirectory(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());
            string missingDir = Path.Combine(root.Path, "subdir");

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            // Watch a wildcard pattern whose non-wildcard prefix directory doesn't exist
            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("subdir/**/*.json");
            Assert.NotNull(token);
            Assert.False(token.HasChanged);

            Task changed = WhenChanged(token);

            // Create the missing directory - token must not fire yet
            Directory.CreateDirectory(missingDir);
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire when only the prefix directory is created");

            // Create a matching file — now the token must fire
            File.WriteAllText(Path.Combine(missingDir, "app.json"), "{}");

            await changed;
        }

        [Theory]
        [MemberData(nameof(WatcherModeData))]
        public async Task CreateFileChangeToken_FiresWhenDirectoryIsCreated(bool useActivePolling)
        {
            using var root = new TempDirectory(GetTestFilePath());

            using var physicalFilesWatcher = CreateWatcher(root.Path, useActivePolling);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("newdir");
            Assert.False(token.HasChanged);

            Task changed = WhenChanged(token);

            Assert.False(changed.IsCompleted, "Token must not fire when watching for a directory that doesn't exist");

            Directory.CreateDirectory(Path.Combine(root.Path, "newdir"));

            await changed;
        }

        [Fact]
        // Hidden directories only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task CreateFileChangeToken_DoesNotFireForHiddenDirectory()
        {
            using var root = new TempDirectory(GetTestFilePath());
            string dirPath = Path.Combine(root.Path, "hiddendir");
            Directory.CreateDirectory(dirPath);
            File.SetAttributes(dirPath, File.GetAttributes(dirPath) | FileAttributes.Hidden);

            using var fileSystemWatcher = new MockFileSystemWatcher(root.Path);
            using var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path, fileSystemWatcher, pollForChanges: false);

            IChangeToken token = physicalFilesWatcher.CreateFileChangeToken("hiddendir");
            Assert.False(token.HasChanged);

            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, "hiddendir"));
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(token.HasChanged, "Token must not fire for a hidden directory");
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task Watch_DoesNotFireForSiblingDirectoryWithSharedPrefix()
        {
            using var tempDir = new TempDirectory(GetTestFilePath());

            string rootDir = Path.Combine(tempDir.Path, "rootDir");
            string siblingDir = Path.Combine(tempDir.Path, "nextDir");

            Assert.Equal(rootDir.Length, siblingDir.Length);
            Assert.NotEqual(rootDir, siblingDir);

            // rootDir doesn't exist yet — the FSW watches tempDir (ancestor of rootDir).
            using var fsw = new FileSystemWatcher(tempDir.Path);
            using var physicalFilesWatcher = new PhysicalFilesWatcher(
                rootDir, fsw, pollForChanges: false);

            var token = physicalFilesWatcher.CreateFileChangeToken("appsettings.json");
            Task changed = WhenChanged(token);

            Directory.CreateDirectory(rootDir);
            Directory.CreateDirectory(siblingDir);

            // Create a file with the same name in the sibling directory — token must NOT fire
            File.WriteAllText(Path.Combine(siblingDir, "appsettings.json"), "{}");
            await Task.Delay(WaitTimeForTokenToFire);
            Assert.False(changed.IsCompleted, "Token must not fire for a file in a sibling directory with a shared prefix.");

            // Create the file in the actual root directory — token must fire
            File.WriteAllText(Path.Combine(rootDir, "appsettings.json"), "{}");

            await changed;
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
