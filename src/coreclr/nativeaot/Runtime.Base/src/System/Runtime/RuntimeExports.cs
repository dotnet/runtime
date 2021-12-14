// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This is where we group together all the runtime export calls.
//

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Internal.Runtime;
using Internal.Runtime.CompilerServices;

namespace System.Runtime
{
    internal static class RuntimeExports
    {
        //
        // internal calls for allocation
        //
        [RuntimeExport("RhNewObject")]
        public static unsafe object RhNewObject(MethodTable* pEEType)
        {
            // This is structured in a funny way because at the present state of things in CoreRT, the Debug.Assert
            // below will call into the assert defined in the class library (and not the MRT version of it). The one
            // in the class library is not low level enough to be callable when GC statics are not initialized yet.
            // Feel free to restructure once that's not a problem.
#if DEBUG
            bool isValid = !pEEType->IsGenericTypeDefinition &&
                !pEEType->IsInterface &&
                !pEEType->IsArray &&
                !pEEType->IsString &&
                !pEEType->IsByRefLike;
            if (!isValid)
                Debug.Assert(false);
#endif

#if FEATURE_64BIT_ALIGNMENT
            if (pEEType->RequiresAlign8)
            {
                if (pEEType->IsValueType)
                    return InternalCalls.RhpNewFastMisalign(pEEType);
                if (pEEType->IsFinalizable)
                    return InternalCalls.RhpNewFinalizableAlign8(pEEType);
                return InternalCalls.RhpNewFastAlign8(pEEType);
            }
            else
#endif // FEATURE_64BIT_ALIGNMENT
            {
                if (pEEType->IsFinalizable)
                    return InternalCalls.RhpNewFinalizable(pEEType);
                return InternalCalls.RhpNewFast(pEEType);
            }
        }

        [RuntimeExport("RhNewArray")]
        public static unsafe object RhNewArray(MethodTable* pEEType, int length)
        {
            Debug.Assert(pEEType->IsArray || pEEType->IsString);

#if FEATURE_64BIT_ALIGNMENT
            if (pEEType->RequiresAlign8)
            {
                return InternalCalls.RhpNewArrayAlign8(pEEType, length);
            }
            else
#endif // FEATURE_64BIT_ALIGNMENT
            {
                return InternalCalls.RhpNewArray(pEEType, length);
            }
        }

        [RuntimeExport("RhBox")]
        public static unsafe object RhBox(MethodTable* pEEType, ref byte data)
        {
            ref byte dataAdjustedForNullable = ref data;

            // Can box value types only (which also implies no finalizers).
            Debug.Assert(pEEType->IsValueType && !pEEType->IsFinalizable);

            // If we're boxing a Nullable<T> then either box the underlying T or return null (if the
            // nullable's value is empty).
            if (pEEType->IsNullable)
            {
                // The boolean which indicates whether the value is null comes first in the Nullable struct.
                if (data == 0)
                    return null;

                // Switch type we're going to box to the Nullable<T> target type and advance the data pointer
                // to the value embedded within the nullable.
                dataAdjustedForNullable = ref Unsafe.Add(ref data, pEEType->NullableValueOffset);
                pEEType = pEEType->NullableType;
            }

            object result;
#if FEATURE_64BIT_ALIGNMENT
            if (pEEType->RequiresAlign8)
            {
                result = InternalCalls.RhpNewFastMisalign(pEEType);
            }
            else
#endif // FEATURE_64BIT_ALIGNMENT
            {
                result = InternalCalls.RhpNewFast(pEEType);
            }

            // Copy the unboxed value type data into the new object.
            // Perform any write barriers necessary for embedded reference fields.
            if (pEEType->HasGCPointers)
            {
                InternalCalls.RhBulkMoveWithWriteBarrier(ref result.GetRawData(), ref dataAdjustedForNullable, pEEType->ValueTypeSize);
            }
            else
            {
                fixed (byte* pFields = &result.GetRawData())
                fixed (byte* pData = &dataAdjustedForNullable)
                    InternalCalls.memmove(pFields, pData, pEEType->ValueTypeSize);
            }

            return result;
        }

