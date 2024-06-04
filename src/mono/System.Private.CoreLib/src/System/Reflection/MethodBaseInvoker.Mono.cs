// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        internal unsafe MethodBaseInvoker(RuntimeMethodInfo method) : this(method, method.ArgumentTypes)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.ArgumentTypes)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Constructor;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr *args)
        {
            object? o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return o;
        }

        internal unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr* args)
        {
            object? o = ((RuntimeConstructorInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
