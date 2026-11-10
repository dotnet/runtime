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

namespace Microsoft.DotNet.ILAssembler.Tests
{
    public class ExportedTypeTests
    {
        private const TypeAttributes Forwarder = (TypeAttributes)0x00200000;

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
        public void ExportHead_InvalidTopLevelSyntax_ReportsDiagnostic()
        {
            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                ".assembly test {} .export [System.Object] .class Test {}",
                new Options());

            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.Parser && diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.Null(result);
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


        [Theory]
        [InlineData(true, "D1,D2")]
        [InlineData(false, "D1,D2")]
        [InlineData(true, "D3,D4")]
        [InlineData(false, "D3,D4")]
        [InlineData(true, "D,D")]
        [InlineData(false, "D,D")]
        [InlineData(true, "D,E")]
        [InlineData(false, "D,E")]
        [InlineData(true, "D1,D2,D3,D4,D5,D1,D2")]
        [InlineData(false, "D1,D2,D3,D4,D5,D1,D2")]
        public void TypeForwarders_PreserveDuplicateDeclarations(bool isAssembly, string destinations)
        {
            string[] assemblyNames = destinations.Split(',');
            var source = new StringBuilder();
            if (isAssembly)
            {
                source.AppendLine(".assembly Forwarder { }");
            }
            source.AppendLine(".module ForwarderModule.dll");
            foreach (string assemblyName in assemblyNames.Distinct())
            {
                source.AppendLine($".assembly extern {assemblyName} {{ }}");
            }
            foreach (string assemblyName in assemblyNames)
            {
                source.AppendLine($$"""
                    .class extern forwarder Testspace.TestType
                    {
                        .assembly extern {{assemblyName}}
                    }
                    """);
            }

            string declarations = source.ToString();
            var (normalDiagnostics, normalResult) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                declarations, new Options { Dll = true });
            Assert.Null(normalResult);
            AssertDuplicateDiagnostics(normalDiagnostics, declarations, assemblyNames.Length - 1);

            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                declarations, new Options { Dll = true, ErrorTolerant = true });
            AssertDuplicateDiagnostics(diagnostics, declarations, assemblyNames.Length - 1);
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            Assert.Equal(isAssembly, reader.IsAssembly);
            Assert.Equal("ForwarderModule.dll", reader.GetString(reader.GetModuleDefinition().Name));
            Assert.Equal(assemblyNames.Length, reader.ExportedTypes.Count);

