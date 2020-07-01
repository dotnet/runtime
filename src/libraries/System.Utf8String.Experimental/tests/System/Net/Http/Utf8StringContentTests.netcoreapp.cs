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
    [SkipOnMono("The features from System.Utf8String.Experimental namespace are experimental.")]
    public partial class Utf8StringContentTests
    {
        [Fact]
        public static void Ctor_CopyTo_GetStream()
        {
            MemoryStream memoryStream = new MemoryStream();

            new Utf8StringContent(u8("Hello")).CopyTo(memoryStream, default, default);

            Assert.Equal(u8("Hello").ToByteArray(), memoryStream.ToArray());
        }
    }
}
