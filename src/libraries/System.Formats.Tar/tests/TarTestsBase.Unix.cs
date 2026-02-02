// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase
    {
        protected void VerifyPathsAreHardLinked(string path1, string path2)
        {
            Assert.True(File.Exists(path1));
            Assert.True(File.Exists(path2));
            Assert.Equal(0, Interop.Sys.LStat(path1, out Interop.Sys.FileStatus status1));
            Assert.Equal(0, Interop.Sys.LStat(path2, out Interop.Sys.FileStatus status2));
            Assert.Equal(status1.Ino, status2.Ino);
            Assert.Equal(status1.Dev, status2.Dev);
        }
    }
}
