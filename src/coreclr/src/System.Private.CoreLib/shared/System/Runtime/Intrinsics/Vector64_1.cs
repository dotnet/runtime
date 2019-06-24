// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
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
        // This also allows the debug view to work https://github.com/dotnet/coreclr/issues/15694)
        private readonly ulong _00;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector64{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            get
            {
                ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
                return Vector64.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector64{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector64<T> Zero
        {
            get
            {
                ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
                return default;
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
            get
            {
                return (typeof(T) == typeof(byte)) ||
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
        }

        /// <summary>Determines whether the specified <see cref="Vector64{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector64{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public bool Equals(Vector64<T> other)
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

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
        public override bool Equals(object? obj)
        {
            return (obj is Vector64<T>) && Equals((Vector64<T>)(obj));
        }

        /// <summary>Gets the hash code for the instance.</summary>
        /// <returns>The hash code for the instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override int GetHashCode()
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            int hashCode = 0;

            for (int i = 0; i < Count; i++)
            {
                hashCode = HashCode.Combine(hashCode, this.GetElement(i).GetHashCode());
            }

            return hashCode;
        }

        /// <summary>Converts the current instance to an equivalent string representation.</summary>
        /// <returns>An equivalent string representation of the current instance.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override string ToString()
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            int lastElement = Count - 1;
            StringBuilder sb = StringBuilderCache.Acquire();
            CultureInfo invariant = CultureInfo.InvariantCulture;

            sb.Append('<');
            for (int i = 0; i < lastElement; i++)
            {
                sb.Append(((IFormattable)this.GetElement(i)).ToString("G", invariant))
                 .Append(',')
                 .Append(' ');
            }
            sb.Append(((IFormattable)this.GetElement(lastElement)).ToString("G", invariant))
             .Append('>');

            return StringBuilderCache.GetStringAndRelease(sb);
        }
    }
}
