using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace coreclr_test_13647
{
    public class doublenegate
    {
        static int _dummyValueInt = 6;
        static double _dummyValueDouble = 6.0;

        [Fact]
        public static int TestEntryPoint()
        {
            if (Test1() && Test2() && Test3()) {
                Console.WriteLine("PASSED");
                return 100;
            }
            Console.WriteLine("FAILED");
            return 1;
        }

        static bool Test1()
        {
            var x = DoubleNegationInt(6);
            var y = 
                DoubleNegationInt(
                    DoubleNegationInt(
                        DoubleNegationInt(x)));
            var z = DoubleNegationInt(_dummyValueInt);

           if (x == 6 && y == 6 && z == 6)
                return true;
            return false;
        }       

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static int NegationInt(int value) => -value;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static int DoubleNegationInt(int value) => NegationInt(NegationInt(value));

        static bool Test2()
        {
            var x = DoubleNegationDouble(6.0);
            var y = 
                DoubleNegationDouble(
                    DoubleNegationDouble(
                        DoubleNegationDouble(x)));
            var z = DoubleNegationDouble(_dummyValueDouble);

            if (x == 6.0 && y == 6.0 && z == 6.0)
                return true;
            return false;
        }       

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static double NegationDouble(double value) => -value;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static double DoubleNegationDouble(double value) => NegationDouble(NegationDouble(value));

        static bool Test3()
        {
            var x = DoubleNotInt(6);
            var y = 
                DoubleNotInt(
                    DoubleNotInt(
                        DoubleNotInt(x)));
            var z = DoubleNotInt(_dummyValueInt);

           if (x == 6 && y == 6 && z == 6)
                return true;
            return false;
        }       

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static int NotInt(int value) => ~value;

        [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
        static int DoubleNotInt(int value) => NotInt(NotInt(value));
    }
}
