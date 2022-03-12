// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal sealed partial class DynamicMethodInvoker
    {
        private readonly bool _hasRefs;

        public Signature Signature { get; }

        public DynamicMethodInvoker(Signature signature)
        {
            Signature = signature;

            RuntimeType[] sigTypes = signature.Arguments;
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

        public unsafe object? InvokeUnsafe(object? obj, IntPtr* args, BindingFlags invokeAttr)
        {
            // Todo: add strategy for calling IL Emit-based version
            return InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }

        private unsafe object? InvokeNonEmitUnsafe(object? obj, IntPtr* arguments, BindingFlags invokeAttr)
        {
            if ((invokeAttr & BindingFlags.DoNotWrapExceptions) == 0)
            {
                bool rethrow = false;

                try
                {
                    return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, Signature, isConstructor: false, out rethrow);
                }
                catch (Exception e) when (!rethrow)
                {
                    throw new TargetInvocationException(e);
                }
            }
            else
            {
                return RuntimeMethodHandle.InvokeMethod(obj, (void**)arguments, Signature, isConstructor: false, out _);
            }
        }
    }
}
