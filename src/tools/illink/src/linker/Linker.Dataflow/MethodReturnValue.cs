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
	/// Return value from a method
	/// </summary>
	internal partial record MethodReturnValue
	{
		public MethodReturnValue (TypeDefinition? staticType, MethodDefinition method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			StaticType = staticType == null ? null : new (staticType);
			Method = method;
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
		}

		public readonly MethodDefinition Method;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> new string[] { DiagnosticUtilities.GetMethodSignatureDisplayName (Method) };

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (Method, DynamicallyAccessedMemberTypes);
	}
}
