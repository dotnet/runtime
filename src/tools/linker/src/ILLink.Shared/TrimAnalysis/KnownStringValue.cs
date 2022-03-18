// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// A known string - such as the result of a ldstr.
	/// </summary>
	sealed partial record KnownStringValue : SingleValue
	{
		public KnownStringValue (string contents) => Contents = contents;

		public readonly string Contents;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString ("\"" + Contents + "\"");
	}
}
