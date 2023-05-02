// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Reflection.Augments;
using Internal.Runtime;
using Internal.Runtime.Augments;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;
using System.Threading;

using Debug = System.Diagnostics.Debug;

namespace System.Runtime.CompilerServices
{
    public static partial class RuntimeHelpers
    {
        [Intrinsic]
        public static void InitializeArray(Array array, RuntimeFieldHandle fldHandle)
        {
            // We only support this intrinsic when it occurs within a well-defined IL sequence.
            // If a call to this method occurs within the recognized sequence, codegen must expand the IL sequence completely.
            // For any other purpose, the API is currently unsupported.
            // https://github.com/dotnet/corert/issues/364
            throw new PlatformNotSupportedException();
        }

#pragma warning disable IDE0060
        private static unsafe void* GetSpanDataFrom(
            RuntimeFieldHandle fldHandle,
            RuntimeTypeHandle targetTypeHandle,
            out int count)
        {
            // We only support this intrinsic when it occurs within a well-defined IL sequence.
            // If a call to this method occurs within the recognized sequence, codegen must expand the IL sequence completely.
            // For any other purpose, the API is currently unsupported.
            // https://github.com/dotnet/corert/issues/364
            throw new PlatformNotSupportedException();
        }
#pragma warning disable IDE0060

        [RequiresUnreferencedCode("Trimmer can't guarantee existence of class constructor")]
        public static void RunClassConstructor(RuntimeTypeHandle type)
        {
            if (type.IsNull)
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized);

            IntPtr pStaticClassConstructionContext = RuntimeAugments.Callbacks.TryGetStaticClassConstructionContext(type);
            if (pStaticClassConstructionContext == IntPtr.Zero)
                return;

            unsafe
            {
                ClassConstructorRunner.EnsureClassConstructorRun((StaticClassConstructionContext*)pStaticClassConstructionContext);
            }
        }

        public static void RunModuleConstructor(ModuleHandle module)
        {
            if (module.AssociatedModule == null)
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized);

