// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Tests
{
    // to avoid a lot of code duplication, we reuse FileStream tests
    public class File_OpenHandle : FileStream_ctor_options_as
    {
        protected override FileStream CreateFileStream(string path, FileMode mode)
            => new FileStream(File.OpenHandle(path, mode), mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access)
            => new FileStream(File.OpenHandle(path, mode, access), access);

        protected override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
            => new FileStream(File.OpenHandle(path, mode, access, share, options), access, bufferSize);

        [Fact]
        public override void NegativePreallocationSizeThrows()
        {
            ArgumentOutOfRangeException ex = Assert.Throws<ArgumentOutOfRangeException>(
                () => File.OpenHandle("validPath", FileMode.CreateNew, FileAccess.Write, FileShare.None, FileOptions.None, -1));
        }
    }
}
