// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static class MethodInvokerCommon
    {
        internal static void Initialize(
            RuntimeType[] argumentTypes,
            out InvokerStrategy strategy,
            out InvokerArgFlags[] invokerFlags,
            out bool needsByRefStrategy)
        {
            if (LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke)
            {
                // Always use the native interpreted invoke.
                // Useful for testing, to avoid startup overhead of emit, or for calling a ctor on already initialized object.
                strategy = GetStrategyForUsingInterpreted();
            }
            else if (LocalAppContextSwitches.ForceEmitInvoke && !LocalAppContextSwitches.ForceInterpretedInvoke)
            {
                // Always use emit invoke (if IsDynamicCodeSupported == true); useful for testing.
                strategy = GetStrategyForUsingEmit();
            }
            else
            {
                strategy = default;
            }

            int argCount = argumentTypes.Length;
            invokerFlags = new InvokerArgFlags[argCount];
            needsByRefStrategy = false;

            for (int i = 0; i < argCount; i++)
            {
                RuntimeType type = argumentTypes[i];
                if (RuntimeTypeHandle.IsByRef(type))
                {
                    type = (RuntimeType)type.GetElementType()!;
                    invokerFlags[i] |= InvokerArgFlags.IsValueType_ByRef_Or_Pointer;
                    needsByRefStrategy = true;
                    if (type.IsNullableOfT)
                    {
                        invokerFlags[i] |= InvokerArgFlags.IsNullableOfT;
                    }
                }

                if (RuntimeTypeHandle.IsPointer(type))
                {
                    invokerFlags[i] |= InvokerArgFlags.IsValueType | InvokerArgFlags.IsValueType_ByRef_Or_Pointer;
                }
                else if (RuntimeTypeHandle.IsFunctionPointer(type))
                {
                    invokerFlags[i] |= InvokerArgFlags.IsValueType;
                }
                else if (type.IsActualValueType)
                {
                    invokerFlags[i] |= InvokerArgFlags.IsValueType | InvokerArgFlags.IsValueType_ByRef_Or_Pointer;

                    if (type.IsNullableOfT)
                    {
                        invokerFlags[i] |= InvokerArgFlags.IsNullableOfT;
                    }
                }
            }
        }

        internal static InvokerStrategy GetStrategyForUsingInterpreted()
        {
            // This causes the default strategy, which is interpreted, to always be used.
            return InvokerStrategy.StrategyDetermined_Obj4Args | InvokerStrategy.StrategyDetermined_ObjSpanArgs | InvokerStrategy.StrategyDetermined_RefArgs;
        }

        private static InvokerStrategy GetStrategyForUsingEmit()
        {
            // This causes the emit strategy, if supported, to be used on the first call as well as subsequent calls.
            return InvokerStrategy.HasBeenInvoked_Obj4Args | InvokerStrategy.HasBeenInvoked_ObjSpanArgs | InvokerStrategy.HasBeenInvoked_RefArgs;
        }

        /// <summary>
        /// Confirm member invocation has an instance and is of the correct type
        /// </summary>
        internal static void ValidateInvokeTarget(object? target, MethodBase method)
        {
            Debug.Assert(!method.IsStatic);

            if (target == null)
            {
                throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);
            }

            if (!method.DeclaringType!.IsInstanceOfType(target))
            {
                throw new TargetException(SR.Format(SR.RFLCT_Targ_ITargMismatch_WithType, method.DeclaringType, target.GetType()));
            }
        }

        internal static void DetermineStrategy_ObjSpanArgs(
            ref InvokerStrategy strategy,
            ref InvokeFunc_ObjSpanArgs?
            invokeFunc_ObjSpanArgs,
            MethodBase method,
#if MONO
            bool needsByRefStrategy,
            bool backwardsCompat)
#else
            bool needsByRefStrategy)
