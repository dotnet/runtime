// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

namespace Microsoft.Interop
{
    internal sealed record ComClassInfo
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

        public static ComClassInfo? TryGetFrom(INamedTypeSymbol type, ClassDeclarationSyntax syntax, Compilation compilation)
        {
            if (!syntax.IsInPartialContext(out _))
            {
                return null;
            }

            ImmutableArray<string>.Builder names = ImmutableArray.CreateBuilder<string>();
            INamedTypeSymbol? generatedComInterfaceAttributeType = compilation.GetBestTypeByMetadataName(TypeNames.GeneratedComInterfaceAttribute);
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

            return names.Count == 0 ? null : new ComClassInfo(
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

        public override int GetHashCode()
        {
            return HashCode.Combine(ClassName, ContainingSyntaxContext, ImplementedInterfacesNames);
        }
    }
}
