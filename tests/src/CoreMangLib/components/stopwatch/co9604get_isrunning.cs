// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;

public class Co9604get_IsRunning
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer       = "2003/02/12 08:43 LakshanF";
    public static String s_strClassMethod   = "StopWatch.IsRunning";
    public static String s_strTFName        = "Co9604get_IsRunning.cs";
    public static String s_strTFAbbrev      = s_strTFName.Substring(0, 6);

    public Boolean runTest()
    {
		int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

		Stopwatch watch;

        try
        {				

			//Scenario 1: Createa a new Stopwatch instance and ensure that IsRunning is false
			//Scenario 2: Start and ensure that the property is true
			//Sceanrio 3: Start, stop and check that property is false

			strLoc = "Loc_001oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();

				if(watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_367sfg! Unexpected value returned: {0}", watch.IsRunning);
				}

				watch.Start();

				if(!watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_3tw7sdg! Unexpected value returned: {0}", watch.IsRunning);
				}

				watch.Stop();

				if(watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_24t7dg! Unexpected value returned: {0}", watch.IsRunning);
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_346gr! Unexpected exception thrown! {0}", ex);
			}

			
			//Scenario 4: "	Start multiple times and check that property is true
			//Scenario 5: "	Stop multiple times and check property is false

			strLoc = "Loc_002oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();

				for(int i=0; i<10; i++)
				{
					watch.Start();

					if(!watch.IsRunning)
					{
						iCountErrors++;
						Console.WriteLine("Err_23readg_{0}! Unexpected value returned: {1}", i, watch.IsRunning);
					}
				}

				for(int i=0; i<10; i++)
				{
					watch.Stop();

					if(watch.IsRunning)
					{
						iCountErrors++;
						Console.WriteLine("Err_234457gd_{0}! Unexpected value returned: {1}", i, watch.IsRunning);
					}
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_346gr! Unexpected exception thrown! {0}", ex);
			}


			//Scenario 6: "	Start, Reset and then check that property is false

			strLoc = "Loc_003oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();

				watch.Start();

				watch.Reset();

				if(watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_24tsdg! Unexpected value returned: {0}", watch.IsRunning);
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_346gr! Unexpected exception thrown! {0}", ex);
			}

			//Scenario 7: "	Reset (once and then  multiple times) and check that property is false

			strLoc = "Loc_005oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();

				for(int i=0; i<10; i++)
				{
					watch.Reset();

					if(watch.IsRunning)
					{
						iCountErrors++;
						Console.WriteLine("Err_234t7g_{0}! Unexpected value returned: {1}", i, watch.IsRunning);
					}
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_346gr! Unexpected exception thrown! {0}", ex);
			}

        }
        catch(Exception globalE)
        {
            iCountErrors++;
            Console.WriteLine("Err_9374sfg! Unexpected exception thrown: location: {0}\r\n{1}", strLoc, globalE);
        }

        ////  Finish Diagnostics

        if ( iCountErrors == 0 )
        {
            return true;
        }
        else
        {
            return false;
        }	
    }
	
	
    static int Main()
    {
      
        Boolean bResult = false;
        Co9604get_IsRunning cbA = new Co9604get_IsRunning();

        try 
        {
            bResult = cbA.runTest();
        } 
        catch (Exception exc_main)
        {
            bResult = false;
            Console.WriteLine(s_strTFAbbrev + " : FAiL! Error Err_9999zzz! Uncaught Exception in main(), exc_main=="+exc_main);
        }

        if (bResult)
        {
            Console.WriteLine("Pass");
            return 100;
        }
        else
        {
            Console.WriteLine("FAiL!  " + s_strTFAbbrev);
            Console.WriteLine(" ");
            return 1;
        }
    }


}
