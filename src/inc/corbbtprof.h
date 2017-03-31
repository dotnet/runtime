// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*****************************************************************************\
*                                                                             *
* CorBBTProf.h -    File format for profile data                              *
*                                                                             *
*               Version 1.0                                                   *
*******************************************************************************
*                                                                             *
*  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY      *
*  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE        *
*  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR      *
*  PURPOSE.                                                                   *
*                                                                             *
\*****************************************************************************/

#ifndef _COR_BBTPROF_H_
#define _COR_BBTPROF_H_

#include <cor.h>
#include <corinfo.h>

const CorTokenType     ibcExternalNamespace    = CorTokenType(0x61000000);
const CorTokenType     ibcExternalType         = CorTokenType(0x62000000);
const CorTokenType     ibcExternalSignature    = CorTokenType(0x63000000);
const CorTokenType     ibcExternalMethod       = CorTokenType(0x64000000);
const CorTokenType     ibcTypeSpec             = CorTokenType(0x68000000);
const CorTokenType     ibcMethodSpec           = CorTokenType(0x69000000);

typedef mdToken idExternalNamespace;    // External Namespace token in the IBC data
typedef mdToken idExternalType;         // External Type token in the IBC data
typedef mdToken idExternalSignature;    // External Signature token in the IBC data
typedef mdToken idExternalMethod;       // External Method token in the IBC data
typedef mdToken idTypeSpec;             // TypeSpec token in the IBC data
typedef mdToken idMethodSpec;           // MethodSpec token in the IBC data

#define idExternalNamespaceNil      ((idExternalNamespace) ibcExternalNamespace)
#define idExternalTypeNil           ((idExternalType)      ibcExternalType)
#define idExternalSignatureNil      ((idExternalSignature) ibcExternalSignature)
#define idExternalMethodNil         ((idExternalMethod)    ibcExternalMethod)
#define idTypeSpecNil               ((idTypeSpec)          ibcTypeSpec)
#define idMethodSpecNil             ((idMethodSpec)        ibcMethodSpec)

//
// File format:
//
// CORBBTPROF_FILE_HEADER
// CORBBTPROF_SECTION_TABLE_HEADER 
// CORBBTPROF_SECTION_TABLE_ENTRY
// ... (can be multiple entries)
//
// Method block counts section:
// CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER 
//   CORBBTPROF_METHOD_HEADER 
//     CORBBTPROF_BLOCK_DATA
// ... (can be multiple method header/block data entries)
//
// Method load order section:
// CORBBTPROF_TOKEN_LIST_SECTION_HEADER
// ... (list of tokens)
//
// Type token usage information
// CORBBTPROF_TOKEN_LIST_SECTION_HEADER
// ... (list of tokens)
//

// MethodDef token usage information
// CORBBTPROF_TOKEN_LIST_SECTION_HEADER
// ... (list of tokens)
//

// RIDs to not use slim headers section
// CORBBTPROF_TOKEN_LIST_SECTION_HEADER
// ... (list of tokens)
//

// Metadata hints to re-order some tables
// Instantiated TypeSPecs to re-order EEClasses
//
// The header for the profile data file.
// ... (list of CORBBTPROF_BLOB_ENTRY)
//      terminated by null

struct CORBBTPROF_FILE_HEADER
{
    DWORD                          HeaderSize;
    DWORD                          Magic;
    DWORD                          Version;
    GUID                           MVID;
};

// Optional in V1 and V2. Usually present in V2. Must be present in V3.
struct CORBBTPROF_FILE_OPTIONAL_HEADER
{
    DWORD                          Size; // Including the size field
    DWORD                          MinorVersion;
    DWORD                          FileFlags; // Only in V3 or later
    // future fields
};

enum CORBBTPROF_FILE_FLAGS
{
    CORBBTPROF_FILE_FLAG_MINIFIED  = 1,
    CORBBTPROF_FILE_FLAG_PARTIAL_NGEN = 2
};

enum
{
    CORBBTPROF_V0_VERSION          = 0,
    CORBBTPROF_V1_VERSION          = 1,
    CORBBTPROF_V2_VERSION          = 2,
    CORBBTPROF_V3_VERSION          = 3,
    CORBBTPROF_CURRENT_VERSION     = CORBBTPROF_V2_VERSION, // V3 is opt-in
    CORBBTPROF_MAGIC               = 0xb1d0f11e,
    CORBBTPROF_END_TOKEN           = 0xb4356f98
};

//
// The profile data can be mapped anywhere in memory.  So instead of using pointers,
// to denote sections, we will instead use offsets from the beginning of the file.
//

