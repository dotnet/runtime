// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using Xunit;

namespace StringMarshaling
{
    [OuterLoop]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public class UTF8Tests
    {
        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPUTF8Str)]
        static extern string StringParameterInOut([In, Out][MarshalAs(UnmanagedType.LPUTF8Str)] string s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPUTF8Str)]
        static extern string StringParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)] string s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        static extern void StringParameterRefOut([MarshalAs(UnmanagedType.LPUTF8Str)] out string s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool StringParameterRef([MarshalAs(UnmanagedType.LPUTF8Str)] ref string s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool StringBuilderParameterInOut([In, Out][MarshalAs(UnmanagedType.LPUTF8Str)] StringBuilder s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        static extern void StringBuilderParameterOut([Out][MarshalAs(UnmanagedType.LPUTF8Str)] StringBuilder s, int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPUTF8Str, SizeConst = 512)]
        static extern StringBuilder StringBuilderParameterReturn(int index);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool TestStructWithUtf8Field(Utf8Struct utfStruct);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        static extern void SetStringInStruct(ref Utf8Struct utfStruct, [MarshalAs(UnmanagedType.LPUTF8Str)] string str);

        [DllImport("UTF8TestNative", CallingConvention = CallingConvention.Cdecl)]
        static extern void Utf8DelegateAsParameter(DelegateUTF8Parameter param);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void DelegateUTF8Parameter([MarshalAs(UnmanagedType.LPUTF8Str)] string utf8String, int index);

        public struct Utf8Struct
        {
            [MarshalAs(UnmanagedType.LPUTF8Str)]
            public string FirstName;
            public int index;
        }

        unsafe struct UnmanagedStruct
        {
            public fixed byte psz[8];
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        struct ManagedStruct
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string str;
        }

        public static readonly string[] Utf8Strings =
        {
            "Managed",
            "S\u00EEne kl\u00E2wen durh die wolken sint geslagen",
            "\u0915\u093E\u091A\u0902 \u0936\u0915\u094D\u0928\u094B\u092E\u094D\u092F\u0924\u094D\u0924\u0941\u092E\u094D \u0964 \u0928\u094B\u092A\u0939\u093F\u0928\u0938\u094D\u0924\u093F \u092E\u093E\u092E\u094D",
            "\u6211\u80FD\u541E\u4E0B\u73BB\u7483\u800C\u4E0D\u4F24\u8EAB\u4F53",
            "\u10E6\u10DB\u10D4\u10E0\u10D7\u10E1\u10D8 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4, \u10DC\u10E3\u10D7\u10E3 \u10D9\u10D5\u10DA\u10D0 \u10D3\u10D0\u10DB\u10EE\u10E1\u10DC\u10D0\u10E1 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E1\u10DD\u10E4\u10DA\u10D8\u10E1\u10D0 \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4, \u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10E0\u10DD\u10DB\u10D0\u10E1\u10D0, \u10EA\u10D4\u10EA\u10EE\u10DA\u10E1, \u10EC\u10E7\u10D0\u10DA\u10E1\u10D0 \u10D3\u10D0 \u10DB\u10D8\u10EC\u10D0\u10E1\u10D0, \u10F0\u10D0\u10D4\u10E0\u10D7\u10D0 \u10D7\u10D0\u10DC\u10D0 \u10DB\u10E0\u10DD\u10DB\u10D0\u10E1\u10D0; \u10DB\u10DD\u10DB\u10EA\u10DC\u10D4\u10E1 \u10E4\u10E0\u10D7\u10D4\u10DC\u10D8 \u10D3\u10D0 \u10D0\u10E6\u10D5\u10E4\u10E0\u10D8\u10DC\u10D3\u10D4, \u10DB\u10D8\u10D5\u10F0\u10EE\u10D5\u10D3\u10D4 \u10DB\u10D0\u10E1 \u10E9\u10D4\u10DB\u10E1\u10D0 \u10DC\u10D3\u10DD\u10DB\u10D0\u10E1\u10D0, \u10D3\u10E6\u10D8\u10E1\u10D8\u10D7 \u10D3\u10D0 \u10E6\u10D0\u10DB\u10D8\u10D7 \u10D5\u10F0\u10EE\u10D4\u10D3\u10D5\u10D8\u10D3\u10D4 \u10DB\u10D6\u10D8\u10E1\u10D0 \u10D4\u10DA\u10D5\u10D0\u10D7\u10D0 \u10D9\u10E0\u10D7\u10DD\u10DB\u10D0\u10D0\u10E1\u10D0\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,\u10E8\u10D4\u10DB\u10D5\u10D4\u10D3\u10E0\u10D4,",
            "\u03A4\u03B7 \u03B3\u03BB\u03CE\u03C3\u03C3\u03B1 \u03BC\u03BF\u03C5 \u03AD\u03B4\u03C9\u03C3\u03B1\u03BD \u03B5\u03BB\u03BB\u03B7\u03BD\u03B9\u03BA\u03AE",
            null,
        };

        public static IEnumerable<object[]> Utf8StringsWithIndex()
        {
            for (int i = 0; i < Utf8Strings.Length; i++)
                yield return [Utf8Strings[i], i];
        }

        public static IEnumerable<object[]> NonNullUtf8StringsWithIndex()
        {
            for (int i = 0; i < Utf8Strings.Length - 1; i++)
                yield return [Utf8Strings[i], i];
        }

        [Theory]
        [MemberData(nameof(Utf8StringsWithIndex))]
        public static void TestInOutStringParameter(string orgString, int index)
        {
            string nativeString = StringParameterInOut(orgString, index);
            Assert.Equal(orgString, nativeString);
        }

        [Theory]
        [MemberData(nameof(Utf8StringsWithIndex))]
        public static void TestOutStringParameter(string orgString, int index)
        {
            string nativeString = StringParameterOut(orgString, index);
            Assert.Equal(orgString, nativeString);
        }

        [Theory]
        [MemberData(nameof(NonNullUtf8StringsWithIndex))]
        public static void TestStringPassByOut(string orgString, int index)
        {
            StringParameterRefOut(out string result, index);
            Assert.Equal(orgString, result);
        }

        [Theory]
        [MemberData(nameof(NonNullUtf8StringsWithIndex))]
        public static void TestStringPassByRef(string orgString, int index)
        {
            string copy = new string(orgString.ToCharArray());
            Assert.True(StringParameterRef(ref orgString, index));
            Assert.Equal(copy, orgString);
        }

        [Theory]
        [MemberData(nameof(NonNullUtf8StringsWithIndex))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/123529", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        public static void TestInOutStringBuilderParameter(string expectedString, int index)
        {
            var builder = new StringBuilder(expectedString);
            Assert.True(StringBuilderParameterInOut(builder, index));
            Assert.Equal(expectedString, builder.ToString());
        }

        [Theory]
        [MemberData(nameof(NonNullUtf8StringsWithIndex))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/123529", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        public static void TestOutStringBuilderParameter(string expectedString, int index)
        {
            var builder = new StringBuilder(expectedString.Length);
            StringBuilderParameterOut(builder, index);
            Assert.Equal(expectedString, builder.ToString());
        }

        [Theory]
        [MemberData(nameof(NonNullUtf8StringsWithIndex))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/123529", typeof(TestLibrary.Utilities), nameof(TestLibrary.Utilities.IsNativeAot))]
        public static void TestReturnStringBuilder(string expectedReturn, int index)
        {
            StringBuilder nativeString = StringBuilderParameterReturn(index);
            Assert.Equal(expectedReturn, nativeString.ToString());
        }

        [Theory]
        [MemberData(nameof(Utf8StringsWithIndex))]
        public static void TestStructWithUtf8FieldParameter(string str, int index)
        {
            var utf8Struct = new Utf8Struct { FirstName = str, index = index };
            Assert.True(TestStructWithUtf8Field(utf8Struct));
        }

        [Fact]
        public static void TestSetStringInStruct()
        {
            var utf8Struct = new Utf8Struct();
            string testString = "StructTestString\uD83D\uDE00";
            SetStringInStruct(ref utf8Struct, testString);
            Assert.Equal(testString, utf8Struct.FirstName);
        }

        [Fact]
        public static void TestUTF8DelegateMarshalling()
        {
            Utf8DelegateAsParameter(new DelegateUTF8Parameter(Utf8StringCallback));
        }

        [Fact]
        public static void TestEmptyString()
        {
            Assert.Null(StringParameterInOut(string.Empty, 0));
            Assert.Null(StringParameterOut(string.Empty, 0));
        }

        [Fact]
        public static unsafe void CompareWithUTF8Encoding()
        {
            if (OperatingSystem.IsWindows())
                return;

            UnmanagedStruct ums;
            ums.psz[0] = 0xFF;
            ums.psz[1] = (byte)'a';
            ums.psz[2] = (byte)'b';
            ums.psz[3] = (byte)'c';
            ums.psz[4] = (byte)'d';
            ums.psz[5] = 0;

            IntPtr ptr = (IntPtr)(&ums);
            ManagedStruct ms = Marshal.PtrToStructure<ManagedStruct>(ptr);
            string actual = ms.str;

            UTF8Encoding uTF8Encoding = new UTF8Encoding();
            byte[] b = new byte[5];
            b[0] = 0xFF;
            b[1] = (byte)'a';
            b[2] = (byte)'b';
            b[3] = (byte)'c';
            b[4] = (byte)'d';
            string expected = uTF8Encoding.GetString(b);
            Assert.Equal(expected, actual);
        }

        static void Utf8StringCallback(string nativeString, int index)
        {
            Assert.Equal(0, string.CompareOrdinal(nativeString, Utf8Strings[index]));
        }
    }
}
