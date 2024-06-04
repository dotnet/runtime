// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Options.Generators
{
    /// <summary>
    /// Holds required symbols for the <see cref="Generator"/>.
    /// </summary>
    internal sealed record class SymbolHolder(
        INamedTypeSymbol OptionsValidatorSymbol,
        INamedTypeSymbol ValidationAttributeSymbol,
        INamedTypeSymbol MaxLengthAttributeSymbol,
        INamedTypeSymbol MinLengthAttributeSymbol,
        INamedTypeSymbol CompareAttributeSymbol,
        INamedTypeSymbol? LengthAttributeSymbol,
        INamedTypeSymbol? UnconditionalSuppressMessageAttributeSymbol,
        INamedTypeSymbol RangeAttributeSymbol,
        INamedTypeSymbol ICollectionSymbol,
        INamedTypeSymbol DataTypeAttributeSymbol,
        INamedTypeSymbol ValidateOptionsSymbol,
        INamedTypeSymbol IValidatableObjectSymbol,
        INamedTypeSymbol GenericIEnumerableSymbol,
        INamedTypeSymbol TypeSymbol,
        INamedTypeSymbol TimeSpanSymbol,
        INamedTypeSymbol ValidateObjectMembersAttributeSymbol,
        INamedTypeSymbol ValidateEnumeratedItemsAttributeSymbol);
}