        [RuntimeExport("RhBoxAny")]
        public static unsafe object RhBoxAny(ref byte data, MethodTable* pEEType)
        {
            if (pEEType->IsValueType)
            {
                return RhBox(pEEType, ref data);
            }
            else
            {
                return Unsafe.As<byte, object>(ref data);
            }
        }

        private static unsafe bool UnboxAnyTypeCompare(MethodTable* pEEType, MethodTable* ptrUnboxToEEType)
        {
            if (TypeCast.AreTypesEquivalent(pEEType, ptrUnboxToEEType))
                return true;

            if (pEEType->ElementType == ptrUnboxToEEType->ElementType)
            {
                // Enum's and primitive types should pass the UnboxAny exception cases
                // if they have an exactly matching cor element type.
                switch (ptrUnboxToEEType->ElementType)
                {
                    case EETypeElementType.Byte:
                    case EETypeElementType.SByte:
                    case EETypeElementType.Int16:
                    case EETypeElementType.UInt16:
                    case EETypeElementType.Int32:
                    case EETypeElementType.UInt32:
                    case EETypeElementType.Int64:
                    case EETypeElementType.UInt64:
                    case EETypeElementType.IntPtr:
                    case EETypeElementType.UIntPtr:
                        return true;
                }
            }

            return false;
        }

        [RuntimeExport("RhUnboxAny")]
        public static unsafe void RhUnboxAny(object? o, ref byte data, EETypePtr pUnboxToEEType)
        {
            MethodTable* ptrUnboxToEEType = (MethodTable*)pUnboxToEEType.ToPointer();
            if (ptrUnboxToEEType->IsValueType)
            {
                bool isValid = false;

                if (ptrUnboxToEEType->IsNullable)
                {
                    isValid = (o == null) || TypeCast.AreTypesEquivalent(o.MethodTable, ptrUnboxToEEType->NullableType);
                }
                else
                {
                    isValid = (o != null) && UnboxAnyTypeCompare(o.MethodTable, ptrUnboxToEEType);
                }

                if (!isValid)
                {
                    // Throw the invalid cast exception defined by the classlib, using the input unbox MethodTable*
                    // to find the correct classlib.

                    ExceptionIDs exID = o == null ? ExceptionIDs.NullReference : ExceptionIDs.InvalidCast;

                    throw ptrUnboxToEEType->GetClasslibException(exID);
                }

                RhUnbox(o, ref data, ptrUnboxToEEType);
            }
            else
            {
                if (o != null && (TypeCast.IsInstanceOf(ptrUnboxToEEType, o) == null))
                {
                    throw ptrUnboxToEEType->GetClasslibException(ExceptionIDs.InvalidCast);
                }

                Unsafe.As<byte, object?>(ref data) = o;
            }
        }

        //
        // Unbox helpers with RyuJIT conventions
        //
        [RuntimeExport("RhUnbox2")]
        public static unsafe ref byte RhUnbox2(MethodTable* pUnboxToEEType, object obj)
        {
            if ((obj == null) || !UnboxAnyTypeCompare(obj.MethodTable, pUnboxToEEType))
            {
                ExceptionIDs exID = obj == null ? ExceptionIDs.NullReference : ExceptionIDs.InvalidCast;
                throw pUnboxToEEType->GetClasslibException(exID);
            }
            return ref obj.GetRawData();
        }

        [RuntimeExport("RhUnboxNullable")]
        public static unsafe void RhUnboxNullable(ref byte data, MethodTable* pUnboxToEEType, object obj)
        {
            if ((obj != null) && !TypeCast.AreTypesEquivalent(obj.MethodTable, pUnboxToEEType->NullableType))
            {
                throw pUnboxToEEType->GetClasslibException(ExceptionIDs.InvalidCast);
            }
            RhUnbox(obj, ref data, pUnboxToEEType);
        }

