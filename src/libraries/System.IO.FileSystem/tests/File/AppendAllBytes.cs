// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Tests;
using System.Linq;
using System.Text;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendAllBytes : FileSystemTest
    {

        [Fact]
        public void NullParameters()
        {
            string path = GetTestFilePath();

            Assert.Throws<ArgumentNullException>(() => File.AppendAllBytes(null, new byte[] { 1, 2, 3 }));
            Assert.Throws<ArgumentException>(() => File.AppendAllBytes(string.Empty, new byte[] { 1, 2, 3 }));
            Assert.Throws<ArgumentNullException>(() => File.AppendAllBytes(path, null));
        }


        [Fact]
        public void AppendAllBytes_WithValidInput_AppendsBytes()
        {
            string path = GetTestFilePath();

            byte[] initialBytes = new byte[] { 1, 2, 3 };
            byte[] additionalBytes = new byte[] { 4, 5, 6 };

            File.WriteAllBytes(path, initialBytes);
            File.AppendAllBytes(path, additionalBytes);

            byte[] result = File.ReadAllBytes(path);

            Assert.True(result.SequenceEqual(new byte[] { 1, 2, 3, 4, 5, 6 }));
        }
    }
}
