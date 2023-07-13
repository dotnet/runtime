// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
using Xunit;

public class rep
{
    [Fact]
    public static int TestEntryPoint()
    {
        char[] chars = new char[] { (char)0x800 };
        byte[] bytes = new byte[20];
        int numBytes = Encoding.UTF8.GetBytes(chars, 0, 1, bytes, 0);
        Console.WriteLine("Converted to bytes - got " + numBytes + " bytes!");
        char[] chars2 = Encoding.UTF8.GetChars(bytes, 0, numBytes);
        Console.WriteLine("chars2.Length: " + chars2.Length);
        if (chars2.Length != 1)
            throw new Exception("Expected length to be 1!");
        if (chars2[0] != chars[0])
            throw new Exception("Char differed after being roundtripped!  got: U+" + ((short)chars2[0]).ToString("x"));
        Console.WriteLine("looks good.");
        return 100;
    }
}

