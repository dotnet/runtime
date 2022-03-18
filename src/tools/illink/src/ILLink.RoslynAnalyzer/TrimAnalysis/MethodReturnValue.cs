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
	partial record MethodReturnValue
	{
		public MethodReturnValue (IMethodSymbol methodSymbol)
		{
			MethodSymbol = methodSymbol;
			DynamicallyAccessedMemberTypes = FlowAnnotations.GetMethodReturnValueAnnotation (methodSymbol);
		}

		public MethodReturnValue (IMethodSymbol methodSymbol, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			MethodSymbol = methodSymbol;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly IMethodSymbol MethodSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { MethodSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);
	}
}
