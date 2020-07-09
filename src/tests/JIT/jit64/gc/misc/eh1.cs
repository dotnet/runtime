// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

class T
{
    public static int Main()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception E)
        {
            Console.WriteLine("Caught expected exception " + E.GetType());
            return 100;
        }
#pragma warning disable 0162
        return -1;
#pragma warning restore 0252
    }
}
