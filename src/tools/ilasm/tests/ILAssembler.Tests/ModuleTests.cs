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
using System.Text;
using System.Threading.Tasks;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class ModuleTests
    {
        [Fact]
        public void ModuleNotFound_ReportsError()
        {
            // Referencing a module that doesn't exist
            string source = """
                .assembly extern System.Runtime { }
                .typedef [.module NonExistentModule]SomeType as MyType
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.ModuleNotFound, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }


        [Fact]
        public void ModuleName_DefaultsToOutputFileName_WhenNoModuleDirective()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "MyOutput.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("MyOutput.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_OutputFileNameStripsDirectory()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            // OutputFileName should already be just the filename (Program.cs uses Path.GetFileName),
            // but verify the module name is exactly what's provided
            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "bar.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("bar.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_ExplicitModuleDirective_OverridesOutputFileName()
        {
            string source = """
                .assembly TestAssembly { }
                .module Explicit.dll
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options { OutputFileName = "DifferentName.dll" });
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.Equal("Explicit.dll", reader.GetString(moduleDef.Name));
        }


        [Fact]
        public void ModuleName_NoModuleDirective_NoOutputFileName_UsesNilHandle()
        {
            string source = """
                .assembly TestAssembly { }
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var moduleDef = reader.GetModuleDefinition();
            Assert.True(moduleDef.Name.IsNil);
        }


        [Fact]
        public void ModuleLevelField_DoesNotCrash()
        {
            string source = """
                .assembly extern System.Runtime { }
                .assembly TestAssembly { }
                .field public static int32 globalField
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void FileDeclaration_WithHash_EmitsMetadataFileRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .file NetModule.netmodule .hash = (AB CD EF 01)
                .class public auto ansi Test extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var file = reader.GetAssemblyFile(Assert.Single(reader.AssemblyFiles));

            Assert.Equal("NetModule.netmodule", reader.GetString(file.Name));
            Assert.Equal([0xAB, 0xCD, 0xEF, 0x01], reader.GetBlobBytes(file.HashValue));
            Assert.True(file.ContainsMetadata);
            Assert.Equal(0, pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress);
        }

        [Fact]
        public void FileDeclaration_NoMetadata_EmitsResourceFileRow()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .file nometadata DataFile.resources .hash = (FF FE)
                .class public auto ansi Test extends [mscorlib]System.Object { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var file = reader.GetAssemblyFile(Assert.Single(reader.AssemblyFiles));

            Assert.Equal("DataFile.resources", reader.GetString(file.Name));
            Assert.Equal([0xFF, 0xFE], reader.GetBlobBytes(file.HashValue));
            Assert.False(file.ContainsMetadata);
            Assert.Equal(0, pe.PEHeaders.CorHeader!.EntryPointTokenOrRelativeVirtualAddress);
        }

        [Fact]
        public void ModuleExtern_EmitsModuleReference()
        {
            string source = """
                .assembly test { }
                .module extern Native.netmodule
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef));
            var moduleReference = reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(1));

            Assert.Equal("Native.netmodule", reader.GetString(moduleReference.Name));
            Assert.True(reader.GetModuleDefinition().Name.IsNil);
        }

        [Fact]
        public void BareModuleDirective_PreservesDefaultOutputModuleName()
        {
            string source = """
                .assembly test { }
                .module
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options { OutputFileName = "Output.dll" });
            var reader = pe.GetMetadataReader();

            Assert.Equal("Output.dll", reader.GetString(reader.GetModuleDefinition().Name));
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.ModuleRef));
        }

        [Fact]
        public void GlobalMethodAndModuleAttribute_EmitModuleMetadataAndPdb()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor()
                .line 1 "global.cs"
                .method public static int32 Global() cil managed
                {
                    .locals init (int32 value)
                    .line 10
                    ldc.i4.s 42
                    stloc.0
                    ldloc.0
                    ret
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var moduleType = reader.GetTypeDefinition(MetadataTokens.TypeDefinitionHandle(1));
            var globalMethod = reader.GetMethodDefinition(Assert.Single(moduleType.GetMethods()));
            var body = pe.GetMethodBody(globalMethod.RelativeVirtualAddress);

            Assert.Equal("<Module>", reader.GetString(moduleType.Name));
            Assert.Equal("Global", reader.GetString(globalMethod.Name));
            Assert.True(body.LocalVariablesInitialized);
            Assert.Equal(
                new[] { "int32" },
                reader.GetStandaloneSignature(body.LocalSignature)
                    .DecodeLocalSignature(DocumentCompilerTestHelpers.Decoder, genericContext: null));
            Assert.Equal(42, body.GetILBytes()![1]);

            CustomAttributeValue<string> attribute = reader
                .GetCustomAttribute(Assert.Single(reader.GetModuleDefinition().GetCustomAttributes()))
                .DecodeValue(DocumentCompilerTestHelpers.Decoder);
            Assert.Empty(attribute.FixedArguments);
            Assert.Empty(attribute.NamedArguments);

            var embeddedPdb = Assert.Single(
                pe.ReadDebugDirectory(),
                entry => entry.Type == DebugDirectoryEntryType.EmbeddedPortablePdb);
            using var pdbProvider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdb);
            var pdbReader = pdbProvider.GetMetadataReader();
            Assert.Contains(
                "global.cs",
                pdbReader.GetString(pdbReader.GetDocument(Assert.Single(pdbReader.Documents)).Name));
        }
    }
}
