// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // This is an instance method to avoid overhead of shuffle thunk.
        internal unsafe delegate object? InvokeFunc<T>(T obj, object? target, IntPtr* arguments);

        public static unsafe InvokeFunc<T> CreateInvokeDelegate<T>(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !(emitNew || method.IsStatic);

            Type[] delegateParameters = new Type[3] { typeof(T), typeof(object), typeof(IntPtr*) };

            var dm = new DynamicMethod(
                "InvokeStub_" + method.DeclaringType!.Name + "." + method.Name,
                returnType: typeof(object),
                delegateParameters,
                owner: typeof(T),
                skipVisibility: true);

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
                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                if (parameterType.IsPointer)
                {
                    parameterType = (RuntimeType)typeof(IntPtr);
                }
                else if (parameterType.IsByRef && RuntimeTypeHandle.GetElementType(parameterType).IsPointer)
                {
                    parameterType = (RuntimeType)typeof(IntPtr).MakeByRefType();
                }

                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Call, Methods.ByReferenceOfByte_Value()); // This can be replaced by ldfld once byref fields are available in C#
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType);
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
                Type returnType = ((RuntimeMethodInfo)method).ReturnType;
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
                        LocalBuilder? local_return = il.DeclareLocal(typeof(object));
                        il.Emit(OpCodes.Ldobj, elementType);
                        il.Emit(OpCodes.Box, elementType);
                        il.Emit(OpCodes.Stloc_S, local_return);
                        il.Emit(OpCodes.Ldloc_S, local_return);
                    }
                    else if (elementType.IsPointer)
                    {
                        LocalBuilder? local_return = il.DeclareLocal(elementType);
                        il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Stloc_S, local_return);
                        il.Emit(OpCodes.Ldloc_S, local_return);
                        il.Emit(OpCodes.Ldtoken, elementType);
                        il.Emit(OpCodes.Call, Methods.Type_GetTypeFromHandle());
                        il.Emit(OpCodes.Call, Methods.Pointer_Box());
                    }
                    else
                    {
                        LocalBuilder? local_return = il.DeclareLocal(elementType);
                        il.Emit(OpCodes.Ldind_Ref);
                        il.Emit(OpCodes.Stloc_S, local_return);
                        il.Emit(OpCodes.Ldloc_S, local_return);
                    }
                }
            }

            il.Emit(OpCodes.Ret);

            // Create the delegate.
            return (InvokeFunc<T>)dm.CreateDelegate(typeof(InvokeFunc<T>));
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
