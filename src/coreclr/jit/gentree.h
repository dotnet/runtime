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
#include "reglist.h"
#include "valuenumtype.h"
#include "jitstd.h"
#include "jithashtable.h"
#include "simd.h"
#include "namedintrinsiclist.h"
#include "layout.h"

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
// SCK_PAUSE_EXEC is not currently used.
//
enum SpecialCodeKind
{
    SCK_NONE,
    SCK_RNGCHK_FAIL,                // target when range check fails
    SCK_PAUSE_EXEC,                 // target to stop (e.g. to allow GC)
    SCK_DIV_BY_ZERO,                // target for divide by zero (Not used on X86/X64)
    SCK_ARITH_EXCPN,                // target on arithmetic exception
    SCK_OVERFLOW = SCK_ARITH_EXCPN, // target on overflow
    SCK_ARG_EXCPN,                  // target on ArgumentException (currently used only for SIMD intrinsics)
    SCK_ARG_RNG_EXCPN,              // target on ArgumentOutOfRangeException (currently used only for SIMD intrinsics)
    SCK_COUNT
};

/*****************************************************************************/

enum genTreeOps : BYTE
{
#define GTNODE(en, st, cm, ok) GT_##en,
#include "gtlist.h"

    GT_COUNT,

#ifdef TARGET_64BIT
    // GT_CNS_NATIVELONG is the gtOper symbol for GT_CNS_LNG or GT_CNS_INT, depending on the target.
    // For the 64-bit targets we will only use GT_CNS_INT as it used to represent all the possible sizes
    GT_CNS_NATIVELONG = GT_CNS_INT,
#else
    // For the 32-bit targets we use a GT_CNS_LNG to hold a 64-bit integer constant and GT_CNS_INT for all others.
    // In the future when we retarget the JIT for x86 we should consider eliminating GT_CNS_LNG
    GT_CNS_NATIVELONG = GT_CNS_LNG,
#endif
};

/*****************************************************************************
 *
 *  The following enum defines a set of bit flags that can be used
 *  to classify expression tree nodes. Note that some operators will
 *  have more than one bit set, as follows:
 *
 *          GTK_CONST    implies    GTK_LEAF
 *          GTK_RELOP    implies    GTK_BINOP
 *          GTK_LOGOP    implies    GTK_BINOP
 */

enum genTreeKinds
{
    GTK_SPECIAL = 0x0000, // unclassified operator (special handling reqd)

    GTK_CONST = 0x0001, // constant     operator
    GTK_LEAF  = 0x0002, // leaf         operator
    GTK_UNOP  = 0x0004, // unary        operator
    GTK_BINOP = 0x0008, // binary       operator
    GTK_RELOP = 0x0010, // comparison   operator
    GTK_LOGOP = 0x0020, // logical      operator

    GTK_KINDMASK = 0x007F, // operator kind mask

    GTK_COMMUTE = 0x0080, // commutative  operator

    GTK_EXOP = 0x0100, // Indicates that an oper for a node type that extends GenTreeOp (or GenTreeUnOp)
                       // by adding non-node fields to unary or binary operator.

    GTK_LOCAL = 0x0200, // is a local access (load, store, phi)

    GTK_NOVALUE = 0x0400, // node does not produce a value
    GTK_NOTLIR  = 0x0800, // node is not allowed in LIR

    GTK_NOCONTAIN = 0x1000, // this node is a value, but may not be contained

    /* Define composite value(s) */

    GTK_SMPOP = (GTK_UNOP | GTK_BINOP | GTK_RELOP | GTK_LOGOP)
};

/*****************************************************************************/

enum gtCallTypes : BYTE
{
    CT_USER_FUNC, // User function
    CT_HELPER,    // Jit-helper
    CT_INDIRECT,  // Indirect call

    CT_COUNT // fake entry (must be last)
};

#ifdef DEBUG
/*****************************************************************************
*
*  TargetHandleTypes are used to determine the type of handle present inside GenTreeIntCon node.
*  The values are such that they don't overlap with helper's or user function's handle.
*/
enum TargetHandleType : BYTE
{
    THT_Unknown                  = 2,
    THT_GSCookieCheck            = 4,
    THT_SetGSCookie              = 6,
    THT_IntializeArrayIntrinsics = 8
};
#endif
/*****************************************************************************/

struct BasicBlock;
enum BasicBlockFlags : unsigned __int64;
struct InlineCandidateInfo;
struct GuardedDevirtualizationCandidateInfo;
struct ClassProfileCandidateInfo;

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
    // true if the assertion holds on the bbNext edge instead of the bbJumpDest edge (for GT_JTRUE nodes)
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

/*****************************************************************************/

// GT_FIELD nodes will be lowered into more "code-gen-able" representations, like
// GT_IND's of addresses, or GT_LCL_FLD nodes.  We'd like to preserve the more abstract
// information, and will therefore annotate such lowered nodes with FieldSeq's.  A FieldSeq
// represents a (possibly) empty sequence of fields.  The fields are in the order
// in which they are dereferenced.  The first field may be an object field or a struct field;
// all subsequent fields must be struct fields.
struct FieldSeqNode
{
    CORINFO_FIELD_HANDLE m_fieldHnd;
    FieldSeqNode*        m_next;

    FieldSeqNode(CORINFO_FIELD_HANDLE fieldHnd, FieldSeqNode* next) : m_fieldHnd(fieldHnd), m_next(next)
    {
    }

    // returns true when this is the pseudo #FirstElem field sequence
    bool IsFirstElemFieldSeq();

    // returns true when this is the pseudo #ConstantIndex field sequence
    bool IsConstantIndexFieldSeq();

    // returns true when this is the the pseudo #FirstElem field sequence or the pseudo #ConstantIndex field sequence
    bool IsPseudoField() const;

    CORINFO_FIELD_HANDLE GetFieldHandle() const
    {
        assert(!IsPseudoField() && (m_fieldHnd != nullptr));
        return m_fieldHnd;
    }

    FieldSeqNode* GetTail()
    {
        FieldSeqNode* tail = this;
        while (tail->m_next != nullptr)
        {
            tail = tail->m_next;
        }
        return tail;
    }

    // Make sure this provides methods that allow it to be used as a KeyFuncs type in SimplerHash.
    static int GetHashCode(FieldSeqNode fsn)
    {
        return static_cast<int>(reinterpret_cast<intptr_t>(fsn.m_fieldHnd)) ^
               static_cast<int>(reinterpret_cast<intptr_t>(fsn.m_next));
    }

    static bool Equals(const FieldSeqNode& fsn1, const FieldSeqNode& fsn2)
    {
        return fsn1.m_fieldHnd == fsn2.m_fieldHnd && fsn1.m_next == fsn2.m_next;
    }
};

// This class canonicalizes field sequences.
class FieldSeqStore
{
    typedef JitHashTable<FieldSeqNode, /*KeyFuncs*/ FieldSeqNode, FieldSeqNode*> FieldSeqNodeCanonMap;

    CompAllocator         m_alloc;
    FieldSeqNodeCanonMap* m_canonMap;

    static FieldSeqNode s_notAField; // No value, just exists to provide an address.

    // Dummy variables to provide the addresses for the "pseudo field handle" statics below.
    static int FirstElemPseudoFieldStruct;
    static int ConstantIndexPseudoFieldStruct;

public:
    FieldSeqStore(CompAllocator alloc);

    // Returns the (canonical in the store) singleton field sequence for the given handle.
    FieldSeqNode* CreateSingleton(CORINFO_FIELD_HANDLE fieldHnd);

    // This is a special distinguished FieldSeqNode indicating that a constant does *not*
    // represent a valid field sequence.  This is "infectious", in the sense that appending it
    // (on either side) to any field sequence yields the "NotAField()" sequence.
    static FieldSeqNode* NotAField()
    {
        return &s_notAField;
    }

    // Returns the (canonical in the store) field sequence representing the concatenation of
    // the sequences represented by "a" and "b".  Assumes that "a" and "b" are canonical; that is,
    // they are the results of CreateSingleton, NotAField, or Append calls.  If either of the arguments
    // are the "NotAField" value, so is the result.
    FieldSeqNode* Append(FieldSeqNode* a, FieldSeqNode* b);

    // We have a few "pseudo" field handles:

    // This treats the constant offset of the first element of something as if it were a field.
    // Works for method table offsets of boxed structs, or first elem offset of arrays/strings.
    static CORINFO_FIELD_HANDLE FirstElemPseudoField;

    // If there is a constant index, we make a psuedo field to correspond to the constant added to
    // offset of the indexed field.  This keeps the field sequence structure "normalized", especially in the
    // case where the element type is a struct, so we might add a further struct field offset.
    static CORINFO_FIELD_HANDLE ConstantIndexPseudoField;

    static bool IsPseudoField(CORINFO_FIELD_HANDLE hnd)
    {
        return hnd == FirstElemPseudoField || hnd == ConstantIndexPseudoField;
    }
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
    GTF_USE_FLAGS   = 0x00000400, // Indicates that this node uses the flags bits.

    GTF_MAKE_CSE    = 0x00000800, // Hoisted expression: try hard to make this into CSE (see optPerformHoistExpr)
    GTF_DONT_CSE    = 0x00001000, // Don't bother CSE'ing this expr
    GTF_COLON_COND  = 0x00002000, // This node is conditionally executed (part of ? :)

    GTF_NODE_MASK   = GTF_COLON_COND,

    GTF_BOOLEAN     = 0x00004000, // value is known to be 0/1

    GTF_UNSIGNED    = 0x00008000, // With GT_CAST:   the source operand is an unsigned type
                                  // With operators: the specified node is an unsigned operator
    GTF_LATE_ARG    = 0x00010000, // The specified node is evaluated to a temp in the arg list, and this temp is added to gtCallLateArgs.
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

// NB: GTF_VAR_* and GTF_REG_* share the same namespace of flags.
// These flags are also used by GT_LCL_FLD, and the last-use (DEATH) flags are also used by GenTreeCopyOrReload.

    GTF_VAR_DEF             = 0x80000000, // GT_LCL_VAR -- this is a definition
    GTF_VAR_USEASG          = 0x40000000, // GT_LCL_VAR -- this is a partial definition, a use of the previous definition is implied
                                          // A partial definition usually occurs when a struct field is assigned to (s.f = ...) or
                                          // when a scalar typed variable is assigned to via a narrow store (*((byte*)&i) = ...).

// Last-use bits.
// Note that a node marked GTF_VAR_MULTIREG can only be a pure definition of all the fields, or a pure use of all the fields,
// so we don't need the equivalent of GTF_VAR_USEASG.

    GTF_VAR_MULTIREG_DEATH0 = 0x04000000, // GT_LCL_VAR -- The last-use bit for a lclVar (the first register if it is multireg).
    GTF_VAR_DEATH           = GTF_VAR_MULTIREG_DEATH0,
    GTF_VAR_MULTIREG_DEATH1 = 0x08000000, // GT_LCL_VAR -- The last-use bit for the second register of a multireg lclVar.
    GTF_VAR_MULTIREG_DEATH2 = 0x10000000, // GT_LCL_VAR -- The last-use bit for the third register of a multireg lclVar.
    GTF_VAR_MULTIREG_DEATH3 = 0x20000000, // GT_LCL_VAR -- The last-use bit for the fourth register of a multireg lclVar.
    GTF_VAR_DEATH_MASK      = GTF_VAR_MULTIREG_DEATH0 | GTF_VAR_MULTIREG_DEATH1 | GTF_VAR_MULTIREG_DEATH2 | GTF_VAR_MULTIREG_DEATH3,

// This is the amount we have to shift, plus the regIndex, to get the last use bit we want.
#define MULTIREG_LAST_USE_SHIFT 26

    GTF_VAR_MULTIREG        = 0x02000000, // This is a struct or (on 32-bit platforms) long variable that is used or defined
                                          // to/from a multireg source or destination (e.g. a call arg or return, or an op
                                          // that returns its result in multiple registers such as a long multiply).

    GTF_LIVENESS_MASK   = GTF_VAR_DEF | GTF_VAR_USEASG | GTF_VAR_DEATH_MASK,

    GTF_VAR_CAST        = 0x01000000, // GT_LCL_VAR -- has been explictly cast (variable node may not be type of local)
    GTF_VAR_ITERATOR    = 0x00800000, // GT_LCL_VAR -- this is a iterator reference in the loop condition
    GTF_VAR_CLONED      = 0x00400000, // GT_LCL_VAR -- this node has been cloned or is a clone
    GTF_VAR_CONTEXT     = 0x00200000, // GT_LCL_VAR -- this node is part of a runtime lookup
    GTF_VAR_FOLDED_IND  = 0x00100000, // GT_LCL_VAR -- this node was folded from *(typ*)&lclVar expression tree in fgMorphSmpOp()
                                      // where 'typ' is a small type and 'lclVar' corresponds to a normalized-on-store local variable.
                                      // This flag identifies such nodes in order to make sure that fgDoNormalizeOnStore() is called
                                      // on their parents in post-order morph.
                                      // Relevant for inlining optimizations (see fgInlinePrependStatements)

    GTF_VAR_ARR_INDEX   = 0x00000020, // The variable is part of (the index portion of) an array index expression.
                                      // Shares a value with GTF_REVERSE_OPS, which is meaningless for local var.

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

    GTF_MEMORYBARRIER_LOAD      = 0x40000000, // GT_MEMORYBARRIER -- Load barrier

    GTF_NOP_DEATH               = 0x40000000, // GT_NOP -- operand dies here

    GTF_FLD_VOLATILE            = 0x40000000, // GT_FIELD/GT_CLS_VAR -- same as GTF_IND_VOLATILE
    GTF_FLD_INITCLASS           = 0x20000000, // GT_FIELD/GT_CLS_VAR -- field access requires preceding class/static init helper

    GTF_INX_RNGCHK              = 0x80000000, // GT_INDEX/GT_INDEX_ADDR -- the array reference should be range-checked.
    GTF_INX_STRING_LAYOUT       = 0x40000000, // GT_INDEX -- this uses the special string array layout

    GTF_IND_TGT_NOT_HEAP        = 0x80000000, // GT_IND   -- the target is not on the heap
    GTF_IND_VOLATILE            = 0x40000000, // GT_IND   -- the load or store must use volatile sematics (this is a nop on X86)
    GTF_IND_NONFAULTING         = 0x20000000, // Operations for which OperIsIndir() is true  -- An indir that cannot fault.
                                              // Same as GTF_ARRLEN_NONFAULTING.
    GTF_IND_TGTANYWHERE         = 0x10000000, // GT_IND   -- the target could be anywhere
    GTF_IND_TLS_REF             = 0x08000000, // GT_IND   -- the target is accessed via TLS
    GTF_IND_ASG_LHS             = 0x04000000, // GT_IND   -- this GT_IND node is (the effective val) of the LHS of an
                                              //             assignment; don't evaluate it independently.
    GTF_IND_REQ_ADDR_IN_REG     = GTF_IND_ASG_LHS, // GT_IND  -- requires its addr operand to be evaluated
                                              // into a register. This flag is useful in cases where it
                                              // is required to generate register indirect addressing mode.
                                              // One such case is virtual stub calls on xarch.  This is only
                                              // valid in the backend, where GTF_IND_ASG_LHS is not necessary
                                              // (all such indirections will be lowered to GT_STOREIND).
    GTF_IND_UNALIGNED           = 0x02000000, // GT_IND   -- the load or store is unaligned (we assume worst case
                                              //             alignment of 1 byte)
    GTF_IND_INVARIANT           = 0x01000000, // GT_IND   -- the target is invariant (a prejit indirection)
    GTF_IND_ARR_INDEX           = 0x00800000, // GT_IND   -- the indirection represents an (SZ) array index
    GTF_IND_NONNULL             = 0x00400000, // GT_IND   -- the indirection never returns null (zero)

