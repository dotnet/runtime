// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                          GenTree                                          XX
XX                                                                           XX
XX  This is the node in the semantic tree graph. It represents the operation XX
XX  corresponding to the node, and other information during code-gen.        XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

/*****************************************************************************/
#ifndef _GENTREE_H_
#define _GENTREE_H_
/*****************************************************************************/

#include "vartype.h"   // For "var_types"
#include "target.h"    // For "regNumber"
#include "ssaconfig.h" // For "SsaConfig::RESERVED_SSA_NUM"
#include "valuenumtype.h"
#include "jitstd.h"
#include "jithashtable.h"
#include "gentreeopsdef.h"
#include "simd.h"
#include "namedintrinsiclist.h"
#include "layout.h"
#include "debuginfo.h"

// Debugging GenTree is much easier if we add a magic virtual function to make the debugger able to figure out what type
// it's got. This is enabled by default in DEBUG. To enable it in RET builds (temporarily!), you need to change the
// build to define DEBUGGABLE_GENTREE=1, as well as pass /OPT:NOICF to the linker (or else all the vtables get merged,
// making the debugging value supplied by them useless).
#ifndef DEBUGGABLE_GENTREE
#ifdef DEBUG
#define DEBUGGABLE_GENTREE 1
#else // !DEBUG
#define DEBUGGABLE_GENTREE 0
#endif // !DEBUG
#endif // !DEBUGGABLE_GENTREE

// The SpecialCodeKind enum is used to indicate the type of special (unique)
// target block that will be targeted by an instruction.
// These are used by:
//   GenTreeBoundsChk nodes (SCK_RNGCHK_FAIL, SCK_ARG_EXCPN, SCK_ARG_RNG_EXCPN)
//     - these nodes have a field (gtThrowKind) to indicate which kind
//   GenTreeOps nodes, for which codegen will generate the branch
//     - it will use the appropriate kind based on the opcode, though it's not
//       clear why SCK_OVERFLOW == SCK_ARITH_EXCPN
//
enum SpecialCodeKind
{
    SCK_NONE,
    SCK_RNGCHK_FAIL,                // target when range check fails
    SCK_DIV_BY_ZERO,                // target for divide by zero (Not used on X86/X64)
    SCK_ARITH_EXCPN,                // target on arithmetic exception
    SCK_OVERFLOW = SCK_ARITH_EXCPN, // target on overflow
    SCK_ARG_EXCPN,                  // target on ArgumentException (currently used only for SIMD intrinsics)
    SCK_ARG_RNG_EXCPN,              // target on ArgumentOutOfRangeException (currently used only for SIMD intrinsics)
    SCK_FAIL_FAST,                  // target for fail fast exception
    SCK_COUNT
};

/*****************************************************************************/

// The following enum defines a set of bit flags that can be used
// to classify expression tree nodes.
//
enum GenTreeOperKind
{
    GTK_SPECIAL = 0x00, // special operator
    GTK_LEAF    = 0x01, // leaf    operator
    GTK_UNOP    = 0x02, // unary   operator
    GTK_BINOP   = 0x04, // binary  operator

    GTK_KINDMASK = (GTK_SPECIAL | GTK_LEAF | GTK_UNOP | GTK_BINOP), // operator kind mask
    GTK_SMPOP    = (GTK_UNOP | GTK_BINOP),

    GTK_COMMUTE = 0x08, // commutative  operator
    GTK_EXOP    = 0x10, // Indicates that an oper for a node type that extends GenTreeOp (or GenTreeUnOp)
                        // by adding non-node fields to unary or binary operator.
    GTK_NOVALUE = 0x20, // node does not produce a value
    GTK_STORE   = 0x40, // node represents a store

    GTK_MASK = 0xFF
};

// The following enum defines a set of bit flags that describe opers for the purposes
// of DEBUG-only checks. This is separate from the above "GenTreeOperKind"s to avoid
// making the table for those larger in Release builds. However, it resides in the same
// "namespace" and so all values here must be distinct from those in "GenTreeOperKind".
//
enum GenTreeDebugOperKind
{
    DBK_FIRST_FLAG = GTK_MASK + 1,

    DBK_NOTHIR    = DBK_FIRST_FLAG,      // This oper is not supported in HIR (before rationalization).
    DBK_NOTLIR    = DBK_FIRST_FLAG << 1, // This oper is not supported in LIR (after rationalization).
    DBK_NOCONTAIN = DBK_FIRST_FLAG << 2, // This oper produces a value, but may not be contained.

    DBK_MASK = ~GTK_MASK
};

/*****************************************************************************/

enum gtCallTypes : BYTE
{
    CT_USER_FUNC, // User function
    CT_HELPER,    // Jit-helper
    CT_INDIRECT,  // Indirect call

    CT_COUNT // fake entry (must be last)
};

enum class ExceptionSetFlags : uint32_t
{
    None                     = 0x0,
    OverflowException        = 0x1,
    DivideByZeroException    = 0x2,
    ArithmeticException      = 0x4,
    NullReferenceException   = 0x8,
    IndexOutOfRangeException = 0x10,
    StackOverflowException   = 0x20,

    All = OverflowException | DivideByZeroException | ArithmeticException | NullReferenceException |
          IndexOutOfRangeException | StackOverflowException,
};

inline constexpr ExceptionSetFlags operator~(ExceptionSetFlags a)
{
    return (ExceptionSetFlags)(~(uint32_t)a);
}

inline constexpr ExceptionSetFlags operator|(ExceptionSetFlags a, ExceptionSetFlags b)
{
    return (ExceptionSetFlags)((uint32_t)a | (uint32_t)b);
}

inline constexpr ExceptionSetFlags operator&(ExceptionSetFlags a, ExceptionSetFlags b)
{
    return (ExceptionSetFlags)((uint32_t)a & (uint32_t)b);
}

inline ExceptionSetFlags& operator|=(ExceptionSetFlags& a, ExceptionSetFlags b)
{
    return a = (ExceptionSetFlags)((uint32_t)a | (uint32_t)b);
}

inline ExceptionSetFlags& operator&=(ExceptionSetFlags& a, ExceptionSetFlags b)
{
    return a = (ExceptionSetFlags)((uint32_t)a & (uint32_t)b);
}

#ifdef DEBUG
/*****************************************************************************
*
*  TargetHandleTypes are used to determine the type of handle present inside GenTreeIntCon node.
*  The values are such that they don't overlap with helper's or user function's handle.
*/
enum TargetHandleType : BYTE
{
    THT_Unknown                   = 2,
    THT_GSCookieCheck             = 4,
    THT_SetGSCookie               = 6,
    THT_InitializeArrayIntrinsics = 8
};
#endif
/*****************************************************************************/

struct BasicBlock;
enum BasicBlockFlags : unsigned __int64;
struct InlineCandidateInfo;
struct HandleHistogramProfileCandidateInfo;
struct LateDevirtualizationInfo;

typedef unsigned short AssertionIndex;

static const AssertionIndex NO_ASSERTION_INDEX = 0;

//------------------------------------------------------------------------
// GetAssertionIndex: return 1-based AssertionIndex from 0-based int index.
//
// Arguments:
//    index - 0-based index
// Return Value:
//    1-based AssertionIndex.
inline AssertionIndex GetAssertionIndex(unsigned index)
{
    return (AssertionIndex)(index + 1);
}

class AssertionInfo
{
    // true if the assertion holds on the bbNext edge instead of the bbTarget edge (for GT_JTRUE nodes)
    unsigned short m_isNextEdgeAssertion : 1;
    // 1-based index of the assertion
    unsigned short m_assertionIndex : 15;

    AssertionInfo(bool isNextEdgeAssertion, AssertionIndex assertionIndex)
        : m_isNextEdgeAssertion(isNextEdgeAssertion), m_assertionIndex(assertionIndex)
    {
        assert(m_assertionIndex == assertionIndex);
    }

public:
    AssertionInfo() : AssertionInfo(false, 0)
    {
    }

    AssertionInfo(AssertionIndex assertionIndex) : AssertionInfo(false, assertionIndex)
    {
    }

    static AssertionInfo ForNextEdge(AssertionIndex assertionIndex)
    {
        // Ignore the edge information if there's no assertion
        bool isNextEdge = (assertionIndex != NO_ASSERTION_INDEX);
        return AssertionInfo(isNextEdge, assertionIndex);
    }

    void Clear()
    {
        m_isNextEdgeAssertion = 0;
        m_assertionIndex      = NO_ASSERTION_INDEX;
    }

    bool HasAssertion() const
    {
        return m_assertionIndex != NO_ASSERTION_INDEX;
    }

    AssertionIndex GetAssertionIndex() const
    {
        return m_assertionIndex;
    }

    bool IsNextEdgeAssertion() const
    {
        return m_isNextEdgeAssertion;
    }
};

// GT_FIELD_ADDR nodes will be lowered into more "code-gen-able" representations, like ADD's of addresses.
// For value numbering, we would like to preserve the aliasing information for class and static fields,
// and so will annotate such lowered addresses with "field sequences", representing the "base" static or
// class field and any additional struct fields. We only need to preserve the handle for the first field,
// so any struct fields will be represented implicitly (via offsets). See also "IsFieldAddr".
//
class FieldSeq
{
public:
    enum class FieldKind : uintptr_t
    {
        Instance                 = 0, // An instance field.
        SimpleStatic             = 1, // Simple static field - the handle represents a unique location.
        SimpleStaticKnownAddress = 2, // Simple static field - the handle represents a known location.
        SharedStatic             = 3, // Static field on a shared generic type: "Class<__Canon>.StaticField".
    };

private:
    static const uintptr_t FIELD_KIND_MASK = 0b11;

    static_assert_no_msg(sizeof(CORINFO_FIELD_HANDLE) == sizeof(uintptr_t));

    uintptr_t m_fieldHandleAndKind;
    ssize_t   m_offset;

public:
    FieldSeq(CORINFO_FIELD_HANDLE fieldHnd, ssize_t offset, FieldKind fieldKind);

    FieldKind GetKind() const
    {
        return static_cast<FieldKind>(m_fieldHandleAndKind & FIELD_KIND_MASK);
    }

    CORINFO_FIELD_HANDLE GetFieldHandle() const
    {
        return CORINFO_FIELD_HANDLE(m_fieldHandleAndKind & ~FIELD_KIND_MASK);
    }

    //------------------------------------------------------------------------
    // GetOffset: Retrieve "the offset" for the field this node represents.
    //
    // For statics with a known (constant) address it will be the value of that address.
    // For boxed statics, it will be TARGET_POINTER_SIZE (the method table pointer size).
    // For other fields, it will be equal to the value "getFieldOffset" would return.
    //
    ssize_t GetOffset() const
    {
        return m_offset;
    }

    bool IsStaticField() const
    {
        return (GetKind() == FieldKind::SimpleStatic) || (GetKind() == FieldKind::SharedStatic) ||
               (GetKind() == FieldKind::SimpleStaticKnownAddress);
    }

    bool IsSharedStaticField() const
    {
        return GetKind() == FieldKind::SharedStatic;
    }
};

// This class canonicalizes field sequences.
//
class FieldSeqStore
{
    // Maps field handles to field sequence instances.
    //
    JitHashTable<CORINFO_FIELD_HANDLE, JitPtrKeyFuncs<CORINFO_FIELD_STRUCT_>, FieldSeq> m_map;

public:
    FieldSeqStore(CompAllocator alloc) : m_map(alloc)
    {
    }

    FieldSeq* Create(CORINFO_FIELD_HANDLE fieldHnd, ssize_t offset, FieldSeq::FieldKind fieldKind);

    FieldSeq* Append(FieldSeq* a, FieldSeq* b);
};

class GenTreeUseEdgeIterator;
class GenTreeOperandIterator;

struct Statement;

/*****************************************************************************/

// Forward declarations of the subtypes
#define GTSTRUCT_0(fn, en) struct GenTree##fn;
#define GTSTRUCT_1(fn, en) struct GenTree##fn;
#define GTSTRUCT_2(fn, en, en2) struct GenTree##fn;
#define GTSTRUCT_3(fn, en, en2, en3) struct GenTree##fn;
#define GTSTRUCT_4(fn, en, en2, en3, en4) struct GenTree##fn;
#define GTSTRUCT_N(fn, ...) struct GenTree##fn;
#define GTSTRUCT_2_SPECIAL(fn, en, en2) GTSTRUCT_2(fn, en, en2)
#define GTSTRUCT_3_SPECIAL(fn, en, en2, en3) GTSTRUCT_3(fn, en, en2, en3)
#include "gtstructs.h"

/*****************************************************************************/

// Don't format the GenTreeFlags declaration
// clang-format off

//------------------------------------------------------------------------
// GenTreeFlags: a bitmask of flags for GenTree stored in gtFlags
//
enum GenTreeFlags : unsigned int
{
    GTF_EMPTY         = 0,

//---------------------------------------------------------------------
//  The first set of flags can be used with a large set of nodes, and
//  thus they must all have distinct values. That is, one can test any
//  expression node for one of these flags.
//---------------------------------------------------------------------

    GTF_ASG           = 0x00000001, // sub-expression contains an assignment
    GTF_CALL          = 0x00000002, // sub-expression contains a  func. call
    GTF_EXCEPT        = 0x00000004, // sub-expression might throw an exception
    GTF_GLOB_REF      = 0x00000008, // sub-expression uses global variable(s)
    GTF_ORDER_SIDEEFF = 0x00000010, // sub-expression has a re-ordering side effect

// If you set these flags, make sure that code:gtExtractSideEffList knows how to find the tree,
// otherwise the C# (run csc /o-) code:
//     var v = side_eff_operation
// with no use of `v` will drop your tree on the floor.

    GTF_PERSISTENT_SIDE_EFFECTS = GTF_ASG | GTF_CALL,
    GTF_SIDE_EFFECT             = GTF_PERSISTENT_SIDE_EFFECTS | GTF_EXCEPT,
    GTF_GLOB_EFFECT             = GTF_SIDE_EFFECT | GTF_GLOB_REF,
    GTF_ALL_EFFECT              = GTF_GLOB_EFFECT | GTF_ORDER_SIDEEFF,

    GTF_REVERSE_OPS = 0x00000020, // operand op2 should be evaluated before op1 (normally, op1 is evaluated first and op2 is evaluated second)
    GTF_CONTAINED   = 0x00000040, // This node is contained (executed as part of its parent)
    GTF_SPILLED     = 0x00000080, // the value has been spilled

    GTF_NOREG_AT_USE = 0x00000100, // tree node is in memory at the point of use

    GTF_SET_FLAGS   = 0x00000200, // Requires that codegen for this node set the flags. Use gtSetFlags() to check this flag.

#ifdef TARGET_XARCH
    GTF_DONT_EXTEND = 0x00000400, // This small-typed tree produces a value with undefined upper bits. Used on x86/x64 as a
                                  // lowering optimization and tells the codegen to use instructions like "mov al, [addr]"
                                  // instead of "movzx/movsx", when the user node doesn't need the upper bits.
#endif // TARGET_XARCH

    GTF_MAKE_CSE    = 0x00000800, // Hoisted expression: try hard to make this into CSE (see optPerformHoistExpr)
    GTF_DONT_CSE    = 0x00001000, // Don't bother CSE'ing this expr
    GTF_COLON_COND  = 0x00002000, // This node is conditionally executed (part of ? :)

    GTF_NODE_MASK   = GTF_COLON_COND,

    GTF_BOOLEAN     = 0x00004000, // value is known to be 0/1

    GTF_UNSIGNED    = 0x00008000, // With GT_CAST:   the source operand is an unsigned type
                                  // With operators: the specified node is an unsigned operator
    GTF_SPILL       = 0x00020000, // Needs to be spilled here

// The extra flag GTF_IS_IN_CSE is used to tell the consumer of the side effect flags
// that we are calling in the context of performing a CSE, thus we
// should allow the run-once side effects of running a class constructor.
//
// The only requirement of this flag is that it not overlap any of the
// side-effect flags. The actual bit used is otherwise arbitrary.

    GTF_IS_IN_CSE   = GTF_BOOLEAN,

    GTF_COMMON_MASK = 0x0003FFFF, // mask of all the flags above

    GTF_REUSE_REG_VAL = 0x00800000, // This is set by the register allocator on nodes whose value already exists in the
                                    // register assigned to this node, so the code generator does not have to generate
                                    // code to produce the value. It is currently used only on constant nodes.
                                    // It CANNOT be set on var (GT_LCL*) nodes, or on indir (GT_IND or GT_STOREIND) nodes, since
                                    // it is not needed for lclVars and is highly unlikely to be useful for indir nodes.

//---------------------------------------------------------------------
//  The following flags can be used only with a small set of nodes, and
//  thus their values need not be distinct (other than within the set
//  that goes with a particular node/nodes, of course). That is, one can
//  only test for one of these flags if the 'gtOper' value is tested as
//  well to make sure it's the right operator for the particular flag.
//---------------------------------------------------------------------

    GTF_VAR_DEF             = 0x80000000, // GT_STORE_LCL_VAR/GT_STORE_LCL_FLD/GT_LCL_ADDR -- this is a definition
    GTF_VAR_USEASG          = 0x40000000, // GT_STORE_LCL_FLD/GT_STORE_LCL_FLD/GT_LCL_ADDR -- this is a partial definition, a use of
                                          // the previous definition is implied. A partial definition usually occurs when a struct
                                          // field is assigned to (s.f = ...) or when a scalar typed variable is assigned to via a
                                          // narrow store (*((byte*)&i) = ...).

// Last-use bits. Also used by GenTreeCopyOrReload.
// Note that a node marked GTF_VAR_MULTIREG can only be a pure definition of all the fields, or a pure use of all the fields,
// so we don't need the equivalent of GTF_VAR_USEASG.

    GTF_VAR_FIELD_DEATH0 = 0x04000000, // The last-use bit for the first field of a promoted local.
    GTF_VAR_FIELD_DEATH1 = 0x08000000, // The last-use bit for the second field of a promoted local.
    GTF_VAR_FIELD_DEATH2 = 0x10000000, // The last-use bit for the third field of a promoted local.
    GTF_VAR_FIELD_DEATH3 = 0x20000000, // The last-use bit for the fourth field of a promoted local.
    GTF_VAR_DEATH_MASK   = GTF_VAR_FIELD_DEATH0 | GTF_VAR_FIELD_DEATH1 | GTF_VAR_FIELD_DEATH2 | GTF_VAR_FIELD_DEATH3,
    GTF_VAR_DEATH        = GTF_VAR_FIELD_DEATH0, // The last-use bit for a tracked local.

// This is the amount we have to shift, plus the index, to get the last use bit we want.
#define FIELD_LAST_USE_SHIFT 26

    GTF_VAR_MULTIREG        = 0x02000000, // This is a struct or (on 32-bit platforms) long variable that is used or defined
                                          // to/from a multireg source or destination (e.g. a call arg or return, or an op
                                          // that returns its result in multiple registers such as a long multiply). Set by
                                          // (and thus only valid after) lowering.

    GTF_LIVENESS_MASK     = GTF_VAR_DEF | GTF_VAR_USEASG | GTF_VAR_DEATH_MASK,

    GTF_VAR_MOREUSES      = 0x00800000, // GT_LCL_VAR -- this node has additional uses, for example due to cloning
    GTF_VAR_CONTEXT       = 0x00400000, // GT_LCL_VAR -- this node is part of a runtime lookup
    GTF_VAR_EXPLICIT_INIT = 0x00200000, // GT_LCL_VAR -- this node is an "explicit init" store. Valid until rationalization.

    // For additional flags for GT_CALL node see GTF_CALL_M_*

    GTF_CALL_UNMANAGED          = 0x80000000, // GT_CALL -- direct call to unmanaged code
    GTF_CALL_INLINE_CANDIDATE   = 0x40000000, // GT_CALL -- this call has been marked as an inline candidate

    GTF_CALL_VIRT_KIND_MASK     = 0x30000000, // GT_CALL -- mask of the below call kinds
    GTF_CALL_NONVIRT            = 0x00000000, // GT_CALL -- a non virtual call
    GTF_CALL_VIRT_STUB          = 0x10000000, // GT_CALL -- a stub-dispatch virtual call
    GTF_CALL_VIRT_VTABLE        = 0x20000000, // GT_CALL -- a  vtable-based virtual call

    GTF_CALL_NULLCHECK          = 0x08000000, // GT_CALL -- must check instance pointer for null
    GTF_CALL_POP_ARGS           = 0x04000000, // GT_CALL -- caller pop arguments?
    GTF_CALL_HOISTABLE          = 0x02000000, // GT_CALL -- call is hoistable
    GTF_TLS_GET_ADDR            = 0x01000000, // GT_CALL -- call is tls_get_addr

    GTF_MEMORYBARRIER_LOAD      = 0x40000000, // GT_MEMORYBARRIER -- Load barrier

    GTF_FLD_TLS                 = 0x80000000, // GT_FIELD_ADDR -- field address is a Windows x86 TLS reference
    GTF_FLD_DEREFERENCED        = 0x40000000, // GT_FIELD_ADDR -- used to preserve previous behavior

    GTF_INX_RNGCHK              = 0x80000000, // GT_INDEX_ADDR -- this array address should be range-checked
    GTF_INX_ADDR_NONNULL        = 0x40000000, // GT_INDEX_ADDR -- this array address is not null

    GTF_IND_TGT_NOT_HEAP        = 0x80000000, // GT_IND -- the target is not GC-tracked, such as an object on the nongc heap
    GTF_IND_VOLATILE            = 0x40000000, // OperIsIndir() -- the load or store must use volatile semantics (this is a nop on X86)
    GTF_IND_NONFAULTING         = 0x20000000, // OperIsIndir() -- An indir that cannot fault.
    GTF_IND_TGT_HEAP            = 0x10000000, // GT_IND -- the target is on the heap
    GTF_IND_REQ_ADDR_IN_REG     = 0x08000000, // GT_IND -- requires its addr operand to be evaluated into a register.
                                              //           This flag is useful in cases where it is required to generate register
                                              //           indirect addressing mode. One such case is virtual stub calls on xarch.
    GTF_IND_UNALIGNED           = 0x02000000, // OperIsIndir() -- the load or store is unaligned (we assume worst case alignment of 1 byte)
    GTF_IND_INVARIANT           = 0x01000000, // GT_IND -- the target is invariant (a prejit indirection)
    GTF_IND_NONNULL             = 0x00400000, // GT_IND -- the indirection never returns null (zero)
    GTF_IND_INITCLASS           = 0x00200000, // OperIsIndir() -- the indirection requires preceding static cctor
    GTF_IND_ALLOW_NON_ATOMIC    = 0x00100000, // GT_IND -- this memory access does not need to be atomic

    GTF_IND_COPYABLE_FLAGS = GTF_IND_VOLATILE | GTF_IND_NONFAULTING | GTF_IND_UNALIGNED | GTF_IND_INITCLASS,
    GTF_IND_FLAGS = GTF_IND_COPYABLE_FLAGS | GTF_IND_NONNULL | GTF_IND_TGT_NOT_HEAP | GTF_IND_TGT_HEAP | GTF_IND_INVARIANT,

    GTF_ADDRMODE_NO_CSE         = 0x80000000, // GT_ADD/GT_MUL/GT_LSH -- Do not CSE this node only, forms complex
                                              //                         addressing mode

    GTF_MUL_64RSLT              = 0x40000000, // GT_MUL     -- produce 64-bit result

    GTF_RELOP_NAN_UN            = 0x80000000, // GT_<relop> -- Is branch taken if ops are NaN?
    GTF_RELOP_JMP_USED          = 0x40000000, // GT_<relop> -- result of compare used for jump or ?:
                                              //               with explicit "loop test" in the header block.

    GTF_RET_MERGED              = 0x80000000, // GT_RETURN -- This is a return generated during epilog merging.

    GTF_QMARK_CAST_INSTOF       = 0x80000000, // GT_QMARK -- Is this a top (not nested) level qmark created for
                                              //             castclass or instanceof?

    GTF_BOX_CLONED              = 0x40000000, // GT_BOX -- this box and its operand has been cloned, cannot assume it to be single-use anymore
    GTF_BOX_VALUE               = 0x80000000, // GT_BOX -- "box" is on a value type

    GTF_ARR_ADDR_NONNULL        = 0x80000000, // GT_ARR_ADDR -- this array's address is not null

    GTF_ICON_HDL_MASK           = 0xFF000000, // Bits used by handle types below
    GTF_ICON_SCOPE_HDL          = 0x01000000, // GT_CNS_INT -- constant is a scope handle
    GTF_ICON_CLASS_HDL          = 0x02000000, // GT_CNS_INT -- constant is a class handle
    GTF_ICON_METHOD_HDL         = 0x03000000, // GT_CNS_INT -- constant is a method handle
    GTF_ICON_FIELD_HDL          = 0x04000000, // GT_CNS_INT -- constant is a field handle
    GTF_ICON_STATIC_HDL         = 0x05000000, // GT_CNS_INT -- constant is a handle to static data
    GTF_ICON_STR_HDL            = 0x06000000, // GT_CNS_INT -- constant is a pinned handle pointing to a string object
    GTF_ICON_OBJ_HDL            = 0x12000000, // GT_CNS_INT -- constant is an object handle (e.g. frozen string or Type object)
    GTF_ICON_CONST_PTR          = 0x07000000, // GT_CNS_INT -- constant is a pointer to immutable data, (e.g. IAT_PPVALUE)
    GTF_ICON_GLOBAL_PTR         = 0x08000000, // GT_CNS_INT -- constant is a pointer to mutable data (e.g. from the VM state)
    GTF_ICON_VARG_HDL           = 0x09000000, // GT_CNS_INT -- constant is a var arg cookie handle
    GTF_ICON_PINVKI_HDL         = 0x0A000000, // GT_CNS_INT -- constant is a pinvoke calli handle
    GTF_ICON_TOKEN_HDL          = 0x0B000000, // GT_CNS_INT -- constant is a token handle (other than class, method or field)
    GTF_ICON_TLS_HDL            = 0x0C000000, // GT_CNS_INT -- constant is a TLS ref with offset
    GTF_ICON_FTN_ADDR           = 0x0D000000, // GT_CNS_INT -- constant is a function address
    GTF_ICON_CIDMID_HDL         = 0x0E000000, // GT_CNS_INT -- constant is a class ID or a module ID
    GTF_ICON_BBC_PTR            = 0x0F000000, // GT_CNS_INT -- constant is a basic block count pointer
    GTF_ICON_STATIC_BOX_PTR     = 0x10000000, // GT_CNS_INT -- constant is an address of the box for a STATIC_IN_HEAP field
    GTF_ICON_FIELD_SEQ          = 0x11000000, // <--------> -- constant is a FieldSeq* (used only as VNHandle)
    GTF_ICON_STATIC_ADDR_PTR    = 0x13000000, // GT_CNS_INT -- constant is a pointer to a static base address
    GTF_ICON_SECREL_OFFSET      = 0x14000000, // GT_CNS_INT -- constant is an offset in a certain section.
    GTF_ICON_TLSGD_OFFSET       = 0x15000000, // GT_CNS_INT -- constant is an argument to tls_get_addr.

 // GTF_ICON_REUSE_REG_VAL      = 0x00800000  // GT_CNS_INT -- GTF_REUSE_REG_VAL, defined above
    GTF_ICON_SIMD_COUNT         = 0x00200000, // GT_CNS_INT -- constant is Vector<T>.Count

    GTF_OVERFLOW                = 0x10000000, // Supported for: GT_ADD, GT_SUB, GT_MUL and GT_CAST.
                                              // Requires an overflow check. Use gtOverflow(Ex)() to check this flag.

    GTF_DIV_MOD_NO_BY_ZERO      = 0x20000000, // GT_DIV, GT_MOD -- Div or mod definitely does not divide-by-zero.

    GTF_DIV_MOD_NO_OVERFLOW     = 0x40000000, // GT_DIV, GT_MOD -- Div or mod definitely does not overflow.

    GTF_CHK_INDEX_INBND         = 0x80000000, // GT_BOUNDS_CHECK -- have proven this check is always in-bounds

    GTF_ARRLEN_NONFAULTING      = 0x20000000, // GT_ARR_LENGTH  -- An array length operation that cannot fault. Same as GT_IND_NONFAULTING.

    GTF_MDARRLEN_NONFAULTING    = 0x20000000, // GT_MDARR_LENGTH -- An MD array length operation that cannot fault. Same as GT_IND_NONFAULTING.

    GTF_MDARRLOWERBOUND_NONFAULTING = 0x20000000, // GT_MDARR_LOWER_BOUND -- An MD array lower bound operation that cannot fault. Same as GT_IND_NONFAULTING.

#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    GTF_HW_EM_OP                  = 0x10000000, // GT_HWINTRINSIC -- node is used as an operand to an embedded mask
#endif // TARGET_XARCH && FEATURE_HW_INTRINSICS
};

inline constexpr GenTreeFlags operator ~(GenTreeFlags a)
{
    return (GenTreeFlags)(~(unsigned int)a);
}

inline constexpr GenTreeFlags operator |(GenTreeFlags a, GenTreeFlags b)
{
    return (GenTreeFlags)((unsigned int)a | (unsigned int)b);
}

inline constexpr GenTreeFlags operator &(GenTreeFlags a, GenTreeFlags b)
{
    return (GenTreeFlags)((unsigned int)a & (unsigned int)b);
}

inline GenTreeFlags& operator |=(GenTreeFlags& a, GenTreeFlags b)
{
    return a = (GenTreeFlags)((unsigned int)a | (unsigned int)b);
}

inline GenTreeFlags& operator &=(GenTreeFlags& a, GenTreeFlags b)
{
    return a = (GenTreeFlags)((unsigned int)a & (unsigned int)b);
}

inline GenTreeFlags& operator ^=(GenTreeFlags& a, GenTreeFlags b)
{
    return a = (GenTreeFlags)((unsigned int)a ^ (unsigned int)b);
}

// Can any side-effects be observed externally, say by a caller method?
// For assignments, only assignments to global memory can be observed
// externally, whereas simple assignments to local variables can not.
//
// Be careful when using this inside a "try" protected region as the
// order of assignments to local variables would need to be preserved
// wrt side effects if the variables are alive on entry to the
// "catch/finally" region. In such cases, even assignments to locals
// will have to be restricted.
#define GTF_GLOBALLY_VISIBLE_SIDE_EFFECTS(flags) \
    (((flags) & (GTF_CALL | GTF_EXCEPT)) || (((flags) & (GTF_ASG | GTF_GLOB_REF)) == (GTF_ASG | GTF_GLOB_REF)))

#if defined(DEBUG)

//------------------------------------------------------------------------
// GenTreeDebugFlags: a bitmask of debug-only flags for GenTree stored in gtDebugFlags
//
enum GenTreeDebugFlags : unsigned int
{
    GTF_DEBUG_NONE              = 0x00000000, // No debug flags.

    GTF_DEBUG_NODE_MORPHED      = 0x00000001, // the node has been morphed (in the global morphing phase)
    GTF_DEBUG_NODE_SMALL        = 0x00000002,
    GTF_DEBUG_NODE_LARGE        = 0x00000004,
    GTF_DEBUG_NODE_CG_PRODUCED  = 0x00000008, // genProduceReg has been called on this node
    GTF_DEBUG_NODE_CG_CONSUMED  = 0x00000010, // genConsumeReg has been called on this node
    GTF_DEBUG_NODE_LSRA_ADDED   = 0x00000020, // This node was added by LSRA

    GTF_DEBUG_NODE_MASK         = 0x0000003F, // These flags are all node (rather than operation) properties.

    GTF_DEBUG_VAR_CSE_REF       = 0x00800000, // GT_LCL_VAR -- This is a CSE LCL_VAR node
    GTF_DEBUG_CAST_DONT_FOLD    = 0x00400000, // GT_CAST    -- Try to prevent this cast from being folded
};

inline constexpr GenTreeDebugFlags operator ~(GenTreeDebugFlags a)
{
    return (GenTreeDebugFlags)(~(unsigned int)a);
}

inline constexpr GenTreeDebugFlags operator |(GenTreeDebugFlags a, GenTreeDebugFlags b)
{
    return (GenTreeDebugFlags)((unsigned int)a | (unsigned int)b);
}

inline constexpr GenTreeDebugFlags operator &(GenTreeDebugFlags a, GenTreeDebugFlags b)
{
    return (GenTreeDebugFlags)((unsigned int)a & (unsigned int)b);
}

inline GenTreeDebugFlags& operator |=(GenTreeDebugFlags& a, GenTreeDebugFlags b)
{
    return a = (GenTreeDebugFlags)((unsigned int)a | (unsigned int)b);
}

inline GenTreeDebugFlags& operator &=(GenTreeDebugFlags& a, GenTreeDebugFlags b)
{
    return a = (GenTreeDebugFlags)((unsigned int)a & (unsigned int)b);
}

#endif // defined(DEBUG)

// clang-format on

#ifndef HOST_64BIT
#include <pshpack4.h>
#endif

struct GenTree
{
// We use GT_STRUCT_0 only for the category of simple ops.
#define GTSTRUCT_0(fn, en)                                                                                             \
    GenTree##fn* As##fn()                                                                                              \
    {                                                                                                                  \
        assert(OperIsSimple());                                                                                        \
        return reinterpret_cast<GenTree##fn*>(this);                                                                   \
    }                                                                                                                  \
    const GenTree##fn* As##fn() const                                                                                  \
    {                                                                                                                  \
        assert(OperIsSimple());                                                                                        \
        return reinterpret_cast<const GenTree##fn*>(this);                                                             \
    }                                                                                                                  \
    GenTree##fn& As##fn##Ref()                                                                                         \
    {                                                                                                                  \
        return *As##fn();                                                                                              \
    }

#define GTSTRUCT_N(fn, ...)                                                                                            \
    GenTree##fn* As##fn()                                                                                              \
    {                                                                                                                  \
        assert(OperIs(__VA_ARGS__));                                                                                   \
        return reinterpret_cast<GenTree##fn*>(this);                                                                   \
    }                                                                                                                  \
    const GenTree##fn* As##fn() const                                                                                  \
    {                                                                                                                  \
        assert(OperIs(__VA_ARGS__));                                                                                   \
        return reinterpret_cast<const GenTree##fn*>(this);                                                             \
    }                                                                                                                  \
    GenTree##fn& As##fn##Ref()                                                                                         \
    {                                                                                                                  \
        return *As##fn();                                                                                              \
    }

#define GTSTRUCT_1(fn, en) GTSTRUCT_N(fn, en)
#define GTSTRUCT_2(fn, en, en2) GTSTRUCT_N(fn, en, en2)
#define GTSTRUCT_3(fn, en, en2, en3) GTSTRUCT_N(fn, en, en2, en3)
#define GTSTRUCT_4(fn, en, en2, en3, en4) GTSTRUCT_N(fn, en, en2, en3, en4)
#define GTSTRUCT_2_SPECIAL(fn, en, en2) GTSTRUCT_2(fn, en, en2)
#define GTSTRUCT_3_SPECIAL(fn, en, en2, en3) GTSTRUCT_3(fn, en, en2, en3)

#include "gtstructs.h"

    genTreeOps gtOper; // enum subtype BYTE
    var_types  gtType; // enum subtype BYTE

    genTreeOps OperGet() const
    {
        return gtOper;
    }
    var_types TypeGet() const
    {
        return gtType;
    }

    ClassLayout* GetLayout(Compiler* compiler) const;

#ifdef DEBUG
    genTreeOps gtOperSave; // Only used to save gtOper when we destroy a node, to aid debugging.
#endif

#define NO_CSE (0)

#define IS_CSE_INDEX(x) ((x) != 0)
#define IS_CSE_USE(x) ((x) > 0)
#define IS_CSE_DEF(x) ((x) < 0)
#define GET_CSE_INDEX(x) (((x) > 0) ? x : -(x))
#define TO_CSE_DEF(x) (-(x))

    signed char gtCSEnum; // 0 or the CSE index (negated if def)
                          // valid only for CSE expressions

    unsigned char gtLIRFlags; // Used for nodes that are in LIR. See LIR::Flags in lir.h for the various flags.

    AssertionInfo gtAssertionInfo;

    bool GeneratesAssertion() const
    {
        return gtAssertionInfo.HasAssertion();
    }

    void ClearAssertion()
    {
        gtAssertionInfo.Clear();
    }

    AssertionInfo GetAssertionInfo() const
    {
        return gtAssertionInfo;
    }

    void SetAssertionInfo(AssertionInfo info)
    {
        gtAssertionInfo = info;
    }

    //
    // Cost metrics on the node. Don't allow direct access to the variable for setting.
    //

public:
#ifdef DEBUG
    // You are not allowed to read the cost values before they have been set in gtSetEvalOrder().
    // Keep track of whether the costs have been initialized, and assert if they are read before being initialized.
    // Obviously, this information does need to be initialized when a node is created.
    // This is public so the dumpers can see it.

    bool gtCostsInitialized;
#endif // DEBUG

#define MAX_COST UCHAR_MAX
#define IND_COST_EX 3 // execution cost for an indirection

    unsigned char GetCostEx() const
    {
        assert(gtCostsInitialized);
        return _gtCostEx;
    }
    unsigned char GetCostSz() const
    {
        assert(gtCostsInitialized);
        return _gtCostSz;
    }

    // Set the costs. They are always both set at the same time.
    // Don't use the "put" property: force calling this function, to make it more obvious in the few places
    // that set the values.
    // Note that costs are only set in gtSetEvalOrder() and its callees.
    void SetCosts(unsigned costEx, unsigned costSz)
    {
        assert(costEx != (unsigned)-1); // looks bogus
        assert(costSz != (unsigned)-1); // looks bogus
        INDEBUG(gtCostsInitialized = true;)

        _gtCostEx = (costEx > MAX_COST) ? MAX_COST : (unsigned char)costEx;
        _gtCostSz = (costSz > MAX_COST) ? MAX_COST : (unsigned char)costSz;
    }

    // Opimized copy function, to avoid the SetCosts() function comparisons, and make it more clear that a node copy is
    // happening.
    void CopyCosts(const GenTree* const tree)
    {
        // If the 'tree' costs aren't initialized, we'll hit an assert below.
        INDEBUG(gtCostsInitialized = tree->gtCostsInitialized;)
        _gtCostEx = tree->GetCostEx();
        _gtCostSz = tree->GetCostSz();
    }

