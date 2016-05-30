// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    /// <summary>
    /// Provides extension methods for <see cref="Tuple"/> instances to interop with C# tuples features (deconstruction syntax, converting from and to <see cref="ValueTuple"/>).
    /// </summary>
    public static class TupleExtensions
    {
        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 1 elements.
        /// </summary>
        public static void Deconstruct<T1>(
            this Tuple<T1> value,
            out T1 item1)
        {
            item1 = value.Item1;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 2 elements.
        /// </summary>
        public static void Deconstruct<T1, T2>(
            this Tuple<T1, T2> value,
            out T1 item1, out T2 item2)
        {
            item1 = value.Item1;
            item2 = value.Item2;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 3 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3>(
            this Tuple<T1, T2, T3> value,
            out T1 item1, out T2 item2, out T3 item3)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 4 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4>(
            this Tuple<T1, T2, T3, T4> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 5 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5>(
            this Tuple<T1, T2, T3, T4, T5> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 6 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6>(
            this Tuple<T1, T2, T3, T4, T5, T6> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 7 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 8 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 9 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 10 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 11 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 12 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 13 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 14 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 15 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 16 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 17 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
            item17 = value.Rest.Rest.Item3;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 18 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
            item17 = value.Rest.Rest.Item3;
            item18 = value.Rest.Rest.Item4;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 19 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
            item17 = value.Rest.Rest.Item3;
            item18 = value.Rest.Rest.Item4;
            item19 = value.Rest.Rest.Item5;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 20 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
            item17 = value.Rest.Rest.Item3;
            item18 = value.Rest.Rest.Item4;
            item19 = value.Rest.Rest.Item5;
            item20 = value.Rest.Rest.Item6;
        }

        /// <summary>
        /// Deconstruct a propertly nested <see cref="Tuple"/> with 21 elements.
        /// </summary>
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20, out T21 item21)
        {
            item1 = value.Item1;
            item2 = value.Item2;
            item3 = value.Item3;
            item4 = value.Item4;
            item5 = value.Item5;
            item6 = value.Item6;
            item7 = value.Item7;
            item8 = value.Rest.Item1;
            item9 = value.Rest.Item2;
            item10 = value.Rest.Item3;
            item11 = value.Rest.Item4;
            item12 = value.Rest.Item5;
            item13 = value.Rest.Item6;
            item14 = value.Rest.Item7;
            item15 = value.Rest.Rest.Item1;
            item16 = value.Rest.Rest.Item2;
            item17 = value.Rest.Rest.Item3;
            item18 = value.Rest.Rest.Item4;
            item19 = value.Rest.Rest.Item5;
            item20 = value.Rest.Rest.Item6;
            item21 = value.Rest.Rest.Item7;
        }

        /// <summary>
        /// Make a properly nested <see cref="ValueTuple"/> from a properly nested <see cref="Tuple"/> with 21 elements.
        /// </summary>
        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value)
        {
            return CreateLong(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7,
                        CreateLong(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7,
                            ValueTuple.Create(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6, value.Rest.Rest.Item7)));
        }

        /// <summary>
        /// Make a properly nested <see cref="Tuple"/> from a properly nested <see cref="ValueTuple"/> with 21 elements.
        /// </summary>
        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>>
            ToRefTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> value)
        {
            return CreateLongRef(value.Item1, value.Item2, value.Item3, value.Item4, value.Item5, value.Item6, value.Item7,
                        CreateLongRef(value.Rest.Item1, value.Rest.Item2, value.Rest.Item3, value.Rest.Item4, value.Rest.Item5, value.Rest.Item6, value.Rest.Item7,
                            Tuple.Create(value.Rest.Rest.Item1, value.Rest.Rest.Item2, value.Rest.Rest.Item3, value.Rest.Rest.Item4, value.Rest.Rest.Item5, value.Rest.Rest.Item6, value.Rest.Rest.Item7)));
        }

        private static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLong<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : struct =>
            new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);

        private static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) =>
            new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
    }
}