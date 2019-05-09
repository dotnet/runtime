// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//
// ZapImage.cpp
//

//
// NGEN-specific infrastructure for writing PE files.
// 
// ======================================================================================

#include "common.h"
#include "strsafe.h"

#include "zaprelocs.h"

#include "zapinnerptr.h"
#include "zapwrapper.h"

#include "zapheaders.h"
#include "zapmetadata.h"
#include "zapcode.h"
#include "zapimport.h"

#ifdef FEATURE_READYTORUN_COMPILER
#include "zapreadytorun.h"
#endif

#include "md5.h"

// This is RTL_CONTAINS_FIELD from ntdef.h
#define CONTAINS_FIELD(Struct, Size, Field) \
    ( (((PCHAR)(&(Struct)->Field)) + sizeof((Struct)->Field)) <= (((PCHAR)(Struct))+(Size)) )

/* --------------------------------------------------------------------------- *
 * Destructor wrapper objects
 * --------------------------------------------------------------------------- */

ZapImage::ZapImage(Zapper *zapper)
  : m_zapper(zapper),
    m_stats(new ZapperStats())
    /* Everything else is initialized to 0 by default */
{
}

ZapImage::~ZapImage()
{
#ifdef ZAP_HASHTABLE_TUNING
    // If ZAP_HASHTABLE_TUNING is defined, preallocate is overloaded to print the tunning constants
    Preallocate();
#endif

    //
    // Clean up.
    //
    if (m_stats != NULL)
	    delete m_stats;

    if (m_pModuleFileName != NULL)
        delete [] m_pModuleFileName;

    if (m_pMDImport != NULL)
        m_pMDImport->Release();

    if (m_pAssemblyEmit != NULL)
        m_pAssemblyEmit->Release();

    if (m_profileDataFile != NULL)
        UnmapViewOfFile(m_profileDataFile);

    if (m_pPreloader)
        m_pPreloader->Release();

    if (m_pImportSectionsTable != NULL)
        m_pImportSectionsTable->~ZapImportSectionsTable();

    if (m_pGCInfoTable != NULL)
        m_pGCInfoTable->~ZapGCInfoTable();

#ifdef WIN64EXCEPTIONS
    if (m_pUnwindDataTable != NULL)
        m_pUnwindDataTable->~ZapUnwindDataTable();
#endif

    if (m_pStubDispatchDataTable != NULL)
        m_pStubDispatchDataTable->~ZapImportSectionSignatures();

    if (m_pExternalMethodDataTable != NULL)
        m_pExternalMethodDataTable->~ZapImportSectionSignatures();

    if (m_pDynamicHelperDataTable != NULL)
        m_pDynamicHelperDataTable->~ZapImportSectionSignatures();

    if (m_pDebugInfoTable != NULL)
        m_pDebugInfoTable->~ZapDebugInfoTable();

    if (m_pVirtualSectionsTable != NULL)
        m_pVirtualSectionsTable->~ZapVirtualSectionsTable();

    if (m_pILMetaData != NULL)
        m_pILMetaData->~ZapILMetaData();

    if (m_pBaseRelocs != NULL)
        m_pBaseRelocs->~ZapBaseRelocs();

    if (m_pAssemblyMetaData != NULL)
        m_pAssemblyMetaData->~ZapMetaData();

    //
    // Destruction of auxiliary tables in alphabetical order
    //

    if (m_pImportTable != NULL) 
        m_pImportTable->~ZapImportTable();

    if (m_pInnerPtrs != NULL) 
        m_pInnerPtrs->~ZapInnerPtrTable();

    if (m_pMethodEntryPoints != NULL)
        m_pMethodEntryPoints->~ZapMethodEntryPointTable();

    if (m_pWrappers != NULL) 
        m_pWrappers->~ZapWrapperTable();
}

void ZapImage::InitializeSections()
{
    AllocateVirtualSections();

    m_pCorHeader = new (GetHeap()) ZapCorHeader(this);
    m_pHeaderSection->Place(m_pCorHeader);

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COMHEADER, m_pCorHeader);

    m_pNativeHeader = new (GetHeap()) ZapNativeHeader(this);
    m_pHeaderSection->Place(m_pNativeHeader);

    m_pCodeManagerEntry = new (GetHeap()) ZapCodeManagerEntry(this);
    m_pHeaderSection->Place(m_pCodeManagerEntry);

    m_pImportSectionsTable = new (GetHeap()) ZapImportSectionsTable(this);
    m_pImportTableSection->Place(m_pImportSectionsTable);

    m_pExternalMethodDataTable = new (GetHeap()) ZapImportSectionSignatures(this, m_pExternalMethodThunkSection, m_pGCSection);
    m_pExternalMethodDataSection->Place(m_pExternalMethodDataTable);

    m_pStubDispatchDataTable = new (GetHeap()) ZapImportSectionSignatures(this, m_pStubDispatchCellSection, m_pGCSection);
    m_pStubDispatchDataSection->Place(m_pStubDispatchDataTable);

    m_pImportTable = new (GetHeap()) ZapImportTable(this);

    m_pGCInfoTable = new (GetHeap()) ZapGCInfoTable(this);
    m_pExceptionInfoLookupTable = new (GetHeap()) ZapExceptionInfoLookupTable(this);

#ifdef WIN64EXCEPTIONS
    m_pUnwindDataTable = new (GetHeap()) ZapUnwindDataTable(this);
#endif

    m_pEEInfoTable = ZapBlob::NewAlignedBlob(this, NULL, sizeof(CORCOMPILE_EE_INFO_TABLE), TARGET_POINTER_SIZE);
    m_pEETableSection->Place(m_pEEInfoTable);

    //
    // Allocate Helper table, and fill it out
    //

    m_pHelperThunks = new (GetHeap()) ZapNode * [CORINFO_HELP_COUNT];

    if (!m_zapper->m_pOpt->m_fNoMetaData)
    {
        m_pILMetaData = new (GetHeap()) ZapILMetaData(this);
        m_pILMetaDataSection->Place(m_pILMetaData);
    }

    m_pDebugInfoTable = new (GetHeap()) ZapDebugInfoTable(this);
    m_pDebugSection->Place(m_pDebugInfoTable);

    m_pBaseRelocs = new (GetHeap()) ZapBaseRelocs(this);
    m_pBaseRelocsSection->Place(m_pBaseRelocs);

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC, m_pBaseRelocsSection);

    //
    // Initialization of auxiliary tables in alphabetical order
    //
    m_pInnerPtrs = new (GetHeap()) ZapInnerPtrTable(this);
    m_pMethodEntryPoints = new (GetHeap()) ZapMethodEntryPointTable(this);
    m_pWrappers = new (GetHeap()) ZapWrapperTable(this);

    // Place the virtual sections tables in debug section. It exists for diagnostic purposes
    // only and should not be touched under normal circumstances    
    m_pVirtualSectionsTable = new (GetHeap()) ZapVirtualSectionsTable(this);
    m_pDebugSection->Place(m_pVirtualSectionsTable);

#ifndef ZAP_HASHTABLE_TUNING
    Preallocate();
#endif
}

#ifdef FEATURE_READYTORUN_COMPILER
void ZapImage::InitializeSectionsForReadyToRun()
{
    AllocateVirtualSections();

    // Preload sections are not used for ready to run. Clear the pointers to them to catch accidental use.
    for (int i = 0; i < CORCOMPILE_SECTION_COUNT; i++)
        m_pPreloadSections[i] = NULL;

    m_pCorHeader = new (GetHeap()) ZapCorHeader(this);
    m_pHeaderSection->Place(m_pCorHeader);

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_COMHEADER, m_pCorHeader);

    m_pNativeHeader = new (GetHeap()) ZapReadyToRunHeader(this);
    m_pHeaderSection->Place(m_pNativeHeader);

    m_pImportSectionsTable = new (GetHeap()) ZapImportSectionsTable(this);
    m_pHeaderSection->Place(m_pImportSectionsTable);

    {
#define COMPILER_NAME "CoreCLR"

        const char * pCompilerIdentifier = COMPILER_NAME " " FX_FILEVERSION_STR " " QUOTE_MACRO(__BUILDMACHINE__);
        ZapBlob * pCompilerIdentifierBlob = new (GetHeap()) ZapBlobPtr((PVOID)pCompilerIdentifier, strlen(pCompilerIdentifier) + 1);

        GetReadyToRunHeader()->RegisterSection(READYTORUN_SECTION_COMPILER_IDENTIFIER, pCompilerIdentifierBlob);
        m_pHeaderSection->Place(pCompilerIdentifierBlob);
    }

    m_pImportTable = new (GetHeap()) ZapImportTable(this);

    for (int i=0; i<ZapImportSectionType_Total; i++)
    {
        ZapVirtualSection * pSection;
        if (i == ZapImportSectionType_Eager)
            pSection = m_pDelayLoadInfoDelayListSectionEager;
        else
        if (i < ZapImportSectionType_Cold)
            pSection = m_pDelayLoadInfoDelayListSectionHot;
        else
            pSection = m_pDelayLoadInfoDelayListSectionCold;

        m_pDelayLoadInfoDataTable[i] = new (GetHeap()) ZapImportSectionSignatures(this, m_pDelayLoadInfoTableSection[i]);
        pSection->Place(m_pDelayLoadInfoDataTable[i]);
    }

    m_pDynamicHelperDataTable = new (GetHeap()) ZapImportSectionSignatures(this, m_pDynamicHelperCellSection);
    m_pDynamicHelperDataSection->Place(m_pDynamicHelperDataTable);

    m_pExternalMethodDataTable = new (GetHeap()) ZapImportSectionSignatures(this, m_pExternalMethodCellSection, m_pGCSection);
    m_pExternalMethodDataSection->Place(m_pExternalMethodDataTable);

    m_pStubDispatchDataTable = new (GetHeap()) ZapImportSectionSignatures(this, m_pStubDispatchCellSection, m_pGCSection);
    m_pStubDispatchDataSection->Place(m_pStubDispatchDataTable);

    m_pGCInfoTable = new (GetHeap()) ZapGCInfoTable(this);

#ifdef WIN64EXCEPTIONS
    m_pUnwindDataTable = new (GetHeap()) ZapUnwindDataTable(this);
#endif

    m_pILMetaData = new (GetHeap()) ZapILMetaData(this);
    m_pILMetaDataSection->Place(m_pILMetaData);

    m_pBaseRelocs = new (GetHeap()) ZapBaseRelocs(this);
    m_pBaseRelocsSection->Place(m_pBaseRelocs);

    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_BASERELOC, m_pBaseRelocsSection);

    //
    // Initialization of auxiliary tables in alphabetical order
    //
    m_pInnerPtrs = new (GetHeap()) ZapInnerPtrTable(this);

    m_pExceptionInfoLookupTable = new (GetHeap()) ZapExceptionInfoLookupTable(this);

    //
    // Always allocate slot for module - it is used to determine that the image is used
    //
    m_pImportTable->GetPlacedHelperImport(READYTORUN_HELPER_Module);

    //
    // Make sure the import sections table is in the image, so we can find the slot for module
    //
    _ASSERTE(m_pImportSectionsTable->GetSize() != 0);
    GetReadyToRunHeader()->RegisterSection(READYTORUN_SECTION_IMPORT_SECTIONS, m_pImportSectionsTable);
}
#endif // FEATURE_READYTORUN_COMPILER


#define DATA_MEM_READONLY IMAGE_SCN_MEM_READ
#define DATA_MEM_WRITABLE IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE
#define XDATA_MEM         IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_MEM_READ | IMAGE_SCN_MEM_WRITE
#define TEXT_MEM          IMAGE_SCN_MEM_EXECUTE | IMAGE_SCN_MEM_READ

void ZapImage::AllocateVirtualSections()
{
    //
    // Allocate all virtual sections in the order they will appear in the final image
    //
    // To maximize packing of the data in the native image, the number of named physical sections is minimized -  
    // the named physical sections are used just for memory protection control. All items with the same memory
    // protection are packed together in one physical section.
    //

    {
        //
        // .data section
        //
        DWORD access = DATA_MEM_WRITABLE;

#ifdef FEATURE_LAZY_COW_PAGES
        // READYTORUN: FUTURE: Optional support for COW pages
        if (!IsReadyToRunCompilation() && CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ZapLazyCOWPagesEnabled))
            access = DATA_MEM_READONLY;
#endif

        ZapPhysicalSection * pDataSection = NewPhysicalSection(".data", IMAGE_SCN_CNT_INITIALIZED_DATA | access);

        m_pPreloadSections[CORCOMPILE_SECTION_MODULE] = NewVirtualSection(pDataSection, IBCUnProfiledSection | HotRange | ModuleSection);

        m_pEETableSection = NewVirtualSection(pDataSection, IBCUnProfiledSection | HotRange | EETableSection); // Could be marked bss if it makes sense

        // These are all known to be hot or writeable
        m_pPreloadSections[CORCOMPILE_SECTION_WRITE] = NewVirtualSection(pDataSection, IBCProfiledSection | HotRange | WriteDataSection);
        m_pPreloadSections[CORCOMPILE_SECTION_HOT_WRITEABLE] = NewVirtualSection(pDataSection, IBCProfiledSection | HotRange | WriteableDataSection); // hot for reading, potentially written to 
        m_pPreloadSections[CORCOMPILE_SECTION_WRITEABLE] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | WriteableDataSection); // Cold based on IBC profiling data.
        m_pPreloadSections[CORCOMPILE_SECTION_HOT] = NewVirtualSection(pDataSection, IBCProfiledSection | HotRange | DataSection);

        m_pPreloadSections[CORCOMPILE_SECTION_RVA_STATICS_HOT] = NewVirtualSection(pDataSection, IBCProfiledSection | HotRange | RVAStaticsSection);

        m_pDelayLoadInfoTableSection[ZapImportSectionType_Eager] = NewVirtualSection(pDataSection, IBCUnProfiledSection | HotRange | DelayLoadInfoTableEagerSection, TARGET_POINTER_SIZE);

        //
        // Allocate dynamic info tables
        //

        // Place the HOT CorCompileTables now, the cold ones would be placed later in this routine (after other HOT sections)
        for (int i=0; i<ZapImportSectionType_Count; i++)
        {
            m_pDelayLoadInfoTableSection[i] = NewVirtualSection(pDataSection, IBCProfiledSection | HotRange | DelayLoadInfoTableSection, TARGET_POINTER_SIZE);
        }

        m_pDynamicHelperCellSection = NewVirtualSection(pDataSection, IBCProfiledSection | HotColdSortedRange | ExternalMethodDataSection, TARGET_POINTER_SIZE);

        m_pExternalMethodCellSection = NewVirtualSection(pDataSection, IBCProfiledSection | HotColdSortedRange | ExternalMethodThunkSection, TARGET_POINTER_SIZE);

        // m_pStubDispatchCellSection is  deliberately placed  directly after
        // the last m_pDelayLoadInfoTableSection (all .data sections go together in the order indicated).
        // We do this to place it as the last "hot, written" section.  Why? Because
        // we don't split the dispatch cells into hot/cold sections (We probably should),
        // and so the section is actually half hot and half cold.
        // But it turns out that the hot dispatch cells always come
        // first (because the code that uses them is hot and gets compiled first).
        // Thus m_pStubDispatchCellSection contains all hot cells at the front of
        // this blob of data.  By making them last in a grouping of written data we
        // make sure the hot data is grouped with hot data in the
        // m_pDelayLoadInfoTableSection sections.

        m_pStubDispatchCellSection = NewVirtualSection(pDataSection, IBCProfiledSection | HotColdSortedRange | StubDispatchDataSection, TARGET_POINTER_SIZE);

        // Earlier we placed the HOT corCompile tables. Now place the cold ones after the stub dispatch cell section. 
        for (int i=0; i<ZapImportSectionType_Count; i++)
        {
            m_pDelayLoadInfoTableSection[ZapImportSectionType_Cold + i] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | DelayLoadInfoTableSection, TARGET_POINTER_SIZE);
        }

        //
        // Virtual sections that are moved to .cdata when we have profile data.
        //

        // This is everyhing that is assumed to be warm in the first strata
        // of non-profiled scenarios.  MethodTables related to objects etc.
        m_pPreloadSections[CORCOMPILE_SECTION_WARM] = NewVirtualSection(pDataSection, IBCProfiledSection | WarmRange | EEDataSection, TARGET_POINTER_SIZE);

        m_pPreloadSections[CORCOMPILE_SECTION_RVA_STATICS_COLD] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | RVAStaticsSection);

        // In an ideal world these are cold in both profiled and the first strata
        // of non-profiled scenarios (i.e. no reflection, etc. )  The sections at the
        // bottom correspond to further strata of non-profiled scenarios.
        m_pPreloadSections[CORCOMPILE_SECTION_CLASS_COLD] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | ClassSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_CROSS_DOMAIN_INFO] = NewVirtualSection(pDataSection, IBCUnProfiledSection | ColdRange | CrossDomainInfoSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_DESC_COLD] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | MethodDescSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_DESC_COLD_WRITEABLE] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | MethodDescWriteableSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_MODULE_COLD] = NewVirtualSection(pDataSection, IBCProfiledSection | ColdRange | ModuleSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_DEBUG_COLD] = NewVirtualSection(pDataSection, IBCUnProfiledSection | ColdRange | DebugSection, TARGET_POINTER_SIZE);

        //
        // If we're instrumenting allocate a section for writing profile data
        //
        if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR))
        {
            m_pInstrumentSection = NewVirtualSection(pDataSection, IBCUnProfiledSection | ColdRange | InstrumentSection, TARGET_POINTER_SIZE);
        }
    }

    // No RWX pages in ready to run images
    if (!IsReadyToRunCompilation())
    {
        DWORD access = XDATA_MEM;

#ifdef FEATURE_LAZY_COW_PAGES
        if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ZapLazyCOWPagesEnabled))
            access = TEXT_MEM;
