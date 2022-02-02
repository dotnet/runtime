// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class MethodInvoker
    {
        public delegate object? InvokeFunc(object? obj, Span<object?> args, BindingFlags invokeAttr);
        internal InvocationFlags _invocationFlags;
        private readonly bool _hasRefs;
        private InvokeFunc _invokeFunc;

        public MethodInvoker(InvokeFunc invokeFunc, RuntimeType[] sigTypes)
        {
            _invokeFunc = invokeFunc;

            for (int i = 0; i < sigTypes.Length; i++)
            {
                if (sigTypes[i].IsByRef)
                {
                    _hasRefs = true;
                    break;
                }
            }
        }

        public bool HasRefs => _hasRefs;

        public object? Invoke(object? obj, Span<object?> parameters, BindingFlags invokeAttr)
        {
            return _invokeFunc(obj, parameters, invokeAttr);
        }
    }
}
