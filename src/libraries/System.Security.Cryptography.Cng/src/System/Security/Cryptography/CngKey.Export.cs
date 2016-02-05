// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        ///     Export the key out of the KSP
        /// </summary>
        public byte[] Export(CngKeyBlobFormat format)
        {
            if (format == null)
                throw new ArgumentNullException(nameof(format));

            int numBytesNeeded;
            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, null, 0, out numBytesNeeded, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            byte[] buffer = new byte[numBytesNeeded];
            errorCode = Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, buffer, buffer.Length, out numBytesNeeded, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            Array.Resize(ref buffer, numBytesNeeded);
            return buffer;
        }
    }
}

