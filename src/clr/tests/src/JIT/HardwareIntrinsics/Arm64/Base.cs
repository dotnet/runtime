using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm.Arm64;

namespace Arm64intrisicsTest
{
    class Program
    {
        static void testUnaryOp<T>(String testCaseDescription, Func<T, int> func, int expected, T value)
        {
            bool failed = false;
            try
            {
                int result = func(value);

                if (result != expected)
                {
                    Console.WriteLine($"testUnaryOp<{typeof(T).Name}>{testCaseDescription}: Check Failed");
                    Console.WriteLine($"    result = {result}, expected = {expected}");
                    throw new Exception($"testUnaryOp<{typeof(T).Name}>{testCaseDescription}: Failed");
                }
            }
            catch
            {
                Console.WriteLine($"testUnaryOp<{typeof(T).Name}>{testCaseDescription}: Unexpected exception");
                throw;
            }
        }

        static void testThrowsPlatformNotSupported<T>(String testCaseDescription, Func<T, int> func, T value)
        {
            bool notSupported = false;

            try
            {
                func(value);
            }
            catch (PlatformNotSupportedException)
            {
                notSupported = true;
            }
            catch
            {
                Console.WriteLine($"testThrowsPlatformNotSupported: Unexpected exception");
                throw;
            }

            if (notSupported == false)
            {
                throw new Exception($"testThrowsPlatformNotSupported<{typeof(T).Name} >{testCaseDescription}: Failed");
            }
        }

        static int s_SignBit32 = 1 << 31;
        static long s_SignBit64 = 1L << 63;

        static int GenLeadingSignBitsI32(int num)
        {
            Debug.Assert(0 <= num && num < 32);
            return s_SignBit32 >> num;
        }

        static long GenLeadingSignBitsI64(int num)
        {
            Debug.Assert(0 <= num && num < 64);
            return s_SignBit64 >> num;
        }

        static uint GenLeadingZeroBitsU32(int num)
        {
            Debug.Assert(0 <= num && num <= 32);
            return (num < 32) ? (~0U >> num) : 0;
        }

        static int GenLeadingZeroBitsI32(int num)
        {
            return (int)GenLeadingZeroBitsU32(num);
        }

        static ulong GenLeadingZeroBitsU64(int num)
        {
            Debug.Assert(0 <= num && num <= 64);
            return (num < 64) ? (~0UL >> num) : 0;
        }

        static long GenLeadingZeroBitsI64(int num)
        {
            return (long)GenLeadingZeroBitsU64(num);
        }

        static void TestLeadingSignCount()
        {
            String name = "LeadingSignCount";

            if (Base.IsSupported)
            {
                for (int num = 0; num < 32; num++)
                {
                     testUnaryOp<int>(name, (x) => Base.LeadingSignCount(x), num,  GenLeadingSignBitsI32(num));
                }

                for (int num = 0; num < 64; num++)
                {
                     testUnaryOp<long>(name, (x) => Base.LeadingSignCount(x), num,  GenLeadingSignBitsI64(num));
                }
            }
            else
            {
                testThrowsPlatformNotSupported<int >(name, (x) => Base.LeadingSignCount(x), 0);
                testThrowsPlatformNotSupported<long>(name, (x) => Base.LeadingSignCount(x), 0);
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void TestLeadingZeroCount()
        {
            String name = "LeadingZeroCount";

            if (Base.IsSupported)
            {
                for (int num = 0; num <= 32; num++)
                {
                     testUnaryOp<int >(name, (x) => Base.LeadingZeroCount(x), num,  GenLeadingZeroBitsI32(num));
                     testUnaryOp<uint>(name, (x) => Base.LeadingZeroCount(x), num,  GenLeadingZeroBitsU32(num));
                }

                for (int num = 0; num <= 64; num++)
                {
                     testUnaryOp<long >(name, (x) => Base.LeadingZeroCount(x), num,  GenLeadingZeroBitsI64(num));
                     testUnaryOp<ulong>(name, (x) => Base.LeadingZeroCount(x), num,  GenLeadingZeroBitsU64(num));
                }
            }
            else
            {
                testThrowsPlatformNotSupported<int  >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<uint >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<long >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<ulong>(name, (x) => Base.LeadingZeroCount(x), 0);
            }

            Console.WriteLine($"Test{name} passed");
        }

        static void ExecuteAllTests()
        {
            TestLeadingSignCount();
            TestLeadingZeroCount();
        }

        static int Main(string[] args)
        {
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Base.IsSupported = {Base.IsSupported}");

            // Reflection call
            var issupported = "get_IsSupported";
            bool reflectedIsSupported = Convert.ToBoolean(typeof(Base).GetMethod(issupported).Invoke(null, null));

            Debug.Assert(reflectedIsSupported == Base.IsSupported, "Reflection result does not match");

            ExecuteAllTests();

            return 100;
        }
    }
}
