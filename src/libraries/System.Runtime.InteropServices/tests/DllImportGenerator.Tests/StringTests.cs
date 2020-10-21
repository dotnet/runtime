using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        public partial class Unicode
        {
            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_ushort", CharSet = CharSet.Unicode)]
            public static partial int ReturnLength(string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_return_ushort", CharSet = CharSet.Unicode)]
            public static partial string Reverse_Return(string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_out_ushort", CharSet = CharSet.Unicode)]
            public static partial void Reverse_Out(string s, out string ret);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort", CharSet = CharSet.Unicode)]
            public static partial void Reverse_Ref(ref string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort", CharSet = CharSet.Unicode)]
            public static partial void Reverse_In(in string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_replace_ref_ushort", CharSet = CharSet.Unicode)]
            public static partial void Reverse_Replace_Ref(ref string s);
        }

        public partial class LPTStr
        {
            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_ushort")]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPTStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_ushort", CharSet = CharSet.None)]
            public static partial int ReturnLength_IgnoreCharSet([MarshalAs(UnmanagedType.LPTStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_return_ushort")]
            [return: MarshalAs(UnmanagedType.LPTStr)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPTStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_out_ushort")]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPTStr)] string s, [MarshalAs(UnmanagedType.LPTStr)] out string ret);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort")]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPTStr)] ref string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort")]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPTStr)] in string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_replace_ref_ushort")]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPTStr)] ref string s);
        }

        public partial class LPWStr
        {
            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_ushort")]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPWStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_ushort", CharSet = CharSet.None)]
            public static partial int ReturnLength_IgnoreCharSet([MarshalAs(UnmanagedType.LPWStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_return_ushort")]
            [return: MarshalAs(UnmanagedType.LPWStr)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPWStr)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_out_ushort")]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPWStr)] string s, [MarshalAs(UnmanagedType.LPWStr)] out string ret);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort")]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_ushort")]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPWStr)] in string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_replace_ref_ushort")]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPWStr)] ref string s);
        }

        public partial class LPUTF8Str
        {
            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_byte")]
            public static partial int ReturnLength([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "return_length_byte", CharSet = CharSet.None)]
            public static partial int ReturnLength_IgnoreCharSet([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_return_byte")]
            [return: MarshalAs(UnmanagedType.LPUTF8Str)]
            public static partial string Reverse_Return([MarshalAs(UnmanagedType.LPUTF8Str)] string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_out_byte")]
            public static partial void Reverse_Out([MarshalAs(UnmanagedType.LPUTF8Str)] string s, [MarshalAs(UnmanagedType.LPUTF8Str)] out string ret);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_byte")]
            public static partial void Reverse_In([MarshalAs(UnmanagedType.LPUTF8Str)] in string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_inplace_ref_byte")]
            public static partial void Reverse_Ref([MarshalAs(UnmanagedType.LPUTF8Str)] ref string s);

            [GeneratedDllImport(nameof(NativeExportsNE), EntryPoint = "reverse_replace_ref_byte")]
            public static partial void Reverse_Replace_Ref([MarshalAs(UnmanagedType.LPUTF8Str)] ref string s);
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
            Assert.Equal(expectedLen, NativeExportsNE.Unicode.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPWStr.ReturnLength(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPTStr.ReturnLength(value));

            Assert.Equal(expectedLen, NativeExportsNE.LPWStr.ReturnLength_IgnoreCharSet(value));
            Assert.Equal(expectedLen, NativeExportsNE.LPTStr.ReturnLength_IgnoreCharSet(value));
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UnicodeStringReturn(string value)
        {
            string expected = ReverseChars(value);

            Assert.Equal(expected, NativeExportsNE.Unicode.Reverse_Return(value));
            Assert.Equal(expected, NativeExportsNE.LPWStr.Reverse_Return(value));
            Assert.Equal(expected, NativeExportsNE.LPTStr.Reverse_Return(value));

            string ret;
            NativeExportsNE.Unicode.Reverse_Out(value, out ret);
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

            NativeExportsNE.Unicode.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            NativeExportsNE.LPWStr.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            NativeExportsNE.LPTStr.Reverse_In(in refValue);
            Assert.Equal(value, refValue); // Should not be updated when using 'in'

            refValue = value;
            NativeExportsNE.Unicode.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPWStr.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPTStr.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.Unicode.Reverse_Replace_Ref(ref refValue);
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
            Assert.Equal(expectedLen, NativeExportsNE.LPUTF8Str.ReturnLength_IgnoreCharSet(value));
        }

        [Theory]
        [MemberData(nameof(UnicodeStrings))]
        public void UTF8StringReturn(string value)
        {
            string expected = ReverseBytes(value, Encoding.UTF8);

            Assert.Equal(expected, NativeExportsNE.LPUTF8Str.Reverse_Return(value));

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

            refValue = value;
            NativeExportsNE.LPUTF8Str.Reverse_Ref(ref refValue);
            Assert.Equal(expected, refValue);

            refValue = value;
            NativeExportsNE.LPUTF8Str.Reverse_Replace_Ref(ref refValue);
            Assert.Equal(expected, refValue);
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
    }
}
