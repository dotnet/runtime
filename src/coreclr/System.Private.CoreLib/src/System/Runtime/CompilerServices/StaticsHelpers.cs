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
        private static partial void GetThreadStaticsByIndex(RefHandleOnStack result, int index, [MarshalAs(UnmanagedType.Bool)] bool gcStatics);

        [LibraryImport(RuntimeHelpers.QCall)]
        private static partial void GetThreadStaticsByMethodTable(RefHandleOnStack result, MethodTable* pMT, [MarshalAs(UnmanagedType.Bool)] bool nonGC);

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref MethodTable.MaskStaticsPointer(ref mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics);
        }

        [DebuggerHidden]
        private static ref byte GetNonGCStaticBase(MethodTable* mt)
        {
            ref byte nonGCStaticBase = ref mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetNonGCStaticBaseSlow(mt);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicNonGCStaticBase(DynamicStaticsInfo *dynamicStaticsInfo)
        {
            ref byte nonGCStaticBase = ref dynamicStaticsInfo->_pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref nonGCStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetNonGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref nonGCStaticBase;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCStaticBaseSlow(MethodTable* mt)
        {
            InitHelpers.InitClassSlow(mt);
            return ref MethodTable.MaskStaticsPointer(ref mt->AuxiliaryData->DynamicStaticsInfo._pGCStatics);
        }

        [DebuggerHidden]
        private static ref byte GetGCStaticBase(MethodTable* mt)
        {
            ref byte gcStaticBase = ref mt->AuxiliaryData->DynamicStaticsInfo._pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetGCStaticBaseSlow(mt);
            else
                return ref gcStaticBase;
        }

        [DebuggerHidden]
        private static ref byte GetDynamicGCStaticBase(DynamicStaticsInfo *dynamicStaticsInfo)
        {
            ref byte gcStaticBase = ref dynamicStaticsInfo->_pNonGCStatics;

            if ((((nuint)Unsafe.AsPointer(ref gcStaticBase)) & DynamicStaticsInfo.ISCLASSINITED) != 0)
                return ref GetGCStaticBaseSlow(dynamicStaticsInfo->_methodTable);
            else
                return ref gcStaticBase;
        }

        // Thread static helpers

        [StructLayout(LayoutKind.Sequential)]
        private sealed class RawData
        {
            public byte Data;
        }

        /// <summary>
        /// Return beginning of the object as a reference to byte
        /// </summary>
        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetObjectAsRefByte(object obj)
        {
            return ref Unsafe.Add(ref Unsafe.As<RawData>(obj).Data, -sizeof(MethodTable*));
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ThreadStatics
        {
            public const int NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY = 2;
            public int cNonCollectibleTlsData; // Size of offset into the non-collectible TLS array which is valid, NOTE: this is relative to the start of the pNonCollectibleTlsArrayData object, not the start of the data in the array
            public int cCollectibleTlsData; // Size of offset into the TLS array which is valid
            public object[] pNonCollectibleTlsArrayData;
            public IntPtr* pCollectibleTlsArrayData; // Points at the Thread local array data.
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
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCThreadStaticsByIndexSlow(int index)
        {
            RefHandle<byte> result = default;
            GetThreadStaticsByIndex(RefHandleOnStack.Create(ref result), index, false);
            return ref result.Reference;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCThreadStaticsByIndexSlow(int index)
        {
            RefHandle<byte> result = default;
            GetThreadStaticsByIndex(RefHandleOnStack.Create(ref result), index, true);
            return ref result.Reference;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetNonGCThreadStaticBaseSlow(MethodTable* mt)
        {
            RefHandle<byte> result = default;
            GetThreadStaticsByMethodTable(RefHandleOnStack.Create(ref result), mt, false);
            return ref result.Reference;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ref byte GetGCThreadStaticBaseSlow(MethodTable* mt)
        {
            RefHandle<byte> result = default;
            GetThreadStaticsByMethodTable(RefHandleOnStack.Create(ref result), mt, false);
            return ref result.Reference;
        }

        [DebuggerHidden]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref byte GetThreadLocalStaticBaseByIndex(int index, bool gcStatics)
        {
            ThreadStatics *t_ThreadStatics = (ThreadStatics*)System.Threading.Thread.GetThreadStaticsBase();
            int indexOffset = GetIndexOffset(index);
            if (GetIndexType(index) == NonCollectibleTLSIndexType)
            {
                if (t_ThreadStatics->cNonCollectibleTlsData > GetIndexOffset(index))
                {
                    return ref GetObjectAsRefByte(Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(t_ThreadStatics->pNonCollectibleTlsArrayData), indexOffset - ThreadStatics.NUMBER_OF_TLSOFFSETS_NOT_USED_IN_NONCOLLECTIBLE_ARRAY));
                }
            }
            else if (GetIndexType(index) == DirectOnThreadLocalDataTLSIndexType)
            {
                return ref Unsafe.Add(ref Unsafe.AsRef<byte>(t_ThreadStatics), indexOffset);
            }
            else
            {
                int cCollectibleTlsData = t_ThreadStatics->cCollectibleTlsData;
                if (cCollectibleTlsData > indexOffset)
                {
                    IntPtr* pCollectibleTlsArrayData = t_ThreadStatics->pCollectibleTlsArrayData;

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
            int index = mt->AuxiliaryData->ThreadStaticsInfo.NonGCTlsIndex;
            if (index != 0)
                return ref GetThreadLocalStaticBaseByIndex(index, false);
            else
                return ref GetNonGCThreadStaticBaseSlow(mt);
        }

        [DebuggerHidden]
        private static ref byte GetGCThreadStaticBase(MethodTable* mt)
        {
            int index = mt->AuxiliaryData->ThreadStaticsInfo.NonGCTlsIndex;
            if (index != 0)
                return ref GetThreadLocalStaticBaseByIndex(index, true);
            else
                return ref GetGCThreadStaticBaseSlow(mt);
        }

        [DebuggerHidden]
        private static ref byte GetDynamicNonGCThreadStaticBase(ThreadStaticsInfo *threadStaticsInfo)
        {
            int index = threadStaticsInfo->NonGCTlsIndex;
            if (index != 0)
                return ref GetThreadLocalStaticBaseByIndex(index, false);
            else
                return ref GetNonGCThreadStaticBaseSlow(threadStaticsInfo->_genericStatics._dynamicStatics._methodTable);
        }

        [DebuggerHidden]
        private static ref byte GetDynamicGCThreadStaticBase(ThreadStaticsInfo *threadStaticsInfo)
        {
            int index = threadStaticsInfo->GCTlsIndex;
            if (index != 0)
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
    }
}
