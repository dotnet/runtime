// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;

public partial class MemoryTest
{
    [JSImport("joinStringArray", "main.js")]
    internal static partial string JoinStringArray(string[] testArray);

    [JSExport]
    internal static string Run()
    {
        // Allocate a 2GB space (20 int arrays of 100MB, 100MB = 4 * 1024 * 1024 * 25)
        const int arrayCnt = 20;
        int[][] arrayHolder = new int[arrayCnt][];
        string errors = "";
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

        // marshall string array to JS
        string [] testArray = new [] { "M", "e", "m", "o", "r", "y", "T", "e", "s", "t" };
        string response = JoinStringArray(testArray);

        bool correct = AssertJoinCorrect(testArray, response);
        if (!correct)
            errors += $"expected response: {response}, testArray: {string.Join("", testArray)}";

        // call a method many times to trigger tier-up optimization
        for (int i = 0; i < 10000; i++)
        {
            AssertJoinCorrect(testArray, response);
        }
        return errors;
    }

    private static bool AssertJoinCorrect(string[] testArray, string expected)
    {
        string joinedArray = string.Join("", testArray);
        return joinedArray.Equals(expected);
    }
}
