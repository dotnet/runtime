// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal interface IChainPal : IDisposable
    {
        /// <summary>
        /// Does not throw on api error. Returns default(bool?) and sets "exception" instead.
        /// </summary>
        bool? Verify(X509VerificationFlags flags, out Exception? exception);

        X509ChainElement[]? ChainElements { get; }
        X509ChainStatus[]? ChainStatus { get; }
        SafeX509ChainHandle? SafeHandle { get; }
    }
}
