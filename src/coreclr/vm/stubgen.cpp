// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: StubGen.cpp
//

//


#include "common.h"

#include "stubgen.h"
#include "jitinterface.h"
#include "ilstubcache.h"
#include "sigbuilder.h"

#include "formattype.h"
#include "typestring.h"


#include "field.h"

//
//   ....[.....\xxxxx]..0...  -> ....[xxxxx]..0...
//       ^     ^        ^

void DumpIL_RemoveFullPath(SString &strTokenFormatting)
{
    STANDARD_VM_CONTRACT;
    if (strTokenFormatting.IsEmpty())
        return;

    SString::Iterator begin = strTokenFormatting.Begin();
    SString::Iterator end = strTokenFormatting.End();
    SString::Iterator leftBracket = strTokenFormatting.Begin();

    // Find the first '[' in the string.
    while ((leftBracket != end) && (*leftBracket != '['))
    {
        ++leftBracket;
    }

    if (leftBracket != end)
    {
        SString::Iterator lastSlash = strTokenFormatting.End() - 1;

        // Find the last '\\' in the string.
        while ((lastSlash != leftBracket) && (*lastSlash != '\\'))
        {
            --lastSlash;
        }

        if (leftBracket != lastSlash)
        {
            strTokenFormatting.Delete(leftBracket + 1, lastSlash - leftBracket);
        }
    }
}

void ILStubLinker::DumpIL_FormatToken(mdToken token, SString &strTokenFormatting)
{
    void* pvLookupRetVal = (void*)POISONC;
    _ASSERTE(strTokenFormatting.IsEmpty());

    EX_TRY
    {
        if (TypeFromToken(token) == mdtMethodDef)
        {
            MethodDesc* pMD = m_tokenMap.LookupMethodDef(token);
            pvLookupRetVal = pMD;
            CONSISTENCY_CHECK(CheckPointer(pMD));

            pMD->GetFullMethodInfo(strTokenFormatting);
        }
        else if (TypeFromToken(token) == mdtTypeDef)
        {
            TypeHandle typeHnd = m_tokenMap.LookupTypeDef(token);
            pvLookupRetVal = typeHnd.AsPtr();
            CONSISTENCY_CHECK(!typeHnd.IsNull());

            MethodTable *pMT = NULL;
            if (typeHnd.IsTypeDesc())
            {
                TypeDesc *pTypeDesc = typeHnd.AsTypeDesc();
                pMT = pTypeDesc->GetMethodTable();
            }
            else
            {
                pMT = typeHnd.AsMethodTable();
            }

            // AppendType handles NULL correctly
            SString typeName;
            TypeString::AppendType(typeName, TypeHandle(pMT));

            if (pMT && typeHnd.IsNativeValueType())
                typeName.Append(W("_NativeValueType"));
            typeName.ConvertToUTF8(strTokenFormatting);
        }
        else if (TypeFromToken(token) == mdtFieldDef)
        {
            FieldDesc* pFD = m_tokenMap.LookupFieldDef(token);
            pvLookupRetVal = pFD;
            CONSISTENCY_CHECK(CheckPointer(pFD));

            SString typeName;
            TypeString::AppendType(typeName, TypeHandle(pFD->GetApproxEnclosingMethodTable()));

            strTokenFormatting.Printf("%s::%s", typeName.GetUTF8(), pFD->GetName());
        }
        else if (TypeFromToken(token) == mdtModule)
        {
            // Do nothing, because strTokenFormatting is already empty.
        }
        else if (TypeFromToken(token) == mdtSignature)
        {
            CQuickBytes qbTargetSigBytes;
            PCCOR_SIGNATURE pSig;
            uint32_t cbSig;

            if (token == TOKEN_ILSTUB_TARGET_SIG)
            {
                // Build the current target sig into the buffer.
                cbSig = GetStubTargetMethodSigSize();
                pSig = (PCCOR_SIGNATURE)qbTargetSigBytes.AllocThrows(cbSig);

                GetStubTargetMethodSig((BYTE*)pSig, cbSig);
            }
            else
            {
                SigPointer sig = m_tokenMap.LookupSig(token);
                sig.GetSignature(&pSig, &cbSig);
            }

            IMDInternalImport * pIMDI = CoreLibBinder::GetModule()->GetMDImport();
            CQuickBytes sigStr;
            PrettyPrintSig(pSig, cbSig, "", &sigStr, pIMDI, NULL);

            strTokenFormatting.SetUTF8((LPUTF8)sigStr.Ptr());
        }
        else
        {
            strTokenFormatting.Printf("%d", token);
        }
        DumpIL_RemoveFullPath(strTokenFormatting);
    }
    EX_CATCH
    {
        strTokenFormatting.Printf("%d", token);
    }
    EX_END_CATCH(SwallowAllExceptions)
}

void ILCodeStream::Emit(ILInstrEnum instr, INT16 iStackDelta, UINT_PTR uArg)
{
    STANDARD_VM_CONTRACT;

    UINT idxCurInstr = 0;
    ILStubLinker::ILInstruction* pInstrBuffer = NULL;

    if (NULL == m_pqbILInstructions)
    {
        m_pqbILInstructions = new ILCodeStreamBuffer();
    }

    idxCurInstr = m_uCurInstrIdx;

    m_uCurInstrIdx++;
    m_pqbILInstructions->ReSizeThrows(m_uCurInstrIdx * sizeof(ILStubLinker::ILInstruction));

    pInstrBuffer = (ILStubLinker::ILInstruction*)m_pqbILInstructions->Ptr();

    pInstrBuffer[idxCurInstr].uInstruction = static_cast<UINT16>(instr);
    pInstrBuffer[idxCurInstr].iStackDelta = iStackDelta;
    pInstrBuffer[idxCurInstr].uArg = uArg;
}

ILCodeLabel* ILStubLinker::NewCodeLabel()
{
    STANDARD_VM_CONTRACT;

    ILCodeLabel* pCodeLabel = new ILCodeLabel();

    pCodeLabel->m_pNext = m_pLabelList;
    pCodeLabel->m_pOwningStubLinker = this;
    pCodeLabel->m_pCodeStreamOfLabel = NULL;
    pCodeLabel->m_idxLabeledInstruction = -1;

    m_pLabelList = pCodeLabel;

    return pCodeLabel;
}

void ILCodeStream::EmitLabel(ILCodeLabel* pCodeLabel)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION_MSG(m_pOwner == pCodeLabel->m_pOwningStubLinker, "you can only use a code label in the ILStubLinker that created it!");
    }
    CONTRACTL_END;

    pCodeLabel->m_pCodeStreamOfLabel    = this;
    pCodeLabel->m_idxLabeledInstruction = m_uCurInstrIdx;

    Emit(CEE_CODE_LABEL, 0, (UINT_PTR)pCodeLabel);
}

static const BYTE s_rgbOpcodeSizes[] =
{

#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
    ((l) + (oprType)),

#define InlineNone            0
#define ShortInlineVar        1
#define ShortInlineI          1
#define InlineI               4
#define InlineI8              8
#define ShortInlineR          4
#define InlineR               8
#define InlineMethod          4
#define InlineSig             4
#define ShortInlineBrTarget   1
#define InlineBrTarget        4
#define InlineSwitch         -1
#define InlineType            4
#define InlineString          4
#define InlineField           4
#define InlineTok             4
#define InlineVar             2

#include "opcode.def"

#undef OPDEF
#undef InlineNone
#undef ShortInlineVar
#undef ShortInlineI
#undef InlineI
#undef InlineI8
#undef ShortInlineR
#undef InlineR
#undef InlineMethod
#undef InlineSig
#undef ShortInlineBrTarget
#undef InlineBrTarget
#undef InlineSwitch
#undef InlineType
#undef InlineString
#undef InlineField
#undef InlineTok
#undef InlineVar

};

struct ILOpcode
{
    BYTE byte1;
    BYTE byte2;
};

static const ILOpcode s_rgOpcodes[] =
{

#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
    { (s1), (s2) },
#include "opcode.def"
#undef OPDEF

};

ILCodeStream::ILInstrEnum ILCodeStream::LowerOpcode(ILInstrEnum instr, ILStubLinker::ILInstruction* pInstr)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(instr == (ILInstrEnum)pInstr->uInstruction);
    }
    CONTRACTL_END;

    //
    // NOTE:  we do not lower branches to their smallest form because that
    //        would introduce extra passes at link time, which isn't really
    //        worth the savings in IL code size.
    //

    UINT_PTR uConst = pInstr->uArg;

    switch (instr)
    {
        case CEE_LDC_I8:
        {
            if (uConst == (UINT_PTR)-1)
            {
                instr = CEE_LDC_I4_M1;
            }
            else
            if (uConst < 9)
            {
                instr = (ILInstrEnum)((UINT_PTR)CEE_LDC_I4_0 + uConst);
            }
            else
            if (FitsInI1(uConst))
            {
                instr = CEE_LDC_I4_S;
            }
            else
            if (FitsInI4(uConst))
            {
                instr = CEE_LDC_I4;
            }
            break;
        }

        case CEE_LDARG:
        {
            if (uConst <= 3)
            {
                instr = (ILInstrEnum)((UINT_PTR)CEE_LDARG_0 + uConst);
                break;
            }
            goto lShortForm;
        }
        case CEE_LDLOC:
        {
            if (uConst <= 3)
            {
                instr = (ILInstrEnum)((UINT_PTR)CEE_LDLOC_0 + uConst);
                break;
            }
            goto lShortForm;
        }
        case CEE_STLOC:
        {
            if (uConst <= 3)
            {
                instr = (ILInstrEnum)((UINT_PTR)CEE_STLOC_0 + uConst);
                break;
            }

lShortForm:
            if (FitsInI1(uConst))
            {
                static const UINT_PTR c_uMakeShortDelta = ((UINT_PTR)CEE_LDARG - (UINT_PTR)CEE_LDARG_S);
                static_assert_no_msg(((UINT_PTR)CEE_LDARG - c_uMakeShortDelta) == (UINT_PTR)CEE_LDARG_S);
                static_assert_no_msg(((UINT_PTR)CEE_LDLOC - c_uMakeShortDelta) == (UINT_PTR)CEE_LDLOC_S);
                static_assert_no_msg(((UINT_PTR)CEE_STLOC - c_uMakeShortDelta) == (UINT_PTR)CEE_STLOC_S);

                instr = (ILInstrEnum)((UINT_PTR)instr - c_uMakeShortDelta);
            }
            break;
        }

        case CEE_LDARGA:
        case CEE_STARG:
        case CEE_LDLOCA:
        {
            if (FitsInI1(uConst))
            {
                static const UINT_PTR c_uMakeShortDelta = ((UINT_PTR)CEE_LDARGA - (UINT_PTR)CEE_LDARGA_S);
                static_assert_no_msg(((UINT_PTR)CEE_LDARGA - c_uMakeShortDelta) == (UINT_PTR)CEE_LDARGA_S);
                static_assert_no_msg(((UINT_PTR)CEE_STARG  - c_uMakeShortDelta) == (UINT_PTR)CEE_STARG_S);
                static_assert_no_msg(((UINT_PTR)CEE_LDLOCA - c_uMakeShortDelta) == (UINT_PTR)CEE_LDLOCA_S);

                instr = (ILInstrEnum)((UINT_PTR)instr - c_uMakeShortDelta);
            }
            break;
        }

        default:
            break;
    }

    pInstr->uInstruction = static_cast<UINT16>(instr);
    return instr;
}

void ILStubLinker::PatchInstructionArgument(ILCodeLabel* pLabel, UINT_PTR uNewArg
    DEBUG_ARG(UINT16 uExpectedInstruction))
{
    LIMITED_METHOD_CONTRACT;

    UINT            idx                 = pLabel->m_idxLabeledInstruction;
    ILCodeStream*   pLabelCodeStream    = pLabel->m_pCodeStreamOfLabel;
    ILInstruction*  pLabelInstrBuffer   = (ILInstruction*)pLabelCodeStream->m_pqbILInstructions->Ptr();

    CONSISTENCY_CHECK(pLabelInstrBuffer[idx].uInstruction == ILCodeStream::CEE_CODE_LABEL);
    CONSISTENCY_CHECK(pLabelInstrBuffer[idx].iStackDelta == 0);

    idx++;

    CONSISTENCY_CHECK(idx < pLabelCodeStream->m_uCurInstrIdx);
    CONSISTENCY_CHECK(pLabelInstrBuffer[idx].uInstruction == uExpectedInstruction);

    pLabelInstrBuffer[idx].uArg = uNewArg;
}

