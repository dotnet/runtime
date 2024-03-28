// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Diagnostics.Metrics
{
    public sealed class HistogramAdvice<T> where T : struct
    {
        public IReadOnlyList<T>? ExplicitBucketBoundaries { get; init; }
    }
}
