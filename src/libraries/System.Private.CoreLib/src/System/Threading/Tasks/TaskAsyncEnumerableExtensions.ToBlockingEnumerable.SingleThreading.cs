// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;

namespace System.Threading.Tasks;

public static partial class TaskAsyncEnumerableExtensions
{
    [UnsupportedOSPlatform("browser")]
    private static IEnumerable<T> ToBlockingEnumerableCore<T>(IAsyncEnumerable<T> source, CancellationToken cancellationToken)
    {
        throw new PlatformNotSupportedException();
#pragma warning disable CS0162 // The unreachable yield defers the exception until the iterator's first MoveNext.
        yield break;
#pragma warning restore CS0162
    }
}
