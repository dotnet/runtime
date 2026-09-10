// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Reflection
{
    // Shared, precompilable thunks avoid emitting a method-specific stub for cold invocations.
    // This type is included in SystemDomain::IsReflectionInvocationMethod for caller stack walks.
    internal static unsafe class IntrinsicInvokeHelper
    {
        private const int SpecializationThreshold = 100;
        private const MethodBase.InvokerStrategy StrategyDetermined =
            MethodBase.InvokerStrategy.StrategyDetermined_Obj4Args |
            MethodBase.InvokerStrategy.StrategyDetermined_ObjSpanArgs |
            MethodBase.InvokerStrategy.StrategyDetermined_RefArgs;

        internal struct InvokeState
        {
            internal IntPtr Thunk;
            internal IntPtr FunctionPointer;
            internal int InvocationCount;
        }

        internal static object? Invoke(
            ref InvokeState state,
            ref MethodBase.InvokerStrategy strategy,
            ref InvokerEmitUtil.InvokeFunc_RefArgs? invokeFunc,
            MethodBase method,
            RuntimeType[] argumentTypes,
            object? obj,
            IntPtr* args,
            bool backwardsCompat)
        {
            if (!method.IsStatic && obj is not null && obj.GetType().IsValueType)
            {
                return InvokeEmitted(ref strategy, ref invokeFunc, method, obj, args, backwardsCompat);
            }

            var thunk = (delegate*<IntPtr, object?, IntPtr*, Type?, object?>)Volatile.Read(ref state.Thunk);
            if (thunk is null)
            {
                if (!TryGetShape(method, argumentTypes, out thunk, out IntPtr functionPointer))
                {
                    return InvokeEmitted(ref strategy, ref invokeFunc, method, obj, args, backwardsCompat);
                }

                state.FunctionPointer = functionPointer;
                strategy |= StrategyDetermined;
                Volatile.Write(ref state.Thunk, (IntPtr)thunk);
            }

            if (RuntimeFeature.IsDynamicCodeSupported &&
                !(LocalAppContextSwitches.ForceInterpretedInvoke && !LocalAppContextSwitches.ForceEmitInvoke) &&
                Interlocked.Increment(ref state.InvocationCount) >= SpecializationThreshold)
            {
                // Let the normal strategy selection specialize the next invocation's argument path.
                strategy &= ~StrategyDetermined;
            }

            IntPtr target = state.FunctionPointer;
            if (target == IntPtr.Zero)
            {
                // The same MethodInfo can be invoked on different implementations.
                target = RuntimeMethodHandle.GetVirtualFunctionPointer((RuntimeMethodInfo)method, obj!);
            }

            object? result = thunk(target, obj, args, method.DeclaringType);
            GC.KeepAlive(method);
            return result;
        }

        private static object? InvokeEmitted(
            ref MethodBase.InvokerStrategy strategy,
            ref InvokerEmitUtil.InvokeFunc_RefArgs? invokeFunc,
            MethodBase method, object? obj, IntPtr* args, bool backwardsCompat)
        {
            InvokerEmitUtil.InvokeFunc_RefArgs emitDelegate;
            using (AssemblyBuilder.ForceAllowDynamicCode())
            {
                emitDelegate = InvokerEmitUtil.CreateInvokeDelegate_RefArgs(method, backwardsCompat);
            }

            Volatile.Write(ref invokeFunc, emitDelegate);
            strategy |= MethodBase.InvokerStrategy.StrategyDetermined_RefArgs;
            return emitDelegate(obj, args);
        }

        private static bool TryGetShape(
            MethodBase method,
            ReadOnlySpan<RuntimeType> argumentTypes,
            out delegate*<IntPtr, object?, IntPtr*, Type?, object?> thunk,
            out IntPtr functionPointer)
        {
            thunk = null;
            functionPointer = IntPtr.Zero;

            if (method is System.Reflection.Emit.DynamicMethod ||
                method.ContainsGenericParameters ||
                (method.CallingConvention & CallingConventions.VarArgs) != 0)
            {
                return false;
            }

            int argCount = argumentTypes.Length;

            bool referenceArguments = true;
            for (int i = 0; i < argCount; i++)
            {
                if (!IsReferenceType(argumentTypes[i]))
                {
                    referenceArguments = false;
                    break;
                }
            }

            if (method is ConstructorInfo)
            {
                if (method.IsStatic ||
                    method.DeclaringType is not Type declaringType ||
                    !IsReferenceType(declaringType) ||
                    declaringType.IsAbstract ||
                    declaringType.IsArray ||
                    declaringType.ContainsGenericParameters ||
                    declaringType == typeof(string))
                {
                    return false;
                }

                thunk = referenceArguments ? argCount switch
                {
                    0 => &Ctor_0,
                    1 => &Ctor_1,
                    2 => &Ctor_2,
                    3 => &Ctor_3,
                    4 => &Ctor_4,
                    5 => &Ctor_5,
                    6 => &Ctor_6,
                    7 => &Ctor_7,
                    8 => &Ctor_8,
                    _ => null,
                } : ClassifyConstructor(argumentTypes);
            }
            else if (method is MethodInfo methodInfo)
            {
                Type returnType = methodInfo.ReturnType;
                if (method.IsStatic)
                {
                    if (referenceArguments)
                    {
                        thunk = ClassifyStaticReferenceArguments(argCount, returnType);
                    }
                    else if (argCount == 1 && GetInputType(argumentTypes[0]) == typeof(int) && IsReferenceType(returnType))
                    {
                        thunk = &Static_Object_Int;
                    }
                    else if (argCount == 2 && IsReferenceType(argumentTypes[0]) &&
                        argumentTypes[1].IsByRef && IsReferenceType(argumentTypes[1].GetElementType()!) &&
                        returnType == typeof(bool))
                    {
                        // Thunk for the .NET TryParse pattern with reference-type results.
                        thunk = &Static_Bool_ObjByRefObj;
                    }
                }
                else if (method.DeclaringType is Type declaringType && IsReferenceType(declaringType))
                {
                    if (referenceArguments)
                    {
                        thunk = ClassifyInstanceReferenceArguments(argCount, returnType);
                    }
                    else if (returnType == typeof(void))
                    {
                        if (argCount == 1)
                        {
                            thunk = ClassifyInstancePrimitive(GetInputType(argumentTypes[0]));
                        }
                        else if (argCount == 4 &&
                            argumentTypes[0] == typeof(float) && argumentTypes[1] == typeof(float) &&
                            argumentTypes[2] == typeof(float) && GetInputType(argumentTypes[3]) == typeof(int))
                        {
                            thunk = &Instance_Void_FloatFloatFloatInt;
                        }
                    }
                }
            }

            if (thunk is null)
            {
                return false;
            }

            if (method.IsStatic || !method.IsVirtual || method.IsFinal)
            {
                functionPointer = method.MethodHandle.GetFunctionPointer();
            }

            return true;
        }

        private static bool IsReferenceType(Type type) =>
            !type.IsValueType && !type.IsByRef && !type.IsPointer && !type.IsFunctionPointer;

        private static Type GetInputType(RuntimeType type) =>
            type.IsActualEnum ? type.GetEnumUnderlyingType() : type;

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyConstructor(ReadOnlySpan<RuntimeType> arguments)
        {
            if (arguments.Length == 1)
            {
                Type type = GetInputType(arguments[0]);
                if (type == typeof(bool)) return &Ctor_Bool;
                if (type == typeof(int)) return &Ctor_Int;
                if (type == typeof(long)) return &Ctor_Long;
            }
            else if (arguments.Length == 2)
            {
                Type first = GetInputType(arguments[0]);
                Type second = GetInputType(arguments[1]);
                if (first == typeof(int) && second == typeof(int)) return &Ctor_IntInt;
                if (first == typeof(long) && second == typeof(long)) return &Ctor_LongLong;
                if (IsReferenceType(first) && second == typeof(int)) return &Ctor_ObjInt;
            }
            else if (arguments.Length == 4 && IsReferenceType(arguments[0]) && IsReferenceType(arguments[3]))
            {
                if (GetInputType(arguments[1]) == typeof(int) && IsReferenceType(arguments[2])) return &Ctor_ObjIntObjObj;
                if (IsReferenceType(arguments[1]) && GetInputType(arguments[2]) == typeof(bool)) return &Ctor_ObjObjBoolObj;
            }
            else if (arguments.Length == 5 &&
                IsReferenceType(arguments[0]) && IsReferenceType(arguments[1]) && IsReferenceType(arguments[2]) &&
                GetInputType(arguments[3]) == typeof(bool) && IsReferenceType(arguments[4]))
            {
                return &Ctor_ObjObjObjBoolObj;
            }

            return null;
        }

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyStaticReferenceArguments(int count, Type returnType)
        {
            if (count == 0)
            {
                return ClassifyStatic0Return(returnType);
            }

            if (returnType == typeof(void))
            {
                return count switch
                {
                    1 => &Static_Void_1Obj,
                    2 => &Static_Void_2Obj,
                    3 => &Static_Void_3Obj,
                    4 => &Static_Void_4Obj,
                    _ => null,
                };
            }

            return IsReferenceType(returnType) ? count switch
            {
                1 => &Static_Object_1Obj,
                2 => &Static_Object_2Obj,
                3 => &Static_Object_3Obj,
                4 => &Static_Object_4Obj,
                _ => null,
            } : null;
        }

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyInstanceReferenceArguments(int count, Type returnType)
        {
            if (returnType == typeof(void))
            {
                return count switch
                {
                    0 => &Instance_Void_0,
                    1 => &Instance_Void_1Obj,
                    2 => &Instance_Void_2Obj,
                    3 => &Instance_Void_3Obj,
                    4 => &Instance_Void_4Obj,
                    _ => null,
                };
            }

            if (IsReferenceType(returnType))
            {
                return count switch
                {
                    0 => &Instance_Object_0,
                    1 => &Instance_Object_1Obj,
                    2 => &Instance_Object_2Obj,
                    3 => &Instance_Object_3Obj,
                    4 => &Instance_Object_4Obj,
                    _ => null,
                };
            }

            if (count == 2 && returnType == typeof(int))
            {
                return &Instance_Int_2Obj;
            }

            if (count == 0)
            {
                if (returnType == typeof(bool)) return &Instance_Bool_0;
                if (returnType == typeof(byte)) return &Instance_Byte_0;
                if (returnType == typeof(sbyte)) return &Instance_SByte_0;
                if (returnType == typeof(char)) return &Instance_Char_0;
                if (returnType == typeof(short)) return &Instance_Short_0;
                if (returnType == typeof(ushort)) return &Instance_UShort_0;
                if (returnType == typeof(int)) return &Instance_Int_0;
                if (returnType == typeof(uint)) return &Instance_UInt_0;
                if (returnType == typeof(long)) return &Instance_Long_0;
                if (returnType == typeof(ulong)) return &Instance_ULong_0;
                if (returnType == typeof(float)) return &Instance_Float_0;
                if (returnType == typeof(double)) return &Instance_Double_0;
                if (returnType == typeof(nint)) return &Instance_NInt_0;
                if (returnType == typeof(nuint)) return &Instance_NUInt_0;
            }

            return null;
        }

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyInstancePrimitive(Type type)
        {
            if (type == typeof(bool)) return &Instance_Void_Bool;
            if (type == typeof(byte)) return &Instance_Void_Byte;
            if (type == typeof(sbyte)) return &Instance_Void_SByte;
            if (type == typeof(char)) return &Instance_Void_Char;
            if (type == typeof(short)) return &Instance_Void_Short;
            if (type == typeof(ushort)) return &Instance_Void_UShort;
            if (type == typeof(int)) return &Instance_Void_Int;
            if (type == typeof(uint)) return &Instance_Void_UInt;
            if (type == typeof(long)) return &Instance_Void_Long;
            if (type == typeof(ulong)) return &Instance_Void_ULong;
            if (type == typeof(float)) return &Instance_Void_Float;
            if (type == typeof(double)) return &Instance_Void_Double;
            if (type == typeof(nint)) return &Instance_Void_NInt;
            if (type == typeof(nuint)) return &Instance_Void_NUInt;
            return null;
        }

        // Classifiers return a fn pointer (not invoking it) so the JIT doesn't pull thunks into
        // the classifier's compiled body.

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyStatic0Return(Type returnType)
        {
            if (returnType == typeof(void)) return &Static_Void_0;
            if (returnType == typeof(bool)) return &Static_Bool_0;
            if (returnType == typeof(byte)) return &Static_Byte_0;
            if (returnType == typeof(sbyte)) return &Static_SByte_0;
            if (returnType == typeof(char)) return &Static_Char_0;
            if (returnType == typeof(short)) return &Static_Short_0;
            if (returnType == typeof(ushort)) return &Static_UShort_0;
            if (returnType == typeof(int)) return &Static_Int_0;
            if (returnType == typeof(uint)) return &Static_UInt_0;
            if (returnType == typeof(long)) return &Static_Long_0;
            if (returnType == typeof(ulong)) return &Static_ULong_0;
            if (returnType == typeof(float)) return &Static_Float_0;
            if (returnType == typeof(double)) return &Static_Double_0;
            if (returnType == typeof(nint) || returnType.IsFunctionPointer) return &Static_NInt_0;
            if (returnType == typeof(nuint)) return &Static_NUInt_0;

            if (!returnType.IsValueType && !returnType.IsByRef && !returnType.IsPointer && !returnType.IsFunctionPointer)
                return &Static_Object_0;

            return null;
        }

        // Per-shape thunks. JIT compiles only the ones used.

        private static object? Static_Void_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
        {
            ((delegate*<void>)fn)();
            return null;
        }

        private static object? Static_Bool_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<bool>)fn)();
        private static object? Static_Byte_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<byte>)fn)();
        private static object? Static_SByte_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<sbyte>)fn)();
        private static object? Static_Char_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<char>)fn)();
        private static object? Static_Short_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<short>)fn)();
        private static object? Static_UShort_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<ushort>)fn)();
        private static object? Static_Int_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<int>)fn)();
        private static object? Static_UInt_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<uint>)fn)();
        private static object? Static_Long_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<long>)fn)();
        private static object? Static_ULong_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<ulong>)fn)();
        private static object? Static_Float_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<float>)fn)();
        private static object? Static_Double_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<double>)fn)();
        private static object? Static_NInt_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<nint>)fn)();
        private static object? Static_NUInt_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<nuint>)fn)();
        private static object? Static_Object_0(IntPtr fn, object? _, IntPtr* __, Type? ___)
            => ((delegate*<object?>)fn)();

        private static object? Static_Void_1Obj(IntPtr fn, object? _, IntPtr* args, Type? __)
        {
            ((delegate*<object?, void>)fn)(Unsafe.Read<object?>((void*)args[0]));
            return null;
        }

        private static object? Static_Object_1Obj(IntPtr fn, object? _, IntPtr* args, Type? __)
            => ((delegate*<object?, object?>)fn)(Unsafe.Read<object?>((void*)args[0]));

        private static object? Static_Void_2Obj(IntPtr fn, object? _, IntPtr* args, Type? __)
        {
            ((delegate*<object?, object?, void>)fn)(
                Unsafe.Read<object?>((void*)args[0]),
                Unsafe.Read<object?>((void*)args[1]));
            return null;
        }

        private static object? ReadReference(IntPtr* args, int index) =>
            Unsafe.Read<object?>((void*)args[index]);

        private static T ReadPrimitive<T>(IntPtr* args, int index) where T : unmanaged =>
            Unsafe.ReadUnaligned<T>((void*)args[index]);

        private static object? Static_Object_2Obj(IntPtr fn, object? _, IntPtr* args, Type? __) =>
            ((delegate*<object?, object?, object?>)fn)(ReadReference(args, 0), ReadReference(args, 1));

        private static object? Static_Object_3Obj(IntPtr fn, object? _, IntPtr* args, Type? __) =>
            ((delegate*<object?, object?, object?, object?>)fn)(ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2));

        private static object? Static_Object_4Obj(IntPtr fn, object? _, IntPtr* args, Type? __) =>
            ((delegate*<object?, object?, object?, object?, object?>)fn)(ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3));

        private static object? Static_Void_3Obj(IntPtr fn, object? _, IntPtr* args, Type? __)
        {
            ((delegate*<object?, object?, object?, void>)fn)(ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2));
            return null;
        }

        private static object? Static_Void_4Obj(IntPtr fn, object? _, IntPtr* args, Type? __)
        {
            ((delegate*<object?, object?, object?, object?, void>)fn)(ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3));
            return null;
        }

        private static object? Static_Object_Int(IntPtr fn, object? _, IntPtr* args, Type? __) =>
            ((delegate*<int, object?>)fn)(ReadPrimitive<int>(args, 0));

        private static object? Static_Bool_ObjByRefObj(IntPtr fn, object? _, IntPtr* args, Type? __) =>
            ((delegate*<object?, ref object?, bool>)fn)(ReadReference(args, 0), ref Unsafe.AsRef<object?>((void*)args[1]));

        private static object? Instance_Object_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, object?>)fn, obj!);

