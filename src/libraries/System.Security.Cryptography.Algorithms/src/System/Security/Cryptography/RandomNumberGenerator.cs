// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;

namespace System.Security.Cryptography
{
    public abstract class RandomNumberGenerator : IDisposable
    {
        protected RandomNumberGenerator() { }

        public static RandomNumberGenerator Create()
        {
            return new RandomNumberGeneratorImplementation();
        }

        public static RandomNumberGenerator Create(string rngName)
        {
            return (RandomNumberGenerator)CryptoConfig.CreateFromName(rngName);
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

        internal void VerifyGetBytes(byte[] data, int offset, int count)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (offset < 0) throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count > data.Length - offset) throw new ArgumentException(SR.Argument_InvalidOffLen);
        }
    }
}
