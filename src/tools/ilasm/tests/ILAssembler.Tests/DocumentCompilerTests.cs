// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
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
        public void SyntaxErrorInDeclarationBody_DoesNotLeakScopesIntoFollowingDeclarations(string source)
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
    }
}
