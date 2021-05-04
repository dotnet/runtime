// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Internal.Cryptography.Pal
{
    internal sealed class UnsupportedDisallowedStore : IStorePal
    {
        private readonly bool _readOnly;

        internal UnsupportedDisallowedStore(OpenFlags openFlags)
        {
            // ReadOnly is 0x00, so it is implicit unless either ReadWrite or MaxAllowed
            // was requested.
            OpenFlags writeFlags = openFlags & (OpenFlags.ReadWrite | OpenFlags.MaxAllowed);

            if (writeFlags == OpenFlags.ReadOnly)
            {
                _readOnly = true;
            }
        }

        public void Dispose()
        {
            // Nothing to do.
        }

        public void CloneTo(X509Certificate2Collection collection)
        {
            // Never show any data.
        }

        public void Add(ICertificatePal cert)
        {
            if (_readOnly)
            {
                throw new CryptographicException(SR.Cryptography_X509_StoreReadOnly);
            }

            throw new CryptographicException(
                SR.Cryptography_Unix_X509_NoDisallowedStore,
                new PlatformNotSupportedException(SR.Cryptography_Unix_X509_NoDisallowedStore));
        }

        public void Remove(ICertificatePal cert)
        {
            // Remove never throws if it does no measurable work.
            // Since CloneTo always says the store is empty, no measurable work is ever done.
        }

        SafeHandle? IStorePal.SafeHandle { get; }
    }
}
