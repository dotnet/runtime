// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ILAssembler.Tests
{
    public class DocumentCompilerTests
    {
        [Fact]
        public void SingleTypeNoMembers()
        {
            string source = """
                .class public auto ansi sealed beforefieldinit Test
                {
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // One for the <Module> type, one for Test.
            Assert.Equal(2, reader.TypeDefinitions.Count);
            var typeDef = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(2));
            Assert.Equal(string.Empty, reader.GetString(typeDef.Namespace));
            Assert.Equal("Test", reader.GetString(typeDef.Name));
        }

        [Fact]
        public void TypeNotFound_ReportsError()
        {
            // Referencing a type that doesn't exist should report an error
            string source = """
                .class public auto ansi sealed beforefieldinit Test extends NonExistentType
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypeNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData("\"hello\"", "hello")]
        [InlineData("\"hello\\tworld\"", "hello\tworld")]
        [InlineData("\"hello\\nworld\"", "hello\nworld")]
        [InlineData("\"hello\\rworld\"", "hello\rworld")]
        [InlineData("\"\\\"quoted\\\"\"", "\"quoted\"")]
        [InlineData("\"back\\\\slash\"", "back\\slash")]
        [InlineData("\"null\\0char\"", "null\0char")]
        [InlineData("\"octal\\101\"", "octalA")]  // \101 = 65 = 'A'
        public void StringHelpers_ParsesEscapeSequences(string input, string expected)
        {
            var result = StringHelpers.ParseQuotedString(input);
            Assert.Equal(expected, result);
        }

        [Fact]
        public void Typedef_ClassNameAsAlias_ResolvesInFieldSignature()
        {
            // Define a typedef for a class and use it in a field type
            // Real-world usage: .typedef [System.Runtime]System.GC as GC
            string source = """
                .assembly extern System.Runtime { }
                .typedef [System.Runtime]System.Object as Obj

                .class public auto ansi beforefieldinit Test
                {
                    .field public Obj myField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the field and verify its signature references System.Object
            var typeDef = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "Test");
            var fields = typeDef.GetFields().Select(reader.GetFieldDefinition).ToArray();
            Assert.Single(fields);
            Assert.Equal("myField", reader.GetString(fields[0].Name));
        }

        [Fact]
        public void Typedef_ClassNameAsAlias_FieldCompiles()
        {
            // Test pattern from src/tests/JIT/Methodical/Coverage/copy_prop_byref_to_native_int.il
            // .typedef [System.Runtime]System.WeakReference as WeakRef
            string source = """
                .assembly extern System.Runtime { }
                .typedef [System.Runtime]System.String as Str

                .class public auto ansi beforefieldinit Test
                {
                    .field public Str myField
                }
                """;

            using var pe = CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Find the field and verify it compiled
            var typeDef = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(t => reader.GetString(t.Name) == "Test");
            var fields = typeDef.GetFields().Select(reader.GetFieldDefinition).ToArray();
            Assert.Single(fields);
            Assert.Equal("myField", reader.GetString(fields[0].Name));
        }

        [Fact]
        public void Typedef_NotFound_ReportsError()
        {
            // Using an undefined typedef alias should report an error
            string source = """
                .class public auto ansi beforefieldinit Test
                {
                    .field public UndefinedTypedef myField
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypedefNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void MultipleTypeNotFound_ReportsMultipleErrors()
        {
            // Multiple references to non-existent types should each report an error
            string source = """
                .class public auto ansi beforefieldinit Test extends NonExistentBase implements NonExistentInterface
                {
                }
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            Assert.Equal(2, diagnostics.Length);
            Assert.All(diagnostics, d =>
            {
                Assert.Equal(DiagnosticIds.TypeNotFound, d.Id);
                Assert.Equal(DiagnosticSeverity.Error, d.Severity);
            });
        }

        [Fact]
        public void ThisOutsideClass_ReportsError()
        {
            // Using .this outside of a class definition should report an error
            // Test at module level where there's no class context
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .this as MyThis
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ThisOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void BaseOutsideClass_ReportsError()
        {
            // Using .base outside of a class definition should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .base as MyBase
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.BaseOutsideClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void NesterOutsideNestedClass_ReportsError()
        {
            // Using .nester outside of a nested class should report an error
            string source = """
                .assembly extern System.Runtime { }
                .typedef class .nester as MyNester
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.NesterOutsideNestedClass, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void TypeParameterOutsideType_ReportsError()
        {
            // Using a named type parameter reference outside of a generic type should report an error
            // Note: !0 (by index) is allowed for compat, but !T (by name) requires a type context
            string source = """
                .assembly extern System.Runtime { }
                .typedef !T as MyTypeParam
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.TypeParameterOutsideType, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void ModuleNotFound_ReportsError()
        {
            // Referencing a module that doesn't exist
            string source = """
                .assembly extern System.Runtime { }
                .typedef [.module NonExistentModule]SomeType as MyType
                """;

            var diagnostics = CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ModuleNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        private static PEReader CompileAndGetReader(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, result) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            Assert.Empty(diagnostics);
            Assert.NotNull(result);
            var blobBuilder = new BlobBuilder();
            result!.Serialize(blobBuilder);
            return new PEReader(blobBuilder.ToImmutableArray());
        }

        private static ImmutableArray<Diagnostic> CompileAndGetDiagnostics(string source, Options options)
        {
            var sourceText = new SourceText(source, "test.il");
            var documentCompiler = new DocumentCompiler();
            var (diagnostics, _) = documentCompiler.Compile(sourceText, _ =>
            {
                Assert.Fail("Expected no includes");
                return default;
            }, _ => { Assert.Fail("Expected no resources"); return default; }, options);
            return diagnostics;
        }
    }
}
