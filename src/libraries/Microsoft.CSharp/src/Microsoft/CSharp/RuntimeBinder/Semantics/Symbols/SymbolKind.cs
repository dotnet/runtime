// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal enum SYMKIND
    {
        SK_NamespaceSymbol,
        SK_NamespaceDeclaration,
        SK_AssemblyQualifiedNamespaceSymbol,
        SK_AggregateSymbol,
        SK_AggregateDeclaration,
        SK_TypeParameterSymbol,
        SK_FieldSymbol,
        SK_LocalVariableSymbol,
        SK_MethodSymbol,
        SK_PropertySymbol,
        SK_EventSymbol,
        SK_TransparentIdentifierMemberSymbol,
        SK_AliasSymbol,
        SK_ExternalAliasDefinitionSymbol,
        SK_Scope,
        SK_CachedNameSymbol,
        SK_LabelSymbol,
        SK_GlobalAttributeDeclaration,
        SK_LambdaScope,
        SK_UnresolvedAggregateSymbol,
        SK_InterfaceImplementationMethodSymbol,
        SK_IndexerSymbol,
        SK_ParentSymbol,
        SK_IteratorFinallyMethodSymbol,
        SK_LIM
    }

    // The kinds of Synthesized aggregates
    internal enum SynthAggKind
    {
        NotSynthesized,

        AnonymousMethodDisplayClass,
        IteratorClass,
        FixedBufferStruct,

        Lim
    }
}
