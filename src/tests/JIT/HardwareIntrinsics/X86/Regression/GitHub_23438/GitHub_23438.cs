using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace GitHub_23438
{
    public static class Program
    {
        private const int Pass = 100;
        private const int Fail = 0;

        [Fact]
        public static void Test()
        {
            bool succeeded = true;

            if (Sse.X64.IsSupported)
            {
                succeeded &= TestSseX64ConvertScalarToVector128Single();
                succeeded &= TestSseX64ConvertToInt64();
                succeeded &= TestSseX64ConvertToInt64WithTruncation();
            }

            if (Sse2.X64.IsSupported)
            {
                succeeded &= TestSse2X64ConvertScalarToVector128Double();
                succeeded &= TestSse2X64ConvertScalarToVector128Int64();
                succeeded &= TestSse2X64ConvertScalarToVector128UInt64();
                succeeded &= TestSse2X64ConvertToInt64_Vector128Double();
                succeeded &= TestSse2X64ConvertToInt64_Vector128Int64();
                succeeded &= TestSse2X64ConvertToInt64WithTruncation();
                succeeded &= TestSse2X64ConvertToUInt64();
                succeeded &= TestSse2X64StoreNonTemporal_Int64();
                succeeded &= TestSse2X64StoreNonTemporal_UInt64();
            }

            if (Sse41.X64.IsSupported)
            {
                succeeded &= TestSse41X64Extract_Int64();
                succeeded &= TestSse41X64Extract_UInt64();
                succeeded &= TestSse41X64Insert_Int64();
                succeeded &= TestSse41X64Insert_UInt64();
            }

            Assert.True(succeeded);
        }

        private static bool AreEqual(long expectedResult, long actualResult, [CallerMemberName] string methodName = "")
        {
            bool areEqual = (expectedResult == actualResult);

            if (!areEqual)
            {
                Console.WriteLine($"{methodName} failed. Expected: {expectedResult}; Actual: {actualResult}");
            }

            return areEqual;
        }

        private static bool AreEqual(ulong expectedResult, ulong actualResult, [CallerMemberName] string methodName = "")
        {
            bool areEqual = (expectedResult == actualResult);

            if (!areEqual)
            {
                Console.WriteLine($"{methodName} failed. Expected: {expectedResult}; Actual: {actualResult}");
            }

            return areEqual;
        }

        private static bool TestSseX64ConvertScalarToVector128Single()
        {
            Vector128<float> val = Sse.X64.ConvertScalarToVector128Single(Vector128<float>.Zero, long.MaxValue);
            float result = val.GetElement(0);
            return AreEqual(0x5F000000, BitConverter.SingleToInt32Bits(result));
        }

        private static bool TestSseX64ConvertToInt64()
        {
            Vector128<float> val = Vector128.CreateScalar((float)long.MaxValue);
            long result = Sse.X64.ConvertToInt64(val);
            return AreEqual(long.MinValue, result);
        }

        private static bool TestSseX64ConvertToInt64WithTruncation()
        {
            Vector128<float> val = Vector128.CreateScalar((float)long.MaxValue);
            long result = Sse.X64.ConvertToInt64WithTruncation(val);
            return AreEqual(long.MinValue, result);
        }

        private static bool TestSse2X64ConvertScalarToVector128Double()
        {
            Vector128<double> val = Sse2.X64.ConvertScalarToVector128Double(Vector128<double>.Zero, long.MaxValue);
            double result = val.GetElement(0);
            return AreEqual(0x43E0000000000000, BitConverter.DoubleToInt64Bits(result));
        }

        private static bool TestSse2X64ConvertScalarToVector128Int64()
        {
            Vector128<long> val = Sse2.X64.ConvertScalarToVector128Int64(long.MaxValue);
            long result = val.GetElement(0);
            return AreEqual(long.MaxValue, result);
        }

        private static bool TestSse2X64ConvertScalarToVector128UInt64()
        {
            Vector128<ulong> val = Sse2.X64.ConvertScalarToVector128UInt64(ulong.MaxValue);
            ulong result = val.GetElement(0);
            return AreEqual(ulong.MaxValue, result);
        }

        private static bool TestSse2X64ConvertToInt64_Vector128Double()
        {
            Vector128<double> val = Vector128.CreateScalar((double)long.MaxValue);
            long result = Sse2.X64.ConvertToInt64(val);
            return AreEqual(long.MinValue, result);
        }

        private static bool TestSse2X64ConvertToInt64_Vector128Int64()
        {
            Vector128<long> val = Vector128.CreateScalar(long.MaxValue);
            long result = Sse2.X64.ConvertToInt64(val);
            return AreEqual(long.MaxValue, result);
        }

        private static bool TestSse2X64ConvertToInt64WithTruncation()
        {
            Vector128<double> val = Vector128.CreateScalar((double)long.MaxValue);
            long result = Sse2.X64.ConvertToInt64WithTruncation(val);
            return AreEqual(long.MinValue, result);
        }

        private static bool TestSse2X64ConvertToUInt64()
        {
            Vector128<ulong> val = Vector128.CreateScalar(ulong.MaxValue);
            ulong result = Sse2.X64.ConvertToUInt64(val);
            return AreEqual(ulong.MaxValue, result);
        }

        private static unsafe bool TestSse2X64StoreNonTemporal_Int64()
        {
            long result;
            Sse2.X64.StoreNonTemporal(&result, long.MaxValue);
            return AreEqual(long.MaxValue, result);
        }

        private static unsafe bool TestSse2X64StoreNonTemporal_UInt64()
        {
            ulong result;
            Sse2.X64.StoreNonTemporal(&result, ulong.MaxValue);
            return AreEqual(ulong.MaxValue, result);
        }

        private static bool TestSse41X64Extract_Int64()
        {
            Vector128<long> val = Vector128.CreateScalar(long.MaxValue);
            long result = Sse41.X64.Extract(val, 0);
            return AreEqual(long.MaxValue, result);
        }

        private static bool TestSse41X64Extract_UInt64()
        {
            Vector128<ulong> val = Vector128.CreateScalar(ulong.MaxValue);
            ulong result = Sse41.X64.Extract(val, 0);
            return AreEqual(ulong.MaxValue, result);
        }

        private static bool TestSse41X64Insert_Int64()
        {
            Vector128<long> val = Sse41.X64.Insert(Vector128<long>.Zero, long.MaxValue, 0);
            long result = val.GetElement(0);
            return AreEqual(long.MaxValue, result);
        }

        private static bool TestSse41X64Insert_UInt64()
        {
            Vector128<ulong> val = Sse41.X64.Insert(Vector128<ulong>.Zero, ulong.MaxValue, 0);
            ulong result = val.GetElement(0);
            return AreEqual(ulong.MaxValue, result);
        }
    }
}
