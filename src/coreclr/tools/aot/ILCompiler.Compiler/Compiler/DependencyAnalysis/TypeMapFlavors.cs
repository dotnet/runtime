// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// The kinds of values a type map group can be asked for by the compiled program.
    /// The numeric values are also the state of the group in the type map hash table
    /// of the image (0 is reserved for invalid groups).
    /// </summary>
    [Flags]
    public enum TypeMapFlavors
    {
        None = 0,

        /// <summary>
        /// The mapped types (TypeMapping.GetOrCreate*TypeMapping).
        /// </summary>
        Types = 1,

        /// <summary>
        /// Data returned by ITypeMapValue&lt;TValue&gt; implemented by the mapped types (TypeMapping.TryGetValue).
        /// A group can have any number of these, each one for a different TValue. The state of the group in the image has a single
        /// Values bit regardless of how many TValue flavors the group has, and the descriptors of the TValue flavors follow the state.
        /// </summary>
        Values = 2,
    }
}