ILCodeLabel::ILCodeLabel()
{
    m_pNext                 = NULL;
    m_pOwningStubLinker     = NULL;
    m_pCodeStreamOfLabel    = NULL;
    m_codeOffset            = -1;
    m_idxLabeledInstruction = -1;
}

ILCodeLabel::~ILCodeLabel()
{
}

size_t ILCodeLabel::GetCodeOffset()
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK(m_codeOffset != (size_t)-1);
    return m_codeOffset;
}


void ILCodeLabel::SetCodeOffset(size_t codeOffset)
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK((m_codeOffset == (size_t)-1) && (codeOffset != (size_t)-1));
    m_codeOffset = codeOffset;
}

static const LPCSTR s_rgOpcodeNames[] =
{
#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
    string,
#include "opcode.def"
#undef OPDEF

};

#include "openum.h"

static const BYTE s_rgbOpcodeArgType[] =
{

#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
    oprType,
#include "opcode.def"
#undef OPDEF

};


//---------------------------------------------------------------------------------------
//
void
ILStubLinker::LogILInstruction(
    size_t          curOffset,
    bool            isLabeled,
    INT             iCurStack,
    ILInstruction * pInstruction,
    SString *       pDumpILStubCode)
{
    STANDARD_VM_CONTRACT;
    //
    // format label
    //
    SString strLabel;

    if (isLabeled)
    {
        strLabel.Printf("IL_%04x:", (uint32_t)curOffset);
    }
    else
    {
        strLabel.SetUTF8("        ");
    }

    //
    // format opcode
    //
    SString strOpcode;

    ILCodeStream::ILInstrEnum instr = (ILCodeStream::ILInstrEnum)pInstruction->uInstruction;
    // Set the width of the opcode to 15.
    strOpcode.Printf("%-15s", s_rgOpcodeNames[instr]);

    //
    // format argument
    //

    static const size_t c_cchPreallocateArgument = 512;
    SString strArgument;
    strArgument.Preallocate(c_cchPreallocateArgument);

    static const size_t c_cchPreallocateTokenName = 1024;
    SString strTokenName;
    strTokenName.Preallocate(c_cchPreallocateTokenName);

    if (ILCodeStream::IsBranchInstruction(instr))
    {
        size_t branchDistance = (size_t)pInstruction->uArg;
        size_t targetOffset = curOffset + s_rgbOpcodeSizes[instr] + branchDistance;
        strArgument.Printf("IL_%04x", (uint32_t)targetOffset);
    }
    else if ((ILCodeStream::ILInstrEnum)CEE_NOP == instr)
    {
        strArgument.Printf("%s", (char *)pInstruction->uArg);
    }
    else
    {
        switch (s_rgbOpcodeArgType[instr])
        {
        case InlineNone:
            break;

        case ShortInlineVar:
        case ShortInlineI:
        case InlineI:
            strArgument.Printf("0x%p", pInstruction->uArg);
            break;

        case InlineI8:
            strArgument.Printf("0x%llx", (uint64_t)pInstruction->uArg);
            break;

        case InlineMethod:
        case InlineField:
        case InlineType:
        case InlineString:
        case InlineSig:
        case InlineRVA:
        case InlineTok:
            // No token value when we dump IL for ETW
            if (pDumpILStubCode == NULL)
                strArgument.Printf("0x%08p", pInstruction->uArg);

            // Dump to szTokenNameBuffer if logging, otherwise dump to szArgumentBuffer to avoid an extra space because we are omitting the token
            _ASSERTE(FitsIn<mdToken>(pInstruction->uArg));
            if (pDumpILStubCode == NULL)
                DumpIL_FormatToken(static_cast<mdToken>(pInstruction->uArg), strTokenName);
            else
                DumpIL_FormatToken(static_cast<mdToken>(pInstruction->uArg), strArgument);

            break;
        }
    }

    //
    // log it!
    //
    if (pDumpILStubCode)
    {
        pDumpILStubCode->AppendPrintf("%s /*(%2d)*/ %s %s %s\n", strLabel.GetUTF8(), iCurStack, strOpcode.GetUTF8(),
            strArgument.GetUTF8(), strTokenName.GetUTF8());
    }
    else
    {
        LOG((LF_STUBS, LL_INFO1000, "%s (%2d) %s %s %s\n", strLabel.GetUTF8(), iCurStack, \
            strOpcode.GetUTF8(), strArgument.GetUTF8(), strTokenName.GetUTF8()));
    }
} // ILStubLinker::LogILInstruction

//---------------------------------------------------------------------------------------
//
void
ILStubLinker::LogILStubWorker(
    ILInstruction * pInstrBuffer,
    UINT            numInstr,
    size_t *        pcbCode,
    INT *           piCurStack,
    SString *       pDumpILStubCode)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pcbCode));
        PRECONDITION(CheckPointer(piCurStack));
        PRECONDITION(CheckPointer(pDumpILStubCode, NULL_OK));
    }
    CONTRACTL_END;

    bool isLabeled = false;

    for (UINT i = 0; i < numInstr; i++)
    {
        ILCodeStream::ILInstrEnum instr = (ILCodeStream::ILInstrEnum)pInstrBuffer[i].uInstruction;
        CONSISTENCY_CHECK(ILCodeStream::IsSupportedInstruction(instr));

        if (instr == ILCodeStream::CEE_CODE_LABEL)
        {
            isLabeled = true;
            continue;
        }

        LogILInstruction(*pcbCode, isLabeled, *piCurStack, &pInstrBuffer[i], pDumpILStubCode);
        isLabeled = false;

        //
        // calculate the code size
        //
        PREFIX_ASSUME((size_t)instr < sizeof(s_rgbOpcodeSizes));
        *pcbCode += s_rgbOpcodeSizes[instr];

        //
        // calculate curstack
        //
        *piCurStack += pInstrBuffer[i].iStackDelta;
    }

    // Be sure to log any trailing label that has no associated instruction.
    if (isLabeled)
    {
        if (pDumpILStubCode)
        {
            pDumpILStubCode->AppendPrintf("IL_%04x:\n", (uint32_t)*pcbCode);
        }
        else
        {
            LOG((LF_STUBS, LL_INFO1000, "IL_%04zx:\n", *pcbCode));
        }
    }
}

static void LogJitFlags(DWORD facility, DWORD level, CORJIT_FLAGS jitFlags)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
    }
    CONTRACTL_END;

    LOG((facility, level, "jitFlags:\n"));

#define LOG_FLAG(name) \
    if (jitFlags.IsSet(name)) \
    { \
        LOG((facility, level, "   " #name "\n")); \
        jitFlags.Clear(name); \
    }

    // these are all we care about at the moment
    LOG_FLAG(CORJIT_FLAGS::CORJIT_FLAG_IL_STUB);
    LOG_FLAG(CORJIT_FLAGS::CORJIT_FLAG_PUBLISH_SECRET_PARAM);

#undef LOG_FLAGS

    if (!jitFlags.IsEmpty())
    {
        LOG((facility, level, "UNKNOWN FLAGS also set\n"));
    }
}

void ILStubLinker::LogILStub(CORJIT_FLAGS jitFlags, SString *pDumpILStubCode)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pDumpILStubCode, NULL_OK));
    }
    CONTRACTL_END;

    ILCodeStream*   pCurrentStream = m_pCodeStreamList;
    size_t          cbCode = 0;
    INT             iCurStack = 0;

    if (pDumpILStubCode == NULL)
        LogJitFlags(LF_STUBS, LL_INFO1000, jitFlags);

    while (pCurrentStream)
    {
        if (pCurrentStream->m_pqbILInstructions)
        {
            if (pDumpILStubCode)
                pDumpILStubCode->AppendPrintf("// %s {\n", pCurrentStream->GetStreamDescription(pCurrentStream->GetStreamType()));
            else
                LOG((LF_STUBS, LL_INFO1000, "%s {\n", pCurrentStream->GetStreamDescription(pCurrentStream->GetStreamType())));

            ILInstruction* pInstrBuffer = (ILInstruction*)pCurrentStream->m_pqbILInstructions->Ptr();
            LogILStubWorker(pInstrBuffer, pCurrentStream->m_uCurInstrIdx, &cbCode, &iCurStack, pDumpILStubCode);

            if (pDumpILStubCode)
                pDumpILStubCode->AppendPrintf("// } %s \n", pCurrentStream->GetStreamDescription(pCurrentStream->GetStreamType()));
            else
                LOG((LF_STUBS, LL_INFO1000, "}\n"));
        }

        pCurrentStream = pCurrentStream->m_pNextStream;
    }
}

bool ILStubLinker::FirstPassLink(ILInstruction* pInstrBuffer, UINT numInstr, size_t* pcbCode, INT* piCurStack, UINT* puMaxStack)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(puMaxStack));
    }
    CONTRACTL_END;

    bool fStackUnderflow = false;

    for (UINT i = 0; i < numInstr; i++)
    {
        ILCodeStream::ILInstrEnum instr = (ILCodeStream::ILInstrEnum)pInstrBuffer[i].uInstruction;
        CONSISTENCY_CHECK(ILCodeStream::IsSupportedInstruction(instr));

        //
        // down-size instructions
        //
        instr = ILCodeStream::LowerOpcode(instr, &pInstrBuffer[i]);

        //
        // fill in code label offsets
        //
        if (instr == ILCodeStream::CEE_CODE_LABEL)
        {
            ILCodeLabel* pLabel = (ILCodeLabel*)(pInstrBuffer[i].uArg);
            pLabel->SetCodeOffset(*pcbCode);
        }

        //
        // calculate the code size
        //
        PREFIX_ASSUME((size_t)instr < sizeof(s_rgbOpcodeSizes));
        *pcbCode += s_rgbOpcodeSizes[instr];

        //
        // calculate maxstack
        //
        *piCurStack += pInstrBuffer[i].iStackDelta;
        if (*piCurStack > (INT)*puMaxStack)
        {
            *puMaxStack = *piCurStack;
        }
#ifdef _DEBUG
        if (*piCurStack < 0)
        {
            fStackUnderflow = true;
        }
#endif // _DEBUG
    }

    return fStackUnderflow;
}

void ILStubLinker::SecondPassLink(ILInstruction* pInstrBuffer, UINT numInstr, size_t* pCurCodeOffset)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pCurCodeOffset));
    }
    CONTRACTL_END;

    for (UINT i = 0; i < numInstr; i++)
    {
        ILCodeStream::ILInstrEnum instr = (ILCodeStream::ILInstrEnum)pInstrBuffer[i].uInstruction;
        CONSISTENCY_CHECK(ILCodeStream::IsSupportedInstruction(instr));
        *pCurCodeOffset += s_rgbOpcodeSizes[instr];

        if (ILCodeStream::IsBranchInstruction(instr))
        {
            ILCodeLabel* pLabel = (ILCodeLabel*) pInstrBuffer[i].uArg;

            CONSISTENCY_CHECK(this == pLabel->m_pOwningStubLinker);
            CONSISTENCY_CHECK(IsInCodeStreamList(pLabel->m_pCodeStreamOfLabel));

            pInstrBuffer[i].uArg = pLabel->GetCodeOffset() - *pCurCodeOffset;
        }
    }
}

size_t ILStubLinker::Link(UINT* puMaxStack)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(puMaxStack));
    }
    CONTRACTL_END;

    //
    // Pass1: calculate code size, lower instructions to smallest form,
    //        fill in branch target offsets, and calculate maxstack
    //

    ILCodeStream*   pCurrentStream = m_pCodeStreamList;
    size_t          cbCode = 0;
    INT             iCurStack = 0;
    UINT            uMaxStack = 0;
    DEBUG_STMT(bool fStackUnderflow = false);

    while (pCurrentStream)
    {
        _ASSERTE(pCurrentStream->m_buildingEHClauses.GetCount() == 0);

        if (pCurrentStream->m_pqbILInstructions)
        {
            ILInstruction* pInstrBuffer = (ILInstruction*)pCurrentStream->m_pqbILInstructions->Ptr();
            INDEBUG( fStackUnderflow = ) FirstPassLink(pInstrBuffer, pCurrentStream->m_uCurInstrIdx, &cbCode, &iCurStack, &uMaxStack);
        }

        pCurrentStream = pCurrentStream->m_pNextStream;
    }

    //
    // Pass2: go back and patch the branch instructions
    //

    pCurrentStream = m_pCodeStreamList;
    size_t curCodeOffset = 0;

    while (pCurrentStream)
    {
        if (pCurrentStream->m_pqbILInstructions)
        {
            ILInstruction* pInstrBuffer = (ILInstruction*)pCurrentStream->m_pqbILInstructions->Ptr();
            SecondPassLink(pInstrBuffer, pCurrentStream->m_uCurInstrIdx, &curCodeOffset);
        }

        pCurrentStream = pCurrentStream->m_pNextStream;
    }

