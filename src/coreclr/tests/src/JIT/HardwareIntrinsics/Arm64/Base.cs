using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm.Arm64;

namespace Arm64intrisicsTest
{
    class Program
    {
        static void testUnaryOp<T>(String testCaseDescription, Func<T, int> binOp, Func<T, int> check, T value)
        {
            bool failed = false;
            try
            {
                int expected = check(value);
                int result = binOp(value);

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

        static void testThrowsPlatformNotSupported<T>(String testCaseDescription, Func<T, int> unaryOp, T value)
        {
            bool notSupported = false;

            try
            {
                unaryOp(value);
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

        static ulong bitsToUint64<T>(T x)
        {
            return Unsafe.As<T, ulong>(ref x) & ~(~0UL << 8*Unsafe.SizeOf<T>());
        }

        static int leadingZero<T>(T x)
            where T : IConvertible
        {
            ulong compare = 0x1UL << (8*Unsafe.SizeOf<T>() - 1);
            int result = 0;
            ulong value = bitsToUint64(x);

            while(value < compare)
            {
                result++;
                compare >>= 1;
            }

            return result;

            throw new Exception("Unexpected Type");
        }

        static int leadingSign<T>(T x)
            where T : IConvertible
        {
            ulong value = bitsToUint64(x);
            ulong signBit = value & (0x1UL << (8*Unsafe.SizeOf<T>() - 1));
            int result = 0;

            if (signBit == 0)
            {
                result = leadingZero(x);
            }
            else
            {
                result = leadingZero((T) Convert.ChangeType(value ^ (signBit + (signBit - 1)),  typeof(T)));
            }

            return result - 1;
        }

        static void TestLeadingSignCount()
        {
#if COREFX_HAS_ARM64_BASE
            String name = "LeadingSignCount";

            if (Base.IsSupported)
            {
                testUnaryOp<int >(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x),  0);
                testUnaryOp<int >(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x), -1);
                testUnaryOp<int >(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x),  1 << 30);
                testUnaryOp<int >(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x), -1 << 30);
                testUnaryOp<long>(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x),  0);
                testUnaryOp<long>(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x), -1);
                testUnaryOp<long>(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x),  1L << 60);
                testUnaryOp<long>(name, (x) => Base.LeadingSignCount(x), (x) => leadingSign(x), -1L << 60);
            }
            else
            {
                testThrowsPlatformNotSupported<int >(name, (x) => Base.LeadingSignCount(x), 0);
                testThrowsPlatformNotSupported<long>(name, (x) => Base.LeadingSignCount(x), 0);
            }

            Console.WriteLine($"Test{name} passed");
#endif // COREFX_HAS_ARM64_BASE
        }

        static void TestLeadingZeroCount()
        {
#if COREFX_HAS_ARM64_BASE
            String name = "LeadingZeroCount";

            if (Base.IsSupported)
            {
                testUnaryOp<int  >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  0);
                testUnaryOp<int  >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x), -1);
                testUnaryOp<int  >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  1 << 30);
                testUnaryOp<int  >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x), -1 << 30);
                testUnaryOp<uint >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  0);
                testUnaryOp<uint >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  1 << 30);
                testUnaryOp<long >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  0);
                testUnaryOp<long >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x), -1);
                testUnaryOp<long >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  1L << 60);
                testUnaryOp<long >(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x), -1L << 60);
                testUnaryOp<ulong>(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  0);
                testUnaryOp<ulong>(name, (x) => Base.LeadingZeroCount(x), (x) => leadingZero(x),  1L << 60);
            }
            else
            {
                testThrowsPlatformNotSupported<int  >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<uint >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<long >(name, (x) => Base.LeadingZeroCount(x), 0);
                testThrowsPlatformNotSupported<ulong>(name, (x) => Base.LeadingZeroCount(x), 0);
            }

            Console.WriteLine($"Test{name} passed");
#endif // COREFX_HAS_ARM64_BASE
        }

        static void ExecuteAllTests()
        {
            TestLeadingSignCount();
            TestLeadingZeroCount();
        }

        static int Main(string[] args)
        {
#if COREFX_HAS_ARM64_BASE
            Console.WriteLine($"System.Runtime.Intrinsics.Arm.Arm64.Base.IsSupported = {Base.IsSupported}");

            // Reflection call
            var issupported = "get_IsSupported";
            bool reflectedIsSupported = Convert.ToBoolean(typeof(Base).GetMethod(issupported).Invoke(null, null));

            Debug.Assert(reflectedIsSupported == Base.IsSupported, "Reflection result does not match");
#endif // COREFX_HAS_ARM64_BASE

            ExecuteAllTests();

            return 100;
        }
    }
}
