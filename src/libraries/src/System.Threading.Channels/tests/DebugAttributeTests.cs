// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading.Channels;
using System.Collections.Generic;
using Xunit;

namespace System.Threading.Channels.Tests
{
    public class DebugAttributeTests
    {
        public static IEnumerable<object[]> TestData()
        {
            var c1 = Channel.CreateUnbounded<int>();
            yield return new object[] { c1 };
            yield return new object[] { c1.Reader };
            yield return new object[] { c1.Writer };

            var c2 = Channel.CreateUnbounded<int>(new UnboundedChannelOptions() { SingleReader = true });
            yield return new object[] { c2 };
            yield return new object[] { c2.Reader };
            yield return new object[] { c2.Writer };

            var c3 = Channel.CreateBounded<int>(10);
            yield return new object[] { c3 };
            yield return new object[] { c3.Reader };
            yield return new object[] { c3.Writer };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void TestDebuggerDisplaysAndTypeProxies(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        }

        [Fact]
        public void TestDequeueClass()
        {
            var c = Channel.CreateBounded<int>(10);
            DebuggerAttributes.ValidateDebuggerDisplayReferences(DebuggerAttributes.GetFieldValue(c, "_items"));
        }

    }
}
