// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class SecurityTests
    {
        [Fact]
        public void Diagnostic_UnsupportedSecurityDeclaration()
        {
            // Using .permission instead of .permissionset triggers error
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .permission demand [mscorlib]System.Security.Permissions.SecurityPermissionAttribute
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsupportedSecurityDeclaration, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void PermissionSet_WithDemand_UsesSecurityAction()
        {
            // Test permission set with bytearray (exercises security action handling)
            string source = """
                .assembly test
                {
                    .permissionset demand = (2E)
                    .ver 1:0:0:0
                }
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            // Verify assembly compiled successfully with the permission set
            Assert.True(reader.GetTableRowCount(TableIndex.DeclSecurity) >= 1);
        }

        [Fact]
        public void PermissionSet_ByteArrayWithoutEquals_IsAccepted()
        {
            string source = """
                .assembly test
                {
                    .permissionset reqrefuse bytearray (2E)
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var declaration = reader.GetDeclarativeSecurityAttribute(MetadataTokens.DeclarativeSecurityAttributeHandle(1));

            Assert.Equal(System.Reflection.DeclarativeSecurityAction.RequestRefuse, declaration.Action);
            Assert.Equal([0x2E], reader.GetBlobBytes(declaration.PermissionSet));
        }
    }
}
