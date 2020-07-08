// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.X509Certificates
{
    public struct X509ChainStatus
    {
        public X509ChainStatusFlags Status { get; set; }

        [AllowNull]
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

        private string? _statusInformation;
    }
}
