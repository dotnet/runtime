// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Runtime.InteropServices.JavaScript;

public partial class MemoryTest
{
    [JSImport("countChars", "main.js")]
    internal static partial int CountChars(string testArray);

    [JSExport]
    internal static void Run()
    {
        // Allocate over 2GB space, 2 621 440 000 bytes
        const int arrayCnt = 25;
        int[][] arrayHolder = new int[arrayCnt][];
        string errors = "";
        TestOutput.WriteLine("Starting over 2GB array allocation");
        for (int i = 0; i < arrayCnt; i++)
        {
            try
            {
                arrayHolder[i] = new int[1024 * 1024 * 25];
            }
            catch (Exception ex)
            {
                errors += $"Exception {ex} was thrown on i={i}";
            }
        }
        TestOutput.WriteLine("Finished over 2GB array allocation");

        // call a method many times to trigger tier-up optimization
        string randomString = GenerateRandomString(1000);
        try
        {
            for (int i = 0; i < 1000; i++)
            {
                int count = CountChars(randomString);
                if (count != randomString.Length)
                    errors += $"CountChars returned {count} instead of {randomString.Length} for {i}-th string.";
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
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        var stringBuilder = new StringBuilder(stringLength);
        for (int i = 0; i < stringLength; i++)
        {
            stringBuilder.Append(chars[random.Next(chars.Length)]);
        }
        return stringBuilder.ToString();
    }
}
