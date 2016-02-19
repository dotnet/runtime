// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Class: EqualityComparer<T> 
**
===========================================================*/

using System;
using System.Collections;

namespace System.Collections.Generic
{
    public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        protected EqualityComparer()
        {
        }

        public static EqualityComparer<T> Default
        {
            get
            {
                if (_default == null)
                {
                    object comparer;

                    // NUTC compiler is able to static evalulate the conditions and only put the necessary branches in finally binary code, 
                    // even casting to EqualityComparer<T> can be removed.

                    // For example: for Byte, the code generated is
                    //     if (_default == null) _default = new EqualityComparerForByte(); return _default;

                    // For classes, due to generic sharing, the code generated is:
                    //     if (_default == null) { if (handle == typeof(string).RuntimeTypeHandle) comparer = new EqualityComparerForString(); else comparer = new LastResortEqalityComparer<T>; ...

                    if (typeof(T) == typeof(SByte))
                        comparer = new EqualityComparerForSByte();
                    else if (typeof(T) == typeof(Byte))
                        comparer = new EqualityComparerForByte();
                    else if (typeof(T) == typeof(Int16))
                        comparer = new EqualityComparerForInt16();
                    else if (typeof(T) == typeof(UInt16))
                        comparer = new EqualityComparerForUInt16();
                    else if (typeof(T) == typeof(Int32))
                        comparer = new EqualityComparerForInt32();
                    else if (typeof(T) == typeof(UInt32))
                        comparer = new EqualityComparerForUInt32();
                    else if (typeof(T) == typeof(Int64))
                        comparer = new EqualityComparerForInt64();
                    else if (typeof(T) == typeof(UInt64))
                        comparer = new EqualityComparerForUInt64();
                    else if (typeof(T) == typeof(IntPtr))
                        comparer = new EqualityComparerForIntPtr();
                    else if (typeof(T) == typeof(UIntPtr))
                        comparer = new EqualityComparerForUIntPtr();
                    else if (typeof(T) == typeof(Single))
                        comparer = new EqualityComparerForSingle();
                    else if (typeof(T) == typeof(Double))
                        comparer = new EqualityComparerForDouble();
                    else if (typeof(T) == typeof(Decimal))
                        comparer = new EqualityComparerForDecimal();
                    else if (typeof(T) == typeof(String))
                        comparer = new EqualityComparerForString();
                    else
                        comparer = new LastResortEqualityComparer<T>();

                    _default = (EqualityComparer<T>)comparer;
                }

                return _default;
            }
        }

        private static volatile EqualityComparer<T> _default;

        public abstract bool Equals(T x, T y);

        public abstract int GetHashCode(T obj);

        int IEqualityComparer.GetHashCode(object obj)
        {
            if (obj == null)
                return 0;
            if (obj is T)
                return GetHashCode((T)obj);
            throw new ArgumentException(SR.Argument_InvalidArgumentForComparison, nameof(obj));
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            if (x == y)
                return true;
            if (x == null || y == null)
                return false;
            if ((x is T) && (y is T))
                return Equals((T)x, (T)y);
            throw new ArgumentException(SR.Argument_InvalidArgumentForComparison);
        }
    }

    //
    // ProjectN compatiblity notes:
    //
    //    Unlike the full desktop, we make no attempt to use the IEquatable<T> interface on T. Because we can't generate
    //    code at runtime, we derive no performance benefit from using the type-specific Equals(). We can't even
    //    perform the check for IEquatable<> at the time the type-specific constructor is created (due to the removable of Type.IsAssignableFrom).
    //    We would thus be incurring an interface cast check on each call to Equals() for no performance gain.
    //
    //    This should not cause a compat problem unless some type implements an IEquatable.Equals() that is semantically
    //    incompatible with Object.Equals(). That goes specifically against the documented guidelines (and would in any case,
    //    break any hashcode-dependent collection.) 
    //
    internal sealed class LastResortEqualityComparer<T> : EqualityComparer<T>
    {
        public LastResortEqualityComparer()
        {
        }

        public sealed override bool Equals(T x, T y)
        {
            if (x == null)
                return y == null;
            if (y == null)
                return false;

            return x.Equals(y);
        }

        public sealed override int GetHashCode(T obj)
        {
            if (obj == null)
                return 0;
            return obj.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForSByte : EqualityComparer<SByte>
    {
        public override bool Equals(SByte x, SByte y)
        {
            return x == y;
        }

        public override int GetHashCode(SByte x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForByte : EqualityComparer<Byte>
    {
        public override bool Equals(Byte x, Byte y)
        {
            return x == y;
        }

        public override int GetHashCode(Byte x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForInt16 : EqualityComparer<Int16>
    {
        public override bool Equals(Int16 x, Int16 y)
        {
            return x == y;
        }

        public override int GetHashCode(Int16 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForUInt16 : EqualityComparer<UInt16>
    {
        public override bool Equals(UInt16 x, UInt16 y)
        {
            return x == y;
        }

        public override int GetHashCode(UInt16 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForInt32 : EqualityComparer<Int32>
    {
        public override bool Equals(Int32 x, Int32 y)
        {
            return x == y;
        }

        public override int GetHashCode(Int32 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForUInt32 : EqualityComparer<UInt32>
    {
        public override bool Equals(UInt32 x, UInt32 y)
        {
            return x == y;
        }

        public override int GetHashCode(UInt32 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForInt64 : EqualityComparer<Int64>
    {
        public override bool Equals(Int64 x, Int64 y)
        {
            return x == y;
        }

        public override int GetHashCode(Int64 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForUInt64 : EqualityComparer<UInt64>
    {
        public override bool Equals(UInt64 x, UInt64 y)
        {
            return x == y;
        }

        public override int GetHashCode(UInt64 x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForIntPtr : EqualityComparer<IntPtr>
    {
        public override bool Equals(IntPtr x, IntPtr y)
        {
            return x == y;
        }

        public override int GetHashCode(IntPtr x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForUIntPtr : EqualityComparer<UIntPtr>
    {
        public override bool Equals(UIntPtr x, UIntPtr y)
        {
            return x == y;
        }

        public override int GetHashCode(UIntPtr x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForSingle : EqualityComparer<Single>
    {
        public override bool Equals(Single x, Single y)
        {
            // == has the wrong semantic for NaN for Single
            return x.Equals(y);
        }

        public override int GetHashCode(Single x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForDouble : EqualityComparer<Double>
    {
        public override bool Equals(Double x, Double y)
        {
            // == has the wrong semantic for NaN for Double
            return x.Equals(y);
        }

        public override int GetHashCode(Double x)
        {
            return x.GetHashCode();
        }
    }


    internal sealed class EqualityComparerForDecimal : EqualityComparer<Decimal>
    {
        public override bool Equals(Decimal x, Decimal y)
        {
            return x == y;
        }

        public override int GetHashCode(Decimal x)
        {
            return x.GetHashCode();
        }
    }

    internal sealed class EqualityComparerForString : EqualityComparer<String>
    {
        public override bool Equals(String x, String y)
        {
            return x == y;
        }

        public override int GetHashCode(String x)
        {
            if (x == null)
                return 0;
            return x.GetHashCode();
        }
    }
}