struct Section
{
    DWORD                          Offset;
    DWORD                          Size;
};

//
// Section types, where various sections contains different types of profile data.
//

#define CORBBTPROF_TOKEN_MAX_NUM_FLAGS 32

enum TypeProfilingDataFlags
{
    // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
    ReadMethodTable               = 0,  // 0x00001
    ReadEEClass                   = 1,  // 0x00002
    WriteEEClass                  = 2,  // 0x00004
//  ReadStoredEnumData            = 3,  // 0x00008  // obsolete
    ReadFieldDescs                = 4,  // 0x00010
    ReadCCtorInfo                 = 5,  // 0x00020
    ReadClassHashTable            = 6,  // 0x00040
    ReadDispatchMap               = 7,  // 0x00080
    ReadDispatchTable             = 8,  // 0x00100
    ReadMethodTableWriteableData  = 9,  // 0x00200
    ReadFieldMarshalers           = 10, // 0x00400
//  WriteDispatchTable            = 11, // 0x00800  // obsolete
//  WriteMethodTable              = 12, // 0x01000  // obsolete
    WriteMethodTableWriteableData = 13, // 0x02000
    ReadTypeDesc                  = 14, // 0x04000
    WriteTypeDesc                 = 15, // 0x08000
    ReadTypeHashTable             = 16, // 0x10000
//  WriteTypeHashTable            = 17, // 0x20000  // obsolete
//  ReadDictionary                = 18, // 0x40000  // obsolete
//  WriteDictionary               = 19, // 0x80000  // obsolete
    ReadNonVirtualSlots           = 20, // 0x100000
};

enum MethodProfilingDataFlags
{
    // Important: update toolbox\ibcmerge\ibcmerge.cs if you change these
    ReadMethodCode                = 0,  // 0x00001  // Also means the method was executed
    ReadMethodDesc                = 1,  // 0x00002
    RunOnceMethod                 = 2,  // 0x00004
    RunNeverMethod                = 3,  // 0x00008
//  MethodStoredDataAccess        = 4,  // 0x00010  // obsolete
    WriteMethodDesc               = 5,  // 0x00020
//  ReadFCallHash                 = 6,  // 0x00040  // obsolete
    ReadGCInfo                    = 7,  // 0x00080
    CommonReadGCInfo              = 8,  // 0x00100
//  ReadMethodDefRidMap           = 9,  // 0x00200  // obsolete
    ReadCerMethodList             = 10, // 0x00400
    ReadMethodPrecode             = 11, // 0x00800
    WriteMethodPrecode            = 12, // 0x01000
    ExcludeHotMethodCode          = 13, // 0x02000  // Hot method should be excluded from the ReadyToRun image
    ExcludeColdMethodCode         = 14, // 0x04000  // Cold method should be excluded from the ReadyToRun image
    DisableInlining               = 15, // 0x08000  // Disable inlining of this method in optimized AOT native code
};

enum GeneralProfilingDataFlags
{
    // Important: update ibcmerge.cs if you change these
    // ZapImage.h depends on 0xFFFFFFFF being an invalid flag value. If this
    // changes, update ReadFlagWithMemory in that file.
    // Important: make sure these don't collide with TypeProfilingDataFlags or MethodProfilingDataFlags
    // These grow downward from CORBBTPROF_TOKEN_MAX_NUM_FLAGS-1 to minimize the chance of collision
    ProfilingFlags_MetaData                = 31, // 0x800...
    CommonMetaData                         = 30, // 0x400...
    RidMap                                 = 29, // 0x200...
    RVAFieldData                           = 28, // 0x100...
    ProfilingFlags_MetaDataSearch          = 27, // 0x080...
};

enum BlobType
{
    /* IMPORTANT: Keep the first four enums together in the same order and at
       the very begining of this enum. See MetaModelPub.h for the order */
    MetadataStringPool          = 0,
    MetadataGuidPool            = 1,
    MetadataBlobPool            = 2,
    MetadataUserStringPool      = 3,

    FirstMetadataPool           = 0,
    LastMetadataPool            = 3,

    // SectionFormat only supports tokens, which have to already exist in the module.
    // For instantiated paramterized types, there may be no corresponding token
    // in the module, if a dependent module caused the type to be instantiated.
    // For such instantiated types, we save a blob/signature to identify the type.
    // 
    ParamTypeSpec               = 4,    // Instantiated Type Signature
    ParamMethodSpec             = 5,    // Instantiated Method Signature
    ExternalNamespaceDef        = 6,    // External Namespace Token Definition 
    ExternalTypeDef             = 7,    // External Type Token Definition
    ExternalSignatureDef        = 8,    // External Signature Definition
    ExternalMethodDef           = 9,    // External Method Token Definition

