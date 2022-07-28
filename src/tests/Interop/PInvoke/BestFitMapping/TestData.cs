// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

public static class TestData
{
    public const char InvalidChar = (char)0x2216;
    public const char UnmappableChar = '火';
    public const char ValidChar = 'c';

    public static readonly string InvalidString = new string(new char[]
        {
            (char)0x2216,
            (char)0x2044,
            (char)0x2215,
            (char)0x0589,
            (char)0x2236
        });
    public static readonly string UnmappableString = new string(new char[] { '乀', 'Ω', '火' });
    public static readonly string ValidString = "This is the initial test string.";

    public static readonly StringBuilder InvalidStringBuilder = new StringBuilder(InvalidString);
    public static readonly StringBuilder UnmappableStringBuilder = new StringBuilder(UnmappableString);
    public static readonly StringBuilder ValidStringBuilder = new StringBuilder(ValidString);

    public static readonly string[] InvalidStringArray = new string[] { InvalidString, InvalidString, InvalidString };
    public static readonly string[] UnmappableStringArray = new string[] { UnmappableString, UnmappableString, UnmappableString };
    public static readonly string[] ValidStringArray = new string[] { ValidString, ValidString, ValidString };
}
