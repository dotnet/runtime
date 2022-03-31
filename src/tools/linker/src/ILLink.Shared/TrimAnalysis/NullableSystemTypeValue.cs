// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a Nullable<T> where T is a known SystemTypeValue.
	/// It is necessary to track the underlying type to propagate DynamicallyAccessedMembers annotations to the underlying type when applied to a Nullable.
	/// </summary>
	sealed record NullableSystemTypeValue : SingleValue
	{
		public NullableSystemTypeValue (in TypeProxy nullableType, in SystemTypeValue underlyingTypeValue)
		{
			Debug.Assert (nullableType.IsTypeOf ("System", "Nullable`1"));
			UnderlyingTypeValue = underlyingTypeValue;
			NullableType = nullableType;
		}
		public readonly TypeProxy NullableType;

		public readonly SystemTypeValue UnderlyingTypeValue;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (UnderlyingTypeValue, NullableType);
	}
}
