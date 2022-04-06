// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public static partial class Runtime
    {
        private const string TaskGetResultName = "get_Result";
        private static readonly MethodInfo _taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName)!;

        /// <summary>
        /// Execute the provided string in the JavaScript context
        /// </summary>
        /// <returns>The js.</returns>
        /// <param name="str">String.</param>
        public static string InvokeJS(string str)
        {
            return Interop.Runtime.InvokeJS(str);
        }

        public static Function? CompileFunction(string snippet)
        {
            return Interop.Runtime.CompileFunction(snippet);
        }

        public static object GetGlobalObject(string? str = null)
        {
            return Interop.Runtime.GetGlobalObject(str);
        }

        public static void DumpAotProfileData(ref byte buf, int len, string extraArg)
        {
            Interop.Runtime.DumpAotProfileData(ref buf, len, extraArg);
        }

        public static bool IsSimpleArrayRef(ref object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntPtrAndHandle
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

        public static string GetCallSignatureRef(IntPtr _methodHandle, in object? objForRuntimeType)
        {
            var methodHandle = GetMethodHandleFromIntPtr(_methodHandle);

            MethodBase? mb = objForRuntimeType is null ? MethodBase.GetMethodFromHandle(methodHandle) : MethodBase.GetMethodFromHandle(methodHandle, Type.GetTypeHandle(objForRuntimeType));
            if (mb is null)
                return string.Empty;

            ParameterInfo[] parms = mb.GetParameters();
            int parmsLength = parms.Length;
            if (parmsLength == 0)
                return string.Empty;

            var result = new char[parmsLength];
            for (int i = 0; i < parmsLength; i++)
            {
                Type t = parms[i].ParameterType;
                var mt = GetMarshalTypeFromType(t);
                result[i] = GetCallSignatureCharacterForMarshalType(mt, null);
            }

            return new string(result);
        }

        private static RuntimeMethodHandle GetMethodHandleFromIntPtr(IntPtr ptr)
        {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.methodHandle;
        }

        private static RuntimeTypeHandle GetTypeHandleFromIntPtr(IntPtr ptr)
        {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.typeHandle;
        }

        internal static MarshalType GetMarshalTypeFromType(Type? type)
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
                        throw new JSException($"Unsupported enum underlying type {typeCode}");
                }
            }

            switch (typeCode)
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    return MarshalType.INT;
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
                    throw new JSException("Only single-dimensional arrays with a zero lower bound can be marshaled to JS");

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
                        throw new JSException($"Unsupported array element type {elementType}");
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
            else if (typeof(Uri) == type)
                return MarshalType.URI;
            else if (type.IsPointer)
                return MarshalType.POINTER;

            if (type.IsValueType)
                return MarshalType.VT;
            else
                return MarshalType.OBJECT;
        }

        internal static char GetCallSignatureCharacterForMarshalType(MarshalType t, char? defaultValue)
        {
            switch (t)
            {
                case MarshalType.BOOL:
                case MarshalType.INT:
                case MarshalType.UINT32:
                case MarshalType.POINTER:
                    return 'i';
                case MarshalType.UINT64:
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
                    return 'j';
                case MarshalType.ENUM64:
                    return 'k';
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
                        throw new JSException($"Unsupported marshal type {t}");
            }
        }

        /// <summary>
        /// Gets the MethodInfo for the Task{T}.Result property getter.
        /// </summary>
        /// <remarks>
        /// This ensures the returned MethodInfo is strictly for the Task{T} type, and not
        /// a "Result" property on some other class that derives from Task or a "new Result"
        /// property on a class that derives from Task{T}.
        ///
        /// The reason for this restriction is to make this use of Reflection trim-compatible,
        /// ensuring that trimming doesn't change the application's behavior.
        /// </remarks>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Task<T>.Result is preserved by the ILLinker because _taskGetResultMethodInfo was initialized with it.")]
        private static MethodInfo? GetTaskResultMethodInfo(Type taskType)
        {
            MethodInfo? result = taskType.GetMethod(TaskGetResultName);
            if (result != null && result.HasSameMetadataDefinitionAs(_taskGetResultMethodInfo))
            {
                return result;
            }

            return null;
        }

        public static string ObjectToStringRef(ref object o)
        {
            return o.ToString() ?? string.Empty;
        }

        public static double GetDateValueRef(ref object dtv!!)
        {
            if (!(dtv is DateTime dt))
                throw new InvalidCastException(SR.Format(SR.UnableCastObjectToType, dtv.GetType(), typeof(DateTime)));
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        // HACK: We need to implicitly box by using an 'object' out-param.
        // Note that the return value would have been boxed on the C#->JS transition anyway.
        public static void CreateDateTimeRef(double ticks, out object result)
        {
            DateTimeOffset unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            result = unixTime.DateTime;
        }

        public static void CreateUriRef(string uri, out Uri result)
        {
            result = new Uri(uri);
        }

        public static void CancelPromise(IntPtr promiseJSHandle)
        {
            var res = Interop.Runtime.CancelPromise(promiseJSHandle, out int exception);
            if (exception != 0)
                throw new JSException(res);
        }

        public static Task<object> WebSocketOpen(string uri, object[]? subProtocols, Delegate onClosed, out JSObject webSocket, out IntPtr promiseJSHandle)
        {
            Interop.Runtime.WebSocketOpenRef(uri, subProtocols, onClosed, out IntPtr webSocketJSHandle, out promiseJSHandle, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);
            webSocket = new JSObject((IntPtr)webSocketJSHandle);

            return (Task<object>)res;
        }

        public static unsafe Task<object>? WebSocketSend(JSObject webSocket, ArraySegment<byte> buffer, int messageType, bool endOfMessage, out IntPtr promiseJSHandle)
        {
            fixed (byte* messagePtr = buffer.Array)
            {
                Interop.Runtime.WebSocketSend(webSocket.JSHandle, (IntPtr)messagePtr, buffer.Offset, buffer.Count, messageType, endOfMessage, out promiseJSHandle, out int exception, out object res);
                if (exception != 0)
                    throw new JSException((string)res);

                if (res == null)
                {
                    return null;
                }

                return (Task<object>)res;
            }
        }

        public static unsafe Task<object>? WebSocketReceive(JSObject webSocket, ArraySegment<byte> buffer, ReadOnlySpan<int> response, out IntPtr promiseJSHandle)
        {
            fixed (int* responsePtr = response)
            fixed (byte* bufferPtr = buffer.Array)
            {
                Interop.Runtime.WebSocketReceive(webSocket.JSHandle, (IntPtr)bufferPtr, buffer.Offset, buffer.Count, (IntPtr)responsePtr, out promiseJSHandle, out int exception, out object res);
                if (exception != 0)
                    throw new JSException((string)res);
                if (res == null)
                {
                    return null;
                }
                return (Task<object>)res;
            }
        }

        public static Task<object>? WebSocketClose(JSObject webSocket, int code, string? reason, bool waitForCloseReceived, out IntPtr promiseJSHandle)
        {
            Interop.Runtime.WebSocketCloseRef(webSocket.JSHandle, code, reason, waitForCloseReceived, out promiseJSHandle, out int exception, out object res);
            if (exception != 0)
                throw new JSException((string)res);

            if (res == null)
            {
                return null;
            }
            return (Task<object>)res;
        }

        public static void WebSocketAbort(JSObject webSocket)
        {
            Interop.Runtime.WebSocketAbort(webSocket.JSHandle, out int exception, out string res);
            if (exception != 0)
                throw new JSException(res);
        }
    }
}
