// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    internal sealed class ComClassInfo : IEquatable<ComClassInfo>
    {
        public string ClassName { get; init; }
        public ContainingSyntaxContext ContainingSyntaxContext { get; init; }
        public ContainingSyntax ClassSyntax { get; init; }
        public SequenceEqualImmutableArray<string> ImplementedInterfacesNames { get; init; }

        private ComClassInfo(string className, ContainingSyntaxContext containingSyntaxContext, ContainingSyntax classSyntax, SequenceEqualImmutableArray<string> implementedInterfacesNames)
        {
            ClassName = className;
            ContainingSyntaxContext = containingSyntaxContext;
            ClassSyntax = classSyntax;
            ImplementedInterfacesNames = implementedInterfacesNames;
        }

        public static ComClassInfo From(INamedTypeSymbol type, ClassDeclarationSyntax syntax, INamedTypeSymbol? generatedComInterfaceAttributeType)
        {
            ImmutableArray<string>.Builder names = ImmutableArray.CreateBuilder<string>();
            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                AttributeData? generatedComInterfaceAttribute = iface.GetAttributes().FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, generatedComInterfaceAttributeType));
                if (generatedComInterfaceAttribute is not null)
                {
                    var attributeData = GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComInterfaceAttribute);
                    if (attributeData.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
                    {
                        names.Add(iface.ToDisplayString());
                    }
                }
            }

            return new ComClassInfo(
                type.ToDisplayString(),
                new ContainingSyntaxContext(syntax),
                new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                new(names.ToImmutable()));
        }

        public bool Equals(ComClassInfo? other)
        {
            return other is not null
                && ClassName == other.ClassName
                && ContainingSyntaxContext.Equals(other.ContainingSyntaxContext)
                && ImplementedInterfacesNames.SequenceEqual(other.ImplementedInterfacesNames);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ComClassInfo);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassName, ContainingSyntaxContext, ImplementedInterfacesNames);
        }
    }
}