    // Same as CopyCosts, but avoids asserts if the costs we are copying have not been initialized.
    // This is because the importer, for example, clones nodes, before these costs have been initialized.
    // Note that we directly access the 'tree' costs, not going through the accessor functions (either
    // directly or through the properties).
    void CopyRawCosts(const GenTree* const tree)
    {
        INDEBUG(gtCostsInitialized = tree->gtCostsInitialized;)
        _gtCostEx = tree->_gtCostEx;
        _gtCostSz = tree->_gtCostSz;
    }

private:
    unsigned char _gtCostEx; // estimate of expression execution cost
    unsigned char _gtCostSz; // estimate of expression code size cost

    //
    // Register or register pair number of the node.
    //
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG

public:
    enum genRegTag
    {
        GT_REGTAG_NONE, // Nothing has been assigned to _gtRegNum
        GT_REGTAG_REG   // _gtRegNum has been assigned
    };
    genRegTag GetRegTag() const
    {
        assert(gtRegTag == GT_REGTAG_NONE || gtRegTag == GT_REGTAG_REG);
        return gtRegTag;
    }

private:
    genRegTag gtRegTag; // What is in _gtRegNum?

#endif // DEBUG

private:
    // This stores the register assigned to the node. If a register is not assigned, _gtRegNum is set to REG_NA.
    regNumberSmall _gtRegNum;

    // Count of operands. Used *only* by GenTreeMultiOp, exists solely due to padding constraints.
    friend struct GenTreeMultiOp;
    uint8_t m_operandCount;

public:
    // The register number is stored in a small format (8 bits), but the getters return and the setters take
    // a full-size (unsigned) format, to localize the casts here.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    bool canBeContained() const;
#endif

    // for codegen purposes, is this node a subnode of its parent
    bool isContained() const;

    bool isContainedIndir() const;

    bool isIndirAddrMode();

    // This returns true only for GT_IND and GT_STOREIND, and is used in contexts where a "true"
    // indirection is expected (i.e. either a load to or a store from a single register).
    // OperIsIndir() returns true also for indirection nodes such as GT_BLK, etc. as well as GT_NULLCHECK.
    bool isIndir() const;

    bool isContainedIntOrIImmed() const
    {
        return isContained() && IsCnsIntOrI() && !isUsedFromSpillTemp();
    }

    bool isContainedFltOrDblImmed() const
    {
        return isContained() && OperIs(GT_CNS_DBL);
    }

    bool isContainedVecImmed() const
    {
        return isContained() && IsCnsVec();
    }

    bool isLclField() const
    {
        return OperGet() == GT_LCL_FLD || OperGet() == GT_STORE_LCL_FLD;
    }

    bool isUsedFromSpillTemp() const;

    // Indicates whether it is a memory op.
    // Right now it includes Indir and LclField ops.
    bool isMemoryOp() const
    {
        return isIndir() || isLclField();
    }

    bool isUsedFromMemory() const
    {
        return ((isContained() && (isMemoryOp() || OperIs(GT_LCL_VAR, GT_CNS_DBL, GT_CNS_VEC))) ||
                isUsedFromSpillTemp());
    }

    bool isLclVarUsedFromMemory() const
    {
        return (OperGet() == GT_LCL_VAR) && (isContained() || isUsedFromSpillTemp());
    }

    bool isLclFldUsedFromMemory() const
    {
        return isLclField() && (isContained() || isUsedFromSpillTemp());
    }

    bool isUsedFromReg() const
    {
        return !isContained() && !isUsedFromSpillTemp();
    }

    regNumber GetRegNum() const
    {
        assert((gtRegTag == GT_REGTAG_REG) || (gtRegTag == GT_REGTAG_NONE)); // TODO-Cleanup: get rid of the NONE case,
                                                                             // and fix everyplace that reads undefined
                                                                             // values
        regNumber reg = (regNumber)_gtRegNum;
        assert((gtRegTag == GT_REGTAG_NONE) || // TODO-Cleanup: get rid of the NONE case, and fix everyplace that reads
                                               // undefined values
               (reg >= REG_FIRST && reg <= REG_COUNT));
        return reg;
    }

    void SetRegNum(regNumber reg)
    {
        assert(reg >= REG_FIRST && reg <= REG_COUNT);
        _gtRegNum = (regNumberSmall)reg;
        INDEBUG(gtRegTag = GT_REGTAG_REG;)
        assert(_gtRegNum == reg);
    }

    void ClearRegNum()
    {
        _gtRegNum = REG_NA;
        INDEBUG(gtRegTag = GT_REGTAG_NONE;)
    }

    // Copy the _gtRegNum/gtRegTag fields
    void CopyReg(GenTree* from);
    bool gtHasReg(Compiler* comp) const;

    int GetRegisterDstCount(Compiler* compiler) const;

    regMaskTP gtGetRegMask() const;
    regMaskTP gtGetContainedRegMask();

    GenTreeFlags gtFlags;

#if defined(DEBUG)
    GenTreeDebugFlags gtDebugFlags;
#endif // defined(DEBUG)

    ValueNumPair gtVNPair;

    regMaskSmall gtRsvdRegs; // set of fixed trashed  registers

    unsigned AvailableTempRegCount(regMaskTP mask = (regMaskTP)-1) const;
    regNumber GetSingleTempReg(regMaskTP mask = (regMaskTP)-1);
    regNumber ExtractTempReg(regMaskTP mask = (regMaskTP)-1);

    void SetVNsFromNode(GenTree* tree)
    {
        gtVNPair = tree->gtVNPair;
    }

    ValueNum GetVN(ValueNumKind vnk) const
    {
        if (vnk == VNK_Liberal)
        {
            return gtVNPair.GetLiberal();
        }
        else
        {
            assert(vnk == VNK_Conservative);
            return gtVNPair.GetConservative();
        }
    }
    void SetVN(ValueNumKind vnk, ValueNum vn)
    {
        if (vnk == VNK_Liberal)
        {
            return gtVNPair.SetLiberal(vn);
        }
        else
        {
            assert(vnk == VNK_Conservative);
            return gtVNPair.SetConservative(vn);
        }
    }
    void SetVNs(ValueNumPair vnp)
    {
        gtVNPair = vnp;
    }
    void ClearVN()
    {
        gtVNPair = ValueNumPair(); // Initializes both elements to "NoVN".
    }

    GenTree* gtNext;
    GenTree* gtPrev;

#ifdef DEBUG
    unsigned gtTreeID;
    unsigned gtSeqNum; // liveness traversal order within the current statement

    int gtUseNum; // use-ordered traversal within the function
#endif

    static const unsigned char gtOperKindTable[];

    static unsigned OperKind(unsigned gtOper)
    {
        assert(gtOper < GT_COUNT);

        return gtOperKindTable[gtOper];
    }

    unsigned OperKind() const
    {
        assert(gtOper < GT_COUNT);

        return gtOperKindTable[gtOper];
    }

    static bool IsExOp(unsigned opKind)
    {
        return (opKind & GTK_EXOP) != 0;
    }

    bool IsValue() const
    {
        if ((OperKind(gtOper) & GTK_NOVALUE) != 0)
        {
            return false;
        }

        if (gtType == TYP_VOID)
        {
            // These are the only operators which can produce either VOID or non-VOID results.
            assert(OperIs(GT_NOP, GT_CALL, GT_COMMA) || OperIsCompare() || OperIsLong() || OperIsHWIntrinsic() ||
                   IsCnsVec());
            return false;
        }

        return true;
    }

    bool IsNotGcDef() const
    {
        return IsIntegralConst(0) || OperIs(GT_LCL_ADDR);
    }

    // LIR flags
    //   These helper methods, along with the flag values they manipulate, are defined in lir.h
    //
    // UnusedValue indicates that, although this node produces a value, it is unused.
    inline void SetUnusedValue();
    inline void ClearUnusedValue();
    inline bool IsUnusedValue() const;
    // RegOptional indicates that codegen can still generate code even if it isn't allocated a register.
    inline bool IsRegOptional() const;
    inline void SetRegOptional();
    inline void ClearRegOptional();
#ifdef DEBUG
    void dumpLIRFlags();
#endif

    bool TypeIs(var_types type) const
    {
        return gtType == type;
    }

    template <typename... T>
    bool TypeIs(var_types type, T... rest) const
    {
        return TypeIs(type) || TypeIs(rest...);
    }

    static constexpr bool StaticOperIs(genTreeOps operCompare, genTreeOps oper)
    {
        return operCompare == oper;
    }

    template <typename... T>
    static bool StaticOperIs(genTreeOps operCompare, genTreeOps oper, T... rest)
    {
        return StaticOperIs(operCompare, oper) || StaticOperIs(operCompare, rest...);
    }

    bool OperIs(genTreeOps oper) const
    {
        return OperGet() == oper;
    }

    template <typename... T>
    bool OperIs(genTreeOps oper, T... rest) const
    {
        return OperIs(oper) || OperIs(rest...);
    }

    static bool OperIsConst(genTreeOps gtOper)
    {
        static_assert_no_msg(AreContiguous(GT_CNS_INT, GT_CNS_LNG, GT_CNS_DBL, GT_CNS_STR, GT_CNS_VEC));
        return (GT_CNS_INT <= gtOper) && (gtOper <= GT_CNS_VEC);
    }

    bool OperIsConst() const
    {
        return OperIsConst(gtOper);
    }

    static bool OperIsLeaf(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_LEAF) != 0;
    }

    bool OperIsLeaf() const
    {
        return (OperKind(gtOper) & GTK_LEAF) != 0;
    }

    static bool OperIsLocal(genTreeOps gtOper)
    {
        static_assert_no_msg(AreContiguous(GT_PHI_ARG, GT_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_VAR, GT_STORE_LCL_FLD));
        return (GT_PHI_ARG <= gtOper) && (gtOper <= GT_STORE_LCL_FLD);
    }

    static bool OperIsAnyLocal(genTreeOps gtOper)
    {
        static_assert_no_msg(
            AreContiguous(GT_PHI_ARG, GT_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_VAR, GT_STORE_LCL_FLD, GT_LCL_ADDR));
        return (GT_PHI_ARG <= gtOper) && (gtOper <= GT_LCL_ADDR);
    }

    static bool OperIsLocalField(genTreeOps gtOper)
    {
        return (gtOper == GT_LCL_FLD || gtOper == GT_LCL_ADDR || gtOper == GT_STORE_LCL_FLD);
    }

    bool OperIsLocalField() const
    {
        return OperIsLocalField(gtOper);
    }

    static bool OperIsScalarLocal(genTreeOps gtOper)
    {
        return (gtOper == GT_LCL_VAR || gtOper == GT_STORE_LCL_VAR);
    }

    static bool OperIsNonPhiLocal(genTreeOps gtOper)
    {
        return OperIsLocal(gtOper) && (gtOper != GT_PHI_ARG);
    }

    static bool OperIsLocalRead(genTreeOps gtOper)
    {
        return (OperIsLocal(gtOper) && !OperIsLocalStore(gtOper));
    }

    static bool OperIsLocalStore(genTreeOps gtOper)
    {
        return (gtOper == GT_STORE_LCL_VAR || gtOper == GT_STORE_LCL_FLD);
    }

    static bool OperIsAddrMode(genTreeOps gtOper)
    {
        return (gtOper == GT_LEA);
    }

    static bool OperIsInitVal(genTreeOps gtOper)
    {
        return (gtOper == GT_INIT_VAL);
    }

    bool OperIsInitVal() const
    {
        return OperIsInitVal(OperGet());
    }

    bool IsInitVal() const
    {
        return IsIntegralConst(0) || OperIsInitVal();
    }

    bool IsConstInitVal() const
    {
        return (gtOper == GT_CNS_INT) || (OperIsInitVal() && (gtGetOp1()->gtOper == GT_CNS_INT));
    }

    bool OperIsBlkOp();
    bool OperIsCopyBlkOp();
    bool OperIsInitBlkOp();

    static bool OperIsBlk(genTreeOps gtOper)
    {
        return (gtOper == GT_BLK) || OperIsStoreBlk(gtOper);
    }

    bool OperIsBlk() const
    {
        return OperIsBlk(OperGet());
    }

    static bool OperIsStoreBlk(genTreeOps gtOper)
    {
        return StaticOperIs(gtOper, GT_STORE_BLK, GT_STORE_DYN_BLK);
    }

    bool OperIsStoreBlk() const
    {
        return OperIsStoreBlk(OperGet());
    }

    bool OperIsPutArgSplit() const
    {
#if FEATURE_ARG_SPLIT
        assert((gtOper != GT_PUTARG_SPLIT) || compFeatureArgSplit());
        return gtOper == GT_PUTARG_SPLIT;
#else // !FEATURE_ARG_SPLIT
        return false;
#endif
    }

    bool OperIsPutArgStk() const
    {
        return gtOper == GT_PUTARG_STK;
    }

    bool OperIsPutArgStkOrSplit() const
    {
        return OperIsPutArgStk() || OperIsPutArgSplit();
    }

    bool OperIsPutArgReg() const
    {
        return gtOper == GT_PUTARG_REG;
    }

    bool OperIsPutArg() const
    {
        return OperIsPutArgStk() || OperIsPutArgReg() || OperIsPutArgSplit();
    }

    bool OperIsFieldList() const
    {
        return OperIs(GT_FIELD_LIST);
    }

    bool OperIsMultiRegOp() const
    {
#if !defined(TARGET_64BIT)
        if (OperIs(GT_MUL_LONG))
        {
            return true;
        }
#if defined(TARGET_ARM)
        if (OperIs(GT_PUTARG_REG, GT_BITCAST))
        {
            return true;
        }
#endif // TARGET_ARM
#endif // TARGET_64BIT
        return false;
    }

    bool OperIsAddrMode() const
    {
        return OperIsAddrMode(OperGet());
    }

    bool OperIsLocal() const
    {
        return OperIsLocal(OperGet());
    }

    bool OperIsAnyLocal() const
    {
        return OperIsAnyLocal(OperGet());
    }

    bool OperIsScalarLocal() const
    {
        return OperIsScalarLocal(OperGet());
    }

    bool OperIsNonPhiLocal() const
    {
        return OperIsNonPhiLocal(OperGet());
    }

    bool OperIsLocalStore() const
    {
        return OperIsLocalStore(OperGet());
    }

    bool OperIsLocalRead() const
    {
        return OperIsLocalRead(OperGet());
    }

    static bool OperIsCompare(genTreeOps gtOper)
    {
        // Note that only GT_EQ to GT_GT are HIR nodes, GT_TEST and GT_BITTEST
        // nodes are backend nodes only.
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef TARGET_XARCH
        static_assert_no_msg(AreContiguous(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT, GT_TEST_EQ, GT_TEST_NE,
                                           GT_BITTEST_EQ, GT_BITTEST_NE));
        return (GT_EQ <= gtOper) && (gtOper <= GT_BITTEST_NE);
#else
        static_assert_no_msg(AreContiguous(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT, GT_TEST_EQ, GT_TEST_NE));
        return (GT_EQ <= gtOper) && (gtOper <= GT_TEST_NE);
#endif
    }

    bool OperIsCompare() const
    {
        return OperIsCompare(OperGet());
    }

    // Oper is a compare that generates a cmp instruction (as opposed to a test instruction).
    static bool OperIsCmpCompare(genTreeOps gtOper)
    {
        static_assert_no_msg(AreContiguous(GT_EQ, GT_NE, GT_LT, GT_LE, GT_GE, GT_GT));
        return (GT_EQ <= gtOper) && (gtOper <= GT_GT);
    }

    bool OperIsCmpCompare() const
    {
        return OperIsCmpCompare(OperGet());
    }

    static bool OperIsConditional(genTreeOps gtOper)
    {
        return (GT_SELECT == gtOper);
    }

    bool OperIsConditional() const
    {
        return OperIsConditional(OperGet());
    }

    static bool OperIsCC(genTreeOps gtOper)
    {
        return (gtOper == GT_JCC) || (gtOper == GT_SETCC);
    }

    bool OperIsCC() const
    {
        return OperIsCC(OperGet());
    }

    static bool OperIsShift(genTreeOps gtOper)
    {
        return (gtOper == GT_LSH) || (gtOper == GT_RSH) || (gtOper == GT_RSZ);
    }

    bool OperIsShift() const
    {
        return OperIsShift(OperGet());
    }

    static bool OperIsShiftLong(genTreeOps gtOper)
    {
#ifdef TARGET_64BIT
        return false;
#else
        return (gtOper == GT_LSH_HI) || (gtOper == GT_RSH_LO);
#endif
    }

    bool OperIsShiftLong() const
    {
        return OperIsShiftLong(OperGet());
    }

    static bool OperIsRotate(genTreeOps gtOper)
    {
        return (gtOper == GT_ROL) || (gtOper == GT_ROR);
    }

    bool OperIsRotate() const
    {
        return OperIsRotate(OperGet());
    }

    static bool OperIsShiftOrRotate(genTreeOps gtOper)
    {
        return OperIsShift(gtOper) || OperIsRotate(gtOper) || OperIsShiftLong(gtOper);
    }

    bool OperIsShiftOrRotate() const
    {
        return OperIsShiftOrRotate(OperGet());
    }

    static bool OperIsMul(genTreeOps gtOper)
    {
        return (gtOper == GT_MUL) || (gtOper == GT_MULHI)
#if !defined(TARGET_64BIT) || defined(TARGET_ARM64)
               || (gtOper == GT_MUL_LONG)
#endif
            ;
    }

    bool OperIsMul() const
    {
        return OperIsMul(gtOper);
    }

    bool OperIsArithmetic() const
    {
        genTreeOps op = OperGet();
        return op == GT_ADD || op == GT_SUB || op == GT_MUL || op == GT_DIV || op == GT_MOD

               || op == GT_UDIV || op == GT_UMOD

               || op == GT_OR || op == GT_XOR || op == GT_AND

               || OperIsShiftOrRotate(op);
    }

#ifdef TARGET_XARCH
    static bool OperIsRMWMemOp(genTreeOps gtOper)
    {
        // Return if binary op is one of the supported operations for RMW of memory.
        return (gtOper == GT_ADD || gtOper == GT_SUB || gtOper == GT_AND || gtOper == GT_OR || gtOper == GT_XOR ||
                gtOper == GT_NOT || gtOper == GT_NEG || OperIsShiftOrRotate(gtOper));
    }
    bool OperIsRMWMemOp() const
    {
        // Return if binary op is one of the supported operations for RMW of memory.
        return OperIsRMWMemOp(gtOper);
    }
#endif // TARGET_XARCH

    static bool OperIsUnary(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_UNOP) != 0;
    }

    bool OperIsUnary() const
    {
        return OperIsUnary(gtOper);
    }

    static bool OperIsBinary(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_BINOP) != 0;
    }

    bool OperIsBinary() const
    {
        return OperIsBinary(gtOper);
    }

    static bool OperIsSimple(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_SMPOP) != 0;
    }

    static bool OperIsSpecial(genTreeOps gtOper)
    {
        return ((OperKind(gtOper) & GTK_KINDMASK) == GTK_SPECIAL);
    }

    bool OperIsSimple() const
    {
        return OperIsSimple(gtOper);
    }

#ifdef FEATURE_HW_INTRINSICS
    bool isCommutativeHWIntrinsic() const;
    bool isContainableHWIntrinsic() const;
    bool isRMWHWIntrinsic(Compiler* comp);
    bool isEvexCompatibleHWIntrinsic() const;
    bool isEvexEmbeddedMaskingCompatibleHWIntrinsic() const;
#else
    bool isCommutativeHWIntrinsic() const
    {
        return false;
    }

    bool isContainableHWIntrinsic() const
    {
        return false;
    }

    bool isRMWHWIntrinsic(Compiler* comp)
    {
        return false;
    }

    bool isEvexCompatibleHWIntrinsic() const
    {
        return false;
    }

    bool isEvexEmbeddedMaskingCompatibleHWIntrinsic() const
    {
        return false;
    }
#endif // FEATURE_HW_INTRINSICS

    static bool OperIsCommutative(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_COMMUTE) != 0;
    }

    bool OperIsCommutative()
    {
        return OperIsCommutative(gtOper) || (OperIsHWIntrinsic(gtOper) && isCommutativeHWIntrinsic());
    }

    static bool OperMayOverflow(genTreeOps gtOper)
    {
        return ((gtOper == GT_ADD) || (gtOper == GT_SUB) || (gtOper == GT_MUL) || (gtOper == GT_CAST)
#if !defined(TARGET_64BIT)
                || (gtOper == GT_ADD_HI) || (gtOper == GT_SUB_HI)
#endif
                    );
    }

    bool OperMayOverflow() const
    {
        return OperMayOverflow(gtOper);
    }

    // This returns true only for GT_IND and GT_STOREIND, and is used in contexts where a "true"
    // indirection is expected (i.e. either a load to or a store from a single register).
    // OperIsIndir() returns true also for indirection nodes such as GT_BLK, etc. as well as GT_NULLCHECK.
    static bool OperIsIndir(genTreeOps gtOper)
    {
        static_assert_no_msg(AreContiguous(GT_LOCKADD, GT_XAND, GT_XORR, GT_XADD, GT_XCHG, GT_CMPXCHG, GT_IND,
                                           GT_STOREIND, GT_BLK, GT_STORE_BLK, GT_STORE_DYN_BLK, GT_NULLCHECK));
        return (GT_LOCKADD <= gtOper) && (gtOper <= GT_NULLCHECK);
    }

    static bool OperIsArrLength(genTreeOps gtOper)
    {
        return (gtOper == GT_ARR_LENGTH) || (gtOper == GT_MDARR_LENGTH);
    }

    static bool OperIsMDArr(genTreeOps gtOper)
    {
        return (gtOper == GT_MDARR_LENGTH) || (gtOper == GT_MDARR_LOWER_BOUND);
    }

    // Is this an access of an SZ array length, MD array length, or MD array lower bounds?
    static bool OperIsArrMetaData(genTreeOps gtOper)
    {
        return (gtOper == GT_ARR_LENGTH) || (gtOper == GT_MDARR_LENGTH) || (gtOper == GT_MDARR_LOWER_BOUND);
    }

    static bool OperIsIndirOrArrMetaData(genTreeOps gtOper)
    {
        return OperIsIndir(gtOper) || OperIsArrMetaData(gtOper);
    }

    bool OperIsIndir() const
    {
        return OperIsIndir(gtOper);
    }

    bool OperIsArrLength() const
    {
        return OperIsArrLength(gtOper);
    }

    bool OperIsMDArr() const
    {
        return OperIsMDArr(gtOper);
    }

    bool OperIsIndirOrArrMetaData() const
    {
        return OperIsIndirOrArrMetaData(gtOper);
    }

    // Helper function to return the array reference of an array length node.
    GenTree* GetArrLengthArrRef();

    // Helper function to return the address of an indir or array meta-data node.
    GenTree* GetIndirOrArrMetaDataAddr();

    bool IndirMayFault(Compiler* compiler);

    bool OperIsImplicitIndir() const;

    static bool OperIsAtomicOp(genTreeOps gtOper)
    {
        switch (gtOper)
        {
            case GT_XADD:
            case GT_XORR:
            case GT_XAND:
            case GT_XCHG:
            case GT_LOCKADD:
            case GT_CMPXCHG:
                return true;
            default:
                return false;
        }
    }

    bool OperIsAtomicOp() const
    {
        return OperIsAtomicOp(gtOper);
    }

    static bool OperIsLoad(genTreeOps gtOper)
    {
        return (gtOper == GT_IND) || (gtOper == GT_BLK);
    }

    bool OperIsLoad() const
    {
        return OperIsLoad(gtOper);
    }

    static bool OperIsStore(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_STORE) != 0;
    }

    bool OperIsStore() const
    {
        return OperIsStore(gtOper);
    }

    static bool OperIsMultiOp(genTreeOps gtOper)
    {
        return OperIsHWIntrinsic(gtOper);
    }

    bool OperIsMultiOp() const
    {
        return OperIsMultiOp(OperGet());
    }

    bool OperIsSsaDef() const
    {
        return OperIsLocalStore() || OperIs(GT_CALL);
    }

    static bool OperIsHWIntrinsic(genTreeOps gtOper)
    {
#ifdef FEATURE_HW_INTRINSICS
        return gtOper == GT_HWINTRINSIC;
#else
        return false;
#endif // FEATURE_HW_INTRINSICS
    }

    bool OperIsHWIntrinsic() const
    {
        return OperIsHWIntrinsic(gtOper);
    }

    bool OperIsHWIntrinsic(NamedIntrinsic intrinsicId) const;

    // This is here for cleaner GT_LONG #ifdefs.
    static bool OperIsLong(genTreeOps gtOper)
    {
#if defined(TARGET_64BIT)
        return false;
#else
        return gtOper == GT_LONG;
#endif
    }

    bool OperIsLong() const
    {
        return OperIsLong(gtOper);
    }

    bool OperIsConditionalJump() const
    {
        return OperIs(GT_JTRUE, GT_JCMP, GT_JTEST, GT_JCC);
    }

    bool OperConsumesFlags() const
    {
#if !defined(TARGET_64BIT)
        if (OperIs(GT_ADD_HI, GT_SUB_HI))
        {
            return true;
        }
#endif
#if defined(TARGET_ARM64)
        if (OperIs(GT_CCMP, GT_SELECT_INCCC, GT_SELECT_INVCC, GT_SELECT_NEGCC))
        {
            return true;
        }
#endif
        return OperIs(GT_JCC, GT_SETCC, GT_SELECTCC);
    }

#ifdef DEBUG
    static const GenTreeDebugOperKind gtDebugOperKindTable[];

    static GenTreeDebugOperKind DebugOperKind(genTreeOps oper)
    {
        assert(oper < GT_COUNT);

        return gtDebugOperKindTable[oper];
    }

    GenTreeDebugOperKind DebugOperKind() const
    {
        return DebugOperKind(OperGet());
    }

    bool NullOp1Legal() const
    {
        assert(OperIsSimple());
        switch (gtOper)
        {
            case GT_LEA:
            case GT_RETFILT:
            case GT_FIELD_ADDR:
                return true;
            case GT_RETURN:
                return gtType == TYP_VOID;
            default:
                return false;
        }
    }

    bool NullOp2Legal() const
    {
        assert(OperIsSimple(gtOper) || OperIsBlk(gtOper));
        if (!OperIsBinary(gtOper))
        {
            return true;
        }
        switch (gtOper)
        {
            case GT_INTRINSIC:
            case GT_LEA:
#if defined(TARGET_ARM)
            case GT_PUTARG_REG:
#endif // defined(TARGET_ARM)
#if defined(TARGET_ARM64)
            case GT_SELECT_NEGCC:
            case GT_SELECT_INCCC:
#endif // defined(TARGET_ARM64)

                return true;
            default:
                return false;
        }
    }

    bool OperIsLIR() const
    {
        if (OperIs(GT_NOP))
        {
            // NOPs may only be present in LIR if they do not produce a value.
            return IsNothingNode();
        }

        return (DebugOperKind() & DBK_NOTLIR) == 0;
    }

    bool OperSupportsReverseOpEvalOrder(Compiler* comp) const;
    static bool RequiresNonNullOp2(genTreeOps oper);
    bool IsValidCallArgument();
#endif // DEBUG

    inline bool IsIntegralConst(ssize_t constVal) const;
    inline bool IsFloatAllBitsSet() const;
    inline bool IsFloatNaN() const;
    inline bool IsFloatPositiveZero() const;
    inline bool IsFloatNegativeZero() const;
    inline bool IsVectorZero() const;
    inline bool IsVectorCreate() const;
    inline bool IsVectorAllBitsSet() const;
    inline bool IsVectorConst();

    inline uint64_t GetIntegralVectorConstElement(size_t index, var_types simdBaseType);

    inline bool IsBoxedValue();

    inline GenTree* gtGetOp1() const;

    // Directly return op2. Asserts the node is binary. Might return nullptr if the binary node allows
    // a nullptr op2, such as GT_LEA. This is more efficient than gtGetOp2IfPresent() if you know what
    // node type you have.
    inline GenTree* gtGetOp2() const;

    // The returned pointer might be nullptr if the node is not binary, or if non-null op2 is not required.
    inline GenTree* gtGetOp2IfPresent() const;

    inline GenTree*& Data();

    bool TryGetUse(GenTree* operand, GenTree*** pUse);

    bool TryGetUse(GenTree* operand)
    {
        GenTree** unusedUse = nullptr;
        return TryGetUse(operand, &unusedUse);
    }

private:
    bool TryGetUseBinOp(GenTree* operand, GenTree*** pUse);

public:
    GenTree* gtGetParent(GenTree*** pUse);

    void ReplaceOperand(GenTree** useEdge, GenTree* replacement);

    inline GenTree* gtEffectiveVal();

    inline GenTree* gtCommaStoreVal();

    // Return the child of this node if it is a GT_RELOAD or GT_COPY; otherwise simply return the node itself
    inline GenTree* gtSkipReloadOrCopy();

    // Returns true if it is a call node returning its value in more than one register
    inline bool IsMultiRegCall() const;

    // Returns true if it is a struct lclVar node residing in multiple registers.
    inline bool IsMultiRegLclVar() const;

    // Returns true if it is a node returning its value in more than one register
    bool IsMultiRegNode() const;

    // Returns the number of registers defined by a multireg node.
    unsigned GetMultiRegCount(Compiler* comp) const;

    // Returns the regIndex'th register defined by a possibly-multireg node.
    regNumber GetRegByIndex(int regIndex) const;

    // Returns the type of the regIndex'th register defined by a multi-reg node.
    var_types GetRegTypeByIndex(int regIndex) const;

    // Returns the GTF flag equivalent for the regIndex'th register of a multi-reg node.
    GenTreeFlags GetRegSpillFlagByIdx(int regIndex) const;

    // Sets the GTF flag equivalent for the regIndex'th register of a multi-reg node.
    void SetRegSpillFlagByIdx(GenTreeFlags flags, int regIndex);

#ifdef TARGET_ARM64
    bool NeedsConsecutiveRegisters() const;
#endif

    // Last-use information for either GenTreeLclVar or GenTreeCopyOrReload nodes.
private:
    GenTreeFlags GetLastUseBit(int regIndex) const;

public:
    bool IsLastUse(int fieldIndex) const;
    bool HasLastUse() const;
    void SetLastUse(int fieldIndex);
    void ClearLastUse(int fieldIndex);

    // Returns true if it is a GT_COPY or GT_RELOAD node
    inline bool IsCopyOrReload() const;

    // Returns true if it is a GT_COPY or GT_RELOAD of a multi-reg call node
    inline bool IsCopyOrReloadOfMultiRegCall() const;

    bool OperRequiresAsgFlag() const;

    bool OperRequiresCallFlag(Compiler* comp) const;

    ExceptionSetFlags OperExceptions(Compiler* comp);
    bool OperMayThrow(Compiler* comp);

    bool OperRequiresGlobRefFlag(Compiler* comp) const;

    bool OperSupportsOrderingSideEffect() const;

    GenTreeFlags OperEffects(Compiler* comp);

    unsigned GetScaleIndexMul();
    unsigned GetScaleIndexShf();
    unsigned GetScaledIndex();

public:
    static unsigned char s_gtNodeSizes[];
#if NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS
    static unsigned char s_gtTrueSizes[];
#endif
#if COUNT_AST_OPERS
    static unsigned s_gtNodeCounts[];
#endif

    static void InitNodeSize();

    size_t GetNodeSize() const;

    bool IsNodeProperlySized() const;

    void ReplaceWith(GenTree* src, Compiler* comp);

    static genTreeOps ReverseRelop(genTreeOps relop);

    static genTreeOps SwapRelop(genTreeOps relop);

    //---------------------------------------------------------------------

    static bool Compare(GenTree* op1, GenTree* op2, bool swapOK = false);

//---------------------------------------------------------------------

#if defined(DEBUG) || CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_MEM_ALLOC ||     \
    NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS || DUMP_FLOWGRAPHS
    static const char* OpName(genTreeOps op);
#endif

#if MEASURE_NODE_SIZE
    static const char* OpStructName(genTreeOps op);
#endif

    //---------------------------------------------------------------------

    bool IsNothingNode() const;
    void gtBashToNOP();

    // Value number update action enumeration
    enum ValueNumberUpdate
    {
        CLEAR_VN,   // Clear value number
        PRESERVE_VN // Preserve value number
    };

    void SetOper(genTreeOps oper, ValueNumberUpdate vnUpdate = CLEAR_VN);
    void ChangeOper(genTreeOps oper, ValueNumberUpdate vnUpdate = CLEAR_VN);
    void SetOperRaw(genTreeOps oper);

    void ChangeType(var_types newType)
    {
        var_types oldType = gtType;
        gtType            = newType;
        GenTree* node     = this;
        while (node->gtOper == GT_COMMA)
        {
            node = node->gtGetOp2();
            if (node->gtType != newType)
            {
                assert(node->gtType == oldType);
                node->gtType = newType;
            }
        }
    }

    template <typename T>
    void BashToConst(T value, var_types type = TYP_UNDEF);
    void BashToZeroConst(var_types type);
    GenTreeLclVar* BashToLclVar(Compiler* comp, unsigned lclNum);

#if NODEBASH_STATS
    static void RecordOperBashing(genTreeOps operOld, genTreeOps operNew);
    static void ReportOperBashing(FILE* fp);
#else
    static void RecordOperBashing(genTreeOps operOld, genTreeOps operNew)
    { /* do nothing */
    }
    static void ReportOperBashing(FILE* fp)
    { /* do nothing */
    }
#endif

    bool IsLocal() const
    {
        return OperIsLocal(OperGet());
    }

    bool IsAnyLocal() const
    {
        return OperIsAnyLocal(OperGet());
    }

    bool IsLclVarAddr() const;

    // Returns "true" iff 'this' is a GT_LCL_FLD or GT_STORE_LCL_FLD on which the type
    // is not the same size as the type of the GT_LCL_VAR.
    bool IsPartialLclFld(Compiler* comp);

    bool DefinesLocal(Compiler*             comp,
                      GenTreeLclVarCommon** pLclVarTree,
                      bool*                 pIsEntire = nullptr,
                      ssize_t*              pOffset   = nullptr,
                      unsigned*             pSize     = nullptr);

    GenTreeLclVarCommon* IsImplicitByrefParameterValuePreMorph(Compiler* compiler);
    GenTreeLclVar* IsImplicitByrefParameterValuePostMorph(Compiler* compiler, GenTree** addr);

    // Determine whether this is an assignment tree of the form X = X (op) Y,
    // where Y is an arbitrary tree, and X is a lclVar.
    unsigned IsLclVarUpdateTree(GenTree** otherTree, genTreeOps* updateOper);

    // Determine whether this tree is a basic block profile count update.
    bool IsBlockProfileUpdate();

    bool IsFieldAddr(Compiler* comp, GenTree** pBaseAddr, FieldSeq** pFldSeq, ssize_t* pOffset);

    bool IsArrayAddr(GenTreeArrAddr** pArrAddr);

    bool SupportsSettingZeroFlag();

    // These are only used for dumping.
    // The GetRegNum() is only valid in LIR, but the dumping methods are not easily
    // modified to check this.
    CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
    bool InReg() const
    {
        return (GetRegTag() != GT_REGTAG_NONE) ? true : false;
    }
    regNumber GetReg() const
    {
        return (GetRegTag() != GT_REGTAG_NONE) ? GetRegNum() : REG_NA;
    }
#endif

    static bool IsContained(unsigned flags)
    {
        return ((flags & GTF_CONTAINED) != 0);
    }

    void SetContained()
    {
        assert(IsValue());
        gtFlags |= GTF_CONTAINED;
        assert(isContained());
    }

    void ClearContained()
    {
        assert(IsValue());
        gtFlags &= ~GTF_CONTAINED;
        ClearRegOptional();
    }

    bool CanCSE() const
    {
        return ((gtFlags & GTF_DONT_CSE) == 0);
    }

    void SetDoNotCSE()
    {
        gtFlags |= GTF_DONT_CSE;
    }

    void ClearDoNotCSE()
    {
        gtFlags &= ~GTF_DONT_CSE;
    }

    bool IsReverseOp() const
    {
        return (gtFlags & GTF_REVERSE_OPS) ? true : false;
    }

    void SetReverseOp()
    {
        gtFlags |= GTF_REVERSE_OPS;
    }

    void ClearReverseOp()
    {
        gtFlags &= ~GTF_REVERSE_OPS;
    }

#if defined(TARGET_XARCH)
    void SetDontExtend()
    {
        assert(varTypeIsSmall(TypeGet()) && OperIs(GT_IND, GT_LCL_FLD));
        gtFlags |= GTF_DONT_EXTEND;
    }

    void ClearDontExtend()
    {
        gtFlags &= ~GTF_DONT_EXTEND;
    }

    bool DontExtend() const
    {
        assert(varTypeIsSmall(TypeGet()) || ((gtFlags & GTF_DONT_EXTEND) == 0));
        return (gtFlags & GTF_DONT_EXTEND) != 0;
    }
