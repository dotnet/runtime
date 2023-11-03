// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Mail;
using System.Runtime.CompilerServices;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.Mail")]
    internal sealed partial class NetEventSource { }
}
