// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace System.Collections.Generic
{
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public abstract partial class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
    {
        // public static EqualityComparer<T> Default is runtime-specific

        /// <summary>
        /// Creates an <see cref="EqualityComparer{T}"/> by using the specified delegates as the implementation of the comparer's
        /// <see cref="EqualityComparer{T}.Equals"/> and <see cref="EqualityComparer{T}.GetHashCode"/> methods.
        /// </summary>
        /// <param name="equals">The delegate to use to implement the <see cref="EqualityComparer{T}.Equals"/> method.</param>
        /// <param name="getHashCode">
        /// The delegate to use to implement the <see cref="EqualityComparer{T}.GetHashCode"/> method.
        /// If no delegate is supplied, calls to the resulting comparer's <see cref="EqualityComparer{T}.GetHashCode"/>
        /// will throw <see cref="NotSupportedException"/>.
        /// </param>
        /// <returns>The new comparer.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="equals"/> delegate was null.</exception>
        public static EqualityComparer<T> Create(Func<T?, T?, bool> equals, Func<T, int>? getHashCode = null)
        {
            ArgumentNullException.ThrowIfNull(equals);

            getHashCode ??= _ => throw new NotSupportedException();

            return new DelegateEqualityComparer<T>(equals, getHashCode);
        }

        public abstract bool Equals(T? x, T? y);
        public abstract int GetHashCode([DisallowNull] T obj);

        int IEqualityComparer.GetHashCode(object? obj)
        {
            if (obj == null) return 0;
            if (obj is T) return GetHashCode((T)obj);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return 0;
        }

        bool IEqualityComparer.Equals(object? x, object? y)
        {
            if (x == y) return true;
            if (x == null || y == null) return false;
            if ((x is T) && (y is T)) return Equals((T)x, (T)y);
            ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
            return false;
        }

#if !NATIVEAOT
        internal virtual int IndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (Equals(array[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count)
        {
            int endIndex = startIndex - count + 1;
            for (int i = startIndex; i >= endIndex; i--)
            {
                if (Equals(array[i], value))
                {
                    return i;
                }
            }
            return -1;
        }
#endif
    }

    internal sealed class DelegateEqualityComparer<T> : EqualityComparer<T>
    {
        private readonly Func<T?, T?, bool> _equals;
        private readonly Func<T, int> _getHashCode;

        public DelegateEqualityComparer(Func<T?, T?, bool> equals, Func<T, int> getHashCode)
        {
            _equals = equals;
            _getHashCode = getHashCode;
        }

        public override bool Equals(T? x, T? y) =>
            _equals(x, y);

        public override int GetHashCode([DisallowNull] T obj) =>
            _getHashCode(obj);

        public override bool Equals(object? obj) =>
            obj is DelegateEqualityComparer<T> other &&
            _equals == other._equals &&
            _getHashCode == other._getHashCode;

        public override int GetHashCode() =>
            HashCode.Combine(_equals.GetHashCode(), _getHashCode.GetHashCode());
    }

    // The methods in this class look identical to the inherited methods, but the calls
    // to Equal bind to IEquatable<T>.Equals(T) instead of Object.Equals(Object)
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class GenericEqualityComparer<T> : EqualityComparer<T> where T : IEquatable<T>?
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
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
        public override int GetHashCode([DisallowNull] T obj) =>
            obj?.GetHashCode() ?? 0;

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class NullableEqualityComparer<T> : EqualityComparer<T?>, ISerializable where T : struct
    {
        public NullableEqualityComparer() { }
        private NullableEqualityComparer(SerializationInfo info, StreamingContext context) { }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (!typeof(T).IsAssignableTo(typeof(IEquatable<T>)))
            {
                // We used to use NullableComparer only for types implementing IEquatable<T>
                info.SetType(typeof(ObjectEqualityComparer<T?>));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
        {
            if (x.HasValue)
            {
                if (y.HasValue) return EqualityComparer<T>.Default.Equals(x.value, y.value);
                return false;
            }
            if (y.HasValue) return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T? obj) =>
            obj.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class ObjectEqualityComparer<T> : EqualityComparer<T>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(T? x, T? y)
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
        public override int GetHashCode([DisallowNull] T obj) =>
            obj?.GetHashCode() ?? 0;

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class ByteEqualityComparer : EqualityComparer<byte>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(byte x, byte y) =>
            x == y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(byte b) =>
            b.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // Needs to be public to support binary serialization compatibility
    public sealed partial class EnumEqualityComparer<T> : EqualityComparer<T>, ISerializable where T : struct, Enum
    {
        public EnumEqualityComparer() { }

        // This is used by the serialization engine.
        private EnumEqualityComparer(SerializationInfo information, StreamingContext context) { }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // For back-compat we need to serialize the comparers for enums with underlying types other than int as ObjectEqualityComparer
            if (Type.GetTypeCode(typeof(T)) != TypeCode.Int32)
            {
                info.SetType(typeof(ObjectEqualityComparer<T>));
            }
        }

        // public override bool Equals(T x, T y) is runtime-specific

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode(T obj) =>
            obj.GetHashCode();

        // Equals method for the comparer itself.
        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj != null && GetType() == obj.GetType();

        public override int GetHashCode() =>
            GetType().GetHashCode();
    }

    // This class exists to be EqualityComparer<string>.Default. It can't just use the GenericEqualityComparer<string>,
    // as it needs to also implement IAlternateEqualityComparer<ReadOnlySpan<char>, string>, and it can't be
    // StringComparer.Ordinal, as that doesn't derive from the required abstract EqualityComparer<T> base class.
    [Serializable]
    internal sealed partial class StringEqualityComparer :
        EqualityComparer<string>,
        IAlternateEqualityComparer<ReadOnlySpan<char>, string>,
        ISerializable
    {
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // This type is added as an internal implementation detail in .NET 9. Even though as of .NET 9 BinaryFormatter has been
            // deprecated, for back compat we still need to support serializing this type, especially when EqualityComparer<string>.Default
            // is used as part of a collection, like Dictionary<string, TValue>.
            //
            // BinaryFormatter treats types in the core library as being special, in that it doesn't include the assembly as part of the
            // serialized data, and then on deserialization it assumes the type is in mscorlib. We could make the type public and type forward
            // it from the mscorlib shim, which would enable roundtripping on .NET 9+, but because this type doesn't exist downlevel, it would
            // break serializing on .NET 9+ and deserializing downlevel. Therefore, we need to serialize as something that exists downlevel.

            // We could serialize as OrdinalComparer, which does exist downlevel, and which has the nice property that it also implements
            // IAlternateEqualityComparer<ReadOnlySpan<char>, string>, which means serializing an instance on .NET 9+ and deserializing it
            // on .NET 9+ would continue to support span-based lookups. However, OrdinalComparer is not an EqualityComparer<string>, which
            // means the type's public ancestry would not be retained, which could lead to strange casting-related errors, including downlevel.

            // Instead, we can serialize as a GenericEqualityComparer<string>. This exists downlevel and also derives from EqualityComparer<string>,
            // but doesn't implement IAlternateEqualityComparer<ReadOnlySpan<char>, string>. This means that upon deserializing on .NET 9+,
            // the comparer loses its ability to handle span-based lookups. As BinaryFormatter is deprecated on .NET 9+, this is a readonable tradeoff.

            info.SetType(typeof(GenericEqualityComparer<string>));
        }

        public override bool Equals(string? x, string? y) => string.Equals(x, y);

        public override int GetHashCode([DisallowNull] string obj) => obj?.GetHashCode() ?? 0;

        public bool Equals(ReadOnlySpan<char> span, string target)
        {
            // See explanation in OrdinalComparer.Equals.
            if (span.IsEmpty && target is null)
            {
                return false;
            }

            return span.SequenceEqual(target);
        }

        public int GetHashCode(ReadOnlySpan<char> span) => string.GetHashCode(span);

        public string Create(ReadOnlySpan<char> span) => span.ToString();

        public override bool Equals(object? obj) => obj is StringEqualityComparer;
        public override int GetHashCode() => GetType().GetHashCode();
    }
}