    GTF_IND_FLAGS = GTF_IND_VOLATILE | GTF_IND_TGTANYWHERE | GTF_IND_NONFAULTING | GTF_IND_TLS_REF | \
                    GTF_IND_UNALIGNED | GTF_IND_INVARIANT | GTF_IND_NONNULL | GTF_IND_ARR_INDEX | GTF_IND_TGT_NOT_HEAP,

    GTF_CLS_VAR_VOLATILE        = 0x40000000, // GT_FIELD/GT_CLS_VAR -- same as GTF_IND_VOLATILE
    GTF_CLS_VAR_INITCLASS       = 0x20000000, // GT_FIELD/GT_CLS_VAR -- same as GTF_FLD_INITCLASS
    GTF_CLS_VAR_ASG_LHS         = 0x04000000, // GT_CLS_VAR   -- this GT_CLS_VAR node is (the effective val) of the LHS
                                              //                 of an assignment; don't evaluate it independently.

    GTF_ADDRMODE_NO_CSE         = 0x80000000, // GT_ADD/GT_MUL/GT_LSH -- Do not CSE this node only, forms complex
                                              //                         addressing mode

    GTF_MUL_64RSLT              = 0x40000000, // GT_MUL     -- produce 64-bit result

    GTF_RELOP_NAN_UN            = 0x80000000, // GT_<relop> -- Is branch taken if ops are NaN?
    GTF_RELOP_JMP_USED          = 0x40000000, // GT_<relop> -- result of compare used for jump or ?:
    GTF_RELOP_QMARK             = 0x20000000, // GT_<relop> -- the node is the condition for ?:
    GTF_RELOP_ZTT               = 0x08000000, // GT_<relop> -- Loop test cloned for converting while-loops into do-while
                                              //               with explicit "loop test" in the header block.

    GTF_JCMP_EQ                 = 0x80000000, // GTF_JCMP_EQ  -- Branch on equal rather than not equal
    GTF_JCMP_TST                = 0x40000000, // GTF_JCMP_TST -- Use bit test instruction rather than compare against zero instruction

    GTF_RET_MERGED              = 0x80000000, // GT_RETURN -- This is a return generated during epilog merging.

    GTF_QMARK_CAST_INSTOF       = 0x80000000, // GT_QMARK -- Is this a top (not nested) level qmark created for
                                              //             castclass or instanceof?

    GTF_BOX_VALUE               = 0x80000000, // GT_BOX -- "box" is on a value type

    GTF_ICON_HDL_MASK           = 0xF0000000, // Bits used by handle types below
    GTF_ICON_SCOPE_HDL          = 0x10000000, // GT_CNS_INT -- constant is a scope handle
    GTF_ICON_CLASS_HDL          = 0x20000000, // GT_CNS_INT -- constant is a class handle
    GTF_ICON_METHOD_HDL         = 0x30000000, // GT_CNS_INT -- constant is a method handle
    GTF_ICON_FIELD_HDL          = 0x40000000, // GT_CNS_INT -- constant is a field handle
    GTF_ICON_STATIC_HDL         = 0x50000000, // GT_CNS_INT -- constant is a handle to static data
    GTF_ICON_STR_HDL            = 0x60000000, // GT_CNS_INT -- constant is a string handle
    GTF_ICON_CONST_PTR          = 0x70000000, // GT_CNS_INT -- constant is a pointer to immutable data, (e.g. IAT_PPVALUE)
    GTF_ICON_GLOBAL_PTR         = 0x80000000, // GT_CNS_INT -- constant is a pointer to mutable data (e.g. from the VM state)
    GTF_ICON_VARG_HDL           = 0x90000000, // GT_CNS_INT -- constant is a var arg cookie handle
    GTF_ICON_PINVKI_HDL         = 0xA0000000, // GT_CNS_INT -- constant is a pinvoke calli handle
    GTF_ICON_TOKEN_HDL          = 0xB0000000, // GT_CNS_INT -- constant is a token handle (other than class, method or field)
    GTF_ICON_TLS_HDL            = 0xC0000000, // GT_CNS_INT -- constant is a TLS ref with offset
    GTF_ICON_FTN_ADDR           = 0xD0000000, // GT_CNS_INT -- constant is a function address
    GTF_ICON_CIDMID_HDL         = 0xE0000000, // GT_CNS_INT -- constant is a class ID or a module ID
    GTF_ICON_BBC_PTR            = 0xF0000000, // GT_CNS_INT -- constant is a basic block count pointer

    GTF_ICON_FIELD_OFF          = 0x08000000, // GT_CNS_INT -- constant is a field offset
    GTF_ICON_SIMD_COUNT         = 0x04000000, // GT_CNS_INT -- constant is Vector<T>.Count

    GTF_ICON_INITCLASS          = 0x02000000, // GT_CNS_INT -- Constant is used to access a static that requires preceding
                                              //               class/static init helper.  In some cases, the constant is
                                              //               the address of the static field itself, and in other cases
                                              //               there's an extra layer of indirection and it is the address
                                              //               of the cell that the runtime will fill in with the address
                                              //               of the static field; in both of those cases, the constant
                                              //               is what gets flagged.

    GTF_BLK_VOLATILE            = GTF_IND_VOLATILE,  // GT_ASG, GT_STORE_BLK, GT_STORE_OBJ, GT_STORE_DYNBLK -- is a volatile block operation
    GTF_BLK_UNALIGNED           = GTF_IND_UNALIGNED, // GT_ASG, GT_STORE_BLK, GT_STORE_OBJ, GT_STORE_DYNBLK -- is an unaligned block operation

    GTF_OVERFLOW                = 0x10000000, // Supported for: GT_ADD, GT_SUB, GT_MUL and GT_CAST.
                                              // Requires an overflow check. Use gtOverflow(Ex)() to check this flag.

    GTF_DIV_BY_CNS_OPT          = 0x80000000, // GT_DIV -- Uses the division by constant optimization to compute this division

    GTF_ARR_BOUND_INBND         = 0x80000000, // GT_ARR_BOUNDS_CHECK -- have proved this check is always in-bounds

    GTF_ARRLEN_ARR_IDX          = 0x80000000, // GT_ARR_LENGTH -- Length which feeds into an array index expression
    GTF_ARRLEN_NONFAULTING      = 0x20000000, // GT_ARR_LENGTH  -- An array length operation that cannot fault. Same as GT_IND_NONFAULTING.

    GTF_SIMDASHW_OP             = 0x80000000, // GT_HWINTRINSIC -- Indicates that the structHandle should be gotten from gtGetStructHandleForSIMD
                                              //                   rather than from gtGetStructHandleForHWSIMD.

    // Flag used by assertion prop to indicate that a type is a TYP_LONG
#ifdef TARGET_64BIT
    GTF_ASSERTION_PROP_LONG     = 0x00000001,
#endif // TARGET_64BIT
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

#if ASSERTION_PROP
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
#endif

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

public:
    // The register number is stored in a small format (8 bits), but the getters return and the setters take
    // a full-size (unsigned) format, to localize the casts here.

    bool canBeContained() const;

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
        return isContained() && (OperGet() == GT_CNS_DBL);
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
        return ((isContained() && (isMemoryOp() || (OperGet() == GT_LCL_VAR) || (OperGet() == GT_CNS_DBL))) ||
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
    bool gtHasReg() const;

    int GetRegisterDstCount(Compiler* compiler) const;

    regMaskTP gtGetRegMask() const;

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

    static const unsigned short gtOperKindTable[];

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
    // Returns the operKind with the GTK_EX_OP bit removed (the
    // kind of operator, unary or binary, that is extended).
    static unsigned StripExOp(unsigned opKind)
    {
        return opKind & ~GTK_EXOP;
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
            assert(OperIs(GT_NOP, GT_CALL, GT_COMMA) || OperIsCompare() || OperIsLong() || OperIsSIMD() ||
                   OperIsHWIntrinsic());
            return false;
        }

        return true;
    }

    bool IsLIR() const
    {
        if ((OperKind(gtOper) & GTK_NOTLIR) != 0)
        {
            return false;
        }

        switch (gtOper)
        {
            case GT_NOP:
                // NOPs may only be present in LIR if they do not produce a value.
                return IsNothingNode();

            case GT_LIST:
                // LIST nodes may not be present in a block's LIR sequence, but they may
                // be present as children of an LIR node.
                return (gtNext == nullptr) && (gtPrev == nullptr);

            case GT_ADDR:
            {
                // ADDR ndoes may only be present in LIR if the location they refer to is not a
                // local, class variable, or IND node.
                GenTree*   location   = gtGetOp1();
                genTreeOps locationOp = location->OperGet();
                return !location->IsLocal() && (locationOp != GT_CLS_VAR) && (locationOp != GT_IND);
            }

            default:
                // All other nodes are assumed to be correct.
                return true;
        }
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

    static bool StaticOperIs(genTreeOps operCompare, genTreeOps oper)
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
        return (OperKind(gtOper) & GTK_CONST) != 0;
    }

    bool OperIsConst() const
    {
        return (OperKind(gtOper) & GTK_CONST) != 0;
    }

