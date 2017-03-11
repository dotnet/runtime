// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a regression test for a crashing bug in Serial Port.  
// The original bug is DevDiv Bugs #142639, fixed with a QFE in WhidbeyQFE.  It only repros in 64-bit.
// BWadswor later reported the bug again using essentially the following code.  See Dev10 #619306.

using System;
using System.Threading;
using System.IO.Ports;
using System.Diagnostics;
using Legacy.Support;

internal class Test_CLRS
{
    private static bool s_done;

    public static void gc_food()
    {
        object[] bah = new object[100];
        Random r = new Random(-55);
        while (!s_done)
        {
            int i = r.Next(100);
            bah[i] = new object[r.Next(1000)];
            GC.Collect(2, GCCollectionMode.Forced);
        }
    }

    private static int Main()
    {
        try
        {
            String[] portNames = PortHelper.GetPorts();
            if (portNames.Length == 0)
            {
                Debug.WriteLine("No serial ports available.  Not running test.");
            }
            else
            {
                s_done = false;
                Thread food = null;
                food = new Thread(new ThreadStart(gc_food));
                food.Start();

                foreach (String portName in portNames)
                {
                    Debug.WriteLine(portName);
                    OpenClose(portName);
                    LeaveOpen(portName);
                }
            }
            // if neither of these crashed, then pass.
            s_done = true;  // stop the gc thread
            return 100;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Unexpected exception!  " + ex.ToString());
            return 99;
        }
    }

    private static void OpenClose(String portName)
    {
        Debug.WriteLine("OpenClose (should finish quickly)");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        SerialPort Port = new SerialPort(portName);
        Port.Open();
        Port.Close();
        Port.Open();
        Port.Close();
        Port.Open();
        Port.Close();
        Port.Open();
        Port.Close();
        Port.Open();
        Port.Close();
        stopwatch.Stop();
        Debug.WriteLine("Elapsed time: {0:mm'm'ss'.'fff's'}", stopwatch.Elapsed);
    }

    private static void LeaveOpen(String portName)
    {
        Debug.WriteLine("LeaveOpen (runs for 1 minute)");
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        Thread.Sleep(100);
        SerialPort Port = new SerialPort(portName);
        Port.Open();
        while (Port.IsOpen && stopwatch.ElapsedMilliseconds < 60000) // 1 minute
        {
            Thread.Sleep(10);
        }
        Port.Close();
        stopwatch.Stop();
        Debug.WriteLine("Elapsed time: {0:mm'm'ss'.'fff's'}", stopwatch.Elapsed);
    }
}
