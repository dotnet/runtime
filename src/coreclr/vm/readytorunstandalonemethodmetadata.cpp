#include "common.h"

#include "openum.h"

#ifdef FEATURE_READYTORUN
// Alternate form of metadata that represents a single method. Self contained except for type references
// The behavior of this code must exactly match that of the ReadyToRunStandaloneMethodMetadata class in CrossGen2
// That code can be found in src\coreclr\tools\aot\ILCompiler.ReadyToRun\Compiler\ReadyToRunStandaloneMethodMetadata.cs
class ReadyToRunStandaloneMethodMetadataHelper
{
    COR_ILMETHOD_DECODER header;

    SigBuilder nonCodeAlternateBlob;
    SigBuilder alternateNonTypeRefStream;
    SArray<uint8_t> ilStream;
    COUNT_T currentILStreamIterator;
    SArray<uint32_t> *pTypeRefTokenStream;

    MapSHash<uint32_t, uint32_t> alternateTokens;
    Module* pModule;
    IMDInternalImport* pMDImport;

public:

    ReadyToRunStandaloneMethodMetadataHelper(MethodDesc *pMD, SArray<uint32_t> *pTypeRefTokenStreamInput) :
        header(pMD->GetILHeader(TRUE), pMD->GetMDImport(), NULL),
        currentILStreamIterator(0),
        pTypeRefTokenStream(pTypeRefTokenStreamInput),
        pModule(pMD->GetModule()),
        pMDImport(pMD->GetMDImport())
    {
        {
            // Fill IL stream with initial data
            byte* ilStreamData = ilStream.OpenRawBuffer(header.CodeSize);
            memcpy(ilStreamData, header.Code, header.CodeSize);
            ilStream.CloseRawBuffer(header.CodeSize);
        }
    }

