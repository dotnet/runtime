// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator() { }

        public static RandomNumberGenerator Create() => RandomNumberGeneratorImplementation.s_singleton;

        [UnsupportedOSPlatform("browser")]
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

            Span<uint> resultSpan = stackalloc uint[1];
            uint result;

            do
            {
                RandomNumberGeneratorImplementation.FillSpan(MemoryMarshal.AsBytes(resultSpan));
                result = mask & resultSpan[0];
            }
            while (result > range);

            return (int)result + fromInclusive;
        }

        public static int GetInt32(int toExclusive)
        {
            if (toExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(toExclusive), SR.ArgumentOutOfRange_NeedPosNum);

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
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);

            byte[] ret = new byte[count];
            RandomNumberGeneratorImplementation.FillSpan(ret);
            return ret;
        }

        internal void VerifyGetBytes(byte[] data, int offset, int count)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count > data.Length - offset)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
        }
    }
}
