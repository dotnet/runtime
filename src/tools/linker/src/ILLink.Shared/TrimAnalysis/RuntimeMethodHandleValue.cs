// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// This is the System.RuntimeMethodHandle equivalent to a <see cref="SystemReflectionMethodBaseValue"/> node.
    /// </summary>
    sealed partial record RuntimeMethodHandleValue : SingleValue
    {
        public RuntimeMethodHandleValue(in MethodProxy representedMethod)
        {
            RepresentedMethod = representedMethod;
        }

        public readonly MethodProxy RepresentedMethod;

        public override SingleValue DeepCopy() => this; // immutable value

        public override string ToString() => this.ValueToString();
    }
}
