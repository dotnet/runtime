// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    internal static class InvokerEmitUtil
    {
        internal unsafe delegate object? InvokeFunc_RefArgs(object? obj, IntPtr functionPointer, IntPtr* refArguments);
        internal delegate object? InvokeFunc_ObjSpanArgs(object? obj, IntPtr functionPointer, Span<object?> arguments);
        internal delegate object? InvokeFunc_Obj4Args(object? obj, IntPtr functionPointer, object? arg1, object? arg2, object? arg3, object? arg4);

        public static unsafe InvokeFunc_Obj4Args CreateInvokeDelegate_Obj4Args(Type declaringType, bool isStatic, Type[] calliParameterTypes, Type returnType)
        {
            Type[] delegateParameters = [typeof(object), typeof(IntPtr), typeof(object), typeof(object), typeof(object), typeof(object)];

            DynamicMethod dm = new (
                GetInvokeStubName(calliParameterTypes, returnType),
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, isStatic, declaringType);

            // Push the arguments.
            for (int i = 0; i < calliParameterTypes.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)calliParameterTypes[i];

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

                if (parameterType.IsPointer || parameterType.IsFunctionPointer)
                {
                    Unbox(il, typeof(IntPtr));
                }
                else if (parameterType.IsValueType)
                {
                    Unbox(il, parameterType);
                }
            }

            EmitCall(il, isStatic, calliParameterTypes, returnType);
            EmitReturnHandling(il, returnType);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_Obj4Args)dm.CreateDelegate(typeof(InvokeFunc_Obj4Args), target: null);
        }

        public static unsafe InvokeFunc_ObjSpanArgs CreateInvokeDelegate_ObjSpanArgs(Type declaringType, bool isStatic, Type[] calliParameterTypes, Type returnType)
        {
            Type[] delegateParameters = [typeof(object), typeof(IntPtr), typeof(Span<object>)];

            DynamicMethod dm = new (
                GetInvokeStubName(calliParameterTypes, returnType),
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, isStatic, declaringType);

            // Push the arguments.
            for (int i = 0; i < calliParameterTypes.Length; i++)
            {
                RuntimeType parameterType = (RuntimeType)calliParameterTypes[i];

                il.Emit(OpCodes.Ldarga_S, 2);
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

            EmitCall(il, isStatic, calliParameterTypes, returnType);
            EmitReturnHandling(il, returnType);

            // Create the delegate; it is also compiled at this point due to restrictedSkipVisibility=true.
            return (InvokeFunc_ObjSpanArgs)dm.CreateDelegate(typeof(InvokeFunc_ObjSpanArgs), target: null);
        }

        public static unsafe InvokeFunc_RefArgs CreateInvokeDelegate_RefArgs(Type declaringType, bool isStatic, Type[] calliParameterTypes, Type returnType)
        {
            Type[] delegateParameters = [typeof(object), typeof(IntPtr), typeof(IntPtr*)];

            DynamicMethod dm = new (
                GetInvokeStubName(calliParameterTypes, returnType),
                returnType: typeof(object),
                delegateParameters,
                typeof(object).Module, // Use system module to identify our DynamicMethods.
                skipVisibility: true);

            ILGenerator il = dm.GetILGenerator();

            EmitLdargForInstance(il, isStatic, declaringType);

            // Push the arguments.
            for (int i = 0; i < calliParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg_2);
                if (i != 0)
                {
                    il.Emit(OpCodes.Ldc_I4, i * IntPtr.Size);
                    il.Emit(OpCodes.Add);
                }

                il.Emit(OpCodes.Ldfld, Methods.ByReferenceOfByte_Value());

                RuntimeType parameterType = (RuntimeType)calliParameterTypes[i];
                if (!parameterType.IsByRef)
                {
                    il.Emit(OpCodes.Ldobj, parameterType.IsPointer || parameterType.IsFunctionPointer ? typeof(IntPtr) : parameterType);
                }
            }

            EmitCall(il, isStatic, calliParameterTypes, returnType);
            EmitReturnHandling(il, returnType);

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

        private static void EmitCall(ILGenerator il, bool isStatic, Type[]? calliParameterTypes, Type returnType)
        {
            il.Emit(OpCodes.Ldarg_1);

            CallingConventions callingConventions = CallingConventions.Standard;
            if (!isStatic)
            {
                callingConventions |= CallingConventions.HasThis;
            }

            il.EmitCalli(OpCodes.Calli, callingConventions, returnType, calliParameterTypes, optionalParameterTypes: null);
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

        private static void EmitLdargForInstance(ILGenerator il, bool isStatic, Type declaringType)
        {
            if (!isStatic)
            {
                il.Emit(OpCodes.Ldarg_0);
                if (declaringType.IsValueType)
                {
                    il.Emit(OpCodes.Unbox, declaringType);
                }
            }
        }

        /// <summary>
        /// Return the name of the dynamic method that will be created using the function pointer syntax of
        /// of listing the parameter types and then the return type.
        /// </summary>
        private static string GetInvokeStubName(ReadOnlySpan<Type> parameterTypes, Type returnType)
        {
            // If changed, update native stack walking code that also uses "InvokeStub_" to ignore reflection frames.
            const string InvokeStubPrefix = "InvokeStub_<";
            const int MaxChars = 255;

            Span<char> value = stackalloc char[MaxChars];
            InvokeStubPrefix.AsSpan().CopyTo(value);
            int charsWritten = InvokeStubPrefix.Length;
            int parameterCount = parameterTypes.Length;
            string typeName;

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
            typeName = returnType.Name;
            if (charsWritten + typeName.Length + 2 >= MaxChars)
            {
                return GetDefaultWhenLengthTooLong();
            }

            typeName.AsSpan().CopyTo(value.Slice(charsWritten, typeName.Length));
            charsWritten += typeName.Length;

            // Closing '>'.
            value[charsWritten++] = '>';
            value[charsWritten++] = ' ';

            // Success. The dynamic method's Name property will append the delegate's parameter types
            // to the end of the name.
            return new string(value.Slice(0, charsWritten));

            string GetDefaultWhenLengthTooLong()
            {
                return $"{InvokeStubPrefix}({parameterCount}) ";
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

            private static MethodInfo? s_NextCallReturnAddress;
            public static MethodInfo NextCallReturnAddress() =>
                s_NextCallReturnAddress ??= typeof(StubHelpers.StubHelpers).GetMethod(nameof(StubHelpers.StubHelpers.NextCallReturnAddress), BindingFlags.NonPublic | BindingFlags.Static)!;
        }
    }
}
