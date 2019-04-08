// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Text;
using System.Diagnostics;

namespace System.Globalization
{
    ////////////////////////////////////////////////////////////////////////////
    //
    // Used in HebrewNumber.ParseByChar to maintain the context information (
    // the state in the state machine and current Hebrew number values, etc.)
    // when parsing Hebrew number character by character.
    //
    ////////////////////////////////////////////////////////////////////////////

    internal struct HebrewNumberParsingContext
    {
        // The current state of the state machine for parsing Hebrew numbers.
        internal HebrewNumber.HS state;
        // The current value of the Hebrew number.
        // The final value is determined when state is FoundEndOfHebrewNumber.
        internal int result;

        public HebrewNumberParsingContext(int result)
        {
            // Set the start state of the state machine for parsing Hebrew numbers.
            state = HebrewNumber.HS.Start;
            this.result = result;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    // Please see ParseByChar() for comments about different states defined here.
    //
    ////////////////////////////////////////////////////////////////////////////

    internal enum HebrewNumberParsingState
    {
        InvalidHebrewNumber,
        NotHebrewDigit,
        FoundEndOfHebrewNumber,
        ContinueParsing,
    }

    ////////////////////////////////////////////////////////////////////////////
    //
    // class HebrewNumber
    //
    //  Provides static methods for formatting integer values into
    //  Hebrew text and parsing Hebrew number text.
    //
    //  Limitations:
    //      Parse can only handle value 1 ~ 999.
    //      Append() can only handle 1 ~ 999. If value is greater than 5000,
    //      5000 will be subtracted from the value.
    //
    ////////////////////////////////////////////////////////////////////////////

    internal static class HebrewNumber
    {
        ////////////////////////////////////////////////////////////////////////////
        //
        //  Append
        //
        //  Converts the given number to Hebrew letters according to the numeric
        //  value of each Hebrew letter, appending to the supplied StringBuilder.
        //  Basically, this converts the lunar year and the lunar month to letters.
        //
        //  The character of a year is described by three letters of the Hebrew
        //  alphabet, the first and third giving, respectively, the days of the
        //  weeks on which the New Year occurs and Passover begins, while the
        //  second is the initial of the Hebrew word for defective, normal, or
        //  complete.
        //
        //  Defective Year : Both Heshvan and Kislev are defective (353 or 383 days)
        //  Normal Year    : Heshvan is defective, Kislev is full  (354 or 384 days)
        //  Complete Year  : Both Heshvan and Kislev are full      (355 or 385 days)
        //
        ////////////////////////////////////////////////////////////////////////////

        internal static void Append(StringBuilder outputBuffer, int Number)
        {
            Debug.Assert(outputBuffer != null);
            int outputBufferStartingLength = outputBuffer.Length;

            char cTens = '\x0';
            char cUnits;               // tens and units chars
            int Hundreds, Tens;              // hundreds and tens values

            //
            //  Adjust the number if greater than 5000.
            //
            if (Number > 5000)
            {
                Number -= 5000;
            }

            Debug.Assert(Number > 0 && Number <= 999, "Number is out of range."); ;

            //
            //  Get the Hundreds.
            //
            Hundreds = Number / 100;

            if (Hundreds > 0)
            {
                Number -= Hundreds * 100;
                // \x05e7 = 100
                // \x05e8 = 200
                // \x05e9 = 300
                // \x05ea = 400
                // If the number is greater than 400, use the multiples of 400.
                for (int i = 0; i < (Hundreds / 4); i++)
                {
                    outputBuffer.Append('\x05ea');
                }

                int remains = Hundreds % 4;
                if (remains > 0)
                {
                    outputBuffer.Append((char)((int)'\x05e6' + remains));
                }
            }

            //
            //  Get the Tens.
            //
            Tens = Number / 10;
            Number %= 10;

            switch (Tens)
            {
                case (0):
                    cTens = '\x0';
                    break;
                case (1):
                    cTens = '\x05d9';          // Hebrew Letter Yod
                    break;
                case (2):
                    cTens = '\x05db';          // Hebrew Letter Kaf
                    break;
                case (3):
                    cTens = '\x05dc';          // Hebrew Letter Lamed
                    break;
                case (4):
                    cTens = '\x05de';          // Hebrew Letter Mem
                    break;
                case (5):
                    cTens = '\x05e0';          // Hebrew Letter Nun
                    break;
                case (6):
                    cTens = '\x05e1';          // Hebrew Letter Samekh
                    break;
                case (7):
                    cTens = '\x05e2';          // Hebrew Letter Ayin
                    break;
                case (8):
                    cTens = '\x05e4';          // Hebrew Letter Pe
                    break;
                case (9):
                    cTens = '\x05e6';          // Hebrew Letter Tsadi
                    break;
            }

            //
            //  Get the Units.
            //
            cUnits = (char)(Number > 0 ? ((int)'\x05d0' + Number - 1) : 0);

            if ((cUnits == '\x05d4') &&            // Hebrew Letter He  (5)
                (cTens == '\x05d9'))
            {              // Hebrew Letter Yod (10)
                cUnits = '\x05d5';                 // Hebrew Letter Vav (6)
                cTens = '\x05d8';                 // Hebrew Letter Tet (9)
            }

            if ((cUnits == '\x05d5') &&            // Hebrew Letter Vav (6)
                (cTens == '\x05d9'))
            {               // Hebrew Letter Yod (10)
                cUnits = '\x05d6';                 // Hebrew Letter Zayin (7)
                cTens = '\x05d8';                 // Hebrew Letter Tet (9)
            }

            //
            //  Copy the appropriate info to the given buffer.
            //

            if (cTens != '\x0')
            {
                outputBuffer.Append(cTens);
            }

            if (cUnits != '\x0')
            {
                outputBuffer.Append(cUnits);
            }

            if (outputBuffer.Length - outputBufferStartingLength > 1)
            {
                outputBuffer.Insert(outputBuffer.Length - 1, '"');
            }
            else
            {
                outputBuffer.Append('\'');
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Token used to tokenize a Hebrew word into tokens so that we can use in the
        // state machine.
        //
        ////////////////////////////////////////////////////////////////////////////

        private enum HebrewToken : short
        {
            Invalid = -1,
            Digit400 = 0,
            Digit200_300 = 1,
            Digit100 = 2,
            Digit10 = 3,    // 10 ~ 90
            Digit1 = 4,     // 1, 2, 3, 4, 5, 8,
            Digit6_7 = 5,
            Digit7 = 6,
            Digit9 = 7,
            SingleQuote = 8,
            DoubleQuote = 9,
        };

        ////////////////////////////////////////////////////////////////////////////
        //
        // This class is used to map a token into its Hebrew digit value.
        //
        ////////////////////////////////////////////////////////////////////////////

        private struct HebrewValue
        {
            internal HebrewToken token;
            internal short value;
            internal HebrewValue(HebrewToken token, short value)
            {
                this.token = token;
                this.value = value;
            }
        }

        //
        // Map a Hebrew character from U+05D0 ~ U+05EA to its digit value.
        // The value is -1 if the Hebrew character does not have a associated value.
        //
        private static readonly HebrewValue[] s_hebrewValues = {
            new HebrewValue(HebrewToken.Digit1, 1) , // '\x05d0
            new HebrewValue(HebrewToken.Digit1, 2) , // '\x05d1
            new HebrewValue(HebrewToken.Digit1, 3) , // '\x05d2
            new HebrewValue(HebrewToken.Digit1, 4) , // '\x05d3
            new HebrewValue(HebrewToken.Digit1, 5) , // '\x05d4
            new HebrewValue(HebrewToken.Digit6_7,6) , // '\x05d5
            new HebrewValue(HebrewToken.Digit6_7,7) , // '\x05d6
            new HebrewValue(HebrewToken.Digit1, 8) , // '\x05d7
            new HebrewValue(HebrewToken.Digit9, 9) , // '\x05d8
            new HebrewValue(HebrewToken.Digit10, 10) , // '\x05d9;          // Hebrew Letter Yod
            new HebrewValue(HebrewToken.Invalid, -1) , // '\x05da;
            new HebrewValue(HebrewToken.Digit10, 20) , // '\x05db;          // Hebrew Letter Kaf
            new HebrewValue(HebrewToken.Digit10, 30) , // '\x05dc;          // Hebrew Letter Lamed
            new HebrewValue(HebrewToken.Invalid, -1) , // '\x05dd;
            new HebrewValue(HebrewToken.Digit10, 40) , // '\x05de;          // Hebrew Letter Mem
            new HebrewValue(HebrewToken.Invalid, -1) , // '\x05df;
            new HebrewValue(HebrewToken.Digit10, 50) , // '\x05e0;          // Hebrew Letter Nun
            new HebrewValue(HebrewToken.Digit10, 60) , // '\x05e1;          // Hebrew Letter Samekh
            new HebrewValue(HebrewToken.Digit10, 70) , // '\x05e2;          // Hebrew Letter Ayin
            new HebrewValue(HebrewToken.Invalid, -1) , // '\x05e3;
            new HebrewValue(HebrewToken.Digit10, 80) , // '\x05e4;          // Hebrew Letter Pe
            new HebrewValue(HebrewToken.Invalid, -1) , // '\x05e5;
            new HebrewValue(HebrewToken.Digit10, 90) , // '\x05e6;          // Hebrew Letter Tsadi
            new HebrewValue(HebrewToken.Digit100, 100) , // '\x05e7;
            new HebrewValue(HebrewToken.Digit200_300, 200) , // '\x05e8;
            new HebrewValue(HebrewToken.Digit200_300, 300) , // '\x05e9;
            new HebrewValue(HebrewToken.Digit400, 400) , // '\x05ea;
        };

        private const int minHebrewNumberCh = 0x05d0;
        private static char s_maxHebrewNumberCh = (char)(minHebrewNumberCh + s_hebrewValues.Length - 1);

        ////////////////////////////////////////////////////////////////////////////
        //
        // Hebrew number parsing State
        // The current state and the next token will lead to the next state in the state machine.
        // DQ = Double Quote
        //
        ////////////////////////////////////////////////////////////////////////////

        internal enum HS : sbyte
        {
            _err = -1,          // an error state
            Start = 0,
            S400 = 1,           // a Hebrew digit 400
            S400_400 = 2,       // Two Hebrew digit 400
            S400_X00 = 3,       // Two Hebrew digit 400 and followed by 100
            S400_X0 = 4,       // Hebrew digit 400 and followed by 10 ~ 90
            X00_DQ = 5,         // A hundred number and followed by a double quote.
            S400_X00_X0 = 6,
            X0_DQ = 7,          // A two-digit number and followed by a double quote.
            X = 8,              // A single digit Hebrew number.
            X0 = 9,            // A two-digit Hebrew number
            X00 = 10,           // A three-digit Hebrew number
            S400_DQ = 11,       // A Hebrew digit 400 and followed by a double quote.
            S400_400_DQ = 12,
            S400_400_100 = 13,
            S9 = 14,            // Hebrew digit 9
            X00_S9 = 15,        // A hundered number and followed by a digit 9
            S9_DQ = 16,         // Hebrew digit 9 and followed by a double quote
            END = 100,          // A terminial state is reached.
        }

        //
        // The state machine for Hebrew number pasing.
        //
        private static readonly HS[] s_numberPasingState =
        {
            // 400            300/200         100             90~10           8~1      6,       7,       9,          '           "
            /* 0 */
                             HS.S400,       HS.X00,         HS.X00,         HS.X0,          HS.X,    HS.X,    HS.X,    HS.S9,      HS._err,    HS._err,
            /* 1: S400 */
                             HS.S400_400,   HS.S400_X00,    HS.S400_X00,    HS.S400_X0,     HS._err, HS._err, HS._err, HS.X00_S9  ,HS.END,     HS.S400_DQ,
            /* 2: S400_400 */
                             HS._err,       HS._err,        HS.S400_400_100,HS.S400_X0,     HS._err, HS._err, HS._err, HS.X00_S9  ,HS._err,    HS.S400_400_DQ,
            /* 3: S400_X00 */
                             HS._err,       HS._err,        HS._err,        HS.S400_X00_X0, HS._err, HS._err, HS._err, HS.X00_S9  ,HS._err,    HS.X00_DQ,
            /* 4: S400_X0 */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS._err,    HS.X0_DQ,
            /* 5: X00_DQ */
                             HS._err,       HS._err,        HS._err,        HS.END,         HS.END,  HS.END,  HS.END,  HS.END,     HS._err,    HS._err,
            /* 6: S400_X00_X0 */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS._err,    HS.X0_DQ,
            /* 7: X0_DQ */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS.END,  HS.END,  HS.END,  HS.END,     HS._err,    HS._err,
            /* 8: X */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS.END,     HS._err,
            /* 9: X0 */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS.END,     HS.X0_DQ,
            /* 10: X00 */
                             HS._err,       HS._err,        HS._err,        HS.S400_X0,     HS._err, HS._err, HS._err, HS.X00_S9,  HS.END,     HS.X00_DQ,
            /* 11: S400_DQ */
                             HS.END,        HS.END,         HS.END,         HS.END,         HS.END,  HS.END,  HS.END,  HS.END,     HS._err,    HS._err,
            /* 12: S400_400_DQ*/
                             HS._err,       HS._err,        HS.END,         HS.END,         HS.END,  HS.END,  HS.END,  HS.END,     HS._err,    HS._err,
            /* 13: S400_400_100*/
                             HS._err,       HS._err,        HS._err,        HS.S400_X00_X0, HS._err, HS._err, HS._err, HS.X00_S9,  HS._err,    HS.X00_DQ,
            /* 14: S9 */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS.END,     HS.S9_DQ,
            /* 15: X00_S9 */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS._err, HS._err, HS._err,    HS._err,    HS.S9_DQ,
            /* 16: S9_DQ */
                             HS._err,       HS._err,        HS._err,        HS._err,        HS._err, HS.END,  HS.END,  HS._err,    HS._err,    HS._err
        };

        // Count of valid HebrewToken, column count in the NumberPasingState array
        private const int HebrewTokenCount = 10;

        ////////////////////////////////////////////////////////////////////////
        //
        //  Actions:
        //      Parse the Hebrew number by passing one character at a time.
        //      The state between characters are maintained at HebrewNumberPasingContext.
        //  Returns:
        //      Return a enum of HebrewNumberParsingState.
        //          NotHebrewDigit: The specified ch is not a valid Hebrew digit.
        //          InvalidHebrewNumber: After parsing the specified ch, it will lead into
        //              an invalid Hebrew number text.
        //          FoundEndOfHebrewNumber: A terminal state is reached.  This means that
        //              we find a valid Hebrew number text after the specified ch is parsed.
        //          ContinueParsing: The specified ch is a valid Hebrew digit, and
        //              it will lead into a valid state in the state machine, we should
        //              continue to parse incoming characters.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static HebrewNumberParsingState ParseByChar(char ch, ref HebrewNumberParsingContext context)
        {
            Debug.Assert(s_numberPasingState.Length == HebrewTokenCount * ((int)HS.S9_DQ + 1));

            HebrewToken token;
            if (ch == '\'')
            {
                token = HebrewToken.SingleQuote;
            }
            else if (ch == '\"')
            {
                token = HebrewToken.DoubleQuote;
            }
            else
            {
                int index = (int)ch - minHebrewNumberCh;
                if (index >= 0 && index < s_hebrewValues.Length)
                {
                    token = s_hebrewValues[index].token;
                    if (token == HebrewToken.Invalid)
                    {
                        return (HebrewNumberParsingState.NotHebrewDigit);
                    }
                    context.result += s_hebrewValues[index].value;
                }
                else
                {
                    // Not in valid Hebrew digit range.
                    return (HebrewNumberParsingState.NotHebrewDigit);
                }
            }
            context.state = s_numberPasingState[(int)context.state * (int)HebrewTokenCount + (int)token];
            if (context.state == HS._err)
            {
                // Invalid Hebrew state.  This indicates an incorrect Hebrew number.
                return (HebrewNumberParsingState.InvalidHebrewNumber);
            }
            if (context.state == HS.END)
            {
                // Reach a terminal state.
                return (HebrewNumberParsingState.FoundEndOfHebrewNumber);
            }
            // We should continue to parse.
            return (HebrewNumberParsingState.ContinueParsing);
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // Actions:
        //  Check if the ch is a valid Hebrew number digit.
        //  This function will return true if the specified char is a legal Hebrew
        //  digit character, single quote, or double quote.
        // Returns:
        //  true if the specified character is a valid Hebrew number character.
        //
        ////////////////////////////////////////////////////////////////////////

        internal static bool IsDigit(char ch)
        {
            if (ch >= minHebrewNumberCh && ch <= s_maxHebrewNumberCh)
            {
                return (s_hebrewValues[ch - minHebrewNumberCh].value >= 0);
            }
            return (ch == '\'' || ch == '\"');
        }
    }
}
