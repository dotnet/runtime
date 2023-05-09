// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    public sealed partial class ComInterfaceGenerator
    {
        /// <summary>
        /// Represents a method that has been determined to be a COM interface method. Only contains info immediately available from an IMethodSymbol and MethodDeclarationSyntax.
        /// </summary>
        private sealed record ComMethodInfo(
            [property: Obsolete] IMethodSymbol Symbol,
            MethodDeclarationSyntax Syntax,
            string MethodName,
            SequenceEqualImmutableArray<ParameterInfo> Parameters,
            Diagnostic? Diagnostic)
        {
            private static Diagnostic? GetDiagnosticIfInvalidMethodForGeneration(MethodDeclarationSyntax comMethodDeclaringSyntax, IMethodSymbol method)
            {
                // Verify the method has no generic types or defined implementation
                // and is not marked static or sealed
                if (comMethodDeclaringSyntax.TypeParameterList is not null
                    || comMethodDeclaringSyntax.Body is not null
                    || comMethodDeclaringSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
                {
                    return Diagnostic.Create(GeneratorDiagnostics.InvalidAttributedMethodSignature, comMethodDeclaringSyntax.Identifier.GetLocation(), method.Name);
                }

                // Verify the method does not have a ref return
                if (method.ReturnsByRef || method.ReturnsByRefReadonly)
                {
                    return Diagnostic.Create(GeneratorDiagnostics.ReturnConfigurationNotSupported, comMethodDeclaringSyntax.Identifier.GetLocation(), "ref return", method.ToDisplayString());
                }

                return null;
            }

            public static bool IsComMethod(ComInterfaceInfo ifaceContext, ISymbol member, [NotNullWhen(true)] out ComMethodInfo? comMethodInfo)
            {
                Diagnostic diag;
                comMethodInfo = null;
                Location interfaceLocation = ifaceContext.Declaration.GetLocation();
                if (member.Kind == SymbolKind.Method && !member.IsStatic)
                {
                    // We only support methods that are defined in the same partial interface definition as the
                    // [GeneratedComInterface] attribute.
                    // This restriction not only makes finding the syntax for a given method cheaper,
                    // but it also enables us to ensure that we can determine vtable method order easily.
                    Location? methodLocationInAttributedInterfaceDeclaration = null;
                    foreach (var methodLocation in member.Locations)
                    {
                        if (methodLocation.SourceTree == interfaceLocation.SourceTree
                            && interfaceLocation.SourceSpan.Contains(methodLocation.SourceSpan))
                        {
                            methodLocationInAttributedInterfaceDeclaration = methodLocation;
                            break;
                        }
                    }

                    // TODO: this should cause a diagnostic
                    if (methodLocationInAttributedInterfaceDeclaration is null)
                    {
                        return false;
                    }

                    // Find the matching declaration syntax
                    MethodDeclarationSyntax? comMethodDeclaringSyntax = null;
                    foreach (var declaringSyntaxReference in member.DeclaringSyntaxReferences)
                    {
                        var declaringSyntax = declaringSyntaxReference.GetSyntax();
                        Debug.Assert(declaringSyntax.IsKind(SyntaxKind.MethodDeclaration));
                        if (declaringSyntax.GetLocation().SourceSpan.Contains(methodLocationInAttributedInterfaceDeclaration.SourceSpan))
                        {
                            comMethodDeclaringSyntax = (MethodDeclarationSyntax)declaringSyntax;
                            break;
                        }
                    }
                    if (comMethodDeclaringSyntax is null)
                        throw new NotImplementedException("Found a method that was declared in the attributed interface declaration, but couldn't find the syntax for it.");

                    List<ParameterInfo> parameters = new();
                    foreach (var parameter in ((IMethodSymbol)member).Parameters)
                    {
                        parameters.Add(ParameterInfo.From(parameter));
                    }

                    diag = GetDiagnosticIfInvalidMethodForGeneration(comMethodDeclaringSyntax, (IMethodSymbol)member);

                    comMethodInfo = new((IMethodSymbol)member, comMethodDeclaringSyntax, member.Name, parameters.ToSequenceEqualImmutableArray(), diag);
                    return true;
                }
                return false;
            }
        }
    }

    internal record struct ParameterInfo(ManagedTypeInfo Type, string Name, RefKind RefKind, SequenceEqualImmutableArray<AttributeInfo> Attributes)
    {
        public static ParameterInfo From(IParameterSymbol parameter)
        {
            var attributes = new List<AttributeInfo>();
            foreach (var attribute in parameter.GetAttributes())
            {
                attributes.Add(AttributeInfo.From(attribute));
            }
            return new(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(parameter.Type), parameter.Name, parameter.RefKind, attributes.ToSequenceEqualImmutableArray());
        }
    }
}
