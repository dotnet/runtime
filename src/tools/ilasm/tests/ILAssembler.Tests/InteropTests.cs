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
    }
}
