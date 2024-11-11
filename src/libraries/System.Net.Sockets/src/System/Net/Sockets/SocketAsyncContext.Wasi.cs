// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace System.Net.Sockets
{
    internal sealed partial class SocketAsyncContext
    {
        public CancellationTokenSource unregisterPollHook = new();
    }
}
