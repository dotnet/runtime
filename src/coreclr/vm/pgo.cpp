// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "log.h"
#include "pgo.h"
#include "versionresilienthashcode.h"
#include "typestring.h"
#include "pgo_formatprocessing.h"

#ifdef FEATURE_PGO

// Data structure for holding pgo data
// Need to be walkable at process shutdown without taking meaningful locks
//  Need to have an associated MethodDesc for emission
//
//  Need to support lookup by Exact method, and at the non-generic level as well
//   In addition, lookup by some form of stable hash would be really nice for both R2R multi-module scenarios
//    as well as the existing text format approach
//
// In the current implementation, the method stable hash code isn't a good replacement for "token" as it doesn't
// carry any detail about signatures, and is probably quite slow to compute
// The plan is to swap over to the typenamehash

// Goals
// 1. Need to be able to walk at any time.
// 2. Need to be able to lookup by MethodDesc
// 3. Need to be able to lookup by Hash!

// Solution:

// Lookup patterns for use by JIT
// 1. For Current Runtime generated lookups, there is a SHash in each LoaderAllocator, using the MethodDesc as
//    key for non-dynamic methods, and a field in the DynamicMethodDesc for the dynamic methods.
// 2. For R2R lookups, lookup via IL token exact match, as well as a hash based lookup.
// 3. For text based lookups, lookup by hash (only enabled if the ReadPGOData COMPlus is set).

// For emission into output, we will use an approach that relies on walking linked lists
// 1. InstrumentationDataHeader shall be placed before any instrumentation data. It will be part of a linked
//    list of instrumentation data that has the same lifetime.
// 2. InstrumentationDataWithEqualLifetimeHeader shall be part of a doubly linked list. This list shall be protected
//    by a lock, and serves to point at the various singly linked lists of InstrumentationData.

const char* const         PgoManager::s_FileHeaderString  = "*** START PGO Data, max index = %u ***\n";
const char* const         PgoManager::s_FileTrailerString = "*** END PGO Data ***\n";
const char* const         PgoManager::s_MethodHeaderString = "@@@ codehash 0x%08X methodhash 0x%08X ilSize 0x%08X records 0x%08X\n";
const char* const         PgoManager::s_RecordString = "Schema InstrumentationKind %u ILOffset %u Count %u Other %u\n";
const char* const         PgoManager::s_None = "None\n";
const char* const         PgoManager::s_FourByte = "%u\n";
const char* const         PgoManager::s_EightByte = "%u %u\n";
const char* const         PgoManager::s_TypeHandle = "TypeHandle: %s\n";


PtrSHash<PgoManager::Header, PgoManager::CodeAndMethodHash> PgoManager::s_textFormatPgoData;
CrstStatic PgoManager::s_pgoMgrLock;
PgoManager PgoManager::s_InitialPgoManager;

void PgoManager::Initialize()
{
    STANDARD_VM_CONTRACT;

    s_pgoMgrLock.Init(CrstLeafLock, CRST_DEFAULT);

    // If we're reading in counts, do that now
    ReadPgoData();
}

void PgoManager::Shutdown()
{
    static bool written = false;

    if (!written)
    {
        written = true;
        WritePgoData();
    }
}

void PgoManager::VerifyAddress(void* address)
{
    // TODO Insert an assert to check that an address is a valid pgo address
}

class SArrayByteWriterFunctor
{
    SArray<uint8_t>& m_byteData;
public:
    SArrayByteWriterFunctor(SArray<uint8_t>& byteData) :
        m_byteData(byteData)
    {}

    bool operator()(uint8_t data) const
    {
        m_byteData.Append(data);
        return true;
    }
};

class SchemaWriterFunctor
{

public:
    StackSArray<uint8_t> byteData;
    StackSArray<TypeHandle> typeHandlesEncountered;
    StackSArray<MethodDesc*> methodsEncountered;
private:
    const PgoManager::HeaderList *pgoData;
    SArrayByteWriterFunctor byteWriter;
public:
    SchemaAndDataWriter<SArrayByteWriterFunctor> writer;

    SchemaWriterFunctor(PgoManager::HeaderList *pgoData) :
        pgoData(pgoData),
        byteWriter(byteData),
        writer(byteWriter, pgoData->header.GetData())
    {}

    bool operator()(const ICorJitInfo::PgoInstrumentationSchema &schema)
    {
        if (!writer.AppendSchema(schema))
            return false;

        auto thProcessor = [&](intptr_t thWritten)
        {
            if (thWritten == 0 || ICorJitInfo::IsUnknownHandle((intptr_t)thWritten))
                return;

            TypeHandle th = *(TypeHandle*)&thWritten;
            if (!th.IsNull())
            {
                typeHandlesEncountered.Append(th);
            }
            return;
        };

        auto mhProcessor = [&](intptr_t mhWritten)
        {
            if (mhWritten == 0 || ICorJitInfo::IsUnknownHandle(mhWritten))
                return;

            MethodDesc* pMD = reinterpret_cast<MethodDesc*>(mhWritten);
            methodsEncountered.Append(pMD);
        };

        if (!writer.AppendDataFromLastSchema(thProcessor, mhProcessor))
        {
            return false;
        }

        return true;
    }
};

#ifndef DACCESS_COMPILE

void CallFClose(FILE* file)
{
    fclose(file);
}

typedef Holder<FILE*, DoNothing, CallFClose> FILEHolder;

