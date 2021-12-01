// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Runtime.InteropServices
{
    public sealed partial class PosixSignalRegistration
    {
        private PosixSignalRegistration() { }

        [DynamicDependency("#ctor")] // Prevent the private ctor and the IDisposable implementation from getting linked away
        private static PosixSignalRegistration Register(PosixSignal signal, Action<PosixSignalContext> handler) =>
            throw new PlatformNotSupportedException();

        private void Unregister() { }
    }
}
