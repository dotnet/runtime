// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// Represents an array of <see cref="System.Type"/> where initially each element of the array has the same DynamicallyAccessedMembers annotation.
    /// </summary>
    internal sealed record ArrayOfAnnotatedSystemTypeValue : SingleValue
    {
        private readonly ValueWithDynamicallyAccessedMembers _initialValue;

        public bool IsModified { get; private set; }

        public ArrayOfAnnotatedSystemTypeValue(ValueWithDynamicallyAccessedMembers value) => _initialValue = value;

        public override SingleValue DeepCopy()
        {
            return new ArrayOfAnnotatedSystemTypeValue(this);
        }

        public SingleValue GetAnyElementValue()
        {
            Debug.Assert(!IsModified);
            return _initialValue;
        }

        public void MarkModified() => IsModified = true;

        public override string ToString() => this.ValueToString(_initialValue.DynamicallyAccessedMemberTypes);
    }
}
