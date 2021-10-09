// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates.Tests.Common;
using Xunit;

namespace System.Security.Cryptography.X509Certificates.Tests.RevocationTests
{
    public static partial class DynamicRevocationTests
    {
        public static bool SupportsDynamicRevocation { get; } = Interop.AndroidCrypto.X509ChainSupportsRevocationOptions();
    }
}
