// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if defined(TARGET_ARM64)

// The ARM64 instructions are all 32 bits in size.
// we use an unsigned int to hold the encoded instructions.
// This typedef defines the type that we use to hold encoded instructions.
//
typedef unsigned int code_t;

static bool strictArmAsm;

/************************************************************************/
/*         Routines that compute the size of / encode instructions      */
/************************************************************************/

/************************************************************************/
/*             Debug-only routines to display instructions              */
/************************************************************************/

enum PredicateType
{
    PREDICATE_NONE = 0, // Predicate printed with no extensions
    PREDICATE_MERGE,    // Predicate printed with /m
    PREDICATE_ZERO,     // Predicate printed with /z
    PREDICATE_SIZED,    // Predicate printed with element size
    PREDICATE_N,        // Predicate printed as counter
    PREDICATE_N_SIZED,  // Predicate printed as counter with element size
};

const char* emitSveRegName(regNumber reg);
const char* emitVectorRegName(regNumber reg);
const char* emitPredicateRegName(regNumber reg, PredicateType ptype);

void emitDispInsHelp(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig);
void emitDispLargeJmp(
    instrDesc* id, bool isNew, bool doffs, bool asmfm, unsigned offset, BYTE* pCode, size_t sz, insGroup* ig);
void emitDispComma();
void emitDispInst(instruction ins);
void emitDispImm(ssize_t imm, bool addComma, bool alwaysHex = false, bool isAddrOffset = false);
void emitDispElementIndex(const ssize_t imm, const bool addComma);
void emitDispFloatZero();
void emitDispFloatImm(ssize_t imm8);
void emitDispSmallFloatImm(ssize_t imm, instruction ins);
void emitDispImmOptsLSL(ssize_t imm, bool hasShift, unsigned shiftAmount);
void emitDispCond(insCond cond);
void emitDispFlags(insCflags flags);
void emitDispBarrier(insBarrier barrier);
void emitDispShiftOpts(insOpts opt);
void emitDispExtendOpts(insOpts opt);
void emitDispSveExtendOpts(insOpts opt);
void emitDispSveExtendOptsModN(insOpts opt, int n);
void emitDispSveModAddr(instruction ins, regNumber reg1, regNumber reg2, insOpts opt, insFormat fmt);
void emitDispSveImm(regNumber reg1, ssize_t imm, insOpts opt);
void emitDispSveImmMulVl(regNumber reg1, ssize_t imm);
void emitDispSveImmIndex(regNumber reg1, insOpts opt, ssize_t imm);
void emitDispLSExtendOpts(insOpts opt);
void emitDispReg(regNumber reg, emitAttr attr, bool addComma);
void emitDispSveReg(regNumber reg, insOpts opt, bool addComma);
void emitDispVectorReg(regNumber reg, insOpts opt, bool addComma);
void emitDispVectorRegIndex(regNumber reg, emitAttr elemsize, ssize_t index, bool addComma);
void emitDispVectorRegList(regNumber firstReg, unsigned listSize, insOpts opt, bool addComma);
void emitDispVectorElemList(regNumber firstReg, unsigned listSize, emitAttr elemsize, unsigned index, bool addComma);
void emitDispSveConsecutiveRegList(regNumber firstReg, unsigned listSize, insOpts opt, bool addComma);
void emitDispPredicateReg(regNumber reg, PredicateType ptype, insOpts opt, bool addComma);
void emitDispPredicateRegPair(regNumber reg, insOpts opt);
void emitDispLowPredicateReg(regNumber reg, PredicateType ptype, insOpts opt, bool addComma);
void emitDispLowPredicateRegPair(regNumber reg, insOpts opt);
void emitDispVectorLengthSpecifier(instrDesc* id);
void emitDispArrangement(insOpts opt);
void emitDispElemsize(emitAttr elemsize);
void emitDispShiftedReg(regNumber reg, insOpts opt, ssize_t imm, emitAttr attr);
void emitDispExtendReg(regNumber reg, insOpts opt, ssize_t imm);
void emitDispAddrRI(regNumber reg, insOpts opt, ssize_t imm);
void emitDispAddrRRExt(regNumber reg1, regNumber reg2, insOpts opt, bool isScaled, emitAttr size);
void emitDispSvePattern(insSvePattern pattern, bool addComma);
void emitDispSvePrfop(insSvePrfop prfop, bool addComma);

/************************************************************************/
/*  Private members that deal with target-dependent instr. descriptors  */
/************************************************************************/

