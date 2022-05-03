// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace LibraryImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        private class EntryPoints
        {
            private const string ReturnLength = "return_length";
            private const string ReverseReturn = "reverse_return";
            private const string ReverseOut = "reverse_out";
            private const string ReverseInplace = "reverse_inplace_ref";
            private const string ReverseReplace = "reverse_replace_ref";

            private const string UShortSuffix = "_ushort";
            private const string ByteSuffix = "_byte";

            public class Byte
            {
                public const string ReturnLength = EntryPoints.ReturnLength + ByteSuffix;
                public const string ReverseReturn = EntryPoints.ReverseReturn + ByteSuffix;
                public const string ReverseOut = EntryPoints.ReverseOut + ByteSuffix;
                public const string ReverseInplace = EntryPoints.ReverseInplace + ByteSuffix;
                public const string ReverseReplace = EntryPoints.ReverseReplace + ByteSuffix;
            }

            public class UShort
            {
                public const string ReturnLength = EntryPoints.ReturnLength + UShortSuffix;
                public const string ReverseReturn = EntryPoints.ReverseReturn + UShortSuffix;
                public const string ReverseOut = EntryPoints.ReverseOut + UShortSuffix;
                public const string ReverseInplace = EntryPoints.ReverseInplace + UShortSuffix;
                public const string ReverseReplace = EntryPoints.ReverseReplace + UShortSuffix;
            }
        }

        public partial class Utf16
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength, StringMarshalling = StringMarshalling.Utf16)]
            public static partial int ReturnLength(string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReturn, StringMarshalling = StringMarshalling.Utf16)]
            public static partial string Reverse_Return(string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseOut, StringMarshalling = StringMarshalling.Utf16)]
            public static partial void Reverse_Out(string s, out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace, StringMarshalling = StringMarshalling.Utf16)]
            public static partial void Reverse_Ref(ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace, StringMarshalling = StringMarshalling.Utf16)]
            public static partial void Reverse_In(in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReplace, StringMarshalling = StringMarshalling.Utf16)]
            public static partial void Reverse_Replace_Ref(ref string s);
        }

        public partial class LPTStr
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength)]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPTStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength, StringMarshalling = StringMarshalling.Utf8)]
            public static partial int ReturnLength_IgnoreStringMarshalling([MarshalAs(UnmanagedType.LPTStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReturn)]
            [return: MarshalAs(UnmanagedType.LPTStr)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPTStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseOut)]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPTStr)] string s, [MarshalAs(UnmanagedType.LPTStr)] out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPTStr)] ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPTStr)] in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPTStr)] ref string s);
        }

        public partial class LPWStr
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength)]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPWStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength, StringMarshalling = StringMarshalling.Utf8)]
            public static partial int ReturnLength_IgnoreStringMarshalling([MarshalAs(UnmanagedType.LPWStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReturn)]
            [return: MarshalAs(UnmanagedType.LPWStr)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPWStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseOut)]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPWStr)] string s, [MarshalAs(UnmanagedType.LPWStr)] out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPWStr)] in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace)]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string s);
        }

        public partial class LPUTF8Str
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength)]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength, StringMarshalling = StringMarshalling.Utf16)]
            public static partial int ReturnLength_IgnoreStringMarshalling([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseReturn)]
            [return: MarshalAs(UnmanagedType.LPUTF8Str)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseOut)]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPUTF8Str)] in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPUTF8Str)] ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPUTF8Str)] ref string s);
        }

        public partial class Utf8
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength, StringMarshalling = StringMarshalling.Utf8)]
            public static partial int ReturnLength(string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseReturn, StringMarshalling = StringMarshalling.Utf8)]
            public static partial string Reverse_Return(string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseOut, StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Reverse_Out(string s, out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace, StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Reverse_Ref(ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace, StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Reverse_In(in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace, StringMarshalling = StringMarshalling.Utf8)]
            public static partial void Reverse_Replace_Ref(ref string s);
        }

        public partial class LPStr
        {
            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength)]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength, StringMarshalling = StringMarshalling.Utf16)]
            public static partial int ReturnLength_IgnoreStringMarshalling([MarshalAs(UnmanagedType.LPStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseReturn)]
            [return: MarshalAs(UnmanagedType.LPStr)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPStr)] string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseOut)]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPStr)] string s, [MarshalAs(UnmanagedType.LPStr)] out string ret);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPStr)] ref string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPStr)] in string s);

            [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace)]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPStr)] ref string s);
        }

        public partial class StringMarshallingCustomType
        {
            public partial class Utf16
            {
                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReturnLength, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial int ReturnLength(string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReturn, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial string Reverse_Return(string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseOut, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial void Reverse_Out(string s, out string ret);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial void Reverse_Ref(ref string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseInplace, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial void Reverse_In(in string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.UShort.ReverseReplace, StringMarshallingCustomType = typeof(Utf16StringMarshaller))]
                public static partial void Reverse_Replace_Ref(ref string s);
            }

            public partial class Utf8
            {
                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReturnLength, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial int ReturnLength(string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseReturn, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial string Reverse_Return(string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseOut, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial void Reverse_Out(string s, out string ret);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial void Reverse_Ref(ref string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseInplace, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial void Reverse_In(in string s);

                [LibraryImport(NativeExportsNE_Binary, EntryPoint = EntryPoints.Byte.ReverseReplace, StringMarshallingCustomType = typeof(Utf8StringMarshaller))]
                public static partial void Reverse_Replace_Ref(ref string s);
            }
        }
    }

    public class StringTests
    {
        public static IEnumerable<object[]> UnicodeStrings() => new []
        {
            new object[] { "ABCdef 123$%^" },
            new object[] { "🍜 !! 🍜 !!"},
            new object[] { "🌲 木 🔥 火 🌾 土 🛡 金 🌊 水" },
            new object[] { "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed vitae posuere mauris, sed ultrices leo. Suspendisse potenti. Mauris enim enim, blandit tincidunt consequat in, varius sit amet neque. Morbi eget porttitor ex. Duis mattis aliquet ante quis imperdiet. Duis sit." },
            new object[] { string.Empty },
            new object[] { null },
        };

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UnicodeStringMarshalledAsExpected(string value)
        {
            int expectedLen = value != null ? value.Length : -1;
            Assert.Equal(expectedLen, NativeExportsNE.Utf16.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPWStr.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPTStr.ReturnLength(value));

            Assert.Equal(expectedLen, NativeExportsNE.LPWStr.ReturnLength_IgnoreStringMarshalling(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPTStr.ReturnLength_IgnoreStringMarshalling(value));
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UnicodeStringReturn(string value)
        {
            string expected = ReverseChars(value);

            Assert.Equal(expected, NativeExportsNE.Utf16.Reverse_Return(value));
            Assert.Equal(expected, NativeExportsNE.LPWStr.Reverse_Return(value));
            Assert.Equal(expected, NativeExportsNE.LPTStr.Reverse_Return(value));

            string ret;
            NativeExportsNE.Utf16.Reverse_Out(value, out ret);
            Assert.Equal(expected, ret);

            ret = null;
            NativeExportsNE.LPWStr.Reverse_Out(value, out ret);
            Assert.Equal(expected, ret);

            ret = null;
            NativeExportsNE.LPWStr.Reverse_Out(value, out ret);
            Assert.Equal(expected, ret);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UnicodeStringByRef(string value)
        {
            string refValue = value;
            string expected = ReverseChars(value);

            NativeExportsNE.Utf16.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            NativeExportsNE.LPWStr.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            NativeExportsNE.LPTStr.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            refValue = value;
            NativeExportsNE.Utf16.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPWStr.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPTStr.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.Utf16.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPWStr.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPTStr.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UTF8StringMarshalledAsExpected(string value)
        {
            int expectedLen = value != null ? Encoding.UTF8.GetByteCount(value) : -1;
            Assert.Equal(expectedLen, NativeExportsNE.LPUTF8Str.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPUTF8Str.ReturnLength_IgnoreStringMarshalling(value));
            Assert.Equal(expectedLen, NativeExportsNE.Utf8.ReturnLength(value));
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UTF8StringReturn(string value)
        {
            string expected = ReverseBytes(value, Encoding.UTF8);

            Assert.Equal(expected, NativeExportsNE.LPUTF8Str.Reverse_Return(value));
            Assert.Equal(expected, NativeExportsNE.Utf8.Reverse_Return(value));

            string ret;
            NativeExportsNE.LPUTF8Str.Reverse_Out(value, out ret);
            Assert.Equal(expected, ret);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UTF8StringByRef(string value)
        {
            string refValue = value;
            string expected = ReverseBytes(value, Encoding.UTF8);

            NativeExportsNE.LPUTF8Str.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            NativeExportsNE.Utf8.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            refValue = value;
            NativeExportsNE.LPUTF8Str.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.Utf8.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPUTF8Str.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.Utf8.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void AnsiStringMarshalledAsExpected(string value)
        {
            int expectedLen = value != null
                ? OperatingSystem.IsWindows() ? GetLengthAnsi(value) : Encoding.UTF8.GetByteCount(value)
                : -1;

            Assert.Equal(expectedLen, NativeExportsNE.LPStr.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPStr.ReturnLength_IgnoreStringMarshalling(value));
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void AnsiStringReturn(string value)
        {
            string expected = OperatingSystem.IsWindows() ? ReverseAnsi(value) : ReverseBytes(value, Encoding.UTF8);

            Assert.Equal(expected, NativeExportsNE.LPStr.Reverse_Return(value));

            string ret;
            NativeExportsNE.LPStr.Reverse_Out(value, out ret);
            Assert.Equal(expected, ret);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void AnsiStringByRef(string value)
        {
            string refValue = value;
            string expected = OperatingSystem.IsWindows() ? ReverseAnsi(value) : ReverseBytes(value, Encoding.UTF8);

            NativeExportsNE.LPStr.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            refValue = value;
            NativeExportsNE.LPStr.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPStr.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void StringMarshallingCustomType_MarshalledAsExpected(string value)
        {
            {
                int expectedLen = value != null ? value.Length : -1;
                Assert.Equal(expectedLen, NativeExportsNE.StringMarshallingCustomType.Utf16.ReturnLength(value));
            }
            {
                int expectedLen = value != null ? Encoding.UTF8.GetByteCount(value) : -1;
                Assert.Equal(expectedLen, NativeExportsNE.StringMarshallingCustomType.Utf8.ReturnLength(value));
            }
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void StringMarshallingCustomType_Return(string value)
        {
            {
                string expected = ReverseChars(value);

                Assert.Equal(expected, NativeExportsNE.StringMarshallingCustomType.Utf16.Reverse_Return(value));

                string ret;
                NativeExportsNE.StringMarshallingCustomType.Utf16.Reverse_Out(value, out ret);
                Assert.Equal(expected, ret);
            }
            {
                string expected = ReverseBytes(value, Encoding.UTF8);

                Assert.Equal(expected, NativeExportsNE.StringMarshallingCustomType.Utf8.Reverse_Return(value));

                string ret;
                NativeExportsNE.StringMarshallingCustomType.Utf8.Reverse_Out(value, out ret);
                Assert.Equal(expected, ret);
            }
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void StringMarshallingCustomType_ByRef(string value)
        {
            {
                string refValue = value;
                string expected = ReverseChars(value);

                NativeExportsNE.StringMarshallingCustomType.Utf16.Reverse_In(in refValue);
                Assert.Equal(value, refValue); // Should not be updated when using 'in'

                refValue = value;
                NativeExportsNE.StringMarshallingCustomType.Utf16.Reverse_Ref(ref refValue);
                Assert.Equal(expected, refValue);

                refValue = value;
                NativeExportsNE.StringMarshallingCustomType.Utf16.Reverse_Replace_Ref(ref refValue);
                Assert.Equal(expected, refValue);
            }
            {
                string refValue = value;
                string expected = ReverseBytes(value, Encoding.UTF8);

                NativeExportsNE.StringMarshallingCustomType.Utf8.Reverse_In(in refValue);
                Assert.Equal(value, refValue); // Should not be updated when using 'in'

                refValue = value;
                NativeExportsNE.StringMarshallingCustomType.Utf8.Reverse_Ref(ref refValue);
                Assert.Equal(expected, refValue);

                refValue = value;
                NativeExportsNE.StringMarshallingCustomType.Utf8.Reverse_Replace_Ref(ref refValue);
                Assert.Equal(expected, refValue);
            }
        }

        private static string ReverseChars(string value)
        {
            if (value == null)
                return null;

            var chars = value.ToCharArray();
            Array.Reverse(chars);
            return new string(chars);
        }

        private static string ReverseBytes(string value, Encoding encoding)
        {
            if (value == null)
                return null;

            byte[] bytes = encoding.GetBytes(value);
            Array.Reverse(bytes);
            return encoding.GetString(bytes);
        }

        public static string ReverseAnsi(string value)
        {
            if (value == null)
                return null;

            string ansi;
            IntPtr ptr = Marshal.StringToCoTaskMemAnsi(value);

            try
            {
                unsafe
                {
                    ansi = new string((sbyte*)ptr.ToPointer());
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }

            return ReverseChars(ansi);
        }

        public static int GetLengthAnsi(string value)
        {
            int len = 0;
            IntPtr ptr = Marshal.StringToCoTaskMemAnsi(value);

            try
            {
                byte nextByte = Marshal.ReadByte(ptr, len);
                while (nextByte != '\0')
                {
                    len++;
                    nextByte = Marshal.ReadByte(ptr, len);
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(ptr);
            }

            return len;
        }
    }
}
