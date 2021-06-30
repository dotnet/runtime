// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// ZapReadyToRun.cpp
//

//
// Zapping of ready-to-run specific structures
//
// ======================================================================================

#include "common.h"

#include "zapreadytorun.h"

#include "zapimport.h"

#include "nativeformatwriter.h"

#include "nibblestream.h"

#include "../vm/typehashingalgorithms.h"
#include "../vm/nativeformatreader.h"

using namespace NativeFormat;

void ZapReadyToRunHeader::Save(ZapWriter * pZapWriter)
{
    ZapImage * pImage = ZapImage::GetImage(pZapWriter);

    READYTORUN_HEADER readyToRunHeader;

    ZeroMemory(&readyToRunHeader, sizeof(readyToRunHeader));

    readyToRunHeader.Signature = READYTORUN_SIGNATURE;
    readyToRunHeader.MajorVersion = READYTORUN_MAJOR_VERSION;
    readyToRunHeader.MinorVersion = READYTORUN_MINOR_VERSION;

    if (pImage->m_ModuleDecoder.IsPlatformNeutral())
        readyToRunHeader.CoreHeader.Flags |= READYTORUN_FLAG_PLATFORM_NEUTRAL_SOURCE;

    // If all types loaded succesfully, set a flag to skip type loading sanity checks at runtime
    if (pImage->GetCompileInfo()->AreAllClassesFullyLoaded(pImage->GetModuleHandle()))
        readyToRunHeader.CoreHeader.Flags |= READYTORUN_FLAG_SKIP_TYPE_VALIDATION;

    if (pImage->GetZapperOptions()->m_fPartialNGen)
        readyToRunHeader.CoreHeader.Flags |= READYTORUN_FLAG_PARTIAL;

    readyToRunHeader.CoreHeader.NumberOfSections = m_Sections.GetCount();

    pZapWriter->Write(&readyToRunHeader, sizeof(readyToRunHeader));

    qsort(&m_Sections[0], m_Sections.GetCount(), sizeof(Section), SectionCmp);

    for(COUNT_T i = 0; i < m_Sections.GetCount(); i++)
    {
        READYTORUN_SECTION section;
        section.Type = m_Sections[i].type;
        ZapWriter::SetDirectoryData(&section.Section, m_Sections[i].pSection);
        pZapWriter->Write(&section, sizeof(section));
    }
}

class BlobVertex : public NativeFormat::Vertex
{
    int m_cbSize;

public:
    BlobVertex(int cbSize)
        : m_cbSize(cbSize)
    {
    }

    void * GetData()
    {
        return this + 1;
    }

    int GetSize()
    {
        return m_cbSize;
    }

    virtual void Save(NativeWriter * pWriter)
    {
        byte * pData = (byte *)GetData();
        for (int i = 0; i < m_cbSize; i++)
            pWriter->WriteByte(pData[i]);
    }
};

class BlobVertexKey
{
    PVOID   _pData;
    int     _cbSize;

public:
    BlobVertexKey(PVOID pData, int cbSize)
        : _pData(pData), _cbSize(cbSize)
    {
    }

    void * GetData()
    {
        return _pData;
    }

    int GetSize()
    {
        return _cbSize;
    }
};

class BlobVertexSHashTraits : public DefaultSHashTraits<BlobVertex *>
{
public:
    typedef BlobVertexKey key_t;

    static key_t GetKey(element_t e)
    {
        LIMITED_METHOD_CONTRACT;
        return key_t(e->GetData(), e->GetSize());
    }
    static BOOL Equals(key_t k1, key_t k2)
    {
        LIMITED_METHOD_CONTRACT;
        if (k1.GetSize() != k2.GetSize())
            return FALSE;
        return memcmp(k1.GetData(), k2.GetData(), k1.GetSize()) == 0;
    }
    static count_t Hash(key_t k)
    {
        LIMITED_METHOD_CONTRACT;
        count_t hash = 5381 + (count_t)(k.GetSize() << 7);

        PBYTE pbData = (PBYTE)k.GetData();
        PBYTE pbDataEnd = pbData + k.GetSize();

        for (/**/ ; pbData < pbDataEnd; pbData++)
        {
            hash = ((hash << 5) + hash) ^ *pbData;
        }
        return hash;
    }
};


class EntryPointVertex : public NativeFormat::Vertex
{
    DWORD m_methodIndex;
    BlobVertex * m_pFixups;

public:
    EntryPointVertex(DWORD methodIndex, BlobVertex * pFixups)
        : m_methodIndex(methodIndex), m_pFixups(pFixups)
    {
    }

