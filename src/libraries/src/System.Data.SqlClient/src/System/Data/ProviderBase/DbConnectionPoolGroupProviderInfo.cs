// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------


namespace System.Data.ProviderBase
{
    internal class DbConnectionPoolGroupProviderInfo
    {
        private DbConnectionPoolGroup _poolGroup;

        internal DbConnectionPoolGroup PoolGroup
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
