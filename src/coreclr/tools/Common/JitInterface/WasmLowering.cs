// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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

        public static TypeDesc LowerTypeForWasm(TypeDesc type)
        {
            if (!(type.IsValueType && !type.IsPrimitiveNumeric))
            {
                Debug.Fail("Non-struct types should be passed directly in Wasm.");
                return null;
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

                if (type.IsValueType && !type.IsPrimitiveNumeric)
                {
                    continue;
                }

                return type;
           }
        }

        // This looks a lot like WasmAbiContext.LowerType....
        //
        public static CorInfoWasmType WasmTypeClassification(TypeDesc typeDesc)
        {
            switch (typeDesc.Category)
            {
                case TypeFlags.Boolean:
                case TypeFlags.Char:
                case TypeFlags.SByte:
                case TypeFlags.Byte:
                case TypeFlags.Int16:
                case TypeFlags.UInt16:
                case TypeFlags.Int32:
                case TypeFlags.UInt32:
                    return CorInfoWasmType.CORINFO_WASM_TYPE_I32;
                    
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                    return CorInfoWasmType.CORINFO_WASM_TYPE_I64;

                case TypeFlags.Single:
                    return CorInfoWasmType.CORINFO_WASM_TYPE_F32;
                case TypeFlags.Double:
                    return CorInfoWasmType.CORINFO_WASM_TYPE_F64;

                case TypeFlags.Enum:
                    return WasmTypeClassification(typeDesc.UnderlyingType);

                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                    return (typeDesc.Context.Target.PointerSize == 4) ? CorInfoWasmType.CORINFO_WASM_TYPE_I32 :  CorInfoWasmType.CORINFO_WASM_TYPE_I64;

                default:
                    return CorInfoWasmType.CORINFO_WASM_TYPE_VOID;
            }
        }
    }
}
