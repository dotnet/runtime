// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.TypeInfos;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    internal sealed class OpenMethodInvoker : MethodBaseInvoker
    {
        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        protected sealed override object CreateInstance(object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        protected internal sealed override object CreateInstance(Span<object?> arguments)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        protected internal sealed override object CreateInstanceWithFewArgs(Span<object?> arguments)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        protected internal sealed override object? Invoke(object? thisObject, Span<object?> arguments)
        {
            throw new InvalidOperationException(SR.Arg_UnboundGenParam);
        }

        protected internal sealed override object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments)
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
