// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial record FieldValue
	{
		public FieldValue (IFieldSymbol fieldSymbol) => FieldSymbol = fieldSymbol;

		public readonly IFieldSymbol FieldSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => FieldSymbol.GetDynamicallyAccessedMemberTypes ();

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { FieldSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (FieldSymbol, DynamicallyAccessedMemberTypes);
	}
}
