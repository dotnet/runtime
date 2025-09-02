// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using System.Collections.Immutable;
using ILLink.RoslynAnalyzer;

namespace ILLink.Shared.TrimAnalysis
{
    internal partial struct RequireDynamicallyAccessedMembersAction
    {
        readonly Location _location;
        readonly Action<Diagnostic>? _reportDiagnostic;
        readonly ReflectionAccessAnalyzer _reflectionAccessAnalyzer;
        readonly TypeNameResolver _typeNameResolver;
        readonly ISymbol _owningSymbol;
        readonly DataFlowAnalyzerContext _context;
        readonly FeatureContext _featureContext;
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
#pragma warning disable IDE0060 // Unused parameters - should be removed once methods are actually implemented

        public RequireDynamicallyAccessedMembersAction(
            DataFlowAnalyzerContext context,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            Location location,
            Action<Diagnostic>? reportDiagnostic,
            ReflectionAccessAnalyzer reflectionAccessAnalyzer,
            ISymbol owningSymbol)
        {
            _context = context;
            _featureContext = featureContext;
            _typeNameResolver = typeNameResolver;
            _location = location;
            _reportDiagnostic = reportDiagnostic;
            _reflectionAccessAnalyzer = reflectionAccessAnalyzer;
            _owningSymbol = owningSymbol;
            _diagnosticContext = new(location, reportDiagnostic);
        }

        public partial bool TryResolveTypeNameAndMark(string typeName, bool needsAssemblyName, out TypeProxy type)
        {
            var diagnosticContext = new DiagnosticContext(_location, _reportDiagnostic);
            if (_reflectionAccessAnalyzer.TryResolveTypeNameAndMark(typeName, diagnosticContext, needsAssemblyName, out ITypeSymbol? foundType))
            {
                if (foundType is INamedTypeSymbol namedType && namedType.IsGenericType)
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(_context, _featureContext, _typeNameResolver, _owningSymbol, _location, namedType, _reportDiagnostic);

                type = new TypeProxy(foundType);
                return true;
            }

            type = default;
            return false;
        }

        private partial void MarkTypeForDynamicallyAccessedMembers(in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes) =>
            _reflectionAccessAnalyzer.GetReflectionAccessDiagnostics(_location, type.Type, dynamicallyAccessedMemberTypes);
    }
}
