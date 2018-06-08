// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using CoreFXTestLibrary;

public class CopyInt32ArrayTest 
{
    private int[] TestArray = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

    private bool IsArrayEqual(int[] array1, int[] array2)
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

    private bool IsSubArrayEqual(int[] array1, int[] array2, int startIndex, int Length)
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
        int[] array = null;

        try
        {
            Marshal.Copy(array, 0, IntPtr.Zero, 0);

            Assert.Fail("Failed null values test. No exception from Copy when passed null as parameter.");
        }
        catch (ArgumentNullException)
        {
        }

        try
        {
            Marshal.Copy(IntPtr.Zero, array, 0, 0);

            Assert.Fail("Failed null values test. No exception from Copy when passed null as parameter.");
        }
        catch (ArgumentNullException)
        {
        }
    }

    private void OutOfRangeTests()
    {
        int sizeOfArray = sizeof(int) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        try //try to copy more elements than the TestArray has
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length + 1);

            Assert.Fail("Failed out of range values test. No exception from Copy when trying to copy more elements than the TestArray has.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        try //try to copy from an out of bound startIndex
        {
            Marshal.Copy(TestArray, TestArray.Length + 1, ptr, 1);

            Assert.Fail("Failed out of range values test. No exception from Copy when trying to copy from an out of bound startIndex.");
        }
        catch (ArgumentOutOfRangeException)
        {
            
        }

        try //try to copy from a positive startIndex, with length taking it out of bounds
        {
            Marshal.Copy(TestArray, 2, ptr, TestArray.Length);

            Assert.Fail("Failed out of range values test. No exception from Copy when trying to copy from a positive startIndex, with length taking it out of bounds.");
        }
        catch (ArgumentOutOfRangeException)
        {
        }

        Marshal.FreeCoTaskMem(ptr);
    }

    private void CopyRoundTripTests()
    {
        int sizeOfArray = sizeof(int) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        //try to copy the entire array
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length);

            int[] array = new int[TestArray.Length];

            Marshal.Copy(ptr, array, 0, TestArray.Length);

            if (!IsArrayEqual(TestArray, array))
            {
                Assert.Fail("Failed copy round trip test. Original array and round trip copied arrays do not match.");
            }
        }

        //try to copy part of the array
        {
            Marshal.Copy(TestArray, 2, ptr, TestArray.Length - 4);

            int[] array = new int[TestArray.Length];

            Marshal.Copy(ptr, array, 2, TestArray.Length - 4);

            if (!IsSubArrayEqual(TestArray, array, 2, TestArray.Length - 4))
            {
                Assert.Fail("Failed copy round trip test. Original array and round trip partially copied arrays do not match.");
            }
        }

        Marshal.FreeCoTaskMem(ptr);
    }

    public void RunTests()
    {        
        NullValueTests();
        OutOfRangeTests();
        CopyRoundTripTests();
    }

    public static int Main(String[] unusedArgs)
    {
        try
        {
            new CopyInt32ArrayTest().RunTests();
        }
        catch (Exception e)
        {
            Console.WriteLine("Test failure: " + e.Message);
            return 101;
        }

        return 100;
    }

}
