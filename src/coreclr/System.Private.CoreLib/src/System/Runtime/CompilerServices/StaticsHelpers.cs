// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
    [StackTraceHidden]
    [DebuggerStepThrough]
    internal static unsafe partial class StaticsHelpers
    {
        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void GetThreadStaticsByIndex(ByteRefOnStack result, int index, [MarshalAs(UnmanagedType.Bool)] bool gcStatics);

        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void GetThreadStaticsByMethodTable(ByteRefOnStack result, MethodTable* pMT, [MarshalAs(UnmanagedType.Bool)] bool gcStatics);

        [Intrinsic]
        private static ref byte VolatileReadAsByref(ref IntPtr address) => ref VolatileReadAsByref(ref address);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref DynamicStaticsInfo.MaskStaticsPointer(ref VolatileReadAsByref(ref mt->AuxiliaryData->GetDynamicStaticsInfo()._pNonGCStatics));
        }

        [DebuggerHidden]
        private static ref byte GetNonGCStaticBase(MethodTable* mt)
        {
            ref byte nonGCStaticBase = ref VolatileReadAsByref(ref mt->AuxiliaryData->GetDynamicStaticsInfo()._pNonGCStatics);

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSNOTINITED) != 0)
                return ref GetNonGCStaticBaseSlow(mt);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicNonGCStaticBase(DynamicStaticsInfo* dynamicStaticsInfo)
        {
            ref byte nonGCStaticBase = ref VolatileReadAsByref(ref dynamicStaticsInfo->_pNonGCStatics);

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSNOTINITED) != 0)
                return ref GetNonGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref DynamicStaticsInfo.MaskStaticsPointer(ref VolatileReadAsByref(ref mt->AuxiliaryData->GetDynamicStaticsInfo()._pGCStatics));
        }

        [DebuggerHidden]
        private static ref byte GetGCStaticBase(MethodTable* mt)
        {
            ref byte gcStaticBase = ref VolatileReadAsByref(ref mt->AuxiliaryData->GetDynamicStaticsInfo()._pGCStatics);

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSNOTINITED) != 0)
                return ref GetGCStaticBaseSlow(mt);
            else
                return ref gcStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicGCStaticBase(DynamicStaticsInfo* dynamicStaticsInfo)
        {
            ref byte gcStaticBase = ref VolatileReadAsByref(ref dynamicStaticsInfo->_pGCStatics);

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSNOTINITED) != 0)
                return ref GetGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref gcStaticBase;
        }

        // Thread static helpers

        /// <summary>
        /// Return beginning of the object as a reference to byte
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetObjectAsRefByte(object obj)
        {
            return ref Unsafe.Subtract(ref RuntimeHelpers.GetRawData(obj), sizeof(MethodTable*));
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct ThreadLocalData
        {
            internal const int NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY = 2;
            internal int _cNonCollectibleTlsData; // Size of offset into the non-collectible TLS array which is valid, NOTE: this is relative to the start of the nonCollectibleTlsArrayData object, not the start of the data in the array
            internal int _cCollectibleTlsData; // Size of offset into the TLS array which is valid
            private IntPtr _nonCollectibleTlsArrayData_private; // This is object[], but using object[] directly causes the structure to be laid out via auto-layout, which is not what we want.
            internal IntPtr* _collectibleTlsArrayData; // Points at the Thread local array data.

            internal object[] NonCollectibleTlsArrayData
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return Unsafe.As<IntPtr, object[]>(ref _nonCollectibleTlsArrayData_private);
                }
            }
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndexOffset(int index)
        {
            return index & 0xFFFFFF;
        }

        private const int NonCollectibleTLSIndexType = 0;
        private const int DirectOnThreadLocalDataTLSIndexType = 2;

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetIndexType(int index)
        {
            return index >> 24;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsIndexAllocated(int index)
        {
            return index != -1;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCThreadStaticsByIndexSlow(int index)
        {
            ByteRef result = default;
            GetThreadStaticsByIndex(ByteRefOnStack.Create(ref result), index, false);
            return ref result.Get();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCThreadStaticsByIndexSlow(int index)
        {
            ByteRef result = default;
            GetThreadStaticsByIndex(ByteRefOnStack.Create(ref result), index, true);
            return ref result.Get();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCThreadStaticBaseSlow(MethodTable* mt)
        {
            ByteRef result = default;
            GetThreadStaticsByMethodTable(ByteRefOnStack.Create(ref result), mt, false);
            return ref result.Get();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCThreadStaticBaseSlow(MethodTable* mt)
        {
            ByteRef result = default;
            GetThreadStaticsByMethodTable(ByteRefOnStack.Create(ref result), mt, true);
            return ref result.Get();
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetThreadLocalStaticBaseByIndex(int index, bool gcStatics)
        {
            ThreadLocalData* t_ThreadStatics = System.Threading.Thread.GetThreadStaticsBase();
            int indexOffset = GetIndexOffset(index);
            if (GetIndexType(index) == NonCollectibleTLSIndexType)
            {
                if (t_ThreadStatics->_cNonCollectibleTlsData > GetIndexOffset(index))
                {
                    object? threadStaticObjectNonCollectible = Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(t_ThreadStatics->NonCollectibleTlsArrayData), indexOffset - ThreadLocalData.NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY);
                    if (threadStaticObjectNonCollectible != null)
                    {
                        return ref GetObjectAsRefByte(threadStaticObjectNonCollectible);
                    }
                }
            }
            else if (GetIndexType(index) == DirectOnThreadLocalDataTLSIndexType)
            {
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(t_ThreadStatics), indexOffset);
            }
            else
            {
                int cCollectibleTlsData = t_ThreadStatics->_cCollectibleTlsData;
                if (cCollectibleTlsData > indexOffset)
                {
                    IntPtr* pCollectibleTlsArrayData = t_ThreadStatics->_collectibleTlsArrayData;

                    pCollectibleTlsArrayData += indexOffset;
                    IntPtr objHandle = *pCollectibleTlsArrayData;
                    if (objHandle != IntPtr.Zero)
                    {
                        object? threadStaticObject = GCHandle.InternalGet(objHandle);
                        if (threadStaticObject != null)
                        {
                            return ref GetObjectAsRefByte(threadStaticObject);
                        }
                    }
                }
            }

            if (gcStatics)
                return ref GetGCThreadStaticsByIndexSlow(index);
            else
                return ref GetNonGCThreadStaticsByIndexSlow(index);
        }

        [DebuggerHidden]
        private static ref byte GetNonGCThreadStaticBase(MethodTable* mt)
        {
            int index = mt->AuxiliaryData->GetThreadStaticsInfo()._nonGCTlsIndex;
            if (IsIndexAllocated(index))
                return ref GetThreadLocalStaticBaseByIndex(index, false);
            else
                return ref GetNonGCThreadStaticBaseSlow(mt);
        }

        [DebuggerHidden]
        private static ref byte GetGCThreadStaticBase(MethodTable* mt)
        {
            int index = mt->AuxiliaryData->GetThreadStaticsInfo()._gcTlsIndex;
            if (IsIndexAllocated(index))
                return ref GetThreadLocalStaticBaseByIndex(index, true);
            else
                return ref GetGCThreadStaticBaseSlow(mt);
        }

        [DebuggerHidden]
        private static ref byte GetDynamicNonGCThreadStaticBase(ThreadStaticsInfo* threadStaticsInfo)
        {
            int index = threadStaticsInfo->_nonGCTlsIndex;
            if (IsIndexAllocated(index))
                return ref GetThreadLocalStaticBaseByIndex(index, false);
            else
                return ref GetNonGCThreadStaticBaseSlow(threadStaticsInfo->_genericStatics._dynamicStatics._methodTable);
        }

        [DebuggerHidden]
        private static ref byte GetDynamicGCThreadStaticBase(ThreadStaticsInfo* threadStaticsInfo)
        {
            int index = threadStaticsInfo->_gcTlsIndex;
            if (IsIndexAllocated(index))
                return ref GetThreadLocalStaticBaseByIndex(index, true);
            else
                return ref GetGCThreadStaticBaseSlow(threadStaticsInfo->_genericStatics._dynamicStatics._methodTable);
        }

        [DebuggerHidden]
        private static ref byte GetOptimizedNonGCThreadStaticBase(int index)
        {
            return ref GetThreadLocalStaticBaseByIndex(index, false);
        }

        [DebuggerHidden]
        private static ref byte GetOptimizedGCThreadStaticBase(int index)
        {
            return ref GetThreadLocalStaticBaseByIndex(index, true);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StaticFieldAddressArgs
        {
            public delegate*<IntPtr, ref byte> staticBaseHelper; // Function pointer to get the static base address
            public IntPtr arg0; // Argument to pass to the staticBaseHelper function
            public nint offset; // Offset from the static base address
        }

        [DebuggerHidden]
        private static unsafe ref byte StaticFieldAddress_Dynamic(StaticFieldAddressArgs* pArgs)
        {
            return ref Unsafe.Add(ref pArgs->staticBaseHelper(pArgs->arg0), pArgs->offset);
        }

        [DebuggerHidden]
        private static unsafe ref byte StaticFieldAddressUnbox_Dynamic(StaticFieldAddressArgs* pArgs)
        {
            object boxedObject = Unsafe.As<byte, object>(ref Unsafe.Add(ref pArgs->staticBaseHelper(pArgs->arg0), pArgs->offset));
            return ref boxedObject.GetRawData();
        }
    }
}