#endif // TARGET_XARCH

    bool IsUnsigned() const
    {
        return ((gtFlags & GTF_UNSIGNED) != 0);
    }

    void SetUnsigned()
    {
        assert(OperIs(GT_ADD, GT_SUB, GT_CAST, GT_LE, GT_LT, GT_GT, GT_GE) || OperIsMul());
        gtFlags |= GTF_UNSIGNED;
    }

    void ClearUnsigned()
    {
        assert(OperIs(GT_ADD, GT_SUB, GT_CAST) || OperIsMul());
        gtFlags &= ~GTF_UNSIGNED;
    }

    void SetOverflow()
    {
        assert(OperMayOverflow());
        gtFlags |= GTF_OVERFLOW;
    }

    void ClearOverflow()
    {
        assert(OperMayOverflow());
        gtFlags &= ~GTF_OVERFLOW;
    }

    bool Is64RsltMul() const
    {
        return (gtFlags & GTF_MUL_64RSLT) != 0;
    }

    void Set64RsltMul()
    {
        gtFlags |= GTF_MUL_64RSLT;
    }

    void Clear64RsltMul()
    {
        gtFlags &= ~GTF_MUL_64RSLT;
    }

    void SetAllEffectsFlags(GenTree* source)
    {
        SetAllEffectsFlags(source->gtFlags & GTF_ALL_EFFECT);
    }

    void SetAllEffectsFlags(GenTree* firstSource, GenTree* secondSource)
    {
        SetAllEffectsFlags((firstSource->gtFlags | secondSource->gtFlags) & GTF_ALL_EFFECT);
    }

    void SetAllEffectsFlags(GenTree* firstSource, GenTree* secondSource, GenTree* thirdSource)
    {
        SetAllEffectsFlags((firstSource->gtFlags | secondSource->gtFlags | thirdSource->gtFlags) & GTF_ALL_EFFECT);
    }

    void SetAllEffectsFlags(GenTreeFlags sourceFlags)
    {
        assert((sourceFlags & ~GTF_ALL_EFFECT) == 0);

        gtFlags &= ~GTF_ALL_EFFECT;
        gtFlags |= sourceFlags;
    }

    void AddAllEffectsFlags(GenTree* source)
    {
        AddAllEffectsFlags(source->gtFlags & GTF_ALL_EFFECT);
    }

    void AddAllEffectsFlags(GenTree* firstSource, GenTree* secondSource)
    {
        AddAllEffectsFlags((firstSource->gtFlags | secondSource->gtFlags) & GTF_ALL_EFFECT);
    }

    void AddAllEffectsFlags(GenTreeFlags sourceFlags)
    {
        assert((sourceFlags & ~GTF_ALL_EFFECT) == 0);
        gtFlags |= sourceFlags;
    }

    void SetHasOrderingSideEffect()
    {
        assert(OperSupportsOrderingSideEffect());
        gtFlags |= GTF_ORDER_SIDEEFF;
    }

    inline bool IsCnsIntOrI() const;

    inline bool IsIntegralConst() const;

    inline bool IsIntegralConstPow2() const;

    inline bool IsIntegralConstUnsignedPow2() const;

    inline bool IsIntegralConstAbsPow2() const;

    inline bool IsIntCnsFitsInI32(); // Constant fits in INT32

    inline bool IsCnsFltOrDbl() const;

    inline bool IsCnsNonZeroFltOrDbl() const;

    inline bool IsCnsVec() const;

    bool IsIconHandle() const
    {
        return (gtOper == GT_CNS_INT) && ((gtFlags & GTF_ICON_HDL_MASK) != 0);
    }

    bool IsIconHandle(GenTreeFlags handleType) const
    {
        // check that handleType is one of the valid GTF_ICON_* values
        assert((handleType & GTF_ICON_HDL_MASK) != 0);
        assert((handleType & ~GTF_ICON_HDL_MASK) == 0);
        return (gtOper == GT_CNS_INT) && ((gtFlags & GTF_ICON_HDL_MASK) == handleType);
    }

    template <typename... T>
    bool IsIconHandle(GenTreeFlags handleType, T... rest) const
    {
        return IsIconHandle(handleType) || IsIconHandle(rest...);
    }

    // Return just the part of the flags corresponding to the GTF_ICON_*_HDL flag.
    // For non-icon handle trees, returns GTF_EMPTY.
    GenTreeFlags GetIconHandleFlag() const
    {
        return (gtOper == GT_CNS_INT) ? (gtFlags & GTF_ICON_HDL_MASK) : GTF_EMPTY;
    }

    // Mark this node as no longer being a handle; clear its GTF_ICON_*_HDL bits.
    void ClearIconHandleMask()
    {
        assert(gtOper == GT_CNS_INT);
        gtFlags &= ~GTF_ICON_HDL_MASK;
    }

#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    bool IsEmbMaskOp()
    {
        bool result = (gtFlags & GTF_HW_EM_OP) != 0;
        assert(!result || (gtOper == GT_HWINTRINSIC));
        return result;
    }

    void MakeEmbMaskOp()
    {
        assert(!IsEmbMaskOp());
        gtFlags |= GTF_HW_EM_OP;
    }

#endif // TARGET_XARCH && FEATURE_HW_INTRINSICS

    bool IsCall() const
    {
        return OperGet() == GT_CALL;
    }
    inline bool IsHelperCall();

    bool gtOverflow() const;
    bool gtOverflowEx() const;
    bool gtSetFlags() const;
    bool gtRequestSetFlags();

#ifdef DEBUG
    static int gtDispFlags(GenTreeFlags flags, GenTreeDebugFlags debugFlags);
    static const char* gtGetHandleKindString(GenTreeFlags flags);
#endif

    // cast operations
    inline var_types  CastFromType();
    inline var_types& CastToType();

    // Returns "true" iff "this" is a phi-related node (i.e. a GT_PHI_ARG, GT_PHI, or a PhiDefn).
    bool IsPhiNode();

    // Returns "true" iff "*this" is a store (GT_STORE_LCL_VAR) tree that defines an SSA name (lcl = phi(...));
    bool IsPhiDefn();

    // Because of the fact that we hid the assignment operator of "BitSet" (in DEBUG),
    // we can't synthesize an assignment operator.
    // TODO-Cleanup: Could change this w/o liveset on tree nodes
    // (This is also necessary for the VTable trick.)
    GenTree()
    {
    }

    // Returns an iterator that will produce the use edge to each operand of this node. Differs
    // from the sequence of nodes produced by a loop over `GetChild` in its handling of call, phi,
    // and block op nodes.
    GenTreeUseEdgeIterator UseEdgesBegin();
    GenTreeUseEdgeIterator UseEdgesEnd();

    IteratorPair<GenTreeUseEdgeIterator> UseEdges();

    // Returns an iterator that will produce each operand of this node, in execution order.
    GenTreeOperandIterator OperandsBegin();
    GenTreeOperandIterator OperandsEnd();

    // Returns a range that will produce the operands of this node in execution order.
    IteratorPair<GenTreeOperandIterator> Operands();

    enum class VisitResult
    {
        Abort    = false,
        Continue = true
    };

    // Visits each operand of this node. The operand must be either a lambda, function, or functor with the signature
    // `GenTree::VisitResult VisitorFunction(GenTree* operand)`. Here is a simple example:
    //
    //     unsigned operandCount = 0;
    //     node->VisitOperands([&](GenTree* operand) -> GenTree::VisitResult)
    //     {
    //         operandCount++;
    //         return GenTree::VisitResult::Continue;
    //     });
    //
    // This function is generally more efficient that the operand iterator and should be preferred over that API for
    // hot code, as it affords better opportunities for inlining and achieves shorter dynamic path lengths when
    // deciding how operands need to be accessed.
    //
    // Note that this function does not respect `GTF_REVERSE_OPS`. This is always safe in LIR, but may be dangerous
    // in HIR if for some reason you need to visit operands in the order in which they will execute.
    template <typename TVisitor>
    void VisitOperands(TVisitor visitor);

private:
    template <typename TVisitor>
    void VisitBinOpOperands(TVisitor visitor);

public:
    bool Precedes(GenTree* other);

    bool IsInvariant() const;

    bool IsNeverNegative(Compiler* comp) const;
    bool IsNeverNegativeOne(Compiler* comp) const;
    bool IsNeverZero() const;
    bool CanDivOrModPossiblyOverflow(Compiler* comp) const;

    bool IsReuseRegVal() const
    {
        // This can be extended to non-constant nodes, but not to local or indir nodes.
        return OperIsConst() && ((gtFlags & GTF_REUSE_REG_VAL) != 0);
    }

    void SetReuseRegVal()
    {
        assert(OperIsConst());
        gtFlags |= GTF_REUSE_REG_VAL;
    }

    void ResetReuseRegVal()
    {
        assert(OperIsConst());
        gtFlags &= ~GTF_REUSE_REG_VAL;
    }

    void SetIndirExceptionFlags(Compiler* comp);

#if MEASURE_NODE_SIZE
    static void DumpNodeSizes();
#endif

#ifdef DEBUG

private:
    GenTree& operator=(const GenTree& gt)
    {
        assert(!"Don't copy");
        return *this;
    }
#endif // DEBUG

#if DEBUGGABLE_GENTREE
    // In DEBUG builds, add a dummy virtual method, to give the debugger run-time type information.
    virtual void DummyVirt()
    {
    }

    typedef void* VtablePtr;

    VtablePtr GetVtableForOper(genTreeOps oper);
    void SetVtableForOper(genTreeOps oper);

    static VtablePtr s_vtablesForOpers[GT_COUNT];
    static VtablePtr s_vtableForOp;
#endif // DEBUGGABLE_GENTREE

public:
    inline void* operator new(size_t sz, class Compiler*, genTreeOps oper);

    inline GenTree(genTreeOps oper, var_types type DEBUGARG(bool largeNode = false));
};

// Represents a GT_PHI node - a variable sized list of GT_PHI_ARG nodes.
// All PHI_ARG nodes must represent uses of the same local variable and
// the PHI node's type must be the same as the local variable's type.
//
// The PHI node does not represent a definition by itself, it is always
// the value operand of a STORE_LCL_VAR node. The local store node itself
// is the definition for the same local variable referenced by all the
// used PHI_ARG nodes:
//
//   STORE_LCL_VAR<V01>(PHI(PHI_ARG(V01), PHI_ARG(V01), PHI_ARG(V01)))
//
// The order of the PHI_ARG uses is not currently relevant and it may be
// the same or not as the order of the predecessor blocks.
//
struct GenTreePhi final : public GenTree
{
    class Use
    {
        GenTree* m_node;
        Use*     m_next;

    public:
        Use(GenTree* node, Use* next = nullptr) : m_node(node), m_next(next)
        {
            assert(node->OperIs(GT_PHI_ARG));
        }

        GenTree*& NodeRef()
        {
            return m_node;
        }

        GenTree* GetNode() const
        {
            assert(m_node->OperIs(GT_PHI_ARG));
            return m_node;
        }

        void SetNode(GenTree* node)
        {
            assert(node->OperIs(GT_PHI_ARG));
            m_node = node;
        }

        Use*& NextRef()
        {
            return m_next;
        }

        Use* GetNext() const
        {
            return m_next;
        }
    };

    class UseIterator
    {
        Use* m_use;

    public:
        UseIterator(Use* use) : m_use(use)
        {
        }

        Use& operator*() const
        {
            return *m_use;
        }

        Use* operator->() const
        {
            return m_use;
        }

        UseIterator& operator++()
        {
            m_use = m_use->GetNext();
            return *this;
        }

        bool operator==(const UseIterator& i) const
        {
            return m_use == i.m_use;
        }

        bool operator!=(const UseIterator& i) const
        {
            return m_use != i.m_use;
        }
    };

    class UseList
    {
        Use* m_uses;

    public:
        UseList(Use* uses) : m_uses(uses)
        {
        }

        UseIterator begin() const
        {
            return UseIterator(m_uses);
        }

        UseIterator end() const
        {
            return UseIterator(nullptr);
        }
    };

    Use* gtUses;

    GenTreePhi(var_types type) : GenTree(GT_PHI, type), gtUses(nullptr)
    {
    }

    UseList Uses()
    {
        return UseList(gtUses);
    }

    //--------------------------------------------------------------------------
    // Equals: Checks if 2 PHI nodes are equal.
    //
    // Arguments:
    //    phi1 - The first PHI node
    //    phi2 - The second PHI node
    //
    // Return Value:
    //    true if the 2 PHI nodes have the same type, number of uses, and the
    //    uses are equal.
    //
    // Notes:
    //    The order of uses must be the same for equality, even if the
    //    order is not usually relevant and is not guaranteed to reflect
    //    a particular order of the predecessor blocks.
    //
    static bool Equals(GenTreePhi* phi1, GenTreePhi* phi2)
    {
        if (phi1->TypeGet() != phi2->TypeGet())
        {
            return false;
        }

        GenTreePhi::UseIterator i1   = phi1->Uses().begin();
        GenTreePhi::UseIterator end1 = phi1->Uses().end();
        GenTreePhi::UseIterator i2   = phi2->Uses().begin();
        GenTreePhi::UseIterator end2 = phi2->Uses().end();

        for (; (i1 != end1) && (i2 != end2); ++i1, ++i2)
        {
            if (!Compare(i1->GetNode(), i2->GetNode()))
            {
                return false;
            }
        }

        return (i1 == end1) && (i2 == end2);
    }

#if DEBUGGABLE_GENTREE
    GenTreePhi() : GenTree()
    {
    }
#endif
};

// Represents a list of fields constituting a struct, when it is passed as an argument.
//
struct GenTreeFieldList : public GenTree
{
    class Use
    {
        GenTree*  m_node;
        Use*      m_next;
        uint16_t  m_offset;
        var_types m_type;

    public:
        Use(GenTree* node, unsigned offset, var_types type)
            : m_node(node), m_next(nullptr), m_offset(static_cast<uint16_t>(offset)), m_type(type)
        {
            // We can save space on 32 bit hosts by storing the offset as uint16_t. Struct promotion
            // only accepts structs which are much smaller than that - 128 bytes = max 4 fields * max
            // SIMD vector size (32 bytes).
            assert(offset <= UINT16_MAX);
        }

        GenTree*& NodeRef()
        {
            return m_node;
        }

        GenTree* GetNode() const
        {
            return m_node;
        }

        void SetNode(GenTree* node)
        {
            assert(node != nullptr);
            m_node = node;
        }

        Use*& NextRef()
        {
            return m_next;
        }

        Use* GetNext() const
        {
            return m_next;
        }

        void SetNext(Use* next)
        {
            m_next = next;
        }

        unsigned GetOffset() const
        {
            return m_offset;
        }

        var_types GetType() const
        {
            return m_type;
        }

        void SetType(var_types type)
        {
            m_type = type;
        }
    };

    class UseIterator
    {
        Use* use;

    public:
        UseIterator(Use* use) : use(use)
        {
        }

        Use& operator*()
        {
            return *use;
        }

        Use* operator->()
        {
            return use;
        }

        void operator++()
        {
            use = use->GetNext();
        }

        bool operator==(const UseIterator& other)
        {
            return use == other.use;
        }

        bool operator!=(const UseIterator& other)
        {
            return use != other.use;
        }
    };

    class UseList
    {
        Use* m_head;
        Use* m_tail;

    public:
        UseList() : m_head(nullptr), m_tail(nullptr)
        {
        }

        Use* GetHead() const
        {
            return m_head;
        }

        UseIterator begin() const
        {
            return m_head;
        }

        UseIterator end() const
        {
            return nullptr;
        }

        void AddUse(Use* newUse)
        {
            assert(newUse->GetNext() == nullptr);

            if (m_head == nullptr)
            {
                m_head = newUse;
            }
            else
            {
                m_tail->SetNext(newUse);
            }

            m_tail = newUse;
        }

        void InsertUse(Use* insertAfter, Use* newUse)
        {
            assert(newUse->GetNext() == nullptr);

            newUse->SetNext(insertAfter->GetNext());
            insertAfter->SetNext(newUse);

            if (m_tail == insertAfter)
            {
                m_tail = newUse;
            }
        }

        void Reverse()
        {
            m_tail = m_head;
            m_head = nullptr;

            for (Use *next, *use = m_tail; use != nullptr; use = next)
            {
                next = use->GetNext();
                use->SetNext(m_head);
                m_head = use;
            }
        }

        bool IsSorted() const
        {
            unsigned offset = 0;
            for (GenTreeFieldList::Use& use : *this)
            {
                if (use.GetOffset() < offset)
                {
                    return false;
                }
                offset = use.GetOffset();
            }
            return true;
        }
    };

private:
    UseList m_uses;

public:
    GenTreeFieldList() : GenTree(GT_FIELD_LIST, TYP_STRUCT)
    {
        SetContained();
    }

    UseList& Uses()
    {
        return m_uses;
    }

    // Add a new field use to the end of the use list and update side effect flags.
    void AddField(Compiler* compiler, GenTree* node, unsigned offset, var_types type);
    // Add a new field use to the end of the use list without updating side effect flags.
    void AddFieldLIR(Compiler* compiler, GenTree* node, unsigned offset, var_types type);
    // Insert a new field use after the specified use and update side effect flags.
    void InsertField(Compiler* compiler, Use* insertAfter, GenTree* node, unsigned offset, var_types type);
    // Insert a new field use after the specified use without updating side effect flags.
    void InsertFieldLIR(Compiler* compiler, Use* insertAfter, GenTree* node, unsigned offset, var_types type);

    //--------------------------------------------------------------------------
    // Equals: Check if 2 FIELD_LIST nodes are equal.
    //
    // Arguments:
    //    list1 - The first FIELD_LIST node
    //    list2 - The second FIELD_LIST node
    //
    // Return Value:
    //    true if the 2 FIELD_LIST nodes have the same type, number of uses, and the
    //    uses are equal.
    //
    static bool Equals(GenTreeFieldList* list1, GenTreeFieldList* list2)
    {
        assert(list1->TypeGet() == TYP_STRUCT);
        assert(list2->TypeGet() == TYP_STRUCT);

        UseIterator i1   = list1->Uses().begin();
        UseIterator end1 = list1->Uses().end();
        UseIterator i2   = list2->Uses().begin();
        UseIterator end2 = list2->Uses().end();

        for (; (i1 != end1) && (i2 != end2); ++i1, ++i2)
        {
            if (!Compare(i1->GetNode(), i2->GetNode()) || (i1->GetOffset() != i2->GetOffset()) ||
                (i1->GetType() != i2->GetType()))
            {
                return false;
            }
        }

        return (i1 == end1) && (i2 == end2);
    }
};

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator: an iterator that will produce each use edge of a GenTree node in the order in which
//                         they are used.
//
// Operand iteration is common enough in the back end of the compiler that the implementation of this type has
// traded some simplicity for speed:
// - As much work as is reasonable is done in the constructor rather than during operand iteration
// - Node-specific functionality is handled by a small class of "advance" functions called by operator++
//   rather than making operator++ itself handle all nodes
// - Some specialization has been performed for specific node types/shapes (e.g. the advance function for
//   binary nodes is specialized based on whether or not the node has the GTF_REVERSE_OPS flag set)
//
// Valid values of this type may be obtained by calling `GenTree::UseEdgesBegin` and `GenTree::UseEdgesEnd`.
//
class GenTreeUseEdgeIterator final
{
    friend class GenTreeOperandIterator;
    friend GenTreeUseEdgeIterator GenTree::UseEdgesBegin();
    friend GenTreeUseEdgeIterator GenTree::UseEdgesEnd();

    enum
    {
        CALL_ARGS         = 0,
        CALL_LATE_ARGS    = 1,
        CALL_CONTROL_EXPR = 2,
        CALL_COOKIE       = 3,
        CALL_ADDRESS      = 4,
        CALL_TERMINAL     = 5,
    };

    typedef void (GenTreeUseEdgeIterator::*AdvanceFn)();

    AdvanceFn m_advance;
    GenTree*  m_node;
    GenTree** m_edge;
    // Pointer sized state storage, GenTreePhi::Use* or CallArg*
    // or the exclusive end/beginning of GenTreeMultiOp's operand array.
    void* m_statePtr;
    // Integer sized state storage, usually the operand index for non-list based nodes.
    int m_state;

    GenTreeUseEdgeIterator(GenTree* node);

    // Advance functions for special nodes
    void AdvanceCmpXchg();
    void AdvanceArrElem();
    void AdvanceStoreDynBlk();
    void AdvanceFieldList();
    void AdvancePhi();
    void AdvanceConditional();

    template <bool ReverseOperands>
    void           AdvanceBinOp();
    void           SetEntryStateForBinOp();

    // The advance function for call nodes
    template <int state>
    void          AdvanceCall();

#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
    void AdvanceMultiOp();
    void AdvanceReversedMultiOp();
    void SetEntryStateForMultiOp();
#endif

    void Terminate();

public:
    GenTreeUseEdgeIterator();

    inline GenTree** operator*()
    {
        assert(m_state != -1);
        return m_edge;
    }

    inline GenTree** operator->()
    {
        assert(m_state != -1);
        return m_edge;
    }

    inline bool operator==(const GenTreeUseEdgeIterator& other) const
    {
        if (m_state == -1 || other.m_state == -1)
        {
            return m_state == other.m_state;
        }

        return (m_node == other.m_node) && (m_edge == other.m_edge) && (m_statePtr == other.m_statePtr) &&
               (m_state == other.m_state);
    }

    inline bool operator!=(const GenTreeUseEdgeIterator& other) const
    {
        return !(operator==(other));
    }

    GenTreeUseEdgeIterator& operator++();
};

//------------------------------------------------------------------------
// GenTreeOperandIterator: an iterator that will produce each operand of a
//                         GenTree node in the order in which they are
//                         used. This uses `GenTreeUseEdgeIterator` under
//                         the covers.
//
// Note: valid values of this type may be obtained by calling
// `GenTree::OperandsBegin` and `GenTree::OperandsEnd`.
class GenTreeOperandIterator final
{
    friend GenTreeOperandIterator GenTree::OperandsBegin();
    friend GenTreeOperandIterator GenTree::OperandsEnd();

    GenTreeUseEdgeIterator m_useEdges;

    GenTreeOperandIterator(GenTree* node) : m_useEdges(node)
    {
    }

public:
    GenTreeOperandIterator() : m_useEdges()
    {
    }

    inline GenTree* operator*()
    {
        return *(*m_useEdges);
    }

    inline GenTree* operator->()
    {
        return *(*m_useEdges);
    }

    inline bool operator==(const GenTreeOperandIterator& other) const
    {
        return m_useEdges == other.m_useEdges;
    }

    inline bool operator!=(const GenTreeOperandIterator& other) const
    {
        return !(operator==(other));
    }

    inline GenTreeOperandIterator& operator++()
    {
        ++m_useEdges;
        return *this;
    }
};

/*****************************************************************************/
// In the current design, we never instantiate GenTreeUnOp: it exists only to be
// used as a base class.  For unary operators, we instantiate GenTreeOp, with a NULL second
// argument.  We check that this is true dynamically.  We could tighten this and get static
// checking, but that would entail accessing the first child of a unary operator via something
// like gtUnOp.gtOp1 instead of AsOp()->gtOp1.
struct GenTreeUnOp : public GenTree
{
    GenTree* gtOp1;

protected:
    GenTreeUnOp(genTreeOps oper, var_types type DEBUGARG(bool largeNode = false))
        : GenTree(oper, type DEBUGARG(largeNode)), gtOp1(nullptr)
    {
    }

    GenTreeUnOp(genTreeOps oper, var_types type, GenTree* op1 DEBUGARG(bool largeNode = false))
        : GenTree(oper, type DEBUGARG(largeNode)), gtOp1(op1)
    {
        assert(op1 != nullptr || NullOp1Legal());
        if (op1 != nullptr)
        { // Propagate effects flags from child.
            gtFlags |= op1->gtFlags & GTF_ALL_EFFECT;
        }
    }

#if DEBUGGABLE_GENTREE
    GenTreeUnOp() : GenTree(), gtOp1(nullptr)
    {
    }
#endif
};

struct GenTreeOp : public GenTreeUnOp
{
    GenTree* gtOp2;

    GenTreeOp(genTreeOps oper, var_types type, GenTree* op1, GenTree* op2 DEBUGARG(bool largeNode = false))
        : GenTreeUnOp(oper, type, op1 DEBUGARG(largeNode)), gtOp2(op2)
    {
        // comparisons are always integral types
        assert(!GenTree::OperIsCompare(oper) || varTypeIsIntegral(type));
        // Binary operators, with a few exceptions, require a non-nullptr
        // second argument.
        assert(op2 != nullptr || NullOp2Legal());
        // Unary operators, on the other hand, require a null second argument.
        assert(!OperIsUnary(oper) || op2 == nullptr);
        // Propagate effects flags from child.  (UnOp handled this for first child.)
        if (op2 != nullptr)
        {
            gtFlags |= op2->gtFlags & GTF_ALL_EFFECT;
        }
    }

    // A small set of types are unary operators with optional arguments.  We use
    // this constructor to build those.
    GenTreeOp(genTreeOps oper, var_types type DEBUGARG(bool largeNode = false))
        : GenTreeUnOp(oper, type DEBUGARG(largeNode)), gtOp2(nullptr)
    {
        // Unary operators with optional arguments:
        assert(oper == GT_RETURN || oper == GT_RETFILT || OperIsBlk(oper));
    }

    // returns true if we will use the division by constant optimization for this node.
    bool UsesDivideByConstOptimized(Compiler* comp);

    // checks if we will use the division by constant optimization this node
    // then sets the flag GTF_DIV_BY_CNS_OPT and GTF_DONT_CSE on the constant
    void CheckDivideByConstOptimized(Compiler* comp);

#if !defined(TARGET_64BIT) || defined(TARGET_ARM64)
    bool IsValidLongMul();
#endif

#if !defined(TARGET_64BIT) && defined(DEBUG)
    void DebugCheckLongMul();
#endif

#if DEBUGGABLE_GENTREE
    GenTreeOp() : GenTreeUnOp(), gtOp2(nullptr)
    {
    }
#endif
};

struct GenTreeVal : public GenTree
{
    size_t gtVal1;

    GenTreeVal(genTreeOps oper, var_types type, ssize_t val) : GenTree(oper, type), gtVal1(val)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeVal() : GenTree()
    {
    }
#endif
};

struct GenTreeIntConCommon : public GenTree
{
    inline INT64 LngValue() const;
    inline void SetLngValue(INT64 val);
    inline ssize_t IconValue() const;
    inline void SetIconValue(ssize_t val);
    inline INT64 IntegralValue() const;
    inline void SetIntegralValue(int64_t value);

    template <typename T>
    inline void SetValueTruncating(T value);

    GenTreeIntConCommon(genTreeOps oper, var_types type DEBUGARG(bool largeNode = false))
        : GenTree(oper, type DEBUGARG(largeNode))
    {
    }

    bool FitsInI8() // IconValue() fits into 8-bit signed storage
    {
        return FitsInI8(IconValue());
    }

    static bool FitsInI8(ssize_t val) // Constant fits into 8-bit signed storage
    {
        return (int8_t)val == val;
    }

    bool FitsInI32() // IconValue() fits into 32-bit signed storage
    {
        return FitsInI32(IconValue());
    }

    static bool FitsInI32(ssize_t val) // Constant fits into 32-bit signed storage
    {
#ifdef TARGET_64BIT
        return (int32_t)val == val;
#else
        return true;
#endif
    }

    bool ImmedValNeedsReloc(Compiler* comp);
    bool ImmedValCanBeFolded(Compiler* comp, genTreeOps op);

#ifdef TARGET_XARCH
    bool FitsInAddrBase(Compiler* comp);
    bool AddrNeedsReloc(Compiler* comp);
#endif

#if DEBUGGABLE_GENTREE
    GenTreeIntConCommon() : GenTree()
    {
    }
#endif
};

// node representing a read from a physical register
struct GenTreePhysReg : public GenTree
{
    // physregs need a field beyond GetRegNum() because
    // GetRegNum() indicates the destination (and can be changed)
    // whereas reg indicates the source
    regNumber gtSrcReg;
    GenTreePhysReg(regNumber r, var_types type = TYP_I_IMPL) : GenTree(GT_PHYSREG, type), gtSrcReg(r)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreePhysReg() : GenTree()
    {
    }
#endif
};

/* gtIntCon -- integer constant (GT_CNS_INT) */
struct GenTreeIntCon : public GenTreeIntConCommon
{
    /*
     * This is the GT_CNS_INT struct definition.
     * It's used to hold for both int constants and pointer handle constants.
     * For the 64-bit targets we will only use GT_CNS_INT as it used to represent all the possible sizes
     * For the 32-bit targets we use a GT_CNS_LNG to hold a 64-bit integer constant and GT_CNS_INT for all others.
     * In the future when we retarget the JIT for x86 we should consider eliminating GT_CNS_LNG
     */
    ssize_t gtIconVal; // Must overlap and have the same offset with the gtIconVal field in GenTreeLngCon below.

    /* The InitializeArray intrinsic needs to go back to the newarray statement
       to find the class handle of the array so that we can get its size.  However,
       in ngen mode, the handle in that statement does not correspond to the compile
       time handle (rather it lets you get a handle at run-time).  In that case, we also
       need to store a compile time handle, which goes in this gtCompileTimeHandle field.
    */
    ssize_t gtCompileTimeHandle;

    // TODO-Cleanup: It's not clear what characterizes the cases where the field
    // above is used.  It may be that its uses and those of the "gtFieldSeq" field below
    // are mutually exclusive, and they could be put in a union.  Or else we should separate
    // this type into three subtypes.

    // If this constant represents the offset of one or more fields, "gtFieldSeq" represents that
    // sequence of fields.
    FieldSeq* gtFieldSeq;

#ifdef DEBUG
    // If the value represents target address (for a field or call), holds the handle of the field (or call).
    size_t gtTargetHandle = 0;
#endif

    GenTreeIntCon(var_types type, ssize_t value DEBUGARG(bool largeNode = false))
        : GenTreeIntConCommon(GT_CNS_INT, type DEBUGARG(largeNode))
        , gtIconVal(value)
        , gtCompileTimeHandle(0)
        , gtFieldSeq(nullptr)
    {
    }

    GenTreeIntCon(var_types type, ssize_t value, FieldSeq* fields DEBUGARG(bool largeNode = false))
        : GenTreeIntConCommon(GT_CNS_INT, type DEBUGARG(largeNode))
        , gtIconVal(value)
        , gtCompileTimeHandle(0)
        , gtFieldSeq(fields)
    {
    }

    void FixupInitBlkValue(var_types type);

#if DEBUGGABLE_GENTREE
    GenTreeIntCon() : GenTreeIntConCommon()
    {
    }
#endif
};

/* gtLngCon -- long    constant (GT_CNS_LNG) */

struct GenTreeLngCon : public GenTreeIntConCommon
{
    INT64 gtLconVal; // Must overlap and have the same offset with the gtIconVal field in GenTreeIntCon above.
    INT32 LoVal()
    {
        return (INT32)(gtLconVal & 0xffffffff);
    }

    INT32 HiVal()
    {
        return (INT32)(gtLconVal >> 32);
    }

    GenTreeLngCon(INT64 val) : GenTreeIntConCommon(GT_CNS_NATIVELONG, TYP_LONG)
    {
        SetLngValue(val);
    }
#if DEBUGGABLE_GENTREE
    GenTreeLngCon() : GenTreeIntConCommon()
    {
    }
#endif
};

inline INT64 GenTreeIntConCommon::LngValue() const
{
#ifndef TARGET_64BIT
    assert(gtOper == GT_CNS_LNG);
    return AsLngCon()->gtLconVal;
#else
    return IconValue();
#endif
}

inline void GenTreeIntConCommon::SetLngValue(INT64 val)
{
#ifndef TARGET_64BIT
    assert(gtOper == GT_CNS_LNG);
    AsLngCon()->gtLconVal = val;
#else
    // Compile time asserts that these two fields overlap and have the same offsets:  gtIconVal and gtLconVal
    C_ASSERT(offsetof(GenTreeLngCon, gtLconVal) == offsetof(GenTreeIntCon, gtIconVal));
    C_ASSERT(sizeof(AsLngCon()->gtLconVal) == sizeof(AsIntCon()->gtIconVal));

    SetIconValue(ssize_t(val));
#endif
}

inline ssize_t GenTreeIntConCommon::IconValue() const
{
    assert(gtOper == GT_CNS_INT); //  We should never see a GT_CNS_LNG for a 64-bit target!
    return AsIntCon()->gtIconVal;
}

inline void GenTreeIntConCommon::SetIconValue(ssize_t val)
{
    assert(gtOper == GT_CNS_INT); //  We should never see a GT_CNS_LNG for a 64-bit target!
    AsIntCon()->gtIconVal = val;
}

inline INT64 GenTreeIntConCommon::IntegralValue() const
{
#ifdef TARGET_64BIT
    return LngValue();
#else
    return gtOper == GT_CNS_LNG ? LngValue() : (INT64)IconValue();
#endif // TARGET_64BIT
}

inline void GenTreeIntConCommon::SetIntegralValue(int64_t value)
{
#ifdef TARGET_64BIT
    SetIconValue(value);
#else
    if (OperIs(GT_CNS_LNG))
    {
        SetLngValue(value);
    }
    else
    {
        assert(FitsIn<int32_t>(value));
        SetIconValue(static_cast<int32_t>(value));
    }
#endif // TARGET_64BIT
}

//------------------------------------------------------------------------
// SetValueTruncating: Set the value, truncating to TYP_INT if necessary.
//
// The function will truncate the supplied value to a 32 bit signed
// integer if the node's type is not TYP_LONG, otherwise setting it
// as-is. Note that this function intentionally does not check for
// small types (such nodes are created in lowering) for TP reasons.
//
// This function is intended to be used where its truncating behavior is
// desirable. One example is folding of ADD(CNS_INT, CNS_INT) performed in
// wider integers, which is typical when compiling on 64 bit hosts, as
// most arithmetic is done in ssize_t's aka int64_t's in that case, while
// the node itself can be of a narrower type.
//
// Arguments:
//    value - Value to set, truncating to TYP_INT if the node is not of TYP_LONG
//
// Notes:
//    This function is templated so that it works well with compiler warnings of
//    the form "Operation may overflow before being assigned to a wider type", in
//    case "value" is of type ssize_t, which is common.
//
template <typename T>
inline void GenTreeIntConCommon::SetValueTruncating(T value)
{
    static_assert_no_msg(
        (std::is_same<T, int32_t>::value || std::is_same<T, int64_t>::value || std::is_same<T, ssize_t>::value));

    if (TypeIs(TYP_LONG))
    {
        SetLngValue(value);
    }
    else
    {
        SetIconValue(static_cast<int32_t>(value));
    }
}

/* gtDblCon -- double  constant (GT_CNS_DBL) */

struct GenTreeDblCon : public GenTree
{
private:
    double gtDconVal;

public:
    double DconValue() const
    {
        return gtDconVal;
    }

    void SetDconValue(double value)
    {
        gtDconVal = FloatingPointUtils::normalize(value);
    }

    bool isBitwiseEqual(GenTreeDblCon* other)
    {
        unsigned __int64 bits      = *(unsigned __int64*)(&gtDconVal);
        unsigned __int64 otherBits = *(unsigned __int64*)(&(other->gtDconVal));
        return (bits == otherBits);
    }

    GenTreeDblCon(double val, var_types type = TYP_DOUBLE) : GenTree(GT_CNS_DBL, type)
    {
        assert(varTypeIsFloating(type));
        SetDconValue(val);
    }
#if DEBUGGABLE_GENTREE
    GenTreeDblCon() : GenTree()
    {
    }
#endif
};

/* gtStrCon -- string  constant (GT_CNS_STR) */

#define EMPTY_STRING_SCON (unsigned)-1

struct GenTreeStrCon : public GenTree
{
    unsigned              gtSconCPX;
    CORINFO_MODULE_HANDLE gtScpHnd;

    // Returns true if this GT_CNS_STR was imported for String.Empty field
    bool IsStringEmptyField()
    {
        return gtSconCPX == EMPTY_STRING_SCON && gtScpHnd == nullptr;
    }

    // Because this node can come from an inlined method we need to
    // have the scope handle, since it will become a helper call.
    GenTreeStrCon(unsigned sconCPX, CORINFO_MODULE_HANDLE mod DEBUGARG(bool largeNode = false))
        : GenTree(GT_CNS_STR, TYP_REF DEBUGARG(largeNode)), gtSconCPX(sconCPX), gtScpHnd(mod)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeStrCon() : GenTree()
    {
    }
#endif
};

// Encapsulates the SSA info carried by local nodes. Most local nodes have simple 1-to-1
// relationships with their SSA refs. However, defs of promoted structs can represent
// many SSA defs at the same time, and we need to efficiently encode that.
//
class SsaNumInfo final
{
    // This can be in one of four states:
    //  1. Single SSA name: > RESERVED_SSA_NUM (0).
    //  2. RESERVED_SSA_NUM (0)
    //  3. "Inline composite name": packed SSA numbers of field locals (each could be RESERVED):
    //     [byte 3]: [top bit][ssa num 3] (7 bits)
    //     [byte 2]: [ssa num 2] (8 bits)
    //     [byte 1]: [compact encoding bit][ssa num 1] (7 bits)
    //     [byte 0]: [ssa num 0] (8 bits)
    //     We expect this encoding to cover the 99%+ case of composite names: locals with more
    //     than 127 defs, maximum for this encoding, are rare, and the current limit on the count
    //     of promoted fields is 4.
    //  4. "Outlined composite name": index into the "composite SSA nums" table. The table itself
    //     will have the very simple format of N (the total number of fields / simple names) slots
    //     with full SSA numbers, starting at the encoded index. Notably, the table entries will
    //     include "empty" slots (for untracked fields), as we don't expect to use the table in
    //     the common case, and in the pathological cases, the space overhead should be mitigated
    //     by the cap on the number of tracked locals.
    //
    static const int BITS_PER_SIMPLE_NUM     = 8;
    static const int MAX_SIMPLE_NUM          = (1 << (BITS_PER_SIMPLE_NUM - 1)) - 1;
    static const int SIMPLE_NUM_MASK         = MAX_SIMPLE_NUM;
    static const int SIMPLE_NUM_COUNT        = (sizeof(int) * BITS_PER_BYTE) / BITS_PER_SIMPLE_NUM;
    static const int COMPOSITE_ENCODING_BIT  = 1 << 31;
    static const int OUTLINED_ENCODING_BIT   = 1 << 15;
    static const int OUTLINED_INDEX_LOW_MASK = OUTLINED_ENCODING_BIT - 1;
    static const int OUTLINED_INDEX_HIGH_MASK =
        ~(COMPOSITE_ENCODING_BIT | OUTLINED_ENCODING_BIT | OUTLINED_INDEX_LOW_MASK);
    static_assert_no_msg(SsaConfig::RESERVED_SSA_NUM == 0); // A lot in the encoding relies on this.

    int m_value;

    SsaNumInfo(int value) : m_value(value)
    {
    }

public:
    SsaNumInfo() : m_value(SsaConfig::RESERVED_SSA_NUM)
    {
    }

    bool IsSimple() const
    {
        return IsInvalid() || IsSsaNum(m_value);
    }

    bool IsComposite() const
    {
        return !IsSimple();
    }

    bool IsInvalid() const
    {
        return m_value == SsaConfig::RESERVED_SSA_NUM;
    }

    unsigned GetNum() const
    {
        assert(IsSimple());
        return m_value;
    }

    unsigned GetNum(Compiler* compiler, unsigned index) const;

    static SsaNumInfo Simple(unsigned ssaNum)
    {
        assert(IsSsaNum(ssaNum) || (ssaNum == SsaConfig::RESERVED_SSA_NUM));
        return SsaNumInfo(ssaNum);
    }

    static SsaNumInfo Composite(
        SsaNumInfo baseNum, Compiler* compiler, unsigned parentLclNum, unsigned index, unsigned ssaNum);

private:
    bool HasCompactFormat() const
    {
        assert(IsComposite());
        return (m_value & OUTLINED_ENCODING_BIT) == 0;
    }

    unsigned* GetOutlinedNumSlot(Compiler* compiler, unsigned index) const;

    static bool NumCanBeEncodedCompactly(unsigned index, unsigned ssaNum);

    static bool IsSsaNum(int value)
    {
        return value > SsaConfig::RESERVED_SSA_NUM;
    }
};

