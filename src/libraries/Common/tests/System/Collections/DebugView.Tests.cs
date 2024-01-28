// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.Collections.Tests
{
    public class DebugView_Tests
    {
        private static IEnumerable<object[]> TestDebuggerAttributes_GenericDictionaries()
        {
            yield return new object[] { new Dictionary<int, string>(), new KeyValuePair<string, string>[0] };
            yield return new object[] { new ReadOnlyDictionary<int, string>(new Dictionary<int, string>()), new KeyValuePair<string, string>[0] };
            yield return new object[] { new SortedDictionary<string, int>(), new KeyValuePair<string, string>[0] };
            yield return new object[] { new SortedList<int, string>(), new KeyValuePair<string, string>[0] };

            yield return new object[] { new Dictionary<int, string>{{1, "One"}, {2, "Two"}},
                new KeyValuePair<string, string>[]
                {
                    new ("[1]", "\"One\""),
                    new ("[2]", "\"Two\""),
                }
            };
            yield return new object[] { new ReadOnlyDictionary<int,string>(new Dictionary<int, string>{{1, "One"}, {2, "Two"}}),
                new KeyValuePair<string, string>[]
                {
                    new ("[1]", "\"One\""),
                    new ("[2]", "\"Two\""),
                }
            };
            yield return new object[] { new SortedDictionary<string, int>{{"One", 1}, {"Two", 2}} ,
                new KeyValuePair<string, string>[]
                {
                    new ("[\"One\"]", "1"),
                    new ("[\"Two\"]", "2"),
                }
            };
            yield return new object[] { new SortedList<string, double> { { "One", 1.0 }, { "Two", 2.0 } },
                new KeyValuePair<string, string>[]
                {
                    new ("[\"One\"]", "1"),
                    new ("[\"Two\"]", "2"),
                }
            };
            CustomKeyedCollection<string, int> collection = new ();
            collection.GetKeyForItemHandler = value =>  (2 * value).ToString();
            collection.InsertItem(0, 1);
            collection.InsertItem(1, 3);
            yield return new object[] { collection,
                new KeyValuePair<string, string>[]
                {
                    new ("[\"2\"]", "1"),
                    new ("[\"6\"]", "3"),
                }
            };
        }

        private static IEnumerable<object[]> TestDebuggerAttributes_NonGenericDictionaries()
        {
            yield return new object[] { new Hashtable(), new KeyValuePair<string, string>[0] };
            yield return new object[] { Hashtable.Synchronized(new Hashtable()), new KeyValuePair<string, string>[0] };
            yield return new object[] { new SortedList(), new KeyValuePair<string, string>[0] };
            yield return new object[] { SortedList.Synchronized(new SortedList()), new KeyValuePair<string, string>[0] };

            yield return new object[] { new Hashtable { { "a", 1 }, { "b", "B" } },
                new KeyValuePair<string, string>[]
                {
                    new ("[\"a\"]", "1"),
                    new ("[\"b\"]", "\"B\""),
                }
            };
            yield return new object[] { Hashtable.Synchronized(new Hashtable { { "a", 1 }, { "b", "B" } }),
                new KeyValuePair<string, string>[]
                {
                    new ("[\"a\"]", "1"),
                    new ("[\"b\"]", "\"B\""),
                }
            };
            yield return new object[] { new SortedList { { "a", 1 }, { "b", "B" } },
                new KeyValuePair<string, string>[]
                {
                    new ("[\"a\"]", "1"),
                    new ("[\"b\"]", "\"B\""),
                }
            };
            yield return new object[] { SortedList.Synchronized(new SortedList { { "a", 1 }, { "b", "B" } }),
                new KeyValuePair<string, string>[]
                {
                    new ("[\"a\"]", "1"),
                    new ("[\"b\"]", "\"B\""),
                }
            };
#if !NETFRAMEWORK // ListDictionaryInternal in .Net Framework is not annotated with debugger attributes.
            yield return new object[] { new Exception().Data, new KeyValuePair<string, string>[0] };
            yield return new object[] { new Exception { Data = { { "a", 1 }, { "b", "B" } } }.Data,
                new KeyValuePair<string, string>[]
                {
                    new ("[\"a\"]", "1"),
                    new ("[\"b\"]", "\"B\""),
                }
            };
#endif
        }

        private static IEnumerable<object[]> TestDebuggerAttributes_ListInputs()
        {
            yield return new object[] { new HashSet<string>() };
            yield return new object[] { new LinkedList<object>() };
            yield return new object[] { new List<int>() };
            yield return new object[] { new Queue<double>() };
            yield return new object[] { new SortedList<int, string>() };
            yield return new object[] { new SortedSet<int>() };
            yield return new object[] { new Stack<object>() };

            yield return new object[] { new Dictionary<double, float>().Keys };
            yield return new object[] { new Dictionary<float, double>().Values };
            yield return new object[] { new SortedDictionary<Guid, string>().Keys };
            yield return new object[] { new SortedDictionary<long, Guid>().Values };
            yield return new object[] { new SortedList<string, int>().Keys };
            yield return new object[] { new SortedList<float, long>().Values };

            yield return new object[] { new HashSet<string> { "One", "Two" } };

            LinkedList<object> linkedList = new();
            linkedList.AddFirst(1);
            linkedList.AddLast(2);
            yield return new object[] { linkedList };
            yield return new object[] { new List<int> { 1, 2 } };

            Queue<double> queue = new();
            queue.Enqueue(1);
            queue.Enqueue(2);
            yield return new object[] { queue };
            yield return new object[] { new SortedSet<int> { 1, 2 } };

            Stack<object> stack = new();
            stack.Push(1);
            stack.Push(2);
            yield return new object[] { stack };

            yield return new object[] { new SortedList<string, int> { { "One", 1 }, { "Two", 2 } }.Keys };
            yield return new object[] { new SortedList<float, long> { { 1f, 1L }, { 2f, 2L } }.Values };

            yield return new object[] { new Dictionary<double, float> { { 1.0, 1.0f }, { 2.0, 2.0f } }.Keys };
            yield return new object[] { new Dictionary<float, double> { { 1.0f, 1.0 }, { 2.0f, 2.0 } }.Values };
            yield return new object[] { new SortedDictionary<Guid, string> { { Guid.NewGuid(), "One" }, { Guid.NewGuid(), "Two" } }.Keys };
            yield return new object[] { new SortedDictionary<long, Guid> { { 1L, Guid.NewGuid() }, { 2L, Guid.NewGuid() } }.Values };
#if !NETFRAMEWORK
            // In .Net Framework 4.8 KeyCollection and ValueCollection from ReadOnlyDictionary are marked with
            // an incorrect debugger type proxy attribute. Both classes have two template parameters, but
            // ICollectionDebugView<> used there has only one. Neither VS nor this testing code is able to
            // create a type proxy in such case.
            yield return new object[] { new ReadOnlyDictionary<double, float>(new Dictionary<double, float> { { 1.0, 1.0f }, { 2.0, 2.0f } }).Keys };
            yield return new object[] { new ReadOnlyDictionary<float, double>(new Dictionary<float, double> { { 1.0f, 1.0 }, { 2.0f, 2.0 } }).Values };
#endif
        }

        public static IEnumerable<object[]> TestDebuggerAttributes_InputsPresentedAsDictionary()
        {
#if !NETFRAMEWORK
            return TestDebuggerAttributes_NonGenericDictionaries()
                .Concat(TestDebuggerAttributes_GenericDictionaries());
#else
            // In .Net Framework only non-generic dictionaries are displayed in a dictionary format by the debugger.
            return TestDebuggerAttributes_NonGenericDictionaries();
#endif
        }

        public static IEnumerable<object[]> TestDebuggerAttributes_InputsPresentedAsList()
        {
#if !NETFRAMEWORK
            return TestDebuggerAttributes_ListInputs();
#else
            // In .Net Framework generic dictionaries are displayed in a list format by the debugger.
            return TestDebuggerAttributes_GenericDictionaries()
                .Select(t => new[] { t[0] })
                .Concat(TestDebuggerAttributes_ListInputs());
#endif
        }

        public static IEnumerable<object[]> TestDebuggerAttributes_Inputs()
        {
            return TestDebuggerAttributes_InputsPresentedAsDictionary()
                .Select(t => new[] { t[0] })
                .Concat(TestDebuggerAttributes_InputsPresentedAsList());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [MemberData(nameof(TestDebuggerAttributes_InputsPresentedAsDictionary))]
        public static void TestDebuggerAttributes_Dictionary(object obj, KeyValuePair<string, string>[] expected)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Array itemArray = (Array)itemProperty.GetValue(info.Instance);
            List<KeyValuePair<string, string>> formatted = itemArray.Cast<object>()
                .Select(DebuggerAttributes.ValidateFullyDebuggerDisplayReferences)
                .Select(formattedResult => new KeyValuePair<string, string>(formattedResult.Key, formattedResult.Value))
               .ToList();

            CollectionAsserts.EqualUnordered((ICollection)expected, formatted);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [MemberData(nameof(TestDebuggerAttributes_InputsPresentedAsList))]
        public static void TestDebuggerAttributes_List(object obj)
        {
            DebuggerAttributes.ValidateDebuggerDisplayReferences(obj);
            DebuggerAttributeInfo info = DebuggerAttributes.ValidateDebuggerTypeProxyProperties(obj);
            PropertyInfo itemProperty = info.Properties.Single(pr => pr.GetCustomAttribute<DebuggerBrowsableAttribute>().State == DebuggerBrowsableState.RootHidden);
            Array items = itemProperty.GetValue(info.Instance) as Array;
            Assert.Equal((obj as IEnumerable).Cast<object>().ToArray(), items.Cast<object>());
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsDebuggerTypeProxyAttributeSupported))]
        [MemberData(nameof(TestDebuggerAttributes_Inputs))]
        public static void TestDebuggerAttributes_Null(object obj)
        {
            TargetInvocationException tie = Assert.Throws<TargetInvocationException>(() => DebuggerAttributes.CreateDebuggerTypeProxyWithNullArgument(obj.GetType()));
            Assert.IsType<ArgumentNullException>(tie.InnerException);
        }

        private class CustomKeyedCollection<TKey, TValue> : KeyedCollection<TKey, TValue> where TKey : notnull
        {
            public CustomKeyedCollection() : base()
            {
            }

            public CustomKeyedCollection(IEqualityComparer<TKey> comparer) : base(comparer)
            {
            }

            public CustomKeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold) : base(comparer, dictionaryCreationThreshold)
            {
            }

            public Func<TValue, TKey> GetKeyForItemHandler { get; set; }

            protected override TKey GetKeyForItem(TValue item)
            {
                return GetKeyForItemHandler(item);
            }

            public new void InsertItem(int index, TValue item)
            {
                base.InsertItem(index, item);
            }
        }
    }
}
