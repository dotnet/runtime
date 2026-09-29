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
    public class DataTests
    {
        [Fact]
        public void Diagnostic_InvalidMetadataToken()
        {
            // Reference an invalid token in an exported type declaration
            // Uses an assembly reference instead of a file to avoid file entry point issues
            string source = """
                .assembly extern mscorlib { }
                .assembly extern ForwardedAssembly { }
                .assembly test { }
                .class extern public MyExportedType
                {
                    .assembly extern ForwardedAssembly
                    mdtoken(0x99999999)
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidMetadataToken, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_UnsupportedTlsData()
        {
            string source = """
                .assembly test { }
                .data tls T_00006000 = bytearray (02)
                """;

            ImmutableArray<Diagnostic> diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            Diagnostic error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsupportedTlsData, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
            Assert.Equal("TLS RVA data declarations are not supported", error.Message);
        }

        [Fact]
        public void ExecutableWithMappedFieldData_HasExecutablePeHeaderAndFieldRva()
        {
            string source = """
                .mscorlib
                .assembly dataseg { }
                .field static int8 result at ResultData
                .data ResultData = bytearray (63)
                .method static int32 Main()
                {
                    .entrypoint
                    ldsfld int8 result
                    ldc.i4.1
                    add
                    ret
                }
                """;

            using PEReader pe = DocumentCompilerTestHelpers.CompileAndGetReader(
                source,
                new Options { OutputFileName = "dataseg.exe" });

            Assert.True(pe.PEHeaders.IsExe);
            Assert.Equal(
                Characteristics.ExecutableImage | Characteristics.Bit32Machine,
                pe.PEHeaders.CoffHeader.Characteristics);

            MetadataReader reader = pe.GetMetadataReader();
            FieldDefinition field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));
            Assert.Equal(FieldAttributes.Static | FieldAttributes.HasFieldRVA, field.Attributes);
            Assert.Equal(
                0x63,
                pe.GetSectionData(field.GetRelativeVirtualAddress()).GetContent(0, 1)[0]);
        }
    }
}