    void GenerateDataStreams(SArray<uint8_t> *pDataStream)
    {
        unsigned ehCount = header.EHCount();
        nonCodeAlternateBlob.AppendData(ehCount);
        for (unsigned e = 0; e < ehCount; e++)
        {
            IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehBuff;
            const IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT* ehInfo;

            ehInfo = header.EH->EHClause(e, &ehBuff);

            nonCodeAlternateBlob.AppendData(ehInfo->Flags);
            nonCodeAlternateBlob.AppendData(ehInfo->TryOffset);
            nonCodeAlternateBlob.AppendData(ehInfo->TryLength);
            nonCodeAlternateBlob.AppendData(ehInfo->HandlerOffset);
            nonCodeAlternateBlob.AppendData(ehInfo->HandlerLength);
            if (ehInfo->Flags == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_NONE) // typed eh
            {
                nonCodeAlternateBlob.AppendToken(GetAlternateToken(ehInfo->ClassToken));
            }
            else if (ehInfo->Flags == CorExceptionFlag::COR_ILEXCEPTION_CLAUSE_FILTER)
            {
                nonCodeAlternateBlob.AppendData(ehInfo->FilterOffset);
            }
        }

        if (header.cbLocalVarSig == 0)
        {
            nonCodeAlternateBlob.AppendByte(2);
        }
        else
        {
            nonCodeAlternateBlob.AppendByte((header.Flags & CorILMethod_InitLocals) ? 1 : 0);
            SigParser localSigParser(header.LocalVarSig, header.cbLocalVarSig);
            StandaloneSigTranslator sigTranslator(&localSigParser, &nonCodeAlternateBlob, this);
            sigTranslator.ParseLocalsSignature();
        }

        while (!DoneReadingIL())
        {
            uint32_t ilOpcode = ReadILByte();
            if (ilOpcode == CEE_PREFIX1)
            {
                ilOpcode = 0x100 + ReadILByte();
            }

            switch (ilOpcode)
            {
                case CEE_LDARG_S:
                case CEE_LDARGA_S:
                case CEE_STARG_S:
                case CEE_LDLOC_S:
                case CEE_LDLOCA_S:
                case CEE_STLOC_S:
                case CEE_LDC_I4_S:
                case CEE_UNALIGNED:
                case CEE_UNUSED69: // This is the no. prefix that is partially defined in Partition III.
                    SkipIL(1);
                    break;
                case CEE_LDARG:
                case CEE_LDARGA:
                case CEE_STARG:
                case CEE_LDLOC:
                case CEE_LDLOCA:
                case CEE_STLOC:
                    SkipIL(2);
                    break;
                case CEE_LDC_I4:
                case CEE_LDC_R4:
                    SkipIL(4);
                    break;
                case CEE_LDC_I8:
                case CEE_LDC_R8:
                    SkipIL(8);
                    break;
                case CEE_JMP:
                case CEE_CALL:
                case CEE_CALLI:
                case CEE_CALLVIRT:
                case CEE_CPOBJ:
                case CEE_LDOBJ:
                case CEE_LDSTR:
                case CEE_NEWOBJ:
                case CEE_CASTCLASS:
                case CEE_ISINST:
                case CEE_UNBOX:
                case CEE_LDFLD:
                case CEE_LDFLDA:
                case CEE_STFLD:
                case CEE_LDSFLD:
                case CEE_LDSFLDA:
                case CEE_STSFLD:
                case CEE_STOBJ:
                case CEE_BOX:
                case CEE_NEWARR:
                case CEE_LDELEMA:
                case CEE_LDELEM:
                case CEE_STELEM:
                case CEE_UNBOX_ANY:
                case CEE_REFANYVAL:
                case CEE_MKREFANY:
                case CEE_LDTOKEN:
                case CEE_LDFTN:
                case CEE_LDVIRTFTN:
                case CEE_INITOBJ:
                case CEE_CONSTRAINED:
                case CEE_SIZEOF:
                    ReplaceToken();
                    break;
                case CEE_BR_S:
                case CEE_LEAVE_S:
                case CEE_BRFALSE_S:
                case CEE_BRTRUE_S:
                case CEE_BEQ_S:
                case CEE_BGE_S:
                case CEE_BGT_S:
                case CEE_BLE_S:
                case CEE_BLT_S:
                case CEE_BNE_UN_S:
                case CEE_BGE_UN_S:
                case CEE_BGT_UN_S:
                case CEE_BLE_UN_S:
                case CEE_BLT_UN_S:
                    SkipIL(1);
                    break;
                case CEE_BR:
                case CEE_LEAVE:
                case CEE_BRFALSE:
                case CEE_BRTRUE:
                case CEE_BEQ:
                case CEE_BGE:
                case CEE_BGT:
                case CEE_BLE:
                case CEE_BLT:
                case CEE_BNE_UN:
                case CEE_BGE_UN:
                case CEE_BGT_UN:
                case CEE_BLE_UN:
                case CEE_BLT_UN:
                    SkipIL(4);
                    break;
                case CEE_SWITCH:
                    {
                        uint32_t count = ReadILUInt32();
                        if (count > 0x1FFFFFFF)
                            ThrowHR(COR_E_BADIMAGEFORMAT);
                        SkipIL(count * 4);
                    }
                    break;
                default:
                    continue;
            }
        }

        ClrSafeInt<COUNT_T> dataStreamSize;
        DWORD nonCodeAlternateDataSize = 0;
        DWORD alternateNonTypeRefStreamSize = 0;
        PVOID nonCodeAlternateData = nonCodeAlternateBlob.GetSignature(&nonCodeAlternateDataSize);
        PVOID alternateNonTypeRefStreamData = alternateNonTypeRefStream.GetSignature(&alternateNonTypeRefStreamSize);
        dataStreamSize = ClrSafeInt<COUNT_T>(S_SIZE_T(nonCodeAlternateDataSize) + S_SIZE_T(ilStream.GetCount()) + S_SIZE_T(alternateNonTypeRefStreamSize));
        if (dataStreamSize.IsOverflow())
            ThrowHR(COR_E_BADIMAGEFORMAT);

        uint8_t *pData = pDataStream->OpenRawBuffer(dataStreamSize.Value());
        memcpy(pData, nonCodeAlternateData, nonCodeAlternateDataSize);
        ilStream.Copy(pData + nonCodeAlternateDataSize, ilStream.Begin(), ilStream.GetCount());
        memcpy(pData + nonCodeAlternateDataSize + ilStream.GetCount(), alternateNonTypeRefStreamData, alternateNonTypeRefStreamSize);
        pDataStream->CloseRawBuffer();
    }

