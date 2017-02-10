// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a regression test for Dev10 #511456, DevDiv Bugs #186131, and DevDiv Bugs #191753.
// Unplugged USB virtual serial port causes SerialPort object to crash application in finalizer thread.
//
// For this manual test, you will need a USB Serial adapater, probably one like this:
// Keyspan (keyspan.com) model USA-19HS
// http://www.tripplite.com/en/products/model.cfm?txtSeriesID=518&txtModelID=3914
// There should be one around here somewhere.
// This site should have a driver if you need one.
//
// Connect the serial end to loopback (connect pins 3 and 4).
//
// Plug the adapter into a USB port on the machine, and run this test.
// By default it runs using COM3.  If you need a different port name, pass it as an argument.
// Unplug the adapter from the USB port when prompted.
// Look for no exception and a passing exit code.
//
// Before the fix, an unhandled exception would be seen:
// Unhandled Exception: System.UnauthorizedAccessException: Access to the port is denied.
//   at System.IO.Ports.InternalResources.WinIOError(Int32 errorCode, String str)
//   at System.IO.Ports.SerialStream.Dispose(Boolean disposing)
//   at System.IO.Ports.SerialStream.Finalize()

using System;
using System.Diagnostics;
using System.IO.Ports;

public class Test
{
    public static void UnhandledExceptionHandler(Object sender, UnhandledExceptionEventArgs eventArgs)
    {
        Debug.WriteLine("Exception caught in UnhandledExceptionHandler:  {0}", eventArgs.ExceptionObject);
        Environment.ExitCode = 97;
    }

    public static void Main(String[] args)
    {
        System.AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;

        try
        {
            Debug.WriteLine("Connect the USB/serial adapter to a USB port and press Enter to continue...");
            Console.ReadLine();

            // Get the port name, if one is passed in.
            String portName = "COM3";
            if (args.Length > 0)
                portName = args[0];
            SerialPort port = new SerialPort(portName);
            Debug.WriteLine("Using {0}", portName);

            try
            {
                port.Open();
            }
            catch (System.IO.IOException)
            {
                Debug.WriteLine("Error opening serial port.  This can happen if the adapter was just connected.  Sleeping 5 seconds before trying again...");
                System.Threading.Thread.Sleep(5000);
                try
                {
                    port.Open();
                }
                catch (System.IO.IOException ex)
                {
                    Debug.WriteLine("Error opening serial port.  Make sure the USB serial adapter is plugged into a USB port and that the port name is correct.");
                    Debug.WriteLine(ex);

                    Environment.ExitCode = 98;
                }
            }

            Debug.WriteLine("{0} opened.", port.PortName);
            Debug.WriteLine("Disconnect USB serial adapter and press Enter to continue...");
            Console.ReadLine();
            // Calling port.Close() after the adapter is disconnected will still get an 
            // UnauthorizedAccessException, so don't do it.
            Debug.WriteLine("Pass if no exception and exitcode==100.");
            Environment.ExitCode = 100;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception caught in Main() - {0}", ex);
            Environment.ExitCode = 99;
        }
    }
}
