// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
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
                    type = (RuntimeType)type.GetElementType();
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
            bool needsByRefStrategy)
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
                    invokeFunc_ObjSpanArgs = CreateInvokeDelegate_ObjSpanArgs(method.DeclaringType!, method.IsStatic, GetNormalizedCalliParameters(method), GetNormalizedCalliReturnType(method));
                }

                strategy |= InvokerStrategy.StrategyDetermined_ObjSpanArgs;
            }
        }

        internal static void DetermineStrategy_Obj4Args(
            ref InvokerStrategy strategy,
            ref InvokeFunc_Obj4Args? invokeFunc_Obj4Args,
            MethodBase method,
            bool needsByRefStrategy)
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
                    invokeFunc_Obj4Args = CreateInvokeDelegate_Obj4Args(method.DeclaringType!, method.IsStatic, GetNormalizedCalliParameters(method), GetNormalizedCalliReturnType(method));
                }

                strategy |= InvokerStrategy.StrategyDetermined_Obj4Args;
            }
        }

        internal static void DetermineStrategy_RefArgs(
            ref InvokerStrategy strategy,
            ref InvokeFunc_RefArgs? invokeFunc_RefArgs,
            MethodBase method)
        {
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
                    invokeFunc_RefArgs = CreateInvokeDelegate_RefArgs(method.DeclaringType!, method.IsStatic, GetNormalizedCalliParameters(method), GetNormalizedCalliReturnType(method));
                }

                strategy |= InvokerStrategy.StrategyDetermined_RefArgs;
            }
        }

        private static Type[] GetNormalizedCalliParameters(MethodBase method)
        {
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            Type[]? parameterTypes = parameters.Length == 0 ? Array.Empty<Type>() : new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = NormalizeCallIParameterType(parameters[i].ParameterType);
            }

            return parameterTypes;
        }

        internal static Type GetNormalizedCalliReturnType(MethodBase method)
        {
            Type returnType;

            if (method is RuntimeMethodInfo rmi)
            {
                returnType = NormalizeCallIParameterType(rmi.ReturnType);
            }
            else if (method is DynamicMethod dm)
            {
                returnType = NormalizeCallIParameterType(dm.ReturnType);
            }
            else
            {
                Debug.Assert(method is RuntimeConstructorInfo);
                returnType = typeof(void);
            }

            return returnType;
        }

        /// <summary>
        /// For reference types, use System.Object so we can share DynamicMethods in more places.
        /// </summary>
        internal static Type NormalizeCallIParameterType(Type type)
        {
            if (type.IsValueType || type.IsByRef || type.IsPointer || type.IsFunctionPointer)
            {
                return type;
            }

            return typeof(object);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IntPtr GetFunctionPointer(object? obj, IntPtr functionPointer, MethodBase method, Type[] parameterTypes)
        {
            if (obj is null || !method.IsVirtual || method.DeclaringType!.IsSealed || method.IsFinal)
            {
                return functionPointer;
            }

            Type actualType = obj.GetType();
            if (actualType == method.DeclaringType)
            {
                return functionPointer;
            }

            return GetFunctionPointerSlow(actualType, method, parameterTypes);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "Reflection implementation")]
        // todo: optimize the polymorphic checks below.
        internal static IntPtr GetFunctionPointerSlow(Type type, MethodBase method, Type[] parameterTypes)
        {
            if (method.DeclaringType!.IsInterface)
            {
                InterfaceMapping mapping = type.GetInterfaceMap(method.DeclaringType);

                for (int i = 0; i < mapping.InterfaceMethods.Length; i++)
                {
                    if (mapping.InterfaceMethods[i] == method)
                    {
                        return mapping.TargetMethods[i].MethodHandle.GetFunctionPointer();
                    }
                }

                throw new InvalidOperationException("todo:Method not found in interface mapping!!!");
            }

            method = type.GetMethod(method.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, parameterTypes)!;
            if (method == null)
            {
                throw new InvalidOperationException("todo:Method not found!!!");
            }

            return method.MethodHandle.GetFunctionPointer();
        }
    }
}