    uint32_t MemberRefParentCodedIndex(mdToken tk)
    {
        RID     rid = RidFromToken(tk);
        ULONG32 ulTyp = TypeFromToken(tk);

        if ((rid > 0xFFFFFF) || rid == 0)
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        rid = (rid << 3);
        uint32_t codedIndexType;
        switch (TypeFromToken(tk))
        {
            // TypeDef  is encoded with low bits 000
            case mdtTypeDef: codedIndexType = 0; break;
            // TypeRef  is encoded with low bits 001
            case mdtTypeRef: codedIndexType = 1; break;
            // ModuleRef is encoded with low bits 010
            case mdtModuleRef: codedIndexType = 2; break;
            // MethodDef is encoded with low bit 011
            case mdtMethodDef: codedIndexType = 3; break;
            // TypeSpec is encoded with low bits 100
            case mdtTypeSpec: codedIndexType = 4; break;
            default:
                ThrowHR(COR_E_BADIMAGEFORMAT);
        }

        return rid + codedIndexType;
    }

    uint32_t GetAlternateToken(uint32_t inputToken)
    {
        uint32_t alternativeToken = 0;
        if (!alternateTokens.Lookup(inputToken, &alternativeToken))
        {
            if ((TypeFromToken(inputToken) == mdtTypeDef) || (TypeFromToken(inputToken) == mdtTypeRef))
            {
                pTypeRefTokenStream->Append(inputToken);
                alternativeToken = TokenFromRid((uint32_t)pTypeRefTokenStream->GetCount(), mdtTypeRef);
            }
            else
            {
                uint32_t newTokenType = 0;
                SigBuilder blob;
                switch (TypeFromToken(inputToken))
                {
                    case mdtString:
                    {
                        newTokenType = mdtString;
                        LPCWSTR wszUserString;
                        DWORD cchString;
                        IfFailThrow(pMDImport->GetUserString(inputToken, &cchString, NULL, &wszUserString));
                        blob.AppendData(cchString);
                        blob.AppendBlob((void * const)wszUserString, sizeof(WCHAR) * cchString);
                        // TODO: consider encoding via wtf-8, or possibly utf-8
                        break;
                    }
                    case mdtTypeSpec:
                    {
                        newTokenType = mdtTypeSpec;
                        PCCOR_SIGNATURE sigPtr;
                        ULONG cbSig;
                        IfFailThrow(pMDImport->GetTypeSpecFromToken(inputToken, &sigPtr, &cbSig));
                        SigParser typeSpecSig(sigPtr, cbSig);
                        StandaloneSigTranslator sigTranslator(&typeSpecSig, &blob, this);
                        sigTranslator.ParseType();
                        break;
                    }
                    case mdtMemberRef:
                    {
                        newTokenType = mdtMemberRef;
                        PCCOR_SIGNATURE sig;
                        ULONG cbSig;
                        LPCSTR name;
                        mdToken memberRefParent;

                        IfFailThrow(pMDImport->GetNameAndSigOfMemberRef(inputToken, &sig, &cbSig, &name));
                        IfFailThrow(pMDImport->GetParentOfMemberRef(inputToken, &memberRefParent));

                        SigParser memberRefSigParse(sig, cbSig);
                        
                        StandaloneSigTranslator sigTranslator(&memberRefSigParse, &blob, this);
                        sigTranslator.ParseMemberRefSignature();
                        ULONG strLen = (ULONG)strlen(name); // Cast to ULONG is safe, as the data is held in a PE file
                        blob.AppendData(strLen);
                        blob.AppendBlob((const PVOID)name, strLen);
                        blob.AppendData(MemberRefParentCodedIndex(GetAlternateToken(memberRefParent)));
                        break;
                    }
                    case mdtMethodDef:
                    {
                        newTokenType = mdtMemberRef;
                        PCCOR_SIGNATURE sig;
                        ULONG cbSig;
                        LPCSTR name;
                        mdToken methodDefParent;

                        IfFailThrow(pMDImport->GetNameAndSigOfMethodDef(inputToken, &sig, &cbSig, &name));
                        IfFailThrow(pMDImport->GetParentToken(inputToken, &methodDefParent));
                        SigParser methodDefSigParse(sig, cbSig);
                        StandaloneSigTranslator sigTranslator(&methodDefSigParse, &blob, this);
                        sigTranslator.ParseMethodSignature();
                        ULONG strLen = (ULONG)strlen(name); // Cast to ULONG is safe, as the data is held in a PE file
                        blob.AppendData(strLen);
                        blob.AppendBlob((const PVOID)name, strLen);
                        blob.AppendData(MemberRefParentCodedIndex(GetAlternateToken(methodDefParent)));
                        break;
                    }
                    case mdtFieldDef:
                    {
                        newTokenType = mdtMemberRef;
                        PCCOR_SIGNATURE sig;
                        ULONG cbSig;
                        LPCSTR name;
                        mdToken fieldDefParent;

                        IfFailThrow(pMDImport->GetSigOfFieldDef(inputToken, &cbSig, &sig));
                        IfFailThrow(pMDImport->GetNameOfFieldDef(inputToken, &name));
                        IfFailThrow(pMDImport->GetParentToken(inputToken, &fieldDefParent));
                        SigParser fieldDefSigParse(sig, cbSig);
                        StandaloneSigTranslator sigTranslator(&fieldDefSigParse, &blob, this);
                        sigTranslator.ParseFieldSignature();
                        ULONG strLen = (ULONG)strlen(name); // Cast to ULONG is safe, as the data is held in a PE file
                        blob.AppendData(strLen);
                        blob.AppendBlob((const PVOID)name, strLen);
                        blob.AppendData(MemberRefParentCodedIndex(GetAlternateToken(fieldDefParent)));
                        break;
                    }
                    case mdtMethodSpec:
                    {
                        newTokenType = mdtMethodSpec;
                        PCCOR_SIGNATURE sig;
                        ULONG cbSig;
                        mdToken methodSpecParent;
                        IfFailThrow(pMDImport->GetMethodSpecProps(inputToken, &methodSpecParent, &sig, &cbSig));
                        mdToken tkMethodSpecParentAlternate = GetAlternateToken(methodSpecParent);
                        if (TypeFromToken(tkMethodSpecParentAlternate) != mdtMemberRef)
                        {
                            ThrowHR(COR_E_BADIMAGEFORMAT);
                        }
                        blob.AppendData(RidFromToken(tkMethodSpecParentAlternate));
                        SigParser methodSpecSigParse(sig, cbSig);
                        StandaloneSigTranslator sigTranslator(&methodSpecSigParse, &blob, this);
                        sigTranslator.ParseMethodSpecSignature();
                        break;
                    }
                    case mdtSignature:
                    {
                        newTokenType = mdtSignature;
                        PCCOR_SIGNATURE sig;
                        ULONG cbSig;
                        IfFailThrow(pMDImport->GetSigFromToken(inputToken, &cbSig, &sig));
                        SigParser standaloneSigParse(sig, cbSig);
                        StandaloneSigTranslator sigTranslator(&standaloneSigParse, &blob, this);
                        sigTranslator.ParseMethodSignature();
                        break;
                    }

                default:
                    ThrowHR(COR_E_BADIMAGEFORMAT);
                }

                ULONG newStreamLen;
                PVOID newSig = blob.GetSignature(&newStreamLen);

                alternateNonTypeRefStream.AppendBlob(newSig, newStreamLen);
                alternativeToken = TokenFromRid((uint32_t)alternateTokens.GetCount() + 1, newTokenType);
            }
            alternateTokens.Add(inputToken, alternativeToken);
        }
        return alternativeToken;
    }

