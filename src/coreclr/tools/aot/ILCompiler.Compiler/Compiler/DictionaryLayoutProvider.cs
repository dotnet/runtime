// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysis;

namespace ILCompiler
{
    /// <summary>
    /// Provides generic dictionary layout information for a specific type or method.
    /// </summary>
    public abstract class DictionaryLayoutProvider
    {
        public abstract DictionaryLayoutNode GetLayout(TypeSystemEntity methodOrType);
    }

    /// <summary>
    /// Provides dictionary layout information that collects data during the compilation to build a dictionary layout
    /// for a type or method on demand.
    /// </summary>
    public sealed class LazyDictionaryLayoutProvider : DictionaryLayoutProvider
    {
        public override DictionaryLayoutNode GetLayout(TypeSystemEntity methodOrType)
        {
            return new LazilyBuiltDictionaryLayoutNode(methodOrType);
        }
    }
}
