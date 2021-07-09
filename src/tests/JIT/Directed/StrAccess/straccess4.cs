// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

//random length and random content string
//IndexOutOfRangeException

using System;

internal struct VT
{
    public String str;
}

internal class CL
{
    public String str;
}

internal class StrAccess1
{
    public static String str1;
    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    public static Random rand = new Random(Seed);

    private static int randomUnicodeLetterOrDigit()
    {
        int c = (char)rand.Next((int)Char.MinValue, (int)Char.MaxValue);
        while (!Char.IsLetterOrDigit((char)c))
            c = rand.Next((int)Char.MinValue, (int)Char.MaxValue);
        return c;
    }

    private static string randomUnicodeString(int len)
    {
        string str = "";
        while (len-- >= 0)
            str += "\\u" + randomUnicodeLetterOrDigit().ToString("X4");
        return str;
    }

    public static int Main(string[] args)
    {
        bool passed;

        string teststr = "";
        int len = 0;

        if (args.Length != 0)
        {
            teststr = args[0];
            len = teststr.Length;
        }
        else
        {
            //construct random string with random length
            len = rand.Next(50);
            teststr = randomUnicodeString(len);
        }

        Console.WriteLine("Test string is {0}", teststr);

        String str2 = "";
        CL cl1 = new CL();
        VT vt1;

        str1 = str2 = cl1.str = vt1.str = teststr;

        String[] str1darr = new String[len];
        for (int j = 0; j < len; j++)
            str1darr[j] = Convert.ToString(teststr[j]);

        char b0, b1, b2, b3, b4;
        try
        {
            passed = false;
            b0 = cl1.str[len];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b1 = str1[len];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b2 = str2[len];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b3 = vt1.str[len];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b4 = Convert.ToChar(str1darr[len]);
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }

        try
        {
            passed = false;
            b0 = cl1.str[-1];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b1 = str1[-1];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b2 = str2[-1];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b3 = vt1.str[-1];
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }
        try
        {
            passed = false;
            b4 = Convert.ToChar(str1darr[-1]);
        }
        catch (IndexOutOfRangeException)
        {
            passed = true;
        }

        int i;
        while (len != 0)
        {
            i = rand.Next(0, len);
            b0 = cl1.str[i];
            b1 = str1[i];
            b2 = str2[i];
            b3 = vt1.str[i];
            b4 = Convert.ToChar(str1darr[i]);
            if ((b0 != b1) || (b1 != b2) || (b2 != b3) || (b3 != b4))
                passed = false;
            len /= 2;
        }

        Console.WriteLine();
        if (!passed)
        {
            Console.WriteLine("FAILED");
            Console.WriteLine("Use the following command to repro:");
            Console.WriteLine("straccess3.exe {0}", teststr);
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}




