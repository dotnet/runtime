// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using SharedTypes;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "stringcontainer_deepduplicate")]
        public static partial void DeepDuplicateStrings(StringContainer strings, out StringContainer pStringsOut);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "stringcontainer_reverse_strings")]
        public static partial void ReverseStrings(ref StringContainer strings);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes_as_double")]
        public static partial double GetLongBytesAsDouble([MarshalUsing(typeof(DoubleToLongMarshaler))] double d);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bools")]
        public static partial void NegateBools(
            BoolStruct boolStruct,
            out BoolStruct pBoolStructOut);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bools_ref")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool AndBoolsRef(in BoolStruct boolStruct);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
        public static partial IntWrapper DoubleIntRef(IntWrapper pInt);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "reverse_replace_ref_ushort")]
        public static partial void ReverseReplaceString([MarshalUsing(typeof(Utf16StringMarshaller))] ref string s);

        [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_length_ushort")]
        public static partial int ReturnStringLength([MarshalUsing(typeof(Utf16StringMarshaller))] string s);
    }

    public class CustomMarshallingTests
    {
        [Fact]
        public void NonBlittableStructWithFree()
        {
            var stringContainer = new StringContainer
            {
                str1 = "Foo",
                str2 = "Bar"
            };

            NativeExportsNE.DeepDuplicateStrings(stringContainer, out var stringContainer2);

            Assert.Equal(stringContainer, stringContainer2);
        }

        [Fact]
        public void MarshalUsing()
        {
            double d = 1234.56789;

            Assert.Equal(d, NativeExportsNE.GetLongBytesAsDouble(d));
        }

        [Fact]
        public void NonBlittableStructWithoutAllocation()
        {
            var boolStruct = new BoolStruct
            {
                b1 = true,
                b2 = false,
                b3 = true
            };

            NativeExportsNE.NegateBools(boolStruct, out BoolStruct boolStructNegated);

            Assert.Equal(!boolStruct.b1, boolStructNegated.b1);
            Assert.Equal(!boolStruct.b2, boolStructNegated.b2);
            Assert.Equal(!boolStruct.b3, boolStructNegated.b3);
        }

        [Fact]
        public void GetPinnableReferenceMarshalling()
        {
            int originalValue = 42;
            var wrapper = new IntWrapper { i = originalValue };

            var retVal = NativeExportsNE.DoubleIntRef(wrapper);

            Assert.Equal(originalValue * 2, wrapper.i);
            Assert.Equal(originalValue * 2, retVal.i);
        }

        [Fact]
        public void NonBlittableStructRef()
        {
            var stringContainer = new StringContainer
            {
                str1 = "Foo",
                str2 = "Bar"
            };

            var expected = new StringContainer
            {
                str1 = ReverseUTF8Bytes(stringContainer.str1),
                str2 = ReverseUTF8Bytes(stringContainer.str2)
            };

            var stringContainerCopy = stringContainer;

            NativeExportsNE.ReverseStrings(ref stringContainerCopy);

            Assert.Equal(expected, stringContainerCopy);
        }

        [Theory]
        [InlineData(true, true, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        [InlineData(false, false, false)]
        public void NonBlittableStructIn(bool b1, bool b2, bool b3)
        {
            var container = new BoolStruct
            {
                b1 = b1,
                b2 = b2,
                b3 = b3
            };

            Assert.Equal(b1 && b2 && b3, NativeExportsNE.AndBoolsRef(container));
        }

        [Fact]
        public void NonBlittableStructStackallocPinnableNativeMarshalling()
        {
            string str = "Hello world!";
            Assert.Equal(str.Length, NativeExportsNE.ReturnStringLength(str));
        }

        [Fact]
        public void NonBlittableStructPinnableMarshalerPassByRef()
        {
            string str = "Hello world!";
            string expected = ReverseChars(str);
            NativeExportsNE.ReverseReplaceString(ref str);
            Assert.Equal(expected, str);
        }

        private static string ReverseChars(string value)
        {
            if (value == null)
                return null;

            var chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static string ReverseUTF8Bytes(string value)
        {
            if (value == null)
                return null;

            byte[] bytes = Encoding.UTF8.GetBytes(value);
            Array.Reverse(bytes);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
