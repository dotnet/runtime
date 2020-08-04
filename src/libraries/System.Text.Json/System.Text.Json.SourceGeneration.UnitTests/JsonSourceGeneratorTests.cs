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
            using System;
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
                }
              }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation outCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Check base functionality of found types.
            Assert.Equal(1, generator.FoundTypes.Count);
            Type myType = generator.FoundTypes["MyType"];
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNames = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] receivedPropertyNames = myType.GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNames, receivedPropertyNames);

            // Check for fields in created type.
            string[] expectedFieldNames = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] receivedFieldNames = myType.GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNames, receivedFieldNames);

            // Check for methods in created type.
            string[] expectedMethodNames = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod" };
            string[] receivedMethodNames = myType.GetMethods().Select(method => method.Name).ToArray();
            Assert.Equal(expectedMethodNames, receivedMethodNames);
        }

        [Fact]
        public void TypeDiscoveryPrimitiveTemporaryPOCO()
        {
            string source = @"
            using System;
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
                }

                [JsonSerializable(typeof(JsonConverterAttribute))]
                public class NotMyType { }

              }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation outCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Check base functionality of found types.
            Assert.Equal(2, generator.FoundTypes.Count);
            Type myType = generator.FoundTypes["MyType"];
            Type notMyType = generator.FoundTypes["NotMyType"];

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", myType.FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] receivedPropertyNamesMyType = myType.GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNamesMyType, receivedPropertyNamesMyType);

            // Check for fields in created type.
            string[] expectedFieldNamesMyType = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] receivedFieldNamesMyType = myType.GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNamesMyType, receivedFieldNamesMyType);

            // Check for methods in created type.
            string[] expectedMethodNamesMyType = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod" };
            string[] receivedMethodNamesMyType = myType.GetMethods().Select(method => method.Name).ToArray();
            Assert.Equal(expectedMethodNamesMyType, receivedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.FoundTypes["NotMyType"].FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] receivedPropertyNamesNotMyType = notMyType.GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNamesNotMyType, receivedPropertyNamesNotMyType);

            // Check for fields in created type.
            string[] expectedFieldNamesNotMyType = { };
            string[] receivedFieldNamesNotMyType = notMyType.GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNamesNotMyType, receivedFieldNamesNotMyType);

            // Check for methods in created type.
            string[] expectedMethodNamesNotMyType = { "get_ConverterType", "CreateConverter" };
            string[] receivedMethodNamesNotMyType = notMyType.GetMethods().Select(method => method.Name).ToArray();
            Assert.Equal(expectedMethodNamesNotMyType, receivedMethodNamesNotMyType);
        }
    }
}
