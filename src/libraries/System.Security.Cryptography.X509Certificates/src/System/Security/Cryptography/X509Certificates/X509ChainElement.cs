// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;

namespace System.Security.Cryptography.X509Certificates
{
    public class X509ChainElement
    {
        internal X509ChainElement(X509Certificate2 certificate, X509ChainStatus[] chainElementStatus, string information)
        {
            Certificate = certificate;
            ChainElementStatus = chainElementStatus;
            Information = information;
        }

        public X509Certificate2 Certificate { get; private set; }

        // For compat purposes, ChainElementStatus does *not* give each caller a private copy of the array.
        public X509ChainStatus[] ChainElementStatus { get; private set; }

        public string Information { get; private set; }
    }
}

