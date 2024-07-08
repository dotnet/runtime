// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a type handle obtained from a Type with DynamicallyAccessedMembers annotations.
	/// </summary>
	internal sealed record RuntimeTypeHandleForValueWithDynamicallyAccessedMembers : SingleValue
	{
		public RuntimeTypeHandleForValueWithDynamicallyAccessedMembers (in ValueWithDynamicallyAccessedMembers underlyingTypeValue)
		{
			UnderlyingTypeValue = underlyingTypeValue;
		}

		public readonly ValueWithDynamicallyAccessedMembers UnderlyingTypeValue;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (UnderlyingTypeValue);
	}
}
