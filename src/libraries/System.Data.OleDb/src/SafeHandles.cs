// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Data.Common;
using System.Data.ProviderBase;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static System.Data.Common.UnsafeNativeMethods;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace System.Data.OleDb
{
    internal sealed class DualCoTaskMem : SafeHandle
    {
        private IntPtr handle2;   // this must be protected so derived classes can use out params.

        public DualCoTaskMem() : base(IntPtr.Zero, true)
        {
            this.handle2 = IntPtr.Zero;
        }

        // IDBInfo.GetLiteralInfo
        internal DualCoTaskMem(UnsafeNativeMethods.IDBInfo dbInfo, int[]? literals, out int literalCount, out IntPtr literalInfo, out OleDbHResult hr) : this()
        {
            int count = (null != literals) ? literals.Length : 0;
            hr = dbInfo.GetLiteralInfo(count, literals, out literalCount, out base.handle, out this.handle2);
            literalInfo = base.handle;
        }

        // IColumnsInfo.GetColumnInfo
        internal DualCoTaskMem(UnsafeNativeMethods.IColumnsInfo columnsInfo, out IntPtr columnCount, out IntPtr columnInfos, out OleDbHResult hr) : this()
        {
            hr = columnsInfo.GetColumnInfo(out columnCount, out base.handle, out this.handle2);
            columnInfos = base.handle;
        }

        // IDBSchemaRowset.GetSchemas
        internal DualCoTaskMem(UnsafeNativeMethods.IDBSchemaRowset dbSchemaRowset, out int schemaCount, out IntPtr schemaGuids, out IntPtr schemaRestrictions, out OleDbHResult hr) : this()
        {
            hr = dbSchemaRowset.GetSchemas(out schemaCount, out base.handle, out this.handle2);
            schemaGuids = base.handle;
            schemaRestrictions = this.handle2;
        }

        internal DualCoTaskMem(UnsafeNativeMethods.IColumnsRowset icolumnsRowset, out IntPtr cOptColumns, out OleDbHResult hr) : base(IntPtr.Zero, true)
        {
            hr = icolumnsRowset.GetAvailableColumns(out cOptColumns, out base.handle);
        }

        public override bool IsInvalid
        {
            get
            {
                return (((IntPtr.Zero == base.handle)) && (IntPtr.Zero == this.handle2));
            }
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once.

            IntPtr ptr = base.handle;
            base.handle = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                SafeNativeMethods.CoTaskMemFree(ptr);
            }

            ptr = this.handle2;
            this.handle2 = IntPtr.Zero;
            if (IntPtr.Zero != ptr)
            {
                SafeNativeMethods.CoTaskMemFree(ptr);
            }
            return true;
        }
    }

    internal sealed class RowHandleBuffer : DbBuffer
    {
        internal RowHandleBuffer(IntPtr rowHandleFetchCount) : base((int)rowHandleFetchCount * ADP.PtrSize)
        {
        }

        internal IntPtr GetRowHandle(int index)
        {
            IntPtr value = ReadIntPtr(index * ADP.PtrSize);
            Debug.Assert(ODB.DB_NULL_HROW != value, "bad rowHandle");
            return value;
        }
    }

    internal sealed class StringMemHandle : DbBuffer
    {
        internal StringMemHandle(string? value) : base((null != value) ? checked(2 + 2 * value.Length) : 0)
        {
            if (null != value)
            {
                // null-termination exists because of the extra 2+ which is zero'd during on allocation
                WriteCharArray(0, value.ToCharArray(), 0, value.Length);
            }
        }
    }

    internal sealed class ChapterHandle : WrappedIUnknown
    {
        internal static readonly ChapterHandle DB_NULL_HCHAPTER = new ChapterHandle(IntPtr.Zero);
        private IntPtr _chapterHandle;

        internal static ChapterHandle CreateChapterHandle(object chapteredRowset, RowBinding binding, int valueOffset)
        {
            if ((null == chapteredRowset) || (IntPtr.Zero == binding.ReadIntPtr(valueOffset)))
            {
                return ChapterHandle.DB_NULL_HCHAPTER;
            }
            return new ChapterHandle(chapteredRowset, binding, valueOffset);
        }

        // from ADODBRecordSetConstruction we do not want to release the initial chapter handle
        internal static ChapterHandle CreateChapterHandle(IntPtr chapter)
        {
            if (IntPtr.Zero == chapter)
            {
                return ChapterHandle.DB_NULL_HCHAPTER;
            }
            return new ChapterHandle(chapter);
        }

        // from ADODBRecordSetConstruction we do not want to release the initial chapter handle
        private ChapterHandle(IntPtr chapter) : base((object?)null)
        {
            _chapterHandle = chapter;
        }

        private ChapterHandle(object chapteredRowset, RowBinding binding, int valueOffset) : base(chapteredRowset)
        {
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                _chapterHandle = binding.InterlockedExchangePointer(valueOffset);
            }
        }

        internal IntPtr HChapter
        {
            get
            {
                return _chapterHandle;
            }
        }

        protected override bool ReleaseHandle()
        {
            // NOTE: The SafeHandle class guarantees this will be called exactly once and is non-interrutible.
            IntPtr chapter = _chapterHandle;
            _chapterHandle = IntPtr.Zero;

            if ((IntPtr.Zero != base.handle) && (IntPtr.Zero != chapter))
            {
                NativeOledbWrapper.IChapteredRowsetReleaseChapter(base.handle, chapter);
            }
            return base.ReleaseHandle();
        }
    }

    internal enum XACTTC
    {
        XACTTC_NONE = 0x0000,
        XACTTC_SYNC_PHASEONE = 0x0001,
        XACTTC_SYNC_PHASETWO = 0x0002,
        XACTTC_SYNC = 0x0002,
        XACTTC_ASYNC_PHASEONE = 0x0004,
        XACTTC_ASYNC = 0x0004
    }

    #region PROPVARIANT

    /// <summary>
    /// Managed view of unmanaged PROPVARIANT type
    /// </summary>
    /// <remarks>
    /// PROPVARIANT can represent many different things.  We are only interested in strings
    /// for this version but the full range of values is listed her for completeness.
    ///
    /// typedef unsigned short VARTYPE;
    /// typedef unsigned short WORD;
    /// typedef struct PROPVARIANT {
    /// VARTYPE vt;  WORD wReserved1;  WORD wReserved2;  WORD wReserved3;
    /// union {
    ///     CHAR cVal;
    ///     UCHAR bVal;
    ///     SHORT iVal;
    ///     USHORT uiVal;
    ///     LONG lVal;
    ///     INT intVal;
    ///     ULONG ulVal;
    ///     UINT uintVal;
    ///     LARGE_INTEGER hVal;
    ///     ULARGE_INTEGER uhVal;
    ///     FLOAT fltVal;    DOUBLE dblVal;    CY cyVal;    DATE date;
    ///     BSTR bstrVal;    VARIANT_BOOL boolVal;    SCODE scode;
    ///     FILETIME filetime;    LPSTR pszVal;    LPWSTR pwszVal;
    ///     CLSID* puuid;    CLIPDATA* pclipdata;    BLOB blob;
    ///     IStream* pStream;    IStorage* pStorage;    IUnknown* punkVal;
    ///     IDispatch* pdispVal;    LPSAFEARRAY parray;    CAC cac;
    ///     CAUB caub;    CAI cai;    CAUI caui;    CAL cal;    CAUL caul;
    ///     CAH cah;    CAUH cauh;    CAFLT caflt;    CADBL cadbl;
    ///     CACY cacy;    CADATE cadate;    CABSTR cabstr;
    ///     CABOOL cabool;    CASCODE cascode;    CALPSTR calpstr;
    ///     CALPWSTR calpwstr;    CAFILETIME cafiletime;    CACLSID cauuid;
    ///     CACLIPDATA caclipdata;    CAPROPVARIANT capropvar;
    ///     CHAR* pcVal;    UCHAR* pbVal;    SHORT* piVal;    USHORT* puiVal;
    ///     LONG* plVal;    ULONG* pulVal;    INT* pintVal;    UINT* puintVal;
    ///     FLOAT* pfltVal;    DOUBLE* pdblVal;    VARIANT_BOOL* pboolVal;
    ///     DECIMAL* pdecVal;    SCODE* pscode;    CY* pcyVal;
    ///     PROPVARIANT* pvarVal;
    /// };
    /// } PROPVARIANT;
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct PROPVARIANT
    {
        /// <summary>
        /// Variant type
        /// </summary>
        internal VARTYPE vt;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved1;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved2;

        /// <summary>
        /// unused
        /// </summary>
        internal ushort wReserved3;

        /// <summary>
        /// union where the actual variant value lives
        /// </summary>
        internal PropVariantUnion union;
    }

    /// <summary>
    /// enumeration for all legal types of a PROPVARIANT
    /// </summary>
    /// <remarks>add definitions as needed</remarks>
    internal enum VARTYPE : short
    {
        /// <summary>
        /// BSTR
        /// </summary>
        VT_BSTR = 8,        // BSTR allocated using SysAllocString

        /// <summary>
        /// LPSTR
        /// </summary>
        VT_LPSTR = 30,

        /// <summary>
        /// FILETIME
        /// </summary>
        VT_FILETIME = 64,
    }

    /// <summary>
    /// Union portion of PROPVARIANT
    /// </summary>
    /// <remarks>
    /// All fields (or their placeholders) are declared even if
    /// they are not used. This is to make sure that the size of
    /// the union matches the size of the union in
    /// the actual unmanaged PROPVARIANT structure
    /// for all architectures (32-bit/64-bit).
    /// Points to note:
    /// - All pointer type fields are declared as IntPtr.
    /// - CAxxx type fields (like CAC, CAUB, etc.) are all of same
    ///     structural layout, hence not all declared individually
    ///     since they are not used. A placeholder CArray
    ///     is used to represent all of them to account for the
    ///     size of these types. CArray is defined later.
    /// - Rest of the fields are declared with corresponding
    ///     managed equivalent types.
    /// </remarks>
    [StructLayout(LayoutKind.Explicit)]
    internal struct PropVariantUnion
    {
        /// <summary>
        /// CHAR
        /// </summary>
        [FieldOffset(0)]
        internal sbyte cVal;

        /// <summary>
        /// UCHAR
        /// </summary>
        [FieldOffset(0)]
        internal byte bVal;

        /// <summary>
        /// SHORT
        /// </summary>
        [FieldOffset(0)]
        internal short iVal;

        /// <summary>
        /// USHORT
        /// </summary>
        [FieldOffset(0)]
        internal ushort uiVal;

        /// <summary>
        /// LONG
        /// </summary>
        [FieldOffset(0)]
        internal int lVal;

        /// <summary>
        /// ULONG
        /// </summary>
        [FieldOffset(0)]
        internal uint ulVal;

        /// <summary>
        /// INT
        /// </summary>
        [FieldOffset(0)]
        internal int intVal;

        /// <summary>
        /// UINT
        /// </summary>
        [FieldOffset(0)]
        internal uint uintVal;

        /// <summary>
        /// LARGE_INTEGER
        /// </summary>
        [FieldOffset(0)]
        internal long hVal;

        /// <summary>
        /// ULARGE_INTEGER
        /// </summary>
        [FieldOffset(0)]
        internal ulong uhVal;

        /// <summary>
        /// FLOAT
        /// </summary>
        [FieldOffset(0)]
        internal float fltVal;

        /// <summary>
        /// DOUBLE
        /// </summary>
        [FieldOffset(0)]
        internal double dblVal;

        /// <summary>
        /// VARIANT_BOOL
        /// </summary>
        [FieldOffset(0)]
        internal short boolVal;

        /// <summary>
        /// SCODE
        /// </summary>
        [FieldOffset(0)]
        internal int scode;

        /// <summary>
        /// CY
        /// </summary>
        [FieldOffset(0)]
        internal CY cyVal;

        /// <summary>
        /// DATE
        /// </summary>
        [FieldOffset(0)]
        internal double date;

        /// <summary>
        /// FILETIME
        /// </summary>
        [FieldOffset(0)]
        internal System.Runtime.InteropServices.ComTypes.FILETIME filetime;

        /// <summary>
        /// CLSID*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puuid;

        /// <summary>
        /// CLIPDATA*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pclipdata;

        /// <summary>
        /// BSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr bstrVal;

        /// <summary>
        /// BSTRBLOB
        /// </summary>
        [FieldOffset(0)]
        internal BSTRBLOB bstrblobVal;

        /// <summary>
        /// BLOB
        /// </summary>
        [FieldOffset(0)]
        internal BLOB blob;

        /// <summary>
        /// LPSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pszVal;

        /// <summary>
        /// LPWSTR
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pwszVal;

        /// <summary>
        /// IUnknown*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr punkVal;

        /// <summary>
        /// IDispatch*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdispVal;

        /// <summary>
        /// IStream*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pStream;

        /// <summary>
        /// IStorage*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pStorage;

        /// <summary>
        /// LPVERSIONEDSTREAM
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pVersionedStream;

        /// <summary>
        /// LPSAFEARRAY
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr parray;

        /// <summary>
        /// Placeholder for
        /// CAC, CAUB, CAI, CAUI, CAL, CAUL, CAH, CAUH; CAFLT,
        /// CADBL, CABOOL, CASCODE, CACY, CADATE, CAFILETIME,
        /// CACLSID, CACLIPDATA, CABSTR, CABSTRBLOB,
        /// CALPSTR, CALPWSTR, CAPROPVARIANT
        /// </summary>
        [FieldOffset(0)]
        internal CArray cArray;

        /// <summary>
        /// CHAR*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pcVal;

        /// <summary>
        /// UCHAR*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pbVal;

        /// <summary>
        /// SHORT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr piVal;

        /// <summary>
        /// USHORT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puiVal;

        /// <summary>
        /// LONG*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr plVal;

        /// <summary>
        /// ULONG*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pulVal;

        /// <summary>
        /// INT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pintVal;

        /// <summary>
        /// UINT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr puintVal;

        /// <summary>
        /// FLOAT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pfltVal;

        /// <summary>
        /// DOUBLE*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdblVal;

        /// <summary>
        /// VARIANT_BOOL*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pboolVal;

        /// <summary>
        /// DECIMAL*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdecVal;

        /// <summary>
        /// SCODE*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pscode;

        /// <summary>
        /// CY*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pcyVal;

        /// <summary>
        /// DATE*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pdate;

        /// <summary>
        /// BSTR*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pbstrVal;

        /// <summary>
        /// IUnknown**
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr ppunkVal;

        /// <summary>
        /// IDispatch**
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr ppdispVal;

        /// <summary>
        /// LPSAFEARRAY*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pparray;

        /// <summary>
        /// PROPVARIANT*
        /// </summary>
        [FieldOffset(0)]
        internal IntPtr pvarVal;
    }

    #region Structs used by PropVariantUnion

    //
    // NOTE: Verifiability requires that the
    // fields of these value-types need to be public
    // since PropVariantUnion has explicit layout,
    // and has these value-types as its fields in a way that
    // overlaps with other PropVariantUnion fields
    // (same FieldOffset for multiple fields).
    //

    /// <summary>
    /// CY, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct CY
    {
        public uint Lo;
        public int Hi;
    }

    /// <summary>
    /// BSTRBLOB, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct BSTRBLOB
    {
        public uint cbSize;
        public IntPtr pData;
    }

    /// <summary>
    /// BLOB, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct BLOB
    {
        public uint cbSize;
        public IntPtr pBlobData;
    }

    /// <summary>
    /// CArray, used in PropVariantUnion.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct CArray
    {
        public uint cElems;
        public IntPtr pElems;
    }

    #endregion Structs used by PropVariantUnion

    #endregion PROPVARIANT

    internal static class NativeOledbWrapper
    {
        internal static unsafe OleDbHResult IChapteredRowsetReleaseChapter(System.IntPtr ptr, System.IntPtr chapter)
        {
            OleDbHResult hr = OleDbHResult.E_UNEXPECTED;
            IntPtr hchapter = chapter;
            System.Data.Common.UnsafeNativeMethods.IChapteredRowset? chapteredRowset = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                Guid IID_IChapteredRowset = typeof(System.Data.Common.UnsafeNativeMethods.IChapteredRowset).GUID;
                hr = (OleDbHResult)Marshal.QueryInterface(ptr, ref IID_IChapteredRowset, out var pChapteredRowset);
                if (pChapteredRowset != IntPtr.Zero)
                {
                    chapteredRowset = (System.Data.Common.UnsafeNativeMethods.IChapteredRowset)Marshal.GetObjectForIUnknown(pChapteredRowset);
                    hr = (OleDbHResult)chapteredRowset.ReleaseChapter(hchapter, out var refcount);
                    Marshal.ReleaseComObject(chapteredRowset);
                    Marshal.Release(pChapteredRowset);
                }
            }
            return hr;
        }

        internal static unsafe OleDbHResult ITransactionAbort(System.IntPtr ptr)
        {
            OleDbHResult hr = OleDbHResult.E_UNEXPECTED;
            ITransactionLocal? transactionLocal = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                Guid IID_ITransactionLocal = typeof(ITransactionLocal).GUID;
                hr = (OleDbHResult)Marshal.QueryInterface(ptr, ref IID_ITransactionLocal, out var pTransaction);
                if (pTransaction != IntPtr.Zero)
                {
                    transactionLocal = (ITransactionLocal)Marshal.GetObjectForIUnknown(pTransaction);
                    hr = (OleDbHResult)transactionLocal.Abort(IntPtr.Zero, false, false);
                    Marshal.ReleaseComObject(transactionLocal);
                    Marshal.Release(pTransaction);
                }
            }
            return hr;
        }

        internal static unsafe OleDbHResult ITransactionCommit(System.IntPtr ptr)
        {
            OleDbHResult hr = OleDbHResult.E_UNEXPECTED;
            ITransactionLocal? transactionLocal = null;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { }
            finally
            {
                Guid IID_ITransactionLocal = typeof(ITransactionLocal).GUID;
                hr = (OleDbHResult)Marshal.QueryInterface(ptr, ref IID_ITransactionLocal, out var pTransaction);
                if (pTransaction != IntPtr.Zero)
                {
                    transactionLocal = (ITransactionLocal)Marshal.GetObjectForIUnknown(pTransaction);
                    hr = (OleDbHResult)transactionLocal.Commit(false, (uint)XACTTC.XACTTC_SYNC_PHASETWO, 0);
                    Marshal.ReleaseComObject(transactionLocal);
                    Marshal.Release(pTransaction);
                }
            }
            return hr;
        }

        internal static bool MemoryCompare(System.IntPtr buf1, System.IntPtr buf2, int count)
        {
            Debug.Assert(buf1 != buf2, "buf1 and buf2 are the same");
            Debug.Assert(buf1.ToInt64() < buf2.ToInt64() || buf2.ToInt64() + count <= buf1.ToInt64(), "overlapping region buf1");
            Debug.Assert(buf2.ToInt64() < buf1.ToInt64() || buf1.ToInt64() + count <= buf2.ToInt64(), "overlapping region buf2");
            Debug.Assert(0 <= count, "negative count");
            unsafe
            {
                ReadOnlySpan<byte> span1 = new ReadOnlySpan<byte>(buf1.ToPointer(), count);
                ReadOnlySpan<byte> span2 = new ReadOnlySpan<byte>(buf2.ToPointer(), count);
                return !MemoryExtensions.SequenceEqual(span1, span2);
                //0 if all count bytes of lhs and rhs are equal.
                // TODO: confirm condition with tests
            }
        }

        internal static void MemoryCopy(System.IntPtr dst, System.IntPtr src, int count)
        {
            Debug.Assert(dst != src, "dst and src are the same");
            Debug.Assert(dst.ToInt64() < src.ToInt64() || src.ToInt64() + count <= dst.ToInt64(), "overlapping region dst");
            Debug.Assert(src.ToInt64() < dst.ToInt64() || dst.ToInt64() + count <= src.ToInt64(), "overlapping region src");
            Debug.Assert(0 <= count, "negative count");
            unsafe
            {
                var dstSpan = new System.Span<byte>(dst.ToPointer(), count);
                var srcSpan = new System.Span<byte>(src.ToPointer(), count);
                srcSpan.CopyTo(dstSpan);
            }
        }
    }
}
