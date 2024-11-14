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
            bool isForArrayInput,
            MethodBase method,
            Type declaringType,
            RuntimeType[] parameterTypes,
            Type returnType,
            out bool allocateObject,
            out IntPtr functionPointer,
            out Delegate invokeFunc,
            out InvokerStrategy strategy,
            out InvokerArgFlags[] invokerArgFlags)
        {
            functionPointer = method.MethodHandle.GetFunctionPointer();
            strategy = GetStrategy(parameterTypes, out invokerArgFlags);

            if (UseCalli(method))
            {
                allocateObject = method is RuntimeConstructorInfo;
                if (CanCache(method))
                {
                    // For constructors we allocate before calling invokeFunc so invokeFunc will be cacheable.
                    invokeFunc = GetOrCreateInvokeFunc(isForArrayInput, strategy, declaringType, parameterTypes, returnType, method.IsStatic);
                }
                else
                {
                    InvokeSignatureInfoKey signatureInfo = new(declaringType, parameterTypes, returnType, method.IsStatic);
                    invokeFunc = CreateInvokeFunc(isForArrayInput, method: null, signatureInfo, strategy);
                }
            }
            else
            {
                // Use Call\Callvirt\Newobj path.
                allocateObject = false;
                InvokeSignatureInfoKey signatureInfo = new(declaringType, parameterTypes, returnType, method.IsStatic);
                invokeFunc = CreateInvokeFunc(isForArrayInput, method, signatureInfo, strategy);
            }
        }

        private static InvokerStrategy GetStrategy(
            RuntimeType[] parameterTypes,
            out InvokerArgFlags[] invokerFlags)
        {
            bool needsByRefStrategy = false;
            int argCount = parameterTypes.Length;
            invokerFlags = new InvokerArgFlags[argCount];

            for (int i = 0; i < argCount; i++)
            {
                RuntimeType type = (RuntimeType)parameterTypes[i];
                if (RuntimeTypeHandle.IsByRef(type))
                {
                    type = (RuntimeType)type.GetElementType();
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

            return GetInvokerStrategy(argCount, needsByRefStrategy);
        }

        internal static bool UseCalli(MethodBase method)
        {
            Type declaringType = method.DeclaringType!;

            if (method is RuntimeConstructorInfo)
            {
                // Constructors are not polymorphic but avoid calli for constructors that require initialization through newobj.
                return !ReferenceEquals(declaringType, typeof(string)) && !declaringType.IsArray;
            }

            if (declaringType.IsValueType)
            {
                // For value types, calli is not supported for virtual methods (e.g. ToString()).
                return !method.IsVirtual;
            }

            // If not polymorphic and thus can be called with a calli instruction.
            return !method.IsVirtual || declaringType.IsSealed || method.IsFinal;
        }

        internal static bool CanCache(MethodBase method)
        {
            return CanCacheDynamicMethod(method) &&
                !HasDefaultParameterValues(method);

            static bool CanCacheDynamicMethod(MethodBase method) =>
                // The cache method's DeclaringType and other collectible parameters would be referenced.
                !method.DeclaringType!.Assembly.IsCollectible &&
                // An instance method on a value type needs to be unbox which requires its type in IL, so caching would not be very sharable.
                !(method.DeclaringType!.IsValueType && !method.IsStatic);

            // Supporting default values would increase cache memory usage and slow down the common case
            // since the default values would have to be cached as well.
            static bool HasDefaultParameterValues(MethodBase method)
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
        }

        private static InvokerStrategy GetInvokerStrategy(int argCount, bool needsByRefStrategy)
        {
            if (needsByRefStrategy)
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

        internal static Delegate CreateInvokeFunc(bool isForArrayInput, MethodBase? method, in InvokeSignatureInfoKey signatureInfo, InvokerStrategy strategy)
        {
            Delegate? invokeFunc;
            bool backwardsCompat = isForArrayInput && method is not null;

            if (!TryCreateWellKnownInvokeFunc(method, signatureInfo, strategy, out invokeFunc))
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

            return invokeFunc;

            static bool TryCreateWellKnownInvokeFunc(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, InvokerStrategy strategy, [NotNullWhen(true)] out Delegate? wellKnown)
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

        internal static Delegate GetOrCreateInvokeFunc(
            bool isForArrayInput,
            InvokerStrategy strategy,
            Type declaringType,
            RuntimeType[] parameterTypes,
            Type returnType,
            bool isStatic)
        {
            InvokeSignatureInfoKey key = InvokeSignatureInfoKey.CreateNormalized(declaringType, parameterTypes, returnType, isStatic);

            int hashcode = key.AlternativeGetHashCode();
            Delegate invokeFunc;
            unsafe
            {
                invokeFunc = s_invokerFuncs.GetValue<InvokeSignatureInfoKey>(hashcode, key, &InvokeSignatureInfoKey.AlternativeEquals);
            }

            if (invokeFunc is not null)
            {
                return invokeFunc;
            }

            if (s_invokerFuncsLock is null)
            {
                Interlocked.CompareExchange(ref s_invokerFuncsLock!, new object(), null);
            }

            // To minimize the lock scope, create the new delegate outside the lock even though it may not be used.
            Delegate newInvokeFunc = CreateInvokeFunc(isForArrayInput, method: null, key, strategy);
            bool lockTaken = false;
            try
            {
                Monitor.Enter(s_invokerFuncsLock, ref lockTaken);
                unsafe
                {
                    invokeFunc = s_invokerFuncs.GetValue<InvokeSignatureInfoKey>(hashcode, key, &InvokeSignatureInfoKey.AlternativeEquals);
                }
                if (invokeFunc is null)
                {
                    s_invokerFuncs[InvokeSignatureInfo.Create(key)] = newInvokeFunc;
                    invokeFunc = newInvokeFunc;
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Monitor.Exit(s_invokerFuncsLock);
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
