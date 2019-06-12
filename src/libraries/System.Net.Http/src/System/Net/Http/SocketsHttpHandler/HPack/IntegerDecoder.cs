// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0.
// See THIRD-PARTY-NOTICES.TXT in the project root for license information.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal class IntegerDecoder
    {
        private int _i;
        private int _m;

        public int Value { get; private set; }

        /// <summary>
        /// Decodes the first byte of the integer.
        /// </summary>
        /// <param name="prefixLength">The length of the prefix, in bits, that the integer was encoded with. Must be between 1 and 8.</param>
        /// <returns>If the integer has been fully decoded, true. Otherwise, false -- <see cref="Decode(byte)"/> must be called on subsequent bytes.</returns>
        public bool StartDecode(byte b, int prefixLength)
        {
            Debug.Assert(prefixLength >= 1 && prefixLength <= 8);

            if (b < ((1 << prefixLength) - 1))
            {
                Value = b;
                return true;
            }
            else
            {
                _i = b;
                _m = 0;
                return false;
            }
        }

        /// <summary>
        /// Decodes subsequent bytes of an integer.
        /// </summary>
        /// <returns>If the integer has been fully decoded, true. Otherwise, false -- <see cref="Decode(byte)"/> must be called on subsequent bytes.</returns>
        public bool Decode(byte b)
        {
            _i = _i + (b & 127) * (1 << _m);
            _m = _m + 7;

            if ((b & 128) != 128)
            {
                Value = _i;
                return true;
            }

            return false;
        }
    }
}
