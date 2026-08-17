// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using ILCompiler;
using ILCompiler.DependencyAnalysis.Wasm;

using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public static partial class WasmLowering
    {
        public static MethodSignature GetStringCtorActualSignature(MethodSignature signature)
        {
            Debug.Assert(signature.Context.GetWellKnownType(WellKnownType.String).GetMethod(".ctor"u8, signature) != null);
            Debug.Assert(signature.GenericParameterCount == 0);
            Debug.Assert(signature.Flags == 0);

            TypeDesc[] arguments = new TypeDesc[signature.Length];
            for (int i = 0; i < signature.Length; i++)
            {
                arguments[i] = signature[i];
            }

            return new MethodSignature(MethodSignatureFlags.Static, 0, signature.Context.GetWellKnownType(WellKnownType.String), arguments);
        }

        // The Wasm "basic C ABI" passes structs that contain one
        // primitive field as that primitive field.
        //
        // Analyze the type and determine if it should be passed
        // as a primitive, and if so, which type. If not, return
        // null.

        public static TypeDesc LowerToAbiType(TypeDesc type) => LowerToAbiType(type, out _);

        private static TypeDesc LowerToAbiType(TypeDesc type, out TypeDesc multiSegmentType)
        {
            multiSegmentType = null;

            // Types split across several wasm parameters are not a single ABI primitive, but the
            // caller still needs to know which one the type unwrapped to.
            if (IsMultiSegmentType(type))
            {
                multiSegmentType = type;
                return null;
            }

            // Vector128<T> and a 128-bit Vector<T> are wasm v128 ABI primitives passed by value.
            if (IsWasmV128Type(type))
            {
                return type;
            }

            if (!(type.IsValueType && !type.IsPrimitive))
            {
                return type;
            }

            int size = type.GetElementSize().AsInt;

            while (true)
            {
                FieldDesc firstField = null;
                int numIntroducedFields = 0;
                foreach (FieldDesc field in type.GetFields())
                {
                    if (!field.IsStatic)
                    {
                        firstField ??= field;
                        numIntroducedFields++;
                    }

                    if (numIntroducedFields > 1)
                    {
                        break;
                    }
                }

                if (numIntroducedFields != 1)
                {
                    // Multi-field aggregates (including a homogeneous 2x v128) use the generic by-ref
                    // struct ABI; the wasm C ABI has no HFA/HVA concept. Only emscripten's opt-in
                    // experimental multivalue ABI expands these into per-field registers, which we
                    // don't target.
                    return null;
                }

                TypeDesc firstFieldElementType = firstField.FieldType;

                if (firstFieldElementType.GetElementSize().AsInt != size)
                {
                    // One-field struct with padding.
                    return null;
                }

                type = firstFieldElementType;

                // A single-field wrapper struct is passed as the type it wraps, matching clang: a
                // struct wrapping an __int128 lowers to the same two i64 parameters the bare type does.
                if (IsMultiSegmentType(type))
                {
                    multiSegmentType = type;
                    return null;
                }

                // A single-field wrapper struct around a v128 lowers to the v128 primitive, matching
                // emscripten, which passes a struct wrapping a v128 as a v128.
                if (IsWasmV128Type(type))
                {
                    return type;
                }

                if (type.IsValueType && !type.IsPrimitive)
                {
                    continue;
                }

                return type;
            }
        }

        /// <summary>
        /// Reports how a type is split when the wasm ABI passes it by value across several
        /// parameters, because no single wasm value type is wide enough to hold it. Returns false
        /// for every other type. These types are still returned via a hidden buffer.
        /// </summary>
        /// <remarks>
        /// The special behavior is limited to the known CoreLib types <see cref="System.Int128"/>,
        /// <see cref="System.UInt128"/>, <see cref="System.Numerics.Decimal128"/>,
        /// <c>Vector256&lt;T&gt;</c>, and <c>Vector512&lt;T&gt;</c>. Ordinary aggregates remain
        /// indirect even if their fields have the same shape. The size and alignment checks also
        /// verify that the selected type has the layout required by its multi-slot ABI.
        /// </remarks>
        public static bool TryGetMultiSegmentLayout(TypeDesc type, out WasmValueType slotType, out int slotCount)
        {
            slotType = default;
            slotCount = 0;

            // A single-field wrapper is passed as the type it wraps, so classify what it unwraps to.
            LowerToAbiType(type, out TypeDesc multiSegmentType);
            if (multiSegmentType is null)
            {
                return false;
            }

            slotType = GetSlotType(multiSegmentType).Value;

            // A wrapper only unwraps when its field fills it exactly, so the declared type's size
            // is also the wrapped type's size.
            int size = type.GetElementSize().AsInt;
            int slotSize = GetMultiSegmentSlotSize(slotType);
            Debug.Assert((size % slotSize) == 0);

            slotCount = size / slotSize;
            return true;
        }

        /// <summary>
        /// Determines whether a type is itself passed by value across several wasm parameters,
        /// ignoring any single-field struct wrapping it. See <see cref="TryGetMultiSegmentLayout"/>
        /// for why each condition is needed.
        /// </summary>
        private static bool IsMultiSegmentType(TypeDesc type)
        {
            if (type is not DefType defType || !IsKnownMultiSegmentType(defType))
            {
                return false;
            }

            int size = defType.InstanceFieldSize.AsInt;
            if (defType.InstanceFieldAlignment.AsInt != size)
            {
                return false;
            }

            WasmValueType? slotType = GetSlotType(type);
            return (slotType is not null) && (size > GetMultiSegmentSlotSize(slotType.Value));
        }

        /// <summary>
        /// Determines whether a type is one of the CoreLib types with special multi-slot Wasm ABI
        /// behavior. This check must remain in sync with <c>IsWasmMultiSlotTypeHandle</c> in
        /// <c>vm/wasm/helpers.cpp</c>.
        /// </summary>
        private static bool IsKnownMultiSegmentType(DefType type)
        {
            if (type.GetTypeDefinition() is not MetadataType typeDefinition ||
                typeDefinition.Module != type.Context.SystemModule)
            {
                return false;
            }

            if (Int128FieldLayoutAlgorithm.IsIntegerType(type))
            {
                return true;
            }

            if (DecimalFieldLayoutAlgorithm.IsDecimalFloatingPointType(type))
            {
                return type.Name == "Decimal128"u8;
            }

            return VectorFieldLayoutAlgorithm.IsVectorType(type) &&
                (type.Name == "Vector256`1"u8 || type.Name == "Vector512`1"u8) &&
                VectorFieldLayoutAlgorithm.IsSupportedVectorBaseType(type.Instantiation[0]);
        }

        /// <summary>
        /// Walks a known multi-slot CoreLib type's first fields down to the wasm value type its slots
        /// use. This is safe only after <see cref="IsKnownMultiSegmentType"/> succeeds: the known
        /// integer and decimal types have homogeneous <c>ulong</c> fields, and the known vectors
        /// have homogeneous vector fields. It is not valid for an arbitrary aggregate.
        /// </summary>
        private static WasmValueType? GetSlotType(TypeDesc type)
        {
            Debug.Assert(type is DefType defType && IsKnownMultiSegmentType(defType));

            // Three iterations cover the deepest supported chain:
            // Vector512<T> -> Vector256<T> -> Vector128<T>.
            for (int depth = 0; depth < 3; depth++)
            {
                if (IsWasmV128Type(type))
                {
                    return WasmValueType.V128;
                }

                if (type.IsPrimitive)
                {
                    // Only a wasm scalar is a valid slot; a narrower one would mean the type is not
                    // an even multiple of its slots.
                    WasmValueType lowered = LowerType(type);
                    return lowered == WasmValueType.I64 ? lowered : null;
                }

                if (type is not DefType || !type.IsValueType)
                {
                    return null;
                }

                // A generic intrinsic whose base type is not a supported vector element -- the
                // shared __Canon form, say -- is not ABI-classifiable, and its fields are an
                // implementation detail rather than its slots. Same guard IsWasmV128Type applies,
                // and the same one GetWasmSlotSize applies in the runtime; without it a
                // Vector512<__Canon> walks past the v128 check into Vector128's raw ulong fields
                // and reports eight i64 slots.
                if (type.IsIntrinsic && (type.Instantiation.Length == 1) &&
                    !VectorFieldLayoutAlgorithm.IsSupportedVectorBaseType(type.Instantiation[0]))
                {
                    return null;
                }

                FieldDesc firstField = null;
                foreach (FieldDesc field in type.GetFields())
                {
                    if (!field.IsStatic)
                    {
                        firstField = field;
                        break;
                    }
                }

                if (firstField is null)
                {
                    return null;
                }

                type = firstField.FieldType;
            }

            return null;
        }

        /// <summary>
        /// Size in bytes of a wasm slot used by <see cref="TryGetMultiSegmentLayout"/>. Taken from
        /// the wasm value type, never from a JIT or managed type: TYP_SIMD8 and TYP_SIMD12 also
        /// occupy a v128 slot but report their own narrower sizes.
        /// </summary>
        public static int GetMultiSegmentSlotSize(WasmValueType slotType)
        {
            Debug.Assert(slotType is WasmValueType.I64 or WasmValueType.V128);
            return slotType == WasmValueType.I64 ? 8 : 16;
        }

        /// <summary>
        /// Determines whether a type is passed and returned by value as a wasm <c>v128</c>, matching
        /// the SIMD types the JIT recognizes as <c>TYP_SIMD16</c> on wasm. This is
        /// <see cref="System.Runtime.Intrinsics.Vector128{T}"/> and a 128-bit
        /// <see cref="System.Numerics.Vector{T}"/>, in both cases only when <c>T</c> is a supported
        /// primitive numeric base type. Other SIMD types (Vector2/3/4, Vector64/256/512&lt;T&gt;, ...)
        /// and non-primitive instantiations (e.g. the shared <c>__Canon</c> form) are not ABI
        /// primitives and continue to use the generic struct ABI.
        /// </summary>
        private static bool IsWasmV128Type(TypeDesc type)
        {
            if (!type.IsIntrinsic ||
                type.Instantiation.Length != 1 ||
                !VectorFieldLayoutAlgorithm.IsSupportedVectorBaseType(type.Instantiation[0]))
            {
                return false;
            }

            // Vector128<T> is always a 16-byte v128.
            //
            // Vector<T> is target-sized, so it is only a v128 when the target's maximum SIMD width is
            // 128-bit (i.e. it is exactly 16 bytes). This matches the JIT recognizing it as TYP_SIMD16
            // via getVectorTByteLength() and keeps the ABI correct should wasm later gain wider vectors.
            bool isV128 = Internal.TypeSystem.Interop.InteropTypes.IsSystemRuntimeIntrinsicsVector128T(type.Context, type) ||
                          (type is DefType vectorOfT &&
                           VectorOfTFieldLayoutAlgorithm.IsVectorOfTType(vectorOfT) &&
                           type.GetElementSize().AsInt == 16);

            // The wasm ABI gives every v128 a 16-byte aligned argument slot, so a smaller metadata
            // alignment would silently misplace it relative to the runtime's own ArgIterator layout.
            Debug.Assert(!isV128 || ((DefType)type).InstanceFieldAlignment.AsInt == 16,
                $"v128 type {type} must be 16-byte aligned");

            return isV128;
        }

        public static WasmValueType LowerType(TypeDesc type)
        {
            WasmValueType pointerType = (type.Context.Target.PointerSize == 4) ? WasmValueType.I32 : WasmValueType.I64;

            if (IsWasmV128Type(type))
            {
                return WasmValueType.V128;
            }

            TypeDesc abiType = LowerToAbiType(type);

            if (abiType == null)
            {
                return pointerType;
            }

            switch (abiType.UnderlyingType.Category)
            {
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.Byte:
                case TypeFlags.SByte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                    return WasmValueType.I32;

                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                    return WasmValueType.I64;

                case TypeFlags.Single:
                    return WasmValueType.F32;

                case TypeFlags.Double:
                    return WasmValueType.F64;

                // Pointer and reference types
                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                    return pointerType;

                default:
                    throw new NotSupportedException($"Unknown wasm mapping for type: {type.UnderlyingType.Category}");
            }
        }

        /// <summary>
        /// Determines whether a type is an empty struct (no instance fields) that should
        /// be ignored in the WebAssembly calling convention per the BasicCABI spec.
        /// </summary>
        // WASM-TODO: This currently always returns false because .NET pads empty structs
        // to size 1. A proper implementation should check for 0 non-static fields.
        // See https://github.com/dotnet/runtime/issues/127361
        public static bool IsEmptyStruct(TypeDesc type) => false;

        /// <summary>
        /// Maps a WasmValueType to its single-character signature encoding.
        /// </summary>
        private static char WasmValueTypeToSigChar(WasmValueType vt) => vt switch
        {
            WasmValueType.I32 => 'i',
            WasmValueType.I64 => 'l',
            WasmValueType.F32 => 'f',
            WasmValueType.F64 => 'd',
            WasmValueType.V128 => 'V',
            _ => throw new NotSupportedException($"Unknown WasmValueType: {vt}")
        };

        private static TypeDesc RaiseSigChar(char c, TypeSystemContext context) => c switch
        {
            'i' => context.GetWellKnownType(WellKnownType.Int32),
            'l' => context.GetWellKnownType(WellKnownType.Int64),
            'f' => context.GetWellKnownType(WellKnownType.Single),
            'd' => context.GetWellKnownType(WellKnownType.Double),
            'V' => ((CompilerTypeSystemContext)context).WasmV128Type,
            _ => throw new InvalidOperationException($"Unknown signature char: {c}")
        };

        private static int ParseStructSize(string sig, ref int pos)
        {
            Debug.Assert(sig[pos] is 'S' or 'A');
            pos++; // skip 'S'/'A'
            int start = pos;
            while (pos < sig.Length && char.IsDigit(sig[pos]))
            {
                pos++;
            }

            return int.Parse(sig.AsSpan(start, pos - start));
        }

        public static MethodSignature RaiseSignature(WasmSignature wasmSignature, TypeSystemContext context)
        {
            string sig = wasmSignature.SignatureString;
            int pos = 0;

            // Parse return type
            TypeDesc returnType;
            if (sig[pos] == 'v')
            {
                returnType = context.GetWellKnownType(WellKnownType.Void);
                pos++;
            }
            else if (sig[pos] == 'S')
            {
                int structSize = ParseStructSize(sig, ref pos);
                returnType = ((CompilerTypeSystemContext)context).GetCachedReturnStructOfSize(structSize);
                Debug.Assert(returnType is not null, $"No cached struct of size {structSize} for return type in signature '{sig}'");
            }
            else
            {
                returnType = RaiseSigChar(sig[pos], context);
                pos++;
            }

            List<TypeDesc> parameters = new List<TypeDesc>();
            bool hasThis = false;
            bool isAsyncCall = false;
            bool hasGenericContextBeforeAsync = false;

            if (pos < sig.Length && sig[pos] == 'T')
            {
                hasThis = true;
                pos++;
            }

            // A generic context precedes the async marker in the Wasm ABI; it is encoded with the
            // hidden-pointer char (matching the encode side), i32 on wasm32 and i64 on wasm64.
            char hiddenParamChar = (context.Target.PointerSize == 4) ? 'i' : 'l';
            if ((pos + 1 < sig.Length) && (sig[pos] == hiddenParamChar) && (sig[pos + 1] == 'a'))
            {
                hasGenericContextBeforeAsync = true;
                parameters.Add(RaiseSigChar(sig[pos], context));
                pos++;
            }

            if (pos < sig.Length && sig[pos] == 'a')
            {
                isAsyncCall = true;
                pos++;
            }

            // Parse explicit parameters (everything until the portable-entrypoint suffix or end of string).
            while (pos < sig.Length && sig[pos] != 'p')
            {
                char c = sig[pos];
                if (c == 'T')
                {
                    // 'this' parameter — not added as explicit param, sets hasThis flag
                    hasThis = true;
                    pos++;
                }
                else if (c == 'e')
                {
                    // Empty struct — include the cached empty struct type for roundtrip fidelity
                    TypeDesc emptyStruct = ((CompilerTypeSystemContext)context).CachedEmptyStruct;
                    Debug.Assert(emptyStruct is not null, "Encountered 'e' in signature but no empty struct was cached during lowering");
                    parameters.Add(emptyStruct);
                    pos++;
                }
                else if (((c == 'l') || (c == 'V')) && (pos + 1 < sig.Length) && char.IsDigit(sig[pos + 1]))
                {
                    int elevation = sig[pos + 1] - '0';
                    parameters.Add(((CompilerTypeSystemContext)context).GetWasmElevatedType(c, elevation));
                    pos += 2;
                }
                else if (c is 'S' or 'A')
                {
                    bool isAlignedStruct = c == 'A';
                    int structSize = ParseStructSize(sig, ref pos);
                    CompilerTypeSystemContext compilerContext = (CompilerTypeSystemContext)context;
                    TypeDesc cachedStruct = isAlignedStruct
                        ? compilerContext.GetCachedAlignedStructOfSize(structSize)
                        : compilerContext.GetCachedStructOfSize(structSize);
                    Debug.Assert(cachedStruct is not null,
                        $"No cached {(isAlignedStruct ? "aligned " : "")}struct of size {structSize} for parameter in signature '{sig}'");
                    parameters.Add(cachedStruct);
                }
                else
                {
                    parameters.Add(RaiseSigChar(c, context));
                    pos++;
                }
            }

            bool isManaged = pos < sig.Length && sig[pos] == 'p';
            MethodSignatureFlags flags = hasThis ? MethodSignatureFlags.None : MethodSignatureFlags.Static;
            if (!isManaged)
            {
                flags |= MethodSignatureFlags.UnmanagedCallingConvention;
            }

            MethodSignature result = new MethodSignature(flags, 0, returnType, parameters.ToArray());

            WasmSignature roundtripped = GetSignature(result, isAsyncCall ? LoweringFlags.IsAsyncCall : LoweringFlags.None);
            string roundtrippedStr = roundtripped.SignatureString;
            if (hasGenericContextBeforeAsync && isAsyncCall)
            {
                // The roundtrip re-encodes the generic context as a leading parameter, so it emits the
                // async marker before the hidden-pointer char; swap them back to match the input ordering.
                roundtrippedStr = roundtrippedStr.Replace($"a{hiddenParamChar}", $"{hiddenParamChar}a");
            }
            Debug.Assert(roundtrippedStr.Equals(wasmSignature.SignatureString, StringComparison.Ordinal),
                $"RaiseSignature roundtrip failed: input='{wasmSignature.SignatureString}', roundtripped='{roundtrippedStr}'");

            return result;
        }

        /// <summary>
        /// Gets the Wasm-level signature for a given MethodDesc.
        /// The signature string format is documented in docs/design/coreclr/botr/readytorun-format.md
        /// (section "Wasm Signature String Encoding").
        ///
        /// Parameters for managed Wasm calls have the following layout:
        /// i32 (SP), loweredParam0, ..., loweredParamN, i32 (PE entrypoint)
        ///
        /// For unmanaged callers only (reverse P/Invoke), the layout is simply the native signature
        /// which is just the lowered parameters+return.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static WasmSignature GetSignature(MethodDesc method)
        {
            return GetSignature(method.Signature, GetLoweringFlags(method));
        }

        public static LoweringFlags GetLoweringFlags(MethodDesc method)
        {
            LoweringFlags flags = 0;
            if (method.RequiresInstMethodDescArg() || method.RequiresInstMethodTableArg())
            {
                flags |= LoweringFlags.HasGenericContextArg;
            }
            if (method.IsAsyncCall())
            {
                flags |= LoweringFlags.IsAsyncCall;
            }
            if (method.IsUnmanagedCallersOnly)
            {
                flags |= LoweringFlags.IsUnmanagedCallersOnly;
            }
            return flags;
        }

        [Flags]
        public enum LoweringFlags
        {
            None = 0x0,
            HasGenericContextArg = 0x1,
            IsAsyncCall = 0x2,
            IsUnmanagedCallersOnly = 0x4
        }

        public static WasmSignature GetSignature(MethodSignature signature, LoweringFlags flags)
        {
            if (!flags.HasFlag(LoweringFlags.IsUnmanagedCallersOnly) && signature.Flags.HasFlag(MethodSignatureFlags.UnmanagedCallingConvention))
            {
                flags = flags | LoweringFlags.IsUnmanagedCallersOnly;
            }

            TypeDesc returnType = signature.ReturnType;
            WasmValueType pointerType = (signature.ReturnType.Context.Target.PointerSize == 4) ? WasmValueType.I32 : WasmValueType.I64;
            char hiddenParamChar = WasmValueTypeToSigChar(pointerType);

            StringBuilder sigBuilder = new StringBuilder();

            // Determine if the return value is via a return buffer
            //
            TypeDesc loweredReturnType = LowerToAbiType(returnType);
            bool hasReturnBuffer = false;
            bool returnIsVoid = false;
            bool hasThis = false;
            bool explicitThis = false;

            if (loweredReturnType == null)
            {
                if (IsEmptyStruct(returnType))
                {
                    // Empty struct return — treated as void with no return buffer
                    returnIsVoid = true;
                    sigBuilder.Append('v');
                }
                else
                {
                    hasReturnBuffer = true;
                    returnIsVoid = true;
                    int returnSize = returnType.GetElementSize().AsInt;
                    sigBuilder.Append('S');
                    sigBuilder.Append(returnSize);

                    // A multi-slot type spells 'S<N>' only as a return; as a parameter it re-lowers
                    // to its slot form. Keep it in the return cache alone, so an ordinary same-sized
                    // struct parameter does not raise with this type's larger alignment.
                    CompilerTypeSystemContext returnContext = (CompilerTypeSystemContext)returnType.Context;
                    returnContext.CacheReturnStructBySize(returnType);
                    if (!TryGetMultiSegmentLayout(returnType, out _, out _))
                    {
                        int returnAlignment = CorInfoImpl.GetClassAlignmentRequirementStatic((DefType)returnType);
                        returnContext.CacheStruct(returnType, returnAlignment > 8);
                    }
                }
            }
            else if (loweredReturnType.IsVoid)
            {
                returnIsVoid = true;
                sigBuilder.Append('v');
            }
            else
            {
                sigBuilder.Append(WasmValueTypeToSigChar(LowerType(loweredReturnType)));
            }

            // Reserve space for potential implicit this, stack pointer parameter, portable entrypoint parameter,
            // generic context, async continuation, and return buffer
            ArrayBuilder<WasmValueType> result = new(signature.Length + 6);

            if (!signature.IsStatic)
            {
                hasThis = true;

                if (signature.IsExplicitThis)
                {
                    explicitThis = true;
                }
            }

            if (flags.HasFlag(LoweringFlags.IsUnmanagedCallersOnly)) // reverse P/Invoke
            {
                if (hasReturnBuffer)
                {
                    result.Add(pointerType);
                }
            }
            else // managed call
            {
                result.Add(pointerType); // Stack pointer parameter (encoded via 'p' suffix, not here)

                if (hasThis)
                {
                    result.Add(pointerType);
                    sigBuilder.Append('T');
                }

                if (hasReturnBuffer)
                {
                    result.Add(pointerType);
                }
            }

            if (flags.HasFlag(LoweringFlags.HasGenericContextArg))
            {
                result.Add(pointerType); // generic context
                sigBuilder.Append(hiddenParamChar);
            }

            if (flags.HasFlag(LoweringFlags.IsAsyncCall))
            {
                result.Add(pointerType); // async continuation
                sigBuilder.Append('a');
            }

            for (int i = explicitThis ? 1 : 0; i < signature.Length; i++)
            {
                TypeDesc paramType = signature[i];
                TypeDesc loweredParamType = LowerToAbiType(paramType);

                if (loweredParamType == null)
                {
                    if (IsEmptyStruct(paramType))
                    {
                        // Empty struct — not emitted as a WebAssembly argument
                        sigBuilder.Append('e');
                        ((CompilerTypeSystemContext)signature.ReturnType.Context).CacheEmptyStruct(paramType);
                        continue;
                    }

                    // Struct that cannot be lowered to a single primitive — passed by reference
                    int paramSize = paramType.GetElementSize().AsInt;
                    if (TryGetMultiSegmentLayout(paramType, out WasmValueType slotType, out int slotCount))
                    {
                        // Passed by value across several wasm parameters, matching the wasm C ABI.
                        // Spelled '<slot><elevation>'; the elevation factor equals the slot count
                        // for every type in the wasm ABI today, and the encoding cannot express
                        // them differing. See readytorun-format.md.
                        Debug.Assert(slotCount is >= 2 and <= 9,
                            $"Slot count {slotCount} is not a single digit, so raising cannot read it back");
                        sigBuilder.Append(WasmValueTypeToSigChar(slotType));
                        sigBuilder.Append(slotCount);
                        for (int slot = 0; slot < slotCount; slot++)
                        {
                            result.Add(slotType);
                        }
                    }
                    else
                    {
                        Debug.Assert(paramType is DefType);
                        int paramAlignment = CorInfoImpl.GetClassAlignmentRequirementStatic((DefType)paramType);
                        bool requiresAlignedSlot = paramAlignment > 8;
                        sigBuilder.Append(requiresAlignedSlot ? 'A' : 'S');
                        sigBuilder.Append(paramSize);
                        ((CompilerTypeSystemContext)paramType.Context).CacheStruct(paramType, requiresAlignedSlot);
                        result.Add(pointerType);
                    }
                }
                else
                {
                    WasmValueType paramWasmType = LowerType(loweredParamType);
                    sigBuilder.Append(WasmValueTypeToSigChar(paramWasmType));
                    result.Add(paramWasmType);
                }
            }

            if (!flags.HasFlag(LoweringFlags.IsUnmanagedCallersOnly))
            {
                result.Add(pointerType); // PE entrypoint parameter (encoded via 'p' suffix)
                sigBuilder.Append('p');
            }

            WasmResultType ps = new(result.ToArray());
            WasmResultType ret = returnIsVoid ? new(Array.Empty<WasmValueType>())
                : new([LowerType(loweredReturnType)]);

            return new WasmSignature(new WasmFuncType(ps, ret), sigBuilder.ToString());
        }
    }
}
