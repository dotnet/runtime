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
            internal RuntimeMethodHandle methodHandle;

            [FieldOffset(0)]
            internal RuntimeTypeHandle typeHandle;
        }

        private static RuntimeMethodHandle GetMethodHandleFromIntPtr (IntPtr ptr) {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.methodHandle;
        }

        private static RuntimeTypeHandle GetTypeHandleFromIntPtr (IntPtr ptr) {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.typeHandle;
        }

        private static string MakeMarshalTypeRecord (Type type, MarshalType mtype) {
            var result = $"{{ \"marshalType\": {(int)mtype}, " +
                $"\"typePtr\": {type.TypeHandle.Value}, " +
                $"\"signatureChar\": \"{GetCallSignatureCharacterForMarshalType(mtype, 'a')}\" }}";
            return result;
        }

        private static MethodBase? MethodFromPointers (IntPtr typePtr, IntPtr methodPtr) {
            if (methodPtr == IntPtr.Zero)
                return null;

            var methodHandle = GetMethodHandleFromIntPtr(methodPtr);

            if (typePtr != IntPtr.Zero) {
                var typeHandle = GetTypeHandleFromIntPtr(typePtr);
                return MethodBase.GetMethodFromHandle(methodHandle, typeHandle);
            } else {
                return MethodBase.GetMethodFromHandle(methodHandle);
            }
        }

        public static unsafe string? MakeMarshalSignatureInfo (IntPtr typePtr, IntPtr methodPtr) {
            var mb = MethodFromPointers(typePtr, methodPtr);
            if (mb is null)
                return null;

            var returnType = (mb as MethodInfo)?.ReturnType ?? typeof(void);
            var returnMtype = GetMarshalTypeFromType(returnType);
            var sb = new StringBuilder();
            sb.Append("{ ");
            sb.Append("\"result\": ");
            sb.Append(MakeMarshalTypeRecord(returnType, returnMtype));
            sb.Append(", \"typePtr\": ");
            sb.Append(typePtr.ToInt32());
            sb.Append(", \"methodPtr\": ");
            sb.Append(methodPtr.ToInt32());
            sb.Append(", \"parameters\": [");

            int i = 0;
            foreach (var p in mb.GetParameters()) {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(MakeMarshalTypeRecord(p.ParameterType, GetMarshalTypeFromType(p.ParameterType)));
                i++;
            }

            sb.Append("] }");

            return sb.ToString();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        private static unsafe string GetAndEscapeJavascriptLiteralProperty (Type type, string name) {
            var info = type.GetProperty(
                name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            var value = info?.GetValue(null) as string;
            if (value is null)
                return "null";

            var sb = new StringBuilder();
            sb.Append('\"');
            foreach (var ch in value) {
                switch (ch) {
                    case '\'':
                        sb.Append('\'');
                        continue;
                    case '"':
                        sb.Append('\"');
                        continue;
                    case '\\':
                        sb.Append("\\\\");
                        continue;
                    case '\n':
                        sb.Append("\\n");
                        continue;
                }

                if (ch < ' ') {
                    sb.Append("\\u");
                    sb.Append(((int)ch).ToString("X4"));
                } else {
                    sb.Append(ch);
                }
            }
            sb.Append('\"');

            return sb.ToString();
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        private static unsafe IntPtr GetMarshalMethodPointer (Type type, string name, out Type? returnType, out Type parameterType, bool hasScratchBuffer) {
            var info = type.GetMethod(
                name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );
            if (info is null)
                throw new WasmInteropException($"{type.Name} must have a static {name} method");

            var p = info.GetParameters();
            int expectedLength = hasScratchBuffer ? 2 : 1;
            if ((p.Length != expectedLength) || (p[0].ParameterType is null))
                throw new WasmInteropException($"Method {type.Name}.{name} must accept exactly {expectedLength} parameter(s)");

            if (hasScratchBuffer) {
                if ((info.ReturnType != null) && (info.ReturnType != typeof(void)))
                    throw new WasmInteropException($"Method {type.Name}.{name} must not have a return value");
                if ((p[1].ParameterType != typeof(Span<byte>)) && (p[1].ParameterType != typeof(ReadOnlySpan<byte>)))
                    throw new WasmInteropException($"Method {type.Name}.{name}'s second parameter must be of type Span<byte> or ReadOnlySpan<byte>");
            } else {
                if (info.ReturnType is null)
                    throw new WasmInteropException($"Method {type.Name}.{name} must have a return value");
            }

            parameterType = p[0].ParameterType;
            returnType = info.ReturnType;

            return info.MethodHandle.Value;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
            Justification = "Trimming doesn't affect types eligible for marshalling. Different exception for invalid inputs doesn't matter.")]
        public static unsafe string GetCustomMarshalerInfoForType (IntPtr typePtr, string? marshalerFullName) {
            if ((typePtr == IntPtr.Zero) || string.IsNullOrEmpty(marshalerFullName))
                return "null";

            var typeHandle = GetTypeHandleFromIntPtr(typePtr);

            var type = Type.GetTypeFromHandle(typeHandle);
            if (type is null)
                return "null";
            var marshalerType = Type.GetType(marshalerFullName) ?? type.Assembly.GetType(marshalerFullName);
            if (marshalerType is null)
                return "null";

            var scratchInfo = marshalerType.GetProperty("ScratchBufferSize", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var _scratchBufferSize = scratchInfo?.GetValue(null);
            var scratchBufferSize = _scratchBufferSize != null
                ? (int)_scratchBufferSize
                : (int?)null;

            var jsToInterchange = GetAndEscapeJavascriptLiteralProperty(marshalerType, "JavaScriptToInterchangeTransform");
            var interchangeToJs = GetAndEscapeJavascriptLiteralProperty(marshalerType, "InterchangeToJavaScriptTransform");

            if (scratchBufferSize.HasValue) {
                if ((jsToInterchange == "null") || (interchangeToJs == "null"))
                    throw new WasmInteropException($"{marshalerType.Name} must provide interchange transforms if it has a scratch buffer");
            }

            var inputPtr = GetMarshalMethodPointer(marshalerType, "FromJavaScript", out Type? fromReturnType, out Type fromParameterType, false);
            var outputPtr = GetMarshalMethodPointer(marshalerType, "ToJavaScript", out Type? toReturnType, out Type toParameterType, scratchBufferSize.HasValue);

            if (fromReturnType != type)
                throw new WasmInteropException($"{marshalerType.Name}.FromJavaScript's return type must be {type.Name} but was {fromReturnType}");

            if (type.IsValueType) {
                var typeMatches = toParameterType.GetElementType() == type;
                if (!typeMatches || !(toParameterType.IsPointer || toParameterType.IsByRef))
                    throw new WasmInteropException($"{marshalerType.Name}.ToJavaScript's parameter must be 'in {type.Name}' or '{type.Name}*' but was {toParameterType}");
            } else {
                if (toParameterType != type)
                    throw new WasmInteropException($"{marshalerType.Name}.ToJavaScript's parameter must be of type {type.Name} but was {toParameterType}");
            }

            var result = new StringBuilder();
            result.AppendLine("{");
            result.AppendLine($"\"typePtr\": {typePtr},");
            if (scratchBufferSize.HasValue)
                result.AppendLine($"\"scratchBufferSize\": {scratchBufferSize.Value},");
            result.AppendLine($"\"jsToInterchange\": {jsToInterchange},");
            result.AppendLine($"\"interchangeToJs\": {interchangeToJs},");
            result.AppendLine($"\"inputPtr\": {inputPtr},");
            result.AppendLine($"\"outputPtr\": {outputPtr}");
            result.AppendLine("}");
            return result.ToString();
        }

        internal static MarshalType GetMarshalTypeFromType (Type? type) {
            if (type is null)
                return MarshalType.VOID;

            var typeCode = Type.GetTypeCode(type);
            if (type.IsEnum) {
                switch (typeCode) {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        return MarshalType.ENUM;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        return MarshalType.ENUM64;
                    default:
                        throw new WasmInteropException($"Unsupported enum underlying type {typeCode}");
                }
            }

            switch (typeCode) {
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

            if (type.IsArray) {
                if (!type.IsSZArray)
                    throw new WasmInteropException("Only single-dimensional arrays with a zero lower bound can be marshaled to JS");

                var elementType = type.GetElementType();
                switch (Type.GetTypeCode(elementType)) {
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
                        throw new WasmInteropException($"Unsupported array element type {elementType}");
                }
            } else if (type == typeof(IntPtr))
                return MarshalType.POINTER;
            else if (type == typeof(UIntPtr))
                return MarshalType.POINTER;
            else if (type == typeof(SafeHandle))
                return MarshalType.SAFEHANDLE;
            else if (typeof(Delegate).IsAssignableFrom(type))
                return MarshalType.DELEGATE;
            else if ((type == typeof(Task)) || typeof(Task).IsAssignableFrom(type))
                return MarshalType.TASK;
            // HACK: You could theoretically inherit from Uri, but I consider this out of scope.
            // If you really need to marshal a custom Uri, define a custom marshaler for it
            else if (typeof(Uri) == type)
                return MarshalType.URI;
            else if ((type == typeof(Span<byte>)) || (type == typeof(ReadOnlySpan<byte>)))
                return MarshalType.SPAN_BYTE;
            else if (type.IsPointer)
                return MarshalType.POINTER;

            if (type.IsValueType)
                return MarshalType.VT;
            else
                return MarshalType.OBJECT;
        }

        internal static char GetCallSignatureCharacterForMarshalType (MarshalType t, char? defaultValue) {
            switch (t) {
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
                case MarshalType.SPAN_BYTE:
                    return 'b';
                default:
                    if (defaultValue.HasValue)
                        return defaultValue.Value;
                    else
                        throw new WasmInteropException($"Unsupported marshal type {t}");
            }
        }

        public static string GetCallSignature(IntPtr _methodHandle, object? objForRuntimeType)
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
            for (int i = 0; i < parmsLength; i++) {
                Type t = parms[i].ParameterType;
                var mt = GetMarshalTypeFromType(t);
                result[i] = GetCallSignatureCharacterForMarshalType(mt, null);
            }

            return new string(result);
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

        public static string ObjectToString(object? o)
        {
            return o?.ToString() ?? string.Empty;
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

        public static string GenerateArgsMarshaler (IntPtr typeHandle, IntPtr methodHandle, string signature) {
            MethodBase? method;
            try {
                // It's generally harmless for this to fail unless the signature contains an 'a', so we log it and continue
                method = MethodFromPointers(typeHandle, methodHandle);
            } catch (Exception exc) {
                Debug.WriteLine($"Failed to resolve method when generating marshaler: {exc.Message}");
                method = null;
            }

            var state = new Codegen.MarshalBuilderState {
                MarshalString = new MarshalString(signature, method)
            };
            Codegen.GenerateSignatureConverter(state);
            return state.Output.ToString();
        }

        public static string GenerateBoundMethod (IntPtr typeHandle, IntPtr methodHandle, string signature, string? friendlyName) {
            MethodBase? method;
            method = MethodFromPointers(typeHandle, methodHandle);
            if (method == null)
                throw new Exception("Failed to resolve method");

            var state = new Codegen.BoundMethodBuilderState((MethodInfo)method) {
                MarshalString = new MarshalString(signature, method),
                FriendlyName = friendlyName,
            };
            Codegen.GenerateBoundMethod(state);
            return state.Output.ToString();
        }
    }
}
