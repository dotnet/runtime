// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using NativeExports;
using SharedTypes;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        internal partial class Stateless
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "stringcontainer_deepduplicate")]
            public static partial void DeepDuplicateStrings(StringContainer strings, out StringContainer pStringsOut);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "stringcontainer_reverse_strings")]
            public static partial void ReverseStrings(ref StringContainer strings);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_long_bytes_as_double")]
            public static partial double GetLongBytesAsDouble([MarshalUsing(typeof(DoubleToLongMarshaller))] double d);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "get_bytes_as_double_big_endian")]
            public static partial double GetBytesAsDoubleBigEndian([MarshalUsing(typeof(DoubleToBytesBigEndianMarshaller))] double d);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bools")]
            public static partial void NegateBools(
                BoolStruct boolStruct,
                out BoolStruct pBoolStructOut);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bools_ref")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool AndBoolsRef(in BoolStruct boolStruct);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
            public static partial IntWrapper DoubleIntRef(IntWrapper pInt);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
            public static partial IntWrapperWithoutGetPinnableReference DoubleIntRef(IntWrapperWithoutGetPinnableReference pInt);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
            [return: MarshalUsing(typeof(IntGuaranteedUnmarshal))]
            public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ExceptionOnUnmarshal))] out int ret);

            [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(IntGuaranteedUnmarshal))]
            public static unsafe class IntGuaranteedUnmarshal
            {
                public static bool ConvertToManagedFinallyCalled = false;
                public static int ConvertToManagedFinally(int unmanaged)
                {
                    ConvertToManagedFinallyCalled = true;
                    return unmanaged;
                }
            }
        }

        internal partial class Stateful
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_return_int")]
            public static partial IntWrapperWithNotification SubtractInts(IntWrapperWithNotification x, IntWrapperWithNotification y);
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "subtract_out_int")]
            public static partial void SubtractInts(IntWrapperWithNotification x, IntWrapperWithNotification y, out IntWrapperWithNotification result);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "negate_bools")]
            public static partial void NegateBools(
                [MarshalUsing(typeof(BoolStructMarshallerStateful))] BoolStruct boolStruct,
                [MarshalUsing(typeof(BoolStructMarshallerStateful))] out BoolStruct pBoolStructOut);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "and_bools_ref")]
            [return: MarshalAs(UnmanagedType.U1)]
            public static partial bool AndBoolsRef([MarshalUsing(typeof(BoolStructMarshallerStateful))] in BoolStruct boolStruct);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
            public static partial IntWrapperWithoutGetPinnableReference DoubleIntRef([MarshalUsing(typeof(IntWrapperWithoutGetPinnableReferenceStatefulMarshaller))] IntWrapperWithoutGetPinnableReference pInt);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
            public static partial IntWrapperWithoutGetPinnableReference DoubleIntRefNoAlloc([MarshalUsing(typeof(IntWrapperWithoutGetPinnableReferenceStatefulNoAllocMarshaller))] IntWrapperWithoutGetPinnableReference pInt);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "double_int_ref")]
            [return: MarshalUsing(typeof(IntWrapperMarshallerStateful))]
            public static partial IntWrapper DoubleIntRef([MarshalUsing(typeof(IntWrapperMarshallerStateful))] IntWrapper pInt);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = "return_zero")]
            [return: MarshalUsing(typeof(IntGuaranteedUnmarshal))]
            public static partial int GuaranteedUnmarshal([MarshalUsing(typeof(ExceptionOnUnmarshal))] out int ret);

            [CustomMarshaller(typeof(int), MarshalMode.ManagedToUnmanagedOut, typeof(IntGuaranteedUnmarshal.Marshaller))]
            public static class IntGuaranteedUnmarshal
            {
                public unsafe struct Marshaller
                {
                    public static bool ToManagedFinallyCalled = false;
                    public int ToManagedFinally()
                    {
                        ToManagedFinallyCalled = true;
                        return 0;
                    }

                    public void FromUnmanaged(int value) { }

                    public void Free() {}
                }
            }
        }

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

            NativeExportsNE.Stateless.DeepDuplicateStrings(stringContainer, out var stringContainer2);

            Assert.Equal(stringContainer, stringContainer2);
        }

        [Fact]
        public void MarshalUsing()
        {
            double d = 1234.56789;

            Assert.Equal(d, NativeExportsNE.Stateless.GetLongBytesAsDouble(d));
        }

        [Fact]
        public void CallerAllocatedBuffer()
        {
            double d = 1234.56789;

            Assert.Equal(d, NativeExportsNE.Stateless.GetBytesAsDoubleBigEndian(d));
        }

        [Fact]
        public void GuaranteedUnmarshal()
        {
            NativeExportsNE.Stateless.IntGuaranteedUnmarshal.ConvertToManagedFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Stateless.GuaranteedUnmarshal(out _));
            Assert.True(NativeExportsNE.Stateless.IntGuaranteedUnmarshal.ConvertToManagedFinallyCalled);

            NativeExportsNE.Stateful.IntGuaranteedUnmarshal.Marshaller.ToManagedFinallyCalled = false;
            Assert.Throws<Exception>(() => NativeExportsNE.Stateful.GuaranteedUnmarshal(out _));
            Assert.True(NativeExportsNE.Stateful.IntGuaranteedUnmarshal.Marshaller.ToManagedFinallyCalled);
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

            NativeExportsNE.Stateless.NegateBools(boolStruct, out BoolStruct boolStructNegated);

            Assert.Equal(!boolStruct.b1, boolStructNegated.b1);
            Assert.Equal(!boolStruct.b2, boolStructNegated.b2);
            Assert.Equal(!boolStruct.b3, boolStructNegated.b3);
        }

        [Fact]
        public void MarshallerStaticGetPinnableReferenceMarshalling()
        {
            int originalValue = 42;
            var wrapper = new IntWrapperWithoutGetPinnableReference { i = originalValue };

            var retVal = NativeExportsNE.Stateless.DoubleIntRef(wrapper);

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

            NativeExportsNE.Stateless.ReverseStrings(ref stringContainerCopy);

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

            Assert.Equal(b1 && b2 && b3, NativeExportsNE.Stateless.AndBoolsRef(container));
        }

        [Fact]
        public void NonBlittableStructStackallocPinnableNativeMarshalling()
        {
            string str = "Hello world!";
            Assert.Equal(str.Length, NativeExportsNE.ReturnStringLength(str));
        }

        [Fact]
        public void NonBlittableStructPinnableMarshallerPassByRef()
        {
            string str = "Hello world!";
            string expected = ReverseChars(str);
            NativeExportsNE.ReverseReplaceString(ref str);
            Assert.Equal(expected, str);
        }

        [Fact]
        public void OnInvokedInNoReturn()
        {
            bool xNotified = false;
            bool yNotified = false;
            IntWrapperWithNotification x = new() { Value = 23 };
            x.InvokeSucceeded += (sender, args) => xNotified = true;
            IntWrapperWithNotification y = new() { Value = 897 };
            y.InvokeSucceeded += (sender, args) => yNotified = true;

            int oldNumInvokeSucceededOnUninitialized = IntWrapperWithNotification.NumInvokeSucceededOnUninitialized;

            int result = NativeExportsNE.Stateful.SubtractInts(x, y).Value;

            Assert.Equal(x.Value - y.Value, result);
            Assert.True(xNotified);
            Assert.True(yNotified);
            Assert.Equal(oldNumInvokeSucceededOnUninitialized, IntWrapperWithNotification.NumInvokeSucceededOnUninitialized);
        }

        [Fact]
        public void OnInvokedInNoOut()
        {
            bool xNotified = false;
            bool yNotified = false;
            IntWrapperWithNotification x = new() { Value = 23 };
            x.InvokeSucceeded += (sender, args) => xNotified = true;
            IntWrapperWithNotification y = new() { Value = 897 };
            y.InvokeSucceeded += (sender, args) => yNotified = true;

            int oldNumInvokeSucceededOnUninitialized = IntWrapperWithNotification.NumInvokeSucceededOnUninitialized;

            NativeExportsNE.Stateful.SubtractInts(x, y, out IntWrapperWithNotification result);

            Assert.Equal(x.Value - y.Value, result.Value);
            Assert.True(xNotified);
            Assert.True(yNotified);
            Assert.Equal(oldNumInvokeSucceededOnUninitialized, IntWrapperWithNotification.NumInvokeSucceededOnUninitialized);
        }

        [Fact]
        public void NonBlittableStructWithoutAllocation_Stateful()
        {
            var boolStruct = new BoolStruct
            {
                b1 = true,
                b2 = false,
                b3 = true
            };

            NativeExportsNE.Stateful.NegateBools(boolStruct, out BoolStruct boolStructNegated);

            Assert.Equal(!boolStruct.b1, boolStructNegated.b1);
            Assert.Equal(!boolStruct.b2, boolStructNegated.b2);
            Assert.Equal(!boolStruct.b3, boolStructNegated.b3);
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
        public void NonBlittableStructIn_Stateful(bool b1, bool b2, bool b3)
        {
            var container = new BoolStruct
            {
                b1 = b1,
                b2 = b2,
                b3 = b3
            };

            Assert.Equal(b1 && b2 && b3, NativeExportsNE.Stateful.AndBoolsRef(container));
        }

        [Fact]
        public void NonBlittableType_Stateful_Marshalling_Free()
        {
            int originalValue = 42;
            var wrapper = new IntWrapper { i = originalValue };

            var retVal = NativeExportsNE.Stateful.DoubleIntRef(wrapper);

            // We don't pin the managed value, so it shouldn't update.
            Assert.Equal(originalValue, wrapper.i);
            Assert.Equal(originalValue * 2, retVal.i);
        }

        [Fact]
        public void StatefulMarshallerStaticGetPinnableReferenceMarshalling()
        {
            int originalValue = 42;
            var wrapper = new IntWrapperWithoutGetPinnableReference { i = originalValue };

            var retVal = NativeExportsNE.Stateful.DoubleIntRef(wrapper);

            Assert.Equal(originalValue * 2, wrapper.i);
            Assert.Equal(originalValue * 2, retVal.i);
        }

        [Fact]
        public void StatefulMarshallerInstanceGetPinnableReferenceMarshalling()
        {
            int originalValue = 42;
            var wrapper = new IntWrapperWithoutGetPinnableReference { i = originalValue };

            var retVal = NativeExportsNE.Stateful.DoubleIntRefNoAlloc(wrapper);

            Assert.Equal(originalValue * 2, wrapper.i);
            Assert.Equal(originalValue * 2, retVal.i);
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
