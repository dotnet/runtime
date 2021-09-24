// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class PreserveSig
        {
            public partial class False
            {
                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_return", PreserveSig = false)]
                public static partial void NoReturnValue(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int", PreserveSig = false)]
                public static partial void Int_Out(int i, out int ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int", PreserveSig = false)]
                public static partial int Int_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int", PreserveSig = false)]
                public static partial void Bool_Out(int i, [MarshalAs(UnmanagedType.U4)] out bool ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int", PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.U4)]
                public static partial bool Bool_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort", PreserveSig = false)]
                public static partial void Char_Out(int i, [MarshalAs(UnmanagedType.U2)] out char ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort", PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.U2)]
                public static partial char Char_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort_string", PreserveSig = false)]
                public static partial void String_Out(int i, [MarshalAs(UnmanagedType.LPWStr)] out string ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort_string", PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.LPWStr)]
                public static partial string String_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int_array", PreserveSig = false)]
                public static partial void IntArray_Out(int i, [MarshalAs(UnmanagedType.LPArray, SizeConst = sizeof(int))] out int[] ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int_array", PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.LPArray, SizeConst = sizeof(int))]
                public static partial int[] IntArray_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort_string_array", PreserveSig = false)]
                public static partial void StringArray_Out(int i, [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeConst = sizeof(int))] out string[] ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_ushort_string_array", PreserveSig = false)]
                [return: MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeConst = sizeof(int))]
                public static partial string[] StringArray_AsReturn(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_handle", PreserveSig = false)]
                public static partial void SafeHandle_Out(int hr, out DummySafeHandle ret);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_handle", PreserveSig = false)]
                public static partial DummySafeHandle SafeHandle_AsReturn(int hr);

            }

            public class DummySafeHandle : Microsoft.Win32.SafeHandles.SafeHandleMinusOneIsInvalid
            {
                private DummySafeHandle() : base(ownsHandle: true) { }
                protected override bool ReleaseHandle() => true;
            }

            public partial class True
            {
                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_return", PreserveSig = true)]
                public static partial int NoReturnValue(int i);

                [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "hresult_out_int", PreserveSig = true)]
                public static partial int Int_Out(int i, out int ret);
            }
        }
    }

    public class PreserveSigTests
    {
        private const int E_INVALIDARG = unchecked((int)0x80070057);
        private const int COR_E_NOTSUPPORTED = unchecked((int)0x80131515);
        private const int S_OK = 0;
        private const int S_FALSE = 1;

        [Theory]
        [InlineData(E_INVALIDARG)]
        [InlineData(COR_E_NOTSUPPORTED)]
        [InlineData(-1)]
        public void PreserveSigFalse_Error(int input)
        {
            Exception exception = Marshal.GetExceptionForHR(input);
            Assert.NotNull(exception);

            int expectedHR = input;
            var exceptionType = exception.GetType();
            Assert.Equal(expectedHR, exception.HResult);
            Exception ex;
                
            ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.NoReturnValue(input));
            Assert.Equal(expectedHR, ex.HResult);

            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Int_Out(input, out int ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Int_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Bool_Out(input, out bool ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Bool_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Char_Out(input, out char ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.Char_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.String_Out(input, out string ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.String_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.IntArray_Out(input, out int[] ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.IntArray_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.StringArray_Out(input, out string[] ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.StringArray_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
            {
                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.SafeHandle_Out(input, out NativeExportsNE.PreserveSig.DummySafeHandle ret));
                Assert.Equal(expectedHR, ex.HResult);

                ex = Assert.Throws(exceptionType, () => NativeExportsNE.PreserveSig.False.SafeHandle_AsReturn(input));
                Assert.Equal(expectedHR, ex.HResult);
            }
        }

        [Theory]
        [InlineData(S_OK)]
        [InlineData(S_FALSE)]
        [InlineData(10)]
        public void PreserveSigFalse_Success(int input)
        {
            Assert.True(input >= 0);

            NativeExportsNE.PreserveSig.False.NoReturnValue(input);

            {
                int expected = input;

                int ret;
                NativeExportsNE.PreserveSig.False.Int_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.Int_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                bool expected = input != 0;

                bool ret;
                NativeExportsNE.PreserveSig.False.Bool_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.Bool_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                char expected = (char)input;

                char ret;
                NativeExportsNE.PreserveSig.False.Char_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.Char_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                string expected = input.ToString();

                string ret;
                NativeExportsNE.PreserveSig.False.String_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.String_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                int[] expected = new int[sizeof(int)];
                Array.Fill(expected, input);

                int[] ret;
                NativeExportsNE.PreserveSig.False.IntArray_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.IntArray_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                string[] expected = new string[sizeof(int)];
                Array.Fill(expected, input.ToString());

                string[] ret;
                NativeExportsNE.PreserveSig.False.StringArray_Out(input, out ret);
                Assert.Equal(expected, ret);

                ret = NativeExportsNE.PreserveSig.False.StringArray_AsReturn(input);
                Assert.Equal(expected, ret);
            }
            {
                nint expected = input;

                NativeExportsNE.PreserveSig.DummySafeHandle ret;
                NativeExportsNE.PreserveSig.False.SafeHandle_Out(input, out ret);
                Assert.Equal(expected, (nint)ret.DangerousGetHandle());
                ret.Dispose();

                ret = NativeExportsNE.PreserveSig.False.SafeHandle_AsReturn(input);
                Assert.Equal(expected, (nint)ret.DangerousGetHandle());
                ret.Dispose();
            }
        }

        [Theory]
        [InlineData(S_OK)]
        [InlineData(S_FALSE)]
        [InlineData(E_INVALIDARG)]
        [InlineData(COR_E_NOTSUPPORTED)]
        [InlineData(-1)]
        public void PreserveSigTrue(int input)
        {
            int expected = input;
            int hr;

            hr = NativeExportsNE.PreserveSig.True.NoReturnValue(input);
            Assert.Equal(expected, hr);

            int ret;
            hr = NativeExportsNE.PreserveSig.True.Int_Out(input, out ret);
            Assert.Equal(expected, hr);
            Assert.Equal(expected, ret);
        }
    }
}
