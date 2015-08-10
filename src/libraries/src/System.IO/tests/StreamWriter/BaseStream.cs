// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using Xunit;

namespace StreamWriterTests
{
    public class BaseStream
    {
        [Fact]
        public static void GetBaseStream()
        {
            // [] Get an underlying memorystream
            MemoryStream memstr2 = new MemoryStream();
            StreamWriter sw = new StreamWriter(memstr2);
            Assert.Same(sw.BaseStream, memstr2);
        }
    }
}