    IllegalBlob                 = 10,   // Failed to allocate the blob

    EndOfBlobStream             = -1
};

enum SectionFormat
{
    // Important: update ibcmerge.cs if you change these
    ScenarioInfo                        = 0,
    MethodBlockCounts                   = 1, // Basic-block counts. Cold blocks will be placed in the cold-code section
    BlobStream                          = 2, // metadata access, inst-type-spec and inst-method-spec blobs

    FirstTokenFlagSection               = 3,

    ModuleProfilingData                 = FirstTokenFlagSection + (mdtModule >> 24),
    TypeRefProfilingData                = FirstTokenFlagSection + (mdtTypeRef >> 24),
    TypeProfilingData                   = FirstTokenFlagSection + (mdtTypeDef >> 24),
    FieldDefProfilingData               = FirstTokenFlagSection + (mdtFieldDef >> 24),
    MethodProfilingData                 = FirstTokenFlagSection + (mdtMethodDef >> 24),
    ParamDefProfilingData               = FirstTokenFlagSection + (mdtParamDef >> 24),
    InterfaceImplProfilingData          = FirstTokenFlagSection + (mdtInterfaceImpl >> 24),
    MemberRefProfilingData              = FirstTokenFlagSection + (mdtMemberRef >> 24),
    CustomAttributeProfilingData        = FirstTokenFlagSection + (mdtCustomAttribute >> 24),
    PermissionProfilingData             = FirstTokenFlagSection + (mdtPermission >> 24),
    SignatureProfilingData              = FirstTokenFlagSection + (mdtSignature >> 24),
    EventProfilingData                  = FirstTokenFlagSection + (mdtEvent >> 24),
    PropertyProfilingData               = FirstTokenFlagSection + (mdtProperty >> 24),
    ModuleRefProfilingData              = FirstTokenFlagSection + (mdtModuleRef >> 24),
    TypeSpecProfilingData               = FirstTokenFlagSection + (mdtTypeSpec >> 24),
    AssemblyProfilingData               = FirstTokenFlagSection + (mdtAssembly >> 24),
    AssemblyRefProfilingData            = FirstTokenFlagSection + (mdtAssemblyRef >> 24),
    FileProfilingData                   = FirstTokenFlagSection + (mdtFile >> 24),
    ExportedTypeProfilingData           = FirstTokenFlagSection + (mdtExportedType >> 24),
    ManifestResourceProfilingData       = FirstTokenFlagSection + (mdtManifestResource >> 24),
    GenericParamProfilingData           = FirstTokenFlagSection + (mdtGenericParam >> 24),
    MethodSpecProfilingData             = FirstTokenFlagSection + (mdtMethodSpec >> 24),
    GenericParamConstraintProfilingData = FirstTokenFlagSection + (mdtGenericParamConstraint >> 24),

    StringPoolProfilingData,
    GuidPoolProfilingData,
    BlobPoolProfilingData,
    UserStringPoolProfilingData,

    FirstMetadataPoolSection            = StringPoolProfilingData, 
    LastMetadataPoolSection             = UserStringPoolProfilingData,
    LastTokenFlagSection                = LastMetadataPoolSection,

    IbcTypeSpecSection,
    IbcMethodSpecSection,

    GenericTypeProfilingData            = 63, // Deprecated with V2 IBC data
    SectionFormatCount                  = 64, // 0x40

    SectionFormatInvalid                = -1
};

struct CORBBTPROF_SECTION_TABLE_ENTRY
{
    SectionFormat                  FormatID;
    Section                        Data;
};

struct CORBBTPROF_SECTION_TABLE_HEADER
{
    DWORD                          NumEntries;
    CORBBTPROF_SECTION_TABLE_ENTRY Entries[0];
};

//
// ScenarioInfo section
//

struct CORBBTPROF_SCENARIO_RUN
{
    FILETIME  runTime;      // the FILETIME when the scenario was cnt    
    GUID      mvid;         // The GUID of this assembly when the scenario was run (useful for incremental ibcdata)
    DWORD     cCmdLine;     // the count of WCHAR's in the cmdLine for this run of the scenario
    DWORD     cSystemInfo;  // the count of WCHAR's in the systemInfo string for this run of the scenario  
    WCHAR     cmdLine[0];   // the command line used, the array is 'cName' in length   
//  WCHAR     systemInfo[]; // the system information, the array is 'cSystemInfo' in length

