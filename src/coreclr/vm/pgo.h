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

#define DEFAULT_UNKNOWN_TYPEHANDLE 1
#define UNKNOWN_TYPEHANDLE_MIN 1
#define UNKNOWN_TYPEHANDLE_MAX 32

inline bool AddTypeHandleToUnknownTypeHandleMask(INT_PTR typeHandle, uint32_t *unknownTypeHandleMask)
{
    uint32_t bitMask = (uint32_t)(1 << (typeHandle - UNKNOWN_TYPEHANDLE_MIN));
    bool result = (bitMask & *unknownTypeHandleMask) == 0;
    *unknownTypeHandleMask |= bitMask;
    return result;
}

inline bool IsUnknownTypeHandle(INT_PTR typeHandle)
{
    return ((typeHandle >= UNKNOWN_TYPEHANDLE_MIN) && (typeHandle <= UNKNOWN_TYPEHANDLE_MAX));
}

inline INT_PTR HashToPgoUnknownTypeHandle(uint32_t hash)
{
    // Map from a 32bit hash to the 32 different unknown type handle values
    return (hash & 0x1F) + 1;
}

inline PgoInstrumentationKind operator|(PgoInstrumentationKind a, PgoInstrumentationKind b)
{
    return static_cast<PgoInstrumentationKind>(static_cast<int>(a) | static_cast<int>(b));
}

inline PgoInstrumentationKind operator&(PgoInstrumentationKind a, PgoInstrumentationKind b)
{
    return static_cast<PgoInstrumentationKind>(static_cast<int>(a) & static_cast<int>(b));
}
inline PgoInstrumentationKind operator-(PgoInstrumentationKind a, PgoInstrumentationKind b)
{
    return static_cast<PgoInstrumentationKind>(static_cast<int>(a) - static_cast<int>(b));
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
        // This logic is a variant on CorSigUncompressSignedInt which allows for the full range of an int32_t
        int32_t signedInt;
        if ((*pByte & 0x80) == 0x0) // 0??? ????
        {
            signedInt = *pByte >> 1;
            if (*pByte & 1)
                signedInt |= SIGN_MASK_ONEBYTE;

            pByte += 1;
            cbDataMax -=1;
        }
        else if ((*pByte & 0xC0) == 0x80) // 10?? ????
        {
            if (cbDataMax < 2)
                return false;
            
            int shiftedInt = ((*pByte & 0x3f) << 8) | *(pByte + 1);
            signedInt = shiftedInt >> 1;
            if (shiftedInt & 1)
                signedInt |= SIGN_MASK_TWOBYTE;

            pByte += 2;
            cbDataMax -= 2;
        }
        else
        {
            if (cbDataMax < 5)
                return false;

            signedInt = (int32_t)((*(pByte + 1) << 24 | *(pByte+2) << 16 | *(pByte+3) << 8 | *(pByte+4)));

            pByte += 5;
            cbDataMax -= 5;
        }
        
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
            processingState = InstrumentationDataProcessingState::UpdateProcessMaskFlag;
            if (curSchema.InstrumentationKind == PgoInstrumentationKind::Done)
            {
                done = true;
                return false;
            }

            if (!handler(curSchema))
            {
                return false;
            }

        }
        return true;
    });

    return done;
}

inline bool CountInstrumentationDataSize(const uint8_t *pByte, size_t cbDataMax, int32_t *pInstrumentationSchemaCount)
{
    *pInstrumentationSchemaCount = 0;
    return ReadInstrumentationData(pByte, cbDataMax, [pInstrumentationSchemaCount](const PgoInstrumentationSchema& schema) { (*pInstrumentationSchemaCount)++; return true; });
}

inline bool ComparePgoSchemaEquals(const uint8_t *pByte, size_t cbDataMax, const PgoInstrumentationSchema* schemaTable, size_t cSchemas)
{
    size_t iSchema = 0;
    return ReadInstrumentationData(pByte, cbDataMax, [schemaTable, cSchemas, &iSchema](const PgoInstrumentationSchema& schema) 
    {
        if (iSchema >= cSchemas)
            return false;
        
        if (schema.InstrumentationKind != schemaTable[iSchema].InstrumentationKind)
            return false;

        if (schema.ILOffset != schemaTable[iSchema].ILOffset)
            return false;

        if (schema.Count != schemaTable[iSchema].Count)
            return false;

        if (schema.Other != schemaTable[iSchema].Other)
            return false;

        return true;
    });
}

inline uint32_t InstrumentationKindToSize(PgoInstrumentationKind kind)
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
        currentSchema->Offset = (UINT)AlignUp((size_t)prevSchema.Offset + (size_t)InstrumentationKindToSize(prevSchema.InstrumentationKind) * prevSchema.Count,
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
        LayoutPgoInstrumentationSchema(prevSchema, &curSchema);
        if (!handler(curSchema))
            return false;
        prevSchema = curSchema;
        return true;
    });
}

inline bool ReadInstrumentationDataWithLayoutIntoSArray(const uint8_t *pByte, size_t cbDataMax, size_t initialOffset, SArray<PgoInstrumentationSchema>* pSchemas)
{
    return ReadInstrumentationDataWithLayout(pByte, cbDataMax, initialOffset, [pSchemas](const PgoInstrumentationSchema &schema)
    {
        pSchemas->Append(schema);
        return true;
    });
}


