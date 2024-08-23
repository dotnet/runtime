// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial record FieldValue
	{
		public FieldValue (IFieldSymbol fieldSymbol)
		{
			FieldSymbol = fieldSymbol;
			StaticType = new (fieldSymbol.Type);
		}

		public readonly IFieldSymbol FieldSymbol;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => FieldSymbol.GetDynamicallyAccessedMemberTypes ();

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { FieldSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (FieldSymbol, DynamicallyAccessedMemberTypes);
	}
}
