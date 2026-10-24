// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop;

/// <summary>
/// Type used to pass on default marshalling details.
/// </summary>
/// <remarks>
/// This type used to pass default marshalling details to the various marshalling info parsers.
/// Since it contains a <see cref="INamedTypeSymbol"/>, it should not be used as a field on any types
/// derived from <see cref="MarshallingInfo"/>. See remarks on <see cref="MarshallingInfo"/>.
/// </remarks>
public sealed record DefaultMarshallingInfo(
    CharEncoding CharEncoding,
    INamedTypeSymbol? StringMarshallingCustomType
);
