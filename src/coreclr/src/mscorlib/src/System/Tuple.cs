// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace System {

    /// <summary>
    /// Helper so we can call some tuple methods recursively without knowing the underlying types.
    /// </summary>
    internal interface ITuple {
        string ToString(StringBuilder sb);
        int GetHashCode(IEqualityComparer comparer);
        int Size { get; }

    }

    public static class Tuple {
        public static Tuple<T1> Create<T1>(T1 item1) {
            return new Tuple<T1>(item1);
        }

        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2) {
            return new Tuple<T1, T2>(item1, item2);
        }

        public static Tuple<T1, T2, T3> Create<T1, T2, T3>(T1 item1, T2 item2, T3 item3) {
            return new Tuple<T1, T2, T3>(item1, item2, item3);
        }

        public static Tuple<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) {
            return new Tuple<T1, T2, T3, T4>(item1, item2, item3, item4);
        }

        public static Tuple<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
            return new Tuple<T1, T2, T3, T4, T5>(item1, item2, item3, item4, item5);
        }

        public static Tuple<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
            return new Tuple<T1, T2, T3, T4, T5, T6>(item1, item2, item3, item4, item5, item6);
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7>(item1, item2, item3, item4, item5, item6, item7);
        }

        public static Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>> Create<T1, T2, T3, T4, T5, T6, T7, T8>(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, T8 item8) {
            return new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>(item1, item2, item3, item4, item5, item6, item7, new Tuple<T8>(item8));
        }

        internal static int CombineHashCodes(int h1, int h2)
        {
            // SRP: Keep the actual hashing logic in a separate class
            // Note if that class is updated, the corresponding file in corefx
            // should be as well
            return System.Numerics.Hashing.HashHelpers.Combine(h1, h2);
        }
        
        // These overloads mirror the ones in corefx/ValueTuple.
        // We combine the hashes sequentially instead of in chunks,
        // which results in simpler logic and a better spreading effect.
        
        internal static int CombineHashCodes(int h1, int h2, int h3)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2), h3);
        }
        
        internal static int CombineHashCodes(int h1, int h2, int h3, int h4)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3), h4);
        }
        
        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4), h5);
        }
        
        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4, h5), h6);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4, h5, h6), h7);
        }

        internal static int CombineHashCodes(int h1, int h2, int h3, int h4, int h5, int h6, int h7, int h8)
        {
            return CombineHashCodes(CombineHashCodes(h1, h2, h3, h4, h5, h6, h7), h8);
        }
    }

    [Serializable]
    public class Tuple<T1> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;

        public T1 Item1 { get { return m_Item1; } }

        public Tuple(T1 item1) {
            m_Item1 = item1;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1> objTuple = other as Tuple<T1>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1> objTuple = other as Tuple<T1>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            return comparer.Compare(m_Item1, objTuple.m_Item1);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T1>.Default.GetHashCode(Item1);
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return comparer.GetHashCode(m_Item1);
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 1;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }

        public Tuple(T1 item1, T2 item2) {
            m_Item1 = item1;
            m_Item2 = item2;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2> objTuple = other as Tuple<T1, T2>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2> objTuple = other as Tuple<T1, T2>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            return comparer.Compare(m_Item2, objTuple.m_Item2);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 2;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }

        public Tuple(T1 item1, T2 item2, T3 item3) {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3> objTuple = other as Tuple<T1, T2, T3>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3> objTuple = other as Tuple<T1, T2, T3>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            return comparer.Compare(m_Item3, objTuple.m_Item3);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2),
                                          EqualityComparer<T3>.Default.GetHashCode(Item3));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 3;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3, T4> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }
        public T4 Item4 { get { return m_Item4; } }

        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4) {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3, T4>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3)
                && EqualityComparer<T4>.Default.Equals(Item4, other.Item4);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3, T4> objTuple = other as Tuple<T1, T2, T3, T4>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3) && comparer.Equals(m_Item4, objTuple.m_Item4);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3, T4> objTuple = other as Tuple<T1, T2, T3, T4>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            c = comparer.Compare(m_Item3, objTuple.m_Item3);

            if (c != 0) return c;

            return comparer.Compare(m_Item4, objTuple.m_Item4);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2),
                                          EqualityComparer<T3>.Default.GetHashCode(Item3),
                                          EqualityComparer<T4>.Default.GetHashCode(Item4));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(", ");
            sb.Append(m_Item4);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 4;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3, T4, T5> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }
        public T4 Item4 { get { return m_Item4; } }
        public T5 Item5 { get { return m_Item5; } }

        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5) {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3, T4, T5>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3)
                && EqualityComparer<T4>.Default.Equals(Item4, other.Item4)
                && EqualityComparer<T5>.Default.Equals(Item5, other.Item5);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3, T4, T5> objTuple = other as Tuple<T1, T2, T3, T4, T5>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3) && comparer.Equals(m_Item4, objTuple.m_Item4) && comparer.Equals(m_Item5, objTuple.m_Item5);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3, T4, T5> objTuple = other as Tuple<T1, T2, T3, T4, T5>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            c = comparer.Compare(m_Item3, objTuple.m_Item3);

            if (c != 0) return c;

            c = comparer.Compare(m_Item4, objTuple.m_Item4);

            if (c != 0) return c;

            return comparer.Compare(m_Item5, objTuple.m_Item5);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2),
                                          EqualityComparer<T3>.Default.GetHashCode(Item3),
                                          EqualityComparer<T4>.Default.GetHashCode(Item4),
                                          EqualityComparer<T5>.Default.GetHashCode(Item5));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(", ");
            sb.Append(m_Item4);
            sb.Append(", ");
            sb.Append(m_Item5);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 5;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3, T4, T5, T6> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }
        public T4 Item4 { get { return m_Item4; } }
        public T5 Item5 { get { return m_Item5; } }
        public T6 Item6 { get { return m_Item6; } }

        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6) {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3, T4, T5, T6>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3)
                && EqualityComparer<T4>.Default.Equals(Item4, other.Item4)
                && EqualityComparer<T5>.Default.Equals(Item5, other.Item5)
                && EqualityComparer<T6>.Default.Equals(Item6, other.Item6);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3, T4, T5, T6> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3) && comparer.Equals(m_Item4, objTuple.m_Item4) && comparer.Equals(m_Item5, objTuple.m_Item5) && comparer.Equals(m_Item6, objTuple.m_Item6);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3, T4, T5, T6> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            c = comparer.Compare(m_Item3, objTuple.m_Item3);

            if (c != 0) return c;

            c = comparer.Compare(m_Item4, objTuple.m_Item4);

            if (c != 0) return c;

            c = comparer.Compare(m_Item5, objTuple.m_Item5);

            if (c != 0) return c;

            return comparer.Compare(m_Item6, objTuple.m_Item6);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2),
                                          EqualityComparer<T3>.Default.GetHashCode(Item3),
                                          EqualityComparer<T4>.Default.GetHashCode(Item4),
                                          EqualityComparer<T5>.Default.GetHashCode(Item5),
                                          EqualityComparer<T6>.Default.GetHashCode(Item6));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(", ");
            sb.Append(m_Item4);
            sb.Append(", ");
            sb.Append(m_Item5);
            sb.Append(", ");
            sb.Append(m_Item6);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 6;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3, T4, T5, T6, T7> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }
        public T4 Item4 { get { return m_Item4; } }
        public T5 Item5 { get { return m_Item5; } }
        public T6 Item6 { get { return m_Item6; } }
        public T7 Item7 { get { return m_Item7; } }

        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7) {
            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
        }
        
        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3, T4, T5, T6, T7>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3)
                && EqualityComparer<T4>.Default.Equals(Item4, other.Item4)
                && EqualityComparer<T5>.Default.Equals(Item5, other.Item5)
                && EqualityComparer<T6>.Default.Equals(Item6, other.Item6)
                && EqualityComparer<T7>.Default.Equals(Item7, other.Item7);
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3, T4, T5, T6, T7> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3) && comparer.Equals(m_Item4, objTuple.m_Item4) && comparer.Equals(m_Item5, objTuple.m_Item5) && comparer.Equals(m_Item6, objTuple.m_Item6) && comparer.Equals(m_Item7, objTuple.m_Item7);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3, T4, T5, T6, T7> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            c = comparer.Compare(m_Item3, objTuple.m_Item3);

            if (c != 0) return c;

            c = comparer.Compare(m_Item4, objTuple.m_Item4);

            if (c != 0) return c;

            c = comparer.Compare(m_Item5, objTuple.m_Item5);

            if (c != 0) return c;

            c = comparer.Compare(m_Item6, objTuple.m_Item6);

            if (c != 0) return c;

            return comparer.Compare(m_Item7, objTuple.m_Item7);
        }

        public override int GetHashCode()
        {
            return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                          EqualityComparer<T2>.Default.GetHashCode(Item2),
                                          EqualityComparer<T3>.Default.GetHashCode(Item3),
                                          EqualityComparer<T4>.Default.GetHashCode(Item4),
                                          EqualityComparer<T5>.Default.GetHashCode(Item5),
                                          EqualityComparer<T6>.Default.GetHashCode(Item6),
                                          EqualityComparer<T7>.Default.GetHashCode(Item7));
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7));
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(", ");
            sb.Append(m_Item4);
            sb.Append(", ");
            sb.Append(m_Item5);
            sb.Append(", ");
            sb.Append(m_Item6);
            sb.Append(", ");
            sb.Append(m_Item7);
            sb.Append(")");
            return sb.ToString();
        }

        int ITuple.Size {
            get {
                return 7;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> : IStructuralEquatable, IStructuralComparable, IComparable, ITuple {

        private readonly T1 m_Item1;
        private readonly T2 m_Item2;
        private readonly T3 m_Item3;
        private readonly T4 m_Item4;
        private readonly T5 m_Item5;
        private readonly T6 m_Item6;
        private readonly T7 m_Item7;
        private readonly TRest m_Rest;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }
        public T3 Item3 { get { return m_Item3; } }
        public T4 Item4 { get { return m_Item4; } }
        public T5 Item5 { get { return m_Item5; } }
        public T6 Item6 { get { return m_Item6; } }
        public T7 Item7 { get { return m_Item7; } }
        public TRest Rest { get { return m_Rest; } }

        public Tuple(T1 item1, T2 item2, T3 item3, T4 item4, T5 item5, T6 item6, T7 item7, TRest rest) {
            if (!(rest is ITuple)) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleLastArgumentNotATuple"));
            }

            m_Item1 = item1;
            m_Item2 = item2;
            m_Item3 = item3;
            m_Item4 = item4;
            m_Item5 = item5;
            m_Item6 = item6;
            m_Item7 = item7;
            m_Rest = rest;
        }

        public override bool Equals(object obj)
        {
            var other = obj as Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>;
            
            if (other == null)
            {
                return false;
            }
            
            return EqualityComparer<T1>.Default.Equals(Item1, other.Item1)
                && EqualityComparer<T2>.Default.Equals(Item2, other.Item2)
                && EqualityComparer<T3>.Default.Equals(Item3, other.Item3)
                && EqualityComparer<T4>.Default.Equals(Item4, other.Item4)
                && EqualityComparer<T5>.Default.Equals(Item5, other.Item5)
                && EqualityComparer<T6>.Default.Equals(Item6, other.Item6)
                && EqualityComparer<T7>.Default.Equals(Item7, other.Item7)
                && EqualityComparer<TRest>.Default.Equals(Rest, other.Rest); // object.Equals(Rest, other.Rest) is not used here, since this
                                                                             // may be faster if 1) Tuple eventually implements IEquatable or
                                                                             // 2) calls to EqualityComparer.Default.Equals are intrinsified
                                                                             // by the JIT.
        }

        Boolean IStructuralEquatable.Equals(Object other, IEqualityComparer comparer) {
            if (other == null) return false;

            Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>;

            if (objTuple == null) {
                return false;
            }

            return comparer.Equals(m_Item1, objTuple.m_Item1) && comparer.Equals(m_Item2, objTuple.m_Item2) && comparer.Equals(m_Item3, objTuple.m_Item3) && comparer.Equals(m_Item4, objTuple.m_Item4) && comparer.Equals(m_Item5, objTuple.m_Item5) && comparer.Equals(m_Item6, objTuple.m_Item6) && comparer.Equals(m_Item7, objTuple.m_Item7) && comparer.Equals(m_Rest, objTuple.m_Rest);
        }

        Int32 IComparable.CompareTo(Object obj) {
            return ((IStructuralComparable) this).CompareTo(obj, Comparer<Object>.Default);
        }

        Int32 IStructuralComparable.CompareTo(Object other, IComparer comparer) {
            if (other == null) return 1;

            Tuple<T1, T2, T3, T4, T5, T6, T7, TRest> objTuple = other as Tuple<T1, T2, T3, T4, T5, T6, T7, TRest>;

            if (objTuple == null) {
                throw new ArgumentException(Environment.GetResourceString("ArgumentException_TupleIncorrectType", this.GetType().ToString()), "other");
            }

            int c = 0;

            c = comparer.Compare(m_Item1, objTuple.m_Item1);

            if (c != 0) return c;

            c = comparer.Compare(m_Item2, objTuple.m_Item2);

            if (c != 0) return c;

            c = comparer.Compare(m_Item3, objTuple.m_Item3);

            if (c != 0) return c;

            c = comparer.Compare(m_Item4, objTuple.m_Item4);

            if (c != 0) return c;

            c = comparer.Compare(m_Item5, objTuple.m_Item5);

            if (c != 0) return c;

            c = comparer.Compare(m_Item6, objTuple.m_Item6);

            if (c != 0) return c;

            c = comparer.Compare(m_Item7, objTuple.m_Item7);

            if (c != 0) return c;

            return comparer.Compare(m_Rest, objTuple.m_Rest);
        }

        public override int GetHashCode()
        {
            // We want to have a limited hash in this case. We'll use the last 8 elements of the tuple

            var rest = (ITuple)Rest; // We checked that Rest was an ITuple in the constructor
            int size = rest.Size;
            
            if (size >= 8)
            {
                return rest.GetHashCode();
            }
            
            // In this case, the Rest member has less than 8 elements so we need to combine some of our elements with the ones in Rest
            int before = 8 - size; // Number of elements we will hash in this tuple before Rest
            switch (before)
            {
                case 1:
                    return Tuple.CombineHashCodes(EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 2:
                    return Tuple.CombineHashCodes(EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 3:
                    return Tuple.CombineHashCodes(EqualityComparer<T5>.Default.GetHashCode(Item5),
                                                  EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 4:
                    return Tuple.CombineHashCodes(EqualityComparer<T4>.Default.GetHashCode(Item4),
                                                  EqualityComparer<T5>.Default.GetHashCode(Item5),
                                                  EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 5:
                    return Tuple.CombineHashCodes(EqualityComparer<T3>.Default.GetHashCode(Item3),
                                                  EqualityComparer<T4>.Default.GetHashCode(Item4),
                                                  EqualityComparer<T5>.Default.GetHashCode(Item5),
                                                  EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 6:
                    return Tuple.CombineHashCodes(EqualityComparer<T2>.Default.GetHashCode(Item2),
                                                  EqualityComparer<T3>.Default.GetHashCode(Item3),
                                                  EqualityComparer<T4>.Default.GetHashCode(Item4),
                                                  EqualityComparer<T5>.Default.GetHashCode(Item5),
                                                  EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
                case 7:
                    return Tuple.CombineHashCodes(EqualityComparer<T1>.Default.GetHashCode(Item1),
                                                  EqualityComparer<T2>.Default.GetHashCode(Item2),
                                                  EqualityComparer<T3>.Default.GetHashCode(Item3),
                                                  EqualityComparer<T4>.Default.GetHashCode(Item4),
                                                  EqualityComparer<T5>.Default.GetHashCode(Item5),
                                                  EqualityComparer<T6>.Default.GetHashCode(Item6),
                                                  EqualityComparer<T7>.Default.GetHashCode(Item7),
                                                  rest.GetHashCode());
            }
            
            Contract.Assert(false, "Missed all cases for computing Tuple hash code");
            return -1;
        }

        Int32 IStructuralEquatable.GetHashCode(IEqualityComparer comparer) {
            // We want to have a limited hash in this case.  We'll use the last 8 elements of the tuple
            ITuple t = (ITuple) m_Rest;
            int size = t.Size; // cache the size to avoid an unncessary interface call
            if (size >= 8) { return t.GetHashCode(comparer); }
            
            // In this case, the rest memeber has less than 8 elements so we need to combine some our elements with the elements in rest
            int k = 8 - size;
            switch(k) {
                case 1:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 2:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 3:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 4:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 5:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 6:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
                case 7:
                return Tuple.CombineHashCodes(comparer.GetHashCode(m_Item1), comparer.GetHashCode(m_Item2), comparer.GetHashCode(m_Item3), comparer.GetHashCode(m_Item4), comparer.GetHashCode(m_Item5), comparer.GetHashCode(m_Item6), comparer.GetHashCode(m_Item7), t.GetHashCode(comparer));
            }
            Contract.Assert(false, "Missed all cases for computing Tuple hash code");
            return -1;
        }

        Int32 ITuple.GetHashCode(IEqualityComparer comparer) {
            return ((IStructuralEquatable) this).GetHashCode(comparer);
        }
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            return ((ITuple)this).ToString(sb);
        }

        string ITuple.ToString(StringBuilder sb) {
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(", ");
            sb.Append(m_Item3);
            sb.Append(", ");
            sb.Append(m_Item4);
            sb.Append(", ");
            sb.Append(m_Item5);
            sb.Append(", ");
            sb.Append(m_Item6);
            sb.Append(", ");
            sb.Append(m_Item7);
            sb.Append(", ");
            return ((ITuple)m_Rest).ToString(sb);
        }

        int ITuple.Size {
            get {
                return 7 + ((ITuple)m_Rest).Size;
            }
        }
    }
}
