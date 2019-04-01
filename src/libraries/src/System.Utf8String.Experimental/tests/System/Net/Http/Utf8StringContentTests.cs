// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public static void Ctor_NullContent_Throws()
        {
            Assert.Throws<ArgumentNullException>("content", () => new Utf8StringContent(null));
            Assert.Throws<ArgumentNullException>("content", () => new Utf8StringContent(null, "application/json"));
        }

        [Theory]
        [InlineData(null, "text/plain")]
        [InlineData("application/json", "application/json")]
        public static void Ctor_SetsContentTypeHeader(string mediaTypeForCtor, string expectedMediaType)
        {
            HttpContent httpContent = new Utf8StringContent(u8("Hello"), mediaTypeForCtor);

            Assert.Equal(expectedMediaType, httpContent.Headers.ContentType.MediaType);
            Assert.Equal(Encoding.UTF8.WebName, httpContent.Headers.ContentType.CharSet);
        }

        [Fact]
        public static async Task Ctor_GetStream()
        {
            MemoryStream memoryStream = new MemoryStream();

            await new Utf8StringContent(u8("Hello")).CopyToAsync(memoryStream);

            Assert.Equal(u8("Hello").ToByteArray(), memoryStream.ToArray());
        }
    }
}
