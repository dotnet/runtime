// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Runtime.TypeInfos;
using System.Reflection.Runtime.ParameterInfos;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal sealed class OpenMethodInvoker : MethodInvoker
    {
        protected sealed override object? Invoke(object? thisObject, object?[] arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        public sealed override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        public sealed override IntPtr LdFtnResult
        {
            get
            {
                throw new InvalidOperationException(SR.Arg_UnboundGenParam);
            }
        }
    }
}