#endif            

        //
        // .xdata section
        //
        ZapPhysicalSection * pXDataSection  = NewPhysicalSection(".xdata", IMAGE_SCN_CNT_INITIALIZED_DATA | access);

        // Some sections are placed in a sorted order. Hot items are placed first,
        // then cold items. These sections are marked as HotColdSortedRange since
        // they are neither completely hot, nor completely cold. 
        m_pVirtualImportThunkSection        = NewVirtualSection(pXDataSection, IBCProfiledSection | HotColdSortedRange | VirtualImportThunkSection, HELPER_TABLE_ALIGN);
        m_pExternalMethodThunkSection       = NewVirtualSection(pXDataSection, IBCProfiledSection | HotColdSortedRange | ExternalMethodThunkSection, HELPER_TABLE_ALIGN);
        m_pHelperTableSection               = NewVirtualSection(pXDataSection, IBCProfiledSection | HotColdSortedRange| HelperTableSection, HELPER_TABLE_ALIGN);

        // hot for writing, i.e. profiling has indicated a write to this item, so at least one write likely per item at some point
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_PRECODE_WRITE] = NewVirtualSection(pXDataSection, IBCProfiledSection | HotRange | MethodPrecodeWriteSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_PRECODE_HOT] = NewVirtualSection(pXDataSection, IBCProfiledSection | HotRange | MethodPrecodeSection, TARGET_POINTER_SIZE);

        //
        // cold sections
        //
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_PRECODE_COLD] = NewVirtualSection(pXDataSection, IBCProfiledSection | ColdRange | MethodPrecodeSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_METHOD_PRECODE_COLD_WRITEABLE] = NewVirtualSection(pXDataSection, IBCProfiledSection | ColdRange | MethodPrecodeWriteableSection, TARGET_POINTER_SIZE);
    }

    {
        // code:NativeUnwindInfoLookupTable::LookupUnwindInfoForMethod and code:NativeImageJitManager::GetFunctionEntry expects 
        // sentinel value right after end of .pdata section. 
        static const DWORD dwRuntimeFunctionSectionSentinel = (DWORD)-1;


        //
        // .text section
        //
#if defined(_TARGET_ARM_)
        // for ARM, put the resource section at the end if it's very large - this
        // is because b and bl instructions have a limited distance range of +-16MB
        // which we should not exceed if we can avoid it.
        // we draw the limit at 1 MB resource size, somewhat arbitrarily
        COUNT_T resourceSize;
        m_ModuleDecoder.GetResources(&resourceSize);
        BOOL bigResourceSection = resourceSize >= 1024*1024;
#endif
        ZapPhysicalSection * pTextSection = NewPhysicalSection(".text", IMAGE_SCN_CNT_CODE | TEXT_MEM);
        m_pTextSection = pTextSection;

        // Marked as HotRange since it contains items that are always touched by
        // the OS during NGEN image loading (i.e. VersionInfo) 
        m_pWin32ResourceSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | Win32ResourcesSection);

        // Marked as a HotRange since it is always touched during Ngen image load. 
        m_pHeaderSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | HeaderSection);

        // Marked as a HotRange since it is always touched during Ngen image binding.
        m_pMetaDataSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | MetadataSection);

        m_pImportTableSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | ImportTableSection, sizeof(DWORD));

        m_pDelayLoadInfoDelayListSectionEager = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | DelayLoadInfoDelayListSection, sizeof(DWORD));

        //
        // GC Info for methods which were profiled hot AND had their GC Info touched during profiling
        //
        m_pHotTouchedGCSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | GCInfoSection, sizeof(DWORD));

        m_pLazyHelperSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | HelperTableSection, MINIMUM_CODE_ALIGN);
        m_pLazyHelperSection->SetDefaultFill(DEFAULT_CODE_BUFFER_INIT);

        m_pLazyMethodCallHelperSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | HotRange | HelperTableSection, MINIMUM_CODE_ALIGN);
        m_pLazyMethodCallHelperSection->SetDefaultFill(DEFAULT_CODE_BUFFER_INIT);

        int codeSectionAlign = DEFAULT_CODE_ALIGN;

        m_pHotCodeSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | CodeSection, codeSectionAlign);
        m_pHotCodeSection->SetDefaultFill(DEFAULT_CODE_BUFFER_INIT);

#if defined(WIN64EXCEPTIONS)
        m_pHotUnwindDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | UnwindDataSection, sizeof(DWORD)); // .rdata area

        // All RuntimeFunctionSections have to be together for WIN64EXCEPTIONS
        m_pHotRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | RuntimeFunctionSection, sizeof(DWORD));  // .pdata area
        m_pRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange  | ColdRange | RuntimeFunctionSection, sizeof(DWORD));
        m_pColdRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | RuntimeFunctionSection, sizeof(DWORD));

        // The following sentinel section is just a padding for RuntimeFunctionSection - Apply same classification 
        NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | RuntimeFunctionSection, sizeof(DWORD))
            ->Place(new (GetHeap()) ZapBlobPtr((PVOID)&dwRuntimeFunctionSectionSentinel, sizeof(DWORD)));
#endif  // defined(WIN64EXCEPTIONS)

        m_pStubsSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | StubsSection);
        m_pReadOnlyDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ReadonlyDataSection);

        m_pDynamicHelperDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ExternalMethodDataSection, sizeof(DWORD));
        m_pExternalMethodDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ExternalMethodDataSection, sizeof(DWORD));
        m_pStubDispatchDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | StubDispatchDataSection, sizeof(DWORD));

        m_pHotRuntimeFunctionLookupSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | RuntimeFunctionSection, sizeof(DWORD));
#if !defined(WIN64EXCEPTIONS)
        m_pHotRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | RuntimeFunctionSection, sizeof(DWORD));

        // The following sentinel section is just a padding for RuntimeFunctionSection - Apply same classification 
        NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | RuntimeFunctionSection, sizeof(DWORD))
            ->Place(new (GetHeap()) ZapBlobPtr((PVOID)&dwRuntimeFunctionSectionSentinel, sizeof(DWORD)));
#endif
        m_pHotCodeMethodDescsSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | CodeManagerSection, sizeof(DWORD));

        m_pDelayLoadInfoDelayListSectionHot = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | DelayLoadInfoDelayListSection, sizeof(DWORD));

        //
        // The hot set of read-only data structures.  Note that read-only data structures are the things that we can (and aggressively do) intern
        // to share between different owners.  However, this can have a bad interaction with IBC, which performs its ordering optimizations without
        // knowing that NGen may jumble around layout with interning.  Thankfully, it is a relatively small percentage of the items that are duplicates
        // (many of them used a great deal to add up to large interning savings).  This means that we can track all of the interned items for which we
        // actually find any duplicates and put those in a small section.  For the rest, where there wasn't a duplicate in the entire image, we leave the
        // singleton in its normal place in the READONLY_HOT section, which was selected carefully by IBC.
        //
        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_SHARED_HOT] = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | ReadonlySharedSection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_HOT] = NewVirtualSection(pTextSection, IBCProfiledSection | HotRange | ReadonlySection, TARGET_POINTER_SIZE);

        //
        // GC Info for methods which were touched during profiling but didn't explicitly have
        // their GC Info touched during profiling
        //
        m_pHotGCSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | GCInfoSection, sizeof(DWORD));

#if !defined(_TARGET_ARM_)
        // For ARM, put these sections more towards the end because bl/b instructions have limited diplacement

        // IL
        m_pILSection  = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ILSection, sizeof(DWORD));

        //ILMetadata/Resources sections are reported as a statically known warm ranges for now.
        m_pILMetaDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ILMetadataSection, sizeof(DWORD));
#endif  // _TARGET_ARM_

#if defined(_TARGET_ARM_)
        if (!bigResourceSection) // for ARM, put the resource section at the end if it's very large - see comment above
#endif
            m_pResourcesSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | WarmRange | ResourcesSection);

        //
        // Allocate the unprofiled code section and code manager nibble map here
        //
        m_pCodeSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ColdRange | CodeSection, codeSectionAlign);
        m_pCodeSection->SetDefaultFill(DEFAULT_CODE_BUFFER_INIT);

        m_pRuntimeFunctionLookupSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ColdRange | RuntimeFunctionSection, sizeof(DWORD));
#if !defined(WIN64EXCEPTIONS)
        m_pRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange  | ColdRange | RuntimeFunctionSection, sizeof(DWORD));

        // The following sentinel section is just a padding for RuntimeFunctionSection - Apply same classification 
        NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange  | ColdRange | RuntimeFunctionSection, sizeof(DWORD))
            ->Place(new (GetHeap()) ZapBlobPtr((PVOID)&dwRuntimeFunctionSectionSentinel, sizeof(DWORD)));
#endif
        m_pCodeMethodDescsSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ColdRange | CodeHeaderSection,sizeof(DWORD));

#ifdef FEATURE_READYTORUN_COMPILER
        if (IsReadyToRunCompilation())
        {
            m_pAvailableTypesSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | WarmRange | ReadonlySection);
        }
#endif    

#if defined(WIN64EXCEPTIONS)
        m_pUnwindDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ColdRange | UnwindDataSection, sizeof(DWORD));
#endif // defined(WIN64EXCEPTIONS)

        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_WARM] = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ReadonlySection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_VCHUNKS] = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ReadonlySection, TARGET_POINTER_SIZE);
        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_DICTIONARY] = NewVirtualSection(pTextSection, IBCProfiledSection | WarmRange | ReadonlySection, TARGET_POINTER_SIZE);

        //
        // GC Info for methods which were not touched in profiling
        //
        m_pGCSection = NewVirtualSection(pTextSection, IBCProfiledSection | ColdRange | GCInfoSection, sizeof(DWORD));

        m_pDelayLoadInfoDelayListSectionCold = NewVirtualSection(pTextSection, IBCProfiledSection | ColdRange | DelayLoadInfoDelayListSection, sizeof(DWORD));

        m_pPreloadSections[CORCOMPILE_SECTION_READONLY_COLD] = NewVirtualSection(pTextSection, IBCProfiledSection | ColdRange | ReadonlySection, TARGET_POINTER_SIZE);

        //
        // Allocate the cold code section near the end of the image
        //
        m_pColdCodeSection = NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | CodeSection, codeSectionAlign);
        m_pColdCodeSection->SetDefaultFill(DEFAULT_CODE_BUFFER_INIT);

#if defined(_TARGET_ARM_)
        // For ARM, put these sections more towards the end because bl/b instructions have limited diplacement

        // IL
        m_pILSection  = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ILSection, sizeof(DWORD));

        //ILMetadata/Resources sections are reported as a statically known warm ranges for now.
        m_pILMetaDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ILMetadataSection, sizeof(DWORD));

        if (bigResourceSection) // for ARM, put the resource section at the end if it's very large - see comment above
            m_pResourcesSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | WarmRange | ResourcesSection);
#endif // _TARGET_ARM_
        m_pColdCodeMapSection = NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | CodeManagerSection, sizeof(DWORD));

#if !defined(WIN64EXCEPTIONS)
        m_pColdRuntimeFunctionSection = NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | RuntimeFunctionSection, sizeof(DWORD));

        // The following sentinel section is just a padding for RuntimeFunctionSection - Apply same classification 
        NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | RuntimeFunctionSection, sizeof(DWORD))
            ->Place(new (GetHeap()) ZapBlobPtr((PVOID)&dwRuntimeFunctionSectionSentinel, sizeof(DWORD)));
#endif

#if defined(WIN64EXCEPTIONS)
        m_pColdUnwindDataSection = NewVirtualSection(pTextSection, IBCProfiledSection | IBCUnProfiledSection | ColdRange | UnwindDataSection, sizeof(DWORD));
#endif // defined(WIN64EXCEPTIONS)

        //
        // Allocate space for compressed LookupMaps (ridmaps). This needs to come after the .data physical
        // section (which is currently true for the .text section) and late enough in the .text section to be
        // after any structure referenced by the LookupMap (current MethodTables and MethodDescs). This is a
        // hard requirement since the compression algorithm requires that all referenced data structures have
        // been laid out by the time we come to lay out the compressed nodes.
        //
        m_pPreloadSections[CORCOMPILE_SECTION_COMPRESSED_MAPS] = NewVirtualSection(pTextSection, IBCProfiledSection | ColdRange | CompressedMapsSection, sizeof(DWORD));

        m_pExceptionSection = NewVirtualSection(pTextSection, IBCProfiledSection | HotColdSortedRange | ExceptionSection, sizeof(DWORD));

        //
        // Debug info is sometimes used during exception handling to build stacktrace
        //
        m_pDebugSection = NewVirtualSection(pTextSection, IBCUnProfiledSection | ColdRange | DebugSection, sizeof(DWORD));
    }

    {
        //
        // .reloc section
        //

        ZapPhysicalSection * pRelocSection = NewPhysicalSection(".reloc", IMAGE_SCN_CNT_INITIALIZED_DATA | IMAGE_SCN_MEM_DISCARDABLE | IMAGE_SCN_MEM_READ);

        // .reloc section is always read by the OS when the image is opted in ASLR
        // (Vista+ default behavior). 
        m_pBaseRelocsSection = NewVirtualSection(pRelocSection, IBCUnProfiledSection | HotRange | BaseRelocsSection);

    }
}

void ZapImage::Preallocate()
{
    COUNT_T cbILImage = m_ModuleDecoder.GetSize();

    // Curb the estimate to handle corner cases gracefuly
    cbILImage = min(cbILImage, 50000000);

    PREALLOCATE_HASHTABLE(ZapImage::m_CompiledMethods, 0.0050, cbILImage);
    PREALLOCATE_HASHTABLE(ZapImage::m_ClassLayoutOrder, 0.0003, cbILImage);

    //
    // Preallocation of auxiliary tables in alphabetical order
    //
    m_pImportTable->Preallocate(cbILImage);
    m_pInnerPtrs->Preallocate(cbILImage);
    m_pMethodEntryPoints->Preallocate(cbILImage);
    m_pWrappers->Preallocate(cbILImage);

    if (m_pILMetaData != NULL)
        m_pILMetaData->Preallocate(cbILImage);
    m_pGCInfoTable->Preallocate(cbILImage);
#ifdef WIN64EXCEPTIONS
    m_pUnwindDataTable->Preallocate(cbILImage);
#endif // WIN64EXCEPTIONS
    m_pDebugInfoTable->Preallocate(cbILImage);
}

void ZapImage::SetVersionInfo(CORCOMPILE_VERSION_INFO * pVersionInfo)
{
    m_pVersionInfo = new (GetHeap()) ZapVersionInfo(pVersionInfo);
    m_pHeaderSection->Place(m_pVersionInfo);
}

void ZapImage::SetDependencies(CORCOMPILE_DEPENDENCY *pDependencies, DWORD cDependencies)
{
    m_pDependencies = new (GetHeap()) ZapDependencies(pDependencies, cDependencies);
    m_pHeaderSection->Place(m_pDependencies);
}

void ZapImage::SetPdbFileName(const SString &strFileName)
{
    m_pdbFileName.Set(strFileName);
}

#ifdef WIN64EXCEPTIONS
void ZapImage::SetRuntimeFunctionsDirectoryEntry()
{
    //
    // Runtime functions span multiple virtual sections and so there is no natural ZapNode * to cover them all.
    // Create dummy ZapNode * that covers them all for IMAGE_DIRECTORY_ENTRY_EXCEPTION directory entry.
    //
    ZapVirtualSection * rgRuntimeFunctionSections[] = {
        m_pHotRuntimeFunctionSection,
        m_pRuntimeFunctionSection,
        m_pColdRuntimeFunctionSection
    };

    DWORD dwTotalSize = 0, dwStartRVA = (DWORD)-1, dwEndRVA = 0;

    for (size_t i = 0; i < _countof(rgRuntimeFunctionSections); i++)
    {
        ZapVirtualSection * pSection = rgRuntimeFunctionSections[i];

        DWORD dwSize = pSection->GetSize();
        if (dwSize == 0)
            continue;

        DWORD dwRVA = pSection->GetRVA();

        dwTotalSize += dwSize;

        dwStartRVA = min(dwStartRVA, dwRVA);
        dwEndRVA = max(dwEndRVA, dwRVA + dwSize);
    }

    if (dwTotalSize != 0)
    {
        // Verify that there are no holes between the sections
        _ASSERTE(dwStartRVA + dwTotalSize == dwEndRVA);

        ZapNode * pAllRuntimeFunctionSections = new (GetHeap()) ZapDummyNode(dwTotalSize);
        pAllRuntimeFunctionSections->SetRVA(dwStartRVA);

        // Write the address of the sorted pdata to the optionalHeader.DataDirectory
        SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXCEPTION, pAllRuntimeFunctionSections);
    }
}
#endif // WIN64EXCEPTIONS

