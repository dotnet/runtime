// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    // see src/mono/wasm/driver.c MARSHAL_TYPE_xxx
    public enum MarshalType : int {
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
        POINTER = 32,
        SPAN_BYTE = 33,
    }

    // see src/mono/wasm/driver.c MARSHAL_ERROR_xxx
    public enum MarshalError : int {
        BUFFER_TOO_SMALL = 512,
        NULL_CLASS_POINTER = 513,
        NULL_TYPE_POINTER = 514,
        UNSUPPORTED_TYPE = 515,
        FIRST = BUFFER_TOO_SMALL
    }

    public enum ArgsMarshalCharacter {
        Int32 = 'i', // int32
        Int32Enum = 'j', // int32 - Enum with underlying type of int32
        Int64 = 'l', // int64
        Int64Enum = 'k', // int64 - Enum with underlying type of int64
        Float32 = 'f', // float
        Float64 = 'd', // double
        String = 's', // string
        InternedString = 'S', // interned string
        Uri = 'u',
        JSObj = 'o', // js object will be converted to a C# object (this will box numbers/bool/promises)
        MONOObj = 'm', // raw mono object. Don't use it unless you know what you're doing
        Auto = 'a', // the bindings layer will select an appropriate converter based on the C# method signature
        ByteSpan = 'b', // Span<byte>
    }

    public struct MarshalString {
        public string Signature { get; private set; }
        public string Key { get; private set; }
        public MethodBase? Method { get; private set; }
        public int ArgumentCount { get; private set; }
        public bool RawReturnValue { get; private set; }
        public bool ContainsAuto { get; private set; }

        public MarshalString (string s, MethodBase? method = null) {
            Signature = s;
            Method = method;
            RawReturnValue = s.EndsWith("!");
            ArgumentCount = Signature.Length;
            ContainsAuto = s.Contains((char)(int)ArgsMarshalCharacter.Auto);

            if (RawReturnValue)
                ArgumentCount -= 1;

            var keySig = Signature.Replace("!", "_result_unmarshaled");
            if (keySig.Length == 0)
                keySig = "$void";

            if (ContainsAuto && (Method != null))
                Key = $"{keySig}_m{Method.MethodHandle.Value.ToInt32()}";
            else
                Key = keySig;
        }

        public ArgsMarshalCharacter this [int index] =>
            (ArgsMarshalCharacter)(int)Signature[index];
    }

    public class WasmInteropException : Exception {
        public WasmInteropException (string message)
            : base (message) {
        }

        public WasmInteropException (string message, Exception innerException)
            : base (message, innerException) {
        }
    }
}
