// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop.Analyzers
{
    /// <summary>
    /// Validates that if an interface has GeneratedComInterfaceAttribute and <see cref="InterfaceTypeAttribute"/>,
    /// the <see cref="InterfaceTypeAttribute"/> is given a <see cref="ComInterfaceType"/> that is supported by the generator.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GeneratedComInterfaceAttributeAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(GeneratorDiagnostics.InterfaceTypeNotSupported);

        public static readonly ImmutableArray<ComInterfaceType> SupportedComInterfaceTypes = ImmutableArray.Create(ComInterfaceType.InterfaceIsIUnknown);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction((context) =>
            {
                INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.Symbol;
                if (typeSymbol.TypeKind != TypeKind.Interface)
                    return;

                ImmutableArray<AttributeData> customAttributes = typeSymbol.GetAttributes();
                if (customAttributes.Length == 0)
                    return;

                // Interfaces with both GeneratedComInterfaceAttribute and InterfaceTypeAttribute should only have [InterfaceTypeAttribute(InterfaceIsIUnknown)]
                if (GetAttribute(typeSymbol, TypeNames.GeneratedComInterfaceAttribute, out _)
                    && GetAttribute(typeSymbol, TypeNames.InterfaceTypeAttribute, out AttributeData? comInterfaceAttribute)
                    && !InterfaceTypeAttributeIsSupported(comInterfaceAttribute, out string unsupportedValue))
                {
                    context.ReportDiagnostic(comInterfaceAttribute.CreateDiagnosticInfo(GeneratorDiagnostics.InterfaceTypeNotSupported, unsupportedValue).ToDiagnostic());
                }
            }, SymbolKind.NamedType);
        }

        private static bool InterfaceTypeAttributeIsSupported(AttributeData comInterfaceAttribute, out string argument)
        {
            if (comInterfaceAttribute.ConstructorArguments.IsEmpty)
            {
                argument = "<empty>";
                return false;
            }
            TypedConstant ctorArg0 = comInterfaceAttribute.ConstructorArguments[0];
            ComInterfaceType interfaceType;

            argument = ctorArg0.ToCSharpString();
            switch (ctorArg0.Type.ToDisplayString())
            {
                case TypeNames.ComInterfaceType:
                    interfaceType = (ComInterfaceType)ctorArg0.Value;
                    break;
                case TypeNames.System_Int16:
                case TypeNames.@short:
                    interfaceType = (ComInterfaceType)(short)ctorArg0.Value;
                    break;
                default:
                    return false;
            }

            return SupportedComInterfaceTypes.Contains(interfaceType);
        }

        private static bool GetAttribute(ISymbol symbol, string attributeDisplayName, [NotNullWhen(true)] out AttributeData? attribute)
        {
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.ToDisplayString() == attributeDisplayName)
                {
                    attribute = attr;
                    return true;
                }
            }

            attribute = null;
            return false;
        }
    }
}
