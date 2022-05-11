// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "bytebool_return_as_uint")]
        public static partial uint ReturnByteBoolAsUInt([MarshalAs(UnmanagedType.U1)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "bytebool_return_as_uint")]
        public static partial uint ReturnSByteBoolAsUInt([MarshalAs(UnmanagedType.I1)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "variantbool_return_as_uint")]
        public static partial uint ReturnVariantBoolAsUInt([MarshalAs(UnmanagedType.VariantBool)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        public static partial uint ReturnIntBoolAsUInt([MarshalAs(UnmanagedType.I4)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        public static partial uint ReturnUIntBoolAsUInt([MarshalAs(UnmanagedType.U4)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        public static partial uint ReturnWinBoolAsUInt([MarshalAs(UnmanagedType.Bool)] bool input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool ReturnUIntAsByteBool(uint input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.VariantBool)]
        public static partial bool ReturnUIntAsVariantBool(uint input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_uint")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static partial bool ReturnUIntAsWinBool(uint input);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsByteBool_Ref(uint input, [MarshalAs(UnmanagedType.U1)] ref bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsByteBool_Out(uint input, [MarshalAs(UnmanagedType.U1)] out bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsByteBool_In(uint input, [MarshalAs(UnmanagedType.U1)] in bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsVariantBool_Ref(uint input, [MarshalAs(UnmanagedType.VariantBool)] ref bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsVariantBool_Out(uint input, [MarshalAs(UnmanagedType.VariantBool)] out bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsVariantBool_In(uint input, [MarshalAs(UnmanagedType.VariantBool)] in bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsWinBool_Ref(uint input, [MarshalAs(UnmanagedType.Bool)] ref bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsWinBool_Out(uint input, [MarshalAs(UnmanagedType.Bool)] out bool res);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "winbool_return_as_refuint")]
        public static partial void ReturnUIntAsWinBool_In(uint input, [MarshalAs(UnmanagedType.Bool)] in bool res);
    }

    public class BooleanTests
    {
        // See definition of Windows' VARIANT_BOOL
        const ushort VARIANT_TRUE = unchecked((ushort)-1);
        const ushort VARIANT_FALSE = 0;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/60649", TestRuntimes.Mono)]
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
            NativeExportsNE.ReturnUIntAsByteBool_Ref(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsByteBool_Out(value, out result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsByteBool_In(value, in result);
            Assert.Equal(!expected, result); // Should not be updated when using 'in'
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
            NativeExportsNE.ReturnUIntAsVariantBool_Ref(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsVariantBool_Out(value, out result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsVariantBool_In(value, in result);
            Assert.Equal(!expected, result); // Should not be updated when using 'in'
        }

        [Theory]
        [InlineData(new object[] { 0, false })]
        [InlineData(new object[] { 1, true })]
        [InlineData(new object[] { 37, true })]
        [InlineData(new object[] { 0xffffffff, true })]
        [InlineData(new object[] { 0x80000000, true })]
        public void ValidateWinBoolReturns(uint value, bool expected)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUIntAsWinBool(value));

            bool result = !expected;
            NativeExportsNE.ReturnUIntAsWinBool_Ref(value, ref result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsWinBool_Out(value, out result);
            Assert.Equal(expected, result);

            result = !expected;
            NativeExportsNE.ReturnUIntAsWinBool_In(value, in result);
            Assert.Equal(!expected, result); // Should not be updated when using 'in'
        }
    }
}
