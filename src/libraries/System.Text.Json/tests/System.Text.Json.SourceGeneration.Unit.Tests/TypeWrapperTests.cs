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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/58226", TestPlatforms.Browser)]
    [SkipOnCoreClr("https://github.com/dotnet/runtime/issues/71962", ~RuntimeConfiguration.Release)]
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

        [Fact]
        public void VariousGenericSerializableTypesAreSupported()
        {
            string source = @"
            using System;
            using System.Collections.Generic;
            using System.Text.Json.Serialization;

            namespace HelloWorld
            {
                [JsonSerializable(typeof(Dictionary<string, string>))]
                [JsonSerializable(typeof(HelloWorld.MyClass.NestedGenericClass<string>))]
                [JsonSerializable(typeof(HelloWorld.MyGenericClass<string>.NestedClass))]
                [JsonSerializable(typeof(HelloWorld.MyGenericClass<string>.NestedGenericClass<int>))]
                internal partial class JsonContext : JsonSerializerContext
                {
                }

                public class MyClass
                {
                    public class NestedGenericClass<T>
                    {
                    }
                }

                public class MyGenericClass<T1>
                {
                    public class NestedClass
                    {
                    }
                    public class NestedGenericClass<T2>
                    {
                    }
                }
            }";

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGenerator generator = new JsonSourceGenerator();

            Compilation outCompilation = CompilationHelper.RunGenerators(compilation, out ImmutableArray<Diagnostic> generatorDiags, generator);

            // Make sure compilation was successful.
            Assert.Empty(generatorDiags.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(outCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            Dictionary<string, Type> types = generator.GetSerializableTypes();
            Assert.Equal(4, types.Count);

            // Check for generic class.
            Type originalType = typeof(Dictionary<string, string>);
            Type foundType = types[originalType.FullName];
            Assert.Equal(originalType, foundType, TestComparerForType.Instance);
            Assert.Equal(originalType.GetGenericArguments(), foundType.GetGenericArguments(), TestComparerForType.Instance);

            // Check for generic type definition.
            Type foundGenericTypeDefinition = foundType.GetGenericTypeDefinition();
            Type originalGenericTypeDefinition = originalType.GetGenericTypeDefinition();
            Assert.Equal(originalGenericTypeDefinition, foundGenericTypeDefinition, TestComparerForType.Instance);
            Assert.Equal(originalGenericTypeDefinition.GetGenericArguments(), foundGenericTypeDefinition.GetGenericArguments(), TestComparerForType.Instance);

            // Check for nested generic class.
            foundType = types.Values.Single(t => t.FullName.Contains("MyClass") && t.FullName.Contains("NestedGenericClass"));
            Assert.Equal("NestedGenericClass`1", foundType.Name);
            Assert.Equal($"HelloWorld.MyClass+NestedGenericClass`1[[{typeof(string).AssemblyQualifiedName}]]", foundType.FullName);
            Assert.True(foundType.IsGenericType);
            Assert.Equal(new[] { typeof(string) }, foundType.GetGenericArguments(), TestComparerForType.Instance);

            // Check for declaring type.
            foundType = foundType.DeclaringType;
            Assert.Equal("MyClass", foundType.Name);
            Assert.Equal("HelloWorld.MyClass", foundType.FullName);
            Assert.False(foundType.IsGenericType);

            // Check for class nested in generic class.
            foundType = types.Values.Single(t => t.FullName.Contains("MyGenericClass") && t.FullName.Contains("NestedClass"));
            Assert.Equal("NestedClass", foundType.Name);
            Assert.Equal($"HelloWorld.MyGenericClass`1+NestedClass[[{typeof(string).AssemblyQualifiedName}]]", foundType.FullName);
            Assert.True(foundType.IsGenericType);
            Assert.Equal(new[] { typeof(string) }, foundType.GetGenericArguments(), TestComparerForType.Instance);

            // Check for generic class nested in generic class.
            foundType = types.Values.Single(t => t.FullName.Contains("MyGenericClass") && t.FullName.Contains("NestedGenericClass"));
            Assert.Equal("NestedGenericClass`1", foundType.Name);
            Assert.Equal($"HelloWorld.MyGenericClass`1+NestedGenericClass`1[[{typeof(string).AssemblyQualifiedName}],[{typeof(int).AssemblyQualifiedName}]]", foundType.FullName);
            Assert.True(foundType.IsGenericType);
            Assert.Equal(new[] { typeof(string), typeof(int) }, foundType.GetGenericArguments(), TestComparerForType.Instance);

            // Check for generic declaring type.
            foundType = foundType.DeclaringType;
            Assert.Equal("MyGenericClass`1", foundType.Name);
            Assert.Equal("HelloWorld.MyGenericClass`1", foundType.FullName);
            Assert.True(foundType.IsGenericType);
            Assert.True(foundType.IsGenericTypeDefinition);
            Assert.Equal("T1", foundType.GetGenericArguments().Single().Name);
        }

        sealed class TestComparerForType : EqualityComparer<Type>
        {
            public static TestComparerForType Instance { get; } = new TestComparerForType();
            public override bool Equals(Type? x, Type? y)
            {
                if (x is null || y is null)
                {
                    return x == y;
                }
                return x.Name == y.Name &&
                    x.FullName == y.FullName &&
                    x.AssemblyQualifiedName == y.AssemblyQualifiedName &&
                    Instance.Equals(x.DeclaringType, y.DeclaringType) &&
                    x.IsGenericType == y.IsGenericType &&
                    x.IsGenericParameter == y.IsGenericParameter &&
                    x.IsGenericTypeDefinition == y.IsGenericTypeDefinition &&
                    x.ContainsGenericParameters == y.ContainsGenericParameters;
            }
            public override int GetHashCode(Type obj) => obj.Name.GetHashCode();
        }
    }
}
