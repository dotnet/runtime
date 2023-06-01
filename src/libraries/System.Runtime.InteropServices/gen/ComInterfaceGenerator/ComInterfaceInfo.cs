// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Information about a Com interface, but not its methods.
    /// </summary>
    internal sealed record ComInterfaceInfo(
        ManagedTypeInfo Type,
        string ThisInterfaceKey, // For associating interfaces to its base
        string? BaseInterfaceKey, // For associating interfaces to its base
        InterfaceDeclarationSyntax Declaration,
        ContainingSyntaxContext TypeDefinitionContext,
        ContainingSyntax ContainingSyntax,
        Guid InterfaceId,
        LocationInfo DiagnosticLocation)
    {
        public static DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)> From(INamedTypeSymbol symbol, InterfaceDeclarationSyntax syntax, CancellationToken _)
        {
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (syntax.TypeParameterList is not null)
            {
                // Verify the interface has no generic types or defined implementation
                // and is not marked static or sealed
                if (syntax.TypeParameterList is not null)
                {
                    return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(
                        Diagnostic.Create(
                            GeneratorDiagnostics.InvalidAttributedInterfaceGenericNotSupported,
                            syntax.Identifier.GetLocation(),
                            symbol.Name));
                }
            }

            // Verify that the types the method is declared in are marked partial.
            for (SyntaxNode? parentNode = syntax.Parent; parentNode is TypeDeclarationSyntax typeDecl; parentNode = parentNode.Parent)
            {
                if (!typeDecl.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(
                        Diagnostic.Create(
                            GeneratorDiagnostics.InvalidAttributedMethodContainingTypeMissingModifiers,
                            syntax.Identifier.GetLocation(),
                            symbol.Name,
                            typeDecl.Identifier));
                }
            }

            if (!TryGetGuid(symbol, syntax, out Guid? guid, out Diagnostic? guidDiagnostic))
                return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(guidDiagnostic);

            if (!TryGetBaseComInterface(symbol, syntax, out INamedTypeSymbol? baseSymbol, out Diagnostic? baseDiagnostic))
                return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(baseDiagnostic);

            if (!StringMarshallingIsValid(symbol, syntax, baseSymbol, out Diagnostic? stringMarshallingDiagnostic))
                return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(stringMarshallingDiagnostic);

            return DiagnosticOr<(ComInterfaceInfo InterfaceInfo, INamedTypeSymbol Symbol)>.From(
                (new ComInterfaceInfo(
                    ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol),
                    symbol.ToDisplayString(),
                    baseSymbol?.ToDisplayString(),
                    syntax,
                    new ContainingSyntaxContext(syntax),
                    new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                    guid ?? Guid.Empty,
                    LocationInfo.From(symbol)),
                symbol));
        }

        private static bool StringMarshallingIsValid(INamedTypeSymbol symbol, InterfaceDeclarationSyntax syntax, INamedTypeSymbol? baseSymbol, [NotNullWhen(false)] out Diagnostic? stringMarshallingDiagnostic)
        {
            var attrInfo = GeneratedComInterfaceData.From(GeneratedComInterfaceCompilationData.GetAttributeDataFromInterfaceSymbol(symbol));
            if (attrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling) || attrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
            {
                if (attrInfo.StringMarshalling is StringMarshalling.Custom && attrInfo.StringMarshallingCustomType is null)
                {
                    stringMarshallingDiagnostic = Diagnostic.Create(
                        GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        SR.InvalidStringMarshallingConfigurationMissingCustomType);
                    return false;
                }
                if (attrInfo.StringMarshalling is not StringMarshalling.Custom && attrInfo.StringMarshallingCustomType is not null)
                {
                    stringMarshallingDiagnostic = Diagnostic.Create(
                        GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        SR.InvalidStringMarshallingConfigurationNotCustom);
                    return false;
                }
            }
            if (baseSymbol is not null)
            {
                var baseAttrInfo = GeneratedComInterfaceData.From(GeneratedComInterfaceCompilationData.GetAttributeDataFromInterfaceSymbol(baseSymbol));
                // The base can be undefined string marshalling
                if ((baseAttrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling) || baseAttrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
                    && baseAttrInfo != attrInfo)
                {
                    stringMarshallingDiagnostic = Diagnostic.Create(
                        GeneratorDiagnostics.InvalidStringMarshallingMismatchBetweenBaseAndDerived,
                        syntax.Identifier.GetLocation(),
                        symbol.ToDisplayString(),
                        SR.GeneratedComInterfaceStringMarshallingMustMatchBase);
                    return false;
                }
            }
            stringMarshallingDiagnostic = null;
            return true;
        }

        /// <summary>
        /// Returns true if there is 0 or 1 base Com interfaces (i.e. the inheritance is valid), and returns false when there are 2 or more base Com interfaces and sets <paramref name="diagnostic"/>.
        /// </summary>
        private static bool TryGetBaseComInterface(INamedTypeSymbol comIface, InterfaceDeclarationSyntax syntax, out INamedTypeSymbol? baseComIface, [NotNullWhen(false)] out Diagnostic? diagnostic)
        {
            baseComIface = null;
            foreach (var implemented in comIface.Interfaces)
            {
                foreach (var attr in implemented.GetAttributes())
                {
                    if (attr.AttributeClass?.ToDisplayString() == TypeNames.GeneratedComInterfaceAttribute)
                    {
                        if (baseComIface is not null)
                        {
                            diagnostic = Diagnostic.Create(
                                GeneratorDiagnostics.MultipleComInterfaceBaseTypes,
                                syntax.Identifier.GetLocation(),
                                comIface.ToDisplayString());
                            return false;
                        }
                        baseComIface = implemented;
                    }
                }
            }
            diagnostic = null;
            return true;
        }

        /// <summary>
        /// Returns true and sets <paramref name="guid"/> if the guid is present. Returns false and sets diagnostic if the guid is not present or is invalid.
        /// </summary>
        private static bool TryGetGuid(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax syntax, [NotNullWhen(true)] out Guid? guid, [NotNullWhen(false)] out Diagnostic? diagnostic)
        {
            guid = null;
            AttributeData? guidAttr = null;
            AttributeData? _ = null; // Interface Attribute Type. We'll always assume IUnkown for now.
            foreach (var attr in interfaceSymbol.GetAttributes())
            {
                var attrDisplayString = attr.AttributeClass?.ToDisplayString();
                if (attrDisplayString is TypeNames.System_Runtime_InteropServices_GuidAttribute)
                    guidAttr = attr;
                else if (attrDisplayString is TypeNames.InterfaceTypeAttribute)
                    _ = attr;
            }

            if (guidAttr is not null
                && guidAttr.ConstructorArguments.Length == 1
                && guidAttr.ConstructorArguments[0].Value is string guidStr
                && Guid.TryParse(guidStr, out var result))
            {
                guid = result;
            }

            // Assume interfaceType is IUnknown for now
            if (guid is null)
            {
                diagnostic = Diagnostic.Create(
                    GeneratorDiagnostics.InvalidAttributedInterfaceMissingGuidAttribute,
                    syntax.Identifier.GetLocation(),
                    interfaceSymbol.ToDisplayString());
                return false;
            }
            diagnostic = null;
            return true;
        }

        public override int GetHashCode()
        {
            // ContainingSyntax and ContainingSyntaxContext do not implement GetHashCode
            return HashCode.Combine(Type, TypeDefinitionContext, InterfaceId);
        }

        public bool Equals(ComInterfaceInfo other)
        {
            // ContainingSyntax and ContainingSyntaxContext are not used in the hash code
            return Type == other.Type
                && TypeDefinitionContext == other.TypeDefinitionContext
                && InterfaceId == other.InterfaceId;
        }
    }
}
