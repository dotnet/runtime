// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    [DebuggerTypeProxy(typeof(Vector256DebugView<>))]
    [StructLayout(LayoutKind.Sequential, Size = Vector256.Size)]
    public readonly struct Vector256<T> : IEquatable<Vector256<T>>
        where T : struct
    {
        // These fields exist to ensure the alignment is 8, rather than 1.
        // This also allows the debug view to work https://github.com/dotnet/runtime/issues/9495)
        private readonly ulong _00;
        private readonly ulong _01;
        private readonly ulong _02;
        private readonly ulong _03;

        /// <summary>Gets the number of <typeparamref name="T" /> that are in a <see cref="Vector256{T}" />.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return Vector256.Size / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Gets a new <see cref="Vector256{T}" /> with all elements initialized to zero.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return default;
            }
        }


        /// <summary>Gets a new <see cref="Vector256{T}" /> with all bits set to 1.</summary>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public static Vector256<T> AllBitsSet
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
                return Vector256.Create(0xFFFFFFFF).As<uint, T>();
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

        /// <summary>Determines whether the specified <see cref="Vector256{T}" /> is equal to the current instance.</summary>
        /// <param name="other">The <see cref="Vector256{T}" /> to compare with the current instance.</param>
        /// <returns><c>true</c> if <paramref name="other" /> is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector256<T> other)
        {
            if (Avx.IsSupported)
            {
                if (typeof(T) == typeof(float))
                {
                    Vector256<float> result = Avx.Compare(this.AsSingle(), other.AsSingle(), FloatComparisonMode.OrderedEqualNonSignaling);
                    return Avx.MoveMask(result) == 0b1111_1111; // We have one bit per element
                }

                if (typeof(T) == typeof(double))
                {
                    Vector256<double> result = Avx.Compare(this.AsDouble(), other.AsDouble(), FloatComparisonMode.OrderedEqualNonSignaling);
                    return Avx.MoveMask(result) == 0b1111; // We have one bit per element
                }
            }

            if (Avx2.IsSupported)
            {
                // Unlike float/double, there are no special values to consider
                // for integral types and we can just do a comparison that all
                // bytes are exactly the same.

                Debug.Assert((typeof(T) != typeof(float)) && (typeof(T) != typeof(double)));
                Vector256<byte> result = Avx2.CompareEqual(this.AsByte(), other.AsByte());
                return Avx2.MoveMask(result) == unchecked((int)(0b1111_1111_1111_1111_1111_1111_1111_1111)); // We have one bit per element
            }

            return SoftwareFallback(in this, other);

            static bool SoftwareFallback(in Vector256<T> vector, Vector256<T> other)
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
        /// <returns><c>true</c> if <paramref name="obj" /> is a <see cref="Vector256{T}" /> and is equal to the current instance; otherwise, <c>false</c>.</returns>
        /// <exception cref="NotSupportedException">The type of the current instance (<typeparamref name="T" />) is not supported.</exception>
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return (obj is Vector256<T>) && Equals((Vector256<T>)(obj));
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
