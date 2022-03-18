// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="GenericParameterValue"/> node.
	/// </summary>
	sealed record RuntimeTypeHandleForGenericParameterValue : SingleValue
	{
		public readonly GenericParameterProxy GenericParameter;

		public RuntimeTypeHandleForGenericParameterValue (GenericParameterProxy genericParameter) => GenericParameter = genericParameter;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (GenericParameter);
	}
}
