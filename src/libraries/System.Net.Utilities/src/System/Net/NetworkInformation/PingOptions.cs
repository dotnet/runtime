// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net.NetworkInformation
{
    // Represent the possible IP options used for the ICMP packet.
    public class PingOptions
    {
        private const int DontFragmentFlag = 2;
        private int _ttl = 128;
        private bool _dontFragment;

        internal PingOptions(Interop.IpHlpApi.IPOptions options)
        {
            _ttl = options.ttl;
            _dontFragment = ((options.flags & DontFragmentFlag) > 0 ? true : false);
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

        public PingOptions()
        {
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
