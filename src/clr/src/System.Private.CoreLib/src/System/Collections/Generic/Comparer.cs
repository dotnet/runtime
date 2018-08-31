// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    [TypeDependencyAttribute("System.Collections.Generic.ObjectComparer`1")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    public abstract class Comparer<T> : IComparer, IComparer<T>
    {
        // To minimize generic instantiation overhead of creating the comparer per type, we keep the generic portion of the code as small
        // as possible and define most of the creation logic in a non-generic class.
        public static Comparer<T> Default { get; } = (Comparer<T>)ComparerHelpers.CreateDefaultComparer(typeof(T));

        public static Comparer<T> Create(Comparison<T> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException(nameof(comparison));

            return new ComparisonComparer<T>(comparison);
        }

        public abstract int Compare(T x, T y);

        int IComparer.Compare(object x, object y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;
            if (x is T && y is T) return Compare((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }
    }

    // Note: although there is a lot of shared code in the following
    // comparers, we do not incorporate it into a base class for perf
    // reasons. Adding another base class (even one with no fields)
    // means another generic instantiation, which can be costly esp.
    // for value types.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class GenericComparer<T> : Comparer<T> where T : IComparable<T>
    {
        public override int Compare(T x, T y)
        {
            if (x != null)
            {
                if (y != null) return x.CompareTo(y);
                return 1;
            }
            if (y != null) return -1;
            return 0;
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class NullableComparer<T> : Comparer<T?> where T : struct, IComparable<T>
    {
        public override int Compare(T? x, T? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue) return x.value.CompareTo(y.value);
                return 1;
            }
            if (y.HasValue) return -1;
            return 0;
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class ObjectComparer<T> : Comparer<T>
    {
        public override int Compare(T x, T y)
        {
            return System.Collections.Comparer.Default.Compare(x, y);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    internal sealed class ComparisonComparer<T> : Comparer<T>
    {
        private readonly Comparison<T> _comparison;

        public ComparisonComparer(Comparison<T> comparison)
        {
            _comparison = comparison;
        }

        public override int Compare(T x, T y)
        {
            return _comparison(x, y);
        }
    }

    // Enum comparers (specialized to avoid boxing)
    // NOTE: Each of these needs to implement ISerializable
    // and have a SerializationInfo/StreamingContext ctor,
    // since we want to serialize as ObjectComparer for
    // back-compat reasons (see below).
    [Serializable]
    internal sealed class Int32EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public Int32EnumComparer()
        {
            Debug.Assert(typeof(T).IsEnum);
        }

        // Used by the serialization engine.
        private Int32EnumComparer(SerializationInfo info, StreamingContext context) { }

        public override int Compare(T x, T y)
        {
            int ix = JitHelpers.UnsafeEnumCast(x);
            int iy = JitHelpers.UnsafeEnumCast(y);
            return ix.CompareTo(iy);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Previously Comparer<T> was not specialized for enums,
            // and instead fell back to ObjectComparer which uses boxing.
            // Set the type as ObjectComparer here so code that serializes
            // Comparer for enums will not break.
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class UInt32EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public UInt32EnumComparer()
        {
            Debug.Assert(typeof(T).IsEnum);
        }

        // Used by the serialization engine.
        private UInt32EnumComparer(SerializationInfo info, StreamingContext context) { }

        public override int Compare(T x, T y)
        {
            uint ix = (uint)JitHelpers.UnsafeEnumCast(x);
            uint iy = (uint)JitHelpers.UnsafeEnumCast(y);
            return ix.CompareTo(iy);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class Int64EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public Int64EnumComparer()
        {
            Debug.Assert(typeof(T).IsEnum);
        }

        public override int Compare(T x, T y)
        {
            long lx = JitHelpers.UnsafeEnumCastLong(x);
            long ly = JitHelpers.UnsafeEnumCastLong(y);
            return lx.CompareTo(ly);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }

    [Serializable]
    internal sealed class UInt64EnumComparer<T> : Comparer<T>, ISerializable where T : struct
    {
        public UInt64EnumComparer()
        {
            Debug.Assert(typeof(T).IsEnum);
        }

        // Used by the serialization engine.
        private UInt64EnumComparer(SerializationInfo info, StreamingContext context) { }

        public override int Compare(T x, T y)
        {
            ulong lx = (ulong)JitHelpers.UnsafeEnumCastLong(x);
            ulong ly = (ulong)JitHelpers.UnsafeEnumCastLong(y);
            return lx.CompareTo(ly);
        }

        // Equals method for the comparer itself. 
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(ObjectComparer<T>));
        }
    }
}
