// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

public class RegexUnicodeCharTests
{
    // This test case checks the unicode-aware features in theregex engine.

    private const Int32 MaxUnicodeRange = 2 << 15;//we are adding 0's here?

    [Fact]
    public static void RegexUnicodeChar()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;

        Char ch1;
        List<Char> alstValidChars;
        List<Char> alstInvalidChars;
        Int32 iCharCount;
        Int32 iNonCharCount;

        String pattern;
        String input;
        Regex regex;
        Match match;

        Int32 iNumLoop;
        Int32 iWordCharLength;
        Int32 iNonWordCharLength;
        StringBuilder sbldr1;
        StringBuilder sbldr2;
        Random random;

        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////

            //[]Regex engine is Unicode aware now for the \w and \d character classes
            // \s is not - i.e. it still only recognizes the ASCII space separators, not Unicode ones
            // The new character classes for this:
            //[\p{L1}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}]
            iCountTestcases++;


            Console.WriteLine("MaxUnicodeRange, " + MaxUnicodeRange);

            alstValidChars = new List<Char>();
            alstInvalidChars = new List<Char>();
            for (int i = 0; i < MaxUnicodeRange; i++)
            {
                ch1 = (Char)i;
                switch (CharUnicodeInfo.GetUnicodeCategory(ch1))
                {
                    case UnicodeCategory.UppercaseLetter:        //Lu
                    case UnicodeCategory.LowercaseLetter:        //Li
                    case UnicodeCategory.TitlecaseLetter:        // Lt
                    case UnicodeCategory.ModifierLetter:         // Lm
                    case UnicodeCategory.OtherLetter:            // Lo
                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                    //                    case UnicodeCategory.LetterNumber:           // ??
                    //                    case UnicodeCategory.OtherNumber:            // ??
                    case UnicodeCategory.NonSpacingMark:
                    //                    case UnicodeCategory.SpacingCombiningMark:   // Mc
                    case UnicodeCategory.ConnectorPunctuation:   // Pc
                        alstValidChars.Add(ch1);
                        break;
                    default:
                        alstInvalidChars.Add(ch1);
                        break;
                }
            }



            //[]\w - we will create strings from valid characters that form \w and make sure that the regex engine catches this.
            //Build a random string with valid characters followed by invalid characters
            iCountTestcases++;

            random = new Random(-55);

            pattern = @"\w*";
            regex = new Regex(pattern);

            iNumLoop = 100;
            iWordCharLength = 10;
            iCharCount = alstValidChars.Count;
            iNonCharCount = alstInvalidChars.Count;
            iNonWordCharLength = 15;

            for (int i = 0; i < iNumLoop; i++)
            {
                sbldr1 = new StringBuilder();
                sbldr2 = new StringBuilder();
                for (int j = 0; j < iWordCharLength; j++)
                {
                    ch1 = alstValidChars[random.Next(iCharCount)];
                    sbldr1.Append(ch1);
                    sbldr2.Append(ch1);
                }
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                input = sbldr1.ToString();
                match = regex.Match(input);
                if (!match.Success
                    || (match.Index != 0)
                    || (match.Length != iWordCharLength)
                    || !(match.Value.Equals(sbldr2.ToString()))
                )
                {
                    iCountErrors++;
                    Console.WriteLine("Err_753wfgg_" + i + "! Error detected: input-{0}", input);
                    Console.WriteLine("Match index={0}, length={1}, value={2}, match.Value.Equals(expected)={3}\n",
                        match.Index, match.Length, match.Value, match.Value.Equals(sbldr2.ToString()));

                    if (match.Length > iWordCharLength)
                    {
                        Console.WriteLine("FAIL!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n{0}={1}",
                            (int)match.Value[iWordCharLength], CharUnicodeInfo.GetUnicodeCategory(match.Value[iWordCharLength]));
                    }
                }

                match = match.NextMatch();
                do
                {
                    //This is tedious. But we report empty Matches for each of the non-matching characters!!!
                    //duh!!! because we say so on the pattern - remember what * stands for :-)
                    if (!match.Value.Equals(String.Empty)
                        || (match.Length != 0)
                    )
                    {
                        iCountErrors++;
                        Console.WriteLine("Err_2975sg_" + i + "! Error detected: input-{0}", input);
                        Console.WriteLine("Match index={0}, length={1}, value={2}\n",
                            match.Index, match.Length, match.Value);
                    }
                    match = match.NextMatch();
                } while (match.Success);
            }

