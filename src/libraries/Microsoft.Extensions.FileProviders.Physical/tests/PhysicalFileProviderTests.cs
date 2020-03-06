// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.FileProviders.Internal;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
using Xunit;

namespace Microsoft.Extensions.FileProviders
{
    public class PhysicalFileProviderTests
    {
        private const int WaitTimeForTokenToFire = 500;

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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public void GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes_Windows(string path)
        {
            GetFileInfoReturnsPhysicalFileInfoForValidPathsWithLeadingSlashes(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Windows(string path)
        {
            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/\0/")]
        public void GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes_Unix(string path)
        {
            GetFileInfoReturnsNotFoundFileInfoForIllegalPathWithLeadingSlashes(path);
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

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Paths starting with / are considered relative.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Paths starting with / are considered relative.")]
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
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var info = provider.GetFileInfo(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));
                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsNotFoundFileInfoForRelativePathWithEmptySegmentsThatNavigates()
        {
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var info = provider.GetFileInfo("a///../../" + root.DirectoryInfo.Name + "/b");
                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void CreateReadStreamSucceedsOnEmptyFile()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
                    File.WriteAllBytes(filePath, new byte[0]);
                    var info = provider.GetFileInfo(fileName);
                    using (var stream = info.CreateReadStream())
                    {
                        Assert.NotNull(stream);
                    }
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public void GetFileInfoReturnsNotFoundFileInfoForHiddenFile()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
                    File.Create(filePath);
                    var fileInfo = new FileInfo(filePath);
                    File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public void GetFileInfoReturnsNotFoundFileInfoForSystemFile()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);
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
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = "." + Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<NotFoundFileInfo>(info);
                }
            }
        }

