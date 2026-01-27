// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ILCompiler;
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
        // CORINFO_TYPE_UNDEF.

        public static CorInfoType LowerTypeForWasm(TypeDesc type, uint size)
        {
            if (!type.IsValueType)
            {
                Debug.Fail("Non-struct types should passed directly in Wasm.");
                return CorInfoType.CORINFO_TYPE_UNDEF;
            }

            MetadataType mdType = type as MetadataType;
            if (mdType.HasImpliedRepeatedFields())
            {
                return CorInfoType.CORINFO_TYPE_UNDEF;
            }

            switch (size)
            {
                case 1:
                case 2:
                case 4:
                case 8:
                    break;

                default:
                    return CorInfoType.CORINFO_TYPE_UNDEF;
            }

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
                    return CorInfoType.CORINFO_TYPE_UNDEF;
                }
                
                TypeDesc firstFieldElementType = firstField.FieldType;

                if (firstFieldElementType.IsValueType)
                {
                    type = firstFieldElementType;
                    continue;
                }

                return WasmTypeClassification(firstFieldElementType, size);
            }
        }

        private static CorInfoType WasmTypeClassification(TypeDesc typeDesc, uint size)
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
                    return (size > 4) ? CorInfoType.CORINFO_TYPE_LONG : CorInfoType.CORINFO_TYPE_INT;
                    
                case TypeFlags.Int64:
                case TypeFlags.UInt64:
                    return CorInfoType.CORINFO_TYPE_LONG;

                case TypeFlags.Single:
                    return (size > 4) ? CorInfoType.CORINFO_TYPE_DOUBLE : CorInfoType.CORINFO_TYPE_FLOAT;
                case TypeFlags.Double:
                    return CorInfoType.CORINFO_TYPE_DOUBLE;

                case TypeFlags.Enum:
                    return WasmTypeClassification(typeDesc.UnderlyingType, size);

                case TypeFlags.IntPtr:
                case TypeFlags.UIntPtr:
                case TypeFlags.Pointer:
                case TypeFlags.FunctionPointer:
                case TypeFlags.Class:
                case TypeFlags.Interface:
                case TypeFlags.Array:
                case TypeFlags.SzArray:
                case TypeFlags.ByRef:
                    return CorInfoType.CORINFO_TYPE_NATIVEINT;

                default:
                    return CorInfoType.CORINFO_TYPE_UNDEF;
            }
        }
    }
}
