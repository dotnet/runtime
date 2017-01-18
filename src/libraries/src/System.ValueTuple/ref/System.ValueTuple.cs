// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public struct ValueTuple
        : IEquatable<ValueTuple>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple>
    {
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
        public static ValueTuple Create() { throw null; }
        public static ValueTuple<T1> Create<T1>(T1 item1) { throw null; }
        public static ValueTuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) { throw null; }
        public static ValueTuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) { throw null; }
        public static ValueTuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) { throw null; }
        public static ValueTuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { throw null; }
        public static ValueTuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { throw null; }
        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { throw null; }
        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) { throw null; }
    }
    public struct ValueTuple<T1>
        : IEquatable<ValueTuple<T1>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1>>
    {
        public T1 Item1;
        public ValueTuple(T1 item1) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2>
        : IEquatable<ValueTuple<T1, T2>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2>>
    {
        public T1 Item1;
        public T2 Item2;
        public ValueTuple(T1 item1, T2 item2) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3>
        : IEquatable<ValueTuple<T1, T2, T3>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public ValueTuple(T1 item1, T2 item2, T3 item3) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3, T4>
        : IEquatable<ValueTuple<T1, T2, T3, T4>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3, T4> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3, T4> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3, T4, T5>
        : IEquatable<ValueTuple<T1, T2, T3, T4, T5>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4, T5>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3, T4, T5> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3, T4, T5> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6>
        : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4, T5, T6>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3, T4, T5, T6> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7>
        : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3, T4, T5, T6, T7> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
    [Runtime.InteropServices.StructLayout(Runtime.InteropServices.LayoutKind.Auto)]
    public struct ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>
        : IEquatable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>, Collections.IStructuralEquatable, Collections.IStructuralComparable, IComparable, IComparable<ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>>
        where TRest : struct
    {
        public T1 Item1;
        public T2 Item2;
        public T3 Item3;
        public T4 Item4;
        public T5 Item5;
        public T6 Item6;
        public T7 Item7;
        public TRest Rest;
        public ValueTuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other) { throw null; }
        bool Collections.IStructuralEquatable.Equals(object other, Collections.IEqualityComparer comparer) { throw null; }
        int IComparable.CompareTo(object other) { throw null; }
        public int CompareTo(ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> other) { throw null; }
        int Collections.IStructuralComparable.CompareTo(object other, Collections.IComparer comparer) { throw null; }
        public override int GetHashCode() { throw null; }
        int Collections.IStructuralEquatable.GetHashCode(Collections.IEqualityComparer comparer) { throw null; }
        public override string ToString() { throw null; }
    }
}

namespace System
{
    using System.ComponentModel;
    public static class TupleExtensions
    {
        #region Deconstruct
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1>(
            this Tuple<T1> value,
            out T1 item1)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2>(
            this Tuple<T1, T2> value,
            out T1 item1, out T2 item2)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3>(
            this Tuple<T1, T2, T3> value,
            out T1 item1, out T2 item2, out T3 item3)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4>(
            this Tuple<T1, T2, T3, T4> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5>(
            this Tuple<T1, T2, T3, T4, T5> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6>(
            this Tuple<T1, T2, T3, T4, T5, T6> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20)
        {
            throw null;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Deconstruct<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
            this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value,
            out T1 item1, out T2 item2, out T3 item3, out T4 item4, out T5 item5, out T6 item6, out T7 item7, out T8 item8, out T9 item9, out T10 item10, out T11 item11, out T12 item12, out T13 item13, out T14 item14, out T15 item15, out T16 item16, out T17 item17, out T18 item18, out T19 item19, out T20 item20, out T21 item21)
        {
            throw null;
        }
        #endregion

        #region ToValueTuple
        public static ValueTuple<T1>
            ToValueTuple<T1>(
                this Tuple<T1> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2>
            ToValueTuple<T1, T2>(
                this Tuple<T1, T2> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3>
            ToValueTuple<T1, T2, T3>(
                this Tuple<T1, T2, T3> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4>
            ToValueTuple<T1, T2, T3, T4>(
                this Tuple<T1, T2, T3, T4> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5>
            ToValueTuple<T1, T2, T3, T4, T5>(
                this Tuple<T1, T2, T3, T4, T5> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6>
            ToValueTuple<T1, T2, T3, T4, T5, T6>(
                this Tuple<T1, T2, T3, T4, T5, T6> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>> value)
        {
            throw null;
        }

        public static ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>>
            ToValueTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
                this Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>> value)
        {
            throw null;
        }
        #endregion

        #region ToTuple
        public static Tuple<T1>
            ToTuple<T1>(
                this ValueTuple<T1> value)
        {
            throw null;
        }

        public static Tuple<T1, T2>
            ToTuple<T1, T2>(
                this ValueTuple<T1, T2> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3>
            ToTuple<T1, T2, T3>(
                this ValueTuple<T1, T2, T3> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4>
            ToTuple<T1, T2, T3, T4>(
                this ValueTuple<T1, T2, T3, T4> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5>
            ToTuple<T1, T2, T3, T4, T5>(
                this ValueTuple<T1, T2, T3, T4, T5> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6>
            ToTuple<T1, T2, T3, T4, T5, T6>(
                this ValueTuple<T1, T2, T3, T4, T5, T6> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7>
            ToTuple<T1, T2, T3, T4, T5, T6, T7>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20>>> value)
        {
            throw null;
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9, T10, T11, T12, T13, T14, Tuple<T15, T16, T17, T18, T19, T20, T21>>>
            ToTuple<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(
                this ValueTuple<T1, T2, T3, T4, T5, T6, T7, ValueTuple<T8, T9, T10, T11, T12, T13, T14, ValueTuple<T15, T16, T17, T18, T19, T20, T21>>> value)
        {
            throw null;
        }
        #endregion

        private static ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLong<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) where TRest : struct =>
            new ValueTuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);

        private static Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> CreateLongRef<T1, T2, T3, T4, T5, T6, T7, TRest>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) =>
            new Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>(item1, item2, item3, item4, item5, item6, item7, rest);
    }
}

namespace System.Runtime.CompilerServices
{
    using System.Collections.Generic;
    [CLSCompliant(false)]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Event)]
    public sealed class TupleElementNamesAttribute : Attribute
    {
        public TupleElementNamesAttribute(string[] transformNames) { throw null; }
        public IList<string> TransformNames { get { throw null; } }
    }
}