    virtual void Save(NativeWriter * pWriter)
    {
        if (m_pFixups != NULL)
        {
            int existingOffset = pWriter->GetCurrentOffset(m_pFixups);
            if (existingOffset != -1)
            {
                pWriter->WriteUnsigned((m_methodIndex << 2) | 3);
                pWriter->WriteUnsigned(pWriter->GetCurrentOffset() - existingOffset);
            }
            else
            {
                pWriter->WriteUnsigned((m_methodIndex << 2) | 1);
                pWriter->SetCurrentOffset(m_pFixups);
                m_pFixups->Save(pWriter);
            }
        }
        else
        {
            pWriter->WriteUnsigned(m_methodIndex << 1);
        }
    }
};

class EntryPointWithBlobVertex : public EntryPointVertex
{
    BlobVertex * m_pBlob;

public:
    EntryPointWithBlobVertex(DWORD methodIndex, BlobVertex * pFixups, BlobVertex * pBlob)
        : EntryPointVertex(methodIndex, pFixups), m_pBlob(pBlob)
    {
    }

    virtual void Save(NativeWriter * pWriter)
    {
        m_pBlob->Save(pWriter);
        EntryPointVertex::Save(pWriter);
    }
};

void ZapImage::OutputEntrypointsTableForReadyToRun()
{
    BeginRegion(CORINFO_REGION_COLD);

    NativeWriter arrayWriter;
    NativeWriter hashtableWriter;

    NativeSection * pArraySection = arrayWriter.NewSection();
    NativeSection * pHashtableSection = hashtableWriter.NewSection();

    VertexArray vertexArray(pArraySection);
    pArraySection->Place(&vertexArray);
    VertexHashtable vertexHashtable;
    pHashtableSection->Place(&vertexHashtable);

    bool fEmpty = true;

    SHash< NoRemoveSHashTraits < BlobVertexSHashTraits > > fixupBlobs;

    COUNT_T nCount = m_MethodCompilationOrder.GetCount();
    for (COUNT_T i = 0; i < nCount; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];


        BlobVertex * pFixupBlob = NULL;

        if (pMethod->m_pFixupList != NULL)
        {
            NibbleWriter writer;
            m_pImportTable->PlaceFixups(pMethod->m_pFixupList, writer);

            DWORD cbBlob;
            PVOID pBlob = writer.GetBlob(&cbBlob);

            pFixupBlob = fixupBlobs.Lookup(BlobVertexKey(pBlob, cbBlob));
            if (pFixupBlob == NULL)
            {
                void * pMemory = new (GetHeap()) BYTE[sizeof(BlobVertex) + cbBlob];
                pFixupBlob = new (pMemory) BlobVertex(cbBlob);
                memcpy(pFixupBlob->GetData(), pBlob, cbBlob);

                fixupBlobs.Add(pFixupBlob);
            }
        }

        CORINFO_SIG_INFO sig;
        GetJitInfo()->getMethodSig(pMethod->GetHandle(), &sig);

        mdMethodDef token = GetJitInfo()->getMethodDefFromMethod(pMethod->GetHandle());
        int rid = RidFromToken(token);

        if (sig.sigInst.classInstCount > 0 || sig.sigInst.methInstCount > 0)
        {
            _ASSERTE(rid != 0);

            CORINFO_MODULE_HANDLE module = GetJitInfo()->getClassModule(pMethod->GetClassHandle());
            _ASSERTE(GetCompileInfo()->IsInCurrentVersionBubble(module));
            SigBuilder sigBuilder;
            CORINFO_RESOLVED_TOKEN resolvedToken = {};
            resolvedToken.tokenScope = module;
            resolvedToken.token = token;
            resolvedToken.hClass = pMethod->GetClassHandle();
            resolvedToken.hMethod = pMethod->GetHandle();
            GetCompileInfo()->EncodeMethod(module, pMethod->GetHandle(), &sigBuilder, m_pImportTable, EncodeModuleHelper, &resolvedToken);

            DWORD cbBlob;
            PVOID pBlob = sigBuilder.GetSignature(&cbBlob);
            void * pMemory = new (GetHeap()) BYTE[sizeof(BlobVertex) + cbBlob];
            BlobVertex * pSigBlob = new (pMemory) BlobVertex(cbBlob);
            memcpy(pSigBlob->GetData(), pBlob, cbBlob);

            int dwHash = GetCompileInfo()->GetVersionResilientMethodHashCode(pMethod->GetHandle());
            vertexHashtable.Append(dwHash, pHashtableSection->Place(new (GetHeap()) EntryPointWithBlobVertex(pMethod->GetMethodIndex(), pFixupBlob, pSigBlob)));
        }
        else
        {
            int rid = RidFromToken(token);
            if (rid != 0)
            {
                vertexArray.Set(rid - 1, new (GetHeap()) EntryPointVertex(pMethod->GetMethodIndex(), pFixupBlob));
            }
            else
            {
                // This is a p/invoke stub, get the list of methods associated with the stub, and put this code in that set of rids
                void *targetMethodEnum;
                BOOL isStubWithTargetMethods = GetCompileInfo()->EnumMethodsForStub(pMethod->GetHandle(), &targetMethodEnum);
                _ASSERTE(isStubWithTargetMethods);

                CORINFO_METHOD_HANDLE hTargetMethod;
                while (GetCompileInfo()->EnumNextMethodForStub(targetMethodEnum, &hTargetMethod))
                {
                    mdMethodDef token = GetJitInfo()->getMethodDefFromMethod(hTargetMethod);
                    int rid = RidFromToken(token);
                    _ASSERTE(rid != 0);
                    vertexArray.Set(rid - 1, new (GetHeap()) EntryPointVertex(pMethod->GetMethodIndex(), pFixupBlob));
                }

                GetCompileInfo()->EnumCloseForStubEnumerator(targetMethodEnum);
            }
        }

        fEmpty = false;
    }

    if (fEmpty)
        return;

    vertexArray.ExpandLayout();

    vector<byte>& arrayBlob = arrayWriter.Save();
    ZapNode * pArrayBlob = ZapBlob::NewBlob(this, &arrayBlob[0], arrayBlob.size());
    m_pCodeMethodDescsSection->Place(pArrayBlob);

    vector<byte>& hashtableBlob = hashtableWriter.Save();
    ZapNode * pHashtableBlob = ZapBlob::NewBlob(this, &hashtableBlob[0], hashtableBlob.size());
    m_pCodeMethodDescsSection->Place(pHashtableBlob);

    ZapReadyToRunHeader * pReadyToRunHeader = GetReadyToRunHeader();
    pReadyToRunHeader->RegisterSection(ReadyToRunSectionType::MethodDefEntryPoints, pArrayBlob);
    pReadyToRunHeader->RegisterSection(ReadyToRunSectionType::InstanceMethodEntryPoints, pHashtableBlob);
    pReadyToRunHeader->RegisterSection(ReadyToRunSectionType::RuntimeFunctions, m_pRuntimeFunctionSection);

    if (m_pLazyMethodCallHelperSection->GetNodeCount() != 0)
        pReadyToRunHeader->RegisterSection(ReadyToRunSectionType::DelayLoadMethodCallThunks, m_pLazyMethodCallHelperSection);

    if (m_pExceptionInfoLookupTable->GetSize() != 0)
        pReadyToRunHeader->RegisterSection(ReadyToRunSectionType::ExceptionInfo, m_pExceptionInfoLookupTable);

    EndRegion(CORINFO_REGION_COLD);
}

