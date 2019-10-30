// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
using System.Reflection;
using System.Xml;
using Xunit;

namespace System.ConfigurationTests
{
    public class ConfigurationElementTests
    {
        [Fact]
        public void HasNoConfiguration()
        {
            Assert.Null(new SimpleElement().CurrentConfiguration);
        }

        [Fact]
        public void HasNoContext()
        {
            // There should be no configuration record and thus, no context
            Assert.False(new SimpleElement().TestHasContext);
        }

        [Fact]
        public void EvaluationContextThrows()
        {
            // If we haven't associated a configuration record we should fail
            SimpleElement element = new SimpleElement();
            Assert.Throws<ConfigurationErrorsException>(() => element.TestEvaluationContext);
        }

        [Fact]
        public void TransformedNullTypeStringIsNull()
        {
            Assert.Null(new SimpleElement().TestGetTransformedTypeString(null));
        }

        [Fact]
        public void TransformedTypeStringIsOriginal()
        {
            string foo = "foo";

            // With no config record the transformed typename should be original
            Assert.Same(foo, new SimpleElement().TestGetTransformedTypeString(foo));
        }

        [Fact]
        public void TransformedNullAssemblyStringIsNull()
        {
            Assert.Null(new SimpleElement().TestGetTransformedAssemblyString(null));
        }

        [Fact]
        public void TransformedAssemblyStringIsOriginal()
        {
            string foo = "foo";

            // With no config record the transformed assembly name should be original
            Assert.Same(foo, new SimpleElement().TestGetTransformedAssemblyString(foo));
        }

        private static PropertyInfo LockType = typeof(ConfigurationLockCollection).GetProperty("LockType", BindingFlags.NonPublic | BindingFlags.Instance);

        [Fact]
        public void LockAttributesCollectionIsCorrectType()
        {
            var lockCollection = new SimpleElement().LockAttributes;
            Assert.Equal(0, lockCollection.Count);
            Assert.Equal("LockedAttributes", LockType.GetValue(lockCollection).ToString());
        }

        [Fact]
        public void LockAllAttributesExceptCollectionIsCorrectType()
        {
            var lockCollection = new SimpleElement().LockAllAttributesExcept;
            Assert.Equal(0, lockCollection.Count);
            Assert.Equal("LockedExceptionList", LockType.GetValue(lockCollection).ToString());
        }

        [Fact]
        public void LockElementsCollectionIsCorrectType()
        {
            var lockCollection = new SimpleElement().LockElements;
            Assert.Equal(0, lockCollection.Count);
            Assert.Equal("LockedElements", LockType.GetValue(lockCollection).ToString());
        }

        [Fact]
        public void LockAllElementsExceptCollectionIsCorrectType()
        {
            var lockCollection = new SimpleElement().LockAllElementsExcept;
            Assert.Equal(0, lockCollection.Count);
            Assert.Equal("LockedElementsExceptionList", LockType.GetValue(lockCollection).ToString());
        }

        [Fact]
        public void LockItemIsFalse()
        {
            Assert.False(new SimpleElement().LockItem);
        }

        [Fact]
        public void SetLockItem()
        {
            var element = new SimpleElement();
            element.LockItem = true;
            Assert.True(element.LockItem);
        }

        [Fact]
        public void ElementInformationIsNotCollection()
        {
            Assert.False(new SimpleElement().ElementInformation.IsCollection);
        }

        [Fact]
        public void ElementInformationIsNotPresent()
        {
            Assert.False(new SimpleElement().ElementInformation.IsPresent);
        }

        [Fact]
        public void ElementInformationPropertiesEmpty()
        {
            Assert.Empty(new SimpleElement().ElementInformation.Properties);
        }

        [Fact]
        public void CurrentConfigurationIsNull()
        {
            Assert.Null(new SimpleElement().CurrentConfiguration);
        }

        [Fact]
        public void OnDeserializeUnrecognizedAttributeReturnsFalse()
        {
            Assert.False(new SimpleElement().TestOnDeserializeUnrecognizedAttribute(null, null));
        }

        [Fact]
        public void OnDeserializeUnrecognizedElementReturnsFalse()
        {
            Assert.False(new SimpleElement().TestOnDeserializeUnrecognizedElement(null, null));
        }

        public class SimpleElement : ConfigurationElement
        {
            public ContextInformation TestEvaluationContext => EvaluationContext;
            public bool TestHasContext => HasContext;
            public string TestGetTransformedTypeString(string typeName) => GetTransformedTypeString(typeName);
            public string TestGetTransformedAssemblyString(string assemblyName) => GetTransformedAssemblyString(assemblyName);
            public bool TestOnDeserializeUnrecognizedAttribute(string name, string value) => OnDeserializeUnrecognizedAttribute(name, value);
            public bool TestOnDeserializeUnrecognizedElement(string elementName, XmlReader reader) => OnDeserializeUnrecognizedElement(elementName, reader);
        }
    }
}