#ifdef _DEBUG
    if (fStackUnderflow)
    {
        LogILStub(CORJIT_FLAGS());
        CONSISTENCY_CHECK_MSG(false, "IL stack underflow! -- see logging output");
    }
#endif // _DEBUG

    *puMaxStack = uMaxStack;
    return cbCode;
}

size_t ILStubLinker::GetNumEHClauses()
{
    size_t result = 0;
    for (ILCodeStream* stream = m_pCodeStreamList; stream; stream = stream->m_pNextStream)
    {
        result += stream->m_finishedEHClauses.GetCount();
    }

    return result;
}

void ILStubLinker::WriteEHClauses(COR_ILMETHOD_SECT_EH* pSect)
{
    unsigned int clauseIndex = 0;
    for (ILCodeStream* stream = m_pCodeStreamList; stream; stream = stream->m_pNextStream)
    {
        const SArray<ILStubEHClauseBuilder>& clauses = stream->m_finishedEHClauses;
        for (COUNT_T i = 0; i < clauses.GetCount(); i++)
        {
            const ILStubEHClauseBuilder& builder = clauses[i];

            CorExceptionFlag flags;
            switch (builder.kind)
            {
            case ILStubEHClause::kTypedCatch: flags = COR_ILEXCEPTION_CLAUSE_NONE; break;
            case ILStubEHClause::kFinally: flags = COR_ILEXCEPTION_CLAUSE_FINALLY; break;
            default: UNREACHABLE_MSG("unexpected EH clause kind");
            }

            size_t tryBegin = builder.tryBeginLabel->GetCodeOffset();
            size_t tryEnd = builder.tryEndLabel->GetCodeOffset();
            size_t handlerBegin = builder.handlerBeginLabel->GetCodeOffset();
            size_t handlerEnd = builder.handlerEndLabel->GetCodeOffset();

            IMAGE_COR_ILMETHOD_SECT_EH_CLAUSE_FAT& clause = pSect->Fat.Clauses[clauseIndex];
            clause.Flags = flags;
            clause.TryOffset = static_cast<DWORD>(tryBegin);
            clause.TryLength = static_cast<DWORD>(tryEnd - tryBegin);
            clause.HandlerOffset = static_cast<DWORD>(handlerBegin);
            clause.HandlerLength = static_cast<DWORD>(handlerEnd - handlerBegin);
            clause.ClassToken = builder.typeToken;

            clauseIndex++;
        }
    }

    pSect->Fat.Kind = CorILMethod_Sect_EHTable | CorILMethod_Sect_FatFormat;
    pSect->Fat.DataSize = COR_ILMETHOD_SECT_EH_FAT::Size(clauseIndex);
}

#ifdef _DEBUG

static const PCSTR s_rgOpNames[] =
{

#define OPDEF(name,string,pop,push,oprType,opcType,l,s1,s2,ctrl) \
     #name,
#include "opcode.def"
#undef OPDEF

};


#endif // _DEBUG

BYTE* ILStubLinker::GenerateCodeWorker(BYTE* pbBuffer, ILInstruction* pInstrBuffer, UINT numInstr, size_t* pcbCode)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pcbCode));
    }
    CONTRACTL_END;

    for (UINT i = 0; i < numInstr; i++)
    {
        ILCodeStream::ILInstrEnum instr = (ILCodeStream::ILInstrEnum)pInstrBuffer[i].uInstruction;
        CONSISTENCY_CHECK(ILCodeStream::IsSupportedInstruction(instr));

        //
        // copy the IL instructions from the various linkers into the given buffer
        //
        if (instr != ILCodeStream::CEE_CODE_LABEL)
        {
            const ILOpcode* pOpcode = &s_rgOpcodes[instr];

            PREFIX_ASSUME((size_t)instr < sizeof(s_rgbOpcodeSizes));
            int     opSize = s_rgbOpcodeSizes[instr];
            bool    twoByteOp = (pOpcode->byte1 != 0xFF);
            int     argSize = opSize - (twoByteOp ? 2 : 1);

            if (twoByteOp)
            {
                *pbBuffer++ = pOpcode->byte1;
            }

            *pbBuffer++ = pOpcode->byte2;

            switch (argSize)
            {
                case 0:
                    break;

                case 1:
                    *pbBuffer = (BYTE)pInstrBuffer[i].uArg;
                    break;

                case 2:
                    SET_UNALIGNED_VAL16(pbBuffer, pInstrBuffer[i].uArg);
                    break;

                case 4:
                    SET_UNALIGNED_VAL32(pbBuffer, pInstrBuffer[i].uArg);
                    break;

                case 8:
                    {
                        UINT64 uVal = pInstrBuffer[i].uArg;
#ifndef TARGET_64BIT  // We don't have room on 32-bit platforms to store the CLR_NAN_64 value, so
                // we use a special value to represent CLR_NAN_64 and then recreate it here.
                        if ((instr == ILCodeStream::CEE_LDC_R8) && (((UINT32)uVal) == ILCodeStream::SPECIAL_VALUE_NAN_64_ON_32))
                            uVal = CLR_NAN_64;
#endif // TARGET_64BIT
                        SET_UNALIGNED_VAL64(pbBuffer, uVal);
                    }
                    break;

                default:
                    UNREACHABLE_MSG("unexpected il opcode argument size");
            }

            pbBuffer += argSize;
            *pcbCode += opSize;
        }
    }

    return pbBuffer;
}

void ILStubLinker::GenerateCode(BYTE* pbBuffer, size_t cbBufferSize)
{
    STANDARD_VM_CONTRACT;

    ILCodeStream*   pCurrentStream = m_pCodeStreamList;
    size_t          cbCode = 0;

    while (pCurrentStream)
    {
        if (pCurrentStream->m_pqbILInstructions)
        {
            ILInstruction* pInstrBuffer = (ILInstruction*)pCurrentStream->m_pqbILInstructions->Ptr();
            pbBuffer = GenerateCodeWorker(pbBuffer, pInstrBuffer, pCurrentStream->m_uCurInstrIdx, &cbCode);
        }

        pCurrentStream = pCurrentStream->m_pNextStream;
    }

    CONSISTENCY_CHECK(cbCode <= cbBufferSize);
}


#ifdef _DEBUG
bool ILStubLinker::IsInCodeStreamList(ILCodeStream* pcs)
{
    LIMITED_METHOD_CONTRACT;

    ILCodeStream*   pCurrentStream = m_pCodeStreamList;
    while (pCurrentStream)
    {
        if (pcs == pCurrentStream)
        {
            return true;
        }

        pCurrentStream = pCurrentStream->m_pNextStream;
    }

    return false;
}

// static
bool ILCodeStream::IsSupportedInstruction(ILInstrEnum instr)
{
    LIMITED_METHOD_CONTRACT;

    CONSISTENCY_CHECK_MSG(instr != CEE_SWITCH, "CEE_SWITCH is not supported currently due to InlineSwitch in s_rgbOpcodeSizes");
    CONSISTENCY_CHECK_MSG(((instr >= CEE_BR_S) && (instr <= CEE_BLT_UN_S)) || instr == CEE_LEAVE, "we only use long-form branch opcodes");
    return true;
}
#endif // _DEBUG

LPCSTR ILCodeStream::GetStreamDescription(ILStubLinker::CodeStreamType streamType)
{
    LIMITED_METHOD_CONTRACT;

    static LPCSTR lpszDescriptions[] = {
        "Initialize",
        "Marshal",
        "CallMethod",
        "UnmarshalReturn",
        "Unmarshal",
        "ExceptionCleanup",
        "Cleanup",
        "ExceptionHandler",
    };

#ifdef _DEBUG
    size_t len = sizeof(lpszDescriptions)/sizeof(LPCSTR);
    _ASSERT(streamType >= 0 && (size_t)streamType < len);
#endif // _DEBUG

    return lpszDescriptions[streamType];
}

void ILCodeStream::BeginTryBlock()
{
    ILStubEHClauseBuilder& clause = *m_buildingEHClauses.Append();
    memset(&clause, 0, sizeof(ILStubEHClauseBuilder));
    clause.kind = ILStubEHClause::kNone;
    clause.tryBeginLabel = NewCodeLabel();
    EmitLabel(clause.tryBeginLabel);
}

void ILCodeStream::EndTryBlock()
{
    _ASSERTE(m_buildingEHClauses.GetCount() > 0);
    ILStubEHClauseBuilder& clause = m_buildingEHClauses[m_buildingEHClauses.GetCount() - 1];

    _ASSERTE(clause.tryBeginLabel != NULL && clause.tryEndLabel == NULL);
    clause.tryEndLabel = NewCodeLabel();
    EmitLabel(clause.tryEndLabel);
}

void ILCodeStream::BeginHandler(DWORD kind, DWORD typeToken)
{
    _ASSERTE(m_buildingEHClauses.GetCount() > 0);
    ILStubEHClauseBuilder& clause = m_buildingEHClauses[m_buildingEHClauses.GetCount() - 1];

    _ASSERTE(clause.tryBeginLabel != NULL && clause.tryEndLabel != NULL &&
             clause.handlerBeginLabel == NULL && clause.kind == ILStubEHClause::kNone);

    clause.kind = kind;
    clause.typeToken = typeToken;
    clause.handlerBeginLabel = NewCodeLabel();
    EmitLabel(clause.handlerBeginLabel);
}

void ILCodeStream::EndHandler(DWORD kind)
{
    _ASSERTE(m_buildingEHClauses.GetCount() > 0);
    ILStubEHClauseBuilder& clause = m_buildingEHClauses[m_buildingEHClauses.GetCount() - 1];

    _ASSERTE(clause.tryBeginLabel != NULL && clause.tryEndLabel != NULL &&
             clause.handlerBeginLabel != NULL && clause.handlerEndLabel == NULL &&
             clause.kind == kind);

    clause.handlerEndLabel = NewCodeLabel();
    EmitLabel(clause.handlerEndLabel);

    m_finishedEHClauses.Append(clause);
    m_buildingEHClauses.SetCount(m_buildingEHClauses.GetCount() - 1);
}

void ILCodeStream::BeginCatchBlock(int token)
{
    BeginHandler(ILStubEHClause::kTypedCatch, static_cast<DWORD>(token));
}

void ILCodeStream::EndCatchBlock()
{
    EndHandler(ILStubEHClause::kTypedCatch);
}

void ILCodeStream::BeginFinallyBlock()
{
    BeginHandler(ILStubEHClause::kFinally, 0);
}

void ILCodeStream::EndFinallyBlock()
{
    EndHandler(ILStubEHClause::kFinally);
}

void ILCodeStream::EmitADD()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_ADD, -1, 0);
}
void ILCodeStream::EmitADD_OVF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_ADD_OVF, -1, 0);
}
void ILCodeStream::EmitAND()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_AND, -1, 0);
}
void ILCodeStream::EmitARGLIST()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_ARGLIST, 1, 0);
}

void ILCodeStream::EmitBEQ(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BEQ, -2, (UINT_PTR)pCodeLabel);
}

void ILCodeStream::EmitBGE(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BGE, -2, (UINT_PTR)pCodeLabel);
}

void ILCodeStream::EmitBGE_UN(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BGE_UN, -2, (UINT_PTR)pCodeLabel);
}

