// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Runtime;

namespace ILCompiler.DependencyAnalysis
{
    internal static class IndirectionExtensions
    {
        /// <summary>
        /// Use this api to generate a reloc to a symbol that may be an indirection cell or not as a pointer
        /// </summary>
        /// <param name="symbol">symbol to reference</param>
        /// <param name="indirectionBit">value to OR in to the reloc to represent to runtime code that this pointer is an indirection. Defaults to IndirectionConstants.IndirectionCellPointer</param>
        /// <param name="delta">Delta from symbol start for value</param>
        public static void EmitPointerRelocOrIndirectionReference(ref this ObjectDataBuilder builder, ISymbolNode symbol, int delta = 0, int indirectionBit = IndirectionConstants.IndirectionCellPointer)
        {
            if (symbol.RepresentsIndirectionCell)
                delta |= indirectionBit;

            builder.EmitReloc(symbol, (builder.TargetPointerSize == 8) ? RelocType.IMAGE_REL_BASED_DIR64 : RelocType.IMAGE_REL_BASED_HIGHLOW, delta);
        }

        public static void EmitRelativeRelocOrIndirectionReference(ref this ObjectDataBuilder builder, ISymbolNode symbol, int delta = 0, int indirectionBit = IndirectionConstants.IndirectionCellPointer)
        {
            if (symbol.RepresentsIndirectionCell)
                delta |= indirectionBit;

            builder.EmitReloc(symbol, RelocType.IMAGE_REL_BASED_RELPTR32, delta);
        }
    }
}
