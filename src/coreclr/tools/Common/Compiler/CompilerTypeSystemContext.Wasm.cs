// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private readonly object _structCacheLock = new object();
        private readonly Dictionary<(int Size, int Alignment), TypeDesc> _structsBySizeAndAlignment = new Dictionary<(int, int), TypeDesc>();
        private volatile TypeDesc _cachedEmptyStruct;
        private volatile TypeDesc _wasmV128Type;
        private volatile TypeDesc _wasmInt128Type;
        private volatile TypeDesc _wasmV256Type;
        private volatile TypeDesc _wasmV512Type;

        /// <summary>
        /// Gets the type RaiseSignature produces for the 'V' encoding. All v128 types share the same
        /// wasm ABI (16 bytes, 16-byte aligned), so any one of them round-trips 'V' identically;
        /// resolving a fixed one keeps raising independent of the order lowering encountered them in.
        /// </summary>
        public TypeDesc WasmV128Type
        {
            get
            {
                TypeDesc type = _wasmV128Type;
                if (type is null)
                {
                    var vector128 = (MetadataType)SystemModule.GetType("System.Runtime.Intrinsics"u8, "Vector128`1"u8);
                    _wasmV128Type = type = vector128.MakeInstantiatedType(GetWellKnownType(WellKnownType.Byte));
                }

                return type;
            }
        }

        /// <summary>
        /// Gets the type RaiseSignature produces for an elevated multi-slot encoding ('l2', 'V2', 'V4'),
        /// where the digit is the factor by which the type's alignment is elevated above the slot's
        /// natural alignment. A thunk is keyed by its signature string, so every type spelling an
        /// encoding shares one and raising can only resolve a stand-in. For 'V2' and 'V4' the
        /// candidates are instantiations of a single generic type, so any element type stands for
        /// the rest; for 'l2' they are unrelated types -- Int128, UInt128, Decimal128 -- agreeing in
        /// nothing but the signature. Both are sound because the signature is all the thunk's frame
        /// layout derives from, which is what the asserts below hold the stand-in to. Resolving a
        /// fixed type also keeps raising independent of the order lowering encountered them in.
        /// </summary>
        public TypeDesc GetWasmElevatedType(char slot, int elevation)
        {
            TypeDesc type = (slot, elevation) switch
            {
                ('l', 2) => _wasmInt128Type ??= SystemModule.GetType("System"u8, "Int128"u8),
                ('V', 2) => _wasmV256Type ??= InstantiateOverByte("Vector256`1"u8),
                ('V', 4) => _wasmV512Type ??= InstantiateOverByte("Vector512`1"u8),
                _ => throw new InvalidOperationException($"Unknown elevated signature encoding: {slot}{elevation}")
            };

            // The slot layout alone does not imply alignment, so the resolved type must supply it:
            // a 16-byte 8-aligned struct is spelled 'S16', not 'l2'.
            int slotSize = slot == 'l' ? 8 : 16;
            Debug.Assert(((DefType)type).InstanceFieldSize.AsInt == slotSize * elevation);
            Debug.Assert(((DefType)type).InstanceFieldAlignment.AsInt == slotSize * elevation);

            return type;

            TypeDesc InstantiateOverByte(ReadOnlySpan<byte> name) =>
                ((MetadataType)SystemModule.GetType("System.Runtime.Intrinsics"u8, name))
                    .MakeInstantiatedType(GetWellKnownType(WellKnownType.Byte));
        }

        /// <summary>
        /// Gets the first empty struct type encountered during lowering, or null if none has been seen.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'e' encoding.
        /// </summary>
        public TypeDesc CachedEmptyStruct => _cachedEmptyStruct;

        /// <summary>
        /// Caches an empty struct type discovered during lowering. Only the first one is retained.
        /// </summary>
        public void CacheEmptyStruct(TypeDesc type)
        {
            _cachedEmptyStruct ??= type;
        }

        /// <summary>
        /// Caches a struct type by its element size and argument alignment, so RaiseSignature can
        /// retrieve a real type with the same Wasm argument layout. Only the first struct
        /// encountered for a given (size, alignment) pair is retained.
        /// </summary>
        public void CacheStructBySize(TypeDesc type, int alignment)
        {
            int size = type.GetElementSize().AsInt;
            if (size <= 0)
                return;

            lock (_structCacheLock)
            {
                _structsBySizeAndAlignment.TryAdd((size, alignment), type);
            }
        }

        /// <summary>
        /// Gets a previously cached struct type of the specified byte size and argument alignment.
        /// Returns null if no such struct has been cached.
        /// Used by RaiseSignature to produce a roundtrippable type for the 'S&lt;N&gt;'/'A&lt;N&gt;' encodings.
        /// </summary>
        public TypeDesc GetCachedStructOfSize(int size, int alignment)
        {
            lock (_structCacheLock)
            {
                if (_structsBySizeAndAlignment.TryGetValue((size, alignment), out TypeDesc result))
                    return result;
            }

            return null;
        }
    }
}
