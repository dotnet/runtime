// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    internal sealed partial class FieldAccessor
    {
        public delegate object? InvokeGetterFunc(object? obj);
        public delegate void InvokeSetterFunc(object? obj, object? value);

        public InvocationFlags _invocationFlags;
        private InvokeGetterFunc _invokeGetterFunc;
        private InvokeSetterFunc _invokeSetterFunc;

        public FieldAccessor(InvokeGetterFunc getterFallback, InvokeSetterFunc setterFallback)
        {
            _invokeGetterFunc = getterFallback;
            _invokeSetterFunc = setterFallback;
        }

        public object? InvokeGetter(object? obj)
        {
            return _invokeGetterFunc(obj);
        }

        public void InvokeSetter(object? obj, object? value)
        {
            _invokeSetterFunc(obj, value);
        }
    }
}
