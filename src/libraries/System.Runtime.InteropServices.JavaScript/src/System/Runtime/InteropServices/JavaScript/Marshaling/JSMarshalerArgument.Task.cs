// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSHostImplementation;
using System.Runtime.CompilerServices;

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
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
        public delegate void ArgumentToManagedCallback<T>(ref JSMarshalerArgument arg, out T value);

        /// <summary>
        /// Assists in marshalling of Task results and Function arguments.
        /// This API is used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <typeparam name="T">Type of the marshaled value.</typeparam>
        /// <param name="arg">The low-level argument representation.</param>
        /// <param name="value">The value to be marshaled.</param>
        [EditorBrowsableAttribute(EditorBrowsableState.Never)]
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
            var ctx = ToManagedContext;
            lock (ctx)
            {
                PromiseHolder holder = ctx.GetPromiseHolder(slot.GCHandle);
                TaskCompletionSource tcs = new TaskCompletionSource(holder, TaskCreationOptions.RunContinuationsAsynchronously);
                ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
                {
                    if (arguments_buffer == null)
                    {
                        if (!tcs.TrySetException(new TaskCanceledException("WebWorker which is origin of the Promise is being terminated.")))
                        {
                            Environment.FailFast("Failed to set exception to TaskCompletionSource (arguments buffer is null)");
                        }
                        return;
                    }
                    ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // set by caller when this is SetException call
                                                                             // arg_3 set by caller when this is SetResult call, un-used here
                    if (arg_2.slot.Type != MarshalerType.None)
                    {
                        arg_2.ToManaged(out Exception? fail);
                        if (!tcs.TrySetException(fail!))
                        {
                            Environment.FailFast("Failed to set exception to TaskCompletionSource (exception raised)");
                        }
                    }
                    else
                    {
                        if (!tcs.TrySetResult())
                        {
                            Environment.FailFast("Failed to set result to TaskCompletionSource (marshaler type is none)");
                        }
                    }
                    // eventual exception is handled by caller
                };
                holder.Callback = callback;
                value = tcs.Task;
#if FEATURE_WASM_MANAGED_THREADS
                // if the other thread created it, signal that it's ready
                holder.CallbackReady?.Set();
#endif
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        /// <param name="value">The value to be marshaled.</param>
        /// <param name="marshaler">The generated callback which marshals the result value of the <see cref="Task"/>.</param>
        /// <typeparam name="T">Type of marshaled result of the <see cref="Task"/>.</typeparam>
        public unsafe void ToManaged<T>(out Task<T>? value, ArgumentToManagedCallback<T> marshaler)
        {
            // there is no nice way in JS how to check that JS promise is already resolved, to send MarshalerType.TaskRejected, MarshalerType.TaskResolved
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }
            var ctx = ToManagedContext;
            lock (ctx)
            {
                var holder = ctx.GetPromiseHolder(slot.GCHandle);
                TaskCompletionSource<T> tcs = new TaskCompletionSource<T>(holder, TaskCreationOptions.RunContinuationsAsynchronously);
                ToManagedCallback callback = (JSMarshalerArgument* arguments_buffer) =>
                {
                    if (arguments_buffer == null)
                    {
                        if (!tcs.TrySetException(new TaskCanceledException("WebWorker which is origin of the Promise is being terminated.")))
                        {
                            Environment.FailFast("Failed to set exception to TaskCompletionSource (arguments buffer is null)");
                        }
                        return;
                    }

                    ref JSMarshalerArgument arg_2 = ref arguments_buffer[3]; // set by caller when this is SetException call
                    ref JSMarshalerArgument arg_3 = ref arguments_buffer[4]; // set by caller when this is SetResult call
                    if (arg_2.slot.Type != MarshalerType.None)
                    {
                        arg_2.ToManaged(out Exception? fail);
                        if (fail == null) throw new InvalidOperationException(SR.FailedToMarshalException);
                        if (!tcs.TrySetException(fail))
                        {
                            Environment.FailFast("Failed to set exception to TaskCompletionSource (exception raised)");
                        }
                    }
                    else
                    {
                        marshaler(ref arg_3, out T result);
                        if(!tcs.TrySetResult(result))
                        {
                            Environment.FailFast("Failed to set result to TaskCompletionSource (marshaler type is none)");
                        }
                    }
                    // eventual exception is handled by caller
                };
                holder.Callback = callback;
                value = tcs.Task;
#if FEATURE_WASM_MANAGED_THREADS
                // if the other thread created it, signal that it's ready
                holder.CallbackReady?.Set();
#endif
            }
        }


        internal void ToJSDynamic(Task? value)
        {
            Task? task = value;

            var ctx = ToJSContext;
            var canMarshalTaskResultOnSameCall = CanMarshalTaskResultOnSameCall(ctx);

            if (task == null)
            {
                if (!canMarshalTaskResultOnSameCall)
                {
                    Environment.FailFast("Marshalling null return Task to JS is not supported in MT");
                }
                slot.Type = MarshalerType.None;
                return;
            }

            if (canMarshalTaskResultOnSameCall && task.IsCompleted)
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
                slot.JSHandle = ctx.AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = ctx.CreateCSOwnedProxy(slot.JSHandle);

#if FEATURE_WASM_MANAGED_THREADS
            // AsyncTaskScheduler will make sure that the resolve message is always sent after this call is completed
            // that is: synchronous marshaling and eventually message to the target thread, which need to arrive before the resolve message
            task.ContinueWith(Complete, taskHolder, ctx.AsyncTaskScheduler!);
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
            var ctx = ToJSContext;
            var canMarshalTaskResultOnSameCall = CanMarshalTaskResultOnSameCall(ctx);

            if (task == null)
            {
                if (!canMarshalTaskResultOnSameCall)
                {
                    Environment.FailFast("Marshalling null return Task to JS is not supported in MT");
                }
                slot.Type = MarshalerType.None;
                return;
            }
            if (canMarshalTaskResultOnSameCall && task.IsCompleted)
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
                    slot.ElementType = MarshalerType.Void;
                    slot.Type = MarshalerType.TaskResolved;
                    return;
                }
            }

            if (slot.Type != MarshalerType.TaskPreCreated)
            {
                // this path should only happen when the Task is passed as argument of JSImport
                slot.JSHandle = ctx.AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = ctx.CreateCSOwnedProxy(slot.JSHandle);

#if FEATURE_WASM_MANAGED_THREADS
            // AsyncTaskScheduler will make sure that the resolve message is always sent after this call is completed
            // that is: synchronous marshaling and eventually message to the target thread, which need to arrive before the resolve message
            task.ContinueWith(Complete, taskHolder, ctx.AsyncTaskScheduler!);
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
            var ctx = ToJSContext;
            var canMarshalTaskResultOnSameCall = CanMarshalTaskResultOnSameCall(ctx);

            if (task == null)
            {
                if (!canMarshalTaskResultOnSameCall)
                {
                    Environment.FailFast("Marshalling null return Task to JS is not supported in MT");
                }
                slot.Type = MarshalerType.None;
                return;
            }

            if (canMarshalTaskResultOnSameCall && task.IsCompleted)
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
                slot.JSHandle = ctx.AllocJSVHandle();
                slot.Type = MarshalerType.Task;
            }
            else
            {
                // this path should hit for return values from JSExport/call_entry_point
                // promise and handle is pre-allocated in slot.JSHandle
            }

            var taskHolder = ctx.CreateCSOwnedProxy(slot.JSHandle);

