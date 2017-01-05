// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Configuration;
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

        public class SimpleElement : ConfigurationElement
        {
            public ContextInformation TestEvaluationContext => EvaluationContext;

            public bool TestHasContext => HasContext;

            public string TestGetTransformedTypeString(string typeName)
            {
                return GetTransformedTypeString(typeName);
            }

            public string TestGetTransformedAssemblyString(string assemblyName)
            {
                return GetTransformedAssemblyString(assemblyName);
            }
        }

    }
}