    static bool OperIsLeaf(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_LEAF) != 0;
    }

    bool OperIsLeaf() const
    {
        return (OperKind(gtOper) & GTK_LEAF) != 0;
    }

    static bool OperIsCompare(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_RELOP) != 0;
    }

    static bool OperIsLocal(genTreeOps gtOper)
    {
        bool result = (OperKind(gtOper) & GTK_LOCAL) != 0;
        assert(result == (gtOper == GT_LCL_VAR || gtOper == GT_PHI_ARG || gtOper == GT_LCL_FLD ||
                          gtOper == GT_STORE_LCL_VAR || gtOper == GT_STORE_LCL_FLD));
        return result;
    }

    static bool OperIsLocalAddr(genTreeOps gtOper)
    {
        return (gtOper == GT_LCL_VAR_ADDR || gtOper == GT_LCL_FLD_ADDR);
    }

    static bool OperIsLocalField(genTreeOps gtOper)
    {
        return (gtOper == GT_LCL_FLD || gtOper == GT_LCL_FLD_ADDR || gtOper == GT_STORE_LCL_FLD);
    }

    inline bool OperIsLocalField() const
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

    bool IsConstInitVal()
    {
        return (gtOper == GT_CNS_INT) || (OperIsInitVal() && (gtGetOp1()->gtOper == GT_CNS_INT));
    }

    bool OperIsBlkOp();
    bool OperIsCopyBlkOp();
    bool OperIsInitBlkOp();
    bool OperIsDynBlkOp();

    static bool OperIsBlk(genTreeOps gtOper)
    {
        return ((gtOper == GT_BLK) || (gtOper == GT_OBJ) || (gtOper == GT_DYN_BLK) || (gtOper == GT_STORE_BLK) ||
                (gtOper == GT_STORE_OBJ) || (gtOper == GT_STORE_DYN_BLK));
    }

    bool OperIsBlk() const
    {
        return OperIsBlk(OperGet());
    }

    static bool OperIsDynBlk(genTreeOps gtOper)
    {
        return ((gtOper == GT_DYN_BLK) || (gtOper == GT_STORE_DYN_BLK));
    }

    bool OperIsDynBlk() const
    {
        return OperIsDynBlk(OperGet());
    }

    static bool OperIsStoreBlk(genTreeOps gtOper)
    {
        return ((gtOper == GT_STORE_BLK) || (gtOper == GT_STORE_OBJ) || (gtOper == GT_STORE_DYN_BLK));
    }

    bool OperIsStoreBlk() const
    {
        return OperIsStoreBlk(OperGet());
    }

    bool OperIsPutArgSplit() const
    {
#if FEATURE_ARG_SPLIT
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

    bool OperIsLocalAddr() const
    {
        return OperIsLocalAddr(OperGet());
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

    bool OperIsCompare() const
    {
        return (OperKind(gtOper) & GTK_RELOP) != 0;
    }

    static bool OperIsLogical(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_LOGOP) != 0;
    }

    bool OperIsLogical() const
    {
        return (OperKind(gtOper) & GTK_LOGOP) != 0;
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
#if !defined(TARGET_64BIT)
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

    static bool OperIsRelop(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_RELOP) != 0;
    }

    bool OperIsRelop() const
    {
        return OperIsRelop(gtOper);
    }

#ifdef FEATURE_SIMD
    bool isCommutativeSIMDIntrinsic();
#else  // !
    bool isCommutativeSIMDIntrinsic()
    {
        return false;
    }
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
    bool isCommutativeHWIntrinsic() const;
    bool isContainableHWIntrinsic() const;
    bool isRMWHWIntrinsic(Compiler* comp);
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
#endif // FEATURE_HW_INTRINSICS

    static bool OperIsCommutative(genTreeOps gtOper)
    {
        return (OperKind(gtOper) & GTK_COMMUTE) != 0;
    }

    bool OperIsCommutative()
    {
        return OperIsCommutative(gtOper) || (OperIsSIMD(gtOper) && isCommutativeSIMDIntrinsic()) ||
               (OperIsHWIntrinsic(gtOper) && isCommutativeHWIntrinsic());
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
        return gtOper == GT_IND || gtOper == GT_STOREIND || gtOper == GT_NULLCHECK || OperIsBlk(gtOper);
    }

    static bool OperIsIndirOrArrLength(genTreeOps gtOper)
    {
        return OperIsIndir(gtOper) || (gtOper == GT_ARR_LENGTH);
    }

    bool OperIsIndir() const
    {
        return OperIsIndir(gtOper);
    }

    bool OperIsIndirOrArrLength() const
    {
        return OperIsIndirOrArrLength(gtOper);
    }

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

    bool OperIsStore() const
    {
        return OperIsStore(gtOper);
    }

    static bool OperIsStore(genTreeOps gtOper)
    {
        return (gtOper == GT_STOREIND || gtOper == GT_STORE_LCL_VAR || gtOper == GT_STORE_LCL_FLD ||
                OperIsStoreBlk(gtOper) || OperIsAtomicOp(gtOper));
    }

    // This is here for cleaner FEATURE_SIMD #ifdefs.
    static bool OperIsSIMD(genTreeOps gtOper)
    {
#ifdef FEATURE_SIMD
        return gtOper == GT_SIMD;
#else  // !FEATURE_SIMD
        return false;
#endif // !FEATURE_SIMD
    }

    bool OperIsSIMD() const
    {
        return OperIsSIMD(gtOper);
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

    bool OperIsSimdOrHWintrinsic() const
    {
        return OperIsSIMD() || OperIsHWIntrinsic();
    }

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
        return (gtOper == GT_JTRUE) || (gtOper == GT_JCMP) || (gtOper == GT_JCC);
    }

    static bool OperIsBoundsCheck(genTreeOps op)
    {
        if (op == GT_ARR_BOUNDS_CHECK)
        {
            return true;
        }
#ifdef FEATURE_SIMD
        if (op == GT_SIMD_CHK)
        {
            return true;
        }
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
        if (op == GT_HW_INTRINSIC_CHK)
        {
            return true;
        }
#endif // FEATURE_HW_INTRINSICS
        return false;
    }

    bool OperIsBoundsCheck() const
    {
        return OperIsBoundsCheck(OperGet());
    }

#ifdef DEBUG
    bool NullOp1Legal() const
    {
        assert(OperIsSimple(gtOper));
        switch (gtOper)
        {
            case GT_LEA:
            case GT_RETFILT:
            case GT_NOP:
#ifdef FEATURE_HW_INTRINSICS
            case GT_HWINTRINSIC:
#endif // FEATURE_HW_INTRINSICS
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
            case GT_LIST:
            case GT_INTRINSIC:
            case GT_LEA:
#ifdef FEATURE_SIMD
            case GT_SIMD:
#endif // !FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
            case GT_HWINTRINSIC:
#endif // FEATURE_HW_INTRINSICS

#if defined(TARGET_ARM)
            case GT_PUTARG_REG:
#endif // defined(TARGET_ARM)

                return true;
            default:
                return false;
        }
    }

    static inline bool RequiresNonNullOp2(genTreeOps oper);
    bool IsValidCallArgument();
#endif // DEBUG

    inline bool IsFPZero() const;
    inline bool IsIntegralConst(ssize_t constVal) const;
    inline bool IsIntegralConstVector(ssize_t constVal) const;
    inline bool IsSIMDZero() const;

    inline bool IsBoxedValue();

    static bool OperIsList(genTreeOps gtOper)
    {
        return gtOper == GT_LIST;
    }

    bool OperIsList() const
    {
        return OperIsList(gtOper);
    }

    static bool OperIsAnyList(genTreeOps gtOper)
    {
        return OperIsList(gtOper);
    }

    bool OperIsAnyList() const
    {
        return OperIsAnyList(gtOper);
    }

    inline GenTree* gtGetOp1() const;

    // Directly return op2. Asserts the node is binary. Might return nullptr if the binary node allows
    // a nullptr op2, such as GT_LIST. This is more efficient than gtGetOp2IfPresent() if you know what
    // node type you have.
    inline GenTree* gtGetOp2() const;

    // The returned pointer might be nullptr if the node is not binary, or if non-null op2 is not required.
    inline GenTree* gtGetOp2IfPresent() const;

    // Given a tree node, if this is a child of that node, return the pointer to the child node so that it
    // can be modified; otherwise, return null.
    GenTree** gtGetChildPointer(GenTree* parent) const;

    // Given a tree node, if this node uses that node, return the use as an out parameter and return true.
    // Otherwise, return false.
    bool TryGetUse(GenTree* def, GenTree*** use);

private:
    bool TryGetUseList(GenTree* def, GenTree*** use);

    bool TryGetUseBinOp(GenTree* def, GenTree*** use);

public:
    // Get the parent of this node, and optionally capture the pointer to the child so that it can be modified.
    GenTree* gtGetParent(GenTree*** parentChildPtrPtr) const;

    void ReplaceOperand(GenTree** useEdge, GenTree* replacement);

    inline GenTree* gtEffectiveVal(bool commaOnly = false);

    inline GenTree* gtCommaAssignVal();

    // Tunnel through any GT_RET_EXPRs
    GenTree* gtRetExprVal(BasicBlockFlags* pbbFlags = nullptr);

    inline GenTree* gtSkipPutArgType();

    // Return the child of this node if it is a GT_RELOAD or GT_COPY; otherwise simply return the node itself
    inline GenTree* gtSkipReloadOrCopy();

    // Returns true if it is a call node returning its value in more than one register
    inline bool IsMultiRegCall() const;

    // Returns true if it is a struct lclVar node residing in multiple registers.
    inline bool IsMultiRegLclVar() const;

    // Returns true if it is a node returning its value in more than one register
    inline bool IsMultiRegNode() const;

    // Returns the number of registers defined by a multireg node.
    unsigned GetMultiRegCount();

    // Returns the regIndex'th register defined by a possibly-multireg node.
    regNumber GetRegByIndex(int regIndex);

    // Returns the type of the regIndex'th register defined by a multi-reg node.
    var_types GetRegTypeByIndex(int regIndex);

    // Returns the GTF flag equivalent for the regIndex'th register of a multi-reg node.
    GenTreeFlags GetRegSpillFlagByIdx(int regIndex) const;

    // Last-use information for either GenTreeLclVar or GenTreeCopyOrReload nodes.
private:
    GenTreeFlags GetLastUseBit(int regIndex);

public:
    bool IsLastUse(int regIndex);
    bool HasLastUse();
    void SetLastUse(int regIndex);
    void ClearLastUse(int regIndex);

    // Returns true if it is a GT_COPY or GT_RELOAD node
    inline bool IsCopyOrReload() const;

    // Returns true if it is a GT_COPY or GT_RELOAD of a multi-reg call node
    inline bool IsCopyOrReloadOfMultiRegCall() const;

    bool OperRequiresAsgFlag();

    bool OperRequiresCallFlag(Compiler* comp);

    bool OperMayThrow(Compiler* comp);

    unsigned GetScaleIndexMul();
    unsigned GetScaleIndexShf();
    unsigned GetScaledIndex();

    // Returns true if "addr" is a GT_ADD node, at least one of whose arguments is an integer
    // (<= 32 bit) constant.  If it returns true, it sets "*offset" to (one of the) constant value(s), and
    // "*addr" to the other argument.
    bool IsAddWithI32Const(GenTree** addr, int* offset);

public:
    static unsigned char s_gtNodeSizes[];
#if NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS
    static unsigned char s_gtTrueSizes[];
#endif
#if COUNT_AST_OPERS
    static LONG s_gtNodeCounts[];
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

#if defined(DEBUG) || NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS || DUMP_FLOWGRAPHS
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

    void SetOper(genTreeOps oper, ValueNumberUpdate vnUpdate = CLEAR_VN); // set gtOper
    void SetOperResetFlags(genTreeOps oper);                              // set gtOper and reset flags

    void ChangeOperConst(genTreeOps oper); // ChangeOper(constOper)
    // set gtOper and only keep GTF_COMMON_MASK flags
    void ChangeOper(genTreeOps oper, ValueNumberUpdate vnUpdate = CLEAR_VN);
    void ChangeOperUnchecked(genTreeOps oper);
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

    // Returns "true" iff 'this' is a GT_LCL_FLD or GT_STORE_LCL_FLD on which the type
    // is not the same size as the type of the GT_LCL_VAR.
    bool IsPartialLclFld(Compiler* comp);

    // Returns "true" iff "this" defines a local variable.  Requires "comp" to be the
    // current compilation.  If returns "true", sets "*pLclVarTree" to the
    // tree for the local that is defined, and, if "pIsEntire" is non-null, sets "*pIsEntire" to
    // true or false, depending on whether the assignment writes to the entirety of the local
    // variable, or just a portion of it.
    bool DefinesLocal(Compiler* comp, GenTreeLclVarCommon** pLclVarTree, bool* pIsEntire = nullptr);

    bool IsLocalAddrExpr(Compiler*             comp,
                         GenTreeLclVarCommon** pLclVarTree,
                         FieldSeqNode**        pFldSeq,
                         ssize_t*              pOffset = nullptr);

    // Simpler variant of the above which just returns the local node if this is an expression that
    // yields an address into a local
    GenTreeLclVarCommon* IsLocalAddrExpr();

    // Determine if this tree represents the value of an entire implicit byref parameter,
    // and if so return the tree for the parameter.
    GenTreeLclVar* IsImplicitByrefParameterValue(Compiler* compiler);

    // Determine if this is a LclVarCommon node and return some additional info about it in the
    // two out parameters.
    bool IsLocalExpr(Compiler* comp, GenTreeLclVarCommon** pLclVarTree, FieldSeqNode** pFldSeq);

    // Determine whether this is an assignment tree of the form X = X (op) Y,
    // where Y is an arbitrary tree, and X is a lclVar.
    unsigned IsLclVarUpdateTree(GenTree** otherTree, genTreeOps* updateOper);

    // If returns "true", "this" may represent the address of a static or instance field
    // (or a field of such a field, in the case of an object field of type struct).
    // If returns "true", then either "*pObj" is set to the object reference,
    // or "*pStatic" is set to the baseAddr or offset to be added to the "*pFldSeq"
    // Only one of "*pObj" or "*pStatic" will be set, the other one will be null.
    // The boolean return value only indicates that "this" *may* be a field address
    // -- the field sequence must also be checked.
    // If it is a field address, the field sequence will be a sequence of length >= 1,
    // starting with an instance or static field, and optionally continuing with struct fields.
    bool IsFieldAddr(Compiler* comp, GenTree** pObj, GenTree** pStatic, FieldSeqNode** pFldSeq);

    // Requires "this" to be the address of an array (the child of a GT_IND labeled with GTF_IND_ARR_INDEX).
    // Sets "pArr" to the node representing the array (either an array object pointer, or perhaps a byref to the some
    // element).
    // Sets "*pArrayType" to the class handle for the array type.
    // Sets "*inxVN" to the value number inferred for the array index.
    // Sets "*pFldSeq" to the sequence, if any, of struct fields used to index into the array element.
    void ParseArrayAddress(
        Compiler* comp, struct ArrayInfo* arrayInfo, GenTree** pArr, ValueNum* pInxVN, FieldSeqNode** pFldSeq);

    // Helper method for the above.
    void ParseArrayAddressWork(Compiler*       comp,
                               target_ssize_t  inputMul,
                               GenTree**       pArr,
                               ValueNum*       pInxVN,
                               target_ssize_t* pOffset,
                               FieldSeqNode**  pFldSeq);

    // Requires "this" to be a GT_IND.  Requires the outermost caller to set "*pFldSeq" to nullptr.
    // Returns true if it is an array index expression, or access to a (sequence of) struct field(s)
    // within a struct array element.  If it returns true, sets *arrayInfo to the array information, and sets *pFldSeq
    // to the sequence of struct field accesses.
    bool ParseArrayElemForm(Compiler* comp, ArrayInfo* arrayInfo, FieldSeqNode** pFldSeq);

    // Requires "this" to be the address of a (possible) array element (or struct field within that).
    // If it is, sets "*arrayInfo" to the array access info, "*pFldSeq" to the sequence of struct fields
    // accessed within the array element, and returns true.  If not, returns "false".
    bool ParseArrayElemAddrForm(Compiler* comp, ArrayInfo* arrayInfo, FieldSeqNode** pFldSeq);

    // Requires "this" to be an int expression.  If it is a sequence of one or more integer constants added together,
    // returns true and sets "*pFldSeq" to the sequence of fields with which those constants are annotated.
    bool ParseOffsetForm(Compiler* comp, FieldSeqNode** pFldSeq);

    // Labels "*this" as an array index expression: label all constants and variables that could contribute, as part of
    // an affine expression, to the value of the of the index.
    void LabelIndex(Compiler* comp, bool isConst = true);

    // Assumes that "this" occurs in a context where it is being dereferenced as the LHS of an assignment-like
    // statement (assignment, initblk, or copyblk).  The "width" should be the number of bytes copied by the
    // operation.  Returns "true" if "this" is an address of (or within)
    // a local variable; sets "*pLclVarTree" to that local variable instance; and, if "pIsEntire" is non-null,
    // sets "*pIsEntire" to true if this assignment writes the full width of the local.
    bool DefinesLocalAddr(Compiler* comp, unsigned width, GenTreeLclVarCommon** pLclVarTree, bool* pIsEntire);

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

    bool IsUnsigned() const
    {
        return ((gtFlags & GTF_UNSIGNED) != 0);
    }

    void SetUnsigned()
    {
        assert(OperIs(GT_ADD, GT_SUB, GT_MUL, GT_CAST));
        gtFlags |= GTF_UNSIGNED;
    }

    void ClearUnsigned()
    {
        assert(OperIs(GT_ADD, GT_SUB, GT_MUL, GT_CAST));
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

    void SetAllEffectsFlags(GenTree* source, GenTree* otherSource)
    {
        SetAllEffectsFlags((source->gtFlags | otherSource->gtFlags) & GTF_ALL_EFFECT);
    }

    void SetAllEffectsFlags(GenTreeFlags sourceFlags)
    {
        assert((sourceFlags & ~GTF_ALL_EFFECT) == 0);

        gtFlags &= ~GTF_ALL_EFFECT;
        gtFlags |= sourceFlags;
    }

    inline bool IsCnsIntOrI() const;

    inline bool IsIntegralConst() const;

    inline bool IsIntCnsFitsInI32(); // Constant fits in INT32

    inline bool IsCnsFltOrDbl() const;

    inline bool IsCnsNonZeroFltOrDbl();

    bool IsIconHandle() const
    {
        assert(gtOper == GT_CNS_INT);
        return (gtFlags & GTF_ICON_HDL_MASK) ? true : false;
    }

    bool IsIconHandle(GenTreeFlags handleType) const
    {
        assert(gtOper == GT_CNS_INT);
        assert((handleType & GTF_ICON_HDL_MASK) != 0); // check that handleType is one of the valid GTF_ICON_* values
        assert((handleType & ~GTF_ICON_HDL_MASK) == 0);
        return (gtFlags & GTF_ICON_HDL_MASK) == handleType;
    }

    // Return just the part of the flags corresponding to the GTF_ICON_*_HDL flag. For example,
    // GTF_ICON_SCOPE_HDL. The tree node must be a const int, but it might not be a handle, in which
    // case we'll return zero.
    GenTreeFlags GetIconHandleFlag() const
    {
        assert(gtOper == GT_CNS_INT);
        return (gtFlags & GTF_ICON_HDL_MASK);
    }

    // Mark this node as no longer being a handle; clear its GTF_ICON_*_HDL bits.
    void ClearIconHandleMask()
    {
        assert(gtOper == GT_CNS_INT);
        gtFlags &= ~GTF_ICON_HDL_MASK;
    }

    // Return true if the two GT_CNS_INT trees have the same handle flag (GTF_ICON_*_HDL).
    static bool SameIconHandleFlag(GenTree* t1, GenTree* t2)
    {
        return t1->GetIconHandleFlag() == t2->GetIconHandleFlag();
    }

    bool IsArgPlaceHolderNode() const
    {
        return OperGet() == GT_ARGPLACE;
    }
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
    bool       gtIsValid64RsltMul();
    static int gtDispFlags(GenTreeFlags flags, GenTreeDebugFlags debugFlags);
#endif

    // cast operations
    inline var_types  CastFromType();
    inline var_types& CastToType();

    // Returns "true" iff "this" is a phi-related node (i.e. a GT_PHI_ARG, GT_PHI, or a PhiDefn).
    bool IsPhiNode();

    // Returns "true" iff "*this" is an assignment (GT_ASG) tree that defines an SSA name (lcl = phi(...));
    bool IsPhiDefn();

    // Returns "true" iff "*this" is a statement containing an assignment that defines an SSA name (lcl = phi(...));

    // Because of the fact that we hid the assignment operator of "BitSet" (in DEBUG),
    // we can't synthesize an assignment operator.
    // TODO-Cleanup: Could change this w/o liveset on tree nodes
    // (This is also necessary for the VTable trick.)
    GenTree()
    {
    }

    // Returns the number of children of the current node.
    unsigned NumChildren();

    // Requires "childNum < NumChildren()".  Returns the "n"th child of "this."
    GenTree* GetChild(unsigned childNum);

    // Returns an iterator that will produce the use edge to each operand of this node. Differs
    // from the sequence of nodes produced by a loop over `GetChild` in its handling of call, phi,
    // and block op nodes.
    GenTreeUseEdgeIterator UseEdgesBegin();
    GenTreeUseEdgeIterator UseEdgesEnd();

    IteratorPair<GenTreeUseEdgeIterator> UseEdges();

    // Returns an iterator that will produce each operand of this node. Differs from the sequence
    // of nodes produced by a loop over `GetChild` in its handling of call, phi, and block op
    // nodes.
    GenTreeOperandIterator OperandsBegin();
    GenTreeOperandIterator OperandsEnd();

    // Returns a range that will produce the operands of this node in use order.
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
    // hot code, as it affords better opportunities for inlining and acheives shorter dynamic path lengths when
    // deciding how operands need to be accessed.
    //
    // Note that this function does not respect `GTF_REVERSE_OPS` and `gtEvalSizeFirst`. This is always safe in LIR,
    // but may be dangerous in HIR if for some reason you need to visit operands in the order in which they will
    // execute.
    template <typename TVisitor>
    void VisitOperands(TVisitor visitor);

private:
    template <typename TVisitor>
    VisitResult VisitListOperands(TVisitor visitor);

    template <typename TVisitor>
    void VisitBinOpOperands(TVisitor visitor);

public:
    bool Precedes(GenTree* other);

    bool IsInvariant() const;

    bool IsReuseRegVal() const
    {
        // This can be extended to non-constant nodes, but not to local or indir nodes.
        if (IsInvariant() && ((gtFlags & GTF_REUSE_REG_VAL) != 0))
        {
            return true;
        }
        return false;
    }
    void SetReuseRegVal()
    {
        assert(IsInvariant());
        gtFlags |= GTF_REUSE_REG_VAL;
    }
    void ResetReuseRegVal()
    {
        assert(IsInvariant());
        gtFlags &= ~GTF_REUSE_REG_VAL;
    }

    void SetIndirExceptionFlags(Compiler* comp);

#if MEASURE_NODE_SIZE
    static void DumpNodeSizes(FILE* fp);
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
// the RHS of a GT_ASG node. The LHS of the ASG node is always a GT_LCL_VAR
// node, that is a definition for the same local variable referenced by
// all the used PHI_ARG nodes:
//
//   ASG(LCL_VAR(lcl7), PHI(PHI_ARG(lcl7), PHI_ARG(lcl7), PHI_ARG(lcl7)))
//
// PHI nodes are also present in LIR, where GT_STORE_LCL_VAR replaces the
// ASG node.
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
// The use edges of a node may not correspond exactly to the nodes on the other ends of its use edges: in
// particular, GT_LIST nodes are expanded into their component parts. This differs from the behavior of
// GenTree::GetChildPointer(), which does not expand lists.
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
        CALL_INSTANCE     = 0,
        CALL_ARGS         = 1,
        CALL_LATE_ARGS    = 2,
        CALL_CONTROL_EXPR = 3,
        CALL_COOKIE       = 4,
        CALL_ADDRESS      = 5,
        CALL_TERMINAL     = 6,
    };

    typedef void (GenTreeUseEdgeIterator::*AdvanceFn)();

    AdvanceFn m_advance;
    GenTree*  m_node;
    GenTree** m_edge;
    // Pointer sized state storage, GenTreeArgList* or GenTreePhi::Use* or GenTreeCall::Use* currently.
    void* m_statePtr;
    // Integer sized state storage, usually the operand index for non-list based nodes.
    int m_state;

    GenTreeUseEdgeIterator(GenTree* node);

    // Advance functions for special nodes
    void AdvanceCmpXchg();
    void AdvanceBoundsChk();
    void AdvanceArrElem();
    void AdvanceArrOffset();
    void AdvanceDynBlk();
    void AdvanceStoreDynBlk();
    void AdvanceFieldList();
    void AdvancePhi();

    template <bool ReverseOperands>
    void           AdvanceBinOp();
    void           SetEntryStateForBinOp();

    // An advance function for list-like nodes (Phi, SIMDIntrinsicInitN, FieldList)
    void AdvanceList();
    void SetEntryStateForList(GenTreeArgList* list);

    // The advance function for call nodes
    template <int state>
    void          AdvanceCall();

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
//                         the covers and comes with the same caveats
//                         w.r.t. `GetChild`.
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
        assert(oper == GT_NOP || oper == GT_RETURN || oper == GT_RETFILT || OperIsBlk(oper));
    }

    // returns true if we will use the division by constant optimization for this node.
    bool UsesDivideByConstOptimized(Compiler* comp);

    // checks if we will use the division by constant optimization this node
    // then sets the flag GTF_DIV_BY_CNS_OPT and GTF_DONT_CSE on the constant
    void CheckDivideByConstOptimized(Compiler* comp);

    // True if this node is marked as using the division by constant optimization
    bool MarkedDivideByConstOptimized() const
    {
        return (gtFlags & GTF_DIV_BY_CNS_OPT) != 0;
    }

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
    FieldSeqNode* gtFieldSeq;