#if FEATURE_WASM_MANAGED_THREADS
            // AsyncTaskScheduler will make sure that the resolve message is always sent after this call is completed
            // that is: synchronous marshaling and eventually message to the target thread, which need to arrive before the resolve message
            task.ContinueWith(Complete, new HolderAndMarshaler<T>(taskHolder, marshaler), ctx.AsyncTaskScheduler!);
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

#if !DEBUG
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
#if FEATURE_WASM_MANAGED_THREADS
        // We can't marshal resolved/rejected/null Task.Result directly into current argument when this is marshaling return of JSExport across threads
        private bool CanMarshalTaskResultOnSameCall(JSProxyContext ctx)
        {
            if (slot.Type != MarshalerType.TaskPreCreated)
            {
                // this means that we are not in the return value of JSExport
                // we are marshaling parameter of JSImport
                return true;
            }

            if (ctx.IsCurrentThread())
            {
                // If the JS and Managed is running on the same thread we can use the args buffer,
                // because the call is synchronous and the buffer will be processed.
                // In that case the pre-allocated Promise would be discarded as necessary
                // and the result will be marshaled by `try_marshal_sync_task_to_js`
                return true;
            }

            // Otherwise this is JSExport return value and we can't use the args buffer, because the args buffer arrived in async message and nobody is reading after this.
            // In such case the JS side already pre-created the Promise and we have to use it, to resolve it in separate call via `mono_wasm_resolve_or_reject_promise_post`
            // there is JSVHandle in this arg
            return false;
        }