template<class ByteWriter>
bool WriteCompressedIntToBytes(int32_t value, ByteWriter& byteWriter)
{
    uint8_t isSigned = 0;

    // This function is modeled on CorSigCompressSignedInt, but differs in that
    // it handles arbitrary int32 values, not just a subset
    if (value < 0)
        isSigned = 1;

    if ((value & SIGN_MASK_ONEBYTE) == 0 || (value & SIGN_MASK_ONEBYTE) == SIGN_MASK_ONEBYTE)
    {
        return byteWriter((uint8_t)((value & ~SIGN_MASK_ONEBYTE) << 1 | isSigned));
    }
    else if ((value & SIGN_MASK_TWOBYTE) == 0 || (value & SIGN_MASK_TWOBYTE) == SIGN_MASK_TWOBYTE)
    {
        int32_t iData = (int32_t)((value & ~SIGN_MASK_TWOBYTE) << 1 | isSigned);
        _ASSERTE(iData <= 0x3fff);
        byteWriter(uint8_t((iData >> 8) | 0x80));
        return byteWriter(uint8_t(iData & 0xff));
    }
    else
    {
        // Unlike CorSigCompressSignedInt, this just writes a header bit
        // then a full 4 bytes, ignoring the whole signed bit detail
        byteWriter(0xC0);
        byteWriter(uint8_t((value >> 24) & 0xff));
        byteWriter(uint8_t((value >> 16) & 0xff));
        byteWriter(uint8_t((value >> 8) & 0xff));
        return byteWriter(uint8_t((value >> 0) & 0xff));
    }
}

template<class ByteWriter>
bool WriteIndividualSchemaToBytes(PgoInstrumentationSchema prevSchema, PgoInstrumentationSchema curSchema, ByteWriter& byteWriter)
{
    int32_t ilOffsetDiff = curSchema.ILOffset - prevSchema.ILOffset;
    int32_t OtherDiff = curSchema.Other - prevSchema.Other;
    int32_t CountDiff = curSchema.Count - prevSchema.Count;
    int32_t TypeDiff = (int32_t)curSchema.InstrumentationKind - (int32_t)prevSchema.InstrumentationKind;

    InstrumentationDataProcessingState modifyMask = (InstrumentationDataProcessingState)0;

    if (ilOffsetDiff != 0)
        modifyMask = modifyMask | InstrumentationDataProcessingState::ILOffset;
    if (TypeDiff != 0)
        modifyMask = modifyMask | InstrumentationDataProcessingState::Type;
    if (CountDiff != 0)
        modifyMask = modifyMask | InstrumentationDataProcessingState::Count;
    if (OtherDiff != 0)
        modifyMask = modifyMask | InstrumentationDataProcessingState::Other;

    _ASSERTE(modifyMask != InstrumentationDataProcessingState::Done);

    WriteCompressedIntToBytes((int32_t)modifyMask, byteWriter);
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
bool WriteInstrumentationToBytes(const PgoInstrumentationSchema* schemaTable, size_t cSchemas, const ByteWriter& byteWriter)
{
    PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(PgoInstrumentationSchema));

    for (size_t iSchema = 0; iSchema < cSchemas; iSchema++)
    {
        if (!WriteIndividualSchemaToBytes(prevSchema, schemaTable[iSchema], byteWriter))
            return false;
        prevSchema = schemaTable[iSchema];
    }

    // Terminate the schema list with an entry which is Done
    PgoInstrumentationSchema terminationSchema = prevSchema;
    terminationSchema.InstrumentationKind = PgoInstrumentationKind::Done;
    if (!WriteIndividualSchemaToBytes(prevSchema, terminationSchema, byteWriter))
        return false;

    return true;
}

inline bool WriteInstrumentationSchema(const PgoInstrumentationSchema* schemaTable, size_t cSchemas, uint8_t* array, size_t byteCount)
{
    return WriteInstrumentationToBytes(schemaTable, cSchemas, [&array, &byteCount](uint8_t data)
    {
        if (byteCount == 0)
            return false;
        *array = data;
        array += 1;
        byteCount--;
        return true;
    });
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

    static HRESULT getPgoInstrumentationResults(MethodDesc* pMD, SArray<PgoInstrumentationSchema>* pSchema, BYTE**pInstrumentationData);
    static HRESULT allocPgoInstrumentationBySchema(MethodDesc* pMD, PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, BYTE** pInstrumentationData);

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
                                                 SArray<PgoInstrumentationSchema>* pSchema,
                                                 BYTE**pInstrumentationData);

    HRESULT allocPgoInstrumentationBySchemaInstance(MethodDesc* pMD,
                                                    PgoInstrumentationSchema* pSchema,
                                                    UINT32 countSchemaItems,
                                                    BYTE** pInstrumentationData);

private:
    static HRESULT ComputeOffsetOfActualInstrumentationData(const PgoInstrumentationSchema* pSchema, UINT32 countSchemaItems, size_t headerInitialSize, UINT *offsetOfActualInstrumentationData);

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
        m_lock(CrstLeafLock, CRST_DEFAULT)
    {}

    virtual ~LoaderAllocatorPgoManager(){}
};
#endif // FEATURE_PGO

#endif // PGO_H