private:
instrDesc* emitNewInstrCallDir(int              argCnt,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

instrDesc* emitNewInstrCallInd(int              argCnt,
                               ssize_t          disp,
                               VARSET_VALARG_TP GCvars,
                               regMaskTP        gcrefRegs,
                               regMaskTP        byrefRegs,
                               emitAttr         retSize,
                               emitAttr         secondRetSize);

/************************************************************************/
/*   enum to allow instruction optimisation to specify register order   */
/************************************************************************/

enum RegisterOrder
{
    eRO_none = 0,
    eRO_ascending,
    eRO_descending
};

/************************************************************************/
/*               Private helpers for instruction output                 */
/************************************************************************/

private:
bool emitInsIsCompare(instruction ins);
bool emitInsIsLoad(instruction ins);
bool emitInsIsStore(instruction ins);
bool emitInsIsLoadOrStore(instruction ins);
bool emitInsIsVectorRightShift(instruction ins);
bool emitInsIsVectorLong(instruction ins);
bool emitInsIsVectorNarrow(instruction ins);
bool emitInsIsVectorWide(instruction ins);
bool emitInsDestIsOp2(instruction ins);
emitAttr emitInsTargetRegSize(instrDesc* id);
emitAttr emitInsLoadStoreSize(instrDesc* id);

emitter::insFormat emitInsFormat(instruction ins);
emitter::code_t emitInsCode(instruction ins, insFormat fmt);
emitter::code_t emitInsCodeSve(instruction ins, insFormat fmt);

// Generate code for a load or store operation and handle the case of contained GT_LEA op1 with [base + index<<scale +
// offset]
void emitInsLoadStoreOp(instruction ins, emitAttr attr, regNumber dataReg, GenTreeIndir* indir);

//  Emit the 32-bit Arm64 instruction 'code' into the 'dst'  buffer
unsigned emitOutput_Instr(BYTE* dst, code_t code);

// Append the machine code corresponding to the given SVE instruction descriptor.
BYTE* emitOutput_InstrSve(BYTE* dst, instrDesc* id);

// A helper method to return the natural scale for an EA 'size'
static unsigned NaturalScale_helper(emitAttr size);

// A helper method to perform a Rotate-Right shift operation
static UINT64 ROR_helper(UINT64 value, unsigned sh, unsigned width);

// A helper method to perform a 'NOT' bitwise complement operation
static UINT64 NOT_helper(UINT64 value, unsigned width);

// A helper method to perform a bit Replicate operation
static UINT64 Replicate_helper(UINT64 value, unsigned width, emitAttr size);

// Method to do check if mov is redundant with respect to the last instruction.
// If yes, the caller of this method can choose to omit current mov instruction.
static bool IsMovInstruction(instruction ins);
bool IsRedundantMov(instruction ins, emitAttr size, regNumber dst, regNumber src, bool canSkip);

// Methods to optimize a Ldr or Str with an alternative instruction.
bool IsRedundantLdStr(instruction ins, regNumber reg1, regNumber reg2, ssize_t imm, emitAttr size, insFormat fmt);
RegisterOrder IsOptimizableLdrStrWithPair(
    instruction ins, regNumber reg1, regNumber reg2, ssize_t imm, emitAttr size, insFormat fmt);
bool ReplaceLdrStrWithPairInstr(instruction ins,
                                emitAttr    reg1Attr,
                                regNumber   reg1,
                                regNumber   reg2,
                                ssize_t     imm,
                                emitAttr    size,
                                insFormat   fmt,
                                bool        localVar = false,
                                int         varx     = -1,
                                int         offs     = -1);
bool IsOptimizableLdrToMov(instruction ins, regNumber reg1, regNumber reg2, ssize_t imm, emitAttr size, insFormat fmt);
FORCEINLINE bool OptimizeLdrStr(instruction ins,
                                emitAttr    reg1Attr,
                                regNumber   reg1,
                                regNumber   reg2,
                                ssize_t     imm,
                                emitAttr    size,
                                insFormat   fmt,
                                bool        localVar = false,
                                int         varx     = -1,
                                int offs = -1 DEBUG_ARG(bool useRsvdReg = false));

emitLclVarAddr* emitGetLclVarPairLclVar2(instrDesc* id)
{
    assert(id->idIsLclVarPair());
    if (id->idIsLargeCns())
    {
        return &(((instrDescLclVarPairCns*)id)->iiaLclVar2);
    }
    else
    {
        return &(((instrDescLclVarPair*)id)->iiaLclVar2);
    }
}

/************************************************************************
*
* This union is used to encode/decode the special ARM64 immediate values
* that is listed as imm(N,r,s) and referred to as 'bitmask immediate'
*/

union bitMaskImm {
    struct
    {
        unsigned immS : 6; // bits 0..5
        unsigned immR : 6; // bits 6..11
        unsigned immN : 1; // bits 12
    };
    unsigned immNRS; // concat N:R:S forming a 13-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 64-bit immediate and its 'bitmask immediate'
*   representation imm(i16,hw)
*/

static emitter::bitMaskImm emitEncodeBitMaskImm(INT64 imm, emitAttr size);

static INT64 emitDecodeBitMaskImm(const emitter::bitMaskImm bmImm, emitAttr size);

/************************************************************************
*
* This union is used to encode/decode the special ARM64 immediate values
* that is listed as imm(i16,hw) and referred to as 'halfword immediate'
*/

union halfwordImm {
    struct
    {
        unsigned immVal : 16; // bits  0..15
        unsigned immHW : 2;   // bits 16..17
    };
    unsigned immHWVal; // concat HW:Val forming a 18-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 64-bit immediate and its 'halfword immediate'
*   representation imm(i16,hw)
*/

static emitter::halfwordImm emitEncodeHalfwordImm(INT64 imm, emitAttr size);

static INT64 emitDecodeHalfwordImm(const emitter::halfwordImm hwImm, emitAttr size);

/************************************************************************
*
* This union is used to encode/decode the special ARM64 immediate values
* that is listed as imm(i16,by) and referred to as 'byteShifted immediate'
*/

union byteShiftedImm {
    struct
    {
        unsigned immVal : 8;  // bits  0..7
        unsigned immBY : 2;   // bits  8..9
        unsigned immOnes : 1; // bit   10
    };
    unsigned immBSVal; // concat Ones:BY:Val forming a 10-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a 16/32-bit immediate and its 'byteShifted immediate'
*   representation imm(i8,by)
*/

static emitter::byteShiftedImm emitEncodeByteShiftedImm(INT64 imm, emitAttr size, bool allow_MSL);

static UINT32 emitDecodeByteShiftedImm(const emitter::byteShiftedImm bsImm, emitAttr size);

/************************************************************************
*
* This union is used to encode/decode the special ARM64 immediate values
* that are use for FMOV immediate and referred to as 'float 8-bit immediate'
*/

union floatImm8 {
    struct
    {
        unsigned immMant : 4; // bits 0..3
        unsigned immExp : 3;  // bits 4..6
        unsigned immSign : 1; // bits 7
    };
    unsigned immFPIVal; // concat Sign:Exp:Mant forming an 8-bit unsigned immediate
};

/************************************************************************
*
*  Convert between a double and its 'float 8-bit immediate' representation
*/

static emitter::floatImm8 emitEncodeFloatImm8(double immDbl);

static double emitDecodeFloatImm8(const emitter::floatImm8 fpImm);

static ssize_t emitEncodeRotationImm90_or_270(ssize_t imm);

static ssize_t emitDecodeRotationImm90_or_270(ssize_t imm);

static bool emitIsValidEncodedRotationImm90_or_270(ssize_t imm);

static ssize_t emitEncodeRotationImm0_to_270(ssize_t imm);

static ssize_t emitDecodeRotationImm0_to_270(ssize_t imm);

static bool emitIsValidEncodedRotationImm0_to_270(ssize_t imm);

static ssize_t emitEncodeSmallFloatImm(double immDbl, instruction ins);

static double emitDecodeSmallFloatImm(ssize_t imm, instruction ins);

static bool emitIsValidEncodedSmallFloatImm(size_t imm);

/************************************************************************
*
*  This union is used to encode/decode the cond, nzcv and imm5 values for
*   instructions that use them in the small constant immediate field
*/

union condFlagsImm {
    struct
    {
        insCond   cond : 4;  // bits  0..3
        insCflags flags : 4; // bits  4..7
        unsigned  imm5 : 5;  // bits  8..12
    };
    unsigned immCFVal; // concat imm5:flags:cond forming an 13-bit unsigned immediate
};

// Returns an encoding for the specified register used in the 'Rd' position
static code_t insEncodeReg_Rd(regNumber reg);

// Returns an encoding for the specified register used in the 'Rt' position
static code_t insEncodeReg_Rt(regNumber reg);

// Returns an encoding for the specified register used in the 'Rn' position
static code_t insEncodeReg_Rn(regNumber reg);

// Returns an encoding for the specified register used in the 'Rm' position
static code_t insEncodeReg_Rm(regNumber reg);

// Returns an encoding for the specified register used in the 'Ra' position
static code_t insEncodeReg_Ra(regNumber reg);

// Returns an encoding for the specified register used in the 'Vd' position
static code_t insEncodeReg_Vd(regNumber reg);

// Returns an encoding for the specified register used in the 'Vt' position
static code_t insEncodeReg_Vt(regNumber reg);

// Returns an encoding for the specified register used in the 'Vn' position
static code_t insEncodeReg_Vn(regNumber reg);

// Returns an encoding for the specified register used in the 'Vm' position
static code_t insEncodeReg_Vm(regNumber reg);

// Returns an encoding for the specified register used in the 'Va' position
static code_t insEncodeReg_Va(regNumber reg);

// Return an encoding for the specified 'V' register used in '4' thru '0' position.
static code_t insEncodeReg_V_4_to_0(regNumber reg);

// Return an encoding for the specified 'V' register used in '9' thru '5' position.
static code_t insEncodeReg_V_9_to_5(regNumber reg);

// Return an encoding for the specified 'P' register used in '12' thru '10' position.
static code_t insEncodeReg_P_12_to_10(regNumber reg);

// Return an encoding for the specified 'V' register used in '20' thru '16' position.
static code_t insEncodeReg_V_20_to_16(regNumber reg);

// Return an encoding for the specified 'R' register used in '20' thru '16' position.
static code_t insEncodeReg_R_20_to_16(regNumber reg);

// Return an encoding for the specified 'R' register used in '9' thru '5' position.
static code_t insEncodeReg_R_9_to_5(regNumber reg);

// Return an encoding for the specified 'R' register used in '4' thru '0' position.
static code_t insEncodeReg_R_4_to_0(regNumber reg);

// Return an encoding for the specified 'P' register used in '19' thru '16' position.
static code_t insEncodeReg_P_19_to_16(regNumber reg);

// Return an encoding for the specified 'P' register used in '3' thru '0' position.
static code_t insEncodeReg_P_3_to_0(regNumber reg);

// Return an encoding for the specified 'P' register used in '8' thru '5' position.
static code_t insEncodeReg_P_8_to_5(regNumber reg);

// Return an encoding for the specified 'P' register used in '13' thru '10' position.
static code_t insEncodeReg_P_13_to_10(regNumber reg);

// Return an encoding for the specified 'R' register used in '17' thru '16' position.
static code_t insEncodeReg_R_17_to_16(regNumber reg);

// Return an encoding for the specified 'P' register used in '7' thru '5' position.
static code_t insEncodeReg_P_7_to_5(regNumber reg);

// Return an encoding for the specified 'P' register used in '3' thru '1' position.
static code_t insEncodeReg_P_3_to_1(regNumber reg);

// Return an encoding for the specified 'P' register used in '2' thru '0' position.
static code_t insEncodeReg_P_2_to_0(regNumber reg);

// Return an encoding for the specified predicate type used in '16' position.
static code_t insEncodePredQualifier_16(bool merge);

// Return an encoding for the specified predicate type used in '4' position.
static code_t insEncodePredQualifier_4(bool merge);

// Return an encoding for the specified 'V' register used in '18' thru '16' position.
static code_t insEncodeReg_V_18_to_16(regNumber reg);

// Return an encoding for the specified 'V' register used in '19' thru '16' position.
static code_t insEncodeReg_V_19_to_16(regNumber reg);

// Return an encoding for the specified 'V' register used in '9' thru '6' position.
static code_t insEncodeReg_V_9_to_6(regNumber reg);

// Return an encoding for the specified 'V' register used in '9' thru '6' position with the times two encoding.
// This encoding requires that the register number be divisible by two.
static code_t insEncodeReg_V_9_to_6_Times_Two(regNumber reg);

// Returns an encoding for the imm which represents the condition code.
static code_t insEncodeCond(insCond cond);

// Returns an encoding for the imm which represents the 'condition code'
//  with the lowest bit inverted (marked by invert(<cond>) in the architecture manual.
static code_t insEncodeInvertedCond(insCond cond);

// Returns an encoding for the imm which represents the flags.
static code_t insEncodeFlags(insCflags flags);

// Returns the encoding for the Shift Count bits to be used for Arm64 encodings
static code_t insEncodeShiftCount(ssize_t imm, emitAttr size);

// Returns the encoding to select the datasize for most Arm64 instructions
static code_t insEncodeDatasize(emitAttr size);

// Returns the encoding to select the datasize for the general load/store Arm64 instructions
static code_t insEncodeDatasizeLS(code_t code, emitAttr size);

// Returns the encoding to select the datasize for the vector load/store Arm64 instructions
static code_t insEncodeDatasizeVLS(code_t code, emitAttr size);

// Returns the encoding to select the datasize for the vector load/store pair Arm64 instructions
static code_t insEncodeDatasizeVPLS(code_t code, emitAttr size);

// Returns the encoding to select the datasize for bitfield Arm64 instructions
static code_t insEncodeDatasizeBF(code_t code, emitAttr size);

// Returns the encoding to select the vectorsize for SIMD Arm64 instructions
static code_t insEncodeVectorsize(emitAttr size);

// Returns the encoding to set the vector length specifier (vl) for an Arm64 SVE instruction
static code_t insEncodeVectorLengthSpecifier(instrDesc* id);

// Returns the encoding to select 'index' for an Arm64 vector elem instruction
static code_t insEncodeVectorIndex(emitAttr elemsize, ssize_t index);

// Returns the encoding to select 'index2' for an Arm64 'ins' elem instruction
static code_t insEncodeVectorIndex2(emitAttr elemsize, ssize_t index2);

// Returns the encoding to select 'index' for an Arm64 'mul' elem instruction
static code_t insEncodeVectorIndexLMH(emitAttr elemsize, ssize_t index);

// Returns the encoding for a shift instruction, ready for insertion into an instruction.
static code_t insEncodeShiftImmediate(emitAttr size, bool isRightShift, ssize_t shiftAmount);

// Returns the encoding for ASIMD Shift instruction.
static code_t insEncodeVectorShift(emitAttr size, bool isRightShift, ssize_t shiftAmount)
{
    return insEncodeShiftImmediate(size, isRightShift, shiftAmount) << 16;
}

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 vector instruction
static code_t insEncodeElemsize(emitAttr size);

// Returns the encoding to select the 4/8 byte elemsize for an Arm64 float vector instruction
static code_t insEncodeFloatElemsize(emitAttr size);

// Returns the encoding to select the index for an Arm64 float vector by element instruction
static code_t insEncodeFloatIndex(emitAttr elemsize, ssize_t index);

// Returns the encoding to select the vector elemsize for an Arm64 ld/st# vector instruction
static code_t insEncodeVLSElemsize(emitAttr size);

// Returns the encoding to select the index for an Arm64 ld/st# vector by element instruction
static code_t insEncodeVLSIndex(emitAttr elemsize, ssize_t index);

// Returns the encoding to select the 'conversion' operation for a type 'fmt' Arm64 instruction
static code_t insEncodeConvertOpt(insFormat fmt, insOpts conversion);

// Returns the encoding to have the Rn register of a ld/st reg be Pre/Post/Not indexed updated
static code_t insEncodeIndexedOpt(insOpts opt);

// Returns the encoding to have the Rn register of a ld/st pair be Pre/Post/Not indexed updated
static code_t insEncodePairIndexedOpt(instruction ins, insOpts opt);

// Returns the encoding to apply a Shift Type on the Rm register
static code_t insEncodeShiftType(insOpts opt);

// Returns the encoding to apply a 12 bit left shift to the immediate
static code_t insEncodeShiftImm12(insOpts opt);

// Returns the encoding to have the Rm register use an extend operation
static code_t insEncodeExtend(insOpts opt);

// Returns the encoding to scale the Rm register by {0,1,2,3,4} in an extend operation
static code_t insEncodeExtendScale(ssize_t imm);

// Returns the encoding to have the Rm register be auto scaled by the ld/st size
static code_t insEncodeReg3Scale(bool isScaled);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 SVE vector instruction
static code_t insEncodeSveElemsize(emitAttr size);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
// This specifically encodes the size at bit locations '22-21'.
static code_t insEncodeSveElemsize_22_to_21(emitAttr size);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
// This specifically encodes the size at bit locations '18-17'.
static code_t insEncodeSveElemsize_18_to_17(emitAttr size);

// Returns the encoding to select the 4/8 byte elemsize for an Arm64 Sve vector instruction
// This specifically encodes the field 'sz' at bit location '21'.
static code_t insEncodeSveElemsize_sz_21(emitAttr size);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 SVE vector instruction
// This specifically encodes the field 'tszh:tszl' at bit locations '22:20-19'.
static code_t insEncodeSveElemsize_tszh_22_tszl_20_to_19(emitAttr size);

// Returns the encoding to select the 4/8 byte elemsize for an Arm64 Sve vector instruction at bit location '30' or
// '21'.
// This only works on select formats.
static code_t insEncodeSveElemsize_30_or_21(insFormat fmt, emitAttr size);

// Returns the encoding for the field 'i1:tszh:tszl' at bit locations '23-22:20-18'.
static code_t insEncodeSveElemsize_tszh_tszl_and_imm(const insOpts opt, const ssize_t imm);

// Returns the encoding for the field 'tszh:tszl:imm3' at bit locations '23-22:20-19:18-16'.
static code_t insEncodeSveElemsizeWithShift_tszh_tszl_imm3(const insOpts opt, ssize_t imm, bool isRightShift);

// Returns the encoding to select the constant values 90 or 270 for an Arm64 SVE vector instruction
// This specifically encode the field 'rot' at bit location '16'.
static code_t insEncodeSveImm90_or_270_rot(ssize_t imm);

// Returns the encoding to select the constant values 0, 90, 180 or 270 for an Arm64 SVE vector instruction
// This specifically encode the field 'rot' at bit locations '14-13'.
static code_t insEncodeSveImm0_to_270_rot(ssize_t imm);

// Returns the encoding to select the constant float values 0, 0.5, 1.0 or 2.0 for an Arm64 SVE vector instruction
// This specifically encode the field 'i1' at bit location '5'.
static code_t insEncodeSveSmallFloatImm(ssize_t imm);

// Returns the first register list size for the given SVE instruction.
static int insGetSveReg1ListSize(instruction ins);

// Returns the predicate type for the given SVE format.
// Register position is required for instructions with multiple predicates.
static PredicateType insGetPredicateType(insFormat fmt, int regpos = 0);

// Returns true if the SVE instruction has a LSL addr.
// This is for formats that have [<Xn|SP>, <Xm>, LSL #N], [<Xn|SP>{, <Xm>, LSL #N}]
static bool insSveIsLslN(instruction ins, insFormat fmt);

// Returns true if the SVE instruction has a <mod> addr.
// This is for formats that have [<Xn|SP>, <Zm>.T, <mod>], [<Xn|SP>, <Zm>.T, <mod> #N]
static bool insSveIsModN(instruction ins, insFormat fmt);

// Returns 0, 1, 2, 3 or 4 depending on the instruction and format.
// This is for formats that have [<Xn|SP>, <Zm>.T, <mod>], [<Xn|SP>, <Zm>.T, <mod> #N], [<Xn|SP>, <Xm>, LSL #N],
// [<Xn|SP>{, <Xm>, LSL #N}]
static int insSveGetLslOrModN(instruction ins, insFormat fmt);

// Returns true if the specified instruction can encode the 'dtype' field.
static bool canEncodeSveElemsize_dtype(instruction ins);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
// for the 'dtype' field.
static code_t insEncodeSveElemsize_dtype(instruction ins, emitAttr size, code_t code);

// Returns the encoding to select the 4/8/16 byte elemsize for the Arm64 Sve vector instruction 'ld1w'
// for the 'dtype' field.
static code_t insEncodeSveElemsize_dtype_ld1w(instruction ins, insFormat fmt, emitAttr size, code_t code);

// Returns the encoding to select the 1/2/4/8 byte elemsize for an Arm64 Sve vector instruction
// for the 'dtypeh' and 'dtypel' fields.
static code_t insEncodeSveElemsize_dtypeh_dtypel(instruction ins, insFormat fmt, emitAttr size, code_t code);

// Returns the encoding for the immediate value as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 9-bits at bit locations '21-16' for high and '12-10' for low.
static code_t insEncodeSimm9h9l_21_to_16_and_12_to_10(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 2 as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_MultipleOf2_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 3 as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_MultipleOf3_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 4 as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_MultipleOf4_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 16 as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_MultipleOf16_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 32 as 4-bits at bit locations '19-16'.
static code_t insEncodeSimm4_MultipleOf32_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 2 as 5-bits at bit locations '20-16'.
static code_t insEncodeUimm5_MultipleOf2_20_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 4 as 5-bits at bit locations '20-16'.
static code_t insEncodeUimm5_MultipleOf4_20_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 8 as 5-bits at bit locations '20-16'.
static code_t insEncodeUimm5_MultipleOf8_20_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 6-bits at bit locations '21-16'.
static code_t insEncodeSimm6_21_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 2 as 6-bits at bit locations '21-16'.
static code_t insEncodeUimm6_MultipleOf2_21_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 4 as 6-bits at bit locations '21-16'.
static code_t insEncodeUimm6_MultipleOf4_21_to_16(ssize_t imm);

// Returns the encoding for the immediate value that is a multiple of 8 as 6-bits at bit locations '21-16'.
static code_t insEncodeUimm6_MultipleOf8_21_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 5-bits at bit locations '20-16'.
static code_t insEncodeSimm5_20_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 2-bits at bit locations '9-8'.
static code_t insEncodeUimm2_9_to_8(ssize_t imm);

// Returns the encoding for the immediate value as 2-bits at bit locations '11-10'.
static code_t insEncodeUimm2_11_to_10(ssize_t imm);

// Returns the encoding for the immediate value as 2-bits at bit locations '20-19'.
static code_t insEncodeUimm2_20_to_19(ssize_t imm);

// Returns the encoding for the immediate value as 2-bits at bit locations '23-22'.
static code_t insEncodeUimm2_23_to_22(ssize_t imm);

// Returns the encoding for the immediate value as 1-bit at bit locations '23'.
static code_t insEncodeUimm1_23(ssize_t imm);

// Returns the encoding for the immediate value as 3-bits at bit locations '23-22' for high and '12' for low.
static code_t insEncodeUimm3h3l_23_to_22_and_12(ssize_t imm);

// Returns the encoding for the immediate value as 1 bit at bit location '10'.
static code_t insEncodeImm1_10(ssize_t imm);

// Returns the encoding for the immediate value as 1 bit at bit location '11'.
static code_t insEncodeImm1_11(ssize_t imm);

// Returns the encoding for the immediate value as 1 bit at bit location '22'.
static code_t insEncodeImm1_22(ssize_t imm);

// Returns the encoding for the immediate value as 7-bits at bit locations '20-14'.
static code_t insEncodeUimm7_20_to_14(ssize_t imm);

// Returns the encoding for the immediate value as 4-bits starting from 1, at bit locations '19-16'.
static code_t insEncodeUimm4From1_19_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 5-bits at bit locations '20-16'.
static code_t insEncodeUimm5_20_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 6-bits at bit locations '21-16'.
static code_t insEncodeUimm6_21_to_16(ssize_t imm);

// Returns the encoding for the immediate value as 8-bits at bit locations '12-5'.
static code_t insEncodeImm8_12_to_5(ssize_t imm);

// Returns the encoding for the unsigned immediate value as 3-bits at bit locations '18-16'.
static code_t insEncodeUimm3_18_to_16(ssize_t imm);

// Returns the encoding to select the elemsize for an Arm64 SVE vector instruction plus an immediate.
// This specifically encodes the field 'tszh:tszl' at bit locations '23-22:9-8'.
static code_t insEncodeSveShift_23_to_22_9_to_0(emitAttr size, bool isRightShift, size_t imm);

// Returns the encoding to select the 4/8-byte width specifier <R> at bit location 22
// for an Arm64 Sve instruction.
static code_t insEncodeSveElemsize_R_22(emitAttr size);

// Returns the immediate value for instructions that encode it as a difference from tszh:tszl:imm3.
static ssize_t insGetImmDiff(const ssize_t imm, const insOpts opt);

// Returns the encoding to select an insSvePattern
static code_t insEncodeSvePattern(insSvePattern pattern);

// Returns true if 'reg' represents an integer register.
static bool isIntegerRegister(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_INT_LAST);
}

//  Returns true if reg encodes for REG_SP or REG_FP
static bool isStackRegister(regNumber reg)
{
    return (reg == REG_ZR) || (reg == REG_FP);
} // ZR (R31) encodes the SP register

// Returns true if 'value' is a legal signed immediate 4 bit encoding (such as for LDNF1SW).
static bool isValidSimm4(ssize_t value)
{
    return (-8 <= value) && (value <= 7);
};

// Returns true if 'value' is a legal signed immediate 9 bit encoding (such as for LDR).
static bool isValidSimm9(ssize_t value)
{
    return (-0x100 <= value) && (value <= 0xFF);
};

// Returns true if 'value' is a legal signed multiple of 2 immediate 4 bit encoding (such as for LD2Q).
static bool isValidSimm4_MultipleOf2(ssize_t value)
{
    return (-16 <= value) && (value <= 14) && (value % 2 == 0);
};

// Returns true if 'value' is a legal signed multiple of 3 immediate 4 bit encoding (such as for LD3Q).
static bool isValidSimm4_MultipleOf3(ssize_t value)
{
    return (-24 <= value) && (value <= 21) && (value % 3 == 0);
};

// Returns true if 'value' is a legal signed multiple of 4 immediate 4 bit encoding (such as for LD4Q).
static bool isValidSimm4_MultipleOf4(ssize_t value)
{
    return (-32 <= value) && (value <= 28) && (value % 4 == 0);
};

// Returns true if 'value' is a legal signed multiple of 16 immediate 4 bit encoding (such as for LD1RQB).
static bool isValidSimm4_MultipleOf16(ssize_t value)
{
    return (-128 <= value) && (value <= 112) && (value % 16 == 0);
};

// Returns true if 'value' is a legal signed multiple of 32 immediate 4 bit encoding (such as for LD1ROB).
static bool isValidSimm4_MultipleOf32(ssize_t value)
{
    return (-256 <= value) && (value <= 224) && (value % 32 == 0);
};

// Returns true if 'value' is a legal unsigned multiple of 2 immediate 5 bit encoding (such as for LD1H).
static bool isValidUimm5_MultipleOf2(ssize_t value)
{
    return (0 <= value) && (value <= 62) && (value % 2 == 0);
};

// Returns true if 'value' is a legal unsigned multiple of 4 immediate 5 bit encoding (such as for LD1W).
static bool isValidUimm5_MultipleOf4(ssize_t value)
{
    return (0 <= value) && (value <= 124) && (value % 4 == 0);
};

// Returns true if 'value' is a legal unsigned multiple of 8 immediate 5 bit encoding (such as for LD1D).
static bool isValidUimm5_MultipleOf8(ssize_t value)
{
    return (0 <= value) && (value <= 248) && (value % 8 == 0);
};

// Returns true if 'value' is a legal signed multiple of 2 immediate 6 bit encoding (such as for LD1RH).
static bool isValidUimm6_MultipleOf2(ssize_t value)
{
    return (0 <= value) && (value <= 126) && (value % 2 == 0);
};

// Returns true if 'value' is a legal signed multiple of 4 immediate 6 bit encoding (such as for LD1RSW).
static bool isValidUimm6_MultipleOf4(ssize_t value)
{
    return (0 <= value) && (value <= 252) && (value % 4 == 0);
};

// Returns true if 'value' is a legal signed multiple of 8 immediate 6 bit encoding (such as for LD1RD).
static bool isValidUimm6_MultipleOf8(ssize_t value)
{
    return (0 <= value) && (value <= 504) && (value % 8 == 0);
};

// Returns true if 'value' is a legal immediate 1 bit encoding (such as for PEXT).
static bool isValidImm1(ssize_t value)
{
    return (value == 0) || (value == 1);
};

// Returns true if 'value' is a legal unsigned immediate 2 bit encoding (such as for PEXT).
static bool isValidUimm2(ssize_t value)
{
    return (0 <= value) || (value <= 3);
};

// Returns true if 'value' is a legal unsigned immediate 3 bit encoding.
static bool isValidUimm3(ssize_t value)
{
    return (0 <= value) && (value <= 7);
};

// Returns true if 'value' is a legal unsigned immediate 3 bit encoding, starting from 1 (such as for SHRNB).
static bool isValidUimm3From1(ssize_t value)
{
    return (1 <= value) && (value <= 8);
};

// Returns true if 'value' is a legal unsigned immediate 4 bit encoding.
static bool isValidUimm4(ssize_t value)
{
    return (0 <= value) && (value <= 0xF);
};

// Returns true if 'value' is a legal unsigned immediate 4 bit encoding, starting from 1 (such as for CNTB).
static bool isValidUimm4From1(ssize_t value)
{
    return (1 <= value) && (value <= 0x10);
};

// Returns true if 'value' is a legal unsigned immediate 5 bit encoding (such as for CCMP).
static bool isValidUimm5(ssize_t value)
{
    return (0 <= value) && (value <= 0x1FLL);
};

// Returns true if 'value' is a legal unsigned immediate 6 bit encoding (such as for LD1RD).
static bool isValidUimm6(ssize_t value)
{
    return (0 <= value) && (value <= 63);
}

// Returns true if 'value' is a legal unsigned immediate 5 bit encoding, starting from 1 (such as for SHRNB).
static bool isValidUimm5From1(ssize_t value)
{
    return (1 <= value) && (value <= 0x20);
};

// Returns true if 'value' is a legal unsigned immediate 7 bit encoding (such as for CMPLT, CMPNE).
static bool isValidUimm7(ssize_t value)
{
    return (0 <= value) && (value <= 0x7FLL);
};

// Returns true if 'value' is a legal unsigned immediate 8 bit encoding (such as for FMOV).
static bool isValidUimm8(ssize_t value)
{
    return (0 <= value) && (value <= 0xFFLL);
};

// Returns true if 'value' is a legal signed immediate 8 bit encoding (such as for SMAX, SMIN).
static bool isValidSimm8(ssize_t value)
{
    return (-0x80 <= value) && (value <= 0x7F);
};

// Returns true if 'value' is a legal unsigned immediate 12 bit encoding (such as for CMP, CMN).
static bool isValidUimm12(ssize_t value)
{
    return (0 <= value) && (value <= 0xFFFLL);
};

// Returns true if 'value' is a legal unsigned immediate 16 bit encoding (such as for MOVZ, MOVN, MOVK).
static bool isValidUimm16(ssize_t value)
{
    return (0 <= value) && (value <= 0xFFFFLL);
};

// Returns true if 'value' is a legal signed immediate 26 bit encoding (such as for B or BL).
static bool isValidSimm26(ssize_t value)
{
    return (-0x2000000LL <= value) && (value <= 0x1FFFFFFLL);
};

// Returns true if 'value' is a legal signed immediate 19 bit encoding (such as for B.cond, CBNZ, CBZ).
static bool isValidSimm19(ssize_t value)
{
    return (-0x40000LL <= value) && (value <= 0x3FFFFLL);
};

// Returns true if 'value' is a legal signed immediate 14 bit encoding (such as for TBNZ, TBZ).
static bool isValidSimm14(ssize_t value)
{
    return (-0x2000LL <= value) && (value <= 0x1FFFLL);
};

// Returns true if 'value' is a legal signed immediate 5 bit encoding (such as for CMPLO, CMPHI).
static bool isValidSimm5(ssize_t value)
{
    return (-0x10LL <= value) && (value <= 0xFLL);
};

// Returns true if 'value' is a legal signed immediate 6 bit encoding (such as for PRFB).
static bool isValidSimm6(ssize_t value)
{
    return (-32 <= value) && (value <= 31);
};

// Returns true if 'value' is a legal rotation value (such as for CDOT, CMLA).
static bool isValidRot(ssize_t value)
{
    return (value == 0) || (value == 90) || (value == 180) || (value == 270);
}

// Returns true if 'value' represents a valid 'bitmask immediate' encoding.
static bool isValidImmNRS(size_t value, emitAttr size)
{
    return (value >= 0) && (value < 0x2000);
} // any unsigned 13-bit immediate

// Returns true if 'value' represents a valid 'halfword immediate' encoding.
static bool isValidImmHWVal(size_t value, emitAttr size)
{
    return (value >= 0) && (value < 0x40000);
} // any unsigned 18-bit immediate

// Returns true if 'value' represents a valid 'byteShifted immediate' encoding.
static bool isValidImmBSVal(size_t value, emitAttr size)
{
    return (value >= 0) && (value < 0x800);
} // any unsigned 11-bit immediate

//  The return value replaces REG_ZR with REG_SP
static regNumber encodingZRtoSP(regNumber reg)
{
    return (reg == REG_ZR) ? REG_SP : reg;
} // ZR (R31) encodes the SP register

//  The return value replaces REG_SP with REG_ZR
static regNumber encodingSPtoZR(regNumber reg)
{
    return (reg == REG_SP) ? REG_ZR : reg;
} // SP is encoded using ZR (R31)

//  For the given 'ins' returns the reverse instruction, if one exists, otherwise returns INS_INVALID
static instruction insReverse(instruction ins);

//  For the given 'datasize' and 'elemsize' returns the insOpts that specifies the vector register arrangement
static insOpts optMakeArrangement(emitAttr datasize, emitAttr elemsize);

//    For the given 'datasize' and 'opt' returns true if it specifies a valid vector register arrangement
static bool isValidArrangement(emitAttr datasize, insOpts opt);

// Expands an option that has different size operands (INS_OPTS_*_TO_*) into a pair of scalable options where
// the first describes the size of the destination operand and the second describes the size of the source operand.
static void optExpandConversionPair(insOpts opt, insOpts& dst, insOpts& src);

//  For the given 'arrangement' returns the 'datasize' specified by the vector register arrangement
static emitAttr optGetDatasize(insOpts arrangement);

//  For the given 'arrangement' returns the 'elemsize' specified by the vector register arrangement
static emitAttr optGetElemsize(insOpts arrangement);

//  For the given 'arrangement' returns the 'elemsize' specified by the SVE vector register arrangement
static emitAttr optGetSveElemsize(insOpts arrangement);

//  For the given 'arrangement' returns the one with the element width that is double that of the 'arrangement' element.
static insOpts optWidenElemsizeArrangement(insOpts arrangement);

//  For the given SVE 'arrangement' returns the one with the element width that is double that of the 'arrangement'
//  element.
static insOpts optWidenSveElemsizeArrangement(insOpts arrangement);

//  For the given SVE 'arrangement', return the one when reduced to a quadword vector.
static insOpts optSveToQuadwordElemsizeArrangement(insOpts arrangement);

//  For the given 'datasize' returns the one that is double that of the 'datasize'.
static emitAttr widenDatasize(emitAttr datasize);

//  For the given 'srcArrangement' returns the "widen" 'dstArrangement' specifying the destination vector register
//  arrangement
//  of Long Pairwise instructions. Note that destination vector elements twice as long as the source vector elements.
static insOpts optWidenDstArrangement(insOpts srcArrangement);

//  For the given 'conversion' returns the 'dstsize' specified by the conversion option
static emitAttr optGetDstsize(insOpts conversion);

//  For the given 'conversion' returns the 'srcsize' specified by the conversion option
static emitAttr optGetSrcsize(insOpts conversion);

//    For the given 'datasize', 'elemsize' and 'index' returns true, if it specifies a valid 'index'
//    for an element of size 'elemsize' in a vector register of size 'datasize'
static bool isValidVectorIndex(emitAttr datasize, emitAttr elemsize, ssize_t index);

// For a given instruction 'ins' which contains a register lists returns a
// number of consecutive SIMD registers the instruction loads to/store from.
static unsigned insGetRegisterListSize(instruction ins);

/************************************************************************/
/*           Public inline informational methods                        */
/************************************************************************/

public:
// true if this 'imm' can be encoded as a input operand to a mov instruction
static bool emitIns_valid_imm_for_mov(INT64 imm, emitAttr size);

// true if this 'imm' can be encoded as a input operand to a vector movi instruction
static bool emitIns_valid_imm_for_movi(INT64 imm, emitAttr size);

// true if this 'immDbl' can be encoded as a input operand to a fmov instruction
static bool emitIns_valid_imm_for_fmov(double immDbl);

// true if this 'imm' can be encoded as a input operand to an add instruction
static bool emitIns_valid_imm_for_add(INT64 imm, emitAttr size = EA_8BYTE);

// true if this 'imm' can be encoded as a input operand to a cmp instruction
static bool emitIns_valid_imm_for_cmp(INT64 imm, emitAttr size);

// true if this 'imm' can be encoded as a input operand to an alu instruction
static bool emitIns_valid_imm_for_alu(INT64 imm, emitAttr size);

// true if this 'imm' can be encoded as the offset in a ldr/str instruction
static bool emitIns_valid_imm_for_ldst_offset(INT64 imm, emitAttr size);

// true if this 'imm' can be encoded as the offset in an unscaled ldr/str instruction
static bool emitIns_valid_imm_for_unscaled_ldst_offset(INT64 imm);

// true if this 'imm' can be encoded as a input operand to a ccmp instruction
static bool emitIns_valid_imm_for_ccmp(INT64 imm);

// true if 'imm' can be encoded as an offset in a ldp/stp instruction
static bool canEncodeLoadOrStorePairOffset(INT64 imm, emitAttr size);

// true if 'imm' can use the left shifted by 12 bits encoding
static bool canEncodeWithShiftImmBy12(INT64 imm);

// Normalize the 'imm' so that the upper bits, as defined by 'size' are zero
static INT64 normalizeImm64(INT64 imm, emitAttr size);

// Normalize the 'imm' so that the upper bits, as defined by 'size' are zero
static INT32 normalizeImm32(INT32 imm, emitAttr size);

// true if 'imm' can be encoded using a 'bitmask immediate', also returns the encoding if wbBMI is non-null
static bool canEncodeBitMaskImm(INT64 imm, emitAttr size, emitter::bitMaskImm* wbBMI = nullptr);

// true if 'imm' can be encoded using a 'halfword immediate', also returns the encoding if wbHWI is non-null
static bool canEncodeHalfwordImm(INT64 imm, emitAttr size, emitter::halfwordImm* wbHWI = nullptr);

// true if 'imm' can be encoded using a 'byteShifted immediate', also returns the encoding if wbBSI is non-null
static bool canEncodeByteShiftedImm(INT64 imm, emitAttr size, bool allow_MSL, emitter::byteShiftedImm* wbBSI = nullptr);

// true if 'immDbl' can be encoded using a 'float immediate', also returns the encoding if wbFPI is non-null
static bool canEncodeFloatImm8(double immDbl, emitter::floatImm8* wbFPI = nullptr);

// Returns the number of bits used by the given 'size'.
inline static unsigned getBitWidth(emitAttr size)
{
    assert(size <= EA_8BYTE);
    return (unsigned)size * BITS_PER_BYTE;
}

// Returns true if the imm represents a valid bit shift or bit position for the given 'size' [0..31] or [0..63]
inline static unsigned isValidImmShift(ssize_t imm, emitAttr size)
{
    return (imm >= 0) && (imm < getBitWidth(size));
}

// Returns true if the 'shiftAmount' represents a valid shift for the given 'size'.
inline static unsigned isValidVectorShiftAmount(ssize_t shiftAmount, emitAttr size, bool rightShift)
{
    return (rightShift && (shiftAmount >= 1) && (shiftAmount <= getBitWidth(size))) ||
           ((shiftAmount >= 0) && (shiftAmount < getBitWidth(size)));
}

inline static bool isValidGeneralDatasize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE);
}

inline static bool isValidScalarDatasize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE);
}

