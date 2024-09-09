// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
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

        private static unsafe ref byte GetSpanDataFrom(
            RuntimeFieldHandle fldHandle,
            RuntimeTypeHandle targetTypeHandle,
            out int count)
        {
            fixed (int *pCount = &count)
            {
                return ref GetSpanDataFrom(fldHandle.Value, targetTypeHandle.Value, new IntPtr(pCount));
            }
        }

        [Obsolete("OffsetToStringData has been deprecated. Use string.GetPinnableReference() instead.")]
        public static int OffsetToStringData => string.OFFSET_TO_STRING;

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int InternalGetHashCode(object? o);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetHashCode(object? o)
        {
            // NOTE: the interpreter does not run this code.  It intrinsifies the whole RuntimeHelpers.GetHashCode function
            if (Threading.ObjectHeader.TryGetHashCode(o, out int hash))
                return hash;
            return InternalGetHashCode(o);
        }

        /// <summary>
        /// If a hash code has been assigned to the object, it is returned. Otherwise zero is
        /// returned.
        /// </summary>
        /// <remarks>
        /// The advantage of this over <see cref="GetHashCode" /> is that it avoids assigning a hash
        /// code to the object if it does not already have one.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int TryGetHashCode(object? o)
        {
            // NOTE: the interpreter does not run this code.  It intrinsifies the whole RuntimeHelpers.TryGetHashCode function
            if (Threading.ObjectHeader.TryGetHashCode(o, out int hash))
                return hash;
            return 0;
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
        [return: NotNullIfNotNull(nameof(obj))]
        public static extern object? GetObjectValue(object? obj);

        [RequiresUnreferencedCode("Trimmer can't guarantee existence of class constructor")]
        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            if (type.Value == IntPtr.Zero)
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, nameof(type));

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
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, nameof(module));

            RunModuleConstructor(module.Value);
        }

        public static unsafe IntPtr AllocateTypeAssociatedMemory(Type type, int size)
        {
            if (type is not RuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            ArgumentOutOfRangeException.ThrowIfNegative(size);

            // We don't support unloading; the memory will never be freed.
            return (IntPtr)NativeMemory.AllocZeroed((uint)size);
        }

        [Intrinsic]
        internal static ref byte GetRawData(this object obj) => ref obj.GetRawData();

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
        private static extern unsafe ref byte GetSpanDataFrom(
            IntPtr fldHandle,
            IntPtr targetTypeHandle,
            IntPtr count);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunClassConstructor(IntPtr type);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void RunModuleConstructor(IntPtr module);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern bool SufficientExecutionStack();

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern object InternalBox(QCallTypeHandle type, ref byte target);

        /// <summary>
        /// Create a boxed object of the specified type from the data located at the target reference.
        /// </summary>
        /// <param name="target">The target data</param>
        /// <param name="type">The type of box to create.</param>
        /// <returns>A boxed object containing the specified data.</returns>
        /// <exception cref="ArgumentNullException">The specified type handle is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The specified type cannot have a boxed instance of itself created.</exception>
        /// <exception cref="NotSupportedException">The passed in type is a by-ref-like type.</exception>
        /// <remarks>This returns an object that is equivalent to executing the IL box instruction with the provided target address and type.</remarks>
        public static object? Box(ref byte target, RuntimeTypeHandle type)
        {
            if (type.Value is 0)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.type);

            // Compatibility with CoreCLR, throw on a null reference to the unboxed data.
            if (Unsafe.IsNullRef(ref target))
                throw new NullReferenceException();

            RuntimeType rtType = (RuntimeType)Type.GetTypeFromHandle(type)!;

            if (rtType.ContainsGenericParameters
                || rtType.IsPointer
                || rtType.IsFunctionPointer
                || rtType.IsByRef
                || rtType.IsGenericParameter
                || rtType == typeof(void))
            {
                throw new ArgumentException(SR.Arg_TypeNotSupported);
            }

            if (!rtType.IsValueType)
            {
                return Unsafe.As<byte, object?>(ref target);
            }

            if (rtType.IsByRefLike)
                throw new NotSupportedException(SR.NotSupported_ByRefLike);

            object? result = InternalBox(new QCallTypeHandle(ref rtType), ref target);
            return result;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern int SizeOf(QCallTypeHandle handle);

        /// <summary>
        /// Get the size of an object of the given type.
        /// </summary>
        /// <param name="type">The type to get the size of.</param>
        /// <returns>The size of instances of the type.</returns>
        /// <exception cref="ArgumentException">The passed-in type is not a valid type to get the size of.</exception>
        /// <remarks>
        /// This API returns the same value as <see cref="Unsafe.SizeOf{T}"/> for the type that <paramref name="type"/> represents.
        /// </remarks>
        public static int SizeOf(RuntimeTypeHandle type)
        {
            if (type.Value == IntPtr.Zero)
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.type);

            Type typeObj = Type.GetTypeFromHandle(type)!;
            if (typeObj.ContainsGenericParameters || typeObj.IsGenericParameter || typeObj == typeof(void))
                throw new ArgumentException(SR.Arg_TypeNotSupported);

            return SizeOf(new QCallTypeHandle(ref type));
        }
    }
}