    bool DoneReadingIL()
    {
        return currentILStreamIterator == ilStream.GetCount();
    }
    uint8_t ReadILByte()
    {
        if (DoneReadingIL())
        {
            ThrowHR(COR_E_BADIMAGEFORMAT);
        }
        uint8_t result = ilStream[currentILStreamIterator];
        ++currentILStreamIterator;
        return result;
    }
    void SkipIL(size_t countToSkip)
    {
        currentILStreamIterator = currentILStreamIterator + (COUNT_T)countToSkip;
        if (currentILStreamIterator > ilStream.GetCount())
            ThrowHR(COR_E_BADIMAGEFORMAT);
    }
    uint32_t ReadILUInt32()
    {
        uint32_t val;
        if ((currentILStreamIterator + 4) > ilStream.GetCount())
            ThrowHR(COR_E_BADIMAGEFORMAT);
        ilStream.Copy(&val, ilStream.Begin() + currentILStreamIterator, 4);
        currentILStreamIterator = currentILStreamIterator + 4;
        val = VAL32(val);
        return val;
    }
    void ReplaceToken()
    {
        if ((currentILStreamIterator + 4) > ilStream.GetCount())
            ThrowHR(COR_E_BADIMAGEFORMAT);

        uint32_t token;
        ilStream.Copy(&token, ilStream.Begin() + currentILStreamIterator, 4);
        token = VAL32(token);
        uint32_t newToken = GetAlternateToken(token);
        newToken = VAL32(newToken);
        ilStream.Copy(ilStream.Begin() + currentILStreamIterator, reinterpret_cast<uint8_t*>(&newToken), 4);
        currentILStreamIterator = currentILStreamIterator + 4;
    }

