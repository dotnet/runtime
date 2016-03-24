// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

//
// main
//

public class TestSet
{
    static void CountResults(int testReturnValue, ref int nSuccesses, ref int nFailures)
    {
        if (100 == testReturnValue)
        {
            nSuccesses++;
        }
        else
        {
            nFailures++;
        }
    }

    public static int Main()
    {
        int nSuccesses = 0;
        int nFailures = 0;

        CountResults(new BaadbaadTest().Run(),                  ref nSuccesses, ref nFailures);
        
        if (0 == nFailures)
        {
            Console.WriteLine("OVERALL PASS: " + nSuccesses + " tests");
            return 100;
        }
        else
        {
            Console.WriteLine("OVERALL FAIL: " + nFailures + " tests failed");
            return 999;
        }
    }
}

public class BaadbaadTest
{
	Trace _trace;
	public int Run()
	{
		_trace = new Trace("BaadbaadTest", "1234");
		try
		{
			DoStuff();
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			_trace.Write("4");
		}
		return _trace.Match();
	}
	void DoStuff()
	{
		try
		{
			try
			{
				try
				{
					throw new Exception();
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					_trace.Write("1");
					throw;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				_trace.Write("2");
				throw;
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e);
			_trace.Write("3");
			throw;
		}
	}
}

