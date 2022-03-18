// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeValue"/> node.
	/// </summary>
	sealed record RuntimeTypeHandleValue : SingleValue
	{
		public RuntimeTypeHandleValue (in TypeProxy representedType)
		{
			RepresentedType = representedType;
		}

		public readonly TypeProxy RepresentedType;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (RepresentedType);
	}
}
