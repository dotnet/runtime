// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Test22348
{
    public static int Main()
    {
        try
        {
            byte[,] tooBig = new byte[267784, 15351];
            tooBig[139893, 12] = 100;
            return (byte)tooBig.GetValue(139893, 12);
        }
        catch (OutOfMemoryException e)
        {
            Console.WriteLine(e);
        }
        
        return 100;
    }
}
