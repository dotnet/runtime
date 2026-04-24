// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private static TypeDesc RaiseType(WasmValueType valueType, TypeSystemContext context)
        {
            return valueType switch
            {
                WasmValueType.I32 => context.GetWellKnownType(WellKnownType.Int32),
                WasmValueType.I64 => context.GetWellKnownType(WellKnownType.Int64),
                WasmValueType.F32 => context.GetWellKnownType(WellKnownType.Single),
                WasmValueType.F64 => context.GetWellKnownType(WellKnownType.Double),
                WasmValueType.V128 => throw new NotSupportedException("SIMD types are not supported in this version of the compiler"),
                _ => throw new InvalidOperationException("Unknown WasmValueType: " + valueType),
            };
        }

        public static MethodSignature RaiseSignature(WasmFuncType funcType, TypeSystemContext context)
        {
            List<TypeDesc> parameters = new List<TypeDesc>();
            for (int i = 1; i < funcType.Params.Types.Length - 1; i++)
            {
                parameters.Add(RaiseType(funcType.Params.Types[i], context));
            }
            TypeDesc returnType = funcType.Returns.Types.Length > 0 ? RaiseType(funcType.Returns.Types[0], context) : context.GetWellKnownType(WellKnownType.Void);
            return new MethodSignature(MethodSignatureFlags.Static, 0, returnType, parameters.ToArray());
        }

        /// <summary>
        /// Gets the Wasm-level signature for a given MethodDesc.
        ///
        /// Parameters for managed Wasm calls have the following layout:
        /// i32 (SP), loweredParam0, ..., loweredParamN, i32 (PE entrypoint)
        ///
        /// For unmanaged callers only (reverse P/Invoke), the layout is simply the native signature
        /// which is just the lowered parameters+return.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static WasmFuncType GetSignature(MethodDesc method)
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

        public static WasmFuncType GetSignature(MethodSignature signature, LoweringFlags flags)
        {
            TypeDesc returnType = signature.ReturnType;
            WasmValueType pointerType = (signature.ReturnType.Context.Target.PointerSize == 4) ? WasmValueType.I32 : WasmValueType.I64;

            // Determine if the return value is via a return buffer
            //
            TypeDesc loweredReturnType = LowerToAbiType(returnType);
            bool hasReturnBuffer = false;
            bool returnIsVoid = false;
            bool hasThis = false;
            bool explicitThis = false;

            if (loweredReturnType == null)
            {
                hasReturnBuffer = true;
                returnIsVoid = true;
            }
            else if (loweredReturnType.IsVoid)
            {
                returnIsVoid = true;
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
                result.Add(pointerType); // Stack pointer parameter

                if (hasThis)
                {
                    result.Add(pointerType);
                }

                if (hasReturnBuffer)
                {
                    result.Add(pointerType);
                }
            }

            if (flags.HasFlag(LoweringFlags.HasGenericContextArg))
            {
                result.Add(pointerType); // generic context
            }

            if (flags.HasFlag(LoweringFlags.IsAsyncCall))
            {
                result.Add(pointerType); // async continuation
            }

            for (int i = explicitThis ? 1 : 0; i < signature.Length; i++)
            {
                result.Add(LowerType(signature[i]));
            }

            if (!flags.HasFlag(LoweringFlags.IsUnmanagedCallersOnly))
            {
                result.Add(pointerType); // PE entrypoint parameter
            }

            WasmResultType ps = new(result.ToArray());
            WasmResultType ret = returnIsVoid ? new(Array.Empty<WasmValueType>())
                : new([LowerType(loweredReturnType)]);

            return new WasmFuncType(ps, ret);
        }
    }
}