void ILCodeStream::EmitBGT(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BGT, -2, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBLE(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BLE, -2, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBLE_UN(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BLE_UN, -2, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBLT(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BLT, -2, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBNE_UN(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BNE_UN, -2, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBR(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BR, 0, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBREAK()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BREAK, 0, 0);
}
void ILCodeStream::EmitBRFALSE(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BRFALSE, -1, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitBRTRUE(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_BRTRUE, -1, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitCALL(int token, int numInArgs, int numRetArgs)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CALL, (INT16)(numRetArgs - numInArgs), token);
}
void ILCodeStream::EmitCALLVIRT(int token, int numInArgs, int numRetArgs)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CALLVIRT, (INT16)(numRetArgs - numInArgs), token);
}
void ILCodeStream::EmitCALLI(int token, int numInArgs, int numRetArgs)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CALLI, (INT16)(numRetArgs - numInArgs - 1), token);
}
void ILCodeStream::EmitCEQ()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CEQ, -1, 0);
}
void ILCodeStream::EmitCGT()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CGT, -1, 0);
}
void ILCodeStream::EmitCGT_UN()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CGT_UN, -1, 0);
}
void ILCodeStream::EmitCLT()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CLT, -1, 0);
}
void ILCodeStream::EmitCLT_UN()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CLT_UN, -1, 0);
}
void ILCodeStream::EmitCONV_I()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_I, 0, 0);
}
void ILCodeStream::EmitCONV_I1()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_I1, 0, 0);
}
void ILCodeStream::EmitCONV_I2()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_I2, 0, 0);
}
void ILCodeStream::EmitCONV_I4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_I4, 0, 0);
}
void ILCodeStream::EmitCONV_I8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_I8, 0, 0);
}
void ILCodeStream::EmitCONV_U()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_U, 0, 0);
}
void ILCodeStream::EmitCONV_U1()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_U1, 0, 0);
}
void ILCodeStream::EmitCONV_U2()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_U2, 0, 0);
}
void ILCodeStream::EmitCONV_U4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_U4, 0, 0);
}
void ILCodeStream::EmitCONV_U8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_U8, 0, 0);
}
void ILCodeStream::EmitCONV_R4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_R4, 0, 0);
}
void ILCodeStream::EmitCONV_R8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_R8, 0, 0);
}
void ILCodeStream::EmitCONV_OVF_I4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CONV_OVF_I4, 0, 0);
}
void ILCodeStream::EmitCONV_T(CorElementType t)
{
    STANDARD_VM_CONTRACT;

    switch (t)
    {
    case ELEMENT_TYPE_U1:
        EmitCONV_U1();
        break;
    case ELEMENT_TYPE_I1:
        EmitCONV_I1();
        break;
    case ELEMENT_TYPE_U2:
        EmitCONV_U2();
        break;
    case ELEMENT_TYPE_I2:
        EmitCONV_I2();
        break;
    case ELEMENT_TYPE_U4:
        EmitCONV_U4();
        break;
    case ELEMENT_TYPE_I4:
        EmitCONV_I4();
        break;
    case ELEMENT_TYPE_U8:
        EmitCONV_U8();
        break;
    case ELEMENT_TYPE_I8:
        EmitCONV_I8();
        break;
    case ELEMENT_TYPE_R4:
        EmitCONV_R4();
        break;
    case ELEMENT_TYPE_R8:
        EmitCONV_R8();
        break;
    case ELEMENT_TYPE_I:
        EmitCONV_I();
        break;
    case ELEMENT_TYPE_U:
        EmitCONV_U();
        break;
    default:
        _ASSERTE(!"Invalid type for conversion");
        break;
    }
}
void ILCodeStream::EmitCPBLK()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CPBLK, -3, 0);
}
void ILCodeStream::EmitCPOBJ(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_CPOBJ, -2, token);
}
void ILCodeStream::EmitDUP     ()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_DUP, 1, 0);
}
void ILCodeStream::EmitENDFINALLY()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_ENDFINALLY, 0, 0);
}
void ILCodeStream::EmitINITBLK()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_INITBLK, -3, 0);
}
void ILCodeStream::EmitINITOBJ(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_INITOBJ, -1, token);
}
void ILCodeStream::EmitJMP(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_JMP, 0, token);
}
void ILCodeStream::EmitLDARG   (unsigned uArgIdx)
{
    WRAPPER_NO_CONTRACT;

    if (m_pOwner->m_fHasThis)
    {
        uArgIdx++;
    }
    Emit(CEE_LDARG, 1, uArgIdx);
}
void ILCodeStream::EmitLDARGA  (unsigned uArgIdx)
{
    WRAPPER_NO_CONTRACT;
    if (m_pOwner->m_fHasThis)
    {
        uArgIdx++;
    }
    Emit(CEE_LDARGA, 1, uArgIdx);
}
void ILCodeStream::EmitLDC(DWORD_PTR uConst)
{
    WRAPPER_NO_CONTRACT;
    Emit(
#ifdef TARGET_64BIT
        CEE_LDC_I8
#else
        CEE_LDC_I4
#endif
        , 1, uConst);
}
void ILCodeStream::EmitLDC_R4(UINT32 uConst)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDC_R4, 1, uConst);
}
void ILCodeStream::EmitLDC_R8(UINT64 uConst)
{
    STANDARD_VM_CONTRACT;
#ifndef TARGET_64BIT  // We don't have room on 32-bit platforms to stor the CLR_NAN_64 value, so
                // we use a special value to represent CLR_NAN_64 and then recreate it later.
    CONSISTENCY_CHECK(((UINT32)uConst) != SPECIAL_VALUE_NAN_64_ON_32);
    if (uConst == CLR_NAN_64)
        uConst = SPECIAL_VALUE_NAN_64_ON_32;
    else
        CONSISTENCY_CHECK(FitsInU4(uConst));
#endif // TARGET_64BIT
    Emit(CEE_LDC_R8, 1, (UINT_PTR)uConst);
}
void ILCodeStream::EmitLDELEMA(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDELEMA, -1, token);
}
void ILCodeStream::EmitLDELEM_REF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDELEM_REF, -1, 0);
}
void ILCodeStream::EmitLDFLD(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDFLD, 0, token);
}
void ILCodeStream::EmitLDFLDA(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDFLDA, 0, token);
}
void ILCodeStream::EmitLDFTN(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDFTN, 1, token);
}
void ILCodeStream::EmitLDIND_I()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_I, 0, 0);
}
void ILCodeStream::EmitLDIND_I1()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_I1, 0, 0);
}
void ILCodeStream::EmitLDIND_I2()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_I2, 0, 0);
}
void ILCodeStream::EmitLDIND_I4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_I4, 0, 0);
}
void ILCodeStream::EmitLDIND_I8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_I8, 0, 0);
}
void ILCodeStream::EmitLDIND_R4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_R4, 0, 0);
}
void ILCodeStream::EmitLDIND_R8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_R8, 0, 0);
}
void ILCodeStream::EmitLDIND_REF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_REF, 0, 0);
}
void ILCodeStream::EmitLDIND_T(LocalDesc* pType)
{
    CONTRACTL
    {
        PRECONDITION(pType->cbType >= 1);
    }
    CONTRACTL_END;

    CorElementType elementType = ELEMENT_TYPE_END;

    bool onlyFoundModifiers = true;
    for(size_t i = 0; i < pType->cbType && onlyFoundModifiers; i++)
    {
        elementType = (CorElementType)pType->ElementType[i];
        onlyFoundModifiers = (elementType == ELEMENT_TYPE_PINNED);
    }


    switch (elementType)
    {
        case ELEMENT_TYPE_I1:       EmitLDIND_I1(); break;
        case ELEMENT_TYPE_BOOLEAN:  // fall through
        case ELEMENT_TYPE_U1:       EmitLDIND_U1(); break;
        case ELEMENT_TYPE_I2:       EmitLDIND_I2(); break;
        case ELEMENT_TYPE_CHAR:     // fall through
        case ELEMENT_TYPE_U2:       EmitLDIND_U2(); break;
        case ELEMENT_TYPE_I4:       EmitLDIND_I4(); break;
        case ELEMENT_TYPE_U4:       EmitLDIND_U4(); break;
        case ELEMENT_TYPE_I8:       EmitLDIND_I8(); break;
        case ELEMENT_TYPE_U8:       EmitLDIND_I8(); break;
        case ELEMENT_TYPE_R4:       EmitLDIND_R4(); break;
        case ELEMENT_TYPE_R8:       EmitLDIND_R8(); break;
        case ELEMENT_TYPE_PTR:      // same as ELEMENT_TYPE_I
        case ELEMENT_TYPE_FNPTR:    // same as ELEMENT_TYPE_I
        case ELEMENT_TYPE_I:        EmitLDIND_I();  break;
        case ELEMENT_TYPE_U:        EmitLDIND_I();  break;
        case ELEMENT_TYPE_STRING:   // fall through
        case ELEMENT_TYPE_CLASS:    // fall through
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_OBJECT:   EmitLDIND_REF(); break;

        case ELEMENT_TYPE_INTERNAL:
        {
            if (pType->InternalToken.GetMethodTable()->IsValueType())
            {
                EmitLDOBJ(m_pOwner->GetToken(pType->InternalToken.GetMethodTable()));
            }
            else
            {
                EmitLDIND_REF();
            }
            break;
        }

        default:
            UNREACHABLE_MSG("unexpected type passed to EmitLDIND_T");
            break;
    }
}
void ILCodeStream::EmitLDIND_U1()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_U1, 0, 0);
}
void ILCodeStream::EmitLDIND_U2()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_U2, 0, 0);
}
void ILCodeStream::EmitLDIND_U4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDIND_U4, 0, 0);
}
void ILCodeStream::EmitLDLEN()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDLEN, 0, 0);
}
void ILCodeStream::EmitLDLOC   (DWORD dwLocalNum)
{
    STANDARD_VM_CONTRACT;
    CONSISTENCY_CHECK(dwLocalNum != (DWORD)-1);
    CONSISTENCY_CHECK(dwLocalNum != (WORD)-1);
    Emit(CEE_LDLOC, 1, dwLocalNum);
}
void ILCodeStream::EmitLDLOCA  (DWORD dwLocalNum)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDLOCA, 1, dwLocalNum);
}
void ILCodeStream::EmitLDNULL()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDNULL, 1, 0);
}
void ILCodeStream::EmitLDOBJ   (int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDOBJ, 0, token);
}
void ILCodeStream::EmitLDSFLD(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDSFLD, 1, token);
}
void ILCodeStream::EmitLDSFLDA(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDSFLDA, 1, token);
}
void ILCodeStream::EmitLDTOKEN(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LDTOKEN, 1, token);
}
void ILCodeStream::EmitLEAVE(ILCodeLabel* pCodeLabel)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LEAVE, 0, (UINT_PTR)pCodeLabel);
}
void ILCodeStream::EmitLOCALLOC()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_LOCALLOC, 0, 0);
}
void ILCodeStream::EmitMUL()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_MUL, -1, 0);
}
void ILCodeStream::EmitMUL_OVF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_MUL_OVF, -1, 0);
}
void ILCodeStream::EmitNEWOBJ(int token, int numInArgs)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_NEWOBJ, (INT16)(1 - numInArgs), token);
}

void ILCodeStream::EmitNOP(LPCSTR pszNopComment)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_NOP, 0, (UINT_PTR)pszNopComment);
}

void ILCodeStream::EmitPOP()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_POP, -1, 0);
}
void ILCodeStream::EmitRET()
{
    WRAPPER_NO_CONTRACT;
    INT16 iStackDelta = m_pOwner->ReturnOpcodePopsStack() ? -1 : 0;
    Emit(CEE_RET, iStackDelta, 0);
}
void ILCodeStream::EmitSHR_UN()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_SHR_UN, -1, 0);
}
void ILCodeStream::EmitSTARG(unsigned uArgIdx)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STARG, -1, uArgIdx);
}
void ILCodeStream::EmitSTELEM_REF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STELEM_REF, -3, 0);
}
void ILCodeStream::EmitSTIND_I()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_I, -2, 0);
}
void ILCodeStream::EmitSTIND_I1()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_I1, -2, 0);
}
void ILCodeStream::EmitSTIND_I2()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_I2, -2, 0);
}
void ILCodeStream::EmitSTIND_I4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_I4, -2, 0);
}
void ILCodeStream::EmitSTIND_I8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_I8, -2, 0);
}
void ILCodeStream::EmitSTIND_R4()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_R4, -2, 0);
}
void ILCodeStream::EmitSTIND_R8()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_R8, -2, 0);
}
void ILCodeStream::EmitSTIND_REF()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STIND_REF, -2, 0);
}
void ILCodeStream::EmitSTIND_T(LocalDesc* pType)
{
    CONTRACTL
    {
        PRECONDITION(pType->cbType >= 1);
    }
    CONTRACTL_END;

    CorElementType elementType = ELEMENT_TYPE_END;

    bool onlyFoundModifiers = true;
    for(size_t i = 0; i < pType->cbType && onlyFoundModifiers; i++)
    {
        elementType = (CorElementType)pType->ElementType[i];
        onlyFoundModifiers = (elementType == ELEMENT_TYPE_PINNED);
    }
    switch (pType->ElementType[0])
    {
        case ELEMENT_TYPE_I1:       EmitSTIND_I1(); break;
        case ELEMENT_TYPE_BOOLEAN:  // fall through
        case ELEMENT_TYPE_U1:       EmitSTIND_I1(); break;
        case ELEMENT_TYPE_I2:       EmitSTIND_I2(); break;
        case ELEMENT_TYPE_CHAR:     // fall through
        case ELEMENT_TYPE_U2:       EmitSTIND_I2(); break;
        case ELEMENT_TYPE_I4:       EmitSTIND_I4(); break;
        case ELEMENT_TYPE_U4:       EmitSTIND_I4(); break;
        case ELEMENT_TYPE_I8:       EmitSTIND_I8(); break;
        case ELEMENT_TYPE_U8:       EmitSTIND_I8(); break;
        case ELEMENT_TYPE_R4:       EmitSTIND_R4(); break;
        case ELEMENT_TYPE_R8:       EmitSTIND_R8(); break;
        case ELEMENT_TYPE_PTR:      // same as ELEMENT_TYPE_I
        case ELEMENT_TYPE_FNPTR:    // same as ELEMENT_TYPE_I
        case ELEMENT_TYPE_I:        EmitSTIND_I();  break;
        case ELEMENT_TYPE_U:        EmitSTIND_I();  break;
        case ELEMENT_TYPE_STRING:   // fall through
        case ELEMENT_TYPE_CLASS:   // fall through
        case ELEMENT_TYPE_ARRAY:
        case ELEMENT_TYPE_SZARRAY:
        case ELEMENT_TYPE_OBJECT:   EmitSTIND_REF(); break;

        case ELEMENT_TYPE_INTERNAL:
        {
            if (pType->InternalToken.GetMethodTable()->IsValueType())
            {
                EmitSTOBJ(m_pOwner->GetToken(pType->InternalToken.GetMethodTable()));
            }
            else
            {
                EmitSTIND_REF();
            }
            break;
        }

        default:
            UNREACHABLE_MSG("unexpected type passed to EmitSTIND_T");
            break;
    }
}
void ILCodeStream::EmitSTFLD(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STFLD, -2, token);
}
void ILCodeStream::EmitSTLOC(DWORD dwLocalNum)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STLOC, -1, dwLocalNum);
}
void ILCodeStream::EmitSTOBJ(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STOBJ, -2, token);
}
void ILCodeStream::EmitSTSFLD(int token)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_STSFLD, -1, token);
}
void ILCodeStream::EmitSUB()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_SUB, -1, 0);
}
void ILCodeStream::EmitTHROW()
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_THROW, -1, 0);
}