#else
#pragma warning disable CA1822 // Mark members as static
        private bool CanMarshalTaskResultOnSameCall(JSProxyContext _)
        {
            // in ST build this is always synchronous and we can marshal the result directly
            return true;
        }
#pragma warning restore CA1822 // Mark members as static
#endif

        private sealed record HolderAndMarshaler<T>(JSObject TaskHolder, ArgumentToJSCallback<T> Marshaler);

        private static void RejectPromise(JSObject holder, Exception ex)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

#if FEATURE_WASM_MANAGED_THREADS
            exc.InitializeWithContext(holder.ProxyContext);
            res.InitializeWithContext(holder.ProxyContext);
            arg_value.InitializeWithContext(holder.ProxyContext);
            arg_handle.InitializeWithContext(holder.ProxyContext);
            JSProxyContext.JSImportNoCapture();
#else
            exc.Initialize();
            res.Initialize();
#endif

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskRejected;
            arg_handle.slot.JSHandle = holder.JSHandle;

            // should fail it with exception
            arg_value.ToJS(ex);

            // we can free the JSHandle here and the holder.resolve_or_reject will do the rest
            holder.DisposeImpl(skipJsCleanup: true);

            // order of operations with DisposeImpl matters
            JSFunctionBinding.ResolveOrRejectPromise(holder.ProxyContext, args);
        }

        private static void ResolveVoidPromise(JSObject holder)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

#if FEATURE_WASM_MANAGED_THREADS
            exc.InitializeWithContext(holder.ProxyContext);
            res.InitializeWithContext(holder.ProxyContext);
            arg_value.InitializeWithContext(holder.ProxyContext);
            arg_handle.InitializeWithContext(holder.ProxyContext);
            JSProxyContext.JSImportNoCapture();
#else
            exc.Initialize();
            res.Initialize();
#endif

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskResolved;
            arg_handle.slot.JSHandle = holder.JSHandle;

            arg_value.slot.Type = MarshalerType.Void;

            // we can free the JSHandle here and the holder.resolve_or_reject will do the rest
            holder.DisposeImpl(skipJsCleanup: true);

            // order of operations with DisposeImpl matters
            JSFunctionBinding.ResolveOrRejectPromise(holder.ProxyContext, args);
        }

        private static void ResolvePromise<T>(JSObject holder, T value, ArgumentToJSCallback<T> marshaler)
        {
            holder.AssertNotDisposed();

            Span<JSMarshalerArgument> args = stackalloc JSMarshalerArgument[4];
            ref JSMarshalerArgument exc = ref args[0];
            ref JSMarshalerArgument res = ref args[1];
            ref JSMarshalerArgument arg_handle = ref args[2];
            ref JSMarshalerArgument arg_value = ref args[3];

#if FEATURE_WASM_MANAGED_THREADS
            exc.InitializeWithContext(holder.ProxyContext);
            res.InitializeWithContext(holder.ProxyContext);
            arg_value.InitializeWithContext(holder.ProxyContext);
            arg_handle.InitializeWithContext(holder.ProxyContext);
            JSProxyContext.JSImportNoCapture();
#else
            exc.Initialize();
            res.Initialize();
#endif

            // should update existing promise
            arg_handle.slot.Type = MarshalerType.TaskResolved;
            arg_handle.slot.JSHandle = holder.JSHandle;

            // and resolve it with value
            marshaler(ref arg_value, value);

            // we can free the JSHandle here and the holder.resolve_or_reject will do the rest
            holder.DisposeImpl(skipJsCleanup: true);

            // order of operations with DisposeImpl matters
            JSFunctionBinding.ResolveOrRejectPromise(holder.ProxyContext, args);
        }
    }
}
