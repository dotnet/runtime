// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ctor_str_fm_fa_fs_buffer_fo : FileStream_ctor_str_fm_fa_fs_buffer
    {
        protected sealed override FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
        {
            return CreateFileStream(path, mode, access, share, bufferSize, FileOptions.None);
        }

        protected virtual FileStream CreateFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
        {
            return new FileStream(path, mode, access, share, bufferSize, options);
        }

        [Fact]
        public void InvalidFileOptionsThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>("options", () => CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, ~FileOptions.None));
        }

        [Fact]
        public void ValidFileOptions()
        {
            foreach(FileOptions option in Enum.GetValues(typeof(FileOptions)))
            {
                using (CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, option))
                { }
            }

            // FILE_FLAG_NO_BUFFERRING is also supported
            using (CreateFileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite, FileShare.Read, c_DefaultBufferSize, (FileOptions)0x20000000))
            { }
        }

    }
}
