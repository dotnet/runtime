// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Tests
{
    public class DebugView_Tests
    {
        public static IEnumerable<object[]> TestDebuggerAttributes_Inputs()
        {
            yield return new object[] { new Dictionary<int, string>() };
            yield return new object[] { new HashSet<string>() };
            yield return new object[] { new LinkedList<object>() };
            yield return new object[] { new List<int>() };
            yield return new object[] { new Queue<double>() };
            yield return new object[] { new SortedDictionary<string, int>() };
            yield return new object[] { new SortedList<int, string>() };
            yield return new object[] { new SortedSet<int>() };
            yield return new object[] { new Stack<object>() };

            yield return new object[] { new Dictionary<double, float>().Keys };
            yield return new object[] { new Dictionary<float, double>().Values };
            yield return new object[] { new SortedDictionary<Guid, string>().Keys };
            yield return new object[] { new SortedDictionary<long, Guid>().Values };
            yield return new object[] { new SortedList<string, int>().Keys };
            yield return new object[] { new SortedList<float, long>().Values };
        }

        [Theory]
        [MemberData("TestDebuggerAttributes_Inputs")]
        public static void TestDebuggerAttributes(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        }

        [Theory]
        [MemberData("TestDebuggerAttributes_Inputs")]
        public static void TestDebuggerAttributes_Null(object obj)
        {
            Type proxyType = DebuggerAttributes.GetProxyType(obj);
            Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
        }
    }
}
