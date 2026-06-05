// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Denotes that this node's symbol definition is PCode, rather than PInstr.
    /// That is, the symbol can be used as a pointer to code, and may include a Thumb bit on ARM.
    /// This means that consumers wanting to reference this symbol as an RVA should adjust accordingly,
    /// usually with a -1 addend on ARM.
    /// </summary>
    public interface IPCodeSymbolNode
    {
    }
}
