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
            var thunk = (delegate*<IntPtr, object?, IntPtr*, Type?, object?>)Volatile.Read(ref state.Thunk);
            if (thunk is null)
            {
                if (!TryGetShape(method, argumentTypes, out thunk, out IntPtr functionPointer))
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

            object? result = thunk(state.FunctionPointer, obj, args, method.DeclaringType);
            GC.KeepAlive(method);
            return result;
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

            // Only reference-typed args qualify for the object-passing fast path.
            for (int i = 0; i < argCount; i++)
            {
                RuntimeType pt = argumentTypes[i];
                if (pt.IsByRef || pt.IsPointer || pt.IsFunctionPointer || pt.IsValueType)
                {
                    return false;
                }
            }

            if (method is ConstructorInfo &&
                !method.IsStatic &&
                method.DeclaringType is Type declaringType &&
                !declaringType.IsValueType &&
                !declaringType.IsAbstract &&
                !declaringType.IsByRefLike &&
                !declaringType.ContainsGenericParameters &&
                declaringType != typeof(string)) // see Ctor_0
            {
                thunk = argCount switch
                {
                    0 => &Ctor_0,
                    1 => &Ctor_1,
                    2 => &Ctor_2,
                    3 => &Ctor_3,
                    _ => null,
                };

                if (thunk is null)
                {
                    return false;
                }

                functionPointer = method.MethodHandle.GetFunctionPointer();
                return true;
            }

            if (method.IsStatic)
            {
                Type returnType = method is MethodInfo mi ? mi.ReturnType : typeof(void);

                thunk = argCount switch
                {
                    0 => ClassifyStatic0Return(returnType),
                    1 => ClassifyStatic1Obj(returnType),
                    2 => ClassifyStatic2Obj(returnType),
                    _ => null,
                };

                if (thunk is null)
                {
                    return false;
                }

                functionPointer = method.MethodHandle.GetFunctionPointer();
                return true;
            }

            return false;
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

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyStatic1Obj(Type returnType)
        {
            if (returnType == typeof(void)) return &Static_Void_1Obj;
            if (!returnType.IsValueType && !returnType.IsByRef && !returnType.IsPointer && !returnType.IsFunctionPointer)
                return &Static_Object_1Obj;
            return null;
        }

        private static delegate*<IntPtr, object?, IntPtr*, Type?, object?> ClassifyStatic2Obj(Type returnType)
        {
            if (returnType == typeof(void)) return &Static_Void_2Obj;
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

        // Ctor thunks: `obj` non-null = call ctor on existing instance, null = allocate first.
        // `string` excluded: `newobj String(...)` is JIT-lowered to a hidden static allocator
        // (`METHOD__STRING__CTORF_*` in src/coreclr/vm/corelib.h, wired by
        // `ECall::PopulateManagedStringConstructors`); the public ctor has no callable instance
        // entry, and `GetUninitializedObject(typeof(string))` is runtime-rejected.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "Caller anchors the ctor MethodBase, keeping its type reachable.")]
        private static object? Ctor_0(IntPtr fn, object? obj, IntPtr* _, Type? declaringType)
        {
            object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
            InstanceCalliHelper.Call((delegate*<object, void>)fn, instance);
            return obj is null ? instance : null;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "See Ctor_0.")]
        private static object? Ctor_1(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]));
            return obj is null ? instance : null;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "See Ctor_0.")]
        private static object? Ctor_2(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]),
                Unsafe.Read<object?>((void*)args[1]));
            return obj is null ? instance : null;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "See Ctor_0.")]
        private static object? Ctor_3(IntPtr fn, object? obj, IntPtr* args, Type? declaringType)
        {
            object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
            InstanceCalliHelper.Call(
                (delegate*<object, object?, object?, object?, void>)fn,
                instance,
                Unsafe.Read<object?>((void*)args[0]),
                Unsafe.Read<object?>((void*)args[1]),
                Unsafe.Read<object?>((void*)args[2]));
            return obj is null ? instance : null;
        }
    }
}
