// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Threading;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.RuntimeType;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        private static CerHashtable<InvokeSignatureInfo, Delegate> s_invokerFuncs;
        private static object? s_invokerFuncsLock;

        internal static void Initialize(
            bool isForInvokerClasses,
            MethodBase method,
            RuntimeType[] parameterTypes,
            Type returnType,
            out IntPtr functionPointer,
            out Delegate? invokeFunc,
            out InvokerStrategy strategy,
            out InvokerArgFlags[] invokerArgFlags)
        {
            invokerArgFlags = GetInvokerArgFlags(parameterTypes, out bool needsByRefStrategy);
            strategy = GetInvokerStrategy(parameterTypes.Length, needsByRefStrategy);

            if (UseInterpretedPath)
            {
                // The caller will create the invokeFunc; each of the 3 invoker classes have a different implementation.
                invokeFunc = null;
                functionPointer = IntPtr.Zero;
            }
            else if (UseCalli(method))
            {
                InvokeSignatureInfoKey key = InvokeSignatureInfoKey.CreateNormalized((RuntimeType)method.DeclaringType!, parameterTypes, returnType, method.IsStatic);
                invokeFunc = GetWellKnownInvokeFunc(key, strategy);
                invokeFunc ??= GetOrCreateInvokeFunc(isForInvokerClasses, key, strategy);
                functionPointer = method.MethodHandle.GetFunctionPointer();
            }
            else
            {
                InvokeSignatureInfoKey signatureInfo = new((RuntimeType?)method.DeclaringType, parameterTypes, returnType, method.IsStatic);
                invokeFunc = CreateIlInvokeFunc(isForInvokerClasses, method, callCtorAsMethod: false, signatureInfo, strategy);
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

            return invokerFlags;
        }

        private static bool UseCalli(MethodBase method)
        {
            return SupportsCalli(method) && CanCache(method);

            static bool SupportsCalli(MethodBase method)
            {
                if (method is DynamicMethod)
                {
                    return false;
                }

                RuntimeType declaringType = (RuntimeType)method.DeclaringType!;

                if (declaringType.IsGenericType || method.IsGenericMethod)
                {
                    // Generic types require newobj\call\callvirt.
                    return false;
                }

                if (method is RuntimeConstructorInfo)
                {
                    // Strings and arrays require initialization through newobj.
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

            static bool CanCache(MethodBase method)
            {
                return
                    // The cache method's DeclaringType and other collectible parameters would be referenced.
                    !method.DeclaringType!.Assembly.IsCollectible &&
                    // An instance method on a value type needs to be unbox which requires its type in IL, so caching would not be very sharable.
                    !(method.DeclaringType!.IsValueType && !method.IsStatic);
            }
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

        internal static Delegate CreateIlInvokeFunc(bool isForInvokerClasses, MethodBase? method, bool callCtorAsMethod, in InvokeSignatureInfoKey signatureInfo, InvokerStrategy strategy)
        {
            Debug.Assert(!UseInterpretedPath);

            bool backwardsCompat = method is null ? false : !isForInvokerClasses;

            return strategy switch
            {
                InvokerStrategy.Obj0 => CreateInvokeDelegateForObj0Args(method, callCtorAsMethod, signatureInfo, backwardsCompat),
                InvokerStrategy.Obj1 => CreateInvokeDelegateForObj1Arg(method, callCtorAsMethod, signatureInfo, backwardsCompat),
                InvokerStrategy.Obj4 => CreateInvokeDelegateForObj4Args(method, callCtorAsMethod, signatureInfo, backwardsCompat),
                InvokerStrategy.ObjSpan => CreateInvokeDelegateForObjSpanArgs(method, callCtorAsMethod, signatureInfo, backwardsCompat),
                _ => CreateInvokeDelegateForRefArgs(method, callCtorAsMethod, signatureInfo, backwardsCompat)
            };
        }

        internal static Delegate? GetWellKnownInvokeFunc(in InvokeSignatureInfoKey signatureInfo, InvokerStrategy strategy)
        {
            // Check if the method has a well-known signature that can be invoked directly using calli and a function pointer.
            switch (strategy)
            {
                case InvokerStrategy.Obj0:
                    return GetWellKnownSignatureFor0Args(signatureInfo);
                case InvokerStrategy.Obj1:
                    return GetWellKnownSignatureFor1Arg(signatureInfo);
            }

            return null;
        }

        internal static Delegate GetOrCreateInvokeFunc(
            bool isForInvokerClasses,
            InvokeSignatureInfoKey key,
            InvokerStrategy strategy)
        {
            Debug.Assert(!UseInterpretedPath);

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
            Delegate newInvokeFunc = CreateIlInvokeFunc(isForInvokerClasses, method: null, callCtorAsMethod: false, key, strategy)!;
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
