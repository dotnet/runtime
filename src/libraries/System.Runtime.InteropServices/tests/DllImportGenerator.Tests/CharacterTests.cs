// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using Xunit;

namespace DllImportGenerator.IntegrationTests
{
    partial class NativeExportsNE
    {
        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "unicode_return_as_uint", CharSet = CharSet.Unicode)]
        public static partial uint ReturnUnicodeAsUInt(char input);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_uint", CharSet = CharSet.Unicode)]
        public static partial char ReturnUIntAsUnicode(uint input);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_refuint", CharSet = CharSet.Unicode)]
        public static partial void ReturnUIntAsUnicode_Ref(uint input, ref char res);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_refuint", CharSet = CharSet.Unicode)]
        public static partial void ReturnUIntAsUnicode_Out(uint input, out char res);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_refuint", CharSet = CharSet.Unicode)]
        public static partial void ReturnUIntAsUnicode_In(uint input, in char res);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_uint", CharSet = CharSet.None)]
        [return: MarshalAs(UnmanagedType.U2)]
        public static partial char ReturnU2AsU2IgnoreCharSet([MarshalAs(UnmanagedType.U2)] char input);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_return_as_uint", CharSet = CharSet.Ansi)]
        [return: MarshalAs(UnmanagedType.I2)]
        public static partial char ReturnI2AsI2IgnoreCharSet([MarshalAs(UnmanagedType.I2)] char input);

        [GeneratedDllImport(NativeExportsNE_Binary, EntryPoint = "char_reverse_buffer_ref", CharSet = CharSet.Unicode)]
        public static partial void ReverseBuffer(ref char buffer, int len);
    }

    public class CharacterTests
    {
        public static IEnumerable<object[]> CharacterMappings()
        {
            yield return new object[] { 'A', 0x41 };
            yield return new object[] { 'E', 0x45 };
            yield return new object[] { 'J', 0x4a };
            yield return new object[] { 'ß', 0xdf };
            yield return new object[] { '✅', 0x2705 };
            yield return new object[] { '鸟', 0x9e1f };
        }

        [Theory]
        [MemberData(nameof(CharacterMappings))]
        public void ValidateUnicodeCharIsMarshalledAsExpected(char value, uint expected)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUnicodeAsUInt(value));
        }

        [Theory]
        [MemberData(nameof(CharacterMappings))]
        public void ValidateUnicodeReturns(char expected, uint value)
        {
            Assert.Equal(expected, NativeExportsNE.ReturnUIntAsUnicode(value));

            char initial = '\u0000';
            char result = initial;
            NativeExportsNE.ReturnUIntAsUnicode_Ref(value, ref result);
            Assert.Equal(expected, result);

            result = initial;
            NativeExportsNE.ReturnUIntAsUnicode_Out(value, out result);
            Assert.Equal(expected, result);

            result = initial;
            NativeExportsNE.ReturnUIntAsUnicode_In(value, in result);
            Assert.Equal(expected, result); // Value is updated even when passed with 'in' keyword (matches built-in system)
        }

        [Theory]
        [MemberData(nameof(CharacterMappings))]
        public void ValidateIgnoreCharSet(char value, uint expectedUInt)
        {
            char expected = (char)expectedUInt;
            Assert.Equal(expected, NativeExportsNE.ReturnU2AsU2IgnoreCharSet(value));
            Assert.Equal(expected, NativeExportsNE.ReturnI2AsI2IgnoreCharSet(value));
        }

        [Fact]
        public void ValidateRefCharAsBuffer()
        {
            char[] chars = CharacterMappings().Select(o => (char)o[0]).ToArray();
            char[] expected = new char[chars.Length];
            Array.Copy(chars, expected, chars.Length);
            Array.Reverse(expected);

            NativeExportsNE.ReverseBuffer(ref MemoryMarshal.GetArrayDataReference(chars), chars.Length);
            Assert.Equal(expected, chars);
        }
    }
}
