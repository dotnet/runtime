// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Text;
using Xunit;

public class Co8639FromBase64CharArray_chArr_ii
{
    [Fact]
    public static void runTest()
    {
        String str1;
        String str2;
        Char[] chArr;
        Byte[] returnValue;
        UInt32 threeByteRep;

        StringBuilder builder;
        /////////////////////////  START TESTS ////////////////////////////
        ///////////////////////////////////////////////////////////////////

        //[]Vanila - The base 64 digits in ascending order from zero are the uppercase characters 'A' to 'Z', lowercase characters 'a' to 'z', 
        //numerals '0' to '9', and the symbols '+' and '/'. The valueless character, '=', is used for trailing padding

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

        str1 = "test";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);

        //Please read the above description to understand this check			
        Assert.Equal(3, returnValue.Length);
        threeByteRep = (uint)((returnValue[0] << 16) | (returnValue[1] << 8) | returnValue[2]);
        if (((threeByteRep >> 18) != 45)
            || (((threeByteRep << 14) >> 26) != 30)
            || (((threeByteRep << 20) >> 26) != 44)
            || (((threeByteRep << 26) >> 26) != 45)
        )
        {
            Assert.True(false, "Err_834sdg! Unexpected returned result");
        }
        //we will do the round trip as well to make sure!!!!
        Assert.Equal(str1, Convert.ToBase64String(returnValue));

