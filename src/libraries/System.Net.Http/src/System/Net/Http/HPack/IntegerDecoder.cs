// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Http.HPack
{
    internal class IntegerDecoder
    {
        private int _i;
        private int _m;

        public int Value { get; private set; }

        public bool StartDecode(byte b, int prefixLength)
        {
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
