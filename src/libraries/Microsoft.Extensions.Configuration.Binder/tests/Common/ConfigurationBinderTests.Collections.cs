// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if BUILDING_SOURCE_GENERATOR_TESTS
using Microsoft.Extensions.Configuration;
#endif
using Xunit;

namespace Microsoft.Extensions
#if BUILDING_SOURCE_GENERATOR_TESTS
    .SourceGeneration
#endif
    .Configuration.Binder.Tests
{
    public sealed partial class ConfigurationBinderCollectionTests : ConfigurationBinderTestsBase
    {
        [Fact]
        public void GetList()
        {
            var input = new Dictionary<string, string>
            {
                {"StringList:0", "val0"},
                {"StringList:1", "val1"},
                {"StringList:2", "val2"},
                {"StringList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var list = new List<string>();
            config.GetSection("StringList").Bind(list);

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void GetListNullValues()
        {
            var input = new Dictionary<string, string>
            {
                {"StringList:0", null},
                {"StringList:1", null},
                {"StringList:2", null},
                {"StringList:x", null}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var list = new List<string>();
            config.GetSection("StringList").Bind(list);

            Assert.Empty(list);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void GetListInvalidValues()
        {
            var input = new Dictionary<string, string>
            {
                {"InvalidList:0", "true"},
                {"InvalidList:1", "invalid"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var list = new List<bool>();
            config.GetSection("InvalidList").Bind(list);

            Assert.Single(list);
            Assert.True(list[0]);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void GetDictionaryInvalidValues()
        {
            var input = new Dictionary<string, string>
            {
                {"InvalidDictionary:0", "true"},
                {"InvalidDictionary:1", "invalid"},
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(input).Build();
            var dict = new Dictionary<string, bool>();

            config.Bind("InvalidDictionary", dict);

            Assert.Single(dict);
            Assert.True(dict["0"]);
        }

        [Fact]
        public void BindList()
        {
            var input = new Dictionary<string, string>
            {
                {"StringList:0", "val0"},
                {"StringList:1", "val1"},
                {"StringList:2", "val2"},
                {"StringList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var list = new List<string>();
            config.GetSection("StringList").Bind(list);

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void GetObjectList()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectList:0:Integer", "30"},
                {"ObjectList:1:Integer", "31"},
                {"ObjectList:2:Integer", "32"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new List<NestedOptions>();
            config.GetSection("ObjectList").Bind(options);

            Assert.Equal(3, options.Count);

            Assert.Equal(30, options[0].Integer);
            Assert.Equal(31, options[1].Integer);
            Assert.Equal(32, options[2].Integer);
        }

        [Fact]
        public void GetStringDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"StringDictionary:abc", "val_1"},
                {"StringDictionary:def", "val_2"},
                {"StringDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new Dictionary<string, string>();
            config.GetSection("StringDictionary").Bind(options);

            Assert.Equal(3, options.Count);

            Assert.Equal("val_1", options["abc"]);
            Assert.Equal("val_2", options["def"]);
            Assert.Equal("val_3", options["ghi"]);
        }

        [Fact]
        public void GetEnumDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"EnumDictionary:abc", "val_1"},
                {"EnumDictionary:def", "val_2"},
                {"EnumDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new Dictionary<KeyEnum, string>();
            config.GetSection("EnumDictionary").Bind(options);

            Assert.Equal(3, options.Count);

            Assert.Equal("val_1", options[KeyEnum.abc]);
            Assert.Equal("val_2", options[KeyEnum.def]);
            Assert.Equal("val_3", options[KeyEnum.ghi]);
        }

        [Fact]
        public void GetUintEnumDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"EnumDictionary:abc", "val_1"},
                {"EnumDictionary:def", "val_2"},
                {"EnumDictionary:ghi", "val_3"}
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new Dictionary<KeyUintEnum, string>();
            config.GetSection("EnumDictionary").Bind(options);
            Assert.Equal(3, options.Count);
            Assert.Equal("val_1", options[KeyUintEnum.abc]);
            Assert.Equal("val_2", options[KeyUintEnum.def]);
            Assert.Equal("val_3", options[KeyUintEnum.ghi]);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetSByteDictionary()
        {
            GetIntDictionaryT<sbyte>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetByteDictionary()
        {
            GetIntDictionaryT<byte>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetShortDictionary()
        {
            GetIntDictionaryT<short>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetUShortDictionary()
        {
            GetIntDictionaryT<ushort>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetIntDictionary()
        {
            GetIntDictionaryT<int>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetUIntDictionary()
        {
            GetIntDictionaryT<uint>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetLongDictionary()
        {
            GetIntDictionaryT<long>(0, 1, 2);
        }

        // Reflection fallback: generic type info not supported with source gen.
        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))]
        public void GetULongDictionary()
        {
            GetIntDictionaryT<ulong>(0, 1, 2);
        }

        private void GetIntDictionaryT<T>(T k1, T k2, T k3)
        {
            var input = new Dictionary<string, string>
            {
                {"IntegerKeyDictionary:0", "val_0"},
                {"IntegerKeyDictionary:1", "val_1"},
                {"IntegerKeyDictionary:2", "val_2"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new Dictionary<T, string>();
#pragma warning disable SYSLIB1104
            config.GetSection("IntegerKeyDictionary").Bind(options);
#pragma warning restore SYSLIB1104

            Assert.Equal(3, options.Count);

            Assert.Equal("val_0", options[k1]);
            Assert.Equal("val_1", options[k2]);
            Assert.Equal("val_2", options[k3]);
        }

        [Fact]
        public void BindStringList()
        {
            var input = new Dictionary<string, string>
            {
                {"StringList:0", "val0"},
                {"StringList:1", "val1"},
                {"StringList:2", "val2"},
                {"StringList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new OptionsWithLists();
            config.Bind(options);

            var list = options.StringList;

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void BindIntList()
        {
            var input = new Dictionary<string, string>
            {
                {"IntList:0", "42"},
                {"IntList:1", "43"},
                {"IntList:2", "44"},
                {"IntList:x", "45"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            var list = options.IntList;

            Assert.Equal(4, list.Count);

            Assert.Equal(42, list[0]);
            Assert.Equal(43, list[1]);
            Assert.Equal(44, list[2]);
            Assert.Equal(45, list[3]);
        }

        [Fact]
        public void AlreadyInitializedListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedList:0", "val0"},
                {"AlreadyInitializedList:1", "val1"},
                {"AlreadyInitializedList:2", "val2"},
                {"AlreadyInitializedList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            var list = options.AlreadyInitializedList;

            Assert.Equal(5, list.Count);

            Assert.Equal("This was here before", list[0]);
            Assert.Equal("val0", list[1]);
            Assert.Equal("val1", list[2]);
            Assert.Equal("val2", list[3]);
            Assert.Equal("valx", list[4]);
        }

        [Fact]
        public void AlreadyInitializedListInterfaceBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedListInterface:0", "val0"},
                {"AlreadyInitializedListInterface:1", "val1"},
                {"AlreadyInitializedListInterface:2", "val2"},
                {"AlreadyInitializedListInterface:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            var list = options.AlreadyInitializedListInterface;

            Assert.Equal(5, list.Count);

            Assert.Equal("This was here too", list[0]);
            Assert.Equal("val0", list[1]);
            Assert.Equal("val1", list[2]);
            Assert.Equal("val2", list[3]);
            Assert.Equal("valx", list[4]);

            // Ensure expandability of the returned list
            options.AlreadyInitializedListInterface.Add("ExtraItem");
            Assert.Equal(6, options.AlreadyInitializedListInterface.Count);
            Assert.Equal("ExtraItem", options.AlreadyInitializedListInterface[5]);
        }

        [Fact]
        public void CustomListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"CustomList:0", "val0"},
                {"CustomList:1", "val1"},
                {"CustomList:2", "val2"},
                {"CustomList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            var list = options.CustomList;

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);
        }

        [Fact]
        public void ObjectListBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectList:0:Integer", "30"},
                {"ObjectList:1:Integer", "31"},
                {"ObjectList:2:Integer", "32"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            Assert.Equal(3, options.ObjectList.Count);

            Assert.Equal(30, options.ObjectList[0].Integer);
            Assert.Equal(31, options.ObjectList[1].Integer);
            Assert.Equal(32, options.ObjectList[2].Integer);
        }

        [Fact]
        public void NestedListsBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NestedLists:0:0", "val00"},
                {"NestedLists:0:1", "val01"},
                {"NestedLists:1:0", "val10"},
                {"NestedLists:1:1", "val11"},
                {"NestedLists:1:2", "val12"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            Assert.Equal(2, options.NestedLists.Count);
            Assert.Equal(2, options.NestedLists[0].Count);
            Assert.Equal(3, options.NestedLists[1].Count);

            Assert.Equal("val00", options.NestedLists[0][0]);
            Assert.Equal("val01", options.NestedLists[0][1]);
            Assert.Equal("val10", options.NestedLists[1][0]);
            Assert.Equal("val11", options.NestedLists[1][1]);
            Assert.Equal("val12", options.NestedLists[1][2]);
        }

        [Fact]
        public void StringDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"StringDictionary:abc", "val_1"},
                {"StringDictionary:def", "val_2"},
                {"StringDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.Equal(3, options.StringDictionary.Count);

            Assert.Equal("val_1", options.StringDictionary["abc"]);
            Assert.Equal("val_2", options.StringDictionary["def"]);
            Assert.Equal("val_3", options.StringDictionary["ghi"]);
        }

        [Fact]
        public void ShouldPreserveExistingKeysInDictionary()
        {
            var input = new Dictionary<string, string> { { "ascii:b", "98" } };
            var config = new ConfigurationBuilder().AddInMemoryCollection(input).Build();
            var origin = new Dictionary<string, int> { ["a"] = 97 };

            config.Bind("ascii", origin);

            Assert.Equal(2, origin.Count);
            Assert.Equal(97, origin["a"]);
            Assert.Equal(98, origin["b"]);
        }

        [Fact]
        public void ShouldPreserveExistingKeysInNestedDictionary()
        {
            var input = new Dictionary<string, string> { ["ascii:b"] = "98" };
            var config = new ConfigurationBuilder().AddInMemoryCollection(input).Build();
            var origin = new Dictionary<string, IDictionary<string, int>>
            {
                ["ascii"] = new Dictionary<string, int> { ["a"] = 97 }
            };

            config.Bind(origin);

            Assert.Equal(2, origin["ascii"].Count);
            Assert.Equal(97, origin["ascii"]["a"]);
            Assert.Equal(98, origin["ascii"]["b"]);
        }

        [Fact]
        public void ShouldPreserveExistingKeysInDictionaryWithEnumAsKeyType()
        {
            var input = new Dictionary<string, string>
            {
                ["abc:def"] = "val_2",
                ["abc:ghi"] = "val_3"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(input).Build();
            var origin = new Dictionary<KeyEnum, IDictionary<KeyUintEnum, string>>
            {
                [KeyEnum.abc] = new Dictionary<KeyUintEnum, string> { [KeyUintEnum.abc] = "val_1" }
            };

            config.Bind(origin);

            Assert.Equal(3, origin[KeyEnum.abc].Count);
            Assert.Equal("val_1", origin[KeyEnum.abc][KeyUintEnum.abc]);
            Assert.Equal("val_2", origin[KeyEnum.abc][KeyUintEnum.def]);
            Assert.Equal("val_3", origin[KeyEnum.abc][KeyUintEnum.ghi]);
        }

        [Fact]
        public void ShouldPreserveExistingValuesInArrayWhenItIsDictionaryElement()
        {
            var input = new Dictionary<string, string>
            {
                ["ascii:b"] = "98",
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(input).Build();
            var origin = new Dictionary<string, int[]>
            {
                ["ascii"] = new int[] { 97 }
            };
            config.Bind(origin);

            Assert.Equal(new int[] { 97, 98 }, origin["ascii"]);
        }

        [Fact]
        public void AlreadyInitializedStringDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedStringDictionaryInterface:abc", "val_1"},
                {"AlreadyInitializedStringDictionaryInterface:def", "val_2"},
                {"AlreadyInitializedStringDictionaryInterface:ghi", "val_3"},

                {"IDictionaryNoSetter:Key1", "Value1"},
                {"IDictionaryNoSetter:Key2", "Value2"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.NotNull(options.AlreadyInitializedStringDictionaryInterface);
            Assert.Equal(4, options.AlreadyInitializedStringDictionaryInterface.Count);

            Assert.Equal("This was already here", options.AlreadyInitializedStringDictionaryInterface["123"]);
            Assert.Equal("val_1", options.AlreadyInitializedStringDictionaryInterface["abc"]);
            Assert.Equal("val_2", options.AlreadyInitializedStringDictionaryInterface["def"]);
            Assert.Equal("val_3", options.AlreadyInitializedStringDictionaryInterface["ghi"]);

            Assert.Equal(2, options.IDictionaryNoSetter.Count);
            Assert.Equal("Value1", options.IDictionaryNoSetter["Key1"]);
            Assert.Equal("Value2", options.IDictionaryNoSetter["Key2"]);
        }

        [Fact]
        public void AlreadyInitializedHashSetDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedHashSetDictionary:123:0", "val_1"},
                {"AlreadyInitializedHashSetDictionary:123:1", "val_2"},
                {"AlreadyInitializedHashSetDictionary:123:2", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.NotNull(options.AlreadyInitializedHashSetDictionary);
            Assert.Equal(1, options.AlreadyInitializedHashSetDictionary.Count);

            Assert.Equal("This was already here", options.AlreadyInitializedHashSetDictionary["123"].ElementAt(0));
            Assert.Equal("val_1", options.AlreadyInitializedHashSetDictionary["123"].ElementAt(1));
            Assert.Equal("val_2", options.AlreadyInitializedHashSetDictionary["123"].ElementAt(2));
            Assert.Equal("val_3", options.AlreadyInitializedHashSetDictionary["123"].ElementAt(3));
        }

        [Fact]
        public void CanOverrideExistingDictionaryKey()
        {
            var input = new Dictionary<string, string>
            {
                {"abc", "override"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new Dictionary<string, string>
            {
                {"abc", "default"}
            };

            config.Bind(options);

            var optionsCount = options.Count;

            Assert.Equal(1, optionsCount);
            Assert.Equal("override", options["abc"]);
        }

        [Fact]
        public void IntDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"IntDictionary:abc", "42"},
                {"IntDictionary:def", "43"},
                {"IntDictionary:ghi", "44"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.Equal(3, options.IntDictionary.Count);

            Assert.Equal(42, options.IntDictionary["abc"]);
            Assert.Equal(43, options.IntDictionary["def"]);
            Assert.Equal(44, options.IntDictionary["ghi"]);
        }

        [Fact]
        public void ObjectDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectDictionary:abc:Integer", "1"},
                {"ObjectDictionary:def:Integer", "2"},
                {"ObjectDictionary:ghi:Integer", "3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.Equal(3, options.ObjectDictionary.Count);

            Assert.Equal(1, options.ObjectDictionary["abc"].Integer);
            Assert.Equal(2, options.ObjectDictionary["def"].Integer);
            Assert.Equal(3, options.ObjectDictionary["ghi"].Integer);
        }

        [Fact]
        public void ObjectDictionaryWithHardcodedElements()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectDictionary:abc:Integer", "1"},
                {"ObjectDictionary:def", null },
                {"ObjectDictionary:ghi", null }
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            options.ObjectDictionary = new()
            {
                {"abc", new(){ Integer = 42}},
                {"def", new(){ Integer = 42}},
            };

            Assert.Equal(2, options.ObjectDictionary.Count);
            Assert.Equal(42, options.ObjectDictionary["abc"].Integer);
            Assert.Equal(42, options.ObjectDictionary["def"].Integer);

            config.Bind(options);

            Assert.Equal(3, options.ObjectDictionary.Count);

            Assert.Equal(1, options.ObjectDictionary["abc"].Integer);
            Assert.Equal(42, options.ObjectDictionary["def"].Integer);
            Assert.Equal(0, options.ObjectDictionary["ghi"].Integer);
        }

        [Fact]
        public void ListDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"ListDictionary:abc:0", "abc_0"},
                {"ListDictionary:abc:1", "abc_1"},
                {"ListDictionary:def:0", "def_0"},
                {"ListDictionary:def:1", "def_1"},
                {"ListDictionary:def:2", "def_2"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.Equal(2, options.ListDictionary.Count);
            Assert.Equal(2, options.ListDictionary["abc"].Count);
            Assert.Equal(3, options.ListDictionary["def"].Count);

            Assert.Equal("abc_0", options.ListDictionary["abc"][0]);
            Assert.Equal("abc_1", options.ListDictionary["abc"][1]);
            Assert.Equal("def_0", options.ListDictionary["def"][0]);
            Assert.Equal("def_1", options.ListDictionary["def"][1]);
            Assert.Equal("def_2", options.ListDictionary["def"][2]);
        }

        [Fact]
        public void ISetDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"ISetDictionary:abc:0", "abc_0"},
                {"ISetDictionary:abc:1", "abc_1"},
                {"ISetDictionary:def:0", "def_0"},
                {"ISetDictionary:def:1", "def_1"},
                {"ISetDictionary:def:2", "def_2"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);

            Assert.Equal(2, options.ISetDictionary.Count);
            Assert.Equal(2, options.ISetDictionary["abc"].Count);
            Assert.Equal(3, options.ISetDictionary["def"].Count);

            Assert.Equal("abc_0", options.ISetDictionary["abc"].ElementAt(0));
            Assert.Equal("abc_1", options.ISetDictionary["abc"].ElementAt(1));
            Assert.Equal("def_0", options.ISetDictionary["def"].ElementAt(0));
            Assert.Equal("def_1", options.ISetDictionary["def"].ElementAt(1));
            Assert.Equal("def_2", options.ISetDictionary["def"].ElementAt(2));
        }

        [Fact]
        public void ListInNestedOptionBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectList:0:ListInNestedOption:0", "00"},
                {"ObjectList:0:ListInNestedOption:1", "01"},
                {"ObjectList:1:ListInNestedOption:0", "10"},
                {"ObjectList:1:ListInNestedOption:1", "11"},
                {"ObjectList:1:ListInNestedOption:2", "12"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            Assert.Equal(2, options.ObjectList.Count);
            Assert.Equal(2, options.ObjectList[0].ListInNestedOption.Count);
            Assert.Equal(3, options.ObjectList[1].ListInNestedOption.Count);

            Assert.Equal("00", options.ObjectList[0].ListInNestedOption[0]);
            Assert.Equal("01", options.ObjectList[0].ListInNestedOption[1]);
            Assert.Equal("10", options.ObjectList[1].ListInNestedOption[0]);
            Assert.Equal("11", options.ObjectList[1].ListInNestedOption[1]);
            Assert.Equal("12", options.ObjectList[1].ListInNestedOption[2]);
        }

        [Fact]
        public void NonStringKeyDictionaryBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"NonStringKeyDictionary:abc", "val_1"},
                {"NonStringKeyDictionary:def", "val_2"},
                {"NonStringKeyDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDictionary();
            config.Bind(options);
#if BUILDING_SOURCE_GENERATOR_TESTS // Source generator will not touch the property if it is not supported.
            Assert.Null(options.NonStringKeyDictionary);
#else
            Assert.Empty(options.NonStringKeyDictionary);
#endif
        }

        [Fact]
        public void GetStringArray()
        {
            var input = new Dictionary<string, string>
            {
                {"StringArray:0", "val0"},
                {"StringArray:1", "val1"},
                {"StringArray:2", "val2"},
                {"StringArray:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithArrays();
            config.Bind(options);

            var array = options.StringArray;

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);
        }


        [Fact]
        public void BindStringArray()
        {
            var input = new Dictionary<string, string>
            {
                {"StringArray:0", "val0"},
                {"StringArray:1", "val1"},
                {"StringArray:2", "val2"},
                {"StringArray:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var instance = new OptionsWithArrays();
            config.Bind(instance);

            var array = instance.StringArray;

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);
        }

        [Fact]
        public void GetAlreadyInitializedArray()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedArray:0", "val0"},
                {"AlreadyInitializedArray:1", "val1"},
                {"AlreadyInitializedArray:2", "val2"},
                {"AlreadyInitializedArray:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithArrays();
            config.Bind(options);
            var array = options.AlreadyInitializedArray;

            Assert.Equal(7, array.Length);

            Assert.Equal(OptionsWithArrays.InitialValue, array[0]);
            Assert.Null(array[1]);
            Assert.Null(array[2]);
            Assert.Equal("val0", array[3]);
            Assert.Equal("val1", array[4]);
            Assert.Equal("val2", array[5]);
            Assert.Equal("valx", array[6]);
        }

        [Fact]
        public void BindAlreadyInitializedArray()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedArray:0", "val0"},
                {"AlreadyInitializedArray:1", "val1"},
                {"AlreadyInitializedArray:2", "val2"},
                {"AlreadyInitializedArray:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithArrays();
            config.Bind(options);

            var array = options.AlreadyInitializedArray;

            Assert.Equal(7, array.Length);

            Assert.Equal(OptionsWithArrays.InitialValue, array[0]);
            Assert.Null(array[1]);
            Assert.Null(array[2]);
            Assert.Equal("val0", array[3]);
            Assert.Equal("val1", array[4]);
            Assert.Equal("val2", array[5]);
            Assert.Equal("valx", array[6]);
        }

        [Fact]
        public void ArrayInNestedOptionBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"ObjectArray:0:ArrayInNestedOption:0", "0"},
                {"ObjectArray:0:ArrayInNestedOption:1", "1"},
                {"ObjectArray:1:ArrayInNestedOption:0", "10"},
                {"ObjectArray:1:ArrayInNestedOption:1", "11"},
                {"ObjectArray:1:ArrayInNestedOption:2", "12"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new OptionsWithArrays();
            config.Bind(options);

            Assert.Equal(2, options.ObjectArray.Length);
            Assert.Equal(2, options.ObjectArray[0].ArrayInNestedOption.Length);
            Assert.Equal(3, options.ObjectArray[1].ArrayInNestedOption.Length);

            Assert.Equal(0, options.ObjectArray[0].ArrayInNestedOption[0]);
            Assert.Equal(1, options.ObjectArray[0].ArrayInNestedOption[1]);
            Assert.Equal(10, options.ObjectArray[1].ArrayInNestedOption[0]);
            Assert.Equal(11, options.ObjectArray[1].ArrayInNestedOption[1]);
            Assert.Equal(12, options.ObjectArray[1].ArrayInNestedOption[2]);
        }

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
        public void UnsupportedMultidimensionalArrays()
        {
            var input = new Dictionary<string, string>
            {
                {"DimensionalArray:0:0", "a"},
                {"DimensionalArray:0:1", "b"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new OptionsWithArrays();

            var exception = Assert.Throws<InvalidOperationException>(
                () => config.Bind(options));
            Assert.Equal(
                SR.Format(SR.Error_UnsupportedMultidimensionalArray, typeof(string[,])),
                exception.Message);
        }

        [Fact]
        public void JaggedArrayBinding()
        {
            var input = new Dictionary<string, string>
            {
                {"JaggedArray:0:0", "00"},
                {"JaggedArray:0:1", "01"},
                {"JaggedArray:1:0", "10"},
                {"JaggedArray:1:1", "11"},
                {"JaggedArray:1:2", "12"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new OptionsWithArrays();
            config.Bind(options);

            Assert.Equal(2, options.JaggedArray.Length);
            Assert.Equal(2, options.JaggedArray[0].Length);
            Assert.Equal(3, options.JaggedArray[1].Length);

            Assert.Equal("00", options.JaggedArray[0][0]);
            Assert.Equal("01", options.JaggedArray[0][1]);
            Assert.Equal("10", options.JaggedArray[1][0]);
            Assert.Equal("11", options.JaggedArray[1][1]);
            Assert.Equal("12", options.JaggedArray[1][2]);
        }

        [Fact]
        public void ReadOnlyArrayIsIgnored()
        {
            var input = new Dictionary<string, string>
            {
                {"ReadOnlyArray:0", "10"},
                {"ReadOnlyArray:1", "20"},
                {"ReadOnlyArray:2", "30"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();
            var options = new OptionsWithArrays();
            config.Bind(options);

            Assert.Equal(new OptionsWithArrays().ReadOnlyArray, options.ReadOnlyArray);
        }

        [Fact]
        public void CanBindUninitializedIEnumerable()
        {
            var input = new Dictionary<string, string>
            {
                {"IEnumerable:0", "val0"},
                {"IEnumerable:1", "val1"},
                {"IEnumerable:2", "val2"},
                {"IEnumerable:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            var array = options.IEnumerable.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);
        }

        [Fact]
        public void CanBindInitializedIEnumerableAndTheOriginalItemsAreNotMutated()
        {
            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedIEnumerableInterface:0", "val0"},
                {"AlreadyInitializedIEnumerableInterface:1", "val1"},
                {"AlreadyInitializedIEnumerableInterface:2", "val2"},
                {"AlreadyInitializedIEnumerableInterface:x", "valx"},

                {"ICollectionNoSetter:0", "val0"},
                {"ICollectionNoSetter:1", "val1"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new InitializedCollectionsOptions();
            config.Bind(options);

            var array = options.AlreadyInitializedIEnumerableInterface.ToArray();

            Assert.Equal(6, array.Length);

            Assert.Equal("This was here too", array[0]);
            Assert.Equal("Don't touch me!", array[1]);
            Assert.Equal("val0", array[2]);
            Assert.Equal("val1", array[3]);
            Assert.Equal("val2", array[4]);
            Assert.Equal("valx", array[5]);

            // the original list hasn't been touched
            Assert.Equal(2, options.ListUsedInIEnumerableFieldAndShouldNotBeTouched.Count);
            Assert.Equal("This was here too", options.ListUsedInIEnumerableFieldAndShouldNotBeTouched.ElementAt(0));
            Assert.Equal("Don't touch me!", options.ListUsedInIEnumerableFieldAndShouldNotBeTouched.ElementAt(1));

            Assert.Equal(2, options.ICollectionNoSetter.Count);
            Assert.Equal("val0", options.ICollectionNoSetter.ElementAt(0));
            Assert.Equal("val1", options.ICollectionNoSetter.ElementAt(1));

            // Ensure expandability of the returned collection
            options.ICollectionNoSetter.Add("ExtraItem");
            Assert.Equal(3, options.ICollectionNoSetter.Count);
            Assert.Equal("ExtraItem", options.ICollectionNoSetter.ElementAt(2));
        }

        [Fact]
        public void CanBindInitializedCustomIEnumerableBasedList()
        {
            // A field declared as IEnumerable<T> that is instantiated with a class
            // that directly implements IEnumerable<T> is still bound, but with
            // a new List<T> with the original values copied over.

            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedCustomListDerivedFromIEnumerable:0", "val0"},
                {"AlreadyInitializedCustomListDerivedFromIEnumerable:1", "val1"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new InitializedCollectionsOptions();
            config.Bind(options);

            var array = options.AlreadyInitializedCustomListDerivedFromIEnumerable.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("Item1", array[0]);
            Assert.Equal("Item2", array[1]);
            Assert.Equal("val0", array[2]);
            Assert.Equal("val1", array[3]);
        }

        [Fact]
        public void CanBindInitializedCustomIndirectlyDerivedIEnumerableList()
        {
            // A field declared as IEnumerable<T> that is instantiated with a class
            // that indirectly implements IEnumerable<T> is still bound, but with
            // a new List<T> with the original values copied over.

            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedCustomListIndirectlyDerivedFromIEnumerable:0", "val0"},
                {"AlreadyInitializedCustomListIndirectlyDerivedFromIEnumerable:1", "val1"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new InitializedCollectionsOptions();
            config.Bind(options);

            var array = options.AlreadyInitializedCustomListIndirectlyDerivedFromIEnumerable.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("Item1", array[0]);
            Assert.Equal("Item2", array[1]);
            Assert.Equal("val0", array[2]);
            Assert.Equal("val1", array[3]);
        }

        [Fact]
        public void CanBindInitializedIReadOnlyDictionaryAndDoesNotModifyTheOriginal()
        {
            // A field declared as IEnumerable<T> that is instantiated with a class
            // that indirectly implements IEnumerable<T> is still bound, but with
            // a new List<T> with the original values copied over.

            var input = new Dictionary<string, string>
            {
                {"AlreadyInitializedDictionary:existing_key_1", "overridden!"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new InitializedCollectionsOptions();
            config.Bind(options);

            var array = options.AlreadyInitializedDictionary.ToArray();

            Assert.Equal(2, array.Length);

            Assert.Equal("overridden!", options.AlreadyInitializedDictionary["existing_key_1"]);
            Assert.Equal("val_2", options.AlreadyInitializedDictionary["existing_key_2"]);

            Assert.NotEqual(options.AlreadyInitializedDictionary, InitializedCollectionsOptions.ExistingDictionary);

            Assert.Equal("val_1", InitializedCollectionsOptions.ExistingDictionary["existing_key_1"]);
            Assert.Equal("val_2", InitializedCollectionsOptions.ExistingDictionary["existing_key_2"]);
        }

        [Fact]
        public void CanBindUninitializedICollection()
        {
            var input = new Dictionary<string, string>
            {
                {"ICollection:0", "val0"},
                {"ICollection:1", "val1"},
                {"ICollection:2", "val2"},
                {"ICollection:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            var array = options.ICollection.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);

            // Ensure expandability of the returned collection
            options.ICollection.Add("ExtraItem");
            Assert.Equal(5, options.ICollection.Count);
            Assert.Equal("ExtraItem", options.ICollection.ElementAt(4));
        }

        [Fact]
        public void CanBindUninitializedIList()
        {
            var input = new Dictionary<string, string>
            {
                {"IList:0", "val0"},
                {"IList:1", "val1"},
                {"IList:2", "val2"},
                {"IList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            IList<string> list = options.IList;

            Assert.Equal(4, list.Count);

            Assert.Equal("val0", list[0]);
            Assert.Equal("val1", list[1]);
            Assert.Equal("val2", list[2]);
            Assert.Equal("valx", list[3]);

            // Ensure expandability of the returned list
            options.IList.Add("ExtraItem");
            Assert.Equal(5, options.IList.Count);
            Assert.Equal("ExtraItem", options.IList[4]);
        }

        [Fact]
        public void CanBindUninitializedIReadOnlyCollection()
        {
            var input = new Dictionary<string, string>
            {
                {"IReadOnlyCollection:0", "val0"},
                {"IReadOnlyCollection:1", "val1"},
                {"IReadOnlyCollection:2", "val2"},
                {"IReadOnlyCollection:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            var array = options.IReadOnlyCollection.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);
        }

        [Fact]
        public void CanBindUninitializedIReadOnlyList()
        {
            var input = new Dictionary<string, string>
            {
                {"IReadOnlyList:0", "val0"},
                {"IReadOnlyList:1", "val1"},
                {"IReadOnlyList:2", "val2"},
                {"IReadOnlyList:x", "valx"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            var array = options.IReadOnlyList.ToArray();

            Assert.Equal(4, array.Length);

            Assert.Equal("val0", array[0]);
            Assert.Equal("val1", array[1]);
            Assert.Equal("val2", array[2]);
            Assert.Equal("valx", array[3]);
        }

        [Fact]
        public void CanBindUninitializedIDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"IDictionary:abc", "val_1"},
                {"IDictionary:def", "val_2"},
                {"IDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            Assert.Equal(3, options.IDictionary.Count);

            Assert.Equal("val_1", options.IDictionary["abc"]);
            Assert.Equal("val_2", options.IDictionary["def"]);
            Assert.Equal("val_3", options.IDictionary["ghi"]);
        }

        [Fact]
        public void CanBindUninitializedIReadOnlyDictionary()
        {
            var input = new Dictionary<string, string>
            {
                {"IReadOnlyDictionary:abc", "val_1"},
                {"IReadOnlyDictionary:def", "val_2"},
                {"IReadOnlyDictionary:ghi", "val_3"}
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new UninitializedCollectionsOptions();
            config.Bind(options);

            Assert.Equal(3, options.IReadOnlyDictionary.Count);

            Assert.Equal("val_1", options.IReadOnlyDictionary["abc"]);
            Assert.Equal("val_2", options.IReadOnlyDictionary["def"]);
            Assert.Equal("val_3", options.IReadOnlyDictionary["ghi"]);
        }

        /// <summary>
        /// Replicates scenario from https://github.com/dotnet/runtime/issues/65710
        /// </summary>
        [Fact]
        public void CanBindWithInterdependentProperties()
        {
            var input = new Dictionary<string, string>
            {
                {"ConfigValues:0", "5"},
                {"ConfigValues:1", "50"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithInterdependentProperties();
            config.Bind(options);

            Assert.Equal(new[] { 5, 50 }, options.ConfigValues);
            Assert.Equal(new[] { 50 }, options.FilteredConfigValues);
        }

        /// <summary>
        /// Replicates scenario from https://github.com/dotnet/runtime/issues/63479
        /// </summary>
        [Fact]
        public void TestCanBindListPropertyWithoutSetter()
        {
            var input = new Dictionary<string, string>
            {
                {"ListPropertyWithoutSetter:0", "a"},
                {"ListPropertyWithoutSetter:1", "b"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithLists();
            config.Bind(options);

            Assert.Equal(new[] { "a", "b" }, options.ListPropertyWithoutSetter);
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

        [Fact]
        public void CanBindISetNoSetter()
        {
            var dic = new Dictionary<string, string>
            {
                {"ISetNoSetter:0", "Yo1"},
                {"ISetNoSetter:1", "Yo2"},
                {"ISetNoSetter:2", "Yo2"},
            };
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(dic);

            var config = configurationBuilder.Build();

            var options = config.Get<ComplexOptions>()!;

            Assert.Equal(2, options.ISetNoSetter.Count);
            Assert.Equal("Yo1", options.ISetNoSetter.ElementAt(0));
            Assert.Equal("Yo2", options.ISetNoSetter.ElementAt(1));
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Dropped members for binding: diagnostic warning issued instead.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Dropped members for binding: diagnostic warning issued instead.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Dropped members for binding: diagnostic warning issued instead.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Dropped members for binding: diagnostic warning issued instead.
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
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

        [ConditionalFact(typeof(TestHelpers), nameof(TestHelpers.NotSourceGenMode))] // Ensure exception messages are in sync
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
        public void DifferentDictionaryBindingCasesTest()
        {
            var dic = new Dictionary<string, string>() { { "key", "value" } };
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(dic)
                .Build();

            Assert.Single(config.Get<Dictionary<string, string>>());
            Assert.Single(config.Get<IDictionary<string, string>>());
            Assert.Single(config.Get<ExtendedDictionary<string, string>>());
            // The System.Reflection.AmbiguousMatchException scenario that
            // this test validates is not applicable. Source generator will
            // statically bind to best-fit dictionary value indexer.
#if !BUILDING_SOURCE_GENERATOR_TESTS
            Assert.Single(config.Get<ImplementerOfIDictionaryClass<string, string>>());
#endif
        }

        [Fact]
        public void TestOptionsWithDifferentCollectionInterfaces()
        {
            var input = new Dictionary<string, string>
            {
                {"InstantiatedIEnumerable:0", "value3"},
                {"UnInstantiatedIEnumerable:0", "value1"},
                {"InstantiatedIList:0", "value3"},
                {"InstantiatedIReadOnlyList:0", "value3"},
                {"UnInstantiatedIReadOnlyList:0", "value"},
                {"UnInstantiatedIList:0", "value"},
                {"InstantiatedIDictionary:Key3", "value3"},
                {"InstantiatedIReadOnlyDictionary:Key3", "value3"},
                {"UnInstantiatedIReadOnlyDictionary:Key", "value"},
                {"InstantiatedISet:0", "B"},
                {"InstantiatedISet:1", "C"},
                {"UnInstantiatedISet:0", "a"},
                {"UnInstantiatedISet:1", "A"},
                {"UnInstantiatedISet:2", "B"},
                {"InstantiatedIReadOnlySet:0", "Z"},
                {"UnInstantiatedIReadOnlySet:0", "y"},
                {"UnInstantiatedIReadOnlySet:1", "z"},
                {"InstantiatedICollection:0", "d"},
                {"UnInstantiatedICollection:0", "t"},
                {"UnInstantiatedICollection:1", "a"},
                {"InstantiatedIReadOnlyCollection:0", "d"},
                {"UnInstantiatedIReadOnlyCollection:0", "r"},
                {"UnInstantiatedIReadOnlyCollection:1", "e"},
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(input);
            var config = configurationBuilder.Build();

            var options = new OptionsWithDifferentCollectionInterfaces();
            config.Bind(options);

            Assert.True(3 == options.InstantiatedIEnumerable.Count(), $"InstantiatedIEnumerable count is {options.InstantiatedIEnumerable.Count()} .. {options.InstantiatedIEnumerable.ElementAt(options.InstantiatedIEnumerable.Count() - 1)}");
            Assert.Equal("value1", options.InstantiatedIEnumerable.ElementAt(0));
            Assert.Equal("value2", options.InstantiatedIEnumerable.ElementAt(1));
            Assert.Equal("value3", options.InstantiatedIEnumerable.ElementAt(2));
            Assert.False(options.IsSameInstantiatedIEnumerable());

            Assert.Equal(1, options.UnInstantiatedIEnumerable.Count());
            Assert.Equal("value1", options.UnInstantiatedIEnumerable.ElementAt(0));

            Assert.True(3 == options.InstantiatedIList.Count(), $"InstantiatedIList count is {options.InstantiatedIList.Count()} .. {options.InstantiatedIList[options.InstantiatedIList.Count() - 1]}");
            Assert.Equal("value1", options.InstantiatedIList[0]);
            Assert.Equal("value2", options.InstantiatedIList[1]);
            Assert.Equal("value3", options.InstantiatedIList[2]);
            Assert.True(options.IsSameInstantiatedIList());

            Assert.Equal(1, options.UnInstantiatedIList.Count());
            Assert.Equal("value", options.UnInstantiatedIList[0]);

            Assert.True(3 == options.InstantiatedIReadOnlyList.Count(), $"InstantiatedIReadOnlyList count is {options.InstantiatedIReadOnlyList.Count()} .. {options.InstantiatedIReadOnlyList[options.InstantiatedIReadOnlyList.Count() - 1]}");
            Assert.Equal("value1", options.InstantiatedIReadOnlyList[0]);
            Assert.Equal("value2", options.InstantiatedIReadOnlyList[1]);
            Assert.Equal("value3", options.InstantiatedIReadOnlyList[2]);
            Assert.False(options.IsSameInstantiatedIReadOnlyList());

            Assert.Equal(1, options.UnInstantiatedIReadOnlyList.Count());
            Assert.Equal("value", options.UnInstantiatedIReadOnlyList[0]);

            Assert.True(3 == options.InstantiatedIReadOnlyList.Count(), $"InstantiatedIReadOnlyList count is {options.InstantiatedIReadOnlyList.Count()} .. {options.InstantiatedIReadOnlyList[options.InstantiatedIReadOnlyList.Count() - 1]}");
            Assert.Equal(new string[] { "Key1", "Key2", "Key3" }, options.InstantiatedIDictionary.Keys);
            Assert.Equal(new string[] { "value1", "value2", "value3" }, options.InstantiatedIDictionary.Values);
            Assert.True(options.IsSameInstantiatedIDictionary());

            Assert.True(3 == options.InstantiatedIReadOnlyDictionary.Count(), $"InstantiatedIReadOnlyDictionary count is {options.InstantiatedIReadOnlyDictionary.Count()} .. {options.InstantiatedIReadOnlyDictionary.ElementAt(options.InstantiatedIReadOnlyDictionary.Count() - 1)}");
            Assert.Equal(new string[] { "Key1", "Key2", "Key3" }, options.InstantiatedIReadOnlyDictionary.Keys);
            Assert.Equal(new string[] { "value1", "value2", "value3" }, options.InstantiatedIReadOnlyDictionary.Values);
            Assert.False(options.IsSameInstantiatedIReadOnlyDictionary());

            Assert.Equal(1, options.UnInstantiatedIReadOnlyDictionary.Count());
            Assert.Equal(new string[] { "Key" }, options.UnInstantiatedIReadOnlyDictionary.Keys);
            Assert.Equal(new string[] { "value" }, options.UnInstantiatedIReadOnlyDictionary.Values);

            Assert.True(3 == options.InstantiatedISet.Count(), $"InstantiatedISet count is {options.InstantiatedISet.Count()} .. {string.Join(", ", options.InstantiatedISet)} .. {options.IsSameInstantiatedISet()}");
            Assert.Equal(new string[] { "a", "b", "C" }, options.InstantiatedISet);
            Assert.True(options.IsSameInstantiatedISet());

            Assert.True(3 == options.UnInstantiatedISet.Count(), $"UnInstantiatedISet count is {options.UnInstantiatedISet.Count()} .. {options.UnInstantiatedISet.ElementAt(options.UnInstantiatedISet.Count() - 1)}");
            Assert.Equal(new string[] { "a", "A", "B" }, options.UnInstantiatedISet);

#if NETCOREAPP
            Assert.True(3 == options.InstantiatedIReadOnlySet.Count(), $"InstantiatedIReadOnlySet count is {options.InstantiatedIReadOnlySet.Count()} .. {options.InstantiatedIReadOnlySet.ElementAt(options.InstantiatedIReadOnlySet.Count() - 1)}");
            Assert.Equal(new string[] { "a", "b", "Z" }, options.InstantiatedIReadOnlySet);
            Assert.False(options.IsSameInstantiatedIReadOnlySet());

            Assert.Equal(2, options.UnInstantiatedIReadOnlySet.Count());
            Assert.Equal(new string[] { "y", "z" }, options.UnInstantiatedIReadOnlySet);
#endif
            Assert.Equal(4, options.InstantiatedICollection.Count());
            Assert.Equal(new string[] { "a", "b", "c", "d" }, options.InstantiatedICollection);
            Assert.True(options.IsSameInstantiatedICollection());

            Assert.Equal(2, options.UnInstantiatedICollection.Count());
            Assert.Equal(new string[] { "t", "a" }, options.UnInstantiatedICollection);

            Assert.Equal(4, options.InstantiatedIReadOnlyCollection.Count());
            Assert.Equal(new string[] { "a", "b", "c", "d" }, options.InstantiatedIReadOnlyCollection);
            Assert.False(options.IsSameInstantiatedIReadOnlyCollection());

            Assert.Equal(2, options.UnInstantiatedIReadOnlyCollection.Count());
            Assert.Equal(new string[] { "r", "e" }, options.UnInstantiatedIReadOnlyCollection);
        }

        [Fact]
        public void TestMutatingDictionaryValues()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection()
                .Build();

            config["Key:0"] = "NewValue";
            var dict = new Dictionary<string, string[]>() { { "Key", new[] { "InitialValue" } } };

            Assert.Equal(1, dict["Key"].Length);
            Assert.Equal("InitialValue", dict["Key"][0]);

            // Binding will accumulate to the values inside the dictionary.
            config.Bind(dict);
            Assert.Equal(2, dict["Key"].Length);
            Assert.Equal("InitialValue", dict["Key"][0]);
            Assert.Equal("NewValue", dict["Key"][1]);
        }

        [Fact]
        public void TestCollectionWithNullOrEmptyItems()
        {
            string json = @"
                {
                    ""CollectionContainer"": [
                    {
                        ""Elements"":
                        {
                            ""Typdde"": ""UserCredentials"",
                            ""poop"": ""00"",
                            ""111"": """",
                            ""BaseUrl"": ""cccccc"",
                            ""Valid"": {
                                ""Type"": ""System.Boolean""
                            },
                        }
                    }
                    ]
                }
            ";

            var builder = new ConfigurationBuilder();
            Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            builder.AddJsonStream(stream);
            IConfigurationRoot config = builder.Build();

            List<CollectionContainer> result = config.GetSection("CollectionContainer").Get<List<CollectionContainer>>();
            Assert.Equal(1, result.Count);
            Assert.Equal(2, result[0].Elements.Count);
            Assert.Null(result[0].Elements[0].Type);
            Assert.Equal("System.Boolean", result[0].Elements[1].Type);
        }

        // Test behavior for root level arrays.

        // Tests for TypeConverter usage.
    }
}
