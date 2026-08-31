// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Net.Http.Json
{
    public sealed partial class JsonContent : System.Net.Http.HttpContent
    {
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
    }
}
