// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics
{
    // We mark certain methods with AggressiveInlining to ensure that the JIT will
    // inline them. The JIT would otherwise not inline the method since it, at the
    // point it tries to determine inline profability, currently cannot determine
    // that most of the code-paths will be optimized away as "dead code".
    //
    // We then manually inline cases (such as certain intrinsic code-paths) that
    // will generate code small enough to make the AgressiveInlining profitable. The
    // other cases (such as the software fallback) are placed in their own method.
    // This ensures we get good codegen for the "fast-path" and allows the JIT to
    // determine inline profitability of the other paths as it would normally.

    [Intrinsic]
    [DebuggerDisplay("{DisplayString,nq}")]
    [DebuggerTypeProxy(typeof(Vector128DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector128.Size)]
    public readonly struct Vector128<T> : IEquatable<Vector128<T>>
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/coreclr/issues/15694)
        private readonly ulong _00;
        private readonly ulong _01;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector128{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();
                return Vector128.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector128{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector128<T> Zero
        {
            [Intrinsic]
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

        /// <summary>Determines whether the specified <see cref="Vector128{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector128{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector128<T> other)
        {
            ThrowHelper.ThrowForUnsupportedVectorBaseType<T>();

            if (Sse.IsSupported && (typeof(T) == typeof(float)))
            {
                Vector128<float> result = Sse.CompareEqual(this.AsSingle(), other.AsSingle());
                return Sse.MoveMask(result) == 0b1111; // We have one bit per element
            }

            if (Sse2.IsSupported)
            {
                if (typeof(T) == typeof(double))
                {
                    Vector128<double> result = Sse2.CompareEqual(this.AsDouble(), other.AsDouble());
                    return Sse2.MoveMask(result) == 0b11; // We have one bit per element
                }
                else
                {
                    // Unlike float/double, there are no special values to consider
                    // for integral types and we can just do a comparison that all
                    // bytes are exactly the same.

                    Debug.Assert((typeof(T) != typeof(float)) && (typeof(T) != typeof(double)));
                    Vector128<byte> result = Sse2.CompareEqual(this.AsByte(), other.AsByte());
                    return Sse2.MoveMask(result) == 0b1111_1111_1111_1111; // We have one bit per element
                }
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Vector128<T> vector, Vector128<T> other)
            {
                for (int i = 0; i < Count; i++)
                {
                    if (!((IEquatable<T>)(vector.GetElement(i))).Equals(other.GetElement(i)))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>Determines whether the specified object is equal to the current instance.</summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector128{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals(object? obj)
        {
            return (obj is Vector128<T>) && Equals((Vector128<T>)(obj));
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