inline static bool isValidScalableDatasize(emitAttr size)
{
    return ((size & EA_SCALABLE) == EA_SCALABLE);
}

inline static bool isValidVectorDatasize(emitAttr size)
{
    return (size == EA_16BYTE) || (size == EA_8BYTE);
}

inline static bool isValidGeneralLSDatasize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE) || (size == EA_1BYTE);
}

inline static bool isValidVectorLSDatasize(emitAttr size)
{
    return (size == EA_16BYTE) || (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE) || (size == EA_1BYTE);
}

inline static bool isValidVectorLSPDatasize(emitAttr size)
{
    return (size == EA_16BYTE) || (size == EA_8BYTE) || (size == EA_4BYTE);
}

inline static bool isValidVectorElemsize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE) || (size == EA_1BYTE);
}

inline static bool isValidVectorFcvtsize(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE);
}

inline static bool isValidVectorElemsizeFloat(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE);
}

inline static bool isValidVectorElemsizeSveFloat(emitAttr size)
{
    return (size == EA_8BYTE) || (size == EA_4BYTE) || (size == EA_2BYTE);
}

inline static bool isValidVectorElemsizeWidening(emitAttr size)
{
    return (size == EA_4BYTE) || (size == EA_2BYTE) || (size == EA_1BYTE);
}

