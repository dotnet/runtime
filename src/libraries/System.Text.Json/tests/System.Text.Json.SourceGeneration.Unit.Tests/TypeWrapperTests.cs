// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Xunit;

namespace System.Text.Json.SourceGeneration.UnitTests
{
    public class TypeWrapperTests
    {
        [Fact]
        public void MetadataLoadFilePathHandle()
        {
            // Create a MetadataReference from new code.
            string referencedSource = @"
            namespace ReferencedAssembly
            {
                public class ReferencedType
                {
                    public int ReferencedPublicInt;
                    public double ReferencedPublicDouble;     
                }
            }";

            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

            // Emit the image of the referenced assembly.
            byte[] referencedImage;
            using (MemoryStream ms = new MemoryStream())
            {
                var emitResult = referencedCompilation.Emit(ms);
                if (!emitResult.Success)
                {
                    throw new InvalidOperationException();
                }
                referencedImage = ms.ToArray();
            }

            string source = @"
            using System.Text.Json.Serialization;
            using ReferencedAssembly;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(HelloWorld.MyType))]
                [JsonSerializable(typeof(ReferencedAssembly.ReferencedType))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyType
                {
                    public void MyMethod() { }
                    public void MySecondMethod() { }
                }
            }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            // Compilation using the referenced image should fail if out MetadataLoadContext does not handle.
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(newCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            // Should find both types since compilation above was successful.
            Assert.Equal(2, generator.GetSerializableTypes().Count);
        }

        [Fact]
        public void CanGetAttributes()
        {
            string source = @"
            using System;
            using System.Text.Json.Serialization;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(HelloWorld.MyType))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyType
                {
                    [JsonInclude]
                    public double PublicDouble;

                    [JsonPropertyName(""PPublicDouble"")]
                    public char PublicChar;

                    [JsonIgnore]
                    private double PrivateDouble;

                    private char PrivateChar;

                    public MyType() {{ }}

                    [JsonConstructor]
                    public MyType(double d) {{ PrivateDouble = d; }}

                    [JsonPropertyName(""TestName"")]
                    public int PublicPropertyInt { get; set; }

                    [JsonExtensionData]
                    public string PublicPropertyString { get; set; }

                    [JsonIgnore]
                    private int PrivatePropertyInt { get; set; }

                    private string PrivatePropertyString { get; set; }

                    [Obsolete(""Testing"", true)]
                    public void MyMethod() { }

                    public void MySecondMethod() { }
                }
            }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation outCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Check base functionality of found types.
            Dictionary<string, Type> types = generator.GetSerializableTypes();
            Assert.Equal(1, types.Count);
            Type foundType = types.First().Value;

            Assert.Equal("HelloWorld.MyType", foundType.FullName);

            // Check for ConstructorInfoWrapper attribute usage.
            (string, string[])[] receivedCtorsWithAttributeNames = foundType.GetConstructors().Select(ctor => (ctor.DeclaringType.FullName, ctor.GetCustomAttributesData().Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray())).ToArray();
            Assert.Equal(
                new (string, string[])[] {
                    ("HelloWorld.MyType", new string[] { }),
                    ("HelloWorld.MyType", new string[] { "JsonConstructorAttribute" })
                },
                receivedCtorsWithAttributeNames
            );

            // Check for MethodInfoWrapper attribute usage.
            (string, string[])[] receivedMethodsWithAttributeNames = foundType.GetMethods().Select(method => (method.Name, method.GetCustomAttributesData().Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray())).Where(x => x.Item2.Any()).ToArray();
            Assert.Equal(
                new (string, string[])[] { ("MyMethod", new string[] { "ObsoleteAttribute" }) },
                receivedMethodsWithAttributeNames
            );

            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // Check for FieldInfoWrapper attribute usage.
            (string, string[])[] receivedFieldsWithAttributeNames = foundType.GetFields(bindingFlags).Select(field => (field.Name, field.GetCustomAttributesData().Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray())).Where(x => x.Item2.Any()).ToArray();
            Assert.Equal(
                new (string, string[])[] {
                    ("PublicDouble", new string[] { "JsonIncludeAttribute" }),
                    ("PublicChar", new string[] { "JsonPropertyNameAttribute" }),
                },
                receivedFieldsWithAttributeNames
            );

            // Check for PropertyInfoWrapper attribute usage.
            (string, string[])[] receivedPropertyWithAttributeNames  = foundType.GetProperties(bindingFlags).Select(property => (property.Name, property.GetCustomAttributesData().Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray())).Where(x => x.Item2.Any()).ToArray();
            Assert.Equal(
                new (string, string[])[] {
                    ("PublicPropertyInt", new string[] { "JsonPropertyNameAttribute" }),
                    ("PublicPropertyString", new string[] { "JsonExtensionDataAttribute" }),
                },
                receivedPropertyWithAttributeNames
            );

            // Check for MemberInfoWrapper attribute usage.
            (string, string[])[] receivedMembersWithAttributeNames = foundType.GetMembers().Select(member => (member.Name, member.GetCustomAttributesData().Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray())).Where(x => x.Item2.Any()).ToArray();
            Assert.Equal(
                new (string, string[])[] {
                    ("PublicDouble", new string[] { "JsonIncludeAttribute" }),
                    ("PublicChar", new string[] { "JsonPropertyNameAttribute" }),
                    ("PrivateDouble", new string[] { "JsonIgnoreAttribute" } ),
                    (".ctor", new string[] { "JsonConstructorAttribute" }),
                    ("PublicPropertyInt", new string[] { "JsonPropertyNameAttribute" }),
                    ("PublicPropertyString", new string[] { "JsonExtensionDataAttribute" }),
                    ("PrivatePropertyInt", new string[] { "JsonIgnoreAttribute" } ),
                    ("MyMethod", new string[] { "ObsoleteAttribute" }),
                },
                receivedMembersWithAttributeNames
            );
        }
    }
}
