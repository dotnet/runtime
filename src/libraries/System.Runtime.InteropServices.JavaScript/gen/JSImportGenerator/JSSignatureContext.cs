// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Threading;

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop.JavaScript
{
    internal sealed record JSSignatureContext
    {
        private static SymbolDisplayFormat TypeAndContainingTypesStyle { get; } = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes
        );

        private static SymbolDisplayFormat TypeContainingTypesAndNamespacesStyle { get; } = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

        internal static readonly string GeneratorName = typeof(JSImportGenerator).Assembly.GetName().Name;

        internal static readonly string GeneratorVersion = typeof(JSImportGenerator).Assembly.GetName().Version.ToString();

        public SignatureContext SignatureContext { get; private init; }

        public static JSSignatureContext Create(
            IMethodSymbol method,
            StubEnvironment env,
            GeneratorDiagnosticsBag diagnostics,
            CancellationToken token)
        {
            // Cancel early if requested
            token.ThrowIfCancellationRequested();

            ImmutableArray<IUseSiteAttributeParser> useSiteAttributeParsers = ImmutableArray.Create<IUseSiteAttributeParser>(new JSMarshalAsAttributeParser(env.Compilation));
            var jsMarshallingAttributeParser = new MarshallingInfoParser(
                diagnostics,
                new MethodSignatureElementInfoProvider(env.Compilation, diagnostics, method, useSiteAttributeParsers),
                useSiteAttributeParsers,
                ImmutableArray.Create<IMarshallingInfoAttributeParser>(new JSMarshalAsAttributeParser(env.Compilation)),
                ImmutableArray.Create<ITypeBasedMarshallingInfoProvider>(new FallbackJSMarshallingInfoProvider()));
            SignatureContext sigContext = SignatureContext.Create(method, jsMarshallingAttributeParser, env, new CodeEmitOptions(SkipInit: true), typeof(JSImportGenerator).Assembly);

            string stubTypeFullName = method.ContainingType.ToDisplayString(TypeContainingTypesAndNamespacesStyle);

            // there could be multiple method signatures with the same name, get unique signature name
            uint hash = 17;
            unchecked
            {
                foreach (var param in sigContext.ElementTypeInformation)
                {
                    hash = hash * 31 + (uint)param.ManagedType.FullTypeName.GetHashCode();
                }
            };
            int typesHash = Math.Abs((int)hash);

            var fullName = $"{method.ContainingType.ToDisplayString()}.{method.Name}";
            string qualifiedName = GetFullyQualifiedMethodName(env, method);

            return new JSSignatureContext()
            {
                SignatureContext = sigContext,
                TypesHash = typesHash,
                StubTypeFullName = stubTypeFullName,
                MethodName = fullName,
                QualifiedMethodName = qualifiedName,
                BindingName = "__signature_" + method.Name + "_" + typesHash,
                AssemblyName = env.Compilation.AssemblyName,
            };
        }

        private static string GetFullyQualifiedMethodName(StubEnvironment env, IMethodSymbol method)
        {
            // Mono style nested class name format.
            string typeName = method.ContainingType.ToDisplayString(TypeAndContainingTypesStyle).Replace(".", "/");

            if (!method.ContainingType.ContainingNamespace.IsGlobalNamespace)
                typeName = $"{method.ContainingType.ContainingNamespace.ToDisplayString()}.{typeName}";

            return $"[{env.Compilation.AssemblyName}]{typeName}:{method.Name}";
        }
        public string? StubTypeFullName { get; init; }
        public int TypesHash { get; init; }

        public string MethodName { get; init; }
        public string QualifiedMethodName { get; init; }
        public string BindingName { get; init; }
        public string AssemblyName { get; init; }

        public override int GetHashCode()
        {
            throw new UnreachableException();
        }
    }
}