class DebugInfoVertex : public NativeFormat::Vertex
{
    BlobVertex * m_pDebugInfo;

public:
    DebugInfoVertex(BlobVertex * pDebugInfo)
        : m_pDebugInfo(pDebugInfo)
    {
    }

    virtual void Save(NativeWriter * pWriter)
    {
        int existingOffset = pWriter->GetCurrentOffset(m_pDebugInfo);
        if (existingOffset != -1)
        {
            _ASSERTE(pWriter->GetCurrentOffset() > existingOffset);
            pWriter->WriteUnsigned(pWriter->GetCurrentOffset() - existingOffset);
        }
        else
        {
            pWriter->WriteUnsigned(0);
            pWriter->SetCurrentOffset(m_pDebugInfo);
            m_pDebugInfo->Save(pWriter);
        }
    }
};
// At ngen time Zapper::CompileModule PlaceFixups called from
//     code:ZapSig.GetSignatureForTypeHandle
//
/*static*/ DWORD ZapImage::EncodeModuleHelper(LPVOID compileContext,
    CORINFO_MODULE_HANDLE referencedModule)
{
    _ASSERTE(!IsReadyToRunCompilation() || IsLargeVersionBubbleEnabled());
    ZapImportTable * pTable = (ZapImportTable *)compileContext;
    return pTable->GetIndexOfModule(referencedModule);
}

