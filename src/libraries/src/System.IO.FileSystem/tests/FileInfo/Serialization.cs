// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.IO.Tests
{
    public class FileInfo_Serialization : FileSystemTest
    {
        [Fact]
        public void SerializeDeserialize_Roundtrip()
        {
            var orig = new DirectoryInfo("SomePath");
            DirectoryInfo cloned = BinaryFormatterHelpers.Clone(orig);
            Assert.Equal(orig.Name, cloned.Name);
            Assert.Equal(orig.FullName, cloned.FullName);
            Assert.Equal(orig.ToString(), cloned.ToString());
        }
    }
}
