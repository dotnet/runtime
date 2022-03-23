// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test<TException>() where TException : Exception
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (TException)
        {
            return;
        }
    }

    static int Main()
    {
        try
        {
            Test<InvalidOperationException>();
        }
        catch
        {
            return -1;
        }

        return 100;
    }
}
