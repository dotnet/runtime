// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public class signTest
    {
        private static int s_samples = 10;
        private static Random s_random = new Random(100);

        [Fact]
        public static void RunSignTests()
        {
            byte[] tempByteArray1 = new byte[0];

            // Sign Method - Large BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random);
                VerifySignString(Print(tempByteArray1) + "uSign");
            }

            // Sign Method - Small BigIntegers
            for (int i = 0; i < s_samples; i++)
            {
                tempByteArray1 = GetRandomByteArray(s_random, 2);
                VerifySignString(Print(tempByteArray1) + "uSign");
            }

            // Sign Method - zero
            VerifySignString("0 uSign");

            // Sign Method - -1
            VerifySignString("-1 uSign");

            // Sign Method - 1
            VerifySignString("1 uSign");

            // Sign Method - Int32.MinValue
            VerifySignString(Int32.MinValue.ToString() + " uSign");

            // Sign Method - Int32.MinValue-1
            VerifySignString(Int32.MinValue.ToString() + " -1 b+ uSign");

            // Sign Method - Int32.MinValue+1
            VerifySignString(Int32.MinValue.ToString() + " 1 b+ uSign");
            
            // Sign Method - Int32.MaxValue
            VerifySignString(Int32.MaxValue.ToString() + " uSign");

            // Sign Method - Int32.MaxValue-1
            VerifySignString(Int32.MaxValue.ToString() + " -1 b+ uSign");

            // Sign Method - Int32.MaxValue+1
            VerifySignString(Int32.MaxValue.ToString() + " 1 b+ uSign");

            // Sign Method - Int64.MinValue
            VerifySignString(Int64.MinValue.ToString() + " uSign");

            // Sign Method - Int64.MinValue-1
            VerifySignString(Int64.MinValue.ToString() + " -1 b+ uSign");

            // Sign Method - Int64.MinValue+1
            VerifySignString(Int64.MinValue.ToString() + " 1 b+ uSign");

            // Sign Method - Int64.MaxValue
            VerifySignString(Int64.MaxValue.ToString() + " uSign");

            // Sign Method - Int64.MaxValue-1
            VerifySignString(Int64.MaxValue.ToString() + " -1 b+ uSign");

            // Sign Method - Int64.MaxValue+1
            VerifySignString(Int64.MaxValue.ToString() + " 1 b+ uSign");
        }

        private static void VerifySignString(string opstring)
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
            return GetRandomByteArray(random, random.Next(0, 1024));
        }

        private static byte[] GetRandomByteArray(Random random, int size)
        {
            return MyBigIntImp.GetNonZeroRandomByteArray(random, size);
        }

        private static String Print(byte[] bytes)
        {
            return MyBigIntImp.Print(bytes);
        }
    }
}
