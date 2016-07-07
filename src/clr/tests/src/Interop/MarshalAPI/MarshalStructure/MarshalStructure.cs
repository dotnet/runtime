using System;
using System.Runtime.InteropServices;

class Program
{
    static int Main(string[] args)
    {
        VerifyByValBoolArray();
        VerifyByValArrayInStruct();
        VerfiyByValDateArray();
        return 100;
    }
    
    static void VerifyByValBoolArray()
    {
        var structure1 = new StructWithBoolArray()
        {
            array = new bool[]
            {
                true,true,true,true
            }
        };

        int size = Marshal.SizeOf(structure1);
        IntPtr memory = Marshal.AllocHGlobal(size + sizeof(Int32));

        try
        {
            Marshal.WriteInt32(memory, size, 0xFF);
            Marshal.StructureToPtr(structure1, memory, false);

            if (Marshal.ReadInt32(memory, size) != 0xFF)
                throw new Exception("Marshal.StructureToPtr buffer overwritten...");
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    static void VerifyByValArrayInStruct()
    {
        // equal
        var structure1 = new StructWithByValArray()
        {
            array = new StructWithIntField[]
            {
                new StructWithIntField { value = 1 },
                new StructWithIntField { value = 2 },
                new StructWithIntField { value = 3 },
                new StructWithIntField { value = 4 },
                new StructWithIntField { value = 5 }
            }
        };
        int size = Marshal.SizeOf(structure1);
        IntPtr memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structure1, memory, false);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }

        // underflow
        var structure2 = new StructWithByValArray()
        {
            array = new StructWithIntField[]
         {
                new StructWithIntField { value = 1 },
                new StructWithIntField { value = 2 },
                new StructWithIntField { value = 3 },
                new StructWithIntField { value = 4 }
         }
        };
        bool expectedException = false;
        size = Marshal.SizeOf(structure2);
        memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structure2, memory, false);
        }
        catch (ArgumentException)
        {
            expectedException = true;
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
        if (!expectedException)
            throw new Exception("Expected ArgumentException");

        // overflow
        var structure3 = new StructWithByValArray()
        {
            array = new StructWithIntField[]
         {
                new StructWithIntField { value = 1 },
                new StructWithIntField { value = 2 },
                new StructWithIntField { value = 3 },
                new StructWithIntField { value = 4 },
                new StructWithIntField { value = 5 },
                new StructWithIntField { value = 6 }
         }
        };

        size = Marshal.SizeOf(structure3);
        memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structure3, memory, false);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }
    
    static void VerfiyByValDateArray()
    {
        var structure1 = new StructWithDateArray()
        {
           array = new DateTime[]
           {
                DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now , DateTime.Now, DateTime.Now
           }
        };

        int size = Marshal.SizeOf(structure1);
        IntPtr memory = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(structure1, memory, false);
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    public struct StructWithIntField
    {
        public int value;
    }

    public struct StructWithByValArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public StructWithIntField[] array;
    }

    public struct StructWithBoolArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public bool[] array;
    }

    public struct StructWithDateArray
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public DateTime[] array;
    }
}