using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.TrimAnalysis;

namespace ILLink.RoslynAnalyzer
{
    internal interface IGenericInstantiationAnalysis
    {
        void ProcessGenericInstantiation(
            ITypeSymbol typeArgument,
            ITypeParameterSymbol typeParameter,
            FeatureContext featureContext,
            TypeNameResolver typeNameResolver,
            ISymbol owningSymbol,
            Location location,
            Action<Diagnostic>? reportDiagnostic);
    }
}
