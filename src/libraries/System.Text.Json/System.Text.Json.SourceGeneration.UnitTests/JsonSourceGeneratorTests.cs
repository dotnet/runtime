// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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

            Compilation compilation = CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation outCompilation = RunGenerators(compilation, out var generatorDiags, generator);

            // Check base functionality of found types.
            Assert.Equal(1, generator.foundTypes.Count);
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNames = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] receivedPropertyNames = generator.foundTypes["MyType"].GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNames, receivedPropertyNames);

            // Check for fields in created type.
            string[] expectedFieldNames = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] receivedFieldNames = generator.foundTypes["MyType"].GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNames, receivedFieldNames);

            // Check for methods in created type.
            string[] expectedMethodNames = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod" };
            string[] receivedMethodNames = generator.foundTypes["MyType"].GetMethods().Select(method => method.Name).ToArray();
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

            Compilation compilation = CreateCompilation(source);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation outCompilation = RunGenerators(compilation, out var generatorDiags, generator);

            // Check base functionality of found types.
            Assert.Equal(2, generator.foundTypes.Count);

            // Check for MyType.
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNamesMyType = { "PublicPropertyInt", "PublicPropertyString", "PrivatePropertyInt", "PrivatePropertyString" };
            string[] receivedPropertyNamesMyType = generator.foundTypes["MyType"].GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNamesMyType, receivedPropertyNamesMyType);

            // Check for fields in created type.
            string[] expectedFieldNamesMyType = { "PublicDouble", "PublicChar", "PrivateDouble", "PrivateChar" };
            string[] receivedFieldNamesMyType = generator.foundTypes["MyType"].GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNamesMyType, receivedFieldNamesMyType);

            // Check for methods in created type.
            string[] expectedMethodNamesMyType = { "get_PublicPropertyInt", "set_PublicPropertyInt", "get_PublicPropertyString", "set_PublicPropertyString", "get_PrivatePropertyInt", "set_PrivatePropertyInt", "get_PrivatePropertyString", "set_PrivatePropertyString", "MyMethod", "MySecondMethod" };
            string[] receivedMethodNamesMyType = generator.foundTypes["MyType"].GetMethods().Select(method => method.Name).ToArray();
            Assert.Equal(expectedMethodNamesMyType, receivedMethodNamesMyType);

            // Check for NotMyType.
            Assert.Equal("System.Text.Json.Serialization.JsonConverterAttribute", generator.foundTypes["NotMyType"].FullName);

            // Check for received properties in created type.
            string[] expectedPropertyNamesNotMyType = { "ConverterType" };
            string[] receivedPropertyNamesNotMyType = generator.foundTypes["NotMyType"].GetProperties().Select(property => property.Name).ToArray();
            Assert.Equal(expectedPropertyNamesNotMyType, receivedPropertyNamesNotMyType);

            // Check for fields in created type.
            string[] expectedFieldNamesNotMyType = { };
            string[] receivedFieldNamesNotMyType = generator.foundTypes["NotMyType"].GetFields().Select(field => field.Name).ToArray();
            Assert.Equal(expectedFieldNamesNotMyType, receivedFieldNamesNotMyType);

            // Check for methods in created type.
            string[] expectedMethodNamesNotMyType = { "get_ConverterType", "CreateConverter" };
            string[] receivedMethodNamesNotMyType = generator.foundTypes["NotMyType"].GetMethods().Select(method => method.Name).ToArray();
            Assert.Equal(expectedMethodNamesNotMyType, receivedMethodNamesNotMyType);
        }

        private Compilation CreateCompilation(string source)
        {
            // Bypass System.Runtime error.
            Assembly systemRuntimeAssembly = Assembly.Load("System.Runtime, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
            string systemRuntimeAssemblyPath = systemRuntimeAssembly.Location;

            MetadataReference[] references = new MetadataReference[] {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonSerializableAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JsonSerializerOptions).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Type).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(KeyValuePair).Assembly.Location),
                MetadataReference.CreateFromFile(systemRuntimeAssemblyPath),
            };

            return CSharpCompilation.Create(
                "TestAssembly",
                syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );
        }

        private GeneratorDriver CreateDriver(Compilation compilation, params ISourceGenerator[] generators)
            => new CSharpGeneratorDriver(
                 new CSharpParseOptions(kind: SourceCodeKind.Regular, documentationMode: DocumentationMode.Parse),
                ImmutableArray.Create(generators),
                ImmutableArray<AdditionalText>.Empty);

        private Compilation RunGenerators(Compilation compilation, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CreateDriver(compilation, generators).RunFullGeneration(compilation, out Compilation outCompilation, out diagnostics);
            return outCompilation;
        }
    }
}