#ifdef DEBUG
    // If the value represents target address, holds the method handle to that target which is used
    // to fetch target method name and display in the disassembled code.
    size_t gtTargetHandle = 0;
#endif

    GenTreeIntCon(var_types type, ssize_t value DEBUGARG(bool largeNode = false))
        : GenTreeIntConCommon(GT_CNS_INT, type DEBUGARG(largeNode))
        , gtIconVal(value)
        , gtCompileTimeHandle(0)
        , gtFieldSeq(FieldSeqStore::NotAField())
    {
    }

    GenTreeIntCon(var_types type, ssize_t value, FieldSeqNode* fields DEBUGARG(bool largeNode = false))
        : GenTreeIntConCommon(GT_CNS_INT, type DEBUGARG(largeNode))
        , gtIconVal(value)
        , gtCompileTimeHandle(0)
        , gtFieldSeq(fields)
    {
        assert(fields != nullptr);
    }

    void FixupInitBlkValue(var_types asgType);

#ifdef TARGET_64BIT
    void TruncateOrSignExtend32()
    {
        if (gtFlags & GTF_UNSIGNED)
        {
            gtIconVal = UINT32(gtIconVal);
        }
        else
        {
            gtIconVal = INT32(gtIconVal);
        }
    }
#endif // TARGET_64BIT

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

/* gtDblCon -- double  constant (GT_CNS_DBL) */

struct GenTreeDblCon : public GenTree
{
    double gtDconVal;

    bool isBitwiseEqual(GenTreeDblCon* other)
    {
        unsigned __int64 bits      = *(unsigned __int64*)(&gtDconVal);
        unsigned __int64 otherBits = *(unsigned __int64*)(&(other->gtDconVal));
        return (bits == otherBits);
    }

    GenTreeDblCon(double val, var_types type = TYP_DOUBLE) : GenTree(GT_CNS_DBL, type), gtDconVal(val)
    {
        assert(varTypeIsFloating(type));
    }
#if DEBUGGABLE_GENTREE
    GenTreeDblCon() : GenTree()
    {
    }
#endif
};

/* gtStrCon -- string  constant (GT_CNS_STR) */

struct GenTreeStrCon : public GenTree
{
    unsigned              gtSconCPX;
    CORINFO_MODULE_HANDLE gtScpHnd;

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

// Common supertype of LCL_VAR, LCL_FLD, REG_VAR, PHI_ARG
// This inherits from UnOp because lclvar stores are Unops
struct GenTreeLclVarCommon : public GenTreeUnOp
{
private:
    unsigned _gtLclNum; // The local number. An index into the Compiler::lvaTable array.
    unsigned _gtSsaNum; // The SSA number.

public:
    GenTreeLclVarCommon(genTreeOps oper, var_types type, unsigned lclNum DEBUGARG(bool largeNode = false))
        : GenTreeUnOp(oper, type DEBUGARG(largeNode))
    {
        SetLclNum(lclNum);
    }

    unsigned GetLclNum() const
    {
        return _gtLclNum;
    }

    void SetLclNum(unsigned lclNum)
    {
        _gtLclNum = lclNum;
        _gtSsaNum = SsaConfig::RESERVED_SSA_NUM;
    }

    uint16_t GetLclOffs() const;

    unsigned GetSsaNum() const
    {
        return _gtSsaNum;
    }

    void SetSsaNum(unsigned ssaNum)
    {
        _gtSsaNum = ssaNum;
    }

    bool HasSsaName()
    {
        return (GetSsaNum() != SsaConfig::RESERVED_SSA_NUM);
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
    static_assert_no_msg(MAX_RET_REG_COUNT * 2 <= sizeof(unsigned char) * BITS_PER_BYTE);
    assert(idx < MAX_RET_REG_COUNT);

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
    static_assert_no_msg(MAX_RET_REG_COUNT * 2 <= sizeof(unsigned char) * BITS_PER_BYTE);
    assert(idx < MAX_RET_REG_COUNT);

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
    INDEBUG(IL_OFFSET gtLclILoffs;) // instr offset of ref (only for JIT dumps)

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

    regNumber GetRegNumByIdx(int regIndex)
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
            gtOtherReg[regIndex - 1] = regNumberSmall(reg);
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

    GenTreeLclVar(genTreeOps oper,
                  var_types  type,
                  unsigned lclNum DEBUGARG(IL_OFFSET ilOffs = BAD_IL_OFFSET) DEBUGARG(bool largeNode = false))
        : GenTreeLclVarCommon(oper, type, lclNum DEBUGARG(largeNode)) DEBUGARG(gtLclILoffs(ilOffs))
    {
        assert(OperIsLocal(oper) || OperIsLocalAddr(oper));
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
    uint16_t      m_lclOffs;  // offset into the variable to access
    FieldSeqNode* m_fieldSeq; // This LclFld node represents some sequences of accesses.

public:
    GenTreeLclFld(genTreeOps oper, var_types type, unsigned lclNum, unsigned lclOffs)
        : GenTreeLclVarCommon(oper, type, lclNum), m_lclOffs(static_cast<uint16_t>(lclOffs)), m_fieldSeq(nullptr)
    {
        assert(lclOffs <= UINT16_MAX);
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

    FieldSeqNode* GetFieldSeq() const
    {
        return m_fieldSeq;
    }

    void SetFieldSeq(FieldSeqNode* fieldSeq)
    {
        m_fieldSeq = fieldSeq;
    }

#ifdef TARGET_ARM
    bool IsOffsetMisaligned() const;
#endif // TARGET_ARM

#if DEBUGGABLE_GENTREE
    GenTreeLclFld() : GenTreeLclVarCommon()
    {
    }
#endif
};

/* gtCast -- conversion to a different type  (GT_CAST) */

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
    Statement* gtAsgStmtWhenInlinedBoxValue;
    // And this is the statement that copies from the value being boxed to the box payload
    Statement* gtCopyStmtWhenInlinedBoxValue;

    GenTreeBox(var_types  type,
               GenTree*   boxOp,
               Statement* asgStmtWhenInlinedBoxValue,
               Statement* copyStmtWhenInlinedBoxValue)
        : GenTreeUnOp(GT_BOX, type, boxOp)
        , gtAsgStmtWhenInlinedBoxValue(asgStmtWhenInlinedBoxValue)
        , gtCopyStmtWhenInlinedBoxValue(copyStmtWhenInlinedBoxValue)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeBox() : GenTreeUnOp()
    {
    }
#endif
};

/* gtField  -- data member ref  (GT_FIELD) */

struct GenTreeField : public GenTree
{
    GenTree*             gtFldObj;
    CORINFO_FIELD_HANDLE gtFldHnd;
    DWORD                gtFldOffset;
    bool                 gtFldMayOverlap;
#ifdef FEATURE_READYTORUN_COMPILER
    CORINFO_CONST_LOOKUP gtFieldLookup;
#endif

    GenTreeField(var_types type, GenTree* obj, CORINFO_FIELD_HANDLE fldHnd, DWORD offs)
        : GenTree(GT_FIELD, type), gtFldObj(obj), gtFldHnd(fldHnd), gtFldOffset(offs), gtFldMayOverlap(false)
    {
        if (obj != nullptr)
        {
            gtFlags |= (obj->gtFlags & GTF_ALL_EFFECT);
        }

#ifdef FEATURE_READYTORUN_COMPILER
        gtFieldLookup.addr = nullptr;
#endif
    }

    // True if this field is a volatile memory operation.
    bool IsVolatile() const
    {
        return (gtFlags & GTF_FLD_VOLATILE) != 0;
    }

#if DEBUGGABLE_GENTREE
    GenTreeField() : GenTree()
    {
    }
#endif
};

// Represents the Argument list of a call node, as a Lisp-style linked list.
// (Originally I had hoped that this could have *only* the m_arg/m_rest fields, but it turns out
// that enough of the GenTree mechanism is used that it makes sense just to make it a subtype.  But
// note that in many ways, this is *not* a "real" node of the tree, but rather a mechanism for
// giving call nodes a flexible number of children.  GenTreeArgListNodes never evaluate to registers,
// for example.)

// Note that while this extends GenTreeOp, it is *not* an EXOP.  We don't add any new fields, and one
// is free to allocate a GenTreeOp of type GT_LIST.  If you use this type, you get the convenient Current/Rest
// method names for the arguments.
struct GenTreeArgList : public GenTreeOp
{
    GenTree*& Current()
    {
        return gtOp1;
    }
    GenTreeArgList*& Rest()
    {
        assert(gtOp2 == nullptr || gtOp2->OperIsAnyList());
        return *reinterpret_cast<GenTreeArgList**>(&gtOp2);
    }

#if DEBUGGABLE_GENTREE
    GenTreeArgList() : GenTreeOp()
    {
    }
#endif

    GenTreeArgList(GenTree* arg) : GenTreeArgList(arg, nullptr)
    {
    }

    GenTreeArgList(GenTree* arg, GenTreeArgList* rest) : GenTreeArgList(GT_LIST, arg, rest)
    {
    }

    GenTreeArgList(genTreeOps oper, GenTree* arg, GenTreeArgList* rest) : GenTreeOp(oper, TYP_VOID, arg, rest)
    {
        assert(OperIsAnyList(oper));
        assert((arg != nullptr) && arg->IsValidCallArgument());
        gtFlags |= arg->gtFlags & GTF_ALL_EFFECT;
        if (rest != nullptr)
        {
            gtFlags |= rest->gtFlags & GTF_ALL_EFFECT;
        }
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

// gtCall   -- method call      (GT_CALL)
enum class InlineObservation;

//------------------------------------------------------------------------
// GenTreeCallFlags: a bitmask of flags for GenTreeCall stored in gtCallMoreFlags.
//
// clang-format off
enum GenTreeCallFlags : unsigned int
{
    GTF_CALL_M_EMPTY                   = 0,

    GTF_CALL_M_EXPLICIT_TAILCALL       = 0x00000001, // the call is "tail" prefixed and importer has performed tail call checks
    GTF_CALL_M_TAILCALL                = 0x00000002, // the call is a tailcall
    GTF_CALL_M_VARARGS                 = 0x00000004, // the call uses varargs ABI
    GTF_CALL_M_RETBUFFARG              = 0x00000008, // call has a return buffer argument
    GTF_CALL_M_DELEGATE_INV            = 0x00000010, // call to Delegate.Invoke
    GTF_CALL_M_NOGCCHECK               = 0x00000020, // not a call for computing full interruptability and therefore no GC check is required.
    GTF_CALL_M_SPECIAL_INTRINSIC       = 0x00000040, // function that could be optimized as an intrinsic
                                                     // in special cases. Used to optimize fast way out in morphing
    GTF_CALL_M_UNMGD_THISCALL          = 0x00000080, // "this" pointer (first argument) should be enregistered (only for GTF_CALL_UNMANAGED)
    GTF_CALL_M_VIRTSTUB_REL_INDIRECT   = 0x00000080, // the virtstub is indirected through a relative address (only for GTF_CALL_VIRT_STUB)
    GTF_CALL_M_NONVIRT_SAME_THIS       = 0x00000080, // callee "this" pointer is equal to caller this pointer (only for GTF_CALL_NONVIRT)
    GTF_CALL_M_FRAME_VAR_DEATH         = 0x00000100, // the compLvFrameListRoot variable dies here (last use)
    GTF_CALL_M_TAILCALL_VIA_JIT_HELPER = 0x00000200, // call is a tail call dispatched via tail call JIT helper.

#if FEATURE_TAILCALL_OPT
    GTF_CALL_M_IMPLICIT_TAILCALL       = 0x00000400, // call is an opportunistic tail call and importer has performed tail call checks
    GTF_CALL_M_TAILCALL_TO_LOOP        = 0x00000800, // call is a fast recursive tail call that can be converted into a loop
#endif

    GTF_CALL_M_PINVOKE                 = 0x00001000, // call is a pinvoke.  This mirrors VM flag CORINFO_FLG_PINVOKE.
                                                     // A call marked as Pinvoke is not necessarily a GT_CALL_UNMANAGED. For e.g.
                                                     // an IL Stub dynamically generated for a PInvoke declaration is flagged as
                                                     // a Pinvoke but not as an unmanaged call. See impCheckForPInvokeCall() to
                                                     // know when these flags are set.

    GTF_CALL_M_R2R_REL_INDIRECT        = 0x00002000, // ready to run call is indirected through a relative address
    GTF_CALL_M_DOES_NOT_RETURN         = 0x00004000, // call does not return
    GTF_CALL_M_WRAPPER_DELEGATE_INV    = 0x00008000, // call is in wrapper delegate
    GTF_CALL_M_FAT_POINTER_CHECK       = 0x00010000, // CoreRT managed calli needs transformation, that checks
                                                     // special bit in calli address. If it is set, then it is necessary
                                                     // to restore real function address and load hidden argument
                                                     // as the first argument for calli. It is CoreRT replacement for instantiating
                                                     // stubs, because executable code cannot be generated at runtime.
    GTF_CALL_M_HELPER_SPECIAL_DCE      = 0x00020000, // this helper call can be removed if it is part of a comma and
                                                     // the comma result is unused.
    GTF_CALL_M_DEVIRTUALIZED           = 0x00040000, // this call was devirtualized
    GTF_CALL_M_UNBOXED                 = 0x00080000, // this call was optimized to use the unboxed entry point
    GTF_CALL_M_GUARDED_DEVIRT          = 0x00100000, // this call is a candidate for guarded devirtualization
    GTF_CALL_M_GUARDED_DEVIRT_CHAIN    = 0x00200000, // this call is a candidate for chained guarded devirtualization
    GTF_CALL_M_GUARDED                 = 0x00400000, // this call was transformed by guarded devirtualization
    GTF_CALL_M_ALLOC_SIDE_EFFECTS      = 0x00800000, // this is a call to an allocator with side effects
    GTF_CALL_M_SUPPRESS_GC_TRANSITION  = 0x01000000, // suppress the GC transition (i.e. during a pinvoke) but a separate GC safe point is required.
    GTF_CALL_M_EXP_RUNTIME_LOOKUP      = 0x02000000, // this call needs to be tranformed into CFG for the dynamic dictionary expansion feature.
    GTF_CALL_M_STRESS_TAILCALL         = 0x04000000, // the call is NOT "tail" prefixed but GTF_CALL_M_EXPLICIT_TAILCALL was added because of tail call stress mode
    GTF_CALL_M_EXPANDED_EARLY          = 0x08000000, // the Virtual Call target address is expanded and placed in gtControlExpr in Morph rather than in Lower

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
    bool      m_isEnclosingType;

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

    // Reset type descriptor to defaults
    void Reset()
    {
        for (unsigned i = 0; i < MAX_RET_REG_COUNT; ++i)
        {
            m_regType[i] = TYP_UNKNOWN;
        }
        m_isEnclosingType = false;
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

    // True if this value is returned in integer register
    // that is larger than the type itself.
    bool IsEnclosingType() const
    {
        return m_isEnclosingType;
    }

    // Get ith ABI return register
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

class fgArgInfo;

struct GenTreeCall final : public GenTree
{
    class Use
    {
        GenTree* m_node;
        Use*     m_next;

    public:
        Use(GenTree* node, Use* next = nullptr) : m_node(node), m_next(next)
        {
            assert(node != nullptr);
        }

        GenTree*& NodeRef()
        {
            return m_node;
        }

        GenTree* GetNode() const
        {
            assert(m_node != nullptr);
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

        Use* GetUse() const
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

    Use* gtCallThisArg;  // The instance argument ('this' pointer)
    Use* gtCallArgs;     // The list of arguments in original evaluation order
    Use* gtCallLateArgs; // On x86:     The register arguments in an optimal order
                         // On ARM/x64: - also includes any outgoing arg space arguments
                         //             - that were evaluated into a temp LclVar
    fgArgInfo* fgArgInfo;

    UseList Args()
    {
        return UseList(gtCallArgs);
    }

    UseList LateArgs()
    {
        return UseList(gtCallLateArgs);
    }

#if !FEATURE_FIXED_OUT_ARGS
    int     regArgListCount;
    regList regArgList;
#endif

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
    // GetRegNumByIdx: get ith return register allocated to this call node.
    //
    // Arguments:
    //     idx   -   index of the return register
    //
    // Return Value:
    //     Return regNumber of ith return register of call node.
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
    // SetRegNumByIdx: set ith return register of this call node
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
#if defined(FEATURE_READYTORUN_COMPILER) && defined(TARGET_ARMARCH)
        bool isVirtualStub = (gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_STUB;
        return ((IsR2RRelativeIndir()) || (isVirtualStub && (IsVirtualStubRelativeIndir())));
#else
        return false;
#endif // FEATURE_READYTORUN_COMPILER && TARGET_ARMARCH
    }

    bool HasNonStandardAddedArgs(Compiler* compiler) const;
    int GetNonStandardAddedArgCount(Compiler* compiler) const;

    // Returns true if this call uses a retBuf argument and its calling convention
    bool HasRetBufArg() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_RETBUFFARG) != 0;
    }

    //-------------------------------------------------------------------------
    // TreatAsHasRetBufArg:
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
    //     will make HasRetBufArg() return true, but will also force the
    //     use of register x8 to pass the RetBuf argument.
    //
    bool TreatAsHasRetBufArg(Compiler* compiler) const;

    bool HasFixedRetBufArg() const
    {
#if defined(TARGET_WINDOWS) && !defined(TARGET_ARM)
        return hasFixedRetBuffReg() && HasRetBufArg() && !callConvIsInstanceMethodCallConv(GetUnmanagedCallConv());
#else
        return hasFixedRetBuffReg() && HasRetBufArg();
#endif
    }

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
#if defined(TARGET_X86) || defined(TARGET_ARM)
        if (varTypeIsLong(gtType))
        {
            return true;
        }
#endif

        if (!varTypeIsStruct(gtType) || HasRetBufArg())
        {
            return false;
        }
        // Now it is a struct that is returned in registers.
        return GetReturnTypeDesc()->IsMultiRegRetType();
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
        return (gtCallMoreFlags & GTF_CALL_M_STRESS_TAILCALL) != 0;
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
        return (gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT) != 0;
    }

#ifdef FEATURE_READYTORUN_COMPILER
    bool IsR2RRelativeIndir() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_R2R_REL_INDIRECT) != 0;
    }
    void setEntryPoint(const CORINFO_CONST_LOOKUP& entryPoint)
    {
        gtEntryPoint = entryPoint;
        if (gtEntryPoint.accessType == IAT_PVALUE)
        {
            gtCallMoreFlags |= GTF_CALL_M_R2R_REL_INDIRECT;
        }
    }
#endif // FEATURE_READYTORUN_COMPILER

    bool IsVarargs() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_VARARGS) != 0;
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

    bool IsDevirtualized() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_DEVIRTUALIZED) != 0;
    }