#pragma warning disable CA1859 // These thunks must match the shared object-returning function-pointer signature.
        private static object? Instance_Bool_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, bool>)fn, obj!);

        private static object? Instance_Byte_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, byte>)fn, obj!);

        private static object? Instance_SByte_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, sbyte>)fn, obj!);

        private static object? Instance_Char_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, char>)fn, obj!);

        private static object? Instance_Short_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, short>)fn, obj!);

        private static object? Instance_UShort_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, ushort>)fn, obj!);

        private static object? Instance_Int_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, int>)fn, obj!);

        private static object? Instance_UInt_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, uint>)fn, obj!);

        private static object? Instance_Long_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, long>)fn, obj!);

        private static object? Instance_ULong_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, ulong>)fn, obj!);

        private static object? Instance_Float_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, float>)fn, obj!);

        private static object? Instance_Double_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, double>)fn, obj!);

        private static object? Instance_NInt_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, nint>)fn, obj!);

        private static object? Instance_NUInt_0(IntPtr fn, object? obj, IntPtr* _, Type? __) =>
            InstanceCalliHelper.Call((delegate*<object, nuint>)fn, obj!);

        private static object? Instance_Int_2Obj(IntPtr fn, object? obj, IntPtr* args, Type? _) =>
            InstanceCalliHelper.Call((delegate*<object, object?, object?, int>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1));
