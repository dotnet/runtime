﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal unsafe delegate object? InvokeFunc(object? target, IntPtr* arguments);

        public static unsafe InvokeFunc CreateInvokeDelegate(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !(emitNew || method.IsStatic);

            // The first parameter is unused but supports treating the DynamicMethod as an instance method which is slightly faster than a static.
            Type[] delegateParameters = new Type[3] { typeof(object), typeof(object), typeof(IntPtr*) };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: typeof(object),
                delegateParameters,
                restrictedSkipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_1);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            // Push the arguments.
            ParameterInfo[] parameters = method.GetParametersNoCopy();
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Call, Methods.ByReferenceOfByte_Value()); // This can be replaced by ldfld once byref fields are available in C#

                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer ? typeof(IntPtr) : parameterType);
                }
            }

            // Invoke the method.
            if (emitNew)
            {
                il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
            }
            else if (method.IsStatic || method.DeclaringType!.IsValueType)
            {
                il.Emit(OpCodes.Call, (MethodInfo)method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }

            // Handle the return.
            if (emitNew)
            {
                Type returnType = method.DeclaringType!;
                if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
            }
            else
            {
                RuntimeType returnType;
                if (method is RuntimeMethodInfo rmi)
                {
                    returnType = (RuntimeType)rmi.ReturnType;
                }
                else
                {
                    Debug.Assert(method is DynamicMethod);
                    returnType = (RuntimeType)((DynamicMethod)method).ReturnType;
                }

                if (returnType == typeof(void))
                {
                    il.Emit(OpCodes.Ldnull);
                }
                else if (returnType.IsValueType)
                {
                    il.Emit(OpCodes.Box, returnType);
                }
                else if (returnType.IsPointer)
                {
                    il.Emit(OpCodes.Ldtoken, returnType);
                    il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                    il.Emit(OpCodes.Call, Methods.Pointer_Box());
                }
                else if (returnType.IsByRef)
                {
                    // Check for null ref return.
                    Type elementType = returnType.GetElementType()!;
                    Label retValueOk = il.DefineLabel();
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Brtrue_S, retValueOk);
                    il.Emit(OpCodes.Call, Methods.ThrowHelper_Throw_NullReference_InvokeNullRefReturned());
                    il.MarkLabel(retValueOk);

                    // Handle per-type differences.
                    if (elementType.IsValueType)
                    {
                        il.Emit(OpCodes.Ldobj, elementType);
                        il.Emit(OpCodes.Box, elementType);
                    }
                    else if (elementType.IsPointer)
                    {
                        il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Conv_U);
                        il.Emit(OpCodes.Ldtoken, elementType);
                        il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                        il.Emit(OpCodes.Call, Methods.Pointer_Box());
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldobj, elementType);
                    }
                }
            }

            il.Emit(OpCodes.Ret);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc)dm.CreateDelegate(typeof(InvokeFunc), target: null);
        }

        private static class ThrowHelper
        {
            public static void Throw_NullReference_InvokeNullRefReturned()
            {
                throw new NullReferenceException(SR.NullReference_InvokeNullRefReturned);
            }
        }

        private static class Methods
        {
            private static MethodInfo? s_ByReferenceOfByte_Value;
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ByReference<>))]
            public static MethodInfo ByReferenceOfByte_Value() =>
                                      s_ByReferenceOfByte_Value ??
                                     (s_ByReferenceOfByte_Value = typeof(ByReference<byte>).GetMethod("get_Value")!);

            private static MethodInfo? s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned;
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(ThrowHelper))]
            public static MethodInfo ThrowHelper_Throw_NullReference_InvokeNullRefReturned() =>
                                      s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned ??
                                     (s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned = typeof(ThrowHelper).GetMethod(nameof(ThrowHelper.Throw_NullReference_InvokeNullRefReturned))!);

            private static MethodInfo? s_Pointer_Box;
            [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, typeof(Pointer))]
            public static MethodInfo Pointer_Box() =>
                                      s_Pointer_Box ??
                                     (s_Pointer_Box = typeof(Pointer).GetMethod(nameof(Pointer.Box), new[] { typeof(void*), typeof(Type) })!);

            private static MethodInfo? s_Type_GetTypeFromHandle;
            public static MethodInfo Type_GetTypeFromHandle() =>
                                      s_Type_GetTypeFromHandle ??
                                     (s_Type_GetTypeFromHandle = typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!);
        }
    }
}