void ZapImage::OutputDebugInfoForReadyToRun()
{
    NativeWriter writer;

    NativeSection * pSection = writer.NewSection();

    VertexArray vertexArray(pSection);
    pSection->Place(&vertexArray);

    bool fEmpty = true;

    SHash< NoRemoveSHashTraits < BlobVertexSHashTraits > > debugInfoBlobs;

    COUNT_T nCount = m_MethodCompilationOrder.GetCount();
    for (COUNT_T i = 0; i < nCount; i++)
    {
        ZapMethodHeader * pMethod = m_MethodCompilationOrder[i];

        ZapBlob * pDebugInfo = pMethod->GetDebugInfo();
        if (pDebugInfo == NULL)
            continue;

        DWORD cbBlob = pDebugInfo->GetBlobSize();
        PVOID pBlob = pDebugInfo->GetData();

        BlobVertex * pDebugInfoBlob = debugInfoBlobs.Lookup(BlobVertexKey(pBlob, cbBlob));
        if (pDebugInfoBlob == NULL)
        {
            void * pMemory = new (GetHeap()) BYTE[sizeof(BlobVertex) + cbBlob];
            pDebugInfoBlob = new (pMemory) BlobVertex(cbBlob);
            memcpy(pDebugInfoBlob->GetData(), pBlob, cbBlob);

            debugInfoBlobs.Add(pDebugInfoBlob);
        }

        vertexArray.Set(pMethod->GetMethodIndex(), new (GetHeap()) DebugInfoVertex(pDebugInfoBlob));

        fEmpty = false;
    }

    if (fEmpty)
        return;

    vertexArray.ExpandLayout();

    vector<byte>& blob = writer.Save();

    ZapNode * pBlob = ZapBlob::NewBlob(this, &blob[0], blob.size());
    m_pDebugSection->Place(pBlob);

    GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::DebugInfo, pBlob);
}

void ZapImage::OutputInliningTableForReadyToRun()
{
    SBuffer serializedInlineTrackingBuffer;
    m_pPreloader->GetSerializedInlineTrackingMap(&serializedInlineTrackingBuffer);
    ZapNode * pBlob = ZapBlob::NewAlignedBlob(this, (PVOID)(const BYTE*) serializedInlineTrackingBuffer, serializedInlineTrackingBuffer.GetSize(), 4);
    m_pDebugSection->Place(pBlob);
    GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::InliningInfo, pBlob);
}

void ZapImage::OutputProfileDataForReadyToRun()
{
    if (m_pInstrumentSection != nullptr)
    {
        GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::ProfileDataInfo, m_pInstrumentSection);
    }
}

void ZapImage::OutputManifestMetadataForReadyToRun()
{
    if (m_pMetaDataSection != nullptr)
    {
        GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::ManifestMetadata, m_pMetaDataSection);
    }
}

void ZapImage::OutputTypesTableForReadyToRun(IMDInternalImport * pMDImport)
{
    NativeWriter writer;
    VertexHashtable typesHashtable;

    NativeSection * pSection = writer.NewSection();
    pSection->Place(&typesHashtable);

    // Note on duplicate types with same name: there is not need to perform that check when building
    // the hashtable. If such types were encountered, the R2R compilation would fail before reaching here.

    // Save the TypeDefs to the hashtable
    {
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumAllInit(mdtTypeDef);

        mdToken mdTypeToken;
        while (pMDImport->EnumNext(&hEnum, &mdTypeToken))
        {
            mdTypeDef mdCurrentToken = mdTypeToken;
            DWORD dwHash = GetCompileInfo()->GetVersionResilientTypeHashCode(GetModuleHandle(), mdTypeToken);

            typesHashtable.Append(dwHash, pSection->Place(new UnsignedConstant(RidFromToken(mdTypeToken) << 1)));
        }
    }

    // Save the ExportedTypes to the hashtable
    {
        HENUMInternalHolder hEnum(pMDImport);
        hEnum.EnumInit(mdtExportedType, mdTokenNil);

        mdToken mdTypeToken;
        while (pMDImport->EnumNext(&hEnum, &mdTypeToken))
        {
            DWORD dwHash = GetCompileInfo()->GetVersionResilientTypeHashCode(GetModuleHandle(), mdTypeToken);

            typesHashtable.Append(dwHash, pSection->Place(new UnsignedConstant((RidFromToken(mdTypeToken) << 1) | 1)));
        }
    }

    vector<byte>& blob = writer.Save();

    ZapNode * pBlob = ZapBlob::NewBlob(this, &blob[0], blob.size());
    _ASSERTE(m_pAvailableTypesSection);
    m_pAvailableTypesSection->Place(pBlob);

    GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::AvailableTypes, pBlob);
}

