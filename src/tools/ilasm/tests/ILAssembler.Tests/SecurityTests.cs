// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;
using Internal.IL;
using Xunit;

namespace Microsoft.DotNet.ILAssembler.Tests
{
    public class SecurityTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("(\"UnmanagedCode\" = true)")]
        [InlineData("= { property bool UnmanagedCode = bool(true) }")]
        public void Diagnostic_UnsupportedSecurityDeclaration(string arguments)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod() cil managed
                    {
                        .permission demand [mscorlib]System.Security.Permissions.SecurityPermissionAttribute {{arguments}}
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.UnsupportedSecurityDeclaration, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"<PermissionSet/>\"", "<PermissionSet/>")]
        [InlineData("\"<Permission\" + \"Set/>\"", "<PermissionSet/>")]
        [InlineData("\"caf\u00e9\"", "caf\u00e9")]
        [InlineData("\"before\\0after\"", "before")]
        public void PermissionSet_StringForm_EmitsUnterminatedUtf16(string literal, string expected)
        {
            string source = $$"""
                .assembly test
                {
                    .permissionset demand {{literal}}
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            DeclarativeSecurityAttribute security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));

            Assert.Equal(DeclarativeSecurityAction.Demand, security.Action);
            Assert.Equal(Encoding.Unicode.GetBytes(expected), reader.GetBlobBytes(security.PermissionSet));
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
            Assert.Equal(12, permissionSet.ReadCompressedInteger());
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal((byte)CustomAttributeNamedArgumentKind.Property, permissionSet.ReadByte());
            Assert.Equal((byte)SerializationTypeCode.Boolean, permissionSet.ReadByte());
            Assert.Equal("Enabled", permissionSet.ReadSerializedString());
            Assert.True(permissionSet.ReadBoolean());
            Assert.Equal(0, permissionSet.RemainingBytes);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 7)]
        [InlineData(32, 215)]
        [InlineData(128, 916)]
        public void PermissionSet_VerbalForm_CompressesAttributeLengthAndArgumentCount(int argumentCount, int expectedLength)
        {
            string arguments = string.Join(
                Environment.NewLine,
                Enumerable.Range(0, argumentCount).Select(i =>
                    $"{(i % 2 == 0 ? "property" : "field")} bool P{i} = bool({(i % 2 == 0 ? "true" : "false")})"));
            string source = $$"""
                .assembly test
                {
                    .permissionset demand = {
                        class 'My.Permission' = {
                            {{arguments}}
                        },
                        class 'Next.Permission' = {}
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            DeclarativeSecurityAttribute security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));
            BlobReader permissionSet = reader.GetBlobReader(security.PermissionSet);

            Assert.Equal((byte)'.', permissionSet.ReadByte());
            Assert.Equal(2, permissionSet.ReadCompressedInteger());
            Assert.Equal("My.Permission", permissionSet.ReadSerializedString());
            Assert.Equal(expectedLength, permissionSet.ReadCompressedInteger());
            int argumentsStart = permissionSet.Offset;
            Assert.Equal(argumentCount, permissionSet.ReadCompressedInteger());
            for (int i = 0; i < argumentCount; i++)
            {
                Assert.Equal(
                    (byte)(i % 2 == 0 ? CustomAttributeNamedArgumentKind.Property : CustomAttributeNamedArgumentKind.Field),
                    permissionSet.ReadByte());
                Assert.Equal((byte)SerializationTypeCode.Boolean, permissionSet.ReadByte());
                Assert.Equal($"P{i}", permissionSet.ReadSerializedString());
                Assert.Equal(i % 2 == 0, permissionSet.ReadBoolean());
            }

            Assert.Equal(expectedLength, permissionSet.Offset - argumentsStart);
            Assert.Equal("Next.Permission", permissionSet.ReadSerializedString());
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal(0, permissionSet.ReadCompressedInteger());
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

        [Fact]
        public void PermissionSet_VerbalAttributesPreserveNamesAndTypeReferenceOrder()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test
                {
                    .permissionset demand = {
                        [mscorlib]Contoso.FirstPermission = { },
                        class 'Contoso.QuotedPermission' = {
                            property bool Enabled = bool(true)
                        },
                        [mscorlib]Contoso.SecondPermission = { }
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            MetadataReader reader = pe.GetMetadataReader();
            DeclarativeSecurityAttribute security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));
            BlobReader permissionSet = reader.GetBlobReader(security.PermissionSet);

            Assert.Equal((byte)'.', permissionSet.ReadByte());
            Assert.Equal(3, permissionSet.ReadCompressedInteger());
            Assert.StartsWith(
                "Contoso.FirstPermission, mscorlib",
                permissionSet.ReadSerializedString(),
                StringComparison.Ordinal);
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal(0, permissionSet.ReadCompressedInteger());
            Assert.Equal("Contoso.QuotedPermission", permissionSet.ReadSerializedString());
            Assert.Equal(12, permissionSet.ReadCompressedInteger());
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal((byte)CustomAttributeNamedArgumentKind.Property, permissionSet.ReadByte());
            Assert.Equal((byte)SerializationTypeCode.Boolean, permissionSet.ReadByte());
            Assert.Equal("Enabled", permissionSet.ReadSerializedString());
            Assert.True(permissionSet.ReadBoolean());
            Assert.StartsWith(
                "Contoso.SecondPermission, mscorlib",
                permissionSet.ReadSerializedString(),
                StringComparison.Ordinal);
            Assert.Equal(1, permissionSet.ReadCompressedInteger());
            Assert.Equal(0, permissionSet.ReadCompressedInteger());
            Assert.Equal(0, permissionSet.RemainingBytes);

            Assert.Equal(
                ["FirstPermission", "SecondPermission"],
                reader.TypeReferences
                    .Select(handle => reader.GetString(reader.GetTypeReference(handle).Name))
                    .ToArray());
        }

        [Fact]
        public void MalformedPermissionSet_DoesNotLeakAttributesIntoNextDocument()
        {
            ImmutableArray<SourceText> documents =
            [
                new SourceText("""
                    .assembly test { }
                    .permissionset demand = {
                        class 'Broken.Permission' = {
                            property bool Enabled = bool(true)
                        },
                    }
                    """, "broken.il"),
                new SourceText("""
                    .permissionset assert = (2F)
                    .class public auto ansi Test { }
                    """, "valid.il"),
            ];

            var compiler = new DocumentCompiler();
            (ImmutableArray<Diagnostic> diagnostics, CompilationResult? result) = compiler.Compile(
                documents,
                _ => throw new InvalidOperationException("Unexpected include"),
                _ => throw new InvalidOperationException("Unexpected resource"),
                new Options { ErrorTolerant = true });

            Assert.Contains(diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            Assert.NotNull(result);

            var image = new BlobBuilder();
            result!.Serialize(image);
            using var pe = new PEReader(image.ToImmutableArray());
            MetadataReader reader = pe.GetMetadataReader();
            DeclarativeSecurityAttribute security = reader.GetDeclarativeSecurityAttribute(
                Assert.Single(reader.GetAssemblyDefinition().GetDeclarativeSecurityAttributes()));

            Assert.Equal(DeclarativeSecurityAction.Assert, security.Action);
            Assert.Equal([0x2F], reader.GetBlobBytes(security.PermissionSet));
        }
    }
}
