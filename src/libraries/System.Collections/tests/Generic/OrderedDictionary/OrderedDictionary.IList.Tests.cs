// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Collections.Tests
{
    public class OrderedDictionary_IList_Tests : IList_Generic_Tests<KeyValuePair<string, string>>
    {
        protected override bool DefaultValueAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool DefaultValueWhenNotAllowed_Throws => true;
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;

        protected override KeyValuePair<string, string> CreateT(int seed) =>
            new KeyValuePair<string, string>(CreateString(seed), CreateString(seed + 500));

        protected override IList<KeyValuePair<string, string>> GenericIListFactory() => new OrderedDictionary<string, string>();

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        [Fact]
        public void IList_Generic_IndexOfRequiresValueMatch()
        {
            var dictionary = new OrderedDictionary<string, string>()
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3"
            };

            KeyValuePair<string, string> pair = dictionary.GetAt(2);
            Assert.Equal(2, ((IList<KeyValuePair<string, string>>)dictionary).IndexOf(pair));
            Assert.Equal(-1, ((IList<KeyValuePair<string, string>>)dictionary).IndexOf(new KeyValuePair<string, string>(pair.Key, "d")));
        }
    }

    public class OrderedDictionary_IList_NonGeneric_Tests : IList_NonGeneric_Tests
    {
        protected override bool NullAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throw => true;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override bool SupportsSerialization => false;
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override bool IList_Empty_CurrentAfterAdd_Throws => true;
        protected override ModifyOperation ModifyEnumeratorThrows => ModifyOperation.Add | ModifyOperation.Insert | ModifyOperation.Remove | ModifyOperation.Clear;

        protected override object CreateT(int seed) =>
            new KeyValuePair<string, string>(CreateString(seed), CreateString(seed + 500));

        protected override IList NonGenericIListFactory() => new OrderedDictionary<string, string>();

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }

        [Fact]
        public void Indexer_Set_WrongType_ThrowsException()
        {
            IList list = NonGenericIListFactory();
            list.Add(new KeyValuePair<string, string>("key", "value"));
            AssertExtensions.Throws<ArgumentException>("value", () => list[0] = new KeyValuePair<int, int>(42, 42));
            AssertExtensions.Throws<ArgumentException>("value", () => list[0] = "key");
        }

        [Fact]
        public void Add_WrongType_ThrowsException()
        {
            IList list = NonGenericIListFactory();
            list.Add(KeyValuePair.Create("key", "value"));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Add(new KeyValuePair<int, int>(42, 42)));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Add(new KeyValuePair<string, int>("42", 42)));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Add(42));
            Assert.Equal(1, list.Count);
        }

        [Fact]
        public void Insert_WrongType_ThrowsException()
        {
            IList list = NonGenericIListFactory();
            list.Insert(0, KeyValuePair.Create("key", "value"));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Insert(0, new KeyValuePair<int, int>(42, 42)));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Insert(0, new KeyValuePair<string, int>("42", 42)));
            AssertExtensions.Throws<ArgumentException>("value", () => list.Insert(0, 42));
            Assert.Equal(1, list.Count);
        }
    }

    public class OrderedDictionary_Keys_IList_Generic_Tests : IList_Generic_Tests<string>
    {
        protected override bool DefaultValueAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool DefaultValueWhenNotAllowed_Throws => true;
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Enumerable.Empty<ModifyEnumerable>();
        protected override bool IsReadOnly => true;

        protected override string CreateT(int seed) => CreateString(seed);

        protected override IList<string> GenericIListFactory() => new OrderedDictionary<string, string>().Keys;

        protected override IList<string> GenericIListFactory(int count)
        {
            OrderedDictionary<string, string> dictionary = new();

            int seed = 42;
            while (dictionary.Count < count)
            {
                string key = CreateT(seed++);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, CreateT(seed++));
                }
            }

            return dictionary.Keys;
        }

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }
    }

    public class OrderedDictionary_Keys_IList_NonGeneric_Tests : IList_NonGeneric_Tests
    {
        protected override bool NullAllowed => false;
        protected override bool DuplicateValuesAllowed => false;
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throw => true;
        protected override bool SupportsSerialization => false;
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Enumerable.Empty<ModifyEnumerable>();
        protected override bool IsReadOnly => true;

        protected override object CreateT(int seed) =>
            CreateString(seed);

        protected override IList NonGenericIListFactory() => new OrderedDictionary<string, string>().Keys;

        protected override IList NonGenericIListFactory(int count)
        {
            OrderedDictionary<string, string> dictionary = new();

            int seed = 42;
            while (dictionary.Count < count)
            {
                string key = CreateString(seed++);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, CreateString(seed++));
                }
            }

            return dictionary.Keys;
        }

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }
    }

    public class OrderedDictionary_Values_IList_Generic_Tests : IList_Generic_Tests<string>
    {
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_Empty_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Enumerable.Empty<ModifyEnumerable>();
        protected override bool IsReadOnly => true;

        protected override string CreateT(int seed) => CreateString(seed);

        protected override IList<string> GenericIListFactory() => new OrderedDictionary<string, string>().Values;

        protected override IList<string> GenericIListFactory(int count)
        {
            OrderedDictionary<string, string> dictionary = new();

            int seed = 42;
            while (dictionary.Count < count)
            {
                string key = CreateT(seed++);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, CreateT(seed++));
                }
            }

            return dictionary.Values;
        }

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }
    }

    public class OrderedDictionary_Values_IList_NonGeneric_Tests : IList_NonGeneric_Tests
    {
        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
        protected override bool Enumerator_Empty_Current_UndefinedOperation_Throw => true;
        protected override bool SupportsSerialization => false;
        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfEnumType_ThrowType => typeof(ArgumentException);
        protected override IEnumerable<ModifyEnumerable> GetModifyEnumerables(ModifyOperation operations) => Enumerable.Empty<ModifyEnumerable>();
        protected override bool IsReadOnly => true;

        protected override object CreateT(int seed) =>
            CreateString(seed);

        protected override IList NonGenericIListFactory() => new OrderedDictionary<string, string>().Values;

        protected override IList NonGenericIListFactory(int count)
        {
            OrderedDictionary<string, string> dictionary = new();

            int seed = 42;
            while (dictionary.Count < count)
            {
                string key = CreateString(seed++);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, CreateString(seed++));
                }
            }

            return dictionary.Values;
        }

        private string CreateString(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes1 = new byte[stringLength];
            rand.NextBytes(bytes1);
            return Convert.ToBase64String(bytes1);
        }
    }
}
