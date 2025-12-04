// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "codegen.h"

// clang-format off
/*static*/ const BYTE CodeGenInterface::instInfo[] =
{
    #define INST(id, nm, info, fmt, opcode) info,
    #include "instrs.h"
};

static const uint8_t insOpcodes[]
{
    #define INST(id, nm, info, fmt, opcode) static_cast<uint8_t>(opcode),
    #include "instrs.h"
};
// clang-format on

void emitter::emitIns(instruction ins)
{
    instrDesc* id = emitNewInstrSmall(EA_8BYTE);
    id->idIns(ins);
    id->idInsFmt(IF_OPCODE);

    dispIns(id);
    appendToCurIG(id);
}

//------------------------------------------------------------------------
// emitIns_I: Emit an instruction with an immediate operand.
//
void emitter::emitIns_I(instruction ins, emitAttr attr, target_ssize_t imm)
{
    instrDesc* id  = emitNewInstrSC(attr, imm);
    insFormat  fmt = emitInsFormat(ins);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

//------------------------------------------------------------------------
// emitIns_S: Emit a memory instruction with a stack-based address mode operand.
//
void emitter::emitIns_S(instruction ins, emitAttr attr, int varx, int offs)
{
    bool FPBased;
    int  lclOffset = emitComp->lvaFrameAddress(varx, &FPBased);
    int  offset    = lclOffset + offs;
    noway_assert(offset >= 0); // WASM address modes are unsigned.

    emitIns_I(ins, attr, offset);
}

void emitter::emitIns_R(instruction ins, emitAttr attr, regNumber reg)
{
    NYI_WASM("emitIns_R");
}

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, ssize_t imm)
{
    NYI_WASM("emitIns_R_I");
}

void emitter::emitIns_Mov(instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip)
{
    NYI_WASM("emitIns_Mov");
}

void emitter::emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2)
{
    NYI_WASM("emitIns_R_R");
}

void emitter::emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs)
{
    NYI_WASM("emitIns_S_R");
}

bool emitter::emitInsIsStore(instruction ins)
{
    NYI_WASM("emitInsIsStore");
    return false;
}

emitter::insFormat emitter::emitInsFormat(instruction ins)
{
    // clang-format off
    const static insFormat insFormats[] =
    {
        #define INST(id, nm, info, fmt, opcode) fmt,
        #include "instrs.h"
    };
    // clang-format on

    assert(ins < ArrLen(insFormats));
    assert((insFormats[ins] != IF_NONE));
    return insFormats[ins];
}

static unsigned GetInsOpcode(instruction ins)
{
    assert(ins < ArrLen(insOpcodes));
    return insOpcodes[ins];
}

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    if (emitIsSmallInsDsc(id))
        return SMALL_IDSC_SIZE;

    if (id->idIsLargeCns())
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCall());
        return sizeof(instrDescCns);
    }
    return sizeof(instrDesc);
}

static unsigned SizeOfULEB128(uint64_t value)
{
    // bits_to_encode = (data != 0) ? 64 - CLZ(x) : 1 = 64 - CLZ(data | 1)
    // bytes = ceil(bits_to_encode / 7.0);            = (6 + bits_to_encode) / 7
    unsigned x = 6 + 64 - (unsigned)BitOperations::LeadingZeroCount(value | 1UL);
    // Division by 7 is done by (x * 37) >> 8 where 37 = ceil(256 / 7).
    // This works for 0 <= x < 256 / (7 * 37 - 256), i.e. 0 <= x <= 85.
    return (x * 37) >> 8;
}

unsigned emitter::instrDesc::idCodeSize() const
{
#ifdef TARGET_WASM32
    static const unsigned PADDED_RELOC_SIZE = 5;
#else
#error WASM64
#endif

    // Currently, all our instructions have 1 byte opcode.
    unsigned size = 1;
    assert(FitsIn<uint8_t>(GetInsOpcode(idIns())));
    switch (idInsFmt())
    {
        case IF_OPCODE:
            break;
        case IF_BLOCK:
            size += 1;
            break;
        case IF_LABEL:
            assert(!idIsCnsReloc());
            size = SizeOfULEB128(static_cast<target_size_t>(emitGetInsSC(this)));
            break;
        case IF_ULEB128:
            size += idIsCnsReloc() ? PADDED_RELOC_SIZE : SizeOfULEB128(static_cast<target_size_t>(emitGetInsSC(this)));
            break;
        case IF_MEMARG:
            size += 1; // The alignment hint byte.
            size += idIsCnsReloc() ? PADDED_RELOC_SIZE : SizeOfULEB128(static_cast<target_size_t>(emitGetInsSC(this)));
            break;
        default:
            unreached();
    }
    return size;
}

void emitter::emitSetShortJump(instrDescJmp* id)
{
    NYI_WASM("emitSetShortJump");
}

