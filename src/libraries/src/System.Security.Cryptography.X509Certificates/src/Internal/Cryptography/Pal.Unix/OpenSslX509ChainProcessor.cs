using System;
using System.Collections.Generic;
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

        public IEnumerable<X509ChainElement> ChainElements
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