inline static bool isScalableVectorSize(emitAttr size)
{
    return (size == EA_SCALABLE);
}

inline static bool isGeneralRegister(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_LR);
} // Excludes REG_ZR

inline static bool isGeneralRegisterOrZR(regNumber reg)
{
    return (reg >= REG_INT_FIRST) && (reg <= REG_ZR);
} // Includes REG_ZR

inline static bool isGeneralRegisterOrSP(regNumber reg)
{
    return isGeneralRegister(reg) || (reg == REG_SP);
} // Includes REG_SP, Excludes REG_ZR

inline static bool isVectorRegister(regNumber reg)
{
    return (reg >= REG_FP_FIRST && reg <= REG_FP_LAST);
}

inline static bool isLowVectorRegister(regNumber reg)
{
    return (reg >= FIRST_FP_ARGREG) && (reg <= LAST_FP_ARGREG);
}

inline static bool isFloatReg(regNumber reg)
{
    return isVectorRegister(reg);
}

inline static bool isPredicateRegister(regNumber reg)
{
    return (reg >= REG_PREDICATE_FIRST) && (reg <= REG_PREDICATE_LAST);
}

inline static bool isLowPredicateRegister(regNumber reg)
{
    return (reg >= REG_PREDICATE_FIRST) && (reg <= REG_PREDICATE_LOW_LAST);
}

