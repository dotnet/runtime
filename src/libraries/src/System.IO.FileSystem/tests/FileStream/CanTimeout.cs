// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FileStream_CanTimeout : FileSystemTest
    {
        [Fact]
        public void CanTimeoutFalse()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.False(fs.CanTimeout);
                Assert.Throws<InvalidOperationException>(() => fs.ReadTimeout);
                Assert.Throws<InvalidOperationException>(() => fs.ReadTimeout = 1);
                Assert.Throws<InvalidOperationException>(() => fs.WriteTimeout);
                Assert.Throws<InvalidOperationException>(() => fs.WriteTimeout = 1);
            }
        }
    }
}
