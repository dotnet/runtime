// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    /// <summary>Empty struct used as placeholder for generic type parameters when none is needed.</summary>
    internal struct VoidValueTypeParameter { }

    /// <summary>This can be used instead of <code>VoidValueTypeParameter</code> when a reference type is required.
    /// In case of an actual instantiation (e.g. through <code>default(T)</code>),
    /// using <code>VoidValueTypeParameter</code> offers better performance.</summary>
    internal class VoidReferenceTypeParameter { }
}  // namespace
