// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    [Flags]
    internal enum symbmask_t : long
    {
        MASK_NamespaceSymbol = 1 << SYMKIND.SK_NamespaceSymbol,
        MASK_AggregateSymbol = 1 << SYMKIND.SK_AggregateSymbol,
        MASK_TypeParameterSymbol = 1 << SYMKIND.SK_TypeParameterSymbol,
        MASK_FieldSymbol = 1 << SYMKIND.SK_FieldSymbol,
        MASK_MethodSymbol = 1 << SYMKIND.SK_MethodSymbol,
        MASK_PropertySymbol = 1 << SYMKIND.SK_PropertySymbol,
        MASK_EventSymbol = 1 << SYMKIND.SK_EventSymbol,
        MASK_ALL = ~0,
    }
}
