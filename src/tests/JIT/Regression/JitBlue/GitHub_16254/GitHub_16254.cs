using System;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace UnsafeTesting
{
    public class Program
    {
        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public static Quaternion Add(Quaternion value1, Quaternion value2)
        {
            Vector4 q1 = Unsafe.As<Quaternion, Vector4>(ref value1);
            Vector4 q2 = Unsafe.As<Quaternion, Vector4>(ref value2);

            Vector4 result = q1 + q2;

            return Unsafe.As<Vector4, Quaternion>(ref result);
        }
        public static bool QuaternionAddTest()
        {
            Quaternion a = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
            Quaternion b = new Quaternion(5.0f, 6.0f, 7.0f, 8.0f);

            Quaternion expected = new Quaternion(6.0f, 8.0f, 10.0f, 12.0f);
            Quaternion actual;

            actual = Add(a, b);

            if (actual != expected)
            {
                return false;
            }
            return true;
        }
        [Fact]
        public static int TestEntryPoint()
        {
            if (QuaternionAddTest())
            {
                Console.WriteLine("PASS");
                return 100;
            }
            else
            {
                Console.WriteLine("FAIL");
                return -1;
            }
        }

    }
}
