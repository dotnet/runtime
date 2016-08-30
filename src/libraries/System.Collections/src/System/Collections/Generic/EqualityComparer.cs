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
    [Serializable]
    public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        protected EqualityComparer()
        {
        }

        // .NET Native for UWP toolchain overwrites the Default property with optimized 
        // instantiation-specific implementation. It depends on subtle implementation details of this
        // class to do so. Once the packaging infrastructure allows it, the implementation 
        // of EqualityComparer<T> should be moved to CoreRT repo to avoid the fragile dependency.
        // Until that happens, nothing in this class can change.

        // TODO: Change the _default field to non-volatile and initialize it via implicit static 
        // constructor for better performance (https://github.com/dotnet/coreclr/pull/4340).

        public static EqualityComparer<T> Default
        {
            get
            {
                if (_default == null)
                {
                    object comparer;

                    // NUTC compiler is able to static evaluate the conditions and only put the necessary branches in finally binary code, 
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

        // WARNING: We allow diagnostic tools to directly inspect this member (_default). 
        // See https://github.com/dotnet/corert/blob/master/Documentation/design-docs/diagnostics/diagnostics-tools-contract.md for more details. 
        // Please do not change the type, the name, or the semantic usage of this member without understanding the implication for tools. 
        // Get in touch with the diagnostics team if you have questions.
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
    // ProjectN compatibility notes:
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
    [Serializable]
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

    [Serializable]
    internal sealed class EqualityComparerForSByte : EqualityComparer<sbyte>
    {
        public override bool Equals(sbyte x, sbyte y)
        {
            return x == y;
        }

        public override int GetHashCode(sbyte x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForByte : EqualityComparer<byte>
    {
        public override bool Equals(byte x, byte y)
        {
            return x == y;
        }

        public override int GetHashCode(byte x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForInt16 : EqualityComparer<short>
    {
        public override bool Equals(short x, short y)
        {
            return x == y;
        }

        public override int GetHashCode(short x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForUInt16 : EqualityComparer<ushort>
    {
        public override bool Equals(ushort x, ushort y)
        {
            return x == y;
        }

        public override int GetHashCode(ushort x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForInt32 : EqualityComparer<int>
    {
        public override bool Equals(int x, int y)
        {
            return x == y;
        }

        public override int GetHashCode(int x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForUInt32 : EqualityComparer<uint>
    {
        public override bool Equals(uint x, uint y)
        {
            return x == y;
        }

        public override int GetHashCode(uint x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForInt64 : EqualityComparer<long>
    {
        public override bool Equals(long x, long y)
        {
            return x == y;
        }

        public override int GetHashCode(long x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForUInt64 : EqualityComparer<ulong>
    {
        public override bool Equals(ulong x, ulong y)
        {
            return x == y;
        }

        public override int GetHashCode(ulong x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
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

    [Serializable]
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

    [Serializable]
    internal sealed class EqualityComparerForSingle : EqualityComparer<float>
    {
        public override bool Equals(float x, float y)
        {
            // == has the wrong semantic for NaN for Single
            return x.Equals(y);
        }

        public override int GetHashCode(float x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForDouble : EqualityComparer<double>
    {
        public override bool Equals(double x, double y)
        {
            // == has the wrong semantic for NaN for Double
            return x.Equals(y);
        }

        public override int GetHashCode(double x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForDecimal : EqualityComparer<decimal>
    {
        public override bool Equals(decimal x, decimal y)
        {
            return x == y;
        }

        public override int GetHashCode(decimal x)
        {
            return x.GetHashCode();
        }
    }

    [Serializable]
    internal sealed class EqualityComparerForString : EqualityComparer<string>
    {
        public override bool Equals(string x, string y)
        {
            return x == y;
        }

        public override int GetHashCode(string x)
        {
            if (x == null)
                return 0;
            return x.GetHashCode();
        }
    }
}

