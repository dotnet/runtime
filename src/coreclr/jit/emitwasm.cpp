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
// Arguments:
//   ins      - instruction to emit
//   attr     - emit attributes
//   imm      - immediate value
//
void emitter::emitIns_I(instruction ins, emitAttr attr, cnsval_ssize_t imm)
{
    instrDesc* id  = emitNewInstrSC(attr, imm);
    insFormat  fmt = emitInsFormat(ins);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

//------------------------------------------------------------------------
// emitIns_J: Emit a jump instruction with an immediate operand.
//
// Arguments:
//   ins         - instruction to emit
//   attr        - emit attributes
//   imm         - immediate value (depth in control flow stack)
//   targetBlock - block at that depth
//
void emitter::emitIns_J(instruction ins, emitAttr attr, cnsval_ssize_t imm, BasicBlock* targetBlock)
{
    instrDesc* id  = emitNewInstrSC(attr, imm);
    insFormat  fmt = emitInsFormat(ins);

    id->idIns(ins);
    id->idInsFmt(fmt);

    if (m_debugInfoSize > 0)
    {
        id->idDebugOnlyInfo()->idTargetBlock = targetBlock;
    }

    dispIns(id);
    appendToCurIG(id);
}

//------------------------------------------------------------------------
// emitIns_S: Emit an instruction with a stack offset immediate.
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

void emitter::emitIns_R_I(instruction ins, emitAttr attr, regNumber reg, cnsval_ssize_t imm)
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

//-----------------------------------------------------------------------------
// emitNewInstrLclVarDecl: Construct an instrDesc corresponding to a wasm local
// declaration.
//
// Arguments:
//   attr        - emit attributes
//   localCount  - the count of locals in this declaration
//   type        - the type of local in the declaration
//   lclOffset   - used to provide the starting index of this local
//
// Notes:
//   `lclOffset` is stored as debug info attached to the instruction,
//    so the offset will only be used if m_debugInfoSize > 0
emitter::instrDesc* emitter::emitNewInstrLclVarDecl(emitAttr      attr,
                                                    unsigned int  localCount,
                                                    WasmValueType type,
                                                    int           lclOffset)
{
    instrDescLclVarDecl* id = static_cast<instrDescLclVarDecl*>(emitAllocAnyInstr(sizeof(instrDescLclVarDecl), attr));
    id->idLclCnt(localCount);
    id->idLclType(type);

    if (m_debugInfoSize > 0)
    {
        id->idDebugOnlyInfo()->lclOffset = lclOffset;
    }

    return id;
}

//-----------------------------------------------------------------------------------
// emitIns_I_Ty: Emit an instruction for a local variable declaration, encoding both
// a count (immediate) and a value type. This is specifically used for local variable
// declarations that require both the number of locals and their type to be encoded.
//
// Arguments:
//   ins      - instruction to emit
//   imm      - immediate value (local count)
//   valType  - value type of the local variable
//   offs     - local variable offset (= count of preceding locals) for debug info
void emitter::emitIns_I_Ty(instruction ins, unsigned int imm, WasmValueType valType, int offs)
{
    instrDesc* id  = emitNewInstrLclVarDecl(EA_8BYTE, imm, valType, offs);
    insFormat  fmt = emitInsFormat(ins);

    id->idIns(ins);
    id->idInsFmt(fmt);

    dispIns(id);
    appendToCurIG(id);
}

WasmValueType emitter::emitGetLclVarDeclType(const instrDesc* id)
{
    assert(id->idIsLclVarDecl());
    return static_cast<const instrDescLclVarDecl*>(id)->lclType;
}

unsigned int emitter::emitGetLclVarDeclCount(const instrDesc* id)
{
    assert(id->idIsLclVarDecl());
    return static_cast<const instrDescLclVarDecl*>(id)->lclCnt;
}

emitter::insFormat emitter::emitInsFormat(instruction ins)
{
    static_assert(IF_COUNT < 255);

    const static uint8_t insFormats[] = {
#define INST(id, nm, info, fmt, opcode) fmt,
#include "instrs.h"
    };

    assert(ins < ArrLen(insFormats));
    assert((insFormats[ins] != IF_NONE));
    return static_cast<insFormat>(insFormats[ins]);
}

static unsigned GetInsOpcode(instruction ins)
{
    static const uint8_t insOpcodes[] = {
#define INST(id, nm, info, fmt, opcode) static_cast<uint8_t>(opcode),
#include "instrs.h"
    };

    assert(ins < ArrLen(insOpcodes));
    return insOpcodes[ins];
}

size_t emitter::emitSizeOfInsDsc(instrDesc* id) const
{
    if (emitIsSmallInsDsc(id))
    {
        return SMALL_IDSC_SIZE;
    }

    if (id->idIsLargeCns())
    {
        assert(!id->idIsLargeDsp());
        assert(!id->idIsLargeCall());
        return sizeof(instrDescCns);
    }

    if (id->idIsLclVarDecl())
    {
        return sizeof(instrDescLclVarDecl);
    }

    return sizeof(instrDesc);
}

unsigned emitter::emitGetAlignHintLog2(const instrDesc* id)
{
    // FIXME
    return 0;
}

unsigned emitter::SizeOfULEB128(uint64_t value)
{
    // bits_to_encode = (data != 0) ? 64 - CLZ(x) : 1 = 64 - CLZ(data | 1)
    // bytes = ceil(bits_to_encode / 7.0);            = (6 + bits_to_encode) / 7
    unsigned x = 6 + 64 - (unsigned)BitOperations::LeadingZeroCount(value | 1UL);
    // Division by 7 is done by (x * 37) >> 8 where 37 = ceil(256 / 7).
    // This works for 0 <= x < 256 / (7 * 37 - 256), i.e. 0 <= x <= 85.
    return (x * 37) >> 8;
}

unsigned emitter::SizeOfSLEB128(int64_t value)
{
    // The same as SizeOfULEB128 calculation but we have to account for the sign bit.
    unsigned x = 1 + 6 + 64 - (unsigned)BitOperations::LeadingZeroCount((uint64_t)(value ^ (value >> 63)) | 1UL);
    return (x * 37) >> 8;
}

static uint8_t GetWasmValueTypeCode(WasmValueType type)
{
    // clang-format off
    static const uint8_t typecode_mapping[] = {
        0x00, // WasmValueType::Invalid = 0,
        0x7C, // WasmValueType::F64 = 1,
        0x7D, // WasmValueType::F32 = 2,
        0x7E, // WasmValueType::I64 = 3,
        0x7F, // WasmValueType::I32 = 4,
    };
    static const int WASM_TYP_COUNT = ArrLen(typecode_mapping);
    static_assert(ArrLen(typecode_mapping) == (int)WasmValueType::Count);
    // clang-format on

    return typecode_mapping[static_cast<unsigned>(type)];
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
        case IF_RAW_ULEB128:
            assert(!idIsCnsReloc());
            size = SizeOfULEB128(emitGetInsSC(this));
            break;
        case IF_LOCAL_DECL:
        {
            assert(idIsLclVarDecl());
            uint8_t typeCode = GetWasmValueTypeCode(emitGetLclVarDeclType(this));
            size             = SizeOfULEB128(emitGetLclVarDeclCount(this)) + sizeof(typeCode);
            break;
        }
        case IF_ULEB128:
            size += idIsCnsReloc() ? PADDED_RELOC_SIZE : SizeOfULEB128(emitGetInsSC(this));
            break;
        case IF_SLEB128:
            size += idIsCnsReloc() ? PADDED_RELOC_SIZE : SizeOfSLEB128(emitGetInsSC(this));
            break;
        case IF_F32:
            size += 4;
            break;
        case IF_F64:
            size += 8;
            break;
        case IF_MEMARG:
        {
            uint64_t align = emitGetAlignHintLog2(this);
            assert(align < 64); // spec says align > 2^6 produces a memidx for multiple memories.
            size += SizeOfULEB128(align);
            size += idIsCnsReloc() ? PADDED_RELOC_SIZE : SizeOfULEB128(emitGetInsSC(this));
            break;
        }
        default:
            unreached();
    }
    return size;
}

