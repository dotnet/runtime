// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private unsafe Delegate CreateInvokeDelegateForInterpreted()
        {
            Debug.Assert(MethodInvokerCommon.UseInterpretedPath);
            Debug.Assert(_strategy == InvokerStrategy.Ref4 || _strategy == InvokerStrategy.RefMany);

            if (_method is RuntimeMethodInfo)
            {
                return (InvokeFunc_RefArgs)InterpretedInvoke_Method;
            }

            Debug.Assert(_method is RuntimeConstructorInfo);
            return (InvokeFunc_RefArgs)InterpretedInvoke_Constructor;
        }

        private unsafe object? InterpretedInvoke_Method(IntPtr _, object? obj, IntPtr *args)
        {
            object? ret = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return ret;
        }

        private unsafe object? InterpretedInvoke_Constructor(IntPtr _, object? obj, IntPtr *args)
        {
            object? ret = ((RuntimeConstructorInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? ret : null;
        }
    }
}
