// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Diagnostics;

public class Co9600Ctor
{
    public static String s_strActiveBugNums = "";
    public static String s_strDtTmVer       = "2003/02/03 09:49 LakshanF";
    public static String s_strClassMethod   = "StopWatch.Ctor()";
    public static String s_strTFName        = "Co9600Ctor.cs";
    public static String s_strTFAbbrev      = s_strTFName.Substring(0, 6);


    public Boolean runTest()
    {	
		int iCountErrors = 0;
        int iCountTestcases = 0;
        String strLoc = "Loc_000oo";

		Stopwatch watch;
		

        try
        {				

			//Scenario 1: Vanilla - ensure that a valid componenet is returned

			strLoc = "Loc_001oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();
				
				if(watch==null)
				{
					iCountErrors++;
					Console.WriteLine("Err_974tsg! null returned");
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_346gr! Unexpected exception thrown! {0}", ex);
			}

			//Scenario 2: Check that ElapsedXXX values are zero. Check this after resetting the watch

			strLoc = "Loc_002oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();
				
				if(watch.Elapsed!=TimeSpan.Zero)
				{
					iCountErrors++;
					Console.WriteLine("Err_342rsdg! Unexpected value returned: {0}", watch.Elapsed);
				}

				if(watch.ElapsedTicks!=0)
				{
					iCountErrors++;
					Console.WriteLine("Err_34rsg! Unexpected value returned: {0}", watch.ElapsedTicks);
				}

				if(watch.ElapsedMilliseconds!=0)
				{
					iCountErrors++;
					Console.WriteLine("Err_346sg! Unexpected value returned: {0}", watch.ElapsedMilliseconds);
				}

				watch.Reset();

				if(watch.Elapsed!=TimeSpan.Zero)
				{
					iCountErrors++;
					Console.WriteLine("Err_342rsdg! Unexpected value returned: {0}", watch.Elapsed);
				}

				if(watch.ElapsedTicks!=0)
				{
					iCountErrors++;
					Console.WriteLine("Err_34rsg! Unexpected value returned: {0}", watch.ElapsedTicks);
				}

				if(watch.ElapsedMilliseconds!=0)
				{
					iCountErrors++;
					Console.WriteLine("Err_346sg! Unexpected value returned: {0}", watch.ElapsedMilliseconds);
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_234s7g! Unexpected exception thrown! {0}", ex);
			}

			//Scenario 3: "	Check that IsRunning returns false

			strLoc = "Loc_003oo";

			iCountTestcases++;

			try
			{
				watch = new Stopwatch();
				
				if(watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_432s7g! Unexpected value returned: {0}", watch.IsRunning);
				}

				watch.Reset();

				if(watch.IsRunning)
				{
					iCountErrors++;
					Console.WriteLine("Err_467sfg! Unexpected value returned: {0}", watch.IsRunning);
				}

			}catch(Exception ex){
				iCountErrors++;
				Console.WriteLine("Err_3407sfg! Unexpected exception thrown! {0}", ex);
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
        Co9600Ctor cbA = new Co9600Ctor();

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
			Console.WriteLine( "FAiL!  "+ s_strTFAbbrev);
            Console.WriteLine( " " );
			return 1;
		}
    }


}
