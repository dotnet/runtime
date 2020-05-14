// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Security;
using System.Threading.Tasks;

namespace System.Net.Quic.Tests
{
    public class MsQuicTestBase : QuicTestBase
    {
        internal MsQuicTestBase() : base(QuicImplementationProviders.MsQuic)
        {
        }
    }
}
