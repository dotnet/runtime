// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

using Internal.IntrinsicSupport;
using Internal.Runtime.Augments;
using Internal.Runtime.CompilerServices;

using EETypeElementType = Internal.Runtime.EETypeElementType;
using MethodTable = Internal.Runtime.MethodTable;

namespace System
{
    // Note that we make a T[] (single-dimensional w/ zero as the lower bound) implement both
    // IList<U> and IReadOnlyList<U>, where T : U dynamically.  See the SZArrayHelper class for details.
    public abstract partial class Array : ICollection, IEnumerable, IList, IStructuralComparable, IStructuralEquatable, ICloneable
    {
        // CS0169: The field 'Array._numComponents' is never used
        // CA1823: Unused field '_numComponents'
#pragma warning disable 0169
#pragma warning disable CA1823
        // This field should be the first field in Array as the runtime/compilers depend on it
        [NonSerialized]
        private int _numComponents;
#pragma warning restore

        public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);

        // This could return a length greater than int.MaxValue
        internal nuint NativeLength => Unsafe.As<RawArrayData>(this).Length;

        public long LongLength => (long)NativeLength;

        // This is the classlib-provided "get array MethodTable" function that will be invoked whenever the runtime
        // needs to know the base type of an array.
        [RuntimeExport("GetSystemArrayEEType")]
        private static unsafe MethodTable* GetSystemArrayEEType()
        {
            return MethodTable.Of<Array>();
        }