    bool IsGuarded() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_GUARDED) != 0;
    }

    bool IsUnboxed() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_UNBOXED) != 0;
    }

    bool IsSuppressGCTransition() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_SUPPRESS_GC_TRANSITION) != 0;
    }

    void ClearGuardedDevirtualizationCandidate()
    {
        gtCallMoreFlags &= ~GTF_CALL_M_GUARDED_DEVIRT;
    }

    void SetGuardedDevirtualizationCandidate()
    {
        gtCallMoreFlags |= GTF_CALL_M_GUARDED_DEVIRT;
    }

    void SetIsGuarded()
    {
        gtCallMoreFlags |= GTF_CALL_M_GUARDED;
    }

    void SetExpRuntimeLookup()
    {
        gtCallMoreFlags |= GTF_CALL_M_EXP_RUNTIME_LOOKUP;
    }

    void ClearExpRuntimeLookup()
    {
        gtCallMoreFlags &= ~GTF_CALL_M_EXP_RUNTIME_LOOKUP;
    }

    bool IsExpRuntimeLookup() const
    {
        return (gtCallMoreFlags & GTF_CALL_M_EXP_RUNTIME_LOOKUP) != 0;
    }

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

    void ResetArgInfo();

    GenTreeCallFlags gtCallMoreFlags; // in addition to gtFlags

    unsigned char gtCallType : 3;   // value from the gtCallTypes enumeration
    unsigned char gtReturnType : 5; // exact return type

    CORINFO_CLASS_HANDLE gtRetClsHnd; // The return type handle of the call if it is a struct; always available

    union {
        // only used for CALLI unmanaged calls (CT_INDIRECT)
        GenTree* gtCallCookie;
        // gtInlineCandidateInfo is only used when inlining methods
        InlineCandidateInfo*                  gtInlineCandidateInfo;
        GuardedDevirtualizationCandidateInfo* gtGuardedDevirtualizationCandidateInfo;
        ClassProfileCandidateInfo*            gtClassProfileCandidateInfo;
        void*                                 gtStubCallStubAddr; // GTF_CALL_VIRT_STUB - these are never inlined
        CORINFO_GENERIC_HANDLE compileTimeHelperArgumentHandle; // Used to track type handle argument of dynamic helpers
        void*                  gtDirectCallAddress; // Used to pass direct call address between lower and codegen
    };

    // expression evaluated after args are placed which determines the control target
    GenTree* gtControlExpr;

    union {
        CORINFO_METHOD_HANDLE gtCallMethHnd; // CT_USER_FUNC
        GenTree*              gtCallAddr;    // CT_INDIRECT
    };

#ifdef FEATURE_READYTORUN_COMPILER
    // Call target lookup info for method call from a Ready To Run module
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

#if defined(DEBUG) || defined(INLINE_DATA)
    // For non-inline candidates, track the first observation
    // that blocks candidacy.
    InlineObservation gtInlineObservation;

    // IL offset of the call wrt its parent method.
    IL_OFFSET gtRawILOffset;
#endif // defined(DEBUG) || defined(INLINE_DATA)

    bool IsHelperCall() const
    {
        return gtCallType == CT_HELPER;
    }

    bool IsHelperCall(CORINFO_METHOD_HANDLE callMethHnd) const
    {
        return IsHelperCall() && (callMethHnd == gtCallMethHnd);
    }

    bool IsHelperCall(Compiler* compiler, unsigned helper) const;

    void ReplaceCallOperand(GenTree** operandUseEdge, GenTree* replacement);

    bool AreArgsComplete() const;

    CorInfoCallConvExtension GetUnmanagedCallConv() const
    {
        return IsUnmanaged() ? unmgdCallConv : CorInfoCallConvExtension::Managed;
    }

    static bool Equals(GenTreeCall* c1, GenTreeCall* c2);

    GenTreeCall(var_types type) : GenTree(GT_CALL, type)
    {
        fgArgInfo = nullptr;
    }
#if DEBUGGABLE_GENTREE
    GenTreeCall() : GenTree()
    {
    }
#endif
};

struct GenTreeCmpXchg : public GenTree
{
    GenTree* gtOpLocation;
    GenTree* gtOpValue;
    GenTree* gtOpComparand;

    GenTreeCmpXchg(var_types type, GenTree* loc, GenTree* val, GenTree* comparand)
        : GenTree(GT_CMPXCHG, type), gtOpLocation(loc), gtOpValue(val), gtOpComparand(comparand)
    {
        // There's no reason to do a compare-exchange on a local location, so we'll assume that all of these
        // have global effects.
        gtFlags |= (GTF_GLOB_REF | GTF_ASG);

        // Merge in flags from operands
        gtFlags |= gtOpLocation->gtFlags & GTF_ALL_EFFECT;
        gtFlags |= gtOpValue->gtFlags & GTF_ALL_EFFECT;
        gtFlags |= gtOpComparand->gtFlags & GTF_ALL_EFFECT;
    }
#if DEBUGGABLE_GENTREE
    GenTreeCmpXchg() : GenTree()
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
    // GetRegNumByIdx: get ith register allocated to this struct argument.
    //
    // Arguments:
    //     idx   -   index of the register
    //
    // Return Value:
    //     Return regNumber of ith register of this register argument
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

    var_types GetRegType(unsigned index)
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

#ifdef FEATURE_READYTORUN_COMPILER
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

    GenTreeFptrVal(var_types type, CORINFO_METHOD_HANDLE meth) : GenTree(GT_FTN_ADDR, type), gtFptrMethod(meth)
    {
#ifdef FEATURE_READYTORUN_COMPILER
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
    // The "Compiler*" argument is not a DEBUGARG here because we use it to keep track of the set of
    // (possible) QMark nodes.
    GenTreeQmark(var_types type, GenTree* cond, GenTree* colonOp, class Compiler* comp);

#if DEBUGGABLE_GENTREE
    GenTreeQmark() : GenTreeOp(GT_QMARK, TYP_INT, nullptr, nullptr)
    {
    }
#endif
};

/* gtIntrinsic   -- intrinsic   (possibly-binary op [NULL op2 is allowed] with an additional field) */

struct GenTreeIntrinsic : public GenTreeOp
{
    CorInfoIntrinsics     gtIntrinsicId;
    NamedIntrinsic        gtIntrinsicName;
    CORINFO_METHOD_HANDLE gtMethodHandle; // Method handle of the method which is treated as an intrinsic.

#ifdef FEATURE_READYTORUN_COMPILER
    // Call target lookup info for method call from a Ready To Run module
    CORINFO_CONST_LOOKUP gtEntryPoint;
#endif

    GenTreeIntrinsic(var_types             type,
                     GenTree*              op1,
                     CorInfoIntrinsics     intrinsicId,
                     NamedIntrinsic        intrinsicName,
                     CORINFO_METHOD_HANDLE methodHandle)
        : GenTreeOp(GT_INTRINSIC, type, op1, nullptr)
        , gtIntrinsicId(intrinsicId)
        , gtIntrinsicName(intrinsicName)
        , gtMethodHandle(methodHandle)
    {
        assert(intrinsicId != CORINFO_INTRINSIC_Illegal || intrinsicName != NI_Illegal);
    }

    GenTreeIntrinsic(var_types             type,
                     GenTree*              op1,
                     GenTree*              op2,
                     CorInfoIntrinsics     intrinsicId,
                     NamedIntrinsic        intrinsicName,
                     CORINFO_METHOD_HANDLE methodHandle)
        : GenTreeOp(GT_INTRINSIC, type, op1, op2)
        , gtIntrinsicId(intrinsicId)
        , gtIntrinsicName(intrinsicName)
        , gtMethodHandle(methodHandle)
    {
        assert(intrinsicId != CORINFO_INTRINSIC_Illegal || intrinsicName != NI_Illegal);
    }

#if DEBUGGABLE_GENTREE
    GenTreeIntrinsic() : GenTreeOp()
    {
    }
#endif
};

struct GenTreeJitIntrinsic : public GenTreeOp
{
private:
    ClassLayout* gtLayout;

