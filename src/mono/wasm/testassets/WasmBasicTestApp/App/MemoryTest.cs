// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class MemoryTest
{
    [JSImport("countChars", "main.js")]
    internal static partial int CountChars(string[] testArray);

    [JSExport]
    internal static void Run()
    {
        // Allocate a 2GB space (1024 * 1024 * 25 string arrays of 80 bytes (1 char = 2bytes))
        const int arrayCnt = 1024 * 1024 * 25;
        const int stringLength = 40;
        string[] testArray = new string[arrayCnt];
        string errors = "";
        Random random = new();
        for (int i = 0; i < arrayCnt; i++)
        {
            try
            {
                testArray[i] = GenerateRandomString(stringLength);
            }
            catch (Exception ex)
            {
                errors += $"Exception {ex} was thrown on i={i}";
            }
        }
        TestOutput.WriteLine("Finished 2GB array allocation");

        // call a method many times to trigger tier-up optimization
        try
        {
            for (int i = 0; i < 10000; i++)
            {
                int count = CountChars(testArray);
                if (count != stringLength)
                    errors += $"CountChars returned {count} instead of {stringLength} for {i}-th string.";
            }
        }
        catch (Exception ex)
        {
            errors += $"Exception {ex} was thrown when CountChars was called in a loop";
        }
        if (!string.IsNullOrEmpty(errors))
        {
            TestOutput.WriteLine(errors);
            throw new Exception(errors);
        }
        else
        {
            TestOutput.WriteLine("Great success, MemoryTest finished without errors.");
        }
    }

    private static Random random = new Random();

    private static string GenerateRandomString(int stringLength)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        char[] stringChars = new char[stringLength];
        for (int i = 0; i < stringLength; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }
        return new string(stringChars);
    }
}
