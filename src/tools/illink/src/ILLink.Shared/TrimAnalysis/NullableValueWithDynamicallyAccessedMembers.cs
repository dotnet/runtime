// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a Nullable<T> where T is an unknown value with DynamicallyAccessedMembers annotations.
	/// It is necessary to track the underlying type to ensure DynamicallyAccessedMembers annotations on the underlying type match the target parameters where the Nullable is used.
	/// </summary>
	internal sealed record NullableValueWithDynamicallyAccessedMembers : ValueWithDynamicallyAccessedMembers
	{
		public NullableValueWithDynamicallyAccessedMembers (in TypeProxy nullableType, in ValueWithDynamicallyAccessedMembers underlyingTypeValue)
		{
			Debug.Assert (nullableType.IsTypeOf (WellKnownType.System_Nullable_T));
			NullableType = nullableType;
			UnderlyingTypeValue = underlyingTypeValue;
		}

		public readonly TypeProxy NullableType;
		public readonly ValueWithDynamicallyAccessedMembers UnderlyingTypeValue;

		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => UnderlyingTypeValue.DynamicallyAccessedMemberTypes;
		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
			=> UnderlyingTypeValue.GetDiagnosticArgumentsForAnnotationMismatch ();

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (UnderlyingTypeValue, NullableType);
	}
}