void PgoManager::WritePgoData()
{
    if (ETW_EVENT_ENABLED(MICROSOFT_WINDOWS_DOTNETRUNTIME_PROVIDER_DOTNET_Context, JitInstrumentationDataVerbose))
    {
        EnumeratePGOHeaders([](HeaderList *pgoData)
        {
            SchemaWriterFunctor schemaWriter(pgoData);
            if (ReadInstrumentationSchemaWithLayout(pgoData->header.GetData(), pgoData->header.SchemaSizeMax(), pgoData->header.countsOffset, schemaWriter))
            {
                if (!schemaWriter.writer.Finish())
                    return false;
                ETW::MethodLog::LogMethodInstrumentationData(
                    pgoData->header.method,
                    schemaWriter.byteData.GetCount(), schemaWriter.byteData.GetElements(),
                    schemaWriter.typeHandlesEncountered.GetElements(), schemaWriter.typeHandlesEncountered.GetCount(),
                    schemaWriter.methodsEncountered.GetElements(), schemaWriter.methodsEncountered.GetCount());
            }

            return true;
        });
    }

    if (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WritePGOData) == 0)
    {
        return;
    }

    int pgoDataCount = 0;
    EnumeratePGOHeaders([&pgoDataCount](HeaderList *pgoData)
    {
        pgoDataCount++;
        return true;
    });

    if (pgoDataCount == 0)
    {
        return;
    }
    CLRConfigStringHolder fileName(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PGODataPath));

    if (fileName == NULL)
    {
        return;
    }

    FILE* const pgoDataFile = _wfopen(fileName, W("w"));

    if (pgoDataFile == NULL)
    {
        return;
    }

    FILEHolder fileHolder(pgoDataFile);

    fprintf(pgoDataFile, s_FileHeaderString, pgoDataCount);

    EnumeratePGOHeaders([pgoDataFile](HeaderList *pgoData)
    {
        int32_t schemaItems;
        if (!CountInstrumentationDataSize(pgoData->header.GetData(), pgoData->header.SchemaSizeMax(), &schemaItems))
        {
            _ASSERTE(!"Invalid instrumentation schema");
            return true;
        }

        fprintf(pgoDataFile, s_MethodHeaderString, pgoData->header.codehash, pgoData->header.methodhash, pgoData->header.ilSize, schemaItems);

        SString tClass, tMethodName, tMethodSignature;
        pgoData->header.method->GetMethodInfo(tClass, tMethodName, tMethodSignature);

        fprintf(pgoDataFile, "MethodName: %s.%s\n", tClass.GetUTF8(), tMethodName.GetUTF8());
        fprintf(pgoDataFile, "Signature: %s\n", tMethodSignature.GetUTF8());

        uint8_t* data = pgoData->header.GetData();

        unsigned lastOffset  = 0;
        auto lambda = [data, pgoDataFile] (const ICorJitInfo::PgoInstrumentationSchema &schema)
        {
            fprintf(pgoDataFile, s_RecordString, schema.InstrumentationKind, schema.ILOffset, schema.Count, schema.Other);
            for (int32_t iEntry = 0; iEntry < schema.Count; iEntry++)
            {
                size_t entryOffset = schema.Offset + iEntry * InstrumentationKindToSize(schema.InstrumentationKind);

                switch(schema.InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask)
                {
                    case ICorJitInfo::PgoInstrumentationKind::None:
                        fprintf(pgoDataFile, s_None);
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::FourByte:
                        fprintf(pgoDataFile, s_FourByte, (unsigned)*(uint32_t*)(data + entryOffset));
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::EightByte:
                        // Print a pair of 4 byte values as the PRIu64 specifier isn't generally available
                        fprintf(pgoDataFile, s_EightByte, (unsigned)*(uint32_t*)(data + entryOffset), (unsigned)*(uint32_t*)(data + entryOffset + 4));
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
                        {
                            intptr_t thData = *(intptr_t*)(data + entryOffset);
                            if (thData == 0)
                            {
                                fprintf(pgoDataFile, s_TypeHandle, "NULL");
                            }
                            else if (ICorJitInfo::IsUnknownHandle(thData))
                            {
                                fprintf(pgoDataFile, s_TypeHandle, "UNKNOWN");
                            }
                            else
                            {
                                TypeHandle th = TypeHandle::FromPtr((void*)thData);
                                StackSString ss;
                                TypeString::AppendType(ss, th, TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly);
                                if (ss.GetCount() > 8192)
                                {
                                    fprintf(pgoDataFile, s_TypeHandle, "UNKNOWN");
                                }
                                else
                                {
                                    fprintf(pgoDataFile, s_TypeHandle, ss.GetUTF8());
                                }
                            }
                            break;
                        }
                    case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
                        {
                            intptr_t mdData = *(intptr_t*)(data + entryOffset);
                            if (mdData == 0)
                            {
                                fprintf(pgoDataFile, "MethodHandle: NULL\n");
                            }
                            else if (ICorJitInfo::IsUnknownHandle(mdData))
                            {
                                fprintf(pgoDataFile, "MethodHandle: UNKNOWN\n");
                            }
                            else
                            {
                                MethodDesc* md = reinterpret_cast<MethodDesc*>(mdData);
                                SString garbage1, tMethodName, garbage2;
                                md->GetMethodInfo(garbage1, tMethodName, garbage2);
                                StackSString tTypeName;
                                TypeString::AppendType(tTypeName, TypeHandle(md->GetMethodTable()), TypeString::FormatNamespace | TypeString::FormatFullInst | TypeString::FormatAssembly);
                                // Format is:
                                // MethodName|@|fully_qualified_type_name
                                if (tTypeName.GetCount() + 1 + tMethodName.GetCount() > 8192)
                                {
                                    fprintf(pgoDataFile, "MethodHandle: UNKNOWN\n");
                                }
                                else
                                {
                                    fprintf(pgoDataFile, "MethodHandle: %s|@|%s\n", tMethodName.GetUTF8(), tTypeName.GetUTF8());
                                }
                            }
                            break;
                        }
                    default:
                        break;
                }
            }
            return true;
        };

        if (!ReadInstrumentationSchemaWithLayout(pgoData->header.GetData(), pgoData->header.SchemaSizeMax(), pgoData->header.countsOffset, lambda))
        {
            return true;;
        }

        return true;
    });

    fprintf(pgoDataFile, s_FileTrailerString);
}
#endif // DACCESS_COMPILE

