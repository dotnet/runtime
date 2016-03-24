// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Diagnostics;

public class GetExceptionForHRTest
{
    //HR:0x80020006
    void RunTests1()
    {
        
        int err = unchecked((int)0x80020006);
        Exception ex = Marshal.GetExceptionForHR(err);
        if(ex.HResult !=  err) throw new Exception();
    }

    //0x80020101
    void RunTest2()
    {     
        int err = unchecked((int)0x80020101);
        Exception ex = Marshal.GetExceptionForHR(err);             
        if(ex.HResult !=  err) throw new Exception();                
    }

    public bool RunTests()
    {
        RunTests1();
        RunTest2();
        return true;
    }

    public static int Main(String[] unusedArgs)
    {
        if (new GetExceptionForHRTest().RunTests())
            return 100;
        return 99;
    }

}
