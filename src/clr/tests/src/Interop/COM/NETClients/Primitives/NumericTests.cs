// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NetClient
{
    using System;
    using TestLibrary;

    class NumericTests
    {
        private readonly int seed;
        private readonly Random rng;
        private readonly Server.Contract.Servers.NumericTesting server;

        public NumericTests(int seed = 37)
        {
            this.seed = seed;
            Console.WriteLine($"Numeric RNG seed: {this.seed}");

            this.rng = new Random(this.seed);
            this.server = (Server.Contract.Servers.NumericTesting)new Server.Contract.Servers.NumericTestingClass();
        }

        public void Run()
        {
            int a = this.rng.Next();
            int b = this.rng.Next();

            this.Marshal_Byte((byte)a, (byte)b);
            this.Marshal_Short((short)a, (short)b);
            this.Marshal_UShort((ushort)a, (ushort)b);
            this.Marshal_Int(a, b);
            this.Marshal_UInt((uint)a, (uint)b);
            this.Marshal_Long(a, b);
            this.Marshal_ULong((ulong)a, (ulong)b);

            this.Marshal_Float(a / 100f, b / 100f);
            this.Marshal_Double(a / 100.0, b / 100.0);
            this.Marshal_ManyInts();
        }

        static private bool EqualByBound(float expected, float actual)
        {
            float low = expected - 0.0001f;
            float high = expected + 0.0001f;
            float eps = Math.Abs(expected - actual);
            return eps < float.Epsilon || (low < actual && actual < high);
        }

        static private bool EqualByBound(double expected, double actual)
        {
            double low = expected - 0.00001;
            double high = expected + 0.00001;
            double eps = Math.Abs(expected - actual);
            return eps < double.Epsilon || (low < actual && actual < high);
        }

        private void Marshal_Byte(byte a, byte b)
        {
            var expected = (byte)(a + b);
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_Byte(a, b));

            var c = byte.MaxValue;
            this.server.Add_Byte_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_Byte_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_Short(short a, short b)
        {
            var expected = (short)(a + b);
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_Short(a, b));

            var c = short.MaxValue;
            this.server.Add_Short_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_Short_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_UShort(ushort a, ushort b)
        {
            var expected = (ushort)(a + b);
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_UShort(a, b));

            var c = ushort.MaxValue;
            this.server.Add_UShort_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_UShort_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_Int(int a, int b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_Int(a, b));

            var c = int.MaxValue;
            this.server.Add_Int_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_Int_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_UInt(uint a, uint b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_UInt(a, b));

            var c = uint.MaxValue;
            this.server.Add_UInt_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_UInt_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_Long(long a, long b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_Long(a, b));

            var c = long.MaxValue;
            this.server.Add_Long_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_Long_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_ULong(ulong a, ulong b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.AreEqual(expected, this.server.Add_ULong(a, b));

            var c = ulong.MaxValue;
            this.server.Add_ULong_Ref(a, b, ref c);
            Assert.AreEqual(expected, c);

            c = 0;
            this.server.Add_ULong_Out(a, b, out c);
            Assert.AreEqual(expected, c);
        }

        private void Marshal_Float(float a, float b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.IsTrue(EqualByBound(expected, this.server.Add_Float(a, b)), $"Add_Float: {this.server.Add_Float(a, b)}");

            var c = float.MaxValue;
            this.server.Add_Float_Ref(a, b, ref c);
            Assert.IsTrue(EqualByBound(expected, c), "Add_Float_Ref");

            c = 0;
            this.server.Add_Float_Out(a, b, out c);
            Assert.IsTrue(EqualByBound(expected, c), "Add_Float_Out");
        }

        private void Marshal_Double(double a, double b)
        {
            var expected = a + b;
            Console.WriteLine($"{expected.GetType().Name} test invariant: {a} + {b} = {expected}");
            Assert.IsTrue(EqualByBound(expected, this.server.Add_Double(a, b)));

            var c = double.MaxValue;
            this.server.Add_Double_Ref(a, b, ref c);
            Assert.IsTrue(EqualByBound(expected, c));

            c = 0;
            this.server.Add_Double_Out(a, b, out c);
            Assert.IsTrue(EqualByBound(expected, c));
        }

        private void Marshal_ManyInts()
        {
            var expected = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11;
            Console.WriteLine($"{expected.GetType().Name} 11 test invariant: 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 = {expected}");
            Assert.IsTrue(expected == this.server.Add_ManyInts11(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11));
            expected = 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12;
            Console.WriteLine($"{expected.GetType().Name} 12 test invariant: 1 + 2 + 3 + 4 + 5 + 6 + 7 + 8 + 9 + 10 + 11 + 12 = {expected}");
            Assert.IsTrue(expected == this.server.Add_ManyInts12(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12));
        }
    }
}
