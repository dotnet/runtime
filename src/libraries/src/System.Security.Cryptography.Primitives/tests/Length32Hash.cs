// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.Hashing.Tests
{
    internal class Length32Hash : HashAlgorithm
    {
        private uint _length;

#if netstandard17
        public Length32Hash()
        {
            HashSizeValue = sizeof(uint);
        }
#else
        public override int HashSize
        {
            get { return sizeof(uint); }
        }
#endif

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