// Assign RVAs to all ZapNodes
void ZapImage::ComputeRVAs()
{
    ZapWriter::ComputeRVAs();

    if (!IsReadyToRunCompilation())
    {
        m_pMethodEntryPoints->Resolve();
        m_pWrappers->Resolve();
    }

    m_pInnerPtrs->Resolve();

#ifdef WIN64EXCEPTIONS
    SetRuntimeFunctionsDirectoryEntry();
#endif

#if defined(_DEBUG) 
#ifdef FEATURE_SYMDIFF
    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_SymDiffDump))
    {
        COUNT_T curMethod = 0;
        COUNT_T numMethods = m_MethodCompilationOrder.GetCount();

        for (; curMethod < numMethods; curMethod++)
        {
            bool fCold = false;
            //if(curMethod >= m_iUntrainedMethod) fCold = true;
    		
            ZapMethodHeader * pMethod = m_MethodCompilationOrder[curMethod];

            ZapBlobWithRelocs * pCode = fCold ? pMethod->m_pColdCode : pMethod->m_pCode;
            if (pCode == NULL)
            {            
                continue;
            }
            CORINFO_METHOD_HANDLE handle = pMethod->GetHandle();
            mdMethodDef token;
            GetCompileInfo()->GetMethodDef(handle, &token);
            GetSvcLogger()->Printf(W("(EntryPointRVAMap (MethodToken %0X) (RVA %0X) (SIZE %0X))\n"), token, pCode->GetRVA(), pCode->GetSize()); 
        }

    }
#endif // FEATURE_SYMDIFF 
#endif //_DEBUG
}

class ZapFileStream : public IStream
{
    HANDLE  m_hFile;
    MD5 m_hasher;

public:
    ZapFileStream()
        : m_hFile(INVALID_HANDLE_VALUE)
    {
        m_hasher.Init();
    }

    ~ZapFileStream()
    {
        Close();
    }

    void SetHandle(HANDLE hFile)
    {
        _ASSERTE(m_hFile == INVALID_HANDLE_VALUE);
        m_hFile = hFile;
    }

    // IUnknown methods:
    STDMETHODIMP_(ULONG) AddRef()
    {
        return 1;
    }

    STDMETHODIMP_(ULONG) Release()
    {
        return 1;
    }

    STDMETHODIMP QueryInterface(REFIID riid, LPVOID *ppv)
    {
        HRESULT hr = S_OK;
        if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IStream)) {
            *ppv = static_cast<IStream *>(this);
        }
        else {
            hr = E_NOINTERFACE;
        }
        return hr;
    }

    // ISequentialStream methods:
    STDMETHODIMP Read(void *pv, ULONG cb, ULONG *pcbRead)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Write(void const *pv, ULONG cb, ULONG *pcbWritten)
    {
        HRESULT hr = S_OK;

        _ASSERTE(m_hFile != INVALID_HANDLE_VALUE);

        m_hasher.HashMore(pv, cb);

        // We are calling with lpOverlapped == NULL so pcbWritten has to be present
        // to prevent crashes in Win7 and below.
        _ASSERTE(pcbWritten);

        if (!::WriteFile(m_hFile, pv, cb, pcbWritten, NULL))
        {
            hr = HRESULT_FROM_GetLastError();
            goto Exit;
        }

    Exit:
        return hr;
    }

    // IStream methods:
    STDMETHODIMP Seek(LARGE_INTEGER dlibMove, DWORD dwOrigin, ULARGE_INTEGER *plibNewPosition)
    {
        HRESULT hr = S_OK;        

        _ASSERTE(m_hFile != INVALID_HANDLE_VALUE);

        DWORD dwFileOrigin;
        switch (dwOrigin) {
            case STREAM_SEEK_SET:
                dwFileOrigin = FILE_BEGIN;
                break;
                
            case STREAM_SEEK_CUR:
                dwFileOrigin = FILE_CURRENT;
                break;
                
            case STREAM_SEEK_END:
                dwFileOrigin = FILE_END;
                break;
                
            default:
                hr = E_UNEXPECTED;
                goto Exit;
        }
        if (!::SetFilePointerEx(m_hFile, dlibMove, (LARGE_INTEGER *)plibNewPosition, dwFileOrigin))
        {
            hr = HRESULT_FROM_GetLastError();
            goto Exit;
        }

    Exit:
        return hr;
    }

    STDMETHODIMP SetSize(ULARGE_INTEGER libNewSize)
    {
        HRESULT hr = S_OK;

        _ASSERTE(m_hFile != INVALID_HANDLE_VALUE);

        hr = Seek(*(LARGE_INTEGER *)&libNewSize, FILE_BEGIN, NULL);
        if (FAILED(hr))
        {
            goto Exit;
        }

        if (!::SetEndOfFile(m_hFile))
        {
            hr = HRESULT_FROM_GetLastError();
            goto Exit;
        }

    Exit:
        return hr;
    }

    STDMETHODIMP CopyTo(IStream *pstm, ULARGE_INTEGER cb, ULARGE_INTEGER *pcbRead, ULARGE_INTEGER *pcbWritten)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Commit(DWORD grfCommitFlags)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Revert()
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, DWORD dwLockType)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Stat(STATSTG *pstatstg, DWORD grfStatFlag)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    STDMETHODIMP Clone(IStream **ppIStream)
    {
        _ASSERTE(false);
        return E_NOTIMPL;
    }

    HRESULT Close()
    {
        HRESULT hr = S_OK;

        HANDLE hFile = m_hFile;
        if (hFile != INVALID_HANDLE_VALUE)
        {
            m_hFile = INVALID_HANDLE_VALUE;

            if (!::CloseHandle(hFile))
            {
                hr = HRESULT_FROM_GetLastError();
                goto Exit;
            }
        }

    Exit:
        return hr;
    }

    void SuppressClose()
    {
        m_hFile = INVALID_HANDLE_VALUE;
    }

    void GetHash(MD5HASHDATA* pHash)
    {
        m_hasher.GetHashValue(pHash);
    }
};

HANDLE ZapImage::GenerateFile(LPCWSTR wszOutputFileName, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    ZapFileStream outputStream;

    HANDLE hFile = WszCreateFile(wszOutputFileName,
                        GENERIC_READ | GENERIC_WRITE,
                        FILE_SHARE_READ | FILE_SHARE_DELETE,
                        NULL,
                        CREATE_ALWAYS,
                        FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                        NULL);

    if (hFile == INVALID_HANDLE_VALUE)
        ThrowLastError();

    outputStream.SetHandle(hFile);

    Save(&outputStream);

    LARGE_INTEGER filePos;

    if (m_pNativeHeader != NULL)
    {
        // Write back the updated CORCOMPILE_HEADER (relocs and guid is not correct the first time around)
        filePos.QuadPart = m_pTextSection->GetFilePos() + 
            (m_pNativeHeader->GetRVA() - m_pTextSection->GetRVA());
        IfFailThrow(outputStream.Seek(filePos, STREAM_SEEK_SET, NULL));
        m_pNativeHeader->Save(this);
        FlushWriter();
    }

    GUID signature = {0};

    static_assert_no_msg(sizeof(GUID) == sizeof(MD5HASHDATA));
    outputStream.GetHash((MD5HASHDATA*)&signature);

    {    
        // Write the debug directory entry for the NGEN PDB
        RSDS rsds = {0};

        rsds.magic = VAL32(0x53445352); // "SDSR";
        rsds.age = 1;
        // our PDB signature will be the same as our NGEN signature.  
        // However we want the printed version of the GUID to be be the same as the
        // byte dump of the signature so we swap bytes to make this work.  
        // 
        // * See code:CCorSvcMgr::CreatePdb for where this is used.
        BYTE* asBytes = (BYTE*) &signature;
        rsds.signature.Data1 = ((asBytes[0] * 256 + asBytes[1]) * 256 + asBytes[2]) * 256 + asBytes[3];
        rsds.signature.Data2 = asBytes[4] * 256 + asBytes[5];
        rsds.signature.Data3 = asBytes[6] * 256 + asBytes[7];
        memcpy(&rsds.signature.Data4, &asBytes[8], 8);

        _ASSERTE(!m_pdbFileName.IsEmpty());
        ZeroMemory(&rsds.path[0], sizeof(rsds.path));
        if (WideCharToMultiByte(CP_UTF8, 
                                0, 
                                m_pdbFileName.GetUnicode(),
                                m_pdbFileName.GetCount(), 
                                &rsds.path[0], 
                                sizeof(rsds.path) - 1, // -1 to keep the buffer zero terminated
                                NULL, 
                                NULL) == 0)
            ThrowHR(E_FAIL);
        
        ULONG cbWritten = 0;
        filePos.QuadPart = m_pTextSection->GetFilePos() + (m_pNGenPdbDebugData->GetRVA() - m_pTextSection->GetRVA());
        IfFailThrow(outputStream.Seek(filePos, STREAM_SEEK_SET, NULL));
        IfFailThrow(outputStream.Write(&rsds, sizeof rsds, &cbWritten));
    }

    if (m_pVersionInfo != NULL)
    {
        ULONG cbWritten;

        filePos.QuadPart = m_pTextSection->GetFilePos() + 
            (m_pVersionInfo->GetRVA() - m_pTextSection->GetRVA()) + 
            offsetof(CORCOMPILE_VERSION_INFO, signature);
        IfFailThrow(outputStream.Seek(filePos, STREAM_SEEK_SET, NULL));
        IfFailThrow(outputStream.Write(&signature, sizeof(signature), &cbWritten));

        if (pNativeImageSig != NULL)
            *pNativeImageSig = signature;
    }
    else
    {
        _ASSERTE(pNativeImageSig == NULL);
    }

    outputStream.SuppressClose();
    return hFile;
}


HANDLE ZapImage::SaveImage(LPCWSTR wszOutputFileName, LPCWSTR wszDllPath, CORCOMPILE_NGEN_SIGNATURE * pNativeImageSig)
{
    if(!IsReadyToRunCompilation() || IsLargeVersionBubbleEnabled())
    {
        OutputManifestMetadata();
    }

    OutputTables();

    // Create a empty export table.  This makes tools like symchk not think
    // that native images are resoure-only DLLs.  It is important to NOT
    // be a resource-only DLL because those DLL's PDBS are not put up on the
    // symbol server and we want NEN PDBS to be placed there.  
    ZapPEExports* exports = new(GetHeap()) ZapPEExports(wszDllPath);
    m_pDebugSection->Place(exports);
    SetDirectoryEntry(IMAGE_DIRECTORY_ENTRY_EXPORT, exports);
    
    ComputeRVAs();

    if (!IsReadyToRunCompilation())
    {
        m_pPreloader->FixupRVAs();
    }

    HANDLE hFile = GenerateFile(wszOutputFileName, pNativeImageSig);

    if (m_zapper->m_pOpt->m_verbose)
    {
        PrintStats(wszOutputFileName);
    }

    return hFile;
}

void ZapImage::PrintStats(LPCWSTR wszOutputFileName)
{
#define ACCUM_SIZE(dest, src) if( src != NULL ) dest+= src->GetSize()
    ACCUM_SIZE(m_stats->m_gcInfoSize, m_pHotTouchedGCSection);
    ACCUM_SIZE(m_stats->m_gcInfoSize, m_pHotGCSection);
    ACCUM_SIZE(m_stats->m_gcInfoSize, m_pGCSection);
#if defined(WIN64EXCEPTIONS)
    ACCUM_SIZE(m_stats->m_unwindInfoSize, m_pUnwindDataSection);
    ACCUM_SIZE(m_stats->m_unwindInfoSize, m_pHotRuntimeFunctionSection);
    ACCUM_SIZE(m_stats->m_unwindInfoSize, m_pRuntimeFunctionSection);
    ACCUM_SIZE(m_stats->m_unwindInfoSize, m_pColdRuntimeFunctionSection);
#endif // defined(WIN64EXCEPTIONS)

    //
    // Get the size of the input & output files
    //

    {
        WIN32_FIND_DATA inputData;
        FindHandleHolder inputHandle = WszFindFirstFile(m_pModuleFileName, &inputData);
        if (inputHandle != INVALID_HANDLE_VALUE)
            m_stats->m_inputFileSize = inputData.nFileSizeLow;
    }

    {
        WIN32_FIND_DATA outputData;
        FindHandleHolder outputHandle = WszFindFirstFile(wszOutputFileName, &outputData);
        if (outputHandle != INVALID_HANDLE_VALUE)
            m_stats->m_outputFileSize = outputData.nFileSizeLow;
    }

    ACCUM_SIZE(m_stats->m_metadataSize, m_pAssemblyMetaData);

    DWORD dwPreloadSize = 0;
    for (int iSection = 0; iSection < CORCOMPILE_SECTION_COUNT; iSection++)
        ACCUM_SIZE(dwPreloadSize, m_pPreloadSections[iSection]);
    m_stats->m_preloadImageSize = dwPreloadSize;

    ACCUM_SIZE(m_stats->m_hotCodeMgrSize, m_pHotCodeMethodDescsSection);
    ACCUM_SIZE(m_stats->m_unprofiledCodeMgrSize, m_pCodeMethodDescsSection);
    ACCUM_SIZE(m_stats->m_coldCodeMgrSize, m_pHotRuntimeFunctionLookupSection);

    ACCUM_SIZE(m_stats->m_eeInfoTableSize, m_pEEInfoTable);
    ACCUM_SIZE(m_stats->m_helperTableSize, m_pHelperTableSection);
    ACCUM_SIZE(m_stats->m_dynamicInfoTableSize, m_pImportSectionsTable);

    ACCUM_SIZE(m_stats->m_dynamicInfoDelayListSize, m_pDelayLoadInfoDelayListSectionEager);
    ACCUM_SIZE(m_stats->m_dynamicInfoDelayListSize, m_pDelayLoadInfoDelayListSectionHot);
    ACCUM_SIZE(m_stats->m_dynamicInfoDelayListSize, m_pDelayLoadInfoDelayListSectionCold);

    ACCUM_SIZE(m_stats->m_debuggingTableSize, m_pDebugSection);
    ACCUM_SIZE(m_stats->m_headerSectionSize, m_pGCSection);
    ACCUM_SIZE(m_stats->m_codeSectionSize, m_pHotCodeSection);
    ACCUM_SIZE(m_stats->m_coldCodeSectionSize, m_pColdCodeSection);
    ACCUM_SIZE(m_stats->m_exceptionSectionSize, m_pExceptionSection);
    ACCUM_SIZE(m_stats->m_readOnlyDataSectionSize, m_pReadOnlyDataSection);
    ACCUM_SIZE(m_stats->m_relocSectionSize, m_pBaseRelocsSection);
    ACCUM_SIZE(m_stats->m_ILMetadataSize, m_pILMetaData);
    ACCUM_SIZE(m_stats->m_virtualImportThunkSize, m_pVirtualImportThunkSection);
    ACCUM_SIZE(m_stats->m_externalMethodThunkSize, m_pExternalMethodThunkSection);
    ACCUM_SIZE(m_stats->m_externalMethodDataSize, m_pExternalMethodDataSection);
#undef ACCUM_SIZE

    if (m_stats->m_failedMethods)
        m_zapper->Warning(W("Warning: %d methods (%d%%) could not be compiled.\n"),
                          m_stats->m_failedMethods, (m_stats->m_failedMethods*100) / m_stats->m_methods);
    if (m_stats->m_failedILStubs)
        m_zapper->Warning(W("Warning: %d IL STUB methods could not be compiled.\n"),
                          m_stats->m_failedMethods);
    m_stats->PrintStats();
}

// Align native images to 64K
const SIZE_T BASE_ADDRESS_ALIGNMENT  = 0xffff;
const double CODE_EXPANSION_FACTOR   =  3.6;

void ZapImage::CalculateZapBaseAddress()
{
    static SIZE_T nextBaseAddressForMultiModule;

    SIZE_T baseAddress = 0;

    {
        // Read the actual preferred base address from the disk

        // Note that we are reopening the file here. We are not guaranteed to get the same file.
        // The worst thing that can happen is that we will read a bogus preferred base address from the file.
        HandleHolder hFile(WszCreateFile(m_pModuleFileName,
                                            GENERIC_READ,
                                            FILE_SHARE_READ|FILE_SHARE_DELETE,
                                            NULL,
                                            OPEN_EXISTING,
                                            FILE_ATTRIBUTE_NORMAL,
                                            NULL));
        if (hFile == INVALID_HANDLE_VALUE)
            ThrowLastError();

        HandleHolder hFileMap(WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL));
        if (hFileMap == NULL)
            ThrowLastError();

        MapViewHolder base(MapViewOfFile(hFileMap, FILE_MAP_READ, 0, 0, 0));
        if (base == NULL)
            ThrowLastError();
    
        DWORD dwFileLen = SafeGetFileSize(hFile, 0);
        if (dwFileLen == INVALID_FILE_SIZE)
            ThrowLastError();

        PEDecoder peFlat((void *)base, (COUNT_T)dwFileLen);

        baseAddress = (SIZE_T) peFlat.GetPreferredBase();
    }

    // See if the header has the linker's default preferred base address
    if (baseAddress == (SIZE_T) 0x00400000)
    {
        if (m_fManifestModule)
        {
            // Set the base address for the main assembly with the manifest
        
            if (!m_ModuleDecoder.IsDll())
            {
#if defined(_TARGET_X86_)
                // We use 30000000 for an exe
                baseAddress = 0x30000000;
#elif defined(_TARGET_64BIT_)
                // We use 04000000 for an exe
                // which is remapped to 0x642`88000000 on x64
                baseAddress = 0x04000000;
#endif
            }
            else
            {
#if defined(_TARGET_X86_)
                // We start a 31000000 for the main assembly with the manifest
                baseAddress = 0x31000000;
#elif defined(_TARGET_64BIT_)
                // We start a 05000000 for the main assembly with the manifest
                // which is remapped to 0x642`8A000000 on x64
                baseAddress = 0x05000000;
#endif
            }
        }
        else // is dependent assembly of a multi-module assembly
        {
            // Set the base address for a dependant multi module assembly
                
            // We should have already set the nextBaseAddressForMultiModule
            // when we compiled the manifest module
            _ASSERTE(nextBaseAddressForMultiModule != 0);
            baseAddress = nextBaseAddressForMultiModule;
        }
    }
    else 
    {
        //
        // For some assemblies we have to move the ngen image base address up
        // past the end of IL image so that that we don't have a conflict.
        //
        // CoreCLR currently always loads both the IL and the native image, so
        // move the native image out of the way.
        {
            baseAddress += m_ModuleDecoder.GetVirtualSize();
        }
    }

    // Round to a multiple of 64K
    // 64K is the allocation granularity of VirtualAlloc. (Officially this number is not a constant -
    // we should be querying the system for its allocation granularity, but we do this all over the place
    // currently.)

    baseAddress = (baseAddress + BASE_ADDRESS_ALIGNMENT) & ~BASE_ADDRESS_ALIGNMENT;

    //
    // Calculate the nextBaseAddressForMultiModule
    //
    SIZE_T tempBaseAddress = baseAddress;
    tempBaseAddress += (SIZE_T) (CODE_EXPANSION_FACTOR * (double) m_ModuleDecoder.GetVirtualSize());
    tempBaseAddress += BASE_ADDRESS_ALIGNMENT;
    tempBaseAddress = (tempBaseAddress + BASE_ADDRESS_ALIGNMENT) & ~BASE_ADDRESS_ALIGNMENT;
    
    nextBaseAddressForMultiModule = tempBaseAddress;

    //
    // Now we remap the 32-bit address range used for x86 and PE32 images into thre
    // upper address range used on 64-bit platforms
    //
