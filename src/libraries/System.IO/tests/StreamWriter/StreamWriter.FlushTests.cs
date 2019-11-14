// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Xunit;

namespace System.IO.Tests
{
    public class FlushTests
    {
        protected virtual Stream CreateStream()
        {
            return new MemoryStream();
        }

        [Fact]
        public void AutoFlushSetTrue()
        {
            // [] Set the autoflush to true
            var sw2 = new StreamWriter(CreateStream());
            sw2.AutoFlush = true;
            Assert.True(sw2.AutoFlush);
        }

        [Fact]
        public void AutoFlushSetFalse()
        {
            // [] Set autoflush to false
            var sw2 = new StreamWriter(CreateStream());
            sw2.AutoFlush = false;
            Assert.False(sw2.AutoFlush);
        }
    }
}
