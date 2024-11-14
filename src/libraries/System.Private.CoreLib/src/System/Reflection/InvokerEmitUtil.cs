// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        // If changed, update native stack walking code that also uses "InvokeStub_" to ignore reflection frames.
        private const string InvokeStubPrefix = "InvokeStub_";

        internal delegate object? InvokeFunc_Obj0Args(object? obj, IntPtr functionPointer);
        internal delegate object? InvokeFunc_Obj1Arg(object? obj, IntPtr functionPointer, object? arg1);
        internal delegate object? InvokeFunc_Obj4Args(object? obj, IntPtr functionPointer, object? arg1, object? arg2, object? arg3, object? arg4);
        internal delegate object? InvokeFunc_ObjSpanArgs(object? obj, IntPtr functionPointer, Span<object?> arguments);
        internal unsafe delegate object? InvokeFunc_RefArgs(object? obj, IntPtr functionPointer, IntPtr* refArguments);

        public static unsafe InvokeFunc_Obj0Args CreateInvokeDelegate_Obj0Args(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, signatureInfo, [typeof(object), typeof(IntPtr)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, signatureInfo.IsStatic, signatureInfo.DeclaringType);
            EmitCall(il, method, signatureInfo, signatureInfo.IsStatic, backwardsCompat);
            EmitReturnHandling(il, method is RuntimeConstructorInfo ? method.DeclaringType! : signatureInfo.ReturnType);
            return (InvokeFunc_Obj0Args)dm.CreateDelegate(typeof(InvokeFunc_Obj0Args), target: null);
        }

        public static unsafe InvokeFunc_Obj1Arg CreateInvokeDelegate_Obj1Arg(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, signatureInfo, [typeof(object), typeof(IntPtr), typeof(object)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, signatureInfo.IsStatic, signatureInfo.DeclaringType);

            Debug.Assert(signatureInfo.ParameterTypes.Length == 1);
            il.Emit(OpCodes.Ldarg_2);
            UnboxSpecialType(il, (RuntimeType)signatureInfo.ParameterTypes[0]);

            EmitCall(il, method, signatureInfo, signatureInfo.IsStatic, backwardsCompat);
            EmitReturnHandling(il, method is RuntimeConstructorInfo ? method.DeclaringType! : signatureInfo.ReturnType);
            return (InvokeFunc_Obj1Arg)dm.CreateDelegate(typeof(InvokeFunc_Obj1Arg), target: null);
        }

        public static unsafe InvokeFunc_Obj4Args CreateInvokeDelegate_Obj4Args(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, signatureInfo, [typeof(object), typeof(IntPtr), typeof(object), typeof(object), typeof(object), typeof(object)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, signatureInfo.IsStatic, signatureInfo.DeclaringType);

            ReadOnlySpan<Type> parameterTypes = signatureInfo.ParameterTypes;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameterTypes[i];

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

            EmitCall(il, method, signatureInfo, signatureInfo.IsStatic, backwardsCompat);
            EmitReturnHandling(il, method is RuntimeConstructorInfo ? method.DeclaringType! : signatureInfo.ReturnType);
            return (InvokeFunc_Obj4Args)dm.CreateDelegate(typeof(InvokeFunc_Obj4Args), target: null);
        }

        public static unsafe InvokeFunc_ObjSpanArgs CreateInvokeDelegate_ObjSpanArgs(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, signatureInfo, [typeof(object), typeof(IntPtr), typeof(Span<object>)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, signatureInfo.IsStatic, signatureInfo.DeclaringType);

            ReadOnlySpan<Type> parameterTypes = signatureInfo.ParameterTypes;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)parameterTypes[i];

                il.Emit(OpCodes.Ldarga_S, 2);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Call, Methods.Span_get_Item());
                il.Emit(OpCodes.Ldind_Ref);

                UnboxSpecialType(il, parameterType);
            }

            EmitCall(il, method, signatureInfo, signatureInfo.IsStatic, backwardsCompat);
            EmitReturnHandling(il, method is RuntimeConstructorInfo ? method.DeclaringType! : signatureInfo.ReturnType);
            return (InvokeFunc_ObjSpanArgs)dm.CreateDelegate(typeof(InvokeFunc_ObjSpanArgs), target: null);
        }

        public static unsafe InvokeFunc_RefArgs CreateInvokeDelegate_RefArgs(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool backwardsCompat)
        {
            DynamicMethod dm = CreateDynamicMethod(method, signatureInfo, [typeof(object), typeof(IntPtr), typeof(IntPtr*)]);
            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, method, signatureInfo.IsStatic, signatureInfo.DeclaringType);

            ReadOnlySpan<Type> parameterTypes = signatureInfo.ParameterTypes;
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)parameterTypes[i];
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer || parameterType.IsFunctionPointer ? typeof(IntPtr) : parameterType);
                }
            }

            EmitCall(il, method, signatureInfo, signatureInfo.IsStatic, backwardsCompat);
            EmitReturnHandling(il, method is RuntimeConstructorInfo ? method.DeclaringType! : signatureInfo.ReturnType);
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

        private static DynamicMethod CreateDynamicMethod(MethodBase? method, in InvokeSignatureInfoKey signatureInfo, Type[] delegateParameters)
        {
            return new DynamicMethod(
                GetInvokeStubName(method, signatureInfo),
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true); // Supports creating the delegate immediately when calling CreateDelegate().
        }

        private static void EmitLdargForInstance(ILGenerator il, MethodBase? method, bool isStatic, Type declaringType)
        {
            if (method is not RuntimeConstructorInfo && !isStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (declaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, declaringType);
                }
            }
        }

        private static void EmitCall(ILGenerator il, MethodBase? method, in InvokeSignatureInfoKey signatureInfo, bool isStatic, bool backwardsCompat)
        {
            if (method is null)
            {
                // Use calli

                CallingConventions callingConventions = CallingConventions.Standard;
                if (!isStatic)
                {
                    callingConventions |= CallingConventions.HasThis;
                }

                il.Emit(OpCodes.Ldarg_1);
                il.EmitCalli(OpCodes.Calli, callingConventions, signatureInfo.ReturnType, signatureInfo.ParameterTypes, optionalParameterTypes: null);
                return;
            }

            // Use Call\CallVirt\NewObj

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
                il.Emit(OpCodes.Newobj, rci);
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

        /// <summary>
        /// Return the name of the dynamic method that will be created using the function pointer syntax
        /// of listing the parameter types and then the return type.
        /// </summary>
        private static string GetInvokeStubName(MethodBase? method, in InvokeSignatureInfoKey signatureInfo)
        {
            if (method is not null)
            {
                string declaringTypeName = method.DeclaringType != null ? method.DeclaringType.Name + "." : string.Empty;
                return InvokeStubPrefix + declaringTypeName + method.Name;
            }

            return GetInvokeStubName(signatureInfo);

            static string GetInvokeStubName(in InvokeSignatureInfoKey signatureInfo)
            {
                const int MaxChars = 256;
                Span<char> value = stackalloc char[MaxChars];

                InvokeStubPrefix.AsSpan().CopyTo(value);
                int charsWritten = InvokeStubPrefix.Length;
                ReadOnlySpan<Type> parameterTypes = signatureInfo.ParameterTypes;
                int parameterCount = parameterTypes.Length;
                string typeName;

                value[charsWritten++] = '<';

                // Parameters.
                for (int i = 0; i < parameterCount; i++)
                {
                    typeName = parameterTypes[i].Name;
                    if (charsWritten + typeName.Length + 2 >= MaxChars)
                    {
                        return GetDefaultWhenLengthTooLong();
                    }

                    typeName.AsSpan().CopyTo(value.Slice(charsWritten, typeName.Length));
                    charsWritten += typeName.Length;

                    value[charsWritten++] = ',';
                    value[charsWritten++] = ' ';
                }

                // Return type.
                typeName = signatureInfo.ReturnType.Name;
                if (charsWritten + typeName.Length + 2 >= MaxChars)
                {
                    return GetDefaultWhenLengthTooLong();
                }

                typeName.AsSpan().CopyTo(value.Slice(charsWritten, typeName.Length));
                charsWritten += typeName.Length;

                // Closing '>'.
                value[charsWritten++] = '>';
                value[charsWritten++] = ' ';

                // Success. Later on the dynamic method's Name property will append the delegate's parameter type names.
                return new string(value.Slice(0, charsWritten));

                string GetDefaultWhenLengthTooLong()
                {
                    return $"{InvokeStubPrefix}({parameterCount}) ";
                }
            }
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
