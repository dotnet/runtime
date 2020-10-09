using System.Collections.Generic;
using System.Runtime.InteropServices;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bytebool_return_as_uint")]
        public static partial uint ReturnByteBoolAsUInt([MarshalAs(UnmanagedType.U1)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bytebool_return_as_uint")]
        public static partial uint ReturnSByteBoolAsUInt([MarshalAs(UnmanagedType.I1)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "variantbool_return_as_uint")]
        public static partial uint ReturnVariantBoolAsUInt([MarshalAs(UnmanagedType.VariantBool)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        public static partial uint ReturnIntBoolAsUInt([MarshalAs(UnmanagedType.I4)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        public static partial uint ReturnUIntBoolAsUInt([MarshalAs(UnmanagedType.U4)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        public static partial uint ReturnWinBoolAsUInt([MarshalAs(UnmanagedType.Bool)] bool input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool ReturnUIntAsByteBool(uint input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        public static partial bool ReturnUIntAsVariantBool(uint input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReturnUIntAsWinBool(uint input);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsRefByteBool(uint input, [MarshalAs(UnmanagedType.U1)] ref bool res);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsOutByteBool(uint input, [MarshalAs(UnmanagedType.U1)] out bool res);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsRefVariantBool(uint input, [MarshalAs(UnmanagedType.VariantBool)] ref bool res);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsOutVariantBool(uint input, [MarshalAs(UnmanagedType.VariantBool)] out bool res);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsRefWinBool(uint input, [MarshalAs(UnmanagedType.Bool)] ref bool res);

        [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "bool_return_as_refuint")]
        public static partial void ReturnUIntAsOutWinBool(uint input, [MarshalAs(UnmanagedType.Bool)] out bool res);
    }

    public class BooleanTests
    {
        // See definition of Windows' VARIANT_BOOL
        const ushort VARIANT_TRUE = unchecked((ushort)-1);
        const ushort VARIANT_FALSE = 0;

        [Fact]
        public void ValidateBoolIsMarshalledAsExpected()
        {
            Assert.Equal((uint)1, NativeExportsNE.ReturnByteBoolAsUInt(true));
            Assert.Equal((uint)0, NativeExportsNE.ReturnByteBoolAsUInt(false));
            Assert.Equal((uint)1, NativeExportsNE.ReturnSByteBoolAsUInt(true));
            Assert.Equal((uint)0, NativeExportsNE.ReturnSByteBoolAsUInt(false));
            Assert.Equal(VARIANT_TRUE, NativeExportsNE.ReturnVariantBoolAsUInt(true));
            Assert.Equal(VARIANT_FALSE, NativeExportsNE.ReturnVariantBoolAsUInt(false));
            Assert.Equal((uint)1, NativeExportsNE.ReturnIntBoolAsUInt(true));
            Assert.Equal((uint)0, NativeExportsNE.ReturnIntBoolAsUInt(false));
            Assert.Equal((uint)1, NativeExportsNE.ReturnUIntBoolAsUInt(true));
            Assert.Equal((uint)0, NativeExportsNE.ReturnUIntBoolAsUInt(false));
            Assert.Equal((uint)1, NativeExportsNE.ReturnWinBoolAsUInt(true));
            Assert.Equal((uint)0, NativeExportsNE.ReturnWinBoolAsUInt(false));
        }

        [Theory]
        [InlineData(new object[] { 0, false })]
        [InlineData(new object[] { 1, true })]
        [InlineData(new object[] { 37, true })]
        [InlineData(new object[] { 0xff, true })]
        [InlineData(new object[] { 0xffffff00, false })]
        public void ValidateByteBoolReturns(uint value, bool expected)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUIntAsByteBool(value));

            bool result = !expected;
            NativeExportsNE.ReturnUIntAsRefByteBool(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsOutByteBool(value, out result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(new object[] { 0, false })]
        [InlineData(new object[] { 1, false })]
        [InlineData(new object[] { 0xff, false })]
        [InlineData(new object[] { VARIANT_TRUE, true })]
        [InlineData(new object[] { 0xffffffff, true })]
        [InlineData(new object[] { 0xffff0000, false })]
        public void ValidateVariantBoolReturns(uint value, bool expected)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUIntAsVariantBool(value));

            bool result = !expected;
            NativeExportsNE.ReturnUIntAsRefVariantBool(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsOutVariantBool(value, out result);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(new object[] { 0, false })]
        [InlineData(new object[] { 1, true})]
        [InlineData(new object[] { 37, true })]
        [InlineData(new object[] { 0xffffffff, true })]
        [InlineData(new object[] { 0x80000000, true })]
        public void ValidateWinBoolReturns(uint value, bool expected)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUIntAsWinBool(value));

            bool result = !expected;
            NativeExportsNE.ReturnUIntAsRefWinBool(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsOutWinBool(value, out result);
            Assert.Equal(expected, result);
        }
    }
}
