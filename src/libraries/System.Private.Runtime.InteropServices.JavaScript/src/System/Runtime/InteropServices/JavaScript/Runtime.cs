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

        public static bool IsSimpleArray(object a)
        {
            return a is System.Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntPtrAndHandle
        {
            [FieldOffset(0)]
            internal IntPtr ptr;

            [FieldOffset(0)]
            internal RuntimeMethodHandle handle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }

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
            POINTER = 32
        }

        // see src/mono/wasm/driver.c MARSHAL_ERROR_xxx
        public enum MarshalError : int {
            BUFFER_TOO_SMALL = 512,
            NULL_CLASS_POINTER = 513,
            NULL_TYPE_POINTER = 514,
            UNSUPPORTED_TYPE = 515,
            FIRST = BUFFER_TOO_SMALL
        }

        public static string GetCallSignature(IntPtr methodHandle, object objForRuntimeType)
        {
            IntPtrAndHandle tmp = default(IntPtrAndHandle);
            tmp.ptr = methodHandle;

            MethodBase? mb = objForRuntimeType == null ? MethodBase.GetMethodFromHandle(tmp.handle) : MethodBase.GetMethodFromHandle(tmp.handle, Type.GetTypeHandle(objForRuntimeType));
            if (mb == null)
                return string.Empty;

            ParameterInfo[] parms = mb.GetParameters();
            int parmsLength = parms.Length;
            if (parmsLength == 0)
                return string.Empty;

            char[] res = new char[parmsLength];

            for (int c = 0; c < parmsLength; c++)
            {
                Type t = parms[c].ParameterType;
                switch (Type.GetTypeCode(t))
                {
                    case TypeCode.Byte:
                    case TypeCode.SByte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Boolean:
                        // Enums types have the same code as their underlying numeric types
                        if (t.IsEnum)
                            res[c] = 'j';
                        else
                            res[c] = 'i';
                        break;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        // Enums types have the same code as their underlying numeric types
                        if (t.IsEnum)
                            res[c] = 'k';
                        else
                            res[c] = 'l';
                        break;
                    case TypeCode.Single:
                        res[c] = 'f';
                        break;
                    case TypeCode.Double:
                        res[c] = 'd';
                        break;
                    case TypeCode.String:
                        res[c] = 's';
                        break;
                    default:
                        if (t == typeof(IntPtr))
                        {
                            res[c] = 'i';
                        }
                        else if (t == typeof(Uri))
                        {
                            res[c] = 'u';
                        }
                        else if (t == typeof(SafeHandle))
                        {
                            res[c] = 'h';
                        }
                        else
                        {
                            if (t.IsValueType)
                                throw new NotSupportedException(SR.ValueTypeNotSupported);
                            res[c] = 'o';
                        }
                        break;
                }
            }
            return new string(res);
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

        public static string ObjectToString(object o)
        {
            return o.ToString() ?? string.Empty;
        }

        public static double GetDateValue(object dtv)
        {
            if (dtv == null)
                throw new ArgumentNullException(nameof(dtv));
            if (!(dtv is DateTime dt))
                throw new InvalidCastException(SR.Format(SR.UnableCastObjectToType, dtv.GetType(), typeof(DateTime)));
            if (dt.Kind == DateTimeKind.Local)
                dt = dt.ToUniversalTime();
            else if (dt.Kind == DateTimeKind.Unspecified)
                dt = new DateTime(dt.Ticks, DateTimeKind.Utc);
            return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }

        public static DateTime CreateDateTime(double ticks)
        {
            DateTimeOffset unixTime = DateTimeOffset.FromUnixTimeMilliseconds((long)ticks);
            return unixTime.DateTime;
        }

        public static Uri CreateUri(string uri)
        {
            return new Uri(uri);
        }

        public static void CancelPromise(int promiseJSHandle)
        {
            var res = Interop.Runtime.CancelPromise(promiseJSHandle, out int exception);
            if (exception != 0)
                throw new JSException(res);
        }

        public static Task<object> WebSocketOpen(string uri, object[]? subProtocols, Delegate onClosed, out JSObject webSocket, out int promiseJSHandle)
        {
            var res = Interop.Runtime.WebSocketOpen(uri, subProtocols, onClosed, out int webSocketJSHandle, out promiseJSHandle, out int exception);
            if (exception != 0)
                throw new JSException((string)res);
            webSocket = new JSObject((IntPtr)webSocketJSHandle);

            return (Task<object>)res;
        }

        public static unsafe Task<object>? WebSocketSend(JSObject webSocket, ArraySegment<byte> buffer, int messageType, bool endOfMessage, out int promiseJSHandle)
        {
            fixed (byte* messagePtr = buffer.Array)
            {
                var res = Interop.Runtime.WebSocketSend(webSocket.JSHandle, (IntPtr)messagePtr, buffer.Offset, buffer.Count, messageType, endOfMessage, out promiseJSHandle, out int exception);
                if (exception != 0)
                    throw new JSException((string)res);

                if (res == null)
                {
                    return null;
                }

                return (Task<object>)res;
            }
        }

        public static unsafe Task<object>? WebSocketReceive(JSObject webSocket, ArraySegment<byte> buffer, ReadOnlySpan<int> response, out int promiseJSHandle)
        {
            fixed (int* responsePtr = response)
            fixed (byte* bufferPtr = buffer.Array)
            {
                var res = Interop.Runtime.WebSocketReceive(webSocket.JSHandle, (IntPtr)bufferPtr, buffer.Offset, buffer.Count, (IntPtr)responsePtr, out promiseJSHandle, out int exception);
                if (exception != 0)
                    throw new JSException((string)res);
                if (res == null)
                {
                    return null;
                }
                return (Task<object>)res;
            }
        }

        public static Task<object>? WebSocketClose(JSObject webSocket, int code, string? reason, bool waitForCloseReceived, out int promiseJSHandle)
        {
            var res = Interop.Runtime.WebSocketClose(webSocket.JSHandle, code, reason, waitForCloseReceived, out promiseJSHandle, out int exception);
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
            var res = Interop.Runtime.WebSocketAbort(webSocket.JSHandle, out int exception);
            if (exception != 0)
                throw new JSException(res);
        }
    }
}
