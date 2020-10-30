// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    public partial class RuntimeHelpers
    {
        public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (fldHandle.Value == IntPtr.Zero)
                throw new ArgumentNullException(nameof(fldHandle));

            InitializeArray(array, fldHandle.Value);
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
        public static bool IsReferenceOrContainsReferences<T>() => IsReferenceOrContainsReferences<T>();

        [Intrinsic]
        internal static bool IsBitwiseEquatable<T>() => IsBitwiseEquatable<T>();

        [Intrinsic]
        internal static bool ObjectHasComponentSize(object obj) => ObjectHasComponentSize(obj);

        [Intrinsic]
        internal static bool ObjectHasReferences(object obj)
        {
            // TODO: Missing intrinsic in interpreter
            return RuntimeTypeHandle.HasReferences(obj.GetType() as RuntimeType);
        }

        private static object GetUninitializedObjectInternal(Type type)
        {
            return GetUninitializedObjectInternal(new RuntimeTypeHandle((RuntimeType)type).Value);
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern unsafe void PrepareMethod(IntPtr method, IntPtr* instantiations, int ninst);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object GetUninitializedObjectInternal(IntPtr type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void InitializeArray(Array array, IntPtr fldHandle);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunClassConstructor(IntPtr type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunModuleConstructor(IntPtr module);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SufficientExecutionStack();
    }
}
