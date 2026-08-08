// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
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

        [Fact]
        public void PermissionSet_WithDemand_EmitsDeclSecurityBlob()
        {
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

            var securityHandle = MetadataTokens.DeclarativeSecurityAttributeHandle(1);
            var security = reader.GetDeclarativeSecurityAttribute(securityHandle);

            Assert.Equal(1, reader.GetTableRowCount(TableIndex.DeclSecurity));
            Assert.Equal(2, (int)security.Action);
            Assert.Equal(HandleKind.AssemblyDefinition, security.Parent.Kind);
            Assert.Equal([0x2E], reader.GetBlobBytes(security.PermissionSet));
        }

        [Theory]
        [InlineData("request", 1)]
        [InlineData("demand", 2)]
        [InlineData("assert", 3)]
        [InlineData("deny", 4)]
        [InlineData("permitonly", 5)]
        [InlineData("linkcheck", 6)]
        [InlineData("inheritcheck", 7)]
        [InlineData("reqmin", 8)]
        [InlineData("reqopt", 9)]
        [InlineData("reqrefuse", 10)]
        [InlineData("prejitgrant", 11)]
        [InlineData("prejitdeny", 12)]
        [InlineData("noncasdemand", 13)]
        [InlineData("noncaslinkdemand", 14)]
        [InlineData("noncasinheritance", 15)]
        public void PermissionSet_Action_EmitsExpectedDeclSecurityAction(string action, int expectedAction)
        {
            string source = $$"""
                .assembly test
                {
                    .permissionset {{action}} = (2E)
                }
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));

            Assert.Equal((DeclarativeSecurityAction)expectedAction, security.Action);
            Assert.Equal(HandleKind.AssemblyDefinition, security.Parent.Kind);
            Assert.Equal([0x2E], reader.GetBlobBytes(security.PermissionSet));
        }

        [Fact]
        public void PermissionSet_VerbalForm_EmitsSerializedAttributeAndNamedProperty()
        {
            string source = """
                .assembly test
                {
                    .permissionset deny = {
                        class 'My.Permission' = {
                            property bool Enabled = bool(true)
                        }
                    }
                }
                .class public auto ansi Test { }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));

            Assert.Equal(DeclarativeSecurityAction.Deny, security.Action);
            BlobReader permissionSet = reader.GetBlobReader(security.PermissionSet);
            Assert.Equal((byte)'.', permissionSet.ReadByte());
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal("My.Permission", permissionSet.ReadSerializedString());
            Assert.Equal(1, permissionSet.ReadUInt16());
            Assert.Equal((byte)CustomAttributeNamedArgumentKind.Property, permissionSet.ReadByte());
            Assert.Equal((byte)SerializationTypeCode.Boolean, permissionSet.ReadByte());
            Assert.Equal("Enabled", permissionSet.ReadSerializedString());
            Assert.True(permissionSet.ReadBoolean());
            Assert.Equal(0, permissionSet.RemainingBytes);
        }

        [Fact]
        public void UnsupportedPermission_NameValueForms_ErrorTolerantImageRemainsValid()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .permission demand [mscorlib]System.Security.Permissions.SecurityPermissionAttribute(
                    "Boolean" = true,
                    "Integer" = 123,
                    "WrappedInteger" = int32(456),
                    "Text" = "hello",
                    "Enum8" = [mscorlib]Contoso.Kind(int8:1),
                    "Enum16" = [mscorlib]Contoso.Kind(int16:2),
                    "Enum32" = [mscorlib]Contoso.Kind(int32:3),
                    "EnumDefault" = [mscorlib]Contoso.Kind(4)
                )
                .class public auto ansi Test extends [mscorlib]System.Object { }
                """;

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options { ErrorTolerant = true });

            Assert.Contains(
                diagnostics,
                diagnostic => diagnostic.Id == DiagnosticIds.UnsupportedSecurityDeclaration);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            var reader = pe.GetMetadataReader();

            Assert.Contains(
                reader.TypeDefinitions,
                handle => reader.GetString(reader.GetTypeDefinition(handle).Name) == "Test");
            Assert.Equal(0, reader.GetTableRowCount(TableIndex.DeclSecurity));
        }

        [Fact]
        public void TypeAndMethodPermissionSets_EmitExpectedParentsAndActions()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .permissionset demand = (2E)
                    .method public static void M() cil managed
                    {
                        .permissionset assert = (2F)
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var securityDeclarations = Enumerable.Range(1, reader.GetTableRowCount(TableIndex.DeclSecurity))
                .Select(row => reader.GetDeclarativeSecurityAttribute(MetadataTokens.DeclarativeSecurityAttributeHandle(row)))
                .ToArray();

            Assert.Contains(
                securityDeclarations,
                security =>
                    security.Parent.Kind == HandleKind.TypeDefinition &&
                    security.Action == DeclarativeSecurityAction.Demand &&
                    reader.GetBlobBytes(security.PermissionSet).SequenceEqual((byte[])[0x2E]));
            Assert.Contains(
                securityDeclarations,
                security =>
                    security.Parent.Kind == HandleKind.MethodDefinition &&
                    security.Action == DeclarativeSecurityAction.Assert &&
                    reader.GetBlobBytes(security.PermissionSet).SequenceEqual((byte[])[0x2F]));
        }
    }
}