void ILCodeStream::EmitUNALIGNED(BYTE alignment)
{
    WRAPPER_NO_CONTRACT;
    Emit(CEE_UNALIGNED, 0, alignment);
}


void ILCodeStream::EmitNEWOBJ(BinderMethodID id, int numInArgs)
{
    STANDARD_VM_CONTRACT;
    EmitNEWOBJ(GetToken(CoreLibBinder::GetMethod(id)), numInArgs);
}

void ILCodeStream::EmitCALL(BinderMethodID id, int numInArgs, int numRetArgs)
{
    STANDARD_VM_CONTRACT;
    EmitCALL(GetToken(CoreLibBinder::GetMethod(id)), numInArgs, numRetArgs);
}
void ILCodeStream::EmitLDFLD(BinderFieldID id)
{
    STANDARD_VM_CONTRACT;
    EmitLDFLD(GetToken(CoreLibBinder::GetField(id)));
}
void ILCodeStream::EmitSTFLD(BinderFieldID id)
{
    STANDARD_VM_CONTRACT;
    EmitSTFLD(GetToken(CoreLibBinder::GetField(id)));
}
void ILCodeStream::EmitLDFLDA(BinderFieldID id)
{
    STANDARD_VM_CONTRACT;
    EmitLDFLDA(GetToken(CoreLibBinder::GetField(id)));
}

void ILStubLinker::SetHasThis (bool fHasThis)
{
    LIMITED_METHOD_CONTRACT;
    m_fHasThis = fHasThis;
}

void ILCodeStream::EmitLoadThis ()
{
    WRAPPER_NO_CONTRACT;
    _ASSERTE(m_pOwner->m_fHasThis);
    // OK, this is ugly, but we add 1 to all LDARGs when
    // m_fHasThis is true, so we compensate for that here
    // so that we don't have to have a special method to
    // load arguments.
    EmitLDARG((unsigned)-1);
}

void ILCodeStream::EmitLoadNullPtr()
{
    WRAPPER_NO_CONTRACT;

    // This is the correct way to load unmanaged zero pointer. EmitLDC(0) alone works
    // fine in most cases but may lead to wrong code being generated on 64-bit if the
    // flow graph is complex.
    EmitLDC(0);
    EmitCONV_I();
}

void ILCodeStream::EmitArgIteratorCreateAndLoad()
{
    STANDARD_VM_CONTRACT;

    //
    // we insert the ArgIterator in the same spot that the VASigCookie will go for sanity
    //
    LocalDesc   aiLoc(CoreLibBinder::GetClass(CLASS__ARG_ITERATOR));
    int         aiLocNum;

    aiLocNum = NewLocal(aiLoc);

    EmitLDLOCA(aiLocNum);
    EmitDUP();
    EmitARGLIST();
    EmitLoadNullPtr();
    EmitCALL(METHOD__ARG_ITERATOR__CTOR2, 2, 0);

    aiLoc.ElementType[0]    = ELEMENT_TYPE_BYREF;
    aiLoc.ElementType[1]    = ELEMENT_TYPE_INTERNAL;
    aiLoc.cbType            = 2;
    aiLoc.InternalToken     = CoreLibBinder::GetClass(CLASS__ARG_ITERATOR);

    SetStubTargetArgType(&aiLoc, false);
}

DWORD ILStubLinker::NewLocal(CorElementType typ)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END;

    LocalDesc locDesc(typ);
    return NewLocal(locDesc);
}

StubSigBuilder::StubSigBuilder() :
    m_nItems(0),
    m_cbSig(0)
{
    STANDARD_VM_CONTRACT;

    m_pbSigCursor  = (BYTE*) m_qbSigBuffer.AllocThrows(INITIAL_BUFFER_SIZE);
}

void StubSigBuilder::EnsureEnoughQuickBytes(size_t cbToAppend)
{
    STANDARD_VM_CONTRACT;

    SIZE_T cbBuffer = m_qbSigBuffer.Size();
    if ((m_cbSig + cbToAppend) >= cbBuffer)
    {
        m_qbSigBuffer.ReSizeThrows(2 * cbBuffer);
        m_pbSigCursor = ((BYTE*)m_qbSigBuffer.Ptr()) + m_cbSig;
    }
}

DWORD StubSigBuilder::Append(LocalDesc* pLoc)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        INJECT_FAULT(COMPlusThrowOM());
        PRECONDITION(CheckPointer(pLoc));
    }
    CONTRACTL_END;

    EnsureEnoughQuickBytes(pLoc->cbType + sizeof(TypeHandle));

    memcpyNoGCRefs(m_pbSigCursor, pLoc->ElementType, pLoc->cbType);
    m_pbSigCursor   += pLoc->cbType;
    m_cbSig         += pLoc->cbType;

    size_t i = 0;

    while (i < pLoc->cbType)
    {
        CONSISTENCY_CHECK(   ELEMENT_TYPE_CLASS     != pLoc->ElementType[i]
                          && ELEMENT_TYPE_VALUETYPE != pLoc->ElementType[i]);

        switch (pLoc->ElementType[i])
        {
            case ELEMENT_TYPE_INTERNAL:
                SET_UNALIGNED_PTR(m_pbSigCursor, (UINT_PTR)pLoc->InternalToken.AsPtr());
                m_pbSigCursor   += sizeof(TypeHandle);
                m_cbSig         += sizeof(TypeHandle);
                break;

            case ELEMENT_TYPE_FNPTR:
                {
                    SigPointer  ptr(pLoc->pSig);

                    SigBuilder sigBuilder;
                    ptr.ConvertToInternalSignature(pLoc->pSigModule, NULL, &sigBuilder);

                    DWORD cbFnPtrSig;
                    PVOID pFnPtrSig = sigBuilder.GetSignature(&cbFnPtrSig);

                    EnsureEnoughQuickBytes(cbFnPtrSig);

                    memcpyNoGCRefs(m_pbSigCursor, pFnPtrSig, cbFnPtrSig);

                    m_pbSigCursor += cbFnPtrSig;
                    m_cbSig       += cbFnPtrSig;
                }
                break;

            default:
                break;
        }

        i++;
    }

    if (pLoc->ElementType[0] == ELEMENT_TYPE_ARRAY)
    {
        EnsureEnoughQuickBytes(pLoc->cbArrayBoundsInfo);

        memcpyNoGCRefs(m_pbSigCursor, pLoc->pSig, pLoc->cbArrayBoundsInfo);
        m_pbSigCursor   += pLoc->cbArrayBoundsInfo;
        m_cbSig         += pLoc->cbArrayBoundsInfo;
    }

    _ASSERTE(m_cbSig <= m_qbSigBuffer.Size());  // we corrupted our buffer resizing if this assert fires

    return m_nItems++;
}

//---------------------------------------------------------------------------------------
//
DWORD
LocalSigBuilder::GetSigSize()
{
    STANDARD_VM_CONTRACT;

    BYTE   temp[4];
    UINT32 cbEncoded   = CorSigCompressData(m_nItems, temp);

    S_UINT32 cbSigSize =
        S_UINT32(1) +           // IMAGE_CEE_CS_CALLCONV_LOCAL_SIG
        S_UINT32(cbEncoded) +   // encoded number of locals
        S_UINT32(m_cbSig) +     // types
        S_UINT32(1);            // ELEMENT_TYPE_END
    if (cbSigSize.IsOverflow())
    {
        IfFailThrow(COR_E_OVERFLOW);
    }
    return cbSigSize.Value();
}

//---------------------------------------------------------------------------------------
//
DWORD
LocalSigBuilder::GetSig(
    BYTE * pbLocalSig,
    DWORD  cbBuffer)
{
    STANDARD_VM_CONTRACT;
    BYTE    temp[4];
    size_t  cb = CorSigCompressData(m_nItems, temp);

    _ASSERTE((1 + cb + m_cbSig + 1) == GetSigSize());

    if ((1 + cb + m_cbSig + 1) <= cbBuffer)
    {
        pbLocalSig[0] = IMAGE_CEE_CS_CALLCONV_LOCAL_SIG;
        memcpyNoGCRefs(&pbLocalSig[1],      temp,                cb);
        memcpyNoGCRefs(&pbLocalSig[1 + cb], m_qbSigBuffer.Ptr(), m_cbSig);
        pbLocalSig[1 + cb + m_cbSig] = ELEMENT_TYPE_END;
        return (DWORD)(1 + cb + m_cbSig + 1);
    }
    else
    {
        return NULL;
    }
}

#ifndef DACCESS_COMPILE

FunctionSigBuilder::FunctionSigBuilder() :
    m_callingConv(IMAGE_CEE_CS_CALLCONV_DEFAULT)
{
    STANDARD_VM_CONTRACT;
    m_qbCallConvModOpts.Init();
    m_qbReturnSig.ReSizeThrows(1);
    *(CorElementType *)m_qbReturnSig.Ptr() = ELEMENT_TYPE_VOID;
}


void FunctionSigBuilder::SetReturnType(LocalDesc* pLoc)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(pLoc->cbType > 0);
    }
    CONTRACTL_END;

    m_qbReturnSig.ReSizeThrows(pLoc->cbType);
    memcpyNoGCRefs((BYTE*)m_qbReturnSig.Ptr(), pLoc->ElementType, pLoc->cbType);

    size_t i = 0;

    while (i < pLoc->cbType)
    {
        CONSISTENCY_CHECK(   ELEMENT_TYPE_CLASS     != pLoc->ElementType[i]
                          && ELEMENT_TYPE_VALUETYPE != pLoc->ElementType[i]);

        switch (pLoc->ElementType[i])
        {
            case ELEMENT_TYPE_INTERNAL:
                m_qbReturnSig.ReSizeThrows(m_qbReturnSig.Size() + sizeof(TypeHandle));
                SET_UNALIGNED_PTR((BYTE *)m_qbReturnSig.Ptr() + m_qbReturnSig.Size() - + sizeof(TypeHandle), (UINT_PTR)pLoc->InternalToken.AsPtr());
                break;

            case ELEMENT_TYPE_FNPTR:
                {
                    SigPointer  ptr(pLoc->pSig);

                    SigBuilder sigBuilder;
                    ptr.ConvertToInternalSignature(pLoc->pSigModule, NULL, &sigBuilder);

                    DWORD cbFnPtrSig;
                    PVOID pFnPtrSig = sigBuilder.GetSignature(&cbFnPtrSig);

                    m_qbReturnSig.ReSizeThrows(m_qbReturnSig.Size() + cbFnPtrSig);

                    memcpyNoGCRefs((BYTE *)m_qbReturnSig.Ptr() + m_qbReturnSig.Size() - cbFnPtrSig, pFnPtrSig, cbFnPtrSig);
                }
                break;

            default:
                break;
        }

        i++;
    }

    if (pLoc->ElementType[0] == ELEMENT_TYPE_ARRAY)
    {
        SIZE_T size = m_qbReturnSig.Size();
        m_qbReturnSig.ReSizeThrows(size + pLoc->cbArrayBoundsInfo);
        memcpyNoGCRefs((BYTE *)m_qbReturnSig.Ptr() + size, pLoc->pSig, pLoc->cbArrayBoundsInfo);
    }
}

