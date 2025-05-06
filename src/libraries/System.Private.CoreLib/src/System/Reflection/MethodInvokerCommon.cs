// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    /// <summary>
    /// Shared functionality for MethodBaseInvoker, MethodInvoker and ConstructorInvoker.
    /// </summary>
    /// <remarks>
    /// This class is known by the runtime in order to ignore reflection frames during stack walks.
    /// </remarks>
    internal static partial class MethodInvokerCommon
    {
        internal static void Initialize(
            bool backwardsCompat,
            MethodBase method,
            RuntimeType[] parameterTypes,
            RuntimeType returnType,
            bool callCtorAsMethod,
            out IntPtr functionPointer,
            out Delegate? invokeFunc,
            out InvokerStrategy strategy,
            out InvokerArgFlags[] invokerArgFlags)
        {
            invokerArgFlags = GetInvokerArgFlags(parameterTypes, out bool needsByRefStrategy);
            strategy = GetInvokerStrategy(parameterTypes.Length, needsByRefStrategy);

            if (TryGetCalliFunc(method, parameterTypes, returnType, strategy, out invokeFunc))
            {
                functionPointer = method.MethodHandle.GetFunctionPointer();
                Debug.Assert(functionPointer != IntPtr.Zero);
            }
            else if (UseInterpretedPath)
            {
                invokeFunc = null; // The caller will create the invokeFunc.
                functionPointer = IntPtr.Zero;
            }
            else
            {
                invokeFunc = CreateIlInvokeFunc(backwardsCompat, method, callCtorAsMethod, strategy);
                functionPointer = IntPtr.Zero;
            }
        }

        private static InvokerArgFlags[] GetInvokerArgFlags(RuntimeType[] parameterTypes, out bool needsByRefStrategy)
        {
            needsByRefStrategy = false;
            int argCount = parameterTypes.Length;
            InvokerArgFlags[] invokerFlags = new InvokerArgFlags[argCount];

            for (int i = 0; i < argCount; i++)
            {
                RuntimeType type = (RuntimeType)parameterTypes[i];
                if (RuntimeTypeHandle.IsByRef(type))
                {
                    type = (RuntimeType)type.GetElementType()!;
                    invokerFlags[i] |= InvokerArgFlags.IsValueType_ByRef_Or_Pointer;
                    needsByRefStrategy = true;
                    if (type.IsValueType)
                    {
                        invokerFlags[i] |= InvokerArgFlags.IsByRefForValueType;

                        if (type.IsNullableOfT)
                        {
                            invokerFlags[i] |= InvokerArgFlags.IsNullableOfT;
                        }
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

            return invokerFlags;
        }

        private static InvokerStrategy GetInvokerStrategy(int argCount, bool needsByRefStrategy)
        {
            if (needsByRefStrategy || UseInterpretedPath)
            {
                return argCount <= 4 ? InvokerStrategy.Ref4 : InvokerStrategy.RefMany;
            }

            return argCount switch
            {
                0 => InvokerStrategy.Obj0,
                1 => InvokerStrategy.Obj1,
                2 or 3 or 4 => InvokerStrategy.Obj4,
                _ => InvokerStrategy.ObjSpan
            };
        }

        internal static Delegate CreateIlInvokeFunc(bool backwardsCompat, MethodBase method, bool callCtorAsMethod, InvokerStrategy strategy)
        {
            Debug.Assert(!UseInterpretedPath);

            return strategy switch
            {
                InvokerStrategy.Obj0 => CreateInvokeDelegateForObj0Args(method, callCtorAsMethod, backwardsCompat),
                InvokerStrategy.Obj1 => CreateInvokeDelegateForObj1Arg(method, callCtorAsMethod, backwardsCompat),
                InvokerStrategy.Obj4 => CreateInvokeDelegateForObj4Args(method, callCtorAsMethod, backwardsCompat),
                InvokerStrategy.ObjSpan => CreateInvokeDelegateForObjSpanArgs(method, callCtorAsMethod, backwardsCompat),
                _ => CreateInvokeDelegateForRefArgs(method, callCtorAsMethod, backwardsCompat)
            };
        }

        /// <summary>
        /// Confirm member invocation has an instance and is of the correct type.
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
    }
}
