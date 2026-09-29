// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Interop;

/// <summary>Describes how a signature element is passed by reference.</summary>
public enum RefKind
{
    /// <summary>The element is passed by value.</summary>
    None,
    /// <summary>The element is passed by writable reference.</summary>
    Ref,
    /// <summary>The element is an output parameter.</summary>
    Out,
    /// <summary>The element is an input parameter passed by readonly reference.</summary>
    In,
    /// <summary>The element is returned by readonly reference.</summary>
    RefReadOnly = In,
    /// <summary>The element is a ref readonly parameter.</summary>
    RefReadOnlyParameter
}
