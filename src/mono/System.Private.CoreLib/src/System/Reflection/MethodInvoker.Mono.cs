// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.ArgumentTypes)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
        }

        private unsafe MethodInvoker(DynamicMethod method) : this(method.GetRuntimeMethodInfo(), method.ArgumentTypes)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            // No _invocationFlags for DynamicMethod.
        }

        private unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.ArgumentTypes)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Constructor;
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr *args)
        {
            object? o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return o;
        }

        private unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr *args)
        {
            object? o = ((RuntimeConstructorInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
