// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Mono
{
    // Internal type used by Mono runtime only
    internal sealed class MonoListItem
    {
        public MonoListItem? next;
        public object? data;
    }
}
