// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	sealed record NullValue : SingleValue
	{
		private NullValue ()
		{
		}

		public static NullValue Instance { get; } = new NullValue ();

		public override string ToString () => this.ValueToString ();
	}
}
