// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Test;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    internal static class TestHelpers
    {
        public const bool NotSourceGenMode
#if BUILDING_SOURCE_GENERATOR_TESTS
            = false;
#else
            = true;
#endif

        public static IConfiguration GetConfigurationFromJsonString(string json)
        {
            var builder = new ConfigurationBuilder();
            var configuration = builder
                .AddJsonStream(TestStreamHelpers.StringToStream(json))
                .Build();
            return configuration;
        }
    }

    #region // Shared test classes
    public class ComplexOptions
    {
        private static Dictionary<string, int> _existingDictionary = new()
            {
                {"existing-item1", 1},
                {"existing-item2", 2},
            };

        public ComplexOptions()
        {
            Nested = new NestedOptions();
            Virtual = "complex";
        }

        public NestedOptions Nested { get; set; }
        public int Integer { get; set; }
        public bool Boolean { get; set; }
        public virtual string Virtual { get; set; }
        public object Object { get; set; }

        public string PrivateSetter { get; private set; }
        public string ProtectedSetter { get; protected set; }
        public string InternalSetter { get; internal set; }
        public static string StaticProperty { get; set; }

        private string PrivateProperty { get; set; }
        internal string InternalProperty { get; set; }
        protected string ProtectedProperty { get; set; }

        [ConfigurationKeyName("Named_Property")]
        public string NamedProperty { get; set; }

        protected string ProtectedPrivateSet { get; private set; }

        private string PrivateReadOnly { get; }
        internal string InternalReadOnly { get; }
        protected string ProtectedReadOnly { get; }

        public string ReadOnly
        {
            get { return null; }
        }

        public ISet<string> NonInstantiatedISet { get; set; } = null!;
        public HashSet<string> NonInstantiatedHashSet { get; set; } = null!;
        public IDictionary<string, ISet<string>> NonInstantiatedDictionaryWithISet { get; set; } = null!;
        public IDictionary<string, HashSet<string>> InstantiatedDictionaryWithHashSet { get; set; } =
            new Dictionary<string, HashSet<string>>();

        public IDictionary<string, HashSet<string>> InstantiatedDictionaryWithHashSetWithSomeValues { get; set; } =
            new Dictionary<string, HashSet<string>>
            {
                {"item1", new HashSet<string>(new[] {"existing1", "existing2"})}
            };

        public IEnumerable<string> NonInstantiatedIEnumerable { get; set; } = null!;

        public ISet<string> InstantiatedISet { get; set; } = new HashSet<string>();

        public ISet<string> ISetNoSetter { get; } = new HashSet<string>();

        public HashSet<string> InstantiatedHashSetWithSomeValues { get; set; } =
            new HashSet<string>(new[] { "existing1", "existing2" });

        public SortedSet<string> InstantiatedSortedSetWithSomeValues { get; set; } =
            new SortedSet<string>(new[] { "existing1", "existing2" });

        public SortedSet<string> NonInstantiatedSortedSetWithSomeValues { get; set; } = null!;

        public ISet<string> InstantiatedISetWithSomeValues { get; set; } =
            new HashSet<string>(new[] { "existing1", "existing2" });

        public ISet<UnsupportedTypeInHashSet> HashSetWithUnsupportedKey { get; set; } =
            new HashSet<UnsupportedTypeInHashSet>();

        public ISet<UnsupportedTypeInHashSet> UninstantiatedHashSetWithUnsupportedKey { get; set; }

#if NETCOREAPP
        public IReadOnlySet<string> InstantiatedIReadOnlySet { get; set; } = new HashSet<string>();
        public IReadOnlySet<string> InstantiatedIReadOnlySetWithSomeValues { get; set; } =
            new HashSet<string>(new[] { "existing1", "existing2" });
        public IReadOnlySet<string> NonInstantiatedIReadOnlySet { get; set; }
        public IDictionary<string, IReadOnlySet<string>> InstantiatedDictionaryWithReadOnlySetWithSomeValues { get; set; } =
            new Dictionary<string, IReadOnlySet<string>>
            {
                            {"item1", new HashSet<string>(new[] {"existing1", "existing2"})}
            };
#endif
        public IReadOnlyDictionary<string, int> InstantiatedReadOnlyDictionaryWithWithSomeValues { get; set; } =
            _existingDictionary;

        public IReadOnlyDictionary<string, int> NonInstantiatedReadOnlyDictionary { get; set; }

        public CustomICollectionWithoutAnAddMethod InstantiatedCustomICollectionWithoutAnAddMethod { get; set; } = new();
        public CustomICollectionWithoutAnAddMethod NonInstantiatedCustomICollectionWithoutAnAddMethod { get; set; }

        public IEnumerable<string> InstantiatedIEnumerable { get; set; } = new List<string>();
        public ICollection<string> InstantiatedICollection { get; set; } = new List<string>();
        public IReadOnlyCollection<string> InstantiatedIReadOnlyCollection { get; set; } = new List<string>();
    }

    public class NestedOptions
    {
        public int Integer { get; set; }
    }

    public class UnsupportedTypeInHashSet { }

    public class CustomICollectionWithoutAnAddMethod : ICollection<string>
    {
        private readonly List<string> _items = new();
        public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection<string>.Add(string item) => _items.Add(item);

        public void Clear() => _items.Clear();

        public bool Contains(string item) => _items.Contains(item);

        public void CopyTo(string[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);

        public bool Remove(string item) => _items.Remove(item);

        public int Count => _items.Count;
        public bool IsReadOnly => false;
    }
    #endregion
}
