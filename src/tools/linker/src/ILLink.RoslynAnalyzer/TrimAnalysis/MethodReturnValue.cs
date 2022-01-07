// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record MethodReturnValue
	{
		public MethodReturnValue (IMethodSymbol methodSymbol) => MethodSymbol = methodSymbol;

		public readonly IMethodSymbol MethodSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes {
			get {
				var returnDamt = MethodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType ();

				// Is this a property getter?
				// If there are conflicts between the getter and the property annotation,
				// the getter annotation wins. (But DAMT.None is ignored)
				if (MethodSymbol.MethodKind is MethodKind.PropertyGet && returnDamt == DynamicallyAccessedMemberTypes.None) {
					var property = (IPropertySymbol) MethodSymbol.AssociatedSymbol!;
					Debug.Assert (property != null);
					returnDamt = property!.GetDynamicallyAccessedMemberTypes ();
				}

				return returnDamt;
			}
		}

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { MethodSymbol.GetDisplayName () };

		public override string ToString () => this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);
	}
}
