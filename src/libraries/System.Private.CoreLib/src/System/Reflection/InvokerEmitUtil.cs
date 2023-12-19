// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal unsafe delegate object? InvokeFunc_RefArgs(object? obj, IntPtr* refArguments);
        internal delegate object? InvokeFunc_ObjSpanArgs(object? obj, Span<object?> arguments);
        internal delegate object? InvokeFunc_Obj4Args(object? obj, object? arg1, object? arg2, object? arg3, object? arg4);

        public static unsafe InvokeFunc_Obj4Args CreateInvokeDelegate_Obj4Args(MethodBase method, bool backwardsCompat)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !emitNew && !method.IsStatic;

            Type[] delegateParameters = new Type[5] { typeof(object), typeof(object), typeof(object), typeof(object), typeof(object) };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            // Push the arguments.
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            for (int i = 0; i < parameters.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;

                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, i + 1);
                        break;
                }

                if (parameterType.IsPointer || parameterType.IsFunctionPointer)
                {
                    Unbox(il, typeof(IntPtr));
                }
                else if (parameterType.IsValueType)
                {
                    Unbox(il, parameterType);
                }
            }

            EmitCallAndReturnHandling(il, method, emitNew, backwardsCompat);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_Obj4Args)dm.CreateDelegate(typeof(InvokeFunc_Obj4Args), target: null);
        }

        public static unsafe InvokeFunc_ObjSpanArgs CreateInvokeDelegate_ObjSpanArgs(MethodBase method, bool backwardsCompat)
        {
            Debug.Assert(!method.ContainsGenericParameters);

            bool emitNew = method is RuntimeConstructorInfo;
            bool hasThis = !emitNew && !method.IsStatic;

            // The first parameter is unused but supports treating the DynamicMethod as an instance method which is slightly faster than a static.
            Type[] delegateParameters = new Type[2] { typeof(object), typeof(Span<object>) };

            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            var dm = new DynamicMethod(
                InvokeStubPrefix + declaringTypeName + method.Name,
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            // Handle instance methods.
            if (hasThis)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }

            // Push the arguments.
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            for (int i = 0; i < parameters.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;

                il.Emit(OpCodes.Ldarga_S, 1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Call, Methods.Span_get_Item());
                il.Emit(OpCodes.Ldind_Ref);

                if (parameterType.IsPointer || parameterType.IsFunctionPointer)
                {
                    Unbox(il, typeof(IntPtr));
                }
                else if (parameterType.IsValueType)
                {
                    Unbox(il, parameterType);
                }
            }

            EmitCallAndReturnHandling(il, method, emitNew, backwardsCompat);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_ObjSpanArgs)dm.CreateDelegate(typeof(InvokeFunc_ObjSpanArgs), target: null);
        }

        public static unsafe InvokeFunc_RefArgs CreateInvokeDelegate_RefArgs(MethodBase method, bool backwardsCompat)
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
                typeof(object).Module, // Use system module to identify our DynamicMethods.
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
            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            for (int i = 0; i < parameters.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer || parameterType.IsFunctionPointer ? typeof(IntPtr) : parameterType);
                }
            }

            EmitCallAndReturnHandling(il, method, emitNew, backwardsCompat);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_RefArgs)dm.CreateDelegate(typeof(InvokeFunc_RefArgs), target: null);
        }

        private static void Unbox(ILGenerator il, Type parameterType)
        {
            // Unbox without using OpCodes.Unbox\UnboxAny to avoid a type check since that was already done by reflection.
            // Also required for unboxing true nullables created by reflection since that is not necessarily a valid CLI operation.
            Debug.Assert(parameterType.IsValueType);
            il.Emit(OpCodes.Call, Methods.Object_GetRawData());
            il.Emit(OpCodes.Ldobj, parameterType);
        }

        private static void EmitCallAndReturnHandling(ILGenerator il, MethodBase method, bool emitNew, bool backwardsCompat)
        {
            // For CallStack reasons, don't inline target method.
            // Mono interpreter does not support\need this.
            if (backwardsCompat && RuntimeFeature.IsDynamicCodeCompiled)
            {
#if MONO
                il.Emit(OpCodes.Call, Methods.DisableInline());
#else
                il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
                il.Emit(OpCodes.Pop);
#endif
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
                else if (returnType.IsFunctionPointer)
                {
                    il.Emit(OpCodes.Box, typeof(IntPtr));
                }
                else if (returnType.IsByRef)
                {
                    // Check for null ref return.
                    RuntimeType elementType = (RuntimeType)returnType.GetElementType()!;
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
                    else if (elementType.IsFunctionPointer)
                    {
                        il.Emit(OpCodes.Box, typeof(IntPtr));
                    }
                    else
                    {
                        il.Emit(OpCodes.Ldobj, elementType);
                    }
                }
            }

            il.Emit(OpCodes.Ret);
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
            private static FieldInfo? s_ByReferenceOfByte_Value;
            public static FieldInfo ByReferenceOfByte_Value() =>
                s_ByReferenceOfByte_Value ??= typeof(ByReference).GetField("Value")!;

            private static MethodInfo? s_Span_get_Item;
            public static MethodInfo Span_get_Item() =>
                s_Span_get_Item ??= typeof(Span<object>).GetProperty("Item")!.GetGetMethod()!;

            private static MethodInfo? s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned;
            public static MethodInfo ThrowHelper_Throw_NullReference_InvokeNullRefReturned() =>
                s_ThrowHelper_Throw_NullReference_InvokeNullRefReturned ??= typeof(ThrowHelper).GetMethod(nameof(ThrowHelper.Throw_NullReference_InvokeNullRefReturned))!;

            private static MethodInfo? s_Object_GetRawData;
            public static MethodInfo Object_GetRawData() =>
                s_Object_GetRawData ??= typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetRawData), BindingFlags.NonPublic | BindingFlags.Static)!;

            private static MethodInfo? s_Pointer_Box;
            public static MethodInfo Pointer_Box() =>
                s_Pointer_Box ??= typeof(Pointer).GetMethod(nameof(Pointer.Box), new[] { typeof(void*), typeof(Type) })!;

            private static MethodInfo? s_Type_GetTypeFromHandle;
            public static MethodInfo Type_GetTypeFromHandle() =>
                s_Type_GetTypeFromHandle ??= typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), new[] { typeof(RuntimeTypeHandle) })!;

#if MONO
            private static MethodInfo? s_DisableInline;
            public static MethodInfo DisableInline() =>
                s_DisableInline ??= typeof(System.Runtime.CompilerServices.JitHelpers).GetMethod(nameof(System.Runtime.CompilerServices.JitHelpers.DisableInline), BindingFlags.NonPublic | BindingFlags.Static)!;
#else
            private static MethodInfo? s_NextCallReturnAddress;
            public static MethodInfo NextCallReturnAddress() =>
                s_NextCallReturnAddress ??= typeof(StubHelpers.StubHelpers).GetMethod(nameof(StubHelpers.StubHelpers.NextCallReturnAddress), BindingFlags.NonPublic | BindingFlags.Static)!;
#endif
        }
    }
}
