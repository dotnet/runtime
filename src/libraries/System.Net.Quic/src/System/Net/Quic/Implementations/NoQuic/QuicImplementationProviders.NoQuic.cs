// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Quic
{
    public static partial class QuicImplementationProviders
    {
        public static Implementations.QuicImplementationProvider NoQuic { get; } = new Implementations.NoQuic.NoQuicImplementationProvider();
        public static Implementations.QuicImplementationProvider Default => NoQuic;
    }
}