void ReadLineAndDiscard(FILE* file)
{
    char buffer[255];
    while (fgets(buffer, sizeof(buffer), file) != NULL)
    {
        auto stringLen = strlen(buffer);
        if (stringLen == 0)
            return;

        if (buffer[stringLen - 1] == '\n')
        {
            return;
        }
    }
}

#ifndef DACCESS_COMPILE
void PgoManager::ReadPgoData()
{
    // Skip, if we're not reading, or we're writing profile data, or doing tiered pgo
    //
    if (g_pConfig->TieredPGO() ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_WritePGOData) > 0) ||
        (CLRConfig::GetConfigValue(CLRConfig::INTERNAL_ReadPGOData) == 0))
    {
        return;
    }

    CLRConfigStringHolder fileName(CLRConfig::GetConfigValue(CLRConfig::INTERNAL_PGODataPath));

    if (fileName == NULL)
    {
        return;
    }

    FILE* const pgoDataFile = _wfopen(fileName, W("r"));

    if (pgoDataFile == NULL)
    {
        return;
    }

    FILEHolder fileHolder(pgoDataFile);

    char     buffer[16384];
    unsigned maxIndex = 0;

    // Header must be first line
    //
    if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
    {
        return;
    }

    if (sscanf_s(buffer, s_FileHeaderString, &maxIndex) != 1)
    {
        return;
    }


    // Fill in the data
    //
    unsigned methods = 0;
    unsigned probes = 0;


    while (true) // Read till the file is empty
    {
        if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
        {
            break;
        }

        bool failed = false;

        // Find the next method entry line
        //
        unsigned schemaCount = 0;
        unsigned codehash    = 0;
        unsigned methodhash  = 0;
        unsigned ilSize      = 0;

        if (sscanf_s(buffer, s_MethodHeaderString, &codehash, &methodhash, &ilSize, &schemaCount) != 4)
        {
            continue;
        }

        // Discard the next two lines that hold the string name of the method
        ReadLineAndDiscard(pgoDataFile);
        ReadLineAndDiscard(pgoDataFile);

        StackSArray<ICorJitInfo::PgoInstrumentationSchema> schemaElements;
        StackSArray<uint8_t> methodInstrumentationData;
        schemaElements.Preallocate((int)schemaCount);
        ICorJitInfo::PgoInstrumentationSchema lastSchema = {};

        for (unsigned i = 0; i < schemaCount; i++)
        {
            if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
            {
                failed = true;
                break;
            }

            // Read schema
            ICorJitInfo::PgoInstrumentationSchema schema;

            if (sscanf_s(buffer, s_RecordString, &schema.InstrumentationKind, &schema.ILOffset, &schema.Count, &schema.Other) != 4)
            {
                failed = true;
                break;
            }

            LayoutPgoInstrumentationSchema(lastSchema, &schema);
            schemaElements[i] = schema;
            COUNT_T entrySize = InstrumentationKindToSize(schema.InstrumentationKind);
            COUNT_T maxSize = entrySize * schema.Count + (COUNT_T)schema.Offset;
            methodInstrumentationData.SetCount(maxSize);

            for (int32_t iEntry = 0; !failed && iEntry < schema.Count; iEntry++)
            {
                size_t entryOffset = schema.Offset + iEntry * entrySize;
                if (fgets(buffer, sizeof(buffer), pgoDataFile) == nullptr)
                {
                    failed = true;
                    break;
                }

                switch(schema.InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask)
                {
                    case ICorJitInfo::PgoInstrumentationKind::None:
                        if (sscanf_s(buffer, s_None) != 0)
                        {
                            failed = true;
                        }
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::FourByte:
                        {
                            unsigned val;
                            if (sscanf_s(buffer, s_FourByte, &val) != 1)
                            {
                                failed = true;
                            }
                            else
                            {
                                uint8_t *rawBuffer = methodInstrumentationData.OpenRawBuffer(maxSize);
                                *(uint32_t *)(rawBuffer + entryOffset) = (uint32_t)val;
                                methodInstrumentationData.CloseRawBuffer();
                            }
                        }
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::EightByte:
                        {
                            // Print a pair of 4 byte values as the PRIu64 specifier isn't generally avaialble
                            unsigned val, val2;
                            if (sscanf_s(buffer, s_EightByte, &val, &val2) != 2)
                            {
                                failed = true;
                            }
                            else
                            {
                                uint8_t *rawBuffer = methodInstrumentationData.OpenRawBuffer(maxSize);
                                *(uint32_t *)(rawBuffer + entryOffset) = (uint32_t)val;
                                *(uint32_t *)(rawBuffer + entryOffset + 4) = (uint32_t)val2;
                                methodInstrumentationData.CloseRawBuffer();
                            }
                        }
                        break;
                    case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
                        {
                            char* typeString;
                            if (strncmp(buffer, "TypeHandle: ", 12) != 0)
                            {
                                failed = true;
                                break;
                            }
                            typeString = buffer + 12;
                            size_t endOfString = strlen(typeString);
                            if (endOfString == 0 || (typeString[endOfString - 1] != '\n'))
                            {
                                failed = true;
                                break;
                            }
                            // Remove \n and replace will null
                            typeString[endOfString - 1] = '\0';

                            TypeHandle th;
                            INT_PTR ptrVal = 0;
                            if ((strcmp(typeString, "NULL") != 0) && (strcmp(typeString, "UNKNOWN") != 0))
                            {
                                // As early type loading is likely problematic, simply drop the string into the data, and fix it up later
                                void* tempString = malloc(endOfString);
                                memcpy(tempString, typeString, endOfString);

                                ptrVal = (INT_PTR)tempString;
                                ptrVal += 1; // Set low bit to indicate that this isn't actually a TypeHandle, but is instead a pointer
                            }

                            uint8_t *rawBuffer = methodInstrumentationData.OpenRawBuffer(maxSize);
                            *(INT_PTR *)(rawBuffer + entryOffset) = ptrVal;
                            methodInstrumentationData.CloseRawBuffer();
                            break;
                        }
                    case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
                        {
                            char* methodString;
                            if (strncmp(buffer, "MethodHandle: ", 14) != 0)
                            {
                                failed = true;
                                break;
                            }
                            methodString = buffer + 14;
                            size_t endOfString = strlen(methodString);
                            if (endOfString == 0 || (methodString[endOfString - 1] != '\n'))
                            {
                                failed = true;
                                break;
                            }
                            // Remove \n and replace will null
                            methodString[endOfString - 1] = '\0';

                            TypeHandle th;
                            INT_PTR ptrVal = 0;
                            if ((strcmp(methodString, "NULL") != 0) && (strcmp(methodString, "UNKNOWN") != 0))
                            {
                                // As early type loading is likely problematic, simply drop the string into the data, and fix it up later
                                void* tempString = malloc(endOfString);
                                memcpy(tempString, methodString, endOfString);

                                ptrVal = (INT_PTR)tempString;
                                ptrVal += 1; // Set low bit to indicate that this isn't actually a TypeHandle, but is instead a pointer
                            }

                            uint8_t *rawBuffer = methodInstrumentationData.OpenRawBuffer(maxSize);
                            *(INT_PTR *)(rawBuffer + entryOffset) = ptrVal;
                            methodInstrumentationData.CloseRawBuffer();
                            break;
                        }
                    default:
                        break;
                }
            }

            if (failed)
                break;

            lastSchema = schema;
        }

        if (failed)
            continue;

        methods++;

        UINT offsetOfActualInstrumentationData;
        HRESULT hr = ComputeOffsetOfActualInstrumentationData(schemaElements.GetElements(), schemaCount, sizeof(Header), &offsetOfActualInstrumentationData);
        if (FAILED(hr))
        {
            continue;
        }
        UINT offsetOfInstrumentationDataFromStartOfDataRegion = offsetOfActualInstrumentationData - sizeof(Header);

        // Adjust schema offsets to account for embedding the instrumentation schema in front of the data
        for (unsigned iSchema = 0; iSchema < schemaCount; iSchema++)
        {
            schemaElements[iSchema].Offset += offsetOfInstrumentationDataFromStartOfDataRegion;
        }

        S_SIZE_T allocationSize = S_SIZE_T(offsetOfActualInstrumentationData) + S_SIZE_T(methodInstrumentationData.GetCount());
        if (allocationSize.IsOverflow())
        {
            _ASSERTE(!"Unexpected overflow");
            return;
        }

        Header* methodData = (Header*)malloc(allocationSize.Value());
        methodData->HashInit(methodhash, codehash, ilSize, offsetOfInstrumentationDataFromStartOfDataRegion);

        if (!WriteInstrumentationSchema(schemaElements.GetElements(), schemaCount, methodData->GetData(), offsetOfInstrumentationDataFromStartOfDataRegion))
        {
            _ASSERTE(!"Unable to write schema");
            return;
        }

        methodInstrumentationData.Copy(((uint8_t*)methodData) + offsetOfActualInstrumentationData, methodInstrumentationData.Begin(), methodInstrumentationData.GetCount());

        s_textFormatPgoData.Add(methodData);
        probes += schemaCount;
    }
}
#endif // DACCESS_COMPILE

