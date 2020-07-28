// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                public class ReferencedType {
                    public int ReferencedPublicInt;     
                    public double ReferencedPublicDouble;     
                }
            }";

            // Compile the referenced assembly first.
            Compilation referencedCompilation = CompilationHelper.CreateCompilation(referencedSource);

            //// Emit the image of the referenced assembly.
            byte[] referencedImage = null;
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
                [JsonSerializable]
                public class MyType {
                    public void MyMethod() { }
                    public void MySecondMethod() { }
                }
                [JsonSerializable(typeof(ReferencedType))]
                public static partial class ExternType { }
              }";

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            // Compilation using the referenced image should fail if out MetadataLoadContext does not handle.
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation newCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags);
            Assert.Empty(newCompilation.GetDiagnostics());

            // Should find both types since compilation above was successful.
            Assert.Equal(2, generator.foundTypes.Count);
        }

        [Fact]
        public void CanGetAttributes()
        {
            string source = @"
            using System;
            using System.Text.Json.Serialization;

              namespace HelloWorld
              {
                [JsonSerializable]
                public class MyType {

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

            JsonSerializerSourceGenerator generator = new JsonSerializerSourceGenerator();

            Compilation outCompilation = CompilationHelper.RunGenerators(compilation, out var generatorDiags, generator);

            // Check base functionality of found types.
            Assert.Equal(1, generator.foundTypes.Count);
            Assert.Equal("HelloWorld.MyType", generator.foundTypes["MyType"].FullName);

            Type foundType = generator.foundTypes.First().Value;

            // Check for ConstructorInfoWrapper attribute usage.
            string[] receivedCtorAttributeNames = foundType.GetConstructors().SelectMany(ctor => ctor.GetCustomAttributesData()).Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray();
            Assert.Equal(receivedCtorAttributeNames, new string[] { "JsonConstructorAttribute" });

            // Check for MethodInfoWrapper attribute usage.
            string[] receivedMethodAttributeNames = foundType.GetMethods().SelectMany(method => method.GetCustomAttributesData()).Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray();
            Assert.Equal(receivedMethodAttributeNames, new string[] { "ObsoleteAttribute" });

            // Check for FieldInfoWrapper attribute usage.
            string[] receivedFieldAttributeNames = foundType.GetFields().SelectMany(field => field.GetCustomAttributesData()).Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray();
            Assert.Equal(receivedFieldAttributeNames, new string[] { "JsonIncludeAttribute", "JsonPropertyNameAttribute", "JsonIgnoreAttribute" });

            // Check for PropertyInfoWrapper attribute usage.
            string[] receivedPropertyAttributeNames = foundType.GetProperties().SelectMany(property => property.GetCustomAttributesData()).Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray();
            Assert.Equal(receivedPropertyAttributeNames, new string[] { "JsonPropertyNameAttribute", "JsonExtensionDataAttribute", "JsonIgnoreAttribute" });

            // Check for MemberInfoWrapper attribute usage.
            string[] receivedMemberAttributeNames = foundType.GetMembers().SelectMany(member => member.GetCustomAttributesData()).Cast<CustomAttributeData>().Select(attributeData => attributeData.AttributeType.Name).ToArray();
            Assert.Equal(receivedMemberAttributeNames, new string[] { "JsonIncludeAttribute", "JsonPropertyNameAttribute", "JsonIgnoreAttribute", "JsonConstructorAttribute", "JsonPropertyNameAttribute", "JsonExtensionDataAttribute", "JsonIgnoreAttribute", "ObsoleteAttribute" });
        }
    }
}
