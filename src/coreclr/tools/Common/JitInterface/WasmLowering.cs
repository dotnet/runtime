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
    public static class WasmLowering
    {
        // The Wasm "basic C ABI" passes structs that contain one
        // primitive field as that primitive field.
        //
        // Analyze the type and determine if it should be passed
        // as a primitive, and if so, which type. If not, return
        // null.

        public static TypeDesc LowerToAbiType(TypeDesc type)
        {
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
                    return null;
                }

                TypeDesc firstFieldElementType = firstField.FieldType;

                if (firstFieldElementType.GetElementSize().AsInt != size)
                {
                    // One-field struct with padding.
                    return null;
                }

                type = firstFieldElementType;

                if (type.IsValueType && !type.IsPrimitive)
                {
                    continue;
                }

                return type;
            }
        }

        public static WasmValueType LowerType(TypeDesc type)
        {
            WasmValueType pointerType = (type.Context.Target.PointerSize == 4) ? WasmValueType.I32 : WasmValueType.I64;

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
            'V' => throw new NotSupportedException("SIMD types are not supported in this version of the compiler"),
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

            // Parse parameters (everything until 'p' suffix or end of string)
            List<TypeDesc> parameters = new List<TypeDesc>();
            bool hasThis = false;

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

            WasmSignature roundtripped = GetSignature(result, LoweringFlags.None);
            Debug.Assert(roundtripped.Equals(wasmSignature),
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
                sigBuilder.Append(hiddenParamChar);
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
                    WasmValueType paramWasmType = LowerType(paramType);
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
