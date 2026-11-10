// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// This represents a typeof of a generic parameter that was unwrapped with Nullable.GetUnderlyingType.
    /// </summary>
    internal sealed record NullableUnwrappedGenericParameterValue : ValueWithDynamicallyAccessedMembers
    {
        public NullableUnwrappedGenericParameterValue(in GenericParameterValue genericParameter)
        {
            GenericParameter = genericParameter;
        }

        public readonly GenericParameterValue GenericParameter;

        public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes => GenericParameter.DynamicallyAccessedMemberTypes;
        public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch()
            => GenericParameter.GetDiagnosticArgumentsForAnnotationMismatch();

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(GenericParameter, "Nullable-unwrapped");
    }
}
