// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Serialization;

namespace System.Text
{
    // Our input file data structures look like:
    //
    // Header structure looks like:
    //   struct NLSPlusHeader
    //   {
    //       WORD[16]    filename;       // 32 bytes
    //       WORD[4]     version;        // 8 bytes = 40     // e.g.: 3, 2, 0, 0
    //       WORD        count;          // 2 bytes = 42     // Number of code page indexes that will follow
    //   }
    //
    // Each code page section looks like:
    //   struct NLSCodePageIndex
    //   {
    //       WORD[16]    codePageName;   // 32 bytes
    //       WORD        codePage;       // +2 bytes = 34
    //       WORD        byteCount;      // +2 bytes = 36
    //       DWORD       offset;         // +4 bytes = 40    // Bytes from beginning of FILE.
    //   }
    //
    // Each code page then has its own header
    //   struct NLSCodePage
    //   {
    //       WORD[16]    codePageName;   // 32 bytes
    //       WORD[4]     version;        // 8 bytes = 40     // e.g.: 3.2.0.0
    //       WORD        codePage;       // 2 bytes = 42
    //       WORD        byteCount;      // 2 bytes = 44     // 1 or 2 byte code page (SBCS or DBCS)
    //       WORD        unicodeReplace; // 2 bytes = 46     // default replacement unicode character
    //       WORD        byteReplace;    // 2 bytes = 48     // default replacement byte(s)
    //       BYTE[]      data;           // data section
    //   }
    internal abstract class BaseCodePageEncoding : EncodingNLS, ISerializable
    {
        internal const String CODE_PAGE_DATA_FILE_NAME = "codepages.nlp";

        protected int dataTableCodePage;

        // Variables to help us allocate/mark our memory section correctly
        protected int iExtraBytes = 0;

        // Our private unicode-to-bytes best-fit-array, and vice versa.
        protected char[] arrayUnicodeBestFit = null;
        protected char[] arrayBytesBestFit = null;

        internal BaseCodePageEncoding(int codepage)
            : this(codepage, codepage)
        {
        }

        internal BaseCodePageEncoding(int codepage, int dataCodePage)
            : base(codepage, new InternalEncoderBestFitFallback(null), new InternalDecoderBestFitFallback(null))
        {
            SetFallbackEncoding();
            // Remember number of code pages that we'll be using the table for.
            dataTableCodePage = dataCodePage;
            LoadCodePageTables();
        }

