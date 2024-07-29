// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// A known string - such as the result of a ldstr.
	/// </summary>
	internal sealed partial record KnownStringValue : SingleValue
	{
		public KnownStringValue (string contents) => Contents = contents;

		public readonly string Contents;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString ("\"" + Contents + "\"");
	}
}
