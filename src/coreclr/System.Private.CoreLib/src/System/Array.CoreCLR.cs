// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    // Note that we make a T[] (single-dimensional w/ zero as the lower bound) implement both
    // IList<U> and IReadOnlyList<U>, where T : U dynamically.  See the SZArrayHelper class for details.
    public abstract partial class Array : ICloneable, IList, IStructuralComparable, IStructuralEquatable
    {
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Array_CreateInstance")]
        private static unsafe partial void InternalCreate(QCallTypeHandle type, int rank, int* pLengths, int* pLowerBounds,
            [MarshalAs(UnmanagedType.Bool)] bool fromArrayType, ObjectHandleOnStack retArray);

        private static unsafe Array InternalCreate(RuntimeType elementType, int rank, int* pLengths, int* pLowerBounds)
        {
            Array? retArray = null;
            InternalCreate(new QCallTypeHandle(ref elementType), rank, pLengths, pLowerBounds,
                fromArrayType: false, ObjectHandleOnStack.Create(ref retArray));
            return retArray!;
        }

        private static unsafe Array InternalCreateFromArrayType(RuntimeType arrayType, int rank, int* pLengths, int* pLowerBounds)
        {
            Array? retArray = null;
            InternalCreate(new QCallTypeHandle(ref arrayType), rank, pLengths, pLowerBounds,
                fromArrayType: true, ObjectHandleOnStack.Create(ref retArray));
            return retArray!;
        }

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Array_CreateInstanceMDArray")]
        private static unsafe partial void CreateInstanceMDArray(nint typeHandle, uint dwNumArgs, void* pArgList, ObjectHandleOnStack retArray);

        // implementation of CORINFO_HELP_NEW_MDARR and CORINFO_HELP_NEW_MDARR_RARE.
        [StackTraceHidden]
        [DebuggerStepThrough]
        [DebuggerHidden]
        internal static unsafe object CreateInstanceMDArray(nint typeHandle, uint dwNumArgs, void* pArgList)
        {
            Array? arr = null;
            CreateInstanceMDArray(typeHandle, dwNumArgs, pArgList, ObjectHandleOnStack.Create(ref arr));
            return arr!;
        }

        private static bool SupportsNonZeroLowerBound => true;

        private static CorElementType GetNormalizedIntegralArrayElementType(CorElementType elementType)
        {
            Debug.Assert(elementType.IsPrimitiveType());

            // Array Primitive types such as E_T_I4 and E_T_U4 are interchangeable
            // Enums with interchangeable underlying types are interchangeable
            // BOOL is NOT interchangeable with I1/U1, neither CHAR -- with I2/U2

            // U1/U2/U4/U8/U
            int shift = (0b0010_0000_0000_0000_1010_1010_0000 >> (int)elementType) & 1;
            return (CorElementType)((int)elementType - shift);
        }

        private static unsafe ArrayAssignType CanAssignArrayType(Array sourceArray, Array destinationArray)
        {
            TypeHandle srcTH = RuntimeHelpers.GetMethodTable(sourceArray)->GetArrayElementTypeHandle();
            TypeHandle destTH = RuntimeHelpers.GetMethodTable(destinationArray)->GetArrayElementTypeHandle();

            if (TypeHandle.AreSameType(srcTH, destTH)) // This check kicks for different array kind or dimensions
                return ArrayAssignType.SimpleCopy;

            if (srcTH.IsTypeDesc || destTH.IsTypeDesc)
            {
                // Only pointers are valid for TypeDesc in array element

                // Compatible pointers
                if (srcTH.CanCastTo(destTH))
                    return ArrayAssignType.SimpleCopy;
                else
                    return ArrayAssignType.WrongType;
            }

            MethodTable* pMTsrc = srcTH.AsMethodTable();
            MethodTable* pMTdest = destTH.AsMethodTable();

            // Value class boxing
            if (pMTsrc->IsValueType && !pMTdest->IsValueType)
            {
                if (srcTH.CanCastTo(destTH))
                    return ArrayAssignType.BoxValueClassOrPrimitive;
                else
                    return ArrayAssignType.WrongType;
            }

            // Value class unboxing.
            if (!pMTsrc->IsValueType && pMTdest->IsValueType)
            {
                if (srcTH.CanCastTo(destTH))
                    return ArrayAssignType.UnboxValueClass;
                else if (destTH.CanCastTo(srcTH))   // V extends IV. Copying from IV to V, or Object to V.
                    return ArrayAssignType.UnboxValueClass;
                else
                    return ArrayAssignType.WrongType;
            }

            // Copying primitives from one type to another
            if (pMTsrc->IsPrimitive && pMTdest->IsPrimitive)
            {
                CorElementType srcElType = pMTsrc->GetPrimitiveCorElementType();
                CorElementType destElType = pMTdest->GetPrimitiveCorElementType();

                if (GetNormalizedIntegralArrayElementType(srcElType) == GetNormalizedIntegralArrayElementType(destElType))
                    return ArrayAssignType.SimpleCopy;
                else if (RuntimeHelpers.CanPrimitiveWiden(srcElType, destElType))
                    return ArrayAssignType.PrimitiveWiden;
                else
                    return ArrayAssignType.WrongType;
            }

            // src Object extends dest
            if (srcTH.CanCastTo(destTH))
                return ArrayAssignType.SimpleCopy;

            // dest Object extends src
            if (destTH.CanCastTo(srcTH))
                return ArrayAssignType.MustCast;

            // class X extends/implements src and implements dest.
            if (pMTdest->IsInterface)
                return ArrayAssignType.MustCast;

            // class X implements src and extends/implements dest
            if (pMTsrc->IsInterface)
                return ArrayAssignType.MustCast;

            return ArrayAssignType.WrongType;
        }

        internal unsafe object? InternalGetValue(nint flattenedIndex)
        {
            MethodTable* pMethodTable = RuntimeHelpers.GetMethodTable(this);

            TypeHandle arrayElementTypeHandle = pMethodTable->GetArrayElementTypeHandle();

            // Legacy behavior (none of the cases where the element type is a type descriptor are supported).
            // That is, this happens when the element type is either a pointer or a function pointer type.
            if (arrayElementTypeHandle.IsTypeDesc)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            Debug.Assert((nuint)flattenedIndex < NativeLength);

            ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(this);
            object? result;

            MethodTable* pElementMethodTable = arrayElementTypeHandle.AsMethodTable();

            if (pElementMethodTable->IsValueType)
            {
                // If the element is a value type, shift to the right offset using the component size, then box
                ref byte offsetDataRef = ref Unsafe.Add(ref arrayDataRef, flattenedIndex * pMethodTable->ComponentSize);

                result = RuntimeHelpers.Box(pElementMethodTable, ref offsetDataRef);
            }
            else
            {
                // The element is a reference type, so no need to retrieve the component size.
                // Just offset with object size as element type, since it's the same regardless of T.
                ref object elementRef = ref Unsafe.As<byte, object>(ref arrayDataRef);
                ref object offsetElementRef = ref Unsafe.Add(ref elementRef, (nuint)flattenedIndex);

                result = offsetElementRef;
            }

            GC.KeepAlive(this); // Keep the method table alive

            return result;
        }

        private unsafe void InternalSetValue(object? value, nint flattenedIndex)
        {
            MethodTable* pMethodTable = RuntimeHelpers.GetMethodTable(this);

            TypeHandle arrayElementTypeHandle = pMethodTable->GetArrayElementTypeHandle();

            // Legacy behavior (this handles pointers and function pointers)
            if (arrayElementTypeHandle.IsTypeDesc)
            {
                ThrowHelper.ThrowNotSupportedException(ExceptionResource.Arg_TypeNotSupported);
            }

            Debug.Assert((nuint)flattenedIndex < NativeLength);

            ref byte arrayDataRef = ref MemoryMarshal.GetArrayDataReference(this);

            MethodTable* pElementMethodTable = arrayElementTypeHandle.AsMethodTable();

            if (value == null)
            {
                // Null is the universal zero...
                if (pElementMethodTable->IsValueType)
                {
                    ref byte offsetDataRef = ref Unsafe.Add(ref arrayDataRef, flattenedIndex * pMethodTable->ComponentSize);
                    if (pElementMethodTable->ContainsGCPointers)
                    {
                        nuint elementSize = pElementMethodTable->GetNumInstanceFieldBytesIfContainsGCPointers();
                        SpanHelpers.ClearWithReferences(ref Unsafe.As<byte, nint>(ref offsetDataRef), elementSize / (nuint)sizeof(IntPtr));
                    }
                    else
                    {
                        nuint elementSize = pElementMethodTable->GetNumInstanceFieldBytes();
                        SpanHelpers.ClearWithoutReferences(ref offsetDataRef, elementSize);
                    }
                }
                else
                {
                    Unsafe.Add(ref Unsafe.As<byte, object?>(ref arrayDataRef), (nuint)flattenedIndex) = null;
                }
            }
            else if (!pElementMethodTable->IsValueType)
            {
                if (pElementMethodTable != TypeHandle.TypeHandleOf<object>().AsMethodTable() //  Everything is compatible with Object
                    && CastHelpers.IsInstanceOfAny(pElementMethodTable, value) == null)
                    throw new InvalidCastException(SR.InvalidCast_StoreArrayElement);

                Unsafe.Add(ref Unsafe.As<byte, object?>(ref arrayDataRef), (nuint)flattenedIndex) = value;
            }
            else
            {
                // value class or primitive type

                ref byte offsetDataRef = ref Unsafe.Add(ref arrayDataRef, flattenedIndex * pMethodTable->ComponentSize);
                if (CastHelpers.IsInstanceOfAny(pElementMethodTable, value) != null)
                {
                    if (pElementMethodTable->IsNullable)
                    {
                        CastHelpers.Unbox_Nullable(ref offsetDataRef, pElementMethodTable, value);
                    }
                    else
                    {
                        if (pElementMethodTable->ContainsGCPointers)
                        {
                            nuint elementSize = pElementMethodTable->GetNumInstanceFieldBytesIfContainsGCPointers();
                            Buffer.BulkMoveWithWriteBarrier(ref offsetDataRef, ref value.GetRawData(), elementSize);
                        }
                        else
                        {
                            nuint elementSize = pElementMethodTable->GetNumInstanceFieldBytes();
                            SpanHelpers.Memmove(ref offsetDataRef, ref value.GetRawData(), elementSize);
                        }
                    }
                }
                else
                {
                    // Allow enum -> primitive conversion, disallow primitive -> enum conversion
                    MethodTable* pValueMethodTable = RuntimeHelpers.GetMethodTable(value);

                    // Array.SetValue() does *not* permit conversion from a primitive to an Enum.
                    if (!pValueMethodTable->IsPrimitive || !pElementMethodTable->IsTruePrimitive)
                        throw new InvalidCastException(SR.InvalidCast_StoreArrayElement);

                    CorElementType srcType = pValueMethodTable->GetPrimitiveCorElementType();
                    CorElementType targetType = pElementMethodTable->GetPrimitiveCorElementType();

                    // Get a properly widened type
                    if (!RuntimeHelpers.CanPrimitiveWiden(srcType, targetType))
                        throw new ArgumentException(SR.Arg_PrimWiden);

                    if (srcType == targetType)
                    {
                        // Primitive types are always tightly packed in array, using ComponentSize is sufficient.
                        SpanHelpers.Memmove(ref offsetDataRef, ref value.GetRawData(), pMethodTable->ComponentSize);
                    }
                    else
                    {
                        InvokeUtils.PrimitiveWiden(ref value.GetRawData(), ref offsetDataRef, srcType, targetType);
                    }
                }
            }

            GC.KeepAlive(this); // Keep the method table alive
        }

        public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);

        // This could return a length greater than int.MaxValue
        internal nuint NativeLength => Unsafe.As<RawArrayData>(this).Length;

        public long LongLength => (long)NativeLength;

        public int Rank
        {
            get
            {
                int rank = RuntimeHelpers.GetMultiDimensionalArrayRank(this);
                return (rank != 0) ? rank : 1;
            }
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal extern CorElementType GetCorElementTypeOfElementType();

        private unsafe MethodTable* ElementMethodTable => RuntimeHelpers.GetMethodTable(this)->GetArrayElementTypeHandle().AsMethodTable();

        private unsafe bool IsValueOfElementType(object value)
        {
            MethodTable* thisMT = RuntimeHelpers.GetMethodTable(this);
            return (IntPtr)thisMT->ElementType == (IntPtr)RuntimeHelpers.GetMethodTable(value);
        }

        // if this is an array of value classes and that value class has a default constructor
        // then this calls this default constructor on every element in the value class array.
        // otherwise this is a no-op.  Generally this method is called automatically by the compiler
        public unsafe void Initialize()
        {
            MethodTable* pArrayMT = RuntimeHelpers.GetMethodTable(this);
            TypeHandle thElem = pArrayMT->GetArrayElementTypeHandle();
            if (thElem.IsTypeDesc)
            {
                return;
            }

            MethodTable* pElemMT = thElem.AsMethodTable();
            if (!pElemMT->HasDefaultConstructor || !pElemMT->IsValueType)
            {
                return;
            }

            RuntimeType arrayType = (RuntimeType)GetType();

            ArrayInitializeCache cache = arrayType.GetOrCreateCacheEntry<ArrayInitializeCache>();

            delegate*<ref byte, void> constructorFtn = cache.ConstructorEntrypoint;
            ref byte arrayRef = ref MemoryMarshal.GetArrayDataReference(this);
            nuint elementSize = pArrayMT->ComponentSize;

            for (nuint i = 0; i < NativeLength; i++)
            {
                constructorFtn(ref arrayRef);
                arrayRef = ref Unsafe.Add(ref arrayRef, elementSize);
            }
        }

        internal sealed unsafe partial class ArrayInitializeCache : RuntimeType.IGenericCacheEntry<ArrayInitializeCache>
        {
            internal readonly delegate*<ref byte, void> ConstructorEntrypoint;

            [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "Array_GetElementConstructorEntrypoint")]
            private static partial delegate*<ref byte, void> GetElementConstructorEntrypoint(QCallTypeHandle arrayType);

            private ArrayInitializeCache(delegate*<ref byte, void> constructorEntrypoint)
            {
                ConstructorEntrypoint = constructorEntrypoint;
            }

            public static ArrayInitializeCache Create(RuntimeType arrayType) => new(GetElementConstructorEntrypoint(new QCallTypeHandle(ref arrayType)));
            public void InitializeCompositeCache(RuntimeType.CompositeCacheEntry compositeEntry) => compositeEntry._arrayInitializeCache = this;
            public static ref ArrayInitializeCache? GetStorageRef(RuntimeType.CompositeCacheEntry compositeEntry) => ref compositeEntry._arrayInitializeCache;
        }
    }

