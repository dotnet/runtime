// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class Program
{
    public static int Main()
    {
        int retCode = 100;
        for (int i = 0; i < 1000; i++)
        {
            string f1 = i.ToString();
            string f2 = string.Intern(f1);
            if (!ReferenceEquals(f1, f2))
                retCode++;
            if (!ReferenceEquals(f1, string.IsInterned(f2)))
                retCode++;
        }
        return retCode;
    }
}
