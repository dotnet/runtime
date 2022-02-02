// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class ConstructorInvoker
    {
        public delegate object? InvokeCtorFunc(object? obj, Span<object?> args, BindingFlags invokeAttr);

        private readonly bool _hasRefs;
        public InvocationFlags _invocationFlags;
        private InvokeCtorFunc _invoke;

        public ConstructorInvoker(InvokeCtorFunc invokeFunc, RuntimeType[] sigTypes)
        {
            _invoke = invokeFunc;

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
            return _invoke(obj, parameters, invokeAttr);
        }
    }
}
