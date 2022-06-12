// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Microsoft.Extensions.Configuration.Binder.Test
{
    public class ConfigurationBinderTests
    {
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

            public HashSet<string> InstantiatedHashSetWithSomeValues { get; set; } =
                new HashSet<string>(new[] {"existing1", "existing2"});

            public SortedSet<string> InstantiatedSortedSetWithSomeValues { get; set; } =
                new SortedSet<string>(new[] {"existing1", "existing2"});

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

        public class DerivedOptions : ComplexOptions
        {
            public override string Virtual
            {
                get
                {
                    return base.Virtual;
                }
                set
                {
                    base.Virtual = "Derived:" + value;
                }
            }
        }

        public class UnsupportedTypeInHashSet { }

        public interface ICustomCollectionDerivedFromIEnumerableT<out T> : IEnumerable<T> { }
        public interface ICustomCollectionDerivedFromICollectionT<T> : ICollection<T> { }

        public class MyClassWithCustomCollections
        {
            public ICustomCollectionDerivedFromIEnumerableT<string> CustomIEnumerableCollection { get; set; }
            public ICustomCollectionDerivedFromICollectionT<string> CustomCollection { get; set; }
        }

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

        public interface ICustomSet<T> : ISet<T>
        {
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

        public interface ICustomDictionary<T, T1> : IDictionary<T, T1>
        {
        }

        public class NullableOptions
        {
            public bool? MyNullableBool { get; set; }
            public int? MyNullableInt { get; set; }
            public DateTime? MyNullableDateTime { get; set; }
        }

        public class EnumOptions
        {
            public UriKind UriKind { get; set; }
        }

        public class GenericOptions<T>
        {
            public T Value { get; set; }
        }

        public class OptionsWithNesting
        {
            public NestedOptions Nested { get; set; }

            public class NestedOptions
            {
                public int Value { get; set; }
            }
        }

        public class ConfigurationInterfaceOptions
        {
            public IConfigurationSection Section { get; set; }
        }

        public class DerivedOptionsWithIConfigurationSection : DerivedOptions
        {
            public IConfigurationSection DerivedSection { get; set; }
        }

        public record struct RecordStructTypeOptions(string Color, int Length);

        // Here, the constructor has three parameters, but not all of those match
        // match to a property or field
        public class ClassWhereParametersDoNotMatchProperties
        {
            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersDoNotMatchProperties(string name, string address, int age)
            {
                Name = name;
                Address = address;
            }
        }

        // Here, the constructor has three parameters, and two of them match properties
        // and one of them match a field.
        public class ClassWhereParametersMatchPropertiesAndFields
        {
            private int Age;

            public string Name { get; }
            public string Address { get; }

            public ClassWhereParametersMatchPropertiesAndFields(string name, string address, int age)
            {
                Name = name;
                Address = address;
                Age = age;
            }

            public int GetAge() => Age;
        }

        public record RecordWhereParametersHaveDefaultValue(string Name, string Address, int Age = 42);

        public record ClassWhereParametersHaveDefaultValue
        {
            public string? Name { get; }
            public string Address { get; }
            public int Age { get; }

            public ClassWhereParametersHaveDefaultValue(string? name, string address, int age = 42)
            {
                Name = name;
                Address = address;
                Age = age;
            }
        }


        public record RecordTypeOptions(string Color, int Length);

        public record Line(string Color, int Length, int Thickness);

        public class ClassWithMatchingParametersAndProperties
        {
            private readonly string _color;

            public ClassWithMatchingParametersAndProperties(string Color, int Length)
            {
                _color = Color;
                this.Length = Length;
            }

            public int Length { get; set; }

            public string Color
            {
                get => _color;
                init => _color = "the color is " + value;
            }
        }

        public readonly record struct ReadonlyRecordStructTypeOptions(string Color, int Length);

        public class ContainerWithNestedImmutableObject
        {
            public string ContainerName { get; set; }
            public ImmutableLengthAndColorClass LengthAndColor { get; set; }
        }

        public struct MutableStructWithConstructor
        {
            public MutableStructWithConstructor(string randomParameter)
            {
                Color = randomParameter;
                Length = randomParameter.Length;
            }

            public string Color { get; set; }
            public int Length { get; set;  }
        }

        public class ImmutableLengthAndColorClass
        {
            public ImmutableLengthAndColorClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructor
        {
            public ImmutableClassWithOneParameterizedConstructor(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithInParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithInParameter(in string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithRefParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithRefParameter(string string1, ref int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithOneParameterizedConstructorButWithOutParameter
        {
            public ImmutableClassWithOneParameterizedConstructorButWithOutParameter(string string1, int int1,
                string string2, out decimal int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                int2 = 0;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class ImmutableClassWithMultipleParameterizedConstructors
        {
            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1)
            {
                String1 = string1;
                Int1 = int1;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1, int int1, string string2, int int2)
            {
                String1 = string1;
                Int1 = int1;
                String2 = string2;
                Int2 = int2;
            }

            public ImmutableClassWithMultipleParameterizedConstructors(string string1)
            {
                String1 = string1;
            }

            public string String1 { get; }
            public string String2 { get; }
            public int Int1 { get; }
            public int Int2 { get; }
        }

        public class SemiImmutableClass
        {
            public SemiImmutableClass(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; set; }
        }

        public class SemiImmutableClassWithInit
        {
            public SemiImmutableClassWithInit(string color, int length)
            {
                Color = color;
                Length = length;
            }

            public string Color { get; }
            public int Length { get; }
            public decimal Thickness { get; init; }
        }

        public struct ValueTypeOptions
        {
            public int MyInt32 { get; set; }
            public string MyString { get; set; }
        }

        public class ByteArrayOptions
        {
            public byte[] MyByteArray { get; set; }
        }

        [Fact]
        public void CanBindIConfigurationSection()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ConfigurationInterfaceOptions>();

            var childOptions = options.Section.Get<DerivedOptions>();

            Assert.True(childOptions.Boolean);
            Assert.Equal(-2, childOptions.Integer);
            Assert.Equal(11, childOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childOptions.Virtual);

            Assert.Equal("Section", options.Section.Key);
            Assert.Equal("Section", options.Section.Path);
            Assert.Null(options.Section.Value);
        }

        [Fact]
        public void CanBindWithKeyOverload()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new DerivedOptions();
            config.Bind("Section", options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void CanBindIConfigurationSectionWithDerivedOptionsSection()
        {
            var dic = new Dictionary<string, string>
            {
                {"Section:Integer", "-2"},
                {"Section:Boolean", "TRUe"},
                {"Section:Nested:Integer", "11"},
                {"Section:Virtual", "Sup"},
                {"Section:DerivedSection:Nested:Integer", "11"},
                {"Section:DerivedSection:Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ConfigurationInterfaceOptions>();

            var childOptions = options.Section.Get<DerivedOptionsWithIConfigurationSection>();

            var childDerivedOptions = childOptions.DerivedSection.Get<DerivedOptions>();

            Assert.True(childOptions.Boolean);
            Assert.Equal(-2, childOptions.Integer);
            Assert.Equal(11, childOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childOptions.Virtual);
            Assert.Equal(11, childDerivedOptions.Nested.Integer);
            Assert.Equal("Derived:Sup", childDerivedOptions.Virtual);

            Assert.Equal("Section", options.Section.Key);
            Assert.Equal("Section", options.Section.Path);
            Assert.Equal("DerivedSection", childOptions.DerivedSection.Key);
            Assert.Equal("Section:DerivedSection", childOptions.DerivedSection.Path);
            Assert.Null(options.Section.Value);
        }

        [Fact]
        public void CanBindConfigurationKeyNameAttributes()
        {
            var dic = new Dictionary<string, string>
            {
                {"Named_Property", "Yo"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>();

            Assert.Equal("Yo", options.NamedProperty);
        }

        [Fact]
        public void CanBindNonInstantiatedIEnumerableWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedIEnumerable:0", "Yo1"},
                {"NonInstantiatedIEnumerable:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.NonInstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedIEnumerable.ElementAt(1));
        }

        [Fact]
        public void CanBindNonInstantiatedISet()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedISet:0", "Yo1"},
                {"NonInstantiatedISet:1", "Yo2"},
                {"NonInstantiatedISet:2", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedISet.Count);
            Assert.Equal("Yo1", options.NonInstantiatedISet.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedISet.ElementAt(1));
        }

#if NETCOREAPP
        [Fact]
        public void CanBindInstantiatedIReadOnlySet()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIReadOnlySet:0", "Yo1"},
                {"InstantiatedIReadOnlySet:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIReadOnlySet.Count);
            Assert.Equal("Yo1", options.InstantiatedIReadOnlySet.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIReadOnlySet.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIReadOnlyWithSomeValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIReadOnlySetWithSomeValues:0", "Yo1"},
                {"InstantiatedIReadOnlySetWithSomeValues:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(4, options.InstantiatedIReadOnlySetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedIReadOnlySetWithSomeValues.ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedIReadOnlySetWithSomeValues.ElementAt(1));
            Assert.Equal("Yo1", options.InstantiatedIReadOnlySetWithSomeValues.ElementAt(2));
            Assert.Equal("Yo2", options.InstantiatedIReadOnlySetWithSomeValues.ElementAt(3));
        }

        [Fact]
        public void CanBindNonInstantiatedIReadOnlySet()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedIReadOnlySet:0", "Yo1"},
                {"NonInstantiatedIReadOnlySet:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedIReadOnlySet.Count);
            Assert.Equal("Yo1", options.NonInstantiatedIReadOnlySet.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedIReadOnlySet.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedDictionaryOfIReadOnlySetWithSomeExistingValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedDictionaryWithReadOnlySetWithSomeValues:foo:0", "foo-1"},
                {"InstantiatedDictionaryWithReadOnlySetWithSomeValues:foo:1", "foo-2"},
                {"InstantiatedDictionaryWithReadOnlySetWithSomeValues:bar:0", "bar-1"},
                {"InstantiatedDictionaryWithReadOnlySetWithSomeValues:bar:1", "bar-2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(3, options.InstantiatedDictionaryWithReadOnlySetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["item1"].ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["item1"].ElementAt(1));

            Assert.Equal("foo-1", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["foo"].ElementAt(0));
            Assert.Equal("foo-2", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["foo"].ElementAt(1));
            Assert.Equal("bar-1", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["bar"].ElementAt(0));
            Assert.Equal("bar-2", options.InstantiatedDictionaryWithReadOnlySetWithSomeValues["bar"].ElementAt(1));
        }
#endif

        public class Foo
        {
            public IReadOnlyDictionary<string, int> Items { get; set; } =
                new Dictionary<string, int> {{"existing-item1", 1}, {"existing-item2", 2}};

        }

        [Fact]
        public void CanBindInstantiatedReadOnlyDictionary2()
        {
            var dic = new Dictionary<string, string>
            {
                {"Items:item3", "3"},
                {"Items:item4", "4"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<Foo>()!;

            Assert.Equal(4, options.Items.Count);
            Assert.Equal(1, options.Items["existing-item1"]);
            Assert.Equal(2, options.Items["existing-item2"]);
            Assert.Equal(3, options.Items["item3"]);
            Assert.Equal(4, options.Items["item4"]);

            
        }

        [Fact]
        public void BindInstantiatedIReadOnlyDictionary_CreatesCopyOfOriginal()
        {
            var dic = new Dictionary<string, string>
            {
                {"Dictionary:existing-item1", "666"},
                {"Dictionary:item3", "3"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ConfigWithInstantiatedIReadOnlyDictionary>()!;

            Assert.Equal(3, options.Dictionary.Count);

            // does not overwrite original
            Assert.Equal(1, ConfigWithInstantiatedIReadOnlyDictionary._existingDictionary["existing-item1"]);

            Assert.Equal(666, options.Dictionary["existing-item1"]);
            Assert.Equal(2, options.Dictionary["existing-item2"]);
            Assert.Equal(3, options.Dictionary["item3"]);
        }

        [Fact]
        public void BindNonInstantiatedIReadOnlyDictionary()
        {
            var dic = new Dictionary<string, string>
            {
                {"Dictionary:item1", "1"},
                {"Dictionary:item2", "2"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ConfigWithNonInstantiatedReadOnlyDictionary>()!;

            Assert.Equal(2, options.Dictionary.Count);

            Assert.Equal(1, options.Dictionary["item1"]);
            Assert.Equal(2, options.Dictionary["item2"]);
        }

        [Fact]
        public void BindInstantiatedConcreteDictionary_OverwritesOriginal()
        {
            var dic = new Dictionary<string, string>
            {
                {"Dictionary:existing-item1", "666"},
                {"Dictionary:item3", "3"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ConfigWithInstantiatedConcreteDictionary>()!;

            Assert.Equal(3, options.Dictionary.Count);

            // overwrites original
            Assert.Equal(666, ConfigWithInstantiatedConcreteDictionary._existingDictionary["existing-item1"]);
            Assert.Equal(666, options.Dictionary["existing-item1"]);
            Assert.Equal(2, options.Dictionary["existing-item2"]);
            Assert.Equal(3, options.Dictionary["item3"]);
        }

        [Fact]
        public void CanBindInstantiatedReadOnlyDictionary()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedReadOnlyDictionaryWithWithSomeValues:item3", "3"},
                {"InstantiatedReadOnlyDictionaryWithWithSomeValues:item4", "4"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            var resultingDictionary = options.InstantiatedReadOnlyDictionaryWithWithSomeValues;
            Assert.Equal(4, resultingDictionary.Count);
            Assert.Equal(1, resultingDictionary["existing-item1"]);
            Assert.Equal(2, resultingDictionary["existing-item2"]);
            Assert.Equal(3, resultingDictionary["item3"]);
            Assert.Equal(4, resultingDictionary["item4"]);
        }

        [Fact]
        public void CanBindNonInstantiatedReadOnlyDictionary()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedReadOnlyDictionary:item3", "3"},
                {"NonInstantiatedReadOnlyDictionary:item4", "4"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedReadOnlyDictionary.Count);
            Assert.Equal(3, options.NonInstantiatedReadOnlyDictionary["item3"]);
            Assert.Equal(4, options.NonInstantiatedReadOnlyDictionary["item4"]);
        }
        

        [Fact]
        public void CanBindNonInstantiatedDictionaryOfISet()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedDictionaryWithISet:foo:0", "foo-1"},
                {"NonInstantiatedDictionaryWithISet:foo:1", "foo-2"},
                {"NonInstantiatedDictionaryWithISet:bar:0", "bar-1"},
                {"NonInstantiatedDictionaryWithISet:bar:1", "bar-2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedDictionaryWithISet.Count);
            Assert.Equal("foo-1", options.NonInstantiatedDictionaryWithISet["foo"].ElementAt(0));
            Assert.Equal("foo-2", options.NonInstantiatedDictionaryWithISet["foo"].ElementAt(1));
            Assert.Equal("bar-1", options.NonInstantiatedDictionaryWithISet["bar"].ElementAt(0));
            Assert.Equal("bar-2", options.NonInstantiatedDictionaryWithISet["bar"].ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedDictionaryOfISet()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedDictionaryWithHashSet:foo:0", "foo-1"},
                {"InstantiatedDictionaryWithHashSet:foo:1", "foo-2"},
                {"InstantiatedDictionaryWithHashSet:bar:0", "bar-1"},
                {"InstantiatedDictionaryWithHashSet:bar:1", "bar-2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedDictionaryWithHashSet.Count);
            Assert.Equal("foo-1", options.InstantiatedDictionaryWithHashSet["foo"].ElementAt(0));
            Assert.Equal("foo-2", options.InstantiatedDictionaryWithHashSet["foo"].ElementAt(1));
            Assert.Equal("bar-1", options.InstantiatedDictionaryWithHashSet["bar"].ElementAt(0));
            Assert.Equal("bar-2", options.InstantiatedDictionaryWithHashSet["bar"].ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedDictionaryOfISetWithSomeExistingValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedDictionaryWithHashSetWithSomeValues:foo:0", "foo-1"},
                {"InstantiatedDictionaryWithHashSetWithSomeValues:foo:1", "foo-2"},
                {"InstantiatedDictionaryWithHashSetWithSomeValues:bar:0", "bar-1"},
                {"InstantiatedDictionaryWithHashSetWithSomeValues:bar:1", "bar-2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(3, options.InstantiatedDictionaryWithHashSetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedDictionaryWithHashSetWithSomeValues["item1"].ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedDictionaryWithHashSetWithSomeValues["item1"].ElementAt(1));

            Assert.Equal("foo-1", options.InstantiatedDictionaryWithHashSetWithSomeValues["foo"].ElementAt(0));
            Assert.Equal("foo-2", options.InstantiatedDictionaryWithHashSetWithSomeValues["foo"].ElementAt(1));
            Assert.Equal("bar-1", options.InstantiatedDictionaryWithHashSetWithSomeValues["bar"].ElementAt(0));
            Assert.Equal("bar-2", options.InstantiatedDictionaryWithHashSetWithSomeValues["bar"].ElementAt(1));
        }

        [Fact]
        public void ThrowsForCustomIEnumerableCollection()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CustomIEnumerableCollection:0"] = "Yo!",
            });
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<MyClassWithCustomCollections>());
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ICustomCollectionDerivedFromIEnumerableT<string>)),
                exception.Message);
        }

        [Fact]
        public void ThrowsForCustomICollection()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CustomCollection:0"] = "Yo!",
            });
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<MyClassWithCustomCollections>());
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ICustomCollectionDerivedFromICollectionT<string>)),
                exception.Message);
        }

        [Fact]
        public void ThrowsForCustomDictionary()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CustomDictionary:0"] = "Yo!",
            });
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<MyClassWithCustomDictionary>());
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ICustomDictionary<string, int>)),
                exception.Message);
        }

        [Fact]
        public void ThrowsForCustomSet()
        {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CustomSet:0"] = "Yo!",
            });
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<MyClassWithCustomSet>());
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ICustomSet<string>)),
                exception.Message);
        }

        [Fact]
        public void CanBindInstantiatedISet()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedISet:0", "Yo1"},
                {"InstantiatedISet:1", "Yo2"},
                {"InstantiatedISet:2", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedISet.Count());
            Assert.Equal("Yo1", options.InstantiatedISet.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedISet.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedISetWithSomeValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedISetWithSomeValues:0", "Yo1"},
                {"InstantiatedISetWithSomeValues:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(4, options.InstantiatedISetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedISetWithSomeValues.ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedISetWithSomeValues.ElementAt(1));
            Assert.Equal("Yo1", options.InstantiatedISetWithSomeValues.ElementAt(2));
            Assert.Equal("Yo2", options.InstantiatedISetWithSomeValues.ElementAt(3));
        }

        [Fact]
        public void CanBindInstantiatedHashSetWithSomeValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedHashSetWithSomeValues:0", "Yo1"},
                {"InstantiatedHashSetWithSomeValues:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(4, options.InstantiatedHashSetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedHashSetWithSomeValues.ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedHashSetWithSomeValues.ElementAt(1));
            Assert.Equal("Yo1", options.InstantiatedHashSetWithSomeValues.ElementAt(2));
            Assert.Equal("Yo2", options.InstantiatedHashSetWithSomeValues.ElementAt(3));
        }

        [Fact]
        public void CanBindNonInstantiatedHashSet()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedHashSet:0", "Yo1"},
                {"NonInstantiatedHashSet:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedHashSet.Count);
            Assert.Equal("Yo1", options.NonInstantiatedHashSet.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedHashSet.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedSortedSetWithSomeValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedSortedSetWithSomeValues:0", "Yo1"},
                {"InstantiatedSortedSetWithSomeValues:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(4, options.InstantiatedSortedSetWithSomeValues.Count);
            Assert.Equal("existing1", options.InstantiatedSortedSetWithSomeValues.ElementAt(0));
            Assert.Equal("existing2", options.InstantiatedSortedSetWithSomeValues.ElementAt(1));
            Assert.Equal("Yo1", options.InstantiatedSortedSetWithSomeValues.ElementAt(2));
            Assert.Equal("Yo2", options.InstantiatedSortedSetWithSomeValues.ElementAt(3));
        }

        [Fact]
        public void CanBindNonInstantiatedSortedSetWithSomeValues()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedSortedSetWithSomeValues:0", "Yo1"},
                {"NonInstantiatedSortedSetWithSomeValues:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedSortedSetWithSomeValues.Count);
            Assert.Equal("Yo1", options.NonInstantiatedSortedSetWithSomeValues.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedSortedSetWithSomeValues.ElementAt(1));
        }

        [Fact]
        public void DoesNotBindInstantiatedISetWithUnsupportedKeys()
        {
            var dic = new Dictionary<string, string>
            {
                {"HashSetWithUnsupportedKey:0", "Yo1"},
                {"HashSetWithUnsupportedKey:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(0, options.HashSetWithUnsupportedKey.Count);
        }

        [Fact]
        public void DoesNotBindUninstantiatedISetWithUnsupportedKeys()
        {
            var dic = new Dictionary<string, string>
            {
                {"UninstantiatedHashSetWithUnsupportedKey:0", "Yo1"},
                {"UninstantiatedHashSetWithUnsupportedKey:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Null(options.UninstantiatedHashSetWithUnsupportedKey);
        }

        [Fact]
        public void CanBindInstantiatedIEnumerableWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIEnumerable:0", "Yo1"},
                {"InstantiatedIEnumerable:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.InstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIEnumerable.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedCustomICollectionWithoutAnAddMethodWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedCustomICollectionWithoutAnAddMethod:0", "Yo1"},
                {"InstantiatedCustomICollectionWithoutAnAddMethod:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedCustomICollectionWithoutAnAddMethod.Count);
            Assert.Equal("Yo1", options.InstantiatedCustomICollectionWithoutAnAddMethod.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedCustomICollectionWithoutAnAddMethod.ElementAt(1));
        }

        [Fact]
        public void CanBindNonInstantiatedCustomICollectionWithoutAnAddMethodWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"NonInstantiatedCustomICollectionWithoutAnAddMethod:0", "Yo1"},
                {"NonInstantiatedCustomICollectionWithoutAnAddMethod:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.NonInstantiatedCustomICollectionWithoutAnAddMethod.Count);
            Assert.Equal("Yo1", options.NonInstantiatedCustomICollectionWithoutAnAddMethod.ElementAt(0));
            Assert.Equal("Yo2", options.NonInstantiatedCustomICollectionWithoutAnAddMethod.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedICollectionWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedICollection:0", "Yo1"},
                {"InstantiatedICollection:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedICollection.Count());
            Assert.Equal("Yo1", options.InstantiatedICollection.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedICollection.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIReadOnlyCollectionWithItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIReadOnlyCollection:0", "Yo1"},
                {"InstantiatedIReadOnlyCollection:1", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIReadOnlyCollection.Count);
            Assert.Equal("Yo1", options.InstantiatedIReadOnlyCollection.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIReadOnlyCollection.ElementAt(1));
        }

        [Fact]
        public void CanBindInstantiatedIEnumerableWithNullItems()
        {
            var dic = new Dictionary<string, string>
            {
                {"InstantiatedIEnumerable:0", null},
                {"InstantiatedIEnumerable:1", "Yo1"},
                {"InstantiatedIEnumerable:2", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.InstantiatedIEnumerable.Count());
            Assert.Equal("Yo1", options.InstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("Yo2", options.InstantiatedIEnumerable.ElementAt(1));
        }

        [Fact]
        public void EmptyStringIsNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"empty", ""},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.Null(config.GetValue<bool?>("empty"));
            Assert.Null(config.GetValue<int?>("empty"));
        }

        [Fact]
        public void GetScalarNullable()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.True(config.GetValue<bool?>("Boolean"));
            Assert.Equal(-2, config.GetValue<int?>("Integer"));
            Assert.Equal(11, config.GetValue<int?>("Nested:Integer"));
        }

        [Fact]
        public void CanBindToObjectProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"Object", "whatever" }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Equal("whatever", options.Object);
        }

        [Fact]
        public void GetNullValue()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", null},
                {"Boolean", null},
                {"Nested:Integer", null},
                {"Object", null }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.False(config.GetValue<bool>("Boolean"));
            Assert.Equal(0, config.GetValue<int>("Integer"));
            Assert.Equal(0, config.GetValue<int>("Nested:Integer"));
            Assert.Null(config.GetValue<ComplexOptions>("Object"));
            Assert.False(config.GetSection("Boolean").Get<bool>());
            Assert.Equal(0, config.GetSection("Integer").Get<int>());
            Assert.Equal(0, config.GetSection("Nested:Integer").Get<int>());
            Assert.Null(config.GetSection("Object").Get<ComplexOptions>());
        }

        [Fact]
        public void ThrowsIfPropertyInConfigMissingInModel()
        {
            var dic = new Dictionary<string, string>
            {
                {"ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(ComplexOptions), "'ThisDoesNotExistInTheModel'");

            Assert.Equal(expectedMessage, ex.Message);
        }
        [Fact]
        public void ThrowsIfPropertyInConfigMissingInNestedModel()
        {
            var dic = new Dictionary<string, string>
            {
                {"Nested:ThisDoesNotExistInTheModel", "42"},
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();

            string expectedMessage = SR.Format(SR.Error_MissingConfig,
                nameof(BinderOptions.ErrorOnUnknownConfiguration), nameof(BinderOptions), typeof(NestedOptions), "'ThisDoesNotExistInTheModel'");

            var ex = Assert.Throws<InvalidOperationException>(
                () => config.Bind(instance, o => o.ErrorOnUnknownConfiguration = true));

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void GetDefaultsWhenDataDoesNotExist()
        {
            var dic = new Dictionary<string, string>
            {
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            Assert.False(config.GetValue<bool>("Boolean"));
            Assert.Equal(0, config.GetValue<int>("Integer"));
            Assert.Equal(0, config.GetValue<int>("Nested:Integer"));
            Assert.Null(config.GetValue<ComplexOptions>("Object"));
            Assert.True(config.GetValue("Boolean", true));
            Assert.Equal(3, config.GetValue("Integer", 3));
            Assert.Equal(1, config.GetValue("Nested:Integer", 1));
            var foo = new ComplexOptions();
            Assert.Same(config.GetValue("Object", foo), foo);
        }

        [Fact]
        public void GetUri()
        {
            var dic = new Dictionary<string, string>
            {
                {"AnUri", "http://www.bing.com"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var uri = config.GetValue<Uri>("AnUri");

            Assert.Equal("http://www.bing.com", uri.OriginalString);
        }

        [Theory]
        [InlineData("2147483647", typeof(int))]
        [InlineData("4294967295", typeof(uint))]
        [InlineData("32767", typeof(short))]
        [InlineData("65535", typeof(ushort))]
        [InlineData("-9223372036854775808", typeof(long))]
        [InlineData("18446744073709551615", typeof(ulong))]
        [InlineData("trUE", typeof(bool))]
        [InlineData("255", typeof(byte))]
        [InlineData("127", typeof(sbyte))]
        [InlineData("\uffff", typeof(char))]
        [InlineData("79228162514264337593543950335", typeof(decimal))]
        [InlineData("1.79769e+308", typeof(double))]
        [InlineData("3.40282347E+38", typeof(float))]
        [InlineData("2015-12-24T07:34:42-5:00", typeof(DateTime))]
        [InlineData("12/24/2015 13:44:55 +4", typeof(DateTimeOffset))]
        [InlineData("99.22:22:22.1234567", typeof(TimeSpan))]
        [InlineData("http://www.bing.com", typeof(Uri))]
        // enum test
        [InlineData("Constructor", typeof(AttributeTargets))]
        [InlineData("CA761232-ED42-11CE-BACD-00AA0057B223", typeof(Guid))]
        public void CanReadAllSupportedTypes(string value, Type type)
        {
            // arrange
            var dic = new Dictionary<string, string>
            {
                {"Value", value}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);
            var expectedValue = TypeDescriptor.GetConverter(type).ConvertFromInvariantString(value);

            // act
            config.Bind(options);
            var optionsValue = options.GetType().GetProperty("Value").GetValue(options);
            var getValueValue = config.GetValue(type, "Value");
            var getValue = config.GetSection("Value").Get(type);

            // assert
            Assert.Equal(expectedValue, optionsValue);
            Assert.Equal(expectedValue, getValue);
            Assert.Equal(expectedValue, getValueValue);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(char))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(double))]
        [InlineData(typeof(float))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(AttributeTargets))]
        [InlineData(typeof(Guid))]
        public void ConsistentExceptionOnFailedBinding(Type type)
        {
            // arrange
            const string IncorrectValue = "Invalid data";
            const string ConfigKey = "Value";
            var dic = new Dictionary<string, string>
            {
                {ConfigKey, IncorrectValue}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var optionsType = typeof(GenericOptions<>).MakeGenericType(type);
            var options = Activator.CreateInstance(optionsType);

            // act
            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            var getValueException = Assert.Throws<InvalidOperationException>(
                () => config.GetValue(type, "Value"));

            var getException = Assert.Throws<InvalidOperationException>(
                () => config.GetSection("Value").Get(type));

            // assert
            Assert.NotNull(exception.InnerException);
            Assert.NotNull(getException.InnerException);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                exception.Message);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                getException.Message);
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, ConfigKey, type),
                getValueException.Message);
        }

        [Fact]
        public void ExceptionOnFailedBindingIncludesPath()
        {
            const string IncorrectValue = "Invalid data";
            const string ConfigKey = "Nested:Value";

            var dic = new Dictionary<string, string>
            {
                {ConfigKey, IncorrectValue}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new OptionsWithNesting();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));

            Assert.Equal(SR.Format(SR.Error_FailedBinding, ConfigKey, typeof(int)),
                exception.Message);
        }

        [Fact]
        public void BinderIgnoresIndexerProperties()
        {
            var configurationBuilder = new ConfigurationBuilder();
            var config = configurationBuilder.Build();
            config.Bind(new List<string>());
        }

        [Fact]
        public void BindCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();
            config.Bind(instance);

            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
        }

        [Fact]
        public void GetCanReadComplexProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Fact]
        public void BindCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new DerivedOptions();
            config.Bind(instance);

            Assert.True(instance.Boolean);
            Assert.Equal(-2, instance.Integer);
            Assert.Equal(11, instance.Nested.Integer);
            Assert.Equal("Derived:Sup", instance.Virtual);
        }

        [Fact]
        public void GetCanReadInheritedProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Integer", "-2"},
                {"Boolean", "TRUe"},
                {"Nested:Integer", "11"},
                {"Virtual", "Sup"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new DerivedOptions();
            config.Bind(options);

            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
            Assert.Equal("Derived:Sup", options.Virtual);
        }

        [Fact]
        public void GetCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();
            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Equal("stuff", ComplexOptions.StaticProperty);
        }

        [Fact]
        public void BindCanReadStaticProperty()
        {
            var dic = new Dictionary<string, string>
            {
                {"StaticProperty", "other stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var instance = new ComplexOptions();
            config.Bind(instance);

            Assert.Equal("other stuff", ComplexOptions.StaticProperty);
        }

        [Fact]
        public void CanGetComplexOptionsWhichHasAlsoHasValue()
        {
            var dic = new Dictionary<string, string>
            {
                {"obj", "whut" },
                {"obj:Integer", "-2"},
                {"obj:Boolean", "TRUe"},
                {"obj:Nested:Integer", "11"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.GetSection("obj").Get<ComplexOptions>();
            Assert.NotNull(options);
            Assert.True(options.Boolean);
            Assert.Equal(-2, options.Integer);
            Assert.Equal(11, options.Nested.Integer);
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void GetIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>();
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void GetCanSetNonPublicWhenSet(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("InternalReadOnly")]
        [InlineData("PrivateReadOnly")]
        [InlineData("ProtectedReadOnly")]
        public void NonPublicModeGetStillIgnoresReadonly(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>(o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("ReadOnly")]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void BindIgnoresTests(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options);

            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("PrivateSetter")]
        [InlineData("ProtectedSetter")]
        [InlineData("InternalSetter")]
        [InlineData("InternalProperty")]
        [InlineData("PrivateProperty")]
        [InlineData("ProtectedProperty")]
        [InlineData("ProtectedPrivateSet")]
        public void BindCanSetNonPublicWhenSet(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Equal("stuff", options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Theory]
        [InlineData("InternalReadOnly")]
        [InlineData("PrivateReadOnly")]
        [InlineData("ProtectedReadOnly")]
        public void NonPublicModeBindStillIgnoresReadonly(string property)
        {
            var dic = new Dictionary<string, string>
            {
                {property, "stuff"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = new ComplexOptions();
            config.Bind(options, o => o.BindNonPublicProperties = true);
            Assert.Null(options.GetType().GetTypeInfo().GetDeclaredProperty(property).GetValue(options));
        }

        [Fact]
        public void ExceptionWhenTryingToBindToInterface()
        {
            var input = new Dictionary<string, string>
            {
                {"ISomeInterfaceProperty:Subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ISomeInterface)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindClassWithoutParameterlessConstructor()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWithoutPublicConstructorProperty:Subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_MissingPublicInstanceConstructor, typeof(ClassWithoutPublicConstructor)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindClassWherePropertiesDoMatchConstructorParameters()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Name", "John"},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Address", "123, Abc St."},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersDoNotMatchProperties), "age"),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToConstructorWithMissingConfig()
        {
            var input = new Dictionary<string, string>
            {
                {"LineProperty:Color", "Red"},
                {"LineProperty:Length", "22"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ParameterHasNoMatchingConfig, typeof(Line), nameof(Line.Thickness)),
                exception.Message);
        }

        [Fact]
        public void ExceptionWhenTryingToBindConfigToClassWhereNoMatchingParameterIsFoundInConstructor()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Name", "John"},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Address", "123, Abc St."},
                {"ClassWhereParametersDoNotMatchPropertiesProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersDoNotMatchProperties), "age"),
                exception.Message);
        }

        [Fact]
        public void BindsToClassConstructorParametersWithDefaultValues()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersHaveDefaultValueProperty:Name", "John"},
                {"ClassWhereParametersHaveDefaultValueProperty:Address", "123, Abc St."}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            TestOptions testOptions = new TestOptions();

            config.Bind(testOptions);
            Assert.Equal("John", testOptions.ClassWhereParametersHaveDefaultValueProperty.Name);
            Assert.Equal("123, Abc St.", testOptions.ClassWhereParametersHaveDefaultValueProperty.Address);
            Assert.Equal(42, testOptions.ClassWhereParametersHaveDefaultValueProperty.Age);
        }

        [Fact]
        public void FieldsNotSupported_ExceptionBindingToConstructorWithParameterMatchingAField()
        {
            var input = new Dictionary<string, string>
            {
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Name", "John"},
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Address", "123, Abc St."},
                {"ClassWhereParametersMatchPropertiesAndFieldsProperty:Age", "42"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));

            Assert.Equal(
                SR.Format(SR.Error_ConstructorParametersDoNotMatchProperties, typeof(ClassWhereParametersMatchPropertiesAndFields), "age"),
                exception.Message);
        }

        [Fact]
        public void BindsToRecordPrimaryConstructorParametersWithDefaultValues()
        {
            var input = new Dictionary<string, string>
            {
                {"RecordWhereParametersHaveDefaultValueProperty:Name", "John"},
                {"RecordWhereParametersHaveDefaultValueProperty:Address", "123, Abc St."}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            TestOptions testOptions = new TestOptions();

            config.Bind(testOptions);
            Assert.Equal("John", testOptions.RecordWhereParametersHaveDefaultValueProperty.Name);
            Assert.Equal("123, Abc St.", testOptions.RecordWhereParametersHaveDefaultValueProperty.Address);
            Assert.Equal(42, testOptions.RecordWhereParametersHaveDefaultValueProperty.Age);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToTypeThrowsWhenActivated()
        {
            var input = new Dictionary<string, string>
            {
                {"ThrowsWhenActivatedProperty:subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                SR.Format(SR.Error_FailedToActivate, typeof(ThrowsWhenActivated)),
                exception.Message);
        }

        [Fact]
        public void ExceptionIncludesKeyOfFailedBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NestedOptionsProperty:NestedOptions2Property:ISomeInterfaceProperty:subkey", "x"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(new TestOptions()));
            Assert.Equal(
                SR.Format(SR.Error_CannotActivateAbstractOrInterface, typeof(ISomeInterface)),
                exception.Message);
        }

        [Fact]
        public void CanBindValueTypeOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"MyInt32", "42"},
                {"MyString", "hello world"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ValueTypeOptions>();
            Assert.Equal(42, options.MyInt32);
            Assert.Equal("hello world", options.MyString);
        }

        [Fact]
        public void CanBindImmutableClass()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ImmutableLengthAndColorClass>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindMutableClassWitNestedImmutableObject()
        {
            var dic = new Dictionary<string, string>
            {
                {"ContainerName", "Container123"},
                {"LengthAndColor:Length", "42"},
                {"LengthAndColor:Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ContainerWithNestedImmutableObject>();
            Assert.Equal("Container123", options.ContainerName);
            Assert.Equal(42, options.LengthAndColor.Length);
            Assert.Equal("Green", options.LengthAndColor.Color);
        }

        // If the immutable type has multiple public parameterized constructors, then throw
        // an exception.
        [Fact]
        public void CanBindImmutableClass_ThrowsOnMultipleParameterizedConstructors()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_MultipleParameterizedConstructors, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithMultipleParameterizedConstructors");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithMultipleParameterizedConstructors>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructor, then throw
        // that constructor has an 'in' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithAnInParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithInParameter", "string1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithInParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // that constructor has a 'ref' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithARefParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithRefParameter", "int1");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithRefParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        // If the immutable type has a parameterized constructors, then throw
        // if the constructor has an 'out' parameter
        [Fact]
        public void CanBindImmutableClass_ThrowsOnParameterizedConstructorWithAnOutParameter()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            string expectedMessage = SR.Format(SR.Error_CannotBindToConstructorParameter, "Microsoft.Extensions.Configuration.Binder.Test.ConfigurationBinderTests+ImmutableClassWithOneParameterizedConstructorButWithOutParameter", "int2");

            var ex = Assert.Throws<InvalidOperationException>(() => config.Get<ImmutableClassWithOneParameterizedConstructorButWithOutParameter>());

            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public void CanBindMutableStruct_UnmatchedConstructorsAreIgnored()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<MutableStructWithConstructor>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        // If the immutable type has a public parameterized constructor,
        // then pick it.
        [Fact]
        public void CanBindImmutableClass_PicksParameterizedConstructorIfNoParameterlessConstructorExists()
        {
            var dic = new Dictionary<string, string>
            {
                {"String1", "s1"},
                {"Int1", "1"},
                {"String2", "s2"},
                {"Int2", "2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ImmutableClassWithOneParameterizedConstructor>();
            Assert.Equal("s1", options.String1);
            Assert.Equal("s2", options.String2);
            Assert.Equal(1, options.Int1);
            Assert.Equal(2, options.Int2);
        }

        [Fact]
        public void CanBindSemiImmutableClass()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
                {"Thickness", "1.23"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<SemiImmutableClass>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
            Assert.Equal(1.23m, options.Thickness);
        }

        [Fact]
        public void CanBindSemiImmutableClass_WithInitProperties()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
                {"Thickness", "1.23"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<SemiImmutableClassWithInit>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
            Assert.Equal(1.23m, options.Thickness);
        }

        [Fact]
        public void CanBindRecordOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<RecordTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindRecordStructOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<RecordStructTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindOnParametersAndProperties_PropertiesAreSetAfterTheConstructor()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ClassWithMatchingParametersAndProperties>();
            Assert.Equal(42, options.Length);
            Assert.Equal("the color is Green", options.Color);
        }

        [Fact]
        public void CanBindReadonlyRecordStructOptions()
        {
            var dic = new Dictionary<string, string>
            {
                {"Length", "42"},
                {"Color", "Green"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ReadonlyRecordStructTypeOptions>();
            Assert.Equal(42, options.Length);
            Assert.Equal("Green", options.Color);
        }

        [Fact]
        public void CanBindByteArray()
        {
            var bytes = new byte[] { 1, 2, 3, 4 };
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", Convert.ToBase64String(bytes) }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ByteArrayOptions>();
            Assert.Equal(bytes, options.MyByteArray);
        }

        [Fact]
        public void CanBindByteArrayWhenValueIsNull()
        {
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", null }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var options = config.Get<ByteArrayOptions>();
            Assert.Null(options.MyByteArray);
        }

        [Fact]
        public void ExceptionWhenTryingToBindToByteArray()
        {
            var dic = new Dictionary<string, string>
            {
                { "MyByteArray", "(not a valid base64 string)" }
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);
            var config = configurationBuilder.Build();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Get<ByteArrayOptions>());
            Assert.Equal(
                SR.Format(SR.Error_FailedBinding, "MyByteArray", typeof(byte[])),
                exception.Message);
        }

        [Fact]
        public void DoesNotReadPropertiesUnnecessarily()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { nameof(ClassWithReadOnlyPropertyThatThrows.Safe), "value" },
                { nameof(ClassWithReadOnlyPropertyThatThrows.StringThrows), "value" },
                { $"{nameof(ClassWithReadOnlyPropertyThatThrows.EnumerableThrows)}:0", "0" },
            });
            IConfiguration config = configurationBuilder.Build();

            ClassWithReadOnlyPropertyThatThrows bound = config.Get<ClassWithReadOnlyPropertyThatThrows>();
            Assert.Equal("value", bound.Safe);
        }

        /// <summary>
        /// Binding to mutable structs is important to support properties
        /// like JsonConsoleFormatterOptions.JsonWriterOptions.
        /// </summary>
        [Fact]
        public void CanBindNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ReadWriteNestedStruct:String", "s" },
                { "ReadWriteNestedStruct:DeeplyNested:Int32", "100" },
                { "ReadWriteNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();

            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.Equal("s", bound.ReadWriteNestedStruct.String);
            Assert.Equal(100, bound.ReadWriteNestedStruct.DeeplyNested.Int32);
            Assert.True(bound.ReadWriteNestedStruct.DeeplyNested.Boolean);
        }

        [Fact]
        public void IgnoresReadOnlyNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ReadOnlyNestedStruct:String", "s" },
                { "ReadOnlyNestedStruct:DeeplyNested:Int32", "100" },
                { "ReadOnlyNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();

            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.Null(bound.ReadOnlyNestedStruct.String);
            Assert.Equal(0, bound.ReadWriteNestedStruct.DeeplyNested.Int32);
            Assert.False(bound.ReadWriteNestedStruct.DeeplyNested.Boolean);
        }

        [Fact]
        public void CanBindNullableNestedStructProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "NullableNestedStruct:String", "s" },
                { "NullableNestedStruct:DeeplyNested:Int32", "100" },
                { "NullableNestedStruct:DeeplyNested:Boolean", "true" },
            });
            IConfiguration config = configurationBuilder.Build();

            StructWithNestedStructs bound = config.Get<StructWithNestedStructs>();
            Assert.NotNull(bound.NullableNestedStruct);
            Assert.Equal("s", bound.NullableNestedStruct.Value.String);
            Assert.Equal(100, bound.NullableNestedStruct.Value.DeeplyNested.Int32);
            Assert.True(bound.NullableNestedStruct.Value.DeeplyNested.Boolean);
        }

        [Fact]
        public void CanBindVirtualProperties()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { $"{nameof(BaseClassWithVirtualProperty.Test)}:0", "1" },
                { $"{nameof(BaseClassWithVirtualProperty.TestGetSetOverridden)}", "2" },
                { $"{nameof(BaseClassWithVirtualProperty.TestGetOverridden)}", "3" },
                { $"{nameof(BaseClassWithVirtualProperty.TestSetOverridden)}", "4" },
                { $"{nameof(BaseClassWithVirtualProperty.TestNoOverridden)}", "5" },
                { $"{nameof(BaseClassWithVirtualProperty.TestVirtualSet)}", "6" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();
            config.Bind(test);

            Assert.Equal("1", Assert.Single(test.Test));
            Assert.Equal("2", test.TestGetSetOverridden);
            Assert.Equal("3", test.TestGetOverridden);
            Assert.Equal("4", test.TestSetOverridden);
            Assert.Equal("5", test.TestNoOverridden);
            Assert.Null(test.ExposeTestVirtualSet());
        }

        [Fact]
        public void CanBindPrivatePropertiesFromBaseClass()
        {
            ConfigurationBuilder configurationBuilder = new();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "PrivateProperty", "a" }
            });
            IConfiguration config = configurationBuilder.Build();

            var test = new ClassOverridingVirtualProperty();
            config.Bind(test, b => b.BindNonPublicProperties = true);
            Assert.Equal("a", test.ExposePrivatePropertyValue());
        }

        private interface ISomeInterface
        {
        }

        private class ClassWithoutPublicConstructor
        {
            private ClassWithoutPublicConstructor()
            {
            }
        }

        private class ThrowsWhenActivated
        {
            public ThrowsWhenActivated()
            {
                throw new Exception();
            }
        }

        private class NestedOptions1
        {
            public NestedOptions2 NestedOptions2Property { get; set; }
        }

        private class NestedOptions2
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }
        }

        private class TestOptions
        {
            public ISomeInterface ISomeInterfaceProperty { get; set; }

            public ClassWithoutPublicConstructor ClassWithoutPublicConstructorProperty { get; set; }
            public ClassWhereParametersDoNotMatchProperties ClassWhereParametersDoNotMatchPropertiesProperty { get; set; }
            public Line LineProperty { get; set; }
            public ClassWhereParametersHaveDefaultValue ClassWhereParametersHaveDefaultValueProperty { get; set; }
            public ClassWhereParametersMatchPropertiesAndFields ClassWhereParametersMatchPropertiesAndFieldsProperty { get; set; }
            public RecordWhereParametersHaveDefaultValue RecordWhereParametersHaveDefaultValueProperty { get; set; }

            public int IntProperty { get; set; }

            public ThrowsWhenActivated ThrowsWhenActivatedProperty { get; set; }

            public NestedOptions1 NestedOptionsProperty { get; set; }
        }

        private class ClassWithReadOnlyPropertyThatThrows
        {
            public string StringThrows => throw new InvalidOperationException(nameof(StringThrows));

            public IEnumerable<int> EnumerableThrows => throw new InvalidOperationException(nameof(EnumerableThrows));

            public string Safe { get; set; }
        }

        private struct StructWithNestedStructs
        {
            public Nested ReadWriteNestedStruct { get; set; }

            public Nested ReadOnlyNestedStruct { get; }

            public Nested? NullableNestedStruct { get; set; }

            public struct Nested
            {
                public string String { get; set; }
                public DeeplyNested DeeplyNested { get; set; }
            }

            public struct DeeplyNested
            {
                public int Int32 { get; set; }
                public bool Boolean { get; set; }
            }
        }

        public class BaseClassWithVirtualProperty
        {
            private string? PrivateProperty { get; set; }

            public virtual string[] Test { get; set; } = System.Array.Empty<string>();

            public virtual string? TestGetSetOverridden { get; set; }
            public virtual string? TestGetOverridden { get; set; }
            public virtual string? TestSetOverridden { get; set; }

            private string? _testVirtualSet;
            public virtual string? TestVirtualSet
            {
                set => _testVirtualSet = value;
            }

            public virtual string? TestNoOverridden { get; set; }

            public string? ExposePrivatePropertyValue() => PrivateProperty;
        }

        public class ClassOverridingVirtualProperty : BaseClassWithVirtualProperty
        {
            public override string[] Test { get => base.Test; set => base.Test = value; }

            public override string? TestGetSetOverridden { get; set; }
            public override string? TestGetOverridden => base.TestGetOverridden;
            public override string? TestSetOverridden
            {
                set => base.TestSetOverridden = value;
            }

            private string? _testVirtualSet;
            public override string? TestVirtualSet
            {
                set => _testVirtualSet = value;
            }

            public string? ExposeTestVirtualSet() => _testVirtualSet;
        }
    }
}
