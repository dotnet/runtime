// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
#if !NETNATIVE
    /// <summary>
    /// @todo: This is a stub DSACryptoServiceProvider that allows the rest of the X509 code to compile. Once the DSACryptoServiceProvider contract has been
    /// brought up, delete this file and add a reference to the DSA contract to the X509 .csproj file.
    /// </summary>
    internal sealed class DSACryptoServiceProvider : AsymmetricAlgorithm, ICspAsymmetricAlgorithm
    {
        public DSACryptoServiceProvider()
        {
        }

        public DSACryptoServiceProvider(CspParameters cspParameters)
        {
            throw new NotImplementedException("DSACryptoServiceProvider is not supported on this platform at this time.");
        }

        public void ImportCspBlob(byte[] cspBlob)
        {
            _cspBlob = (byte[])cspBlob.Clone();
        }

        public CspKeyContainerInfo CspKeyContainerInfo
        {
            get { throw new NotImplementedException("DSACryptoServiceProvider is not supported on this platform at this time."); }
        }

        public byte[] ExportCspBlob(bool includePrivateParameters)
        {
            return (byte[])(_cspBlob.Clone());
        }

        private byte[] _cspBlob;
    }
#endif
}