// Common supertype of [STORE_]LCL_VAR, [STORE_]LCL_FLD, PHI_ARG, LCL_VAR_ADDR, LCL_FLD_ADDR.
// This inherits from UnOp because lclvar stores are unary.
//
struct GenTreeLclVarCommon : public GenTreeUnOp
{
private:
    unsigned   m_lclNum; // The local number. An index into the Compiler::lvaTable array.
    SsaNumInfo m_ssaNum; // The SSA info.

public:
    GenTreeLclVarCommon(genTreeOps oper, var_types type, unsigned lclNum DEBUGARG(bool largeNode = false))
        : GenTreeUnOp(oper, type DEBUGARG(largeNode))
    {
        SetLclNum(lclNum);
    }

    GenTreeLclVarCommon(genTreeOps oper, var_types type, unsigned lclNum, GenTree* data)
        : GenTreeUnOp(oper, type, data DEBUGARG(/* largeNode */ false))
    {
        assert(OperIsLocalStore());
        SetLclNum(lclNum);
    }

    GenTree*& Data()
    {
        assert(OperIsLocalStore());
        return gtOp1;
    }

    unsigned GetLclNum() const
    {
        return m_lclNum;
    }

    void SetLclNum(unsigned lclNum)
    {
        m_lclNum = lclNum;
        m_ssaNum = SsaNumInfo();
    }

    uint16_t GetLclOffs() const;

    ClassLayout* GetLayout(Compiler* compiler) const;

    unsigned GetSsaNum() const
    {
        return m_ssaNum.IsSimple() ? m_ssaNum.GetNum() : SsaConfig::RESERVED_SSA_NUM;
    }

    unsigned GetSsaNum(Compiler* compiler, unsigned index) const
    {
        return m_ssaNum.IsComposite() ? m_ssaNum.GetNum(compiler, index) : SsaConfig::RESERVED_SSA_NUM;
    }

    void SetSsaNum(unsigned ssaNum)
    {
        m_ssaNum = SsaNumInfo::Simple(ssaNum);
    }

    void SetSsaNum(Compiler* compiler, unsigned index, unsigned ssaNum)
    {
        m_ssaNum = SsaNumInfo::Composite(m_ssaNum, compiler, GetLclNum(), index, ssaNum);
    }

    bool HasSsaName() const
    {
        return GetSsaNum() != SsaConfig::RESERVED_SSA_NUM;
    }

    bool HasCompositeSsaName() const
    {
        return m_ssaNum.IsComposite();
    }

    bool HasSsaIdentity() const
    {
        return !m_ssaNum.IsInvalid();
    }

#if DEBUGGABLE_GENTREE
    GenTreeLclVarCommon() : GenTreeUnOp()
    {
    }
#endif
};

//------------------------------------------------------------------------
// MultiRegSpillFlags
//
// GTF_SPILL or GTF_SPILLED flag on a multi-reg node indicates that one or
// more of its result regs are in that state.  The spill flags of each register
// are stored here. We only need 2 bits per returned register,
// so this is treated as a 2-bit array. No architecture needs more than 8 bits.
//
typedef unsigned char MultiRegSpillFlags;
static const unsigned PACKED_GTF_SPILL   = 1;
static const unsigned PACKED_GTF_SPILLED = 2;

//----------------------------------------------------------------------
// GetMultiRegSpillFlagsByIdx: get spill flag associated with the return register
// specified by its index.
//
// Arguments:
//    idx  -  Position or index of the return register
//
// Return Value:
//    Returns GTF_* flags associated with the register. Only GTF_SPILL and GTF_SPILLED are considered.
//
inline GenTreeFlags GetMultiRegSpillFlagsByIdx(MultiRegSpillFlags flags, unsigned idx)
{
    static_assert_no_msg(MAX_MULTIREG_COUNT * 2 <= sizeof(unsigned char) * BITS_PER_BYTE);
    assert(idx < MAX_MULTIREG_COUNT);

    unsigned     bits       = flags >> (idx * 2); // It doesn't matter that we possibly leave other high bits here.
    GenTreeFlags spillFlags = GTF_EMPTY;
    if (bits & PACKED_GTF_SPILL)
    {
        spillFlags |= GTF_SPILL;
    }
    if (bits & PACKED_GTF_SPILLED)
    {
        spillFlags |= GTF_SPILLED;
    }
    return spillFlags;
}

//----------------------------------------------------------------------
// SetMultiRegSpillFlagsByIdx: set spill flags for the register specified by its index.
//
// Arguments:
//    oldFlags   - The current value of the MultiRegSpillFlags for a node.
//    flagsToSet - GTF_* flags. Only GTF_SPILL and GTF_SPILLED are allowed.
//                 Note that these are the flags used on non-multireg nodes,
//                 and this method adds the appropriate flags to the
//                 incoming MultiRegSpillFlags and returns it.
//    idx    -     Position or index of the register
//
// Return Value:
//    The new value for the node's MultiRegSpillFlags.
//
inline MultiRegSpillFlags SetMultiRegSpillFlagsByIdx(MultiRegSpillFlags oldFlags, GenTreeFlags flagsToSet, unsigned idx)
{
    static_assert_no_msg(MAX_MULTIREG_COUNT * 2 <= sizeof(unsigned char) * BITS_PER_BYTE);
    assert(idx < MAX_MULTIREG_COUNT);

    MultiRegSpillFlags newFlags = oldFlags;
    unsigned           bits     = 0;
    if (flagsToSet & GTF_SPILL)
    {
        bits |= PACKED_GTF_SPILL;
    }
    if (flagsToSet & GTF_SPILLED)
    {
        bits |= PACKED_GTF_SPILLED;
    }

    const unsigned char packedFlags = PACKED_GTF_SPILL | PACKED_GTF_SPILLED;

    // Clear anything that was already there by masking out the bits before 'or'ing in what we want there.
    newFlags = (unsigned char)((newFlags & ~(packedFlags << (idx * 2))) | (bits << (idx * 2)));
    return newFlags;
}

// gtLclVar -- load/store/addr of local variable

struct GenTreeLclVar : public GenTreeLclVarCommon
{
private:
    regNumberSmall     gtOtherReg[MAX_MULTIREG_COUNT - 1];
    MultiRegSpillFlags gtSpillFlags;

public:
    INDEBUG(IL_OFFSET gtLclILoffs = BAD_IL_OFFSET;) // instr offset of ref (only for JIT dumps)

    // Multireg support
    bool IsMultiReg() const
    {
        return ((gtFlags & GTF_VAR_MULTIREG) != 0);
    }
    void ClearMultiReg()
    {
        gtFlags &= ~GTF_VAR_MULTIREG;
    }
    void SetMultiReg()
    {
        gtFlags |= GTF_VAR_MULTIREG;
        ClearOtherRegFlags();
    }

    regNumber GetRegNumByIdx(int regIndex) const
    {
        assert(regIndex < MAX_MULTIREG_COUNT);
        return (regIndex == 0) ? GetRegNum() : (regNumber)gtOtherReg[regIndex - 1];
    }

    void SetRegNumByIdx(regNumber reg, int regIndex)
    {
        assert(regIndex < MAX_MULTIREG_COUNT);
        if (regIndex == 0)
        {
            SetRegNum(reg);
        }
        else
        {
            gtOtherReg[regIndex - 1] = (regNumberSmall)reg;
        }
    }

    GenTreeFlags GetRegSpillFlagByIdx(unsigned idx) const
    {
        return GetMultiRegSpillFlagsByIdx(gtSpillFlags, idx);
    }

    void SetRegSpillFlagByIdx(GenTreeFlags flags, unsigned idx)
    {
        gtSpillFlags = SetMultiRegSpillFlagsByIdx(gtSpillFlags, flags, idx);
    }

    unsigned int GetFieldCount(Compiler* compiler) const;
    var_types GetFieldTypeByIndex(Compiler* compiler, unsigned idx);

    bool IsNeverNegative(Compiler* comp) const;

    //-------------------------------------------------------------------
    // clearOtherRegFlags: clear GTF_* flags associated with gtOtherRegs
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     None
    void ClearOtherRegFlags()
    {
        gtSpillFlags = 0;
    }

    //-------------------------------------------------------------------------
    // CopyOtherRegFlags: copy GTF_* flags associated with gtOtherRegs from
    // the given LclVar node.
    //
    // Arguments:
    //    fromCall  -  GenTreeLclVar node from which to copy
    //
    // Return Value:
    //    None
    //
    void CopyOtherRegFlags(GenTreeLclVar* from)
    {
        this->gtSpillFlags = from->gtSpillFlags;
    }

#ifdef DEBUG
    void ResetLclILoffs()
    {
        gtLclILoffs = BAD_IL_OFFSET;
    }
#endif

    GenTreeLclVar(genTreeOps oper,
                  var_types  type,
                  unsigned lclNum DEBUGARG(IL_OFFSET ilOffs = BAD_IL_OFFSET) DEBUGARG(bool largeNode = false))
        : GenTreeLclVarCommon(oper, type, lclNum DEBUGARG(largeNode)) DEBUGARG(gtLclILoffs(ilOffs))
    {
        assert(OperIsScalarLocal(oper));
    }

    GenTreeLclVar(var_types type, unsigned lclNum, GenTree* data)
        : GenTreeLclVarCommon(GT_STORE_LCL_VAR, type, lclNum, data)
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeLclVar() : GenTreeLclVarCommon()
    {
    }
#endif
};

// gtLclFld -- load/store/addr of local variable field

struct GenTreeLclFld : public GenTreeLclVarCommon
{
private:
    uint16_t     m_lclOffs; // offset into the variable to access
    ClassLayout* m_layout;  // The struct layout for this local field.

public:
    GenTreeLclFld(genTreeOps oper, var_types type, unsigned lclNum, unsigned lclOffs, ClassLayout* layout = nullptr)
        : GenTreeLclVarCommon(oper, type, lclNum), m_lclOffs(static_cast<uint16_t>(lclOffs))
    {
        assert(lclOffs <= UINT16_MAX);
        SetLayout(layout);
    }

    GenTreeLclFld(var_types type, unsigned lclNum, unsigned lclOffs, GenTree* data, ClassLayout* layout)
        : GenTreeLclVarCommon(GT_STORE_LCL_FLD, type, lclNum, data), m_lclOffs(static_cast<uint16_t>(lclOffs))
    {
        assert(lclOffs <= UINT16_MAX);
        SetLayout(layout);
    }

    uint16_t GetLclOffs() const
    {
        return m_lclOffs;
    }

    void SetLclOffs(unsigned lclOffs)
    {
        assert(lclOffs <= UINT16_MAX);
        m_lclOffs = static_cast<uint16_t>(lclOffs);
    }

    ClassLayout* GetLayout() const
    {
        assert(!TypeIs(TYP_STRUCT) || (m_layout != nullptr));
        return m_layout;
    }

    void SetLayout(ClassLayout* layout)
    {
        m_layout = layout;
    }

    unsigned GetSize() const;

#ifdef TARGET_ARM
    bool IsOffsetMisaligned() const;
#endif // TARGET_ARM

#if DEBUGGABLE_GENTREE
    GenTreeLclFld() : GenTreeLclVarCommon()
    {
    }
#endif
};

// GenTreeCast - conversion to a different type (GT_CAST).
//
// This node represents all "conv[.ovf].{type}[.un]" IL opcodes.
//
// There are four semantically significant values that determine what it does:
//
//  1) "genActualType(CastOp())"              - the type being cast from.
//  2) "gtCastType"                           - the type being cast to.
//  3) "IsUnsigned" (the "GTF_UNSIGNED" flag) - whether the cast is "unsigned".
//  4) "gtOverflow" (the "GTF_OVERFLOW" flag) - whether the cast is checked.
//
// Different "kinds" of casts use these values differently; not all are always
// meaningful or legal:
//
//  1) For casts from FP types, "IsUnsigned" will always be "false".
//  2) Checked casts use "IsUnsigned" to represent the fact the type being cast
//     from is unsigned. The target type's signedness is similarly significant.
//  3) For unchecked casts, "IsUnsigned" is significant for "int -> long", where
//     it decides whether the cast sign- or zero-extends its source, and "integer
//     -> FP" cases. For all other unchecked casts, "IsUnsigned" is meaningless.
//  4) For unchecked casts, signedness of the target type is only meaningful if
//     the cast is to an FP or small type. In the latter case (and everywhere
//     else in IR) it decides whether the value will be sign- or zero-extended.
//
// For additional context on "GT_CAST"'s semantics, see "IntegralRange::ForCast"
// methods and "GenIntCastDesc"'s constructor.
//
struct GenTreeCast : public GenTreeOp
{
    GenTree*& CastOp()
    {
        return gtOp1;
    }
    var_types gtCastType;

    GenTreeCast(var_types type, GenTree* op, bool fromUnsigned, var_types castType DEBUGARG(bool largeNode = false))
        : GenTreeOp(GT_CAST, type, op, nullptr DEBUGARG(largeNode)), gtCastType(castType)
    {
        // We do not allow casts from floating point types to be treated as from
        // unsigned to avoid bugs related to wrong GTF_UNSIGNED in case the
        // CastOp's type changes.
        assert(!varTypeIsFloating(op) || !fromUnsigned);

        gtFlags |= fromUnsigned ? GTF_UNSIGNED : GTF_EMPTY;
    }
#if DEBUGGABLE_GENTREE
    GenTreeCast() : GenTreeOp()
    {
    }
#endif

    bool IsZeroExtending()
    {
        assert(varTypeIsIntegral(CastOp()) && varTypeIsIntegral(CastToType()));

        if (varTypeIsSmall(CastToType()))
        {
            return varTypeIsUnsigned(CastToType());
        }
        if (TypeIs(TYP_LONG) && genActualTypeIsInt(CastOp()))
        {
            return IsUnsigned();
        }

        return false;
    }
};

// GT_BOX nodes are place markers for boxed values.  The "real" tree
// for most purposes is in gtBoxOp.
struct GenTreeBox : public GenTreeUnOp
{
    // An expanded helper call to implement the "box" if we don't get
    // rid of it any other way.  Must be in same position as op1.

    GenTree*& BoxOp()
    {
        return gtOp1;
    }
    // This is the statement that contains the assignment tree when the node is an inlined GT_BOX on a value
    // type
    Statement* gtDefStmtWhenInlinedBoxValue;
    // And this is the statement that copies from the value being boxed to the box payload
    Statement* gtCopyStmtWhenInlinedBoxValue;

    GenTreeBox(var_types  type,
               GenTree*   boxOp,
               Statement* defStmtWhenInlinedBoxValue,
               Statement* copyStmtWhenInlinedBoxValue)
        : GenTreeUnOp(GT_BOX, type, boxOp)
        , gtDefStmtWhenInlinedBoxValue(defStmtWhenInlinedBoxValue)
        , gtCopyStmtWhenInlinedBoxValue(copyStmtWhenInlinedBoxValue)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeBox() : GenTreeUnOp()
    {
    }
#endif

    bool WasCloned()
    {
        return (gtFlags & GTF_BOX_CLONED) != 0;
    }

    void SetCloned()
    {
        gtFlags |= GTF_BOX_CLONED;
    }
};

// GenTreeFieldAddr -- data member address (GT_FIELD_ADDR)
struct GenTreeFieldAddr : public GenTreeUnOp
{
    CORINFO_FIELD_HANDLE gtFldHnd;
    DWORD                gtFldOffset;
    bool                 gtFldMayOverlap : 1;

private:
    bool gtFldIsSpanLength : 1;

public:
#ifdef FEATURE_READYTORUN
    CORINFO_CONST_LOOKUP gtFieldLookup;
#endif

    GenTreeFieldAddr(var_types type, GenTree* obj, CORINFO_FIELD_HANDLE fldHnd, DWORD offs)
        : GenTreeUnOp(GT_FIELD_ADDR, type, obj)
        , gtFldHnd(fldHnd)
        , gtFldOffset(offs)
        , gtFldMayOverlap(false)
        , gtFldIsSpanLength(false)
    {
#ifdef FEATURE_READYTORUN
        gtFieldLookup.addr = nullptr;
#endif
    }

#if DEBUGGABLE_GENTREE
    GenTreeFieldAddr() : GenTreeUnOp()
    {
    }
#endif

    // The object this field belongs to. Will be "nullptr" for static fields.
    // Note that this is an address, i. e. for struct fields it will be ADDR(STRUCT).
    GenTree* GetFldObj() const
    {
        return gtOp1;
    }

    bool IsSpanLength() const
    {
        // This is limited to span length today rather than a more general "IsNeverNegative"
        // to help avoid confusion around propagating the value to promoted lcl vars.
        //
        // Extending this support more in the future will require additional work and
        // considerations to help ensure it is correctly used since people may want
        // or intend to use this as more of a "point in time" feature like GTF_IND_NONNULL
        return gtFldIsSpanLength;
    }

    void SetIsSpanLength(bool value)
    {
        gtFldIsSpanLength = value;
    }

    bool IsInstance() const
    {
        return GetFldObj() != nullptr;
    }

    bool IsStatic() const
    {
        return !IsInstance();
    }

    bool IsTlsStatic() const
    {
        assert(((gtFlags & GTF_FLD_TLS) == 0) || IsStatic());
        return (gtFlags & GTF_FLD_TLS) != 0;
    }

    bool IsOffsetKnown() const
    {
#ifdef FEATURE_READYTORUN
        return gtFieldLookup.addr == nullptr;
#endif // FEATURE_READYTORUN
        return true;
    }
};

// There was quite a bit of confusion in the code base about which of gtOp1 and gtOp2 was the
// 'then' and 'else' clause of a colon node.  Adding these accessors, while not enforcing anything,
// at least *allows* the programmer to be obviously correct.
// However, these conventions seem backward.
// TODO-Cleanup: If we could get these accessors used everywhere, then we could switch them.
struct GenTreeColon : public GenTreeOp
{
    GenTree*& ThenNode()
    {
        return gtOp2;
    }
    GenTree*& ElseNode()
    {
        return gtOp1;
    }

#if DEBUGGABLE_GENTREE
    GenTreeColon() : GenTreeOp()
    {
    }
#endif

    GenTreeColon(var_types typ, GenTree* thenNode, GenTree* elseNode) : GenTreeOp(GT_COLON, typ, elseNode, thenNode)
    {
    }
};

// GenTreeConditional -- Conditionally do an operation

struct GenTreeConditional : public GenTreeOp
{
    GenTree* gtCond;

    GenTreeConditional(
        genTreeOps oper, var_types type, GenTree* cond, GenTree* op1, GenTree* op2 DEBUGARG(bool largeNode = false))
        : GenTreeOp(oper, type, op1, op2 DEBUGARG(largeNode)), gtCond(cond)
    {
        assert(cond != nullptr);
    }

#if DEBUGGABLE_GENTREE
    GenTreeConditional() : GenTreeOp()
    {
    }
#endif
};

// gtCall   -- method call      (GT_CALL)
enum class InlineObservation;

//------------------------------------------------------------------------
// GenTreeCallFlags: a bitmask of flags for GenTreeCall stored in gtCallMoreFlags.
//
// clang-format off
enum GenTreeCallFlags : unsigned int
{
    GTF_CALL_M_EMPTY                   = 0,

    GTF_CALL_M_RETBUFFARG              = 0x00000001, // the ABI dictates that this call needs a ret buffer
    GTF_CALL_M_RETBUFFARG_LCLOPT       = 0x00000002, // Does this call have a local ret buffer that we are optimizing?
    GTF_CALL_M_DELEGATE_INV            = 0x00000004, // call to Delegate.Invoke
    GTF_CALL_M_NOGCCHECK               = 0x00000008, // not a call for computing full interruptability and therefore no GC check is required.
    GTF_CALL_M_SPECIAL_INTRINSIC       = 0x00000010, // function that could be optimized as an intrinsic
                                                     // in special cases. Used to optimize fast way out in morphing
    GTF_CALL_M_VIRTSTUB_REL_INDIRECT   = 0x00000020, // the virtstub is indirected through a relative address (only for GTF_CALL_VIRT_STUB)
    GTF_CALL_M_NONVIRT_SAME_THIS       = 0x00000020, // callee "this" pointer is equal to caller this pointer (only for GTF_CALL_NONVIRT)
    GTF_CALL_M_FRAME_VAR_DEATH         = 0x00000040, // the compLvFrameListRoot variable dies here (last use)

    GTF_CALL_M_TAILCALL                = 0x00000080, // the call is a tailcall
    GTF_CALL_M_EXPLICIT_TAILCALL       = 0x00000100, // the call is "tail" prefixed and importer has performed tail call checks
    GTF_CALL_M_TAILCALL_VIA_JIT_HELPER = 0x00000200, // call is a tail call dispatched via tail call JIT helper.
    GTF_CALL_M_IMPLICIT_TAILCALL       = 0x00000400, // call is an opportunistic tail call and importer has performed tail call checks
    GTF_CALL_M_TAILCALL_TO_LOOP        = 0x00000800, // call is a fast recursive tail call that can be converted into a loop

    GTF_CALL_M_PINVOKE                 = 0x00001000, // call is a pinvoke.  This mirrors VM flag CORINFO_FLG_PINVOKE.
                                                     // A call marked as Pinvoke is not necessarily a GT_CALL_UNMANAGED. For e.g.
                                                     // an IL Stub dynamically generated for a PInvoke declaration is flagged as
                                                     // a Pinvoke but not as an unmanaged call. See impCheckForPInvokeCall() to
                                                     // know when these flags are set.

    GTF_CALL_M_DOES_NOT_RETURN         = 0x00002000, // call does not return
    GTF_CALL_M_WRAPPER_DELEGATE_INV    = 0x00004000, // call is in wrapper delegate
    GTF_CALL_M_FAT_POINTER_CHECK       = 0x00008000, // NativeAOT managed calli needs transformation, that checks
                                                     // special bit in calli address. If it is set, then it is necessary
                                                     // to restore real function address and load hidden argument
                                                     // as the first argument for calli. It is NativeAOT replacement for instantiating
                                                     // stubs, because executable code cannot be generated at runtime.
    GTF_CALL_M_HELPER_SPECIAL_DCE      = 0x00010000, // this helper call can be removed if it is part of a comma and
                                                     // the comma result is unused.
    GTF_CALL_M_GUARDED_DEVIRT          = 0x00020000, // this call is a candidate for guarded devirtualization
    GTF_CALL_M_GUARDED_DEVIRT_EXACT    = 0x00040000, // this call is a candidate for guarded devirtualization without a fallback
    GTF_CALL_M_GUARDED_DEVIRT_CHAIN    = 0x00080000, // this call is a candidate for chained guarded devirtualization
    GTF_CALL_M_ALLOC_SIDE_EFFECTS      = 0x00100000, // this is a call to an allocator with side effects
    GTF_CALL_M_SUPPRESS_GC_TRANSITION  = 0x00200000, // suppress the GC transition (i.e. during a pinvoke) but a separate GC safe point is required.
    GTF_CALL_M_EXPANDED_EARLY          = 0x00800000, // the Virtual Call target address is expanded and placed in gtControlExpr in Morph rather than in Lower
    GTF_CALL_M_HAS_LATE_DEVIRT_INFO    = 0x01000000, // this call has late devirtualzation info
    GTF_CALL_M_LDVIRTFTN_INTERFACE     = 0x02000000, // ldvirtftn on an interface type
    GTF_CALL_M_CAST_CAN_BE_EXPANDED    = 0x04000000, // this cast (helper call) can be expanded if it's profitable. To be removed.
    GTF_CALL_M_CAST_OBJ_NONNULL        = 0x08000000, // if we expand this specific cast we don't need to check the input object for null
                                                     // NOTE: if needed, this flag can be removed, and we can introduce new _NONNUL cast helpers
};

inline constexpr GenTreeCallFlags operator ~(GenTreeCallFlags a)
{
    return (GenTreeCallFlags)(~(unsigned int)a);
}

inline constexpr GenTreeCallFlags operator |(GenTreeCallFlags a, GenTreeCallFlags b)
{
    return (GenTreeCallFlags)((unsigned int)a | (unsigned int)b);
}

inline constexpr GenTreeCallFlags operator &(GenTreeCallFlags a, GenTreeCallFlags b)
{
    return (GenTreeCallFlags)((unsigned int)a & (unsigned int)b);
}

inline GenTreeCallFlags& operator |=(GenTreeCallFlags& a, GenTreeCallFlags b)
{
    return a = (GenTreeCallFlags)((unsigned int)a | (unsigned int)b);
}

inline GenTreeCallFlags& operator &=(GenTreeCallFlags& a, GenTreeCallFlags b)
{
    return a = (GenTreeCallFlags)((unsigned int)a & (unsigned int)b);
}

#ifdef DEBUG
enum GenTreeCallDebugFlags : unsigned int
{
    GTF_CALL_MD_EMPTY                   = 0,
    GTF_CALL_MD_STRESS_TAILCALL         = 0x00000001, // the call is NOT "tail" prefixed but GTF_CALL_M_EXPLICIT_TAILCALL was added because of tail call stress mode
    GTF_CALL_MD_DEVIRTUALIZED           = 0x00000002, // this call was devirtualized
    GTF_CALL_MD_UNBOXED                 = 0x00000004, // this call was optimized to use the unboxed entry point
    GTF_CALL_MD_GUARDED                 = 0x00000008, // this call was transformed by guarded devirtualization
    GTF_CALL_MD_RUNTIME_LOOKUP_EXPANDED = 0x00000010, // this runtime lookup helper is expanded
};

inline constexpr GenTreeCallDebugFlags operator ~(GenTreeCallDebugFlags a)
{
    return (GenTreeCallDebugFlags)(~(unsigned int)a);
}

inline constexpr GenTreeCallDebugFlags operator |(GenTreeCallDebugFlags a, GenTreeCallDebugFlags b)
{
    return (GenTreeCallDebugFlags)((unsigned int)a | (unsigned int)b);
}

inline constexpr GenTreeCallDebugFlags operator &(GenTreeCallDebugFlags a, GenTreeCallDebugFlags b)
{
    return (GenTreeCallDebugFlags)((unsigned int)a & (unsigned int)b);
}

inline GenTreeCallDebugFlags& operator |=(GenTreeCallDebugFlags& a, GenTreeCallDebugFlags b)
{
    return a = (GenTreeCallDebugFlags)((unsigned int)a | (unsigned int)b);
}

inline GenTreeCallDebugFlags& operator &=(GenTreeCallDebugFlags& a, GenTreeCallDebugFlags b)
{
    return a = (GenTreeCallDebugFlags)((unsigned int)a & (unsigned int)b);
}
#endif

// clang-format on

// Return type descriptor of a GT_CALL node.
// x64 Unix, Arm64, Arm32 and x86 allow a value to be returned in multiple
// registers. For such calls this struct provides the following info
// on their return type
//    - type of value returned in each return register
//    - ABI return register numbers in which the value is returned
//    - count of return registers in which the value is returned
//
// TODO-ARM: Update this to meet the needs of Arm64 and Arm32
//
// TODO-AllArch: Right now it is used for describing multi-reg returned types.
// Eventually we would want to use it for describing even single-reg
// returned types (e.g. structs returned in single register x64/arm).
// This would allow us not to lie or normalize single struct return
// values in importer/morph.
struct ReturnTypeDesc
{
private:
    var_types m_regType[MAX_RET_REG_COUNT];

#ifdef DEBUG
    bool m_inited;
#endif

public:
    ReturnTypeDesc()
    {
        Reset();
    }

    // Initialize the Return Type Descriptor for a method that returns a struct type
    void InitializeStructReturnType(Compiler* comp, CORINFO_CLASS_HANDLE retClsHnd, CorInfoCallConvExtension callConv);

    // Initialize the Return Type Descriptor for a method that returns a TYP_LONG
    // Only needed for X86 and arm32.
    void InitializeLongReturnType();

    // Initialize the Return Type Descriptor.
    void InitializeReturnType(Compiler*                comp,
                              var_types                type,
                              CORINFO_CLASS_HANDLE     retClsHnd,
                              CorInfoCallConvExtension callConv);

    // Reset type descriptor to defaults
    void Reset()
    {
        for (unsigned i = 0; i < MAX_RET_REG_COUNT; ++i)
        {
            m_regType[i] = TYP_UNKNOWN;
        }
#ifdef DEBUG
        m_inited = false;
#endif
    }

#ifdef DEBUG
    // NOTE: we only use this function when writing out IR dumps. These dumps may take place before the ReturnTypeDesc
    // has been initialized.
    unsigned TryGetReturnRegCount() const
    {
        return m_inited ? GetReturnRegCount() : 0;
    }
#endif // DEBUG

    //--------------------------------------------------------------------------------------------
    // GetReturnRegCount:  Get the count of return registers in which the return value is returned.
    //
    // Arguments:
    //    None
    //
    // Return Value:
    //   Count of return registers.
    //   Returns 0 if the return type is not returned in registers.
    //
    unsigned GetReturnRegCount() const
    {
        assert(m_inited);

        int regCount = 0;
        for (unsigned i = 0; i < MAX_RET_REG_COUNT; ++i)
        {
            if (m_regType[i] == TYP_UNKNOWN)
            {
                break;
            }
            // otherwise
            regCount++;
        }

#ifdef DEBUG
        // Any remaining elements in m_regTypes[] should also be TYP_UNKNOWN
        for (unsigned i = regCount + 1; i < MAX_RET_REG_COUNT; ++i)
        {
            assert(m_regType[i] == TYP_UNKNOWN);
        }
#endif

        return regCount;
    }

    //-----------------------------------------------------------------------
    // IsMultiRegRetType: check whether the type is returned in multiple
    // return registers.
    //
    // Arguments:
    //    None
    //
    // Return Value:
    //    Returns true if the type is returned in multiple return registers.
    //    False otherwise.
    // Note that we only have to examine the first two values to determine this
    //
    bool IsMultiRegRetType() const
    {
        if (MAX_RET_REG_COUNT < 2)
        {
            return false;
        }
        else
        {
            assert(m_inited);
            return ((m_regType[0] != TYP_UNKNOWN) && (m_regType[1] != TYP_UNKNOWN));
        }
    }

    //--------------------------------------------------------------------------
    // GetReturnRegType:  Get var_type of the return register specified by index.
    //
    // Arguments:
    //    index - Index of the return register.
    //            First return register will have an index 0 and so on.
    //
    // Return Value:
    //    var_type of the return register specified by its index.
    //    asserts if the index does not have a valid register return type.

    var_types GetReturnRegType(unsigned index) const
    {
        var_types result = m_regType[index];
        assert(result != TYP_UNKNOWN);

        return result;
    }

    // Get i'th ABI return register
    regNumber GetABIReturnReg(unsigned idx) const;

    // Get reg mask of ABI return registers
    regMaskTP GetABIReturnRegs() const;
};

class TailCallSiteInfo
{
    bool                   m_isCallvirt : 1;
    bool                   m_isCalli : 1;
    CORINFO_SIG_INFO       m_sig;
    CORINFO_RESOLVED_TOKEN m_token;

public:
    // Is the tailcall a callvirt instruction?
    bool IsCallvirt()
    {
        return m_isCallvirt;
    }

    // Is the tailcall a calli instruction?
    bool IsCalli()
    {
        return m_isCalli;
    }

    // Get the token of the callee
    CORINFO_RESOLVED_TOKEN* GetToken()
    {
        assert(!IsCalli());
        return &m_token;
    }

    // Get the signature of the callee
    CORINFO_SIG_INFO* GetSig()
    {
        return &m_sig;
    }

    // Mark the tailcall as a calli with the given signature
    void SetCalli(CORINFO_SIG_INFO* sig)
    {
        m_isCallvirt = false;
        m_isCalli    = true;
        m_sig        = *sig;
    }

    // Mark the tailcall as a callvirt with the given signature and token
    void SetCallvirt(CORINFO_SIG_INFO* sig, CORINFO_RESOLVED_TOKEN* token)
    {
        m_isCallvirt = true;
        m_isCalli    = false;
        m_sig        = *sig;
        m_token      = *token;
    }

    // Mark the tailcall as a call with the given signature and token
    void SetCall(CORINFO_SIG_INFO* sig, CORINFO_RESOLVED_TOKEN* token)
    {
        m_isCallvirt = false;
        m_isCalli    = false;
        m_sig        = *sig;
        m_token      = *token;
    }
};

enum class CFGCallKind
{
    ValidateAndCall,
    Dispatch,
};

class CallArgs;

enum class WellKnownArg
{
    None,
    ThisPointer,
    VarArgsCookie,
    InstParam,
    RetBuffer,
    PInvokeFrame,
    SecretStubParam,
    WrapperDelegateCell,
    ShiftLow,
    ShiftHigh,
    VirtualStubCell,
    PInvokeCookie,
    PInvokeTarget,
    R2RIndirectionCell,
    ValidateIndirectCallTarget,
    DispatchIndirectCallTarget,
};

#ifdef DEBUG
const char* getWellKnownArgName(WellKnownArg arg);
#endif

struct CallArgABIInformation
{
    CallArgABIInformation()
        : NumRegs(0)
        , ByteOffset(0)
        , ByteSize(0)
        , ByteAlignment(0)
#ifdef UNIX_AMD64_ABI
        , StructIntRegs(0)
        , StructFloatRegs(0)
#endif
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        , StructFloatFieldType()
#endif
        , ArgType(TYP_UNDEF)
        , IsBackFilled(false)
        , PassedByRef(false)
#if FEATURE_ARG_SPLIT
        , m_isSplit(false)
#endif
#ifdef FEATURE_HFA_FIELDS_PRESENT
        , m_hfaElemKind(CORINFO_HFA_ELEM_NONE)
#endif
    {
        for (size_t i = 0; i < MAX_ARG_REG_COUNT; i++)
        {
            RegNums[i] = REG_NA;
        }
    }

private:
    // The registers to use when passing this argument, set to REG_STK for
    // arguments passed on the stack
    regNumberSmall RegNums[MAX_ARG_REG_COUNT];

public:
    // Count of number of registers that this argument uses. Note that on ARM,
    // if we have a double hfa, this reflects the number of DOUBLE registers.
    unsigned NumRegs;
    unsigned ByteOffset;
    unsigned ByteSize;
    unsigned ByteAlignment;
#if defined(UNIX_AMD64_ABI)
    // Unix amd64 will split floating point types and integer types in structs
    // between floating point and general purpose registers. Keep track of that
    // information so we do not need to recompute it later.
    unsigned                                            StructIntRegs;
    unsigned                                            StructFloatRegs;
    SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR StructDesc;
#endif // UNIX_AMD64_ABI
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    // For LoongArch64's ABI, the struct which has float field(s) and no more than two fields
    // may be passed by float register(s).
    // e.g  `struct {int a; float b;}` passed by an integer register and a float register.
    var_types StructFloatFieldType[2];
#endif
    // The type used to pass this argument. This is generally the original
    // argument type, but when a struct is passed as a scalar type, this is
    // that type. Note that if a struct is passed by reference, this will still
    // be the struct type.
    var_types ArgType : 5;
    // True when the argument fills a register slot skipped due to alignment
    // requirements of previous arguments.
    bool IsBackFilled : 1;
    // True iff the argument is passed by reference.
    bool PassedByRef : 1;

private:
#if FEATURE_ARG_SPLIT
    // True when this argument is split between the registers and OutArg area
    bool m_isSplit : 1;
#endif

#ifdef FEATURE_HFA_FIELDS_PRESENT
    // What kind of an HFA this is (CORINFO_HFA_ELEM_NONE if it is not an HFA).
    CorInfoHFAElemType m_hfaElemKind : 3;
#endif

public:
    CorInfoHFAElemType GetHfaElemKind() const
    {
#ifdef FEATURE_HFA_FIELDS_PRESENT
        return m_hfaElemKind;
#else
        NOWAY_MSG("GetHfaElemKind");
        return CORINFO_HFA_ELEM_NONE;
#endif
    }

    void SetHfaElemKind(CorInfoHFAElemType elemKind)
    {
#ifdef FEATURE_HFA_FIELDS_PRESENT
        m_hfaElemKind = elemKind;
#else
        NOWAY_MSG("SetHfaElemKind");
#endif
    }

    bool      IsHfaArg() const;
    bool      IsHfaRegArg() const;
    var_types GetHfaType() const;
    void SetHfaType(var_types type, unsigned hfaSlots);

    regNumber GetRegNum() const
    {
        return (regNumber)RegNums[0];
    }

    regNumber GetOtherRegNum() const
    {
        return (regNumber)RegNums[1];
    }
    regNumber GetRegNum(unsigned int i)
    {
        assert(i < MAX_ARG_REG_COUNT);
        return (regNumber)RegNums[i];
    }
    void SetRegNum(unsigned int i, regNumber regNum)
    {
        assert(i < MAX_ARG_REG_COUNT);
        RegNums[i] = (regNumberSmall)regNum;
    }

    bool IsSplit() const
    {
#if FEATURE_ARG_SPLIT
        return compFeatureArgSplit() && m_isSplit;
#else // FEATURE_ARG_SPLIT
        return false;
#endif
    }
    void SetSplit(bool value)
    {
#if FEATURE_ARG_SPLIT
        m_isSplit = value;
#endif
    }

    bool IsPassedInRegisters() const
    {
        return !IsSplit() && (NumRegs != 0);
    }

    bool IsPassedInFloatRegisters() const
    {
#ifdef TARGET_X86
        return false;
#else
        return isValidFloatArgReg(GetRegNum());
#endif
    }

    bool IsMismatchedArgType() const
    {
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        return isValidIntArgReg(GetRegNum()) && varTypeUsesFloatReg(ArgType);
#else
        return false;
#endif // TARGET_LOONGARCH64 || TARGET_RISCV64
    }

    void SetByteSize(unsigned byteSize, unsigned byteAlignment, bool isStruct, bool isFloatHfa);

    // Get the number of bytes that this argument is occupying on the stack,
    // including padding up to the target pointer size for platforms
    // where a stack argument can't take less.
    unsigned GetStackByteSize() const;

    // Set the register numbers for a multireg argument.
    // There's nothing to do on x64/Ux because the structDesc has already been used to set the
    // register numbers.
    void SetMultiRegNums();

    // Return number of stack slots that this argument is taking.
    // This value is not meaningful on Apple arm64 where multiple arguments can
    // be passed in the same stack slot.
    unsigned GetStackSlotsNumber() const
    {
        return roundUp(GetStackByteSize(), TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE;
    }
};

struct NewCallArg
{
    // The node being passed.
    GenTree* Node = nullptr;
    // The signature type of the node.
    var_types SignatureType = TYP_UNDEF;
    // The class handle if SignatureType == TYP_STRUCT.
    CORINFO_CLASS_HANDLE SignatureClsHnd = NO_CLASS_HANDLE;
    // The type of well known arg
    WellKnownArg WellKnownArg = ::WellKnownArg::None;

