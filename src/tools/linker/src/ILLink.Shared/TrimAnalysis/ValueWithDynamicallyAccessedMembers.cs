// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    public abstract record ValueWithDynamicallyAccessedMembers : SingleValue
    {
        public abstract DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }

        public abstract IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch();
    }
}
