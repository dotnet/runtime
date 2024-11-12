// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        private static CerHashtable<InvokeSignatureInfo, Delegate> s_invokerFuncs;
        private static object? s_invokerFuncsLock;

        internal static void Initialize(
            InvokeSignatureInfo signatureInfo,
            out bool needsByRefStrategy,
            out InvokerArgFlags[] invokerFlags)
        {
            needsByRefStrategy = false;
            ReadOnlySpan<Type> parameterTypes = signatureInfo.ParameterTypes;
            int argCount = parameterTypes.Length;
            invokerFlags = new InvokerArgFlags[argCount];

            // Set invokerFlags[] and needsByRefStrategy.
            for (int i = 0; i < argCount; i++)
            {
                RuntimeType type = (RuntimeType)parameterTypes[i];
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

        public static InvokerStrategy GetInvokerStrategyForSpanInput(int argCount, bool needsByRefStrategy)
        {
            if (needsByRefStrategy)
            {
                return InvokerStrategy.RefMany;
            }

            return argCount <= 4 ? InvokerStrategy.Obj4 : InvokerStrategy.ObjSpan;
        }

        internal static Delegate CreateInvokeFunc(MethodBase? method, InvokeSignatureInfo signatureInfo, InvokerStrategy strategy, bool isForArrayInput, bool backwardsCompat)
        {
            Delegate? invokeFunc;

            if (!TryCreateWellKnownInvokeFunc(method, signatureInfo, strategy, out invokeFunc))
            {
                if (isForArrayInput)
                {
                    invokeFunc = strategy switch
                    {
                        InvokerStrategy.Obj0 => CreateInvokeDelegate_Obj0Args(method, signatureInfo, backwardsCompat),
                        InvokerStrategy.Obj1 => CreateInvokeDelegate_Obj1Arg(method, signatureInfo, backwardsCompat),
                        InvokerStrategy.Obj4 or InvokerStrategy.ObjSpan => CreateInvokeDelegate_ObjSpanArgs(method, signatureInfo, backwardsCompat),
                        _ => CreateInvokeDelegate_RefArgs(method, signatureInfo, backwardsCompat)
                    };
                }
                else
                {
                    invokeFunc = strategy switch
                    {
                        InvokerStrategy.Obj0 => CreateInvokeDelegate_Obj0Args(method, signatureInfo, backwardsCompat),
                        InvokerStrategy.Obj1 => CreateInvokeDelegate_Obj1Arg(method, signatureInfo, backwardsCompat),
                        InvokerStrategy.Obj4 => CreateInvokeDelegate_Obj4Args(method, signatureInfo, backwardsCompat),
                        InvokerStrategy.ObjSpan => CreateInvokeDelegate_ObjSpanArgs(method, signatureInfo, backwardsCompat),
                        _ => CreateInvokeDelegate_RefArgs(method, signatureInfo, backwardsCompat)
                    };
                }
            }

            return invokeFunc;

            static bool TryCreateWellKnownInvokeFunc(MethodBase? method, InvokeSignatureInfo signatureInfo, InvokerStrategy strategy, [NotNullWhen(true)] out Delegate? wellKnown)
            {
                if (method is null)
                {
                    // Check if the method has a well-known signature that can be invoked directly using calli and a function pointer.
                    switch (strategy)
                    {
                        case InvokerStrategy.Obj0:
                            if (TryGetWellKnownSignatureFor0Args(signatureInfo, out wellKnown)) return true;
                            break;
                        case InvokerStrategy.Obj1:
                            if (TryGetWellKnownSignatureFor1Arg(signatureInfo, out wellKnown)) return true;
                            break;
                    }
                }

                wellKnown = null;
                return false;
            }
        }

        internal static bool CanCacheDynamicMethod(MethodBase method) =>
            // The cache method's DeclaringType and other collectible parameters would be referenced.
            !method.IsCollectible &&
            // Value types need to be unboxed which requires its type, so a cached value would not be very sharable.
            !(method.DeclaringType!.IsValueType && !method.IsStatic);

        /// <summary>
        /// Determines if the method is not polymorphic and thus can be called with a calli instruction.
        /// </summary>
        internal static bool SupportsCalli(MethodBase method)
        {
            return method.IsStatic ||
                method.DeclaringType!.IsSealed ||
                method.IsFinal ||
                (
                    method is ConstructorInfo &&
                    // A string cannot be allocated with an uninitialized value, so we use NewObj.
                    !ReferenceEquals(method.DeclaringType, typeof(string))
                );
        }

        internal static Delegate GetOrCreateInvokeFunc(
            InvokeSignatureInfo signatureInfo,
            InvokerStrategy strategy,
            bool isForArrayInput,
            bool backwardsCompat)
        {
            // Get the cached dynamic method.
            Delegate invokeFunc = s_invokerFuncs[signatureInfo];
            if (invokeFunc is null)
            {
                if (s_invokerFuncsLock is null)
                {
                    Interlocked.CompareExchange(ref s_invokerFuncsLock!, new object(), null);
                }

                bool lockTaken = false;
                try
                {
                    Monitor.Enter(s_invokerFuncsLock, ref lockTaken);
                    invokeFunc = s_invokerFuncs[signatureInfo];
                    if (invokeFunc is null)
                    {
                        invokeFunc = CreateInvokeFunc(method: null, signatureInfo, strategy, isForArrayInput, backwardsCompat);
                        s_invokerFuncs[signatureInfo] = invokeFunc;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(s_invokerFuncsLock);
                    }
                }
            }

            return invokeFunc;
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
    }
}
