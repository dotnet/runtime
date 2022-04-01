// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using TypeDefinition = Mono.Cecil.TypeDefinition;


namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// Return value from a method
	/// </summary>
	partial record MethodReturnValue : IValueWithStaticType
	{
		public MethodReturnValue (TypeDefinition? staticType, MethodDefinition method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType;
			Method = method;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { DiagnosticUtilities.GetMethodSignatureDisplayName (Method) };

		public TypeDefinition? StaticType { get; }

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (Method, DynamicallyAccessedMemberTypes);
	}
}