inline static bool isHighPredicateRegister(regNumber reg)
{
    return (reg >= REG_PREDICATE_HIGH_FIRST) && (reg <= REG_PREDICATE_HIGH_LAST);
}

inline static bool insOptsNone(insOpts opt)
{
    return (opt == INS_OPTS_NONE);
}

inline static bool insOptsIndexed(insOpts opt)
{
    return (opt == INS_OPTS_PRE_INDEX) || (opt == INS_OPTS_POST_INDEX);
}

inline static bool insOptsPreIndex(insOpts opt)
{
    return (opt == INS_OPTS_PRE_INDEX);
}

inline static bool insOptsPostIndex(insOpts opt)
{
    return (opt == INS_OPTS_POST_INDEX);
}

inline static bool insOptsLSL12(insOpts opt) // special 12-bit shift only used for imm12
{
    return (opt == INS_OPTS_LSL12);
}

inline static bool insOptsAnyShift(insOpts opt)
{
    return ((opt >= INS_OPTS_LSL) && (opt <= INS_OPTS_ROR));
}

inline static bool insOptsAluShift(insOpts opt) // excludes ROR
{
    return ((opt >= INS_OPTS_LSL) && (opt <= INS_OPTS_ASR));
}

inline static bool insOptsVectorImmShift(insOpts opt)
{
    return ((opt == INS_OPTS_LSL) || (opt == INS_OPTS_MSL));
}

