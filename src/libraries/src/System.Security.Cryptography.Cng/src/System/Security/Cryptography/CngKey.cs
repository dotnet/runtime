// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        private CngKey(SafeNCryptProviderHandle providerHandle, SafeNCryptKeyHandle keyHandle)
        {
            Debug.Assert(keyHandle != null && !keyHandle.IsInvalid && !keyHandle.IsClosed);
            Debug.Assert(providerHandle != null && !providerHandle.IsInvalid && !providerHandle.IsClosed);

            _providerHandle = providerHandle;
            _keyHandle = keyHandle;
        }

        public void Dispose()
        {
            if (_providerHandle != null)
            {
                _providerHandle.Dispose();
            }

            if (_keyHandle != null)
            {
                _keyHandle.Dispose();
            }
        }

        private readonly SafeNCryptKeyHandle _keyHandle;
        private readonly SafeNCryptProviderHandle _providerHandle;
    }
}

