// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public class JsonSerializerSourceGeneratorTests
    {
        [JsonSerializable]
        public class SampleInternalTest
        {
            public char PublicCharField;
            private string PrivateStringField;
            public int PublicIntPropertyPublic { get; set; }
            public int PublicIntPropertyPrivateSet { get; private set; }
            public int PublicIntPropertyPrivateGet { private get; set; }

            public SampleInternalTest()
            {
                PublicCharField = 'a';
                PrivateStringField = "privateStringField";
            }

            public SampleInternalTest(char c, string s)
            {
                PublicCharField = c;
                PrivateStringField = s;
            }

            private SampleInternalTest(int i)
            {
                PublicIntPropertyPublic = i;
            }

            private void UseFields()
            {
                string use = PublicCharField.ToString() + PrivateStringField;
            }
        }

        [JsonSerializable(typeof(JsonConverterAttribute))]
        public class SampleExternalTest { }

        [Fact]
        public void TestGeneratedCode()
        {
            var internalTypeTest = new HelloWorldGenerated.SampleInternalTestClassInfo();
            var externalTypeTest = new HelloWorldGenerated.SampleExternalTestClassInfo();

            // Check base class names.
            Assert.Equal("SampleInternalTestClassInfo", internalTypeTest.GetClassName());
            Assert.Equal("SampleExternalTestClassInfo", externalTypeTest.GetClassName());

            // Public and private Ctors are visible.
            Assert.Equal(3, internalTypeTest.Ctors.Count);
            Assert.Equal(2, externalTypeTest.Ctors.Count);

            // Ctor params along with its types are visible.
            Dictionary<string, string> expectedCtorParamsInternal = new Dictionary<string, string> { { "c", "Char"}, { "s", "String" }, { "i", "Int32" } };
            Assert.Equal(expectedCtorParamsInternal, internalTypeTest.CtorParams);

            Dictionary<string, string> expectedCtorParamsExternal = new Dictionary<string, string> { { "converterType", "Type"} };
            Assert.Equal(expectedCtorParamsExternal, externalTypeTest.CtorParams);

            // Public and private methods are visible.
            List<string> expectedMethodsInternal = new List<string> { "get_PublicIntPropertyPublic", "set_PublicIntPropertyPublic", "get_PublicIntPropertyPrivateSet", "set_PublicIntPropertyPrivateSet", "get_PublicIntPropertyPrivateGet", "set_PublicIntPropertyPrivateGet", "UseFields" };
            Assert.Equal(expectedMethodsInternal, internalTypeTest.Methods);

            List<string> expectedMethodsExternal = new List<string> { "get_ConverterType", "CreateConverter" };
            Assert.Equal(expectedMethodsExternal, externalTypeTest.Methods);

            // Public and private fields are visible.
            Dictionary<string, string> expectedFieldsInternal = new Dictionary<string, string> { { "PublicCharField", "Char" }, { "PrivateStringField", "String" } };
            Assert.Equal(expectedFieldsInternal, internalTypeTest.Fields);

            Dictionary<string, string> expectedFieldsExternal = new Dictionary<string, string> { };
            Assert.Equal(expectedFieldsExternal, externalTypeTest.Fields);

            // Public properties are visible.
            Dictionary<string, string> expectedPropertiesInternal = new Dictionary<string, string> { { "PublicIntPropertyPublic", "Int32" }, { "PublicIntPropertyPrivateSet", "Int32" }, { "PublicIntPropertyPrivateGet", "Int32" } };
            Assert.Equal(expectedPropertiesInternal, internalTypeTest.Properties);

            Dictionary<string, string> expectedPropertiesExternal = new Dictionary<string, string> { { "ConverterType", "Type"} };
            Assert.Equal(expectedPropertiesExternal, externalTypeTest.Properties);
        }
    }
}
