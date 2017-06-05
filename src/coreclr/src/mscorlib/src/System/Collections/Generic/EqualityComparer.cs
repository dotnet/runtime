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
using System.Diagnostics.Contracts;

namespace System.Collections.Generic
{
    [Serializable]
    [TypeDependencyAttribute("System.Collections.Generic.ObjectEqualityComparer`1")]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        // To minimize generic instantiation overhead of creating the comparer per type, we keep the generic portion of the code as small
        // as possible and define most of the creation logic in a non-generic class.
        public static EqualityComparer<T> Default { get; } = (EqualityComparer<T>)ComparerHelpers.CreateDefaultEqualityComparer(typeof(T));

        [Pure]
        public abstract bool Equals(T x, T y);
        [Pure]
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
    internal class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>
    {
        [Pure]
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

        [Pure]
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
        public override bool Equals(Object obj) =>
            obj is GenericEqualityComparer<T>;

        // If in the future this type is made sealed, change typeof(...) to GetType().
        public override int GetHashCode() =>
            typeof(GenericEqualityComparer<T>).GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
    {
        [Pure]
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

        [Pure]
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
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class ObjectEqualityComparer<T> : EqualityComparer<T>
    {
        [Pure]
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

        [Pure]
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
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    // NonRandomizedStringEqualityComparer is the comparer used by default with the Dictionary<string,...> 
    // As the randomized string hashing is turned on by default on coreclr, we need to keep the performance not affected 
    // as much as possible in the main stream scenarios like Dictionary<string,>
    // We use NonRandomizedStringEqualityComparer as default comparer as it doesnt use the randomized string hashing which 
    // keep the perofrmance not affected till we hit collision threshold and then we switch to the comparer which is using 
    // randomized string hashing GenericEqualityComparer<string>
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal class NonRandomizedStringEqualityComparer : GenericEqualityComparer<string>
    {
        private static IEqualityComparer<string> s_nonRandomizedComparer;

        internal static new IEqualityComparer<string> Default
        {
            get
            {
                if (s_nonRandomizedComparer == null)
                {
                    s_nonRandomizedComparer = new NonRandomizedStringEqualityComparer();
                }
                return s_nonRandomizedComparer;
            }
        }

        [Pure]
        public override int GetHashCode(string obj)
        {
            if (obj == null) return 0;
            return obj.GetLegacyNonRandomizedHashCode();
        }
    }

    // Performance of IndexOf on byte array is very important for some scenarios.
    // We will call the C runtime function memchr, which is optimized.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class ByteEqualityComparer : EqualityComparer<byte>
    {
        [Pure]
        public override bool Equals(byte x, byte y)
        {
            return x == y;
        }

        [Pure]
        public override int GetHashCode(byte b)
        {
            return b.GetHashCode();
        }

        internal unsafe override int IndexOf(byte[] array, byte value, int startIndex, int count)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (startIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.ArgumentOutOfRange_Index);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_Count);
            if (count > array.Length - startIndex)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            Contract.EndContractBlock();
            if (count == 0) return -1;
            fixed (byte* pbytes = array)
            {
                return Buffer.IndexOfByte(pbytes, value, startIndex, count);
            }
        }

        internal override int LastIndexOf(byte[] array, byte value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (array[i] == value) return i;
            }
            return -1;
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
    {
        [Pure]
        public override bool Equals(T x, T y)
        {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(x);
            int y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(y);
            return x_final == y_final;
        }

        [Pure]
        public override int GetHashCode(T obj)
        {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return x_final.GetHashCode();
        }

        public EnumEqualityComparer() { }

        // This is used by the serialization engine.
        protected EnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
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

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // For back-compat we need to serialize the comparers for enums with underlying types other than int as ObjectEqualityComparer 
            if (Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T))) != TypeCode.Int32) {
                info.SetType(typeof(ObjectEqualityComparer<T>));
            }
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class SByteEnumEqualityComparer<T> : EnumEqualityComparer<T> where T : struct
    {
        public SByteEnumEqualityComparer() { }

        // This is used by the serialization engine.
        public SByteEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [Pure]
        public override int GetHashCode(T obj)
        {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((sbyte)x_final).GetHashCode();
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class ShortEnumEqualityComparer<T> : EnumEqualityComparer<T>, ISerializable where T : struct
    {
        public ShortEnumEqualityComparer() { }

        // This is used by the serialization engine.
        public ShortEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        [Pure]
        public override int GetHashCode(T obj)
        {
            int x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCast(obj);
            return ((short)x_final).GetHashCode();
        }
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    internal sealed class LongEnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct
    {
        [Pure]
        public override bool Equals(T x, T y)
        {
            long x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(x);
            long y_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(y);
            return x_final == y_final;
        }

        [Pure]
        public override int GetHashCode(T obj)
        {
            long x_final = System.Runtime.CompilerServices.JitHelpers.UnsafeEnumCastLong(obj);
            return x_final.GetHashCode();
        }

        // Equals method for the comparer itself.
        public override bool Equals(Object obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();

        public LongEnumEqualityComparer() { }

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

        // This is used by the serialization engine.
        public LongEnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // The LongEnumEqualityComparer does not exist on 4.0 so we need to serialize this comparer as ObjectEqualityComparer
            // to allow for roundtrip between 4.0 and 4.5.
            info.SetType(typeof(ObjectEqualityComparer<T>));
        }
    }
}
