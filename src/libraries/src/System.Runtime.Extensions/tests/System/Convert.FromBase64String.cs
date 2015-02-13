// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Text;
using Xunit;

public class Co8640FromBase64String_str
{
    [Fact]
    public static void runTest()
    {
        //////////// Global Variables used for all tests

        String str1;
        String str2;
        Byte[] returnValue;
        UInt32 threeByteRep;
        String returnString;

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
        returnValue = Convert.FromBase64String(str1);

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

        //[] parms - BUT if string is empty, then OK - NDPWhidbey 11468

        str1 = "";
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(0, returnValue.Length);

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
        // intepration
        // If there is only 1 = sign, there will be only 2 bytes in the stream
        // if there are 2 = sign, there will be only 1 byte in the stream
        // if there are more than 2 =, this cannot be represented in the stream

        //testing with 1 trailing '='

        str1 = "abc=";
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(2, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str1, Convert.ToBase64String(returnValue));

        //We can demostrate the failing of the 3rd character by using this string!!!
        str1 = "789=";
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(2, returnValue.Length);

        //Now, we cannot decode the string correctly
        returnString = Convert.ToBase64String(returnValue);
        Assert.NotEqual(str1, returnString);

        //But the first 2 characters should be correct
        Assert.Equal(str1.Substring(0, 2), returnString.Substring(0, 2));

        //[]We should correctly handle with 2 = characters
        str1 = "ab==";
        returnValue = Convert.FromBase64String(str1);
        //there will be only 1 byte in the array
        Assert.Equal(1, returnValue.Length);

        //Now, we cannot decode the string correctly
        returnString = Convert.ToBase64String(returnValue);
        Assert.NotEqual(str1, returnString);

        //But the first 1 character should be correct
        Assert.Equal(str1.Substring(0, 1), returnString.Substring(0, 1));

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after = and before
        str1 = "abc= \t \r\n =";
        returnValue = Convert.FromBase64String(str1);

        //we'll make sure of above by inserting blank characters in the middle
        str1 = "te  st";
        returnValue = Convert.FromBase64String(str1);

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
        Assert.Equal("test", Convert.ToBase64String(returnValue));

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after =
        str1 = "abc=  \t\n\t\r ";
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(str1.Trim(), Convert.ToBase64String(returnValue));

        //[] we should parse if there is any trailing space, '\t', '\n' and 'r' after =
        str1 = "abc \r\n\t   =  \t\n\t\r ";
        returnValue = Convert.FromBase64String(str1);

        Assert.Equal("abc=", Convert.ToBase64String(returnValue));

        //[]Is there a limit on the length of the string for this encoding?

        builder = new StringBuilder();
        for (int i = 0; i < 10000; i++)
            builder.Append('a');

        str1 = builder.ToString();
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(str1, Convert.ToBase64String(returnValue));

        //[]Bug 86920
        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1453, ' ');
        str1 = str1.PadLeft(1605, ' ');
        str1 = str1.Insert(str1.IndexOf('e'), new String(' ', 1543));
        returnValue = Convert.FromBase64String(str1);
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
        str1 = str1.PadLeft(1871, (Char)9);
        str1 = str1.PadLeft(1007, (Char)9);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)9, 1829));
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1299, (Char)10);
        str1 = str1.PadLeft(1883, (Char)10);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)10, 1091));
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        str1 = "test";
        str2 = str1;
        str1 = str1.PadLeft(1633, (Char)13);
        str1 = str1.PadLeft(1888, (Char)13);
        str1 = str1.Insert(str1.IndexOf('e'), new String((Char)13, 1344));
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(3, returnValue.Length);

        //amazingly, we can still get the original string!!!!
        Assert.Equal(str2, Convert.ToBase64String(returnValue));

        // VSWhidbey bug #103109 - empty string should succeed
        str1 = String.Empty;
        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(0, returnValue.Length);

        // VSWhidbey bug #155629 - whitespace-only should succeed
        str1 = "    ";

        returnValue = Convert.FromBase64String(str1);
        Assert.Equal(0, returnValue.Length);
        ///////////////////////////////////////////////////////////////////
        /////////////////////////// END TESTS /////////////////////////////
    }
    [Fact]
    public static void runTest_negative()
    {
        //[] parms - if string is null
        string str1 = null;
        byte[] returnValue = null;
        StringBuilder builder = new StringBuilder();
        int count = 0;
        Assert.Throws<ArgumentNullException>(() => returnValue = Convert.FromBase64String(str1));

        //[] parms - if string is less than 4 characters

        str1 = "No";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[] parms - if string is not multiple of 4 characters
        str1 = "NoMore";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[] parms - if string does not contain valid characters

        str1 = "2-34";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

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
            count = 0;
            builder = new StringBuilder("abc");
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
            Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));
        }

        //[] we should throw if the = character is in the middle
        //UPDATE: 2001/9/22 aha!! bug 86920 states that whitespace should be allowed but this string now fails due to the length problem!!

        str1 = "No=n";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[] we should throw if there are more than 3 = character in the string
        str1 = "abc=====";

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[] we should throw if there are 3 = character in the string
        str1 = "a===";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[] we should throw if there are 3 = character in the string, after removing space, '\t', '\n' and '\r'
        str1 = "a===\r  \t  \n";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        //[]Bug 97500 try adding some = characters in a valid range - "abcdabc=abcd"

        str1 = "abcdabc=abcd";

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        str1 = "abcdab==abcd";

        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        str1 = "abcda===abcd";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));

        str1 = "abcd====abcd";
        Assert.Throws<FormatException>(() => returnValue = Convert.FromBase64String(str1));
    }
}

