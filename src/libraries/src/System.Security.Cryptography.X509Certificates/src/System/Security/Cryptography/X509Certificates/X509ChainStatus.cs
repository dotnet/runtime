// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography.X509Certificates
{
    public struct X509ChainStatus
    {
        public X509ChainStatusFlags Status { get; set; }

        public string StatusInformation
        {
            get
            {
                if (_statusInformation == null)
                    return string.Empty;
                return _statusInformation;
            }
            set
            {
                _statusInformation = value;
            }
        }

        private string _statusInformation;
    }
}

