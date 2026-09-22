// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class CompilerOptionsTests
    {
        [Fact]
        public void AssemblyNameMetadataVersionAndModuleNameOptions_AreApplied()
        {
            string source = """
                .assembly SourceAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                AssemblyName = "Overridden.Assembly",
                MetadataVersion = "vTestMetadata",
                OutputFileName = "override.dll"
            });
            var reader = pe.GetMetadataReader();

            Assert.Equal("Overridden.Assembly", reader.GetString(reader.GetAssemblyDefinition().Name));
            Assert.Equal("vTestMetadata", reader.MetadataVersion);
            Assert.Equal("override.dll", reader.GetString(reader.GetModuleDefinition().Name));
        }

        [Fact]
        public void PeHeaderAndCorFlagsOptions_AreApplied()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                Machine = Machine.Amd64,
                FileAlignment = 0x200,
                ImageBase = 0x140000000,
                Subsystem = Subsystem.WindowsCui,
                SubsystemVersion = (6, 1),
                StackReserve = 0x200000,
                CorFlags = CorFlags.ILOnly
            });

            var peHeader = pe.PEHeaders.PEHeader!;
            Assert.Equal(0x200, peHeader.FileAlignment);
            Assert.Equal((ulong)0x140000000, peHeader.ImageBase);
            Assert.Equal(Subsystem.WindowsCui, peHeader.Subsystem);
            Assert.Equal((ushort)6, peHeader.MajorSubsystemVersion);
            Assert.Equal((ushort)1, peHeader.MinorSubsystemVersion);
            Assert.Equal((ulong)0x200000, peHeader.SizeOfStackReserve);
            Assert.Equal(Machine.Amd64, pe.PEHeaders.CoffHeader.Machine);
            Assert.Equal(CorFlags.ILOnly, pe.PEHeaders.CorHeader!.Flags);
        }

        [Fact]
        public void Prefer32BitOption_AddsPreferredCorFlag()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                CorFlags = CorFlags.ILOnly | CorFlags.Requires32Bit,
                Prefer32Bit = true
            });

            CorFlags flags = pe.PEHeaders.CorHeader!.Flags;
            Assert.True(flags.HasFlag(CorFlags.Requires32Bit));
            Assert.True(flags.HasFlag(CorFlags.Prefers32Bit));
        }

        [Fact]
        public void NoAutoInheritOption_SuppressesImplicitObjectBaseType()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { NoAutoInherit = true });
            var reader = pe.GetMetadataReader();
            var typeDef = reader.TypeDefinitions
                .Select(reader.GetTypeDefinition)
                .First(type => reader.GetString(type.Name) == "Test");

            Assert.True(typeDef.BaseType.IsNil);
        }

        [Fact]
        public void DebugModeOpt_EmitsDebuggableAttributeBlob()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                Debug = true,
                DebugMode = DebugMode.Opt
            });
            var reader = pe.GetMetadataReader();

            var assemblyAttributes = reader.GetAssemblyDefinition().GetCustomAttributes().ToArray();
            var attributeHandle = Assert.Single(assemblyAttributes);
            var attribute = reader.GetCustomAttribute(attributeHandle);
            var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            var attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);

            Assert.Equal(".ctor", reader.GetString(constructor.Name));
            Assert.Equal("DebuggableAttribute", reader.GetString(attributeType.Name));
            AssertDebuggableAttribute(attribute, expectedMode: 0x03);
        }

        [Fact]
        public void DllCharacteristicsOptions_EmitExpectedPeBits()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                Machine = Machine.Amd64,
                AppContainer = true,
                HighEntropyVA = true,
                StripReloc = true
            });

            DllCharacteristics characteristics = pe.PEHeaders.PEHeader!.DllCharacteristics;
            Assert.True(characteristics.HasFlag(DllCharacteristics.AppContainer));
            Assert.True(characteristics.HasFlag(DllCharacteristics.HighEntropyVirtualAddressSpace));
            Assert.True(characteristics.HasFlag(DllCharacteristics.NxCompatible));
            Assert.True(characteristics.HasFlag(DllCharacteristics.NoSeh));
            Assert.True(characteristics.HasFlag(DllCharacteristics.TerminalServerAware));
            Assert.False(characteristics.HasFlag(DllCharacteristics.DynamicBase));
        }

        [Fact]
        public void DeterministicOption_ProducesValidImageAndMvid()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void Main() cil managed
                    {
                        .entrypoint
                        ret
                    }
                }
                """;

            var image = DocumentCompilerTestHelpers.CompileAndGetImageBytes(source, new Options { Deterministic = true });

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            Assert.NotEqual(Guid.Empty, reader.GetGuid(reader.GetModuleDefinition().Mvid));
            Assert.NotEqual(0, pe.PEHeaders.PEHeader!.SizeOfImage);
        }

        [Fact]
        public void PdbOption_EmitsEmbeddedPortablePdbWithoutLineDirectives()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                    .method public static void M() cil managed
                    {
                        nop
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Pdb = true });

            var debugDirectory = pe.ReadDebugDirectory();
            var embeddedPdbEntry = debugDirectory.Single(entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
            var pdbReader = pdbProvider.GetMetadataReader();

            Assert.Empty(pdbReader.Documents);
            Assert.NotEmpty(pdbReader.MethodDebugInformation);
        }

        [Fact]
        public void DebugModeImpl_EmitsDebuggableAttributeBlob()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                Debug = true,
                DebugMode = DebugMode.Impl
            });
            var reader = pe.GetMetadataReader();

            var assemblyAttributes = reader.GetAssemblyDefinition().GetCustomAttributes().ToArray();
            var attributeHandle = Assert.Single(assemblyAttributes);
            var attribute = reader.GetCustomAttribute(attributeHandle);
            var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            var attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);

            Assert.Equal(".ctor", reader.GetString(constructor.Name));
            Assert.Equal("DebuggableAttribute", reader.GetString(attributeType.Name));
            AssertDebuggableAttribute(attribute, expectedMode: 0x103);
        }

        [Fact]
        public void DebugOption_WithoutExplicitMode_EmitsDefaultDebuggableAttributeBlob()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
            {
                Debug = true
            });
            var reader = pe.GetMetadataReader();

            var assemblyAttributes = reader.GetAssemblyDefinition().GetCustomAttributes().ToArray();
            var attributeHandle = Assert.Single(assemblyAttributes);
            var attribute = reader.GetCustomAttribute(attributeHandle);
            var constructor = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
            var attributeType = reader.GetTypeReference((TypeReferenceHandle)constructor.Parent);

            Assert.Equal(".ctor", reader.GetString(constructor.Name));
            Assert.Equal("DebuggableAttribute", reader.GetString(attributeType.Name));
            AssertDebuggableAttribute(attribute, expectedMode: 0x101);
        }

        [Fact]
        public void ValidKeyFile_EmbedsPublicKeyAndSetsAssemblyFlag()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;
            string tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDirectory);
            string keyFile = Path.Combine(tempDirectory, "test.snk");
            byte[] expectedKeyBytes = [0x06, 0x02, 0x23, 0x29, 0x47, 0x6B, 0x8D, 0xAF];

            try
            {
                File.WriteAllBytes(keyFile, expectedKeyBytes);

                using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options
                {
                    KeyFile = keyFile
                });
                var reader = pe.GetMetadataReader();
                var assemblyDefinition = reader.GetAssemblyDefinition();

                Assert.True(assemblyDefinition.Flags.HasFlag(AssemblyFlags.PublicKey));
                Assert.Equal(expectedKeyBytes, reader.GetBlobBytes(assemblyDefinition.PublicKey));
            }
            finally
            {
                if (File.Exists(keyFile))
                {
                    File.Delete(keyFile);
                }

                if (Directory.Exists(tempDirectory))
                {
                    Directory.Delete(tempDirectory);
                }
            }
        }

        [Fact]
        public void InvalidKeyFile_WithErrorTolerantOption_EmitsAssemblyAndReportsDiagnostic()
        {
            string source = """
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;
            string missingKeyFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.snk");
            var sourceText = new SourceText(source, "test.il");
            var compiler = new DocumentCompiler();

            var (diagnostics, image) = compiler.Compile(
                sourceText,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options
                {
                    ErrorTolerant = true,
                    KeyFile = missingKeyFile
                });

            var diagnostic = Assert.Single(diagnostics, d => d.Id == DiagnosticIds.KeyFileError);
            Assert.Contains(missingKeyFile, diagnostic.Message);
            Assert.NotNull(image);

            var blobBuilder = new BlobBuilder();
            image!.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            var assemblyDefinition = reader.GetAssemblyDefinition();

            Assert.False(assemblyDefinition.Flags.HasFlag(AssemblyFlags.PublicKey));
            Assert.True(assemblyDefinition.PublicKey.IsNil);
        }

        [Fact]
        public void ErrorTolerantOption_ReturnsImageForStructuralMetadataError()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern public MissingType
                {
                    .assembly extern MissingAssembly
                }
                """;
            var sourceText = new SourceText(source, "test.il");
            var compiler = new DocumentCompiler();

            var (strictDiagnostics, strictImage) = compiler.Compile(
                sourceText,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options());
            var strictDiagnostic = Assert.Single(strictDiagnostics, d => d.Id == DiagnosticIds.AssemblyNotFound);
            Assert.Equal(DiagnosticSeverity.Error, strictDiagnostic.Severity);
            Assert.Null(strictImage);

            var (tolerantDiagnostics, tolerantImage) = compiler.Compile(
                sourceText,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });
            var tolerantDiagnostic = Assert.Single(tolerantDiagnostics, d => d.Id == DiagnosticIds.AssemblyNotFound);
            Assert.Equal(DiagnosticSeverity.Error, tolerantDiagnostic.Severity);
            Assert.NotNull(tolerantImage);

            var blobBuilder = new BlobBuilder();
            tolerantImage!.Serialize(blobBuilder);
            using var pe = new PEReader(blobBuilder.ToImmutableArray());
            var reader = pe.GetMetadataReader();
            Assert.True(reader.TypeDefinitions.Count >= 1);
        }

        [Fact]
        public void PeDirectives_EmitSpecifiedHeaderValues()
        {
            string source = """
                .assembly test { }
                .subsystem 0x0002
                .corflags 0x00000003
                .file alignment 0x00000400
                .imagebase 0x10000000
                .stackreserve 0x00200000
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var peHeader = pe.PEHeaders.PEHeader!;
            var corHeader = pe.PEHeaders.CorHeader!;

            Assert.Equal(Subsystem.WindowsGui, peHeader.Subsystem);
            Assert.Equal(0x400, peHeader.FileAlignment);
            Assert.Equal(0x10000000UL, peHeader.ImageBase);
            Assert.Equal(0x00200000UL, peHeader.SizeOfStackReserve);
            Assert.Equal(CorFlags.ILOnly | CorFlags.Requires32Bit, corHeader.Flags);
        }

        private static void AssertDebuggableAttribute(CustomAttribute attribute, int expectedMode)
        {
            CustomAttributeValue<string> value =
                attribute.DecodeValue(DocumentCompilerTestHelpers.Decoder);
            var argument = Assert.Single(value.FixedArguments);
            Assert.Equal(expectedMode, argument.Value);
            Assert.Empty(value.NamedArguments);
        }
    }
}