#pragma warning disable CA1822 // Mark members as static
    //----------------------------------------------------------------------------------------
    // ! READ THIS BEFORE YOU WORK ON THIS CLASS.
    //
    // The methods on this class must be written VERY carefully to avoid introducing security holes.
    // That's because they are invoked with special "this"! The "this" object
    // for all of these methods are not SZArrayHelper objects. Rather, they are of type U[]
    // where U[] is castable to T[]. No actual SZArrayHelper object is ever instantiated. Thus, you will
    // see a lot of expressions that cast "this" "T[]".
    //
    // This class is needed to allow an SZ array of type T[] to expose IList<T>,
    // IList<T.BaseType>, etc., etc. all the way up to IList<Object>. When the following call is
    // made:
    //
    //   ((IList<T>) (new U[n])).SomeIListMethod()
    //
    // the interface stub dispatcher treats this as a special case, loads up SZArrayHelper,
    // finds the corresponding generic method (matched simply by method name), instantiates
    // it for type <T> and executes it.
    //
    // The "T" will reflect the interface used to invoke the method. The actual runtime "this" will be
    // array that is castable to "T[]" (i.e. for primitives and valuetypes, it will be exactly
    // "T[]" - for orefs, it may be a "U[]" where U derives from T.)
    //----------------------------------------------------------------------------------------
    internal sealed class SZArrayHelper
    {
        // It is never legal to instantiate this class.
        private SZArrayHelper()
        {
            Debug.Fail("Hey! How'd I get here?");
        }

        [Intrinsic]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IEnumerator<T> GetEnumerator<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            int length = @this.Length;
            return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(@this, length);
        }

        private void CopyTo<T>(T[] array, int index)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!

            T[] @this = Unsafe.As<T[]>(this);
            Array.Copy(@this, 0, array, index, @this.Length);
        }

        internal int get_Count<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            return @this.Length;
        }

        internal T get_Item<T>(int index)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            if ((uint)index >= (uint)@this.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
            }

            return @this[index];
        }

        internal void set_Item<T>(int index, T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            if ((uint)index >= (uint)@this.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
            }

            @this[index] = value;
        }

        private void Add<T>(T _)
        {
            // Not meaningful for arrays.
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        private bool Contains<T>(T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, value, 0, @this.Length) >= 0;
        }

        private bool get_IsReadOnly<T>()
        {
            return true;
        }

        private void Clear<T>()
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!

            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_ReadOnlyCollection);
        }

        private int IndexOf<T>(T value)
        {
            // ! Warning: "this" is an array, not an SZArrayHelper. See comments above
            // ! or you may introduce a security hole!
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, value, 0, @this.Length);
        }

        private void Insert<T>(int _, T _1)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }

        private bool Remove<T>(T _)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
            return default;
        }

        private void RemoveAt<T>(int _)
        {
            // Not meaningful for arrays
            ThrowHelper.ThrowNotSupportedException(ExceptionResource.NotSupported_FixedSizeCollection);
        }
    }
#pragma warning restore CA1822
}
