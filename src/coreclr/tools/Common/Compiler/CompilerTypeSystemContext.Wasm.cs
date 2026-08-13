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
        private readonly Dictionary<int, TypeDesc> _structsBySize = new Dictionary<int, TypeDesc>();
        private readonly Dictionary<int, TypeDesc> _returnStructsBySize = new Dictionary<int, TypeDesc>();
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
        /// Caches a struct type by its element size, so RaiseSignature can retrieve a real
        /// type of that size. Only the first struct encountered for a given size is retained.
        /// </summary>
        public void CacheStructBySize(TypeDesc type)
        {
            int size = type.GetElementSize().AsInt;
            if (size <= 0)
                return;

            lock (_structCacheLock)
            {
                _structsBySize.TryAdd(size, type);
            }
        }

        /// <summary>
        /// Caches a struct return type by size. Kept apart from the parameter cache because the two
        /// classes are not interchangeable: a multi-slot type spells <c>S&lt;N&gt;</c> as a return but
        /// re-lowers to its slot form as a parameter, so letting one answer for the other would give
        /// an ordinary same-sized struct that type's larger alignment.
        /// </summary>
        public void CacheReturnStructBySize(TypeDesc type)
        {
            int size = type.GetElementSize().AsInt;
            if (size <= 0)
                return;

            lock (_structCacheLock)
            {
                _returnStructsBySize.TryAdd(size, type);
            }
        }

        /// <summary>
        /// Gets a previously cached struct return type of the specified byte size, falling back to a
        /// parameter of that size. Returns null if neither has been cached.
        /// </summary>
        public TypeDesc GetCachedReturnStructOfSize(int size)
        {
            lock (_structCacheLock)
            {
                if (_returnStructsBySize.TryGetValue(size, out TypeDesc result))
                {
                    return result;
                }
            }

            return GetCachedStructOfSize(size);
        }

        public TypeDesc GetCachedStructOfSize(int size)
        {
            lock (_structCacheLock)
            {
                if (_structsBySize.TryGetValue(size, out TypeDesc result))
                    return result;
            }

            return null;
        }
    }
}
