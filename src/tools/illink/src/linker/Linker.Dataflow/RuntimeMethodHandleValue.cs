// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;
using Mono.Cecil;


namespace ILLink.Shared.TrimAnalysis
{

	/// <summary>
	/// This is the System.RuntimeMethodHandle equivalent to a <see cref="SystemReflectionMethodBaseValue"/> node.
	/// </summary>
	partial record RuntimeMethodHandleValue
	{
		public RuntimeMethodHandleValue (MethodDefinition methodRepresented) => MethodRepresented = methodRepresented;

		public readonly MethodDefinition MethodRepresented;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (MethodRepresented);
	}
}