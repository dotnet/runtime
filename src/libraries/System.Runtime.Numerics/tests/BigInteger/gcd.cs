// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class gcdTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void SpecialRegressionTests()
        {
            byte[] tempByteArray1;
            byte[] tempByteArray2;

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 1 };
            tempByteArray2 = new byte[] { 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 41 };
            tempByteArray2 = new byte[] { 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 2 };
            tempByteArray2 = new byte[] { 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 2 };
            tempByteArray2 = new byte[] { 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 255, 255, 255, 255, 1 };
            tempByteArray2 = new byte[] { 255, 255, 255, 255, 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 255, 255, 255, 255, 1 };
            tempByteArray2 = new byte[] { 255, 255, 255, 255, 1 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 1 };
            tempByteArray2 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 1 };
            tempByteArray2 = new byte[] { 255, 255, 255, 255, 255, 255, 255, 255, 1 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            tempByteArray2 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            tempByteArray2 = new byte[] { 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 254 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
            tempByteArray2 = new byte[] { 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 0 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

            tempByteArray1 = new byte[] { 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 255, 0 };
            tempByteArray2 = new byte[] { 0, 0, 0, 0, 255, 255, 255, 255, 255, 255, 255, 254 };
            VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
        }

        [Fact]
        public static void RunGCDTests()
        {
            byte[] tempByteArray1 = new byte[0];
            byte[] tempByteArray2 = new byte[0];

            // GCD Method - Two Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
            }

            // GCD Method - Two Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
            }

            // GCD Method - One large and one small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
            }

            // GCD Method - One large BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                tempByteArray2 = new byte[] { 0 };
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
            }

            // GCD Method - One small BigIntegers and zero
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                tempByteArray2 = new byte[] { 0 };
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");

                tempByteArray1 = new byte[] { 0 };
                tempByteArray2 = GetRandomByteArray(s_random, 2);
                VerifyGCDString(Print(tempByteArray1) + Print(tempByteArray2) + "bGCD");
            }

            // Identity GCD(GCD(x,y),z) == GCD(GCD(y,z),x)
            // Check some identities
            // (x+y)+z = (y+z)+x

            VerifyIdentityString( 
                Int64.MaxValue.ToString() + " " + Int32.MaxValue.ToString() + " bGCD " + Int16.MaxValue.ToString() + " bGCD",
                Int32.MaxValue.ToString() + " " + Int16.MaxValue.ToString() + " bGCD " + Int64.MaxValue.ToString() + " bGCD"
            );

            byte[] x = GetRandomByteArray(s_random);
            byte[] y = GetRandomByteArray(s_random);
            byte[] z = GetRandomByteArray(s_random);

            VerifyIdentityString(Print(x) + Print(y) + Print(z) + "bGCD bGCD", Print(y) + Print(z) + Print(x) + "bGCD bGCD");
        }

        private static void VerifyGCDString(string opstring)
        {
            StackCalc sc = new StackCalc(opstring);
            while (sc.DoNextOperation())
            {
                Assert.Equal(sc.snCalc.Peek().ToString(), sc.myCalc.Peek().ToString());
            }
        }

        private static void VerifyIdentityString(string opstring1, string opstring2)
        {
            StackCalc sc1 = new StackCalc(opstring1);
            while (sc1.DoNextOperation())
            {
                //Run the full calculation
                sc1.DoNextOperation();
            }

            StackCalc sc2 = new StackCalc(opstring2);
            while (sc2.DoNextOperation())
            {	
                //Run the full calculation
                sc2.DoNextOperation();
            }

            Assert.Equal(sc1.snCalc.Peek().ToString(), sc2.snCalc.Peek().ToString());
        }

        private static byte[] GetRandomByteArray(Random random)
        {
            return GetRandomByteArray(random, random.Next(0, 100));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetRandomByteArray(random, size);
        }

        private static string Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
