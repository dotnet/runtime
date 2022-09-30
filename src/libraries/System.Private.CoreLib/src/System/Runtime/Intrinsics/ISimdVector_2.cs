// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Runtime.Intrinsics
{
    /// <summary>Defines a single instruction, multiple data (SIMD) vector type.</summary>
    /// <typeparam name="TSelf">The type that implements the interface.</typeparam>
    /// <typeparam name="T">The type of the elements in the vector.</typeparam>
    internal interface ISimdVector<TSelf, T>
        : IAdditionOperators<TSelf, TSelf, TSelf>,
          // IAdditiveIdentity<TSelf, TSelf>,
          IBitwiseOperators<TSelf, TSelf, TSelf>,
          // IComparisonOperators<TSelf, TSelf, bool>,
          // IDecrementOperators<TSelf>,
          IDivisionOperators<TSelf, TSelf, TSelf>,
          IEqualityOperators<TSelf, TSelf, bool>,
          IEquatable<TSelf>,
          // IIncrementOperators<TSelf>,
          // IMinMaxValue<TSelf>,
          // IModulusOperators<TSelf, TSelf, TSelf>,
          // IMultiplicativeIdentity<TSelf, TSelf>,
          IMultiplyOperators<TSelf, TSelf, TSelf>,
          // IShiftOperators<TSelf, int, TSelf>
          // ISpanFormattable,
          ISubtractionOperators<TSelf, TSelf, TSelf>,
          IUnaryNegationOperators<TSelf, TSelf>,
          IUnaryPlusOperators<TSelf, TSelf>
        where TSelf : unmanaged, ISimdVector<TSelf, T>
        where T : struct
    {
        /// <summary>Gets an instance of the vector type in which all bits are set.</summary>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf AllBitsSet { get; }

        /// <summary>Gets the number of <typeparamref name="T" /> that are in the vector.</summary>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract int Count { get; }

        /// <summary>Gets a value that indicates whether the vector operations are subject to hardware acceleration through JIT intrinsic support.</summary>
        /// <value><see langword="true" /> if the vector operations are subject to hardware acceleration; otherwise, <see langword="false" />.</value>
        static abstract bool IsHardwareAccelerated { get; }

        /// <summary>Gets <see langword="true" /> if <typeparamref name="T" /> is supported; otherwise, <see langword="false" />.</summary>
        /// <returns><see langword="true" /> if <typeparamref name="T" /> is supported; otherwise, <see langword="false" />.</returns>
        static abstract bool IsSupported { get; }

        /// <summary>Gets an instance of the vector type in which each element is the value <c>zero</c>.</summary>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Zero { get; }

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        T this[int index] { get; }

        /// <summary>Computes the absolute of a vector.</summary>
        /// <param name="vector">The vector for which to get its absolute.</param>
        /// <returns>A absolute of <paramref name="vector" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Abs(TSelf vector);

        /// <summary>Conditionally selects bits from two vectors based on a given condition.</summary>
        /// <param name="condition">The mask that is used to select a value from <paramref name="left" /> or <paramref name="right" />.</param>
        /// <param name="left">The vector that is selected when the corresponding bit in <paramref name="condition" /> is one.</param>
        /// <param name="right">The vector that is selected when the corresponding bit in <paramref name="condition" /> is zero.</param>
        /// <returns>A vector whose bits come from <paramref name="left" /> or <paramref name="right" /> based on the value of <paramref name="condition" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf ConditionalSelect(TSelf condition, TSelf left, TSelf right);

        /// <summary>Copies a vector to a given array.</summary>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        static virtual void CopyTo(TSelf vector, T[] destination)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (destination.Length < TSelf.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(destination));
            Unsafe.WriteUnaligned(ref address, vector);
        }

        /// <summary>Copies a vector to a given array starting at the specified index.</summary>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The array to which <paramref name="vector" /> is copied.</param>
        /// <param name="startIndex">The starting index of <paramref name="destination" /> which <paramref name="vector" /> will be copied to.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Count" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex" /> is negative or greater than the length of <paramref name="destination" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="destination" /> is <c>null</c>.</exception>
        static virtual void CopyTo(TSelf vector, T[] destination, int startIndex)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((uint)(startIndex) >= (uint)(destination.Length))
            {
                ThrowHelper.ThrowStartIndexArgumentOutOfRange_ArgumentOutOfRange_IndexMustBeLess();
            }

            if ((destination.Length - startIndex) < TSelf.Count)
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(destination));
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref address, startIndex), vector);
        }

        /// <summary>Copies a vector to a given span.</summary>
        /// <param name="vector">The vector to be copied.</param>
        /// <param name="destination">The span to which the <paramref name="vector" /> is copied.</param>
        /// <exception cref="ArgumentException">The length of <paramref name="destination" /> is less than <see cref="Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static virtual void CopyTo(TSelf vector, Span<T> destination)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            if ((uint)(destination.Length) < (uint)(TSelf.Count))
            {
                ThrowHelper.ThrowArgumentException_DestinationTooShort();
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination));
            Unsafe.WriteUnaligned(ref address, vector);
        }

        /// <summary>Creates a new vector with all elements initialized to the specified value.</summary>
        /// <param name="value">The value that all elements will be initialized to.</param>
        /// <returns>A new vector with all elements initialized to <paramref name="value" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Create(T value);

        /// <summary>Creates a new vector from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <returns>A new vector with its elements set to the first <see cref="Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        static virtual TSelf Create(T[] values)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if (values.Length < TSelf.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(values));
            return Unsafe.ReadUnaligned<TSelf>(ref address);
        }

        /// <summary>Creates a new vector from a given array.</summary>
        /// <param name="values">The array from which the vector is created.</param>
        /// <param name="index">The index in <paramref name="values" /> at which to being reading elements.</param>
        /// <returns>A new vector with its elements set to the first <see cref="Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" />, starting from <paramref name="index" />, is less than <see cref="Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        /// <exception cref="NullReferenceException"><paramref name="values" /> is <c>null</c>.</exception>
        static virtual TSelf Create(T[] values, int index)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            // We explicitly don't check for `null` because historically this has thrown `NullReferenceException` for perf reasons

            if ((index < 0) || ((values.Length - index) < TSelf.Count))
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(values));
            return Unsafe.ReadUnaligned<TSelf>(ref Unsafe.Add(ref address, index));
        }

        /// <summary>Creates a new vector from a given readonly span.</summary>
        /// <param name="values">The readonly span from which the vector is created.</param>
        /// <returns>A new vector with its elements set to the first <see cref="Count" /> elements from <paramref name="values" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The length of <paramref name="values" /> is less than <see cref="Count" />.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static virtual TSelf Create(ReadOnlySpan<T> values)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            if (values.Length < TSelf.Count)
            {
                ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.values);
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values));
            return Unsafe.ReadUnaligned<TSelf>(ref address);
        }

        /// <summary>Creates a new vector with the first element initialized to the specified value and the remaining elements initialized to zero.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new vector with the first element initialized to <paramref name="value" /> and the remaining elements initialized to zero.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf CreateScalar(T value);

        /// <summary>Creates a new vector with the first element initialized to the specified value and the remaining elements left uninitialized.</summary>
        /// <param name="value">The value that element 0 will be initialized to.</param>
        /// <returns>A new vector with the first element initialized to <paramref name="value" /> and the remaining elements left uninitialized.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf CreateScalarUnsafe(T value);

        /// <summary>Compares two vectors to determine if they are equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were equal.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Equals(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if all elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool EqualsAll(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if any elements are equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool EqualsAny(TSelf left, TSelf right);

        /// <summary>Gets the element at the specified index.</summary>
        /// <param name="vector">The vector to get the element from.</param>
        /// <param name="index">The index of the element to get.</param>
        /// <returns>The value of the element at <paramref name="index" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract T GetElement(TSelf vector, int index);

        /// <summary>Compares two vectors to determine which is greater on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf GreaterThan(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if all elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool GreaterThanAll(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if any elements are greater.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool GreaterThanAny(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine which is greater or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were greater or equal.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf GreaterThanOrEqual(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if all elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool GreaterThanOrEqualAll(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if any elements are greater or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was greater than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool GreaterThanOrEqualAny(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine which is less on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf LessThan(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if all elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool LessThanAll(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if any elements are less.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool LessThanAny(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine which is less or equal on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="left" />.</param>
        /// <param name="right">The vector to compare with <paramref name="right" />.</param>
        /// <returns>A vector whose elements are all-bits-set or zero, depending on if which of the corresponding elements in <paramref name="left" /> and <paramref name="right" /> were less or equal.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf LessThanOrEqual(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if all elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if all elements in <paramref name="left" /> were less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool LessThanOrEqualAll(TSelf left, TSelf right);

        /// <summary>Compares two vectors to determine if any elements are less or equal.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns><c>true</c> if any elements in <paramref name="left" /> was less than or equal to the corresponding element in <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract bool LessThanOrEqualAny(TSelf left, TSelf right);

        /// <summary>Loads a vector from the given source.</summary>
        /// <param name="source">The source from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf LoadUnsafe(ref T source);

        /// <summary>Loads a vector from the given source and element offset.</summary>
        /// <param name="source">The source to which <paramref name="elementOffset" /> will be added before loading the vector.</param>
        /// <param name="elementOffset">The element offset from <paramref name="source" /> from which the vector will be loaded.</param>
        /// <returns>The vector loaded from <paramref name="source" /> plus <paramref name="elementOffset" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf LoadUnsafe(ref T source, nuint elementOffset);

        /// <summary>Computes the maximum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the maximum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Max(TSelf left, TSelf right);

        /// <summary>Computes the minimum of two vectors on a per-element basis.</summary>
        /// <param name="left">The vector to compare with <paramref name="right" />.</param>
        /// <param name="right">The vector to compare with <paramref name="left" />.</param>
        /// <returns>A vector whose elements are the minimum of the corresponding elements in <paramref name="left" /> and <paramref name="right" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf Min(TSelf left, TSelf right);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="vector">The vector that will be stored.</param>
        /// <param name="destination">The destination at which <paramref name="vector" /> will be stored.</param>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract void StoreUnsafe(TSelf vector, ref T destination);

        /// <summary>Stores a vector at the given destination.</summary>
        /// <param name="vector">The vector that will be stored.</param>
        /// <param name="destination">The destination to which <paramref name="elementOffset" /> will be added before the vector will be stored.</param>
        /// <param name="elementOffset">The element offset from <paramref name="destination" /> from which the vector will be stored.</param>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract void StoreUnsafe(TSelf vector, ref T destination, nuint elementOffset);

        /// <summary>Converts the given vector to a scalar containing the value of the first element.</summary>
        /// <param name="vector">The vector to get the first element from.</param>
        /// <returns>A scalar <typeparamref name="T" /> containing the value of the first element.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract T ToScalar(TSelf vector);

        /// <summary>Tries to copy a <see cref="Vector{T}" /> to a given span.</summary>
        /// <param name="vector">The vector to copy.</param>
        /// <param name="destination">The span to which <paramref name="destination" /> is copied.</param>
        /// <returns><c>true</c> if <paramref name="vector" /> was successfully copied to <paramref name="destination" />; otherwise, <c>false</c> if the length of <paramref name="destination" /> is less than <see cref="Count" />.</returns>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static virtual bool TryCopyTo(TSelf vector, Span<T> destination)
        {
            if (!TSelf.IsSupported)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            if ((uint)(destination.Length) < (uint)(TSelf.Count))
            {
                return false;
            }

            ref byte address = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination));
            Unsafe.WriteUnaligned(ref address, vector);

            return true;
        }

        /// <summary>Creates a new <see cref="Vector128{T}" /> with the element at the specified index set to the specified value and the remaining elements set to the same value as that in the given vector.</summary>
        /// <param name="vector">The vector to get the remaining elements from.</param>
        /// <param name="index">The index of the element to set.</param>
        /// <param name="value">The value to set the element to.</param>
        /// <returns>A <see cref="Vector128{T}" /> with the value of the element at <paramref name="index" /> set to <paramref name="value" /> and the remaining elements set to the same value as that in <paramref name="vector" />.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index" /> was less than zero or greater than the number of elements.</exception>
        /// <exception cref="NotSupportedException">The type of the elements in the vector (<typeparamref name="T" />) is not supported.</exception>
        static abstract TSelf WithElement(TSelf vector, int index, T value);

        // TODO: static abstract operator *(TSelf left, T right);
        // TODO: As<TTo>()
        // TODO: Ceiling()
        // TODO: Dot
        // TODO: ExtractMostSignificantBits
        // TODO: Floor
        // TODO: Load
        // TODO: LoadAligned
        // TODO: LoadAlignedNonTemporal
        // TODO: ShiftLeft
        // TODO: ShiftRightArithmetic
        // TODO: ShiftRightLogical
        // TODO: Shuffle
        // TODO: Sqrt
        // TODO: Store
        // TODO: StoreAligned
        // TODO: StoreAlignedNonTemporal
        // TODO: Sum
    }
}
