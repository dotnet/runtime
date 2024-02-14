// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#include "jitstd.h"
#include "jithashtable.h"
#include "gentree.h"
#include "debuginfo.h"
#include "lir.h"
#include "block.h"
#include "inline.h"
#include "jiteh.h"
#include "instr.h"
#include "regalloc.h"
#include "sm.h"
#include "cycletimer.h"
#include "blockset.h"
#include "arraystack.h"
#include "hashbv.h"
#include "jitexpandarray.h"
#include "tinyarray.h"
#include "valuenum.h"
#include "namedintrinsiclist.h"
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

// Forward declaration
template <class T>
inline var_types genActualType(T value);

#include "hwintrinsic.h"
#include "simd.h"
#include "simdashwintrinsic.h"

/*****************************************************************************
 *                  Forward declarations
 */

struct InfoHdr;              // defined in GCInfo.h
struct escapeMapping_t;      // defined in fgdiagnostic.cpp
class emitter;               // defined in emit.h
struct ShadowParamVarInfo;   // defined in GSChecks.cpp
struct InitVarDscInfo;       // defined in registerargconvention.h
class FgStack;               // defined in fgbasic.cpp
class Instrumentor;          // defined in fgprofile.cpp
class SpanningTreeVisitor;   // defined in fgprofile.cpp
class CSE_DataFlow;          // defined in optcse.cpp
struct CSEdsc;               // defined in optcse.h
class CSE_HeuristicCommon;   // defined in optcse.h
class OptBoolsDsc;           // defined in optimizer.cpp
struct RelopImplicationInfo; // defined in redundantbranchopts.cpp
struct JumpThreadInfo;       // defined in redundantbranchopts.cpp
class ProfileSynthesis;      // defined in profilesynthesis.h
#ifdef DEBUG
struct IndentStack;
#endif

class Lowering; // defined in lower.h

// The following are defined in this file, Compiler.h

class Compiler;

/*****************************************************************************
 *                  Unwind info
 */

#include "unwind.h"

/*****************************************************************************/

//
// Declare global operator new overloads that use the compiler's arena allocator
//

void* operator new(size_t n, Compiler* context, CompMemKind cmk);
void* operator new[](size_t n, Compiler* context, CompMemKind cmk);

// Requires the definitions of "operator new" so including "LoopCloning.h" after the definitions.
#include "loopcloning.h"

/*****************************************************************************/

/* This is included here and not earlier as it needs the definition of "CSE"
 * which is defined in the section above */

/*****************************************************************************/

unsigned genLog2(unsigned value);
unsigned genLog2(unsigned __int64 value);

unsigned ReinterpretHexAsDecimal(unsigned in);

/*****************************************************************************/

const unsigned FLG_CCTOR = (CORINFO_FLG_CONSTRUCTOR | CORINFO_FLG_STATIC);

#ifdef DEBUG
const int BAD_STK_OFFS = 0xBAADF00D; // for LclVarDsc::lvStkOffs
#endif

//------------------------------------------------------------------------
// HFA info shared by LclVarDsc and CallArgABIInformation
//------------------------------------------------------------------------
inline bool IsHfa(CorInfoHFAElemType kind)
{
    return kind != CORINFO_HFA_ELEM_NONE;
}
inline var_types HfaTypeFromElemKind(CorInfoHFAElemType kind)
{
    switch (kind)
    {
        case CORINFO_HFA_ELEM_FLOAT:
            return TYP_FLOAT;
        case CORINFO_HFA_ELEM_DOUBLE:
            return TYP_DOUBLE;
#ifdef FEATURE_SIMD
        case CORINFO_HFA_ELEM_VECTOR64:
            return TYP_SIMD8;
        case CORINFO_HFA_ELEM_VECTOR128:
            return TYP_SIMD16;
#endif
        case CORINFO_HFA_ELEM_NONE:
            return TYP_UNDEF;
        default:
            assert(!"Invalid HfaElemKind");
            return TYP_UNDEF;
    }
}
inline CorInfoHFAElemType HfaElemKindFromType(var_types type)
{
    switch (type)
    {
        case TYP_FLOAT:
            return CORINFO_HFA_ELEM_FLOAT;
        case TYP_DOUBLE:
            return CORINFO_HFA_ELEM_DOUBLE;
#ifdef FEATURE_SIMD
        case TYP_SIMD8:
            return CORINFO_HFA_ELEM_VECTOR64;
        case TYP_SIMD16:
            return CORINFO_HFA_ELEM_VECTOR128;
#endif
        case TYP_UNDEF:
            return CORINFO_HFA_ELEM_NONE;
        default:
            assert(!"Invalid HFA Type");
            return CORINFO_HFA_ELEM_NONE;
    }
}

// The following holds the Local var info (scope information)
typedef const char* VarName; // Actual ASCII string
struct VarScopeDsc
{
    unsigned vsdVarNum; // (remapped) LclVarDsc number
    unsigned vsdLVnum;  // 'which' in eeGetLVinfo().
                        // Also, it is the index of this entry in the info.compVarScopes array,
                        // which is useful since the array is also accessed via the
                        // compEnterScopeList and compExitScopeList sorted arrays.

    IL_OFFSET vsdLifeBeg; // instr offset of beg of life
    IL_OFFSET vsdLifeEnd; // instr offset of end of life

#ifdef DEBUG
    VarName vsdName; // name of the var
#endif
};

// This class stores information associated with a LclVar SSA definition.
class LclSsaVarDsc
{
    // The basic block where the definition occurs. Definitions of uninitialized variables
    // are considered to occur at the start of the first basic block (fgFirstBB).
    //
    // TODO-Cleanup: In the case of uninitialized variables the block is set to nullptr by
    // SsaBuilder and changed to fgFirstBB during value numbering. It would be useful to
    // investigate and perhaps eliminate this rather unexpected behavior.
    BasicBlock* m_block = nullptr;
    // The store node that generates the definition, or nullptr for definitions
    // of uninitialized variables.
    GenTreeLclVarCommon* m_defNode = nullptr;
    // The SSA number associated with the previous definition for partial (GTF_USEASG) defs.
    unsigned m_useDefSsaNum = SsaConfig::RESERVED_SSA_NUM;
    // Number of uses of this SSA def (may be an over-estimate).
    // May not be accurate for for promoted fields.
    unsigned short m_numUses = 0;
    // True if there may be phi args uses of this def
    // May not be accurate for for promoted fields.
    // (false implies all uses are non-phi).
    bool m_hasPhiUse = false;
    // True if there may be uses of the def in a different block.
    // May not be accurate for for promoted fields.
    bool m_hasGlobalUse = false;

public:
    LclSsaVarDsc()
    {
    }

    LclSsaVarDsc(BasicBlock* block) : m_block(block)
    {
    }

    LclSsaVarDsc(BasicBlock* block, GenTreeLclVarCommon* defNode) : m_block(block)
    {
        SetDefNode(defNode);
    }

    BasicBlock* GetBlock() const
    {
        return m_block;
    }

    void SetBlock(BasicBlock* block)
    {
        m_block = block;
    }

    GenTreeLclVarCommon* GetDefNode() const
    {
        return m_defNode;
    }

    void SetDefNode(GenTreeLclVarCommon* defNode)
    {
        assert((defNode == nullptr) || defNode->OperIsLocalStore());
        m_defNode = defNode;
    }

    unsigned GetUseDefSsaNum() const
    {
        return m_useDefSsaNum;
    }

    void SetUseDefSsaNum(unsigned ssaNum)
    {
        m_useDefSsaNum = ssaNum;
    }

    unsigned GetNumUses() const
    {
        return m_numUses;
    }

    void AddUse(BasicBlock* block)
    {
        if (block != m_block)
        {
            m_hasGlobalUse = true;
        }

        if (m_numUses < USHRT_MAX)
        {
            m_numUses++;
        }
    }

    void AddPhiUse(BasicBlock* block)
    {
        m_hasPhiUse = true;
        AddUse(block);
    }

    bool HasPhiUse() const
    {
        return m_hasPhiUse;
    }

    bool HasGlobalUse() const
    {
        return m_hasGlobalUse;
    }

    ValueNumPair m_vnPair;
};

// This class stores information associated with a memory SSA definition.
class SsaMemDef
{
public:
    ValueNumPair m_vnPair;
};

//------------------------------------------------------------------------
// SsaDefArray: A resizable array of SSA definitions.
//
// Unlike an ordinary resizable array implementation, this allows only element
// addition (by calling AllocSsaNum) and has special handling for RESERVED_SSA_NUM
// (basically it's a 1-based array). The array doesn't impose any particular
// requirements on the elements it stores and AllocSsaNum forwards its arguments
// to the array element constructor, this way the array supports both LclSsaVarDsc
// and SsaMemDef elements.
//
template <typename T>
class SsaDefArray
{
    T*       m_array;
    unsigned m_arraySize;
    unsigned m_count;

    static_assert_no_msg(SsaConfig::RESERVED_SSA_NUM == 0);
    static_assert_no_msg(SsaConfig::FIRST_SSA_NUM == 1);

    // Get the minimum valid SSA number.
    unsigned GetMinSsaNum() const
    {
        return SsaConfig::FIRST_SSA_NUM;
    }

    // Increase (double) the size of the array.
    void GrowArray(CompAllocator alloc)
    {
        unsigned oldSize = m_arraySize;
        unsigned newSize = max(2, oldSize * 2);

        T* newArray = alloc.allocate<T>(newSize);

        for (unsigned i = 0; i < oldSize; i++)
        {
            newArray[i] = m_array[i];
        }

        m_array     = newArray;
        m_arraySize = newSize;
    }

public:
    // Construct an empty SsaDefArray.
    SsaDefArray() : m_array(nullptr), m_arraySize(0), m_count(0)
    {
    }

    // Reset the array (used only if the SSA form is reconstructed).
    void Reset()
    {
        m_count = 0;
    }

    // Allocate a new SSA number (starting with SsaConfig::FIRST_SSA_NUM).
    template <class... Args>
    unsigned AllocSsaNum(CompAllocator alloc, Args&&... args)
    {
        if (m_count == m_arraySize)
        {
            GrowArray(alloc);
        }

        unsigned ssaNum    = GetMinSsaNum() + m_count;
        m_array[m_count++] = T(std::forward<Args>(args)...);

        // Ensure that the first SSA number we allocate is SsaConfig::FIRST_SSA_NUM
        assert((ssaNum == SsaConfig::FIRST_SSA_NUM) || (m_count > 1));

        return ssaNum;
    }

    // Get the number of SSA definitions in the array.
    unsigned GetCount() const
    {
        return m_count;
    }

    // Get a pointer to the SSA definition at the specified index.
    T* GetSsaDefByIndex(unsigned index) const
    {
        assert(index < m_count);
        return &m_array[index];
    }

    // Check if the specified SSA number is valid.
    bool IsValidSsaNum(unsigned ssaNum) const
    {
        return (GetMinSsaNum() <= ssaNum) && (ssaNum < (GetMinSsaNum() + m_count));
    }

    // Get a pointer to the SSA definition associated with the specified SSA number.
    T* GetSsaDef(unsigned ssaNum) const
    {
        assert(ssaNum != SsaConfig::RESERVED_SSA_NUM);
        return GetSsaDefByIndex(ssaNum - GetMinSsaNum());
    }

    // Get an SSA number associated with the specified SSA def (that must be in this array).
    unsigned GetSsaNum(T* ssaDef) const
    {
        assert((m_array <= ssaDef) && (ssaDef < &m_array[m_count]));
        return GetMinSsaNum() + static_cast<unsigned>(ssaDef - &m_array[0]);
    }
};

enum RefCountState
{
    RCS_INVALID, // not valid to get/set ref counts
    RCS_EARLY,   // early counts for struct promotion and struct passing
    RCS_NORMAL,  // normal ref counts (from lvaMarkRefs onward)
};

#ifdef DEBUG
// Reasons why we can't enregister a local.
enum class DoNotEnregisterReason
{
    None,
    AddrExposed,      // the address of this local is exposed.
    DontEnregStructs, // struct enregistration is disabled.
    NotRegSizeStruct, // the struct size does not much any register size, usually the struct size is too big.
    LocalField,       // the local is accessed with LCL_FLD, note we can do it not only for struct locals.
    VMNeedsStackAddr,
    LiveInOutOfHandler, // the local is alive in and out of exception handler and not single def.
    BlockOp,            // Is read or written via a block operation.
    IsStructArg,        // Is a struct passed as an argument in a way that requires a stack location.
    DepField,           // It is a field of a dependently promoted struct
    NoRegVars,          // opts.compFlags & CLFLG_REGVAR is not set
    MinOptsGC,          // It is a GC Ref and we are compiling MinOpts
#if !defined(TARGET_64BIT)
    LongParamField, // It is a decomposed field of a long parameter.
#endif
#ifdef JIT32_GCENCODER
    PinningRef,
#endif
    LclAddrNode, // the local is accessed with LCL_ADDR_VAR/FLD.
    CastTakesAddr,
    StoreBlkSrc,          // the local is used as STORE_BLK source.
    SwizzleArg,           // the local is passed using LCL_FLD as another type.
    BlockOpRet,           // the struct is returned and it promoted or there is a cast.
    ReturnSpCheck,        // the local is used to do SP check on return from function
    CallSpCheck,          // the local is used to do SP check on every call
    SimdUserForcesDep,    // a promoted struct was used by a SIMD/HWI node; it must be dependently promoted
    HiddenBufferStructArg // the argument is a hidden return buffer passed to a method.
};

enum class AddressExposedReason
{
    NONE,
    PARENT_EXPOSED,                // This is a promoted field but the parent is exposed.
    TOO_CONSERVATIVE,              // Were marked as exposed to be conservative, fix these places.
    ESCAPE_ADDRESS,                // The address is escaping, for example, passed as call argument.
    WIDE_INDIR,                    // We access via indirection with wider type.
    OSR_EXPOSED,                   // It was exposed in the original method, osr has to repeat it.
    STRESS_LCL_FLD,                // Stress mode replaces localVar with localFld and makes them addrExposed.
    DISPATCH_RET_BUF,              // Caller return buffer dispatch.
    STRESS_POISON_IMPLICIT_BYREFS, // This is an implicit byref we want to poison.
    EXTERNALLY_VISIBLE_IMPLICITLY, // Local is visible externally without explicit escape in JIT IR.
                                   // For example because it is used by GC or is the outgoing arg area
                                   // that belongs to callees.
};

#endif // DEBUG

class LclVarDsc
{
public:
    // The constructor. Most things can just be zero'ed.
    //
    // Initialize the ArgRegs to REG_STK.
    // Morph will update if this local is passed in a register.
    LclVarDsc()
        : _lvArgReg(REG_STK)
#if FEATURE_MULTIREG_ARGS
        , _lvOtherArgReg(REG_STK)
#endif // FEATURE_MULTIREG_ARGS
        , lvClassHnd(NO_CLASS_HANDLE)
        , lvPerSsaData()
    {
    }

    // note this only packs because var_types is a typedef of unsigned char
    var_types lvType : 5; // TYP_INT/LONG/FLOAT/DOUBLE/REF

    unsigned char lvIsParam : 1;           // is this a parameter?
    unsigned char lvIsRegArg : 1;          // is this an argument that was passed by register?
    unsigned char lvFramePointerBased : 1; // 0 = off of REG_SPBASE (e.g., ESP), 1 = off of REG_FPBASE (e.g., EBP)

    unsigned char lvOnFrame : 1;  // (part of) the variable lives on the frame
    unsigned char lvRegister : 1; // assigned to live in a register? For RyuJIT backend, this is only set if the
                                  // variable is in the same register for the entire function.
    unsigned char lvTracked : 1;  // is this a tracked variable?
    bool          lvTrackedNonStruct()
    {
        return lvTracked && lvType != TYP_STRUCT;
    }
#ifdef DEBUG
    unsigned char lvTrackedWithoutIndex : 1; // Tracked but has no lvVarIndex (i.e. only valid GTF_VAR_DEATH flags, used
                                             // by physical promotion)
#endif
    unsigned char lvPinned : 1; // is this a pinned variable?

    unsigned char lvMustInit : 1; // must be initialized

private:
    bool m_addrExposed : 1; // The address of this variable is "exposed" -- passed as an argument, stored in a
                            // global location, etc.
                            // We cannot reason reliably about the value of the variable.
public:
    unsigned char lvDoNotEnregister : 1; // Do not enregister this variable.
    unsigned char lvFieldAccessed : 1;   // The var is a struct local, and a field of the variable is accessed.  Affects
                                         // struct promotion.
    unsigned char lvLiveInOutOfHndlr : 1; // The variable is live in or out of an exception handler, and therefore must
                                          // be on the stack (at least at those boundaries.)

    unsigned char lvInSsa : 1;       // The variable is in SSA form (set by SsaBuilder)
    unsigned char lvIsCSE : 1;       // Indicates if this LclVar is a CSE variable.
    unsigned char lvHasLdAddrOp : 1; // has ldloca or ldarga opcode on this local.

    unsigned char lvHasILStoreOp : 1;         // there is at least one STLOC or STARG on this local
    unsigned char lvHasMultipleILStoreOp : 1; // there is more than one STLOC on this local

    unsigned char lvIsTemp : 1; // Short-lifetime compiler temp

#if FEATURE_IMPLICIT_BYREFS
    // Set if the argument is an implicit byref.
    unsigned char lvIsImplicitByRef : 1;
    // Set if the local appears as a last use that will be passed as an implicit byref.
    unsigned char lvIsLastUseCopyOmissionCandidate : 1;
#endif // FEATURE_IMPLICIT_BYREFS

#if defined(TARGET_LOONGARCH64)
    unsigned char lvIs4Field1 : 1; // Set if the 1st field is int or float within struct for LA-ABI64.
    unsigned char lvIs4Field2 : 1; // Set if the 2nd field is int or float within struct for LA-ABI64.
    unsigned char lvIsSplit : 1;   // Set if the argument is splited.
#endif                             // defined(TARGET_LOONGARCH64)

#if defined(TARGET_RISCV64)
    unsigned char lvIs4Field1 : 1; // Set if the 1st field is int or float within struct for RISCV64.
    unsigned char lvIs4Field2 : 1; // Set if the 2nd field is int or float within struct for RISCV64.
    unsigned char lvIsSplit : 1;   // Set if the argument is splited.
#endif                             // defined(TARGET_RISCV64)

    unsigned char lvSingleDef : 1; // variable has a single def. Used to identify ref type locals that can get type
                                   // updates

    unsigned char lvSingleDefRegCandidate : 1; // variable has a single def and hence is a register candidate
                                               // Currently, this is only used to decide if an EH variable can be
                                               // a register candidate or not.

    unsigned char lvDisqualifySingleDefRegCandidate : 1; // tracks variable that are disqualified from register
                                                         // candidancy

    unsigned char lvSpillAtSingleDef : 1; // variable has a single def (as determined by LSRA interval scan)
                                          // and is spilled making it candidate to spill right after the
                                          // first (and only) definition.
                                          // Note: We cannot reuse lvSingleDefRegCandidate because it is set
                                          // in earlier phase and the information might not be appropriate
                                          // in LSRA.

    unsigned char lvHasExceptionalUsesHint : 1; // hint for CopyProp

#ifndef TARGET_64BIT
    unsigned char lvStructDoubleAlign : 1; // Must we double align this struct?
#endif                                     // !TARGET_64BIT
#ifdef TARGET_64BIT
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
                                  // 32-bit target.  For implicit byref parameters, this gets hijacked between
                                  // fgRetypeImplicitByRefArgs and fgMarkDemotedImplicitByRefArgs to indicate whether
                                  // references to the arg are being rewritten as references to a promoted shadow local.
    unsigned char lvIsStructField : 1; // Is this local var a field of a promoted struct local?
    unsigned char lvContainsHoles : 1; // Is this a promoted struct whose fields do not cover the struct local?

    // True for a promoted struct that has significant padding in it.
    // Significant padding is any data in the struct that is not covered by a
    // promoted field and that the EE told us we need to preserve on block
    // copies/inits.
    unsigned char lvAnySignificantPadding : 1;

    unsigned char lvIsMultiRegArg : 1; // true if this is a multireg LclVar struct used in an argument context
    unsigned char lvIsMultiRegRet : 1; // true if this is a multireg LclVar struct assigned from a multireg call

#ifdef DEBUG
    unsigned char lvHiddenBufferStructArg : 1; // True when this struct (or its field) are passed as hidden buffer
                                               // pointer.
#endif

#ifdef FEATURE_HFA_FIELDS_PRESENT
    CorInfoHFAElemType _lvHfaElemKind : 3; // What kind of an HFA this is (CORINFO_HFA_ELEM_NONE if it is not an HFA).
#endif                                     // FEATURE_HFA_FIELDS_PRESENT

#ifdef DEBUG
    // TODO-Cleanup: See the note on lvSize() - this flag is only in use by asserts that are checking for struct
    // types, and is needed because of cases where TYP_STRUCT is bashed to an integral type.
    // Consider cleaning this up so this workaround is not required.
    unsigned char lvUnusedStruct : 1; // All references to this promoted struct are through its field locals.
                                      // I.e. there is no longer any reference to the struct directly.
                                      // In this case we can simply remove this struct local.

    unsigned char lvUndoneStructPromotion : 1; // The struct promotion was undone and hence there should be no
                                               // reference to the fields of this struct.
#endif

    unsigned char lvLRACandidate : 1; // Tracked for linear scan register allocation purposes

#ifdef FEATURE_SIMD
    unsigned char lvUsedInSIMDIntrinsic : 1; // This tells lclvar is used for simd intrinsic
#endif                                       // FEATURE_SIMD

    unsigned char lvRegStruct : 1; // This is a reg-sized non-field-addressed struct.

    unsigned char lvClassIsExact : 1; // lvClassHandle is the exact type

#ifdef DEBUG
    unsigned char lvClassInfoUpdated : 1; // true if this var has updated class handle or exactness
    unsigned char lvIsHoist : 1;          // CSE temp for a hoisted tree
    unsigned char lvIsMultiDefCSE : 1;    // CSE temp for a multi-def CSE
#endif

    unsigned char lvImplicitlyReferenced : 1; // true if there are non-IR references to this local (prolog, epilog, gc,
                                              // eh)

    unsigned char lvSuppressedZeroInit : 1; // local needs zero init if we transform tail call to loop

    unsigned char lvHasExplicitInit : 1; // The local is explicitly initialized and doesn't need zero initialization in
                                         // the prolog. If the local has gc pointers, there are no gc-safe points
                                         // between the prolog and the explicit initialization.

    unsigned char lvIsOSRLocal : 1; // Root method local in an OSR method. Any stack home will be on the Tier0 frame.
                                    // Initial value will be defined by Tier0. Requires special handing in prolog.

    unsigned char lvIsOSRExposedLocal : 1; // OSR local that was address exposed in Tier0

    unsigned char lvRedefinedInEmbeddedStatement : 1; // Local has redefinitions inside embedded statements that
                                                      // disqualify it from local copy prop.
private:
    unsigned char lvIsNeverNegative : 1; // The local is known to be never negative

    unsigned char lvIsSpan : 1; // The local is a Span<T>

public:
    union {
        unsigned lvFieldLclStart; // The index of the local var representing the first field in the promoted struct
                                  // local.  For implicit byref parameters, this gets hijacked between
                                  // fgRetypeImplicitByRefArgs and fgMarkDemotedImplicitByRefArgs to point to the
                                  // struct local created to model the parameter's struct promotion, if any.
        unsigned lvParentLcl; // The index of the local var representing the parent (i.e. the promoted struct local).
                              // Valid on promoted struct local fields.
    };

    unsigned char lvFieldCnt; //  Number of fields in the promoted VarDsc.
    unsigned char lvFldOffset;
    unsigned char lvFldOrdinal;

#ifdef DEBUG
    unsigned char lvSingleDefDisqualifyReason = 'H';
#endif

    unsigned char lvAllDefsAreNoGc : 1; // For pinned locals: true if all defs of this local are no-gc

#if FEATURE_MULTIREG_ARGS
    regNumber lvRegNumForSlot(unsigned slotNum)
    {
        if (slotNum == 0)
        {
            return (regNumber)_lvArgReg;
        }
        else if (slotNum == 1)
        {
            return GetOtherArgReg();
        }
        else
        {
            assert(false && "Invalid slotNum!");
        }

        unreached();
    }
#endif // FEATURE_MULTIREG_ARGS

    CorInfoHFAElemType GetLvHfaElemKind() const
    {
#ifdef FEATURE_HFA_FIELDS_PRESENT
        return _lvHfaElemKind;
#else
        NOWAY_MSG("GetLvHfaElemKind");
        return CORINFO_HFA_ELEM_NONE;
#endif // FEATURE_HFA_FIELDS_PRESENT
    }

    void SetLvHfaElemKind(CorInfoHFAElemType elemKind)
    {
#ifdef FEATURE_HFA_FIELDS_PRESENT
        _lvHfaElemKind = elemKind;
#else
        NOWAY_MSG("SetLvHfaElemKind");
#endif // FEATURE_HFA_FIELDS_PRESENT
    }

    bool lvIsHfa() const
    {
        if (GlobalJitOptions::compFeatureHfa)
        {
            return IsHfa(GetLvHfaElemKind());
        }
        else
        {
            return false;
        }
    }

    bool lvIsHfaRegArg() const
    {
        if (GlobalJitOptions::compFeatureHfa)
        {
            return lvIsRegArg && lvIsHfa();
        }
        else
        {
            return false;
        }
    }

    //------------------------------------------------------------------------------
    // lvHfaSlots: Get the number of slots used by an HFA local
    //
    // Return Value:
    //    On Arm64 - Returns 1-4 indicating the number of register slots used by the HFA
    //    On Arm32 - Returns the total number of single FP register slots used by the HFA, max is 8
    //
    unsigned lvHfaSlots() const
    {
        assert(lvIsHfa());
        assert(varTypeIsStruct(lvType));
        unsigned slots = 0;
#ifdef TARGET_ARM
        slots = lvExactSize() / sizeof(float);
        assert(slots <= 8);
#elif defined(TARGET_ARM64)
        switch (GetLvHfaElemKind())
        {
            case CORINFO_HFA_ELEM_NONE:
                assert(!"lvHfaSlots called for non-HFA");
                break;
            case CORINFO_HFA_ELEM_FLOAT:
                assert((lvExactSize() % 4) == 0);
                slots = lvExactSize() >> 2;
                break;
            case CORINFO_HFA_ELEM_DOUBLE:
            case CORINFO_HFA_ELEM_VECTOR64:
                assert((lvExactSize() % 8) == 0);
                slots = lvExactSize() >> 3;
                break;
            case CORINFO_HFA_ELEM_VECTOR128:
                assert((lvExactSize() % 16) == 0);
                slots = lvExactSize() >> 4;
                break;
            default:
                unreached();
        }
        assert(slots <= 4);
#endif //  TARGET_ARM64
        return slots;
    }

    // lvIsMultiRegArgOrRet()
    //     returns true if this is a multireg LclVar struct used in an argument context
    //               or if this is a multireg LclVar struct assigned from a multireg call
    bool lvIsMultiRegArgOrRet()
    {
        return lvIsMultiRegArg || lvIsMultiRegRet;
    }

#if defined(DEBUG)
private:
    DoNotEnregisterReason m_doNotEnregReason;

    AddressExposedReason m_addrExposedReason;

public:
    void SetDoNotEnregReason(DoNotEnregisterReason reason)
    {
        m_doNotEnregReason = reason;
    }

    DoNotEnregisterReason GetDoNotEnregReason() const
    {
        return m_doNotEnregReason;
    }

    AddressExposedReason GetAddrExposedReason() const
    {
        return m_addrExposedReason;
    }
#endif // DEBUG

public:
    void SetAddressExposed(bool value DEBUGARG(AddressExposedReason reason))
    {
        m_addrExposed = value;
        INDEBUG(m_addrExposedReason = reason);
    }

    void CleanAddressExposed()
    {
        m_addrExposed = false;
    }

    bool IsAddressExposed() const
    {
        return m_addrExposed;
    }

#ifdef DEBUG
    void SetHiddenBufferStructArg(char value)
    {
        lvHiddenBufferStructArg = value;
    }

    bool IsHiddenBufferStructArg() const
    {
        return lvHiddenBufferStructArg;
    }
#endif

private:
    regNumberSmall _lvRegNum; // Used to store the register this variable is in (or, the low register of a
                              // register pair). It is set during codegen any time the
                              // variable is enregistered (lvRegister is only set
                              // to non-zero if the variable gets the same register assignment for its entire
                              // lifetime).
#if !defined(TARGET_64BIT)
    regNumberSmall _lvOtherReg; // Used for "upper half" of long var.
#endif                          // !defined(TARGET_64BIT)

    regNumberSmall _lvArgReg; // The (first) register in which this argument is passed.

#if FEATURE_MULTIREG_ARGS
    regNumberSmall _lvOtherArgReg; // Used for the second part of the struct passed in a register.
                                   // Note this is defined but not used by ARM32
#endif                             // FEATURE_MULTIREG_ARGS

    regNumberSmall _lvArgInitReg; // the register into which the argument is moved at entry

public:
    // The register number is stored in a small format (8 bits), but the getters return and the setters take
    // a full-size (unsigned) format, to localize the casts here.

    /////////////////////

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

#if defined(TARGET_64BIT)

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
#else  // !TARGET_64BIT

    regNumber GetOtherReg() const
    {
        return (regNumber)_lvOtherReg;
    }

    void SetOtherReg(regNumber reg)
    {
        _lvOtherReg = (regNumberSmall)reg;
        assert(_lvOtherReg == reg);
    }
#endif // !TARGET_64BIT

    /////////////////////

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
    // Is this is a SIMD struct which is used for SIMD intrinsic?
    bool lvIsUsedInSIMDIntrinsic() const
    {
        return lvUsedInSIMDIntrinsic;
    }
#else
    bool lvIsUsedInSIMDIntrinsic() const
    {
        return false;
    }
#endif

    // Is this is local never negative?
    bool IsNeverNegative() const
    {
        return lvIsNeverNegative;
    }

    // Is this is local never negative?
    void SetIsNeverNegative(bool value)
    {
        lvIsNeverNegative = value;
    }

    // Is this is local a Span<T>?
    bool IsSpan() const
    {
        return lvIsSpan;
    }

    // Is this is local a Span<T>?
    void SetIsSpan(bool value)
    {
        lvIsSpan = value;
    }

    /////////////////////

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

    bool lvIsRegCandidate() const
    {
        return lvLRACandidate != 0;
    }

    bool lvIsInReg() const
    {
        return lvIsRegCandidate() && (GetRegNum() != REG_STK);
    }

    regMaskTP lvRegMask() const
    {
        regMaskTP regMask = RBM_NONE;
        if (GetRegNum() != REG_STK)
        {
            if (varTypeUsesFloatReg(this))
            {
                regMask = genRegMaskFloat(GetRegNum() ARM_ARG(TypeGet()));
            }
            else
            {
#ifdef TARGET_XARCH
                assert(varTypeUsesIntReg(this) || varTypeUsesMaskReg(this));
#else
                assert(varTypeUsesIntReg(this));
#endif

                regMask = genRegMask(GetRegNum());
            }
        }
        return regMask;
    }

    //-----------------------------------------------------------------------------
    // AllFieldDeathFlags: Get a bitset of flags that represents all fields dying.
    //
    // Returns:
    //    A bit mask that has GTF_VAR_FIELD_DEATH0 to GTF_VAR_FIELD_DEATH3 set,
    //    depending on how many fields this promoted local has.
    //
    // Remarks:
    //    Only usable for promoted locals.
    //
    GenTreeFlags AllFieldDeathFlags() const
    {
        assert(lvPromoted && (lvFieldCnt > 0) && (lvFieldCnt <= 4));
        GenTreeFlags flags = static_cast<GenTreeFlags>(((1 << lvFieldCnt) - 1) << FIELD_LAST_USE_SHIFT);
        assert((flags & ~GTF_VAR_DEATH_MASK) == 0);
        return flags;
    }

    //-----------------------------------------------------------------------------
    // FullDeathFlags: Get a bitset of flags that represents this local fully dying.
    //
    // Returns:
    //    For promoted locals, this returns AllFieldDeathFlags(). Otherwise
    //    returns GTF_VAR_DEATH.
    //
    GenTreeFlags FullDeathFlags() const
    {
        return lvPromoted ? AllFieldDeathFlags() : GTF_VAR_DEATH;
    }

    unsigned short lvVarIndex; // variable tracking index

private:
    unsigned short m_lvRefCnt; // unweighted (real) reference count.  For implicit by reference
                               // parameters, this gets hijacked from fgResetImplicitByRefRefCount
                               // through fgMarkDemotedImplicitByRefArgs, to provide a static
                               // appearance count (computed during address-exposed analysis)
                               // that fgMakeOutgoingStructArgCopy consults during global morph
                               // to determine if eliding its copy is legal.

    weight_t m_lvRefCntWtd; // weighted reference count

public:
    unsigned short lvRefCnt(RefCountState state = RCS_NORMAL) const;
    void incLvRefCnt(unsigned short delta, RefCountState state = RCS_NORMAL);
    void setLvRefCnt(unsigned short newValue, RefCountState state = RCS_NORMAL);
    void incLvRefCntSaturating(unsigned short delta, RefCountState state = RCS_NORMAL);

    weight_t lvRefCntWtd(RefCountState state = RCS_NORMAL) const;
    void incLvRefCntWtd(weight_t delta, RefCountState state = RCS_NORMAL);
    void setLvRefCntWtd(weight_t newValue, RefCountState state = RCS_NORMAL);

private:
    int lvStkOffs; // stack offset of home in bytes.

public:
    int GetStackOffset() const
    {
        return lvStkOffs;
    }

    void SetStackOffset(int offset)
    {
        lvStkOffs = offset;
    }

    unsigned lvExactSize() const;
    unsigned lvSize() const;

    size_t lvArgStackSize() const;

    unsigned lvSlotNum; // original slot # (if remapped)

    // class handle for the local or null if not known or not a class
    CORINFO_CLASS_HANDLE lvClassHnd;

private:
    ClassLayout* m_layout; // layout info for structs

public:
    var_types TypeGet() const
    {
        return (var_types)lvType;
    }
    bool lvStackAligned() const
    {
        assert(lvIsStructField);
        return ((lvFldOffset % TARGET_POINTER_SIZE) == 0);
    }

    // NormalizeOnLoad Rules:
    //     1. All small locals are actually TYP_INT locals.
    //     2. NOL locals are such that not all definitions can be controlled by the compiler and so the upper bits can
    //        be undefined.For parameters this is the case because of ABI.For struct fields - because of padding.For
    //        address - exposed locals - because not all stores are direct.
    //     3. Hence, all NOL uses(unless proven otherwise) are assumed in morph to have undefined upper bits and
    //        explicit casts have be inserted to "normalize" them back to conform to IL semantics.
    bool lvNormalizeOnLoad() const
    {
        return varTypeIsSmall(TypeGet()) &&
               // OSR exposed locals were normalize on load in the Tier0 frame so must be so for OSR too.
               (lvIsParam || m_addrExposed || lvIsStructField || lvIsOSRExposedLocal);
    }

    bool lvNormalizeOnStore() const
    {
        return varTypeIsSmall(TypeGet()) &&
               // OSR exposed locals were normalize on load in the Tier0 frame so must be so for OSR too.
               !(lvIsParam || m_addrExposed || lvIsStructField || lvIsOSRExposedLocal);
    }

    void incRefCnts(weight_t weight, Compiler* pComp, RefCountState state = RCS_NORMAL, bool propagate = true);

    var_types GetHfaType() const
    {
        if (GlobalJitOptions::compFeatureHfa)
        {
            assert(lvIsHfa());
            return HfaTypeFromElemKind(GetLvHfaElemKind());
        }
        else
        {
            return TYP_UNDEF;
        }
    }

    void SetHfaType(var_types type)
    {
        if (GlobalJitOptions::compFeatureHfa)
        {
            CorInfoHFAElemType elemKind = HfaElemKindFromType(type);
            SetLvHfaElemKind(elemKind);
            // Ensure we've allocated enough bits.
            assert(GetLvHfaElemKind() == elemKind);
        }
    }

    // Returns true if this variable contains GC pointers (including being a GC pointer itself).
    bool HasGCPtr() const
    {
        return varTypeIsGC(lvType) || ((lvType == TYP_STRUCT) && m_layout->HasGCPtr());
    }

    // Returns the layout of a struct variable or implicit byref.
    ClassLayout* GetLayout() const
    {
#if FEATURE_IMPLICIT_BYREFS
        assert(varTypeIsStruct(TypeGet()) || (lvIsImplicitByRef && (TypeGet() == TYP_BYREF)));
#else
        assert(varTypeIsStruct(TypeGet()));
#endif
        return m_layout;
    }

    // Sets the layout of a struct variable.
    void SetLayout(ClassLayout* layout)
    {
        assert(varTypeIsStruct(lvType));
        assert((m_layout == nullptr) || ClassLayout::AreCompatible(m_layout, layout));
        m_layout = layout;
    }

    // Grow the size of a block layout local.
    void GrowBlockLayout(ClassLayout* layout)
    {
        assert(varTypeIsStruct(lvType));
        assert((m_layout == nullptr) || (m_layout->IsBlockLayout() && (m_layout->GetSize() <= layout->GetSize())));
        assert(layout->IsBlockLayout());
        m_layout = layout;
    }

    SsaDefArray<LclSsaVarDsc> lvPerSsaData;

    // True if ssaNum is a viable ssaNum for this local.
    bool IsValidSsaNum(unsigned ssaNum) const
    {
        return lvPerSsaData.IsValidSsaNum(ssaNum);
    }

    // Returns the address of the per-Ssa data for the given ssaNum (which is required
    // not to be the SsaConfig::RESERVED_SSA_NUM, which indicates that the variable is
    // not an SSA variable).
    LclSsaVarDsc* GetPerSsaData(unsigned ssaNum) const
    {
        return lvPerSsaData.GetSsaDef(ssaNum);
    }

    // Returns the SSA number for "ssaDef". Requires "ssaDef" to be a valid definition
    // of this variable.
    unsigned GetSsaNumForSsaDef(LclSsaVarDsc* ssaDef)
    {
        return lvPerSsaData.GetSsaNum(ssaDef);
    }

    var_types GetRegisterType(const GenTreeLclVarCommon* tree) const;

    var_types GetRegisterType() const;

    var_types GetStackSlotHomeType() const;

    bool IsEnregisterableType() const
    {
        return GetRegisterType() != TYP_UNDEF;
    }

    bool IsEnregisterableLcl() const
    {
        if (lvDoNotEnregister)
        {
            return false;
        }
        return IsEnregisterableType();
    }

    //-----------------------------------------------------------------------------
    //  IsAlwaysAliveInMemory: Determines if this variable's value is always
    //     up-to-date on stack. This is possible if this is an EH-var or
    //     we decided to spill after single-def.
    //
    bool IsAlwaysAliveInMemory() const
    {
        return lvLiveInOutOfHndlr || lvSpillAtSingleDef;
    }

    bool CanBeReplacedWithItsField(Compiler* comp) const;

#ifdef DEBUG
public:
    const char* lvReason;

    void PrintVarReg() const
    {
        printf("%s", getRegName(GetRegNum()));
    }
#endif // DEBUG

}; // class LclVarDsc

enum class SymbolicIntegerValue : int32_t
{
    LongMin,
    IntMin,
    ShortMin,
    ByteMin,
    Zero,
    One,
    ByteMax,
    UByteMax,
    ShortMax,
    UShortMax,
    ArrayLenMax,
    IntMax,
    UIntMax,
    LongMax,
};

inline constexpr bool operator>(SymbolicIntegerValue left, SymbolicIntegerValue right)
{
    return static_cast<int32_t>(left) > static_cast<int32_t>(right);
}

inline constexpr bool operator>=(SymbolicIntegerValue left, SymbolicIntegerValue right)
{
    return static_cast<int32_t>(left) >= static_cast<int32_t>(right);
}

inline constexpr bool operator<(SymbolicIntegerValue left, SymbolicIntegerValue right)
{
    return static_cast<int32_t>(left) < static_cast<int32_t>(right);
}

inline constexpr bool operator<=(SymbolicIntegerValue left, SymbolicIntegerValue right)
{
    return static_cast<int32_t>(left) <= static_cast<int32_t>(right);
}

// Represents an integral range useful for reasoning about integral casts.
// It uses a symbolic representation for lower and upper bounds so
// that it can efficiently handle integers of all sizes on all hosts.
//
// Note that the ranges represented by this class are **always** in the
// "signed" domain. This is so that if we know the range a node produces, it
// can be trivially used to determine if a cast above the node does or does not
// overflow, which requires that the interpretation of integers be the same both
// for the "input" and "output". We choose signed interpretation here because it
// produces nice continuous ranges and because IR uses sign-extension for constants.
//
// Some examples of how ranges are computed for casts:
// 1. CAST_OVF(ubyte <- uint): does not overflow for [0..UBYTE_MAX], produces the
//    same range - all casts that do not change the representation, i. e. have the same
//    "actual" input and output type, have the same "input" and "output" range.
// 2. CAST_OVF(ulong <- uint): never overflows => the "input" range is [INT_MIN..INT_MAX]
//    (aka all possible 32 bit integers). Produces [0..UINT_MAX] (aka all possible 32
//    bit integers zero-extended to 64 bits).
// 3. CAST_OVF(int <- uint): overflows for inputs larger than INT_MAX <=> less than 0
//    when interpreting as signed => the "input" range is [0..INT_MAX], the same range
//    being the produced one as the node does not change the width of the integer.
//
class IntegralRange
{
private:
    SymbolicIntegerValue m_lowerBound;
    SymbolicIntegerValue m_upperBound;

public:
    IntegralRange() = default;

    IntegralRange(SymbolicIntegerValue lowerBound, SymbolicIntegerValue upperBound)
        : m_lowerBound(lowerBound), m_upperBound(upperBound)
    {
        assert(lowerBound <= upperBound);
    }

    SymbolicIntegerValue GetLowerBound() const
    {
        return m_lowerBound;
    }

    SymbolicIntegerValue GetUpperBound() const
    {
        return m_upperBound;
    }

    bool Contains(int64_t value) const;

    bool Contains(IntegralRange other) const
    {
        return (m_lowerBound <= other.m_lowerBound) && (other.m_upperBound <= m_upperBound);
    }

    bool IsNonNegative() const
    {
        return m_lowerBound >= SymbolicIntegerValue::Zero;
    }

    bool Equals(IntegralRange other) const
    {
        return (m_lowerBound == other.m_lowerBound) && (m_upperBound == other.m_upperBound);
    }

    static int64_t SymbolicToRealValue(SymbolicIntegerValue value);
    static SymbolicIntegerValue LowerBoundForType(var_types type);
    static SymbolicIntegerValue UpperBoundForType(var_types type);

    static IntegralRange ForType(var_types type)
    {
        return {LowerBoundForType(type), UpperBoundForType(type)};
    }

    static IntegralRange ForNode(GenTree* node, Compiler* compiler);
    static IntegralRange ForCastInput(GenTreeCast* cast);
    static IntegralRange ForCastOutput(GenTreeCast* cast, Compiler* compiler);
    static IntegralRange Union(IntegralRange range1, IntegralRange range2);

#ifdef DEBUG
    static void Print(IntegralRange range);
#endif // DEBUG
};

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
        // temps must have a negative number (so they have a different number from all local variables)
        assert(tdNum < 0);
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

// Specify compiler data that a phase might modify
enum class PhaseStatus : unsigned
{
    MODIFIED_NOTHING,    // Phase did not make any changes that warrant running post-phase checks or dumping
                         // the main jit data strutures.
    MODIFIED_EVERYTHING, // Phase made changes that warrant running post-phase checks or dumping
                         // the main jit data strutures.
};

// interface to hide linearscan implementation from rest of compiler
class LinearScanInterface
{
public:
    virtual PhaseStatus doLinearScan()                         = 0;
    virtual void recordVarLocationsAtStartOfBB(BasicBlock* bb) = 0;
    virtual bool willEnregisterLocalVars() const               = 0;
#if TRACK_LSRA_STATS
    virtual void dumpLsraStatsCsv(FILE* file)     = 0;
    virtual void dumpLsraStatsSummary(FILE* file) = 0;
#endif // TRACK_LSRA_STATS
};

LinearScanInterface* getLinearScanAllocator(Compiler* comp);

// This enumeration names the phases into which we divide compilation.  The phases should completely
// partition a compilation.
enum Phases
{
#define CompPhaseNameMacro(enum_nm, string_nm, hasChildren, parent, measureIR) enum_nm,
#include "compphases.h"
    PHASE_NUMBER_OF
};

extern const char* PhaseNames[];
extern const char* PhaseEnums[];

// Specify which checks should be run after each phase
//
// clang-format off
enum class PhaseChecks : unsigned int
{
    CHECK_NONE          = 0,
    CHECK_IR            = 1 << 0, // ir flags, etc
    CHECK_UNIQUE        = 1 << 1, // tree node uniqueness
    CHECK_FG            = 1 << 2, // flow graph integrity
    CHECK_EH            = 1 << 3, // eh table integrity
    CHECK_LOOPS         = 1 << 4, // loop integrity/canonicalization
    CHECK_PROFILE       = 1 << 5, // profile data integrity
    CHECK_LINKED_LOCALS = 1 << 6, // check linked list of locals
};

inline constexpr PhaseChecks operator ~(PhaseChecks a)
{
    return (PhaseChecks)(~(unsigned int)a);
}

inline constexpr PhaseChecks operator |(PhaseChecks a, PhaseChecks b)
{
    return (PhaseChecks)((unsigned int)a | (unsigned int)b);
}

inline constexpr PhaseChecks operator &(PhaseChecks a, PhaseChecks b)
{
    return (PhaseChecks)((unsigned int)a & (unsigned int)b);
}

inline PhaseChecks& operator |=(PhaseChecks& a, PhaseChecks b)
{
    return a = (PhaseChecks)((unsigned int)a | (unsigned int)b);
}

inline PhaseChecks& operator &=(PhaseChecks& a, PhaseChecks b)
{
    return a = (PhaseChecks)((unsigned int)a & (unsigned int)b);
}

inline PhaseChecks& operator ^=(PhaseChecks& a, PhaseChecks b)
{
    return a = (PhaseChecks)((unsigned int)a ^ (unsigned int)b);
}
// clang-format on

// Specify which dumps should be run after each phase
//
enum class PhaseDumps
{
    DUMP_NONE,
    DUMP_ALL
};

// The following enum provides a simple 1:1 mapping to CLR API's
enum API_ICorJitInfo_Names
{
#define DEF_CLR_API(name) API_##name,
#include "ICorJitInfo_names_generated.h"
    API_COUNT
};

// Profile checking options
//
// clang-format off
enum class ProfileChecks : unsigned int
{
    CHECK_NONE          = 0,
    CHECK_CLASSIC       = 1 << 0, // check "classic" jit weights
    CHECK_HASLIKELIHOOD = 1 << 1, // check all FlowEdges for hasLikelihood
    CHECK_LIKELY        = 1 << 2, // fully check likelihood based weights
    RAISE_ASSERT        = 1 << 3, // assert on check failure
    CHECK_ALL_BLOCKS    = 1 << 4, // check blocks even if bbHasProfileWeight is false
};

inline constexpr ProfileChecks operator ~(ProfileChecks a)
{
    return (ProfileChecks)(~(unsigned int)a);
}

inline constexpr ProfileChecks operator |(ProfileChecks a, ProfileChecks b)
{
    return (ProfileChecks)((unsigned int)a | (unsigned int)b);
}

inline constexpr ProfileChecks operator &(ProfileChecks a, ProfileChecks b)
{
    return (ProfileChecks)((unsigned int)a & (unsigned int)b);
}

inline ProfileChecks& operator |=(ProfileChecks& a, ProfileChecks b)
{
    return a = (ProfileChecks)((unsigned int)a | (unsigned int)b);
}

inline ProfileChecks& operator &=(ProfileChecks& a, ProfileChecks b)
{
    return a = (ProfileChecks)((unsigned int)a & (unsigned int)b);
}

inline ProfileChecks& operator ^=(ProfileChecks& a, ProfileChecks b)
{
    return a = (ProfileChecks)((unsigned int)a ^ (unsigned int)b);
}

inline bool hasFlag(const ProfileChecks& flagSet, const ProfileChecks& flag)
{
    return ((flagSet & flag) == flag);
}

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
    static bool PhaseReportsIRSize[];

    unsigned         m_byteCodeBytes;
    unsigned __int64 m_totalCycles;
    unsigned __int64 m_invokesByPhase[PHASE_NUMBER_OF];
    unsigned __int64 m_cyclesByPhase[PHASE_NUMBER_OF];
#if MEASURE_CLRAPI_CALLS
    unsigned __int64 m_CLRinvokesByPhase[PHASE_NUMBER_OF];
    unsigned __int64 m_CLRcyclesByPhase[PHASE_NUMBER_OF];
#endif

    unsigned m_nodeCountAfterPhase[PHASE_NUMBER_OF];

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

#if MEASURE_CLRAPI_CALLS
    // The following measures the time spent inside each individual CLR API call.
    unsigned         m_allClrAPIcalls;
    unsigned         m_perClrAPIcalls[API_ICorJitInfo_Names::API_COUNT];
    unsigned __int64 m_allClrAPIcycles;
    unsigned __int64 m_perClrAPIcycles[API_ICorJitInfo_Names::API_COUNT];
    unsigned __int32 m_maxClrAPIcycles[API_ICorJitInfo_Names::API_COUNT];
#endif // MEASURE_CLRAPI_CALLS

    CompTimeInfo(unsigned byteCodeBytes);
#endif
};

#ifdef FEATURE_JIT_METHOD_PERF

#if MEASURE_CLRAPI_CALLS
struct WrapICorJitInfo;
#endif

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
    int          m_totMethods;
    CompTimeInfo m_total;
    CompTimeInfo m_maximum;

    int          m_numFilteredMethods;
    CompTimeInfo m_filtered;

    // This can use what ever data you want to determine if the value to be added
    // belongs in the filtered section (it's always included in the unfiltered section)
    bool IncludedInFilteredData(CompTimeInfo& info);

public:
    // This is the unique CompTimeSummaryInfo object for this instance of the runtime.
    static CompTimeSummaryInfo s_compTimeSummary;

    CompTimeSummaryInfo()
        : m_numMethods(0), m_totMethods(0), m_total(0), m_maximum(0), m_numFilteredMethods(0), m_filtered(0)
    {
    }

    // Assumes that "info" is a completed CompTimeInfo for a compilation; adds it to the summary.
    // This is thread safe.
    void AddInfo(CompTimeInfo& info, bool includePhases);

    // Print the summary information to "f".
    // This is not thread-safe; assumed to be called by only one thread.
    void Print(FILE* f);
};

// A JitTimer encapsulates a CompTimeInfo for a single compilation. It also tracks the start of compilation,
// and when the current phase started.  This is intended to be part of a Compilation object.
//
class JitTimer
{
    unsigned __int64 m_start;         // Start of the compilation.
    unsigned __int64 m_curPhaseStart; // Start of the current phase.
#if MEASURE_CLRAPI_CALLS
    unsigned __int64 m_CLRcallStart;   // Start of the current CLR API call (if any).
    unsigned __int64 m_CLRcallInvokes; // CLR API invokes under current outer so far
    unsigned __int64 m_CLRcallCycles;  // CLR API  cycles under current outer so far.
    int              m_CLRcallAPInum;  // The enum/index of the current CLR API call (or -1).
    static double    s_cyclesPerSec;   // Cached for speedier measurements
#endif
#ifdef DEBUG
    Phases m_lastPhase; // The last phase that was completed (or (Phases)-1 to start).
#endif
    CompTimeInfo m_info; // The CompTimeInfo for this compilation.

    static CritSecObject s_csvLock; // Lock to protect the time log file.
    static FILE*         s_csvFile; // The time log file handle.
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
    void EndPhase(Compiler* compiler, Phases phase);

#if MEASURE_CLRAPI_CALLS
    // Start and end a timed CLR API call.
    void CLRApiCallEnter(unsigned apix);
    void CLRApiCallLeave(unsigned apix);
#endif // MEASURE_CLRAPI_CALLS

    // Completes the timing of the current method, which is assumed to have "byteCodeBytes" bytes of bytecode,
    // and adds it to "sum".
    void Terminate(Compiler* comp, CompTimeSummaryInfo& sum, bool includePhases);

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

    static void Shutdown();
};
#endif // FEATURE_JIT_METHOD_PERF

//------------------- Function/Funclet info -------------------------------
enum FuncKind : BYTE
{
    FUNC_ROOT,    // The main/root function (always id==0)
    FUNC_HANDLER, // a funclet associated with an EH handler (finally, fault, catch, filter handler)
    FUNC_FILTER,  // a funclet associated with an EH filter
    FUNC_COUNT
};

class emitLocation;

struct FuncInfoDsc
{
    FuncKind       funKind;
    BYTE           funFlags;   // Currently unused, just here for padding
    unsigned short funEHIndex; // index, into the ebd table, of innermost EH clause corresponding to this
                               // funclet. It is only valid if funKind field indicates this is a
                               // EH-related funclet: FUNC_HANDLER or FUNC_FILTER

#if defined(TARGET_AMD64)

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

#elif defined(TARGET_X86)

    emitLocation* startLoc;
    emitLocation* endLoc;
    emitLocation* coldStartLoc; // locations for the cold section, if there is one.
    emitLocation* coldEndLoc;

#elif defined(TARGET_ARMARCH) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)

    UnwindInfo  uwi;     // Unwind information for this function/funclet's hot  section
    UnwindInfo* uwiCold; // Unwind information for this function/funclet's cold section
                         //   Note: we only have a pointer here instead of the actual object,
                         //   to save memory in the JIT case (compared to the NGEN case),
                         //   where we don't have any cold section.
                         //   Note 2: we currently don't support hot/cold splitting in functions
                         //   with EH, so uwiCold will be NULL for all funclets.

    emitLocation* startLoc;
    emitLocation* endLoc;
    emitLocation* coldStartLoc; // locations for the cold section, if there is one.
    emitLocation* coldEndLoc;

#endif // TARGET_ARMARCH || TARGET_LOONGARCH64 || TARGET_RISCV64

#if defined(FEATURE_CFI_SUPPORT)
    jitstd::vector<CFI_CODE>* cfiCodes;
#endif // FEATURE_CFI_SUPPORT

    // Eventually we may want to move rsModifiedRegsMask, lvaOutgoingArgSize, and anything else
    // that isn't shared between the main function body and funclets.
};

struct TempInfo
{
    GenTree* store;
    GenTree* load;
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

typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, TestLabelAndNum> NodeToTestDataMap;

// XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
#endif // DEBUG

// Represents a depth-first search tree of the flow graph.
class FlowGraphDfsTree
{
    Compiler* m_comp;

    // Post-order that we saw reachable basic blocks in. This order can be
    // particularly useful to iterate in reverse, as reverse post-order ensures
    // that all predecessors are visited before successors whenever possible.
    BasicBlock** m_postOrder;
    unsigned m_postOrderCount;

    // Whether the DFS that produced the tree found any backedges.
    bool m_hasCycle;

public:
    FlowGraphDfsTree(Compiler* comp, BasicBlock** postOrder, unsigned postOrderCount, bool hasCycle)
        : m_comp(comp)
        , m_postOrder(postOrder)
        , m_postOrderCount(postOrderCount)
        , m_hasCycle(hasCycle)
    {
    }

    Compiler* GetCompiler() const
    {
        return m_comp;
    }

    BasicBlock** GetPostOrder() const
    {
        return m_postOrder;
    }

    unsigned GetPostOrderCount() const
    {
        return m_postOrderCount;
    }

    BasicBlock* GetPostOrder(unsigned index) const
    {
        assert(index < m_postOrderCount);
        return m_postOrder[index];
    }

    BitVecTraits PostOrderTraits() const
    {
        return BitVecTraits(m_postOrderCount, m_comp);
    }

    bool HasCycle() const
    {
        return m_hasCycle;
    }

    bool Contains(BasicBlock* block) const;
    bool IsAncestor(BasicBlock* ancestor, BasicBlock* descendant) const;
};

// Represents the result of induction variable analysis. See
// FlowGraphNaturalLoop::AnalyzeIteration.
struct NaturalLoopIterInfo
{
    // The local that is the induction variable.
    unsigned IterVar = BAD_VAR_NUM;

#ifdef DEBUG
    // Tree that initializes induction variable outside the loop.
    // Only valid if HasConstInit is true.
    GenTree* InitTree = nullptr;
#endif

    // Constant value that the induction variable is initialized with, outside
    // the loop. Only valid if HasConstInit is true.
    int ConstInitValue = 0;

    // Tree that has the loop test for the induction variable.
    GenTree* TestTree = nullptr;

    // Block that has the loop test.
    BasicBlock* TestBlock = nullptr;

    // Tree that mutates the induction variable.
    GenTree* IterTree = nullptr;

    // Is the loop exited when TestTree is true?
    bool ExitedOnTrue : 1;

    // Whether or not we found an initialization of the induction variable.
    bool HasConstInit : 1;

    // Whether or not the loop test compares the induction variable with a
    // constant value.
    bool HasConstLimit : 1;

    // Whether or not the loop test constant value is a SIMD vector element count.
    bool HasSimdLimit : 1;

    // Whether or not the loop test compares the induction variable with an
    // invariant local.
    bool HasInvariantLocalLimit : 1;

    // Whether or not the loop test compares the induction variable with the
    // length of an invariant array.
    bool HasArrayLengthLimit : 1;

    NaturalLoopIterInfo()
        : ExitedOnTrue(false)
        , HasConstInit(false)
        , HasConstLimit(false)
        , HasSimdLimit(false)
        , HasInvariantLocalLimit(false)
        , HasArrayLengthLimit(false)
    {
    }

    int IterConst();
    genTreeOps IterOper();
    var_types IterOperType();
    genTreeOps TestOper();
    bool IsIncreasingLoop();
    bool IsDecreasingLoop();
    GenTree* Iterator();
    GenTree* Limit();
    int ConstLimit();
    unsigned VarLimit();
    bool ArrLenLimit(Compiler* comp, ArrIndex* index);

private:
    bool IsReversed();
};

// Represents a natural loop in the flow graph. Natural loops are characterized
// by the following properties:
//
// * All loop blocks are strongly connected, meaning that every block of the
//   loop can reach every other block of the loop.
//
// * All loop blocks are dominated by the header block, i.e. the header block
//   is guaranteed to be entered on every iteration. Note that in the prescence
//   of exceptional flow the header might not fully execute on every iteration.
//
// * From the above it follows that the loop can only be entered at the header
//   block. FlowGraphNaturalLoop::EntryEdges() gives a vector of these edges.
//   After loop canonicalization it is expected that this vector has exactly one
//   edge, from the "preheader".
//
// * The loop can have multiple exits. The regular exit edges are recorded in
//   FlowGraphNaturalLoop::ExitEdges(). The loop can also be exited by
//   exceptional flow.
//
class FlowGraphNaturalLoop
{
    friend class FlowGraphNaturalLoops;

    // The DFS tree that contains the loop blocks.
    const FlowGraphDfsTree* m_dfsTree;

    // The header block; dominates all other blocks in the loop, and is the
    // only block branched to from outside the loop.
    BasicBlock* m_header;

    // Parent loop. By loop properties, well-scopedness is always guaranteed.
    // That is, the parent loop contains all blocks of this loop.
    FlowGraphNaturalLoop* m_parent = nullptr;
    // First child loop.
    FlowGraphNaturalLoop* m_child = nullptr;
    // Sibling child loop, in reverse post order of the header blocks.
    FlowGraphNaturalLoop* m_sibling = nullptr;

    // Bit vector of blocks in the loop; each index is the RPO index a block,
    // with the head block's RPO index subtracted.
    BitVec m_blocks;
    // Size of m_blocks.
    unsigned m_blocksSize = 0;

    // Edges from blocks inside the loop back to the header.
    jitstd::vector<FlowEdge*> m_backEdges;

    // Edges from blocks outside the loop to the header.
    jitstd::vector<FlowEdge*> m_entryEdges;

    // Edges from inside the loop to outside the loop. Note that exceptional
    // flow can also exit the loop and is not modelled.
    jitstd::vector<FlowEdge*> m_exitEdges;

    // Index of the loop in the range [0..FlowGraphNaturalLoops::NumLoops()).
    // Can be used to store additional annotations for this loop on the side.
    unsigned m_index = 0;

    FlowGraphNaturalLoop(const FlowGraphDfsTree* dfsTree, BasicBlock* head);

    unsigned LoopBlockBitVecIndex(BasicBlock* block);
    bool TryGetLoopBlockBitVecIndex(BasicBlock* block, unsigned* pIndex);

    BitVecTraits LoopBlockTraits();

    template<typename TFunc>
    bool VisitDefs(TFunc func);

    GenTreeLclVarCommon* FindDef(unsigned lclNum);

    void MatchInit(NaturalLoopIterInfo* info, BasicBlock* initBlock, GenTree* init);
    bool MatchLimit(unsigned iterVar, GenTree* test, NaturalLoopIterInfo* info);
    bool CheckLoopConditionBaseCase(BasicBlock* initBlock, NaturalLoopIterInfo* info);
    bool IsZeroTripTest(BasicBlock* initBlock, NaturalLoopIterInfo* info);
    bool InitBlockEntersLoopOnTrue(BasicBlock* initBlock);
    template<typename T>
    static bool EvaluateRelop(T op1, T op2, genTreeOps oper);
public:
    BasicBlock* GetHeader() const
    {
        return m_header;
    }

    const FlowGraphDfsTree* GetDfsTree() const
    {
        return m_dfsTree;
    }

    FlowGraphNaturalLoop* GetParent() const
    {
        return m_parent;
    }

    FlowGraphNaturalLoop* GetChild() const
    {
        return m_child;
    }

    FlowGraphNaturalLoop* GetSibling() const
    {
        return m_sibling;
    }

    unsigned GetIndex() const
    {
        return m_index;
    }

    const jitstd::vector<FlowEdge*>& BackEdges()
    {
        return m_backEdges;
    }

    const jitstd::vector<FlowEdge*>& EntryEdges()
    {
        return m_entryEdges;
    }

    const jitstd::vector<FlowEdge*>& ExitEdges()
    {
        return m_exitEdges;
    }

    FlowEdge* BackEdge(unsigned index)
    {
        assert(index < m_backEdges.size());
        return m_backEdges[index];
    }

    FlowEdge* EntryEdge(unsigned index)
    {
        assert(index < m_entryEdges.size());
        return m_entryEdges[index];
    }

    FlowEdge* ExitEdge(unsigned index)
    {
        assert(index < m_exitEdges.size());
        return m_exitEdges[index];
    }

    unsigned GetDepth() const;

    bool ContainsBlock(BasicBlock* block);
    bool ContainsLoop(FlowGraphNaturalLoop* childLoop);

    unsigned NumLoopBlocks();

    template<typename TFunc>
    BasicBlockVisit VisitLoopBlocksReversePostOrder(TFunc func);

    template<typename TFunc>
    BasicBlockVisit VisitLoopBlocksPostOrder(TFunc func);

    template<typename TFunc>
    BasicBlockVisit VisitLoopBlocks(TFunc func);

    template<typename TFunc>
    BasicBlockVisit VisitLoopBlocksLexical(TFunc func);

    template<typename TFunc>
    BasicBlockVisit VisitRegularExitBlocks(TFunc func);

    BasicBlock* GetLexicallyTopMostBlock();
    BasicBlock* GetLexicallyBottomMostBlock();

    bool AnalyzeIteration(NaturalLoopIterInfo* info);

    bool HasDef(unsigned lclNum);

    bool CanDuplicate(INDEBUG(const char** reason));
    void Duplicate(BasicBlock** insertAfter, BlockToBlockMap* map, weight_t weightScale);

#ifdef DEBUG
    static void Dump(FlowGraphNaturalLoop* loop);
#endif // DEBUG
};

// Represents a collection of the natural loops in the flow graph. See
// FlowGraphNaturalLoop for the characteristics of these loops.
//
// Loops are stored in a vector, with easily accessible indices (see
// FlowGraphNaturalLoop::GetIndex()). These indices can be used to store
// additional annotations for each loop on the side.
//
class FlowGraphNaturalLoops
{
    const FlowGraphDfsTree* m_dfsTree;

    // Collection of loops that were found.
    jitstd::vector<FlowGraphNaturalLoop*> m_loops;

    FlowGraphNaturalLoops(const FlowGraphDfsTree* dfs);

    static bool FindNaturalLoopBlocks(FlowGraphNaturalLoop* loop, ArrayStack<BasicBlock*>& worklist);

public:
    const FlowGraphDfsTree* GetDfsTree()
    {
        return m_dfsTree;
    }

    size_t NumLoops()
    {
        return m_loops.size();
    }

    FlowGraphNaturalLoop* GetLoopByIndex(unsigned index);
    FlowGraphNaturalLoop* GetLoopByHeader(BasicBlock* header);

    bool IsLoopBackEdge(FlowEdge* edge);
    bool IsLoopExitEdge(FlowEdge* edge);

    class LoopsPostOrderIter
    {
        jitstd::vector<FlowGraphNaturalLoop*>* m_loops;

    public:
        LoopsPostOrderIter(jitstd::vector<FlowGraphNaturalLoop*>* loops)
            : m_loops(loops)
        {
        }

        jitstd::vector<FlowGraphNaturalLoop*>::reverse_iterator begin()
        {
            return m_loops->rbegin();
        }

        jitstd::vector<FlowGraphNaturalLoop*>::reverse_iterator end()
        {
            return m_loops->rend();
        }
    };

    class LoopsReversePostOrderIter
    {
        jitstd::vector<FlowGraphNaturalLoop*>* m_loops;

    public:
        LoopsReversePostOrderIter(jitstd::vector<FlowGraphNaturalLoop*>* loops)
            : m_loops(loops)
        {
        }

        jitstd::vector<FlowGraphNaturalLoop*>::iterator begin()
        {
            return m_loops->begin();
        }

        jitstd::vector<FlowGraphNaturalLoop*>::iterator end()
        {
            return m_loops->end();
        }
    };

    // Iterate the loops in post order (child loops before parent loops)
    LoopsPostOrderIter InPostOrder()
    {
        return LoopsPostOrderIter(&m_loops);
    }

    // Iterate the loops in reverse post order (parent loops before child loops)
    LoopsReversePostOrderIter InReversePostOrder()
    {
        return LoopsReversePostOrderIter(&m_loops);
    }

    static FlowGraphNaturalLoops* Find(const FlowGraphDfsTree* dfs);

#ifdef DEBUG
    static void Dump(FlowGraphNaturalLoops* loops);
#endif // DEBUG
};

// Represents the dominator tree of the flow graph.
class FlowGraphDominatorTree
{
    template<typename TVisitor>
    friend class DomTreeVisitor;

    const FlowGraphDfsTree* m_dfsTree;
    const DomTreeNode* m_domTree;
    const unsigned* m_preorderNum;
    const unsigned* m_postorderNum;

    FlowGraphDominatorTree(const FlowGraphDfsTree* dfsTree, const DomTreeNode* domTree, const unsigned* preorderNum, const unsigned* postorderNum)
        : m_dfsTree(dfsTree)
        , m_domTree(domTree)
        , m_preorderNum(preorderNum)
        , m_postorderNum(postorderNum)
    {
    }

    static BasicBlock* IntersectDom(BasicBlock* block1, BasicBlock* block2);
public:
    BasicBlock* Intersect(BasicBlock* block, BasicBlock* block2);
    bool Dominates(BasicBlock* dominator, BasicBlock* dominated);

#ifdef DEBUG
    void Dump();
#endif

    static FlowGraphDominatorTree* Build(const FlowGraphDfsTree* dfsTree);
};

// Represents a reverse mapping from block back to its (most nested) containing loop.
class BlockToNaturalLoopMap
{
    FlowGraphNaturalLoops* m_loops;
    // Array from postorder num -> index of most-nested loop containing the
    // block, or UINT_MAX if no loop contains it.
    unsigned* m_indices;

    BlockToNaturalLoopMap(FlowGraphNaturalLoops* loops, unsigned* indices)
        : m_loops(loops), m_indices(indices)
    {
    }

public:
    FlowGraphNaturalLoop* GetLoop(BasicBlock* block);

    static BlockToNaturalLoopMap* Build(FlowGraphNaturalLoops* loops);
};

// Represents a data structure that can answer A -> B reachability queries in
// O(1) time. Only takes regular flow into account; if A -> B requires
// exceptional flow, then CanReach returns false.
class BlockReachabilitySets
{
    FlowGraphDfsTree* m_dfsTree;
    BitVec* m_reachabilitySets;

    BlockReachabilitySets(FlowGraphDfsTree* dfsTree, BitVec* reachabilitySets)
        : m_dfsTree(dfsTree)
        , m_reachabilitySets(reachabilitySets)
    {
    }

public:
    bool CanReach(BasicBlock* from, BasicBlock* to);

#ifdef DEBUG
    void Dump();
#endif

    static BlockReachabilitySets* Build(FlowGraphDfsTree* dfsTree);
};

enum class FieldKindForVN
{
    SimpleStatic,
    WithBaseAddr
};

typedef JitHashTable<CORINFO_FIELD_HANDLE, JitPtrKeyFuncs<struct CORINFO_FIELD_STRUCT_>, FieldKindForVN> FieldHandleSet;

typedef JitHashTable<CORINFO_CLASS_HANDLE, JitPtrKeyFuncs<struct CORINFO_CLASS_STRUCT_>, bool> ClassHandleSet;

// Represents a distillation of the useful side effects that occur inside a loop.
// Used by VN to be able to reason more precisely when entering loops.
struct LoopSideEffects
{
    // The loop contains an operation that we assume has arbitrary memory side
    // effects. If this is set, the fields below may not be accurate (since
    // they become irrelevant.)
    bool HasMemoryHavoc[MemoryKindCount];
    // The set of variables that are IN or OUT during the execution of this loop
    VARSET_TP VarInOut;
    // The set of variables that are USE or DEF during the execution of this loop.
    VARSET_TP VarUseDef;
    // This has entries for all static field and object instance fields modified
    // in the loop.
    FieldHandleSet* FieldsModified = nullptr;
    // Bits set indicate the set of sz array element types such that
    // arrays of that type are modified
    // in the loop.
    ClassHandleSet* ArrayElemTypesModified = nullptr;
    bool            ContainsCall           = false;

    LoopSideEffects();

    void AddVariableLiveness(Compiler* comp, BasicBlock* block);
    void AddModifiedField(Compiler* comp, CORINFO_FIELD_HANDLE fldHnd, FieldKindForVN fieldKind);
    void AddModifiedElemType(Compiler* comp, CORINFO_CLASS_HANDLE structHnd);
};

//  The following holds information about instr offsets in terms of generated code.

enum class IPmappingDscKind
{
    Prolog,    // The mapping represents the start of a prolog.
    Epilog,    // The mapping represents the start of an epilog.
    NoMapping, // This does not map to any IL offset.
    Normal,    // The mapping maps to an IL offset.
};

struct IPmappingDsc
{
    emitLocation     ipmdNativeLoc; // the emitter location of the native code corresponding to the IL offset
    IPmappingDscKind ipmdKind;      // The kind of mapping
    ILLocation       ipmdLoc;       // The location for normal mappings
    bool             ipmdIsLabel;   // Can this code be a branch label?
};

struct RichIPMapping
{
    emitLocation nativeLoc;
    DebugInfo    debugInfo;
};

// Current kind of node threading stored in GenTree::gtPrev and GenTree::gtNext.
// See fgNodeThreading for more information.
enum class NodeThreading
{
    None,
    AllLocals, // Locals are threaded (after local morph when optimizing)
    AllTrees,  // All nodes are threaded (after gtSetBlockOrder)
    LIR,       // Nodes are in LIR form (after rationalization)
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

struct HWIntrinsicInfo;

class Compiler
{
    friend class emitter;
    friend class UnwindInfo;
    friend class UnwindFragmentInfo;
    friend class UnwindEpilogInfo;
    friend class JitTimer;
    friend class LinearScan;
    friend class Rationalizer;
    friend class Phase;
    friend class Lowering;
    friend class CSE_DataFlow;
    friend class CSE_HeuristicCommon;
    friend class CSE_HeuristicRandom;
    friend class CSE_HeuristicReplay;
    friend class CSE_HeuristicRL;
    friend class CSE_Heuristic;
    friend class CodeGenInterface;
    friend class CodeGen;
    friend class LclVarDsc;
    friend class TempDsc;
    friend class LIR;
    friend class ObjectAllocator;
    friend class LocalAddressVisitor;
    friend struct Statement;
    friend struct GenTree;
    friend class MorphInitBlockHelper;
    friend class MorphCopyBlockHelper;
    friend class SharedTempsScope;
    friend class CallArgs;
    friend class IndirectCallTransformer;
    friend class ProfileSynthesis;
    friend class LocalsUseVisitor;
    friend class Promotion;
    friend class ReplaceVisitor;
    friend class FlowGraphNaturalLoop;

#ifdef FEATURE_HW_INTRINSICS
    friend struct HWIntrinsicInfo;
    friend struct SimdAsHWIntrinsicInfo;
#endif // FEATURE_HW_INTRINSICS

#ifndef TARGET_64BIT
    friend class DecomposeLongs;
#endif // !TARGET_64BIT

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
    bool verbose;
    bool verboseTrees;
    bool shouldUseVerboseTrees();
    bool asciiTrees; // If true, dump trees using only ASCII characters
    bool shouldDumpASCIITrees();
    bool verboseSsa; // If true, produce especially verbose dump output in SSA construction.
    bool shouldUseVerboseSsa();
    bool treesBeforeAfterMorph; // If true, print trees before/after morphing (paired by an intra-compilation id:
    int  morphNum;              // This counts the trees that have been morphed, allowing us to label each uniquely.
    void makeExtraStructQueries(CORINFO_CLASS_HANDLE structHandle, int level); // Make queries recursively 'level' deep.

    const char* VarNameToStr(VarName name)
    {
        return name;
    }

    DWORD expensiveDebugCheckLevel;
#endif

    GenTree* impStoreMultiRegValueToVar(GenTree*             op,
                                        CORINFO_CLASS_HANDLE hClass DEBUGARG(CorInfoCallConvExtension callConv));

#ifdef TARGET_X86
    bool isTrivialPointerSizedStruct(CORINFO_CLASS_HANDLE clsHnd) const;
#endif // TARGET_X86

    //-------------------------------------------------------------------------
    // Functions to handle homogeneous floating-point aggregates (HFAs) in ARM/ARM64.
    // HFAs are one to four element structs where each element is the same
    // type, either all float or all double. We handle HVAs (one to four elements of
    // vector types) uniformly with HFAs. HFAs are treated specially
    // in the ARM/ARM64 Procedure Call Standards, specifically, they are passed in
    // floating-point registers instead of the general purpose registers.
    //

    bool IsHfa(CORINFO_CLASS_HANDLE hClass);
    var_types GetHfaType(CORINFO_CLASS_HANDLE hClass);
    unsigned GetHfaCount(CORINFO_CLASS_HANDLE hClass);

    bool IsMultiRegReturnedType(CORINFO_CLASS_HANDLE hClass, CorInfoCallConvExtension callConv);

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

        void ehnSetTryNodeType()
        {
            ehnBlockType = TryNode;
        }
        void ehnSetFilterNodeType()
        {
            ehnBlockType = FilterNode;
        }
        void ehnSetHandlerNodeType()
        {
            ehnBlockType = HandlerNode;
        }
        void ehnSetFinallyNodeType()
        {
            ehnBlockType = FinallyNode;
        }
        void ehnSetFaultNodeType()
        {
            ehnBlockType = FaultNode;
        }

        bool ehnIsTryBlock()
        {
            return ehnBlockType == TryNode;
        }
        bool ehnIsFilterBlock()
        {
            return ehnBlockType == FilterNode;
        }
        bool ehnIsHandlerBlock()
        {
            return ehnBlockType == HandlerNode;
        }
        bool ehnIsFinallyBlock()
        {
            return ehnBlockType == FinallyNode;
        }
        bool ehnIsFaultBlock()
        {
            return ehnBlockType == FaultNode;
        }

        // returns true if there is any overlap between the two nodes
        static bool ehnIsOverlap(pEHNodeDsc node1, pEHNodeDsc node2)
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
        static bool ehnIsNested(pEHNodeDsc inner, pEHNodeDsc outer)
        {
            return ((inner->ehnStartOffset >= outer->ehnStartOffset) && (inner->ehnEndOffset <= outer->ehnEndOffset));
        }
    };

//-------------------------------------------------------------------------
// Exception handling functions
//

#if !defined(FEATURE_EH_FUNCLETS)

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
    bool bbInFilterBBRange(BasicBlock* blk);
    bool bbInTryRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInExnFlowRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInHandlerRegions(unsigned regionIndex, BasicBlock* blk);
    bool bbInCatchHandlerRegions(BasicBlock* tryBlk, BasicBlock* hndBlk);
    unsigned short bbFindInnermostCommonTryRegion(BasicBlock* bbOne, BasicBlock* bbTwo);

    unsigned short bbFindInnermostTryRegionContainingHandlerRegion(unsigned handlerIndex);
    unsigned short bbFindInnermostHandlerRegionContainingTryRegion(unsigned tryIndex);

    // Returns true if "block" is the start of a try region.
    bool bbIsTryBeg(const BasicBlock* block);

    // Returns true if "block" is the start of a handler or filter region.
    bool bbIsHandlerBeg(const BasicBlock* block);

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
    EHblkDsc* ehGetBlockTryDsc(const BasicBlock* block);

    // Return the EH descriptor for the most nested filter or handler region this BasicBlock is a member of (or nullptr
    // if this block is not in a filter or handler region).
    EHblkDsc* ehGetBlockHndDsc(const BasicBlock* block);

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

    // Find the range of basic blocks in which all BBJ_CALLFINALLY will be found that target the 'finallyIndex'
    // region's handler. Set `firstBlock` to the first block, and `lastBlock` to the last block of the range
    // (the range is inclusive of `firstBlock` and `lastBlock`). Thus, the range is [firstBlock .. lastBlock].
    // Precondition: 'finallyIndex' is the EH region of a try/finally clause.
    void ehGetCallFinallyBlockRange(unsigned finallyIndex, BasicBlock** firstBlock, BasicBlock** lastBlock);

#ifdef DEBUG
    // Given a BBJ_CALLFINALLY block and the EH region index of the finally it is calling, return
    // 'true' if the BBJ_CALLFINALLY is in the correct EH region.
    bool ehCallFinallyInCorrectRegion(BasicBlock* blockCallFinally, unsigned finallyIndex);
#endif // DEBUG

#if defined(FEATURE_EH_FUNCLETS)
    // Do we need a PSPSym in the main function? For codegen purposes, we only need one
    // if there is a filter that protects a region with a nested EH clause (such as a
    // try/catch nested in the 'try' body of a try/filter/filter-handler). See
    // genFuncletProlog() for more details. However, the VM seems to use it for more
    // purposes, maybe including debugging. Until we are sure otherwise, always create
    // a PSPSym for functions with any EH.
    bool ehNeedsPSPSym() const
    {
#ifdef TARGET_X86
        return false;
#else  // TARGET_X86
        return compHndBBtabCount > 0;
#endif // TARGET_X86
    }

    bool     ehAnyFunclets();  // Are there any funclets in this function?
    unsigned ehFuncletCount(); // Return the count of funclets in the function

    unsigned bbThrowIndex(BasicBlock* blk); // Get the index to use as the cache key for sharing throw blocks

#else  // !FEATURE_EH_FUNCLETS

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
#endif // !FEATURE_EH_FUNCLETS

    FlowEdge* BlockPredsWithEH(BasicBlock* blk);
    FlowEdge* BlockDominancePreds(BasicBlock* blk);

    // This table is useful for memoization of the method above.
    typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, FlowEdge*> BlockToFlowEdgeMap;
    BlockToFlowEdgeMap* m_blockToEHPreds;
    BlockToFlowEdgeMap* GetBlockToEHPreds()
    {
        if (m_blockToEHPreds == nullptr)
        {
            m_blockToEHPreds = new (getAllocator()) BlockToFlowEdgeMap(getAllocator());
        }
        return m_blockToEHPreds;
    }

    BlockToFlowEdgeMap* m_dominancePreds;
    BlockToFlowEdgeMap* GetDominancePreds()
    {
        if (m_dominancePreds == nullptr)
        {
            m_dominancePreds = new (getAllocator()) BlockToFlowEdgeMap(getAllocator());
        }
        return m_dominancePreds;
    }

    void* ehEmitCookie(BasicBlock* block);
    UNATIVE_OFFSET ehCodeOffset(BasicBlock* block);

    EHblkDsc* ehInitHndRange(BasicBlock* src, IL_OFFSET* hndBeg, IL_OFFSET* hndEnd, bool* inFilter);

    EHblkDsc* ehInitTryRange(BasicBlock* src, IL_OFFSET* tryBeg, IL_OFFSET* tryEnd);

    EHblkDsc* ehInitHndBlockRange(BasicBlock* blk, BasicBlock** hndBeg, BasicBlock** hndLast, bool* inFilter);

    EHblkDsc* ehInitTryBlockRange(BasicBlock* blk, BasicBlock** tryBeg, BasicBlock** tryLast);

    void fgSetTryBeg(EHblkDsc* handlerTab, BasicBlock* newTryBeg);

    void fgSetTryEnd(EHblkDsc* handlerTab, BasicBlock* newTryLast);

    void fgSetHndEnd(EHblkDsc* handlerTab, BasicBlock* newHndLast);

    void fgSkipRmvdBlocks(EHblkDsc* handlerTab);

    void fgAllocEHTable();

    void fgRemoveEHTableEntry(unsigned XTnum);

#if defined(FEATURE_EH_FUNCLETS)

    EHblkDsc* fgAddEHTableEntry(unsigned XTnum);

#endif // FEATURE_EH_FUNCLETS

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

    bool fgCreateFiltersForGenericExceptions();

    void fgCheckForLoopsInHandlers();

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
    Statement* gtNewStmt(GenTree* expr = nullptr);
    Statement* gtNewStmt(GenTree* expr, const DebugInfo& di);

    // For unary opers.
    GenTree* gtNewOperNode(genTreeOps oper, var_types type, GenTree* op1);

    // For binary opers.
    GenTreeOp* gtNewOperNode(genTreeOps oper, var_types type, GenTree* op1, GenTree* op2);

    GenTreeCC* gtNewCC(genTreeOps oper, var_types type, GenCondition cond);
    GenTreeOpCC* gtNewOperCC(genTreeOps oper, var_types type, GenCondition cond, GenTree* op1, GenTree* op2);

    GenTreeColon* gtNewColonNode(var_types type, GenTree* thenNode, GenTree* elseNode);
    GenTreeQmark* gtNewQmarkNode(var_types type, GenTree* cond, GenTreeColon* colon);

    GenTree* gtNewLargeOperNode(genTreeOps oper,
                                var_types  type = TYP_I_IMPL,
                                GenTree*   op1  = nullptr,
                                GenTree*   op2  = nullptr);

    GenTreeIntCon* gtNewIconNode(ssize_t value, var_types type = TYP_INT);
    GenTreeIntCon* gtNewIconNode(unsigned fieldOffset, FieldSeq* fieldSeq);
    GenTreeIntCon* gtNewNull();
    GenTreeIntCon* gtNewTrue();
    GenTreeIntCon* gtNewFalse();

    GenTree* gtNewPhysRegNode(regNumber reg, var_types type);

    GenTree* gtNewJmpTableNode();

    GenTree* gtNewIndOfIconHandleNode(var_types indType, size_t addr, GenTreeFlags iconFlags, bool isInvariant);

    GenTreeIntCon*   gtNewIconHandleNode(size_t value, GenTreeFlags flags, FieldSeq* fields = nullptr);

    static var_types gtGetTypeForIconFlags(GenTreeFlags flags)
    {
        return flags == GTF_ICON_OBJ_HDL ? TYP_REF : TYP_I_IMPL;
    }

    GenTreeFlags gtTokenToIconFlags(unsigned token);

    GenTree* gtNewIconEmbHndNode(void* value, void* pValue, GenTreeFlags flags, void* compileTimeHandle);

    GenTree* gtNewIconEmbScpHndNode(CORINFO_MODULE_HANDLE scpHnd);
    GenTree* gtNewIconEmbClsHndNode(CORINFO_CLASS_HANDLE clsHnd);
    GenTree* gtNewIconEmbMethHndNode(CORINFO_METHOD_HANDLE methHnd);
    GenTree* gtNewIconEmbFldHndNode(CORINFO_FIELD_HANDLE fldHnd);

    GenTree* gtNewStringLiteralNode(InfoAccessType iat, void* pValue);
    GenTreeIntCon* gtNewStringLiteralLength(GenTreeStrCon* node);

    GenTree* gtNewLconNode(__int64 value);

    GenTree* gtNewDconNodeF(float value);
    GenTree* gtNewDconNodeD(double value);
    GenTree* gtNewDconNode(float value, var_types type) = delete; // use gtNewDconNodeF instead
    GenTree* gtNewDconNode(double value, var_types type);

    GenTree* gtNewSconNode(int CPX, CORINFO_MODULE_HANDLE scpHandle);

    GenTreeVecCon* gtNewVconNode(var_types type);

    GenTreeVecCon* gtNewVconNode(var_types type, void* data);

    GenTree* gtNewAllBitsSetConNode(var_types type);

    GenTree* gtNewZeroConNode(var_types type);

    GenTree* gtNewOneConNode(var_types type, var_types simdBaseType = TYP_UNDEF);

    GenTree* gtNewGenericCon(var_types type, uint8_t* cnsVal);

    GenTree* gtNewConWithPattern(var_types type, uint8_t pattern);

    GenTreeLclVar* gtNewStoreLclVarNode(unsigned lclNum, GenTree* data);

    GenTreeLclFld* gtNewStoreLclFldNode(
        unsigned lclNum, var_types type, ClassLayout* layout, unsigned offset, GenTree* data);

    GenTreeLclFld* gtNewStoreLclFldNode(unsigned lclNum, var_types type, unsigned offset, GenTree* data)
    {
        return gtNewStoreLclFldNode(lclNum, type, (type == TYP_STRUCT) ? data->GetLayout(this) : nullptr, offset, data);
    }

    GenTree* gtNewPutArgReg(var_types type, GenTree* arg, regNumber argReg);

    GenTree* gtNewBitCastNode(var_types type, GenTree* arg);

public:
    GenTreeCall* gtNewCallNode(gtCallTypes           callType,
                               CORINFO_METHOD_HANDLE handle,
                               var_types             type,
                               const DebugInfo&      di = DebugInfo());

    GenTreeCall* gtNewIndCallNode(GenTree* addr, var_types type, const DebugInfo& di = DebugInfo());

    GenTreeCall* gtNewHelperCallNode(
        unsigned helper, var_types type, GenTree* arg1 = nullptr, GenTree* arg2 = nullptr, GenTree* arg3 = nullptr);

    GenTreeCall* gtNewRuntimeLookupHelperCallNode(CORINFO_RUNTIME_LOOKUP* pRuntimeLookup,
                                                  GenTree*                ctxTree,
                                                  void*                   compileTimeHandle);

    GenTreeLclVar* gtNewLclvNode(unsigned lnum, var_types type DEBUGARG(IL_OFFSET offs = BAD_IL_OFFSET));
    GenTreeLclVar* gtNewLclVarNode(unsigned lclNum, var_types type = TYP_UNDEF);
    GenTreeLclVar* gtNewLclLNode(unsigned lnum, var_types type DEBUGARG(IL_OFFSET offs = BAD_IL_OFFSET));

    GenTreeLclFld* gtNewLclVarAddrNode(unsigned lclNum, var_types type = TYP_I_IMPL);
    GenTreeLclFld* gtNewLclAddrNode(unsigned lclNum, unsigned lclOffs, var_types type = TYP_I_IMPL);

    GenTreeConditional* gtNewConditionalNode(
        genTreeOps oper, GenTree* cond, GenTree* op1, GenTree* op2, var_types type);

#ifdef FEATURE_SIMD
    void SetOpLclRelatedToSIMDIntrinsic(GenTree* op);
#endif

#ifdef FEATURE_HW_INTRINSICS
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 GenTree*       op1,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 GenTree*       op1,
                                                 GenTree*       op2,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 GenTree*       op1,
                                                 GenTree*       op2,
                                                 GenTree*       op3,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 GenTree*       op1,
                                                 GenTree*       op2,
                                                 GenTree*       op3,
                                                 GenTree*       op4,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types      type,
                                                 GenTree**      operands,
                                                 size_t         operandCount,
                                                 NamedIntrinsic hwIntrinsicID,
                                                 CorInfoType    simdBaseJitType,
                                                 unsigned       simdSize);
    GenTreeHWIntrinsic* gtNewSimdHWIntrinsicNode(var_types              type,
                                                 IntrinsicNodeBuilder&& nodeBuilder,
                                                 NamedIntrinsic         hwIntrinsicID,
                                                 CorInfoType            simdBaseJitType,
                                                 unsigned               simdSize);

    GenTreeHWIntrinsic* gtNewSimdAsHWIntrinsicNode(var_types      type,
                                                   NamedIntrinsic hwIntrinsicID,
                                                   CorInfoType    simdBaseJitType,
                                                   unsigned       simdSize)
    {
        return gtNewSimdHWIntrinsicNode(type, hwIntrinsicID, simdBaseJitType, simdSize);
    }

    GenTreeHWIntrinsic* gtNewSimdAsHWIntrinsicNode(
        var_types type, GenTree* op1, NamedIntrinsic hwIntrinsicID, CorInfoType simdBaseJitType, unsigned simdSize)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, hwIntrinsicID, simdBaseJitType, simdSize);
    }

    GenTreeHWIntrinsic* gtNewSimdAsHWIntrinsicNode(var_types      type,
                                                   GenTree*       op1,
                                                   GenTree*       op2,
                                                   NamedIntrinsic hwIntrinsicID,
                                                   CorInfoType    simdBaseJitType,
                                                   unsigned       simdSize)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, op2, hwIntrinsicID, simdBaseJitType, simdSize);
    }

    GenTreeHWIntrinsic* gtNewSimdAsHWIntrinsicNode(var_types      type,
                                                   GenTree*       op1,
                                                   GenTree*       op2,
                                                   GenTree*       op3,
                                                   NamedIntrinsic hwIntrinsicID,
                                                   CorInfoType    simdBaseJitType,
                                                   unsigned       simdSize)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, op2, op3, hwIntrinsicID, simdBaseJitType, simdSize);
    }

    GenTree* gtNewSimdAbsNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdBinOpNode(genTreeOps  op,
                                var_types   type,
                                GenTree*    op1,
                                GenTree*    op2,
                                CorInfoType simdBaseJitType,
                                unsigned    simdSize);

    GenTree* gtNewSimdCeilNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdCmpOpNode(genTreeOps  op,
                                var_types   type,
                                GenTree*    op1,
                                GenTree*    op2,
                                CorInfoType simdBaseJitType,
                                unsigned    simdSize);

    GenTree* gtNewSimdCmpOpAllNode(genTreeOps  op,
                                   var_types   type,
                                   GenTree*    op1,
                                   GenTree*    op2,
                                   CorInfoType simdBaseJitType,
                                   unsigned    simdSize);

    GenTree* gtNewSimdCmpOpAnyNode(genTreeOps  op,
                                   var_types   type,
                                   GenTree*    op1,
                                   GenTree*    op2,
                                   CorInfoType simdBaseJitType,
                                   unsigned    simdSize);

    GenTree* gtNewSimdCndSelNode(var_types   type,
                                 GenTree*    op1,
                                 GenTree*    op2,
                                 GenTree*    op3,
                                 CorInfoType simdBaseJitType,
                                 unsigned    simdSize);

    GenTree* gtNewSimdCreateBroadcastNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdCreateScalarNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdCreateScalarUnsafeNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdCreateSequenceNode(
        var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdDotProdNode(var_types   type,
                                  GenTree*    op1,
                                  GenTree*    op2,
                                  CorInfoType simdBaseJitType,
                                  unsigned    simdSize);

    GenTree* gtNewSimdFloorNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdGetElementNode(var_types   type,
                                     GenTree*    op1,
                                     GenTree*    op2,
                                     CorInfoType simdBaseJitType,
                                     unsigned    simdSize);

    GenTree* gtNewSimdGetIndicesNode(var_types type, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdGetLowerNode(var_types   type,
                                   GenTree*    op1,
                                   CorInfoType simdBaseJitType,
                                   unsigned    simdSize);

    GenTree* gtNewSimdGetUpperNode(var_types   type,
                                   GenTree*    op1,
                                   CorInfoType simdBaseJitType,
                                   unsigned    simdSize);

    GenTree* gtNewSimdLoadNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdLoadAlignedNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdLoadNonTemporalNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdMaxNode(var_types   type,
                              GenTree*    op1,
                              GenTree*    op2,
                              CorInfoType simdBaseJitType,
                              unsigned    simdSize);

    GenTree* gtNewSimdMinNode(var_types   type,
                              GenTree*    op1,
                              GenTree*    op2,
                              CorInfoType simdBaseJitType,
                              unsigned    simdSize);

    GenTree* gtNewSimdNarrowNode(var_types   type,
                                 GenTree*    op1,
                                 GenTree*    op2,
                                 CorInfoType simdBaseJitType,
                                 unsigned    simdSize);

    GenTree* gtNewSimdShuffleNode(var_types   type,
                                  GenTree*    op1,
                                  GenTree*    op2,
                                  CorInfoType simdBaseJitType,
                                  unsigned    simdSize);

    GenTree* gtNewSimdSqrtNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdStoreNode(
        GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdStoreAlignedNode(
        GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdStoreNonTemporalNode(
        GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdSumNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

#if defined(TARGET_XARCH)
    GenTree* gtNewSimdTernaryLogicNode(var_types   type,
                                       GenTree*    op1,
                                       GenTree*    op2,
                                       GenTree*    op3,
                                       GenTree*    op4,
                                       CorInfoType simdBaseJitType,
                                       unsigned    simdSize);
#endif // TARGET_XARCH


    GenTree* gtNewSimdToScalarNode(var_types   type,
                                   GenTree*    op1,
                                   CorInfoType simdBaseJitType,
                                   unsigned    simdSize);

    GenTree* gtNewSimdUnOpNode(genTreeOps  op,
                               var_types   type,
                               GenTree*    op1,
                               CorInfoType simdBaseJitType,
                               unsigned    simdSize);

    GenTree* gtNewSimdWidenLowerNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdWidenUpperNode(
        var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize);

    GenTree* gtNewSimdWithElementNode(var_types   type,
                                      GenTree*    op1,
                                      GenTree*    op2,
                                      GenTree*    op3,
                                      CorInfoType simdBaseJitType,
                                      unsigned    simdSize);

    GenTree* gtNewSimdWithLowerNode(var_types   type,
                                    GenTree*    op1,
                                    GenTree*    op2,
                                    CorInfoType simdBaseJitType,
                                    unsigned    simdSize);

    GenTree* gtNewSimdWithUpperNode(var_types   type,
                                    GenTree*    op1,
                                    GenTree*    op2,
                                    CorInfoType simdBaseJitType,
                                    unsigned    simdSize);

    GenTreeHWIntrinsic* gtNewScalarHWIntrinsicNode(var_types type, NamedIntrinsic hwIntrinsicID);
    GenTreeHWIntrinsic* gtNewScalarHWIntrinsicNode(var_types type, GenTree* op1, NamedIntrinsic hwIntrinsicID);
    GenTreeHWIntrinsic* gtNewScalarHWIntrinsicNode(var_types      type,
                                                   GenTree*       op1,
                                                   GenTree*       op2,
                                                   NamedIntrinsic hwIntrinsicID);
    GenTreeHWIntrinsic* gtNewScalarHWIntrinsicNode(
        var_types type, GenTree* op1, GenTree* op2, GenTree* op3, NamedIntrinsic hwIntrinsicID);
    CorInfoType getBaseJitTypeFromArgIfNeeded(NamedIntrinsic       intrinsic,
                                              CORINFO_CLASS_HANDLE clsHnd,
                                              CORINFO_SIG_INFO*    sig,
                                              CorInfoType          simdBaseJitType);

#ifdef TARGET_ARM64
    GenTreeFieldList* gtConvertTableOpToFieldList(GenTree* op, unsigned fieldCount);
    GenTreeFieldList* gtConvertParamOpToFieldList(GenTree* op, unsigned fieldCount, CORINFO_CLASS_HANDLE clsHnd);
#endif
#endif // FEATURE_HW_INTRINSICS

    GenTree* gtNewMustThrowException(unsigned helper, var_types type, CORINFO_CLASS_HANDLE clsHnd);

    GenTreeLclFld* gtNewLclFldNode(unsigned lnum, var_types type, unsigned offset);
    GenTreeRetExpr* gtNewInlineCandidateReturnExpr(GenTreeCall* inlineCandidate, var_types type);

    GenTreeFieldAddr* gtNewFieldAddrNode(var_types            type,
                                         CORINFO_FIELD_HANDLE fldHnd,
                                         GenTree*             obj    = nullptr,
                                         DWORD                offset = 0);

    GenTreeFieldAddr* gtNewFieldAddrNode(CORINFO_FIELD_HANDLE fldHnd, GenTree* obj, unsigned offset)
    {
        return gtNewFieldAddrNode(varTypeIsGC(obj) ? TYP_BYREF : TYP_I_IMPL, fldHnd, obj, offset);
    }

    GenTreeIndexAddr* gtNewIndexAddr(GenTree*             arrayOp,
                                     GenTree*             indexOp,
                                     var_types            elemType,
                                     CORINFO_CLASS_HANDLE elemClassHandle,
                                     unsigned             firstElemOffset,
                                     unsigned             lengthOffset);

    GenTreeIndexAddr* gtNewArrayIndexAddr(GenTree*             arrayOp,
                                          GenTree*             indexOp,
                                          var_types            elemType,
                                          CORINFO_CLASS_HANDLE elemClassHandle);

    GenTreeIndir* gtNewIndexIndir(GenTreeIndexAddr* indexAddr);

    void gtAnnotateNewArrLen(GenTree* arrLen, BasicBlock* block);

    GenTreeArrLen* gtNewArrLen(var_types typ, GenTree* arrayOp, int lenOffset, BasicBlock* block);

    GenTreeMDArr* gtNewMDArrLen(GenTree* arrayOp, unsigned dim, unsigned rank, BasicBlock* block);

    GenTreeMDArr* gtNewMDArrLowerBound(GenTree* arrayOp, unsigned dim, unsigned rank, BasicBlock* block);

    void gtInitializeStoreNode(GenTree* store, GenTree* data);

    void gtInitializeIndirNode(GenTreeIndir* indir, GenTreeFlags indirFlags);

    GenTreeBlk* gtNewBlkIndir(ClassLayout* layout, GenTree* addr, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTreeIndir* gtNewIndir(var_types typ, GenTree* addr, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTreeBlk* gtNewStoreBlkNode(
        ClassLayout* layout, GenTree* addr, GenTree* data, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTreeStoreDynBlk* gtNewStoreDynBlkNode(
        GenTree* addr, GenTree* data, GenTree* dynamicSize, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTreeStoreInd* gtNewStoreIndNode(
        var_types type, GenTree* addr, GenTree* data, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTree* gtNewLoadValueNode(
        var_types type, ClassLayout* layout, GenTree* addr, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTree* gtNewLoadValueNode(ClassLayout* layout, GenTree* addr, GenTreeFlags indirFlags = GTF_EMPTY)
    {
        return gtNewLoadValueNode(layout->GetType(), layout, addr, indirFlags);
    }

    GenTree* gtNewLoadValueNode(var_types type, GenTree* addr, GenTreeFlags indirFlags = GTF_EMPTY)
    {
        return gtNewLoadValueNode(type, nullptr, addr, indirFlags);
    }

    GenTree* gtNewStoreValueNode(
        var_types type, ClassLayout* layout, GenTree* addr, GenTree* data, GenTreeFlags indirFlags = GTF_EMPTY);

    GenTree* gtNewStoreValueNode(ClassLayout* layout, GenTree* addr, GenTree* data, GenTreeFlags indirFlags = GTF_EMPTY)
    {
        return gtNewStoreValueNode(layout->GetType(), layout, addr, data, indirFlags);
    }

    GenTree* gtNewStoreValueNode(var_types type, GenTree* addr, GenTree* data, GenTreeFlags indirFlags = GTF_EMPTY)
    {
        return gtNewStoreValueNode(type, nullptr, addr, data, indirFlags);
    }

    GenTree* gtNewNullCheck(GenTree* addr, BasicBlock* basicBlock);

    var_types gtTypeForNullCheck(GenTree* tree);
    void gtChangeOperToNullCheck(GenTree* tree, BasicBlock* block);

    GenTree* gtNewAtomicNode(
        genTreeOps oper, var_types type, GenTree* addr, GenTree* value, GenTree* comparand = nullptr);

    GenTree* gtNewTempStore(unsigned         tmp,
                            GenTree*         val,
                            unsigned         curLevel   = CHECK_SPILL_NONE,
                            Statement**      pAfterStmt = nullptr,
                            const DebugInfo& di         = DebugInfo(),
                            BasicBlock*      block      = nullptr);

    GenTree* gtNewRefCOMfield(GenTree*                objPtr,
                              CORINFO_RESOLVED_TOKEN* pResolvedToken,
                              CORINFO_ACCESS_FLAGS    access,
                              CORINFO_FIELD_INFO*     pFieldInfo,
                              var_types               lclTyp,
                              GenTree*                assg);

    GenTree* gtNewNothingNode();

    GenTree* gtUnusedValNode(GenTree* expr);

    GenTree* gtNewKeepAliveNode(GenTree* op);

    GenTreeCast* gtNewCastNode(var_types typ, GenTree* op1, bool fromUnsigned, var_types castType);

    GenTreeCast* gtNewCastNodeL(var_types typ, GenTree* op1, bool fromUnsigned, var_types castType);

    GenTreeAllocObj* gtNewAllocObjNode(
        unsigned int helper, bool helperHasSideEffects, CORINFO_CLASS_HANDLE clsHnd, var_types type, GenTree* op1);

    GenTreeAllocObj* gtNewAllocObjNode(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool useParent);

    GenTree* gtNewRuntimeLookup(CORINFO_GENERIC_HANDLE hnd, CorInfoGenericHandleType hndTyp, GenTree* lookupTree);

    GenTreeIndir* gtNewMethodTableLookup(GenTree* obj);

    //------------------------------------------------------------------------
    // Other GenTree functions

    GenTree* gtClone(GenTree* tree, bool complexOK = false);

    // Create a copy of `tree`
    GenTree* gtCloneExpr(GenTree* tree);

    Statement* gtCloneStmt(Statement* stmt)
    {
        GenTree* exprClone = gtCloneExpr(stmt->GetRootNode());
        return gtNewStmt(exprClone, stmt->GetDebugInfo());
    }

    // Internal helper for cloning a call
    GenTreeCall* gtCloneExprCallHelper(GenTreeCall* call);

    // Create copy of an inline or guarded devirtualization candidate tree.
    GenTreeCall* gtCloneCandidateCall(GenTreeCall* call);

    void gtUpdateSideEffects(Statement* stmt, GenTree* tree);

    void gtUpdateTreeAncestorsSideEffects(GenTree* tree);

    void gtUpdateStmtSideEffects(Statement* stmt);

    void gtUpdateNodeSideEffects(GenTree* tree);

    void gtUpdateNodeOperSideEffects(GenTree* tree);

    // Returns "true" iff the complexity (not formally defined, but first interpretation
    // is #of nodes in subtree) of "tree" is greater than "limit".
    // (This is somewhat redundant with the "GetCostEx()/GetCostSz()" fields, but can be used
    // before they have been set.)
    bool gtComplexityExceeds(GenTree* tree, unsigned limit);

    GenTree* gtReverseCond(GenTree* tree);

    static bool gtHasRef(GenTree* tree, unsigned lclNum);

    bool gtHasLocalsWithAddrOp(GenTree* tree);
    bool gtHasAddressExposedLocals(GenTree* tree);

    unsigned gtSetCallArgsOrder(CallArgs* args, bool lateArgs, int* callCostEx, int* callCostSz);
    unsigned gtSetMultiOpOrder(GenTreeMultiOp* multiOp);

    void gtWalkOp(GenTree** op1, GenTree** op2, GenTree* base, bool constOnly);

#ifdef DEBUG
    unsigned gtHashValue(GenTree* tree);

    GenTree* gtWalkOpEffectiveVal(GenTree* op);
#endif

    void gtPrepareCost(GenTree* tree);
    bool gtIsLikelyRegVar(GenTree* tree);
    void gtGetLclVarNodeCost(GenTreeLclVar* node, int* pCostEx, int* pCostSz, bool isLikelyRegVar);
    void gtGetLclFldNodeCost(GenTreeLclFld* node, int* pCostEx, int* pCostSz);
    bool gtGetIndNodeCost(GenTreeIndir* node, int* pCostEx, int* pCostSz);

    // Returns true iff the secondNode can be swapped with firstNode.
    bool gtCanSwapOrder(GenTree* firstNode, GenTree* secondNode);

    // Given an address expression, compute its costs and addressing mode opportunities,
    // and mark addressing mode candidates as GTF_DONT_CSE.
    // TODO-Throughput - Consider actually instantiating these early, to avoid
    // having to re-run the algorithm that looks for them (might also improve CQ).
    bool gtMarkAddrMode(GenTree* addr, int* costEx, int* costSz, var_types type);

    unsigned gtSetEvalOrder(GenTree* tree);
    bool gtMayHaveStoreInterference(GenTree* treeWithStores, GenTree* tree);
    bool gtTreeHasLocalRead(GenTree* tree, unsigned lclNum);

    void gtSetStmtInfo(Statement* stmt);

    // Returns "true" iff "node" has any of the side effects in "flags".
    bool gtNodeHasSideEffects(GenTree* node, GenTreeFlags flags);

    // Returns "true" iff "tree" or its (transitive) children have any of the side effects in "flags".
    bool gtTreeHasSideEffects(GenTree* tree, GenTreeFlags flags);

    void gtExtractSideEffList(GenTree*     expr,
                              GenTree**    pList,
                              GenTreeFlags GenTreeFlags = GTF_SIDE_EFFECT,
                              bool         ignoreRoot   = false);

    bool gtSplitTree(
        BasicBlock* block, Statement* stmt, GenTree* splitPoint, Statement** firstNewStmt, GenTree*** splitPointUse);

    // Static fields of struct types (and sometimes the types that those are reduced to) are represented by having the
    // static field contain an object pointer to the boxed struct.  This simplifies the GC implementation...but
    // complicates the JIT somewhat.  This predicate returns "true" iff a node with type "fieldNodeType", representing
    // the given "fldHnd", is such an object pointer.
    bool gtIsStaticFieldPtrToBoxedStruct(var_types fieldNodeType, CORINFO_FIELD_HANDLE fldHnd);

    bool gtStoreDefinesField(
        LclVarDsc* fieldVarDsc, ssize_t offset, unsigned size, ssize_t* pFieldStoreOffset, unsigned* pFieldStoreSize);

    void gtPeelOffsets(GenTree** addr, target_ssize_t* offset, FieldSeq** fldSeq = nullptr);

    // Return true if call is a recursive call; return false otherwise.
    // Note when inlining, this looks for calls back to the root method.
    bool gtIsRecursiveCall(GenTreeCall* call)
    {
        return gtIsRecursiveCall(call->gtCallMethHnd);
    }

    bool gtIsRecursiveCall(CORINFO_METHOD_HANDLE callMethodHandle)
    {
        return (callMethodHandle == impInlineRoot()->info.compMethodHnd);
    }

    //-------------------------------------------------------------------------

    GenTree* gtFoldExpr(GenTree* tree);
    GenTree* gtFoldExprConst(GenTree* tree);
    GenTree* gtFoldIndirConst(GenTreeIndir* indir);
    GenTree* gtFoldExprSpecial(GenTree* tree);
    GenTree* gtFoldBoxNullable(GenTree* tree);
    GenTree* gtFoldExprCompare(GenTree* tree);
    GenTree* gtFoldExprConditional(GenTree* tree);
    GenTree* gtFoldExprCall(GenTreeCall* call);
    GenTree* gtFoldTypeCompare(GenTree* tree);
    GenTree* gtFoldTypeEqualityCall(bool isEq, GenTree* op1, GenTree* op2);

    // Options to control behavior of gtTryRemoveBoxUpstreamEffects
    enum BoxRemovalOptions
    {
        BR_REMOVE_AND_NARROW, // remove effects, minimize remaining work, return possibly narrowed source tree
        BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE, // remove effects and minimize remaining work, return type handle tree
        BR_REMOVE_BUT_NOT_NARROW,              // remove effects, return original source tree
        BR_DONT_REMOVE,                        // check if removal is possible, return copy source tree
        BR_DONT_REMOVE_WANT_TYPE_HANDLE,       // check if removal is possible, return type handle tree
        BR_MAKE_LOCAL_COPY                     // revise box to copy to temp local and return local's address
    };

    GenTree* gtTryRemoveBoxUpstreamEffects(GenTree* tree, BoxRemovalOptions options = BR_REMOVE_AND_NARROW);
    GenTree* gtOptimizeEnumHasFlag(GenTree* thisOp, GenTree* flagOp);

    //-------------------------------------------------------------------------
    // Get the handle for a ref type.
    CORINFO_CLASS_HANDLE gtGetClassHandle(GenTree* tree, bool* pIsExact, bool* pIsNonNull);
    // Get the class handle for an helper call
    CORINFO_CLASS_HANDLE gtGetHelperCallClassHandle(GenTreeCall* call, bool* pIsExact, bool* pIsNonNull);
    // Get the element handle for an array of ref type.
    CORINFO_CLASS_HANDLE gtGetArrayElementClassHandle(GenTree* array);
    // Get a class handle from a helper call argument
    CORINFO_CLASS_HANDLE gtGetHelperArgClassHandle(GenTree* array);
    // Get the class handle for a field
    CORINFO_CLASS_HANDLE gtGetFieldClassHandle(CORINFO_FIELD_HANDLE fieldHnd, bool* pIsExact, bool* pIsNonNull);
    // Check if this tree is a typeof()
    bool gtIsTypeof(GenTree* tree, CORINFO_CLASS_HANDLE* handle = nullptr);

    GenTreeLclVarCommon* gtCallGetDefinedRetBufLclAddr(GenTreeCall* call);

//-------------------------------------------------------------------------
// Functions to display the trees

#ifdef DEBUG
    void gtDispNode(GenTree* tree, IndentStack* indentStack, _In_z_ const char* msg, bool isLIR);

    void gtDispConst(GenTree* tree);
    void gtDispLeaf(GenTree* tree, IndentStack* indentStack);
    void gtDispLocal(GenTreeLclVarCommon* tree, IndentStack* indentStack);
    void gtDispNodeName(GenTree* tree);
#if FEATURE_MULTIREG_RET
    unsigned gtDispMultiRegCount(GenTree* tree);
#endif
    void gtDispRegVal(GenTree* tree);
    void gtDispVN(GenTree* tree);
    void gtDispCommonEndLine(GenTree* tree);

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
    void gtDispChild(GenTree*             child,
                     IndentStack*         indentStack,
                     IndentInfo           arcType,
                     _In_opt_ const char* msg     = nullptr,
                     bool                 topOnly = false);
    void gtDispTree(GenTree*             tree,
                    IndentStack*         indentStack = nullptr,
                    _In_opt_ const char* msg         = nullptr,
                    bool                 topOnly     = false,
                    bool                 isLIR       = false);
    void gtGetLclVarNameInfo(unsigned lclNum, const char** ilKindOut, const char** ilNameOut, unsigned* ilNumOut);
    int gtGetLclVarName(unsigned lclNum, char* buf, unsigned buf_remaining);
    char* gtGetLclVarName(unsigned lclNum);
    void gtDispLclVar(unsigned lclNum, bool padForBiggestDisp = true);
    void gtDispLclVarStructType(unsigned lclNum);
    void gtDispSsaName(unsigned lclNum, unsigned ssaNum, bool isDef);
    void gtDispClassLayout(ClassLayout* layout, var_types type);
    void gtDispILLocation(const ILLocation& loc);
    void gtDispStmt(Statement* stmt, const char* msg = nullptr);
    void gtDispBlockStmts(BasicBlock* block);
    void gtPrintArgPrefix(GenTreeCall* call, CallArg* arg, char** bufp, unsigned* bufLength);
    const char* gtGetWellKnownArgNameForArgMsg(WellKnownArg arg);
    void gtGetArgMsg(GenTreeCall* call, CallArg* arg, char* bufp, unsigned bufLength);
    void gtGetLateArgMsg(GenTreeCall* call, CallArg* arg, char* bufp, unsigned bufLength);
    void gtDispArgList(GenTreeCall* call, GenTree* lastCallOperand, IndentStack* indentStack);
    void gtDispFieldSeq(FieldSeq* fieldSeq, ssize_t offset);

    void gtDispRange(LIR::ReadOnlyRange const& range);

    void gtDispTreeRange(LIR::Range& containingRange, GenTree* tree);

    void gtDispLIRNode(GenTree* node, const char* prefixMsg = nullptr);
#endif

    // For tree walks

    enum fgWalkResult
    {
        WALK_CONTINUE,
        WALK_SKIP_SUBTREES,
        WALK_ABORT
    };
    struct fgWalkData;
    typedef fgWalkResult(fgWalkPreFn)(GenTree** pTree, fgWalkData* data);
    typedef fgWalkResult(fgWalkPostFn)(GenTree** pTree, fgWalkData* data);

    static fgWalkPreFn gtMarkColonCond;
    static fgWalkPreFn gtClearColonCond;

    struct FindLinkData
    {
        GenTree*  nodeToFind;
        GenTree** result;
        GenTree*  parent;
    };

    FindLinkData gtFindLink(Statement* stmt, GenTree* node);
    bool gtHasCatchArg(GenTree* tree);

    typedef ArrayStack<GenTree*> GenTreeStack;

//=========================================================================
// BasicBlock functions
#ifdef DEBUG
    // When false, assert when creating a new basic block.
    bool fgSafeBasicBlockCreation;

    // When false, assert when creating a new flow edge
    bool fgSafeFlowEdgeCreation;
#endif

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
    RefCountState lvaRefCountState; // Current local ref count state

    bool lvaLocalVarRefCounted() const
    {
        return lvaRefCountState == RCS_NORMAL;
    }

    bool     lvaTrackedFixed; // true: We cannot add new 'tracked' variable
    unsigned lvaCount;        // total number of locals, which includes function arguments,
                              // special arguments, IL local variables, and JIT temporary variables

    LclVarDsc* lvaTable;    // variable descriptor table
    unsigned   lvaTableCnt; // lvaTable size (>= lvaCount)

    unsigned lvaTrackedCount;             // actual # of locals being tracked
    unsigned lvaTrackedCountInSizeTUnits; // min # of size_t's sufficient to hold a bit for all the locals being tracked

#ifdef DEBUG
    VARSET_TP lvaTrackedVars; // set of tracked variables
#endif
#ifndef TARGET_64BIT
    VARSET_TP lvaLongVars; // set of long (64-bit) variables
#endif
    VARSET_TP lvaFloatVars; // set of floating-point (32-bit and 64-bit) or SIMD variables
#ifdef TARGET_XARCH
    VARSET_TP lvaMaskVars; // set of mask variables
#endif // TARGET_XARCH

    unsigned lvaCurEpoch; // VarSets are relative to a specific set of tracked var indices.
                          // It that changes, this changes.  VarSets from different epochs
                          // cannot be meaningfully combined.

    unsigned GetCurLVEpoch()
    {
        return lvaCurEpoch;
    }

    // reverse map of tracked number to var number
    unsigned  lvaTrackedToVarNumSize;
    unsigned* lvaTrackedToVarNum;

#if DOUBLE_ALIGN
#ifdef DEBUG
    // # of procs compiled a with double-aligned stack
    static unsigned s_lvaDoubleAlignedProcsCount;
#endif
#endif

    // Getters and setters for address-exposed and do-not-enregister local var properties.
    bool lvaVarAddrExposed(unsigned varNum) const;
    void lvaSetVarAddrExposed(unsigned varNum DEBUGARG(AddressExposedReason reason));
    void lvaSetHiddenBufferStructArg(unsigned varNum);
    void lvaSetVarLiveInOutOfHandler(unsigned varNum);
    bool lvaVarDoNotEnregister(unsigned varNum);

    void lvSetMinOptsDoNotEnreg();

    bool lvaEnregEHVars;
    bool lvaEnregMultiRegVars;

    void lvaSetVarDoNotEnregister(unsigned varNum DEBUGARG(DoNotEnregisterReason reason));

    unsigned lvaVarargsHandleArg;
#ifdef TARGET_X86
    unsigned lvaVarargsBaseOfStkArgs; // Pointer (computed based on incoming varargs handle) to the start of the stack
                                      // arguments
#endif                                // TARGET_X86

    unsigned lvaInlinedPInvokeFrameVar; // variable representing the InlinedCallFrame
    unsigned lvaReversePInvokeFrameVar; // variable representing the reverse PInvoke frame
    unsigned lvaMonAcquired; // boolean variable introduced into in synchronized methods
                             // that tracks whether the lock has been taken

    unsigned lvaArg0Var; // The lclNum of arg0. Normally this will be info.compThisArg.
                         // However, if there is a "ldarga 0" or "starg 0" in the IL,
                         // we will redirect all "ldarg(a) 0" and "starg 0" to this temp.

    unsigned lvaInlineeReturnSpillTemp; // The temp to spill the non-VOID return expression
                                        // in case there are multiple BBJ_RETURN blocks in the inlinee
                                        // or if the inlinee has GC ref locals.

#if FEATURE_FIXED_OUT_ARGS
    unsigned            lvaOutgoingArgSpaceVar;  // var that represents outgoing argument space
    PhasedVar<unsigned> lvaOutgoingArgSpaceSize; // size of fixed outgoing argument space
#endif                                           // FEATURE_FIXED_OUT_ARGS

    static unsigned GetOutgoingArgByteSize(unsigned sizeWithoutPadding)
    {
        return roundUp(sizeWithoutPadding, TARGET_POINTER_SIZE);
    }

    // Variable representing the return address. The helper-based tailcall
    // mechanism passes the address of the return address to a runtime helper
    // where it is used to detect tail-call chains.
    unsigned lvaRetAddrVar;

#if defined(DEBUG) && defined(TARGET_XARCH)

    unsigned lvaReturnSpCheck; // Stores SP to confirm it is not corrupted on return.

#endif // defined(DEBUG) && defined(TARGET_XARCH)

#if defined(DEBUG) && defined(TARGET_X86)

    unsigned lvaCallSpCheck; // Stores SP to confirm it is not corrupted after every call.

#endif // defined(DEBUG) && defined(TARGET_X86)

    bool lvaGenericsContextInUse;

    bool lvaKeepAliveAndReportThis(); // Synchronized instance method of a reference type, or
                                      // CORINFO_GENERICS_CTXT_FROM_THIS?
    bool lvaReportParamTypeArg();     // Exceptions and CORINFO_GENERICS_CTXT_FROM_PARAMTYPEARG?

//-------------------------------------------------------------------------
// All these frame offsets are inter-related and must be kept in sync

#if !defined(FEATURE_EH_FUNCLETS)
    // This is used for the callable handlers
    unsigned lvaShadowSPslotsVar; // Block-layout TYP_STRUCT variable for all the shadow SP slots
#endif                            // FEATURE_EH_FUNCLETS

    int lvaCachedGenericContextArgOffs;
    int lvaCachedGenericContextArgOffset(); // For CORINFO_CALLCONV_PARAMTYPE and if generic context is passed as
                                            // THIS pointer

#ifdef JIT32_GCENCODER

    unsigned lvaLocAllocSPvar; // variable which stores the value of ESP after the last alloca/localloc

#endif // JIT32_GCENCODER

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
#ifdef TARGET_ARM
    bool lvaIsPreSpilled(unsigned lclNum, regMaskTP preSpillMask);
#endif // TARGET_ARM
    void lvaAssignFrameOffsets(FrameLayoutState curState);
    void lvaFixVirtualFrameOffsets();
    void lvaUpdateArgWithInitialReg(LclVarDsc* varDsc);
    void lvaUpdateArgsWithInitialReg();
    void lvaAssignVirtualFrameOffsetsToArgs();
#ifdef UNIX_AMD64_ABI
    int lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs, int* callerArgOffset);
#else  // !UNIX_AMD64_ABI
    int lvaAssignVirtualFrameOffsetToArg(unsigned lclNum, unsigned argSize, int argOffs);
#endif // !UNIX_AMD64_ABI
    void lvaAssignVirtualFrameOffsetsToLocals();
    int lvaAllocLocalAndSetVirtualOffset(unsigned lclNum, unsigned size, int stkOffs);
#ifdef TARGET_AMD64
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
    int lvaToCallerSPRelativeOffset(int offs, bool isFpBased, bool forRootFrame = true) const;

    // Returns the caller-SP-relative offset for the local variable "varNum."
    int lvaGetCallerSPRelativeOffset(unsigned varNum);

    // Returns the SP-relative offset for the local variable "varNum". Illegal to ask this for functions with localloc.
    int lvaGetSPRelativeOffset(unsigned varNum);

    int lvaToInitialSPRelativeOffset(unsigned offset, bool isFpBased);
    int lvaGetInitialSPRelativeOffset(unsigned varNum);

    // True if this is an OSR compilation and this local is potentially
    // located on the original method stack frame.
    bool lvaIsOSRLocal(unsigned varNum);

    //------------------------ For splitting types ----------------------------

    void lvaInitTypeRef();

    void lvaInitArgs(InitVarDscInfo* varDscInfo);
    void lvaInitThisPtr(InitVarDscInfo* varDscInfo);
    void lvaInitRetBuffArg(InitVarDscInfo* varDscInfo, bool useFixedRetBufReg);
    void lvaInitUserArgs(InitVarDscInfo* varDscInfo, unsigned skipArgs, unsigned takeArgs);
    void lvaInitGenericsCtxt(InitVarDscInfo* varDscInfo);
    void lvaInitVarArgsHandle(InitVarDscInfo* varDscInfo);

    void lvaInitVarDsc(LclVarDsc*              varDsc,
                       unsigned                varNum,
                       CorInfoType             corInfoType,
                       CORINFO_CLASS_HANDLE    typeHnd,
                       CORINFO_ARG_LIST_HANDLE varList,
                       CORINFO_SIG_INFO*       varSig);

    var_types lvaGetActualType(unsigned lclNum);
    var_types lvaGetRealType(unsigned lclNum);

    //-------------------------------------------------------------------------

    void lvaInit();

    LclVarDsc* lvaGetDesc(unsigned lclNum)
    {
        assert(lclNum < lvaCount);
        return &lvaTable[lclNum];
    }

    LclVarDsc* lvaGetDesc(unsigned lclNum) const
    {
        assert(lclNum < lvaCount);
        return &lvaTable[lclNum];
    }

    LclVarDsc* lvaGetDesc(const GenTreeLclVarCommon* lclVar)
    {
        return lvaGetDesc(lclVar->GetLclNum());
    }

    unsigned lvaTrackedIndexToLclNum(unsigned trackedIndex)
    {
        assert(trackedIndex < lvaTrackedCount);
        unsigned lclNum = lvaTrackedToVarNum[trackedIndex];
        assert(lclNum < lvaCount);
        return lclNum;
    }

    LclVarDsc* lvaGetDescByTrackedIndex(unsigned trackedIndex)
    {
        return lvaGetDesc(lvaTrackedIndexToLclNum(trackedIndex));
    }

    unsigned lvaGetLclNum(const LclVarDsc* varDsc)
    {
        assert((lvaTable <= varDsc) && (varDsc < lvaTable + lvaCount)); // varDsc must point within the table
        assert(((char*)varDsc - (char*)lvaTable) % sizeof(LclVarDsc) ==
               0); // varDsc better not point in the middle of a variable
        unsigned varNum = (unsigned)(varDsc - lvaTable);
        assert(varDsc == &lvaTable[varNum]);
        return varNum;
    }

    unsigned lvaLclSize(unsigned varNum);
    unsigned lvaLclExactSize(unsigned varNum);

    bool lvaHaveManyLocals(float percent = 1.0f) const;

    unsigned lvaGrabTemp(bool shortLifetime DEBUGARG(const char* reason));
    unsigned lvaGrabTemps(unsigned cnt DEBUGARG(const char* reason));
    unsigned lvaGrabTempWithImplicitUse(bool shortLifetime DEBUGARG(const char* reason));

    void lvaSortByRefCount();

    PhaseStatus lvaMarkLocalVars(); // Local variable ref-counting
    void lvaComputeRefCounts(bool isRecompute, bool setSlotNumbers);
    void lvaMarkLocalVars(BasicBlock* block, bool isRecompute);

    void lvaAllocOutgoingArgSpaceVar(); // Set up lvaOutgoingArgSpaceVar

#ifdef DEBUG
    struct lvaStressLclFldArgs
    {
        Compiler* m_pCompiler;
        bool      m_bFirstPass;
    };

    static fgWalkPreFn lvaStressLclFldCB;
    void               lvaStressLclFld();
    unsigned lvaStressLclFldPadding(unsigned lclNum);

    void lvaDispVarSet(VARSET_VALARG_TP set, VARSET_VALARG_TP allVars);
    void lvaDispVarSet(VARSET_VALARG_TP set);

#endif

#ifdef TARGET_ARM
    int lvaFrameAddress(int varNum, bool mustBeFPBased, regNumber* pBaseReg, int addrModeOffset, bool isFloatUsage);
#else
    int lvaFrameAddress(int varNum, bool* pFPbased);
#endif

    bool lvaIsParameter(unsigned varNum);
    bool lvaIsRegArgument(unsigned varNum);
    bool lvaIsOriginalThisArg(unsigned varNum); // Is this varNum the original this argument?
    bool lvaIsOriginalThisReadOnly();           // return true if there is no place in the code
                                                // that writes to arg0

#ifdef TARGET_X86
    bool lvaIsArgAccessedViaVarArgsCookie(unsigned lclNum)
    {
        if (!info.compIsVarArgs)
        {
            return false;
        }

        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        return varDsc->lvIsParam && !varDsc->lvIsRegArg && (lclNum != lvaVarargsHandleArg);
    }
#endif // TARGET_X86

    bool lvaIsImplicitByRefLocal(unsigned lclNum) const;
    bool lvaIsLocalImplicitlyAccessedByRef(unsigned lclNum) const;

    // Returns true if this local var is a multireg struct
    bool lvaIsMultiregStruct(LclVarDsc* varDsc, bool isVararg);

    // If the local is a TYP_STRUCT, get/set a class handle describing it
    void lvaSetStruct(unsigned varNum, ClassLayout* layout, bool unsafeValueClsCheck);
    void lvaSetStruct(unsigned varNum, CORINFO_CLASS_HANDLE typeHnd, bool unsafeValueClsCheck);
    void lvaSetStructUsedAsVarArg(unsigned varNum);

    // If the local is TYP_REF, set or update the associated class information.
    void lvaSetClass(unsigned varNum, CORINFO_CLASS_HANDLE clsHnd, bool isExact = false);
    void lvaSetClass(unsigned varNum, GenTree* tree, CORINFO_CLASS_HANDLE stackHandle = nullptr);
    void lvaUpdateClass(unsigned varNum, CORINFO_CLASS_HANDLE clsHnd, bool isExact = false);
    void lvaUpdateClass(unsigned varNum, GenTree* tree, CORINFO_CLASS_HANDLE stackHandle = nullptr);

#define MAX_NumOfFieldsInPromotableStruct 4 // Maximum number of fields in promotable struct

    // Info about struct type fields.
    struct lvaStructFieldInfo
    {
        // Class handle for SIMD type recognition, see CORINFO_TYPE_LAYOUT_NODE
        // for more details on the restrictions.
        CORINFO_CLASS_HANDLE fldSIMDTypeHnd = NO_CLASS_HANDLE;
        uint8_t              fldOffset = 0;
        uint8_t              fldOrdinal = 0;
        var_types            fldType = TYP_UNDEF;
        unsigned             fldSize = 0;

#ifdef DEBUG
        // Field handle for diagnostic purposes only. See CORINFO_TYPE_LAYOUT_NODE.
        CORINFO_FIELD_HANDLE diagFldHnd = NO_FIELD_HANDLE;
#endif
    };

    // Info about a struct type, instances of which may be candidates for promotion.
    struct lvaStructPromotionInfo
    {
        CORINFO_CLASS_HANDLE typeHnd;
        bool                 canPromote;
        bool                 containsHoles;
        bool                 anySignificantPadding;
        bool                 fieldsSorted;
        unsigned char        fieldCnt;
        lvaStructFieldInfo   fields[MAX_NumOfFieldsInPromotableStruct];

        lvaStructPromotionInfo(CORINFO_CLASS_HANDLE typeHnd = nullptr)
            : typeHnd(typeHnd)
            , canPromote(false)
            , containsHoles(false)
            , anySignificantPadding(false)
            , fieldsSorted(false)
            , fieldCnt(0)
        {
        }
    };

    // This class is responsible for checking validity and profitability of struct promotion.
    // If it is both legal and profitable, then TryPromoteStructVar promotes the struct and initializes
    // necessary information for fgMorphStructField to use.
    class StructPromotionHelper
    {
    public:
        StructPromotionHelper(Compiler* compiler);

        bool CanPromoteStructType(CORINFO_CLASS_HANDLE typeHnd);
        bool TryPromoteStructVar(unsigned lclNum);
        void Clear()
        {
            structPromotionInfo.typeHnd = NO_CLASS_HANDLE;
        }

    private:
        bool CanPromoteStructVar(unsigned lclNum);
        bool ShouldPromoteStructVar(unsigned lclNum);
        void PromoteStructVar(unsigned lclNum);
        void SortStructFields();

        var_types TryPromoteValueClassAsPrimitive(CORINFO_TYPE_LAYOUT_NODE* treeNodes, size_t maxTreeNodes, size_t index);
        void AdvanceSubTree(CORINFO_TYPE_LAYOUT_NODE* treeNodes, size_t maxTreeNodes, size_t* index);

    private:
        Compiler*              compiler;
        lvaStructPromotionInfo structPromotionInfo;
    };

    StructPromotionHelper* structPromotionHelper;

    unsigned lvaGetFieldLocal(const LclVarDsc* varDsc, unsigned int fldOffset);
    lvaPromotionType lvaGetPromotionType(const LclVarDsc* varDsc);
    lvaPromotionType lvaGetPromotionType(unsigned varNum);
    lvaPromotionType lvaGetParentPromotionType(const LclVarDsc* varDsc);
    lvaPromotionType lvaGetParentPromotionType(unsigned varNum);
    bool lvaIsFieldOfDependentlyPromotedStruct(const LclVarDsc* varDsc);
    bool lvaIsGCTracked(const LclVarDsc* varDsc);

#if defined(FEATURE_SIMD)
    bool lvaMapSimd12ToSimd16(const LclVarDsc* varDsc)
    {
        assert(varDsc->lvType == TYP_SIMD12);

#if defined(TARGET_64BIT)
        assert(compAppleArm64Abi() || varDsc->lvSize() == 16);
#endif // defined(TARGET_64BIT)

        // We make local variable SIMD12 types 16 bytes instead of just 12.
        // lvSize() will return 16 bytes for SIMD12, even for fields.
        // However, we can't do that mapping if the var is a dependently promoted struct field.
        // Such a field must remain its exact size within its parent struct unless it is a single
        // field *and* it is the only field in a struct of 16 bytes.
        if (varDsc->lvSize() != 16)
        {
            return false;
        }
        if (lvaIsFieldOfDependentlyPromotedStruct(varDsc))
        {
            LclVarDsc* parentVarDsc = lvaGetDesc(varDsc->lvParentLcl);
            return (parentVarDsc->lvFieldCnt == 1) && (parentVarDsc->lvSize() == 16);
        }
        return true;
    }
#endif // defined(FEATURE_SIMD)

    unsigned lvaGSSecurityCookie; // LclVar number
    bool     lvaTempsHaveLargerOffsetThanVars();

    // Returns "true" iff local variable "lclNum" is in SSA form.
    bool lvaInSsa(unsigned lclNum) const
    {
        return lvaGetDesc(lclNum)->lvInSsa;
    }

    unsigned lvaStubArgumentVar; // variable representing the secret stub argument coming in EAX

#if defined(FEATURE_EH_FUNCLETS)
    unsigned lvaPSPSym; // variable representing the PSPSym
#endif

    InlineInfo*     impInlineInfo; // Only present for inlinees
    InlineStrategy* m_inlineStrategy;

    InlineContext* compInlineContext; // Always present

    // The Compiler* that is the root of the inlining tree of which "this" is a member.
    Compiler* impInlineRoot();

#if defined(DEBUG)
    unsigned __int64 getInlineCycleCount()
    {
        return m_compCycles;
    }
#endif // defined(DEBUG)

    bool fgNoStructPromotion;      // Set to TRUE to turn off struct promotion for this method.
    bool fgNoStructParamPromotion; // Set to TRUE to turn off struct promotion for parameters this method.

    //=========================================================================
    //                          PROTECTED
    //=========================================================================

protected:
    //---------------- Local variable ref-counting ----------------------------

    void lvaMarkLclRefs(GenTree* tree, BasicBlock* block, Statement* stmt, bool isRecompute);
    bool IsDominatedByExceptionalEntry(BasicBlock* block);
    void SetHasExceptionalUsesHint(LclVarDsc* varDsc);

    // Keeps the mapping from SSA #'s to VN's for the implicit memory variables.
    SsaDefArray<SsaMemDef> lvMemoryPerSsaData;

public:
    // Returns the address of the per-Ssa data for memory at the given ssaNum (which is required
    // not to be the SsaConfig::RESERVED_SSA_NUM, which indicates that the variable is
    // not an SSA variable).
    SsaMemDef* GetMemoryPerSsaData(unsigned ssaNum)
    {
        return lvMemoryPerSsaData.GetSsaDef(ssaNum);
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

private:
    // For prefixFlags
    enum
    {
        PREFIX_TAILCALL_EXPLICIT = 0x00000001, // call has "tail" IL prefix
        PREFIX_TAILCALL_IMPLICIT =
            0x00000002, // call is treated as having "tail" prefix even though there is no "tail" IL prefix
        PREFIX_TAILCALL    = PREFIX_TAILCALL_EXPLICIT | PREFIX_TAILCALL_IMPLICIT,
        PREFIX_VOLATILE    = 0x00000004,
        PREFIX_UNALIGNED   = 0x00000008,
        PREFIX_CONSTRAINED = 0x00000010,
        PREFIX_READONLY    = 0x00000020,

#ifdef DEBUG
        PREFIX_TAILCALL_STRESS = 0x00000040, // call doesn't "tail" IL prefix but is treated as explicit because of tail call stress
#endif
    };

    static void impValidateMemoryAccessOpcode(const BYTE* codeAddr, const BYTE* codeEndp, bool volatilePrefix);
    static OPCODE impGetNonPrefixOpcode(const BYTE* codeAddr, const BYTE* codeEndp);
    static GenTreeFlags impPrefixFlagsToIndirFlags(unsigned prefixFlags);
    static bool impOpcodeIsCallOpcode(OPCODE opcode);

public:
    void impInit();
    void impImport();
    void impFixPredLists();

    CORINFO_CLASS_HANDLE impGetRefAnyClass();
    CORINFO_CLASS_HANDLE impGetRuntimeArgumentHandle();
    CORINFO_CLASS_HANDLE impGetTypeHandleClass();
    CORINFO_CLASS_HANDLE impGetStringClass();
    CORINFO_CLASS_HANDLE impGetObjectClass();

    // Returns underlying type of handles returned by ldtoken instruction
    var_types GetRuntimeHandleUnderlyingType()
    {
        // RuntimeTypeHandle is backed by raw pointer on NativeAOT and by object reference on other runtimes
        return IsTargetAbi(CORINFO_NATIVEAOT_ABI) ? TYP_I_IMPL : TYP_REF;
    }

    void impDevirtualizeCall(GenTreeCall*            call,
                             CORINFO_RESOLVED_TOKEN* pResolvedToken,
                             CORINFO_METHOD_HANDLE*  method,
                             unsigned*               methodFlags,
                             CORINFO_CONTEXT_HANDLE* contextHandle,
                             CORINFO_CONTEXT_HANDLE* exactContextHandle,
                             bool                    isLateDevirtualization,
                             bool                    isExplicitTailCall,
                             IL_OFFSET               ilOffset = BAD_IL_OFFSET);

    bool impConsiderCallProbe(GenTreeCall* call, IL_OFFSET ilOffset);

    enum class GDVProbeType
    {
        None,
        ClassProfile,
        MethodProfile,
        MethodAndClassProfile,
    };

    GDVProbeType compClassifyGDVProbeType(GenTreeCall* call);

    //=========================================================================
    //                          PROTECTED
    //=========================================================================

protected:
    //-------------------- Stack manipulation ---------------------------------

    unsigned impStkSize; // Size of the full stack

#define SMALL_STACK_SIZE 16 // number of elements in impSmallStack

    struct SavedStack // used to save/restore stack contents.
    {
        unsigned    ssDepth; // number of values on stack
        StackEntry* ssTrees; // saved tree values
    };

    bool impIsPrimitive(CorInfoType type);
    bool impILConsumesAddr(const BYTE* codeAddr);

    void impResolveToken(const BYTE* addr, CORINFO_RESOLVED_TOKEN* pResolvedToken, CorInfoTokenKind kind);

    void impPushOnStack(GenTree* tree, typeInfo ti);
    StackEntry impPopStack();
    void impPopStack(unsigned n);
    StackEntry& impStackTop(unsigned n = 0);
    unsigned impStackHeight();

    void impSaveStackState(SavedStack* savePtr, bool copy);
    void impRestoreStackState(SavedStack* savePtr);

    GenTree* impImportLdvirtftn(GenTree* thisPtr, CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);

    enum class BoxPatterns
    {
        None                  = 0,
        IsByRefLike           = 1,
        MakeInlineObservation = 2,
    };

    int impBoxPatternMatch(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                           const BYTE*             codeAddr,
                           const BYTE*             codeEndp,
                           BoxPatterns             opts);
    void impImportAndPushBox(CORINFO_RESOLVED_TOKEN* pResolvedToken);

    void impImportNewObjArray(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);

    bool impCanPInvokeInline();
    bool impCanPInvokeInlineCallSite(BasicBlock* block);
    void impCheckForPInvokeCall(
        GenTreeCall* call, CORINFO_METHOD_HANDLE methHnd, CORINFO_SIG_INFO* sig, unsigned mflags, BasicBlock* block);
    GenTreeCall* impImportIndirectCall(CORINFO_SIG_INFO* sig, const DebugInfo& di = DebugInfo());
    void impPopArgsForUnmanagedCall(GenTreeCall* call, CORINFO_SIG_INFO* sig);

    void impInsertHelperCall(CORINFO_HELPER_DESC* helperCall);
    void impHandleAccessAllowed(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall);
    void impHandleAccessAllowedInternal(CorInfoIsAccessAllowedResult result, CORINFO_HELPER_DESC* helperCall);

    var_types impImportCall(OPCODE                  opcode,
                            CORINFO_RESOLVED_TOKEN* pResolvedToken,
                            CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken, // Is this a "constrained." call on a
                                                                               // type parameter?
                            GenTree*           newobjThis,
                            int                prefixFlags,
                            CORINFO_CALL_INFO* callInfo,
                            IL_OFFSET          rawILOffset);

    CORINFO_CLASS_HANDLE impGetSpecialIntrinsicExactReturnType(GenTreeCall* call);

    GenTree* impFixupCallStructReturn(GenTreeCall* call, CORINFO_CLASS_HANDLE retClsHnd);

    GenTree* impFixupStructReturnType(GenTree* op);

    GenTree* impDuplicateWithProfiledArg(GenTreeCall* call, IL_OFFSET ilOffset);

#ifdef DEBUG
    var_types impImportJitTestLabelMark(int numArgs);
#endif // DEBUG

    GenTree* impInitClass(CORINFO_RESOLVED_TOKEN* pResolvedToken);

    GenTree* impImportStaticReadOnlyField(CORINFO_FIELD_HANDLE field, CORINFO_CLASS_HANDLE ownerCls);

    GenTree* impImportStaticFieldAddress(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                         CORINFO_ACCESS_FLAGS    access,
                                         CORINFO_FIELD_INFO*     pFieldInfo,
                                         var_types               lclTyp,
                                         GenTreeFlags*           pIndirFlags,
                                         bool*                   pIsHoistable = nullptr);
    void impAnnotateFieldIndir(GenTreeIndir* indir);

    static void impBashVarAddrsToI(GenTree* tree1, GenTree* tree2 = nullptr);

    GenTree* impImplicitIorI4Cast(GenTree* tree, var_types dstTyp, bool zeroExtend = false);

    GenTree* impImplicitR4orR8Cast(GenTree* tree, var_types dstTyp);

    void impImportLeave(BasicBlock* block);
    void impResetLeaveBlock(BasicBlock* block, unsigned jmpAddr);
    GenTree* impTypeIsAssignable(GenTree* typeTo, GenTree* typeFrom);

    // Mirrors StringComparison.cs
    enum StringComparison
    {
        Ordinal           = 4,
        OrdinalIgnoreCase = 5
    };
    enum StringComparisonJoint
    {
        Eq,  // (d1 == cns1) && (s2 == cns2)
        Xor, // (d1 ^ cns1) | (s2 ^ cns2)
    };
    GenTree* impStringEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags);
    GenTree* impSpanEqualsOrStartsWith(bool startsWith, CORINFO_SIG_INFO* sig, unsigned methodFlags);
    GenTree* impExpandHalfConstEquals(GenTreeLclVarCommon*   data,
                                      GenTree*         lengthFld,
                                      bool             checkForNull,
                                      bool             startsWith,
                                      WCHAR*           cnsData,
                                      int              len,
                                      int              dataOffset,
                                      StringComparison cmpMode);
    GenTree* impCreateCompareInd(GenTreeLclVarCommon*        obj,
                                 var_types             type,
                                 ssize_t               offset,
                                 ssize_t               value,
                                 StringComparison      ignoreCase,
                                 StringComparisonJoint joint = Eq);
    GenTree* impExpandHalfConstEqualsSWAR(
        GenTreeLclVarCommon* data, WCHAR* cns, int len, int dataOffset, StringComparison cmpMode);
    GenTree* impExpandHalfConstEqualsSIMD(
        GenTreeLclVarCommon* data, WCHAR* cns, int len, int dataOffset, StringComparison cmpMode);
    GenTreeStrCon* impGetStrConFromSpan(GenTree* span);

    GenTree* impIntrinsic(GenTree*                newobjThis,
                          CORINFO_CLASS_HANDLE    clsHnd,
                          CORINFO_METHOD_HANDLE   method,
                          CORINFO_SIG_INFO*       sig,
                          unsigned                methodFlags,
                          CORINFO_RESOLVED_TOKEN* pResolvedToken,
                          bool                    readonlyCall,
                          bool                    tailCall,
                          bool                    callvirt,
                          CORINFO_RESOLVED_TOKEN* pContstrainedResolvedToken,
                          CORINFO_THIS_TRANSFORM  constraintCallThisTransform,
                          NamedIntrinsic*         pIntrinsicName,
                          bool*                   isSpecialIntrinsic = nullptr);
    GenTree* impMathIntrinsic(CORINFO_METHOD_HANDLE method,
                              CORINFO_SIG_INFO*     sig,
                              var_types             callType,
                              NamedIntrinsic        intrinsicName,
                              bool                  tailCall);
    GenTree* impMinMaxIntrinsic(CORINFO_METHOD_HANDLE method,
                                CORINFO_SIG_INFO*     sig,
                                CorInfoType           callJitType,
                                NamedIntrinsic        intrinsicName,
                                bool                  tailCall,
                                bool                  isMax,
                                bool                  isMagnitude,
                                bool                  isNumber);

    NamedIntrinsic lookupPrimitiveFloatNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    NamedIntrinsic lookupPrimitiveIntNamedIntrinsic(CORINFO_METHOD_HANDLE method, const char* methodName);
    GenTree* impUnsupportedNamedIntrinsic(unsigned              helper,
                                          CORINFO_METHOD_HANDLE method,
                                          CORINFO_SIG_INFO*     sig,
                                          bool                  mustExpand);

    GenTree* impSRCSUnsafeIntrinsic(NamedIntrinsic        intrinsic,
                                    CORINFO_CLASS_HANDLE  clsHnd,
                                    CORINFO_METHOD_HANDLE method,
                                    CORINFO_SIG_INFO*     sig,
        CORINFO_RESOLVED_TOKEN* pResolvedToken);

    GenTree* impPrimitiveNamedIntrinsic(NamedIntrinsic        intrinsic,
                                        CORINFO_CLASS_HANDLE  clsHnd,
                                        CORINFO_METHOD_HANDLE method,
                                        CORINFO_SIG_INFO*     sig);

#ifdef FEATURE_HW_INTRINSICS
    GenTree* impHWIntrinsic(NamedIntrinsic        intrinsic,
                            CORINFO_CLASS_HANDLE  clsHnd,
                            CORINFO_METHOD_HANDLE method,
                            CORINFO_SIG_INFO*     sig,
                            bool                  mustExpand);
    GenTree* impSimdAsHWIntrinsic(NamedIntrinsic        intrinsic,
                                  CORINFO_CLASS_HANDLE  clsHnd,
                                  CORINFO_METHOD_HANDLE method,
                                  CORINFO_SIG_INFO*     sig,
                                  GenTree*              newobjThis);

protected:
    bool compSupportsHWIntrinsic(CORINFO_InstructionSet isa);

    GenTree* impSimdAsHWIntrinsicSpecial(NamedIntrinsic       intrinsic,
                                         CORINFO_CLASS_HANDLE clsHnd,
                                         CORINFO_SIG_INFO*    sig,
                                         var_types            retType,
                                         CorInfoType          simdBaseJitType,
                                         unsigned             simdSize,
                                         GenTree*             newobjThis);

    GenTree* impSpecialIntrinsic(NamedIntrinsic        intrinsic,
                                 CORINFO_CLASS_HANDLE  clsHnd,
                                 CORINFO_METHOD_HANDLE method,
                                 CORINFO_SIG_INFO*     sig,
                                 CorInfoType           simdBaseJitType,
                                 var_types             retType,
                                 unsigned              simdSize);

    GenTree* getArgForHWIntrinsic(var_types            argType,
                                  CORINFO_CLASS_HANDLE argClass,
                                  bool                 expectAddr = false,
                                  GenTree*             newobjThis = nullptr);
    GenTree* impNonConstFallback(NamedIntrinsic intrinsic, var_types simdType, CorInfoType simdBaseJitType);
    GenTree* addRangeCheckIfNeeded(
        NamedIntrinsic intrinsic, GenTree* immOp, bool mustExpand, int immLowerBound, int immUpperBound);
    GenTree* addRangeCheckForHWIntrinsic(GenTree* immOp, int immLowerBound, int immUpperBound);

#endif // FEATURE_HW_INTRINSICS
    GenTree* impArrayAccessIntrinsic(CORINFO_CLASS_HANDLE clsHnd,
                                     CORINFO_SIG_INFO*    sig,
                                     int                  memberRef,
                                     bool                 readonlyCall,
                                     NamedIntrinsic       intrinsicName);
    GenTree* impInitializeArrayIntrinsic(CORINFO_SIG_INFO* sig);
    GenTree* impCreateSpanIntrinsic(CORINFO_SIG_INFO* sig);

    GenTree* impKeepAliveIntrinsic(GenTree* objToKeepAlive);

    GenTree* impMethodPointer(CORINFO_RESOLVED_TOKEN* pResolvedToken, CORINFO_CALL_INFO* pCallInfo);

    GenTree* impTransformThis(GenTree*                thisPtr,
                              CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken,
                              CORINFO_THIS_TRANSFORM  transform);

    //----------------- Manipulating the trees and stmts ----------------------

    Statement* impStmtList; // Statements for the BB being imported.
    Statement* impLastStmt; // The last statement for the current BB.

public:
    static const unsigned CHECK_SPILL_ALL  = static_cast<unsigned>(-1);
    static const unsigned CHECK_SPILL_NONE = static_cast<unsigned>(-2);

    NamedIntrinsic lookupNamedIntrinsic(CORINFO_METHOD_HANDLE method);
    void impBeginTreeList();
    void impEndTreeList(BasicBlock* block, Statement* firstStmt, Statement* lastStmt);
    void impEndTreeList(BasicBlock* block);
    void impAppendStmtCheck(Statement* stmt, unsigned chkLevel);
    void impAppendStmt(Statement* stmt, unsigned chkLevel, bool checkConsumedDebugInfo = true);
    void impAppendStmt(Statement* stmt);
    void impInsertStmtBefore(Statement* stmt, Statement* stmtBefore);
    Statement* impAppendTree(GenTree* tree, unsigned chkLevel, const DebugInfo& di, bool checkConsumedDebugInfo = true);
    void impStoreTemp(unsigned         lclNum,
                      GenTree*         val,
                      unsigned         curLevel,
                      Statement**      pAfterStmt = nullptr,
                      const DebugInfo& di         = DebugInfo(),
                      BasicBlock*      block      = nullptr);
    Statement* impExtractLastStmt();
    GenTree* impCloneExpr(GenTree*             tree,
                          GenTree**            clone,
                          unsigned             curLevel,
                          Statement** pAfterStmt DEBUGARG(const char* reason));
    GenTree* impStoreStruct(GenTree*         store,
                             unsigned         curLevel,
                             Statement**      pAfterStmt = nullptr,
                             const DebugInfo& di         = DebugInfo(),
                             BasicBlock*      block      = nullptr);
    GenTree* impStoreStructPtr(GenTree* destAddr, GenTree* value, unsigned curLevel);

    GenTree* impGetNodeAddr(GenTree* val, unsigned curLevel, GenTreeFlags* pDerefFlags);

    var_types impNormStructType(CORINFO_CLASS_HANDLE structHnd, CorInfoType* simdBaseJitType = nullptr);

    GenTree* impNormStructVal(GenTree* structVal, unsigned curLevel);

    GenTree* impTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                              bool*                   pRuntimeLookup    = nullptr,
                              bool                    mustRestoreHandle = false,
                              bool                    importParent      = false);

    GenTree* impParentClassTokenToHandle(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                         bool*                   pRuntimeLookup    = nullptr,
                                         bool                    mustRestoreHandle = false)
    {
        return impTokenToHandle(pResolvedToken, pRuntimeLookup, mustRestoreHandle, true);
    }

    GenTree* impLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                             CORINFO_LOOKUP*         pLookup,
                             GenTreeFlags            flags,
                             void*                   compileTimeHandle);

    GenTree* getRuntimeContextTree(CORINFO_RUNTIME_LOOKUP_KIND kind);

    GenTree* impRuntimeLookupToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_LOOKUP*         pLookup,
                                    void*                   compileTimeHandle);

    GenTree* impReadyToRunLookupToTree(CORINFO_CONST_LOOKUP* pLookup, GenTreeFlags flags, void* compileTimeHandle);

    GenTreeCall* impReadyToRunHelperToTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                           CorInfoHelpFunc         helper,
                                           var_types               type,
                                           CORINFO_LOOKUP_KIND*    pGenericLookupKind = nullptr,
                                           GenTree*                arg1               = nullptr);

    bool impIsCastHelperEligibleForClassProbe(GenTree* tree);
    bool impIsCastHelperMayHaveProfileData(CorInfoHelpFunc helper);

    GenTree* impCastClassOrIsInstToTree(
        GenTree* op1, GenTree* op2, CORINFO_RESOLVED_TOKEN* pResolvedToken, bool isCastClass, IL_OFFSET ilOffset);

    GenTree* impOptimizeCastClassOrIsInst(GenTree* op1, CORINFO_RESOLVED_TOKEN* pResolvedToken, bool isCastClass);

    bool VarTypeIsMultiByteAndCanEnreg(var_types                type,
                                       CORINFO_CLASS_HANDLE     typeClass,
                                       unsigned*                typeSize,
                                       bool                     forReturn,
                                       bool                     isVarArg,
                                       CorInfoCallConvExtension callConv);

    bool IsIntrinsicImplementedByUserCall(NamedIntrinsic intrinsicName);
    bool IsTargetIntrinsic(NamedIntrinsic intrinsicName);
    bool IsMathIntrinsic(NamedIntrinsic intrinsicName);
    bool IsMathIntrinsic(GenTree* tree);

private:
    //----------------- Importing the method ----------------------------------

    CORINFO_CONTEXT_HANDLE impTokenLookupContextHandle; // The context used for looking up tokens.

#ifdef DEBUG
    unsigned    impCurOpcOffs;
    const char* impCurOpcName;
    bool        impNestedStackSpill;

    // For displaying instrs with generated native code (-n:B)
    Statement* impLastILoffsStmt; // oldest stmt added for which we did not call SetLastILOffset().
    void       impNoteLastILoffs();
#endif

    // Debug info of current statement being imported. It gets set to contain
    // no IL location (!impCurStmtDI.GetLocation().IsValid) after it has been
    // set in the appended trees. Then it gets updated at IL instructions for
    // which we have to report mapping info.
    // It will always contain the current inline context.
    DebugInfo impCurStmtDI;

    DebugInfo impCreateDIWithCurrentStackInfo(IL_OFFSET offs, bool isCall);
    void impCurStmtOffsSet(IL_OFFSET offs);

    void impNoteBranchOffs();

    unsigned impInitBlockLineInfo();

    bool impIsThis(GenTree* obj);

    void impPopCallArgs(CORINFO_SIG_INFO* sig, GenTreeCall* call);

public:
    static bool impCheckImplicitArgumentCoercion(var_types sigType, var_types nodeType);

private:
    void impPopReverseCallArgs(CORINFO_SIG_INFO* sig, GenTreeCall* call, unsigned skipReverseCount);

    //---------------- Spilling the importer stack ----------------------------

    // The maximum number of bytes of IL processed without clean stack state.
    // It allows to limit the maximum tree size and depth.
    static const unsigned MAX_TREE_SIZE = 200;
    bool impCanSpillNow(OPCODE prevOpcode);

    struct PendingDsc
    {
        PendingDsc* pdNext;
        BasicBlock* pdBB;
        SavedStack  pdSavedStack;
    };

    PendingDsc* impPendingList; // list of BBs currently waiting to be imported.
    PendingDsc* impPendingFree; // Freed up dscs that can be reused

    // We keep a byte-per-block map (dynamically extended) in the top-level Compiler object of a compilation.
    JitExpandArray<BYTE> impPendingBlockMembers;

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
    void impSpillSideEffect(bool spillGlobEffects, unsigned chkLevel DEBUGARG(const char* reason));
    void impSpillSideEffects(bool spillGlobEffects, unsigned chkLevel DEBUGARG(const char* reason));
    void impSpillLclRefs(unsigned lclNum, unsigned chkLevel);

    BasicBlock* impPushCatchArgOnStack(BasicBlock* hndBlk, CORINFO_CLASS_HANDLE clsHnd, bool isSingleBlockFilter);

    bool impBlockIsInALoop(BasicBlock* block);
    void impImportBlockCode(BasicBlock* block);

    void impReimportMarkBlock(BasicBlock* block);

    void impVerifyEHBlock(BasicBlock* block);

    void impImportBlockPending(BasicBlock* block);

    // Similar to impImportBlockPending, but assumes that block has already been imported once and is being
    // reimported for some reason.  It specifically does *not* look at verCurrentState to set the EntryState
    // for the block, but instead, just re-uses the block's existing EntryState.
    void impReimportBlockPending(BasicBlock* block);

    var_types impGetByRefResultType(genTreeOps oper, bool fUnsigned, GenTree** pOp1, GenTree** pOp2);

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
    JitExpandArray<BYTE> impSpillCliquePredMembers;
    JitExpandArray<BYTE> impSpillCliqueSuccMembers;

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

    GenTreeLclVar* impCreateLocalNode(unsigned lclNum DEBUGARG(IL_OFFSET offset));
    void impLoadVar(unsigned lclNum, IL_OFFSET offset);
    void impLoadArg(unsigned ilArgNum, IL_OFFSET offset);
    void impLoadLoc(unsigned ilLclNum, IL_OFFSET offset);
    bool impReturnInstruction(int prefixFlags, OPCODE& opcode);
    void impPoisonImplicitByrefsBeforeReturn();

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

    void FreeBlockListNode(BlockListNode* node);

    var_types mangleVarArgsType(var_types type);

    regNumber getCallArgIntRegister(regNumber floatReg);
    regNumber getCallArgFloatRegister(regNumber intReg);

    static unsigned jitTotalMethodCompiled;

#ifdef DEBUG
    static LONG jitNestingLevel;
#endif // DEBUG

    static bool impIsInvariant(const GenTree* tree);
    static bool impIsAddressInLocal(const GenTree* tree, GenTree** lclVarTreeOut = nullptr);

    void impMakeDiscretionaryInlineObservations(InlineInfo* pInlineInfo, InlineResult* inlineResult);

    // STATIC inlining decision based on the IL code.
    void impCanInlineIL(CORINFO_METHOD_HANDLE fncHandle,
                        CORINFO_METHOD_INFO*  methInfo,
                        bool                  forceInline,
                        InlineResult*         inlineResult);

    void impCheckCanInline(GenTreeCall*           call,
                           uint8_t                candidateIndex,
                           CORINFO_METHOD_HANDLE  fncHandle,
                           unsigned               methAttr,
                           CORINFO_CONTEXT_HANDLE exactContextHnd,
                           InlineCandidateInfo**  ppInlineCandidateInfo,
                           InlineResult*          inlineResult);

    void impInlineRecordArgInfo(InlineInfo* pInlineInfo, CallArg* arg, unsigned argNum, InlineResult* inlineResult);

    void impInlineInitVars(InlineInfo* pInlineInfo);

    unsigned impInlineFetchLocal(unsigned lclNum DEBUGARG(const char* reason));

    GenTree* impInlineFetchArg(unsigned lclNum, InlArgInfo* inlArgInfo, InlLclVarInfo* lclTypeInfo);

    bool impInlineIsThis(GenTree* tree, InlArgInfo* inlArgInfo);

    bool impInlineIsGuaranteedThisDerefBeforeAnySideEffects(GenTree*    additionalTree,
                                                            CallArgs*   additionalCallArgs,
                                                            GenTree*    dereferencedAddress,
                                                            InlArgInfo* inlArgInfo);

    void impMarkInlineCandidate(GenTree*               call,
                                CORINFO_CONTEXT_HANDLE exactContextHnd,
                                bool                   exactContextNeedsRuntimeLookup,
                                CORINFO_CALL_INFO*     callInfo,
                                IL_OFFSET              ilOffset);

    void impMarkInlineCandidateHelper(GenTreeCall*           call,
                                      uint8_t                candidateIndex,
                                      CORINFO_CONTEXT_HANDLE exactContextHnd,
                                      bool                   exactContextNeedsRuntimeLookup,
                                      CORINFO_CALL_INFO*     callInfo,
                                      IL_OFFSET              ilOffset,
                                      InlineResult*          inlineResult);

    bool impTailCallRetTypeCompatible(bool                     allowWidening,
                                      var_types                callerRetType,
                                      CORINFO_CLASS_HANDLE     callerRetTypeClass,
                                      CorInfoCallConvExtension callerCallConv,
                                      var_types                calleeRetType,
                                      CORINFO_CLASS_HANDLE     calleeRetTypeClass,
                                      CorInfoCallConvExtension calleeCallConv);

    bool impIsTailCallILPattern(
        bool tailPrefixed, OPCODE curOpcode, const BYTE* codeAddrOfNextOpcode, const BYTE* codeEnd, bool isRecursive);

    bool impIsImplicitTailCallCandidate(
        OPCODE curOpcode, const BYTE* codeAddrOfNextOpcode, const BYTE* codeEnd, int prefixFlags, bool isRecursive);

    bool impCanSkipCovariantStoreCheck(GenTree* value, GenTree* array);

    methodPointerInfo* impAllocateMethodPointerInfo(const CORINFO_RESOLVED_TOKEN& token, mdToken tokenConstrained);

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
    BasicBlock* fgEntryBB;        // For OSR, the original method's entry point
    BasicBlock* fgOSREntryBB;     // For OSR, the logical entry point (~ patchpoint)
#if defined(FEATURE_EH_FUNCLETS)
    BasicBlock* fgFirstFuncletBB; // First block of outlined funclets (to allow block insertion before the funclets)
#endif
    BasicBlock* fgFirstBBScratch;   // Block inserted for initialization stuff. Is nullptr if no such block has been
                                    // created.
    BasicBlockList* fgReturnBlocks; // list of BBJ_RETURN blocks
    unsigned        fgEdgeCount;    // # of control flow edges between the BBs
    unsigned        fgBBcount;      // # of BBs in the method (in the linked list that starts with fgFirstBB)
#ifdef DEBUG
    unsigned                     fgBBcountAtCodegen; // # of BBs in the method at the start of codegen
    jitstd::vector<BasicBlock*>* fgBBOrder;          // ordered vector of BBs
#endif
    // Used as a quick check for whether phases downstream of loop finding should look for natural loops.
    // If true: there may or may not be any natural loops in the flow graph, so try to find them
    // If false: there's definitely not any natural loops in the flow graph
    bool         fgMightHaveNaturalLoops;

    unsigned     fgBBNumMax;           // The max bbNum that has been assigned to basic blocks
    unsigned     fgDomBBcount;         // # of BBs for which we have dominator and reachability information
    BasicBlock** fgBBReversePostorder; // Blocks in reverse postorder

    FlowGraphDfsTree* m_dfsTree;
    // The next members are annotations on the flow graph used during the
    // optimization phases. They are invalidated once RBO runs and modifies the
    // flow graph.
    FlowGraphNaturalLoops* m_loops;
    LoopSideEffects* m_loopSideEffects;
    BlockToNaturalLoopMap* m_blockToLoop;
    // Dominator tree used by SSA construction and copy propagation (the two are expected to use the same tree
    // in order to avoid the need for SSA reconstruction and an "out of SSA" phase).
    FlowGraphDominatorTree* m_domTree;
    BlockReachabilitySets* m_reachabilitySets;

    // Do we require loops to be in canonical form? The canonical form ensures that:
    // 1. All loops have preheaders (single entry blocks that always enter the loop)
    // 2. All loop exits where bbIsHandlerBeg(exit) is false have only loop predecessors.
    //
    bool optLoopsCanonical;
    unsigned optNumNaturalLoopsFound; // Number of natural loops found in the loop finding phase

    bool fgBBVarSetsInited;

    // Track how many artificial ref counts we've added to fgEntryBB (for OSR)
    unsigned fgEntryBBExtraRefs;

    // Allocate array like T* a = new T[fgBBNumMax + 1];
    // Using helper so we don't keep forgetting +1.
    template <typename T>
    T* fgAllocateTypeForEachBlk(CompMemKind cmk = CMK_Unknown)
    {
        return getAllocator(cmk).allocate<T>(fgBBNumMax + 1);
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
            roundUp(fgCurBBEpochSize, (unsigned)(sizeof(size_t) * 8)) / unsigned(sizeof(size_t) * 8);

#ifdef DEBUG
        if (verbose)
        {
            unsigned epochArrSize = BasicBlockBitSetTraits::GetArrSize(this);
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

    bool fgEnsureFirstBBisScratch();
    bool fgFirstBBisScratch();
    bool fgBBisScratch(BasicBlock* block);

    void fgExtendEHRegionBefore(BasicBlock* block);
    void fgExtendEHRegionAfter(BasicBlock* block);

    BasicBlock* fgNewBBbefore(BBKinds jumpKind, BasicBlock* block, bool extendRegion, BasicBlock* jumpDest = nullptr);

    BasicBlock* fgNewBBafter(BBKinds jumpKind, BasicBlock* block, bool extendRegion, BasicBlock* jumpDest = nullptr);

    BasicBlock* fgNewBBFromTreeAfter(BBKinds jumpKind, BasicBlock* block, GenTree* tree, DebugInfo& debugInfo, BasicBlock* jumpDest = nullptr, bool updateSideEffects = false);

    BasicBlock* fgNewBBinRegion(BBKinds jumpKind,
                                unsigned    tryIndex,
                                unsigned    hndIndex,
                                BasicBlock* nearBlk,
                                BasicBlock* jumpDest    = nullptr,
                                bool        putInFilter = false,
                                bool        runRarely   = false,
                                bool        insertAtEnd = false);

    BasicBlock* fgNewBBinRegion(BBKinds jumpKind,
                                BasicBlock* srcBlk,
                                BasicBlock* jumpDest    = nullptr,
                                bool        runRarely   = false,
                                bool        insertAtEnd = false);

    BasicBlock* fgNewBBinRegion(BBKinds jumpKind, BasicBlock* jumpDest = nullptr);

    BasicBlock* fgNewBBinRegionWorker(BBKinds jumpKind,
                                      BasicBlock* afterBlk,
                                      unsigned    xcptnIndex,
                                      bool        putInTryRegion,
                                      BasicBlock* jumpDest = nullptr);

    void fgInsertBBbefore(BasicBlock* insertBeforeBlk, BasicBlock* newBlk);
    void fgInsertBBafter(BasicBlock* insertAfterBlk, BasicBlock* newBlk);
    void fgUnlinkBlock(BasicBlock* block);
    void fgUnlinkBlockForRemoval(BasicBlock* block);

#ifdef FEATURE_JIT_METHOD_PERF
    unsigned fgMeasureIR();
#endif // FEATURE_JIT_METHOD_PERF

    bool fgModified;             // True if the flow graph has been modified recently
    bool fgPredsComputed;        // Have we computed the bbPreds list
    bool fgReturnBlocksComputed; // Have we computed the return blocks list?
    bool fgOptimizedFinally;     // Did we optimize any try-finallys?
    bool fgCanonicalizedFirstBB; // TODO-Quirk: did we end up canonicalizing first BB?

    bool fgHasSwitch; // any BBJ_SWITCH jumps?

    bool fgRemoveRestOfBlock; // true if we know that we will throw
    bool fgStmtRemoved;       // true if we remove statements -> need new DFA

    enum FlowGraphOrder
    {
        FGOrderTree,
        FGOrderLinear
    };
    // There are two modes for ordering of the trees.
    //  - In FGOrderTree, the dominant ordering is the tree order, and the nodes contained in
    //    each tree and sub-tree are contiguous, and can be traversed (in gtNext/gtPrev order)
    //    by traversing the tree according to the order of the operands.
    //  - In FGOrderLinear, the dominant ordering is the linear order.
    FlowGraphOrder fgOrder;

    // The following are flags that keep track of the state of internal data structures

    // Even in tree form (fgOrder == FGOrderTree) the trees are threaded in a
    // doubly linked lists during certain phases of the compilation.
    // - Local morph threads all locals to be used for early liveness and
    //   forward sub when optimizing. This is kept valid until after forward sub.
    //   The first local is kept in Statement::GetTreeList() and the last
    //   local in Statement::GetTreeListEnd(). fgSequenceLocals can be used
    //   to (re-)sequence a statement into this form, and
    //   Statement::LocalsTreeList for range-based iteration. The order must
    //   match tree order.
    //
    // - fgSetBlockOrder threads all nodes. This is kept valid until LIR form.
    //   In this form the first node is given by Statement::GetTreeList and the
    //   last node is given by Statement::GetRootNode(). fgSetStmtSeq can be used
    //   to (re-)sequence a statement into this form, and Statement::TreeList for
    //   range-based iteration. The order must match tree order.
    //
    // - Rationalization links all nodes into linear form which is kept until
    //   the end of compilation. The first and last nodes are stored in the block.
    NodeThreading fgNodeThreading;
    bool          fgCanRelocateEHRegions;   // true if we are allowed to relocate the EH regions
    bool          fgEdgeWeightsComputed;    // true after we have called fgComputeEdgeWeights
    bool          fgHaveValidEdgeWeights;   // true if we were successful in computing all of the edge weights
    bool          fgSlopUsedInEdgeWeights;  // true if their was some slop used when computing the edge weights
    bool          fgRangeUsedInEdgeWeights; // true if some of the edgeWeight are expressed in Min..Max form
    weight_t      fgCalledCount;            // count of the number of times this method was called
                                            // This is derived from the profile data
                                            // or is BB_UNITY_WEIGHT when we don't have profile data

#if defined(FEATURE_EH_FUNCLETS)
    bool fgFuncletsCreated; // true if the funclet creation phase has been run
#endif                      // FEATURE_EH_FUNCLETS

    bool fgGlobalMorph; // indicates if we are during the global morphing phase
                        // since fgMorphTree can be called from several places

    bool fgGlobalMorphDone;

    bool     impBoxTempInUse; // the temp below is valid and available
    unsigned impBoxTemp;      // a temporary that is used for boxing

#ifdef DEBUG
    bool jitFallbackCompile; // Are we doing a fallback compile? That is, have we executed a NO_WAY assert,
                             //   and we are trying to compile again in a "safer", minopts mode?
#endif

#if defined(DEBUG)
    unsigned impInlinedCodeSize;
    bool     fgPrintInlinedMethods;
#endif

    jitstd::vector<FlowEdge*>* fgPredListSortVector;

    //-------------------------------------------------------------------------

    void fgInit();

    PhaseStatus fgImport();

    PhaseStatus fgTransformIndirectCalls();

    PhaseStatus fgTransformPatchpoints();

    PhaseStatus fgMorphInit();

    PhaseStatus fgInline();

    PhaseStatus fgRemoveEmptyTry();

    PhaseStatus fgRemoveEmptyFinally();

    PhaseStatus fgMergeFinallyChains();

    PhaseStatus fgCloneFinally();

    void fgCleanupContinuation(BasicBlock* continuation);

    PhaseStatus fgTailMergeThrows();
    void fgTailMergeThrowsFallThroughHelper(BasicBlock* predBlock,
                                            BasicBlock* nonCanonicalBlock,
                                            BasicBlock* canonicalBlock,
                                            FlowEdge*   predEdge);
    void fgTailMergeThrowsJumpToHelper(BasicBlock* predBlock,
                                       BasicBlock* nonCanonicalBlock,
                                       BasicBlock* canonicalBlock,
                                       FlowEdge*   predEdge);

    bool fgRetargetBranchesToCanonicalCallFinally(BasicBlock*      block,
                                                  BasicBlock*      handler,
                                                  BlockToBlockMap& continuationMap);

    GenTree* fgGetCritSectOfStaticMethod();

#if defined(FEATURE_EH_FUNCLETS)

    void fgAddSyncMethodEnterExit();

    GenTree* fgCreateMonitorTree(unsigned lvaMonitorBool, unsigned lvaThisVar, BasicBlock* block, bool enter);

    void fgConvertSyncReturnToLeave(BasicBlock* block);

#endif // FEATURE_EH_FUNCLETS

    void fgAddReversePInvokeEnterExit();

    bool fgMoreThanOneReturnBlock();

    // The number of separate return points in the method.
    unsigned fgReturnCount;

    PhaseStatus fgAddInternal();

    enum class FoldResult
    {
        FOLD_DID_NOTHING,
        FOLD_CHANGED_CONTROL_FLOW,
        FOLD_REMOVED_LAST_STMT,
        FOLD_ALTERED_LAST_STMT,
    };

    FoldResult fgFoldConditional(BasicBlock* block);

    PhaseStatus fgMorphBlocks();
    void fgMorphBlock(BasicBlock* block);
    void fgMorphStmts(BasicBlock* block);

    void fgMergeBlockReturn(BasicBlock* block);

    bool fgMorphBlockStmt(BasicBlock* block, Statement* stmt DEBUGARG(const char* msg));
    void fgMorphStmtBlockOps(BasicBlock* block, Statement* stmt);

    //------------------------------------------------------------------------------------------------------------
    // MorphMDArrayTempCache: a simple cache of compiler temporaries in the local variable table, used to minimize
    // the number of locals allocated when doing early multi-dimensional array operation expansion. Two types of
    // temps are created and cached (due to the two types of temps needed by the MD array expansion): TYP_INT and
    // TYP_REF. `GrabTemp` either returns an available temp from the cache or allocates a new temp and returns it
    // after adding it to the cache. `Reset` makes all the temps in the cache available for subsequent re-use.
    //
    class MorphMDArrayTempCache
    {
    private:
        class TempList
        {
        public:
            TempList(Compiler* compiler)
                : m_compiler(compiler), m_first(nullptr), m_insertPtr(&m_first), m_nextAvail(nullptr)
            {
            }

            unsigned GetTemp();

            void Reset()
            {
                m_nextAvail = m_first;
            }

        private:
            struct Node
            {
                Node(unsigned tmp) : next(nullptr), tmp(tmp)
                {
                }

                Node*    next;
                unsigned tmp;
            };

            Compiler* m_compiler;
            Node*     m_first;
            Node**    m_insertPtr;
            Node*     m_nextAvail;
        };

        TempList intTemps; // Temps for genActualType() == TYP_INT
        TempList refTemps; // Temps for TYP_REF

    public:
        MorphMDArrayTempCache(Compiler* compiler) : intTemps(compiler), refTemps(compiler)
        {
        }

        unsigned GrabTemp(var_types type);

        void Reset()
        {
            intTemps.Reset();
            refTemps.Reset();
        }
    };

    bool fgMorphArrayOpsStmt(MorphMDArrayTempCache* pTempCache, BasicBlock* block, Statement* stmt);
    PhaseStatus fgMorphArrayOps();

    void fgSetOptions();

#ifdef DEBUG
    void fgPreExpandQmarkChecks(GenTree* expr);
    void fgPostExpandQmarkChecks();
#endif

    IL_OFFSET fgFindBlockILOffset(BasicBlock* block);
    void fgFixEntryFlowForOSR();

    BasicBlock* fgSplitBlockAtBeginning(BasicBlock* curr);
    BasicBlock* fgSplitBlockAtEnd(BasicBlock* curr);
    BasicBlock* fgSplitBlockAfterStatement(BasicBlock* curr, Statement* stmt);
    BasicBlock* fgSplitBlockAfterNode(BasicBlock* curr, GenTree* node); // for LIR
    BasicBlock* fgSplitEdge(BasicBlock* curr, BasicBlock* succ);
    BasicBlock* fgSplitBlockBeforeTree(BasicBlock* block, Statement* stmt, GenTree* splitPoint, Statement** firstNewStmt, GenTree*** splitNodeUse);

    Statement* fgNewStmtFromTree(GenTree* tree, BasicBlock* block, const DebugInfo& di);
    Statement* fgNewStmtFromTree(GenTree* tree);
    Statement* fgNewStmtFromTree(GenTree* tree, BasicBlock* block);
    Statement* fgNewStmtFromTree(GenTree* tree, const DebugInfo& di);

    GenTreeQmark* fgGetTopLevelQmark(GenTree* expr, GenTree** ppDst = nullptr);
    bool fgExpandQmarkForCastInstOf(BasicBlock* block, Statement* stmt);
    bool fgExpandQmarkStmt(BasicBlock* block, Statement* stmt);
    void fgExpandQmarkNodes();

    bool fgSimpleLowerCastOfSmpOp(LIR::Range& range, GenTreeCast* cast);

#if FEATURE_LOOP_ALIGN
    bool shouldAlignLoop(FlowGraphNaturalLoop* loop, BasicBlock* top);
    PhaseStatus placeLoopAlignInstructions();
#endif

    // This field keep the R2R helper call that would be inserted to trigger the constructor
    // of the static class. It is set as nongc or gc static base if they are imported, so
    // CSE can eliminate the repeated call, or the chepeast helper function that triggers it.
    CorInfoHelpFunc m_preferredInitCctor;
    void            fgSetPreferredInitCctor();

    GenTree* fgInitThisClass();

    GenTreeCall* fgGetStaticsCCtorHelper(CORINFO_CLASS_HANDLE cls, CorInfoHelpFunc helper, uint32_t typeIndex = 0);

    GenTreeCall* fgGetSharedCCtor(CORINFO_CLASS_HANDLE cls);

    bool backendRequiresLocalVarLifetimes()
    {
        return !opts.MinOpts() || m_pLinearScan->willEnregisterLocalVars();
    }

    void fgLocalVarLiveness();

    void fgLocalVarLivenessInit();

    void fgPerNodeLocalVarLiveness(GenTree* node);
    void fgPerBlockLocalVarLiveness();

#if defined(FEATURE_HW_INTRINSICS)
    void fgPerNodeLocalVarLiveness(GenTreeHWIntrinsic* hwintrinsic);
#endif // FEATURE_HW_INTRINSICS

    void fgAddHandlerLiveVars(BasicBlock* block, VARSET_TP& ehHandlerLiveVars, MemoryKindSet& memoryLiveness);

    void fgLiveVarAnalysis(bool updateInternalOnly = false);

    void fgComputeLifeCall(VARSET_TP& life, GenTreeCall* call);

    void fgComputeLifeTrackedLocalUse(VARSET_TP& life, LclVarDsc& varDsc, GenTreeLclVarCommon* node);
    bool fgComputeLifeTrackedLocalDef(VARSET_TP&           life,
                                      VARSET_VALARG_TP     keepAliveVars,
                                      LclVarDsc&           varDsc,
                                      GenTreeLclVarCommon* node);
    bool fgComputeLifeUntrackedLocal(VARSET_TP&           life,
                                     VARSET_VALARG_TP     keepAliveVars,
                                     LclVarDsc&           varDsc,
                                     GenTreeLclVarCommon* lclVarNode);
    bool fgComputeLifeLocal(VARSET_TP& life, VARSET_VALARG_TP keepAliveVars, GenTree* lclVarNode);

    GenTree* fgTryRemoveDeadStoreEarly(Statement* stmt, GenTreeLclVarCommon* dst);

    void fgComputeLife(VARSET_TP&       life,
                       GenTree*         startNode,
                       GenTree*         endNode,
                       VARSET_VALARG_TP volatileVars,
                       bool* pStmtInfoDirty DEBUGARG(bool* treeModf));

    void fgComputeLifeLIR(VARSET_TP& life, BasicBlock* block, VARSET_VALARG_TP volatileVars);

    bool fgTryRemoveNonLocal(GenTree* node, LIR::Range* blockRange);

    bool fgTryRemoveDeadStoreLIR(GenTree* store, GenTreeLclVarCommon* lclNode, BasicBlock* block);

    bool fgRemoveDeadStore(GenTree**        pTree,
                           LclVarDsc*       varDsc,
                           VARSET_VALARG_TP life,
                           bool*            doAgain,
                           bool*            pStmtInfoDirty,
                           bool* pStoreRemoved DEBUGARG(bool* treeModf));

    void fgInterBlockLocalVarLiveness();

    // Blocks: convenience methods for enabling range-based `for` iteration over the function's blocks, e.g.:
    // 1.   for (BasicBlock* const block : compiler->Blocks()) ...
    // 2.   for (BasicBlock* const block : compiler->Blocks(startBlock)) ...
    // 3.   for (BasicBlock* const block : compiler->Blocks(startBlock, endBlock)) ...
    // In case (1), the block list can be empty. In case (2), `startBlock` can be nullptr. In case (3),
    // both `startBlock` and `endBlock` must be non-null.
    //
    BasicBlockSimpleList Blocks() const
    {
        return BasicBlockSimpleList(fgFirstBB);
    }

    BasicBlockSimpleList Blocks(BasicBlock* startBlock) const
    {
        return BasicBlockSimpleList(startBlock);
    }

    BasicBlockRangeList Blocks(BasicBlock* startBlock, BasicBlock* endBlock) const
    {
        return BasicBlockRangeList(startBlock, endBlock);
    }

    // This array, managed by the SSA numbering infrastructure, keeps "outlined composite SSA numbers".
    // See "SsaNumInfo::GetNum" for more details on when this is needed.
    JitExpandArrayStack<unsigned>* m_outlinedCompositeSsaNums;

    // This map tracks nodes whose value numbers explicitly or implicitly depend on memory states.
    // The map provides the entry block of the most closely enclosing loop that
    // defines the memory region accessed when defining the nodes's VN.
    //
    // This information should be consulted when considering hoisting node out of a loop, as the VN
    // for the node will only be valid within the indicated loop.
    //
    // It is not fine-grained enough to track memory dependence within loops, so cannot be used
    // for more general code motion.
    //
    // If a node does not have an entry in the map we currently assume the VN is not memory dependent
    // and so memory does not constrain hoisting.
    //
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, BasicBlock*> NodeToLoopMemoryBlockMap;
    NodeToLoopMemoryBlockMap* m_nodeToLoopMemoryBlockMap;
    NodeToLoopMemoryBlockMap* GetNodeToLoopMemoryBlockMap()
    {
        if (m_nodeToLoopMemoryBlockMap == nullptr)
        {
            m_nodeToLoopMemoryBlockMap = new (getAllocator()) NodeToLoopMemoryBlockMap(getAllocator());
        }
        return m_nodeToLoopMemoryBlockMap;
    }

    typedef JitHashTable<void*, JitPtrKeyFuncs<void>, CORINFO_RUNTIME_LOOKUP> SignatureToLookupInfoMap;
    SignatureToLookupInfoMap* m_signatureToLookupInfoMap;
    SignatureToLookupInfoMap* GetSignatureToLookupInfoMap()
    {
        if (m_signatureToLookupInfoMap == nullptr)
        {
            m_signatureToLookupInfoMap = new (getAllocator()) SignatureToLookupInfoMap(getAllocator());
        }
        return m_signatureToLookupInfoMap;
    }

    void optRecordLoopMemoryDependence(GenTree* tree, BasicBlock* block, ValueNum memoryVN);
    void optCopyLoopMemoryDependence(GenTree* fromTree, GenTree* toTree);

    inline bool PreciseRefCountsRequired();

    // Performs SSA conversion.
    PhaseStatus fgSsaBuild();

    // Reset any data structures to the state expected by "fgSsaBuild", so it can be run again.
    void fgResetForSsa();

    unsigned fgSsaPassesCompleted; // Number of times fgSsaBuild has been run.
    bool     fgSsaValid;           // True if SSA info is valid and can be cross-checked versus IR

#ifdef DEBUG
    void DumpSsaSummary();
#endif

    // Returns "true" if this is a special variable that is never zero initialized in the prolog.
    inline bool fgVarIsNeverZeroInitializedInProlog(unsigned varNum);

    // Returns "true" if the variable needs explicit zero initialization.
    inline bool fgVarNeedsExplicitZeroInit(unsigned varNum, bool bbInALoop, bool bbIsReturn);

    // The value numbers for this compilation.
    ValueNumStore* vnStore;
    class ValueNumberState* vnState;

public:
    ValueNumStore* GetValueNumStore()
    {
        return vnStore;
    }

    // Do value numbering (assign a value number to each
    // tree node).
    PhaseStatus fgValueNumber();

    void fgValueNumberLocalStore(GenTree*             storeNode,
                                 GenTreeLclVarCommon* lclDefNode,
                                 ssize_t              offset,
                                 unsigned             storeSize,
                                 ValueNumPair         value,
                                 bool                 normalize = true);

    void fgValueNumberArrayElemLoad(GenTree* loadTree, VNFuncApp* addrFunc);

    void fgValueNumberArrayElemStore(GenTree* storeNode, VNFuncApp* addrFunc, unsigned storeSize, ValueNum value);

    void fgValueNumberFieldLoad(GenTree* loadTree, GenTree* baseAddr, FieldSeq* fieldSeq, ssize_t offset);

    void fgValueNumberFieldStore(
        GenTree* storeNode, GenTree* baseAddr, FieldSeq* fieldSeq, ssize_t offset, unsigned storeSize, ValueNum value);

    bool fgValueNumberConstLoad(GenTreeIndir* tree);

    // Compute the value number for a byref-exposed load of the given type via the given pointerVN.
    ValueNum fgValueNumberByrefExposedLoad(var_types type, ValueNum pointerVN);

    unsigned fgVNPassesCompleted; // Number of times fgValueNumber has been run.

    // Utility functions for fgValueNumber.

    // Perform value-numbering for the trees in "blk".
    void fgValueNumberBlock(BasicBlock* blk);

    // Requires that "entryBlock" is the header block of "loop" and that "loop" is the
    // innermost loop of which "entryBlock" is the entry.  Returns the value number that should be
    // assumed for the memoryKind at the start "entryBlk".
    ValueNum fgMemoryVNForLoopSideEffects(MemoryKind memoryKind, BasicBlock* entryBlock, FlowGraphNaturalLoop* loop);

    // Called when an operation (performed by "tree", described by "msg") may cause the GcHeap to be mutated.
    // As GcHeap is a subset of ByrefExposed, this will also annotate the ByrefExposed mutation.
    void fgMutateGcHeap(GenTree* tree DEBUGARG(const char* msg));

    // Called when an operation (performed by "tree", described by "msg") may cause an address-exposed local to be
    // mutated.
    void fgMutateAddressExposedLocal(GenTree* tree DEBUGARG(const char* msg));

    // For a GC heap store at curTree, record the new curMemoryVN's and update curTree's MemorySsaMap.
    // As GcHeap is a subset of ByrefExposed, this will also record the ByrefExposed store.
    void recordGcHeapStore(GenTree* curTree, ValueNum gcHeapVN DEBUGARG(const char* msg));

    // For a store to an address-exposed local at curTree, record the new curMemoryVN and update curTree's MemorySsaMap.
    void recordAddressExposedLocalStore(GenTree* curTree, ValueNum memoryVN DEBUGARG(const char* msg));

    void fgSetCurrentMemoryVN(MemoryKind memoryKind, ValueNum newMemoryVN);

    // Tree caused an update in the current memory VN.  If "tree" has an associated heap SSA #, record that
    // value in that SSA #.
    void fgValueNumberRecordMemorySsa(MemoryKind memoryKind, GenTree* tree);

    // The input 'tree' is a leaf node that is a constant
    // Assign the proper value number to the tree
    void fgValueNumberTreeConst(GenTree* tree);

    // If the constant has a field sequence associated with it, then register
    void fgValueNumberRegisterConstFieldSeq(GenTreeIntCon* tree);

    // If the VN store has been initialized, reassign the
    // proper value number to the constant tree.
    void fgUpdateConstTreeValueNumber(GenTree* tree);

    // Assumes that all inputs to "tree" have had value numbers assigned; assigns a VN to tree.
    // (With some exceptions: the VN of the lhs of an assignment is assigned as part of the
    // assignment.)
    void fgValueNumberTree(GenTree* tree);

    void fgValueNumberStore(GenTree* tree);

    void fgValueNumberSsaVarDef(GenTreeLclVarCommon* lcl);

    // Does value-numbering for a cast tree.
    void fgValueNumberCastTree(GenTree* tree);

    // Does value-numbering for a bitcast tree.
    void fgValueNumberBitCast(GenTree* tree);

    // Does value-numbering for an intrinsic tree.
    void fgValueNumberIntrinsic(GenTree* tree);

    void fgValueNumberArrIndexAddr(GenTreeArrAddr* arrAddr);

#ifdef FEATURE_HW_INTRINSICS
    // Does value-numbering for a GT_HWINTRINSIC tree
    void fgValueNumberHWIntrinsic(GenTreeHWIntrinsic* tree);
#endif // FEATURE_HW_INTRINSICS

    // Does value-numbering for a call.  We interpret some helper calls.
    void fgValueNumberCall(GenTreeCall* call);

    // Does value-numbering for a special intrinsic call.
    bool fgValueNumberSpecialIntrinsic(GenTreeCall* call);

    // Does value-numbering for a helper representing a cast operation.
    void fgValueNumberCastHelper(GenTreeCall* call);

    // Does value-numbering for a helper "call" that has a VN function symbol "vnf".
    void fgValueNumberHelperCallFunc(GenTreeCall* call, VNFunc vnf, ValueNumPair vnpExc);

    // Requires "helpCall" to be a helper call.  Assigns it a value number;
    // we understand the semantics of some of the calls.  Returns "true" if
    // the call may modify the heap (we assume arbitrary memory side effects if so).
    bool fgValueNumberHelperCall(GenTreeCall* helpCall);

    // Requires that "helpFunc" is one of the pure Jit Helper methods.
    // Returns the corresponding VNFunc to use for value numbering
    VNFunc fgValueNumberJitHelperMethodVNFunc(CorInfoHelpFunc helpFunc);

    // Adds the exception set for the current tree node which has a memory indirection operation
    void fgValueNumberAddExceptionSetForIndirection(GenTree* tree, GenTree* baseAddr);

    // Adds the exception sets for the current tree node which is performing a division or modulus operation
    void fgValueNumberAddExceptionSetForDivision(GenTree* tree);

    // Adds the exception set for the current tree node which is performing a overflow checking operation
    void fgValueNumberAddExceptionSetForOverflow(GenTree* tree);

    // Adds the exception set for the current tree node which is performing a bounds check operation
    void fgValueNumberAddExceptionSetForBoundsCheck(GenTree* tree);

    // Adds the exception set for the current tree node which is performing a ckfinite operation
    void fgValueNumberAddExceptionSetForCkFinite(GenTree* tree);

    // Adds the exception sets for the current tree node
    void fgValueNumberAddExceptionSet(GenTree* tree);

#ifdef DEBUG
    void fgDebugCheckExceptionSets();
#endif

    // These are the current value number for the memory implicit variables while
    // doing value numbering.  These are the value numbers under the "liberal" interpretation
    // of memory values; the "conservative" interpretation needs no VN, since every access of
    // memory yields an unknown value.
    ValueNum fgCurMemoryVN[MemoryKindCount];

    // Return a "pseudo"-class handle for an array element type. If `elemType` is TYP_STRUCT,
    // `elemStructType` is the struct handle (it must be non-null and have a low-order zero bit).
    // Otherwise, `elemTyp` is encoded by left-shifting by 1 and setting the low-order bit to 1.
    // Decode the result by calling `DecodeElemType`.
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
            assert(elemTyp != TYP_STRUCT);
            elemTyp = varTypeToSigned(elemTyp);
            return CORINFO_CLASS_HANDLE(size_t(elemTyp) << 1 | 0x1);
        }
    }

    // Decodes a pseudo-class handle encoded by `EncodeElemType`. Returns TYP_STRUCT if `clsHnd` represents
    // a struct (in which case `clsHnd` is the struct handle). Otherwise, returns the primitive var_types
    // value it represents.
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

    bool GetObjectHandleAndOffset(GenTree* tree, ssize_t* byteOffset, CORINFO_OBJECT_HANDLE* pObj);

    // Convert a BYTE which represents the VM's CorInfoGCtype to the JIT's var_types
    var_types getJitGCType(BYTE gcType);

    // Returns true if the provided type should be treated as a primitive type
    // for the unmanaged calling conventions.
    bool isNativePrimitiveStructType(CORINFO_CLASS_HANDLE clsHnd);

    enum structPassingKind
    {
        SPK_Unknown,       // Invalid value, never returned
        SPK_PrimitiveType, // The struct is passed/returned using a primitive type.
        SPK_EnclosingType, // Like SPK_Primitive type, but used for return types that
                           //  require a primitive type temp that is larger than the struct size.
                           //  Currently used for structs of size 3, 5, 6, or 7 bytes.
        SPK_ByValue,       // The struct is passed/returned by value (using the ABI rules)
                           //  for ARM64 and UNIX_X64 in multiple registers. (when all of the
                           //   parameters registers are used, then the stack will be used)
                           //  for X86 passed on the stack, for ARM32 passed in registers
                           //   or the stack or split between registers and the stack.
        SPK_ByValueAsHfa,  // The struct is passed/returned as an HFA in multiple registers.
        SPK_ByReference
    }; // The struct is passed/returned by reference to a copy/buffer.

    // Get the "primitive" type that is used when we are given a struct of size 'structSize'.
    // For pointer sized structs the 'clsHnd' is used to determine if the struct contains GC ref.
    // A "primitive" type is one of the scalar types: byte, short, int, long, ref, float, double
    // If we can't or shouldn't use a "primitive" type then TYP_UNKNOWN is returned.
    //
    // isVarArg is passed for use on Windows Arm64 to change the decision returned regarding
    // hfa types.
    //
    var_types getPrimitiveTypeForStruct(unsigned structSize, CORINFO_CLASS_HANDLE clsHnd, bool isVarArg);

    // Get the type that is used to pass values of the given struct type.
    // isVarArg is passed for use on Windows Arm64 to change the decision returned regarding
    // hfa types.
    //
    var_types getArgTypeForStruct(CORINFO_CLASS_HANDLE clsHnd,
                                  structPassingKind*   wbPassStruct,
                                  bool                 isVarArg,
                                  unsigned             structSize);

    // Get the type that is used to return values of the given struct type.
    // If the size is unknown, pass 0 and it will be determined from 'clsHnd'.
    var_types getReturnTypeForStruct(CORINFO_CLASS_HANDLE     clsHnd,
                                     CorInfoCallConvExtension callConv,
                                     structPassingKind*       wbPassStruct = nullptr,
                                     unsigned                 structSize   = 0);

#ifdef DEBUG
    // Print a representation of "vnp" or "vn" on standard output.
    // If "level" is non-zero, we also print out a partial expansion of the value.
    void vnpPrint(ValueNumPair vnp, unsigned level);
    void vnPrint(ValueNum vn, unsigned level);
#endif

    // Dominator computation member functions
    // Not exposed outside Compiler
protected:
    void fgComputeReturnBlocks(); // Initialize fgReturnBlocks to a list of BBJ_RETURN blocks.

    // Remove blocks determined to be unreachable by the 'canRemoveBlock'.
    template <typename CanRemoveBlockBody>
    bool fgRemoveUnreachableBlocks(CanRemoveBlockBody canRemoveBlock);

    PhaseStatus fgComputeReachability(); // Perform flow graph node reachability analysis.

    PhaseStatus fgComputeDominators(); // Compute dominators

    bool fgRemoveDeadBlocks(); // Identify and remove dead blocks.

public:
    enum GCPollType
    {
        GCPOLL_NONE,
        GCPOLL_CALL,
        GCPOLL_INLINE
    };

    // Initialize the per-block variable sets (used for liveness analysis).
    void fgInitBlockVarSets();

    PhaseStatus StressSplitTree();
    void SplitTreesRandomly();
    void SplitTreesRemoveCommas();

    template <bool (Compiler::*ExpansionFunction)(BasicBlock**, Statement*, GenTreeCall*)>
    PhaseStatus fgExpandHelper(bool skipRarelyRunBlocks);

    template <bool (Compiler::*ExpansionFunction)(BasicBlock**, Statement*, GenTreeCall*)>
    bool fgExpandHelperForBlock(BasicBlock** pBlock);

    PhaseStatus fgExpandRuntimeLookups();
    bool fgExpandRuntimeLookupsForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);

    PhaseStatus fgExpandThreadLocalAccess();
    bool fgExpandThreadLocalAccessForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);
    bool fgExpandThreadLocalAccessForCallNativeAOT(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);

    PhaseStatus fgExpandStaticInit();
    bool fgExpandStaticInitForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);

    PhaseStatus fgVNBasedIntrinsicExpansion();
    bool fgVNBasedIntrinsicExpansionForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);
    bool fgVNBasedIntrinsicExpansionForCall_ReadUtf8(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);

    PhaseStatus fgLateCastExpansion();
    bool fgLateCastExpansionForCall(BasicBlock** pBlock, Statement* stmt, GenTreeCall* call);

    PhaseStatus fgInsertGCPolls();
    BasicBlock* fgCreateGCPoll(GCPollType pollType, BasicBlock* block);

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
        void UpdateTarget(CompAllocator alloc, BasicBlock* switchBlk, BasicBlock* from, BasicBlock* to);
    };

    typedef JitHashTable<BasicBlock*, JitPtrKeyFuncs<BasicBlock>, SwitchUniqueSuccSet> BlockToSwitchDescMap;

private:
    // Maps BasicBlock*'s that end in switch statements to SwitchUniqueSuccSets that allow
    // iteration over only the distinct successors.
    BlockToSwitchDescMap* m_switchDescMap;

public:
    BlockToSwitchDescMap* GetSwitchDescMap(bool createIfNull = true)
    {
        if ((m_switchDescMap == nullptr) && createIfNull)
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

    bool fgIsFirstBlockOfFilterOrHandler(BasicBlock* block);

    FlowEdge* fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred);

    FlowEdge* fgGetPredForBlock(BasicBlock* block, BasicBlock* blockPred, FlowEdge*** ptrToPred);

    FlowEdge* fgRemoveRefPred(BasicBlock* block, BasicBlock* blockPred);

    FlowEdge* fgRemoveAllRefPreds(BasicBlock* block, BasicBlock* blockPred);

    void fgRemoveBlockAsPred(BasicBlock* block);

    void fgChangeSwitchBlock(BasicBlock* oldSwitchBlock, BasicBlock* newSwitchBlock);

    void fgChangeEhfBlock(BasicBlock* oldBlock, BasicBlock* newBlock);

    void fgReplaceEhfSuccessor(BasicBlock* block, BasicBlock* oldSucc, BasicBlock* newSucc);

    void fgRemoveEhfSuccessor(BasicBlock* block, BasicBlock* succ);

    void fgReplaceJumpTarget(BasicBlock* block, BasicBlock* oldTarget, BasicBlock* newTarget);

    void fgReplacePred(BasicBlock* block, BasicBlock* oldPred, BasicBlock* newPred);

    // initializingPreds is only 'true' when we are computing preds in fgLinkBasicBlocks()
    template <bool initializingPreds = false>
    FlowEdge* fgAddRefPred(BasicBlock* block, BasicBlock* blockPred, FlowEdge* oldEdge = nullptr);

    void fgFindBasicBlocks();

    bool fgCheckEHCanInsertAfterBlock(BasicBlock* blk, unsigned regionIndex, bool putInTryRegion);

    BasicBlock* fgFindInsertPoint(unsigned    regionIndex,
                                  bool        putInTryRegion,
                                  BasicBlock* startBlk,
                                  BasicBlock* endBlk,
                                  BasicBlock* nearBlk,
                                  BasicBlock* jumpBlk,
                                  bool        runRarely);

    unsigned fgGetNestingLevel(BasicBlock* block, unsigned* pFinallyNesting = nullptr);

    PhaseStatus fgPostImportationCleanup();

    void fgRemoveStmt(BasicBlock* block, Statement* stmt DEBUGARG(bool isUnlink = false));
    void fgUnlinkStmt(BasicBlock* block, Statement* stmt);

    bool fgCheckRemoveStmt(BasicBlock* block, Statement* stmt);

    PhaseStatus fgCanonicalizeFirstBB();

    void fgSetEHRegionForNewPreheaderOrExit(BasicBlock* preheader);

    void fgUnreachableBlock(BasicBlock* block);

    void fgRemoveConditionalJump(BasicBlock* block);

    BasicBlock* fgLastBBInMainFunction();

    BasicBlock* fgEndBBAfterMainFunction();

    BasicBlock* fgGetDomSpeculatively(const BasicBlock* block);

    void fgUnlinkRange(BasicBlock* bBeg, BasicBlock* bEnd);

    BasicBlock* fgRemoveBlock(BasicBlock* block, bool unreachable);

    void fgPrepareCallFinallyRetForRemoval(BasicBlock* block);

    bool fgCanCompactBlocks(BasicBlock* block, BasicBlock* bNext);

    void fgCompactBlocks(BasicBlock* block, BasicBlock* bNext);

    BasicBlock* fgConnectFallThrough(BasicBlock* bSrc, BasicBlock* bDst);

    bool fgRenumberBlocks();

    bool fgExpandRarelyRunBlocks();

    bool fgEhAllowsMoveBlock(BasicBlock* bBefore, BasicBlock* bAfter);

    void fgMoveBlocksAfter(BasicBlock* bStart, BasicBlock* bEnd, BasicBlock* insertAfterBlk);

    PhaseStatus fgHeadTailMerge(bool early);
    bool fgHeadMerge(BasicBlock* block, bool early);
    bool fgTryOneHeadMerge(BasicBlock* block, bool early);
    bool gtTreeContainsTailCall(GenTree* tree);
    bool fgCanMoveFirstStatementIntoPred(bool early, Statement* firstStmt, BasicBlock* pred);

    enum FG_RELOCATE_TYPE
    {
        FG_RELOCATE_TRY,    // relocate the 'try' region
        FG_RELOCATE_HANDLER // relocate the handler region (including the filter if necessary)
    };
    BasicBlock* fgRelocateEHRange(unsigned regionIndex, FG_RELOCATE_TYPE relocateType);

#if defined(FEATURE_EH_FUNCLETS)
    bool fgIsIntraHandlerPred(BasicBlock* predBlock, BasicBlock* block);
    bool fgAnyIntraHandlerPreds(BasicBlock* block);
    void fgInsertFuncletPrologBlock(BasicBlock* block);
    void        fgCreateFuncletPrologBlocks();
    PhaseStatus fgCreateFunclets();
#else  // !FEATURE_EH_FUNCLETS
    bool fgRelocateEHRegions();
#endif // !FEATURE_EH_FUNCLETS

    bool fgOptimizeUncondBranchToSimpleCond(BasicBlock* block, BasicBlock* target);

    bool fgBlockEndFavorsTailDuplication(BasicBlock* block, unsigned lclNum);

    bool fgBlockIsGoodTailDuplicationCandidate(BasicBlock* block, unsigned* lclNum);

    bool fgOptimizeEmptyBlock(BasicBlock* block);

    bool fgOptimizeBranchToEmptyUnconditional(BasicBlock* block, BasicBlock* bDest);

    bool fgOptimizeBranch(BasicBlock* bJump);

    bool fgOptimizeSwitchBranches(BasicBlock* block);

    bool fgOptimizeSwitchJumps();
#ifdef DEBUG
    void fgPrintEdgeWeights();
#endif
    PhaseStatus fgComputeBlockAndEdgeWeights();
    bool fgComputeMissingBlockWeights(weight_t* returnWeight);
    bool fgComputeCalledCount(weight_t returnWeight);
    PhaseStatus fgComputeEdgeWeights();

    bool fgReorderBlocks(bool useProfile);

#ifdef FEATURE_EH_FUNCLETS
    bool fgFuncletsAreCold();
#endif // FEATURE_EH_FUNCLETS

    PhaseStatus fgDetermineFirstColdBlock();

    bool fgIsForwardBranch(BasicBlock* bJump, BasicBlock* bDest, BasicBlock* bSrc = nullptr);

    bool fgUpdateFlowGraph(bool doTailDup = false, bool isPhase = false);
    PhaseStatus fgUpdateFlowGraphPhase();

    PhaseStatus fgDfsBlocksAndRemove();

    PhaseStatus fgFindOperOrder();

    // method that returns if you should split here
    typedef bool(fgSplitPredicate)(GenTree* tree, GenTree* parent, fgWalkData* data);

    PhaseStatus fgSetBlockOrder();
    bool fgHasCycleWithoutGCSafePoint();

    template<typename VisitPreorder, typename VisitPostorder, typename VisitEdge>
    unsigned fgRunDfs(VisitPreorder assignPreorder, VisitPostorder assignPostorder, VisitEdge visitEdge);

    FlowGraphDfsTree* fgComputeDfs();
    void fgInvalidateDfsTree();

    void fgRemoveReturnBlock(BasicBlock* block);

    void fgConvertBBToThrowBB(BasicBlock* block);

    bool fgCastNeeded(GenTree* tree, var_types toType);

    void fgLoopCallTest(BasicBlock* srcBB, BasicBlock* dstBB);
    void fgLoopCallMark();

    unsigned fgGetCodeEstimate(BasicBlock* block);

#if DUMP_FLOWGRAPHS
    enum class PhasePosition
    {
        PrePhase,
        PostPhase
    };
    const char* fgProcessEscapes(const char* nameIn, escapeMapping_t* map);
    static void fgDumpTree(FILE* fgxFile, GenTree* const tree);
    FILE* fgOpenFlowGraphFile(bool* wbDontClose, Phases phase, PhasePosition pos, const char* type);
    bool fgDumpFlowGraph(Phases phase, PhasePosition pos);
    void fgDumpFlowGraphLoops(FILE* file);
#endif // DUMP_FLOWGRAPHS

#ifdef DEBUG

    void fgDispBBLiveness(BasicBlock* block);
    void fgDispBBLiveness();
    void fgTableDispBasicBlock(const BasicBlock* block, const BasicBlock* nextBlock = nullptr, int blockTargetFieldWidth = 21, int ibcColWidth = 0);
    void fgDispBasicBlocks(BasicBlock* firstBlock, BasicBlock* lastBlock, bool dumpTrees);
    void fgDispBasicBlocks(bool dumpTrees = false);
    void fgDumpStmtTree(const BasicBlock* block, Statement* stmt);
    void fgDumpBlock(BasicBlock* block);
    void fgDumpTrees(BasicBlock* firstBlock, BasicBlock* lastBlock);

    void fgDumpBlockMemorySsaIn(BasicBlock* block);
    void fgDumpBlockMemorySsaOut(BasicBlock* block);

    static fgWalkPreFn fgStress64RsltMulCB;
    void               fgStress64RsltMul();
    void               fgDebugCheckUpdate();

    void fgDebugCheckBBNumIncreasing();
    void fgDebugCheckBBlist(bool checkBBNum = false, bool checkBBRefs = true);
    void fgDebugCheckBlockLinks();
    void fgDebugCheckLinks(bool morphTrees = false);
    void fgDebugCheckStmtsList(BasicBlock* block, bool morphTrees);
    void fgDebugCheckNodeLinks(BasicBlock* block, Statement* stmt);
    void fgDebugCheckLinkedLocals();
    void fgDebugCheckNodesUniqueness();
    void fgDebugCheckLoops();
    void fgDebugCheckSsa();

    void fgDebugCheckTypes(GenTree* tree);
    void fgDebugCheckFlags(GenTree* tree, BasicBlock* block);
    void fgDebugCheckDispFlags(GenTree* tree, GenTreeFlags dispFlags, GenTreeDebugFlags debugFlags);
    void fgDebugCheckFlagsHelper(GenTree* tree, GenTreeFlags actualFlags, GenTreeFlags expectedFlags);
    void fgDebugCheckTryFinallyExits();
    void fgDebugCheckProfileWeights();
    void fgDebugCheckProfileWeights(ProfileChecks checks);
    bool fgDebugCheckIncomingProfileData(BasicBlock* block, ProfileChecks checks);
    bool fgDebugCheckOutgoingProfileData(BasicBlock* block, ProfileChecks checks);

    void fgDebugCheckDfsTree();

#endif // DEBUG

    static bool fgProfileWeightsEqual(weight_t weight1, weight_t weight2, weight_t epsilon = 0.01);
    static bool fgProfileWeightsConsistent(weight_t weight1, weight_t weight2);

    static GenTree* fgGetFirstNode(GenTree* tree);

    //--------------------- Walking the trees in the IR -----------------------

    struct fgWalkData
    {
        Compiler*     compiler;
        fgWalkPreFn*  wtprVisitorFn;
        fgWalkPostFn* wtpoVisitorFn;
        void*         pCallbackData; // user-provided data
        GenTree*      parent;        // parent of current node, provided to callback
        bool          wtprLclsOnly;  // whether to only visit lclvar nodes
#ifdef DEBUG
        bool printModified; // callback can use this
#endif
    };

    fgWalkResult fgWalkTreePre(GenTree**    pTree,
                               fgWalkPreFn* visitor,
                               void*        pCallBackData = nullptr,
                               bool         lclVarsOnly   = false,
                               bool         computeStack  = false);

    fgWalkResult fgWalkTree(GenTree**     pTree,
                            fgWalkPreFn*  preVisitor,
                            fgWalkPostFn* postVisitor,
                            void*         pCallBackData = nullptr);

    void fgWalkAllTreesPre(fgWalkPreFn* visitor, void* pCallBackData);

    //----- Postorder

    fgWalkResult fgWalkTreePost(GenTree**     pTree,
                                fgWalkPostFn* visitor,
                                void*         pCallBackData = nullptr,
                                bool          computeStack  = false);

#ifdef DEBUG
    void fgInvalidateBBLookup();
#endif // DEBUG

    /**************************************************************************
     *                          PROTECTED
     *************************************************************************/

protected:
    friend class SsaBuilder;
    friend class ValueNumberState;

    //--------------------- Detect the basic blocks ---------------------------

    BasicBlock** fgBBs; // Table of pointers to the BBs

    void        fgInitBBLookup();
    BasicBlock* fgLookupBB(unsigned addr);

    bool fgCanSwitchToOptimized();
    void fgSwitchToOptimized(const char* reason);

    bool fgMayExplicitTailCall();

    void fgFindJumpTargets(const BYTE* codeAddr, IL_OFFSET codeSize, FixedBitVect* jumpTarget);

    void fgMarkBackwardJump(BasicBlock* startBlock, BasicBlock* endBlock);

    void fgLinkBasicBlocks();

    unsigned fgMakeBasicBlocks(const BYTE* codeAddr, IL_OFFSET codeSize, FixedBitVect* jumpTarget);

    void fgCheckBasicBlockControlFlow();

    void fgControlFlowPermitted(BasicBlock* blkSrc,
                                BasicBlock* blkDest,
                                bool        IsLeave = false /* is the src a leave block */);

    bool fgFlowToFirstBlockOfInnerTry(BasicBlock* blkSrc, BasicBlock* blkDest, bool sibling);

    void fgObserveInlineConstants(OPCODE opcode, const FgStack& stack, bool isInlining);

    void fgAdjustForAddressExposedOrWrittenThis();

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
    bool fgHaveProfileWeights();
    bool fgGetProfileWeightForBasicBlock(IL_OFFSET offset, weight_t* weight);

    Instrumentor* fgCountInstrumentor;
    Instrumentor* fgHistogramInstrumentor;
    Instrumentor* fgValueInstrumentor;

    PhaseStatus fgPrepareToInstrumentMethod();
    PhaseStatus fgInstrumentMethod();
    PhaseStatus fgIncorporateProfileData();
    bool        fgIncorporateBlockCounts();
    bool        fgIncorporateEdgeCounts();

public:
    const char*                            fgPgoFailReason;
    bool                                   fgPgoDisabled;
    ICorJitInfo::PgoSource                 fgPgoSource;
    ICorJitInfo::PgoInstrumentationSchema* fgPgoSchema;
    BYTE*                                  fgPgoData;
    UINT32                                 fgPgoSchemaCount;
    HRESULT                                fgPgoQueryResult;
    UINT32                                 fgNumProfileRuns;
    UINT32                                 fgPgoBlockCounts;
    UINT32                                 fgPgoEdgeCounts;
    UINT32                                 fgPgoClassProfiles;
    UINT32                                 fgPgoMethodProfiles;
    unsigned                               fgPgoInlineePgo;
    unsigned                               fgPgoInlineeNoPgo;
    unsigned                               fgPgoInlineeNoPgoSingleBlock;
    bool                                   fgPgoHaveWeights;

    void WalkSpanningTree(SpanningTreeVisitor* visitor);
    void fgSetProfileWeight(BasicBlock* block, weight_t weight);
    void fgApplyProfileScale();
    bool fgHaveSufficientProfileWeights();
    bool fgHaveTrustedProfileWeights();

    // fgIsUsingProfileWeights - returns true if we have real profile data for this method
    //                           or if we have some fake profile data for the stress mode
    bool fgIsUsingProfileWeights()
    {
        return (fgHaveProfileWeights() || fgStressBBProf());
    }

    // fgProfileRunsCount - returns total number of scenario runs for the profile data
    //                      or BB_UNITY_WEIGHT_UNSIGNED when we aren't using profile data.
    unsigned fgProfileRunsCount()
    {
        return fgIsUsingProfileWeights() ? fgNumProfileRuns : BB_UNITY_WEIGHT_UNSIGNED;
    }

//-------- Insert a statement at the start or end of a basic block --------

#ifdef DEBUG
public:
    static bool fgBlockContainsStatementBounded(BasicBlock* block, Statement* stmt, bool answerOnBoundExceeded = true);
#endif

public:
    Statement* fgNewStmtAtBeg(BasicBlock* block, GenTree* tree, const DebugInfo& di = DebugInfo());
    void fgInsertStmtAtEnd(BasicBlock* block, Statement* stmt);
    Statement* fgNewStmtAtEnd(BasicBlock* block, GenTree* tree, const DebugInfo& di = DebugInfo());
    Statement* fgNewStmtNearEnd(BasicBlock* block, GenTree* tree, const DebugInfo& di = DebugInfo());

private:
    void fgInsertStmtNearEnd(BasicBlock* block, Statement* stmt);
    void fgInsertStmtAtBeg(BasicBlock* block, Statement* stmt);

public:
    void fgInsertStmtAfter(BasicBlock* block, Statement* insertionPoint, Statement* stmt);
    void fgInsertStmtBefore(BasicBlock* block, Statement* insertionPoint, Statement* stmt);

private:
    Statement* fgInsertStmtListAfter(BasicBlock* block, Statement* stmtAfter, Statement* stmtList);

    //                  Create a new temporary variable to hold the result of *ppTree,
    //                  and transform the graph accordingly.
    GenTree* fgInsertCommaFormTemp(GenTree** ppTree);
    TempInfo fgMakeTemp(GenTree* rhs);
    GenTree* fgMakeMultiUse(GenTree** ppTree);

    //                  Recognize a bitwise rotation pattern and convert into a GT_ROL or a GT_ROR node.
    GenTree* fgRecognizeAndMorphBitwiseRotation(GenTree* tree);
    bool fgOperIsBitwiseRotationRoot(genTreeOps oper);

#if !defined(TARGET_64BIT)
    //                  Recognize and morph a long multiplication with 32 bit operands.
    GenTreeOp* fgRecognizeAndMorphLongMul(GenTreeOp* mul);
    GenTreeOp* fgMorphLongMul(GenTreeOp* mul);
#endif

    //-------- Determine the order in which the trees will be evaluated -------
public:
    void fgSetStmtSeq(Statement* stmt);

private:
    GenTree* fgSetTreeSeq(GenTree* tree, bool isLIR = false);
    void fgSetBlockOrder(BasicBlock* block);

    //------------------------- Morphing --------------------------------------

    unsigned fgPtrArgCntMax;

public:
    //------------------------------------------------------------------------
    // fgGetPtrArgCntMax: Return the maximum number of pointer-sized stack arguments that calls inside this method
    // can push on the stack. This value is calculated during morph.
    //
    // Return Value:
    //    Returns fgPtrArgCntMax, that is a private field.
    //
    unsigned fgGetPtrArgCntMax() const
    {
        return fgPtrArgCntMax;
    }

    //------------------------------------------------------------------------
    // fgSetPtrArgCntMax: Set the maximum number of pointer-sized stack arguments that calls inside this method
    // can push on the stack. This function is used during StackLevelSetter to fix incorrect morph calculations.
    //
    void fgSetPtrArgCntMax(unsigned argCntMax)
    {
        fgPtrArgCntMax = argCntMax;
    }

    bool compCanEncodePtrArgCntMax();

private:
    hashBv*               fgAvailableOutgoingArgTemps;
    ArrayStack<unsigned>* fgUsedSharedTemps;

    void fgSetRngChkTarget(GenTree* tree, bool delay = true);

    BasicBlock* fgSetRngChkTargetInner(SpecialCodeKind kind, bool delay);

#if REARRANGE_ADDS
    void fgMoveOpsLeft(GenTree* tree);
#endif

    bool fgIsCommaThrow(GenTree* tree, bool forFolding = false);

    bool fgIsThrow(GenTree* tree);

public:
    bool fgInDifferentRegions(const BasicBlock* blk1, const BasicBlock* blk2) const;

private:
    bool fgIsBlockCold(BasicBlock* block);

    GenTree* fgMorphCastIntoHelper(GenTree* tree, int helper, GenTree* oper);

    GenTree* fgMorphIntoHelperCall(
        GenTree* tree, int helper, bool morphArgs, GenTree* arg1 = nullptr, GenTree* arg2 = nullptr);

    // A "MorphAddrContext" carries information from the surrounding context.  If we are evaluating a byref address,
    // it is useful to know whether the address will be immediately dereferenced, or whether the address value will
    // be used, perhaps by passing it as an argument to a called method.  This affects how null checking is done:
    // for sufficiently small offsets, we can rely on OS page protection to implicitly null-check addresses that we
    // know will be dereferenced.  To know that reliance on implicit null checking is sound, we must further know that
    // all offsets between the top-level indirection and the bottom are constant, and that their sum is sufficiently
    // small; hence the other fields of MorphAddrContext.
    struct MorphAddrContext
    {
        GenTreeIndir* m_user = nullptr;  // Indirection using this address.
        size_t        m_totalOffset = 0; // Sum of offsets between the top-level indirection and here (current context).
    };

#ifdef FEATURE_SIMD
    GenTree* getSIMDStructFromField(GenTree*  tree,
                                    unsigned* indexOut,
                                    unsigned* simdSizeOut,
                                    bool      ignoreUsedInSIMDIntrinsic = false);
    bool fgMorphCombineSIMDFieldStores(BasicBlock* block, Statement* stmt);
    void impMarkContiguousSIMDFieldStores(Statement* stmt);

    // fgPreviousCandidateSIMDFieldStoreStmt is only used for tracking previous simd field assignment
    // in function: Compiler::impMarkContiguousSIMDFieldStores.
    Statement* fgPreviousCandidateSIMDFieldStoreStmt;

#endif // FEATURE_SIMD
    GenTree* fgMorphIndexAddr(GenTreeIndexAddr* tree);
    GenTree* fgMorphExpandCast(GenTreeCast* tree);
    GenTreeFieldList* fgMorphLclArgToFieldlist(GenTreeLclVarCommon* lcl);
    GenTreeCall* fgMorphArgs(GenTreeCall* call);

    void fgMakeOutgoingStructArgCopy(GenTreeCall* call, CallArg* arg);

    GenTree* fgMorphLeafLocal(GenTreeLclVarCommon* lclNode);
#ifdef TARGET_X86
    GenTree* fgMorphExpandStackArgForVarArgs(GenTreeLclVarCommon* lclNode);
#endif // TARGET_X86
    GenTree* fgMorphExpandImplicitByRefArg(GenTreeLclVarCommon* lclNode);
    GenTree* fgMorphExpandLocal(GenTreeLclVarCommon* lclNode);

public:
    bool fgAddrCouldBeNull(GenTree* addr);
    void fgAssignSetVarDef(GenTree* tree);

private:
    GenTree* fgMorphFieldAddr(GenTree* tree, MorphAddrContext* mac);
    GenTree* fgMorphExpandInstanceField(GenTree* tree, MorphAddrContext* mac);
    GenTree* fgMorphExpandTlsFieldAddr(GenTree* tree);
    bool fgCanFastTailCall(GenTreeCall* call, const char** failReason);
#if FEATURE_FASTTAILCALL
    bool fgCallHasMustCopyByrefParameter(GenTreeCall* call);
    bool fgCallArgWillPointIntoLocalFrame(GenTreeCall* call, CallArg& arg);

#endif
    GenTree* fgMorphTailCallViaHelpers(GenTreeCall* call, CORINFO_TAILCALL_HELPERS& help);
    bool fgCanTailCallViaJitHelper(GenTreeCall* call);
    void fgMorphTailCallViaJitHelper(GenTreeCall* call);
    GenTree* fgCreateCallDispatcherAndGetResult(GenTreeCall*          origCall,
                                                CORINFO_METHOD_HANDLE callTargetStubHnd,
                                                CORINFO_METHOD_HANDLE dispatcherHnd);
    GenTree* getLookupTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                           CORINFO_LOOKUP*         pLookup,
                           GenTreeFlags            handleFlags,
                           void*                   compileTimeHandle);
    GenTree* getRuntimeLookupTree(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                  CORINFO_LOOKUP*         pLookup,
                                  void*                   compileTimeHandle);
    GenTree* getVirtMethodPointerTree(GenTree*                thisPtr,
                                      CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                      CORINFO_CALL_INFO*      pCallInfo);
    GenTree* getTokenHandleTree(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool parent);

    GenTree* fgMorphPotentialTailCall(GenTreeCall* call);
    void fgValidateIRForTailCall(GenTreeCall* call);
    GenTree* fgGetStubAddrArg(GenTreeCall* call);
    unsigned fgGetArgParameterLclNum(GenTreeCall* call, CallArg* arg);
    void fgMorphRecursiveFastTailCallIntoLoop(BasicBlock* block, GenTreeCall* recursiveTailCall);
    Statement* fgAssignRecursiveCallArgToCallerParam(GenTree*         arg,
                                                     CallArg*         callArg,
                                                     unsigned         lclParamNum,
                                                     BasicBlock*      block,
                                                     const DebugInfo& callDI,
                                                     Statement*       tmpAssignmentInsertionPoint,
                                                     Statement*       paramAssignmentInsertionPoint);
    GenTree* fgMorphCall(GenTreeCall* call);
    GenTree* fgExpandVirtualVtableCallTarget(GenTreeCall* call);

    void fgMorphCallInline(GenTreeCall* call, InlineResult* result);
    void fgMorphCallInlineHelper(GenTreeCall* call, InlineResult* result, InlineContext** createdContext);
#if DEBUG
    void fgNoteNonInlineCandidate(Statement* stmt, GenTreeCall* call);
    static fgWalkPreFn fgFindNonInlineCandidate;
#endif
    GenTree* fgOptimizeDelegateConstructor(GenTreeCall*            call,
                                           CORINFO_CONTEXT_HANDLE* ExactContextHnd,
                                           methodPointerInfo*      ldftnToken);
    GenTree* fgMorphLeaf(GenTree* tree);
public:
    GenTree* fgMorphInitBlock(GenTree* tree);
    GenTree* fgMorphCopyBlock(GenTree* tree);
    GenTree* fgMorphStoreDynBlock(GenTreeStoreDynBlk* tree);
private:
    GenTree* fgMorphSmpOp(GenTree* tree, MorphAddrContext* mac, bool* optAssertionPropDone = nullptr);
    void fgTryReplaceStructLocalWithField(GenTree* tree);
    GenTree* fgMorphFinalizeIndir(GenTreeIndir* indir);
    GenTree* fgOptimizeCast(GenTreeCast* cast);
    GenTree* fgOptimizeCastOnStore(GenTree* store);
    GenTree* fgOptimizeBitCast(GenTreeUnOp* bitCast);
    GenTree* fgOptimizeEqualityComparisonWithConst(GenTreeOp* cmp);
    GenTree* fgOptimizeRelationalComparisonWithConst(GenTreeOp* cmp);
    GenTree* fgOptimizeRelationalComparisonWithFullRangeConst(GenTreeOp* cmp);
#ifdef FEATURE_HW_INTRINSICS
    GenTree* fgOptimizeHWIntrinsic(GenTreeHWIntrinsic* node);
#endif
    GenTree* fgOptimizeCommutativeArithmetic(GenTreeOp* tree);
    GenTree* fgOptimizeRelationalComparisonWithCasts(GenTreeOp* cmp);
    GenTree* fgOptimizeAddition(GenTreeOp* add);
    GenTree* fgOptimizeMultiply(GenTreeOp* mul);
    GenTree* fgOptimizeBitwiseAnd(GenTreeOp* andOp);
    GenTree* fgOptimizeBitwiseXor(GenTreeOp* xorOp);
    GenTree* fgPropagateCommaThrow(GenTree* parent, GenTreeOp* commaThrow, GenTreeFlags precedingSideEffects);
    GenTree* fgMorphRetInd(GenTreeUnOp* tree);
    GenTree* fgMorphModToZero(GenTreeOp* tree);
    GenTree* fgMorphModToSubMulDiv(GenTreeOp* tree);
    GenTree* fgMorphUModToAndSub(GenTreeOp* tree);
    GenTree* fgMorphSmpOpOptional(GenTreeOp* tree, bool* optAssertionPropDone);
    GenTree* fgMorphMultiOp(GenTreeMultiOp* multiOp);
    GenTree* fgMorphConst(GenTree* tree);

    GenTreeOp* fgMorphCommutative(GenTreeOp* tree);

    GenTree* fgMorphReduceAddOps(GenTree* tree);

public:
    GenTree* fgMorphTree(GenTree* tree, MorphAddrContext* mac = nullptr);

private:
    void fgAssertionGen(GenTree* tree);
    void fgKillDependentAssertionsSingle(unsigned lclNum DEBUGARG(GenTree* tree));
    void fgKillDependentAssertions(unsigned lclNum DEBUGARG(GenTree* tree));
    void fgMorphTreeDone(GenTree* tree);
    void fgMorphTreeDone(GenTree* tree, bool optAssertionPropDone, bool isMorphedTree DEBUGARG(int morphNum = 0));

    Statement* fgMorphStmt;
    unsigned   fgBigOffsetMorphingTemps[TYP_COUNT];

    unsigned fgGetFieldMorphingTemp(GenTreeFieldAddr* fieldNode);

    //----------------------- Liveness analysis -------------------------------

    VARSET_TP fgCurUseSet; // vars used     by block (before an assignment)
    VARSET_TP fgCurDefSet; // vars assigned by block (before a use)

    MemoryKindSet fgCurMemoryUse;   // True iff the current basic block uses memory.
    MemoryKindSet fgCurMemoryDef;   // True iff the current basic block modifies memory.
    MemoryKindSet fgCurMemoryHavoc; // True if  the current basic block is known to set memory to a "havoc" value.

    bool byrefStatesMatchGcHeapStates; // True iff GcHeap and ByrefExposed memory have all the same def points.

    PhaseStatus fgEarlyLiveness();

    void fgMarkUseDef(GenTreeLclVarCommon* tree);

    void fgBeginScopeLife(VARSET_TP* inScope, VarScopeDsc* var);
    void fgEndScopeLife(VARSET_TP* inScope, VarScopeDsc* var);

    void fgMarkInScope(BasicBlock* block, VARSET_VALARG_TP inScope);
    void fgUnmarkInScope(BasicBlock* block, VARSET_VALARG_TP unmarkScope);

    void fgExtendDbgScopes();
    void fgExtendDbgLifetimes();

#ifdef DEBUG
    void fgDispDebugScopes();
#endif // DEBUG

    //-------------------------------------------------------------------------
    //
    //  The following keeps track of any code we've added for things like array
    //  range checking or explicit calls to enable GC, and so on.
    //
public:
    struct AddCodeDsc
    {
        AddCodeDsc*     acdNext;

        // Initially the source block of the exception. After fgCreateThrowHelperBlocks, the block to which
        // we jump to raise the exception.
        BasicBlock*     acdDstBlk;

        unsigned        acdData;
        SpecialCodeKind acdKind; // what kind of a special block is this?
        bool            acdUsed; // do we need to keep this helper block?
#if !FEATURE_FIXED_OUT_ARGS
        bool     acdStkLvlInit; // has acdStkLvl value been already set?
        unsigned acdStkLvl;     // stack level in stack slots.
#endif                          // !FEATURE_FIXED_OUT_ARGS
    };

    struct AddCodeDscKey
    {
    public:
        AddCodeDscKey(): acdKind(SCK_NONE), acdData(0) {}
        AddCodeDscKey(SpecialCodeKind kind, unsigned data): acdKind(kind), acdData(data) {}

        static bool Equals(const AddCodeDscKey& x, const AddCodeDscKey& y)
        {
            return (x.acdData == y.acdData) && (x.acdKind == y.acdKind);
        }

        static unsigned GetHashCode(const AddCodeDscKey& x)
        {
            return (x.acdData << 3) | (unsigned) x.acdKind;
        }

    private:
        SpecialCodeKind acdKind;
        unsigned acdData;
    };

    typedef JitHashTable<AddCodeDscKey, AddCodeDscKey, AddCodeDsc*> AddCodeDscMap;

    AddCodeDscMap* fgGetAddCodeDscMap();

private:
    static unsigned acdHelper(SpecialCodeKind codeKind);

    AddCodeDsc* fgAddCodeList;
    bool        fgRngChkThrowAdded;
    AddCodeDscMap* fgAddCodeDscMap;

    void fgAddCodeRef(BasicBlock* srcBlk, SpecialCodeKind kind);
    PhaseStatus fgCreateThrowHelperBlocks();

public:
    AddCodeDsc* fgFindExcptnTarget(SpecialCodeKind kind, unsigned refData);

    bool fgUseThrowHelperBlocks();

    AddCodeDsc* fgGetAdditionalCodeDescriptors()
    {
        return fgAddCodeList;
    }

private:
    bool fgIsThrowHlpBlk(BasicBlock* block);

#if !FEATURE_FIXED_OUT_ARGS
    unsigned fgThrowHlpBlkStkLevel(BasicBlock* block);
#endif // !FEATURE_FIXED_OUT_ARGS

    unsigned fgCheckInlineDepthAndRecursion(InlineInfo* inlineInfo);
    bool IsDisallowedRecursiveInline(InlineContext* ancestor, InlineInfo* inlineInfo);
    bool ContextComplexityExceeds(CORINFO_CONTEXT_HANDLE handle, int max);
    bool MethodInstantiationComplexityExceeds(CORINFO_METHOD_HANDLE handle, int& cur, int max);
    bool TypeInstantiationComplexityExceeds(CORINFO_CLASS_HANDLE handle, int& cur, int max);

    void fgInvokeInlineeCompiler(GenTreeCall* call, InlineResult* result, InlineContext** createdContext);
    void fgInsertInlineeBlocks(InlineInfo* pInlineInfo);
    Statement* fgInlinePrependStatements(InlineInfo* inlineInfo);
    void fgInlineAppendStatements(InlineInfo* inlineInfo, BasicBlock* block, Statement* stmt);

#ifdef DEBUG
    static fgWalkPreFn fgDebugCheckInlineCandidates;

    void               CheckNoTransformableIndirectCallsRemain();
    static fgWalkPreFn fgDebugCheckForTransformableIndirectCalls;
#endif

    PhaseStatus fgPromoteStructs();
    void fgMorphLocalField(GenTree* tree, GenTree* parent);

    // Reset the refCount for implicit byrefs.
    void fgResetImplicitByRefRefCount();

    // Identify all candidates for last-use copy omission.
    PhaseStatus fgMarkImplicitByRefCopyOmissionCandidates();

    // Change implicit byrefs' types from struct to pointer, and for any that were
    // promoted, create new promoted struct temps.
    PhaseStatus fgRetypeImplicitByRefArgs();

    // Clear up annotations for any struct promotion temps created for implicit byrefs.
    void fgMarkDemotedImplicitByRefArgs();

    PhaseStatus fgMarkAddressExposedLocals();
    void fgSequenceLocals(Statement* stmt);

    PhaseStatus PhysicalPromotion();

    PhaseStatus fgForwardSub();
    bool fgForwardSubBlock(BasicBlock* block);
    bool fgForwardSubStatement(Statement* statement);
    bool fgForwardSubHasStoreInterference(Statement* defStmt, Statement* nextStmt, GenTree* nextStmtUse);
    void fgForwardSubUpdateLiveness(GenTree* newSubListFirst, GenTree* newSubListLast);

    // The given local variable, required to be a struct variable, is being assigned via
    // a "lclField", to make it masquerade as an integral type in the ABI.  Make sure that
    // the variable is not enregistered, and is therefore not promoted independently.
    void fgLclFldAssign(unsigned lclNum);

    enum TypeProducerKind
    {
        TPK_Unknown = 0, // May not be a RuntimeType
        TPK_Handle  = 1, // RuntimeType via handle
        TPK_GetType = 2, // RuntimeType via Object.get_Type()
        TPK_Null    = 3, // Tree value is null
        TPK_Other   = 4  // RuntimeType via other means
    };

    TypeProducerKind gtGetTypeProducerKind(GenTree* tree);
    bool gtIsTypeHandleToRuntimeTypeHelper(GenTreeCall* call);
    bool gtIsTypeHandleToRuntimeTypeHandleHelper(GenTreeCall* call, CorInfoHelpFunc* pHelper = nullptr);
    bool gtIsActiveCSE_Candidate(GenTree* tree);

    bool gtTreeContainsOper(GenTree* tree, genTreeOps op);
    ExceptionSetFlags gtCollectExceptions(GenTree* tree);

public:
    bool fgIsBigOffset(size_t offset);

private:
    bool fgNeedReturnSpillTemp();

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

    PhaseStatus rangeCheckPhase();
    GenTree* optRemoveRangeCheck(GenTreeBoundsChk* check, GenTree* comma, Statement* stmt);
    GenTree* optRemoveStandaloneRangeCheck(GenTreeBoundsChk* check, Statement* stmt);
    void optRemoveCommaBasedRangeCheck(GenTree* comma, Statement* stmt);

protected:
    // Do hoisting for all loops.
    PhaseStatus optHoistLoopCode();

    // To represent sets of VN's that have already been hoisted in outer loops.
    typedef JitHashTable<ValueNum, JitSmallPrimitiveKeyFuncs<ValueNum>, bool> VNSet;

    struct LoopHoistContext
    {
    private:
        // The set of variables hoisted in the current loop (or nullptr if there are none).
        VNSet* m_pHoistedInCurLoop;

    public:
        // Value numbers of expressions that have been hoisted in the current (or most recent) loop in the nest.
        // Previous decisions on loop-invariance of value numbers in the current loop.
        VNSet m_curLoopVnInvariantCache;

        int m_loopVarInOutCount;
        int m_loopVarCount;
        int m_hoistedExprCount;

        int m_loopVarInOutFPCount;
        int m_loopVarFPCount;
        int m_hoistedFPExprCount;

#ifdef TARGET_XARCH
        int m_loopVarInOutMskCount;
        int m_loopVarMskCount;
        int m_hoistedMskExprCount;
#endif // TARGET_XARCH

        // Get the VN cache for current loop
        VNSet* GetHoistedInCurLoop(Compiler* comp)
        {
            if (m_pHoistedInCurLoop == nullptr)
            {
                m_pHoistedInCurLoop = new (comp->getAllocatorLoopHoist()) VNSet(comp->getAllocatorLoopHoist());
            }
            return m_pHoistedInCurLoop;
        }

        // Return the so far collected VNs in cache for current loop and reset it.
        void ResetHoistedInCurLoop()
        {
            m_pHoistedInCurLoop = nullptr;
            JITDUMP("Resetting m_pHoistedInCurLoop\n");
        }

        LoopHoistContext(Compiler* comp)
            : m_pHoistedInCurLoop(nullptr), m_curLoopVnInvariantCache(comp->getAllocatorLoopHoist())
        {
        }
    };

    // Do hoisting for a particular loop
    bool optHoistThisLoop(FlowGraphNaturalLoop* loop, LoopHoistContext* hoistCtxt);

    // Hoist all expressions in "blocks" that are invariant in "loop"
    // outside of that loop.
    void optHoistLoopBlocks(FlowGraphNaturalLoop* loop, ArrayStack<BasicBlock*>* blocks, LoopHoistContext* hoistContext);

    // Return true if the tree looks profitable to hoist out of "loop"
    bool optIsProfitableToHoistTree(GenTree* tree, FlowGraphNaturalLoop* loop, LoopHoistContext* hoistCtxt);

    // Performs the hoisting "tree" into the PreHeader for "loop"
    void optHoistCandidate(GenTree* tree, BasicBlock* treeBb, FlowGraphNaturalLoop* loop, LoopHoistContext* hoistCtxt);

    // Note the new SSA uses in tree
    void optRecordSsaUses(GenTree* tree, BasicBlock* block);

    // Returns true iff the ValueNum "vn" represents a value that is loop-invariant in "loop".
    //   Constants and init values are always loop invariant.
    //   VNPhi's connect VN's to the SSA definition, so we can know if the SSA def occurs in the loop.
    bool optVNIsLoopInvariant(ValueNum vn, FlowGraphNaturalLoop* loop, VNSet* recordedVNs);

    // Records the set of "side effects" of all loops: fields (object instance and static)
    // written to, and SZ-array element type equivalence classes updated.
    void optComputeLoopSideEffects();

    // Compute the sets of long and float vars (lvaLongVars, lvaFloatVars, lvaMaskVars).
    void optComputeInterestingVarSets();

private:
    // Given a loop mark it and any nested loops as having 'memoryHavoc'
    void optRecordLoopNestsMemoryHavoc(FlowGraphNaturalLoop* loop, MemoryKindSet memoryHavoc);

    // Add the side effects of "blk" (which is required to be within a loop) to all loops of which it is a part.
    void optComputeLoopSideEffectsOfBlock(BasicBlock* blk, FlowGraphNaturalLoop* mostNestedLoop);

    // Hoist the expression "expr" out of "loop"
    void optPerformHoistExpr(GenTree* expr, BasicBlock* exprBb, FlowGraphNaturalLoop* loop);

public:
    PhaseStatus optOptimizeBools();
    PhaseStatus optSwitchRecognition();
    bool optSwitchConvert(BasicBlock* firstBlock, int testsCount, ssize_t* testValues, GenTree* nodeToTest);
    bool optSwitchDetectAndConvert(BasicBlock* firstBlock);

    PhaseStatus optInvertLoops();    // Invert loops so they're entered at top and tested at bottom.
    PhaseStatus optOptimizeFlow();   // Simplify flow graph and do tail duplication
    PhaseStatus optOptimizeLayout(); // Optimize the BasicBlock layout of the method
    PhaseStatus optSetBlockWeights();
    PhaseStatus optFindLoopsPhase(); // Finds loops and records them in the loop table

    void optFindLoops();
    bool optCanonicalizeLoops();

    void optCompactLoops();
    void optCompactLoop(FlowGraphNaturalLoop* loop);
    BasicBlock* optFindLoopCompactionInsertionPoint(FlowGraphNaturalLoop* loop, BasicBlock* top);
    BasicBlock* optTryAdvanceLoopCompactionInsertionPoint(FlowGraphNaturalLoop* loop, BasicBlock* insertionPoint, BasicBlock* top, BasicBlock* bottom);
    bool optCreatePreheader(FlowGraphNaturalLoop* loop);
    void optSetPreheaderWeight(FlowGraphNaturalLoop* loop, BasicBlock* preheader);

    bool optCanonicalizeExits(FlowGraphNaturalLoop* loop);
    bool optCanonicalizeExit(FlowGraphNaturalLoop* loop, BasicBlock* exit);
    weight_t optEstimateEdgeLikelihood(BasicBlock* from, BasicBlock* to, bool* fromProfile);
    void optSetExitWeight(FlowGraphNaturalLoop* loop, BasicBlock* exit);

    PhaseStatus optCloneLoops();
    void optCloneLoop(FlowGraphNaturalLoop* loop, LoopCloneContext* context);
    PhaseStatus optUnrollLoops(); // Unrolls loops (needs to have cost info)
    bool optTryUnrollLoop(FlowGraphNaturalLoop* loop, bool* changedIR);
    void optRedirectPrevUnrollIteration(FlowGraphNaturalLoop* loop, BasicBlock* prevTestBlock, BasicBlock* target);
    void optReplaceScalarUsesWithConst(BasicBlock* block, unsigned lclNum, ssize_t cnsVal);
    void        optRemoveRedundantZeroInits();
    PhaseStatus optIfConversion(); // If conversion

public:
    bool fgHasLoops;
#ifdef DEBUG
    unsigned loopAlignCandidates; // number of candidates identified by placeLoopAlignInstructions
    unsigned loopsAligned;        // number of loops actually aligned
#endif                          // DEBUG

protected:
    unsigned optCallCount;         // number of calls made in the method
    unsigned optIndirectCallCount; // number of virtual, interface and indirect calls made in the method
    unsigned optNativeCallCount;   // number of Pinvoke/Native calls made in the method
    unsigned optLoopsCloned;       // number of loops cloned in the current method.

#ifdef DEBUG
    void optCheckPreds();
#endif

    void optResetLoopInfo();
    void optFindAndScaleGeneralLoopBlocks();

    // Determine if there are any potential loops, and set BBF_LOOP_HEAD on potential loop heads.
    void optMarkLoopHeads();

    void optScaleLoopBlocks(BasicBlock* begBlk, BasicBlock* endBlk);

    bool optIsLoopTestEvalIntoTemp(Statement* testStmt, Statement** newTestStmt);
    unsigned optIsLoopIncrTree(GenTree* incr);
    bool optExtractInitTestIncr(
        BasicBlock** pInitBlock, BasicBlock* bottom, BasicBlock* top, GenTree** ppInit, GenTree** ppTest, GenTree** ppIncr);

    enum class RedirectBlockOption
    {
        DoNotChangePredLists, // do not modify pred lists
        UpdatePredLists,      // add/remove to pred lists
        AddToPredLists,       // only add to pred lists
    };

    void optRedirectBlock(BasicBlock*      blk,
                          BlockToBlockMap* redirectMap,
                          const RedirectBlockOption = RedirectBlockOption::DoNotChangePredLists);

    // Marks the containsCall information to "loop" and any parent loops.
    void AddContainsCallAllContainingLoops(FlowGraphNaturalLoop* loop);

    // Adds the variable liveness information from 'blk' to "loop" and any parent loops.
    void AddVariableLivenessAllContainingLoops(FlowGraphNaturalLoop* loop, BasicBlock* blk);

    // Adds "fldHnd" to the set of modified fields of "loop" and any parent loops.
    void AddModifiedFieldAllContainingLoops(FlowGraphNaturalLoop* loop, CORINFO_FIELD_HANDLE fldHnd, FieldKindForVN fieldKind);

    // Adds "elemType" to the set of modified array element types of "loop" and any parent loops.
    void AddModifiedElemTypeAllContainingLoops(FlowGraphNaturalLoop* loop, CORINFO_CLASS_HANDLE elemType);

    // Struct used in optInvertWhileLoop to count interesting constructs to boost the profitability score.
    struct OptInvertCountTreeInfoType
    {
        int sharedStaticHelperCount;
        int arrayLengthCount;
    };

    OptInvertCountTreeInfoType optInvertCountTreeInfo(GenTree* tree);

    bool optInvertWhileLoop(BasicBlock* block);
    bool optIfConvert(BasicBlock* block);

private:
    static bool optIterSmallOverflow(int iterAtExit, var_types incrType);
    static bool optIterSmallUnderflow(int iterAtExit, var_types decrType);

    bool optComputeLoopRep(int        constInit,
                           int        constLimit,
                           int        iterInc,
                           genTreeOps iterOper,
                           var_types  iterType,
                           genTreeOps testOper,
                           bool       unsignedTest,
                           unsigned*  iterCount);

protected:
    bool optNarrowTree(GenTree* tree, var_types srct, var_types dstt, ValueNumPair vnpNarrow, bool doit);

    //  The following is the upper limit on how many expressions we'll keep track
    //  of for the CSE analysis.
    //
    static const unsigned MAX_CSE_CNT = EXPSET_SZ;

    static const int MIN_CSE_COST = 2;

    // BitVec trait information only used by the optCSE_canSwap() method, for the  CSE_defMask and CSE_useMask.
    // This BitVec uses one bit per CSE candidate
    BitVecTraits* cseMaskTraits; // one bit per CSE candidate

    // BitVec trait information for computing CSE availability using the CSE_DataFlow algorithm.
    // Two bits are allocated per CSE candidate to compute CSE availability
    // plus an extra bit to handle the initial unvisited case.
    // (See CSE_DataFlow::EndMerge for an explanation of why this is necessary.)
    //
    // The two bits per CSE candidate have the following meanings:
    //     11 - The CSE is available, and is also available when considering calls as killing availability.
    //     10 - The CSE is available, but is not available when considering calls as killing availability.
    //     00 - The CSE is not available
    //     01 - An illegal combination
    //
    BitVecTraits* cseLivenessTraits;

    //-----------------------------------------------------------------------------------------------------------------
    // getCSEnum2bit: Return the normalized index to use in the EXPSET_TP for the CSE with the given CSE index.
    // Each GenTree has a `gtCSEnum` field. Zero is reserved to mean this node is not a CSE, positive values indicate
    // CSE uses, and negative values indicate CSE defs. The caller must pass a non-zero positive value, as from
    // GET_CSE_INDEX().
    //
    static unsigned genCSEnum2bit(unsigned CSEnum)
    {
        assert((CSEnum > 0) && (CSEnum <= MAX_CSE_CNT));
        return CSEnum - 1;
    }

    //-----------------------------------------------------------------------------------------------------------------
    // getCSEAvailBit: Return the bit used by CSE dataflow sets (bbCseGen, etc.) for the availability bit for a CSE.
    //
    static unsigned getCSEAvailBit(unsigned CSEnum)
    {
        return genCSEnum2bit(CSEnum) * 2;
    }

    //-----------------------------------------------------------------------------------------------------------------
    // getCSEAvailCrossCallBit: Return the bit used by CSE dataflow sets (bbCseGen, etc.) for the availability bit
    // for a CSE considering calls as killing availability bit (see description above).
    //
    static unsigned getCSEAvailCrossCallBit(unsigned CSEnum)
    {
        return getCSEAvailBit(CSEnum) + 1;
    }

    void optPrintCSEDataFlowSet(EXPSET_VALARG_TP cseDataFlowSet, bool includeBits = true);

    EXPSET_TP cseCallKillsMask; // Computed once - A mask that is used to kill available CSEs at callsites

    static const size_t s_optCSEhashSizeInitial;
    static const size_t s_optCSEhashGrowthFactor;
    static const size_t s_optCSEhashBucketSize;
    size_t              optCSEhashSize;                 // The current size of hashtable
    size_t              optCSEhashCount;                // Number of entries in hashtable
    size_t              optCSEhashMaxCountBeforeResize; // Number of entries before resize
    CSEdsc**            optCSEhash;
    CSEdsc**            optCSEtab;

    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, GenTree*> NodeToNodeMap;

    NodeToNodeMap* optCseCheckedBoundMap; // Maps bound nodes to ancestor compares that should be
                                          // re-numbered with the bound to improve range check elimination

    // Given a compare, look for a cse candidate checked bound feeding it and add a map entry if found.
    void optCseUpdateCheckedBoundMap(GenTree* compare);

    void optCSEstop();

    CSEdsc* optCSEfindDsc(unsigned index);
    bool optUnmarkCSE(GenTree* tree);

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
    void optCSE_GetMaskData(GenTree* tree, optCSE_MaskData* pMaskData);

    // Given a binary tree node return true if it is safe to swap the order of evaluation for op1 and op2.
    bool optCSE_canSwap(GenTree* firstNode, GenTree* secondNode);

    struct optCSEcostCmpEx
    {
        bool operator()(const CSEdsc* op1, const CSEdsc* op2);
    };
    struct optCSEcostCmpSz
    {
        bool operator()(const CSEdsc* op1, const CSEdsc* op2);
    };

    void optCleanupCSEs();

#ifdef DEBUG
    void optEnsureClearCSEInfo();
#endif // DEBUG

    static bool Is_Shared_Const_CSE(size_t key)
    {
        return ((key & TARGET_SIGN_BIT) != 0);
    }

    // returns the encoded key
    static size_t Encode_Shared_Const_CSE_Value(size_t key)
    {
        return TARGET_SIGN_BIT | (key >> CSE_CONST_SHARED_LOW_BITS);
    }

    // returns the original key
    static size_t Decode_Shared_Const_CSE_Value(size_t enckey)
    {
        assert(Is_Shared_Const_CSE(enckey));
        return (enckey & ~TARGET_SIGN_BIT) << CSE_CONST_SHARED_LOW_BITS;
    }

/**************************************************************************
 *                   Value Number based CSEs
 *************************************************************************/

// String to use for formatting CSE numbers. Note that this is the positive number, e.g., from GET_CSE_INDEX().
#define FMT_CSE "CSE #%02u"

public:
    PhaseStatus optOptimizeValnumCSEs();

    // some phases (eg hoisting) need to anticipate
    // what CSE will do
    CSE_HeuristicCommon* optGetCSEheuristic();

protected:
    void     optValnumCSE_Init();
    unsigned optValnumCSE_Index(GenTree* tree, Statement* stmt);
    bool optValnumCSE_Locate(CSE_HeuristicCommon* heuristic);
    void optValnumCSE_InitDataFlow();
    void optValnumCSE_DataFlow();
    void optValnumCSE_Availability();
    void optValnumCSE_Heuristic(CSE_HeuristicCommon* heuristic);

    bool     optDoCSE;             // True when we have found a duplicate CSE tree
    bool     optValnumCSE_phase;   // True when we are executing the optOptimizeValnumCSEs() phase
    unsigned optCSECandidateCount; // Count of CSE candidates
    unsigned optCSEstart;          // The first local variable number that is a CSE
    unsigned optCSEattempt;        // The number of CSEs attempted so far.
    unsigned optCSEcount;          // The total count of CSEs introduced.
    weight_t optCSEweight;         // The weight of the current block when we are doing PerformCSE
    CSE_HeuristicCommon* optCSEheuristic; // CSE Heuristic to use for this method

    bool optIsCSEcandidate(GenTree* tree, bool isReturn = false);

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
        return lvaGetDesc(lclNum)->lvIsCSE;
    }

#ifdef DEBUG
    bool optConfigDisableCSE();
    bool optConfigDisableCSE2();
#endif

    void optOptimizeCSEs();

public:
    // VN based copy propagation.

    // In DEBUG builds, we'd like to know the tree that the SSA definition was pushed for.
    // While for ordinary SSA defs it will be available (as a store) in the SSA descriptor,
    // for locals which will use "definitions from uses", it will not be, so we store it
    // in this class instead.
    class CopyPropSsaDef
    {
        LclSsaVarDsc* m_ssaDef;
#ifdef DEBUG
        GenTree* m_defNode;
#endif
    public:
        CopyPropSsaDef(LclSsaVarDsc* ssaDef, GenTree* defNode)
            : m_ssaDef(ssaDef)
#ifdef DEBUG
            , m_defNode(defNode)
#endif
        {
        }

        LclSsaVarDsc* GetSsaDef() const
        {
            return m_ssaDef;
        }

#ifdef DEBUG
        GenTree* GetDefNode() const
        {
            return m_defNode;
        }
#endif
    };

    typedef ArrayStack<CopyPropSsaDef> CopyPropSsaDefStack;
    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, CopyPropSsaDefStack*> LclNumToLiveDefsMap;

    // Copy propagation functions.
    bool optCopyProp(BasicBlock*          block,
                     Statement*           stmt,
                     GenTreeLclVarCommon* tree,
                     unsigned             lclNum,
                     LclNumToLiveDefsMap* curSsaName);
    void optBlockCopyPropPopStacks(BasicBlock* block, LclNumToLiveDefsMap* curSsaName);
    bool optBlockCopyProp(BasicBlock* block, LclNumToLiveDefsMap* curSsaName);
    void optCopyPropPushDef(GenTree* defNode, GenTreeLclVarCommon* lclNode, LclNumToLiveDefsMap* curSsaName);
    int optCopyProp_LclVarScore(const LclVarDsc* lclVarDsc, const LclVarDsc* copyVarDsc, bool preferOp2);
    PhaseStatus optVnCopyProp();
    INDEBUG(void optDumpCopyPropStack(LclNumToLiveDefsMap* curSsaName));

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

    PhaseStatus optVNBasedDeadStoreRemoval();

// clang-format off

#define OMF_HAS_NEWARRAY                       0x00000001 // Method contains 'new' of an SD array
#define OMF_HAS_NEWOBJ                         0x00000002 // Method contains 'new' of an object type.
#define OMF_HAS_ARRAYREF                       0x00000004 // Method contains array element loads or stores.
#define OMF_HAS_NULLCHECK                      0x00000008 // Method contains null check.
#define OMF_HAS_FATPOINTER                     0x00000010 // Method contains call, that needs fat pointer transformation.
#define OMF_HAS_OBJSTACKALLOC                  0x00000020 // Method contains an object allocated on the stack.
#define OMF_HAS_GUARDEDDEVIRT                  0x00000040 // Method contains guarded devirtualization candidate
#define OMF_HAS_EXPRUNTIMELOOKUP               0x00000080 // Method contains a runtime lookup to an expandable dictionary.
#define OMF_HAS_PATCHPOINT                     0x00000100 // Method contains patchpoints
#define OMF_NEEDS_GCPOLLS                      0x00000200 // Method needs GC polls
#define OMF_HAS_FROZEN_OBJECTS                 0x00000400 // Method has frozen objects (REF constant int)
#define OMF_HAS_PARTIAL_COMPILATION_PATCHPOINT 0x00000800 // Method contains partial compilation patchpoints
#define OMF_HAS_TAILCALL_SUCCESSOR             0x00001000 // Method has potential tail call in a non BBJ_RETURN block
#define OMF_HAS_MDNEWARRAY                     0x00002000 // Method contains 'new' of an MD array
#define OMF_HAS_MDARRAYREF                     0x00004000 // Method contains multi-dimensional intrinsic array element loads or stores.
#define OMF_HAS_STATIC_INIT                    0x00008000 // Method has static initializations we might want to partially inline
#define OMF_HAS_TLS_FIELD                      0x00010000 // Method contains TLS field access
#define OMF_HAS_SPECIAL_INTRINSICS             0x00020000 // Method contains special intrinsics expanded in late phases
#define OMF_HAS_RECURSIVE_TAILCALL             0x00040000 // Method contains recursive tail call
#define OMF_HAS_EXPANDABLE_CAST                0x00080000 // Method contains casts eligible for late expansion

    // clang-format on

    bool doesMethodHaveFatPointer()
    {
        return (optMethodFlags & OMF_HAS_FATPOINTER) != 0;
    }

    void setMethodHasFatPointer()
    {
        optMethodFlags |= OMF_HAS_FATPOINTER;
    }

    void clearMethodHasFatPointer()
    {
        optMethodFlags &= ~OMF_HAS_FATPOINTER;
    }

    void addFatPointerCandidate(GenTreeCall* call);

    bool doesMethodHaveFrozenObjects() const
    {
        return (optMethodFlags & OMF_HAS_FROZEN_OBJECTS) != 0;
    }

    void setMethodHasFrozenObjects()
    {
        optMethodFlags |= OMF_HAS_FROZEN_OBJECTS;
    }

    bool doesMethodHaveStaticInit()
    {
        return (optMethodFlags & OMF_HAS_STATIC_INIT) != 0;
    }

    void setMethodHasStaticInit()
    {
        optMethodFlags |= OMF_HAS_STATIC_INIT;
    }

    bool doesMethodHaveExpandableCasts()
    {
        return (optMethodFlags & OMF_HAS_EXPANDABLE_CAST) != 0;
    }

    void setMethodHasExpandableCasts()
    {
        optMethodFlags |= OMF_HAS_EXPANDABLE_CAST;
    }

    bool doesMethodHaveGuardedDevirtualization() const
    {
        return (optMethodFlags & OMF_HAS_GUARDEDDEVIRT) != 0;
    }

    void setMethodHasGuardedDevirtualization()
    {
        optMethodFlags |= OMF_HAS_GUARDEDDEVIRT;
    }

    bool methodHasTlsFieldAccess()
    {
        return (optMethodFlags & OMF_HAS_TLS_FIELD) != 0;
    }

    void setMethodHasTlsFieldAccess()
    {
        optMethodFlags |= OMF_HAS_TLS_FIELD;
    }

    bool doesMethodHaveSpecialIntrinsics()
    {
        return (optMethodFlags & OMF_HAS_SPECIAL_INTRINSICS) != 0;
    }

    void setMethodHasSpecialIntrinsics()
    {
        optMethodFlags |= OMF_HAS_SPECIAL_INTRINSICS;
    }

    bool doesMethodHaveRecursiveTailcall()
    {
        return (optMethodFlags & OMF_HAS_RECURSIVE_TAILCALL) != 0;
    }

    void setMethodHasRecursiveTailcall()
    {
        optMethodFlags |= OMF_HAS_RECURSIVE_TAILCALL;
    }

    void pickGDV(GenTreeCall*           call,
                 IL_OFFSET              ilOffset,
                 bool                   isInterface,
                 CORINFO_CLASS_HANDLE*  classGuesses,
                 CORINFO_METHOD_HANDLE* methodGuesses,
                 int*                   candidatesCount,
                 unsigned*              likelihoods);

    void considerGuardedDevirtualization(GenTreeCall*            call,
                                         IL_OFFSET               ilOffset,
                                         bool                    isInterface,
                                         CORINFO_METHOD_HANDLE   baseMethod,
                                         CORINFO_CLASS_HANDLE    baseClass,
                                         CORINFO_CONTEXT_HANDLE* pContextHandle);

    bool isCompatibleMethodGDV(GenTreeCall* call, CORINFO_METHOD_HANDLE gdvTarget);

    void addGuardedDevirtualizationCandidate(GenTreeCall*           call,
                                             CORINFO_METHOD_HANDLE  methodHandle,
                                             CORINFO_CLASS_HANDLE   classHandle,
                                             CORINFO_CONTEXT_HANDLE contextHandle,
                                             unsigned               methodAttr,
                                             unsigned               classAttr,
                                             unsigned               likelihood);

    int getGDVMaxTypeChecks()
    {
        int typeChecks = JitConfig.JitGuardedDevirtualizationMaxTypeChecks();
        if (typeChecks < 0)
        {
            // Negative value means "it's up to JIT to decide"
            if (IsTargetAbi(CORINFO_NATIVEAOT_ABI) && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_SIZE_OPT))
            {
                return 3;
            }

            // We plan to use 3 for CoreCLR too, but we need to make sure it doesn't regress performance
            // as CoreCLR heavily relies on Dynamic PGO while for NativeAOT we *usually* don't have it and
            // can only perform the "exact" devirtualization.
            return 1;
        }

        // MAX_GDV_TYPE_CHECKS is the upper limit. The constant can be changed, we just suspect that even
        // 4 type checks is already too much.
        return min(MAX_GDV_TYPE_CHECKS, typeChecks);
    }

    bool doesMethodHaveExpRuntimeLookup()
    {
        return (optMethodFlags & OMF_HAS_EXPRUNTIMELOOKUP) != 0;
    }

    void setMethodHasExpRuntimeLookup()
    {
        optMethodFlags |= OMF_HAS_EXPRUNTIMELOOKUP;
    }

    bool doesMethodHavePatchpoints()
    {
        return (optMethodFlags & OMF_HAS_PATCHPOINT) != 0;
    }

    void setMethodHasPatchpoint()
    {
        optMethodFlags |= OMF_HAS_PATCHPOINT;
    }

    bool doesMethodHavePartialCompilationPatchpoints()
    {
        return (optMethodFlags & OMF_HAS_PARTIAL_COMPILATION_PATCHPOINT) != 0;
    }

    void setMethodHasPartialCompilationPatchpoint()
    {
        optMethodFlags |= OMF_HAS_PARTIAL_COMPILATION_PATCHPOINT;
    }

    unsigned optMethodFlags;

    bool doesMethodHaveNoReturnCalls()
    {
        return optNoReturnCallCount > 0;
    }

    void setMethodHasNoReturnCalls()
    {
        optNoReturnCallCount++;
    }

    unsigned optNoReturnCallCount;

    // Recursion bound controls how far we can go backwards tracking for a SSA value.
    // No throughput diff was found with backward walk bound between 3-8.
    static const int optEarlyPropRecurBound = 5;

    enum class optPropKind
    {
        OPK_INVALID,
        OPK_ARRAYLEN,
        OPK_NULLCHECK
    };

    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, GenTree*> LocalNumberToNullCheckTreeMap;

    GenTree* getArrayLengthFromAllocation(GenTree* tree DEBUGARG(BasicBlock* block));
    GenTree* optPropGetValueRec(unsigned lclNum, unsigned ssaNum, optPropKind valueKind, int walkDepth);
    GenTree* optPropGetValue(unsigned lclNum, unsigned ssaNum, optPropKind valueKind);
    GenTree* optEarlyPropRewriteTree(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap);
    bool optDoEarlyPropForBlock(BasicBlock* block);
    bool        optDoEarlyPropForFunc();
    PhaseStatus optEarlyProp();
    bool optFoldNullCheck(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap);
    GenTree* optFindNullCheckToFold(GenTree* tree, LocalNumberToNullCheckTreeMap* nullCheckMap);
    bool optIsNullCheckFoldingLegal(GenTree*    tree,
                                    GenTree*    nullCheckTree,
                                    GenTree**   nullCheckParent,
                                    Statement** nullCheckStmt);
    bool optCanMoveNullCheckPastTree(GenTree* tree,
                                     unsigned nullCheckLclNum,
                                     bool     isInsideTry,
                                     bool     checkSideEffectSummary);
#if DEBUG
    void optCheckFlagsAreSet(unsigned    methodFlag,
                             const char* methodFlagStr,
                             unsigned    bbFlag,
                             const char* bbFlagStr,
                             GenTree*    tree,
                             BasicBlock* basicBlock);
#endif

    PhaseStatus optInductionVariables();
    bool optCanSinkWidenedIV(unsigned lclNum, FlowGraphNaturalLoop* loop);
    bool optIsIVWideningProfitable(unsigned lclNum, struct ScevAddRec* addRec, FlowGraphNaturalLoop* loop);
    void optReplaceWidenedIV(unsigned lclNum, unsigned newLclNum, Statement* stmt);
    bool optSinkWidenedIV(unsigned lclNum, unsigned newLclNum, FlowGraphNaturalLoop* loop);

    // Redundant branch opts
    //
    PhaseStatus optRedundantBranches();
    bool optRedundantRelop(BasicBlock* const block);
    bool optRedundantBranch(BasicBlock* const block);
    bool optJumpThreadDom(BasicBlock* const block, BasicBlock* const domBlock, bool domIsSameRelop);
    bool optJumpThreadPhi(BasicBlock* const block, GenTree* tree, ValueNum treeNormVN);
    bool optJumpThreadCheck(BasicBlock* const block, BasicBlock* const domBlock);
    bool optJumpThreadCore(JumpThreadInfo& jti);
    bool optReachable(BasicBlock* const fromBlock, BasicBlock* const toBlock, BasicBlock* const excludedBlock);
    BitVecTraits* optReachableBitVecTraits;
    BitVec        optReachableBitVec;
    void optRelopImpliesRelop(RelopImplicationInfo* rii);

    /**************************************************************************
     *               Value/Assertion propagation
     *************************************************************************/
public:
    // Data structures for assertion prop
    BitVecTraits* apTraits;
    ASSERT_TP     apFull;
    ASSERT_TP     apLocal;
    ASSERT_TP     apLocalIfTrue;

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
        O1K_BOUND_OPER_BND,
        O1K_BOUND_LOOP_BND,
        O1K_CONSTANT_LOOP_BND,
        O1K_CONSTANT_LOOP_BND_UN,
        O1K_EXACT_TYPE,
        O1K_SUBTYPE,
        O1K_VALUE_NUMBER,
        O1K_COUNT
    };

    enum optOp2Kind : uint16_t
    {
        O2K_INVALID,
        O2K_LCLVAR_COPY,
        O2K_IND_CNS_INT,
        O2K_CONST_INT,
        O2K_CONST_LONG,
        O2K_CONST_DOUBLE,
        O2K_ZEROOBJ,
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
        private:
            uint16_t m_encodedIconFlags; // encoded icon gtFlags, don't use directly
        public:
            ValueNum vn;
            struct IntVal
            {
                ssize_t iconVal; // integer
#if !defined(HOST_64BIT)
                unsigned padding; // unused; ensures iconFlags does not overlap lconVal
#endif
                FieldSeq* fieldSeq;
            };
            union {
                SsaVar        lcl;
                IntVal        u1;
                __int64       lconVal;
                double        dconVal;
                IntegralRange u2;
            };

            bool HasIconFlag()
            {
                assert(m_encodedIconFlags <= 0xFF);
                return m_encodedIconFlags != 0;
            }
            GenTreeFlags GetIconFlag()
            {
                // number of trailing zeros in GTF_ICON_HDL_MASK
                const uint16_t iconMaskTzc = 24;
                static_assert_no_msg((0xFF000000 == GTF_ICON_HDL_MASK) && (GTF_ICON_HDL_MASK >> iconMaskTzc) == 0xFF);

                GenTreeFlags flags = (GenTreeFlags)(m_encodedIconFlags << iconMaskTzc);
                assert((flags & ~GTF_ICON_HDL_MASK) == 0);
                return flags;
            }
            void SetIconFlag(GenTreeFlags flags, FieldSeq* fieldSeq = nullptr)
            {
                const uint16_t iconMaskTzc = 24;
                assert((flags & ~GTF_ICON_HDL_MASK) == 0);
                m_encodedIconFlags = flags >> iconMaskTzc;
                u1.fieldSeq        = fieldSeq;
            }
        } op2;

        bool IsCheckedBoundArithBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) && op1.kind == O1K_BOUND_OPER_BND);
        }
        bool IsCheckedBoundBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) && op1.kind == O1K_BOUND_LOOP_BND);
        }
        bool IsConstantBound()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) &&
                    (op1.kind == O1K_CONSTANT_LOOP_BND));
        }
        bool IsConstantBoundUnsigned()
        {
            return ((assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL) &&
                    (op1.kind == O1K_CONSTANT_LOOP_BND_UN));
        }
        bool IsBoundsCheckNoThrow()
        {
            return ((assertionKind == OAK_NO_THROW) && (op1.kind == O1K_ARR_BND));
        }

        bool IsCopyAssertion()
        {
            return ((assertionKind == OAK_EQUAL) && (op1.kind == O1K_LCLVAR) && (op2.kind == O2K_LCLVAR_COPY));
        }

        bool IsConstantInt32Assertion()
        {
            return ((assertionKind == OAK_EQUAL) || (assertionKind == OAK_NOT_EQUAL)) && (op2.kind == O2K_CONST_INT);
        }

        bool CanPropLclVar()
        {
            return assertionKind == OAK_EQUAL && op1.kind == O1K_LCLVAR;
        }

        bool CanPropEqualOrNotEqual()
        {
            return assertionKind == OAK_EQUAL || assertionKind == OAK_NOT_EQUAL;
        }

        bool CanPropNonNull()
        {
            return assertionKind == OAK_NOT_EQUAL && op2.vn == ValueNumStore::VNForNull();
        }

        bool CanPropBndsCheck()
        {
            return op1.kind == O1K_ARR_BND;
        }

        bool CanPropSubRange()
        {
            return assertionKind == OAK_SUBRANGE && op1.kind == O1K_LCLVAR;
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

        bool HasSameOp1(AssertionDsc* that, bool vnBased)
        {
            if (op1.kind != that->op1.kind)
            {
                return false;
            }
            else if (op1.kind == O1K_ARR_BND)
            {
                assert(vnBased);
                return (op1.bnd.vnIdx == that->op1.bnd.vnIdx) && (op1.bnd.vnLen == that->op1.bnd.vnLen);
            }
            else
            {
                return ((vnBased && (op1.vn == that->op1.vn)) ||
                        (!vnBased && (op1.lcl.lclNum == that->op1.lcl.lclNum)));
            }
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
                    return ((op2.u1.iconVal == that->op2.u1.iconVal) && (op2.GetIconFlag() == that->op2.GetIconFlag()));

                case O2K_CONST_LONG:
                    return (op2.lconVal == that->op2.lconVal);

                case O2K_CONST_DOUBLE:
                    // exact match because of positive and negative zero.
                    return (memcmp(&op2.dconVal, &that->op2.dconVal, sizeof(double)) == 0);

                case O2K_ZEROOBJ:
                    return true;

                case O2K_LCLVAR_COPY:
                    return (op2.lcl.lclNum == that->op2.lcl.lclNum) &&
                           (!vnBased || (op2.lcl.ssaNum == that->op2.lcl.ssaNum));

                case O2K_SUBRANGE:
                    return op2.u2.Equals(that->op2.u2);

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
            if (assertionKind != that->assertionKind)
            {
                return false;
            }
            else if (assertionKind == OAK_NO_THROW)
            {
                assert(op2.kind == O2K_INVALID);
                return HasSameOp1(that, vnBased);
            }
            else
            {
                return HasSameOp1(that, vnBased) && HasSameOp2(that, vnBased);
            }
        }
    };

protected:
    static fgWalkPreFn optVNAssertionPropCurStmtVisitor;

    bool optLocalAssertionProp;  // indicates that we are performing local assertion prop
    bool optAssertionPropagated; // set to true if we modified the trees
    bool optAssertionPropagatedCurrentStmt;
#ifdef DEBUG
    GenTree* optAssertionPropCurrentTree;
#endif
    AssertionIndex*            optComplementaryAssertionMap;
    JitExpandArray<ASSERT_TP>* optAssertionDep; // table that holds dependent assertions (assertions
                                                // using the value of a local var) for each local var
    AssertionDsc*  optAssertionTabPrivate;      // table that holds info about value assignments
    AssertionIndex optAssertionCount;           // total number of assertions in the assertion table
    AssertionIndex optMaxAssertionCount;
    bool           optCrossBlockLocalAssertionProp;
    unsigned       optAssertionOverflow;
    bool           optCanPropLclVar;
    bool           optCanPropEqual;
    bool           optCanPropNonNull;
    bool           optCanPropBndsChk;
    bool           optCanPropSubRange;

public:
    void optVnNonNullPropCurStmt(BasicBlock* block, Statement* stmt, GenTree* tree);
    fgWalkResult optVNConstantPropCurStmt(BasicBlock* block, Statement* stmt, GenTree* parent, GenTree* tree);
    GenTree* optVNConstantPropOnJTrue(BasicBlock* block, GenTree* test);
    GenTree* optVNConstantPropOnTree(BasicBlock* block, GenTree* parent, GenTree* tree);
    GenTree* optExtractSideEffListFromConst(GenTree* tree);

    AssertionIndex GetAssertionCount()
    {
        return optAssertionCount;
    }
    ASSERT_TP* bbJtrueAssertionOut;
    typedef JitHashTable<ValueNum, JitSmallPrimitiveKeyFuncs<ValueNum>, ASSERT_TP> ValueNumToAssertsMap;
    ValueNumToAssertsMap* optValueNumToAsserts;

    // Assertion prop helpers.
    ASSERT_TP& GetAssertionDep(unsigned lclNum);
    AssertionDsc* optGetAssertion(AssertionIndex assertIndex);
    void optAssertionInit(bool isLocalProp);
    void optAssertionTraitsInit(AssertionIndex assertionCount);
    void optAssertionReset(AssertionIndex limit);
    void optAssertionRemove(AssertionIndex index);

    // Assertion prop data flow functions.
    PhaseStatus optAssertionPropMain();
    Statement* optVNAssertionPropCurStmt(BasicBlock* block, Statement* stmt);
    bool optIsTreeKnownIntValue(bool vnBased, GenTree* tree, ssize_t* pConstant, GenTreeFlags* pIconFlags);
    ASSERT_TP* optInitAssertionDataflowFlags();
    ASSERT_TP* optComputeAssertionGen();

    // Assertion Gen functions.
    void optAssertionGen(GenTree* tree);
    AssertionIndex optAssertionGenCast(GenTreeCast* cast);
    AssertionIndex optAssertionGenPhiDefn(GenTree* tree);
    AssertionInfo optCreateJTrueBoundsAssertion(GenTree* tree);
    AssertionInfo optAssertionGenJtrue(GenTree* tree);
    AssertionIndex optCreateJtrueAssertions(GenTree*                   op1,
                                            GenTree*                   op2,
                                            Compiler::optAssertionKind assertionKind,
                                            bool                       helperCallArgs = false);
    AssertionIndex optFindComplementary(AssertionIndex assertionIndex);
    void optMapComplementary(AssertionIndex assertionIndex, AssertionIndex index);

    ValueNum optConservativeNormalVN(GenTree* tree);

    ssize_t optCastConstantSmall(ssize_t iconVal, var_types smallType);

    // Assertion creation functions.
    AssertionIndex optCreateAssertion(GenTree*         op1,
                                      GenTree*         op2,
                                      optAssertionKind assertionKind,
                                      bool             helperCallArgs = false);

    AssertionIndex optFinalizeCreatingAssertion(AssertionDsc* assertion);

    bool optTryExtractSubrangeAssertion(GenTree* source, IntegralRange* pRange);

    void optCreateComplementaryAssertion(AssertionIndex assertionIndex,
                                         GenTree*       op1,
                                         GenTree*       op2,
                                         bool           helperCallArgs = false);

    bool optAssertionVnInvolvesNan(AssertionDsc* assertion);
    AssertionIndex optAddAssertion(AssertionDsc* assertion);
    void optAddVnAssertionMapping(ValueNum vn, AssertionIndex index);
#ifdef DEBUG
    void optPrintVnAssertionMapping();
#endif
    ASSERT_TP optGetVnMappedAssertions(ValueNum vn);

    // Used for respective assertion propagations.
    AssertionIndex optAssertionIsSubrange(GenTree* tree, IntegralRange range, ASSERT_VALARG_TP assertions);
    AssertionIndex optAssertionIsSubtype(GenTree* tree, GenTree* methodTableArg, ASSERT_VALARG_TP assertions);
    AssertionIndex optAssertionIsNonNullInternal(GenTree* op, ASSERT_VALARG_TP assertions DEBUGARG(bool* pVnBased));
    bool optAssertionIsNonNull(GenTree*         op,
                               ASSERT_VALARG_TP assertions DEBUGARG(bool* pVnBased) DEBUGARG(AssertionIndex* pIndex));

    AssertionIndex optGlobalAssertionIsEqualOrNotEqual(ASSERT_VALARG_TP assertions, GenTree* op1, GenTree* op2);
    AssertionIndex optGlobalAssertionIsEqualOrNotEqualZero(ASSERT_VALARG_TP assertions, GenTree* op1);
    AssertionIndex optLocalAssertionIsEqualOrNotEqual(
        optOp1Kind op1Kind, unsigned lclNum, optOp2Kind op2Kind, ssize_t cnsVal, ASSERT_VALARG_TP assertions);

    // Assertion prop for lcl var functions.
    bool optAssertionProp_LclVarTypeCheck(GenTree* tree, LclVarDsc* lclVarDsc, LclVarDsc* copyVarDsc);
    GenTree* optCopyAssertionProp(AssertionDsc*        curAssertion,
                                  GenTreeLclVarCommon* tree,
                                  Statement* stmt DEBUGARG(AssertionIndex index));
    GenTree* optConstantAssertionProp(AssertionDsc*        curAssertion,
                                      GenTreeLclVarCommon* tree,
                                      Statement* stmt DEBUGARG(AssertionIndex index));
    bool optIsProfitableToSubstitute(GenTree* dest, BasicBlock* destBlock, GenTree* destParent, GenTree* value);
    bool optZeroObjAssertionProp(GenTree* tree, ASSERT_VALARG_TP assertions);

    // Assertion propagation functions.
    GenTree* optAssertionProp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt, BasicBlock* block);
    GenTree* optAssertionProp_LclVar(ASSERT_VALARG_TP assertions, GenTreeLclVarCommon* tree, Statement* stmt);
    GenTree* optAssertionProp_LclFld(ASSERT_VALARG_TP assertions, GenTreeLclVarCommon* tree, Statement* stmt);
    GenTree* optAssertionProp_LocalStore(ASSERT_VALARG_TP assertions, GenTreeLclVarCommon* store, Statement* stmt);
    GenTree* optAssertionProp_BlockStore(ASSERT_VALARG_TP assertions, GenTreeBlk* store, Statement* stmt);
    GenTree* optAssertionProp_ModDiv(ASSERT_VALARG_TP assertions, GenTreeOp* tree, Statement* stmt);
    GenTree* optAssertionProp_Return(ASSERT_VALARG_TP assertions, GenTreeUnOp* ret, Statement* stmt);
    GenTree* optAssertionProp_Ind(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionProp_Cast(ASSERT_VALARG_TP assertions, GenTreeCast* cast, Statement* stmt);
    GenTree* optAssertionProp_Call(ASSERT_VALARG_TP assertions, GenTreeCall* call, Statement* stmt);
    GenTree* optAssertionProp_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionProp_Comma(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionProp_BndsChk(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionPropGlobal_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionPropLocal_RelOp(ASSERT_VALARG_TP assertions, GenTree* tree, Statement* stmt);
    GenTree* optAssertionProp_Update(GenTree* newTree, GenTree* tree, Statement* stmt);
    GenTree* optNonNullAssertionProp_Call(ASSERT_VALARG_TP assertions, GenTreeCall* call);
    bool optNonNullAssertionProp_Ind(ASSERT_VALARG_TP assertions, GenTree* indir);
    bool optWriteBarrierAssertionProp_StoreInd(ASSERT_VALARG_TP assertions, GenTreeStoreInd* indir);

    void optAssertionProp_RangeProperties(ASSERT_VALARG_TP assertions,
                                          GenTree*         tree,
                                          bool*            isKnownNonZero,
                                          bool*            isKnownNonNegative);

    // Implied assertion functions.
    void optImpliedAssertions(AssertionIndex assertionIndex, ASSERT_TP& activeAssertions);
    void optImpliedByTypeOfAssertions(ASSERT_TP& activeAssertions);
    void optImpliedByCopyAssertion(AssertionDsc* copyAssertion, AssertionDsc* depAssertion, ASSERT_TP& result);
    void optImpliedByConstAssertion(AssertionDsc* curAssertion, ASSERT_TP& result);

#ifdef DEBUG
    void optPrintAssertion(AssertionDsc* newAssertion, AssertionIndex assertionIndex = 0);
    void optPrintAssertionIndex(AssertionIndex index);
    void optPrintAssertionIndices(ASSERT_TP assertions);
    void optDebugCheckAssertion(AssertionDsc* assertion);
    void optDebugCheckAssertions(AssertionIndex AssertionIndex);
#endif

    static void optDumpAssertionIndices(const char* header, ASSERT_TP assertions, const char* footer = nullptr);
    static void optDumpAssertionIndices(ASSERT_TP assertions, const char* footer = nullptr);

    /**************************************************************************
     *                          Range checks
     *************************************************************************/

public:
    struct LoopCloneVisitorInfo
    {
        LoopCloneContext*     context;
        Statement*            stmt;
        FlowGraphNaturalLoop* loop;
        const bool            cloneForArrayBounds;
        const bool            cloneForGDVTests;
        LoopCloneVisitorInfo(LoopCloneContext*     context,
                             FlowGraphNaturalLoop* loop,
                             Statement*            stmt,
                             bool                  cloneForArrayBounds,
                             bool                  cloneForGDVTests)
            : context(context)
            , stmt(nullptr)
            , loop(loop)
            , cloneForArrayBounds(cloneForArrayBounds)
            , cloneForGDVTests(cloneForGDVTests)
        {
        }
    };

    bool optIsStackLocalInvariant(FlowGraphNaturalLoop* loop, unsigned lclNum);
    bool optExtractArrIndex(GenTree* tree, ArrIndex* result, unsigned lhsNum, bool* topLevelIsFinal);
    bool optReconstructArrIndexHelp(GenTree* tree, ArrIndex* result, unsigned lhsNum, bool* topLevelIsFinal);
    bool optReconstructArrIndex(GenTree* tree, ArrIndex* result);
    bool optIdentifyLoopOptInfo(FlowGraphNaturalLoop* loop, LoopCloneContext* context);
    static fgWalkPreFn optCanOptimizeByLoopCloningVisitor;
    fgWalkResult optCanOptimizeByLoopCloning(GenTree* tree, LoopCloneVisitorInfo* info);
    bool optObtainLoopCloningOpts(LoopCloneContext* context);
    bool optIsLoopClonable(FlowGraphNaturalLoop* loop, LoopCloneContext* context);
    bool optCheckLoopCloningGDVTestProfitable(GenTreeOp* guard, LoopCloneVisitorInfo* info);
    bool optIsHandleOrIndirOfHandle(GenTree* tree, GenTreeFlags handleType);

    static bool optLoopCloningEnabled();

#ifdef DEBUG
    void optDebugLogLoopCloning(BasicBlock* block, Statement* insertBefore);
#endif
    void optPerformStaticOptimizations(FlowGraphNaturalLoop* loop, LoopCloneContext* context DEBUGARG(bool fastPath));
    bool optComputeDerefConditions(FlowGraphNaturalLoop* loop, LoopCloneContext* context);
    bool optDeriveLoopCloningConditions(FlowGraphNaturalLoop* loop, LoopCloneContext* context);
    BasicBlock* optInsertLoopChoiceConditions(LoopCloneContext*     context,
                                              FlowGraphNaturalLoop* loop,
                                              BasicBlock*           slowHead,
                                              BasicBlock*           insertAfter);

protected:
    ssize_t optGetArrayRefScaleAndIndex(GenTree* mul, GenTree** pIndex DEBUGARG(bool bRngChk));

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
    regNumber raUpdateRegStateForArg(RegState* regState, LclVarDsc* argDsc);

    void raMarkStkVars();

#if FEATURE_PARTIAL_SIMD_CALLEE_SAVE
#if defined(TARGET_AMD64)
    static bool varTypeNeedsPartialCalleeSave(var_types type)
    {
        assert(type != TYP_STRUCT);
        return (type == TYP_SIMD32) || (type == TYP_SIMD64);
    }
#elif defined(TARGET_ARM64)
    static bool varTypeNeedsPartialCalleeSave(var_types type)
    {
        assert(type != TYP_STRUCT);
        // ARM64 ABI FP Callee save registers only require Callee to save lower 8 Bytes
        // For SIMD types longer than 8 bytes Caller is responsible for saving and restoring Upper bytes.
        return ((type == TYP_SIMD16) || (type == TYP_SIMD12));
    }
#else // !defined(TARGET_AMD64) && !defined(TARGET_ARM64)
#error("Unknown target architecture for FEATURE_PARTIAL_SIMD_CALLEE_SAVE")
#endif // !defined(TARGET_AMD64) && !defined(TARGET_ARM64)
#endif // FEATURE_PARTIAL_SIMD_CALLEE_SAVE

protected:
    // Some things are used by both LSRA and regpredict allocators.

    FrameType rpFrameType;
    bool      rpMustCreateEBPCalled; // Set to true after we have called rpMustCreateEBPFrame once

    bool rpMustCreateEBPFrame(INDEBUG(const char** wbReason));

private:
    Lowering*            m_pLowering;   // Lowering; needed to Lower IR that's added or modified after Lowering.
    LinearScanInterface* m_pLinearScan; // Linear Scan allocator

    /* raIsVarargsStackArg is called by raMaskStkVars and by
       lvaComputeRefCounts.  It identifies the special case
       where a varargs function has a parameter passed on the
       stack, other than the special varargs handle.  Such parameters
       require special treatment, because they cannot be tracked
       by the GC (their offsets in the stack are not known
       at compile time).
    */

    bool raIsVarargsStackArg(unsigned lclNum)
    {
#ifdef TARGET_X86

        LclVarDsc* varDsc = lvaGetDesc(lclNum);

        assert(varDsc->lvIsParam);

        return (info.compIsVarArgs && !varDsc->lvIsRegArg && (lclNum != lvaVarargsHandleArg));

#else // TARGET_X86

        return false;

#endif // TARGET_X86
    }

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
    // Get handles

    void eeGetCallInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                       CORINFO_RESOLVED_TOKEN* pConstrainedToken,
                       CORINFO_CALLINFO_FLAGS  flags,
                       CORINFO_CALL_INFO*      pResult);

    void eeGetFieldInfo(CORINFO_RESOLVED_TOKEN* pResolvedToken,
                        CORINFO_ACCESS_FLAGS    flags,
                        CORINFO_FIELD_INFO*     pResult);

    // Get the flags

    bool eeIsValueClass(CORINFO_CLASS_HANDLE clsHnd);
    bool eeIsIntrinsic(CORINFO_METHOD_HANDLE ftn);
    bool eeIsFieldStatic(CORINFO_FIELD_HANDLE fldHnd);

    var_types eeGetFieldType(CORINFO_FIELD_HANDLE fldHnd, CORINFO_CLASS_HANDLE* pStructHnd = nullptr);

    template <typename TPrint>
    void eeAppendPrint(class StringPrinter* printer, TPrint print);
    // Conventions: the "base" primitive printing functions take StringPrinter*
    // and do not do any SPMI handling. There are then convenience printing
    // functions exposed on top that have SPMI handling and additional buffer
    // handling. Note that the strings returned are never truncated here.
    void eePrintJitType(class StringPrinter* printer, var_types jitType);
    void eePrintType(class StringPrinter* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation);
    void eePrintTypeOrJitAlias(class StringPrinter* printer, CORINFO_CLASS_HANDLE clsHnd, bool includeInstantiation);
    void eePrintMethod(class StringPrinter*  printer,
                       CORINFO_CLASS_HANDLE  clsHnd,
                       CORINFO_METHOD_HANDLE methodHnd,
                       CORINFO_SIG_INFO*     sig,
                       bool                  includeClassInstantiation,
                       bool                  includeMethodInstantiation,
                       bool                  includeSignature,
                       bool                  includeReturnType,
                       bool                  includeThisSpecifier);

    void eePrintField(class StringPrinter* printer, CORINFO_FIELD_HANDLE fldHnd, bool includeType);

    const char* eeGetMethodFullName(CORINFO_METHOD_HANDLE hnd,
                                    bool                  includeReturnType    = true,
                                    bool                  includeThisSpecifier = true,
                                    char*                 buffer               = nullptr,
                                    size_t                bufferSize           = 0);

    const char* eeGetMethodName(CORINFO_METHOD_HANDLE methHnd, char* buffer = nullptr, size_t bufferSize = 0);

    const char* eeGetFieldName(CORINFO_FIELD_HANDLE fldHnd,
                               bool                 includeType,
                               char*                buffer     = nullptr,
                               size_t               bufferSize = 0);

    const char* eeGetClassName(CORINFO_CLASS_HANDLE clsHnd, char* buffer = nullptr, size_t bufferSize = 0);

    void eePrintObjectDescription(const char* prefix, CORINFO_OBJECT_HANDLE handle);
    const char* eeGetShortClassName(CORINFO_CLASS_HANDLE clsHnd);

#if defined(DEBUG)
    unsigned eeTryGetClassSize(CORINFO_CLASS_HANDLE clsHnd);
#endif

    unsigned compMethodHash(CORINFO_METHOD_HANDLE methodHandle);

    var_types eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig);
    var_types eeGetArgType(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig, bool* isPinned);
    CORINFO_CLASS_HANDLE eeGetArgClass(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE list);
    CORINFO_CLASS_HANDLE eeGetClassFromContext(CORINFO_CONTEXT_HANDLE context);
    unsigned eeGetArgSize(CORINFO_ARG_LIST_HANDLE list, CORINFO_SIG_INFO* sig);
    static unsigned eeGetArgSizeAlignment(var_types type, bool isFloatHfa);

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

    CORINFO_EE_INFO eeInfo;
    bool            eeInfoInitialized;

    CORINFO_EE_INFO* eeGetEEInfo();

    // Gets the offset of a SDArray's first element
    static unsigned eeGetArrayDataOffset();

    // Get the offset of a MDArray's first element
    static unsigned eeGetMDArrayDataOffset(unsigned rank);

    // Get the offset of a MDArray's dimension length for a given dimension.
    static unsigned eeGetMDArrayLengthOffset(unsigned rank, unsigned dimension);

    // Get the offset of a MDArray's lower bound for a given dimension.
    static unsigned eeGetMDArrayLowerBoundOffset(unsigned rank, unsigned dimension);

    GenTree* eeGetPInvokeCookie(CORINFO_SIG_INFO* szMetaSig);

    // Returns the page size for the target machine as reported by the EE.
    target_size_t eeGetPageSize()
    {
        return (target_size_t)eeGetEEInfo()->osPageSize;
    }

    //------------------------------------------------------------------------
    // VirtualStubParam: virtual stub dispatch extra parameter (slot address).
    //
    // It represents Abi and target specific registers for the parameter.
    //
    class VirtualStubParamInfo
    {
    public:
        VirtualStubParamInfo(bool isNativeAOT)
        {
#if defined(TARGET_X86)
            reg     = REG_EAX;
            regMask = RBM_EAX;
#elif defined(TARGET_AMD64)
            if (isNativeAOT)
            {
                reg     = REG_R10;
                regMask = RBM_R10;
            }
            else
            {
                reg     = REG_R11;
                regMask = RBM_R11;
            }
#elif defined(TARGET_ARM)
            if (isNativeAOT)
            {
                reg     = REG_R12;
                regMask = RBM_R12;
            }
            else
            {
                reg     = REG_R4;
                regMask = RBM_R4;
            }
#elif defined(TARGET_ARM64)
            reg     = REG_R11;
            regMask = RBM_R11;
#elif defined(TARGET_LOONGARCH64)
            reg     = REG_T8;
            regMask = RBM_T8;
#elif defined(TARGET_RISCV64)
            reg     = REG_T5;
            regMask = RBM_T5;
#else
#error Unsupported or unset target architecture
#endif
        }

        regNumber GetReg() const
        {
            return reg;
        }

        _regMask_enum GetRegMask() const
        {
            return regMask;
        }

    private:
        regNumber     reg;
        _regMask_enum regMask;
    };

    VirtualStubParamInfo* virtualStubParamInfo;

    bool IsTargetAbi(CORINFO_RUNTIME_ABI abi)
    {
        return eeGetEEInfo()->targetAbi == abi;
    }

    bool generateCFIUnwindCodes()
    {
#if defined(FEATURE_CFI_SUPPORT)
        return TargetOS::IsUnix && IsTargetAbi(CORINFO_NATIVEAOT_ABI);
#else
        return false;
#endif
    }

    // Debugging support - Line number info

    void eeGetStmtOffsets();

    unsigned eeBoundariesCount;

    ICorDebugInfo::OffsetMapping* eeBoundaries; // Boundaries to report to the EE
    void eeSetLIcount(unsigned count);
    void eeSetLIinfo(unsigned which, UNATIVE_OFFSET offs, IPmappingDscKind kind, const ILLocation& loc);
    void eeSetLIdone();

#ifdef DEBUG
    static void eeDispILOffs(IL_OFFSET offs);
    static void eeDispSourceMappingOffs(uint32_t offs);
    static void eeDispLineInfo(const ICorDebugInfo::OffsetMapping* line);
    void eeDispLineInfos();
#endif // DEBUG

    // Debugging support - Local var info

    void eeGetVars();

    unsigned eeVarsCount;

    struct VarResultInfo
    {
        UNATIVE_OFFSET             startOffset;
        UNATIVE_OFFSET             endOffset;
        DWORD                      varNumber;
        CodeGenInterface::siVarLoc loc;
    } * eeVars;
    void eeSetLVcount(unsigned count);
    void eeSetLVinfo(unsigned                          which,
                     UNATIVE_OFFSET                    startOffs,
                     UNATIVE_OFFSET                    length,
                     unsigned                          varNum,
                     const CodeGenInterface::siVarLoc& loc);
    void eeSetLVdone();

#ifdef DEBUG
    void eeDispVar(ICorDebugInfo::NativeVarInfo* var);
    void eeDispVars(CORINFO_METHOD_HANDLE ftn, ULONG32 cVars, ICorDebugInfo::NativeVarInfo* vars);
#endif // DEBUG

    // ICorJitInfo wrappers

    void eeAllocMem(AllocMemArgs* args, const UNATIVE_OFFSET roDataSectionAlignment);

    void eeReserveUnwindInfo(bool isFunclet, bool isColdCode, ULONG unwindSize);

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

#if defined(UNIX_AMD64_ABI)
#ifdef DEBUG
    static void dumpSystemVClassificationType(SystemVClassificationType ct);
#endif // DEBUG

    void eeGetSystemVAmd64PassStructInRegisterDescriptor(
        /*IN*/ CORINFO_CLASS_HANDLE                                  structHnd,
        /*OUT*/ SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR* structPassInRegDescPtr);
#endif // UNIX_AMD64_ABI

    template <typename ParamType>
    bool eeRunWithErrorTrap(void (*function)(ParamType*), ParamType* param)
    {
        return eeRunWithErrorTrapImp(reinterpret_cast<void (*)(void*)>(function), reinterpret_cast<void*>(param));
    }

    bool eeRunWithErrorTrapImp(void (*function)(void*), void* param);

    template <typename ParamType>
    bool eeRunWithSPMIErrorTrap(void (*function)(ParamType*), ParamType* param)
    {
        return eeRunWithSPMIErrorTrapImp(reinterpret_cast<void (*)(void*)>(function), reinterpret_cast<void*>(param));
    }

    template <typename Functor>
    bool eeRunFunctorWithSPMIErrorTrap(Functor f)
    {
        return eeRunWithSPMIErrorTrap<Functor>([](Functor* pf) { (*pf)(); }, &f);
    }

    bool eeRunWithSPMIErrorTrapImp(void (*function)(void*), void* param);

    // Utility functions

    static CORINFO_METHOD_HANDLE eeFindHelper(unsigned helper);
    static CorInfoHelpFunc eeGetHelperNum(CORINFO_METHOD_HANDLE method);

    enum StaticHelperReturnValue
    {
        SHRV_STATIC_BASE_PTR,
        SHRV_VOID,
    };
    static bool IsStaticHelperEligibleForExpansion(GenTree*                 tree,
                                                   bool*                    isGc       = nullptr,
                                                   StaticHelperReturnValue* retValKind = nullptr);
    static bool IsSharedStaticHelper(GenTree* tree);
    static bool IsGcSafePoint(GenTreeCall* call);

    static CORINFO_FIELD_HANDLE eeFindJitDataOffs(unsigned jitDataOffs);
    // returns true/false if 'field' is a Jit Data offset
    static bool eeIsJitDataOffs(CORINFO_FIELD_HANDLE field);
    // returns a number < 0 if 'field' is not a Jit Data offset, otherwise the data offset (limited to 2GB)
    static int eeGetJitDataOffs(CORINFO_FIELD_HANDLE field);

    /*****************************************************************************/

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

    // Record the instr offset mapping to the generated code

    jitstd::list<IPmappingDsc>  genIPmappings;
    jitstd::list<RichIPMapping> genRichIPmappings;

    // Managed RetVal - A side hash table meant to record the mapping from a
    // GT_CALL node to its debug info.  This info is used to emit sequence points
    // that can be used by debugger to determine the native offset at which the
    // managed RetVal will be available.
    //
    // In fact we can store debug info in a GT_CALL node.  This was ruled out in
    // favor of a side table for two reasons: 1) We need debug info for only those
    // GT_CALL nodes (created during importation) that correspond to an IL call and
    // whose return type is other than TYP_VOID. 2) GT_CALL node is a frequently used
    // structure and IL offset is needed only when generating debuggable code. Therefore
    // it is desirable to avoid memory size penalty in retail scenarios.
    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, DebugInfo> CallSiteDebugInfoTable;
    CallSiteDebugInfoTable* genCallSite2DebugInfoMap;

    unsigned    genReturnLocal; // Local number for the return value when applicable.
    BasicBlock* genReturnBB;    // jumped to when not optimizing for speed.

    // The following properties are part of CodeGenContext.  Getters are provided here for
    // convenience and backward compatibility, but the properties can only be set by invoking
    // the setter on CodeGenContext directly.

    emitter* GetEmitter() const
    {
        return codeGen->GetEmitter();
    }

    bool isFramePointerUsed() const
    {
        return codeGen->isFramePointerUsed();
    }

    bool GetInterruptible()
    {
        return codeGen->GetInterruptible();
    }
    void SetInterruptible(bool value)
    {
        codeGen->SetInterruptible(value);
    }

#if DOUBLE_ALIGN
    const bool genDoubleAlign()
    {
        return codeGen->doDoubleAlign();
    }
    DWORD getCanDoubleAlign();
    bool shouldDoubleAlign(unsigned refCntStk,
                           unsigned refCntReg,
                           weight_t refCntWtdReg,
                           unsigned refCntStkParam,
                           weight_t refCntWtdStkDbl);
#endif // DOUBLE_ALIGN

    bool IsFullPtrRegMapRequired()
    {
        return codeGen->IsFullPtrRegMapRequired();
    }
    void SetFullPtrRegMapRequired(bool value)
    {
        codeGen->SetFullPtrRegMapRequired(value);
    }

// Things that MAY belong either in CodeGen or CodeGenContext

#if defined(FEATURE_EH_FUNCLETS)
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

    VARSET_TP compCurLife;     // current live variables
    GenTree*  compCurLifeTree; // node after which compCurLife has been computed

    // Compare the given "newLife" with last set of live variables and update
    // codeGen "gcInfo", siScopes, "regSet" with the new variable's homes/liveness.
    template <bool ForCodeGen>
    void compChangeLife(VARSET_VALARG_TP newLife);

    // Update the GC's masks, register's masks and reports change on variable's homes given a set of
    // current live variables if changes have happened since "compCurLife".
    template <bool ForCodeGen>
    inline void compUpdateLife(VARSET_VALARG_TP newLife);

    // Gets a register mask that represent the kill set for a helper call since
    // not all JIT Helper calls follow the standard ABI on the target architecture.
    regMaskTP compHelperCallKillSet(CorInfoHelpFunc helper);

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

#if defined(TARGET_ARM)
    void unwindPushMaskInt(regMaskTP mask);
    void unwindPushMaskFloat(regMaskTP mask);
    void unwindPopMaskInt(regMaskTP mask);
    void unwindPopMaskFloat(regMaskTP mask);
    void unwindBranch16();                    // The epilog terminates with a 16-bit branch (e.g., "bx lr")
    void unwindNop(unsigned codeSizeInBytes); // Generate unwind NOP code. 'codeSizeInBytes' is 2 or 4 bytes. Only
                                              // called via unwindPadding().
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
#endif                    // TARGET_ARM

#if defined(TARGET_ARM64)
    void unwindNop();
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
    void unwindSaveReg(regNumber reg, int offset);                                // str reg, [sp, #offset]
    void unwindSaveRegPreindexed(regNumber reg, int offset);                      // str reg, [sp, #offset]!
    void unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset);           // stp reg1, reg2, [sp, #offset]
    void unwindSaveRegPairPreindexed(regNumber reg1, regNumber reg2, int offset); // stp reg1, reg2, [sp, #offset]!
    void unwindSaveNext();                                                        // unwind code: save_next
    void unwindReturn(regNumber reg);                                             // ret lr
#endif                                                                            // defined(TARGET_ARM64)

#if defined(TARGET_LOONGARCH64)
    void unwindNop();
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
    void unwindSaveReg(regNumber reg, int offset);
    void unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset);
    void unwindReturn(regNumber reg);
#endif // defined(TARGET_LOONGARCH64)

#if defined(TARGET_RISCV64)
    void unwindNop();
    void unwindPadding(); // Generate a sequence of unwind NOP codes representing instructions between the last
                          // instruction and the current location.
    void unwindSaveReg(regNumber reg, int offset);
    void unwindSaveRegPair(regNumber reg1, regNumber reg2, int offset);
    void unwindReturn(regNumber reg);
#endif // defined(TARGET_RISCV64)

    //
    // Private "helper" functions for the unwind implementation.
    //

private:
#if defined(FEATURE_EH_FUNCLETS)
    void unwindGetFuncLocations(FuncInfoDsc*             func,
                                bool                     getHotSectionData,
                                /* OUT */ emitLocation** ppStartLoc,
                                /* OUT */ emitLocation** ppEndLoc);
#endif // FEATURE_EH_FUNCLETS

    void unwindReserveFunc(FuncInfoDsc* func);
    void unwindEmitFunc(FuncInfoDsc* func, void* pHotCode, void* pColdCode);

#if defined(TARGET_AMD64) || (defined(TARGET_X86) && defined(FEATURE_EH_FUNCLETS))

    void unwindReserveFuncHelper(FuncInfoDsc* func, bool isHotCode);
    void unwindEmitFuncHelper(FuncInfoDsc* func, void* pHotCode, void* pColdCode, bool isHotCode);

#endif // TARGET_AMD64 || (TARGET_X86 && FEATURE_EH_FUNCLETS)

    UNATIVE_OFFSET unwindGetCurrentOffset(FuncInfoDsc* func);

#if defined(TARGET_AMD64)

    void unwindBegPrologWindows();
    void unwindPushWindows(regNumber reg);
    void unwindAllocStackWindows(unsigned size);
    void unwindSetFrameRegWindows(regNumber reg, unsigned offset);
    void unwindSaveRegWindows(regNumber reg, unsigned offset);

#ifdef UNIX_AMD64_ABI
    void unwindSaveRegCFI(regNumber reg, unsigned offset);
#endif // UNIX_AMD64_ABI
#elif defined(TARGET_ARM)

    void unwindPushPopMaskInt(regMaskTP mask, bool useOpsize16);
    void unwindPushPopMaskFloat(regMaskTP mask);

#endif // TARGET_ARM

#if defined(FEATURE_CFI_SUPPORT)
    short mapRegNumToDwarfReg(regNumber reg);
    void createCfiCode(FuncInfoDsc* func, UNATIVE_OFFSET codeOffset, UCHAR opcode, short dwarfReg, INT offset = 0);
    void unwindPushPopCFI(regNumber reg);
    void unwindBegPrologCFI();
    void unwindPushPopMaskCFI(regMaskTP regMask, bool isFloat);
    void unwindAllocStackCFI(unsigned size);
    void unwindSetFrameRegCFI(regNumber reg, unsigned offset);
    void unwindEmitFuncCFI(FuncInfoDsc* func, void* pHotCode, void* pColdCode);
#ifdef DEBUG
    void DumpCfiInfo(bool                  isHotCode,
                     UNATIVE_OFFSET        startOffset,
                     UNATIVE_OFFSET        endOffset,
                     DWORD                 cfiCodeBytes,
                     const CFI_CODE* const pCfiCode);
#endif

#endif // FEATURE_CFI_SUPPORT

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

    bool IsBaselineSimdIsaSupported()
    {
#ifdef FEATURE_SIMD
#if defined(TARGET_XARCH)
        CORINFO_InstructionSet minimumIsa = InstructionSet_SSE2;
#elif defined(TARGET_ARM64)
        CORINFO_InstructionSet minimumIsa = InstructionSet_AdvSimd;
#elif defined(TARGET_LOONGARCH64)
        // TODO: supporting SIMD feature for LoongArch64.
        assert(!"unimplemented yet on LA");
        CORINFO_InstructionSet minimumIsa = 0;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64 && !TARGET_LOONGARCH64

        return compOpportunisticallyDependsOn(minimumIsa);
#else
        return false;
#endif
    }

#if defined(DEBUG)
    bool IsBaselineSimdIsaSupportedDebugOnly()
    {
#ifdef FEATURE_SIMD
#if defined(TARGET_XARCH)
        CORINFO_InstructionSet minimumIsa = InstructionSet_SSE2;
#elif defined(TARGET_ARM64)
        CORINFO_InstructionSet minimumIsa = InstructionSet_AdvSimd;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        return compIsaSupportedDebugOnly(minimumIsa);
#else
        return false;
#endif // FEATURE_SIMD
    }
#endif // DEBUG

    bool isIntrinsicType(CORINFO_CLASS_HANDLE clsHnd)
    {
        return info.compCompHnd->isIntrinsicType(clsHnd);
    }

    const char* getClassNameFromMetadata(CORINFO_CLASS_HANDLE cls, const char** namespaceName)
    {
        return info.compCompHnd->getClassNameFromMetadata(cls, namespaceName);
    }

    CORINFO_CLASS_HANDLE getTypeInstantiationArgument(CORINFO_CLASS_HANDLE cls, unsigned index)
    {
        return info.compCompHnd->getTypeInstantiationArgument(cls, index);
    }

    bool isNumericsNamespace(const char* ns)
    {
        return strcmp(ns, "System.Numerics") == 0;
    }

    bool isRuntimeIntrinsicsNamespace(const char* ns)
    {
        return strcmp(ns, "System.Runtime.Intrinsics") == 0;
    }

    bool isSpanClass(const CORINFO_CLASS_HANDLE clsHnd)
    {
        if (isIntrinsicType(clsHnd))
        {
            const char* namespaceName = nullptr;
            const char* className     = getClassNameFromMetadata(clsHnd, &namespaceName);
            return strcmp(namespaceName, "System") == 0 &&
                   (strcmp(className, "Span`1") == 0 || strcmp(className, "ReadOnlySpan`1") == 0);
        }
        return false;
    }

#ifdef FEATURE_SIMD
    // Have we identified any SIMD types?
    // This is currently used by struct promotion to avoid getting type information for a struct
    // field to see if it is a SIMD type, if we haven't seen any SIMD types or operations in
    // the method.
    bool _usesSIMDTypes;
    bool usesSIMDTypes()
    {
        return _usesSIMDTypes;
    }
    void setUsesSIMDTypes(bool value)
    {
        _usesSIMDTypes = value;
    }

    // This is a temp lclVar allocated on the stack as TYP_SIMD.  It is used to implement intrinsics
    // that require indexed access to the individual fields of the vector, which is not well supported
    // by the hardware.  It is allocated when/if such situations are encountered during Lowering.
    unsigned lvaSIMDInitTempVarNum;

    struct SIMDHandlesCache
    {
        CORINFO_CLASS_HANDLE PlaneHandle;
        CORINFO_CLASS_HANDLE QuaternionHandle;
        CORINFO_CLASS_HANDLE Vector2Handle;
        CORINFO_CLASS_HANDLE Vector3Handle;
        CORINFO_CLASS_HANDLE Vector4Handle;
        CORINFO_CLASS_HANDLE VectorHandle;

        SIMDHandlesCache()
        {
            memset(this, 0, sizeof(*this));
        }
    };

    SIMDHandlesCache* m_simdHandleCache;

    // Returns true if this is a SIMD type that should be considered an opaque
    // vector type (i.e. do not analyze or promote its fields).
    // Note that all but the fixed vector types are opaque, even though they may
    // actually be declared as having fields.
    bool isOpaqueSIMDType(CORINFO_CLASS_HANDLE structHandle) const
    {
        // We order the checks roughly by expected hit count so early exits are possible

        if (m_simdHandleCache == nullptr)
        {
            return false;
        }

        if (structHandle == m_simdHandleCache->Vector4Handle)
        {
            return false;
        }

        if (structHandle == m_simdHandleCache->Vector3Handle)
        {
            return false;
        }

        if (structHandle == m_simdHandleCache->Vector2Handle)
        {
            return false;
        }

        if (structHandle == m_simdHandleCache->QuaternionHandle)
        {
            return false;
        }

        if (structHandle == m_simdHandleCache->PlaneHandle)
        {
            return false;
        }

        return true;
    }

    bool isOpaqueSIMDType(ClassLayout* layout) const
    {
        if (layout->IsBlockLayout())
        {
            return true;
        }

        return isOpaqueSIMDType(layout->GetClassHandle());
    }

    // Returns true if the lclVar is an opaque SIMD type.
    bool isOpaqueSIMDLclVar(const LclVarDsc* varDsc) const
    {
        if (!varTypeIsSIMD(varDsc))
        {
            return false;
        }

        if (varDsc->GetLayout() == nullptr)
        {
            return true;
        }

        return isOpaqueSIMDType(varDsc->GetLayout());
    }

    bool isSIMDClass(CORINFO_CLASS_HANDLE clsHnd)
    {
        if (isIntrinsicType(clsHnd))
        {
            const char* namespaceName = nullptr;
            (void)getClassNameFromMetadata(clsHnd, &namespaceName);
            return isNumericsNamespace(namespaceName);
        }
        return false;
    }

    bool isHWSIMDClass(CORINFO_CLASS_HANDLE clsHnd)
    {
#ifdef FEATURE_HW_INTRINSICS
        if (isIntrinsicType(clsHnd))
        {
            const char* namespaceName = nullptr;
            (void)getClassNameFromMetadata(clsHnd, &namespaceName);
            return isRuntimeIntrinsicsNamespace(namespaceName);
        }
#endif // FEATURE_HW_INTRINSICS
        return false;
    }

    bool isSIMDorHWSIMDClass(CORINFO_CLASS_HANDLE clsHnd)
    {
        return isSIMDClass(clsHnd) || isHWSIMDClass(clsHnd);
    }

    // Get the base (element) type and size in bytes for a SIMD type. Returns CORINFO_TYPE_UNDEF
    // if it is not a SIMD type or is an unsupported base JIT type.
    CorInfoType getBaseJitTypeAndSizeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd, unsigned* sizeBytes = nullptr);

    CorInfoType getBaseJitTypeOfSIMDType(CORINFO_CLASS_HANDLE typeHnd)
    {
        return getBaseJitTypeAndSizeOfSIMDType(typeHnd, nullptr);
    }

    GenTree* impSIMDPopStack();

    void setLclRelatedToSIMDIntrinsic(GenTree* tree);
    bool areFieldsContiguous(GenTreeIndir* op1, GenTreeIndir* op2);
    bool areLocalFieldsContiguous(GenTreeLclFld* first, GenTreeLclFld* second);
    bool areArrayElementsContiguous(GenTree* op1, GenTree* op2);
    bool areArgumentsContiguous(GenTree* op1, GenTree* op2);
    GenTree* CreateAddressNodeForSimdHWIntrinsicCreate(GenTree* tree, var_types simdBaseType, unsigned simdSize);

    // Get the size of the SIMD type in bytes
    int getSIMDTypeSizeInBytes(CORINFO_CLASS_HANDLE typeHnd)
    {
        unsigned sizeBytes = 0;
        (void)getBaseJitTypeAndSizeOfSIMDType(typeHnd, &sizeBytes);
        return sizeBytes;
    }

    // Get the number of elements of baseType of SIMD vector given by its size and baseType
    static int getSIMDVectorLength(unsigned simdSize, var_types baseType);

    // Get the number of elements of baseType of SIMD vector given by its type handle
    int getSIMDVectorLength(CORINFO_CLASS_HANDLE typeHnd);

    // Get preferred alignment of SIMD type.
    int getSIMDTypeAlignment(var_types simdType);

public:
    // Get the number of bytes in a System.Numeric.Vector<T> for the current compilation.
    // Note - cannot be used for System.Runtime.Intrinsic
    uint32_t getVectorTByteLength()
    {
        // We need to report the ISA dependency to the VM so that scenarios
        // such as R2R work correctly for larger vector sizes, so we always
        // do `compExactlyDependsOn` for such cases.
        CLANG_FORMAT_COMMENT_ANCHOR;

#if defined(TARGET_XARCH)
        if (compExactlyDependsOn(InstructionSet_VectorT512))
        {
            assert(!compIsaSupportedDebugOnly(InstructionSet_VectorT256));
            assert(!compIsaSupportedDebugOnly(InstructionSet_VectorT128));
            return ZMM_REGSIZE_BYTES;
        }
        else if (compExactlyDependsOn(InstructionSet_VectorT256))
        {
            assert(!compIsaSupportedDebugOnly(InstructionSet_VectorT128));
            return YMM_REGSIZE_BYTES;
        }
        else if (compExactlyDependsOn(InstructionSet_VectorT128))
        {
            return XMM_REGSIZE_BYTES;
        }
        else
        {
            // TODO: We should be returning 0 here, but there are a number of
            // places that don't quite get handled correctly in that scenario

            return XMM_REGSIZE_BYTES;
        }
#elif defined(TARGET_ARM64)
        if (compExactlyDependsOn(InstructionSet_VectorT128))
        {
            return FP_REGSIZE_BYTES;
        }
        else
        {
            // TODO: We should be returning 0 here, but there are a number of
            // places that don't quite get handled correctly in that scenario

            return FP_REGSIZE_BYTES;
        }
#else
        assert(!"getVectorTByteLength() unimplemented on target arch");
        unreached();
#endif
    }

    // The minimum and maximum possible number of bytes in a SIMD vector.

    // getMaxVectorByteLength
    // The minimum SIMD size supported by System.Numeric.Vectors or System.Runtime.Intrinsic
    // Arm.AdvSimd:  16-byte Vector<T> and Vector128<T>
    // X86.SSE:      16-byte Vector<T> and Vector128<T>
    // X86.AVX:      16-byte Vector<T> and Vector256<T>
    // X86.AVX2:     32-byte Vector<T> and Vector256<T>
    // X86.AVX512F:  32-byte Vector<T> and Vector512<T>
    uint32_t getMaxVectorByteLength() const
    {
#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_XARCH)
        if (compOpportunisticallyDependsOn(InstructionSet_AVX512F))
        {
            return ZMM_REGSIZE_BYTES;
        }
        else if (compOpportunisticallyDependsOn(InstructionSet_AVX))
        {
            return YMM_REGSIZE_BYTES;
        }
        else if (compOpportunisticallyDependsOn(InstructionSet_SSE))
        {
            return XMM_REGSIZE_BYTES;
        }
        else
        {
            // TODO: We should be returning 0 here, but there are a number of
            // places that don't quite get handled correctly in that scenario

            return XMM_REGSIZE_BYTES;
        }
#elif defined(TARGET_ARM64)
        if (compOpportunisticallyDependsOn(InstructionSet_AdvSimd))
        {
            return FP_REGSIZE_BYTES;
        }
        else
        {
            // TODO: We should be returning 0 here, but there are a number of
            // places that don't quite get handled correctly in that scenario

            return FP_REGSIZE_BYTES;
        }
#else
        assert(!"getMaxVectorByteLength() unimplemented on target arch");
        unreached();
#endif
    }

    //------------------------------------------------------------------------
    // getPreferredVectorByteLength: Gets the preferred length, in bytes, to use for vectorization
    //
    uint32_t getPreferredVectorByteLength() const
    {
#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_XARCH)
        uint32_t preferredVectorByteLength = opts.preferredVectorByteLength;

        if (preferredVectorByteLength != 0)
        {
            return min(getMaxVectorByteLength(), preferredVectorByteLength);
        }
#endif // FEATURE_HW_INTRINSICS && TARGET_XARCH

        return getMaxVectorByteLength();
    }

    //------------------------------------------------------------------------
    // roundUpSIMDSize: rounds the given size up to the nearest SIMD size
    //                  available on the target. Examples on XARCH:
    //
    //    size: 7 -> XMM
    //    size: 30 -> YMM (or XMM if target doesn't support AVX)
    //    size: 70 -> ZMM (or YMM or XMM depending on target)
    //
    // Arguments:
    //    size   - size of the data to process with SIMD
    //
    // Notes:
    //    It's only supposed to be used for scenarios where we can
    //    perform an overlapped load/store.
    //
    uint32_t roundUpSIMDSize(unsigned size)
    {
#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_XARCH)
        uint32_t maxSize = getPreferredVectorByteLength();
        assert(maxSize <= ZMM_REGSIZE_BYTES);

        if ((size <= XMM_REGSIZE_BYTES) && (maxSize > XMM_REGSIZE_BYTES))
        {
            return XMM_REGSIZE_BYTES;
        }

        if ((size <= YMM_REGSIZE_BYTES) && (maxSize > YMM_REGSIZE_BYTES))
        {
            return YMM_REGSIZE_BYTES;
        }

        return maxSize;
#elif defined(TARGET_ARM64)
        assert(getMaxVectorByteLength() == FP_REGSIZE_BYTES);
        return FP_REGSIZE_BYTES;
#else
        assert(!"roundUpSIMDSize() unimplemented on target arch");
        unreached();
#endif
    }

    //------------------------------------------------------------------------
    // roundDownSIMDSize: rounds the given size down to the nearest SIMD size
    //                    available on the target. Examples on XARCH:
    //
    //    size: 7 -> 0
    //    size: 30 -> XMM (not enough for AVX)
    //    size: 60 -> YMM (or XMM if target doesn't support AVX)
    //    size: 70 -> ZMM/YMM/XMM whatever the current system can offer
    //
    // Arguments:
    //    size   - size of the data to process with SIMD
    //
    uint32_t roundDownSIMDSize(unsigned size)
    {
#if defined(FEATURE_HW_INTRINSICS) && defined(TARGET_XARCH)
        uint32_t maxSize = getPreferredVectorByteLength();
        assert(maxSize <= ZMM_REGSIZE_BYTES);

        if (size >= maxSize)
        {
            // Size is bigger than max SIMD size the current target supports
            return maxSize;
        }

        if ((size >= YMM_REGSIZE_BYTES) && (maxSize >= YMM_REGSIZE_BYTES))
        {
            // Size is >= YMM but not enough for ZMM -> YMM
            return YMM_REGSIZE_BYTES;
        }

        // Return 0 if size is even less than XMM, otherwise - XMM
        return (size >= XMM_REGSIZE_BYTES) ? XMM_REGSIZE_BYTES : 0;
#elif defined(TARGET_ARM64)
        assert(getMaxVectorByteLength() == FP_REGSIZE_BYTES);
        return (size >= FP_REGSIZE_BYTES) ? FP_REGSIZE_BYTES : 0;
#else
        assert(!"roundDownSIMDSize() unimplemented on target arch");
        unreached();
#endif
    }

    uint32_t getMinVectorByteLength()
    {
        return emitTypeSize(TYP_SIMD8);
    }

    // Returns the codegen type for a given SIMD size.
    static var_types getSIMDTypeForSize(unsigned size)
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
#if defined(TARGET_XARCH)
        else if (size == 32)
        {
            simdType = TYP_SIMD32;
        }
        else if (size == 64)
        {
            simdType = TYP_SIMD64;
        }
#endif // TARGET_XARCH
        else
        {
            noway_assert(!"Unexpected size for SIMD type");
        }
        return simdType;
    }

private:
    unsigned getSIMDInitTempVarNum(var_types simdType);

#else  // !FEATURE_SIMD
    bool isOpaqueSIMDLclVar(LclVarDsc* varDsc)
    {
        return false;
    }
    unsigned int roundUpSIMDSize(unsigned size)
    {
        return 0;
    }
    unsigned int roundDownSIMDSize(unsigned size)
    {
        return 0;
    }
#endif // FEATURE_SIMD

public:
    // Similar to roundUpSIMDSize, but for General Purpose Registers (GPR)
    unsigned roundUpGPRSize(unsigned size)
    {
        if (size > 4 && (REGSIZE_BYTES == 8))
        {
            return 8;
        }
        else if (size > 2)
        {
            return 4;
        }
        return size; // 2, 1, 0
    }

    var_types roundDownMaxType(unsigned size)
    {
        assert(size > 0);
        var_types result = TYP_UNDEF;
#ifdef FEATURE_SIMD
        if (IsBaselineSimdIsaSupported() && (roundDownSIMDSize(size) > 0))
        {
            return getSIMDTypeForSize(roundDownSIMDSize(size));
        }
#endif
        int nearestPow2 = 1 << BitOperations::Log2((unsigned)size);
        switch (min(nearestPow2, REGSIZE_BYTES))
        {
            case 1:
                return TYP_UBYTE;
            case 2:
                return TYP_USHORT;
            case 4:
                return TYP_INT;
            case 8:
                assert(REGSIZE_BYTES == 8);
                return TYP_LONG;
            default:
                unreached();
        }
    }

    enum UnrollKind
    {
        Memset,
        Memcpy,
        Memmove,
        ProfiledMemmove,
        ProfiledMemcmp
    };

    //------------------------------------------------------------------------
    // getUnrollThreshold: Calculates the unrolling threshold for the given operation
    //
    // Arguments:
    //    type       - kind of the operation (memset/memcpy)
    //    canUseSimd - whether it is allowed to use SIMD or not
    //
    // Return Value:
    //    The unrolling threshold for the given operation in bytes
    //
    unsigned int getUnrollThreshold(UnrollKind type, bool canUseSimd = true)
    {
        unsigned maxRegSize = REGSIZE_BYTES;
        unsigned threshold  = maxRegSize;

#if defined(FEATURE_SIMD)
        if (canUseSimd)
        {
            maxRegSize = getPreferredVectorByteLength();

#if defined(TARGET_XARCH)
            assert(maxRegSize <= ZMM_REGSIZE_BYTES);
            threshold = maxRegSize;
#elif defined(TARGET_ARM64)
            // ldp/stp instructions can load/store two 16-byte vectors at once, e.g.:
            //
            //   ldp q0, q1, [x1]
            //   stp q0, q1, [x0]
            //
            threshold = maxRegSize * 2;
#endif
        }
#if defined(TARGET_XARCH)
        else
        {
            // Compatibility with previous logic: we used to allow memset:128/memcpy:64
            // on AMD64 (and 64/32 on x86) for cases where we don't use SIMD
            // see https://github.com/dotnet/runtime/issues/83297
            threshold *= 2;
        }
#endif
#endif

        if (type == UnrollKind::Memset)
        {
            // Typically, memset-like operations require less instructions than memcpy
            threshold *= 2;
        }

        // Use 4 as a multiplier by default, thus, the final threshold will be:
        //
        // | arch        | memset | memcpy |
        // |-------------|--------|--------|
        // | x86 avx512  |   512  |   256  |
        // | x86 avx     |   256  |   128  |
        // | x86 sse     |   128  |    64  |
        // | arm64       |   256  |   128  | ldp/stp (2x128bit)
        // | arm         |    32  |    16  | no SIMD support
        // | loongarch64 |    64  |    32  | no SIMD support
        //
        // We might want to use a different multiplier for truly hot/cold blocks based on PGO data
        //
        threshold *= 4;

        if (type == UnrollKind::Memmove)
        {
            // NOTE: Memmove's unrolling is currently limited with LSRA -
            // up to LinearScan::MaxInternalCount number of temp regs, e.g. 5*16=80 bytes on arm64
            threshold = maxRegSize * 4;
        }

        // For profiled memcmp/memmove we don't want to unroll too much as it's just a guess,
        // and it works better for small sizes.
        if ((type == UnrollKind::ProfiledMemcmp) || (type == UnrollKind::ProfiledMemmove))
        {
            threshold = maxRegSize * 2;
        }

        return threshold;
    }

    // Use to determine if a struct *might* be a SIMD type. As this function only takes a size, many
    // structs will fit the criteria.
    bool structSizeMightRepresentSIMDType(size_t structSize)
    {
#ifdef FEATURE_SIMD
        return (structSize >= getMinVectorByteLength()) && (structSize <= getMaxVectorByteLength());
#else
        return false;
#endif // FEATURE_SIMD
    }

#ifdef FEATURE_HW_INTRINSICS
    static bool vnEncodesResultTypeForHWIntrinsic(NamedIntrinsic hwIntrinsicID);
#endif // FEATURE_HW_INTRINSICS

private:
    // Returns true if the TYP_SIMD locals on stack are aligned at their
    // preferred byte boundary specified by getSIMDTypeAlignment().
    //
    // As per the Intel manual, the preferred alignment for AVX vectors is
    // 32-bytes. It is not clear whether additional stack space used in
    // aligning stack is worth the benefit and for now will use 16-byte
    // alignment for AVX 256-bit vectors with unaligned load/stores to/from
    // memory. On x86, the stack frame is aligned to 4 bytes. We need to extend
    // existing support for double (8-byte) alignment to 16 or 32 byte
    // alignment for frames with local SIMD vars, if that is determined to be
    // profitable.
    //
    // On Amd64 and SysV, RSP+8 is aligned on entry to the function (before
    // prolog has run). This means that in RBP-based frames RBP will be 16-byte
    // aligned. For RSP-based frames these are only sometimes aligned, depending
    // on the frame size.
    //
    bool isSIMDTypeLocalAligned(unsigned varNum)
    {
#if defined(FEATURE_SIMD) && ALIGN_SIMD_TYPES
        LclVarDsc* lcl = lvaGetDesc(varNum);
        if (varTypeIsSIMD(lcl))
        {
            // TODO-Cleanup: Can't this use the lvExactSize on the varDsc?
            int alignment = getSIMDTypeAlignment(lcl->TypeGet());
            if (alignment <= STACK_ALIGN)
            {
                bool rbpBased;
                int  off = lvaFrameAddress(varNum, &rbpBased);
                // On SysV and Winx64 ABIs RSP+8 will be 16-byte aligned at the
                // first instruction of a function. If our frame is RBP based
                // then RBP will always be 16 bytes aligned, so we can simply
                // check the offset.
                if (rbpBased)
                {
                    return (off % alignment) == 0;
                }

                // For RSP-based frame the alignment of RSP depends on our
                // locals. rsp+8 is aligned on entry and we just subtract frame
                // size so it is not hard to compute. Note that the compiler
                // tries hard to make sure the frame size means RSP will be
                // 16-byte aligned, but for leaf functions without locals (i.e.
                // frameSize = 0) it will not be.
                int frameSize = codeGen->genTotalFrameSize();
                return ((8 - frameSize + off) % alignment) == 0;
            }
        }
#endif // FEATURE_SIMD

        return false;
    }

#ifdef DEBUG
    // Answer the question: Is a particular ISA supported?
    // Use this api when asking the question so that future
    // ISA questions can be asked correctly or when asserting
    // support/nonsupport for an instruction set
    bool compIsaSupportedDebugOnly(CORINFO_InstructionSet isa) const
    {
#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        return opts.compSupportsISA.HasInstructionSet(isa);
#else
        return false;
#endif
    }
#endif // DEBUG

    bool notifyInstructionSetUsage(CORINFO_InstructionSet isa, bool supported) const;

    // Answer the question: Is a particular ISA allowed to be used implicitly by optimizations?
    // The result of this api call will exactly match the target machine
    // on which the function is executed (except for CoreLib, where there are special rules)
    bool compExactlyDependsOn(CORINFO_InstructionSet isa) const
    {
#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
        if ((opts.compSupportsISAReported.HasInstructionSet(isa)) == false)
        {
            if (notifyInstructionSetUsage(isa, (opts.compSupportsISA.HasInstructionSet(isa))))
                ((Compiler*)this)->opts.compSupportsISAExactly.AddInstructionSet(isa);
            ((Compiler*)this)->opts.compSupportsISAReported.AddInstructionSet(isa);
        }
        return (opts.compSupportsISAExactly.HasInstructionSet(isa));
#else
        return false;
#endif
    }

    // Answer the question: Is a particular ISA allowed to be used implicitly by optimizations?
    // The result of this api call will match the target machine if the result is true.
    // If the result is false, then the target machine may have support for the instruction.
    bool compOpportunisticallyDependsOn(CORINFO_InstructionSet isa) const
    {
        if (opts.compSupportsISA.HasInstructionSet(isa))
        {
            return compExactlyDependsOn(isa);
        }
        else
        {
            return false;
        }
    }

    // Answer the question: Is a particular ISA supported for explicit hardware intrinsics?
    bool compHWIntrinsicDependsOn(CORINFO_InstructionSet isa) const
    {
        // Report intent to use the ISA to the EE
        compExactlyDependsOn(isa);
        return opts.compSupportsISA.HasInstructionSet(isa);
    }

#ifdef DEBUG
    //------------------------------------------------------------------------
    // IsBaselineVector512IsaSupportedDebugOnly - Does isa support exist for Vector512.
    //
    // Returns:
    //    `true` if AVX512F, AVX512BW, AVX512CD, AVX512DQ, and AVX512VL are supported.
    //
    bool IsBaselineVector512IsaSupportedDebugOnly() const
    {
#ifdef TARGET_XARCH
        return compIsaSupportedDebugOnly(InstructionSet_AVX512F);
#else
        return false;
#endif
    }
#endif // DEBUG

    //------------------------------------------------------------------------
    // IsBaselineVector512IsaSupportedOpportunistically - Does opportunistic isa support exist for Vector512.
    //
    // Returns:
    //    `true` if AVX512F, AVX512BW, AVX512CD, AVX512DQ, and AVX512VL are supported.
    //
    bool IsBaselineVector512IsaSupportedOpportunistically() const
    {
#ifdef TARGET_XARCH
        return compOpportunisticallyDependsOn(InstructionSet_AVX512F);
#else
        return false;
#endif
    }

#ifdef TARGET_XARCH
    bool canUseVexEncoding() const
    {
        return compOpportunisticallyDependsOn(InstructionSet_AVX);
    }

    //------------------------------------------------------------------------
    // canUseEvexEncoding - Answer the question: Is Evex encoding supported on this target.
    //
    // Returns:
    //    `true` if Evex encoding is supported, `false` if not.
    //
    bool canUseEvexEncoding() const
    {
        return compOpportunisticallyDependsOn(InstructionSet_AVX512F);
    }

    //------------------------------------------------------------------------
    // DoJitStressEvexEncoding- Answer the question: Do we force EVEX encoding.
    //
    // Returns:
    //    `true` if user requests EVEX encoding and it's safe, `false` if not.
    //
    bool DoJitStressEvexEncoding() const
    {
#ifdef DEBUG
        // Using JitStressEVEXEncoding flag will force instructions which would
        // otherwise use VEX encoding but can be EVEX encoded to use EVEX encoding
        // This requires AVX512F, AVX512BW, AVX512CD, AVX512DQ, and AVX512VL support

        if (JitConfig.JitStressEvexEncoding() && IsBaselineVector512IsaSupportedOpportunistically())
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW_VL));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512CD));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512CD_VL));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512DQ));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512DQ_VL));

            return true;
        }
#endif // DEBUG

        return false;
    }
#endif // TARGET_XARCH

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

    bool compDoAggressiveInlining;     // If true, mark every method as CORINFO_FLG_FORCEINLINE
    bool compJmpOpUsed;                // Does the method do a JMP
    bool compLongUsed;                 // Does the method use TYP_LONG
    bool compFloatingPointUsed;        // Does the method use TYP_FLOAT or TYP_DOUBLE
    bool compTailCallUsed;             // Does the method do a tailcall
    bool compTailPrefixSeen;           // Does the method IL have tail. prefix
    bool compLocallocSeen;             // Does the method IL have localloc opcode
    bool compLocallocUsed;             // Does the method use localloc.
    bool compLocallocOptimized;        // Does the method have an optimized localloc
    bool compQmarkUsed;                // Does the method use GT_QMARK/GT_COLON
    bool compQmarkRationalized;        // Is it allowed to use a GT_QMARK/GT_COLON node.
    bool compHasBackwardJump;          // Does the method (or some inlinee) have a lexically backwards jump?
    bool compHasBackwardJumpInHandler; // Does the method have a lexically backwards jump in a handler?
    bool compSwitchedToOptimized;      // Codegen initially was Tier0 but jit switched to FullOpts
    bool compSwitchedToMinOpts;        // Codegen initially was Tier1/FullOpts but jit switched to MinOpts
    bool compSuppressedZeroInit;       // There are vars with lvSuppressedZeroInit set

// NOTE: These values are only reliable after
//       the importing is completely finished.

#ifdef DEBUG
    // State information - which phases have completed?
    // These are kept together for easy discoverability

    bool    compAllowStress;
    bool    compCodeGenDone;
    int64_t compNumStatementLinksTraversed; // # of links traversed while doing debug checks
    bool    fgNormalizeEHDone;              // Has the flowgraph EH normalization phase been done?
    size_t  compSizeEstimate;               // The estimated size of the method as per `gtSetEvalOrder`.
    size_t  compCycleEstimate;              // The estimated cycle count of the method as per `gtSetEvalOrder`
    bool    compPoisoningAnyImplicitByrefs; // Importer inserted IR before returns to poison implicit byrefs

#endif // DEBUG

    bool fgLocalVarLivenessDone; // Note that this one is used outside of debug.
    bool fgLocalVarLivenessChanged;
    bool fgIsDoingEarlyLiveness;
    bool fgDidEarlyLiveness;
    bool compPostImportationCleanupDone;
    bool compLSRADone;
    bool compRationalIRForm;

    bool compUsesThrowHelper; // There is a call to a THROW_HELPER for the compiled method.

    bool compGeneratingProlog;
    bool compGeneratingEpilog;
    bool compGeneratingUnwindProlog;
    bool compGeneratingUnwindEpilog;
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
        JitFlags* jitFlags; // all flags passed from the EE

        // The instruction sets that the compiler is allowed to emit.
        CORINFO_InstructionSetFlags compSupportsISA;
        // The instruction sets that were reported to the VM as being used by the current method. Subset of
        // compSupportsISA.
        CORINFO_InstructionSetFlags compSupportsISAReported;
        // The instruction sets that the compiler is allowed to take advantage of implicitly during optimizations.
        // Subset of compSupportsISA.
        // The instruction sets available in compSupportsISA and not available in compSupportsISAExactly can be only
        // used via explicit hardware intrinsics.
        CORINFO_InstructionSetFlags compSupportsISAExactly;

        void setSupportedISAs(CORINFO_InstructionSetFlags isas)
        {
            compSupportsISA = isas;
        }

        unsigned compFlags; // method attributes
        unsigned instrCount;
        unsigned lvRefCount;

        codeOptimize compCodeOpt; // what type of code optimizations

#if defined(TARGET_XARCH)
        uint32_t preferredVectorByteLength;
#endif // TARGET_XARCH

// optimize maximally and/or favor speed over size?

#define DEFAULT_MIN_OPTS_CODE_SIZE 60000
#define DEFAULT_MIN_OPTS_INSTR_COUNT 20000
#define DEFAULT_MIN_OPTS_BB_COUNT 2000
#define DEFAULT_MIN_OPTS_LV_NUM_COUNT 2000
#define DEFAULT_MIN_OPTS_LV_REF_COUNT 8000

// Maximum number of locals before turning off the inlining
#define MAX_LV_NUM_COUNT_FOR_INLINING 512

        bool compMinOpts;
        bool compMinOptsIsSet;
#ifdef DEBUG
        mutable bool compMinOptsIsUsed;

        bool MinOpts() const
        {
            assert(compMinOptsIsSet);
            compMinOptsIsUsed = true;
            return compMinOpts;
        }
        bool IsMinOptsSet() const
        {
            return compMinOptsIsSet;
        }
#else  // !DEBUG
        bool MinOpts() const
        {
            return compMinOpts;
        }
        bool IsMinOptsSet() const
        {
            return compMinOptsIsSet;
        }
#endif // !DEBUG

        bool OptimizationDisabled() const
        {
            return MinOpts() || compDbgCode;
        }
        bool OptimizationEnabled() const
        {
            return !OptimizationDisabled();
        }

        void SetMinOpts(bool val)
        {
            assert(!compMinOptsIsUsed);
            assert(!compMinOptsIsSet || (compMinOpts == val));
            compMinOpts      = val;
            compMinOptsIsSet = true;
        }

        // true if the CLFLG_* for an optimization is set.
        bool OptEnabled(unsigned optFlag) const
        {
            return !!(compFlags & optFlag);
        }

#ifdef FEATURE_READYTORUN
        bool IsReadyToRun() const
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_READYTORUN);
        }
#else
        bool IsReadyToRun() const
        {
            return false;
        }
#endif

        // Check if the compilation is control-flow guard enabled.
        bool IsCFGEnabled() const
        {
#if defined(TARGET_ARM64) || defined(TARGET_AMD64)
            // On these platforms we assume the register that the target is
            // passed in is preserved by the validator and take care to get the
            // target from the register for the call (even in debug mode).
            static_assert_no_msg((RBM_VALIDATE_INDIRECT_CALL_TRASH & (1 << REG_VALIDATE_INDIRECT_CALL_ADDR)) == 0);
            if (JitConfig.JitForceControlFlowGuard())
                return true;

            return jitFlags->IsSet(JitFlags::JIT_FLAG_ENABLE_CFG);
#else
            // The remaining platforms are not supported and would require some
            // work to support.
            //
            // ARM32:
            //   The ARM32 validator does not preserve any volatile registers
            //   which means we have to take special care to allocate and use a
            //   callee-saved register (reloading the target from memory is a
            //   security issue).
            //
            // x86:
            //   On x86 some VSD calls disassemble the call site and expect an
            //   indirect call which is fundamentally incompatible with CFG.
            //   This would require a different way to pass this information
            //   through.
            //
            return false;
#endif
        }

#ifdef FEATURE_ON_STACK_REPLACEMENT
        bool IsOSR() const
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_OSR);
        }
#else
        bool IsOSR() const
        {
            return false;
        }
#endif

        bool IsTier0() const
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0);
        }

        bool IsInstrumented() const
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_BBINSTR);
        }

        bool IsOptimizedWithProfile() const
        {
            return OptimizationEnabled() && jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT);
        }

        bool IsInstrumentedAndOptimized() const
        {
            return IsInstrumented() && jitFlags->IsSet(JitFlags::JIT_FLAG_BBOPT);
        }

        bool DoEarlyBlockMerging() const
        {
            if (jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_EnC) || jitFlags->IsSet(JitFlags::JIT_FLAG_DEBUG_CODE))
            {
                return false;
            }

            if (jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT) && !jitFlags->IsSet(JitFlags::JIT_FLAG_TIER0))
            {
                return false;
            }

            return true;
        }

        // true if we should use the PINVOKE_{BEGIN,END} helpers instead of generating
        // PInvoke transitions inline. Normally used by R2R, but also used when generating a reverse pinvoke frame, as
        // the current logic for frame setup initializes and pushes
        // the InlinedCallFrame before performing the Reverse PInvoke transition, which is invalid (as frames cannot
        // safely be pushed/popped while the thread is in a preemptive state.).
        bool ShouldUsePInvokeHelpers()
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_USE_PINVOKE_HELPERS) ||
                   jitFlags->IsSet(JitFlags::JIT_FLAG_REVERSE_PINVOKE);
        }

        // true if we should use insert the REVERSE_PINVOKE_{ENTER,EXIT} helpers in the method
        // prolog/epilog
        bool IsReversePInvoke()
        {
            return jitFlags->IsSet(JitFlags::JIT_FLAG_REVERSE_PINVOKE);
        }

        bool compScopeInfo; // Generate the LocalVar info ?
        bool compDbgCode;   // Generate debugger-friendly code?
        bool compDbgInfo;   // Gather debugging info?
        bool compDbgEnC;

#ifdef PROFILING_SUPPORTED
        bool compNoPInvokeInlineCB;
#else
        static const bool compNoPInvokeInlineCB;
#endif

#ifdef DEBUG
        bool compGcChecks; // Check arguments and return values to ensure they are sane
#endif

#if defined(DEBUG) && defined(TARGET_XARCH)

        bool compStackCheckOnRet; // Check stack pointer on return to ensure it is correct.

#endif // defined(DEBUG) && defined(TARGET_XARCH)

#if defined(DEBUG) && defined(TARGET_X86)

        bool compStackCheckOnCall; // Check stack pointer after call to ensure it is correct. Only for x86.

#endif // defined(DEBUG) && defined(TARGET_X86)

        bool compReloc; // Generate relocs for pointers in code, true for all ngen/prejit codegen

#ifdef DEBUG
#if defined(TARGET_XARCH)
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

#ifdef OPT_CONFIG
        bool optRepeat; // Repeat optimizer phases k times
#endif

        bool disAsm;       // Display native code as it is generated
        bool disTesting;   // Display BEGIN METHOD/END METHOD anchors for disasm testing
        bool dspDiffable;  // Makes the Jit Dump 'diff-able' (currently uses same DOTNET_* flag as disDiffable)
        bool disDiffable;  // Makes the Disassembly code 'diff-able'
        bool disAlignment; // Display alignment boundaries in disassembly code
        bool disCodeBytes; // Display instruction code bytes in disassembly code
#ifdef DEBUG
        bool compProcedureSplittingEH; // Separate cold code from hot code for functions with EH
        bool dspCode;                  // Display native code generated
        bool dspEHTable;               // Display the EH table reported to the VM
        bool dspDebugInfo;             // Display the Debug info reported to the VM
        bool dspInstrs;                // Display the IL instructions intermixed with the native code output
        bool dspLines;                 // Display source-code lines intermixed with native code output
        bool varNames;                 // Display variables names in native code output
        bool disAsmSpilled;            // Display native code when any register spilling occurs
        bool disasmWithGC;             // Display GC info interleaved with disassembly.
        bool disAddr;                  // Display process address next to each instruction in disassembly code
        bool disAsm2;                  // Display native code after it is generated using external disassembler
        bool dspOrder;                 // Display names of each of the methods that we ngen/jit
        bool dspUnwind;                // Display the unwind info output
        bool compLongAddress;          // Force using large pseudo instructions for long address
                                       // (IF_LARGEJMP/IF_LARGEADR/IF_LARGLDC)
        bool dspGCtbls;                // Display the GC tables
        bool dspMetrics;               // Display metrics
#endif

// Default numbers used to perform loop alignment. All the numbers are chosen
// based on experimenting with various benchmarks.

// Default minimum loop block weight required to enable loop alignment.
#define DEFAULT_ALIGN_LOOP_MIN_BLOCK_WEIGHT 3

// By default a loop will be aligned at 32B address boundary to get better
// performance as per architecture manuals.
#define DEFAULT_ALIGN_LOOP_BOUNDARY 0x20

// For non-adaptive loop alignment, by default, only align a loop whose size is
// at most 3 times the alignment block size. If the loop is bigger than that, it is most
// likely complicated enough that loop alignment will not impact performance.
#define DEFAULT_MAX_LOOPSIZE_FOR_ALIGN DEFAULT_ALIGN_LOOP_BOUNDARY * 3

// By default only loops with a constant iteration count less than or equal to this will be unrolled
#define DEFAULT_UNROLL_LOOP_MAX_ITERATION_COUNT 4

#ifdef DEBUG
        // Loop alignment variables

        // If set, for non-adaptive alignment, ensure loop jmps are not on or cross alignment boundary.
        bool compJitAlignLoopForJcc;
#endif
        // For non-adaptive alignment, minimum loop size (in bytes) for which alignment will be done.
        unsigned short compJitAlignLoopMaxCodeSize;

        // Minimum weight needed for the first block of a loop to make it a candidate for alignment.
        unsigned short compJitAlignLoopMinBlockWeight;

        // For non-adaptive alignment, address boundary (power of 2) at which loop alignment should
        // be done. By default, 32B.
        unsigned short compJitAlignLoopBoundary;

        // Padding limit to align a loop.
        unsigned short compJitAlignPaddingLimit;

        // If set, perform adaptive loop alignment that limits number of padding based on loop size.
        bool compJitAlignLoopAdaptive;

        // If set, tries to hide alignment instructions behind unconditional jumps.
        bool compJitHideAlignBehindJmp;

        // If set, tracks the hidden return buffer for struct arg.
        bool compJitOptimizeStructHiddenBuffer;

        // Iteration limit to unroll a loop.
        unsigned short compJitUnrollLoopMaxIterationCount;

#ifdef LATE_DISASM
        bool doLateDisasm; // Run the late disassembler
#endif                     // LATE_DISASM

#if DUMP_GC_TABLES && !defined(DEBUG)
#pragma message("NOTE: this non-debug build has GC ptr table dumping always enabled!")
        static const bool dspGCtbls = true;
#endif

#ifdef PROFILING_SUPPORTED
        // Whether to emit Enter/Leave/TailCall hooks using a dummy stub (DummyProfilerELTStub()).
        // This option helps make the JIT behave as if it is running under a profiler.
        bool compJitELTHookEnabled;
#endif // PROFILING_SUPPORTED

#if FEATURE_TAILCALL_OPT
        // Whether opportunistic or implicit tail call optimization is enabled.
        bool compTailCallOpt;
        // Whether optimization of transforming a recursive tail call into a loop is enabled.
        bool compTailCallLoopOpt;
#endif

#if FEATURE_FASTTAILCALL
        // Whether fast tail calls are allowed.
        bool compFastTailCalls;
#endif // FEATURE_FASTTAILCALL

#if defined(TARGET_ARM64)
        // Decision about whether to save FP/LR registers with callee-saved registers (see
        // DOTNET_JitSaveFpLrWithCalleSavedRegisters).
        int compJitSaveFpLrWithCalleeSavedRegisters;
#endif // defined(TARGET_ARM64)

#ifdef CONFIGURABLE_ARM_ABI
        bool compUseSoftFP = false;
#else
#ifdef ARM_SOFTFP
        static const bool compUseSoftFP = true;
#else  // !ARM_SOFTFP
        static const bool compUseSoftFP = false;
#endif // ARM_SOFTFP
#endif // CONFIGURABLE_ARM_ABI

        // Collect 64 bit counts for PGO data.
        bool compCollect64BitCounts;

    } opts;

    static bool                s_pAltJitExcludeAssembliesListInitialized;
    static AssemblyNamesList2* s_pAltJitExcludeAssembliesList;

#ifdef DEBUG
    static bool                s_pJitDisasmIncludeAssembliesListInitialized;
    static AssemblyNamesList2* s_pJitDisasmIncludeAssembliesList;

    static bool       s_pJitFunctionFileInitialized;
    static MethodSet* s_pJitMethodSet;

// silence warning of cast to greater size. It is easier to silence than construct code the compiler is happy with, and
// it is safe in this case
#pragma warning(push)
#pragma warning(disable : 4312)

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
#pragma warning(pop)
#else
#pragma warning(push)
#pragma warning(disable : 4312)
    template <typename T>
    T dspPtr(T p)
    {
        return p;
    }

    template <typename T>
    T dspOffset(T o)
    {
        return o;
    }
#pragma warning(pop)
#endif

#ifdef DEBUG

    static int dspTreeID(GenTree* tree)
    {
        return tree->gtTreeID;
    }

    static void printStmtID(Statement* stmt)
    {
        assert(stmt != nullptr);
        printf(FMT_STMT, stmt->GetID());
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

    const char* devirtualizationDetailToString(CORINFO_DEVIRTUALIZATION_DETAIL detail);

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
        STRESS_MODE(REGS)                                                                       \
        STRESS_MODE(DBL_ALN)                                                                    \
        STRESS_MODE(LCL_FLDS)                                                                   \
        STRESS_MODE(UNROLL_LOOPS)                                                               \
        STRESS_MODE(MAKE_CSE)                                                                   \
        STRESS_MODE(LEGACY_INLINE)                                                              \
        STRESS_MODE(CLONE_EXPR)                                                                 \
        STRESS_MODE(FOLD)                                                                       \
        STRESS_MODE(MERGED_RETURNS)                                                             \
        STRESS_MODE(BB_PROFILE)                                                                 \
        STRESS_MODE(OPT_BOOLS_GC)                                                               \
        STRESS_MODE(OPT_BOOLS_COMPARE_CHAIN_COST)                                               \
        STRESS_MODE(REMORPH_TREES)                                                              \
        STRESS_MODE(64RSLT_MUL)                                                                 \
        STRESS_MODE(DO_WHILE_LOOPS)                                                             \
        STRESS_MODE(MIN_OPTS)                                                                   \
        STRESS_MODE(REVERSE_FLAG)     /* Will set GTF_REVERSE_OPS whenever we can */            \
        STRESS_MODE(TAILCALL)         /* Will make the call as a tailcall whenever legal */     \
        STRESS_MODE(CATCH_ARG)        /* Will spill catch arg */                                \
        STRESS_MODE(UNSAFE_BUFFER_CHECKS)                                                       \
        STRESS_MODE(NULL_OBJECT_CHECK)                                                          \
        STRESS_MODE(RANDOM_INLINE)                                                              \
        STRESS_MODE(SWITCH_CMP_BR_EXPANSION)                                                    \
        STRESS_MODE(GENERIC_VARN)                                                               \
        STRESS_MODE(PROFILER_CALLBACKS) /* Will generate profiler hooks for ELT callbacks */    \
        STRESS_MODE(BYREF_PROMOTION) /* Change undoPromotion decisions for byrefs */            \
        STRESS_MODE(PROMOTE_FEWER_STRUCTS)/* Don't promote some structs that can be promoted */ \
        STRESS_MODE(VN_BUDGET)/* Randomize the VN budget */                                     \
        STRESS_MODE(SSA_INFO) /* Select lower thresholds for "complex" SSA num encoding */      \
        STRESS_MODE(SPLIT_TREES_RANDOMLY) /* Split all statements at a random tree */           \
        STRESS_MODE(SPLIT_TREES_REMOVE_COMMAS) /* Remove all GT_COMMA nodes */                  \
        STRESS_MODE(NO_OLD_PROMOTION) /* Do not use old promotion */                            \
        STRESS_MODE(PHYSICAL_PROMOTION) /* Use physical promotion */                            \
        STRESS_MODE(PHYSICAL_PROMOTION_COST)                                                    \
        STRESS_MODE(UNWIND) /* stress unwind info; e.g., create function fragments */           \
                                                                                                \
        /* After COUNT_VARN, stress level 2 does all of these all the time */                   \
                                                                                                \
        STRESS_MODE(COUNT_VARN)                                                                 \
                                                                                                \
        /* "Check" stress areas that can be exhaustively used if we */                          \
        /*  dont care about performance at all */                                               \
                                                                                                \
        STRESS_MODE(FORCE_INLINE) /* Treat every method as AggressiveInlining */                \
        STRESS_MODE(CHK_FLOW_UPDATE)                                                            \
        STRESS_MODE(EMITTER)                                                                    \
        STRESS_MODE(CHK_REIMPORT)                                                               \
        STRESS_MODE(GENERIC_CHECK)                                                              \
        STRESS_MODE(IF_CONVERSION_COST)                                                         \
        STRESS_MODE(IF_CONVERSION_INNER_LOOPS)                                                  \
        STRESS_MODE(POISON_IMPLICIT_BYREFS)                                                     \
        STRESS_MODE(STORE_BLOCK_UNROLLING)                                                      \
        STRESS_MODE(COUNT)

    enum                compStressArea
    {
#define STRESS_MODE(mode) STRESS_##mode,
        STRESS_MODES
#undef STRESS_MODE
    };
// clang-format on

#ifdef DEBUG
    static const LPCWSTR s_compStressModeNamesW[STRESS_COUNT + 1];
    static const char*   s_compStressModeNames[STRESS_COUNT + 1];
    BYTE                 compActiveStressModes[STRESS_COUNT];
#endif // DEBUG

#define MAX_STRESS_WEIGHT 100

    bool compStressCompile(compStressArea stressArea, unsigned weightPercentage);
    bool compStressCompileHelper(compStressArea stressArea, unsigned weightPercentage);
    static unsigned compStressAreaHash(compStressArea area);

#ifdef DEBUG

    bool compInlineStress()
    {
        return compStressCompile(STRESS_LEGACY_INLINE, 50);
    }

    bool compRandomInlineStress()
    {
        return compStressCompile(STRESS_RANDOM_INLINE, 50);
    }

    bool compPromoteFewerStructs(unsigned lclNum);

#endif // DEBUG

    bool compTailCallStress()
    {
#ifdef DEBUG
        // Do not stress tailcalls in IL stubs as the runtime creates several IL
        // stubs to implement the tailcall mechanism, which would then
        // recursively create more IL stubs.
        return !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_IL_STUB) &&
               (JitConfig.TailcallStress() != 0 || compStressCompile(STRESS_TAILCALL, 5));
#else
        return false;
#endif
    }

    const char* compGetTieringName(bool wantShortName = false) const;
    const char* compGetPgoSourceName() const;
    const char* compGetStressMessage() const;

    codeOptimize compCodeOpt() const
    {
#if 0
        // Switching between size & speed has measurable throughput impact
        // (3.5% on NGen CoreLib when measured). It used to be enabled for
        // DEBUG, but should generate identical code between CHK & RET builds,
        // so that's not acceptable.
        // TODO-Throughput: Figure out what to do about size vs. speed & throughput.
        //                  Investigate the cause of the throughput regression.

        return opts.compCodeOpt;
#else
        return BLENDED_CODE;
#endif
    }

    //--------------------- Info about the procedure --------------------------

    struct Info
    {
        COMP_HANDLE           compCompHnd;
        CORINFO_MODULE_HANDLE compScopeHnd;
        CORINFO_CLASS_HANDLE  compClassHnd;
        CORINFO_METHOD_HANDLE compMethodHnd;
        CORINFO_METHOD_INFO*  compMethodInfo;

        bool hasCircularClassConstraints;
        bool hasCircularMethodConstraints;

#if defined(DEBUG) || defined(LATE_DISASM) || DUMP_FLOWGRAPHS || DUMP_GC_TABLES

        const char* compMethodName;
        const char* compClassName;
        const char* compFullName;
        double      compPerfScore;
        int         compMethodSuperPMIIndex; // useful when debugging under SuperPMI

#endif // defined(DEBUG) || defined(LATE_DISASM) || DUMP_FLOWGRAPHS

#if defined(DEBUG)
        // Method hash is logically const, but computed
        // on first demand.
        mutable unsigned compMethodHashPrivate;
        unsigned         compMethodHash() const;
#endif // defined(DEBUG)

#ifdef PSEUDORANDOM_NOP_INSERTION
        // things for pseudorandom nop insertion
        unsigned  compChecksum;
        CLRRandom compRNG;
#endif

        // The following holds the FLG_xxxx flags for the method we're compiling.
        unsigned compFlags;

        // The following holds the class attributes for the method we're compiling.
        unsigned compClassAttr;

        const BYTE*     compCode;
        IL_OFFSET       compILCodeSize;     // The IL code size
        IL_OFFSET       compILImportSize;   // Estimated amount of IL actually imported
        IL_OFFSET       compILEntry;        // The IL entry point (normally 0)
        PatchpointInfo* compPatchpointInfo; // Patchpoint data for OSR (normally nullptr)
        UNATIVE_OFFSET  compNativeCodeSize; // The native code size, after instructions are issued. This
        // is less than (compTotalHotCodeSize + compTotalColdCodeSize) only if:
        // (1) the code is not hot/cold split, and we issued less code than we expected, or
        // (2) the code is hot/cold split, and we issued less code than we expected
        // in the cold section (the hot section will always be padded out to compTotalHotCodeSize).

        bool compIsStatic : 1;           // Is the method static (no 'this' pointer)?
        bool compIsVarArgs : 1;          // Does the method have varargs parameters?
        bool compInitMem : 1;            // Is the CORINFO_OPT_INIT_LOCALS bit set in the method info options?
        bool compProfilerCallback : 1;   // JIT inserted a profiler Enter callback
        bool compPublishStubParam : 1;   // EAX captured in prolog will be available through an intrinsic
        bool compHasNextCallRetAddr : 1; // The NextCallReturnAddress intrinsic is used.

        var_types compRetType;       // Return type of the method as declared in IL (including SIMD normalization)
        var_types compRetNativeType; // Normalized return type as per target arch ABI
        unsigned  compILargsCount;   // Number of arguments (incl. implicit but not hidden)
        unsigned  compArgsCount;     // Number of arguments (incl. implicit and     hidden)

#if FEATURE_FASTTAILCALL
        unsigned compArgStackSize; // Incoming argument stack size in bytes
#endif                             // FEATURE_FASTTAILCALL

        unsigned compRetBuffArg; // position of hidden return param var (0, 1) (BAD_VAR_NUM means not present);
        int compTypeCtxtArg; // position of hidden param for type context for generic code (CORINFO_CALLCONV_PARAMTYPE)
        unsigned       compThisArg; // position of implicit this pointer param (not to be confused with lvaArg0Var)
        unsigned       compILlocalsCount; // Number of vars : args + locals (incl. implicit but not hidden)
        unsigned       compLocalsCount;   // Number of vars : args + locals (incl. implicit and     hidden)
        unsigned       compMaxStack;
        UNATIVE_OFFSET compTotalHotCodeSize;  // Total number of bytes of Hot Code in the method
        UNATIVE_OFFSET compTotalColdCodeSize; // Total number of bytes of Cold Code in the method

        unsigned compUnmanagedCallCountWithGCTransition; // count of unmanaged calls with GC transition.

        CorInfoCallConvExtension compCallConv; // The entry-point calling convention for this method.

        unsigned compLvFrameListRoot; // lclNum for the Frame root
        unsigned compXcptnsCount;     // Number of exception-handling clauses read in the method's IL.
                                      // You should generally use compHndBBtabCount instead: it is the
                                      // current number of EH clauses (after additions like synchronized
        // methods and funclets, and removals like unreachable code deletion).

        Target::ArgOrder compArgOrder;

        bool compMatchedVM; // true if the VM is "matched": either the JIT is a cross-compiler
                            // and the VM expects that, or the JIT is a "self-host" compiler
                            // (e.g., x86 hosted targeting x86) and the VM expects that.

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

        // Number of class profile probes in this method
        unsigned compHandleHistogramProbeCount;

#ifdef TARGET_ARM64
        bool compNeedsConsecutiveRegisters;
#endif

    } info;

#if defined(DEBUG)
    // Are we running a replay under SuperPMI?
    bool RunningSuperPmiReplay() const
    {
        return info.compMethodSuperPMIIndex != -1;
    }
#endif // DEBUG

    ReturnTypeDesc compRetTypeDesc; // ABI return type descriptor for the method

    //------------------------------------------------------------------------
    // compMethodHasRetVal: Does this method return some kind of value?
    //
    // Return Value:
    //    If this method returns a struct via a return buffer, whether that
    //    buffer's address needs to be returned, otherwise whether signature
    //    return type is not "TYP_VOID".
    //
    bool compMethodHasRetVal() const
    {
        return (info.compRetBuffArg != BAD_VAR_NUM) ? compMethodReturnsRetBufAddr() : (info.compRetType != TYP_VOID);
    }

    // Returns true if the method being compiled returns RetBuf addr as its return value
    bool compMethodReturnsRetBufAddr() const
    {
        // There are cases where implicit RetBuf argument should be explicitly returned in a register.
        // In such cases the return type is changed to TYP_BYREF and appropriate IR is generated.
        // These cases are:
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef TARGET_AMD64
        // 1. on x64 Windows and Unix the address of RetBuf needs to be returned by
        //    methods with hidden RetBufArg in RAX. In such case GT_RETURN is of TYP_BYREF,
        //    returning the address of RetBuf.
        return (info.compRetBuffArg != BAD_VAR_NUM);
#else // TARGET_AMD64
#ifdef PROFILING_SUPPORTED
        // 2.  Profiler Leave callback expects the address of retbuf as return value for
        //    methods with hidden RetBuf argument.  impReturnInstruction() when profiler
        //    callbacks are needed creates GT_RETURN(TYP_BYREF, op1 = Addr of RetBuf) for
        //    methods with hidden RetBufArg.
        if (compIsProfilerHookNeeded())
        {
            return (info.compRetBuffArg != BAD_VAR_NUM);
        }
#endif
        // 3. Windows ARM64 native instance calling convention requires the address of RetBuff
        //    to be returned in x0.
        CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_ARM64)
        if (TargetOS::IsWindows)
        {
            auto callConv = info.compCallConv;
            if (callConvIsInstanceMethodCallConv(callConv))
            {
                return (info.compRetBuffArg != BAD_VAR_NUM);
            }
        }
#endif // TARGET_ARM64
        // 4. x86 unmanaged calling conventions require the address of RetBuff to be returned in eax.
        CLANG_FORMAT_COMMENT_ANCHOR;
#if defined(TARGET_X86)
        if (info.compCallConv != CorInfoCallConvExtension::Managed)
        {
            return (info.compRetBuffArg != BAD_VAR_NUM);
        }
#endif

        return false;
#endif // TARGET_AMD64
    }

    //------------------------------------------------------------------------
    // compMethodReturnsMultiRegRetType: Does this method return a multi-reg value?
    //
    // Return Value:
    //    If this method returns a value in multiple registers, "true", "false"
    //    otherwise.
    //
    bool compMethodReturnsMultiRegRetType() const
    {
        return compRetTypeDesc.IsMultiRegRetType();
    }

    bool compEnregLocals()
    {
        return ((opts.compFlags & CLFLG_REGVAR) != 0);
    }

    bool compEnregStructLocals()
    {
        return (JitConfig.JitEnregStructLocals() != 0);
    }

    bool compObjectStackAllocation()
    {
        return (JitConfig.JitObjectStackAllocation() != 0);
    }

    // Returns true if the method requires a PInvoke prolog and epilog
    bool compMethodRequiresPInvokeFrame()
    {
        return (info.compUnmanagedCallCountWithGCTransition > 0);
    }

    // Returns true if address-exposed user variables should be poisoned with a recognizable value
    bool compShouldPoisonFrame()
    {
#ifdef FEATURE_ON_STACK_REPLACEMENT
        if (opts.IsOSR())
            return false;
#endif
        return !info.compInitMem && opts.compDbgCode;
    }

    // Returns true if the jit supports having patchpoints in this method.
    // Optionally, get the reason why not.
    bool compCanHavePatchpoints(const char** reason = nullptr);

#if defined(DEBUG)

    void compDispLocalVars();

#endif // DEBUG

private:
    class ClassLayoutTable* m_classLayoutTable;

    class ClassLayoutTable* typCreateClassLayoutTable();
    class ClassLayoutTable* typGetClassLayoutTable();

public:
    // Get the layout having the specified layout number.
    ClassLayout* typGetLayoutByNum(unsigned layoutNum);
    // Get the layout number of the specified layout.
    unsigned typGetLayoutNum(ClassLayout* layout);
    // Get the layout having the specified size but no class handle.
    ClassLayout* typGetBlkLayout(unsigned blockSize);
    // Get the number of a layout having the specified size but no class handle.
    unsigned typGetBlkLayoutNum(unsigned blockSize);
    // Get the layout for the specified class handle.
    ClassLayout* typGetObjLayout(CORINFO_CLASS_HANDLE classHandle);
    // Get the number of a layout for the specified class handle.
    unsigned typGetObjLayoutNum(CORINFO_CLASS_HANDLE classHandle);

    var_types TypeHandleToVarType(CORINFO_CLASS_HANDLE handle, ClassLayout** pLayout = nullptr);
    var_types TypeHandleToVarType(CorInfoType jitType, CORINFO_CLASS_HANDLE handle, ClassLayout** pLayout = nullptr);

//-------------------------- Global Compiler Data ------------------------------------

#ifdef DEBUG
private:
    static LONG s_compMethodsCount; // to produce unique label names
#endif

public:
#ifdef DEBUG
    unsigned compGenTreeID;
    unsigned compStatementID;
    unsigned compBasicBlockID;
#endif
    LONG compMethodID;

    BasicBlock* compCurBB;   // the current basic block in process
    Statement*  compCurStmt; // the current statement in process
    GenTree*    compCurTree; // the current tree in process

    //  The following is used to create the 'method JIT info' block.
    size_t compInfoBlkSize;
    BYTE*  compInfoBlkAddr;

    EHblkDsc* compHndBBtab;           // array of EH data
    unsigned  compHndBBtabCount;      // element count of used elements in EH data array
    unsigned  compHndBBtabAllocCount; // element count of allocated elements in EH data array

#if !defined(FEATURE_EH_FUNCLETS)

    //-------------------------------------------------------------------------
    //  Tracking of region covered by the monitor in synchronized methods
    void* syncStartEmitCookie; // the emitter cookie for first instruction after the call to MON_ENTER
    void* syncEndEmitCookie;   // the emitter cookie for first instruction after the call to MON_EXIT

#endif // !FEATURE_EH_FUNCLETS

    Phases      mostRecentlyActivePhase; // the most recently active phase
    PhaseChecks activePhaseChecks;       // the currently active phase checks
    PhaseDumps  activePhaseDumps;        // the currently active phase dumps

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

#if defined(TARGET_XARCH)
    // Mask of callee saved float regs on stack.
    regMaskTP compCalleeFPRegsSavedMask;
#endif
#ifdef TARGET_AMD64
// Quirk for VS debug-launch scenario to work:
// Bytes of padding between save-reg area and locals.
#define VSQUIRK_STACK_PAD (2 * REGSIZE_BYTES)
    unsigned compVSQuirkStackPaddingNeeded;
#endif

    unsigned compArgSize; // total size of arguments in bytes (including register args (lvIsRegArg))

#if defined(TARGET_ARM) || defined(TARGET_RISCV64)
    bool compHasSplitParam;
#endif

    unsigned compMapILargNum(unsigned ILargNum);      // map accounting for hidden args
    unsigned compMapILvarNum(unsigned ILvarNum);      // map accounting for hidden args
    unsigned compMap2ILvarNum(unsigned varNum) const; // map accounting for hidden args

#if defined(TARGET_ARM64)
    struct FrameInfo
    {
        // Frame type (1-5)
        int frameType;

        // Distance from established (method body) SP to base of callee save area
        int calleeSaveSpOffset;

        // Amount to subtract from SP before saving (prolog) OR
        // to add to SP after restoring (epilog) callee saves
        int calleeSaveSpDelta;

        // Distance from established SP to where caller's FP was saved
        int offsetSpToSavedFp;
    } compFrameInfo;
#endif

    //-------------------------------------------------------------------------

    static void compStartup();  // One-time initialization
    static void compShutdown(); // One-time finalization

    void compInit(ArenaAllocator*       pAlloc,
                  CORINFO_METHOD_HANDLE methodHnd,
                  COMP_HANDLE           compHnd,
                  CORINFO_METHOD_INFO*  methodInfo,
                  InlineInfo*           inlineInfo);
    void compDone();

    static void compDisplayStaticSizes();

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
    // in debug.  (Perhaps should be under the control of a DOTNET_ flag.)
    // These should fail by asserting.
    void compDoComponentUnitTestsOnce();
#endif // DEBUG

    int compCompile(CORINFO_MODULE_HANDLE classPtr,
                    void**                methodCodePtr,
                    uint32_t*             methodCodeSize,
                    JitFlags*             compileFlags);
    void compCompileFinish();
    int compCompileHelper(CORINFO_MODULE_HANDLE classPtr,
                          COMP_HANDLE           compHnd,
                          CORINFO_METHOD_INFO*  methodInfo,
                          void**                methodCodePtr,
                          uint32_t*             methodCodeSize,
                          JitFlags*             compileFlag);

    ArenaAllocator* compGetArenaAllocator();

    void generatePatchpointInfo();

#if MEASURE_MEM_ALLOC
    static bool s_dspMemStats; // Display per-phase memory statistics for every function
#endif                         // MEASURE_MEM_ALLOC

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

#if TRACK_ENREG_STATS
    class EnregisterStats
    {
    private:
        unsigned m_totalNumberOfVars;
        unsigned m_totalNumberOfStructVars;
        unsigned m_totalNumberOfEnregVars;
        unsigned m_totalNumberOfStructEnregVars;

        unsigned m_addrExposed;
        unsigned m_hiddenStructArg;
        unsigned m_VMNeedsStackAddr;
        unsigned m_localField;
        unsigned m_blockOp;
        unsigned m_dontEnregStructs;
        unsigned m_notRegSizeStruct;
        unsigned m_structArg;
        unsigned m_lclAddrNode;
        unsigned m_castTakesAddr;
        unsigned m_storeBlkSrc;
        unsigned m_swizzleArg;
        unsigned m_blockOpRet;
        unsigned m_returnSpCheck;
        unsigned m_callSpCheck;
        unsigned m_simdUserForcesDep;
        unsigned m_liveInOutHndlr;
        unsigned m_depField;
        unsigned m_noRegVars;
        unsigned m_minOptsGC;
#ifdef JIT32_GCENCODER
        unsigned m_PinningRef;
#endif // JIT32_GCENCODER
#if !defined(TARGET_64BIT)
        unsigned m_longParamField;
#endif // !TARGET_64BIT
        unsigned m_parentExposed;
        unsigned m_tooConservative;
        unsigned m_escapeAddress;
        unsigned m_osrExposed;
        unsigned m_stressLclFld;
        unsigned m_dispatchRetBuf;
        unsigned m_wideIndir;
        unsigned m_stressPoisonImplicitByrefs;
        unsigned m_externallyVisibleImplicitly;

    public:
        void RecordLocal(const LclVarDsc* varDsc);
        void Dump(FILE* fout) const;
    };

    static EnregisterStats s_enregisterStats;
#endif // TRACK_ENREG_STATS

    bool compIsForInlining() const;
    bool compDonotInline();

#ifdef DEBUG
    // Get the default fill char value we randomize this value when JitStress is enabled.
    static unsigned char compGetJitDefaultFill(Compiler* comp);

    const char* compLocalVarName(unsigned varNum, unsigned offs);
    VarName compVarName(regNumber reg, bool isFloatReg = false);
    const char* compFPregVarName(unsigned fpReg, bool displayVar = false);
    void compDspSrcLinesByNativeIP(UNATIVE_OFFSET curIP);
    void compDspSrcLinesByLineNum(unsigned line, bool seek = false);
#endif // DEBUG
    const char* compRegNameForSize(regNumber reg, size_t size);
    const char* compRegVarName(regNumber reg, bool displayVar = false, bool isFloatReg = false);

    //-------------------------------------------------------------------------

    struct VarScopeListNode
    {
        VarScopeDsc*             data;
        VarScopeListNode*        next;
        static VarScopeListNode* Create(VarScopeDsc* value, CompAllocator alloc)
        {
            VarScopeListNode* node = new (alloc) VarScopeListNode;
            node->data             = value;
            node->next             = nullptr;
            return node;
        }
    };

    struct VarScopeMapInfo
    {
        VarScopeListNode*       head;
        VarScopeListNode*       tail;
        static VarScopeMapInfo* Create(VarScopeListNode* node, CompAllocator alloc)
        {
            VarScopeMapInfo* info = new (alloc) VarScopeMapInfo;
            info->head            = node;
            info->tail            = node;
            return info;
        }
    };

    // Max value of scope count for which we would use linear search; for larger values we would use hashtable lookup.
    static const unsigned MAX_LINEAR_FIND_LCL_SCOPELIST = 32;

    typedef JitHashTable<unsigned, JitSmallPrimitiveKeyFuncs<unsigned>, VarScopeMapInfo*> VarNumToScopeDscMap;

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

    bool compIsProfilerHookNeeded() const;

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

    ArenaAllocator* compArenaAllocator;

public:
    void compFunctionTraceStart();
    void compFunctionTraceEnd(void* methodCodePtr, ULONG methodCodeSize, bool isNYI);

protected:
    size_t compMaxUncheckedOffsetForNullObject;

    void compInitOptions(JitFlags* compileFlags);

    void compSetProcessor();
    void compInitDebuggingInfo();
    void compSetOptimizationLevel();
#if defined(TARGET_ARMARCH) || defined(TARGET_RISCV64)
    bool compRsvdRegCheck(FrameLayoutState curState);
#endif
    void compCompile(void** methodCodePtr, uint32_t* methodCodeSize, JitFlags* compileFlags);

    // Clear annotations produced during optimizations; to be used between iterations when repeating opts.
    void ResetOptAnnotations();

    // Regenerate flow graph annotations; to be used between iterations when repeating opts.
    void RecomputeFlowGraphAnnotations();

#ifdef PROFILING_SUPPORTED
    // Data required for generating profiler Enter/Leave/TailCall hooks

    bool  compProfilerHookNeeded; // Whether profiler Enter/Leave/TailCall hook needs to be generated for the method
    void* compProfilerMethHnd;    // Profiler handle of the method being compiled. Passed as param to ELT callbacks
    bool  compProfilerMethHndIndirected; // Whether compProfilerHandle is pointer to the handle or is an actual handle
#endif

public:
    // Assumes called as part of process shutdown; does any compiler-specific work associated with that.
    static void ProcessShutdownWork(ICorStaticInfo* statInfo);

    CompAllocator getAllocator(CompMemKind cmk = CMK_Generic)
    {
        return CompAllocator(compArenaAllocator, cmk);
    }

    CompAllocator getAllocatorGC()
    {
        return getAllocator(CMK_GC);
    }

    CompAllocator getAllocatorLoopHoist()
    {
        return getAllocator(CMK_LoopHoist);
    }

#ifdef DEBUG
    CompAllocator getAllocatorDebugOnly()
    {
        return getAllocator(CMK_DebugOnly);
    }
#endif // DEBUG

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
    EntryState verCurrentState;

    void verInitBBEntryState(BasicBlock* block, EntryState* currentState);

    void verInitCurrentState();
    void verResetCurrentState(BasicBlock* block, EntryState* currentState);

    void verConvertBBToThrowVerificationException(BasicBlock* block DEBUGARG(bool logMsg));
    void verHandleVerificationFailure(BasicBlock* block DEBUGARG(bool logMsg));
    typeInfo verMakeTypeInfoForLocal(unsigned lclNum);
    typeInfo verMakeTypeInfo(CORINFO_CLASS_HANDLE clsHnd); // converts from jit type representation to typeInfo
    typeInfo verMakeTypeInfo(CorInfoType          ciType,
                             CORINFO_CLASS_HANDLE clsHnd); // converts from jit type representation to typeInfo

    typeInfo verParseArgSigToTypeInfo(CORINFO_SIG_INFO* sig, CORINFO_ARG_LIST_HANDLE args);

    bool verCheckTailCallConstraint(OPCODE                  opcode,
                                    CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_RESOLVED_TOKEN* pConstrainedResolvedToken);

#ifdef DEBUG

    // One line log function. Default level is 0. Increasing it gives you
    // more log information

    // levels are currently unused: #define JITDUMP(level,...)                     ();
    void JitLogEE(unsigned level, const char* fmt, ...);

    bool compDebugBreak;

    bool compJitHaltMethod();

    void dumpRegMask(regMaskTP regs) const;

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
        unsigned      shadowCopy;  // Lcl var num, if not valid set to BAD_VAR_NUM

        static bool mayNeedShadowCopy(LclVarDsc* varDsc)
        {
#if defined(TARGET_AMD64)
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
            //   - Whenever a parameter passed in an argument register needs to be spilled by LSRA, we
            //     create a new spill temp if the method needs GS cookie check.
            return varDsc->lvIsParam;
#else // !defined(TARGET_AMD64)
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

    PhaseStatus gsPhase();
    void        gsGSChecksInitCookie();   // Grabs cookie variable
    void        gsCopyShadowParams();     // Identify vulnerable params and create dhadow copies
    bool        gsFindVulnerableParams(); // Shadow param analysis code
    void        gsParamsToShadows();      // Insert copy code and replave param uses by shadow

    static fgWalkPreFn gsMarkPtrsAndAssignGroups; // Shadow param analysis tree-walk
    static fgWalkPreFn gsReplaceShadowParams;     // Shadow param replacement tree-walk

#define DEFAULT_MAX_INLINE_SIZE 100 // Methods with >  DEFAULT_MAX_INLINE_SIZE IL bytes will never be inlined.
                                    // This can be overwritten by setting DOTNET_JITInlineSize env variable.

#define DEFAULT_MAX_INLINE_DEPTH 20 // Methods at more than this level deep will not be inlined

#define DEFAULT_MAX_FORCE_INLINE_DEPTH 1 // Methods at more than this level deep will not be force inlined

#define DEFAULT_MAX_LOCALLOC_TO_LOCAL_SIZE 32 // fixed locallocs of this size or smaller will convert to local buffers

private:
#ifdef FEATURE_JIT_METHOD_PERF
    JitTimer*                  pCompJitTimer;         // Timer data structure (by phases) for current compilation.
    static CompTimeSummaryInfo s_compJitTimerSummary; // Summary of the Timer information for the whole run.

    static LPCWSTR JitTimeLogCsv();        // Retrieve the file name for CSV from ConfigDWORD.
    static LPCWSTR compJitTimeLogFilename; // If a log file for JIT time is desired, filename to write it to.
#endif
    void BeginPhase(Phases phase); // Indicate the start of the given phase.
    void EndPhase(Phases phase);   // Indicate the end of the given phase.

#if MEASURE_CLRAPI_CALLS
    // Thin wrappers that call into JitTimer (if present).
    inline void CLRApiCallEnter(unsigned apix);
    inline void CLRApiCallLeave(unsigned apix);

public:
    inline void CLR_API_Enter(API_ICorJitInfo_Names ename);
    inline void CLR_API_Leave(API_ICorJitInfo_Names ename);

private:
#endif

#if defined(DEBUG)
    // These variables are associated with maintaining SQM data about compile time.
    unsigned __int64 m_compCyclesAtEndOfInlining; // The thread-virtualized cycle count at the end of the inlining phase
                                                  // in the current compilation.
    unsigned __int64 m_compCycles;                // Net cycle count for current compilation
    DWORD m_compTickCountAtEndOfInlining; // The result of GetTickCount() (# ms since some epoch marker) at the end of
                                          // the inlining phase in the current compilation.
#endif                                    // defined(DEBUG)

    // Records the SQM-relevant (cycles and tick count).  Should be called after inlining is complete.
    // (We do this after inlining because this marks the last point at which the JIT is likely to cause
    // type-loading and class initialization).
    void RecordStateAtEndOfInlining();
    // Assumes being called at the end of compilation.  Update the SQM state.
    void RecordStateAtEndOfCompilation();

public:
#if FUNC_INFO_LOGGING
    static LPCWSTR compJitFuncInfoFilename; // If a log file for per-function information is required, this is the
                                            // filename to write it to.
    static FILE* compJitFuncInfoFile;       // And this is the actual FILE* to write to.
#endif                                      // FUNC_INFO_LOGGING

    Compiler* prevCompiler; // Previous compiler on stack for TLS Compiler* linked list for reentrant compilers.

#if MEASURE_NOWAY
    void RecordNowayAssert(const char* filename, unsigned line, const char* condStr);
#endif // MEASURE_NOWAY

    // Should we actually fire the noway assert body and the exception handler?
    bool compShouldThrowOnNoway();

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

    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, int> NodeToIntMap;

    // Returns the set (i.e., the domain of the result map) of nodes that are keys in m_nodeTestData, and
    // currently occur in the AST graph.
    NodeToIntMap* FindReachableNodesInNodeTestData();

    // Node "from" is being eliminated, and being replaced by node "to".  If "from" had any associated
    // test data, associate that data with "to".
    void TransferTestDataToNode(GenTree* from, GenTree* to);

    // These are the methods that test that the various conditions implied by the
    // test attributes are satisfied.
    void JitTestCheckSSA(); // SSA builder tests.
    void JitTestCheckVN();  // Value numbering tests.
#endif                      // DEBUG

    FieldSeqStore* m_fieldSeqStore;

    FieldSeqStore* GetFieldSeqStore()
    {
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_fieldSeqStore == nullptr)
        {
            CompAllocator alloc       = getAllocator(CMK_FieldSeqStore);
            compRoot->m_fieldSeqStore = new (alloc) FieldSeqStore(alloc);
        }
        return compRoot->m_fieldSeqStore;
    }

    typedef JitHashTable<GenTree*, JitPtrKeyFuncs<GenTree>, unsigned> NodeToUnsignedMap;

    NodeToUnsignedMap* m_memorySsaMap[MemoryKindCount];

    // In some cases, we want to assign intermediate SSA #'s to memory states, and know what nodes create those memory
    // states. (We do this for try blocks, where, if the try block doesn't do a call that loses track of the memory
    // state, all the possible memory states are possible initial states of the corresponding catch block(s).)
    NodeToUnsignedMap* GetMemorySsaMap(MemoryKind memoryKind)
    {
        if (memoryKind == GcHeap && byrefStatesMatchGcHeapStates)
        {
            // Use the same map for GCHeap and ByrefExposed when their states match.
            memoryKind = ByrefExposed;
        }

        assert(memoryKind < MemoryKindCount);
        Compiler* compRoot = impInlineRoot();
        if (compRoot->m_memorySsaMap[memoryKind] == nullptr)
        {
            // Create a CompAllocator that labels sub-structure with CMK_MemorySsaMap, and use that for allocation.
            CompAllocator ialloc(getAllocator(CMK_MemorySsaMap));
            compRoot->m_memorySsaMap[memoryKind] = new (ialloc) NodeToUnsignedMap(ialloc);
        }
        return compRoot->m_memorySsaMap[memoryKind];
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

#ifdef UNIX_AMD64_ABI
    static var_types GetTypeFromClassificationAndSizes(SystemVClassificationType classType, int size);
    static var_types GetEightByteType(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                      unsigned                                                   slotNum);

    static void GetStructTypeOffset(const SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR& structDesc,
                                    var_types*                                                 type0,
                                    var_types*                                                 type1,
                                    unsigned __int8*                                           offset0,
                                    unsigned __int8*                                           offset1);

    void GetStructTypeOffset(CORINFO_CLASS_HANDLE typeHnd,
                             var_types*           type0,
                             var_types*           type1,
                             unsigned __int8*     offset0,
                             unsigned __int8*     offset1);

#endif // defined(UNIX_AMD64_ABI)

    void fgMorphMultiregStructArgs(GenTreeCall* call);
    GenTree* fgMorphMultiregStructArg(CallArg* arg);

    bool killGCRefs(GenTree* tree);

#if defined(TARGET_AMD64)
private:
    // The following are for initializing register allocator "constants" defined in targetamd64.h
    // that now depend upon runtime ISA information, e.g., the presence of AVX512F/VL, which increases
    // the number of SIMD (xmm, ymm, and zmm) registers from 16 to 32.
    // As only 64-bit xarch has the capability to have the additional registers, we limit the changes
    // to TARGET_AMD64 only.
    //
    // Users of these values need to define four accessor functions:
    //
    //    regMaskTP get_RBM_ALLFLOAT();
    //    regMaskTP get_RBM_FLT_CALLEE_TRASH();
    //    unsigned get_CNT_CALLEE_TRASH_FLOAT();
    //    unsigned get_AVAILABLE_REG_COUNT();
    //
    // which return the values of these variables.
    //
    // This was done to avoid polluting all `targetXXX.h` macro definitions with a compiler parameter, where only
    // TARGET_AMD64 requires one.
    //
    regMaskTP rbmAllFloat;
    regMaskTP rbmFltCalleeTrash;
    unsigned  cntCalleeTrashFloat;

public:
    FORCEINLINE regMaskTP get_RBM_ALLFLOAT() const
    {
        return this->rbmAllFloat;
    }
    FORCEINLINE regMaskTP get_RBM_FLT_CALLEE_TRASH() const
    {
        return this->rbmFltCalleeTrash;
    }
    FORCEINLINE unsigned get_CNT_CALLEE_TRASH_FLOAT() const
    {
        return this->cntCalleeTrashFloat;
    }

#endif // TARGET_AMD64

#if defined(TARGET_XARCH)
private:
    // The following are for initializing register allocator "constants" defined in targetamd64.h
    // that now depend upon runtime ISA information, e.g., the presence of AVX512F/VL, which adds
    // 8 mask registers for use.
    //
    // Users of these values need to define four accessor functions:
    //
    //    regMaskTP get_RBM_ALLMASK();
    //    regMaskTP get_RBM_MSK_CALLEE_TRASH();
    //    unsigned get_CNT_CALLEE_TRASH_MASK();
    //    unsigned get_AVAILABLE_REG_COUNT();
    //
    // which return the values of these variables.
    //
    // This was done to avoid polluting all `targetXXX.h` macro definitions with a compiler parameter, where only
    // TARGET_XARCH requires one.
    //
    regMaskTP rbmAllMask;
    regMaskTP rbmMskCalleeTrash;
    unsigned  cntCalleeTrashMask;
    regMaskTP varTypeCalleeTrashRegs[TYP_COUNT];

public:
    FORCEINLINE regMaskTP get_RBM_ALLMASK() const
    {
        return this->rbmAllMask;
    }
    FORCEINLINE regMaskTP get_RBM_MSK_CALLEE_TRASH() const
    {
        return this->rbmMskCalleeTrash;
    }
    FORCEINLINE unsigned get_CNT_CALLEE_TRASH_MASK() const
    {
        return this->cntCalleeTrashMask;
    }
#endif // TARGET_XARCH

}; // end of class Compiler

//---------------------------------------------------------------------------------------------------------------------
// GenTreeVisitor: a flexible tree walker implemented using the curiously-recurring-template pattern.
//
// This class implements a configurable walker for IR trees. There are five configuration options (defaults values are
// shown in parentheses):
//
// - ComputeStack (false): when true, the walker will push each node onto the `m_ancestors` stack. "Ancestors" is a bit
//                         of a misnomer, as the first entry will always be the current node.
//
// - DoPreOrder (false): when true, the walker will invoke `TVisitor::PreOrderVisit` with the current node as an
//                       argument before visiting the node's operands.
//
// - DoPostOrder (false): when true, the walker will invoke `TVisitor::PostOrderVisit` with the current node as an
//                        argument after visiting the node's operands.
//
// - DoLclVarsOnly (false): when true, the walker will only invoke `TVisitor::PreOrderVisit` for lclVar nodes.
//                          `DoPreOrder` must be true if this option is true.
//
// - UseExecutionOrder (false): when true, then walker will visit a node's operands in execution order (e.g. if a
//                              binary operator has the `GTF_REVERSE_OPS` flag set, the second operand will be
//                              visited before the first).
//
// At least one of `DoPreOrder` and `DoPostOrder` must be specified.
//
// A simple pre-order visitor might look something like the following:
//
//     class CountingVisitor final : public GenTreeVisitor<CountingVisitor>
//     {
//     public:
//         enum
//         {
//             DoPreOrder = true
//         };
//
//         unsigned m_count;
//
//         CountingVisitor(Compiler* compiler)
//             : GenTreeVisitor<CountingVisitor>(compiler), m_count(0)
//         {
//         }
//
//         Compiler::fgWalkResult PreOrderVisit(GenTree* node)
//         {
//             m_count++;
//         }
//     };
//
// This visitor would then be used like so:
//
//     CountingVisitor countingVisitor(compiler);
//     countingVisitor.WalkTree(root);
//
template <typename TVisitor>
class GenTreeVisitor
{
protected:
    typedef Compiler::fgWalkResult fgWalkResult;

    enum
    {
        ComputeStack      = false,
        DoPreOrder        = false,
        DoPostOrder       = false,
        DoLclVarsOnly     = false,
        UseExecutionOrder = false,
    };

    Compiler*            m_compiler;
    ArrayStack<GenTree*> m_ancestors;

    GenTreeVisitor(Compiler* compiler) : m_compiler(compiler), m_ancestors(compiler->getAllocator(CMK_ArrayStack))
    {
        assert(compiler != nullptr);

        static_assert_no_msg(TVisitor::DoPreOrder || TVisitor::DoPostOrder);
        static_assert_no_msg(!TVisitor::DoLclVarsOnly || TVisitor::DoPreOrder);
    }

    fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        return fgWalkResult::WALK_CONTINUE;
    }

    fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        return fgWalkResult::WALK_CONTINUE;
    }

public:
    fgWalkResult WalkTree(GenTree** use, GenTree* user)
    {
        assert(use != nullptr);

        GenTree* node = *use;

        if (TVisitor::ComputeStack)
        {
            m_ancestors.Push(node);
        }

        fgWalkResult result = fgWalkResult::WALK_CONTINUE;
        if (TVisitor::DoPreOrder && !TVisitor::DoLclVarsOnly)
        {
            result = reinterpret_cast<TVisitor*>(this)->PreOrderVisit(use, user);
            if (result == fgWalkResult::WALK_ABORT)
            {
                return result;
            }

            node = *use;
            if ((node == nullptr) || (result == fgWalkResult::WALK_SKIP_SUBTREES))
            {
                goto DONE;
            }
        }

        switch (node->OperGet())
        {
            // Leaf lclVars
            case GT_LCL_VAR:
            case GT_LCL_FLD:
            case GT_LCL_ADDR:
                if (TVisitor::DoLclVarsOnly)
                {
                    result = reinterpret_cast<TVisitor*>(this)->PreOrderVisit(use, user);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                FALLTHROUGH;

            // Leaf nodes
            case GT_CATCH_ARG:
            case GT_LABEL:
            case GT_FTN_ADDR:
            case GT_RET_EXPR:
            case GT_CNS_INT:
            case GT_CNS_LNG:
            case GT_CNS_DBL:
            case GT_CNS_STR:
            case GT_CNS_VEC:
            case GT_MEMORYBARRIER:
            case GT_JMP:
            case GT_JCC:
            case GT_SETCC:
            case GT_NO_OP:
            case GT_START_NONGC:
            case GT_START_PREEMPTGC:
            case GT_PROF_HOOK:
#if !defined(FEATURE_EH_FUNCLETS)
            case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            case GT_PHI_ARG:
            case GT_JMPTABLE:
            case GT_PHYSREG:
            case GT_EMITNOP:
            case GT_PINVOKE_PROLOG:
            case GT_PINVOKE_EPILOG:
            case GT_IL_OFFSET:
            case GT_NOP:
                break;

            // Lclvar unary operators
            case GT_STORE_LCL_VAR:
            case GT_STORE_LCL_FLD:
                if (TVisitor::DoLclVarsOnly)
                {
                    result = reinterpret_cast<TVisitor*>(this)->PreOrderVisit(use, user);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                FALLTHROUGH;

            // Standard unary operators
            case GT_NOT:
            case GT_NEG:
            case GT_BSWAP:
            case GT_BSWAP16:
            case GT_COPY:
            case GT_RELOAD:
            case GT_ARR_LENGTH:
            case GT_MDARR_LENGTH:
            case GT_MDARR_LOWER_BOUND:
            case GT_CAST:
            case GT_BITCAST:
            case GT_CKFINITE:
            case GT_LCLHEAP:
            case GT_IND:
            case GT_BLK:
            case GT_BOX:
            case GT_ALLOCOBJ:
            case GT_INIT_VAL:
            case GT_JTRUE:
            case GT_SWITCH:
            case GT_NULLCHECK:
            case GT_PUTARG_REG:
            case GT_PUTARG_STK:
            case GT_RETURNTRAP:
            case GT_FIELD_ADDR:
            case GT_RETURN:
            case GT_RETFILT:
            case GT_RUNTIMELOOKUP:
            case GT_ARR_ADDR:
            case GT_KEEPALIVE:
            case GT_INC_SATURATE:
            {
                GenTreeUnOp* const unOp = node->AsUnOp();
                if (unOp->gtOp1 != nullptr)
                {
                    result = WalkTree(&unOp->gtOp1, unOp);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                break;
            }

            // Special nodes
            case GT_PHI:
                for (GenTreePhi::Use& use : node->AsPhi()->Uses())
                {
                    result = WalkTree(&use.NodeRef(), node);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                break;

            case GT_FIELD_LIST:
                for (GenTreeFieldList::Use& use : node->AsFieldList()->Uses())
                {
                    result = WalkTree(&use.NodeRef(), node);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                break;

            case GT_CMPXCHG:
            {
                GenTreeCmpXchg* const cmpXchg = node->AsCmpXchg();

                result = WalkTree(&cmpXchg->Addr(), cmpXchg);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&cmpXchg->Data(), cmpXchg);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&cmpXchg->Comparand(), cmpXchg);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                break;
            }

            case GT_ARR_ELEM:
            {
                GenTreeArrElem* const arrElem = node->AsArrElem();

                result = WalkTree(&arrElem->gtArrObj, arrElem);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }

                const unsigned rank = arrElem->gtArrRank;
                for (unsigned dim = 0; dim < rank; dim++)
                {
                    result = WalkTree(&arrElem->gtArrInds[dim], arrElem);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                break;
            }

            case GT_STORE_DYN_BLK:
            {
                GenTreeStoreDynBlk* const dynBlock = node->AsStoreDynBlk();

                result = WalkTree(&dynBlock->gtOp1, dynBlock);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&dynBlock->gtOp2, dynBlock);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&dynBlock->gtDynamicSize, dynBlock);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                break;
            }

            case GT_CALL:
            {
                GenTreeCall* const call = node->AsCall();

                for (CallArg& arg : call->gtArgs.EarlyArgs())
                {
                    result = WalkTree(&arg.EarlyNodeRef(), call);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }

                for (CallArg& arg : call->gtArgs.LateArgs())
                {
                    result = WalkTree(&arg.LateNodeRef(), call);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }

                if (call->gtCallType == CT_INDIRECT)
                {
                    if (call->gtCallCookie != nullptr)
                    {
                        result = WalkTree(&call->gtCallCookie, call);
                        if (result == fgWalkResult::WALK_ABORT)
                        {
                            return result;
                        }
                    }

                    result = WalkTree(&call->gtCallAddr, call);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }

                if (call->gtControlExpr != nullptr)
                {
                    result = WalkTree(&call->gtControlExpr, call);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }

                break;
            }

#if defined(FEATURE_HW_INTRINSICS)
            case GT_HWINTRINSIC:
                if (TVisitor::UseExecutionOrder && node->IsReverseOp())
                {
                    assert(node->AsMultiOp()->GetOperandCount() == 2);
                    result = WalkTree(&node->AsMultiOp()->Op(2), node);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                    result = WalkTree(&node->AsMultiOp()->Op(1), node);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                else
                {
                    for (GenTree** use : node->AsMultiOp()->UseEdges())
                    {
                        result = WalkTree(use, node);
                        if (result == fgWalkResult::WALK_ABORT)
                        {
                            return result;
                        }
                    }
                }
                break;
#endif // defined(FEATURE_HW_INTRINSICS)

            case GT_SELECT:
            {
                GenTreeConditional* const conditional = node->AsConditional();

                result = WalkTree(&conditional->gtCond, conditional);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&conditional->gtOp1, conditional);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                result = WalkTree(&conditional->gtOp2, conditional);
                if (result == fgWalkResult::WALK_ABORT)
                {
                    return result;
                }
                break;
            }

            // Binary nodes
            default:
            {
                assert(node->OperIsBinary());

                GenTreeOp* const op = node->AsOp();

                GenTree** op1Use = &op->gtOp1;
                GenTree** op2Use = &op->gtOp2;

                if (TVisitor::UseExecutionOrder && node->IsReverseOp())
                {
                    std::swap(op1Use, op2Use);
                }

                if (*op1Use != nullptr)
                {
                    result = WalkTree(op1Use, op);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }

                if (*op2Use != nullptr)
                {
                    result = WalkTree(op2Use, op);
                    if (result == fgWalkResult::WALK_ABORT)
                    {
                        return result;
                    }
                }
                break;
            }
        }

    DONE:
        // Finally, visit the current node
        if (TVisitor::DoPostOrder)
        {
            result = reinterpret_cast<TVisitor*>(this)->PostOrderVisit(use, user);
        }

        if (TVisitor::ComputeStack)
        {
            m_ancestors.Pop();
        }

        return result;
    }
};

template <bool doPreOrder, bool doPostOrder, bool doLclVarsOnly, bool useExecutionOrder>
class GenericTreeWalker final
    : public GenTreeVisitor<GenericTreeWalker<doPreOrder, doPostOrder, doLclVarsOnly, useExecutionOrder>>
{
public:
    enum
    {
        ComputeStack      = false,
        DoPreOrder        = doPreOrder,
        DoPostOrder       = doPostOrder,
        DoLclVarsOnly     = doLclVarsOnly,
        UseExecutionOrder = useExecutionOrder,
    };

private:
    Compiler::fgWalkData* m_walkData;

public:
    GenericTreeWalker(Compiler::fgWalkData* walkData)
        : GenTreeVisitor<GenericTreeWalker<doPreOrder, doPostOrder, doLclVarsOnly, useExecutionOrder>>(
              walkData->compiler)
        , m_walkData(walkData)
    {
        assert(walkData != nullptr);
    }

    Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        m_walkData->parent = user;
        return m_walkData->wtprVisitorFn(use, m_walkData);
    }

    Compiler::fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
    {
        m_walkData->parent = user;
        return m_walkData->wtpoVisitorFn(use, m_walkData);
    }
};

// A dominator tree visitor implemented using the curiously-recurring-template pattern, similar to GenTreeVisitor.
template <typename TVisitor>
class DomTreeVisitor
{
    friend class FlowGraphDominatorTree;

protected:
    Compiler* m_compiler;

    DomTreeVisitor(Compiler* compiler) : m_compiler(compiler)
    {
    }

    void Begin()
    {
    }

    void PreOrderVisit(BasicBlock* block)
    {
    }

    void PostOrderVisit(BasicBlock* block)
    {
    }

    void End()
    {
    }

private:
    void WalkTree(const DomTreeNode* tree)
    {
        static_cast<TVisitor*>(this)->Begin();

        for (BasicBlock *next, *block = m_compiler->fgFirstBB; block != nullptr; block = next)
        {
            static_cast<TVisitor*>(this)->PreOrderVisit(block);

            next = tree[block->bbPostorderNum].firstChild;

            if (next != nullptr)
            {
                assert(next->bbIDom == block);
                continue;
            }

            do
            {
                static_cast<TVisitor*>(this)->PostOrderVisit(block);

                next = tree[block->bbPostorderNum].nextSibling;

                if (next != nullptr)
                {
                    assert(next->bbIDom == block->bbIDom);
                    break;
                }

                block = block->bbIDom;

            } while (block != nullptr);
        }

        static_cast<TVisitor*>(this)->End();
    }

public:
    //------------------------------------------------------------------------
    // WalkTree: Walk the dominator tree.
    //
    // Parameter:
    //    domTree - Dominator tree.
    //
    // Notes:
    //    This performs a non-recursive, non-allocating walk of the dominator
    //    tree.
    //
    void WalkTree(const FlowGraphDominatorTree* domTree)
    {
        WalkTree(domTree->m_domTree);
    }
};

// EHClauses: adapter class for forward iteration of the exception handling table using range-based `for`, e.g.:
//    for (EHblkDsc* const ehDsc : EHClauses(compiler))
//
class EHClauses
{
    EHblkDsc* m_begin;
    EHblkDsc* m_end;

    // Forward iterator for the exception handling table entries. Iteration is in table order.
    //
    class iterator
    {
        EHblkDsc* m_ehDsc;

    public:
        iterator(EHblkDsc* ehDsc) : m_ehDsc(ehDsc)
        {
        }

        EHblkDsc* operator*() const
        {
            return m_ehDsc;
        }

        iterator& operator++()
        {
            ++m_ehDsc;
            return *this;
        }

        bool operator!=(const iterator& i) const
        {
            return m_ehDsc != i.m_ehDsc;
        }
    };

public:
    EHClauses(Compiler* comp) : m_begin(comp->compHndBBtab), m_end(comp->compHndBBtab + comp->compHndBBtabCount)
    {
        assert((m_begin != nullptr) || (m_begin == m_end));
    }

    iterator begin() const
    {
        return iterator(m_begin);
    }

    iterator end() const
    {
        return iterator(m_end);
    }
};

/*
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                   Miscellaneous Compiler stuff                            XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

class StringPrinter
{
    CompAllocator m_alloc;
    char*         m_buffer;
    size_t        m_bufferMax;
    size_t        m_bufferIndex = 0;

    void Grow(size_t newSize);

public:
    StringPrinter(CompAllocator alloc, char* buffer = nullptr, size_t bufferMax = 0)
        : m_alloc(alloc), m_buffer(buffer), m_bufferMax(bufferMax)
    {
        if ((m_buffer == nullptr) || (m_bufferMax == 0))
        {
            m_bufferMax = 128;
            m_buffer    = alloc.allocate<char>(m_bufferMax);
        }

        m_buffer[0] = '\0';
    }

    size_t GetLength()
    {
        return m_bufferIndex;
    }

    char* GetBuffer()
    {
        assert(m_buffer[GetLength()] == '\0');
        return m_buffer;
    }
    void Truncate(size_t newLength)
    {
        assert(newLength <= m_bufferIndex);
        m_bufferIndex           = newLength;
        m_buffer[m_bufferIndex] = '\0';
    }

    void Append(const char* str);
    void Append(char chr);
};

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
extern Histogram computeReachabilitySetsIterationTable;
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

extern unsigned  totalLoopMethods;        // counts the total number of methods that have natural loops
extern unsigned  maxLoopsPerMethod;       // counts the maximum number of loops a method has
extern unsigned  totalLoopCount;          // counts the total number of natural loops
extern unsigned  totalUnnatLoopCount;     // counts the total number of (not-necessarily natural) loops
extern unsigned  totalUnnatLoopOverflows; // # of methods that identified more unnatural loops than we can represent
extern unsigned  iterLoopCount;           // counts the # of loops with an iterator (for like)
extern unsigned  constIterLoopCount;      // counts the # of loops with a constant iterator (for like)
extern bool      hasMethodLoops;          // flag to keep track if we already counted a method as having loops
extern unsigned  loopsThisMethod;         // counts the number of loops in the current method
extern bool      loopOverflowThisMethod;  // True if we exceeded the max # of loops in the method.
extern Histogram loopCountTable;          // Histogram of loop counts
extern Histogram loopExitCountTable;      // Histogram of loop exit counts

#endif // COUNT_LOOPS

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

    // Count of tree nodes allocated.
    unsigned __int64 genTreeNodeCnt;

    // The size we allocate.
    unsigned __int64 genTreeNodeSize;

    // The actual size of the node. Note that the actual size will likely be smaller
    // than the allocated size, but we sometimes use SetOper()/ChangeOper() to change
    // a smaller node to a larger one. TODO-Cleanup: add stats on
    // SetOper()/ChangeOper() usage to quantify this.
    unsigned __int64 genTreeNodeActualSize;
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
extern unsigned fatal_implLimitation;
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

#ifdef TARGET_XARCH

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

#endif // TARGET_XARCH

#ifdef TARGET_ARM

const instruction INS_SHIFT_LEFT_LOGICAL  = INS_lsl;
const instruction INS_SHIFT_RIGHT_LOGICAL = INS_lsr;
const instruction INS_SHIFT_RIGHT_ARITHM  = INS_asr;

const instruction INS_AND             = INS_and;
const instruction INS_OR              = INS_orr;
const instruction INS_XOR             = INS_eor;
const instruction INS_NEG             = INS_rsb;
const instruction INS_TEST            = INS_tst;
const instruction INS_MUL             = INS_mul;
const instruction INS_MULADD          = INS_mla;
const instruction INS_SIGNED_DIVIDE   = INS_sdiv;
const instruction INS_UNSIGNED_DIVIDE = INS_udiv;
const instruction INS_BREAKPOINT      = INS_bkpt;
const instruction INS_ADDC            = INS_adc;
const instruction INS_SUBC            = INS_sbc;
const instruction INS_NOT             = INS_mvn;

const instruction INS_ABS  = INS_vabs;
const instruction INS_SQRT = INS_vsqrt;

#endif // TARGET_ARM

#ifdef TARGET_ARM64

const instruction        INS_MULADD = INS_madd;
inline const instruction INS_BREAKPOINT_osHelper()
{
    // GDB needs the encoding of brk #0
    // Windbg needs the encoding of brk #F000
    return TargetOS::IsUnix ? INS_brk_unix : INS_brk_windows;
}
#define INS_BREAKPOINT INS_BREAKPOINT_osHelper()

const instruction INS_ABS  = INS_fabs;
const instruction INS_SQRT = INS_fsqrt;

#endif // TARGET_ARM64

#ifdef TARGET_LOONGARCH64
const instruction INS_BREAKPOINT = INS_break;
const instruction INS_MULADD     = INS_fmadd_d; // NOTE: default is double.
const instruction INS_ABS        = INS_fabs_d;  // NOTE: default is double.
const instruction INS_SQRT       = INS_fsqrt_d; // NOTE: default is double.
#endif                                          // TARGET_LOONGARCH64

#ifdef TARGET_RISCV64
const instruction INS_BREAKPOINT = INS_ebreak;
#endif // TARGET_RISCV64

/*****************************************************************************/

extern const BYTE genTypeSizes[];
extern const BYTE genTypeAlignments[];
extern const BYTE genTypeStSzs[];
extern const BYTE genActualTypes[];

/*****************************************************************************/

#ifdef DEBUG
void dumpConvertedVarSet(Compiler* comp, VARSET_VALARG_TP vars);
#endif // DEBUG

#include "compiler.hpp" // All the shared inline functions

/*****************************************************************************/
#endif //_COMPILER_H_
/*****************************************************************************/
