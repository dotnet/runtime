// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe MethodInvoker(DynamicMethod method) : this(method, method.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            // No _invocationFlags for DynamicMethod.
        }

        private unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Constructor;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args)
        {
            InvokeFunc_RefArgs emitDelegate = CreateEmitDelegate(_method);

            if (!IsCollectible(_method))
            {
                _invokeFunc_RefArgs = emitDelegate;
            }

            return emitDelegate(obj, args);
        }

        private unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr* args)
        {
            InvokeFunc_RefArgs emitDelegate = CreateEmitDelegate(_method);

            if (!IsCollectible(_method))
            {
                _invokeFunc_RefArgs = emitDelegate;
            }

            return emitDelegate(obj, args);
        }

        private static bool IsCollectible(MethodBase method)
        {
            Type? declaringType = method.DeclaringType;
            return declaringType is not null && declaringType.Assembly.IsCollectible;
        }

        private static InvokeFunc_RefArgs CreateEmitDelegate(MethodBase method)
        {
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                return CreateInvokeDelegate_RefArgs(method);
            }
        }
    }
}