    NewCallArg WellKnown(::WellKnownArg type) const
    {
        NewCallArg copy   = *this;
        copy.WellKnownArg = type;
        return copy;
    }

    static NewCallArg Struct(GenTree* node, var_types type, CORINFO_CLASS_HANDLE clsHnd)
    {
        assert(varTypeIsStruct(node) && varTypeIsStruct(type));
        NewCallArg arg;
        arg.Node            = node;
        arg.SignatureType   = type;
        arg.SignatureClsHnd = clsHnd;
        arg.ValidateTypes();
        return arg;
    }

    static NewCallArg Primitive(GenTree* node, var_types type = TYP_UNDEF)
    {
        assert(!varTypeIsStruct(node) && !varTypeIsStruct(type));
        NewCallArg arg;
        arg.Node          = node;
        arg.SignatureType = type == TYP_UNDEF ? node->TypeGet() : type;
        arg.ValidateTypes();
        return arg;
    }

#ifdef DEBUG
    void ValidateTypes();
#else
    void ValidateTypes()
    {
    }
#endif
};

class CallArg
{
    friend class CallArgs;

    GenTree* m_earlyNode;
    GenTree* m_lateNode;
    CallArg* m_next;
    CallArg* m_lateNext;

    // The class handle for the signature type (when varTypeIsStruct(SignatureType)).
    CORINFO_CLASS_HANDLE m_signatureClsHnd;
    // The LclVar number if we had to force evaluation of this arg.
    unsigned m_tmpNum;
    // The type of the argument in the signature.
    var_types m_signatureType : 5;
    // The type of well-known argument this is.
    WellKnownArg m_wellKnownArg : 5;
    // True when we force this argument's evaluation into a temp LclVar.
    bool m_needTmp : 1;
    // True when we must replace this argument with a placeholder node.
    bool m_needPlace : 1;
    // True when we setup a temp LclVar for this argument.
    bool m_isTmp : 1;
    // True when we have decided the evaluation order for this argument in LateArgs
    bool m_processed : 1;

private:
    CallArg()
        : m_earlyNode(nullptr)
        , m_lateNode(nullptr)
        , m_next(nullptr)
        , m_lateNext(nullptr)
        , m_signatureClsHnd(NO_CLASS_HANDLE)
        , m_tmpNum(BAD_VAR_NUM)
        , m_signatureType(TYP_UNDEF)
        , m_wellKnownArg(WellKnownArg::None)
        , m_needTmp(false)
        , m_needPlace(false)
        , m_isTmp(false)
        , m_processed(false)
    {
    }

public:
    CallArgABIInformation AbiInfo;

    CallArg(const NewCallArg& arg) : CallArg()
    {
        m_earlyNode       = arg.Node;
        m_wellKnownArg    = arg.WellKnownArg;
        m_signatureType   = arg.SignatureType;
        m_signatureClsHnd = arg.SignatureClsHnd;
    }

    CallArg(const CallArg&) = delete;
    CallArg& operator=(CallArg&) = delete;

    // clang-format off
    GenTree*& EarlyNodeRef() { return m_earlyNode; }
    GenTree* GetEarlyNode() { return m_earlyNode; }
    void SetEarlyNode(GenTree* node) { m_earlyNode = node; }
    GenTree*& LateNodeRef() { return m_lateNode; }
    GenTree* GetLateNode() { return m_lateNode; }
    void SetLateNode(GenTree* lateNode) { m_lateNode = lateNode; }
    CallArg*& NextRef() { return m_next; }
    CallArg* GetNext() { return m_next; }
    void SetNext(CallArg* next) { m_next = next; }
    CallArg*& LateNextRef() { return m_lateNext; }
    CallArg* GetLateNext() { return m_lateNext; }
    void SetLateNext(CallArg* lateNext) { m_lateNext = lateNext; }
    CORINFO_CLASS_HANDLE GetSignatureClassHandle() { return m_signatureClsHnd; }
    var_types GetSignatureType() { return m_signatureType; }
    WellKnownArg GetWellKnownArg() { return m_wellKnownArg; }
    bool IsTemp() { return m_isTmp; }
    // clang-format on

    // Get the real argument node, i.e. not a setup or placeholder node.
    // This is the same as GetEarlyNode() until morph.
    // After lowering, this is a PUTARG_* node.
    GenTree* GetNode()
    {
        return m_lateNode == nullptr ? m_earlyNode : m_lateNode;
    }

    bool IsArgAddedLate() const;

    bool IsUserArg() const;

#ifdef DEBUG
    void Dump(Compiler* comp);
    // Check that the value of 'AbiInfo.IsStruct' is consistent.
    // A struct arg must be one of the following:
    // - A node of struct type,
    // - A GT_FIELD_LIST, or
    // - A node of a scalar type, passed in a single register or slot
    //   (or two slots in the case of a struct pass on the stack as TYP_DOUBLE).
    //
    void CheckIsStruct();
#endif
};

class CallArgs
{
    CallArg* m_head;
    CallArg* m_lateHead;

    unsigned m_nextStackByteOffset;
#ifdef UNIX_X86_ABI
    // Number of stack bytes pushed before we start pushing these arguments.
    unsigned m_stkSizeBytes;
    // Stack alignment in bytes required before arguments are pushed for this
    // call. Computed dynamically during codegen, based on m_stkSizeBytes and the
    // current stack level (genStackLevel) when the first stack adjustment is
    // made for this call.
    unsigned m_padStkAlign;
#endif
    bool m_hasThisPointer : 1;
    bool m_hasRetBuffer : 1;
    bool m_isVarArgs : 1;
    bool m_abiInformationDetermined : 1;
    // True if we have one or more register arguments.
    bool m_hasRegArgs : 1;
    // True if we have one or more stack arguments.
    bool m_hasStackArgs : 1;
    bool m_argsComplete : 1;
    // One or more arguments must be copied to a temp by EvalArgsToTemps.
    bool m_needsTemps : 1;
#ifdef UNIX_X86_ABI
    // Updateable flag, set to 'true' after we've done any required alignment.
    bool m_alignmentDone : 1;
#endif

    void AddedWellKnownArg(WellKnownArg arg);
    void RemovedWellKnownArg(WellKnownArg arg);
    regNumber GetCustomRegister(Compiler* comp, CorInfoCallConvExtension cc, WellKnownArg arg);
    void SplitArg(CallArg* arg, unsigned numRegs, unsigned numSlots);
    void SortArgs(Compiler* comp, GenTreeCall* call, CallArg** sortedArgs);

public:
    CallArgs();
    CallArgs(const CallArgs&) = delete;
    CallArgs& operator=(CallArgs&) = delete;

    CallArg* FindByNode(GenTree* node);
    CallArg* FindWellKnownArg(WellKnownArg arg);
    CallArg* GetThisArg();
    CallArg* GetRetBufferArg();
    CallArg* GetArgByIndex(unsigned index);
    CallArg* GetUserArgByIndex(unsigned index);
    unsigned GetIndex(CallArg* arg);

    bool IsEmpty() const
    {
        return m_head == nullptr;
    }

    // Reverse the args from [index..index + count) in place.
    void Reverse(unsigned index, unsigned count);

    CallArg* PushFront(Compiler* comp, const NewCallArg& arg);
    CallArg* PushBack(Compiler* comp, const NewCallArg& arg);
    CallArg* InsertAfter(Compiler* comp, CallArg* after, const NewCallArg& arg);
    CallArg* InsertAfterUnchecked(Compiler* comp, CallArg* after, const NewCallArg& arg);
    CallArg* InsertInstParam(Compiler* comp, GenTree* node);
    CallArg* InsertAfterThisOrFirst(Compiler* comp, const NewCallArg& arg);
    void PushLateBack(CallArg* arg);
    void Remove(CallArg* arg);

    template <typename CopyNodeFunc>
    void InternalCopyFrom(Compiler* comp, CallArgs* other, CopyNodeFunc copyFunc);

    template <typename... Args>
    void PushFront(Compiler* comp, const NewCallArg& arg, Args&&... rest)
    {
        PushFront(comp, std::forward<Args>(rest)...);
        PushFront(comp, arg);
    }

    void ResetFinalArgsAndABIInfo();
    void AddFinalArgsAndDetermineABIInfo(Compiler* comp, GenTreeCall* call);

    void ArgsComplete(Compiler* comp, GenTreeCall* call);
    void EvalArgsToTemps(Compiler* comp, GenTreeCall* call);
    void SetNeedsTemp(CallArg* arg);
    bool IsNonStandard(Compiler* comp, GenTreeCall* call, CallArg* arg);

    GenTree* MakeTmpArgNode(Compiler* comp, CallArg* arg);
    void SetTemp(CallArg* arg, unsigned tmpNum);

    // clang-format off
    bool HasThisPointer() const { return m_hasThisPointer; }
    bool HasRetBuffer() const { return m_hasRetBuffer; }
    bool IsVarArgs() const { return m_isVarArgs; }
    void SetIsVarArgs() { m_isVarArgs = true; }
    void ClearIsVarArgs() { m_isVarArgs = false; }
    bool IsAbiInformationDetermined() const { return m_abiInformationDetermined; }
    bool AreArgsComplete() const { return m_argsComplete; }
    bool HasRegArgs() const { return m_hasRegArgs; }
    bool HasStackArgs() const { return m_hasStackArgs; }
    bool NeedsTemps() const { return m_needsTemps; }

#ifdef UNIX_X86_ABI
    void ComputeStackAlignment(unsigned curStackLevelInBytes)
    {
        m_padStkAlign = AlignmentPad(curStackLevelInBytes, STACK_ALIGN);
    }
    unsigned GetStkAlign() const { return m_padStkAlign; }
    unsigned GetStkSizeBytes() { return m_stkSizeBytes; }
    void SetStkSizeBytes(unsigned bytes) { m_stkSizeBytes = bytes; }
    bool IsStkAlignmentDone() const { return m_alignmentDone; }
    void SetStkAlignmentDone() { m_alignmentDone = true; }
#endif
    // clang-format on

    unsigned OutgoingArgsStackSize() const;

    unsigned CountArgs();
    unsigned CountUserArgs();

    template <CallArg* (CallArg::*Next)()>
    class CallArgIterator
    {
        CallArg* m_arg;

    public:
        explicit CallArgIterator(CallArg* arg) : m_arg(arg)
        {
        }

        // clang-format off
        CallArg& operator*() const { return *m_arg; }
        CallArg* operator->() const { return m_arg; }
        CallArg* GetArg() const { return m_arg; }
        // clang-format on

        CallArgIterator& operator++()
        {
            m_arg = (m_arg->*Next)();
            return *this;
        }

        bool operator==(const CallArgIterator& i) const
        {
            return m_arg == i.m_arg;
        }

        bool operator!=(const CallArgIterator& i) const
        {
            return m_arg != i.m_arg;
        }
    };

    class EarlyArgIterator
    {
        friend class CallArgs;

        CallArg* m_arg;

        static CallArg* NextEarlyArg(CallArg* cur)
        {
            while ((cur != nullptr) && (cur->GetEarlyNode() == nullptr))
            {
                cur = cur->GetNext();
            }

            return cur;
        }

    public:
        explicit EarlyArgIterator(CallArg* arg) : m_arg(arg)
        {
        }

        // clang-format off
        CallArg& operator*() const { return *m_arg; }
        CallArg* operator->() const { return m_arg; }
        CallArg* GetArg() const { return m_arg; }
        // clang-format on

        EarlyArgIterator& operator++()
        {
            m_arg = NextEarlyArg(m_arg->GetNext());
            return *this;
        }

        bool operator==(const EarlyArgIterator& i) const
        {
            return m_arg == i.m_arg;
        }

        bool operator!=(const EarlyArgIterator& i) const
        {
            return m_arg != i.m_arg;
        }
    };

    using ArgIterator     = CallArgIterator<&CallArg::GetNext>;
    using LateArgIterator = CallArgIterator<&CallArg::GetLateNext>;

    IteratorPair<ArgIterator> Args()
    {
        return IteratorPair<ArgIterator>(ArgIterator(m_head), ArgIterator(nullptr));
    }

    IteratorPair<EarlyArgIterator> EarlyArgs()
    {
        CallArg* firstEarlyArg = EarlyArgIterator::NextEarlyArg(m_head);
        return IteratorPair<EarlyArgIterator>(EarlyArgIterator(firstEarlyArg), EarlyArgIterator(nullptr));
    }

    IteratorPair<LateArgIterator> LateArgs()
    {
        return IteratorPair<LateArgIterator>(LateArgIterator(m_lateHead), LateArgIterator(nullptr));
    }
};

struct GenTreeCall final : public GenTree
{
    CallArgs gtArgs;

#ifdef DEBUG
    // Used to register callsites with the EE
    CORINFO_SIG_INFO* callSig;
#endif

    union {
        TailCallSiteInfo* tailCallInfo;
        // Only used for unmanaged calls, which cannot be tail-called
        CorInfoCallConvExtension unmgdCallConv;
    };

#if FEATURE_MULTIREG_RET

    // State required to support multi-reg returning call nodes.
    //
    // TODO-AllArch: enable for all call nodes to unify single-reg and multi-reg returns.
    ReturnTypeDesc gtReturnTypeDesc;

    // GetRegNum() would always be the first return reg.
    // The following array holds the other reg numbers of multi-reg return.
    regNumberSmall gtOtherRegs[MAX_RET_REG_COUNT - 1];

    MultiRegSpillFlags gtSpillFlags;

#endif // FEATURE_MULTIREG_RET

    //-----------------------------------------------------------------------
    // GetReturnTypeDesc: get the type descriptor of return value of the call
    //
    // Arguments:
    //    None
    //
    // Returns
    //    Type descriptor of the value returned by call
    //
    // TODO-AllArch: enable for all call nodes to unify single-reg and multi-reg returns.
    const ReturnTypeDesc* GetReturnTypeDesc() const
    {
#if FEATURE_MULTIREG_RET
        return &gtReturnTypeDesc;
#else
        return nullptr;
#endif
    }

    void InitializeLongReturnType()
    {
#if FEATURE_MULTIREG_RET
        gtReturnTypeDesc.InitializeLongReturnType();
#endif
    }

    void InitializeStructReturnType(Compiler* comp, CORINFO_CLASS_HANDLE retClsHnd, CorInfoCallConvExtension callConv)
    {
#if FEATURE_MULTIREG_RET
        gtReturnTypeDesc.InitializeStructReturnType(comp, retClsHnd, callConv);
#endif
    }

    void ResetReturnType()
    {
#if FEATURE_MULTIREG_RET
        gtReturnTypeDesc.Reset();
#endif
    }

    //---------------------------------------------------------------------------
    // GetRegNumByIdx: get i'th return register allocated to this call node.
    //
    // Arguments:
    //     idx   -   index of the return register
    //
    // Return Value:
    //     Return regNumber of i'th return register of call node.
    //     Returns REG_NA if there is no valid return register for the given index.
    //
    regNumber GetRegNumByIdx(unsigned idx) const
    {
        assert(idx < MAX_RET_REG_COUNT);

        if (idx == 0)
        {
            return GetRegNum();
        }

#if FEATURE_MULTIREG_RET
        return (regNumber)gtOtherRegs[idx - 1];
#else
        return REG_NA;
#endif
    }

    //----------------------------------------------------------------------
    // SetRegNumByIdx: set i'th return register of this call node
    //
    // Arguments:
    //    reg    -   reg number
    //    idx    -   index of the return register
    //
    // Return Value:
    //    None
    //
    void SetRegNumByIdx(regNumber reg, unsigned idx)
    {
        assert(idx < MAX_RET_REG_COUNT);

        if (idx == 0)
        {
            SetRegNum(reg);
        }
#if FEATURE_MULTIREG_RET
        else
        {
            gtOtherRegs[idx - 1] = (regNumberSmall)reg;
            assert(gtOtherRegs[idx - 1] == reg);
        }
#else
        unreached();
#endif
    }

    //----------------------------------------------------------------------------
    // ClearOtherRegs: clear multi-reg state to indicate no regs are allocated
    //
    // Arguments:
    //    None
    //
    // Return Value:
    //    None
    //
    void ClearOtherRegs()
    {
#if FEATURE_MULTIREG_RET
        for (unsigned i = 0; i < MAX_RET_REG_COUNT - 1; ++i)
        {
            gtOtherRegs[i] = REG_NA;
        }
#endif
    }

    //----------------------------------------------------------------------------
    // CopyOtherRegs: copy multi-reg state from the given call node to this node
    //
    // Arguments:
    //    fromCall  -  GenTreeCall node from which to copy multi-reg state
    //
    // Return Value:
    //    None
    //
    void CopyOtherRegs(GenTreeCall* fromCall)
    {
#if FEATURE_MULTIREG_RET
        for (unsigned i = 0; i < MAX_RET_REG_COUNT - 1; ++i)
        {
            this->gtOtherRegs[i] = fromCall->gtOtherRegs[i];
        }
#endif
    }

#ifdef TARGET_XARCH
    bool NeedsVzeroupper(Compiler* comp);
#endif // TARGET_XARCH

    // Get reg mask of all the valid registers of gtOtherRegs array
    regMaskTP GetOtherRegMask() const;

    GenTreeFlags GetRegSpillFlagByIdx(unsigned idx) const
    {
#if FEATURE_MULTIREG_RET
        return GetMultiRegSpillFlagsByIdx(gtSpillFlags, idx);
#else
        assert(!"unreached");
        return GTF_EMPTY;
#endif
    }

    void SetRegSpillFlagByIdx(GenTreeFlags flags, unsigned idx)
    {
#if FEATURE_MULTIREG_RET
        gtSpillFlags = SetMultiRegSpillFlagsByIdx(gtSpillFlags, flags, idx);
#endif
    }

    //-------------------------------------------------------------------
    // clearOtherRegFlags: clear GTF_* flags associated with gtOtherRegs
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     None
    void ClearOtherRegFlags()
    {
#if FEATURE_MULTIREG_RET
        gtSpillFlags = 0;
#endif
    }

    //-------------------------------------------------------------------------
    // CopyOtherRegFlags: copy GTF_* flags associated with gtOtherRegs from
    // the given call node.
    //
    // Arguments:
    //    fromCall  -  GenTreeCall node from which to copy
    //
    // Return Value:
    //    None
    //
    void CopyOtherRegFlags(GenTreeCall* fromCall)
    {
#if FEATURE_MULTIREG_RET
        this->gtSpillFlags = fromCall->gtSpillFlags;
#endif
    }

    bool IsUnmanaged() const
    {
        return (gtFlags & GTF_CALL_UNMANAGED) != 0;
    }
    bool NeedsNullCheck() const
    {
        return (gtFlags & GTF_CALL_NULLCHECK) != 0;
    }
    bool CallerPop() const
    {
        return (gtFlags & GTF_CALL_POP_ARGS) != 0;
    }
    bool IsVirtual() const
    {
        return (gtFlags & GTF_CALL_VIRT_KIND_MASK) != GTF_CALL_NONVIRT;
    }
    bool IsVirtualStub() const
    {
        return (gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_STUB;
    }
    bool IsVirtualVtable() const
    {
        return (gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_VTABLE;
    }
    bool IsInlineCandidate() const
    {
        return (gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0;
    }

    bool IsR2ROrVirtualStubRelativeIndir()
    {
#if defined(FEATURE_READYTORUN)
        if (IsR2RRelativeIndir())
        {
            return true;
        }
#endif

        return IsVirtualStubRelativeIndir();
    }

    bool HasNonStandardAddedArgs(Compiler* compiler) const;
    int GetNonStandardAddedArgCount(Compiler* compiler) const;

    // Returns true if the ABI dictates that this call should get a ret buf
    // arg. This may be out of sync with gtArgs.HasRetBuffer during import
    // until we actually create the ret buffer.
    bool ShouldHaveRetBufArg() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_RETBUFFARG) != 0;
    }

    //-------------------------------------------------------------------------
    // TreatAsShouldHaveRetBufArg:
    //
    // Arguments:
    //     compiler, the compiler instance so that we can call eeGetHelperNum
    //
    // Return Value:
    //     Returns true if we treat the call as if it has a retBuf argument
    //     This method may actually have a retBuf argument
    //     or it could be a JIT helper that we are still transforming during
    //     the importer phase.
    //
    // Notes:
    //     On ARM64 marking the method with the GTF_CALL_M_RETBUFFARG flag
    //     will make ShouldHaveRetBufArg() return true, but will also force the
    //     use of register x8 to pass the RetBuf argument.
    //
    bool TreatAsShouldHaveRetBufArg(Compiler* compiler) const;

    //-----------------------------------------------------------------------------------------
    // HasMultiRegRetVal: whether the call node returns its value in multiple return registers.
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     True if the call is returning a multi-reg return value. False otherwise.
    //
    bool HasMultiRegRetVal() const
    {
#ifdef FEATURE_MULTIREG_RET
#if defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
        return (gtType == TYP_STRUCT) && (gtReturnTypeDesc.GetReturnRegCount() > 1);
#else

#if defined(TARGET_X86) || defined(TARGET_ARM)
        if (varTypeIsLong(gtType))
        {
            return true;
        }
#endif

        if (!varTypeIsStruct(gtType) || ShouldHaveRetBufArg())
        {
            return false;
        }
        // Now it is a struct that is returned in registers.
        return GetReturnTypeDesc()->IsMultiRegRetType();
#endif

#else  // !FEATURE_MULTIREG_RET
        return false;
#endif // !FEATURE_MULTIREG_RET
    }

    // Returns true if VM has flagged this method as CORINFO_FLG_PINVOKE.
    bool IsPInvoke() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_PINVOKE) != 0;
    }

    // Note that the distinction of whether tail prefixed or an implicit tail call
    // is maintained on a call node till fgMorphCall() after which it will be
    // either a tail call (i.e. IsTailCall() is true) or a non-tail call.
    bool IsTailPrefixedCall() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_EXPLICIT_TAILCALL) != 0;
    }

    // Returns true if this call didn't have an explicit tail. prefix in the IL
    // but was marked as an explicit tail call because of tail call stress mode.
    bool IsStressTailCall() const
    {
#ifdef DEBUG
        return (gtCallDebugFlags & GTF_CALL_MD_STRESS_TAILCALL) != 0;
#else
        return false;
#endif
    }

    // This method returning "true" implies that tail call flowgraph morhphing has
    // performed final checks and committed to making a tail call.
    bool IsTailCall() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_TAILCALL) != 0;
    }

    // This method returning "true" implies that importer has performed tail call checks
    // and providing a hint that this can be converted to a tail call.
    bool CanTailCall() const
    {
        return IsTailPrefixedCall() || IsImplicitTailCall();
    }

    // Check whether this is a tailcall dispatched via JIT helper. We only use
    // this mechanism on x86 as it is faster than our other more general
    // tailcall mechanism.
    bool IsTailCallViaJitHelper() const
    {
#ifdef TARGET_X86
        return IsTailCall() && (gtCallMoreFlags & GTF_CALL_M_TAILCALL_VIA_JIT_HELPER);
#else
        return false;
#endif
    }

#if FEATURE_FASTTAILCALL
    bool IsFastTailCall() const
    {
#ifdef TARGET_X86
        return IsTailCall() && !(gtCallMoreFlags & GTF_CALL_M_TAILCALL_VIA_JIT_HELPER);
#else
        return IsTailCall();
#endif
    }
#else  // !FEATURE_FASTTAILCALL
    bool IsFastTailCall() const
    {
        return false;
    }
#endif // !FEATURE_FASTTAILCALL

#if FEATURE_TAILCALL_OPT
    // Returns true if this is marked for opportunistic tail calling.
    // That is, can be tail called though not explicitly prefixed with "tail" prefix.
    bool IsImplicitTailCall() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_IMPLICIT_TAILCALL) != 0;
    }
    bool IsTailCallConvertibleToLoop() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_TAILCALL_TO_LOOP) != 0;
    }
#else  // !FEATURE_TAILCALL_OPT
    bool IsImplicitTailCall() const
    {
        return false;
    }
    bool IsTailCallConvertibleToLoop() const
    {
        return false;
    }
#endif // !FEATURE_TAILCALL_OPT

    bool NormalizesSmallTypesOnReturn() const
    {
        return GetUnmanagedCallConv() == CorInfoCallConvExtension::Managed;
    }

    bool IsSameThis() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_NONVIRT_SAME_THIS) != 0;
    }
    bool IsDelegateInvoke() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_DELEGATE_INV) != 0;
    }
    bool IsVirtualStubRelativeIndir() const
    {
        return IsVirtualStub() && (gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT) != 0;
    }
    bool IsSpecialIntrinsic() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) != 0;
    }

    bool IsR2RRelativeIndir() const
    {
#ifdef FEATURE_READYTORUN
        return gtEntryPoint.accessType == IAT_PVALUE;
#else
        return false;
#endif
    }
#ifdef FEATURE_READYTORUN
    void setEntryPoint(const CORINFO_CONST_LOOKUP& entryPoint)
    {
        gtEntryPoint = entryPoint;
    }
#endif // FEATURE_READYTORUN

    bool IsVarargs() const
    {
        return gtArgs.IsVarArgs();
    }

    bool IsNoReturn() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_DOES_NOT_RETURN) != 0;
    }

    bool IsFatPointerCandidate() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_FAT_POINTER_CHECK) != 0;
    }

    bool IsGuardedDevirtualizationCandidate() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT) != 0;
    }

    bool IsPure(Compiler* compiler) const;

    bool HasSideEffects(Compiler* compiler, bool ignoreExceptions = false, bool ignoreCctors = false) const;

    void ClearFatPointerCandidate()
    {
        gtCallMoreFlags &= ~GTF_CALL_M_FAT_POINTER_CHECK;
    }

    void SetFatPointerCandidate()
    {
        gtCallMoreFlags |= GTF_CALL_M_FAT_POINTER_CHECK;
    }

#ifdef DEBUG
    bool IsDevirtualized() const
    {
        return (gtCallDebugFlags & GTF_CALL_MD_DEVIRTUALIZED) != 0;
    }

    bool IsGuarded() const
    {
        return (gtCallDebugFlags & GTF_CALL_MD_GUARDED) != 0;
    }

    bool IsUnboxed() const
    {
        return (gtCallDebugFlags & GTF_CALL_MD_UNBOXED) != 0;
    }
#endif

    bool IsSuppressGCTransition() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_SUPPRESS_GC_TRANSITION) != 0;
    }

    void ClearGuardedDevirtualizationCandidate()
    {
        gtCallMoreFlags &= ~(GTF_CALL_M_GUARDED_DEVIRT | GTF_CALL_M_GUARDED_DEVIRT_EXACT);
    }

#ifdef DEBUG
    void SetIsGuarded()
    {
        gtCallDebugFlags |= GTF_CALL_MD_GUARDED;
    }
#endif

    void SetExpandedEarly()
    {
        gtCallMoreFlags |= GTF_CALL_M_EXPANDED_EARLY;
    }

    void ClearExpandedEarly()
    {
        gtCallMoreFlags &= ~GTF_CALL_M_EXPANDED_EARLY;
    }

    bool IsExpandedEarly() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_EXPANDED_EARLY) != 0;
    }

    bool IsOptimizingRetBufAsLocal() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_RETBUFFARG_LCLOPT) != 0;
    }

    InlineCandidateInfo* GetSingleInlineCandidateInfo()
    {
        // gtInlineInfoCount can be 0 (not an inline candidate) or 1
        if (gtInlineInfoCount == 0)
        {
            assert(!IsInlineCandidate());
            assert(gtInlineCandidateInfo == nullptr);
            return nullptr;
        }
        else if (gtInlineInfoCount > 1)
        {
            assert(!"Call has multiple inline candidates");
        }
        return gtInlineCandidateInfo;
    }

    void SetSingleInlineCandidateInfo(InlineCandidateInfo* candidateInfo);

    InlineCandidateInfo* GetGDVCandidateInfo(uint8_t index);

    void AddGDVCandidateInfo(Compiler* comp, InlineCandidateInfo* candidateInfo);

    void RemoveGDVCandidateInfo(Compiler* comp, uint8_t index);

    void ClearInlineInfo()
    {
        SetSingleInlineCandidateInfo(nullptr);
    }

    uint8_t GetInlineCandidatesCount()
    {
        return gtInlineInfoCount;
    }

    //-----------------------------------------------------------------------------------------
    // GetIndirectionCellArgKind: Get the kind of indirection cell used by this call.
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     The kind (either R2RIndirectionCell or VirtualStubCell),
    //     or WellKnownArg::None if this call does not have an indirection cell.
    //
    WellKnownArg GetIndirectionCellArgKind() const
    {
        if (IsVirtualStub())
        {
            return WellKnownArg::VirtualStubCell;
        }

#if defined(TARGET_ARMARCH) || defined(TARGET_RISCV64)
        // For ARM architectures, we always use an indirection cell for R2R calls.
        if (IsR2RRelativeIndir() && !IsDelegateInvoke())
        {
            return WellKnownArg::R2RIndirectionCell;
        }
#elif defined(TARGET_XARCH)
        // On XARCH we disassemble it from callsite except for tailcalls that need indirection cell.
        if (IsR2RRelativeIndir() && IsFastTailCall())
        {
            return WellKnownArg::R2RIndirectionCell;
        }
#endif

        return WellKnownArg::None;
    }

    CFGCallKind GetCFGCallKind() const
    {
#if defined(TARGET_AMD64)
        // On x64 the dispatcher is more performant, but we cannot use it when
        // we need to pass indirection cells as those go into registers that
        // are clobbered by the dispatch helper.
        bool mayUseDispatcher    = GetIndirectionCellArgKind() == WellKnownArg::None;
        bool shouldUseDispatcher = true;
#elif defined(TARGET_ARM64)
        bool mayUseDispatcher = true;
        // Branch predictors on ARM64 generally do not handle the dispatcher as
        // well as on x64 hardware, so only use the validator by default.
        bool shouldUseDispatcher = false;
#else
        // Other platforms do not even support the dispatcher.
        bool mayUseDispatcher    = false;
        bool shouldUseDispatcher = false;
#endif

#ifdef DEBUG
        switch (JitConfig.JitCFGUseDispatcher())
        {
            case 0:
                shouldUseDispatcher = false;
                break;
            case 1:
                shouldUseDispatcher = true;
                break;
            default:
                break;
        }
#endif

        return mayUseDispatcher && shouldUseDispatcher ? CFGCallKind::Dispatch : CFGCallKind::ValidateAndCall;
    }

    GenTreeCallFlags gtCallMoreFlags;  // in addition to gtFlags
    gtCallTypes      gtCallType : 3;   // value from the gtCallTypes enumeration
    var_types        gtReturnType : 5; // exact return type

    uint8_t gtInlineInfoCount; // number of inline candidates for the given call

    CORINFO_CLASS_HANDLE gtRetClsHnd; // The return type handle of the call if it is a struct; always available
    union {
        void*                gtStubCallStubAddr;   // GTF_CALL_VIRT_STUB - these are never inlined
        CORINFO_CLASS_HANDLE gtInitClsHnd;         // Used by static init helpers, represents a class they init
        IL_OFFSET            gtCastHelperILOffset; // Used by cast helpers to save corresponding IL offset
    };

    union {
        // only used for CALLI unmanaged calls (CT_INDIRECT)
        GenTree* gtCallCookie;

        // gtInlineCandidateInfo is only used when inlining methods
        InlineCandidateInfo* gtInlineCandidateInfo;
        // gtInlineCandidateInfoList is used when we have more than one GDV candidate
        jitstd::vector<InlineCandidateInfo*>* gtInlineCandidateInfoList;

        HandleHistogramProfileCandidateInfo* gtHandleHistogramProfileCandidateInfo;
        LateDevirtualizationInfo*            gtLateDevirtualizationInfo;
        CORINFO_GENERIC_HANDLE compileTimeHelperArgumentHandle; // Used to track type handle argument of dynamic helpers
        void*                  gtDirectCallAddress; // Used to pass direct call address between lower and codegen
    };

    // expression evaluated after args are placed which determines the control target
    GenTree* gtControlExpr;

    union {
        CORINFO_METHOD_HANDLE gtCallMethHnd; // CT_USER_FUNC or CT_HELPER
        GenTree*              gtCallAddr;    // CT_INDIRECT
    };

#ifdef FEATURE_READYTORUN
    // Call target lookup info for method call from a Ready To Run module
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

#ifdef DEBUG
    GenTreeCallDebugFlags gtCallDebugFlags;

    // For non-inline candidates, track the first observation
    // that blocks candidacy.
    InlineObservation gtInlineObservation;

    // IL offset of the call wrt its parent method.
    IL_OFFSET gtRawILOffset;

    // In DEBUG we report even non inline candidates in the inline tree in
    // fgNoteNonInlineCandidate. We need to keep around the inline context for
    // this as normally it's part of the candidate info.
    class InlineContext* gtInlineContext;
#endif // defined(DEBUG)

    bool IsHelperCall() const
    {
        return gtCallType == CT_HELPER;
    }

    bool IsHelperCall(CORINFO_METHOD_HANDLE callMethHnd) const
    {
        return IsHelperCall() && (callMethHnd == gtCallMethHnd);
    }

    bool IsHelperCall(Compiler* compiler, unsigned helper) const;

    bool IsRuntimeLookupHelperCall(Compiler* compiler) const;

    bool IsSpecialIntrinsic(Compiler* compiler, NamedIntrinsic ni) const;

    CorInfoHelpFunc GetHelperNum() const;

    bool AreArgsComplete() const;

    CorInfoCallConvExtension GetUnmanagedCallConv() const
    {
        return IsUnmanaged() ? unmgdCallConv : CorInfoCallConvExtension::Managed;
    }

    static bool Equals(GenTreeCall* c1, GenTreeCall* c2);

    GenTreeCall(var_types type) : GenTree(GT_CALL, type)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeCall() : GenTree()
    {
    }
#endif
};

#if !defined(TARGET_64BIT)
struct GenTreeMultiRegOp : public GenTreeOp
{
    regNumber gtOtherReg;

    // GTF_SPILL or GTF_SPILLED flag on a multi-reg node indicates that one or
    // more of its result regs are in that state.  The spill flag of each of the
    // return register is stored here. We only need 2 bits per returned register,
    // so this is treated as a 2-bit array. No architecture needs more than 8 bits.

    MultiRegSpillFlags gtSpillFlags;

    GenTreeMultiRegOp(genTreeOps oper, var_types type, GenTree* op1, GenTree* op2)
        : GenTreeOp(oper, type, op1, op2), gtOtherReg(REG_NA)
    {
        ClearOtherRegFlags();
    }

    unsigned GetRegCount() const
    {
        return (TypeGet() == TYP_LONG) ? 2 : 1;
    }

    //---------------------------------------------------------------------------
    // GetRegNumByIdx: get i'th register allocated to this struct argument.
    //
    // Arguments:
    //     idx   -   index of the register
    //
    // Return Value:
    //     Return regNumber of i'th register of this register argument
    //
    regNumber GetRegNumByIdx(unsigned idx) const
    {
        assert(idx < 2);

        if (idx == 0)
        {
            return GetRegNum();
        }

        return gtOtherReg;
    }

    GenTreeFlags GetRegSpillFlagByIdx(unsigned idx) const
    {
        return GetMultiRegSpillFlagsByIdx(gtSpillFlags, idx);
    }

    void SetRegSpillFlagByIdx(GenTreeFlags flags, unsigned idx)
    {
#if FEATURE_MULTIREG_RET
        gtSpillFlags = SetMultiRegSpillFlagsByIdx(gtSpillFlags, flags, idx);
#endif
    }

    //--------------------------------------------------------------------------
    // GetRegType:  Get var_type of the register specified by index.
    //
    // Arguments:
    //    index - Index of the register.
    //            First register will have an index 0 and so on.
    //
    // Return Value:
    //    var_type of the register specified by its index.
    //
    var_types GetRegType(unsigned index) const
    {
        assert(index < 2);
        // The type of register is usually the same as GenTree type, since GenTreeMultiRegOp usually defines a single
        // reg.
        // The special case is when we have TYP_LONG, which may be a MUL_LONG, or a DOUBLE arg passed as LONG,
        // in which case we need to separate them into int for each index.
        var_types result = TypeGet();
        if (result == TYP_LONG)
        {
            result = TYP_INT;
        }
        return result;
    }

    //-------------------------------------------------------------------
    // clearOtherRegFlags: clear GTF_* flags associated with gtOtherRegs
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     None
    //
    void ClearOtherRegFlags()
    {
        gtSpillFlags = 0;
    }

#if DEBUGGABLE_GENTREE
    GenTreeMultiRegOp() : GenTreeOp()
    {
    }
#endif
};
#endif // !defined(TARGET_64BIT)

struct GenTreeFptrVal : public GenTree
{
    CORINFO_METHOD_HANDLE gtFptrMethod;

    bool gtFptrDelegateTarget;

#ifdef FEATURE_READYTORUN
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

    GenTreeFptrVal(var_types type, CORINFO_METHOD_HANDLE meth)
        : GenTree(GT_FTN_ADDR, type), gtFptrMethod(meth), gtFptrDelegateTarget(false)
    {
#ifdef FEATURE_READYTORUN
        gtEntryPoint.addr       = nullptr;
        gtEntryPoint.accessType = IAT_VALUE;
#endif
    }
#if DEBUGGABLE_GENTREE
    GenTreeFptrVal() : GenTree()
    {
    }
#endif
};

/* gtQmark */
struct GenTreeQmark : public GenTreeOp
{
    unsigned gtThenLikelihood;

    GenTreeQmark(var_types type, GenTree* cond, GenTreeColon* colon, unsigned thenLikelihood = 50)
        : GenTreeOp(GT_QMARK, type, cond, colon), gtThenLikelihood(thenLikelihood)
    {
        // These must follow a specific form.
        assert((cond != nullptr) && cond->TypeIs(TYP_INT));
        assert((colon != nullptr) && colon->OperIs(GT_COLON));
    }

    GenTree* ThenNode() const
    {
        return gtOp2->AsColon()->ThenNode();
    }

    GenTree* ElseNode() const
    {
        return gtOp2->AsColon()->ElseNode();
    }

    unsigned ThenNodeLikelihood() const
    {
        assert(gtThenLikelihood <= 100);
        return gtThenLikelihood;
    }

    unsigned ElseNodeLikelihood() const
    {
        assert(gtThenLikelihood <= 100);
        return 100 - gtThenLikelihood;
    }

    void SetThenNodeLikelihood(unsigned thenLikelihood)
    {
        assert(thenLikelihood <= 100);
        gtThenLikelihood = thenLikelihood;
    }

#if DEBUGGABLE_GENTREE
    GenTreeQmark() : GenTreeOp()
    {
    }
#endif
};