#if USE_UPPER_ADDRESS
#if defined(_TARGET_64BIT_)
    if (baseAddress < 0x80000000)
    {
        if (baseAddress < 0x40000000)
            baseAddress += 0x40000000; // We map [00000000..3fffffff] to [642'80000000..642'ffffffff]
        else
            baseAddress -= 0x40000000; // We map [40000000..7fffffff] to [642'00000000..642'7fffffff]

        baseAddress *= UPPER_ADDRESS_MAPPING_FACTOR;
        baseAddress += CLR_UPPER_ADDRESS_MIN;
    }
#endif
#endif


    // Apply the calculated base address.
    SetBaseAddress(baseAddress);

    m_NativeBaseAddress = baseAddress;
}

void ZapImage::Open(CORINFO_MODULE_HANDLE hModule,
                        IMetaDataAssemblyEmit *pEmit)
{
    m_hModule   = hModule;
    m_fManifestModule = (hModule == m_zapper->m_pEECompileInfo->GetAssemblyModule(m_zapper->m_hAssembly));

    m_ModuleDecoder = *m_zapper->m_pEECompileInfo->GetModuleDecoder(hModule);


    //
    // Get file name, and base address from module
    //

    StackSString moduleFileName;
    m_zapper->m_pEECompileInfo->GetModuleFileName(hModule, moduleFileName);

    DWORD fileNameLength = moduleFileName.GetCount();
    m_pModuleFileName = new WCHAR[fileNameLength+1];
    wcscpy_s(m_pModuleFileName, fileNameLength+1, moduleFileName.GetUnicode());

    //
    // Load the IBC Profile data for the assembly if it exists
    // 
    LoadProfileData();

    //
    // Get metadata of module to be compiled
    //
    m_pMDImport = m_zapper->m_pEECompileInfo->GetModuleMetaDataImport(m_hModule);
    _ASSERTE(m_pMDImport != NULL);

    //
    // Open new assembly metadata data for writing.  We may not use it,
    // if so we'll just discard it at the end.
    //
    if (pEmit != NULL)
    {
        pEmit->AddRef();
        m_pAssemblyEmit = pEmit;
    }
    else
    {
        // Hardwire the metadata version to be the current runtime version so that the ngen image
        // does not change when the directory runtime is installed in different directory (e.g. v2.0.x86chk vs. v2.0.80826).
        BSTRHolder strVersion(SysAllocString(W("v")VER_PRODUCTVERSION_NO_QFE_STR_L));
        VARIANT versionOption;
        V_VT(&versionOption) = VT_BSTR;
        V_BSTR(&versionOption) = strVersion;
        IfFailThrow(m_zapper->m_pMetaDataDispenser->SetOption(MetaDataRuntimeVersion, &versionOption));

        IfFailThrow(m_zapper->m_pMetaDataDispenser->
                    DefineScope(CLSID_CorMetaDataRuntime, 0, IID_IMetaDataAssemblyEmit,
                                (IUnknown **) &m_pAssemblyEmit));
    }

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        InitializeSectionsForReadyToRun();
    }
    else
#endif
    {
        InitializeSections();
    }

    // Set the module base address for the ngen native image
    CalculateZapBaseAddress();
}




//
// Load the module and populate all the data-structures
//

void ZapImage::Preload()
{

    CorProfileData *  pProfileData = NewProfileData();
    m_pPreloader = m_zapper->m_pEECompileInfo->PreloadModule(m_hModule, this, pProfileData);
}

//
// Store the module
//

void ZapImage::LinkPreload()
{
    m_pPreloader->Link();
}

void ZapImage::OutputManifestMetadata()
{
    //
    // Write out manifest metadata
    //

    //
    // First, see if we have useful metadata to store
    //

    BOOL fMetadata = FALSE;

    if (m_pAssemblyEmit != NULL)
    {
        //
        // We may have added some assembly refs for exports.
        //

        NonVMComHolder<IMetaDataAssemblyImport> pAssemblyImport;
        IfFailThrow(m_pAssemblyEmit->QueryInterface(IID_IMetaDataAssemblyImport,
                                                    (void **)&pAssemblyImport));

        NonVMComHolder<IMetaDataImport> pImport;
        IfFailThrow(m_pAssemblyEmit->QueryInterface(IID_IMetaDataImport,
                                                    (void **)&pImport));

        HCORENUM hEnum = 0;
        ULONG cRefs;
        IfFailThrow(pAssemblyImport->EnumAssemblyRefs(&hEnum, NULL, 0, &cRefs));
        IfFailThrow(pImport->CountEnum(hEnum, &cRefs));
        pImport->CloseEnum(hEnum);

        if (cRefs > 0)
            fMetadata = TRUE;

        //
        // If we are the main module, we have the assembly def for the zap file.
        //

        mdAssembly a;
        if (pAssemblyImport->GetAssemblyFromScope(&a) == S_OK)
            fMetadata = TRUE;
    }

    if (fMetadata)
    {
        // Metadata creates a new MVID for every instantiation.
        // However, we want the generated ngen image to always be the same
        // for the same input. So set the metadata MVID to NGEN_IMAGE_MVID.

        NonVMComHolder<IMDInternalEmit> pMDInternalEmit;
        IfFailThrow(m_pAssemblyEmit->QueryInterface(IID_IMDInternalEmit,
                                                  (void**)&pMDInternalEmit));

        IfFailThrow(pMDInternalEmit->ChangeMvid(NGEN_IMAGE_MVID));

        m_pAssemblyMetaData = new (GetHeap()) ZapMetaData();
        m_pAssemblyMetaData->SetMetaData(m_pAssemblyEmit);

        m_pMetaDataSection->Place(m_pAssemblyMetaData);
    }
}

void ZapImage::OutputTables()
{
    //
    // Copy over any resources to the native image
    //

    COUNT_T size;
    PVOID resource = (PVOID)m_ModuleDecoder.GetResources(&size);

    if (size != 0)
    {
        m_pResources = new (GetHeap()) ZapBlobPtr(resource, size);
        m_pResourcesSection->Place(m_pResources);
    }

    CopyDebugDirEntry();
    CopyWin32Resources();

    if (m_pILMetaData != NULL)
    {
        m_pILMetaData->CopyIL();
        m_pILMetaData->CopyMetaData();
    }

    if (IsReadyToRunCompilation())
    {
        m_pILMetaData->CopyRVAFields();
    }

    // Copy over the timestamp from IL image for determinism
    SetTimeDateStamp(m_ModuleDecoder.GetTimeDateStamp());

    SetSubsystem(m_ModuleDecoder.GetSubsystem());

    {
        USHORT dllCharacteristics = 0;

#ifndef _TARGET_64BIT_
        dllCharacteristics |= IMAGE_DLLCHARACTERISTICS_NO_SEH;
#endif

#ifdef _TARGET_ARM_
        // Images without NX compat bit set fail to load on ARM
        dllCharacteristics |= IMAGE_DLLCHARACTERISTICS_NX_COMPAT;
#endif

        // Copy over selected DLL characteristics bits from IL image
        dllCharacteristics |= (m_ModuleDecoder.GetDllCharacteristics() & 
            (IMAGE_DLLCHARACTERISTICS_NX_COMPAT | IMAGE_DLLCHARACTERISTICS_TERMINAL_SERVER_AWARE | IMAGE_DLLCHARACTERISTICS_APPCONTAINER));

#ifdef _DEBUG
        if (0 == CLRConfig::GetConfigValue(CLRConfig::INTERNAL_NoASLRForNgen))
#endif // _DEBUG
        {
            dllCharacteristics |= IMAGE_DLLCHARACTERISTICS_DYNAMIC_BASE;
#ifdef _TARGET_64BIT_
            // Large address aware, required for High Entry VA, is always enabled for 64bit native images.
            dllCharacteristics |= IMAGE_DLLCHARACTERISTICS_HIGH_ENTROPY_VA;
#endif
        }

        SetDllCharacteristics(dllCharacteristics);
    }

    if (IsReadyToRunCompilation())
    {

        SetSizeOfStackReserve(m_ModuleDecoder.GetSizeOfStackReserve());
        SetSizeOfStackCommit(m_ModuleDecoder.GetSizeOfStackCommit());
    }

#if defined(FEATURE_PAL) && !defined(_TARGET_64BIT_)
    // To minimize wasted VA space on 32 bit systems align file to page bounaries (presumed to be 4K).
    SetFileAlignment(0x1000);
#elif defined(_TARGET_ARM_) && defined(FEATURE_CORESYSTEM)
    if (!IsReadyToRunCompilation())
    {
        // On ARM CoreSys builds, crossgen will use 4k file alignment, as requested by Phone perf team
        // to improve perf on phones with compressed system partitions.
        SetFileAlignment(0x1000);
    }
#endif
}

ZapImage::CompileStatus ZapImage::CompileProfileDataWorker(mdToken token, unsigned methodProfilingDataFlags)
{
    if ((TypeFromToken(token) != mdtMethodDef) ||
        (!m_pMDImport->IsValidToken(token)))
    {
        m_zapper->Info(W("Warning: Invalid method token %08x in profile data.\n"), token);
        return NOT_COMPILED;
    }

#ifdef _DEBUG
    static ConfigDWORD g_NgenOrder;

    if ((g_NgenOrder.val(CLRConfig::INTERNAL_NgenOrder) & 2) == 2)
    {
        const ProfileDataHashEntry * foundEntry = profileDataHashTable.LookupPtr(token);
    
        if (foundEntry == NULL)
            return NOT_COMPILED;

        // The md must match.
        _ASSERTE(foundEntry->md == token); 
        // The target position cannot be 0.
        _ASSERTE(foundEntry->pos > 0);
    }
#endif

    // Now compile the method
    return TryCompileMethodDef(token, methodProfilingDataFlags);
}

//  ProfileDisableInlining
//     Before we start compiling any methods we may need to suppress the inlining
//     of certain methods based upon our profile data.
//     This method will arrange to disable this inlining.
//
void ZapImage::ProfileDisableInlining()
{
    // We suppress the inlining of any Hot methods that have the ExcludeHotMethodCode flag.
    // We want such methods to be Jitted at runtime rather than compiled in the AOT native image.
    // The inlining of such a method also need to be suppressed.
    //
    ProfileDataSection* methodProfileData = &(m_profileDataSections[MethodProfilingData]);
    if (methodProfileData->tableSize > 0)
    {
        for (DWORD i = 0; i < methodProfileData->tableSize; i++)
        {
            CORBBTPROF_TOKEN_INFO * pTokenInfo = &(methodProfileData->pTable[i]);
            unsigned methodProfilingDataFlags = pTokenInfo->flags;

            // Hot methods can be marked to be excluded from the AOT native image.
            // We also need to disable inlining of such methods.
            //
            if ((methodProfilingDataFlags & (1 << DisableInlining)) != 0)
            {
                // Disable the inlining of this method
                //
                // @ToDo: Figure out how to disable inlining for this method.               
            }
        }
    }
}

//  CompileHotRegion
//     Performs the compilation and placement for all methods in the the "Hot" code region
//     Methods placed in this region typically correspond to all of the methods that were
//     executed during any of the profiling scenarios.
//
void ZapImage::CompileHotRegion()
{
    // Compile all of the methods that were executed during profiling into the "Hot" code region.
    //
    BeginRegion(CORINFO_REGION_HOT);

    CorProfileData* pProfileData = GetProfileData();
        
    ProfileDataSection* methodProfileData = &(m_profileDataSections[MethodProfilingData]);
    if (methodProfileData->tableSize > 0)
    {
        // record the start of hot IBC methods.
        m_iIBCMethod = m_MethodCompilationOrder.GetCount();

        //
        // Compile the hot methods in the order specified in the MethodProfilingData
        //
        for (DWORD i = 0; i < methodProfileData->tableSize; i++)
        {
            CompileStatus compileResult = NOT_COMPILED;
            CORBBTPROF_TOKEN_INFO * pTokenInfo = &(methodProfileData->pTable[i]);

            mdToken token = pTokenInfo->token;
            unsigned methodProfilingDataFlags = pTokenInfo->flags;
            _ASSERTE(methodProfilingDataFlags != 0);

            if (TypeFromToken(token) == mdtMethodDef)
            {
                //
                // Compile a non-generic method
                // 
                compileResult = CompileProfileDataWorker(token, methodProfilingDataFlags);
            }
            else if (TypeFromToken(token) == ibcMethodSpec)
            {
                //
                //  compile a generic/parameterized method
                // 
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY *pBlobSigEntry = pProfileData->GetBlobSigEntry(token);
                
                if (pBlobSigEntry == NULL)
                {
                    m_zapper->Info(W("Warning: Did not find definition for method token %08x in profile data.\n"), token);
                }
                else // (pBlobSigEntry  != NULL)
                {
                    _ASSERTE(pBlobSigEntry->blob.token == token);

                    // decode method desc
                    CORINFO_METHOD_HANDLE pMethod = m_pPreloader->FindMethodForProfileEntry(pBlobSigEntry);
                   
                    if (pMethod)
                    {
                        m_pPreloader->AddMethodToTransitiveClosureOfInstantiations(pMethod);

                        compileResult = TryCompileInstantiatedMethod(pMethod, methodProfilingDataFlags);
                    }
                    else
                    {
                        // This generic/parameterized method is not part of the native image
                        // Either the IBC type  specified no longer exists or it is a SIMD types
                        // or the type can't be loaded in a ReadyToRun native image because of
                        // a cross-module type dependencies.
                        //
                        compileResult = COMPILE_EXCLUDED;
                    }
                }
            }

            // Update the 'flags' and 'compileResult' saved in the profileDataHashTable hash table.
            //
            hashBBUpdateFlagsAndCompileResult(token, methodProfilingDataFlags, compileResult);
        }
        // record the start of hot Generics methods.
        m_iGenericsMethod = m_MethodCompilationOrder.GetCount();
    }

    // record the start of untrained code
    m_iUntrainedMethod = m_MethodCompilationOrder.GetCount();

    EndRegion(CORINFO_REGION_HOT);
}

//  CompileColdRegion
//     Performs the compilation and placement for all methods in the the "Cold" code region
//     Methods placed in this region typically correspond to all of the methods that were
//     NOT executed during any of the profiling scenarios.
//
void ZapImage::CompileColdRegion()
{
    // Compile all of the methods that were NOT executed during profiling into the "Cold" code region.
    //

    BeginRegion(CORINFO_REGION_COLD);
    
    IMDInternalImport * pMDImport = m_pMDImport;
    
    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);
    
    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
    {
        //
        // Compile the remaining methods that weren't compiled during the CompileHotRegion phase
        //
        TryCompileMethodDef(md, 0);
    }

    // Compile any generic code which lands in this LoaderModule
    // that resulted from the above compilations
    CORINFO_METHOD_HANDLE handle = m_pPreloader->NextUncompiledMethod();
    while (handle != NULL)
    {
        TryCompileInstantiatedMethod(handle, 0);
        handle = m_pPreloader->NextUncompiledMethod();
    }
    
    EndRegion(CORINFO_REGION_COLD);
}

//  PlaceMethodIL
//     Copy the IL for all method into the AOT native image
//
void ZapImage::PlaceMethodIL()
{
    // Place the IL for all of the methods 
    //
    IMDInternalImport * pMDImport = m_pMDImport;
    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtMethodDef);
    
    mdMethodDef md;
    while (pMDImport->EnumNext(&hEnum, &md))
    {
        if (m_pILMetaData != NULL)
        {
            // Copy IL for all methods. We treat errors during copying IL 
            // over as fatal error. These errors are typically caused by 
            // corrupted IL images.
            // 
            m_pILMetaData->EmitMethodIL(md);
        }
    }
}

