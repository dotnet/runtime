// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This header defines the algorithms for generating and parsing pgo compressed schema formats

#ifndef PGO_FORMATPROCESSING_H
#define PGO_FORMATPROCESSING_H

#ifdef FEATURE_PGO

inline INT_PTR HashToPgoUnknownHandle(uint32_t hash)
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
        case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
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

#define SIGN_MASK_ONEBYTE_64BIT  0xffffffffffffffc0LL
#define SIGN_MASK_TWOBYTE_64BIT  0xffffffffffffe000LL
#define SIGN_MASK_FOURBYTE_64BIT 0xffffffff80000000LL

template<class IntHandler>
bool ReadCompressedInts(const uint8_t *pByte, size_t cbDataMax, IntHandler intProcessor)
{
    while (cbDataMax > 0)
    {
        // This logic is a variant on CorSigUncompressSignedInt which allows for the full range of an int64_t
        int64_t signedInt;
        if ((*pByte & 0x80) == 0x0) // 0??? ????
        {
            signedInt = *pByte >> 1;
            if (*pByte & 1)
                signedInt |= SIGN_MASK_ONEBYTE_64BIT;

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
                signedInt |= SIGN_MASK_TWOBYTE_64BIT;

            pByte += 2;
            cbDataMax -= 2;
        }
        else if ((*pByte) == 0xC1)
        {
            if (cbDataMax < 9)
                return false;
            signedInt = (int64_t)((((int64_t)*(pByte + 1)) << 56 | ((int64_t)*(pByte+2)) << 48 | ((int64_t)*(pByte+3)) << 40 | ((int64_t)*(pByte+4)) << 32) | ((int64_t)*(pByte + 5)) << 24 | ((int64_t)*(pByte+6)) << 16 | ((int64_t)*(pByte+7)) << 8 | ((int64_t)*(pByte+8)));
            pByte += 9;
            cbDataMax -= 9;
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

class ProcessSchemaUpdateFunctor
{
    ICorJitInfo::PgoInstrumentationSchema curSchema = {};
    InstrumentationDataProcessingState processingState = InstrumentationDataProcessingState::UpdateProcessMaskFlag;

public:
    const ICorJitInfo::PgoInstrumentationSchema& GetSchema() const { return curSchema; }
    bool ProcessInteger(int32_t curValue)
    {
        if (processingState == InstrumentationDataProcessingState::UpdateProcessMaskFlag)
        {
            processingState = (InstrumentationDataProcessingState)curValue;
            assert((processingState & ~InstrumentationDataProcessingState::UpdateProcessMask) == InstrumentationDataProcessingState::Done);
            return false;
        }

        if ((processingState & InstrumentationDataProcessingState::ILOffset) == InstrumentationDataProcessingState::ILOffset)
        {
            curSchema.ILOffset = (int32_t)((int64_t)curSchema.ILOffset + curValue);
            processingState = processingState & ~InstrumentationDataProcessingState::ILOffset;
        }
        else if ((processingState & InstrumentationDataProcessingState::Type) == InstrumentationDataProcessingState::Type)
        {
            curSchema.InstrumentationKind = static_cast<ICorJitInfo::PgoInstrumentationKind>(static_cast<int64_t>(curSchema.InstrumentationKind) + curValue);
            processingState = processingState & ~InstrumentationDataProcessingState::Type;
        }
        else if ((processingState & InstrumentationDataProcessingState::Count) == InstrumentationDataProcessingState::Count)
        {
            curSchema.Count = (int32_t)((int64_t)curSchema.Count + curValue);
            processingState = processingState & ~InstrumentationDataProcessingState::Count;
        }
        else if ((processingState & InstrumentationDataProcessingState::Other) == InstrumentationDataProcessingState::Other)
        {
            curSchema.Other = (int32_t)((int64_t)curSchema.Other + curValue);
            processingState = processingState & ~InstrumentationDataProcessingState::Other;
        }

        if (processingState == InstrumentationDataProcessingState::Done)
        {
            processingState = InstrumentationDataProcessingState::UpdateProcessMaskFlag;
            return true;
        }
        return false;
    }
};

template<class SchemaHandler>
bool ReadInstrumentationSchema(const uint8_t *pByte, size_t cbDataMax, SchemaHandler handler)
{
    ProcessSchemaUpdateFunctor schemaHandlerUpdate;
    bool done = false;

    ReadCompressedInts(pByte, cbDataMax, [&handler, &schemaHandlerUpdate, &done](int64_t curValue)
    {
        if (schemaHandlerUpdate.ProcessInteger((int32_t)curValue))
        {
            if (schemaHandlerUpdate.GetSchema().InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::Done)
            {
                done = true;
                return false;
            }

            if (!handler(schemaHandlerUpdate.GetSchema()))
            {
                return false;
            }
        }
        return true;
    });

    return done;
}

template<class SchemaAndDataHandler>
bool ReadInstrumentationData(const uint8_t *pByte, size_t cbDataMax, SchemaAndDataHandler& handler)
{
    ProcessSchemaUpdateFunctor schemaHandler;
    bool done = false;
    int64_t lastDataValue = 0;
    int64_t lastTypeDataValue = 0;
    int64_t lastMethodDataValue = 0;
    int32_t dataCountToRead = 0;

    ReadCompressedInts(pByte, cbDataMax, [&](int64_t curValue)
    {
        if (dataCountToRead > 0)
        {
            switch(schemaHandler.GetSchema().InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask)
            {
            case ICorJitInfo::PgoInstrumentationKind::FourByte:
            case ICorJitInfo::PgoInstrumentationKind::EightByte:
                lastDataValue += curValue;

                if (!handler(schemaHandler.GetSchema(), lastDataValue, schemaHandler.GetSchema().Count - dataCountToRead))
                {
                    return false;
                }
                break;
            case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
                lastTypeDataValue += curValue;

                if (!handler(schemaHandler.GetSchema(), lastTypeDataValue, schemaHandler.GetSchema().Count - dataCountToRead))
                {
                    return false;
                }
                break;
            case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
                lastMethodDataValue += curValue;

                if (!handler(schemaHandler.GetSchema(), lastMethodDataValue, schemaHandler.GetSchema().Count - dataCountToRead))
                {
                    return false;
                }
                break;
            default:
                assert(!"Unexpected PGO instrumentation data type");
                break;
            }
            dataCountToRead--;
            return true;
        }
        if (schemaHandler.ProcessInteger((int32_t)curValue))
        {
            if (schemaHandler.GetSchema().InstrumentationKind == ICorJitInfo::PgoInstrumentationKind::Done)
            {
                done = true;
                return false;
            }

            if (InstrumentationKindToSize(schemaHandler.GetSchema().InstrumentationKind) == 0)
            {
                if (!handler(schemaHandler.GetSchema(), 0, 0))
                {
                    return false;
                }
            }
            else
            {
                dataCountToRead = schemaHandler.GetSchema().Count;
            }
        }
        return true;
    });

    return done;
}

inline bool CountInstrumentationDataSize(const uint8_t *pByte, size_t cbDataMax, int32_t *pInstrumentationSchemaCount)
{
    *pInstrumentationSchemaCount = 0;
    return ReadInstrumentationSchema(pByte, cbDataMax, [pInstrumentationSchemaCount](const ICorJitInfo::PgoInstrumentationSchema& schema) { (*pInstrumentationSchemaCount)++; return true; });
}

inline bool ComparePgoSchemaEquals(const uint8_t *pByte, size_t cbDataMax, const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas)
{
    size_t iSchema = 0;
    return ReadInstrumentationSchema(pByte, cbDataMax, [schemaTable, cSchemas, &iSchema](const ICorJitInfo::PgoInstrumentationSchema& schema)
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
bool ReadInstrumentationSchemaWithLayout(const uint8_t *pByte, size_t cbDataMax, size_t initialOffset, SchemaHandler& handler)
{
    ICorJitInfo::PgoInstrumentationSchema prevSchema;
    memset(&prevSchema, 0, sizeof(ICorJitInfo::PgoInstrumentationSchema));
    prevSchema.Offset = initialOffset;

    return ReadInstrumentationSchema(pByte, cbDataMax, [&prevSchema, &handler](ICorJitInfo::PgoInstrumentationSchema curSchema)
    {
        LayoutPgoInstrumentationSchema(prevSchema, &curSchema);
        if (!handler(curSchema))
            return false;
        prevSchema = curSchema;
        return true;
    });
}


// Return true if schemaTable entries are a subset of the schema described by pByte, with matching entries in the same order.
// Also updates offset of the matching entries in schemaTable to those of the pByte schema.
//
inline bool CheckIfPgoSchemaIsCompatibleAndSetOffsets(const uint8_t *pByte, size_t cbDataMax, ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas)
{
    size_t nMatched = 0;
    size_t initialOffset = cbDataMax;

    auto handler = [schemaTable, cSchemas, &nMatched](const ICorJitInfo::PgoInstrumentationSchema& schema)
    {
        if ((nMatched < cSchemas)
            && (schema.InstrumentationKind == schemaTable[nMatched].InstrumentationKind)
            && (schema.ILOffset == schemaTable[nMatched].ILOffset)
            && (schema.Count == schemaTable[nMatched].Count)
            && (schema.Other == schemaTable[nMatched].Other))
        {
            schemaTable[nMatched].Offset = schema.Offset;
            nMatched++;
        }

        return true;
    };

    ReadInstrumentationSchemaWithLayout(pByte, cbDataMax, initialOffset, handler);

    return (nMatched == cSchemas);
}

inline bool ReadInstrumentationSchemaWithLayoutIntoSArray(const uint8_t *pByte, size_t cbDataMax, size_t initialOffset, SArray<ICorJitInfo::PgoInstrumentationSchema>* pSchemas)
{
    auto lambda = [pSchemas](const ICorJitInfo::PgoInstrumentationSchema &schema)
    {
        pSchemas->Append(schema);
        return true;
    };

    return ReadInstrumentationSchemaWithLayout(pByte, cbDataMax, initialOffset, lambda);
}

#define SIGN_MASK_ONEBYTE_64BIT  0xffffffffffffffc0LL
#define SIGN_MASK_TWOBYTE_64BIT  0xffffffffffffe000LL
#define SIGN_MASK_FOURBYTE_64BIT 0xffffffff80000000LL

template<class ByteWriter>
bool WriteCompressedIntToBytes(int64_t value, ByteWriter& byteWriter)
{
    uint8_t isSigned = 0;

    // This function is modeled on CorSigCompressSignedInt, but differs in that
    // it handles arbitrary int64 values, not just a subset
    if (value < 0)
        isSigned = 1;

    if ((value & SIGN_MASK_ONEBYTE_64BIT) == 0 || (value & SIGN_MASK_ONEBYTE_64BIT) == SIGN_MASK_ONEBYTE_64BIT)
    {
        return byteWriter((uint8_t)((value & ~SIGN_MASK_ONEBYTE) << 1 | isSigned));
    }
    else if ((value & SIGN_MASK_TWOBYTE_64BIT) == 0 || (value & SIGN_MASK_TWOBYTE_64BIT) == SIGN_MASK_TWOBYTE_64BIT)
    {
        int32_t iData = (int32_t)((value & ~SIGN_MASK_TWOBYTE_64BIT) << 1 | isSigned);
        _ASSERTE(iData <= 0x3fff);
        byteWriter(uint8_t((iData >> 8) | 0x80));
        return byteWriter(uint8_t(iData & 0xff));
    }
    else if ((value & SIGN_MASK_FOURBYTE_64BIT) == 0 || (value & SIGN_MASK_FOURBYTE_64BIT) == SIGN_MASK_FOURBYTE_64BIT)
    {
        // Unlike CorSigCompressSignedInt, this just writes a header byte
        // then 4 bytes, ignoring the whole signed bit detail
        byteWriter(0xC0);
        byteWriter(uint8_t((value >> 24) & 0xff));
        byteWriter(uint8_t((value >> 16) & 0xff));
        byteWriter(uint8_t((value >> 8) & 0xff));
        return byteWriter(uint8_t((value >> 0) & 0xff));
    }
    else
    {
        // Unlike CorSigCompressSignedInt, this just writes a header byte
        // then 8 bytes, ignoring the whole signed bit detail
        byteWriter(0xC1);
        byteWriter(uint8_t((value >> 56) & 0xff));
        byteWriter(uint8_t((value >> 48) & 0xff));
        byteWriter(uint8_t((value >> 40) & 0xff));
        byteWriter(uint8_t((value >> 32) & 0xff));
        byteWriter(uint8_t((value >> 24) & 0xff));
        byteWriter(uint8_t((value >> 16) & 0xff));
        byteWriter(uint8_t((value >> 8) & 0xff));
        return byteWriter(uint8_t((value >> 0) & 0xff));
    }
}

template<class ByteWriter>
bool WriteIndividualSchemaToBytes(ICorJitInfo::PgoInstrumentationSchema prevSchema, ICorJitInfo::PgoInstrumentationSchema curSchema, ByteWriter& byteWriter)
{
    int64_t ilOffsetDiff = (int64_t)curSchema.ILOffset - (int64_t)prevSchema.ILOffset;
    int64_t OtherDiff = (int64_t)curSchema.Other - (int64_t)prevSchema.Other;
    int64_t CountDiff = (int64_t)curSchema.Count - (int64_t)prevSchema.Count;
    int64_t TypeDiff = (int64_t)curSchema.InstrumentationKind - (int64_t)prevSchema.InstrumentationKind;

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
bool WriteInstrumentationSchemaToBytes(const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas, const ByteWriter& byteWriter)
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

template<class ByteWriter>
class SchemaAndDataWriter
{
    const ByteWriter& byteWriter;
    uint8_t* pInstrumentationData;
    ICorJitInfo::PgoInstrumentationSchema prevSchema = {};
    int64_t lastIntDataWritten = 0;
    int64_t lastTypeDataWritten = 0;
    int64_t lastMethodDataWritten = 0;

public:
    SchemaAndDataWriter(const ByteWriter& byteWriter, uint8_t* pInstrumentationData) :
        byteWriter(byteWriter),
        pInstrumentationData(pInstrumentationData)
    {}

    bool AppendSchema(ICorJitInfo::PgoInstrumentationSchema schema)
    {
        if (!WriteIndividualSchemaToBytes(prevSchema, schema, byteWriter))
            return false;

        prevSchema = schema;
        return true;
    }

    template<class TypeHandleProcessor, class MethodHandleProcessor>
    bool AppendDataFromLastSchema(TypeHandleProcessor& thProcessor, MethodHandleProcessor& mhProcessor)
    {
        uint8_t *pData = (pInstrumentationData + prevSchema.Offset);
        for (int32_t iDataElem = 0; iDataElem < prevSchema.Count; iDataElem++)
        {
            int64_t logicalDataToWrite;
            switch(prevSchema.InstrumentationKind & ICorJitInfo::PgoInstrumentationKind::MarshalMask)
            {
                case ICorJitInfo::PgoInstrumentationKind::None:
                    return true;
                case ICorJitInfo::PgoInstrumentationKind::FourByte:
                {
                    logicalDataToWrite = *(volatile int32_t*)pData;
                    bool returnValue = WriteCompressedIntToBytes(logicalDataToWrite - lastIntDataWritten, byteWriter);
                    lastIntDataWritten = logicalDataToWrite;
                    if (!returnValue)
                        return false;
                    pData += 4;
                    break;
                }
                case ICorJitInfo::PgoInstrumentationKind::EightByte:
                {
                    logicalDataToWrite = *(volatile int64_t*)pData;
                    bool returnValue = WriteCompressedIntToBytes(logicalDataToWrite - lastIntDataWritten, byteWriter);
                    lastIntDataWritten = logicalDataToWrite;
                    if (!returnValue)
                        return false;
                    pData += 8;
                    break;
                }
                case ICorJitInfo::PgoInstrumentationKind::TypeHandle:
                {
                    logicalDataToWrite = *(volatile intptr_t*)pData;

                    // As there could be tearing otherwise, inform the caller of exactly what value was written.
                    thProcessor((intptr_t)logicalDataToWrite);

                    bool returnValue = WriteCompressedIntToBytes(logicalDataToWrite - lastTypeDataWritten, byteWriter);
                    lastTypeDataWritten = logicalDataToWrite;
                    if (!returnValue)
                        return false;
                    pData += sizeof(intptr_t);
                    break;
                }
                case ICorJitInfo::PgoInstrumentationKind::MethodHandle:
                {
                    logicalDataToWrite = *(volatile intptr_t*)pData;

                    // As there could be tearing otherwise, inform the caller of exactly what value was written.
                    mhProcessor((intptr_t)logicalDataToWrite);

                    bool returnValue = WriteCompressedIntToBytes(logicalDataToWrite - lastMethodDataWritten, byteWriter);
                    lastMethodDataWritten = logicalDataToWrite;
                    if (!returnValue)
                        return false;
                    pData += sizeof(intptr_t);
                    break;
                }
                default:
                    _ASSERTE(!"Unexpected type");
                    return false;
            }
        }
        return true;
    }

    bool Finish()
    {
        // Terminate the schema list with an entry which is Done
        ICorJitInfo::PgoInstrumentationSchema terminationSchema = prevSchema;
        terminationSchema.InstrumentationKind = ICorJitInfo::PgoInstrumentationKind::Done;
        if (!WriteIndividualSchemaToBytes(prevSchema, terminationSchema, byteWriter))
            return false;

        return true;
    }
};

inline bool WriteInstrumentationSchema(const ICorJitInfo::PgoInstrumentationSchema* schemaTable, size_t cSchemas, uint8_t* array, size_t byteCount)
{
    return WriteInstrumentationSchemaToBytes(schemaTable, cSchemas, [&array, &byteCount](uint8_t data)
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
