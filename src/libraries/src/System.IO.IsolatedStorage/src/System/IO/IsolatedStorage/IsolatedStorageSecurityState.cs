// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.IsolatedStorage
{
    public class IsolatedStorageSecurityState
    {
        public IsolatedStorageSecurityOptions Options
        {
            get
            {
                return IsolatedStorageSecurityOptions.IncreaseQuotaForApplication;
            }
        }

        public long Quota
        {
            get
            {
                return long.MaxValue;
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
        }

        public long UsedSize
        {
            get
            {
                return default(long);
            }
        }

        public bool IsStateAvailable()
        {
            // We only have one state
            return true;
        }

        public virtual void EnsureState()
        {
            // Nothing to do
        }
    }
}
