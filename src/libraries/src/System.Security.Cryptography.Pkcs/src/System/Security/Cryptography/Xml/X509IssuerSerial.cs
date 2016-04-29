// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.Xml
{
    public struct X509IssuerSerial
    {
        internal X509IssuerSerial(string issuerName, string serialNumber)
            : this()
        {
            Debug.Assert(!string.IsNullOrEmpty(issuerName));
            Debug.Assert(!string.IsNullOrEmpty(serialNumber));

            IssuerName = issuerName;
            SerialNumber = serialNumber;
        }

        public string IssuerName { get; set; }
        public string SerialNumber { get; set; }
    }
}

