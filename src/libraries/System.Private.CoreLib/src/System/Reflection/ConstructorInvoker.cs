// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Reflection
{
    internal sealed partial class ConstructorInvoker
    {
        private readonly RuntimeConstructorInfo _constructorInfo;
        public InvocationFlags _invocationFlags;

        public ConstructorInvoker(RuntimeConstructorInfo constructorInfo)
        {
            _constructorInfo = constructorInfo;

            RuntimeType[] argTypes = constructorInfo.ArgumentTypes;
            for (int i = 0; i < argTypes.Length; i++)
            {
                if (argTypes[i].IsByRef)
                {
                    HasRefs = true;
                    break;
                }
            }
        }

        public bool HasRefs { get; }

        [DebuggerStepThrough]
        [DebuggerHidden]
        public unsafe object? InvokeUnsafe(object? obj, IntPtr** args, BindingFlags invokeAttr)
        {
            // Todo: add strategy for calling IL Emit-based version
            return _constructorInfo.InvokeNonEmitUnsafe(obj, args, invokeAttr);
        }
    }
}