void FunctionSigBuilder::SetSig(PCCOR_SIGNATURE pSig, DWORD cSig)
{
    STANDARD_VM_CONTRACT;

    // parse the incoming signature
    SigPointer sigPtr(pSig, cSig);

    // 1) calling convention
    uint32_t callConv;
    IfFailThrow(sigPtr.GetCallingConvInfo(&callConv));
    SetCallingConv((CorCallingConvention)callConv);

    // 2) number of parameters
    IfFailThrow(sigPtr.GetData(&m_nItems));

    // 3) return type
    PCCOR_SIGNATURE ptr = sigPtr.GetPtr();
    IfFailThrow(sigPtr.SkipExactlyOne());

    size_t retSigLength = sigPtr.GetPtr() - ptr;

    m_qbReturnSig.ReSizeThrows(retSigLength);
    memcpyNoGCRefs(m_qbReturnSig.Ptr(), ptr, retSigLength);

    // 4) parameters
    m_cbSig = 0;

    size_t cbSigLen = (cSig - (sigPtr.GetPtr() - pSig));

    m_pbSigCursor = (BYTE *)m_qbSigBuffer.Ptr();
    EnsureEnoughQuickBytes(cbSigLen);

    memcpyNoGCRefs(m_pbSigCursor, sigPtr.GetPtr(), cbSigLen);

    m_cbSig = cbSigLen;
    m_pbSigCursor += cbSigLen;
}

//--------------------------------------------------------------------------------------
//

void
FunctionSigBuilder::AddCallConvModOpt(mdToken token)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(TypeFromToken(token) == mdtTypeDef || TypeFromToken(token) == mdtTypeRef);

    int temp;
    ULONG len = CorSigCompressToken(token, &temp);
    m_qbCallConvModOpts.ReSizeThrows(m_qbCallConvModOpts.Size() + 1 + len);
    SET_UNALIGNED_PTR((BYTE*)m_qbCallConvModOpts.Ptr() + m_qbCallConvModOpts.Size() - len - 1, (BYTE)ELEMENT_TYPE_CMOD_OPT);
    memcpyNoGCRefs((BYTE*)m_qbCallConvModOpts.Ptr() + m_qbCallConvModOpts.Size() - len, &temp, len);
}

//---------------------------------------------------------------------------------------
//
DWORD
FunctionSigBuilder::GetSigSize()
{
    STANDARD_VM_CONTRACT;

    BYTE   temp[4];
    DWORD  cbEncodedLen     = CorSigCompressData(m_nItems, temp);
    SIZE_T cbEncodedRetType = m_qbReturnSig.Size();

    SIZE_T cbEncodedCallConv = m_qbCallConvModOpts.Size();

    CONSISTENCY_CHECK(cbEncodedRetType > 0);

    S_UINT32 cbSigSize =
        S_UINT32(1) +                   // calling convention
        S_UINT32(cbEncodedLen) +        // encoded number of args
        S_UINT32(cbEncodedCallConv) +   // encoded callconv modopts
        S_UINT32(cbEncodedRetType) +    // encoded return type
        S_UINT32(m_cbSig) +             // types
        S_UINT32(1);                    // ELEMENT_TYPE_END
    if (cbSigSize.IsOverflow())
    {
        IfFailThrow(COR_E_OVERFLOW);
    }
    return cbSigSize.Value();
}

//---------------------------------------------------------------------------------------
//
DWORD
FunctionSigBuilder::GetSig(
    BYTE * pbLocalSig,
    DWORD  cbBuffer)
{
    STANDARD_VM_CONTRACT;
    BYTE    tempLen[4];
    size_t  cbEncodedLen      = CorSigCompressData(m_nItems, tempLen);
    size_t  cbEncodedCallConv = m_qbCallConvModOpts.Size();
    size_t  cbEncodedRetType  = m_qbReturnSig.Size();

    CONSISTENCY_CHECK(cbEncodedRetType > 0);

    _ASSERTE((1 + cbEncodedLen + cbEncodedCallConv + cbEncodedRetType + m_cbSig + 1) == GetSigSize());

    if ((1 + cbEncodedLen + cbEncodedRetType + m_cbSig + 1) <= cbBuffer)
    {
        BYTE* pbCursor = pbLocalSig;
        *pbCursor = static_cast<BYTE>(m_callingConv);
        pbCursor++;

        memcpyNoGCRefs(pbCursor, tempLen, cbEncodedLen);
        pbCursor += cbEncodedLen;

        memcpyNoGCRefs(pbCursor, m_qbCallConvModOpts.Ptr(), m_qbCallConvModOpts.Size());
        pbCursor += m_qbCallConvModOpts.Size();

        memcpyNoGCRefs(pbCursor, m_qbReturnSig.Ptr(), m_qbReturnSig.Size());
        pbCursor += m_qbReturnSig.Size();

        memcpyNoGCRefs(pbCursor, m_qbSigBuffer.Ptr(), m_cbSig);
        pbCursor += m_cbSig;
        pbCursor[0] = ELEMENT_TYPE_END;
        return (DWORD)(1 + cbEncodedLen + cbEncodedRetType + m_cbSig + 1);
    }
    else
    {
        return NULL;
    }
}

DWORD ILStubLinker::NewLocal(LocalDesc loc)
{
    WRAPPER_NO_CONTRACT;

    return m_localSigBuilder.NewLocal(&loc);
}

//---------------------------------------------------------------------------------------
//
DWORD
ILStubLinker::GetLocalSigSize()
{
    LIMITED_METHOD_CONTRACT;

    return m_localSigBuilder.GetSigSize();
}

//---------------------------------------------------------------------------------------
//
DWORD
ILStubLinker::GetLocalSig(
    BYTE * pbLocalSig,
    DWORD  cbBuffer)
{
    STANDARD_VM_CONTRACT;

    DWORD dwRet = m_localSigBuilder.GetSig(pbLocalSig, cbBuffer);
    return dwRet;
}

//---------------------------------------------------------------------------------------
//
DWORD
ILStubLinker::GetStubTargetMethodSigSize()
{
    STANDARD_VM_CONTRACT;

    return m_nativeFnSigBuilder.GetSigSize();
}

//---------------------------------------------------------------------------------------
//
DWORD
ILStubLinker::GetStubTargetMethodSig(
    BYTE * pbSig,
    DWORD  cbSig)
{
    LIMITED_METHOD_CONTRACT;

    DWORD dwRet = m_nativeFnSigBuilder.GetSig(pbSig, cbSig);
    return dwRet;
}

void ILStubLinker::SetStubTargetMethodSig(PCCOR_SIGNATURE pSig, DWORD cSig)
{
    STANDARD_VM_CONTRACT;

    m_nativeFnSigBuilder.SetSig(pSig, cSig);
}

#endif // DACCESS_COMPILE

static BOOL SigHasVoidReturnType(const Signature &signature)
{
    CONTRACTL
    {
        THROWS;
        GC_NOTRIGGER;
    }
    CONTRACTL_END

    SigPointer ptr = signature.CreateSigPointer();

    uint32_t data;
    IfFailThrow(ptr.GetCallingConvInfo(&data));
    // Skip number of type arguments
    if (data & IMAGE_CEE_CS_CALLCONV_GENERIC)
    {
        IfFailThrow(ptr.GetData(NULL));
    }

    // skip number of args
    IfFailThrow(ptr.GetData(NULL));

    CorElementType retType;
    IfFailThrow(ptr.PeekElemType(&retType));

    return (ELEMENT_TYPE_VOID == retType);
}

static PTR_MethodTable GetModOptTypeForCallConv(CorUnmanagedCallingConvention callConv)
{
    switch(callConv)
    {
        case IMAGE_CEE_UNMANAGED_CALLCONV_C:
            return CoreLibBinder::GetClass(CLASS__CALLCONV_CDECL);
            break;
        case IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL:
            return CoreLibBinder::GetClass(CLASS__CALLCONV_STDCALL);
            break;
        case IMAGE_CEE_UNMANAGED_CALLCONV_THISCALL:
            return CoreLibBinder::GetClass(CLASS__CALLCONV_THISCALL);
            break;
        case IMAGE_CEE_UNMANAGED_CALLCONV_FASTCALL:
            return CoreLibBinder::GetClass(CLASS__CALLCONV_FASTCALL);
            break;
        default:
            return NULL;
    }
}

ILStubLinker::ILStubLinker(Module* pStubSigModule, const Signature &signature, SigTypeContext *pTypeContext, MethodDesc *pMD, ILStubLinkerFlags flags) :
    m_pCodeStreamList(NULL),
    m_stubSig(signature),
    m_pTypeContext(pTypeContext),
    m_pCode(NULL),
    m_pStubSigModule(pStubSigModule),
    m_pLabelList(NULL),
    m_StubHasVoidReturnType(FALSE),
    m_fIsReverseStub((flags & ILSTUB_LINKER_FLAG_REVERSE) != 0),
    m_iTargetStackDelta(0),
    m_cbCurrentCompressedSigLen(1),
    m_nLocals(0),
    m_fHasThis(false),
    m_pMD(pMD)
{
    CONTRACTL
    {
        THROWS;
        GC_TRIGGERS;
        INJECT_FAULT(COMPlusThrowOM());
    }
    CONTRACTL_END

    m_managedSigPtr = signature.CreateSigPointer();
    if ((flags & ILSTUB_LINKER_FLAG_SUPPRESSGCTRANSITION) != 0)
    {
        m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_SUPPRESSGCTRANSITION)));
        m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_UNMANAGED);
    }
    if (!signature.IsEmpty())
    {
        // Until told otherwise, assume that the stub has the same return type as the signature.
        m_StubHasVoidReturnType = SigHasVoidReturnType(signature);
        m_StubTargetHasVoidReturnType = m_StubHasVoidReturnType;

        //
        // Get the stub's calling convention.  Set m_fHasThis to match
        // IMAGE_CEE_CS_CALLCONV_HASTHIS.
        //

        uint32_t   uStubCallingConvInfo;
        IfFailThrow(m_managedSigPtr.GetCallingConvInfo(&uStubCallingConvInfo));

        m_fHasThis = (flags & ILSTUB_LINKER_FLAG_STUB_HAS_THIS) != 0;

        //
        // If target calling convention was specified, use it instead.
        // Otherwise, derive one based on the stub's signature.
        //

        uint32_t   uCallingConvInfo = uStubCallingConvInfo;

        uint32_t   uCallingConv    = (uCallingConvInfo & IMAGE_CEE_CS_CALLCONV_MASK);
        uint32_t   uNativeCallingConv;

        if (IMAGE_CEE_CS_CALLCONV_VARARG == uCallingConv)
        {
            //
            // If we have a PInvoke stub that has a VARARG calling convention
            // we will transition to a NATIVEVARARG calling convention for the
            // target call. The JIT64 knows about this calling convention,
            // basically it is the same as the managed vararg calling convention
            // except without a VASigCookie.
            //
            // If our stub is not a PInvoke stub and has a vararg calling convention,
            // we are most likely going to have to forward those variable arguments
            // on to our call target.  Unfortunately, callsites to varargs methods
            // in IL always have full signatures (that's where the VASigCookie comes
            // from). But we don't have that in this case, so we play some tricks and
            // pass an ArgIterator down to an assembly routine that pulls out the
            // variable arguments and puts them in the right spot before forwarding
            // to the stub target.
            //
            // The net result is that we don't want to set the native calling
            // convention to be vararg for non-PInvoke stubs, so we just use
            // the default callconv.
            //
            if ((flags & ILSTUB_LINKER_FLAG_NDIRECT) == 0)
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_DEFAULT;
            else
                uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_NATIVEVARARG;
        }
        else
        {
            uNativeCallingConv = IMAGE_CEE_CS_CALLCONV_DEFAULT;
        }

        if ((flags & (ILSTUB_LINKER_FLAG_TARGET_HAS_THIS | ILSTUB_LINKER_FLAG_NDIRECT)) == ILSTUB_LINKER_FLAG_TARGET_HAS_THIS)
        {
            // ndirect native sig never has a 'this' pointer
            uNativeCallingConv |= IMAGE_CEE_CS_CALLCONV_HASTHIS;
        }

        if ((flags & (ILSTUB_LINKER_FLAG_TARGET_HAS_THIS | ILSTUB_LINKER_FLAG_REVERSE)) == ILSTUB_LINKER_FLAG_TARGET_HAS_THIS)
        {
            m_iTargetStackDelta--;
        }

        if (m_nativeFnSigBuilder.GetCallingConv() == IMAGE_CEE_CS_CALLCONV_UNMANAGED)
        {
            switch((CorUnmanagedCallingConvention)uNativeCallingConv)
            {
                case IMAGE_CEE_UNMANAGED_CALLCONV_C:
                case IMAGE_CEE_UNMANAGED_CALLCONV_STDCALL:
                case IMAGE_CEE_UNMANAGED_CALLCONV_THISCALL:
                case IMAGE_CEE_UNMANAGED_CALLCONV_FASTCALL:
                    m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(GetModOptTypeForCallConv((CorUnmanagedCallingConvention)uNativeCallingConv)));
                    break;
                default:
                    // If the calling convention isn't one of the unmanaged calling conventions
                    // and we've already decided that we need to emit the Unmanaged calling convention in metadata,
                    // let the code that builds the stub set the calling convention.
                    break;
            }
        }
        else
        {
            m_nativeFnSigBuilder.SetCallingConv((CorCallingConvention)uNativeCallingConv);
        }

        if (uStubCallingConvInfo & IMAGE_CEE_CS_CALLCONV_GENERIC)
            IfFailThrow(m_managedSigPtr.GetData(NULL));    // skip number of type parameters

        uint32_t numParams = 0;
        IfFailThrow(m_managedSigPtr.GetData(&numParams));
        // If we are a reverse stub, then the target signature called in the stub
        // is the managed signature. In that case, we calculate the target IL stack delta
        // here from the managed signature.
        if ((flags & ILSTUB_LINKER_FLAG_REVERSE) != 0)
        {
            // As per ECMA 335, the max number of parameters is 0x1FFFFFFF (see section 11.23.2), which fits in a 32-bit signed integer
            // So this cast is safe.
            m_iTargetStackDelta -= (INT)numParams;
            if (!m_StubHasVoidReturnType)
            {
                m_iTargetStackDelta++;
            }
        }
        IfFailThrow(m_managedSigPtr.SkipExactlyOne()); // skip return type
    }
}

