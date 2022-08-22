// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Data.OleDb
{
    internal sealed class RowBinding : System.Data.ProviderBase.DbBuffer
    {
        private readonly int _bindingCount;
        private readonly int _headerLength;
        private readonly int _dataLength;
        private readonly int _emptyStringOffset;

        private UnsafeNativeMethods.IAccessor? _iaccessor;
        private IntPtr _accessorHandle;

        private readonly bool _needToReset;
        private bool _haveData;

        // tagDBBINDING[] starting 64bit aligned
        // all DBSTATUS values (32bit per value), starting 64bit aligned
        // all DBLENGTH values (32/64bit per value), starting 64bit alignedsa
        // all data values listed after that (variable length), each individual starting 64bit aligned
        // Int64 - zero for pointers to emptystring

        internal static RowBinding CreateBuffer(int bindingCount, int databuffersize, bool needToReset)
        {
            int headerLength = RowBinding.AlignDataSize(bindingCount * ODB.SizeOf_tagDBBINDING);
            int length = RowBinding.AlignDataSize(headerLength + databuffersize) + 8; // 8 bytes for a null terminated string
            return new RowBinding(bindingCount, headerLength, databuffersize, length, needToReset);
        }

        private RowBinding(int bindingCount, int headerLength, int dataLength, int length, bool needToReset) : base(length)
        {
            _bindingCount = bindingCount;
            _headerLength = headerLength;
            _dataLength = dataLength;
            _emptyStringOffset = length - 8; // 8 bytes for a null terminated string
            _needToReset = needToReset;

            Debug.Assert(0 < _bindingCount, "bad _bindingCount");
            Debug.Assert(0 < _headerLength, "bad _headerLength");
            Debug.Assert(0 < _dataLength, "bad _dataLength");
            Debug.Assert(_bindingCount * 3 * IntPtr.Size <= _dataLength, "_dataLength too small");
            Debug.Assert(_headerLength + _dataLength <= _emptyStringOffset, "bad string offset");
            Debug.Assert(_headerLength + _dataLength + 8 <= length, "bad length");
        }

        internal void StartDataBlock()
        {
            if (_haveData)
            {
                Debug.Assert(false, "previous row not yet cleared");
                ResetValues();
            }
            _haveData = true;
        }

        internal int BindingCount()
        {
            return _bindingCount;
        }

        internal IntPtr DangerousGetAccessorHandle()
        {
            return _accessorHandle;
        }

        internal IntPtr DangerousGetDataPtr()
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // NOTE: You must have called DangerousAddRef before calling this
            //       method, or you run the risk of allowing Handle Recycling
            //       to occur!
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            return ADP.IntPtrOffset(DangerousGetHandle(), _headerLength);
        }

        internal IntPtr DangerousGetDataPtr(int valueOffset)
        {
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // NOTE: You must have called DangerousAddRef before calling this
            //       method, or you run the risk of allowing Handle Recycling
            //       to occur!
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            return ADP.IntPtrOffset(DangerousGetHandle(), valueOffset);
        }

        internal OleDbHResult CreateAccessor(UnsafeNativeMethods.IAccessor iaccessor, int flags, ColumnBinding[] bindings)
        {
            int[] rowBindStatus = new int[BindingCount()];

            _iaccessor = iaccessor;
            OleDbHResult hr = iaccessor.CreateAccessor(flags, (IntPtr)rowBindStatus.Length, this, (IntPtr)_dataLength, out _accessorHandle, rowBindStatus);

            for (int k = 0; k < rowBindStatus.Length; ++k)
            {
                if (DBBindStatus.OK != (DBBindStatus)rowBindStatus[k])
                {
                    if (ODB.DBACCESSOR_PARAMETERDATA == flags)
                    {
                        throw ODB.BadStatus_ParamAcc(bindings[k].ColumnBindingOrdinal, (DBBindStatus)rowBindStatus[k]);
                    }
                    else if (ODB.DBACCESSOR_ROWDATA == flags)
                    {
                        throw ODB.BadStatusRowAccessor(bindings[k].ColumnBindingOrdinal, (DBBindStatus)rowBindStatus[k]);
                    }
                    else
                        Debug.Assert(false, "unknown accessor buffer");
                }
            }
            return hr;
        }

        internal ColumnBinding[] SetBindings(OleDbDataReader? dataReader, Bindings bindings,
                                             int indexStart, int indexForAccessor,
                                             OleDbParameter[]? parameters, tagDBBINDING[] dbbindings, bool ifIRowsetElseIRow)
        {
            Debug.Assert(null != bindings, "null bindings");
            Debug.Assert(dbbindings.Length == BindingCount(), "count mismatch");

            bool mustRelease = false;

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr buffer = DangerousGetHandle();
                for (int i = 0; i < dbbindings.Length; ++i)
                {
                    IntPtr ptr = ADP.IntPtrOffset(buffer, (i * ODB.SizeOf_tagDBBINDING));
                    Marshal.StructureToPtr(dbbindings[i], ptr, false/*deleteold*/);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }

            ColumnBinding[] columns = new ColumnBinding[dbbindings.Length];
            for (int indexWithinAccessor = 0; indexWithinAccessor < columns.Length; ++indexWithinAccessor)
            {
                int index = indexStart + indexWithinAccessor;
                OleDbParameter? parameter = parameters?[index];
                columns[indexWithinAccessor] = new ColumnBinding(
                    dataReader!, index, indexForAccessor, indexWithinAccessor,
                    parameter, this, bindings, dbbindings[indexWithinAccessor], _headerLength,
                    ifIRowsetElseIRow);
            }
            return columns;
        }

        internal static int AlignDataSize(int value)
        {
            // buffer data to start on 8-byte boundary
            return Math.Max(8, (value + 7) & ~0x7);
        }

        internal object GetVariantValue(int offset)
        {
            Debug.Assert(_needToReset, "data type requires resetting and _needToReset is false");
            Debug.Assert(0 == (ODB.SizeOf_Variant % 8), "unexpected VARIANT size mutiplier");
            Debug.Assert(0 == offset % 8, "invalid alignment");
            ValidateCheck(offset, 2 * ODB.SizeOf_Variant);

            object? value = null;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr buffer = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                value = Marshal.GetObjectForNativeVariant(buffer);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }

            return value ?? DBNull.Value;
        }

        // translate to native
        internal void SetVariantValue(int offset, object value)
        {
            // two contiguous VARIANT structures, second should be a binary copy of the first
            Debug.Assert(_needToReset, "data type requires resetting and _needToReset is false");
            Debug.Assert(0 == (ODB.SizeOf_Variant % 8), "unexpected VARIANT size mutiplier");
            Debug.Assert(0 == offset % 8, "invalid alignment");
            ValidateCheck(offset, 2 * ODB.SizeOf_Variant);

            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr buffer = ADP.IntPtrOffset(DangerousGetHandle(), offset);

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    // GetNativeVariantForObject must be in try block since it has no reliability contract
                    Marshal.GetNativeVariantForObject(value, buffer);
                }
                finally
                {
                    // safe to copy memory(dst,src,count), even if GetNativeVariantForObject failed
                    NativeOledbWrapper.MemoryCopy(ADP.IntPtrOffset(buffer, ODB.SizeOf_Variant), buffer, ODB.SizeOf_Variant);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        // value
        // cached value
        // cached zero value
        // translate to native
        internal void SetBstrValue(int offset, string value)
        {
            // two contiguous BSTR ptr, second should be a binary copy of the first
            Debug.Assert(_needToReset, "data type requires resetting and _needToReset is false");
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            ValidateCheck(offset, 2 * IntPtr.Size);

            IntPtr ptr;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    ptr = Interop.OleAut32.SysAllocStringLen(value, (uint)value.Length);

                    // safe to copy ptr, even if SysAllocStringLen failed
                    Marshal.WriteIntPtr(base.handle, offset, ptr);
                    Marshal.WriteIntPtr(base.handle, offset + ADP.PtrSize, ptr);
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            if (IntPtr.Zero == ptr)
            {
                throw new OutOfMemoryException();
            }
        }

        // translate to native
        internal void SetByRefValue(int offset, IntPtr pinnedValue)
        {
            Debug.Assert(_needToReset, "data type requires resetting and _needToReset is false");
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");
            ValidateCheck(offset, 2 * IntPtr.Size);

            if (ADP.PtrZero == pinnedValue)
            { // empty array scenario
                pinnedValue = ADP.IntPtrOffset(base.handle, _emptyStringOffset);
            }
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                RuntimeHelpers.PrepareConstrainedRegions();
                try
                { }
                finally
                {
                    Marshal.WriteIntPtr(base.handle, offset, pinnedValue);               // parameter input value
                    Marshal.WriteIntPtr(base.handle, offset + ADP.PtrSize, pinnedValue); // original parameter value
                }
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
        }

        internal void CloseFromConnection()
        {
            _iaccessor = null;
            _accessorHandle = ODB.DB_INVALID_HACCESSOR;
        }

        internal new void Dispose()
        {
            ResetValues();

            UnsafeNativeMethods.IAccessor? iaccessor = _iaccessor;
            IntPtr accessorHandle = _accessorHandle;

            _iaccessor = null;
            _accessorHandle = ODB.DB_INVALID_HACCESSOR;

            if ((ODB.DB_INVALID_HACCESSOR != accessorHandle) && (null != iaccessor))
            {
                OleDbHResult hr;
                hr = iaccessor.ReleaseAccessor(accessorHandle, out _);
                if (hr < 0)
                { // ignore any error msgs
                    SafeNativeMethods.Wrapper.ClearErrorInfo();
                }
            }

            base.Dispose();
        }

        internal void ResetValues()
        {
            if (_needToReset && _haveData)
            {
                lock (this)
                { // prevent Dispose/ResetValues race condition

                    bool mustRelease = false;
                    RuntimeHelpers.PrepareConstrainedRegions();
                    try
                    {
                        DangerousAddRef(ref mustRelease);

                        ResetValues(DangerousGetHandle(), _iaccessor);
                    }
                    finally
                    {
                        if (mustRelease)
                        {
                            DangerousRelease();
                        }
                    }
                }
            }
            else
            {
                _haveData = false;
            }
#if DEBUG
            // verify types that need resetting are not forgotton, since the code
            // that sets this up is in dbbinding.cs, MaxLen { set; }
            if (!_needToReset)
            {
                Debug.Assert(0 <= _bindingCount && (_bindingCount * ODB.SizeOf_tagDBBINDING) < Length, "bad _bindingCount");
                for (int i = 0; i < _bindingCount; ++i)
                {
                    short wtype = ReadInt16((i * ODB.SizeOf_tagDBBINDING) + ODB.OffsetOf_tagDBBINDING_wType);
                    switch (wtype)
                    {
                        case (NativeDBType.BYREF | NativeDBType.BYTES):
                        case (NativeDBType.BYREF | NativeDBType.WSTR):
                        case NativeDBType.PROPVARIANT:
                        case NativeDBType.VARIANT:
                        case NativeDBType.BSTR:
                        case NativeDBType.HCHAPTER:
                            Debug.Assert(false, "expected _needToReset");
                            break;
                    }
                }
            }
#endif
        }

        private unsafe void ResetValues(IntPtr buffer, object? iaccessor)
        {
            Debug.Assert(ADP.PtrZero != buffer && _needToReset && _haveData, "shouldn't be calling ResetValues");
            for (int i = 0; i < _bindingCount; ++i)
            {
                IntPtr ptr = ADP.IntPtrOffset(buffer, (i * ODB.SizeOf_tagDBBINDING));

                int valueOffset = _headerLength + Marshal.ReadIntPtr(ptr, ODB.OffsetOf_tagDBBINDING_obValue).ToInt32();
                short wtype = Marshal.ReadInt16(ptr, ODB.OffsetOf_tagDBBINDING_wType);

                switch (wtype)
                {
                    case (NativeDBType.BYREF | NativeDBType.BYTES):
                    case (NativeDBType.BYREF | NativeDBType.WSTR):
                        ValidateCheck(valueOffset, 2 * IntPtr.Size);
                        FreeCoTaskMem(buffer, valueOffset);
                        break;
                    case NativeDBType.PROPVARIANT:
                        ValidateCheck(valueOffset, 2 * sizeof(PROPVARIANT));
                        FreePropVariant(buffer, valueOffset);
                        break;
                    case NativeDBType.VARIANT:
                        ValidateCheck(valueOffset, 2 * ODB.SizeOf_Variant);
                        FreeVariant(buffer, valueOffset);
                        break;
                    case NativeDBType.BSTR:
                        ValidateCheck(valueOffset, 2 * IntPtr.Size);
                        FreeBstr(buffer, valueOffset);
                        break;
                    case NativeDBType.HCHAPTER:
                        if (null != iaccessor)
                        {
                            // iaccessor will always be null when from ReleaseHandle
                            FreeChapter(buffer, valueOffset, iaccessor);
                        }
                        break;
#if DEBUG

                    case NativeDBType.EMPTY:
                    case NativeDBType.NULL:
                    case NativeDBType.I2:
                    case NativeDBType.I4:
                    case NativeDBType.R4:
                    case NativeDBType.R8:
                    case NativeDBType.CY:
                    case NativeDBType.DATE:
                    case NativeDBType.ERROR:
                    case NativeDBType.BOOL:
                    case NativeDBType.DECIMAL:
                    case NativeDBType.I1:
                    case NativeDBType.UI1:
                    case NativeDBType.UI2:
                    case NativeDBType.UI4:
                    case NativeDBType.I8:
                    case NativeDBType.UI8:
                    case NativeDBType.FILETIME:
                    case NativeDBType.GUID:
                    case NativeDBType.BYTES:
                    case NativeDBType.WSTR:
                    case NativeDBType.NUMERIC:
                    case NativeDBType.DBDATE:
                    case NativeDBType.DBTIME:
                    case NativeDBType.DBTIMESTAMP:
                        break; // known, do nothing
                    case NativeDBType.IDISPATCH:
                    case NativeDBType.IUNKNOWN:
                        break; // known, releasing RowHandle will handle lifetimes correctly
                    default:
                        Debug.Assert(false, "investigate");
                        break;
#endif
                }
            }
            _haveData = false;
        }

        private static void FreeChapter(IntPtr buffer, int valueOffset, object iaccessor)
        {
            Debug.Assert(0 == valueOffset % 8, "unexpected unaligned ptr offset");

            UnsafeNativeMethods.IChapteredRowset chapteredRowset = (iaccessor as UnsafeNativeMethods.IChapteredRowset)!;
            IntPtr chapter = SafeNativeMethods.InterlockedExchangePointer(ADP.IntPtrOffset(buffer, valueOffset), ADP.PtrZero);
            if (ODB.DB_NULL_HCHAPTER != chapter)
            {
                chapteredRowset.ReleaseChapter(chapter, out _);
            }
        }

        private static void FreeBstr(IntPtr buffer, int valueOffset)
        {
            Debug.Assert(0 == valueOffset % 8, "unexpected unaligned ptr offset");

            // two contiguous BSTR ptrs that need to be freed
            // the second should only be freed if different from the first
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                IntPtr currentValue = Marshal.ReadIntPtr(buffer, valueOffset);
                IntPtr originalValue = Marshal.ReadIntPtr(buffer, valueOffset + ADP.PtrSize);

                if ((ADP.PtrZero != currentValue) && (currentValue != originalValue))
                {
                    Interop.OleAut32.SysFreeString(currentValue);
                }
                if (ADP.PtrZero != originalValue)
                {
                    Interop.OleAut32.SysFreeString(originalValue);
                }

                // for debugability - delay clearing memory until after FreeBSTR
                Marshal.WriteIntPtr(buffer, valueOffset, ADP.PtrZero);
                Marshal.WriteIntPtr(buffer, valueOffset + ADP.PtrSize, ADP.PtrZero);
            }
        }

        private static void FreeCoTaskMem(IntPtr buffer, int valueOffset)
        {
            Debug.Assert(0 == valueOffset % 8, "unexpected unaligned ptr offset");

            // two contiguous CoTaskMemAlloc ptrs that need to be freed
            // the first should only be freed if different from the first
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                IntPtr currentValue = Marshal.ReadIntPtr(buffer, valueOffset);
                IntPtr originalValue = Marshal.ReadIntPtr(buffer, valueOffset + ADP.PtrSize);

                // originalValue is pinned managed memory or pointer to emptyStringOffset
                if ((ADP.PtrZero != currentValue) && (currentValue != originalValue))
                {
                    Interop.Ole32.CoTaskMemFree(currentValue);
                }

                // for debugability - delay clearing memory until after CoTaskMemFree
                Marshal.WriteIntPtr(buffer, valueOffset, ADP.PtrZero);
                Marshal.WriteIntPtr(buffer, valueOffset + ADP.PtrSize, ADP.PtrZero);
            }
        }

        private static void FreeVariant(IntPtr buffer, int valueOffset)
        {
            // two contiguous VARIANT structures that need to be freed
            // the second should only be freed if different from the first

            Debug.Assert(0 == (ODB.SizeOf_Variant % 8), "unexpected VARIANT size mutiplier");
            Debug.Assert(0 == valueOffset % 8, "unexpected unaligned ptr offset");

            IntPtr currentHandle = ADP.IntPtrOffset(buffer, valueOffset);
            IntPtr originalHandle = ADP.IntPtrOffset(buffer, valueOffset + ODB.SizeOf_Variant);
            bool different = NativeOledbWrapper.MemoryCompare(currentHandle, originalHandle, ODB.SizeOf_Variant);

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                // always clear the first structure
                Interop.OleAut32.VariantClear(currentHandle);
                if (different)
                {
                    // second structure different from the first
                    Interop.OleAut32.VariantClear(originalHandle);
                }
                else
                {
                    // second structure same as the first, just clear the field
                    SafeNativeMethods.ZeroMemory(originalHandle, ODB.SizeOf_Variant);
                }
            }
        }

        private static unsafe void FreePropVariant(IntPtr buffer, int valueOffset)
        {
            // two contiguous PROPVARIANT structures that need to be freed
            // the second should only be freed if different from the first
            Debug.Assert(0 == (sizeof(PROPVARIANT) % 8), "unexpected PROPVARIANT size mutiplier");
            Debug.Assert(0 == valueOffset % 8, "unexpected unaligned ptr offset");

            IntPtr currentHandle = ADP.IntPtrOffset(buffer, valueOffset);
            IntPtr originalHandle = ADP.IntPtrOffset(buffer, valueOffset + sizeof(PROPVARIANT));
            bool different = NativeOledbWrapper.MemoryCompare(currentHandle, originalHandle, sizeof(PROPVARIANT));

            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                // always clear the first structure
                Interop.Ole32.PropVariantClear(currentHandle);
                if (different)
                {
                    // second structure different from the first
                    Interop.Ole32.PropVariantClear(originalHandle);
                }
                else
                {
                    // second structure same as the first, just clear the field
                    SafeNativeMethods.ZeroMemory(originalHandle, sizeof(PROPVARIANT));
                }
            }
        }

        internal IntPtr InterlockedExchangePointer(int offset)
        {
            ValidateCheck(offset, IntPtr.Size);
            Debug.Assert(0 == offset % ADP.PtrSize, "invalid alignment");

            IntPtr value;
            bool mustRelease = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            {
                DangerousAddRef(ref mustRelease);

                IntPtr ptr = ADP.IntPtrOffset(DangerousGetHandle(), offset);
                value = SafeNativeMethods.InterlockedExchangePointer(ptr, IntPtr.Zero);
            }
            finally
            {
                if (mustRelease)
                {
                    DangerousRelease();
                }
            }
            return value;
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.
            _iaccessor = null;
            if (_needToReset && _haveData)
            {
                IntPtr buffer = base.handle;
                if (IntPtr.Zero != buffer)
                {
                    ResetValues(buffer, null);
                }
            }
            return base.ReleaseHandle();
        }
    }
}
