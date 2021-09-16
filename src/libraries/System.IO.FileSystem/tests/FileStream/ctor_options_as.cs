// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace System.IO.Tests
{
    public abstract class FileStream_ctor_options_as_base : FileStream_ctor_str_fm_fa_fs_buffer_fo
    {
        protected abstract long PreallocationSize { get; }

        protected override string GetExpectedParamName(string paramName) => "value";

        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
                        PreallocationSize = PreallocationSize
                    });

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        PreallocationSize = PreallocationSize
                    });

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(path,
                    new FileStreamOptions
                    {
                        Mode = mode,
                        Access = access,
                        Share = share,
                        BufferSize = bufferSize,
                        Options = options,
                        PreallocationSize = PreallocationSize
                    });

        protected FileStreamOptions GetOptions(FileMode mode, FileAccess access, FileShare share, FileOptions options, long preAllocationSize)
            => new FileStreamOptions
            {
                Mode = mode,
                Access = access,
                Share = share,
                Options = options,
                PreallocationSize = preAllocationSize
            };
    }

    public class FileStream_ctor_options_as_zero : FileStream_ctor_options_as_base
    {
        protected override long PreallocationSize => 0; // specifying 0 should have no effect

        protected override long InitialLength => 0;
    }

    [CollectionDefinition("NoParallelTests", DisableParallelization = true)]
    public partial class NoParallelTests { }

    // Don't run in parallel as the WhenDiskIsFullTheErrorMessageContainsAllDetails test
    // consumes entire available free space on the disk (only on Linux, this is how posix_fallocate works)
    // and if we try to run other disk-writing test in the meantime we are going to get "No space left on device" exception.
    [Collection("NoParallelTests")]
    public partial class FileStream_ctor_options_as : FileStream_ctor_options_as_base
    {
        protected override long PreallocationSize => 10;

        protected override long InitialLength => 10;

        [Fact]
        public virtual void NegativePreallocationSizeThrows()
        {
            string filePath = GetPathToNonExistingFile();
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => new FileStream(filePath, GetOptions(FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, -1)));
        }

        [Theory]
        [InlineData(FileMode.Create, 0L)]
        [InlineData(FileMode.CreateNew, 0L)]
        [InlineData(FileMode.OpenOrCreate, 0L)]
        public void WhenFileIsCreatedWithoutPreallocationSizeSpecifiedItsLengthIsZero(FileMode mode, long preallocationSize)
        {
            using (var fs = new FileStream(GetPathToNonExistingFile(), GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(0, fs.Length);
                Assert.Equal(0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.Open, 20L)]
        [InlineData(FileMode.Open, 5L)]
        [InlineData(FileMode.Append, 20L)]
        [InlineData(FileMode.Append, 5L)]
        public void PreallocationSizeIsIgnoredForFileModeOpenAndAppend(FileMode mode, long preallocationSize)
        {
            const int initialSize = 10;
            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, new byte[initialSize]);

            using (var fs = new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(initialSize, fs.Length); // it has NOT been changed
                Assert.Equal(mode == FileMode.Append ? initialSize : 0, fs.Position);
            }
        }

        [Theory]
        [InlineData(FileMode.OpenOrCreate, 20L)] // preallocationSize > initialSize
        [InlineData(FileMode.OpenOrCreate, 5L)] // preallocationSize < initialSize
        public void WhenExistingFileIsBeingOpenedWithOpenOrCreateModeTheLengthRemainsUnchanged(FileMode mode, long preallocationSize)
        {
            const int initialSize = 10;
            string filePath = GetPathToNonExistingFile();
            byte[] initialData = RandomNumberGenerator.GetBytes(initialSize);
            File.WriteAllBytes(filePath, initialData);

            using (var fs = new FileStream(filePath, GetOptions(mode, FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(initialSize, fs.Length); // it was not changed
                Assert.Equal(0, fs.Position);

                byte[] actualContent = new byte[initialData.Length];
                Assert.Equal(actualContent.Length, fs.Read(actualContent));
                AssertExtensions.SequenceEqual(initialData, actualContent); // the initial content was not changed
            }
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenFileIsCreatedWithPreallocationSizeSpecifiedTheLengthIsSetAndTheContentIsZeroed(FileMode mode)
        {
            const long preallocationSize = 123;

            using (var fs = new FileStream(GetPathToNonExistingFile(), GetOptions(mode, FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(preallocationSize, fs.Length);
                Assert.Equal(0, fs.Position);

                AssertFileContentHasBeenZeroed(0, (int)fs.Length, fs);
            }
        }

        [OuterLoop("Might allocate 1 TB file if there is enough space on the disk")]
        // macOS fcntl doc does not mention ENOSPC error: https://developer.apple.com/library/archive/documentation/System/Conceptual/ManPages_iPhoneOS/man2/fcntl.2.html
        // But depending on the OS version, it might actually return it.
        // Since we don't want to have unstable tests, it's better to not run it on macOS at all.
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.Linux)]
        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.OpenOrCreate)]
        public void WhenDiskIsFullTheErrorMessageContainsAllDetails(FileMode mode)
        {
            const long tooMuch = 1024L * 1024L * 1024L * 1024L; // 1 TB

            string filePath = GetPathToNonExistingFile();

            IOException ex = Assert.Throws<IOException>(() => new FileStream(filePath, GetOptions(mode, FileAccess.Write, FileShare.None, FileOptions.None, tooMuch)));
            Assert.Contains(filePath, ex.Message);
            Assert.Contains(tooMuch.ToString(), ex.Message);

            // ensure it was NOT created
            bool exists = File.Exists(filePath);
            if (exists)
            {
                File.Delete(filePath);
            }
            Assert.False(exists);
        }

        [Fact]
        public void WhenFileIsTruncatedWithPreallocationSizeSpecifiedTheLengthIsSetAndTheContentIsZeroed()
        {
            const int initialSize = 10_000; // this must be more than 4kb which seems to be minimum allocation size on Windows
            const long preallocationSize = 100;

            string filePath = GetPathToNonExistingFile();
            File.WriteAllBytes(filePath, Enumerable.Repeat((byte)1, initialSize).ToArray());

            using (var fs = new FileStream(filePath, GetOptions(FileMode.Truncate, FileAccess.ReadWrite, FileShare.None, FileOptions.None, preallocationSize)))
            {
                Assert.Equal(preallocationSize, fs.Length);
                Assert.Equal(0, fs.Position);

                AssertFileContentHasBeenZeroed(0, (int)fs.Length, fs);
            }
        }

        private string GetPathToNonExistingFile()
        {
            string filePath = GetTestFilePath();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            return filePath;
        }

        private static void AssertFileContentHasBeenZeroed(int from, int to, FileStream fs)
        {
            int expectedByteCount = to - from;
            int extraByteCount = 1;
            byte[] content = Enumerable.Repeat((byte)1, expectedByteCount + extraByteCount).ToArray();
            fs.Position = from;
            Assert.Equal(expectedByteCount, fs.Read(content));
            Assert.All(content.SkipLast(extraByteCount), @byte => Assert.Equal(0, @byte));
            Assert.Equal(to, fs.Position);
        }
    }
}
