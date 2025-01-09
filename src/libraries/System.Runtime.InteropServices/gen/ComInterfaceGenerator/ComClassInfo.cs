// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public static DiagnosticOr<ComClassInfo> From(INamedTypeSymbol type, ClassDeclarationSyntax syntax, bool unsafeCodeIsEnabled)
        {
            if (!unsafeCodeIsEnabled)
            {
                return DiagnosticOr<ComClassInfo>.From(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, syntax.Identifier.GetLocation()));
            }

            if (!syntax.IsInPartialContext(out _))
            {
                return DiagnosticOr<ComClassInfo>.From(
                    DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidAttributedClassMissingPartialModifier,
                        syntax.Identifier.GetLocation(),
                        type.ToDisplayString()));
            }

            ImmutableArray<string>.Builder names = ImmutableArray.CreateBuilder<string>();
            foreach (INamedTypeSymbol iface in type.AllInterfaces)
            {
                AttributeData? generatedComInterfaceAttribute = iface.GetAttributes().FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute);
                if (generatedComInterfaceAttribute is not null)
                {
                    var attributeData = GeneratedComInterfaceCompilationData.GetDataFromAttribute(generatedComInterfaceAttribute);
                    if (attributeData.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
                    {
                        names.Add(iface.ToDisplayString());
                    }
                }
            }

            if (names.Count == 0)
            {
                return DiagnosticOr<ComClassInfo>.From(DiagnosticInfo.Create(GeneratorDiagnostics.ClassDoesNotImplementAnyGeneratedComInterface,
                    syntax.Identifier.GetLocation(),
                    type.ToDisplayString()));
            }

            return DiagnosticOr<ComClassInfo>.From(
                new ComClassInfo(
                    type.ToDisplayString(),
                    new ContainingSyntaxContext(syntax),
                    new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                    new(names.ToImmutable())));
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
