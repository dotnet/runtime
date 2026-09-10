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
    public class ExportedTypeTests
    {

        [Fact]
        public void Diagnostic_ExportedTypeNotFound()
        {
            // Reference a nested exported type that doesn't exist
            // Uses assembly references instead of files to avoid file entry point issues
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern ForwardedAssembly
                }
                .class extern public NestedType
                {
                    .class extern NonExistentParent
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.NotEmpty(diagnostics);
            // Check only error diagnostics (warnings are also expected for missing implementations)
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            Assert.NotEmpty(errors);
            Assert.All(errors, d => Assert.Equal(DiagnosticIds.ExportedTypeNotFound, d.Id));
        }


        [Fact]
        public void ExportHead_ObsoleteSyntax_DoesNotThrow()
        {
            string source = """
                .assembly test { }
                .export [System.Object]
                .class public auto ansi beforefieldinit Test
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics);
        }


        [Fact]
        public void ExportDirective_WithoutVtfixup_CompilesSuccessfully()
        {
            // .export directive without .vtfixup records export info but doesn't create vtable
            // This is valid IL - the export ordinal/name is stored for potential use by tools
            string source = """
                .assembly test { }
                .assembly extern mscorlib { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void ExportedMethod() cil managed
                    {
                        .export [1] as MyExport
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());

            // Without vtfixup, ILOnly flag should remain set (no vtable slot data needed)
            var corHeader = pe.PEHeaders.CorHeader;
            Assert.NotNull(corHeader);
            Assert.True(corHeader!.Flags.HasFlag(CorFlags.ILOnly),
                "Without vtfixup, assembly should remain IL-only");

            // Verify the method exists in metadata
            var reader = pe.GetMetadataReader();
            var exportedMethod = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .FirstOrDefault(m => reader.GetString(m.Name) == "ExportedMethod");
            Assert.False(exportedMethod.Equals(default), "ExportedMethod should exist in metadata");
        }


        [Fact]
        public void TypeForwarder_EmitsExportedType()
        {
            // Test type forwarder (exercises ExportedType with assembly reference implementation)
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern forwarder System.ForwardedType
                {
                    .assembly extern ForwardedAssembly
                }
                """;

            // First check diagnostics to see if there are any errors
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify the ExportedType table has an entry
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.ExportedType));

            var exportedType = reader.ExportedTypes.Select(h => reader.GetExportedType(h)).First();
            Assert.Equal("ForwardedType", reader.GetString(exportedType.Name));
            Assert.Equal("System", reader.GetString(exportedType.Namespace));
            // Forwarder flag is TypeAttributes.Forwarder (0x00200000)
            Assert.True(exportedType.Attributes.HasFlag((System.Reflection.TypeAttributes)0x00200000));
        }


        [Fact]
        public void TypeForwarder_WithMissingImplementation_EmitsWarning()
        {
            // Test that missing implementation emits a warning and doesn't emit the ExportedType
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class extern forwarder System.ForwardedType
                {
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());

            // Should have a warning about missing implementation
            var warning = diagnostics.FirstOrDefault(d => d.Id == DiagnosticIds.MissingExportedTypeImplementation);
            Assert.NotNull(warning);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void ExportedTypeAttributesAndImplementations_EmitExpectedMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .file Module.netmodule .hash = (01 02 03 04)

                .class extern public Outer
                {
                    .assembly extern ForwardedAssembly
                }
                .class extern private PrivateType
                {
                    .assembly extern ForwardedAssembly
                }
                .class extern forwarder ForwardedType
                {
                    .assembly extern ForwardedAssembly
                }
                .class extern nested public NestedPublic
                {
                    .class extern Outer
                }
                .class extern nested private NestedPrivate
                {
                    .class extern Outer
                }
                .class extern nested family NestedFamily
                {
                    .class extern Outer
                }
                .class extern nested assembly NestedAssembly
                {
                    .class extern Outer
                }
                .class extern nested famandassem NestedFamAndAssem
                {
                    .class extern Outer
                }
                .class extern nested famorassem NestedFamOrAssem
                {
                    .class extern Outer
                }
                .class extern public FileBackedType
                {
                    .file Module.netmodule
                }
                .class extern public TypeDefIdType
                {
                    .assembly extern ForwardedAssembly
                    .class 42
                }
                .class extern public AttributedType
                {
                    .assembly extern ForwardedAssembly
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                    ;
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var exportedTypes = reader.ExportedTypes
                .ToDictionary(
                    handle => reader.GetString(reader.GetExportedType(handle).Name),
                    handle => (Handle: handle, Type: reader.GetExportedType(handle)));

            Assert.Equal(TypeAttributes.Public, exportedTypes["Outer"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NotPublic, exportedTypes["PrivateType"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.True(exportedTypes["ForwardedType"].Type.IsForwarder);
            Assert.Equal(TypeAttributes.NestedPublic, exportedTypes["NestedPublic"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NestedPrivate, exportedTypes["NestedPrivate"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NestedFamily, exportedTypes["NestedFamily"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NestedAssembly, exportedTypes["NestedAssembly"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NestedFamANDAssem, exportedTypes["NestedFamAndAssem"].Type.Attributes & TypeAttributes.VisibilityMask);
            Assert.Equal(TypeAttributes.NestedFamORAssem, exportedTypes["NestedFamOrAssem"].Type.Attributes & TypeAttributes.VisibilityMask);

            Assert.Equal(HandleKind.AssemblyReference, exportedTypes["Outer"].Type.Implementation.Kind);
            Assert.Equal(HandleKind.AssemblyFile, exportedTypes["FileBackedType"].Type.Implementation.Kind);
            Assert.Equal(HandleKind.ExportedType, exportedTypes["NestedPublic"].Type.Implementation.Kind);
            Assert.Equal(42, exportedTypes["TypeDefIdType"].Type.GetTypeDefinitionId());

            var attribute = reader.GetCustomAttribute(
                Assert.Single(reader.GetCustomAttributes(exportedTypes["AttributedType"].Handle)));
            Assert.Equal(
                [0x01, 0x00, 0x00, 0x00],
                reader.GetBlobBytes(attribute.Value));
        }

    }
}
