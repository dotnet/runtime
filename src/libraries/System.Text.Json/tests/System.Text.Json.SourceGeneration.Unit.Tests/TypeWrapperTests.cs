// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            string referencedSource = """
                namespace ReferencedAssembly
                {
                    public class ReferencedType
                    {
                        public int ReferencedPublicInt;
                        public double ReferencedPublicDouble;     
                    }
                }
                """;

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

            string source = """
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
                }
                """;

            MetadataReference[] additionalReferences = { MetadataReference.CreateFromImage(referencedImage) };

            // Compilation using the referenced image should fail if out MetadataLoadContext does not handle.
            Compilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            Assert.Empty(result.Diagnostics.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(result.NewCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            // Should find both types since compilation above was successful.
            Assert.Equal(4, result.AllGeneratedTypes.Count());
        }

        [Fact]
        public void VariousGenericSerializableTypesAreSupported()
        {
            string source = """
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
                }
                """;

            Compilation compilation = CompilationHelper.CreateCompilation(source);

            JsonSourceGeneratorResult result = CompilationHelper.RunJsonSourceGenerator(compilation);

            // Make sure compilation was successful.
            Assert.Empty(result.Diagnostics.Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));
            Assert.Empty(result.NewCompilation.GetDiagnostics().Where(diag => diag.Severity.Equals(DiagnosticSeverity.Error)));

            Assert.Equal(5, result.AllGeneratedTypes.Count());
            result.AssertContainsType("global::System.Collections.Generic.Dictionary<global::System.String, global::System.String>");
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
