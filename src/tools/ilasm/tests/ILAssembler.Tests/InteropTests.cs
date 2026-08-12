// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
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
        [InlineData("{ 2A 50 }", "2A50")]
        [InlineData("{ 1E }", "1E")]
        [InlineData("{ 00 0A FF }", "000AFF")]
        public void RawMarshalBlob_EmitsSuppliedBytes(string blob, string expectedHex)
        {
            string source = $$"""
                .assembly test { }
                .class public auto ansi Test
                {
                    .field public marshal({{blob}}) int32[] Values
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

        [Fact]
        public void MarshalLpStrParameter_EmitsMarshallingDescriptorBlob()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void Print(string marshal(lpstr) text) cil managed
                    {
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var method = reader.MethodDefinitions
                .Select(h => reader.GetMethodDefinition(h))
                .First(m => reader.GetString(m.Name) == "Print");
            var parameterHandle = Assert.Single(method.GetParameters());
            var parameter = reader.GetParameter(parameterHandle);
            var descriptor = parameter.GetMarshallingDescriptor();

            Assert.False(descriptor.IsNil);
            Assert.Equal("text", reader.GetString(parameter.Name));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.FieldMarshal));
            BlobReader descriptorReader = reader.GetBlobReader(descriptor);
            Assert.Equal((byte)UnmanagedType.LPStr, descriptorReader.ReadByte());
            Assert.Equal(0, descriptorReader.RemainingBytes);
        }

        [Fact]
        public void PinvokeMethod_WithAlias_EmitsImportNameAndModuleReference()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi beforefieldinit NativeMethods extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("kernel32.dll" as "GetTickCount" winapi)
                        int32 GetTickCountManaged() cil managed preservesig
                    {
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "GetTickCountManaged");
            var import = method.GetImport();
            var moduleRef = reader.GetModuleReference(import.Module);

            Assert.True(method.Attributes.HasFlag(MethodAttributes.PinvokeImpl));
            Assert.Equal("GetTickCount", reader.GetString(import.Name));
            Assert.Equal("kernel32.dll", reader.GetString(moduleRef.Name));
        }

        [Fact]
        public void PinvokeMethod_EmitsImportMetadata()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi beforefieldinit NativeMethods extends [mscorlib]System.Object
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
                .Select(reader.GetMethodDefinition)
                .First(definition => reader.GetString(definition.Name) == "GetCurrentProcessId");
            var import = method.GetImport();
            var moduleRef = reader.GetModuleReference(import.Module);

            Assert.True(method.Attributes.HasFlag(MethodAttributes.PinvokeImpl));
            Assert.Equal("kernel32.dll", reader.GetString(moduleRef.Name));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.ModuleRef));
        }

        [Theory]
        [InlineData("nomangle", (int)MethodImportAttributes.ExactSpelling)]
        [InlineData("ansi", (int)MethodImportAttributes.CharSetAnsi)]
        [InlineData("unicode", (int)MethodImportAttributes.CharSetUnicode)]
        [InlineData("autochar", (int)MethodImportAttributes.CharSetAuto)]
        [InlineData("lasterr", (int)MethodImportAttributes.SetLastError)]
        [InlineData("cdecl", (int)MethodImportAttributes.CallingConventionCDecl)]
        [InlineData("stdcall", (int)MethodImportAttributes.CallingConventionStdCall)]
        [InlineData("thiscall", (int)MethodImportAttributes.CallingConventionThisCall)]
        [InlineData("fastcall", (int)MethodImportAttributes.CallingConventionFastCall)]
        [InlineData("bestfit:on", (int)MethodImportAttributes.BestFitMappingEnable)]
        [InlineData("bestfit:off", (int)MethodImportAttributes.BestFitMappingDisable)]
        [InlineData("charmaperror:on", (int)MethodImportAttributes.ThrowOnUnmappableCharEnable)]
        [InlineData("charmaperror:off", (int)MethodImportAttributes.ThrowOnUnmappableCharDisable)]
        [InlineData("flags(0x1234)", 0x1234)]
        public void PinvokeAttribute_EmitsExpectedImportFlags(string attribute, int expectedFlags)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi NativeMethods extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("native.dll" {{attribute}})
                        void M() cil managed preservesig
                    {
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = reader.GetMethodDefinition(Assert.Single(reader.MethodDefinitions));
            var import = method.GetImport();

            Assert.Equal((MethodImportAttributes)expectedFlags, import.Attributes);
            Assert.Equal("M", reader.GetString(method.Name));
            Assert.Equal("M", reader.GetString(import.Name));
            Assert.Equal("native.dll", reader.GetString(reader.GetModuleReference(import.Module).Name));
        }

        [Theory]
        [InlineData("bool", 0x02)]
        [InlineData("int8", 0x03)]
        [InlineData("uint8", 0x04)]
        [InlineData("int16", 0x05)]
        [InlineData("uint16", 0x06)]
        [InlineData("int32", 0x07)]
        [InlineData("uint32", 0x08)]
        [InlineData("int64", 0x09)]
        [InlineData("uint64", 0x0A)]
        [InlineData("float32", 0x0B)]
        [InlineData("float64", 0x0C)]
        [InlineData("currency", 0x0F)]
        [InlineData("bstr", 0x13)]
        [InlineData("lpstr", 0x14)]
        [InlineData("lpwstr", 0x15)]
        [InlineData("lptstr", 0x16)]
        [InlineData("iunknown", 0x19)]
        [InlineData("idispatch", 0x1A)]
        [InlineData("struct", 0x1B)]
        [InlineData("interface", 0x1C)]
        [InlineData("int", 0x1F)]
        [InlineData("uint", 0x20)]
        [InlineData("byvalstr", 0x22)]
        [InlineData("ansi bstr", 0x23)]
        [InlineData("tbstr", 0x24)]
        [InlineData("method", 0x26)]
        [InlineData("as any", 0x28)]
        [InlineData("lpstruct", 0x2B)]
        [InlineData("error", 0x2D)]
        public void MarshalSimpleNativeType_EmitsExpectedDescriptor(string nativeType, int expectedTypeCode)
        {
            var (diagnostics, image) = CompileParameterMarshallingDescriptor(nativeType);
            Assert.Empty(diagnostics);

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobReader descriptor = reader.GetBlobReader(GetOnlyMarshallingDescriptor(reader));

            Assert.Equal((byte)expectedTypeCode, descriptor.ReadByte());
            Assert.Equal(0, descriptor.RemainingBytes);
        }

        [Theory]
        [InlineData("fixed sysstring[5]")]
        [InlineData("fixed array[3] int16")]
        [InlineData("iunknown(iidparam=2)")]
        [InlineData("idispatch(iidparam=3)")]
        [InlineData("interface(iidparam=4)")]
        [InlineData("int32*")]
        [InlineData("int32[]")]
        [InlineData("int32[3]")]
        [InlineData("int32[3+2]")]
        [InlineData("int32[+2]")]
        public void MarshalNativeTypeWithPayload_EmitsExpectedDescriptor(string nativeType)
        {
            var (diagnostics, image) = CompileParameterMarshallingDescriptor(nativeType);
            if (nativeType == "int32*")
            {
                Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.DeprecatedNativeType);
            }
            else
            {
                Assert.Empty(diagnostics);
            }

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobReader descriptor = reader.GetBlobReader(GetOnlyMarshallingDescriptor(reader));

            switch (nativeType)
            {
                case "fixed sysstring[5]":
                    Assert.Equal((byte)UnmanagedType.ByValTStr, descriptor.ReadByte());
                    Assert.Equal(5, descriptor.ReadCompressedInteger());
                    break;

                case "fixed array[3] int16":
                    Assert.Equal((byte)UnmanagedType.ByValArray, descriptor.ReadByte());
                    Assert.Equal(3, descriptor.ReadCompressedInteger());
                    Assert.Equal((byte)UnmanagedType.I2, descriptor.ReadByte());
                    break;

                case "iunknown(iidparam=2)":
                    Assert.Equal((byte)UnmanagedType.IUnknown, descriptor.ReadByte());
                    Assert.Equal(2, descriptor.ReadCompressedInteger());
                    break;

                case "idispatch(iidparam=3)":
                    Assert.Equal((byte)UnmanagedType.IDispatch, descriptor.ReadByte());
                    Assert.Equal(3, descriptor.ReadCompressedInteger());
                    break;

                case "interface(iidparam=4)":
                    Assert.Equal((byte)UnmanagedType.Interface, descriptor.ReadByte());
                    Assert.Equal(4, descriptor.ReadCompressedInteger());
                    break;

                case "int32*":
                    Assert.Equal(0x10, descriptor.ReadByte());
                    Assert.Equal((byte)UnmanagedType.I4, descriptor.ReadByte());
                    break;

                case "int32[]":
                    Assert.Equal((byte)UnmanagedType.LPArray, descriptor.ReadByte());
                    Assert.Equal((byte)UnmanagedType.I4, descriptor.ReadByte());
                    break;

                case "int32[3]":
                    Assert.Equal((byte)UnmanagedType.LPArray, descriptor.ReadByte());
                    Assert.Equal((byte)UnmanagedType.I4, descriptor.ReadByte());
                    Assert.Equal(0, descriptor.ReadCompressedInteger());
                    Assert.Equal(3, descriptor.ReadCompressedInteger());
                    Assert.Equal(0, descriptor.ReadCompressedInteger());
                    break;

                case "int32[3+2]":
                    Assert.Equal((byte)UnmanagedType.LPArray, descriptor.ReadByte());
                    Assert.Equal((byte)UnmanagedType.I4, descriptor.ReadByte());
                    Assert.Equal(2, descriptor.ReadCompressedInteger());
                    Assert.Equal(3, descriptor.ReadCompressedInteger());
                    Assert.Equal(1, descriptor.ReadCompressedInteger());
                    break;

                case "int32[+2]":
                    Assert.Equal((byte)UnmanagedType.LPArray, descriptor.ReadByte());
                    Assert.Equal((byte)UnmanagedType.I4, descriptor.ReadByte());
                    Assert.Equal(2, descriptor.ReadCompressedInteger());
                    break;
            }

            Assert.Equal(0, descriptor.RemainingBytes);
        }

        [Theory]
        [InlineData("custom(\"Marshaller.Type\", \"cookie\")", false)]
        [InlineData("custom(\"guid\", \"native\", \"Marshaller.Type\", \"cookie\")", true)]
        public void MarshalCustomMarshaller_EmitsStructuredDescriptor(
            string nativeType,
            bool usesDeprecatedForm)
        {
            var (diagnostics, image) = CompileParameterMarshallingDescriptor(nativeType);
            if (usesDeprecatedForm)
            {
                Assert.Contains(
                    diagnostics,
                    diagnostic => diagnostic.Id == DiagnosticIds.DeprecatedCustomMarshaller);
            }
            else
            {
                Assert.Empty(diagnostics);
            }

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobReader descriptor = reader.GetBlobReader(GetOnlyMarshallingDescriptor(reader));

            Assert.Equal((byte)UnmanagedType.CustomMarshaler, descriptor.ReadByte());
            if (usesDeprecatedForm)
            {
                Assert.Equal("guid", descriptor.ReadSerializedString());
                Assert.Equal("native", descriptor.ReadSerializedString());
            }
            else
            {
                Assert.Equal(0, descriptor.ReadCompressedInteger());
                Assert.Equal(0, descriptor.ReadCompressedInteger());
            }

            Assert.Equal("Marshaller.Type", descriptor.ReadSerializedString());
            Assert.Equal("cookie", descriptor.ReadSerializedString());
            Assert.Equal(0, descriptor.RemainingBytes);
        }

        [Theory]
        [InlineData("variant", (int)VarEnum.VT_VARIANT)]
        [InlineData("currency", (int)VarEnum.VT_CY)]
        [InlineData("void", (int)VarEnum.VT_VOID)]
        [InlineData("bool", (int)VarEnum.VT_BOOL)]
        [InlineData("int8", (int)VarEnum.VT_I1)]
        [InlineData("int16", (int)VarEnum.VT_I2)]
        [InlineData("int32", (int)VarEnum.VT_I4)]
        [InlineData("int64", (int)VarEnum.VT_I8)]
        [InlineData("float32", (int)VarEnum.VT_R4)]
        [InlineData("float64", (int)VarEnum.VT_R8)]
        [InlineData("uint8", (int)VarEnum.VT_UI1)]
        [InlineData("uint16", (int)VarEnum.VT_UI2)]
        [InlineData("uint32", (int)VarEnum.VT_UI4)]
        [InlineData("uint64", (int)VarEnum.VT_UI8)]
        [InlineData("decimal", (int)VarEnum.VT_DECIMAL)]
        [InlineData("date", (int)VarEnum.VT_DATE)]
        [InlineData("bstr", (int)VarEnum.VT_BSTR)]
        [InlineData("lpstr", (int)VarEnum.VT_LPSTR)]
        [InlineData("lpwstr", (int)VarEnum.VT_LPWSTR)]
        [InlineData("iunknown", (int)VarEnum.VT_UNKNOWN)]
        [InlineData("idispatch", (int)VarEnum.VT_DISPATCH)]
        [InlineData("safearray", (int)VarEnum.VT_SAFEARRAY)]
        [InlineData("int", (int)VarEnum.VT_INT)]
        [InlineData("uint", (int)VarEnum.VT_UINT)]
        [InlineData("error", (int)VarEnum.VT_ERROR)]
        [InlineData("hresult", (int)VarEnum.VT_HRESULT)]
        [InlineData("carray", (int)VarEnum.VT_CARRAY)]
        [InlineData("userdefined", (int)VarEnum.VT_USERDEFINED)]
        [InlineData("record", (int)VarEnum.VT_RECORD)]
        [InlineData("filetime", (int)VarEnum.VT_FILETIME)]
        [InlineData("blob", (int)VarEnum.VT_BLOB)]
        [InlineData("stream", (int)VarEnum.VT_STREAM)]
        [InlineData("storage", (int)VarEnum.VT_STORAGE)]
        [InlineData("streamed_object", (int)VarEnum.VT_STREAMED_OBJECT)]
        [InlineData("stored_object", (int)VarEnum.VT_STORED_OBJECT)]
        [InlineData("blob_object", (int)VarEnum.VT_BLOB_OBJECT)]
        [InlineData("cf", (int)VarEnum.VT_CF)]
        [InlineData("clsid", (int)VarEnum.VT_CLSID)]
        [InlineData("int32[]", (int)(VarEnum.VT_I4 | VarEnum.VT_ARRAY))]
        [InlineData("int32 vector", (int)(VarEnum.VT_I4 | VarEnum.VT_VECTOR))]
        [InlineData("int32&", (int)(VarEnum.VT_I4 | VarEnum.VT_BYREF))]
        public void MarshalSafeArrayVariantType_EmitsExpectedDescriptor(string variantType, int expectedVariantType)
        {
            var (diagnostics, image) = CompileParameterMarshallingDescriptor($"safearray {variantType}");
            Assert.Empty(diagnostics);

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobHandle descriptorHandle = GetOnlyMarshallingDescriptor(reader);
            BlobReader descriptor = reader.GetBlobReader(descriptorHandle);

            Assert.True(
                descriptor.ReadByte() == (byte)UnmanagedType.SafeArray,
                Convert.ToHexString(reader.GetBlobBytes(descriptorHandle)));
            Assert.Equal(expectedVariantType, descriptor.ReadCompressedInteger());
            Assert.Equal(0, descriptor.ReadCompressedInteger());
            Assert.Equal(0, descriptor.RemainingBytes);
        }

        [Theory]
        [InlineData("variant", 0x0E)]
        [InlineData("syschar", 0x0D)]
        [InlineData("void", 0x01)]
        [InlineData("decimal", 0x11)]
        [InlineData("date", 0x12)]
        [InlineData("objectref", 0x18)]
        [InlineData("nested struct", 0x21)]
        public void DeprecatedMarshalNativeType_EmitsDescriptorAndWarning(
            string nativeType,
            int expectedTypeCode)
        {
            var (diagnostics, image) = CompileParameterMarshallingDescriptor(nativeType);
            Assert.Contains(diagnostics, diagnostic => diagnostic.Id == DiagnosticIds.DeprecatedNativeType);

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobReader descriptor = reader.GetBlobReader(GetOnlyMarshallingDescriptor(reader));

            Assert.Equal((byte)expectedTypeCode, descriptor.ReadByte());
            Assert.Equal(0, descriptor.RemainingBytes);
        }

        [Fact]
        public void MarshalSafeArrayWithUserDefinedType_EmitsVariantAndTypeName()
        {
            var (diagnostics, image) =
                CompileParameterMarshallingDescriptor("safearray int32, \"Contoso.Element\"");
            Assert.Empty(diagnostics);

            using var pe = new PEReader(image);
            var reader = pe.GetMetadataReader();
            BlobHandle descriptorHandle = GetOnlyMarshallingDescriptor(reader);
            BlobReader descriptor = reader.GetBlobReader(descriptorHandle);

            Assert.True(
                descriptor.ReadByte() == (byte)UnmanagedType.SafeArray,
                Convert.ToHexString(reader.GetBlobBytes(descriptorHandle)));
            Assert.Equal((int)VarEnum.VT_I4, descriptor.ReadCompressedInteger());
            Assert.Equal("Contoso.Element", descriptor.ReadSerializedString());
            Assert.Equal(0, descriptor.RemainingBytes);
        }

        private static (ImmutableArray<Diagnostic> Diagnostics, ImmutableArray<byte> Image)
            CompileParameterMarshallingDescriptor(string nativeType)
        {
            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi beforefieldinit Test extends [mscorlib]System.Object
                {
                    .method public static void M(object marshal({{nativeType}}) value) cil managed
                    {
                        ret
                    }
                }
                """;

            var compiler = new DocumentCompiler();
            var (diagnostics, result) = compiler.Compile(
                new SourceText(source, "test.il"),
                _ => { Assert.Fail("Expected no includes"); return default; },
                _ => { Assert.Fail("Expected no resources"); return default; },
                new Options());

            Assert.NotNull(result);
            var image = new BlobBuilder();
            result!.Serialize(image);
            return (diagnostics, image.ToImmutableArray());
        }

        private static BlobHandle GetOnlyMarshallingDescriptor(MetadataReader reader)
        {
            var method = reader.GetMethodDefinition(Assert.Single(reader.MethodDefinitions));
            var parameter = reader.GetParameter(Assert.Single(method.GetParameters()));
            Assert.Equal(1, reader.GetTableRowCount(TableIndex.FieldMarshal));
            Assert.Equal("value", reader.GetString(parameter.Name));
            return parameter.GetMarshallingDescriptor();
        }

        private static MethodDefinition GetMethod(MetadataReader reader, string name) =>
            reader.MethodDefinitions
                .Select(reader.GetMethodDefinition)
                .Single(definition => reader.GetString(definition.Name) == name);

        [Fact]
        public void PseudoCustomAttribute_PreserveSig_LowersToImplFlagAndIsDropped()
        {
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M() cil managed
                    {
                        .custom instance void [mscorlib]System.Runtime.InteropServices.PreserveSigAttribute::.ctor() = ( 01 00 00 00 )
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = GetMethod(reader, "M");

            Assert.Equal(MethodImplAttributes.PreserveSig, method.ImplAttributes & MethodImplAttributes.PreserveSig);
            Assert.Empty(method.GetCustomAttributes());
        }

        private static string DllImportSource(string blob) => $$"""
            .assembly extern mscorlib { }
            .assembly test { }
            .module test.dll
            .class public auto ansi Test extends [mscorlib]System.Object
            {
                .method public static void Native() cil managed
                {
                    .custom instance void [mscorlib]System.Runtime.InteropServices.DllImportAttribute::.ctor(string) = {{blob}}
                    ret
                }
            }
            """;

        [Fact]
        public void PseudoCustomAttribute_DllImport_CreatesImplMapAndIsDropped()
        {
            // DllImportAttribute("kernel32.dll") with no named arguments.
            string blob = "( 01 00 0C 6B 65 72 6E 65 6C 33 32 2E 64 6C 6C 00 00 )";

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(DllImportSource(blob), new Options());
            var reader = pe.GetMetadataReader();
            var method = GetMethod(reader, "Native");

            Assert.Equal(MethodAttributes.PinvokeImpl, method.Attributes & MethodAttributes.PinvokeImpl);
            Assert.Equal(MethodImplAttributes.PreserveSig, method.ImplAttributes & MethodImplAttributes.PreserveSig);
            Assert.Empty(method.GetCustomAttributes());

            var import = method.GetImport();
            Assert.Equal("kernel32.dll", reader.GetString(reader.GetModuleReference(import.Module).Name));
            // The entry point defaults to the method name.
            Assert.Equal("Native", reader.GetString(import.Name));
            Assert.Equal(MethodImportAttributes.CallingConventionWinApi, import.Attributes & MethodImportAttributes.CallingConventionMask);
        }

        [Fact]
        public void PseudoCustomAttribute_DllImport_NamedArgumentsSetImportAttributes()
        {
            // DllImportAttribute("kernel32.dll") with EntryPoint = "GetLastError", CharSet = Unicode (3),
            // CallingConvention = Cdecl (2), SetLastError = true, ExactSpelling = true and PreserveSig = false.
            string blob = "( 01 00 0C 6B 65 72 6E 65 6C 33 32 2E 64 6C 6C 06 00 "
                + "53 0E 0A 45 6E 74 72 79 50 6F 69 6E 74 0C 47 65 74 4C 61 73 74 45 72 72 6F 72 "
                + "53 55 26 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 49 6E 74 65 72 6F 70 53 65 72 76 69 63 65 73 2E 43 68 61 72 53 65 74 07 43 68 61 72 53 65 74 03 00 00 00 "
                + "53 55 30 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 49 6E 74 65 72 6F 70 53 65 72 76 69 63 65 73 2E 43 61 6C 6C 69 6E 67 43 6F 6E 76 65 6E 74 69 6F 6E 11 43 61 6C 6C 69 6E 67 43 6F 6E 76 65 6E 74 69 6F 6E 02 00 00 00 "
                + "53 02 0C 53 65 74 4C 61 73 74 45 72 72 6F 72 01 "
                + "53 02 0D 45 78 61 63 74 53 70 65 6C 6C 69 6E 67 01 "
                + "53 02 0B 50 72 65 73 65 72 76 65 53 69 67 00 )";

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(DllImportSource(blob), new Options());
            var reader = pe.GetMetadataReader();
            var method = GetMethod(reader, "Native");
            var import = method.GetImport();

            Assert.Equal("GetLastError", reader.GetString(import.Name));
            Assert.Equal(MethodImportAttributes.CharSetUnicode, import.Attributes & MethodImportAttributes.CharSetMask);
            Assert.Equal(MethodImportAttributes.CallingConventionCDecl, import.Attributes & MethodImportAttributes.CallingConventionMask);
            Assert.Equal(MethodImportAttributes.SetLastError, import.Attributes & MethodImportAttributes.SetLastError);
            Assert.Equal(MethodImportAttributes.ExactSpelling, import.Attributes & MethodImportAttributes.ExactSpelling);
            Assert.Equal(default, method.ImplAttributes & MethodImplAttributes.PreserveSig);
            Assert.Empty(method.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_DllImport_EmptyModuleName_ReportsInvalidValue()
        {
            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(
                DllImportSource("( 01 00 00 00 00 )"),
                new Options());

            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.PseudoCustomAttributeInvalidValue, diagnostic.Id);
        }

        [Fact]
        public void PseudoCustomAttribute_DllImport_ExplicitPinvokeImplWinsButModuleReferenceIsStillCreated()
        {
            // The native assembler emits the ImplMap row for an explicit pinvokeimpl clause in a
            // later phase than the one that applies the attribute, so the clause takes precedence.
            // The attribute's module reference is still resolved, and so is still emitted.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .module test.dll
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static pinvokeimpl("explicit.dll" as "ExplicitEntry" cdecl) void Native() cil managed
                    {
                        .custom instance void [mscorlib]System.Runtime.InteropServices.DllImportAttribute::.ctor(string) = ( 01 00 08 61 74 74 72 2E 64 6C 6C 00 00 )
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var import = GetMethod(reader, "Native").GetImport();

            Assert.Equal("explicit.dll", reader.GetString(reader.GetModuleReference(import.Module).Name));
            Assert.Equal("ExplicitEntry", reader.GetString(import.Name));
            Assert.Equal(MethodImportAttributes.CallingConventionCDecl, import.Attributes & MethodImportAttributes.CallingConventionMask);

            Assert.Contains(
                "attr.dll",
                Enumerable.Range(1, reader.GetTableRowCount(TableIndex.ModuleRef))
                    .Select(rid => reader.GetString(
                        reader.GetModuleReference(MetadataTokens.ModuleReferenceHandle(rid)).Name)));
        }

        [Fact]
        public void PseudoCustomAttribute_MarshalAs_OnParameter_CreatesFieldMarshalAndIsDropped()
        {
            // MarshalAsAttribute(UnmanagedType.Bool) applied to the parameter via .param [1].
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32 value) cil managed
                    {
                        .param [1]
                        .custom instance void [mscorlib]System.Runtime.InteropServices.MarshalAsAttribute::.ctor(int16) = ( 01 00 02 00 00 00 )
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var method = GetMethod(reader, "M");
            var parameter = reader.GetParameter(Assert.Single(method.GetParameters()));

            Assert.Equal(ParameterAttributes.HasFieldMarshal, parameter.Attributes & ParameterAttributes.HasFieldMarshal);
            Assert.Equal([(byte)UnmanagedType.Bool], reader.GetBlobBytes(parameter.GetMarshallingDescriptor()));
            Assert.Empty(parameter.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_MarshalAs_ByValArrayOnField_EncodesSizeAndSubType()
        {
            // MarshalAsAttribute(UnmanagedType.ByValArray) with SizeConst = 4 and ArraySubType = I4.
            string blob = "( 01 00 1E 00 00 00 02 00 "
                + "53 08 09 53 69 7A 65 43 6F 6E 73 74 04 00 00 00 "
                + "53 55 2C 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D 65 2E 49 6E 74 65 72 6F 70 53 65 72 76 69 63 65 73 2E 55 6E 6D 61 6E 61 67 65 64 54 79 70 65 "
                + "0C 41 72 72 61 79 53 75 62 54 79 70 65 07 00 00 00 )";

            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public sequential ansi sealed Test extends [mscorlib]System.ValueType
                {
                    .field public int32[] Values
                    .custom (field int32[] Test::Values) instance void [mscorlib]System.Runtime.InteropServices.MarshalAsAttribute::.ctor(int32) = {{blob}}
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var field = reader.GetFieldDefinition(Assert.Single(reader.FieldDefinitions));

            Assert.Equal(FieldAttributes.HasFieldMarshal, field.Attributes & FieldAttributes.HasFieldMarshal);
            Assert.Equal(
                [(byte)UnmanagedType.ByValArray, 0x04, (byte)UnmanagedType.I4],
                reader.GetBlobBytes(field.GetMarshallingDescriptor()));
            Assert.Empty(field.GetCustomAttributes());
        }

        [Fact]
        public void PseudoCustomAttribute_MarshalAs_ByValArrayOnParameter_ReportsInvalidTarget()
        {
            // UnmanagedType.ByValArray is only valid on fields.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(int32[] value) cil managed
                    {
                        .param [1]
                        .custom instance void [mscorlib]System.Runtime.InteropServices.MarshalAsAttribute::.ctor(int32) = ( 01 00 1E 00 00 00 00 00 )
                        ret
                    }
                }
                """;

            var diagnostics = DocumentCompilerTestHelpers.CompileAndGetDiagnostics(source, new Options());
            var diagnostic = Assert.Single(diagnostics);
            Assert.Equal(DiagnosticIds.PseudoCustomAttributeInvalidTarget, diagnostic.Id);
        }

        [Fact]
        public void PseudoCustomAttribute_MarshalAs_OnProperty_AppliesToAccessorParameters()
        {
            // MarshalAsAttribute(UnmanagedType.Bool) on a property fans out to the getter's return
            // parameter and to the setter's last parameter.
            string source = """
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public instance int32 get_Value() cil managed
                    {
                        ldc.i4.0
                        ret
                    }
                    .method public instance void set_Value(int32 'value') cil managed
                    {
                        ret
                    }
                    .property instance int32 Value()
                    {
                        .custom instance void [mscorlib]System.Runtime.InteropServices.MarshalAsAttribute::.ctor(int16) = ( 01 00 02 00 00 00 )
                        .get instance int32 Test::get_Value()
                        .set instance void Test::set_Value(int32)
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();

            var property = reader.GetPropertyDefinition(Assert.Single(reader.PropertyDefinitions));
            Assert.Empty(property.GetCustomAttributes());

            var getterReturn = reader.GetParameter(GetMethod(reader, "get_Value").GetParameters().Single());
            Assert.Equal(0, getterReturn.SequenceNumber);
            Assert.Equal([(byte)UnmanagedType.Bool], reader.GetBlobBytes(getterReturn.GetMarshallingDescriptor()));

            var setterValue = reader.GetParameter(GetMethod(reader, "set_Value").GetParameters()
                .Single(handle => reader.GetParameter(handle).SequenceNumber == 1));
            Assert.Equal([(byte)UnmanagedType.Bool], reader.GetBlobBytes(setterValue.GetMarshallingDescriptor()));
        }

        [Fact]
        public void PseudoCustomAttribute_MarshalAs_CustomMarshaler_EncodesMarshalerAndCookie()
        {
            // MarshalAsAttribute(UnmanagedType.CustomMarshaler) with MarshalType = "M" and MarshalCookie = "C".
            string blob = "( 01 00 2C 00 00 00 02 00 "
                + "53 0E 0B 4D 61 72 73 68 61 6C 54 79 70 65 01 4D "
                + "53 0E 0D 4D 61 72 73 68 61 6C 43 6F 6F 6B 69 65 01 43 )";

            string source = $$"""
                .assembly extern mscorlib { }
                .assembly test { }
                .class public auto ansi Test extends [mscorlib]System.Object
                {
                    .method public static void M(object value) cil managed
                    {
                        .param [1]
                        .custom instance void [mscorlib]System.Runtime.InteropServices.MarshalAsAttribute::.ctor(int32) = {{blob}}
                        ret
                    }
                }
                """;

            using var pe = DocumentCompilerTestHelpers.CompileAndGetReader(source, new Options());
            var reader = pe.GetMetadataReader();
            var parameter = reader.GetParameter(Assert.Single(GetMethod(reader, "M").GetParameters()));

            // Native type, empty GUID placeholder, empty native type name placeholder, marshaler name, cookie.
            Assert.Equal(
                [(byte)UnmanagedType.CustomMarshaler, 0x00, 0x00, 0x01, (byte)'M', 0x01, (byte)'C'],
                reader.GetBlobBytes(parameter.GetMarshallingDescriptor()));
        }
    }
}
