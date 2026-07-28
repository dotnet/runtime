// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;

namespace ILAssembler.Tests
{
    public class ManifestResourceTests
    {
        [Fact]
        public void EmbeddedManifestResource_UsesResourceLocatorAliasAndEmitsResourceBytes()
        {
            string source = """
                .assembly test { }
                .mresource public Test.Resource as ResourceAlias
                {
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;
            byte[] expectedResourceBytes = [0x10, 0x20, 0x30, 0x40];
            List<string> requestedAliases = new();

            var (diagnostics, imageBytes) = CompileAndGetImageBytes(
                source,
                new Options(),
                resourceLocator: alias =>
                {
                    requestedAliases.Add(alias);
                    return expectedResourceBytes;
                });

            Assert.Empty(diagnostics);
            Assert.Equal(new[] { "ResourceAlias" }, requestedAliases);
            Assert.False(imageBytes.IsDefault);

            using var pe = new PEReader(imageBytes);
            var reader = pe.GetMetadataReader();

            var resource = reader.GetManifestResource(Assert.Single(reader.ManifestResources));
            Assert.Equal("Test.Resource", reader.GetString(resource.Name));
            Assert.Equal(ManifestResourceAttributes.Public, resource.Attributes);
            Assert.True(resource.Implementation.IsNil);
            Assert.Equal(0u, resource.Offset);
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.File));
            Assert.Equal(expectedResourceBytes, ReadEmbeddedResource(pe, resource));
        }

        [Fact]
        public void AssemblyBackedManifestResource_EmitsAssemblyImplementation()
        {
            string source = """
                .assembly extern ResourceAssembly { }
                .assembly test { }
                .mresource private External.Resource
                {
                    .assembly extern ResourceAssembly
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;
            int resourceLocatorCallCount = 0;

            var (diagnostics, imageBytes) = CompileAndGetImageBytes(
                source,
                new Options(),
                resourceLocator: _ =>
                {
                    resourceLocatorCallCount++;
                    return new byte[] { 0xFF };
                });

            Assert.Empty(diagnostics);
            Assert.Equal(0, resourceLocatorCallCount);
            Assert.False(imageBytes.IsDefault);

            using var pe = new PEReader(imageBytes);
            var reader = pe.GetMetadataReader();

            var resource = reader.GetManifestResource(Assert.Single(reader.ManifestResources));
            Assert.Equal("External.Resource", reader.GetString(resource.Name));
            Assert.Equal(ManifestResourceAttributes.Private, resource.Attributes);
            Assert.Equal(HandleKind.AssemblyReference, resource.Implementation.Kind);
            Assert.Equal("ResourceAssembly", reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)resource.Implementation).Name));
            Assert.Equal(0u, resource.Offset);
        }

        [Fact]
        public void MissingEmbeddedManifestResource_WithErrorTolerantOption_StillEmitsMetadataRow()
        {
            string source = """
                .assembly test { }
                .mresource public Missing.Resource as MissingAlias
                {
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var (strictDiagnostics, strictImageBytes) = CompileAndGetImageBytes(
                source,
                new Options(),
                resourceLocator: alias =>
                {
                    Assert.Equal("MissingAlias", alias);
                    return null!;
                });

            var strictDiagnostic = Assert.Single(strictDiagnostics.Where(d => d.Id == DiagnosticIds.FileNotFound));
            Assert.Equal(DiagnosticSeverity.Error, strictDiagnostic.Severity);
            Assert.Equal("test.il", strictDiagnostic.Location.Source.Path);
            Assert.True(strictImageBytes.IsDefault);

            var (tolerantDiagnostics, tolerantImageBytes) = CompileAndGetImageBytes(
                source,
                new Options { ErrorTolerant = true },
                resourceLocator: alias =>
                {
                    Assert.Equal("MissingAlias", alias);
                    return null!;
                });

            var tolerantDiagnostic = Assert.Single(tolerantDiagnostics.Where(d => d.Id == DiagnosticIds.FileNotFound));
            Assert.Equal(DiagnosticSeverity.Error, tolerantDiagnostic.Severity);
            Assert.False(tolerantImageBytes.IsDefault);

            using var pe = new PEReader(tolerantImageBytes);
            var reader = pe.GetMetadataReader();
            var resource = reader.GetManifestResource(Assert.Single(reader.ManifestResources));

            Assert.Equal("Missing.Resource", reader.GetString(resource.Name));
            Assert.True(resource.Implementation.IsNil);
            Assert.Equal(0u, resource.Offset);
            Assert.Equal(0, pe.PEHeaders.CorHeader!.ResourcesDirectory.Size);
        }

        [Fact]
        public void DeterministicOption_WithEmbeddedManifestResource_ProducesValidResourceImage()
        {
            string source = """
                .assembly test { }
                .mresource public Stable.Resource as StableAlias
                {
                }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;
            byte[] expectedResourceBytes = [0x01, 0x23, 0x45, 0x67];

            var (firstDiagnostics, firstImageBytes) = CompileAndGetImageBytes(
                source,
                new Options { Deterministic = true },
                resourceLocator: alias =>
                {
                    Assert.Equal("StableAlias", alias);
                    return expectedResourceBytes;
                });
            Assert.Empty(firstDiagnostics);
            Assert.False(firstImageBytes.IsDefault);

            using var firstPe = new PEReader(firstImageBytes);
            var firstReader = firstPe.GetMetadataReader();
            var firstResource = firstReader.GetManifestResource(Assert.Single(firstReader.ManifestResources));

            Assert.NotEqual(Guid.Empty, firstReader.GetGuid(firstReader.GetModuleDefinition().Mvid));
            Assert.Equal("Stable.Resource", firstReader.GetString(firstResource.Name));
            Assert.Equal(expectedResourceBytes, ReadEmbeddedResource(firstPe, firstResource));
        }

        [Fact]
        public void FileBackedManifestResource_EmitsFileImplementationOffsetAndCustomAttribute()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .file nometadata Data.resources .hash = (01 02 03 04)
                .mresource public File.Resource
                {
                    .file Data.resources at 12
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                }
                .class public auto ansi Test extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var resourceHandle = Assert.Single(reader.ManifestResources);
            var resource = reader.GetManifestResource(resourceHandle);

            Assert.Equal("File.Resource", reader.GetString(resource.Name));
            Assert.Equal(ManifestResourceAttributes.Public, resource.Attributes);
            Assert.Equal(12u, resource.Offset);
            Assert.Equal(HandleKind.AssemblyFile, resource.Implementation.Kind);

            var file = reader.GetAssemblyFile((AssemblyFileHandle)resource.Implementation);
            Assert.Equal("Data.resources", reader.GetString(file.Name));
            Assert.False(file.ContainsMetadata);
            Assert.Equal([0x01, 0x02, 0x03, 0x04], reader.GetBlobBytes(file.HashValue));

            var attribute = reader.GetCustomAttribute(Assert.Single(reader.GetCustomAttributes(resourceHandle)));
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(attribute.Value));
        }

        private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<byte> ImageBytes) CompileAndGetImageBytes(
            string source,
            Options options,
            Func<string, SourceText>? includedDocumentLoader = null,
            Func<string, byte[]>? resourceLocator = null)
        {
            return CompileAndGetImageBytes(
                ImmutableArray.Create(new SourceText(source, "test.il")),
                options,
                includedDocumentLoader,
                resourceLocator);
        }

        private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<byte> ImageBytes) CompileAndGetImageBytes(
            ImmutableArray<SourceText> documents,
            Options options,
            Func<string, SourceText>? includedDocumentLoader = null,
            Func<string, byte[]>? resourceLocator = null)
        {
            var compiler = new DocumentCompiler();
            var (diagnostics, image) = compiler.Compile(
                documents,
                includedDocumentLoader ?? (_ => throw new InvalidOperationException("Unexpected include")),
                resourceLocator ?? (_ => throw new InvalidOperationException("Unexpected resource")),
                options);

            if (image is null)
            {
                return (diagnostics, default);
            }

            var blobBuilder = new BlobBuilder();
            image.Serialize(blobBuilder);
            return (diagnostics, blobBuilder.ToImmutableArray());
        }

        private static byte[] ReadEmbeddedResource(PEReader pe, ManifestResource resource)
        {
            Assert.True(resource.Implementation.IsNil);

            int resourcesRva = pe.PEHeaders.CorHeader!.ResourcesDirectory.RelativeVirtualAddress;
            Assert.NotEqual(0, resourcesRva);

            var resourceBlob = pe.GetSectionData(resourcesRva).GetContent();
            int offset = checked((int)resource.Offset);
            int length = BinaryPrimitives.ReadInt32LittleEndian(resourceBlob.AsSpan(offset, sizeof(int)));
            return resourceBlob.AsSpan(offset + sizeof(int), length).ToArray();
        }
    }
}
