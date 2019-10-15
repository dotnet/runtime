// This is the template C# code from which the simplearg.il test case is derived
// It was produced by ildasm of the exe produced by this C# program
// The IL is edited to remove beforefieldinit
//  and to add an RVA initialization for the field 'Holder.RvaStatic'

using System;
using System.Runtime.CompilerServices;

namespace SimpleArg
{
    struct MyValue
    {
        public int IntValue;
        public int PadValue;
        public long LongValue;

        public MyValue(int init1, long init2) { IntValue = init1; PadValue = 0; LongValue = init2; }
    }

    class Holder
    {
        public static MyValue RvaStatic;
        public static MyValue NormalStatic = new MyValue(47, 11);
    }

    class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        static int Method(MyValue arg)
        {
            return arg.IntValue;
        }

        static int Main(string[] args)
        {
            int result = Method(Holder.RvaStatic);
            result += Method(Holder.NormalStatic);

            if (result == 100)
            {
                Console.WriteLine("Passed");
            }
            else
            {
                Console.WriteLine("Failed");
                Console.WriteLine(result);
            }
            return result;
        }
    }
}