inline static bool insOptsLSL(insOpts opt)
{
    return (opt == INS_OPTS_LSL);
}

inline static bool insOptsLSR(insOpts opt)
{
    return (opt == INS_OPTS_LSR);
}

inline static bool insOptsASR(insOpts opt)
{
    return (opt == INS_OPTS_ASR);
}

inline static bool insOptsROR(insOpts opt)
{
    return (opt == INS_OPTS_ROR);
}

inline static bool insOptsAnyExtend(insOpts opt)
{
    return ((opt >= INS_OPTS_UXTB) && (opt <= INS_OPTS_SXTX));
}

inline static bool insOptsLSExtend(insOpts opt)
{
    return ((opt == INS_OPTS_NONE) || (opt == INS_OPTS_LSL) || (opt == INS_OPTS_UXTW) || (opt == INS_OPTS_SXTW) ||
            (opt == INS_OPTS_UXTX) || (opt == INS_OPTS_SXTX));
}

inline static bool insOpts64BitExtend(insOpts opt)
{
    return ((opt == INS_OPTS_UXTX) || (opt == INS_OPTS_SXTX));
}

inline static bool insOptsAnyArrangement(insOpts opt)
{
    return ((opt >= INS_OPTS_8B) && (opt <= INS_OPTS_2D));
}

