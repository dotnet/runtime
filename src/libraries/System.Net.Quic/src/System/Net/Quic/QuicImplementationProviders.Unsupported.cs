// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations;

namespace System.Net.Quic
{
    public static partial class QuicImplementationProviders
    {
        public static Implementations.QuicImplementationProvider Mock => Default;
        public static Implementations.QuicImplementationProvider MsQuic => Default;
        public static Implementations.QuicImplementationProvider Default { get; } = new UnsupportedQuicImplementationProvider();

        private class UnsupportedQuicImplementationProvider : QuicImplementationProvider
        {
            internal UnsupportedQuicImplementationProvider() : base(false) { }
            public override bool IsSupported => false;
        }
    }
}

namespace System.Net.Quic.Implementations
{
    public abstract partial class QuicImplementationProvider
    {
        // alternative constructor because currently it is not possible to exlude ctors from
        // PNSE autogeneration (https://github.com/dotnet/arcade/issues/8676)
        internal QuicImplementationProvider(bool _) { }
    }
}
