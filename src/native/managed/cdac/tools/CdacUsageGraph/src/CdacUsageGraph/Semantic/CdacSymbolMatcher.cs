// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CdacUsageGraph.Semantic;

/// <summary>Matches cDAC well-known types resolved from one compilation.</summary>
internal sealed class CdacSymbolMatcher
{
    private readonly INamedTypeSymbol? _contractRegistry;
    private readonly INamedTypeSymbol? _iContract;
    private readonly IMethodSymbol? _readGlobalPointer;
    private readonly IMethodSymbol? _tryReadGlobalPointer;
    private readonly IMethodSymbol? _readGlobalString;
    private readonly IMethodSymbol? _tryReadGlobalString;
    private readonly IMethodSymbol? _readGlobal;
    private readonly IMethodSymbol? _tryReadGlobal;

    public CdacSymbolMatcher(CSharpCompilation compilation)
    {
        _contractRegistry = compilation.GetTypeByMetadataName(
            CdacSymbols.ContractRegistryMetadataName);
        _iContract = compilation.GetTypeByMetadataName(
            CdacSymbols.IContractMetadataName);

        INamedTypeSymbol? target = compilation.GetTypeByMetadataName(
            CdacSymbols.TargetMetadataName);
        _readGlobalPointer = GetMethod(
            target,
            CdacSymbols.ReadGlobalPointerMethodName,
            parameterCount: 1,
            typeParameterCount: 0);
        _tryReadGlobalPointer = GetMethod(
            target,
            CdacSymbols.TryReadGlobalPointerMethodName,
            parameterCount: 2,
            typeParameterCount: 0);
        _readGlobalString = GetMethod(
            target,
            CdacSymbols.ReadGlobalStringMethodName,
            parameterCount: 1,
            typeParameterCount: 0);
        _tryReadGlobalString = GetMethod(
            target,
            CdacSymbols.TryReadGlobalStringMethodName,
            parameterCount: 2,
            typeParameterCount: 0);
        _readGlobal = GetMethod(
            target,
            CdacSymbols.ReadGlobalMethodName,
            parameterCount: 1,
            typeParameterCount: 1);
        _tryReadGlobal = GetMethod(
            target,
            CdacSymbols.TryReadGlobalMethodName,
            parameterCount: 2,
            typeParameterCount: 1);
    }

    public bool IsContractRegistry(ITypeSymbol? type) =>
        Matches(type, _contractRegistry);

    public bool IsContract(ITypeSymbol? type) =>
        Matches(type, _iContract) ||
        type is INamedTypeSymbol named &&
        named.AllInterfaces.Any(@interface => Matches(@interface, _iContract));

    public bool TryGetGlobalRead(IMethodSymbol method, out GlobalReadKind kind)
    {
        if (Matches(method, _readGlobalPointer))
            kind = GlobalReadKind.Pointer;
        else if (Matches(method, _tryReadGlobalPointer))
            kind = GlobalReadKind.OptionalPointer;
        else if (Matches(method, _readGlobalString))
            kind = GlobalReadKind.String;
        else if (Matches(method, _tryReadGlobalString))
            kind = GlobalReadKind.OptionalString;
        else if (Matches(method, _readGlobal))
            kind = GlobalReadKind.Generic;
        else if (Matches(method, _tryReadGlobal))
            kind = GlobalReadKind.OptionalGeneric;
        else
        {
            kind = default;
            return false;
        }

        return true;
    }

    private static IMethodSymbol? GetMethod(
        INamedTypeSymbol? type,
        string name,
        int parameterCount,
        int typeParameterCount) =>
        type?.GetMembers(name)
            .OfType<IMethodSymbol>()
            .SingleOrDefault(method =>
                method.Parameters.Length == parameterCount &&
                method.TypeParameters.Length == typeParameterCount);

    private static bool Matches(ITypeSymbol? type, INamedTypeSymbol? wellKnownType) =>
        type is not null &&
        wellKnownType is not null &&
        SymbolEqualityComparer.Default.Equals(
            type.OriginalDefinition,
            wellKnownType.OriginalDefinition);

    private static bool Matches(IMethodSymbol method, IMethodSymbol? wellKnownMethod) =>
        wellKnownMethod is not null &&
        SymbolEqualityComparer.Default.Equals(
            method.OriginalDefinition,
            wellKnownMethod.OriginalDefinition);
}

internal enum GlobalReadKind
{
    Pointer,
    OptionalPointer,
    String,
    OptionalString,
    Generic,
    OptionalGeneric,
}