inline static bool insOptsConvertFloatStepwise(insOpts opt)
{
    return (opt == INS_OPTS_H_TO_S || opt == INS_OPTS_S_TO_H || opt == INS_OPTS_D_TO_S || opt == INS_OPTS_S_TO_D);
}

inline static bool insOptsConvertFloatToFloat(insOpts opt)
{
    return ((opt >= INS_OPTS_S_TO_D) && (opt <= INS_OPTS_D_TO_H));
}

inline static bool insOptsConvertFloatToInt(insOpts opt)
{
    return ((opt >= INS_OPTS_S_TO_4BYTE) && (opt <= INS_OPTS_D_TO_8BYTE));
}

inline static bool insOptsConvertIntToFloat(insOpts opt)
{
    return ((opt >= INS_OPTS_4BYTE_TO_S) && (opt <= INS_OPTS_8BYTE_TO_D));
}

inline static bool insOptsScalable(insOpts opt)
{
    // `opt` is any of the scalable types.
    return ((opt == INS_OPTS_SCALABLE_B) || (opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_S) ||
            (opt == INS_OPTS_SCALABLE_D) || (opt == INS_OPTS_SCALABLE_Q));
}

inline static bool insOptsScalableStandard(insOpts opt)
{
    // `opt` is any of the scalable types, except Quadword.
    return ((opt == INS_OPTS_SCALABLE_B) || (opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_S) ||
            (opt == INS_OPTS_SCALABLE_D));
}

inline static bool insOptsScalableWords(insOpts opt)
{
    // `opt` is any of the word and above scalable types.
    return ((opt == INS_OPTS_SCALABLE_S) || (opt == INS_OPTS_SCALABLE_D));
}

inline static bool insOptsScalableWordsOrQuadwords(insOpts opt)
{
    // `opt` is any of the word, quadword and above scalable types.
    return (insOptsScalableWords(opt) || (opt == INS_OPTS_SCALABLE_Q));
}

inline static bool insOptsScalableDoubleWordsOrQuadword(insOpts opt)
{
    // `opt` is a double-word or quad-word.
    return ((opt == INS_OPTS_SCALABLE_D) || (opt == INS_OPTS_SCALABLE_Q));
}

inline static bool insOptsScalableAtLeastHalf(insOpts opt)
{
    // `opt` is any of the half and above scalable types.
    return ((opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_S) || (opt == INS_OPTS_SCALABLE_D));
}

inline static bool insOptsScalableAtMaxHalf(insOpts opt)
{
    // `opt` is any of the standard half and below scalable types.
    return ((opt == INS_OPTS_SCALABLE_B) || (opt == INS_OPTS_SCALABLE_H));
}

inline static bool insOptsScalableFloat(insOpts opt)
{
    // `opt` is any of the scalable types that are valid for FP.
    return ((opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_S) || (opt == INS_OPTS_SCALABLE_D));
}

inline static bool insOptsScalableWide(insOpts opt)
{
    // `opt` is any of the scalable types that are valid for widening to size D.
    return ((opt == INS_OPTS_SCALABLE_B) || (opt == INS_OPTS_SCALABLE_H) || (opt == INS_OPTS_SCALABLE_S));
}

inline static bool insOptsScalable32bitExtends(insOpts opt)
{
    return insOptsScalableSingleWord32bitExtends(opt) || insOptsScalableDoubleWord32bitExtends(opt);
}

inline static bool insOptsScalableSingleWord32bitExtends(insOpts opt)
{
    return (opt == INS_OPTS_SCALABLE_S_UXTW) || (opt == INS_OPTS_SCALABLE_S_SXTW);
}

inline static bool insOptsScalableDoubleWord32bitExtends(insOpts opt)
{
    return (opt == INS_OPTS_SCALABLE_D_UXTW) || (opt == INS_OPTS_SCALABLE_D_SXTW);
}

inline static bool insScalableOptsNone(insScalableOpts sopt)
{
    // `sopt` is used for instructions with no extra encoding variants.
    return sopt == INS_SCALABLE_OPTS_NONE;
}

inline static bool insScalableOptsWithPredicatePair(insScalableOpts sopt)
{
    // `sopt` denotes the instruction's predicate register should be encoded as a {<Pd1>.<T>, <Pd2>.<T>} pair.
    return sopt == INS_SCALABLE_OPTS_WITH_PREDICATE_PAIR;
}

inline static bool insScalableOptsWithVectorLength(insScalableOpts sopt)
{
    // `sopt` is any of the scalable types that are valid for use with instructions with a vector length specifier (vl).
    return ((sopt == INS_SCALABLE_OPTS_VL_2X) || (sopt == INS_SCALABLE_OPTS_VL_4X));
}

static bool isValidImmCond(ssize_t imm);
static bool isValidImmCondFlags(ssize_t imm);
static bool isValidImmCondFlagsImm5(ssize_t imm);

// Computes page "delta" between two addresses
inline static ssize_t computeRelPageAddr(size_t dstAddr, size_t srcAddr)
{
    return (dstAddr >> 12) - (srcAddr >> 12);
}

/************************************************************************/
/*                   Output target-independent instructions             */
/************************************************************************/

void emitIns_J(instruction ins, BasicBlock* dst, int instrCount = 0);

/************************************************************************/
/*           The public entry points to output instructions             */
/************************************************************************/

public:
void emitIns(instruction ins);

void emitIns_I(instruction ins, emitAttr attr, ssize_t imm);

void emitIns_R(instruction ins, emitAttr attr, regNumber reg, insOpts opt = INS_OPTS_NONE);

