// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler
{
    /// <summary>
    /// The side table wasm signature lowering needs in order to be reversible. Lowering erases a
    /// struct down to its size (the <c>S&lt;N&gt;</c> encoding) and an empty struct down to
    /// <c>'e'</c>, so raising a signature back to a <see cref="MethodSignature"/> needs a real type
    /// to hand back. Lowering records the types it saw here and raising looks them up.
    ///
    /// This is an interface rather than a direct <see cref="CompilerTypeSystemContext"/> reference
    /// so that <see cref="Internal.JitInterface.WasmLowering"/> can be linked into tools that only
    /// compute signatures and do not want the rest of the compiler. See ILCompiler.Wasm.Lowering.
    /// </summary>
    public interface IWasmTypeCacheContext
    {
        /// <summary>
        /// The type the <c>'V'</c> encoding raises to. All v128 types share the same wasm ABI
        /// (16 bytes, 16-byte aligned), so any one of them round-trips <c>'V'</c> identically.
        /// </summary>
        TypeDesc WasmV128Type { get; }

        /// <summary>
        /// The first empty struct seen during lowering, or <see langword="null"/> if there was none.
        /// </summary>
        TypeDesc CachedEmptyStruct { get; }

        /// <summary>
        /// Records an empty struct seen during lowering. Only the first one is retained.
        /// </summary>
        void CacheEmptyStruct(TypeDesc type);

        /// <summary>
        /// Records a struct seen during lowering, keyed by its element size. Only the first struct
        /// encountered for a given size is retained.
        /// </summary>
        void CacheStructBySize(TypeDesc type);

        /// <summary>
        /// Returns a previously cached struct of the given byte size, or <see langword="null"/> if
        /// no struct of that size has been cached.
        /// </summary>
        TypeDesc GetCachedStructOfSize(int size);
    }
}
