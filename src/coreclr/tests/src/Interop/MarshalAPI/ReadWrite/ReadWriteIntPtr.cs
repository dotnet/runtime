using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public class ReadWriteIntPtrTest
{
    private IntPtr[] TestValues;

    private void NullValueTests()
    {
        IntPtr value;

        try
        {
            value = Marshal.ReadIntPtr(IntPtr.Zero);
           
        }
        catch (Exception e)
        {
            if (e.GetType().FullName == "System.AccessViolationException")
            {
                
            }
            else if (e.GetType().FullName == "System.NullReferenceException") // ProjectN throws NullReferenceException
            {
                
            }
            else
            {
                throw e;
            }
        }

        try
        {
            value = Marshal.ReadIntPtr(IntPtr.Zero, 2);
            
        }
        catch (Exception e)
        {
            if (e.GetType().FullName == "System.AccessViolationException")
            {
                
            }
            else if (e.GetType().FullName == "System.NullReferenceException") // ProjectN throws NullReferenceException
            {
                
            }
            else
            {
                throw e;
            }
        }

        try
        {
            Marshal.WriteIntPtr(IntPtr.Zero, TestValues[0]);            
        }
        catch (Exception e)
        {
            if (e.GetType().FullName == "System.AccessViolationException")
            {

            }
            else if (e.GetType().FullName == "System.NullReferenceException") 
            {

            }
            else
            {
                throw e;
            }
        }

        try
        {
            Marshal.WriteIntPtr(IntPtr.Zero, 2, TestValues[0]);        
        }
        catch (Exception e)
        {
            if (e.GetType().FullName == "System.AccessViolationException")
            {

            }
            else if (e.GetType().FullName == "System.NullReferenceException") 
            {

            }
            else
            {
                throw e;
            }
        }
    }

    private void ReadWriteRoundTripTests()
    {
        int sizeOfArray = Marshal.SizeOf(TestValues[0]) * TestValues.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);


        Marshal.WriteIntPtr(ptr, TestValues[0]);

        for (int i = 1; i < TestValues.Length; i++)
        {
            Marshal.WriteIntPtr(ptr, i * Marshal.SizeOf(TestValues[0]), TestValues[i]);
        }


        IntPtr value = Marshal.ReadIntPtr(ptr);
        if (!value.Equals(TestValues[0]))
        {
            throw new Exception("Failed round trip ReadWrite test.");
        }

        for (int i = 1; i < TestValues.Length; i++)
        {
            value = Marshal.ReadIntPtr(ptr, i * Marshal.SizeOf(TestValues[0]));
            if (!value.Equals(TestValues[i]))
            {
                throw new Exception("Failed round trip ReadWrite test.");

            }
        }

        Marshal.FreeCoTaskMem(ptr);
    }

    public void RunTests()
    {
        NullValueTests();
        ReadWriteRoundTripTests();
    }

    public void Initialize()
    {        

        TestValues = new IntPtr[10];
        for (int i = 0; i < TestValues.Length; i++)
            TestValues[i] = new IntPtr(i);        
    }

    public static int Main(String[] unusedArgs)
    {
        ReadWriteIntPtrTest test = new ReadWriteIntPtrTest();
        test.Initialize();
        test.RunTests();
        return 100;
    }

}
