// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;
using Microsoft.CodeAnalysis;

namespace CdacUsageGraph.Analysis;

/// <summary>Discovers implementation entry points and returned interface types for one contract.</summary>
internal static class ContractEntryPoints
{
    public static IReadOnlyCollection<ISymbol> Get(ContractRegistration registration)
    {
        HashSet<ISymbol> entries = new(
            ConstructionEntryPoints.Get(
                registration.Impl,
                registration.Constructor),
            SymbolEqualityComparer.Default);

        foreach (INamedTypeSymbol @interface in registration.Impl.AllInterfaces)
        {
            foreach (ISymbol member in @interface.GetMembers())
                AddImplementation(registration.Impl, member, entries);
        }

        return entries;
    }

    public static IReadOnlySet<INamedTypeSymbol> GetReturnedInterfaces(
        ContractRegistration registration)
    {
        HashSet<INamedTypeSymbol> interfaces = new(SymbolEqualityComparer.Default);

        foreach (ISymbol member in registration.Interface.GetMembers())
        {
            switch (member)
            {
                case IMethodSymbol method:
                    AddInterfaceTypes(method.ReturnType, interfaces);
                    foreach (IParameterSymbol parameter in method.Parameters)
                    {
                        if (parameter.RefKind == RefKind.Out)
                            AddInterfaceTypes(parameter.Type, interfaces);
                    }
                    break;
                case IPropertySymbol property:
                    AddInterfaceTypes(property.Type, interfaces);
                    break;
            }
        }

        return interfaces;
    }

    private static void AddInterfaceTypes(
        ITypeSymbol type,
        HashSet<INamedTypeSymbol> interfaces)
    {
        switch (type)
        {
            case INamedTypeSymbol named:
                if (named.TypeKind == TypeKind.Interface)
                    interfaces.Add(named.OriginalDefinition);
                foreach (ITypeSymbol argument in named.TypeArguments)
                    AddInterfaceTypes(argument, interfaces);
                break;
            case IArrayTypeSymbol array:
                AddInterfaceTypes(array.ElementType, interfaces);
                break;
        }
    }

    private static void AddImplementation(
        INamedTypeSymbol implementationType,
        ISymbol interfaceMember,
        HashSet<ISymbol> entries)
    {
        if (interfaceMember is IPropertySymbol property)
        {
            if (property.GetMethod is not null)
                AddImplementation(implementationType, property.GetMethod, entries);
            if (property.SetMethod is not null)
                AddImplementation(implementationType, property.SetMethod, entries);
        }
        else if (implementationType.FindImplementationForInterfaceMember(interfaceMember)
                     is IMethodSymbol implementation &&
                 SymbolEqualityComparer.Default.Equals(
                     implementation.ContainingAssembly,
                     implementationType.ContainingAssembly))
        {
            IMethodSymbol entryPoint =
                GenericDispatch.FindVirtualImplementation(
                    implementationType,
                    implementation) ?? implementation;
            entries.Add(entryPoint.OriginalDefinition);
        }
    }
}