void PgoManager::CreatePgoManager(PgoManager* volatile* ppMgr, bool loaderAllocator)
{
    CrstHolder lock(&s_pgoMgrLock);
    if (*ppMgr != NULL)
        return;

    PgoManager* newManager;
    if (loaderAllocator)
        newManager = new LoaderAllocatorPgoManager();
    else
        newManager = new PgoManager();

    VolatileStore((PgoManager**)ppMgr, newManager);
}

void PgoManager::Header::Init(MethodDesc *pMD, unsigned codehash, unsigned ilSize, unsigned countsOffset)
{
    this->codehash = codehash;
    this->methodhash = pMD->GetStableHash();
    this->ilSize = ilSize;
    this->method = pMD;
    this->countsOffset = countsOffset;
}

HRESULT PgoManager::allocPgoInstrumentationBySchema(MethodDesc* pMD, ICorJitInfo::PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, BYTE** pInstrumentationData)
{
    STANDARD_VM_CONTRACT;

    PgoManager* mgr;
    if (!pMD->IsDynamicMethod())
    {
        mgr = pMD->GetLoaderAllocator()->GetOrCreatePgoManager();
    }
    else
    {
        PgoManager* volatile* ppMgr = pMD->AsDynamicMethodDesc()->GetResolver()->GetDynamicPgoManagerPointer();
        if (ppMgr == NULL)
        {
            return E_NOTIMPL;
        }

        CreatePgoManager(ppMgr, false);
        mgr = *ppMgr;
    }

    if (mgr == NULL)
    {
        return E_NOTIMPL;
    }

    return mgr->allocPgoInstrumentationBySchemaInstance(pMD, pSchema, countSchemaItems, pInstrumentationData);
}

HRESULT PgoManager::ComputeOffsetOfActualInstrumentationData(const ICorJitInfo::PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, size_t headerInitialSize, UINT *offsetOfActualInstrumentationData)
{
    // Determine size of compressed schema representation
    size_t headerSize = headerInitialSize;
    if (!WriteInstrumentationSchemaToBytes(pSchema, countSchemaItems, [&headerSize](uint8_t byte) { headerSize = headerSize + 1; return true; }))
    {
        return E_NOTIMPL;
    }

    // Align all instrumentation at size_t alignment, as the copy routine will copy in size_t units
    *offsetOfActualInstrumentationData = (UINT)AlignUp(headerSize, sizeof(size_t));
    return S_OK;
}

