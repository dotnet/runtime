// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    public partial class ConfigurationBinderCollectionTests
    {
        public class UninitializedCollectionsOptions
        {
            public IEnumerable<string> IEnumerable { get; set; }
            public IDictionary<string, string> IDictionary { get; set; }
            public ICollection<string> ICollection { get; set; }
            public IList<string> IList { get; set; }
            public IReadOnlyCollection<string> IReadOnlyCollection { get; set; }
            public IReadOnlyList<string> IReadOnlyList { get; set; }
            public IReadOnlyDictionary<string, string> IReadOnlyDictionary { get; set; }
        }

        public class InitializedCollectionsOptions
        {
            public InitializedCollectionsOptions()
            {
                AlreadyInitializedIEnumerableInterface = ListUsedInIEnumerableFieldAndShouldNotBeTouched;
                AlreadyInitializedDictionary = ExistingDictionary;
            }

            public List<string> ListUsedInIEnumerableFieldAndShouldNotBeTouched = new()
            {
                "This was here too",
                "Don't touch me!"
            };

            public static ReadOnlyDictionary<string, string> ExistingDictionary = new(
                new Dictionary<string, string>
                {
                    {"existing_key_1", "val_1"},
                    {"existing_key_2", "val_2"}
                });

            public IEnumerable<string> AlreadyInitializedIEnumerableInterface { get; set; }

            public IEnumerable<string> AlreadyInitializedCustomListDerivedFromIEnumerable { get; set; } =
                new CustomListDerivedFromIEnumerable();

            public IEnumerable<string> AlreadyInitializedCustomListIndirectlyDerivedFromIEnumerable { get; set; } =
                new CustomListIndirectlyDerivedFromIEnumerable();

            public IReadOnlyDictionary<string, string> AlreadyInitializedDictionary { get; set; }

            public ICollection<string> ICollectionNoSetter { get; } = new List<string>();
        }

        public class CustomList : List<string>
        {
            // Add an overload, just to make sure binding picks the right Add method
            public void Add(string a, string b)
            {
            }
        }

        public class CustomListDerivedFromIEnumerable : IEnumerable<string>
        {
            private readonly List<string> _items = new List<string> { "Item1", "Item2" };

            public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal interface IDerivedOne : IDerivedTwo
        {
        }

        internal interface IDerivedTwo : IEnumerable<string>
        {
        }

        public class CustomListIndirectlyDerivedFromIEnumerable : IDerivedOne
        {
            private readonly List<string> _items = new List<string> { "Item1", "Item2" };

            public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class NestedOptions
        {
            public int Integer { get; set; }

            public List<string> ListInNestedOption { get; set; }

            public int[] ArrayInNestedOption { get; set; }
        }

        public enum KeyEnum
        {
            abc,
            def,
            ghi
        }

        public enum KeyUintEnum : uint
        {
            abc,
            def,
            ghi
        }

        public class OptionsWithArrays
        {
            public const string InitialValue = "This was here before";

            public OptionsWithArrays()
            {
                AlreadyInitializedArray = new string[] { InitialValue, null, null };
            }

            public string[] AlreadyInitializedArray { get; set; }

            public string[] StringArray { get; set; }

            // this should throw because we do not support multidimensional arrays
            public string[,] DimensionalArray { get; set; }

            public string[][] JaggedArray { get; set; }

            public NestedOptions[] ObjectArray { get; set; }

            public int[] ReadOnlyArray { get; } = new[] { 1, 2 };
        }

        public class OptionsWithLists
        {
            public OptionsWithLists()
            {
                AlreadyInitializedList = new List<string>
                {
                    "This was here before"
                };
                AlreadyInitializedListInterface = new List<string>
                {
                    "This was here too"
                };
            }

            public CustomList CustomList { get; set; }

            public List<string> StringList { get; set; }

            public List<int> IntList { get; set; }

            // This cannot be initialized because we cannot
            // activate an interface
            public IList<string> StringListInterface { get; set; }

            public List<List<string>> NestedLists { get; set; }

            public List<string> AlreadyInitializedList { get; set; }

            public List<NestedOptions> ObjectList { get; set; }

            public IList<string> AlreadyInitializedListInterface { get; set; }

            public List<string> ListPropertyWithoutSetter { get; } = new();
        }

        public class OptionsWithDictionary
        {
            public OptionsWithDictionary()
            {
                AlreadyInitializedStringDictionaryInterface = new Dictionary<string, string>
                {
                    ["123"] = "This was already here"
                };

                AlreadyInitializedHashSetDictionary = new Dictionary<string, HashSet<string>>
                {
                    ["123"] = new HashSet<string>(new[] { "This was already here" })
                };
            }

            public Dictionary<string, int> IntDictionary { get; set; }

            public Dictionary<string, string> StringDictionary { get; set; }

            public IDictionary<string, string> IDictionaryNoSetter { get; } = new Dictionary<string, string>();

            public Dictionary<string, NestedOptions> ObjectDictionary { get; set; }

            public Dictionary<string, ISet<string>> ISetDictionary { get; set; }
            public Dictionary<string, List<string>> ListDictionary { get; set; }

            public Dictionary<NestedOptions, string> NonStringKeyDictionary { get; set; }

            // This cannot be initialized because we cannot
            // activate an interface
            public IDictionary<string, string> StringDictionaryInterface { get; set; }

            public IDictionary<string, string> AlreadyInitializedStringDictionaryInterface { get; set; }
            public IDictionary<string, HashSet<string>> AlreadyInitializedHashSetDictionary { get; set; }
        }

        public class OptionsWithInterdependentProperties
        {
            public IEnumerable<int> FilteredConfigValues => ConfigValues.Where(p => p > 10);
            public IEnumerable<int> ConfigValues { get; set; }
        }

        public class ImplementerOfIDictionaryClass<TKey, TValue> : IDictionary<TKey, TValue>
        {
            private Dictionary<TKey, TValue> _dict = new();

            public TValue this[TKey key] { get => _dict[key]; set => _dict[key] = value; }

            public ICollection<TKey> Keys => _dict.Keys;

            public ICollection<TValue> Values => _dict.Values;

            public int Count => _dict.Count;

            public bool IsReadOnly => false;

            public void Add(TKey key, TValue value) => _dict.Add(key, value);

            public void Add(KeyValuePair<TKey, TValue> item) => _dict.Add(item.Key, item.Value);

            public void Clear() => _dict.Clear();

            public bool Contains(KeyValuePair<TKey, TValue> item) => _dict.Contains(item);

            public bool ContainsKey(TKey key) => _dict.ContainsKey(key);

            public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotImplementedException();

            public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dict.GetEnumerator();

            public bool Remove(TKey key) => _dict.Remove(key);

            public bool Remove(KeyValuePair<TKey, TValue> item) => _dict.Remove(item.Key);

            public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _dict.GetEnumerator();

            // The following are members which have the same names as the IDictionary<,> members.
            // The following members test that there's no System.Reflection.AmbiguousMatchException when binding to the dictionary.
            private string? v;
            public string? this[string key] { get => v; set => v = value; }
            public bool TryGetValue() { return true; }

        }

        public class ExtendedDictionary<TKey, TValue> : Dictionary<TKey, TValue>
        {
        }

        public class Foo
        {
            public IReadOnlyDictionary<string, int> Items { get; set; } =
                new Dictionary<string, int> { { "existing-item1", 1 }, { "existing-item2", 2 } };

        }

        public class MyClassWithCustomSet
        {
            public ICustomSet<string> CustomSet { get; set; }
        }

        public class MyClassWithCustomDictionary
        {
            public ICustomDictionary<string, int> CustomDictionary { get; set; }
        }

        public class ConfigWithInstantiatedIReadOnlyDictionary
        {
            public static Dictionary<string, int> _existingDictionary = new()
                    {
                        {"existing-item1", 1},
                        {"existing-item2", 2},
                    };

            public IReadOnlyDictionary<string, int> Dictionary { get; set; } =
                _existingDictionary;
        }

        public class ConfigWithNonInstantiatedReadOnlyDictionary
        {
            public IReadOnlyDictionary<string, int> Dictionary { get; set; } = null!;
        }

        public class ConfigWithInstantiatedConcreteDictionary
        {
            public static Dictionary<string, int> _existingDictionary = new()
                    {
                        {"existing-item1", 1},
                        {"existing-item2", 2},
                    };

            public Dictionary<string, int> Dictionary { get; set; } =
                _existingDictionary;
        }

        public class MyClassWithCustomCollections
        {
            public ICustomCollectionDerivedFromIEnumerableT<string> CustomIEnumerableCollection { get; set; }
            public ICustomCollectionDerivedFromICollectionT<string> CustomCollection { get; set; }
        }

        public interface ICustomCollectionDerivedFromIEnumerableT<out T> : IEnumerable<T> { }
        public interface ICustomCollectionDerivedFromICollectionT<T> : ICollection<T> { }

        public interface ICustomSet<T> : ISet<T>
        {
        }

        public interface ICustomDictionary<T, T1> : IDictionary<T, T1>
        {
        }

        public class OptionsWithDifferentCollectionInterfaces
        {
            private static IEnumerable<string> s_instantiatedIEnumerable = new List<string> { "value1", "value2" };
            public bool IsSameInstantiatedIEnumerable() => object.ReferenceEquals(s_instantiatedIEnumerable, InstantiatedIEnumerable);
            public IEnumerable<string> InstantiatedIEnumerable { get; set; } = s_instantiatedIEnumerable;

            private static IList<string> s_instantiatedIList = new List<string> { "value1", "value2" };
            public bool IsSameInstantiatedIList() => object.ReferenceEquals(s_instantiatedIList, InstantiatedIList);
            public IList<string> InstantiatedIList { get; set; } = s_instantiatedIList;

            private static IReadOnlyList<string> s_instantiatedIReadOnlyList = new List<string> { "value1", "value2" };
            public bool IsSameInstantiatedIReadOnlyList() => object.ReferenceEquals(s_instantiatedIReadOnlyList, InstantiatedIReadOnlyList);
            public IReadOnlyList<string> InstantiatedIReadOnlyList { get; set; } = s_instantiatedIReadOnlyList;

            private static IDictionary<string, string> s_instantiatedIDictionary = new Dictionary<string, string> { ["Key1"] = "value1", ["Key2"] = "value2" };
            public IDictionary<string, string> InstantiatedIDictionary { get; set; } = s_instantiatedIDictionary;
            public bool IsSameInstantiatedIDictionary() => object.ReferenceEquals(s_instantiatedIDictionary, InstantiatedIDictionary);

            private static IReadOnlyDictionary<string, string> s_instantiatedIReadOnlyDictionary = new Dictionary<string, string> { ["Key1"] = "value1", ["Key2"] = "value2" };
            public IReadOnlyDictionary<string, string> InstantiatedIReadOnlyDictionary { get; set; } = s_instantiatedIReadOnlyDictionary;
            public bool IsSameInstantiatedIReadOnlyDictionary() => object.ReferenceEquals(s_instantiatedIReadOnlyDictionary, InstantiatedIReadOnlyDictionary);

            private static ISet<string> s_instantiatedISet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "A", "b" };
            public ISet<string> InstantiatedISet { get; set; } = s_instantiatedISet;
            public bool IsSameInstantiatedISet() => object.ReferenceEquals(s_instantiatedISet, InstantiatedISet);

#if NETCOREAPP
            private static IReadOnlySet<string> s_instantiatedIReadOnlySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a", "A", "b" };
            public IReadOnlySet<string> InstantiatedIReadOnlySet { get; set; } = s_instantiatedIReadOnlySet;
            public bool IsSameInstantiatedIReadOnlySet() => object.ReferenceEquals(s_instantiatedIReadOnlySet, InstantiatedIReadOnlySet);

            public IReadOnlySet<string> UnInstantiatedIReadOnlySet { get; set; }
#endif
            private static ICollection<string> s_instantiatedICollection = new List<string> { "a", "b", "c" };
            public ICollection<string> InstantiatedICollection { get; set; } = s_instantiatedICollection;
            public bool IsSameInstantiatedICollection() => object.ReferenceEquals(s_instantiatedICollection, InstantiatedICollection);

            private static IReadOnlyCollection<string> s_instantiatedIReadOnlyCollection = new List<string> { "a", "b", "c" };
            public IReadOnlyCollection<string> InstantiatedIReadOnlyCollection { get; set; } = s_instantiatedIReadOnlyCollection;
            public bool IsSameInstantiatedIReadOnlyCollection() => object.ReferenceEquals(s_instantiatedIReadOnlyCollection, InstantiatedIReadOnlyCollection);

            public IReadOnlyCollection<string> UnInstantiatedIReadOnlyCollection { get; set; }
            public ICollection<string> UnInstantiatedICollection { get; set; }
            public ISet<string> UnInstantiatedISet { get; set; }
            public IReadOnlyDictionary<string, string> UnInstantiatedIReadOnlyDictionary { get; set; }
            public IEnumerable<string> UnInstantiatedIEnumerable { get; set; }
            public IList<string> UnInstantiatedIList { get; set; }
            public IReadOnlyList<string> UnInstantiatedIReadOnlyList { get; set; }
        }
    }
}
