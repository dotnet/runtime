// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
** Purpose: Pointer Type to a MethodTable in the runtime.
**
**
===========================================================*/

using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

using CorElementType = System.Reflection.CorElementType;
using EETypeElementType = Internal.Runtime.EETypeElementType;
using MethodTable = Internal.Runtime.MethodTable;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct EETypePtr
    {
        private MethodTable* _value;

        internal EETypePtr(MethodTable* value)
        {
            _value = value;
        }

        internal CorElementType CorElementType
        {
            get
            {
                ReadOnlySpan<byte> map =
                [
                    default,
                    (byte)CorElementType.ELEMENT_TYPE_VOID,      // EETypeElementType.Void
                    (byte)CorElementType.ELEMENT_TYPE_BOOLEAN,   // EETypeElementType.Boolean
                    (byte)CorElementType.ELEMENT_TYPE_CHAR,      // EETypeElementType.Char
                    (byte)CorElementType.ELEMENT_TYPE_I1,        // EETypeElementType.SByte
                    (byte)CorElementType.ELEMENT_TYPE_U1,        // EETypeElementType.Byte
                    (byte)CorElementType.ELEMENT_TYPE_I2,        // EETypeElementType.Int16
                    (byte)CorElementType.ELEMENT_TYPE_U2,        // EETypeElementType.UInt16
                    (byte)CorElementType.ELEMENT_TYPE_I4,        // EETypeElementType.Int32
                    (byte)CorElementType.ELEMENT_TYPE_U4,        // EETypeElementType.UInt32
                    (byte)CorElementType.ELEMENT_TYPE_I8,        // EETypeElementType.Int64
                    (byte)CorElementType.ELEMENT_TYPE_U8,        // EETypeElementType.UInt64
                    (byte)CorElementType.ELEMENT_TYPE_I,         // EETypeElementType.IntPtr
                    (byte)CorElementType.ELEMENT_TYPE_U,         // EETypeElementType.UIntPtr
                    (byte)CorElementType.ELEMENT_TYPE_R4,        // EETypeElementType.Single
                    (byte)CorElementType.ELEMENT_TYPE_R8,        // EETypeElementType.Double

                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE, // EETypeElementType.ValueType
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE,
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE, // EETypeElementType.Nullable
                    (byte)CorElementType.ELEMENT_TYPE_VALUETYPE,
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.Class
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.Interface
                    (byte)CorElementType.ELEMENT_TYPE_CLASS,     // EETypeElementType.SystemArray
                    (byte)CorElementType.ELEMENT_TYPE_ARRAY,     // EETypeElementType.Array
                    (byte)CorElementType.ELEMENT_TYPE_SZARRAY,   // EETypeElementType.SzArray
                    (byte)CorElementType.ELEMENT_TYPE_BYREF,     // EETypeElementType.ByRef
                    (byte)CorElementType.ELEMENT_TYPE_PTR,       // EETypeElementType.Pointer
                    (byte)CorElementType.ELEMENT_TYPE_FNPTR,     // EETypeElementType.FunctionPointer
                    default, // Pad the map to 32 elements to enable range check elimination
                    default,
                    default,
                    default
                ];

                // Verify last element of the map
                Debug.Assert((byte)CorElementType.ELEMENT_TYPE_FNPTR == map[(int)EETypeElementType.FunctionPointer]);

                return (CorElementType)map[(int)ElementType];
            }
        }

        private EETypeElementType ElementType
        {
            get
            {
                return _value->ElementType;
            }
        }

        internal RuntimeImports.RhCorElementTypeInfo CorElementTypeInfo
        {
            get
            {
                return RuntimeImports.GetRhCorElementTypeInfo(CorElementType);
            }
        }
    }
}
