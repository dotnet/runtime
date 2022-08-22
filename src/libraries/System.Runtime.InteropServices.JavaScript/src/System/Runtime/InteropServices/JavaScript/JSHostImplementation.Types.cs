// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        internal unsafe delegate void ToManagedCallback(JSMarshalerArgument* arguments_buffer);

        public sealed class TaskCallback
        {
            public ToManagedCallback? Callback;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle methodHandle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }

        // see src/mono/wasm/driver.c MARSHAL_TYPE_xxx
        public enum MarshalType : int
        {
            NULL = 0,
            INT = 1,
            FP64 = 2,
            STRING = 3,
            VT = 4,
            DELEGATE = 5,
            TASK = 6,
            OBJECT = 7,
            BOOL = 8,
            ENUM = 9,
            URI = 22,
            SAFEHANDLE = 23,
            ARRAY_BYTE = 10,
            ARRAY_UBYTE = 11,
            ARRAY_UBYTE_C = 12,
            ARRAY_SHORT = 13,
            ARRAY_USHORT = 14,
            ARRAY_INT = 15,
            ARRAY_UINT = 16,
            ARRAY_FLOAT = 17,
            ARRAY_DOUBLE = 18,
            FP32 = 24,
            UINT32 = 25,
            INT64 = 26,
            UINT64 = 27,
            CHAR = 28,
            STRING_INTERNED = 29,
            VOID = 30,
            ENUM64 = 31,
            POINTER = 32
        }

        // see src/mono/wasm/driver.c MARSHAL_ERROR_xxx
        public enum MarshalError : int
        {
            BUFFER_TOO_SMALL = 512,
            NULL_CLASS_POINTER = 513,
            NULL_TYPE_POINTER = 514,
            UNSUPPORTED_TYPE = 515,
            FIRST = BUFFER_TOO_SMALL
        }

        // please keep BINDING wasm_type_symbol in sync
        public enum MappedType
        {
            JSObject = 0,
            Array = 1,
            ArrayBuffer = 2,
            DataView = 3,
            Function = 4,
            Uint8Array = 11,
        }
    }
}
