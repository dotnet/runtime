// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography
{
    internal sealed partial class RandomNumberGeneratorImplementation : RandomNumberGenerator
    {
        // a singleton which always calls into a thread-safe implementation
        // and whose Dispose method no-ops
        internal static readonly RandomNumberGeneratorImplementation s_singleton = new RandomNumberGeneratorImplementation();

        // private ctor used only by singleton
        private RandomNumberGeneratorImplementation()
        {
        }

        // As long as each implementation can provide a static GetBytes(ref byte buf, int length)
        // they can share this one implementation of FillSpan.
        internal static unsafe void FillSpan(Span<byte> data)
        {
            if (data.Length > 0)
            {
                fixed (byte* ptr = data) GetBytes(ptr, data.Length);
            }
        }

        public override void GetBytes(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            GetBytes(new Span<byte>(data));
        }

        public override void GetBytes(byte[] data, int offset, int count)
        {
            VerifyGetBytes(data, offset, count);
            GetBytes(new Span<byte>(data, offset, count));
        }

        public override unsafe void GetBytes(Span<byte> data)
        {
            if (data.Length > 0)
            {
                fixed (byte* ptr = data) GetBytes(ptr, data.Length);
            }
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            GetNonZeroBytes(new Span<byte>(data));
        }

        public override void GetNonZeroBytes(Span<byte> data)
        {
            while (data.Length > 0)
            {
                // Fill the remaining portion of the span with random bytes.
                GetBytes(data);

                // Find the first zero in the remaining portion.
                int indexOfFirst0Byte = data.Length;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0)
                    {
                        indexOfFirst0Byte = i;
                        break;
                    }
                }

                // If there were any zeros, shift down all non-zeros.
                for (int i = indexOfFirst0Byte + 1; i < data.Length; i++)
                {
                    if (data[i] != 0)
                    {
                        data[indexOfFirst0Byte++] = data[i];
                    }
                }

                // Request new random bytes if necessary; dont re-use
                // existing bytes since they were shifted down.
                data = data.Slice(indexOfFirst0Byte);
            }
        }
    }
}