            //[]Build a random string with invalid characters followed by valid characters and then again invalid
            iCountTestcases++;

            random = new Random(-55);

            pattern = @"\w+";
            regex = new Regex(pattern);

            iNumLoop = 500;
            iWordCharLength = 10;
            iCharCount = alstValidChars.Count;
            iNonCharCount = alstInvalidChars.Count;
            iNonWordCharLength = 15;

            for (int i = 0; i < iNumLoop; i++)
            {
                sbldr1 = new StringBuilder();
                sbldr2 = new StringBuilder();
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                for (int j = 0; j < iWordCharLength; j++)
                {
                    ch1 = alstValidChars[random.Next(iCharCount)];
                    sbldr1.Append(ch1);
                    sbldr2.Append(ch1);
                }
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                input = sbldr1.ToString();
                match = regex.Match(input);

                if (!match.Success
                    || (match.Index != iNonWordCharLength)
                    || (match.Length != iWordCharLength)
                    || !(match.Value.Equals(sbldr2.ToString()))
                )
                {
                    iCountErrors++;
                    Console.WriteLine("Err_2975sgf_" + i + "! Error detected: input-{0}", input);
                }

                match = match.NextMatch();
                if (match.Success)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_246sdg_" + i + "! Error detected: input-{0}", input);
                }
            }

            alstValidChars = new List<Char>();
            alstInvalidChars = new List<Char>();
            for (int i = 0; i < MaxUnicodeRange; i++)
            {
                ch1 = (Char)i;
                switch (CharUnicodeInfo.GetUnicodeCategory(ch1))
                {
                    case UnicodeCategory.DecimalDigitNumber:     // Nd
                        alstValidChars.Add(ch1);
                        break;
                    default:
                        alstInvalidChars.Add(ch1);
                        break;
                }
            }

            //[]\d - we will create strings from valid characters that form \d and make sure that the regex engine catches this.            
            //[]Build a random string with valid characters and then again invalid
            iCountTestcases++;

            pattern = @"\d+";
            regex = new Regex(pattern);

            iNumLoop = 100;
            iWordCharLength = 10;
            iNonWordCharLength = 15;
            iCharCount = alstValidChars.Count;
            iNonCharCount = alstInvalidChars.Count;

            for (int i = 0; i < iNumLoop; i++)
            {
                sbldr1 = new StringBuilder();
                sbldr2 = new StringBuilder();
                for (int j = 0; j < iWordCharLength; j++)
                {
                    ch1 = alstValidChars[random.Next(iCharCount)];
                    sbldr1.Append(ch1);
                    sbldr2.Append(ch1);
                }
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                input = sbldr1.ToString();
                match = regex.Match(input);
                if (!match.Success
                    || (match.Index != 0)
                    || (match.Length != iWordCharLength)
                    || !(match.Value.Equals(sbldr2.ToString()))
                )
                {
                    iCountErrors++;
                    Console.WriteLine("Err_245sfg_" + i + "! Error detected: input-{0}", input);
                }

                match = match.NextMatch();
                if (match.Success)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_29765sg_" + i + "! Error detected: input-{0}", input);
                }
            }

            //[]Build a random string with invalid characters, valid and then again invalid
            iCountTestcases++;

            pattern = @"\d+";
            regex = new Regex(pattern);

            iNumLoop = 100;
            iWordCharLength = 10;
            iNonWordCharLength = 15;
            iCharCount = alstValidChars.Count;
            iNonCharCount = alstInvalidChars.Count;

            for (int i = 0; i < iNumLoop; i++)
            {
                sbldr1 = new StringBuilder();
                sbldr2 = new StringBuilder();
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                for (int j = 0; j < iWordCharLength; j++)
                {
                    ch1 = alstValidChars[random.Next(iCharCount)];
                    sbldr1.Append(ch1);
                    sbldr2.Append(ch1);
                }
                for (int j = 0; j < iNonWordCharLength; j++)
                    sbldr1.Append(alstInvalidChars[random.Next(iNonCharCount)]);
                input = sbldr1.ToString();
                match = regex.Match(input);
                if (!match.Success
                    || (match.Index != iNonWordCharLength)
                    || (match.Length != iWordCharLength)
                    || !(match.Value.Equals(sbldr2.ToString()))
                )
                {
                    iCountErrors++;
                    Console.WriteLine("Err_29756tsg_" + i + "! Error detected: input-{0}", input);
                }

                match = match.NextMatch();
                if (match.Success)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_27sjhr_" + i + "! Error detected: input-{0}", input);
                }
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }
        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }
}


