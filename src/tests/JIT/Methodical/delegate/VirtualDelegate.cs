// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using System.Runtime.InteropServices;

public class VirtualDelegate
{
    public static int Main () {
        int retVal = 100;
        try {
            var del = (Func<string, string>)Delegate.CreateDelegate (typeof (Func<string, string>), null, typeof (object).GetMethod ("ToString"));
            if (del ("FOO") != "FOO")
                retVal = 1;
        } catch(Exception e) {
            Console.WriteLine(e);
            retVal = 1;
        }
        
        return retVal;
        
    }
}
