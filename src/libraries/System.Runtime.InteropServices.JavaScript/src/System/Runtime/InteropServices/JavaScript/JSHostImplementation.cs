// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        private const string TaskGetResultName = "get_Result";
        private static readonly MethodInfo s_taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName)!;
        // we use this to maintain identity of JSHandle for a JSObject proxy
        public static readonly Dictionary<int, WeakReference<JSObject>> s_csOwnedObjects = new Dictionary<int, WeakReference<JSObject>>();
        // we use this to maintain identity of GCHandle for a managed object
        public static Dictionary<object, IntPtr> s_gcHandleFromJSOwnedObject = new Dictionary<object, IntPtr>(ReferenceEqualityComparer.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterCSOwnedObject(JSObject proxy)
        {
            lock (s_csOwnedObjects)
            {
                s_csOwnedObjects[(int)proxy.JSHandle] = new WeakReference<JSObject>(proxy, trackResurrection: true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseCSOwnedObject(IntPtr jsHandle)
        {
            if (jsHandle != IntPtr.Zero)
            {
                lock (s_csOwnedObjects)
                {
                    s_csOwnedObjects.Remove((int)jsHandle);
                }
                Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object? GetTaskResult(Task task)
        {
            MethodInfo method = GetTaskResultMethodInfo(task.GetType());
            if (method != null)
            {
                return method.Invoke(task, null);
            }
            throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseInFlight(object obj)
        {
            JSObject? jsObj = obj as JSObject;
            jsObj?.ReleaseInFlight();
        }

        // A JSOwnedObject is a managed object with its lifetime controlled by javascript.
        // The managed side maintains a strong reference to the object, while the JS side
        //  maintains a weak reference and notifies the managed side if the JS wrapper object
        //  has been reclaimed by the JS GC. At that point, the managed side will release its
        //  strong references, allowing the managed object to be collected.
        // This ensures that things like delegates and promises will never 'go away' while JS
        //  is expecting to be able to invoke or await them.
        public static IntPtr GetJSOwnedObjectGCHandle(object obj, GCHandleType handleType = GCHandleType.Normal)
        {
            if (obj == null)
                return IntPtr.Zero;

            IntPtr result;
            lock (s_gcHandleFromJSOwnedObject)
            {
                IntPtr gcHandle;
                if (s_gcHandleFromJSOwnedObject.TryGetValue(obj, out gcHandle))
                    return gcHandle;

                result = (IntPtr)GCHandle.Alloc(obj, handleType);
                s_gcHandleFromJSOwnedObject[obj] = result;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static RuntimeMethodHandle GetMethodHandleFromIntPtr(IntPtr ptr)
        {
            var temp = new IntPtrAndHandle { ptr = ptr };
            return temp.methodHandle;
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
                        throw new JSException($"Unsupported enum underlying type {typeCode}");
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
            else if (type.FullName == "System.Uri")
                return MarshalType.URI;
            else if (type.IsPointer)
                return MarshalType.POINTER;

            if (type.IsValueType)
                return MarshalType.VT;
            else
                return MarshalType.OBJECT;
        }

        public static char GetCallSignatureCharacterForMarshalType(MarshalType t, char? defaultValue)
        {
            switch (t)
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
        public static MethodInfo GetTaskResultMethodInfo(Type taskType)
        {
            if (taskType != null)
            {
                MethodInfo? result = taskType.GetMethod(TaskGetResultName);
                if (result != null && result.HasSameMetadataDefinitionAs(s_taskGetResultMethodInfo))
                {
                    return result;
                }
            }

            throw new InvalidOperationException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowException(ref JSMarshalerArgument arg)
        {
            arg.ToManaged(out Exception? ex);

            if (ex != null)
            {
                throw ex;
            }
            throw new InvalidProgramException();
        }

        public static async Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken )
        {
            Task<JSObject> modulePromise = JavaScriptImports.DynamicImport(moduleName, moduleUrl);
            var wrappedTask = CancelationHelper(modulePromise, cancellationToken);
            await Task.Yield();// this helps to finish the import before we bind the module in [JSImport]
            return await wrappedTask.ConfigureAwait(true);
        }

        public static async Task<JSObject> CancelationHelper(Task<JSObject> jsTask, CancellationToken cancellationToken)
        {
            if (jsTask.IsCompletedSuccessfully)
            {
                return jsTask.Result;
            }
            using (var receiveRegistration = cancellationToken.Register(() =>
            {
                CancelablePromise.CancelPromise(jsTask);
            }))
            {
                return await jsTask.ConfigureAwait(true);
            }
        }

        // res type is first argument
        public static unsafe JSFunctionBinding GetMethodSignature(ReadOnlySpan<JSMarshalerType> types)
        {
            int argsCount = types.Length - 1;
            int size = JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + ((argsCount + 2) * sizeof(JSFunctionBinding.JSBindingType));
            // this is never unallocated
            IntPtr buffer = Marshal.AllocHGlobal(size);

            var signature = new JSFunctionBinding
            {
                Header = (JSFunctionBinding.JSBindingHeader*)buffer,
                Sigs = (JSFunctionBinding.JSBindingType*)(buffer + JSFunctionBinding.JSBindingHeader.JSMarshalerSignatureHeaderSize + (2 * sizeof(JSFunctionBinding.JSBindingType))),
            };

            signature.Version = 1;
            signature.ArgumentCount = argsCount;
            signature.Exception = JSMarshalerType.Exception._signatureType;
            signature.Result = types[0]._signatureType;
            for (int i = 0; i < argsCount; i++)
            {
                signature.Sigs[i] = types[i + 1]._signatureType;
            }

            return signature;
        }

        public static JSObject CreateCSOwnedProxy(IntPtr jsHandle)
        {
            JSObject? res = null;

            lock (s_csOwnedObjects)
            {
                if (!s_csOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                    !reference.TryGetTarget(out res) ||
                    res.IsDisposed)
                {
                    res = new JSObject(jsHandle);
                    s_csOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
                }
            }
            return res;
        }
    }
}