        [Fact]
        public void GetFileInfoReturnsFileInfoWhenExclusionDisabled()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath, ExclusionFilters.None))
                {
                    var fileName = "." + Guid.NewGuid().ToString();
                    var filePath = Path.Combine(root.RootPath, fileName);

                    var info = provider.GetFileInfo(fileName);

                    Assert.IsType<PhysicalFileInfo>(info);
                }
            }
        }

        [Fact]
        public void TokenIsSameForSamePath()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var provider = new PhysicalFileProvider(root.RootPath))
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
        public async Task TokensFiredOnFileChange()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenCallbackInvokedOnFileChange()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
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

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(callbackInvoked, "Callback should have been invoked");
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task WatcherWithPolling_ReturnsTrueForFileChangedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.RootPath, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // emptyRoot is not used for creating and modifying files,
                // but is passed into the MockFileSystemWatcher so FileSystemWatcher events aren't triggered
                // during file changes in the test
                using (var emptyRoot = new DisposableFileSystem())
                using (var fileSystemWatcher = new MockFileSystemWatcher(emptyRoot.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
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
        public async Task WatcherWithPolling_ReturnsTrueForFileRemovedWhenFileSystemWatcherDoesNotRaiseEvents()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Path.GetRandomFileName();
                var fileLocation = Path.Combine(root.RootPath, fileName);
                PollingFileChangeToken.PollingInterval = TimeSpan.FromMilliseconds(10);

                // emptyRoot is not used for creating and modifying files,
                // but is passed into the MockFileSystemWatcher so FileSystemWatcher events aren't triggered
                // during file changes in the test
                using (var emptyRoot = new DisposableFileSystem())
                using (var fileSystemWatcher = new MockFileSystemWatcher(emptyRoot.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: true))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
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
        public async Task TokensFiredOnFileDeleted()
        {
            using (var root = new DisposableFileSystem())
            {
                var fileName = Guid.NewGuid().ToString();
                var fileLocation = Path.Combine(root.RootPath, fileName);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var token = provider.Watch(fileName);
                            Assert.NotNull(token);
                            Assert.False(token.HasChanged);
                            Assert.True(token.ActiveChangeCallbacks);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire).ConfigureAwait(false);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        // On Unix the minimum invalid file path characters are / and \0
        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        public void InvalidPath_DoesNotThrowWindows_GetFileInfo(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetFileInfo(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        [InlineData("/test:test")]
        [InlineData("/dir/name\"")]
        [InlineData("/dir>/name")]
        public void InvalidPath_DoesNotThrowWindows_GetDirectoryContents(string path)
        {
            InvalidPath_DoesNotThrowGeneric_GetDirectoryContents(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows)]
        [InlineData("/test:test\0")]
        [InlineData("/dir/\0name\"")]
        [InlineData("/dir>/name\0")]
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public void GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes_Windows(string path)
        {
            GetDirectoryContentsReturnsEnumerableDirectoryContentsForValidPathWithLeadingSlashes(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
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

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        [MemberData(nameof(InvalidPaths))]
        public void GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath_Windows(string path)
        {
            GetDirectoryContentsReturnsNotFoundDirectoryContentsForInvalidPath(path);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/\0/")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        [MemberData(nameof(InvalidPaths))]
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
            using (var root = new DisposableFileSystem())
            {
                File.Create(Path.Combine(root.RootPath, "File" + Guid.NewGuid().ToString()));
                Directory.CreateDirectory(Path.Combine(root.RootPath, "Dir" + Guid.NewGuid().ToString()));

                using (var provider = new PhysicalFileProvider(root.RootPath))
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
            using (var root = new DisposableFileSystem())
            {
                Directory.CreateDirectory(Path.Combine(root.RootPath, "b"));

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));
                    Assert.IsType<NotFoundDirectoryContents>(contents);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public void GetDirectoryContentsDoesNotReturnFileInfoForHiddenFile()
        {
            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.Hidden);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public void GetDirectoryContentsDoesNotReturnFileInfoForSystemFile()
        {
            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);
                var fileInfo = new FileInfo(filePath);
                File.SetAttributes(filePath, fileInfo.Attributes | FileAttributes.System);

                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.Empty(contents);
                }
            }
        }

        [Fact]
        public void GetDirectoryContentsDoesNotReturnFileInfoForFileNameStartingWithPeriodByDefault()
        {
            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = "." + Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);

                using (var provider = new PhysicalFileProvider(root.RootPath))
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
            using (var root = new DisposableFileSystem())
            {
                var directoryName = Guid.NewGuid().ToString();
                var directoryPath = Path.Combine(root.RootPath, directoryName);
                Directory.CreateDirectory(directoryPath);

                var fileName = "." + Guid.NewGuid().ToString();
                var filePath = Path.Combine(directoryPath, fileName);
                File.Create(filePath);

                using (var provider = new PhysicalFileProvider(root.RootPath, ExclusionFilters.None))
                {
                    var contents = provider.GetDirectoryContents(directoryName);
                    Assert.NotEmpty(contents);
                }
            }
        }

        [Fact]
        public async Task FileChangeTokenNotNotifiedAfterExpiry()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var changeToken = provider.Watch(fileName);
                            var invocationCount = 0;
                            changeToken.RegisterChangeCallback(_ => { invocationCount++; }, null);

                            // Callback expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            // Callback not expected.
                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.Equal(1, invocationCount);
                        }
                    }
                }
            }
        }

        [Fact]
        public void TokenIsSameForSamePathCaseInsensitive()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var fileName = Guid.NewGuid().ToString();
                    var token = provider.Watch(fileName);
                    var lowerCaseToken = provider.Watch(fileName.ToLowerInvariant());
                    Assert.Equal(token, lowerCaseToken);
                }
            }
        }

        [Fact]
        public async Task CorrectTokensFiredForMultipleFiles()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName1 = Guid.NewGuid().ToString();
                            var token1 = provider.Watch(fileName1);
                            var fileName2 = Guid.NewGuid().ToString();
                            var token2 = provider.Watch(fileName2);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName1));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token1.HasChanged);
                            Assert.False(token2.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName2));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token2.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenNotAffectedByExceptions()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(fileName);

                            token.RegisterChangeCallback(_ =>
                            {
                                throw new Exception();
                            }, null);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForNullFilter()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch(null);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void NoopChangeTokenForFilterThatNavigatesAboveRoot()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch(Path.Combine("a", "..", "..", root.DirectoryInfo.Name, "b"));

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public void TokenForEmptyFilter()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
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
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token = provider.Watch("  ");

                    Assert.False(token.HasChanged);
                    Assert.True(token.ActiveChangeCallbacks);
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux,
            SkipReason = "We treat forward slash differently so rooted path can happen only on windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX,
            SkipReason = "We treat forward slash differently so rooted path can happen only on windows.")]
        public void NoopChangeTokenForAbsolutePathFilters()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var path = Path.Combine(root.RootPath, Guid.NewGuid().ToString());
                    var token = provider.Watch(path);

                    Assert.Same(NullChangeToken.Singleton, token);
                }
            }
        }

        [Fact]
        public async Task TokenFiredOnCreation()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Created, root.RootPath, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredOnDeletion()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var name = Guid.NewGuid().ToString();
                            var token = provider.Watch(name);

                            fileSystemWatcher.CallOnDeleted(new FileSystemEventArgs(WatcherChangeTypes.Deleted, root.RootPath, name));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredForFilesUnderPathEndingWithSlash()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var directoryName = Guid.NewGuid().ToString();
                            root.CreateFolder(directoryName)
                                .CreateFile(Path.Combine(directoryName, "some-file"));
                            var newDirectory = Path.GetRandomFileName();

                            var token = provider.Watch(directoryName + Path.DirectorySeparatorChar);

                            Directory.Move(
                                Path.Combine(root.RootPath, directoryName),
                                Path.Combine(root.RootPath, newDirectory));

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(
                                WatcherChangeTypes.Renamed,
                                root.RootPath,
                                newDirectory,
                                directoryName));

                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        [InlineData("/\\/")]
        [InlineData("\\/\\/")]
        public async Task TokenFiredForRelativePathStartingWithSlash_Windows(string slashes)
        {
            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/")]
        [InlineData("///")]
        public async Task TokenFiredForRelativePathStartingWithSlash_Unix(string slashes)
        {
            await TokenFiredForRelativePathStartingWithSlash(slashes);
        }

        private async Task TokenFiredForRelativePathStartingWithSlash(string slashes)
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Testing Windows specific behaviour on leading slashes.")]
        [InlineData("/C:\\Windows\\System32")]
        [InlineData("/\0/")]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Windows(string slashes)
        {
            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        [ConditionalTheory]
        [OSSkipCondition(OperatingSystems.Windows, SkipReason = "Testing Unix specific behaviour on leading slashes.")]
        [InlineData("/\0/")]
        public async Task TokenNotFiredForInvalidPathStartingWithSlash_Unix(string slashes)
        {
            await TokenNotFiredForInvalidPathStartingWithSlash(slashes);
        }

        private async Task TokenNotFiredForInvalidPathStartingWithSlash(string slashes)
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = Guid.NewGuid().ToString();
                            var token = provider.Watch(slashes + fileName);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.IsType<NullChangeToken>(token);
                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokenFiredForGlobbingPatternsPointingToSubDirectory()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var subDirectoryName = Guid.NewGuid().ToString();
                            var subSubDirectoryName = Guid.NewGuid().ToString();
                            var fileName = Guid.NewGuid().ToString() + ".cshtml";

                            root.CreateFolder(subDirectoryName)
                                .CreateFolder(Path.Combine(subDirectoryName, subSubDirectoryName))
                                .CreateFile(Path.Combine(subDirectoryName, subSubDirectoryName, fileName));

                            var pattern = string.Format(Path.Combine(subDirectoryName, "**", "*.cshtml"));
                            var token = provider.Watch(pattern);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.Combine(root.RootPath, subDirectoryName, subSubDirectoryName), fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(token.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public void TokensWithForwardAndBackwardSlashesAreSame()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    var token1 = provider.Watch(@"a/b\c");
                    var token2 = provider.Watch(@"a\b/c");

                    Assert.Equal(token1, token2);
                }
            }
        }

        [Fact]
        public async Task TokensFiredForOldAndNewNamesOnRename()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var oldFileName = Guid.NewGuid().ToString();
                            var oldToken = provider.Watch(oldFileName);

                            var newFileName = Guid.NewGuid().ToString();
                            var newToken = provider.Watch(newFileName);

                            fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, newFileName, oldFileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.True(oldToken.HasChanged);
                            Assert.True(newToken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensFiredForNewDirectoryContentsOnRename()
        {
            var tcsShouldNotFire = new TaskCompletionSource<object>();
            void Fail(object state)
            {
                tcsShouldNotFire.TrySetException(new InvalidOperationException("This token should not have fired"));
            }

            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
            using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
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

                Directory.CreateDirectory(Path.Combine(root.RootPath, newDirectoryName));
                Directory.CreateDirectory(Path.Combine(root.RootPath, newDirectoryName, newSubDirectoryName));
                File.Create(Path.Combine(root.RootPath, newDirectoryName, newSubDirectoryName, newFileName));

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

                fileSystemWatcher.CallOnRenamed(new RenamedEventArgs(WatcherChangeTypes.Renamed, root.RootPath, newDirectoryName, oldDirectoryName));

                await Task.WhenAll(oldDirectoryTcs.Task, newDirectoryTcs.Task, newSubDirectoryTcs.Task, newFileTcs.Task).TimeoutAfter(TimeSpan.FromSeconds(30));

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
        public async Task TokenNotFiredForFileNameStartingWithPeriod()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var fileName = "." + Guid.NewGuid().ToString();
                            var token = provider.Watch(Path.GetFileName(fileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, fileName));
                            await Task.Delay(WaitTimeForTokenToFire);

                            Assert.False(token.HasChanged);
                        }
                    }
                }
            }
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Linux, SkipReason = "Hidden and system files only make sense on Windows.")]
        [OSSkipCondition(OperatingSystems.MacOSX, SkipReason = "Hidden and system files only make sense on Windows.")]
        public async Task TokensNotFiredForHiddenAndSystemFiles()
        {
            using (var root = new DisposableFileSystem())
            {
                var hiddenFileName = Guid.NewGuid().ToString();
                var hiddenFilePath = Path.Combine(root.RootPath, hiddenFileName);
                File.Create(hiddenFilePath);
                var fileInfo = new FileInfo(hiddenFilePath);
                File.SetAttributes(hiddenFilePath, fileInfo.Attributes | FileAttributes.Hidden);

                var systemFileName = Guid.NewGuid().ToString();
                var systemFilePath = Path.Combine(root.RootPath, systemFileName);
                File.Create(systemFilePath);
                fileInfo = new FileInfo(systemFilePath);
                File.SetAttributes(systemFilePath, fileInfo.Attributes | FileAttributes.System);

                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            var hiddenFiletoken = provider.Watch(Path.GetFileName(hiddenFileName));
                            var systemFiletoken = provider.Watch(Path.GetFileName(systemFileName));

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, hiddenFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(hiddenFiletoken.HasChanged);

                            fileSystemWatcher.CallOnChanged(new FileSystemEventArgs(WatcherChangeTypes.Changed, root.RootPath, systemFileName));
                            await Task.Delay(WaitTimeForTokenToFire);
                            Assert.False(systemFiletoken.HasChanged);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task TokensFiredForAllEntriesOnError()
        {
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
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
        public async Task WildCardToken_RaisesEventsForNewFilesAdded()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.RootPath + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: false))

            using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
            {
                var token = provider.Watch("**/*.txt");
                var directory = Path.Combine(root.RootPath, "subdir1", "subdir2");

                // Act
                fileSystemWatcher.CallOnCreated(new FileSystemEventArgs(WatcherChangeTypes.Created, directory, "a.txt"));
                await Task.Delay(WaitTimeForTokenToFire);

                // Assert
                Assert.True(token.HasChanged);
            }
        }

        [Fact]
        public async Task WildCardToken_RaisesEventsWhenFileSystemWatcherDoesNotFire()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
            using (var physicalFilesWatcher = new PhysicalFilesWatcher(
                root.RootPath + Path.DirectorySeparatorChar,
                fileSystemWatcher,
                pollForChanges: true))

            using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
            {
                var filePath = Path.Combine(root.RootPath, "subdir1", "subdir2", "file.txt");
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
            using (var root = new DisposableFileSystem())
            {
                using (var provider = new PhysicalFileProvider(root.RootPath))
                {
                    Assert.False(provider.UsePollingFileWatcher);

                    // Act / Assert
                    provider.UsePollingFileWatcher = true;
                    Assert.True(provider.UsePollingFileWatcher);
                }
            }
        }

        [Fact]
        public void UsePollingFileWatcher_FileWatcherNotNull_SetterThrows()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            // Act / Assert
                            Assert.Throws<InvalidOperationException>(() => { provider.UsePollingFileWatcher = true; });
                        }
                    }
                }
            }
        }

        [Fact]
        public void UsePollingFileWatcher_FileWatcherNotNull_ReturnsFalse()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            {
                using (var fileSystemWatcher = new MockFileSystemWatcher(root.RootPath))
                {
                    using (var physicalFilesWatcher = new PhysicalFilesWatcher(root.RootPath + Path.DirectorySeparatorChar, fileSystemWatcher, pollForChanges: false))
                    {
                        using (var provider = new PhysicalFileProvider(root.RootPath) { FileWatcher = physicalFilesWatcher })
                        {
                            // Act / Assert
                            Assert.False(provider.UsePollingFileWatcher);
                        }
                    }
                }
            }
        }

        [Fact]
        public void CreateFileWatcher_CreatesWatcherWithPollingAndActiveFlags()
        {
            // Arrange
            using (var root = new DisposableFileSystem())
            using (var provider = new PhysicalFileProvider(root.RootPath))
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
    }
}
