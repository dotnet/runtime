// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared.TrimAnalysis
{
    /// <summary>
    /// A known Assembly value, represented by its simple name.
    /// For example, the result of typeof(SomeType).Assembly.
    /// </summary>
    internal sealed record AssemblyValue : SingleValue
    {
        public AssemblyValue(string assemblyName) => AssemblyName = assemblyName;

        public readonly string AssemblyName;

        public override SingleValue DeepCopy() => this; // This value is immutable

        public override string ToString() => this.ValueToString(AssemblyName);
    }
}
