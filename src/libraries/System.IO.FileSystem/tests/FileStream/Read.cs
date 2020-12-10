// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_Read : FileSystemTest
    {
        [Fact]
        public void NegativeReadRootThrows()
        {
            Assert.Throws<UnauthorizedAccessException>(() =>
                new FileStream(Path.GetPathRoot(Directory.GetCurrentDirectory()), FileMode.Open, FileAccess.Read));
        }
    }
}
