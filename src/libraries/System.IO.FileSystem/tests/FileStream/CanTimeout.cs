// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
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
