// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "common.h"
#include "versionresilienthashcode.h"
#include "typehashingalgorithms.h"
#include "openum.h"

bool GetVersionResilientTypeHashCode(IMDInternalImport *pMDImport, mdExportedType token, int * pdwHashCode)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
        MODE_ANY;
        PRECONDITION(CheckPointer(pdwHashCode));
    }
    CONTRACTL_END

    _ASSERTE(TypeFromToken(token) == mdtTypeDef ||
        TypeFromToken(token) == mdtTypeRef ||
        TypeFromToken(token) == mdtExportedType);
    _ASSERTE(!IsNilToken(token));

    HRESULT hr;
    LPCUTF8 szNamespace;
    LPCUTF8 szName;
    bool hasTypeToken = true;
    int hashcode = 0;

    while (hasTypeToken)
    {
        if (IsNilToken(token))
            return false;

        switch (TypeFromToken(token))
        {
        case mdtTypeDef:
            if (FAILED(pMDImport->GetNameOfTypeDef(token, &szName, &szNamespace)))
                return false;
            hr = pMDImport->GetNestedClassProps(token, &token);
            if (hr == CLDB_E_RECORD_NOTFOUND)
                hasTypeToken = false;
            else if (FAILED(hr))
                return false;
            break;

        case mdtTypeRef:
            if (FAILED(pMDImport->GetNameOfTypeRef(token, &szNamespace, &szName)))
                return false;
            if (FAILED(pMDImport->GetResolutionScopeOfTypeRef(token, &token)))
                return false;
            hasTypeToken = (TypeFromToken(token) == mdtTypeRef);
            break;

        case mdtExportedType:
            if (FAILED(pMDImport->GetExportedTypeProps(token, &szNamespace, &szName, &token, NULL, NULL)))
                return false;
            hasTypeToken = (TypeFromToken(token) == mdtExportedType);
            break;

        default:
            return false;
        }

        hashcode ^= ComputeNameHashCode(szNamespace, szName);
    }

    *pdwHashCode = hashcode;

    return true;
}

#ifndef DACCESS_COMPILE
int GetVersionResilientTypeHashCode(TypeHandle type)
{
    STANDARD_VM_CONTRACT;

    if (type.IsArray())
    {
        return ComputeArrayTypeHashCode(GetVersionResilientTypeHashCode(type.GetArrayElementTypeHandle()), type.GetRank());
    }
    else
    if (!type.IsTypeDesc())
    {
        MethodTable *pMT = type.AsMethodTable();

        _ASSERTE(!pMT->IsArray());
        _ASSERTE(!IsNilToken(pMT->GetCl()));

        LPCUTF8 szNamespace;
        LPCUTF8 szName;
        IfFailThrow(pMT->GetMDImport()->GetNameOfTypeDef(pMT->GetCl(), &szName, &szNamespace));
        int hashcode = ComputeNameHashCode(szNamespace, szName);

        MethodTable *pMTEnclosing = pMT->LoadEnclosingMethodTable(CLASS_LOAD_UNRESTOREDTYPEKEY);
        if (pMTEnclosing != NULL)
        {
            hashcode = ComputeNestedTypeHashCode(GetVersionResilientTypeHashCode(TypeHandle(pMTEnclosing)), hashcode);
        }

        if (!pMT->IsGenericTypeDefinition() && pMT->HasInstantiation())
        {
            return ComputeGenericInstanceHashCode(hashcode,
                pMT->GetInstantiation().GetNumArgs(), pMT->GetInstantiation(), GetVersionResilientTypeHashCode);
        }
        else
        {
            return hashcode;
        }
    }
    else
    if (type.IsPointer())
    {
        return ComputePointerTypeHashCode(GetVersionResilientTypeHashCode(type.AsTypeDesc()->GetTypeParam()));
    }
    else
    if (type.IsByRef())
    {
        return ComputeByrefTypeHashCode(GetVersionResilientTypeHashCode(type.AsTypeDesc()->GetTypeParam()));
    }

    assert(false);
    return 0;
}

