// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase
    {
        protected void AssertPathsAreHardLinked(string path1, string path2)
        {
            Assert.Equal(GetFileId(path1), GetFileId(path2));
        }

        protected void AssertPathsAreNotHardLinked(string path1, string path2)
        {
            Assert.NotEqual(GetFileId(path1), GetFileId(path2));
        }

        private static (long dev, long ino) GetFileId(string path)
        {
            Assert.Equal(0, Interop.Sys.LStat(path, out Interop.Sys.FileStatus status));

            return (status.Dev, status.Ino);
        }
    }
}
