// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using TestLibrary;

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
    [DllImport(@"IUnknownNative", CallingConvention = CallingConvention.Cdecl)]
    private static extern bool Marshal_IUnknown([In]IntPtr ptr);

    private object[] TestObjects;

    public void GetIUnknownForObjectTest()
    {
        
        try
        {
            //test null
            IntPtr nullPtr = Marshal.GetIUnknownForObject(null);            
        }
        catch (ArgumentNullException) { }
        

        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.GetIUnknownForObject(obj);

                if (!Marshal_IUnknown(ptr))
                {
                    throw new Exception("Failure on native side. Ref counts do not work as expected");
                }
            }          
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.Release(ptr);
            }
        }
    }

    public void GetComInterfaceForObjectTest()
    {

        //test null
        IntPtr nullPtr = Marshal.GetComInterfaceForObject(null, typeof(object));
            if (nullPtr != IntPtr.Zero)
                throw new Exception("A valid ptr was returned for null object.");
       
        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.GetComInterfaceForObject(obj, typeof(object));

                if (!Marshal_IUnknown(ptr))
                {
                    throw new Exception("Failure on native side. Ref counts do not work as expected");
                }
            }           
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.Release(ptr);
            }
        }
    }

    public void GetComInterfaceForObjectQueryInterfaceTest()
    {
            IntPtr nullPtr = Marshal.GetComInterfaceForObject(null, typeof(object), CustomQueryInterfaceMode.Allow);
            if (nullPtr != IntPtr.Zero)
                throw new Exception("A valid ptr was returned for null object.");

        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;
            ptr = Marshal.GetComInterfaceForObject(obj, typeof(object), CustomQueryInterfaceMode.Allow);
            try
            {
               if (!Marshal_IUnknown(ptr))
                {
                   throw new Exception("Failure on native side. Ref counts do not work as expected");
                } 
            }   
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.Release(ptr);
            }
        }
    }

    public void GetObjectForIUnknownTest()
    {
        try
        {
            //test IntPtr.Zero
            Object nullObj = Marshal.GetObjectForIUnknown(IntPtr.Zero);
           
        }
        catch (ArgumentNullException) { }

        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;

            try
            {
                ptr = Marshal.GetIUnknownForObject(obj);

                Object tmpObj = Marshal.GetObjectForIUnknown(ptr);

                //compare the new object reference with the original object, they should point to the same underlying object
                if (!object.ReferenceEquals(obj, tmpObj))
                    throw new Exception("GetObjectForIUnknown returned a different object. Original: " + obj + ", New: " + tmpObj);
            }          
            finally
            {
                if (ptr != IntPtr.Zero)
                    Marshal.Release(ptr);
            }
        }
    }

    public void GetUniqueObjectForIUnknownTest()
    {
        
            //test IntPtr.Zero
            Object nullObj = Marshal.GetUniqueObjectForIUnknown(IntPtr.Zero);

            if (nullObj != null)
                throw new Exception("Object returned for IntPtr.Zero is not null.");   
      

        foreach (object obj in TestObjects)
        {
            IntPtr ptr = IntPtr.Zero;
            object tmpObj = null;

            try
            {
                ptr = Marshal.GetIUnknownForObject(obj);

                tmpObj = Marshal.GetUniqueObjectForIUnknown(ptr);

                //compare the new object reference with the original object, they should point to differnet objects
                if (object.ReferenceEquals(obj, tmpObj))
                    throw new Exception("GetUniqueObjectForIUnknown returned the original object");

                //The value should be the same
                if (!obj.Equals(tmpObj))
                    throw new Exception("GetUniqueObjectForIUnknown returned an object with different value. Original: " + obj + ", New: " + tmpObj);

            }         
            finally
            {
                if (tmpObj != null)
                    Marshal.ReleaseComObject(tmpObj);
                if (ptr != IntPtr.Zero)
                    Marshal.Release(ptr);
            }
        }
    }

    public  bool RunTests()
    {
        Initialize();
        GetIUnknownForObjectTest();
        GetObjectForIUnknownTest();
        return true;
    }

    public bool Initialize()
    {
        TestObjects = new object[7];
        TestObjects[0] = 1;                             //int
        TestObjects[1] = 'a';                           //char
        TestObjects[2] = false;                         //bool
        TestObjects[3] = "string";                      //string
        TestObjects[4] = new TestClass();               //Object of type TestClass 
        TestObjects[5] = new List<int>();               //Projected Type
        TestObjects[6] = new Nullable<int>(2);          //Nullable Type        
        return true;
    }

    public static int Main(String[] unusedArgs)
    {
        IUnknownMarshalingTest testObj = new IUnknownMarshalingTest(); 
        testObj.Initialize();
        testObj.RunTests();
        return 100;
    }

}
#pragma warning restore 618
