// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Configuration;
using System.IO;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationElementCollectionTests
    {
        public class SimpleElement : ConfigurationElement
        {
        }

        private class SimpleCollection : ConfigurationElementCollection
        {
            public SimpleCollection()
                : base()
            { }

            public SimpleCollection(IComparer comparer)
                : base(comparer)
            { }

            protected override ConfigurationElement CreateNewElement()
            {
                throw new NotImplementedException();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                return "FooKey";
            }

            public bool TestThrowOnDuplicate => ThrowOnDuplicate;
            public string TestElementName => ElementName;

            public void TestBaseAdd(ConfigurationElement element)
            {
                BaseAdd(element);
            }

            public void TestBaseAdd(int index, ConfigurationElement element)
            {
                BaseAdd(index, element);
            }
        }

        private class ReadOnlySimpleCollection : SimpleCollection
        {
            public override bool IsReadOnly() => true;
        }

        [Fact]
        public void NullComparerThrows()
        {
            Assert.Equal("comparer", Assert.Throws<ArgumentNullException>(() => new SimpleCollection(null)).ParamName);
        }

        [Fact]
        public void ReadOnlyFalseByDefault()
        {
            Assert.False(new SimpleCollection().IsReadOnly());
        }

        [Fact]
        public void InitialCountIsZero()
        {
            Assert.Equal(0, new SimpleCollection().Count);
        }

        [Fact]
        public void ElementNameIsEmpty()
        {
            Assert.Equal("", new SimpleCollection().TestElementName);
        }

        [Fact]
        public void CollectionTypeIsAddRemoveMap()
        {
            Assert.Equal(ConfigurationElementCollectionType.AddRemoveClearMap, new SimpleCollection().CollectionType);
        }

        [Fact]
        public void SyncRootIsNull()
        {
            Assert.Null(new SimpleCollection().SyncRoot);
        }

        [Fact]
        public void SynchronizedIsFalse()
        {
            Assert.False(new SimpleCollection().IsSynchronized);
        }

        [Fact]
        public void EmitClearIsFalse()
        {
            Assert.False(new SimpleCollection().EmitClear);
        }

        [Fact]
        public void SetEmitClear()
        {
            var collection = new SimpleCollection();
            collection.EmitClear = true;
            Assert.True(collection.EmitClear);
        }

        [Fact]
        public void AddBaseElementSucceeds()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(new SimpleElement());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void AddBaseElementAtIndexSucceeds()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(-1, new SimpleElement());
            Assert.Equal(1, collection.Count);
        }

        [Fact]
        public void BaseAddIndexOutOfRangeThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new SimpleCollection().TestBaseAdd(-2, null));
        }

        [Fact]
        public void BaseAddReadOnlyThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new ReadOnlySimpleCollection().TestBaseAdd(null));
        }

        [Fact]
        public void BaseAddIndexReadOnlyThrows()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new ReadOnlySimpleCollection().TestBaseAdd(-1, null));
        }

        [Fact]
        public void BaseAddNullThrows()
        {
            Assert.Throws<NullReferenceException>(() => new SimpleCollection().TestBaseAdd(null));
        }

        [Fact]
        public void BaseAddIndexNullThrows()
        {
            Assert.Throws<NullReferenceException>(() => new SimpleCollection().TestBaseAdd(-1, null));
        }

        [Fact]
        public void ThrowOnDuplicateIsTrue()
        {
            Assert.True(new SimpleCollection().TestThrowOnDuplicate);
        }

        public class BasicCollection : ConfigurationElementCollection
        {
            protected override ConfigurationElement CreateNewElement()
            {
                throw new NotImplementedException();
            }

            protected override object GetElementKey(ConfigurationElement element)
            {
                throw new NotImplementedException();
            }

            public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;
            public bool TestThrowOnDuplicate => ThrowOnDuplicate;
        }

        [Fact]
        public void BasicThrowOnDuplicateIsFalse()
        {
            Assert.False(new BasicCollection().TestThrowOnDuplicate);
        }
    }
}
