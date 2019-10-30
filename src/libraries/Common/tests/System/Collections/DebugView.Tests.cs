// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

            yield return new object[] { new Dictionary<int, string>{{1, "One"}, {2, "Two"}} };
            yield return new object[] { new HashSet<string>{"One", "Two"} };

            LinkedList<object> linkedList = new LinkedList<object>();
            linkedList.AddFirst(1);
            linkedList.AddLast(2);
            yield return new object[] { linkedList };
            yield return new object[] { new List<int>{1, 2} };

            Queue<double> queue = new Queue<double>();
            queue.Enqueue(1);
            queue.Enqueue(2);
            yield return new object[] { queue };
            yield return new object[] { new SortedDictionary<string, int>{{"One", 1}, {"Two", 2}} };
            yield return new object[] { new SortedList<int, string>{{1, "One"}, {2, "Two"}} };
            yield return new object[] { new SortedSet<int>{1, 2} };

            var stack = new Stack<object>();
            stack.Push(1);
            stack.Push(2);
            yield return new object[] { stack };

            yield return new object[] { new Dictionary<double, float>{{1.0, 1.0f}, {2.0, 2.0f}}.Keys };
            yield return new object[] { new Dictionary<float, double>{{1.0f, 1.0}, {2.0f, 2.0}}.Values };
            yield return new object[] { new SortedDictionary<Guid, string>{{Guid.NewGuid(), "One"}, {Guid.NewGuid(), "Two"}}.Keys };
            yield return new object[] { new SortedDictionary<long, Guid>{{1L, Guid.NewGuid()}, {2L, Guid.NewGuid()}}.Values };
            yield return new object[] { new SortedList<string, int>{{"One", 1}, {"Two", 2}}.Keys };
            yield return new object[] { new SortedList<float, long>{{1f, 1L}, {2f, 2L}}.Values };
        }

        [Theory]
        [MemberData(nameof(TestDebuggerAttributes_Inputs))]
        public static void TestDebuggerAttributes(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Array items = itemProperty.GetValue(info.Instance) as Array;
            Assert.Equal((obj as IEnumerable).Cast<object>().ToArray(), items.Cast<object>());
        }

        [Theory]
        [MemberData(nameof(TestDebuggerAttributes_Inputs))]
        public static void TestDebuggerAttributes_Null(object obj)
        {
            Type proxyType = DebuggerAttributes.GetProxyType(obj);
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => Activator.CreateInstance(proxyType, (object)null));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }
    }
}