    DWORD sizeofCmdLine()
    {
        return (cCmdLine * (DWORD)sizeof(WCHAR));
    }

    DWORD sizeofSystemInfo()
    {
        return (cSystemInfo * (DWORD)sizeof(WCHAR));
    }

    DWORD Size()
    {
        return (DWORD)sizeof(CORBBTPROF_SCENARIO_RUN) + sizeofCmdLine() + sizeofSystemInfo();
    }

    CORBBTPROF_SCENARIO_RUN* GetNextRun()
    {
        return reinterpret_cast< CORBBTPROF_SCENARIO_RUN* >(
                reinterpret_cast< PBYTE >( this + 1 ) + Size() );
    }
};

struct CORBBTPROF_SCENARIO_INFO
{
    DWORD     ordinal;      // the id number for this scenario
    DWORD     mask;         // the one-bit mask use to identify this scenario
    DWORD     priority;     // the priority of this scenario 
    DWORD     numRuns;      // the number of times this scenario was run
    DWORD     cName;        // the count of WCHAR's in name[]
    WCHAR     name[0];      // the name of this scenario, the array is 'cName' in length
//  CORBBTPROF_SCENARIO_RUN run[]; // the array is 'numRuns' in length

    DWORD sizeofName()
    {
        return (DWORD) (cName * sizeof(WCHAR));
    }

    DWORD Size()
    {
        return (DWORD) sizeof(CORBBTPROF_SCENARIO_INFO) + sizeofName() + sizeofRuns();
    }

    CORBBTPROF_SCENARIO_RUN* GetScenarioRun()
    {
        return reinterpret_cast< CORBBTPROF_SCENARIO_RUN* >(
                reinterpret_cast< PBYTE >( this ) + (DWORD)sizeof(CORBBTPROF_SCENARIO_INFO) + sizeofName());
    }

    DWORD sizeofRuns()
    {
        DWORD sum = 0;
        if (numRuns > 0)
        {
            DWORD cnt = 1;
            CORBBTPROF_SCENARIO_RUN* pRun = GetScenarioRun();
            do
            {
                sum += pRun->Size();
                if (cnt == numRuns)
                    break;
                cnt++;
                pRun = pRun->GetNextRun();
            }
            while (true);
        }
        return sum;
    }
};

struct CORBBTPROF_SCENARIO_HEADER
{
    DWORD                    size;         // Size to skip to get to the next CORBBTPROF_SCENARIO_HEADER
    CORBBTPROF_SCENARIO_INFO scenario;

    DWORD Size()
    {
        return (DWORD) sizeof(CORBBTPROF_SCENARIO_HEADER) + scenario.sizeofName() + scenario.sizeofRuns();
    }
};

struct CORBBTPROF_SCENARIO_INFO_SECTION_HEADER
{
    DWORD                       TotalNumRuns;
    DWORD                       NumScenarios;
//  CORBBTPROF_SCENARIO_HEADER  scenario[0];   // array is 'NumScenarios' in length
};

//
// MethodBlockCounts section
//

struct CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER_V1
{
    DWORD                          NumMethods;
    DWORD                          NumRuns;
};

struct CORBBTPROF_METHOD_BLOCK_COUNTS_SECTION_HEADER
{
    DWORD                          NumMethods;
};

struct CORBBTPROF_BLOCK_DATA    // Also defined here code:ICorJitInfo.ProfileBuffer
{
    DWORD                          ILOffset;
    DWORD                          ExecutionCount;
};

struct CORBBTPROF_METHOD_DETAIL_HEADER
{
    DWORD                          size;            // Size to skip to get to the next CORBBTPROF_METHOD_DETAIL_HEADER at this level   
    DWORD                          kind;            // Identifier that specifies what kind this CORBBTPROF_METHOD_DETAIL_HEADER actually represents

    size_t Size()
    {
        return size; 
    }
};

//
//  This struct records the basic block execution counts for a method
//
struct CORBBTPROF_METHOD_INFO
{
    DWORD                          token;             // token for this method
    DWORD                          ILSize;            // IL size for this method
    DWORD                          cBlock;            // count for block[]
    CORBBTPROF_BLOCK_DATA          block[0];          // actually 'cBlock' in length 

    size_t Size()
    { 
        return sizeof(CORBBTPROF_METHOD_INFO) + sizeofBlock(); 
    }

    size_t sizeofBlock() 
    {
        return cBlock * sizeof(CORBBTPROF_BLOCK_DATA); 
    }
};

