// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexValidations
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsIcuGlobalization))]
        public void ValidateLowercaseMapTableInRegexCharClass()
        {
            CultureInfo culture = CultureInfo.InvariantCulture;
            for (int k = 0; k < RegexCharClass.s_lcTable.Length; k++)
            {
                RegexCharClass.LowerCaseMapping loc = RegexCharClass.s_lcTable[k];
                if (loc.LcOp == RegexCharClass.LowercaseAdd)
                {
                    int offset = loc.Data;
                    for (char l = loc.ChMin; l <= loc.ChMax; l++)
                    {
                        Assert.True(culture.TextInfo.ToLower((char)l) == (char)(l + offset), $"The Unicode character range at index {k} in s_lcTable contains the character {(char)l} (decimal value: {l}). Its lowercase value cannot be obtained by using the specified offset.");
                    }
                }
                else if (loc.LcOp == RegexCharClass.LowercaseSet)
                {
                    char lowercase = (char)loc.Data;
                    for (char l = loc.ChMin; l <= loc.ChMax; l++)
                    {
                        char uppercase = l;
                        Assert.True(culture.TextInfo.ToLower(uppercase) == lowercase, $"The Unicode character range at index {k} in s_lcTable contains the character {uppercase} (decimal value: {(int)uppercase}, hex: {(int)uppercase:X}). Its lowercase value {culture.TextInfo.ToLower(uppercase).ToString()} (decimal value: {(int)culture.TextInfo.ToLower(uppercase)}, hex: {(int)culture.TextInfo.ToLower(uppercase):X}) is not the stored value {lowercase} (decimal value: {(int)lowercase}, hex: {(int)lowercase:X}).");
                    }
                }
                else if (loc.LcOp == RegexCharClass.LowercaseBor)
                {
                    for (char l = loc.ChMin; l <= loc.ChMax; l++)
                    {
                        Assert.True(culture.TextInfo.ToLower((char)l) == (char)(l | (char)1), $"The Unicode character range at index {k} in s_lcTable contains the character {(char)l} (decimal value: {l}). Its lowercase value {culture.TextInfo.ToLower(l)} cannot be obtained by OR-ing with 1: {(char)(l | (char)1)}");
                    }
                }
                else if (loc.LcOp == RegexCharClass.LowercaseBad)
                {
                    for (char l = loc.ChMin; l <= loc.ChMax; l++)
                    {
                        Assert.True(culture.TextInfo.ToLower((char)l) == (char)(l + (l & 1)), $"The Unicode character range at index {k} in s_lcTable contains the character {(char)l} (decimal value: {l}). Its lowercase value cannot be obtained by AND-ing with 1.");
                    }
                }
            }
        }
    }
}
