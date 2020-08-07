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

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags);
            Assert.Empty(newCompilation.GetDiagnostics());

            // Check base functionality of found types.
            Assert.Equal(1, generator.FoundTypes.Count);
            Type myType = generator.FoundTypes["MyType"];
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received fields, properties and methods in created type.
            string[] expectedPropertyNames = { "PrivatePropertyInt", "PrivatePropertyString", "PublicPropertyInt", "PublicPropertyString",};
            string[] expectedFieldNames = { "PrivateChar", "PrivateDouble", "PublicChar", "PublicDouble" };
            string[] expectedMethodNames = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
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
            Assert.Equal(2, generator.FoundTypes.Count);
            Type myType = generator.FoundTypes["MyType"];
            Type notMyType = generator.FoundTypes["NotMyType"];

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PrivateChar", "PrivateDouble", "PublicChar", "PublicDouble" };
            string[] expectedPropertyNamesMyType = { "PrivatePropertyInt", "PrivatePropertyString", "PublicPropertyInt", "PublicPropertyString" };
            string[] expectedMethodNamesMyType = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods("MyType", ref generator, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.FoundTypes["NotMyType"].FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] expectedMethodNamesNotMyType = { "CreateConverter", "get_ConverterType" };
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
            Assert.Equal(2, generator.FoundTypes.Count);

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", generator.FoundTypes["MyType"].FullName);

            // Check for received fields, properties and methods for MyType.
            string[] expectedFieldNamesMyType = { "PrivateChar", "PrivateDouble", "PublicChar", "PublicDouble" };
            string[] expectedPropertyNamesMyType = { "PrivatePropertyInt", "PrivatePropertyString", "PublicPropertyInt", "PublicPropertyString" };
            string[] expectedMethodNamesMyType = { "get_PrivatePropertyInt", "get_PrivatePropertyString", "get_PublicPropertyInt", "get_PublicPropertyString", "MyMethod", "MySecondMethod", "set_PrivatePropertyInt", "set_PrivatePropertyString", "set_PublicPropertyInt", "set_PublicPropertyString", "UsePrivates" };
            CheckFieldsPropertiesMethods("MyType", ref generator, expectedFieldNamesMyType, expectedPropertyNamesMyType, expectedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.FoundTypes["NotMyType"].FullName);

            // Check for received fields, properties and methods for NotMyType.
            string[] expectedFieldNamesNotMyType = { };
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] expectedMethodNamesNotMyType = { "CreateConverter", "get_ConverterType" };
            CheckFieldsPropertiesMethods("NotMyType", ref generator, expectedFieldNamesNotMyType, expectedPropertyNamesNotMyType, expectedMethodNamesNotMyType );
        }

        private void CheckFieldsPropertiesMethods(string typeName, ref JsonSerializerSourceGenerator generator, string[] expectedFields, string[] expectedProperties, string[] expectedMethods)
        {
            string[] receivedFields = generator.FoundTypes[typeName].GetFields().Select(field => field.Name).OrderBy(s => s).ToArray();
            string[] receivedProperties = generator.FoundTypes[typeName].GetProperties().Select(property => property.Name).OrderBy(s => s).ToArray();
            string[] receivedMethods = generator.FoundTypes[typeName].GetMethods().Select(method => method.Name).OrderBy(s => s).ToArray();

            Assert.Equal(expectedFields, receivedFields);
            Assert.Equal(expectedProperties, receivedProperties);
            Assert.Equal(expectedMethods, receivedMethods);
        }
    }
}
