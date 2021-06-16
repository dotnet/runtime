// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace System.Runtime.InteropServices
{
    public sealed class PosixSignalRegistration : IDisposable
    {
        private PosixSignalRegistration() { }

        public static PosixSignalRegistration Create(PosixSignal signal, Action<PosixSignalContext> handler)
            => throw new PlatformNotSupportedException();

        public void Dispose()
            => throw new PlatformNotSupportedException();
    }
}
