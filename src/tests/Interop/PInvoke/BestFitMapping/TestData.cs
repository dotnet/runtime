// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

public static class TestData
{
    public static string GetInvalidString()
    {
        return GetInvalidStringBuilder().ToString();
    }

    public static string GetUnmappableString()
    {
        return GetUnmappableStringBuilder().ToString();
    }

    public static string GetValidString()
    {
        return GetValidStringBuilder().ToString();
    }

    public static StringBuilder GetInvalidStringBuilder()
    {
        StringBuilder sbl = new StringBuilder();
        sbl.Append((char)0x2216);
        sbl.Append((char)0x2044);
        sbl.Append((char)0x2215);
        sbl.Append((char)0x0589);
        sbl.Append((char)0x2236);
        return sbl;
    }

    public static StringBuilder GetUnmappableStringBuilder()
    {
        StringBuilder sbl = new StringBuilder();
        sbl.Append('乀');
        sbl.Append('Ω');
        sbl.Append('火');
        return sbl;
    }

    public static StringBuilder GetValidStringBuilder()
    {
        return new StringBuilder("This is the initial test string.");
    }

    public static char GetInvalidChar()
    {
        return (char)0x2216;
    }

    public static char GetValidChar()
    {
        return 'c';
    }

    public static char GetUnmappableChar()
    {
        return '火';
    }

    public static string[] GetInvalidStringArray()
    {
        string invalid = GetInvalidString();
        return new string[]
        {
            invalid,
            invalid,
            invalid
        };
    }

    public static string[] GetUnmappableStringArray()
    {
        string unmappable = GetUnmappableString();
        return new string[]
        {
            unmappable,
            unmappable,
            unmappable
        };
    }

    public static string[] GetValidStringArray()
    {
        string valid = GetValidString();
        return new string[]
        {
            valid,
            valid,
            valid
        };
    }
}
