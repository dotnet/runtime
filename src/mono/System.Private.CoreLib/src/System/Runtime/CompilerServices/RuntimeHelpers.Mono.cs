// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.Serialization;

namespace System.Runtime.CompilerServices
{
    public partial class RuntimeHelpers
    {
        public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentNullException.ThrowIfNull(fldHandle.Value, nameof(fldHandle));

            InitializeArray(array, fldHandle.Value);
        }

        private static unsafe void* GetSpanDataFrom(
            RuntimeFieldHandle fldHandle,
            RuntimeTypeHandle targetTypeHandle,
            out int count)
        {
            fixed (int *pCount = &count)
            {
                return (void*)GetSpanDataFrom(fldHandle.Value, targetTypeHandle.Value, new IntPtr(pCount));
            }
        }

        public static int OffsetToStringData
        {
            [Intrinsic]
            get => OffsetToStringData;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalGetHashCode(object? o);

        public static int GetHashCode(object? o)
        {
            return InternalGetHashCode(o);
        }

        public static new bool Equals(object? o1, object? o2)
        {
            if (o1 == o2)
                return true;

            if (o1 == null || o2 == null)
                return false;

            if (o1 is ValueType)
                return ValueType.DefaultEquals(o1, o2);

            return false;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern object? GetObjectValue(object? obj);

        [RequiresUnreferencedCode("Trimmer can't guarantee existence of class constructor")]
        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            if (type.Value == IntPtr.Zero)
                throw new ArgumentException("Handle is not initialized.", nameof(type));

            RunClassConstructor(type.Value);
        }

        public static void EnsureSufficientExecutionStack()
        {
            if (SufficientExecutionStack())
                return;

            throw new InsufficientExecutionStackException();
        }

        public static bool TryEnsureSufficientExecutionStack()
        {
            return SufficientExecutionStack();
        }

        public static void PrepareDelegate(Delegate d)
        {
        }

        public static void PrepareMethod(RuntimeMethodHandle method)
        {
            if (method.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);
            unsafe
            {
                PrepareMethod(method.Value, null, 0);
            }
        }

        public static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[]? instantiation)
        {
            if (method.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);
            unsafe
            {
                IntPtr[]? instantiations = RuntimeTypeHandle.CopyRuntimeTypeHandles(instantiation, out int length);
                fixed (IntPtr* pinst = instantiations)
                {
                    PrepareMethod(method.Value, pinst, length);
                    GC.KeepAlive(instantiation);
                }
            }
        }

        public static void RunModuleConstructor(ModuleHandle module)
        {
            if (module == ModuleHandle.EmptyHandle)
                throw new ArgumentException("Handle is not initialized.", nameof(module));

            RunModuleConstructor(module.Value);
        }

        public static IntPtr AllocateTypeAssociatedMemory(Type type, int size)
        {
            throw new PlatformNotSupportedException();
        }

        [Intrinsic]
        internal static ref byte GetRawData(this object obj) => ref obj.GetRawData();

        [Intrinsic]
        public static bool IsReferenceOrContainsReferences<T>() => IsReferenceOrContainsReferences<T>();

        [Intrinsic]
        internal static bool IsBitwiseEquatable<T>() => IsBitwiseEquatable<T>();

        [Intrinsic]
        internal static bool ObjectHasComponentSize(object obj) => ObjectHasComponentSize(obj);

        [Intrinsic]
        internal static bool ObjectHasReferences(object obj)
        {
            // TODO: Missing intrinsic in interpreter
            return RuntimeTypeHandle.HasReferences((obj.GetType() as RuntimeType)!);
        }

        // A conservative GC already scans the stack looking for potential object-refs or by-refs.
        // Mono uses a conservative GC so there is no need for this API to be full implemented.
        internal unsafe ref struct GCFrameRegistration
        {
            public GCFrameRegistration(void* allocation, uint elemCount, bool areByRefs = true)
            {
            }
        }

        internal static unsafe void RegisterForGCReporting(GCFrameRegistration* pRegistration) { /* nop */ }
        internal static unsafe void UnregisterForGCReporting(GCFrameRegistration* pRegistration) { /* nop */ }

        public static object GetUninitializedObject(
            // This API doesn't call any constructors, but the type needs to be seen as constructed.
            // A type is seen as constructed if a constructor is kept.
            // This obviously won't cover a type with no constructor. Reference types with no
            // constructor are an academic problem. Valuetypes with no constructors are a problem,
            // but IL Linker currently treats them as always implicitly boxed.
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type)
        {
            if (type is not RuntimeType rt)
            {
                ArgumentNullException.ThrowIfNull(type);

                throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type));
            }

            return GetUninitializedObjectInternal(new RuntimeTypeHandle(rt).Value);
        }


        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void PrepareMethod(IntPtr method, IntPtr* instantiations, int ninst);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object GetUninitializedObjectInternal(IntPtr type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InitializeArray(Array array, IntPtr fldHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe IntPtr GetSpanDataFrom(
            IntPtr fldHandle,
            IntPtr targetTypeHandle,
            IntPtr count);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunClassConstructor(IntPtr type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunModuleConstructor(IntPtr module);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SufficientExecutionStack();
    }
}
