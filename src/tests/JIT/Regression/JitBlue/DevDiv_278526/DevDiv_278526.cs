// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test was originally a repro for an assertion regarding incorrect value number of the tree in cse.
// The repro requires that the tree and its child are considered by cse and child is binary sub (a - b).
// Cse calls morph of the parent tree and morphs child to (a + (-b)) and sets the clean VN state to the child.
// It causes assert when cse processes the child with the clean vn state.


using System;
using Xunit;


public class Program
{
      public sealed class Variables
    {
        public static byte[] decryptedApplicationData
        {
            get;
            set;
        }
    }

    private static bool VerifyMacvalueSSlV2(string sourceIP)
    {
        if (sourceIP == "skip")
            return false;        

        byte[] array3 = new byte[0];

        // Assert happens on the next two statements.
        int l = Variables.decryptedApplicationData.Length - array3.Length - 16;
        byte[] array2 = new byte[l];       
        
        if (array3[0] != array2[0])
            return false;      
        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        string s = "skip"; // Test checks commpilation process.
        VerifyMacvalueSSlV2(s);
        return 100;
    }
}