        [RuntimeExport("RhUnbox")]
        public static unsafe void RhUnbox(object? obj, ref byte data, MethodTable* pUnboxToEEType)
        {
            // When unboxing to a Nullable the input object may be null.
            if (obj == null)
            {
                Debug.Assert(pUnboxToEEType != null && pUnboxToEEType->IsNullable);

                // Set HasValue to false and clear the value (in case there were GC references we wish to stop reporting).
                InternalCalls.RhpInitMultibyte(
                    ref data,
                    0,
                    pUnboxToEEType->ValueTypeSize);

                return;
            }

            MethodTable* pEEType = obj.MethodTable;

            // Can unbox value types only.
            Debug.Assert(pEEType->IsValueType);

            // A special case is that we can unbox a value type T into a Nullable<T>. It's the only case where
            // pUnboxToEEType is useful.
            Debug.Assert((pUnboxToEEType == null) || UnboxAnyTypeCompare(pEEType, pUnboxToEEType) || pUnboxToEEType->IsNullable);
            if (pUnboxToEEType != null && pUnboxToEEType->IsNullable)
            {
                Debug.Assert(pUnboxToEEType->NullableType->IsEquivalentTo(pEEType));

                // Set the first field of the Nullable to true to indicate the value is present.
                Unsafe.As<byte, bool>(ref data) = true;

                // Adjust the data pointer so that it points at the value field in the Nullable.
                data = ref Unsafe.Add(ref data, pUnboxToEEType->NullableValueOffset);
            }

            ref byte fields = ref obj.GetRawData();

            if (pEEType->HasGCPointers)
            {
                // Copy the boxed fields into the new location in a GC safe manner
                InternalCalls.RhBulkMoveWithWriteBarrier(ref data, ref fields, pEEType->ValueTypeSize);
            }
            else
            {
                // Copy the boxed fields into the new location.
                fixed (byte *pData = &data)
                    fixed (byte* pFields = &fields)
                        InternalCalls.memmove(pData, pFields, pEEType->ValueTypeSize);
            }
        }

        [RuntimeExport("RhMemberwiseClone")]
        public static unsafe object RhMemberwiseClone(object src)
        {
            object objClone;

            if (src.MethodTable->IsArray)
                objClone = RhNewArray(src.MethodTable, Unsafe.As<Array>(src).Length);
            else
                objClone = RhNewObject(src.MethodTable);

            InternalCalls.RhpCopyObjectContents(objClone, src);

            return objClone;
        }

        [RuntimeExport("RhpReversePInvokeBadTransition")]
        public static void RhpReversePInvokeBadTransition(IntPtr returnAddress)
        {
            EH.FailFastViaClasslib(
                RhFailFastReason.IllegalUnmanagedCallersOnlyEntry,
                null,
                returnAddress);
        }

        [RuntimeExport("RhGetCurrentThreadStackTrace")]
        [MethodImpl(MethodImplOptions.NoInlining)] // Ensures that the RhGetCurrentThreadStackTrace frame is always present
        public static unsafe int RhGetCurrentThreadStackTrace(IntPtr[] outputBuffer)
        {
            fixed (IntPtr* pOutputBuffer = outputBuffer)
                return RhpGetCurrentThreadStackTrace(pOutputBuffer, (uint)((outputBuffer != null) ? outputBuffer.Length : 0), new UIntPtr(&pOutputBuffer));
        }

        [DllImport(Redhawk.BaseName, CallingConvention = CallingConvention.Cdecl)]
        private static extern unsafe int RhpGetCurrentThreadStackTrace(IntPtr* pOutputBuffer, uint outputBufferLength, UIntPtr addressInCurrentFrame);

        // Worker for RhGetCurrentThreadStackTrace.  RhGetCurrentThreadStackTrace just allocates a transition
        // frame that will be used to seed the stack trace and this method does all the real work.
        //
        // Input:           outputBuffer may be null or non-null
        // Return value:    positive: number of entries written to outputBuffer
        //                  negative: number of required entries in outputBuffer in case it's too small (or null)
        // Output:          outputBuffer is filled in with return address IPs, starting with placing the this
        //                  method's return address into index 0
        //
        // NOTE: We don't want to allocate the array on behalf of the caller because we don't know which class
        // library's objects the caller understands (we support multiple class libraries with multiple root
        // System.Object types).
        [UnmanagedCallersOnly(EntryPoint = "RhpCalculateStackTraceWorker", CallConvs = new Type[] { typeof(CallConvCdecl) })]
        private static unsafe int RhpCalculateStackTraceWorker(IntPtr* pOutputBuffer, uint outputBufferLength, UIntPtr addressInCurrentFrame)
        {
            uint nFrames = 0;
            bool success = true;

            StackFrameIterator frameIter = default;

            bool isValid = frameIter.Init(null);
            Debug.Assert(isValid, "Missing RhGetCurrentThreadStackTrace frame");

            // Note that the while loop will skip RhGetCurrentThreadStackTrace frame
            while (frameIter.Next())
            {
                if ((void*)frameIter.SP < (void*)addressInCurrentFrame)
                    continue;

                if (nFrames < outputBufferLength)
                    pOutputBuffer[nFrames] = new IntPtr(frameIter.ControlPC);
                else
                    success = false;

                nFrames++;
            }

            return success ? (int)nFrames : -(int)nFrames;
        }

