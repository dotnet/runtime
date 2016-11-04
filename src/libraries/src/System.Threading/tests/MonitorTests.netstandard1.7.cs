// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Xunit;

namespace System.Threading.Tests
{
    static partial class MonitorTests
    {
        [Fact]
        public static void WaitTest()
        {
            var obj = new object();
            var waitTests =
                new Func<bool>[]
                {
                    () => Monitor.Wait(obj, FailTimeoutMilliseconds, false),
                    () => Monitor.Wait(obj, FailTimeoutMilliseconds, true),
                    () => Monitor.Wait(obj, TimeSpan.FromMilliseconds(FailTimeoutMilliseconds), false),
                    () => Monitor.Wait(obj, TimeSpan.FromMilliseconds(FailTimeoutMilliseconds), true),
                };

            var t =
                new Thread(() =>
                {
                    Monitor.Enter(obj);
                    for (int i = 0; i < waitTests.Length; ++i)
                    {
                        Monitor.Pulse(obj);
                        Monitor.Wait(obj, FailTimeoutMilliseconds);
                    }
                    Monitor.Exit(obj);
                });
            t.IsBackground = true;

            Monitor.Enter(obj);
            t.Start();
            foreach (var waitTest in waitTests)
            {
                Assert.True(waitTest());
                Monitor.Pulse(obj);
            }
            Monitor.Exit(obj);
        }
    }
}
