// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef PGO_H
#define PGO_H
#include "typehashingalgorithms.h"
#include "shash.h"


enum class PgoInstrumentationKind
{
    // Schema data types
    None = 0,
    FourByte = 1,
    EightByte = 2,
    TypeHandle = 3,

    // Mask of all schema data types
    MarshalMask = 0xF,

    // ExcessAlignment
    Align4Byte = 0x10,
    Align8Byte = 0x20,
    AlignPointer = 0x30,

    // Mask of all schema data types
    AlignMask = 0x30,

    DescriptorMin = 0x40,

    Done = None, // All instrumentation schemas must end with a record which is "Done"
    BasicBlockIntCount = DescriptorMin | FourByte, // 4 byte basic block counter, using unsigned 4 byte int
    TypeHandleHistogramCount = (DescriptorMin * 1) | FourByte, // 4 byte counter that is part of a type histogram
    TypeHandleHistogramTypeHandle = (DescriptorMin * 1) | TypeHandle, // TypeHandle that is part of a type histogram
    Version = (DescriptorMin * 2) | None, // Version is encoded in the Other field of the schema
};

inline PgoInstrumentationKind operator|(PgoInstrumentationKind a, PgoInstrumentationKind b)
{
    return static_cast<PgoInstrumentationKind>(static_cast<int>(a) | static_cast<int>(b));
}

inline PgoInstrumentationKind operator&(PgoInstrumentationKind a, PgoInstrumentationKind b)
{
    return static_cast<PgoInstrumentationKind>(static_cast<int>(a) & static_cast<int>(b));
}

inline PgoInstrumentationKind operator~(PgoInstrumentationKind a)
{
    return static_cast<PgoInstrumentationKind>(~static_cast<int>(a));
}

struct PgoInstrumentationSchema
{
    size_t Offset;
    PgoInstrumentationKind InstrumentationKind;
    int32_t ILOffset;
    int32_t Count;
    int32_t Other;
};

template<class IntHandler>
bool ReadCompressedInts(const uint8_t *pByte, size_t cbDataMax, IntHandler intProcessor)
{
    while (cbDataMax > 0)
    {
        int32_t signedInt;
        uint32_t bytesRead = CorSigUncompressSignedInt(pByte, &signedInt);
        if (cbDataMax < bytesRead)
            return false;
        cbDataMax -= bytesRead;
        if (!intProcessor(signedInt))
        {
            return false;
        }
    }

    return true;
}

enum class InstrumentationDataProcessingState
{
    Done = 0,
    ILOffset = 0x1,
    Type = 0x2,
    Count = 0x4,
    Other = 0x8,
    UpdateProcessMask = 0xF,
    UpdateProcessMaskFlag = 0x100,
};

inline InstrumentationDataProcessingState operator|(InstrumentationDataProcessingState a, InstrumentationDataProcessingState b)
{
    return static_cast<InstrumentationDataProcessingState>(static_cast<int>(a) | static_cast<int>(b));
}

inline InstrumentationDataProcessingState operator&(InstrumentationDataProcessingState a, InstrumentationDataProcessingState b)
{
    return static_cast<InstrumentationDataProcessingState>(static_cast<int>(a) & static_cast<int>(b));
}

inline InstrumentationDataProcessingState operator~(InstrumentationDataProcessingState a)
{
    return static_cast<InstrumentationDataProcessingState>(~static_cast<int>(a));
}

template<class SchemaHandler>
bool ReadInstrumentationData(const uint8_t *pByte, size_t cbDataMax, SchemaHandler handler)
{
    PgoInstrumentationSchema curSchema;
    InstrumentationDataProcessingState processingState;
    bool done = false;
    
    memset(&curSchema, 0, sizeof(curSchema));
    processingState = InstrumentationDataProcessingState::UpdateProcessMaskFlag;
    ReadCompressedInts(pByte, cbDataMax, [&curSchema, handler, &processingState, &done](int32_t curValue)
    {
        if (processingState == InstrumentationDataProcessingState::UpdateProcessMaskFlag)
        {
            processingState = (InstrumentationDataProcessingState)curValue;
            return true;
        }

        if ((processingState & InstrumentationDataProcessingState::ILOffset) == InstrumentationDataProcessingState::ILOffset)
        {
            curSchema.ILOffset += curValue;
            processingState = processingState & ~InstrumentationDataProcessingState::ILOffset;
        }
        else if ((processingState & InstrumentationDataProcessingState::Type) == InstrumentationDataProcessingState::Type)
        {
            curSchema.InstrumentationKind = static_cast<PgoInstrumentationKind>(static_cast<int>(curSchema.InstrumentationKind) + curValue);
            processingState = processingState & ~InstrumentationDataProcessingState::Type;
        }
        else if ((processingState & InstrumentationDataProcessingState::Count) == InstrumentationDataProcessingState::Count)
        {
            curSchema.Count += curValue;
            processingState = processingState & ~InstrumentationDataProcessingState::Count;
        }
        else if ((processingState & InstrumentationDataProcessingState::Other) == InstrumentationDataProcessingState::Other)
        {
            curSchema.Other += curValue;
            processingState = processingState & ~InstrumentationDataProcessingState::Other;
        }

        if (processingState == InstrumentationDataProcessingState::Done)
        {
            if (!handler(curSchema))
            {
                return false;
            }

            if (curSchema.InstrumentationKind == PgoInstrumentationKind::Done)
            {
                done = true;
                return false;
            }
        }
        return true;
    });

    return done;
}

