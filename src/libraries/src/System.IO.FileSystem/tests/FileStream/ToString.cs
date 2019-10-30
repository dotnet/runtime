// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace System.IO.Tests
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