HRESULT PgoManager::allocPgoInstrumentationBySchemaInstance(MethodDesc* pMD,
                                                            ICorJitInfo::PgoInstrumentationSchema* pSchema,
                                                            UINT32 countSchemaItems,
                                                            BYTE** pInstrumentationData)
{
    // Initialize our out param
    *pInstrumentationData = NULL;
    int codehash;
    unsigned ilSize;

    if (!GetVersionResilientILCodeHashCode(pMD, &codehash, &ilSize))
    {
        return E_NOTIMPL;
    }

    UINT offsetOfActualInstrumentationData;
    HRESULT hr = ComputeOffsetOfActualInstrumentationData(pSchema, countSchemaItems, sizeof(HeaderList), &offsetOfActualInstrumentationData);
    UINT offsetOfInstrumentationDataFromStartOfDataRegion = offsetOfActualInstrumentationData - sizeof(HeaderList);
    if (FAILED(hr))
    {
        return hr;
    }

    // Compute offsets for each instrumentation entry
    ICorJitInfo::PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(ICorJitInfo::PgoInstrumentationSchema));
    prevSchema.Offset = offsetOfInstrumentationDataFromStartOfDataRegion;
    for (UINT32 iSchema = 0; iSchema < countSchemaItems; iSchema++)
    {
        LayoutPgoInstrumentationSchema(prevSchema, &pSchema[iSchema]);
        prevSchema = pSchema[iSchema];
    }

    S_SIZE_T allocationSize = S_SIZE_T(sizeof(HeaderList)) + S_SIZE_T(pSchema[countSchemaItems - 1].Offset) + S_SIZE_T(pSchema[countSchemaItems - 1].Count) * S_SIZE_T(InstrumentationKindToSize(pSchema[countSchemaItems - 1].InstrumentationKind));

    if (allocationSize.IsOverflow())
    {
        return E_NOTIMPL;
    }
    size_t unsafeAllocationSize = allocationSize.Value();
    if (unsafeAllocationSize % sizeof(size_t) == 4)
    {
        allocationSize = allocationSize + S_SIZE_T(4);

        if (allocationSize.IsOverflow())
        {
            return E_NOTIMPL;
        }
        unsafeAllocationSize = allocationSize.Value();
    }

    HeaderList* pHeaderList = NULL;

    if (pMD->IsDynamicMethod())
    {
        HeaderList *currentHeaderList = m_pgoHeaders;
        if (currentHeaderList != NULL)
        {
            if (!CheckIfPgoSchemaIsCompatibleAndSetOffsets(currentHeaderList->header.GetData(), currentHeaderList->header.countsOffset, pSchema, countSchemaItems))
            {
                return E_NOTIMPL;
            }
            _ASSERTE(currentHeaderList->header.method == pMD);
            *pInstrumentationData = currentHeaderList->header.GetData();
            return S_OK;
        }

        pHeaderList = (HeaderList*)pMD->AsDynamicMethodDesc()->GetResolver()->GetJitMetaHeap()->New(unsafeAllocationSize);

        memset(pHeaderList, 0, unsafeAllocationSize);
        pHeaderList->header.Init(pMD, codehash, ilSize, offsetOfInstrumentationDataFromStartOfDataRegion);
        *pInstrumentationData = pHeaderList->header.GetData();
        if (!WriteInstrumentationSchema(pSchema, countSchemaItems, *pInstrumentationData, pHeaderList->header.countsOffset))
        {
            _ASSERTE(!"Unable to write schema");
            return E_NOTIMPL;
        }
        m_pgoHeaders = pHeaderList;
        return S_OK;
    }
    else
    {
        LoaderAllocatorPgoManager *laPgoManagerThis = (LoaderAllocatorPgoManager *)this;
        CrstHolder lock(&laPgoManagerThis->m_lock);

        HeaderList* existingData = laPgoManagerThis->m_pgoDataLookup.Lookup(pMD);
        if (existingData != NULL)
        {
            if (!CheckIfPgoSchemaIsCompatibleAndSetOffsets(existingData->header.GetData(), existingData->header.countsOffset, pSchema, countSchemaItems))
            {
                return E_NOTIMPL;
            }
            *pInstrumentationData = existingData->header.GetData();
            return S_OK;
        }

        AllocMemTracker loaderHeapAllocation;
        pHeaderList = (HeaderList*)loaderHeapAllocation.Track(pMD->GetLoaderAllocator()->GetHighFrequencyHeap()->AllocMem(allocationSize));
        memset(pHeaderList, 0, unsafeAllocationSize);
        pHeaderList->header.Init(pMD, codehash, ilSize, offsetOfInstrumentationDataFromStartOfDataRegion);
        pHeaderList->next = m_pgoHeaders;
        *pInstrumentationData = pHeaderList->header.GetData();
        if (!WriteInstrumentationSchema(pSchema, countSchemaItems, *pInstrumentationData, pHeaderList->header.countsOffset))
        {
            _ASSERTE(!"Unable to write schema");
            return E_NOTIMPL;
        }
        laPgoManagerThis->m_pgoDataLookup.Add(pHeaderList);
        loaderHeapAllocation.SuppressRelease();
        m_pgoHeaders = pHeaderList;
        return S_OK;
    }
}

