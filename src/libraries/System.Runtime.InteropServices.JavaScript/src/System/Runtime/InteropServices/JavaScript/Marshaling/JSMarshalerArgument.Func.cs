// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JSMarshalerArgument
    {
        private sealed class ActionJS
        {
            private JSObject JSObject;

            public ActionJS(IntPtr jsHandle)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
            }

            public void InvokeJS()
            {
                // JSObject (held by this lambda) would be collected by GC after the lambda is collected
                // and would also allow the JS function to be collected

                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                args_exception.Initialize();
                args_return.Initialize();

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);
            }

        }

        private sealed class ActionJS<T>
        {
            private ArgumentToJSCallback<T> Arg1Marshaler;
            private JSObject JSObject;

            public ActionJS(IntPtr jsHandle, ArgumentToJSCallback<T> arg1Marshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
            }

            public void InvokeJS(T arg1)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);
            }
        }

        private sealed class ActionJS<T1, T2>
        {
            private ArgumentToJSCallback<T1> Arg1Marshaler;
            private ArgumentToJSCallback<T2> Arg2Marshaler;
            private JSObject JSObject;

            public ActionJS(IntPtr jsHandle, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
                Arg2Marshaler = arg2Marshaler;
            }

            public void InvokeJS(T1 arg1, T2 arg2)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];
                ref JSMarshalerArgument args_arg2 = ref arguments[3];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);
                Arg2Marshaler(ref args_arg2, arg2);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);
            }
        }

        private sealed class ActionJS<T1, T2, T3>
        {
            private ArgumentToJSCallback<T1> Arg1Marshaler;
            private ArgumentToJSCallback<T2> Arg2Marshaler;
            private ArgumentToJSCallback<T3> Arg3Marshaler;
            private JSObject JSObject;

            public ActionJS(IntPtr jsHandle, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
                Arg2Marshaler = arg2Marshaler;
                Arg3Marshaler = arg3Marshaler;
            }

            public void InvokeJS(T1 arg1, T2 arg2, T3 arg3)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[5];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];
                ref JSMarshalerArgument args_arg2 = ref arguments[3];
                ref JSMarshalerArgument args_arg3 = ref arguments[4];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);
                Arg2Marshaler(ref args_arg2, arg2);
                Arg3Marshaler(ref args_arg3, arg3);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged(out Action? value)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new ActionJS(slot.JSHandle).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T>(out Action<T>? value, ArgumentToJSCallback<T> arg1Marshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new ActionJS<T>(slot.JSHandle, arg1Marshaler).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2>(out Action<T1, T2>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new ActionJS<T1, T2>(slot.JSHandle, arg1Marshaler, arg2Marshaler).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, T3>(out Action<T1, T2, T3>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new ActionJS<T1, T2, T3>(slot.JSHandle, arg1Marshaler, arg2Marshaler, arg3Marshaler).InvokeJS;
        }

        private sealed class FuncJS<TResult>
        {
            private JSObject JSObject;
            private ArgumentToManagedCallback<TResult> ResMarshaler;

            public FuncJS(IntPtr jsHandle, ArgumentToManagedCallback<TResult> resMarshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                ResMarshaler = resMarshaler;
            }

            public TResult InvokeJS()
            {
                // JSObject (held by this lambda) would be collected by GC after the lambda is collected
                // and would also allow the JS function to be collected

                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                args_exception.Initialize();
                args_return.Initialize();

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);

                ResMarshaler(ref args_return, out TResult res);
                return res;
            }

        }

        private sealed class FuncJS<T, TResult>
        {
            private ArgumentToJSCallback<T> Arg1Marshaler;
            private ArgumentToManagedCallback<TResult> ResMarshaler;
            private JSObject JSObject;

            public FuncJS(IntPtr jsHandle, ArgumentToJSCallback<T> arg1Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
                ResMarshaler = resMarshaler;
            }

            public TResult InvokeJS(T arg1)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);

                ResMarshaler(ref args_return, out TResult res);
                return res;
            }
        }

        private sealed class FuncJS<T1, T2, TResult>
        {
            private ArgumentToJSCallback<T1> Arg1Marshaler;
            private ArgumentToJSCallback<T2> Arg2Marshaler;
            private ArgumentToManagedCallback<TResult> ResMarshaler;
            private JSObject JSObject;

            public FuncJS(IntPtr jsHandle, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
                Arg2Marshaler = arg2Marshaler;
                ResMarshaler = resMarshaler;
            }

            public TResult InvokeJS(T1 arg1, T2 arg2)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[4];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];
                ref JSMarshalerArgument args_arg2 = ref arguments[3];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);
                Arg2Marshaler(ref args_arg2, arg2);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);

                ResMarshaler(ref args_return, out TResult res);
                return res;
            }
        }

        private sealed class FuncJS<T1, T2, T3, TResult>
        {
            private ArgumentToJSCallback<T1> Arg1Marshaler;
            private ArgumentToJSCallback<T2> Arg2Marshaler;
            private ArgumentToJSCallback<T3> Arg3Marshaler;
            private ArgumentToManagedCallback<TResult> ResMarshaler;
            private JSObject JSObject;

            public FuncJS(IntPtr jsHandle, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
            {
                JSObject = JSHostImplementation.CreateCSOwnedProxy(jsHandle);
                Arg1Marshaler = arg1Marshaler;
                Arg2Marshaler = arg2Marshaler;
                Arg3Marshaler = arg3Marshaler;
                ResMarshaler = resMarshaler;
            }

            public TResult InvokeJS(T1 arg1, T2 arg2, T3 arg3)
            {
                Span<JSMarshalerArgument> arguments = stackalloc JSMarshalerArgument[5];
                ref JSMarshalerArgument args_exception = ref arguments[0];
                ref JSMarshalerArgument args_return = ref arguments[1];
                ref JSMarshalerArgument args_arg1 = ref arguments[2];
                ref JSMarshalerArgument args_arg2 = ref arguments[3];
                ref JSMarshalerArgument args_arg3 = ref arguments[4];

                args_exception.Initialize();
                args_return.Initialize();
                Arg1Marshaler(ref args_arg1, arg1);
                Arg2Marshaler(ref args_arg2, arg2);
                Arg3Marshaler(ref args_arg3, arg3);

                JSFunctionBinding.InvokeJSImpl(JSObject, arguments);

                ResMarshaler(ref args_return, out TResult res);
                return res;
            }
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<TResult>(out Func<TResult>? value, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new FuncJS<TResult>(slot.JSHandle, resMarshaler).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T, TResult>(out Func<T, TResult>? value, ArgumentToJSCallback<T> arg1Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new FuncJS<T, TResult>(slot.JSHandle, arg1Marshaler, resMarshaler).InvokeJS;

        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, TResult>(out Func<T1, T2, TResult>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new FuncJS<T1, T2, TResult>(slot.JSHandle, arg1Marshaler, arg2Marshaler, resMarshaler).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToManaged<T1, T2, T3, TResult>(out Func<T1, T2, T3, TResult>? value, ArgumentToJSCallback<T1> arg1Marshaler, ArgumentToJSCallback<T2> arg2Marshaler, ArgumentToJSCallback<T3> arg3Marshaler, ArgumentToManagedCallback<TResult> resMarshaler)
        {
            if (slot.Type == MarshalerType.None)
            {
                value = null;
                return;
            }

            value = new FuncJS<T1, T2, T3, TResult>(slot.JSHandle, arg1Marshaler, arg2Marshaler, arg3Marshaler, resMarshaler).InvokeJS;
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS(Action value)
        {
            Action cpy = value;
            // TODO: we could try to cache value -> existing GCHandle
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                cpy.Invoke();
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Function;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T>(Action<T> value, ArgumentToManagedCallback<T> arg1Marshaler)
        {
            Action<T> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument arg2 = ref arguments[3]; // set by JS caller
                arg1Marshaler(ref arg2, out T arg1cs);
                cpy.Invoke(arg1cs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Action;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2>(Action<T1, T2> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler)
        {
            Action<T1, T2> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument arg2 = ref arguments[3];// set by JS caller
                ref JSMarshalerArgument arg3 = ref arguments[4];// set by JS caller
                arg1Marshaler(ref arg2, out T1 arg1cs);
                arg2Marshaler(ref arg3, out T2 arg2cs);
                cpy.Invoke(arg1cs, arg2cs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Action;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, T3>(Action<T1, T2, T3> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler)
        {
            Action<T1, T2, T3> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument arg2 = ref arguments[3];// set by JS caller
                ref JSMarshalerArgument arg3 = ref arguments[4];// set by JS caller
                ref JSMarshalerArgument arg4 = ref arguments[5];// set by JS caller
                arg1Marshaler(ref arg2, out T1 arg1cs);
                arg2Marshaler(ref arg3, out T2 arg2cs);
                arg3Marshaler(ref arg4, out T3 arg3cs);
                cpy.Invoke(arg1cs, arg2cs, arg3cs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Action;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<TResult>(Func<TResult> value, ArgumentToJSCallback<TResult> resMarshaler)
        {
            Func<TResult> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument res = ref arguments[1];
                TResult resCs = cpy.Invoke();
                resMarshaler(ref res, resCs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Function;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T, TResult>(Func<T, TResult> value, ArgumentToManagedCallback<T> arg1Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            Func<T, TResult> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument res = ref arguments[1];
                ref JSMarshalerArgument arg2 = ref arguments[3];// set by JS caller
                arg1Marshaler(ref arg2, out T arg1cs);
                TResult resCs = cpy.Invoke(arg1cs);
                resMarshaler(ref res, resCs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Function;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, TResult>(Func<T1, T2, TResult> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            Func<T1, T2, TResult> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument res = ref arguments[1];
                ref JSMarshalerArgument arg2 = ref arguments[3];// set by JS caller
                ref JSMarshalerArgument arg3 = ref arguments[4];// set by JS caller
                arg1Marshaler(ref arg2, out T1 arg1cs);
                arg2Marshaler(ref arg3, out T2 arg2cs);
                TResult resCs = cpy.Invoke(arg1cs, arg2cs);
                resMarshaler(ref res, resCs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Function;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }

        /// <summary>
        /// Implementation of the argument marshaling.
        /// It's used by JSImport code generator and should not be used by developers in source code.
        /// </summary>
        public unsafe void ToJS<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> value, ArgumentToManagedCallback<T1> arg1Marshaler, ArgumentToManagedCallback<T2> arg2Marshaler, ArgumentToManagedCallback<T3> arg3Marshaler, ArgumentToJSCallback<TResult> resMarshaler)
        {
            Func<T1, T2, T3, TResult> cpy = value;
            JSHostImplementation.ToManagedCallback cb = (JSMarshalerArgument* arguments) =>
            {
                ref JSMarshalerArgument res = ref arguments[1];
                ref JSMarshalerArgument arg2 = ref arguments[3];// set by JS caller
                ref JSMarshalerArgument arg3 = ref arguments[4];// set by JS caller
                ref JSMarshalerArgument arg4 = ref arguments[5];// set by JS caller
                arg1Marshaler(ref arg2, out T1 arg1cs);
                arg2Marshaler(ref arg3, out T2 arg2cs);
                arg3Marshaler(ref arg4, out T3 arg3cs);
                TResult resCs = cpy.Invoke(arg1cs, arg2cs, arg3cs);
                resMarshaler(ref res, resCs);
                // eventual exception is handled by C# caller
            };
            slot.Type = MarshalerType.Function;
            slot.GCHandle = JSHostImplementation.GetJSOwnedObjectGCHandle(cb);
        }
    }
}
