// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Tools;
using Xunit;

namespace System.Numerics.Tests
{
    public class multiplyTest
    {
        private static int s_samples = 10;
        private static int s_seed = 100;

        [Fact]
        public static void RunMultiply_TwoLargeBigIntegers()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random);
                tempByteArray2 = GetRandomByteArray(random);
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_TwoSmallBigIntegers()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random, 2);
                tempByteArray2 = GetRandomByteArray(random, 2);
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_OneLargeOneSmall()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                try
                {
                    tempByteArray1 = GetRandomByteArray(random);
                    tempByteArray2 = GetRandomByteArray(random, 2);
                    Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");

                    tempByteArray1 = GetRandomByteArray(random, 2);
                    tempByteArray2 = GetRandomByteArray(random);
                    Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");
                }
                catch (IndexOutOfRangeException)
                {
                    Console.WriteLine("Array1: " + Print(tempByteArray1));
                    Console.WriteLine("Array2: " + Print(tempByteArray2));
                    throw;
                }
            }
        }

        [Fact]
        public static void RunMultiply_OneLargeOneZero()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(random);
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_OneSmallOneZero()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Multiply Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(random, 2);
                tempByteArray2 = new byte[] { 0 };
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(random, 2);
                Assert.True(VerifyMultiplyString(Print(tempByteArray1) + Print(tempByteArray2) + "bMultiply"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_AxiomXX1()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X*1 = X
            Assert.True(VerifyIdentityString(Int32.MaxValue + " " + BigInteger.One + " bMultiply", Int32.MaxValue.ToString()), " Verification Failed");
            Assert.True(VerifyIdentityString(Int64.MaxValue + " " + BigInteger.One + " bMultiply", Int64.MaxValue.ToString()), " Verification Failed");

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(random));
                Assert.True(VerifyIdentityString(randBigInt + BigInteger.One + " bMultiply", randBigInt + "u+"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_AxiomXX0()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: X*0 = 0
            Assert.True(VerifyIdentityString(Int32.MaxValue + " " + BigInteger.Zero + " bMultiply", BigInteger.Zero.ToString()), " Verification Failed");
            Assert.True(VerifyIdentityString(Int64.MaxValue + " " + BigInteger.Zero + " bMultiply", BigInteger.Zero.ToString()), " Verification Failed");

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt = Print(GetRandomByteArray(random));
                ;
                Assert.True(VerifyIdentityString(randBigInt + BigInteger.Zero + " bMultiply", BigInteger.Zero.ToString()), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_Commutat()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // Axiom: a*b = b*a
            Assert.True(VerifyIdentityString(Int32.MaxValue + " " + Int64.MaxValue + " bMultiply", Int64.MaxValue + " " + Int32.MaxValue + " bMultiply"), " Verification Failed");

            for (int i = 0; i < s_samples; i++)
            {
                String randBigInt1 = Print(GetRandomByteArray(random));
                ;
                String randBigInt2 = Print(GetRandomByteArray(random));
                ;
                Assert.True(VerifyIdentityString(randBigInt1 + randBigInt2 + "bMultiply", randBigInt2 + randBigInt1 + "bMultiply"), " Verification Failed");
            }
        }

        [Fact]
        public static void RunMultiply_Boundary()
        {
            Random random = new Random(s_seed);
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            //Check interesting cases for boundary conditions
            //You'll either be shifting a 0 or 1 across the boundary
            // 32 bit boundary  n2=0
            Assert.True(VerifyMultiplyString(Math.Pow(2, 32) + " 2 bMultiply"), " Verification Failed");

            // 32 bit boundary  n1=0 n2=1
            Assert.True(VerifyMultiplyString(Math.Pow(2, 33) + " 2 bMultiply"), " Verification Failed");
        }

        private static bool VerifyMultiplyString(string opstring)
        {
            bool ret = true;
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                ret &= Eval(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger {0} Mine {1}", sc.snCalc.Peek(), sc.myCalc.Peek()));
            }
            return ret;
        }
        private static bool VerifyIdentityString(string opstring1, string opstring2)
        {
            bool ret = true;

            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {	//Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {	//Run the full calculation
                sc2.DoNextOperation();
            }

            ret &= Eval(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString(), String.Format("Out of Sync stacks found.  BigInteger1: {0} BigInteger2: {1}", sc1.snCalc.Peek(), sc2.snCalc.Peek()));

            return ret;
        }

        private static Byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 100));
        }
        private static Byte[] GetRandomByteArray(Random random, int size)
        {
            byte[] value = new byte[size];

            for (int i = 0; i < value.Length; ++i)
            {
                value[i] = (byte)random.Next(0, 256);
            }

            return value;
        }

        private static String Print(byte[] bytes)
        {
            String ret = "make ";

            for (int i = 0; i < bytes.Length; i++)
            {
                ret += bytes[i] + " ";
            }
            ret += "endmake ";

            return ret;
        }

        public static bool Eval<T>(T expected, T actual, String errorMsg)
        {
            bool retValue = expected == null ? actual == null : expected.Equals(actual);

            if (!retValue)
                return Eval(retValue, errorMsg +
                " Expected:" + (null == expected ? "<null>" : expected.ToString()) +
                " Actual:" + (null == actual ? "<null>" : actual.ToString()));

            return true;
        }
        public static bool Eval(bool expression, string message)
        {
            if (!expression)
            {
                Console.WriteLine(message);
            }

            return expression;
        }
    }
}