#ifndef DACCESS_COMPILE
HRESULT PgoManager::getPgoInstrumentationResults(MethodDesc* pMD, BYTE** pAllocatedData, ICorJitInfo::PgoInstrumentationSchema** ppSchema, UINT32 *pCountSchemaItems, BYTE**pInstrumentationData, ICorJitInfo::PgoSource *pPgoSource)
{
    // Initialize our out params
    *pAllocatedData = NULL;
    *pInstrumentationData = NULL;
    *pCountSchemaItems = 0;
    *pPgoSource = ICorJitInfo::PgoSource::Unknown;

    HRESULT hr = E_NOTIMPL;

    // If there is text format PGO data, prefer that over any dynamic or static data.
    //
    if (s_textFormatPgoData.GetCount() > 0)
    {
        hr = getPgoInstrumentationResultsFromText(pMD, pAllocatedData, ppSchema, pCountSchemaItems, pInstrumentationData, pPgoSource);
    }

    // If we didn't find any text format data, look for dynamic or static data.
    //
    if (FAILED(hr))
    {
        PgoManager *mgr;
        if (!pMD->IsDynamicMethod())
        {
            mgr = pMD->GetLoaderAllocator()->GetPgoManager();
        }
        else
        {
            mgr = pMD->AsDynamicMethodDesc()->GetResolver()->GetDynamicPgoManager();
        }

        if (mgr != NULL)
        {
            hr = mgr->getPgoInstrumentationResultsInstance(pMD, pAllocatedData, ppSchema, pCountSchemaItems, pInstrumentationData, pPgoSource);
        }
    }

    return hr;
}

HRESULT PgoManager::getPgoInstrumentationResultsFromText(MethodDesc* pMD, BYTE** pAllocatedData, ICorJitInfo::PgoInstrumentationSchema** ppSchema, UINT32* pCountSchemaItems, BYTE** pInstrumentationData, ICorJitInfo::PgoSource* pPgoSource)
{
    int codehash;
    unsigned ilSize;
    if (!GetVersionResilientILCodeHashCode(pMD, &codehash, &ilSize))
    {
        return E_NOTIMPL;
    }

    COUNT_T methodhash = pMD->GetStableHash();
    Header* found = s_textFormatPgoData.Lookup(CodeAndMethodHash(codehash, methodhash));
    if (found == NULL)
    {
        return E_NOTIMPL;
    }

    StackSArray<ICorJitInfo::PgoInstrumentationSchema> schemaArray;

    if (!ReadInstrumentationSchemaWithLayoutIntoSArray(found->GetData(), found->countsOffset, found->countsOffset, &schemaArray))
    {
        _ASSERTE(!"Unable to parse schema data");
        return E_NOTIMPL;
    }

    HRESULT hr = E_NOTIMPL;
    EX_TRY
    {
        for (unsigned iSchema = 0; iSchema < schemaArray.GetCount(); iSchema++)
        {
            ICorJitInfo::PgoInstrumentationSchema* schema = &(schemaArray)[iSchema];
            ICorJitInfo::PgoInstrumentationKind kind = schema->InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask;
            if ((kind == ICorJitInfo::PgoInstrumentationKind::TypeHandle) || (kind == ICorJitInfo::PgoInstrumentationKind::MethodHandle))
            {
                for (int iEntry = 0; iEntry < schema->Count; iEntry++)
                {
                    INT_PTR* handleValueAddress = (INT_PTR*)(found->GetData() + schema->Offset + iEntry * InstrumentationKindToSize(schema->InstrumentationKind));
                    INT_PTR initialHandleValue = VolatileLoad(handleValueAddress);

                    // TypeHandles can't reliably be loaded at ReadPGO time
                    // Instead, translate them before leaving this method.
                    // The ReadPgo method will place pointers to C style null
                    // terminated strings in the TypeHandle slots, and this will
                    // translate any of those into loaded TypeHandles as appropriate
                    if (((initialHandleValue & 1) == 1) && !ICorJitInfo::IsUnknownHandle(initialHandleValue))
                    {
                        INT_PTR newPtr = 0;
                        char* string = ((char*)initialHandleValue) - 1;

                        // Resolving types and methods here can invoke managed code where the
                        // JIT may recursively ask for PGO data. We do not support textual PGO
                        // for those cases.
                        static thread_local bool t_resolvingTypeOrMethod;

                        struct ResolveScope
                        {
                            ResolveScope()
                            {
                                t_resolvingTypeOrMethod = true;
                            }

                            ~ResolveScope()
                            {
                                t_resolvingTypeOrMethod = false;
                            }
                        };

                        // Don't attempt to load any types or methods until the EE is started
                        if (g_fEEStarted && !t_resolvingTypeOrMethod)
                        {
                            ResolveScope resolve;

                            if (kind == ICorJitInfo::PgoInstrumentationKind::TypeHandle)
                            {
                                StackSString ts(SString::Utf8, string);
                                TypeHandle th = TypeName::GetTypeFromAsmQualifiedName(ts.GetUnicode());
                                newPtr = (INT_PTR)th.AsPtr();
                            }
                            else
                            {
                                assert(kind == ICorJitInfo::PgoInstrumentationKind::MethodHandle);
                                // Format is:
                                // MethodName|@|fully_qualified_type_name
                                char* sep = strstr(string, "|@|");
                                if (sep != nullptr)
                                {
                                    StackSString typeString(SString::Utf8, sep + 3);
                                    StackSString methodString(SString::Utf8, string, (COUNT_T)(sep - string));
                                    TypeHandle th = TypeName::GetTypeFromAsmQualifiedName(typeString.GetUnicode());

                                    if (!th.IsNull())
                                    {
                                        MethodDesc* pMD = MemberLoader::FindMethodByName(th.GetMethodTable(), methodString.GetUTF8());
                                        if (pMD != nullptr && !pMD->IsGenericMethodDefinition())
                                        {
                                            newPtr = (INT_PTR)pMD;
                                        }
                                    }
                                }
                            }
                        }

                        if (newPtr == 0)
                        {
                            newPtr = HashToPgoUnknownHandle(HashStringA(string));
                        }

                        InterlockedCompareExchangeT(handleValueAddress, newPtr, initialHandleValue);
                    }
                }
            }
        }

        *pAllocatedData = new BYTE[schemaArray.GetCount() * sizeof(ICorJitInfo::PgoInstrumentationSchema)];
        memcpy(*pAllocatedData, schemaArray.OpenRawBuffer(), schemaArray.GetCount() * sizeof(ICorJitInfo::PgoInstrumentationSchema));
        schemaArray.CloseRawBuffer();
        *ppSchema = (ICorJitInfo::PgoInstrumentationSchema*)*pAllocatedData;

        *pCountSchemaItems = schemaArray.GetCount();
        *pInstrumentationData = found->GetData();
        *pPgoSource = ICorJitInfo::PgoSource::Text;

        hr = S_OK;
    }
    EX_CATCH
    {
        hr = E_FAIL;
    }
    EX_END_CATCH(RethrowTerminalExceptions)

    return hr;
}

