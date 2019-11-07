// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;


public class Class1
{
    [DllImport("fpcw.dll")]
    private static extern int RaiseFPException();

    public static int Main(string[] args)
    {
        int retVal = RaiseFPException();

        return ( retVal==100 ) ? 100 : 101;
    }
}
