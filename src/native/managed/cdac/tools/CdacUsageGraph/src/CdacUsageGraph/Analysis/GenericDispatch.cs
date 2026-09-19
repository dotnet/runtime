// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace CdacUsageGraph.Analysis;

internal static class GenericDispatch
{
    public static ITypeSymbol Resolve(
        ITypeSymbol type,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        int guard = 0;
        while (type is ITypeParameterSymbol parameter &&
            substitutions.TryGetValue(parameter, out ITypeSymbol? mapped) &&
            guard++ < 16)
        {
            type = mapped;
        }
        return type;
    }

    public static Dictionary<ITypeParameterSymbol, ITypeSymbol> BuildSubstitutions(
        INamedTypeSymbol constructed,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> outer,
        SymbolEqualityComparer comparer)
    {
        Dictionary<ITypeParameterSymbol, ITypeSymbol> substitutions = new(comparer);
        Stack<INamedTypeSymbol> stack = new();
        HashSet<INamedTypeSymbol> seen = new(comparer);
        stack.Push(constructed);
        while (stack.Count > 0)
        {
            INamedTypeSymbol current = stack.Pop();
            if (!seen.Add(current))
                continue;

            INamedTypeSymbol definition = current.OriginalDefinition;
            for (int i = 0;
                i < definition.TypeParameters.Length && i < current.TypeArguments.Length;
                i++)
            {
                substitutions[definition.TypeParameters[i]] =
                    Resolve(current.TypeArguments[i], outer);
            }
            if (current.BaseType is INamedTypeSymbol baseType)
                stack.Push(baseType);
            if (current.ContainingType is INamedTypeSymbol containingType)
                stack.Push(containingType);
        }
        return substitutions;
    }

    public static IMethodSymbol? ResolveStaticAbstractTarget(
        Microsoft.CodeAnalysis.Compilation compilation,
        IInvocationOperation invocation,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> substitutions)
    {
        IMethodSymbol target = invocation.TargetMethod;
        ITypeSymbol? receiver = invocation.ConstrainedToType
            ?? (target.ContainingType is ITypeParameterSymbol ? target.ContainingType : null);
        if (receiver is null ||
            Resolve(receiver, substitutions) is not INamedTypeSymbol implementationType ||
            !SymbolEqualityComparer.Default.Equals(
                implementationType.OriginalDefinition.ContainingAssembly,
                compilation.Assembly))
        {
            return null;
        }

        IMethodSymbol? interfaceImplementation =
            FindInterfaceImplementation(implementationType, target);
        if (interfaceImplementation is not null)
            return interfaceImplementation;

        for (INamedTypeSymbol? current = implementationType;
            current is not null &&
            SymbolEqualityComparer.Default.Equals(
                current.OriginalDefinition.ContainingAssembly,
                compilation.Assembly);
            current = current.BaseType)
        {
            foreach (IMethodSymbol method in current.OriginalDefinition
                .GetMembers()
                .OfType<IMethodSymbol>())
            {
                if (HasCompatibleShape(method, target))
                    return method;
            }
        }

        return null;
    }

    public static IMethodSymbol? FindInterfaceImplementation(
        INamedTypeSymbol implementationType,
        IMethodSymbol interfaceMethod)
    {
        INamedTypeSymbol? declaringInterface = interfaceMethod.ContainingType;
        if (declaringInterface is null)
            return null;

        foreach (INamedTypeSymbol @interface in implementationType.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(
                @interface.OriginalDefinition,
                declaringInterface.OriginalDefinition))
            {
                continue;
            }

            foreach (IMethodSymbol constructedMember in @interface.GetMembers()
                .OfType<IMethodSymbol>())
            {
                if (!HasCompatibleShape(constructedMember, interfaceMethod))
                    continue;
                if (implementationType.FindImplementationForInterfaceMember(
                    constructedMember) is IMethodSymbol implementation)
                {
                    return FindVirtualImplementation(
                        implementationType,
                        implementation) ?? implementation;
                }
            }
        }

        foreach (IMethodSymbol candidate in implementationType.GetMembers()
            .OfType<IMethodSymbol>())
        {
            if (candidate.ExplicitInterfaceImplementations.Any(implementation =>
                HasCompatibleShape(implementation, interfaceMethod)))
            {
                return candidate;
            }
        }
        return null;
    }

    public static IMethodSymbol? FindVirtualImplementation(
        INamedTypeSymbol constructedType,
        IMethodSymbol method)
    {
        for (INamedTypeSymbol? current = constructedType;
            current is not null;
            current = current.BaseType)
        {
            foreach (IMethodSymbol candidate in current.OriginalDefinition
                .GetMembers(method.Name)
                .OfType<IMethodSymbol>())
            {
                if (candidate.Parameters.Length != method.Parameters.Length)
                    continue;
                for (IMethodSymbol? overridden = candidate;
                    overridden is not null;
                    overridden = overridden.OverriddenMethod)
                {
                    if (SymbolEqualityComparer.Default.Equals(
                        overridden.OriginalDefinition,
                        method.OriginalDefinition))
                    {
                        return candidate;
                    }
                }
            }
        }
        return method.IsAbstract ? null : method;
    }

    public static IMethodSymbol? FindDataFactory(INamedTypeSymbol dataType)
    {
        INamedTypeSymbol? dataInterface = dataType.AllInterfaces.FirstOrDefault(
            @interface =>
                @interface.OriginalDefinition.ContainingNamespace.ToDisplayString() +
                    "." + @interface.OriginalDefinition.MetadataName ==
                    CdacSymbols.IDataMetadataName &&
                @interface.TypeArguments.Length == 1 &&
                SymbolEqualityComparer.Default.Equals(
                    @interface.TypeArguments[0],
                    dataType));
        IMethodSymbol? create = dataInterface?.GetMembers("Create")
            .OfType<IMethodSymbol>()
            .SingleOrDefault();
        return create is null
            ? null
            : FindInterfaceImplementation(dataType, create);
    }

    private static bool HasCompatibleShape(
        IMethodSymbol candidate,
        IMethodSymbol target)
    {
        bool nameMatches =
            candidate.Name == target.Name ||
            candidate.ExplicitInterfaceImplementations.Any(implementation =>
                implementation.Name == target.Name);
        if (!nameMatches || candidate.Parameters.Length != target.Parameters.Length)
            return false;

        for (int i = 0; i < candidate.Parameters.Length; i++)
        {
            ITypeSymbol candidateType = candidate.Parameters[i].Type;
            ITypeSymbol targetType = target.Parameters[i].Type;
            if (candidateType.TypeKind == TypeKind.TypeParameter ||
                targetType.TypeKind == TypeKind.TypeParameter)
            {
                continue;
            }
            if (!SymbolEqualityComparer.Default.Equals(
                candidateType.OriginalDefinition,
                targetType.OriginalDefinition))
            {
                return false;
            }
        }
        return true;
    }
}