    unsigned char  gtAuxiliaryJitType; // For intrinsics than need another type (e.g. Avx2.Gather* or SIMD (by element))
    regNumberSmall gtOtherReg;         // For intrinsics that return 2 registers

    unsigned char gtSimdBaseJitType; // SIMD vector base JIT type
    unsigned char gtSimdSize;        // SIMD vector size in bytes, use 0 for scalar intrinsics

public:
#if defined(FEATURE_SIMD)
    union {
        SIMDIntrinsicID gtSIMDIntrinsicID; // operation Id
        NamedIntrinsic  gtHWIntrinsicId;
    };
#else
    NamedIntrinsic gtHWIntrinsicId;
#endif

    ClassLayout* GetLayout() const
    {
        return gtLayout;
    }

    void SetLayout(ClassLayout* layout)
    {
        assert(layout != nullptr);
        gtLayout = layout;
    }

    regNumber GetOtherReg() const
    {
        return (regNumber)gtOtherReg;
    }

    void SetOtherReg(regNumber reg)
    {
        gtOtherReg = (regNumberSmall)reg;
        assert(gtOtherReg == reg);
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

    GenTreeJitIntrinsic(
        genTreeOps oper, var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
        : GenTreeOp(oper, type, op1, op2)
        , gtLayout(nullptr)
        , gtAuxiliaryJitType(CORINFO_TYPE_UNDEF)
        , gtOtherReg(REG_NA)
        , gtSimdBaseJitType((unsigned char)simdBaseJitType)
        , gtSimdSize((unsigned char)simdSize)
        , gtHWIntrinsicId(NI_Illegal)
    {
        assert(gtSimdBaseJitType == simdBaseJitType);
        assert(gtSimdSize == simdSize);
    }

    bool isSIMD() const
    {
        return gtSimdSize != 0;
    }

#if DEBUGGABLE_GENTREE
    GenTreeJitIntrinsic() : GenTreeOp()
    {
    }
#endif
};

#ifdef FEATURE_SIMD

/* gtSIMD   -- SIMD intrinsic   (possibly-binary op [NULL op2 is allowed] with additional fields) */
struct GenTreeSIMD : public GenTreeJitIntrinsic
{

    GenTreeSIMD(
        var_types type, GenTree* op1, SIMDIntrinsicID simdIntrinsicID, CorInfoType simdBaseJitType, unsigned simdSize)
        : GenTreeJitIntrinsic(GT_SIMD, type, op1, nullptr, simdBaseJitType, simdSize)
    {
        gtSIMDIntrinsicID = simdIntrinsicID;
    }

    GenTreeSIMD(var_types       type,
                GenTree*        op1,
                GenTree*        op2,
                SIMDIntrinsicID simdIntrinsicID,
                CorInfoType     simdBaseJitType,
                unsigned        simdSize)
        : GenTreeJitIntrinsic(GT_SIMD, type, op1, op2, simdBaseJitType, simdSize)
    {
        gtSIMDIntrinsicID = simdIntrinsicID;
    }

    bool OperIsMemoryLoad() const; // Returns true for the SIMD Intrinsic instructions that have MemoryLoad semantics,
                                   // false otherwise

#if DEBUGGABLE_GENTREE
    GenTreeSIMD() : GenTreeJitIntrinsic()
    {
    }
#endif
};
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
struct GenTreeHWIntrinsic : public GenTreeJitIntrinsic
{
    GenTreeHWIntrinsic(var_types type, NamedIntrinsic hwIntrinsicID, CorInfoType simdBaseJitType, unsigned simdSize)
        : GenTreeJitIntrinsic(GT_HWINTRINSIC, type, nullptr, nullptr, simdBaseJitType, simdSize)
    {
        gtHWIntrinsicId = hwIntrinsicID;
    }

    GenTreeHWIntrinsic(
        var_types type, GenTree* op1, NamedIntrinsic hwIntrinsicID, CorInfoType simdBaseJitType, unsigned simdSize)
        : GenTreeJitIntrinsic(GT_HWINTRINSIC, type, op1, nullptr, simdBaseJitType, simdSize)
    {
        gtHWIntrinsicId = hwIntrinsicID;
        if (OperIsMemoryStore())
        {
            gtFlags |= (GTF_GLOB_REF | GTF_ASG);
        }
    }

    GenTreeHWIntrinsic(var_types      type,
                       GenTree*       op1,
                       GenTree*       op2,
                       NamedIntrinsic hwIntrinsicID,
                       CorInfoType    simdBaseJitType,
                       unsigned       simdSize)
        : GenTreeJitIntrinsic(GT_HWINTRINSIC, type, op1, op2, simdBaseJitType, simdSize)
    {
        gtHWIntrinsicId = hwIntrinsicID;
        if (OperIsMemoryStore())
        {
            gtFlags |= (GTF_GLOB_REF | GTF_ASG);
        }
    }

    // Note that HW Intrinsic instructions are a sub class of GenTreeOp which only supports two operands
    // However there are HW Intrinsic instructions that have 3 or even 4 operands and this is
    // supported using a single op1 and using an ArgList for it:  gtNewArgList(op1, op2, op3)

    bool OperIsMemoryLoad() const;  // Returns true for the HW Intrinsic instructions that have MemoryLoad semantics,
                                    // false otherwise
    bool OperIsMemoryStore() const; // Returns true for the HW Intrinsic instructions that have MemoryStore semantics,
                                    // false otherwise
    bool OperIsMemoryLoadOrStore() const; // Returns true for the HW Intrinsic instructions that have MemoryLoad or
                                          // MemoryStore semantics, false otherwise

#if DEBUGGABLE_GENTREE
    GenTreeHWIntrinsic() : GenTreeJitIntrinsic()
    {
    }
#endif
};
#endif // FEATURE_HW_INTRINSICS

/* gtIndex -- array access */

struct GenTreeIndex : public GenTreeOp
{
    GenTree*& Arr()
    {
        return gtOp1;
    }
    GenTree*& Index()
    {
        return gtOp2;
    }

    unsigned             gtIndElemSize;     // size of elements in the array
    CORINFO_CLASS_HANDLE gtStructElemClass; // If the element type is a struct, this is the struct type.

    GenTreeIndex(var_types type, GenTree* arr, GenTree* ind, unsigned indElemSize)
        : GenTreeOp(GT_INDEX, type, arr, ind)
        , gtIndElemSize(indElemSize)
        , gtStructElemClass(nullptr) // We always initialize this after construction.
    {
#ifdef DEBUG
        if (JitConfig.JitSkipArrayBoundCheck() == 1)
        {
            // Skip bounds check
        }
        else
#endif
        {
            // Do bounds check
            gtFlags |= GTF_INX_RNGCHK;
        }

        gtFlags |= GTF_EXCEPT | GTF_GLOB_REF;
    }
#if DEBUGGABLE_GENTREE
    GenTreeIndex() : GenTreeOp()
    {
    }
#endif
};

// gtIndexAddr: given an array object and an index, checks that the index is within the bounds of the array if
//              necessary and produces the address of the value at that index of the array.
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
                     unsigned             elemOffset)
        : GenTreeOp(GT_INDEX_ADDR, TYP_BYREF, arr, ind)
        , gtStructElemClass(structElemClass)
        , gtIndRngFailBB(nullptr)
        , gtElemType(elemType)
        , gtElemSize(elemSize)
        , gtLenOffset(lenOffset)
        , gtElemOffset(elemOffset)
    {
#ifdef DEBUG
        if (JitConfig.JitSkipArrayBoundCheck() == 1)
        {
            // Skip bounds check
        }
        else
#endif
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
};

/* gtArrLen -- array length (GT_ARR_LENGTH)
   GT_ARR_LENGTH is used for "arr.length" */

struct GenTreeArrLen : public GenTreeUnOp
{
    GenTree*& ArrRef()
    {
        return gtOp1;
    } // the array address node
private:
    int gtArrLenOffset; // constant to add to "gtArrRef" to get the address of the array length.

public:
    inline int ArrLenOffset()
    {
        return gtArrLenOffset;
    }

    GenTreeArrLen(var_types type, GenTree* arrRef, int lenOffset)
        : GenTreeUnOp(GT_ARR_LENGTH, type, arrRef), gtArrLenOffset(lenOffset)
    {
    }

#if DEBUGGABLE_GENTREE
    GenTreeArrLen() : GenTreeUnOp()
    {
    }
#endif
};

// This takes:
// - a comparison value (generally an array length),
// - an index value, and
// - the label to jump to if the index is out of range.
// - the "kind" of the throw block to branch to on failure
// It generates no result.

struct GenTreeBoundsChk : public GenTree
{
    GenTree* gtIndex;  // The index expression.
    GenTree* gtArrLen; // An expression for the length of the array being indexed.

    BasicBlock*     gtIndRngFailBB; // Basic block to jump to for array-index-out-of-range
    SpecialCodeKind gtThrowKind;    // Kind of throw block to branch to on failure

    GenTreeBoundsChk(genTreeOps oper, var_types type, GenTree* index, GenTree* arrLen, SpecialCodeKind kind)
        : GenTree(oper, type), gtIndex(index), gtArrLen(arrLen), gtIndRngFailBB(nullptr), gtThrowKind(kind)
    {
        // Effects flags propagate upwards.
        gtFlags |= (index->gtFlags & GTF_ALL_EFFECT);
        gtFlags |= (arrLen->gtFlags & GTF_ALL_EFFECT);
        gtFlags |= GTF_EXCEPT;
    }
#if DEBUGGABLE_GENTREE
    GenTreeBoundsChk() : GenTree()
    {
    }
#endif

    // If the gtArrLen is really an array length, returns array reference, else "NULL".
    GenTree* GetArray()
    {
        if (gtArrLen->OperGet() == GT_ARR_LENGTH)
        {
            return gtArrLen->AsArrLen()->ArrRef();
        }
        else
        {
            return nullptr;
        }
    }
};

// gtArrElem -- general array element (GT_ARR_ELEM), for non "SZ_ARRAYS"
//              -- multidimensional arrays, or 1-d arrays with non-zero lower bounds.

struct GenTreeArrElem : public GenTree
{
    GenTree* gtArrObj;

#define GT_ARR_MAX_RANK 3
    GenTree*      gtArrInds[GT_ARR_MAX_RANK]; // Indices
    unsigned char gtArrRank;                  // Rank of the array

    unsigned char gtArrElemSize; // !!! Caution, this is an "unsigned char", it is used only
                                 // on the optimization path of array intrisics.
                                 // It stores the size of array elements WHEN it can fit
                                 // into an "unsigned char".
                                 // This has caused VSW 571394.
    var_types gtArrElemType;     // The array element type

    // Requires that "inds" is a pointer to an array of "rank" GenTreePtrs for the indices.
    GenTreeArrElem(
        var_types type, GenTree* arr, unsigned char rank, unsigned char elemSize, var_types elemType, GenTree** inds)
        : GenTree(GT_ARR_ELEM, type), gtArrObj(arr), gtArrRank(rank), gtArrElemSize(elemSize), gtArrElemType(elemType)
    {
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

//--------------------------------------------
//
// GenTreeArrIndex (gtArrIndex): Expression to bounds-check the index for one dimension of a
//    multi-dimensional or non-zero-based array., and compute the effective index
//    (i.e. subtracting the lower bound).
//
// Notes:
//    This node is similar in some ways to GenTreeBoundsChk, which ONLY performs the check.
//    The reason that this node incorporates the check into the effective index computation is
//    to avoid duplicating the codegen, as the effective index is required to compute the
//    offset anyway.
//    TODO-CQ: Enable optimization of the lower bound and length by replacing this:
//                /--*  <arrObj>
//                +--*  <index0>
//             +--* ArrIndex[i, ]
//    with something like:
//                   /--*  <arrObj>
//                /--*  ArrLowerBound[i, ]
//                |  /--*  <arrObj>
//                +--*  ArrLen[i, ]    (either generalize GT_ARR_LENGTH or add a new node)
//                +--*  <index0>
//             +--* ArrIndex[i, ]
//    Which could, for example, be optimized to the following when known to be within bounds:
//                /--*  TempForLowerBoundDim0
//                +--*  <index0>
//             +--* - (GT_SUB)
//
struct GenTreeArrIndex : public GenTreeOp
{
    // The array object - may be any expression producing an Array reference, but is likely to be a lclVar.
    GenTree*& ArrObj()
    {
        return gtOp1;
    }
    // The index expression - may be any integral expression.
    GenTree*& IndexExpr()
    {
        return gtOp2;
    }
    unsigned char gtCurrDim;     // The current dimension
    unsigned char gtArrRank;     // Rank of the array
    var_types     gtArrElemType; // The array element type

    GenTreeArrIndex(var_types     type,
                    GenTree*      arrObj,
                    GenTree*      indexExpr,
                    unsigned char currDim,
                    unsigned char arrRank,
                    var_types     elemType)
        : GenTreeOp(GT_ARR_INDEX, type, arrObj, indexExpr)
        , gtCurrDim(currDim)
        , gtArrRank(arrRank)
        , gtArrElemType(elemType)
    {
        gtFlags |= GTF_EXCEPT;
    }
#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    // Used only for GenTree::GetVtableForOper()
    GenTreeArrIndex() : GenTreeOp()
    {
    }
#endif
};

//--------------------------------------------
//
// GenTreeArrOffset (gtArrOffset): Expression to compute the accumulated offset for the address
//    of an element of a multi-dimensional or non-zero-based array.
//
// Notes:
//    The result of this expression is (gtOffset * dimSize) + gtIndex
//    where dimSize is the length/stride/size of the dimension, and is obtained from gtArrObj.
//    This node is generated in conjunction with the GenTreeArrIndex node, which computes the
//    effective index for a single dimension.  The sub-trees can be separately optimized, e.g.
//    within a loop body where the expression for the 0th dimension may be invariant.
//
//    Here is an example of how the tree might look for a two-dimension array reference:
//                /--*  const 0
//                |  /--* <arrObj>
//                |  +--* <index0>
//                +--* ArrIndex[i, ]
//                +--*  <arrObj>
//             /--| arrOffs[i, ]
//             |  +--*  <arrObj>
//             |  +--*  <index1>
//             +--* ArrIndex[*,j]
//             +--*  <arrObj>
//          /--| arrOffs[*,j]
//    TODO-CQ: see comment on GenTreeArrIndex for how its representation may change.  When that
//    is done, we will also want to replace the <arrObj> argument to arrOffs with the
//    ArrLen as for GenTreeArrIndex.
//
struct GenTreeArrOffs : public GenTree
{
    GenTree* gtOffset;           // The accumulated offset for lower dimensions - must be TYP_I_IMPL, and
                                 // will either be a CSE temp, the constant 0, or another GenTreeArrOffs node.
    GenTree* gtIndex;            // The effective index for the current dimension - must be non-negative
                                 // and can be any expression (though it is likely to be either a GenTreeArrIndex,
                                 // node, a lclVar, or a constant).
    GenTree* gtArrObj;           // The array object - may be any expression producing an Array reference,
                                 // but is likely to be a lclVar.
    unsigned char gtCurrDim;     // The current dimension
    unsigned char gtArrRank;     // Rank of the array
    var_types     gtArrElemType; // The array element type

    GenTreeArrOffs(var_types     type,
                   GenTree*      offset,
                   GenTree*      index,
                   GenTree*      arrObj,
                   unsigned char currDim,
                   unsigned char rank,
                   var_types     elemType)
        : GenTree(GT_ARR_OFFSET, type)
        , gtOffset(offset)
        , gtIndex(index)
        , gtArrObj(arrObj)
        , gtCurrDim(currDim)
        , gtArrRank(rank)
        , gtArrElemType(elemType)
    {
        assert(index->gtFlags & GTF_EXCEPT);
        gtFlags |= GTF_EXCEPT;
    }
#if DEBUGGABLE_GENTREE
    GenTreeArrOffs() : GenTree()
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
    //      2. If Index is null, Scale should be zero (or unintialized / unused)
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
    // Since GenTreeDynBlk derives from this, but is an "EXOP" (i.e. it has extra fields),
    // we can't access Op1 and Op2 in the normal manner if we may have a DynBlk.
    GenTree*& Addr()
    {
        return gtOp1;
    }

    void SetAddr(GenTree* addr)
    {
        assert(addr != nullptr);
        assert(addr->TypeIs(TYP_I_IMPL, TYP_BYREF));
        gtOp1 = addr;
    }

    // these methods provide an interface to the indirection node which
    bool     HasBase();
    bool     HasIndex();
    GenTree* Base();
    GenTree* Index();
    unsigned Scale();
    ssize_t  Offset();

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

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    // Used only for GenTree::GetVtableForOper()
    GenTreeIndir() : GenTreeOp()
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
        assert((layout != nullptr) || OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK));
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
        assert((m_layout != nullptr) || OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK));
        return (m_layout != nullptr) ? m_layout->GetSize() : 0;
    }

