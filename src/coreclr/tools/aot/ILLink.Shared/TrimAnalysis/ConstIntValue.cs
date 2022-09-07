// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// Represents a ldc on an int32.
	/// </summary>
	internal sealed record ConstIntValue : SingleValue
	{
		public ConstIntValue (int value) => Value = value;

		public readonly int Value;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (Value);
	}
}
