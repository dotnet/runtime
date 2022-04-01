// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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