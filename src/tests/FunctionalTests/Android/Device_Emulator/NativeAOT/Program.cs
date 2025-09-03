// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

public class Program
{
    public static int Main()
    {
        string message = "Hello, Android!";
        Console.WriteLine(message); // logcat
        // Test the linux-bionic cryptography library
        Console.WriteLine(System.Security.Cryptography.SHA256.HashData(new byte[] {0x1, 0x2, 0x3}));
        return 42;
    }
}
