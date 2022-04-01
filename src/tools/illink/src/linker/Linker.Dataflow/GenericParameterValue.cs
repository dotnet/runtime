// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Mono.Linker.Dataflow;
using GenericParameter = Mono.Cecil.GenericParameter;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a System.Type value which represents generic parameter (basically result of typeof(T))
	/// Its actual type is unknown, but it can have annotations.
	/// </summary>
	partial record GenericParameterValue
	{
		public GenericParameterValue (GenericParameter genericParameter, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			GenericParameter = new (genericParameter);
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public partial bool HasDefaultConstructorConstraint () => GenericParameter.GenericParameter.HasDefaultConstructorConstraint;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { GenericParameter.GenericParameter.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (GenericParameter.GenericParameter) };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (GenericParameter, DynamicallyAccessedMemberTypes);
	}
}