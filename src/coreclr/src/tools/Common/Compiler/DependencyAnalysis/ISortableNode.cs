// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    public interface ISortableNode
    {
#if !SUPPORT_JIT
        /// <summary>
        /// Gets an identifier that is the same for all instances of this <see cref="ObjectNode"/>
        /// descendant, but different from the <see cref="ClassCode"/> of any other descendant.
        /// </summary>
        /// <remarks>
        /// This is really just a number, ideally produced by "new Random().Next(int.MinValue, int.MaxValue)".
        /// If two manage to conflict (which is pretty unlikely), just make a new one...
        /// </remarks>
        int ClassCode { get; }

        int CompareToImpl(ISortableNode other, CompilerComparer comparer);
#endif
    }
}
