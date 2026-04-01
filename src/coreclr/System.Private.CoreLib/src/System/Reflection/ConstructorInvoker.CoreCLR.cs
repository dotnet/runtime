// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        private readonly Signature? _signature;

        internal unsafe ConstructorInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _signature = constructor.Signature;
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        [RequiresUnsafe]
        private unsafe object? InterpretedInvoke(object? obj, IntPtr* args)
        {
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature!, isConstructor: obj is null);
        }
    }
}
