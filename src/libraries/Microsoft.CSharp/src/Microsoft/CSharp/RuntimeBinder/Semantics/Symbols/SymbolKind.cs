// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum SYMKIND
    {
        SK_NamespaceSymbol,
        SK_AggregateSymbol,
        SK_TypeParameterSymbol,
        SK_FieldSymbol,
        SK_LocalVariableSymbol,
        SK_MethodSymbol,
        SK_PropertySymbol,
        SK_EventSymbol,
        SK_Scope,
        SK_IndexerSymbol
    }
}
