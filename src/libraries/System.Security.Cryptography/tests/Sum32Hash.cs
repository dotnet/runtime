// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Hashing.Tests
{
    internal class Sum32Hash : HashAlgorithm
    {
        private uint _sum;

        public Sum32Hash()
        {
            HashSizeValue = sizeof(uint);
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
