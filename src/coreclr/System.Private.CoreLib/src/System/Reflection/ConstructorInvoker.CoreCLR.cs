// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        internal unsafe ConstructorInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr* args)
        {
            InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = CreateInvokeDelegate_RefArgs(_method);
            }

            // Don't cache for collectible assemblies: the DynamicMethod holds token
            // references to types that would prevent the assembly from being unloaded.
            Type? declaringType = _method.DeclaringType;
            if (declaringType is null || !declaringType.Assembly.IsCollectible)
            {
                _invokeFunc_RefArgs = emitDelegate;
            }

            return emitDelegate(obj, args);
        }
    }
}
