// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static unsafe class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses this prefix to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal delegate object? InvokeFunc_Obj0Args(IntPtr functionPointer, object? obj);
        internal delegate object? InvokeFunc_Obj1Arg(IntPtr functionPointer, object? obj, object? arg1);
        internal delegate object? InvokeFunc_Obj4Args(IntPtr functionPointer, object? obj, object? arg1, object? arg2, object? arg3, object? arg4);
        internal delegate object? InvokeFunc_ObjSpanArgs(IntPtr functionPointer, object? obj, Span<object?> arguments);
        internal delegate object? InvokeFunc_RefArgs(IntPtr functionPointer, object? obj, IntPtr* refArguments);

        public static InvokeFunc_Obj0Args CreateInvokeDelegateForObj0Args(MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, [typeof(IntPtr), typeof(object)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, callCtorAsMethod);
            EmitCall(il, method, callCtorAsMethod, backwardsCompat);
            EmitReturnHandling(il, GetReturnType(method, callCtorAsMethod));
            return (InvokeFunc_Obj0Args)dm.CreateDelegate(typeof(InvokeFunc_Obj0Args), target: null);
        }

        public static InvokeFunc_Obj1Arg CreateInvokeDelegateForObj1Arg(MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, [typeof(IntPtr), typeof(object), typeof(object)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, callCtorAsMethod);

            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            Debug.Assert(parameters.Length == 1);
            il.Emit(OpCodes.Ldarg_2);
            UnboxSpecialType(il, (RuntimeType)parameters[0].ParameterType);

            EmitCall(il, method, callCtorAsMethod, backwardsCompat);
            EmitReturnHandling(il, GetReturnType(method, callCtorAsMethod));
            return (InvokeFunc_Obj1Arg)dm.CreateDelegate(typeof(InvokeFunc_Obj1Arg), target: null);
        }

        public static InvokeFunc_Obj4Args CreateInvokeDelegateForObj4Args(MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, [typeof(IntPtr), typeof(object), typeof(object), typeof(object), typeof(object), typeof(object)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, callCtorAsMethod);

            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            for (int i = 0; i < parameters.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameters[i].ParameterType;

                switch (i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        il.Emit(OpCodes.Ldarg_S, i + 2);
                        break;
                }

                UnboxSpecialType(il, parameterType);
            }

            EmitCall(il, method, callCtorAsMethod, backwardsCompat);
            EmitReturnHandling(il, GetReturnType(method, callCtorAsMethod));
            return (InvokeFunc_Obj4Args)dm.CreateDelegate(typeof(InvokeFunc_Obj4Args), target: null);
        }

        public static InvokeFunc_ObjSpanArgs CreateInvokeDelegateForObjSpanArgs(MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, [typeof(IntPtr), typeof(object), typeof(Span<object>)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, callCtorAsMethod);

            ReadOnlySpan<ParameterInfo> parameterTypes = method.GetParametersAsSpan();
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameterTypes[i].ParameterType;

                il.Emit(OpCodes.Ldarga_S, 2);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Call, Methods.Span_get_Item());
                il.Emit(OpCodes.Ldind_Ref);

                UnboxSpecialType(il, parameterType);
            }

            EmitCall(il, method, callCtorAsMethod, backwardsCompat);
            EmitReturnHandling(il, GetReturnType(method, callCtorAsMethod));
            return (InvokeFunc_ObjSpanArgs)dm.CreateDelegate(typeof(InvokeFunc_ObjSpanArgs), target: null);
        }

        public static InvokeFunc_RefArgs CreateInvokeDelegateForRefArgs(MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, [typeof(IntPtr), typeof(object), typeof(IntPtr*)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, callCtorAsMethod);

            ReadOnlySpan<ParameterInfo> parameterTypes = method.GetParametersAsSpan();
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)parameterTypes[i].ParameterType;
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer || parameterType.IsFunctionPointer ? typeof(IntPtr) : parameterType);
                }
            }

            EmitCall(il, method, callCtorAsMethod, backwardsCompat);
            EmitReturnHandling(il, GetReturnType(method, callCtorAsMethod));
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

        private static void UnboxSpecialType(ILGenerator il, Type parameterType)
        {
            if (parameterType.IsPointer || parameterType.IsFunctionPointer)
            {
                Unbox(il, typeof(IntPtr));
            }
            else if (parameterType.IsValueType)
            {
                Unbox(il, parameterType);
            }
        }

        private static DynamicMethod CreateDynamicMethod(MethodBase method, Type[] delegateParameters)
        {
            string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
            string stubName = InvokeStubPrefix + declaringTypeName + method.Name;

            return new DynamicMethod(
                stubName,
                returnType: typeof(object),
                delegateParameters,
                method?.DeclaringType is Type declaringType ? declaringType.Module : typeof(object).Module,
                skipVisibility: true); // Supports creating the delegate immediately when calling CreateDelegate().
        }

        private static void EmitLdargForInstance(ILGenerator il, MethodBase method, bool callCtorAsMethod)
        {
            if (method is RuntimeConstructorInfo)
            {
                if (callCtorAsMethod)
                {
                    EmitLdArg1();
                }
            }
            else if (!method.IsStatic)
            {
                EmitLdArg1();
            }

            void EmitLdArg1()
            {
                il.Emit(OpCodes.Ldarg_1);
                if (method.DeclaringType!.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, method.DeclaringType);
                }
            }
        }

        private static void EmitCall(ILGenerator il, MethodBase method, bool callCtorAsMethod, bool backwardsCompat)
        {
            // For CallStack reasons, don't inline target method.
            // EmitCalli above and Mono interpreter do not need this.
            if (backwardsCompat && RuntimeFeature.IsDynamicCodeCompiled)
            {
#if MONO
                il.Emit(OpCodes.Call, Methods.DisableInline());
#else
                il.Emit(OpCodes.Call, Methods.NextCallReturnAddress());
                il.Emit(OpCodes.Pop);
#endif
            }

            if (method is RuntimeConstructorInfo rci)
            {
                if (callCtorAsMethod)
                {
                    il.Emit(OpCodes.Call, rci);
                    il.Emit(OpCodes.Ldnull);
                }
                else
                {
                    il.Emit(OpCodes.Newobj, rci);
                }
            }
            else if (method.IsStatic || method.DeclaringType!.IsValueType)
            {
                il.Emit(OpCodes.Call, (MethodInfo)method);
            }
            else
            {
                il.Emit(OpCodes.Callvirt, (MethodInfo)method);
            }
        }

        private static Type GetReturnType(MethodBase method, bool callCtorAsMethod)
        {
            if (method is RuntimeConstructorInfo rci)
            {
                // When calling a constructor as a method we return null.
                return callCtorAsMethod ? typeof(object) : rci.DeclaringType!;
            }

            if (method is DynamicMethod dm)
            {
                return dm.ReturnType;
            }

            return ((RuntimeMethodInfo)method).ReturnType;
        }

        private static void EmitReturnHandling(ILGenerator il, Type returnType)
        {
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
                s_Pointer_Box ??= typeof(Pointer).GetMethod(nameof(Pointer.Box), [typeof(void*), typeof(Type)])!;

            private static MethodInfo? s_Type_GetTypeFromHandle;
            public static MethodInfo Type_GetTypeFromHandle() =>
                s_Type_GetTypeFromHandle ??= typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle), [typeof(RuntimeTypeHandle)])!;

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
