// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                throw new ArgumentOutOfRangeException("ttl");
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
                    throw new ArgumentOutOfRangeException("value");
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