#endif
        {
            if (needsByRefStrategy)
            {
                // If ByRefs are used, we can't use this strategy.
                strategy |= InvokerStrategy.StrategyDetermined_ObjSpanArgs;
            }
            else if (((strategy & InvokerStrategy.HasBeenInvoked_ObjSpanArgs) == 0) && !Debugger.IsAttached)
            {
                // The first time, ignoring race conditions, use the slow path, except for the case when running under a debugger.
                // This is a workaround for the debugger issues with understanding exceptions propagation over the slow path.
                strategy |= InvokerStrategy.HasBeenInvoked_ObjSpanArgs;
            }
            else
            {
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
#if MONO
                    invokeFunc_ObjSpanArgs = CreateInvokeDelegate_ObjSpanArgs(method, backwardsCompat);
#else
                    invokeFunc_ObjSpanArgs = CreateInvokeDelegate_ObjSpanArgs(method);
#endif
                }

                strategy |= InvokerStrategy.StrategyDetermined_ObjSpanArgs;
            }
        }

        internal static void DetermineStrategy_Obj4Args(
            ref InvokerStrategy strategy,
            ref InvokeFunc_Obj4Args? invokeFunc_Obj4Args,
            MethodBase method,
#if MONO
            bool needsByRefStrategy,
            bool backwardsCompat)
#else
            bool needsByRefStrategy)
#endif
        {
            if (needsByRefStrategy)
            {
                // If ByRefs are used, we can't use this strategy.
                strategy |= InvokerStrategy.StrategyDetermined_Obj4Args;
            }
            else if (((strategy & InvokerStrategy.HasBeenInvoked_Obj4Args) == 0) && !Debugger.IsAttached)
            {
                // The first time, ignoring race conditions, use the slow path, except for the case when running under a debugger.
                // This is a workaround for the debugger issues with understanding exceptions propagation over the slow path.
                strategy |= InvokerStrategy.HasBeenInvoked_Obj4Args;
            }
            else
            {
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
#if MONO
                    invokeFunc_Obj4Args = CreateInvokeDelegate_Obj4Args(method, backwardsCompat);
#else
                    invokeFunc_Obj4Args = CreateInvokeDelegate_Obj4Args(method);
#endif
                }

                strategy |= InvokerStrategy.StrategyDetermined_Obj4Args;
            }
        }

        internal static void DetermineStrategy_RefArgs(
            ref InvokerStrategy strategy,
            ref InvokeFunc_RefArgs? invokeFunc_RefArgs,
#if MONO
            MethodBase method,
            bool backwardsCompat)
#else
            MethodBase method)
#endif
        {
#if !MONO
            if (((strategy & InvokerStrategy.HasBeenInvoked_RefArgs) == 0) && !Debugger.IsAttached)
            {
                // The first time, ignoring race conditions, use the interpreted path (already assigned
                // in the constructor). This avoids the cost of emit+JIT for single-use invocations.
                strategy |= InvokerStrategy.HasBeenInvoked_RefArgs;
            }
            else
            {
                // Skip caching for collectible assemblies: the DynamicMethod holds token
                // references to types that would prevent the assembly from being unloaded.
                bool isCollectible = method.DeclaringType is { Assembly.IsCollectible: true };
                if (RuntimeFeature.IsDynamicCodeSupported && !isCollectible)
                {
                    invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(method);
                }

                strategy |= InvokerStrategy.StrategyDetermined_RefArgs;
            }

            return;
#else
            if (!backwardsCompat)
            {
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
                    invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(method, backwardsCompat);
                }

                strategy |= InvokerStrategy.StrategyDetermined_RefArgs;
                return;
            }

            if (((strategy & InvokerStrategy.HasBeenInvoked_RefArgs) == 0) && !Debugger.IsAttached)
            {
                // The first time, ignoring race conditions, use the slow path, except for the case when running under a debugger.
                // This is a workaround for the debugger issues with understanding exceptions propagation over the slow path.
                strategy |= InvokerStrategy.HasBeenInvoked_RefArgs;
            }
            else
            {
                if (RuntimeFeature.IsDynamicCodeSupported)
                {
#if MONO
                    invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(method, backwardsCompat);
#else
                    invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(method);
#endif
                }

                strategy |= InvokerStrategy.StrategyDetermined_RefArgs;
            }
#endif
        }
    }
}