int GetVersionResilientMethodHashCode(MethodDesc *pMD)
{
    STANDARD_VM_CONTRACT;

    int hashCode = GetVersionResilientTypeHashCode(TypeHandle(pMD->GetMethodTable()));

    // Todo: Add signature to hash.
    if (pMD->GetNumGenericMethodArgs() > 0)
    {
        hashCode ^= ComputeGenericInstanceHashCode(ComputeNameHashCode(pMD->GetName()), pMD->GetNumGenericMethodArgs(), pMD->GetMethodInstantiation(), GetVersionResilientTypeHashCode);
    }
    else
    {
        hashCode ^= ComputeNameHashCode(pMD->GetName());
    }

    return hashCode;
}

int GetVersionResilientModuleHashCode(Module* pModule)
{
    return ComputeNameHashCode(pModule->GetSimpleName());
}

class ILInstructionParser
{
    const uint8_t *_pCode;
    uint32_t _cbCode;

public:
    ILInstructionParser(const uint8_t *pCode, uint32_t cbCode) :
        _pCode(pCode), _cbCode(cbCode)
    {}

    bool GetByte(uint8_t *data)
    {
        if (_cbCode >= 1)
        {
            *data = *_pCode;
            _cbCode--;
            _pCode++;
            return true;
        }
        return false;
    }

    bool GetUInt16(uint16_t *data)
    {
        if (_cbCode >= 2)
        {
            *data = *(uint16_t UNALIGNED*)_pCode;
            _cbCode -= 2;
            _pCode += 2;
            return true;
        }
        return false;
    }

    bool GetUInt32(uint32_t *data)
    {
        if (_cbCode >= 4)
        {
            *data = *(uint32_t UNALIGNED*)_pCode;
            _cbCode -= 4;
            _pCode += 4;
            return true;
        }
        return false;
    }

    bool IsEmpty()
    {
        return _cbCode == 0;
    }
};

// Use the SigParser type to handle bounds checks safely
bool AddVersionResilientHashCodeForInstruction(ILInstructionParser *parser, xxHash *hash)
{
    uint16_t opcodeValue;
    BYTE firstByte;
    if (!parser->GetByte(&firstByte))
    {
        return false;
    }
    if (firstByte != 0xFE)
    {
        opcodeValue = 0xFF00 | firstByte;
    }
    else
    {
        BYTE secondByte;
        if (!parser->GetByte(&secondByte))
        {
            return false;
        }
        opcodeValue = (((uint16_t)firstByte) << 8) | (uint16_t)secondByte;
    }

    hash->Add(opcodeValue);

    opcode_format_t opcodeFormat;
    switch (opcodeValue)
    {
#define OPDEF_REAL_OPCODES_ONLY
#define OPDEF(name, stringname, stackpop, stackpush, params, kind, len, byte1, byte2, ctrl) \
        case (((uint16_t)byte1) << 8) | (uint16_t)byte2: opcodeFormat = params; break;
#include "opcode.def"
#undef  OPDEF
#undef OPDEF_REAL_OPCODES_ONLY
        default: _ASSERTE(false); return false;
    }

    switch (opcodeFormat)
    {
        case InlineNone: // no inline args
            break;

        case ShortInlineI:
        case ShortInlineBrTarget:
        case ShortInlineVar: // 1 byte value which is token change resilient
        {
            uint8_t varValue;
            if (!parser->GetByte(&varValue))
                return false;
            hash->Add(varValue);
            break;
        }
        
        case InlineVar: // 2 byte value which is token change resilient
        {
            uint16_t varValue;
            if (!parser->GetUInt16(&varValue))
                return false;
            hash->Add(varValue);
            break;
        }
        case InlineI:
        case InlineBrTarget:
        case ShortInlineR: // 4 byte value which is token change resilient
        {
            uint32_t varValue;
            if (!parser->GetUInt32(&varValue))
                return false;
            hash->Add(varValue);
            break;
        }

        case InlineR:
        case InlineI8: // 8 byte value which is token change resilient
        {
            // Handle as a pair of 4 byte values
            uint32_t varValue;
            uint32_t varValue2;
            if (!parser->GetUInt32(&varValue))
                return false;
            if (!parser->GetUInt32(&varValue2))
                return false;
            hash->Add(varValue);
            hash->Add(varValue2);
            break;
        }

        case InlineSwitch:
        {
            // Switch is variable length, so use a variable length hash function
            uint32_t switchCount;
            if (!parser->GetUInt32(&switchCount))
                return false;

            hash->Add(opcodeValue);
            hash->Add(switchCount);
            for (;switchCount > 0; switchCount--)
            {
                uint32_t switchEntry;
                if (!parser->GetUInt32(&switchEntry))
                    return false;
                hash->Add(switchEntry);
            }
            break;
        }

        case InlineMethod:
        case InlineField:
        case InlineType:
        case InlineString:
        case InlineSig:
        case InlineTok:
        {
            // 4 byte value which is token dependent. Ignore.
            uint32_t varValue;
            if (!parser->GetUInt32(&varValue))
                return false;
            break;
        }
        default:
        {
            // Bad code
            _ASSERTE(FALSE);
            return false;
        }
    }

    return true;
}

