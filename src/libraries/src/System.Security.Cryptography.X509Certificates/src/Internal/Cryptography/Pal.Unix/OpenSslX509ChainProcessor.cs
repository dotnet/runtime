// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal sealed class OpenSslX509ChainProcessor : IChainPal
    {
        public void Dispose()
        {
        }

        public bool? Verify(X509VerificationFlags flags, out Exception exception)
        {
            exception = new NotImplementedException();
            return null;
        }

        public X509ChainElement[] ChainElements
        {
            get { throw new NotImplementedException(); }
        }

        public X509ChainStatus[] ChainStatus
        {
            get { throw new NotImplementedException(); }
        }

        public SafeX509ChainHandle SafeHandle
        {
            get { return null; }
        }
    }
}
