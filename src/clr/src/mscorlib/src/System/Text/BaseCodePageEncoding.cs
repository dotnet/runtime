// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if FEATURE_CODEPAGES_FILE
namespace System.Text
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;
    using System.Security.Permissions;
    using Microsoft.Win32.SafeHandles;

    // Our input file data structures look like:
    //
    // Header Structure Looks Like:
    //   struct NLSPlusHeader
    //   {
    //       WORD[16]    filename;       // 32 bytes
    //       WORD[4]     version;        // 8 bytes = 40     // I.e: 3, 2, 0, 0
    //       WORD        count;          // 2 bytes = 42     // Number of code page index's that'll follow
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
    //       WORD[4]     version;        // 8 bytes = 40     // I.e: 3.2.0.0
    //       WORD        codePage;       // 2 bytes = 42
    //       WORD        byteCount;      // 2 bytes = 44     // 1 or 2 byte code page (SBCS or DBCS)
    //       WORD        unicodeReplace; // 2 bytes = 46     // default replacement unicode character
    //       WORD        byteReplace;    // 2 bytes = 48     // default replacement byte(s)
    //       BYTE[]      data;           // data section
    //   }

    [Serializable]
    internal abstract class BaseCodePageEncoding : EncodingNLS, ISerializable
    {
        // Static & Const stuff
        internal const String CODE_PAGE_DATA_FILE_NAME = "codepages.nlp";
        [NonSerialized]
        protected int dataTableCodePage;

        // Variables to help us allocate/mark our memory section correctly
        [NonSerialized]
        protected bool bFlagDataTable = true;
        [NonSerialized]
        protected int iExtraBytes = 0;

        // Our private unicode to bytes best fit array and visa versa.
        [NonSerialized]
        protected char[] arrayUnicodeBestFit = null;
        [NonSerialized]
        protected char[] arrayBytesBestFit = null;

        // This is used to help ISCII, EUCJP and ISO2022 figure out they're MlangEncodings
        [NonSerialized]
        protected bool m_bUseMlangTypeForSerialization = false;

        [System.Security.SecuritySafeCritical] // static constructors should be safe to call
        static BaseCodePageEncoding()
        {
        }

        //
        // This is the header for the native data table that we load from CODE_PAGE_DATA_FILE_NAME.
        //
        // Explicit layout is used here since a syntax like char[16] can not be used in sequential layout.
        [StructLayout(LayoutKind.Explicit)]
        internal unsafe struct CodePageDataFileHeader
        {
            [FieldOffset(0)]
            internal char TableName;            // WORD[16]
            [FieldOffset(0x20)]
            internal ushort Version;            // WORD[4]
            [FieldOffset(0x28)]
            internal short CodePageCount;       // WORD
            [FieldOffset(0x2A)]
            internal short unused1;             // Add a unused WORD so that CodePages is aligned with DWORD boundary.
                                                // Otherwise, 64-bit version will fail.
            [FieldOffset(0x2C)]
            internal CodePageIndex CodePages;   // Start of code page index
        }

        [StructLayout(LayoutKind.Explicit, Pack=2)]
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
            [FieldOffset(0x30)]
            internal short FirstDataWord;   // WORD[]
        }

        // Initialize our global stuff
        [SecurityCritical]
        unsafe static CodePageDataFileHeader* m_pCodePageFileHeader = 
            (CodePageDataFileHeader*)GlobalizationAssembly.GetGlobalizationResourceBytePtr(
                typeof(CharUnicodeInfo).Assembly, CODE_PAGE_DATA_FILE_NAME);

        // Real variables
        [NonSerialized]
        [SecurityCritical]
        unsafe protected CodePageHeader* pCodePage = null;

        // Safe handle wrapper around section map view
        [System.Security.SecurityCritical] // auto-generated
        [NonSerialized]
        protected SafeViewOfFileHandle safeMemorySectionHandle = null;

        // Safe handle wrapper around mapped file handle
        [System.Security.SecurityCritical] // auto-generated
        [NonSerialized]
        protected SafeFileMappingHandle safeFileMappingHandle = null;

        [System.Security.SecurityCritical]  // auto-generated
        internal BaseCodePageEncoding(int codepage) : this(codepage, codepage)
        {
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal BaseCodePageEncoding(int codepage, int dataCodePage) :
            base(codepage == 0? Microsoft.Win32.Win32Native.GetACP(): codepage)
        {
            // Remember number of code page that we'll be using the table for.
            dataTableCodePage = dataCodePage;
            LoadCodePageTables();
        }

        // Constructor called by serialization.
        [System.Security.SecurityCritical]  // auto-generated
        internal BaseCodePageEncoding(SerializationInfo info, StreamingContext context) : base(0)
        {
            // We cannot ever call this, we've proxied ourselved to CodePageEncoding
            throw new ArgumentNullException("this");
        }

        // ISerializable implementation
        [System.Security.SecurityCritical]  // auto-generated_required
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            // Make sure to get the base stuff too This throws if info is null
            SerializeEncoding(info, context);
            Contract.Assert(info!=null, "[BaseCodePageEncoding.GetObjectData] Expected null info to throw");

            // Just need Everett maxCharSize (BaseCodePageEncoding) or m_maxByteSize (MLangBaseCodePageEncoding)
            info.AddValue(m_bUseMlangTypeForSerialization ? "m_maxByteSize" : "maxCharSize",
                          this.IsSingleByte ? 1 : 2);

            // Use this class or MLangBaseCodePageEncoding as our deserializer.
            info.SetType(m_bUseMlangTypeForSerialization ? typeof(MLangCodePageEncoding) :
                                                           typeof(CodePageEncoding));
        }

        // We need to load tables for our code page
        [System.Security.SecurityCritical]  // auto-generated
        private unsafe void LoadCodePageTables()
        {
            CodePageHeader* pCodePage = FindCodePage(dataTableCodePage);

            // Make sure we have one
            if (pCodePage == null)
            {
                // Didn't have one
                throw new NotSupportedException(
                    Environment.GetResourceString("NotSupported_NoCodepageData", CodePage));
            }

            // Remember our code page
            this.pCodePage = pCodePage;

            // We had it, so load it
            LoadManagedCodePage();
        }

        // Look up the code page pointer
        [System.Security.SecurityCritical]  // auto-generated
        private static unsafe CodePageHeader* FindCodePage(int codePage)
        {
            // We'll have to loop through all of the m_pCodePageIndex[] items to find our code page, this isn't
            // binary or anything so its not monsterously fast.
            for (int i = 0; i < m_pCodePageFileHeader->CodePageCount; i++)
            {
                CodePageIndex* pCodePageIndex = (&(m_pCodePageFileHeader->CodePages)) + i;

                if (pCodePageIndex->CodePage == codePage)
                {
                    // Found it!
                    CodePageHeader* pCodePage =
                        (CodePageHeader*)((byte*)m_pCodePageFileHeader + pCodePageIndex->Offset);
                    return pCodePage;
                }
            }

            // Couldn't find it
            return null;
        }

        // Get our code page byte count
        [System.Security.SecurityCritical]  // auto-generated
        internal static unsafe int GetCodePageByteSize(int codePage)
        {
            // Get our code page info
            CodePageHeader* pCodePage = FindCodePage(codePage);

            // If null return 0
            if (pCodePage == null)
                return 0;

            Contract.Assert(pCodePage->ByteCount == 1 || pCodePage->ByteCount == 2,
                "[BaseCodePageEncoding] Code page (" + codePage + ") has invalid byte size (" + pCodePage->ByteCount + ") in table");
            // Return what it says for byte count
            return pCodePage->ByteCount;
        }

        // We have a managed code page entry, so load our tables
        [System.Security.SecurityCritical]
        protected abstract unsafe void LoadManagedCodePage();

        // Allocate memory to load our code page
        [System.Security.SecurityCritical]  // auto-generated
        protected unsafe byte* GetSharedMemory(int iSize)
        {
            // Build our name
            String strName = GetMemorySectionName();

            IntPtr mappedFileHandle;

            // This gets shared memory for our map.  If its can't, it gives us clean memory.
            Byte *pMemorySection = EncodingTable.nativeCreateOpenFileMapping(strName, iSize, out mappedFileHandle);
            Contract.Assert(pMemorySection != null,
                "[BaseCodePageEncoding.GetSharedMemory] Expected non-null memory section to be opened");

            // If that failed, we have to die.
            if (pMemorySection == null)
                throw new OutOfMemoryException(
                    Environment.GetResourceString("Arg_OutOfMemoryException"));

            // if we have null file handle. this means memory was allocated after 
            // failing to open the mapped file.
            
            if (mappedFileHandle != IntPtr.Zero)
            {
                safeMemorySectionHandle = new SafeViewOfFileHandle((IntPtr) pMemorySection, true);
                safeFileMappingHandle = new SafeFileMappingHandle(mappedFileHandle, true);
            }

            return pMemorySection;
        }

        [System.Security.SecurityCritical]  // auto-generated
        protected unsafe virtual String GetMemorySectionName()
        {
            int iUseCodePage = this.bFlagDataTable ? dataTableCodePage : CodePage;

            String strName = String.Format(CultureInfo.InvariantCulture, "NLS_CodePage_{0}_{1}_{2}_{3}_{4}",
                iUseCodePage, this.pCodePage->VersionMajor, this.pCodePage->VersionMinor,
                this.pCodePage->VersionRevision, this.pCodePage->VersionBuild);

            return strName;
        }

        [System.Security.SecurityCritical]
        protected abstract unsafe void ReadBestFitTable();

        [System.Security.SecuritySafeCritical]  
        internal override char[] GetBestFitUnicodeToBytesData()
        {
            // Read in our best fit table if necessary
            if (arrayUnicodeBestFit == null) ReadBestFitTable();

            Contract.Assert(arrayUnicodeBestFit != null,
                "[BaseCodePageEncoding.GetBestFitUnicodeToBytesData]Expected non-null arrayUnicodeBestFit");

            // Normally we don't have any best fit data.
            return arrayUnicodeBestFit;
        }

        [System.Security.SecuritySafeCritical] 
        internal override char[] GetBestFitBytesToUnicodeData()
        {
            // Read in our best fit table if necessary
            if (arrayBytesBestFit == null) ReadBestFitTable();

            Contract.Assert(arrayBytesBestFit != null,
                "[BaseCodePageEncoding.GetBestFitBytesToUnicodeData]Expected non-null arrayBytesBestFit");

            // Normally we don't have any best fit data.
            return arrayBytesBestFit;
        }

        // During the AppDomain shutdown the Encoding class may already finalized and the memory section 
        // is invalid. so we detect that by validating the memory section handle then re-initialize the memory 
        // section by calling LoadManagedCodePage() method and eventually the mapped file handle and
        // the memory section pointer will get finalized one more time.
        [System.Security.SecurityCritical]  // auto-generated
        internal unsafe void CheckMemorySection()
        {
            if (safeMemorySectionHandle != null && safeMemorySectionHandle.DangerousGetHandle() == IntPtr.Zero)
            {
                LoadManagedCodePage();
            }
        }
    }
}

#endif // FEATURE_CODEPAGES_FILE