#pragma warning restore CA1859

        private static object? Instance_Object_1Obj(IntPtr fn, object? obj, IntPtr* args, Type? _) =>
            InstanceCalliHelper.Call((delegate*<object, object?, object?>)fn, obj!, ReadReference(args, 0));

        private static object? Instance_Object_2Obj(IntPtr fn, object? obj, IntPtr* args, Type? _) =>
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1));

        private static object? Instance_Object_3Obj(IntPtr fn, object? obj, IntPtr* args, Type? _) =>
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2));

        private static object? Instance_Object_4Obj(IntPtr fn, object? obj, IntPtr* args, Type? _) =>
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3));

        private static object? Instance_Void_0(IntPtr fn, object? obj, IntPtr* _, Type? __)
        {
            InstanceCalliHelper.Call((delegate*<object, void>)fn, obj!);
            return null;
        }

        private static object? Instance_Void_1Obj(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, object?, void>)fn, obj!, ReadReference(args, 0));
            return null;
        }

        private static object? Instance_Void_2Obj(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, object?, object?, void>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1));
            return null;
        }

        private static object? Instance_Void_3Obj(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, void>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2));
            return null;
        }

        private static object? Instance_Void_4Obj(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, void>)fn, obj!, ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3));
            return null;
        }

        private static object? Instance_Void_Bool(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, bool, void>)fn, obj!, ReadPrimitive<bool>(args, 0));
            return null;
        }

        private static object? Instance_Void_Byte(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, byte, void>)fn, obj!, ReadPrimitive<byte>(args, 0));
            return null;
        }

        private static object? Instance_Void_SByte(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, sbyte, void>)fn, obj!, ReadPrimitive<sbyte>(args, 0));
            return null;
        }

        private static object? Instance_Void_Char(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, char, void>)fn, obj!, ReadPrimitive<char>(args, 0));
            return null;
        }

        private static object? Instance_Void_Short(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, short, void>)fn, obj!, ReadPrimitive<short>(args, 0));
            return null;
        }

        private static object? Instance_Void_UShort(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, ushort, void>)fn, obj!, ReadPrimitive<ushort>(args, 0));
            return null;
        }

        private static object? Instance_Void_Int(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, int, void>)fn, obj!, ReadPrimitive<int>(args, 0));
            return null;
        }

        private static object? Instance_Void_UInt(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, uint, void>)fn, obj!, ReadPrimitive<uint>(args, 0));
            return null;
        }

        private static object? Instance_Void_Long(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, long, void>)fn, obj!, ReadPrimitive<long>(args, 0));
            return null;
        }

        private static object? Instance_Void_ULong(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, ulong, void>)fn, obj!, ReadPrimitive<ulong>(args, 0));
            return null;
        }

        private static object? Instance_Void_Float(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, float, void>)fn, obj!, ReadPrimitive<float>(args, 0));
            return null;
        }

        private static object? Instance_Void_Double(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, double, void>)fn, obj!, ReadPrimitive<double>(args, 0));
            return null;
        }

        private static object? Instance_Void_NInt(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, nint, void>)fn, obj!, ReadPrimitive<nint>(args, 0));
            return null;
        }

        private static object? Instance_Void_NUInt(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, nuint, void>)fn, obj!, ReadPrimitive<nuint>(args, 0));
            return null;
        }

        private static object? Instance_Void_FloatFloatFloatInt(IntPtr fn, object? obj, IntPtr* args, Type? _)
        {
            InstanceCalliHelper.Call((delegate*<object, float, float, float, int, void>)fn, obj!,
                ReadPrimitive<float>(args, 0), ReadPrimitive<float>(args, 1), ReadPrimitive<float>(args, 2), ReadPrimitive<int>(args, 3));
            return null;
        }

        // Ctor thunks: `obj` non-null = call ctor on existing instance, null = allocate first.
        // `string` excluded: `newobj String(...)` is JIT-lowered to a hidden static allocator
        // (`METHOD__STRING__CTORF_*` in src/coreclr/vm/corelib.h, wired by
        // `ECall::PopulateManagedStringConstructors`); the public ctor has no callable instance
        // entry, and `GetUninitializedObject(typeof(string))` is runtime-rejected.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "Caller anchors the ctor MethodBase, keeping its type reachable.")]
        private static object GetConstructorInstance(object? obj, Type? declaringType) =>
            obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);

        private static object? Ctor_0(IntPtr fn, object? obj, IntPtr* _, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, void>)fn, instance);
            return obj is null ? instance : null;
        }

        private static object? Ctor_1(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]));
            return obj is null ? instance : null;
        }

        private static object? Ctor_2(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]),
                Unsafe.Read<object?>((void*)args[1]));
            return obj is null ? instance : null;
        }

        private static object? Ctor_3(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, object?, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]),
                Unsafe.Read<object?>((void*)args[1]),
                Unsafe.Read<object?>((void*)args[2]));
            return obj is null ? instance : null;
        }

        private static object? Ctor_4(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            Instance_Void_4Obj(fn, instance, args, declaringType);
            return obj is null ? instance : null;
        }

        private static object? Ctor_5(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3), ReadReference(args, 4));
            return obj is null ? instance : null;
        }

        private static object? Ctor_6(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3), ReadReference(args, 4), ReadReference(args, 5));
            return obj is null ? instance : null;
        }

        private static object? Ctor_7(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, object?, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3), ReadReference(args, 4), ReadReference(args, 5), ReadReference(args, 6));
            return obj is null ? instance : null;
        }

        private static object? Ctor_8(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, object?, object?, object?, object?, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadReference(args, 3), ReadReference(args, 4), ReadReference(args, 5), ReadReference(args, 6), ReadReference(args, 7));
            return obj is null ? instance : null;
        }

        private static object? Ctor_Bool(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            Instance_Void_Bool(fn, instance, args, declaringType);
            return obj is null ? instance : null;
        }

        private static object? Ctor_Int(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            Instance_Void_Int(fn, instance, args, declaringType);
            return obj is null ? instance : null;
        }

        private static object? Ctor_Long(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            Instance_Void_Long(fn, instance, args, declaringType);
            return obj is null ? instance : null;
        }

        private static object? Ctor_IntInt(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, int, int, void>)fn, instance, ReadPrimitive<int>(args, 0), ReadPrimitive<int>(args, 1));
            return obj is null ? instance : null;
        }

        private static object? Ctor_LongLong(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, long, long, void>)fn, instance, ReadPrimitive<long>(args, 0), ReadPrimitive<long>(args, 1));
            return obj is null ? instance : null;
        }

        private static object? Ctor_ObjInt(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, int, void>)fn, instance, ReadReference(args, 0), ReadPrimitive<int>(args, 1));
            return obj is null ? instance : null;
        }

        private static object? Ctor_ObjIntObjObj(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, int, object?, object?, void>)fn, instance,
                ReadReference(args, 0), ReadPrimitive<int>(args, 1), ReadReference(args, 2), ReadReference(args, 3));
            return obj is null ? instance : null;
        }

        private static object? Ctor_ObjObjBoolObj(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, bool, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadPrimitive<bool>(args, 2), ReadReference(args, 3));
            return obj is null ? instance : null;
        }

        private static object? Ctor_ObjObjObjBoolObj(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = GetConstructorInstance(obj, declaringType);
            InstanceCalliHelper.Call((delegate*<object, object?, object?, object?, bool, object?, void>)fn, instance,
                ReadReference(args, 0), ReadReference(args, 1), ReadReference(args, 2), ReadPrimitive<bool>(args, 3), ReadReference(args, 4));
            return obj is null ? instance : null;
        }
    }
}
