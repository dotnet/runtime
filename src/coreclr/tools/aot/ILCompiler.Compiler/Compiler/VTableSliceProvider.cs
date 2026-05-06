// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    /// <summary>
    /// Provides VTable information for a specific type.
    /// </summary>
    public abstract class VTableSliceProvider
    {
        internal abstract VTableSliceNode GetSlice(TypeDesc type);
    }

    /// <summary>
    /// Provides VTable information that collects data during the compilation to build a VTable for a type.
    /// </summary>
    public sealed class LazyVTableSliceProvider : VTableSliceProvider
    {
        internal override VTableSliceNode GetSlice(TypeDesc type)
        {
            return new LazilyBuiltVTableSliceNode(type);
        }
    }
}