        // The GC conservative reporting descriptor is a special structure of data that the GC
        // parses to determine whether there are specific regions of memory that it should not
        // collect or move around.
        // During garbage collection, the GC will inspect the data in this structure, and verify that:
        //  1) _magic is set to the magic number (also hard coded on the GC side)
        //  2) The reported region is valid (checks alignments, size, within bounds of the thread memory, etc...)
        //  3) The ConservativelyReportedRegionDesc pointer must be reported by a frame which does not make a pinvoke transition.
        //  4) The value of the _hash field is the computed hash of _regionPointerLow with _regionPointerHigh
        //  5) The region must be IntPtr aligned, and have a size which is also IntPtr aligned
        // If all conditions are satisfied, the region of memory starting at _regionPointerLow and ending at
        // _regionPointerHigh will be conservatively reported.
        // This can only be used to report memory regions on the current stack and the structure must itself
        // be located on the stack.
        public struct ConservativelyReportedRegionDesc
        {
            internal const ulong MagicNumber64 = 0x87DF7A104F09E0A9UL;
            internal const uint MagicNumber32 = 0x4F09E0A9;

            internal UIntPtr _magic;
            internal UIntPtr _regionPointerLow;
            internal UIntPtr _regionPointerHigh;
            internal UIntPtr _hash;
        }

        [RuntimeExport("RhInitializeConservativeReportingRegion")]
        public static unsafe void RhInitializeConservativeReportingRegion(ConservativelyReportedRegionDesc* regionDesc, void* bufferBegin, int cbBuffer)
        {
            Debug.Assert((((int)bufferBegin) & (sizeof(IntPtr) - 1)) == 0, "Buffer not IntPtr aligned");
            Debug.Assert((cbBuffer & (sizeof(IntPtr) - 1)) == 0, "Size of buffer not IntPtr aligned");

            UIntPtr regionPointerLow = (UIntPtr)bufferBegin;
            UIntPtr regionPointerHigh = (UIntPtr)(((byte*)bufferBegin) + cbBuffer);

            // Setup pointers to start and end of region
            regionDesc->_regionPointerLow = regionPointerLow;
            regionDesc->_regionPointerHigh = regionPointerHigh;

            // Activate the region for processing
#if TARGET_64BIT
            ulong hash = ConservativelyReportedRegionDesc.MagicNumber64;
            hash = ((hash << 13) ^ hash) ^ (ulong)regionPointerLow;
            hash = ((hash << 13) ^ hash) ^ (ulong)regionPointerHigh;

            regionDesc->_hash = new UIntPtr(hash);
            regionDesc->_magic = new UIntPtr(ConservativelyReportedRegionDesc.MagicNumber64);
#else
            uint hash = ConservativelyReportedRegionDesc.MagicNumber32;
            hash = ((hash << 13) ^ hash) ^ (uint)regionPointerLow;
            hash = ((hash << 13) ^ hash) ^ (uint)regionPointerHigh;

            regionDesc->_hash = new UIntPtr(hash);
            regionDesc->_magic = new UIntPtr(ConservativelyReportedRegionDesc.MagicNumber32);
#endif
        }

        // Disable conservative reporting
        [RuntimeExport("RhDisableConservativeReportingRegion")]
        public static unsafe void RhDisableConservativeReportingRegion(ConservativelyReportedRegionDesc* regionDesc)
        {
            regionDesc->_magic = default(UIntPtr);
        }