template<class Tlambda>
HRESULT EnumerateAllCustomAttributes(IMDInternalImport *pMDImport, Tlambda lambda)
{
    HENUMInternalHolder hEnum(pMDImport);
    hEnum.EnumAllInit(mdtCustomAttribute);

    HRESULT hr = S_OK;

    mdCustomAttribute tkCustomAttribute;
    while (pMDImport->EnumNext(&hEnum, &tkCustomAttribute))
    {
        LPCUTF8 szNamespace;
        LPCUTF8 szName;

        hr = pMDImport->GetNameOfCustomAttribute(tkCustomAttribute, &szNamespace, &szName);
        if (FAILED(hr))
            return hr;

        if (szNamespace == NULL)
            continue;

        if (szName == NULL)
            continue;

        // System.Runtime.CompilerServices.NullableAttribute is NEVER added to the table (There are *many* of these, and they provide no useful value to the runtime)
        if ((strcmp(szNamespace, "System.Runtime.CompilerServices") == 0) && (strcmp(szName, "NullableAttribute") == 0))
            continue;

        bool addToTable = false;
        // Other than Nullable attribute, all attributes under System.Runtime are added to the table
        if (strncmp(szNamespace, "System.Runtime.", strlen("System.Runtime.")) == 0)
        {
            addToTable = true;
        }
        else if (strcmp(szNamespace, "Windows.Foundation.Metadata") == 0)
        {
            // Windows.Foundation.Metadata attributes are a similar construct to compilerservices attributes. Add them to the table
            addToTable = true;
        }
        else if (strcmp(szNamespace, "System") == 0)
        {
            // Some historical well known attributes were placed in the System namespace. Special case them
            if (strcmp(szName, "ParamArrayAttribute") == 0)
                addToTable = true;
            else if (strcmp(szName, "ThreadStaticAttribute") == 0)
                addToTable = true;
        }
        else if (strcmp(szNamespace, "System.Reflection") == 0)
        {
            // Historical attribute in the System.Reflection namespace
            if (strcmp(szName, "DefaultMemberAttribute") == 0)
                addToTable = true;
        }

        if (!addToTable)
            continue;

        mdToken tkParent;
        hr = pMDImport->GetParentToken(tkCustomAttribute, &tkParent);
        if (FAILED(hr))
            return hr;

        hr = lambda(szNamespace, szName, tkParent);
        if (FAILED(hr))
            return hr;
    }

    return hr;
}

static inline uint32_t RotateLeft(const uint32_t x, int k) {
	return (x << k) | (x >> (32 - k));
}

uint32_t Xoshiro128StarStar(uint32_t s[]) {
    const uint32_t result = RotateLeft(s[1] * 5, 7) * 9;
    const uint32_t t = s[1] << 9;

    s[2] ^= s[0];
    s[3] ^= s[1];
    s[1] ^= s[2];
    s[0] ^= s[3];

    s[2] ^= t;
    s[3] = RotateLeft(s[3], 11);

    return result;
}

