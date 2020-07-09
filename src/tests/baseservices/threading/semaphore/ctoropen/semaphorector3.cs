// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

class CtorTest
{
    public static int Main(string[] args)
    {
        // Check args
        if (args.Length < 2)
        {
            Console.WriteLine("USAGE: SemaphoreCtor3 /iCount:<int> /mCount:<int> " + 
                "[/semName:<string>] [/iRandom:<int>]");
            return -1;
        }

        // Get the args
        int iCount = -1, mCount = -1, iRandom = -1;
        string semName = "DefaultString";

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].ToLower().StartsWith("/icount:"))
            {
                iCount = Convert.ToInt32(args[i].Substring(8));
                continue;
            }

            if (args[i].ToLower().StartsWith("/mcount:"))
            {
                mCount = Convert.ToInt32(args[i].Substring(8));
                continue;
            }

            if (args[i].ToLower().StartsWith("/irandom:"))
            {
                iRandom = Convert.ToInt32(args[i].Substring(9));
                continue;
            }

            if (args[i].ToLower().StartsWith("/semname:"))
            {
                semName = args[i].Substring(9);
                if (semName.ToLower() == "null")
                    semName = null;
                continue;
            }

            if (args[i].ToLower().StartsWith("/unisemname:"))
            {
                semName = string.Empty;
                //    Convert to unicode
                string[] s = args[i].Substring(12).Split(';');
                foreach (string str in s)
                    semName += Convert.ToChar(Convert.ToInt32(str));
                continue;
            }
        }
        CtorTest ct = new CtorTest();
        return ct.Run(iCount, mCount, semName, iRandom);
    }
           
    private int Run(int iCount, int mCount, string semName, int iRandom)
    {
        // Testing overlap of long strings
        int iRet = -1;
        Semaphore sem1 = null, sem2 = null, sem3 = null, 
            sem4 = null, sem6 = null;
        if (iRandom > 0)
	{
	    //TestFramework.GlobalData intl = new TestFramework.GlobalData();
            //semName = intl.GetString(iRandom, iRandom).Replace(@"\", "");
            Console.WriteLine("WARNING: No random name generation is ocurring");
	}
        string semNewName = semName.Remove(semName.Length - 2, 1);
        try
        {
            //  Create slightly different names
            using (sem1 = new Semaphore(iCount, mCount, semName))
            {
                sem2 = new Semaphore(iCount, mCount, semNewName);
                //  Make sure we can open it
                using(sem3 = Semaphore.OpenExisting(semName))
                {
                    sem4 = Semaphore.OpenExisting(semNewName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Unexpected exception thrown:  " + ex.ToString());
            Console.WriteLine("Test Failed");
            return iRet;
        }

        //  Make sure you can't open it
        try
        {
            Semaphore sem5 = Semaphore.OpenExisting(semName);
        }
        catch (WaitHandleCannotBeOpenedException)
        {
            //  This is expected
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception thrown:  " + e.ToString());
            Console.WriteLine("Test Failed");
            return iRet;
        }

        //  Make sure you can still open the other one
        sem6 = Semaphore.OpenExisting(semNewName);
        //  Do a wait and release
        sem6.WaitOne();
        sem6.Release();
        iRet = 100;

        //Add a subsequent use of sem2 to avoid system event be GCed.
        GC.KeepAlive(sem2);

        Console.WriteLine(100 == iRet ? "Test Passed" : "Test Failed");
        return iRet;
    }
}
