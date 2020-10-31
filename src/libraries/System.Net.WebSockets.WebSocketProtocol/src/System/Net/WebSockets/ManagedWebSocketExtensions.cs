// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System.Net.WebSockets
{
    internal static partial class ManagedWebSocketExtensions
    {
        internal static CancellationTokenRegistration UnsafeRegister(this CancellationToken cancellationToken, Action<object?> callback, object? state) =>
            cancellationToken.Register(callback, state);
    }
}
