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

        CountResults(new BaseClassTest().Run(),                 ref nSuccesses, ref nFailures);
        
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

class BaseClassTest
{
  Trace _trace;
  
  void f2()
  {
    throw new FileNotFoundException("1");
  }

  void f1()
  {
    try
    {
      f2();
    }
    catch(FileNotFoundException e)
    {
      Console.WriteLine(e);
      _trace.Write("0" + e.Message);
      throw e;
    }
    catch(IOException e)
    {
      Console.WriteLine(e);
      _trace.Write("!" + e.Message);
      throw e;
    }
    catch(Exception e)
    {
      Console.WriteLine(e);
      _trace.Write("@" + e.Message);
      throw e;
    }
  }

  public int Run() 
  {
      _trace = new Trace("BaseClassTest", "0121");
      
      try
      {
        f1();
      }
      catch(Exception e)
      {
        Console.WriteLine(e);
        _trace.Write("2" + e.Message);
      }

      return _trace.Match();
  }
}