    class StandaloneSigTranslator
    {
        SigParser *pSigInput;
        SigBuilder *pSigOutput;
        ReadyToRunStandaloneMethodMetadataHelper *pHelper;

        uint8_t ParseByte()
        {
            uint8_t value;
            IfFailThrow(pSigInput->GetByte(&value));
            pSigOutput->AppendByte(value);
            return value;
        }

        uint8_t PeekByte()
        {
            uint8_t value;
            IfFailThrow(pSigInput->PeekByte(&value));
            return value;
        }

        uint32_t ParseCompressedInt()
        {
            uint32_t value;
            IfFailThrow(pSigInput->GetData(&value));
            pSigOutput->AppendData(value);
            return value;
        }

        void ParseTypeHandle()
        {
            uint32_t token;
            IfFailThrow(pSigInput->GetToken(&token));
            uint32_t newToken = pHelper->GetAlternateToken(token);
            pSigOutput->AppendToken(newToken);
        }

        public:
        StandaloneSigTranslator(SigParser *sigInput, SigBuilder* sigOutput, ReadyToRunStandaloneMethodMetadataHelper *helper) :
            pSigInput(sigInput),
            pSigOutput(sigOutput),
            pHelper(helper)
        {}

        void ParseType()
        {
            CorElementType elemType;
            for (;;)
            {
                elemType = (CorElementType)ParseByte();
                switch (elemType)
                {
                    case ELEMENT_TYPE_CMOD_REQD:
                    case ELEMENT_TYPE_CMOD_OPT:
                        ParseTypeHandle();
                        continue;
                    case ELEMENT_TYPE_PINNED:
                    case ELEMENT_TYPE_SENTINEL:
                        continue;

                    default:
                        break;
                }
                break;
            }

            switch (elemType)
            {
                case ELEMENT_TYPE_VOID:
                case ELEMENT_TYPE_BOOLEAN:
                case ELEMENT_TYPE_CHAR:
                case ELEMENT_TYPE_I1:
                case ELEMENT_TYPE_U1:
                case ELEMENT_TYPE_I2:
                case ELEMENT_TYPE_U2:
                case ELEMENT_TYPE_I4:
                case ELEMENT_TYPE_U4:
                case ELEMENT_TYPE_I8:
                case ELEMENT_TYPE_U8:
                case ELEMENT_TYPE_R4:
                case ELEMENT_TYPE_R8:
                case ELEMENT_TYPE_STRING:
                case ELEMENT_TYPE_OBJECT:
                case ELEMENT_TYPE_TYPEDBYREF:
                case ELEMENT_TYPE_I:
                case ELEMENT_TYPE_U:
                    break;

                case ELEMENT_TYPE_SZARRAY:
                case ELEMENT_TYPE_BYREF:
                case ELEMENT_TYPE_PTR:
                    ParseType();
                    break;
                case ELEMENT_TYPE_ARRAY:
                {
                    ParseType();
                    uint32_t rank = ParseCompressedInt();
                    uint32_t boundsCount = ParseCompressedInt();
                    for (uint32_t i = 0; i < boundsCount; i++)
                    {
                        ParseCompressedInt();
                    }
                    uint32_t lowerBoundsCount = ParseCompressedInt();
                    for (uint32_t i = 0; i < lowerBoundsCount; i++)
                    {
                        ParseCompressedInt(); // We don't need to parse as signed compressed ints as those can be round-tripped without distinguishing from a normal compressed int
                    }
                    break;
                }
                case ELEMENT_TYPE_VAR:
                case ELEMENT_TYPE_MVAR:
                    ParseCompressedInt();
                    break;
                case ELEMENT_TYPE_GENERICINST:
                {
                    ParseType();
                    uint32_t instanceLength = ParseCompressedInt();
                    for (uint32_t i = 0; i < instanceLength; i++)
                    {
                        ParseType();
                    }
                    break;
                }
                case ELEMENT_TYPE_FNPTR:
                {
                    ParseMethodSignature();
                    break;
                }
                case ELEMENT_TYPE_CLASS:
                case ELEMENT_TYPE_VALUETYPE:
                {
                    ParseTypeHandle();
                    break;
                }
                default:
                    ThrowHR(COR_E_BADIMAGEFORMAT);
            }
        }