inline bool CountInstrumentationDataSize(const uint8_t *pByte, size_t cbDataMax, int32_t *pInstrumentationSchemaCount)
{
    return ReadInstrumentationData(pByte, cbDataMax, [pInstrumentationSchemaCount](const PgoInstrumentationSchema& schema) { (*pInstrumentationSchemaCount)++; return true; });
}

inline size_t InstrumentationKindToSize(PgoInstrumentationKind kind)
{
    switch(kind & PgoInstrumentationKind::MarshalMask)
    {
        case PgoInstrumentationKind::None:
            return 0;
        case PgoInstrumentationKind::FourByte:
            return 4;
        case PgoInstrumentationKind::EightByte:
            return 8;
        case PgoInstrumentationKind::TypeHandle:
            return TARGET_POINTER_SIZE;
        default:
            _ASSERTE(FALSE);
            return 0;
    }
}

inline UINT InstrumentationKindToAlignment(PgoInstrumentationKind kind)
{
    switch(kind & PgoInstrumentationKind::AlignMask)
    {
        case PgoInstrumentationKind::Align4Byte:
            return 4;
        case PgoInstrumentationKind::Align8Byte:
            return 8;
        case PgoInstrumentationKind::AlignPointer:
            return TARGET_POINTER_SIZE;
        default:
            return (UINT)InstrumentationKindToSize(kind);
    }
}

inline void LayoutPgoInstrumentationSchema(const PgoInstrumentationSchema& prevSchema, PgoInstrumentationSchema* currentSchema)
{
    size_t instrumentationSize = InstrumentationKindToSize(currentSchema->InstrumentationKind);
    if (instrumentationSize != 0)
    {
        currentSchema->Offset = (UINT)AlignUp((size_t)prevSchema.Offset + (size_t)InstrumentationKindToSize(prevSchema.InstrumentationKind),
                                        InstrumentationKindToAlignment(currentSchema->InstrumentationKind));
    }
    else
    {
        currentSchema->Offset = prevSchema.Offset;
    }
}

template<class SchemaHandler>
bool ReadInstrumentationDataWithLayout(const uint8_t *pByte, size_t cbDataMax, size_t initialOffset, SchemaHandler handler)
{
    PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(PgoInstrumentationSchema));
    prevSchema.Offset = initialOffset;

    return ReadInstrumentationData(pByte, cbDataMax, [&prevSchema, handler](PgoInstrumentationSchema curSchema)
    {
        LayoutPgoInstrumentationSchema(prevSchema, curSchema);
        if (!handler(curSchema))
            return false;
        prevSchema = curSchema;
        return true;
    });
}

template<class ByteWriter>
bool WriteCompressedIntToBytes(int32_t value, ByteWriter& byteWriter)
{
    uint8_t bytes[4];
    uint32_t bytesToWrite = CorSigCompressSignedInt(value, bytes);
    uint8_t*pByte = bytes;
    
    while (bytesToWrite > 0)
    {
        if (!byteWriter(*pByte))
            return false;
        bytesToWrite--;
        pByte++;
    }

    return true;
}

