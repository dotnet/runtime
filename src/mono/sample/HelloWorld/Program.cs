using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.CompilerServices;
using System.Numerics;

namespace HelloWorld
{
    internal class Program
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool test(Vector<short> a, Vector<short> b, Vector<short> c)
        {
            // return AdvSimd.Multiply(AdvSimd.Subtract(AdvSimd.Add(a, b), c), b);
            // return Vector.Multiply(Vector.Subtract(Vector.Add(a, b), c), b);
            return Vector.EqualsAll(a,b);
            // return AdvSimd.Arm64.MinAcross(a);
        }

        private static void Main(string[] args)
        {
            Vector<short> A = new Vector<short>(-3);
            Vector<short> B = new Vector<short>(5);
            Vector<short> C = new Vector<short>(50);

            // Vector128<short> A = Vector128.Create((short)3.1);
            // Vector128<short> B = Vector128.Create((short)5.7);
            // Vector128<short> C = Vector128.Create((short)50);

            // Vector<short> result = Vector.Abs(A);
            // Vector<short> result = AdvSimd.Add(A, B);
            var result = test(A, B, C);
            Console.WriteLine(result);
        }
    }
}
