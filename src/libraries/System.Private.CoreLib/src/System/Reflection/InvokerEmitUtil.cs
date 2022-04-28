// Licensed to the .NET Foundation under one or more agreements.
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

        // The 'obj' parameter allows the DynamicMethod to be treated as an instance method which is slightly faster than a static.
        internal unsafe delegate object? InvokeFunc<T>(T obj, object? target, IntPtr* arguments);

        public static unsafe InvokeFunc<T> CreateInvokeDelegate<T>(MethodBase method)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !(emitNew || method.IsStatic);

            Type[] delegateParameters = new Type[3] { typeof(T), typeof(object), typeof(IntPtr*) };

            var dm = new DynamicMethod(
                InvokeStubPrefix + method.DeclaringType!.Name + "." + method.Name,
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
                RuntimeType parameterType = NormalizePointerType((RuntimeType)parameters[i].ParameterType);

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
                RuntimeType returnType = (RuntimeType)((RuntimeMethodInfo)method).ReturnType;
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

            // Create the delegate; it is also compiled at this point due to skipVisibility=true.
            return (InvokeFunc<T>)dm.CreateDelegate(typeof(InvokeFunc<T>));
        }

        private static RuntimeType NormalizePointerType(RuntimeType type)
        {
            if (type.IsPointer)
            {
                type = (RuntimeType)typeof(IntPtr);
            }
            else if (type.IsByRef && RuntimeTypeHandle.GetElementType(type).IsPointer)
            {
                type = (RuntimeType)typeof(IntPtr).MakeByRefType();
            }

            return type;
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
