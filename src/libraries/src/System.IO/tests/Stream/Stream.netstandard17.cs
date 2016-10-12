// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Xunit;

namespace System.IO.Tests
{
    public partial class StreamMethods
    {
        [Fact]
        public void CreateWaitHandle()
        {
            using (Stream str = CreateStream())
            {
                ManualResetEvent first = str.CreateWaitHandle();
                ManualResetEvent second = str.CreateWaitHandle();
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
                }
                Assert.Equal(1, str.Length);
            }
        }
    }
}
