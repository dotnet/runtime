// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class absolutevalueTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunAbsoluteValueTests()
        {
            byte[] byteArray = new byte[0];

            // AbsoluteValue Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                byteArray = GetRandomByteArray(s_random);
                VerifyAbsoluteValueString(Print(byteArray) + "uAbs");
            }

            // AbsoluteValue Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                byteArray = MyBigIntImp.GetRandomByteArray(s_random, 2);
                VerifyAbsoluteValueString(Print(byteArray) + "uAbs");
            }

            // AbsoluteValue Method - zero
            VerifyAbsoluteValueString("0 uAbs");

            // AbsoluteValue Method - -1
            VerifyAbsoluteValueString("-1 uAbs");

            // AbsoluteValue Method - 1
            VerifyAbsoluteValueString("1 uAbs");

            // AbsoluteValue Method - Int32.MinValue
            VerifyAbsoluteValueString(Int32.MinValue.ToString() + " uAbs");

            // AbsoluteValue Method - Int32.MinValue-1
            VerifyAbsoluteValueString(Int32.MinValue.ToString() + " -1 b+ uAbs");

            // AbsoluteValue Method - Int32.MinValue+1
            VerifyAbsoluteValueString(Int32.MinValue.ToString() + " 1 b+ uAbs");

            // AbsoluteValue Method - Int32.MaxValue
            VerifyAbsoluteValueString(Int32.MaxValue.ToString() + " uAbs");

            // AbsoluteValue Method - Int32.MaxValue-1
            VerifyAbsoluteValueString(Int32.MaxValue.ToString() + " -1 b+ uAbs");

            // AbsoluteValue Method - Int32.MaxValue+1
            VerifyAbsoluteValueString(Int32.MaxValue.ToString() + " 1 b+ uAbs");

            // AbsoluteValue Method - Int64.MinValue
            VerifyAbsoluteValueString(Int64.MinValue.ToString() + " uAbs");

            // AbsoluteValue Method - Int64.MinValue-1
            VerifyAbsoluteValueString(Int64.MinValue.ToString() + " -1 b+ uAbs");

            // AbsoluteValue Method - Int64.MinValue+1
            VerifyAbsoluteValueString(Int64.MinValue.ToString() + " 1 b+ uAbs");

            // AbsoluteValue Method - Int64.MaxValue
            VerifyAbsoluteValueString(Int64.MaxValue.ToString() + " uAbs");

            // AbsoluteValue Method - Int64.MaxValue-1
            VerifyAbsoluteValueString(Int64.MaxValue.ToString() + " -1 b+ uAbs");

            // AbsoluteValue Method - Int64.MaxValue+1
            VerifyAbsoluteValueString(Int64.MaxValue.ToString() + " 1 b+ uAbs");
        }

        private static void VerifyAbsoluteValueString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return MyBigIntImp.GetRandomByteArray(random, random.Next(0, 1024));
        }

        private static String Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
