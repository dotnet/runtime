// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a type handle of a Nullable<T> where T is an unknown value with DynamicallyAccessedMembers annotations. 
	/// It is necessary to track the underlying type to ensure DynamicallyAccessedMembers annotations on the underlying type match the target parameters where the Nullable is used.
	/// </summary>
	sealed record RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers : SingleValue
	{
		public RuntimeTypeHandleForNullableValueWithDynamicallyAccessedMembers (in TypeProxy nullableType, in SingleValue underlyingTypeValue)
		{
			Debug.Assert (nullableType.IsTypeOf ("System", "Nullable`1"));
			NullableType = nullableType;
			UnderlyingTypeValue = underlyingTypeValue;
		}

		public readonly TypeProxy NullableType;
		public readonly SingleValue UnderlyingTypeValue;

		public override SingleValue DeepCopy () => this; // This value is immutable	}

		public override string ToString () => this.ValueToString (UnderlyingTypeValue, NullableType);
	}
}