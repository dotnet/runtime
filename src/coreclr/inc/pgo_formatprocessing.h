// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header defines the algorithms for generating and parsing pgo compressed schema formats

#ifndef PGO_FORMATPROCESSING_H
#define PGO_FORMATPROCESSING_H

#ifdef FEATURE_PGO

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

inline ICorJitInfo::PgoInstrumentationKind operator|(ICorJitInfo::PgoInstrumentationKind a, ICorJitInfo::PgoInstrumentationKind b)
{
    return static_cast<ICorJitInfo::PgoInstrumentationKind>(static_cast<int>(a) | static_cast<int>(b));
}

inline ICorJitInfo::PgoInstrumentationKind operator&(ICorJitInfo::PgoInstrumentationKind a, ICorJitInfo::PgoInstrumentationKind b)
{
    return static_cast<ICorJitInfo::PgoInstrumentationKind>(static_cast<int>(a) & static_cast<int>(b));
}

inline ICorJitInfo::PgoInstrumentationKind operator-(ICorJitInfo::PgoInstrumentationKind a, ICorJitInfo::PgoInstrumentationKind b)
{
    return static_cast<ICorJitInfo::PgoInstrumentationKind>(static_cast<int>(a) - static_cast<int>(b));
}

inline ICorJitInfo::PgoInstrumentationKind operator~(ICorJitInfo::PgoInstrumentationKind a)
{
    return static_cast<ICorJitInfo::PgoInstrumentationKind>(~static_cast<int>(a));
}

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
    ICorJitInfo::PgoInstrumentationSchema curSchema;
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
            curSchema.InstrumentationKind = static_cast<ICorJitInfo::PgoInstrumentationKind>(static_cast<int>(curSchema.InstrumentationKind) + curValue);
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
            if (curSchema.InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::Done)
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
    return ReadInstrumentationData(pByte, cbDataMax, [pInstrumentationSchemaCount](const ICorJitInfo::PgoInstrumentationSchema& schema) { (*pInstrumentationSchemaCount)++; return true; });
}

inline bool ComparePgoSchemaEquals(const uint8_t *pByte, size_t cbDataMax, const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas)
{
    size_t iSchema = 0;
    return ReadInstrumentationData(pByte, cbDataMax, [schemaTable, cSchemas, &iSchema](const ICorJitInfo::PgoInstrumentationSchema& schema) 
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

inline uint32_t InstrumentationKindToSize(ICorJitInfo::PgoInstrumentationKind kind)
{
    switch(kind & ICorJitInfo::PgoInstrumentationKind::MarshalMask)
    {
        case ICorJitInfo::PgoInstrumentationKind::None:
            return 0;
        case ICorJitInfo::PgoInstrumentationKind::FourByte:
            return 4;
        case ICorJitInfo::PgoInstrumentationKind::EightByte:
            return 8;
        case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
            return TARGET_POINTER_SIZE;
        default:
            _ASSERTE(FALSE);
            return 0;
    }
}

inline UINT InstrumentationKindToAlignment(ICorJitInfo::PgoInstrumentationKind kind)
{
    switch(kind & ICorJitInfo::PgoInstrumentationKind::AlignMask)
    {
        case ICorJitInfo::PgoInstrumentationKind::Align4Byte:
            return 4;
        case ICorJitInfo::PgoInstrumentationKind::Align8Byte:
            return 8;
        case ICorJitInfo::PgoInstrumentationKind::AlignPointer:
            return TARGET_POINTER_SIZE;
        default:
            return (UINT)InstrumentationKindToSize(kind);
    }
}

inline void LayoutPgoInstrumentationSchema(const ICorJitInfo::PgoInstrumentationSchema& prevSchema, ICorJitInfo::PgoInstrumentationSchema* currentSchema)
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
    ICorJitInfo::PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(ICorJitInfo::PgoInstrumentationSchema));
    prevSchema.Offset = initialOffset;

    return ReadInstrumentationData(pByte, cbDataMax, [&prevSchema, handler](ICorJitInfo::PgoInstrumentationSchema curSchema)
    {
        LayoutPgoInstrumentationSchema(prevSchema, &curSchema);
        if (!handler(curSchema))
            return false;
        prevSchema = curSchema;
        return true;
    });
}

inline bool ReadInstrumentationDataWithLayoutIntoSArray(const uint8_t *pByte, size_t cbDataMax, size_t initialOffset, SArray<ICorJitInfo::PgoInstrumentationSchema>* pSchemas)
{
    return ReadInstrumentationDataWithLayout(pByte, cbDataMax, initialOffset, [pSchemas](const ICorJitInfo::PgoInstrumentationSchema &schema)
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
bool WriteIndividualSchemaToBytes(ICorJitInfo::PgoInstrumentationSchema prevSchema, ICorJitInfo::PgoInstrumentationSchema curSchema, ByteWriter& byteWriter)
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
bool WriteInstrumentationToBytes(const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas, const ByteWriter& byteWriter)
{
    ICorJitInfo::PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(ICorJitInfo::PgoInstrumentationSchema));

    for (size_t iSchema = 0; iSchema < cSchemas; iSchema++)
    {
        if (!WriteIndividualSchemaToBytes(prevSchema, schemaTable[iSchema], byteWriter))
            return false;
        prevSchema = schemaTable[iSchema];
    }

    // Terminate the schema list with an entry which is Done
    ICorJitInfo::PgoInstrumentationSchema terminationSchema = prevSchema;
    terminationSchema.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::Done;
    if (!WriteIndividualSchemaToBytes(prevSchema, terminationSchema, byteWriter))
        return false;

    return true;
}

inline bool WriteInstrumentationSchema(const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas, uint8_t* array, size_t byteCount)
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

#endif // FEATURE_PGO
#endif // PGO_FORMATPROCESSING_H
