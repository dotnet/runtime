// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector64DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector64.Size)]
    public readonly struct Vector64<T> : IEquatable<Vector64<T>>
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/runtime/issues/9495)
        private readonly ulong _00;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector64{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return Vector64.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return default;
            }
        }


        /// <summary>Gets a new <see cref="Vector64{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> AllBitsSet
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return Vector64.Create(0xFFFFFFFF).As<uint, T>();
            }
        }


        internal unsafe string DisplayString
        {
            get
            {
                if (IsSupported)
                {
                    return ToString();
                }
                else
                {
                    return SR.NotSupported_Type;
                }
            }
        }

        internal static bool IsSupported
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (typeof(T) == typeof(byte)) ||
                   (typeof(T) == typeof(sbyte)) ||
                   (typeof(T) == typeof(short)) ||
                   (typeof(T) == typeof(ushort)) ||
                   (typeof(T) == typeof(int)) ||
                   (typeof(T) == typeof(uint)) ||
                   (typeof(T) == typeof(long)) ||
                   (typeof(T) == typeof(ulong)) ||
                   (typeof(T) == typeof(float)) ||
                   (typeof(T) == typeof(double));
        }

        /// <summary>Determines whether the specified <see cref="Vector64{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector64{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public bool Equals(Vector64<T> other)
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();

            for (int i = 0; i < Count; i++)
            {
                if (!((IEquatable<T>)(this.GetElement(i))).Equals(other.GetElement(i)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector64{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Vector64<T>) && Equals((Vector64<T>)(obj));
        }

        /// <summary>Gets the hash code for the instance.</summary>
        /// <returns>The hash code for the instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override int GetHashCode()
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();

            HashCode hashCode = default;

            for (int i = 0; i < Count; i++)
            {
                hashCode.Add(this.GetElement(i).GetHashCode());
            }

            return hashCode.ToHashCode();
        }

        /// <summary>Converts the current instance to an equivalent string representation.</summary>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override string ToString()
        {
            ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();

            int lastElement = Count - 1;
            var sb = new ValueStringBuilder(stackalloc char[64]);
            CultureInfo invariant = CultureInfo.InvariantCulture;

            sb.Append('<');
            for (int i = 0; i < lastElement; i++)
            {
                sb.Append(((IFormattable)this.GetElement(i)).ToString("G", invariant));
                sb.Append(',');
                sb.Append(' ');
            }
            sb.Append(((IFormattable)this.GetElement(lastElement)).ToString("G", invariant));
            sb.Append('>');

            return sb.ToString();
        }
    }
}