HRESULT ZapImage::ComputeAttributePresenceTable(IMDInternalImport * pMDImport, SArray<UINT16> *table)
{
    int countOfEntries = 0;
    HRESULT hr = EnumerateAllCustomAttributes(pMDImport, [&countOfEntries](LPCUTF8 szNamespace, LPCUTF8 szName, mdToken tkParent)
        {
            countOfEntries++;
            return S_OK;
        });
    if (FAILED(hr))
        return hr;

    if (countOfEntries == 0)
    {
        table->Clear();
        _ASSERTE(table->IsEmpty());
        return S_OK;
    }

    // Buckets have 8 entries
    UINT minTableBucketCount = (countOfEntries / 8) + 1;
    UINT bucketCount = 1;

    // Bucket count must be power of two
    while (bucketCount < minTableBucketCount)
        bucketCount *= 2;

    // Resize the array.
    bool tryAgainWithBiggerTable = false;
    int countOfRetries = 0;
    do
    {
        tryAgainWithBiggerTable = false;
        UINT actualSizeOfTable = bucketCount * 8; // Buckets have 8 entries in them
        UINT16* pTable = table->OpenRawBuffer(actualSizeOfTable);
        memset(pTable, 0, sizeof(UINT16) * actualSizeOfTable);
        table->CloseRawBuffer();

        uint32_t state[4] = {729055690, 833774698, 218408041, 493449127}; // 4 randomly generated numbers to initialize random number state

        // Attempt to fill  table

        hr = EnumerateAllCustomAttributes(pMDImport, [&](LPCUTF8 szNamespace, LPCUTF8 szName, mdToken tkParent)
        {
            StackSString name(SString::Utf8);
            name.AppendUTF8(szNamespace);
            name.AppendUTF8(NAMESPACE_SEPARATOR_STR);
            name.AppendUTF8(szName);

            StackScratchBuffer buff;
            const char* pDebugNameUTF8 = name.GetUTF8(buff);
            size_t len = strlen(pDebugNameUTF8);

            // This hashing algorithm MUST match exactly the logic in NativeCuckooFilter
            DWORD hashOfAttribute = ComputeNameHashCode(pDebugNameUTF8);
            UINT32 hash = CombineTwoValuesIntoHash(hashOfAttribute, tkParent);
            UINT16 fingerprint = (UINT16)(hash >> 16);
            if (fingerprint == 0)
                fingerprint = 1;

            UINT bucketAIndex = hash % bucketCount;
            UINT bucketBIndex = (bucketAIndex ^ (NativeFormat::NativeCuckooFilter::ComputeFingerprintHash(fingerprint)  % bucketCount));

            _ASSERTE(bucketAIndex == (bucketBIndex ^ (NativeFormat::NativeCuckooFilter::ComputeFingerprintHash(fingerprint) % bucketCount)));

            if (Xoshiro128StarStar(state) & 1) // Randomly choose which bucket to attempt to fill first
            {
                UINT temp = bucketAIndex;
                bucketAIndex = bucketBIndex;
                bucketBIndex = temp;
            }

            auto hasEntryInBucket = [&table](UINT bucketIndex, UINT16 fprint)
            {
                for (int i = 0; i < 8; i++)
                {
                    if ((*table)[(bucketIndex * 8) + i] == fprint)
                        return true;
                }
                return false;
            };

            auto isEmptyEntryInBucket = [&table](UINT bucketIndex)
            {
                for (int i = 0; i < 8; i++)
                {
                    if ((*table)[(bucketIndex * 8) + i] == 0)
                        return true;
                }
                return false;
            };

            auto fillEmptyEntryInBucket = [&table](UINT bucketIndex, UINT16 fprint)
            {
                for (int i = 0; i < 8; i++)
                {
                    if ((*table)[(bucketIndex * 8) + i] == 0)
                    {
                        (*table)[(bucketIndex * 8) + i] = fprint;
                        return;
                    }
                }
                _ASSERTE(!"Not possible to reach here");
                return;
            };

            // Scan for pre-existing fingerprint entry in buckets
            if (hasEntryInBucket(bucketAIndex, fingerprint) || hasEntryInBucket(bucketBIndex, fingerprint))
                return S_OK;

            // Determine if there is space in a bucket to add a new entry
            if (isEmptyEntryInBucket(bucketAIndex))
            {
                fillEmptyEntryInBucket(bucketAIndex, fingerprint);
                return S_OK;
            }
            if (isEmptyEntryInBucket(bucketBIndex))
            {
                fillEmptyEntryInBucket(bucketBIndex, fingerprint);
                return S_OK;
            }

            int MaxNumKicks = 256;
            // Note, that bucketAIndex itself was chosen randomly above.
            for (int n = 0; n < MaxNumKicks; n++)
            {
                // Randomly swap an entry in bucket bucketAIndex with fingerprint
                UINT entryIndexInBucket = Xoshiro128StarStar(state) & 0x7;
                UINT16 temp = fingerprint;
                fingerprint = (*table)[(bucketAIndex * 8) + entryIndexInBucket];
                (*table)[(bucketAIndex * 8) + entryIndexInBucket] = temp;

                // Find other bucket
                bucketAIndex = bucketAIndex ^ (NativeFormat::NativeCuckooFilter::ComputeFingerprintHash(fingerprint) % bucketCount);
                if (isEmptyEntryInBucket(bucketAIndex))
                {
                    fillEmptyEntryInBucket(bucketAIndex, fingerprint);
                    return S_OK;
                }
            }

            tryAgainWithBiggerTable = true;
            return E_FAIL;
        });

        if (tryAgainWithBiggerTable)
        {
            // bucket entry kicking path requires bucket counts to be power of two in size due to use of xor to retrieve second hash
            bucketCount *= 2;
        }
    } while(tryAgainWithBiggerTable && ((countOfRetries++) < 2));

    if (tryAgainWithBiggerTable)
    {
        return E_FAIL;
    }

    return S_OK;
}

void ZapImage::OutputAttributePresenceFilter(IMDInternalImport * pMDImport)
{
    // Core library attributes are checked FAR more often than other dlls
    // attributes, so produce a highly efficient table for determining if they are
    // present. Other assemblies *MAY* benefit from this feature, but it doesn't show
    // as useful at this time.

    if (m_hModule != m_zapper->m_pEECompileInfo->GetLoaderModuleForCoreLib())
        return;

    SArray<UINT16> table;
    if (SUCCEEDED(ComputeAttributePresenceTable(pMDImport, &table)))
    {
        UINT16* pRawTable = table.OpenRawBuffer(table.GetCount());
        ZapNode * pBlob = ZapBlob::NewBlob(this, pRawTable, table.GetCount() * sizeof(UINT16));
        table.CloseRawBuffer();

        _ASSERTE(m_pAttributePresenceSection);
        m_pAttributePresenceSection->Place(pBlob);
        GetReadyToRunHeader()->RegisterSection(ReadyToRunSectionType::AttributePresence, pBlob);
    }
}

