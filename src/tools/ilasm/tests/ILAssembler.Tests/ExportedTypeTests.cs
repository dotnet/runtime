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

    }
}
