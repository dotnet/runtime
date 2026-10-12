// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class CompilerOptionsTests
    {
        [Theory]
        [InlineData("System.SerializableAttribute", "( 01 00 00 00 )")]
        [InlineData("System.SerializableAttribute", "( FF FF )")]
        [InlineData("System.Security.SuppressUnmanagedCodeSecurityAttribute", "( 01 00 00 00 )")]
        [InlineData("System.Security.DynamicSecurityMethodAttribute", "( 01 00 00 00 )")]
        public void Pseudoattributes_DefaultPreservesCustomAttribute(string attributeType, string blob)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .custom instance void [mscorlib]{{attributeType}}::.ctor() = {{blob}}
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var type = reader.GetTypeDefinition(reader.TypeDefinitions.Single(
                handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test"));
            var attribute = reader.GetCustomAttribute(Assert.Single(type.GetCustomAttributes()));

#pragma warning disable SYSLIB0050 // Inspect the metadata serialization flag.
            Assert.Equal(default, type.Attributes & (TypeAttributes.Serializable | TypeAttributes.HasSecurity));
#pragma warning restore SYSLIB0050
            Assert.Equal(Convert.FromHexString(blob.Replace("(", "").Replace(")", "").Replace(" ", "")),
                reader.GetBlobBytes(attribute.Value));
        }

        private const string PortablePdbSource = """
            .assembly extern System.Runtime { }
            .assembly test { }
            .class public auto ansi beforefieldinit Test
            {
                .method public static void M() cil managed
                {
                    .line 10 'test.cs'
                    nop
                    ret
                }
            }
            """;

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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FoldOption_SharesOnlyIdenticalMethodBodies(bool fold)
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static void First() cil managed { ret }
                    .method public static void Second() cil managed { ret }
                    .method public static void DifferentCode() cil managed { nop ret }
                    .method public static void DifferentStack() cil managed
                    {
                        .maxstack 1
                        ret
                    }
                    .method public static void FirstLocals() cil managed
                    {
                        .locals init (int32 V_0)
                        ret
                    }
                    .method public static void SecondLocals() cil managed
                    {
                        .locals init (int32 V_0)
                        ret
                    }
                    .method public static void DifferentLocals() cil managed
                    {
                        .locals init (int64 V_0)
                        ret
                    }
                    .method public static void FirstBranch() cil managed
                    {
                        br.s Done
                        Done: ret
                    }
                    .method public static void SecondBranch() cil managed
                    {
                        br.s Done
                        Done: ret
                    }
                    .method public static void FirstFinally() cil managed
                    {
                        .try { leave.s Done }
                        finally { endfinally }
                        Done: ret
                    }
                    .method public static void SecondFinally() cil managed
                    {
                        .try { leave.s Done }
                        finally { endfinally }
                        Done: ret
                    }
                    .method public static void DifferentHandler() cil managed
                    {
                        .try { leave.s Done }
                        fault { endfinally }
                        Done: ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Fold = fold });
            var reader = pe.GetMetadataReader();
            var methods = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .ToDictionary(method => reader.GetString(method.Name), method => method.RelativeVirtualAddress);

            Assert.NotEqual(0, methods["First"]);
            Assert.Equal(fold, methods["First"] == methods["Second"]);
            Assert.Equal(fold, methods["FirstLocals"] == methods["SecondLocals"]);
            Assert.Equal(fold, methods["FirstBranch"] == methods["SecondBranch"]);
            Assert.Equal(fold, methods["FirstFinally"] == methods["SecondFinally"]);
            Assert.NotEqual(methods["First"], methods["DifferentCode"]);
            Assert.NotEqual(methods["First"], methods["DifferentStack"]);
            Assert.NotEqual(methods["First"], methods["FirstLocals"]);
            Assert.NotEqual(methods["FirstLocals"], methods["DifferentLocals"]);
            Assert.NotEqual(methods["FirstFinally"], methods["DifferentHandler"]);
            Assert.Equal(new byte[] { 0x2a }, pe.GetMethodBody(methods["Second"]).GetILBytes());
            Assert.False(pe.GetMethodBody(methods["SecondLocals"]).LocalSignature.IsNil);
            Assert.Single(pe.GetMethodBody(methods["SecondFinally"]).ExceptionRegions);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void FoldOption_PreservesMalformedZeroCodeExceptionRegions(bool fold)
        {
            const string source = """
                .assembly test {}
                .class public Test
                {
                    .method public static void Prefix() cil managed { ret }
                    .method public static void First() cil managed
                    {
                        .try -1 to 0 finally handler 0 to 1
                    }
                    .method public static void Second() cil managed
                    {
                        .try -1 to 0 finally handler 0 to 1
                    }
                    .method public static void DifferentKind() cil managed
                    {
                        .try -1 to 0 fault handler 0 to 1
                    }
                    .method public static void DifferentBounds() cil managed
                    {
                        .try -2 to 0 finally handler 0 to 1
                    }
                }
                """;
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { Fold = fold, ErrorTolerant = true });
            Assert.Equal(4, diagnostics.Length);
            Assert.All(diagnostics, diagnostic => Assert.Equal(DiagnosticIds.InvalidExceptionRegion, diagnostic.Id));
            Assert.NotNull(result);
            using PEReader pe = new(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            Dictionary<string, int> methods = reader.MethodDefinitions.Select(reader.GetMethodDefinition)
                .ToDictionary(method => reader.GetString(method.Name), method => method.RelativeVirtualAddress);
            Assert.Equal(fold, methods["First"] == methods["Second"]);
            Assert.NotEqual(methods["First"], methods["DifferentKind"]);
            Assert.NotEqual(methods["First"], methods["DifferentBounds"]);
            Assert.Equal(new byte[] { 0x2A }, pe.GetMethodBody(methods["Prefix"]).GetILBytes());
            foreach (string name in new[] { "First", "Second", "DifferentKind", "DifferentBounds" })
            {
                Assert.True(methods[name] > 0);
                Assert.Equal(0, methods[name] % 4);
                MethodBodyBlock body = pe.GetMethodBody(methods[name]);
                Assert.Empty(body.GetILBytes()!);
                ExceptionRegion region = Assert.Single(body.ExceptionRegions);
                Assert.Equal(name == "DifferentKind" ? ExceptionRegionKind.Fault : ExceptionRegionKind.Finally, region.Kind);
                Assert.Equal(name == "DifferentBounds" ? -2 : -1, region.TryOffset);
                Assert.Equal(name == "DifferentBounds" ? 2 : 1, region.TryLength);
                Assert.Equal(0, region.HandlerOffset);
                Assert.Equal(1, region.HandlerLength);
            }
        }

        [Fact]
        public void PdbOption_ProducesPortablePdbWithoutLineDirectives()
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

            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Pdb = true });
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
            var pdbReader = pdbProvider.GetMetadataReader();

            Assert.Empty(pdbReader.Documents);
            Assert.NotEmpty(pdbReader.MethodDebugInformation);
        }

        [Theory]
        [InlineData(DebugMode.Impl)]
        [InlineData(DebugMode.Opt)]
        public void DebugModeWithoutDebug_ProducesPortablePdb(DebugMode debugMode)
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options { DebugMode = debugMode });

            Assert.NotNull(result.PortablePdb);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WithoutDebugOrPdb_ImageHasNoDebugDirectory(bool deterministic, bool lineDirective)
        {
            string source = lineDirective ? PortablePdbSource : PortablePdbSource.Replace(".line 10 'test.cs'", string.Empty);
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Deterministic = deterministic });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            DirectoryEntry debugTable = pe.PEHeaders.PEHeader!.DebugTableDirectory;

            // As in native ilasm: no debug directory at all, not even the Reproducible entry that
            // ManagedPEBuilder adds by default to a deterministic image.
            Assert.Equal((0, 0), (debugTable.RelativeVirtualAddress, debugTable.Size));
        }

        [Fact]
        public void WithoutDebugOrPdb_DeterministicImageCarriesNoDebugData()
        {
            // Given no debug directory, ManagedPEBuilder writes a Reproducible entry into a deterministic image.
            // Clearing the PE header's debug directory alone would leave that entry's bytes in .text, so the
            // deterministic image must have the same .text size as a nondeterministic one.
            string source = PortablePdbSource.Replace(".line 10 'test.cs'", string.Empty);

            static int TextSize(string source, bool deterministic)
            {
                CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(source, new Options { Deterministic = deterministic });
                using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
                return pe.PEHeaders.SectionHeaders.Single(section => section.Name == ".text").VirtualSize;
            }

            Assert.Equal(TextSize(source, deterministic: false), TextSize(source, deterministic: true));
        }

        [Fact]
        public void PdbOption_DoesNotAddDebuggableAttribute()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Pdb = true });
            var reader = pe.GetMetadataReader();

            Assert.Empty(reader.GetAssemblyDefinition().GetCustomAttributes());
        }

        [Fact]
        public void WithoutDebugOrPdb_DeterministicImageWithExportsHasNoDebugDirectory()
        {
            // .vtfixup and .export images are built by VTableExportPEBuilder rather than the standard builder;
            // without a PDB they too have no debug directory, not even ManagedPEBuilder's default Reproducible one.
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .data VT = int32(0)
                .vtfixup [1] int32 fromunmanaged at VT
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .vtentry 1 : 1
                        .export [1]
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { Deterministic = true });
            DirectoryEntry debugTable = pe.PEHeaders.PEHeader!.DebugTableDirectory;

            Assert.Contains(pe.PEHeaders.SectionHeaders, section => section.Name == ".sdata");
            Assert.Equal((0, 0), (debugTable.RelativeVirtualAddress, debugTable.Size));
        }

        [Fact]
        public void PortablePdb_IsReferencedByCodeViewThenPdbChecksumAndNotEmbedded()
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options { Debug = true });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));

            Assert.Equal(
                new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum },
                pe.ReadDebugDirectory().Select(entry => entry.Type));
        }

        [Fact]
        public void PortablePdb_Deterministic_IsReferencedByCodeViewThenPdbChecksumThenReproducible()
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options { Debug = true, Deterministic = true });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));

            Assert.Equal(
                new[] { DebugDirectoryEntryType.CodeView, DebugDirectoryEntryType.PdbChecksum, DebugDirectoryEntryType.Reproducible },
                pe.ReadDebugDirectory().Select(entry => entry.Type));
        }

        [Fact]
        public void PortablePdb_CodeViewEntry_NamesPdbFilePath()
        {
            string pdbFilePath = Path.Combine(Path.GetTempPath(), "out", "Output.pdb");
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options
            {
                Debug = true,
                OutputFileName = "Output.dll",
                PdbFilePath = pdbFilePath,
            });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);

            Assert.Equal(pdbFilePath, pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Path);
        }

        [Theory]
        [InlineData("Output.dll", "Output.pdb")]
        [InlineData("Output", "Output.pdb")]
        [InlineData(null, "assembly.pdb")]
        public void PortablePdb_CodeViewEntry_WithoutPdbFilePath_NamesPdbAfterOutputFileName(string? outputFileName, string expectedPath)
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options
            {
                Debug = true,
                OutputFileName = outputFileName,
            });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);

            Assert.Equal(expectedPath, pe.ReadCodeViewDebugDirectoryData(codeViewEntry).Path);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PortablePdb_CodeViewEntry_CarriesPdbId(bool deterministic)
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options
            {
                Debug = true,
                Deterministic = deterministic,
            });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            DebugDirectoryEntry codeViewEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.CodeView);
            CodeViewDebugDirectoryData codeView = pe.ReadCodeViewDebugDirectoryData(codeViewEntry);
            using var pdbProvider = DocumentCompilerTestHelpers.GetPortablePdbReaderProvider(result);
            BlobContentId pdbId = new(pdbProvider.GetMetadataReader().DebugMetadataHeader!.Id);

            Assert.Equal(pdbId.Guid, codeView.Guid);
            Assert.Equal(pdbId.Stamp, codeViewEntry.Stamp);
            Assert.Equal(1, codeView.Age);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PortablePdb_PdbChecksumEntry_IsSha256OfPdbWithIdZeroed(bool deterministic)
        {
            CompilationResult result = DocumentCompilerTestHelpers.CompileAndGetResult(PortablePdbSource, new Options
            {
                Debug = true,
                Deterministic = deterministic,
            });
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            DebugDirectoryEntry checksumEntry = Assert.Single(pe.ReadDebugDirectory(), entry => entry.Type == DebugDirectoryEntryType.PdbChecksum);
            PdbChecksumDebugDirectoryData checksum = pe.ReadPdbChecksumDebugDirectoryData(checksumEntry);

            byte[] pdb = DocumentCompilerTestHelpers.GetPortablePdb(result).ToArray();
            using (var pdbProvider = MetadataReaderProvider.FromPortablePdbImage(ImmutableArray.Create(pdb)))
            {
                DebugMetadataHeader header = pdbProvider.GetMetadataReader().DebugMetadataHeader!;
                Array.Clear(pdb, header.IdStartOffset, header.Id.Length);
            }

            Assert.Equal("SHA256", checksum.AlgorithmName);
            Assert.Equal(SHA256.HashData(pdb), checksum.Checksum.ToArray());
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