ILStubLinker::~ILStubLinker()
{
    CONTRACTL
    {
        NOTHROW;
        GC_TRIGGERS;
        MODE_ANY;
    }
    CONTRACTL_END;
    DeleteCodeLabels();
    DeleteCodeStreams();
}

void ILStubLinker::DeleteCodeLabels()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    //
    // walk the list of labels and free each one
    //
    ILCodeLabel* pCurrent = m_pLabelList;
    while (pCurrent)
    {
        ILCodeLabel* pDeleteMe = pCurrent;
        pCurrent = pCurrent->m_pNext;
        delete pDeleteMe;
    }
    m_pLabelList = NULL;
}

void ILStubLinker::DeleteCodeStreams()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    ILCodeStream* pCurrent = m_pCodeStreamList;
    while (pCurrent)
    {
        ILCodeStream* pDeleteMe = pCurrent;
        pCurrent = pCurrent->m_pNextStream;
        delete pDeleteMe;
    }
    m_pCodeStreamList = NULL;
}

void ILStubLinker::ClearCodeStreams()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    ILCodeStream* pCurrent = m_pCodeStreamList;
    while (pCurrent)
    {
        pCurrent->ClearCode();
        pCurrent = pCurrent->m_pNextStream;
    }
}

void ILStubLinker::GetStubReturnType(LocalDesc* pLoc)
{
    WRAPPER_NO_CONTRACT;

    GetStubReturnType(pLoc, m_pStubSigModule);
}

void ILStubLinker::GetStubReturnType(LocalDesc* pLoc, Module* pModule)
{
    STANDARD_VM_CONTRACT;
    SigPointer ptr = m_stubSig.CreateSigPointer();
    uint32_t uCallingConv;
    uint32_t nTypeArgs = 0;
    uint32_t nArgs;

    IfFailThrow(ptr.GetCallingConvInfo(&uCallingConv));

    if (uCallingConv & IMAGE_CEE_CS_CALLCONV_GENERIC)
        IfFailThrow(ptr.GetData(&nTypeArgs));

    IfFailThrow(ptr.GetData(&nArgs));

    GetManagedTypeHelper(pLoc, pModule, ptr.GetPtr(), m_pTypeContext, m_pMD);
}

CorCallingConvention ILStubLinker::GetStubTargetCallingConv()
{
    LIMITED_METHOD_CONTRACT;
    return m_nativeFnSigBuilder.GetCallingConv();
}

void ILStubLinker::TransformArgForJIT(LocalDesc *pLoc)
{
    STANDARD_VM_CONTRACT;
    // Turn everything into blittable primitives. The reason this method is needed are
    // byrefs which are OK only when they ref stack data or are pinned. This condition
    // cannot be verified by code:NDirect.MarshalingRequired so we explicitly get rid
    // of them here.
    switch (pLoc->ElementType[0])
    {
        // primitives
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
        case ELEMENT_TYPE_I:
        case ELEMENT_TYPE_U:
        {
            // no transformation needed
            break;
        }

        case ELEMENT_TYPE_VALUETYPE:
        {
            _ASSERTE(!"Should have been replaced by a native value type!");
            break;
        }

        case ELEMENT_TYPE_PTR:
        {
#ifdef TARGET_X86
            if (pLoc->bIsCopyConstructed)
            {
                // The only pointers that we don't transform to ELEMENT_TYPE_I are those that are
                // ET_TYPE_CMOD_REQD<IsCopyConstructed>/ET_TYPE_CMOD_REQD<NeedsCopyConstructorModifier>
                // in the original signature. This convention is understood by the UM thunk compiler
                // (code:UMThunkMarshInfo.CompileNExportThunk) which will generate different thunk code.
                // Such parameters come from unmanaged by value but must enter the IL stub by reference
                // because we are not supposed to make a copy.
            }
            else
#endif // TARGET_X86
            {
                pLoc->ElementType[0] = ELEMENT_TYPE_I;
                pLoc->cbType = 1;
            }
            break;
        }

        case ELEMENT_TYPE_INTERNAL:
        {
            // JIT will handle structures
            if (pLoc->InternalToken.IsValueType())
            {
                _ASSERTE(pLoc->InternalToken.IsNativeValueType() || !pLoc->InternalToken.GetMethodTable()->ContainsPointers());
                break;
            }
            FALLTHROUGH;
        }

        // pointers, byrefs, strings, arrays, other ref types -> ELEMENT_TYPE_I
        default:
        {
            pLoc->ElementType[0] = ELEMENT_TYPE_I;
            pLoc->cbType = 1;
            break;
        }
    }
}

Module *ILStubLinker::GetStubSigModule()
{
    LIMITED_METHOD_CONTRACT;
    return m_pStubSigModule;
}

SigTypeContext *ILStubLinker::GetStubSigTypeContext()
{
    LIMITED_METHOD_CONTRACT;
    return m_pTypeContext;
}

void ILStubLinker::SetStubTargetReturnType(CorElementType typ)
{
    WRAPPER_NO_CONTRACT;

    LocalDesc locDesc(typ);
    SetStubTargetReturnType(&locDesc);
}

void ILStubLinker::SetStubTargetReturnType(LocalDesc* pLoc)
{
    CONTRACTL
    {
        WRAPPER(NOTHROW);
        WRAPPER(GC_NOTRIGGER);
        WRAPPER(MODE_ANY);
        PRECONDITION(CheckPointer(pLoc, NULL_NOT_OK));
    }
    CONTRACTL_END;

    TransformArgForJIT(pLoc);

    m_nativeFnSigBuilder.SetReturnType(pLoc);

    if (!m_fIsReverseStub)
    {
        // Update check for if a stub has a void return type based on the provided return type.
        m_StubTargetHasVoidReturnType = ((1 == pLoc->cbType) && (ELEMENT_TYPE_VOID == pLoc->ElementType[0])) ? TRUE : FALSE;
        if (!m_StubTargetHasVoidReturnType)
        {
            m_iTargetStackDelta++;
        }
    }
}

DWORD ILStubLinker::SetStubTargetArgType(CorElementType typ, bool fConsumeStubArg /*= true*/)
{
    STANDARD_VM_CONTRACT;

    LocalDesc locDesc(typ);
    return SetStubTargetArgType(&locDesc, fConsumeStubArg);
}

void ILStubLinker::SetStubTargetCallingConv(CorCallingConvention uNativeCallingConv)
{
    LIMITED_METHOD_CONTRACT;
    CorCallingConvention originalCallingConvention = m_nativeFnSigBuilder.GetCallingConv();
    if (originalCallingConvention == IMAGE_CEE_CS_CALLCONV_UNMANAGED)
    {
        m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(GetModOptTypeForCallConv((CorUnmanagedCallingConvention)uNativeCallingConv)));
    }
    else
    {
        m_nativeFnSigBuilder.SetCallingConv(uNativeCallingConv);
    }

    if (!m_fIsReverseStub)
    {
        if ( (originalCallingConvention & CORINFO_CALLCONV_HASTHIS) && !(uNativeCallingConv & CORINFO_CALLCONV_HASTHIS))
        {
            // Our calling convention had an implied-this beforehand and now it doesn't.
            // Account for this in the target stack delta.
            m_iTargetStackDelta++;
        }
    }
}

void ILStubLinker::SetStubTargetCallingConv(CorInfoCallConvExtension callConv)
{
    LIMITED_METHOD_CONTRACT;
    _ASSERTE(callConv != CorInfoCallConvExtension::Managed);

    const CorCallingConvention originalCallingConvention = m_nativeFnSigBuilder.GetCallingConv();
    if (originalCallingConvention != IMAGE_CEE_CS_CALLCONV_UNMANAGED)
    {
        // For performance reasons, we try to encode the calling convention using
        // the flags-based method if possible before using modopts.
        switch (callConv)
        {
            case CorInfoCallConvExtension::C:
                m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_C);
                break;
            case CorInfoCallConvExtension::Stdcall:
                m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_STDCALL);
                break;
            case CorInfoCallConvExtension::Thiscall:
                m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_THISCALL);
                break;
            case CorInfoCallConvExtension::Fastcall:
                m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_FASTCALL);
                break;
            default:
                m_nativeFnSigBuilder.SetCallingConv(IMAGE_CEE_CS_CALLCONV_UNMANAGED);
                break;
        }
    }

    // We may have updated the calling convention above, so reread the calling convention
    // from the signature builder instead of using originalCallingConvention.
    if (m_nativeFnSigBuilder.GetCallingConv() == IMAGE_CEE_CS_CALLCONV_UNMANAGED)
    {
        // In this case we're already using the "Unmanaged" calling convention, so encode the specific callconv
        // with the requisite modopts for the calling convention.
        switch (callConv)
        {
            case CorInfoCallConvExtension::C:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_CDECL)));
                break;
            case CorInfoCallConvExtension::Stdcall:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_STDCALL)));
                break;
            case CorInfoCallConvExtension::Thiscall:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_THISCALL)));
                break;
            case CorInfoCallConvExtension::Fastcall:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_FASTCALL)));
                break;
            case CorInfoCallConvExtension::CMemberFunction:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_CDECL)));
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_MEMBERFUNCTION)));
                break;
            case CorInfoCallConvExtension::StdcallMemberFunction:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_STDCALL)));
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_MEMBERFUNCTION)));
                break;
            case CorInfoCallConvExtension::FastcallMemberFunction:
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_FASTCALL)));
                m_nativeFnSigBuilder.AddCallConvModOpt(GetToken(CoreLibBinder::GetClass(CLASS__CALLCONV_MEMBERFUNCTION)));
                break;
            default:
                _ASSERTE("Unknown calling convention. Unable to encode it in the native function pointer signature.");
                break;
        }
    }

    if (!m_fIsReverseStub)
    {
        if (originalCallingConvention & CORINFO_CALLCONV_HASTHIS)
        {
            // Our calling convention had an implied-this beforehand and now it doesn't.
            // Account for this in the target stack delta.
            m_iTargetStackDelta++;
        }
    }
}

