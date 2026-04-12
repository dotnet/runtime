// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        internal unsafe MethodBaseInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(DynamicMethod method, Signature signature) : this(method, signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args)
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
