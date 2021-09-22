// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.FileProviders
{
    internal interface IPollingChangeToken : IChangeToken
    {
        CancellationTokenSource? CancellationTokenSource { get; }
    }
}
