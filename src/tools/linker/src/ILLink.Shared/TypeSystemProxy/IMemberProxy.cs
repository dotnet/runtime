// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
    interface IMemberProxy
    {
        public string Name { get; }

        public string GetDisplayName();
    }
}
