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
        private static Vector128<int> test(Vector128<int> a, Vector128<int> b)
        {
            // return AdvSimd.Multiply(AdvSimd.Subtract(AdvSimd.Add(a, b), c), b);
            // return Vector128.Multiply(Vector128.Subtract(Vector128.Add(a, b), c), b);
            return Vector128.Add(a,b);
        }

        private static void Main(string[] args)
        {
            Vector128<int> A = Vector128.Create((int)3.1);
            Vector128<int> B = Vector128.Create((int)5.7);

            // Vector128<short> result = Vector128.Abs(A);
            // Vector128<int> result = AdvSimd.Add(A, B);
            var result = test(A, B);
            Console.WriteLine(result);
        }
    }
}