void emitIns_R_I(instruction     ins,
                 emitAttr        attr,
                 regNumber       reg,
                 ssize_t         imm,
                 insOpts         opt  = INS_OPTS_NONE,
                 insScalableOpts sopt = INS_SCALABLE_OPTS_NONE DEBUGARG(size_t targetHandle = 0)
                     DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_R_F(instruction ins, emitAttr attr, regNumber reg, double immDbl, insOpts opt = INS_OPTS_NONE);

void emitIns_Mov(
    instruction ins, emitAttr attr, regNumber dstReg, regNumber srcReg, bool canSkip, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R(instruction     ins,
                 emitAttr        attr,
                 regNumber       reg1,
                 regNumber       reg2,
                 insOpts         opt  = INS_OPTS_NONE,
                 insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insFlags flags)
{
    emitIns_R_R(ins, attr, reg1, reg2);
}

void emitIns_R_I_I(instruction ins,
                   emitAttr    attr,
                   regNumber   reg1,
                   ssize_t     imm1,
                   ssize_t     imm2,
                   insOpts opt = INS_OPTS_NONE DEBUGARG(size_t targetHandle = 0)
                       DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_R_R_I(instruction     ins,
                   emitAttr        attr,
                   regNumber       reg1,
                   regNumber       reg2,
                   ssize_t         imm,
                   insOpts         opt  = INS_OPTS_NONE,
                   insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R_F(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, double immDbl, insOpts opt = INS_OPTS_NONE);

// Checks for a large immediate that needs a second instruction
void emitIns_R_R_Imm(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, ssize_t imm);

void emitIns_R_R_R(instruction     ins,
                   emitAttr        attr,
                   regNumber       reg1,
                   regNumber       reg2,
                   regNumber       reg3,
                   insOpts         opt  = INS_OPTS_NONE,
                   insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R_R_I(instruction     ins,
                     emitAttr        attr,
                     regNumber       reg1,
                     regNumber       reg2,
                     regNumber       reg3,
                     ssize_t         imm,
                     insOpts         opt      = INS_OPTS_NONE,
                     emitAttr        attrReg2 = EA_UNKNOWN,
                     insScalableOpts sopt     = INS_SCALABLE_OPTS_NONE);

void emitInsSve_R_R_R_I(instruction     ins,
                        emitAttr        attr,
                        regNumber       reg1,
                        regNumber       reg2,
                        regNumber       reg3,
                        ssize_t         imm,
                        insOpts         opt  = INS_OPTS_NONE,
                        insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R_R_I_I(instruction ins,
                       emitAttr    attr,
                       regNumber   reg1,
                       regNumber   reg2,
                       regNumber   reg3,
                       ssize_t     imm1,
                       ssize_t     imm2,
                       insOpts     opt);

void emitIns_R_R_R_Ext(instruction ins,
                       emitAttr    attr,
                       regNumber   reg1,
                       regNumber   reg2,
                       regNumber   reg3,
                       insOpts     opt         = INS_OPTS_NONE,
                       int         shiftAmount = -1);

void emitIns_R_R_I_I(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, int imm1, int imm2, insOpts opt = INS_OPTS_NONE);

void emitIns_R_R_R_R(instruction     ins,
                     emitAttr        attr,
                     regNumber       reg1,
                     regNumber       reg2,
                     regNumber       reg3,
                     regNumber       reg4,
                     insOpts         opt  = INS_OPTS_NONE,
                     insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitInsSve_R_R_R_R(instruction     ins,
                        emitAttr        attr,
                        regNumber       reg1,
                        regNumber       reg2,
                        regNumber       reg3,
                        regNumber       reg4,
                        insOpts         opt  = INS_OPTS_NONE,
                        insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_R_R_R_R_I(instruction ins,
                       emitAttr    attr,
                       regNumber   reg1,
                       regNumber   reg2,
                       regNumber   reg3,
                       regNumber   reg4,
                       ssize_t     imm,
                       insOpts     opt = INS_OPTS_NONE);

void emitIns_R_COND(instruction ins, emitAttr attr, regNumber reg, insCond cond);

void emitIns_R_R_COND(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insCond cond);

void emitIns_R_R_R_COND(instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, regNumber reg3, insCond cond);

void emitIns_R_R_FLAGS_COND(
    instruction ins, emitAttr attr, regNumber reg1, regNumber reg2, insCflags flags, insCond cond);

void emitIns_R_I_FLAGS_COND(instruction ins, emitAttr attr, regNumber reg1, int imm, insCflags flags, insCond cond);

void emitIns_R_PATTERN(
    instruction ins, emitAttr attr, regNumber reg1, insOpts opt, insSvePattern pattern = SVE_PATTERN_ALL);

void emitIns_R_PATTERN_I(instruction ins, emitAttr attr, regNumber reg1, insSvePattern pattern, int imm);

void emitIns_PRFOP_R_R_R(instruction     ins,
                         emitAttr        attr,
                         insSvePrfop     prfop,
                         regNumber       reg1,
                         regNumber       reg2,
                         regNumber       reg3,
                         insOpts         opt  = INS_OPTS_NONE,
                         insScalableOpts sopt = INS_SCALABLE_OPTS_NONE);

void emitIns_PRFOP_R_R_I(instruction ins,
                         emitAttr    attr,
                         insSvePrfop prfop,
                         regNumber   reg1,
                         regNumber   reg2,
                         int         imm,
                         insOpts     opt = INS_OPTS_NONE);

void emitIns_BARR(instruction ins, insBarrier barrier);

void emitIns_C(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, int offs);

void emitIns_S(instruction ins, emitAttr attr, int varx, int offs);

void emitIns_S_R(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_S_S_R_R(
    instruction ins, emitAttr attr, emitAttr attr2, regNumber ireg, regNumber ireg2, int varx, int offs);

void emitIns_R_R_R_I_LdStPair(instruction ins,
                              emitAttr    attr,
                              emitAttr    attr2,
                              regNumber   reg1,
                              regNumber   reg2,
                              regNumber   reg3,
                              ssize_t     imm,
                              int         varx1 = -1,
                              int         varx2 = -1,
                              int         offs1 = -1,
                              int offs2 = -1 DEBUG_ARG(unsigned var1RefsOffs = BAD_IL_OFFSET)
                                              DEBUG_ARG(unsigned var2RefsOffs = BAD_IL_OFFSET));

void emitIns_R_S(instruction ins, emitAttr attr, regNumber ireg, int varx, int offs);

void emitIns_R_R_S_S(
    instruction ins, emitAttr attr, emitAttr attr2, regNumber ireg, regNumber ireg2, int varx, int offs);

void emitIns_S_I(instruction ins, emitAttr attr, int varx, int offs, int val);

void emitIns_R_C(
    instruction ins, emitAttr attr, regNumber reg, regNumber tmpReg, CORINFO_FIELD_HANDLE fldHnd, int offs);

void emitIns_C_R(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fldHnd, regNumber reg, int offs);

void emitIns_C_I(instruction ins, emitAttr attr, CORINFO_FIELD_HANDLE fdlHnd, ssize_t offs, ssize_t val);

void emitIns_R_L(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_R_D(instruction ins, emitAttr attr, unsigned offs, regNumber reg);

void emitIns_J_R(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg);

void emitIns_J_R_I(instruction ins, emitAttr attr, BasicBlock* dst, regNumber reg, int imm);

void emitIns_I_AR(instruction ins, emitAttr attr, int val, regNumber reg, int offs);

void emitIns_R_AR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_Adrp_Ldr_Add(emitAttr  attr,
                          regNumber reg1,
                          regNumber reg2,
                          ssize_t addr DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_R_AI(instruction ins,
                  emitAttr    attr,
                  regNumber   ireg,
                  ssize_t disp DEBUGARG(size_t targetHandle = 0) DEBUGARG(GenTreeFlags gtFlags = GTF_EMPTY));

void emitIns_AR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, int offs);

void emitIns_R_ARR(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_ARR_R(instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, int disp);

void emitIns_R_ARX(
    instruction ins, emitAttr attr, regNumber ireg, regNumber reg, regNumber rg2, unsigned mul, int disp);

enum EmitCallType
{
    EC_FUNC_TOKEN, // Direct call to a helper/static/nonvirtual/global method
    EC_INDIR_R,    // Indirect call via register
    EC_COUNT
};

void emitIns_Call(EmitCallType          callType,
                  CORINFO_METHOD_HANDLE methHnd,
                  INDEBUG_LDISASM_COMMA(CORINFO_SIG_INFO* sigInfo) // used to report call sites to the EE
                  void*            addr,
                  ssize_t          argSize,
                  emitAttr         retSize,
                  emitAttr         secondRetSize,
                  VARSET_VALARG_TP ptrVars,
                  regMaskTP        gcrefRegs,
                  regMaskTP        byrefRegs,
                  const DebugInfo& di,
                  regNumber        ireg,
                  regNumber        xreg,
                  unsigned         xmul,
                  ssize_t          disp,
                  bool             isJump);

BYTE* emitOutputLJ(insGroup* ig, BYTE* dst, instrDesc* i);
unsigned emitOutputCall(insGroup* ig, BYTE* dst, instrDesc* i, code_t code);
BYTE* emitOutputLoadLabel(BYTE* dst, BYTE* srcAddr, BYTE* dstAddr, instrDescJmp* id);
BYTE* emitOutputShortBranch(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, instrDescJmp* id);
BYTE* emitOutputShortAddress(BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, regNumber reg);
BYTE* emitOutputShortConstant(
    BYTE* dst, instruction ins, insFormat fmt, ssize_t distVal, regNumber reg, emitAttr opSize);
BYTE* emitOutputVectorConstant(
    BYTE* dst, ssize_t distVal, regNumber dstReg, regNumber addrReg, emitAttr opSize, emitAttr elemSize);

/*****************************************************************************
 *
 *  Given an instrDesc, return true if it's a conditional jump.
 */

inline bool emitIsCondJump(instrDesc* jmp)
{
    return ((jmp->idInsFmt() == IF_BI_0B) || (jmp->idInsFmt() == IF_BI_1A) || (jmp->idInsFmt() == IF_BI_1B) ||
            (jmp->idInsFmt() == IF_LARGEJMP));
}

/*****************************************************************************
 *
 *  Given a instrDesc, return true if it's an unconditional jump.
 */

inline bool emitIsUncondJump(instrDesc* jmp)
{
    return (jmp->idInsFmt() == IF_BI_0A);
}

/*****************************************************************************
 *
 *  Given a instrDesc, return true if it's a direct call.
 */

inline bool emitIsDirectCall(instrDesc* call)
{
    return (call->idInsFmt() == IF_BI_0C);
}

/*****************************************************************************
 *
 *  Given a instrDesc, return true if it's a load label instruction.
 */

inline bool emitIsLoadLabel(instrDesc* jmp)
{
    return ((jmp->idInsFmt() == IF_DI_1E) || // adr or arp
            (jmp->idInsFmt() == IF_LARGEADR));
}

/*****************************************************************************
*
*  Given a instrDesc, return true if it's a load constant instruction.
*/

inline bool emitIsLoadConstant(instrDesc* jmp)
{
    return ((jmp->idInsFmt() == IF_LS_1A) || // ldr
            (jmp->idInsFmt() == IF_LARGELDC));
}

#endif // TARGET_ARM64
