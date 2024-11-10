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

        internal static Delegate CreateInvokeFunc(MethodBase? method, InvokeSignatureInfo signatureInfo, InvokerStrategy strategy, bool backwardsCompat)
        {
            Delegate? wellKnown;

            return strategy switch
            {
                InvokerStrategy.Obj0 =>
                    TryGetWellKnownSignatureForInstanceAny(signatureInfo, out wellKnown) ? wellKnown :
                        CreateInvokeDelegate_Obj0Args(method, signatureInfo, backwardsCompat),
                InvokerStrategy.Obj1 =>
                    TryGetWellKnownSignatureForInstanceAnyVoid(signatureInfo, out wellKnown) ? wellKnown :
                        CreateInvokeDelegate_Obj1Arg(method, signatureInfo, backwardsCompat),
                InvokerStrategy.Obj4 => CreateInvokeDelegate_Obj4Args(method, signatureInfo, backwardsCompat),
                InvokerStrategy.ObjSpan => CreateInvokeDelegate_ObjSpanArgs(method, signatureInfo, backwardsCompat),
                _ => CreateInvokeDelegate_RefArgs(method, signatureInfo, backwardsCompat)
            };
        }

        internal static bool CanCacheDynamicMethod(MethodBase method) =>
            // The cache method's DeclaringType and other collectible parameters would be referenced.
            !method.IsCollectible &&
            // Value types need to be unboxed which requires its type, so a cached value would not be very sharable.
            !(method.DeclaringType!.IsValueType && !method.IsStatic) &&
            SupportsCalli(method);

        /// <summary>
        /// Determines if the method is not polymorphic and thus can be called with a calli instruction.
        /// </summary>
        internal static bool SupportsCalli(MethodBase method)
        {
            Debug.Assert(!method.DeclaringType!.IsValueType || method.DeclaringType.IsSealed);

            return method.IsStatic ||
                method.DeclaringType!.IsSealed ||
                method.IsFinal ||
                method is ConstructorInfo;
        }

        internal static Delegate GetOrCreateDynamicMethod(
            ref CerHashtable<InvokeSignatureInfo, Delegate> cache,
            ref object? lockObject,
            InvokeSignatureInfo signatureInfo,
            MethodBase method,
            InvokerStrategy strategy)
        {
            if (!CanCacheDynamicMethod(method))
            {
                return CreateInvokeFunc(method, signatureInfo, strategy, backwardsCompat: false);
            }

            // Get the cached dynamic method.
            Delegate invokeFunc = cache[signatureInfo];
            if (invokeFunc is null)
            {
                if (lockObject is null)
                {
                    Interlocked.CompareExchange(ref lockObject!, new object(), null);
                }

                bool lockTaken = false;
                try
                {
                    Monitor.Enter(lockObject, ref lockTaken);
                    invokeFunc = cache[signatureInfo];
                    if (invokeFunc is null)
                    {
                        invokeFunc = CreateInvokeFunc(method, signatureInfo, strategy, backwardsCompat: false);
                        cache[signatureInfo] = invokeFunc;
                    }
                }
                finally
                {
                    if (lockTaken)
                    {
                        Monitor.Exit(lockObject);
                    }
                }
            }

            return invokeFunc;
        }

        internal static bool HasDefaultParameterValues(MethodBase method)
        {
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue)
                {
                    return true;
                }
            }

            return false;
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
