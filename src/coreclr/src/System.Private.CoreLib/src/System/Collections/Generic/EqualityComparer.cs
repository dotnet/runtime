// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Security;

using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    [TypeDependencyAttribute("System.Collections.Generic.ObjectEqualityComparer`1")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        // To minimize generic instantiation overhead of creating the comparer per type, we keep the generic portion of the code as small
        // as possible and define most of the creation logic in a non-generic class.
        public static EqualityComparer<T> Default { [Intrinsic] get; } = (EqualityComparer<T>)ComparerHelpers.CreateDefaultEqualityComparer(typeof(T));

        public abstract bool Equals(T x, T y);
        public abstract int GetHashCode(T obj);

        internal virtual int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }

        internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (Equals(array[i], value)) return i;
            }
            return -1;
        }

        int IEqualityComparer.GetHashCode(object obj)
        {
            if (obj == null) return 0;
            if (obj is T) return GetHashCode((T)obj);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }

        bool IEqualityComparer.Equals(object x, object y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if ((x is T) && (y is T)) return Equals((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return false;
        }
    }

    // The methods in this class look identical to the inherited methods, but the calls
    // to Equal bind to IEquatable<T>.Equals(T) instead of Object.Equals(Object)
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y)
        {
            if (x != null)
            {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (value == null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (value == null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        // Equals method for the comparer itself.
        // If in the future this type is made sealed, change the is check to obj != null && GetType() == obj.GetType().
        public override bool Equals(object obj) =>
            obj is GenericEqualityComparer<T>;

        // If in the future this type is made sealed, change typeof(...) to GetType().
        public override int GetHashCode() =>
            typeof(GenericEqualityComparer<T>).GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue) return x.value.Equals(y.value);
                return false;
            }
            if (y.HasValue) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T? obj) => obj.GetHashCode();

        internal override int IndexOf(T?[] array, T? value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (!value.HasValue)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (!array[i].HasValue) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (!value.HasValue)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (!array[i].HasValue) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i].HasValue && array[i].value.Equals(value.value)) return i;
                }
            }
            return -1;
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
    public sealed class ObjectEqualityComparer<T> : EqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y)
        {
            if (x != null)
            {
                if (y != null) return x.Equals(y);
                return false;
            }
            if (y != null) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj) => obj?.GetHashCode() ?? 0;

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            if (value == null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            if (value == null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] == null) return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (array[i] != null && array[i].Equals(value)) return i;
                }
            }
            return -1;
        }

        // Equals method for the comparer itself.
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    // Performance of IndexOf on byte array is very important for some scenarios.
    // We will call the C runtime function memchr, which is optimized.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed class ByteEqualityComparer : EqualityComparer<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(byte x, byte y)
        {
            return x == y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(byte b)
        {
            return b.GetHashCode();
        }

        internal unsafe override int IndexOf(byte[] array, byte value, int startIndex, int count)
        {
            int found = new ReadOnlySpan<byte>(array, startIndex, count).IndexOf(value);
            return (found >= 0) ? (startIndex + found) : found;
        }

        internal override int LastIndexOf(byte[] array, byte value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            int found = new ReadOnlySpan<byte>(array, endIndex, count).LastIndexOf(value);
            return (found >= 0) ? (endIndex + found) : found;
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
    public sealed class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
    {
        internal EnumEqualityComparer() { }

        // This is used by the serialization engine.
        private EnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // For back-compat we need to serialize the comparers for enums with underlying types other than int as ObjectEqualityComparer 
            if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.Int32) {
                info.SetType(typeof(ObjectEqualityComparer<T>));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y)
        {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(x);
            int y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(y);
            return x_final == y_final;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        // Equals method for the comparer itself.
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int toFind = JitHelpers.UnsafeEnumCast(value);
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                int current = JitHelpers.UnsafeEnumCast(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int toFind = JitHelpers.UnsafeEnumCast(value);
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                int current = JitHelpers.UnsafeEnumCast(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }
    }

    [Serializable]
    internal sealed class LongEnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
    {
        internal LongEnumEqualityComparer() { }

        // This is used by the serialization engine.
        private LongEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // The LongEnumEqualityComparer does not exist on 4.0 so we need to serialize this comparer as ObjectEqualityComparer
            // to allow for roundtrip between 4.0 and 4.5.
            info.SetType(typeof(ObjectEqualityComparer<T>));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T x, T y)
        {
            long x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(x);
            long y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(y);
            return x_final == y_final;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        // Equals method for the comparer itself.
        public override bool Equals(object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        internal override int IndexOf(T[] array, T value, int startIndex, int count)
        {
            long toFind = JitHelpers.UnsafeEnumCastLong(value);
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                long current = JitHelpers.UnsafeEnumCastLong(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }

        internal override int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            long toFind = JitHelpers.UnsafeEnumCastLong(value);
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                long current = JitHelpers.UnsafeEnumCastLong(array[i]);
                if (toFind == current) return i;
            }
            return -1;
        }
    }
}
