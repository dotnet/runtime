using System;

namespace HelloWorld
{
    [StructLayout(LayoutKind.Sequential)]
    public struct InnerSequential
    {
        public int f1;
        public float f2;
        public String f3;
    }

    [StructLayout(LayoutKind.Sequential)]//struct containing one field of array type
    public struct InnerArraySequential
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public InnerSequential[] arr;
    }
    internal class Program
    {
        public static InnerArraySequential NewInnerArraySequential(int f1, float f2, string f3)
        {
            InnerArraySequential outer = new InnerArraySequential();
            outer.arr = new InnerSequential[Common.NumArrElements];
            for (int i = 0; i < Common.NumArrElements; i++)
            {
                outer.arr[i].f1 = f1;
                outer.arr[i].f2 = f2;
                outer.arr[i].f3 = f3;
            }
            return outer;
        }

        private static void Main(string[] args)
        {
            InnerArraySequential source_ias = NewInnerArraySequential(1, 1.0F, "some string");
        }
    }
}