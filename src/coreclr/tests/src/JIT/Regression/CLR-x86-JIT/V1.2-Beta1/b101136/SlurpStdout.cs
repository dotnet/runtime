// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

public class SlurpStdout
{
    public static string expSTDOUT = "test = hello world!";

    public static int Main()
    {
        Process p;
        string strSTDOUT;

        p = new Process();
        p.StartInfo.FileName = "StringBugNewSyntax.exe";
        p.StartInfo.UseShellExecute = false;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.Arguments = null;
        p.Start();
        p.WaitForExit();

        strSTDOUT = p.StandardOutput.ReadToEnd();

        strSTDOUT = strSTDOUT.Trim();

        if (strSTDOUT.StartsWith(expSTDOUT) && strSTDOUT.EndsWith(expSTDOUT))
        {
            Console.WriteLine("Pass");
            return 100;
        }
        else
        {
            Console.WriteLine("Received         : [{0}]", strSTDOUT);
            Console.WriteLine("Expected Begining: [{0}]", expSTDOUT);
            Console.WriteLine("Expected End     : [{0}]", expSTDOUT);
            Console.WriteLine("FAIL");
            return 0;
        }
    }
}