void ZapImage::Compile()
{
    //
    // Compile all of the methods for our AOT native image
    //

    bool doNothingNgen = false;
#ifdef _DEBUG
    static ConfigDWORD fDoNothingNGen;
    doNothingNgen = !!fDoNothingNGen.val(CLRConfig::INTERNAL_ZapDoNothing);
#endif

    ProfileDisableInlining();

    if (!doNothingNgen)
    {
        CompileHotRegion();

        CompileColdRegion();
    }

    PlaceMethodIL();

    // Compute a preferred class layout order based on analyzing the graph
    // of which classes contain calls to other classes.
    ComputeClassLayoutOrder();

    // Sort the unprofiled methods by this preferred class layout, if available
    if (m_fHasClassLayoutOrder)
    {
        SortUnprofiledMethodsByClassLayoutOrder();
    }

    if (IsReadyToRunCompilation())
    {
        // Pretend that no methods are trained, so that everything is in single code section
        // READYTORUN: FUTURE: More than one code section
        m_iUntrainedMethod = 0;
    }

    OutputCode(ProfiledHot);
    OutputCode(Unprofiled);
    OutputCode(ProfiledCold);

    OutputCodeInfo(ProfiledHot);
    OutputCodeInfo(ProfiledCold);  // actually both Unprofiled and ProfiledCold

    OutputGCInfo();
    OutputProfileData();

#ifdef FEATURE_READYTORUN_COMPILER
    if (IsReadyToRunCompilation())
    {
        OutputEntrypointsTableForReadyToRun();
        OutputDebugInfoForReadyToRun();
        OutputTypesTableForReadyToRun(m_pMDImport);
        OutputInliningTableForReadyToRun();
        OutputProfileDataForReadyToRun();
        if (IsLargeVersionBubbleEnabled())
        {
            OutputManifestMetadataForReadyToRun();
        }
    }
    else
#endif
    {
        OutputDebugInfo();
    }
}

struct CompileMethodStubContext
{
    ZapImage *                  pImage;
    unsigned                    methodProfilingDataFlags;
    ZapImage::CompileStatus     enumCompileStubResult;

    CompileMethodStubContext(ZapImage * _image, unsigned _methodProfilingDataFlags)
    {
        pImage                   = _image;
        methodProfilingDataFlags = _methodProfilingDataFlags;
        enumCompileStubResult    = ZapImage::NOT_COMPILED;
    }
};

//-----------------------------------------------------------------------------
// This method is a callback function use to compile any IL_STUBS that are
// associated with a normal IL method.  It is called from CompileMethodStubIfNeeded
// via the function pointer stored in the CompileMethodStubContext.
// It handles the temporary change to the m_compilerFlags and removes any flags
// that we don't want set when compiling IL_STUBS.
//-----------------------------------------------------------------------------

// static void __stdcall 
void ZapImage::TryCompileMethodStub(LPVOID pContext, CORINFO_METHOD_HANDLE hStub, CORJIT_FLAGS jitFlags)
{
    STANDARD_VM_CONTRACT;

    // The caller must always set the IL_STUB flag
    _ASSERTE(jitFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB));

    CompileMethodStubContext *pCompileContext = reinterpret_cast<CompileMethodStubContext *>(pContext);
    ZapImage *pImage = pCompileContext->pImage;

    CORJIT_FLAGS oldFlags = pImage->m_zapper->m_pOpt->m_compilerFlags;

    CORJIT_FLAGS* pCompilerFlags = &pImage->m_zapper->m_pOpt->m_compilerFlags;
    pCompilerFlags->Add(jitFlags);
    pCompilerFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE);
    pCompilerFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_CODE);
    pCompilerFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_EnC);
    pCompilerFlags->Clear(CORJIT_FLAGS::CORJIT_FLAG_DEBUG_INFO);

    mdMethodDef md = mdMethodDefNil;

    pCompileContext->enumCompileStubResult = pImage->TryCompileMethodWorker(hStub, md,
                                                         pCompileContext->methodProfilingDataFlags);

    pImage->m_zapper->m_pOpt->m_compilerFlags = oldFlags;
}

//-----------------------------------------------------------------------------
// Helper for ZapImage::TryCompileMethodDef that indicates whether a given method def token refers to a
// "vtable gap" method. These are pseudo-methods used to lay out the vtable for COM interop and as such don't
// have any associated code (or even a method handle).
//-----------------------------------------------------------------------------
BOOL ZapImage::IsVTableGapMethod(mdMethodDef md)
{
#ifdef FEATURE_COMINTEROP 
    HRESULT hr;
    DWORD dwAttributes;

    // Get method attributes and check that RTSpecialName was set for the method (this means the name has
    // semantic import to the runtime and must be formatted rigorously with one of a few well known rules).
    // Note that we just return false on any failure path since this will just lead to our caller continuing
    // to throw the exception they were about to anyway.
    hr = m_pMDImport->GetMethodDefProps(md, &dwAttributes);
    if (FAILED(hr) || !IsMdRTSpecialName(dwAttributes))
        return FALSE;

    // Now check the name of the method. All vtable gap methods will have a prefix of "_VtblGap".
    LPCSTR szMethod;
    PCCOR_SIGNATURE pvSigBlob;
    ULONG cbSigBlob;    
    hr = m_pMDImport->GetNameAndSigOfMethodDef(md, &pvSigBlob, &cbSigBlob, &szMethod);
    if (FAILED(hr) || (strncmp(szMethod, "_VtblGap", 8) != 0))
        return FALSE;

    // If we make it to here we have a vtable gap method.
    return TRUE;
#else
    return FALSE;
#endif // FEATURE_COMINTEROP
}

//-----------------------------------------------------------------------------
// This function is called for non-generic methods in the current assembly,
// and for the typical "System.__Canon" instantiations of generic methods
// in the current assembly.
//-----------------------------------------------------------------------------

ZapImage::CompileStatus ZapImage::TryCompileMethodDef(mdMethodDef md, unsigned methodProfilingDataFlags)
{
    _ASSERTE(!IsNilToken(md));

    CORINFO_METHOD_HANDLE handle = NULL;
    CompileStatus         result = NOT_COMPILED;

    if (ShouldCompileMethodDef(md))
    {
        handle = m_pPreloader->LookupMethodDef(md);
        if (handle == nullptr)
        {
            result = LOOKUP_FAILED;
        }
    }
    else
    {
        result = COMPILE_EXCLUDED;
    }

    if (handle == NULL)
        return result;

    // compile the method
    //
    CompileStatus methodCompileStatus = TryCompileMethodWorker(handle, md, methodProfilingDataFlags);

    // Don't bother compiling the IL_STUBS if we failed to compile the parent IL method
    //
    if (methodCompileStatus == COMPILE_SUCCEED)
    {
        CompileMethodStubContext context(this, methodProfilingDataFlags);

        // compile stubs associated with the method
        m_pPreloader->GenerateMethodStubs(handle, m_zapper->m_pOpt->m_ngenProfileImage,
                                          &TryCompileMethodStub,
                                          &context);
    }

    return methodCompileStatus;
}


//-----------------------------------------------------------------------------
// This function is called for non-"System.__Canon" instantiations of generic methods.
// These could be methods defined in other assemblies too.
//-----------------------------------------------------------------------------

ZapImage::CompileStatus ZapImage::TryCompileInstantiatedMethod(CORINFO_METHOD_HANDLE handle, 
                                                               unsigned methodProfilingDataFlags)
{
    if (IsReadyToRunCompilation())
    {
        if (!GetCompileInfo()->IsInCurrentVersionBubble(m_zapper->m_pEEJitInfo->getMethodModule(handle)))
            return COMPILE_EXCLUDED;
    }

    if (!ShouldCompileInstantiatedMethod(handle))
        return COMPILE_EXCLUDED;

    // If we compiling this method because it was specified by the IBC profile data
    // then issue an warning if this method is not on our uncompiled method list
    // 
    if (methodProfilingDataFlags != 0)
    {
        if (methodProfilingDataFlags & (1 << ReadMethodCode))
        {
            // When we have stale IBC data the method could have been rejected from this image.
            if (!m_pPreloader->IsUncompiledMethod(handle))
            {
                const char* szClsName;
                const char* szMethodName = m_zapper->m_pEEJitInfo->getMethodName(handle, &szClsName);

                SString fullname(SString::Utf8, szClsName);
                fullname.AppendUTF8(NAMESPACE_SEPARATOR_STR);
                fullname.AppendUTF8(szMethodName);

                m_zapper->Info(W("Warning: Invalid method instantiation in profile data: %s\n"), fullname.GetUnicode());

                return NOT_COMPILED;
            }
        }
    }
   
    CompileStatus methodCompileStatus = TryCompileMethodWorker(handle, mdMethodDefNil, methodProfilingDataFlags);

    // Don't bother compiling the IL_STUBS if we failed to compile the parent IL method
    //
    if (methodCompileStatus == COMPILE_SUCCEED)
    {
        CompileMethodStubContext context(this, methodProfilingDataFlags);

        // compile stubs associated with the method
        m_pPreloader->GenerateMethodStubs(handle, m_zapper->m_pOpt->m_ngenProfileImage,
                                          &TryCompileMethodStub, 
                                          &context);
    }

    return methodCompileStatus;
}

//-----------------------------------------------------------------------------

ZapImage::CompileStatus ZapImage::TryCompileMethodWorker(CORINFO_METHOD_HANDLE handle, mdMethodDef md, 
                                                         unsigned methodProfilingDataFlags)
{
    _ASSERTE(handle != NULL);

    if (m_zapper->m_pOpt->m_onlyOneMethod && (m_zapper->m_pOpt->m_onlyOneMethod != md))
        return NOT_COMPILED;

    if (GetCompileInfo()->HasCustomAttribute(handle, "System.Runtime.BypassNGenAttribute"))
        return NOT_COMPILED;

#ifdef FEATURE_READYTORUN_COMPILER
    // This is a quick workaround to opt specific methods out of ReadyToRun compilation to work around bugs.
    if (IsReadyToRunCompilation())
    {
        if (GetCompileInfo()->HasCustomAttribute(handle, "System.Runtime.BypassReadyToRunAttribute"))
            return NOT_COMPILED;
    }
#endif

    // Do we have a profile entry for this method?
    //
    if (methodProfilingDataFlags != 0)
    {
        // Report the profiling data flags for layout of the EE datastructures
        m_pPreloader->SetMethodProfilingFlags(handle, methodProfilingDataFlags);

        // Hot methods can be marked to be excluded from the AOT native image.
        // A Jitted method executes faster than a ReadyToRun compiled method.
        //
        if ((methodProfilingDataFlags & (1 << ExcludeHotMethodCode)) != 0)
        {
            // returning COMPILE_HOT_EXCLUDED excludes this method from the AOT native image
            return COMPILE_HOT_EXCLUDED;
        }

        // Cold methods can be marked to be excluded from the AOT native image.
        // We can reduced the size of the AOT native image by selectively
        // excluding the code for some of the cold methods.
        //
        if ((methodProfilingDataFlags & (1 << ExcludeColdMethodCode)) != 0)
        {
            // returning COMPILE_COLD_EXCLUDED excludes this method from the AOT native image
            return COMPILE_COLD_EXCLUDED;
        }

        // If the code was never executed based on the profile data
        // then don't compile this method now. Wait until until later
        // when we are compiling the methods in the cold section.
        //
        if ((methodProfilingDataFlags & (1 << ReadMethodCode)) == 0)
        {
            // returning NOT_COMPILED will defer until later the compilation of this method
            return NOT_COMPILED;
        }
    }
    else  // we are compiling methods for the cold region
    {
        // Retrieve any information that we have about a previous compilation attempt of this method
        const ProfileDataHashEntry* pEntry = profileDataHashTable.LookupPtr(md);
        
        // When Partial Ngen is specified we will omit the AOT native code for every
        // method that does not have profile data
        //
        if (pEntry == nullptr && m_zapper->m_pOpt->m_fPartialNGen)
        {
            // returning COMPILE_COLD_EXCLUDED excludes this method from the AOT native image
            return COMPILE_COLD_EXCLUDED;
        }

        if (pEntry != nullptr)
        { 
            if ((pEntry->status == COMPILE_HOT_EXCLUDED) || (pEntry->status == COMPILE_COLD_EXCLUDED))
            {
                // returning COMPILE_HOT_EXCLUDED excludes this method from the AOT native image
                return pEntry->status;
            }
        }
    }

    // Have we already compiled it?
    if (GetCompiledMethod(handle) != NULL)
        return ALREADY_COMPILED;

    _ASSERTE(m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB) || IsNilToken(md) || handle == m_pPreloader->LookupMethodDef(md));

    CompileStatus result = NOT_COMPILED;
    
    CORINFO_MODULE_HANDLE module;

    // We only compile IL_STUBs from the current assembly
    if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
        module = m_hModule;
    else
        module = m_zapper->m_pEEJitInfo->getMethodModule(handle);

    ZapInfo zapInfo(this, md, handle, module, methodProfilingDataFlags);

    EX_TRY
    {
        zapInfo.CompileMethod();
        result = COMPILE_SUCCEED;
    }
    EX_CATCH
    {
        // Continue unwinding if fatal error was hit.
        if (FAILED(g_hrFatalError))
            ThrowHR(g_hrFatalError);

        Exception *ex = GET_EXCEPTION();
        HRESULT hrException = ex->GetHR();

        CorZapLogLevel level;

#ifdef CROSSGEN_COMPILE
        // Warnings should not go to stderr during crossgen
        level = CORZAP_LOGLEVEL_WARNING;
#else
        level = CORZAP_LOGLEVEL_ERROR;

        m_zapper->m_failed = TRUE;
#endif

        result = COMPILE_FAILED;

#ifdef FEATURE_READYTORUN_COMPILER
        // NYI features in R2R - Stop crossgen from spitting unnecessary
        //     messages to the console
        if (IsReadyToRunCompilation())
        {
            // When compiling the method we may receive an exeception when the
            // method uses a feature that is Not Implemented for ReadyToRun 
            // or a Type Load exception if the method uses for a SIMD type.
            //
            // We skip the compilation of such methods and we don't want to
            // issue a warning or error
            //
            if ((hrException == E_NOTIMPL) || (hrException == (HRESULT)IDS_CLASSLOAD_GENERAL))
            {
                result = NOT_COMPILED;
                level = CORZAP_LOGLEVEL_INFO;
            }
        }
#endif
        {
            StackSString message;
            ex->GetMessage(message);

            // FileNotFound errors here can be converted into a single error string per ngen compile, 
            //  and the detailed error is available with verbose logging
            if (hrException == COR_E_FILENOTFOUND)
            {
                StackSString logMessage(W("System.IO.FileNotFoundException: "));
                logMessage.Append(message);
                FileNotFoundError(logMessage.GetUnicode());
                level = CORZAP_LOGLEVEL_INFO;
            }

            m_zapper->Print(level, W("%s while compiling method %s\n"), message.GetUnicode(), zapInfo.m_currentMethodName.GetUnicode());

            if ((result == COMPILE_FAILED) && (m_stats != NULL))
            {
                if (!m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB))
                    m_stats->m_failedMethods++;
                else
                    m_stats->m_failedILStubs++;
            }
        }
    }
    EX_END_CATCH(SwallowAllExceptions);
    
    return result;
}


// Should we compile this method, defined in the ngen'ing module?
// Result is FALSE if any of the controls (only used by prejit.exe) exclude the method
BOOL ZapImage::ShouldCompileMethodDef(mdMethodDef md)
{
    DWORD partialNGenStressVal = PartialNGenStressPercentage();
    if (partialNGenStressVal &&
        // Module::AddCerListToRootTable has problems if mscorlib.dll is
        // a partial ngen image
        m_hModule != m_zapper->m_pEECompileInfo->GetLoaderModuleForMscorlib())
    {
        _ASSERTE(partialNGenStressVal <= 100);
        DWORD methodPercentageVal = (md % 100) + 1;
        if (methodPercentageVal <= partialNGenStressVal)
            return FALSE;
    }
    
    mdTypeDef td;
    IfFailThrow(m_pMDImport->GetParentToken(md, &td));
    
#ifdef FEATURE_COMINTEROP
    mdToken tkExtends;
    if (td != mdTypeDefNil)
    {
        m_pMDImport->GetTypeDefProps(td, NULL, &tkExtends);
        
        mdAssembly tkAssembly;
        DWORD dwAssemblyFlags;
        
        m_pMDImport->GetAssemblyFromScope(&tkAssembly);
        if (TypeFromToken(tkAssembly) == mdtAssembly)
        {
            m_pMDImport->GetAssemblyProps(tkAssembly,
                                            NULL, NULL,     // Public Key
                                            NULL,           // Hash Algorithm
                                            NULL,           // Name
                                            NULL,           // MetaData
                                            &dwAssemblyFlags);
            
            if (IsAfContentType_WindowsRuntime(dwAssemblyFlags))
            {
                if (TypeFromToken(tkExtends) == mdtTypeRef)
                {
                    LPCSTR szNameSpace = NULL;
                    LPCSTR szName = NULL;
                    m_pMDImport->GetNameOfTypeRef(tkExtends, &szNameSpace, &szName);
                    
                    if (!strcmp(szNameSpace, "System") && !_stricmp((szName), "Attribute"))
                    {
                        return FALSE;
                    }
                }
            }
        }
    }
#endif

#ifdef _DEBUG
    static ConfigMethodSet fZapOnly;
    fZapOnly.ensureInit(CLRConfig::INTERNAL_ZapOnly);

    static ConfigMethodSet fZapExclude;
    fZapExclude.ensureInit(CLRConfig::INTERNAL_ZapExclude);

    PCCOR_SIGNATURE pvSigBlob;
    ULONG cbSigBlob;

    // Get the name of the current method and its class
    LPCSTR szMethod;
    IfFailThrow(m_pMDImport->GetNameAndSigOfMethodDef(md, &pvSigBlob, &cbSigBlob, &szMethod));
    
    LPCWSTR wszClass = W("");
    SString sClass;

    if (td != mdTypeDefNil)
    {
        LPCSTR szNameSpace = NULL;
        LPCSTR szName = NULL;
        
        IfFailThrow(m_pMDImport->GetNameOfTypeDef(td, &szName, &szNameSpace));
        
        const SString nameSpace(SString::Utf8, szNameSpace);
        const SString name(SString::Utf8, szName);
        sClass.MakeFullNamespacePath(nameSpace, name);
        wszClass = sClass.GetUnicode();
    }

    MAKE_UTF8PTR_FROMWIDE(szClass,  wszClass);

    if (!fZapOnly.isEmpty() && !fZapOnly.contains(szMethod, szClass, pvSigBlob))
    {
        LOG((LF_ZAP, LL_INFO1000, "Rejecting compilation of method %08x, %s::%s\n", md, szClass, szMethod));
        return FALSE;
    }

    if (fZapExclude.contains(szMethod, szClass, pvSigBlob))
    {
        LOG((LF_ZAP, LL_INFO1000, "Rejecting compilation of method %08x, %s::%s\n", md, szClass, szMethod));
        return FALSE;
    }

    LOG((LF_ZAP, LL_INFO1000, "Compiling method %08x, %s::%s\n", md, szClass, szMethod));
#endif    
    
    return TRUE;
}


