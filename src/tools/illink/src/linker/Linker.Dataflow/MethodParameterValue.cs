// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using TypeDefinition = Mono.Cecil.TypeDefinition;


namespace ILLink.Shared.TrimAnalysis
{

	/// <summary>
	/// A value that came from a method parameter - such as the result of a ldarg.
	/// </summary>
	partial record MethodParameterValue : IValueWithStaticType
	{
		public MethodParameterValue (TypeDefinition? staticType, MethodDefinition method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Method = method;
			ParameterIndex = parameterIndex;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		/// <summary>
		/// This is the index of non-implicit parameter - so the index into MethodDefinition.Parameters array.
		/// It's NOT the IL parameter index which could be offset by 1 if the method has an implicit this.
		/// </summary>
		public readonly int ParameterIndex;

		public ParameterDefinition ParameterDefinition => Method.Parameters[ParameterIndex];

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { DiagnosticUtilities.GetParameterNameForErrorMessage (ParameterDefinition), DiagnosticUtilities.GetMethodSignatureDisplayName (Method) };

		public TypeDefinition? StaticType { get; }

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (Method, ParameterIndex, DynamicallyAccessedMemberTypes);
	}
}