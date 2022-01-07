// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodThisParameterValue
	{
		public MethodThisParameterValue (IMethodSymbol methodSymbol) => MethodSymbol = methodSymbol;

		public readonly IMethodSymbol MethodSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => MethodSymbol.GetDynamicallyAccessedMemberTypes ();

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { MethodSymbol.GetDisplayName () };

		public override string ToString () => this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);
	}
}
