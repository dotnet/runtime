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
        private static MethodInfo? s_taskGetResultMethodInfo;
        // we use this to maintain identity of JSHandle for a JSObject proxy
#if FEATURE_WASM_THREADS
        [ThreadStatic]
#endif
        private static Dictionary<int, WeakReference<JSObject>>? s_csOwnedObjects;

        public static Dictionary<int, WeakReference<JSObject>> ThreadCsOwnedObjects
        {
            get
            {
                s_csOwnedObjects ??= new ();
                return s_csOwnedObjects;
            }
        }

        // we use this to maintain identity of GCHandle for a managed object
        public static Dictionary<object, IntPtr> s_gcHandleFromJSOwnedObject = new Dictionary<object, IntPtr>(ReferenceEqualityComparer.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReleaseCSOwnedObject(nint jsHandle)
        {
            if (jsHandle != IntPtr.Zero)
            {
                ThreadCsOwnedObjects.Remove((int)jsHandle);
                Interop.Runtime.ReleaseCSOwnedObject(jsHandle);
            }
        }

        public static object? GetTaskResultDynamic(Task task)
        {
            MethodInfo method = GetTaskResultMethodInfo(task.GetType());
            if (method != null)
            {
                return method.Invoke(task, null);
            }
            throw new InvalidOperationException();
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
            Justification = "Task<T>.Result is preserved by the ILLinker because s_taskGetResultMethodInfo was initialized with it.")]
        public static MethodInfo GetTaskResultMethodInfo(Type taskType)
        {
            if (taskType != null)
            {
                if (s_taskGetResultMethodInfo == null)
                {
                    s_taskGetResultMethodInfo = typeof(Task<>).GetMethod(TaskGetResultName);
                }
                MethodInfo? getter = taskType.GetMethod(TaskGetResultName);
                if (getter != null && getter.HasSameMetadataDefinitionAs(s_taskGetResultMethodInfo!))
                {
                    return getter;
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
            throw new InvalidOperationException();
        }

        public static async Task<JSObject> ImportAsync(string moduleName, string moduleUrl, CancellationToken cancellationToken)
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

        public static unsafe void FreeMethodSignatureBuffer(JSFunctionBinding signature)
        {
            Marshal.FreeHGlobal((nint)signature.Header);
            signature.Header = null;
            signature.Sigs = null;
        }

        public static JSObject CreateCSOwnedProxy(nint jsHandle)
        {
            JSObject? res;

            if (!ThreadCsOwnedObjects.TryGetValue((int)jsHandle, out WeakReference<JSObject>? reference) ||
                !reference.TryGetTarget(out res) ||
                res.IsDisposed)
            {
                res = new JSObject(jsHandle);
                ThreadCsOwnedObjects[(int)jsHandle] = new WeakReference<JSObject>(res, trackResurrection: true);
            }
            return res;
        }
    }
}
