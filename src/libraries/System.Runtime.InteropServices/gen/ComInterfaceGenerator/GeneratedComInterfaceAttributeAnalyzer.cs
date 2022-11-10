// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

#nullable enable
namespace Microsoft.Interop
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class GeneratedComInterfaceAttributeAnalyzer : DiagnosticAnalyzer
    {
        private const string _generatedComInterfaceAttributeName = "GeneratedComInterfaceAttribute";
        private const string _generatedComInterfaceAttributeNamespace = "System.Runtime.InteropServices.Marshalling";

        private const string _interfaceTypeAttributeName = "InterfaceTypeAttribute";
        private const string _interfaceTypeAttributeNamespace = "System.Runtime.InteropServices";

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(GeneratorDiagnostics.InterfaceTypeNotSupported);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction((context) =>
            {
                INamedTypeSymbol typeSymbol = (INamedTypeSymbol)context.Symbol;
                if (typeSymbol.TypeKind != TypeKind.Interface)
                    return;
                ImmutableArray<AttributeData> customAttributes = typeSymbol.GetAttributes();
                if (customAttributes.Length == 0)
                    return;

                // Interfaces should not have both GeneratedComInterfaceTypeAttribute and InterfaceTypeAttribute
                if (GetGeneratedComInterfaceAttribute(typeSymbol, out AttributeData? generatedComInterfaceAttribute)
                    && GetComInterfaceAttribute(typeSymbol, out AttributeData? comInterfaceAttribute))
                {
                    if (!InterfaceTypeAttributeIsIUnknown(comInterfaceAttribute))
                    {
                        context.ReportDiagnostic(comInterfaceAttribute.CreateDiagnostic(GeneratorDiagnostics.InterfaceTypeNotSupported));
                    }
                }
            }, SymbolKind.NamedType);
        }

        private static bool InterfaceTypeAttributeIsIUnknown(AttributeData comInterfaceAttribute)
        {
            if (comInterfaceAttribute.ConstructorArguments.IsEmpty)
                return false;

            if ((int)comInterfaceAttribute.ConstructorArguments[0].Value == (int)ComInterfaceType.InterfaceIsIUnknown)
                return true;

            return false;
        }

        private static bool GetGeneratedComInterfaceAttribute(ISymbol symbol, [NotNullWhen(true)] out AttributeData? generatedComInterfaceAttribute)
        {
            return GetAttribute(symbol, _generatedComInterfaceAttributeNamespace, _generatedComInterfaceAttributeName, out generatedComInterfaceAttribute);
        }

        private static bool GetComInterfaceAttribute(ISymbol symbol, [NotNullWhen(true)] out AttributeData? comInterfaceAttribute)
        {
            return GetAttribute(symbol, _interfaceTypeAttributeNamespace, _interfaceTypeAttributeName, out comInterfaceAttribute);
        }

        private static bool GetAttribute(ISymbol symbol, string attributeNamespace, string attributeName, out AttributeData? attribute)
        {
            foreach (AttributeData attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass?.Name == attributeName
                    && attr.AttributeClass?.ContainingNamespace.ToDisplayString() == attributeNamespace)
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