bool GetVersionResilientILCodeHashCode(MethodDesc *pMD, int* hashCode, unsigned* ilSize)
{
    uint32_t maxStack;
    uint32_t EHCount;
    const BYTE* pILCode;
    uint32_t cbILCode;
    bool initLocals;
    SigParser localSig;

    xxHash hashILData;

    if (pMD->IsDynamicMethod())
    {
        DynamicResolver * pResolver = pMD->AsDynamicMethodDesc()->GetResolver();
        CorInfoOptions options;
        pILCode = pResolver->GetCodeInfo(&cbILCode,
                                         &maxStack,
                                         &options,
                                         &EHCount);

        localSig = pResolver->GetLocalSig();

        initLocals = (options & CORINFO_OPT_INIT_LOCALS) == CORINFO_OPT_INIT_LOCALS;
    }
    else
    {
        COR_ILMETHOD_DECODER header(pMD->GetILHeader(TRUE), pMD->GetMDImport(), NULL);

        pILCode = header.Code;
        cbILCode = header.GetCodeSize();
        maxStack = header.GetMaxStack();
        EHCount = header.EHCount();
        initLocals = (header.GetFlags() & CorILMethod_InitLocals) == CorILMethod_InitLocals;
        localSig = SigParser(header.LocalVarSig, header.cbLocalVarSig);

        for (uint32_t ehClause = 0; ehClause < EHCount; ehClause++)
        {
            IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT ehClauseBuf;
            auto ehInfo = header.EH->EHClause(ehClause, &ehClauseBuf);

            hashILData.Add(ehInfo->Flags);
            hashILData.Add(ehInfo->TryOffset);
            hashILData.Add(ehInfo->TryLength);
            hashILData.Add(ehInfo->HandlerLength);
            hashILData.Add(ehInfo->HandlerOffset);
            if ((ehInfo->Flags & COR_ILEXCEPTION_CLAUSE_FILTER) == COR_ILEXCEPTION_CLAUSE_FILTER)
            {
                hashILData.Add(ehInfo->FilterOffset);
            }
            // Do not hash the classToken field, as is possibly token dependent
        }
    }

    hashILData.Add(maxStack);
    hashILData.Add(EHCount);

    ILInstructionParser ilParser(pILCode, cbILCode);
    *ilSize = cbILCode;
    while (!ilParser.IsEmpty())
    {
        if (!AddVersionResilientHashCodeForInstruction(&ilParser, &hashILData))
            return false;
    }

    // TODO! Analyze if adding hash of non-token depenendent portions of local signature is useful
    *hashCode = (int)hashILData.ToHashCode();
    return true;
}


#endif // DACCESS_COMPILE
