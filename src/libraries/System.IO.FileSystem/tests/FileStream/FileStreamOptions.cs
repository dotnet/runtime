// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_FileStreamOptions : FileSystemTest
    {
        [Fact]
        public void NullOptionsThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("options", () => new FileStream(GetTestFilePath(), options: null));
        }

        [Theory]
        [InlineData(FileMode.Create)]
        [InlineData(FileMode.CreateNew)]
        [InlineData(FileMode.Append)]
        [InlineData(FileMode.Truncate)]
        public void ModesThatRequireWriteAccessThrowWhenReadAccessIsProvided(FileMode fileMode)
        {
            Assert.Throws<ArgumentException>(() => new FileStream(GetTestFilePath(), new FileStreamOptions
            {
                Mode = fileMode,
                Access = FileAccess.Read
            }));
        }

        [Theory]
        [InlineData(FileAccess.Read)]
        [InlineData(FileAccess.ReadWrite)]
        public void AppendWorksOnlyForWriteAccess(FileAccess fileAccess)
        {
            Assert.Throws<ArgumentException>(() => new FileStream(GetTestFilePath(), new FileStreamOptions
            {
                Mode = FileMode.Append,
                Access = fileAccess
            }));
        }

        [Fact]
        public void Mode()
        {
            Assert.Equal(FileMode.Open, new FileStreamOptions().Mode);

            FileMode[] validValues = Enum.GetValues<FileMode>();

            foreach (var vaidValue in validValues)
            {
                Assert.Equal(vaidValue, (new FileStreamOptions { Mode = vaidValue }).Mode);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Mode = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Mode = validValues.Max() + 1 });
        }

        [Fact]
        public void Access()
        {
            Assert.Equal(FileAccess.Read, new FileStreamOptions().Access);

            FileAccess[] validValues = Enum.GetValues<FileAccess>();

            foreach (var vaidValue in validValues)
            {
                Assert.Equal(vaidValue, (new FileStreamOptions { Access = vaidValue }).Access);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Access = validValues.Min() - 1 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new FileStreamOptions { Access = validValues.Max() + 1 });
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
