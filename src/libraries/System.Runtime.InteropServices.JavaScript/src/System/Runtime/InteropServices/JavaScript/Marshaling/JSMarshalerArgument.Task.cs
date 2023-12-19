// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSHostImplementation;

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
            // there is no nice way in JS how to check that JS promise is already resolved, to send MarshalerType.TaskRejected, MarshalerType.TaskResolved
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            PromiseHolder holder = GetPromiseHolder(slot.GCHandle);
            TaskCompletionSource tcs = new TaskCompletionSource(holder);
            ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
            {
                if (arguments_buffer == null)
                {
                    tcs.TrySetException(new TaskCanceledException("WebWorker which is origin of the Promise is being terminated."));
                    return;
                }
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
            // there is no nice way in JS how to check that JS promise is already resolved, to send MarshalerType.TaskRejected, MarshalerType.TaskResolved
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            PromiseHolder holder = GetPromiseHolder(slot.GCHandle);
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(holder);
            ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
            {
                if (arguments_buffer == null)
                {
                    tcs.TrySetException(new TaskCanceledException("WebWorker which is origin of the Promise is being terminated."));
                    return;
                }

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

        // TODO unregister and collect pending PromiseHolder also when no C# is awaiting ?
        private static PromiseHolder GetPromiseHolder(nint gcHandle)
        {
            PromiseHolder holder;
            if (IsGCVHandle(gcHandle))
            {
                // this path should only happen when the Promise is passed as argument of JSExport
                holder = new PromiseHolder(gcHandle);
                // TODO for MT this must hit the ThreadJsOwnedHolders in the correct thread
                ThreadJsOwnedHolders.Add(gcHandle, holder);
            }
            else
            {
                holder = (PromiseHolder)((GCHandle)gcHandle).Target!;
            }
            return holder;
        }

        internal void ToJSDynamic(Task? value)
        {
            Task? task = value;

            if (task == null)
            {
                slot.Type = MarshalerType.None;
                return;
            }

            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    ToJS(ex);
                    slot.ElementType = slot.Type;
                    slot.Type = MarshalerType.TaskRejected;
                    return;
                }
                else
                {
                    if (GetTaskResultDynamic(task, out object? result))
                    {
                        ToJS(result);
                        slot.ElementType = slot.Type;
                    }
                    else
                    {
                        slot.ElementType = MarshalerType.Void;
                    }
                    slot.Type = MarshalerType.TaskResolved;
                    return;
                }
            }

            if (slot.Type != MarshalerType.TaskPreCreated)
            {
                // this path should only happen when the Task is passed as argument of JSImport
                slot.JSHandle = AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = new JSObject(slot.JSHandle);

#if FEATURE_WASM_THREADS
            task.ContinueWith(Complete, taskHolder, TaskScheduler.FromCurrentSynchronizationContext());
#else
            task.ContinueWith(Complete, taskHolder, TaskScheduler.Current);
#endif

            static void Complete(Task task, object? th)
            {
                var taskHolderArg = (JSObject)th!;
                if (task.Exception != null)
                {
                    RejectPromise(taskHolderArg, task.Exception);
                }
                else
                {
                    if (GetTaskResultDynamic(task, out object? result))
                    {
                        ResolvePromise(taskHolderArg, result, MarshalResult);
                    }
                    else
                    {
                        ResolveVoidPromise(taskHolderArg);
                    }
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
            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    ToJS(ex);
                    slot.ElementType = slot.Type;
                    slot.Type = MarshalerType.TaskRejected;
                    return;
                }
                else
                {
                    slot.ElementType = slot.Type;
                    slot.Type = MarshalerType.TaskResolved;
                    return;
                }
            }

            if (slot.Type != MarshalerType.TaskPreCreated)
            {
                // this path should only happen when the Task is passed as argument of JSImport
                slot.JSHandle = AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = new JSObject(slot.JSHandle);

#if FEATURE_WASM_THREADS
            task.ContinueWith(Complete, taskHolder, TaskScheduler.FromCurrentSynchronizationContext());
#else
            task.ContinueWith(Complete, taskHolder, TaskScheduler.Current);
#endif

            static void Complete(Task task, object? th)
            {
                JSObject taskHolderArg = (JSObject)th!;
                if (task.Exception != null)
                {
                    RejectPromise(taskHolderArg, task.Exception);
                }
                else
                {
                    ResolveVoidPromise(taskHolderArg);
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

            if (task.IsCompleted)
            {
                if (task.Exception != null)
                {
                    Exception ex = task.Exception;
                    ToJS(ex);
                    slot.ElementType = slot.Type;
                    slot.Type = MarshalerType.TaskRejected;
                    return;
                }
                else
                {
                    T result = task.Result;
                    ToJS(result);
                    slot.ElementType = slot.Type;
                    slot.Type = MarshalerType.TaskResolved;
                    return;
                }
            }

            if (slot.Type != MarshalerType.TaskPreCreated)
            {
                // this path should only happen when the Task is passed as argument of JSImport
                slot.JSHandle = AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = new JSObject(slot.JSHandle);

#if FEATURE_WASM_THREADS
            task.ContinueWith(Complete, new HolderAndMarshaler<T>(taskHolder, marshaler), TaskScheduler.FromCurrentSynchronizationContext());
#else
            task.ContinueWith(Complete, new HolderAndMarshaler<T>(taskHolder, marshaler), TaskScheduler.Current);
#endif

            static void Complete(Task<T> task, object? thm)
            {
                var hm = (HolderAndMarshaler<T>)thm!;
                if (task.Exception != null)
                {
                    RejectPromise(hm.TaskHolder, task.Exception);
                }
                else
                {
                    T result = task.Result;
                    ResolvePromise(hm.TaskHolder, result, hm.Marshaler);
                }
            }
        }

        private sealed record HolderAndMarshaler<T>(JSObject TaskHolder, ArgumentToJSCallback<T> Marshaler);

        private static void RejectPromise(JSObject holder, Exception ex)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskRejected;
            arg_handle.slot.JSHandle = holder.JSHandle;

            // should fail it with exception
            arg_value.ToJS(ex);

            JavaScriptImports.ResolveOrRejectPromise(args);

            holder.DisposeLocal();
        }

        private static void ResolveVoidPromise(JSObject holder)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskResolved;
            arg_handle.slot.JSHandle = holder.JSHandle;

            arg_value.slot.Type = MarshalerType.Void;

            JavaScriptImports.ResolveOrRejectPromise(args);

            holder.DisposeLocal();
        }

        private static void ResolvePromise<T>(JSObject holder, T value, ArgumentToJSCallback<T> marshaler)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

            exc.Initialize();
            res.Initialize();

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskResolved;
            arg_handle.slot.JSHandle = holder.JSHandle;

            // and resolve it with value
            marshaler(ref arg_value, value);

            JavaScriptImports.ResolveOrRejectPromise(args);

            holder.DisposeLocal();
        }
    }
}