        [RuntimeExport("RhCreateThunksHeap")]
        public static object RhCreateThunksHeap(IntPtr commonStubAddress)
        {
            return ThunksHeap.CreateThunksHeap(commonStubAddress);
        }

        [RuntimeExport("RhAllocateThunk")]
        public static IntPtr RhAllocateThunk(object thunksHeap)
        {
            return ((ThunksHeap)thunksHeap).AllocateThunk();
        }

        [RuntimeExport("RhFreeThunk")]
        public static void RhFreeThunk(object thunksHeap, IntPtr thunkAddress)
        {
            ((ThunksHeap)thunksHeap).FreeThunk(thunkAddress);
        }

        [RuntimeExport("RhSetThunkData")]
        public static void RhSetThunkData(object thunksHeap, IntPtr thunkAddress, IntPtr context, IntPtr target)
        {
            ((ThunksHeap)thunksHeap).SetThunkData(thunkAddress, context, target);
        }

        [RuntimeExport("RhTryGetThunkData")]
        public static bool RhTryGetThunkData(object thunksHeap, IntPtr thunkAddress, out IntPtr context, out IntPtr target)
        {
            return ((ThunksHeap)thunksHeap).TryGetThunkData(thunkAddress, out context, out target);
        }

        [RuntimeExport("RhGetThunkSize")]
        public static int RhGetThunkSize()
        {
            return InternalCalls.RhpGetThunkSize();
        }

        [RuntimeExport("RhGetRuntimeHelperForType")]
        internal static unsafe IntPtr RhGetRuntimeHelperForType(MethodTable* pEEType, RuntimeHelperKind kind)
        {
            switch (kind)
            {
                case RuntimeHelperKind.AllocateObject:
#if FEATURE_64BIT_ALIGNMENT
                    if (pEEType->RequiresAlign8)
                    {
                        if (pEEType->IsFinalizable)
                            return (IntPtr)(delegate*<MethodTable*, object>)&InternalCalls.RhpNewFinalizableAlign8;
                        else if (pEEType->IsValueType)            // returns true for enum types as well
                            return (IntPtr)(delegate*<MethodTable*, object>)&InternalCalls.RhpNewFastMisalign;
                        else
                            return (IntPtr)(delegate*<MethodTable*, object>)&InternalCalls.RhpNewFastAlign8;
                    }
#endif // FEATURE_64BIT_ALIGNMENT

                    if (pEEType->IsFinalizable)
                        return (IntPtr)(delegate*<MethodTable*, object>)&InternalCalls.RhpNewFinalizable;
                    else
                        return (IntPtr)(delegate*<MethodTable*, object>)&InternalCalls.RhpNewFast;

                case RuntimeHelperKind.IsInst:
                    if (pEEType->IsArray)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.IsInstanceOfArray;
                    else if (pEEType->IsInterface)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.IsInstanceOfInterface;
                    else if (pEEType->IsParameterizedType)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.IsInstanceOf; // Array handled above; pointers and byrefs handled here
                    else
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.IsInstanceOfClass;

                case RuntimeHelperKind.CastClass:
                    if (pEEType->IsArray)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.CheckCastArray;
                    else if (pEEType->IsInterface)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.CheckCastInterface;
                    else if (pEEType->IsParameterizedType)
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.CheckCast; // Array handled above; pointers and byrefs handled here
                    else
                        return (IntPtr)(delegate*<MethodTable*, object, object>)&TypeCast.CheckCastClass;

                case RuntimeHelperKind.AllocateArray:
#if FEATURE_64BIT_ALIGNMENT
                    if (pEEType->RequiresAlign8)
                        return (IntPtr)(delegate*<MethodTable*, int, object>)&InternalCalls.RhpNewArrayAlign8;
#endif // FEATURE_64BIT_ALIGNMENT

                    return (IntPtr)(delegate*<MethodTable*, int, object>)&InternalCalls.RhpNewArray;

                default:
                    Debug.Assert(false, "Unknown RuntimeHelperKind");
                    return IntPtr.Zero;
            }
        }
    }
}
