// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { GenericParameter.GenericParameter.Name, DiagnosticUtilities.GetGenericParameterDeclaringMemberDisplayName (GenericParameter.GenericParameter) };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (GenericParameter, DynamicallyAccessedMemberTypes);
	}
}