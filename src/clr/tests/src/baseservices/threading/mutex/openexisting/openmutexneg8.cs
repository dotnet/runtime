// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

class OpenMutexNeg
{
    public Mutex mut;

    public static int Main()
    {
        OpenMutexNeg omn = new OpenMutexNeg();
        return omn.Run();
    }

    private int Run()
    {
        int iRet = -1;
        //  open a Mutex, not case specific
        mut = new Mutex(false, "This is a Mutex that is Case Specific");
        try
        {
            Mutex mut1 = Mutex.OpenExisting("This is a Mutex that is Case specific");
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            //Expected
            iRet = 100;
        }
        catch (Exception e)
        {
            Console.WriteLine("Caught unexpected exception: " + 
                e.ToString());
        }

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}