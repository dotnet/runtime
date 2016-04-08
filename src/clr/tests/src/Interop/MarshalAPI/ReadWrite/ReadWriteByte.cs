using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;


public class ReadWriteByteTest
{
    private byte[] TestValues = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, byte.MaxValue };

    private void NullValueTests()
    {
        byte value;

        try
        {
            value = Marshal.ReadByte(IntPtr.Zero);
          
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
            value = Marshal.ReadByte(IntPtr.Zero, 2);           
            
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
            Marshal.WriteByte(IntPtr.Zero, TestValues[0]);
            
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
            Marshal.WriteByte(IntPtr.Zero, 2, TestValues[0]);
            
        }
        catch (Exception e)
        {
            if (e.GetType().FullName == "System.AccessViolationException") {
                
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
        Marshal.WriteByte(ptr, TestValues[0]);

        for (int i = 1; i < TestValues.Length; i++)
        {
            Marshal.WriteByte(ptr, i * Marshal.SizeOf(TestValues[0]), TestValues[i]);
        }

        byte value = Marshal.ReadByte(ptr);
        if (!value.Equals(TestValues[0]))
        {
            throw new Exception("Failed round trip ReadWrite test.");
        }

        for (int i = 1; i < TestValues.Length; i++)
        {
            value = Marshal.ReadByte(ptr, i * Marshal.SizeOf(TestValues[0]));
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
       new ReadWriteByteTest().RunTests();
       return 100;
    }
}