class R2RInstrumentationDataReader
{
    ReadyToRunInfo *m_pReadyToRunInfo;
    Module* m_pModule;
    PEDecoder* m_pNativeImage;

public:
    StackSArray<ICorJitInfo::PgoInstrumentationSchema> schemaArray;
    StackSArray<BYTE> instrumentationData;

    R2RInstrumentationDataReader(ReadyToRunInfo *pReadyToRunInfo, Module* pModule, PEDecoder* pNativeImage) :
        m_pReadyToRunInfo(pReadyToRunInfo),
        m_pModule(pModule),
        m_pNativeImage(pNativeImage)
    {}

    bool operator()(const ICorJitInfo::PgoInstrumentationSchema &schema, int64_t dataItem, int32_t iDataItem)
    {
        if (iDataItem == 0)
        {
            schemaArray.Append(schema);
            schemaArray[schemaArray.GetCount() - 1].Offset = instrumentationData.GetCount();
        }

        ICorJitInfo::PgoInstrumentationKind kind = schema.InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask;
        switch (kind)
        {
            case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
            case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
            {
                intptr_t handleData = 0;
                if (dataItem != 0)
                {
                    uint32_t importSection = dataItem & 0xF;
                    int64_t typeIndex = dataItem >> 4;
                    if (importSection != 0xF)
                    {
                        COUNT_T countImportSections;
                        PTR_READYTORUN_IMPORT_SECTION pImportSections = m_pReadyToRunInfo->GetImportSections(&countImportSections);

                        if (importSection >= countImportSections)
                        {
                            _ASSERTE(!"Malformed PGO type or method handle data");
                            return false;
                        }

                        PTR_READYTORUN_IMPORT_SECTION pImportSection = &pImportSections[importSection];
                        COUNT_T cbData;
                        TADDR pData = m_pNativeImage->GetDirectoryData(&pImportSection->Section, &cbData);
                        uint32_t fixupIndex = (uint32_t)typeIndex;
                        PTR_SIZE_T fixupAddress = dac_cast<PTR_SIZE_T>(pData + fixupIndex * sizeof(TADDR));
                        if (!m_pModule->FixupNativeEntry(pImportSections + importSection, fixupIndex, fixupAddress))
                        {
                            return false;
                        }

                        handleData = *(intptr_t*)fixupAddress;
                    }
                    else
                    {
                        handleData = HashToPgoUnknownHandle((uint32_t)typeIndex);
                    }
                }

                BYTE* pHandleData = (BYTE*)&handleData;
                for (size_t i = 0; i < sizeof(intptr_t); i++)
                {
                    instrumentationData.Append(pHandleData[i]);
                }

                break;
            }
            case ICorJitInfo::PgoInstrumentationKind::FourByte:
            {
                BYTE* pFourByteData = (BYTE*)&dataItem;
                for (int i = 0; i < 4; i++)
                {
                    instrumentationData.Append(pFourByteData[i]);
                }

                break;
            }
            case ICorJitInfo::PgoInstrumentationKind::EightByte:
            {
                BYTE* pEightByteData = (BYTE*)&dataItem;
                for (int i = 0; i < 8; i++)
                {
                    instrumentationData.Append(pEightByteData[i]);
                }

                break;
            }
            case ICorJitInfo::PgoInstrumentationKind::None:
                break;
            default:
                assert(!"Unexpected PGO instrumentation data type");
                break;
        }

        return true;
    }
};

HRESULT PgoManager::getPgoInstrumentationResultsFromR2RFormat(ReadyToRunInfo *pReadyToRunInfo,
                                                              Module* pModule,
                                                              PEDecoder* pNativeImage,
                                                              BYTE* pR2RFormatData,
                                                              size_t pR2RFormatDataMaxSize,
                                                              BYTE** pAllocatedData,
                                                              ICorJitInfo::PgoInstrumentationSchema** ppSchema,
                                                              UINT32 *pCountSchemaItems,
                                                              BYTE**pInstrumentationData)
{
    *pAllocatedData = NULL;
    *ppSchema = NULL;
    *pInstrumentationData = NULL;
    *pCountSchemaItems = 0;

    R2RInstrumentationDataReader r2rReader(pReadyToRunInfo, pModule, pNativeImage);

    if (!ReadInstrumentationData(pR2RFormatData, pR2RFormatDataMaxSize, r2rReader))
    {
        return E_NOTIMPL;
    }
    else
    {
        while (r2rReader.instrumentationData.GetCount() & (sizeof(int64_t) - 1))
        {
            r2rReader.instrumentationData.Append(0);
        }
        size_t schemaDataSize = r2rReader.schemaArray.GetCount() * sizeof(ICorJitInfo::PgoInstrumentationSchema);
        NewArrayHolder<BYTE> allocatedData = new BYTE[r2rReader.instrumentationData.GetCount() + schemaDataSize];

        *pInstrumentationData = (BYTE*)allocatedData;
        *pCountSchemaItems = r2rReader.schemaArray.GetCount();
        memcpy(allocatedData, r2rReader.instrumentationData.OpenRawBuffer(), r2rReader.instrumentationData.GetCount());
        r2rReader.instrumentationData.CloseRawBuffer();
        *ppSchema = (ICorJitInfo::PgoInstrumentationSchema*)(((BYTE*)allocatedData) + r2rReader.instrumentationData.GetCount());
        memcpy(*ppSchema, r2rReader.schemaArray.OpenRawBuffer(), schemaDataSize);
        r2rReader.schemaArray.CloseRawBuffer();

        allocatedData.SuppressRelease();
        *pAllocatedData = allocatedData;
        return S_OK;
    }
}
#endif // DACCESS_COMPILE

