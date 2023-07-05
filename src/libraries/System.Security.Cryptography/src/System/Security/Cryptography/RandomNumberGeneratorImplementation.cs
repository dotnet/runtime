// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            FillNonZeroBytes(data);
        }

        public override void GetNonZeroBytes(Span<byte> data)
        {
            FillNonZeroBytes(data);
        }

        internal static void FillNonZeroBytes(Span<byte> data)
        {
            while (data.Length > 0)
            {
                // Fill the remaining portion of the span with random bytes.
                FillSpan(data);

                // Find the first zero in the remaining portion. If there isn't any, we're all done.
                int first0Byte = data.IndexOf((byte)0);
                if (first0Byte < 0)
                {
                    return;
                }

                // There's at least one zero.  Shift down all non-zeros.
                int zerosFound = 1;
                Span<byte> remainder = data.Slice(first0Byte + 1);
                while (true)
                {
                    // Find the next zero.
                    int next0Byte = remainder.IndexOf((byte)0);
                    if (next0Byte < 0)
                    {
                        // There weren't any more zeros. Copy the remaining valid data down.
                        remainder.CopyTo(data.Slice(first0Byte));
                        break;
                    }

                    // Copy down until the next zero, then reset to continue copying from there.
                    remainder.Slice(0, next0Byte).CopyTo(data.Slice(first0Byte));
                    zerosFound++;
                    first0Byte += next0Byte;
                    remainder = remainder.Slice(next0Byte + 1);
                }

                // Slice to any remaining space that needs to be filled. This is equal to the
                // number of zeros found.
                data = data.Slice(data.Length - zerosFound);
            }
        }
    }
}
