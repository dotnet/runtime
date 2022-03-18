// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodThisParameterValue
	{
		public MethodThisParameterValue (IMethodSymbol methodSymbol)
			: this (methodSymbol, methodSymbol.GetDynamicallyAccessedMemberTypes ()) { }

		public MethodThisParameterValue (IMethodSymbol methodSymbol, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> (MethodSymbol, DynamicallyAccessedMemberTypes) = (methodSymbol, dynamicallyAccessedMemberTypes);

		public readonly IMethodSymbol MethodSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { MethodSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);
	}
}