template<class ByteWriter>
bool WriteIndividualSchemaToBytes(PgoInstrumentationSchema prevSchema, PgoInstrumentationSchema curSchema, ByteWriter& byteWriter)
{
    int32_t ilOffsetDiff = curSchema.ILOffset - prevSchema.ILOffset;
    int32_t OtherDiff = curSchema.Other - prevSchema.Other;
    int32_t CountDiff = curSchema.Count - prevSchema.Count;
    int32_t TypeDiff = curSchema.InstrumentationKind - prevSchema.InstrumentationKind;

    InstrumentationDataProcessingState modifyMask = (InstrumentationDataProcessingState)0;

    if (ilOffsetDiff != 0)
        modifyMask |= InstrumentationDataProcessingState::ILOffset;
    if (TypeDiff != 0)
        modifyMask |= InstrumentationDataProcessingState::Type;
    if (CountDiff != 0)
        modifyMask |= InstrumentationDataProcessingState::Count;
    if (OtherDiff != 0)
        modifyMask |= InstrumentationDataProcessingState::Other;

    _ASSERTE(modifyMask != 0);

    WriteCompressedIntToBytes(modifyMask, byteWriter);
    if ((ilOffsetDiff != 0) && !WriteCompressedIntToBytes(ilOffsetDiff, byteWriter))
        return false;
    if ((TypeDiff != 0) && !WriteCompressedIntToBytes(TypeDiff, byteWriter))
        return false;
    if ((CountDiff != 0) && !WriteCompressedIntToBytes(CountDiff, byteWriter))
        return false;
    if ((OtherDiff != 0) && !WriteCompressedIntToBytes(OtherDiff, byteWriter))
        return false;

    return true;
}

template<class ByteWriter>
bool WriteInstrumentationToBytes(PgoInstrumentationSchema* schemaTable, size_t cSchemas, ByteWriter& byteWriter)
{
    PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(PgoInstrumentationSchema));

    for (size_t iSchema = 0; iSchema < cSchemas; iSchema++)
    {
        if (!WriteIndividualSchemaToNibbles(prevSchema, schemaTable[i], byteWriter))
            return false;
        prevSchema = schemaTable[i];
    }

    return true;
}

// PgoManager handles in-process and out of band profile data for jitted code.
class PgoManager
{
#ifdef FEATURE_PGO

public:

    static void Initialize();
    static void Shutdown();

#endif // FEATURE_PGO

public:

    // Allocate a profile block count buffer for a method
    static HRESULT allocMethodBlockCounts(MethodDesc* pMD, UINT32 count,
        ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize);

    // Retrieve the profile block count buffer for a method
    static HRESULT getMethodBlockCounts(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
        ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns);

    static void CreatePgoManager(PgoManager* volatile* ppPgoManager, bool loaderAllocator);

    // Retrieve the most likely class for a particular call
    static CORINFO_CLASS_HANDLE getLikelyClass(MethodDesc* pMD, unsigned ilSize, unsigned ilOffset, UINT32* pLikelihood, UINT32* pNumberOfClasses);

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
        unsigned recordCount;

        CodeAndMethodHash GetKey() const
        {
            return CodeAndMethodHash(codehash, methodhash);
        }

        static COUNT_T Hash(CodeAndMethodHash hashpair)
        {
            return hashpair.Hash();
        }

        void Init(MethodDesc *pMD, unsigned codehash, unsigned ilSize, unsigned recordCount);
        void HashInit(unsigned methodhash, unsigned codehash, unsigned ilSize, unsigned recordCount)
        {
            method = NULL;
            this->codehash = codehash;
            this->methodhash = methodhash;
            this->ilSize = ilSize;
            this->recordCount = recordCount;
        }

        ICorJitInfo::BlockCounts* GetData() const
        {
            return (ICorJitInfo::BlockCounts*)(this + 1);
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

    // Allocate a profile block count buffer for a method
    HRESULT allocMethodBlockCountsInstance(MethodDesc* pMD, UINT32 count,
        ICorJitInfo::BlockCounts** pBlockCounts, unsigned ilSize);

    // Retreive the profile block count buffer for a method
    HRESULT getMethodBlockCountsInstance(MethodDesc* pMD, unsigned ilSize, UINT32* pCount,
        ICorJitInfo::BlockCounts** pBlockCounts, UINT32* pNumRuns);


private:

    static void ReadPgoData();
    static void WritePgoData();

private:

    // Formatting strings for file input/output
    static const char* const s_FileHeaderString;
    static const char* const s_FileTrailerString;
    static const char* const s_MethodHeaderString;
    static const char* const s_RecordString;
    static const char* const s_ClassProfileHeader;
    static const char* const s_ClassProfileEntry;

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
        m_lock(CrstLeafLock, CRST_DEFAULT)
    {}

    virtual ~LoaderAllocatorPgoManager(){}
};
#endif // FEATURE_PGO

#endif // PGO_H
