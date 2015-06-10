// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal
{
    internal interface IChainPal : IDisposable
    {
        /// <summary>
        /// Does not throw on api error. Returns default(bool?) and sets "exception" instead. 
        /// </summary>
        bool? Verify(X509VerificationFlags flags, out Exception exception);

        IEnumerable<X509ChainElement> ChainElements { get; }
        X509ChainStatus[] ChainStatus { get; }
        SafeX509ChainHandle SafeHandle { get; }
    }
}