/* gtIntrinsic   -- intrinsic   (possibly-binary op [NULL op2 is allowed] with an additional field) */

struct GenTreeIntrinsic : public GenTreeOp
{
    NamedIntrinsic        gtIntrinsicName;
    CORINFO_METHOD_HANDLE gtMethodHandle; // Method handle of the method which is treated as an intrinsic.

#ifdef FEATURE_READYTORUN
    // Call target lookup info for method call from a Ready To Run module
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

    GenTreeIntrinsic(var_types type, GenTree* op1, NamedIntrinsic intrinsicName, CORINFO_METHOD_HANDLE methodHandle)
        : GenTreeOp(GT_INTRINSIC, type, op1, nullptr), gtIntrinsicName(intrinsicName), gtMethodHandle(methodHandle)
    {
        assert(intrinsicName != NI_Illegal);
    }

    GenTreeIntrinsic(
        var_types type, GenTree* op1, GenTree* op2, NamedIntrinsic intrinsicName, CORINFO_METHOD_HANDLE methodHandle)
        : GenTreeOp(GT_INTRINSIC, type, op1, op2), gtIntrinsicName(intrinsicName), gtMethodHandle(methodHandle)
    {
        assert(intrinsicName != NI_Illegal);
    }

#if DEBUGGABLE_GENTREE
    GenTreeIntrinsic() : GenTreeOp()
    {
    }
#endif
};

// GenTreeMultiOp - a node with a flexible count of operands stored in an array.
// The array can be an inline one, or a dynamic one, or both, with switching
// between them supported. See GenTreeJitIntrinsic for an example of a node
// utilizing GenTreeMultiOp. GTF_REVERSE_OPS is supported for GenTreeMultiOp's
// with two operands.
//
struct GenTreeMultiOp : public GenTree
{
public:
    class Iterator
    {
    protected:
        GenTree** m_use;

        Iterator(GenTree** use) : m_use(use)
        {
        }

    public:
        Iterator& operator++()
        {
            m_use++;
            return *this;
        }

        bool operator==(const Iterator& other) const
        {
            return m_use == other.m_use;
        }

        bool operator!=(const Iterator& other) const
        {
            return m_use != other.m_use;
        }
    };

    class OperandsIterator final : public Iterator
    {
    public:
        OperandsIterator(GenTree** use) : Iterator(use)
        {
        }

        GenTree* operator*()
        {
            return *m_use;
        }
    };

    class UseEdgesIterator final : public Iterator
    {
    public:
        UseEdgesIterator(GenTree** use) : Iterator(use)
        {
        }

        GenTree** operator*()
        {
            return m_use;
        }
    };

private:
    GenTree** m_operands;

protected:
    template <unsigned InlineOperandCount, typename... Operands>
    GenTreeMultiOp(genTreeOps    oper,
                   var_types     type,
                   CompAllocator allocator,
                   GenTree* (&inlineOperands)[InlineOperandCount] DEBUGARG(bool largeNode),
                   Operands... operands)
        : GenTree(oper, type DEBUGARG(largeNode))
    {
        const size_t OperandCount = sizeof...(Operands);

        m_operands = (OperandCount <= InlineOperandCount) ? inlineOperands : allocator.allocate<GenTree*>(OperandCount);

        // "OperandCount + 1" so that it works well when OperandCount is 0.
        GenTree* operandsArray[OperandCount + 1]{operands...};
        InitializeOperands(operandsArray, OperandCount);
    }

    // Note that this constructor takes the owndership of the "operands" array.
    template <unsigned InlineOperandCount>
    GenTreeMultiOp(genTreeOps oper,
                   var_types  type,
                   GenTree**  operands,
                   size_t     operandCount,
                   GenTree* (&inlineOperands)[InlineOperandCount] DEBUGARG(bool largeNode))
        : GenTree(oper, type DEBUGARG(largeNode))
    {
        m_operands = (operandCount <= InlineOperandCount) ? inlineOperands : operands;

        InitializeOperands(operands, operandCount);
    }

public:
#if DEBUGGABLE_GENTREE
    GenTreeMultiOp() : GenTree()
    {
    }
#endif

    GenTree*& Op(size_t index)
    {
        size_t actualIndex = index - 1;
        assert(actualIndex < m_operandCount);
        assert(m_operands[actualIndex] != nullptr);

        return m_operands[actualIndex];
    }

    GenTree* Op(size_t index) const
    {
        return const_cast<GenTreeMultiOp*>(this)->Op(index);
    }

    // Note that unlike the general "Operands" iterator, this specialized version does not respect GTF_REVERSE_OPS.
    IteratorPair<OperandsIterator> Operands()
    {
        return MakeIteratorPair(OperandsIterator(GetOperandArray()),
                                OperandsIterator(GetOperandArray() + GetOperandCount()));
    }

    // Note that unlike the general "UseEdges" iterator, this specialized version does not respect GTF_REVERSE_OPS.
    IteratorPair<UseEdgesIterator> UseEdges()
    {
        return MakeIteratorPair(UseEdgesIterator(GetOperandArray()),
                                UseEdgesIterator(GetOperandArray() + GetOperandCount()));
    }

    size_t GetOperandCount() const
    {
        return m_operandCount;
    }

    GenTree** GetOperandArray(size_t startIndex = 0) const
    {
        return m_operands + startIndex;
    }

protected:
    // Reconfigures the operand array, leaving it in a "dirty" state.
    void ResetOperandArray(size_t    newOperandCount,
                           Compiler* compiler,
                           GenTree** inlineOperands,
                           size_t    inlineOperandCount);

    static bool OperandsAreEqual(GenTreeMultiOp* op1, GenTreeMultiOp* op2);

private:
    void InitializeOperands(GenTree** operands, size_t operandCount);

    void SetOperandCount(size_t newOperandCount)
    {
        assert(FitsIn<uint8_t>(newOperandCount));
        m_operandCount = static_cast<uint8_t>(newOperandCount);
    }
};

// Helper class used to implement the constructor of GenTreeJitIntrinsic which
// transfers the ownership of the passed-in array to the underlying MultiOp node.
class IntrinsicNodeBuilder final
{
    friend struct GenTreeJitIntrinsic;

    GenTree** m_operands;
    size_t    m_operandCount;
    GenTree*  m_inlineOperands[2];

public:
    IntrinsicNodeBuilder(CompAllocator allocator, size_t operandCount) : m_operandCount(operandCount)
    {
        m_operands =
            (operandCount <= ArrLen(m_inlineOperands)) ? m_inlineOperands : allocator.allocate<GenTree*>(operandCount);
#ifdef DEBUG
        for (size_t i = 0; i < operandCount; i++)
        {
            m_operands[i] = nullptr;
        }
#endif // DEBUG
    }

    IntrinsicNodeBuilder(CompAllocator allocator, GenTreeMultiOp* source) : m_operandCount(source->GetOperandCount())
    {
        m_operands = (m_operandCount <= ArrLen(m_inlineOperands)) ? m_inlineOperands
                                                                  : allocator.allocate<GenTree*>(m_operandCount);
        for (size_t i = 0; i < m_operandCount; i++)
        {
            m_operands[i] = source->Op(i + 1);
        }
    }

    void AddOperand(size_t index, GenTree* operand)
    {
        assert(index < m_operandCount);
        assert(m_operands[index] == nullptr);
        m_operands[index] = operand;
    }

    GenTree* GetOperand(size_t index) const
    {
        assert(index < m_operandCount);
        assert(m_operands[index] != nullptr);
        return m_operands[index];
    }

    size_t GetOperandCount() const
    {
        return m_operandCount;
    }

private:
    GenTree** GetBuiltOperands()
    {
#ifdef DEBUG
        for (size_t i = 0; i < m_operandCount; i++)
        {
            assert(m_operands[i] != nullptr);
        }
#endif // DEBUG

        return m_operands;
    }
};

struct GenTreeJitIntrinsic : public GenTreeMultiOp
{
protected:
    GenTree*           gtInlineOperands[2];
    regNumberSmall     gtOtherReg;     // The second register for multi-reg intrinsics.
    MultiRegSpillFlags gtSpillFlags;   // Spill flags for multi-reg intrinsics.
    unsigned char  gtAuxiliaryJitType; // For intrinsics than need another type (e.g. Avx2.Gather* or SIMD (by element))
    unsigned char  gtSimdBaseJitType;  // SIMD vector base JIT type
    unsigned char  gtSimdSize;         // SIMD vector size in bytes, use 0 for scalar intrinsics
    NamedIntrinsic gtHWIntrinsicId;

public:
    //-----------------------------------------------------------
    // GetRegNumByIdx: Get regNumber of i'th position.
    //
    // Arguments:
    //    idx   -   register position.
    //
    // Return Value:
    //    Returns regNumber assigned to i'th position.
    //
    regNumber GetRegNumByIdx(unsigned idx) const
    {
#ifdef TARGET_ARM64
        assert(idx < MAX_MULTIREG_COUNT);

        if (idx == 0)
        {
            return GetRegNum();
        }

        if (NeedsConsecutiveRegisters())
        {
            assert(IsMultiRegNode());
            return (regNumber)(GetRegNum() + idx);
        }
#endif
        // should only be used to get otherReg
        assert(idx == 1);
        return (regNumber)gtOtherReg;
    }

    //-----------------------------------------------------------
    // SetRegNumByIdx: Set the regNumber for i'th position.
    //
    // Arguments:
    //    reg   -   reg number
    //    idx   -   register position.
    //
    // Return Value:
    //    None.
    //
    void SetRegNumByIdx(regNumber reg, unsigned idx)
    {
#ifdef TARGET_ARM64
        assert(idx < MAX_MULTIREG_COUNT);

        if (idx == 0)
        {
            SetRegNum(reg);
            return;
        }
        if (NeedsConsecutiveRegisters())
        {
            assert(IsMultiRegNode());
            assert(reg == (regNumber)(GetRegNum() + idx));
            return;
        }
#endif
        // should only be used to set otherReg
        assert(idx == 1);
        gtOtherReg = (regNumberSmall)reg;
    }

    GenTreeFlags GetRegSpillFlagByIdx(unsigned idx) const
    {
        return GetMultiRegSpillFlagsByIdx(gtSpillFlags, idx);
    }

    void SetRegSpillFlagByIdx(GenTreeFlags flags, unsigned idx)
    {
        gtSpillFlags = SetMultiRegSpillFlagsByIdx(gtSpillFlags, flags, idx);
    }

    CorInfoType GetAuxiliaryJitType() const
    {
        return (CorInfoType)gtAuxiliaryJitType;
    }

    void SetAuxiliaryJitType(CorInfoType auxiliaryJitType)
    {
        gtAuxiliaryJitType = (unsigned char)auxiliaryJitType;
        assert(gtAuxiliaryJitType == auxiliaryJitType);
    }

    var_types GetAuxiliaryType() const;

    CorInfoType GetSimdBaseJitType() const
    {
        return (CorInfoType)gtSimdBaseJitType;
    }

    CorInfoType GetNormalizedSimdBaseJitType() const
    {
        CorInfoType simdBaseJitType = GetSimdBaseJitType();
        switch (simdBaseJitType)
        {
            case CORINFO_TYPE_NATIVEINT:
            {
#ifdef TARGET_64BIT
                return CORINFO_TYPE_LONG;
#else
                return CORINFO_TYPE_INT;
#endif
            }

            case CORINFO_TYPE_NATIVEUINT:
            {
#ifdef TARGET_64BIT
                return CORINFO_TYPE_ULONG;
#else
                return CORINFO_TYPE_UINT;
#endif
            }

            default:
                return simdBaseJitType;
        }
    }

    void SetSimdBaseJitType(CorInfoType simdBaseJitType)
    {
        gtSimdBaseJitType = (unsigned char)simdBaseJitType;
        assert(gtSimdBaseJitType == simdBaseJitType);
    }

    var_types GetSimdBaseType() const;

    unsigned char GetSimdSize() const
    {
        return gtSimdSize;
    }

    void SetSimdSize(unsigned simdSize)
    {
        gtSimdSize = (unsigned char)simdSize;
        assert(gtSimdSize == simdSize);
    }

    template <typename... Operands>
    GenTreeJitIntrinsic(genTreeOps    oper,
                        var_types     type,
                        CompAllocator allocator,
                        CorInfoType   simdBaseJitType,
                        unsigned      simdSize,
                        Operands... operands)
        : GenTreeMultiOp(oper, type, allocator, gtInlineOperands DEBUGARG(false), operands...)
        , gtOtherReg(REG_NA)
        , gtSpillFlags(0)
        , gtAuxiliaryJitType(CORINFO_TYPE_UNDEF)
        , gtSimdBaseJitType((unsigned char)simdBaseJitType)
        , gtSimdSize((unsigned char)simdSize)
        , gtHWIntrinsicId(NI_Illegal)
    {
        assert(gtSimdBaseJitType == simdBaseJitType);
        assert(gtSimdSize == simdSize);
    }

#if DEBUGGABLE_GENTREE
    GenTreeJitIntrinsic() : GenTreeMultiOp()
    {
    }
#endif

protected:
    GenTreeJitIntrinsic(genTreeOps             oper,
                        var_types              type,
                        IntrinsicNodeBuilder&& nodeBuilder,
                        CorInfoType            simdBaseJitType,
                        unsigned               simdSize)
        : GenTreeMultiOp(oper,
                         type,
                         nodeBuilder.GetBuiltOperands(),
                         nodeBuilder.GetOperandCount(),
                         gtInlineOperands DEBUGARG(false))
        , gtOtherReg(REG_NA)
        , gtSpillFlags(0)
        , gtAuxiliaryJitType(CORINFO_TYPE_UNDEF)
        , gtSimdBaseJitType((unsigned char)simdBaseJitType)
        , gtSimdSize((unsigned char)simdSize)
        , gtHWIntrinsicId(NI_Illegal)
    {
        assert(gtSimdBaseJitType == simdBaseJitType);
        assert(gtSimdSize == simdSize);
    }

public:
    bool isSIMD() const
    {
        return gtSimdSize != 0;
    }
};

#ifdef FEATURE_HW_INTRINSICS

struct GenTreeHWIntrinsic : public GenTreeJitIntrinsic
{
    GenTreeHWIntrinsic(var_types              type,
                       IntrinsicNodeBuilder&& nodeBuilder,
                       NamedIntrinsic         hwIntrinsicID,
                       CorInfoType            simdBaseJitType,
                       unsigned               simdSize)
        : GenTreeJitIntrinsic(GT_HWINTRINSIC, type, std::move(nodeBuilder), simdBaseJitType, simdSize)
    {
        Initialize(hwIntrinsicID);
    }

    template <typename... Operands>
    GenTreeHWIntrinsic(var_types      type,
                       CompAllocator  allocator,
                       NamedIntrinsic hwIntrinsicID,
                       CorInfoType    simdBaseJitType,
                       unsigned       simdSize,
                       Operands... operands)
        : GenTreeJitIntrinsic(GT_HWINTRINSIC, type, allocator, simdBaseJitType, simdSize, operands...)
    {
        Initialize(hwIntrinsicID);
    }

#if DEBUGGABLE_GENTREE
    GenTreeHWIntrinsic() : GenTreeJitIntrinsic()
    {
    }
#endif

    bool OperIsMemoryLoad(GenTree** pAddr = nullptr) const;
    bool OperIsMemoryStore(GenTree** pAddr = nullptr) const;
    bool OperIsMemoryLoadOrStore() const;
    bool OperIsMemoryStoreOrBarrier() const;
    bool OperIsEmbBroadcastCompatible() const;
    bool OperIsBroadcastScalar() const;
    bool OperIsCreateScalarUnsafe() const;
    bool OperIsBitwiseHWIntrinsic() const;

    bool OperRequiresAsgFlag() const;
    bool OperRequiresCallFlag() const;
    bool OperRequiresGlobRefFlag() const;

    unsigned GetResultOpNumForRmwIntrinsic(GenTree* use, GenTree* op1, GenTree* op2, GenTree* op3);
    uint8_t GetTernaryControlByte(GenTreeHWIntrinsic* second) const;

    ClassLayout* GetLayout(Compiler* compiler) const;

    NamedIntrinsic GetHWIntrinsicId() const;

    //---------------------------------------------------------------------------------------
    // ChangeHWIntrinsicId: Change the intrinsic id for this node.
    //
    // This method just sets the intrinsic id, asserting that the new intrinsic
    // has the same number of operands as the old one, optionally setting some of
    // the new operands. Intrinsics with an unknown number of operands are exempt
    // from the "do I have the same number of operands" check however, so this method must
    // be used with care. Use "ResetHWIntrinsicId" if you need to fully reconfigure
    // the node for a different intrinsic, with a possibly different number of operands.
    //
    // Arguments:
    //    intrinsicId - the new intrinsic id for the node
    //    operands    - optional operands to set while changing the id
    //
    // Notes:
    //    It is the caller's responsibility to update side effect flags.
    //
    template <typename... Operands>
    void ChangeHWIntrinsicId(NamedIntrinsic intrinsicId, Operands... operands)
    {
        const size_t OperandCount = sizeof...(Operands);
        assert(OperandCount <= GetOperandCount());

        SetHWIntrinsicId(intrinsicId);

        GenTree*  operandsArray[OperandCount + 1]{operands...};
        GenTree** operandsStore = GetOperandArray();

        for (size_t i = 0; i < OperandCount; i++)
        {
            operandsStore[i] = operandsArray[i];
        }
    }

    //---------------------------------------------------------------------------------------
    // ResetHWIntrinsicId: Reset the intrinsic id for this node.
    //
    // This method resets the intrinsic id, fully reconfiguring the node. It must
    // be supplied with all the operands the new node needs, and can allocate a
    // new dynamic array if the operands do not fit into in an inline one, in which
    // case a compiler argument is used to get the memory allocator.
    //
    // This method is similar to "ChangeHWIntrinsicId" but is more versatile and
    // thus more expensive. Use it when you need to bash to an intrinsic id with
    // a different number of operands than what the original node had, or, which
    // is equivalent, when you do not know the original number of operands.
    //
    // Arguments:
    //    intrinsicId - the new intrinsic id for the node
    //    compiler    - compiler to allocate memory with, can be "nullptr" if the
    //                  number of new operands does not exceed the length of the
    //                  inline array (so, there are 2 or fewer of them)
    //    operands    - *all* operands for the new node
    //
    // Notes:
    //    It is the caller's responsibility to update side effect flags.
    //
    template <typename... Operands>
    void ResetHWIntrinsicId(NamedIntrinsic intrinsicId, Compiler* compiler, Operands... operands)
    {
        const size_t NewOperandCount = sizeof...(Operands);
        assert((compiler != nullptr) || (NewOperandCount <= ArrLen(gtInlineOperands)));

        ResetOperandArray(NewOperandCount, compiler, gtInlineOperands, ArrLen(gtInlineOperands));
        ChangeHWIntrinsicId(intrinsicId, operands...);
    }

    void ResetHWIntrinsicId(NamedIntrinsic intrinsicId, GenTree* op1, GenTree* op2)
    {
        ResetHWIntrinsicId(intrinsicId, static_cast<Compiler*>(nullptr), op1, op2);
    }

    void ResetHWIntrinsicId(NamedIntrinsic intrinsicId, GenTree* op1)
    {
        ResetHWIntrinsicId(intrinsicId, static_cast<Compiler*>(nullptr), op1);
    }

    void ResetHWIntrinsicId(NamedIntrinsic intrinsicId)
    {
        ResetHWIntrinsicId(intrinsicId, static_cast<Compiler*>(nullptr));
    }

    static bool Equals(GenTreeHWIntrinsic* op1, GenTreeHWIntrinsic* op2);

    genTreeOps HWOperGet() const;

private:
    void SetHWIntrinsicId(NamedIntrinsic intrinsicId);

    void Initialize(NamedIntrinsic intrinsicId);
};
#endif // FEATURE_HW_INTRINSICS

// GenTreeVecCon -- vector  constant (GT_CNS_VEC)
//
struct GenTreeVecCon : public GenTree
{
    union {
        simd8_t  gtSimd8Val;
        simd12_t gtSimd12Val;
        simd16_t gtSimd16Val;

#if defined(TARGET_XARCH)
        simd32_t gtSimd32Val;
        simd64_t gtSimd64Val;
#endif // TARGET_XARCH

        simd_t gtSimdVal;
    };

#if defined(FEATURE_HW_INTRINSICS)
    static unsigned ElementCount(unsigned simdSize, var_types simdBaseType);

    template <typename simdTypename>
    static bool IsHWIntrinsicCreateConstant(GenTreeHWIntrinsic* node, simdTypename& simdVal)
    {
        NamedIntrinsic intrinsic    = node->GetHWIntrinsicId();
        var_types      simdType     = node->TypeGet();
        var_types      simdBaseType = node->GetSimdBaseType();
        unsigned       simdSize     = node->GetSimdSize();

        size_t argCnt    = node->GetOperandCount();
        size_t cnsArgCnt = 0;

        switch (intrinsic)
        {
            case NI_Vector128_Create:
            case NI_Vector128_CreateScalar:
            case NI_Vector128_CreateScalarUnsafe:
#if defined(TARGET_XARCH)
            case NI_Vector256_Create:
            case NI_Vector512_Create:
            case NI_Vector256_CreateScalar:
            case NI_Vector512_CreateScalar:
            case NI_Vector256_CreateScalarUnsafe:
            case NI_Vector512_CreateScalarUnsafe:
#elif defined(TARGET_ARM64)
            case NI_Vector64_Create:
            case NI_Vector64_CreateScalar:
            case NI_Vector64_CreateScalarUnsafe:
#endif
            {
                // Zero out the simdVal
                simdVal = {};

                // These intrinsics are meant to set the same value to every element.
                if ((argCnt == 1) && HandleArgForHWIntrinsicCreate<simdTypename>(node->Op(1), 0, simdVal, simdBaseType))
                {
// CreateScalar leaves the upper bits as zero

#if defined(TARGET_XARCH)
                    if ((intrinsic != NI_Vector128_CreateScalar) && (intrinsic != NI_Vector256_CreateScalar) &&
                        (intrinsic != NI_Vector512_CreateScalar))
#elif defined(TARGET_ARM64)
                    if ((intrinsic != NI_Vector64_CreateScalar) && (intrinsic != NI_Vector128_CreateScalar))
#endif
                    {
                        // Now assign the rest of the arguments.
                        for (unsigned i = 1; i < ElementCount(simdSize, simdBaseType); i++)
                        {
                            HandleArgForHWIntrinsicCreate<simdTypename>(node->Op(1), i, simdVal, simdBaseType);
                        }
                    }

                    cnsArgCnt = 1;
                }
                else
                {
                    for (unsigned i = 1; i <= argCnt; i++)
                    {
                        if (HandleArgForHWIntrinsicCreate<simdTypename>(node->Op(i), i - 1, simdVal, simdBaseType))
                        {
                            cnsArgCnt++;
                        }
                    }
                }

                assert((argCnt == 1) || (argCnt == ElementCount(simdSize, simdBaseType)));
                return argCnt == cnsArgCnt;
            }

            default:
            {
                return false;
            }
        }
    }

