// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xunit;

namespace TestSupport.Collections
{
    public class DebuggerTests
    {
        [Theory]
        [MemberData("TestDebuggerAttributes_Inputs")]
        public static void TestDebuggerAttributes(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
        }

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
    }
}
