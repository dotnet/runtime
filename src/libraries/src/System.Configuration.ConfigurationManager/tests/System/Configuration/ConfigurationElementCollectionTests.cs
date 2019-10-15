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
            public static ConfigurationElement TestElement = new SimpleElement();

            public SimpleCollection()
                : base()
            { }

            public SimpleCollection(IComparer comparer)
                : base(comparer)
            { }

            // These two are abstract
            protected override ConfigurationElement CreateNewElement() => TestElement;
            protected override object GetElementKey(ConfigurationElement element) => element == null ? null : "FooKey";

            public bool TestThrowOnDuplicate => ThrowOnDuplicate;
            public string TestElementName => ElementName;
            public bool TestIsElementName(string elementName) => IsElementName(elementName);
            public bool TestIsElementRemovable(ConfigurationElement element) => IsElementRemovable(element);
            public void TestBaseAdd(ConfigurationElement element) => BaseAdd(element);
            public void TestBaseAdd(int index, ConfigurationElement element) => BaseAdd(index, element);
            public ConfigurationElement TestCreateNewElement(string elementName) => CreateNewElement(elementName);
            public int TestBaseIndexOf(ConfigurationElement element) => BaseIndexOf(element);
        }

        private class ReadOnlySimpleCollection : SimpleCollection
        {
            public override bool IsReadOnly() => true;
        }

        [Fact]
        public void NullComparerThrows()
        {
            AssertExtensions.Throws<ArgumentNullException>("comparer", () => new SimpleCollection(null));
        }

        [Fact]
        public void ReadOnlyFalse()
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
        public void IsElementNameIsFalse()
        {
            Assert.False(new SimpleCollection().TestIsElementName("foo"));
        }

        [Fact]
        public void IsElementRemovableIsTrue()
        {
            Assert.True(new SimpleCollection().TestIsElementRemovable(null));
        }

        [Fact]
        public void CreateNewElementByNameCallsCreateNewElement()
        {
            Assert.Same(SimpleCollection.TestElement, new SimpleCollection().TestCreateNewElement("foo"));
        }

        [Fact]
        public void BaseIndexOfThrowsForNull()
        {
            Assert.Throws<ConfigurationErrorsException>(() => new SimpleCollection().TestBaseIndexOf(null));
        }

        [Fact]
        public void BaseIndexOfReturnsMinusOneForEmpty()
        {
            Assert.Equal(-1, new SimpleCollection().TestBaseIndexOf(SimpleCollection.TestElement));
        }

        [Fact]
        public void BaseIndexReturnsZeroForOneItem()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(SimpleCollection.TestElement);
            Assert.Equal(0, collection.TestBaseIndexOf(SimpleCollection.TestElement));
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
            Assert.Throws<ConfigurationErrorsException>(() => new SimpleCollection().TestBaseAdd(null));
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

        private class BasicCollection : SimpleCollection
        {
            public override ConfigurationElementCollectionType CollectionType => ConfigurationElementCollectionType.BasicMap;
        }

        [Fact]
        public void BasicThrowOnDuplicateIsFalse()
        {
            Assert.False(new BasicCollection().TestThrowOnDuplicate);
        }

        [Fact]
        [ActiveIssue("dotnet/corefx #19338", TargetFrameworkMonikers.NetFramework)]
        public void EqualsNullIsFalse()
        {
            // Note: this null refs on desktop
            Assert.False(new SimpleCollection().Equals(null));
        }

        [Fact]
        public void EmptyEqualsEmpty()
        {
            Assert.True(new SimpleCollection().Equals(new SimpleCollection()));
        }

        [Fact]
        public void DifferentSubclassesNotEqual()
        {
            Assert.False(new SimpleCollection().Equals(new BasicCollection()));
        }

        [Fact]
        public void DifferentCountNotEqual()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(SimpleCollection.TestElement);
            Assert.False(new SimpleCollection().Equals(collection));
        }

        [Fact]
        public void ICollectionCopyToCopiesItem()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(SimpleCollection.TestElement);
            Array array = new ConfigurationElement[1];
            ((ICollection)collection).CopyTo(array, 0);
            Assert.Equal(SimpleCollection.TestElement, array.GetValue(0));
        }

        [Fact]
        public void CopyToCopiesItem()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(SimpleCollection.TestElement);
            ConfigurationElement[] array = new ConfigurationElement[1];
            collection.CopyTo(array, 0);
            Assert.Equal(SimpleCollection.TestElement, array.GetValue(0));
        }

        [Fact]
        public void EnumerateEmptyCollection()
        {
            var collection = new SimpleCollection();
            Assert.Empty(collection);
            var enumerator = collection.GetEnumerator();
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void EnumerateNonEmptyCollection()
        {
            var collection = new SimpleCollection();
            collection.TestBaseAdd(SimpleCollection.TestElement);
            Assert.NotEmpty(collection);
            var enumerator = collection.GetEnumerator();
            Assert.True(enumerator.MoveNext());
            Assert.Equal(SimpleCollection.TestElement, enumerator.Current);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void ElementInformationIsCollection()
        {
            Assert.True(new SimpleCollection().ElementInformation.IsCollection);
        }

        [Fact]
        public void ElementInformationIsNotLocked()
        {
            Assert.False(new SimpleCollection().ElementInformation.IsLocked);
        }

        [Fact]
        public void ElementInformationIsNotPresent()
        {
            Assert.False(new SimpleCollection().ElementInformation.IsPresent);
        }

        [Fact]
        public void ElementInformationPropertiesEmpty()
        {
            Assert.Empty(new SimpleCollection().ElementInformation.Properties);
        }

        [Fact]
        public void CurrentConfigurationIsNull()
        {
            Assert.Null(new SimpleCollection().CurrentConfiguration);
        }
    }
}
