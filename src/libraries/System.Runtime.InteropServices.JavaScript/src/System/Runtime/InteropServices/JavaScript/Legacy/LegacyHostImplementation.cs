// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    [SupportedOSPlatform("browser")]
    internal static class LegacyHostImplementation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseInFlight(object obj)
        {
            JSObject? jsObj = obj as JSObject;
            jsObj?.ReleaseInFlight();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterCSOwnedObject(JSObject proxy)
        {
            JSHostImplementation.ThreadCsOwnedObjects[(int)proxy.JSHandle] = new WeakReference<JSObject>(proxy, trackResurrection: true);
        }

        public static MarshalType GetMarshalTypeFromType(Type type)
        {
            if (type is null)
                return MarshalType.VOID;

            var typeCode = Type.GetTypeCode(type);
            if (type.IsEnum)
            {
                switch (typeCode)
                {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        return MarshalType.ENUM;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return MarshalType.ENUM64;
                    default:
                        throw new ArgumentException(SR.Format(SR.UnsupportedEnumType, type.FullName), nameof(type));
                }
            }

            switch (typeCode)
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                    return MarshalType.INT;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                    return MarshalType.UINT32;
                case TypeCode.Boolean:
                    return MarshalType.BOOL;
                case TypeCode.Int64:
                    return MarshalType.INT64;
                case TypeCode.UInt64:
                    return MarshalType.UINT64;
                case TypeCode.Single:
                    return MarshalType.FP32;
                case TypeCode.Double:
                    return MarshalType.FP64;
                case TypeCode.String:
                    return MarshalType.STRING;
                case TypeCode.Char:
                    return MarshalType.CHAR;
            }

            if (type.IsArray)
            {
                if (!type.IsSZArray)
                    throw new ArgumentException(SR.Format(SR.UnsupportedArrayType, type.FullName), nameof(type));

                var elementType = type.GetElementType();
                switch (Type.GetTypeCode(elementType))
                {
                    case TypeCode.Byte:
                        return MarshalType.ARRAY_UBYTE;
                    case TypeCode.SByte:
                        return MarshalType.ARRAY_BYTE;
                    case TypeCode.Int16:
                        return MarshalType.ARRAY_SHORT;
                    case TypeCode.UInt16:
                        return MarshalType.ARRAY_USHORT;
                    case TypeCode.Int32:
                        return MarshalType.ARRAY_INT;
                    case TypeCode.UInt32:
                        return MarshalType.ARRAY_UINT;
                    case TypeCode.Single:
                        return MarshalType.ARRAY_FLOAT;
                    case TypeCode.Double:
                        return MarshalType.ARRAY_DOUBLE;
                    default:
                        throw new ArgumentException(SR.Format(SR.UnsupportedElementType, elementType), nameof(type));
                }
            }
            else if (type == typeof(IntPtr))
                return MarshalType.POINTER;
            else if (type == typeof(UIntPtr))
                return MarshalType.POINTER;
            else if (type == typeof(SafeHandle))
                return MarshalType.SAFEHANDLE;
            else if (typeof(Delegate).IsAssignableFrom(type))
                return MarshalType.DELEGATE;
            else if ((type == typeof(Task)) || typeof(Task).IsAssignableFrom(type))
                return MarshalType.TASK;
            else if (type.FullName == "System.Uri")
                return MarshalType.URI;
            else if (type.IsPointer)
                return MarshalType.POINTER;

            if (type.IsValueType)
                return MarshalType.VT;
            else
                return MarshalType.OBJECT;
        }

        public static char GetCallSignatureCharacterForMarshalType(MarshalType type, char? defaultValue)
        {
            switch (type)
            {
                case MarshalType.BOOL:
                    return 'b';
                case MarshalType.UINT32:
                case MarshalType.POINTER:
                    return 'I';
                case MarshalType.INT:
                    return 'i';
                case MarshalType.UINT64:
                    return 'L';
                case MarshalType.INT64:
                    return 'l';
                case MarshalType.FP32:
                    return 'f';
                case MarshalType.FP64:
                    return 'd';
                case MarshalType.STRING:
                    return 's';
                case MarshalType.URI:
                    return 'u';
                case MarshalType.SAFEHANDLE:
                    return 'h';
                case MarshalType.ENUM:
                    return 'j'; // this is wrong for uint enums
                case MarshalType.ENUM64:
                    return 'k'; // this is wrong for ulong enums
                case MarshalType.TASK:
                case MarshalType.DELEGATE:
                case MarshalType.OBJECT:
                    return 'o';
                case MarshalType.VT:
                    return 'a';
                default:
                    if (defaultValue.HasValue)
                        return defaultValue.Value;
                    else
                        throw new ArgumentException(SR.Format(SR.UnsupportedLegacyMarshlerType, type), nameof(type));
            }
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

#if FEATURE_WASM_THREADS
        public static void ThrowIfLegacyWorkerThread()
        {
            if (Thread.CurrentThread.ManagedThreadId != 1)
            {
                throw new PlatformNotSupportedException("Legacy interop is not supported with WebAssembly threads.");
            }
        }
#endif
    }
}
