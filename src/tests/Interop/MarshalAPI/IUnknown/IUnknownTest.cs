// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Xunit;

#pragma warning disable 618

public class TestClass
{
    public int value;
    public string str;

    public TestClass()
    {
        value = int.MaxValue;
        str = "Test String";
    }

    public override string ToString()
    {
        return str + value;
    }
}

public class IUnknownMarshalingTest
{
    private object[] TestObjects;

    public unsafe void GetIUnknownForObjectTest()
    {
        // Test null
        Assert.Throws<ArgumentNullException>(() => Marshal.GetIUnknownForObject(null));
        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.GetIUnknownForObject(obj);

                // Validate IUnknown AddRef/Release usage
                int plusOne = Marshal.AddRef(ptr);
                int count = Marshal.Release(ptr);
                if ((plusOne - 1) != count)
                {
                    throw new Exception("Ref counts do not work as expected");
                }

                // Validate IUnknown QueryInterface usage
                Guid noIID = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
                int hr = Marshal.QueryInterface(ptr, noIID, out IntPtr _);
                if (hr == 0)
                {
                    throw new Exception("QueryInterface() does not work as expected");
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.Release(ptr);
                }
            }
        }
    }

    public void GetObjectForIUnknownTest()
    {
        // Test null
        Assert.Throws<ArgumentNullException>(() => Marshal.GetObjectForIUnknown(IntPtr.Zero));

        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.GetIUnknownForObject(obj);

                var tmpObj = Marshal.GetObjectForIUnknown(ptr);

                //compare the new object reference with the original object, they should point to the same underlying object
                if (!object.ReferenceEquals(obj, tmpObj))
                {
                    throw new Exception("GetObjectForIUnknown returned a different object. Original: " + obj + ", New: " + tmpObj);
                }
            }
            finally
            {
                if (ptr != IntPtr.Zero)
                {
                    Marshal.Release(ptr);
                }
            }
        }
    }

    public void RunTests()
    {
        GetIUnknownForObjectTest();
        GetObjectForIUnknownTest();
    }

    public void Initialize()
    {
        TestObjects = new object[7];
        TestObjects[0] = 1;                             //int
        TestObjects[1] = 'a';                           //char
        TestObjects[2] = false;                         //bool
        TestObjects[3] = "string";                      //string
        TestObjects[4] = new TestClass();               //Object of type TestClass
        TestObjects[5] = new List<int>();               //Projected Type
        TestObjects[6] = new Nullable<int>(2);          //Nullable Type
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [SkipOnMono("Requires COM support")]
    public static void Run()
    {
        IUnknownMarshalingTest testObj = new IUnknownMarshalingTest();
        testObj.Initialize();
        testObj.RunTests();
    }

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsBuiltInComEnabled))]
    [SkipOnMono("Requires COM support")]
    public static void RunInALC()
    {
        TestLibrary.Utilities.ExecuteAndUnload(typeof(IUnknownMarshalingTest).Assembly.Location, nameof(IUnknownMarshalingTest), nameof(Run));
    }

}
#pragma warning restore 618