void emitter::emitSetShortJump(instrDescJmp* id)
{
    NYI_WASM("emitSetShortJump");
}

size_t emitter::emitOutputULEB128(uint8_t* destination, uint64_t value)
{
    uint8_t* buffer = destination + writeableOffset;
    if (value >= 0x80)
    {
        int pos = 0;
        do
        {
            buffer[pos++] = (uint8_t)((value & 0x7F) | ((value >= 0x80) ? 0x80u : 0));
            value >>= 7;
        } while (value > 0);

        return pos;
    }
    else
    {
        buffer[0] = (uint8_t)value;
        return 1;
    }
}

size_t emitter::emitOutputSLEB128(uint8_t* destination, int64_t value)
{
    uint8_t* buffer = destination + writeableOffset;
    bool     cont   = true;
    int      pos    = 0;
    while (cont)
    {
        uint8_t b = ((uint8_t)value & 0x7F);
        value >>= 7;
        bool isSignBitSet = (b & 0x40) != 0;
        if ((value == 0 && !isSignBitSet) || (value == -1 && isSignBitSet))
        {
            cont = false;
        }
        else
        {
            b |= 0x80;
        }
        buffer[pos++] = b;
    }
    return pos;
}

size_t emitter::emitRawBytes(uint8_t* destination, const void* source, size_t count)
{
    memcpy(destination + writeableOffset, source, count);
    return count;
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
            dst += emitOutputByte(dst, 0x40 /* block type of void */);
            break;
        case IF_ULEB128:
        {
            dst += emitOutputByte(dst, opcode);
            cnsval_ssize_t constant = emitGetInsSC(id);
            dst += emitOutputULEB128(dst, (uint64_t)constant);
            break;
        }
        case IF_SLEB128:
        {
            dst += emitOutputByte(dst, opcode);
            cnsval_ssize_t constant = emitGetInsSC(id);
            dst += emitOutputSLEB128(dst, (int64_t)constant);
            break;
        }
        case IF_F32:
        {
            dst += emitOutputByte(dst, opcode);
            // Reinterpret the bits as a double constant and then truncate it to f32,
            //  then finally copy the raw truncated f32 bits to the output.
            cnsval_ssize_t bits = emitGetInsSC(id);
            double         value;
            float          truncated;
            memcpy(&value, &bits, sizeof(double));
            truncated = FloatingPointUtils::convertToSingle(value);
            dst += emitRawBytes(dst, &truncated, sizeof(float));
            break;
        }
        case IF_F64:
        {
            dst += emitOutputByte(dst, opcode);
            // The int64 bits are actually a double constant we can copy directly
            //  to the output stream.
            cnsval_ssize_t bits = emitGetInsSC(id);
            dst += emitRawBytes(dst, &bits, sizeof(cnsval_ssize_t));
            break;
        }
        case IF_RAW_ULEB128:
        {
            cnsval_ssize_t constant = emitGetInsSC(id);
            dst += emitOutputULEB128(dst, (uint64_t)constant);
            break;
        }
        case IF_MEMARG:
        {
            dst += emitOutputByte(dst, opcode);
            uint64_t align  = emitGetAlignHintLog2(id);
            uint64_t offset = emitGetInsSC(id);
            assert(align <= UINT32_MAX); // spec says memarg alignment is u32
            assert(align < 64);          // spec says align > 2^6 produces a memidx for multiple memories.
            dst += emitOutputULEB128(dst, align);
            dst += emitOutputULEB128(dst, offset);
            break;
        }
        case IF_LOCAL_DECL:
        {
            assert(id->idIsLclVarDecl());
            cnsval_ssize_t count   = emitGetLclVarDeclCount(id);
            uint8_t        valType = GetWasmValueTypeCode(emitGetLclVarDeclType(id));
            dst += emitOutputULEB128(dst, (uint64_t)count);
            dst += emitOutputByte(dst, valType);
            break;
        }
        default:
            NYI_WASM("emitOutputInstr");
            break;
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

    auto dispJumpTargetIfAny = [this, id]() {
        if (m_debugInfoSize > 0)
        {
            BasicBlock* const targetBlock = id->idDebugOnlyInfo()->idTargetBlock;
            if (targetBlock != nullptr)
            {
                printf(" ;; ");
                insGroup* const targetGroup = (insGroup*)emitCodeGetCookie(targetBlock);
                assert(targetGroup != nullptr);
                emitPrintLabel(targetGroup);
            }
        }
    };

    // The reference for the following style of display is wasm-objdump output.
    //
    switch (fmt)
    {
        case IF_OPCODE:
        case IF_BLOCK:
            break;

        case IF_RAW_ULEB128:
        case IF_ULEB128:
        {
            cnsval_ssize_t imm = emitGetInsSC(id);
            printf(" %llu", (uint64_t)imm);
            dispJumpTargetIfAny();
        }
        break;

        case IF_LOCAL_DECL:
        {
            unsigned int  count   = emitGetLclVarDeclCount(id);
            WasmValueType valType = emitGetLclVarDeclType(id);
            assert(count > 0); // we should not be declaring a local entry with zero count

            if (m_debugInfoSize > 0)
            {
                // With debug info: print the local offsets being declared
                int offs = id->idDebugOnlyInfo()->lclOffset;
                if (count > 1)
                {
                    printf("[%u..%u] type=%s", offs, offs + count - 1, WasmValueTypeName(valType));
                }
                else // single local case
                {
                    printf("[%u] type=%s", offs, WasmValueTypeName(valType));
                }
            }
            else
            {
                // No debug info case: just print the count and type of the locals
                printf(" count=%u type=%s", count, WasmValueTypeName(valType));
            }
        }
        break;

        case IF_SLEB128:
        {
            cnsval_ssize_t imm = emitGetInsSC(id);
            printf(" %lli", (int64_t)imm);
        }
        break;

        case IF_F32:
        case IF_F64:
        {
            cnsval_ssize_t bits = emitGetInsSC(id);
            double         value;
            memcpy(&value, &bits, sizeof(double));
            printf(" %f", value);
        }
        break;

        case IF_MEMARG:
        {
            unsigned       log2align = emitGetAlignHintLog2(id);
            cnsval_ssize_t offset    = emitGetInsSC(id);
            printf(" %u %llu", log2align, (uint64_t)offset);
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
