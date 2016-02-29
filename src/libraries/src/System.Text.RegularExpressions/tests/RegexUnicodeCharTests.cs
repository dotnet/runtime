// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

    private const int MaxUnicodeRange = 2 << 15;//we are adding 0's here?

    [Fact]
    public static void RegexUnicodeChar()
    {        
        // Regex engine is Unicode aware now for the \w and \d character classes
        // \s is not - i.e. it still only recognizes the ASCII space separators, not Unicode ones
        // The new character classes for this:
        // [\p{L1}\p{Lu}\p{Lt}\p{Lo}\p{Nd}\p{Pc}]
        List<char> validChars = new List<char>();
        List<char> invalidChars = new List<char>();
        for (int i = 0; i < MaxUnicodeRange; i++)
        {
            char c = (char)i;
            switch (CharUnicodeInfo.GetUnicodeCategory(c))
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
                    validChars.Add(c);
                    break;
                default:
                    invalidChars.Add(c);
                    break;
            }
        }

        // \w - we will create strings from valid characters that form \w and make sure that the regex engine catches this.
        // Build a random string with valid characters followed by invalid characters
        Random random = new Random(-55);
        Regex regex = new Regex(@"\w*");
        
        int validCharLength = 10;
        int charCount = validChars.Count;
        int invalidCharCount = invalidChars.Count;
        int invalidCharLength = 15;

        for (int i = 0; i < 100; i++)
        {
            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            for (int j = 0; j < validCharLength; j++)
            {
                char c = validChars[random.Next(charCount)];
                builder1.Append(c);
                builder2.Append(c);
            }
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);

            string input = builder1.ToString();
            Match match = regex.Match(input);
            Assert.True(match.Success);

            Assert.Equal(builder2.ToString(), match.Value);
            Assert.Equal(0, match.Index);
            Assert.Equal(validCharLength, match.Length);

            match = match.NextMatch();
            do
            {
                // This is tedious. But we report empty Matches for each of the non-matching characters!!!
                // duh!!! because we say so on the pattern - remember what * stands for :-)
                Assert.Equal(string.Empty, match.Value);
                Assert.Equal(0, match.Length);
                match = match.NextMatch();
            } while (match.Success);
        }

        // Build a random string with invalid characters followed by valid characters and then again invalid
        random = new Random(-55);
        regex = new Regex(@"\w+");
        
        validCharLength = 10;
        charCount = validChars.Count;
        invalidCharCount = invalidChars.Count;
        invalidCharLength = 15;

        for (int i = 0; i < 500; i++)
        {
            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);
            for (int j = 0; j < validCharLength; j++)
            {
                char c = validChars[random.Next(charCount)];
                builder1.Append(c);
                builder2.Append(c);
            }
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);
            string input = builder1.ToString();

            Match match = regex.Match(input);
            Assert.True(match.Success);

            Assert.Equal(builder2.ToString(), match.Value);
            Assert.Equal(invalidCharLength, match.Index);
            Assert.Equal(validCharLength, match.Length);

            match = match.NextMatch();
            Assert.False(match.Success);
        }

        validChars = new List<char>();
        invalidChars = new List<char>();
        for (int i = 0; i < MaxUnicodeRange; i++)
        {
            char c = (char)i;
            switch (CharUnicodeInfo.GetUnicodeCategory(c))
            {
                case UnicodeCategory.DecimalDigitNumber:     // Nd
                    validChars.Add(c);
                    break;
                default:
                    invalidChars.Add(c);
                    break;
            }
        }

        // \d - we will create strings from valid characters that form \d and make sure that the regex engine catches this.
        // Build a random string with valid characters and then again invalid
        regex = new Regex(@"\d+");

        validCharLength = 10;
        invalidCharLength = 15;
        charCount = validChars.Count;
        invalidCharCount = invalidChars.Count;

        for (int i = 0; i < 100; i++)
        {
            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            for (int j = 0; j < validCharLength; j++)
            {
                char c = validChars[random.Next(charCount)];
                builder1.Append(c);
                builder2.Append(c);
            }
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);
            string input = builder1.ToString();
            Match match = regex.Match(input);


            Assert.Equal(builder2.ToString(), match.Value);
            Assert.Equal(0, match.Index);
            Assert.Equal(validCharLength, match.Length);

            match = match.NextMatch();
            Assert.False(match.Success);
        }

        // Build a random string with invalid characters, valid and then again invalid
        regex = new Regex(@"\d+");
        
        validCharLength = 10;
        invalidCharLength = 15;
        charCount = validChars.Count;
        invalidCharCount = invalidChars.Count;

        for (int i = 0; i < 100; i++)
        {
            StringBuilder builder1 = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);
            for (int j = 0; j < validCharLength; j++)
            {
                char c = validChars[random.Next(charCount)];
                builder1.Append(c);
                builder2.Append(c);
            }
            for (int j = 0; j < invalidCharLength; j++)
                builder1.Append(invalidChars[random.Next(invalidCharCount)]);
            string input = builder1.ToString();

            Match match = regex.Match(input);
            Assert.True(match.Success);

            Assert.Equal(builder2.ToString(), match.Value);
            Assert.Equal(invalidCharLength, match.Index);
            Assert.Equal(validCharLength, match.Length);

            match = match.NextMatch();
            Assert.False(match.Success);
        }
    }
}
