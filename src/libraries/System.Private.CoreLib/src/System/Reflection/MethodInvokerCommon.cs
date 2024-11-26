// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal static partial class MethodInvokerCommon
    {
        private static CerHashtable<InvokeSignatureInfo, Delegate> s_invokerFuncs;
        private static object s_invokerFuncsLock = new object();
        private static bool s_wellKnownCacheAbandoned;

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

            // The well-known cache it is abandoned after a miss to avoid the cost of checking each time.
            if (!s_wellKnownCacheAbandoned)
            {
                if (TryGetWellKnownInvokeFunc(method, out invokeFunc, out strategy))
                {
                    functionPointer = IntPtr.Zero;
                    return;
                }

                s_wellKnownCacheAbandoned = true;
            }

            strategy = GetInvokerStrategy(parameterTypes.Length, needsByRefStrategy);

            if (UseInterpretedPath)
            {
                // The caller will create the invokeFunc; each of the 3 invoker classes have a different implementation.
                invokeFunc = null;
                functionPointer = IntPtr.Zero;
            }
            else if (UseCalli(method))
            {
                invokeFunc = GetOrCreateInvokeFunc(isForInvokerClasses, method, parameterTypes, returnType, strategy);
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

                // Generic types require newobj\call\callvirt.
                if (declaringType.IsGenericType || method.IsGenericMethod)
                {
                    return false;
                }

                // Arrays have element types that are not supported by calli plus the constructor is special.
                if (declaringType.IsArray)
                {
                    return false;
                }

                if (method is RuntimeConstructorInfo)
                {
                    // Strings require initialization through newobj.
                    if (ReferenceEquals(declaringType, typeof(string)))
                    {
                        return false;
                    }
                }
                else
                {
                    // Check if polymorphic. For value types, calli is not supported for object-based virtual methods (e.g. ToString()).
                    if (method.IsVirtual && (declaringType.IsValueType || (!declaringType.IsSealed && !method.IsFinal)))
                    {
                        return false;
                    }

                    // Error case; let the runtime handle it.
                    if (method.IsStatic && method.GetCustomAttribute<UnmanagedCallersOnlyAttribute>() is not null)
                    {
                        return false;
                    }
                }

                return true;
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

        private static Delegate GetOrCreateInvokeFunc(
            bool isForInvokerClasses,
            MethodBase method,
            RuntimeType[] parameterTypes,
            Type returnType,
            InvokerStrategy strategy)
        {
            InvokeSignatureInfoKey key = InvokeSignatureInfoKey.CreateNormalized((RuntimeType)method.DeclaringType!, parameterTypes, returnType, method.IsStatic);

            int hashcode = key.AlternativeGetHashCode();
            Delegate invokeFunc;
            unsafe
            {
                invokeFunc = s_invokerFuncs.GetValue<InvokeSignatureInfoKey>(hashcode, key, &InvokeSignatureInfoKey.AlternativeEquals);
            }

            if (invokeFunc is null)
            {
                // To minimize the lock scope, create the new delegate outside the lock even though it may not be used.
                Delegate newInvokeFunc = CreateIlInvokeFunc(isForInvokerClasses, method: null, callCtorAsMethod: false, key, strategy)!;
                lock (s_invokerFuncsLock)
                {
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
