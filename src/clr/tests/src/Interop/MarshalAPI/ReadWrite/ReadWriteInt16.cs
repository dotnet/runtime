﻿using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public class ReadWriteInt16Test
{
    private short[] TestValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, short.MaxValue };

    private void NullValueTests()
    {
        short value;

        try
        {
            value = Marshal.ReadInt16(IntPtr.Zero);          
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
            value = Marshal.ReadInt16(IntPtr.Zero, 2);

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
            Marshal.WriteInt16(IntPtr.Zero, TestValues[0]);            
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
            Marshal.WriteInt16(IntPtr.Zero, 2, TestValues[0]);            
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
    }

    private void ReadWriteRoundTripTests()
    {
        int sizeOfArray = Marshal.SizeOf(TestValues[0]) * TestValues.Length;

        IntPtr ptr = Marshal.AllocCoTaskMem(sizeOfArray);


        Marshal.WriteInt16(ptr, TestValues[0]);

        for (int i = 1; i < TestValues.Length; i++)
        {
            Marshal.WriteInt16(ptr, i * Marshal.SizeOf(TestValues[0]), TestValues[i]);
        }


        short value = Marshal.ReadInt16(ptr);
        if (!value.Equals(TestValues[0]))
        {
            throw new Exception("Failed round trip ReadWrite test.");

        }

        for (int i = 1; i < TestValues.Length; i++)
        {
            value = Marshal.ReadInt16(ptr, i * Marshal.SizeOf(TestValues[0]));
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

    public static int Main(String[] unusedArgs)
    {
        new ReadWriteInt16Test().RunTests();
        return 100;
    }

}
