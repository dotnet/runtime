// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

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
        /// <summary>
        ///     Delete this key
        /// </summary>
        public void Delete()
        {
            ErrorCode errorCode = Interop.NCrypt.NCryptDeleteKey(_keyHandle, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();
            _keyHandle.SetHandleAsInvalid();

            // Once the key is deleted, the handles are no longer valid so dispose of this instance
            Dispose();
        }
    }
}

