// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	partial record RuntimeMethodHandleValue
	{
		public override SingleValue DeepCopy () => this; // immutable value

		public override string ToString () => this.ValueToString ();
	}
}