BOOL ZapImage::ShouldCompileInstantiatedMethod(CORINFO_METHOD_HANDLE handle)
{
    DWORD partialNGenStressVal = PartialNGenStressPercentage();
    if (partialNGenStressVal &&
        // Module::AddCerListToRootTable has problems if mscorlib.dll is
        // a partial ngen image
        m_hModule != m_zapper->m_pEECompileInfo->GetLoaderModuleForMscorlib())
    {
        _ASSERTE(partialNGenStressVal <= 100);
        DWORD methodPercentageVal = (m_zapper->m_pEEJitInfo->getMethodHash(handle) % 100) + 1;
        if (methodPercentageVal <= partialNGenStressVal)
            return FALSE;
    }

    return TRUE;
}

HRESULT ZapImage::PrintTokenDescription(CorZapLogLevel level, mdToken token)
{
    HRESULT hr;

    if (RidFromToken(token) == 0)
        return S_OK;

    LPCSTR szNameSpace = NULL;
    LPCSTR szName = NULL;

    if (m_pMDImport->IsValidToken(token))
    {
        switch (TypeFromToken(token))
        {
            case mdtMemberRef:
            {
                mdToken parent;
                IfFailRet(m_pMDImport->GetParentOfMemberRef(token, &parent));
                if (RidFromToken(parent) != 0)
                {
                    PrintTokenDescription(level, parent);
                    m_zapper->Print(level, W("."));
                }
                IfFailRet(m_pMDImport->GetNameAndSigOfMemberRef(token, NULL, NULL, &szName));
                break;
            }

            case mdtMethodDef:
            {
                mdToken parent;
                IfFailRet(m_pMDImport->GetParentToken(token, &parent));
                if (RidFromToken(parent) != 0)
                {
                    PrintTokenDescription(level, parent);
                    m_zapper->Print(level, W("."));
                }
                IfFailRet(m_pMDImport->GetNameOfMethodDef(token, &szName));
                break;
            }

            case mdtTypeRef:
            {   
                IfFailRet(m_pMDImport->GetNameOfTypeRef(token, &szNameSpace, &szName));
                break;
            }

            case mdtTypeDef:
            {
                IfFailRet(m_pMDImport->GetNameOfTypeDef(token, &szName, &szNameSpace));
                break;
            }

            default:
                break;
        }      
    }
    else
    {
        szName = "InvalidToken";
    }

    SString fullName;

    if (szNameSpace != NULL)
    {
        const SString nameSpace(SString::Utf8, szNameSpace);
        const SString name(SString::Utf8, szName);
        fullName.MakeFullNamespacePath(nameSpace, name);
    }
    else
    {
        fullName.SetUTF8(szName);
    }

    m_zapper->Print(level, W("%s"), fullName.GetUnicode());

    return S_OK;
}


HRESULT ZapImage::LocateProfileData()
{
    if (m_zapper->m_pOpt->m_ignoreProfileData)
    {
        return S_FALSE;
    }

    //
    // In the past, we have ignored profile data when instrumenting the assembly.
    // However, this creates significant differences between the tuning image and the eventual
    // optimized image (e.g. generic instantiations) which in turn leads to missed data during
    // training and cold touches during execution.  Instead, we take advantage of any IBC data
    // the assembly already has and attempt to make the tuning image as close as possible to
    // the final image.
    //
#if 0
    if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_BBINSTR))
        return S_FALSE;
#endif

    //
    // Don't use IBC data from untrusted assemblies--this allows us to assume that
    // the IBC data is not malicious
    //
    if (m_zapper->m_pEEJitInfo->canSkipVerification(m_hModule) != CORINFO_VERIFICATION_CAN_SKIP)
    {
        return S_FALSE;
    }

    //
    // See if there's profile data in the resource section of the PE
    //
    m_pRawProfileData = (BYTE*)m_ModuleDecoder.GetWin32Resource(W("PROFILE_DATA"), W("IBC"), &m_cRawProfileData);

    if ((m_pRawProfileData != NULL) && (m_cRawProfileData != 0))
    {
        m_zapper->Info(W("Found embedded profile resource in %s.\n"), m_pModuleFileName);
        return S_OK;
    }

    static ConfigDWORD g_UseIBCFile;
    if (g_UseIBCFile.val(CLRConfig::EXTERNAL_UseIBCFile) != 1)
        return S_OK;

    //
    // Couldn't find profile resource--let's see if there's an ibc file to use instead
    //

    SString path(m_pModuleFileName);

    SString::Iterator dot = path.End();
    if (path.FindBack(dot, '.'))
    {
        SString slName(SString::Literal, "ibc");
        path.Replace(dot+1, path.End() - (dot+1), slName);

        HandleHolder hFile = WszCreateFile(path.GetUnicode(),
                                     GENERIC_READ,
                                     FILE_SHARE_READ,
                                     NULL,
                                     OPEN_EXISTING,
                                     FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN,
                                     NULL);
        if (hFile != INVALID_HANDLE_VALUE)
        {
            HandleHolder hMapFile = WszCreateFileMapping(hFile, NULL, PAGE_READONLY, 0, 0, NULL);
            DWORD dwFileLen = SafeGetFileSize(hFile, 0);
            if (dwFileLen != INVALID_FILE_SIZE)
            {
                if (hMapFile == NULL)
                {
                    m_zapper->Warning(W("Found profile data file %s, but could not open it"), path.GetUnicode());
                }
                else
                {
                    m_zapper->Info(W("Found ibc file %s.\n"), path.GetUnicode());

                    m_profileDataFile  = (BYTE*) MapViewOfFile(hMapFile, FILE_MAP_READ, 0, 0, 0);

                    m_pRawProfileData  = m_profileDataFile;
                    m_cRawProfileData  = dwFileLen;
                }
            }
        }
    }

    return S_OK;
}


bool ZapImage::CanConvertIbcData()
{
    static ConfigDWORD g_iConvertIbcData;
    DWORD val = g_iConvertIbcData.val(CLRConfig::UNSUPPORTED_ConvertIbcData);
    return (val != 0);
}

HRESULT ZapImage::parseProfileData()
{
    if (m_pRawProfileData == NULL)
    {
        return S_FALSE;
    }

    ProfileReader profileReader(m_pRawProfileData, m_cRawProfileData);

    CORBBTPROF_FILE_HEADER *fileHeader;

    READ(fileHeader, CORBBTPROF_FILE_HEADER);
    if (fileHeader->HeaderSize < sizeof(CORBBTPROF_FILE_HEADER))
    {
        _ASSERTE(!"HeaderSize is too small");
        return E_FAIL;
    }

    // Read any extra header data. It will be needed for V3 files.

    DWORD extraHeaderDataSize = fileHeader->HeaderSize - sizeof(CORBBTPROF_FILE_HEADER);
    void *extraHeaderData = profileReader.Read(extraHeaderDataSize);

    bool convertFromV1 = false;
    bool minified = false;

    if (fileHeader->Magic != CORBBTPROF_MAGIC) 
    {
        _ASSERTE(!"ibcHeader contains bad values");
        return E_FAIL;
    }

    // CoreCLR should never be presented with V1 IBC data.
    if (fileHeader->Version == CORBBTPROF_V3_VERSION)
    {
        CORBBTPROF_FILE_OPTIONAL_HEADER *optionalHeader =
            (CORBBTPROF_FILE_OPTIONAL_HEADER *)extraHeaderData;

        if (!optionalHeader ||
            !CONTAINS_FIELD(optionalHeader, extraHeaderDataSize, Size) ||
            (optionalHeader->Size > extraHeaderDataSize))
        {
            m_zapper->Info(W("Optional header missing or corrupt."));
            return E_FAIL;
        }

        if (CONTAINS_FIELD(optionalHeader, optionalHeader->Size, FileFlags))
        {
            minified = !!(optionalHeader->FileFlags & CORBBTPROF_FILE_FLAG_MINIFIED);

            if (!m_zapper->m_pOpt->m_fPartialNGenSet)
            {
                m_zapper->m_pOpt->m_fPartialNGen = !!(optionalHeader->FileFlags & CORBBTPROF_FILE_FLAG_PARTIAL_NGEN);
            }
        }
    }
    else if (fileHeader->Version != CORBBTPROF_V2_VERSION)
    {
        m_zapper->Info(W("Discarding profile data with unknown version."));
        return S_FALSE;
    }

    // This module has profile data (this ends up controling the layout of physical and virtual
    // sections within the image, see ZapImage::AllocateVirtualSections.
    m_fHaveProfileData = true;
    m_zapper->m_pOpt->m_fHasAnyProfileData = true;

    CORBBTPROF_SECTION_TABLE_HEADER *sectionHeader;
    READ(sectionHeader, CORBBTPROF_SECTION_TABLE_HEADER);

    //
    // Parse the section table
    //

    for (ULONG i = 0; i < sectionHeader->NumEntries; i++)
    {
        CORBBTPROF_SECTION_TABLE_ENTRY *entry;
        READ(entry,CORBBTPROF_SECTION_TABLE_ENTRY);

        SectionFormat format = sectionHeader->Entries[i].FormatID;
        _ASSERTE(format >= 0);
        if (format < 0)
        {
            continue;
        }

        if (convertFromV1)
        {
            if (format < LastTokenFlagSection)
            {
                format = (SectionFormat) (format + 1);
            }
        }

        _ASSERTE(format < SectionFormatCount);

        if (format < SectionFormatCount)
        {
            BYTE *start = m_pRawProfileData + sectionHeader->Entries[i].Data.Offset;
            BYTE *end   = start             + sectionHeader->Entries[i].Data.Size;

            if ((start > m_pRawProfileData)                     &&
                (end   < m_pRawProfileData + m_cRawProfileData) &&
                (start < end))
            {
                _ASSERTE(m_profileDataSections[format].pData  == 0);
                _ASSERTE(m_profileDataSections[format].dataSize == 0);

                m_profileDataSections[format].pData     = start;
                m_profileDataSections[format].dataSize  = (DWORD) (end - start);
            }
            else
            {
                _ASSERTE(!"Invalid profile section offset or size");
                return E_FAIL;
            }
        }
    }

    HRESULT hr = S_OK;

    if (convertFromV1)
    {
        hr = convertProfileDataFromV1();
        if (FAILED(hr))
        {
            return hr;
        }
    }
    else if (minified)
    {
        hr = RehydrateProfileData();
        if (FAILED(hr))
        {
            return hr;
        }
    }
    else
    {
        //
        // For those sections that are collections of tokens, further parse that format to get
        // the token pointer and number of tokens
        //

        for (int format = FirstTokenFlagSection; format < SectionFormatCount; format++)
        {
            if (m_profileDataSections[format].pData)
            {
                SEEK(((ULONG) (m_profileDataSections[format].pData - m_pRawProfileData)));

                CORBBTPROF_TOKEN_LIST_SECTION_HEADER *header;
                READ(header, CORBBTPROF_TOKEN_LIST_SECTION_HEADER);

                DWORD tableSize = header->NumTokens;
                DWORD dataSize  = (m_profileDataSections[format].dataSize - sizeof(CORBBTPROF_TOKEN_LIST_SECTION_HEADER));
                DWORD expectedSize = tableSize * sizeof (CORBBTPROF_TOKEN_INFO);

                if (dataSize == expectedSize)
                {
                    BYTE * startOfTable = m_profileDataSections[format].pData + sizeof(CORBBTPROF_TOKEN_LIST_SECTION_HEADER);
                    m_profileDataSections[format].tableSize = tableSize;
                    m_profileDataSections[format].pTable = (CORBBTPROF_TOKEN_INFO *) startOfTable;
                }
                else
                {
                    _ASSERTE(!"Invalid CORBBTPROF_TOKEN_LIST_SECTION_HEADER header");
                    return E_FAIL;
                }
            }
        }
    }

    ZapImage::ProfileDataSection * DataSection_ScenarioInfo = & m_profileDataSections[ScenarioInfo];
    if (DataSection_ScenarioInfo->pData != NULL)
    {
        CORBBTPROF_SCENARIO_INFO_SECTION_HEADER * header = (CORBBTPROF_SCENARIO_INFO_SECTION_HEADER *) DataSection_ScenarioInfo->pData;
        m_profileDataNumRuns = header->TotalNumRuns;
    }

    return S_OK;
}