        void ParseLocalsSignature()
        {
            uint8_t sigHeader = ParseByte();
            if (sigHeader != IMAGE_CEE_CS_CALLCONV_LOCAL_SIG)
                ThrowHR(COR_E_BADIMAGEFORMAT);
            
            uint32_t localsCount = ParseCompressedInt();
            for (uint32_t i = 0; i < localsCount; i++)
            {
                ParseType();
            }
        }

        void ParseMemberRefSignature()
        {
            uint8_t sigHeader = PeekByte();
            if (sigHeader == IMAGE_CEE_CS_CALLCONV_FIELD)
            {
                ParseFieldSignature();
            }
            else
            {
                ParseMethodSignature();
            }
        }

        void ParseFieldSignature()
        {
            uint8_t sigHeader = ParseByte();
            if (sigHeader != IMAGE_CEE_CS_CALLCONV_FIELD)
            {
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }

            ParseType();
        }

        void ParseMethodSpecSignature()
        {
            uint8_t sigHeader = ParseByte();
            if (sigHeader != IMAGE_CEE_CS_CALLCONV_GENERICINST)
            {
                ThrowHR(COR_E_BADIMAGEFORMAT);
            }

            uint32_t argCount = ParseCompressedInt();
            for (uint32_t i = 0; i < argCount; i++)
            {
                ParseType();
            }
        }

        void ParseMethodSignature()
        {
            uint8_t sigHeader = ParseByte();
            if (sigHeader & IMAGE_CEE_CS_CALLCONV_GENERIC)
            {
                // Parse arity
                ParseCompressedInt();
            }
            uint32_t argCount = ParseCompressedInt();
            for (uint32_t i = 0; i <= argCount; i++)
            {
                ParseType();
            }
        }
    };
};

#ifndef DACCESS_COMPILE
static CrstStatic s_csReadyToRunStandaloneMethodMetadata;
static MapSHash<MethodDesc*, ReadyToRunStandaloneMethodMetadata*> *s_methodMetadata = NULL;

void GenerateReadyToRunStandaloneMethodMetadata(MethodDesc *pMD, ReadyToRunStandaloneMethodMetadata *pBlock)
{
    SArray<uint32_t> tokenStream;
    SArray<uint8_t> byteData;
    ReadyToRunStandaloneMethodMetadataHelper helper(pMD, &tokenStream);
    helper.GenerateDataStreams(&byteData);

    pBlock->cByteData = byteData.GetCount();
    pBlock->pByteData = new uint8_t[pBlock->cByteData];
    pBlock->cTypes = tokenStream.GetCount();
    pBlock->pTypes = new TypeHandle[pBlock->cTypes];

    byteData.Copy((uint8_t*)pBlock->pByteData, byteData.Begin(), byteData.GetCount());
    for (COUNT_T i = 0; i < tokenStream.GetCount(); i++)
    {
        ((TypeHandle*)pBlock->pTypes)[i] = ClassLoader::LoadTypeDefOrRefThrowing(pMD->GetModule(), tokenStream[i], ClassLoader::ThrowIfNotFound, ClassLoader::PermitUninstDefOrRef, 0, CLASS_LOAD_APPROXPARENTS);
    }
}

void InitReadyToRunStandaloneMethodMetadata()
{
    s_csReadyToRunStandaloneMethodMetadata.Init(CrstLeafLock);
    s_methodMetadata = new MapSHash<MethodDesc*, ReadyToRunStandaloneMethodMetadata*>;
}

ReadyToRunStandaloneMethodMetadata* GetReadyToRunStandaloneMethodMetadata(MethodDesc *pMD)
{
    ReadyToRunStandaloneMethodMetadata* retVal;

    {
        CrstHolder lock(&s_csReadyToRunStandaloneMethodMetadata);
        if (s_methodMetadata->Lookup(pMD, &retVal))
        {
            return retVal;
        }
    }

    NewHolder<ReadyToRunStandaloneMethodMetadata> newMethodBlock = new ReadyToRunStandaloneMethodMetadata();
    GenerateReadyToRunStandaloneMethodMetadata(pMD, newMethodBlock);

    {
        CrstHolder lock(&s_csReadyToRunStandaloneMethodMetadata);
        if (s_methodMetadata->Lookup(pMD, &retVal))
        {
            return retVal;
        }

        s_methodMetadata->Add(pMD, newMethodBlock);
        retVal = newMethodBlock.Extract();
    }
    return retVal;
}

#endif // !DACCESS_COMPILE
#endif // FEATURE_READYTORUN
