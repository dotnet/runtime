// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Hashing.Tests
{
    internal class Sum32Hash : HashAlgorithm
    {
        private uint _sum;

        public override int HashSize
        {
            get { return sizeof(uint); }
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            unchecked
            {
                for (int i = 0; i < cbSize; i++)
                {
                    _sum += array[ibStart + i];
                }
            }
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_sum);
        }

        public override void Initialize()
        {
            _sum = 0;
        }
    }
}
