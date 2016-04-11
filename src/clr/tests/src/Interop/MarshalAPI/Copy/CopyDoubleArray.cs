﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CoreFXTestLibrary;

public class CopyDoubleArrayTest 
{
    private double[] TestArray = { 0.0, 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0 };

    private bool IsArrayEqual(double[] array1, double[] array2)
    {
        if (array1.Length != array2.Length)
        {            
            return false;
        }

        for (int i = 0; i < array1.Length; i++)
            if (!array1[i].Equals(array2[i]))
            {         
                return false;
            }

        return true;
    }

    private bool IsSubArrayEqual(double[] array1, double[] array2, int startIndex, int Length)
    {
        if (startIndex + Length > array1.Length)
        {            
            return false;
        }

        if (startIndex + Length > array2.Length)
        {         
            return false;
        }

        for (int i = 0; i < Length; i++)
            if (!array1[startIndex + i].Equals(array2[startIndex + i]))
            {         
                return false;
            }

        return true;
    }

    private void NullValueTests()
    {
        double[] array = null;

        try
        {
            Marshal.Copy(array, 0, IntPtr.Zero, 0);
            Assert.ErrorWriteLine("Failed null values test.");
            Assert.ErrorWriteLine("No exception from Copy when passed null as parameter.");
        }
        catch (ArgumentNullException)
        {            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed null values test.");            
        }

        try
        {
            Marshal.Copy(IntPtr.Zero, array, 0, 0);

            Assert.ErrorWriteLine("Failed null values test.");
            Assert.ErrorWriteLine("No exception from Copy when passed null as parameter.");
        }
        catch (ArgumentNullException)
        {            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed null values test.");            
        }
    }

    private void OutOfRangeTests()
    {
        int sizeOfArray = sizeof(double) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        try //try to copy more elements than the TestArray has
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length + 1);

            Assert.ErrorWriteLine("Failed out of range values test.");
            Assert.ErrorWriteLine("No exception from Copy when trying to copy more elements than the TestArray has.");
        }
        catch (ArgumentOutOfRangeException)
        {            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed out of range values test.");            
        }

        try //try to copy from an out of bound startIndex
        {
            Marshal.Copy(TestArray, TestArray.Length + 1, ptr, 1);

            Assert.ErrorWriteLine("Failed out of range values test.");
            Assert.ErrorWriteLine("No exception from Copy when trying to copy from an out of bound startIndex.");
        }
        catch (ArgumentOutOfRangeException)
        {            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed out of range values test.");            
        }

        try //try to copy from a positive startIndex, with length taking it out of bounds
        {
            Marshal.Copy(TestArray, 2, ptr, TestArray.Length);

            Assert.ErrorWriteLine("Failed out of range values test.");
            Assert.ErrorWriteLine("No exception from Copy when trying to copy from a positive startIndex, with length taking it out of bounds.");
        }
        catch (ArgumentOutOfRangeException)
        {
            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed out of range values test.");            
        }

        Marshal.FreeCoTaskMem(ptr);
    }

    private void CopyRoundTripTests()
    {
        int sizeOfArray = sizeof(double) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        try //try to copy the entire array
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length);

            double[] array = new double[TestArray.Length];

            Marshal.Copy(ptr, array, 0, TestArray.Length);

            if (!IsArrayEqual(TestArray, array))
            {
                Assert.ErrorWriteLine("Failed copy round trip test");
                Assert.ErrorWriteLine("Original array and round trip copied arrays do not match.");
            }
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed copy round trip test.");            
        }

        try //try to copy part of the array
        {
            Marshal.Copy(TestArray, 2, ptr, TestArray.Length - 4);

            double[] array = new double[TestArray.Length];

            Marshal.Copy(ptr, array, 2, TestArray.Length - 4);

            if (!IsSubArrayEqual(TestArray, array, 2, TestArray.Length - 4))
            {
                Assert.ErrorWriteLine("Failed copy round trip test");
                Assert.ErrorWriteLine("Original array and round trip partially copied arrays do not match.");
            }
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed copy round trip test.");            
        }

        Marshal.FreeCoTaskMem(ptr);
    }

    public bool RunTests()
    {        
        NullValueTests();
        OutOfRangeTests();        
        CopyRoundTripTests();        
        return true;
    }

    public static int Main(String[] unusedArgs)
    {
        if (new CopyDoubleArrayTest().RunTests())
            return 100;
        return 99;
    }

}
