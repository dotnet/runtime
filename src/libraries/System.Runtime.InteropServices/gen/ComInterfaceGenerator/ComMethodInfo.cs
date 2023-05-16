// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// Represents a method that has been determined to be a COM interface method. Only contains info immediately available from an IMethodSymbol and MethodDeclarationSyntax.
    /// </summary>
    internal sealed record ComMethodInfo(
        MethodDeclarationSyntax Syntax,
        string MethodName)
    {
        /// <summary>
        /// Returns a list of tuples of ComMethodInfo, IMethodSymbol, and Diagnostic. If ComMethodInfo is null, Diagnostic will not be null, and vice versa.
        /// </summary>
        public static SequenceEqualImmutableArray<(ComMethodInfo? ComMethod, IMethodSymbol Symbol, Diagnostic? Diagnostic)> GetMethodsFromInterface((ComInterfaceInfo ifaceContext, INamedTypeSymbol ifaceSymbol) data, CancellationToken ct)
        {
            var methods = ImmutableArray.CreateBuilder<(ComMethodInfo, IMethodSymbol, Diagnostic?)>();
            foreach (var member in data.ifaceSymbol.GetMembers())
            {
                if (IsComMethodCandidate(member))
                {
                    methods.Add(CalculateMethodInfo(data.ifaceContext, (IMethodSymbol)member, ct));
                }
            }
            return methods.ToImmutable().ToSequenceEqual();
        }

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

        private static bool IsComMethodCandidate(ISymbol member)
        {
            return member.Kind == SymbolKind.Method && !member.IsStatic;
        }

        private static (ComMethodInfo?, IMethodSymbol, Diagnostic?) CalculateMethodInfo(ComInterfaceInfo ifaceContext, IMethodSymbol method, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Debug.Assert(IsComMethodCandidate(method));

            // We only support methods that are defined in the same partial interface definition as the
            // [GeneratedComInterface] attribute.
            // This restriction not only makes finding the syntax for a given method cheaper,
            // but it also enables us to ensure that we can determine vtable method order easily.
            Location interfaceLocation = ifaceContext.Declaration.GetLocation();
            Location? methodLocationInAttributedInterfaceDeclaration = null;
            foreach (var methodLocation in method.Locations)
            {
                if (methodLocation.SourceTree == interfaceLocation.SourceTree
                    && interfaceLocation.SourceSpan.Contains(methodLocation.SourceSpan))
                {
                    methodLocationInAttributedInterfaceDeclaration = methodLocation;
                    break;
                }
            }

            if (methodLocationInAttributedInterfaceDeclaration is null)
            {
                return (null, method, Diagnostic.Create(GeneratorDiagnostics.MethodNotDeclaredInAttributedInterface, method.Locations.FirstOrDefault(), method.ToDisplayString()));
            }


            // Find the matching declaration syntax
            MethodDeclarationSyntax? comMethodDeclaringSyntax = null;
            foreach (var declaringSyntaxReference in method.DeclaringSyntaxReferences)
            {
                var declaringSyntax = declaringSyntaxReference.GetSyntax(ct);
                Debug.Assert(declaringSyntax.IsKind(SyntaxKind.MethodDeclaration));
                if (declaringSyntax.GetLocation().SourceSpan.Contains(methodLocationInAttributedInterfaceDeclaration.SourceSpan))
                {
                    comMethodDeclaringSyntax = (MethodDeclarationSyntax)declaringSyntax;
                    break;
                }
            }
            if (comMethodDeclaringSyntax is null)
            {
                return (null, method, Diagnostic.Create(GeneratorDiagnostics.CannotAnalyzeMethodPattern, method.Locations.FirstOrDefault(), method.ToDisplayString()));
            }

            var diag = GetDiagnosticIfInvalidMethodForGeneration(comMethodDeclaringSyntax, method);
            if (diag is not null)
            {
                return (null, method, diag);
            }
            var comMethodInfo = new ComMethodInfo(comMethodDeclaringSyntax, method.Name);
            return (comMethodInfo, method, null);
        }
    }
}
