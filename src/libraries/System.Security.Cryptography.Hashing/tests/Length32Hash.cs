// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Security.Cryptography.Hashing.Tests
{
    internal class Length32Hash : HashAlgorithm
    {
        private uint _length;

        public override int HashSize
        {
            get { return sizeof(uint); }
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _length += (uint)cbSize;
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(_length);
        }

        public override void Initialize()
        {
            _length = 0;
        }
    }
}
