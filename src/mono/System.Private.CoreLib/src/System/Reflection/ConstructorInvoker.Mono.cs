// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        internal unsafe ConstructorInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.ArgumentTypes)
        {
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr *args)
        {
            object? o = _method.InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