struct CORBBTPROF_METHOD_HEADER_V1
{
    DWORD                          HeaderSize;
    mdToken                        MethodToken;
    DWORD                          Size;
};

struct CORBBTPROF_METHOD_HEADER
{
    DWORD                          size;            // Size to skip to get to the next CORBBTPROF_METHOD_HEADER
    DWORD                          cDetail;         // the count of CORBBTPROF_METHOD_DETAIL_HEADER records that folow this record
    CORBBTPROF_METHOD_INFO         method;          // Basic block execution counts for a method
                                                    // ... followed by 'cDetail' occurances of CORBBTPROF_METHOD_DETAIL_HEADER

    size_t Size()
    {
        return sizeof(CORBBTPROF_METHOD_HEADER) + method.sizeofBlock(); 
    }
};


struct CORBBTPROF_TOKEN_LIST_SECTION_HEADER
{
    DWORD                          NumTokens;
};

struct CORBBTPROF_TOKEN_LIST_ENTRY_V1
{
    mdToken                        token;
    DWORD                          flags;
};

struct CORBBTPROF_TOKEN_INFO                   // Was CORBBTPROF_TOKEN_LIST_ENTRY
{
    mdToken                        token;
    DWORD                          flags;
    DWORD                          scenarios;   // Could use UINT64 instead

    CORBBTPROF_TOKEN_INFO()
        : token(0)
        , flags(0)
        , scenarios(0)
    {}
    
    CORBBTPROF_TOKEN_INFO( mdToken t, DWORD f = 0, DWORD s = 0)
       : token(t)
        , flags(f)
        , scenarios(s)
    {}

    CORBBTPROF_TOKEN_INFO( CORBBTPROF_TOKEN_INFO const & right )
        : token(right.token)
        , flags(right.flags)
        , scenarios(right.scenarios)
    {}

    CORBBTPROF_TOKEN_INFO operator=( CORBBTPROF_TOKEN_INFO const & right )
    {
        token = right.token;
        flags = right.flags;
        scenarios = right.scenarios;
        return *this;
    }

    bool operator<( CORBBTPROF_TOKEN_INFO const & right ) const
    {
        return token < right.token;
    }
};

struct CORBBTPROF_BLOB_ENTRY_V1
{
    BlobType        blobType;
    DWORD           flags;
    DWORD           cBuffer;
    BYTE            pBuffer[0];   // actually 'cBuffer' in length

    CORBBTPROF_BLOB_ENTRY_V1 * GetNextEntry()
    {
        return reinterpret_cast< CORBBTPROF_BLOB_ENTRY_V1* >(
                reinterpret_cast< PBYTE >( this + 1 ) + cBuffer );
    }
};

struct CORBBTPROF_BLOB_ENTRY
{
    DWORD           size;
    BlobType        type;
    mdToken         token;    // The code:CORBBTPROF_BLOB_ENTRY.token field is not a real meta-data token
                              // but a look-alike that IBCMerge makes to represent blob entry

    bool TypeIsValid()
    {
        return (type >= MetadataStringPool) && (type < IllegalBlob);
    }

    CORBBTPROF_BLOB_ENTRY * GetNextEntry()
    {
        return reinterpret_cast< CORBBTPROF_BLOB_ENTRY* >(
                reinterpret_cast< PBYTE >( this ) + size);
    }
};

struct CORBBTPROF_BLOB_PARAM_SIG_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    DWORD                  cSig;
    COR_SIGNATURE          sig[0];  // actually 'cSig' in length
};

struct CORBBTPROF_BLOB_NAMESPACE_DEF_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    DWORD                  cName;
    CHAR                   name[0];  // actually cName in length
};

struct CORBBTPROF_BLOB_TYPE_DEF_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    mdToken                assemblyRefToken;
    mdToken                nestedClassToken;
    mdToken                nameSpaceToken;
    DWORD                  cName;
    CHAR                   name[0];  // actually cName in length
};

struct CORBBTPROF_BLOB_SIGNATURE_DEF_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    DWORD                  cSig;
    COR_SIGNATURE          sig[0];  // actually 'cSig' in length
};

struct CORBBTPROF_BLOB_METHOD_DEF_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    mdToken                nestedClassToken;
    mdToken                signatureToken;
    DWORD                  cName;
    CHAR                   name[0];  // actually cName in length
};

struct CORBBTPROF_BLOB_POOL_ENTRY
{
    CORBBTPROF_BLOB_ENTRY  blob;
    DWORD                  cBuffer;
    BYTE                   buffer[0];  // actually 'cBuffer' in length
};
#endif /* COR_BBTPROF_H_ */