        [RequiresDynamicCode("The code for an array of the specified type might not be available.")]
        private static unsafe Array InternalCreate(RuntimeType elementType, int rank, int* pLengths, int* pLowerBounds)
        {
            if (elementType.IsByRef || elementType.IsByRefLike)
                throw new NotSupportedException(SR.NotSupported_ByRefLikeArray);
            if (elementType == typeof(void))
                throw new NotSupportedException(SR.NotSupported_VoidArray);
            if (elementType.ContainsGenericParameters)
                throw new NotSupportedException(SR.NotSupported_OpenType);

            if (pLowerBounds != null)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (pLowerBounds[i] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);
                }
            }

            if (rank == 1)
            {
                return RuntimeAugments.NewArray(elementType.MakeArrayType().TypeHandle, pLengths[0]);
            }
            else
            {
                Type arrayType = elementType.MakeArrayType(rank);

                // Create a local copy of the lengths that cannot be modified by the caller
                int* pImmutableLengths = stackalloc int[rank];
                for (int i = 0; i < rank; i++)
                    pImmutableLengths[i] = pLengths[i];

                return NewMultiDimArray(arrayType.TypeHandle.ToMethodTable(), pImmutableLengths, rank);
            }
        }

        [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
            Justification = "The compiler ensures that if we have a TypeHandle of a Rank-1 MdArray, we also generated the SzArray.")]
        private static unsafe Array InternalCreateFromArrayType(RuntimeType arrayType, int rank, int* pLengths, int* pLowerBounds)
        {
            Debug.Assert(arrayType.IsArray);
            Debug.Assert(arrayType.GetArrayRank() == rank);

            if (arrayType.ContainsGenericParameters)
                throw new NotSupportedException(SR.NotSupported_OpenType);

            if (pLowerBounds != null)
            {
                for (int i = 0; i < rank; i++)
                {
                    if (pLowerBounds[i] != 0)
                        throw new PlatformNotSupportedException(SR.PlatformNotSupported_NonZeroLowerBound);
                }
            }

            if (rank == 1)
            {
                // Multidimensional array of rank 1 with 0 lower bounds gets actually allocated
                // as an SzArray. SzArray is castable to MdArray rank 1.
                RuntimeTypeHandle arrayTypeHandle = arrayType.IsSZArray
                    ? arrayType.TypeHandle
                    : arrayType.GetElementType().MakeArrayType().TypeHandle;

                return RuntimeAugments.NewArray(arrayTypeHandle, pLengths[0]);
            }
            else
            {
                // Create a local copy of the lengths that cannot be modified by the caller
                int* pImmutableLengths = stackalloc int[rank];
                for (int i = 0; i < rank; i++)
                    pImmutableLengths[i] = pLengths[i];

                MethodTable* eeType = arrayType.TypeHandle.ToMethodTable();
                return NewMultiDimArray(eeType, pImmutableLengths, rank);
            }
        }

        public unsafe void Initialize()
        {
            MethodTable* pElementEEType = ElementMethodTable;
            if (!pElementEEType->IsValueType)
                return;

            IntPtr constructorEntryPoint = RuntimeAugments.TypeLoaderCallbacks.TryGetDefaultConstructorForType(new RuntimeTypeHandle(pElementEEType));
            if (constructorEntryPoint == IntPtr.Zero)
                return;

            IntPtr constructorFtn = RuntimeAugments.TypeLoaderCallbacks.ConvertUnboxingFunctionPointerToUnderlyingNonUnboxingPointer(constructorEntryPoint, new RuntimeTypeHandle(pElementEEType));

            ref byte arrayRef = ref MemoryMarshal.GetArrayDataReference(this);
            nuint elementSize = ElementSize;

            for (nuint i = 0; i < NativeLength; i++)
            {
                RawCalliHelper.CallDefaultStructConstructor(constructorFtn, ref arrayRef);
                arrayRef = ref Unsafe.Add(ref arrayRef, elementSize);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe ref int GetMultiDimensionalArrayBounds()
        {
            Debug.Assert(!this.GetMethodTable()->IsSzArray);
            return ref Unsafe.As<byte, int>(ref Unsafe.As<RawArrayData>(this).Data);
        }

        private unsafe int GetMultiDimensionalArrayRank()
        {
            return this.GetMethodTable()->MultiDimensionalArrayRank;
        }

        private static bool SupportsNonZeroLowerBound => false;

        private static unsafe ArrayAssignType CanAssignArrayType(Array sourceArray, Array destinationArray)
        {
            MethodTable* sourceElementEEType = sourceArray.ElementMethodTable;
            MethodTable* destinationElementEEType = destinationArray.ElementMethodTable;

            if (sourceElementEEType == destinationElementEEType) // This check kicks for different array kind or dimensions
                return ArrayAssignType.SimpleCopy;

            if (sourceElementEEType->IsPointer || sourceElementEEType->IsFunctionPointer
                || destinationElementEEType->IsPointer || destinationElementEEType->IsFunctionPointer)
            {
                // Compatible pointers
                if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                    return ArrayAssignType.SimpleCopy;
                else
                    return ArrayAssignType.WrongType;
            }

            // Value class boxing
            if (sourceElementEEType->IsValueType && !destinationElementEEType->IsValueType)
            {
                if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                    return ArrayAssignType.BoxValueClassOrPrimitive;
                else
                    return ArrayAssignType.WrongType;
            }

            // Value class unboxing.
            if (!sourceElementEEType->IsValueType && destinationElementEEType->IsValueType)
            {
                if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                    return ArrayAssignType.UnboxValueClass;
                else if (RuntimeImports.AreTypesAssignable(destinationElementEEType, sourceElementEEType))   // V extends IV. Copying from IV to V, or Object to V.
                    return ArrayAssignType.UnboxValueClass;
                else
                    return ArrayAssignType.WrongType;
            }

            // Copying primitives from one type to another
            if (sourceElementEEType->IsPrimitive && destinationElementEEType->IsPrimitive)
            {
                EETypeElementType sourceElementType = sourceElementEEType->ElementType;
                EETypeElementType destElementType = destinationElementEEType->ElementType;

                if (GetNormalizedIntegralArrayElementType(sourceElementType) == GetNormalizedIntegralArrayElementType(destElementType))
                    return ArrayAssignType.SimpleCopy;
                else if (InvokeUtils.CanPrimitiveWiden(destElementType, sourceElementType))
                    return ArrayAssignType.PrimitiveWiden;
                else
                    return ArrayAssignType.WrongType;
            }

            // Different value types
            if (sourceElementEEType->IsValueType && destinationElementEEType->IsValueType)
            {
                // Different from CanCastTo in coreclr, AreTypesAssignable also allows T -> Nullable<T> conversion.
                // Kick for this path explicitly.
                return ArrayAssignType.WrongType;
            }

            // src Object extends dest
            if (RuntimeImports.AreTypesAssignable(sourceElementEEType, destinationElementEEType))
                return ArrayAssignType.SimpleCopy;

            // dest Object extends src
            if (RuntimeImports.AreTypesAssignable(destinationElementEEType, sourceElementEEType))
                return ArrayAssignType.MustCast;

            // class X extends/implements src and implements dest.
            if (destinationElementEEType->IsInterface)
                return ArrayAssignType.MustCast;

            // class X implements src and extends/implements dest
            if (sourceElementEEType->IsInterface)
                return ArrayAssignType.MustCast;

            return ArrayAssignType.WrongType;
        }

        private static EETypeElementType GetNormalizedIntegralArrayElementType(EETypeElementType elementType)
        {
            Debug.Assert(elementType >= EETypeElementType.Boolean && elementType <= EETypeElementType.Double);

            // Array Primitive types such as E_T_I4 and E_T_U4 are interchangeable
            // Enums with interchangeable underlying types are interchangeable
            // BOOL is NOT interchangeable with I1/U1, neither CHAR -- with I2/U2

            // U1/U2/U4/U8/U
            int shift = (0b0010_1010_1010_0000 >> (int)elementType) & 1;
            return (EETypeElementType)((int)elementType - shift);
        }

        public unsafe int Rank
        {
            get
            {
                return this.GetMethodTable()->ArrayRank;
            }
        }

        // Allocate new multidimensional array of given dimensions. Assumes that pLengths is immutable.
        internal static unsafe Array NewMultiDimArray(MethodTable* eeType, int* pLengths, int rank)
        {
            Debug.Assert(eeType->IsArray && !eeType->IsSzArray);
            Debug.Assert(rank == eeType->ArrayRank);

            // Code below assumes 0 lower bounds. MdArray of rank 1 with zero lower bounds should never be allocated.
            // The runtime always allocates an SzArray for those:
            // * newobj instance void int32[0...]::.ctor(int32)" actually gives you int[]
            // * int[] is castable to int[*] to make it mostly transparent
            // The callers need to check for this.
            Debug.Assert(rank != 1);

            ulong totalLength = 1;
            bool maxArrayDimensionLengthOverflow = false;

            for (int i = 0; i < rank; i++)
            {
                int length = pLengths[i];
                if (length < 0)
                    throw new OverflowException();
                if (length > MaxLength)
                    maxArrayDimensionLengthOverflow = true;
                totalLength *= (ulong)length;
                if (totalLength > int.MaxValue)
                    throw new OutOfMemoryException(); // "Array dimensions exceeded supported range."
            }

            // Throw this exception only after everything else was validated for backward compatibility.
            if (maxArrayDimensionLengthOverflow)
                throw new OutOfMemoryException(); // "Array dimensions exceeded supported range."

            Debug.Assert(eeType->NumVtableSlots != 0, "Compiler enforces we never have unconstructed MTs for multi-dim arrays since those can be template-constructed anytime");
            Array ret = RuntimeImports.RhNewVariableSizeObject(eeType, (int)totalLength);

            ref int bounds = ref ret.GetMultiDimensionalArrayBounds();
            for (int i = 0; i < rank; i++)
            {
                Unsafe.Add(ref bounds, i) = pLengths[i];
            }

            return ret;
        }

        internal unsafe object? InternalGetValue(nint flattenedIndex)
        {
            Debug.Assert((nuint)flattenedIndex < NativeLength);

            if (ElementMethodTable->IsPointer || ElementMethodTable->IsFunctionPointer)
                throw new NotSupportedException(SR.NotSupported_Type);

            ref byte element = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(this), (nuint)flattenedIndex * ElementSize);

            MethodTable* pElementEEType = ElementMethodTable;
            if (pElementEEType->IsValueType)
            {
                return RuntimeExports.RhBox(pElementEEType, ref element);
            }
            else
            {
                Debug.Assert(!pElementEEType->IsPointer && !pElementEEType->IsFunctionPointer);
                return Unsafe.As<byte, object>(ref element);
            }
        }

        private unsafe void InternalSetValue(object? value, nint flattenedIndex)
        {
            Debug.Assert((nuint)flattenedIndex < NativeLength);

            ref byte element = ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(this), (nuint)flattenedIndex * ElementSize);

            MethodTable* pElementEEType = ElementMethodTable;
            if (pElementEEType->IsValueType)
            {
                // Unlike most callers of InvokeUtils.ChangeType(), Array.SetValue() does *not* permit conversion from a primitive to an Enum.
                if (value != null && !(value.GetMethodTable() == pElementEEType) && pElementEEType->IsEnum)
                    throw new InvalidCastException(SR.Format(SR.Arg_ObjObjEx, value.GetType(), Type.GetTypeFromHandle(new RuntimeTypeHandle(pElementEEType))));

                value = InvokeUtils.CheckArgument(value, pElementEEType, InvokeUtils.CheckArgumentSemantics.ArraySet, binderBundle: null);
                Debug.Assert(value == null || RuntimeImports.AreTypesAssignable(value.GetMethodTable(), pElementEEType));

                RuntimeImports.RhUnbox(value, ref element, pElementEEType);
            }
            else if (pElementEEType->IsPointer || pElementEEType->IsFunctionPointer)
            {
                throw new NotSupportedException(SR.NotSupported_Type);
            }
            else
            {
                try
                {
                    RuntimeImports.RhCheckArrayStore(this, value);
                }
                catch (ArrayTypeMismatchException)
                {
                    throw new InvalidCastException(SR.InvalidCast_StoreArrayElement);
                }
                Unsafe.As<byte, object?>(ref element) = value;
            }
        }

        internal unsafe MethodTable* ElementMethodTable => this.GetMethodTable()->RelatedParameterType;

        internal unsafe CorElementType GetCorElementTypeOfElementType()
        {
            return new EETypePtr(ElementMethodTable).CorElementType;
        }

        internal unsafe bool IsValueOfElementType(object o)
        {
            return ElementMethodTable == o.GetMethodTable();
        }

        //
        // Return storage size of an individual element in bytes.
        //
        internal unsafe nuint ElementSize
        {
            get
            {
                return this.GetMethodTable()->ComponentSize;
            }
        }

        private static int IndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            // See comment in EqualityComparerHelpers.GetComparerForReferenceTypesOnly for details
            EqualityComparer<T> comparer = EqualityComparerHelpers.GetComparerForReferenceTypesOnly<T>();

            int endIndex = startIndex + count;
            if (comparer != null)
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (comparer.Equals(array[i], value))
                        return i;
                }
            }
            else
            {
                for (int i = startIndex; i < endIndex; i++)
                {
                    if (EqualityComparerHelpers.StructOnlyEquals<T>(array[i], value))
                        return i;
                }
            }

            return -1;
        }

        private static int LastIndexOfImpl<T>(T[] array, T value, int startIndex, int count)
        {
            // See comment in EqualityComparerHelpers.GetComparerForReferenceTypesOnly for details
            EqualityComparer<T> comparer = EqualityComparerHelpers.GetComparerForReferenceTypesOnly<T>();

            int endIndex = startIndex - count + 1;
            if (comparer != null)
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (comparer.Equals(array[i], value))
                        return i;
                }
            }
            else
            {
                for (int i = startIndex; i >= endIndex; i--)
                {
                    if (EqualityComparerHelpers.StructOnlyEquals<T>(array[i], value))
                        return i;
                }
            }

            return -1;
        }
    }

    //
    // Note: the declared base type and interface list also determines what Reflection returns from TypeInfo.BaseType and TypeInfo.ImplementedInterfaces for array types.
    //
    internal class Array<T> : Array, IEnumerable<T>, ICollection<T>, IList<T>, IReadOnlyList<T>
    {
        // Prevent the C# compiler from generating a public default constructor
        private Array() { }

        [Intrinsic]
        public new IEnumerator<T> GetEnumerator()
        {
            T[] @this = Unsafe.As<T[]>(this);
            // get length so we don't have to call the Length property again in ArrayEnumerator constructor
            // and avoid more checking there too.
            int length = @this.Length;
            return length == 0 ? SZGenericArrayEnumerator<T>.Empty : new SZGenericArrayEnumerator<T>(@this, length);
        }

        public int Count
        {
            get
            {
                return Unsafe.As<T[]>(this).Length;
            }
        }

        //
        // Fun fact:
        //
        //  ((int[])a).IsReadOnly returns false.
        //  ((IList<int>)a).IsReadOnly returns true.
        //
        public new bool IsReadOnly
        {
            get
            {
                return true;
            }
        }

        public void Add(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public void Clear()
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public bool Contains(T item)
        {
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, item, 0, @this.Length) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            T[] @this = Unsafe.As<T[]>(this);
            Array.Copy(@this, 0, array, arrayIndex, @this.Length);
        }

        public bool Remove(T item)
        {
            ThrowHelper.ThrowNotSupportedException();
            return false; // unreachable
        }

        public T this[int index]
        {
            get
            {
                try
                {
                    return Unsafe.As<T[]>(this)[index];
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                    return default; // unreachable
                }
            }
            set
            {
                try
                {
                    Unsafe.As<T[]>(this)[index] = value;
                }
                catch (IndexOutOfRangeException)
                {
                    ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessException();
                }
            }
        }

        public int IndexOf(T item)
        {
            T[] @this = Unsafe.As<T[]>(this);
            return Array.IndexOf(@this, item, 0, @this.Length);
        }

        public void Insert(int index, T item)
        {
            ThrowHelper.ThrowNotSupportedException();
        }

        public void RemoveAt(int index)
        {
            ThrowHelper.ThrowNotSupportedException();
        }
    }

    public class MDArray
    {
        public const int MinRank = 1;
        public const int MaxRank = 32;
    }
}
