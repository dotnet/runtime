// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        internal unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.ArgumentTypes)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        internal unsafe MethodInvoker(DynamicMethod method) : this(method, method.ArgumentTypes)
        {
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        internal unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.ArgumentTypes)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr *args)
        {
            object? o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return o;
        }
    }
}