HRESULT ZapImage::convertProfileDataFromV1()
{
    if (m_pRawProfileData == NULL)
    {
        return S_FALSE;
    }

    //
    // For those sections that are collections of tokens, further parse that format to get
    // the token pointer and number of tokens
    //

    ProfileReader profileReader(m_pRawProfileData, m_cRawProfileData);

    for (SectionFormat format = FirstTokenFlagSection; format < SectionFormatCount; format = (SectionFormat) (format + 1))
    {
        if (m_profileDataSections[format].pData)
        {
            SEEK(((ULONG) (m_profileDataSections[format].pData - m_pRawProfileData)));

            CORBBTPROF_TOKEN_LIST_SECTION_HEADER *header;
            READ(header, CORBBTPROF_TOKEN_LIST_SECTION_HEADER);

            DWORD tableSize = header->NumTokens;

            if (tableSize == 0)
            {
                m_profileDataSections[format].tableSize = 0;
                m_profileDataSections[format].pTable    = NULL;
                continue;
            }

            DWORD dataSize  = (m_profileDataSections[format].dataSize - sizeof(CORBBTPROF_TOKEN_LIST_SECTION_HEADER));
            DWORD expectedSize = tableSize * sizeof (CORBBTPROF_TOKEN_LIST_ENTRY_V1);

            if (dataSize == expectedSize)
            {
                DWORD  newDataSize  = tableSize * sizeof (CORBBTPROF_TOKEN_INFO);

                if (newDataSize < dataSize)
                    return E_FAIL;

                BYTE * startOfTable = new (GetHeap()) BYTE[newDataSize];

                CORBBTPROF_TOKEN_LIST_ENTRY_V1 * pOldEntry;
                CORBBTPROF_TOKEN_INFO *    pNewEntry;

                pOldEntry = (CORBBTPROF_TOKEN_LIST_ENTRY_V1 *) (m_profileDataSections[format].pData + sizeof(CORBBTPROF_TOKEN_LIST_SECTION_HEADER));
                pNewEntry = (CORBBTPROF_TOKEN_INFO *)    startOfTable;

                for (DWORD i=0; i<tableSize; i++)
                {
                    pNewEntry->token = pOldEntry->token;
                    pNewEntry->flags = pOldEntry->flags;
                    pNewEntry->scenarios = 1;

                    pOldEntry++;
                    pNewEntry++;
                }
                m_profileDataSections[format].tableSize = tableSize;
                m_profileDataSections[format].pTable    = (CORBBTPROF_TOKEN_INFO *) startOfTable;
            }
            else
            {
                _ASSERTE(!"Invalid CORBBTPROF_TOKEN_LIST_SECTION_HEADER header");
                return E_FAIL;
            }
        }
    }

    _ASSERTE(m_profileDataSections[ScenarioInfo].pData == 0);
    _ASSERTE(m_profileDataSections[ScenarioInfo].dataSize == 0);

    //
    // Convert the MethodBlockCounts format from V1 to V2
    //
    CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1 * mbcSectionHeader = NULL;
    if (m_profileDataSections[MethodBlockCounts].pData)
    {
        //
        // Compute the size of the method block count stream
        // 
        BYTE *  dstPtr           = NULL;
        BYTE *  srcPtr           = m_profileDataSections[MethodBlockCounts].pData;
        DWORD   maxSizeToRead    = m_profileDataSections[MethodBlockCounts].dataSize;
        DWORD   totalSizeNeeded  = 0; 
        DWORD   totalSizeRead    = 0;
       
        mbcSectionHeader = (CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1 *) srcPtr;

        totalSizeRead   += sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1);
        totalSizeNeeded += sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER); 
        srcPtr          += sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1);

        if (totalSizeRead > maxSizeToRead)
        {
            return E_FAIL;
        }
       
        for (DWORD i=0; (i < mbcSectionHeader->NumMethods); i++)
        {
            CORBBTPROF_METHOD_HEADER_V1* methodEntry = (CORBBTPROF_METHOD_HEADER_V1 *) srcPtr;
            DWORD sizeRead   = 0;
            DWORD sizeWrite  = 0;

            sizeRead  += methodEntry->HeaderSize;
            sizeRead  += methodEntry->Size;
            sizeWrite += sizeof(CORBBTPROF_METHOD_HEADER);
            sizeWrite += methodEntry->Size;

            totalSizeRead   += sizeRead;
            totalSizeNeeded += sizeWrite;            

            if (totalSizeRead > maxSizeToRead)
            {
                return E_FAIL;
            }

            srcPtr += sizeRead;
        }
        assert(totalSizeRead == maxSizeToRead);

        // Reset the srcPtr
        srcPtr = m_profileDataSections[MethodBlockCounts].pData;
       
        BYTE * newMethodData = new (GetHeap()) BYTE[totalSizeNeeded];

        dstPtr = newMethodData;

        memcpy(dstPtr, srcPtr, sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER));
        srcPtr += sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1);
        dstPtr += sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER);
        
        for (DWORD i=0; (i < mbcSectionHeader->NumMethods); i++)
        {
            CORBBTPROF_METHOD_HEADER_V1 *  methodEntryV1 = (CORBBTPROF_METHOD_HEADER_V1 *) srcPtr;
            CORBBTPROF_METHOD_HEADER *     methodEntry   = (CORBBTPROF_METHOD_HEADER *)    dstPtr;
            DWORD sizeRead   = 0;
            DWORD sizeWrite  = 0;

            methodEntry->method.token   = methodEntryV1->MethodToken;
            methodEntry->method.ILSize  = 0;
            methodEntry->method.cBlock  = (methodEntryV1->Size / sizeof(CORBBTPROF_BLOCK_DATA));
            sizeRead  += methodEntryV1->HeaderSize; 
            sizeWrite += sizeof(CORBBTPROF_METHOD_HEADER);

            memcpy( dstPtr + sizeof(CORBBTPROF_METHOD_HEADER),
                    srcPtr + sizeof(CORBBTPROF_METHOD_HEADER_V1), 
                    (methodEntry->method.cBlock * sizeof(CORBBTPROF_BLOCK_DATA)));
            sizeRead  += methodEntryV1->Size; 
            sizeWrite += (methodEntry->method.cBlock * sizeof(CORBBTPROF_BLOCK_DATA));

            methodEntry->size    = sizeWrite;
            methodEntry->cDetail = 0;
            srcPtr += sizeRead;
            dstPtr += sizeWrite;
        }
       
        m_profileDataSections[MethodBlockCounts].pData    = newMethodData;
        m_profileDataSections[MethodBlockCounts].dataSize = totalSizeNeeded;
    }

    //
    // Allocate the scenario info section
    //
    {
        DWORD   sizeNeeded  = sizeof(CORBBTPROF_SCENARIO_INFO_SECTION_HEADER) + sizeof(CORBBTPROF_SCENARIO_HEADER);
        BYTE *  newData     = new (GetHeap()) BYTE[sizeNeeded];
        BYTE *  dstPtr      = newData;
        {
            CORBBTPROF_SCENARIO_INFO_SECTION_HEADER *siHeader = (CORBBTPROF_SCENARIO_INFO_SECTION_HEADER *) dstPtr;
            
            if (mbcSectionHeader != NULL)
                siHeader->TotalNumRuns = mbcSectionHeader->NumRuns;
            else
                siHeader->TotalNumRuns = 1;

            siHeader->NumScenarios = 1;

            dstPtr += sizeof(CORBBTPROF_SCENARIO_INFO_SECTION_HEADER);
        }
        {
            CORBBTPROF_SCENARIO_HEADER *sHeader = (CORBBTPROF_SCENARIO_HEADER *) dstPtr;

            sHeader->scenario.ordinal  = 1;
            sHeader->scenario.mask     = 1;
            sHeader->scenario.priority = 0;
            sHeader->scenario.numRuns  = 0;
            sHeader->scenario.cName    = 0; 

            sHeader->size = sHeader->Size();

            dstPtr += sizeof(CORBBTPROF_SCENARIO_HEADER);
        }
        m_profileDataSections[ScenarioInfo].pData = newData;
        m_profileDataSections[ScenarioInfo].dataSize = sizeNeeded;
    }

    //
    // Convert the BlobStream format from V1 to V2 
    //   
    if (m_profileDataSections[BlobStream].dataSize > 0)
    {
        //
        // Compute the size of the blob stream
        // 
        
        BYTE *  srcPtr           = m_profileDataSections[BlobStream].pData;
        BYTE *  dstPtr           = NULL;
        DWORD   maxSizeToRead    = m_profileDataSections[BlobStream].dataSize;
        DWORD   totalSizeNeeded  = 0;
        DWORD   totalSizeRead    = 0;
        bool    done             = false;
        
        while (!done)
        {
            CORBBTPROF_BLOB_ENTRY_V1* blobEntry = (CORBBTPROF_BLOB_ENTRY_V1 *) srcPtr;
            DWORD sizeWrite  = 0;
            DWORD sizeRead   = 0;

            if ((blobEntry->blobType >= MetadataStringPool) && (blobEntry->blobType <= MetadataUserStringPool))
            {
                sizeWrite += sizeof(CORBBTPROF_BLOB_POOL_ENTRY);
                sizeWrite += blobEntry->cBuffer;
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                sizeRead  += blobEntry->cBuffer;
            }
            else if ((blobEntry->blobType >= ParamTypeSpec) && (blobEntry->blobType <= ParamMethodSpec))
            {
                sizeWrite += sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY);
                sizeWrite += blobEntry->cBuffer;
                if (blobEntry->blobType == ParamMethodSpec)
                {
                    sizeWrite -= 1;  // Adjust for 
                }
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                sizeRead  += blobEntry->cBuffer;
            }
            else if (blobEntry->blobType == EndOfBlobStream)
            {
                sizeWrite += sizeof(CORBBTPROF_BLOB_ENTRY);
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                done = true;
            }
            else
            {
                return E_FAIL;
            }
            
            totalSizeNeeded += sizeWrite;
            totalSizeRead   += sizeRead;
            
            if (sizeRead > maxSizeToRead)
            {
                return E_FAIL;
            }
            
            srcPtr += sizeRead;
        }

        assert(totalSizeRead == maxSizeToRead);

        // Reset the srcPtr
        srcPtr = m_profileDataSections[BlobStream].pData;
        
        BYTE * newBlobData = new (GetHeap()) BYTE[totalSizeNeeded];

        dstPtr = newBlobData;
        done = false;
        
        while (!done)
        {
            CORBBTPROF_BLOB_ENTRY_V1* blobEntryV1 = (CORBBTPROF_BLOB_ENTRY_V1 *) srcPtr;
            DWORD sizeWrite  = 0;
            DWORD sizeRead   = 0;
            
            if ((blobEntryV1->blobType >= MetadataStringPool) && (blobEntryV1->blobType <= MetadataUserStringPool))
            {
                CORBBTPROF_BLOB_POOL_ENTRY* blobPoolEntry = (CORBBTPROF_BLOB_POOL_ENTRY*) dstPtr;
                
                blobPoolEntry->blob.type = blobEntryV1->blobType;
                blobPoolEntry->blob.size = sizeof(CORBBTPROF_BLOB_POOL_ENTRY) + blobEntryV1->cBuffer;
                blobPoolEntry->cBuffer   = blobEntryV1->cBuffer;
                memcpy(blobPoolEntry->buffer, blobEntryV1->pBuffer, blobEntryV1->cBuffer);
                
                sizeWrite += sizeof(CORBBTPROF_BLOB_POOL_ENTRY);
                sizeWrite += blobEntryV1->cBuffer;
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                sizeRead  += blobEntryV1->cBuffer;
            }
            else if ((blobEntryV1->blobType >= ParamTypeSpec) && (blobEntryV1->blobType <= ParamMethodSpec))
            {
                CORBBTPROF_BLOB_PARAM_SIG_ENTRY* blobSigEntry = (CORBBTPROF_BLOB_PARAM_SIG_ENTRY*) dstPtr;

                blobSigEntry->blob.type  = blobEntryV1->blobType;
                blobSigEntry->blob.size  = sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY) + blobEntryV1->cBuffer;
                blobSigEntry->blob.token = 0;
                blobSigEntry->cSig       = blobEntryV1->cBuffer; 

                if (blobEntryV1->blobType == ParamMethodSpec)
                {
                    // Adjust cSig and blob.size
                    blobSigEntry->cSig--; 
                    blobSigEntry->blob.size--;
                }
                memcpy(blobSigEntry->sig, blobEntryV1->pBuffer, blobSigEntry->cSig);
                
                sizeWrite += sizeof(CORBBTPROF_BLOB_PARAM_SIG_ENTRY);
                sizeWrite += blobSigEntry->cSig;
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                sizeRead  += blobEntryV1->cBuffer;
            }
            else if (blobEntryV1->blobType == EndOfBlobStream)
            {
                CORBBTPROF_BLOB_ENTRY* blobEntry = (CORBBTPROF_BLOB_ENTRY*) dstPtr;

                blobEntry->type = blobEntryV1->blobType;
                blobEntry->size = sizeof(CORBBTPROF_BLOB_ENTRY);
                
                sizeWrite += sizeof(CORBBTPROF_BLOB_ENTRY);
                sizeRead  += sizeof(CORBBTPROF_BLOB_ENTRY_V1);
                done = true;
            }
            else
            {
                return E_FAIL;
            }
            srcPtr += sizeRead;
            dstPtr += sizeWrite;
        }
       
        m_profileDataSections[BlobStream].pData    = newBlobData;
        m_profileDataSections[BlobStream].dataSize = totalSizeNeeded;
    }
    else
    {
        m_profileDataSections[BlobStream].pData    = NULL;
        m_profileDataSections[BlobStream].dataSize = 0;
    }

    return S_OK;
}

void ZapImage::RehydrateBasicBlockSection()
{
    ProfileDataSection &section = m_profileDataSections[MethodBlockCounts];
    if (!section.pData)
    {
        return;
    }

    ProfileReader reader(section.pData, section.dataSize);

    m_profileDataNumRuns = reader.Read<unsigned int>();

    // The IBC data provides a hint to the number of basic blocks, which is
    // used here to determine how much space to allocate for the rehydrated
    // data.
    unsigned int blockCountHint = reader.Read<unsigned int>();

    unsigned int numMethods = reader.Read<unsigned int>();

    unsigned int expectedLength =
        sizeof(CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER) +
        sizeof(CORBBTPROF_METHOD_HEADER) * numMethods +
        sizeof(CORBBTPROF_BLOCK_DATA) * blockCountHint;

    BinaryWriter writer(expectedLength, GetHeap());

    writer.Write(numMethods);

    mdToken lastMethodToken = 0x06000000;

    CORBBTPROF_METHOD_HEADER methodHeader;
    methodHeader.cDetail = 0;
    methodHeader.method.ILSize = 0;

    for (unsigned int i = 0; i < numMethods; ++i)
    {
        // Translate the method header
        unsigned int size = reader.Read7BitEncodedInt();
        unsigned int startPosition = reader.GetCurrentPos();

        mdToken token = reader.ReadTokenWithMemory(lastMethodToken);
        unsigned int ilSize = reader.Read7BitEncodedInt();
        unsigned int firstBlockHitCount = reader.Read7BitEncodedInt();

        unsigned int numOtherBlocks = reader.Read7BitEncodedInt();

        methodHeader.method.cBlock = 1 + numOtherBlocks;
        methodHeader.method.token = token;
        methodHeader.method.ILSize = ilSize;
        methodHeader.size = (DWORD)methodHeader.Size();

        writer.Write(methodHeader);

        CORBBTPROF_BLOCK_DATA blockData;

        // The first block is handled specially.
        blockData.ILOffset = 0;
        blockData.ExecutionCount = firstBlockHitCount;

        writer.Write(blockData);

        // Translate the rest of the basic blocks
        for (unsigned int j = 0; j < numOtherBlocks; ++j)
        {
            blockData.ILOffset = reader.Read7BitEncodedInt();
            blockData.ExecutionCount = reader.Read7BitEncodedInt();

            writer.Write(blockData);
        }

        if (!reader.Seek(startPosition + size))
        {
            ThrowHR(E_FAIL);
        }
    }

    // If the expected and actual lengths differ, the result will still be
    // correct but performance may suffer slightly because of reallocations.
    _ASSERTE(writer.GetWrittenSize() == expectedLength);

    section.pData = writer.GetBuffer();
    section.dataSize = writer.GetWrittenSize();
}

void ZapImage::RehydrateTokenSection(int sectionFormat, unsigned int flagTable[255])
{
    ProfileDataSection &section = m_profileDataSections[sectionFormat];
    ProfileReader reader(section.pData, section.dataSize);

    unsigned int numTokens = reader.Read<unsigned int>();

    unsigned int dataLength = sizeof(unsigned int) +
                              numTokens * sizeof(CORBBTPROF_TOKEN_INFO);
    BinaryWriter writer(dataLength, GetHeap());

    writer.Write(numTokens);

    mdToken lastToken = (sectionFormat - FirstTokenFlagSection) << 24;

    CORBBTPROF_TOKEN_INFO tokenInfo;
    tokenInfo.scenarios = 1;

    for (unsigned int i = 0; i < numTokens; ++i)
    {
        tokenInfo.token = reader.ReadTokenWithMemory(lastToken);
        tokenInfo.flags = reader.ReadFlagWithLookup(flagTable);

        writer.Write(tokenInfo);
    }

    _ASSERTE(writer.GetWrittenSize() == dataLength);
    
    section.pData = writer.GetBuffer();
    section.dataSize = writer.GetWrittenSize();
    section.pTable = (CORBBTPROF_TOKEN_INFO *)(section.pData + sizeof(unsigned int));
    section.tableSize = numTokens;
}

void ZapImage::RehydrateBlobStream()
{
    ProfileDataSection &section = m_profileDataSections[BlobStream];

    ProfileReader reader(section.pData, section.dataSize);

    // Evidence suggests that rehydrating the blob stream in Framework binaries
    // increases the size from 1.5-2x. When this was written, 1.85x minimized
    // the amount of extra memory allocated (about 48K in the worst case).
    BinaryWriter writer((DWORD)(section.dataSize * 1.85f), GetHeap());

    mdToken LastBlobToken = 0;
    mdToken LastAssemblyToken = 0x23000000;
    mdToken LastExternalTypeToken = 0x62000000;
    mdToken LastExternalNamespaceToken = 0x61000000;
    mdToken LastExternalSignatureToken = 0x63000000;

    int blobType = 0;
    do
    {
        // Read the blob header.

        unsigned int sizeToRead = reader.Read7BitEncodedInt();
        unsigned int startPositionRead = reader.GetCurrentPos();
    
        blobType = reader.Read7BitEncodedInt();
        mdToken token = reader.ReadTokenWithMemory(LastBlobToken);

        // Write out the blob header.

        // Note the location in the write stream, and write a 0 there. Once
        // this blob has been written in its entirety, this location can be
        // used to calculate the real size and to go back to the right place
        // to write it.

        unsigned int startPositionWrite = writer.GetWrittenSize();
        writer.Write(0U);

        writer.Write(blobType);
        writer.Write(token);

        // All blobs (except the end-of-stream indicator) end as:
        //     <data length> <data>
        // Two blob types (handled immediately below) include tokens as well.
        // Handle those first, then handle the common case.

        if (blobType == ExternalTypeDef)
        {
            writer.Write(reader.ReadTokenWithMemory(LastAssemblyToken));
            writer.Write(reader.ReadTokenWithMemory(LastExternalTypeToken));
            writer.Write(reader.ReadTokenWithMemory(LastExternalNamespaceToken));
        }
        else if (blobType == ExternalMethodDef)
        {
            writer.Write(reader.ReadTokenWithMemory(LastExternalTypeToken));
            writer.Write(reader.ReadTokenWithMemory(LastExternalSignatureToken));
        }

        if ((blobType >= MetadataStringPool) && (blobType < IllegalBlob))
        {
            // This blob is of known type and ends with data.
            unsigned int dataLength = reader.Read7BitEncodedInt();
            char *data = (char *)reader.Read(dataLength);

            if (!data)
            {
                ThrowHR(E_FAIL);
            }

            writer.Write(dataLength);
            writer.Write(data, dataLength);
        }

        // Write the size for this blob.

        writer.WriteAt(startPositionWrite,
                       writer.GetWrittenSize() - startPositionWrite);

        // Move to the next blob.

        if (!reader.Seek(startPositionRead + sizeToRead))
        {
            ThrowHR(E_FAIL);
        }
    }
    while (blobType != EndOfBlobStream);

    section.pData = writer.GetBuffer();
    section.dataSize = writer.GetWrittenSize();
}

HRESULT ZapImage::RehydrateProfileData()
{
    HRESULT hr = S_OK;
    unsigned int flagTable[255];
    memset(flagTable, 0xFF, sizeof(flagTable));
    
    EX_TRY
    {
        RehydrateBasicBlockSection();
        RehydrateBlobStream();
        for (int format = FirstTokenFlagSection;
             format < SectionFormatCount;
             ++format)
        {
            if (m_profileDataSections[format].pData)
            {
                RehydrateTokenSection(format, flagTable);
            }
        }
    }
    EX_CATCH_HRESULT_NO_ERRORINFO(hr);

    return hr;
}

