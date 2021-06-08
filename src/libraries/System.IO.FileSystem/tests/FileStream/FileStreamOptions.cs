// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_FileStreamOptions : FileSystemTest
    {
        private static IEnumerable<FileMode> WritingModes()
        {
            yield return FileMode.Create;
            yield return FileMode.CreateNew;
            yield return FileMode.Append;
            yield return FileMode.Truncate;
        }

        [Fact]
        public void NullOptionsThrows()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new FileStream(GetTestFilePath(), options: null));
            Assert.Equal("options", ex.ParamName);
        }

        [Fact]
        public void Mode()
        {
            FileMode[] validValues = Enum.GetValues<FileMode>();

            foreach (var vaidValue in validValues)
            {
                Assert.Equal(vaidValue, (new FileStreamOptions { Mode = vaidValue }).Mode);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Mode = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Mode = validValues.Max() + 1 });

            var readOnlyOptions = new FileStreamOptions { Access = FileAccess.Read };
            foreach (FileMode writingMode in WritingModes())
            {
                Assert.Throws<ArgumentException>(() => readOnlyOptions.Mode = writingMode);
            }
            var readWriteOptions = new FileStreamOptions { Access = FileAccess.ReadWrite };
            Assert.Throws<ArgumentException>(() => readWriteOptions.Mode = FileMode.Append);
        }

        [Fact]
        public void Access()
        {
            Assert.Equal(FileAccess.ReadWrite, new FileStreamOptions().Access);
            Assert.Equal(FileAccess.Write, new FileStreamOptions() { Mode = FileMode.Append }.Access);

            FileAccess[] validValues = Enum.GetValues<FileAccess>();

            foreach (var vaidValue in validValues)
            {
                Assert.Equal(vaidValue, (new FileStreamOptions { Access = vaidValue }).Access);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Access = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Access = validValues.Max() + 1 });

            var appendOptions = new FileStreamOptions { Mode = FileMode.Append };
            Assert.Throws<ArgumentException>(() => appendOptions.Access = FileAccess.Read);
            Assert.Throws<ArgumentException>(() => appendOptions.Access = FileAccess.ReadWrite);

            foreach (FileMode writingMode in WritingModes())
            {
                FileStreamOptions writingOptions = new FileStreamOptions { Access = FileAccess.Write, Mode = writingMode };
                Assert.Throws<ArgumentException>(() => writingOptions.Access = FileAccess.Read);
            }
        }

        [Fact]
        public void Share()
        {
            Assert.Equal(FileShare.Read, new FileStreamOptions().Share);

            FileShare[] validValues = Enum.GetValues<FileShare>();

            foreach (var vaidValue in validValues)
            {
                Assert.Equal(vaidValue, (new FileStreamOptions { Share = vaidValue }).Share);
            }

            FileShare all = validValues.Aggregate((x, y) => x | y);
            Assert.Equal(all, (new FileStreamOptions { Share = all }).Share);

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Share = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Share = all + 1 });
        }

        [Fact]
        public void Options()
        {
            Assert.Equal(FileOptions.None, new FileStreamOptions().Options);

            FileOptions[] validValues = Enum.GetValues<FileOptions>();

            foreach (var option in validValues)
            {
                Assert.Equal(option, (new FileStreamOptions { Options = option }).Options);
            }

            FileOptions all = validValues.Aggregate((x, y) => x | y);
            Assert.Equal(all, (new FileStreamOptions { Options = all }).Options);

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Options = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Options = all + 1 });
        }

        [Fact]
        public void PreallocationSize()
        {
            Assert.Equal(0, new FileStreamOptions().PreallocationSize);

            Assert.Equal(0, new FileStreamOptions { PreallocationSize = 0 }.PreallocationSize);
            Assert.Equal(1, new FileStreamOptions { PreallocationSize = 1 }.PreallocationSize);
            Assert.Equal(123, new FileStreamOptions { PreallocationSize = 123 }.PreallocationSize);

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { PreallocationSize = -1 });
        }

        [Fact]
        public void BufferSize()
        {
            Assert.Equal(4096, new FileStreamOptions().BufferSize);

            Assert.Equal(0, new FileStreamOptions { BufferSize = 0 }.BufferSize);
            Assert.Equal(1, new FileStreamOptions { BufferSize = 1 }.BufferSize);
            Assert.Equal(123, new FileStreamOptions { BufferSize = 123 }.BufferSize);

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { BufferSize = -1 });
        }
    }
}
