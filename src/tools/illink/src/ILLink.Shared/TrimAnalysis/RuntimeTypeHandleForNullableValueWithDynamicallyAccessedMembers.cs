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
	/// This represents a type handle of a Nullable<T> where T is an unknown value with DynamicallyAccessedMembers annotations.
	/// It is necessary to track the underlying type to ensure DynamicallyAccessedMembers annotations on the underlying type match the target parameters where the Nullable is used.
	/// </summary>
	internal sealed record RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers : SingleValue
	{
		public RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers (in TypeProxy nullableType, in SingleValue underlyingTypeValue)
		{
			Debug.Assert (nullableType.IsTypeOf (WellKnownType.System_Nullable_T));
			NullableType = nullableType;
			UnderlyingTypeValue = underlyingTypeValue;
		}

		public readonly TypeProxy NullableType;
		public readonly SingleValue UnderlyingTypeValue;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (UnderlyingTypeValue, NullableType);
	}
}
