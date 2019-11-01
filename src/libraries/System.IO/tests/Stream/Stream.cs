// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public class TestStream : MemoryStream
    {
        public WaitHandle CreateHandle()
        {
#pragma warning disable CS0618
            return CreateWaitHandle();
        }
    }

    public partial class StreamMethods
    {
        [Fact]
        public void CreateWaitHandleTest()
        {
            using (TestStream str = new TestStream())
            {
                WaitHandle first = str.CreateHandle();
                WaitHandle second = str.CreateHandle();
                Assert.NotNull(first);
                Assert.NotNull(second);
                Assert.NotEqual(first, second);
            }
        }

        [Fact]
        public void Synchronized_NewObject()
        {
            using (Stream str = CreateStream())
            {
                using (Stream synced = Stream.Synchronized(str))
                {
                    Assert.NotEqual(synced, str);
                    synced.Write(new byte[] { 1 }, 0, 1);
                    Assert.Equal(1, str.Length);
                }
            }
        }
    }
}
