// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    /// <summary>
    /// Represents a pseudo-random number generator, which is an algorithm that produces a sequence of numbers
    /// that meet certain statistical requirements for randomness.
    /// </summary>
    public partial class Random
    {
        private const int StackallocThreshold = 256;

        /// <summary>The underlying generator implementation.</summary>
        /// <remarks>
        /// This is separated out so that different generators can be used based on how this Random instance is constructed.
        /// If it's built from a seed, then we may need to ensure backwards compatibility for folks expecting consistent sequences
        /// based on that seed.  If the instance is actually derived from Random, then we need to ensure the derived type's
        /// overrides are called anywhere they were being called previously.  But if the instance is the base type and is constructed
        /// with the default constructor, we have a lot of flexibility as to how to optimize the performance and quality of the generator.
        /// </remarks>
        private readonly ImplBase _impl;

        /// <summary>Initializes a new instance of the <see cref="Random"/> class using a default seed value.</summary>
        public Random() =>
            // With no seed specified, if this is the base type, we can implement this however we like.
            // If it's a derived type, for compat we respect the previous implementation, so that overrides
            // are called as they were previously.
            _impl = GetType() == typeof(Random) ? new XoshiroImpl() : new CompatDerivedImpl(this);

        /// <summary>Initializes a new instance of the Random class, using the specified seed value.</summary>
        /// <param name="Seed">
        /// A number used to calculate a starting value for the pseudo-random number sequence. If a negative number
        /// is specified, the absolute value of the number is used.
        /// </param>
        public Random(int Seed) =>
            // With a custom seed, if this is the base Random class, we still need to respect the same algorithm that's been
            // used in the past, but we can do so without having to deal with calling the right overrides in a derived type.
            // If this is a derived type, we need to handle always using the same overrides we've done previously.
            _impl = GetType() == typeof(Random) ? new CompatSeedImpl(Seed) : new CompatDerivedImpl(this, Seed);

        /// <summary>Constructor used by <see cref="ThreadSafeRandom"/>.</summary>
        /// <param name="isThreadSafeRandom">Must be true.</param>
        private protected Random(bool isThreadSafeRandom)
        {
            Debug.Assert(isThreadSafeRandom);
            _impl = null!; // base implementation isn't used at all
        }

        /// <summary>Provides a thread-safe <see cref="Random"/> instance that may be used concurrently from any thread.</summary>
        public static Random Shared { get; } = new ThreadSafeRandom();

        /// <summary>Returns a non-negative random integer.</summary>
        /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <see cref="int.MaxValue"/>.</returns>
        public virtual int Next()
        {
            int result = _impl.Next();
            AssertInRange(result, 0, int.MaxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 32-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
        /// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public virtual int Next(int maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

            int result = _impl.Next(maxValue);
            AssertInRange(result, 0, maxValue);
            return result;
        }

        /// <summary>Returns a random integer that is within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
        /// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        public virtual int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                ThrowMinMaxValueSwapped();
            }

            int result = _impl.Next(minValue, maxValue);
            AssertInRange(result, minValue, maxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer.</summary>
        /// <returns>A 64-bit signed integer that is greater than or equal to 0 and less than <see cref="long.MaxValue"/>.</returns>
        public virtual long NextInt64()
        {
            long result = _impl.NextInt64();
            AssertInRange(result, 0, long.MaxValue);
            return result;
        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to 0.</param>
        /// <returns>
        /// A 64-bit signed integer that is greater than or equal to 0, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily
        /// includes 0 but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals 0, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than 0.</exception>
        public virtual long NextInt64(long maxValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

            long result = _impl.NextInt64(maxValue);
            AssertInRange(result, 0, maxValue);
            return result;
        }

        /// <summary>Returns a random integer that is within a specified range.</summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A 64-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/>
        /// but not <paramref name="maxValue"/>. If minValue equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        public virtual long NextInt64(long minValue, long maxValue)
        {
            if (minValue > maxValue)
            {
                ThrowMinMaxValueSwapped();
            }

            long result = _impl.NextInt64(minValue, maxValue);
            AssertInRange(result, minValue, maxValue);
            return result;
        }

        /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <returns>A single-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public virtual float NextSingle()
        {
            float result = _impl.NextSingle();
            AssertInRange(result);
            return result;
        }

        /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        public virtual double NextDouble()
        {
            double result = _impl.NextDouble();
            AssertInRange(result);
            return result;
        }

        /// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        public virtual void NextBytes(byte[] buffer)
        {
            if (buffer is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
            }

            _impl.NextBytes(buffer);
        }

        /// <summary>Fills the elements of a specified span of bytes with random numbers.</summary>
        /// <param name="buffer">The array to be filled with random numbers.</param>
        public virtual void NextBytes(Span<byte> buffer) => _impl.NextBytes(buffer);

        /// <summary>Returns a non-negative random integer of type <typeparamref name="T"/>.</summary>
        /// <typeparam name="T">The type of integer to generate.</typeparam>
        /// <returns>
        /// A value of type <typeparamref name="T"/> in the inclusive range [0, <c>T.MaxValue</c>].
        /// </returns>
        /// <remarks>
        /// Unlike <see cref="Next()"/>, which returns an <see cref="int"/> that is less than <see cref="int.MaxValue"/>,
        /// <c>NextInteger&lt;int&gt;()</c> returns an <see cref="int"/> in the inclusive range from zero through
        /// <see cref="int.MaxValue"/> and may return <see cref="int.MaxValue"/>.
        /// <typeparamref name="T"/> must use a two's complement representation for signed values.
        /// </remarks>
        public T NextInteger<T>() where T : IBinaryInteger<T>, IMinMaxValue<T>
        {
            if (T.MaxValue == T.Zero)
            {
                return T.Zero;
            }

            Debug.Assert(T.IsPositive(T.MaxValue));

            int bitLength = T.MaxValue.GetShortestBitLength();
            int byteCount = (bitLength + 7) >> 3;

            // Compute mask for the top byte to avoid negative values for signed types
            // and to reduce rejection rate for custom integer types.
            int topBits = bitLength & 7;
            byte topMask = topBits == 0 ? byte.MaxValue : (byte)((1 << topBits) - 1);

            byte[]? rented = null;
            Span<byte> bytes = byteCount <= StackallocThreshold
                ? stackalloc byte[StackallocThreshold]
                : rented = ArrayPool<byte>.Shared.Rent(byteCount);
            bytes = bytes.Slice(0, byteCount);

            try
            {
                while (true)
                {
                    NextBytes(bytes);
                    bytes[^1] &= topMask;

                    T value = T.ReadLittleEndian(bytes, isUnsigned: true);
                    if (value <= T.MaxValue)
                    {
                        return value;
                    }
                }
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>Returns a non-negative random integer that is less than the specified maximum.</summary>
        /// <typeparam name="T">The type of integer to generate.</typeparam>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated.
        /// <paramref name="maxValue"/> must be greater than or equal to zero.</param>
        /// <returns>
        /// A value of type <typeparamref name="T"/> that is greater than or equal to zero,
        /// and less than <paramref name="maxValue"/>; that is, the range of return values is ordinarily
        /// [0, <paramref name="maxValue"/>). However, if <paramref name="maxValue"/> equals zero, zero is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than zero.</exception>
        /// <remarks><typeparamref name="T"/> must use a two's complement representation for signed values.</remarks>
        public T NextInteger<T>(T maxValue) where T : IBinaryInteger<T>
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

            return NextBinaryIntegerInRange(maxValue);
        }

        /// <summary>Returns a random integer that is within a specified range.</summary>
        /// <typeparam name="T">The type of integer to generate.</typeparam>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned.
        /// <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>.</param>
        /// <returns>
        /// A value of type <typeparamref name="T"/> greater than or equal to <paramref name="minValue"/>
        /// and less than <paramref name="maxValue"/>; that is, the range of return values is ordinarily
        /// [<paramref name="minValue"/>, <paramref name="maxValue"/>). If <paramref name="minValue"/>
        /// equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>.</exception>
        /// <remarks><typeparamref name="T"/> must use a two's complement representation for signed values.</remarks>
        public T NextInteger<T>(T minValue, T maxValue) where T : IBinaryInteger<T>
        {
            if (minValue > maxValue)
            {
                ThrowMinMaxValueSwapped();
            }

            T range = maxValue - minValue;

            // For signed types, subtraction may overflow when the range exceeds T.MaxValue.
            // T.IsNegative(range) detects this. Fall back to full-width generation.
            if (T.IsNegative(range))
            {
                return NextBinaryIntegerFullRange(minValue, maxValue);
            }

            return NextBinaryIntegerInRange(range) + minValue;
        }

        /// <summary>Generates a random value in [T.Zero, maxExclusive) where maxExclusive is non-negative.</summary>
        private T NextBinaryIntegerInRange<T>(T maxExclusive) where T : IBinaryInteger<T>
        {
            Debug.Assert(!T.IsNegative(maxExclusive));

            // Fast paths for common types using existing optimized implementations.
            // The JIT eliminates the dead branches when T is a known value type.
            if (typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(char) ||
                typeof(T) == typeof(int) ||
                (typeof(T) == typeof(nint) && nint.Size == 4))
            {
                return T.CreateTruncating(Next(int.CreateTruncating(maxExclusive)));
            }

            if (typeof(T) == typeof(uint) ||
                typeof(T) == typeof(nint) ||
                typeof(T) == typeof(long) ||
                (typeof(T) == typeof(nuint) && nint.Size == 4))
            {
                return T.CreateTruncating(NextInt64(long.CreateTruncating(maxExclusive)));
            }

            // We can't always use a fast path for these types, but if the maxExclusive value
            // fits within a long, we can just generate a long and cast. The round-trip check
            // ensures we don't silently truncate values for types larger than ulong.
            if (typeof(T) == typeof(ulong) ||
                (typeof(T) == typeof(nuint) && nint.Size == 8) ||
                typeof(T) == typeof(Int128) ||
                typeof(T) == typeof(UInt128))
            {
                ulong maxExclusiveUlong = ulong.CreateTruncating(maxExclusive);
                if (maxExclusiveUlong <= (ulong)long.MaxValue &&
                    T.CreateTruncating(maxExclusiveUlong) == maxExclusive)
                {
                    return T.CreateTruncating(NextInt64((long)maxExclusiveUlong));
                }
            }

            // Generic fallback for large ulong, nuint, Int128, UInt128, BigInteger, etc.
            return NextBinaryIntegerRejectionSampling(maxExclusive);
        }

        /// <summary>Generic rejection sampling for arbitrary <see cref="IBinaryInteger{T}"/> types.</summary>
        private T NextBinaryIntegerRejectionSampling<T>(T maxExclusive) where T : IBinaryInteger<T>
        {
            if (maxExclusive == T.Zero)
            {
                return T.Zero;
            }

            Debug.Assert(T.IsPositive(maxExclusive));

            int bitLength = maxExclusive.GetShortestBitLength();
            int byteCount = (bitLength + 7) >> 3;

            // Compute mask for the top byte to reduce rejection rate.
            int topBits = bitLength & 7;
            byte topMask = topBits == 0 ? byte.MaxValue : (byte)((1 << topBits) - 1);

            byte[]? rented = null;
            Span<byte> bytes = byteCount <= StackallocThreshold
                ? stackalloc byte[StackallocThreshold]
                : rented = ArrayPool<byte>.Shared.Rent(byteCount);
            bytes = bytes.Slice(0, byteCount);

            try
            {
                while (true)
                {
                    NextBytes(bytes);
                    bytes[^1] &= topMask;

                    T value = T.ReadLittleEndian(bytes, isUnsigned: true);
                    if (value < maxExclusive)
                    {
                        return value;
                    }
                }
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>Handles the case where the range overflows for signed types by generating full-width random values.</summary>
        private T NextBinaryIntegerFullRange<T>(T minValue, T maxValue) where T : IBinaryInteger<T>
        {
            Debug.Assert(minValue < maxValue);

            // The range exceeds what T can represent as a positive value.
            // Generate a random value across the full range of T and check bounds.
            // Since the range > T.MaxValue, the acceptance rate is > 50%.
            int byteCount = Math.Max(minValue.GetByteCount(), maxValue.GetByteCount());

            byte[]? rented = null;
            Span<byte> bytes = byteCount <= StackallocThreshold
                ? stackalloc byte[StackallocThreshold]
                : rented = ArrayPool<byte>.Shared.Rent(byteCount);
            bytes = bytes.Slice(0, byteCount);

            try
            {
                while (true)
                {
                    NextBytes(bytes);

                    T value = T.ReadLittleEndian(bytes, isUnsigned: false);
                    if (value >= minValue && value < maxValue)
                    {
                        return value;
                    }
                }
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>Returns a random binary floating-point number of type <typeparamref name="T"/> that is greater than or equal to 0.0, and less than 1.0.</summary>
        /// <typeparam name="T">The type of floating-point number to generate.</typeparam>
        /// <returns>A binary floating-point number of type <typeparamref name="T"/> in the range [0.0, 1.0).</returns>
        public T NextBinaryFloat<T>() where T : IBinaryFloatingPointIeee754<T>
        {
            // Fast paths for common types using existing optimized implementations.
            if (typeof(T) == typeof(float))
            {
                return T.CreateTruncating(NextSingle());
            }

            if (typeof(T) == typeof(double))
            {
                return T.CreateTruncating(NextDouble());
            }

            if (typeof(T) == typeof(NFloat))
            {
                return nint.Size == 8
                    ? T.CreateTruncating(NextDouble())
                    : T.CreateTruncating(NextSingle());
            }

            // For Half, BFloat16, and other low-precision types, converting from NextSingle()
            // can round up to 1.0. Generate the value directly using the type's significand
            // bit length to guarantee the result is in [0.0, 1.0).
            int significandBitLength = T.Zero.GetSignificandBitLength();

            // For types with significand >= 63 bits, 1L << significandBitLength would overflow.
            // Build the random significand using chunks of up to 62 random bits. Since T has
            // significandBitLength bits of precision, all intermediate values are exact.
            // Note: No built-in IEEE type reaches this path (double has the largest significand
            // at 53 bits). This handles hypothetical custom IBinaryFloatingPointIeee754<T>
            // implementations with wider significands (e.g. Quad/Float128 with 113 bits).
            if (significandBitLength >= 63)
            {
                T value = T.Zero;
                int bitsRemaining = significandBitLength;
                while (bitsRemaining > 0)
                {
                    int chunk = Math.Min(bitsRemaining, 62);
                    Debug.Assert(chunk >= 1 && chunk <= 62);
                    long randomChunk = NextInt64(1L << chunk);
                    value = T.ScaleB(value, chunk) + T.CreateTruncating(randomChunk);
                    bitsRemaining -= chunk;
                }

                Debug.Assert(value >= T.Zero && value < T.ScaleB(T.One, significandBitLength));
                return T.ScaleB(value, -significandBitLength);
            }

            long randomBits = NextInt64(1L << significandBitLength);
            return T.ScaleB(T.CreateTruncating(randomBits), -significandBitLength);
        }

        /// <summary>
        ///   Fills the elements of a specified span with items chosen at random from the provided set of choices.
        /// </summary>
        /// <param name="choices">The items to use to populate the span.</param>
        /// <param name="destination">The span to be filled with items.</param>
        /// <typeparam name="T">The type of span.</typeparam>
        /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
        /// <remarks>
        ///   The method uses <see cref="Next(int)" /> to select items randomly from <paramref name="choices" />
        ///   by index and populate <paramref name="destination" />.
        /// </remarks>
        public void GetItems<T>(ReadOnlySpan<T> choices, Span<T> destination)
        {
            if (choices.IsEmpty)
            {
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(choices));
            }

            // The most expensive part of this operation is the call to get random data. If the number of
            // choices is <= 256 (which is the majority use case), we can use a single byte per element,
            // which means we can ammortize the cost of getting random data by getting random bytes in bulk.
            // However, we can only do that if this instance is Random.Shared or an instance created with
            // `new Random()`. If it was created with a seed, changing which members we call and how many
            // times may result in a visible difference in the sequence of output items. Similarly if it's
            // a derived instance, which overrides get called and when is observable.
            ImplBase impl = _impl;
            if ((impl is null || impl.GetType() == typeof(XoshiroImpl)) &&
                choices.Length <= 256)
            {
                // Get stack space to store random bytes. This size was chosen to balance between
                // stack consumed and number of random calls required.
                Span<byte> randomBytes = stackalloc byte[512];

                if (BitOperations.IsPow2(choices.Length))
                {
                    // To avoid bias, we can't just % all bytes to get them into range; that would cause
                    // the lower values to be more likely than the higher values. If the number of choices
                    // is a power of 2, though, we can just mask off the extraneous bits.

                    int mask = choices.Length - 1;

                    while (!destination.IsEmpty)
                    {
                        // If this will be the last iteration, avoid over-requesting randomness.
                        if (destination.Length < randomBytes.Length)
                        {
                            randomBytes = randomBytes.Slice(0, destination.Length);
                        }

                        NextBytes(randomBytes);

                        for (int i = 0; i < randomBytes.Length; i++)
                        {
                            destination[i] = choices[randomBytes[i] & mask];
                        }

                        destination = destination.Slice(randomBytes.Length);
                    }
                }
                else
                {
                    // As the length isn't a power of two, we can't just mask off all extraneous bits, and
                    // instead need to do rejection sampling. However, we can mask off the irrelevant bits, which
                    // then reduces the chances of needing to reject a value.

                    int mask = (int)BitOperations.RoundUpToPowerOf2((uint)choices.Length) - 1;

                    while (!destination.IsEmpty)
                    {
                        // Unlike in the IsPow2 case, where every byte will be used, some bytes here may
                        // be rejected. On average, half the bytes may be rejected, so we heuristically
                        // choose to shrink to twice the destination length.
                        if (destination.Length * 2 < randomBytes.Length)
                        {
                            randomBytes = randomBytes.Slice(0, destination.Length * 2);
                        }

                        NextBytes(randomBytes);

                        int i = 0;
                        foreach (byte b in randomBytes)
                        {
                            if ((uint)i >= (uint)destination.Length)
                            {
                                break;
                            }

                            byte masked = (byte)(b & mask);
                            if (masked < (uint)choices.Length)
                            {
                                destination[i++] = choices[masked];
                            }
                        }

                        destination = destination.Slice(i);
                    }
                }
            }
            else
            {
                // Simple fallback: get each item individually, generating a new random Int32 for each
                // item. This is slower than the above, but it works for all types and sizes of choices.
                for (int i = 0; i < destination.Length; i++)
                {
                    destination[i] = choices[Next(choices.Length)];
                }
            }
        }

        /// <summary>
        ///   Creates an array populated with items chosen at random from the provided set of choices.
        /// </summary>
        /// <param name="choices">The items to use to populate the array.</param>
        /// <param name="length">The length of array to return.</param>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <returns>An array populated with random items.</returns>
        /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="choices" /> is <see langword="null" />.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is not zero or a positive number.
        /// </exception>
        /// <remarks>
        ///   The method uses <see cref="Next(int)" /> to select items randomly from <paramref name="choices" />
        ///   by index. This is used to populate a newly-created array.
        /// </remarks>
        public T[] GetItems<T>(T[] choices, int length)
        {
            ArgumentNullException.ThrowIfNull(choices);
            return GetItems(new ReadOnlySpan<T>(choices), length);
        }

        /// <summary>
        ///   Creates an array populated with items chosen at random from the provided set of choices.
        /// </summary>
        /// <param name="choices">The items to use to populate the array.</param>
        /// <param name="length">The length of array to return.</param>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <returns>An array populated with random items.</returns>
        /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is not zero or a positive number.
        /// </exception>
        /// <remarks>
        ///   The method uses <see cref="Next(int)" /> to select items randomly from <paramref name="choices" />
        ///   by index. This is used to populate a newly-created array.
        /// </remarks>
        public T[] GetItems<T>(ReadOnlySpan<T> choices, int length)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            T[] items = new T[length];
            GetItems(choices, items.AsSpan());
            return items;
        }

        /// <summary>
        ///   Performs an in-place shuffle of an array.
        /// </summary>
        /// <param name="values">The array to shuffle.</param>
        /// <typeparam name="T">The type of array.</typeparam>
        /// <exception cref="ArgumentNullException"><paramref name="values" /> is <see langword="null" />.</exception>
        /// <remarks>
        ///   This method uses <see cref="Next(int, int)" /> to choose values for shuffling.
        ///   This method is an O(n) operation.
        /// </remarks>
        public void Shuffle<T>(T[] values)
        {
            ArgumentNullException.ThrowIfNull(values);
            // this can't use AsSpan due to array covariance
            // forcing it like this is safe due to everything being in the array already
            Shuffle(new Span<T>(ref MemoryMarshal.GetArrayDataReference(values), values.Length));
        }

        /// <summary>
        ///   Performs an in-place shuffle of a span.
        /// </summary>
        /// <param name="values">The span to shuffle.</param>
        /// <typeparam name="T">The type of span.</typeparam>
        /// <remarks>
        ///   This method uses <see cref="Next(int, int)" /> to choose values for shuffling.
        ///   This method is an O(n) operation.
        /// </remarks>
        public void Shuffle<T>(Span<T> values)
        {
            for (int i = 0; i < values.Length - 1; i++)
            {
                int j = Next(i, values.Length);

                // The i != j check is excluded intentionally.
                // Microbenchmarks show that the mispredicted branches cost more than the redundant read/write for small value types.
                // Since large value types are uncommon in shuffle scenarios, the trade-off favors removing the branch.
                T temp = values[i];
                values[i] = values[j];
                values[j] = temp;
            }
        }

        /// <summary>Creates a string populated with characters chosen at random from <paramref name="choices"/>.</summary>
        /// <param name="choices">The characters to use to populate the string.</param>
        /// <param name="length">The length of string to return.</param>
        /// <returns>A string populated with items selected at random from <paramref name="choices"/>.</returns>
        /// <exception cref="ArgumentException"><paramref name="choices" /> is empty.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> is not zero or a positive number.</exception>
        /// <seealso cref="GetItems{T}(ReadOnlySpan{T}, Span{T})" />
        public string GetString(ReadOnlySpan<char> choices, int length)
        {
            if (choices.IsEmpty)
            {
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(choices));
            }

            if (length <= 0)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(length);
                return string.Empty;
            }

            string destination = string.FastAllocateString(length);
            GetItems(choices, new Span<char>(ref destination.GetRawStringData(), destination.Length));
            return destination;
        }

        /// <summary>Creates a string filled with random hexadecimal characters.</summary>
        /// <param name="stringLength">The length of string to create.</param>
        /// <param name="lowercase">
        /// <see langword="true" /> if the hexadecimal characters should be lowercase; <see langword="false" /> if they should be uppercase.
        /// The default is <see langword="false" />.
        /// </param>
        /// <returns>A string populated with random hexadecimal characters.</returns>
        public string GetHexString(int stringLength, bool lowercase = false) =>
            GetString(GetHexChoices(lowercase), stringLength);

        /// <summary>Fills a buffer with random hexadecimal characters.</summary>
        /// <param name="destination">The buffer to receive the characters.</param>
        /// <param name="lowercase">
        /// <see langword="true" /> if the hexadecimal characters should be lowercase; <see langword="false" /> if they should be uppercase.
        /// The default is <see langword="false" />.
        /// </param>
        public void GetHexString(Span<char> destination, bool lowercase = false) =>
            GetItems(GetHexChoices(lowercase), destination);

        /// <summary>Gets all possible hex characters for the specified casing.</summary>
        private static ReadOnlySpan<char> GetHexChoices(bool lowercase) =>
            lowercase ? "0123456789abcdef" : "0123456789ABCDEF";

        /// <summary>Returns a random floating-point number between 0.0 and 1.0.</summary>
        /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
        protected virtual double Sample()
        {
            double result = _impl.Sample();
            AssertInRange(result);
            return result;
        }

        private static void ThrowMinMaxValueSwapped() =>
            throw new ArgumentOutOfRangeException("minValue", SR.Format(SR.Argument_MinMaxValue, "minValue", "maxValue"));

        [Conditional("DEBUG")]
        private static void AssertInRange(long result, long minInclusive, long maxExclusive)
        {
            if (maxExclusive > minInclusive)
            {
                Debug.Assert(result >= minInclusive && result < maxExclusive, $"Expected {minInclusive} <= {result} < {maxExclusive}");
            }
            else
            {
                Debug.Assert(result == minInclusive, $"Expected {minInclusive} == {result}");
            }
        }

        [Conditional("DEBUG")]
        private static void AssertInRange(double result)
        {
            Debug.Assert(result >= 0.0 && result < 1.0, $"Expected 0.0 <= {result} < 1.0");
        }

        /// <summary>Random implementation that delegates all calls to a ThreadStatic Impl instance.</summary>
        private sealed class ThreadSafeRandom : Random
        {
            // We need Random.Shared to return an instance that is thread-safe, as it may be used from multiple threads.
            // It's also possible that one thread could retrieve Shared and pass it to another thread, so Shared can't
            // just access a ThreadStatic to return a Random instance stored there.  As such, we need the instance
            // returned from Shared itself to be thread-safe, which can be accomplished either by locking around all
            // accesses or by itself accessing a ThreadStatic on every access: we've chosen the latter, as it is more
            // scalable.  With that, we have two basic approaches:
            // 1. the instance returned can be a base Random instance constructed with an _impl that is a ThreadSafeImpl.
            // 2. the instance returned can be a special Random-derived instance, where _impl is ignored and the derived
            //    type overrides all methods on the base.
            // (1) is cleaner, as (2) requires duplicating a bit more code, but (2) enables all virtual dispatch to be
            // devirtualized and potentially inlined, so that Random.Shared.NextXx(...) ends up being faster.
            // This implements (2).

            [ThreadStatic]
            private static XoshiroImpl? t_random;

            public ThreadSafeRandom() : base(isThreadSafeRandom: true) { }

            private static XoshiroImpl LocalRandom => t_random ?? Create();

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static XoshiroImpl Create() => t_random = new();

            public override int Next()
            {
                int result = LocalRandom.Next();
                AssertInRange(result, 0, int.MaxValue);
                return result;
            }

            public override int Next(int maxValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

                int result = LocalRandom.Next(maxValue);
                AssertInRange(result, 0, maxValue);
                return result;
            }

            public override int Next(int minValue, int maxValue)
            {
                if (minValue > maxValue)
                {
                    ThrowMinMaxValueSwapped();
                }

                int result = LocalRandom.Next(minValue, maxValue);
                AssertInRange(result, minValue, maxValue);
                return result;
            }

            public override long NextInt64()
            {
                long result = LocalRandom.NextInt64();
                AssertInRange(result, 0, long.MaxValue);
                return result;
            }

            public override long NextInt64(long maxValue)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(maxValue);

                long result = LocalRandom.NextInt64(maxValue);
                AssertInRange(result, 0, maxValue);
                return result;
            }

            public override long NextInt64(long minValue, long maxValue)
            {
                if (minValue > maxValue)
                {
                    ThrowMinMaxValueSwapped();
                }

                long result = LocalRandom.NextInt64(minValue, maxValue);
                AssertInRange(result, minValue, maxValue);
                return result;
            }

            public override float NextSingle()
            {
                float result = LocalRandom.NextSingle();
                AssertInRange(result);
                return result;
            }

            public override double NextDouble()
            {
                double result = LocalRandom.NextDouble();
                AssertInRange(result);
                return result;
            }

            public override void NextBytes(byte[] buffer)
            {
                if (buffer is null)
                {
                    ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
                }

                LocalRandom.NextBytes(buffer);
            }

            public override void NextBytes(Span<byte> buffer) => LocalRandom.NextBytes(buffer);

            protected override double Sample() => throw new NotSupportedException();
        }
    }
}
