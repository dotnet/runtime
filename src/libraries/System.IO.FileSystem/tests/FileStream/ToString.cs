// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Xunit;

namespace System.IO.FileSystem.Tests
{
    public class FileStream_ToString : FileSystemTest
    {
        [Fact]
        public void ToStringDefault()
        {
            using (FileStream fs = new FileStream(GetTestFilePath(), FileMode.Create))
            {
                Assert.Equal(typeof(FileStream).ToString(), fs.ToString());
            }
        }
    }
}
