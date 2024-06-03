// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using DiagnosticOrInterfaceInfo = Microsoft.Interop.DiagnosticOr<(Microsoft.Interop.ComInterfaceInfo InterfaceInfo, Microsoft.CodeAnalysis.INamedTypeSymbol Symbol)>;

namespace Microsoft.Interop
{
    /// <summary>
    /// Information about a Com interface, but not its methods.
    /// </summary>
    internal sealed record ComInterfaceInfo
    {
        public ManagedTypeInfo Type { get; init; }
        public string ThisInterfaceKey { get; init; }
        public string? BaseInterfaceKey { get; init; }
        public InterfaceDeclarationSyntax Declaration { get; init; }
        public ContainingSyntaxContext TypeDefinitionContext { get; init; }
        public ContainingSyntax ContainingSyntax { get; init; }
        public Guid InterfaceId { get; init; }
        public ComInterfaceOptions Options { get; init; }
        public Location DiagnosticLocation { get; init; }

        private ComInterfaceInfo(
            ManagedTypeInfo type,
            string thisInterfaceKey,
            string? baseInterfaceKey,
            InterfaceDeclarationSyntax declaration,
            ContainingSyntaxContext typeDefinitionContext,
            ContainingSyntax containingSyntax,
            Guid interfaceId,
            ComInterfaceOptions options,
            Location diagnosticLocation)
        {
            Type = type;
            ThisInterfaceKey = thisInterfaceKey;
            BaseInterfaceKey = baseInterfaceKey;
            Declaration = declaration;
            TypeDefinitionContext = typeDefinitionContext;
            ContainingSyntax = containingSyntax;
            InterfaceId = interfaceId;
            Options = options;
            DiagnosticLocation = diagnosticLocation;
        }

        public static DiagnosticOrInterfaceInfo From(INamedTypeSymbol symbol, InterfaceDeclarationSyntax syntax, StubEnvironment env, CancellationToken _)
        {
            if (env.Compilation.Options is not CSharpCompilationOptions { AllowUnsafe: true }) // Unsafe code enabled
                return DiagnosticOrInterfaceInfo.From(DiagnosticInfo.Create(GeneratorDiagnostics.RequiresAllowUnsafeBlocks, syntax.Identifier.GetLocation()));
            // Verify the method has no generic types or defined implementation
            // and is not marked static or sealed
            if (syntax.TypeParameterList is not null)
            {
                return DiagnosticOrInterfaceInfo.From(
                    DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidAttributedInterfaceGenericNotSupported,
                        syntax.Identifier.GetLocation(),
                        symbol.Name));
            }

            if (!IsInPartialContext(symbol, syntax, out DiagnosticInfo? partialContextDiagnostic))
                return DiagnosticOrInterfaceInfo.From(partialContextDiagnostic);

            if (!symbol.IsAccessibleFromFileScopedClass(out var details))
            {
                return DiagnosticOrInterfaceInfo.From(DiagnosticInfo.Create(
                    GeneratorDiagnostics.InvalidAttributedInterfaceNotAccessible,
                    syntax.Identifier.GetLocation(),
                    symbol.ToDisplayString(),
                    details));
            }

            if (!TryGetGuid(symbol, syntax, out Guid? guid, out DiagnosticInfo? guidDiagnostic))
                return DiagnosticOrInterfaceInfo.From(guidDiagnostic);

            if (!TryGetBaseComInterface(symbol, syntax, out INamedTypeSymbol? baseSymbol, out DiagnosticInfo? baseDiagnostic))
                return DiagnosticOrInterfaceInfo.From(baseDiagnostic);

            var interfaceAttributeData = GeneratedComInterfaceCompilationData.GetAttributeDataFromInterfaceSymbol(symbol);
            var baseAttributeData = baseSymbol is not null ? GeneratedComInterfaceCompilationData.GetAttributeDataFromInterfaceSymbol(baseSymbol) : null;

            if (!StringMarshallingIsValid(symbol, syntax, interfaceAttributeData, baseAttributeData, out DiagnosticInfo? stringMarshallingDiagnostic))
                return DiagnosticOrInterfaceInfo.From(stringMarshallingDiagnostic);

            if (!OptionsAreValid(symbol, syntax, interfaceAttributeData, baseAttributeData, out DiagnosticInfo? optionsDiagnostic))
                return DiagnosticOrInterfaceInfo.From(optionsDiagnostic);

            return DiagnosticOrInterfaceInfo.From(
                (new ComInterfaceInfo(
                    ManagedTypeInfo.CreateTypeInfoForTypeSymbol(symbol),
                    symbol.ToDisplayString(),
                    baseSymbol?.ToDisplayString(),
                    syntax,
                    new ContainingSyntaxContext(syntax),
                    new ContainingSyntax(syntax.Modifiers, syntax.Kind(), syntax.Identifier, syntax.TypeParameterList),
                    guid ?? Guid.Empty,
                    interfaceAttributeData.Options,
                    syntax.Identifier.GetLocation()),
                symbol));
        }

