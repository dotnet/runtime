// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

public class Test
{
  public bool FinallyCalled;
  public int RunLength;
  public int SpecificWait;

  static public int Main(string[] args)
  {
    if(args.Length < 1)
    {
       Console.WriteLine("Must supply run length");
       return 10;
    }

    Test   T          = new Test();
    T.SpecificWait = 0;
    T.RunLength = Int32.Parse(args[0]);
    if(args.Length == 2)
    {
       T.SpecificWait = Int32.Parse(args[1]);
    }
    int retVal = T.Run();
    Console.WriteLine(100 == retVal ? "Test Passed":"Test Failed");
    return retVal;
  }
    
  public int Run()
  {
    Thread NewThread  = null;
    Random RandomWait = new Random();
    int Runs = 0;

    FinallyCalled = true;

    for (int i = 0; i< RunLength; i++)
    {      
      // Create new Thread for test and start it
      NewThread = new Thread( new ThreadStart(TestFinally ) );
      NewThread.Start();
      // Wait random period of time for thread
      int wait;
      if(SpecificWait == 0)
      {
          wait = RandomWait.Next();
          wait = wait % 2000;
      }
      else
      {
          wait = SpecificWait;
      }
      
      Console.WriteLine("Testing with {0}", wait);
      Thread.Sleep( wait );

      // Abort thread, and wait to finish
      ThreadEx.Abort(NewThread);
      NewThread.Join();

      Console.WriteLine("  Finshed run #{0}", Runs++ );

      // Test if finally was called
      if ( FinallyCalled != true )
      {
        Console.WriteLine("FAILURE!!! Finally was not hit, or this would be true");
        return 5;
      }
    }
    return 100;
  }

  public void TestFinally()
  {
    try
    {
      // We have set FinallyCalled to false, so if we are aborted this should
      // be set to true no matter what!! (this is the bug)
      FinallyCalled = false;

      // Loop until this thread is aborted
      while ( true )
      {
        CallTryTest();
      }
    }
    finally
    {
      // This should be called no matter what
      FinallyCalled = true;
    }
  }

  private void CallTryTest()
  {
    try
    {
      // Do Something/Work (this could be anything)
      FinallyCalled = false;
    }
    catch (Exception e)
    {
      Console.WriteLine("Caught exception '{0}'", e.Message );
    }
  }
}