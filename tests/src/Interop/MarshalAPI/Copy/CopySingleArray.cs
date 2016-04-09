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


public class CopySingleArrayTest 
{
    private float[] TestArray = { 0.0F, 1.0F, 2.0F, 3.0F, 4.0F, 5.0F, 6.0F, 7.0F, 8.0F, 9.0F };

    private bool IsArrayEqual(float[] array1, float[] array2)
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

    private bool IsSubArrayEqual(float[] array1, float[] array2, int startIndex, int Length)
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
        float[] array = null;

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
            Assert.ErrorWriteLine("Exception occurred: {0}", ex);
        }

        try
        {
            Marshal.Copy(IntPtr.Zero, array, 0, 0);
            Assert.ErrorWriteLine("Failed null values test.");            
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
        int sizeOfArray = sizeof(float) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        try //try to copy more elements than the TestArray has
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length + 1);
            Assert.ErrorWriteLine("Failed out of range values test.");
            
        }
        catch (ArgumentOutOfRangeException)
        {
            
        }
        catch (Exception ex)
        {
            Assert.ErrorWriteLine("Failed out of range values test.");
            Assert.ErrorWriteLine("Exception occurred: {0}", ex);
        }

        try //try to copy from an out of bound startIndex
        {
            Marshal.Copy(TestArray, TestArray.Length + 1, ptr, 1);
            Assert.ErrorWriteLine("Failed out of range values test.");            
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
        int sizeOfArray = sizeof(float) * TestArray.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);

        try //try to copy the entire array
        {
            Marshal.Copy(TestArray, 0, ptr, TestArray.Length);

            float[] array = new float[TestArray.Length];

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
            Assert.ErrorWriteLine("Exception occurred: {0}", ex);
        }

        try //try to copy part of the array
        {
            Marshal.Copy(TestArray, 2, ptr, TestArray.Length - 4);

            float[] array = new float[TestArray.Length];

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
        if (new CopySingleArrayTest().RunTests())
            return 100;

        return 99;
    }

}
