// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


//------------------------------------------------------------------------------


namespace System.Data.ProviderBase
{
    internal sealed class DbConnectionPoolGroupOptions
    {
        private readonly bool _poolByIdentity;
        private readonly int _minPoolSize;
        private readonly int _maxPoolSize;
        private readonly int _creationTimeout;
        private readonly TimeSpan _loadBalanceTimeout;
        private readonly bool _useLoadBalancing;

        public DbConnectionPoolGroupOptions(
                                        bool poolByIdentity,
                                        int minPoolSize,
                                        int maxPoolSize,
                                        int creationTimeout,
                                        int loadBalanceTimeout
        )
        {
            _poolByIdentity = poolByIdentity;
            _minPoolSize = minPoolSize;
            _maxPoolSize = maxPoolSize;
            _creationTimeout = creationTimeout;

            if (0 != loadBalanceTimeout)
            {
                _loadBalanceTimeout = new TimeSpan(0, 0, loadBalanceTimeout);
                _useLoadBalancing = true;
            }
        }

        public int CreationTimeout
        {
            get { return _creationTimeout; }
        }
        public TimeSpan LoadBalanceTimeout
        {
            get { return _loadBalanceTimeout; }
        }
        public int MaxPoolSize
        {
            get { return _maxPoolSize; }
        }
        public int MinPoolSize
        {
            get { return _minPoolSize; }
        }
        public bool PoolByIdentity
        {
            get { return _poolByIdentity; }
        }
        public bool UseLoadBalancing
        {
            get { return _useLoadBalancing; }
        }
    }
}


