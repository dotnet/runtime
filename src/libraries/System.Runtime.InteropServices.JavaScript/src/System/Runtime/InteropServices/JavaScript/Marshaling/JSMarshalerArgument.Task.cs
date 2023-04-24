// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        /// <summary>
        /// Assists in marshalling of Task results and Function arguments.
        /// This API is used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <typeparam name="T">Type of the marshaled value.</typeparam>
        /// <param name="arg">The low-level argument representation.</param>
        /// <param name="value">The value to be marshaled.</param>
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public delegate void ArgumentToManagedCallback<T>(ref JSMarshalerArgument arg, out T value);

        /// <summary>
        /// Assists in marshalling of Task results and Function arguments.
        /// This API is used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <typeparam name="T">Type of the marshaled value.</typeparam>
        /// <param name="arg">The low-level argument representation.</param>
        /// <param name="value">The value to be marshaled.</param>
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public delegate void ArgumentToJSCallback<T>(ref JSMarshalerArgument arg, T value);

        /// <summary>
        /// Implementation of the argument marshaling.
        /// This API is used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public unsafe void ToManaged(out Task? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            GCHandle gcHandle = (GCHandle)slot.GCHandle;
            JSHostImplementation.TaskCallback? holder = (JSHostImplementation.TaskCallback?)gcHandle.Target;
            if (holder == null) throw new InvalidOperationException(SR.FailedToMarshalTaskCallback);

            TaskCompletionSource tcs = new TaskCompletionSource(gcHandle);
            JSHostImplementation.ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
            {
                ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // set by caller when this is SetException call
                // arg_3 set by caller when this is SetResult call, un-used here
                if (arg_2.slot.Type != MarshalerType.None)
                {
                    arg_2.ToManaged(out Exception? fail);
                    tcs.SetException(fail!);
                }
                else
                {
                    tcs.SetResult();
                }
                // eventual exception is handled by caller
            };
            holder.Callback = callback;
            value = tcs.Task;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        /// <param name="marshaler">The generated callback which marshals the result value of the <see cref="System.Threading.Tasks.Task"/>.</param>
        /// <typeparam name="T">Type of marshaled result of the <see cref="System.Threading.Tasks.Task"/>.</typeparam>
        public unsafe void ToManaged<T>(out Task<T>? value, ArgumentToManagedCallback<T> marshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            GCHandle gcHandle = (GCHandle)slot.GCHandle;
            JSHostImplementation.TaskCallback? holder = (JSHostImplementation.TaskCallback?)gcHandle.Target;
            if (holder == null) throw new InvalidOperationException(SR.FailedToMarshalTaskCallback);

            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(gcHandle);
            JSHostImplementation.ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
            {
                ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // set by caller when this is SetException call
                ref JSMarshalerArgument arg_3 = ref arguments_buffer[4]; // set by caller when this is SetResult call
                if (arg_2.slot.Type != MarshalerType.None)
                {
                    arg_2.ToManaged(out Exception? fail);
                    if (fail == null) throw new InvalidOperationException(SR.FailedToMarshalException);
                    tcs.SetException(fail);
                }
                else
                {
                    marshaler(ref arg_3, out T result);
                    tcs.SetResult(result);
                }
                // eventual exception is handled by caller
            };
            holder.Callback = callback;
            value = tcs.Task;
        }

        internal void ToJSDynamic(Task? value)
        {
            Task? task = value;

            if (task == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.Task;

            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    slot.JSHandle = CreateFailedPromise(ex);
                    return;
                }
                else
                {
                    object? result = JSHostImplementation.GetTaskResultDynamic(task);
                    slot.JSHandle = CreateResolvedPromise(result, MarshalResult);
                    return;
                }
            }


            IntPtr jsHandle = CreatePendingPromise();
            slot.JSHandle = jsHandle;
            JSObject promise = JSHostImplementation.CreateCSOwnedProxy(jsHandle);

            task.GetAwaiter().OnCompleted(Complete);

            /* TODO multi-threading
             * tasks could resolve on any thread and so this code will have race condition between task.IsCompleted and OnCompleted(Complete) callback
             * This probably needs SynchronizationContext to marshal this call to main thread
             */
            Debug.Assert(!task.IsCompleted, "multithreading race condition");

            void Complete()
            {
                // When this task was never resolved/rejected
                // promise (held by this lambda) would be collected by GC after the Task is collected
                // and would also allow the JS promise to be collected

                try
                {
                    if (task.Exception != null)
                    {
                        FailPromise(promise, task.Exception);
                    }
                    else
                    {
                        object? result = JSHostImplementation.GetTaskResultDynamic(task);

                        ResolvePromise(promise, result, MarshalResult);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message, ex);
                }
                finally
                {
                    // this should never happen after the task was GC'd
                    promise.Dispose();
                }
            }

            static void MarshalResult(ref JSMarshalerArgument arg, object? taskResult)
            {
                arg.ToJS(taskResult);
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        public void ToJS(Task? value)
        {
            Task? task = value;

            if (task == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.Task;

            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    slot.JSHandle = CreateFailedPromise(ex);
                    return;
                }
                else
                {
                    slot.JSHandle = IntPtr.Zero;
                    return;
                }
            }

            IntPtr jsHandle = CreatePendingPromise();
            slot.JSHandle = jsHandle;
            JSObject promise = JSHostImplementation.CreateCSOwnedProxy(jsHandle);

            task.GetAwaiter().OnCompleted(Complete);

            /* TODO multi-threading
             * tasks could resolve on any thread and so this code will have race condition between task.IsCompleted and OnCompleted(Complete) callback
             * This probably needs SynchronizationContext to marshal this call to main thread
             */
            Debug.Assert(!task.IsCompleted, "multithreading race condition");

            void Complete()
            {
#if FEATURE_WASM_THREADS
                JSObject.AssertThreadAffinity(promise);
#endif

                // When this task was never resolved/rejected
                // promise (held by this lambda) would be collected by GC after the Task is collected
                // and would also allow the JS promise to be collected

                try
                {
                    if (task.Exception != null)
                    {
                        FailPromise(promise, task.Exception);
                    }
                    else
                    {
                        ResolveVoidPromise(promise);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message, ex);
                }
                finally
                {
                    // this should never happen after the task was GC'd
                    promise.Dispose();
                }
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        /// <param name="marshaler">The generated callback which marshals the result value of the <see cref="System.Threading.Tasks.Task"/>.</param>
        /// <typeparam name="T">Type of marshaled result of the <see cref="System.Threading.Tasks.Task"/>.</typeparam>
        public void ToJS<T>(Task<T>? value, ArgumentToJSCallback<T> marshaler)
        {
            Task<T>? task = value;

            if (task == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }
            slot.Type = MarshalerType.Task;

            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    slot.JSHandle = CreateFailedPromise(ex);
                    return;
                }
                else
                {
                    T result = task.Result;
                    slot.JSHandle = CreateResolvedPromise(result, marshaler);
                    return;
                }
            }


            IntPtr jsHandle = CreatePendingPromise();
            slot.JSHandle = jsHandle;
            JSObject promise = JSHostImplementation.CreateCSOwnedProxy(jsHandle);

            task.GetAwaiter().OnCompleted(Complete);

            /* TODO multi-threading
             * tasks could resolve on any thread and so this code will have race condition between task.IsCompleted and OnCompleted(Complete) callback
             * This probably needs SynchronizationContext to marshal this call to main thread
             */
            Debug.Assert(!task.IsCompleted, "multithreading race condition");

            void Complete()
            {
#if FEATURE_WASM_THREADS
                JSObject.AssertThreadAffinity(promise);
#endif
                // When this task was never resolved/rejected
                // promise (held by this lambda) would be collected by GC after the Task is collected
                // and would also allow the JS promise to be collected

                try
                {
                    if (task.Exception != null)
                    {
                        FailPromise(promise, task.Exception);
                    }
                    else
                    {
                        T result = task.Result;
                        ResolvePromise(promise, result, marshaler);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message, ex);
                }
                finally
                {
                    // this should never happen after the task was GC'd
                    promise.Dispose();
                }
            }
        }

        private static IntPtr CreatePendingPromise()
        {
            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();
            arg_value.Initialize();

            // should create new promise
            arg_handle.slot.Type = MarshalerType.Task;
            arg_handle.slot.JSHandle = IntPtr.Zero;
            arg_value.slot.Type = MarshalerType.Task;

            JavaScriptImports.MarshalPromise(args);
            return res.slot.JSHandle;
        }

        private static IntPtr CreateFailedPromise(Exception ex)
        {
            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];
            res.Initialize();
            arg_value.Initialize();

            // should create new promise
            arg_handle.slot.Type = MarshalerType.Task;
            arg_handle.slot.JSHandle = IntPtr.Zero;
            // should fail it with exception
            exc.ToJS(ex);
            JavaScriptImports.MarshalPromise(args);
            return res.slot.JSHandle;
        }

        private static void FailPromise(JSObject promise, Exception ex)
        {
            ObjectDisposedException.ThrowIf(promise.IsDisposed, promise);

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();
            arg_value.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.None;
            arg_handle.slot.JSHandle = promise.JSHandle;

            // should fail it with exception
            exc.ToJS(ex);

            JavaScriptImports.MarshalPromise(args);
        }

        private static IntPtr CreateResolvedPromise<T>(T value, ArgumentToJSCallback<T> marshaler)
        {
            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should create new promise
            arg_handle.slot.Type = MarshalerType.Task;
            arg_handle.slot.JSHandle = IntPtr.Zero;

            // and resolve it with value
            marshaler(ref arg_value, value);

            JavaScriptImports.MarshalPromise(args);
            return res.slot.JSHandle;
        }

        private static void ResolveVoidPromise(JSObject promise)
        {
            ObjectDisposedException.ThrowIf(promise.IsDisposed, promise);

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.None;
            arg_handle.slot.JSHandle = promise.JSHandle;

            arg_value.slot.Type = MarshalerType.None;

            JavaScriptImports.MarshalPromise(args);
        }

        private static void ResolvePromise<T>(JSObject promise, T value, ArgumentToJSCallback<T> marshaler)
        {
            ObjectDisposedException.ThrowIf(promise.IsDisposed, promise);

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.None;
            arg_handle.slot.JSHandle = promise.JSHandle;

            // and resolve it with value
            marshaler(ref arg_value, value);

            JavaScriptImports.MarshalPromise(args);
        }
    }
}