size_t emitter::emitOutputInstr(insGroup* ig, instrDesc* id, BYTE** dp)
{
    BYTE*       dst    = *dp;
    size_t      sz     = emitSizeOfInsDsc(id);
    instruction ins    = id->idIns();
    insFormat   insFmt = id->idInsFmt();
    unsigned    opcode = GetInsOpcode(ins);

    switch (insFmt)
    {
        case IF_OPCODE:
            dst += emitOutputByte(dst, opcode);
            break;
        case IF_BLOCK:
            dst += emitOutputByte(dst, opcode);
            dst += emitOutputByte(dst, 0x40);
            break;
        case IF_ULEB128:
            dst += emitOutputByte(dst, opcode);
            // TODO-WASM: emit uleb128
            break;
        case IF_LABEL:
            // TODO-WASM: emit uleb128
        default:
            NYI_WASM("emitOutputInstr");
    }

#ifdef DEBUG
    bool dspOffs = emitComp->opts.dspGCtbls;
    if (emitComp->opts.disAsm || emitComp->verbose)
    {
        emitDispIns(id, false, dspOffs, true, emitCurCodeOffs(*dp), *dp, (dst - *dp), ig);
    }
#else
    if (emitComp->opts.disAsm)
    {
        emitDispIns(id, false, 0, true, emitCurCodeOffs(*dp), *dp, (dst - *dp), ig);
    }
#endif

    *dp = dst;
    return sz;
}

/*static*/ instruction emitter::emitJumpKindToIns(emitJumpKind jumpKind)
{
    const instruction emitJumpKindInstructions[] = {
        INS_nop,
#define JMP_SMALL(en, rev, ins) INS_##ins,
#include "emitjmps.h"
    };

    assert((unsigned)jumpKind < ArrLen(emitJumpKindInstructions));
    return emitJumpKindInstructions[jumpKind];
}

//--------------------------------------------------------------------
// emitDispIns: Dump the given instruction to jitstdout.
//
// Arguments:
//   id     - The instruction
//   isNew  - Whether the instruction is newly generated (before encoding).
//   doffs  - If true, always display the passed-in offset.
//   asmfm  - Whether the instruction should be displayed in assembly format.
//            If false some additional information may be printed for the instruction.
//   offset - The offset of the instruction. Only displayed if doffs is true or if !isNew && !asmfm.
//   code   - Pointer to the actual code, used for displaying the address and encoded bytes if turned on.
//   sz     - The size of the instruction, used to display the encoded bytes.
//   ig     - The instruction group containing the instruction.
//
void emitter::emitDispIns(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig)
{
#ifdef DEBUG
    if (EMITVERBOSE)
    {
        unsigned idNum = id->idDebugOnlyInfo()->idNum;
        printf("IN%04x: ", idNum);
    }
#endif

    if (pCode == nullptr)
    {
        sz = 0;
    }

    if (!isNew && !asmfm && sz)
    {
        doffs = true;
    }

    // Display the instruction address.
    emitDispInsAddr(pCode);

    // Display the instruction offset.
    emitDispInsOffs(offset, doffs);

    BYTE* pCodeRW = nullptr;
    if (pCode != nullptr)
    {
        /* Display the instruction hex code */
        assert(((pCode >= emitCodeBlock) && (pCode < emitCodeBlock + emitTotalHotCodeSize)) ||
               ((pCode >= emitColdCodeBlock) && (pCode < emitColdCodeBlock + emitTotalColdCodeSize)));

        pCodeRW = pCode + writeableOffset;
    }

    emitDispInsHex(id, pCodeRW, sz);

    printf("      ");

    instruction ins = id->idIns();
    insFormat   fmt = id->idInsFmt();

    emitDispInst(ins);

    // The reference for the following style of display is wasm-objdump output.
    //
    switch (fmt)
    {
        case IF_OPCODE:
        case IF_BLOCK:
            break;

        case IF_LABEL:
        case IF_ULEB128:
        {
            target_size_t imm = emitGetInsSC(id);
            printf(" %u", imm);
        }
        break;

        case IF_MEMARG:
        {
            // TODO-WASM: decide what our strategy for alignment hints is and display these accordingly.
            unsigned      log2align = 1;
            target_size_t offset    = emitGetInsSC(id);
            printf(" %u %u", log2align, offset);
        }
        break;

        default:
            unreached();
    }

    printf("\n");
}

//--------------------------------------------------------------------
// emitDispInst: Display the instruction name.
//
void emitter::emitDispInst(instruction ins)
{
    const char* instName = codeGen->genInsName(ins);
    printf("%s", instName);
}

//--------------------------------------------------------------------
// emitDispInsHex: Display (optionally) the instruction encoding in hex.
//
void emitter::emitDispInsHex(instrDesc* id, BYTE* code, size_t sz)
{
    if (!emitComp->opts.disCodeBytes)
    {
        return;
    }

    // We do not display the instruction hex if we want diff-able disassembly
    if (!emitComp->opts.disDiffable && (sz != 0))
    {
        static const int PAD_WIDTH = 28; // From wasm-objdump output.

        int length = 0;
        for (size_t i = 0; i < sz; i++)
        {
            length += printf(" %02X", code[i]);
        }
        if (length < PAD_WIDTH)
        {
            printf("%*c", PAD_WIDTH - length, ' ');
        }
        printf(" | ");
    }
}

#if defined(DEBUG) || defined(LATE_DISASM)
emitter::insExecutionCharacteristics emitter::getInsExecutionCharacteristics(instrDesc* id)
{
    // TODO-WASM: for real...
    insExecutionCharacteristics result;
    result.insThroughput = PERFSCORE_THROUGHPUT_1C;
    result.insLatency    = PERFSCORE_LATENCY_1C;
    return result;
}
#endif // defined(DEBUG) || defined(LATE_DISASM)

#ifdef DEBUG
void emitter::emitInsSanityCheck(instrDesc* id)
{
}
#endif // DEBUG
