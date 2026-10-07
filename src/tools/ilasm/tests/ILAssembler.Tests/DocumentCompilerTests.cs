// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests
{
    public class DocumentCompilerTests
    {
        [Fact]
        public void ParserDiagnosticInLaterDocument_UsesDocumentPath_AndErrorTolerantStillEmitsImage()
        {
            var documents = ImmutableArray.Create(
                new SourceText("""
                    .assembly test { }
                    .class public auto ansi beforefieldinit ValidFromDoc1
                    {
                    }
                    """, "valid.il"),
                new SourceText("""
                    .class public auto ansi beforefieldinit Broken
                    {
                        .method public static void M(int32 int32 int32) cil managed
                        {
                            ret
                        }
                    }
                    """, "broken.il"));

            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });
            var parserDiagnostic = Assert.Single(diagnostics.Where(diagnostic => diagnostic.Id == "Parser"));

            Assert.Equal("broken.il", parserDiagnostic.Location.Source.Path);
            Assert.True(parserDiagnostic.Location.Span.Start >= 0);
            Assert.True(parserDiagnostic.Location.Span.Length > 0);
            Assert.NotNull(image);

            var imageBuilder = new BlobBuilder();
            image!.Serialize(imageBuilder);
            using var pe = new PEReader(imageBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var typeNames = reader.TypeDefinitions
                .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Name))
                .ToArray();

            Assert.Contains("ValidFromDoc1", typeNames);
        }

        [Fact]
        public void TruncatedDocument_DoesNotLeakScopesIntoTheNextDocument()
        {
            var documents = ImmutableArray.Create(
                new SourceText("""
                    .assembly extern mscorlib { }
                    .assembly test { }
                    .namespace Leaky
                    {
                        .class public auto ansi Unterminated
                        {
                            .method public static void M() cil managed
                            {
                                ret
                    """, "truncated.il"),
                new SourceText("""
                    .class public auto ansi AfterTruncation
                    {
                    }
                    """, "next.il"));

            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(image);

            var imageBuilder = new BlobBuilder();
            image!.Serialize(imageBuilder);
            using var pe = new PEReader(imageBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var afterTruncation = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "AfterTruncation");

            Assert.Equal(string.Empty, reader.GetString(afterTruncation.Namespace));
            Assert.True(afterTruncation.GetDeclaringType().IsNil);
        }

        [Theory]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Broken
            {
                .method public static void M(int32 int32 int32) cil managed
                {
                    ret
                }
            }
            .class public auto ansi Following
            {
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .namespace Broken
            {
                .class public auto ansi Nested
                {
                    .method public static void M(int32 int32 int32) cil managed
                    {
                        ret
                    }
                }
            }
            .class public auto ansi Following
            {
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .namespace
            {
            }
            .class public auto ansi Following
            {
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Broken extends
            {
            }
            .class public auto ansi Following
            {
            }
            """)]
        [InlineData("""
            .assembly extern mscorlib { }
            .assembly test { }
            .class public auto ansi Broken
            {
                .method public static void M() cil managed
                {
                    .try
                    {
                        {
                            nop nop nop int32
                        }
                    }
                    finally
                    {
                        endfinally
                    }
                    ret
                }
            }
            .class public auto ansi Following
            {
            }
            """)]
        public void SyntaxErrorInDeclaration_DoesNotLeakScopesIntoFollowingDeclarations(string source)
        {
            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(image);

            var imageBuilder = new BlobBuilder();
            image!.Serialize(imageBuilder);
            using var pe = new PEReader(imageBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var following = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Following");

            Assert.Equal(string.Empty, reader.GetString(following.Namespace));
            Assert.True(following.GetDeclaringType().IsNil);
        }

        [Fact]
        public void MalformedTopLevelClassHeader_DoesNotMaterializeBodyDeclarations()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Broken extends
                {
                    .field public static int32 LeakedField
                    .method public static void LeakedMethod() cil managed
                    {
                        ret
                    }
                    .property int32 LeakedProperty()
                    {
                    }
                    .event [mscorlib]System.EventHandler LeakedEvent
                    {
                    }
                    .class nested public auto ansi LeakedNested
                    {
                    }
                    .data SharedData = int32(1)
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                }
                .data SharedData = int32(2)
                .class public auto ansi Following
                {
                    .field public static int32 KeptData at SharedData
                }
                """;

            using PEReader pe = CompileErrorTolerantAndGetReader(source);
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinition moduleType =
                reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1));
            string[] typeNames = reader.TypeDefinitions
                .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Name))
                .ToArray();

            Assert.Empty(moduleType.GetFields());
            Assert.Empty(moduleType.GetMethods());
            Assert.DoesNotContain("LeakedNested", typeNames);
            Assert.Contains("Following", typeNames);
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.Property));
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.Event));
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.CustomAttribute));

            TypeDefinition following = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Following");
            FieldDefinition keptData =
                reader.GetFieldDefinition(Assert.Single(following.GetFields()));
            Assert.Equal(
                2,
                BinaryPrimitives.ReadInt32LittleEndian(
                    pe.GetSectionData(keptData.GetRelativeVirtualAddress())
                        .GetContent()
                        .AsSpan(0, sizeof(int))));
        }

        [Fact]
        public void MalformedNestedClassHeader_DoesNotMaterializeBodyDeclarations_AndFollowingMembersRemainInScope()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Outer
                {
                    .class nested public auto ansi Broken extends
                    {
                        .field public int32 LeakedField
                        .method public static void LeakedMethod() cil managed
                        {
                            ret
                        }
                        .property int32 LeakedProperty()
                        {
                        }
                        .event [mscorlib]System.EventHandler LeakedEvent
                        {
                        }
                        .class nested public auto ansi LeakedNested
                        {
                        }
                        .pack 4
                        .size 32
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    }
                    .field public int32 KeptField
                    .method public static void KeptMethod() cil managed
                    {
                        ret
                    }
                    .property int32 KeptProperty()
                    {
                    }
                    .event [mscorlib]System.EventHandler KeptEvent
                    {
                    }
                    .class nested public auto ansi KeptNested
                    {
                    }
                }
                .class public auto ansi Following
                {
                }
                """;

            using PEReader pe = CompileErrorTolerantAndGetReader(source);
            MetadataReader reader = pe.GetMetadataReader();
            TypeDefinitionHandle outerHandle = reader.TypeDefinitions
                .Single(handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Outer");
            TypeDefinition outer = reader.GetTypeDefinition(outerHandle);
            string[] typeNames = reader.TypeDefinitions
                .Select(handle => reader.GetString(reader.GetTypeDefinition(handle).Name))
                .ToArray();

            FieldDefinitionHandle fieldHandle = Assert.Single(outer.GetFields());
            Assert.Equal("KeptField", reader.GetString(reader.GetFieldDefinition(fieldHandle).Name));
            MethodDefinitionHandle methodHandle = Assert.Single(outer.GetMethods());
            Assert.Equal("KeptMethod", reader.GetString(reader.GetMethodDefinition(methodHandle).Name));
            PropertyDefinitionHandle propertyHandle = Assert.Single(outer.GetProperties());
            Assert.Equal("KeptProperty", reader.GetString(reader.GetPropertyDefinition(propertyHandle).Name));
            EventDefinitionHandle eventHandle = Assert.Single(outer.GetEvents());
            Assert.Equal("KeptEvent", reader.GetString(reader.GetEventDefinition(eventHandle).Name));
            Assert.Empty(reader.GetCustomAttributes(outerHandle));
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.ClassLayout));
            Assert.DoesNotContain("LeakedNested", typeNames);
            Assert.Contains("KeptNested", typeNames);

            TypeDefinition following = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .Single(type => reader.GetString(type.Name) == "Following");
            Assert.Equal(string.Empty, reader.GetString(following.Namespace));
            Assert.True(following.GetDeclaringType().IsNil);
        }

        [Fact]
        public void MalformedNestedNamespace_DoesNotMaterializeChildren_AndFollowingNamespacesRemainInScope()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .namespace Outer
                {
                    .namespace
                    {
                        .class public auto ansi Suppressed
                        {
                        }
                        .namespace Inner
                        {
                            .class public auto ansi DeeplySuppressed
                            {
                            }
                        }
                        .field public static int32 SuppressedGlobalField
                        .method public static void SuppressedGlobalMethod() cil managed
                        {
                            ret
                        }
                        .subsystem 2
                        .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    }
                    .class public auto ansi Kept
                    {
                    }
                    .namespace Sibling
                    {
                        .class public auto ansi SiblingType
                        {
                        }
                    }
                }
                .class public auto ansi Global
                {
                }
                """;

            using PEReader pe = CompileErrorTolerantAndGetReader(source);
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, TypeDefinition> types = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .ToDictionary(type => reader.GetString(type.Name));
            TypeDefinition moduleType =
                reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1));

            Assert.DoesNotContain("Suppressed", types.Keys);
            Assert.DoesNotContain("DeeplySuppressed", types.Keys);
            Assert.Empty(moduleType.GetFields());
            Assert.Empty(moduleType.GetMethods());
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.CustomAttribute));
            Assert.Equal(Subsystem.WindowsCui, pe.PEHeaders.PEHeader!.Subsystem);
            Assert.Equal("Outer", reader.GetString(types["Kept"].Namespace));
            Assert.Equal("Outer.Sibling", reader.GetString(types["SiblingType"].Namespace));
            Assert.Equal(string.Empty, reader.GetString(types["Global"].Namespace));
        }

        private static PEReader CompileErrorTolerantAndGetReader(string source)
        {
            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == "Parser");
            Assert.NotNull(image);

            var imageBuilder = new BlobBuilder();
            image!.Serialize(imageBuilder);
            return new PEReader(imageBuilder.ToImmutableArray());
        }
    }
}
