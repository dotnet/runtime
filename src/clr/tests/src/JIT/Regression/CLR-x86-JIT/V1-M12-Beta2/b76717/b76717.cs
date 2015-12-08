// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

public class rep
{
    public static int Main()
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