    // Instruction selection: during codegen time, what code sequence we will be using
    // to encode this operation.
    enum
    {
        BlkOpKindInvalid,
#ifndef TARGET_X86
        BlkOpKindHelper,
#endif
#ifdef TARGET_XARCH
        BlkOpKindRepInstr,
#endif
        BlkOpKindUnroll,
    } gtBlkOpKind;

#ifndef JIT32_GCENCODER
    bool gtBlkOpGcUnsafe;
#endif

#ifdef TARGET_XARCH
    bool IsOnHeapAndContainsReferences()
    {
        return (m_layout != nullptr) && m_layout->HasGCPtr() && !Addr()->OperIsLocalAddr();
    }
#endif

    GenTreeBlk(genTreeOps oper, var_types type, GenTree* addr, ClassLayout* layout)
        : GenTreeIndir(oper, type, addr, nullptr)
        , m_layout(layout)
        , gtBlkOpKind(BlkOpKindInvalid)
#ifndef JIT32_GCENCODER
        , gtBlkOpGcUnsafe(false)
#endif
    {
        assert(OperIsBlk(oper));
        assert((layout != nullptr) || OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK));
        gtFlags |= (addr->gtFlags & GTF_ALL_EFFECT);
    }

    GenTreeBlk(genTreeOps oper, var_types type, GenTree* addr, GenTree* data, ClassLayout* layout)
        : GenTreeIndir(oper, type, addr, data)
        , m_layout(layout)
        , gtBlkOpKind(BlkOpKindInvalid)
#ifndef JIT32_GCENCODER
        , gtBlkOpGcUnsafe(false)
#endif
    {
        assert(OperIsBlk(oper));
        assert((layout != nullptr) || OperIs(GT_DYN_BLK, GT_STORE_DYN_BLK));
        gtFlags |= (addr->gtFlags & GTF_ALL_EFFECT);
        gtFlags |= (data->gtFlags & GTF_ALL_EFFECT);
    }

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    GenTreeBlk() : GenTreeIndir()
    {
    }
#endif // DEBUGGABLE_GENTREE
};

// gtObj  -- 'object' (GT_OBJ).
//
// This node is used for block values that may have GC pointers.

struct GenTreeObj : public GenTreeBlk
{
    void Init()
    {
        // By default, an OBJ is assumed to be a global reference, unless it is local.
        GenTreeLclVarCommon* lcl = Addr()->IsLocalAddrExpr();
        if ((lcl == nullptr) || ((lcl->gtFlags & GTF_GLOB_EFFECT) != 0))
        {
            gtFlags |= GTF_GLOB_REF;
        }
        noway_assert(GetLayout()->GetClassHandle() != NO_CLASS_HANDLE);
    }

    GenTreeObj(var_types type, GenTree* addr, ClassLayout* layout) : GenTreeBlk(GT_OBJ, type, addr, layout)
    {
        Init();
    }

    GenTreeObj(var_types type, GenTree* addr, GenTree* data, ClassLayout* layout)
        : GenTreeBlk(GT_STORE_OBJ, type, addr, data, layout)
    {
        Init();
    }

#if DEBUGGABLE_GENTREE
    GenTreeObj() : GenTreeBlk()
    {
    }
#endif
};

// gtDynBlk  -- 'dynamic block' (GT_DYN_BLK).
//
// This node is used for block values that have a dynamic size.
// Note that such a value can never have GC pointers.

struct GenTreeDynBlk : public GenTreeBlk
{
public:
    GenTree* gtDynamicSize;
    bool     gtEvalSizeFirst;

    GenTreeDynBlk(GenTree* addr, GenTree* dynamicSize)
        : GenTreeBlk(GT_DYN_BLK, TYP_STRUCT, addr, nullptr), gtDynamicSize(dynamicSize), gtEvalSizeFirst(false)
    {
        // Conservatively the 'addr' could be null or point into the global heap.
        gtFlags |= GTF_EXCEPT | GTF_GLOB_REF;
        gtFlags |= (dynamicSize->gtFlags & GTF_ALL_EFFECT);
    }

#if DEBUGGABLE_GENTREE
protected:
    friend GenTree;
    GenTreeDynBlk() : GenTreeBlk()
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

/* gtRetExp -- Place holder for the return expression from an inline candidate (GT_RET_EXPR) */

struct GenTreeRetExpr : public GenTree
{
    GenTree* gtInlineCandidate;

    BasicBlockFlags bbFlags;

    CORINFO_CLASS_HANDLE gtRetClsHnd;

    GenTreeRetExpr(var_types type) : GenTree(GT_RET_EXPR, type)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeRetExpr() : GenTree()
    {
    }
#endif
};

class InlineContext;

struct GenTreeILOffset : public GenTree
{
    IL_OFFSETX gtStmtILoffsx; // instr offset (if available)
#ifdef DEBUG
    IL_OFFSET gtStmtLastILoffs; // instr offset at end of stmt
#endif

    GenTreeILOffset(IL_OFFSETX offset DEBUGARG(IL_OFFSET lastOffset = BAD_IL_OFFSET))
        : GenTree(GT_IL_OFFSET, TYP_VOID)
        , gtStmtILoffsx(offset)
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

    // Forward iterator for the execution order GenTree linked list (using `gtNext` pointer).
    //
    class iterator
    {
        GenTree* m_tree;

    public:
        iterator(GenTree* tree) : m_tree(tree)
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

public:
    GenTreeList(GenTree* trees) : m_trees(trees)
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

// We use the following format when printing the Statement number: Statement->GetID()
// This define is used with string concatenation to put this in printf format strings  (Note that %u means unsigned int)
#define FMT_STMT "STMT%05u"

struct Statement
{
public:
    Statement(GenTree* expr, IL_OFFSETX offset DEBUGARG(unsigned stmtID))
        : m_rootNode(expr)
        , m_treeList(nullptr)
        , m_next(nullptr)
        , m_prev(nullptr)
        , m_inlineContext(nullptr)
        , m_ILOffsetX(offset)
#ifdef DEBUG
        , m_lastILOffset(BAD_IL_OFFSET)
        , m_stmtID(stmtID)
#endif
        , m_compilerAdded(false)
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

    void SetTreeList(GenTree* treeHead)
    {
        m_treeList = treeHead;
    }

    // TreeList: convenience method for enabling range-based `for` iteration over the
    // execution order of the GenTree linked list, e.g.:
    //    for (GenTree* const tree : stmt->TreeList()) ...
    //
    GenTreeList TreeList() const
    {
        return GenTreeList(GetTreeList());
    }

    InlineContext* GetInlineContext() const
    {
        return m_inlineContext;
    }

    void SetInlineContext(InlineContext* inlineContext)
    {
        m_inlineContext = inlineContext;
    }

    IL_OFFSETX GetILOffsetX() const
    {
        return m_ILOffsetX;
    }

