// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop;

/// <summary>Describes the escape scope of a signature element.</summary>
public enum ScopedKind
{
    /// <summary>The element has no scoped modifier.</summary>
    None,
    /// <summary>The reference to the element is scoped.</summary>
    ScopedRef,
    /// <summary>The value of the element is scoped.</summary>
    ScopedValue
}