    //----------------------------------------------------------------------------------------------
    // HandleArgForHWIntrinsicCreate: Processes an argument for the GenTreeVecCon::IsHWIntrinsicCreateConstant method
    //
    //  Arguments:
    //     arg       - The argument to process
    //     argIdx    - The index of the argument being processed
    //     simdVal - The vector constant being constructed
    //     baseType  - The base type of the vector constant
    //
    //  Returns:
    //     true if arg was a constant; otherwise, false
    template <typename simdTypename>
    static bool HandleArgForHWIntrinsicCreate(GenTree* arg, int argIdx, simdTypename& simdVal, var_types baseType)
    {
        switch (baseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                if (arg->IsCnsIntOrI())
                {
                    simdVal.i8[argIdx] = static_cast<int8_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the constant to have been already zeroed
                    assert(simdVal.i8[argIdx] == 0);
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                if (arg->IsCnsIntOrI())
                {
                    simdVal.i16[argIdx] = static_cast<int16_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the constant to have been already zeroed
                    assert(simdVal.i16[argIdx] == 0);
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                if (arg->IsCnsIntOrI())
                {
                    simdVal.i32[argIdx] = static_cast<int32_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
                else
                {
                    // We expect the constant to have been already zeroed
                    assert(simdVal.i32[argIdx] == 0);
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
#if defined(TARGET_64BIT)
                if (arg->IsCnsIntOrI())
                {
                    simdVal.i64[argIdx] = static_cast<int64_t>(arg->AsIntCon()->gtIconVal);
                    return true;
                }
#else
                if (arg->OperIsLong() && arg->AsOp()->gtOp1->IsCnsIntOrI() && arg->AsOp()->gtOp2->IsCnsIntOrI())
                {
                    // 32-bit targets will decompose GT_CNS_LNG into two GT_CNS_INT
                    // We need to reconstruct the 64-bit value in order to handle this

                    INT64 gtLconVal = arg->AsOp()->gtOp2->AsIntCon()->gtIconVal;
                    gtLconVal <<= 32;
                    gtLconVal |= arg->AsOp()->gtOp1->AsIntCon()->gtIconVal;

                    simdVal.i64[argIdx] = gtLconVal;
                    return true;
                }
#endif // TARGET_64BIT
                else
                {
                    // We expect the constant to have been already zeroed
                    assert(simdVal.i64[argIdx] == 0);
                }
                break;
            }

            case TYP_FLOAT:
            {
                if (arg->IsCnsFltOrDbl())
                {
                    simdVal.f32[argIdx] = static_cast<float>(arg->AsDblCon()->DconValue());
                    return true;
                }
                else
                {
                    // We expect the constant to have been already zeroed
                    // We check against the i32, rather than f32, to account for -0.0
                    assert(simdVal.i32[argIdx] == 0);
                }
                break;
            }

            case TYP_DOUBLE:
            {
                if (arg->IsCnsFltOrDbl())
                {
                    simdVal.f64[argIdx] = static_cast<double>(arg->AsDblCon()->DconValue());
                    return true;
                }
                else
                {
                    // We expect the constant to have been already zeroed
                    // We check against the i64, rather than f64, to account for -0.0
                    assert(simdVal.i64[argIdx] == 0);
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return false;
    }

#endif // FEATURE_HW_INTRINSICS

    bool IsAllBitsSet() const
    {
        switch (gtType)
        {
#if defined(FEATURE_SIMD)
            case TYP_SIMD8:
            {
                return gtSimd8Val.IsAllBitsSet();
            }

            case TYP_SIMD12:
            {
                return gtSimd12Val.IsAllBitsSet();
            }

            case TYP_SIMD16:
            {
                return gtSimd16Val.IsAllBitsSet();
            }

#if defined(TARGET_XARCH)
            case TYP_SIMD32:
            {
                return gtSimd32Val.IsAllBitsSet();
            }

            case TYP_SIMD64:
            {
                return gtSimd64Val.IsAllBitsSet();
            }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

            default:
            {
                unreached();
            }
        }
    }

    static bool Equals(const GenTreeVecCon* left, const GenTreeVecCon* right)
    {
        var_types gtType = left->TypeGet();

        if (gtType != right->TypeGet())
        {
            return false;
        }

        switch (gtType)
        {
#if defined(FEATURE_SIMD)
            case TYP_SIMD8:
            {
                return left->gtSimd8Val == right->gtSimd8Val;
            }

            case TYP_SIMD12:
            {
                return left->gtSimd12Val == right->gtSimd12Val;
            }

            case TYP_SIMD16:
            {
                return left->gtSimd16Val == right->gtSimd16Val;
            }

#if defined(TARGET_XARCH)
            case TYP_SIMD32:
            {
                return left->gtSimd32Val == right->gtSimd32Val;
            }

            case TYP_SIMD64:
            {
                return left->gtSimd64Val == right->gtSimd64Val;
            }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

            default:
            {
                unreached();
            }
        }
    }

    bool IsZero() const
    {
        switch (gtType)
        {
#if defined(FEATURE_SIMD)
            case TYP_SIMD8:
            {
                return gtSimd8Val.IsZero();
            }

            case TYP_SIMD12:
            {
                return gtSimd12Val.IsZero();
            }

            case TYP_SIMD16:
            {
                return gtSimd16Val.IsZero();
            }

#if defined(TARGET_XARCH)
            case TYP_SIMD32:
            {
                return gtSimd32Val.IsZero();
            }

            case TYP_SIMD64:
            {
                return gtSimd64Val.IsZero();
            }
#endif // TARGET_XARCH
#endif // FEATURE_SIMD

            default:
            {
                unreached();
            }
        }
    }

    GenTreeVecCon(var_types type) : GenTree(GT_CNS_VEC, type)
    {
        assert(varTypeIsSIMD(type));

        // Some uses of GenTreeVecCon do not specify all bits in the vector they are using but failing to zero out the
        // buffer will cause determinism issues with the compiler.
        memset(&gtSimdVal, 0, sizeof(gtSimdVal));

#if defined(TARGET_XARCH)
        assert(sizeof(simd_t) == sizeof(simd64_t));
#else
        assert(sizeof(simd_t) == sizeof(simd16_t));
#endif
    }

#if DEBUGGABLE_GENTREE
    GenTreeVecCon() : GenTree()
    {
    }
#endif
};

// GenTreeIndexAddr: Given an array object and an index, checks that the index is within the bounds of the array if
//                   necessary and produces the address of the value at that index of the array.
//
struct GenTreeIndexAddr : public GenTreeOp
{
    GenTree*& Arr()
    {
        return gtOp1;
    }
    GenTree*& Index()
    {
        return gtOp2;
    }

    CORINFO_CLASS_HANDLE gtStructElemClass; // If the element type is a struct, this is the struct type.

    BasicBlock* gtIndRngFailBB; // Basic block to jump to for array-index-out-of-range

    var_types gtElemType;   // The element type of the array.
    unsigned  gtElemSize;   // size of elements in the array
    unsigned  gtLenOffset;  // The offset from the array's base address to its length.
    unsigned  gtElemOffset; // The offset from the array's base address to its first element.

    GenTreeIndexAddr(GenTree*             arr,
                     GenTree*             ind,
                     var_types            elemType,
                     CORINFO_CLASS_HANDLE structElemClass,
                     unsigned             elemSize,
                     unsigned             lenOffset,
                     unsigned             elemOffset,
                     bool                 boundsCheck)
        : GenTreeOp(GT_INDEX_ADDR, TYP_BYREF, arr, ind)
        , gtStructElemClass(structElemClass)
        , gtIndRngFailBB(nullptr)
        , gtElemType(elemType)
        , gtElemSize(elemSize)
        , gtLenOffset(lenOffset)
        , gtElemOffset(elemOffset)
    {
        assert(!varTypeIsStruct(elemType) || (structElemClass != NO_CLASS_HANDLE));

        if (boundsCheck)
        {
            // Do bounds check
            gtFlags |= GTF_INX_RNGCHK;
        }

        gtFlags |= GTF_EXCEPT | GTF_GLOB_REF;
    }

#if DEBUGGABLE_GENTREE
    GenTreeIndexAddr() : GenTreeOp()
    {
    }
#endif

    bool IsBoundsChecked() const
    {
        return (gtFlags & GTF_INX_RNGCHK) != 0;
    }

    bool IsNotNull() const
    {
        return IsBoundsChecked() || ((gtFlags & GTF_INX_ADDR_NONNULL) != 0);
    }
};

// GenTreeArrAddr - GT_ARR_ADDR, carries information about the array type from morph to VN.
//                  This node is just a wrapper (similar to GenTreeBox), the real address
//                  expression is contained in its first operand.
//
struct GenTreeArrAddr : GenTreeUnOp
{
private:
    CORINFO_CLASS_HANDLE m_elemClassHandle; // The array element class. Currently only used for arrays of TYP_STRUCT.
    var_types            m_elemType;        // The normalized (TYP_SIMD != TYP_STRUCT) array element type.
    uint8_t              m_firstElemOffset; // Offset to the first element of the array.

public:
    GenTreeArrAddr(GenTree* addr, var_types elemType, CORINFO_CLASS_HANDLE elemClassHandle, uint8_t firstElemOffset)
        : GenTreeUnOp(GT_ARR_ADDR, TYP_BYREF, addr DEBUGARG(/* largeNode */ false))
        , m_elemClassHandle(elemClassHandle)
        , m_elemType(elemType)
        , m_firstElemOffset(firstElemOffset)
    {
        assert(addr->TypeIs(TYP_BYREF));
        assert(((elemType == TYP_STRUCT) && (elemClassHandle != NO_CLASS_HANDLE)) ||
               (elemClassHandle == NO_CLASS_HANDLE));
    }

#if DEBUGGABLE_GENTREE
    GenTreeArrAddr() : GenTreeUnOp()
    {
    }
#endif

    GenTree*& Addr()
    {
        return gtOp1;
    }

    CORINFO_CLASS_HANDLE GetElemClassHandle() const
    {
        return m_elemClassHandle;
    }

    var_types GetElemType() const
    {
        return m_elemType;
    }

    uint8_t GetFirstElemOffset() const
    {
        return m_firstElemOffset;
    }

    void ParseArrayAddress(Compiler* comp, GenTree** pArr, ValueNum* pInxVN);

private:
    static void ParseArrayAddressWork(GenTree*        tree,
                                      Compiler*       comp,
                                      target_ssize_t  inputMul,
                                      GenTree**       pArr,
                                      ValueNum*       pInxVN,
                                      target_ssize_t* pOffset);
};

// GenTreeArrCommon -- A parent class for GenTreeArrLen, GenTreeMDArr
// (so, accessing array meta-data for either single-dimensional or multi-dimensional arrays).
// Mostly just a convenience to use the ArrRef() accessor.
//
struct GenTreeArrCommon : public GenTreeUnOp
{
    GenTree*& ArrRef() // the array address node
    {
        return gtOp1;
    }

    GenTreeArrCommon(genTreeOps oper, var_types type, GenTree* arrRef) : GenTreeUnOp(oper, type, arrRef)
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeArrCommon() : GenTreeUnOp()
    {
    }
#endif
};

// GenTreeArrLen (GT_ARR_LENGTH) -- single-dimension (SZ) array length. Used for `array.Length`.
//
struct GenTreeArrLen : public GenTreeArrCommon
{
    GenTree*& ArrRef() // the array address node
    {
        return gtOp1;
    }

private:
    int gtArrLenOffset; // constant to add to "ArrRef()" to get the address of the array length.

public:
    int ArrLenOffset() const
    {
        return gtArrLenOffset;
    }

    GenTreeArrLen(var_types type, GenTree* arrRef, int lenOffset)
        : GenTreeArrCommon(GT_ARR_LENGTH, type, arrRef), gtArrLenOffset(lenOffset)
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeArrLen() : GenTreeArrCommon()
    {
    }
#endif
};

// GenTreeMDArr (GT_MDARR_LENGTH, GT_MDARR_LOWER_BOUND) -- multi-dimension (MD) array length
// or lower bound for a dimension. Used for `array.GetLength(n)`, `array.GetLowerBound(n)`.
//
struct GenTreeMDArr : public GenTreeArrCommon
{
private:
    unsigned gtDim;  // array dimension of this array length
    unsigned gtRank; // array rank of the array

public:
    unsigned Dim() const
    {
        return gtDim;
    }

    unsigned Rank() const
    {
        return gtRank;
    }

    GenTreeMDArr(genTreeOps oper, GenTree* arrRef, unsigned dim, unsigned rank)
        : GenTreeArrCommon(oper, TYP_INT, arrRef), gtDim(dim), gtRank(rank)
    {
        assert(OperIs(GT_MDARR_LENGTH, GT_MDARR_LOWER_BOUND));
    }

#if DEBUGGABLE_GENTREE
    GenTreeMDArr() : GenTreeArrCommon()
    {
    }
#endif
};

// This takes:
// - a length value
// - an index value, and
// - the label to jump to if the index is out of range.
// - the "kind" of the throw block to branch to on failure
// It generates no result.
//
struct GenTreeBoundsChk : public GenTreeOp
{
    BasicBlock*     gtIndRngFailBB; // Basic block to jump to for index-out-of-range
    SpecialCodeKind gtThrowKind;    // Kind of throw block to branch to on failure

    // Store some information about the array element type that was in the GT_INDEX_ADDR node before morphing.
    // Note that this information is also stored in the ARR_ADDR node of the morphed tree, but that can be hard
    // to find.
    var_types gtInxType; // The array element type.

    GenTreeBoundsChk(GenTree* index, GenTree* length, SpecialCodeKind kind)
        : GenTreeOp(GT_BOUNDS_CHECK, TYP_VOID, index, length)
        , gtIndRngFailBB(nullptr)
        , gtThrowKind(kind)
        , gtInxType(TYP_UNKNOWN)
    {
        gtFlags |= GTF_EXCEPT;
    }
#if DEBUGGABLE_GENTREE
    GenTreeBoundsChk() : GenTreeOp()
    {
    }
#endif

    // If this check is against GT_ARR_LENGTH or GT_MDARR_LENGTH, returns array reference, else nullptr.
    GenTree* GetArray() const
    {
        if (GetArrayLength()->OperIsArrLength())
        {
            return GetArrayLength()->GetArrLengthArrRef();
        }
        else
        {
            return nullptr;
        }
    }

    // The index expression.
    GenTree* GetIndex() const
    {
        return gtOp1;
    }

    // An expression for the length.
    GenTree* GetArrayLength() const
    {
        return gtOp2;
    }
};

// GenTreeArrElem - bounds checked address (byref) of a general array element,
//    for multidimensional arrays, or 1-d arrays with non-zero lower bounds.
//
struct GenTreeArrElem : public GenTree
{
    GenTree* gtArrObj;

#define GT_ARR_MAX_RANK 3
    GenTree*      gtArrInds[GT_ARR_MAX_RANK]; // Indices
    unsigned char gtArrRank;                  // Rank of the array

    unsigned char gtArrElemSize; // !!! Caution, this is an "unsigned char", it is used only
                                 // on the optimization path of array intrinsics.
                                 // It stores the size of array elements WHEN it can fit
                                 // into an "unsigned char".
                                 // This has caused VSW 571394.

    // Requires that "inds" is a pointer to an array of "rank" nodes for the indices.
    GenTreeArrElem(var_types type, GenTree* arr, unsigned char rank, unsigned char elemSize, GenTree** inds)
        : GenTree(GT_ARR_ELEM, type), gtArrObj(arr), gtArrRank(rank), gtArrElemSize(elemSize)
    {
        assert(rank <= ArrLen(gtArrInds));
        gtFlags |= (arr->gtFlags & GTF_ALL_EFFECT);
        for (unsigned char i = 0; i < rank; i++)
        {
            gtArrInds[i] = inds[i];
            gtFlags |= (inds[i]->gtFlags & GTF_ALL_EFFECT);
        }
        gtFlags |= GTF_EXCEPT;
    }
#if DEBUGGABLE_GENTREE
    GenTreeArrElem() : GenTree()
    {
    }
#endif
};

/* gtAddrMode -- Target-specific canonicalized addressing expression (GT_LEA) */

struct GenTreeAddrMode : public GenTreeOp
{
    // Address is Base + Index*Scale + Offset.
    // These are the legal patterns:
    //
    //      Base                                // Base != nullptr && Index == nullptr && Scale == 0 && Offset == 0
    //      Base + Index*Scale                  // Base != nullptr && Index != nullptr && Scale != 0 && Offset == 0
    //      Base + Offset                       // Base != nullptr && Index == nullptr && Scale == 0 && Offset != 0
    //      Base + Index*Scale + Offset         // Base != nullptr && Index != nullptr && Scale != 0 && Offset != 0
    //             Index*Scale                  // Base == nullptr && Index != nullptr && Scale >  1 && Offset == 0
    //             Index*Scale + Offset         // Base == nullptr && Index != nullptr && Scale >  1 && Offset != 0
    //                           Offset         // Base == nullptr && Index == nullptr && Scale == 0 && Offset != 0
    //
    // So, for example:
    //      1. Base + Index is legal with Scale==1
    //      2. If Index is null, Scale should be zero (or uninitialized / unused)
    //      3. If Scale==1, then we should have "Base" instead of "Index*Scale", and "Base + Offset" instead of
    //         "Index*Scale + Offset".

    // First operand is base address/pointer
    bool HasBase() const
    {
        return gtOp1 != nullptr;
    }
    GenTree*& Base()
    {
        return gtOp1;
    }

    void SetBase(GenTree* base)
    {
        gtOp1 = base;
    }

    // Second operand is scaled index value
    bool HasIndex() const
    {
        return gtOp2 != nullptr;
    }
    GenTree*& Index()
    {
        return gtOp2;
    }

    void SetIndex(GenTree* index)
    {
        gtOp2 = index;
    }

    unsigned GetScale() const
    {
        return gtScale;
    }

    void SetScale(unsigned scale)
    {
        gtScale = scale;
    }

    int Offset()
    {
        return static_cast<int>(gtOffset);
    }

    void SetOffset(int offset)
    {
        gtOffset = offset;
    }

    unsigned gtScale; // The scale factor

private:
    ssize_t gtOffset; // The offset to add

public:
    GenTreeAddrMode(var_types type, GenTree* base, GenTree* index, unsigned scale, ssize_t offset)
        : GenTreeOp(GT_LEA, type, base, index)
    {
        assert(base != nullptr || index != nullptr);
        gtScale  = scale;
        gtOffset = offset;
    }
#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    // Used only for GenTree::GetVtableForOper()
    GenTreeAddrMode() : GenTreeOp()
    {
    }
#endif
};

// Indir is just an op, no additional data, but some additional abstractions
struct GenTreeIndir : public GenTreeOp
{
    // The address for the indirection.
    GenTree*& Addr()
    {
        return gtOp1;
    }

    void SetAddr(GenTree* addr)
    {
        assert(varTypeIsI(addr));
        gtOp1 = addr;
    }

    GenTree*& Data()
    {
        assert(OperIs(GT_STOREIND) || OperIsStoreBlk() || OperIsAtomicOp());
        return gtOp2;
    }

    bool     HasBase();
    bool     HasIndex();
    GenTree* Base();
    GenTree* Index();
    unsigned Scale();
    ssize_t  Offset();

    unsigned Size() const;

    GenTreeIndir(genTreeOps oper, var_types type, GenTree* addr, GenTree* data) : GenTreeOp(oper, type, addr, data)
    {
    }

    // True if this indirection is a volatile memory operation.
    bool IsVolatile() const
    {
        return (gtFlags & GTF_IND_VOLATILE) != 0;
    }

    // True if this indirection is an unaligned memory operation.
    bool IsUnaligned() const
    {
        return (gtFlags & GTF_IND_UNALIGNED) != 0;
    }

    // True if this indirection is invariant.
    bool IsInvariantLoad() const
    {
        bool isInvariant = (gtFlags & GTF_IND_INVARIANT) != 0;
        assert(!isInvariant || OperIs(GT_IND, GT_BLK));
        return isInvariant;
    }

#if DEBUGGABLE_GENTREE
    // Used only for GenTree::GetVtableForOper()
    GenTreeIndir() : GenTreeOp()
    {
    }
#else
    // Used by XARCH codegen to construct temporary trees to pass to the emitter.
    GenTreeIndir() : GenTreeOp(GT_NOP, TYP_UNDEF)
    {
    }
#endif
};

// gtBlk  -- 'block' (GT_BLK, GT_STORE_BLK).
//
// This is the base type for all of the nodes that represent block or struct
// values.
// Since it can be a store, it includes gtBlkOpKind to specify the type of
// code generation that will be used for the block operation.

struct GenTreeBlk : public GenTreeIndir
{
private:
    ClassLayout* m_layout;

public:
    ClassLayout* GetLayout() const
    {
        return m_layout;
    }

    void SetLayout(ClassLayout* layout)
    {
        assert((layout != nullptr) || OperIs(GT_STORE_DYN_BLK));
        m_layout = layout;
    }

    // The data to be stored (null for GT_BLK)
    GenTree*& Data()
    {
        return gtOp2;
    }
    void SetData(GenTree* dataNode)
    {
        gtOp2 = dataNode;
    }

    // The size of the buffer to be copied.
    unsigned Size() const
    {
        assert((m_layout != nullptr) || OperIs(GT_STORE_DYN_BLK));
        return (m_layout != nullptr) ? m_layout->GetSize() : 0;
    }

    // Instruction selection: during codegen time, what code sequence we will be using
    // to encode this operation.
    enum
    {
        BlkOpKindInvalid,
        BlkOpKindCpObjUnroll,
#ifdef TARGET_XARCH
        BlkOpKindCpObjRepInstr,
#endif
#ifndef TARGET_X86
        BlkOpKindHelper,
#endif
#ifdef TARGET_XARCH
        BlkOpKindRepInstr,
#endif
        BlkOpKindLoop,
        BlkOpKindUnroll,
        BlkOpKindUnrollMemmove,
    } gtBlkOpKind;

#ifndef JIT32_GCENCODER
    bool gtBlkOpGcUnsafe;
#endif

    bool ContainsReferences()
    {
        return (m_layout != nullptr) && m_layout->HasGCPtr();
    }

    bool IsOnHeapAndContainsReferences()
    {
        return ContainsReferences() && !Addr()->OperIs(GT_LCL_ADDR);
    }

    bool IsZeroingGcPointersOnHeap()
    {
        return OperIs(GT_STORE_BLK) && Data()->IsIntegralConst(0) && IsOnHeapAndContainsReferences();
    }

    GenTreeBlk(genTreeOps oper, var_types type, GenTree* addr, ClassLayout* layout)
        : GenTreeIndir(oper, type, addr, nullptr)
    {
        Initialize(layout);
    }

    GenTreeBlk(genTreeOps oper, var_types type, GenTree* addr, GenTree* data, ClassLayout* layout)
        : GenTreeIndir(oper, type, addr, data)
    {
        if (data->IsIntegralConst(0))
        {
            data->gtFlags |= GTF_DONT_CSE;
        }
        Initialize(layout);
    }

    void Initialize(ClassLayout* layout)
    {
        assert(OperIsBlk(OperGet()) && ((layout != nullptr) || OperIs(GT_STORE_DYN_BLK)));
        assert((layout == nullptr) || (layout->GetSize() != 0));

        m_layout    = layout;
        gtBlkOpKind = BlkOpKindInvalid;
#ifndef JIT32_GCENCODER
        gtBlkOpGcUnsafe = false;
#endif
    }

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    GenTreeBlk() : GenTreeIndir()
    {
    }
#endif // DEBUGGABLE_GENTREE
};

// GenTreeStoreDynBlk  -- 'dynamic block store' (GT_STORE_DYN_BLK).
//
// This node is used to represent stores that have a dynamic size - the "cpblk" and "initblk"
// IL instructions are implemented with it. Note that such stores assume the input has no GC
// pointers in it, and as such do not ever use write barriers.
//
// The "Data()" member of this node will either be a "dummy" IND(struct) node, for "cpblk", or
// the zero constant/INIT_VAL for "initblk".
//
struct GenTreeStoreDynBlk : public GenTreeBlk
{
public:
    GenTree* gtDynamicSize;

    GenTreeStoreDynBlk(GenTree* dstAddr, GenTree* data, GenTree* dynamicSize)
        : GenTreeBlk(GT_STORE_DYN_BLK, TYP_VOID, dstAddr, data, nullptr), gtDynamicSize(dynamicSize)
    {
        gtFlags |= dynamicSize->gtFlags & GTF_ALL_EFFECT;
    }

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    GenTreeStoreDynBlk() : GenTreeBlk()
    {
    }
#endif // DEBUGGABLE_GENTREE
};

// Read-modify-write status of a RMW memory op rooted at a storeInd
enum RMWStatus
{
    STOREIND_RMW_STATUS_UNKNOWN, // RMW status of storeInd unknown
                                 // Default status unless modified by IsRMWMemOpRootedAtStoreInd()

    // One of these denote storeind is a RMW memory operation.
    STOREIND_RMW_DST_IS_OP1, // StoreInd is known to be a RMW memory op and dst candidate is op1
    STOREIND_RMW_DST_IS_OP2, // StoreInd is known to be a RMW memory op and dst candidate is op2

    // One of these denote the reason for storeind is marked as non-RMW operation
    STOREIND_RMW_UNSUPPORTED_ADDR, // Addr mode is not yet supported for RMW memory
    STOREIND_RMW_UNSUPPORTED_OPER, // Operation is not supported for RMW memory
    STOREIND_RMW_UNSUPPORTED_TYPE, // Type is not supported for RMW memory
    STOREIND_RMW_INDIR_UNEQUAL     // Indir to read value is not equivalent to indir that writes the value
};

#ifdef DEBUG
inline const char* RMWStatusDescription(RMWStatus status)
{
    switch (status)
    {
        case STOREIND_RMW_STATUS_UNKNOWN:
            return "RMW status unknown";
        case STOREIND_RMW_DST_IS_OP1:
            return "dst candidate is op1";
        case STOREIND_RMW_DST_IS_OP2:
            return "dst candidate is op2";
        case STOREIND_RMW_UNSUPPORTED_ADDR:
            return "address mode is not supported";
        case STOREIND_RMW_UNSUPPORTED_OPER:
            return "oper is not supported";
        case STOREIND_RMW_UNSUPPORTED_TYPE:
            return "type is not supported";
        case STOREIND_RMW_INDIR_UNEQUAL:
            return "read indir is not equivalent to write indir";
        default:
            unreached();
    }
}
#endif

// StoreInd is just a BinOp, with additional RMW status
struct GenTreeStoreInd : public GenTreeIndir
{
#if !CPU_LOAD_STORE_ARCH
    // The below flag is set and used during lowering
    RMWStatus gtRMWStatus;

    bool IsRMWStatusUnknown()
    {
        return gtRMWStatus == STOREIND_RMW_STATUS_UNKNOWN;
    }
    bool IsNonRMWMemoryOp()
    {
        return gtRMWStatus == STOREIND_RMW_UNSUPPORTED_ADDR || gtRMWStatus == STOREIND_RMW_UNSUPPORTED_OPER ||
               gtRMWStatus == STOREIND_RMW_UNSUPPORTED_TYPE || gtRMWStatus == STOREIND_RMW_INDIR_UNEQUAL;
    }
    bool IsRMWMemoryOp()
    {
        return gtRMWStatus == STOREIND_RMW_DST_IS_OP1 || gtRMWStatus == STOREIND_RMW_DST_IS_OP2;
    }
    bool IsRMWDstOp1()
    {
        return gtRMWStatus == STOREIND_RMW_DST_IS_OP1;
    }
    bool IsRMWDstOp2()
    {
        return gtRMWStatus == STOREIND_RMW_DST_IS_OP2;
    }
#endif //! CPU_LOAD_STORE_ARCH

    RMWStatus GetRMWStatus()
    {
#if !CPU_LOAD_STORE_ARCH
        return gtRMWStatus;
#else
        return STOREIND_RMW_STATUS_UNKNOWN;
#endif
    }

    void SetRMWStatusDefault()
    {
#if !CPU_LOAD_STORE_ARCH
        gtRMWStatus = STOREIND_RMW_STATUS_UNKNOWN;
#endif
    }

    void SetRMWStatus(RMWStatus status)
    {
#if !CPU_LOAD_STORE_ARCH
        gtRMWStatus = status;
#endif
    }

    GenTree*& Data()
    {
        return gtOp2;
    }

    GenTreeStoreInd(var_types type, GenTree* destPtr, GenTree* data) : GenTreeIndir(GT_STOREIND, type, destPtr, data)
    {
        SetRMWStatusDefault();
    }

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    // Used only for GenTree::GetVtableForOper()
    GenTreeStoreInd() : GenTreeIndir()
    {
        SetRMWStatusDefault();
    }
#endif
};

struct GenTreeCmpXchg : public GenTreeIndir
{
private:
    GenTree* m_comparand;

public:
    GenTreeCmpXchg(var_types type, GenTree* loc, GenTree* val, GenTree* comparand)
        : GenTreeIndir(GT_CMPXCHG, type, loc, val), m_comparand(comparand)
    {
        gtFlags |= comparand->gtFlags & GTF_ALL_EFFECT;
    }

#if DEBUGGABLE_GENTREE
    GenTreeCmpXchg() : GenTreeIndir()
    {
    }
#endif

    GenTree*& Comparand()
    {
        return m_comparand;
    }
};

/* gtRetExp -- Place holder for the return expression from an inline candidate (GT_RET_EXPR) */

struct GenTreeRetExpr : public GenTree
{
    GenTreeCall* gtInlineCandidate;

    // Expression representing gtInlineCandidate's value (e.g. spill temp or
    // expression from inlinee, or call itself for unsuccessful inlines).
    // Substituted by UpdateInlineReturnExpressionPlaceHolder.
    // This tree is nullptr during the import that created the GenTreeRetExpr and is set later
    // when handling the actual inline candidate.
    GenTree* gtSubstExpr;

    // The basic block that gtSubstExpr comes from, to enable propagating mandatory flags.
    // nullptr for cases where gtSubstExpr is not a tree from the inlinee.
    BasicBlock* gtSubstBB;

    GenTreeRetExpr(var_types type) : GenTree(GT_RET_EXPR, type)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeRetExpr() : GenTree()
    {
    }
#endif
};

// In LIR there are no longer statements so debug information is inserted linearly using these nodes.
struct GenTreeILOffset : public GenTree
{
    DebugInfo gtStmtDI; // debug info
#ifdef DEBUG
    IL_OFFSET gtStmtLastILoffs; // instr offset at end of stmt
#endif

    GenTreeILOffset(const DebugInfo& di DEBUGARG(IL_OFFSET lastOffset = BAD_IL_OFFSET))
        : GenTree(GT_IL_OFFSET, TYP_VOID)
        , gtStmtDI(di)
#ifdef DEBUG
        , gtStmtLastILoffs(lastOffset)
#endif
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeILOffset() : GenTree(GT_IL_OFFSET, TYP_VOID)
    {
    }
#endif
};

// GenTreeList: adapter class for forward iteration of the execution order GenTree linked list
// using range-based `for`, normally used via Statement::TreeList(), e.g.:
//    for (GenTree* const tree : stmt->TreeList()) ...
//
class GenTreeList
{
    GenTree* m_trees;

public:
    // Forward iterator for the execution order GenTree linked list (using `gtNext` pointer).
    //
    class iterator
    {
        GenTree* m_tree;

    public:
        explicit iterator(GenTree* tree) : m_tree(tree)
        {
        }

        GenTree* operator*() const
        {
            return m_tree;
        }

        iterator& operator++()
        {
            m_tree = m_tree->gtNext;
            return *this;
        }

        bool operator!=(const iterator& i) const
        {
            return m_tree != i.m_tree;
        }
    };

    explicit GenTreeList(GenTree* trees) : m_trees(trees)
    {
    }

    iterator begin() const
    {
        return iterator(m_trees);
    }

    iterator end() const
    {
        return iterator(nullptr);
    }
};

class LocalsGenTreeList
{
    Statement* m_stmt;

public:
    class iterator
    {
        GenTreeLclVarCommon* m_tree;

    public:
        explicit iterator(GenTreeLclVarCommon* tree) : m_tree(tree)
        {
        }

        GenTreeLclVarCommon* operator*() const
        {
            return m_tree;
        }

        iterator& operator++()
        {
            assert((m_tree->gtNext == nullptr) || m_tree->gtNext->OperIsLocal() || m_tree->gtNext->OperIs(GT_LCL_ADDR));
            m_tree = static_cast<GenTreeLclVarCommon*>(m_tree->gtNext);
            return *this;
        }

        iterator& operator--()
        {
            assert((m_tree->gtPrev == nullptr) || m_tree->gtPrev->OperIsLocal() || m_tree->gtPrev->OperIs(GT_LCL_ADDR));
            m_tree = static_cast<GenTreeLclVarCommon*>(m_tree->gtPrev);
            return *this;
        }

        bool operator!=(const iterator& i) const
        {
            return m_tree != i.m_tree;
        }
    };

    explicit LocalsGenTreeList(Statement* stmt) : m_stmt(stmt)
    {
    }

    iterator begin() const;

    iterator end() const
    {
        return iterator(nullptr);
    }

    void Remove(GenTreeLclVarCommon* node);
    void Replace(GenTreeLclVarCommon* firstNode,
                 GenTreeLclVarCommon* lastNode,
                 GenTreeLclVarCommon* newFirstNode,
                 GenTreeLclVarCommon* newLastNode);

private:
    GenTree** GetForwardEdge(GenTreeLclVarCommon* node);
    GenTree** GetBackwardEdge(GenTreeLclVarCommon* node);
};

// We use the following format when printing the Statement number: Statement->GetID()
// This define is used with string concatenation to put this in printf format strings  (Note that %u means unsigned int)
#define FMT_STMT "STMT%05u"

struct Statement
{
public:
    Statement(GenTree* expr DEBUGARG(unsigned stmtID))
        : m_rootNode(expr)
        , m_treeList(nullptr)
        , m_treeListEnd(nullptr)
        , m_next(nullptr)
        , m_prev(nullptr)
#ifdef DEBUG
        , m_lastILOffset(BAD_IL_OFFSET)
        , m_stmtID(stmtID)
#endif
    {
    }

    GenTree* GetRootNode() const
    {
        return m_rootNode;
    }

    GenTree** GetRootNodePointer()
    {
        return &m_rootNode;
    }

    void SetRootNode(GenTree* treeRoot)
    {
        m_rootNode = treeRoot;
    }

    GenTree* GetTreeList() const
    {
        return m_treeList;
    }

    GenTree** GetTreeListPointer()
    {
        return &m_treeList;
    }

    void SetTreeList(GenTree* treeHead)
    {
        m_treeList = treeHead;
    }

    GenTree* GetTreeListEnd() const
    {
        return m_treeListEnd;
    }

    GenTree** GetTreeListEndPointer()
    {
        return &m_treeListEnd;
    }

    void SetTreeListEnd(GenTree* end)
    {
        m_treeListEnd = end;
    }

    GenTreeList       TreeList() const;
    LocalsGenTreeList LocalsTreeList();

    const DebugInfo& GetDebugInfo() const
    {
        return m_debugInfo;
    }

    void SetDebugInfo(const DebugInfo& di)
    {
        m_debugInfo = di;
        di.Validate();
    }

#ifdef DEBUG

    IL_OFFSET GetLastILOffset() const
    {
        return m_lastILOffset;
    }

    void SetLastILOffset(IL_OFFSET lastILOffset)
    {
        m_lastILOffset = lastILOffset;
    }

    unsigned GetID() const
    {
        return m_stmtID;
    }
#endif // DEBUG

    Statement* GetNextStmt() const
    {
        return m_next;
    }

    void SetNextStmt(Statement* nextStmt)
    {
        m_next = nextStmt;
    }

    Statement* GetPrevStmt() const
    {
        return m_prev;
    }

    void SetPrevStmt(Statement* prevStmt)
    {
        m_prev = prevStmt;
    }

    bool IsPhiDefnStmt() const
    {
        return m_rootNode->IsPhiDefn();
    }

    unsigned char GetCostSz() const
    {
        return m_rootNode->GetCostSz();
    }

    unsigned char GetCostEx() const
    {
        return m_rootNode->GetCostEx();
    }

private:
    // The root of the expression tree.
    // Note: It will be the last node in evaluation order.
    GenTree* m_rootNode;

    // The tree list head (for forward walks in evaluation order).
    // The value is `nullptr` until we have set the sequencing of the nodes.
    GenTree* m_treeList;

    // The tree list tail. Only valid when locals are linked (fgNodeThreading
    // == AllLocals), in which case this is the last local.
    // When all nodes are linked (fgNodeThreading == AllTrees), m_rootNode
    // should be considered the last node.
    GenTree* m_treeListEnd;

    // The statement nodes are doubly-linked. The first statement node in a block points
    // to the last node in the block via its `m_prev` link. Note that the last statement node
    // does not point to the first: it has `m_next == nullptr`; that is, the list is not fully circular.
    Statement* m_next;
    Statement* m_prev;

    DebugInfo m_debugInfo;

#ifdef DEBUG
    IL_OFFSET m_lastILOffset; // The instr offset at the end of this statement.
    unsigned  m_stmtID;
#endif
};

// StatementList: adapter class for forward iteration of the statement linked list using range-based `for`,
// normally used via BasicBlock::Statements(), e.g.:
//    for (Statement* const stmt : block->Statements()) ...
// or:
//    for (Statement* const stmt : block->NonPhiStatements()) ...
//
class StatementList
{
    Statement* m_stmts;

    // Forward iterator for the statement linked list.
    //
    class iterator
    {
        Statement* m_stmt;

    public:
        iterator(Statement* stmt) : m_stmt(stmt)
        {
        }

        Statement* operator*() const
        {
            return m_stmt;
        }

        iterator& operator++()
        {
            m_stmt = m_stmt->GetNextStmt();
            return *this;
        }

        bool operator!=(const iterator& i) const
        {
            return m_stmt != i.m_stmt;
        }
    };

public:
    StatementList(Statement* stmts) : m_stmts(stmts)
    {
    }

    iterator begin() const
    {
        return iterator(m_stmts);
    }

    iterator end() const
    {
        return iterator(nullptr);
    }
};

/*  NOTE: Any tree nodes that are larger than 8 bytes (two ints or
    pointers) must be flagged as 'large' in GenTree::InitNodeSize().
 */

/* gtPhiArg -- phi node rhs argument, var = phi(phiarg, phiarg, phiarg...); GT_PHI_ARG */
struct GenTreePhiArg : public GenTreeLclVarCommon
{
    BasicBlock* gtPredBB;

    GenTreePhiArg(var_types type, unsigned lclNum, unsigned ssaNum, BasicBlock* block)
        : GenTreeLclVarCommon(GT_PHI_ARG, type, lclNum), gtPredBB(block)
    {
        SetSsaNum(ssaNum);
    }

#if DEBUGGABLE_GENTREE
    GenTreePhiArg() : GenTreeLclVarCommon()
    {
    }
#endif
};

/* gtPutArgStk -- Argument passed on stack (GT_PUTARG_STK) */

struct GenTreePutArgStk : public GenTreeUnOp
{
private:
    unsigned m_byteOffset;
#ifdef FEATURE_PUT_STRUCT_ARG_STK
    unsigned m_byteSize; // The number of bytes that this argument is occupying on the stack with padding.
#endif

public:
#if defined(UNIX_X86_ABI)
    unsigned gtPadAlign; // Number of padding slots for stack alignment
#endif
#if defined(DEBUG) || defined(UNIX_X86_ABI)
    GenTreeCall* gtCall; // the call node to which this argument belongs
#endif

#if FEATURE_FASTTAILCALL

    bool gtPutInIncomingArgArea; // Whether this arg needs to be placed in incoming arg area.
                                 // By default this is false and will be placed in out-going arg area.
                                 // Fast tail calls set this to true.
                                 // In future if we need to add more such bool fields consider bit fields.
#endif

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    // Instruction selection: during codegen time, what code sequence we will be using
    // to encode this operation.
    // TODO-Throughput: The following information should be obtained from the child
    // block node.

    enum class Kind : int8_t{
        Invalid, RepInstr, PartialRepInstr, Unroll, Push,
    };
    Kind gtPutArgStkKind;

#ifdef TARGET_XARCH
private:
    uint8_t m_argLoadSizeDelta;
#endif // TARGET_XARCH
#endif // FEATURE_PUT_STRUCT_ARG_STK

public:
    GenTreePutArgStk(genTreeOps oper,
                     var_types  type,
                     GenTree*   op1,
                     unsigned   stackByteOffset,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                     unsigned stackByteSize,
#endif
                     GenTreeCall* callNode,
                     bool         putInIncomingArgArea)
        : GenTreeUnOp(oper, type, op1 DEBUGARG(/*largeNode*/ false))
        , m_byteOffset(stackByteOffset)
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
        , m_byteSize(stackByteSize)
#endif
#if defined(UNIX_X86_ABI)
        , gtPadAlign(0)
#endif
#if defined(DEBUG) || defined(UNIX_X86_ABI)
        , gtCall(callNode)
#endif
#if FEATURE_FASTTAILCALL
        , gtPutInIncomingArgArea(putInIncomingArgArea)
#endif // FEATURE_FASTTAILCALL
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
        , gtPutArgStkKind(Kind::Invalid)
#if defined(TARGET_XARCH)
        , m_argLoadSizeDelta(UINT8_MAX)
#endif
#endif // FEATURE_PUT_STRUCT_ARG_STK
    {
    }

    GenTree*& Data()
    {
        return gtOp1;
    }

#if FEATURE_FASTTAILCALL
    bool putInIncomingArgArea() const
    {
        return gtPutInIncomingArgArea;
    }

#else // !FEATURE_FASTTAILCALL

    bool putInIncomingArgArea() const
    {
        return false;
    }

#endif // !FEATURE_FASTTAILCALL

    unsigned getArgOffset() const
    {
        return m_byteOffset;
    }

#if defined(UNIX_X86_ABI)
    unsigned getArgPadding() const
    {
        return gtPadAlign;
    }

    void setArgPadding(unsigned padAlign)
    {
        gtPadAlign = padAlign;
    }
#endif

#ifdef FEATURE_PUT_STRUCT_ARG_STK
    unsigned GetStackByteSize() const
    {
        return m_byteSize;
    }

#ifdef TARGET_XARCH
    //------------------------------------------------------------------------
    // SetArgLoadSize: Set the optimal number of bytes to load for this argument.
    //
    // On XARCH, it is profitable to use wider loads when our source is a local
    // variable. To not duplicate the logic between lowering, LSRA and codegen,
    // we do the legality check once, in lowering, and save the result here, as
    // a negative delta relative to the size of the argument with padding.
    //
    // Arguments:
    //    loadSize - The optimal number of bytes to load
    //
    void SetArgLoadSize(unsigned loadSize)
    {
        unsigned argSize = GetStackByteSize();
        assert(roundUp(loadSize, TARGET_POINTER_SIZE) == argSize);

        m_argLoadSizeDelta = static_cast<uint8_t>(argSize - loadSize);
    }

    //------------------------------------------------------------------------
    // GetArgLoadSize: Get the optimal number of bytes to load for this argument.
    //
    unsigned GetArgLoadSize() const
    {
        assert(m_argLoadSizeDelta != UINT8_MAX);
        return GetStackByteSize() - m_argLoadSizeDelta;
    }
#endif // TARGET_XARCH

    // Return true if this is a PutArgStk of a SIMD12 struct.
    // This is needed because such values are re-typed to SIMD16, and the type of PutArgStk is VOID.
    unsigned isSIMD12() const
    {
        return (varTypeIsSIMD(gtOp1) && (GetStackByteSize() == 12));
    }

    bool isPushKind() const
    {
        return gtPutArgStkKind == Kind::Push;
    }
#else  // !FEATURE_PUT_STRUCT_ARG_STK
    unsigned GetStackByteSize() const;
#endif // !FEATURE_PUT_STRUCT_ARG_STK

#if DEBUGGABLE_GENTREE
    GenTreePutArgStk() : GenTreeUnOp()
    {
    }
#endif
};

#if FEATURE_ARG_SPLIT
// Represent the struct argument: split value in register(s) and stack
struct GenTreePutArgSplit : public GenTreePutArgStk
{
    unsigned gtNumRegs;

    GenTreePutArgSplit(GenTree* op1,
                       unsigned stackByteOffset,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                       unsigned stackByteSize,
#endif
                       unsigned     numRegs,
                       GenTreeCall* callNode,
                       bool         putIncomingArgArea)
        : GenTreePutArgStk(GT_PUTARG_SPLIT,
                           TYP_STRUCT,
                           op1,
                           stackByteOffset,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                           stackByteSize,
#endif
                           callNode,
                           putIncomingArgArea)
        , gtNumRegs(numRegs)
    {
        ClearOtherRegs();
        ClearOtherRegFlags();
    }

    // Type required to support multi-reg struct arg.
    var_types m_regType[MAX_REG_ARG];

    // First reg of struct is always given by GetRegNum().
    // gtOtherRegs holds the other reg numbers of struct.
    regNumberSmall gtOtherRegs[MAX_REG_ARG - 1];

    MultiRegSpillFlags gtSpillFlags;

    //---------------------------------------------------------------------------
    // GetRegNumByIdx: get i'th register allocated to this struct argument.
    //
    // Arguments:
    //     idx   -   index of the struct
    //
    // Return Value:
    //     Return regNumber of i'th register of this struct argument
    //
    regNumber GetRegNumByIdx(unsigned idx) const
    {
        assert(idx < MAX_REG_ARG);

        if (idx == 0)
        {
            return GetRegNum();
        }

        return (regNumber)gtOtherRegs[idx - 1];
    }

    //----------------------------------------------------------------------
    // SetRegNumByIdx: set i'th register of this struct argument
    //
    // Arguments:
    //    reg    -   reg number
    //    idx    -   index of the struct
    //
    // Return Value:
    //    None
    //
    void SetRegNumByIdx(regNumber reg, unsigned idx)
    {
        assert(idx < MAX_REG_ARG);
        if (idx == 0)
        {
            SetRegNum(reg);
        }
        else
        {
            gtOtherRegs[idx - 1] = (regNumberSmall)reg;
            assert(gtOtherRegs[idx - 1] == reg);
        }
    }

    //----------------------------------------------------------------------------
    // ClearOtherRegs: clear multi-reg state to indicate no regs are allocated
    //
    // Arguments:
    //    None
    //
    // Return Value:
    //    None
    //
    void ClearOtherRegs()
    {
        for (unsigned i = 0; i < MAX_REG_ARG - 1; ++i)
        {
            gtOtherRegs[i] = REG_NA;
        }
    }

    GenTreeFlags GetRegSpillFlagByIdx(unsigned idx) const
    {
        return GetMultiRegSpillFlagsByIdx(gtSpillFlags, idx);
    }

    void SetRegSpillFlagByIdx(GenTreeFlags flags, unsigned idx)
    {
#if FEATURE_MULTIREG_RET
        gtSpillFlags = SetMultiRegSpillFlagsByIdx(gtSpillFlags, flags, idx);
#endif
    }

    //--------------------------------------------------------------------------
    // GetRegType:  Get var_type of the register specified by index.
    //
    // Arguments:
    //    index - Index of the register.
    //            First register will have an index 0 and so on.
    //
    // Return Value:
    //    var_type of the register specified by its index.

    var_types GetRegType(unsigned index) const
    {
        assert(index < gtNumRegs);
        var_types result = m_regType[index];
        return result;
    }

    //-------------------------------------------------------------------
    // clearOtherRegFlags: clear GTF_* flags associated with gtOtherRegs
    //
    // Arguments:
    //     None
    //
    // Return Value:
    //     None
    //
    void ClearOtherRegFlags()
    {
        gtSpillFlags = 0;
    }

#if DEBUGGABLE_GENTREE
    GenTreePutArgSplit() : GenTreePutArgStk()
    {
    }
#endif
};
#endif // FEATURE_ARG_SPLIT

// Represents GT_COPY or GT_RELOAD node
//
// Needed to support multi-reg ops.
//
struct GenTreeCopyOrReload : public GenTreeUnOp
{
    // State required to support copy/reload of a multi-reg call node.
    // The first register is always given by GetRegNum().
    //
    regNumberSmall gtOtherRegs[MAX_MULTIREG_COUNT - 1];

    //----------------------------------------------------------
    // ClearOtherRegs: set gtOtherRegs to REG_NA.
    //
    // Arguments:
    //    None
    //
    // Return Value:
    //    None
    //
    void ClearOtherRegs()
    {
        for (unsigned i = 0; i < MAX_MULTIREG_COUNT - 1; ++i)
        {
            gtOtherRegs[i] = REG_NA;
        }
    }

    //-----------------------------------------------------------
    // GetRegNumByIdx: Get regNumber of i'th position.
    //
    // Arguments:
    //    idx   -   register position.
    //
    // Return Value:
    //    Returns regNumber assigned to i'th position.
    //
    regNumber GetRegNumByIdx(unsigned idx) const
    {
        assert(idx < MAX_MULTIREG_COUNT);

        if (idx == 0)
        {
            return GetRegNum();
        }

        return (regNumber)gtOtherRegs[idx - 1];
    }

    //-----------------------------------------------------------
    // SetRegNumByIdx: Set the regNumber for i'th position.
    //
    // Arguments:
    //    reg   -   reg number
    //    idx   -   register position.
    //
    // Return Value:
    //    None.
    //
    void SetRegNumByIdx(regNumber reg, unsigned idx)
    {
        assert(idx < MAX_MULTIREG_COUNT);

        if (idx == 0)
        {
            SetRegNum(reg);
        }
        else
        {
            gtOtherRegs[idx - 1] = (regNumberSmall)reg;
            assert(gtOtherRegs[idx - 1] == reg);
        }
    }

    //----------------------------------------------------------------------------
    // CopyOtherRegs: copy multi-reg state from the given copy/reload node to this
    // node.
    //
    // Arguments:
    //    from  -  GenTree node from which to copy multi-reg state
    //
    // Return Value:
    //    None
    //
    // TODO-ARM: Implement this routine for Arm64 and Arm32
    // TODO-X86: Implement this routine for x86
    void CopyOtherRegs(GenTreeCopyOrReload* from)
    {
        assert(OperGet() == from->OperGet());

#ifdef UNIX_AMD64_ABI
        for (unsigned i = 0; i < MAX_MULTIREG_COUNT - 1; ++i)
        {
            gtOtherRegs[i] = from->gtOtherRegs[i];
        }
#endif
    }

    unsigned GetRegCount() const
    {
        // We need to return the highest index for which we have a valid register.
        // Note that the gtOtherRegs array is off by one (the 0th register is GetRegNum()).
        // If there's no valid register in gtOtherRegs, GetRegNum() must be valid.
        // Note that for most nodes, the set of valid registers must be contiguous,
        // but for COPY or RELOAD there is only a valid register for the register positions
        // that must be copied or reloaded.
        //
        for (unsigned i = MAX_MULTIREG_COUNT; i > 1; i--)
        {
            if (gtOtherRegs[i - 2] != REG_NA)
            {
                return i;
            }
        }

        // We should never have a COPY or RELOAD with no valid registers.
        assert(GetRegNum() != REG_NA);
        return 1;
    }

    GenTreeCopyOrReload(genTreeOps oper, var_types type, GenTree* op1) : GenTreeUnOp(oper, type, op1)
    {
        assert(type != TYP_STRUCT || op1->IsMultiRegNode());
        SetRegNum(REG_NA);
        ClearOtherRegs();
    }

#if DEBUGGABLE_GENTREE
    GenTreeCopyOrReload() : GenTreeUnOp()
    {
    }
#endif
};

// Represents GT_ALLOCOBJ node

struct GenTreeAllocObj final : public GenTreeUnOp
{
    unsigned int         gtNewHelper; // Value returned by ICorJitInfo::getNewHelper
    bool                 gtHelperHasSideEffects;
    CORINFO_CLASS_HANDLE gtAllocObjClsHnd;
#ifdef FEATURE_READYTORUN
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

    GenTreeAllocObj(
        var_types type, unsigned int helper, bool helperHasSideEffects, CORINFO_CLASS_HANDLE clsHnd, GenTree* op)
        : GenTreeUnOp(GT_ALLOCOBJ, type, op DEBUGARG(/*largeNode*/ TRUE))
        , // This node in most cases will be changed to a call node
        gtNewHelper(helper)
        , gtHelperHasSideEffects(helperHasSideEffects)
        , gtAllocObjClsHnd(clsHnd)
    {
#ifdef FEATURE_READYTORUN
        gtEntryPoint.addr = nullptr;
#endif
    }
#if DEBUGGABLE_GENTREE
    GenTreeAllocObj() : GenTreeUnOp()
    {
    }
#endif
};

// Represents GT_RUNTIMELOOKUP node

struct GenTreeRuntimeLookup final : public GenTreeUnOp
{
    CORINFO_GENERIC_HANDLE   gtHnd;
    CorInfoGenericHandleType gtHndType;

    GenTreeRuntimeLookup(CORINFO_GENERIC_HANDLE hnd, CorInfoGenericHandleType hndTyp, GenTree* tree)
        : GenTreeUnOp(GT_RUNTIMELOOKUP, tree->gtType, tree DEBUGARG(/*largeNode*/ FALSE)), gtHnd(hnd), gtHndType(hndTyp)
    {
        assert(hnd != nullptr);
    }
#if DEBUGGABLE_GENTREE
    GenTreeRuntimeLookup() : GenTreeUnOp()
    {
    }
#endif

    // Return reference to the actual tree that does the lookup
    GenTree*& Lookup()
    {
        return gtOp1;
    }

    bool IsClassHandle() const
    {
        return gtHndType == CORINFO_HANDLETYPE_CLASS;
    }
    bool IsMethodHandle() const
    {
        return gtHndType == CORINFO_HANDLETYPE_METHOD;
    }
    bool IsFieldHandle() const
    {
        return gtHndType == CORINFO_HANDLETYPE_FIELD;
    }

    // Note these operations describe the handle that is input to the
    // lookup, not the handle produced by the lookup.
    CORINFO_CLASS_HANDLE GetClassHandle() const
    {
        assert(IsClassHandle());
        return (CORINFO_CLASS_HANDLE)gtHnd;
    }
    CORINFO_METHOD_HANDLE GetMethodHandle() const
    {
        assert(IsMethodHandle());
        return (CORINFO_METHOD_HANDLE)gtHnd;
    }
    CORINFO_FIELD_HANDLE GetFieldHandle() const
    {
        assert(IsMethodHandle());
        return (CORINFO_FIELD_HANDLE)gtHnd;
    }
};

// Represents the condition of a GT_JCC or GT_SETCC node.

struct GenCondition
{
    // clang-format off
    enum Code : unsigned char
    {
        OperMask  = 7,
        Unsigned  = 8,
        Unordered = Unsigned,
        Float     = 16,

        // 0 would be the encoding of "signed EQ" but since equality is sign insensitive
        // we'll use 0 as invalid/uninitialized condition code. This will also leave 1
        // as a spare code.
        NONE = 0,

        SLT  = 2,
        SLE  = 3,
        SGE  = 4,
        SGT  = 5,
        S    = 6,
        NS   = 7,

        EQ   = Unsigned | 0,    // = 8
        NE   = Unsigned | 1,    // = 9
        ULT  = Unsigned | SLT,  // = 10
        ULE  = Unsigned | SLE,  // = 11
        UGE  = Unsigned | SGE,  // = 12
        UGT  = Unsigned | SGT,  // = 13
        C    = Unsigned | S,    // = 14
        NC   = Unsigned | NS,   // = 15

        FEQ  = Float | 0,       // = 16
        FNE  = Float | 1,       // = 17
        FLT  = Float | SLT,     // = 18
        FLE  = Float | SLE,     // = 19
        FGE  = Float | SGE,     // = 20
        FGT  = Float | SGT,     // = 21
        O    = Float | S,       // = 22
        NO   = Float | NS,      // = 23

        FEQU = Unordered | FEQ, // = 24
        FNEU = Unordered | FNE, // = 25
        FLTU = Unordered | FLT, // = 26
        FLEU = Unordered | FLE, // = 27
        FGEU = Unordered | FGE, // = 28
        FGTU = Unordered | FGT, // = 29
        P    = Unordered | O,   // = 30
        NP   = Unordered | NO,  // = 31
    };
    // clang-format on

private:
    Code m_code;

public:
    Code GetCode() const
    {
        return m_code;
    }

    bool IsFlag() const
    {
        return (m_code & OperMask) >= S;
    }

    bool IsUnsigned() const
    {
        return (ULT <= m_code) && (m_code <= UGT);
    }

    bool IsFloat() const
    {
        return !IsFlag() && (m_code & Float) != 0;
    }

    bool IsUnordered() const
    {
        return !IsFlag() && (m_code & (Float | Unordered)) == (Float | Unordered);
    }

    bool Is(Code cond) const
    {
        return m_code == cond;
    }

    template <typename... TRest>
    bool Is(Code c, TRest... rest) const
    {
        return Is(c) || Is(rest...);
    }

    // Indicate whether the condition should be swapped in order to avoid generating
    // multiple branches. This happens for certain floating point conditions on XARCH,
    // see GenConditionDesc and its associated mapping table for more details.
    bool PreferSwap() const
    {
#ifdef TARGET_XARCH
        return Is(GenCondition::FLT, GenCondition::FLE, GenCondition::FGTU, GenCondition::FGEU);
#else
        return false;
#endif
    }

    const char* Name() const
    {
        // clang-format off
        static const char* names[]
        {
            "NONE", "???",  "SLT",  "SLE",  "SGE",  "SGT",  "S", "NS",
            "UEQ",  "UNE",  "ULT",  "ULE",  "UGE",  "UGT",  "C", "NC",
            "FEQ",  "FNE",  "FLT",  "FLE",  "FGE",  "FGT",  "O", "NO",
            "FEQU", "FNEU", "FLTU", "FLEU", "FGEU", "FGTU", "P", "NP"
        };
        // clang-format on

        assert(m_code < ArrLen(names));
        return names[m_code];
    }

    GenCondition() : m_code()
    {
    }

    GenCondition(Code cond) : m_code(cond)
    {
    }

    static_assert((GT_NE - GT_EQ) == (NE & ~Unsigned), "bad relop");
    static_assert((GT_LT - GT_EQ) == SLT, "bad relop");
    static_assert((GT_LE - GT_EQ) == SLE, "bad relop");
    static_assert((GT_GE - GT_EQ) == SGE, "bad relop");
    static_assert((GT_GT - GT_EQ) == SGT, "bad relop");
    static_assert((GT_TEST_NE - GT_TEST_EQ) == (NE & ~Unsigned), "bad relop");

    static GenCondition FromRelop(GenTree* relop)
    {
        assert(relop->OperIsCompare());

        if (varTypeIsFloating(relop->gtGetOp1()))
        {
            return FromFloatRelop(relop);
        }
        else
        {
            return FromIntegralRelop(relop);
        }
    }

    static GenCondition FromFloatRelop(GenTree* relop)
    {
        assert(varTypeIsFloating(relop->gtGetOp1()) && varTypeIsFloating(relop->gtGetOp2()));

        return FromFloatRelop(relop->OperGet(), (relop->gtFlags & GTF_RELOP_NAN_UN) != 0);
    }

    static GenCondition FromFloatRelop(genTreeOps oper, bool isUnordered)
    {
        assert(GenTree::OperIsCompare(oper));

        unsigned code = oper - GT_EQ;
        assert(code <= SGT);
        code |= Float;

        if (isUnordered)
        {
            code |= Unordered;
        }

        return GenCondition(static_cast<Code>(code));
    }

    static GenCondition FromIntegralRelop(GenTree* relop)
    {
        assert(!varTypeIsFloating(relop->gtGetOp1()) && !varTypeIsFloating(relop->gtGetOp2()));
        return FromIntegralRelop(relop->OperGet(), relop->IsUnsigned());
    }

    static GenCondition FromIntegralRelop(genTreeOps oper, bool isUnsigned)
    {
        assert(GenTree::OperIsCompare(oper));
        static constexpr unsigned s_codes[] = {EQ, NE, SLT, SLE, SGE, SGT, EQ, NE, NC, C};

        static_assert_no_msg(s_codes[GT_EQ - GT_EQ] == EQ);
        static_assert_no_msg(s_codes[GT_NE - GT_EQ] == NE);
        static_assert_no_msg(s_codes[GT_LT - GT_EQ] == SLT);
        static_assert_no_msg(s_codes[GT_LE - GT_EQ] == SLE);
        static_assert_no_msg(s_codes[GT_GE - GT_EQ] == SGE);
        static_assert_no_msg(s_codes[GT_GT - GT_EQ] == SGT);
        static_assert_no_msg(s_codes[GT_TEST_EQ - GT_EQ] == EQ);
        static_assert_no_msg(s_codes[GT_TEST_NE - GT_EQ] == NE);
#ifdef TARGET_XARCH
        // Generated via bt instruction that sets C flag to the specified bit.
        static_assert_no_msg(s_codes[GT_BITTEST_EQ - GT_EQ] == NC);
        static_assert_no_msg(s_codes[GT_BITTEST_NE - GT_EQ] == C);
#endif

        unsigned code = s_codes[oper - GT_EQ];

        if (isUnsigned || (code <= 1)) // EQ/NE are treated as unsigned
        {
            code |= Unsigned;
        }

        return GenCondition(static_cast<Code>(code));
    }

    static GenCondition Reverse(GenCondition condition)
    {
        // clang-format off
        static const Code reverse[]
        {
        //  EQ    NE    LT    LE    GE    GT    F   NF
            NONE, NONE, SGE,  SGT,  SLT,  SLE,  NS, S,
            NE,   EQ,   UGE,  UGT,  ULT,  ULE,  NC, C,
            FNEU, FEQU, FGEU, FGTU, FLTU, FLEU, NO, O,
            FNE,  FEQ,  FGE,  FGT,  FLT,  FLE,  NP, P
        };
        // clang-format on

        assert(condition.m_code < ArrLen(reverse));
        return GenCondition(reverse[condition.m_code]);
    }

    static GenCondition Swap(GenCondition condition)
    {
        // clang-format off
        static const Code swap[]
        {
        //  EQ    NE    LT    LE    GE    GT    F  NF
            NONE, NONE, SGT,  SGE,  SLE,  SLT,  S, NS,
            EQ,   NE,   UGT,  UGE,  ULE,  ULT,  C, NC,
            FEQ,  FNE,  FGT,  FGE,  FLE,  FLT,  O, NO,
            FEQU, FNEU, FGTU, FGEU, FLEU, FLTU, P, NP
        };
        // clang-format on

        assert(condition.m_code < ArrLen(swap));
        return GenCondition(swap[condition.m_code]);
    }
};

// Represents a GT_JCC or GT_SETCC node.
struct GenTreeCC final : public GenTree
{
    GenCondition gtCondition;

    GenTreeCC(genTreeOps oper, var_types type, GenCondition condition)
        : GenTree(oper, type DEBUGARG(/*largeNode*/ FALSE)), gtCondition(condition)
    {
        assert(OperIs(GT_JCC, GT_SETCC));
    }

#if DEBUGGABLE_GENTREE
    GenTreeCC() : GenTree()
    {
    }
#endif // DEBUGGABLE_GENTREE
};

// Represents a node with two operands and a condition.
struct GenTreeOpCC : public GenTreeOp
{
    GenCondition gtCondition;

    GenTreeOpCC(genTreeOps oper, var_types type, GenCondition condition, GenTree* op1 = nullptr, GenTree* op2 = nullptr)
        : GenTreeOp(oper, type, op1, op2 DEBUGARG(/*largeNode*/ FALSE)), gtCondition(condition)
    {
#ifdef TARGET_ARM64
        assert(OperIs(GT_SELECTCC, GT_SELECT_INCCC, GT_SELECT_INVCC, GT_SELECT_NEGCC));
#else
        assert(OperIs(GT_SELECTCC));
#endif
    }

#if DEBUGGABLE_GENTREE
    GenTreeOpCC() : GenTreeOp()
    {
    }
#endif // DEBUGGABLE_GENTREE
};

#ifdef TARGET_ARM64
enum insCflags : unsigned
{
    INS_FLAGS_NONE,
    INS_FLAGS_V,
    INS_FLAGS_C,
    INS_FLAGS_CV,

    INS_FLAGS_Z,
    INS_FLAGS_ZV,
    INS_FLAGS_ZC,
    INS_FLAGS_ZCV,

    INS_FLAGS_N,
    INS_FLAGS_NV,
    INS_FLAGS_NC,
    INS_FLAGS_NCV,

    INS_FLAGS_NZ,
    INS_FLAGS_NZV,
    INS_FLAGS_NZC,
    INS_FLAGS_NZCV,
};

struct GenTreeCCMP final : public GenTreeOpCC
{
    insCflags gtFlagsVal;

    GenTreeCCMP(var_types type, GenCondition condition, GenTree* op1, GenTree* op2, insCflags flagsVal)
        : GenTreeOpCC(GT_CCMP, type, condition, op1, op2), gtFlagsVal(flagsVal)
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeCCMP() : GenTreeOpCC()
    {
    }
#endif // DEBUGGABLE_GENTREE
};
#endif

//------------------------------------------------------------------------
// Deferred inline functions of GenTree -- these need the subtypes above to
// be defined already.
//------------------------------------------------------------------------

inline bool GenTree::OperIsBlkOp()
{
    if (OperIs(GT_STORE_DYN_BLK))
    {
        return true;
    }
    if (OperIsStore())
    {
        return varTypeIsStruct(this);
    }

    return false;
}

inline bool GenTree::OperIsInitBlkOp()
{
    if (!OperIsBlkOp())
    {
        return false;
    }
    GenTree* src       = Data();
    bool     isInitBlk = src->TypeIs(TYP_INT);
    assert(isInitBlk == src->gtSkipReloadOrCopy()->IsInitVal());

    return isInitBlk;
}

inline bool GenTree::OperIsCopyBlkOp()
{
    return OperIsBlkOp() && !OperIsInitBlkOp();
}

//------------------------------------------------------------------------
// IsIntegralConst: Checks whether this is a constant node with the given value
//
// Arguments:
//    constVal - the value of interest
//
// Return Value:
//    Returns true iff the tree is an integral constant opcode, with
//    the given value.
//
// Notes:
//    Like gtIconVal, the argument is of ssize_t, so cannot check for
//    long constants in a target-independent way.

inline bool GenTree::IsIntegralConst(ssize_t constVal) const
{
    if ((gtOper == GT_CNS_INT) && (AsIntConCommon()->IconValue() == constVal))
    {
        return true;
    }

    if ((gtOper == GT_CNS_LNG) && (AsIntConCommon()->LngValue() == constVal))
    {
        return true;
    }

    return false;
}

//-------------------------------------------------------------------
// IsFloatAllBitsSet: returns true if this is exactly a const float value representing AllBitsSet.
//
// Returns:
//     True if this represents a const floating-point value representing AllBitsSet.
//     Will return false otherwise.
//
inline bool GenTree::IsFloatAllBitsSet() const
{
    if (IsCnsFltOrDbl())
    {
        double constValue = AsDblCon()->DconValue();

        if (TypeIs(TYP_FLOAT))
        {
            return FloatingPointUtils::isAllBitsSet(static_cast<float>(constValue));
        }
        else
        {
            assert(TypeIs(TYP_DOUBLE));
            return FloatingPointUtils::isAllBitsSet(constValue);
        }
    }

    return false;
}

//-------------------------------------------------------------------
// IsFloatNaN: returns true if this is exactly a const float value of NaN
//
// Returns:
//     True if this represents a const floating-point value of NaN.
//     Will return false otherwise.
//
inline bool GenTree::IsFloatNaN() const
{
    if (IsCnsFltOrDbl())
    {
        double constValue = AsDblCon()->DconValue();
        return FloatingPointUtils::isNaN(constValue);
    }

    return false;
}

//-------------------------------------------------------------------
// IsFloatNegativeZero: returns true if this is exactly a const float value of negative zero (-0.0)
//
// Returns:
//     True if this represents a const floating-point value of exactly negative zero (-0.0).
//     Will return false if the value is negative zero (+0.0).
//
inline bool GenTree::IsFloatNegativeZero() const
{
    if (IsCnsFltOrDbl())
    {
        double constValue = AsDblCon()->DconValue();
        return FloatingPointUtils::isNegativeZero(constValue);
    }

    return false;
}

//-------------------------------------------------------------------
// IsFloatPositiveZero: returns true if this is exactly a const float value of positive zero (+0.0)
//
// Returns:
//     True if this represents a const floating-point value of exactly positive zero (+0.0).
//     Will return false if the value is negative zero (-0.0).
//
inline bool GenTree::IsFloatPositiveZero() const
{
    if (IsCnsFltOrDbl())
    {
        // This implementation is almost identical to IsCnsNonZeroFltOrDbl
        // but it is easier to parse out
        // rather than using !IsCnsNonZeroFltOrDbl.
        double constValue = AsDblCon()->DconValue();
        return FloatingPointUtils::isPositiveZero(constValue);
    }

    return false;
}

//-------------------------------------------------------------------
// IsVectorZero: returns true if this node is a vector constant with all bits zero.
//
// Returns:
//     True if this node is a vector constant with all bits zero
//
inline bool GenTree::IsVectorZero() const
{
    return IsCnsVec() && AsVecCon()->IsZero();
}

//-------------------------------------------------------------------
// IsVectorCreate: returns true if this node is the creation of a vector.
//                 Does not include "Unsafe" method calls.
//
// Returns:
//     True if this node is the creation of a vector
//
inline bool GenTree::IsVectorCreate() const
{
#ifdef FEATURE_HW_INTRINSICS
    if (OperIs(GT_HWINTRINSIC))
    {
        switch (AsHWIntrinsic()->GetHWIntrinsicId())
        {
            case NI_Vector128_Create:
#if defined(TARGET_XARCH)
            case NI_Vector256_Create:
            case NI_Vector512_Create:
#elif defined(TARGET_ARMARCH)
            case NI_Vector64_Create:
#endif
                return true;

            default:
                return false;
        }
    }
#endif // FEATURE_HW_INTRINSICS

    return false;
}

//-------------------------------------------------------------------
// IsVectorAllBitsSet: returns true if this node is a vector constant with all bits set.
//
// Returns:
//     True if this node is a vector constant with all bits set
//
inline bool GenTree::IsVectorAllBitsSet() const
{
#ifdef FEATURE_SIMD
    if (IsCnsVec())
    {
        return AsVecCon()->IsAllBitsSet();
    }
#endif // FEATURE_SIMD

    return false;
}

//-------------------------------------------------------------------
// IsVectorConst: returns true if this node is a HWIntrinsic that represents a constant.
//
// Returns:
//     True if this represents a HWIntrinsic node that represents a constant.
//
inline bool GenTree::IsVectorConst()
{
#ifdef FEATURE_SIMD
    if (IsCnsVec())
    {
        return true;
    }
#endif // FEATURE_SIMD

    return false;
}

//-------------------------------------------------------------------
// GetIntegralVectorConstElement: Gets the value of a given element in an integral vector constant
//
// Returns:
//     The value of a given element in an integral vector constant
//
inline uint64_t GenTree::GetIntegralVectorConstElement(size_t index, var_types simdBaseType)
{
#ifdef FEATURE_HW_INTRINSICS
    if (IsCnsVec())
    {
        const GenTreeVecCon* node = AsVecCon();

        switch (simdBaseType)
        {
            case TYP_BYTE:
            {
                return node->gtSimdVal.i8[index];
            }

            case TYP_UBYTE:
            {
                return node->gtSimdVal.u8[index];
            }

            case TYP_SHORT:
            {
                return node->gtSimdVal.i16[index];
            }

            case TYP_USHORT:
            {
                return node->gtSimdVal.u16[index];
            }

            case TYP_INT:
            case TYP_FLOAT:
            {
                return node->gtSimdVal.i32[index];
            }

            case TYP_UINT:
            {
                return node->gtSimdVal.u32[index];
            }

            case TYP_LONG:
            case TYP_DOUBLE:
            {
                return node->gtSimdVal.i64[index];
            }

            case TYP_ULONG:
            {
                return node->gtSimdVal.u64[index];
            }

            default:
            {
                unreached();
            }
        }
    }
#endif // FEATURE_HW_INTRINSICS

    return false;
}

inline bool GenTree::IsBoxedValue()
{
    assert(gtOper != GT_BOX || AsBox()->BoxOp() != nullptr);
    return (gtOper == GT_BOX) && (gtFlags & GTF_BOX_VALUE);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// IsValidCallArgument: Given an GenTree node that represents an argument
//                      enforce (or don't enforce) the following invariant.
//
// Arguments:
//    instance method for a GenTree node
//
// Return values:
//    true:      the GenTree node is accepted as a valid argument
//    false:     the GenTree node is not accepted as a valid argument
//
// Notes:
//    For targets that don't support arguments as a list of fields, we do not support GT_FIELD_LIST.
//
//    Currently for AMD64 UNIX we allow a limited case where a GT_FIELD_LIST is
//    allowed but every element must be a GT_LCL_FLD.
//
//    For the future targets that allow for Multireg args (and this includes the current ARM64 target),
//    or that allow for passing promoted structs, we allow a GT_FIELD_LIST of arbitrary nodes.
//    These would typically start out as GT_LCL_VARs or GT_LCL_FLDS or GT_INDs,
//    but could be changed into constants or GT_COMMA trees by the later
//    optimization phases.

inline bool GenTree::IsValidCallArgument()
{
    if (OperIs(GT_FIELD_LIST))
    {
#if !FEATURE_MULTIREG_ARGS && !FEATURE_PUT_STRUCT_ARG_STK

        return false;

#else // FEATURE_MULTIREG_ARGS or FEATURE_PUT_STRUCT_ARG_STK

        // We allow this GT_FIELD_LIST as an argument
        return true;

#endif // FEATURE_MULTIREG_ARGS or FEATURE_PUT_STRUCT_ARG_STK
    }
    // We don't have either kind of list, so it satisfies the invariant.
    return true;
}
#endif // DEBUG

inline GenTree* GenTree::gtGetOp1() const
{
    return AsOp()->gtOp1;
}

#ifdef DEBUG
/* static */ inline bool GenTree::RequiresNonNullOp2(genTreeOps oper)
{
    switch (oper)
    {
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_DIV:
        case GT_MOD:
        case GT_UDIV:
        case GT_UMOD:
        case GT_OR:
        case GT_XOR:
        case GT_AND:
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
        case GT_INDEX_ADDR:
        case GT_MKREFANY:
            return true;
        default:
            return false;
    }
}

#endif // DEBUG
inline GenTree* GenTree::gtGetOp2() const
{
    assert(OperIsBinary());

    GenTree* op2 = AsOp()->gtOp2;

    // Only allow null op2 if the node type allows it, e.g. GT_LEA.
    assert((op2 != nullptr) || !RequiresNonNullOp2(gtOper));

    return op2;
}

inline GenTree* GenTree::gtGetOp2IfPresent() const
{
    /* AsOp()->gtOp2 is only valid for GTK_BINOP nodes. */

    GenTree* op2 = OperIsBinary() ? AsOp()->gtOp2 : nullptr;

    // This documents the genTreeOps for which AsOp()->gtOp2 cannot be nullptr.
    // This helps prefix in its analysis of code which calls gtGetOp2()

    assert((op2 != nullptr) || !RequiresNonNullOp2(gtOper));

    return op2;
}

inline GenTree*& GenTree::Data()
{
    assert(OperIsStore() || OperIs(GT_STORE_DYN_BLK));
    return OperIsLocalStore() ? AsLclVarCommon()->Data() : AsIndir()->Data();
}

inline GenTree* GenTree::gtEffectiveVal()
{
    GenTree* effectiveVal = this;
    while (true)
    {
        if (effectiveVal->OperIs(GT_COMMA))
        {
            effectiveVal = effectiveVal->gtGetOp2();
        }
        else
        {
            return effectiveVal;
        }
    }
}

//-------------------------------------------------------------------------
// gtCommaStoreVal - find value being assigned to a comma wrapped store
//
// Returns:
//    tree representing value being stored if this tree represents a
//    comma-wrapped local definition and use.
//
//    original tree, if not.
//
inline GenTree* GenTree::gtCommaStoreVal()
{
    GenTree* result = this;

    if (OperIs(GT_COMMA))
    {
        GenTree* commaOp1 = AsOp()->gtOp1;
        GenTree* commaOp2 = AsOp()->gtOp2;

        if (commaOp2->OperIs(GT_LCL_VAR) && commaOp1->OperIs(GT_STORE_LCL_VAR) &&
            (commaOp1->AsLclVar()->GetLclNum() == commaOp2->AsLclVar()->GetLclNum()))
        {
            result = commaOp1->AsLclVar()->Data();
        }
    }

    return result;
}

inline GenTree* GenTree::gtSkipReloadOrCopy()
{
    // There can be only one reload or copy (we can't have a reload/copy of a reload/copy)
    if (OperIs(GT_RELOAD, GT_COPY))
    {
        assert(!gtGetOp1()->OperIs(GT_RELOAD, GT_COPY));
        return gtGetOp1();
    }
    return this;
}

//-----------------------------------------------------------------------------------
// IsMultiRegCall: whether a call node returns its value in more than one register
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a multi register returning call
//
inline bool GenTree::IsMultiRegCall() const
{
    if (this->IsCall())
    {
        return AsCall()->HasMultiRegRetVal();
    }

    return false;
}

//-----------------------------------------------------------------------------------
// IsMultiRegLclVar: whether a local var node defines multiple registers
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a multi register defining local var
//
inline bool GenTree::IsMultiRegLclVar() const
{
    if (OperIsScalarLocal())
    {
        return AsLclVar()->IsMultiReg();
    }
    return false;
}

//-----------------------------------------------------------------------------------
// GetRegByIndex: Get a specific register, based on regIndex, that is produced by this node.
//
// Arguments:
//     regIndex - which register to return (must be 0 for non-multireg nodes)
//
// Return Value:
//     The register, if any, assigned to this index for this node.
//
// Notes:
//     All targets that support multi-reg ops of any kind also support multi-reg return
//     values for calls. Should that change with a future target, this method will need
//     to change accordingly.
//
inline regNumber GenTree::GetRegByIndex(int regIndex) const
{
    if (regIndex == 0)
    {
        return GetRegNum();
    }

#if FEATURE_MULTIREG_RET

    if (IsMultiRegCall())
    {
        return AsCall()->GetRegNumByIdx(regIndex);
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        return AsPutArgSplit()->GetRegNumByIdx(regIndex);
    }
#endif

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegNumByIdx(regIndex);
    }
#endif
#endif // FEATURE_MULTIREG_RET

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return AsCopyOrReload()->GetRegNumByIdx(regIndex);
    }

#ifdef FEATURE_HW_INTRINSICS
    if (OperIs(GT_HWINTRINSIC))
    {
        return AsHWIntrinsic()->GetRegNumByIdx(regIndex);
    }
#endif // FEATURE_HW_INTRINSICS

    if (OperIsScalarLocal())
    {
        return AsLclVar()->GetRegNumByIdx(regIndex);
    }

    assert(!"Invalid regIndex for GetRegFromMultiRegNode");
    return REG_NA;
}

//-----------------------------------------------------------------------------------
// GetRegTypeByIndex: Get a specific register's type, based on regIndex, that is produced
//                    by this multi-reg node.
//
// Arguments:
//     regIndex - index of register whose type will be returned
//
// Return Value:
//     The register type assigned to this index for this node.
//
// Notes:
//     This must be a multireg node that is *not* a copy or reload (which must retrieve the
//     type from its source), and 'regIndex' must be a valid index for this node.
//
//     All targets that support multi-reg ops of any kind also support multi-reg return
//     values for calls. Should that change with a future target, this method will need
//     to change accordingly.
//
inline var_types GenTree::GetRegTypeByIndex(int regIndex) const
{
#if FEATURE_MULTIREG_RET
    if (IsMultiRegCall())
    {
        return AsCall()->AsCall()->GetReturnTypeDesc()->GetReturnRegType(regIndex);
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        return AsPutArgSplit()->GetRegType(regIndex);
    }
#endif // FEATURE_ARG_SPLIT

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegType(regIndex);
    }
#endif // !defined(TARGET_64BIT)
#endif // FEATURE_MULTIREG_RET

#ifdef FEATURE_HW_INTRINSICS
    if (OperIsHWIntrinsic())
    {
        assert(TypeGet() == TYP_STRUCT);
#ifdef TARGET_ARM64
        if (AsHWIntrinsic()->GetSimdSize() == 16)
        {
            return TYP_SIMD16;
        }
        else
        {
            assert(AsHWIntrinsic()->GetSimdSize() == 8);
            return TYP_SIMD8;
        }
#elif defined(TARGET_XARCH)
        // At this time, the only multi-reg HW intrinsics all return the type of their
        // arguments. If this changes, we will need a way to record or determine this.
        return AsHWIntrinsic()->Op(1)->TypeGet();
#endif
    }
#endif // FEATURE_HW_INTRINSICS

    if (OperIsScalarLocal())
    {
        if (TypeGet() == TYP_LONG)
        {
            return TYP_INT;
        }
        assert(TypeGet() == TYP_STRUCT);
        assert((gtFlags & GTF_VAR_MULTIREG) != 0);
        // The register type for a multireg lclVar requires looking at the LclVarDsc,
        // which requires a Compiler instance. The caller must use the GetFieldTypeByIndex
        // on GenTreeLclVar.
        assert(!"GetRegTypeByIndex for LclVar");
    }

    assert(!"Invalid node type for GetRegTypeByIndex");
    return TYP_UNDEF;
}

//-----------------------------------------------------------------------------------
// GetRegSpillFlagByIdx: Get a specific register's spill flags, based on regIndex,
//                       for this multi-reg node.
//
// Arguments:
//     regIndex - which register's spill flags to return
//
// Return Value:
//     The spill flags (GTF_SPILL GTF_SPILLED) for this register.
//
// Notes:
//     This must be a multireg node and 'regIndex' must be a valid index for this node.
//     This method returns the GTF "equivalent" flags based on the packed flags on the multireg node.
//
inline GenTreeFlags GenTree::GetRegSpillFlagByIdx(int regIndex) const
{
#if FEATURE_MULTIREG_RET
    if (IsMultiRegCall())
    {
        return AsCall()->GetRegSpillFlagByIdx(regIndex);
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        return AsPutArgSplit()->GetRegSpillFlagByIdx(regIndex);
    }
#endif // FEATURE_ARG_SPLIT

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegSpillFlagByIdx(regIndex);
    }
#endif // !defined(TARGET_64BIT)
#endif // FEATURE_MULTIREG_RET

#ifdef FEATURE_HW_INTRINSICS
    if (OperIsHWIntrinsic())
    {
        return AsHWIntrinsic()->GetRegSpillFlagByIdx(regIndex);
    }
#endif // FEATURE_HW_INTRINSICS

    if (OperIsScalarLocal())
    {
        return AsLclVar()->GetRegSpillFlagByIdx(regIndex);
    }

    assert(!"Invalid node type for GetRegSpillFlagByIdx");
    return GTF_EMPTY;
}

//-----------------------------------------------------------------------------------
// SetRegSpillFlagByIdx: Set a specific register's spill flags, based on regIndex,
//                       for this multi-reg node.
//
// Arguments:
//     flags    - the flags to set
//     regIndex - which register's spill flags to set
//
// Notes:
//     This must be a multireg node and 'regIndex' must be a valid index for this node.
//     This method takes the GTF "equivalent" flags and sets the packed flags on the
//     multireg node.
//
inline void GenTree::SetRegSpillFlagByIdx(GenTreeFlags flags, int regIndex)
{
#if FEATURE_MULTIREG_RET
    if (IsMultiRegCall())
    {
        AsCall()->SetRegSpillFlagByIdx(flags, regIndex);
        return;
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        AsPutArgSplit()->SetRegSpillFlagByIdx(flags, regIndex);
        return;
    }
#endif // FEATURE_ARG_SPLIT

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        AsMultiRegOp()->SetRegSpillFlagByIdx(flags, regIndex);
        return;
    }
#endif // !defined(TARGET_64BIT)

#endif // FEATURE_MULTIREG_RET

#ifdef FEATURE_HW_INTRINSICS
    if (OperIsHWIntrinsic())
    {
        AsHWIntrinsic()->SetRegSpillFlagByIdx(flags, regIndex);
        return;
    }
#endif // FEATURE_HW_INTRINSICS

    if (OperIsScalarLocal())
    {
        AsLclVar()->SetRegSpillFlagByIdx(flags, regIndex);
        return;
    }

    assert(!"Invalid node type for SetRegSpillFlagByIdx");
}

//-----------------------------------------------------------------------------------
// GetLastUseBit: Get the last use bit for regIndex
//
// Arguments:
//     fieldIndex - the field index
//
// Return Value:
//     The bit to set, clear or query for the last-use of the fieldIndex'th value.
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline GenTreeFlags GenTree::GetLastUseBit(int fieldIndex) const
{
    assert(fieldIndex < 4);
    assert(OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_FLD, GT_LCL_ADDR, GT_COPY, GT_RELOAD));
    static_assert_no_msg((1 << FIELD_LAST_USE_SHIFT) == GTF_VAR_FIELD_DEATH0);
    return (GenTreeFlags)(1 << (FIELD_LAST_USE_SHIFT + fieldIndex));
}

//-----------------------------------------------------------------------------------
// IsLastUse: Determine whether this node is a last use of a promoted field.
//
// Arguments:
//     fieldIndex - the index of the field
//
// Return Value:
//     true iff this is a last use.
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline bool GenTree::IsLastUse(int fieldIndex) const
{
    assert(OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_FLD, GT_LCL_ADDR, GT_COPY, GT_RELOAD));
    return (gtFlags & GetLastUseBit(fieldIndex)) != 0;
}