        private static bool IsInPartialContext(INamedTypeSymbol symbol, InterfaceDeclarationSyntax syntax, [NotNullWhen(false)] out DiagnosticInfo? diagnostic)
        {
            // Verify that the types the interface is declared in are marked partial.
            if (!syntax.IsInPartialContext(out var nonPartialIdentifier))
            {
                diagnostic = DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidAttributedInterfaceMissingPartialModifiers,
                        syntax.Identifier.GetLocation(),
                        symbol.Name,
                        nonPartialIdentifier);
                return false;
            }
            diagnostic = null;
            return true;
        }

        private static bool StringMarshallingIsValid(
            INamedTypeSymbol interfaceSymbol,
            InterfaceDeclarationSyntax syntax,
            GeneratedComInterfaceCompilationData attrSymbolInfo,
            GeneratedComInterfaceCompilationData? baseAttrInfo,
            [NotNullWhen(false)] out DiagnosticInfo? stringMarshallingDiagnostic)
        {
            var attrInfo = GeneratedComInterfaceData.From(attrSymbolInfo);
            if (attrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling) || attrInfo.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
            {
                if (attrInfo.StringMarshalling is StringMarshalling.Custom)
                {
                    if (attrInfo.StringMarshallingCustomType is null)
                    {
                        stringMarshallingDiagnostic = DiagnosticInfo.Create(
                            GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface,
                            syntax.Identifier.GetLocation(),
                            interfaceSymbol.ToDisplayString(),
                            SR.InvalidStringMarshallingConfigurationMissingCustomType);
                        return false;
                    }
                    if (!attrSymbolInfo.StringMarshallingCustomType.IsAccessibleFromFileScopedClass(out var details))
                    {
                        stringMarshallingDiagnostic = DiagnosticInfo.Create(
                            GeneratorDiagnostics.StringMarshallingCustomTypeNotAccessibleByGeneratedCode,
                            syntax.Identifier.GetLocation(),
                            attrInfo.StringMarshallingCustomType.FullTypeName.Replace(TypeNames.GlobalAlias, ""),
                            details);
                        return false;
                    }
                }
                else if (attrInfo.StringMarshallingCustomType is not null)
                {
                    stringMarshallingDiagnostic = DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidStringMarshallingConfigurationOnInterface,
                        syntax.Identifier.GetLocation(),
                        interfaceSymbol.ToDisplayString(),
                        SR.InvalidStringMarshallingConfigurationNotCustom);
                    return false;
                }
            }
            if (baseAttrInfo is not null)
            {
                var baseAttr = GeneratedComInterfaceData.From(baseAttrInfo);
                // The base can be undefined string marshalling
                if ((baseAttr.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshalling) || baseAttr.IsUserDefined.HasFlag(InteropAttributeMember.StringMarshallingCustomType))
                    && (baseAttr.StringMarshalling, baseAttr.StringMarshallingCustomType) != (attrInfo.StringMarshalling, attrInfo.StringMarshallingCustomType))
                {
                    stringMarshallingDiagnostic = DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidStringMarshallingMismatchBetweenBaseAndDerived,
                        syntax.Identifier.GetLocation(),
                        interfaceSymbol.ToDisplayString(),
                        SR.GeneratedComInterfaceStringMarshallingMustMatchBase);
                    return false;
                }
            }
            stringMarshallingDiagnostic = null;
            return true;
        }

        private static bool OptionsAreValid(
            INamedTypeSymbol interfaceSymbol,
            InterfaceDeclarationSyntax syntax,
            GeneratedComInterfaceCompilationData attrSymbolInfo,
            GeneratedComInterfaceCompilationData? baseAttrInfo,
            [NotNullWhen(false)] out DiagnosticInfo? optionsDiagnostic)
        {
            var attrInfo = GeneratedComInterfaceData.From(attrSymbolInfo);
            if (attrInfo.Options == ComInterfaceOptions.None)
            {
                optionsDiagnostic = DiagnosticInfo.Create(
                    GeneratorDiagnostics.InvalidOptionsOnInterface,
                    syntax.Identifier.GetLocation(),
                    interfaceSymbol.ToDisplayString(),
                    SR.OneWrapperMustBeGenerated);
                return false;
            }
            if (baseAttrInfo is not null)
            {
                var baseAttr = GeneratedComInterfaceData.From(baseAttrInfo);
                // The base type must specify at least the same wrappers as the derived type.
                if ((attrInfo.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper) && !baseAttr.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper))
                    || (attrInfo.Options.HasFlag(ComInterfaceOptions.ManagedObjectWrapper) && !baseAttr.Options.HasFlag(ComInterfaceOptions.ComObjectWrapper)))
                {
                    optionsDiagnostic = DiagnosticInfo.Create(
                        GeneratorDiagnostics.InvalidOptionsOnInterface,
                        syntax.Identifier.GetLocation(),
                        interfaceSymbol.ToDisplayString(),
                        SR.BaseInterfaceMustGenerateAtLeastSameWrappers);
                    return false;
                }
            }
            optionsDiagnostic = null;
            return true;
        }

        /// <summary>
        /// Returns true if there is 0 or 1 base Com interfaces (i.e. the inheritance is valid), and returns false when there are 2 or more base Com interfaces and sets <paramref name="diagnostic"/>.
        /// </summary>
        private static bool TryGetBaseComInterface(INamedTypeSymbol comIface, InterfaceDeclarationSyntax syntax, out INamedTypeSymbol? baseComIface, [NotNullWhen(false)] out DiagnosticInfo? diagnostic)
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
                            diagnostic = DiagnosticInfo.Create(
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
        private static bool TryGetGuid(INamedTypeSymbol interfaceSymbol, InterfaceDeclarationSyntax syntax, [NotNullWhen(true)] out Guid? guid, [NotNullWhen(false)] out DiagnosticInfo? diagnostic)
        {
            guid = null;
            AttributeData? guidAttr = null;
            AttributeData? _ = null; // Interface Attribute Type. We'll always assume IUnknown for now.
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
                diagnostic = DiagnosticInfo.Create(
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
            // ContainingSyntax does not implement GetHashCode
            return HashCode.Combine(Type, ThisInterfaceKey, BaseInterfaceKey, TypeDefinitionContext, InterfaceId);
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
