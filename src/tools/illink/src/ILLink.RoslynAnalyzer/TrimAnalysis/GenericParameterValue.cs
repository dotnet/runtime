// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	partial record GenericParameterValue
	{
		public GenericParameterValue (ITypeParameterSymbol typeParameterSymbol)
			=> TypeParameterSymbol = typeParameterSymbol;

		public readonly ITypeParameterSymbol TypeParameterSymbol;

		public partial bool HasDefaultConstructorConstraint () => TypeParameterSymbol.HasConstructorConstraint | TypeParameterSymbol.HasValueTypeConstraint | TypeParameterSymbol.HasUnmanagedTypeConstraint;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => TypeParameterSymbol.GetDynamicallyAccessedMemberTypes ();

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { TypeParameterSymbol.Name, TypeParameterSymbol.ContainingSymbol.GetDisplayName () };

		public override string ToString ()
			=> this.ValueToString (TypeParameterSymbol, DynamicallyAccessedMemberTypes);
	}
}