//-----------------------------------------------------------------------------------
// IsLastUse: Determine whether this node is a last use of any value
//
// Return Value:
//     true iff this has any last uses (i.e. at any index).
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline bool GenTree::HasLastUse() const
{
    assert(OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_LCL_FLD, GT_STORE_LCL_FLD, GT_LCL_ADDR, GT_COPY, GT_RELOAD));
    return (gtFlags & (GTF_VAR_DEATH_MASK)) != 0;
}

//-----------------------------------------------------------------------------------
// SetLastUse: Set the last use bit for the given index
//
// Arguments:
//     fieldIndex - the index
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline void GenTree::SetLastUse(int fieldIndex)
{
    gtFlags |= GetLastUseBit(fieldIndex);
}

//-----------------------------------------------------------------------------------
// ClearLastUse: Clear the last use bit for the given index
//
// Arguments:
//     fieldIndex - the index
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline void GenTree::ClearLastUse(int fieldIndex)
{
    gtFlags &= ~GetLastUseBit(fieldIndex);
}

//-------------------------------------------------------------------------
// IsCopyOrReload: whether this is a GT_COPY or GT_RELOAD node.
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a copy or reload node.
//
inline bool GenTree::IsCopyOrReload() const
{
    return (gtOper == GT_COPY || gtOper == GT_RELOAD);
}

//-----------------------------------------------------------------------------------
// IsCopyOrReloadOfMultiRegCall: whether this is a GT_COPY or GT_RELOAD of a multi-reg
// call node.
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a copy or reload of multi-reg call node.
//
inline bool GenTree::IsCopyOrReloadOfMultiRegCall() const
{
    if (IsCopyOrReload())
    {
        return gtGetOp1()->IsMultiRegCall();
    }

    return false;
}

inline bool GenTree::IsCnsIntOrI() const
{
    return (gtOper == GT_CNS_INT);
}

inline bool GenTree::IsIntegralConst() const
{
#ifdef TARGET_64BIT
    return IsCnsIntOrI();
#else  // !TARGET_64BIT
    return ((gtOper == GT_CNS_INT) || (gtOper == GT_CNS_LNG));
#endif // !TARGET_64BIT
}

//-------------------------------------------------------------------------
// IsIntegralConstPow2: Determines whether an integral constant is
//                      the power of 2.
//
// Return Value:
//     Returns true if the GenTree's integral constant
//     is the power of 2.
//
inline bool GenTree::IsIntegralConstPow2() const
{
    if (IsIntegralConst())
    {
        return isPow2(AsIntConCommon()->IntegralValue());
    }

    return false;
}

//-------------------------------------------------------------------------
// IsIntegralConstUnsignedPow2: Determines whether the unsigned value of
//                              an integral constant is the power of 2.
//
// Return Value:
//     Returns true if the unsigned value of a GenTree's integral constant
//     is the power of 2.
//
// Notes:
//     Integral constant nodes store its value in signed form.
//     This should handle cases where an unsigned-int was logically used in
//     user code.
//
inline bool GenTree::IsIntegralConstUnsignedPow2() const
{
    if (IsIntegralConst())
    {
        return isPow2((UINT64)AsIntConCommon()->IntegralValue());
    }

    return false;
}

//-------------------------------------------------------------------------
// IsIntegralConstAbsPow2: Determines whether the absolute value of
//                         an integral constant is the power of 2.
//
// Return Value:
//     Returns true if the absolute value of a GenTree's integral constant
//     is the power of 2.
//
inline bool GenTree::IsIntegralConstAbsPow2() const
{
    if (IsIntegralConst())
    {
        INT64  svalue = AsIntConCommon()->IntegralValue();
        size_t value  = (svalue == SSIZE_T_MIN) ? static_cast<size_t>(svalue) : static_cast<size_t>(abs(svalue));
        return isPow2(value);
    }

    return false;
}

// Is this node an integer constant that fits in a 32-bit signed integer (INT32)
inline bool GenTree::IsIntCnsFitsInI32()
{
#ifdef TARGET_64BIT
    return IsCnsIntOrI() && AsIntCon()->FitsInI32();
#else  // !TARGET_64BIT
    return IsCnsIntOrI();
#endif // !TARGET_64BIT
}

inline bool GenTree::IsCnsFltOrDbl() const
{
    return OperIs(GT_CNS_DBL);
}

inline bool GenTree::IsCnsNonZeroFltOrDbl() const
{
    if (IsCnsFltOrDbl())
    {
        double constValue = AsDblCon()->DconValue();
        return *(__int64*)&constValue != 0;
    }

    return false;
}

inline bool GenTree::IsCnsVec() const
{
    return OperIs(GT_CNS_VEC);
}

inline bool GenTree::IsHelperCall()
{
    return OperGet() == GT_CALL && AsCall()->gtCallType == CT_HELPER;
}

inline var_types GenTree::CastFromType()
{
    return this->AsCast()->CastOp()->TypeGet();
}
inline var_types& GenTree::CastToType()
{
    return this->AsCast()->gtCastType;
}

inline bool GenTree::isUsedFromSpillTemp() const
{
    // If spilled and no reg at use, then it is used from the spill temp location rather than being reloaded.
    if (((gtFlags & GTF_SPILLED) != 0) && ((gtFlags & GTF_NOREG_AT_USE) != 0))
    {
        return true;
    }

    return false;
}

// Helper function to return the array reference of an array length node.
inline GenTree* GenTree::GetArrLengthArrRef()
{
    assert(OperIsArrLength());
    return AsArrCommon()->ArrRef();
}

// Helper function to return the address of an indir or array meta-data node.
inline GenTree* GenTree::GetIndirOrArrMetaDataAddr()
{
    assert(OperIsIndirOrArrMetaData());

    if (OperIsIndir())
    {
        return AsIndir()->Addr();
    }
    else
    {
        return AsArrCommon()->ArrRef();
    }
}

/*****************************************************************************/

#ifndef HOST_64BIT
#include <poppack.h>
#endif

/*****************************************************************************/

const size_t TREE_NODE_SZ_SMALL = sizeof(GenTreeLclFld);

// For some configurations, such as x86 release, GenTreeVecCon is
// the largest by a small margin due to needing to carry a simd64_t
// constant value. Otherwise, GenTreeCall is the largest.

const size_t TREE_NODE_SZ_LARGE =
    (sizeof(GenTreeVecCon) < sizeof(GenTreeCall)) ? sizeof(GenTreeCall) : sizeof(GenTreeVecCon);

enum varRefKinds
{
    VR_INVARIANT = 0x00, // an invariant value
    VR_NONE      = 0x00,
    VR_IND_REF   = 0x01, // an object reference
    VR_IND_SCL   = 0x02, // a non-object reference
    VR_GLB_VAR   = 0x04, // a global (clsVar)
};

/*****************************************************************************/
#endif // !GENTREE_H
/*****************************************************************************/
