// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator() { }

        public static RandomNumberGenerator Create() => RandomNumberGeneratorImplementation.s_singleton;

        [Obsolete(Obsoletions.CryptoStringFactoryMessage, DiagnosticId = Obsoletions.CryptoStringFactoryDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [RequiresUnreferencedCode(CryptoConfig.CreateFromNameUnreferencedCodeMessage)]
        public static RandomNumberGenerator? Create(string rngName)
        {
            return (RandomNumberGenerator?)CryptoConfig.CreateFromName(rngName);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            return;
        }

        protected virtual void Dispose(bool disposing) { }

        public abstract void GetBytes(byte[] data);

        public virtual void GetBytes(byte[] data, int offset, int count)
        {
            VerifyGetBytes(data, offset, count);
            if (count > 0)
            {
                if (offset == 0 && count == data.Length)
                {
                    GetBytes(data);
                }
                else
                {
                    // For compat we can't avoid an alloc here since we must call GetBytes(data).
                    // However RandomNumberGeneratorImplementation avoids extra allocs.
                    var tempData = new byte[count];
                    GetBytes(tempData);
                    Buffer.BlockCopy(tempData, 0, data, offset, count);
                }
            }
        }

        public virtual void GetBytes(Span<byte> data)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                GetBytes(array, 0, data.Length);
                new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
            }
            finally
            {
                Array.Clear(array, 0, data.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public virtual void GetNonZeroBytes(byte[] data)
        {
            // For compatibility we cannot have it be abstract. Since this technically is an abstract method
            // with no implementation, we'll just throw NotImplementedException.
            throw new NotImplementedException();
        }

        public virtual void GetNonZeroBytes(Span<byte> data)
        {
            // Use ArrayPool.Shared instead of CryptoPool because the array is passed out.
            byte[] array = ArrayPool<byte>.Shared.Rent(data.Length);
            try
            {
                // NOTE: There is no GetNonZeroBytes(byte[], int, int) overload, so this call
                // may end up retrieving more data than was intended, if the array pool
                // gives back a larger array than was actually needed.
                GetNonZeroBytes(array);
                new ReadOnlySpan<byte>(array, 0, data.Length).CopyTo(data);
            }
            finally
            {
                Array.Clear(array, 0, data.Length);
                ArrayPool<byte>.Shared.Return(array);
            }
        }

        public static void Fill(Span<byte> data)
        {
            RandomNumberGeneratorImplementation.FillSpan(data);
        }

        public static int GetInt32(int fromInclusive, int toExclusive)
        {
            if (fromInclusive >= toExclusive)
                throw new ArgumentException(SR.Argument_InvalidRandomRange);

            // The total possible range is [0, 4,294,967,295).
            // Subtract one to account for zero being an actual possibility.
            uint range = (uint)toExclusive - (uint)fromInclusive - 1;

            // If there is only one possible choice, nothing random will actually happen, so return
            // the only possibility.
            if (range == 0)
            {
                return fromInclusive;
            }

            // Create a mask for the bits that we care about for the range. The other bits will be
            // masked away.
            uint mask = range;
            mask |= mask >> 1;
            mask |= mask >> 2;
            mask |= mask >> 4;
            mask |= mask >> 8;
            mask |= mask >> 16;

            uint oneUint = 0;
            Span<byte> oneUintBytes = MemoryMarshal.AsBytes(new Span<uint>(ref oneUint));
            uint result;

            do
            {
                RandomNumberGeneratorImplementation.FillSpan(oneUintBytes);
                result = mask & oneUint;
            }
            while (result > range);

            return (int)result + fromInclusive;
        }

        public static int GetInt32(int toExclusive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(toExclusive);

            return GetInt32(0, toExclusive);
        }

        /// <summary>
        /// Creates an array of bytes with a cryptographically strong random sequence of values.
        /// </summary>
        /// <param name="count">The number of bytes of random values to create.</param>
        /// <returns>
        /// An array populated with cryptographically strong random values.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="count" /> is less than zero.
        /// </exception>
        public static byte[] GetBytes(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);

            byte[] ret = new byte[count];
            RandomNumberGeneratorImplementation.FillSpan(ret);
            return ret;
        }

        /// <summary>
        ///   Fills the elements of a specified span with items chosen at random from the provided set of choices.
        /// </summary>
        /// <param name="choices">The items to use to fill the buffer.</param>
        /// <param name="destination">The buffer to receive the items.</param>
        /// <typeparam name="T">The type of items.</typeparam>
        /// <exception cref="ArgumentException">
        ///   <paramref name="choices" /> is empty.
        /// </exception>
        /// <seealso cref="GetString" />
        /// <seealso cref="GetHexString(Span{char}, bool)" />
        public static void GetItems<T>(ReadOnlySpan<T> choices, Span<T> destination)
        {
            if (choices.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(choices));

            GetItemsCore<T>(choices, destination);
        }

        /// <summary>
        ///   Creates an array populated with items chosen at random from choices.
        /// </summary>
        /// <param name="choices">The items to use to populate the array.</param>
        /// <param name="length">The length of array to return populated with items.</param>
        /// <returns>An array populated with random choices.</returns>
        /// <typeparam name="T">The type of items.</typeparam>
        /// <exception cref="ArgumentException">
        ///   <paramref name="choices" /> is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is not zero or a positive number.
        /// </exception>
        /// <seealso cref="GetString" />
        /// <seealso cref="GetHexString(Span{char}, bool)" />
        public static T[] GetItems<T>(ReadOnlySpan<T> choices, int length)
        {
            if (choices.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(choices));

            ArgumentOutOfRangeException.ThrowIfNegative(length);

            T[] result = new T[length];
            GetItemsCore<T>(choices, result);
            return result;
        }

        /// <summary>
        ///   Creates a string populated with characters chosen at random from choices.
        /// </summary>
        /// <param name="choices">The characters to use to populate the string.</param>
        /// <param name="length">The length of string to return.</param>
        /// <returns>A string populated with random choices.</returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="choices" /> is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="length" /> is not zero or a positive number.
        /// </exception>
        /// <seealso cref="GetItems{T}(ReadOnlySpan{T}, Span{T})" />
        /// <seealso cref="GetItems{T}(ReadOnlySpan{T}, int)" />
        /// <seealso cref="GetHexString(Span{char}, bool)" />
        public static unsafe string GetString(ReadOnlySpan<char> choices, int length)
        {
            if (choices.IsEmpty)
                throw new ArgumentException(SR.Arg_EmptySpan, nameof(choices));

            ArgumentOutOfRangeException.ThrowIfNegative(length);

#pragma warning disable 8500
            return string.Create(length,
                (IntPtr)(&choices),
                static (destination, state) =>
                {
                    GetItemsCore(*(ReadOnlySpan<char>*)state, destination);
                });
#pragma warning restore 8500
        }

        /// <summary>
        ///   Fills a buffer with cryptographically random hexadecimal characters.
        /// </summary>
        /// <param name="destination">The buffer to receive the characters.</param>
        /// <param name="lowercase">
        ///   <see langword="true" /> if the hexadecimal characters should be lowercase; <see langword="false" /> if they should be uppercase.
        ///   The default is <see langword="false" />.
        /// </param>
        /// <remarks>
        ///   The behavior of this is the same as using <seealso cref="GetItems{T}(ReadOnlySpan{T}, Span{T})" /> and
        ///   specifying hexadecimal characters as the choices. This implementation is optimized specifically for
        ///   hexadecimal characters.
        /// </remarks>
        public static void GetHexString(Span<char> destination, bool lowercase = false)
        {
            if (destination.IsEmpty)
                return;

            GetHexStringCore(destination, lowercase);
        }

        /// <summary>
        ///   Creates a string filled with cryptographically random hexadecimal characters.
        /// </summary>
        /// <param name="stringLength">The length of string to create.</param>
        /// <param name="lowercase">
        ///   <see langword="true" /> if the hexadecimal characters should be lowercase; <see langword="false" /> if they should be uppercase.
        ///   The default is <see langword="false" />.
        /// </param>
        /// <returns>A string populated with random hexadecimal characters.</returns>
        /// <remarks>
        ///   The behavior of this is the same as using <seealso cref="GetString" /> and
        ///   specifying hexadecimal characters as the choices. This implementation is optimized specifically for
        ///   hexadecimal characters.
        /// </remarks>
        public static string GetHexString(int stringLength, bool lowercase = false)
        {
            if (stringLength == 0)
                return string.Empty;

            return string.Create(stringLength, lowercase, GetHexStringCore);
        }

        /// <summary>
        ///   Performs an in-place shuffle of a span using cryptographically random number generation.
        /// </summary>
        /// <param name="values">The span to shuffle.</param>
        /// <typeparam name="T">The type of span.</typeparam>
        public static void Shuffle<T>(Span<T> values)
        {
            int n = values.Length;

            for (int i = 0; i < n - 1; i++)
            {
                int j = GetInt32(i, n);

                if (i != j)
                {
                    T temp = values[i];
                    values[i] = values[j];
                    values[j] = temp;
                }
            }
        }

        private static void GetHexStringCore(Span<char> destination, bool lowercase)
        {
            Debug.Assert(!destination.IsEmpty);

            const int RandomBufferSize = 64; // If this changes, the tests need to be updated since they try to exercise boundary conditions.
            Span<byte> randomBuffer = stackalloc byte[RandomBufferSize];
            HexConverter.Casing casing = lowercase ? HexConverter.Casing.Lower : HexConverter.Casing.Upper;

            // Don't overfill the buffer if the destination is smaller than the buffer size. We need to round up when
            // when dividing by two to account for an odd-length destination.
            int needed = (destination.Length + 1) / 2;
            Span<byte> remainingRandom = randomBuffer.Slice(0, Math.Min(RandomBufferSize, needed));
            RandomNumberGenerator.Fill(remainingRandom);

            // HexConverter can only write in multiples of two. If the length is odd, get back to an even length.
            if (destination.Length % 2 != 0)
            {
                destination[0] = lowercase ?
                    HexConverter.ToCharLower(remainingRandom[0]) :
                    HexConverter.ToCharUpper(remainingRandom[0]);

                destination = destination.Slice(1);
                remainingRandom = remainingRandom.Slice(1);
            }

            while (!destination.IsEmpty)
            {
                needed = destination.Length / 2;

                if (remainingRandom.IsEmpty)
                {
                    remainingRandom = randomBuffer.Slice(0, Math.Min(RandomBufferSize, needed));
                    RandomNumberGenerator.Fill(remainingRandom);
                }

                HexConverter.EncodeToUtf16(remainingRandom, destination, casing);
                destination = destination.Slice(remainingRandom.Length * 2);
                remainingRandom = default;
            }
        }

        private static void GetItemsCore<T>(ReadOnlySpan<T> choices, Span<T> destination)
        {
            // The most expensive part of this operation is the call to get random data. We can
            // do so potentially many fewer times if:
            // - the number of choices is <= 256. This let's us get a single byte per choice.
            // - the number of choices is a power of two. This let's us use a byte and simply mask off
            //   unnecessary bits cheaply rather than needing to use rejection sampling.
            // In such a case, we can grab a bunch of random bytes in one call.
            if (BitOperations.IsPow2(choices.Length) && choices.Length <= 256)
            {
                // Get stack space to store random bytes. This size was chosen to balance between
                // stack consumed and number of random calls required.
                Span<byte> randomBytes = stackalloc byte[512];

                while (!destination.IsEmpty)
                {
                    if (destination.Length < randomBytes.Length)
                    {
                        randomBytes = randomBytes.Slice(0, destination.Length);
                    }

                    RandomNumberGeneratorImplementation.FillSpan(randomBytes);

                    int mask = choices.Length - 1;
                    for (int i = 0; i < randomBytes.Length; i++)
                    {
                        destination[i] = choices[randomBytes[i] & mask];
                    }

                    destination = destination.Slice(randomBytes.Length);
                }

                return;
            }

            // Simple fallback: get each item individually, generating a new random Int32 for each
            // item. This is slower than the above, but it works for all types and sizes of choices.
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = choices[GetInt32(choices.Length)];
            }
        }

        internal static void VerifyGetBytes(byte[] data, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(data);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (count > data.Length - offset)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }
    }
}
