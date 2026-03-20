// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

unsafe class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int ReadPointer(int* pVal)
    {
        return *pVal;
    }

    static int Main()
    {
        try
        {
            ReadPointer(null);
        }
        catch (Exception)
        {
            return 100;
        }

        return 1;
    }
}
