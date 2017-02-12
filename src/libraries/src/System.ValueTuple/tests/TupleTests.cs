// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if netcoreapp
using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using System.Runtime.CompilerServices;

namespace System.Tests
{
    public class TupleTests
    {
        [Fact]
        public void Test()
        {
            // Subset of the Tuple tests related to ITuple
            OneTuples();
            TwoTuples();
            ThreeTuples();
            FourTuples();
            FiveTuples();
            SixTuples();
            SevenTuples();
            EightTuples();
        }

        public static void OneTuples()
        {
            ITuple it = Tuple.Create(1);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Throws<IndexOutOfRangeException>(() => it[1].ToString());
        }

        public static void TwoTuples()
        {
            ITuple it = Tuple.Create(1, 2);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Throws<IndexOutOfRangeException>(() => it[2].ToString());
        }

        public static void ThreeTuples()
        {
            ITuple it = Tuple.Create(1, 2, 3);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Throws<IndexOutOfRangeException>(() => it[3].ToString());
        }

        public static void FourTuples()
        {
            ITuple it = Tuple.Create(1, 2, 3, 4);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Throws<IndexOutOfRangeException>(() => it[4].ToString());
        }

        public static void FiveTuples()
        {
            ITuple it = Tuple.Create(1, 2, 3, 4, 5);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Throws<IndexOutOfRangeException>(() => it[5].ToString());
        }

        public static void SixTuples()
        {
            ITuple it = Tuple.Create(1, 2, 3, 4, 5, 6);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Throws<IndexOutOfRangeException>(() => it[6].ToString());
        }

        public static void SevenTuples()
        {
            ITuple it = Tuple.Create(1, 2, 3, 4, 5, 6, 7);
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Equal(7, it[6]);
            Assert.Throws<IndexOutOfRangeException>(() => it[7].ToString());
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest)
        {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
        }

        public static void EightTuples()
        {
            ITuple it = CreateLongRef(1, 2, 3, 4, 5, 6, 7, Tuple.Create(8));
            Assert.Throws<IndexOutOfRangeException>(() => it[-1].ToString());
            Assert.Equal(1, it[0]);
            Assert.Equal(2, it[1]);
            Assert.Equal(3, it[2]);
            Assert.Equal(4, it[3]);
            Assert.Equal(5, it[4]);
            Assert.Equal(6, it[5]);
            Assert.Equal(7, it[6]);
            Assert.Equal(8, it[7]);
            Assert.Throws<IndexOutOfRangeException>(() => it[8].ToString());
        }
    }
}
#endif