            int index = 0;
            foreach (ExportedTypeHandle handle in reader.ExportedTypes)
            {
                ExportedType exportedType = reader.GetExportedType(handle);
                Assert.Equal("Testspace", reader.GetString(exportedType.Namespace));
                Assert.Equal("TestType", reader.GetString(exportedType.Name));
                Assert.Equal(Forwarder, exportedType.Attributes);
                Assert.Equal(0, exportedType.GetTypeDefinitionId());
                Assert.Equal(HandleKind.AssemblyReference, exportedType.Implementation.Kind);
                AssemblyReference destination = reader.GetAssemblyReference((AssemblyReferenceHandle)exportedType.Implementation);
                Assert.Equal(assemblyNames[index++], reader.GetString(destination.Name));
            }
        }

        [Fact]
        public void DuplicateTypeForwarders_KeepAttributesAndFirstNestedParent()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly extern D1 { }
                .assembly extern D2 { }
                .assembly Forwarder { }
                .class extern public forwarder Testspace.TestType
                {
                    .assembly extern D1
                    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor() = (01 00 00 00)
                }
                .class extern private forwarder Testspace.TestType
                {
                    .assembly extern D2
                    .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = (01 00 01 00 00)
                }
                .class extern nested public Nested
                {
                    .class extern Testspace.TestType
                }
                """;

            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { ErrorTolerant = true });
            AssertDuplicateDiagnostics(diagnostics, source, 1);
            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            ExportedTypeHandle[] handles = reader.ExportedTypes.ToArray();
            Assert.Equal(3, handles.Length);
            ExportedType first = reader.GetExportedType(handles[0]);
            ExportedType second = reader.GetExportedType(handles[1]);
            ExportedType nested = reader.GetExportedType(handles[2]);
            Assert.Equal(TypeAttributes.Public | Forwarder, first.Attributes);
            Assert.Equal(Forwarder, second.Attributes);
            Assert.Equal("D1", reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)first.Implementation).Name));
            Assert.Equal("D2", reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)second.Implementation).Name));
            Assert.Equal(
                new byte[] { 0x01, 0x00, 0x00, 0x00 },
                reader.GetBlobBytes(reader.GetCustomAttribute(Assert.Single(first.GetCustomAttributes())).Value));
            Assert.Equal(
                new byte[] { 0x01, 0x00, 0x01, 0x00, 0x00 },
                reader.GetBlobBytes(reader.GetCustomAttribute(Assert.Single(second.GetCustomAttributes())).Value));
            Assert.Equal("Nested", reader.GetString(nested.Name));
            Assert.Equal(TypeAttributes.NestedPublic, nested.Attributes);
            Assert.Equal(handles[0], (ExportedTypeHandle)nested.Implementation);
        }

        private static void AssertDuplicateDiagnostics(ImmutableArray<Diagnostic> diagnostics, string source, int count)
        {
            Assert.Equal(count, diagnostics.Length);
            int position = source.IndexOf(".class extern", StringComparison.Ordinal);
            foreach (Diagnostic diagnostic in diagnostics)
            {
                position = source.IndexOf(".class extern", position + 1, StringComparison.Ordinal);
                Assert.Equal("ILA0065", diagnostic.Id);
                Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
                Assert.Equal("Duplicate exported type 'Testspace.TestType'", diagnostic.Message);
                Assert.Equal("test.il", diagnostic.Location.Source.Path);
                Assert.Equal(position, diagnostic.Location.Span.Start);
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void DuplicateFileBackedExportedTypes_RequireErrorTolerantMode(bool errorTolerant)
        {
            string source = """
                .assembly test { }
                .file Module.netmodule
                .class extern public Testspace.TestType { .file Module.netmodule }
                .class extern private Testspace.TestType { .file Module.netmodule }
                """;

            var (diagnostics, result) = DocumentCompilerTestHelpers.CompileWithDiagnostics(
                source, new Options { ErrorTolerant = errorTolerant });
            AssertDuplicateDiagnostics(diagnostics, source, 1);
            if (!errorTolerant)
            {
                Assert.Null(result);
                return;
            }

            Assert.NotNull(result);
            using var pe = new PEReader(DocumentCompilerTestHelpers.Serialize(result));
            MetadataReader reader = pe.GetMetadataReader();
            ExportedType[] types = reader.ExportedTypes.Select(reader.GetExportedType).ToArray();
            Assert.Equal(2, types.Length);
            Assert.Equal(TypeAttributes.Public, types[0].Attributes);
            Assert.Equal(TypeAttributes.NotPublic, types[1].Attributes);
            Assert.Equal(HandleKind.AssemblyFile, types[0].Implementation.Kind);
            Assert.Equal(types[0].Implementation, types[1].Implementation);
        }

        [Fact]
        public void ExportedTypes_WithDistinctFullNamesOrParents_AreNotDuplicates()
        {
            string source = """
                .assembly test { }
                .assembly extern D { }
                .class extern forwarder First.TestType { .assembly extern D }
                .class extern forwarder Second.TestType { .assembly extern D }
                .class extern forwarder First.OtherType { .assembly extern D }
                .class extern nested public TestType { .class extern First.TestType }
                .class extern nested public TestType { .class extern Second.TestType }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            Assert.Equal(5, pe.GetMetadataReader().ExportedTypes.Count);
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
