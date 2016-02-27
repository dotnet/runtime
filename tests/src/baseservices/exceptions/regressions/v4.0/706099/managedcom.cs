// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Runtime.InteropServices;

[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
[Guid("2A9F41BC-94F4-4889-9F8A-E0290CEF1177")]
public class Class1COMInterop
{
    public delegate void Callback();

    [DllImport("NativePinvoke", CallingConvention=CallingConvention.StdCall)]
    public static extern void Native2(Callback call);

    [DispId(1)]
    public void Managed1() 
    {
        Console.WriteLine("Inside Managed1");
        try
        {
            Managed2();
        }
        finally
        {            
	        try
	        {
	            throw new ArgumentException();
	        }
	        catch(ArgumentException e)
	        {
	            Console.WriteLine("Managed1: Caught Exception: " + e);
	        }
        }
    }

    public static void Managed2()
    {
        Console.WriteLine("Inside Managed2");

        // pinvoke into Native2
        Native2( new Callback(Managed3) );

    }

    public static void Managed3()
    {
        Console.WriteLine("Inside Managed3()");
        Managed4();
    }

    public static void Managed4()
    {

        Console.WriteLine("Inside Managed4()");

        // Throw IndexOutOfBounds
        int[] IntArray = new int[10];
        IntArray[11] = 5;
    }
}