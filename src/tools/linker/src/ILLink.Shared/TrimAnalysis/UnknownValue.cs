// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	sealed record UnknownValue : SingleValue
	{
		private UnknownValue ()
		{
		}

		public static UnknownValue Instance { get; } = new UnknownValue ();

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString ();
	}
}
