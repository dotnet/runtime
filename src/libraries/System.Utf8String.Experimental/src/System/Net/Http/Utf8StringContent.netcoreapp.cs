// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    public sealed partial class Utf8StringContent
    {
        protected override Stream CreateContentReadStream(CancellationToken cancellationToken) =>
            new Utf8StringStream(_content);

        protected override void SerializeToStream(Stream stream, TransportContext? context, CancellationToken cancellationToken) =>
            stream.Write(_content.AsBytes());
    }
}
