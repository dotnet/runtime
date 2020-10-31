// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.IO.Tests
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
