// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

namespace System.Threading.ThreadPools.Tests
{
    public partial class ThreadPoolTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void QueueUserWorkItem_PreferLocal_InvalidArguments_Throws(bool preferLocal)
        {
            Assert.Throws<ArgumentNullException>(() => ThreadPool.QueueUserWorkItem(null, new object(), preferLocal));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_NullValidForState(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(s =>
            {
                tcs.SetResult(84);
            }, null, preferLocal);
            Assert.Equal(84, await tcs.Task);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task QueueUserWorkItem_PreferLocal_StateObjectPassedThrough(bool preferLocal)
        {
            var tcs = new TaskCompletionSource<int>();
            ThreadPool.QueueUserWorkItem(s =>
            {
                ((TaskCompletionSource<int>)s).SetResult(84);
            }, tcs, preferLocal);
            Assert.Equal(84, await tcs.Task);
        }
    }
}
