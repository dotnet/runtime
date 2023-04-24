// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public partial class PhysicalFileProviderTests : FileCleanupTestBase
    {
        private const int WaitTimeForTokenToFire = 500;
        private const int WaitTimeForTokenCallback = 10000;

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForNullPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(null);
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForEmptyPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(string.Empty);
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes_Windows(string path)
        {
            GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes_Unix(string path)
        {
            GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(path);
        }

        private void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType<PhysicalFileInfo>(info);
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Windows(string path)
        {
            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/\0/")]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Unix(string path)
        {
            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public void PollingFileProviderShouldntConsumeINotifyInstances()
        {
            List<IDisposable> disposables = new List<IDisposable>();
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                string maxInstancesFile = "/proc/sys/fs/inotify/max_user_instances";
                Assert.True(File.Exists(maxInstancesFile));
                int maxInstances = int.Parse(File.ReadAllText(maxInstancesFile));

                // choose an arbitrary number that exceeds max
                int instances = maxInstances + 16;

                AutoResetEvent are = new AutoResetEvent(false);

                var oldPollingInterval = PhysicalFilesWatcher.DefaultPollingInterval;
                try
                {
                    PhysicalFilesWatcher.DefaultPollingInterval = TimeSpan.FromMilliseconds(WaitTimeForTokenToFire);
                    for (int i = 0; i < instances; i++)
                    {
                        PhysicalFileProvider pfp = new PhysicalFileProvider(root.Path)
                        {
                            UsePollingFileWatcher = true,
                            UseActivePolling = true
                        };
                        disposables.Add(pfp);
                        disposables.Add(pfp.Watch("*").RegisterChangeCallback(_ => are.Set(), null));
                    }

                    // trigger an event
                    root.CreateFile("test.txt");

                    // wait for at least one event.
                    Assert.True(are.WaitOne(WaitTimeForTokenCallback));
                }
                finally
                {
                    PhysicalFilesWatcher.DefaultPollingInterval = oldPollingInterval;
                    foreach (var disposable in disposables)
                    {
                        disposable.Dispose();
                    }
                }
            }
        }

        private void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        public static TheoryData<string> InvalidPaths
        {
            get
            {
                return new TheoryData<string>
                {
                    Path.Combine(". .", "file"),
                    Path.Combine(" ..", "file"),
                    Path.Combine(".. ", "file"),
                    Path.Combine(" .", "file"),
                    Path.Combine(". ", "file"),
                };
            }
        }

        [Theory]
        [MemberData(nameof(InvalidPaths))]
        public void GetFileInfoReturnsNonExistentFileInfoForIllegalPath(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(path);
                Assert.False(info.Exists);
            }
        }

        [Fact]
        // Paths starting with / are considered relative.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileInfoReturnsNotFoundFileInfoForAbsolutePath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathAboveRootPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var info = provider.GetFileInfo(Path.Combine("..", Guid.NewGuid().ToString()));
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathThatNavigatesAboveRoot()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                File.Create(Path.Combine(root.Path, "b"));

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var info = provider.GetFileInfo(Path.Combine("a", "..", "..", root.GetName(), "b"));
                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathWithEmptySegmentsThatNavigates()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                File.Create(Path.Combine(root.Path, "b"));

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var info = provider.GetFileInfo("a///../../" + root.GetName() + "/b");
                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void CreateReadStreamSucceedsOnEmptyFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.Path, fileName);
                    File.WriteAllBytes(filePath, new byte[0]);
                    var info = provider.GetFileInfo(fileName);
                    using (var stream = info.CreateReadStream())
                    {
                        Assert.NotNull(stream);
                    }
                }
            }
        }

        [Fact]
        // Hidden and system files only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileInfoReturnsNotFoundFileInfoForHiddenFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.Path, fileName);
                    File.Create(filePath);
                    var fileInfo = new FileInfo(filePath);
                    File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        // Hidden and system files only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetFileInfoReturnsNotFoundFileInfoForSystemFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.Path, fileName);
                    File.Create(filePath);
                    var fileInfo = new FileInfo(filePath);
                    File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.System);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForFileNameStartingWithPeriod()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileName = "." + Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.Path, fileName);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsFileInfoWhenExclusionDisabled()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path, ExclusionFilters.None))
                {
                    var fileName = "." + Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.Path, fileName);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<PhysicalFileInfo>(info);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Browser/iOS/tvOS always uses Active Polling which doesn't return the same instance between multiple calls to Watch(string)")]
        public void TokenIsSameForSamePath()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.Path, fileName);

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileInfo = provider.GetFileInfo(fileName);

                    var token1 = provider.Watch(fileName);
                    var token2 = provider.Watch(fileName);

                    Assert.NotNull(token1);
                    Assert.NotNull(token2);
                    Assert.Equal(token2, token1);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokensFiredOnFileChange()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.Path, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        [SkipOnCoreClr("JitStress slows this down too much", RuntimeTestModes.JitStress | RuntimeTestModes.JitStressRegs)]
        public async Task TokenCallbackInvokedOnFileChange()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.Path, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged, "Token should not have changed yet");
                            Assert.True(token.ActiveChangeCallbacks, "Token should have active callbacks");

                            var callbackInvoked = false;
                            token.RegisterChangeCallback(state =>
                            {
                                callbackInvoked = true;
                            }, state: null);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenCallback);

                            Assert.True(callbackInvoked, "Callback should have been invoked");
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task WatcherWithPolling_ReturnsTrueForFileChangedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.Path, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // emptyRoot is not used for creating and modifying files,
                // but is passed into the MockFileSystemWatcher so FileSystemWatcher events aren't triggered
                // during file changes in the test
                using (var emptyRoot = new TempDirectory(GetTestFilePath()))
                using (var fileSystemWatcher = new MockFileSystemWatcher(emptyRoot.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            File.WriteAllText(fileLocation, "some-content");
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task WatcherWithPolling_ReturnsTrueForFileRemovedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.Path, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // emptyRoot is not used for creating and modifying files,
                // but is passed into the MockFileSystemWatcher so FileSystemWatcher events aren't triggered
                // during file changes in the test
                using (var emptyRoot = new TempDirectory(GetTestFilePath()))
                using (var fileSystemWatcher = new MockFileSystemWatcher(emptyRoot.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            root.CreateFile(fileName);
                            var token = provider.Watch(fileName);
                            File.Delete(fileLocation);

                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokensFiredOnFileDeleted()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.Path, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        // On Unix the minimum invalid file path characters are / and \0
        [Theory]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void InvalidPath_DoesNotThrowWindows_GetFileInfo(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetFileInfo(path);
        }

        [Theory]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void InvalidPath_DoesNotThrowUnix_GetFileInfo(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetFileInfo(path);
        }

        private void InvalidPath_DoesNotThrowGeneric_GetFileInfo(string path)
        {
            using (var provider = new PhysicalFileProvider(Directory.GetCurrentDirectory()))
            {
                var info = provider.GetFileInfo(path);
                Assert.NotNull(info);
                Assert.IsType<NotFoundFileInfo>(info);
            }
        }

        [Theory]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void InvalidPath_DoesNotThrowWindows_GetDirectoryContents(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(path);
        }

        [Theory]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void InvalidPath_DoesNotThrowUnix_GetDirectoryContents(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(path);
        }

        private void InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(string path)
        {
            using (var provider = new PhysicalFileProvider(Directory.GetCurrentDirectory()))
            {
                var info = provider.GetDirectoryContents(path);
                Assert.NotNull(info);
                Assert.IsType<NotFoundDirectoryContents>(info);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForNullPath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(null);
                Assert.IsType<NotFoundDirectoryContents>(contents);
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes_Windows(string path)
        {
            GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(path);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes_Unix(string path)
        {
            GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(path);
        }

        private void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(path);
                Assert.IsType<PhysicalDirectoryContents>(contents);
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        [MemberData(nameof(InvalidPaths))]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath_Windows(string path)
        {
            GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(path);
        }

        [Theory]
        [InlineData("/\0/")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        [MemberData(nameof(InvalidPaths))]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath_Unix(string path)
        {
            GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(path);
        }

        private void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(string path)
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(path);
                Assert.IsType<NotFoundDirectoryContents>(contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForAbsolutePath()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
                Assert.IsType<NotFoundDirectoryContents>(contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForNonExistingDirectory()
        {
            using (var provider = new PhysicalFileProvider(Path.GetTempPath()))
            {
                var contents = provider.GetDirectoryContents(Guid.NewGuid().ToString());
                Assert.IsType<NotFoundDirectoryContents>(contents);
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsRootDirectoryContentsForEmptyPath()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                File.Create(Path.Combine(root.Path, "File" + Guid.NewGuid().ToString()));
                Directory.CreateDirectory(Path.Combine(root.Path, "Dir" + Guid.NewGuid().ToString()));

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var contents = provider.GetDirectoryContents(string.Empty);
                    Assert.Collection(contents.OrderBy(c => c.Name),
                        item => Assert.IsType<PhysicalDirectoryInfo>(item),
                        item => Assert.IsType<PhysicalFileInfo>(item));
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForPathThatNavigatesAboveRoot()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                Directory.CreateDirectory(Path.Combine(root.Path, "b"));

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var contents = provider.GetDirectoryContents(Path.Combine("a", "..", "..", root.GetName(), "b"));
                    Assert.IsType<NotFoundDirectoryContents>(contents);
                }
            }
        }

        [Fact]
        // Hidden and system files only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetDirectoryContentsDoesNotReturnFileInfoForHiddenFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.Path, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        // Hidden and system files only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void GetDirectoryContentsDoesNotReturnFileInfoForSystemFile()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.Path, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.System);

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsDoesNotReturnFileInfoForFileNameStartingWithPeriodByDefault()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.Path, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = "." + Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);

                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);

                    Assert.IsType<NotFoundFileInfo>(provider.GetFileInfo(fileName));
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsReturnsFilesWhenExclusionDisabled()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.Path, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = "." + Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);

                using (var provider = new PhysicalFileProvider(root.Path, ExclusionFilters.None))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.NotEmpty(contents);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task FileChangeTokenNotNotifiedAfterExpiry()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var changeToken = provider.Watch(fileName);
                            var invocationCount = 0;
                            changeToken.RegisterChangeCallback(_ => { invocationCount++; }, null);

                            // Callback expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenCallback);

                            // Callback not expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.Equal(1, invocationCount);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Browser/iOS/tvOS always uses Active Polling which doesn't return the same instance between multiple calls to Watch(string)")]
        public void TokenIsSameForSamePathCaseInsensitive()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var token = provider.Watch(fileName);
                    var lowerCaseToken = provider.Watch(fileName.ToLowerInvariant());
                    Assert.Equal(token, lowerCaseToken);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task CorrectTokensFiredForMultipleFiles()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName1 = Guid.NewGuid().ToString();
                            var token1 = provider.Watch(fileName1);
                            var fileName2 = Guid.NewGuid().ToString();
                            var token2 = provider.Watch(fileName2);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName1));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token1.HasChanged);
                            Assert.False(token2.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName2));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token2.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenNotAffectedByExceptions()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(fileName);

                            token.RegisterChangeCallback(_ =>
                            {
                                throw new Exception();
                            }, null);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenCallback);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForNullFilter()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var token = provider.Watch(null);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForFilterThatNavigatesAboveRoot()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var token = provider.Watch(Path.Combine("a", "..", "..", root.GetName(), "b"));

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void TokenForEmptyFilter()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var token = provider.Watch(string.Empty);

                    Assert.False(token.HasChanged);
                    Assert.True(token.ActiveChangeCallbacks);
                }
            }
        }

        [Fact]
        public void TokenForWhitespaceFilters()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var token = provider.Watch("  ");

                    Assert.False(token.HasChanged);
                    Assert.True(token.ActiveChangeCallbacks);
                }
            }
        }

        [Fact]
        // We treat forward slash differently so rooted path can happen only on windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public void NoopChangeTokenForAbsolutePathFilters()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var path = Path.Combine(root.Path, Guid.NewGuid().ToString());
                    var token = provider.Watch(path);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenFiredOnCreation()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Created, root.Path, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenFiredOnDeletion()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.Path, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenFiredForFilesUnderPathEndingWithSlash()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var directoryName = Guid.NewGuid().ToString();
                            root.CreateFolder(directoryName);
                            root.CreateFile(Path.Combine(directoryName, "some-file"));
                            var newDirectory = GetTestFileName();

                            var token = provider.Watch(directoryName + Path.DirectorySeparatorChar);

                            Directory.Move(
                                Path.Combine(root.Path, directoryName),
                                Path.Combine(root.Path, newDirectory));

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(
                                WatcherChangeTypes.Renamed,
                                root.Path,
                                newDirectory,
                                directoryName));

                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task TokenFiredForRelativePathStartingWithSlash_Windows(string slashes)
        {
            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        [Theory]
        [InlineData("/")]
        [InlineData("///")]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenFiredForRelativePathStartingWithSlash_Unix(string slashes)
        {
            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        private async Task TokenFiredForRelativePathStartingWithSlash(string slashes)
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        // Testing Windows specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Windows(string slashes)
        {
            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData("/\0/")]
        // Testing Unix specific behaviour on leading slashes.
        [PlatformSpecific(TestPlatforms.AnyUnix)]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Unix(string slashes)
        {
            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        private async Task TokenNotFiredForInvalidPathStartingWithSlash(string slashes)
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.IsType<NullChangeToken>(token);
                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenFiredForGlobbingPatternsPointingToSubDirectory()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var subDirectoryName = Guid.NewGuid().ToString();
                            var subSubDirectoryName = Guid.NewGuid().ToString();
                            var fileName = Guid.NewGuid().ToString() + ".cshtml";

                            root.CreateFolder(subDirectoryName);
                            root.CreateFolder(Path.Combine(subDirectoryName, subSubDirectoryName));
                            root.CreateFile(Path.Combine(subDirectoryName, subSubDirectoryName, fileName));

                            var pattern = string.Format(Path.Combine(subDirectoryName, "**", "*.cshtml"));
                            var token = provider.Watch(pattern);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.Combine(root.Path, subDirectoryName, subSubDirectoryName), fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "Browser/iOS/tvOS always uses Active Polling which doesn't return the same instance between multiple calls to Watch(string)")]
        public void TokensWithForwardAndBackwardSlashesAreSame()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    var token1 = provider.Watch(@"a/b\c");
                    var token2 = provider.Watch(@"a\b/c");

                    Assert.Equal(token1, token2);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokensFiredForOldAndNewNamesOnRename()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var oldFileName = Guid.NewGuid().ToString();
                            var oldToken = provider.Watch(oldFileName);

                            var newFileName = Guid.NewGuid().ToString();
                            var newToken = provider.Watch(newFileName);

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.Path, newFileName, oldFileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(oldToken.HasChanged);
                            Assert.True(newToken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokensFiredForNewDirectoryContentsOnRename()
        {
            var tcsShouldNotFire = new TaskCompletionSource<object>();
            void Fail(object state)
            {
                tcsShouldNotFire.TrySetException(new InvalidOperationException("This token should not have fired"));
            }

            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
            {
                var oldDirectoryName = Guid.NewGuid().ToString();
                var oldSubDirectoryName = Guid.NewGuid().ToString();
                var oldSubDirectoryPath = Path.Combine(oldDirectoryName, oldSubDirectoryName);
                var oldFileName = Guid.NewGuid().ToString();
                var oldFilePath = Path.Combine(oldDirectoryName, oldSubDirectoryName, oldFileName);

                var newDirectoryName = Guid.NewGuid().ToString();
                var newSubDirectoryName = Guid.NewGuid().ToString();
                var newSubDirectoryPath = Path.Combine(newDirectoryName, newSubDirectoryName);
                var newFileName = Guid.NewGuid().ToString();
                var newFilePath = Path.Combine(newDirectoryName, newSubDirectoryName, newFileName);

                Directory.CreateDirectory(Path.Combine(root.Path, newDirectoryName));
                Directory.CreateDirectory(Path.Combine(root.Path, newDirectoryName, newSubDirectoryName));
                File.Create(Path.Combine(root.Path, newDirectoryName, newSubDirectoryName, newFileName));

                var oldDirectoryToken = provider.Watch(oldDirectoryName);
                var oldDirectoryTcs = new TaskCompletionSource<object>();
                oldDirectoryToken.RegisterChangeCallback(_ => oldDirectoryTcs.TrySetResult(true), null);
                var oldSubDirectoryToken = provider.Watch(oldSubDirectoryPath);
                oldSubDirectoryToken.RegisterChangeCallback(Fail, null);
                var oldFileToken = provider.Watch(oldFilePath);
                oldFileToken.RegisterChangeCallback(Fail, null);

                var newDirectoryToken = provider.Watch(newDirectoryName);
                var newDirectoryTcs = new TaskCompletionSource<object>();
                newDirectoryToken.RegisterChangeCallback(_ => newDirectoryTcs.TrySetResult(true), null);
                var newSubDirectoryToken = provider.Watch(newSubDirectoryPath);
                var newSubDirectoryTcs = new TaskCompletionSource<object>();
                newSubDirectoryToken.RegisterChangeCallback(_ => newSubDirectoryTcs.TrySetResult(true), null);
                var newFileToken = provider.Watch(newFilePath);
                var newFileTcs = new TaskCompletionSource<object>();
                newFileToken.RegisterChangeCallback(_ => newFileTcs.TrySetResult(true), null);

                Assert.False(oldDirectoryToken.HasChanged, "Old directory token should not have changed");
                Assert.False(oldSubDirectoryToken.HasChanged, "Old subdirectory token should not have changed");
                Assert.False(oldFileToken.HasChanged, "Old file token should not have changed");
                Assert.False(newDirectoryToken.HasChanged, "New directory token should not have changed");
                Assert.False(newSubDirectoryToken.HasChanged, "New subdirectory token should not have changed");
                Assert.False(newFileToken.HasChanged, "New file token should not have changed");

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.Path, newDirectoryName, oldDirectoryName));

                await Task.WhenAll(oldDirectoryTcs.Task, newDirectoryTcs.Task, newSubDirectoryTcs.Task, newFileTcs.Task).WaitAsync(TimeSpan.FromSeconds(30));

                Assert.False(oldSubDirectoryToken.HasChanged, "Old subdirectory token should not have changed");
                Assert.False(oldFileToken.HasChanged, "Old file token should not have changed");
                Assert.True(oldDirectoryToken.HasChanged, "Old directory token should have changed");
                Assert.True(newDirectoryToken.HasChanged, "New directory token should have changed");
                Assert.True(newSubDirectoryToken.HasChanged, "New sub directory token should have changed");
                Assert.True(newFileToken.HasChanged, "New file token should have changed");
            }

            // wait a little to ensure these tokens don't fire even after disposing the watcher
            var delay = Task.Delay(3000);
            Assert.Same(delay, await Task.WhenAny(tcsShouldNotFire.Task, delay));
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokenNotFiredForFileNameStartingWithPeriod()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = "." + Guid.NewGuid().ToString();
                            var token = provider.Watch(Path.GetFileName(fileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        // Hidden and system files only make sense on Windows.
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task TokensNotFiredForHiddenAndSystemFiles()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                var hiddenFileName = Guid.NewGuid().ToString();
                var hiddenFilePath = Path.Combine(root.Path, hiddenFileName);
                File.Create(hiddenFilePath);
                var fileInfo = new FileInfo(hiddenFilePath);
                File.SetAttributes(hiddenFilePath, fileInfo.Attributes | FileAttributes.Hidden);

                var systemFileName = Guid.NewGuid().ToString();
                var systemFilePath = Path.Combine(root.Path, systemFileName);
                File.Create(systemFilePath);
                fileInfo = new FileInfo(systemFilePath);
                File.SetAttributes(systemFilePath, fileInfo.Attributes | FileAttributes.System);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var hiddenFiletoken = provider.Watch(Path.GetFileName(hiddenFileName));
                            var systemFiletoken = provider.Watch(Path.GetFileName(systemFileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, hiddenFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(hiddenFiletoken.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.Path, systemFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(systemFiletoken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task TokensFiredForAllEntriesOnError()
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            var token1 = provider.Watch(Guid.NewGuid().ToString());
                            var token2 = provider.Watch(Guid.NewGuid().ToString());
                            var token3 = provider.Watch(Guid.NewGuid().ToString());

                            fileSystemWatcher.CallOnError(new ErrorEventArgs(new Exception()));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token1.HasChanged);
                            Assert.True(token2.HasChanged);
                            Assert.True(token3.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task WildCardToken_RaisesEventsForNewFilesAdded()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: false))

            using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
            {
                var token = provider.Watch("**/*.txt");
                var directory = Path.Combine(root.Path, "subdir1", "subdir2");

                // Act
                fileSystemWatcher.CallOnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, directory, "a.txt"));
                await Task.Delay(WaitTimeForTokenToFire);

                // Assert
                Assert.True(token.HasChanged);
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task WildCardToken_RaisesEventsWhenFileSystemWatcherDoesNotFire()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.Path + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: true))

            using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
            {
                var filePath = Path.Combine(root.Path, "subdir1", "subdir2", "file.txt");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, "some-content");
                var token = provider.Watch("**/*.txt");
                var compositeToken = Assert.IsType<CompositeChangeToken>(token);
                Assert.Equal(2, compositeToken.ChangeTokens.Count);
                var pollingChangeToken = Assert.IsType<PollingWildCardChangeToken>(compositeToken.ChangeTokens[1]);
                pollingChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // Act
                fileSystemWatcher.EnableRaisingEvents = false;
                File.Delete(filePath);
                await Task.Delay(WaitTimeForTokenToFire);

                // Assert
                Assert.True(token.HasChanged);
            }
        }

        [Fact]
        public void UsePollingFileWatcher_FileWatcherNull_SetsSuccessfully()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var provider = new PhysicalFileProvider(root.Path))
                {
                    Assert.False(provider.UsePollingFileWatcher);

                    // Act / Assert
                    provider.UsePollingFileWatcher = true;
                    Assert.True(provider.UsePollingFileWatcher);
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void UsePollingFileWatcher_FileWatcherNotNull_SetterThrows()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            // Act / Assert
                            Assert.Throws<InvalidOperationException>(() => { provider.UsePollingFileWatcher = true; });
                        }
                    }
                }
            }
        }

        [Fact]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public void UsePollingFileWatcher_FileWatcherNotNull_ReturnsFalse()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.Path))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.Path + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.Path) { FileWatcher = physicalFilesWatcher })
                        {
                            // Act / Assert
                            Assert.False(provider.UsePollingFileWatcher);
                        }
                    }
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged(bool useWildcard)
        {
            // Arrange
            using var root = new TempDirectory(GetTestFilePath());
            string fileName = GetTestFileName();
            string filePath = Path.Combine(root.Path, fileName);
            File.WriteAllText(filePath, "v1.1");

            using var provider = new PhysicalFileProvider(root.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken changeToken = provider.Watch(useWildcard ? "*" : fileName);

            var tcs = new TaskCompletionSource<bool>();
            changeToken.RegisterChangeCallback(_ => { tcs.TrySetResult(true); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            // Act
            await Task.Delay(1000); // Wait a second before writing again, see https://github.com/dotnet/runtime/issues/55951.
            File.WriteAllText(filePath, "v1.2");

            // Assert
            Assert.True(await tcs.Task,
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file LastWriteTimeUtc: {File.GetLastWriteTimeUtc(filePath):O}");
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UsePollingFileWatcher_UseActivePolling_HasChanged_FileDeleted(bool useWildcard)
        {
            // Arrange
            using var root = new TempDirectory(GetTestFilePath());
            string fileName = GetTestFileName();
            string filePath = Path.Combine(root.Path, fileName);
            File.WriteAllText(filePath, "v1.1");

            string filter = useWildcard ? "*" : fileName;
            using var provider = new PhysicalFileProvider(root.Path) { UsePollingFileWatcher = true, UseActivePolling = true };
            IChangeToken changeToken = provider.Watch(filter);

            var tcs = new TaskCompletionSource<bool>();
            changeToken.RegisterChangeCallback(_ => { tcs.TrySetResult(true); }, null);

            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            cts.Token.Register(() => tcs.TrySetCanceled());

            // Act
            File.Delete(filePath);

            // Assert
            Assert.True(await tcs.Task,
                $"Change event was not raised - current time: {DateTime.UtcNow:O}, file Exists: {File.Exists(filePath)}.");
        }

        [Fact]
        public void CreateFileWatcher_CreatesWatcherWithPollingAndActiveFlags()
        {
            // Arrange
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var provider = new PhysicalFileProvider(root.Path))
            {
                provider.UsePollingFileWatcher = true;
                provider.UseActivePolling = true;

                // Act
                var fileWatcher = provider.CreateFileWatcher();

                // Assert
                Assert.True(fileWatcher.PollForChanges);
                Assert.True(fileWatcher.UseActivePolling);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [SkipOnPlatform(TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS, "System.IO.FileSystem.Watcher is not supported on Browser/iOS/tvOS")]
        public async Task CanDeleteWatchedDirectory(bool useActivePolling)
        {
            using (var root = new TempDirectory(GetTestFilePath()))
            using (var provider = new PhysicalFileProvider(root.Path))
            {
                var fileName = GetTestFileName();
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                provider.UsePollingFileWatcher = true;  // We must use polling due to https://github.com/dotnet/runtime/issues/44484
                provider.UseActivePolling = useActivePolling;

                root.CreateFile(fileName);
                var token = provider.Watch(fileName);
                Directory.Delete(root.Path, true);

                await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);

                Assert.True(token.HasChanged);
            }
        }
    }

    internal static class TempDirectoryExtensions
    {
        internal static void CreateFolder(this TempDirectory root, string path)
        {
            Directory.CreateDirectory(Path.Combine(root.Path, path));
        }

        internal static void CreateFile(this TempDirectory root, string path)
        {
            File.WriteAllText(Path.Combine(root.Path, path), "temp");
        }

        internal static string GetName(this TempDirectory root)
            => Path.GetFileName(root.Path);
    }
}
