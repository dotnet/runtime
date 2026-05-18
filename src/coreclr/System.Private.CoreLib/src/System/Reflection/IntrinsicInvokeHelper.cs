// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Reflection
{
    // Signature-shape descriptor for the no-emit reflection-invoke fast path. The actual calli is
    // performed inline by the invoker (MethodBaseInvoker / MethodInvoker / ConstructorInvoker) so the
    // frame walked by `StackCrawlMark.LookForMyCaller` (e.g. `Type.GetType(string)`,
    // `Assembly.GetCallingAssembly`) is the invoker itself, which is on the runtime's reflection-skip
    // list (SystemDomain::IsReflectionInvocationMethod). Lambdas / closures would land on
    // compiler-generated nested types not in that list and would misreport the caller's assembly.
    internal enum IntrinsicInvokeShape : byte
    {
        None = 0,

        // Static methods, well-known return type, 0-N reference-typed args.
        StaticVoid_0,
        StaticBool_0,
        StaticByte_0,
        StaticSByte_0,
        StaticChar_0,
        StaticShort_0,
        StaticUShort_0,
        StaticInt_0,
        StaticUInt_0,
        StaticLong_0,
        StaticULong_0,
        StaticFloat_0,
        StaticDouble_0,
        StaticNInt_0,
        StaticNUInt_0,
        StaticObject_0,

        StaticVoid_1Obj,
        StaticObject_1Obj,

        StaticVoid_2Obj,

        // Instance constructors. `obj` may be non-null for the "call ctor on existing instance" case.
        CtorObj_0,
        CtorObj_1,
        CtorObj_2,
        CtorObj_3,
    }

    internal static class IntrinsicInvokeHelper
    {
        // Determines whether `method` has a signature shape the invoker can dispatch without emitting
        // a per-method DynamicMethod. On hit, returns the shape descriptor and the target function
        // pointer; the invoker performs the calli itself (see `Dispatch` below).
        internal static unsafe bool TryGetShape(
            MethodBase method,
            out IntrinsicInvokeShape shape,
            out IntPtr functionPointer)
        {
            shape = IntrinsicInvokeShape.None;
            functionPointer = IntPtr.Zero;

            // DynamicMethod.MethodHandle throws — let the emit path handle it.
            if (method is System.Reflection.Emit.DynamicMethod ||
                method.ContainsGenericParameters ||
                (method.CallingConvention & CallingConventions.VarArgs) != 0)
            {
                return false;
            }

            ReadOnlySpan<ParameterInfo> parameters = method.GetParametersAsSpan();
            int argCount = parameters.Length;

            // Only reference-typed args qualify for the object-passing fast path.
            for (int i = 0; i < argCount; i++)
            {
                Type pt = parameters[i].ParameterType;
                if (pt.IsByRef || pt.IsPointer || pt.IsFunctionPointer || pt.IsValueType)
                {
                    return false;
                }
            }

            // Instance constructors: allocate uninitialized + HASTHIS calli through InstanceCalliHelper.Call.
            if (method is ConstructorInfo &&
                !method.IsStatic &&
                method.DeclaringType is Type declaringType &&
                !declaringType.IsValueType &&
                !declaringType.IsAbstract &&
                !declaringType.IsByRefLike &&
                !declaringType.ContainsGenericParameters &&
                declaringType != typeof(string))
            {
                shape = argCount switch
                {
                    0 => IntrinsicInvokeShape.CtorObj_0,
                    1 => IntrinsicInvokeShape.CtorObj_1,
                    2 => IntrinsicInvokeShape.CtorObj_2,
                    3 => IntrinsicInvokeShape.CtorObj_3,
                    _ => IntrinsicInvokeShape.None,
                };

                if (shape == IntrinsicInvokeShape.None)
                {
                    return false;
                }

                functionPointer = method.MethodHandle.GetFunctionPointer();
                return true;
            }

            // Static methods: static-conv calli through InstanceCalliHelper.CallStatic.
            if (method.IsStatic)
            {
                Type returnType = method is MethodInfo mi ? mi.ReturnType : typeof(void);

                switch (argCount)
                {
                    case 0:
                        shape = ClassifyStatic0Return(returnType);
                        break;

                    case 1:
                        if (returnType == typeof(void))
                        {
                            shape = IntrinsicInvokeShape.StaticVoid_1Obj;
                        }
                        else if (!returnType.IsValueType && !returnType.IsByRef &&
                                 !returnType.IsPointer && !returnType.IsFunctionPointer)
                        {
                            shape = IntrinsicInvokeShape.StaticObject_1Obj;
                        }
                        break;

                    case 2:
                        if (returnType == typeof(void))
                        {
                            shape = IntrinsicInvokeShape.StaticVoid_2Obj;
                        }
                        break;
                }

                if (shape == IntrinsicInvokeShape.None)
                {
                    return false;
                }

                functionPointer = method.MethodHandle.GetFunctionPointer();
                return true;
            }

            return false;
        }

        private static IntrinsicInvokeShape ClassifyStatic0Return(Type returnType)
        {
            if (returnType == typeof(void)) return IntrinsicInvokeShape.StaticVoid_0;
            if (returnType == typeof(bool)) return IntrinsicInvokeShape.StaticBool_0;
            if (returnType == typeof(byte)) return IntrinsicInvokeShape.StaticByte_0;
            if (returnType == typeof(sbyte)) return IntrinsicInvokeShape.StaticSByte_0;
            if (returnType == typeof(char)) return IntrinsicInvokeShape.StaticChar_0;
            if (returnType == typeof(short)) return IntrinsicInvokeShape.StaticShort_0;
            if (returnType == typeof(ushort)) return IntrinsicInvokeShape.StaticUShort_0;
            if (returnType == typeof(int)) return IntrinsicInvokeShape.StaticInt_0;
            if (returnType == typeof(uint)) return IntrinsicInvokeShape.StaticUInt_0;
            if (returnType == typeof(long)) return IntrinsicInvokeShape.StaticLong_0;
            if (returnType == typeof(ulong)) return IntrinsicInvokeShape.StaticULong_0;
            if (returnType == typeof(float)) return IntrinsicInvokeShape.StaticFloat_0;
            if (returnType == typeof(double)) return IntrinsicInvokeShape.StaticDouble_0;
            if (returnType == typeof(nint) || returnType.IsFunctionPointer)
                return IntrinsicInvokeShape.StaticNInt_0;
            if (returnType == typeof(nuint)) return IntrinsicInvokeShape.StaticNUInt_0;

            if (!returnType.IsValueType && !returnType.IsByRef && !returnType.IsPointer && !returnType.IsFunctionPointer)
                return IntrinsicInvokeShape.StaticObject_0;

            return IntrinsicInvokeShape.None;
        }

        // Dispatches the calli identified by `shape` / `fn`. AggressiveInlining so the immediate caller
        // of the target method (as seen by `StackCrawlMark.LookForMyCaller`) is the reflection invoker,
        // not this helper. Callers must be methods on a type listed in
        // `SystemDomain::IsReflectionInvocationMethod`.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067:UnrecognizedReflectionPattern",
            Justification = "GetUninitializedObject does not invoke constructors; the caller has already " +
                            "anchored the constructor MethodBase, which keeps the type and its ctor reachable.")]
        internal static unsafe object? Dispatch(
            IntrinsicInvokeShape shape,
            IntPtr fn,
            object? obj,
            IntPtr* args,
            Type? declaringType)
        {
            switch (shape)
            {
                case IntrinsicInvokeShape.StaticVoid_0:
                    ((delegate*<void>)fn)();
                    return null;
                case IntrinsicInvokeShape.StaticBool_0:
                    return ((delegate*<bool>)fn)();
                case IntrinsicInvokeShape.StaticByte_0:
                    return ((delegate*<byte>)fn)();
                case IntrinsicInvokeShape.StaticSByte_0:
                    return ((delegate*<sbyte>)fn)();
                case IntrinsicInvokeShape.StaticChar_0:
                    return ((delegate*<char>)fn)();
                case IntrinsicInvokeShape.StaticShort_0:
                    return ((delegate*<short>)fn)();
                case IntrinsicInvokeShape.StaticUShort_0:
                    return ((delegate*<ushort>)fn)();
                case IntrinsicInvokeShape.StaticInt_0:
                    return ((delegate*<int>)fn)();
                case IntrinsicInvokeShape.StaticUInt_0:
                    return ((delegate*<uint>)fn)();
                case IntrinsicInvokeShape.StaticLong_0:
                    return ((delegate*<long>)fn)();
                case IntrinsicInvokeShape.StaticULong_0:
                    return ((delegate*<ulong>)fn)();
                case IntrinsicInvokeShape.StaticFloat_0:
                    return ((delegate*<float>)fn)();
                case IntrinsicInvokeShape.StaticDouble_0:
                    return ((delegate*<double>)fn)();
                case IntrinsicInvokeShape.StaticNInt_0:
                    return ((delegate*<nint>)fn)();
                case IntrinsicInvokeShape.StaticNUInt_0:
                    return ((delegate*<nuint>)fn)();
                case IntrinsicInvokeShape.StaticObject_0:
                    return ((delegate*<object?>)fn)();

                case IntrinsicInvokeShape.StaticVoid_1Obj:
                    ((delegate*<object?, void>)fn)(Unsafe.Read<object?>((void*)args[0]));
                    return null;
                case IntrinsicInvokeShape.StaticObject_1Obj:
                    return ((delegate*<object?, object?>)fn)(Unsafe.Read<object?>((void*)args[0]));

                case IntrinsicInvokeShape.StaticVoid_2Obj:
                    ((delegate*<object?, object?, void>)fn)(
                        Unsafe.Read<object?>((void*)args[0]),
                        Unsafe.Read<object?>((void*)args[1]));
                    return null;

                case IntrinsicInvokeShape.CtorObj_0:
                    {
                        object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
                        InstanceCalliHelper.Call((delegate*<object, void>)fn, instance);
                        return obj is null ? instance : null;
                    }
                case IntrinsicInvokeShape.CtorObj_1:
                    {
                        object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
                        InstanceCalliHelper.Call(
                            (delegate*<object, object?, void>)fn,
                            instance,
                            Unsafe.Read<object?>((void*)args[0]));
                        return obj is null ? instance : null;
                    }
                case IntrinsicInvokeShape.CtorObj_2:
                    {
                        object instance = obj ?? RuntimeHelpers.GetUninitializedObject(declaringType!);
                        InstanceCalliHelper.Call(
                            (delegate*<object, object?, object?, void>)fn,
                            instance,
                            Unsafe.Read<object?>((void*)args[0]),
                            Unsafe.Read<object?>((void*)args[1]));
                        return obj is null ? instance : null;
                    }
                case IntrinsicInvokeShape.CtorObj_3:
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

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
