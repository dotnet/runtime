// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Internal.IL;
using Xunit;
using DocumentCompilerTestHelpers = ILAssembler.Tests.DocumentCompilerTestHelpers;

namespace ILAssembler.Tests
{
    public class InteropTests
    {
        [Fact]
        public void Diagnostic_InvalidPInvokeSignature()
        {
            // P/Invoke with no module name triggers InvalidPInvokeSignature
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static pinvokeimpl() void TestMethod() cil managed
                    {
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var error = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.InvalidPInvokeSignature, error.Id);
            Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedNativeType_Variant()
        {
            // Using deprecated VARIANT native type triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(variant) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedNativeType, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void Diagnostic_DeprecatedCustomMarshaller()
        {
            // Using 4-string custom marshaller syntax triggers warning
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void TestMethod(object marshal(custom("guid", "nativeType", "marshallerType", "cookie")) arg) cil managed
                    {
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var warning = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.DeprecatedCustomMarshaller, warning.Id);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        }

        [Fact]
        public void PinvokeMethod_SetsPinvokeImplFlag()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi beforefieldinit MyClass extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("kernel32.dll" winapi)
                        int32 GetCurrentProcessId() cil managed preservesig
                    {
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "GetCurrentProcessId");

            Assert.True(method.Attributes.HasFlag(MethodAttributes.PinvokeImpl));
            var import = method.GetImport();
            Assert.False(import.Module.IsNil);
            Assert.Equal("GetCurrentProcessId", reader.GetString(import.Name));
        }

        [Fact]
        public void FixedArrayMarshalWithoutElementType_EmitsDescriptor()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .field assembly marshal(fixed array [1024]) bool[] Bool
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Equal([0x1E, 0x84, 0x00], reader.GetBlobBytes(field.GetMarshallingDescriptor()));
        }

        [Fact]
        public void SafeArrayMarshalWithoutVariantType_EmitsDescriptor()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public marshal(safearray) object[] Values
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Equal([(byte)UnmanagedType.SafeArray, 0, 0], reader.GetBlobBytes(field.GetMarshallingDescriptor()));
        }

        [Fact]
        public void VariantBoolMarshal_EmitsDescriptor()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public marshal(variant bool) bool Value
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Equal([(byte)UnmanagedType.VariantBool], reader.GetBlobBytes(field.GetMarshallingDescriptor()));
        }

        [Theory]
        [InlineData("[]", "2A50")]
        [InlineData("[100]", "2A50006400")]
        [InlineData("[100 + 1]", "2A50016401")]
        [InlineData("[+ 1]", "2A5001")]
        public void LPArrayMarshalSizeSyntax_EmitsNativeCompatibleDescriptor(string sizeSyntax, string expectedHex)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public marshal({{sizeSyntax}}) int32[] Values
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Equal(Convert.FromHexString(expectedHex), reader.GetBlobBytes(field.GetMarshallingDescriptor()));
        }

        [Theory]
        [InlineData("int8", UnmanagedType.U1)]
        [InlineData("int16", UnmanagedType.U2)]
        [InlineData("int32", UnmanagedType.U4)]
        [InlineData("int64", UnmanagedType.U8)]
        public void UnsignedNativeTypeSpelling_EmitsUnsignedDescriptor(string type, UnmanagedType expected)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .method public static bool marshal(unsigned {{type}}) F() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(1));
            var returnParameter = method.GetParameters()
                .Select(reader.GetParameter)
                .Single(parameter => parameter.SequenceNumber == 0);

            Assert.Equal([(byte)expected], reader.GetBlobBytes(returnParameter.GetMarshallingDescriptor()));
        }

        [Fact]
        public void FixedSysStringMarshal_EmitsDescriptor()
        {
            string source = """
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public marshal(fixed sysstring [256]) string Value
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(MetadataTokens.FieldDefinitionHandle(1));

            Assert.Equal([0x17, 0x81, 0x00], reader.GetBlobBytes(field.GetMarshallingDescriptor()));
        }
    }
}
