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

        public static TypeDesc LowerToAbiType(TypeDesc type)
        {
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
            if (Internal.TypeSystem.Interop.InteropTypes.IsSystemRuntimeIntrinsicsVector128T(type.Context, type))
            {
                return true;
            }

            // Vector<T> is target-sized, so it is only a v128 when the target's maximum SIMD width is
            // 128-bit (i.e. it is exactly 16 bytes). This matches the JIT recognizing it as TYP_SIMD16
            // via getVectorTByteLength() and keeps the ABI correct should wasm later gain wider vectors.
            return type is DefType vectorOfT &&
                   VectorOfTFieldLayoutAlgorithm.IsVectorOfTType(vectorOfT) &&
                   type.GetElementSize().AsInt == 16;
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
            'V' => ((CompilerTypeSystemContext)context).CachedV128Type
                   ?? throw new InvalidOperationException("Encountered 'V' in signature but no v128 type was cached during lowering"),
            _ => throw new InvalidOperationException($"Unknown signature char: {c}")
        };

        private static int ParseStructSize(string sig, ref int pos)
        {
            Debug.Assert(sig[pos] == 'S');
            pos++; // skip 'S'
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
                returnType = ((CompilerTypeSystemContext)context).GetCachedStructOfSize(structSize);
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
                else if (c == 'S')
                {
                    int structSize = ParseStructSize(sig, ref pos);
                    TypeDesc cachedStruct = ((CompilerTypeSystemContext)context).GetCachedStructOfSize(structSize);
                    Debug.Assert(cachedStruct is not null, $"No cached struct of size {structSize} for parameter in signature '{sig}'");
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
                $"RaiseSignature roundtrip failed: input='{wasmSignature.SignatureString}', roundtripped='{roundtripped.SignatureString}'");

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
                    ((CompilerTypeSystemContext)returnType.Context).CacheStructBySize(returnType);
                }
            }
            else if (loweredReturnType.IsVoid)
            {
                returnIsVoid = true;
                sigBuilder.Append('v');
            }
            else
            {
                WasmValueType returnWasmType = LowerType(loweredReturnType);
                if (returnWasmType == WasmValueType.V128)
                {
                    ((CompilerTypeSystemContext)returnType.Context).CacheV128Type(loweredReturnType);
                }
                sigBuilder.Append(WasmValueTypeToSigChar(returnWasmType));
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
                    sigBuilder.Append('S');
                    sigBuilder.Append(paramSize);
                    result.Add(pointerType);
                    ((CompilerTypeSystemContext)paramType.Context).CacheStructBySize(paramType);
                }
                else
                {
                    WasmValueType paramWasmType = LowerType(loweredParamType);
                    if (paramWasmType == WasmValueType.V128)
                    {
                        ((CompilerTypeSystemContext)paramType.Context).CacheV128Type(loweredParamType);
                    }
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
