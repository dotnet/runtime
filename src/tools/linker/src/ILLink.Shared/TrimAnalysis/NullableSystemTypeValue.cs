// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// This represents a Nullable<T> where T is a known SystemTypeValue.
    /// It is necessary to track the underlying type to propagate DynamicallyAccessedMembers annotations to the underlying type when applied to a Nullable.
    /// </summary>
    sealed record NullableSystemTypeValue : SingleValue
    {
        public NullableSystemTypeValue(in TypeProxy nullableType, in SystemTypeValue underlyingTypeValue)
        {
            Debug.Assert(nullableType.IsTypeOf(WellKnownType.System_Nullable_T));
            UnderlyingTypeValue = underlyingTypeValue;
            NullableType = nullableType;
        }
        public readonly TypeProxy NullableType;

        public readonly SystemTypeValue UnderlyingTypeValue;

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(UnderlyingTypeValue, NullableType);
    }
}