HRESULT PgoManager::getPgoInstrumentationResultsInstance(MethodDesc* pMD, BYTE** pAllocatedData, ICorJitInfo::PgoInstrumentationSchema** ppSchema, UINT32 *pCountSchemaItems, BYTE**pInstrumentationData, ICorJitInfo::PgoSource* pPgoSource)
{
    // Initialize our out params
    *pAllocatedData = NULL;
    *pInstrumentationData = NULL;
    *pCountSchemaItems = 0;
    *pPgoSource = ICorJitInfo::PgoSource::Unknown;

    HeaderList *found;

    if (pMD->IsDynamicMethod())
    {
        found = m_pgoHeaders;
    }
    else
    {
        LoaderAllocatorPgoManager *laPgoManagerThis = (LoaderAllocatorPgoManager *)this;
        CrstHolder lock(&laPgoManagerThis->m_lock);
        found = laPgoManagerThis->m_pgoDataLookup.Lookup(pMD);
    }

    if (found == NULL)
    {
        // Prefer live collected data over data from pgo input, but if live data isn't present, use the data from the R2R file
        // Consider merging this data with the live data instead in the future
        if (pMD->GetModule()->IsReadyToRun() && pMD->GetModule()->GetReadyToRunInfo()->GetPgoInstrumentationData(pMD, pAllocatedData, ppSchema, pCountSchemaItems, pInstrumentationData))
        {
            *pPgoSource = ICorJitInfo::PgoSource::Static;
            return S_OK;
        }

        return E_NOTIMPL;
    }

    StackSArray<ICorJitInfo::PgoInstrumentationSchema> schemaArray;
    if (ReadInstrumentationSchemaWithLayoutIntoSArray(found->header.GetData(), found->header.countsOffset, 0, &schemaArray))
    {
        size_t schemaDataSize = AlignUp(schemaArray.GetCount() * sizeof(ICorJitInfo::PgoInstrumentationSchema), sizeof(size_t));
        size_t instrumentationDataSize = 0;
        if (schemaArray.GetCount() > 0)
        {
            auto lastSchema = schemaArray[schemaArray.GetCount() - 1];
            instrumentationDataSize = AlignUp(lastSchema.Offset + lastSchema.Count * InstrumentationKindToSize(lastSchema.InstrumentationKind), sizeof(size_t));
        }
        *pAllocatedData = new BYTE[schemaDataSize + instrumentationDataSize];
        *ppSchema = (ICorJitInfo::PgoInstrumentationSchema*)*pAllocatedData;
        *pCountSchemaItems = schemaArray.GetCount();
        memcpy(*pAllocatedData, schemaArray.OpenRawBuffer(), schemaDataSize);
        schemaArray.CloseRawBuffer();

        size_t* pInstrumentationDataDst = (size_t*)((*pAllocatedData) + schemaDataSize);
        size_t* pInstrumentationDataDstEnd = (size_t*)((*pAllocatedData) + schemaDataSize + instrumentationDataSize);
        *pInstrumentationData = (BYTE*)pInstrumentationDataDst;
        volatile size_t*pSrc = (volatile size_t*)(found->header.GetData() + found->header.countsOffset);
        // Use a volatile memcpy to copy the instrumentation data into a temporary buffer
        // This allows the instrumentation data to be made stable for reading during the execution of the jit
        // and since the copy moves through a volatile pointer, there will be no tearing of individual data elements
        for (;pInstrumentationDataDst < pInstrumentationDataDstEnd; pInstrumentationDataDst++, pSrc++)
        {
            *pInstrumentationDataDst = *pSrc;
        }
        *pPgoSource = ICorJitInfo::PgoSource::Dynamic;
        return S_OK;
    }
    else
    {
        _ASSERTE(!"Unable to parse schema data");
        return E_NOTIMPL;
    }
}

#else

// Stub version for !FEATURE_PGO builds
//
HRESULT PgoManager::allocPgoInstrumentationBySchema(MethodDesc* pMD, ICorJitInfo::PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, BYTE** pInstrumentationData)
{
    *pInstrumentationData = NULL;
    return E_NOTIMPL;
}

// Stub version for !FEATURE_PGO builds
//
HRESULT PgoManager::getPgoInstrumentationResults(MethodDesc* pMD, NewArrayHolder<BYTE> *pAllocatedData, ICorJitInfo::PgoInstrumentationSchema** ppSchema, UINT32 *pCountSchemaItems, BYTE**pInstrumentationData)
{
    *pAllocatedData = NULL;
    *pCountSchemaItems = 0;
    *pInstrumentationData = NULL;
    return E_NOTIMPL;
}

void PgoManager::VerifyAddress(void* address)
{
}

// Stub version for !FEATURE_PGO builds
//
void PgoManager::CreatePgoManager(PgoManager** ppMgr, bool loaderAllocator)
{
    *ppMgr = NULL;
}

#endif // FEATURE_PGO