HRESULT ZapImage::hashMethodBlockCounts()
{
    ProfileDataSection * DataSection_MethodBlockCounts = & m_profileDataSections[MethodBlockCounts];

    if (!DataSection_MethodBlockCounts->pData)
    {
        return E_FAIL;
    }

    ProfileReader profileReader(DataSection_MethodBlockCounts->pData, DataSection_MethodBlockCounts->dataSize);

    CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER *mbcHeader;
    READ(mbcHeader,CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER);

    for (DWORD i = 0; i < mbcHeader->NumMethods; i++)
    {
        ProfileDataHashEntry newEntry;
        newEntry.pos = profileReader.GetCurrentPos();
        
        CORBBTPROF_METHOD_HEADER *methodHeader;
        READ(methodHeader,CORBBTPROF_METHOD_HEADER);
        newEntry.md   = methodHeader->method.token;
        newEntry.size = methodHeader->size;
        newEntry.flags = 0;
        newEntry.status = NOT_COMPILED;

        // Add the new entry to the table
        profileDataHashTable.Add(newEntry);

        // Skip the profileData so we can read the next method.
        void *profileData;
        READ_SIZE(profileData, void, (methodHeader->size - sizeof(CORBBTPROF_METHOD_HEADER)));
    }

    return S_OK;
}

void ZapImage::hashBBUpdateFlagsAndCompileResult(mdToken token, unsigned methodProfilingDataFlags, ZapImage::CompileStatus compileResult)
{
    // SHash only supports replacing an entry so we setup our newEntry and then perform a lookup
    //
    ProfileDataHashEntry newEntry;
    newEntry.md = token;
    newEntry.flags = methodProfilingDataFlags;
    newEntry.status = compileResult;

    const ProfileDataHashEntry* pEntry = profileDataHashTable.LookupPtr(token);
    if (pEntry != nullptr)
    {
        assert(pEntry->md == newEntry.md);
        assert(pEntry->flags == 0);   // the flags should not be set at this point.

        // Copy and keep the two fleids that were previously set
        newEntry.size = pEntry->size;
        newEntry.pos = pEntry->pos;
    }
    else // We have a method that doesn't have basic block counts
    {
        newEntry.size = 0;
        newEntry.pos = 0;
    }
    profileDataHashTable.AddOrReplace(newEntry);
}

void ZapImage::LoadProfileData()
{
    HRESULT hr = E_FAIL;

    m_fHaveProfileData = false;
    m_pRawProfileData  = NULL;
    m_cRawProfileData  = 0;

    EX_TRY
    {
        hr = LocateProfileData();
        
        if (hr == S_OK)
        {
            hr = parseProfileData();
            if (hr == S_OK)
            {
                hr = hashMethodBlockCounts();
            }
        }
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(SwallowAllExceptions);
    
    if (hr != S_OK)
    {
        m_fHaveProfileData = false;
        m_pRawProfileData = NULL;
        m_cRawProfileData = 0;

        if (FAILED(hr))
        {
            m_zapper->Warning(W("Warning: Invalid profile data was ignored for %s\n"), m_pModuleFileName);
        }
    }
}

// Initializes our form of the profile data stored in the assembly.

CorProfileData *  ZapImage::NewProfileData()
{
    this->m_pCorProfileData = new CorProfileData(&m_profileDataSections[0]);

    return this->m_pCorProfileData;
}

// Returns the profile data stored in the assembly.

CorProfileData *  ZapImage::GetProfileData()
{
    _ASSERTE(this->m_pCorProfileData != NULL);

    return this->m_pCorProfileData;
}

CorProfileData::CorProfileData(void *  rawProfileData)
{
    ZapImage::ProfileDataSection * profileData =  (ZapImage::ProfileDataSection *) rawProfileData;

    for (DWORD format = 0; format < SectionFormatCount; format++)
    {
        this->profilingTokenFlagsData[format].count = profileData[format].tableSize;
        this->profilingTokenFlagsData[format].data  = profileData[format].pTable;
    }

    this->blobStream = (CORBBTPROF_BLOB_ENTRY *) profileData[BlobStream].pData;
}


// Determines whether a method can be called directly from another method (without
// going through the prestub) in the current module.
// callerFtn=NULL implies any/unspecified caller in the current module.
//
// Returns NULL if 'calleeFtn' cannot be called directly *at the current time*
// Else returns the direct address that 'calleeFtn' can be called at.


bool ZapImage::canIntraModuleDirectCall(
                        CORINFO_METHOD_HANDLE callerFtn,
                        CORINFO_METHOD_HANDLE targetFtn,
                        CorInfoIndirectCallReason *pReason,
                        CORINFO_ACCESS_FLAGS  accessFlags/*=CORINFO_ACCESS_ANY*/)
{
    CorInfoIndirectCallReason reason;
    if (pReason == NULL)
        pReason = &reason;
    *pReason = CORINFO_INDIRECT_CALL_UNKNOWN;

    // The caller should have checked that the method is in current loader module
    _ASSERTE(m_hModule == m_zapper->m_pEECompileInfo->GetLoaderModuleForEmbeddableMethod(targetFtn));

    // No direct calls at all under some circumstances

    if (m_zapper->m_pOpt->m_compilerFlags.IsSet(CORJIT_FLAGS::CORJIT_FLAG_PROF_ENTERLEAVE)
        && !m_pPreloader->IsDynamicMethod(callerFtn))
    {
        *pReason = CORINFO_INDIRECT_CALL_PROFILING;
        goto CALL_VIA_ENTRY_POINT;
    }

    // Does the methods's class have a cctor, etc?

    if (!m_pPreloader->CanSkipMethodPreparation(callerFtn, targetFtn, pReason, accessFlags))
        goto CALL_VIA_ENTRY_POINT;

    ZapMethodHeader * pMethod;
    pMethod = GetCompiledMethod(targetFtn);

    // If we have not compiled the method then we can't call direct

    if (pMethod == NULL)
    {
        *pReason = CORINFO_INDIRECT_CALL_NO_CODE;
        goto CALL_VIA_ENTRY_POINT;
    }

    // Does the method have fixups?

    if (pMethod->HasFixups() != NULL)
    {
        *pReason = CORINFO_INDIRECT_CALL_FIXUPS;
        goto CALL_VIA_ENTRY_POINT;
    }

#ifdef _DEBUG
    const char* clsName, * methodName;
    methodName = m_zapper->m_pEEJitInfo->getMethodName(targetFtn, &clsName);
    LOG((LF_ZAP, LL_INFO10000, "getIntraModuleDirectCallAddr: Success %s::%s\n",
        clsName, methodName));
#endif

    return true;

CALL_VIA_ENTRY_POINT:

#ifdef _DEBUG
    methodName = m_zapper->m_pEEJitInfo->getMethodName(targetFtn, &clsName);
    LOG((LF_ZAP, LL_INFO10000, "getIntraModuleDirectCallAddr: Via EntryPoint %s::%s\n",
         clsName, methodName));
#endif

    return false;
}

//
// Relocations
//

void ZapImage::WriteReloc(PVOID pSrc, int offset, ZapNode * pTarget, int targetOffset, ZapRelocationType type)
{
    _ASSERTE(!IsWritingRelocs());

    _ASSERTE(m_pBaseRelocs != NULL);
    m_pBaseRelocs->WriteReloc(pSrc, offset, pTarget, targetOffset, type);
}

ZapImage * ZapImage::GetZapImage()
{
    return this;
}

void ZapImage::FileNotFoundError(LPCWSTR pszMessage)
{
    SString message(pszMessage);

    for (COUNT_T i = 0; i < fileNotFoundErrorsTable.GetCount(); i++)
    {
        // Check to see if same error has already been displayed for this ngen operation
        if (message.Equals(fileNotFoundErrorsTable[i]))
            return;
    }

    CorZapLogLevel level;

#ifdef CROSSGEN_COMPILE
    // Warnings should not go to stderr during crossgen
    level = CORZAP_LOGLEVEL_WARNING;
#else
    level = CORZAP_LOGLEVEL_ERROR;
#endif

    m_zapper->Print(level, W("Warning: %s.\n"), pszMessage);

    fileNotFoundErrorsTable.Append(message);
}

void ZapImage::Error(mdToken token, HRESULT hr, UINT resID,  LPCWSTR message)
{
    // Missing dependencies are reported as fatal errors in code:CompilationDomain::BindAssemblySpec.
    // Avoid printing redundant error message for them.
    if (FAILED(g_hrFatalError))
        ThrowHR(g_hrFatalError);

    // COM introduces the notion of a vtable gap method, which is not a real method at all but instead
    // aids in the explicit layout of COM interop vtables. These methods have no implementation and no
    // direct runtime state tracking them. Trying to lookup a method handle for a vtable gap method will
    // throw an exception but we choose to let that happen and filter out the warning here in the
    // handler because (a) vtable gap methods are rare and (b) it's not all that cheap to identify them
    // beforehand.
    if ((TypeFromToken(token) == mdtMethodDef) && IsVTableGapMethod(token))
    {
        return;
    }

    CorZapLogLevel level = CORZAP_LOGLEVEL_ERROR;

    // Some warnings are demoted to informational level
    if (resID == IDS_EE_SIMD_NGEN_DISALLOWED)
    {
        // Supress printing of "Target-dependent SIMD vector types may not be used with ngen."
        level = CORZAP_LOGLEVEL_INFO;
    }

    if (resID == IDS_EE_HWINTRINSIC_NGEN_DISALLOWED)
    {
        // Supress printing of "Hardware intrinsics may not be used with ngen."
        level = CORZAP_LOGLEVEL_INFO;
    }

#ifdef CROSSGEN_COMPILE
    if ((resID == IDS_IBC_MISSING_EXTERNAL_TYPE) ||
        (resID == IDS_IBC_MISSING_EXTERNAL_METHOD))
    {
        // Supress printing of "The generic type/method specified by the IBC data is not available to this assembly"
        level = CORZAP_LOGLEVEL_INFO;
    }   
#endif        

    if (m_zapper->m_pOpt->m_ignoreErrors)
    {
#ifdef CROSSGEN_COMPILE
        // Warnings should not go to stderr during crossgen
        if (level == CORZAP_LOGLEVEL_ERROR)
        {
            level = CORZAP_LOGLEVEL_WARNING;
        }
#endif
        m_zapper->Print(level, W("Warning: "));
    }
    else
    {
        m_zapper->Print(level, W("Error: "));
    }

    if (message != NULL)
        m_zapper->Print(level, W("%s"), message);
    else
        m_zapper->PrintErrorMessage(level, hr);

    m_zapper->Print(level, W(" while resolving 0x%x - "), token);
    PrintTokenDescription(level, token);
    m_zapper->Print(level, W(".\n"));

    if (m_zapper->m_pOpt->m_ignoreErrors)
        return;

    IfFailThrow(hr);
}

ZapNode * ZapImage::GetInnerPtr(ZapNode * pNode, SSIZE_T offset)
{
    return m_pInnerPtrs->Get(pNode, offset);
}

ZapNode * ZapImage::GetHelperThunk(CorInfoHelpFunc ftnNum)
{
    ZapNode * pHelperThunk = m_pHelperThunks[ftnNum];

    if (pHelperThunk == NULL)
    {
        pHelperThunk = new (GetHeap()) ZapHelperThunk(ftnNum);
#ifdef _TARGET_ARM_
        pHelperThunk = GetInnerPtr(pHelperThunk, THUMB_CODE);
#endif
        m_pHelperThunks[ftnNum] = pHelperThunk;
    }

    // Ensure that the thunk is placed
    ZapNode * pTarget = pHelperThunk;
    if (pTarget->GetType() == ZapNodeType_InnerPtr)
        pTarget = ((ZapInnerPtr *)pTarget)->GetBase();
    if (!pTarget->IsPlaced())
        m_pHelperTableSection->Place(pTarget);

    return pHelperThunk;
}

//
// Compute a class-layout order based on a breadth-first traversal of 
// the class graph (based on what classes contain calls to other classes).
// We cannot afford time or space to build the graph, so we do processing
// in place.
// 
void ZapImage::ComputeClassLayoutOrder()
{
    // In order to make the computation efficient, we need to store per-class 
    // intermediate values in the class layout field.  These come in two forms:
    // 
    //   - An entry with the UNSEEN_CLASS_FLAG set is one that is yet to be encountered.
    //   - An entry with METHOD_INDEX_FLAG set is an index into the m_MethodCompilationOrder list
    //     indicating where the unprofiled methods of this class begin
    //   
    // Both flags begin set (by InitializeClassLayoutOrder) since the value initialized is
    // the method index and the class has not been encountered by the algorithm.
    // When a class layout has been computed, both of these flags will have been stripped.


    // Early-out in the (probably impossible) case that these bits weren't available
    if (m_MethodCompilationOrder.GetCount() >= UNSEEN_CLASS_FLAG ||
        m_MethodCompilationOrder.GetCount() >= METHOD_INDEX_FLAG)
    {
        return;
    }

    // Allocate the queue for the breadth-first traversal.
    // Note that the use of UNSEEN_CLASS_FLAG ensures that no class is enqueued more
    // than once, so we can use that bound for the size of the queue.
    CORINFO_CLASS_HANDLE * classQueue = new CORINFO_CLASS_HANDLE[m_ClassLayoutOrder.GetCount()];

    unsigned classOrder = 0;
    for (COUNT_T i = m_iUntrainedMethod; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        unsigned classQueueNext = 0;
        unsigned classQueueEnd = 0;
        COUNT_T  methodIndex = 0;

        //
        // Find an unprocessed method to seed the next breadth-first traversal.
        //

        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        const ClassLayoutOrderEntry * pEntry = m_ClassLayoutOrder.LookupPtr(pMethod->m_classHandle);
        _ASSERTE(pEntry);
        
        if ((pEntry->m_order & UNSEEN_CLASS_FLAG) == 0)
        {
            continue;
        }

        //
        // Enqueue the method's class and start the traversal.
        //

        classQueue[classQueueEnd++] = pMethod->m_classHandle;
        ((ClassLayoutOrderEntry *)pEntry)->m_order &= ~UNSEEN_CLASS_FLAG;

        while (classQueueNext < classQueueEnd)
        {
            //
            // Dequeue a class and pull out the index of its first method
            //
            
            CORINFO_CLASS_HANDLE dequeuedClassHandle = classQueue[classQueueNext++];
            _ASSERTE(dequeuedClassHandle != NULL);

            pEntry = m_ClassLayoutOrder.LookupPtr(dequeuedClassHandle);
            _ASSERTE(pEntry);
            _ASSERTE((pEntry->m_order & UNSEEN_CLASS_FLAG) == 0);
            _ASSERTE((pEntry->m_order & METHOD_INDEX_FLAG) != 0);

            methodIndex = pEntry->m_order & ~METHOD_INDEX_FLAG;
            _ASSERTE(methodIndex < m_MethodCompilationOrder.GetCount());

            //
            // Set the real layout order of the class, and examine its unprofiled methods
            //
            
            ((ClassLayoutOrderEntry *)pEntry)->m_order = ++classOrder;
                
            pMethod = m_MethodCompilationOrder[methodIndex];
            _ASSERTE(pMethod->m_classHandle == dequeuedClassHandle);

            while (pMethod->m_classHandle == dequeuedClassHandle)
            {

                //
                // For each unprofiled method, find target classes and enqueue any that haven't been seen
                //

                ZapMethodHeader::PartialTargetMethodIterator it(pMethod);

                CORINFO_METHOD_HANDLE targetMethodHandle;
                while (it.GetNext(&targetMethodHandle))
                {
                    CORINFO_CLASS_HANDLE targetClassHandle = GetJitInfo()->getMethodClass(targetMethodHandle);
                    if (targetClassHandle != pMethod->m_classHandle)
                    {
                        pEntry = m_ClassLayoutOrder.LookupPtr(targetClassHandle);

                        if (pEntry && (pEntry->m_order & UNSEEN_CLASS_FLAG) != 0)
                        {
                            _ASSERTE(classQueueEnd < m_ClassLayoutOrder.GetCount());
                            classQueue[classQueueEnd++] = targetClassHandle;

                            ((ClassLayoutOrderEntry *)pEntry)->m_order &= ~UNSEEN_CLASS_FLAG;
                        }
                    }
                }

                if (++methodIndex == m_MethodCompilationOrder.GetCount())
                {
                    break;
                }
                    
                pMethod = m_MethodCompilationOrder[methodIndex];
            }
        }
    }

    for (COUNT_T i = m_iUntrainedMethod; i < m_MethodCompilationOrder.GetCount(); i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];
        pMethod->m_cachedLayoutOrder = LookupClassLayoutOrder(pMethod->m_classHandle);
    }

    m_fHasClassLayoutOrder = true;

    delete [] classQueue;
}

static int __cdecl LayoutOrderCmp(const void* a_, const void* b_)
{
    ZapMethodHeader * a = *((ZapMethodHeader**)a_);
    ZapMethodHeader * b = *((ZapMethodHeader**)b_);

    int layoutDiff = a->GetCachedLayoutOrder() - b->GetCachedLayoutOrder();
    if (layoutDiff != 0)
        return layoutDiff;

    // Use compilation order as secondary key to get predictable ordering within the bucket
    return a->GetCompilationOrder() - b->GetCompilationOrder();
}

void ZapImage::SortUnprofiledMethodsByClassLayoutOrder()
{
    qsort(&m_MethodCompilationOrder[m_iUntrainedMethod], m_MethodCompilationOrder.GetCount() - m_iUntrainedMethod, sizeof(ZapMethodHeader *), LayoutOrderCmp);
}
