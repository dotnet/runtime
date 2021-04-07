// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Internal.Runtime.CompilerServices;

namespace System.Numerics
{
    /* Note: The following patterns are used throughout the code here and are described here
    *
    * PATTERN:
    *    if (typeof(T) == typeof(int)) { ... }
    *    else if (typeof(T) == typeof(float)) { ... }
    * EXPLANATION:
    *    At runtime, each instantiation of Vector<T> will be type-specific, and each of these typeof blocks will be eliminated,
    *    as typeof(T) is a (JIT) compile-time constant for each instantiation. This design was chosen to eliminate any overhead from
    *    delegates and other patterns.
    *
    * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

    /// <summary>
    /// A structure that represents a single Vector. The count of this Vector is fixed but CPU register dependent.
    /// This struct only supports numerical types. This type is intended to be used as a building block for vectorizing
    /// large algorithms. This type is immutable, individual elements cannot be modified.
    /// </summary>
    [Intrinsic]
    public struct Vector<T> : IEquatable<Vector<T>>, IFormattable
        where T : struct
    {
        private Register register;

        /// <summary>Returns the number of elements stored in the vector. This value is hardware dependent.</summary>
        public static int Count
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return Unsafe.SizeOf<Vector<T>>() / Unsafe.SizeOf<T>();
            }
        }

        /// <summary>Returns a vector containing all zeroes.</summary>
        public static Vector<T> Zero
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return default;
            }
        }

        /// <summary>Returns a vector containing all ones.</summary>
        public static Vector<T> One
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return new Vector<T>(GetOneValue());
            }
        }

        internal static Vector<T> AllBitsSet
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
                return new Vector<T>(GetAllBitsSetValue());
            }
        }

        /// <summary>Constructs a vector whose components are all <paramref name="value" />.</summary>
        [Intrinsic]
        public unsafe Vector(T value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Unsafe.SkipInit(out this);

            for (nint index = 0; index < Count; index++)
            {
                SetElement(index, value);
            }
        }

        /// <summary>Constructs a vector from the given array. The size of the given array must be at least <see cref="Count" />.</summary>
        [Intrinsic]
        public unsafe Vector(T[] values) : this(values, 0) { }

        /// <summary>
        /// Constructs a vector from the given array, starting from the given index.
        /// The array must contain at least <see cref="Count" /> from the given index.
        /// </summary>
        [Intrinsic]
        public unsafe Vector(T[] values, int index)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values is null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
            }

            if (index < 0 || (values.Length - index) < Count)
            {
                Vector.ThrowInsufficientNumberOfElementsException(Count);
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[index]));
        }

        /// <summary>
        /// Constructs a vector from the given <see cref="ReadOnlySpan{Byte}" />.
        /// The span must contain at least <see cref="Vector{Byte}.Count" /> elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<byte> values)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values.Length < Vector<byte>.Count)
            {
                Vector.ThrowInsufficientNumberOfElementsException(Vector<byte>.Count);
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(values));
        }

        /// <summary>
        /// Constructs a vector from the given <see cref="ReadOnlySpan{T}" />.
        /// The span must contain at least <see cref="Count" /> elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(ReadOnlySpan<T> values)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (values.Length < Count)
            {
                Vector.ThrowInsufficientNumberOfElementsException(Count);
            }

            this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
        }

        /// <summary>
        /// Constructs a vector from the given <see cref="Span{T}" />.
        /// The span must contain at least <see cref="Count" /> elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector(Span<T> values) : this((ReadOnlySpan<T>)values) { }

        /// <summary>
        /// Copies the vector to the given <see cref="Span{Byte}" />.
        /// The destination span must be at least size <see cref="Vector{Byte}.Count" />.
        /// </summary>
        /// <param name="destination">The destination span which the values are copied into</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span</exception>
        public readonly void CopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Vector<byte>.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(destination), this);
        }

        /// <summary>
        /// Copies the vector to the given <see cref="Span{T}" />.
        /// The destination span must be at least size <see cref="Count" />.
        /// </summary>
        /// <param name="destination">The destination span which the values are copied into</param>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination span</exception>
        public readonly void CopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
        }

        /// <summary>
        /// Copies the vector to the given destination array.
        /// The destination array must be at least size <see cref="Count" />.
        /// </summary>
        /// <param name="destination">The destination array which the values are copied into</param>
        /// <exception cref="ArgumentNullException">If the destination array is null</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array</exception>
        [Intrinsic]
        public readonly void CopyTo(T[] destination) => CopyTo(destination, 0);

        /// <summary>
        /// Copies the vector to the given destination array.
        /// The destination array must be at least size <see cref="Count" />.
        /// </summary>
        /// <param name="destination">The destination array which the values are copied into</param>
        /// <param name="startIndex">The index to start copying to</param>
        /// <exception cref="ArgumentNullException">If the destination array is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If index is greater than end of the array or index is less than zero</exception>
        /// <exception cref="ArgumentException">If number of elements in source vector is greater than those available in destination array</exception>
        [Intrinsic]
        public readonly unsafe void CopyTo(T[] destination, int startIndex)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if (destination is null)
            {
                // Match the JIT's exception type here. For perf, a NullReference is thrown instead of an ArgumentNull.
                throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
            }

            if ((uint)startIndex >= (uint)destination.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), SR.Format(SR.Arg_ArgumentOutOfRangeException, startIndex));
            }

            if ((destination.Length - startIndex) < Count)
            {
                throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, startIndex));
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref destination[startIndex]), this);
        }

        /// <summary>Returns the element at the given index.</summary>
        public readonly unsafe T this[int index]
        {
            [Intrinsic]
            get
            {
                ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

                if ((uint)index >= (uint)Count)
                {
                    throw new IndexOutOfRangeException(SR.Format(SR.Arg_ArgumentOutOfRangeException, index));
                }

                return GetElement(index);
            }
        }

        /// <summary>Returns a boolean indicating whether the given Object is equal to this vector instance.</summary>
        /// <param name="obj">The Object to compare against.</param>
        /// <returns>True if the Object is equal to this vector; False otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly bool Equals([NotNullWhen(true)] object? obj) => (obj is Vector<T> other) && Equals(other);

        /// <summary>Returns a boolean indicating whether the given vector is equal to this vector instance.</summary>
        /// <param name="other">The vector to compare this instance to.</param>
        /// <returns>True if the other vector is equal to this instance; False otherwise.</returns>
        [Intrinsic]
        public readonly bool Equals(Vector<T> other) => this == other;

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>The hash code.</returns>
        public override readonly int GetHashCode()
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            HashCode hashCode = default;

            for (nint index = 0; index < Count; index++)
            {
                hashCode.Add(GetElement(index));
            }

            return hashCode.ToHashCode();
        }

        /// <summary>Returns a String representing this vector.</summary>
        /// <returns>The string representation.</returns>
        public override readonly string ToString() => ToString("G", CultureInfo.CurrentCulture);

        /// <summary>Returns a String representing this vector, using the specified format string to format individual elements.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <returns>The string representation.</returns>
        public readonly string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

        /// <summary>Returns a String representing this vector, using the specified format string to format individual elements and the given IFormatProvider.</summary>
        /// <param name="format">The format of individual elements.</param>
        /// <param name="formatProvider">The format provider to use when formatting elements.</param>
        /// <returns>The string representation.</returns>
        public readonly string ToString(string? format, IFormatProvider? formatProvider)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            StringBuilder sb = new StringBuilder();
            string separator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
            sb.Append('<');
            for (int index = 0; index < Count - 1; index++)
            {
                sb.Append(((IFormattable)GetElement(index)).ToString(format, formatProvider));
                sb.Append(separator);
                sb.Append(' ');
            }
            // Append last element w/out separator
            sb.Append(((IFormattable)GetElement(Count - 1)).ToString(format, formatProvider));
            sb.Append('>');
            return sb.ToString();
        }

        /// <summary>
        /// Attempts to copy the vector to the given <see cref="Span{Byte}" />.
        /// The destination span must be at least size <see cref="Vector{Byte}.Count" />.
        /// </summary>
        /// <param name="destination">The destination span which the values are copied into</param>
        /// <returns>True if the source vector was successfully copied to <paramref name="destination" />. False if
        /// <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        public readonly bool TryCopyTo(Span<byte> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Vector<byte>.Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(destination), this);
            return true;
        }

        /// <summary>
        /// Attempts to copy the vector to the given <see cref="Span{T}" />.
        /// The destination span must be at least size <see cref="Count" />.
        /// </summary>
        /// <param name="destination">The destination span which the values are copied into</param>
        /// <returns>True if the source vector was successfully copied to <paramref name="destination" />. False if
        /// <paramref name="destination" /> is not large enough to hold the source vector.</returns>
        public readonly bool TryCopyTo(Span<T> destination)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();

            if ((uint)destination.Length < (uint)Count)
            {
                return false;
            }

            Unsafe.WriteUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
            return true;
        }

        /// <summary>Adds two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The summed vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator +(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarAdd(left.GetElement(index), right.GetElement(index)));
            }

            return result;
        }

        /// <summary>Subtracts the second vector from the first.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The difference vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator -(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarSubtract(left.GetElement(index), right.GetElement(index)));
            }

            return result;
        }

        /// <summary>Multiplies two vectors together.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The product vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator *(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarMultiply(left.GetElement(index), right.GetElement(index)));
            }

            return result;
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="value">The source vector.</param>
        /// <param name="factor">The scalar value.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        public static Vector<T> operator *(Vector<T> value, T factor)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarMultiply(value.GetElement(index), factor));
            }

            return result;
        }

        /// <summary>Multiplies a vector by the given scalar.</summary>
        /// <param name="factor">The scalar value.</param>
        /// <param name="value">The source vector.</param>
        /// <returns>The scaled vector.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator *(T factor, Vector<T> value) => value * factor;

        /// <summary>Divides the first vector by the second.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The vector resulting from the division.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator /(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarDivide(left.GetElement(index), right.GetElement(index)));
            }

            return result;
        }

        /// <summary>Negates a given vector.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The negated vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator -(Vector<T> value) => Zero - value;

        /// <summary>Returns a new vector by performing a bitwise-and operation on each of the elements in the given vectors.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator &(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Vector<T> result = default;

            result.register.uint64_0 = left.register.uint64_0 & right.register.uint64_0;
            result.register.uint64_1 = left.register.uint64_1 & right.register.uint64_1;

            return result;
        }

        /// <summary>Returns a new vector by performing a bitwise-or operation on each of the elements in the given vectors.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator |(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Vector<T> result = default;

            result.register.uint64_0 = left.register.uint64_0 | right.register.uint64_0;
            result.register.uint64_1 = left.register.uint64_1 | right.register.uint64_1;

            return result;
        }

        /// <summary>Returns a new vector by performing a bitwise-exclusive-or operation on each of the elements in the given vectors.</summary>
        /// <param name="left">The first source vector.</param>
        /// <param name="right">The second source vector.</param>
        /// <returns>The resultant vector.</returns>
        [Intrinsic]
        public static unsafe Vector<T> operator ^(Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Vector<T> result = default;

            result.register.uint64_0 = left.register.uint64_0 ^ right.register.uint64_0;
            result.register.uint64_1 = left.register.uint64_1 ^ right.register.uint64_1;

            return result;
        }

        /// <summary>Returns a new vector whose elements are obtained by taking the one's complement of the given vector's elements.</summary>
        /// <param name="value">The source vector.</param>
        /// <returns>The one's complement vector.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector<T> operator ~(Vector<T> value) => AllBitsSet ^ value;

        /// <summary>Returns a boolean indicating whether each pair of elements in the given vectors are equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The first vector to compare.</param>
        /// <returns>True if all elements are equal; False otherwise.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector<T> left, Vector<T> right)
        {
            for (nint index = 0; index < Count; index++)
            {
                if (!ScalarEquals(left.GetElement(index), right.GetElement(index)))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Returns a boolean indicating whether any single pair of elements in the given vectors are not equal.</summary>
        /// <param name="left">The first vector to compare.</param>
        /// <param name="right">The second vector to compare.</param>
        /// <returns>True if left and right are not equal; False otherwise.</returns>
        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector<T> left, Vector<T> right) => !(left == right);

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<byte>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<byte>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<sbyte>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<sbyte>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<ushort>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<ushort>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<short>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<short>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<uint>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<uint>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<int>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<int>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<ulong>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<ulong>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<long>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<long>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<float>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<float>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<double>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<double>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [CLSCompliant(false)]
        [Intrinsic]
        public static explicit operator Vector<nuint>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<nuint>>(ref value);
        }

        /// <summary>Reinterprets the bits of the given vector into those of another type.</summary>
        /// <param name="value">The source vector</param>
        /// <returns>The reinterpreted vector.</returns>
        [Intrinsic]
        public static explicit operator Vector<nint>(Vector<T> value)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            return Unsafe.As<Vector<T>, Vector<nint>>(ref value);
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> Equals(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarEquals(left.GetElement(index), right.GetElement(index)) ? GetAllBitsSetValue() : default;
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> LessThan(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarLessThan(left.GetElement(index), right.GetElement(index)) ? GetAllBitsSetValue() : default;
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe Vector<T> GreaterThan(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarGreaterThan(left.GetElement(index), right.GetElement(index)) ? GetAllBitsSetValue() : default;
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        internal static Vector<T> GreaterThanOrEqual(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarGreaterThanOrEqual(left.GetElement(index), right.GetElement(index)) ? GetAllBitsSetValue() : default;
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        internal static Vector<T> LessThanOrEqual(Vector<T> left, Vector<T> right)
        {

            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarLessThanOrEqual(left.GetElement(index), right.GetElement(index)) ? GetAllBitsSetValue() : default;
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        internal static Vector<T> ConditionalSelect(Vector<T> condition, Vector<T> left, Vector<T> right)
        {
            ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
            Vector<T> result = default;

            result.register.uint64_0 = (left.register.uint64_0 & condition.register.uint64_0) | (right.register.uint64_0 & ~condition.register.uint64_0);
            result.register.uint64_1 = (left.register.uint64_1 & condition.register.uint64_1) | (right.register.uint64_1 & ~condition.register.uint64_1);

            return result;
        }

        [Intrinsic]
        internal static unsafe Vector<T> Abs(Vector<T> value)
        {
            if (typeof(T) == typeof(byte))
            {
                return value;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return value;
            }
            else if (typeof(T) == typeof(uint))
            {
                return value;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return value;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return value;
            }
            else
            {
                Vector<T> result = default;

                for (nint index = 0; index < Count; index++)
                {
                    result.SetElement(index, ScalarAbs(value.GetElement(index)));
                }

                return result;
            }
        }

        [Intrinsic]
        internal static unsafe Vector<T> Min(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarLessThan(left.GetElement(index), right.GetElement(index)) ? left.GetElement(index) : right.GetElement(index);
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        internal static unsafe Vector<T> Max(Vector<T> left, Vector<T> right)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                T value = ScalarGreaterThan(left.GetElement(index), right.GetElement(index)) ? left.GetElement(index) : right.GetElement(index);
                result.SetElement(index, value);
            }

            return result;
        }

        [Intrinsic]
        internal static T Dot(Vector<T> left, Vector<T> right)
        {
            T product = default;

            for (nint index = 0; index < Count; index++)
            {
                product = ScalarAdd(product, ScalarMultiply(left.GetElement(index), right.GetElement(index)));
            }

            return product;
        }

        [Intrinsic]
        internal static unsafe Vector<T> SquareRoot(Vector<T> value)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarSqrt(value.GetElement(index)));
            }

            return result;
        }

        [Intrinsic]
        internal static unsafe Vector<T> Ceiling(Vector<T> value)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarCeiling(value.GetElement(index)));
            }

            return result;
        }

        [Intrinsic]
        internal static unsafe Vector<T> Floor(Vector<T> value)
        {
            Vector<T> result = default;

            for (nint index = 0; index < Count; index++)
            {
                result.SetElement(index, ScalarFloor(value.GetElement(index)));
            }

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarEquals(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left == (byte)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left == (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left == (ushort)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left == (short)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left == (uint)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left == (int)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left == (ulong)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left == (long)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left == (float)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left == (double)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left == (nuint)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left == (nint)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarLessThan(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left < (byte)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left < (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left < (ushort)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left < (short)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left < (uint)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left < (int)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left < (ulong)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left < (long)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left < (float)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left < (double)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left < (nuint)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left < (nint)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarLessThanOrEqual(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left <= (byte)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left <= (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left <= (ushort)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left <= (short)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left <= (uint)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left <= (int)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left <= (ulong)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left <= (long)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left <= (float)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left <= (double)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left <= (nuint)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left <= (nint)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarGreaterThan(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left > (byte)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left > (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left > (ushort)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left > (short)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left > (uint)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left > (int)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left > (ulong)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left > (long)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left > (float)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left > (double)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left > (nuint)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left > (nint)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool ScalarGreaterThanOrEqual(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (byte)(object)left >= (byte)(object)right;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (sbyte)(object)left >= (sbyte)(object)right;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (ushort)(object)left >= (ushort)(object)right;
            }
            else if (typeof(T) == typeof(short))
            {
                return (short)(object)left >= (short)(object)right;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (uint)(object)left >= (uint)(object)right;
            }
            else if (typeof(T) == typeof(int))
            {
                return (int)(object)left >= (int)(object)right;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (ulong)(object)left >= (ulong)(object)right;
            }
            else if (typeof(T) == typeof(long))
            {
                return (long)(object)left >= (long)(object)right;
            }
            else if (typeof(T) == typeof(float))
            {
                return (float)(object)left >= (float)(object)right;
            }
            else if (typeof(T) == typeof(double))
            {
                return (double)(object)left >= (double)(object)right;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (nuint)(object)left >= (nuint)(object)right;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (nint)(object)left >= (nint)(object)right;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarAdd(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left + (byte)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left + (short)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)left + (uint)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)left + (int)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)left + (ulong)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)left + (long)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left + (float)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left + (double)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)left + (nuint)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)left + (nint)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarSubtract(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left - (byte)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left - (short)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)left - (uint)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)left - (int)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)left - (ulong)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)left - (long)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left - (float)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left - (double)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)left - (nuint)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)left - (nint)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarMultiply(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left * (byte)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left * (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left * (ushort)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left * (short)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)left * (uint)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)left * (int)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)left * (ulong)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)left * (long)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left * (float)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left * (double)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)left * (nuint)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)left * (nint)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarDivide(T left, T right)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)((byte)(object)left / (byte)(object)right);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)((sbyte)(object)left / (sbyte)(object)right);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)((ushort)(object)left / (ushort)(object)right);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)((short)(object)left / (short)(object)right);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)((uint)(object)left / (uint)(object)right);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)((int)(object)left / (int)(object)right);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)((ulong)(object)left / (ulong)(object)right);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)((long)(object)left / (long)(object)right);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)((float)(object)left / (float)(object)right);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)((double)(object)left / (double)(object)right);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)((nuint)(object)left / (nuint)(object)right);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)((nint)(object)left / (nint)(object)right);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetOneValue()
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)1;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)1;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)1;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)1;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)1;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)1;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)1;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)1;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)1;
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)1;
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)1;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)1;
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T GetAllBitsSetValue()
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)byte.MaxValue;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)-1;
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)ushort.MaxValue;
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)-1;
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)uint.MaxValue;
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)-1;
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)ulong.MaxValue;
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)-1;
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)BitConverter.Int32BitsToSingle(-1);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)BitConverter.Int64BitsToDouble(-1);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)nuint.MaxValue;
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)(-1);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarAbs(T value)
        {
            // byte, ushort, uint, and ulong should have already been handled

            if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)Math.Abs((sbyte)(object)value);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)Math.Abs((short)(object)value);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)Math.Abs((int)(object)value);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)Math.Abs((long)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)Math.Abs((float)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Abs((double)(object)value);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)Math.Abs((nint)(object)value);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarSqrt(T value)
        {
            if (typeof(T) == typeof(byte))
            {
                return (T)(object)(byte)Math.Sqrt((byte)(object)value);
            }
            else if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)(sbyte)Math.Sqrt((sbyte)(object)value);
            }
            else if (typeof(T) == typeof(ushort))
            {
                return (T)(object)(ushort)Math.Sqrt((ushort)(object)value);
            }
            else if (typeof(T) == typeof(short))
            {
                return (T)(object)(short)Math.Sqrt((short)(object)value);
            }
            else if (typeof(T) == typeof(uint))
            {
                return (T)(object)(uint)Math.Sqrt((uint)(object)value);
            }
            else if (typeof(T) == typeof(int))
            {
                return (T)(object)(int)Math.Sqrt((int)(object)value);
            }
            else if (typeof(T) == typeof(ulong))
            {
                return (T)(object)(ulong)Math.Sqrt((ulong)(object)value);
            }
            else if (typeof(T) == typeof(long))
            {
                return (T)(object)(long)Math.Sqrt((long)(object)value);
            }
            else if (typeof(T) == typeof(float))
            {
                return (T)(object)(float)Math.Sqrt((float)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)(double)Math.Sqrt((double)(object)value);
            }
            else if (typeof(T) == typeof(nuint))
            {
                return (T)(object)(nuint)Math.Sqrt((nuint)(object)value);
            }
            else if (typeof(T) == typeof(nint))
            {
                return (T)(object)(nint)Math.Sqrt((nint)(object)value);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarCeiling(T value)
        {
            if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Ceiling((float)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Ceiling((double)(object)value);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static T ScalarFloor(T value)
        {
            if (typeof(T) == typeof(float))
            {
                return (T)(object)MathF.Floor((float)(object)value);
            }
            else if (typeof(T) == typeof(double))
            {
                return (T)(object)Math.Floor((double)(object)value);
            }
            else
            {
                throw new NotSupportedException(SR.Arg_TypeNotSupported);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly T GetElement(nint index)
        {
            Debug.Assert((index >= 0) && (index < Count));
            return Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef<Vector<T>>(in this)), index);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetElement(nint index, T value)
        {
            Debug.Assert((index >= 0) && (index < Count));
            Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef<Vector<T>>(in this)), index) = value;
        }
    }
}
