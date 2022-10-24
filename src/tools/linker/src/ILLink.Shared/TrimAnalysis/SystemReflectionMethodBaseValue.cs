// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// This is a known System.Reflection.MethodBase value.  MethodRepresented is the 'value' of the MethodBase.
    /// </summary>
    sealed partial record SystemReflectionMethodBaseValue : SingleValue
    {
        public SystemReflectionMethodBaseValue(MethodProxy representedMethod) => RepresentedMethod = representedMethod;

        public readonly MethodProxy RepresentedMethod;

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(RepresentedMethod);
    }
}
