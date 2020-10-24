// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//this is regression test for 307867 
//this failed due to inlining under gcstress
internal class TEST
{
    // prevent induction variable from being optimized away
    private volatile static int s_numLeft;

    public static unsafe int Main()
    {
        string value = "Hello, World!";
        char[] dest = new char[value.Length];
        s_numLeft = value.Length - 1;


        while (s_numLeft >= 0)
        {
            fixed (char* pChars = value)
            {
                dest[s_numLeft] = pChars[s_numLeft];
                s_numLeft -= 1;
            }
        }

        string s = new string(dest);
        System.Console.WriteLine(s);
        if (s != value)
        {
            System.Console.WriteLine("FAIL");
            return -1;
        }

        System.Console.WriteLine("pass");
        return 100;
    }
}

