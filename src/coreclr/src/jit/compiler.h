// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           Compiler                                        XX
XX                                                                           XX
XX  Represents the method data we are currently JIT-compiling.               XX
XX  An instance of this class is created for every method we JIT.            XX
XX  This contains all the info needed for the method. So allocating a        XX
XX  a new instance per method makes it thread-safe.                          XX
XX  It should be used to do all the memory management for the compiler run.  XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#ifndef _COMPILER_H_
#define _COMPILER_H_
/*****************************************************************************/

#include "jit.h"
#include "opcode.h"
#include "varset.h"
#include "gentree.h"
#include "lir.h"
#include "block.h"
#include "inline.h"
#include "jiteh.h"
#include "instr.h"
#include "regalloc.h"
#include "sm.h"
#include "simplerhash.h"
#include "cycletimer.h"
#include "blockset.h"
#include "jitstd.h"
#include "arraystack.h"
#include "hashbv.h"
#include "fp.h"
#include "expandarray.h"
#include "tinyarray.h"
#include "valuenum.h"
#include "reglist.h"
#include "jittelemetry.h"
#ifdef LATE_DISASM
#include "disasm.h"
#endif

#include "codegeninterface.h"
#include "regset.h"
#include "jitgcinfo.h"

#if DUMP_GC_TABLES && defined(JIT32_GCENCODER)
#include "gcdump.h"
#endif

#include "emit.h"

#include "simd.h"

// This is only used locally in the JIT to indicate that
// a verification block should be inserted
#define SEH_VERIFICATION_EXCEPTION 0xe0564552 // VER

/*****************************************************************************
 *                  Forward declarations
 */

struct InfoHdr;            // defined in GCInfo.h
struct escapeMapping_t;    // defined in flowgraph.cpp
class emitter;             // defined in emit.h
struct ShadowParamVarInfo; // defined in GSChecks.cpp
struct InitVarDscInfo;     // defined in register_arg_convention.h
class FgStack;             // defined in flowgraph.cpp
#if FEATURE_STACK_FP_X87
struct FlatFPStateX87; // defined in fp.h
#endif
#if FEATURE_ANYCSE
class CSE_DataFlow; // defined in OptCSE.cpp
#endif
#ifdef DEBUG
struct IndentStack;
#endif

// The following are defined in this file, Compiler.h

class Compiler;

/*****************************************************************************
 *                  Unwind info
 */

#include "unwind.h"

/*****************************************************************************/

//
// Declare global operator new overloads that use the Compiler::compGetMem() function for allocation.
//

// Or the more-general IAllocator interface.
void* __cdecl operator new(size_t n, IAllocator* alloc);
void* __cdecl operator new[](size_t n, IAllocator* alloc);

// I wanted to make the second argument optional, with default = CMK_Unknown, but that
// caused these to be ambiguous with the global placement new operators.
void* __cdecl operator new(size_t n, Compiler* context, CompMemKind cmk);
void* __cdecl operator new[](size_t n, Compiler* context, CompMemKind cmk);
void* __cdecl operator new(size_t n, void* p, const jitstd::placement_t& syntax_difference);

// Requires the definitions of "operator new" so including "LoopCloning.h" after the definitions.
#include "loopcloning.h"

/*****************************************************************************/

/* This is included here and not earlier as it needs the definition of "CSE"
 * which is defined in the section above */

/*****************************************************************************/

unsigned genLog2(unsigned value);
unsigned genLog2(unsigned __int64 value);

var_types genActualType(var_types type);
var_types genUnsignedType(var_types type);
var_types genSignedType(var_types type);

unsigned ReinterpretHexAsDecimal(unsigned);

/*****************************************************************************/

#ifdef FEATURE_SIMD
#ifdef FEATURE_AVX_SUPPORT
const unsigned TEMP_MAX_SIZE = YMM_REGSIZE_BYTES;
#else  // !FEATURE_AVX_SUPPORT
const unsigned TEMP_MAX_SIZE = XMM_REGSIZE_BYTES;
#endif // !FEATURE_AVX_SUPPORT
#else  // !FEATURE_SIMD
const unsigned TEMP_MAX_SIZE = sizeof(double);
#endif // !FEATURE_SIMD
const unsigned TEMP_SLOT_COUNT = (TEMP_MAX_SIZE / sizeof(int));

const unsigned FLG_CCTOR = (CORINFO_FLG_CONSTRUCTOR | CORINFO_FLG_STATIC);

#ifdef DEBUG
const int BAD_STK_OFFS = 0xBAADF00D; // for LclVarDsc::lvStkOffs
#endif

// The following holds the Local var info (scope information)
typedef const char* VarName; // Actual ASCII string
struct VarScopeDsc
{
    IL_OFFSET vsdLifeBeg; // instr offset of beg of life
    IL_OFFSET vsdLifeEnd; // instr offset of end of life
    unsigned  vsdVarNum;  // (remapped) LclVarDsc number

#ifdef DEBUG
    VarName vsdName; // name of the var
#endif

    unsigned vsdLVnum; // 'which' in eeGetLVinfo().
                       // Also, it is the index of this entry in the info.compVarScopes array,
                       // which is useful since the array is also accessed via the
                       // compEnterScopeList and compExitScopeList sorted arrays.
};

/*****************************************************************************
 *
 *  The following holds the local variable counts and the descriptor table.
 */

// This is the location of a definition.
struct DefLoc
{
    BasicBlock* m_blk;
    GenTreePtr  m_tree;

    DefLoc() : m_blk(nullptr), m_tree(nullptr)
    {
    }
};

// This class encapsulates all info about a local variable that may vary for different SSA names
// in the family.
class LclSsaVarDsc
{
public:
    ValueNumPair m_vnPair;
    DefLoc       m_defLoc;

    LclSsaVarDsc()
    {
    }
};

typedef ExpandArray<LclSsaVarDsc> PerSsaArray;

class LclVarDsc
{
public:
    // The constructor. Most things can just be zero'ed.
    LclVarDsc(Compiler* comp);

    // note this only packs because var_types is a typedef of unsigned char
    var_types lvType : 5; // TYP_INT/LONG/FLOAT/DOUBLE/REF

    unsigned char lvIsParam : 1;           // is this a parameter?
    unsigned char lvIsRegArg : 1;          // is this a register argument?
    unsigned char lvFramePointerBased : 1; // 0 = off of REG_SPBASE (e.g., ESP), 1 = off of REG_FPBASE (e.g., EBP)

    unsigned char lvStructGcCount : 3; // if struct, how many GC pointer (stop counting at 7). The only use of values >1
                                       // is to help determine whether to use block init in the prolog.
    unsigned char lvOnFrame : 1;       // (part of) the variable lives on the frame
    unsigned char lvDependReg : 1;     // did the predictor depend upon this being enregistered
    unsigned char lvRegister : 1;      // assigned to live in a register? For RyuJIT backend, this is only set if the
                                       // variable is in the same register for the entire function.
    unsigned char lvTracked : 1;       // is this a tracked variable?
    bool          lvTrackedNonStruct()
    {
        return lvTracked && lvType != TYP_STRUCT;
    }
    unsigned char lvPinned : 1; // is this a pinned variable?

    unsigned char lvMustInit : 1;    // must be initialized
    unsigned char lvAddrExposed : 1; // The address of this variable is "exposed" -- passed as an argument, stored in a
                                     // global location, etc.
                                     // We cannot reason reliably about the value of the variable.
    unsigned char lvDoNotEnregister : 1; // Do not enregister this variable.
    unsigned char lvFieldAccessed : 1;   // The var is a struct local, and a field of the variable is accessed.  Affects
                                         // struct promotion.

#ifdef DEBUG
    // These further document the reasons for setting "lvDoNotEnregister".  (Note that "lvAddrExposed" is one of the
    // reasons;
    // also, lvType == TYP_STRUCT prevents enregistration.  At least one of the reasons should be true.
    unsigned char lvVMNeedsStackAddr : 1; // The VM may have access to a stack-relative address of the variable, and
                                          // read/write its value.
    unsigned char lvLiveInOutOfHndlr : 1; // The variable was live in or out of an exception handler, and this required
                                          // the variable to be
                                          // in the stack (at least at those boundaries.)
    unsigned char lvLclFieldExpr : 1;     // The variable is not a struct, but was accessed like one (e.g., reading a
                                          // particular byte from an int).
    unsigned char lvLclBlockOpAddr : 1;   // The variable was written to via a block operation that took its address.
    unsigned char lvLiveAcrossUCall : 1;  // The variable is live across an unmanaged call.
#endif
    unsigned char lvIsCSE : 1;       // Indicates if this LclVar is a CSE variable.
    unsigned char lvRefAssign : 1;   // involved in pointer assignment
    unsigned char lvHasLdAddrOp : 1; // has ldloca or ldarga opcode on this local.
    unsigned char lvStackByref : 1;  // This is a compiler temporary of TYP_BYREF that is known to point into our local
                                     // stack frame.

    unsigned char lvArgWrite : 1; // variable is a parameter and STARG was used on it
    unsigned char lvIsTemp : 1;   // Short-lifetime compiler temp
#if OPT_BOOL_OPS
    unsigned char lvIsBoolean : 1; // set if variable is boolean
#endif
    unsigned char lvRngOptDone : 1; // considered for range check opt?
    unsigned char lvLoopInc : 1;    // incremented in the loop?
    unsigned char lvLoopAsg : 1;    // reassigned  in the loop (other than a monotonic inc/dec for the index var)?
    unsigned char lvArrIndx : 1;    // used as an array index?
    unsigned char lvArrIndxOff : 1; // used as an array index with an offset?
    unsigned char lvArrIndxDom : 1; // index dominates loop exit
#if ASSERTION_PROP
    unsigned char lvSingleDef : 1;    // variable has a single def
    unsigned char lvDisqualify : 1;   // variable is no longer OK for add copy optimization
    unsigned char lvVolatileHint : 1; // hint for AssertionProp
#endif
#if FANCY_ARRAY_OPT
    unsigned char lvAssignOne : 1; // assigned at least  once?
    unsigned char lvAssignTwo : 1; // assigned at least twice?
#endif

    unsigned char lvSpilled : 1; // enregistered variable was spilled
#ifndef _TARGET_64BIT_
    unsigned char lvStructDoubleAlign : 1; // Must we double align this struct?
#endif                                     // !_TARGET_64BIT_
#ifdef _TARGET_64BIT_
    unsigned char lvQuirkToLong : 1; // Quirk to allocate this LclVar as a 64-bit long
#endif
#ifdef DEBUG
    unsigned char lvKeepType : 1;       // Don't change the type of this variable
    unsigned char lvNoLclFldStress : 1; // Can't apply local field stress on this one
#endif
    unsigned char lvIsPtr : 1; // Might this be used in an address computation? (used by buffer overflow security
                               // checks)
    unsigned char lvIsUnsafeBuffer : 1; // Does this contain an unsafe buffer requiring buffer overflow security checks?
    unsigned char lvPromoted : 1; // True when this local is a promoted struct, a normed struct, or a "split" long on a
                                  // 32-bit target.
    unsigned char lvIsStructField : 1;          // Is this local var a field of a promoted struct local?
    unsigned char lvContainsFloatingFields : 1; // Does this struct contains floating point fields?
    unsigned char lvOverlappingFields : 1;      // True when we have a struct with possibly overlapping fields
    unsigned char lvContainsHoles : 1;          // True when we have a promoted struct that contains holes
    unsigned char lvCustomLayout : 1;           // True when this struct has "CustomLayout"

    unsigned char lvIsMultiRegArg : 1; // true if this is a multireg LclVar struct used in an argument context
    unsigned char lvIsMultiRegRet : 1; // true if this is a multireg LclVar struct assigned from a multireg call

#ifdef FEATURE_HFA
    unsigned char _lvIsHfa : 1;          // Is this a struct variable who's class handle is an HFA type
    unsigned char _lvIsHfaRegArg : 1;    // Is this a HFA argument variable?    // TODO-CLEANUP: Remove this and replace
                                         // with (lvIsRegArg && lvIsHfa())
    unsigned char _lvHfaTypeIsFloat : 1; // Is the HFA type float or double?
#endif                                   // FEATURE_HFA

#ifdef DEBUG
    // TODO-Cleanup: See the note on lvSize() - this flag is only in use by asserts that are checking for struct
    // types, and is needed because of cases where TYP_STRUCT is bashed to an integral type.
    // Consider cleaning this up so this workaround is not required.
    unsigned char lvUnusedStruct : 1; // All references to this promoted struct are through its field locals.
                                      // I.e. there is no longer any reference to the struct directly.
                                      // In this case we can simply remove this struct local.
#endif
#ifndef LEGACY_BACKEND
    unsigned char lvLRACandidate : 1; // Tracked for linear scan register allocation purposes
#endif                                // !LEGACY_BACKEND

#ifdef FEATURE_SIMD
    // Note that both SIMD vector args and locals are marked as lvSIMDType = true, but the
    // type of an arg node is TYP_BYREF and a local node is TYP_SIMD*.
    unsigned char lvSIMDType : 1;            // This is a SIMD struct
    unsigned char lvUsedInSIMDIntrinsic : 1; // This tells lclvar is used for simd intrinsic
#endif                                       // FEATURE_SIMD
    unsigned char lvRegStruct : 1;           // This is a reg-sized non-field-addressed struct.

    union {
        unsigned lvFieldLclStart; // The index of the local var representing the first field in the promoted struct
                                  // local.
        unsigned lvParentLcl; // The index of the local var representing the parent (i.e. the promoted struct local).
                              // Valid on promoted struct local fields.
#ifdef FEATURE_SIMD
        var_types lvBaseType; // The base type of a SIMD local var.  Valid on TYP_SIMD locals.
#endif                        // FEATURE_SIMD
    };

    unsigned char lvFieldCnt; //  Number of fields in the promoted VarDsc.
    unsigned char lvFldOffset;
    unsigned char lvFldOrdinal;

#if FEATURE_MULTIREG_ARGS
    regNumber lvRegNumForSlot(unsigned slotNum)
    {
        if (slotNum == 0)
        {
            return lvArgReg;
        }
        else if (slotNum == 1)
        {
            return lvOtherArgReg;
        }
        else
        {
            assert(false && "Invalid slotNum!");
        }

        unreached();
    }
#endif // FEATURE_MULTIREG_ARGS

    bool lvIsHfa() const
    {
#ifdef FEATURE_HFA
        return _lvIsHfa;
#else
        return false;
#endif
    }

    void lvSetIsHfa()
    {
#ifdef FEATURE_HFA
        _lvIsHfa = true;
#endif
    }

    bool lvIsHfaRegArg() const
    {
#ifdef FEATURE_HFA
        return _lvIsHfaRegArg;
#else
        return false;
#endif
    }

    void lvSetIsHfaRegArg()
    {
#ifdef FEATURE_HFA
        _lvIsHfaRegArg = true;
#endif
    }

    bool lvHfaTypeIsFloat() const
    {
#ifdef FEATURE_HFA
        return _lvHfaTypeIsFloat;
#else
        return false;
#endif
    }

    void lvSetHfaTypeIsFloat(bool value)
    {
#ifdef FEATURE_HFA
        _lvHfaTypeIsFloat = value;
#endif
    }

    // on Arm64 - Returns 1-4 indicating the number of register slots used by the HFA
    // on Arm32 - Returns the total number of single FP register slots used by the HFA, max is 8
    //
    unsigned lvHfaSlots() const
    {
        assert(lvIsHfa());
        assert(lvType == TYP_STRUCT);
#ifdef _TARGET_ARM_
        return lvExactSize / sizeof(float);
#else  //  _TARGET_ARM64_
        if (lvHfaTypeIsFloat())
        {
            return lvExactSize / sizeof(float);
        }
        else
        {
            return lvExactSize / sizeof(double);
        }
#endif //  _TARGET_ARM64_
    }

    // lvIsMultiRegArgOrRet()
    //     returns true if this is a multireg LclVar struct used in an argument context
    //               or if this is a multireg LclVar struct assigned from a multireg call
    bool lvIsMultiRegArgOrRet()
    {
        return lvIsMultiRegArg || lvIsMultiRegRet;
    }

private:
    regNumberSmall _lvRegNum; // Used to store the register this variable is in (or, the low register of a
                              // register pair). For LEGACY_BACKEND, this is only set if lvRegister is
                              // non-zero. For non-LEGACY_BACKEND, it is set during codegen any time the
                              // variable is enregistered (in non-LEGACY_BACKEND, lvRegister is only set
                              // to non-zero if the variable gets the same register assignment for its entire
                              // lifetime).
#if !defined(_TARGET_64BIT_)
    regNumberSmall _lvOtherReg; // Used for "upper half" of long var.
#endif                          // !defined(_TARGET_64BIT_)

    regNumberSmall _lvArgReg; // The register in which this argument is passed.

#if FEATURE_MULTIREG_ARGS
    regNumberSmall _lvOtherArgReg; // Used for the second part of the struct passed in a register.
                                   // Note this is defined but not used by ARM32
#endif                             // FEATURE_MULTIREG_ARGS

#ifndef LEGACY_BACKEND
    union {
        regNumberSmall _lvArgInitReg;     // the register      into which the argument is moved at entry
        regPairNoSmall _lvArgInitRegPair; // the register pair into which the argument is moved at entry
    };
#endif // !LEGACY_BACKEND

public:
    // The register number is stored in a small format (8 bits), but the getters return and the setters take
    // a full-size (unsigned) format, to localize the casts here.

    /////////////////////

    __declspec(property(get = GetRegNum, put = SetRegNum)) regNumber lvRegNum;

    regNumber GetRegNum() const
    {
        return (regNumber)_lvRegNum;
    }

    void SetRegNum(regNumber reg)
    {
        _lvRegNum = (regNumberSmall)reg;
        assert(_lvRegNum == reg);
    }

/////////////////////

#if defined(_TARGET_64BIT_)
    __declspec(property(get = GetOtherReg, put = SetOtherReg)) regNumber lvOtherReg;

    regNumber GetOtherReg() const
    {
        assert(!"shouldn't get here"); // can't use "unreached();" because it's NORETURN, which causes C4072
                                       // "unreachable code" warnings
        return REG_NA;
    }

    void SetOtherReg(regNumber reg)
    {
        assert(!"shouldn't get here"); // can't use "unreached();" because it's NORETURN, which causes C4072
                                       // "unreachable code" warnings
    }
#else  // !_TARGET_64BIT_
    __declspec(property(get = GetOtherReg, put = SetOtherReg)) regNumber lvOtherReg;

    regNumber GetOtherReg() const
    {
        return (regNumber)_lvOtherReg;
    }

    void SetOtherReg(regNumber reg)
    {
        _lvOtherReg = (regNumberSmall)reg;
        assert(_lvOtherReg == reg);
    }
#endif // !_TARGET_64BIT_

    /////////////////////

    __declspec(property(get = GetArgReg, put = SetArgReg)) regNumber lvArgReg;

    regNumber GetArgReg() const
    {
        return (regNumber)_lvArgReg;
    }

    void SetArgReg(regNumber reg)
    {
        _lvArgReg = (regNumberSmall)reg;
        assert(_lvArgReg == reg);
    }

#if FEATURE_MULTIREG_ARGS
    __declspec(property(get = GetOtherArgReg, put = SetOtherArgReg)) regNumber lvOtherArgReg;

    regNumber GetOtherArgReg() const
    {
        return (regNumber)_lvOtherArgReg;
    }

    void SetOtherArgReg(regNumber reg)
    {
        _lvOtherArgReg = (regNumberSmall)reg;
        assert(_lvOtherArgReg == reg);
    }
#endif // FEATURE_MULTIREG_ARGS

#ifdef FEATURE_SIMD
    // Is this is a SIMD struct?
    bool lvIsSIMDType() const
    {
        return lvSIMDType;
    }

    // Is this is a SIMD struct which is used for SIMD intrinsic?
    bool lvIsUsedInSIMDIntrinsic() const
    {
        return lvUsedInSIMDIntrinsic;
    }
#else
    // If feature_simd not enabled, return false
    bool lvIsSIMDType() const
    {
        return false;
    }
    bool lvIsUsedInSIMDIntrinsic() const
    {
        return false;
    }
#endif

/////////////////////

#ifndef LEGACY_BACKEND
    __declspec(property(get = GetArgInitReg, put = SetArgInitReg)) regNumber lvArgInitReg;

    regNumber GetArgInitReg() const
    {
        return (regNumber)_lvArgInitReg;
    }

    void SetArgInitReg(regNumber reg)
    {
        _lvArgInitReg = (regNumberSmall)reg;
        assert(_lvArgInitReg == reg);
    }

    /////////////////////

    __declspec(property(get = GetArgInitRegPair, put = SetArgInitRegPair)) regPairNo lvArgInitRegPair;

    regPairNo GetArgInitRegPair() const
    {
        regPairNo regPair = (regPairNo)_lvArgInitRegPair;
        assert(regPair >= REG_PAIR_FIRST && regPair <= REG_PAIR_LAST);
        return regPair;
    }

    void SetArgInitRegPair(regPairNo regPair)
    {
        assert(regPair >= REG_PAIR_FIRST && regPair <= REG_PAIR_LAST);
        _lvArgInitRegPair = (regPairNoSmall)regPair;
        assert(_lvArgInitRegPair == regPair);
    }

    /////////////////////

    bool lvIsRegCandidate() const
    {
        return lvLRACandidate != 0;
    }

    bool lvIsInReg() const
    {
        return lvIsRegCandidate() && (lvRegNum != REG_STK);
    }

#else // LEGACY_BACKEND

    bool lvIsRegCandidate() const
    {
        return lvTracked != 0;
    }

    bool lvIsInReg() const
    {
        return lvRegister != 0;
    }

#endif // LEGACY_BACKEND

    regMaskTP lvRegMask() const
    {
        regMaskTP regMask = RBM_NONE;
        if (varTypeIsFloating(TypeGet()))
        {
            if (lvRegNum != REG_STK)
            {
                regMask = genRegMaskFloat(lvRegNum, TypeGet());
            }
        }
        else
        {
            if (lvRegNum != REG_STK)
            {
                regMask = genRegMask(lvRegNum);
            }

            // For longs we may have two regs
            if (isRegPairType(lvType) && lvOtherReg != REG_STK)
            {
                regMask |= genRegMask(lvOtherReg);
            }
        }
        return regMask;
    }

    regMaskSmall lvPrefReg; // set of regs it prefers to live in

    unsigned short lvVarIndex;  // variable tracking index
    unsigned short lvRefCnt;    // unweighted (real) reference count
    unsigned       lvRefCntWtd; // weighted reference count
    int            lvStkOffs;   // stack offset of home
    unsigned       lvExactSize; // (exact) size of the type in bytes

    // Is this a promoted struct?
    // This method returns true only for structs (including SIMD structs), not for
    // locals that are split on a 32-bit target.
    // It is only necessary to use this:
    //   1) if only structs are wanted, and
    //   2) if Lowering has already been done.
    // Otherwise lvPromoted is valid.
    bool lvPromotedStruct()
    {
#if !defined(_TARGET_64BIT_)
        return (lvPromoted && !varTypeIsLong(lvType));
#else  // defined(_TARGET_64BIT_)
        return lvPromoted;
#endif // defined(_TARGET_64BIT_)
    }

    unsigned lvSize() // Size needed for storage representation. Only used for structs or TYP_BLK.
    {
        // TODO-Review: Sometimes we get called on ARM with HFA struct variables that have been promoted,
        // where the struct itself is no longer used because all access is via its member fields.
        // When that happens, the struct is marked as unused and its type has been changed to
        // TYP_INT (to keep the GC tracking code from looking at it).
        // See Compiler::raAssignVars() for details. For example:
        //      N002 (  4,  3) [00EA067C] -------------               return    struct $346
        //      N001 (  3,  2) [00EA0628] -------------                  lclVar    struct(U) V03 loc2
        //                                                                        float  V03.f1 (offs=0x00) -> V12 tmp7
        //                                                                        f8 (last use) (last use) $345
        // Here, the "struct(U)" shows that the "V03 loc2" variable is unused. Not shown is that V03
        // is now TYP_INT in the local variable table. It's not really unused, because it's in the tree.

        assert(varTypeIsStruct(lvType) || (lvType == TYP_BLK) || (lvPromoted && lvUnusedStruct));
        return (unsigned)(roundUp(lvExactSize, TARGET_POINTER_SIZE));
    }

#if defined(DEBUGGING_SUPPORT) || defined(DEBUG)
    unsigned lvSlotNum; // original slot # (if remapped)
#endif

    typeInfo lvVerTypeInfo; // type info needed for verification

    BYTE* lvGcLayout; // GC layout info for structs

#if FANCY_ARRAY_OPT
    GenTreePtr lvKnownDim; // array size if known
#endif

#if ASSERTION_PROP
    BlockSet   lvRefBlks;          // Set of blocks that contain refs
    GenTreePtr lvDefStmt;          // Pointer to the statement with the single definition
    void       lvaDisqualifyVar(); // Call to disqualify a local variable from use in optAddCopies
#endif
    var_types TypeGet() const
    {
        return (var_types)lvType;
    }
    bool lvStackAligned() const
    {
        assert(lvIsStructField);
        return ((lvFldOffset % sizeof(void*)) == 0);
    }
    bool lvNormalizeOnLoad() const
    {
        return varTypeIsSmall(TypeGet()) &&
               // lvIsStructField is treated the same as the aliased local, see fgDoNormalizeOnStore.
               (lvIsParam || lvAddrExposed || lvIsStructField);
    }

    bool lvNormalizeOnStore()
    {
        return varTypeIsSmall(TypeGet()) &&
               // lvIsStructField is treated the same as the aliased local, see fgDoNormalizeOnStore.
               !(lvIsParam || lvAddrExposed || lvIsStructField);
    }

    void lvaResetSortAgainFlag(Compiler* pComp);
    void decRefCnts(BasicBlock::weight_t weight, Compiler* pComp, bool propagate = true);
    void incRefCnts(BasicBlock::weight_t weight, Compiler* pComp, bool propagate = true);
    void setPrefReg(regNumber regNum, Compiler* pComp);
    void addPrefReg(regMaskTP regMask, Compiler* pComp);
    bool IsFloatRegType() const
    {
        return isFloatRegType(lvType) || lvIsHfaRegArg();
    }
    var_types GetHfaType() const
    {
        return lvIsHfa() ? (lvHfaTypeIsFloat() ? TYP_FLOAT : TYP_DOUBLE) : TYP_UNDEF;
    }
    void SetHfaType(var_types type)
    {
        assert(varTypeIsFloating(type));
        lvSetHfaTypeIsFloat(type == TYP_FLOAT);
    }

#ifndef LEGACY_BACKEND
    var_types lvaArgType();
#endif

    PerSsaArray lvPerSsaData;

#ifdef DEBUG
    // Keep track of the # of SsaNames, for a bounds check.
    unsigned lvNumSsaNames;
#endif

    // Returns the address of the per-Ssa data for the given ssaNum (which is required
    // not to be the SsaConfig::RESERVED_SSA_NUM, which indicates that the variable is
    // not an SSA variable).
    LclSsaVarDsc* GetPerSsaData(unsigned ssaNum)
    {
        assert(ssaNum != SsaConfig::RESERVED_SSA_NUM);
        assert(SsaConfig::RESERVED_SSA_NUM == 0);
        unsigned zeroBased = ssaNum - SsaConfig::UNINIT_SSA_NUM;
        assert(zeroBased < lvNumSsaNames);
        return &lvPerSsaData.GetRef(zeroBased);
    }

#ifdef DEBUG
public:
    void PrintVarReg() const
    {
        if (isRegPairType(TypeGet()))
        {
            printf("%s:%s", getRegName(lvOtherReg), // hi32
                   getRegName(lvRegNum));           // lo32
        }
        else
        {
            printf("%s", getRegName(lvRegNum));
        }
    }
#endif // DEBUG

}; // class LclVarDsc

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           TempsInfo                                       XX
XX                                                                           XX
XX  The temporary lclVars allocated by the compiler for code generation      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************
 *
 *  The following keeps track of temporaries allocated in the stack frame
 *  during code-generation (after register allocation). These spill-temps are
 *  only used if we run out of registers while evaluating a tree.
 *
 *  These are different from the more common temps allocated by lvaGrabTemp().
 */

class TempDsc
{
public:
    TempDsc* tdNext;

private:
    int tdOffs;
#ifdef DEBUG
    static const int BAD_TEMP_OFFSET = 0xDDDDDDDD; // used as a sentinel "bad value" for tdOffs in DEBUG
#endif                                             // DEBUG

    int       tdNum;
    BYTE      tdSize;
    var_types tdType;

public:
    TempDsc(int _tdNum, unsigned _tdSize, var_types _tdType) : tdNum(_tdNum), tdSize((BYTE)_tdSize), tdType(_tdType)
    {
#ifdef DEBUG
        assert(tdNum <
               0); // temps must have a negative number (so they have a different number from all local variables)
        tdOffs = BAD_TEMP_OFFSET;
#endif // DEBUG
        if (tdNum != _tdNum)
        {
            IMPL_LIMITATION("too many spill temps");
        }
    }

#ifdef DEBUG
    bool tdLegalOffset() const
    {
        return tdOffs != BAD_TEMP_OFFSET;
    }
#endif // DEBUG

    int tdTempOffs() const
    {
        assert(tdLegalOffset());
        return tdOffs;
    }
    void tdSetTempOffs(int offs)
    {
        tdOffs = offs;
        assert(tdLegalOffset());
    }
    void tdAdjustTempOffs(int offs)
    {
        tdOffs += offs;
        assert(tdLegalOffset());
    }

    int tdTempNum() const
    {
        assert(tdNum < 0);
        return tdNum;
    }
    unsigned tdTempSize() const
    {
        return tdSize;
    }
    var_types tdTempType() const
    {
        return tdType;
    }
};

// interface to hide linearscan implementation from rest of compiler
class LinearScanInterface
{
public:
    virtual void doLinearScan()                                = 0;
    virtual void recordVarLocationsAtStartOfBB(BasicBlock* bb) = 0;
};

LinearScanInterface* getLinearScanAllocator(Compiler* comp);

// Information about arrays: their element type and size, and the offset of the first element.
// We label GT_IND's that are array indices with GTF_IND_ARR_INDEX, and, for such nodes,
// associate an array info via the map retrieved by GetArrayInfoMap().  This information is used,
// for example, in value numbering of array index expressions.
struct ArrayInfo
{
    var_types            m_elemType;
    CORINFO_CLASS_HANDLE m_elemStructType;
    unsigned             m_elemSize;
    unsigned             m_elemOffset;

    ArrayInfo() : m_elemType(TYP_UNDEF), m_elemStructType(nullptr), m_elemSize(0), m_elemOffset(0)
    {
    }

    ArrayInfo(var_types elemType, unsigned elemSize, unsigned elemOffset, CORINFO_CLASS_HANDLE elemStructType)
        : m_elemType(elemType), m_elemStructType(elemStructType), m_elemSize(elemSize), m_elemOffset(elemOffset)
    {
    }
};

// This enumeration names the phases into which we divide compilation.  The phases should completely
// partition a compilation.
enum Phases
{
#define CompPhaseNameMacro(enum_nm, string_nm, short_nm, hasChildren, parent) enum_nm,
#include "compphases.h"
    PHASE_NUMBER_OF
};

extern const char*   PhaseNames[];
extern const char*   PhaseEnums[];
extern const LPCWSTR PhaseShortNames[];

//---------------------------------------------------------------
// Compilation time.
//

// A "CompTimeInfo" is a structure for tracking the compilation time of one or more methods.
// We divide a compilation into a sequence of contiguous phases, and track the total (per-thread) cycles
// of the compilation, as well as the cycles for each phase.  We also track the number of bytecodes.
// If there is a failure in reading a timer at any point, the "CompTimeInfo" becomes invalid, as indicated
// by "m_timerFailure" being true.
// If FEATURE_JIT_METHOD_PERF is not set, we define a minimal form of this, enough to let other code compile.
struct CompTimeInfo
{
#ifdef FEATURE_JIT_METHOD_PERF
    // The string names of the phases.
    static const char* PhaseNames[];

    static bool PhaseHasChildren[];
    static int  PhaseParent[];

    unsigned         m_byteCodeBytes;
    unsigned __int64 m_totalCycles;
    unsigned __int64 m_invokesByPhase[PHASE_NUMBER_OF];
    unsigned __int64 m_cyclesByPhase[PHASE_NUMBER_OF];
    // For better documentation, we call EndPhase on
    // non-leaf phases.  We should also call EndPhase on the
    // last leaf subphase; obviously, the elapsed cycles between the EndPhase
    // for the last leaf subphase and the EndPhase for an ancestor should be very small.
    // We add all such "redundant end phase" intervals to this variable below; we print
    // it out in a report, so we can verify that it is, indeed, very small.  If it ever
    // isn't, this means that we're doing something significant between the end of the last
    // declared subphase and the end of its parent.
    unsigned __int64 m_parentPhaseEndSlop;
    bool             m_timerFailure;

    CompTimeInfo(unsigned byteCodeBytes);
#endif
};

#ifdef FEATURE_JIT_METHOD_PERF

// This class summarizes the JIT time information over the course of a run: the number of methods compiled,
// and the total and maximum timings.  (These are instances of the "CompTimeInfo" type described above).
// The operation of adding a single method's timing to the summary may be performed concurrently by several
// threads, so it is protected by a lock.
// This class is intended to be used as a singleton type, with only a single instance.
class CompTimeSummaryInfo
{
    // This lock protects the fields of all CompTimeSummaryInfo(s) (of which we expect there to be one).
    static CritSecObject s_compTimeSummaryLock;

    int          m_numMethods;
    CompTimeInfo m_total;
    CompTimeInfo m_maximum;

    int          m_numFilteredMethods;
    CompTimeInfo m_filtered;

    // This method computes the number of cycles/sec for the current machine.  The cycles are those counted
    // by GetThreadCycleTime; we assume that these are of equal duration, though that is not necessarily true.
    // If any OS interaction fails, returns 0.0.
    double CyclesPerSecond();

    // This can use what ever data you want to determine if the value to be added
    // belongs in the filtered section (it's always included in the unfiltered section)
    bool IncludedInFilteredData(CompTimeInfo& info);

public:
    // This is the unique CompTimeSummaryInfo object for this instance of the runtime.
    static CompTimeSummaryInfo s_compTimeSummary;

    CompTimeSummaryInfo() : m_total(0), m_maximum(0), m_numMethods(0), m_filtered(0), m_numFilteredMethods(0)
    {
    }

    // Assumes that "info" is a completed CompTimeInfo for a compilation; adds it to the summary.
    // This is thread safe.
    void AddInfo(CompTimeInfo& info);

    // Print the summary information to "f".
    // This is not thread-safe; assumed to be called by only one thread.
    void Print(FILE* f);
};

// A JitTimer encapsulates a CompTimeInfo for a single compilation. It also tracks the start of compilation,
// and when the current phase started.  This is intended to be part of a Compilation object.  This is
// disabled (FEATURE_JIT_METHOD_PERF not defined) when FEATURE_CORECLR is set, or on non-windows platforms.
//
class JitTimer
{
    unsigned __int64 m_start;         // Start of the compilation.
    unsigned __int64 m_curPhaseStart; // Start of the current phase.
#ifdef DEBUG
    Phases m_lastPhase; // The last phase that was completed (or (Phases)-1 to start).
#endif
    CompTimeInfo m_info; // The CompTimeInfo for this compilation.

    static CritSecObject s_csvLock; // Lock to protect the time log file.
    void PrintCsvMethodStats(Compiler* comp);

private:
    void* operator new(size_t);
    void* operator new[](size_t);
    void operator delete(void*);
    void operator delete[](void*);

public:
    // Initialized the timer instance
    JitTimer(unsigned byteCodeSize);

    static JitTimer* Create(Compiler* comp, unsigned byteCodeSize)
    {
        return ::new (comp, CMK_Unknown) JitTimer(byteCodeSize);
    }

    static void PrintCsvHeader();

    // Ends the current phase (argument is for a redundant check).
    void EndPhase(Phases phase);

    // Completes the timing of the current method, which is assumed to have "byteCodeBytes" bytes of bytecode,
    // and adds it to "sum".
    void Terminate(Compiler* comp, CompTimeSummaryInfo& sum);

    // Attempts to query the cycle counter of the current thread.  If successful, returns "true" and sets
    // *cycles to the cycle counter value.  Otherwise, returns false and sets the "m_timerFailure" flag of
    // "m_info" to true.
    bool GetThreadCycles(unsigned __int64* cycles)
    {
        bool res = CycleTimer::GetThreadCyclesS(cycles);
        if (!res)
        {
            m_info.m_timerFailure = true;
        }
        return res;
    }
};
#endif // FEATURE_JIT_METHOD_PERF

//------------------- Function/Funclet info -------------------------------
DECLARE_TYPED_ENUM(FuncKind, BYTE)
{
    FUNC_ROOT,        // The main/root function (always id==0)
        FUNC_HANDLER, // a funclet associated with an EH handler (finally, fault, catch, filter handler)
        FUNC_FILTER,  // a funclet associated with an EH filter
        FUNC_COUNT
}
END_DECLARE_TYPED_ENUM(FuncKind, BYTE)

class emitLocation;

struct FuncInfoDsc
{
    FuncKind       funKind;
    BYTE           funFlags;   // Currently unused, just here for padding
    unsigned short funEHIndex; // index, into the ebd table, of innermost EH clause corresponding to this
                               // funclet. It is only valid if funKind field indicates this is a
                               // EH-related funclet: FUNC_HANDLER or FUNC_FILTER

#if defined(_TARGET_AMD64_)

    // TODO-AMD64-Throughput: make the AMD64 info more like the ARM info to avoid having this large static array.
    emitLocation* startLoc;
    emitLocation* endLoc;
    emitLocation* coldStartLoc; // locations for the cold section, if there is one.
    emitLocation* coldEndLoc;
    UNWIND_INFO   unwindHeader;
    // Maximum of 255 UNWIND_CODE 'nodes' and then the unwind header. If there are an odd
    // number of codes, the VM or Zapper will 4-byte align the whole thing.
    BYTE     unwindCodes[offsetof(UNWIND_INFO, UnwindCode) + (0xFF * sizeof(UNWIND_CODE))];
    unsigned unwindCodeSlot;

#ifdef UNIX_AMD64_ABI
    jitstd::vector<CFI_CODE>* cfiCodes;
#endif // UNIX_AMD64_ABI

#elif defined(_TARGET_ARMARCH_)

    UnwindInfo  uwi;     // Unwind information for this function/funclet's hot  section
    UnwindInfo* uwiCold; // Unwind information for this function/funclet's cold section
                         //   Note: we only have a pointer here instead of the actual object,
                         //   to save memory in the JIT case (compared to the NGEN case),
                         //   where we don't have any cold section.
                         //   Note 2: we currently don't support hot/cold splitting in functions
                         //   with EH, so uwiCold will be NULL for all funclets.

#endif // _TARGET_ARMARCH_

    // Eventually we may want to move rsModifiedRegsMask, lvaOutgoingArgSize, and anything else
    // that isn't shared between the main function body and funclets.
};

struct fgArgTabEntry
{

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    fgArgTabEntry()
    {
        otherRegNum = REG_NA;
        isStruct    = false; // is this a struct arg
    }
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    GenTreePtr node; // Initially points at the Op1 field of 'parent', but if the argument is replaced with an GT_ASG or
                     // placeholder
                     //  it will point at the actual argument in the gtCallLateArgs list.
    GenTreePtr parent; // Points at the GT_LIST node in the gtCallArgs for this argument

    unsigned argNum; // The original argument number, also specifies the required argument evaluation order from the IL

    regNumber regNum; // The (first) register to use when passing this argument, set to REG_STK for arguments passed on
                      // the stack
    unsigned numRegs; // Count of number of registers that this argument uses

    // A slot is a pointer sized region in the OutArg area.
    unsigned slotNum;  // When an argument is passed in the OutArg area this is the slot number in the OutArg area
    unsigned numSlots; // Count of number of slots that this argument uses

    unsigned alignment;  // 1 or 2 (slots/registers)
    unsigned lateArgInx; // index into gtCallLateArgs list
    unsigned tmpNum;     // the LclVar number if we had to force evaluation of this arg

    bool isSplit : 1;       // True when this argument is split between the registers and OutArg area
    bool needTmp : 1;       // True when we force this argument's evaluation into a temp LclVar
    bool needPlace : 1;     // True when we must replace this argument with a placeholder node
    bool isTmp : 1;         // True when we setup a temp LclVar for this argument due to size issues with the struct
    bool processed : 1;     // True when we have decided the evaluation order for this argument in the gtCallLateArgs
    bool isHfaRegArg : 1;   // True when the argument is passed as a HFA in FP registers.
    bool isBackFilled : 1;  // True when the argument fills a register slot skipped due to alignment requirements of
                            // previous arguments.
    bool isNonStandard : 1; // True if it is an arg that is passed in a reg other than a standard arg reg, or is forced
                            // to be on the stack despite its arg list position.

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    bool isStruct : 1; // True if this is a struct arg

    regNumber otherRegNum; // The (second) register to use when passing this argument.

    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

#ifdef _TARGET_ARM_
    void SetIsHfaRegArg(bool hfaRegArg)
    {
        isHfaRegArg = hfaRegArg;
    }

    void SetIsBackFilled(bool backFilled)
    {
        isBackFilled = backFilled;
    }

    bool IsBackFilled() const
    {
        return isBackFilled;
    }
#else  // !_TARGET_ARM_
    // To make the callers easier, we allow these calls (and the isHfaRegArg and isBackFilled data members) for all
    // platforms.
    void SetIsHfaRegArg(bool hfaRegArg)
    {
    }

    void SetIsBackFilled(bool backFilled)
    {
    }

    bool IsBackFilled() const
    {
        return false;
    }
#endif // !_TARGET_ARM_

#ifdef DEBUG
    void Dump();
#endif
};
typedef struct fgArgTabEntry* fgArgTabEntryPtr;

//-------------------------------------------------------------------------
//
//  The class fgArgInfo is used to handle the arguments
//  when morphing a GT_CALL node.
//

class fgArgInfo
{
    Compiler*  compiler;    // Back pointer to the compiler instance so that we can allocate memory
    GenTreePtr callTree;    // Back pointer to the GT_CALL node for this fgArgInfo
    unsigned   argCount;    // Updatable arg count value
    unsigned   nextSlotNum; // Updatable slot count value
    unsigned   stkLevel;    // Stack depth when we make this call (for x86)

    unsigned          argTableSize; // size of argTable array (equal to the argCount when done with fgMorphArgs)
    bool              hasRegArgs;   // true if we have one or more register arguments
    bool              hasStackArgs; // true if we have one or more stack arguments
    bool              argsComplete; // marker for state
    bool              argsSorted;   // marker for state
    fgArgTabEntryPtr* argTable;     // variable sized array of per argument descrption: (i.e. argTable[argTableSize])

private:
    void AddArg(fgArgTabEntryPtr curArgTabEntry);

public:
    fgArgInfo(Compiler* comp, GenTreePtr call, unsigned argCount);
    fgArgInfo(GenTreePtr newCall, GenTreePtr oldCall);

    fgArgTabEntryPtr AddRegArg(
        unsigned argNum, GenTreePtr node, GenTreePtr parent, regNumber regNum, unsigned numRegs, unsigned alignment);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    fgArgTabEntryPtr AddRegArg(
        unsigned                                                         argNum,
        GenTreePtr                                                       node,
        GenTreePtr                                                       parent,
        regNumber                                                        regNum,
        unsigned                                                         numRegs,
        unsigned                                                         alignment,
        const bool                                                       isStruct,
        const regNumber                                                  otherRegNum   = REG_NA,
        const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* const structDescPtr = nullptr);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    fgArgTabEntryPtr AddStkArg(unsigned   argNum,
                               GenTreePtr node,
                               GenTreePtr parent,
                               unsigned   numSlots,
                               unsigned alignment FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(const bool isStruct));

    void             RemorphReset();
    fgArgTabEntryPtr RemorphRegArg(
        unsigned argNum, GenTreePtr node, GenTreePtr parent, regNumber regNum, unsigned numRegs, unsigned alignment);

    void RemorphStkArg(unsigned argNum, GenTreePtr node, GenTreePtr parent, unsigned numSlots, unsigned alignment);

    void SplitArg(unsigned argNum, unsigned numRegs, unsigned numSlots);

    void EvalToTmp(unsigned argNum, unsigned tmpNum, GenTreePtr newNode);

    void ArgsComplete();

    void SortArgs();

    void EvalArgsToTemps();

    void RecordStkLevel(unsigned stkLvl);
    unsigned RetrieveStkLevel();

    unsigned ArgCount()
    {
        return argCount;
    }
    fgArgTabEntryPtr* ArgTable()
    {
        return argTable;
    }
    unsigned GetNextSlotNum()
    {
        return nextSlotNum;
    }
    bool HasRegArgs()
    {
        return hasRegArgs;
    }
    bool HasStackArgs()
    {
        return hasStackArgs;
    }
};

#ifdef DEBUG
// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
// We have the ability to mark source expressions with "Test Labels."
// These drive assertions within the JIT, or internal JIT testing.  For example, we could label expressions
// that should be CSE defs, and other expressions that should uses of those defs, with a shared label.

enum TestLabel // This must be kept identical to System.Runtime.CompilerServices.JitTestLabel.TestLabel.
{
    TL_SsaName,
    TL_VN,        // Defines a "VN equivalence class".  (For full VN, including exceptions thrown).
    TL_VNNorm,    // Like above, but uses the non-exceptional value of the expression.
    TL_CSE_Def,   //  This must be identified in the JIT as a CSE def
    TL_CSE_Use,   //  This must be identified in the JIT as a CSE use
    TL_LoopHoist, // Expression must (or must not) be hoisted out of the loop.
};

struct TestLabelAndNum
{
    TestLabel m_tl;
    ssize_t   m_num;

    TestLabelAndNum() : m_tl(TestLabel(0)), m_num(0)
    {
    }
};

typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, TestLabelAndNum, JitSimplerHashBehavior> NodeToTestDataMap;

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
#endif // DEBUG

// This class implements the "IAllocator" interface, so that we can use
// utilcode collection classes in the JIT, and have them use the JIT's allocator.

class CompAllocator : public IAllocator
{
    Compiler* m_comp;
#if MEASURE_MEM_ALLOC
    CompMemKind m_cmk;
#endif
public:
    CompAllocator(Compiler* comp, CompMemKind cmk)
        : m_comp(comp)
#if MEASURE_MEM_ALLOC
        , m_cmk(cmk)
#endif
    {
    }

    inline void* Alloc(size_t sz);

    inline void* ArrayAlloc(size_t elems, size_t elemSize);

    // For the compiler's no-release allocator, free operations are no-ops.
    void Free(void* p)
    {
    }
};

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX   The big guy. The sections are currently organized as :                  XX
XX                                                                           XX
XX    o  GenTree and BasicBlock                                              XX
XX    o  LclVarsInfo                                                         XX
XX    o  Importer                                                            XX
XX    o  FlowGraph                                                           XX
XX    o  Optimizer                                                           XX
XX    o  RegAlloc                                                            XX
XX    o  EEInterface                                                         XX
XX    o  TempsInfo                                                           XX
XX    o  RegSet                                                              XX
XX    o  GCInfo                                                              XX
XX    o  Instruction                                                         XX
XX    o  ScopeInfo                                                           XX
XX    o  PrologScopeInfo                                                     XX
XX    o  CodeGenerator                                                       XX
XX    o  UnwindInfo                                                          XX
XX    o  Compiler                                                            XX
XX    o  typeInfo                                                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

class Compiler
{
    friend class emitter;
    friend class UnwindInfo;
    friend class UnwindFragmentInfo;
    friend class UnwindEpilogInfo;
    friend class JitTimer;
    friend class LinearScan;
    friend class fgArgInfo;
    friend class Rationalizer;
    friend class Phase;
    friend class Lowering;
    friend class CSE_DataFlow;
    friend class CSE_Heuristic;
    friend class CodeGenInterface;
    friend class CodeGen;
    friend class LclVarDsc;
    friend class TempDsc;
    friend class LIR;
    friend class ObjectAllocator;

#ifndef _TARGET_64BIT_
    friend class DecomposeLongs;
#endif // !_TARGET_64BIT_

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX  Misc structs definitions                                                 XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    hashBvGlobalData hbvGlobalData; // Used by the hashBv bitvector package.

#ifdef DEBUG
    bool    verbose;
    bool    dumpIR;
    bool    dumpIRNodes;
    bool    dumpIRTypes;
    bool    dumpIRKinds;
    bool    dumpIRLocals;
    bool    dumpIRRegs;
    bool    dumpIRSsa;
    bool    dumpIRValnums;
    bool    dumpIRCosts;
    bool    dumpIRFlags;
    bool    dumpIRNoLists;
    bool    dumpIRNoLeafs;
    bool    dumpIRNoStmts;
    bool    dumpIRTrees;
    bool    dumpIRLinear;
    bool    dumpIRDataflow;
    bool    dumpIRBlockHeaders;
    bool    dumpIRExit;
    LPCWSTR dumpIRPhase;
    LPCWSTR dumpIRFormat;
    bool    verboseTrees;
    bool    shouldUseVerboseTrees();
    bool    asciiTrees; // If true, dump trees using only ASCII characters
    bool    shouldDumpASCIITrees();
    bool    verboseSsa; // If true, produce especially verbose dump output in SSA construction.
    bool    shouldUseVerboseSsa();
    bool    treesBeforeAfterMorph; // If true, print trees before/after morphing (paired by an intra-compilation id:
    int     morphNum; // This counts the the trees that have been morphed, allowing us to label each uniquely.

    const char* VarNameToStr(VarName name)
    {
        return name;
    }

    DWORD expensiveDebugCheckLevel;
#endif

#if FEATURE_MULTIREG_RET
    GenTreePtr impAssignMultiRegTypeToVar(GenTreePtr op, CORINFO_CLASS_HANDLE hClass);
#endif // FEATURE_MULTIREG_RET

#ifdef ARM_SOFTFP
    bool isSingleFloat32Struct(CORINFO_CLASS_HANDLE hClass);
#endif // ARM_SOFTFP

    //-------------------------------------------------------------------------
    // Functions to handle homogeneous floating-point aggregates (HFAs) in ARM.
    // HFAs are one to four element structs where each element is the same
    // type, either all float or all double. They are treated specially
    // in the ARM Procedure Call Standard, specifically, they are passed in
    // floating-point registers instead of the general purpose registers.
    //

    bool IsHfa(CORINFO_CLASS_HANDLE hClass);
    bool IsHfa(GenTreePtr tree);

    var_types GetHfaType(GenTreePtr tree);
    unsigned GetHfaCount(GenTreePtr tree);

    var_types GetHfaType(CORINFO_CLASS_HANDLE hClass);
    unsigned GetHfaCount(CORINFO_CLASS_HANDLE hClass);

    bool IsMultiRegPassedType(CORINFO_CLASS_HANDLE hClass);
    bool IsMultiRegReturnedType(CORINFO_CLASS_HANDLE hClass);

    //-------------------------------------------------------------------------
    // The following is used for validating format of EH table
    //

    struct EHNodeDsc;
    typedef struct EHNodeDsc* pEHNodeDsc;

    EHNodeDsc* ehnTree; // root of the tree comprising the EHnodes.
    EHNodeDsc* ehnNext; // root of the tree comprising the EHnodes.

    struct EHNodeDsc
    {
        enum EHBlockType
        {
            TryNode,
            FilterNode,
            HandlerNode,
            FinallyNode,
            FaultNode
        };

        EHBlockType ehnBlockType;   // kind of EH block
        IL_OFFSET   ehnStartOffset; // IL offset of start of the EH block
        IL_OFFSET ehnEndOffset; // IL offset past end of the EH block. (TODO: looks like verInsertEhNode() sets this to
                                // the last IL offset, not "one past the last one", i.e., the range Start to End is
                                // inclusive).
        pEHNodeDsc ehnNext;     // next (non-nested) block in sequential order
        pEHNodeDsc ehnChild;    // leftmost nested block
        union {
            pEHNodeDsc ehnTryNode;     // for filters and handlers, the corresponding try node
            pEHNodeDsc ehnHandlerNode; // for a try node, the corresponding handler node
        };
        pEHNodeDsc ehnFilterNode; // if this is a try node and has a filter, otherwise 0
        pEHNodeDsc ehnEquivalent; // if blockType=tryNode, start offset and end offset is same,

        inline void ehnSetTryNodeType()
        {
            ehnBlockType = TryNode;
        }
        inline void ehnSetFilterNodeType()
        {
            ehnBlockType = FilterNode;
        }
        inline void ehnSetHandlerNodeType()
        {
            ehnBlockType = HandlerNode;
        }
        inline void ehnSetFinallyNodeType()
        {
            ehnBlockType = FinallyNode;
        }
        inline void ehnSetFaultNodeType()
        {
            ehnBlockType = FaultNode;
        }

        inline BOOL ehnIsTryBlock()
        {
            return ehnBlockType == TryNode;
        }
        inline BOOL ehnIsFilterBlock()
        {
            return ehnBlockType == FilterNode;
        }
        inline BOOL ehnIsHandlerBlock()
        {
            return ehnBlockType == HandlerNode;
        }
        inline BOOL ehnIsFinallyBlock()
        {
            return ehnBlockType == FinallyNode;
        }
        inline BOOL ehnIsFaultBlock()
        {
            return ehnBlockType == FaultNode;
        }

        // returns true if there is any overlap between the two nodes
        static inline BOOL ehnIsOverlap(pEHNodeDsc node1, pEHNodeDsc node2)
        {
            if (node1->ehnStartOffset < node2->ehnStartOffset)
            {
                return (node1->ehnEndOffset >= node2->ehnStartOffset);
            }
            else
            {
                return (node1->ehnStartOffset <= node2->ehnEndOffset);
            }
        }

        // fails with BADCODE if inner is not completely nested inside outer
        static inline BOOL ehnIsNested(pEHNodeDsc inner, pEHNodeDsc outer)
        {
            return ((inner->ehnStartOffset >= outer->ehnStartOffset) && (inner->ehnEndOffset <= outer->ehnEndOffset));
        }
    };

//-------------------------------------------------------------------------
// Exception handling functions
//

#if !FEATURE_EH_FUNCLETS

    bool ehNeedsShadowSPslots()
    {
        return (info.compXcptnsCount || opts.compDbgEnC);
    }

    // 0 for methods with no EH
    // 1 for methods with non-nested EH, or where only the try blocks are nested
    // 2 for a method with a catch within a catch
    // etc.
    unsigned ehMaxHndNestingCount;

#endif // !FEATURE_EH_FUNCLETS

    static bool jitIsBetween(unsigned value, unsigned start, unsigned end);
    static bool jitIsBetweenInclusive(unsigned value, unsigned start, unsigned end);

    bool bbInCatchHandlerILRange(BasicBlock* blk);
    bool bbInFilterILRange(BasicBlock* blk);
    bool bbInTryRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInExnFlowRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInHandlerRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInCatchHandlerRegions(BasicBlock* tryBlk, BasicBlock* hndBlk);
    unsigned short bbFindInnermostCommonTryRegion(BasicBlock* bbOne, BasicBlock* bbTwo);

    unsigned short bbFindInnermostTryRegionContainingHandlerRegion(unsigned handlerIndex);
    unsigned short bbFindInnermostHandlerRegionContainingTryRegion(unsigned tryIndex);

    // Returns true if "block" is the start of a try region.
    bool bbIsTryBeg(BasicBlock* block);

    // Returns true if "block" is the start of a handler or filter region.
    bool bbIsHandlerBeg(BasicBlock* block);

    // Returns true iff "block" is where control flows if an exception is raised in the
    // try region, and sets "*regionIndex" to the index of the try for the handler.
    // Differs from "IsHandlerBeg" in the case of filters, where this is true for the first
    // block of the filter, but not for the filter's handler.
    bool bbIsExFlowBlock(BasicBlock* block, unsigned* regionIndex);

    bool ehHasCallableHandlers();

    // Return the EH descriptor for the given region index.
    EHblkDsc* ehGetDsc(unsigned regionIndex);

    // Return the EH index given a region descriptor.
    unsigned ehGetIndex(EHblkDsc* ehDsc);

    // Return the EH descriptor index of the enclosing try, for the given region index.
    unsigned ehGetEnclosingTryIndex(unsigned regionIndex);

    // Return the EH descriptor index of the enclosing handler, for the given region index.
    unsigned ehGetEnclosingHndIndex(unsigned regionIndex);

    // Return the EH descriptor for the most nested 'try' region this BasicBlock is a member of (or nullptr if this
    // block is not in a 'try' region).
    EHblkDsc* ehGetBlockTryDsc(BasicBlock* block);

    // Return the EH descriptor for the most nested filter or handler region this BasicBlock is a member of (or nullptr
    // if this block is not in a filter or handler region).
    EHblkDsc* ehGetBlockHndDsc(BasicBlock* block);

    // Return the EH descriptor for the most nested region that may handle exceptions raised in this BasicBlock (or
    // nullptr if this block's exceptions propagate to caller).
    EHblkDsc* ehGetBlockExnFlowDsc(BasicBlock* block);

    EHblkDsc* ehIsBlockTryLast(BasicBlock* block);
    EHblkDsc* ehIsBlockHndLast(BasicBlock* block);
    bool ehIsBlockEHLast(BasicBlock* block);

    bool ehBlockHasExnFlowDsc(BasicBlock* block);

    // Return the region index of the most nested EH region this block is in.
    unsigned ehGetMostNestedRegionIndex(BasicBlock* block, bool* inTryRegion);

    // Find the true enclosing try index, ignoring 'mutual protect' try. Uses IL ranges to check.
    unsigned ehTrueEnclosingTryIndexIL(unsigned regionIndex);

    // Return the index of the most nested enclosing region for a particular EH region. Returns NO_ENCLOSING_INDEX
    // if there is no enclosing region. If the returned index is not NO_ENCLOSING_INDEX, then '*inTryRegion'
    // is set to 'true' if the enclosing region is a 'try', or 'false' if the enclosing region is a handler.
    // (It can never be a filter.)
    unsigned ehGetEnclosingRegionIndex(unsigned regionIndex, bool* inTryRegion);

    // A block has been deleted. Update the EH table appropriately.
    void ehUpdateForDeletedBlock(BasicBlock* block);

    // Determine whether a block can be deleted while preserving the EH normalization rules.
    bool ehCanDeleteEmptyBlock(BasicBlock* block);

    // Update the 'last' pointers in the EH table to reflect new or deleted blocks in an EH region.
    void ehUpdateLastBlocks(BasicBlock* oldLast, BasicBlock* newLast);

    // For a finally handler, find the region index that the BBJ_CALLFINALLY lives in that calls the handler,
    // or NO_ENCLOSING_INDEX if the BBJ_CALLFINALLY lives in the main function body. Normally, the index
    // is the same index as the handler (and the BBJ_CALLFINALLY lives in the 'try' region), but for AMD64 the
    // BBJ_CALLFINALLY lives in the enclosing try or handler region, whichever is more nested, or the main function
    // body. If the returned index is not NO_ENCLOSING_INDEX, then '*inTryRegion' is set to 'true' if the
    // BBJ_CALLFINALLY lives in the returned index's 'try' region, or 'false' if lives in the handler region. (It never
    // lives in a filter.)
    unsigned ehGetCallFinallyRegionIndex(unsigned finallyIndex, bool* inTryRegion);

    // Find the range of basic blocks in which all BBJ_CALLFINALLY will be found that target the 'finallyIndex' region's
    // handler. Set begBlk to the first block, and endBlk to the block after the last block of the range
    // (nullptr if the last block is the last block in the program).
    // Precondition: 'finallyIndex' is the EH region of a try/finally clause.
    void ehGetCallFinallyBlockRange(unsigned finallyIndex, BasicBlock** begBlk, BasicBlock** endBlk);

#ifdef DEBUG
    // Given a BBJ_CALLFINALLY block and the EH region index of the finally it is calling, return
    // 'true' if the BBJ_CALLFINALLY is in the correct EH region.
    bool ehCallFinallyInCorrectRegion(BasicBlock* blockCallFinally, unsigned finallyIndex);
#endif // DEBUG

#if FEATURE_EH_FUNCLETS
    // Do we need a PSPSym in the main function? For codegen purposes, we only need one
    // if there is a filter that protects a region with a nested EH clause (such as a
    // try/catch nested in the 'try' body of a try/filter/filter-handler). See
    // genFuncletProlog() for more details. However, the VM seems to use it for more
    // purposes, maybe including debugging. Until we are sure otherwise, always create
    // a PSPSym for functions with any EH.
    bool ehNeedsPSPSym() const
    {
        return compHndBBtabCount > 0;
    }

    bool     ehAnyFunclets();  // Are there any funclets in this function?
    unsigned ehFuncletCount(); // Return the count of funclets in the function

    unsigned bbThrowIndex(BasicBlock* blk); // Get the index to use as the cache key for sharing throw blocks
#else                                       // !FEATURE_EH_FUNCLETS
    bool ehAnyFunclets()
    {
        return false;
    }
    unsigned ehFuncletCount()
    {
        return 0;
    }

    unsigned bbThrowIndex(BasicBlock* blk)
    {
        return blk->bbTryIndex;
    } // Get the index to use as the cache key for sharing throw blocks
#endif                                      // !FEATURE_EH_FUNCLETS

    // Returns a flowList representing the "EH predecessors" of "blk".  These are the normal predecessors of
    // "blk", plus one special case: if "blk" is the first block of a handler, considers the predecessor(s) of the first
    // first block of the corresponding try region to be "EH predecessors".  (If there is a single such predecessor,
    // for example, we want to consider that the immediate dominator of the catch clause start block, so it's
    // convenient to also consider it a predecessor.)
    flowList* BlockPredsWithEH(BasicBlock* blk);

    // This table is useful for memoization of the method above.
    typedef SimplerHashTable<BasicBlock*, PtrKeyFuncs<BasicBlock>, flowList*, JitSimplerHashBehavior>
                        BlockToFlowListMap;
    BlockToFlowListMap* m_blockToEHPreds;
    BlockToFlowListMap* GetBlockToEHPreds()
    {
        if (m_blockToEHPreds == nullptr)
        {
            m_blockToEHPreds = new (getAllocator()) BlockToFlowListMap(getAllocator());
        }
        return m_blockToEHPreds;
    }

    void* ehEmitCookie(BasicBlock* block);
    UNATIVE_OFFSET ehCodeOffset(BasicBlock* block);

    EHblkDsc* ehInitHndRange(BasicBlock* src, IL_OFFSET* hndBeg, IL_OFFSET* hndEnd, bool* inFilter);

    EHblkDsc* ehInitTryRange(BasicBlock* src, IL_OFFSET* tryBeg, IL_OFFSET* tryEnd);

    EHblkDsc* ehInitHndBlockRange(BasicBlock* blk, BasicBlock** hndBeg, BasicBlock** hndLast, bool* inFilter);

    EHblkDsc* ehInitTryBlockRange(BasicBlock* blk, BasicBlock** tryBeg, BasicBlock** tryLast);

    void fgSetTryEnd(EHblkDsc* handlerTab, BasicBlock* newTryLast);

    void fgSetHndEnd(EHblkDsc* handlerTab, BasicBlock* newHndLast);

    void fgSkipRmvdBlocks(EHblkDsc* handlerTab);

    void fgAllocEHTable();

    void fgRemoveEHTableEntry(unsigned XTnum);

#if FEATURE_EH_FUNCLETS

    EHblkDsc* fgAddEHTableEntry(unsigned XTnum);

#endif // FEATURE_EH_FUNCLETS

#if !FEATURE_EH
    void fgRemoveEH();
#endif // !FEATURE_EH

    void fgSortEHTable();

    // Causes the EH table to obey some well-formedness conditions, by inserting
    // empty BB's when necessary:
    //   * No block is both the first block of a handler and the first block of a try.
    //   * No block is the first block of multiple 'try' regions.
    //   * No block is the last block of multiple EH regions.
    void fgNormalizeEH();
    bool fgNormalizeEHCase1();
    bool fgNormalizeEHCase2();
    bool fgNormalizeEHCase3();

#ifdef DEBUG
    void dispIncomingEHClause(unsigned num, const CORINFO_EH_CLAUSE& clause);
    void dispOutgoingEHClause(unsigned num, const CORINFO_EH_CLAUSE& clause);
    void fgVerifyHandlerTab();
    void fgDispHandlerTab();
#endif // DEBUG

    bool fgNeedToSortEHTable;

    void verInitEHTree(unsigned numEHClauses);
    void verInsertEhNode(CORINFO_EH_CLAUSE* clause, EHblkDsc* handlerTab);
    void verInsertEhNodeInTree(EHNodeDsc** ppRoot, EHNodeDsc* node);
    void verInsertEhNodeParent(EHNodeDsc** ppRoot, EHNodeDsc* node);
    void verCheckNestingLevel(EHNodeDsc* initRoot);

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                        GenTree and BasicBlock                             XX
    XX                                                                           XX
    XX  Functions to allocate and display the GenTrees and BasicBlocks           XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

    // Functions to create nodes
    GenTreeStmt* gtNewStmt(GenTreePtr expr = nullptr, IL_OFFSETX offset = BAD_IL_OFFSET);

    // For unary opers.
    GenTreePtr gtNewOperNode(genTreeOps oper, var_types type, GenTreePtr op1, bool doSimplifications = TRUE);

    // For binary opers.
    GenTreePtr gtNewOperNode(genTreeOps oper, var_types type, GenTreePtr op1, GenTreePtr op2);

    GenTreePtr gtNewQmarkNode(var_types type, GenTreePtr cond, GenTreePtr colon);

    GenTreePtr gtNewLargeOperNode(genTreeOps oper,
                                  var_types  type = TYP_I_IMPL,
                                  GenTreePtr op1  = nullptr,
                                  GenTreePtr op2  = nullptr);

    GenTreeIntCon* gtNewIconNode(ssize_t value, var_types type = TYP_INT);

    GenTree* gtNewPhysRegNode(regNumber reg, var_types type);

    GenTree* gtNewPhysRegNode(regNumber reg, GenTree* src);

    GenTreePtr gtNewJmpTableNode();
    GenTreePtr gtNewIconHandleNode(
        size_t value, unsigned flags, FieldSeqNode* fields = nullptr, unsigned handle1 = 0, void* handle2 = nullptr);

    unsigned gtTokenToIconFlags(unsigned token);

    GenTreePtr gtNewIconEmbHndNode(void*    value,
                                   void*    pValue,
                                   unsigned flags,
                                   unsigned handle1           = 0,
                                   void*    handle2           = nullptr,
                                   void*    compileTimeHandle = nullptr);

    GenTreePtr gtNewIconEmbScpHndNode(CORINFO_MODULE_HANDLE scpHnd, unsigned hnd1 = 0, void* hnd2 = nullptr);
    GenTreePtr gtNewIconEmbClsHndNode(CORINFO_CLASS_HANDLE clsHnd, unsigned hnd1 = 0, void* hnd2 = nullptr);
    GenTreePtr gtNewIconEmbMethHndNode(CORINFO_METHOD_HANDLE methHnd, unsigned hnd1 = 0, void* hnd2 = nullptr);
    GenTreePtr gtNewIconEmbFldHndNode(CORINFO_FIELD_HANDLE fldHnd, unsigned hnd1 = 0, void* hnd2 = nullptr);

    GenTreePtr gtNewStringLiteralNode(InfoAccessType iat, void* pValue);

    GenTreePtr gtNewLconNode(__int64 value);

    GenTreePtr gtNewDconNode(double value);

    GenTreePtr gtNewSconNode(int CPX, CORINFO_MODULE_HANDLE scpHandle);

    GenTreePtr gtNewZeroConNode(var_types type);

    GenTreePtr gtNewOneConNode(var_types type);

    GenTreeBlkOp* gtNewBlkOpNode(
        genTreeOps oper, GenTreePtr dst, GenTreePtr srcOrFillVal, GenTreePtr sizeOrClsTok, bool volatil);

protected:
    void gtBlockOpInit(GenTreePtr node, genTreeOps oper, GenTreePtr dst, GenTreePtr src, GenTreePtr size, bool volatil);

public:
    GenTreeObj* gtNewObjNode(CORINFO_CLASS_HANDLE structHnd, GenTreePtr addr);

    GenTreeBlkOp* gtNewCpObjNode(GenTreePtr dst, GenTreePtr src, CORINFO_CLASS_HANDLE structHnd, bool volatil);

    GenTreeBlkOp* gtCloneCpObjNode(GenTreeCpObj* source);

    GenTreeArgList* gtNewListNode(GenTreePtr op1, GenTreeArgList* op2);

    GenTreeCall* gtNewCallNode(gtCallTypes           callType,
                               CORINFO_METHOD_HANDLE handle,
                               var_types             type,
                               GenTreeArgList*       args,
                               IL_OFFSETX            ilOffset = BAD_IL_OFFSET);

    GenTreeCall* gtNewIndCallNode(GenTreePtr      addr,
                                  var_types       type,
                                  GenTreeArgList* args,
                                  IL_OFFSETX      ilOffset = BAD_IL_OFFSET);

    GenTreeCall* gtNewHelperCallNode(unsigned        helper,
                                     var_types       type,
                                     unsigned        flags = 0,
                                     GenTreeArgList* args  = nullptr);

    GenTreePtr gtNewLclvNode(unsigned lnum, var_types type, IL_OFFSETX ILoffs = BAD_IL_OFFSET);

#ifdef FEATURE_SIMD
    GenTreeSIMD* gtNewSIMDNode(
        var_types type, GenTreePtr op1, SIMDIntrinsicID simdIntrinsicID, var_types baseType, unsigned size);
    GenTreeSIMD* gtNewSIMDNode(var_types       type,
                               GenTreePtr      op1,
                               GenTreePtr      op2,
                               SIMDIntrinsicID simdIntrinsicID,
                               var_types       baseType,
                               unsigned        size);
#endif

    GenTreePtr gtNewLclLNode(unsigned lnum, var_types type, IL_OFFSETX ILoffs = BAD_IL_OFFSET);
    GenTreeLclFld* gtNewLclFldNode(unsigned lnum, var_types type, unsigned offset);
    GenTreePtr gtNewInlineCandidateReturnExpr(GenTreePtr inlineCandidate, var_types type);

    GenTreePtr gtNewCodeRef(BasicBlock* block);

    GenTreePtr gtNewFieldRef(
        var_types typ, CORINFO_FIELD_HANDLE fldHnd, GenTreePtr obj = nullptr, DWORD offset = 0, bool nullcheck = false);

    GenTreePtr gtNewIndexRef(var_types typ, GenTreePtr arrayOp, GenTreePtr indexOp);

    GenTreeArgList* gtNewArgList(GenTreePtr op);

    GenTreeArgList* gtNewArgList(GenTreePtr op1, GenTreePtr op2);

    static fgArgTabEntryPtr gtArgEntryByArgNum(GenTreePtr call, unsigned argNum);
    static fgArgTabEntryPtr gtArgEntryByNode(GenTreePtr call, GenTreePtr node);
    fgArgTabEntryPtr gtArgEntryByLateArgIndex(GenTreePtr call, unsigned lateArgInx);
    bool gtArgIsThisPtr(fgArgTabEntryPtr argEntry);

    GenTreePtr gtNewAssignNode(GenTreePtr dst, GenTreePtr src);

    GenTreePtr gtNewTempAssign(unsigned tmp, GenTreePtr val);

    GenTreePtr gtNewRefCOMfield(GenTreePtr              objPtr,
                                CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                CORINFO_ACCESS_FLAGS    access,
                                CORINFO_FIELD_INFO*     pFieldInfo,
                                var_types               lclTyp,
                                CORINFO_CLASS_HANDLE    structType,
                                GenTreePtr              assg);

    GenTreePtr gtNewNothingNode();

    GenTreePtr gtNewArgPlaceHolderNode(var_types type, CORINFO_CLASS_HANDLE clsHnd);

    GenTreePtr gtUnusedValNode(GenTreePtr expr);

    GenTreePtr gtNewCastNode(var_types typ, GenTreePtr op1, var_types castType);

    GenTreePtr gtNewCastNodeL(var_types typ, GenTreePtr op1, var_types castType);

    GenTreePtr gtNewAllocObjNode(unsigned int helper, CORINFO_CLASS_HANDLE clsHnd, var_types type, GenTreePtr op1);

    //------------------------------------------------------------------------
    // Other GenTree functions

    GenTreePtr gtClone(GenTree* tree, bool complexOK = false);

    GenTreePtr gtCloneExpr(GenTree* tree, unsigned addFlags = 0, unsigned varNum = (unsigned)-1, int varVal = 0);

    GenTreePtr gtReplaceTree(GenTreePtr stmt, GenTreePtr tree, GenTreePtr replacementTree);

    void gtUpdateSideEffects(GenTreePtr tree, unsigned oldGtFlags, unsigned newGtFlags);

    // Returns "true" iff the complexity (not formally defined, but first interpretation
    // is #of nodes in subtree) of "tree" is greater than "limit".
    // (This is somewhat redundant with the "gtCostEx/gtCostSz" fields, but can be used
    // before they have been set.)
    bool gtComplexityExceeds(GenTreePtr* tree, unsigned limit);

    bool gtCompareTree(GenTree* op1, GenTree* op2);

    GenTreePtr gtReverseCond(GenTree* tree);

    bool gtHasRef(GenTree* tree, ssize_t lclNum, bool defOnly);

    bool gtHasLocalsWithAddrOp(GenTreePtr tree);

    unsigned gtHashValue(GenTree* tree);

    unsigned gtSetListOrder(GenTree* list, bool regs);

    void gtWalkOp(GenTree** op1, GenTree** op2, GenTree* adr, bool constOnly);

#ifdef DEBUG
    GenTreePtr gtWalkOpEffectiveVal(GenTreePtr op);
#endif

    void gtPrepareCost(GenTree* tree);
    bool gtIsLikelyRegVar(GenTree* tree);

    unsigned gtSetEvalOrderAndRestoreFPstkLevel(GenTree* tree);

    // Returns true iff the secondNode can be swapped with firstNode.
    bool gtCanSwapOrder(GenTree* firstNode, GenTree* secondNode);

    unsigned gtSetEvalOrder(GenTree* tree);

#if FEATURE_STACK_FP_X87
    bool gtFPstLvlRedo;
    void gtComputeFPlvls(GenTreePtr tree);
#endif // FEATURE_STACK_FP_X87

    void gtSetStmtInfo(GenTree* stmt);

    // Returns "true" iff "node" has any of the side effects in "flags".
    bool gtNodeHasSideEffects(GenTreePtr node, unsigned flags);

    // Returns "true" iff "tree" or its (transitive) children have any of the side effects in "flags".
    bool gtTreeHasSideEffects(GenTreePtr tree, unsigned flags);

    // Appends 'expr' in front of 'list'
    //    'list' will typically start off as 'nullptr'
    //    when 'list' is non-null a GT_COMMA node is used to insert 'expr'
    GenTreePtr gtBuildCommaList(GenTreePtr list, GenTreePtr expr);

    void gtExtractSideEffList(GenTreePtr  expr,
                              GenTreePtr* pList,
                              unsigned    flags      = GTF_SIDE_EFFECT,
                              bool        ignoreRoot = false);

    GenTreePtr gtGetThisArg(GenTreePtr call);

    // Static fields of struct types (and sometimes the types that those are reduced to) are represented by having the
    // static field contain an object pointer to the boxed struct.  This simplifies the GC implementation...but
    // complicates the JIT somewhat.  This predicate returns "true" iff a node with type "fieldNodeType", representing
    // the given "fldHnd", is such an object pointer.
    bool gtIsStaticFieldPtrToBoxedStruct(var_types fieldNodeType, CORINFO_FIELD_HANDLE fldHnd);

    // Return true if call is a recursive call; return false otherwise.
    bool gtIsRecursiveCall(GenTreeCall* call)
    {
        return (call->gtCallMethHnd == info.compMethodHnd);
    }

    //-------------------------------------------------------------------------

    GenTreePtr gtFoldExpr(GenTreePtr tree);
    GenTreePtr
#ifdef __clang__
        // TODO-Amd64-Unix: Remove this when the clang optimizer is fixed and/or the method implementation is
        // refactored in a simpler code. This is a workaround for a bug in the clang-3.5 optimizer. The issue is that in
        // release build the optimizer is mistyping (or just wrongly decides to use 32 bit operation for a corner case
        // of MIN_LONG) the args of the (ltemp / lval2) to int (it does a 32 bit div operation instead of 64 bit) - see
        // the implementation of the method in gentree.cpp. For the case of lval1 and lval2 equal to MIN_LONG
        // (0x8000000000000000) this results in raising a SIGFPE. The method implementation is rather complex. Disable
        // optimizations for now.
        __attribute__((optnone))
#endif // __clang__
        gtFoldExprConst(GenTreePtr tree);
    GenTreePtr gtFoldExprSpecial(GenTreePtr tree);
    GenTreePtr gtFoldExprCompare(GenTreePtr tree);

    //-------------------------------------------------------------------------
    // Get the handle, if any.
    CORINFO_CLASS_HANDLE gtGetStructHandleIfPresent(GenTreePtr tree);
    // Get the handle, and assert if not found.
    CORINFO_CLASS_HANDLE gtGetStructHandle(GenTreePtr tree);

//-------------------------------------------------------------------------
// Functions to display the trees

#ifdef DEBUG
    void gtDispNode(GenTreePtr tree, IndentStack* indentStack, __in_z const char* msg, bool isLIR);

    void gtDispVN(GenTreePtr tree);
    void gtDispConst(GenTreePtr tree);
    void gtDispLeaf(GenTreePtr tree, IndentStack* indentStack);
    void gtDispNodeName(GenTreePtr tree);
    void gtDispRegVal(GenTreePtr tree);

    enum IndentInfo
    {
        IINone,
        IIArc,
        IIArcTop,
        IIArcBottom,
        IIEmbedded,
        IIError,
        IndentInfoCount
    };
    void gtDispChild(GenTreePtr           child,
                     IndentStack*         indentStack,
                     IndentInfo           arcType,
                     __in_opt const char* msg     = nullptr,
                     bool                 topOnly = false);
    void gtDispTree(GenTreePtr           tree,
                    IndentStack*         indentStack = nullptr,
                    __in_opt const char* msg         = nullptr,
                    bool                 topOnly     = false,
                    bool                 isLIR       = false);
    void gtGetLclVarNameInfo(unsigned lclNum, const char** ilKindOut, const char** ilNameOut, unsigned* ilNumOut);
    int gtGetLclVarName(unsigned lclNum, char* buf, unsigned buf_remaining);
    char* gtGetLclVarName(unsigned lclNum);
    void gtDispLclVar(unsigned varNum, bool padForBiggestDisp = true);
    void gtDispTreeList(GenTreePtr tree, IndentStack* indentStack = nullptr);
    void gtGetArgMsg(GenTreePtr call, GenTreePtr arg, unsigned argNum, int listCount, char* bufp, unsigned bufLength);
    void gtGetLateArgMsg(GenTreePtr call, GenTreePtr arg, int argNum, int listCount, char* bufp, unsigned bufLength);
    void gtDispArgList(GenTreePtr tree, IndentStack* indentStack);
    void gtDispFieldSeq(FieldSeqNode* pfsn);

    void gtDispRange(LIR::ReadOnlyRange const& range);

    void gtDispTreeRange(LIR::Range& containingRange, GenTree* tree);

    void gtDispLIRNode(GenTree* node);
#endif

    // For tree walks

    enum fgWalkResult
    {
        WALK_CONTINUE,
        WALK_SKIP_SUBTREES,
        WALK_ABORT
    };
    struct fgWalkData;
    typedef fgWalkResult(fgWalkPreFn)(GenTreePtr* pTree, fgWalkData* data);
    typedef fgWalkResult(fgWalkPostFn)(GenTreePtr* pTree, fgWalkData* data);

#ifdef DEBUG
    static fgWalkPreFn gtAssertColonCond;
#endif
    static fgWalkPreFn gtMarkColonCond;
    static fgWalkPreFn gtClearColonCond;

    GenTreePtr* gtFindLink(GenTreePtr stmt, GenTreePtr node);
    bool gtHasCatchArg(GenTreePtr tree);
    bool gtHasUnmanagedCall(GenTreePtr tree);

    typedef ArrayStack<GenTree*> GenTreeStack;

    static bool gtHasCallOnStack(GenTreeStack* parentStack);
    void gtCheckQuirkAddrExposedLclVar(GenTreePtr argTree, GenTreeStack* parentStack);

//=========================================================================
// BasicBlock functions
#ifdef DEBUG
    // This is a debug flag we will use to assert when creating block during codegen
    // as this interferes with procedure splitting. If you know what you're doing, set
    // it to true before creating the block. (DEBUG only)
    bool fgSafeBasicBlockCreation;
#endif

    BasicBlock* bbNewBasicBlock(BBjumpKinds jumpKind);

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           LclVarsInfo                                     XX
    XX                                                                           XX
    XX   The variables to be used by the code generator.                         XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

    //
    // For both PROMOTION_TYPE_NONE and PROMOTION_TYPE_DEPENDENT the struct will
    // be placed in the stack frame and it's fields must be laid out sequentially.
    //
    // For PROMOTION_TYPE_INDEPENDENT each of the struct's fields is replaced by
    //  a local variable that can be enregistered or placed in the stack frame.
    //  The fields do not need to be laid out sequentially
    //
    enum lvaPromotionType
    {
        PROMOTION_TYPE_NONE,        // The struct local is not promoted
        PROMOTION_TYPE_INDEPENDENT, // The struct local is promoted,
                                    //   and its field locals are independent of its parent struct local.
        PROMOTION_TYPE_DEPENDENT    // The struct local is promoted,
                                    //   but its field locals depend on its parent struct local.
    };

    static int __cdecl RefCntCmp(const void* op1, const void* op2);
    static int __cdecl WtdRefCntCmp(const void* op1, const void* op2);

    /*****************************************************************************/

    enum FrameLayoutState
    {
        NO_FRAME_LAYOUT,
        INITIAL_FRAME_LAYOUT,
        PRE_REGALLOC_FRAME_LAYOUT,
        REGALLOC_FRAME_LAYOUT,
        TENTATIVE_FRAME_LAYOUT,
        FINAL_FRAME_LAYOUT
    };

public:
    bool     lvaRefCountingStarted; // Set to true when we have started counting the local vars
    bool     lvaLocalVarRefCounted; // Set to true after we have called lvaMarkLocalVars()
    bool     lvaSortAgain;          // true: We need to sort the lvaTable
    bool     lvaTrackedFixed;       // true: We cannot add new 'tracked' variable
    unsigned lvaCount;              // total number of locals

    unsigned   lvaRefCount; // total number of references to locals
    LclVarDsc* lvaTable;    // variable descriptor table
    unsigned   lvaTableCnt; // lvaTable size (>= lvaCount)

    LclVarDsc** lvaRefSorted; // table sorted by refcount

    unsigned short lvaTrackedCount;       // actual # of locals being tracked
    unsigned lvaTrackedCountInSizeTUnits; // min # of size_t's sufficient to hold a bit for all the locals being tracked

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    // Only for AMD64 System V cache the first caller stack homed argument.
    unsigned lvaFirstStackIncomingArgNum; // First argument with stack slot in the caller.
#endif                                    // !FEATURE_UNIX_AMD64_STRUCT_PASSING

#ifdef DEBUG
    VARSET_TP lvaTrackedVars; // set of tracked variables
#endif
#ifndef _TARGET_64BIT_
    VARSET_TP lvaLongVars; // set of long (64-bit) variables
#endif
    VARSET_TP lvaFloatVars; // set of floating-point (32-bit and 64-bit) variables

    unsigned lvaCurEpoch; // VarSets are relative to a specific set of tracked var indices.
                          // It that changes, this changes.  VarSets from different epochs
                          // cannot be meaningfully combined.

    unsigned GetCurLVEpoch()
    {
        return lvaCurEpoch;
    }

    // reverse map of tracked number to var number
    unsigned lvaTrackedToVarNum[lclMAX_TRACKED];

#ifdef LEGACY_BACKEND
    // variable interference graph
    VARSET_TP lvaVarIntf[lclMAX_TRACKED];
#endif

    // variable preference graph
    VARSET_TP lvaVarPref[lclMAX_TRACKED];

#if DOUBLE_ALIGN
#ifdef DEBUG
    // # of procs compiled a with double-aligned stack
    static unsigned s_lvaDoubleAlignedProcsCount;
#endif
#endif

    // Getters and setters for address-exposed and do-not-enregister local var properties.
    bool lvaVarAddrExposed(unsigned varNum);
    void lvaSetVarAddrExposed(unsigned varNum);
    bool lvaVarDoNotEnregister(unsigned varNum);
#ifdef DEBUG
    // Reasons why we can't enregister.  Some of these correspond to debug properties of local vars.
    enum DoNotEnregisterReason
    {
        DNER_AddrExposed,
        DNER_IsStruct,
        DNER_LocalField,
        DNER_VMNeedsStackAddr,
        DNER_LiveInOutOfHandler,
        DNER_LiveAcrossUnmanagedCall,
        DNER_BlockOp, // Is read or written via a block operation that explicitly takes the address.
#ifdef JIT32_GCENCODER
        DNER_PinningRef,
#endif
    };
#endif
    void lvaSetVarDoNotEnregister(unsigned varNum DEBUGARG(DoNotEnregisterReason reason));

    unsigned lvaVarargsHandleArg;
#ifdef _TARGET_X86_
    unsigned lvaVarargsBaseOfStkArgs; // Pointer (computed based on incoming varargs handle) to the start of the stack
                                      // arguments
#endif                                // _TARGET_X86_

    unsigned lvaInlinedPInvokeFrameVar; // variable representing the InlinedCallFrame
    unsigned lvaReversePInvokeFrameVar; // variable representing the reverse PInvoke frame
#if FEATURE_FIXED_OUT_ARGS
    unsigned lvaPInvokeFrameRegSaveVar; // variable representing the RegSave for PInvoke inlining.
#endif
    unsigned lvaMonAcquired; // boolean variable introduced into in synchronized methods
                             // that tracks whether the lock has been taken

    unsigned lvaArg0Var; // The lclNum of arg0. Normally this will be info.compThisArg.
                         // However, if there is a "ldarga 0" or "starg 0" in the IL,
                         // we will redirect all "ldarg(a) 0" and "starg 0" to this temp.

    unsigned lvaInlineeReturnSpillTemp; // The temp to spill the non-VOID return expression
                                        // in case there are multiple BBJ_RETURN blocks in the inlinee.

#if FEATURE_FIXED_OUT_ARGS
    unsigned lvaOutgoingArgSpaceVar;  // dummy TYP_LCLBLK var for fixed outgoing argument space
    unsigned lvaOutgoingArgSpaceSize; // size of fixed outgoing argument space
#endif                                // FEATURE_FIXED_OUT_ARGS

#ifdef _TARGET_ARM_
    // On architectures whose ABIs allow structs to be passed in registers, struct promotion will sometimes
    // require us to "rematerialize" a struct from it's separate constituent field variables.  Packing several sub-word
    // field variables into an argument register is a hard problem.  It's easier to reserve a word of memory into which
    // such field can be copied, after which the assembled memory word can be read into the register.  We will allocate
    // this variable to be this scratch word whenever struct promotion occurs.
    unsigned lvaPromotedStructAssemblyScratchVar;
#endif // _TARGET_ARM_

#ifdef DEBUG
    unsigned lvaReturnEspCheck; // confirms ESP not corrupted on return
    unsigned lvaCallEspCheck;   // confirms ESP not corrupted after a call
#endif

    bool lvaGenericsContextUsed;

    bool lvaKeepAliveAndReportThis(); // Synchronized instance method of a reference type, or
                                      // CORINFO_GENERICS_CTXT_FROM_THIS?
    bool lvaReportParamTypeArg();     // Exceptions and CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG?

//-------------------------------------------------------------------------
// All these frame offsets are inter-related and must be kept in sync

#if !FEATURE_EH_FUNCLETS
    // This is used for the callable handlers
    unsigned lvaShadowSPslotsVar; // TYP_BLK variable for all the shadow SP slots
#endif                            // FEATURE_EH_FUNCLETS

    unsigned lvaCachedGenericContextArgOffs;
    unsigned lvaCachedGenericContextArgOffset(); // For CORINFO_CALLCONV_PARAMTYPE and if generic context is passed as
                                                 // THIS pointer

    unsigned lvaLocAllocSPvar; // variable which has the result of the last alloca/localloc

    unsigned lvaNewObjArrayArgs; // variable with arguments for new MD array helper

    // TODO-Review: Prior to reg predict we reserve 24 bytes for Spill temps.
    //              after the reg predict we will use a computed maxTmpSize
    //              which is based upon the number of spill temps predicted by reg predict
    //              All this is necessary because if we under-estimate the size of the spill
    //              temps we could fail when encoding instructions that reference stack offsets for ARM.
    //
    // Pre codegen max spill temp size.
    static const unsigned MAX_SPILL_TEMP_SIZE = 24;

    //-------------------------------------------------------------------------

    unsigned lvaGetMaxSpillTempSize();
#ifdef _TARGET_ARM_
    bool lvaIsPreSpilled(unsigned lclNum, regMaskTP preSpillMask);
#endif // _TARGET_ARM_
    void lvaAssignFrameOffsets(FrameLayoutState curState);
    void lvaFixVirtualFrameOffsets();

#ifndef LEGACY_BACKEND
    void lvaUpdateArgsWithInitialReg();
#endif // !LEGACY_BACKEND

    void lvaAssignVirtualFrameOffsetsToArgs();
#ifdef UNIX_AMD64_ABI
    int lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs, int* callerArgOffset);
#else  // !UNIX_AMD64_ABI
    int lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs);
#endif // !UNIX_AMD64_ABI
    void lvaAssignVirtualFrameOffsetsToLocals();
    int lvaAllocLocalAndSetVirtualOffset(unsigned lclNum, unsigned size, int stkOffs);
#ifdef _TARGET_AMD64_
    // Returns true if compCalleeRegsPushed (including RBP if used as frame pointer) is even.
    bool lvaIsCalleeSavedIntRegCountEven();
#endif
    void lvaAlignFrame();
    void lvaAssignFrameOffsetsToPromotedStructs();
    int lvaAllocateTemps(int stkOffs, bool mustDoubleAlign);

#ifdef DEBUG
    void lvaDumpRegLocation(unsigned lclNum);
    void lvaDumpFrameLocation(unsigned lclNum);
    void lvaDumpEntry(unsigned lclNum, FrameLayoutState curState, size_t refCntWtdWidth = 6);
    void lvaTableDump(FrameLayoutState curState = NO_FRAME_LAYOUT); // NO_FRAME_LAYOUT means use the current frame
                                                                    // layout state defined by lvaDoneFrameLayout
#endif

// Limit frames size to 1GB. The maximum is 2GB in theory - make it intentionally smaller
// to avoid bugs from borderline cases.
#define MAX_FrameSize 0x3FFFFFFF
    void lvaIncrementFrameSize(unsigned size);

    unsigned lvaFrameSize(FrameLayoutState curState);

    // Returns the caller-SP-relative offset for the SP/FP relative offset determined by FP based.
    int lvaToCallerSPRelativeOffset(int offs, bool isFpBased);

    // Returns the caller-SP-relative offset for the local variable "varNum."
    int lvaGetCallerSPRelativeOffset(unsigned varNum);

    // Returns the SP-relative offset for the local variable "varNum". Illegal to ask this for functions with localloc.
    int lvaGetSPRelativeOffset(unsigned varNum);

    int lvaToInitialSPRelativeOffset(unsigned offset, bool isFpBased);
    int lvaGetInitialSPRelativeOffset(unsigned varNum);

    //------------------------ For splitting types ----------------------------

    void lvaInitTypeRef();

    void lvaInitArgs(InitVarDscInfo* varDscInfo);
    void lvaInitThisPtr(InitVarDscInfo* varDscInfo);
    void lvaInitRetBuffArg(InitVarDscInfo* varDscInfo);
    void lvaInitUserArgs(InitVarDscInfo* varDscInfo);
    void lvaInitGenericsCtxt(InitVarDscInfo* varDscInfo);
    void lvaInitVarArgsHandle(InitVarDscInfo* varDscInfo);

    void lvaInitVarDsc(LclVarDsc*              varDsc,
                       unsigned                varNum,
                       CorInfoType             corInfoType,
                       CORINFO_CLASS_HANDLE    typeHnd,
                       CORINFO_ARG_LIST_HANDLE varList,
                       CORINFO_SIG_INFO*       varSig);

    static unsigned lvaTypeRefMask(var_types type);

    var_types lvaGetActualType(unsigned lclNum);
    var_types lvaGetRealType(unsigned lclNum);

    //-------------------------------------------------------------------------

    void lvaInit();

    unsigned lvaArgSize(const void* argTok);
    unsigned lvaLclSize(unsigned varNum);
    unsigned lvaLclExactSize(unsigned varNum);

    bool lvaLclVarRefs(GenTreePtr tree, GenTreePtr* findPtr, varRefKinds* refsPtr, void* result);

    // Call lvaLclVarRefs on "true"; accumulate "*result" into whichever of
    // "allVars" and "trkdVars" is indiated by the nullness of "findPtr"; return
    // the return result.
    bool lvaLclVarRefsAccum(
        GenTreePtr tree, GenTreePtr* findPtr, varRefKinds* refsPtr, ALLVARSET_TP* allVars, VARSET_TP* trkdVars);

    // If "findPtr" is non-NULL, assumes "result" is an "ALLVARSET_TP*", and
    // (destructively) unions "allVars" into "*result".  Otherwise, assumes "result" is a "VARSET_TP*",
    // and (destructively) unions "trkedVars" into "*result".
    void lvaLclVarRefsAccumIntoRes(GenTreePtr*         findPtr,
                                   void*               result,
                                   ALLVARSET_VALARG_TP allVars,
                                   VARSET_VALARG_TP    trkdVars);

    bool lvaHaveManyLocals() const;

    unsigned lvaGrabTemp(bool shortLifetime DEBUGARG(const char* reason));
    unsigned lvaGrabTemps(unsigned cnt DEBUGARG(const char* reason));
    unsigned lvaGrabTempWithImplicitUse(bool shortLifetime DEBUGARG(const char* reason));

    void lvaSortOnly();
    void lvaSortByRefCount();
    void lvaDumpRefCounts();

    void lvaMarkLocalVars(BasicBlock* block);

    void lvaMarkLocalVars(); // Local variable ref-counting

    void lvaAllocOutgoingArgSpace(); // 'Commit' lvaOutgoingArgSpaceSize and lvaOutgoingArgSpaceVar

    VARSET_VALRET_TP lvaStmtLclMask(GenTreePtr stmt);

    static fgWalkPreFn lvaIncRefCntsCB;
    void lvaIncRefCnts(GenTreePtr tree);

    static fgWalkPreFn lvaDecRefCntsCB;
    void lvaDecRefCnts(GenTreePtr tree);
    void lvaDecRefCnts(BasicBlock* basicBlock, GenTreePtr tree);
    void lvaRecursiveDecRefCounts(GenTreePtr tree);
    void lvaRecursiveIncRefCounts(GenTreePtr tree);

#ifdef DEBUG
    struct lvaStressLclFldArgs
    {
        Compiler* m_pCompiler;
        bool      m_bFirstPass;
    };

    static fgWalkPreFn lvaStressLclFldCB;
    void               lvaStressLclFld();

    void lvaDispVarSet(VARSET_VALARG_TP set, VARSET_VALARG_TP allVars);
    void lvaDispVarSet(VARSET_VALARG_TP set);

#endif

#ifdef _TARGET_ARM_
    int lvaFrameAddress(int varNum, bool mustBeFPBased, regNumber* pBaseReg, int addrModeOffset);
#else
    int lvaFrameAddress(int varNum, bool* pFPbased);
#endif

    bool lvaIsParameter(unsigned varNum);
    bool lvaIsRegArgument(unsigned varNum);
    BOOL lvaIsOriginalThisArg(unsigned varNum); // Is this varNum the original this argument?
    BOOL lvaIsOriginalThisReadOnly();           // return TRUE if there is no place in the code
                                                // that writes to arg0

    // Struct parameters that are passed by reference are marked as both lvIsParam and lvIsTemp
    // (this is an overload of lvIsTemp because there are no temp parameters).
    // For x64 this is 3, 5, 6, 7, >8 byte structs that are passed by reference.
    // For ARM64, this is structs larger than 16 bytes that are passed by reference.
    bool lvaIsImplicitByRefLocal(unsigned varNum)
    {
#if defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
        LclVarDsc* varDsc = &(lvaTable[varNum]);
        if (varDsc->lvIsParam && varDsc->lvIsTemp)
        {
            assert((varDsc->lvType == TYP_STRUCT) || (varDsc->lvType == TYP_BYREF));
            return true;
        }
#endif // defined(_TARGET_AMD64_) || defined(_TARGET_ARM64_)
        return false;
    }

    // Returns true if this local var is a multireg struct
    bool lvaIsMultiregStruct(LclVarDsc* varDsc);

    // If the class is a TYP_STRUCT, get/set a class handle describing it

    CORINFO_CLASS_HANDLE lvaGetStruct(unsigned varNum);
    void lvaSetStruct(unsigned varNum, CORINFO_CLASS_HANDLE typeHnd, bool unsafeValueClsCheck, bool setTypeInfo = true);

#define MAX_NumOfFieldsInPromotableStruct 4 // Maximum number of fields in promotable struct

    // Info about struct fields
    struct lvaStructFieldInfo
    {
        CORINFO_FIELD_HANDLE fldHnd;
        unsigned char        fldOffset;
        unsigned char        fldOrdinal;
        var_types            fldType;
        unsigned             fldSize;
        CORINFO_CLASS_HANDLE fldTypeHnd;
    };

    // Info about struct to be promoted.
    struct lvaStructPromotionInfo
    {
        CORINFO_CLASS_HANDLE typeHnd;
        bool                 canPromote;
        bool                 requiresScratchVar;
        bool                 containsHoles;
        bool                 customLayout;
        unsigned char        fieldCnt;
        lvaStructFieldInfo   fields[MAX_NumOfFieldsInPromotableStruct];

        lvaStructPromotionInfo()
            : typeHnd(nullptr), canPromote(false), requiresScratchVar(false), containsHoles(false), customLayout(false)
        {
        }
    };

    static int __cdecl lvaFieldOffsetCmp(const void* field1, const void* field2);
    void lvaCanPromoteStructType(CORINFO_CLASS_HANDLE    typeHnd,
                                 lvaStructPromotionInfo* StructPromotionInfo,
                                 bool                    sortFields);
    void lvaCanPromoteStructVar(unsigned lclNum, lvaStructPromotionInfo* StructPromotionInfo);
    void lvaPromoteStructVar(unsigned lclNum, lvaStructPromotionInfo* StructPromotionInfo);
#if !defined(_TARGET_64BIT_)
    void lvaPromoteLongVars();
#endif // !defined(_TARGET_64BIT_)
    unsigned lvaGetFieldLocal(LclVarDsc* varDsc, unsigned int fldOffset);
    lvaPromotionType lvaGetPromotionType(const LclVarDsc* varDsc);
    lvaPromotionType lvaGetPromotionType(unsigned varNum);
    lvaPromotionType lvaGetParentPromotionType(const LclVarDsc* varDsc);
    lvaPromotionType lvaGetParentPromotionType(unsigned varNum);
    bool lvaIsFieldOfDependentlyPromotedStruct(const LclVarDsc* varDsc);
    bool lvaIsGCTracked(const LclVarDsc* varDsc);

    BYTE* lvaGetGcLayout(unsigned varNum);
    bool lvaTypeIsGC(unsigned varNum);
    unsigned lvaGSSecurityCookie; // LclVar number
    bool     lvaTempsHaveLargerOffsetThanVars();

    unsigned lvaSecurityObject;  // variable representing the security object on the stack
    unsigned lvaStubArgumentVar; // variable representing the secret stub argument coming in EAX

#if FEATURE_EH_FUNCLETS
    unsigned lvaPSPSym; // variable representing the PSPSym
#endif

    InlineInfo*     impInlineInfo;
    InlineStrategy* m_inlineStrategy;

    // The Compiler* that is the root of the inlining tree of which "this" is a member.
    Compiler* impInlineRoot();

#if defined(DEBUG) || defined(INLINE_DATA)
    unsigned __int64 getInlineCycleCount()
    {
        return m_compCycles;
    }
#endif // defined(DEBUG) || defined(INLINE_DATA)

    bool fgNoStructPromotion;      // Set to TRUE to turn off struct promotion for this method.
    bool fgNoStructParamPromotion; // Set to TRUE to turn off struct promotion for parameters this method.

    //=========================================================================
    //                          PROTECTED
    //=========================================================================

protected:
//---------------- Local variable ref-counting ----------------------------

#if ASSERTION_PROP
    BasicBlock* lvaMarkRefsCurBlock;
    GenTreePtr  lvaMarkRefsCurStmt;
#endif
    BasicBlock::weight_t lvaMarkRefsWeight;

    static fgWalkPreFn lvaMarkLclRefsCallback;
    void lvaMarkLclRefs(GenTreePtr tree);

    // Keeps the mapping from SSA #'s to VN's for the implicit "Heap" variable.
    PerSsaArray lvHeapPerSsaData;
    unsigned    lvHeapNumSsaNames;

public:
    // Returns the address of the per-Ssa data for "Heap" at the given ssaNum (which is required
    // not to be the SsaConfig::RESERVED_SSA_NUM, which indicates that the variable is
    // not an SSA variable).
    LclSsaVarDsc* GetHeapPerSsaData(unsigned ssaNum)
    {
        assert(ssaNum != SsaConfig::RESERVED_SSA_NUM);
        assert(SsaConfig::RESERVED_SSA_NUM == 0);
        ssaNum--;
        assert(ssaNum < lvHeapNumSsaNames);
        return &lvHeapPerSsaData.GetRef(ssaNum);
    }

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           Importer                                        XX
    XX                                                                           XX
    XX   Imports the given method and converts it to semantic trees              XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    void impInit();

    void impImport(BasicBlock* method);

    CORINFO_CLASS_HANDLE impGetRefAnyClass();
    CORINFO_CLASS_HANDLE impGetRuntimeArgumentHandle();
    CORINFO_CLASS_HANDLE impGetTypeHandleClass();
    CORINFO_CLASS_HANDLE impGetStringClass();
    CORINFO_CLASS_HANDLE impGetObjectClass();

    //=========================================================================
    //                          PROTECTED
    //=========================================================================

protected:
    //-------------------- Stack manipulation ---------------------------------

    unsigned impStkSize; // Size of the full stack

#define SMALL_STACK_SIZE 16 // number of elements in impSmallStack

    StackEntry impSmallStack[SMALL_STACK_SIZE]; // Use this array if possible

    struct SavedStack // used to save/restore stack contents.
    {
        unsigned    ssDepth; // number of values on stack
        StackEntry* ssTrees; // saved tree values
    };

    bool impIsPrimitive(CorInfoType type);
    bool impILConsumesAddr(const BYTE* codeAddr, CORINFO_METHOD_HANDLE fncHandle, CORINFO_MODULE_HANDLE scpHandle);

    void impResolveToken(const BYTE* addr, CORINFO_RESOLVED_TOKEN* pResolvedToken, CorInfoTokenKind kind);
    void impPushOnStackNoType(GenTreePtr tree);

    void impPushOnStack(GenTreePtr tree, typeInfo ti);
    void       impPushNullObjRefOnStack();
    StackEntry impPopStack();
    StackEntry impPopStack(CORINFO_CLASS_HANDLE& structTypeRet);
    GenTreePtr impPopStack(typeInfo& ti);
    StackEntry& impStackTop(unsigned n = 0);

    void impSaveStackState(SavedStack* savePtr, bool copy);
    void impRestoreStackState(SavedStack* savePtr);

    GenTreePtr impImportLdvirtftn(GenTreePtr              thisPtr,
                                  CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                  CORINFO_CALL_INFO*      pCallInfo);

    void impImportAndPushBox(CORINFO_RESOLVED_TOKEN* pResolvedToken);

    void impImportNewObjArray(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);

    bool impCanPInvokeInline(var_types callRetTyp);
    bool impCanPInvokeInlineCallSite(var_types callRetTyp);
    void impCheckForPInvokeCall(GenTreePtr call, CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* sig, unsigned mflags);
    GenTreePtr impImportIndirectCall(CORINFO_SIG_INFO* sig, IL_OFFSETX ilOffset = BAD_IL_OFFSET);
    void impPopArgsForUnmanagedCall(GenTreePtr call, CORINFO_SIG_INFO* sig);

    void impInsertHelperCall(CORINFO_HELPER_DESC* helperCall);
    void impHandleAccessAllowed(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall);
    void impHandleAccessAllowedInternal(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall);

    void impInsertCalloutForDelegate(CORINFO_METHOD_HANDLE callerMethodHnd,
                                     CORINFO_METHOD_HANDLE calleeMethodHnd,
                                     CORINFO_CLASS_HANDLE  delegateTypeHnd);

    var_types impImportCall(OPCODE                  opcode,
                            CORINFO_RESOLVED_TOKEN* pResolvedToken,
                            CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, // Is this a "constrained." call on a
                                                                               // type parameter?
                            GenTreePtr         newobjThis,
                            int                prefixFlags,
                            CORINFO_CALL_INFO* callInfo,
                            IL_OFFSET          rawILOffset);

    bool impMethodInfo_hasRetBuffArg(CORINFO_METHOD_INFO* methInfo);

    GenTreePtr impFixupCallStructReturn(GenTreePtr call, CORINFO_CLASS_HANDLE retClsHnd);

    GenTreePtr impInitCallLongReturn(GenTreePtr call);

    GenTreePtr impFixupStructReturnType(GenTreePtr op, CORINFO_CLASS_HANDLE retClsHnd);

#ifdef DEBUG
    var_types impImportJitTestLabelMark(int numArgs);
#endif // DEBUG

    GenTreePtr impInitClass(CORINFO_RESOLVED_TOKEN* pResolvedToken);

    GenTreePtr impImportStaticReadOnlyField(void* fldAddr, var_types lclTyp);

    GenTreePtr impImportStaticFieldAccess(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                          CORINFO_ACCESS_FLAGS    access,
                                          CORINFO_FIELD_INFO*     pFieldInfo,
                                          var_types               lclTyp);

    static void impBashVarAddrsToI(GenTreePtr tree1, GenTreePtr tree2 = nullptr);

    GenTreePtr impImplicitIorI4Cast(GenTreePtr tree, var_types dstTyp);

    GenTreePtr impImplicitR4orR8Cast(GenTreePtr tree, var_types dstTyp);

    void impImportLeave(BasicBlock* block);
    void impResetLeaveBlock(BasicBlock* block, unsigned jmpAddr);
    BOOL       impLocAllocOnStack();
    GenTreePtr impIntrinsic(CORINFO_CLASS_HANDLE  clsHnd,
                            CORINFO_METHOD_HANDLE method,
                            CORINFO_SIG_INFO*     sig,
                            int                   memberRef,
                            bool                  readonlyCall,
                            bool                  tailCall,
                            CorInfoIntrinsics*    pIntrinsicID);
    GenTreePtr impArrayAccessIntrinsic(CORINFO_CLASS_HANDLE clsHnd,
                                       CORINFO_SIG_INFO*    sig,
                                       int                  memberRef,
                                       bool                 readonlyCall,
                                       CorInfoIntrinsics    intrinsicID);
    GenTreePtr impInitializeArrayIntrinsic(CORINFO_SIG_INFO* sig);

    GenTreePtr impMethodPointer(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);

    GenTreePtr impTransformThis(GenTreePtr              thisPtr,
                                CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                                CORINFO_THIS_TRANSFORM  transform);

    //----------------- Manipulating the trees and stmts ----------------------

    GenTreePtr impTreeList; // Trees for the BB being imported
    GenTreePtr impTreeLast; // The last tree for the current BB

    enum
    {
        CHECK_SPILL_ALL  = -1,
        CHECK_SPILL_NONE = -2
    };

public:
    void impBeginTreeList();
    void impEndTreeList(BasicBlock* block, GenTreePtr firstStmt, GenTreePtr lastStmt);
    void impEndTreeList(BasicBlock* block);
    void impAppendStmtCheck(GenTreePtr stmt, unsigned chkLevel);
    void impAppendStmt(GenTreePtr stmt, unsigned chkLevel);
    void impInsertStmtBefore(GenTreePtr stmt, GenTreePtr stmtBefore);
    GenTreePtr impAppendTree(GenTreePtr tree, unsigned chkLevel, IL_OFFSETX offset);
    void impInsertTreeBefore(GenTreePtr tree, IL_OFFSETX offset, GenTreePtr stmtBefore);
    void impAssignTempGen(unsigned    tmp,
                          GenTreePtr  val,
                          unsigned    curLevel,
                          GenTreePtr* pAfterStmt = nullptr,
                          IL_OFFSETX  ilOffset   = BAD_IL_OFFSET,
                          BasicBlock* block      = nullptr);
    void impAssignTempGen(unsigned             tmpNum,
                          GenTreePtr           val,
                          CORINFO_CLASS_HANDLE structHnd,
                          unsigned             curLevel,
                          GenTreePtr*          pAfterStmt = nullptr,
                          IL_OFFSETX           ilOffset   = BAD_IL_OFFSET,
                          BasicBlock*          block      = nullptr);
    GenTreePtr impCloneExpr(GenTreePtr           tree,
                            GenTreePtr*          clone,
                            CORINFO_CLASS_HANDLE structHnd,
                            unsigned             curLevel,
                            GenTreePtr* pAfterStmt DEBUGARG(const char* reason));
    GenTreePtr impAssignStruct(GenTreePtr           dest,
                               GenTreePtr           src,
                               CORINFO_CLASS_HANDLE structHnd,
                               unsigned             curLevel,
                               GenTreePtr*          pAfterStmt = nullptr,
                               BasicBlock*          block      = nullptr);
    GenTreePtr impAssignStructPtr(GenTreePtr           dest,
                                  GenTreePtr           src,
                                  CORINFO_CLASS_HANDLE structHnd,
                                  unsigned             curLevel,
                                  GenTreePtr*          pAfterStmt = nullptr,
                                  BasicBlock*          block      = nullptr);

    GenTreePtr impGetStructAddr(GenTreePtr           structVal,
                                CORINFO_CLASS_HANDLE structHnd,
                                unsigned             curLevel,
                                bool                 willDeref);

    var_types impNormStructType(CORINFO_CLASS_HANDLE structHnd,
                                BYTE*                gcLayout     = nullptr,
                                unsigned*            numGCVars    = nullptr,
                                var_types*           simdBaseType = nullptr);

    GenTreePtr impNormStructVal(GenTreePtr           structVal,
                                CORINFO_CLASS_HANDLE structHnd,
                                unsigned             curLevel,
                                bool                 forceNormalization = false);

    GenTreePtr impTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                BOOL*                   pRuntimeLookup    = nullptr,
                                BOOL                    mustRestoreHandle = FALSE,
                                BOOL                    importParent      = FALSE);

    GenTreePtr impParentClassTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                           BOOL*                   pRuntimeLookup    = nullptr,
                                           BOOL                    mustRestoreHandle = FALSE)
    {
        return impTokenToHandle(pResolvedToken, pRuntimeLookup, mustRestoreHandle, TRUE);
    }

    GenTreePtr impLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                               CORINFO_LOOKUP*         pLookup,
                               unsigned                flags,
                               void*                   compileTimeHandle);

    GenTreePtr impRuntimeLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                      CORINFO_LOOKUP*         pLookup,
                                      void*                   compileTimeHandle);

    GenTreePtr impReadyToRunLookupToTree(CORINFO_CONST_LOOKUP* pLookup, unsigned flags, void* compileTimeHandle);

    GenTreePtr impReadyToRunHelperToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                         CorInfoHelpFunc         helper,
                                         var_types               type,
                                         GenTreeArgList*         arg                = nullptr,
                                         CORINFO_LOOKUP_KIND*    pGenericLookupKind = nullptr);

    GenTreePtr impCastClassOrIsInstToTree(GenTreePtr              op1,
                                          GenTreePtr              op2,
                                          CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                          bool                    isCastClass);

    bool VarTypeIsMultiByteAndCanEnreg(var_types            type,
                                       CORINFO_CLASS_HANDLE typeClass,
                                       unsigned*            typeSize,
                                       bool                 forReturn);

    static bool IsIntrinsicImplementedByUserCall(CorInfoIntrinsics intrinsicId);
    static bool IsTargetIntrinsic(CorInfoIntrinsics intrinsicId);
    static bool IsMathIntrinsic(CorInfoIntrinsics intrinsicId);
    static bool IsMathIntrinsic(GenTreePtr tree);

private:
    //----------------- Importing the method ----------------------------------

    CORINFO_CONTEXT_HANDLE impTokenLookupContextHandle; // The context used for looking up tokens.

#ifdef DEBUG
    unsigned    impCurOpcOffs;
    const char* impCurOpcName;
    bool        impNestedStackSpill;

    // For displaying instrs with generated native code (-n:B)
    GenTreePtr impLastILoffsStmt; // oldest stmt added for which we did not gtStmtLastILoffs
    void       impNoteLastILoffs();
#endif

    /* IL offset of the stmt currently being imported. It gets set to
       BAD_IL_OFFSET after it has been set in the appended trees. Then it gets
       updated at IL offsets for which we have to report mapping info.
       It also includes flag bits, so use jitGetILoffs()
       to get the actual IL offset value.
    */

    IL_OFFSETX impCurStmtOffs;
    void impCurStmtOffsSet(IL_OFFSET offs);

    void impNoteBranchOffs();

    unsigned impInitBlockLineInfo();

    GenTreePtr impCheckForNullPointer(GenTreePtr obj);
    bool impIsThis(GenTreePtr obj);
    bool impIsLDFTN_TOKEN(const BYTE* delegateCreateStart, const BYTE* newobjCodeAddr);
    bool impIsDUP_LDVIRTFTN_TOKEN(const BYTE* delegateCreateStart, const BYTE* newobjCodeAddr);
    bool impIsAnySTLOC(OPCODE opcode)
    {
        return ((opcode == CEE_STLOC) || (opcode == CEE_STLOC_S) ||
                ((opcode >= CEE_STLOC_0) && (opcode <= CEE_STLOC_3)));
    }

    GenTreeArgList* impPopList(unsigned          count,
                               unsigned*         flagsPtr,
                               CORINFO_SIG_INFO* sig,
                               GenTreeArgList*   prefixTree = nullptr);

    GenTreeArgList* impPopRevList(unsigned          count,
                                  unsigned*         flagsPtr,
                                  CORINFO_SIG_INFO* sig,
                                  unsigned          skipReverseCount = 0);

    /*
     * Get current IL offset with stack-empty info incoporated
     */
    IL_OFFSETX impCurILOffset(IL_OFFSET offs, bool callInstruction = false);

    //---------------- Spilling the importer stack ----------------------------

    struct PendingDsc
    {
        PendingDsc*   pdNext;
        BasicBlock*   pdBB;
        SavedStack    pdSavedStack;
        ThisInitState pdThisPtrInit;
    };

    PendingDsc* impPendingList; // list of BBs currently waiting to be imported.
    PendingDsc* impPendingFree; // Freed up dscs that can be reused

    // We keep a byte-per-block map (dynamically extended) in the top-level Compiler object of a compilation.
    ExpandArray<BYTE> impPendingBlockMembers;

    // Return the byte for "b" (allocating/extending impPendingBlockMembers if necessary.)
    // Operates on the map in the top-level ancestor.
    BYTE impGetPendingBlockMember(BasicBlock* blk)
    {
        return impInlineRoot()->impPendingBlockMembers.Get(blk->bbInd());
    }

    // Set the byte for "b" to "val" (allocating/extending impPendingBlockMembers if necessary.)
    // Operates on the map in the top-level ancestor.
    void impSetPendingBlockMember(BasicBlock* blk, BYTE val)
    {
        impInlineRoot()->impPendingBlockMembers.Set(blk->bbInd(), val);
    }

    bool impCanReimport;

    bool impSpillStackEntry(unsigned level,
                            unsigned varNum
#ifdef DEBUG
                            ,
                            bool        bAssertOnRecursion,
                            const char* reason
#endif
                            );

    void impSpillStackEnsure(bool spillLeaves = false);
    void impEvalSideEffects();
    void impSpillSpecialSideEff();
    void impSpillSideEffects(bool spillGlobEffects, unsigned chkLevel DEBUGARG(const char* reason));
    void               impSpillValueClasses();
    void               impSpillEvalStack();
    static fgWalkPreFn impFindValueClasses;
    void impSpillLclRefs(ssize_t lclNum);

    BasicBlock* impPushCatchArgOnStack(BasicBlock* hndBlk, CORINFO_CLASS_HANDLE clsHnd);

    void impImportBlockCode(BasicBlock* block);

    void impReimportMarkBlock(BasicBlock* block);
    void impReimportMarkSuccessors(BasicBlock* block);

    void impVerifyEHBlock(BasicBlock* block, bool isTryStart);

    void impImportBlockPending(BasicBlock* block);

    // Similar to impImportBlockPending, but assumes that block has already been imported once and is being
    // reimported for some reason.  It specifically does *not* look at verCurrentState to set the EntryState
    // for the block, but instead, just re-uses the block's existing EntryState.
    void impReimportBlockPending(BasicBlock* block);

    var_types impGetByRefResultType(genTreeOps oper, bool fUnsigned, GenTreePtr* pOp1, GenTreePtr* pOp2);

    void impImportBlock(BasicBlock* block);

    // Assumes that "block" is a basic block that completes with a non-empty stack. We will assign the values
    // on the stack to local variables (the "spill temp" variables). The successor blocks will assume that
    // its incoming stack contents are in those locals. This requires "block" and its successors to agree on
    // the variables that will be used -- and for all the predecessors of those successors, and the
    // successors of those predecessors, etc. Call such a set of blocks closed under alternating
    // successor/predecessor edges a "spill clique." A block is a "predecessor" or "successor" member of the
    // clique (or, conceivably, both). Each block has a specified sequence of incoming and outgoing spill
    // temps. If "block" already has its outgoing spill temps assigned (they are always a contiguous series
    // of local variable numbers, so we represent them with the base local variable number), returns that.
    // Otherwise, picks a set of spill temps, and propagates this choice to all blocks in the spill clique of
    // which "block" is a member (asserting, in debug mode, that no block in this clique had its spill temps
    // chosen already. More precisely, that the incoming or outgoing spill temps are not chosen, depending
    // on which kind of member of the clique the block is).
    unsigned impGetSpillTmpBase(BasicBlock* block);

    // Assumes that "block" is a basic block that completes with a non-empty stack. We have previously
    // assigned the values on the stack to local variables (the "spill temp" variables). The successor blocks
    // will assume that its incoming stack contents are in those locals. This requires "block" and its
    // successors to agree on the variables and their types that will be used.  The CLI spec allows implicit
    // conversions between 'int' and 'native int' or 'float' and 'double' stack types. So one predecessor can
    // push an int and another can push a native int.  For 64-bit we have chosen to implement this by typing
    // the "spill temp" as native int, and then importing (or re-importing as needed) so that all the
    // predecessors in the "spill clique" push a native int (sign-extending if needed), and all the
    // successors receive a native int. Similarly float and double are unified to double.
    // This routine is called after a type-mismatch is detected, and it will walk the spill clique to mark
    // blocks for re-importation as appropriate (both successors, so they get the right incoming type, and
    // predecessors, so they insert an upcast if needed).
    void impReimportSpillClique(BasicBlock* block);

    // When we compute a "spill clique" (see above) these byte-maps are allocated to have a byte per basic
    // block, and represent the predecessor and successor members of the clique currently being computed.
    // *** Access to these will need to be locked in a parallel compiler.
    ExpandArray<BYTE> impSpillCliquePredMembers;
    ExpandArray<BYTE> impSpillCliqueSuccMembers;

    enum SpillCliqueDir
    {
        SpillCliquePred,
        SpillCliqueSucc
    };

    // Abstract class for receiving a callback while walking a spill clique
    class SpillCliqueWalker
    {
    public:
        virtual void Visit(SpillCliqueDir predOrSucc, BasicBlock* blk) = 0;
    };

    // This class is used for setting the bbStkTempsIn and bbStkTempsOut on the blocks within a spill clique
    class SetSpillTempsBase : public SpillCliqueWalker
    {
        unsigned m_baseTmp;

    public:
        SetSpillTempsBase(unsigned baseTmp) : m_baseTmp(baseTmp)
        {
        }
        virtual void Visit(SpillCliqueDir predOrSucc, BasicBlock* blk);
    };

    // This class is used for implementing impReimportSpillClique part on each block within the spill clique
    class ReimportSpillClique : public SpillCliqueWalker
    {
        Compiler* m_pComp;

    public:
        ReimportSpillClique(Compiler* pComp) : m_pComp(pComp)
        {
        }
        virtual void Visit(SpillCliqueDir predOrSucc, BasicBlock* blk);
    };

    // This is the heart of the algorithm for walking spill cliques. It invokes callback->Visit for each
    // predecessor or successor within the spill clique
    void impWalkSpillCliqueFromPred(BasicBlock* pred, SpillCliqueWalker* callback);

    // For a BasicBlock that has already been imported, the EntryState has an array of GenTrees for the
    // incoming locals. This walks that list an resets the types of the GenTrees to match the types of
    // the VarDscs. They get out of sync when we have int/native int issues (see impReimportSpillClique).
    void impRetypeEntryStateTemps(BasicBlock* blk);

    BYTE impSpillCliqueGetMember(SpillCliqueDir predOrSucc, BasicBlock* blk);
    void impSpillCliqueSetMember(SpillCliqueDir predOrSucc, BasicBlock* blk, BYTE val);

    void impPushVar(GenTree* op, typeInfo tiRetVal);
    void impLoadVar(unsigned lclNum, IL_OFFSET offset, typeInfo tiRetVal);
    void impLoadVar(unsigned lclNum, IL_OFFSET offset)
    {
        impLoadVar(lclNum, offset, lvaTable[lclNum].lvVerTypeInfo);
    }
    void impLoadArg(unsigned ilArgNum, IL_OFFSET offset);
    void impLoadLoc(unsigned ilLclNum, IL_OFFSET offset);
    bool impReturnInstruction(BasicBlock* block, int prefixFlags, OPCODE& opcode);

#ifdef _TARGET_ARM_
    void impMarkLclDstNotPromotable(unsigned tmpNum, GenTreePtr op, CORINFO_CLASS_HANDLE hClass);
#endif

    // A free list of linked list nodes used to represent to-do stacks of basic blocks.
    struct BlockListNode
    {
        BasicBlock*    m_blk;
        BlockListNode* m_next;
        BlockListNode(BasicBlock* blk, BlockListNode* next = nullptr) : m_blk(blk), m_next(next)
        {
        }
        void* operator new(size_t sz, Compiler* comp);
    };
    BlockListNode* impBlockListNodeFreeList;

    BlockListNode* AllocBlockListNode();
    void FreeBlockListNode(BlockListNode* node);

    bool impIsValueType(typeInfo* pTypeInfo);
    var_types mangleVarArgsType(var_types type);

#if FEATURE_VARARG
    regNumber getCallArgIntRegister(regNumber floatReg);
    regNumber getCallArgFloatRegister(regNumber intReg);
#endif // FEATURE_VARARG

#if defined(DEBUG)
    static unsigned jitTotalMethodCompiled;
#endif

#ifdef DEBUG
    static LONG jitNestingLevel;
#endif // DEBUG

    bool seenConditionalJump;

    static BOOL impIsAddressInLocal(GenTreePtr tree, GenTreePtr* lclVarTreeOut);

    void impMakeDiscretionaryInlineObservations(InlineInfo* pInlineInfo, InlineResult* inlineResult);

    // STATIC inlining decision based on the IL code.
    void impCanInlineIL(CORINFO_METHOD_HANDLE fncHandle,
                        CORINFO_METHOD_INFO*  methInfo,
                        bool                  forceInline,
                        InlineResult*         inlineResult);

    void impCheckCanInline(GenTreePtr             call,
                           CORINFO_METHOD_HANDLE  fncHandle,
                           unsigned               methAttr,
                           CORINFO_CONTEXT_HANDLE exactContextHnd,
                           InlineCandidateInfo**  ppInlineCandidateInfo,
                           InlineResult*          inlineResult);

    void impInlineRecordArgInfo(InlineInfo*   pInlineInfo,
                                GenTreePtr    curArgVal,
                                unsigned      argNum,
                                InlineResult* inlineResult);

    void impInlineInitVars(InlineInfo* pInlineInfo);

    unsigned impInlineFetchLocal(unsigned lclNum DEBUGARG(const char* reason));

    GenTreePtr impInlineFetchArg(unsigned lclNum, InlArgInfo* inlArgInfo, InlLclVarInfo* lclTypeInfo);

    BOOL impInlineIsThis(GenTreePtr tree, InlArgInfo* inlArgInfo);

    BOOL impInlineIsGuaranteedThisDerefBeforeAnySideEffects(GenTreePtr  additionalTreesToBeEvaluatedBefore,
                                                            GenTreePtr  variableBeingDereferenced,
                                                            InlArgInfo* inlArgInfo);

    void impMarkInlineCandidate(GenTreePtr call, CORINFO_CONTEXT_HANDLE exactContextHnd, CORINFO_CALL_INFO* callInfo);

    bool impTailCallRetTypeCompatible(var_types            callerRetType,
                                      CORINFO_CLASS_HANDLE callerRetTypeClass,
                                      var_types            calleeRetType,
                                      CORINFO_CLASS_HANDLE calleeRetTypeClass);

    bool impIsTailCallILPattern(bool        tailPrefixed,
                                OPCODE      curOpcode,
                                const BYTE* codeAddrOfNextOpcode,
                                const BYTE* codeEnd,
                                bool        isRecursive,
                                bool*       IsCallPopRet = nullptr);

    bool impIsImplicitTailCallCandidate(
        OPCODE curOpcode, const BYTE* codeAddrOfNextOpcode, const BYTE* codeEnd, int prefixFlags, bool isRecursive);

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           FlowGraph                                       XX
    XX                                                                           XX
    XX   Info about the basic-blocks, their contents and the flow analysis       XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    BasicBlock* fgFirstBB;        // Beginning of the basic block list
    BasicBlock* fgLastBB;         // End of the basic block list
    BasicBlock* fgFirstColdBlock; // First block to be placed in the cold section
#if FEATURE_EH_FUNCLETS
    BasicBlock* fgFirstFuncletBB; // First block of outlined funclets (to allow block insertion before the funclets)
#endif
    BasicBlock* fgFirstBBScratch;   // Block inserted for initialization stuff. Is nullptr if no such block has been
                                    // created.
    BasicBlockList* fgReturnBlocks; // list of BBJ_RETURN blocks
    unsigned        fgEdgeCount;    // # of control flow edges between the BBs
    unsigned        fgBBcount;      // # of BBs in the method
#ifdef DEBUG
    unsigned fgBBcountAtCodegen; // # of BBs in the method at the start of codegen
#endif
    unsigned     fgBBNumMax;       // The max bbNum that has been assigned to basic blocks
    unsigned     fgDomBBcount;     // # of BBs for which we have dominator and reachability information
    BasicBlock** fgBBInvPostOrder; // The flow graph stored in an array sorted in topological order, needed to compute
                                   // dominance. Indexed by block number. Size: fgBBNumMax + 1.

    // After the dominance tree is computed, we cache a DFS preorder number and DFS postorder number to compute
    // dominance queries in O(1). fgDomTreePreOrder and fgDomTreePostOrder are arrays giving the block's preorder and
    // postorder number, respectively. The arrays are indexed by basic block number. (Note that blocks are numbered
    // starting from one. Thus, we always waste element zero. This makes debugging easier and makes the code less likely
    // to suffer from bugs stemming from forgetting to add or subtract one from the block number to form an array
    // index). The arrays are of size fgBBNumMax + 1.
    unsigned* fgDomTreePreOrder;
    unsigned* fgDomTreePostOrder;

    bool fgBBVarSetsInited;

    // Allocate array like T* a = new T[fgBBNumMax + 1];
    // Using helper so we don't keep forgetting +1.
    template <typename T>
    T* fgAllocateTypeForEachBlk(CompMemKind cmk = CMK_Unknown)
    {
        return (T*)compGetMem((fgBBNumMax + 1) * sizeof(T), cmk);
    }

    // BlockSets are relative to a specific set of BasicBlock numbers. If that changes
    // (if the blocks are renumbered), this changes. BlockSets from different epochs
    // cannot be meaningfully combined. Note that new blocks can be created with higher
    // block numbers without changing the basic block epoch. These blocks *cannot*
    // participate in a block set until the blocks are all renumbered, causing the epoch
    // to change. This is useful if continuing to use previous block sets is valuable.
    // If the epoch is zero, then it is uninitialized, and block sets can't be used.
    unsigned fgCurBBEpoch;

    unsigned GetCurBasicBlockEpoch()
    {
        return fgCurBBEpoch;
    }

    // The number of basic blocks in the current epoch. When the blocks are renumbered,
    // this is fgBBcount. As blocks are added, fgBBcount increases, fgCurBBEpochSize remains
    // the same, until a new BasicBlock epoch is created, such as when the blocks are all renumbered.
    unsigned fgCurBBEpochSize;

    // The number of "size_t" elements required to hold a bitset large enough for fgCurBBEpochSize
    // bits. This is precomputed to avoid doing math every time BasicBlockBitSetTraits::GetArrSize() is called.
    unsigned fgBBSetCountInSizeTUnits;

    void NewBasicBlockEpoch()
    {
        INDEBUG(unsigned oldEpochArrSize = fgBBSetCountInSizeTUnits);

        // We have a new epoch. Compute and cache the size needed for new BlockSets.
        fgCurBBEpoch++;
        fgCurBBEpochSize = fgBBNumMax + 1;
        fgBBSetCountInSizeTUnits =
            unsigned(roundUp(fgCurBBEpochSize, sizeof(size_t) * 8)) / unsigned(sizeof(size_t) * 8);

#ifdef DEBUG
        // All BlockSet objects are now invalid!
        fgReachabilitySetsValid = false; // the bbReach sets are now invalid!
        fgEnterBlksSetValid     = false; // the fgEnterBlks set is now invalid!

        if (verbose)
        {
            unsigned epochArrSize = BasicBlockBitSetTraits::GetArrSize(this, sizeof(size_t));
            printf("\nNew BlockSet epoch %d, # of blocks (including unused BB00): %u, bitset array size: %u (%s)",
                   fgCurBBEpoch, fgCurBBEpochSize, epochArrSize, (epochArrSize <= 1) ? "short" : "long");
            if ((fgCurBBEpoch != 1) && ((oldEpochArrSize <= 1) != (epochArrSize <= 1)))
            {
                // If we're not just establishing the first epoch, and the epoch array size has changed such that we're
                // going to change our bitset representation from short (just a size_t bitset) to long (a pointer to an
                // array of size_t bitsets), then print that out.
                printf("; NOTE: BlockSet size was previously %s!", (oldEpochArrSize <= 1) ? "short" : "long");
            }
            printf("\n");
        }
#endif // DEBUG
    }

    void EnsureBasicBlockEpoch()
    {
        if (fgCurBBEpochSize != fgBBNumMax + 1)
        {
            NewBasicBlockEpoch();
        }
    }

    BasicBlock* fgNewBasicBlock(BBjumpKinds jumpKind);
    void fgEnsureFirstBBisScratch();
    bool fgFirstBBisScratch();
    bool fgBBisScratch(BasicBlock* block);

    void fgExtendEHRegionBefore(BasicBlock* block);
    void fgExtendEHRegionAfter(BasicBlock* block);

    BasicBlock* fgNewBBbefore(BBjumpKinds jumpKind, BasicBlock* block, bool extendRegion);

    BasicBlock* fgNewBBafter(BBjumpKinds jumpKind, BasicBlock* block, bool extendRegion);

    BasicBlock* fgNewBBinRegion(BBjumpKinds jumpKind,
                                unsigned    tryIndex,
                                unsigned    hndIndex,
                                BasicBlock* nearBlk,
                                bool        putInFilter = false,
                                bool        runRarely   = false,
                                bool        insertAtEnd = false);

    BasicBlock* fgNewBBinRegion(BBjumpKinds jumpKind,
                                BasicBlock* srcBlk,
                                bool        runRarely   = false,
                                bool        insertAtEnd = false);

    BasicBlock* fgNewBBinRegion(BBjumpKinds jumpKind);

    BasicBlock* fgNewBBinRegionWorker(BBjumpKinds jumpKind,
                                      BasicBlock* afterBlk,
                                      unsigned    xcptnIndex,
                                      bool        putInTryRegion);

    void fgInsertBBbefore(BasicBlock* insertBeforeBlk, BasicBlock* newBlk);
    void fgInsertBBafter(BasicBlock* insertAfterBlk, BasicBlock* newBlk);
    void fgUnlinkBlock(BasicBlock* block);

#if OPT_BOOL_OPS // Used to detect multiple logical "not" assignments.
    bool fgMultipleNots;
#endif

    bool fgModified;         // True if the flow graph has been modified recently
    bool fgComputePredsDone; // Have we computed the bbPreds list
    bool fgCheapPredsValid;  // Is the bbCheapPreds list valid?
    bool fgDomsComputed;     // Have we computed the dominator sets?

    bool     fgHasSwitch;  // any BBJ_SWITCH jumps?
    bool     fgHasPostfix; // any postfix ++/-- found?
    unsigned fgIncrCount;  // number of increment nodes found

    BlockSet fgEnterBlks; // Set of blocks which have a special transfer of control; the "entry" blocks plus EH handler
                          // begin blocks.

#ifdef DEBUG
    bool fgReachabilitySetsValid; // Are the bbReach sets valid?
    bool fgEnterBlksSetValid;     // Is the fgEnterBlks set valid?
#endif                            // DEBUG

    bool fgRemoveRestOfBlock; // true if we know that we will throw
    bool fgStmtRemoved;       // true if we remove statements -> need new DFA

    // There are two modes for ordering of the trees.
    //  - In FGOrderTree, the dominant ordering is the tree order, and the nodes contained in
    //    each tree and sub-tree are contiguous, and can be traversed (in gtNext/gtPrev order)
    //    by traversing the tree according to the order of the operands.
    //  - In FGOrderLinear, the dominant ordering is the linear order.

    enum FlowGraphOrder
    {
        FGOrderTree,
        FGOrderLinear
    };
    FlowGraphOrder fgOrder;

    // The following are boolean flags that keep track of the state of internal data structures

    bool                 fgStmtListThreaded;
    bool                 fgCanRelocateEHRegions;   // true if we are allowed to relocate the EH regions
    bool                 fgEdgeWeightsComputed;    // true after we have called fgComputeEdgeWeights
    bool                 fgHaveValidEdgeWeights;   // true if we were successful in computing all of the edge weights
    bool                 fgSlopUsedInEdgeWeights;  // true if their was some slop used when computing the edge weights
    bool                 fgRangeUsedInEdgeWeights; // true if some of the edgeWeight are expressed in Min..Max form
    bool                 fgNeedsUpdateFlowGraph;   // true if we need to run fgUpdateFlowGraph
    BasicBlock::weight_t fgCalledWeight;           // count of the number of times this method was called
                                                   // This is derived from the profile data
                                                   // or is BB_UNITY_WEIGHT when we don't have profile data

#if FEATURE_EH_FUNCLETS
    bool fgFuncletsCreated; // true if the funclet creation phase has been run
#endif                      // FEATURE_EH_FUNCLETS

    bool fgGlobalMorph;  // indicates if we are during the global morphing phase
                         // since fgMorphTree can be called from several places
    bool fgExpandInline; // indicates that we are creating tree for the inliner

    bool     impBoxTempInUse; // the temp below is valid and available
    unsigned impBoxTemp;      // a temporary that is used for boxing

#ifdef DEBUG
    bool jitFallbackCompile; // Are we doing a fallback compile? That is, have we executed a NO_WAY assert,
                             //   and we are trying to compile again in a "safer", minopts mode?
#endif

#if defined(DEBUG)
    unsigned impInlinedCodeSize;
#endif

    //-------------------------------------------------------------------------

    void fgInit();

    void fgImport();

    void fgInline();

    GenTreePtr fgGetCritSectOfStaticMethod();

#if !defined(_TARGET_X86_)

    void fgAddSyncMethodEnterExit();

    GenTree* fgCreateMonitorTree(unsigned lvaMonitorBool, unsigned lvaThisVar, BasicBlock* block, bool enter);

    void fgConvertSyncReturnToLeave(BasicBlock* block);

#endif // !_TARGET_X86_

    void fgAddReversePInvokeEnterExit();

    bool fgMoreThanOneReturnBlock();

    // The number of separate return points in the method.
    unsigned fgReturnCount;

    void fgAddInternal();

    bool fgFoldConditional(BasicBlock* block);

    void fgMorphStmts(BasicBlock* block, bool* mult, bool* lnot, bool* loadw);
    void fgMorphBlocks();

    bool fgMorphBlockStmt(BasicBlock* block, GenTreePtr stmt DEBUGARG(const char* msg));

    void fgSetOptions();

#ifdef DEBUG
    static fgWalkPreFn fgAssertNoQmark;
    void fgPreExpandQmarkChecks(GenTreePtr expr);
    void        fgPostExpandQmarkChecks();
    static void fgCheckQmarkAllowedForm(GenTreePtr tree);
#endif

    IL_OFFSET fgFindBlockILOffset(BasicBlock* block);

    BasicBlock* fgSplitBlockAtBeginning(BasicBlock* curr);
    BasicBlock* fgSplitBlockAtEnd(BasicBlock* curr);
    BasicBlock* fgSplitBlockAfterStatement(BasicBlock* curr, GenTree* stmt);
    BasicBlock* fgSplitBlockAfterNode(BasicBlock* curr, GenTree* node); // for LIR
    BasicBlock* fgSplitEdge(BasicBlock* curr, BasicBlock* succ);

    GenTreeStmt* fgNewStmtFromTree(GenTreePtr tree, BasicBlock* block, IL_OFFSETX offs);
    GenTreeStmt* fgNewStmtFromTree(GenTreePtr tree);
    GenTreeStmt* fgNewStmtFromTree(GenTreePtr tree, BasicBlock* block);
    GenTreeStmt* fgNewStmtFromTree(GenTreePtr tree, IL_OFFSETX offs);

    GenTreePtr fgGetTopLevelQmark(GenTreePtr expr, GenTreePtr* ppDst = nullptr);
    void fgExpandQmarkForCastInstOf(BasicBlock* block, GenTreePtr stmt);
    void fgExpandQmarkStmt(BasicBlock* block, GenTreePtr expr);
    void fgExpandQmarkNodes();

    void fgMorph();

    // Do "simple lowering."  This functionality is (conceptually) part of "general"
    // lowering that is distributed between fgMorph and the lowering phase of LSRA.
    void fgSimpleLowering();

    bool fgShouldCreateAssignOp(GenTreePtr tree, bool* bReverse);

    GenTreePtr fgInitThisClass();

    GenTreePtr fgGetStaticsCCtorHelper(CORINFO_CLASS_HANDLE cls, CorInfoHelpFunc helper);

    GenTreePtr fgGetSharedCCtor(CORINFO_CLASS_HANDLE cls);

    void fgLocalVarLiveness();

    void fgLocalVarLivenessInit();

#ifdef LEGACY_BACKEND
    GenTreePtr fgLegacyPerStatementLocalVarLiveness(GenTreePtr startNode, GenTreePtr relopNode, GenTreePtr asgdLclVar);
#else
    void fgPerNodeLocalVarLiveness(GenTree* node, GenTree* asgdLclVar);
    void fgPerStatementLocalVarLiveness(GenTree* node, GenTree* asgdLclVar);
#endif
    void fgPerBlockLocalVarLiveness();

    VARSET_VALRET_TP fgGetHandlerLiveVars(BasicBlock* block);

    void fgLiveVarAnalysis(bool updateInternalOnly = false);

    // This is used in the liveness computation, as a temporary.  When we use the
    // arbitrary-length VarSet representation, it is better not to allocate a new one
    // at each call.
    VARSET_TP fgMarkIntfUnionVS;

    bool fgMarkIntf(VARSET_VALARG_TP varSet);

    bool fgMarkIntf(VARSET_VALARG_TP varSet1, VARSET_VALARG_TP varSet2);

    void fgUpdateRefCntForClone(BasicBlock* addedToBlock, GenTreePtr clonedTree);

    void fgUpdateRefCntForExtract(GenTreePtr wholeTree, GenTreePtr keptTree);

    void fgComputeLifeCall(VARSET_TP& life, GenTreeCall* call);

    bool fgComputeLifeLocal(VARSET_TP& life, VARSET_TP& keepAliveVars, GenTree* lclVarNode, GenTree* node);

    VARSET_VALRET_TP fgComputeLife(VARSET_VALARG_TP life,
                                   GenTreePtr       startNode,
                                   GenTreePtr       endNode,
                                   VARSET_VALARG_TP volatileVars,
                                   bool* pStmtInfoDirty DEBUGARG(bool* treeModf));

    VARSET_VALRET_TP fgComputeLifeLIR(VARSET_VALARG_TP life, BasicBlock* block, VARSET_VALARG_TP volatileVars);

    bool fgRemoveDeadStore(GenTree**  pTree,
                           LclVarDsc* varDsc,
                           VARSET_TP  life,
                           bool*      doAgain,
                           bool* pStmtInfoDirty DEBUGARG(bool* treeModf));

    bool fgTryRemoveDeadLIRStore(LIR::Range& blockRange, GenTree* node, GenTree** next);

    // For updating liveset during traversal AFTER fgComputeLife has completed
    VARSET_VALRET_TP fgGetVarBits(GenTreePtr tree);
    VARSET_VALRET_TP fgUpdateLiveSet(VARSET_VALARG_TP liveSet, GenTreePtr tree);

    // Returns the set of live variables after endTree,
    // assuming that liveSet is the set of live variables BEFORE tree.
    // Requires that fgComputeLife has completed, and that tree is in the same
    // statement as endTree, and that it comes before endTree in execution order

    VARSET_VALRET_TP fgUpdateLiveSet(VARSET_VALARG_TP liveSet, GenTreePtr tree, GenTreePtr endTree)
    {
        VARSET_TP VARSET_INIT(this, newLiveSet, liveSet);
        while (tree != nullptr && tree != endTree->gtNext)
        {
            VarSetOps::AssignNoCopy(this, newLiveSet, fgUpdateLiveSet(newLiveSet, tree));
            tree = tree->gtNext;
        }
        assert(tree == endTree->gtNext);
        return newLiveSet;
    }

    void fgInterBlockLocalVarLiveness();

    // The presence of "x op= y" operations presents some difficulties for SSA: this is both a use of some SSA name of
    // "x", and a def of a new SSA name for "x".  The tree only has one local variable for "x", so it has to choose
    // whether to treat that as the use or def.  It chooses the "use", and thus the old SSA name.  This map allows us
    // to record/recover the "def" SSA number, given the lcl var node for "x" in such a tree.
    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, unsigned, JitSimplerHashBehavior> NodeToUnsignedMap;
    NodeToUnsignedMap* m_opAsgnVarDefSsaNums;
    NodeToUnsignedMap* GetOpAsgnVarDefSsaNums()
    {
        if (m_opAsgnVarDefSsaNums == nullptr)
        {
            m_opAsgnVarDefSsaNums = new (getAllocator()) NodeToUnsignedMap(getAllocator());
        }
        return m_opAsgnVarDefSsaNums;
    }

    // Requires value numbering phase to have completed. Returns the value number ("gtVN") of the
    // "tree," EXCEPT in the case of GTF_VAR_USEASG, because the tree node's gtVN member is the
    // "use" VN. Performs a lookup into the map of (use asg tree -> def VN.) to return the "def's"
    // VN.
    inline ValueNum GetUseAsgDefVNOrTreeVN(GenTreePtr tree);

    // Requires that "lcl" has the GTF_VAR_DEF flag set.  Returns the SSA number of "lcl".
    // Except: assumes that lcl is a def, and if it is
    // a def appearing in "lcl op= rhs" (GTF_VAR_USEASG), looks up and returns the SSA number for the "def",
    // rather than the "use" SSA number recorded in the tree "lcl".
    inline unsigned GetSsaNumForLocalVarDef(GenTreePtr lcl);

    // Some assignments assign to a local "indirectly": they are part of a comma expression that takes the address
    // of the local (or a field thereof), assigns this address to a temp, and uses an indirection of this temp as
    // the LHS of the assignment.  This actually arises in exactly one situation.  At the source level we assign one
    // struct local to another: "s1 = s2".  This becomes a copyblk.  If "s2" is promoted into  field variables "s2f0",
    // ..."s2fn", then the copyblk will morph to a comma expression that takes the address of "s1" and does field-wise
    // assignments:
    //   (byref addrS1 = &s1,
    //    *(addrS1 * offsetof(f0)) = s2f0,
    //    ...
    //    *(addrS1 * offsetof(fn)) = s2fn)
    //
    // It would be a shame, given the simple form at the source level, to be unable to track the values in the
    // fields of "s1" after this.  But "s1" does not appear in the assignments that modify it.  How, then, to
    // give it SSA names and value numbers?
    //
    // The solution is to use the side table described below to annotate each of the field-wise assignments at the
    // end with an instance of the structure below, whose fields are described in the declaration.
    struct IndirectAssignmentAnnotation
    {
        unsigned      m_lclNum;   // The local num that is being indirectly assigned.
        FieldSeqNode* m_fieldSeq; // If the LHS of the struct assignment is itself a struct field dereference,
                                  // as in "s0.g = s2", then "m_lclNum" would be "s0", and "m_fieldSeq" would
                                  // be the singleton field sequence "g".  The individual assignments would
                                  // further append the fields of "s.g" to that.
        bool m_isEntire;          // True iff this assignment writes all of m_lclNum.  (This can occur if the
                                  // structure has a single field).
        unsigned m_defSsaNum;     // The new SSA number of "m_lclNum" after the assignment.
        unsigned m_useSsaNum;     // Only valid if "m_isEntire" is false; if so, the SSA number of "m_lclNum" before the
                                  // assignment.

        IndirectAssignmentAnnotation(unsigned      lclNum,
                                     FieldSeqNode* fldSeq,
                                     bool          isEntire,
                                     unsigned      defSsaNum = SsaConfig::RESERVED_SSA_NUM,
                                     unsigned      useSsaNum = SsaConfig::RESERVED_SSA_NUM)
            : m_lclNum(lclNum), m_fieldSeq(fldSeq), m_isEntire(isEntire), m_defSsaNum(defSsaNum), m_useSsaNum(useSsaNum)
        {
        }
    };
    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, IndirectAssignmentAnnotation*, JitSimplerHashBehavior>
                          NodeToIndirAssignMap;
    NodeToIndirAssignMap* m_indirAssignMap;
    NodeToIndirAssignMap* GetIndirAssignMap()
    {
        if (m_indirAssignMap == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_IndirAssignMap, and use that for allocation.
            IAllocator* ialloc = new (this, CMK_IndirAssignMap) CompAllocator(this, CMK_IndirAssignMap);
            m_indirAssignMap   = new (ialloc) NodeToIndirAssignMap(ialloc);
        }
        return m_indirAssignMap;
    }

    // Performs SSA conversion.
    void fgSsaBuild();

    // Reset any data structures to the state expected by "fgSsaBuild", so it can be run again.
    void fgResetForSsa();

    unsigned fgSsaPassesCompleted; // Number of times fgSsaBuild has been run.

    // Returns "true" iff lcl "lclNum" should be excluded from SSA.
    inline bool fgExcludeFromSsa(unsigned lclNum);

    // The value numbers for this compilation.
    ValueNumStore* vnStore;

public:
    ValueNumStore* GetValueNumStore()
    {
        return vnStore;
    }

    // Do value numbering (assign a value number to each
    // tree node).
    void fgValueNumber();

    // Updates "fgCurHeap" via the assignment H[elemTypeEq][arrVN][inx][fldSeq] = rhsVN.
    // Assumes that "elemTypeEq" is the (equivalence class rep) of the array element type.
    // The 'indType' is the indirection type of the lhs of the assignment and will typically
    // match the element type of the array or fldSeq.  When this type doesn't match
    // or if the fldSeq is 'NotAField' we invalidate the array contents H[elemTypeEq][arrVN]
    //
    void fgValueNumberArrIndexAssign(CORINFO_CLASS_HANDLE elemTypeEq,
                                     ValueNum             arrVN,
                                     ValueNum             inxVN,
                                     FieldSeqNode*        fldSeq,
                                     ValueNum             rhsVN,
                                     var_types            indType);

    // Requires that "tree" is a GT_IND marked as an array index, and that its address argument
    // has been parsed to yield the other input arguments.  If evaluation of the address
    // can raise exceptions, those should be captured in the exception set "excVN."
    // Assumes that "elemTypeEq" is the (equivalence class rep) of the array element type.
    // Marks "tree" with the VN for H[elemTypeEq][arrVN][inx][fldSeq] (for the liberal VN; a new unique
    // VN for the conservative VN.)  Also marks the tree's argument as the address of an array element.
    // The type tree->TypeGet() will typically match the element type of the array or fldSeq.
    // When this type doesn't match or if the fldSeq is 'NotAField' we return a new unique VN
    //
    ValueNum fgValueNumberArrIndexVal(GenTreePtr           tree,
                                      CORINFO_CLASS_HANDLE elemTypeEq,
                                      ValueNum             arrVN,
                                      ValueNum             inxVN,
                                      ValueNum             excVN,
                                      FieldSeqNode*        fldSeq);

    // Requires "funcApp" to be a VNF_PtrToArrElem, and "addrXvn" to represent the exception set thrown
    // by evaluating the array index expression "tree".  Returns the value number resulting from
    // dereferencing the array in the current heap state.  If "tree" is non-null, it must be the
    // "GT_IND" that does the dereference, and it is given the returned value number.
    ValueNum fgValueNumberArrIndexVal(GenTreePtr tree, struct VNFuncApp* funcApp, ValueNum addrXvn);

    unsigned fgVNPassesCompleted; // Number of times fgValueNumber has been run.

    // Utility functions for fgValueNumber.

    // Perform value-numbering for the trees in "blk".  When giving VN's to the SSA
    // names defined by phi definitions at the start of "blk", "newVNsForPhis" indicates
    // that these should be given new VN's, irrespective of the values of the LHS.
    // If "false", then we may assume that all inputs to phi RHS's of such definitions
    // have already been assigned value numbers; if they are all assigned the *same* value
    // number, then the LHS SSA name gets the same VN.
    void fgValueNumberBlock(BasicBlock* blk, bool newVNsForPhis);

    // Requires that "entryBlock" is the entry block of loop "loopNum", and that "loopNum" is the
    // innermost loop of which "entryBlock" is the entry.  Returns the value number that should be
    // assumed for the heap at the start "entryBlk".
    ValueNum fgHeapVNForLoopSideEffects(BasicBlock* entryBlock, unsigned loopNum);

    // Called when an operation (performed by "tree", described by "msg") may cause the global Heap to be mutated.
    void fgMutateHeap(GenTreePtr tree DEBUGARG(const char* msg));

    // Tree caused an update in the current heap VN.  If "tree" has an associated heap SSA #, record that
    // value in that SSA #.
    void fgValueNumberRecordHeapSsa(GenTreePtr tree);

    // The input 'tree' is a leaf node that is a constant
    // Assign the proper value number to the tree
    void fgValueNumberTreeConst(GenTreePtr tree);

    // Assumes that all inputs to "tree" have had value numbers assigned; assigns a VN to tree.
    // (With some exceptions: the VN of the lhs of an assignment is assigned as part of the
    // assignment.)
    // If "evalAsgLhsInd" is true, evaluate a GT_IND node, even if it's labeled as the LHS of
    // an assignment.
    void fgValueNumberTree(GenTreePtr tree, bool evalAsgLhsInd = false);

    // Does value-numbering for a block assignment.
    void fgValueNumberBlockAssignment(GenTreePtr tree, bool evalAsgLhsInd);

    // Does value-numbering for a cast tree.
    void fgValueNumberCastTree(GenTreePtr tree);

    // Does value-numbering for an intrinsic tree.
    void fgValueNumberIntrinsic(GenTreePtr tree);

    // Does value-numbering for a call.  We interpret some helper calls.
    void fgValueNumberCall(GenTreeCall* call);

    // The VN of some nodes in "args" may have changed -- reassign VNs to the arg list nodes.
    void fgUpdateArgListVNs(GenTreeArgList* args);

    // Does value-numbering for a helper "call" that has a VN function symbol "vnf".
    void fgValueNumberHelperCallFunc(GenTreeCall* call, VNFunc vnf, ValueNumPair vnpExc);

    // Requires "helpCall" to be a helper call.  Assigns it a value number;
    // we understand the semantics of some of the calls.  Returns "true" if
    // the call may modify the heap (we assume arbitrary memory side effects if so).
    bool fgValueNumberHelperCall(GenTreeCall* helpCall);

    // Requires "helpFunc" to be pure.  Returns the corresponding VNFunc.
    VNFunc fgValueNumberHelperMethVNFunc(CorInfoHelpFunc helpFunc);

    // This is the current value number for the "Heap" implicit variable while
    // doing value numbering.  This is the value number under the "liberal" interpretation
    // of heap values; the "conservative" interpretation needs no VN, since every access of
    // the heap yields an unknown value.
    ValueNum fgCurHeapVN;

    // Return a "pseudo"-class handle for an array element type.  If "elemType" is TYP_STRUCT,
    // requires "elemStructType" to be non-null (and to have a low-order zero).  Otherwise, low order bit
    // is 1, and the rest is an encoding of "elemTyp".
    static CORINFO_CLASS_HANDLE EncodeElemType(var_types elemTyp, CORINFO_CLASS_HANDLE elemStructType)
    {
        if (elemStructType != nullptr)
        {
            assert(varTypeIsStruct(elemTyp) || elemTyp == TYP_REF || elemTyp == TYP_BYREF ||
                   varTypeIsIntegral(elemTyp));
            assert((size_t(elemStructType) & 0x1) == 0x0); // Make sure the encoding below is valid.
            return elemStructType;
        }
        else
        {
            elemTyp = varTypeUnsignedToSigned(elemTyp);
            return CORINFO_CLASS_HANDLE(size_t(elemTyp) << 1 | 0x1);
        }
    }
    // If "clsHnd" is the result of an "EncodePrim" call, returns true and sets "*pPrimType" to the
    // var_types it represents.  Otherwise, returns TYP_STRUCT (on the assumption that "clsHnd" is
    // the struct type of the element).
    static var_types DecodeElemType(CORINFO_CLASS_HANDLE clsHnd)
    {
        size_t clsHndVal = size_t(clsHnd);
        if (clsHndVal & 0x1)
        {
            return var_types(clsHndVal >> 1);
        }
        else
        {
            return TYP_STRUCT;
        }
    }

    // Convert a BYTE which represents the VM's CorInfoGCtype to the JIT's var_types
    var_types getJitGCType(BYTE gcType);

    enum structPassingKind
    {
        SPK_Unknown,       // Invalid value, never returned
        SPK_PrimitiveType, // The struct is passed/returned using a primitive type.
        SPK_ByValue,       // The struct is passed/returned by value (using the ABI rules)
                           //  for ARM64 and UNIX_X64 in multiple registers. (when all of the
                           //   parameters registers are used, then the stack will be used)
                           //  for X86 passed on the stack, for ARM32 passed in registers
                           //   or the stack or split between registers and the stack.
        SPK_ByValueAsHfa,  // The struct is passed/returned as an HFA in multiple registers.
        SPK_ByReference
    }; // The struct is passed/returned by reference to a copy/buffer.

    // Get the "primitive" type that is is used when we are given a struct of size 'structSize'.
    // For pointer sized structs the 'clsHnd' is used to determine if the struct contains GC ref.
    // A "primitive" type is one of the scalar types: byte, short, int, long, ref, float, double
    // If we can't or shouldn't use a "primitive" type then TYP_UNKNOWN is returned.
    //
    var_types getPrimitiveTypeForStruct(unsigned structSize, CORINFO_CLASS_HANDLE clsHnd);

    // Get the type that is used to pass values of the given struct type.
    // If you have already retrieved the struct size then pass it as the optional third argument
    //
    var_types getArgTypeForStruct(CORINFO_CLASS_HANDLE clsHnd,
                                  structPassingKind*   wbPassStruct,
                                  unsigned             structSize = 0);

    // Get the type that is used to return values of the given struct type.
    // If you have already retrieved the struct size then pass it as the optional third argument
    //
    var_types getReturnTypeForStruct(CORINFO_CLASS_HANDLE clsHnd,
                                     structPassingKind*   wbPassStruct,
                                     unsigned             structSize = 0);

#ifdef DEBUG
    // Print a representation of "vnp" or "vn" on standard output.
    // If "level" is non-zero, we also print out a partial expansion of the value.
    void vnpPrint(ValueNumPair vnp, unsigned level);
    void vnPrint(ValueNum vn, unsigned level);
#endif

    // Dominator computation member functions
    // Not exposed outside Compiler
protected:
    bool fgDominate(BasicBlock* b1, BasicBlock* b2); // Return true if b1 dominates b2

    bool fgReachable(BasicBlock* b1, BasicBlock* b2); // Returns true if block b1 can reach block b2

    void fgComputeDoms(); // Computes the immediate dominators for each basic block in the
                          // flow graph.  We first assume the fields bbIDom on each
                          // basic block are invalid. This computation is needed later
                          // by fgBuildDomTree to build the dominance tree structure.
                          // Based on: A Simple, Fast Dominance Algorithm
                          // by Keith D. Cooper, Timothy J. Harvey, and Ken Kennedy

    BlockSet_ValRet_T fgGetDominatorSet(BasicBlock* block); // Returns a set of blocks that dominate the given block.
    // Note: this is relatively slow compared to calling fgDominate(),
    // especially if dealing with a single block versus block check.

    void fgComputeReachabilitySets(); // Compute bbReach sets. (Also sets BBF_GC_SAFE_POINT flag on blocks.)

    void fgComputeEnterBlocksSet(); // Compute the set of entry blocks, 'fgEnterBlks'.

    bool fgRemoveUnreachableBlocks(); // Remove blocks determined to be unreachable by the bbReach sets.

    void fgComputeReachability(); // Perform flow graph node reachability analysis.

    BasicBlock* fgIntersectDom(BasicBlock* a, BasicBlock* b); // Intersect two immediate dominator sets.

    void fgDfsInvPostOrder(); // In order to compute dominance using fgIntersectDom, the flow graph nodes must be
                              // processed in topological sort, this function takes care of that.

    void fgDfsInvPostOrderHelper(BasicBlock* block, BlockSet& visited, unsigned* count);

    BlockSet_ValRet_T fgDomFindStartNodes(); // Computes which basic blocks don't have incoming edges in the flow graph.
                                             // Returns this as a set.

    BlockSet_ValRet_T fgDomTreeEntryNodes(BasicBlockList** domTree); // Computes which nodes in the dominance forest are
                                                                     // root nodes. Returns this as a set.

#ifdef DEBUG
    void fgDispDomTree(BasicBlockList** domTree); // Helper that prints out the Dominator Tree in debug builds.
#endif                                            // DEBUG

    void fgBuildDomTree(); // Once we compute all the immediate dominator sets for each node in the flow graph
                           // (performed by fgComputeDoms), this procedure builds the dominance tree represented
                           // adjacency lists.

    // In order to speed up the queries of the form 'Does A dominates B', we can perform a DFS preorder and postorder
    // traversal of the dominance tree and the dominance query will become A dominates B iif preOrder(A) <= preOrder(B)
    // && postOrder(A) >= postOrder(B) making the computation O(1).
    void fgTraverseDomTree(unsigned bbNum, BasicBlockList** domTree, unsigned* preNum, unsigned* postNum);

    // When the flow graph changes, we need to update the block numbers, predecessor lists, reachability sets, and
    // dominators.
    void fgUpdateChangedFlowGraph();

public:
    // Compute the predecessors of the blocks in the control flow graph.
    void fgComputePreds();

    // Remove all predecessor information.
    void fgRemovePreds();

    // Compute the cheap flow graph predecessors lists. This is used in some early phases
    // before the full predecessors lists are computed.
    void fgComputeCheapPreds();

private:
    void fgAddCheapPred(BasicBlock* block, BasicBlock* blockPred);

    void fgRemoveCheapPred(BasicBlock* block, BasicBlock* blockPred);

public:
    enum GCPollType
    {
        GCPOLL_NONE,
        GCPOLL_CALL,
        GCPOLL_INLINE
    };

    // Initialize the per-block variable sets (used for liveness analysis).
    void fgInitBlockVarSets();

    // true if we've gone through and created GC Poll calls.
    bool fgGCPollsCreated;
    void fgMarkGCPollBlocks();
    void fgCreateGCPolls();
    bool fgCreateGCPoll(GCPollType pollType, BasicBlock* block);

    // Requires that "block" is a block that returns from
    // a finally.  Returns the number of successors (jump targets of
    // of blocks in the covered "try" that did a "LEAVE".)
    unsigned fgNSuccsOfFinallyRet(BasicBlock* block);

    // Requires that "block" is a block that returns (in the sense of BBJ_EHFINALLYRET) from
    // a finally.  Returns its "i"th successor (jump targets of
    // of blocks in the covered "try" that did a "LEAVE".)
    // Requires that "i" < fgNSuccsOfFinallyRet(block).
    BasicBlock* fgSuccOfFinallyRet(BasicBlock* block, unsigned i);

private:
    // Factor out common portions of the impls of the methods above.
    void fgSuccOfFinallyRetWork(BasicBlock* block, unsigned i, BasicBlock** bres, unsigned* nres);

public:
    // For many purposes, it is desirable to be able to enumerate the *distinct* targets of a switch statement,
    // skipping duplicate targets.  (E.g., in flow analyses that are only interested in the set of possible targets.)
    // SwitchUniqueSuccSet contains the non-duplicated switch targets.
    // (Code that modifies the jump table of a switch has an obligation to call Compiler::UpdateSwitchTableTarget,
    // which in turn will call the "UpdateTarget" method of this type if a SwitchUniqueSuccSet has already
    // been computed for the switch block.  If a switch block is deleted or is transformed into a non-switch,
    // we leave the entry associated with the block, but it will no longer be accessed.)
    struct SwitchUniqueSuccSet
    {
        unsigned     numDistinctSuccs; // Number of distinct targets of the switch.
        BasicBlock** nonDuplicates;    // Array of "numDistinctSuccs", containing all the distinct switch target
                                       // successors.

        // The switch block "switchBlk" just had an entry with value "from" modified to the value "to".
        // Update "this" as necessary: if "from" is no longer an element of the jump table of "switchBlk",
        // remove it from "this", and ensure that "to" is a member.  Use "alloc" to do any required allocation.
        void UpdateTarget(IAllocator* alloc, BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to);
    };

    typedef SimplerHashTable<BasicBlock*, PtrKeyFuncs<BasicBlock>, SwitchUniqueSuccSet, JitSimplerHashBehavior>
        BlockToSwitchDescMap;

private:
    // Maps BasicBlock*'s that end in switch statements to SwitchUniqueSuccSets that allow
    // iteration over only the distinct successors.
    BlockToSwitchDescMap* m_switchDescMap;

public:
    BlockToSwitchDescMap* GetSwitchDescMap()
    {
        if (m_switchDescMap == nullptr)
        {
            m_switchDescMap = new (getAllocator()) BlockToSwitchDescMap(getAllocator());
        }
        return m_switchDescMap;
    }

    // Invalidate the map of unique switch block successors. For example, since the hash key of the map
    // depends on block numbers, we must invalidate the map when the blocks are renumbered, to ensure that
    // we don't accidentally look up and return the wrong switch data.
    void InvalidateUniqueSwitchSuccMap()
    {
        m_switchDescMap = nullptr;
    }

    // Requires "switchBlock" to be a block that ends in a switch.  Returns
    // the corresponding SwitchUniqueSuccSet.
    SwitchUniqueSuccSet GetDescriptorForSwitch(BasicBlock* switchBlk);

    // The switch block "switchBlk" just had an entry with value "from" modified to the value "to".
    // Update "this" as necessary: if "from" is no longer an element of the jump table of "switchBlk",
    // remove it from "this", and ensure that "to" is a member.
    void UpdateSwitchTableTarget(BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to);

    // Remove the "SwitchUniqueSuccSet" of "switchBlk" in the BlockToSwitchDescMap.
    void fgInvalidateSwitchDescMapEntry(BasicBlock* switchBlk);

    BasicBlock* fgFirstBlockOfHandler(BasicBlock* block);

    flowList* fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred);

    flowList* fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred, flowList*** ptrToPred);

    flowList* fgSpliceOutPred(BasicBlock* block, BasicBlock* blockPred);

    flowList* fgRemoveRefPred(BasicBlock* block, BasicBlock* blockPred);

    flowList* fgRemoveAllRefPreds(BasicBlock* block, BasicBlock* blockPred);

    flowList* fgRemoveAllRefPreds(BasicBlock* block, flowList** ptrToPred);

    void fgRemoveBlockAsPred(BasicBlock* block);

    void fgChangeSwitchBlock(BasicBlock* oldSwitchBlock, BasicBlock* newSwitchBlock);

    void fgReplaceSwitchJumpTarget(BasicBlock* blockSwitch, BasicBlock* newTarget, BasicBlock* oldTarget);

    void fgReplaceJumpTarget(BasicBlock* block, BasicBlock* newTarget, BasicBlock* oldTarget);

    void fgReplacePred(BasicBlock* block, BasicBlock* oldPred, BasicBlock* newPred);

    flowList* fgAddRefPred(BasicBlock* block,
                           BasicBlock* blockPred,
                           flowList*   oldEdge           = nullptr,
                           bool        initializingPreds = false); // Only set to 'true' when we are computing preds in
                                                                   // fgComputePreds()

    void fgFindBasicBlocks();

    bool fgIsBetterFallThrough(BasicBlock* bCur, BasicBlock* bAlt);

    bool fgCheckEHCanInsertAfterBlock(BasicBlock* blk, unsigned regionIndex, bool putInTryRegion);

    BasicBlock* fgFindInsertPoint(unsigned    regionIndex,
                                  bool        putInTryRegion,
                                  BasicBlock* startBlk,
                                  BasicBlock* endBlk,
                                  BasicBlock* nearBlk,
                                  BasicBlock* jumpBlk,
                                  bool        runRarely);

    unsigned fgGetNestingLevel(BasicBlock* block, unsigned* pFinallyNesting = nullptr);

    void fgRemoveEmptyBlocks();

    void fgRemoveStmt(BasicBlock* block, GenTreePtr stmt, bool updateRefCnt = true);

    bool fgCheckRemoveStmt(BasicBlock* block, GenTreePtr stmt);

    void fgCreateLoopPreHeader(unsigned lnum);

    void fgUnreachableBlock(BasicBlock* block);

    void fgRemoveJTrue(BasicBlock* block);

    BasicBlock* fgLastBBInMainFunction();

    BasicBlock* fgEndBBAfterMainFunction();

    void fgUnlinkRange(BasicBlock* bBeg, BasicBlock* bEnd);

    void fgRemoveBlock(BasicBlock* block, bool unreachable);

    bool fgCanCompactBlocks(BasicBlock* block, BasicBlock* bNext);

    void fgCompactBlocks(BasicBlock* block, BasicBlock* bNext);

    void fgUpdateLoopsAfterCompacting(BasicBlock* block, BasicBlock* bNext);

    BasicBlock* fgConnectFallThrough(BasicBlock* bSrc, BasicBlock* bDst);

    bool fgRenumberBlocks();

    bool fgExpandRarelyRunBlocks();

    bool fgEhAllowsMoveBlock(BasicBlock* bBefore, BasicBlock* bAfter);

    void fgMoveBlocksAfter(BasicBlock* bStart, BasicBlock* bEnd, BasicBlock* insertAfterBlk);

    enum FG_RELOCATE_TYPE
    {
        FG_RELOCATE_TRY,    // relocate the 'try' region
        FG_RELOCATE_HANDLER // relocate the handler region (including the filter if necessary)
    };
    BasicBlock* fgRelocateEHRange(unsigned regionIndex, FG_RELOCATE_TYPE relocateType);

#if FEATURE_EH_FUNCLETS
#if defined(_TARGET_ARM_)
    void fgClearFinallyTargetBit(BasicBlock* block);
#endif // defined(_TARGET_ARM_)
    bool fgIsIntraHandlerPred(BasicBlock* predBlock, BasicBlock* block);
    bool fgAnyIntraHandlerPreds(BasicBlock* block);
    void fgInsertFuncletPrologBlock(BasicBlock* block);
    void fgCreateFuncletPrologBlocks();
    void fgCreateFunclets();
#else  // !FEATURE_EH_FUNCLETS
    bool fgRelocateEHRegions();
#endif // !FEATURE_EH_FUNCLETS

    bool fgOptimizeUncondBranchToSimpleCond(BasicBlock* block, BasicBlock* target);

    bool fgBlockEndFavorsTailDuplication(BasicBlock* block);

    bool fgBlockIsGoodTailDuplicationCandidate(BasicBlock* block);

    bool fgOptimizeFallthroughTailDup(BasicBlock* block, BasicBlock* target);

    bool fgOptimizeEmptyBlock(BasicBlock* block);

    bool fgOptimizeBranchToEmptyUnconditional(BasicBlock* block, BasicBlock* bDest);

    bool fgOptimizeBranch(BasicBlock* bJump);

    bool fgOptimizeSwitchBranches(BasicBlock* block);

    bool fgOptimizeBranchToNext(BasicBlock* block, BasicBlock* bNext, BasicBlock* bPrev);

    bool fgOptimizeSwitchJumps();
#ifdef DEBUG
    void fgPrintEdgeWeights();
#endif
    void fgComputeEdgeWeights();

    void fgReorderBlocks();

    void fgDetermineFirstColdBlock();

    bool fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bSrc = nullptr);

    bool fgUpdateFlowGraph(bool doTailDup = false);

    void fgFindOperOrder();

    // method that returns if you should split here
    typedef bool(fgSplitPredicate)(GenTree* tree, GenTree* parent, fgWalkData* data);

    void fgSetBlockOrder();

    void fgRemoveReturnBlock(BasicBlock* block);

    /* Helper code that has been factored out */
    inline void fgConvertBBToThrowBB(BasicBlock* block);

    bool fgCastNeeded(GenTreePtr tree, var_types toType);
    GenTreePtr fgDoNormalizeOnStore(GenTreePtr tree);
    GenTreePtr fgMakeTmpArgNode(
        unsigned tmpVarNum FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(const bool passedInRegisters));

    // The following check for loops that don't execute calls
    bool fgLoopCallMarked;

    void fgLoopCallTest(BasicBlock* srcBB, BasicBlock* dstBB);
    void fgLoopCallMark();

    void fgMarkLoopHead(BasicBlock* block);

    unsigned fgGetCodeEstimate(BasicBlock* block);

#if DUMP_FLOWGRAPHS
    const char* fgProcessEscapes(const char* nameIn, escapeMapping_t* map);
    FILE* fgOpenFlowGraphFile(bool* wbDontClose, Phases phase, LPCWSTR type);
    bool fgDumpFlowGraph(Phases phase);

#endif // DUMP_FLOWGRAPHS

#ifdef DEBUG
    void fgDispDoms();
    void fgDispReach();
    void fgDispBBLiveness(BasicBlock* block);
    void fgDispBBLiveness();
    void fgTableDispBasicBlock(BasicBlock* block, int ibcColWidth = 0);
    void fgDispBasicBlocks(BasicBlock* firstBlock, BasicBlock* lastBlock, bool dumpTrees);
    void fgDispBasicBlocks(bool dumpTrees = false);
    void fgDumpStmtTree(GenTreePtr stmt, unsigned blkNum);
    void fgDumpBlock(BasicBlock* block);
    void fgDumpTrees(BasicBlock* firstBlock, BasicBlock* lastBlock);

    static fgWalkPreFn fgStress64RsltMulCB;
    void               fgStress64RsltMul();
    void               fgDebugCheckUpdate();
    void fgDebugCheckBBlist(bool checkBBNum = false, bool checkBBRefs = true);
    void fgDebugCheckBlockLinks();
    void fgDebugCheckLinks(bool morphTrees = false);
    void fgDebugCheckNodeLinks(BasicBlock* block, GenTreePtr stmt);
    void fgDebugCheckFlags(GenTreePtr tree);
#endif

#ifdef LEGACY_BACKEND
    static void fgOrderBlockOps(GenTreePtr  tree,
                                regMaskTP   reg0,
                                regMaskTP   reg1,
                                regMaskTP   reg2,
                                GenTreePtr* opsPtr,   // OUT
                                regMaskTP*  regsPtr); // OUT
#endif                                                // LEGACY_BACKEND

    static GenTreePtr fgGetFirstNode(GenTreePtr tree);
    static bool fgTreeIsInStmt(GenTree* tree, GenTreeStmt* stmt);

    inline bool fgIsInlining()
    {
        return fgExpandInline;
    }

    void fgTraverseRPO();

    //--------------------- Walking the trees in the IR -----------------------

    struct fgWalkData
    {
        Compiler*     compiler;
        fgWalkPreFn*  wtprVisitorFn;
        fgWalkPostFn* wtpoVisitorFn;
        void*         pCallbackData; // user-provided data
        bool          wtprLclsOnly;  // whether to only visit lclvar nodes
        GenTreePtr    parent;        // parent of current node, provided to callback
        GenTreeStack* parentStack;   // stack of parent nodes, if asked for
#ifdef DEBUG
        bool printModified; // callback can use this
#endif
    };

    template <bool      computeStack>
    static fgWalkResult fgWalkTreePreRec(GenTreePtr* pTree, fgWalkData* fgWalkPre);

    // general purpose tree-walker that is capable of doing pre- and post- order
    // callbacks at the same time
    template <bool doPreOrder, bool doPostOrder>
    static fgWalkResult fgWalkTreeRec(GenTreePtr* pTree, fgWalkData* fgWalkPre);

    fgWalkResult fgWalkTreePre(GenTreePtr*  pTree,
                               fgWalkPreFn* visitor,
                               void*        pCallBackData = nullptr,
                               bool         lclVarsOnly   = false,
                               bool         computeStack  = false);

    fgWalkResult fgWalkTree(GenTreePtr*   pTree,
                            fgWalkPreFn*  preVisitor,
                            fgWalkPostFn* postVisitor,
                            void*         pCallBackData = nullptr);

    void fgWalkAllTreesPre(fgWalkPreFn* visitor, void* pCallBackData);

    //----- Postorder

    template <bool      computeStack>
    static fgWalkResult fgWalkTreePostRec(GenTreePtr* pTree, fgWalkData* fgWalkPre);

    fgWalkResult fgWalkTreePost(GenTreePtr*   pTree,
                                fgWalkPostFn* visitor,
                                void*         pCallBackData = nullptr,
                                bool          computeStack  = false);

    // An fgWalkPreFn that looks for expressions that have inline throws in
    // minopts mode. Basically it looks for tress with gtOverflowEx() or
    // GTF_IND_RNGCHK.  It returns WALK_ABORT if one is found.  It
    // returns WALK_SKIP_SUBTREES if GTF_EXCEPT is not set (assumes flags
    // properly propagated to parent trees).  It returns WALK_CONTINUE
    // otherwise.
    static fgWalkResult fgChkThrowCB(GenTreePtr* pTree, Compiler::fgWalkData* data);
    static fgWalkResult fgChkLocAllocCB(GenTreePtr* pTree, Compiler::fgWalkData* data);
    static fgWalkResult fgChkQmarkCB(GenTreePtr* pTree, Compiler::fgWalkData* data);

    /**************************************************************************
     *                          PROTECTED
     *************************************************************************/

protected:
    friend class SsaBuilder;
    friend struct ValueNumberState;

    //--------------------- Detect the basic blocks ---------------------------

    BasicBlock** fgBBs; // Table of pointers to the BBs

    void        fgInitBBLookup();
    BasicBlock* fgLookupBB(unsigned addr);

    void fgMarkJumpTarget(BYTE* jumpTarget, IL_OFFSET offs);

    void fgFindJumpTargets(const BYTE* codeAddr, IL_OFFSET codeSize, BYTE* jumpTarget);

    void fgMarkBackwardJump(BasicBlock* startBlock, BasicBlock* endBlock);

    void fgLinkBasicBlocks();

    void fgMakeBasicBlocks(const BYTE* codeAddr, IL_OFFSET codeSize, BYTE* jumpTarget);

    void fgCheckBasicBlockControlFlow();

    void fgControlFlowPermitted(BasicBlock* blkSrc,
                                BasicBlock* blkDest,
                                BOOL        IsLeave = false /* is the src a leave block */);

    bool fgFlowToFirstBlockOfInnerTry(BasicBlock* blkSrc, BasicBlock* blkDest, bool sibling);

    void fgObserveInlineConstants(OPCODE opcode, const FgStack& stack, bool isInlining);

    void fgAdjustForAddressExposedOrWrittenThis();

    bool                        fgProfileData_ILSizeMismatch;
    ICorJitInfo::ProfileBuffer* fgProfileBuffer;
    ULONG                       fgProfileBufferCount;
    ULONG                       fgNumProfileRuns;

    unsigned fgStressBBProf()
    {
#ifdef DEBUG
        unsigned result = JitConfig.JitStressBBProf();
        if (result == 0)
        {
            if (compStressCompile(STRESS_BB_PROFILE, 15))
            {
                result = 1;
            }
        }
        return result;
#else
        return 0;
#endif
    }

    bool fgHaveProfileData();
    bool fgGetProfileWeightForBasicBlock(IL_OFFSET offset, unsigned* weight);

    bool fgIsUsingProfileWeights()
    {
        return (fgHaveProfileData() || fgStressBBProf());
    }
    void fgInstrumentMethod();

//-------- Insert a statement at the start or end of a basic block --------

#ifdef DEBUG
public:
    static bool fgBlockContainsStatementBounded(BasicBlock* block, GenTree* stmt, bool answerOnBoundExceeded = true);
#endif

public:
    GenTreeStmt* fgInsertStmtAtEnd(BasicBlock* block, GenTreePtr node);

public: // Used by linear scan register allocation
    GenTreeStmt* fgInsertStmtNearEnd(BasicBlock* block, GenTreePtr node);

private:
    GenTreePtr fgInsertStmtAtBeg(BasicBlock* block, GenTreePtr stmt);
    GenTreePtr fgInsertStmtAfter(BasicBlock* block, GenTreePtr insertionPoint, GenTreePtr stmt);

public: // Used by linear scan register allocation
    GenTreePtr fgInsertStmtBefore(BasicBlock* block, GenTreePtr insertionPoint, GenTreePtr stmt);

private:
    GenTreePtr fgInsertStmtListAfter(BasicBlock* block, GenTreePtr stmtAfter, GenTreePtr stmtList);

    GenTreePtr fgMorphSplitTree(GenTree** splitPoint, GenTree* stmt, BasicBlock* blk);

    //                  Create a new temporary variable to hold the result of *ppTree,
    //                  and transform the graph accordingly.
    GenTree* fgInsertCommaFormTemp(GenTree** ppTree, CORINFO_CLASS_HANDLE structType = nullptr);
    GenTree* fgMakeMultiUse(GenTree** ppTree);

    //                  After replacing oldChild with newChild, fixup the fgArgTabEntryPtr
    //                  if it happens to be an argument to a call.
    void fgFixupIfCallArg(ArrayStack<GenTree*>* parentStack, GenTree* oldChild, GenTree* newChild);

public:
    void fgFixupArgTabEntryPtr(GenTreePtr parentCall, GenTreePtr oldArg, GenTreePtr newArg);

private:
    //                  Recognize a bitwise rotation pattern and convert into a GT_ROL or a GT_ROR node.
    GenTreePtr fgRecognizeAndMorphBitwiseRotation(GenTreePtr tree);
    bool fgOperIsBitwiseRotationRoot(genTreeOps oper);

    //-------- Determine the order in which the trees will be evaluated -------

    unsigned fgTreeSeqNum;
    GenTree* fgTreeSeqLst;
    GenTree* fgTreeSeqBeg;

    GenTree* fgSetTreeSeq(GenTree* tree, GenTree* prev = nullptr, bool isLIR = false);
    void fgSetTreeSeqHelper(GenTree* tree, bool isLIR);
    void fgSetTreeSeqFinish(GenTreePtr tree, bool isLIR);
    void fgSetStmtSeq(GenTree* tree);
    void fgSetBlockOrder(BasicBlock* block);

    //------------------------- Morphing --------------------------------------

    unsigned fgPtrArgCntCur;
    unsigned fgPtrArgCntMax;
    hashBv*  fgOutgoingArgTemps;
    hashBv*  fgCurrentlyInUseArgTemps;

    bool compCanEncodePtrArgCntMax();

    void fgSetRngChkTarget(GenTreePtr tree, bool delay = true);

#if REARRANGE_ADDS
    void fgMoveOpsLeft(GenTreePtr tree);
#endif

    bool fgIsCommaThrow(GenTreePtr tree, bool forFolding = false);

    bool fgIsThrow(GenTreePtr tree);

    bool fgInDifferentRegions(BasicBlock* blk1, BasicBlock* blk2);
    bool fgIsBlockCold(BasicBlock* block);

    GenTreePtr fgMorphCastIntoHelper(GenTreePtr tree, int helper, GenTreePtr oper);

    GenTreePtr fgMorphIntoHelperCall(GenTreePtr tree, int helper, GenTreeArgList* args);

    GenTreePtr fgMorphStackArgForVarArgs(unsigned lclNum, var_types varType, unsigned lclOffs);

    bool fgMorphRelopToQmark(GenTreePtr tree);

    // A "MorphAddrContext" carries information from the surrounding context.  If we are evaluating a byref address,
    // it is useful to know whether the address will be immediately dereferenced, or whether the address value will
    // be used, perhaps by passing it as an argument to a called method.  This affects how null checking is done:
    // for sufficiently small offsets, we can rely on OS page protection to implicitly null-check addresses that we
    // know will be dereferenced.  To know that reliance on implicit null checking is sound, we must further know that
    // all offsets between the top-level indirection and the bottom are constant, and that their sum is sufficiently
    // small; hence the other fields of MorphAddrContext.  Finally, the odd structure of GT_COPYBLK, in which the second
    // argument is a GT_LIST, requires us to "tell" that List node that its parent is a GT_COPYBLK, so it "knows" that
    // each of its arguments should be evaluated in MACK_Ind contexts.  (This would not be true for GT_LIST nodes
    // representing method call argument lists.)
    enum MorphAddrContextKind
    {
        MACK_Ind,
        MACK_Addr,
        MACK_CopyBlock, // This is necessary so we know we have to start a new "Ind" context for each of the
                        // addresses in the arg list.
    };
    struct MorphAddrContext
    {
        MorphAddrContextKind m_kind;
        bool                 m_allConstantOffsets; // Valid only for "m_kind == MACK_Ind".  True iff all offsets between
                                                   // top-level indirection and here have been constants.
        size_t m_totalOffset; // Valid only for "m_kind == MACK_Ind", and if "m_allConstantOffsets" is true.
                              // In that case, is the sum of those constant offsets.

        MorphAddrContext(MorphAddrContextKind kind) : m_kind(kind), m_allConstantOffsets(true), m_totalOffset(0)
        {
        }
    };

    // A MACK_CopyBlock context is immutable, so we can just make one of these and share it.
    static MorphAddrContext s_CopyBlockMAC;

#ifdef FEATURE_SIMD
    GenTreePtr fgCopySIMDNode(GenTreeSIMD* simdNode);
    GenTreePtr getSIMDStructFromField(GenTreePtr tree,
                                      var_types* baseTypeOut,
                                      unsigned*  indexOut,
                                      unsigned*  simdSizeOut,
                                      bool       ignoreUsedInSIMDIntrinsic = false);
    GenTreePtr fgMorphFieldAssignToSIMDIntrinsicSet(GenTreePtr tree);
    GenTreePtr fgMorphFieldToSIMDIntrinsicGet(GenTreePtr tree);
    bool fgMorphCombineSIMDFieldAssignments(BasicBlock* block, GenTreePtr stmt);
    void impMarkContiguousSIMDFieldAssignments(GenTreePtr stmt);

    // fgPreviousCandidateSIMDFieldAsgStmt is only used for tracking previous simd field assignment
    // in function: Complier::impMarkContiguousSIMDFieldAssignments.
    GenTreePtr fgPreviousCandidateSIMDFieldAsgStmt;

#endif // FEATURE_SIMD
    GenTreePtr fgMorphArrayIndex(GenTreePtr tree);
    GenTreePtr fgMorphCast(GenTreePtr tree);
    GenTreePtr fgUnwrapProxy(GenTreePtr objRef);
    GenTreeCall* fgMorphArgs(GenTreeCall* call);

    void fgMakeOutgoingStructArgCopy(GenTreeCall*         call,
                                     GenTree*             args,
                                     unsigned             argIndex,
                                     CORINFO_CLASS_HANDLE copyBlkClass FEATURE_UNIX_AMD64_STRUCT_PASSING_ONLY_ARG(
                                         const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structDescPtr));

    void fgFixupStructReturn(GenTreePtr call);
    GenTreePtr fgMorphLocalVar(GenTreePtr tree);
    bool fgAddrCouldBeNull(GenTreePtr addr);
    GenTreePtr fgMorphField(GenTreePtr tree, MorphAddrContext* mac);
    bool fgCanFastTailCall(GenTreeCall* call);
    void fgMorphTailCall(GenTreeCall* call);
    void fgMorphRecursiveFastTailCallIntoLoop(BasicBlock* block, GenTreeCall* recursiveTailCall);
    GenTreePtr fgAssignRecursiveCallArgToCallerParam(GenTreePtr       arg,
                                                     fgArgTabEntryPtr argTabEntry,
                                                     BasicBlock*      block,
                                                     IL_OFFSETX       callILOffset,
                                                     GenTreePtr       tmpAssignmentInsertionPoint,
                                                     GenTreePtr       paramAssignmentInsertionPoint);
    static int fgEstimateCallStackSize(GenTreeCall* call);
    GenTreePtr fgMorphCall(GenTreeCall* call);
    void fgMorphCallInline(GenTreeCall* call, InlineResult* result);
    void fgMorphCallInlineHelper(GenTreeCall* call, InlineResult* result);
#if DEBUG
    void fgNoteNonInlineCandidate(GenTreePtr tree, GenTreeCall* call);
    static fgWalkPreFn fgFindNonInlineCandidate;
#endif
    GenTreePtr fgOptimizeDelegateConstructor(GenTreePtr call, CORINFO_CONTEXT_HANDLE* ExactContextHnd);
    GenTreePtr fgMorphLeaf(GenTreePtr tree);
    void fgAssignSetVarDef(GenTreePtr tree);
    GenTreePtr fgMorphOneAsgBlockOp(GenTreePtr tree);
    GenTreePtr fgMorphInitBlock(GenTreePtr tree);
    GenTreePtr fgMorphCopyBlock(GenTreePtr tree);
    GenTreePtr fgMorphForRegisterFP(GenTreePtr tree);
    GenTreePtr fgMorphSmpOp(GenTreePtr tree, MorphAddrContext* mac = nullptr);
    GenTreePtr fgMorphSmpOpPre(GenTreePtr tree);
    GenTreePtr fgMorphDivByConst(GenTreeOp* tree);
    GenTreePtr fgMorphModByConst(GenTreeOp* tree);
    GenTreePtr fgMorphModToSubMulDiv(GenTreeOp* tree);
    GenTreePtr fgMorphSmpOpOptional(GenTreeOp* tree);
    GenTreePtr fgMorphRecognizeBoxNullable(GenTree* compare);
    bool fgShouldUseMagicNumberDivide(GenTreeOp* tree);

    GenTreePtr fgMorphToEmulatedFP(GenTreePtr tree);
    GenTreePtr fgMorphConst(GenTreePtr tree);

public:
    GenTreePtr fgMorphTree(GenTreePtr tree, MorphAddrContext* mac = nullptr);

private:
#if LOCAL_ASSERTION_PROP
    void fgKillDependentAssertions(unsigned lclNum DEBUGARG(GenTreePtr tree));
#endif
    void fgMorphTreeDone(GenTreePtr tree, GenTreePtr oldTree = nullptr DEBUGARG(int morphNum = 0));

    GenTreePtr fgMorphStmt;

    unsigned fgGetBigOffsetMorphingTemp(var_types type); // We cache one temp per type to be
                                                         // used when morphing big offset.

    //----------------------- Liveness analysis -------------------------------

    VARSET_TP fgCurUseSet; // vars used     by block (before an assignment)
    VARSET_TP fgCurDefSet; // vars assigned by block (before a use)

    bool fgCurHeapUse;   // True iff the current basic block uses the heap before defining it.
    bool fgCurHeapDef;   // True iff the current basic block defines the heap.
    bool fgCurHeapHavoc; // True if  the current basic block is known to set the heap to a "havoc" value.

    void fgMarkUseDef(GenTreeLclVarCommon* tree, GenTree* asgdLclVar = nullptr);

#ifdef DEBUGGING_SUPPORT
    void fgBeginScopeLife(VARSET_TP* inScope, VarScopeDsc* var);
    void fgEndScopeLife(VARSET_TP* inScope, VarScopeDsc* var);

    void fgMarkInScope(BasicBlock* block, VARSET_VALARG_TP inScope);
    void fgUnmarkInScope(BasicBlock* block, VARSET_VALARG_TP unmarkScope);

    void fgExtendDbgScopes();
    void fgExtendDbgLifetimes();

#ifdef DEBUG
    void fgDispDebugScopes();
#endif // DEBUG

#endif // DEBUGGING_SUPPORT

    //-------------------------------------------------------------------------
    //
    //  The following keeps track of any code we've added for things like array
    //  range checking or explicit calls to enable GC, and so on.
    //
public:
    struct AddCodeDsc
    {
        AddCodeDsc*     acdNext;
        BasicBlock*     acdDstBlk; // block  to  which we jump
        unsigned        acdData;
        SpecialCodeKind acdKind; // what kind of a special block is this?
        unsigned short  acdStkLvl;
    };

private:
    static unsigned acdHelper(SpecialCodeKind codeKind);

    AddCodeDsc* fgAddCodeList;
    bool        fgAddCodeModf;
    bool        fgRngChkThrowAdded;
    AddCodeDsc* fgExcptnTargetCache[SCK_COUNT];

    BasicBlock* fgRngChkTarget(BasicBlock* block, unsigned stkDepth, SpecialCodeKind kind);

    BasicBlock* fgAddCodeRef(BasicBlock* srcBlk, unsigned refData, SpecialCodeKind kind, unsigned stkDepth = 0);

public:
    AddCodeDsc* fgFindExcptnTarget(SpecialCodeKind kind, unsigned refData);

private:
    bool fgIsCodeAdded();

    bool fgIsThrowHlpBlk(BasicBlock* block);
    unsigned fgThrowHlpBlkStkLevel(BasicBlock* block);

    unsigned fgBigOffsetMorphingTemps[TYP_COUNT];

    unsigned fgCheckInlineDepthAndRecursion(InlineInfo* inlineInfo);
    void fgInvokeInlineeCompiler(GenTreeCall* call, InlineResult* result);
    void fgInsertInlineeBlocks(InlineInfo* pInlineInfo);
    GenTreePtr fgInlinePrependStatements(InlineInfo* inlineInfo);

#if FEATURE_MULTIREG_RET
    GenTreePtr fgGetStructAsStructPtr(GenTreePtr tree);
    GenTreePtr fgAssignStructInlineeToVar(GenTreePtr child, CORINFO_CLASS_HANDLE retClsHnd);
    void fgAttachStructInlineeToAsg(GenTreePtr tree, GenTreePtr child, CORINFO_CLASS_HANDLE retClsHnd);
#endif // FEATURE_MULTIREG_RET

    static fgWalkPreFn fgUpdateInlineReturnExpressionPlaceHolder;

#ifdef DEBUG
    static fgWalkPreFn fgDebugCheckInlineCandidates;
#endif

    void         fgPromoteStructs();
    fgWalkResult fgMorphStructField(GenTreePtr tree, fgWalkData* fgWalkPre);
    fgWalkResult fgMorphLocalField(GenTreePtr tree, fgWalkData* fgWalkPre);
    void fgMarkImplicitByRefArgs();
    bool fgMorphImplicitByRefArgs(GenTree** pTree, fgWalkData* fgWalkPre);
    static fgWalkPreFn  fgMarkAddrTakenLocalsPreCB;
    static fgWalkPostFn fgMarkAddrTakenLocalsPostCB;
    void                fgMarkAddressExposedLocals();
    bool fgNodesMayInterfere(GenTree* store, GenTree* load);

    // Returns true if the type of tree is of size at least "width", or if "tree" is not a
    // local variable.
    bool fgFitsInOrNotLoc(GenTreePtr tree, unsigned width);

    // The given local variable, required to be a struct variable, is being assigned via
    // a "lclField", to make it masquerade as an integral type in the ABI.  Make sure that
    // the variable is not enregistered, and is therefore not promoted independently.
    void fgLclFldAssign(unsigned lclNum);

    static fgWalkPreFn gtHasLocalsWithAddrOpCB;
    bool gtCanOptimizeTypeEquality(GenTreePtr tree);
    bool gtIsTypeHandleToRuntimeTypeHelper(GenTreePtr tree);
    bool gtIsActiveCSE_Candidate(GenTreePtr tree);

#ifdef DEBUG
    bool fgPrintInlinedMethods;
#endif

    bool fgIsBigOffset(size_t offset);

    // The following are used when morphing special cases of integer div/mod operations and also by codegen
    bool fgIsSignedDivOptimizable(GenTreePtr divisor);
    bool fgIsUnsignedDivOptimizable(GenTreePtr divisor);
    bool fgIsSignedModOptimizable(GenTreePtr divisor);
    bool fgIsUnsignedModOptimizable(GenTreePtr divisor);

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           Optimizer                                       XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    void optInit();

protected:
    LclVarDsc* optIsTrackedLocal(GenTreePtr tree);

public:
    void optRemoveRangeCheck(
        GenTreePtr tree, GenTreePtr stmt, bool updateCSEcounts, unsigned sideEffFlags = 0, bool forceRemove = false);
    bool optIsRangeCheckRemovable(GenTreePtr tree);

protected:
    static fgWalkPreFn optValidRangeCheckIndex;
    static fgWalkPreFn optRemoveTreeVisitor; // Helper passed to Compiler::fgWalkAllTreesPre() to decrement the LclVar
                                             // usage counts

    void optRemoveTree(GenTreePtr deadTree, GenTreePtr keepList);

    /**************************************************************************
     *
     *************************************************************************/

protected:
    // Do hoisting for all loops.
    void optHoistLoopCode();

    // To represent sets of VN's that have already been hoisted in outer loops.
    typedef SimplerHashTable<ValueNum, SmallPrimitiveKeyFuncs<ValueNum>, bool, JitSimplerHashBehavior> VNToBoolMap;
    typedef VNToBoolMap VNSet;

    struct LoopHoistContext
    {
    private:
        // The set of variables hoisted in the current loop (or nullptr if there are none).
        VNSet* m_pHoistedInCurLoop;

    public:
        // Value numbers of expressions that have been hoisted in parent loops in the loop nest.
        VNSet m_hoistedInParentLoops;
        // Value numbers of expressions that have been hoisted in the current (or most recent) loop in the nest.
        // Previous decisions on loop-invariance of value numbers in the current loop.
        VNToBoolMap m_curLoopVnInvariantCache;

        VNSet* GetHoistedInCurLoop(Compiler* comp)
        {
            if (m_pHoistedInCurLoop == nullptr)
            {
                m_pHoistedInCurLoop = new (comp->getAllocatorLoopHoist()) VNSet(comp->getAllocatorLoopHoist());
            }
            return m_pHoistedInCurLoop;
        }

        VNSet* ExtractHoistedInCurLoop()
        {
            VNSet* res          = m_pHoistedInCurLoop;
            m_pHoistedInCurLoop = nullptr;
            return res;
        }

        LoopHoistContext(Compiler* comp)
            : m_pHoistedInCurLoop(nullptr)
            , m_hoistedInParentLoops(comp->getAllocatorLoopHoist())
            , m_curLoopVnInvariantCache(comp->getAllocatorLoopHoist())
        {
        }
    };

    // Do hoisting for loop "lnum" (an index into the optLoopTable), and all loops nested within it.
    // Tracks the expressions that have been hoisted by containing loops by temporary recording their
    // value numbers in "m_hoistedInParentLoops".  This set is not modified by the call.
    void optHoistLoopNest(unsigned lnum, LoopHoistContext* hoistCtxt);

    // Do hoisting for a particular loop ("lnum" is an index into the optLoopTable.)
    // Assumes that expressions have been hoisted in containing loops if their value numbers are in
    // "m_hoistedInParentLoops".
    //
    void optHoistThisLoop(unsigned lnum, LoopHoistContext* hoistCtxt);

    // Hoist all expressions in "blk" that are invariant in loop "lnum" (an index into the optLoopTable)
    // outside of that loop.  Exempt expressions whose value number is in "m_hoistedInParentLoops"; add VN's of hoisted
    // expressions to "hoistInLoop".
    void optHoistLoopExprsForBlock(BasicBlock* blk, unsigned lnum, LoopHoistContext* hoistCtxt);

    // Return true if the tree looks profitable to hoist out of loop 'lnum'.
    bool optIsProfitableToHoistableTree(GenTreePtr tree, unsigned lnum);

    // Hoist all proper sub-expressions of "tree" (which occurs in "stmt", which occurs in "blk")
    // that are invariant in loop "lnum" (an index into the optLoopTable)
    // outside of that loop.  Exempt expressions whose value number is in "hoistedInParents"; add VN's of hoisted
    // expressions to "hoistInLoop".
    // Returns "true" iff "tree" is loop-invariant (wrt "lnum").
    // Assumes that the value of "*firstBlockAndBeforeSideEffect" indicates that we're in the first block, and before
    // any possible globally visible side effects.  Assume is called in evaluation order, and updates this.
    bool optHoistLoopExprsForTree(GenTreePtr        tree,
                                  unsigned          lnum,
                                  LoopHoistContext* hoistCtxt,
                                  bool*             firstBlockAndBeforeSideEffect,
                                  bool*             pHoistable);

    // Performs the hoisting 'tree' into the PreHeader for loop 'lnum'
    void optHoistCandidate(GenTreePtr tree, unsigned lnum, LoopHoistContext* hoistCtxt);

    // Returns true iff the ValueNum "vn" represents a value that is loop-invariant in "lnum".
    //   Constants and init values are always loop invariant.
    //   VNPhi's connect VN's to the SSA definition, so we can know if the SSA def occurs in the loop.
    bool optVNIsLoopInvariant(ValueNum vn, unsigned lnum, VNToBoolMap* recordedVNs);

    // Returns "true" iff "tree" is valid at the head of loop "lnum", in the context of the hoist substitution
    // "subst".  If "tree" is a local SSA var, it is valid if its SSA definition occurs outside of the loop, or
    // if it is in the domain of "subst" (meaning that it's definition has been previously hoisted, with a "standin"
    // local.)  If tree is a constant, it is valid.  Otherwise, if it is an operator, it is valid iff its children are.
    bool optTreeIsValidAtLoopHead(GenTreePtr tree, unsigned lnum);

    // If "blk" is the entry block of a natural loop, returns true and sets "*pLnum" to the index of the loop
    // in the loop table.
    bool optBlockIsLoopEntry(BasicBlock* blk, unsigned* pLnum);

    // Records the set of "side effects" of all loops: fields (object instance and static)
    // written to, and SZ-array element type equivalence classes updated.
    void optComputeLoopSideEffects();

private:
    // Requires "lnum" to be the index of an outermost loop in the loop table.  Traverses the body of that loop,
    // including all nested loops, and records the set of "side effects" of the loop: fields (object instance and
    // static) written to, and SZ-array element type equivalence classes updated.
    void optComputeLoopNestSideEffects(unsigned lnum);

    // Add the side effects of "blk" (which is required to be within a loop) to all loops of which it is a part.
    void optComputeLoopSideEffectsOfBlock(BasicBlock* blk);

    // Hoist the expression "expr" out of loop "lnum".
    void optPerformHoistExpr(GenTreePtr expr, unsigned lnum);

public:
    void optOptimizeBools();

private:
    GenTree* optIsBoolCond(GenTree* condBranch, GenTree** compPtr, bool* boolPtr);
#ifdef DEBUG
    void optOptimizeBoolsGcStress(BasicBlock* condBlock);
#endif
public:
    void optOptimizeLayout(); // Optimize the BasicBlock layout of the method

    void optOptimizeLoops(); // for "while-do" loops duplicates simple loop conditions and transforms
                             // the loop into a "do-while" loop
                             // Also finds all natural loops and records them in the loop table

    // Optionally clone loops in the loop table.
    void optCloneLoops();

    // Clone loop "loopInd" in the loop table.
    void optCloneLoop(unsigned loopInd, LoopCloneContext* context);

    // Ensure that loop "loopInd" has a unique head block.  (If the existing entry has
    // non-loop predecessors other than the head entry, create a new, empty block that goes (only) to the entry,
    // and redirects the preds of the entry to this new block.)  Sets the weight of the newly created block to
    // "ambientWeight".
    void optEnsureUniqueHead(unsigned loopInd, unsigned ambientWeight);

    void optUnrollLoops(); // Unrolls loops (needs to have cost info)

protected:
    // This enumeration describes what is killed by a call.

    enum callInterf
    {
        CALLINT_NONE,       // no interference                               (most helpers)
        CALLINT_REF_INDIRS, // kills GC ref indirections                     (SETFIELD OBJ)
        CALLINT_SCL_INDIRS, // kills non GC ref indirections                 (SETFIELD non-OBJ)
        CALLINT_ALL_INDIRS, // kills both GC ref and non GC ref indirections (SETFIELD STRUCT)
        CALLINT_ALL,        // kills everything                              (normal method call)
    };

public:
    // A "LoopDsc" describes a ("natural") loop.  We (currently) require the body of a loop to be a contiguous (in
    // bbNext order) sequence of basic blocks.  (At times, we may require the blocks in a loop to be "properly numbered"
    // in bbNext order; we use comparisons on the bbNum to decide order.)
    // The blocks that define the body are
    //   first <= top <= entry <= bottom   .
    // The "head" of the loop is a block outside the loop that has "entry" as a successor. We only support loops with a
    // single 'head' block. The meanings of these blocks are given in the definitions below. Also see the picture at
    // Compiler::optFindNaturalLoops().
    struct LoopDsc
    {
        BasicBlock* lpHead;  // HEAD of the loop (not part of the looping of the loop) -- has ENTRY as a successor.
        BasicBlock* lpFirst; // FIRST block (in bbNext order) reachable within this loop.  (May be part of a nested
                             // loop, but not the outer loop.)
        BasicBlock* lpTop;   // loop TOP (the back edge from lpBottom reaches here) (in most cases FIRST and TOP are the
                             // same)
        BasicBlock* lpEntry; // the ENTRY in the loop (in most cases TOP or BOTTOM)
        BasicBlock* lpBottom; // loop BOTTOM (from here we have a back edge to the TOP)
        BasicBlock* lpExit;   // if a single exit loop this is the EXIT (in most cases BOTTOM)

        callInterf   lpAsgCall;     // "callInterf" for calls in the loop
        ALLVARSET_TP lpAsgVars;     // set of vars assigned within the loop (all vars, not just tracked)
        varRefKinds  lpAsgInds : 8; // set of inds modified within the loop

        unsigned short lpFlags; // Mask of the LPFLG_* constants

        unsigned char lpExitCnt; // number of exits from the loop

        unsigned char lpParent;  // The index of the most-nested loop that completely contains this one,
                                 // or else BasicBlock::NOT_IN_LOOP if no such loop exists.
        unsigned char lpChild;   // The index of a nested loop, or else BasicBlock::NOT_IN_LOOP if no child exists.
                                 // (Actually, an "immediately" nested loop --
                                 // no other child of this loop is a parent of lpChild.)
        unsigned char lpSibling; // The index of another loop that is an immediate child of lpParent,
                                 // or else BasicBlock::NOT_IN_LOOP.  One can enumerate all the children of a loop
                                 // by following "lpChild" then "lpSibling" links.

#define LPFLG_DO_WHILE 0x0001 // it's a do-while loop (i.e ENTRY is at the TOP)
#define LPFLG_ONE_EXIT 0x0002 // the loop has only one exit

#define LPFLG_ITER 0x0004      // for (i = icon or lclVar; test_condition(); i++)
#define LPFLG_HOISTABLE 0x0008 // the loop is in a form that is suitable for hoisting expressions
#define LPFLG_CONST 0x0010     // for (i=icon;i<icon;i++){ ... } - constant loop

#define LPFLG_VAR_INIT 0x0020   // iterator is initialized with a local var (var # found in lpVarInit)
#define LPFLG_CONST_INIT 0x0040 // iterator is initialized with a constant (found in lpConstInit)

#define LPFLG_VAR_LIMIT 0x0100    // iterator is compared with a local var (var # found in lpVarLimit)
#define LPFLG_CONST_LIMIT 0x0200  // iterator is compared with a constant (found in lpConstLimit)
#define LPFLG_ARRLEN_LIMIT 0x0400 // iterator is compared with a.len or a[i].len (found in lpArrLenLimit)

#define LPFLG_HAS_PREHEAD 0x0800 // lpHead is known to be a preHead for this loop
#define LPFLG_REMOVED 0x1000     // has been removed from the loop table (unrolled or optimized away)
#define LPFLG_DONT_UNROLL 0x2000 // do not unroll this loop

#define LPFLG_ASGVARS_YES 0x4000 // "lpAsgVars" has been  computed
#define LPFLG_ASGVARS_INC 0x8000 // "lpAsgVars" is incomplete -- vars beyond those representable in an AllVarSet
                                 // type are assigned to.

        bool lpLoopHasHeapHavoc; // The loop contains an operation that we assume has arbitrary heap side effects.
                                 // If this is set, the fields below may not be accurate (since they become irrelevant.)
        bool lpContainsCall;     // True if executing the loop body *may* execute a call

        VARSET_TP lpVarInOut;  // The set of variables that are IN or OUT during the execution of this loop
        VARSET_TP lpVarUseDef; // The set of variables that are USE or DEF during the execution of this loop

        int lpHoistedExprCount; // The register count for the non-FP expressions from inside this loop that have been
                                // hoisted
        int lpLoopVarCount;     // The register count for the non-FP LclVars that are read/written inside this loop
        int lpVarInOutCount;    // The register count for the non-FP LclVars that are alive inside or accross this loop

        int lpHoistedFPExprCount; // The register count for the FP expressions from inside this loop that have been
                                  // hoisted
        int lpLoopVarFPCount;     // The register count for the FP LclVars that are read/written inside this loop
        int lpVarInOutFPCount;    // The register count for the FP LclVars that are alive inside or accross this loop

        typedef SimplerHashTable<CORINFO_FIELD_HANDLE,
                                 PtrKeyFuncs<struct CORINFO_FIELD_STRUCT_>,
                                 bool,
                                 JitSimplerHashBehavior>
                        FieldHandleSet;
        FieldHandleSet* lpFieldsModified; // This has entries (mappings to "true") for all static field and object
                                          // instance fields modified
                                          // in the loop.

        typedef SimplerHashTable<CORINFO_CLASS_HANDLE,
                                 PtrKeyFuncs<struct CORINFO_CLASS_STRUCT_>,
                                 bool,
                                 JitSimplerHashBehavior>
                        ClassHandleSet;
        ClassHandleSet* lpArrayElemTypesModified; // Bits set indicate the set of sz array element types such that
                                                  // arrays of that type are modified
                                                  // in the loop.

        // Adds the variable liveness information for 'blk' to 'this' LoopDsc
        void AddVariableLiveness(Compiler* comp, BasicBlock* blk);

        inline void AddModifiedField(Compiler* comp, CORINFO_FIELD_HANDLE fldHnd);
        // This doesn't *always* take a class handle -- it can also take primitive types, encoded as class handles
        // (shifted left, with a low-order bit set to distinguish.)
        // Use the {Encode/Decode}ElemType methods to construct/destruct these.
        inline void AddModifiedElemType(Compiler* comp, CORINFO_CLASS_HANDLE structHnd);

        /* The following values are set only for iterator loops, i.e. has the flag LPFLG_ITER set */

        GenTreePtr lpIterTree;    // The "i <op>= const" tree
        unsigned   lpIterVar();   // iterator variable #
        int        lpIterConst(); // the constant with which the iterator is incremented
        genTreeOps lpIterOper();  // the type of the operation on the iterator (ASG_ADD, ASG_SUB, etc.)
        void       VERIFY_lpIterTree();

        var_types lpIterOperType(); // For overflow instructions

        union {
            int lpConstInit; // initial constant value of iterator                           : Valid if LPFLG_CONST_INIT
            unsigned lpVarInit; // initial local var number to which we initialize the iterator : Valid if
                                // LPFLG_VAR_INIT
        };

        /* The following is for LPFLG_ITER loops only (i.e. the loop condition is "i RELOP const or var" */

        GenTreePtr lpTestTree;   // pointer to the node containing the loop test
        genTreeOps lpTestOper(); // the type of the comparison between the iterator and the limit (GT_LE, GT_GE, etc.)
        void       VERIFY_lpTestTree();

        bool       lpIsReversed(); // true if the iterator node is the second operand in the loop condition
        GenTreePtr lpIterator();   // the iterator node in the loop test
        GenTreePtr lpLimit();      // the limit node in the loop test

        int lpConstLimit();    // limit   constant value of iterator - loop condition is "i RELOP const" : Valid if
                               // LPFLG_CONST_LIMIT
        unsigned lpVarLimit(); // the lclVar # in the loop condition ( "i RELOP lclVar" )                : Valid if
                               // LPFLG_VAR_LIMIT
        bool lpArrLenLimit(Compiler* comp, ArrIndex* index); // The array length in the loop condition ( "i RELOP
                                                             // arr.len" or "i RELOP arr[i][j].len" )  : Valid if
                                                             // LPFLG_ARRLEN_LIMIT

        // Returns "true" iff "*this" contains the blk.
        bool lpContains(BasicBlock* blk)
        {
            return lpFirst->bbNum <= blk->bbNum && blk->bbNum <= lpBottom->bbNum;
        }
        // Returns "true" iff "*this" (properly) contains the range [first, bottom] (allowing firsts
        // to be equal, but requiring bottoms to be different.)
        bool lpContains(BasicBlock* first, BasicBlock* bottom)
        {
            return lpFirst->bbNum <= first->bbNum && bottom->bbNum < lpBottom->bbNum;
        }

        // Returns "true" iff "*this" (properly) contains "lp2" (allowing firsts to be equal, but requiring
        // bottoms to be different.)
        bool lpContains(const LoopDsc& lp2)
        {
            return lpContains(lp2.lpFirst, lp2.lpBottom);
        }

        // Returns "true" iff "*this" is (properly) contained by the range [first, bottom]
        // (allowing firsts to be equal, but requiring bottoms to be different.)
        bool lpContainedBy(BasicBlock* first, BasicBlock* bottom)
        {
            return first->bbNum <= lpFirst->bbNum && lpBottom->bbNum < bottom->bbNum;
        }

        // Returns "true" iff "*this" is (properly) contained by "lp2"
        // (allowing firsts to be equal, but requiring bottoms to be different.)
        bool lpContainedBy(const LoopDsc& lp2)
        {
            return lpContains(lp2.lpFirst, lp2.lpBottom);
        }

        // Returns "true" iff "*this" is disjoint from the range [top, bottom].
        bool lpDisjoint(BasicBlock* first, BasicBlock* bottom)
        {
            return bottom->bbNum < lpFirst->bbNum || lpBottom->bbNum < first->bbNum;
        }
        // Returns "true" iff "*this" is disjoint from "lp2".
        bool lpDisjoint(const LoopDsc& lp2)
        {
            return lpDisjoint(lp2.lpFirst, lp2.lpBottom);
        }
        // Returns "true" iff the loop is well-formed (see code for defn).
        bool lpWellFormed()
        {
            return lpFirst->bbNum <= lpTop->bbNum && lpTop->bbNum <= lpEntry->bbNum &&
                   lpEntry->bbNum <= lpBottom->bbNum &&
                   (lpHead->bbNum < lpTop->bbNum || lpHead->bbNum > lpBottom->bbNum);
        }
    };

protected:
    bool fgMightHaveLoop(); // returns true if there are any backedges
    bool fgHasLoops;        // True if this method has any loops, set in fgComputeReachability

public:
    LoopDsc       optLoopTable[MAX_LOOP_NUM]; // loop descriptor table
    unsigned char optLoopCount;               // number of tracked loops

protected:
    unsigned optCallCount;         // number of calls made in the method
    unsigned optIndirectCallCount; // number of virtual, interface and indirect calls made in the method
    unsigned optNativeCallCount;   // number of Pinvoke/Native calls made in the method
    unsigned optLoopsCloned;       // number of loops cloned in the current method.

#ifdef DEBUG
    unsigned optFindLoopNumberFromBeginBlock(BasicBlock* begBlk);
    void optPrintLoopInfo(unsigned      loopNum,
                          BasicBlock*   lpHead,
                          BasicBlock*   lpFirst,
                          BasicBlock*   lpTop,
                          BasicBlock*   lpEntry,
                          BasicBlock*   lpBottom,
                          unsigned char lpExitCnt,
                          BasicBlock*   lpExit,
                          unsigned      parentLoop = BasicBlock::NOT_IN_LOOP);
    void optPrintLoopInfo(unsigned lnum);
    void optPrintLoopRecording(unsigned lnum);

    void optCheckPreds();
#endif

    void optSetBlockWeights();

    void optMarkLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk, bool excludeEndBlk);

    void optUnmarkLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk);

    void optUpdateLoopsBeforeRemoveBlock(BasicBlock* block, bool skipUnmarkLoop = false);

    bool optIsLoopTestEvalIntoTemp(GenTreePtr test, GenTreePtr* newTest);
    unsigned optIsLoopIncrTree(GenTreePtr incr);
    bool optCheckIterInLoopTest(unsigned loopInd, GenTreePtr test, BasicBlock* from, BasicBlock* to, unsigned iterVar);
    bool optComputeIterInfo(GenTreePtr incr, BasicBlock* from, BasicBlock* to, unsigned* pIterVar);
    bool optPopulateInitInfo(unsigned loopInd, GenTreePtr init, unsigned iterVar);
    bool optExtractInitTestIncr(BasicBlock* head,
                                BasicBlock* bottom,
                                BasicBlock* exit,
                                GenTreePtr* ppInit,
                                GenTreePtr* ppTest,
                                GenTreePtr* ppIncr);

    void optRecordLoop(BasicBlock*   head,
                       BasicBlock*   first,
                       BasicBlock*   top,
                       BasicBlock*   entry,
                       BasicBlock*   bottom,
                       BasicBlock*   exit,
                       unsigned char exitCnt);

    void optFindNaturalLoops();

    // Ensures that all the loops in the loop nest rooted at "loopInd" (an index into the loop table) are 'canonical' --
    // each loop has a unique "top."  Returns "true" iff the flowgraph has been modified.
    bool optCanonicalizeLoopNest(unsigned char loopInd);

    // Ensures that the loop "loopInd" (an index into the loop table) is 'canonical' -- it has a unique "top,"
    // unshared with any other loop.  Returns "true" iff the flowgraph has been modified
    bool optCanonicalizeLoop(unsigned char loopInd);

    // Requires "l1" to be a valid loop table index, and not "BasicBlock::NOT_IN_LOOP".  Requires "l2" to be
    // a valid loop table index, or else "BasicBlock::NOT_IN_LOOP".  Returns true
    // iff "l2" is not NOT_IN_LOOP, and "l1" contains "l2".
    bool optLoopContains(unsigned l1, unsigned l2);

    // Requires "loopInd" to be a valid index into the loop table.
    // Updates the loop table by changing loop "loopInd", whose head is required
    // to be "from", to be "to".  Also performs this transformation for any
    // loop nested in "loopInd" that shares the same head as "loopInd".
    void optUpdateLoopHead(unsigned loopInd, BasicBlock* from, BasicBlock* to);

    // Updates the successors of "blk": if "blk2" is a successor of "blk", and there is a mapping for "blk2->blk3" in
    // "redirectMap", change "blk" so that "blk3" is this successor. Note that the predecessor lists are not updated.
    void optRedirectBlock(BasicBlock* blk, BlockToBlockMap* redirectMap);

    // Marks the containsCall information to "lnum" and any parent loops.
    void AddContainsCallAllContainingLoops(unsigned lnum);
    // Adds the variable liveness information from 'blk' to "lnum" and any parent loops.
    void AddVariableLivenessAllContainingLoops(unsigned lnum, BasicBlock* blk);
    // Adds "fldHnd" to the set of modified fields of "lnum" and any parent loops.
    void AddModifiedFieldAllContainingLoops(unsigned lnum, CORINFO_FIELD_HANDLE fldHnd);
    // Adds "elemType" to the set of modified array element types of "lnum" and any parent loops.
    void AddModifiedElemTypeAllContainingLoops(unsigned lnum, CORINFO_CLASS_HANDLE elemType);

    // Requires that "from" and "to" have the same "bbJumpKind" (perhaps because "to" is a clone
    // of "from".)  Copies the jump destination from "from" to "to".
    void optCopyBlkDest(BasicBlock* from, BasicBlock* to);

    // The depth of the loop described by "lnum" (an index into the loop table.) (0 == top level)
    unsigned optLoopDepth(unsigned lnum)
    {
        unsigned par = optLoopTable[lnum].lpParent;
        if (par == BasicBlock::NOT_IN_LOOP)
        {
            return 0;
        }
        else
        {
            return 1 + optLoopDepth(par);
        }
    }

    void fgOptWhileLoop(BasicBlock* block);

    bool optComputeLoopRep(int        constInit,
                           int        constLimit,
                           int        iterInc,
                           genTreeOps iterOper,
                           var_types  iterType,
                           genTreeOps testOper,
                           bool       unsignedTest,
                           bool       dupCond,
                           unsigned*  iterCount);
#if FEATURE_STACK_FP_X87

public:
    VARSET_TP optAllFloatVars; // mask of all tracked      FP variables
    VARSET_TP optAllFPregVars; // mask of all enregistered FP variables
    VARSET_TP optAllNonFPvars; // mask of all tracked  non-FP variables
#endif                         // FEATURE_STACK_FP_X87

private:
    static fgWalkPreFn optIsVarAssgCB;

protected:
    bool optIsVarAssigned(BasicBlock* beg, BasicBlock* end, GenTreePtr skip, unsigned var);

    bool optIsVarAssgLoop(unsigned lnum, unsigned var);

    int optIsSetAssgLoop(unsigned lnum, ALLVARSET_VALARG_TP vars, varRefKinds inds = VR_NONE);

    bool optNarrowTree(GenTreePtr tree, var_types srct, var_types dstt, ValueNumPair vnpNarrow, bool doit);

    /**************************************************************************
     *                       Optimization conditions
     *************************************************************************/

    bool optFastCodeOrBlendedLoop(BasicBlock::weight_t bbWeight);
    bool optPentium4(void);
    bool optAvoidIncDec(BasicBlock::weight_t bbWeight);
    bool optAvoidIntMult(void);

#if FEATURE_ANYCSE

protected:
    //  The following is the upper limit on how many expressions we'll keep track
    //  of for the CSE analysis.
    //
    static const unsigned MAX_CSE_CNT = EXPSET_SZ;

    static const int MIN_CSE_COST = 2;

    /* Generic list of nodes - used by the CSE logic */

    struct treeLst
    {
        treeLst*   tlNext;
        GenTreePtr tlTree;
    };

    typedef struct treeLst* treeLstPtr;

    struct treeStmtLst
    {
        treeStmtLst* tslNext;
        GenTreePtr   tslTree;  // tree node
        GenTreePtr   tslStmt;  // statement containing the tree
        BasicBlock*  tslBlock; // block containing the statement
    };

    typedef struct treeStmtLst* treeStmtLstPtr;

    // The following logic keeps track of expressions via a simple hash table.

    struct CSEdsc
    {
        CSEdsc* csdNextInBucket; // used by the hash table

        unsigned csdHashValue; // the orginal hashkey

        unsigned csdIndex;          // 1..optCSECandidateCount
        char     csdLiveAcrossCall; // 0 or 1

        unsigned short csdDefCount; // definition   count
        unsigned short csdUseCount; // use          count  (excluding the implicit uses at defs)

        unsigned csdDefWtCnt; // weighted def count
        unsigned csdUseWtCnt; // weighted use count  (excluding the implicit uses at defs)

        GenTreePtr  csdTree;  // treenode containing the 1st occurance
        GenTreePtr  csdStmt;  // stmt containing the 1st occurance
        BasicBlock* csdBlock; // block containing the 1st occurance

        treeStmtLstPtr csdTreeList; // list of matching tree nodes: head
        treeStmtLstPtr csdTreeLast; // list of matching tree nodes: tail
    };

    static const size_t s_optCSEhashSize;
    CSEdsc**            optCSEhash;
    CSEdsc**            optCSEtab;

    void optCSEstop();

    CSEdsc* optCSEfindDsc(unsigned index);
    void optUnmarkCSE(GenTreePtr tree);

    // user defined callback data for the tree walk function optCSE_MaskHelper()
    struct optCSE_MaskData
    {
        EXPSET_TP CSE_defMask;
        EXPSET_TP CSE_useMask;
    };

    // Treewalk helper for optCSE_DefMask and optCSE_UseMask
    static fgWalkPreFn optCSE_MaskHelper;

    // This function walks all the node for an given tree
    // and return the mask of CSE definitions and uses for the tree
    //
    void optCSE_GetMaskData(GenTreePtr tree, optCSE_MaskData* pMaskData);

    // Given a binary tree node return true if it is safe to swap the order of evaluation for op1 and op2.
    bool optCSE_canSwap(GenTree* firstNode, GenTree* secondNode);
    bool optCSE_canSwap(GenTree* tree);

    static fgWalkPostFn optPropagateNonCSE;
    static fgWalkPreFn  optHasNonCSEChild;

    static fgWalkPreFn optUnmarkCSEs;

    static int __cdecl optCSEcostCmpEx(const void* op1, const void* op2);
    static int __cdecl optCSEcostCmpSz(const void* op1, const void* op2);

    void optCleanupCSEs();

#ifdef DEBUG
    void optEnsureClearCSEInfo();
#endif // DEBUG

#endif // FEATURE_ANYCSE

#if FEATURE_VALNUM_CSE
    /**************************************************************************
     *                   Value Number based CSEs
     *************************************************************************/

public:
    void optOptimizeValnumCSEs();

protected:
    void     optValnumCSE_Init();
    unsigned optValnumCSE_Index(GenTreePtr tree, GenTreePtr stmt);
    unsigned optValnumCSE_Locate();
    void     optValnumCSE_InitDataFlow();
    void     optValnumCSE_DataFlow();
    void     optValnumCSE_Availablity();
    void     optValnumCSE_Heuristic();
    void optValnumCSE_UnmarkCSEs(GenTreePtr deadTree, GenTreePtr keepList);

#endif // FEATURE_VALNUM_CSE

#if FEATURE_ANYCSE
    bool     optDoCSE;             // True when we have found a duplicate CSE tree
    bool     optValnumCSE_phase;   // True when we are executing the optValnumCSE_phase
    unsigned optCSECandidateTotal; // Grand total of CSE candidates for both Lexical and ValNum
    unsigned optCSECandidateCount; // Count of CSE's candidates, reset for Lexical and ValNum CSE's
    unsigned optCSEstart;          // The first local variable number that is a CSE
    unsigned optCSEcount;          // The total count of CSE's introduced.
    unsigned optCSEweight;         // The weight of the current block when we are
                                   // scanning for CSE expressions

    bool optIsCSEcandidate(GenTreePtr tree);

    // lclNumIsTrueCSE returns true if the LclVar was introduced by the CSE phase of the compiler
    //
    bool lclNumIsTrueCSE(unsigned lclNum) const
    {
        return ((optCSEcount > 0) && (lclNum >= optCSEstart) && (lclNum < optCSEstart + optCSEcount));
    }

    //  lclNumIsCSE returns true if the LclVar should be treated like a CSE with regards to constant prop.
    //
    bool lclNumIsCSE(unsigned lclNum) const
    {
        return lvaTable[lclNum].lvIsCSE;
    }

#ifdef DEBUG
    bool optConfigDisableCSE();
    bool optConfigDisableCSE2();
#endif
    void optOptimizeCSEs();

#endif // FEATURE_ANYCSE

    struct isVarAssgDsc
    {
        GenTreePtr ivaSkip;
#ifdef DEBUG
        void* ivaSelf;
#endif
        unsigned     ivaVar;            // Variable we are interested in, or -1
        ALLVARSET_TP ivaMaskVal;        // Set of variables assigned to.  This is a set of all vars, not tracked vars.
        bool         ivaMaskIncomplete; // Variables not representable in ivaMaskVal were assigned to.
        varRefKinds  ivaMaskInd;        // What kind of indirect assignments are there?
        callInterf   ivaMaskCall;       // What kind of calls are there?
    };

    static callInterf optCallInterf(GenTreePtr call);

public:
    // VN based copy propagation.
    typedef ArrayStack<GenTreePtr> GenTreePtrStack;
    typedef SimplerHashTable<unsigned, SmallPrimitiveKeyFuncs<unsigned>, GenTreePtrStack*, JitSimplerHashBehavior>
        LclNumToGenTreePtrStack;

    // Kill set to track variables with intervening definitions.
    VARSET_TP optCopyPropKillSet;

    // Copy propagation functions.
    void optCopyProp(BasicBlock* block, GenTreePtr stmt, GenTreePtr tree, LclNumToGenTreePtrStack* curSsaName);
    void optBlockCopyPropPopStacks(BasicBlock* block, LclNumToGenTreePtrStack* curSsaName);
    void optBlockCopyProp(BasicBlock* block, LclNumToGenTreePtrStack* curSsaName);
    bool optIsSsaLocal(GenTreePtr tree);
    int optCopyProp_LclVarScore(LclVarDsc* lclVarDsc, LclVarDsc* copyVarDsc, bool preferOp2);
    void optVnCopyProp();

    /**************************************************************************
    *               Early value propagation
    *************************************************************************/
    struct SSAName
    {
        unsigned m_lvNum;
        unsigned m_ssaNum;

        SSAName(unsigned lvNum, unsigned ssaNum) : m_lvNum(lvNum), m_ssaNum(ssaNum)
        {
        }

        static unsigned GetHashCode(SSAName ssaNm)
        {
            return (ssaNm.m_lvNum << 16) | (ssaNm.m_ssaNum);
        }

        static bool Equals(SSAName ssaNm1, SSAName ssaNm2)
        {
            return (ssaNm1.m_lvNum == ssaNm2.m_lvNum) && (ssaNm1.m_ssaNum == ssaNm2.m_ssaNum);
        }
    };

#define OMF_HAS_NEWARRAY 0x00000001  // Method contains 'new' of an array
#define OMF_HAS_NEWOBJ 0x00000002    // Method contains 'new' of an object type.
#define OMF_HAS_ARRAYREF 0x00000004  // Method contains array element loads or stores.
#define OMF_HAS_VTABLEREF 0x00000008 // Method contains method table reference.
#define OMF_HAS_NULLCHECK 0x00000010 // Method contains null check.

    unsigned optMethodFlags;

    // Recursion bound controls how far we can go backwards tracking for a SSA value.
    // No throughput diff was found with backward walk bound between 3-8.
    static const int optEarlyPropRecurBound = 5;

    enum class optPropKind
    {
        OPK_INVALID,
        OPK_ARRAYLEN,
        OPK_OBJ_GETTYPE,
        OPK_NULLCHECK
    };

    bool gtIsVtableRef(GenTreePtr tree);
    GenTreePtr getArrayLengthFromAllocation(GenTreePtr tree);
    GenTreePtr getObjectHandleNodeFromAllocation(GenTreePtr tree);
    GenTreePtr optPropGetValueRec(unsigned lclNum, unsigned ssaNum, optPropKind valueKind, int walkDepth);
    GenTreePtr optPropGetValue(unsigned lclNum, unsigned ssaNum, optPropKind valueKind);
    bool optEarlyPropRewriteTree(GenTreePtr tree);
    bool optDoEarlyPropForBlock(BasicBlock* block);
    bool optDoEarlyPropForFunc();
    void optEarlyProp();
    void optFoldNullCheck(GenTreePtr tree);
    bool optCanMoveNullCheckPastTree(GenTreePtr tree, bool isInsideTry);

#if ASSERTION_PROP
    /**************************************************************************
     *               Value/Assertion propagation
     *************************************************************************/
public:
    // Data structures for assertion prop
    BitVecTraits* apTraits;
    ASSERT_TP     apFull;
    ASSERT_TP     apEmpty;

    enum optAssertionKind
    {
        OAK_INVALID,
        OAK_EQUAL,
        OAK_NOT_EQUAL,
        OAK_SUBRANGE,
        OAK_NO_THROW,
        OAK_COUNT
    };

    enum optOp1Kind
    {
        O1K_INVALID,
        O1K_LCLVAR,
        O1K_ARR_BND,
        O1K_ARRLEN_OPER_BND,
        O1K_ARRLEN_LOOP_BND,
        O1K_CONSTANT_LOOP_BND,
        O1K_EXACT_TYPE,
        O1K_SUBTYPE,
        O1K_VALUE_NUMBER,
        O1K_COUNT
    };

    enum optOp2Kind
    {
        O2K_INVALID,
        O2K_LCLVAR_COPY,
        O2K_IND_CNS_INT,
        O2K_CONST_INT,
        O2K_CONST_LONG,
        O2K_CONST_DOUBLE,
        O2K_ARR_LEN,
        O2K_SUBRANGE,
        O2K_COUNT
    };
    struct AssertionDsc
    {
        optAssertionKind assertionKind;
        struct SsaVar
        {
            unsigned lclNum; // assigned to or property of this local var number
            unsigned ssaNum;
        };
        struct ArrBnd
        {
            ValueNum vnIdx;
            ValueNum vnLen;
        };
        struct AssertionDscOp1
        {
            optOp1Kind kind; // a normal LclVar, or Exact-type or Subtype
            ValueNum   vn;
            union {
                SsaVar lcl;
                ArrBnd bnd;
            };
        } op1;
        struct AssertionDscOp2
        {
            optOp2Kind kind; // a const or copy assignment
            ValueNum   vn;
            struct IntVal
            {
                ssize_t  iconVal;   // integer
                unsigned iconFlags; // gtFlags
            };
            struct Range // integer subrange
            {
                ssize_t loBound;
                ssize_t hiBound;
            };
            union {
                SsaVar  lcl;
                IntVal  u1;
                __int64 lconVal;
                double  dconVal;
                Range   u2;
            };
        } op2;

        bool IsArrLenArithBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) && op1.kind == O1K_ARRLEN_OPER_BND);
        }
        bool IsArrLenBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) && op1.kind == O1K_ARRLEN_LOOP_BND);
        }
        bool IsConstantBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) &&
                    op1.kind == O1K_CONSTANT_LOOP_BND);
        }
        bool IsBoundsCheckNoThrow()
        {
            return ((assertionKind == OAK_NO_THROW) && (op1.kind == O1K_ARR_BND));
        }

        bool IsCopyAssertion()
        {
            return ((assertionKind == OAK_EQUAL) && (op1.kind == O1K_LCLVAR) && (op2.kind == O2K_LCLVAR_COPY));
        }

        static bool SameKind(AssertionDsc* a1, AssertionDsc* a2)
        {
            return a1->assertionKind == a2->assertionKind && a1->op1.kind == a2->op1.kind &&
                   a1->op2.kind == a2->op2.kind;
        }

        static bool ComplementaryKind(optAssertionKind kind, optAssertionKind kind2)
        {
            if (kind == OAK_EQUAL)
            {
                return kind2 == OAK_NOT_EQUAL;
            }
            else if (kind == OAK_NOT_EQUAL)
            {
                return kind2 == OAK_EQUAL;
            }
            return false;
        }

        static ssize_t GetLowerBoundForIntegralType(var_types type)
        {
            switch (type)
            {
                case TYP_BYTE:
                    return SCHAR_MIN;
                case TYP_SHORT:
                    return SHRT_MIN;
                case TYP_INT:
                    return INT_MIN;
                case TYP_BOOL:
                case TYP_UBYTE:
                case TYP_CHAR:
                case TYP_USHORT:
                case TYP_UINT:
                    return 0;
                default:
                    unreached();
            }
        }
        static ssize_t GetUpperBoundForIntegralType(var_types type)
        {
            switch (type)
            {
                case TYP_BOOL:
                    return 1;
                case TYP_BYTE:
                    return SCHAR_MAX;
                case TYP_SHORT:
                    return SHRT_MAX;
                case TYP_INT:
                    return INT_MAX;
                case TYP_UBYTE:
                    return UCHAR_MAX;
                case TYP_CHAR:
                case TYP_USHORT:
                    return USHRT_MAX;
                case TYP_UINT:
                    return UINT_MAX;
                default:
                    unreached();
            }
        }

        bool HasSameOp1(AssertionDsc* that, bool vnBased)
        {
            return (op1.kind == that->op1.kind) &&
                   ((vnBased && (op1.vn == that->op1.vn)) || (!vnBased && (op1.lcl.lclNum == that->op1.lcl.lclNum)));
        }

        bool HasSameOp2(AssertionDsc* that, bool vnBased)
        {
            if (op2.kind != that->op2.kind)
            {
                return false;
            }
            switch (op2.kind)
            {
                case O2K_IND_CNS_INT:
                case O2K_CONST_INT:
                    return ((op2.u1.iconVal == that->op2.u1.iconVal) && (op2.u1.iconFlags == that->op2.u1.iconFlags));

                case O2K_CONST_LONG:
                    return (op2.lconVal == that->op2.lconVal);

                case O2K_CONST_DOUBLE:
                    // exact match because of positive and negative zero.
                    return (memcmp(&op2.dconVal, &that->op2.dconVal, sizeof(double)) == 0);

                case O2K_LCLVAR_COPY:
                case O2K_ARR_LEN:
                    return (op2.lcl.lclNum == that->op2.lcl.lclNum) &&
                           (!vnBased || op2.lcl.ssaNum == that->op2.lcl.ssaNum);

                case O2K_SUBRANGE:
                    return ((op2.u2.loBound == that->op2.u2.loBound) && (op2.u2.hiBound == that->op2.u2.hiBound));

                case O2K_INVALID:
                    // we will return false
                    break;

                default:
                    assert(!"Unexpected value for op2.kind in AssertionDsc.");
                    break;
            }
            return false;
        }

        bool Complementary(AssertionDsc* that, bool vnBased)
        {
            return ComplementaryKind(assertionKind, that->assertionKind) && HasSameOp1(that, vnBased) &&
                   HasSameOp2(that, vnBased);
        }

        bool Equals(AssertionDsc* that, bool vnBased)
        {
            return (assertionKind == that->assertionKind) && HasSameOp1(that, vnBased) && HasSameOp2(that, vnBased);
        }
    };

    typedef unsigned short AssertionIndex;

protected:
    static fgWalkPreFn optAddCopiesCallback;
    static fgWalkPreFn optVNAssertionPropCurStmtVisitor;
    unsigned           optAddCopyLclNum;
    GenTreePtr         optAddCopyAsgnNode;

    bool optLocalAssertionProp;  // indicates that we are performing local assertion prop
    bool optAssertionPropagated; // set to true if we modified the trees
    bool optAssertionPropagatedCurrentStmt;
#ifdef DEBUG
    GenTreePtr optAssertionPropCurrentTree;
#endif
    AssertionIndex*         optComplementaryAssertionMap;
    ExpandArray<ASSERT_TP>* optAssertionDep; // table that holds dependent assertions (assertions
                                             // using the value of a local var) for each local var
    AssertionDsc*  optAssertionTabPrivate;   // table that holds info about value assignments
    AssertionIndex optAssertionCount;        // total number of assertions in the assertion table
    AssertionIndex optMaxAssertionCount;

public:
    void optVnNonNullPropCurStmt(BasicBlock* block, GenTreePtr stmt, GenTreePtr tree);
    fgWalkResult optVNConstantPropCurStmt(BasicBlock* block, GenTreePtr stmt, GenTreePtr tree);
    GenTreePtr optVNConstantPropOnRelOp(GenTreePtr tree);
    GenTreePtr optVNConstantPropOnJTrue(BasicBlock* block, GenTreePtr stmt, GenTreePtr test);
    GenTreePtr optVNConstantPropOnTree(BasicBlock* block, GenTreePtr stmt, GenTreePtr tree);
    GenTreePtr optPrepareTreeForReplacement(GenTreePtr extractTree, GenTreePtr replaceTree);

    AssertionIndex GetAssertionCount()
    {
        return optAssertionCount;
    }
    ASSERT_TP* bbJtrueAssertionOut;
    typedef SimplerHashTable<ValueNum, SmallPrimitiveKeyFuncs<ValueNum>, ASSERT_TP, JitSimplerHashBehavior>
                          ValueNumToAssertsMap;
    ValueNumToAssertsMap* optValueNumToAsserts;

    static const AssertionIndex NO_ASSERTION_INDEX = 0;

    // Assertion prop helpers.
    ASSERT_TP& GetAssertionDep(unsigned lclNum);
    AssertionDsc* optGetAssertion(AssertionIndex assertIndex);
    void optAssertionInit(bool isLocalProp);
    void optAssertionTraitsInit(AssertionIndex assertionCount);
#if LOCAL_ASSERTION_PROP
    void optAssertionReset(AssertionIndex limit);
    void optAssertionRemove(AssertionIndex index);
#endif

    // Assertion prop data flow functions.
    void       optAssertionPropMain();
    GenTreePtr optVNAssertionPropCurStmt(BasicBlock* block, GenTreePtr stmt);
    bool optIsTreeKnownIntValue(bool vnBased, GenTreePtr tree, ssize_t* pConstant, unsigned* pIconFlags);
    ASSERT_TP* optInitAssertionDataflowFlags();
    ASSERT_TP* optComputeAssertionGen();

    // Assertion Gen functions.
    void optAssertionGen(GenTreePtr tree);
    AssertionIndex optAssertionGenPhiDefn(GenTreePtr tree);
    AssertionIndex optCreateJTrueBoundsAssertion(GenTreePtr tree);
    AssertionIndex optAssertionGenJtrue(GenTreePtr tree);
    AssertionIndex optCreateJtrueAssertions(GenTreePtr op1, GenTreePtr op2, Compiler::optAssertionKind assertionKind);
    AssertionIndex optFindComplementary(AssertionIndex assertionIndex);
    void optMapComplementary(AssertionIndex assertionIndex, AssertionIndex index);

    // Assertion creation functions.
    AssertionIndex optCreateAssertion(GenTreePtr op1, GenTreePtr op2, optAssertionKind assertionKind);
    AssertionIndex optCreateAssertion(GenTreePtr       op1,
                                      GenTreePtr       op2,
                                      optAssertionKind assertionKind,
                                      AssertionDsc*    assertion);
    void optCreateComplementaryAssertion(AssertionIndex assertionIndex, GenTreePtr op1, GenTreePtr op2);

    bool optAssertionVnInvolvesNan(AssertionDsc* assertion);
    AssertionIndex optAddAssertion(AssertionDsc* assertion);
    void optAddVnAssertionMapping(ValueNum vn, AssertionIndex index);
#ifdef DEBUG
    void optPrintVnAssertionMapping();
#endif
    ASSERT_TP optGetVnMappedAssertions(ValueNum vn);

    // Used for respective assertion propagations.
    AssertionIndex optAssertionIsSubrange(GenTreePtr tree, var_types toType, ASSERT_VALARG_TP assertions);
    AssertionIndex optAssertionIsSubtype(GenTreePtr tree, GenTreePtr methodTableArg, ASSERT_VALARG_TP assertions);
    AssertionIndex optAssertionIsNonNullInternal(GenTreePtr op, ASSERT_VALARG_TP assertions);
    bool optAssertionIsNonNull(GenTreePtr       op,
                               ASSERT_VALARG_TP assertions DEBUGARG(bool* pVnBased) DEBUGARG(AssertionIndex* pIndex));

    // Used for Relop propagation.
    AssertionIndex optGlobalAssertionIsEqualOrNotEqual(ASSERT_VALARG_TP assertions, GenTreePtr op1, GenTreePtr op2);
    AssertionIndex optLocalAssertionIsEqualOrNotEqual(
        optOp1Kind op1Kind, unsigned lclNum, optOp2Kind op2Kind, ssize_t cnsVal, ASSERT_VALARG_TP assertions);

    // Assertion prop for lcl var functions.
    bool optAssertionProp_LclVarTypeCheck(GenTreePtr tree, LclVarDsc* lclVarDsc, LclVarDsc* copyVarDsc);
    GenTreePtr optCopyAssertionProp(AssertionDsc* curAssertion,
                                    GenTreePtr    tree,
                                    GenTreePtr stmt DEBUGARG(AssertionIndex index));
    GenTreePtr optConstantAssertionProp(AssertionDsc*    curAssertion,
                                        const GenTreePtr tree,
                                        const GenTreePtr stmt DEBUGARG(AssertionIndex index));
    GenTreePtr optVnConstantAssertionProp(const GenTreePtr tree, const GenTreePtr stmt);

    // Assertion propagation functions.
    GenTreePtr optAssertionProp(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_LclVar(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_Ind(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_Cast(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_Call(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_RelOp(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_Comma(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_BndsChk(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionPropGlobal_RelOp(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionPropLocal_RelOp(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optAssertionProp_Update(const GenTreePtr newTree, const GenTreePtr tree, const GenTreePtr stmt);
    GenTreePtr optNonNullAssertionProp_Call(ASSERT_VALARG_TP assertions, const GenTreePtr tree, const GenTreePtr stmt);

    // Implied assertion functions.
    void optImpliedAssertions(AssertionIndex assertionIndex, ASSERT_TP& activeAssertions);
    void optImpliedByTypeOfAssertions(ASSERT_TP& activeAssertions);
    void optImpliedByCopyAssertion(AssertionDsc* copyAssertion, AssertionDsc* depAssertion, ASSERT_TP& result);
    void optImpliedByConstAssertion(AssertionDsc* curAssertion, ASSERT_TP& result);

    ASSERT_VALRET_TP optNewFullAssertSet();
    ASSERT_VALRET_TP optNewEmptyAssertSet();

#ifdef DEBUG
    void optPrintAssertion(AssertionDsc* newAssertion, AssertionIndex assertionIndex = 0);
    void optDebugCheckAssertion(AssertionDsc* assertion);
    void optDebugCheckAssertions(AssertionIndex AssertionIndex);
#endif
    void optAddCopies();
#endif // ASSERTION_PROP

    /**************************************************************************
     *                          Range checks
     *************************************************************************/

public:
    struct LoopCloneVisitorInfo
    {
        LoopCloneContext* context;
        unsigned          loopNum;
        GenTreePtr        stmt;
        LoopCloneVisitorInfo(LoopCloneContext* context, unsigned loopNum, GenTreePtr stmt)
            : context(context), loopNum(loopNum), stmt(nullptr)
        {
        }
    };

    bool optIsStackLocalInvariant(unsigned loopNum, unsigned lclNum);
    bool optExtractArrIndex(GenTreePtr tree, ArrIndex* result, unsigned lhsNum);
    bool optReconstructArrIndex(GenTreePtr tree, ArrIndex* result, unsigned lhsNum);
    bool optIdentifyLoopOptInfo(unsigned loopNum, LoopCloneContext* context);
    static fgWalkPreFn optCanOptimizeByLoopCloningVisitor;
    fgWalkResult optCanOptimizeByLoopCloning(GenTreePtr tree, LoopCloneVisitorInfo* info);
    void optObtainLoopCloningOpts(LoopCloneContext* context);
    bool optIsLoopClonable(unsigned loopInd);

    bool optCanCloneLoops();

#ifdef DEBUG
    void optDebugLogLoopCloning(BasicBlock* block, GenTreePtr insertBefore);
#endif
    void optPerformStaticOptimizations(unsigned loopNum, LoopCloneContext* context DEBUGARG(bool fastPath));
    bool optComputeDerefConditions(unsigned loopNum, LoopCloneContext* context);
    bool optDeriveLoopCloningConditions(unsigned loopNum, LoopCloneContext* context);
    BasicBlock* optInsertLoopChoiceConditions(LoopCloneContext* context,
                                              unsigned          loopNum,
                                              BasicBlock*       head,
                                              BasicBlock*       slow);
    void optInsertLoopCloningStress(BasicBlock* head);

#if COUNT_RANGECHECKS
    static unsigned optRangeChkRmv;
    static unsigned optRangeChkAll;
#endif

protected:
    struct arraySizes
    {
        unsigned arrayVar;
        int      arrayDim;

#define MAX_ARRAYS 4 // a magic max number of arrays tracked for bounds check elimination
    };

    struct RngChkDsc
    {
        RngChkDsc* rcdNextInBucket; // used by the hash table

        unsigned short rcdHashValue; // to make matching faster
        unsigned short rcdIndex;     // 0..optRngChkCount-1

        GenTreePtr rcdTree; // the array index tree
    };

    unsigned            optRngChkCount;
    static const size_t optRngChkHashSize;

    ssize_t optGetArrayRefScaleAndIndex(GenTreePtr mul, GenTreePtr* pIndex DEBUGARG(bool bRngChk));
    GenTreePtr optFindLocalInit(BasicBlock* block, GenTreePtr local, VARSET_TP* pKilledInOut, bool* isKilledAfterInit);

#if FANCY_ARRAY_OPT
    bool optIsNoMore(GenTreePtr op1, GenTreePtr op2, int add1 = 0, int add2 = 0);
#endif

    bool optReachWithoutCall(BasicBlock* srcBB, BasicBlock* dstBB);

protected:
    bool optLoopsMarked;

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           RegAlloc                                        XX
    XX                                                                           XX
    XX  Does the register allocation and puts the remaining lclVars on the stack XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
#ifndef LEGACY_BACKEND
    bool doLSRA() const
    {
        return true;
    }
#else  // LEGACY_BACKEND
    bool doLSRA() const
    {
        return false;
    }
#endif // LEGACY_BACKEND

#ifdef LEGACY_BACKEND
    void raInit();
    void raAssignVars(); // register allocation
#endif                   // LEGACY_BACKEND

    VARSET_TP raRegVarsMask; // Set of all enregistered variables (not including FEATURE_STACK_FP_X87 enregistered
                             // variables)
    regNumber raUpdateRegStateForArg(RegState* regState, LclVarDsc* argDsc);

    void raMarkStkVars();

protected:
    // Some things are used by both LSRA and regpredict allocators.

    FrameType rpFrameType;
    bool      rpMustCreateEBPCalled; // Set to true after we have called rpMustCreateEBPFrame once

#ifdef LEGACY_BACKEND
    regMaskTP rpMaskPInvokeEpilogIntf; // pinvoke epilog trashes esi/edi holding stack args needed to setup tail call's
                                       // args
#endif                                 // LEGACY_BACKEND

    bool rpMustCreateEBPFrame(INDEBUG(const char** wbReason));

#if FEATURE_FP_REGALLOC
    enum enumConfigRegisterFP
    {
        CONFIG_REGISTER_FP_NONE         = 0x0,
        CONFIG_REGISTER_FP_CALLEE_TRASH = 0x1,
        CONFIG_REGISTER_FP_CALLEE_SAVED = 0x2,
        CONFIG_REGISTER_FP_FULL         = 0x3,
    };
    enumConfigRegisterFP raConfigRegisterFP();
#endif // FEATURE_FP_REGALLOC

public:
    regMaskTP raConfigRestrictMaskFP();

private:
#ifndef LEGACY_BACKEND
    LinearScanInterface* m_pLinearScan; // Linear Scan allocator
#else                                   // LEGACY_BACKEND
    unsigned  raAvoidArgRegMask;       // Mask of incoming argument registers that we may need to avoid
    VARSET_TP raLclRegIntf[REG_COUNT]; // variable to register interference graph
    bool      raNewBlocks;             // True is we added killing blocks for FPU registers
    unsigned  rpPasses;                // Number of passes made by the register predicter
    unsigned  rpPassesMax;             // Maximum number of passes made by the register predicter
    unsigned  rpPassesPessimize;       // Number of passes non-pessimizing made by the register predicter
    unsigned rpStkPredict; // Weighted count of variables were predicted STK (lower means register allocation is better)
    unsigned rpPredictSpillCnt;     // Predicted number of integer spill tmps for the current tree
    regMaskTP rpPredictAssignMask;  // Mask of registers to consider in rpPredictAssignRegVars()
    VARSET_TP rpLastUseVars;        // Set of last use variables in rpPredictTreeRegUse
    VARSET_TP rpUseInPlace;         // Set of variables that we used in place
    int       rpAsgVarNum;          // VarNum for the target of GT_ASG node
    bool      rpPredictAssignAgain; // Must rerun the rpPredictAssignRegVars()
    bool      rpAddedVarIntf;       // Set to true if we need to add a new var intf
    bool      rpLostEnreg;          // Set to true if we lost an enregister var that had lvDependReg set
    bool      rpReverseEBPenreg;    // Decided to reverse the enregistration of EBP
public:
    bool rpRegAllocDone; // Set to true after we have completed register allocation
private:
    regMaskTP rpPredictMap[PREDICT_COUNT]; // Holds the regMaskTP for each of the enum values

    void raSetupArgMasks(RegState* r);

    const regNumber* raGetRegVarOrder(var_types regType, unsigned* wbVarOrderSize);
#ifdef DEBUG
    void raDumpVarIntf(); // Dump the variable to variable interference graph
    void raDumpRegIntf(); // Dump the variable to register interference graph
#endif
    void raAdjustVarIntf();

    regMaskTP rpPredictRegMask(rpPredictReg predictReg, var_types type);

    bool rpRecordRegIntf(regMaskTP regMask, VARSET_VALARG_TP life DEBUGARG(const char* msg));

    bool rpRecordVarIntf(unsigned varNum, VARSET_VALARG_TP intfVar DEBUGARG(const char* msg));
    regMaskTP rpPredictRegPick(var_types type, rpPredictReg predictReg, regMaskTP lockedRegs);

    regMaskTP rpPredictGrabReg(var_types type, rpPredictReg predictReg, regMaskTP lockedRegs);

    static fgWalkPreFn rpMarkRegIntf;

    regMaskTP rpPredictAddressMode(
        GenTreePtr tree, var_types type, regMaskTP lockedRegs, regMaskTP rsvdRegs, GenTreePtr lenCSE);

    void rpPredictRefAssign(unsigned lclNum);

    regMaskTP rpPredictBlkAsgRegUse(GenTreePtr tree, rpPredictReg predictReg, regMaskTP lockedRegs, regMaskTP rsvdRegs);

    regMaskTP rpPredictTreeRegUse(GenTreePtr tree, rpPredictReg predictReg, regMaskTP lockedRegs, regMaskTP rsvdRegs);

    regMaskTP rpPredictAssignRegVars(regMaskTP regAvail);

    void rpPredictRegUse(); // Entry point

    unsigned raPredictTreeRegUse(GenTreePtr tree);
    unsigned raPredictListRegUse(GenTreePtr list);

    void raSetRegVarOrder(var_types  regType,
                          regNumber* customVarOrder,
                          unsigned*  customVarOrderSize,
                          regMaskTP  prefReg,
                          regMaskTP  avoidReg);

    // We use (unsigned)-1 as an uninitialized sentinel for rpStkPredict and
    // also as the maximum value of lvRefCntWtd. Don't allow overflow, and
    // saturate at UINT_MAX - 1, to avoid using the sentinel.
    void raAddToStkPredict(unsigned val)
    {
        unsigned newStkPredict = rpStkPredict + val;
        if ((newStkPredict < rpStkPredict) || (newStkPredict == UINT_MAX))
            rpStkPredict = UINT_MAX - 1;
        else
            rpStkPredict = newStkPredict;
    }

#ifdef DEBUG
#if !FEATURE_FP_REGALLOC
    void raDispFPlifeInfo();
#endif
#endif

    regMaskTP genReturnRegForTree(GenTreePtr tree);
#endif // LEGACY_BACKEND

    /* raIsVarargsStackArg is called by raMaskStkVars and by
       lvaSortByRefCount.  It identifies the special case
       where a varargs function has a parameter passed on the
       stack, other than the special varargs handle.  Such parameters
       require special treatment, because they cannot be tracked
       by the GC (their offsets in the stack are not known
       at compile time).
    */

    bool raIsVarargsStackArg(unsigned lclNum)
    {
#ifdef _TARGET_X86_

        LclVarDsc* varDsc = &lvaTable[lclNum];

        assert(varDsc->lvIsParam);

        return (info.compIsVarArgs && !varDsc->lvIsRegArg && (lclNum != lvaVarargsHandleArg));

#else // _TARGET_X86_

        return false;

#endif // _TARGET_X86_
    }

#ifdef LEGACY_BACKEND
    // Records the current prediction, if it's better than any previous recorded prediction.
    void rpRecordPrediction();
    // Applies the best recorded prediction, if one exists and is better than the current prediction.
    void rpUseRecordedPredictionIfBetter();

    // Data members used in the methods above.
    unsigned rpBestRecordedStkPredict;
    struct VarRegPrediction
    {
        bool           m_isEnregistered;
        regNumberSmall m_regNum;
        regNumberSmall m_otherReg;
    };
    VarRegPrediction* rpBestRecordedPrediction;
#endif // LEGACY_BACKEND

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           EEInterface                                     XX
    XX                                                                           XX
    XX   Get to the class and method info from the Execution Engine given        XX
    XX   tokens for the class and method                                         XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    /* These are the different addressing modes used to access a local var.
     * The JIT has to report the location of the locals back to the EE
     * for debugging purposes.
     */

    enum siVarLocType
    {
        VLT_REG,
        VLT_REG_BYREF, // this type is currently only used for value types on X64
        VLT_REG_FP,
        VLT_STK,
        VLT_STK_BYREF, // this type is currently only used for value types on X64
        VLT_REG_REG,
        VLT_REG_STK,
        VLT_STK_REG,
        VLT_STK2,
        VLT_FPSTK,
        VLT_FIXED_VA,

        VLT_COUNT,
        VLT_INVALID
    };

    struct siVarLoc
    {
        siVarLocType vlType;

        union {
            // VLT_REG/VLT_REG_FP -- Any pointer-sized enregistered value (TYP_INT, TYP_REF, etc)
            // eg. EAX
            // VLT_REG_BYREF -- the specified register contains the address of the variable
            // eg. [EAX]

            struct
            {
                regNumber vlrReg;
            } vlReg;

            // VLT_STK       -- Any 32 bit value which is on the stack
            // eg. [ESP+0x20], or [EBP-0x28]
            // VLT_STK_BYREF -- the specified stack location contains the address of the variable
            // eg. mov EAX, [ESP+0x20]; [EAX]

            struct
            {
                regNumber     vlsBaseReg;
                NATIVE_OFFSET vlsOffset;
            } vlStk;

            // VLT_REG_REG -- TYP_LONG/TYP_DOUBLE with both DWords enregistered
            // eg. RBM_EAXEDX

            struct
            {
                regNumber vlrrReg1;
                regNumber vlrrReg2;
            } vlRegReg;

            // VLT_REG_STK -- Partly enregistered TYP_LONG/TYP_DOUBLE
            // eg { LowerDWord=EAX UpperDWord=[ESP+0x8] }

            struct
            {
                regNumber vlrsReg;

                struct
                {
                    regNumber     vlrssBaseReg;
                    NATIVE_OFFSET vlrssOffset;
                } vlrsStk;
            } vlRegStk;

            // VLT_STK_REG -- Partly enregistered TYP_LONG/TYP_DOUBLE
            // eg { LowerDWord=[ESP+0x8] UpperDWord=EAX }

            struct
            {
                struct
                {
                    regNumber     vlsrsBaseReg;
                    NATIVE_OFFSET vlsrsOffset;
                } vlsrStk;

                regNumber vlsrReg;
            } vlStkReg;

            // VLT_STK2 -- Any 64 bit value which is on the stack, in 2 successsive DWords
            // eg 2 DWords at [ESP+0x10]

            struct
            {
                regNumber     vls2BaseReg;
                NATIVE_OFFSET vls2Offset;
            } vlStk2;

            // VLT_FPSTK -- enregisterd TYP_DOUBLE (on the FP stack)
            // eg. ST(3). Actually it is ST("FPstkHeight - vpFpStk")

            struct
            {
                unsigned vlfReg;
            } vlFPstk;

            // VLT_FIXED_VA -- fixed argument of a varargs function.
            // The argument location depends on the size of the variable
            // arguments (...). Inspecting the VARARGS_HANDLE indicates the
            // location of the first arg. This argument can then be accessed
            // relative to the position of the first arg

            struct
            {
                unsigned vlfvOffset;
            } vlFixedVarArg;

            // VLT_MEMORY

            struct
            {
                void* rpValue; // pointer to the in-process
                               // location of the value.
            } vlMemory;
        };

        // Helper functions

        bool vlIsInReg(regNumber reg);
        bool vlIsOnStk(regNumber reg, signed offset);
    };

    /*************************************************************************/

public:
    // Get handles

    void eeGetCallInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                       CORINFO_RESOLVED_TOKEN* pConstrainedToken,
                       CORINFO_CALLINFO_FLAGS  flags,
                       CORINFO_CALL_INFO*      pResult);
    inline CORINFO_CALLINFO_FLAGS addVerifyFlag(CORINFO_CALLINFO_FLAGS flags);

    void eeGetFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                        CORINFO_ACCESS_FLAGS    flags,
                        CORINFO_FIELD_INFO*     pResult);

    // Get the flags

    BOOL eeIsValueClass(CORINFO_CLASS_HANDLE clsHnd);

#if defined(DEBUG) || defined(FEATURE_JIT_METHOD_PERF) || defined(FEATURE_SIMD)

    bool IsSuperPMIException(unsigned code)
    {
        // Copied from NDP\clr\src\ToolBox\SuperPMI\SuperPMI-Shared\ErrorHandling.h

        const unsigned EXCEPTIONCODE_DebugBreakorAV = 0xe0421000;
        const unsigned EXCEPTIONCODE_MC             = 0xe0422000;
        const unsigned EXCEPTIONCODE_LWM            = 0xe0423000;
        const unsigned EXCEPTIONCODE_SASM           = 0xe0424000;
        const unsigned EXCEPTIONCODE_SSYM           = 0xe0425000;
        const unsigned EXCEPTIONCODE_CALLUTILS      = 0xe0426000;
        const unsigned EXCEPTIONCODE_TYPEUTILS      = 0xe0427000;
        const unsigned EXCEPTIONCODE_ASSERT         = 0xe0440000;

        switch (code)
        {
            case EXCEPTIONCODE_DebugBreakorAV:
            case EXCEPTIONCODE_MC:
            case EXCEPTIONCODE_LWM:
            case EXCEPTIONCODE_SASM:
            case EXCEPTIONCODE_SSYM:
            case EXCEPTIONCODE_CALLUTILS:
            case EXCEPTIONCODE_TYPEUTILS:
            case EXCEPTIONCODE_ASSERT:
                return true;
            default:
                return false;
        }
    }

    const char* eeGetMethodName(CORINFO_METHOD_HANDLE hnd, const char** className);
    const char* eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd);

    bool eeIsNativeMethod(CORINFO_METHOD_HANDLE method);
    CORINFO_METHOD_HANDLE eeGetMethodHandleForNative(CORINFO_METHOD_HANDLE method);
#endif

    var_types eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig);
    var_types eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig, bool* isPinned);
    unsigned eeGetArgSize(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig);

    // VOM info, method sigs

    void eeGetSig(unsigned               sigTok,
                  CORINFO_MODULE_HANDLE  scope,
                  CORINFO_CONTEXT_HANDLE context,
                  CORINFO_SIG_INFO*      retSig);

    void eeGetCallSiteSig(unsigned               sigTok,
                          CORINFO_MODULE_HANDLE  scope,
                          CORINFO_CONTEXT_HANDLE context,
                          CORINFO_SIG_INFO*      retSig);

    void eeGetMethodSig(CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* retSig, CORINFO_CLASS_HANDLE owner = nullptr);

    // Method entry-points, instrs

    void* eeGetFieldAddress(CORINFO_FIELD_HANDLE handle, void*** ppIndir);

    CORINFO_METHOD_HANDLE eeMarkNativeTarget(CORINFO_METHOD_HANDLE method);

    CORINFO_EE_INFO eeInfo;
    bool            eeInfoInitialized;

    CORINFO_EE_INFO* eeGetEEInfo();

    // Gets the offset of a SDArray's first element
    unsigned eeGetArrayDataOffset(var_types type);
    // Gets the offset of a MDArray's first element
    unsigned eeGetMDArrayDataOffset(var_types type, unsigned rank);

    GenTreePtr eeGetPInvokeCookie(CORINFO_SIG_INFO* szMetaSig);

    // Returns the page size for the target machine as reported by the EE.
    inline size_t eeGetPageSize()
    {
#if COR_JIT_EE_VERSION > 460
        return eeGetEEInfo()->osPageSize;
#else  // COR_JIT_EE_VERSION <= 460
        return CORINFO_PAGE_SIZE;
#endif // COR_JIT_EE_VERSION > 460
    }

    // Returns the frame size at which we will generate a loop to probe the stack.
    inline size_t getVeryLargeFrameSize()
    {
#ifdef _TARGET_ARM_
        // The looping probe code is 40 bytes, whereas the straight-line probing for
        // the (0x2000..0x3000) case is 44, so use looping for anything 0x2000 bytes
        // or greater, to generate smaller code.
        return 2 * eeGetPageSize();
#else
        return 3 * eeGetPageSize();
#endif
    }

    inline bool generateCFIUnwindCodes()
    {
#if COR_JIT_EE_VERSION > 460 && defined(UNIX_AMD64_ABI)
        return eeGetEEInfo()->targetAbi == CORINFO_CORERT_ABI;
#else
        return false;
#endif
    }

    // Exceptions

    unsigned eeGetEHcount(CORINFO_METHOD_HANDLE handle);

    // Debugging support - Line number info

    void eeGetStmtOffsets();

    unsigned eeBoundariesCount;

    struct boundariesDsc
    {
        UNATIVE_OFFSET nativeIP;
        IL_OFFSET      ilOffset;
        unsigned       sourceReason;
    } * eeBoundaries; // Boundaries to report to EE
    void eeSetLIcount(unsigned count);
    void eeSetLIinfo(unsigned which, UNATIVE_OFFSET offs, unsigned srcIP, bool stkEmpty, bool callInstruction);
    void eeSetLIdone();

#ifdef DEBUG
    static void eeDispILOffs(IL_OFFSET offs);
    static void eeDispLineInfo(const boundariesDsc* line);
    void eeDispLineInfos();
#endif // DEBUG

    // Debugging support - Local var info

    void eeGetVars();

    unsigned eeVarsCount;

    struct VarResultInfo
    {
        UNATIVE_OFFSET startOffset;
        UNATIVE_OFFSET endOffset;
        DWORD          varNumber;
        siVarLoc       loc;
    } * eeVars;
    void eeSetLVcount(unsigned count);
    void eeSetLVinfo(unsigned        which,
                     UNATIVE_OFFSET  startOffs,
                     UNATIVE_OFFSET  length,
                     unsigned        varNum,
                     unsigned        LVnum,
                     VarName         namex,
                     bool            avail,
                     const siVarLoc& loc);
    void eeSetLVdone();

#ifdef DEBUG
    void eeDispVar(ICorDebugInfo::NativeVarInfo* var);
    void eeDispVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars, ICorDebugInfo::NativeVarInfo* vars);
#endif // DEBUG

    // ICorJitInfo wrappers

    void eeReserveUnwindInfo(BOOL isFunclet, BOOL isColdCode, ULONG unwindSize);

    void eeAllocUnwindInfo(BYTE*          pHotCode,
                           BYTE*          pColdCode,
                           ULONG          startOffset,
                           ULONG          endOffset,
                           ULONG          unwindSize,
                           BYTE*          pUnwindBlock,
                           CorJitFuncKind funcKind);

    void eeSetEHcount(unsigned cEH);

    void eeSetEHinfo(unsigned EHnumber, const CORINFO_EH_CLAUSE* clause);

    WORD eeGetRelocTypeHint(void* target);

    // ICorStaticInfo wrapper functions

    bool eeTryResolveToken(CORINFO_RESOLVED_TOKEN* resolvedToken);

#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
#ifdef DEBUG
    static void dumpSystemVClassificationType(SystemVClassificationType ct);
#endif // DEBUG

    void eeGetSystemVAmd64PassStructInRegisterDescriptor(
        /*IN*/ CORINFO_CLASS_HANDLE                                  structHnd,
        /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

    template <typename ParamType>
    bool eeRunWithErrorTrap(void (*function)(ParamType*), ParamType* param)
    {
        return eeRunWithErrorTrapImp(reinterpret_cast<void (*)(void*)>(function), reinterpret_cast<void*>(param));
    }

    bool eeRunWithErrorTrapImp(void (*function)(void*), void* param);

    // Utility functions

    const char* eeGetFieldName(CORINFO_FIELD_HANDLE fieldHnd, const char** classNamePtr = nullptr);

#if defined(DEBUG)
    const wchar_t* eeGetCPString(size_t stringHandle);
#endif

    const char* eeGetClassName(CORINFO_CLASS_HANDLE clsHnd);

    static CORINFO_METHOD_HANDLE eeFindHelper(unsigned helper);
    static CorInfoHelpFunc eeGetHelperNum(CORINFO_METHOD_HANDLE method);

    static fgWalkPreFn CountSharedStaticHelper;
    static bool IsSharedStaticHelper(GenTreePtr tree);
    static bool IsTreeAlwaysHoistable(GenTreePtr tree);

    static CORINFO_FIELD_HANDLE eeFindJitDataOffs(unsigned jitDataOffs);
    // returns true/false if 'field' is a Jit Data offset
    static bool eeIsJitDataOffs(CORINFO_FIELD_HANDLE field);
    // returns a number < 0 if 'field' is not a Jit Data offset, otherwise the data offset (limited to 2GB)
    static int eeGetJitDataOffs(CORINFO_FIELD_HANDLE field);

    /*****************************************************************************/

public:
    void tmpInit();

    enum TEMP_USAGE_TYPE
    {
        TEMP_USAGE_FREE,
        TEMP_USAGE_USED
    };

    static var_types tmpNormalizeType(var_types type);
    TempDsc* tmpGetTemp(var_types type); // get temp for the given type
    void tmpRlsTemp(TempDsc* temp);
    TempDsc* tmpFindNum(int temp, TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;

    void     tmpEnd();
    TempDsc* tmpListBeg(TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;
    TempDsc* tmpListNxt(TempDsc* curTemp, TEMP_USAGE_TYPE usageType = TEMP_USAGE_FREE) const;
    void tmpDone();

#ifdef DEBUG
    bool tmpAllFree() const;
#endif // DEBUG

#ifndef LEGACY_BACKEND
    void tmpPreAllocateTemps(var_types type, unsigned count);
#endif // !LEGACY_BACKEND

protected:
#ifdef LEGACY_BACKEND
    unsigned tmpIntSpillMax;    // number of int-sized spill temps
    unsigned tmpDoubleSpillMax; // number of double-sized spill temps
#endif                          // LEGACY_BACKEND

    unsigned tmpCount; // Number of temps
    unsigned tmpSize;  // Size of all the temps
#ifdef DEBUG
public:
    // Used by RegSet::rsSpillChk()
    unsigned tmpGetCount; // Temps which haven't been released yet
#endif
private:
    static unsigned tmpSlot(unsigned size); // which slot in tmpFree[] or tmpUsed[] to use

    TempDsc* tmpFree[TEMP_MAX_SIZE / sizeof(int)];
    TempDsc* tmpUsed[TEMP_MAX_SIZE / sizeof(int)];

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           CodeGenerator                                   XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    CodeGenInterface* codeGen;

#ifdef DEBUGGING_SUPPORT

    //  The following holds information about instr offsets in terms of generated code.

    struct IPmappingDsc
    {
        IPmappingDsc* ipmdNext;      // next line# record
        IL_OFFSETX    ipmdILoffsx;   // the instr offset
        emitLocation  ipmdNativeLoc; // the emitter location of the native code corresponding to the IL offset
        bool          ipmdIsLabel;   // Can this code be a branch label?
    };

    // Record the instr offset mapping to the generated code

    IPmappingDsc* genIPmappingList;
    IPmappingDsc* genIPmappingLast;

    // Managed RetVal - A side hash table meant to record the mapping from a
    // GT_CALL node to its IL offset.  This info is used to emit sequence points
    // that can be used by debugger to determine the native offset at which the
    // managed RetVal will be available.
    //
    // In fact we can store IL offset in a GT_CALL node.  This was ruled out in
    // favor of a side table for two reasons: 1) We need IL offset for only those
    // GT_CALL nodes (created during importation) that correspond to an IL call and
    // whose return type is other than TYP_VOID. 2) GT_CALL node is a frequently used
    // structure and IL offset is needed only when generating debuggable code. Therefore
    // it is desirable to avoid memory size penalty in retail scenarios.
    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, IL_OFFSETX, JitSimplerHashBehavior>
                           CallSiteILOffsetTable;
    CallSiteILOffsetTable* genCallSite2ILOffsetMap;
#endif // DEBUGGING_SUPPORT

    unsigned    genReturnLocal; // Local number for the return value when applicable.
    BasicBlock* genReturnBB;    // jumped to when not optimizing for speed.

    // The following properties are part of CodeGenContext.  Getters are provided here for
    // convenience and backward compatibility, but the properties can only be set by invoking
    // the setter on CodeGenContext directly.

    __declspec(property(get = getEmitter)) emitter* genEmitter;
    emitter* getEmitter()
    {
        return codeGen->getEmitter();
    }

    const bool isFramePointerUsed()
    {
        return codeGen->isFramePointerUsed();
    }

    __declspec(property(get = getInterruptible, put = setInterruptible)) bool genInterruptible;
    bool getInterruptible()
    {
        return codeGen->genInterruptible;
    }
    void setInterruptible(bool value)
    {
        codeGen->setInterruptible(value);
    }

#if DOUBLE_ALIGN
    const bool genDoubleAlign()
    {
        return codeGen->doDoubleAlign();
    }
    DWORD getCanDoubleAlign(); // Defined & used only by RegAlloc
#endif                         // DOUBLE_ALIGN
    __declspec(property(get = getFullPtrRegMap, put = setFullPtrRegMap)) bool genFullPtrRegMap;
    bool getFullPtrRegMap()
    {
        return codeGen->genFullPtrRegMap;
    }
    void setFullPtrRegMap(bool value)
    {
        codeGen->setFullPtrRegMap(value);
    }

// Things that MAY belong either in CodeGen or CodeGenContext

#if FEATURE_EH_FUNCLETS
    FuncInfoDsc*   compFuncInfos;
    unsigned short compCurrFuncIdx;
    unsigned short compFuncInfoCount;

    unsigned short compFuncCount()
    {
        assert(fgFuncletsCreated);
        return compFuncInfoCount;
    }

#else // !FEATURE_EH_FUNCLETS

    // This is a no-op when there are no funclets!
    void genUpdateCurrentFunclet(BasicBlock* block)
    {
        return;
    }

    FuncInfoDsc compFuncInfoRoot;

    static const unsigned compCurrFuncIdx = 0;

    unsigned short compFuncCount()
    {
        return 1;
    }

#endif // !FEATURE_EH_FUNCLETS

    FuncInfoDsc* funCurrentFunc();
    void funSetCurrentFunc(unsigned funcIdx);
    FuncInfoDsc* funGetFunc(unsigned funcIdx);
    unsigned int funGetFuncIdx(BasicBlock* block);

    // LIVENESS

    VARSET_TP  compCurLife;     // current live variables
    GenTreePtr compCurLifeTree; // node after which compCurLife has been computed

    template <bool ForCodeGen>
    void compChangeLife(VARSET_VALARG_TP newLife DEBUGARG(GenTreePtr tree));

    void genChangeLife(VARSET_VALARG_TP newLife DEBUGARG(GenTreePtr tree))
    {
        compChangeLife</*ForCodeGen*/ true>(newLife DEBUGARG(tree));
    }

    template <bool ForCodeGen>
    void compUpdateLife(GenTreePtr tree);

    // Updates "compCurLife" to its state after evaluate of "true".  If "pLastUseVars" is
    // non-null, sets "*pLastUseVars" to the set of tracked variables for which "tree" was a last
    // use.  (Can be more than one var in the case of dependently promoted struct vars.)
    template <bool ForCodeGen>
    void compUpdateLifeVar(GenTreePtr tree, VARSET_TP* pLastUseVars = nullptr);

    template <bool ForCodeGen>
    inline void compUpdateLife(VARSET_VALARG_TP newLife);

    // Gets a register mask that represent the kill set for a helper call since
    // not all JIT Helper calls follow the standard ABI on the target architecture.
    regMaskTP compHelperCallKillSet(CorInfoHelpFunc helper);

    // Gets a register mask that represent the kill set for a NoGC helper call.
    regMaskTP compNoGCHelperCallKillSet(CorInfoHelpFunc helper);

#ifdef _TARGET_ARM_
    // Requires that "varDsc" be a promoted struct local variable being passed as an argument, beginning at
    // "firstArgRegNum", which is assumed to have already been aligned to the register alignment restriction of the
    // struct type. Adds bits to "*pArgSkippedRegMask" for any argument registers *not* used in passing "varDsc" --
    // i.e., internal "holes" caused by internal alignment constraints.  For example, if the struct contained an int and
    // a double, and we at R0 (on ARM), then R1 would be skipped, and the bit for R1 would be added to the mask.
    void fgAddSkippedRegsInPromotedStructArg(LclVarDsc* varDsc, unsigned firstArgRegNum, regMaskTP* pArgSkippedRegMask);
#endif // _TARGET_ARM_

    // If "tree" is a indirection (GT_IND, or GT_OBJ) whose arg is an ADDR, whose arg is a LCL_VAR, return that LCL_VAR
    // node, else NULL.
    static GenTreePtr fgIsIndirOfAddrOfLocal(GenTreePtr tree);

    // This is indexed by GT_OBJ nodes that are address of promoted struct variables, which
    // have been annotated with the GTF_VAR_DEATH flag.  If such a node is *not* mapped in this
    // table, one may assume that all the (tracked) field vars die at this point.  Otherwise,
    // the node maps to a pointer to a VARSET_TP, containing set bits for each of the tracked field
    // vars of the promoted struct local that go dead at the given node (the set bits are the bits
    // for the tracked var indices of the field vars, as in a live var set).
    NodeToVarsetPtrMap* m_promotedStructDeathVars;

    NodeToVarsetPtrMap* GetPromotedStructDeathVars()
    {
        if (m_promotedStructDeathVars == nullptr)
        {
            m_promotedStructDeathVars = new (getAllocator()) NodeToVarsetPtrMap(getAllocator());
        }
        return m_promotedStructDeathVars;
    }

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                           UnwindInfo                                      XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#if !defined(__GNUC__)
#pragma region Unwind information
#endif

public:
    //
    // Infrastructure functions: start/stop/reserve/emit.
    //

    void unwindBegProlog();
    void unwindEndProlog();
    void unwindBegEpilog();
    void unwindEndEpilog();
    void unwindReserve();
    void unwindEmit(void* pHotCode, void* pColdCode);

    //
    // Specific unwind information functions: called by code generation to indicate a particular
    // prolog or epilog unwindable instruction has been generated.
    //

    void unwindPush(regNumber reg);
    void unwindAllocStack(unsigned size);
    void unwindSetFrameReg(regNumber reg, unsigned offset);
    void unwindSaveReg(regNumber reg, unsigned offset);

#if defined(_TARGET_ARM_)
    void unwindPushMaskInt(regMaskTP mask);
    void unwindPushMaskFloat(regMaskTP mask);
    void unwindPopMaskInt(regMaskTP mask);
    void unwindPopMaskFloat(regMaskTP mask);
    void unwindBranch16();                    // The epilog terminates with a 16-bit branch (e.g., "bx lr")
    void unwindNop(unsigned codeSizeInBytes); // Generate unwind NOP code. 'codeSizeInBytes' is 2 or 4 bytes. Only
                                              // called via unwindPadding().
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
#endif                    // _TARGET_ARM_

#if defined(_TARGET_ARM64_)
    void unwindNop();
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
    void unwindSaveReg(regNumber reg, int offset);                                // str reg, [sp, #offset]
    void unwindSaveRegPreindexed(regNumber reg, int offset);                      // str reg, [sp, #offset]!
    void unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset);           // stp reg1, reg2, [sp, #offset]
    void unwindSaveRegPairPreindexed(regNumber reg1, regNumber reg2, int offset); // stp reg1, reg2, [sp, #offset]!
    void unwindSaveNext();                                                        // unwind code: save_next
    void unwindReturn(regNumber reg);                                             // ret lr
#endif                                                                            // defined(_TARGET_ARM64_)

    //
    // Private "helper" functions for the unwind implementation.
    //

private:
#if FEATURE_EH_FUNCLETS
    void unwindGetFuncLocations(FuncInfoDsc*             func,
                                bool                     getHotSectionData,
                                /* OUT */ emitLocation** ppStartLoc,
                                /* OUT */ emitLocation** ppEndLoc);
#endif // FEATURE_EH_FUNCLETS

    void unwindReserveFunc(FuncInfoDsc* func);
    void unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode);

#if defined(_TARGET_AMD64_)

    void unwindReserveFuncHelper(FuncInfoDsc* func, bool isHotCode);
    void unwindEmitFuncHelper(FuncInfoDsc* func, void* pHotCode, void* pColdCode, bool isHotCode);
    UNATIVE_OFFSET unwindGetCurrentOffset(FuncInfoDsc* func);

    void unwindBegPrologWindows();
    void unwindPushWindows(regNumber reg);
    void unwindAllocStackWindows(unsigned size);
    void unwindSetFrameRegWindows(regNumber reg, unsigned offset);
    void unwindSaveRegWindows(regNumber reg, unsigned offset);

#ifdef UNIX_AMD64_ABI
    void unwindBegPrologCFI();
    void unwindPushCFI(regNumber reg);
    void unwindAllocStackCFI(unsigned size);
    void unwindSetFrameRegCFI(regNumber reg, unsigned offset);
    void unwindSaveRegCFI(regNumber reg, unsigned offset);
    int mapRegNumToDwarfReg(regNumber reg);
    void createCfiCode(FuncInfoDsc* func, UCHAR codeOffset, UCHAR opcode, USHORT dwarfReg, INT offset = 0);
#endif // UNIX_AMD64_ABI
#elif defined(_TARGET_ARM_)

    void unwindPushPopMaskInt(regMaskTP mask, bool useOpsize16);
    void unwindPushPopMaskFloat(regMaskTP mask);
    void unwindSplit(FuncInfoDsc* func);

#endif // _TARGET_ARM_

#if !defined(__GNUC__)
#pragma endregion // Note: region is NOT under !defined(__GNUC__)
#endif

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                               SIMD                                        XX
    XX                                                                           XX
    XX   Info about SIMD types, methods and the SIMD assembly (i.e. the assembly XX
    XX   that contains the distinguished, well-known SIMD type definitions).     XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

    // Get highest available instruction set for floating point codegen
    InstructionSet getFloatingPointInstructionSet()
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        if (canUseAVX())
        {
            return InstructionSet_AVX;
        }

        // min bar is SSE2
        assert(canUseSSE2());
        return InstructionSet_SSE2;
#else
        assert(!"getFPInstructionSet() is not implemented for target arch");
        unreached();
        return InstructionSet_NONE;
#endif
    }

    // Get highest available instruction set for SIMD codegen
    InstructionSet getSIMDInstructionSet()
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        return getFloatingPointInstructionSet();
#else
        assert(!"Available instruction set(s) for SIMD codegen is not defined for target arch");
        unreached();
        return InstructionSet_NONE;
#endif
    }

#ifdef FEATURE_SIMD

    // Should we support SIMD intrinsics?
    bool featureSIMD;

    // This is a temp lclVar allocated on the stack as TYP_SIMD.  It is used to implement intrinsics
    // that require indexed access to the individual fields of the vector, which is not well supported
    // by the hardware.  It is allocated when/if such situations are encountered during Lowering.
    unsigned lvaSIMDInitTempVarNum;

    // SIMD Types
    CORINFO_CLASS_HANDLE SIMDFloatHandle;
    CORINFO_CLASS_HANDLE SIMDDoubleHandle;
    CORINFO_CLASS_HANDLE SIMDIntHandle;
    CORINFO_CLASS_HANDLE SIMDUShortHandle;
    CORINFO_CLASS_HANDLE SIMDUByteHandle;
    CORINFO_CLASS_HANDLE SIMDShortHandle;
    CORINFO_CLASS_HANDLE SIMDByteHandle;
    CORINFO_CLASS_HANDLE SIMDLongHandle;
    CORINFO_CLASS_HANDLE SIMDUIntHandle;
    CORINFO_CLASS_HANDLE SIMDULongHandle;
    CORINFO_CLASS_HANDLE SIMDVector2Handle;
    CORINFO_CLASS_HANDLE SIMDVector3Handle;
    CORINFO_CLASS_HANDLE SIMDVector4Handle;
    CORINFO_CLASS_HANDLE SIMDVectorHandle;

    // Get the handle for a SIMD type.
    CORINFO_CLASS_HANDLE gtGetStructHandleForSIMD(var_types simdType, var_types simdBaseType)
    {
        if (simdBaseType == TYP_FLOAT)
        {
            switch (simdType)
            {
                case TYP_SIMD8:
                    return SIMDVector2Handle;
                case TYP_SIMD12:
                    return SIMDVector3Handle;
                case TYP_SIMD16:
                    if ((getSIMDVectorType() == TYP_SIMD32) || (SIMDVector4Handle != NO_CLASS_HANDLE))
                    {
                        return SIMDVector4Handle;
                    }
                    break;
                case TYP_SIMD32:
                    break;
                default:
                    unreached();
            }
        }
        assert(simdType == getSIMDVectorType());
        switch (simdBaseType)
        {
            case TYP_FLOAT:
                return SIMDFloatHandle;
            case TYP_DOUBLE:
                return SIMDDoubleHandle;
            case TYP_INT:
                return SIMDIntHandle;
            case TYP_CHAR:
                return SIMDUShortHandle;
            case TYP_USHORT:
                return SIMDUShortHandle;
            case TYP_UBYTE:
                return SIMDUByteHandle;
            case TYP_SHORT:
                return SIMDShortHandle;
            case TYP_BYTE:
                return SIMDByteHandle;
            case TYP_LONG:
                return SIMDLongHandle;
            case TYP_UINT:
                return SIMDUIntHandle;
            case TYP_ULONG:
                return SIMDULongHandle;
            default:
                assert(!"Didn't find a class handle for simdType");
        }
        return NO_CLASS_HANDLE;
    }

    // SIMD Methods
    CORINFO_METHOD_HANDLE SIMDVectorFloat_set_Item;
    CORINFO_METHOD_HANDLE SIMDVectorFloat_get_Length;
    CORINFO_METHOD_HANDLE SIMDVectorFloat_op_Addition;

    // Returns true if the tree corresponds to a TYP_SIMD lcl var.
    // Note that both SIMD vector args and locals are mared as lvSIMDType = true, but
    // type of an arg node is TYP_BYREF and a local node is TYP_SIMD or TYP_STRUCT.
    bool isSIMDTypeLocal(GenTree* tree)
    {
        return tree->OperIsLocal() && lvaTable[tree->AsLclVarCommon()->gtLclNum].lvSIMDType;
    }

    // Returns true if the type of the tree is a byref of TYP_SIMD
    bool isAddrOfSIMDType(GenTree* tree)
    {
        if (tree->TypeGet() == TYP_BYREF || tree->TypeGet() == TYP_I_IMPL)
        {
            switch (tree->OperGet())
            {
                case GT_ADDR:
                    return varTypeIsSIMD(tree->gtGetOp1());

                case GT_LCL_VAR_ADDR:
                    return lvaTable[tree->AsLclVarCommon()->gtLclNum].lvSIMDType;

                default:
                    return isSIMDTypeLocal(tree);
            }
        }

        return false;
    }

    static bool isRelOpSIMDIntrinsic(SIMDIntrinsicID intrinsicId)
    {
        return (intrinsicId == SIMDIntrinsicEqual || intrinsicId == SIMDIntrinsicLessThan ||
                intrinsicId == SIMDIntrinsicLessThanOrEqual || intrinsicId == SIMDIntrinsicGreaterThan ||
                intrinsicId == SIMDIntrinsicGreaterThanOrEqual);
    }

    // Returns base type of a TYP_SIMD local.
    // Returns TYP_UNKNOWN if the local is not TYP_SIMD.
    var_types getBaseTypeOfSIMDLocal(GenTree* tree)
    {
        if (isSIMDTypeLocal(tree))
        {
            return lvaTable[tree->AsLclVarCommon()->gtLclNum].lvBaseType;
        }

        return TYP_UNKNOWN;
    }

    bool isSIMDClass(CORINFO_CLASS_HANDLE clsHnd)
    {
        return info.compCompHnd->isInSIMDModule(clsHnd);
    }

    bool isSIMDClass(typeInfo* pTypeInfo)
    {
        return pTypeInfo->IsStruct() && isSIMDClass(pTypeInfo->GetClassHandleForValueClass());
    }

    // Get the base (element) type and size in bytes for a SIMD type. Returns TYP_UNKNOWN
    // if it is not a SIMD type or is an unsupported base type.
    var_types getBaseTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd, unsigned* sizeBytes = nullptr);

    var_types getBaseTypeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd)
    {
        return getBaseTypeAndSizeOfSIMDType(typeHnd, nullptr);
    }

    // Get SIMD Intrinsic info given the method handle.
    // Also sets typeHnd, argCount, baseType and sizeBytes out params.
    const SIMDIntrinsicInfo* getSIMDIntrinsicInfo(CORINFO_CLASS_HANDLE* typeHnd,
                                                  CORINFO_METHOD_HANDLE methodHnd,
                                                  CORINFO_SIG_INFO*     sig,
                                                  bool                  isNewObj,
                                                  unsigned*             argCount,
                                                  var_types*            baseType,
                                                  unsigned*             sizeBytes);

    // Pops and returns GenTree node from importers type stack.
    // Normalizes TYP_STRUCT value in case of GT_CALL, GT_RET_EXPR and arg nodes.
    GenTreePtr impSIMDPopStack(var_types type, bool expectAddr = false);

    // Create a GT_SIMD tree for a Get property of SIMD vector with a fixed index.
    GenTreeSIMD* impSIMDGetFixed(var_types simdType, var_types baseType, unsigned simdSize, int index);

    // Creates a GT_SIMD tree for Select operation
    GenTreePtr impSIMDSelect(CORINFO_CLASS_HANDLE typeHnd,
                             var_types            baseType,
                             unsigned             simdVectorSize,
                             GenTree*             op1,
                             GenTree*             op2,
                             GenTree*             op3);

    // Creates a GT_SIMD tree for Min/Max operation
    GenTreePtr impSIMDMinMax(SIMDIntrinsicID      intrinsicId,
                             CORINFO_CLASS_HANDLE typeHnd,
                             var_types            baseType,
                             unsigned             simdVectorSize,
                             GenTree*             op1,
                             GenTree*             op2);

    // Transforms operands and returns the SIMD intrinsic to be applied on
    // transformed operands to obtain given relop result.
    SIMDIntrinsicID impSIMDRelOp(SIMDIntrinsicID      relOpIntrinsicId,
                                 CORINFO_CLASS_HANDLE typeHnd,
                                 unsigned             simdVectorSize,
                                 var_types*           baseType,
                                 GenTree**            op1,
                                 GenTree**            op2);

#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
    // Transforms operands and returns the SIMD intrinsic to be applied on
    // transformed operands to obtain == comparison result.
    SIMDIntrinsicID impSIMDLongRelOpEqual(CORINFO_CLASS_HANDLE typeHnd,
                                          unsigned             simdVectorSize,
                                          GenTree**            op1,
                                          GenTree**            op2);

    // Transforms operands and returns the SIMD intrinsic to be applied on
    // transformed operands to obtain > comparison result.
    SIMDIntrinsicID impSIMDLongRelOpGreaterThan(CORINFO_CLASS_HANDLE typeHnd,
                                                unsigned             simdVectorSize,
                                                GenTree**            op1,
                                                GenTree**            op2);

    // Transforms operands and returns the SIMD intrinsic to be applied on
    // transformed operands to obtain >= comparison result.
    SIMDIntrinsicID impSIMDLongRelOpGreaterThanOrEqual(CORINFO_CLASS_HANDLE typeHnd,
                                                       unsigned             simdVectorSize,
                                                       GenTree**            op1,
                                                       GenTree**            op2);

    // Transforms operands and returns the SIMD intrinsic to be applied on
    // transformed operands to obtain >= comparison result in case of int32
    // and small int base type vectors.
    SIMDIntrinsicID impSIMDIntegralRelOpGreaterThanOrEqual(
        CORINFO_CLASS_HANDLE typeHnd, unsigned simdVectorSize, var_types baseType, GenTree** op1, GenTree** op2);
#endif // defined(_TARGET_AMD64_) && !defined(LEGACY_BACKEND)

    void setLclRelatedToSIMDIntrinsic(GenTreePtr tree);
    bool areFieldsContiguous(GenTreePtr op1, GenTreePtr op2);
    bool areArrayElementsContiguous(GenTreePtr op1, GenTreePtr op2);
    bool areArgumentsContiguous(GenTreePtr op1, GenTreePtr op2);
    GenTreePtr createAddressNodeForSIMDInit(GenTreePtr tree, unsigned simdSize);

    // check methodHnd to see if it is a SIMD method that is expanded as an intrinsic in the JIT.
    GenTreePtr impSIMDIntrinsic(OPCODE                opcode,
                                GenTreePtr            newobjThis,
                                CORINFO_CLASS_HANDLE  clsHnd,
                                CORINFO_METHOD_HANDLE method,
                                CORINFO_SIG_INFO*     sig,
                                int                   memberRef);

    GenTreePtr getOp1ForConstructor(OPCODE opcode, GenTreePtr newobjThis, CORINFO_CLASS_HANDLE clsHnd);

    // Whether SIMD vector occupies part of SIMD register.
    // SSE2: vector2f/3f are considered sub register SIMD types.
    // AVX: vector2f, 3f and 4f are all considered sub register SIMD types.
    bool isSubRegisterSIMDType(CORINFO_CLASS_HANDLE typeHnd)
    {
        unsigned  sizeBytes = 0;
        var_types baseType  = getBaseTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
        return (baseType == TYP_FLOAT) && (sizeBytes < getSIMDVectorRegisterByteLength());
    }

    bool isSubRegisterSIMDType(GenTreeSIMD* simdNode)
    {
        return (simdNode->gtSIMDSize < getSIMDVectorRegisterByteLength());
    }

    // Get the type for the hardware SIMD vector.
    // This is the maximum SIMD type supported for this target.
    var_types getSIMDVectorType()
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        if (canUseAVX())
        {
            return TYP_SIMD32;
        }
        else
        {
            assert(canUseSSE2());
            return TYP_SIMD16;
        }
#else
        assert(!"getSIMDVectorType() unimplemented on target arch");
        unreached();
#endif
    }

    // Get the size of the SIMD type in bytes
    int getSIMDTypeSizeInBytes(CORINFO_CLASS_HANDLE typeHnd)
    {
        unsigned sizeBytes = 0;
        (void)getBaseTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
        return sizeBytes;
    }

    // Get the the number of elements of basetype of SIMD vector given by its size and baseType
    static int getSIMDVectorLength(unsigned simdSize, var_types baseType);

    // Get the the number of elements of basetype of SIMD vector given by its type handle
    int getSIMDVectorLength(CORINFO_CLASS_HANDLE typeHnd);

    // Get preferred alignment of SIMD type.
    int getSIMDTypeAlignment(var_types simdType);

    // Get the number of bytes in a SIMD Vector for the current compilation.
    unsigned getSIMDVectorRegisterByteLength()
    {
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        if (canUseAVX())
        {
            return YMM_REGSIZE_BYTES;
        }
        else
        {
            assert(canUseSSE2());
            return XMM_REGSIZE_BYTES;
        }
#else
        assert(!"getSIMDVectorRegisterByteLength() unimplemented on target arch");
        unreached();
#endif
    }

    // The minimum and maximum possible number of bytes in a SIMD vector.
    unsigned int maxSIMDStructBytes()
    {
        return getSIMDVectorRegisterByteLength();
    }
    unsigned int minSIMDStructBytes()
    {
        return emitTypeSize(TYP_SIMD8);
    }

#ifdef FEATURE_AVX_SUPPORT
    // (maxPossibleSIMDStructBytes is for use in a context that requires a compile-time constant.)
    static const unsigned maxPossibleSIMDStructBytes = 32;
#else  // !FEATURE_AVX_SUPPORT
    static const unsigned maxPossibleSIMDStructBytes = 16;
#endif // !FEATURE_AVX_SUPPORT

    // Returns the codegen type for a given SIMD size.
    var_types getSIMDTypeForSize(unsigned size)
    {
        var_types simdType = TYP_UNDEF;
        if (size == 8)
        {
            simdType = TYP_SIMD8;
        }
        else if (size == 12)
        {
            simdType = TYP_SIMD12;
        }
        else if (size == 16)
        {
            simdType = TYP_SIMD16;
        }
#ifdef FEATURE_AVX_SUPPORT
        else if (size == 32)
        {
            simdType = TYP_SIMD32;
        }
#endif // FEATURE_AVX_SUPPORT
        else
        {
            noway_assert(!"Unexpected size for SIMD type");
        }
        return simdType;
    }

    unsigned getSIMDInitTempVarNum()
    {
        if (lvaSIMDInitTempVarNum == BAD_VAR_NUM)
        {
            lvaSIMDInitTempVarNum                  = lvaGrabTempWithImplicitUse(false DEBUGARG("SIMDInitTempVar"));
            lvaTable[lvaSIMDInitTempVarNum].lvType = getSIMDVectorType();
        }
        return lvaSIMDInitTempVarNum;
    }

#endif // FEATURE_SIMD

public:
    //------------------------------------------------------------------------
    // largestEnregisterableStruct: The size in bytes of the largest struct that can be enregistered.
    //
    // Notes: It is not guaranteed that the struct of this size or smaller WILL be a
    //        candidate for enregistration.

    unsigned largestEnregisterableStructSize()
    {
#ifdef FEATURE_SIMD
        unsigned vectorRegSize = getSIMDVectorRegisterByteLength();
        if (vectorRegSize > TARGET_POINTER_SIZE)
        {
            return vectorRegSize;
        }
        else
#endif // FEATURE_SIMD
        {
            return TARGET_POINTER_SIZE;
        }
    }

private:
    // These routines need not be enclosed under FEATURE_SIMD since lvIsSIMDType()
    // is defined for both FEATURE_SIMD and !FEATURE_SIMD apropriately. The use
    // of this routines also avoids the need of #ifdef FEATURE_SIMD specific code.

    // Is this var is of type simd struct?
    bool lclVarIsSIMDType(unsigned varNum)
    {
        LclVarDsc* varDsc = lvaTable + varNum;
        return varDsc->lvIsSIMDType();
    }

    // Is this Local node a SIMD local?
    bool lclVarIsSIMDType(GenTreeLclVarCommon* lclVarTree)
    {
        return lclVarIsSIMDType(lclVarTree->gtLclNum);
    }

    // Returns true if the TYP_SIMD locals on stack are aligned at their
    // preferred byte boundary specified by getSIMDTypeAlignment().
    bool isSIMDTypeLocalAligned(unsigned varNum)
    {
#if defined(FEATURE_SIMD) && ALIGN_SIMD_TYPES
        if (lclVarIsSIMDType(varNum) && lvaTable[varNum].lvType != TYP_BYREF)
        {
            bool ebpBased;
            int  off = lvaFrameAddress(varNum, &ebpBased);
            // TODO-Cleanup: Can't this use the lvExactSize on the varDsc?
            int  alignment = getSIMDTypeAlignment(lvaTable[varNum].lvType);
            bool isAligned = ((off % alignment) == 0);
            noway_assert(isAligned || lvaTable[varNum].lvIsParam);
            return isAligned;
        }
#endif // FEATURE_SIMD

        return false;
    }

    // Whether SSE2 is available
    bool canUseSSE2() const
    {
#ifdef _TARGET_XARCH_
        return opts.compCanUseSSE2;
#else
        return false;
#endif
    }

    bool canUseAVX() const
    {
#ifdef FEATURE_AVX_SUPPORT
        return opts.compCanUseAVX;
#else
        return false;
#endif
    }

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           Compiler                                        XX
    XX                                                                           XX
    XX   Generic info about the compilation and the method being compiled.       XX
    XX   It is responsible for driving the other phases.                         XX
    XX   It is also responsible for all the memory management.                   XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    Compiler* InlineeCompiler; // The Compiler instance for the inlinee

    InlineResult* compInlineResult; // The result of importing the inlinee method.

    bool compDoAggressiveInlining; // If true, mark every method as CORINFO_FLG_FORCEINLINE
    bool compJmpOpUsed;            // Does the method do a JMP
    bool compLongUsed;             // Does the method use TYP_LONG
    bool compFloatingPointUsed;    // Does the method use TYP_FLOAT or TYP_DOUBLE
    bool compTailCallUsed;         // Does the method do a tailcall
    bool compLocallocUsed;         // Does the method use localloc.
    bool compQmarkUsed;            // Does the method use GT_QMARK/GT_COLON
    bool compQmarkRationalized;    // Is it allowed to use a GT_QMARK/GT_COLON node.
    bool compUnsafeCastUsed;       // Does the method use LDIND/STIND to cast between scalar/refernce types

    // NOTE: These values are only reliable after
    //       the importing is completely finished.

    ExpandArrayStack<GenTreePtr>* compQMarks; // The set of QMark nodes created in the current compilation, so
                                              // we can iterate over these efficiently.

#if CPU_USES_BLOCK_MOVE
    bool compBlkOpUsed; // Does the method do a COPYBLK or INITBLK
#endif

#ifdef DEBUG
    // State information - which phases have completed?
    // These are kept together for easy discoverability

    bool    bRangeAllowStress;
    bool    compCodeGenDone;
    int64_t compNumStatementLinksTraversed; // # of links traversed while doing debug checks
    bool    fgNormalizeEHDone;              // Has the flowgraph EH normalization phase been done?
    size_t  compSizeEstimate;               // The estimated size of the method as per `gtSetEvalOrder`.
    size_t  compCycleEstimate;              // The estimated cycle count of the method as per `gtSetEvalOrder`
#endif                                      // DEBUG

    bool fgLocalVarLivenessDone; // Note that this one is used outside of debug.
    bool fgLocalVarLivenessChanged;
#if STACK_PROBES
    bool compStackProbePrologDone;
#endif
#ifndef LEGACY_BACKEND
    bool compLSRADone;
#endif // !LEGACY_BACKEND
    bool compRationalIRForm;

    bool compUsesThrowHelper; // There is a call to a THOROW_HELPER for the compiled method.

    bool compGeneratingProlog;
    bool compGeneratingEpilog;
    bool compNeedsGSSecurityCookie; // There is an unsafe buffer (or localloc) on the stack.
                                    // Insert cookie on frame and code to check the cookie, like VC++ -GS.
    bool compGSReorderStackLayout;  // There is an unsafe buffer on the stack, reorder locals and make local
    // copies of susceptible parameters to avoid buffer overrun attacks through locals/params
    bool getNeedsGSSecurityCookie() const
    {
        return compNeedsGSSecurityCookie;
    }
    void setNeedsGSSecurityCookie()
    {
        compNeedsGSSecurityCookie = true;
    }

    FrameLayoutState lvaDoneFrameLayout; // The highest frame layout state that we've completed. During
                                         // frame layout calculations, this is the level we are currently
                                         // computing.

    //---------------------------- JITing options -----------------------------

    enum codeOptimize
    {
        BLENDED_CODE,
        SMALL_CODE,
        FAST_CODE,

        COUNT_OPT_CODE
    };

    struct Options
    {
        CORJIT_FLAGS* jitFlags;  // all flags passed from the EE
        unsigned      eeFlags;   // CorJitFlag flags passed from the EE
        unsigned      compFlags; // method attributes

        codeOptimize compCodeOpt; // what type of code optimizations

        bool compUseFCOMI;
        bool compUseCMOV;
#ifdef _TARGET_XARCH_
        bool compCanUseSSE2; // Allow CodeGen to use "movq XMM" instructions

#ifdef FEATURE_AVX_SUPPORT
        bool compCanUseAVX; // Allow CodeGen to use AVX 256-bit vectors for SIMD operations
#endif
#endif

// optimize maximally and/or favor speed over size?

#define DEFAULT_MIN_OPTS_CODE_SIZE 60000
#define DEFAULT_MIN_OPTS_INSTR_COUNT 20000
#define DEFAULT_MIN_OPTS_BB_COUNT 2000
#define DEFAULT_MIN_OPTS_LV_NUM_COUNT 2000
#define DEFAULT_MIN_OPTS_LV_REF_COUNT 8000

// Maximun number of locals before turning off the inlining
#define MAX_LV_NUM_COUNT_FOR_INLINING 512

        bool     compMinOpts;
        unsigned instrCount;
        unsigned lvRefCount;
        bool     compMinOptsIsSet;
#ifdef DEBUG
        bool compMinOptsIsUsed;

        inline bool MinOpts()
        {
            assert(compMinOptsIsSet);
            compMinOptsIsUsed = true;
            return compMinOpts;
        }
        inline bool IsMinOptsSet()
        {
            return compMinOptsIsSet;
        }
#else  // !DEBUG
        inline bool MinOpts()
        {
            return compMinOpts;
        }
        inline bool IsMinOptsSet()
        {
            return compMinOptsIsSet;
        }
#endif // !DEBUG
        inline void SetMinOpts(bool val)
        {
            assert(!compMinOptsIsUsed);
            assert(!compMinOptsIsSet || (compMinOpts == val));
            compMinOpts      = val;
            compMinOptsIsSet = true;
        }

        // true if the CLFLG_* for an optimization is set.
        inline bool OptEnabled(unsigned optFlag)
        {
            return !!(compFlags & optFlag);
        }

#ifdef FEATURE_READYTORUN_COMPILER
        inline bool IsReadyToRun()
        {
            return (eeFlags & CORJIT_FLG_READYTORUN) != 0;
        }
#else
        inline bool IsReadyToRun()
        {
            return false;
        }
#endif

        // true if we should use the PINVOKE_{BEGIN,END} helpers instead of generating
        // PInvoke transitions inline (e.g. when targeting CoreRT).
        inline bool ShouldUsePInvokeHelpers()
        {
#if COR_JIT_EE_VERSION > 460
            return (jitFlags->corJitFlags2 & CORJIT_FLG2_USE_PINVOKE_HELPERS) != 0;
#else
            return false;
#endif
        }

        // true if we should use insert the REVERSE_PINVOKE_{ENTER,EXIT} helpers in the method
        // prolog/epilog
        inline bool IsReversePInvoke()
        {
#if COR_JIT_EE_VERSION > 460
            return (jitFlags->corJitFlags2 & CORJIT_FLG2_REVERSE_PINVOKE) != 0;
#else
            return false;
#endif
        }

        // true if we must generate code compatible with JIT32 quirks
        inline bool IsJit32Compat()
        {
#if defined(_TARGET_X86_) && COR_JIT_EE_VERSION > 460
            return (jitFlags->corJitFlags2 & CORJIT_FLG2_DESKTOP_QUIRKS) != 0;
#else
            return false;
#endif
        }

        // true if we must generate code compatible with Jit64 quirks
        inline bool IsJit64Compat()
        {
#if defined(_TARGET_AMD64_) && COR_JIT_EE_VERSION > 460
            return (jitFlags->corJitFlags2 & CORJIT_FLG2_DESKTOP_QUIRKS) != 0;
#elif defined(_TARGET_AMD64_) && !defined(FEATURE_CORECLR)
            return true;
#else
            return false;
#endif
        }

#ifdef DEBUGGING_SUPPORT
        bool compScopeInfo; // Generate the LocalVar info ?
        bool compDbgCode;   // Generate debugger-friendly code?
        bool compDbgInfo;   // Gather debugging info?
        bool compDbgEnC;
#else
        static const bool compDbgCode;
#endif

#ifdef PROFILING_SUPPORTED
        bool compNoPInvokeInlineCB;
#else
        static const bool compNoPInvokeInlineCB;
#endif

        bool compMustInlinePInvokeCalli; // Unmanaged CALLI in IL stubs must be inlined

#ifdef DEBUG
        bool compGcChecks;         // Check arguments and return values to ensure they are sane
        bool compStackCheckOnRet;  // Check ESP on return to ensure it is correct
        bool compStackCheckOnCall; // Check ESP after every call to ensure it is correct

#endif

        bool compNeedSecurityCheck; // This flag really means where or not a security object needs
                                    // to be allocated on the stack.
                                    // It will be set to true in the following cases:
                                    //   1. When the method being compiled has a declarative security
                                    //        (i.e. when CORINFO_FLG_NOSECURITYWRAP is reset for the current method).
                                    //        This is also the case when we inject a prolog and epilog in the method.
                                    //   (or)
                                    //   2. When the method being compiled has imperative security (i.e. the method
                                    //        calls into another method that has CORINFO_FLG_SECURITYCHECK flag set).
                                    //   (or)
                                    //   3. When opts.compDbgEnC is true. (See also Compiler::compCompile).
                                    //
// When this flag is set, jit will allocate a gc-reference local variable (lvaSecurityObject),
// which gets reported as a GC root to stackwalker.
// (See also ICodeManager::GetAddrOfSecurityObject.)

#if RELOC_SUPPORT
        bool compReloc;
#endif

#ifdef DEBUG
#if defined(_TARGET_XARCH_) && !defined(LEGACY_BACKEND)
        bool compEnablePCRelAddr; // Whether absolute addr be encoded as PC-rel offset by RyuJIT where possible
#endif
#endif // DEBUG

#ifdef UNIX_AMD64_ABI
        // This flag  is indicating if there is a need to align the frame.
        // On AMD64-Windows, if there are calls, 4 slots for the outgoing ars are allocated, except for
        // FastTailCall. This slots makes the frame size non-zero, so alignment logic will be called.
        // On AMD64-Unix, there are no such slots. There is a possibility to have calls in the method with frame size of
        // 0. The frame alignment logic won't kick in. This flags takes care of the AMD64-Unix case by remembering that
        // there are calls and making sure the frame alignment logic is executed.
        bool compNeedToAlignFrame;
#endif // UNIX_AMD64_ABI

        bool compProcedureSplitting; // Separate cold code from hot code

        bool genFPorder; // Preserve FP order (operations are non-commutative)
        bool genFPopt;   // Can we do frame-pointer-omission optimization?
        bool altJit;     // True if we are an altjit and are compiling this method

#ifdef DEBUG
        bool compProcedureSplittingEH; // Separate cold code from hot code for functions with EH
        bool dspCode;                  // Display native code generated
        bool dspEHTable;               // Display the EH table reported to the VM
        bool dspInstrs;                // Display the IL instructions intermixed with the native code output
        bool dspEmit;                  // Display emitter output
        bool dspLines;                 // Display source-code lines intermixed with native code output
        bool dmpHex;                   // Display raw bytes in hex of native code output
        bool varNames;                 // Display variables names in native code output
        bool disAsm;                   // Display native code as it is generated
        bool disAsmSpilled;            // Display native code when any register spilling occurs
        bool disDiffable;              // Makes the Disassembly code 'diff-able'
        bool disAsm2;                  // Display native code after it is generated using external disassembler
        bool dspOrder;                 // Display names of each of the methods that we ngen/jit
        bool dspUnwind;                // Display the unwind info output
        bool dspDiffable;     // Makes the Jit Dump 'diff-able' (currently uses same COMPlus_* flag as disDiffable)
        bool compLongAddress; // Force using large pseudo instructions for long address
                              // (IF_LARGEJMP/IF_LARGEADR/IF_LARGLDC)
        bool dspGCtbls;       // Display the GC tables
#endif

#ifdef LATE_DISASM
        bool doLateDisasm; // Run the late disassembler
#endif                     // LATE_DISASM

#if DUMP_GC_TABLES && !defined(DEBUG) && defined(JIT32_GCENCODER)
// Only the JIT32_GCENCODER implements GC dumping in non-DEBUG code.
#pragma message("NOTE: this non-debug build has GC ptr table dumping always enabled!")
        static const bool dspGCtbls = true;
#endif

        // We need stack probes to guarantee that we won't trigger a stack overflow
        // when calling unmanaged code until they get a chance to set up a frame, because
        // the EE will have no idea where it is.
        //
        // We will only be doing this currently for hosted environments. Unfortunately
        // we need to take care of stubs, so potentially, we will have to do the probes
        // for any call. We have a plan for not needing for stubs though
        bool compNeedStackProbes;

        // Whether to emit Enter/Leave/TailCall hooks using a dummy stub (DummyProfilerELTStub())
        // This options helps one to make JIT behave as if it is under profiler.
        bool compJitELTHookEnabled;

#if FEATURE_TAILCALL_OPT
        // Whether opportunistic or implicit tail call optimization is enabled.
        bool compTailCallOpt;
        // Whether optimization of transforming a recursive tail call into a loop is enabled.
        bool compTailCallLoopOpt;
#endif

#ifdef ARM_SOFTFP
        static const bool compUseSoftFP = true;
#else // !ARM_SOFTFP
        static const bool compUseSoftFP = false;
#endif

        GCPollType compGCPollType;
    } opts;

#ifdef ALT_JIT
    static bool                s_pAltJitExcludeAssembliesListInitialized;
    static AssemblyNamesList2* s_pAltJitExcludeAssembliesList;
#endif // ALT_JIT

#ifdef DEBUG

    static bool s_dspMemStats; // Display per-phase memory statistics for every function

    template <typename T>
    T dspPtr(T p)
    {
        return (p == ZERO) ? ZERO : (opts.dspDiffable ? T(0xD1FFAB1E) : p);
    }

    template <typename T>
    T dspOffset(T o)
    {
        return (o == ZERO) ? ZERO : (opts.dspDiffable ? T(0xD1FFAB1E) : o);
    }

    static int dspTreeID(GenTree* tree)
    {
        return tree->gtTreeID;
    }
    static void printTreeID(GenTree* tree)
    {
        if (tree == nullptr)
        {
            printf("[------]");
        }
        else
        {
            printf("[%06d]", dspTreeID(tree));
        }
    }

#endif // DEBUG

// clang-format off
#define STRESS_MODES                                                                            \
                                                                                                \
        STRESS_MODE(NONE)                                                                       \
                                                                                                \
        /* "Variations" stress areas which we try to mix up with each other. */                 \
        /* These should not be exhaustively used as they might */                               \
        /* hide/trivialize other areas */                                                       \
                                                                                                \
        STRESS_MODE(REGS) STRESS_MODE(DBL_ALN) STRESS_MODE(LCL_FLDS) STRESS_MODE(UNROLL_LOOPS)  \
        STRESS_MODE(MAKE_CSE) STRESS_MODE(LEGACY_INLINE) STRESS_MODE(CLONE_EXPR)                \
        STRESS_MODE(USE_FCOMI) STRESS_MODE(USE_CMOV) STRESS_MODE(FOLD)                          \
        STRESS_MODE(BB_PROFILE) STRESS_MODE(OPT_BOOLS_GC) STRESS_MODE(REMORPH_TREES)            \
        STRESS_MODE(64RSLT_MUL) STRESS_MODE(DO_WHILE_LOOPS) STRESS_MODE(MIN_OPTS)               \
        STRESS_MODE(REVERSE_FLAG)     /* Will set GTF_REVERSE_OPS whenever we can */            \
        STRESS_MODE(REVERSE_COMMA)    /* Will reverse commas created  with gtNewCommaNode */    \
        STRESS_MODE(TAILCALL)         /* Will make the call as a tailcall whenever legal */     \
        STRESS_MODE(CATCH_ARG)        /* Will spill catch arg */                                \
        STRESS_MODE(UNSAFE_BUFFER_CHECKS)                                                       \
        STRESS_MODE(NULL_OBJECT_CHECK)                                                          \
        STRESS_MODE(PINVOKE_RESTORE_ESP)                                                        \
        STRESS_MODE(RANDOM_INLINE)                                                              \
                                                                                                \
        STRESS_MODE(GENERIC_VARN) STRESS_MODE(COUNT_VARN)                                       \
                                                                                                \
        /* "Check" stress areas that can be exhaustively used if we */                          \
        /*  dont care about performance at all */                                               \
                                                                                                \
        STRESS_MODE(FORCE_INLINE) /* Treat every method as AggressiveInlining */                \
        STRESS_MODE(CHK_FLOW_UPDATE)                                                            \
        STRESS_MODE(EMITTER) STRESS_MODE(CHK_REIMPORT) STRESS_MODE(FLATFP)                      \
                                                                                                \
        STRESS_MODE(GENERIC_CHECK) STRESS_MODE(COUNT)                                           \

    enum                compStressArea
    {
#define STRESS_MODE(mode) STRESS_##mode,
        STRESS_MODES
#undef STRESS_MODE
    };
// clang-format on

#ifdef DEBUG
    static const LPCWSTR s_compStressModeNames[STRESS_COUNT + 1];
    BYTE                 compActiveStressModes[STRESS_COUNT];
#endif // DEBUG

#define MAX_STRESS_WEIGHT 100

    bool compStressCompile(compStressArea stressArea, unsigned weightPercentage);

#ifdef DEBUG

    bool compInlineStress()
    {
        return compStressCompile(STRESS_LEGACY_INLINE, 50);
    }

    bool compRandomInlineStress()
    {
        return compStressCompile(STRESS_RANDOM_INLINE, 50);
    }

#endif // DEBUG

    bool compTailCallStress()
    {
#ifdef DEBUG
        return (JitConfig.TailcallStress() != 0 || compStressCompile(STRESS_TAILCALL, 5));
#else
        return false;
#endif
    }

    codeOptimize compCodeOpt()
    {
#if 0
        // Switching between size & speed has measurable throughput impact 
        // (3.5% on NGen mscorlib when measured). It used to be enabled for 
        // DEBUG, but should generate identical code between CHK & RET builds,
        // so that's not acceptable.
        // TODO-Throughput: Figure out what to do about size vs. speed & throughput.
        //                  Investigate the cause of the throughput regression.

        return opts.compCodeOpt;
#else
        return BLENDED_CODE;
#endif
    }

#ifdef DEBUG
    CLRRandom* inlRNG;
#endif

    //--------------------- Info about the procedure --------------------------

    struct Info
    {
        COMP_HANDLE           compCompHnd;
        CORINFO_MODULE_HANDLE compScopeHnd;
        CORINFO_CLASS_HANDLE  compClassHnd;
        CORINFO_METHOD_HANDLE compMethodHnd;
        CORINFO_METHOD_INFO*  compMethodInfo;

        BOOL hasCircularClassConstraints;
        BOOL hasCircularMethodConstraints;

#if defined(DEBUG) || defined(LATE_DISASM)
        const char* compMethodName;
        const char* compClassName;
        const char* compFullName;
#endif // defined(DEBUG) || defined(LATE_DISASM)

#if defined(DEBUG) || defined(INLINE_DATA)
        // Method hash is logcally const, but computed
        // on first demand.
        mutable unsigned compMethodHashPrivate;
        unsigned         compMethodHash() const;
#endif // defined(DEBUG) || defined(INLINE_DATA)

#ifdef PSEUDORANDOM_NOP_INSERTION
        // things for pseudorandom nop insertion
        unsigned  compChecksum;
        CLRRandom compRNG;
#endif

        // The following holds the FLG_xxxx flags for the method we're compiling.
        unsigned compFlags;

        // The following holds the class attributes for the method we're compiling.
        unsigned compClassAttr;

        const BYTE*    compCode;
        IL_OFFSET      compILCodeSize;     // The IL code size
        UNATIVE_OFFSET compNativeCodeSize; // The native code size, after instructions are issued. This
                                           // is less than (compTotalHotCodeSize + compTotalColdCodeSize) only if:
        // (1) the code is not hot/cold split, and we issued less code than we expected, or
        // (2) the code is hot/cold split, and we issued less code than we expected
        // in the cold section (the hot section will always be padded out to compTotalHotCodeSize).

        bool compIsStatic : 1;         // Is the method static (no 'this' pointer)?
        bool compIsVarArgs : 1;        // Does the method have varargs parameters?
        bool compIsContextful : 1;     // contextful method
        bool compInitMem : 1;          // Is the CORINFO_OPT_INIT_LOCALS bit set in the method info options?
        bool compUnwrapContextful : 1; // JIT should unwrap proxies when possible
        bool compProfilerCallback : 1; // JIT inserted a profiler Enter callback
        bool compPublishStubParam : 1; // EAX captured in prolog will be available through an instrinsic
        bool compRetBuffDefStack : 1;  // The ret buff argument definitely points into the stack.

        var_types compRetType;       // Return type of the method as declared in IL
        var_types compRetNativeType; // Normalized return type as per target arch ABI
        unsigned  compILargsCount;   // Number of arguments (incl. implicit but not hidden)
        unsigned  compArgsCount;     // Number of arguments (incl. implicit and     hidden)
        unsigned  compRetBuffArg;    // position of hidden return param var (0, 1) (BAD_VAR_NUM means not present);
        int compTypeCtxtArg; // position of hidden param for type context for generic code (CORINFO_CALLCONV_PARAMTYPE)
        unsigned       compThisArg; // position of implicit this pointer param (not to be confused with lvaArg0Var)
        unsigned       compILlocalsCount; // Number of vars : args + locals (incl. implicit but not hidden)
        unsigned       compLocalsCount;   // Number of vars : args + locals (incl. implicit and     hidden)
        unsigned       compMaxStack;
        UNATIVE_OFFSET compTotalHotCodeSize;  // Total number of bytes of Hot Code in the method
        UNATIVE_OFFSET compTotalColdCodeSize; // Total number of bytes of Cold Code in the method

        unsigned compCallUnmanaged;   // count of unmanaged calls
        unsigned compLvFrameListRoot; // lclNum for the Frame root
        unsigned compXcptnsCount;     // Number of exception-handling clauses read in the method's IL.
                                      // You should generally use compHndBBtabCount instead: it is the
                                      // current number of EH clauses (after additions like synchronized
                                      // methods and funclets, and removals like unreachable code deletion).

        bool compMatchedVM; // true if the VM is "matched": either the JIT is a cross-compiler
                            // and the VM expects that, or the JIT is a "self-host" compiler
                            // (e.g., x86 hosted targeting x86) and the VM expects that.

#if defined(DEBUGGING_SUPPORT) || defined(DEBUG)

        /*  The following holds IL scope information about local variables.
         */

        unsigned     compVarScopesCount;
        VarScopeDsc* compVarScopes;

        /* The following holds information about instr offsets for
         * which we need to report IP-mappings
         */

        IL_OFFSET*                   compStmtOffsets; // sorted
        unsigned                     compStmtOffsetsCount;
        ICorDebugInfo::BoundaryTypes compStmtOffsetsImplicit;

#endif // DEBUGGING_SUPPORT || DEBUG

#define CPU_X86 0x0100 // The generic X86 CPU
#define CPU_X86_PENTIUM_4 0x0110

#define CPU_X64 0x0200       // The generic x64 CPU
#define CPU_AMD_X64 0x0210   // AMD x64 CPU
#define CPU_INTEL_X64 0x0240 // Intel x64 CPU

#define CPU_ARM 0x0300 // The generic ARM CPU

        unsigned genCPU; // What CPU are we running on
    } info;

    // Returns true if the method being compiled returns a non-void and non-struct value.
    // Note that lvaInitTypeRef() normalizes compRetNativeType for struct returns in a
    // single register as per target arch ABI (e.g on Amd64 Windows structs of size 1, 2,
    // 4 or 8 gets normalized to TYP_BYTE/TYP_SHORT/TYP_INT/TYP_LONG; On Arm HFA structs).
    // Methods returning such structs are considered to return non-struct return value and
    // this method returns true in that case.
    bool compMethodReturnsNativeScalarType()
    {
        return (info.compRetType != TYP_VOID) && !varTypeIsStruct(info.compRetNativeType);
    }

    // Returns true if the method being compiled returns RetBuf addr as its return value
    bool compMethodReturnsRetBufAddr()
    {
        // There are cases where implicit RetBuf argument should be explicitly returned in a register.
        // In such cases the return type is changed to TYP_BYREF and appropriate IR is generated.
        // These cases are:
        // 1. Profiler Leave calllback expects the address of retbuf as return value for
        //    methods with hidden RetBuf argument.  impReturnInstruction() when profiler
        //    callbacks are needed creates GT_RETURN(TYP_BYREF, op1 = Addr of RetBuf) for
        //    methods with hidden RetBufArg.
        //
        // 2. As per the System V ABI, the address of RetBuf needs to be returned by
        //    methods with hidden RetBufArg in RAX. In such case GT_RETURN is of TYP_BYREF,
        //    returning the address of RetBuf.
        //
        // 3. Windows 64-bit native calling convention also requires the address of RetBuff
        //    to be returned in RAX.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef _TARGET_AMD64_
        return (info.compRetBuffArg != BAD_VAR_NUM);
#else  // !_TARGET_AMD64_
        return (compIsProfilerHookNeeded()) && (info.compRetBuffArg != BAD_VAR_NUM);
#endif // !_TARGET_AMD64_
    }

    // Returns true if the method returns a value in more than one return register
    // TODO-ARM-Bug: Deal with multi-register genReturnLocaled structs?
    // TODO-ARM64: Does this apply for ARM64 too?
    bool compMethodReturnsMultiRegRetType()
    {
#if FEATURE_MULTIREG_RET
#if defined(_TARGET_X86_)
        // On x86 only 64-bit longs are returned in multiple registers
        return varTypeIsLong(info.compRetNativeType);
#else  // targets: X64-UNIX, ARM64 or ARM32
        // On all other targets that support multireg return values:
        // Methods returning a struct in multiple registers have a return value of TYP_STRUCT.
        // Such method's compRetNativeType is TYP_STRUCT without a hidden RetBufArg
        return varTypeIsStruct(info.compRetNativeType) && (info.compRetBuffArg == BAD_VAR_NUM);
#endif // TARGET_XXX
#else // not FEATURE_MULTIREG_RET
        // For this architecture there are no multireg returns
        return false;
#endif // FEATURE_MULTIREG_RET
    }

#if FEATURE_MULTIREG_ARGS
    // Given a GenTree node of TYP_STRUCT that represents a pass by value argument
    // return the gcPtr layout for the pointers sized fields
    void getStructGcPtrsFromOp(GenTreePtr op, BYTE* gcPtrsOut);
#endif // FEATURE_MULTIREG_ARGS

    // Returns true if the method being compiled returns a value
    bool compMethodHasRetVal()
    {
        return compMethodReturnsNativeScalarType() || compMethodReturnsRetBufAddr() ||
               compMethodReturnsMultiRegRetType();
    }

#if defined(DEBUG)

    void compDispLocalVars();

#endif // DEBUGGING_SUPPORT || DEBUG

//-------------------------- Global Compiler Data ------------------------------------

#ifdef DEBUG
    static unsigned s_compMethodsCount; // to produce unique label names
    unsigned        compGenTreeID;
#endif

    BasicBlock* compCurBB;   // the current basic block in process
    GenTreePtr  compCurStmt; // the current statement in process
#ifdef DEBUG
    unsigned compCurStmtNum; // to give all statements an increasing StmtNum when printing dumps
#endif

    //  The following is used to create the 'method JIT info' block.
    size_t compInfoBlkSize;
    BYTE*  compInfoBlkAddr;

    EHblkDsc* compHndBBtab;           // array of EH data
    unsigned  compHndBBtabCount;      // element count of used elements in EH data array
    unsigned  compHndBBtabAllocCount; // element count of allocated elements in EH data array

#if defined(_TARGET_X86_)

    //-------------------------------------------------------------------------
    //  Tracking of region covered by the monitor in synchronized methods
    void* syncStartEmitCookie; // the emitter cookie for first instruction after the call to MON_ENTER
    void* syncEndEmitCookie;   // the emitter cookie for first instruction after the call to MON_EXIT

#endif // !_TARGET_X86_

    Phases previousCompletedPhase; // the most recently completed phase

    //-------------------------------------------------------------------------
    //  The following keeps track of how many bytes of local frame space we've
    //  grabbed so far in the current function, and how many argument bytes we
    //  need to pop when we return.
    //

    unsigned compLclFrameSize; // secObject+lclBlk+locals+temps

    // Count of callee-saved regs we pushed in the prolog.
    // Does not include EBP for isFramePointerUsed() and double-aligned frames.
    // In case of Amd64 this doesn't include float regs saved on stack.
    unsigned compCalleeRegsPushed;

#if defined(_TARGET_XARCH_) && !FEATURE_STACK_FP_X87
    // Mask of callee saved float regs on stack.
    regMaskTP compCalleeFPRegsSavedMask;
#endif
#ifdef _TARGET_AMD64_
// Quirk for VS debug-launch scenario to work:
// Bytes of padding between save-reg area and locals.
#define VSQUIRK_STACK_PAD (2 * REGSIZE_BYTES)
    unsigned compVSQuirkStackPaddingNeeded;
    bool     compQuirkForPPPflag;
#endif

    unsigned compArgSize; // total size of arguments in bytes (including register args (lvIsRegArg))

    unsigned compMapILargNum(unsigned ILargNum); // map accounting for hidden args
    unsigned compMapILvarNum(unsigned ILvarNum); // map accounting for hidden args
    unsigned compMap2ILvarNum(unsigned varNum);  // map accounting for hidden args

    //-------------------------------------------------------------------------

    static void compStartup();  // One-time initialization
    static void compShutdown(); // One-time finalization

    void compInit(ArenaAllocator* pAlloc, InlineInfo* inlineInfo);
    void compDone();

    static void compDisplayStaticSizes(FILE* fout);

    //------------ Some utility functions --------------

    void* compGetHelperFtn(CorInfoHelpFunc ftnNum,         /* IN  */
                           void**          ppIndirection); /* OUT */

    // Several JIT/EE interface functions return a CorInfoType, and also return a
    // class handle as an out parameter if the type is a value class.  Returns the
    // size of the type these describe.
    unsigned compGetTypeSize(CorInfoType cit, CORINFO_CLASS_HANDLE clsHnd);

#ifdef DEBUG
    // Components used by the compiler may write unit test suites, and
    // have them run within this method.  They will be run only once per process, and only
    // in debug.  (Perhaps should be under the control of a COMPlus_ flag.)
    // These should fail by asserting.
    void compDoComponentUnitTestsOnce();
#endif // DEBUG

    int compCompile(CORINFO_METHOD_HANDLE methodHnd,
                    CORINFO_MODULE_HANDLE classPtr,
                    COMP_HANDLE           compHnd,
                    CORINFO_METHOD_INFO*  methodInfo,
                    void**                methodCodePtr,
                    ULONG*                methodCodeSize,
                    CORJIT_FLAGS*         compileFlags);
    void compCompileFinish();
    int compCompileHelper(CORINFO_MODULE_HANDLE            classPtr,
                          COMP_HANDLE                      compHnd,
                          CORINFO_METHOD_INFO*             methodInfo,
                          void**                           methodCodePtr,
                          ULONG*                           methodCodeSize,
                          CORJIT_FLAGS*                    compileFlags,
                          CorInfoInstantiationVerification instVerInfo);

    ArenaAllocator* compGetAllocator();

#if MEASURE_MEM_ALLOC
    struct MemStats
    {
        unsigned allocCnt;                 // # of allocs
        UINT64   allocSz;                  // total size of those alloc.
        UINT64   allocSzMax;               // Maximum single allocation.
        UINT64   allocSzByKind[CMK_Count]; // Classified by "kind".
        UINT64   nraTotalSizeAlloc;
        UINT64   nraTotalSizeUsed;

        static const char* s_CompMemKindNames[]; // Names of the kinds.

        MemStats() : allocCnt(0), allocSz(0), allocSzMax(0), nraTotalSizeAlloc(0), nraTotalSizeUsed(0)
        {
            for (int i = 0; i < CMK_Count; i++)
            {
                allocSzByKind[i] = 0;
            }
        }
        MemStats(const MemStats& ms)
            : allocCnt(ms.allocCnt)
            , allocSz(ms.allocSz)
            , allocSzMax(ms.allocSzMax)
            , nraTotalSizeAlloc(ms.nraTotalSizeAlloc)
            , nraTotalSizeUsed(ms.nraTotalSizeUsed)
        {
            for (int i = 0; i < CMK_Count; i++)
            {
                allocSzByKind[i] = ms.allocSzByKind[i];
            }
        }

        // Until we have ubiquitous constructors.
        void Init()
        {
            this->MemStats::MemStats();
        }

        void AddAlloc(size_t sz, CompMemKind cmk)
        {
            allocCnt += 1;
            allocSz += sz;
            if (sz > allocSzMax)
            {
                allocSzMax = sz;
            }
            allocSzByKind[cmk] += sz;
        }

        void Print(FILE* f);       // Print these stats to f.
        void PrintByKind(FILE* f); // Do just the by-kind histogram part.
    };
    MemStats genMemStats;

    struct AggregateMemStats : public MemStats
    {
        unsigned nMethods;

        AggregateMemStats() : MemStats(), nMethods(0)
        {
        }

        void Add(const MemStats& ms)
        {
            nMethods++;
            allocCnt += ms.allocCnt;
            allocSz += ms.allocSz;
            allocSzMax = max(allocSzMax, ms.allocSzMax);
            for (int i = 0; i < CMK_Count; i++)
            {
                allocSzByKind[i] += ms.allocSzByKind[i];
            }
            nraTotalSizeAlloc += ms.nraTotalSizeAlloc;
            nraTotalSizeUsed += ms.nraTotalSizeUsed;
        }

        void Print(FILE* f); // Print these stats to jitstdout.
    };

    static CritSecObject     s_memStatsLock;    // This lock protects the data structures below.
    static MemStats          s_maxCompMemStats; // Stats for the compilation with the largest amount allocated.
    static AggregateMemStats s_aggMemStats;     // Aggregates statistics for all compilations.

#endif // MEASURE_MEM_ALLOC

#if LOOP_HOIST_STATS
    unsigned m_loopsConsidered;
    bool     m_curLoopHasHoistedExpression;
    unsigned m_loopsWithHoistedExpressions;
    unsigned m_totalHoistedExpressions;

    void AddLoopHoistStats();
    void PrintPerMethodLoopHoistStats();

    static CritSecObject s_loopHoistStatsLock; // This lock protects the data structures below.
    static unsigned      s_loopsConsidered;
    static unsigned      s_loopsWithHoistedExpressions;
    static unsigned      s_totalHoistedExpressions;

    static void PrintAggregateLoopHoistStats(FILE* f);
#endif // LOOP_HOIST_STATS

    void* compGetMemArray(size_t numElem, size_t elemSize, CompMemKind cmk = CMK_Unknown);
    void* compGetMemArrayA(size_t numElem, size_t elemSize, CompMemKind cmk = CMK_Unknown);
    void* compGetMem(size_t sz, CompMemKind cmk = CMK_Unknown);
    void* compGetMemA(size_t sz, CompMemKind cmk = CMK_Unknown);
    static void* compGetMemCallback(void*, size_t, CompMemKind cmk = CMK_Unknown);
    void compFreeMem(void*);

    bool compIsForImportOnly();
    bool compIsForInlining();
    bool compDonotInline();

#ifdef DEBUG
    const char* compLocalVarName(unsigned varNum, unsigned offs);
    VarName compVarName(regNumber reg, bool isFloatReg = false);
    const char* compRegVarName(regNumber reg, bool displayVar = false, bool isFloatReg = false);
    const char* compRegPairName(regPairNo regPair);
    const char* compRegNameForSize(regNumber reg, size_t size);
    const char* compFPregVarName(unsigned fpReg, bool displayVar = false);
    void compDspSrcLinesByNativeIP(UNATIVE_OFFSET curIP);
    void compDspSrcLinesByLineNum(unsigned line, bool seek = false);
#endif // DEBUG

//-------------------------------------------------------------------------

#ifdef DEBUGGING_SUPPORT
    typedef ListNode<VarScopeDsc*> VarScopeListNode;

    struct VarScopeMapInfo
    {
        VarScopeListNode*       head;
        VarScopeListNode*       tail;
        static VarScopeMapInfo* Create(VarScopeListNode* node, IAllocator* alloc)
        {
            VarScopeMapInfo* info = new (alloc) VarScopeMapInfo;
            info->head            = node;
            info->tail            = node;
            return info;
        }
    };

    // Max value of scope count for which we would use linear search; for larger values we would use hashtable lookup.
    static const unsigned MAX_LINEAR_FIND_LCL_SCOPELIST = 32;

    typedef SimplerHashTable<unsigned, SmallPrimitiveKeyFuncs<unsigned>, VarScopeMapInfo*, JitSimplerHashBehavior>
        VarNumToScopeDscMap;

    // Map to keep variables' scope indexed by varNum containing it's scope dscs at the index.
    VarNumToScopeDscMap* compVarScopeMap;

    VarScopeDsc* compFindLocalVar(unsigned varNum, unsigned lifeBeg, unsigned lifeEnd);

    VarScopeDsc* compFindLocalVar(unsigned varNum, unsigned offs);

    VarScopeDsc* compFindLocalVarLinear(unsigned varNum, unsigned offs);

    void compInitVarScopeMap();

    VarScopeDsc** compEnterScopeList; // List has the offsets where variables
                                      // enter scope, sorted by instr offset
    unsigned compNextEnterScope;

    VarScopeDsc** compExitScopeList; // List has the offsets where variables
                                     // go out of scope, sorted by instr offset
    unsigned compNextExitScope;

    void compInitScopeLists();

    void compResetScopeLists();

    VarScopeDsc* compGetNextEnterScope(unsigned offs, bool scan = false);

    VarScopeDsc* compGetNextExitScope(unsigned offs, bool scan = false);

    void compProcessScopesUntil(unsigned   offset,
                                VARSET_TP* inScope,
                                void (Compiler::*enterScopeFn)(VARSET_TP* inScope, VarScopeDsc*),
                                void (Compiler::*exitScopeFn)(VARSET_TP* inScope, VarScopeDsc*));

#ifdef DEBUG
    void compDispScopeLists();
#endif // DEBUG

#endif // DEBUGGING_SUPPORT

    bool compIsProfilerHookNeeded();

    //-------------------------------------------------------------------------
    /*               Statistical Data Gathering                               */

    void compJitStats(); // call this function and enable
                         // various ifdef's below for statistical data

#if CALL_ARG_STATS
    void        compCallArgStats();
    static void compDispCallArgStats(FILE* fout);
#endif

    //-------------------------------------------------------------------------

protected:
#ifdef DEBUG
    bool skipMethod();
#endif

    ArenaAllocator* compAllocator;

public:
    // This one presents an implementation of the "IAllocator" abstract class that uses "compAllocator",
    // suitable for use by utilcode collection types.
    IAllocator* compAsIAllocator;

#if MEASURE_MEM_ALLOC
    IAllocator* compAsIAllocatorBitset;    // An allocator that uses the CMK_bitset tracker.
    IAllocator* compAsIAllocatorGC;        // An allocator that uses the CMK_GC tracker.
    IAllocator* compAsIAllocatorLoopHoist; // An allocator that uses the CMK_LoopHoist tracker.
#ifdef DEBUG
    IAllocator* compAsIAllocatorDebugOnly; // An allocator that uses the CMK_DebugOnly tracker.
#endif                                     // DEBUG
#endif                                     // MEASURE_MEM_ALLOC

    void compFunctionTraceStart();
    void compFunctionTraceEnd(void* methodCodePtr, ULONG methodCodeSize, bool isNYI);

protected:
    size_t compMaxUncheckedOffsetForNullObject;

    void compInitOptions(CORJIT_FLAGS* compileFlags);

    void compSetProcessor();
    void compInitDebuggingInfo();
    void compSetOptimizationLevel();
#ifdef _TARGET_ARMARCH_
    bool compRsvdRegCheck(FrameLayoutState curState);
#endif
    void compCompile(void** methodCodePtr, ULONG* methodCodeSize, CORJIT_FLAGS* compileFlags);

    // Data required for generating profiler Enter/Leave/TailCall hooks
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef PROFILING_SUPPORTED
    bool  compProfilerHookNeeded; // Whether profiler Enter/Leave/TailCall hook needs to be generated for the method
    void* compProfilerMethHnd;    // Profiler handle of the method being compiled. Passed as param to ELT callbacks
    bool  compProfilerMethHndIndirected; // Whether compProfilerHandle is pointer to the handle or is an actual handle
#endif
#ifdef _TARGET_AMD64_
    bool compQuirkForPPP(); // Check if this method should be Quirked for the PPP issue
#endif
public:
    // Assumes called as part of process shutdown; does any compiler-specific work associated with that.
    static void ProcessShutdownWork(ICorStaticInfo* statInfo);

    IAllocator* getAllocator()
    {
        return compAsIAllocator;
    }

#if MEASURE_MEM_ALLOC
    IAllocator* getAllocatorBitset()
    {
        return compAsIAllocatorBitset;
    }
    IAllocator* getAllocatorGC()
    {
        return compAsIAllocatorGC;
    }
    IAllocator* getAllocatorLoopHoist()
    {
        return compAsIAllocatorLoopHoist;
    }
#else  // !MEASURE_MEM_ALLOC
    IAllocator* getAllocatorBitset()
    {
        return compAsIAllocator;
    }
    IAllocator* getAllocatorGC()
    {
        return compAsIAllocator;
    }
    IAllocator* getAllocatorLoopHoist()
    {
        return compAsIAllocator;
    }
#endif // !MEASURE_MEM_ALLOC

#ifdef DEBUG
    IAllocator* getAllocatorDebugOnly()
    {
#if MEASURE_MEM_ALLOC
        return compAsIAllocatorDebugOnly;
#else  // !MEASURE_MEM_ALLOC
        return compAsIAllocator;
#endif // !MEASURE_MEM_ALLOC
    }
#endif // DEBUG

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           typeInfo                                        XX
    XX                                                                           XX
    XX   Checks for type compatibility and merges types                          XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    // Set to TRUE if verification cannot be skipped for this method
    // If we detect unverifiable code, we will lazily check
    // canSkipMethodVerification() to see if verification is REALLY needed.
    BOOL tiVerificationNeeded;

    // It it initially TRUE, and it gets set to FALSE if we run into unverifiable code
    // Note that this is valid only if tiVerificationNeeded was ever TRUE.
    BOOL tiIsVerifiableCode;

    // Set to TRUE if runtime callout is needed for this method
    BOOL tiRuntimeCalloutNeeded;

    // Set to TRUE if security prolog/epilog callout is needed for this method
    // Note: This flag is different than compNeedSecurityCheck.
    //     compNeedSecurityCheck means whether or not a security object needs
    //         to be allocated on the stack, which is currently true for EnC as well.
    //     tiSecurityCalloutNeeded means whether or not security callouts need
    //         to be inserted in the jitted code.
    BOOL tiSecurityCalloutNeeded;

    // Returns TRUE if child is equal to or a subtype of parent for merge purposes
    // This support is necessary to suport attributes that are not described in
    // for example, signatures. For example, the permanent home byref (byref that
    // points to the gc heap), isn't a property of method signatures, therefore,
    // it is safe to have mismatches here (that tiCompatibleWith will not flag),
    // but when deciding if we need to reimport a block, we need to take these
    // in account
    BOOL tiMergeCompatibleWith(const typeInfo& pChild, const typeInfo& pParent, bool normalisedForStack) const;

    // Returns TRUE if child is equal to or a subtype of parent.
    // normalisedForStack indicates that both types are normalised for the stack
    BOOL tiCompatibleWith(const typeInfo& pChild, const typeInfo& pParent, bool normalisedForStack) const;

    // Merges pDest and pSrc. Returns FALSE if merge is undefined.
    // *pDest is modified to represent the merged type.  Sets "*changed" to true
    // if this changes "*pDest".
    BOOL tiMergeToCommonParent(typeInfo* pDest, const typeInfo* pSrc, bool* changed) const;

    // Set pDest from the primitive value type.
    // Eg. System.Int32 -> ELEMENT_TYPE_I4

    BOOL tiFromPrimitiveValueClass(typeInfo* pDest, const typeInfo* pVC) const;

#ifdef DEBUG
    // <BUGNUM> VSW 471305
    // IJW allows assigning REF to BYREF. The following allows us to temporarily
    // bypass the assert check in gcMarkRegSetGCref and gcMarkRegSetByref
    // We use a "short" as we need to push/pop this scope.
    // </BUGNUM>
    short compRegSetCheckLevel;
#endif

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                           IL verification stuff                           XX
    XX                                                                           XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */

public:
    // The following is used to track liveness of local variables, initialization
    // of valueclass constructors, and type safe use of IL instructions.

    // dynamic state info needed for verification
    EntryState verCurrentState;

    // this ptr of object type .ctors are considered intited only after
    // the base class ctor is called, or an alternate ctor is called.
    // An uninited this ptr can be used to access fields, but cannot
    // be used to call a member function.
    BOOL verTrackObjCtorInitState;

    void verInitBBEntryState(BasicBlock* block, EntryState* currentState);

    // Requires that "tis" is not TIS_Bottom -- it's a definite init/uninit state.
    void verSetThisInit(BasicBlock* block, ThisInitState tis);
    void verInitCurrentState();
    void verResetCurrentState(BasicBlock* block, EntryState* currentState);

    // Merges the current verification state into the entry state of "block", return FALSE if that merge fails,
    // TRUE if it succeeds.  Further sets "*changed" to true if this changes the entry state of "block".
    BOOL verMergeEntryStates(BasicBlock* block, bool* changed);

    void verConvertBBToThrowVerificationException(BasicBlock* block DEBUGARG(bool logMsg));
    void verHandleVerificationFailure(BasicBlock* block DEBUGARG(bool logMsg));
    typeInfo verMakeTypeInfo(CORINFO_CLASS_HANDLE clsHnd,
                             bool bashStructToRef = false); // converts from jit type representation to typeInfo
    typeInfo verMakeTypeInfo(CorInfoType          ciType,
                             CORINFO_CLASS_HANDLE clsHnd); // converts from jit type representation to typeInfo
    BOOL verIsSDArray(typeInfo ti);
    typeInfo verGetArrayElemType(typeInfo ti);

    typeInfo verParseArgSigToTypeInfo(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE args);
    BOOL verNeedsVerification();
    BOOL verIsByRefLike(const typeInfo& ti);
    BOOL verIsSafeToReturnByRef(const typeInfo& ti);

    // generic type variables range over types that satisfy IsBoxable
    BOOL verIsBoxable(const typeInfo& ti);

    void DECLSPEC_NORETURN verRaiseVerifyException(INDEBUG(const char* reason) DEBUGARG(const char* file)
                                                       DEBUGARG(unsigned line));
    void verRaiseVerifyExceptionIfNeeded(INDEBUG(const char* reason) DEBUGARG(const char* file)
                                             DEBUGARG(unsigned line));
    bool verCheckTailCallConstraint(OPCODE                  opcode,
                                    CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, // Is this a "constrained." call
                                                                                       // on a type parameter?
                                    bool speculative // If true, won't throw if verificatoin fails. Instead it will
                                                     // return false to the caller.
                                                     // If false, it will throw.
                                    );
    bool verIsBoxedValueType(typeInfo ti);

    void verVerifyCall(OPCODE                  opcode,
                       CORINFO_RESOLVED_TOKEN* pResolvedToken,
                       CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                       bool                    tailCall,
                       bool                    readonlyCall, // is this a "readonly." call?
                       const BYTE*             delegateCreateStart,
                       const BYTE*             codeAddr,
                       CORINFO_CALL_INFO* callInfo DEBUGARG(const char* methodName));

    BOOL verCheckDelegateCreation(const BYTE* delegateCreateStart, const BYTE* codeAddr, mdMemberRef& targetMemberRef);

    typeInfo verVerifySTIND(const typeInfo& ptr, const typeInfo& value, const typeInfo& instrType);
    typeInfo verVerifyLDIND(const typeInfo& ptr, const typeInfo& instrType);
    void verVerifyField(CORINFO_RESOLVED_TOKEN*   pResolvedToken,
                        const CORINFO_FIELD_INFO& fieldInfo,
                        const typeInfo*           tiThis,
                        BOOL                      mutator,
                        BOOL                      allowPlainStructAsThis = FALSE);
    void verVerifyCond(const typeInfo& tiOp1, const typeInfo& tiOp2, unsigned opcode);
    void verVerifyThisPtrInitialised();
    BOOL verIsCallToInitThisPtr(CORINFO_CLASS_HANDLE context, CORINFO_CLASS_HANDLE target);

    // Register allocator
    void raInitStackFP();
    void raEnregisterVarsPrePassStackFP();
    void raSetRegLclBirthDeath(GenTreePtr tree, VARSET_VALARG_TP lastlife, bool fromLDOBJ);
    void raEnregisterVarsPostPassStackFP();
    void raGenerateFPRefCounts();
    void raEnregisterVarsStackFP();
    void raUpdateHeightsForVarsStackFP(VARSET_VALARG_TP mask);

    regNumber raRegForVarStackFP(unsigned varTrackedIndex);
    void raAddPayloadStackFP(VARSET_VALARG_TP mask, unsigned weight);

    // returns true if enregistering v1 would save more mem accesses than v2
    bool raVarIsGreaterValueStackFP(LclVarDsc* lv1, LclVarDsc* lv2);

#ifdef DEBUG
    void raDumpHeightsStackFP();
    void raDumpVariableRegIntfFloat();
#endif

#if FEATURE_STACK_FP_X87

    // Currently, we use FP transition blocks in only 2 situations:
    //
    //      -conditional jump on longs where FP stack differs with target: it's not strictly
    //       necessary, but its low frequency and the code would get complicated if we try to
    //       inline the FP stack adjustment, as we have a lot of special casing going on to try
    //       minimize the way we generate the jump code.
    //      -case statements of switch where the FP stack differs with the one of evaluating the switch () statement
    //       We do this as we want to codegen switch as a jumptable. Again, this is low frequency.
    //
    //      However, transition blocks have 2 problems
    //
    //          - Procedure splitting: current implementation of procedure splitting requires all basic blocks to
    //            be known at codegen time, as it generates all hot blocks first and cold blocks later. This ties
    //            us up in codegen and is a solvable problem (we could make procedure splitting generate blocks
    //            in the right place without preordering them), this causes us to have to generate the transition
    //            blocks in the cold area if we want procedure splitting.
    //
    //
    //          - Thread abort exceptions and transition blocks. Transition blocks were designed under the assumption
    //            that no exceptions can happen inside them. Unfortunately Thread.Abort can happen in any instruction,
    //            and if we have handlers we will have to try to call them. Fixing this the right way would imply
    //            having multiple try native code regions for a single try il region. This is doable and shouldnt be
    //            a big change in the exception.
    //
    //      Given the low frequency of the cases where we have transition blocks, I've decided to dumb down
    //      optimizations. For these 2 cases:
    //
    //          - When there is a chance that we will have FP transition blocks, we won't do procedure splitting.
    //          - When a method has a handler, it won't enregister any FP variables that go thru a conditional long or
    //          a switch statement.
    //
    //      If at any point we find we need to optimize this, we should throw work at unblocking the restrictions our
    //      current procedure splitting and exception code have.
    bool compMayHaveTransitionBlocks;

    VARSET_TP raMaskDontEnregFloat; // mask for additional restrictions

    VARSET_TP raLclRegIntfFloat[REG_FPCOUNT];

    unsigned raCntStkStackFP;
    unsigned raCntWtdStkDblStackFP;
    unsigned raCntStkParamDblStackFP;

    // Payload in mem accesses for enregistering a variable (we dont want to mix with refcounts)
    // TODO: Do we want to put this in LclVarDsc?
    unsigned raPayloadStackFP[lclMAX_TRACKED];
    unsigned raHeightsStackFP[lclMAX_TRACKED][FP_VIRTUALREGISTERS + 1];
#ifdef DEBUG
    // Useful for debugging
    unsigned raHeightsNonWeightedStackFP[lclMAX_TRACKED][FP_VIRTUALREGISTERS + 1];
#endif
#endif // FEATURE_STACK_FP_X87

#ifdef DEBUG
    // One line log function. Default level is 0. Increasing it gives you
    // more log information

    // levels are currently unused: #define JITDUMP(level,...)                     ();
    void JitLogEE(unsigned level, const char* fmt, ...);

    bool compDebugBreak;

    bool compJitHaltMethod();

#endif

    /*
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XX                                                                           XX
    XX                   GS Security checks for unsafe buffers                   XX
    XX                                                                           XX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
    */
public:
    struct ShadowParamVarInfo
    {
        FixedBitVect* assignGroup; // the closure set of variables whose values depend on each other
        unsigned      shadowCopy;  // Lcl var num, valid only if not set to NO_SHADOW_COPY

        static bool mayNeedShadowCopy(LclVarDsc* varDsc)
        {
#if defined(_TARGET_AMD64_) && !defined(LEGACY_BACKEND)
            // GS cookie logic to create shadow slots, create trees to copy reg args to shadow
            // slots and update all trees to refer to shadow slots is done immediately after
            // fgMorph().  Lsra could potentially mark a param as DoNotEnregister after JIT determines
            // not to shadow a parameter.  Also, LSRA could potentially spill a param which is passed
            // in register. Therefore, conservatively all params may need a shadow copy.  Note that
            // GS cookie logic further checks whether the param is a ptr or an unsafe buffer before
            // creating a shadow slot even though this routine returns true.
            //
            // TODO-AMD64-CQ: Revisit this conservative approach as it could create more shadow slots than
            // required. There are two cases under which a reg arg could potentially be used from its
            // home location:
            //   a) LSRA marks it as DoNotEnregister (see LinearScan::identifyCandidates())
            //   b) LSRA spills it
            //
            // Possible solution to address case (a)
            //   - The conditions under which LSRA marks a varDsc as DoNotEnregister could be checked
            //     in this routine.  Note that live out of exception handler is something we may not be
            //     able to do it here since GS cookie logic is invoked ahead of liveness computation.
            //     Therefore, for methods with exception handling and need GS cookie check we might have
            //     to take conservative approach.
            //
            // Possible solution to address case (b)
            //   - Whenver a parameter passed in an argument register needs to be spilled by LSRA, we
            //     create a new spill temp if the method needs GS cookie check.
            return varDsc->lvIsParam;
#else // !(defined(_TARGET_AMD64_) && defined(LEGACY_BACKEND))
            return varDsc->lvIsParam && !varDsc->lvIsRegArg;
#endif
        }

#ifdef DEBUG
        void Print()
        {
            printf("assignGroup [%p]; shadowCopy: [%d];\n", assignGroup, shadowCopy);
        }
#endif
    };

    GSCookie*           gsGlobalSecurityCookieAddr; // Address of global cookie for unsafe buffer checks
    GSCookie            gsGlobalSecurityCookieVal;  // Value of global cookie if addr is NULL
    ShadowParamVarInfo* gsShadowVarInfo;            // Table used by shadow param analysis code

    void gsGSChecksInitCookie();   // Grabs cookie variable
    void gsCopyShadowParams();     // Identify vulnerable params and create dhadow copies
    bool gsFindVulnerableParams(); // Shadow param analysis code
    void gsParamsToShadows();      // Insert copy code and replave param uses by shadow

    static fgWalkPreFn gsMarkPtrsAndAssignGroups; // Shadow param analysis tree-walk
    static fgWalkPreFn gsReplaceShadowParams;     // Shadow param replacement tree-walk

#define DEFAULT_MAX_INLINE_SIZE 100 // Methods with >  DEFAULT_MAX_INLINE_SIZE IL bytes will never be inlined.
                                    // This can be overwritten by setting complus_JITInlineSize env variable.

#define DEFAULT_MAX_INLINE_DEPTH 20 // Methods at more than this level deep will not be inlined

private:
#ifdef FEATURE_JIT_METHOD_PERF
    JitTimer*                  pCompJitTimer;         // Timer data structure (by phases) for current compilation.
    static CompTimeSummaryInfo s_compJitTimerSummary; // Summary of the Timer information for the whole run.

    static LPCWSTR JitTimeLogCsv();        // Retrieve the file name for CSV from ConfigDWORD.
    static LPCWSTR compJitTimeLogFilename; // If a log file for JIT time is desired, filename to write it to.
#endif
    inline void EndPhase(Phases phase); // Indicate the end of the given phase.

#if defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)
    // These variables are associated with maintaining SQM data about compile time.
    unsigned __int64 m_compCyclesAtEndOfInlining; // The thread-virtualized cycle count at the end of the inlining phase
                                                  // in the current compilation.
    unsigned __int64 m_compCycles;                // Net cycle count for current compilation
    DWORD m_compTickCountAtEndOfInlining; // The result of GetTickCount() (# ms since some epoch marker) at the end of
                                          // the inlining phase in the current compilation.
#endif                                    // defined(DEBUG) || defined(INLINE_DATA) || defined(FEATURE_CLRSQM)

    // Records the SQM-relevant (cycles and tick count).  Should be called after inlining is complete.
    // (We do this after inlining because this marks the last point at which the JIT is likely to cause
    // type-loading and class initialization).
    void RecordStateAtEndOfInlining();
    // Assumes being called at the end of compilation.  Update the SQM state.
    void RecordStateAtEndOfCompilation();

#ifdef FEATURE_CLRSQM
    // Does anything SQM related necessary at process shutdown time.
    static void ProcessShutdownSQMWork(ICorStaticInfo* statInfo);
#endif // FEATURE_CLRSQM

public:
#if FUNC_INFO_LOGGING
    static LPCWSTR compJitFuncInfoFilename; // If a log file for per-function information is required, this is the
                                            // filename to write it to.
    static FILE* compJitFuncInfoFile;       // And this is the actual FILE* to write to.
#endif                                      // FUNC_INFO_LOGGING

    Compiler* prevCompiler; // Previous compiler on stack for TLS Compiler* linked list for reentrant compilers.

    // Is the compilation in a full trust context?
    bool compIsFullTrust();

#ifndef FEATURE_TRACELOGGING
    // Should we actually fire the noway assert body and the exception handler?
    bool compShouldThrowOnNoway();
#else  // FEATURE_TRACELOGGING
    // Should we actually fire the noway assert body and the exception handler?
    bool compShouldThrowOnNoway(const char* filename, unsigned line);

    // Telemetry instance to use per method compilation.
    JitTelemetry compJitTelemetry;

    // Get common parameters that have to be logged with most telemetry data.
    void compGetTelemetryDefaults(const char** assemblyName,
                                  const char** scopeName,
                                  const char** methodName,
                                  unsigned*    methodHash);
#endif // !FEATURE_TRACELOGGING

#ifdef DEBUG
private:
    NodeToTestDataMap* m_nodeTestData;

    static const unsigned FIRST_LOOP_HOIST_CSE_CLASS = 1000;
    unsigned              m_loopHoistCSEClass; // LoopHoist test annotations turn into CSE requirements; we
                                               // label them with CSE Class #'s starting at FIRST_LOOP_HOIST_CSE_CLASS.
                                               // Current kept in this.
public:
    NodeToTestDataMap* GetNodeTestData()
    {
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_nodeTestData == nullptr)
        {
            compRoot->m_nodeTestData = new (getAllocatorDebugOnly()) NodeToTestDataMap(getAllocatorDebugOnly());
        }
        return compRoot->m_nodeTestData;
    }

    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, int, JitSimplerHashBehavior> NodeToIntMap;

    // Returns the set (i.e., the domain of the result map) of nodes that are keys in m_nodeTestData, and
    // currently occur in the AST graph.
    NodeToIntMap* FindReachableNodesInNodeTestData();

    // Node "from" is being eliminated, and being replaced by node "to".  If "from" had any associated
    // test data, associate that data with "to".
    void TransferTestDataToNode(GenTreePtr from, GenTreePtr to);

    // Requires that "to" is a clone of "from".  If any nodes in the "from" tree
    // have annotations, attach similar annotations to the corresponding nodes in "to".
    void CopyTestDataToCloneTree(GenTreePtr from, GenTreePtr to);

    // These are the methods that test that the various conditions implied by the
    // test attributes are satisfied.
    void JitTestCheckSSA(); // SSA builder tests.
    void JitTestCheckVN();  // Value numbering tests.
#endif                      // DEBUG

    // The "FieldSeqStore", for canonicalizing field sequences.  See the definition of FieldSeqStore for
    // operations.
    FieldSeqStore* m_fieldSeqStore;

    FieldSeqStore* GetFieldSeqStore()
    {
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_fieldSeqStore == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_FieldSeqStore, and use that for allocation.
            IAllocator* ialloc        = new (this, CMK_FieldSeqStore) CompAllocator(this, CMK_FieldSeqStore);
            compRoot->m_fieldSeqStore = new (ialloc) FieldSeqStore(ialloc);
        }
        return compRoot->m_fieldSeqStore;
    }

    typedef SimplerHashTable<GenTreePtr, PtrKeyFuncs<GenTree>, FieldSeqNode*, JitSimplerHashBehavior> NodeToFieldSeqMap;

    // Some nodes of "TYP_BYREF" or "TYP_I_IMPL" actually represent the address of a field within a struct, but since
    // the offset of the field is zero, there's no "GT_ADD" node.  We normally attach a field sequence to the constant
    // that is added, but what do we do when that constant is zero, and is thus not present?  We use this mechanism to
    // attach the field sequence directly to the address node.
    NodeToFieldSeqMap* m_zeroOffsetFieldMap;

    NodeToFieldSeqMap* GetZeroOffsetFieldMap()
    {
        // Don't need to worry about inlining here
        if (m_zeroOffsetFieldMap == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_ZeroOffsetFieldMap, and use that for
            // allocation.
            IAllocator* ialloc   = new (this, CMK_ZeroOffsetFieldMap) CompAllocator(this, CMK_ZeroOffsetFieldMap);
            m_zeroOffsetFieldMap = new (ialloc) NodeToFieldSeqMap(ialloc);
        }
        return m_zeroOffsetFieldMap;
    }

    // Requires that "op1" is a node of type "TYP_BYREF" or "TYP_I_IMPL".  We are dereferencing this with the fields in
    // "fieldSeq", whose offsets are required all to be zero.  Ensures that any field sequence annotation currently on
    // "op1" or its components is augmented by appending "fieldSeq".  In practice, if "op1" is a GT_LCL_FLD, it has
    // a field sequence as a member; otherwise, it may be the addition of an a byref and a constant, where the const
    // has a field sequence -- in this case "fieldSeq" is appended to that of the constant; otherwise, we
    // record the the field sequence using the ZeroOffsetFieldMap described above.
    //
    // One exception above is that "op1" is a node of type "TYP_REF" where "op1" is a GT_LCL_VAR.
    // This happens when System.Object vtable pointer is a regular field at offset 0 in System.Private.CoreLib in
    // CoreRT. Such case is handled same as the default case.
    void fgAddFieldSeqForZeroOffset(GenTreePtr op1, FieldSeqNode* fieldSeq);

    typedef SimplerHashTable<const GenTree*, PtrKeyFuncs<GenTree>, ArrayInfo, JitSimplerHashBehavior>
                        NodeToArrayInfoMap;
    NodeToArrayInfoMap* m_arrayInfoMap;

    NodeToArrayInfoMap* GetArrayInfoMap()
    {
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_arrayInfoMap == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_ArrayInfoMap, and use that for allocation.
            IAllocator* ialloc       = new (this, CMK_ArrayInfoMap) CompAllocator(this, CMK_ArrayInfoMap);
            compRoot->m_arrayInfoMap = new (ialloc) NodeToArrayInfoMap(ialloc);
        }
        return compRoot->m_arrayInfoMap;
    }

    NodeToUnsignedMap* m_heapSsaMap;

    // In some cases, we want to assign intermediate SSA #'s to heap states, and know what nodes create those heap
    // states. (We do this for try blocks, where, if the try block doesn't do a call that loses track of the heap state,
    // all the possible heap states are possible initial states of the corresponding catch block(s).)
    NodeToUnsignedMap* GetHeapSsaMap()
    {
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_heapSsaMap == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_ArrayInfoMap, and use that for allocation.
            IAllocator* ialloc     = new (this, CMK_ArrayInfoMap) CompAllocator(this, CMK_ArrayInfoMap);
            compRoot->m_heapSsaMap = new (ialloc) NodeToUnsignedMap(ialloc);
        }
        return compRoot->m_heapSsaMap;
    }

    // The Refany type is the only struct type whose structure is implicitly assumed by IL.  We need its fields.
    CORINFO_CLASS_HANDLE m_refAnyClass;
    CORINFO_FIELD_HANDLE GetRefanyDataField()
    {
        if (m_refAnyClass == nullptr)
        {
            m_refAnyClass = info.compCompHnd->getBuiltinClass(CLASSID_TYPED_BYREF);
        }
        return info.compCompHnd->getFieldInClass(m_refAnyClass, 0);
    }
    CORINFO_FIELD_HANDLE GetRefanyTypeField()
    {
        if (m_refAnyClass == nullptr)
        {
            m_refAnyClass = info.compCompHnd->getBuiltinClass(CLASSID_TYPED_BYREF);
        }
        return info.compCompHnd->getFieldInClass(m_refAnyClass, 1);
    }

#if VARSET_COUNTOPS
    static BitSetSupport::BitSetOpCounter m_varsetOpCounter;
#endif
#if ALLVARSET_COUNTOPS
    static BitSetSupport::BitSetOpCounter m_allvarsetOpCounter;
#endif

    static HelperCallProperties s_helperCallProperties;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    static var_types GetTypeFromClassificationAndSizes(SystemVClassificationType classType, int size);
    static var_types GetEightByteType(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                      unsigned                                                   slotNum);
    static void GetStructTypeOffset(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                    var_types*                                                 type0,
                                    var_types*                                                 type1,
                                    unsigned __int8*                                           offset0,
                                    unsigned __int8*                                           offset1);
    void fgMorphSystemVStructArgs(GenTreeCall* call, bool hasStructArgument);
#endif // defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    void fgMorphMultiregStructArgs(GenTreeCall* call);
    GenTreePtr fgMorphMultiregStructArg(GenTreePtr arg, fgArgTabEntryPtr fgEntryPtr);

}; // end of class Compiler

// Inline methods of CompAllocator.
void* CompAllocator::Alloc(size_t sz)
{
#if MEASURE_MEM_ALLOC
    return m_comp->compGetMem(sz, m_cmk);
#else
    return m_comp->compGetMem(sz);
#endif
}

void* CompAllocator::ArrayAlloc(size_t elems, size_t elemSize)
{
#if MEASURE_MEM_ALLOC
    return m_comp->compGetMemArray(elems, elemSize, m_cmk);
#else
    return m_comp->compGetMemArray(elems, elemSize);
#endif
}

// LclVarDsc constructor. Uses Compiler, so must come after Compiler definition.
inline LclVarDsc::LclVarDsc(Compiler* comp)
    : // Initialize the ArgRegs to REG_STK.
    // The morph will do the right thing to change
    // to the right register if passed in register.
    _lvArgReg(REG_STK)
    ,
#if FEATURE_MULTIREG_ARGS
    _lvOtherArgReg(REG_STK)
    ,
#endif // FEATURE_MULTIREG_ARGS
#if ASSERTION_PROP
    lvRefBlks(BlockSetOps::UninitVal())
    ,
#endif // ASSERTION_PROP
    lvPerSsaData(comp->getAllocator())
{
}

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                   Miscellaneous Compiler stuff                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

// Values used to mark the types a stack slot is used for

const unsigned TYPE_REF_INT      = 0x01; // slot used as a 32-bit int
const unsigned TYPE_REF_LNG      = 0x02; // slot used as a 64-bit long
const unsigned TYPE_REF_FLT      = 0x04; // slot used as a 32-bit float
const unsigned TYPE_REF_DBL      = 0x08; // slot used as a 64-bit float
const unsigned TYPE_REF_PTR      = 0x10; // slot used as a 32-bit pointer
const unsigned TYPE_REF_BYR      = 0x20; // slot used as a byref pointer
const unsigned TYPE_REF_STC      = 0x40; // slot used as a struct
const unsigned TYPE_REF_TYPEMASK = 0x7F; // bits that represent the type

// const unsigned TYPE_REF_ADDR_TAKEN  = 0x80; // slots address was taken

/*****************************************************************************
 *
 *  Variables to keep track of total code amounts.
 */

#if DISPLAY_SIZES

extern size_t grossVMsize;
extern size_t grossNCsize;
extern size_t totalNCsize;

extern unsigned genMethodICnt;
extern unsigned genMethodNCnt;
extern size_t   gcHeaderISize;
extern size_t   gcPtrMapISize;
extern size_t   gcHeaderNSize;
extern size_t   gcPtrMapNSize;

#endif // DISPLAY_SIZES

/*****************************************************************************
 *
 *  Variables to keep track of basic block counts (more data on 1 BB methods)
 */

#if COUNT_BASIC_BLOCKS
extern Histogram bbCntTable;
extern Histogram bbOneBBSizeTable;
#endif

/*****************************************************************************
 *
 *  Used by optFindNaturalLoops to gather statistical information such as
 *   - total number of natural loops
 *   - number of loops with 1, 2, ... exit conditions
 *   - number of loops that have an iterator (for like)
 *   - number of loops that have a constant iterator
 */

#if COUNT_LOOPS

extern unsigned totalLoopMethods;        // counts the total number of methods that have natural loops
extern unsigned maxLoopsPerMethod;       // counts the maximum number of loops a method has
extern unsigned totalLoopOverflows;      // # of methods that identified more loops than we can represent
extern unsigned totalLoopCount;          // counts the total number of natural loops
extern unsigned totalUnnatLoopCount;     // counts the total number of (not-necessarily natural) loops
extern unsigned totalUnnatLoopOverflows; // # of methods that identified more unnatural loops than we can represent
extern unsigned iterLoopCount;           // counts the # of loops with an iterator (for like)
extern unsigned simpleTestLoopCount;     // counts the # of loops with an iterator and a simple loop condition (iter <
                                         // const)
extern unsigned  constIterLoopCount;     // counts the # of loops with a constant iterator (for like)
extern bool      hasMethodLoops;         // flag to keep track if we already counted a method as having loops
extern unsigned  loopsThisMethod;        // counts the number of loops in the current method
extern bool      loopOverflowThisMethod; // True if we exceeded the max # of loops in the method.
extern Histogram loopCountTable;         // Histogram of loop counts
extern Histogram loopExitCountTable;     // Histogram of loop exit counts

#endif // COUNT_LOOPS

/*****************************************************************************
 * variables to keep track of how many iterations we go in a dataflow pass
 */

#if DATAFLOW_ITER

extern unsigned CSEiterCount; // counts the # of iteration for the CSE dataflow
extern unsigned CFiterCount;  // counts the # of iteration for the Const Folding dataflow

#endif // DATAFLOW_ITER

#if MEASURE_BLOCK_SIZE
extern size_t genFlowNodeSize;
extern size_t genFlowNodeCnt;
#endif // MEASURE_BLOCK_SIZE

#if MEASURE_NODE_SIZE
struct NodeSizeStats
{
    void Init()
    {
        genTreeNodeCnt        = 0;
        genTreeNodeSize       = 0;
        genTreeNodeActualSize = 0;
    }

    size_t genTreeNodeCnt;
    size_t genTreeNodeSize;       // The size we allocate
    size_t genTreeNodeActualSize; // The actual size of the node. Note that the actual size will likely be smaller
                                  //   than the allocated size, but we sometimes use SetOper()/ChangeOper() to change
                                  //   a smaller node to a larger one. TODO-Cleanup: add stats on
                                  //   SetOper()/ChangeOper() usage to quanitfy this.
};
extern NodeSizeStats genNodeSizeStats;        // Total node size stats
extern NodeSizeStats genNodeSizeStatsPerFunc; // Per-function node size stats
extern Histogram     genTreeNcntHist;
extern Histogram     genTreeNsizHist;
#endif // MEASURE_NODE_SIZE

/*****************************************************************************
 *  Count fatal errors (including noway_asserts).
 */

#if MEASURE_FATAL
extern unsigned fatal_badCode;
extern unsigned fatal_noWay;
extern unsigned fatal_NOMEM;
extern unsigned fatal_noWayAssertBody;
#ifdef DEBUG
extern unsigned fatal_noWayAssertBodyArgs;
#endif // DEBUG
extern unsigned fatal_NYI;
#endif // MEASURE_FATAL

/*****************************************************************************
 * Codegen
 */

#ifdef _TARGET_XARCH_

const instruction INS_SHIFT_LEFT_LOGICAL  = INS_shl;
const instruction INS_SHIFT_RIGHT_LOGICAL = INS_shr;
const instruction INS_SHIFT_RIGHT_ARITHM  = INS_sar;

const instruction INS_AND             = INS_and;
const instruction INS_OR              = INS_or;
const instruction INS_XOR             = INS_xor;
const instruction INS_NEG             = INS_neg;
const instruction INS_TEST            = INS_test;
const instruction INS_MUL             = INS_imul;
const instruction INS_SIGNED_DIVIDE   = INS_idiv;
const instruction INS_UNSIGNED_DIVIDE = INS_div;
const instruction INS_BREAKPOINT      = INS_int3;
const instruction INS_ADDC            = INS_adc;
const instruction INS_SUBC            = INS_sbb;
const instruction INS_NOT             = INS_not;

#endif

#ifdef _TARGET_ARM_

const instruction INS_SHIFT_LEFT_LOGICAL  = INS_lsl;
const instruction INS_SHIFT_RIGHT_LOGICAL = INS_lsr;
const instruction INS_SHIFT_RIGHT_ARITHM  = INS_asr;

const instruction INS_AND             = INS_and;
const instruction INS_OR              = INS_orr;
const instruction INS_XOR             = INS_eor;
const instruction INS_NEG             = INS_rsb;
const instruction INS_TEST            = INS_tst;
const instruction INS_MUL             = INS_mul;
const instruction INS_SIGNED_DIVIDE   = INS_sdiv;
const instruction INS_UNSIGNED_DIVIDE = INS_udiv;
const instruction INS_BREAKPOINT      = INS_bkpt;
const instruction INS_ADDC            = INS_adc;
const instruction INS_SUBC            = INS_sbc;
const instruction INS_NOT             = INS_mvn;

#endif

#ifdef _TARGET_ARM64_

const instruction INS_SHIFT_LEFT_LOGICAL  = INS_lsl;
const instruction INS_SHIFT_RIGHT_LOGICAL = INS_lsr;
const instruction INS_SHIFT_RIGHT_ARITHM  = INS_asr;

const instruction INS_AND             = INS_and;
const instruction INS_OR              = INS_orr;
const instruction INS_XOR             = INS_eor;
const instruction INS_NEG             = INS_neg;
const instruction INS_TEST            = INS_tst;
const instruction INS_MUL             = INS_mul;
const instruction INS_SIGNED_DIVIDE   = INS_sdiv;
const instruction INS_UNSIGNED_DIVIDE = INS_udiv;
const instruction INS_BREAKPOINT      = INS_bkpt;
const instruction INS_ADDC            = INS_adc;
const instruction INS_SUBC            = INS_sbc;
const instruction INS_NOT             = INS_mvn;

#endif

/*****************************************************************************/

extern const BYTE genTypeSizes[];
extern const BYTE genTypeAlignments[];
extern const BYTE genTypeStSzs[];
extern const BYTE genActualTypes[];

/*****************************************************************************/

// VERY_LARGE_FRAME_SIZE_REG_MASK is the set of registers we need to use for
// the probing loop generated for very large stack frames (see `getVeryLargeFrameSize`).

#ifdef _TARGET_ARM_
#define VERY_LARGE_FRAME_SIZE_REG_MASK (RBM_R4 | RBM_R5 | RBM_R6)
#elif defined(_TARGET_ARM64_)
#define VERY_LARGE_FRAME_SIZE_REG_MASK (RBM_R9 | RBM_R10 | RBM_R11)
#endif

/*****************************************************************************/

#define REG_CORRUPT regNumber(REG_NA + 1)
#define RBM_CORRUPT (RBM_ILLEGAL | regMaskTP(1))
#define REG_PAIR_CORRUPT regPairNo(REG_PAIR_NONE + 1)

/*****************************************************************************/

extern BasicBlock dummyBB;

/*****************************************************************************/
/*****************************************************************************/

// foreach_treenode_execution_order: An iterator that iterates through all the tree
// nodes of a statement in execution order.
//      __stmt: a GT_STMT type GenTree*
//      __node: a GenTree*, already declared, that gets updated with each node in the statement, in execution order

#define foreach_treenode_execution_order(__node, __stmt)                                                               \
    for ((__node) = (__stmt)->gtStmt.gtStmtList; (__node); (__node) = (__node)->gtNext)

// foreach_block: An iterator over all blocks in the function.
//    __compiler: the Compiler* object
//    __block   : a BasicBlock*, already declared, that gets updated each iteration.

#define foreach_block(__compiler, __block)                                                                             \
    for ((__block) = (__compiler)->fgFirstBB; (__block); (__block) = (__block)->bbNext)

/*****************************************************************************/
/*****************************************************************************/

#ifdef DEBUG

void dumpConvertedVarSet(Compiler* comp, VARSET_VALARG_TP vars);

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          Debugging helpers                                XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
/* The following functions are intended to be called from the debugger, to dump
 * various data structures. The can be used in the debugger Watch or Quick Watch
 * windows. They are designed to be short to type and take as few arguments as
 * possible. The 'c' versions take a Compiler*, whereas the 'd' versions use the TlsCompiler.
 * See the function definition comment for more details.
 */

void cBlock(Compiler* comp, BasicBlock* block);
void cBlocks(Compiler* comp);
void cBlocksV(Compiler* comp);
void cTree(Compiler* comp, GenTree* tree);
void cTrees(Compiler* comp);
void cEH(Compiler* comp);
void cVar(Compiler* comp, unsigned lclNum);
void cVarDsc(Compiler* comp, LclVarDsc* varDsc);
void cVars(Compiler* comp);
void cVarsFinal(Compiler* comp);
void cBlockPreds(Compiler* comp, BasicBlock* block);
void cReach(Compiler* comp);
void cDoms(Compiler* comp);
void cLiveness(Compiler* comp);
void cCVarSet(Compiler* comp, VARSET_VALARG_TP vars);

void cFuncIR(Compiler* comp);
void cBlockIR(Compiler* comp, BasicBlock* block);
void cLoopIR(Compiler* comp, Compiler::LoopDsc* loop);
void cTreeIR(Compiler* comp, GenTree* tree);
int cTreeTypeIR(Compiler* comp, GenTree* tree);
int cTreeKindsIR(Compiler* comp, GenTree* tree);
int cTreeFlagsIR(Compiler* comp, GenTree* tree);
int cOperandIR(Compiler* comp, GenTree* operand);
int cLeafIR(Compiler* comp, GenTree* tree);
int cIndirIR(Compiler* comp, GenTree* tree);
int cListIR(Compiler* comp, GenTree* list);
int cSsaNumIR(Compiler* comp, GenTree* tree);
int cValNumIR(Compiler* comp, GenTree* tree);
int cDependsIR(Compiler* comp, GenTree* comma, bool* first);

void dBlock(BasicBlock* block);
void dBlocks();
void dBlocksV();
void dTree(GenTree* tree);
void dTrees();
void dEH();
void dVar(unsigned lclNum);
void dVarDsc(LclVarDsc* varDsc);
void dVars();
void dVarsFinal();
void dBlockPreds(BasicBlock* block);
void dReach();
void dDoms();
void dLiveness();
void dCVarSet(VARSET_VALARG_TP vars);

void dVarSet(VARSET_VALARG_TP vars);
void dRegMask(regMaskTP mask);

void dFuncIR();
void dBlockIR(BasicBlock* block);
void dTreeIR(GenTree* tree);
void dLoopIR(Compiler::LoopDsc* loop);
void dLoopNumIR(unsigned loopNum);
int dTabStopIR(int curr, int tabstop);
int dTreeTypeIR(GenTree* tree);
int dTreeKindsIR(GenTree* tree);
int dTreeFlagsIR(GenTree* tree);
int dOperandIR(GenTree* operand);
int dLeafIR(GenTree* tree);
int dIndirIR(GenTree* tree);
int dListIR(GenTree* list);
int dSsaNumIR(GenTree* tree);
int dValNumIR(GenTree* tree);
int dDependsIR(GenTree* comma);
void dFormatIR();

GenTree* dFindTree(GenTree* tree, unsigned id);
GenTree* dFindTree(unsigned id);
GenTreeStmt* dFindStmt(unsigned id);
BasicBlock* dFindBlock(unsigned bbNum);

#endif // DEBUG

#include "compiler.hpp" // All the shared inline functions

/*****************************************************************************/
#endif //_COMPILER_H_
/*****************************************************************************/
