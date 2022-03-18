// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	partial record GenericParameterValue
	{
		public GenericParameterValue (ITypeParameterSymbol typeParameterSymbol) => GenericParameter = new (typeParameterSymbol);

		public partial bool HasDefaultConstructorConstraint () =>
			GenericParameter.TypeParameterSymbol.HasConstructorConstraint |
			GenericParameter.TypeParameterSymbol.HasValueTypeConstraint |
			GenericParameter.TypeParameterSymbol.HasUnmanagedTypeConstraint;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => GenericParameter.TypeParameterSymbol.GetDynamicallyAccessedMemberTypes ();

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { GenericParameter.TypeParameterSymbol.Name, GenericParameter.TypeParameterSymbol.ContainingSymbol.GetDisplayName () };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString ()
			=> this.ValueToString (GenericParameter.TypeParameterSymbol, DynamicallyAccessedMemberTypes);
	}
}
