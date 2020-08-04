// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class GeneratorTests
    {
        [Fact]
        public void TypeDiscoveryPrimitivePOCO()
        {
            string source = @"
            using System.Text.Json.Serialization;

              namespace HelloWorld
              {
                [JsonSerializable]
                public class MyType {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }

                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double d = PrivateDouble;
                        char c = PrivateChar;
                    }
                }
              }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags);
            Assert.Empty(newCompilation.GetDiagnostics());

            // Check base functionality of found types.
            Assert.Equal(1, generator.foundTypes.Count);
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            // Check for received fields, properties and methods in created type.
            string[] expectedPropertyNames = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] expectedFieldNames = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] expectedMethodNames = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod", "UsePrivates" };
            CheckFieldsPropertiesMethods("MyType", ref generator, expectedFieldNames, expectedPropertyNames, expectedMethodNames);
        }

        [Fact]
        public void TypeDiscoveryPrimitiveTemporaryPOCO()
        {
            string source = @"
            using System.Text.Json.Serialization;

              namespace HelloWorld
              {
                [JsonSerializable]
                public class MyType {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }
                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double x = PrivateDouble;
                        string s = PrivateChar.ToString();
                    }
                }

                [JsonSerializable(typeof(JsonConverterAttribute))]
                public class NotMyType { }
              }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags);
            Assert.Empty(newCompilation.GetDiagnostics());

            // Check base functionality of found types.
            Assert.Equal(2, generator.foundTypes.Count);

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] expectedMethodNamesMyType = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod", "UsePrivates" };
            CheckFieldsPropertiesMethods("MyType", ref generator, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.foundTypes["NotMyType"].FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] expectedMethodNamesNotMyType = { "get_ConverterType", "CreateConverter" };
            CheckFieldsPropertiesMethods("NotMyType", ref generator, expectedFieldNamesNotMyType, expectedPropertyNamesNotMyType, expectedMethodNamesNotMyType);
        }

        [Fact]
        public void TypeDiscoveryWithRenamedAttribute()
        {
            string source = @"
            using System.Text.Json.Serialization;
            using @JsonSerializable = System.ObsoleteAttribute;
            using AliasedAttribute = System.Text.Json.Serialization.JsonSerializableAttribute;

              namespace HelloWorld
              {
                [AliasedAttribute]
                public class MyType {
                    public int PublicPropertyInt { get; set; }
                    public string PublicPropertyString { get; set; }
                    private int PrivatePropertyInt { get; set; }
                    private string PrivatePropertyString { get; set; }

                    public double PublicDouble;
                    public char PublicChar;
                    private double PrivateDouble;
                    private char PrivateChar;

                    public void MyMethod() { }
                    public void MySecondMethod() { }
                    public void UsePrivates()
                    {
                        PrivateDouble = 0;
                        PrivateChar = ' ';
                        double d = PrivateDouble;
                        char c = PrivateChar;
                    }
                }

                [AliasedAttribute(typeof(JsonConverterAttribute))]
                public class NotMyType { }

                [@JsonSerializable(""Testing"", true)]
                public class ShouldNotFind { }

              }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags);
            Assert.Empty(newCompilation.GetDiagnostics());

            // Check base functionality of found types.
            Assert.Equal(2, generator.foundTypes.Count);

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] expectedMethodNamesMyType = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod", "UsePrivates" };
            CheckFieldsPropertiesMethods("MyType", ref generator, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.foundTypes["NotMyType"].FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] expectedMethodNamesNotMyType = { "get_ConverterType", "CreateConverter" };
            CheckFieldsPropertiesMethods("NotMyType", ref generator, expectedFieldNamesNotMyType, expectedPropertyNamesNotMyType, expectedMethodNamesNotMyType );
        }

        private void CheckFieldsPropertiesMethods(string typeName, ref JsonSerializerSourceGenerator generator, string[] expectedFields, string[] expectedProperties, string[] expectedMethods)
        {
            string[] receivedFields = generator.foundTypes[typeName].GetFields().Select(field => field.Name).ToArray();
            string[] receivedProperties = generator.foundTypes[typeName].GetProperties().Select(property => property.Name).ToArray();
            string[] receivedMethods = generator.foundTypes[typeName].GetMethods().Select(method => method.Name).ToArray();

            Assert.Equal(expectedFields, receivedFields);
            Assert.Equal(expectedProperties, receivedProperties);
            Assert.Equal(expectedMethods, receivedMethods);
        }
    }
}
