// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILCompiler.ObjectWriter
{
    /// <summary>
    /// JIT enum used in the CFI code blob.
    /// </summary>
    internal enum CFI_OPCODE
    {
        CFI_ADJUST_CFA_OFFSET,    // Offset is adjusted relative to the current one.
        CFI_DEF_CFA_REGISTER,     // New register is used to compute CFA
        CFI_REL_OFFSET,           // Register is saved at offset from the current CFA
        CFI_DEF_CFA               // Take address from register and add offset to it.
    }
}