    void SetILOffsetX(IL_OFFSETX offsetX)
    {
        m_ILOffsetX = offsetX;
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

    bool IsCompilerAdded() const
    {
        return m_compilerAdded;
    }

    void SetCompilerAdded()
    {
        m_compilerAdded = true;
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

    // The statement nodes are doubly-linked. The first statement node in a block points
    // to the last node in the block via its `m_prev` link. Note that the last statement node
    // does not point to the first: it's `m_next == nullptr`; that is, the list is not fully circular.
    Statement* m_next;
    Statement* m_prev;

    InlineContext* m_inlineContext; // The inline context for this statement.

    IL_OFFSETX m_ILOffsetX; // The instr offset (if available).

#ifdef DEBUG
    IL_OFFSET m_lastILOffset; // The instr offset at the end of this statement.
    unsigned  m_stmtID;
#endif

    bool m_compilerAdded; // Was the statement created by optimizer?
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

/* AsClsVar() -- 'static data member' (GT_CLS_VAR) */

struct GenTreeClsVar : public GenTree
{
    CORINFO_FIELD_HANDLE gtClsVarHnd;
    FieldSeqNode*        gtFieldSeq;

    GenTreeClsVar(var_types type, CORINFO_FIELD_HANDLE clsVarHnd, FieldSeqNode* fldSeq)
        : GenTree(GT_CLS_VAR, type), gtClsVarHnd(clsVarHnd), gtFieldSeq(fldSeq)
    {
        gtFlags |= GTF_GLOB_REF;
    }

    GenTreeClsVar(genTreeOps oper, var_types type, CORINFO_FIELD_HANDLE clsVarHnd, FieldSeqNode* fldSeq)
        : GenTree(oper, type), gtClsVarHnd(clsVarHnd), gtFieldSeq(fldSeq)
    {
        assert((oper == GT_CLS_VAR) || (oper == GT_CLS_VAR_ADDR));
        gtFlags |= GTF_GLOB_REF;
    }

#if DEBUGGABLE_GENTREE
    GenTreeClsVar() : GenTree()
    {
    }
#endif
};

/* gtArgPlace -- 'register argument placeholder' (GT_ARGPLACE) */

struct GenTreeArgPlace : public GenTree
{
    CORINFO_CLASS_HANDLE gtArgPlaceClsHnd; // Needed when we have a TYP_STRUCT argument

    GenTreeArgPlace(var_types type, CORINFO_CLASS_HANDLE clsHnd) : GenTree(GT_ARGPLACE, type), gtArgPlaceClsHnd(clsHnd)
    {
    }
#if DEBUGGABLE_GENTREE
    GenTreeArgPlace() : GenTree()
    {
    }
#endif
};

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
#if defined(DEBUG_ARG_SLOTS)
    unsigned gtSlotNum; // Slot number of the argument to be passed on stack
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
    unsigned gtNumSlots; // Number of slots for the argument to be passed on stack
#endif
#endif

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

    enum class Kind : __int8{
        Invalid, RepInstr, Unroll, Push, PushAllSlots,
    };
    Kind gtPutArgStkKind;
#endif

    GenTreePutArgStk(genTreeOps oper,
                     var_types  type,
                     GenTree*   op1,
                     unsigned   stackByteOffset,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                     unsigned stackByteSize,
#endif
#if defined(DEBUG_ARG_SLOTS)
                     unsigned slotNum,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                     unsigned numSlots,
#endif
#endif
                     GenTreeCall* callNode,
                     bool         putInIncomingArgArea)
        : GenTreeUnOp(oper, type, op1 DEBUGARG(/*largeNode*/ false))
        , m_byteOffset(stackByteOffset)
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
        , m_byteSize(stackByteSize)
#endif
#if defined(DEBUG_ARG_SLOTS)
        , gtSlotNum(slotNum)
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
        , gtNumSlots(numSlots)
#endif
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
#endif
    {
        DEBUG_ARG_SLOTS_ASSERT(m_byteOffset == slotNum * TARGET_POINTER_SIZE);
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
        DEBUG_ARG_SLOTS_ASSERT(m_byteSize == gtNumSlots * TARGET_POINTER_SIZE);
#endif
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
        DEBUG_ARG_SLOTS_ASSERT(m_byteOffset / TARGET_POINTER_SIZE == gtSlotNum);
        DEBUG_ARG_SLOTS_ASSERT(m_byteOffset % TARGET_POINTER_SIZE == 0);
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

    // Return true if this is a PutArgStk of a SIMD12 struct.
    // This is needed because such values are re-typed to SIMD16, and the type of PutArgStk is VOID.
    unsigned isSIMD12() const
    {
        return (varTypeIsSIMD(gtOp1) && (GetStackByteSize() == 12));
    }

    bool isPushKind() const
    {
        return (gtPutArgStkKind == Kind::Push) || (gtPutArgStkKind == Kind::PushAllSlots);
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
#if defined(DEBUG_ARG_SLOTS)
                       unsigned slotNum,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                       unsigned numSlots,
#endif
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
#if defined(DEBUG_ARG_SLOTS)
                           slotNum,
#if defined(FEATURE_PUT_STRUCT_ARG_STK)
                           numSlots,
#endif
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
    // GetRegNumByIdx: get ith register allocated to this struct argument.
    //
    // Arguments:
    //     idx   -   index of the struct
    //
    // Return Value:
    //     Return regNumber of ith register of this struct argument
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
    // SetRegNumByIdx: set ith register of this struct argument
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
// As it turns out, these are only needed on targets that happen to have multi-reg returns.
// However, they are actually needed on any target that has any multi-reg ops. It is just
// coincidence that those are the same (and there isn't a FEATURE_MULTIREG_OPS).
//
struct GenTreeCopyOrReload : public GenTreeUnOp
{
#if FEATURE_MULTIREG_RET
    // State required to support copy/reload of a multi-reg call node.
    // The first register is always given by GetRegNum().
    //
    regNumberSmall gtOtherRegs[MAX_RET_REG_COUNT - 1];
#endif

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
#if FEATURE_MULTIREG_RET
        for (unsigned i = 0; i < MAX_RET_REG_COUNT - 1; ++i)
        {
            gtOtherRegs[i] = REG_NA;
        }
#endif
    }

    //-----------------------------------------------------------
    // GetRegNumByIdx: Get regNumber of ith position.
    //
    // Arguments:
    //    idx   -   register position.
    //
    // Return Value:
    //    Returns regNumber assigned to ith position.
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

    //-----------------------------------------------------------
    // SetRegNumByIdx: Set the regNumber for ith position.
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
        else
        {
            unreached();
        }
#endif
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
        for (unsigned i = 0; i < MAX_RET_REG_COUNT - 1; ++i)
        {
            gtOtherRegs[i] = from->gtOtherRegs[i];
        }
#endif
    }

    unsigned GetRegCount()
    {
#if FEATURE_MULTIREG_RET
        // We need to return the highest index for which we have a valid register.
        // Note that the gtOtherRegs array is off by one (the 0th register is GetRegNum()).
        // If there's no valid register in gtOtherRegs, GetRegNum() must be valid.
        // Note that for most nodes, the set of valid registers must be contiguous,
        // but for COPY or RELOAD there is only a valid register for the register positions
        // that must be copied or reloaded.
        //
        for (unsigned i = MAX_RET_REG_COUNT; i > 1; i--)
        {
            if (gtOtherRegs[i - 2] != REG_NA)
            {
                return i;
            }
        }
#endif
        // We should never have a COPY or RELOAD with no valid registers.
        assert(GetRegNum() != REG_NA);
        return 1;
    }

    GenTreeCopyOrReload(genTreeOps oper, var_types type, GenTree* op1) : GenTreeUnOp(oper, type, op1)
    {
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
#ifdef FEATURE_READYTORUN_COMPILER
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
#ifdef FEATURE_READYTORUN_COMPILER
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

        assert(m_code < _countof(names));
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

        // GT_TEST_EQ/NE are special, they need to be mapped as GT_EQ/NE
        unsigned code = oper - ((oper >= GT_TEST_EQ) ? GT_TEST_EQ : GT_EQ);

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
            FNE,  FEQ,  FGE,  FGT,  FLT,  FGT,  NP, P
        };
        // clang-format on

        assert(condition.m_code < _countof(reverse));
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

        assert(condition.m_code < _countof(swap));
        return GenCondition(swap[condition.m_code]);
    }
};

// Represents a GT_JCC or GT_SETCC node.

struct GenTreeCC final : public GenTree
{
    GenCondition gtCondition;

    GenTreeCC(genTreeOps oper, GenCondition condition, var_types type = TYP_VOID)
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

//------------------------------------------------------------------------
// Deferred inline functions of GenTree -- these need the subtypes above to
// be defined already.
//------------------------------------------------------------------------

inline bool GenTree::OperIsBlkOp()
{
    return ((gtOper == GT_ASG) && varTypeIsStruct(AsOp()->gtOp1)) || (OperIsBlk() && (AsBlk()->Data() != nullptr));
}

inline bool GenTree::OperIsDynBlkOp()
{
    if (gtOper == GT_ASG)
    {
        return gtGetOp1()->OperGet() == GT_DYN_BLK;
    }
    else if (gtOper == GT_STORE_DYN_BLK)
    {
        return true;
    }
    return false;
}

inline bool GenTree::OperIsInitBlkOp()
{
    if (!OperIsBlkOp())
    {
        return false;
    }
    GenTree* src;
    if (gtOper == GT_ASG)
    {
        src = gtGetOp2();
    }
    else
    {
        src = AsBlk()->Data()->gtSkipReloadOrCopy();
    }
    return src->OperIsInitVal() || src->OperIsConst();
}

inline bool GenTree::OperIsCopyBlkOp()
{
    return OperIsBlkOp() && !OperIsInitBlkOp();
}

//------------------------------------------------------------------------
// IsFPZero: Checks whether this is a floating point constant with value 0.0
//
// Return Value:
//    Returns true iff the tree is an GT_CNS_DBL, with value of 0.0.

inline bool GenTree::IsFPZero() const
{
    if ((gtOper == GT_CNS_DBL) && (AsDblCon()->gtDconVal == 0.0))
    {
        return true;
    }
    return false;
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
// IsIntegralConstVector: returns true if this this is a SIMD vector
// with all its elements equal to an integral constant.
//
// Arguments:
//     constVal  -  const value of vector element
//
// Returns:
//     True if this represents an integral const SIMD vector.
//
inline bool GenTree::IsIntegralConstVector(ssize_t constVal) const
{
#ifdef FEATURE_SIMD
    // SIMDIntrinsicInit intrinsic with a const value as initializer
    // represents a const vector.
    if ((gtOper == GT_SIMD) && (AsSIMD()->gtSIMDIntrinsicID == SIMDIntrinsicInit) &&
        gtGetOp1()->IsIntegralConst(constVal))
    {
        assert(varTypeIsIntegral(AsSIMD()->GetSimdBaseType()));
        assert(gtGetOp2IfPresent() == nullptr);
        return true;
    }
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
    if (gtOper == GT_HWINTRINSIC)
    {
        const GenTreeHWIntrinsic* node = AsHWIntrinsic();

        if (!varTypeIsIntegral(node->GetSimdBaseType()))
        {
            // Can't be an integral constant
            return false;
        }

        GenTree* op1 = gtGetOp1();
        GenTree* op2 = gtGetOp2();

        NamedIntrinsic intrinsicId = node->gtHWIntrinsicId;

        if (op1 == nullptr)
        {
            assert(op2 == nullptr);

            if (constVal == 0)
            {
#if defined(TARGET_XARCH)
                return (intrinsicId == NI_Vector128_get_Zero) || (intrinsicId == NI_Vector256_get_Zero);
#elif defined(TARGET_ARM64)
                return (intrinsicId == NI_Vector64_get_Zero) || (intrinsicId == NI_Vector128_get_Zero);
#endif // !TARGET_XARCH && !TARGET_ARM64
            }
        }
        else if ((op2 == nullptr) && !op1->OperIsList())
        {
            if (op1->IsIntegralConst(constVal))
            {
#if defined(TARGET_XARCH)
                return (intrinsicId == NI_Vector128_Create) || (intrinsicId == NI_Vector256_Create);
#elif defined(TARGET_ARM64)
                return (intrinsicId == NI_Vector64_Create) || (intrinsicId == NI_Vector128_Create);
#endif // !TARGET_XARCH && !TARGET_ARM64
            }
        }
    }
#endif // FEATURE_HW_INTRINSICS

    return false;
}

//-------------------------------------------------------------------
// IsSIMDZero: returns true if this this is a SIMD vector
// with all its elements equal to zero.
//
// Returns:
//     True if this represents an integral const SIMD vector.
//
inline bool GenTree::IsSIMDZero() const
{
#ifdef FEATURE_SIMD
    if ((gtOper == GT_SIMD) && (AsSIMD()->gtSIMDIntrinsicID == SIMDIntrinsicInit))
    {
        return (gtGetOp1()->IsIntegralConst(0) || gtGetOp1()->IsFPZero());
    }
#endif

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
//    false:     the GenTree node is not accepted as a valid argumeny
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
    if (OperIsList())
    {
        return false;
    }
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
/* static */
inline bool GenTree::RequiresNonNullOp2(genTreeOps oper)
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
        case GT_INDEX:
        case GT_ASG:
        case GT_EQ:
        case GT_NE:
        case GT_LT:
        case GT_LE:
        case GT_GE:
        case GT_GT:
        case GT_COMMA:
        case GT_QMARK:
        case GT_COLON:
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

    // Only allow null op2 if the node type allows it, e.g. GT_LIST.
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

inline GenTree* GenTree::gtEffectiveVal(bool commaOnly /* = false */)
{
    GenTree* effectiveVal = this;
    for (;;)
    {
        assert(!effectiveVal->OperIs(GT_PUTARG_TYPE));
        if (effectiveVal->gtOper == GT_COMMA)
        {
            effectiveVal = effectiveVal->AsOp()->gtGetOp2();
        }
        else if (!commaOnly && (effectiveVal->gtOper == GT_NOP) && (effectiveVal->AsOp()->gtOp1 != nullptr))
        {
            effectiveVal = effectiveVal->AsOp()->gtOp1;
        }
        else
        {
            return effectiveVal;
        }
    }
}

//-------------------------------------------------------------------------
// gtCommaAssignVal - find value being assigned to a comma wrapped assigment
//
// Returns:
//    tree representing value being assigned if this tree represents a
//    comma-wrapped local definition and use.
//
//    original tree, of not.
//
inline GenTree* GenTree::gtCommaAssignVal()
{
    GenTree* result = this;

    if (OperIs(GT_COMMA))
    {
        GenTree* commaOp1 = AsOp()->gtOp1;
        GenTree* commaOp2 = AsOp()->gtOp2;

        if (commaOp2->OperIs(GT_LCL_VAR) && commaOp1->OperIs(GT_ASG))
        {
            GenTree* asgOp1 = commaOp1->AsOp()->gtOp1;
            GenTree* asgOp2 = commaOp1->AsOp()->gtOp2;

            if (asgOp1->OperIs(GT_LCL_VAR) && (asgOp1->AsLclVar()->GetLclNum() == commaOp2->AsLclVar()->GetLclNum()))
            {
                result = asgOp2;
            }
        }
    }

    return result;
}

//-------------------------------------------------------------------------
// gtSkipPutArgType - skip PUTARG_TYPE if it is presented.
//
// Returns:
//    the original tree or its child if it was a PUTARG_TYPE.
//
// Notes:
//   PUTARG_TYPE should be skipped when we are doing transformations
//   that are not affected by ABI, for example: inlining, implicit byref morphing.
//
inline GenTree* GenTree::gtSkipPutArgType()
{
    if (OperIs(GT_PUTARG_TYPE))
    {
        GenTree* res = AsUnOp()->gtGetOp1();
        assert(!res->OperIs(GT_PUTARG_TYPE));
        return res;
    }
    return this;
}

inline GenTree* GenTree::gtSkipReloadOrCopy()
{
    // There can be only one reload or copy (we can't have a reload/copy of a reload/copy)
    if (gtOper == GT_RELOAD || gtOper == GT_COPY)
    {
        assert(gtGetOp1()->OperGet() != GT_RELOAD && gtGetOp1()->OperGet() != GT_COPY);
        return gtGetOp1();
    }
    return this;
}

//-----------------------------------------------------------------------------------
// IsMultiRegCall: whether a call node returning its value in more than one register
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a multi register returning call
inline bool GenTree::IsMultiRegCall() const
{
    if (this->IsCall())
    {
        return AsCall()->HasMultiRegRetVal();
    }

    return false;
}

inline bool GenTree::IsMultiRegLclVar() const
{
    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
    {
        return AsLclVar()->IsMultiReg();
    }
    return false;
}

//-----------------------------------------------------------------------------------
// IsMultiRegNode: whether a node returning its value in more than one register
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a multi-reg node.
//
// Notes:
//     All targets that support multi-reg ops of any kind also support multi-reg return
//     values for calls. Should that change with a future target, this method will need
//     to change accordingly.
//
inline bool GenTree::IsMultiRegNode() const
{
#if FEATURE_MULTIREG_RET
    if (IsMultiRegCall())
    {
        return true;
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        return true;
    }
#endif

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return true;
    }
#endif

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return true;
    }
#endif // FEATURE_MULTIREG_RET
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    if (OperIs(GT_HWINTRINSIC))
    {
        return (TypeGet() == TYP_STRUCT);
    }
#endif
    if (IsMultiRegLclVar())
    {
        return true;
    }
    return false;
}
//-----------------------------------------------------------------------------------
// GetMultiRegCount: Return the register count for a multi-reg node.
//
// Arguments:
//     None
//
// Return Value:
//     Returns the number of registers defined by this node.
//
inline unsigned GenTree::GetMultiRegCount()
{
#if FEATURE_MULTIREG_RET
    if (IsMultiRegCall())
    {
        return AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
    }

#if FEATURE_ARG_SPLIT
    if (OperIsPutArgSplit())
    {
        return AsPutArgSplit()->gtNumRegs;
    }
#endif

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegCount();
    }
#endif

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return AsCopyOrReload()->GetRegCount();
    }
#endif // FEATURE_MULTIREG_RET
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    if (OperIs(GT_HWINTRINSIC))
    {
        assert(TypeGet() == TYP_STRUCT);
        return 2;
    }
#endif
    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
    {
        assert((gtFlags & GTF_VAR_MULTIREG) != 0);
        // The register count for a multireg lclVar requires looking at the LclVarDsc,
        // which requires a Compiler instance. The caller must handle this separately.
        // The register count for a multireg lclVar requires looking at the LclVarDsc,
        // which requires a Compiler instance. The caller must use the GetFieldCount
        // method on GenTreeLclVar.

        assert(!"MultiRegCount for LclVar");
    }
    assert(!"GetMultiRegCount called with non-multireg node");
    return 1;
}

//-----------------------------------------------------------------------------------
// GetRegByIndex: Get a specific register, based on regIndex, that is produced
//                by this node.
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
inline regNumber GenTree::GetRegByIndex(int regIndex)
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

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return AsCopyOrReload()->GetRegNumByIdx(regIndex);
    }
#endif // FEATURE_MULTIREG_RET
#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    if (OperIs(GT_HWINTRINSIC))
    {
        assert(regIndex == 1);
        return AsHWIntrinsic()->GetOtherReg();
    }
#endif
    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
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
//     regIndex - which register type to return
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
inline var_types GenTree::GetRegTypeByIndex(int regIndex)
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
#endif
#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegType(regIndex);
    }
#endif

#endif // FEATURE_MULTIREG_RET

#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
    if (OperIs(GT_HWINTRINSIC))
    {
        // At this time, the only multi-reg HW intrinsics all return the type of their
        // arguments. If this changes, we will need a way to record or determine this.
        assert(TypeGet() == TYP_STRUCT);
        return gtGetOp1()->TypeGet();
    }
#endif
    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
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
#endif

#if !defined(TARGET_64BIT)
    if (OperIsMultiRegOp())
    {
        return AsMultiRegOp()->GetRegSpillFlagByIdx(regIndex);
    }
#endif

#endif // FEATURE_MULTIREG_RET

    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
    {
        return AsLclVar()->GetRegSpillFlagByIdx(regIndex);
    }

    assert(!"Invalid node type for GetRegSpillFlagByIdx");
    return GTF_EMPTY;
}

//-----------------------------------------------------------------------------------
// GetLastUseBit: Get the last use bit for regIndex
//
// Arguments:
//     regIndex - the register index
//
// Return Value:
//     The bit to set, clear or query for the last-use of the regIndex'th value.
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline GenTreeFlags GenTree::GetLastUseBit(int regIndex)
{
    assert(regIndex < 4);
    assert(OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_COPY, GT_RELOAD));
    static_assert_no_msg((1 << MULTIREG_LAST_USE_SHIFT) == GTF_VAR_MULTIREG_DEATH0);
    return (GenTreeFlags)(1 << (MULTIREG_LAST_USE_SHIFT + regIndex));
}

//-----------------------------------------------------------------------------------
// IsLastUse: Determine whether this node is a last use of the regIndex'th value
//
// Arguments:
//     regIndex - the register index
//
// Return Value:
//     true iff this is a last use.
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline bool GenTree::IsLastUse(int regIndex)
{
    assert(OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR, GT_COPY, GT_RELOAD));
    return (gtFlags & GetLastUseBit(regIndex)) != 0;
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
inline bool GenTree::HasLastUse()
{
    return (gtFlags & (GTF_VAR_DEATH_MASK)) != 0;
}

//-----------------------------------------------------------------------------------
// SetLastUse: Set the last use bit for the given index
//
// Arguments:
//     regIndex - the register index
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline void GenTree::SetLastUse(int regIndex)
{
    gtFlags |= GetLastUseBit(regIndex);
}

//-----------------------------------------------------------------------------------
// ClearLastUse: Clear the last use bit for the given index
//
// Arguments:
//     regIndex - the register index
//
// Notes:
//     This must be a GenTreeLclVar or GenTreeCopyOrReload node.
//
inline void GenTree::ClearLastUse(int regIndex)
{
    gtFlags &= ~GetLastUseBit(regIndex);
}

//-------------------------------------------------------------------------
// IsCopyOrReload: whether this is a GT_COPY or GT_RELOAD node.
//
// Arguments:
//     None
//
// Return Value:
//     Returns true if this GenTree is a copy or reload node.
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
    return OperGet() == GT_CNS_DBL;
}

inline bool GenTree::IsCnsNonZeroFltOrDbl()
{
    if (OperGet() == GT_CNS_DBL)
    {
        double constValue = AsDblCon()->gtDconVal;
        return *(__int64*)&constValue != 0;
    }

    return false;
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

/*****************************************************************************/

#ifndef HOST_64BIT
#include <poppack.h>
#endif

/*****************************************************************************/

const size_t TREE_NODE_SZ_SMALL = sizeof(GenTreeLclFld);
const size_t TREE_NODE_SZ_LARGE = sizeof(GenTreeCall);

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
