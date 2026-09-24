// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Antlr4.Runtime;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class PreprocessorIntegrationTests
    {
        [Fact]
        public void NestedIncludes_EmitExpectedTypes()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                #include "level1.il"
                .class public auto ansi beforefieldinit AfterInclude extends [System.Runtime]System.Object
                {
                }
                """;

            using var pe = CompileWithIncludesAndGetReader(source, new Dictionary<string, string>
            {
                ["level1.il"] = """
                    #include "level2.il"
                    .class public auto ansi beforefieldinit Included.Namespace.IncludedType extends [System.Runtime]System.Object
                    {
                    }
                    """,
                ["level2.il"] = """
                    .class public auto ansi beforefieldinit Nested.DeepType extends [System.Runtime]System.Object
                    {
                    }
                    """
            });

            var reader = pe.GetMetadataReader();
            var typeNames = reader.TypeDefinitions
                .Select(handle =>
                {
                    var type = reader.GetTypeDefinition(handle);
                    return (Namespace: reader.GetString(type.Namespace), Name: reader.GetString(type.Name));
                })
                .ToArray();

            Assert.Contains(("Included.Namespace", "IncludedType"), typeNames);
            Assert.Contains(("Nested", "DeepType"), typeNames);
            Assert.Contains((string.Empty, "AfterInclude"), typeNames);
        }

        [Fact]
        public void ChainedIncludesEndingAtEof_ReturnToRootSource()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                #include "level1.il"
                .class public auto ansi beforefieldinit AfterInclude extends [System.Runtime]System.Object
                {
                }
                """;

            using PEReader pe = CompileWithIncludesAndGetReader(source, new Dictionary<string, string>
            {
                ["level1.il"] = """
                    #include "level2.il"
                    """,
                ["level2.il"] = """
                    .class public auto ansi beforefieldinit IncludedType extends [System.Runtime]System.Object
                    {
                    }
                    """,
            });

            MetadataReader reader = pe.GetMetadataReader();
            string[] typeNames = reader.TypeDefinitions
                .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Name))
                .ToArray();

            Assert.Contains("IncludedType", typeNames);
            Assert.Contains("AfterInclude", typeNames);
        }

        [Fact]
        public void IncludedUnterminatedConditional_PreservesParentConditionalState()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                #define ROOT
                #ifndef ROOT
                #else
                #include "child.il"
                #else
                .class public auto ansi beforefieldinit SecondElse extends [System.Runtime]System.Object { }
                #endif
                """;

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                new SourceText(source, "root.il"),
                path => new SourceText("""
                    #define CHILD
                    #ifdef CHILD
                    """, path),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            var preprocessorDiagnostics = diagnostics.Where(diagnostic => diagnostic.Id == "Preprocessor").ToArray();
            Assert.Single(preprocessorDiagnostics);
            Assert.Equal("root.il", preprocessorDiagnostics[0].Location.Source.Path);
            Assert.Contains("#else", preprocessorDiagnostics[0].Message);
            Assert.NotNull(result);
        }

        [Fact]
        public void IncludedUnterminatedConditional_ReportsEofDiagnostic()
        {
            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                new SourceText("""
                    .assembly extern System.Runtime { }
                    .assembly test { }
                    #include "child.il"
                    """, "root.il"),
                path => new SourceText("""
                    #define CHILD
                    #ifdef CHILD
                    """, path),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            var preprocessorDiagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "Preprocessor"));
            Assert.Equal("child.il", preprocessorDiagnostic.Location.Source.Path);
            Assert.NotNull(result);
        }

        [Fact]
        public void ConditionalCompilation_EmitsOnlyActiveTypes()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                #define OUTER
                #ifdef OUTER
                .class public auto ansi beforefieldinit OuterSelected extends [System.Runtime]System.Object { }
                #else
                .class public auto ansi beforefieldinit WrongOuter extends [System.Runtime]System.Object { }
                #endif
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var typeNames = reader.TypeDefinitions
                .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Name))
                .ToHashSet();

            Assert.Equal(2, typeNames.Count);
            Assert.Contains("OuterSelected", typeNames);
            Assert.DoesNotContain("WrongOuter", typeNames);
        }

        [Fact]
        public void IncludedMultiTokenMacro_EmitsExpectedFloatOperand()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                #include "constants.il"
                .class public auto ansi beforefieldinit Test extends [System.Runtime]System.Object
                {
                    .method public static float32 GetValue() cil managed
                    {
                        ldc.r4 NEG_INF
                        ret
                    }
                }
                """;

            using var pe = CompileWithIncludesAndGetReader(source, new Dictionary<string, string>
            {
                ["constants.il"] = """
                    #define NEG_INF "float32(0xFF800000)"
                    """
            });

            var reader = pe.GetMetadataReader();
            byte[] il = GetMethodBody(pe, reader, "GetValue");

            Assert.Equal(6, il.Length);
            Assert.True(float.IsNegativeInfinity(BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(il.AsSpan(1, 4)))));
        }

        private static PEReader CompileWithIncludesAndGetReader(string source, IReadOnlyDictionary<string, string> includes)
        {
            return DocumentCompilerTestHelpers.CompileAndGetReader(
                new SourceText(source, "root.il"),
                path =>
                {
                    Assert.True(includes.TryGetValue(path, out string? includedSource), $"Unexpected include path '{path}'");
                    return new SourceText(includedSource!, path);
                },
                new Options());
        }

        private static byte[] GetMethodBody(PEReader pe, MetadataReader reader, string methodName)
        {
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == methodName);

            Assert.True(method.RelativeVirtualAddress > 0, $"Method '{methodName}' should have a body");
            return pe.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()!;
        }
    }
}