            ReflectionAugments.ReflectionCoreCallbacks.RunModuleConstructor(module.AssociatedModule);
        }

        public static object GetObjectValue(object? obj)
        {
            if (obj == null)
                return null;

            EETypePtr eeType = obj.GetEETypePtr();
            if ((!eeType.IsValueType) || eeType.IsPrimitive)
                return obj;

            return obj.MemberwiseClone();
        }

        public static new bool Equals(object? o1, object? o2)
        {
            if (o1 == o2)
                return true;

            if ((o1 == null) || (o2 == null))
                return false;

            // If it's not a value class, don't compare by value
            if (!o1.GetEETypePtr().IsValueType)
                return false;

            // Make sure they are the same type.
            if (o1.GetEETypePtr() != o2.GetEETypePtr())
                return false;

            return RuntimeImports.RhCompareObjectContentsAndPadding(o1, o2);
        }

        internal static int GetNewHashCode()
        {
            return Random.Shared.Next();
        }

        public static unsafe int GetHashCode(object o)
        {
            return ObjectHeader.GetHashCode(o);
        }

        /// <summary>
        /// If a hash code has been assigned to the object, it is returned. Otherwise zero is
        /// returned.
        /// </summary>
        /// <remarks>
        /// The advantage of this over <see cref="GetHashCode" /> is that it avoids assigning a hash
        /// code to the object if it does not already have one.
        /// </remarks>
        internal static int TryGetHashCode(object o)
        {
            return ObjectHeader.TryGetHashCode(o);
        }

        [Obsolete("OffsetToStringData has been deprecated. Use string.GetPinnableReference() instead.")]
        public static int OffsetToStringData
        {
            // This offset is baked in by string indexer intrinsic, so there is no harm
            // in getting it baked in here as well.
            [System.Runtime.Versioning.NonVersionable]
            get =>
                // Number of bytes from the address pointed to by a reference to
                // a String to the first 16-bit character in the String.  Skip
                // over the MethodTable pointer, & String
                // length.  Of course, the String reference points to the memory
                // after the sync block, so don't count that.
                // This property allows C#'s fixed statement to work on Strings.
                // On 64 bit platforms, this should be 12 (8+4) and on 32 bit 8 (4+4).
#if TARGET_64BIT
                12;
#else // 32
                8;
#endif // TARGET_64BIT

        }

        [ThreadStatic]
        private static unsafe byte* t_sufficientStackLimit;

        public static unsafe void EnsureSufficientExecutionStack()
        {
            byte* limit = t_sufficientStackLimit;
            if (limit == null)
                limit = GetSufficientStackLimit();

            byte* currentStackPtr = (byte*)(&limit);
            if (currentStackPtr < limit)
                throw new InsufficientExecutionStackException();
        }

        public static unsafe bool TryEnsureSufficientExecutionStack()
        {
            byte* limit = t_sufficientStackLimit;
            if (limit == null)
                limit = GetSufficientStackLimit();

            byte* currentStackPtr = (byte*)(&limit);
            return (currentStackPtr >= limit);
        }

        [MethodImpl(MethodImplOptions.NoInlining)] // Only called once per thread, no point in inlining.
        private static unsafe byte* GetSufficientStackLimit()
        {
            IntPtr lower, upper;
            RuntimeImports.RhGetCurrentThreadStackBounds(out lower, out upper);

            // Compute the limit used by EnsureSufficientExecutionStack and cache it on the thread. This minimum
            // stack size should be sufficient to allow a typical non-recursive call chain to execute, including
            // potential exception handling and garbage collection.

#if TARGET_64BIT
            const int MinExecutionStackSize = 128 * 1024;
#else
            const int MinExecutionStackSize = 64 * 1024;
#endif

            byte* limit = (((byte*)upper - (byte*)lower > MinExecutionStackSize)) ?
                ((byte*)lower + MinExecutionStackSize) : ((byte*)upper);

            return (t_sufficientStackLimit = limit);
        }

        [Intrinsic]
        public static bool IsReferenceOrContainsReferences<T>()
        {
            var pEEType = EETypePtr.EETypePtrOf<T>();
            return !pEEType.IsValueType || pEEType.ContainsGCPointers;
        }

        [Intrinsic]
        internal static bool IsReference<T>()
        {
            var pEEType = EETypePtr.EETypePtrOf<T>();
            return !pEEType.IsValueType;
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsBitwiseEquatable<T>()
        {
            // Only reachable for universal shared code - the compiler replaces this otherwise.
            // Returning false is conservative.
            return false;
        }

        internal static ref byte GetRawData(this object obj) =>
            ref Unsafe.As<RawData>(obj).Data;

        internal static unsafe nuint GetRawObjectDataSize(this object obj)
        {
            MethodTable* pMT = GetMethodTable(obj);

            // See comment on RawArrayData for details
            nuint rawSize = pMT->BaseSize - (nuint)(2 * sizeof(IntPtr));
            if (pMT->HasComponentSize)
                rawSize += (uint)Unsafe.As<RawArrayData>(obj).Length * (nuint)pMT->ComponentSize;

            GC.KeepAlive(obj); // Keep MethodTable alive

            return rawSize;
        }

        internal static unsafe ushort GetElementSize(this Array array)
        {
            return array.GetMethodTable()->ComponentSize;
        }

        internal static unsafe MethodTable* GetMethodTable(this object obj)
            => obj.m_pEEType;

        internal static unsafe ref MethodTable* GetMethodTableRef(this object obj)
            => ref obj.m_pEEType;

        internal static unsafe EETypePtr GetEETypePtr(this object obj)
            => new EETypePtr(obj.m_pEEType);

        // Returns true iff the object has a component size;
        // i.e., is variable length like System.String or Array.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe bool ObjectHasComponentSize(object obj)
        {
            return GetMethodTable(obj)->HasComponentSize;
        }

        public static void PrepareMethod(RuntimeMethodHandle method)
        {
            if (method.Value == IntPtr.Zero)
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, nameof(method));
        }

        public static void PrepareMethod(RuntimeMethodHandle method, RuntimeTypeHandle[] instantiation)
        {
            if (method.Value == IntPtr.Zero)
                throw new ArgumentException(SR.InvalidOperation_HandleIsNotInitialized, nameof(method));
        }

        /// <summary>
        /// Allocate memory that is associated with the <paramref name="type"/> and
        /// will be freed if and when the <see cref="System.Type"/> is unloaded.
        /// </summary>
        /// <param name="type">Type associated with the allocated memory.</param>
        /// <param name="size">Amount of memory in bytes to allocate.</param>
        /// <returns>The allocated memory</returns>
        public static unsafe IntPtr AllocateTypeAssociatedMemory(Type type, int size)
        {
            if (type is not RuntimeType)
                throw new ArgumentException(SR.Arg_MustBeType, nameof(type));

            ArgumentOutOfRangeException.ThrowIfNegative(size);

            // We don't support unloading; the memory will never be freed.
            return (IntPtr)NativeMemory.AllocZeroed((uint)size);
        }

        public static void PrepareDelegate(Delegate d)
        {
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2059:UnrecognizedReflectionPattern",
            Justification = "We keep class constructors of all types with an MethodTable")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern",
            Justification = "Constructed MethodTable of a Nullable forces a constructed MethodTable of the element type")]
        public static unsafe object GetUninitializedObject(
            // This API doesn't call any constructors, but the type needs to be seen as constructed.
            // A type is seen as constructed if a constructor is kept.
            // This obviously won't cover a type with no constructor. Reference types with no
            // constructor are an academic problem. Valuetypes with no constructors are a problem,
            // but IL Linker currently treats them as always implicitly boxed.
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
            Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type), SR.ArgumentNull_Type);
            }

            if (type is not RuntimeType)
            {
                throw new SerializationException(SR.Format(SR.Serialization_InvalidType, type));
            }

            if (type.HasElementType || type.IsGenericParameter || type.IsFunctionPointer)
            {
                throw new ArgumentException(SR.Argument_InvalidValue);
            }

            if (type.ContainsGenericParameters)
            {
                throw new MemberAccessException(SR.Acc_CreateGeneric);
            }

            if (type.IsCOMObject)
            {
                throw new NotSupportedException(SR.NotSupported_ManagedActivation);
            }

            if (type.IsAbstract)
            {
                throw new MemberAccessException(SR.Acc_CreateAbst);
            }

            MethodTable* mt = type.TypeHandle.ToMethodTable();

            if (mt->ElementType == Internal.Runtime.EETypeElementType.Void)
            {
                throw new ArgumentException(SR.Argument_InvalidValue);
            }

            // Don't allow strings (we already checked for arrays above)
            if (mt->HasComponentSize)
            {
                throw new ArgumentException(SR.Argument_NoUninitializedStrings);
            }

            if (RuntimeImports.AreTypesAssignable(mt, MethodTable.Of<Delegate>()))
            {
                throw new MemberAccessException();
            }

            if (mt->IsByRefLike)
            {
                throw new NotSupportedException(SR.NotSupported_ByRefLike);
            }

            if (mt->IsNullable)
            {
                mt = mt->NullableType;
                return GetUninitializedObject(Type.GetTypeFromEETypePtr(new EETypePtr(mt)));
            }

            // Triggering the .cctor here is slightly different than desktop/CoreCLR, which
            // decide based on BeforeFieldInit, but we don't want to include BeforeFieldInit
            // in MethodTable just for this API to behave slightly differently.
            RunClassConstructor(type.TypeHandle);

            return RuntimeImports.RhNewObject(mt);
        }
    }

    // CLR arrays are laid out in memory as follows (multidimensional array bounds are optional):
    // [ sync block || pMethodTable || num components || MD array bounds || array data .. ]
    //                 ^               ^                 ^                  ^ returned reference
    //                 |               |                 \-- ref Unsafe.As<RawArrayData>(array).Data
    //                 \-- array       \-- ref Unsafe.As<RawData>(array).Data
    // The BaseSize of an array includes all the fields before the array data,
    // including the sync block and method table. The reference to RawData.Data
    // points at the number of components, skipping over these two pointer-sized fields.
    [StructLayout(LayoutKind.Sequential)]
    internal class RawArrayData
    {
        public uint Length; // Array._numComponents padded to IntPtr
#if TARGET_64BIT
        public uint Padding;
#endif
        public byte Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal class RawData
    {
        public byte Data;
    }
}
