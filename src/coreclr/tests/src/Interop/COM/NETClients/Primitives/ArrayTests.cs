// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using TestLibrary;

    class ArrayTests
    {
        static private readonly IEnumerable<int> BaseData = Enumerable.Range(0, 10);

        private readonly Server.Contract.Servers.ArrayTesting server;
        private readonly double expectedMean;

        public ArrayTests()
        {
            this.server = (Server.Contract.Servers.ArrayTesting)new Server.Contract.Servers.ArrayTestingClass();

            double acc = 0.0;
            int[] rawData = BaseData.ToArray();
            foreach (var d in rawData)
            {
                acc += d;
            }

            expectedMean = acc / rawData.Length;
        }

        public void Run()
        {
            this.Marshal_ByteArray();
            this.Marshal_ShortArray();
            this.Marshal_UShortArray();
            this.Marshal_IntArray();
            this.Marshal_UIntArray();
            this.Marshal_LongArray();
            this.Marshal_ULongArray();
            this.Marshal_FloatArray();
            this.Marshal_DoubleArray();
        }

        static private bool EqualByBound(double expected, double actual)
        {
            double low = expected - 0.00001;
            double high = expected + 0.00001;
            double eps = Math.Abs(expected - actual);
            bool isEqual = eps < double.Epsilon || (low < actual && actual < high);
            if (!isEqual)
            {
                Console.WriteLine($"{expected}  {actual}");
            }

            return isEqual;
        }

        private void Marshal_ByteArray()
        {
            int len;
            byte[] data = BaseData.Select(i => (byte)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Byte_LP_PreLen(data.Length, data)), $"Mean_Byte_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Byte_LP_PostLen(data, data.Length)), $"Mean_Byte_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Byte_SafeArray_OutLen(data, out len)), $"Mean_Byte_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_ShortArray()
        {
            int len;
            short[] data = BaseData.Select(i => (short)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Short_LP_PreLen(data.Length, data)), $"Mean_Short_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Short_LP_PostLen(data, data.Length)), $"Mean_Short_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Short_SafeArray_OutLen(data, out len)), $"Mean_Short_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_UShortArray()
        {
            int len;
            ushort[] data = BaseData.Select(i => (ushort)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UShort_LP_PreLen(data.Length, data)), $"Mean_UShort_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UShort_LP_PostLen(data, data.Length)), $"Mean_UShort_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UShort_SafeArray_OutLen(data, out len)), $"Mean_UShort_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_IntArray()
        {
            int len;
            int[] data = BaseData.Select(i => i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Int_LP_PreLen(data.Length, data)), $"Mean_Int_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Int_LP_PostLen(data, data.Length)), $"Mean_Int_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Int_SafeArray_OutLen(data, out len)), $"Mean_Int_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_UIntArray()
        {
            int len;
            uint[] data = BaseData.Select(i => (uint)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UInt_LP_PreLen(data.Length, data)), $"Mean_UInt_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UInt_LP_PostLen(data, data.Length)), $"Mean_UInt_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_UInt_SafeArray_OutLen(data, out len)), $"Mean_UInt_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_LongArray()
        {
            int len;
            long[] data = BaseData.Select(i => (long)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Long_LP_PreLen(data.Length, data)), $"Mean_Long_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Long_LP_PostLen(data, data.Length)), $"Mean_Long_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Long_SafeArray_OutLen(data, out len)), $"Mean_Long_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_ULongArray()
        {
            int len;
            ulong[] data = BaseData.Select(i => (ulong)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_ULong_LP_PreLen(data.Length, data)), $"Mean_ULong_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_ULong_LP_PostLen(data, data.Length)), $"Mean_ULong_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_ULong_SafeArray_OutLen(data, out len)), $"Mean_ULong_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_FloatArray()
        {
            int len;
            float[] data = BaseData.Select(i => (float)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Float_LP_PreLen(data.Length, data)), $"Mean_Float_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Float_LP_PostLen(data, data.Length)), $"Mean_Float_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Float_SafeArray_OutLen(data, out len)), $"Mean_Float_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }

        private void Marshal_DoubleArray()
        {
            int len;
            double[] data = BaseData.Select(i => (double)i).ToArray();

            Console.WriteLine($"{data.GetType().Name} marshalling");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Double_LP_PreLen(data.Length, data)), $"Mean_Double_LP_PreLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Double_LP_PostLen(data, data.Length)), $"Mean_Double_LP_PostLen");
            Assert.IsTrue(EqualByBound(expectedMean, this.server.Mean_Double_SafeArray_OutLen(data, out len)), $"Mean_Double_SafeArray_OutLen");
            Assert.AreEqual(data.Length, len);
        }
    }
}
