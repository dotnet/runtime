// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.NetworkInformation
{
    public class PingOptions
    {
        private int _ttl;
        private bool _dontFragment;

        public PingOptions()
        {
            _ttl = 128;
        }

        public PingOptions(int ttl, bool dontFragment)
        {
            if (ttl <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl));
            }

            _ttl = ttl;
            _dontFragment = dontFragment;
        }

        public int Ttl
        {
            get
            {
                return _ttl;
            }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                // Useful to discover routes.
                _ttl = value;
            }
        }

        public bool DontFragment
        {
            get
            {
                return _dontFragment;
            }
            set
            {
                // Useful for discovering MTU.
                _dontFragment = value;
            }
        }
    }
}
