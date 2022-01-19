// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodParameterValue
	{
		public MethodParameterValue (IParameterSymbol parameterSymbol) => ParameterSymbol = parameterSymbol;

		public readonly IParameterSymbol ParameterSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => FlowAnnotations.GetMethodParameterAnnotation (ParameterSymbol);

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { ParameterSymbol.GetDisplayName (), ParameterSymbol.ContainingSymbol.GetDisplayName () };

		public override string ToString ()
			=> this.ValueToString (ParameterSymbol, DynamicallyAccessedMemberTypes);
	}
}
