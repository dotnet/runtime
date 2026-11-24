// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    // Identifies the type map of a group that starts at an owner type (TypeMapping.TryGetValue<TTypeMapGroup, TTypeMapGroupOwner, TValue>).
    // It is never instantiated: the Native AOT compiler uses the type as the key of the scoped tables of the image.
    internal sealed class TypeMapOwnerScope<TTypeMapGroup, TTypeMapGroupOwner>
    {
    }
}
