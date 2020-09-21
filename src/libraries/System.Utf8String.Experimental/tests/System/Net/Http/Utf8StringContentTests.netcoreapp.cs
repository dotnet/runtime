// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using static System.Tests.Utf8TestUtilities;

namespace System.Net.Http.Tests
{
    public partial class Utf8StringContentTests
    {
        [Fact]
        public static void Ctor_CopyTo_GetStream()
        {
            var memoryStream = new MemoryStream();

            new Utf8StringContent(u8("Hello")).CopyTo(memoryStream, default, default);

            Assert.Equal(u8("Hello").ToByteArray(), memoryStream.ToArray());
        }

        [Fact]
        public static void Ctor_ReadAsStream()
        {
            var content = new Utf8StringContent(u8("Hello"));
            Stream stream = content.ReadAsStream();

            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            memoryStream.Seek(0, SeekOrigin.Begin);
            Assert.Equal(u8("Hello").ToByteArray(), memoryStream.ToArray());
        }
    }
}
