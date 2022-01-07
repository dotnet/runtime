// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodParameterValue
	{
		public MethodParameterValue (IParameterSymbol parameterSymbol) => ParameterSymbol = parameterSymbol;

		public readonly IParameterSymbol ParameterSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes {
			get {
				var damt = ParameterSymbol.GetDynamicallyAccessedMemberTypes ();

				// Is this a property setter parameter?
				var parameterMethod = (IMethodSymbol) ParameterSymbol.ContainingSymbol;
				Debug.Assert (parameterMethod != null);
				// If there are conflicts between the setter and the property annotation,
				// the setter annotation wins. (But DAMT.None is ignored)
				if (parameterMethod!.MethodKind == MethodKind.PropertySet && damt == DynamicallyAccessedMemberTypes.None) {
					var property = (IPropertySymbol) parameterMethod.AssociatedSymbol!;
					Debug.Assert (property != null);
					damt = property!.GetDynamicallyAccessedMemberTypes ();
				}

				return damt;
			}
		}

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { ParameterSymbol.GetDisplayName (), ParameterSymbol.ContainingSymbol.GetDisplayName () };

		public override string ToString ()
			=> this.ValueToString (ParameterSymbol, DynamicallyAccessedMemberTypes);
	}
}
