// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PGO_H
#define PGO_H
#include "typehashingalgorithms.h"
#include "shash.h"

class ReadyToRunInfo;

// PgoManager handles in-process and out of band profile data for jitted code.
class PgoManager
{
#ifdef FEATURE_PGO

public:

    static void Initialize();
    static void Shutdown();

#endif // FEATURE_PGO

public:

    static HRESULT getPgoInstrumentationResults(MethodDesc* pMD, BYTE **pAllocatedDatapAllocatedData, ICorJitInfo::PgoInstrumentationSchema** ppSchema, UINT32 *pCountSchemaItems, BYTE**pInstrumentationData);
    static HRESULT allocPgoInstrumentationBySchema(MethodDesc* pMD, ICorJitInfo::PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, BYTE** pInstrumentationData);
    static HRESULT getPgoInstrumentationResultsFromR2RFormat(ReadyToRunInfo *pReadyToRunInfo,
                                                             Module* pModule,
                                                             PEDecoder* pNativeImage,
                                                             BYTE* pR2RFormatData,
                                                             size_t pR2RFormatDataMaxSize,
                                                             BYTE** pAllocatedData,
                                                             ICorJitInfo::PgoInstrumentationSchema** ppSchema,
                                                             UINT32 *pCountSchemaItems,
                                                             BYTE**pInstrumentationData);

    static void CreatePgoManager(PgoManager* volatile* ppPgoManager, bool loaderAllocator);

    // Verify address in bounds
    static void VerifyAddress(void* address);

#ifdef FEATURE_PGO
    PgoManager()
    {
        if (this != &s_InitialPgoManager)
        {
            _ASSERTE(s_pgoMgrLock.OwnedByCurrentThread());
            m_next = s_InitialPgoManager.m_next;
            m_prev = &s_InitialPgoManager;
            s_InitialPgoManager.m_next = this;
        }
        else
        {
            m_next = NULL;
            m_prev = NULL;
        }
    }

    virtual ~PgoManager()
    {
        if (this != &s_InitialPgoManager)
        {
            CrstHolder holder(&s_pgoMgrLock);
            m_prev->m_next = m_next;
            m_next->m_prev = m_prev;
        }
    }

    struct CodeAndMethodHash
    {
        CodeAndMethodHash(unsigned codehash, unsigned methodhash) :
            m_codehash(codehash),
            m_methodhash(methodhash)
        {}

        const unsigned m_codehash;
        const unsigned m_methodhash;

        COUNT_T Hash() const
        {
            return CombineTwoValuesIntoHash(m_codehash, m_methodhash);
        }

        bool operator==(CodeAndMethodHash other)
        {
            return m_codehash == other.m_codehash && m_methodhash == other.m_methodhash;
        }
    };

    struct Header
    {
        MethodDesc *method;
        unsigned codehash;
        unsigned methodhash;
        unsigned ilSize;
        unsigned countsOffset;

        CodeAndMethodHash GetKey() const
        {
            return CodeAndMethodHash(codehash, methodhash);
        }

        static COUNT_T Hash(CodeAndMethodHash hashpair)
        {
            return hashpair.Hash();
        }

        void Init(MethodDesc *pMD, unsigned codehash, unsigned ilSize, unsigned countsOffset);
        void HashInit(unsigned methodhash, unsigned codehash, unsigned ilSize, unsigned countsOffset)
        {
            method = NULL;
            this->codehash = codehash;
            this->methodhash = methodhash;
            this->ilSize = ilSize;
            this->countsOffset = countsOffset;
        }

        uint8_t* GetData() const
        {
            return (uint8_t*)(this + 1);
        }

        size_t SchemaSizeMax() const
        {
            return this->countsOffset;
        }
    };

    struct HeaderList
    {
        HeaderList*  next;
        Header       header;

        MethodDesc*  GetKey() const
        {
            return header.method;
        }
        static COUNT_T Hash(MethodDesc *ptr)
        {
            return MixPointerIntoHash(ptr);
        }
    };

protected:

    HRESULT getPgoInstrumentationResultsInstance(MethodDesc* pMD,
                                                 BYTE** pAllocatedData,
                                                 ICorJitInfo::PgoInstrumentationSchema** ppSchema,
                                                 UINT32 *pCountSchemaItems,
                                                 BYTE**pInstrumentationData);

    HRESULT allocPgoInstrumentationBySchemaInstance(MethodDesc* pMD,
                                                    ICorJitInfo::PgoInstrumentationSchema* pSchema,
                                                    UINT32 countSchemaItems,
                                                    BYTE** pInstrumentationData);

private:
    static HRESULT ComputeOffsetOfActualInstrumentationData(const ICorJitInfo::PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, size_t headerInitialSize, UINT *offsetOfActualInstrumentationData);

    static void ReadPgoData();
    static void WritePgoData();

private:

    // Formatting strings for file input/output
    static const char* const s_FileHeaderString;
    static const char* const s_FileTrailerString;
    static const char* const s_MethodHeaderString;
    static const char* const s_RecordString;
    static const char* const s_None;
    static const char* const s_FourByte;
    static const char* const s_EightByte;
    static const char* const s_TypeHandle;

    static CrstStatic s_pgoMgrLock;
    static PgoManager s_InitialPgoManager;

    static PtrSHash<Header, CodeAndMethodHash> s_textFormatPgoData;

    PgoManager *m_next = NULL;
    PgoManager *m_prev = NULL;
    HeaderList *m_pgoHeaders = NULL;

    template<class lambda>
    static bool EnumeratePGOHeaders(lambda l)
    {
        CrstHolder lock(&s_pgoMgrLock);
        PgoManager *mgrCurrent = s_InitialPgoManager.m_next;
        while (mgrCurrent != NULL)
        {
            HeaderList *pgoHeaderCur = mgrCurrent->m_pgoHeaders;
            while (pgoHeaderCur != NULL)
            {
                if (!l(pgoHeaderCur))
                {
                    return false;
                }
                pgoHeaderCur = pgoHeaderCur->next;
            }
            mgrCurrent = mgrCurrent->m_next;
        }

        return true;
    }

#endif // FEATURE_PGO
};

#ifdef FEATURE_PGO
class LoaderAllocatorPgoManager : public PgoManager
{
    friend class PgoManager;
    Crst m_lock;
    PtrSHash<PgoManager::HeaderList, MethodDesc*> m_pgoDataLookup;

    public:
    LoaderAllocatorPgoManager() :
        m_lock(CrstPgoData, CRST_DEFAULT)
    {}

    virtual ~LoaderAllocatorPgoManager(){}
};
#endif // FEATURE_PGO

#endif // PGO_H