//
// Verify that data structures and flags shared between NGen and ReadyToRun are in sync
//

//
// READYTORUN_IMPORT_SECTION
//
static_assert_no_msg(sizeof(READYTORUN_IMPORT_SECTION)          == sizeof(CORCOMPILE_IMPORT_SECTION));

static_assert_no_msg((int)READYTORUN_IMPORT_SECTION_TYPE_UNKNOWN     == (int)CORCOMPILE_IMPORT_TYPE_UNKNOWN);

static_assert_no_msg((int)READYTORUN_IMPORT_SECTION_FLAGS_EAGER      == (int)CORCOMPILE_IMPORT_FLAGS_EAGER);

//
// READYTORUN_METHOD_SIG
//
static_assert_no_msg((int)READYTORUN_METHOD_SIG_UnboxingStub         == (int)ENCODE_METHOD_SIG_UnboxingStub);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_InstantiatingStub    == (int)ENCODE_METHOD_SIG_InstantiatingStub);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_MethodInstantiation  == (int)ENCODE_METHOD_SIG_MethodInstantiation);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_SlotInsteadOfToken   == (int)ENCODE_METHOD_SIG_SlotInsteadOfToken);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_MemberRefToken       == (int)ENCODE_METHOD_SIG_MemberRefToken);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_Constrained          == (int)ENCODE_METHOD_SIG_Constrained);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_OwnerType            == (int)ENCODE_METHOD_SIG_OwnerType);
static_assert_no_msg((int)READYTORUN_METHOD_SIG_UpdateContext        == (int)ENCODE_METHOD_SIG_UpdateContext);

//
// READYTORUN_FIELD_SIG
//
static_assert_no_msg((int)READYTORUN_FIELD_SIG_IndexInsteadOfToken   == (int)ENCODE_FIELD_SIG_IndexInsteadOfToken);
static_assert_no_msg((int)READYTORUN_FIELD_SIG_MemberRefToken        == (int)ENCODE_FIELD_SIG_MemberRefToken);
static_assert_no_msg((int)READYTORUN_FIELD_SIG_OwnerType             == (int)ENCODE_FIELD_SIG_OwnerType);

//
// READYTORUN_FIXUP
//
static_assert_no_msg((int)READYTORUN_FIXUP_ThisObjDictionaryLookup   == (int)ENCODE_DICTIONARY_LOOKUP_THISOBJ);
static_assert_no_msg((int)READYTORUN_FIXUP_TypeDictionaryLookup      == (int)ENCODE_DICTIONARY_LOOKUP_TYPE);
static_assert_no_msg((int)READYTORUN_FIXUP_MethodDictionaryLookup    == (int)ENCODE_DICTIONARY_LOOKUP_METHOD);

static_assert_no_msg((int)READYTORUN_FIXUP_TypeHandle                == (int)ENCODE_TYPE_HANDLE);
static_assert_no_msg((int)READYTORUN_FIXUP_MethodHandle              == (int)ENCODE_METHOD_HANDLE);
static_assert_no_msg((int)READYTORUN_FIXUP_FieldHandle               == (int)ENCODE_FIELD_HANDLE);

static_assert_no_msg((int)READYTORUN_FIXUP_MethodEntry               == (int)ENCODE_METHOD_ENTRY);
static_assert_no_msg((int)READYTORUN_FIXUP_MethodEntry_DefToken      == (int)ENCODE_METHOD_ENTRY_DEF_TOKEN);
static_assert_no_msg((int)READYTORUN_FIXUP_MethodEntry_RefToken      == (int)ENCODE_METHOD_ENTRY_REF_TOKEN);

static_assert_no_msg((int)READYTORUN_FIXUP_VirtualEntry              == (int)ENCODE_VIRTUAL_ENTRY);
static_assert_no_msg((int)READYTORUN_FIXUP_VirtualEntry_DefToken     == (int)ENCODE_VIRTUAL_ENTRY_DEF_TOKEN);
static_assert_no_msg((int)READYTORUN_FIXUP_VirtualEntry_RefToken     == (int)ENCODE_VIRTUAL_ENTRY_REF_TOKEN);
static_assert_no_msg((int)READYTORUN_FIXUP_VirtualEntry_Slot         == (int)ENCODE_VIRTUAL_ENTRY_SLOT);

static_assert_no_msg((int)READYTORUN_FIXUP_Helper                    == (int)ENCODE_READYTORUN_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_StringHandle              == (int)ENCODE_STRING_HANDLE);

static_assert_no_msg((int)READYTORUN_FIXUP_NewObject                 == (int)ENCODE_NEW_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_NewArray                  == (int)ENCODE_NEW_ARRAY_HELPER);

