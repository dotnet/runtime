// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http.Functional.Tests
{
    public partial class ThrowingContent : HttpContent
    {
        protected override void SerializeToStream(Stream stream, TransportContext context, CancellationToken cancellationToken)
        {
            throw _exnFactory();
        }
    }
}
