// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    public class RandomAccess_SetLength : RandomAccess_Base<long>
    {
        private const long FileSize = 123;

        protected override long MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
        {
            RandomAccess.SetLength(handle, fileOffset);

            return 0;
        }

        protected override bool UsesOffsets => false;

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ModifiesTheActualFileSize(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                RandomAccess.SetLength(handle, FileSize);

                Assert.Equal(FileSize, RandomAccess.GetLength(handle));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void AllowsForShrinking(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                RandomAccess.SetLength(handle, FileSize);
                Assert.Equal(FileSize, RandomAccess.GetLength(handle));

                RandomAccess.SetLength(handle, FileSize / 2);
                Assert.Equal(FileSize / 2, RandomAccess.GetLength(handle));

                RandomAccess.SetLength(handle, 0);
                Assert.Equal(0, RandomAccess.GetLength(handle));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public void ZeroesTheFileContentsWhenExtendingTheFile(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.Write, options: options))
            {
                RandomAccess.SetLength(handle, FileSize);

                byte[] buffer = new byte[FileSize + 1];
                Assert.Equal(FileSize, RandomAccess.Read(handle, buffer, 0));
                Assert.All(buffer, @byte => Assert.Equal(0, @byte));
            }
        }
    }
}