static_assert_no_msg((int)READYTORUN_FIXUP_IsInstanceOf              == (int)ENCODE_ISINSTANCEOF_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_ChkCast                   == (int)ENCODE_CHKCAST_HELPER);

static_assert_no_msg((int)READYTORUN_FIXUP_FieldAddress              == (int)ENCODE_FIELD_ADDRESS);
static_assert_no_msg((int)READYTORUN_FIXUP_CctorTrigger              == (int)ENCODE_CCTOR_TRIGGER);

static_assert_no_msg((int)READYTORUN_FIXUP_StaticBaseNonGC           == (int)ENCODE_STATIC_BASE_NONGC_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_StaticBaseGC              == (int)ENCODE_STATIC_BASE_GC_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_ThreadStaticBaseNonGC     == (int)ENCODE_THREAD_STATIC_BASE_NONGC_HELPER);
static_assert_no_msg((int)READYTORUN_FIXUP_ThreadStaticBaseGC        == (int)ENCODE_THREAD_STATIC_BASE_GC_HELPER);

static_assert_no_msg((int)READYTORUN_FIXUP_FieldBaseOffset           == (int)ENCODE_FIELD_BASE_OFFSET);
static_assert_no_msg((int)READYTORUN_FIXUP_FieldOffset               == (int)ENCODE_FIELD_OFFSET);

static_assert_no_msg((int)READYTORUN_FIXUP_TypeDictionary            == (int)ENCODE_TYPE_DICTIONARY);
static_assert_no_msg((int)READYTORUN_FIXUP_MethodDictionary          == (int)ENCODE_METHOD_DICTIONARY);

static_assert_no_msg((int)READYTORUN_FIXUP_Check_TypeLayout          == (int)ENCODE_CHECK_TYPE_LAYOUT);
static_assert_no_msg((int)READYTORUN_FIXUP_Check_FieldOffset         == (int)ENCODE_CHECK_FIELD_OFFSET);

static_assert_no_msg((int)READYTORUN_FIXUP_DelegateCtor              == (int)ENCODE_DELEGATE_CTOR);

static_assert_no_msg((int)READYTORUN_FIXUP_DeclaringTypeHandle       == (int)ENCODE_DECLARINGTYPE_HANDLE);

static_assert_no_msg((int)READYTORUN_FIXUP_IndirectPInvokeTarget     == (int)ENCODE_INDIRECT_PINVOKE_TARGET);
static_assert_no_msg((int)READYTORUN_FIXUP_PInvokeTarget             == (int)ENCODE_PINVOKE_TARGET);

static_assert_no_msg((int)READYTORUN_FIXUP_Check_InstructionSetSupport== (int)ENCODE_CHECK_INSTRUCTION_SET_SUPPORT);

static_assert_no_msg((int)READYTORUN_FIXUP_Verify_FieldOffset         == (int)ENCODE_VERIFY_FIELD_OFFSET);
static_assert_no_msg((int)READYTORUN_FIXUP_Verify_TypeLayout          == (int)ENCODE_VERIFY_TYPE_LAYOUT);

static_assert_no_msg((int)READYTORUN_FIXUP_Check_VirtualFunctionOverride  == (int)ENCODE_CHECK_VIRTUAL_FUNCTION_OVERRIDE);
static_assert_no_msg((int)READYTORUN_FIXUP_Verify_VirtualFunctionOverride == (int)ENCODE_VERIFY_VIRTUAL_FUNCTION_OVERRIDE);

//
// READYTORUN_EXCEPTION
//
static_assert_no_msg(sizeof(READYTORUN_EXCEPTION_LOOKUP_TABLE_ENTRY) == sizeof(CORCOMPILE_EXCEPTION_LOOKUP_TABLE_ENTRY));
static_assert_no_msg(sizeof(READYTORUN_EXCEPTION_CLAUSE) == sizeof(CORCOMPILE_EXCEPTION_CLAUSE));

//
// ReadyToRunHFAElemType
//
static_assert_no_msg((int)READYTORUN_HFA_ELEMTYPE_None      == (int)CORINFO_HFA_ELEM_NONE);
static_assert_no_msg((int)READYTORUN_HFA_ELEMTYPE_Float32   == (int)CORINFO_HFA_ELEM_FLOAT);
static_assert_no_msg((int)READYTORUN_HFA_ELEMTYPE_Float64   == (int)CORINFO_HFA_ELEM_DOUBLE);
static_assert_no_msg((int)READYTORUN_HFA_ELEMTYPE_Vector64  == (int)CORINFO_HFA_ELEM_VECTOR64);
static_assert_no_msg((int)READYTORUN_HFA_ELEMTYPE_Vector128 == (int)CORINFO_HFA_ELEM_VECTOR128);

