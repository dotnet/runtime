// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_EndRead : FileSystemTest
    {
        [Fact]
        public void EndReadThrowsForNullAsyncResult()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create, FileAccess.ReadWrite))
            {
                AssertExtensions.Throws<ArgumentNullException>("asyncResult", () => fs.EndRead(null));
            }
        }
    }
}
