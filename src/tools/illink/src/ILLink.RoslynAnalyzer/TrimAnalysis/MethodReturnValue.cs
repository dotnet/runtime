// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial record MethodReturnValue
	{
		public MethodReturnValue (IMethodSymbol methodSymbol, bool isNewObj)
			: this (methodSymbol, isNewObj, FlowAnnotations.GetMethodReturnValueAnnotation (methodSymbol))
		{
		}

		public MethodReturnValue (IMethodSymbol methodSymbol, bool isNewObj, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			Debug.Assert (!isNewObj || methodSymbol.MethodKind == MethodKind.Constructor, "isNewObj can only be true for constructors");
			MethodSymbol = methodSymbol;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
			StaticType = new (isNewObj ? methodSymbol.ContainingType : methodSymbol.ReturnType);
		}

		public readonly IMethodSymbol MethodSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { MethodSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (MethodSymbol, DynamicallyAccessedMemberTypes);
	}
}
