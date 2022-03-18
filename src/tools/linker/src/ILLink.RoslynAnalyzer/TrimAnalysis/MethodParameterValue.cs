// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodParameterValue
	{
		public MethodParameterValue (IParameterSymbol parameterSymbol)
			: this (parameterSymbol, FlowAnnotations.GetMethodParameterAnnotation (parameterSymbol)) { }

		public MethodParameterValue (IParameterSymbol parameterSymbol, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> (ParameterSymbol, DynamicallyAccessedMemberTypes) = (parameterSymbol, dynamicallyAccessedMemberTypes);

		public readonly IParameterSymbol ParameterSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { ParameterSymbol.GetDisplayName (), ParameterSymbol.ContainingSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString ()
			=> this.ValueToString (ParameterSymbol, DynamicallyAccessedMemberTypes);
	}
}