static size_t GetManagedTypeForMDArray(LocalDesc* pLoc, Module* pModule, PCCOR_SIGNATURE psigManagedArg, SigTypeContext *pTypeContext)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pLoc));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(psigManagedArg));
        PRECONDITION(*psigManagedArg == ELEMENT_TYPE_ARRAY);
    }
    CONTRACTL_END;

    SigPointer      ptr;
    size_t          cbDest = 0;

    //
    // copy ELEMENT_TYPE_ARRAY
    //
    pLoc->ElementType[cbDest] = *psigManagedArg;
    psigManagedArg++;
    cbDest++;

    ptr.SetSig(psigManagedArg);

    IfFailThrow(ptr.SkipCustomModifiers());

    psigManagedArg = ptr.GetPtr();

    //
    // get array type
    //
    pLoc->InternalToken = ptr.GetTypeHandleThrowing(pModule, pTypeContext);

    pLoc->ElementType[cbDest] = ELEMENT_TYPE_INTERNAL;
    cbDest++;

    //
    // get array bounds
    //

    size_t          cbType;
    PCCOR_SIGNATURE psigNextManagedArg;

    // find the start of the next argument
    ptr.SetSig(psigManagedArg - 1);     // -1 to back up to E_T_ARRAY;
    IfFailThrow(ptr.SkipExactlyOne());

    psigNextManagedArg  = ptr.GetPtr();

    // find the start of the array bounds information
    ptr.SetSig(psigManagedArg);
    IfFailThrow(ptr.SkipExactlyOne());

    psigManagedArg = ptr.GetPtr();  // point to the array bounds info
    cbType = psigNextManagedArg - psigManagedArg;

    pLoc->pSig = psigManagedArg;        // point to the array bounds info
    pLoc->cbArrayBoundsInfo = cbType;   // size of array bounds info
    pLoc->cbType = cbDest;

    return cbDest;
}


// static
void ILStubLinker::GetManagedTypeHelper(LocalDesc* pLoc, Module* pModule, PCCOR_SIGNATURE psigManagedArg, SigTypeContext *pTypeContext, MethodDesc *pMD)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;

        PRECONDITION(CheckPointer(pLoc));
        PRECONDITION(CheckPointer(pModule));
        PRECONDITION(CheckPointer(psigManagedArg));
    }
    CONTRACTL_END;

    SigPointer      ptr(psigManagedArg);
    CorElementType  eType;
    LOG((LF_STUBS, LL_INFO10000, "GetManagedTypeHelper on type at %p\n", psigManagedArg));

    IfFailThrow(ptr.PeekElemType(&eType));

    size_t cbDest               = 0;

    while (eType == ELEMENT_TYPE_PTR ||
           eType == ELEMENT_TYPE_BYREF ||
           eType == ELEMENT_TYPE_SZARRAY)
    {
        pLoc->ElementType[cbDest] = static_cast<BYTE>(eType);
        cbDest++;

        if (cbDest >= LocalDesc::MAX_LOCALDESC_ELEMENTS)
        {
            COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);
        }

        IfFailThrow(ptr.GetElemType(NULL));
        IfFailThrow(ptr.PeekElemType(&eType));
    }

    SigPointer ptr2(ptr);
    IfFailThrow(ptr2.SkipCustomModifiers());
    psigManagedArg = ptr2.GetPtr();

    switch (eType)
    {
        case ELEMENT_TYPE_VAR:
        case ELEMENT_TYPE_MVAR:
            {
                IfFailThrow(ptr.GetElemType(NULL)); // skip ET
                uint32_t varNum;
                IfFailThrowBF(ptr.GetData(&varNum), BFA_BAD_COMPLUS_SIG, pModule);

                DWORD varCount = (eType == ELEMENT_TYPE_VAR ? pTypeContext->m_classInst.GetNumArgs() :
                                                              pTypeContext->m_methodInst.GetNumArgs());
                THROW_BAD_FORMAT_MAYBE(varNum < varCount, BFA_BAD_COMPLUS_SIG, pModule);

                pLoc->InternalToken = (eType == ELEMENT_TYPE_VAR ? pTypeContext->m_classInst[varNum] :
                                                                   pTypeContext->m_methodInst[varNum]);

                pLoc->ElementType[cbDest] = ELEMENT_TYPE_INTERNAL;
                cbDest++;
                break;
            }

        case ELEMENT_TYPE_CLASS:
        case ELEMENT_TYPE_VALUETYPE:
        case ELEMENT_TYPE_INTERNAL:
            {
                pLoc->InternalToken = ptr.GetTypeHandleThrowing(pModule, pTypeContext);

                pLoc->ElementType[cbDest] = ELEMENT_TYPE_INTERNAL;
                cbDest++;
                break;
            }

        case ELEMENT_TYPE_GENERICINST:
            {
                pLoc->InternalToken = ptr.GetTypeHandleThrowing(pModule, pTypeContext);

                pLoc->ElementType[cbDest] = ELEMENT_TYPE_INTERNAL;
                cbDest++;
                break;
            }

        case ELEMENT_TYPE_FNPTR:
            // save off a pointer to the managed sig
            // we'll convert it in bulk when we store it
            // in the generated sig
            pLoc->pSigModule = pModule;
            pLoc->pSig       = psigManagedArg+1;

            pLoc->ElementType[cbDest] = ELEMENT_TYPE_FNPTR;
            cbDest++;
            break;

        case ELEMENT_TYPE_ARRAY:
            cbDest = GetManagedTypeForMDArray(pLoc, pModule, psigManagedArg, pTypeContext);
            break;

        default:
            {
                size_t          cbType;
                PCCOR_SIGNATURE psigNextManagedArg;

                IfFailThrow(ptr.SkipExactlyOne());

                psigNextManagedArg  = ptr.GetPtr();
                cbType              = psigNextManagedArg - psigManagedArg;

                size_t cbNewDest;
                if (!ClrSafeInt<size_t>::addition(cbDest, cbType, cbNewDest) ||
                    cbNewDest > LocalDesc::MAX_LOCALDESC_ELEMENTS)
                {
                    COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);
                }

                memcpyNoGCRefs(&pLoc->ElementType[cbDest], psigManagedArg, cbType);
                cbDest = cbNewDest;
                break;
            }
    }

    if (cbDest > LocalDesc::MAX_LOCALDESC_ELEMENTS)
    {
        COMPlusThrow(kMarshalDirectiveException, IDS_EE_SIGTOOCOMPLEX);
    }

    pLoc->cbType = cbDest;
}

void ILStubLinker::GetStubTargetReturnType(LocalDesc* pLoc)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pLoc));
    }
    CONTRACTL_END;

    GetStubTargetReturnType(pLoc, m_pStubSigModule);
}

void ILStubLinker::GetStubTargetReturnType(LocalDesc* pLoc, Module* pModule)
{
    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pLoc));
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    GetManagedTypeHelper(pLoc, pModule, m_nativeFnSigBuilder.GetReturnSig(), m_pTypeContext, NULL);
}

void ILStubLinker::GetStubArgType(LocalDesc* pLoc)
{

    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pLoc));
    }
    CONTRACTL_END;

    GetStubArgType(pLoc, m_pStubSigModule);
}

void ILStubLinker::GetStubArgType(LocalDesc* pLoc, Module* pModule)
{

    CONTRACTL
    {
        STANDARD_VM_CHECK;
        PRECONDITION(CheckPointer(pLoc));
        PRECONDITION(CheckPointer(pModule));
    }
    CONTRACTL_END;

    GetManagedTypeHelper(pLoc, pModule, m_managedSigPtr.GetPtr(), m_pTypeContext, m_pMD);
}

//---------------------------------------------------------------------------------------
//
DWORD
ILStubLinker::SetStubTargetArgType(
    LocalDesc * pLoc,            // = NULL
    bool        fConsumeStubArg) // = true
{
    STANDARD_VM_CONTRACT;

    LocalDesc locDesc;

    if (fConsumeStubArg)
    {
        _ASSERTE(m_pStubSigModule);

        if (pLoc == NULL)
        {
            pLoc = &locDesc;
            GetStubArgType(pLoc, m_pStubSigModule);
        }

        IfFailThrow(m_managedSigPtr.SkipExactlyOne());
    }

    TransformArgForJIT(pLoc);

    DWORD dwArgNum = m_nativeFnSigBuilder.NewArg(pLoc);

    // If we are a reverse stub, then the native function signature is the signature of the
    // stub instead of the signature of the target.
    // Otherwise, the native signature is the signature of the target.
    // So, we only want to modify the target IL stack delta here if we are calling the native
    // function as our target signature.
    if (!m_fIsReverseStub)
    {
        m_iTargetStackDelta--;
    }

    return dwArgNum;
} // ILStubLinker::SetStubTargetArgType

//---------------------------------------------------------------------------------------
//
int ILStubLinker::GetToken(MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;
    return m_tokenMap.GetToken(pMD);
}

int ILStubLinker::GetToken(MethodTable* pMT)
{
    STANDARD_VM_CONTRACT;
    return m_tokenMap.GetToken(TypeHandle(pMT));
}

int ILStubLinker::GetToken(TypeHandle th)
{
    STANDARD_VM_CONTRACT;
    return m_tokenMap.GetToken(th);
}

int ILStubLinker::GetToken(FieldDesc* pFD)
{
    STANDARD_VM_CONTRACT;
    return m_tokenMap.GetToken(pFD);
}

int ILStubLinker::GetSigToken(PCCOR_SIGNATURE pSig, DWORD cbSig)
{
    STANDARD_VM_CONTRACT;
    return m_tokenMap.GetSigToken(pSig, cbSig);
}


BOOL ILStubLinker::StubHasVoidReturnType()
{
    LIMITED_METHOD_CONTRACT;
    return m_StubHasVoidReturnType;
}

void ILStubLinker::ClearCode()
{
    CONTRACTL
    {
        NOTHROW;
        MODE_ANY;
        GC_TRIGGERS;
    }
    CONTRACTL_END;

    DeleteCodeLabels();
    ClearCodeStreams();
}

void ILStubLinker::SetStubMethodDesc(MethodDesc* pMD)
{
    m_pMD = pMD;
}

// static
ILCodeStream* ILStubLinker::FindLastCodeStream(ILCodeStream* pList)
{
    LIMITED_METHOD_CONTRACT;

    if (NULL == pList)
    {
        return NULL;
    }

    while (NULL != pList->m_pNextStream)
    {
        pList = pList->m_pNextStream;
    }

    return pList;
}

ILCodeStream* ILStubLinker::NewCodeStream(CodeStreamType codeStreamType)
{
    STANDARD_VM_CONTRACT;

    NewHolder<ILCodeStream> pNewCodeStream = new ILCodeStream(this, codeStreamType);

    if (NULL == m_pCodeStreamList)
    {
        m_pCodeStreamList = pNewCodeStream;
    }
    else
    {
        ILCodeStream* pTail = FindLastCodeStream(m_pCodeStreamList);
        CONSISTENCY_CHECK(NULL == pTail->m_pNextStream);
        pTail->m_pNextStream = pNewCodeStream;
    }

    pNewCodeStream.SuppressRelease();
    return pNewCodeStream;
}

int ILCodeStream::GetToken(MethodDesc* pMD)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->GetToken(pMD);
}
int ILCodeStream::GetToken(MethodTable* pMT)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->GetToken(pMT);
}
int ILCodeStream::GetToken(TypeHandle th)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->GetToken(th);
}
int ILCodeStream::GetToken(FieldDesc* pFD)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->GetToken(pFD);
}
int ILCodeStream::GetSigToken(PCCOR_SIGNATURE pSig, DWORD cbSig)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->GetSigToken(pSig, cbSig);
}

DWORD ILCodeStream::NewLocal(CorElementType typ)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->NewLocal(typ);
}
DWORD ILCodeStream::NewLocal(LocalDesc loc)
{
    WRAPPER_NO_CONTRACT;
    return m_pOwner->NewLocal(loc);
}
DWORD ILCodeStream::SetStubTargetArgType(CorElementType typ, bool fConsumeStubArg)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->SetStubTargetArgType(typ, fConsumeStubArg);
}
DWORD ILCodeStream::SetStubTargetArgType(LocalDesc* pLoc, bool fConsumeStubArg)
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->SetStubTargetArgType(pLoc, fConsumeStubArg);
}
void ILCodeStream::SetStubTargetReturnType(CorElementType typ)
{
    STANDARD_VM_CONTRACT;
    m_pOwner->SetStubTargetReturnType(typ);
}
void ILCodeStream::SetStubTargetReturnType(LocalDesc* pLoc)
{
    STANDARD_VM_CONTRACT;
    m_pOwner->SetStubTargetReturnType(pLoc);
}
ILCodeLabel* ILCodeStream::NewCodeLabel()
{
    STANDARD_VM_CONTRACT;
    return m_pOwner->NewCodeLabel();
}
void ILCodeStream::ClearCode()
{
    LIMITED_METHOD_CONTRACT;
    m_uCurInstrIdx = 0;
}
