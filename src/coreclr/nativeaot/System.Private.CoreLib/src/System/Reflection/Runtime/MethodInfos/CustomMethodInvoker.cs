// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using Internal.Reflection.Core.Execution;
using Internal.Runtime.Augments;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // Custom invoker for edge case scenarios not handled by the toolchain. Examples: Strings and Nullables.
    //
    internal sealed class CustomMethodInvoker : MethodBaseInvoker
    {
        public CustomMethodInvoker(Type thisType, Type[] parameterTypes, InvokerOptions options, CustomMethodInvokerAction action)
        {
            _action = action;
            _options = options;
            _thisType = thisType;
            _parameterTypes = parameterTypes;
        }

        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException) =>
            InvokeSpecial(thisObject, arguments, binderBundle, wrapInTargetInvocationException);

        protected internal sealed override object? Invoke(object? thisObject, Span<object?> arguments) =>
            InvokeSpecial(thisObject, arguments, binderBundle: null, wrapInTargetInvocationException: false);

        protected internal sealed override object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments) =>
            InvokeSpecial(thisObject, arguments, binderBundle: null, wrapInTargetInvocationException: false);

        private object? InvokeSpecial(object? thisObject, ReadOnlySpan<object?> arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            // This does not handle optional parameters. None of the methods we use custom invocation for have them.
            if (!(thisObject == null && 0 != (_options & InvokerOptions.AllowNullThis)))
                ValidateThis(thisObject, _thisType.TypeHandle);

            int argCount = (arguments != null) ? arguments.Length : 0;
            if (argCount != _parameterTypes.Length)
                throw new TargetParameterCountException();

            object[] convertedArguments = new object[argCount];
            for (int i = 0; i < convertedArguments.Length; i++)
            {
                convertedArguments[i] = RuntimeAugments.CheckArgument(arguments[i], _parameterTypes[i].TypeHandle, binderBundle);
            }
            object result;
            try
            {
                result = _action(thisObject, convertedArguments, _thisType);
            }
            catch (Exception e) when (wrapInTargetInvocationException)
            {
                throw new TargetInvocationException(e);
            }
            return result;
        }

        protected sealed override object CreateInstance(object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            // Custom method invokers need to also create the instance, so we just pass a null this.
            Debug.Assert((_options & InvokerOptions.AllowNullThis) != 0);
            return Invoke(null, arguments, binderBundle, wrapInTargetInvocationException);
        }

        protected internal sealed override object CreateInstance(Span<object?> arguments)
        {
            // Custom method invokers need to also create the instance, so we just pass a null this.
            Debug.Assert((_options & InvokerOptions.AllowNullThis) != 0);
            return Invoke(null, arguments);
        }

        protected internal sealed override object CreateInstanceWithFewArgs(Span<object?> arguments)
        {
            // Custom method invokers need to also create the instance, so we just pass a null this.
            Debug.Assert((_options & InvokerOptions.AllowNullThis) != 0);
            return InvokeDirectWithFewArgs(null, arguments);
        }

        public sealed override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            if (_thisType.IsConstructedGenericType && _thisType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (isOpen)
                {
                    return DynamicDelegateAugments.CreateObjectArrayDelegate(Type.GetTypeFromHandle(delegateType),
                        (args) =>
                        {
                            object[] arguments;
                            if (args.Length > 1)
                            {
                                arguments = new object[args.Length - 1];
                                Array.Copy(args, 1, arguments, 0, args.Length - 1);
                            }
                            else
                            {
                                arguments = Array.Empty<object>();
                            }

                            return _action(args[0], arguments, _thisType);
                        });
                }
                else
                {
                    // Desktop compat: MethodInfos to Nullable<T> methods cannot be turned into delegates.
                    throw new ArgumentException(SR.Arg_DlgtTargMeth);
                }
            }

            throw new PlatformNotSupportedException();
        }

        public sealed override IntPtr LdFtnResult => throw new PlatformNotSupportedException();

        private readonly InvokerOptions _options;
        private readonly CustomMethodInvokerAction _action;
        private readonly Type _thisType;
        private readonly Type[] _parameterTypes;
    }
}
