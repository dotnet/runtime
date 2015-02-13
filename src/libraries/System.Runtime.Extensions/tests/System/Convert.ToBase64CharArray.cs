// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public class Co8641ToBase64CharArray_btArr_ii_chArr_i
{
    [Fact]
    public static void runTest()
    {
        /////////////////////////  START TESTS ////////////////////////////
        ///////////////////////////////////////////////////////////////////

        //[]Vanila - The base 64 digits in ascending order from zero are the uppercase characters 'A' to 'Z', lowercase characters 'a' to 'z', 
        //numerals '0' to '9', and the symbols '+' and '/'. The valueless character, '=', is used for trailing padding
        //First, we will test ToBase64String(Byte[])

        /***
                    The encoding process represents 24-bit groups of input bits as output
                       strings of 4 encoded characters.  Proceeding from left to right, a
                       24-bit input group is formed by concatenating 3 8bit input groups.
                       These 24 bits are then treated as 4 concatenated 6-bit groups, each
                       of which is translated into a single digit in the base64 alphabet.
                       When encoding a bit stream via the base64 encoding, the bit stream
                       must be presumed to be ordered with the most-significant-bit first.
                       That is, the first bit in the stream will be the high-order bit in
                       the first 8bit byte, and the eighth bit will be the low-order bit in
                       the first 8bit byte, and so on.

                       Each 6-bit group is used as an index into an array of 64 printable
                       characters.  The character referenced by the index is placed in the
                       output string.  These characters, identified in Table 1, below, are
                       selected so as to be universally representable, and the set excludes
                       characters with particular significance to SMTP (e.g., ".", CR, LF)
                       and to the multipart boundary delimiters defined in RFC 2046 (e.g.,
                       "-").

                            Table 1: The Base64 Alphabet

             Value Encoding  Value Encoding  Value Encoding  Value Encoding
                 0 A            17 R            34 i            51 z
                 1 B            18 S            35 j            52 0
                 2 C            19 T            36 k            53 1
                 3 D            20 U            37 l            54 2
                 4 E            21 V            38 m            55 3
                 5 F            22 W            39 n            56 4
                 6 G            23 X            40 o            57 5
                 7 H            24 Y            41 p            58 6
                 8 I            25 Z            42 q            59 7
                 9 J            26 a            43 r            60 8
                10 K            27 b            44 s            61 9
                11 L            28 c            45 t            62 +
                12 M            29 d            46 u            63 /
                13 N            30 e            47 v
                14 O            31 f            48 w         (pad) =
                15 P            32 g            49 x
                16 Q            33 h            50 y

        ***/
        string str1 = "test";
        char[] chars = str1.ToCharArray();
        byte[] bits = Convert.FromBase64CharArray(chars, 0, chars.Length);

        chars = new Char[4];

        int returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length, chars, 0);
        Assert.Equal(str1.Length, returnValue);
        Assert.Equal(str1, new String(chars));

        //[]Not reading the whole string
        str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = new Char[4];
        returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length - 1, chars, 0);
        Assert.Equal(str1.Length, returnValue);
        Assert.Equal((new String(chars)).Substring(0, 3), str1.Substring(0, 3));

        // NDPWhidbey bug #20933 - short input array may throw exception
        char[] temp = new char[4];
        byte[] inputBuffer = new byte[] { (byte)'a', (byte)'b', (byte)'c' };
        Convert.ToBase64CharArray(inputBuffer, 0, 3, temp, 0);
        Convert.ToBase64CharArray(inputBuffer, 0, 2, temp, 0);

        // VSWhidbey bug #103157 - offsetOut parameter was ignored
        String s = "........";
        chars = s.ToCharArray();
        byte[] bytes = new byte[6];
        for (int i = 0; i < bytes.Length; bytes[i] = (byte)i++) ;

        int c = 0;
        // Convert first half of the byte array, write to first half of char array
        c = Convert.ToBase64CharArray(bytes, 0, 3, chars, 0);
        Assert.Equal(4, c);
        Assert.Equal("AAEC....", new String(chars));

        // Convert second half of the byte array, write to second half of char array.
        c = Convert.ToBase64CharArray(bytes, 3, 3, chars, 4);
        Assert.Equal(4, c);
        Assert.Equal("AAECAwQF", new String(chars));
    }

    [Fact]
    public static void runTest_Negative()
    {
        int returnValue;
        //[] parms - if Byte[] is null
        byte[] bits = null;
        Char[] chars = new Char[1];
        Assert.Throws<ArgumentNullException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, 1, chars, 0));

        //[] parms - if Char[] is null
        string str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = null;
        Assert.Throws<ArgumentNullException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length, chars, 0));

        //[] parms - int parms

        str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = new Char[4];

        Int32[] negativeNumbers = new Int32[]
            {
                -580211910, -1301763964, -1114274334, -484101405, -234109782, -1945711799, -598646168,
                -1589786299, -19199566, -895444420, -1207731394, -1382096580, -1170653708, -836346455, -1866732604,
                -1601915819, -1518260224, -1983761395, -1706826589, -255553951, Int32.MinValue
            };

        for (int i = 0; i < negativeNumbers.Length; i++)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, negativeNumbers[i], bits.Length, chars, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, negativeNumbers[i], chars, 0));
            // offsetOut is negative
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length, chars, negativeNumbers[i]));
        }

        str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = new Char[4];

        Int32[] positiveNumbers = new Int32[]
            {
                987060368, 2051597101, 456045065, 638925134, 81065981, 1338449972, 1179281288, 77476406, 1679264303, 1191885711,
                1743940135, 873187169, 950191869, 179426799, 1032089466, 813931898, 2109084534, 204677719, 356595643, 1311812948,
                100, Int32.MaxValue, (bits.Length + 1)
            };

        for (int i = 0; i < positiveNumbers.Length; i++)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, positiveNumbers[i], chars, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, positiveNumbers[i], bits.Length, chars, 0));
            // offsetOut is too large
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length, chars, positiveNumbers[i]));
        }

        str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = new Char[4];

        Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 1, bits.Length, chars, 0));

        // offsetOut is invalid
        str1 = "test";
        chars = str1.ToCharArray();
        bits = Convert.FromBase64CharArray(chars, 0, chars.Length);
        chars = new Char[4];
        Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.ToBase64CharArray(bits, 0, bits.Length, chars, 1));
    }
}