        internal BaseCodePageEncoding(int codepage, int dataCodePage, EncoderFallback enc, DecoderFallback dec)
            : base(codepage, enc, dec)
        {
            // Remember number of code pages that we'll be using the table for.
            dataTableCodePage = dataCodePage;
            LoadCodePageTables();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        // Just a helper as we cannot use 'this' when calling 'base(...)' 
        private void SetFallbackEncoding()
        {
            (EncoderFallback as InternalEncoderBestFitFallback).encoding = this;
            (DecoderFallback as InternalDecoderBestFitFallback).encoding = this;
        }

        //
        // This is the header for the native data table that we load from CODE_PAGE_DATA_FILE_NAME.
        //
        // Explicit layout is used here since a syntax like char[16] can not be used in sequential layout.
        [StructLayout(LayoutKind.Explicit)]
        internal struct CodePageDataFileHeader
        {
            [FieldOffset(0)]
            internal char TableName;            // WORD[16]
            [FieldOffset(0x20)]
            internal ushort Version;            // WORD[4]
            [FieldOffset(0x28)]
            internal short CodePageCount;       // WORD
            [FieldOffset(0x2A)]
            internal short unused1;             // Add an unused WORD so that CodePages is aligned with DWORD boundary.
        }
        private const int CODEPAGE_DATA_FILE_HEADER_SIZE = 44;

        [StructLayout(LayoutKind.Explicit, Pack = 2)]
        internal unsafe struct CodePageIndex
        {
            [FieldOffset(0)]
            internal char CodePageName;     // WORD[16]
            [FieldOffset(0x20)]
            internal short CodePage;        // WORD
            [FieldOffset(0x22)]
            internal short ByteCount;       // WORD
            [FieldOffset(0x24)]
            internal int Offset;            // DWORD
        }

        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct CodePageHeader
        {
            [FieldOffset(0)]
            internal char CodePageName;     // WORD[16]
            [FieldOffset(0x20)]
            internal ushort VersionMajor;   // WORD
            [FieldOffset(0x22)]
            internal ushort VersionMinor;   // WORD
            [FieldOffset(0x24)]
            internal ushort VersionRevision;// WORD
            [FieldOffset(0x26)]
            internal ushort VersionBuild;   // WORD
            [FieldOffset(0x28)]
            internal short CodePage;        // WORD
            [FieldOffset(0x2a)]
            internal short ByteCount;       // WORD     // 1 or 2 byte code page (SBCS or DBCS)
            [FieldOffset(0x2c)]
            internal char UnicodeReplace;   // WORD     // default replacement unicode character
            [FieldOffset(0x2e)]
            internal ushort ByteReplace;    // WORD     // default replacement bytes
        }
        private const int CODEPAGE_HEADER_SIZE = 48;

        // Initialize our global stuff
        private static byte[] s_codePagesDataHeader = new byte[CODEPAGE_DATA_FILE_HEADER_SIZE];
        protected static Stream s_codePagesEncodingDataStream = GetEncodingDataStream(CODE_PAGE_DATA_FILE_NAME);
        protected static readonly Object s_streamLock = new Object(); // this lock used when reading from s_codePagesEncodingDataStream

        // Real variables
        protected byte[] m_codePageHeader = new byte[CODEPAGE_HEADER_SIZE];
        protected int m_firstDataWordOffset;
        protected int m_dataSize;

        // Safe handle wrapper around section map view
        protected SafeAllocHHandle safeNativeMemoryHandle = null;

        internal static Stream GetEncodingDataStream(String tableName)
        {
            Debug.Assert(tableName != null, "table name can not be null");

            // NOTE: We must reflect on a public type that is exposed in the contract here
            // (i.e. CodePagesEncodingProvider), otherwise we will not get a reference to
            // the right assembly.
            Stream stream = typeof(CodePagesEncodingProvider).GetTypeInfo().Assembly.GetManifestResourceStream(tableName);

            if (stream == null)
            {
                // We can not continue if we can't get the resource.
                throw new InvalidOperationException();
            }

            // Read the header
            stream.Read(s_codePagesDataHeader, 0, s_codePagesDataHeader.Length);

            return stream;
        }

        // We need to load tables for our code page
        private unsafe void LoadCodePageTables()
        {
            if (!FindCodePage(dataTableCodePage))
            {
                // Didn't have one
                throw new NotSupportedException(SR.Format(SR.NotSupported_NoCodepageData, CodePage));
            }

            // We had it, so load it
            LoadManagedCodePage();
        }

        // Look up the code page pointer
        private unsafe bool FindCodePage(int codePage)
        {
            Debug.Assert(m_codePageHeader != null && m_codePageHeader.Length == CODEPAGE_HEADER_SIZE, "m_codePageHeader expected to match in size the struct CodePageHeader");

            // Loop through all of the m_pCodePageIndex[] items to find our code page
            byte[] codePageIndex = new byte[sizeof(CodePageIndex)];

            lock (s_streamLock)
            {
                // seek to the first CodePageIndex entry
                s_codePagesEncodingDataStream.Seek(CODEPAGE_DATA_FILE_HEADER_SIZE, SeekOrigin.Begin);

                int codePagesCount;
                fixed (byte* pBytes = &s_codePagesDataHeader[0])
                {
                    CodePageDataFileHeader* pDataHeader = (CodePageDataFileHeader*)pBytes;
                    codePagesCount = pDataHeader->CodePageCount;
                }

                fixed (byte* pBytes = &codePageIndex[0])
                {
                    CodePageIndex* pCodePageIndex = (CodePageIndex*)pBytes;
                    for (int i = 0; i < codePagesCount; i++)
                    {
                        s_codePagesEncodingDataStream.Read(codePageIndex, 0, codePageIndex.Length);

                        if (pCodePageIndex->CodePage == codePage)
                        {
                            // Found it!
                            long position = s_codePagesEncodingDataStream.Position;
                            s_codePagesEncodingDataStream.Seek((long)pCodePageIndex->Offset, SeekOrigin.Begin);
                            s_codePagesEncodingDataStream.Read(m_codePageHeader, 0, m_codePageHeader.Length);
                            m_firstDataWordOffset = (int)s_codePagesEncodingDataStream.Position; // stream now pointing to the codepage data

                            if (i == codePagesCount - 1) // last codepage
                            {
                                m_dataSize = (int)(s_codePagesEncodingDataStream.Length - pCodePageIndex->Offset - m_codePageHeader.Length);
                            }
                            else
                            {
                                // Read Next codepage data to get the offset and then calculate the size
                                s_codePagesEncodingDataStream.Seek(position, SeekOrigin.Begin);
                                int currentOffset = pCodePageIndex->Offset;
                                s_codePagesEncodingDataStream.Read(codePageIndex, 0, codePageIndex.Length);
                                m_dataSize = pCodePageIndex->Offset - currentOffset - m_codePageHeader.Length;
                            }

                            return true;
                        }
                    }
                }
            }

            // Couldn't find it
            return false;
        }

        // Get our code page byte count
        internal static unsafe int GetCodePageByteSize(int codePage)
        {
            // Loop through all of the m_pCodePageIndex[] items to find our code page
            byte[] codePageIndex = new byte[sizeof(CodePageIndex)];

            lock (s_streamLock)
            {
                // seek to the first CodePageIndex entry
                s_codePagesEncodingDataStream.Seek(CODEPAGE_DATA_FILE_HEADER_SIZE, SeekOrigin.Begin);

                int codePagesCount;
                fixed (byte* pBytes = &s_codePagesDataHeader[0])
                {
                    CodePageDataFileHeader* pDataHeader = (CodePageDataFileHeader*)pBytes;
                    codePagesCount = pDataHeader->CodePageCount;
                }

                fixed (byte* pBytes = &codePageIndex[0])
                {
                    CodePageIndex* pCodePageIndex = (CodePageIndex*)pBytes;
                    for (int i = 0; i < codePagesCount; i++)
                    {
                        s_codePagesEncodingDataStream.Read(codePageIndex, 0, codePageIndex.Length);

                        if (pCodePageIndex->CodePage == codePage)
                        {
                            Debug.Assert(pCodePageIndex->ByteCount == 1 || pCodePageIndex->ByteCount == 2,
                                "[BaseCodePageEncoding] Code page (" + codePage + ") has invalid byte size (" + pCodePageIndex->ByteCount + ") in table");
                            // Return what it says for byte count
                            return pCodePageIndex->ByteCount;
                        }
                    }
                }
            }

            // Couldn't find it
            return 0;
        }

        // We have a managed code page entry, so load our tables
        protected abstract unsafe void LoadManagedCodePage();

        // Allocate memory to load our code page
        protected unsafe byte* GetNativeMemory(int iSize)
        {
            if (safeNativeMemoryHandle == null)
            {
                byte* pNativeMemory = (byte*)Marshal.AllocHGlobal(iSize);
                Debug.Assert(pNativeMemory != null);

                safeNativeMemoryHandle = new SafeAllocHHandle((IntPtr)pNativeMemory);
            }

            return (byte*)safeNativeMemoryHandle.DangerousGetHandle();
        }

        protected abstract unsafe void ReadBestFitTable();

        internal char[] GetBestFitUnicodeToBytesData()
        {
            // Read in our best fit table if necessary
            if (arrayUnicodeBestFit == null) ReadBestFitTable();

            Debug.Assert(arrayUnicodeBestFit != null, "[BaseCodePageEncoding.GetBestFitUnicodeToBytesData]Expected non-null arrayUnicodeBestFit");

            // Normally we don't have any best fit data.
            return arrayUnicodeBestFit;
        }

        internal char[] GetBestFitBytesToUnicodeData()
        {
            // Read in our best fit table if necessary
            if (arrayBytesBestFit == null) ReadBestFitTable();

            Debug.Assert(arrayBytesBestFit != null, "[BaseCodePageEncoding.GetBestFitBytesToUnicodeData]Expected non-null arrayBytesBestFit");

            // Normally we don't have any best fit data.
            return arrayBytesBestFit;
        }

        // During the AppDomain shutdown the Encoding class may have already finalized, making the memory section 
        // invalid. We detect that by validating the memory section handle then re-initializing the memory 
        // section by calling LoadManagedCodePage() method and eventually the mapped file handle and
        // the memory section pointer will get finalized one more time.
        internal unsafe void CheckMemorySection()
        {
            if (safeNativeMemoryHandle != null && safeNativeMemoryHandle.DangerousGetHandle() == IntPtr.Zero)
            {
                LoadManagedCodePage();
            }
        }
    }
}
