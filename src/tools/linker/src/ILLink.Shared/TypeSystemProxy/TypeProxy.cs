// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TypeSystemProxy
{
    internal readonly partial struct TypeProxy : IMemberProxy
    {
        internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters();
    }
}
