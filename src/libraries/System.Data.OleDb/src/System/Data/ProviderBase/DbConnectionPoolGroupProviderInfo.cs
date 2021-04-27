// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Data.ProviderBase
{
    internal class DbConnectionPoolGroupProviderInfo
    {
        private DbConnectionPoolGroup? _poolGroup;

        internal DbConnectionPoolGroup? PoolGroup
        {
            get
            {
                return _poolGroup;
            }
            set
            {
                _poolGroup = value;
            }
        }
    }
}
