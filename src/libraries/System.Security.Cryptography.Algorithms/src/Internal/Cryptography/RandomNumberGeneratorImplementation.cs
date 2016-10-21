// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class RandomNumberGeneratorImplementation : RandomNumberGenerator
    {
        public override void GetBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            GetBytesInternal(data, 0, data.Length);
        }

        public override void GetBytes(byte[] data, int offset, int count)
        {
            VerifyGetBytes(data, offset, count);
            GetBytesInternal(data, offset, count);
        }

        public override void GetNonZeroBytes(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            int offset = 0;
            while (offset < data.Length)
            {
                // Fill the remaining portion of the array with random bytes.
                GetBytesInternal(data, offset, data.Length - offset);

                // Find the first zero in the remaining portion.
                int indexOfFirst0Byte = data.Length;
                for (int i = offset; i < data.Length; i++)
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
                offset = indexOfFirst0Byte;
            }
        }

        private void GetBytesInternal(byte[] data, int offset, int count)
        {
            Debug.Assert(data != null);
            Debug.Assert(offset >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(count <= data.Length - offset);

            if (count > 0)
            {
                unsafe
                {
                    fixed (byte* pb = &data[offset])
                    {
                        GetBytes(pb, count);
                    }
                }
            }
        }
    }
}