        str1 = "";
        chArr = str1.ToCharArray();

        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);

        if (returnValue == null || returnValue.Length != 0)
        {
            Assert.True(false, "Err_521sra! Unexpected result returned, {0}");
        }

        //[]Strings can have the trailing '=' character - again, from the spec
        /**
        Special processing is performed if fewer than 24 bits are available
           at the end of the data being encoded.  A full encoding quantum is
           always completed at the end of a body.  When fewer than 24 input bits
           are available in an input group, zero bits are added (on the right)
           to form an integral number of 6-bit groups.  Padding at the end of
           the data is performed using the "=" character.  Since all base64
           input is an integral number of octets, only the following cases can
           arise: (1) the final quantum of encoding input is an integral
           multiple of 24 bits; here, the final unit of encoded output will be
           an integral multiple of 4 characters with no "=" padding, (2) the
           final quantum of encoding input is exactly 8 bits; here, the final
           unit of encoded output will be two characters followed by two "="
           padding characters, or (3) the final quantum of encoding input is
           exactly 16 bits; here, the final unit of encoded output will be three
           characters followed by one "=" padding character.

           Because it is used only for padding at the end of the data, the
           occurrence of any "=" characters may be taken as evidence that the
           end of the data has been reached (without truncation in transit).  No
           such assurance is possible, however, when the number of octets
           transmitted was a multiple of three and no "=" characters are
           present.
        **/

        //testing with 1 trailing '='

        str1 = "abc=";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(str1, Convert.ToBase64String(returnValue));

        //[]We should correctly handle with 2 = characters

        str1 = "ab==";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(1, returnValue.Length);

        String returnString = Convert.ToBase64String(returnValue);
        Assert.NotEqual(returnString, str1);

        //But the first 1 character should be correct
        Assert.Equal(str1.Substring(0, 1), returnString.Substring(0, 1));

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after = and before
        str1 = "abc= \t \r\n =";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after =
        str1 = "abc=  \t\n\t\r ";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(str1.Trim(), Convert.ToBase64String(returnValue));

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after =
        str1 = "abc \r\n\t   =  \t\n\t\r ";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal("abc=", Convert.ToBase64String(returnValue));

        //[]Is there a limit on the length of the string for this encoding?
        builder = new StringBuilder();
        for (int i = 0; i < 10000; i++)
            builder.Append('a');

        str1 = builder.ToString();
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(str1, Convert.ToBase64String(returnValue));

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1231, ' ');
        str1 = str1.PadLeft(1001, ' ');
        str1 = str1.Insert(str1.IndexOf('e'), new String(' ', 1654));
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        //[] try other characters we are going to ignore
        //we ignore the following white spaces
        // 9
        // 10
        // 13
        // 32 already tested

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1421, (Char)9);
        str1 = str1.PadLeft(1785, (Char)9);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)9, 1987));
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1354, (Char)10);
        str1 = str1.PadLeft(1875, (Char)10);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)10, 1771));
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1981, (Char)13);
        str1 = str1.PadLeft(1673, (Char)13);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)13, 1870));
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        str1 = "test";
        chArr = str1.ToCharArray();
        returnValue = Convert.FromBase64CharArray(chArr, 0, 0);
        Assert.Equal(0, returnValue.Length);
    }

    [Fact]
    public static void runTest_Negative()
    {
        byte[] returnValue = null;

        //[] parms - if Char[] is null
        Assert.Throws<ArgumentNullException>(() => { returnValue = Convert.FromBase64CharArray(null, 0, 3); });

        //[]parm - int parms

        string str1 = "test";
        char[] chArr = str1.ToCharArray();
        Int32[] negativeNumbers = new Int32[]
            {
                -580211910, -1301763964, -1114274334, -484101405, -234109782, -1945711799, -598646168,
                -1589786299, -19199566, -895444420, -1207731394, -1382096580, -1170653708, -836346455, -1866732604,
                -1601915819, -1518260224, -1983761395, -1706826589, -255553951, Int32.MinValue
            };
        for (int i = 0; i < negativeNumbers.Length; i++)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, negativeNumbers[i], chArr.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, negativeNumbers[i]));
        }

        Int32[] positiveNumbers = new Int32[]
            {
                987060368, 2051597101, 456045065, 638925134, 81065981, 1338449972, 1179281288, 74776406, 1679264303, 1191885711,
                1743940135, 873187169, 950191869, 179426799, 1032089466, 813931898, 2109084534, 204677719, 356595643, 1311812948,
                Int32.MaxValue
            };

        for (int i = 0; i < positiveNumbers.Length; i++)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, positiveNumbers[i], chArr.Length));
            Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, positiveNumbers[i]));
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, (chArr.Length + 5), chArr.Length));

        Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length + 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => returnValue = Convert.FromBase64CharArray(chArr, 1, chArr.Length));

        //[] parms - if string is less than 4 characters
        str1 = "No";
        chArr = str1.ToCharArray();

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[] parms - if string is not multiple of 4 characters
        str1 = "NoMore";
        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[] parms - if string does not contain valid characters
        str1 = "2-34";
        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[]We'll make sure of the invalid characters much more thorougly here by looking at the whole unicode range

        Collection<int> exclusionList = new Collection<int>();
        exclusionList.Add(43);
        for (int i = 47; i <= 57; i++)
            exclusionList.Add(i);
        for (int i = 65; i <= 90; i++)
            exclusionList.Add(i);
        for (int i = 97; i <= 122; i++)
            exclusionList.Add(i);

        int[] numbers = new int[] { 30122, 62608, 13917, 19498, 2473, 40845, 35988, 2281, 51246, 36372, };
        for (int i = 0; i < numbers.Length; i++)
        {
            int count = 0;
            StringBuilder builder = new StringBuilder("abc");
            do
            {
                int iValue = numbers[i];
                if (!exclusionList.Contains(iValue))
                {
                    builder.Insert(1, ((Char)iValue));
                    count++;
                }
            } while (count == 0);

            str1 = builder.ToString();
            Assert.Equal(4, str1.Length);
            chArr = str1.ToCharArray();
            Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));
        }

        //[] we should throw if there are 3 = character in the string
        str1 = "a===";
        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[] we should throw if there are 3 = character in the string, after removing space, '\t', '\n' and '\r'
        str1 = "a===\r  \t  \n";
        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[] we should throw if the = character is in the middle
        str1 = "No=n";
        chArr = str1.ToCharArray();

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[] we should throw if there are more than 3 = character in the string
        str1 = "abc=====";
        chArr = str1.ToCharArray();

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        //[]Bug 97500 try adding some = characters in a valid range - "abcdabc=abcd"

        str1 = "abcdabc=abcd";
        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        str1 = "abcdab==abcd";

        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        str1 = "abcda===abcd";

        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));

        str1 = "abcd====abcd";

        chArr = str1.ToCharArray();
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64CharArray(chArr, 0, chArr.Length));
    }
}

