// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace Microsoft.Extensions.Caching.Memory
{
    public partial class MemoryCache
    {
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public bool TryGetValue(System.ReadOnlySpan<char> key, out object? value) { throw null; }
        [System.Runtime.CompilerServices.OverloadResolutionPriority(1)]
        public bool TryGetValue<TItem>(System.ReadOnlySpan<char> key, out TItem? value) { throw null; }
    }
}
