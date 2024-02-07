// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               GenTree                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "hwintrinsic.h"
#include "simd.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************/

const unsigned char GenTree::gtOperKindTable[] = {
#define GTNODE(en, st, cm, ivn, ok) ((ok)&GTK_MASK) + GTK_COMMUTE *cm,
#include "gtlist.h"
};

#ifdef DEBUG
const GenTreeDebugOperKind GenTree::gtDebugOperKindTable[] = {
#define GTNODE(en, st, cm, ivn, ok) static_cast<GenTreeDebugOperKind>((ok)&DBK_MASK),
#include "gtlist.h"
};
#endif // DEBUG

/*****************************************************************************
 *
 *  The types of different GenTree nodes
 */

#ifdef DEBUG

#define INDENT_SIZE 3

//--------------------------------------------
//
// IndentStack: This struct is used, along with its related enums and strings,
//    to control both the indendtation and the printing of arcs.
//
// Notes:
//    The mode of printing is set in the Constructor, using its 'compiler' argument.
//    Currently it only prints arcs when fgOrder == fgOrderLinear.
//    The type of arc to print is specified by the IndentInfo enum, and is controlled
//    by the caller of the Push() method.

enum IndentChars
{
    ICVertical,
    ICBottom,
    ICTop,
    ICMiddle,
    ICDash,
    ICTerminal,
    ICError,
    IndentCharCount
};

// clang-format off
// Sets of strings for different dumping options            vert             bot             top             mid             dash       embedded    terminal    error
static const char*  emptyIndents[IndentCharCount]   = {     " ",             " ",            " ",            " ",            " ",            "",        "?"  };
static const char*  asciiIndents[IndentCharCount]   = {     "|",            "\\",            "/",            "+",            "-",            "*",       "?"  };
static const char*  unicodeIndents[IndentCharCount] = { "\xe2\x94\x82", "\xe2\x94\x94", "\xe2\x94\x8c", "\xe2\x94\x9c", "\xe2\x94\x80", "\xe2\x96\x8c", "?"  };
// clang-format on

typedef ArrayStack<Compiler::IndentInfo> IndentInfoStack;
struct IndentStack
{
    IndentInfoStack stack;
    const char**    indents;

    // Constructor for IndentStack.  Uses 'compiler' to determine the mode of printing.
    IndentStack(Compiler* compiler) : stack(compiler->getAllocator(CMK_DebugOnly))
    {
        if (compiler->asciiTrees)
        {
            indents = asciiIndents;
        }
        else
        {
            indents = unicodeIndents;
        }
    }

    // Return the depth of the current indentation.
    unsigned Depth()
    {
        return stack.Height();
    }

    // Push a new indentation onto the stack, of the given type.
    void Push(Compiler::IndentInfo info)
    {
        stack.Push(info);
    }

    // Pop the most recent indentation type off the stack.
    Compiler::IndentInfo Pop()
    {
        return stack.Pop();
    }

    // Print the current indentation and arcs.
    void print()
    {
        unsigned indentCount = Depth();
        for (unsigned i = 0; i < indentCount; i++)
        {
            unsigned index = indentCount - 1 - i;
            switch (stack.Top(index))
            {
                case Compiler::IndentInfo::IINone:
                    printf("   ");
                    break;
                case Compiler::IndentInfo::IIArc:
                    if (index == 0)
                    {
                        printf("%s%s%s", indents[ICMiddle], indents[ICDash], indents[ICDash]);
                    }
                    else
                    {
                        printf("%s  ", indents[ICVertical]);
                    }
                    break;
                case Compiler::IndentInfo::IIArcBottom:
                    printf("%s%s%s", indents[ICBottom], indents[ICDash], indents[ICDash]);
                    break;
                case Compiler::IndentInfo::IIArcTop:
                    printf("%s%s%s", indents[ICTop], indents[ICDash], indents[ICDash]);
                    break;
                case Compiler::IndentInfo::IIError:
                    printf("%s%s%s", indents[ICError], indents[ICDash], indents[ICDash]);
                    break;
                default:
                    unreached();
            }
        }
        printf("%s", indents[ICTerminal]);
    }
};

//------------------------------------------------------------------------
// printIndent: This is a static method which simply invokes the 'print'
//    method on its 'indentStack' argument.
//
// Arguments:
//    indentStack - specifies the information for the indentation & arcs to be printed
//
// Notes:
//    This method exists to localize the checking for the case where indentStack is null.

static void printIndent(IndentStack* indentStack)
{
    if (indentStack == nullptr)
    {
        return;
    }
    indentStack->print();
}

#endif

#if defined(DEBUG) || CALL_ARG_STATS || COUNT_BASIC_BLOCKS || COUNT_LOOPS || EMITTER_STATS || MEASURE_MEM_ALLOC ||     \
    NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS || DUMP_FLOWGRAPHS

static const char* opNames[] = {
#define GTNODE(en, st, cm, ivn, ok) #en,
#include "gtlist.h"
};

const char* GenTree::OpName(genTreeOps op)
{
    assert((unsigned)op < ArrLen(opNames));

    return opNames[op];
}

#endif

#if MEASURE_NODE_SIZE

static const char* opStructNames[] = {
#define GTNODE(en, st, cm, ivn, ok) #st,
#include "gtlist.h"
};

const char* GenTree::OpStructName(genTreeOps op)
{
    assert((unsigned)op < ArrLen(opStructNames));

    return opStructNames[op];
}

#endif

//
//  We allocate tree nodes in 2 different sizes:
//  - TREE_NODE_SZ_SMALL for most nodes
//  - TREE_NODE_SZ_LARGE for the few nodes (such as calls) that have
//    more fields and take up a lot more space.
//

/* GT_COUNT'th oper is overloaded as 'undefined oper', so allocate storage for GT_COUNT'th oper also */
/* static */
unsigned char GenTree::s_gtNodeSizes[GT_COUNT + 1];

#if NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS

unsigned char GenTree::s_gtTrueSizes[GT_COUNT + 1]{
#define GTNODE(en, st, cm, ivn, ok) sizeof(st),
#include "gtlist.h"
};

#endif // NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS

#if COUNT_AST_OPERS
unsigned GenTree::s_gtNodeCounts[GT_COUNT + 1] = {0};
#endif // COUNT_AST_OPERS

/* static */
void GenTree::InitNodeSize()
{
    /* Set all sizes to 'small' first */

    for (unsigned op = 0; op <= GT_COUNT; op++)
    {
        GenTree::s_gtNodeSizes[op] = TREE_NODE_SZ_SMALL;
    }

    // Now set all of the appropriate entries to 'large'

    // clang-format off
    GenTree::s_gtNodeSizes[GT_CALL]          = TREE_NODE_SZ_LARGE;
#ifdef TARGET_XARCH
    GenTree::s_gtNodeSizes[GT_CNS_VEC]       = TREE_NODE_SZ_LARGE;
#endif // TARGET_XARCH
    GenTree::s_gtNodeSizes[GT_CAST]          = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_FTN_ADDR]      = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_BOX]           = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_INDEX_ADDR]    = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_BOUNDS_CHECK]  = TREE_NODE_SZ_SMALL;
    GenTree::s_gtNodeSizes[GT_ARR_ELEM]      = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_RET_EXPR]      = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_FIELD_ADDR]    = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_CMPXCHG]       = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_QMARK]         = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_STORE_DYN_BLK] = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_INTRINSIC]     = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_ALLOCOBJ]      = TREE_NODE_SZ_LARGE;
#if USE_HELPERS_FOR_INT_DIV
    GenTree::s_gtNodeSizes[GT_DIV]           = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_UDIV]          = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_MOD]           = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_UMOD]          = TREE_NODE_SZ_LARGE;
#endif
#ifdef FEATURE_PUT_STRUCT_ARG_STK
    // TODO-Throughput: This should not need to be a large node. The object info should be
    // obtained from the child node.
    GenTree::s_gtNodeSizes[GT_PUTARG_STK]    = TREE_NODE_SZ_LARGE;
#if FEATURE_ARG_SPLIT
    GenTree::s_gtNodeSizes[GT_PUTARG_SPLIT]  = TREE_NODE_SZ_LARGE;
#endif // FEATURE_ARG_SPLIT
#endif // FEATURE_PUT_STRUCT_ARG_STK

    // This list of assertions should come to contain all GenTree subtypes that are declared
    // "small".
    assert(sizeof(GenTreeLclFld) <= GenTree::s_gtNodeSizes[GT_LCL_FLD]);
    assert(sizeof(GenTreeLclVar) <= GenTree::s_gtNodeSizes[GT_LCL_VAR]);

    static_assert_no_msg(sizeof(GenTree)             <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeUnOp)         <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeOp)           <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeVal)          <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeIntConCommon) <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreePhysReg)      <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeIntCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLngCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeDblCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeStrCon)       <= TREE_NODE_SZ_SMALL);
#ifdef TARGET_XARCH
    static_assert_no_msg(sizeof(GenTreeVecCon)       <= TREE_NODE_SZ_LARGE); // *** large node
#else
    static_assert_no_msg(sizeof(GenTreeVecCon)       <= TREE_NODE_SZ_SMALL);
#endif
    static_assert_no_msg(sizeof(GenTreeLclVarCommon) <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLclVar)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLclFld)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCC)           <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeOpCC)         <= TREE_NODE_SZ_SMALL);
#ifdef TARGET_ARM64
    static_assert_no_msg(sizeof(GenTreeCCMP)         <= TREE_NODE_SZ_SMALL);
#endif
    static_assert_no_msg(sizeof(GenTreeConditional)  <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCast)         <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeBox)          <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeFieldAddr)    <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeFieldList)    <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeColon)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCall)         <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeCmpXchg)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeFptrVal)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeQmark)        <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIntrinsic)    <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIndexAddr)    <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArrLen)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeMDArr)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeBoundsChk)    <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeArrElem)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIndir)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeStoreInd)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeAddrMode)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeBlk)          <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeStoreDynBlk)  <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeRetExpr)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeILOffset)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreePhiArg)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeAllocObj)     <= TREE_NODE_SZ_LARGE); // *** large node
#ifndef FEATURE_PUT_STRUCT_ARG_STK
    static_assert_no_msg(sizeof(GenTreePutArgStk)       <= TREE_NODE_SZ_SMALL);
#else  // FEATURE_PUT_STRUCT_ARG_STK
    // TODO-Throughput: This should not need to be a large node. The object info should be
    // obtained from the child node.
    static_assert_no_msg(sizeof(GenTreePutArgStk)       <= TREE_NODE_SZ_LARGE);
#if FEATURE_ARG_SPLIT
    static_assert_no_msg(sizeof(GenTreePutArgSplit)     <= TREE_NODE_SZ_LARGE);
#endif // FEATURE_ARG_SPLIT
#endif // FEATURE_PUT_STRUCT_ARG_STK

#ifdef FEATURE_HW_INTRINSICS
    static_assert_no_msg(sizeof(GenTreeHWIntrinsic)     <= TREE_NODE_SZ_SMALL);
#endif // FEATURE_HW_INTRINSICS
    // clang-format on
}

size_t GenTree::GetNodeSize() const
{
    return GenTree::s_gtNodeSizes[gtOper];
}

#ifdef DEBUG
bool GenTree::IsNodeProperlySized() const
{
    size_t size;

    if (gtDebugFlags & GTF_DEBUG_NODE_SMALL)
    {
        size = TREE_NODE_SZ_SMALL;
    }
    else
    {
        assert(gtDebugFlags & GTF_DEBUG_NODE_LARGE);
        size = TREE_NODE_SZ_LARGE;
    }

    return GenTree::s_gtNodeSizes[gtOper] <= size;
}
#endif

//------------------------------------------------------------------------
// ReplaceWith: replace this with the src node. The source must be an isolated node
//              and cannot be used after the replacement.
//
// Arguments:
//    src  - source tree, that replaces this.
//    comp - the compiler instance to transfer annotations for arrays.
//
// Remarks:
//    This should not be used for new code.
//
void GenTree::ReplaceWith(GenTree* src, Compiler* comp)
{
    // The source may be big only if the target is also a big node
    assert((gtDebugFlags & GTF_DEBUG_NODE_LARGE) || GenTree::s_gtNodeSizes[src->gtOper] == TREE_NODE_SZ_SMALL);

    // The check is effective only if nodes have been already threaded.
    assert((src->gtPrev == nullptr) && (src->gtNext == nullptr));

    RecordOperBashing(OperGet(), src->OperGet()); // nop unless NODEBASH_STATS is enabled

    GenTree* prev = gtPrev;
    GenTree* next = gtNext;
    // The VTable pointer is copied intentionally here
    memcpy((void*)this, (void*)src, src->GetNodeSize());
    this->gtPrev = prev;
    this->gtNext = next;

#ifdef DEBUG
    gtSeqNum = 0;
#endif
    DEBUG_DESTROY_NODE(src);
}

/*****************************************************************************
 *
 *  When 'NODEBASH_STATS' is enabled in "jit.h" we record all instances of
 *  an existing GenTree node having its operator changed. This can be useful
 *  for two (related) things - to see what is being bashed (and what isn't),
 *  and to verify that the existing choices for what nodes are marked 'large'
 *  are reasonable (to minimize "wasted" space).
 *
 *  And yes, the hash function / logic is simplistic, but it is conflict-free
 *  and transparent for what we need.
 */

#if NODEBASH_STATS

#define BASH_HASH_SIZE 211

inline unsigned hashme(genTreeOps op1, genTreeOps op2)
{
    return ((op1 * 104729) ^ (op2 * 56569)) % BASH_HASH_SIZE;
}

struct BashHashDsc
{
    unsigned __int32 bhFullHash; // the hash value (unique for all old->new pairs)
    unsigned __int32 bhCount;    // the same old->new bashings seen so far
    unsigned __int8  bhOperOld;  // original gtOper
    unsigned __int8  bhOperNew;  // new      gtOper
};

static BashHashDsc BashHash[BASH_HASH_SIZE];

void GenTree::RecordOperBashing(genTreeOps operOld, genTreeOps operNew)
{
    unsigned     hash = hashme(operOld, operNew);
    BashHashDsc* desc = BashHash + hash;

    if (desc->bhFullHash != hash)
    {
        noway_assert(desc->bhCount == 0); // if this ever fires, need fix the hash fn
        desc->bhFullHash = hash;
    }

    desc->bhCount += 1;
    desc->bhOperOld = operOld;
    desc->bhOperNew = operNew;
}

void GenTree::ReportOperBashing(FILE* f)
{
    unsigned total = 0;

    fflush(f);

    fprintf(f, "\n");
    fprintf(f, "Bashed gtOper stats:\n");
    fprintf(f, "\n");
    fprintf(f, "    Old operator        New operator     #bytes old->new      Count\n");
    fprintf(f, "    ---------------------------------------------------------------\n");

    for (unsigned h = 0; h < BASH_HASH_SIZE; h++)
    {
        unsigned count = BashHash[h].bhCount;
        if (count == 0)
            continue;

        unsigned opOld = BashHash[h].bhOperOld;
        unsigned opNew = BashHash[h].bhOperNew;

        fprintf(f, "    GT_%-13s -> GT_%-13s [size: %3u->%3u] %c %7u\n", OpName((genTreeOps)opOld),
                OpName((genTreeOps)opNew), s_gtTrueSizes[opOld], s_gtTrueSizes[opNew],
                (s_gtTrueSizes[opOld] < s_gtTrueSizes[opNew]) ? 'X' : ' ', count);
        total += count;
    }
    fprintf(f, "\n");
    fprintf(f, "Total bashings: %u\n", total);
    fprintf(f, "\n");

    fflush(f);
}

#endif // NODEBASH_STATS

/*****************************************************************************/

#if MEASURE_NODE_SIZE

void GenTree::DumpNodeSizes()
{
    // Dump the sizes of the various GenTree flavors

    FILE* fp = jitstdout();

    fprintf(fp, "Small tree node size = %zu bytes\n", TREE_NODE_SZ_SMALL);
    fprintf(fp, "Large tree node size = %zu bytes\n", TREE_NODE_SZ_LARGE);
    fprintf(fp, "\n");

    // Verify that node sizes are set kosherly and dump sizes
    for (unsigned op = GT_NONE + 1; op < GT_COUNT; op++)
    {
        unsigned needSize = s_gtTrueSizes[op];
        unsigned nodeSize = s_gtNodeSizes[op];

        const char* structNm = OpStructName((genTreeOps)op);
        const char* operName = OpName((genTreeOps)op);

        bool repeated = false;

        // Have we seen this struct flavor before?
        for (unsigned mop = GT_NONE + 1; mop < op; mop++)
        {
            if (strcmp(structNm, OpStructName((genTreeOps)mop)) == 0)
            {
                repeated = true;
                break;
            }
        }

        // Don't repeat the same GenTree flavor unless we have an error
        if (!repeated || needSize > nodeSize)
        {
            unsigned sizeChar = '?';

            if (nodeSize == TREE_NODE_SZ_SMALL)
                sizeChar = 'S';
            else if (nodeSize == TREE_NODE_SZ_LARGE)
                sizeChar = 'L';

            fprintf(fp, "GT_%-16s ... %-19s = %3u bytes (%c)", operName, structNm, needSize, sizeChar);
            if (needSize > nodeSize)
            {
                fprintf(fp, " -- ERROR -- allocation is only %u bytes!", nodeSize);
            }
            else if (needSize <= TREE_NODE_SZ_SMALL && nodeSize == TREE_NODE_SZ_LARGE)
            {
                fprintf(fp, " ... could be small");
            }

            fprintf(fp, "\n");
        }
    }
}

#endif // MEASURE_NODE_SIZE

//-----------------------------------------------------------
// begin: Get the iterator for the beginning of the locals list.
//
// Return Value:
//     Iterator representing the beginning.
//
LocalsGenTreeList::iterator LocalsGenTreeList::begin() const
{
    GenTree* first = m_stmt->GetTreeList();
    assert((first == nullptr) || first->OperIsAnyLocal());
    return iterator(static_cast<GenTreeLclVarCommon*>(first));
}

//-----------------------------------------------------------
// GetForwardEdge: Get the edge that points forward to a node.
//
// Arguments:
//     node - The node the edge should be pointing at.
//
// Return Value:
//     The edge, such that *edge == node.
//
GenTree** LocalsGenTreeList::GetForwardEdge(GenTreeLclVarCommon* node)
{
    if (node->gtPrev == nullptr)
    {
        assert(m_stmt->GetTreeList() == node);
        return m_stmt->GetTreeListPointer();
    }
    else
    {
        assert(node->gtPrev->gtNext == node);
        return &node->gtPrev->gtNext;
    }
}

//-----------------------------------------------------------
// GetBackwardEdge: Get the edge that points backwards to a node.
//
// Arguments:
//     node - The node the edge should be pointing at.
//
// Return Value:
//     The edge, such that *edge == node.
//
GenTree** LocalsGenTreeList::GetBackwardEdge(GenTreeLclVarCommon* node)
{
    if (node->gtNext == nullptr)
    {
        assert(m_stmt->GetTreeListEnd() == node);
        return m_stmt->GetTreeListEndPointer();
    }
    else
    {
        assert(node->gtNext->gtPrev == node);
        return &node->gtNext->gtPrev;
    }
}

//-----------------------------------------------------------
// Remove: Remove a specified node from the locals tree list.
//
// Arguments:
//     node - the local node that should be part of this list.
//
void LocalsGenTreeList::Remove(GenTreeLclVarCommon* node)
{
    GenTree** forwardEdge  = GetForwardEdge(node);
    GenTree** backwardEdge = GetBackwardEdge(node);

    *forwardEdge  = node->gtNext;
    *backwardEdge = node->gtPrev;
}

//-----------------------------------------------------------
// Replace: Replace a sequence of nodes with another (already linked) sequence of nodes.
//
// Arguments:
//     firstNode - The first node, part of this locals tree list, to be replaced.
//     lastNode - The last node, part of this locals tree list, to be replaced.
//     newFirstNode - The start of the replacement sub list.
//     newLastNode - The last node of the replacement sub list.
//
void LocalsGenTreeList::Replace(GenTreeLclVarCommon* firstNode,
                                GenTreeLclVarCommon* lastNode,
                                GenTreeLclVarCommon* newFirstNode,
                                GenTreeLclVarCommon* newLastNode)
{
    assert((newFirstNode != nullptr) && (newLastNode != nullptr));

    GenTree** forwardEdge  = GetForwardEdge(firstNode);
    GenTree** backwardEdge = GetBackwardEdge(lastNode);

    GenTree* prev = firstNode->gtPrev;
    GenTree* next = lastNode->gtNext;

    *forwardEdge         = newFirstNode;
    *backwardEdge        = newLastNode;
    newFirstNode->gtPrev = prev;
    newLastNode->gtNext  = next;
}

//-----------------------------------------------------------
// TreeList: convenience method for enabling range-based `for` iteration over the
// execution order of the GenTree linked list, e.g.:
//    for (GenTree* const tree : stmt->TreeList()) ...
//
// Only valid between fgSetBlockOrder and rationalization. See fgNodeThreading.
//
// Return Value:
//   The tree list.
//
GenTreeList Statement::TreeList() const
{
    assert(JitTls::GetCompiler()->fgNodeThreading == NodeThreading::AllTrees);
    return GenTreeList(GetTreeList());
}

//-----------------------------------------------------------
// LocalsTreeList: Manages the locals tree list and allows for range-based
// iteration.
//
// Only valid between local morph and forward sub. See fgNodeThreading.
//
// Return Value:
//    The locals tree list.
//
LocalsGenTreeList Statement::LocalsTreeList()
{
    assert(JitTls::GetCompiler()->fgNodeThreading == NodeThreading::AllLocals);
    return LocalsGenTreeList(this);
}

/*****************************************************************************
 *
 *  Walk all basic blocks and call the given function pointer for all tree
 *  nodes contained therein.
 */

void Compiler::fgWalkAllTreesPre(fgWalkPreFn* visitor, void* pCallBackData)
{
    for (BasicBlock* const block : Blocks())
    {
        for (Statement* const stmt : block->Statements())
        {
            fgWalkTreePre(stmt->GetRootNodePointer(), visitor, pCallBackData);
        }
    }
}

//-----------------------------------------------------------
// GetLayout: Get the struct layout for this node.
//
// Arguments:
//     compiler - The Compiler instance
//
// Return Value:
//     The struct layout of this node; it must have one.
//
// Notes:
//     This is the "general" method for getting the layout,
//     the more efficient node-specific ones should be used
//     in case the node's oper is known.
//
ClassLayout* GenTree::GetLayout(Compiler* compiler) const
{
    assert(varTypeIsStruct(TypeGet()));

    CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;
    switch (OperGet())
    {
        case GT_LCL_VAR:
        case GT_STORE_LCL_VAR:
            return compiler->lvaGetDesc(AsLclVar())->GetLayout();

        case GT_LCL_FLD:
        case GT_STORE_LCL_FLD:
            return AsLclFld()->GetLayout();

        case GT_BLK:
        case GT_STORE_BLK:
            return AsBlk()->GetLayout();

        case GT_COMMA:
            return AsOp()->gtOp2->GetLayout(compiler);

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return AsHWIntrinsic()->GetLayout(compiler);
#endif // FEATURE_HW_INTRINSICS

        case GT_MKREFANY:
            structHnd = compiler->impGetRefAnyClass();
            break;

        case GT_CALL:
            structHnd = AsCall()->gtRetClsHnd;
            break;

        case GT_RET_EXPR:
            structHnd = AsRetExpr()->gtInlineCandidate->gtRetClsHnd;
            break;

        default:
            unreached();
    }

    return compiler->typGetObjLayout(structHnd);
}

//-----------------------------------------------------------
// CopyReg: Copy the _gtRegNum/gtRegTag fields.
//
// Arguments:
//     from   -  GenTree node from which to copy
//
void GenTree::CopyReg(GenTree* from)
{
    _gtRegNum = from->_gtRegNum;
    INDEBUG(gtRegTag = from->gtRegTag;)

    // Also copy multi-reg state if this is a call node
    if (IsCall())
    {
        assert(from->IsCall());
        this->AsCall()->CopyOtherRegs(from->AsCall());
    }
    else if (IsCopyOrReload())
    {
        this->AsCopyOrReload()->CopyOtherRegs(from->AsCopyOrReload());
    }
}

//------------------------------------------------------------------
// gtHasReg: Whether node been assigned a register by LSRA
//
// Arguments:
//    comp - Compiler instance. Required for multi-reg lcl var; ignored otherwise.
//
// Return Value:
//    Returns true if the node was assigned a register.
//
//    In case of multi-reg call nodes, it is considered having a reg if regs are allocated for ALL its
//    return values.
//    REVIEW: why is this ALL and the other cases are ANY? Explain.
//
//    In case of GT_COPY or GT_RELOAD of a multi-reg call, GT_COPY/GT_RELOAD is considered having a reg if it
//    has a reg assigned to ANY of its positions.
//
//    In case of multi-reg local vars, it is considered having a reg if it has a reg assigned for ANY
//    of its positions.
//
bool GenTree::gtHasReg(Compiler* comp) const
{
    bool hasReg = false;

    if (IsMultiRegCall())
    {
        const GenTreeCall* call     = AsCall();
        const unsigned     regCount = call->GetReturnTypeDesc()->GetReturnRegCount();

        // A Multi-reg call node is said to have regs, if it has
        // reg assigned to each of its result registers.
        for (unsigned i = 0; i < regCount; ++i)
        {
            hasReg = (call->GetRegNumByIdx(i) != REG_NA);
            if (!hasReg)
            {
                break;
            }
        }
    }
    else if (IsCopyOrReloadOfMultiRegCall())
    {
        const GenTreeCopyOrReload* copyOrReload = AsCopyOrReload();
        const GenTreeCall*         call         = copyOrReload->gtGetOp1()->AsCall();
        const unsigned             regCount     = call->GetReturnTypeDesc()->GetReturnRegCount();

        // A Multi-reg copy or reload node is said to have regs,
        // if it has valid regs in any of the positions.
        for (unsigned i = 0; i < regCount; ++i)
        {
            hasReg = (copyOrReload->GetRegNumByIdx(i) != REG_NA);
            if (hasReg)
            {
                break;
            }
        }
    }
    else if (IsMultiRegLclVar())
    {
        assert(comp != nullptr);
        const GenTreeLclVar* lclNode  = AsLclVar();
        const unsigned       regCount = lclNode->GetFieldCount(comp);
        // A Multi-reg local vars is said to have regs,
        // if it has valid regs in any of the positions.
        for (unsigned i = 0; i < regCount; i++)
        {
            hasReg = (lclNode->GetRegNumByIdx(i) != REG_NA);
            if (hasReg)
            {
                break;
            }
        }
    }
    else
    {
        hasReg = (GetRegNum() != REG_NA);
    }

    return hasReg;
}

//-----------------------------------------------------------------------------
// GetRegisterDstCount: Get the number of registers defined by the node.
//
// Arguments:
//    None
//
// Return Value:
//    The number of registers that this node defines.
//
// Notes:
//    This should not be called on a contained node.
//    This does not look at the actual register assignments, if any, and so
//    is valid after Lowering.
//
int GenTree::GetRegisterDstCount(Compiler* compiler) const
{
    assert(!isContained());
    if (!IsMultiRegNode())
    {
        return IsValue() ? 1 : 0;
    }
    else if (IsMultiRegCall())
    {
        return AsCall()->GetReturnTypeDesc()->GetReturnRegCount();
    }
    else if (IsCopyOrReload())
    {
        return gtGetOp1()->GetRegisterDstCount(compiler);
    }
#if FEATURE_ARG_SPLIT
    else if (OperIsPutArgSplit())
    {
        return (const_cast<GenTree*>(this))->AsPutArgSplit()->gtNumRegs;
    }
#endif
#if !defined(TARGET_64BIT)
    else if (OperIsMultiRegOp())
    {
        // A MultiRegOp is a GT_MUL_LONG, GT_PUTARG_REG, or GT_BITCAST.
        // For the latter two (ARM-only), they only have multiple registers if they produce a long value
        // (GT_MUL_LONG always produces a long value).
        CLANG_FORMAT_COMMENT_ANCHOR;
#ifdef TARGET_ARM
        return (TypeGet() == TYP_LONG) ? 2 : 1;
#else
        assert(OperIs(GT_MUL_LONG));
        return 2;
#endif
    }
#endif
#ifdef FEATURE_HW_INTRINSICS
    else if (OperIsHWIntrinsic())
    {
        assert(TypeIs(TYP_STRUCT));

        const GenTreeHWIntrinsic* intrinsic   = AsHWIntrinsic();
        const NamedIntrinsic      intrinsicId = intrinsic->GetHWIntrinsicId();
        assert(HWIntrinsicInfo::IsMultiReg(intrinsicId));

        return HWIntrinsicInfo::GetMultiRegCount(intrinsicId);
    }
#endif // FEATURE_HW_INTRINSICS

    if (OperIsScalarLocal())
    {
        return AsLclVar()->GetFieldCount(compiler);
    }
    assert(!"Unexpected multi-reg node");
    return 0;
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
bool GenTree::IsMultiRegNode() const
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
        return AsMultiRegOp()->GetRegCount() > 1;
    }
#endif
#endif // FEATURE_MULTIREG_RET

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return true;
    }

#ifdef FEATURE_HW_INTRINSICS
    if (OperIsHWIntrinsic())
    {
        return HWIntrinsicInfo::IsMultiReg(AsHWIntrinsic()->GetHWIntrinsicId());
    }
#endif // FEATURE_HW_INTRINSICS

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
//     comp - Compiler instance. Required for MultiRegLclVar, unused otherwise.
//
// Return Value:
//     Returns the number of registers defined by this node.
//
unsigned GenTree::GetMultiRegCount(Compiler* comp) const
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
#endif // FEATURE_MULTIREG_RET

    if (OperIs(GT_COPY, GT_RELOAD))
    {
        return AsCopyOrReload()->GetRegCount();
    }

#ifdef FEATURE_HW_INTRINSICS
    if (OperIsHWIntrinsic())
    {
        return HWIntrinsicInfo::GetMultiRegCount(AsHWIntrinsic()->GetHWIntrinsicId());
    }
#endif // FEATURE_HW_INTRINSICS

    if (IsMultiRegLclVar())
    {
        assert(comp != nullptr);
        return AsLclVar()->GetFieldCount(comp);
    }

    assert(!"GetMultiRegCount called with non-multireg node");
    return 1;
}

#ifdef TARGET_ARM64
//-----------------------------------------------------------------------------------
// NeedsConsecutiveRegisters: Checks if this tree node needs consecutive registers
//
// Return Value:
//     Returns if the tree needs consecutive registers.
//
bool GenTree::NeedsConsecutiveRegisters() const
{
    return HWIntrinsicInfo::NeedsConsecutiveRegisters(AsHWIntrinsic()->GetHWIntrinsicId());
}
#endif

//---------------------------------------------------------------
// gtGetContainedRegMask: Get the reg mask of the node including
//    contained nodes (recursive).
//
// Arguments:
//    None
//
// Return Value:
//    Reg Mask of GenTree node.
//
regMaskTP GenTree::gtGetContainedRegMask()
{
    if (!isContained())
    {
        return isUsedFromReg() ? gtGetRegMask() : RBM_NONE;
    }

    regMaskTP mask = 0;
    for (GenTree* operand : Operands())
    {
        mask |= operand->gtGetContainedRegMask();
    }
    return mask;
}

//---------------------------------------------------------------
// gtGetRegMask: Get the reg mask of the node.
//
// Arguments:
//    None
//
// Return Value:
//    Reg Mask of GenTree node.
//
regMaskTP GenTree::gtGetRegMask() const
{
    regMaskTP resultMask;

    if (IsMultiRegCall())
    {
        resultMask = genRegMask(GetRegNum());
        resultMask |= AsCall()->GetOtherRegMask();
    }
    else if (IsCopyOrReloadOfMultiRegCall())
    {
        // A multi-reg copy or reload, will have valid regs for only those
        // positions that need to be copied or reloaded.  Hence we need
        // to consider only those registers for computing reg mask.

        const GenTreeCopyOrReload* copyOrReload = AsCopyOrReload();
        const GenTreeCall*         call         = copyOrReload->gtGetOp1()->AsCall();
        const unsigned             regCount     = call->GetReturnTypeDesc()->GetReturnRegCount();

        resultMask = RBM_NONE;
        for (unsigned i = 0; i < regCount; ++i)
        {
            regNumber reg = copyOrReload->GetRegNumByIdx(i);
            if (reg != REG_NA)
            {
                resultMask |= genRegMask(reg);
            }
        }
    }
#if FEATURE_ARG_SPLIT
    else if (compFeatureArgSplit() && OperIsPutArgSplit())
    {
        const GenTreePutArgSplit* splitArg = AsPutArgSplit();
        const unsigned            regCount = splitArg->gtNumRegs;

        resultMask = RBM_NONE;
        for (unsigned i = 0; i < regCount; ++i)
        {
            regNumber reg = splitArg->GetRegNumByIdx(i);
            assert(reg != REG_NA);
            resultMask |= genRegMask(reg);
        }
    }
#endif // FEATURE_ARG_SPLIT
    else
    {
        resultMask = genRegMask(GetRegNum());
    }

    return resultMask;
}

void GenTreeFieldList::AddField(Compiler* compiler, GenTree* node, unsigned offset, var_types type)
{
    m_uses.AddUse(new (compiler, CMK_ASTNode) Use(node, offset, type));
    gtFlags |= node->gtFlags & GTF_ALL_EFFECT;
}

void GenTreeFieldList::AddFieldLIR(Compiler* compiler, GenTree* node, unsigned offset, var_types type)
{
    m_uses.AddUse(new (compiler, CMK_ASTNode) Use(node, offset, type));
}

void GenTreeFieldList::InsertField(Compiler* compiler, Use* insertAfter, GenTree* node, unsigned offset, var_types type)
{
    m_uses.InsertUse(insertAfter, new (compiler, CMK_ASTNode) Use(node, offset, type));
    gtFlags |= node->gtFlags & GTF_ALL_EFFECT;
}

void GenTreeFieldList::InsertFieldLIR(
    Compiler* compiler, Use* insertAfter, GenTree* node, unsigned offset, var_types type)
{
    m_uses.InsertUse(insertAfter, new (compiler, CMK_ASTNode) Use(node, offset, type));
}

//---------------------------------------------------------------
// IsHfaArg: Is this arg considered a homogeneous floating-point aggregate?
//
bool CallArgABIInformation::IsHfaArg() const
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        return IsHfa(GetHfaElemKind());
    }
    else
    {
        return false;
    }
}

//---------------------------------------------------------------
// IsHfaRegArg: Is this an HFA argument passed in registers?
//
bool CallArgABIInformation::IsHfaRegArg() const
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        return IsHfa(GetHfaElemKind()) && IsPassedInRegisters();
    }
    else
    {
        return false;
    }
}

//---------------------------------------------------------------
// GetHfaType: Get the type of each element of the HFA arg.
//
var_types CallArgABIInformation::GetHfaType() const
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        return HfaTypeFromElemKind(GetHfaElemKind());
    }
    else
    {
        return TYP_UNDEF;
    }
}

//---------------------------------------------------------------
// SetHfaType: Set the type of each element of the HFA arg.
//
// Arguments:
//   type     - The new type for each element
//   hfaSlots - How many registers are used by the HFA.
//
// Remarks:
//   This can only be called after the passing mode of the argument (registers
//   or stack) has been determined. When passing HFAs of doubles on ARM it is
//   expected that `hfaSlots` refers to the number of float registers used,
//   i.e. twice the number of doubles being passed. This function will convert
//   that into double registers and set `NumRegs` appropriately.
//
void CallArgABIInformation::SetHfaType(var_types type, unsigned hfaSlots)
{
    if (GlobalJitOptions::compFeatureHfa)
    {
        if (type != TYP_UNDEF)
        {
            // We must already have set the passing mode.
            assert(NumRegs != 0 || GetStackByteSize() != 0);
            // We originally set numRegs according to the size of the struct, but if the size of the
            // hfaType is not the same as the pointer size, we need to correct it.
            // Note that hfaSlots is the number of registers we will use. For ARM, that is twice
            // the number of "double registers".
            unsigned numHfaRegs = hfaSlots;
#ifdef TARGET_ARM
            if (type == TYP_DOUBLE)
            {
                // Must be an even number of registers.
                assert((NumRegs & 1) == 0);
                numHfaRegs = hfaSlots / 2;
            }
#endif // TARGET_ARM

            if (!IsHfaArg())
            {
                // We haven't previously set this; do so now.
                CorInfoHFAElemType elemKind = HfaElemKindFromType(type);
                SetHfaElemKind(elemKind);
                // Ensure we've allocated enough bits.
                assert(GetHfaElemKind() == elemKind);
                if (IsPassedInRegisters())
                {
                    NumRegs = numHfaRegs;
                }
            }
            else
            {
                // We've already set this; ensure that it's consistent.
                if (IsPassedInRegisters())
                {
                    assert(NumRegs == numHfaRegs);
                }
                assert(type == HfaTypeFromElemKind(GetHfaElemKind()));
            }
        }
    }
}

//---------------------------------------------------------------
// SetByteSize: Set information related to this argument's size and alignment.
//
// Arguments:
//   byteSize      - The size in bytes of the argument.
//   byteAlignment - The alignment in bytes of the argument.
//   isStruct      - Whether this arg is a struct.
//   isFloatHfa    - Whether this is a float HFA.
//
// Remarks:
//   This function will determine how the argument size needs to be rounded. On
//   most ABIs all arguments are rounded to stack pointer size, but Apple arm64
//   ABI is an exception as it allows packing some small arguments into the
//   same stack slot.
//
void CallArgABIInformation::SetByteSize(unsigned byteSize, unsigned byteAlignment, bool isStruct, bool isFloatHfa)
{
    unsigned roundedByteSize;
    if (compAppleArm64Abi())
    {
        // Only struct types need extension or rounding to pointer size, but HFA<float> does not.
        if (isStruct && !isFloatHfa)
        {
            roundedByteSize = roundUp(byteSize, TARGET_POINTER_SIZE);
        }
        else
        {
            roundedByteSize = byteSize;
        }
    }
    else
    {
        roundedByteSize = roundUp(byteSize, TARGET_POINTER_SIZE);
    }

#if !defined(TARGET_ARM)
    // Arm32 could have a struct with 8 byte alignment
    // which rounded size % 8 is not 0.
    assert(byteAlignment != 0);
    assert(roundedByteSize % byteAlignment == 0);
#endif // TARGET_ARM

    ByteSize      = roundedByteSize;
    ByteAlignment = byteAlignment;
}

//---------------------------------------------------------------
// SetMultiRegsNumw: Set the registers for a multi-reg arg using 'sequential' registers.
//
// Remarks:
//   This assumes that `NumRegs` and the first reg num has already been set and
//   determines how many sequential registers are necessary to pass the
//   argument.
//   Note that on ARM the registers set may skip odd float registers if the arg
//   is a HFA of doubles, since double and float registers overlap.
void CallArgABIInformation::SetMultiRegNums()
{
#if FEATURE_MULTIREG_ARGS && !defined(UNIX_AMD64_ABI) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
    if (NumRegs == 1)
    {
        return;
    }

    regNumber argReg = GetRegNum(0);
#ifdef TARGET_ARM
    unsigned int regSize = (GetHfaType() == TYP_DOUBLE) ? 2 : 1;
#else
    unsigned int regSize = 1;
#endif

    if (NumRegs > MAX_ARG_REG_COUNT)
        NO_WAY("Multireg argument exceeds the maximum length");

    for (unsigned int regIndex = 1; regIndex < NumRegs; regIndex++)
    {
        argReg = (regNumber)(argReg + regSize);
        SetRegNum(regIndex, argReg);
    }
#endif // FEATURE_MULTIREG_ARGS && !defined(UNIX_AMD64_ABI) && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)
}

//---------------------------------------------------------------
// GetStackByteSize: Get the number of stack bytes used to pass this argument.
//
// Returns:
//   For pure register arguments, this returns 0.
//   For pure stack arguments, this returns ByteSize.
//   For split arguments the return value is between 0 and ByteSize.
//
unsigned CallArgABIInformation::GetStackByteSize() const
{
    if (!IsSplit() && NumRegs > 0)
    {
        return 0;
    }

    assert(!IsHfaArg() || !IsSplit());

    assert(ByteSize > TARGET_POINTER_SIZE * NumRegs);
    const unsigned stackByteSize = ByteSize - TARGET_POINTER_SIZE * NumRegs;
    return stackByteSize;
}

#ifdef DEBUG
void NewCallArg::ValidateTypes()
{
    assert(Compiler::impCheckImplicitArgumentCoercion(SignatureType, Node->TypeGet()));

    if (varTypeIsStruct(SignatureType))
    {
        assert(SignatureClsHnd != NO_CLASS_HANDLE);
        assert(SignatureType == Node->TypeGet());

        if (SignatureType == TYP_STRUCT)
        {
            Compiler* comp = JitTls::GetCompiler();
            assert(ClassLayout::AreCompatible(comp->typGetObjLayout(SignatureClsHnd), Node->GetLayout(comp)));
        }
    }
}
#endif

//---------------------------------------------------------------
// IsArgAddedLate: Check if this is an argument that is added late, by
//                 `DetermineArgABIInformation`.
//
// Remarks:
//   These arguments must be removed if ABI information needs to be
//   reclassified by calling `DetermineArgABIInformation` as otherwise they
//   will be readded. See `CallArgs::ResetFinalArgsAndABIInfo`.
//
//   Note that the 'late' here is separate from CallArg::GetLateNode and
//   friends. Late here refers to this being an argument that is added by morph
//   instead of the importer.
//
bool CallArg::IsArgAddedLate() const
{
    // static_cast to "enum class" for old gcc support
    switch (static_cast<WellKnownArg>(m_wellKnownArg))
    {
        case WellKnownArg::WrapperDelegateCell:
        case WellKnownArg::VirtualStubCell:
        case WellKnownArg::PInvokeCookie:
        case WellKnownArg::PInvokeTarget:
        case WellKnownArg::R2RIndirectionCell:
            return true;
        default:
            return false;
    }
}

//---------------------------------------------------------------
// IsUserArg: Check if this is an argument that can be treated as
//   user-defined (in IL).
//
// Remarks:
//   "this" and ShiftLow/ShiftHigh are recognized as user-defined
//
bool CallArg::IsUserArg() const
{
    switch (static_cast<WellKnownArg>(m_wellKnownArg))
    {
        case WellKnownArg::None:
        case WellKnownArg::ShiftLow:
        case WellKnownArg::ShiftHigh:
        case WellKnownArg::ThisPointer:
            return true;
        default:
            return false;
    }
}

#ifdef DEBUG
//---------------------------------------------------------------
// CheckIsStruct: Verify that the struct ABI information is consistent with the IR node.
//
void CallArg::CheckIsStruct()
{
    GenTree* node = GetNode();
    if (varTypeIsStruct(GetSignatureType()))
    {
        if (!varTypeIsStruct(node) && !node->OperIs(GT_FIELD_LIST))
        {
            // This is the case where we are passing a struct as a primitive type.
            // On most targets, this is always a single register or slot.
            // However, on ARM this could be two slots if it is TYP_DOUBLE.
            bool isPassedAsPrimitiveType =
                ((AbiInfo.NumRegs == 1) || ((AbiInfo.NumRegs == 0) && (AbiInfo.ByteSize <= TARGET_POINTER_SIZE)));
#ifdef TARGET_ARM
            if (!isPassedAsPrimitiveType)
            {
                if (node->TypeGet() == TYP_DOUBLE && AbiInfo.NumRegs == 0 && (AbiInfo.GetStackSlotsNumber() == 2))
                {
                    isPassedAsPrimitiveType = true;
                }
            }
#endif // TARGET_ARM
            assert(isPassedAsPrimitiveType);
        }
    }
    else
    {
        assert(!varTypeIsStruct(node));
    }
}
#endif

CallArgs::CallArgs()
    : m_head(nullptr)
    , m_lateHead(nullptr)
    , m_nextStackByteOffset(0)
#ifdef UNIX_X86_ABI
    , m_stkSizeBytes(0)
    , m_padStkAlign(0)
#endif
    , m_hasThisPointer(false)
    , m_hasRetBuffer(false)
    , m_isVarArgs(false)
    , m_abiInformationDetermined(false)
    , m_hasRegArgs(false)
    , m_hasStackArgs(false)
    , m_argsComplete(false)
    , m_needsTemps(false)
#ifdef UNIX_X86_ABI
    , m_alignmentDone(false)
#endif
{
}

//---------------------------------------------------------------
// FindByNode: Find the argument containing the specified early or late node.
//
// Parameters:
//   node - The node to find.
//
// Returns:
//   A pointer to the found CallArg, or otherwise nullptr.
//
CallArg* CallArgs::FindByNode(GenTree* node)
{
    assert(node != nullptr);
    for (CallArg& arg : Args())
    {
        if ((arg.GetEarlyNode() == node) || (arg.GetLateNode() == node))
        {
            return &arg;
        }
    }

    return nullptr;
}

//---------------------------------------------------------------
// FindWellKnownArg: Find a specific well-known argument.
//
// Parameters:
//   arg - The type of well-known argument.
//
// Returns:
//   A pointer to the found CallArg, or null if it was not found.
//
// Remarks:
//   For the 'this' arg or the return buffer arg there are more efficient
//   alternatives available in `GetThisArg` and `GetRetBufferArg`.
//
CallArg* CallArgs::FindWellKnownArg(WellKnownArg arg)
{
    assert(arg != WellKnownArg::None);
    for (CallArg& callArg : Args())
    {
        if (callArg.GetWellKnownArg() == arg)
        {
            return &callArg;
        }
    }

    return nullptr;
}

//---------------------------------------------------------------
// GetThisArg: Get the this-pointer argument.
//
// Returns:
//   A pointer to the 'this' arg, or nullptr if there is no such arg.
//
// Remarks:
//   This is only the managed 'this' arg. We consider the 'this' pointer for
//   unmanaged instance calling conventions as normal (non-this) arguments.
//
CallArg* CallArgs::GetThisArg()
{
    if (!HasThisPointer())
    {
        return nullptr;
    }

    // For calls that do have 'this' pointer the loop is cheap as this is
    // almost always the first or second argument.
    CallArg* result = FindWellKnownArg(WellKnownArg::ThisPointer);
    assert(result && "Expected to find this pointer argument");
    return result;
}

//---------------------------------------------------------------
// GetRetBufferArg: Get the return buffer arg.
//
// Returns:
//   A pointer to the ret-buffer arg, or nullptr if there is no such arg.
//
// Remarks:
//   This is the actual (per-ABI) return buffer argument. On some ABIs this
//   argument has special treatment. Notably on standard ARM64 calling
//   convention it is passed in x8 (see `CallArgs::GetCustomRegister` for the
//   exact conditions).
//
//   Some jit helpers may have "out buffers" that are _not_ classified as the
//   ret buffer. These are normal arguments that function similarly to ret
//   buffers, but they do not have the special ABI treatment of ret buffers.
//   See `GenTreeCall::TreatAsShouldHaveRetBufArg` for more details.
//
CallArg* CallArgs::GetRetBufferArg()
{
    if (!HasRetBuffer())
    {
        return nullptr;
    }

    CallArg* result = FindWellKnownArg(WellKnownArg::RetBuffer);
    assert(result && "Expected to find ret buffer argument");
    return result;
}

//---------------------------------------------------------------
// GetArgByIndex: Get an argument with the specified index.
//
// Parameters:
//   index - The index of the argument to find.
//
// Returns:
//   A pointer to the argument.
//
// Remarks:
//   This function assumes enough arguments exist.
//
CallArg* CallArgs::GetArgByIndex(unsigned index)
{
    CallArg* cur = m_head;
    for (unsigned i = 0; i < index; i++)
    {
        assert((cur != nullptr) && "Not enough arguments in GetArgByIndex");
        cur = cur->GetNext();
    }

    return cur;
}

//---------------------------------------------------------------
// GetUserArgByIndex: Get an argument with the specified index.
//   Unlike GetArgByIndex, this function ignores non-user args
//   like r2r cells.
//
// Parameters:
//   index - The index of the argument to find.
//
// Returns:
//   A pointer to the argument.
//
// Remarks:
//   This function assumes enough arguments exist. Also, see IsUserArg's
//   comments
//
CallArg* CallArgs::GetUserArgByIndex(unsigned index)
{
    CallArg* cur = m_head;
    assert((cur != nullptr) && "Not enough user arguments in GetUserArgByIndex");
    for (unsigned i = 0; i < index || !cur->IsUserArg();)
    {
        if (cur->IsUserArg())
        {
            i++;
        }
        cur = cur->GetNext();
        assert((cur != nullptr) && "Not enough user arguments in GetUserArgByIndex");
    }
    return cur;
}

//---------------------------------------------------------------
// GetIndex: Get the index for the specified argument.
//
// Parameters:
//   arg - The argument to obtain the index of.
//
// Returns:
//   The index.
//
unsigned CallArgs::GetIndex(CallArg* arg)
{
    unsigned i = 0;
    for (CallArg& a : Args())
    {
        if (&a == arg)
        {
            return i;
        }

        i++;
    }

    assert(!"Could not find argument in arg list");
    return (unsigned)-1;
}

//---------------------------------------------------------------
// Reverse: Reverse the specified subrange of arguments.
//
// Parameters:
//   index - The index of the sublist to reverse.
//   count - The length of the sublist to reverse.
//
// Remarks:
//   This function is used for x86 stdcall/cdecl that passes arguments in the
//   opposite order of the managed calling convention.
//
void CallArgs::Reverse(unsigned index, unsigned count)
{
    CallArg** headSlot = &m_head;
    for (unsigned i = 0; i < index; i++)
    {
        assert(*headSlot != nullptr);
        headSlot = &(*headSlot)->NextRef();
    }

    if (count > 1)
    {
        CallArg* newEnd = *headSlot;
        CallArg* cur    = (*headSlot)->GetNext();

        for (unsigned i = 1; i < count; i++)
        {
            CallArg* next = cur->GetNext();
            cur->SetNext(*headSlot);
            *headSlot = cur;
            cur       = next;
        }
        newEnd->SetNext(cur);
    }
}

//---------------------------------------------------------------
// AddedWellKnownArg: Record details when a well known arg was added.
//
// Parameters:
//   arg - The type of well-known arg that was just added.
//
// Remarks:
//   This is used to improve performance of some common argument lookups.
//
void CallArgs::AddedWellKnownArg(WellKnownArg arg)
{
    switch (arg)
    {
        case WellKnownArg::ThisPointer:
            m_hasThisPointer = true;
            break;
        case WellKnownArg::RetBuffer:
            m_hasRetBuffer = true;
            break;
        default:
            break;
    }
}

//---------------------------------------------------------------
// RemovedWellKnownArg: Record details when a well known arg was removed.
//
// Parameters:
//   arg - The type of well-known arg that was just removed.
//
void CallArgs::RemovedWellKnownArg(WellKnownArg arg)
{
    switch (arg)
    {
        case WellKnownArg::ThisPointer:
            assert(FindWellKnownArg(arg) == nullptr);
            m_hasThisPointer = false;
            break;
        case WellKnownArg::RetBuffer:
            assert(FindWellKnownArg(arg) == nullptr);
            m_hasRetBuffer = false;
            break;
        default:
            break;
    }
}

//---------------------------------------------------------------
// GetCustomRegister: Get the custom, non-standard register assignment for an argument.
//
// Parameters:
//   comp - The compiler.
//   cc   - The calling convention.
//   arg  - The kind of argument.
//
// Returns:
//   The custom register assignment, or REG_NA if this is a normally treated
//   argument.
//
// Remarks:
//   Many JIT helpers have custom calling conventions in order to improve
//   performance. The pattern in those cases is to add a WellKnownArg for the
//   arguments that are passed specially and teach this function how to pass
//   them. Note that we only support passing such arguments in custom registers
//   and generally never on stack.
//
regNumber CallArgs::GetCustomRegister(Compiler* comp, CorInfoCallConvExtension cc, WellKnownArg arg)
{
    switch (arg)
    {
#if defined(TARGET_X86) || defined(TARGET_ARM)
        // The x86 and arm32 CORINFO_HELP_INIT_PINVOKE_FRAME helpers have a custom calling convention.
        case WellKnownArg::PInvokeFrame:
            return REG_PINVOKE_FRAME;
#endif
#if defined(TARGET_ARM)
        // A non-standard calling convention using wrapper delegate invoke is used
        // on ARM, only, for wrapper delegates. It is used for VSD delegate calls
        // where the VSD custom calling convention ABI requires passing R4, a
        // callee-saved register, with a special value. Since R4 is a callee-saved
        // register, its value needs to be preserved. Thus, the VM uses a wrapper
        // delegate IL stub, which preserves R4 and also sets up R4 correctly for
        // the VSD call. The VM is simply reusing an existing mechanism (wrapper
        // delegate IL stub) to achieve its goal for delegate VSD call. See
        // COMDelegate::NeedsWrapperDelegate() in the VM for details.
        case WellKnownArg::WrapperDelegateCell:
            return comp->virtualStubParamInfo->GetReg();
#endif
#if defined(TARGET_X86)
        // The x86 shift helpers have custom calling conventions and expect the lo
        // part of the long to be in EAX and the hi part to be in EDX.
        case WellKnownArg::ShiftLow:
            return REG_LNGARG_LO;
        case WellKnownArg::ShiftHigh:
            return REG_LNGARG_HI;
#endif
        case WellKnownArg::RetBuffer:
            if (hasFixedRetBuffReg())
            {
                // Windows does not use fixed ret buff arg for instance calls, but does otherwise.
                if (!TargetOS::IsWindows || !callConvIsInstanceMethodCallConv(cc))
                {
                    return theFixedRetBuffReg();
                }
            }

            break;

        case WellKnownArg::VirtualStubCell:
            return comp->virtualStubParamInfo->GetReg();

        case WellKnownArg::PInvokeCookie:
            return REG_PINVOKE_COOKIE_PARAM;

        case WellKnownArg::PInvokeTarget:
            return REG_PINVOKE_TARGET_PARAM;

        case WellKnownArg::R2RIndirectionCell:
            return REG_R2R_INDIRECT_PARAM;

        case WellKnownArg::ValidateIndirectCallTarget:
            if (REG_VALIDATE_INDIRECT_CALL_ADDR != REG_ARG_0)
            {
                return REG_VALIDATE_INDIRECT_CALL_ADDR;
            }

            break;

#ifdef REG_DISPATCH_INDIRECT_CELL_ADDR
        case WellKnownArg::DispatchIndirectCallTarget:
            return REG_DISPATCH_INDIRECT_CALL_ADDR;
#endif
        default:
            break;
    }

    return REG_NA;
}

//---------------------------------------------------------------
// IsNonStandard: Check if an argument is passed with a non-standard calling
// convention.
//
// Parameters:
//   comp - The compiler object.
//   call - The call node containing these args.
//   arg  - The specific arg to check whether is non-standard.
//
// Returns:
//   True if the argument is non-standard.
//
bool CallArgs::IsNonStandard(Compiler* comp, GenTreeCall* call, CallArg* arg)
{
    return GetCustomRegister(comp, call->GetUnmanagedCallConv(), arg->GetWellKnownArg()) != REG_NA;
}

//---------------------------------------------------------------
// PushFront: Create a new argument at the front of the argument list.
//
// Parameters:
//   comp         - The compiler.
//   arg          - The builder for the new arg.
//
// Returns:
//   The created representative for the argument.
//
CallArg* CallArgs::PushFront(Compiler* comp, const NewCallArg& arg)
{
    CallArg* callArg = new (comp, CMK_CallArgs) CallArg(arg);
    callArg->SetNext(m_head);
    m_head = callArg;
    AddedWellKnownArg(arg.WellKnownArg);
    return callArg;
}

//---------------------------------------------------------------
// PushBack: Create a new argument at the back of the argument list.
//
// Parameters:
//   comp         - The compiler.
//   arg          - The builder for the new arg.
//
// Returns:
//   The created representative for the argument.
//
CallArg* CallArgs::PushBack(Compiler* comp, const NewCallArg& arg)
{
    CallArg** slot = &m_head;
    while (*slot != nullptr)
    {
        slot = &(*slot)->NextRef();
    }

    *slot = new (comp, CMK_CallArgs) CallArg(arg);
    AddedWellKnownArg(arg.WellKnownArg);
    return *slot;
}

//---------------------------------------------------------------
// InsertAfter: Create a new argument after another argument.
//
// Parameters:
//   comp         - The compiler.
//   after        - The existing argument to insert the new argument after.
//   arg          - The builder for the new arg.
//
// Returns:
//   The created representative for the argument.
//
CallArg* CallArgs::InsertAfter(Compiler* comp, CallArg* after, const NewCallArg& arg)
{
#ifdef DEBUG
    bool found = false;
    for (CallArg& arg : Args())
    {
        if (&arg == after)
        {
            found = true;
            break;
        }
    }

    assert(found && "Could not find arg to insert after in argument list");
#endif

    return InsertAfterUnchecked(comp, after, arg);
}

//---------------------------------------------------------------
// InsertAfterUnchecked: Create a new argument after another argument, without debug checks.
//
// Parameters:
//   comp         - The compiler.
//   after        - The existing argument to insert the new argument after.
//   arg          - The builder for the new arg.
//
// Returns:
//   The created representative for the argument.
//
CallArg* CallArgs::InsertAfterUnchecked(Compiler* comp, CallArg* after, const NewCallArg& arg)
{
    CallArg* newArg = new (comp, CMK_CallArgs) CallArg(arg);
    newArg->SetNext(after->GetNext());
    after->SetNext(newArg);
    AddedWellKnownArg(arg.WellKnownArg);
    return newArg;
}

//---------------------------------------------------------------
// InsertInstParam: Insert an instantiation parameter/generic context argument.
//
// Parameters:
//   comp         - The compiler.
//   node         - The IR node for the instantiation parameter.
//
// Returns:
//   The created representative for the argument.
//
// Remarks:
//   The instantiation parameter is a normal parameter, but its position in the
//   arg list depends on a few factors. It is inserted at the end on x86 and on
//   other platforms must always come after the ret-buffer and the 'this'
//   argument.
//
CallArg* CallArgs::InsertInstParam(Compiler* comp, GenTree* node)
{
    NewCallArg newArg = NewCallArg::Primitive(node).WellKnown(WellKnownArg::InstParam);

    if (Target::g_tgtArgOrder == Target::ARG_ORDER_R2L)
    {
        CallArg* retBufferArg = GetRetBufferArg();
        if (retBufferArg != nullptr)
        {
            return InsertAfter(comp, retBufferArg, newArg);
        }
        else
        {
            return InsertAfterThisOrFirst(comp, newArg);
        }
    }
    else
    {
        return PushBack(comp, newArg);
    }
}

//---------------------------------------------------------------
// InsertAfterThisOrFirst: Insert an argument after 'this' if the call has a
//                         'this' argument, or otherwise first.
//
// Parameters:
//   comp         - The compiler.
//   arg          - The builder for the new arg.
//
// Returns:
//   The created representative for the argument.
//
CallArg* CallArgs::InsertAfterThisOrFirst(Compiler* comp, const NewCallArg& arg)
{
    CallArg* thisArg = GetThisArg();
    if (thisArg != nullptr)
    {
        return InsertAfter(comp, thisArg, arg);
    }
    else
    {
        return PushFront(comp, arg);
    }
}

//---------------------------------------------------------------
// PushLateBack: Insert an argument at the end of the 'late' argument list.
//
// Parameters:
//   arg - The arg to add to the late argument list.
//
// Remarks:
//   This function should only be used if adding arguments after the call has
//   already been morphed.
//
void CallArgs::PushLateBack(CallArg* arg)
{
    CallArg** slot = &m_lateHead;
    while (*slot != nullptr)
    {
        slot = &(*slot)->LateNextRef();
    }

    *slot = arg;
}

//---------------------------------------------------------------
// Remove: Remove an argument from the argument list.
//
// Parameters:
//   arg - The arg to remove.
//
// Remarks:
//   This function cannot be used after morph. It will also invalidate ABI
//   information, so it is expected that `CallArgs::AddFinalArgsAndDetermineABIInfo`
//   was not called yet or that `CallArgs::ResetFinalArgsAndABIInfo` has been
//   called prior to this.
//
void CallArgs::Remove(CallArg* arg)
{
    assert(!m_abiInformationDetermined && !m_argsComplete);

    CallArg** slot = &m_head;
    while (*slot != nullptr)
    {
        if (*slot == arg)
        {
            *slot = arg->GetNext();
            RemovedWellKnownArg(arg->GetWellKnownArg());
            return;
        }

        slot = &(*slot)->NextRef();
    }

    assert(!"Did not find arg to remove in CallArgs::Remove");
}

//---------------------------------------------------------------
// GetOtherRegMask: Get the reg mask of gtOtherRegs of call node
//
// Arguments:
//    None
//
// Return Value:
//    Reg mask of gtOtherRegs of call node.
//
regMaskTP GenTreeCall::GetOtherRegMask() const
{
    regMaskTP resultMask = RBM_NONE;

#if FEATURE_MULTIREG_RET
    for (unsigned i = 0; i < MAX_RET_REG_COUNT - 1; ++i)
    {
        if (gtOtherRegs[i] != REG_NA)
        {
            resultMask |= genRegMask((regNumber)gtOtherRegs[i]);
            continue;
        }
        break;
    }
#endif

    return resultMask;
}

//-------------------------------------------------------------------------
// IsPure:
//    Returns true if this call is pure. For now, this uses the same
//    definition of "pure" that is that used by HelperCallProperties: a
//    pure call does not read or write any aliased (e.g. heap) memory or
//    have other global side effects (e.g. class constructors, finalizers),
//    but is allowed to throw an exception.
//
//    NOTE: this call currently only returns true if the call target is a
//    helper method that is known to be pure. No other analysis is
//    performed.
//
// Arguments:
//    compiler - the compiler context.
//
// Returns:
//    True if the call is pure; false otherwise.
//
bool GenTreeCall::IsPure(Compiler* compiler) const
{
    if (IsHelperCall())
    {
        return compiler->s_helperCallProperties.IsPure(compiler->eeGetHelperNum(gtCallMethHnd));
    }
    // If needed, we can annotate other special intrinsic methods as pure as well.
    return IsSpecialIntrinsic(compiler, NI_System_Type_GetTypeFromHandle);
}

//------------------------------------------------------------------------------
// getArrayLengthFromAllocation: Return the array length for an array allocation
//                               helper call.
//
// Arguments:
//    tree           - The array allocation helper call.
//    block          - tree's basic block.
//
// Return Value:
//    Return the array length node.

GenTree* Compiler::getArrayLengthFromAllocation(GenTree* tree DEBUGARG(BasicBlock* block))
{
    assert(tree != nullptr);

    GenTree* arrayLength = nullptr;

    if (tree->OperGet() == GT_CALL)
    {
        GenTreeCall* call = tree->AsCall();

        if (call->gtCallType == CT_HELPER)
        {
            CorInfoHelpFunc helper = eeGetHelperNum(call->gtCallMethHnd);
            switch (helper)
            {
                case CORINFO_HELP_NEWARR_1_MAYBEFROZEN:
                case CORINFO_HELP_NEWARR_1_DIRECT:
                case CORINFO_HELP_NEWARR_1_OBJ:
                case CORINFO_HELP_NEWARR_1_VC:
                case CORINFO_HELP_NEWARR_1_ALIGN8:
                {
                    // This is an array allocation site. Grab the array length node.
                    arrayLength = call->gtArgs.GetUserArgByIndex(1)->GetNode();
                    break;
                }

                default:
                    break;
            }

            assert((arrayLength == nullptr) || ((optMethodFlags & OMF_HAS_NEWARRAY) != 0));
        }
    }

    if (arrayLength != nullptr)
    {
        arrayLength = arrayLength->OperIsPutArg() ? arrayLength->gtGetOp1() : arrayLength;
    }

    return arrayLength;
}

//-------------------------------------------------------------------------
// SetSingleInlineCandidateInfo: set a single inline candidate info in the current call.
//
// Arguments:
//     candidateInfo - inline candidate info
//
void GenTreeCall::SetSingleInlineCandidateInfo(InlineCandidateInfo* candidateInfo)
{
    if (candidateInfo != nullptr)
    {
        gtFlags |= GTF_CALL_INLINE_CANDIDATE;
        gtInlineInfoCount = 1;
    }
    else
    {
        gtInlineInfoCount = 0;
        gtFlags &= ~GTF_CALL_INLINE_CANDIDATE;
    }
    gtInlineCandidateInfo = candidateInfo;
    ClearGuardedDevirtualizationCandidate();
}

//-------------------------------------------------------------------------
// GetGDVCandidateInfo: Get GDV candidate info in the current call by index.
//
// Return Value:
//     GDV candidate info
//
InlineCandidateInfo* GenTreeCall::GetGDVCandidateInfo(uint8_t index)
{
    assert(index < gtInlineInfoCount);
    if (gtInlineInfoCount > 1)
    {
        // In this case we should access it through gtInlineCandidateInfoList
        return gtInlineCandidateInfoList->at(index);
    }
    return gtInlineCandidateInfo;
}

//-------------------------------------------------------------------------
// AddGDVCandidateInfo: Record a guarded devirtualization (GDV) candidate info
//     for this call. A call can't have more than MAX_GDV_TYPE_CHECKS number of candidates
//
// Arguments:
//     comp          - Compiler instance
//     candidateInfo - GDV candidate info
//
void GenTreeCall::AddGDVCandidateInfo(Compiler* comp, InlineCandidateInfo* candidateInfo)
{
    assert((gtCallMoreFlags & GTF_CALL_M_GUARDED_DEVIRT_EXACT) == 0);
    assert(gtInlineInfoCount < MAX_GDV_TYPE_CHECKS);
    assert(candidateInfo != nullptr);

    if (gtInlineInfoCount == 0)
    {
        // Most calls are monomorphic, so we don't need to allocate a vector
        gtInlineCandidateInfo = candidateInfo;
    }
    else if (gtInlineInfoCount == 1)
    {
        // Upgrade gtInlineCandidateInfo to gtInlineCandidateInfoList (vector)
        CompAllocator        allocator      = comp->getAllocator(CMK_Inlining);
        InlineCandidateInfo* firstCandidate = gtInlineCandidateInfo;
        gtInlineCandidateInfoList           = new (allocator) jitstd::vector<InlineCandidateInfo*>(allocator);
        gtInlineCandidateInfoList->push_back(firstCandidate);
        gtInlineCandidateInfoList->push_back(candidateInfo);
    }
    else
    {
        gtInlineCandidateInfoList->push_back(candidateInfo);
    }
    gtCallMoreFlags |= GTF_CALL_M_GUARDED_DEVIRT;
    gtInlineInfoCount++;
}

//-------------------------------------------------------------------------
// RemoveGDVCandidateInfo: Remove a guarded devirtualization (GDV) candidate info
//     by its index. Index must not be greater than gtInlineInfoCount
//     the call will be marked as "has no inline candidates" if the last candidate is removed
//
// Arguments:
//     comp    - Compiler instance
//     index   - GDV candidate to remove
//
void GenTreeCall::RemoveGDVCandidateInfo(Compiler* comp, uint8_t index)
{
    // We change the number of candidates so it's no longer "doesn't need a fallback"
    gtCallMoreFlags &= ~GTF_CALL_M_GUARDED_DEVIRT_EXACT;

    assert(index < gtInlineInfoCount);

    if (gtInlineInfoCount == 1)
    {
        // No longer have any inline candidates
        ClearInlineInfo();
        assert(gtInlineInfoCount == 0);
        return;
    }
    gtInlineCandidateInfoList->erase(gtInlineCandidateInfoList->begin() + index);
    gtInlineInfoCount--;

    // Downgrade gtInlineCandidateInfoList to gtInlineCandidateInfo
    if (gtInlineInfoCount == 1)
    {
        gtInlineCandidateInfo = gtInlineCandidateInfoList->at(0);
    }
}

//-------------------------------------------------------------------------
// HasSideEffects:
//    Returns true if this call has any side effects. All non-helpers are considered to have side-effects. Only helpers
//    that do not mutate the heap, do not run constructors, may not throw, and are either a) pure or b) non-finalizing
//    allocation functions are considered side-effect-free.
//
// Arguments:
//     compiler         - the compiler instance
//     ignoreExceptions - when `true`, ignores exception side effects
//     ignoreCctors     - when `true`, ignores class constructor side effects
//
// Return Value:
//      true if this call has any side-effects; false otherwise.
bool GenTreeCall::HasSideEffects(Compiler* compiler, bool ignoreExceptions, bool ignoreCctors) const
{
    // Generally all GT_CALL nodes are considered to have side-effects, but we may have extra information about helper
    // calls that can prove them side-effect-free.
    if (gtCallType != CT_HELPER)
    {
        // If needed, we can annotate other special intrinsic methods as side effect free as well.
        if (IsSpecialIntrinsic(compiler, NI_System_Type_GetTypeFromHandle))
        {
            return false;
        }

        return true;
    }

    CorInfoHelpFunc       helper           = compiler->eeGetHelperNum(gtCallMethHnd);
    HelperCallProperties& helperProperties = compiler->s_helperCallProperties;

    // We definitely care about the side effects if MutatesHeap is true
    if (helperProperties.MutatesHeap(helper))
    {
        return true;
    }

    // Unless we have been instructed to ignore cctors (CSE, for example, ignores cctors), consider them side effects.
    if (!ignoreCctors && helperProperties.MayRunCctor(helper))
    {
        return true;
    }

    // Consider array allocators side-effect free for constant length (if it's not negative and fits into i32)
    if (helperProperties.IsAllocator(helper))
    {
        GenTree* arrLen = compiler->getArrayLengthFromAllocation((GenTree*)this DEBUGARG(nullptr));
        // if arrLen is nullptr it means it wasn't an array allocator
        if ((arrLen != nullptr) && arrLen->IsIntCnsFitsInI32())
        {
            ssize_t cns = arrLen->AsIntConCommon()->IconValue();
            if ((cns >= 0) && (cns <= CORINFO_Array_MaxLength))
            {
                return false;
            }
        }
    }

    // If we also care about exceptions then check if the helper can throw
    if (!ignoreExceptions && !helperProperties.NoThrow(helper))
    {
        return true;
    }

    // If this is not a Pure helper call or an allocator (that will not need to run a finalizer)
    // then this call has side effects.
    return !helperProperties.IsPure(helper) &&
           (!helperProperties.IsAllocator(helper) || ((gtCallMoreFlags & GTF_CALL_M_ALLOC_SIDE_EFFECTS) != 0));
}

//-------------------------------------------------------------------------
// HasNonStandardAddedArgs: Return true if the method has non-standard args added to the call
// argument list during argument morphing (fgMorphArgs), e.g., passed in R10 or R11 on AMD64.
// See also GetNonStandardAddedArgCount().
//
// Arguments:
//     compiler - the compiler instance
//
// Return Value:
//      true if there are any such args, false otherwise.
//
bool GenTreeCall::HasNonStandardAddedArgs(Compiler* compiler) const
{
    return GetNonStandardAddedArgCount(compiler) != 0;
}

//-------------------------------------------------------------------------
// GetNonStandardAddedArgCount: Get the count of non-standard arguments that have been added
// during call argument morphing (fgMorphArgs). Do not count non-standard args that are already
// counted in the argument list prior to morphing.
//
// This function is used to help map the caller and callee arguments during tail call setup.
//
// Arguments:
//     compiler - the compiler instance
//
// Return Value:
//      The count of args, as described.
//
// Notes:
//      It would be more general to have fgMorphArgs set a bit on the call node when such
//      args are added to a call, and a bit on each such arg, and then have this code loop
//      over the call args when the special call bit is set, counting the args with the special
//      arg bit. This seems pretty heavyweight, though. Instead, this logic needs to be kept
//      in sync with fgMorphArgs.
//
int GenTreeCall::GetNonStandardAddedArgCount(Compiler* compiler) const
{
    if (IsUnmanaged() && !compiler->opts.ShouldUsePInvokeHelpers())
    {
        // R11 = PInvoke cookie param
        return 1;
    }
    else if (IsVirtualStub())
    {
        // R11 = Virtual stub param
        return 1;
    }
    else if ((gtCallType == CT_INDIRECT) && (gtCallCookie != nullptr))
    {
        // R10 = PInvoke target param
        // R11 = PInvoke cookie param
        return 2;
    }
    return 0;
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
//     will make HasRetBufArg() return true, but will also force the
//     use of register x8 to pass the RetBuf argument.
//
//     These two Jit Helpers that we handle here by returning true
//     aren't actually defined to return a struct, so they don't expect
//     their RetBuf to be passed in x8, instead they  expect it in x0.
//
bool GenTreeCall::TreatAsShouldHaveRetBufArg(Compiler* compiler) const
{
    if (ShouldHaveRetBufArg())
    {
        return true;
    }

    // If we see a Jit helper call that returns a TYP_STRUCT we may
    // transform it as if it has a Return Buffer Argument
    //
    if (IsHelperCall() && (gtReturnType == TYP_STRUCT))
    {
        // There are three possible helper calls that use this path:
        //  CORINFO_HELP_GETFIELDSTRUCT,  CORINFO_HELP_UNBOX_NULLABLE
        //  CORINFO_HELP_PINVOKE_CALLI
        CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(gtCallMethHnd);

        if (helpFunc == CORINFO_HELP_GETFIELDSTRUCT)
        {
            return true;
        }
        else if (helpFunc == CORINFO_HELP_UNBOX_NULLABLE)
        {
            return true;
        }
        else if (helpFunc == CORINFO_HELP_PINVOKE_CALLI)
        {
            return false;
        }
        else
        {
            assert(!"Unexpected JIT helper in TreatAsShouldHaveRetBufArg");
        }
    }
    return false;
}

//-------------------------------------------------------------------------
// IsHelperCall: Determine if this GT_CALL node is a specific helper call.
//
// Arguments:
//     compiler - the compiler instance so that we can call eeFindHelper
//
// Return Value:
//     Returns true if this GT_CALL node is a call to the specified helper.
//
bool GenTreeCall::IsHelperCall(Compiler* compiler, unsigned helper) const
{
    return IsHelperCall(compiler->eeFindHelper(helper));
}

//-------------------------------------------------------------------------
// IsRuntimeLookupHelperCall: Determine if this GT_CALL node represents a runtime lookup helper call.
//
// Arguments:
//     compiler - the compiler instance so that we can call eeGetHelperNum
//
// Return Value:
//     Returns true if this GT_CALL node represents a runtime lookup helper call.
//
bool GenTreeCall::IsRuntimeLookupHelperCall(Compiler* compiler) const
{
    if (!IsHelperCall())
    {
        return false;
    }

    switch (compiler->eeGetHelperNum(gtCallMethHnd))
    {
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS:
        case CORINFO_HELP_RUNTIMEHANDLE_METHOD_LOG:
        case CORINFO_HELP_RUNTIMEHANDLE_CLASS_LOG:
            return true;
        default:
            return false;
    }
}

//-------------------------------------------------------------------------
// IsSpecialIntrinsic: Determine if this GT_CALL node is a specific intrinsic.
//
// Arguments:
//     compiler - the compiler instance so that we can call lookupNamedIntrinsic
//     ni       - the intrinsic id
//
// Return Value:
//     Returns true if this GT_CALL node is a special intrinsic call.
//
bool GenTreeCall::IsSpecialIntrinsic(Compiler* compiler, NamedIntrinsic ni) const
{
    return IsSpecialIntrinsic() && (compiler->lookupNamedIntrinsic(gtCallMethHnd) == ni);
}

//-------------------------------------------------------------------------
// GetHelperNum: Get the helper identifier for this call.
//
// Return Value:
//     CORINFO_HELP_UNDEF if this call is not a helper call, appropriate
//     CorInfoHelpFunc otherwise.
//
CorInfoHelpFunc GenTreeCall::GetHelperNum() const
{
    return IsHelperCall() ? Compiler::eeGetHelperNum(gtCallMethHnd) : CORINFO_HELP_UNDEF;
}

//--------------------------------------------------------------------------
// Equals: Check if 2 CALL nodes are equal.
//
// Arguments:
//    c1 - The first call node
//    c2 - The second call node
//
// Return Value:
//    true if the 2 CALL nodes have the same type and operands
//
bool GenTreeCall::Equals(GenTreeCall* c1, GenTreeCall* c2)
{
    assert(c1->OperGet() == c2->OperGet());

    if (c1->TypeGet() != c2->TypeGet())
    {
        return false;
    }

    if (c1->gtCallType != c2->gtCallType)
    {
        return false;
    }

    if (c1->gtCallType != CT_INDIRECT)
    {
        if (c1->gtCallMethHnd != c2->gtCallMethHnd)
        {
            return false;
        }

        if (c1->IsHelperCall() && ((c1->gtCallMoreFlags & GTF_CALL_M_CAST_OBJ_NONNULL) !=
                                   (c2->gtCallMoreFlags & GTF_CALL_M_CAST_OBJ_NONNULL)))
        {
            return false;
        }

#ifdef FEATURE_READYTORUN
        if (c1->gtEntryPoint.addr != c2->gtEntryPoint.addr)
        {
            return false;
        }
#endif

        if ((c1->gtCallType == CT_USER_FUNC) &&
            ((c1->gtFlags & GTF_CALL_VIRT_KIND_MASK) != (c2->gtFlags & GTF_CALL_VIRT_KIND_MASK)))
        {
            return false;
        }
    }
    else
    {
        if (!Compare(c1->gtCallAddr, c2->gtCallAddr))
        {
            return false;
        }
    }

    {
        CallArgs::ArgIterator i1   = c1->gtArgs.Args().begin();
        CallArgs::ArgIterator end1 = c1->gtArgs.Args().end();
        CallArgs::ArgIterator i2   = c2->gtArgs.Args().begin();
        CallArgs::ArgIterator end2 = c2->gtArgs.Args().end();

        for (; (i1 != end1) && (i2 != end2); ++i1, ++i2)
        {
            if (!Compare(i1->GetEarlyNode(), i2->GetEarlyNode()))
            {
                return false;
            }

            if (!Compare(i1->GetLateNode(), i2->GetLateNode()))
            {
                return false;
            }
        }

        if ((i1 != end1) || (i2 != end2))
        {
            return false;
        }
    }

    if (!Compare(c1->gtControlExpr, c2->gtControlExpr))
    {
        return false;
    }

    return true;
}

//--------------------------------------------------------------------------
// ResetFinalArgsAndABIInfo: Reset ABI information classified for arguments,
//                         removing late-added arguments.
//
// Remarks:
//   This function can be called between `CallArgs::AddFinalArgsAndDetermineABIInfo`
//   and actually finishing the morphing of arguments. It cannot be called once
//   the arguments have finished morphing.
//
void CallArgs::ResetFinalArgsAndABIInfo()
{
    if (!IsAbiInformationDetermined())
    {
        return;
    }

    // `CallArgs::AddFinalArgsAndDetermineABIInfo` not only sets up arg info, it
    // also adds non-standard args to the IR, and we need to remove that extra
    // IR so it doesn't get added again.
    CallArg** link = &m_head;

    // We cannot handle this being called after fgMorphArgs, only between
    // CallArgs::AddFinalArgsAndDetermineABIInfo and finishing args.
    assert(!m_argsComplete);

    while ((*link) != nullptr)
    {
        // Check if this is an argument added by AddFinalArgsAndDetermineABIInfo.
        if ((*link)->IsArgAddedLate())
        {
            JITDUMP("Removing arg %s [%06u] to prepare for re-morphing call\n",
                    getWellKnownArgName((*link)->GetWellKnownArg()), Compiler::dspTreeID((*link)->GetNode()));

            *link = (*link)->GetNext();
        }
        else
        {
            link = &(*link)->NextRef();
        }
    }

    m_abiInformationDetermined = false;
}

#if !defined(FEATURE_PUT_STRUCT_ARG_STK)
unsigned GenTreePutArgStk::GetStackByteSize() const
{
    return genTypeSize(genActualType(gtOp1->gtType));
}
#endif // !defined(FEATURE_PUT_STRUCT_ARG_STK)

/*****************************************************************************
 *
 *  Returns non-zero if the two trees are identical.
 */

bool GenTree::Compare(GenTree* op1, GenTree* op2, bool swapOK)
{
    genTreeOps oper;
    unsigned   kind;

AGAIN:

    if (op1 == nullptr)
    {
        return (op2 == nullptr);
    }
    if (op2 == nullptr)
    {
        return false;
    }
    if (op1 == op2)
    {
        return true;
    }

    oper = op1->OperGet();

    /* The operators must be equal */

    if (oper != op2->gtOper)
    {
        return false;
    }

    /* The types must be equal */

    if (op1->gtType != op2->gtType)
    {
        return false;
    }

    /* Overflow must be equal */
    if (op1->gtOverflowEx() != op2->gtOverflowEx())
    {
        return false;
    }

    /* Sensible flags must be equal */
    if ((op1->gtFlags & (GTF_UNSIGNED)) != (op2->gtFlags & (GTF_UNSIGNED)))
    {
        return false;
    }

    /* Figure out what kind of nodes we're comparing */

    kind = op1->OperKind();

    /* Is this a constant node? */

    if (op1->OperIsConst())
    {
        switch (oper)
        {
            case GT_CNS_INT:
                if (op1->AsIntCon()->gtIconVal == op2->AsIntCon()->gtIconVal)
                {
                    return true;
                }
                break;

            case GT_CNS_STR:
                if ((op1->AsStrCon()->gtSconCPX == op2->AsStrCon()->gtSconCPX) &&
                    (op1->AsStrCon()->gtScpHnd == op2->AsStrCon()->gtScpHnd))
                {
                    return true;
                }
                break;

            case GT_CNS_VEC:
            {
                if (GenTreeVecCon::Equals(op1->AsVecCon(), op2->AsVecCon()))
                {
                    return true;
                }
                break;
            }

#if 0
            // TODO-CQ: Enable this in the future
        case GT_CNS_LNG:
            if  (op1->AsLngCon()->gtLconVal == op2->AsLngCon()->gtLconVal)
                return true;
            break;

        case GT_CNS_DBL:
            if  (op1->AsDblCon()->DconValue() == op2->AsDblCon()->DconValue())
                return true;
            break;
#endif
            default:
                break;
        }

        return false;
    }

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
            case GT_LCL_FLD:
                if (op1->AsLclFld()->GetLayout() != op2->AsLclFld()->GetLayout())
                {
                    break;
                }
                FALLTHROUGH;

            case GT_LCL_ADDR:
                if (op1->AsLclFld()->GetLclOffs() != op2->AsLclFld()->GetLclOffs())
                {
                    break;
                }
                FALLTHROUGH;

            case GT_LCL_VAR:
                if (op1->AsLclVarCommon()->GetLclNum() != op2->AsLclVarCommon()->GetLclNum())
                {
                    break;
                }
                return true;

            case GT_NOP:
            case GT_LABEL:
                return true;

            default:
                break;
        }

        return false;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_UNOP)
    {
        if (oper == GT_IND)
        {
            if (op1->TypeIs(TYP_STRUCT))
            {
                // Rare case -- need contextual information to check equality in some cases.
                return false;
            }

            if ((op1->gtFlags & GTF_IND_FLAGS) != (op2->gtFlags & GTF_IND_FLAGS))
            {
                return false;
            }
        }

        if (IsExOp(kind))
        {
            // ExOp operators extend unary operator with extra, non-GenTree* members.  In many cases,
            // these should be included in the comparison.
            switch (oper)
            {
                case GT_STORE_LCL_FLD:
                    if ((op1->AsLclFld()->GetLclOffs() != op2->AsLclFld()->GetLclOffs()) ||
                        (op1->AsLclFld()->GetLayout() != op2->AsLclFld()->GetLayout()))
                    {
                        return false;
                    }
                    FALLTHROUGH;
                case GT_STORE_LCL_VAR:
                    if (op1->AsLclVarCommon()->GetLclNum() != op2->AsLclVarCommon()->GetLclNum())
                    {
                        return false;
                    }
                    break;

                case GT_ARR_LENGTH:
                    if (op1->AsArrLen()->ArrLenOffset() != op2->AsArrLen()->ArrLenOffset())
                    {
                        return false;
                    }
                    break;
                case GT_MDARR_LENGTH:
                case GT_MDARR_LOWER_BOUND:
                    if ((op1->AsMDArr()->Dim() != op2->AsMDArr()->Dim()) ||
                        (op1->AsMDArr()->Rank() != op2->AsMDArr()->Rank()))
                    {
                        return false;
                    }
                    break;
                case GT_CAST:
                    if (op1->AsCast()->gtCastType != op2->AsCast()->gtCastType)
                    {
                        return false;
                    }
                    break;

                case GT_BLK:
                    if (op1->AsBlk()->GetLayout() != op2->AsBlk()->GetLayout())
                    {
                        return false;
                    }

                    if ((op1->gtFlags & GTF_IND_FLAGS) != (op2->gtFlags & GTF_IND_FLAGS))
                    {
                        return false;
                    }

                    break;

                case GT_FIELD_ADDR:
                    if (op1->AsFieldAddr()->gtFldHnd != op2->AsFieldAddr()->gtFldHnd)
                    {
                        return false;
                    }
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_BOX:
                case GT_RUNTIMELOOKUP:
                case GT_ARR_ADDR:
                    break;

                default:
                    assert(!"unexpected unary ExOp operator");
            }
        }
        return Compare(op1->AsOp()->gtOp1, op2->AsOp()->gtOp1);
    }

    if (kind & GTK_BINOP)
    {
        if (IsExOp(kind))
        {
            // ExOp operators extend unary operator with extra, non-GenTree* members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_STORE_BLK:
                    if (op1->AsBlk()->GetLayout() != op2->AsBlk()->GetLayout())
                    {
                        return false;
                    }
                    FALLTHROUGH;

                case GT_STOREIND:
                    if ((op1->gtFlags & GTF_IND_FLAGS) != (op2->gtFlags & GTF_IND_FLAGS))
                    {
                        return false;
                    }
                    break;

                case GT_INTRINSIC:
                    if (op1->AsIntrinsic()->gtIntrinsicName != op2->AsIntrinsic()->gtIntrinsicName)
                    {
                        return false;
                    }
                    break;
                case GT_LEA:
                    if (op1->AsAddrMode()->gtScale != op2->AsAddrMode()->gtScale)
                    {
                        return false;
                    }
                    if (op1->AsAddrMode()->Offset() != op2->AsAddrMode()->Offset())
                    {
                        return false;
                    }
                    break;
                case GT_BOUNDS_CHECK:
                    if (op1->AsBoundsChk()->gtThrowKind != op2->AsBoundsChk()->gtThrowKind)
                    {
                        return false;
                    }
                    break;
                case GT_INDEX_ADDR:
                    if (op1->AsIndexAddr()->gtElemSize != op2->AsIndexAddr()->gtElemSize)
                    {
                        return false;
                    }
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_QMARK:
                    break;

                default:
                    assert(!"unexpected binary ExOp operator");
            }
        }

        if (op1->AsOp()->gtOp2)
        {
            if (!Compare(op1->AsOp()->gtOp1, op2->AsOp()->gtOp1, swapOK))
            {
                if (swapOK && OperIsCommutative(oper) &&
                    ((op1->AsOp()->gtOp1->gtFlags | op1->AsOp()->gtOp2->gtFlags | op2->AsOp()->gtOp1->gtFlags |
                      op2->AsOp()->gtOp2->gtFlags) &
                     GTF_ALL_EFFECT) == 0)
                {
                    if (Compare(op1->AsOp()->gtOp1, op2->AsOp()->gtOp2, swapOK))
                    {
                        op1 = op1->AsOp()->gtOp2;
                        op2 = op2->AsOp()->gtOp1;
                        goto AGAIN;
                    }
                }

                return false;
            }

            op1 = op1->AsOp()->gtOp2;
            op2 = op2->AsOp()->gtOp2;

            goto AGAIN;
        }
        else
        {

            op1 = op1->AsOp()->gtOp1;
            op2 = op2->AsOp()->gtOp1;

            if (!op1)
            {
                return (op2 == nullptr);
            }
            if (!op2)
            {
                return false;
            }

            goto AGAIN;
        }
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_CALL:
            return GenTreeCall::Equals(op1->AsCall(), op2->AsCall());

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return GenTreeHWIntrinsic::Equals(op1->AsHWIntrinsic(), op2->AsHWIntrinsic());
#endif

        case GT_ARR_ELEM:

            if (op1->AsArrElem()->gtArrRank != op2->AsArrElem()->gtArrRank)
            {
                return false;
            }

            // NOTE: gtArrElemSize may need to be handled

            unsigned dim;
            for (dim = 0; dim < op1->AsArrElem()->gtArrRank; dim++)
            {
                if (!Compare(op1->AsArrElem()->gtArrInds[dim], op2->AsArrElem()->gtArrInds[dim]))
                {
                    return false;
                }
            }

            op1 = op1->AsArrElem()->gtArrObj;
            op2 = op2->AsArrElem()->gtArrObj;
            goto AGAIN;

        case GT_PHI:
            return GenTreePhi::Equals(op1->AsPhi(), op2->AsPhi());

        case GT_FIELD_LIST:
            return GenTreeFieldList::Equals(op1->AsFieldList(), op2->AsFieldList());

        case GT_CMPXCHG:
            return Compare(op1->AsCmpXchg()->Addr(), op2->AsCmpXchg()->Addr()) &&
                   Compare(op1->AsCmpXchg()->Data(), op2->AsCmpXchg()->Data()) &&
                   Compare(op1->AsCmpXchg()->Comparand(), op2->AsCmpXchg()->Comparand());

        case GT_STORE_DYN_BLK:
            return Compare(op1->AsStoreDynBlk()->Addr(), op2->AsStoreDynBlk()->Addr()) &&
                   Compare(op1->AsStoreDynBlk()->Data(), op2->AsStoreDynBlk()->Data()) &&
                   Compare(op1->AsStoreDynBlk()->gtDynamicSize, op2->AsStoreDynBlk()->gtDynamicSize);

        default:
            assert(!"unexpected operator");
    }

    return false;
}

//------------------------------------------------------------------------
// gtHasRef: Find out whether the given tree contains a local.
//
// Arguments:
//    tree   - tree to find the local in
//    lclNum - the local's number
//
// Return Value:
//    Whether "tree" has any local nodes that refer to the local.
//
/* static */ bool Compiler::gtHasRef(GenTree* tree, unsigned lclNum)
{
    if (tree == nullptr)
    {
        return false;
    }

    if (tree->OperIsLeaf())
    {
        if (tree->OperIsAnyLocal() && (tree->AsLclVarCommon()->GetLclNum() == lclNum))
        {
            return true;
        }
        if (tree->OperIs(GT_RET_EXPR))
        {
            return gtHasRef(tree->AsRetExpr()->gtInlineCandidate, lclNum);
        }

        return false;
    }

    if (tree->OperIsUnary())
    {
        if (tree->OperIsLocalStore() && (tree->AsLclVarCommon()->GetLclNum() == lclNum))
        {
            return true;
        }

        return gtHasRef(tree->AsUnOp()->gtGetOp1(), lclNum);
    }

    if (tree->OperIsBinary())
    {
        return gtHasRef(tree->AsOp()->gtGetOp1(), lclNum) || gtHasRef(tree->AsOp()->gtGetOp2(), lclNum);
    }

    bool result = false;
    tree->VisitOperands([lclNum, &result](GenTree* operand) -> GenTree::VisitResult {
        if (gtHasRef(operand, lclNum))
        {
            result = true;
            return GenTree::VisitResult::Abort;
        }

        return GenTree::VisitResult::Continue;
    });

    return result;
}

//------------------------------------------------------------------------------
// gtHasLocalsWithAddrOp:
//   Check if this tree contains locals with lvHasLdAddrOp or
//   IsAddressExposed() flags set. Does a full tree walk.
//
// Paramters:
//   tree - the tree
//
// Return Value:
//    True if any sub tree is such a local.
//
bool Compiler::gtHasLocalsWithAddrOp(GenTree* tree)
{
    struct LocalsWithAddrOpVisitor : GenTreeVisitor<LocalsWithAddrOpVisitor>
    {
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        LocalsWithAddrOpVisitor(Compiler* comp) : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            LclVarDsc* varDsc = m_compiler->lvaGetDesc((*use)->AsLclVarCommon());
            if (varDsc->lvHasLdAddrOp || varDsc->IsAddressExposed())
            {
                return WALK_ABORT;
            }

            return WALK_CONTINUE;
        }
    };

    LocalsWithAddrOpVisitor visitor(this);
    return visitor.WalkTree(&tree, nullptr) == WALK_ABORT;
}

//------------------------------------------------------------------------------
// gtHasAddressExposedLocal:
//   Check if this tree contains locals with IsAddressExposed() flags set. Does
//   a full tree walk.
//
// Paramters:
//   tree - the tree
//
// Return Value:
//    True if any sub tree is such a local.
//
bool Compiler::gtHasAddressExposedLocals(GenTree* tree)
{
    struct Visitor : GenTreeVisitor<Visitor>
    {
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        Visitor(Compiler* comp) : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            LclVarDsc* varDsc = m_compiler->lvaGetDesc((*use)->AsLclVarCommon());
            if (varDsc->IsAddressExposed())
            {
                return WALK_ABORT;
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this);
    return visitor.WalkTree(&tree, nullptr) == WALK_ABORT;
}

#ifdef DEBUG

/*****************************************************************************
 *
 *  Helper used to compute hash values for trees.
 */

inline unsigned genTreeHashAdd(unsigned old, unsigned add)
{
    return (old + old / 2) ^ add;
}

inline unsigned genTreeHashAdd(unsigned old, void* add)
{
    return genTreeHashAdd(old, (unsigned)(size_t)add);
}

/*****************************************************************************
 *
 *  Given an arbitrary expression tree, compute a hash value for it.
 */

unsigned Compiler::gtHashValue(GenTree* tree)
{
    genTreeOps oper;
    unsigned   kind;

    unsigned hash = 0;

    GenTree* temp;

AGAIN:
    assert(tree);

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Include the operator value in the hash */

    hash = genTreeHashAdd(hash, oper);

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        size_t add;

        switch (oper)
        {
            UINT64 bits;
            case GT_LCL_VAR:
                add = tree->AsLclVar()->GetLclNum();
                break;
            case GT_LCL_FLD:
                hash = genTreeHashAdd(hash, tree->AsLclFld()->GetLclNum());
                hash = genTreeHashAdd(hash, tree->AsLclFld()->GetLayout());
                add  = tree->AsLclFld()->GetLclOffs();
                break;

            case GT_CNS_INT:
                add = tree->AsIntCon()->gtIconVal;
                break;
            case GT_CNS_LNG:
                bits = (UINT64)tree->AsLngCon()->gtLconVal;
#ifdef HOST_64BIT
                add = bits;
#else // 32-bit host
                add      = genTreeHashAdd(uhi32(bits), ulo32(bits));
#endif
                break;
            case GT_CNS_DBL:
            {
                double dcon = tree->AsDblCon()->DconValue();
                memcpy(&bits, &dcon, sizeof(dcon));
#ifdef HOST_64BIT
                add = bits;
#else // 32-bit host
                add      = genTreeHashAdd(uhi32(bits), ulo32(bits));
#endif
                break;
            }
            case GT_CNS_STR:
                add = tree->AsStrCon()->gtSconCPX;
                break;

            case GT_CNS_VEC:
            {
                GenTreeVecCon* vecCon = tree->AsVecCon();
                add                   = 0;

                switch (vecCon->TypeGet())
                {
#if defined(FEATURE_SIMD)
#if defined(TARGET_XARCH)
                    case TYP_SIMD64:
                    {
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[15]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[14]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[13]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[12]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[11]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[10]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[9]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[8]);
                        FALLTHROUGH;
                    }

                    case TYP_SIMD32:
                    {
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[7]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[6]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[5]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[4]);
                        FALLTHROUGH;
                    }
#endif // TARGET_XARCH

                    case TYP_SIMD16:
                    {
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[3]);
                        FALLTHROUGH;
                    }

                    case TYP_SIMD12:
                    {
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[2]);
                        FALLTHROUGH;
                    }

                    case TYP_SIMD8:
                    {
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[1]);
                        add = genTreeHashAdd(ulo32(add), vecCon->gtSimdVal.u32[0]);
                        break;
                    }
#endif // FEATURE_SIMD

                    default:
                    {
                        unreached();
                    }
                }
                break;
            }

            case GT_JMP:
                add = tree->AsVal()->gtVal1;
                break;

            default:
                add = 0;
                break;
        }

        // clang-format off
        // narrow 'add' into a 32-bit 'val'
        unsigned val;
#ifdef HOST_64BIT
        val = genTreeHashAdd(uhi32(add), ulo32(add));
#else // 32-bit host
        val = add;
#endif
        // clang-format on

        hash = genTreeHashAdd(hash, val);
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    GenTree* op1;

    if (kind & GTK_UNOP)
    {
        op1 = tree->AsOp()->gtOp1;
        /* Special case: no sub-operand at all */

        if (GenTree::IsExOp(kind))
        {
            // ExOp operators extend operators with extra, non-GenTree* members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_STORE_LCL_VAR:
                    hash = genTreeHashAdd(hash, tree->AsLclVar()->GetLclNum());
                    break;
                case GT_STORE_LCL_FLD:
                    hash = genTreeHashAdd(hash, tree->AsLclFld()->GetLclNum());
                    hash = genTreeHashAdd(hash, tree->AsLclFld()->GetLclOffs());
                    hash = genTreeHashAdd(hash, tree->AsLclFld()->GetLayout());
                    break;
                case GT_STOREIND:
                    hash = genTreeHashAdd(hash, tree->AsStoreInd()->GetRMWStatus());
                    break;
                case GT_ARR_LENGTH:
                    hash += tree->AsArrLen()->ArrLenOffset();
                    break;
                case GT_MDARR_LENGTH:
                case GT_MDARR_LOWER_BOUND:
                    hash += tree->AsMDArr()->Dim();
                    hash += tree->AsMDArr()->Rank();
                    break;
                case GT_CAST:
                    hash ^= tree->AsCast()->gtCastType;
                    break;
                case GT_INDEX_ADDR:
                    hash += tree->AsIndexAddr()->gtElemSize;
                    break;
                case GT_ALLOCOBJ:
                    hash = genTreeHashAdd(hash, static_cast<unsigned>(
                                                    reinterpret_cast<uintptr_t>(tree->AsAllocObj()->gtAllocObjClsHnd)));
                    hash = genTreeHashAdd(hash, tree->AsAllocObj()->gtNewHelper);
                    break;
                case GT_RUNTIMELOOKUP:
                    hash = genTreeHashAdd(hash, static_cast<unsigned>(
                                                    reinterpret_cast<uintptr_t>(tree->AsRuntimeLookup()->gtHnd)));
                    break;
                case GT_BLK:
                    hash =
                        genTreeHashAdd(hash,
                                       static_cast<unsigned>(reinterpret_cast<uintptr_t>(tree->AsBlk()->GetLayout())));
                    break;

                case GT_FIELD_ADDR:
                    hash = genTreeHashAdd(hash, tree->AsFieldAddr()->gtFldHnd);
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_BOX:
                case GT_ARR_ADDR:
                    break;

                default:
                    assert(!"unexpected unary ExOp operator");
            }
        }

        if (!op1)
        {
            goto DONE;
        }

        tree = op1;
        goto AGAIN;
    }

    if (kind & GTK_BINOP)
    {
        if (GenTree::IsExOp(kind))
        {
            // ExOp operators extend operators with extra, non-GenTree* members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_STOREIND:
                    hash = genTreeHashAdd(hash, tree->AsStoreInd()->GetRMWStatus());
                    break;

                case GT_STORE_BLK:
                    hash = genTreeHashAdd(hash, tree->AsBlk()->GetLayout());
                    break;

                case GT_INTRINSIC:
                    hash += tree->AsIntrinsic()->gtIntrinsicName;
                    break;
                case GT_LEA:
                    hash += static_cast<unsigned>(tree->AsAddrMode()->Offset() << 3) + tree->AsAddrMode()->gtScale;
                    break;

                case GT_BOUNDS_CHECK:
                    hash = genTreeHashAdd(hash, tree->AsBoundsChk()->gtThrowKind);
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_QMARK:
                case GT_INDEX_ADDR:
                    break;

#ifdef FEATURE_HW_INTRINSICS
                case GT_HWINTRINSIC:
                    hash += tree->AsHWIntrinsic()->GetHWIntrinsicId();
                    hash += tree->AsHWIntrinsic()->GetSimdBaseType();
                    hash += tree->AsHWIntrinsic()->GetSimdSize();
                    hash += tree->AsHWIntrinsic()->GetAuxiliaryType();
                    hash += tree->AsHWIntrinsic()->GetRegByIndex(1);
                    break;
#endif // FEATURE_HW_INTRINSICS

                default:
                    assert(!"unexpected binary ExOp operator");
            }
        }

        op1          = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->AsOp()->gtOp2;

        /* Is there a second sub-operand? */

        if (!op2)
        {
            /* Special case: no sub-operands at all */

            if (!op1)
            {
                goto DONE;
            }

            /* This is a unary operator */

            tree = op1;
            goto AGAIN;
        }

        /* This is a binary operator */

        unsigned hsh1 = gtHashValue(op1);

        /* Add op1's hash to the running value and continue with op2 */

        hash = genTreeHashAdd(hash, hsh1);

        tree = op2;
        goto AGAIN;
    }

    /* See what kind of a special operator we have here */
    switch (tree->gtOper)
    {
        case GT_ARR_ELEM:

            hash = genTreeHashAdd(hash, gtHashValue(tree->AsArrElem()->gtArrObj));

            unsigned dim;
            for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
            {
                hash = genTreeHashAdd(hash, gtHashValue(tree->AsArrElem()->gtArrInds[dim]));
            }

            break;

        case GT_CALL:
            for (CallArg& arg : tree->AsCall()->gtArgs.Args())
            {
                if (arg.GetEarlyNode() != nullptr)
                {
                    hash = genTreeHashAdd(hash, gtHashValue(arg.GetEarlyNode()));
                }

                if (arg.GetLateNode() != nullptr)
                {
                    hash = genTreeHashAdd(hash, gtHashValue(arg.GetLateNode()));
                }
            }

            if (tree->AsCall()->gtCallType == CT_INDIRECT)
            {
                temp = tree->AsCall()->gtCallAddr;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }
            else
            {
                hash = genTreeHashAdd(hash, tree->AsCall()->gtCallMethHnd);
            }

            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            // TODO-List: rewrite with a general visitor / iterator?
            for (GenTree* operand : tree->AsMultiOp()->Operands())
            {
                hash = genTreeHashAdd(hash, gtHashValue(operand));
            }
            break;
#endif // FEATURE_HW_INTRINSICS

        case GT_PHI:
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                hash = genTreeHashAdd(hash, gtHashValue(use.GetNode()));
            }
            break;

        case GT_FIELD_LIST:
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                hash = genTreeHashAdd(hash, gtHashValue(use.GetNode()));
            }
            break;

        case GT_CMPXCHG:
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsCmpXchg()->Addr()));
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsCmpXchg()->Data()));
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsCmpXchg()->Comparand()));
            break;

        case GT_STORE_DYN_BLK:
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsStoreDynBlk()->Data()));
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsStoreDynBlk()->Addr()));
            hash = genTreeHashAdd(hash, gtHashValue(tree->AsStoreDynBlk()->gtDynamicSize));
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
#endif
            assert(!"unexpected operator");
            break;
    }

DONE:

    return hash;
}

#endif // DEBUG

/*****************************************************************************
 *
 *  Return a relational operator that is the reverse of the given one.
 */

/* static */
genTreeOps GenTree::ReverseRelop(genTreeOps relop)
{
    static constexpr genTreeOps reverseOps[] = {
        GT_NE,      // GT_EQ
        GT_EQ,      // GT_NE
        GT_GE,      // GT_LT
        GT_GT,      // GT_LE
        GT_LT,      // GT_GE
        GT_LE,      // GT_GT
        GT_TEST_NE, // GT_TEST_EQ
        GT_TEST_EQ, // GT_TEST_NE
#ifdef TARGET_XARCH
        GT_BITTEST_NE, // GT_BITTEST_EQ
        GT_BITTEST_EQ, // GT_BITTEST_NE
#endif
    };

    static_assert_no_msg(reverseOps[GT_EQ - GT_EQ] == GT_NE);
    static_assert_no_msg(reverseOps[GT_NE - GT_EQ] == GT_EQ);
    static_assert_no_msg(reverseOps[GT_LT - GT_EQ] == GT_GE);
    static_assert_no_msg(reverseOps[GT_LE - GT_EQ] == GT_GT);
    static_assert_no_msg(reverseOps[GT_GE - GT_EQ] == GT_LT);
    static_assert_no_msg(reverseOps[GT_GT - GT_EQ] == GT_LE);
    static_assert_no_msg(reverseOps[GT_TEST_EQ - GT_EQ] == GT_TEST_NE);
    static_assert_no_msg(reverseOps[GT_TEST_NE - GT_EQ] == GT_TEST_EQ);
#ifdef TARGET_XARCH
    static_assert_no_msg(reverseOps[GT_BITTEST_EQ - GT_EQ] == GT_BITTEST_NE);
    static_assert_no_msg(reverseOps[GT_BITTEST_NE - GT_EQ] == GT_BITTEST_EQ);
#endif

    assert(OperIsCompare(relop));
    assert(relop >= GT_EQ && (unsigned)(relop - GT_EQ) < sizeof(reverseOps));

    return reverseOps[relop - GT_EQ];
}

/*****************************************************************************
 *
 *  Return a relational operator that will work for swapped operands.
 */

/* static */
genTreeOps GenTree::SwapRelop(genTreeOps relop)
{
    static constexpr genTreeOps swapOps[] = {
        GT_EQ,      // GT_EQ
        GT_NE,      // GT_NE
        GT_GT,      // GT_LT
        GT_GE,      // GT_LE
        GT_LE,      // GT_GE
        GT_LT,      // GT_GT
        GT_TEST_EQ, // GT_TEST_EQ
        GT_TEST_NE, // GT_TEST_NE
#ifdef TARGET_XARCH
        GT_BITTEST_EQ, // GT_BITTEST_EQ
        GT_BITTEST_NE, // GT_BITTEST_NE
#endif
    };

    static_assert_no_msg(swapOps[GT_EQ - GT_EQ] == GT_EQ);
    static_assert_no_msg(swapOps[GT_NE - GT_EQ] == GT_NE);
    static_assert_no_msg(swapOps[GT_LT - GT_EQ] == GT_GT);
    static_assert_no_msg(swapOps[GT_LE - GT_EQ] == GT_GE);
    static_assert_no_msg(swapOps[GT_GE - GT_EQ] == GT_LE);
    static_assert_no_msg(swapOps[GT_GT - GT_EQ] == GT_LT);
    static_assert_no_msg(swapOps[GT_TEST_EQ - GT_EQ] == GT_TEST_EQ);
    static_assert_no_msg(swapOps[GT_TEST_NE - GT_EQ] == GT_TEST_NE);
#ifdef TARGET_XARCH
    static_assert_no_msg(swapOps[GT_BITTEST_EQ - GT_EQ] == GT_BITTEST_EQ);
    static_assert_no_msg(swapOps[GT_BITTEST_NE - GT_EQ] == GT_BITTEST_NE);
#endif

    assert(OperIsCompare(relop));
    assert(relop >= GT_EQ && (unsigned)(relop - GT_EQ) < sizeof(swapOps));

    return swapOps[relop - GT_EQ];
}

/*****************************************************************************
 *
 *  Reverse the meaning of the given test condition.
 */

GenTree* Compiler::gtReverseCond(GenTree* tree)
{
    if (tree->OperIsCompare())
    {
        tree->SetOper(GenTree::ReverseRelop(tree->OperGet()));

        // Flip the GTF_RELOP_NAN_UN bit
        //     a ord b   === (a != NaN && b != NaN)
        //     a unord b === (a == NaN || b == NaN)
        // => !(a ord b) === (a unord b)
        if (varTypeIsFloating(tree->AsOp()->gtOp1->TypeGet()))
        {
            tree->gtFlags ^= GTF_RELOP_NAN_UN;
        }
    }
    else if (tree->OperIs(GT_JCC, GT_SETCC))
    {
        GenTreeCC* cc   = tree->AsCC();
        cc->gtCondition = GenCondition::Reverse(cc->gtCondition);
    }
    else if (tree->OperIs(GT_JCMP, GT_JTEST))
    {
        GenTreeOpCC* opCC = tree->AsOpCC();
        opCC->gtCondition = GenCondition::Reverse(opCC->gtCondition);
    }
    else
    {
        tree = gtNewOperNode(GT_NOT, TYP_INT, tree);
    }

    return tree;
}

#if !defined(TARGET_64BIT) || defined(TARGET_ARM64)
//------------------------------------------------------------------------------
// IsValidLongMul : Check for long multiplication with 32 bit operands.
//
// Recognizes the following tree: MUL(CAST(long <- int), CAST(long <- int) or CONST),
// where CONST must be an integer constant that fits in 32 bits. Will try to detect
// cases when the multiplication cannot overflow and return "true" for them.
//
// This function does not change the state of the tree and is usable in LIR.
//
// Return Value:
//    Whether this GT_MUL tree is a valid long multiplication candidate.
//
bool GenTreeOp::IsValidLongMul()
{
    assert(OperIs(GT_MUL));

    GenTree* op1 = gtGetOp1();
    GenTree* op2 = gtGetOp2();

    if (!TypeIs(TYP_LONG))
    {
        return false;
    }

    assert(op1->TypeIs(TYP_LONG));
    assert(op2->TypeIs(TYP_LONG));

    if (!(op1->OperIs(GT_CAST) && genActualTypeIsInt(op1->AsCast()->CastOp())))
    {
        return false;
    }

    if (!(op2->OperIs(GT_CAST) && genActualTypeIsInt(op2->AsCast()->CastOp())) &&
        !(op2->IsIntegralConst() && FitsIn<int32_t>(op2->AsIntConCommon()->IntegralValue())))
    {
        return false;
    }

    if (op1->gtOverflow() || op2->gtOverflowEx())
    {
        return false;
    }

    if (gtOverflow())
    {
        auto getMaxValue = [this](GenTree* op) -> int64_t {
            if (op->OperIs(GT_CAST))
            {
                if (op->IsUnsigned())
                {
                    switch (op->AsCast()->CastOp()->TypeGet())
                    {
                        case TYP_UBYTE:
                            return UINT8_MAX;
                        case TYP_USHORT:
                            return UINT16_MAX;
                        default:
                            return UINT32_MAX;
                    }
                }

                return IsUnsigned() ? static_cast<int64_t>(UINT64_MAX) : INT32_MIN;
            }

            return op->AsIntConCommon()->IntegralValue();
        };

        int64_t maxOp1 = getMaxValue(op1);
        int64_t maxOp2 = getMaxValue(op2);

        if (CheckedOps::MulOverflows(maxOp1, maxOp2, IsUnsigned()))
        {
            return false;
        }
    }

    // Both operands must extend the same way.
    bool op1ZeroExtends = op1->IsUnsigned();
    bool op2ZeroExtends = op2->OperIs(GT_CAST) ? op2->IsUnsigned() : op2->AsIntConCommon()->IntegralValue() >= 0;
    bool op2AnyExtensionIsSuitable = op2->IsIntegralConst() && op2ZeroExtends;
    if ((op1ZeroExtends != op2ZeroExtends) && !op2AnyExtensionIsSuitable)
    {
        return false;
    }

    return true;
}

#if !defined(TARGET_64BIT) && defined(DEBUG)
//------------------------------------------------------------------------------
// DebugCheckLongMul : Checks that a GTF_MUL_64RSLT tree is a valid MUL_LONG.
//
// Notes:
//    This function is defined for 32 bit targets only because we *must* maintain
//    the MUL_LONG-compatible tree shape throughout the compilation from morph to
//    decomposition, since we do not have (great) ability to create new calls in LIR.
//
//    It is for this reason that we recognize MUL_LONGs early in morph, mark them with
//    a flag and then pessimize various places (e. g. assertion propagation) to not look
//    at them. In contrast, on ARM64 we recognize MUL_LONGs late, in lowering, and thus
//    do not need this function.
//
void GenTreeOp::DebugCheckLongMul()
{
    assert(OperIs(GT_MUL));
    assert(Is64RsltMul());
    assert(TypeIs(TYP_LONG));
    assert(!gtOverflow());

    GenTree* op1 = gtGetOp1();
    GenTree* op2 = gtGetOp2();

    assert(op1->TypeIs(TYP_LONG));
    assert(op2->TypeIs(TYP_LONG));

    // op1 has to be CAST(long <- int)
    assert(op1->OperIs(GT_CAST) && genActualTypeIsInt(op1->AsCast()->CastOp()));
    assert(!op1->gtOverflow());

    // op2 has to be CAST(long <- int) or a suitably small constant.
    assert((op2->OperIs(GT_CAST) && genActualTypeIsInt(op2->AsCast()->CastOp())) ||
           (op2->IsIntegralConst() && FitsIn<int32_t>(op2->AsIntConCommon()->IntegralValue())));
    assert(!op2->gtOverflowEx());

    // Both operands must extend the same way.
    bool op1ZeroExtends = op1->IsUnsigned();
    bool op2ZeroExtends = op2->OperIs(GT_CAST) ? op2->IsUnsigned() : op2->AsIntConCommon()->IntegralValue() >= 0;
    bool op2AnyExtensionIsSuitable = op2->IsIntegralConst() && op2ZeroExtends;
    assert((op1ZeroExtends == op2ZeroExtends) || op2AnyExtensionIsSuitable);

    // Do unsigned mul iff both operands are zero-extending.
    assert(op1->IsUnsigned() == IsUnsigned());
}
#endif // !defined(TARGET_64BIT) && defined(DEBUG)
#endif // !defined(TARGET_64BIT) || defined(TARGET_ARM64)

unsigned Compiler::gtSetCallArgsOrder(CallArgs* args, bool lateArgs, int* callCostEx, int* callCostSz)
{
    unsigned level  = 0;
    unsigned costEx = 0;
    unsigned costSz = 0;

    auto update = [&level, &costEx, &costSz, lateArgs](GenTree* argNode, unsigned argLevel) {
        if (argLevel > level)
        {
            level = argLevel;
        }

        if (argNode->GetCostEx() != 0)
        {
            costEx += argNode->GetCostEx();
            costEx += lateArgs ? 0 : IND_COST_EX;
        }
        if (argNode->GetCostSz() != 0)
        {
            costSz += argNode->GetCostSz();
#ifdef TARGET_XARCH
            if (lateArgs) // push is smaller than mov to reg
#endif
            {
                costSz += 1;
            }
        }
    };

    if (lateArgs)
    {
        for (CallArg& arg : args->LateArgs())
        {
            GenTree* node  = arg.GetLateNode();
            unsigned level = gtSetEvalOrder(node);
            update(node, level);
        }
    }
    else
    {
        for (CallArg& arg : args->EarlyArgs())
        {
            GenTree* node  = arg.GetEarlyNode();
            unsigned level = gtSetEvalOrder(node);
            update(node, level);
        }
    }

    *callCostEx += costEx;
    *callCostSz += costSz;

    return level;
}

#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
//------------------------------------------------------------------------
// gtSetMultiOpOrder: Calculate the costs for a MultiOp.
//
// Arguments:
//    multiOp - The MultiOp tree in question
//
// Return Value:
//    The Sethi "complexity" for this tree (the idealized number of
//    registers needed to evaluate it).
//
unsigned Compiler::gtSetMultiOpOrder(GenTreeMultiOp* multiOp)
{
    // Most HWI nodes are simple arithmetic operations.
    //
    int      costEx = 1;
    int      costSz = 1;
    unsigned level  = 0;

#if defined(FEATURE_HW_INTRINSICS)
    if (multiOp->OperIs(GT_HWINTRINSIC))
    {
        GenTreeHWIntrinsic* hwTree = multiOp->AsHWIntrinsic();
#if defined(TARGET_XARCH)
        if ((hwTree->GetOperandCount() == 1) && hwTree->OperIsMemoryLoadOrStore())
        {
            costEx = IND_COST_EX;
            costSz = 2;

            GenTree* const addrNode = hwTree->Op(1);
            level                   = gtSetEvalOrder(addrNode);
            GenTree* const addr     = addrNode->gtEffectiveVal();

            // See if we can form a complex addressing mode.
            if (addr->OperIs(GT_ADD) && gtMarkAddrMode(addr, &costEx, &costSz, hwTree->TypeGet()))
            {
                // Nothing to do, costs have been set.
            }
            else
            {
                costEx += addr->GetCostEx();
                costSz += addr->GetCostSz();
            }

            hwTree->SetCosts(costEx, costSz);
            return level;
        }
#endif
        switch (hwTree->GetHWIntrinsicId())
        {
            case NI_Vector128_Create:
            case NI_Vector128_CreateScalar:
            case NI_Vector128_CreateScalarUnsafe:
#if defined(TARGET_XARCH)
            case NI_Vector256_Create:
            case NI_Vector256_CreateScalar:
            case NI_Vector256_CreateScalarUnsafe:
            case NI_Vector512_Create:
            case NI_Vector512_CreateScalar:
            case NI_Vector512_CreateScalarUnsafe:
#elif defined(TARGET_ARM64)
            case NI_Vector64_Create:
            case NI_Vector64_CreateScalar:
            case NI_Vector64_CreateScalarUnsafe:
#endif
            {
                if ((hwTree->GetOperandCount() == 1) && hwTree->Op(1)->OperIsConst())
                {
                    // Vector.Create(cns) is cheap but not that cheap to be (1,1)
                    costEx = IND_COST_EX;
                    costSz = 2;
                    level  = gtSetEvalOrder(hwTree->Op(1));
                    hwTree->SetCosts(costEx, costSz);
                    return level;
                }
                break;
            }
            default:
                break;
        }
    }
#endif // defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)

    // The binary case is special because of GTF_REVERSE_OPS.
    if (multiOp->GetOperandCount() == 2)
    {
        unsigned lvl2 = 0;

        // This way we have "level" be the complexity of the
        // first tree to be evaluated, and "lvl2" - the second.
        if (multiOp->IsReverseOp())
        {
            level = gtSetEvalOrder(multiOp->Op(2));
            lvl2  = gtSetEvalOrder(multiOp->Op(1));
        }
        else
        {
            level = gtSetEvalOrder(multiOp->Op(1));
            lvl2  = gtSetEvalOrder(multiOp->Op(2));
        }

        // We want the more complex tree to be evaluated first.
        if (level < lvl2)
        {
            bool canSwap = multiOp->IsReverseOp() ? gtCanSwapOrder(multiOp->Op(2), multiOp->Op(1))
                                                  : gtCanSwapOrder(multiOp->Op(1), multiOp->Op(2));

            if (canSwap)
            {
                if (multiOp->IsReverseOp())
                {
                    multiOp->ClearReverseOp();
                }
                else
                {
                    multiOp->SetReverseOp();
                }

                std::swap(level, lvl2);
            }
        }

        if (level < 1)
        {
            level = lvl2;
        }
        else if (level == lvl2)
        {
            level += 1;
        }

        costEx += (multiOp->Op(1)->GetCostEx() + multiOp->Op(2)->GetCostEx());
        costSz += (multiOp->Op(1)->GetCostSz() + multiOp->Op(2)->GetCostSz());
    }
    else
    {
        for (size_t i = multiOp->GetOperandCount(); i >= 1; i--)
        {
            GenTree* op  = multiOp->Op(i);
            unsigned lvl = gtSetEvalOrder(op);

            level = max(lvl, level + 1);

            costEx += op->GetCostEx();
            costSz += op->GetCostSz();
        }
    }

    multiOp->SetCosts(costEx, costSz);
    return level;
}
#endif

//-----------------------------------------------------------------------------
// gtWalkOp: Traverse and mark an address expression
//
// Arguments:
//    op1WB - An out parameter which is either the address expression, or one
//            of its operands.
//    op2WB - An out parameter which starts as either null or one of the operands
//            of the address expression.
//    base  - The base address of the addressing mode, or null if 'constOnly' is false
//    constOnly - True if we will only traverse into ADDs with constant op2.
//
// This routine is a helper routine for gtSetEvalOrder() and is used to identify the
// base and index nodes, which will be validated against those identified by
// genCreateAddrMode().
// It also marks the ADD nodes involved in the address expression with the
// GTF_ADDRMODE_NO_CSE flag which prevents them from being considered for CSE's.
//
// Its two output parameters are modified under the following conditions:
//
// It is called once with the original address expression as 'op1WB', and
// with 'constOnly' set to false. On this first invocation, *op1WB is always
// an ADD node, and it will consider the operands of the ADD even if its op2 is
// not a constant. However, when it encounters a non-constant or the base in the
// op2 position, it stops iterating. That operand is returned in the 'op2WB' out
// parameter, and will be considered on the third invocation of this method if
// it is an ADD.
//
// It is called the second time with the two operands of the original expression, in
// the original order, and the third time in reverse order. For these invocations
// 'constOnly' is true, so it will only traverse cascaded ADD nodes if they have a
// constant op2.
//
// The result, after three invocations, is that the values of the two out parameters
// correspond to the base and index in some fashion. This method doesn't attempt
// to determine or validate the scale or offset, if any.
//
// Assumptions (presumed to be ensured by genCreateAddrMode()):
//    If an ADD has a constant operand, it is in the op2 position.
//
// Notes:
//    This method, and its invocation sequence, are quite confusing, and since they
//    were not originally well-documented, this specification is a possibly-imperfect
//    reconstruction.
//    The motivation for the handling of the NOP case is unclear.
//    Note that 'op2WB' is only modified in the initial (!constOnly) case,
//    or if a NOP is encountered in the op1 position.
//
void Compiler::gtWalkOp(GenTree** op1WB, GenTree** op2WB, GenTree* base, bool constOnly)
{
    GenTree* op1 = *op1WB;
    GenTree* op2 = *op2WB;

    op1 = op1->gtEffectiveVal();

    // Now we look for op1's with non-overflow GT_ADDs [of constants]
    while (op1->OperIs(GT_ADD) && !op1->gtOverflow())
    {
        GenTreeOp* add    = op1->AsOp();
        GenTree*   addOp1 = add->gtGetOp1();
        GenTree*   addOp2 = add->gtGetOp2();

        if (constOnly && (!addOp2->IsCnsIntOrI() || !addOp2->AsIntCon()->ImmedValCanBeFolded(this, GT_ADD)))
        {
            break;
        }

        // Ignore ADD(CNS, CNS-gc-handle)
        if (constOnly && addOp2->IsIconHandle(GTF_ICON_OBJ_HDL) && !addOp2->IsIntegralConst(0))
        {
            break;
        }

        // mark it with GTF_ADDRMODE_NO_CSE
        add->gtFlags |= GTF_ADDRMODE_NO_CSE;

        if (!constOnly)
        {
            op2 = addOp2;
        }
        op1 = addOp1;

        // If op1 is a GT_NOP then swap op1 and op2.
        // (Why? Also, presumably op2 is not a GT_NOP in this case?)
        if (op1->OperIs(GT_NOP))
        {
            std::swap(op1, op2);
        }

        if (!constOnly && ((op2 == base) || !op2->IsCnsIntOrI() || !op2->AsIntCon()->ImmedValCanBeFolded(this, GT_ADD)))
        {
            break;
        }

        op1 = op1->gtEffectiveVal();
    }

    *op1WB = op1;
    *op2WB = op2;
}

#ifdef DEBUG
/*****************************************************************************
 * This is a workaround. It is to help implement an assert in gtSetEvalOrder() that the values
 * gtWalkOp() leaves in op1 and op2 correspond with the values of adr, idx, mul, and cns
 * that are returned by genCreateAddrMode(). It's essentially impossible to determine
 * what gtWalkOp() *should* return for all possible trees. This simply loosens one assert
 * to handle the following case:

         indir     int
                    const(h)  int    4 field
                 +         byref
                    lclVar    byref  V00 this               <-- op2
              comma     byref                           <-- adr (base)
                 indir     byte
                    lclVar    byref  V00 this
           +         byref
                 const     int    2                     <-- mul == 4
              <<        int                                 <-- op1
                 lclVar    int    V01 arg1              <-- idx

 * Here, we are planning to generate the address mode [edx+4*eax], where eax = idx and edx = the GT_COMMA expression.
 * To check adr equivalence with op2, we need to walk down the GT_ADD tree just like gtWalkOp() does.
 */
GenTree* Compiler::gtWalkOpEffectiveVal(GenTree* op)
{
    while (true)
    {
        op = op->gtEffectiveVal();

        if ((op->gtOper != GT_ADD) || op->gtOverflow() || !op->AsOp()->gtOp2->IsCnsIntOrI())
        {
            break;
        }

        op = op->AsOp()->gtOp1;
    }

    return op;
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Given a tree, set the GetCostEx and GetCostSz() fields which
 *  are used to measure the relative costs of the codegen of the tree
 *
 */

void Compiler::gtPrepareCost(GenTree* tree)
{
    gtSetEvalOrder(tree);
}

bool Compiler::gtIsLikelyRegVar(GenTree* tree)
{
    if (!tree->OperIsScalarLocal())
    {
        return false;
    }

    const LclVarDsc* varDsc = lvaGetDesc(tree->AsLclVar());

    if (varDsc->lvDoNotEnregister)
    {
        return false;
    }

    // If this is an EH-live var, return false if it is a def,
    // as it will have to go to memory.
    if (varDsc->lvLiveInOutOfHndlr && ((tree->gtFlags & GTF_VAR_DEF) != 0))
    {
        return false;
    }

    // Be pessimistic if ref counts are not yet set up.
    //
    // Perhaps we should be optimistic though.
    // See notes in GitHub issue 18969.
    if (!lvaLocalVarRefCounted())
    {
        return false;
    }

    if (varDsc->lvRefCntWtd() < (BB_UNITY_WEIGHT * 3))
    {
        return false;
    }

#ifdef TARGET_X86
    if (!varTypeUsesIntReg(tree->TypeGet()))
        return false;
    if (varTypeIsLong(tree->TypeGet()))
        return false;
#endif

    return true;
}

//------------------------------------------------------------------------
// gtGetLclVarNodeCost: Calculate the cost for a local variable node.
//
// Used for both uses and defs. Only the node's own cost is calculated.
//
// Arguments:
//    node           - The node in question
//    pCostEx        - [out] parameter for the execution cost
//    pCostSz        - [out] parameter for the size cost
//    isLikelyRegVar - Is the local likely to end up enregistered
//
void Compiler::gtGetLclVarNodeCost(GenTreeLclVar* node, int* pCostEx, int* pCostSz, bool isLikelyRegVar)
{
    int costEx;
    int costSz;
    if (isLikelyRegVar)
    {
        costEx = 1;
        costSz = 1;
        /* Sign-extend and zero-extend are more expensive to load */
        if (lvaGetDesc(node)->lvNormalizeOnLoad())
        {
            costEx += 1;
            costSz += 1;
        }
    }
    else
    {
        costEx = IND_COST_EX;
        costSz = 2;

        // Some types are more expensive to load than others.
        if (varTypeIsSmall(node))
        {
            costEx += 1;
            costSz += 1;
        }
        else if (node->TypeIs(TYP_STRUCT))
        {
            costEx += 2 * IND_COST_EX;
            costSz += 2 * 2;
        }
    }
#if defined(TARGET_AMD64)
    // increase costSz for floating point locals
    if (varTypeIsFloating(node))
    {
        costSz += 1;
        if (!isLikelyRegVar)
        {
            costSz += 1;
        }
    }
#endif

    *pCostEx = costEx;
    *pCostSz = costSz;
}

//------------------------------------------------------------------------
// gtGetLclFldNodeCost: Calculate the cost for a local field node.
//
// Used for both uses and defs. Only the node's own cost is calculated.
//
// Arguments:
//    node           - The node in question
//    pCostEx        - [out] parameter for the execution cost
//    pCostSz        - [out] parameter for the size cost
//
void Compiler::gtGetLclFldNodeCost(GenTreeLclFld* node, int* pCostEx, int* pCostSz)
{
    int costEx = IND_COST_EX;
    int costSz = 4;
    if (varTypeIsSmall(node->TypeGet()))
    {
        costEx += 1;
        costSz += 1;
    }
    else if (node->TypeIs(TYP_STRUCT))
    {
        costEx += 2 * IND_COST_EX;
        costSz += 2 * 2;
    }

    *pCostEx = costEx;
    *pCostSz = costSz;
}

//------------------------------------------------------------------------
// gtGetLclFldNodeCost: Calculate the cost for a primitive indir node.
//
// Used for both loads and stores.
//
// Arguments:
//    node           - The node in question
//    pCostEx        - [out] parameter for the execution cost
//    pCostSz        - [out] parameter for the size cost
//
// Return Value:
//    Whether the cost calculated includes that of the node's address.
//
bool Compiler::gtGetIndNodeCost(GenTreeIndir* node, int* pCostEx, int* pCostSz)
{
    assert(node->isIndir());

    // Indirections have a costEx of IND_COST_EX.
    int  costEx           = IND_COST_EX;
    int  costSz           = 2;
    bool includesAddrCost = false;

    // If we have to sign-extend or zero-extend, bump the cost.
    if (varTypeIsSmall(node))
    {
        costEx += 1;
        costSz += 1;
    }

#ifdef TARGET_ARM
    if (varTypeIsFloating(node))
    {
        costSz += 2;
    }
#endif // TARGET_ARM

    // Can we form an addressing mode with this indirection?
    GenTree* addr = node->Addr();

    if (addr->gtEffectiveVal()->OperIs(GT_ADD))
    {
        // See if we can form a complex addressing mode.
        bool doAddrMode = true;

        // TODO-1stClassStructs: delete once IND<struct> nodes are no more.
        if (node->TypeGet() == TYP_STRUCT)
        {
            doAddrMode = false;
        }
#ifdef TARGET_ARM64
        if (node->IsVolatile())
        {
            // For volatile store/loads when address is contained we always emit `dmb`
            // if it's not - we emit one-way barriers i.e. ldar/stlr
            doAddrMode = false;
        }
#endif // TARGET_ARM64
        if (doAddrMode && gtMarkAddrMode(addr, &costEx, &costSz, node->TypeGet()))
        {
            includesAddrCost = true;
        }
    }
    else if (gtIsLikelyRegVar(addr))
    {
        // Indirection of an enregister LCL_VAR, don't increase costEx/costSz.
        includesAddrCost = true;
    }
#ifdef TARGET_XARCH
    else if (addr->IsCnsIntOrI())
    {
        // Indirection of a CNS_INT, subtract 1 from costEx makes costEx 3 for x86 and 4 for amd64.
        costEx += (addr->GetCostEx() - 1);
        costSz += addr->GetCostSz();
        includesAddrCost = true;
    }
#endif

    *pCostEx = costEx;
    *pCostSz = costSz;
    return includesAddrCost;
}

//------------------------------------------------------------------------
// gtCanSwapOrder: Returns true iff the secondNode can be swapped with firstNode.
//
// Arguments:
//    firstNode  - An operand of a tree that can have GTF_REVERSE_OPS set.
//    secondNode - The other operand of the tree.
//
// Return Value:
//    Returns a boolean indicating whether it is safe to reverse the execution
//    order of the two trees, considering any exception, global effects, or
//    ordering constraints.
//
bool Compiler::gtCanSwapOrder(GenTree* firstNode, GenTree* secondNode)
{
    bool canSwap = true;

    // Don't swap "CONST_HDL op CNS"
    if (firstNode->IsIconHandle() && secondNode->IsIntegralConst())
    {
        canSwap = false;
    }

    // Relative of order of global / side effects can't be swapped.

    if (optValnumCSE_phase)
    {
        canSwap = optCSE_canSwap(firstNode, secondNode);
    }

    // We cannot swap in the presence of special side effects such as GT_CATCH_ARG.

    if (canSwap && (firstNode->gtFlags & GTF_ORDER_SIDEEFF))
    {
        canSwap = false;
    }

    // When strict side effect order is disabled we allow GTF_REVERSE_OPS to be set
    // when one or both sides contains a GTF_CALL or GTF_EXCEPT.
    // Currently only the C and C++ languages allow non strict side effect order.

    unsigned strictEffects = GTF_GLOB_EFFECT;

    if (canSwap && (firstNode->gtFlags & strictEffects))
    {
        // op1 has side efects that can't be reordered.
        // Check for some special cases where we still may be able to swap.

        if (secondNode->gtFlags & strictEffects)
        {
            // op2 has also has non reorderable side effects - can't swap.
            canSwap = false;
        }
        else
        {
            // No side effects in op2 - we can swap iff op1 has no way of modifying op2,
            // i.e. through byref assignments or calls or op2 is a constant.

            if (firstNode->gtFlags & strictEffects & GTF_PERSISTENT_SIDE_EFFECTS)
            {
                // We have to be conservative - can swap iff op2 is constant.
                if (!secondNode->IsInvariant())
                {
                    canSwap = false;
                }
            }
        }
    }
    return canSwap;
}

//------------------------------------------------------------------------
// Given an address expression, compute its costs and addressing mode opportunities,
// and mark addressing mode candidates as GTF_DONT_CSE.
//
// Arguments:
//    addr   - The address expression
//    costEx - The execution cost of this address expression (in/out arg to be updated)
//    costEx - The size cost of this address expression (in/out arg to be updated)
//    type   - The type of the value being referenced by the parent of this address expression.
//
// Return Value:
//    Returns true if it finds an addressing mode.
//
// Notes:
//    TODO-Throughput - Consider actually instantiating these early, to avoid
//    having to re-run the algorithm that looks for them (might also improve CQ).
//
bool Compiler::gtMarkAddrMode(GenTree* addr, int* pCostEx, int* pCostSz, var_types type)
{
    GenTree* addrComma = addr;
    addr               = addr->gtEffectiveVal();

    // These are "out" parameters on the call to genCreateAddrMode():
    bool rev;      // This will be true if the operands will need to be reversed. At this point we
                   // don't care about this because we're not yet instantiating this addressing mode.
    unsigned mul;  // This is the index (scale) value for the addressing mode
    ssize_t  cns;  // This is the constant offset
    GenTree* base; // This is the base of the address.
    GenTree* idx;  // This is the index.

    unsigned naturalMul = 0;
#ifdef TARGET_ARM64
    // Multiplier should be a "natural-scale" power of two number which is equal to target's width.
    //
    //   *(ulong*)(data + index * 8); - can be optimized
    //   *(ulong*)(data + index * 7); - can not be optimized
    //     *(int*)(data + index * 2); - can not be optimized
    //
    naturalMul = genTypeSize(type);
#endif

    if (codeGen->genCreateAddrMode(addr, false /*fold*/, naturalMul, &rev, &base, &idx, &mul, &cns))
    {
#ifdef TARGET_ARM64
        assert((mul == 0) || (mul == 1) || (mul == naturalMul));
#endif

        // We can form a complex addressing mode, so mark each of the interior
        // nodes with GTF_ADDRMODE_NO_CSE and calculate a more accurate cost.
        addr->gtFlags |= GTF_ADDRMODE_NO_CSE;

        int originalAddrCostEx = addr->GetCostEx();
        int originalAddrCostSz = addr->GetCostSz();
        int addrModeCostEx     = 0;
        int addrModeCostSz     = 0;

#ifdef TARGET_XARCH
        // addrmodeCount is the count of items that we used to form
        // an addressing mode.  The maximum value is 4 when we have
        // all of these:   { base, idx, cns, mul }
        //
        unsigned addrmodeCount = 0;
        if (base)
        {
            addrModeCostEx += base->GetCostEx();
            addrModeCostSz += base->GetCostSz();
            addrmodeCount++;
        }

        if (idx)
        {
            addrModeCostEx += idx->GetCostEx();
            addrModeCostSz += idx->GetCostSz();
            addrmodeCount++;
        }

        if (cns)
        {
            if (((signed char)cns) == ((int)cns))
            {
                addrModeCostSz += 1;
            }
            else
            {
                addrModeCostSz += 4;
            }
            addrmodeCount++;
        }
        if (mul)
        {
            addrmodeCount++;
        }
        // When we form a complex addressing mode we can reduced the costs
        // associated with the interior GT_ADD and GT_LSH nodes:
        //
        //                      GT_ADD      -- reduce this interior GT_ADD by (-3,-3)
        //                      /   \       --
        //                  GT_ADD  'cns'   -- reduce this interior GT_ADD by (-2,-2)
        //                  /   \           --
        //               'base'  GT_LSL     -- reduce this interior GT_LSL by (-1,-1)
        //                      /   \       --
        //                   'idx'  'mul'
        //
        if (addrmodeCount > 1)
        {
            // The number of interior GT_ADD and GT_LSL will always be one less than addrmodeCount
            //
            addrmodeCount--;

            GenTree* tmp = addr;
            while (addrmodeCount > 0)
            {
                // decrement the gtCosts for the interior GT_ADD or GT_LSH node by the remaining
                // addrmodeCount
                tmp->SetCosts(tmp->GetCostEx() - addrmodeCount, tmp->GetCostSz() - addrmodeCount);

                addrmodeCount--;
                if (addrmodeCount > 0)
                {
                    GenTree* tmpOp1 = tmp->AsOp()->gtOp1;
                    GenTree* tmpOp2 = tmp->gtGetOp2();
                    assert(tmpOp2 != nullptr);

                    if ((tmpOp1 != base) && (tmpOp1->OperGet() == GT_ADD))
                    {
                        tmp = tmpOp1;
                    }
                    else if (tmpOp2->OperGet() == GT_LSH)
                    {
                        tmp = tmpOp2;
                    }
                    else if (tmpOp1->OperGet() == GT_LSH)
                    {
                        tmp = tmpOp1;
                    }
                    else if (tmpOp2->OperGet() == GT_ADD)
                    {
                        tmp = tmpOp2;
                    }
                    else
                    {
                        // We can very rarely encounter a tree that has a GT_COMMA node
                        // that is difficult to walk, so we just early out without decrementing.
                        addrmodeCount = 0;
                    }
                }
            }
        }
#elif defined TARGET_ARM
        if (base)
        {
            addrModeCostEx += base->GetCostEx();
            addrModeCostSz += base->GetCostSz();
            if ((base->gtOper == GT_LCL_VAR) && ((idx == NULL) || (cns == 0)))
            {
                addrModeCostSz -= 1;
            }
        }

        if (idx)
        {
            addrModeCostEx += idx->GetCostEx();
            addrModeCostSz += idx->GetCostSz();
            if (mul > 0)
            {
                addrModeCostSz += 2;
            }
        }

        if (cns)
        {
            if (cns >= 128) // small offsets fits into a 16-bit instruction
            {
                if (cns < 4096) // medium offsets require a 32-bit instruction
                {
                    if (!varTypeIsFloating(type))
                    {
                        addrModeCostSz += 2;
                    }
                }
                else
                {
                    addrModeCostEx += 2; // Very large offsets require movw/movt instructions
                    addrModeCostSz += 8;
                }
            }
        }
#elif defined TARGET_ARM64
        if (base)
        {
            addrModeCostEx += base->GetCostEx();
            addrModeCostSz += base->GetCostSz();
        }

        if (idx)
        {
            addrModeCostEx += idx->GetCostEx();
            addrModeCostSz += idx->GetCostSz();
        }

        if (cns != 0)
        {
            if (cns >= (4096 * genTypeSize(type)))
            {
                addrModeCostEx += 1;
                addrModeCostSz += 4;
            }
        }
#elif defined(TARGET_LOONGARCH64)
        if (base)
        {
            addrModeCostEx += base->GetCostEx();
            addrModeCostSz += base->GetCostSz();
        }

        if (idx)
        {
            addrModeCostEx += idx->GetCostEx();
            addrModeCostSz += idx->GetCostSz();
        }
        if (cns != 0)
        {
            if (!emitter::isValidSimm12(cns))
            {
                // TODO-LoongArch64-CQ: tune for LoongArch64.
                addrModeCostEx += 1;
                addrModeCostSz += 4;
            }
        }
#elif defined(TARGET_RISCV64)
        if (base)
        {
            addrModeCostEx += base->GetCostEx();
            addrModeCostSz += base->GetCostSz();
        }

        if (idx)
        {
            addrModeCostEx += idx->GetCostEx();
            addrModeCostSz += idx->GetCostSz();
        }
        if (cns != 0)
        {
            if (!emitter::isValidSimm12(cns))
            {
                // TODO-RISCV64-CQ: tune for RISCV64.
                addrModeCostEx += 1;
                addrModeCostSz += 4;
            }
        }
#else
#error "Unknown TARGET"
#endif

        assert(addr->gtOper == GT_ADD);
        assert(!addr->gtOverflow());
        assert(mul != 1);

        // If we have an addressing mode, we have one of:
        //   [base             + cns]
        //   [       idx * mul      ]  // mul >= 2, else we would use base instead of idx
        //   [       idx * mul + cns]  // mul >= 2, else we would use base instead of idx
        //   [base + idx * mul      ]  // mul can be 0, 2, 4, or 8
        //   [base + idx * mul + cns]  // mul can be 0, 2, 4, or 8
        // Note that mul == 0 is semantically equivalent to mul == 1.
        // Note that cns can be zero.
        CLANG_FORMAT_COMMENT_ANCHOR;

        assert((base != nullptr) || (idx != nullptr && mul >= 2));

        INDEBUG(GenTree* op1Save = addr);

        // Walk 'addr' identifying non-overflow ADDs that will be part of the address mode.
        // Note that we will be modifying 'op1' and 'op2' so that eventually they should
        // map to the base and index.
        GenTree* op1 = addr;
        GenTree* op2 = nullptr;
        gtWalkOp(&op1, &op2, base, false);

        // op1 and op2 are now descendents of the root GT_ADD of the addressing mode.
        assert(op1 != op1Save);
        assert(op2 != nullptr);

#if defined(TARGET_XARCH)
        // Walk the operands again (the third operand is unused in this case).
        // This time we will only consider adds with constant op2's, since
        // we have already found either a non-ADD op1 or a non-constant op2.
        // NOTE: we don't support ADD(op1, cns) addressing for ARM/ARM64 yet so
        // this walk makes no sense there.
        gtWalkOp(&op1, &op2, nullptr, true);

        // For XARCH we will fold GT_ADDs in the op2 position into the addressing mode, so we call
        // gtWalkOp on both operands of the original GT_ADD.
        // This is not done for ARMARCH. Though the stated reason is that we don't try to create a
        // scaled index, in fact we actually do create them (even base + index*scale + offset).

        // At this point, 'op2' may itself be an ADD of a constant that should be folded
        // into the addressing mode.
        // Walk op2 looking for non-overflow GT_ADDs of constants.
        gtWalkOp(&op2, &op1, nullptr, true);
#endif // defined(TARGET_XARCH)

        // OK we are done walking the tree
        // Now assert that op1 and op2 correspond with base and idx
        // in one of the several acceptable ways.

        // Note that sometimes op1/op2 is equal to idx/base
        // and other times op1/op2 is a GT_COMMA node with
        // an effective value that is idx/base

        if (mul > 1)
        {
            if ((op1 != base) && (op1->gtOper == GT_LSH))
            {
                op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                if (op1->AsOp()->gtOp1->gtOper == GT_MUL)
                {
                    op1->AsOp()->gtOp1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                }
                assert((base == nullptr) || (op2 == base) || (op2->gtEffectiveVal() == base->gtEffectiveVal()) ||
                       (gtWalkOpEffectiveVal(op2) == gtWalkOpEffectiveVal(base)));
            }
            else
            {
                assert(op2 != nullptr);
                assert(op2->OperIs(GT_LSH, GT_MUL));
                op2->gtFlags |= GTF_ADDRMODE_NO_CSE;
                // We may have eliminated multiple shifts and multiplies in the addressing mode,
                // so navigate down through them to get to "idx".
                GenTree* op2op1 = op2->AsOp()->gtOp1;
                while ((op2op1->gtOper == GT_LSH || op2op1->gtOper == GT_MUL) && op2op1 != idx)
                {
                    op2op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                    op2op1 = op2op1->AsOp()->gtOp1;
                }

                // if genCreateAddrMode reported base as nullptr it means that op1 is effectively null address
                assert((op1->gtEffectiveVal() == base) ||
                       (base == nullptr && op1->gtEffectiveVal()->IsIntegralConst(0)));
                assert(op2op1 == idx);
            }
        }
        else
        {
            assert(mul == 0);

            if ((op1 == idx) || (op1->gtEffectiveVal() == idx))
            {
                if (idx != nullptr)
                {
                    if ((op1->gtOper == GT_MUL) || (op1->gtOper == GT_LSH))
                    {
                        GenTree* op1op1 = op1->AsOp()->gtOp1;
                        if ((op1op1->gtOper == GT_NOP) ||
                            (op1op1->gtOper == GT_MUL && op1op1->AsOp()->gtOp1->gtOper == GT_NOP))
                        {
                            op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                            if (op1op1->gtOper == GT_MUL)
                            {
                                op1op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                            }
                        }
                    }
                }
                assert((op2 == base) || (op2->gtEffectiveVal() == base));
            }
            else if ((op1 == base) || (op1->gtEffectiveVal() == base))
            {
                if (idx != nullptr)
                {
                    assert(op2 != nullptr);
                    if (op2->OperIs(GT_MUL, GT_LSH))
                    {
                        GenTree* op2op1 = op2->AsOp()->gtOp1;
                        if ((op2op1->gtOper == GT_NOP) ||
                            (op2op1->gtOper == GT_MUL && op2op1->AsOp()->gtOp1->gtOper == GT_NOP))
                        {
                            op2->gtFlags |= GTF_ADDRMODE_NO_CSE;
                            if (op2op1->gtOper == GT_MUL)
                            {
                                op2op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                            }
                        }
                    }
                    assert((op2 == idx) || (op2->gtEffectiveVal() == idx));
                }
            }
            else
            {
                // op1 isn't base or idx. Is this possible? Or should there be an assert?
            }
        }

        // Finally, adjust the costs on the parenting COMMAs.
        while (addrComma != addr)
        {
            int addrCostExDelta = originalAddrCostEx - addrModeCostEx;
            int addrCostSzDelta = originalAddrCostSz - addrModeCostSz;
            addrComma->SetCosts(addrComma->GetCostEx() - addrCostExDelta, addrComma->GetCostSz() - addrCostSzDelta);

            *pCostEx += addrComma->AsOp()->gtGetOp1()->GetCostEx();
            *pCostSz += addrComma->AsOp()->gtGetOp1()->GetCostSz();

            addrComma = addrComma->AsOp()->gtGetOp2();
        }

        *pCostEx += addrModeCostEx;
        *pCostSz += addrModeCostSz;

        return true;

    } // if (genCreateAddrMode(...))

    return false;
}

/*****************************************************************************
 *
 *  Given a tree, figure out the order in which its sub-operands should be
 *  evaluated. If the second operand of a binary operator is more expensive
 *  than the first operand, then try to swap the operand trees. Updates the
 *  GTF_REVERSE_OPS bit if necessary in this case.
 *
 *  Returns the Sethi 'complexity' estimate for this tree (the higher
 *  the number, the higher is the tree's resources requirement).
 *
 *  This function sets:
 *      1. GetCostEx() to the execution complexity estimate
 *      2. GetCostSz() to the code size estimate
 *      3. Sometimes sets GTF_ADDRMODE_NO_CSE on nodes in the tree.
 *      4. DEBUG-only: clears GTF_DEBUG_NODE_MORPHED.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
unsigned Compiler::gtSetEvalOrder(GenTree* tree)
{
    assert(tree);

#ifdef DEBUG
    /* Clear the GTF_DEBUG_NODE_MORPHED flag as well */
    tree->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif

    /* Is this a FP value? */

    bool isflt = varTypeIsFloating(tree->TypeGet());

    /* Figure out what kind of a node we have */

    const genTreeOps oper = tree->OperGet();
    const unsigned   kind = tree->OperKind();

    /* Assume no fixed registers will be trashed */

    unsigned level;
    int      costEx;
    int      costSz;

#ifdef DEBUG
    costEx = -1;
    costSz = -1;
#endif

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
#ifdef TARGET_ARM
            case GT_CNS_STR:
                // Uses movw/movt
                costSz = 8;
                costEx = 2;
                goto COMMON_CNS;

            case GT_CNS_LNG:
            {
                GenTreeIntConCommon* con = tree->AsIntConCommon();

                INT64 lngVal = con->LngValue();
                INT32 loVal  = (INT32)(lngVal & 0xffffffff);
                INT32 hiVal  = (INT32)(lngVal >> 32);

                if (lngVal == 0)
                {
                    costSz = 1;
                    costEx = 1;
                }
                else
                {
                    // Minimum of one instruction to setup hiVal,
                    // and one instruction to setup loVal
                    costSz = 4 + 4;
                    costEx = 1 + 1;

                    if (!codeGen->validImmForInstr(INS_mov, (target_ssize_t)hiVal) &&
                        !codeGen->validImmForInstr(INS_mvn, (target_ssize_t)hiVal))
                    {
                        // Needs extra instruction: movw/movt
                        costSz += 4;
                        costEx += 1;
                    }

                    if (!codeGen->validImmForInstr(INS_mov, (target_ssize_t)loVal) &&
                        !codeGen->validImmForInstr(INS_mvn, (target_ssize_t)loVal))
                    {
                        // Needs extra instruction: movw/movt
                        costSz += 4;
                        costEx += 1;
                    }
                }
                goto COMMON_CNS;
            }

            case GT_CNS_INT:
            {
                // If the constant is a handle then it will need to have a relocation
                //  applied to it.
                // Any constant that requires a reloc must use the movw/movt sequence
                //
                GenTreeIntConCommon* con    = tree->AsIntConCommon();
                target_ssize_t       conVal = (target_ssize_t)con->IconValue();

                if (con->ImmedValNeedsReloc(this))
                {
                    // Requires movw/movt
                    costSz = 8;
                    costEx = 2;
                }
                else if (codeGen->validImmForInstr(INS_add, conVal))
                {
                    // Typically included with parent oper
                    costSz = 2;
                    costEx = 1;
                }
                else if (codeGen->validImmForInstr(INS_mov, conVal) || codeGen->validImmForInstr(INS_mvn, conVal))
                {
                    // Uses mov or mvn
                    costSz = 4;
                    costEx = 1;
                }
                else
                {
                    // Needs movw/movt
                    costSz = 8;
                    costEx = 2;
                }
                goto COMMON_CNS;
            }

#elif defined TARGET_XARCH

            case GT_CNS_STR:
#ifdef TARGET_AMD64
                costSz = 10;
                costEx = 2;
#else // TARGET_X86
                costSz = 4;
                costEx = 1;
#endif
                goto COMMON_CNS;

            case GT_CNS_LNG:
            case GT_CNS_INT:
            {
                GenTreeIntConCommon* con       = tree->AsIntConCommon();
                ssize_t              conVal    = (oper == GT_CNS_LNG) ? (ssize_t)con->LngValue() : con->IconValue();
                bool                 fitsInVal = true;

#ifdef TARGET_X86
                if (oper == GT_CNS_LNG)
                {
                    INT64 lngVal = con->LngValue();

                    conVal = (ssize_t)lngVal; // truncate to 32-bits

                    fitsInVal = ((INT64)conVal == lngVal);
                }
#endif // TARGET_X86

                // If the constant is a handle then it will need to have a relocation
                //  applied to it.
                //
                bool iconNeedsReloc = con->ImmedValNeedsReloc(this);

                if (iconNeedsReloc)
                {
                    costSz = 4;
                    costEx = 1;
                }
                else if (fitsInVal && GenTreeIntConCommon::FitsInI8(conVal))
                {
                    costSz = 1;
                    costEx = 1;
                }
#ifdef TARGET_AMD64
                else if (!GenTreeIntConCommon::FitsInI32(conVal))
                {
                    costSz = 10;
                    costEx = 2;
                    if (con->IsIconHandle())
                    {
                        // A sort of a hint for CSE to try harder for class handles
                        costEx += 1;
                    }
                }
#endif // TARGET_AMD64
                else
                {
                    costSz = 4;
                    costEx = 1;
                }
#ifdef TARGET_X86
                if (oper == GT_CNS_LNG)
                {
                    costSz += fitsInVal ? 1 : 4;
                    costEx += 1;
                }
#endif // TARGET_X86

                goto COMMON_CNS;
            }

#elif defined(TARGET_ARM64)

            case GT_CNS_STR:
            case GT_CNS_LNG:
            case GT_CNS_INT:
            {
                GenTreeIntConCommon* con            = tree->AsIntConCommon();
                bool                 iconNeedsReloc = con->ImmedValNeedsReloc(this);
                INT64                imm            = con->LngValue();
                emitAttr             size           = EA_SIZE(emitActualTypeSize(tree));

                if (iconNeedsReloc)
                {
                    costSz = 8;
                    costEx = 2;
                }
                else if (emitter::emitIns_valid_imm_for_add(imm, size))
                {
                    costSz = 2;
                    costEx = 1;
                }
                else if (emitter::emitIns_valid_imm_for_mov(imm, size))
                {
                    costSz = 4;
                    costEx = 1;
                }
                else
                {
                    // Arm64 allows any arbitrary 16-bit constant to be loaded into a register halfword
                    // There are three forms
                    //    movk which loads into any halfword preserving the remaining halfwords
                    //    movz which loads into any halfword zeroing the remaining halfwords
                    //    movn which loads into any halfword zeroing the remaining halfwords then bitwise inverting
                    //    the register
                    // In some cases it is preferable to use movn, because it has the side effect of filling the
                    // other halfwords
                    // with ones

                    // Determine whether movn or movz will require the fewest instructions to populate the immediate
                    bool preferMovz       = false;
                    bool preferMovn       = false;
                    int  instructionCount = 4;

                    for (int i = (size == EA_8BYTE) ? 48 : 16; i >= 0; i -= 16)
                    {
                        if (!preferMovn && (uint16_t(imm >> i) == 0x0000))
                        {
                            preferMovz = true; // by using a movk to start we can save one instruction
                            instructionCount--;
                        }
                        else if (!preferMovz && (uint16_t(imm >> i) == 0xffff))
                        {
                            preferMovn = true; // by using a movn to start we can save one instruction
                            instructionCount--;
                        }
                    }

                    costEx = instructionCount;
                    costSz = 4 * instructionCount;
                }
            }
                goto COMMON_CNS;

#elif defined(TARGET_LOONGARCH64)
            // TODO-LoongArch64-CQ: tune the costs.
            case GT_CNS_STR:
                costEx = IND_COST_EX + 2;
                costSz = 4;
                goto COMMON_CNS;

            case GT_CNS_LNG:
            case GT_CNS_INT:
                costEx = 1;
                costSz = 4;
                goto COMMON_CNS;
#elif defined(TARGET_RISCV64)
            // TODO-RISCV64-CQ: tune the costs.
            case GT_CNS_STR:
                costEx = IND_COST_EX + 2;
                costSz = 4;
                goto COMMON_CNS;

            case GT_CNS_LNG:
            case GT_CNS_INT:
                costEx = 1;
                costSz = 4;
                goto COMMON_CNS;
#else
            case GT_CNS_STR:
            case GT_CNS_LNG:
            case GT_CNS_INT:
#error "Unknown TARGET"
#endif
            COMMON_CNS:
                /*
                    Note that some code below depends on constants always getting
                    moved to be the second operand of a binary operator. This is
                    easily accomplished by giving constants a level of 0, which
                    we do on the next line. If you ever decide to change this, be
                    aware that unless you make other arrangements for integer
                    constants to be moved, stuff will break.
                 */

                level = 0;
                break;

            case GT_CNS_DBL:
            {
                level = 0;
#if defined(TARGET_XARCH)
                if (tree->IsFloatPositiveZero() || tree->IsFloatAllBitsSet())
                {
                    // We generate `xorp* tgtReg, tgtReg` for PositiveZero and
                    // `pcmpeqd tgtReg, tgtReg` for AllBitsSet which is 3-5 bytes
                    // but which can be elided by the instruction decoder.

                    costEx = 1;
                    costSz = 2;
                }
                else
                {
                    // We generate `movs* tgtReg, [mem]` which is 4-6 bytes
                    // and which has the same cost as an indirection.

                    costEx = IND_COST_EX;
                    costSz = 2;
                }
#elif defined(TARGET_ARM)
                var_types targetType = tree->TypeGet();
                if (targetType == TYP_FLOAT)
                {
                    costEx = 1 + 2;
                    costSz = 2 + 4;
                }
                else
                {
                    assert(targetType == TYP_DOUBLE);
                    costEx = 1 + 4;
                    costSz = 2 + 8;
                }
#elif defined(TARGET_ARM64)
                if (tree->IsFloatPositiveZero() || emitter::emitIns_valid_imm_for_fmov(tree->AsDblCon()->DconValue()))
                {
                    // Zero and certain other immediates can be specially created with a single instruction
                    // These can be cheaply reconstituted but still take up 4-bytes of native codegen

                    costEx = 1;
                    costSz = 2;
                }
                else
                {
                    // We load the constant from memory and so will take the same cost as GT_IND

                    costEx = IND_COST_EX;
                    costSz = 2;
                }
#elif defined(TARGET_LOONGARCH64)
                // TODO-LoongArch64-CQ: tune the costs.
                costEx = 2;
                costSz = 8;
#elif defined(TARGET_RISCV64)
                // TODO-RISCV64-CQ: tune the costs.
                costEx = 2;
                costSz = 8;
#else
#error "Unknown TARGET"
#endif
            }
            break;

            case GT_CNS_VEC:
            {
                level = 0;

                if (tree->AsVecCon()->IsAllBitsSet() || tree->AsVecCon()->IsZero())
                {
                    // We generate `cmpeq* tgtReg, tgtReg`, which is 4-5 bytes, for AllBitsSet
                    // and generate `xorp* tgtReg, tgtReg`, which is 3-5 bytes, for Zero
                    // both of which can be elided by the instruction decoder.

                    costEx = 1;
                    costSz = 2;
                }
                else
                {
                    // We generate `movup* tgtReg, [mem]` which is 4-6 bytes
                    // and which has the same cost as an indirection.

                    costEx = IND_COST_EX;
                    costSz = 2;
                }
                break;
            }

            case GT_LCL_VAR:
                level = 1;
                gtGetLclVarNodeCost(tree->AsLclVar(), &costEx, &costSz, gtIsLikelyRegVar(tree));
                break;

            case GT_LCL_FLD:
                level = 1;
                gtGetLclFldNodeCost(tree->AsLclFld(), &costEx, &costSz);
                break;

            case GT_LCL_ADDR:
                level  = 1;
                costEx = 3;
                costSz = 3;
                break;

            case GT_PHI_ARG:
                level  = 0;
                costEx = 0;
                costSz = 0;
                break;

            case GT_NOP:
                level  = 0;
                costEx = 0;
                costSz = 0;
                break;

            default:
                level  = 1;
                costEx = 1;
                costSz = 1;
                break;
        }
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->gtGetOp2IfPresent();

        costEx = 0;
        costSz = 0;

        if (tree->OperIsAddrMode())
        {
            if (op1 == nullptr)
            {
                op1 = op2;
                op2 = nullptr;
            }
        }

        /* Check for a nilary operator */

        if (op1 == nullptr)
        {
            assert(op2 == nullptr);

            level = 0;

            goto DONE;
        }

        /* Is this a unary operator? */

        if (op2 == nullptr)
        {
            /* Process the operand of the operator */

            /* Most Unary ops have costEx of 1 */
            costEx = 1;
            costSz = 1;

            level = gtSetEvalOrder(op1);

            GenTreeIntrinsic* intrinsic;

            /* Special handling for some operators */

            switch (oper)
            {
                case GT_JTRUE:
                    costEx = 2;
                    costSz = 2;
                    break;

                case GT_SWITCH:
                    costEx = 10;
                    costSz = 5;
                    break;

                case GT_CAST:
#if defined(TARGET_ARM)
                    costEx = 1;
                    costSz = 1;
                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        costEx = 3;
                        costSz = 4;
                    }
#elif defined(TARGET_ARM64)
                    costEx = 1;
                    costSz = 2;
                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        costEx = 2;
                        costSz = 4;
                    }
#elif defined(TARGET_XARCH)
                    costEx = 1;
                    costSz = 2;

                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        /* cast involving floats always go through memory */
                        costEx = IND_COST_EX * 2;
                        costSz = 6;
                    }
#elif defined(TARGET_LOONGARCH64)
                    // TODO-LoongArch64-CQ: tune the costs.
                    costEx = 1;
                    costSz = 4;
#elif defined(TARGET_RISCV64)
                    // TODO-RISCV64-CQ: tune the costs.
                    costEx = 1;
                    costSz = 4;
#else
#error "Unknown TARGET"
#endif

                    /* Overflow casts are a lot more expensive */
                    if (tree->gtOverflow())
                    {
                        costEx += 6;
                        costSz += 6;
                    }

                    break;

                case GT_INTRINSIC:
                    intrinsic = tree->AsIntrinsic();
                    // named intrinsic
                    assert(intrinsic->gtIntrinsicName != NI_Illegal);

                    // GT_INTRINSIC intrinsics Sin, Cos, Sqrt, Abs ... have higher costs.
                    // TODO: tune these costs target specific as some of these are
                    // target intrinsics and would cost less to generate code.
                    switch (intrinsic->gtIntrinsicName)
                    {
                        default:
                            assert(!"missing case for gtIntrinsicName");
                            costEx = 12;
                            costSz = 12;
                            break;

                        case NI_System_Math_Abs:
                            costEx = 5;
                            costSz = 15;
                            break;

                        case NI_System_Math_Acos:
                        case NI_System_Math_Acosh:
                        case NI_System_Math_Asin:
                        case NI_System_Math_Asinh:
                        case NI_System_Math_Atan:
                        case NI_System_Math_Atanh:
                        case NI_System_Math_Atan2:
                        case NI_System_Math_Cbrt:
                        case NI_System_Math_Ceiling:
                        case NI_System_Math_Cos:
                        case NI_System_Math_Cosh:
                        case NI_System_Math_Exp:
                        case NI_System_Math_Floor:
                        case NI_System_Math_FMod:
                        case NI_System_Math_FusedMultiplyAdd:
                        case NI_System_Math_ILogB:
                        case NI_System_Math_Log:
                        case NI_System_Math_Log2:
                        case NI_System_Math_Log10:
                        case NI_System_Math_Max:
                        case NI_System_Math_MaxMagnitude:
                        case NI_System_Math_MaxMagnitudeNumber:
                        case NI_System_Math_MaxNumber:
                        case NI_System_Math_Min:
                        case NI_System_Math_MinMagnitude:
                        case NI_System_Math_MinMagnitudeNumber:
                        case NI_System_Math_MinNumber:
                        case NI_System_Math_Pow:
                        case NI_System_Math_Round:
                        case NI_System_Math_Sin:
                        case NI_System_Math_Sinh:
                        case NI_System_Math_Sqrt:
                        case NI_System_Math_Tan:
                        case NI_System_Math_Tanh:
                        case NI_System_Math_Truncate:
                        {
                            // Giving intrinsics a large fixed execution cost is because we'd like to CSE
                            // them, even if they are implemented by calls. This is different from modeling
                            // user calls since we never CSE user calls. We don't do this for target intrinsics
                            // however as they typically represent single instruction calls

                            if (IsIntrinsicImplementedByUserCall(intrinsic->gtIntrinsicName))
                            {
                                costEx = 36;
                                costSz = 4;
                            }
                            else
                            {
                                costEx = 3;
                                costSz = 4;
                            }
                            break;
                        }

                        case NI_System_Object_GetType:
                            // Giving intrinsics a large fixed execution cost is because we'd like to CSE
                            // them, even if they are implemented by calls. This is different from modeling
                            // user calls since we never CSE user calls.
                            costEx = 36;
                            costSz = 4;
                            break;

#if defined(FEATURE_SIMD)
                        case NI_SIMD_UpperRestore:
                        case NI_SIMD_UpperSave:
                        {
                            // TODO-CQ: 1 Ex/Sz isn't necessarily "accurate" but it is what the previous
                            // cost was computed as, in gtSetMultiOpOrder, when this was handled by the
                            // older SIMD intrinsic support.

                            costEx = 1;
                            costSz = 1;
                            break;
                        }
#endif // FEATURE_SIMD
                    }
                    level++;
                    break;

                case GT_NOT:
                case GT_NEG:
                    // We need to ensure that -x is evaluated before x or else
                    // we get burned while adjusting genFPstkLevel in x*-x where
                    // the rhs x is the last use of the enregistered x.
                    //
                    // Even in the integer case we want to prefer to
                    // evaluate the side without the GT_NEG node, all other things
                    // being equal.  Also a GT_NOT requires a scratch register

                    level++;
                    break;

                case GT_ARR_LENGTH:
                case GT_MDARR_LENGTH:
                case GT_MDARR_LOWER_BOUND:
                    level++;

                    // Array meta-data access should be the same as an indirection, which has a costEx of IND_COST_EX.
                    costEx = IND_COST_EX - 1;
                    costSz = 2;
                    break;

                case GT_MKREFANY:
                case GT_BLK:
                    // We estimate the cost of a GT_BLK or GT_MKREFANY to be two loads (GT_INDs)
                    costEx = 2 * IND_COST_EX;
                    costSz = 2 * 2;
                    break;

                case GT_BOX:
                    // We estimate the cost of a GT_BOX to be two stores (GT_INDs)
                    costEx = 2 * IND_COST_EX;
                    costSz = 2 * 2;
                    break;

                case GT_ARR_ADDR:
                    costEx = 0;
                    costSz = 0;

                    // To preserve previous behavior, we will always use "gtMarkAddrMode" for ARR_ADDR.
                    if (op1->OperIs(GT_ADD) && gtMarkAddrMode(op1, &costEx, &costSz, tree->AsArrAddr()->GetElemType()))
                    {
                        op1->SetCosts(costEx, costSz);
                        goto DONE;
                    }
                    break;

                case GT_IND:
                    // An indirection should always have a non-zero level.
                    // Only constant leaf nodes have level 0.
                    if (level == 0)
                    {
                        level = 1;
                    }

                    if (gtGetIndNodeCost(tree->AsIndir(), &costEx, &costSz))
                    {
                        goto DONE;
                    }
                    break;

                case GT_STORE_LCL_VAR:
                    if (gtIsLikelyRegVar(tree))
                    {
                        // Store to an enregistered local.
                        costEx = op1->GetCostEx();
                        costSz = max(3, op1->GetCostSz()); // 3 is an estimate for a reg-reg move.
                        goto DONE;
                    }

                    gtGetLclVarNodeCost(tree->AsLclVar(), &costEx, &costSz, /* isLikelyRegVar */ false);
                    goto SET_LCL_STORE_COSTS;

                case GT_STORE_LCL_FLD:
                    gtGetLclFldNodeCost(tree->AsLclFld(), &costEx, &costSz);

                SET_LCL_STORE_COSTS:
                    costEx += 1;
                    costSz += 1;
#ifdef TARGET_ARM
                    if (isflt)
                    {
                        costSz += 2;
                    }
#endif // TARGET_ARM
#ifndef TARGET_64BIT
                    if (tree->TypeIs(TYP_LONG))
                    {
                        // Operations on longs are more expensive.
                        costEx += 3;
                        costSz += 3;
                    }
#endif // !TARGET_64BIT
                    break;

                default:
                    break;
            }
            costEx += op1->GetCostEx();
            costSz += op1->GetCostSz();
            goto DONE;
        }

        /* Binary operator - check for certain special cases */
        bool includeOp1Cost = true;
        bool includeOp2Cost = true;
        bool allowReversal  = true;

        /* Default Binary ops have a cost of 1,1 */
        costEx = 1;
        costSz = 1;

#ifdef TARGET_ARM
        if (isflt)
        {
            costSz += 2;
        }
#endif
#ifndef TARGET_64BIT
        if (varTypeIsLong(op1->TypeGet()))
        {
            /* Operations on longs are more expensive */
            costEx += 3;
            costSz += 3;
        }
#endif
        level         = gtSetEvalOrder(op1);
        unsigned lvl2 = gtSetEvalOrder(op2);

        switch (oper)
        {
            case GT_MOD:
            case GT_UMOD:

                /* Modulo by a power of 2 is easy */

                if (op2->IsCnsIntOrI())
                {
                    size_t ival = op2->AsIntConCommon()->IconValue();

                    if (ival > 0 && ival == genFindLowestBit(ival))
                    {
                        break;
                    }
                }

                FALLTHROUGH;

            case GT_DIV:
            case GT_UDIV:

                if (isflt)
                {
                    /* fp division is very expensive to execute */
                    costEx = 36; // TYP_DOUBLE
                    costSz += 3;
                }
                else
                {
                    /* integer division is also very expensive */
                    costEx = 20;
                    costSz += 2;

                    // Encourage the first operand to be evaluated (into EAX/EDX) first */
                    level += 3;
                }
                break;

            case GT_MUL:

                if (isflt)
                {
                    /* FP multiplication instructions are more expensive */
                    costEx += 4;
                    costSz += 3;
                }
                else
                {
                    /* Integer multiplication instructions are more expensive */
                    costEx += 3;
                    costSz += 2;

                    if (tree->gtOverflow())
                    {
                        /* Overflow check are more expensive */
                        costEx += 3;
                        costSz += 3;
                    }

#ifdef TARGET_X86
                    if ((tree->gtType == TYP_LONG) || tree->gtOverflow())
                    {
                        /* We use imulEAX for TYP_LONG and overflow multiplications */
                        // Encourage the first operand to be evaluated (into EAX/EDX) first */
                        level += 4;

                        /* The 64-bit imul instruction costs more */
                        costEx += 4;
                    }
#endif //  TARGET_X86
                }
                break;

            case GT_ADD:
            case GT_SUB:
                if (isflt)
                {
                    /* FP instructions are a bit more expensive */
                    costEx += 4;
                    costSz += 3;
                    break;
                }

                /* Overflow check are more expensive */
                if (tree->gtOverflow())
                {
                    costEx += 3;
                    costSz += 3;
                }
                break;

            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            case GT_ROL:
            case GT_ROR:
                /* Variable sized shifts are more expensive and use REG_SHIFT */

                if (!op2->IsCnsIntOrI())
                {
                    costEx += 3;
#ifndef TARGET_64BIT
                    // Variable sized LONG shifts require the use of a helper call
                    //
                    if (tree->gtType == TYP_LONG)
                    {
                        level += 5;
                        lvl2 += 5;
                        costEx += 3 * IND_COST_EX;
                        costSz += 4;
                    }
#endif // !TARGET_64BIT
                }
                break;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
                /* Float compares remove both operands from the FP stack */
                /* Also FP comparison uses EAX for flags */

                if (varTypeIsFloating(op1->TypeGet()))
                {
                    level++;
                    lvl2++;
                }
                if ((tree->gtFlags & GTF_RELOP_JMP_USED) == 0)
                {
                    /* Using a setcc instruction is more expensive */
                    costEx += 3;
                }
                break;

            case GT_BOUNDS_CHECK:
                costEx = 4; // cmp reg,reg and jae throw (not taken)
                costSz = 7; // jump to cold section
                // Bounds check nodes used to not be binary, thus GTF_REVERSE_OPS was not enabled for them. This
                // condition preserves that behavior. Additionally, CQ analysis shows that enabling GTF_REVERSE_OPS
                // for these nodes leads to mixed results at best.
                allowReversal = false;
                break;

            case GT_INTRINSIC:
                // We do not swap operand execution order for intrinsics that are implemented by user calls because
                // of trickiness around ensuring the execution order does not change during rationalization.
                if (IsIntrinsicImplementedByUserCall(tree->AsIntrinsic()->gtIntrinsicName))
                {
                    allowReversal = false;
                }

                switch (tree->AsIntrinsic()->gtIntrinsicName)
                {
                    case NI_System_Math_Atan2:
                    case NI_System_Math_Pow:
                        // These math intrinsics are actually implemented by user calls. Increase the
                        // Sethi 'complexity' by two to reflect the argument register requirement.
                        level += 2;
                        break;

                    case NI_System_Math_Max:
                    case NI_System_Math_MaxMagnitude:
                    case NI_System_Math_MaxMagnitudeNumber:
                    case NI_System_Math_MaxNumber:
                    case NI_System_Math_Min:
                    case NI_System_Math_MinMagnitude:
                    case NI_System_Math_MinMagnitudeNumber:
                    case NI_System_Math_MinNumber:
                    {
                        level++;
                        break;
                    }

                    default:
                        assert(!"Unknown binary GT_INTRINSIC operator");
                        break;
                }
                break;

            case GT_COMMA:
                /* Comma tosses the result of the left operand */
                level = lvl2;
                /* GT_COMMA cost is the sum of op1 and op2 costs */
                costEx = (op1->GetCostEx() + op2->GetCostEx());
                costSz = (op1->GetCostSz() + op2->GetCostSz());
                goto DONE;

            case GT_INDEX_ADDR:
                costEx = 6; // cmp reg,reg; jae throw; mov reg, [addrmode]  (not taken)
                costSz = 9; // jump to cold section
                break;

            case GT_STORE_BLK:
                // We estimate the cost of a GT_STORE_BLK to be two stores.
                costEx += 2 * IND_COST_EX;
                costSz += 2 * 2;
                goto SET_INDIRECT_STORE_ORDER;

            case GT_STOREIND:
                if (gtGetIndNodeCost(tree->AsIndir(), &costEx, &costSz))
                {
                    includeOp1Cost = false;
                }

                costEx += 1;
                costSz += 1;
#ifndef TARGET_64BIT
                if (tree->TypeIs(TYP_LONG))
                {
                    // Operations on longs are more expensive.
                    costEx += 3;
                    costSz += 3;
                }
#endif // !TARGET_64BIT

            SET_INDIRECT_STORE_ORDER:
                // TODO-ASG-Cleanup: this logic emulated the ASG case below. See how of much of it can be deleted.
                if (!optValnumCSE_phase || optCSE_canSwap(op1, op2))
                {
                    if (op1->IsInvariant())
                    {
                        allowReversal = false;
                        tree->SetReverseOp();
                        break;
                    }
                    if ((op1->gtFlags & GTF_ALL_EFFECT) != 0)
                    {
                        break;
                    }

                    // In case op2 assigns to a local var that is used in op1, we have to evaluate op1 first.
                    if (gtMayHaveStoreInterference(op2, op1))
                    {
                        // TODO-ASG-Cleanup: move this guard to "gtCanSwapOrder".
                        allowReversal = false;
                        break;
                    }

                    // If op2 is simple then evaluate op1 first
                    if (op2->OperIsLeaf())
                    {
                        break;
                    }

                    allowReversal = false;
                    tree->SetReverseOp();
                }
                break;

            default:
                break;
        }

        if (includeOp1Cost)
        {
            costEx += op1->GetCostEx();
            costSz += op1->GetCostSz();
        }
        if (includeOp2Cost)
        {
            costEx += op2->GetCostEx();
            costSz += op2->GetCostSz();
        }

        /* We need to evaluate constants later as many places in codegen
           can't handle op1 being a constant. This is normally naturally
           enforced as constants have the least level of 0. However,
           sometimes we end up with a tree like "cns1 < nop(cns2)". In
           such cases, both sides have a level of 0. So encourage constants
           to be evaluated last in such cases */

        if ((level == 0) && (level == lvl2) && op1->OperIsConst() &&
            (tree->OperIsCommutative() || tree->OperIsCompare()))
        {
            lvl2++;
        }

        // We try to swap operands if the second one is more expensive.
        // Don't swap anything if we're in linear order; we're really just interested in the costs.
        bool tryToSwap = false;
        if (allowReversal && (fgOrder != FGOrderLinear))
        {
            if (tree->IsReverseOp())
            {
                tryToSwap = (level > lvl2);
            }
            else
            {
                tryToSwap = (level < lvl2);
            }

            // Try to force extra swapping when in the stress mode:
            if (compStressCompile(STRESS_REVERSE_FLAG, 60) && !tree->IsReverseOp() && !op2->OperIsConst())
            {
                tryToSwap = true;
            }
        }

        if (tryToSwap)
        {
            bool canSwap = tree->IsReverseOp() ? gtCanSwapOrder(op2, op1) : gtCanSwapOrder(op1, op2);

            if (canSwap)
            {
                /* Can we swap the order by commuting the operands? */

                switch (oper)
                {
                    case GT_EQ:
                    case GT_NE:
                    case GT_LT:
                    case GT_LE:
                    case GT_GE:
                    case GT_GT:
                        if (GenTree::SwapRelop(oper) != oper)
                        {
                            tree->SetOper(GenTree::SwapRelop(oper), GenTree::PRESERVE_VN);
                        }

                        FALLTHROUGH;

                    case GT_ADD:
                    case GT_MUL:

                    case GT_OR:
                    case GT_XOR:
                    case GT_AND:

                        /* Swap the operands */

                        tree->AsOp()->gtOp1 = op2;
                        tree->AsOp()->gtOp2 = op1;
                        break;

                    case GT_QMARK:
                    case GT_COLON:
                    case GT_MKREFANY:
                        break;

                    default:
                        // Mark the operand's evaluation order to be swapped.
                        tree->gtFlags ^= GTF_REVERSE_OPS;
                        break;
                }
            }
        }

        /* Swap the level counts */
        if (tree->IsReverseOp())
        {
            std::swap(level, lvl2);
        }

        /* Compute the sethi number for this binary operator */

        if (level < 1)
        {
            level = lvl2;
        }
        else if (level == lvl2)
        {
            level += 1;
        }

        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        unsigned lvl2; // Scratch variable

        case GT_CALL:

            assert(tree->gtFlags & GTF_CALL);

            level  = 0;
            costEx = 5;
            costSz = 2;

            GenTreeCall* call;
            call = tree->AsCall();

            // Evaluate the arguments

            lvl2 = gtSetCallArgsOrder(&call->gtArgs, /* lateArgs */ false, &costEx, &costSz);
            if (level < lvl2)
            {
                level = lvl2;
            }

            // Evaluate the temp register arguments list

            lvl2 = gtSetCallArgsOrder(&call->gtArgs, /* lateArgs */ true, &costEx, &costSz);
            if (level < lvl2)
            {
                level = lvl2;
            }

            if (call->gtCallType == CT_INDIRECT)
            {
                // pinvoke-calli cookie is a constant, or constant indirection
                assert(call->gtCallCookie == nullptr || call->gtCallCookie->gtOper == GT_CNS_INT ||
                       call->gtCallCookie->gtOper == GT_IND);

                GenTree* indirect = call->gtCallAddr;

                lvl2 = gtSetEvalOrder(indirect);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += indirect->GetCostEx() + IND_COST_EX;
                costSz += indirect->GetCostSz();
            }
            else
            {
                if (call->IsVirtual())
                {
                    GenTree* controlExpr = call->gtControlExpr;
                    if (controlExpr != nullptr)
                    {
                        lvl2 = gtSetEvalOrder(controlExpr);
                        if (level < lvl2)
                        {
                            level = lvl2;
                        }
                        costEx += controlExpr->GetCostEx();
                        costSz += controlExpr->GetCostSz();
                    }
                }
#ifdef TARGET_ARM
                if (call->IsVirtualStub())
                {
                    // We generate movw/movt/ldr
                    costEx += (1 + IND_COST_EX);
                    costSz += 8;
                    if (call->gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT)
                    {
                        // Must use R12 for the ldr target -- REG_JUMP_THUNK_PARAM
                        costSz += 2;
                    }
                }
                else if (!opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
                {
                    costEx += 2;
                    costSz += 6;
                }
                costSz += 2;
#endif

#ifdef TARGET_XARCH
                costSz += 3;
#endif
            }

            level += 1;

            /* Virtual calls are a bit more expensive */
            if (call->IsVirtual())
            {
                costEx += 2 * IND_COST_EX;
                costSz += 2;
            }

            level += 5;
            costEx += 3 * IND_COST_EX;
            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            return gtSetMultiOpOrder(tree->AsMultiOp());
#endif // FEATURE_HW_INTRINSICS

        case GT_ARR_ELEM:
        {
            GenTreeArrElem* arrElem = tree->AsArrElem();

            level  = gtSetEvalOrder(arrElem->gtArrObj);
            costEx = arrElem->gtArrObj->GetCostEx();
            costSz = arrElem->gtArrObj->GetCostSz();

            for (unsigned dim = 0; dim < arrElem->gtArrRank; dim++)
            {
                lvl2 = gtSetEvalOrder(arrElem->gtArrInds[dim]);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += arrElem->gtArrInds[dim]->GetCostEx();
                costSz += arrElem->gtArrInds[dim]->GetCostSz();
            }

            level += arrElem->gtArrRank;
            costEx += 2 + (arrElem->gtArrRank * (IND_COST_EX + 1));
            costSz += 2 + (arrElem->gtArrRank * 2);
        }
        break;

        case GT_PHI:
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                lvl2 = gtSetEvalOrder(use.GetNode());
                // PHI args should always have cost 0 and level 0
                assert(lvl2 == 0);
                assert(use.GetNode()->GetCostEx() == 0);
                assert(use.GetNode()->GetCostSz() == 0);
            }
            // Give it a level of 2, just to be sure that it's greater than the LHS of
            // the parent assignment and the PHI gets evaluated first in linear order.
            // See also SsaBuilder::InsertPhi and SsaBuilder::AddPhiArg.
            level  = 2;
            costEx = 0;
            costSz = 0;
            break;

        case GT_FIELD_LIST:
            level  = 0;
            costEx = 0;
            costSz = 0;
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                unsigned opLevel = gtSetEvalOrder(use.GetNode());
                level            = max(level, opLevel);
                gtSetEvalOrder(use.GetNode());
                costEx += use.GetNode()->GetCostEx();
                costSz += use.GetNode()->GetCostSz();
            }
            break;

        case GT_CMPXCHG:
        {
            GenTree* addr = tree->AsCmpXchg()->Addr();
            level         = gtSetEvalOrder(addr);
            costSz        = addr->GetCostSz();

            GenTree* value = tree->AsCmpXchg()->Data();
            lvl2           = gtSetEvalOrder(value);
            if (level < lvl2)
            {
                level = lvl2;
            }
            costSz += value->GetCostSz();

            GenTree* comparand = tree->AsCmpXchg()->Comparand();
            lvl2               = gtSetEvalOrder(comparand);
            if (level < lvl2)
            {
                level = lvl2;
            }
            costSz += comparand->GetCostSz();

            costEx = MAX_COST; // Seriously, what could be more expensive than lock cmpxchg?
            costSz += 5;       // size of lock cmpxchg [reg+C], reg
        }
        break;

        case GT_STORE_DYN_BLK:
            level  = gtSetEvalOrder(tree->AsStoreDynBlk()->Addr());
            costEx = tree->AsStoreDynBlk()->Addr()->GetCostEx();
            costSz = tree->AsStoreDynBlk()->Addr()->GetCostSz();

            lvl2  = gtSetEvalOrder(tree->AsStoreDynBlk()->Data());
            level = max(level, lvl2);
            costEx += tree->AsStoreDynBlk()->Data()->GetCostEx();
            costSz += tree->AsStoreDynBlk()->Data()->GetCostSz();

            lvl2  = gtSetEvalOrder(tree->AsStoreDynBlk()->gtDynamicSize);
            level = max(level, lvl2);
            costEx += tree->AsStoreDynBlk()->gtDynamicSize->GetCostEx();
            costSz += tree->AsStoreDynBlk()->gtDynamicSize->GetCostSz();
            break;

        case GT_SELECT:
            level  = gtSetEvalOrder(tree->AsConditional()->gtCond);
            costEx = tree->AsConditional()->gtCond->GetCostEx();
            costSz = tree->AsConditional()->gtCond->GetCostSz();

            lvl2  = gtSetEvalOrder(tree->AsConditional()->gtOp1);
            level = max(level, lvl2);
            costEx += tree->AsConditional()->gtOp1->GetCostEx();
            costSz += tree->AsConditional()->gtOp1->GetCostSz();

            lvl2  = gtSetEvalOrder(tree->AsConditional()->gtOp2);
            level = max(level, lvl2);
            costEx += tree->AsConditional()->gtOp2->GetCostEx();
            costSz += tree->AsConditional()->gtOp2->GetCostSz();

            costEx += 1;
            costSz += 1;
            break;

        default:
            JITDUMP("unexpected operator in this tree:\n");
            DISPTREE(tree);

            NO_WAY("unexpected operator");
    }

DONE:
    // Some path through this function must have set the costs.
    assert(costEx != -1);
    assert(costSz != -1);

    tree->SetCosts(costEx, costSz);

    return level;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//------------------------------------------------------------------------
// gtMayHaveStoreInterference: Check if two trees may interfere because of a
// store in one of the trees.
//
// Parameters:
//   treeWithStores - Tree that may have stores in it
//   tree           - Tree that may be reading from a local stored to in "treeWithStores"
//
// Returns:
//   False if there is no interference. Returns true if there is any GT_LCL_VAR
//   or GT_LCL_FLD in "tree" whose value depends on a local stored in
//   "treeWithStores". May also return true in cases without interference if
//   the trees are too large and the function runs out of budget.
//
bool Compiler::gtMayHaveStoreInterference(GenTree* treeWithStores, GenTree* tree)
{
    if (((treeWithStores->gtFlags & GTF_ASG) == 0) || tree->IsInvariant())
    {
        return false;
    }

    class Visitor final : public GenTreeVisitor<Visitor>
    {
        GenTree* m_readTree;
        unsigned m_numStoresChecked = 0;

    public:
        enum
        {
            DoPreOrder = true,
        };

        Visitor(Compiler* compiler, GenTree* readTree) : GenTreeVisitor(compiler), m_readTree(readTree)
        {
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if ((node->gtFlags & GTF_ASG) == 0)
            {
                return WALK_SKIP_SUBTREES;
            }

            if (node->OperIsLocalStore())
            {
                // Check up to 8 stores before we bail with a conservative
                // answer. Avoids quadratic behavior in case we have a large
                // number of stores (e.g. created by physical promotion or by
                // call args morphing).
                if ((m_numStoresChecked >= 8) ||
                    m_compiler->gtTreeHasLocalRead(m_readTree, node->AsLclVarCommon()->GetLclNum()))
                {
                    return WALK_ABORT;
                }

                m_numStoresChecked++;
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this, tree);
    return visitor.WalkTree(&treeWithStores, nullptr) == WALK_ABORT;
}

//------------------------------------------------------------------------
// gtTreeHasLocalRead: Check if a tree has a read of the specified local,
// taking promotion into account.
//
// Parameters:
//   tree   - The tree to check.
//   lclNum - The local to look for.
//
// Returns:
//   True if there is any GT_LCL_VAR or GT_LCL_FLD node whose value depends on
//   "lclNum".
//
bool Compiler::gtTreeHasLocalRead(GenTree* tree, unsigned lclNum)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
    public:
        enum
        {
            DoPreOrder    = true,
            DoLclVarsOnly = true,
        };

        unsigned   m_lclNum;
        LclVarDsc* m_lclDsc;

        Visitor(Compiler* compiler, unsigned lclNum) : GenTreeVisitor(compiler), m_lclNum(lclNum)
        {
            m_lclDsc = compiler->lvaGetDesc(lclNum);
        }

        Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;

            if (node->OperIsLocalRead())
            {
                if (node->AsLclVarCommon()->GetLclNum() == m_lclNum)
                {
                    return WALK_ABORT;
                }

                if (m_lclDsc->lvIsStructField && (node->AsLclVarCommon()->GetLclNum() == m_lclDsc->lvParentLcl))
                {
                    return WALK_ABORT;
                }

                if (m_lclDsc->lvPromoted && (node->AsLclVarCommon()->GetLclNum() >= m_lclDsc->lvFieldLclStart) &&
                    (node->AsLclVarCommon()->GetLclNum() < m_lclDsc->lvFieldLclStart + m_lclDsc->lvFieldCnt))
                {
                    return WALK_ABORT;
                }
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this, lclNum);
    return visitor.WalkTree(&tree, nullptr) == WALK_ABORT;
}

#ifdef DEBUG
bool GenTree::OperSupportsReverseOpEvalOrder(Compiler* comp) const
{
    if (OperIsBinary())
    {
        if ((AsOp()->gtGetOp1() == nullptr) || (AsOp()->gtGetOp2() == nullptr))
        {
            return false;
        }
        if (OperIs(GT_COMMA, GT_BOUNDS_CHECK))
        {
            return false;
        }
        if (OperIs(GT_INTRINSIC))
        {
            return !comp->IsIntrinsicImplementedByUserCall(AsIntrinsic()->gtIntrinsicName);
        }
        return true;
    }
#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
    if (OperIsMultiOp())
    {
        return AsMultiOp()->GetOperandCount() == 2;
    }
#endif // FEATURE_SIMD || FEATURE_HW_INTRINSICS
    return false;
}
#endif // DEBUG

/*****************************************************************************
 *
 *  If the given tree is an integer constant that can be used
 *  in a scaled index address mode as a multiplier (e.g. "[4*index]"), then return
 *  the scale factor: 2, 4, or 8. Otherwise, return 0. Note that we never return 1,
 *  to match the behavior of GetScaleIndexShf().
 */

unsigned GenTree::GetScaleIndexMul()
{
    if (IsCnsIntOrI() && jitIsScaleIndexMul(AsIntConCommon()->IconValue()) && AsIntConCommon()->IconValue() != 1)
    {
        return (unsigned)AsIntConCommon()->IconValue();
    }

    return 0;
}

/*****************************************************************************
 *
 *  If the given tree is the right-hand side of a left shift (that is,
 *  'y' in the tree 'x' << 'y'), and it is an integer constant that can be used
 *  in a scaled index address mode as a multiplier (e.g. "[4*index]"), then return
 *  the scale factor: 2, 4, or 8. Otherwise, return 0.
 */

unsigned GenTree::GetScaleIndexShf()
{
    if (IsCnsIntOrI() && jitIsScaleIndexShift(AsIntConCommon()->IconValue()))
    {
        return (unsigned)(1 << AsIntConCommon()->IconValue());
    }

    return 0;
}

/*****************************************************************************
 *
 *  If the given tree is a scaled index (i.e. "op * 4" or "op << 2"), returns
 *  the multiplier: 2, 4, or 8; otherwise returns 0. Note that "1" is never
 *  returned.
 */

unsigned GenTree::GetScaledIndex()
{
    if (AsOp()->gtOp1->IsCnsIntOrI())
    {
        return 0;
    }

    switch (gtOper)
    {
        case GT_MUL:
            return AsOp()->gtOp2->GetScaleIndexMul();

        case GT_LSH:
            return AsOp()->gtOp2->GetScaleIndexShf();

        default:
            assert(!"GenTree::GetScaledIndex() called with illegal gtOper");
            break;
    }

    return 0;
}

//------------------------------------------------------------------------
// TryGetUse: Get the use edge for an operand of this tree.
//
// Arguments:
//    operand - the node to find the use for
//    pUse    - [out] parameter for the use
//
// Return Value:
//    Whether "operand" is a child of this node. If it is, "*pUse" is set,
//    allowing for the replacement of "operand" with some other node.
//
bool GenTree::TryGetUse(GenTree* operand, GenTree*** pUse)
{
    assert(operand != nullptr);
    assert(pUse != nullptr);

    switch (OperGet())
    {
        // Leaf nodes
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_ADDR:
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
            return false;

        // Standard unary operators
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        case GT_NOT:
        case GT_NEG:
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
        case GT_RUNTIMELOOKUP:
        case GT_ARR_ADDR:
        case GT_INIT_VAL:
        case GT_JTRUE:
        case GT_SWITCH:
        case GT_NULLCHECK:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
        case GT_RETURNTRAP:
        case GT_RETURN:
        case GT_RETFILT:
        case GT_BSWAP:
        case GT_BSWAP16:
        case GT_KEEPALIVE:
        case GT_INC_SATURATE:
            if (operand == this->AsUnOp()->gtOp1)
            {
                *pUse = &this->AsUnOp()->gtOp1;
                return true;
            }
            return false;

// Variadic nodes
#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
            if (this->AsUnOp()->gtOp1->gtOper == GT_FIELD_LIST)
            {
                return this->AsUnOp()->gtOp1->TryGetUse(operand, pUse);
            }
            if (operand == this->AsUnOp()->gtOp1)
            {
                *pUse = &this->AsUnOp()->gtOp1;
                return true;
            }
            return false;
#endif // FEATURE_ARG_SPLIT

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            for (GenTree** opUse : this->AsMultiOp()->UseEdges())
            {
                if (*opUse == operand)
                {
                    *pUse = opUse;
                    return true;
                }
            }
            return false;
#endif // FEATURE_HW_INTRINSICS

        // Special nodes
        case GT_PHI:
            for (GenTreePhi::Use& phiUse : AsPhi()->Uses())
            {
                if (phiUse.GetNode() == operand)
                {
                    *pUse = &phiUse.NodeRef();
                    return true;
                }
            }
            return false;

        case GT_FIELD_LIST:
            for (GenTreeFieldList::Use& fieldUse : AsFieldList()->Uses())
            {
                if (fieldUse.GetNode() == operand)
                {
                    *pUse = &fieldUse.NodeRef();
                    return true;
                }
            }
            return false;

        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* const cmpXchg = this->AsCmpXchg();
            if (operand == cmpXchg->Addr())
            {
                *pUse = &cmpXchg->Addr();
                return true;
            }
            if (operand == cmpXchg->Data())
            {
                *pUse = &cmpXchg->Data();
                return true;
            }
            if (operand == cmpXchg->Comparand())
            {
                *pUse = &cmpXchg->Comparand();
                return true;
            }
            return false;
        }

        case GT_ARR_ELEM:
        {
            GenTreeArrElem* const arrElem = this->AsArrElem();
            if (operand == arrElem->gtArrObj)
            {
                *pUse = &arrElem->gtArrObj;
                return true;
            }
            for (unsigned i = 0; i < arrElem->gtArrRank; i++)
            {
                if (operand == arrElem->gtArrInds[i])
                {
                    *pUse = &arrElem->gtArrInds[i];
                    return true;
                }
            }
            return false;
        }

        case GT_STORE_DYN_BLK:
        {
            GenTreeStoreDynBlk* const dynBlock = this->AsStoreDynBlk();
            if (operand == dynBlock->gtOp1)
            {
                *pUse = &dynBlock->gtOp1;
                return true;
            }
            if (operand == dynBlock->gtOp2)
            {
                *pUse = &dynBlock->gtOp2;
                return true;
            }
            if (operand == dynBlock->gtDynamicSize)
            {
                *pUse = &dynBlock->gtDynamicSize;
                return true;
            }
            return false;
        }

        case GT_CALL:
        {
            GenTreeCall* const call = this->AsCall();
            if (operand == call->gtControlExpr)
            {
                *pUse = &call->gtControlExpr;
                return true;
            }
            if (call->gtCallType == CT_INDIRECT)
            {
                if (operand == call->gtCallCookie)
                {
                    *pUse = &call->gtCallCookie;
                    return true;
                }
                if (operand == call->gtCallAddr)
                {
                    *pUse = &call->gtCallAddr;
                    return true;
                }
            }
            for (CallArg& arg : call->gtArgs.Args())
            {
                if (arg.GetEarlyNode() == operand)
                {
                    *pUse = &arg.EarlyNodeRef();
                    return true;
                }
                if (arg.GetLateNode() == operand)
                {
                    *pUse = &arg.LateNodeRef();
                    return true;
                }
            }
            return false;
        }
#ifdef TARGET_ARM64
        case GT_SELECT_NEG:
        case GT_SELECT_INV:
        case GT_SELECT_INC:
#endif
        case GT_SELECT:
        {
            GenTreeConditional* const conditional = this->AsConditional();
            if (operand == conditional->gtCond)
            {
                *pUse = &conditional->gtCond;
                return true;
            }
            if (operand == conditional->gtOp1)
            {
                *pUse = &conditional->gtOp1;
                return true;
            }
            if (operand == conditional->gtOp2)
            {
                *pUse = &conditional->gtOp2;
                return true;
            }
            return false;
        }

        // Binary nodes
        default:
            assert(this->OperIsBinary());
            return TryGetUseBinOp(operand, pUse);
    }
}

bool GenTree::TryGetUseBinOp(GenTree* operand, GenTree*** pUse)
{
    assert(operand != nullptr);
    assert(pUse != nullptr);
    assert(this->OperIsBinary());

    GenTreeOp* const binOp = this->AsOp();
    if (operand == binOp->gtOp1)
    {
        *pUse = &binOp->gtOp1;
        return true;
    }
    if (operand == binOp->gtOp2)
    {
        *pUse = &binOp->gtOp2;
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// GenTree::ReplaceOperand:
//    Replace a given operand to this node with a new operand. If the
//    current node is a call node, this will also update the call
//    argument table if necessary.
//
// Arguments:
//    useEdge - the use edge that points to the operand to be replaced.
//    replacement - the replacement node.
//
void GenTree::ReplaceOperand(GenTree** useEdge, GenTree* replacement)
{
    assert(useEdge != nullptr);
    assert(replacement != nullptr);
    assert(TryGetUse(*useEdge, &useEdge));
    *useEdge = replacement;
}

//------------------------------------------------------------------------
// gtGetParent: Get the parent of this node, and optionally capture the
//    pointer to the child so that it can be modified.
//
// Arguments:
//    pUse - A pointer to a GenTree** (yes, that's three
//           levels, i.e. GenTree ***), which if non-null,
//           will be set to point to the field in the parent
//           that points to this node.
//
//  Return value
//    The parent of this node.
//
//  Notes:
//    This requires that the execution order must be defined (i.e. gtSetEvalOrder() has been called).
//    To enable the child to be replaced, it accepts an argument, "pUse", that, if non-null,
//    will be set to point to the child pointer in the parent that points to this node.
//
GenTree* GenTree::gtGetParent(GenTree*** pUse)
{
    // Find the parent node; it must be after this node in the execution order.
    GenTree*  user;
    GenTree** use = nullptr;
    for (user = gtNext; user != nullptr; user = user->gtNext)
    {
        if (user->TryGetUse(this, &use))
        {
            break;
        }
    }

    if (pUse != nullptr)
    {
        *pUse = use;
    }

    return user;
}

//------------------------------------------------------------------------------
// OperRequiresAsgFlag : Check whether the operation requires GTF_ASG flag regardless
//                       of the children's flags.
//

bool GenTree::OperRequiresAsgFlag() const
{
    switch (OperGet())
    {
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        case GT_STOREIND:
        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        case GT_XADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XCHG:
        case GT_LOCKADD:
        case GT_CMPXCHG:
        case GT_MEMORYBARRIER:
            return true;

        // If the call has return buffer argument, it produced a definition and hence
        // should be marked with assignment.
        case GT_CALL:
            return AsCall()->IsOptimizingRetBufAsLocal();

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            return AsHWIntrinsic()->OperRequiresAsgFlag();
#endif // FEATURE_HW_INTRINSICS

        default:
            assert(!OperIsStore() && !OperIsAtomicOp());
            return false;
    }
}

//------------------------------------------------------------------------------
// OperRequiresCallFlag : Check whether the operation requires GTF_CALL flag regardless
//                        of the children's flags.
//
bool GenTree::OperRequiresCallFlag(Compiler* comp) const
{
    switch (gtOper)
    {
        case GT_CALL:
            return true;

        case GT_KEEPALIVE:
            return true;

        case GT_INTRINSIC:
            return comp->IsIntrinsicImplementedByUserCall(this->AsIntrinsic()->gtIntrinsicName);

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
        {
            return AsHWIntrinsic()->OperRequiresCallFlag();
        }
#endif // FEATURE_HW_INTRINSICS

#if FEATURE_FIXED_OUT_ARGS && !defined(TARGET_64BIT)
        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:

            // Variable shifts of a long end up being helper calls, so mark the tree as such in morph.
            // This is potentially too conservative, since they'll get treated as having side effects.
            // It is important to mark them as calls so if they are part of an argument list,
            // they will get sorted and processed properly (for example, it is important to handle
            // all nested calls before putting struct arguments in the argument registers). We
            // could mark the trees just before argument processing, but it would require a full
            // tree walk of the argument tree, so we just do it when morphing, instead, even though we'll
            // mark non-argument trees (that will still get converted to calls, anyway).
            return (this->TypeGet() == TYP_LONG) && (gtGetOp2()->OperGet() != GT_CNS_INT);
#endif // FEATURE_FIXED_OUT_ARGS && !TARGET_64BIT

        default:
            return false;
    }
}

//------------------------------------------------------------------------
// IndirMayFault: May this indirection-like node throw an NRE?
//
// Arguments:
//    compiler - the compiler instance
//
// Return Value:
//    Whether this node's address may be null.
//
bool GenTree::IndirMayFault(Compiler* compiler)
{
    assert(OperIsIndirOrArrMetaData());
    return ((gtFlags & GTF_IND_NONFAULTING) == 0) && compiler->fgAddrCouldBeNull(GetIndirOrArrMetaDataAddr());
}

//------------------------------------------------------------------------------
// OperIsImplicitIndir : Check whether the operation contains an implicit indirection.
//
// Arguments:
//    this      -  a GenTree node
//
// Return Value:
//    True if the given node contains an implicit indirection
//
// Note that for the [HW]INTRINSIC nodes we have to examine the
// details of the node to determine its result.
//
bool GenTree::OperIsImplicitIndir() const
{
    switch (gtOper)
    {
        case GT_LOCKADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
        case GT_BLK:
        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        case GT_BOX:
        case GT_ARR_ELEM:
        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
        case GT_MDARR_LOWER_BOUND:
            return true;
        case GT_INTRINSIC:
            return AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType;
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
        {
            return AsHWIntrinsic()->OperIsMemoryLoadOrStore();
        }
#endif // FEATURE_HW_INTRINSICS
        default:
            return false;
    }
}

//------------------------------------------------------------------------------
// OperExceptions: Get exception set this tree may throw.
//
// Arguments:
//    comp      -  Compiler instance
//
// Return Value:
//    A bit set of exceptions this tree may throw.
//
// Remarks:
//    Should not be used on calls given that we can say nothing precise about
//    those.
//
ExceptionSetFlags GenTree::OperExceptions(Compiler* comp)
{
    switch (gtOper)
    {
        case GT_ADD:
        case GT_SUB:
        case GT_MUL:
        case GT_CAST:
#ifndef TARGET_64BIT
        case GT_ADD_HI:
        case GT_SUB_HI:
#endif // !TARGET_64BIT
            return gtOverflow() ? ExceptionSetFlags::OverflowException : ExceptionSetFlags::None;

        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:
        {
            if (varTypeIsFloating(this->TypeGet()))
            {
                return ExceptionSetFlags::None;
            }

            ExceptionSetFlags exSetFlags = ExceptionSetFlags::None;

            if (!(this->gtFlags & GTF_DIV_MOD_NO_BY_ZERO) && !this->gtGetOp2()->gtSkipReloadOrCopy()->IsNeverZero())
            {
                exSetFlags |= ExceptionSetFlags::DivideByZeroException;
            }

            if (this->OperIs(GT_DIV, GT_MOD) && this->CanDivOrModPossiblyOverflow(comp))
            {
                exSetFlags |= ExceptionSetFlags::ArithmeticException;
            }

            return exSetFlags;
        }

        case GT_INTRINSIC:
            // If this is an intrinsic that represents the object.GetType(), it can throw an NullReferenceException.
            // Currently, this is the only intrinsic that can throw an exception.
            if (AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType)
            {
                return ExceptionSetFlags::NullReferenceException;
            }

            return ExceptionSetFlags::None;

        case GT_CALL:
            assert(!"Unexpected GT_CALL in OperExceptions");
            return ExceptionSetFlags::All;

        case GT_LOCKADD:
        case GT_XAND:
        case GT_XORR:
        case GT_XADD:
        case GT_XCHG:
        case GT_CMPXCHG:
        case GT_IND:
        case GT_STOREIND:
        case GT_BLK:
        case GT_NULLCHECK:
        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
        case GT_MDARR_LOWER_BOUND:
            return IndirMayFault(comp) ? ExceptionSetFlags::NullReferenceException : ExceptionSetFlags::None;

        case GT_ARR_ELEM:
            if (comp->fgAddrCouldBeNull(this->AsArrElem()->gtArrObj))
            {
                return ExceptionSetFlags::NullReferenceException | ExceptionSetFlags::IndexOutOfRangeException;
            }

            return ExceptionSetFlags::IndexOutOfRangeException;

        case GT_FIELD_ADDR:
            if (AsFieldAddr()->IsInstance() && comp->fgAddrCouldBeNull(AsFieldAddr()->GetFldObj()))
            {
                return ExceptionSetFlags::NullReferenceException;
            }

            return ExceptionSetFlags::None;

        case GT_BOUNDS_CHECK:
            return ExceptionSetFlags::IndexOutOfRangeException;

        case GT_INDEX_ADDR:
            return ExceptionSetFlags::NullReferenceException | ExceptionSetFlags::IndexOutOfRangeException;

        case GT_CKFINITE:
            return ExceptionSetFlags::ArithmeticException;

        case GT_LCLHEAP:
            return ExceptionSetFlags::StackOverflowException;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
        {
            GenTreeHWIntrinsic* hwIntrinsicNode = this->AsHWIntrinsic();

            if (hwIntrinsicNode->OperIsMemoryLoadOrStore())
            {
                assert((gtFlags & GTF_IND_NONFAULTING) == 0);

                // TODO-CQ: We should use comp->fgAddrCouldBeNull on the address operand
                // to determine if this can actually produce an NRE or not

                return ExceptionSetFlags::NullReferenceException;
            }

            return ExceptionSetFlags::None;
        }
#endif // FEATURE_HW_INTRINSICS

        default:
            assert(!OperMayOverflow() && !OperIsIndirOrArrMetaData());
            return ExceptionSetFlags::None;
    }
}

//------------------------------------------------------------------------------
// OperMayThrow : Check whether the operation may throw.
//
//
// Arguments:
//    comp      -  Compiler instance
//
// Return Value:
//    True if the given operator may cause an exception
//
bool GenTree::OperMayThrow(Compiler* comp)
{
    if (OperIs(GT_CALL))
    {
        CorInfoHelpFunc helper;
        helper = comp->eeGetHelperNum(this->AsCall()->gtCallMethHnd);
        return ((helper == CORINFO_HELP_UNDEF) || !comp->s_helperCallProperties.NoThrow(helper));
    }

    return OperExceptions(comp) != ExceptionSetFlags::None;
}

//------------------------------------------------------------------------------
// OperRequiresGlobRefFlag : Check whether the operation requires GTF_GLOB_REF
//                           flag regardless of the children's flags.
//
// Arguments:
//    comp - Compiler instance
//
// Return Value:
//    True if the given operator requires GTF_GLOB_REF
//
// Remarks:
//    Globally visible stores and loads, as well as some equivalently modeled
//    operations, require the GLOB_REF flag to be set on the node.
//
//    This function is only valid after local morph when we know which locals
//    are address exposed, and the flag in gtFlags is only kept valid after
//    morph has run. Before local morph the property can be conservatively
//    approximated for locals with lvHasLdAddrOp.
//
bool GenTree::OperRequiresGlobRefFlag(Compiler* comp) const
{
    switch (OperGet())
    {
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            return comp->lvaGetDesc(AsLclVarCommon())->IsAddressExposed();

        case GT_IND:
        case GT_BLK:
            if (AsIndir()->IsInvariantLoad())
            {
                return false;
            }
            FALLTHROUGH;

        case GT_STOREIND:
        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        case GT_XADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XCHG:
        case GT_LOCKADD:
        case GT_CMPXCHG:
        case GT_MEMORYBARRIER:
        case GT_KEEPALIVE:
            return true;

        case GT_CALL:
            return AsCall()->HasSideEffects(comp, /* ignoreExceptions */ true);

        case GT_ALLOCOBJ:
            return AsAllocObj()->gtHelperHasSideEffects;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            return AsHWIntrinsic()->OperRequiresGlobRefFlag();
#endif // FEATURE_HW_INTRINSICS

        default:
            assert(!OperRequiresCallFlag(comp) || OperIs(GT_INTRINSIC));
            assert((!OperIsIndir() || OperIs(GT_NULLCHECK)) && !OperRequiresAsgFlag());
            return false;
    }
}

//------------------------------------------------------------------------------
// OperSupportsOrderingSideEffect : Check whether the operation supports the
// GTF_ORDER_SIDEEFF flag.
//
// Return Value:
//   True if the given operator supports GTF_ORDER_SIDEEFF.
//
// Remarks:
//   A node will still have this flag set if an operand has it set, even if the
//   parent does not support it. This situation indicates that reordering the
//   parent may be ok as long as it does not break ordering dependencies of the
//   operand.
//
bool GenTree::OperSupportsOrderingSideEffect() const
{
    if (TypeIs(TYP_BYREF))
    {
        // Forming byrefs may only be legal due to previous checks.
        return true;
    }

    switch (OperGet())
    {
        case GT_BOUNDS_CHECK:
        case GT_IND:
        case GT_BLK:
        case GT_STOREIND:
        case GT_NULLCHECK:
        case GT_STORE_BLK:
        case GT_STORE_DYN_BLK:
        case GT_XADD:
        case GT_XORR:
        case GT_XAND:
        case GT_XCHG:
        case GT_LOCKADD:
        case GT_CMPXCHG:
        case GT_MEMORYBARRIER:
        case GT_CATCH_ARG:
            return true;
        default:
            return false;
    }
}

//------------------------------------------------------------------------------
// OperEffects: Compute effect flags that are relevant to this node only,
// excluding its children.
//
// Arguments:
//    comp -  Compiler instance
//
// Return Value:
//    The effect flags.
//
GenTreeFlags GenTree::OperEffects(Compiler* comp)
{
    GenTreeFlags flags = gtFlags & GTF_ALL_EFFECT;

    if (((flags & GTF_ASG) != 0) && !OperRequiresAsgFlag())
    {
        flags &= ~GTF_ASG;
    }

    if (((flags & GTF_CALL) != 0) && !OperRequiresCallFlag(comp))
    {
        flags &= ~GTF_CALL;
    }

    if (((flags & GTF_EXCEPT) != 0) && !OperMayThrow(comp))
    {
        flags &= ~GTF_EXCEPT;
    }

    if (((flags & GTF_GLOB_REF) != 0) && !OperRequiresGlobRefFlag(comp))
    {
        flags &= ~GTF_GLOB_REF;
    }

    if (((flags & GTF_ORDER_SIDEEFF) != 0) && !OperSupportsOrderingSideEffect())
    {
        flags &= ~GTF_ORDER_SIDEEFF;
    }

    return flags;
}

//-----------------------------------------------------------------------------------
// GetFieldCount: Return the register count for a multi-reg lclVar.
//
// Arguments:
//     compiler - the current Compiler instance.
//
// Return Value:
//     Returns the number of registers defined by this node.
//
// Notes:
//     This must be a multireg lclVar.
//
unsigned int GenTreeLclVar::GetFieldCount(Compiler* compiler) const
{
    assert(IsMultiReg());
    LclVarDsc* varDsc = compiler->lvaGetDesc(GetLclNum());
    return varDsc->lvFieldCnt;
}

//-----------------------------------------------------------------------------------
// GetFieldTypeByIndex: Get a specific register's type, based on regIndex, that is produced
//                    by this multi-reg node.
//
// Arguments:
//     compiler - the current Compiler instance.
//     idx      - which register type to return.
//
// Return Value:
//     The register type assigned to this index for this node.
//
// Notes:
//     This must be a multireg lclVar and 'regIndex' must be a valid index for this node.
//
var_types GenTreeLclVar::GetFieldTypeByIndex(Compiler* compiler, unsigned idx)
{
    assert(IsMultiReg());
    LclVarDsc* varDsc      = compiler->lvaGetDesc(GetLclNum());
    LclVarDsc* fieldVarDsc = compiler->lvaGetDesc(varDsc->lvFieldLclStart + idx);
    assert(fieldVarDsc->TypeGet() != TYP_STRUCT); // Don't expect struct fields.
    return fieldVarDsc->TypeGet();
}

#if DEBUGGABLE_GENTREE
// static
GenTree::VtablePtr GenTree::s_vtablesForOpers[] = {nullptr};
GenTree::VtablePtr GenTree::s_vtableForOp       = nullptr;

GenTree::VtablePtr GenTree::GetVtableForOper(genTreeOps oper)
{
    noway_assert(oper < GT_COUNT);

    // First, check a cache.

    if (s_vtablesForOpers[oper] != nullptr)
    {
        return s_vtablesForOpers[oper];
    }

    // Otherwise, look up the correct vtable entry. Note that we want the most derived GenTree subtype
    // for an oper. E.g., GT_LCL_VAR is defined in GTSTRUCT_3 as GenTreeLclVar and in GTSTRUCT_N as
    // GenTreeLclVarCommon. We want the GenTreeLclVar vtable, since nothing should actually be
    // instantiated as a GenTreeLclVarCommon.

    VtablePtr res = nullptr;
    switch (oper)
    {

// clang-format off

#define GTSTRUCT_0(nm, tag)                             /*handle explicitly*/
#define GTSTRUCT_1(nm, tag)                             \
        case tag:                                       \
        {                                               \
            GenTree##nm gt;                             \
            res = *reinterpret_cast<VtablePtr*>(&gt);   \
        }                                               \
        break;
#define GTSTRUCT_2(nm, tag, tag2)                       \
        case tag:                                       \
        case tag2:                                      \
        {                                               \
            GenTree##nm gt;                             \
            res = *reinterpret_cast<VtablePtr*>(&gt);   \
        }                                               \
        break;
#define GTSTRUCT_3(nm, tag, tag2, tag3)                 \
        case tag:                                       \
        case tag2:                                      \
        case tag3:                                      \
        {                                               \
            GenTree##nm gt;                             \
            res = *reinterpret_cast<VtablePtr*>(&gt);   \
        }                                               \
        break;
#define GTSTRUCT_4(nm, tag, tag2, tag3, tag4)           \
        case tag:                                       \
        case tag2:                                      \
        case tag3:                                      \
        case tag4:                                      \
        {                                               \
            GenTree##nm gt;                             \
            res = *reinterpret_cast<VtablePtr*>(&gt);   \
        }                                               \
        break;
#define GTSTRUCT_N(nm, ...)                             /*handle explicitly*/
#define GTSTRUCT_2_SPECIAL(nm, tag, tag2)               /*handle explicitly*/
#define GTSTRUCT_3_SPECIAL(nm, tag, tag2, tag3)         /*handle explicitly*/
#include "gtstructs.h"

        // clang-format on

        // Handle the special cases.
        // The following opers are in GTSTRUCT_N but no other place (namely, no subtypes).
        case GT_STORE_BLK:
        case GT_BLK:
        {
            GenTreeBlk gt;
            res = *reinterpret_cast<VtablePtr*>(&gt);
        }
        break;

        case GT_IND:
        case GT_NULLCHECK:
        {
            GenTreeIndir gt;
            res = *reinterpret_cast<VtablePtr*>(&gt);
        }
        break;

        // We don't need to handle GTSTRUCT_N for LclVarCommon, since all those allowed opers are specified
        // in their proper subtype. Similarly for GenTreeIndir.

        default:
        {
            // Should be unary or binary op.
            if (s_vtableForOp == nullptr)
            {
                unsigned opKind = OperKind(oper);
                assert(!IsExOp(opKind));
                assert(OperIsSimple(oper) || OperIsLeaf(oper));
                // Need to provide non-null operands.
                GenTreeIntCon dummyOp(TYP_INT, 0);
                GenTreeOp     gt(oper, TYP_INT, &dummyOp, ((opKind & GTK_UNOP) ? nullptr : &dummyOp));
                s_vtableForOp = *reinterpret_cast<VtablePtr*>(&gt);
            }
            res = s_vtableForOp;
            break;
        }
    }
    s_vtablesForOpers[oper] = res;
    return res;
}

void GenTree::SetVtableForOper(genTreeOps oper)
{
    *reinterpret_cast<VtablePtr*>(this) = GetVtableForOper(oper);
}
#endif // DEBUGGABLE_GENTREE

GenTreeOp* Compiler::gtNewOperNode(genTreeOps oper, var_types type, GenTree* op1, GenTree* op2)
{
    assert(op1 != nullptr);
    assert(op2 != nullptr);

    // We should not be allocating nodes that extend GenTreeOp with this;
    // should call the appropriate constructor for the extended type.
    assert(!GenTree::IsExOp(GenTree::OperKind(oper)));

    GenTreeOp* node = new (this, oper) GenTreeOp(oper, type, op1, op2);

    return node;
}

GenTreeCC* Compiler::gtNewCC(genTreeOps oper, var_types type, GenCondition cond)
{
    GenTreeCC* node = new (this, oper) GenTreeCC(oper, type, cond);
    return node;
}

GenTreeOpCC* Compiler::gtNewOperCC(genTreeOps oper, var_types type, GenCondition cond, GenTree* op1, GenTree* op2)
{
    GenTreeOpCC* node = new (this, oper) GenTreeOpCC(oper, type, cond, op1, op2);
    return node;
}

GenTreeColon* Compiler::gtNewColonNode(var_types type, GenTree* thenNode, GenTree* elseNode)
{
    return new (this, GT_COLON) GenTreeColon(type, thenNode, elseNode);
}

GenTreeQmark* Compiler::gtNewQmarkNode(var_types type, GenTree* cond, GenTreeColon* colon)
{
    compQmarkUsed        = true;
    GenTreeQmark* result = new (this, GT_QMARK) GenTreeQmark(type, cond, colon);
    assert(!compQmarkRationalized && "QMARKs are illegal to create after QMARK-rationalization");
    return result;
}

GenTreeIntCon* Compiler::gtNewIconNode(ssize_t value, var_types type)
{
    assert(genActualType(type) == type);
    return new (this, GT_CNS_INT) GenTreeIntCon(type, value);
}

GenTreeIntCon* Compiler::gtNewIconNode(unsigned fieldOffset, FieldSeq* fieldSeq)
{
    return new (this, GT_CNS_INT) GenTreeIntCon(TYP_I_IMPL, static_cast<ssize_t>(fieldOffset), fieldSeq);
}

GenTreeIntCon* Compiler::gtNewNull()
{
    return gtNewIconNode(0, TYP_REF);
}

GenTreeIntCon* Compiler::gtNewTrue()
{
    return gtNewIconNode(1, TYP_INT);
}

GenTreeIntCon* Compiler::gtNewFalse()
{
    return gtNewIconNode(0, TYP_INT);
}

// return a new node representing the value in a physical register
GenTree* Compiler::gtNewPhysRegNode(regNumber reg, var_types type)
{
    assert(genIsValidIntReg(reg) || (reg == REG_SPBASE));
    GenTree* result = new (this, GT_PHYSREG) GenTreePhysReg(reg, type);
    return result;
}

GenTree* Compiler::gtNewJmpTableNode()
{
    return new (this, GT_JMPTABLE) GenTree(GT_JMPTABLE, TYP_I_IMPL);
}

/*****************************************************************************
 *
 *  Converts an annotated token into an icon flags (so that we will later be
 *  able to tell the type of the handle that will be embedded in the icon
 *  node)
 */

GenTreeFlags Compiler::gtTokenToIconFlags(unsigned token)
{
    GenTreeFlags flags = GTF_EMPTY;

    switch (TypeFromToken(token))
    {
        case mdtTypeRef:
        case mdtTypeDef:
        case mdtTypeSpec:
            flags = GTF_ICON_CLASS_HDL;
            break;

        case mdtMethodDef:
            flags = GTF_ICON_METHOD_HDL;
            break;

        case mdtFieldDef:
            flags = GTF_ICON_FIELD_HDL;
            break;

        default:
            flags = GTF_ICON_TOKEN_HDL;
            break;
    }

    return flags;
}

//-----------------------------------------------------------------------------------------
// gtNewIndOfIconHandleNode: Creates an indirection GenTree node of a constant handle
//
// Arguments:
//    indType     - The type returned by the indirection node
//    addr        - The constant address to read from
//    iconFlags   - The GTF_ICON flag value that specifies the kind of handle that we have
//    isInvariant - The indNode should also be marked as invariant
//
// Return Value:
//    Returns a GT_IND node representing value at the address provided by 'addr'
//
// Notes:
//    The GT_IND node is marked as non-faulting
//    If the indType is GT_REF we also mark the indNode as GTF_GLOB_REF
//
GenTree* Compiler::gtNewIndOfIconHandleNode(var_types indType, size_t addr, GenTreeFlags iconFlags, bool isInvariant)
{
    GenTree*     addrNode   = gtNewIconHandleNode(addr, iconFlags);
    GenTreeFlags indirFlags = GTF_IND_NONFAULTING; // This indirection won't cause an exception.

    if (isInvariant)
    {
        assert(iconFlags != GTF_ICON_STATIC_HDL); // Pointer to a mutable class Static variable
        assert(iconFlags != GTF_ICON_BBC_PTR);    // Pointer to a mutable basic block count value
        assert(iconFlags != GTF_ICON_GLOBAL_PTR); // Pointer to mutable data from the VM state

        // This indirection also is invariant.
        indirFlags |= GTF_IND_INVARIANT;

        if (iconFlags == GTF_ICON_STR_HDL)
        {
            // String literals are never null
            indirFlags |= GTF_IND_NONNULL;
        }
    }

    GenTree* indNode = gtNewIndir(indType, addrNode, indirFlags);
    return indNode;
}

/*****************************************************************************
 *
 *  Allocates a integer constant entry that represents a HANDLE to something.
 *  It may not be allowed to embed HANDLEs directly into the JITed code (for eg,
 *  as arguments to JIT helpers). Get a corresponding value that can be embedded.
 *  If the handle needs to be accessed via an indirection, pValue points to it.
 */

GenTree* Compiler::gtNewIconEmbHndNode(void* value, void* pValue, GenTreeFlags iconFlags, void* compileTimeHandle)
{
    GenTreeIntCon* iconNode;
    GenTree*       handleNode;

    if (value != nullptr)
    {
        // When 'value' is non-null, pValue is required to be null
        assert(pValue == nullptr);

        // use 'value' to construct an integer constant node
        iconNode = gtNewIconHandleNode((size_t)value, iconFlags);

        // 'value' is the handle
        handleNode = iconNode;
    }
    else
    {
        // When 'value' is null, pValue is required to be non-null
        assert(pValue != nullptr);

        // use 'pValue' to construct an integer constant node
        iconNode = gtNewIconHandleNode((size_t)pValue, iconFlags);

        // 'pValue' is an address of a location that contains the handle, construct the indirection of 'pValue'.
        handleNode = gtNewIndir(TYP_I_IMPL, iconNode, GTF_IND_NONFAULTING | GTF_IND_INVARIANT);
    }

    iconNode->gtCompileTimeHandle = (size_t)compileTimeHandle;
#ifdef DEBUG
    if (iconFlags == GTF_ICON_FTN_ADDR)
    {
        iconNode->gtTargetHandle = (size_t)compileTimeHandle;
    }
    if (iconFlags == GTF_ICON_OBJ_HDL)
    {
        iconNode->gtTargetHandle = (size_t)value;
    }
#endif
    return handleNode;
}

/*****************************************************************************/
GenTree* Compiler::gtNewStringLiteralNode(InfoAccessType iat, void* pValue)
{
    GenTree* tree = nullptr;

    switch (iat)
    {
        case IAT_VALUE:
            setMethodHasFrozenObjects();
            tree = gtNewIconEmbHndNode(pValue, nullptr, GTF_ICON_OBJ_HDL, nullptr);
#ifdef DEBUG
            tree->AsIntCon()->gtTargetHandle = (size_t)pValue;
#endif
            break;

        case IAT_PVALUE: // The value needs to be accessed via an indirection
            // Create an indirection
            tree = gtNewIndOfIconHandleNode(TYP_REF, (size_t)pValue, GTF_ICON_STR_HDL, true);
#ifdef DEBUG
            tree->gtGetOp1()->AsIntCon()->gtTargetHandle = (size_t)pValue;
#endif
            break;

        case IAT_PPVALUE: // The value needs to be accessed via a double indirection
            // Create the first indirection.
            tree = gtNewIndOfIconHandleNode(TYP_I_IMPL, (size_t)pValue, GTF_ICON_CONST_PTR, true);
#ifdef DEBUG
            tree->gtGetOp1()->AsIntCon()->gtTargetHandle = (size_t)pValue;
#endif
            // Create the second indirection.
            tree = gtNewIndir(TYP_REF, tree, GTF_IND_NONFAULTING | GTF_IND_INVARIANT | GTF_IND_NONNULL);
            break;

        default:
            noway_assert(!"Unexpected InfoAccessType");
    }

    return tree;
}

//------------------------------------------------------------------------
// gtNewStringLiteralLength: create GenTreeIntCon node for the given string
//    literal to store its length.
//
// Arguments:
//    node  - string literal node.
//
// Return Value:
//    GenTreeIntCon node with string's length as a value or null.
//
GenTreeIntCon* Compiler::gtNewStringLiteralLength(GenTreeStrCon* node)
{
    if (node->IsStringEmptyField())
    {
        JITDUMP("Folded String.Empty.Length to 0\n");
        return gtNewIconNode(0);
    }

    int length = info.compCompHnd->getStringLiteral(node->gtScpHnd, node->gtSconCPX, nullptr, 0);
    if (length >= 0)
    {
        GenTreeIntCon* iconNode = gtNewIconNode(length);
        JITDUMP("Folded 'CNS_STR.Length' to '%d'\n", length)
        return iconNode;
    }
    return nullptr;
}

/*****************************************************************************/

GenTree* Compiler::gtNewLconNode(__int64 value)
{
#ifdef TARGET_64BIT
    GenTree* node = new (this, GT_CNS_INT) GenTreeIntCon(TYP_LONG, value);
#else
    GenTree* node = new (this, GT_CNS_LNG) GenTreeLngCon(value);
#endif

    return node;
}

GenTree* Compiler::gtNewDconNodeF(float value)
{
    return gtNewDconNode(FloatingPointUtils::convertToDouble(value), TYP_FLOAT);
}

GenTree* Compiler::gtNewDconNodeD(double value)
{
    return gtNewDconNode(value, TYP_DOUBLE);
}

GenTree* Compiler::gtNewDconNode(double value, var_types type)
{
    return new (this, GT_CNS_DBL) GenTreeDblCon(value, type);
}

GenTree* Compiler::gtNewSconNode(int CPX, CORINFO_MODULE_HANDLE scpHandle)
{
    // 'GT_CNS_STR' nodes later get transformed into 'GT_CALL'
    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_CNS_STR]);
    GenTree* node = new (this, GT_CALL) GenTreeStrCon(CPX, scpHandle DEBUGARG(/*largeNode*/ true));
    return node;
}

GenTreeVecCon* Compiler::gtNewVconNode(var_types type)
{
    GenTreeVecCon* vecCon = new (this, GT_CNS_VEC) GenTreeVecCon(type);
    return vecCon;
}

GenTreeVecCon* Compiler::gtNewVconNode(var_types type, void* data)
{
    GenTreeVecCon* vecCon = new (this, GT_CNS_VEC) GenTreeVecCon(type);
    memcpy(&vecCon->gtSimdVal, data, genTypeSize(type));
    return vecCon;
}

GenTree* Compiler::gtNewAllBitsSetConNode(var_types type)
{
#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(type))
    {
        GenTreeVecCon* allBitsSet = gtNewVconNode(type);
        allBitsSet->gtSimdVal     = simd_t::AllBitsSet();
        return allBitsSet;
    }
#endif // FEATURE_SIMD

    switch (type)
    {
        case TYP_INT:
        {
            return gtNewIconNode(-1);
        }

        case TYP_LONG:
        {
            return gtNewLconNode(-1);
        }

        default:
        {
            unreached();
        }
    }
}

GenTree* Compiler::gtNewZeroConNode(var_types type)
{
#ifdef FEATURE_SIMD
    if (varTypeIsSIMD(type))
    {
        GenTreeVecCon* vecCon = gtNewVconNode(type);
        vecCon->gtSimdVal     = simd_t::Zero();
        return vecCon;
    }
#endif // FEATURE_SIMD

    type = genActualType(type);
    switch (type)
    {
        case TYP_INT:
        case TYP_REF:
        case TYP_BYREF:
        {
            return gtNewIconNode(0, type);
        }

        case TYP_LONG:
        {
            return gtNewLconNode(0);
        }

        case TYP_FLOAT:
        case TYP_DOUBLE:
        {
            return gtNewDconNode(0.0, type);
        }

        default:
        {
            unreached();
        }
    }
}

GenTree* Compiler::gtNewOneConNode(var_types type, var_types simdBaseType /* = TYP_UNDEF */)
{
#if defined(FEATURE_SIMD)
    if (varTypeIsSIMD(type))
    {
        GenTreeVecCon* one = gtNewVconNode(type);

        unsigned simdSize   = genTypeSize(type);
        uint32_t simdLength = getSIMDVectorLength(simdSize, simdBaseType);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.u8[index] = 1;
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.u16[index] = 1;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.u32[index] = 1;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.u64[index] = 1;
                }
                break;
            }

            case TYP_FLOAT:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.f32[index] = 1.0f;
                }
                break;
            }

            case TYP_DOUBLE:
            {
                for (uint32_t index = 0; index < simdLength; index++)
                {
                    one->gtSimdVal.f64[index] = 1.0;
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return one;
    }
#endif // FEATURE_SIMD

    switch (type)
    {
        case TYP_INT:
        case TYP_UINT:
        {
            return gtNewIconNode(1);
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            return gtNewLconNode(1);
        }

        case TYP_FLOAT:
        case TYP_DOUBLE:
        {
            return gtNewDconNode(1.0, type);
        }

        default:
        {
            unreached();
        }
    }
}

//------------------------------------------------------------------------
// gtNewGenericCon:
//   Create an IR node representing a constant value of any type.
//
// Parameters:
//   type   - The primitive type. For small types the constant will be
//            zero/sign-extended and a TYP_INT node will be returned.
//   cnsVal - Pointer to data
//
// Returns:
//   An IR node representing the constant.
//
GenTree* Compiler::gtNewGenericCon(var_types type, uint8_t* cnsVal)
{
    switch (type)
    {
#define READ_VALUE(typ)                                                                                                \
    typ val;                                                                                                           \
    memcpy(&val, cnsVal, sizeof(typ));

        case TYP_BYTE:
        {
            READ_VALUE(int8_t);
            return gtNewIconNode(val);
        }
        case TYP_UBYTE:
        {
            READ_VALUE(uint8_t);
            return gtNewIconNode(val);
        }
        case TYP_SHORT:
        {
            READ_VALUE(int16_t);
            return gtNewIconNode(val);
        }
        case TYP_USHORT:
        {
            READ_VALUE(uint16_t);
            return gtNewIconNode(val);
        }
        case TYP_INT:
        {
            READ_VALUE(int32_t);
            return gtNewIconNode(val);
        }
        case TYP_LONG:
        {
            READ_VALUE(int64_t);
            return gtNewLconNode(val);
        }
        case TYP_FLOAT:
        {
            READ_VALUE(float);
            return gtNewDconNodeF(val);
        }
        case TYP_DOUBLE:
        {
            READ_VALUE(double);
            return gtNewDconNodeD(val);
        }
        case TYP_REF:
        {
            READ_VALUE(ssize_t);
            if (val == 0)
            {
                return gtNewNull();
            }
            else
            {
                // Even if the caller doesn't need the resulting tree let's still conservatively call
                // setMethodHasFrozenObjects here to make caller's life easier.
                setMethodHasFrozenObjects();
                GenTree* tree = gtNewIconEmbHndNode((void*)val, nullptr, GTF_ICON_OBJ_HDL, nullptr);
                return tree;
            }
        }
#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        case TYP_SIMD64:
#endif // TARGET_XARCH
        {
            return gtNewVconNode(type, cnsVal);
        }
#endif // FEATURE_SIMD
        default:
            unreached();

#undef READ_VALUE
    }
}

//------------------------------------------------------------------------
// CreateInitValue:
//   Create an IR node representing a constant value with the specified 8
//   byte character broadcast into all of its bytes.
//
// Parameters:
//   type    - The primitive type. For small types the constant will be
//             zero/sign-extended and a TYP_INT node will be returned.
//   pattern - A byte pattern.
//
// Returns:
//   An IR node representing the constant.
//
// Remarks:
//   Should only be called when that pattern can actually be represented; for
//   example, GC pointers only support an init pattern of zero.
//
GenTree* Compiler::gtNewConWithPattern(var_types type, uint8_t pattern)
{
    switch (type)
    {
        case TYP_UBYTE:
            return gtNewIconNode(pattern);
        case TYP_BYTE:
            return gtNewIconNode((int8_t)pattern);
        case TYP_SHORT:
            return gtNewIconNode((int16_t)(pattern * 0x0101));
        case TYP_USHORT:
            return gtNewIconNode((uint16_t)(pattern * 0x0101));
        case TYP_INT:
            return gtNewIconNode(pattern * 0x01010101);
        case TYP_LONG:
            return gtNewLconNode(pattern * 0x0101010101010101LL);
        case TYP_FLOAT:
            float floatPattern;
            memset(&floatPattern, pattern, sizeof(floatPattern));
            return gtNewDconNodeF(floatPattern);
        case TYP_DOUBLE:
            double doublePattern;
            memset(&doublePattern, pattern, sizeof(doublePattern));
            return gtNewDconNodeD(doublePattern);
        case TYP_REF:
        case TYP_BYREF:
            assert(pattern == 0);
            return gtNewZeroConNode(type);
#ifdef FEATURE_SIMD
        case TYP_SIMD8:
        case TYP_SIMD12:
        case TYP_SIMD16:
#if defined(TARGET_XARCH)
        case TYP_SIMD32:
        case TYP_SIMD64:
#endif // TARGET_XARCH
#endif // FEATURE_SIMD
        {
            GenTreeVecCon* node = gtNewVconNode(type);
            memset(&node->gtSimdVal, pattern, sizeof(node->gtSimdVal));
            return node;
        }
        default:
            unreached();
    }
}

GenTreeLclVar* Compiler::gtNewStoreLclVarNode(unsigned lclNum, GenTree* data)
{
    LclVarDsc*     varDsc = lvaGetDesc(lclNum);
    var_types      type   = varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc);
    GenTreeLclVar* store  = new (this, GT_STORE_LCL_VAR) GenTreeLclVar(type, lclNum, data);
    store->gtFlags |= (GTF_VAR_DEF | GTF_ASG);
    if (varDsc->IsAddressExposed())
    {
        store->gtFlags |= GTF_GLOB_REF;
    }

    gtInitializeStoreNode(store, data);

    return store;
}

GenTreeLclFld* Compiler::gtNewStoreLclFldNode(
    unsigned lclNum, var_types type, ClassLayout* layout, unsigned offset, GenTree* data)
{
    assert((type == TYP_STRUCT) == (layout != nullptr));

    GenTreeLclFld* store = new (this, GT_STORE_LCL_FLD) GenTreeLclFld(type, lclNum, offset, data, layout);
    store->gtFlags |= (GTF_VAR_DEF | GTF_ASG);
    if (store->IsPartialLclFld(this))
    {
        store->gtFlags |= GTF_VAR_USEASG;
    }
    if (lvaGetDesc(lclNum)->IsAddressExposed())
    {
        store->gtFlags |= GTF_GLOB_REF;
    }

    gtInitializeStoreNode(store, data);

    return store;
}

GenTreeCall* Compiler::gtNewIndCallNode(GenTree* addr, var_types type, const DebugInfo& di)
{
    return gtNewCallNode(CT_INDIRECT, (CORINFO_METHOD_HANDLE)addr, type, di);
}

GenTreeCall* Compiler::gtNewCallNode(gtCallTypes           callType,
                                     CORINFO_METHOD_HANDLE callHnd,
                                     var_types             type,
                                     const DebugInfo&      di)
{
    GenTreeCall* node = new (this, GT_CALL) GenTreeCall(genActualType(type));

    node->gtFlags |= (GTF_CALL | GTF_GLOB_REF);
#ifdef UNIX_X86_ABI
    if (callType == CT_INDIRECT || callType == CT_HELPER)
        node->gtFlags |= GTF_CALL_POP_ARGS;
#endif // UNIX_X86_ABI
    node->gtCallType    = callType;
    node->gtCallMethHnd = callHnd;
    INDEBUG(node->callSig = nullptr;)
    node->tailCallInfo    = nullptr;
    node->gtRetClsHnd     = nullptr;
    node->gtControlExpr   = nullptr;
    node->gtCallMoreFlags = GTF_CALL_M_EMPTY;
    INDEBUG(node->gtCallDebugFlags = GTF_CALL_MD_EMPTY);
    node->gtInlineInfoCount = 0;

    if (callType == CT_INDIRECT)
    {
        node->gtCallCookie = nullptr;
    }
    else
    {
        node->ClearInlineInfo();
    }
    node->gtReturnType = type;

#ifdef FEATURE_READYTORUN
    node->gtEntryPoint.addr       = nullptr;
    node->gtEntryPoint.accessType = IAT_VALUE;
#endif

#if defined(DEBUG)
    // These get updated after call node is built.
    node->gtInlineObservation = InlineObservation::CALLEE_UNUSED_INITIAL;
    node->gtRawILOffset       = BAD_IL_OFFSET;
    node->gtInlineContext     = compInlineContext;
#endif

    // Spec: Managed Retval sequence points needs to be generated while generating debug info for debuggable code.
    //
    // Implementation note: if not generating MRV info genCallSite2ILOffsetMap will be NULL and
    // codegen will pass DebugInfo() to emitter, which will cause emitter
    // not to emit IP mapping entry.
    if (opts.compDbgCode && opts.compDbgInfo && di.IsValid())
    {
        // Managed Retval - IL offset of the call.  This offset is used to emit a
        // CALL_INSTRUCTION type sequence point while emitting corresponding native call.
        //
        // TODO-Cleanup:
        // a) (Opt) We need not store this offset if the method doesn't return a
        // value.  Rather it can be made BAD_IL_OFFSET to prevent a sequence
        // point being emitted.
        //
        // b) (Opt) Add new sequence points only if requested by debugger through
        // a new boundary type - ICorDebugInfo::BoundaryTypes
        if (genCallSite2DebugInfoMap == nullptr)
        {
            genCallSite2DebugInfoMap = new (getAllocator()) CallSiteDebugInfoTable(getAllocator());
        }

        // Make sure that there are no duplicate entries for a given call node
        assert(!genCallSite2DebugInfoMap->Lookup(node));
        genCallSite2DebugInfoMap->Set(node, di);
    }

    // Initialize gtOtherRegs
    node->ClearOtherRegs();

    // Initialize spill flags of gtOtherRegs
    node->ClearOtherRegFlags();

#if !defined(TARGET_64BIT)
    if (varTypeIsLong(node))
    {
        assert(node->gtReturnType == node->gtType);
        // Initialize Return type descriptor of call node
        node->InitializeLongReturnType();
    }
#endif // !defined(TARGET_64BIT)

    return node;
}

GenTreeLclVar* Compiler::gtNewLclvNode(unsigned lnum, var_types type DEBUGARG(IL_OFFSET offs))
{
    assert(type != TYP_VOID);
    // We need to ensure that all struct values are normalized.
    // It might be nice to assert this in general, but we have assignments of int to long.
    if (varTypeIsStruct(type))
    {
        // Make an exception for implicit by-ref parameters during global morph, since
        // their lvType has been updated to byref but their appearances have not yet all
        // been rewritten and so may have struct type still.
        LclVarDsc* varDsc = lvaGetDesc(lnum);

        bool simd12ToSimd16Widening = false;
#if FEATURE_SIMD
        // We can additionally have a SIMD12 that was widened to a SIMD16, generally as part of lowering
        simd12ToSimd16Widening = (type == TYP_SIMD16) && (varDsc->lvType == TYP_SIMD12);
#endif
        assert((type == varDsc->lvType) || simd12ToSimd16Widening ||
               (lvaIsImplicitByRefLocal(lnum) && fgGlobalMorph && (varDsc->lvType == TYP_BYREF)));
    }
    GenTreeLclVar* node = new (this, GT_LCL_VAR) GenTreeLclVar(GT_LCL_VAR, type, lnum DEBUGARG(offs));

    /* Cannot have this assert because the inliner uses this function
     * to add temporaries */

    // assert(lnum < lvaCount);

    return node;
}

GenTreeLclVar* Compiler::gtNewLclVarNode(unsigned lclNum, var_types type)
{
    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    if (type == TYP_UNDEF)
    {
        type = varDsc->lvNormalizeOnLoad() ? varDsc->TypeGet() : genActualType(varDsc);
    }

    GenTreeLclVar* lclVar = gtNewLclvNode(lclNum, type);
    if (varDsc->IsAddressExposed())
    {
        lclVar->gtFlags |= GTF_GLOB_REF;
    }

    return lclVar;
}

GenTreeLclVar* Compiler::gtNewLclLNode(unsigned lnum, var_types type DEBUGARG(IL_OFFSET offs))
{
    // We need to ensure that all struct values are normalized.
    // It might be nice to assert this in general, but we have assignments of int to long.
    if (varTypeIsStruct(type))
    {
        // Make an exception for implicit by-ref parameters during global morph, since
        // their lvType has been updated to byref but their appearances have not yet all
        // been rewritten and so may have struct type still.
        assert(type == lvaTable[lnum].lvType ||
               (lvaIsImplicitByRefLocal(lnum) && fgGlobalMorph && (lvaTable[lnum].lvType == TYP_BYREF)));
    }
    // This local variable node may later get transformed into a large node
    assert(GenTree::s_gtNodeSizes[LargeOpOpcode()] > GenTree::s_gtNodeSizes[GT_LCL_VAR]);
    GenTreeLclVar* node =
        new (this, LargeOpOpcode()) GenTreeLclVar(GT_LCL_VAR, type, lnum DEBUGARG(offs) DEBUGARG(/*largeNode*/ true));
    return node;
}

GenTreeLclFld* Compiler::gtNewLclVarAddrNode(unsigned lclNum, var_types type)
{
    GenTreeLclFld* node = gtNewLclAddrNode(lclNum, 0, type);
    return node;
}

GenTreeLclFld* Compiler::gtNewLclAddrNode(unsigned lclNum, unsigned lclOffs, var_types type)
{
    GenTreeLclFld* node = new (this, GT_LCL_ADDR) GenTreeLclFld(GT_LCL_ADDR, type, lclNum, lclOffs);
    return node;
}

GenTreeConditional* Compiler::gtNewConditionalNode(
    genTreeOps oper, GenTree* cond, GenTree* op1, GenTree* op2, var_types type)
{
    assert(GenTree::OperIsConditional(oper));
    GenTreeConditional* node = new (this, oper) GenTreeConditional(oper, type, cond, op1, op2);
    node->gtFlags |= (cond->gtFlags & GTF_ALL_EFFECT);
    node->gtFlags |= (op1->gtFlags & GTF_ALL_EFFECT);
    node->gtFlags |= (op2->gtFlags & GTF_ALL_EFFECT);
    return node;
}

GenTreeLclFld* Compiler::gtNewLclFldNode(unsigned lnum, var_types type, unsigned offset)
{
    GenTreeLclFld* node = new (this, GT_LCL_FLD) GenTreeLclFld(GT_LCL_FLD, type, lnum, offset, nullptr);
    return node;
}

GenTreeRetExpr* Compiler::gtNewInlineCandidateReturnExpr(GenTreeCall* inlineCandidate, var_types type)
{
    assert(GenTree::s_gtNodeSizes[GT_RET_EXPR] == TREE_NODE_SZ_LARGE);

    GenTreeRetExpr* node = new (this, GT_RET_EXPR) GenTreeRetExpr(type);

    node->gtInlineCandidate = inlineCandidate;

    node->gtSubstExpr = nullptr;
    node->gtSubstBB   = nullptr;

    // GT_RET_EXPR node eventually might be turned back into GT_CALL (when inlining is aborted for example).
    // Therefore it should carry the GTF_CALL flag so that all the rules about spilling can apply to it as well.
    // For example, impImportLeave or CEE_POP need to spill GT_RET_EXPR before empty the evaluation stack.
    node->gtFlags |= GTF_CALL;

    return node;
}

//------------------------------------------------------------------------
// gtNewFieldAddrNode: Create a new GT_FIELD_ADDR node.
//
// TODO-ADDR: consider creating a variant of this which would skip various
// no-op constructs (such as struct fields with zero offsets), and fold
// others (LCL_VAR_ADDR + FIELD_ADDR => LCL_FLD_ADDR).
//
// Arguments:
//    type   - type for the address node
//    fldHnd - the field handle
//    obj    - the instance, an address
//    offset - the field offset
//
// Return Value:
//    The created node.
//
GenTreeFieldAddr* Compiler::gtNewFieldAddrNode(var_types type, CORINFO_FIELD_HANDLE fldHnd, GenTree* obj, DWORD offset)
{
    assert(varTypeIsI(genActualType(type)));

    GenTreeFieldAddr* fieldNode = new (this, GT_FIELD_ADDR) GenTreeFieldAddr(type, obj, fldHnd, offset);

    // If "obj" is the address of a local, note that a field of that struct local has been accessed.
    if ((obj != nullptr) && obj->IsLclVarAddr())
    {
        LclVarDsc* varDsc       = lvaGetDesc(obj->AsLclVarCommon());
        varDsc->lvFieldAccessed = 1;
    }

    if ((obj != nullptr) && fgAddrCouldBeNull(obj))
    {
        fieldNode->gtFlags |= GTF_EXCEPT;
    }

    return fieldNode;
}

//------------------------------------------------------------------------
// gtInitializeStoreNode: Initialize a store node.
//
// Common initialization for all STORE nodes. Marks SIMD locals as "used in
// a HW intrinsic".
//
// Arguments:
//    store - The store node
//    data  - The value to store
//
void Compiler::gtInitializeStoreNode(GenTree* store, GenTree* data)
{
    // TODO-ASG: add asserts that the types match here.
    assert(store->Data() == data);

#if defined(FEATURE_SIMD)
#ifndef TARGET_X86
    if (varTypeIsSIMD(store))
    {
        // TODO-ASG: delete this zero-diff quirk.
        if (!data->IsCall() || !data->AsCall()->ShouldHaveRetBufArg())
        {
            // We want to track SIMD assignments as being intrinsics since they
            // are functionally SIMD `mov` instructions and are more efficient
            // when we don't promote, particularly when it occurs due to inlining.
            SetOpLclRelatedToSIMDIntrinsic(store);
            SetOpLclRelatedToSIMDIntrinsic(data);
        }
    }
#else  // TARGET_X86
    // TODO-Cleanup: merge into the all-arch.
    if (varTypeIsSIMD(data) && data->OperIs(GT_HWINTRINSIC, GT_CNS_VEC))
    {
        SetOpLclRelatedToSIMDIntrinsic(store);
    }
#endif // TARGET_X86
#endif // FEATURE_SIMD
}

//------------------------------------------------------------------------
// gtInitializeIndirNode: Initialize an indirection node.
//
// Sets side effect flags based on the passed-in indirection flags.
//
// Arguments:
//    indir      - The indirection node
//    indirFlags - The indirection flags
//
void Compiler::gtInitializeIndirNode(GenTreeIndir* indir, GenTreeFlags indirFlags)
{
    assert(varTypeIsI(genActualType(indir->Addr())));
    assert((indirFlags & ~GTF_IND_FLAGS) == GTF_EMPTY);

    indir->gtFlags |= indirFlags;
    indir->SetIndirExceptionFlags(this);

    if ((indirFlags & GTF_IND_INVARIANT) == 0)
    {
        indir->gtFlags |= GTF_GLOB_REF;
    }
    if ((indirFlags & GTF_IND_VOLATILE) != 0)
    {
        indir->SetHasOrderingSideEffect();
    }
}

//------------------------------------------------------------------------
// gtNewBlkIndir: Create a struct indirection node.
//
// Arguments:
//    layout     - The struct layout
//    addr       - Address of the indirection
//    indirFlags - Indirection flags
//
// Return Value:
//    The created GT_BLK node.
//
GenTreeBlk* Compiler::gtNewBlkIndir(ClassLayout* layout, GenTree* addr, GenTreeFlags indirFlags)
{
    assert(layout->GetType() == TYP_STRUCT);
    GenTreeBlk* blkNode = new (this, GT_BLK) GenTreeBlk(GT_BLK, TYP_STRUCT, addr, layout);
    gtInitializeIndirNode(blkNode, indirFlags);

    return blkNode;
}

//------------------------------------------------------------------------------
// gtNewIndir : Create an indirection node.
//
// Arguments:
//    typ        - Type of the node
//    addr       - Address of the indirection
//    indirFlags - Indirection flags
//
// Return Value:
//    The created GT_IND node.
//
GenTreeIndir* Compiler::gtNewIndir(var_types typ, GenTree* addr, GenTreeFlags indirFlags)
{
    GenTreeIndir* indir = new (this, GT_IND) GenTreeIndir(GT_IND, typ, addr, nullptr);
    gtInitializeIndirNode(indir, indirFlags);

    return indir;
}

//------------------------------------------------------------------------
// gtNewLoadValueNode: Return a node that represents a loaded value.
//
// Arguments:
//    type       - Type to load
//    layout     - Struct layout to load
//    addr       - The address
//    indirFlags - Indirection flags
//
// Return Value:
//    A "BLK/IND" node, or "LCL_VAR" if "addr" points to a compatible local.
//
GenTree* Compiler::gtNewLoadValueNode(var_types type, ClassLayout* layout, GenTree* addr, GenTreeFlags indirFlags)
{
    assert(((indirFlags & ~GTF_IND_FLAGS) == 0) && ((type != TYP_STRUCT) || (layout != nullptr)));

    if (((indirFlags & GTF_IND_VOLATILE) == 0) && addr->IsLclVarAddr())
    {
        unsigned   lclNum = addr->AsLclFld()->GetLclNum();
        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        if ((varDsc->TypeGet() == type) &&
            ((type != TYP_STRUCT) || ClassLayout::AreCompatible(layout, varDsc->GetLayout())))
        {
            return gtNewLclvNode(lclNum, type);
        }
    }

    return (type == TYP_STRUCT) ? gtNewBlkIndir(layout, addr, indirFlags) : gtNewIndir(type, addr, indirFlags);
}

//------------------------------------------------------------------------------
// gtNewStoreBlkNode : Create an indirect struct store node.
//
// Arguments:
//    layout     - The struct layout
//    addr       - Destination address
//    data       - Value to store
//    indirFlags - Indirection flags
//
// Return Value:
//    The created GT_STORE_BLK node.
//
GenTreeBlk* Compiler::gtNewStoreBlkNode(ClassLayout* layout, GenTree* addr, GenTree* data, GenTreeFlags indirFlags)
{
    assert((indirFlags & GTF_IND_INVARIANT) == 0);
    assert(data->IsInitVal() || ClassLayout::AreCompatible(layout, data->GetLayout(this)));

    GenTreeBlk* store = new (this, GT_STORE_BLK) GenTreeBlk(GT_STORE_BLK, TYP_STRUCT, addr, data, layout);
    store->gtFlags |= GTF_ASG;
    gtInitializeIndirNode(store, indirFlags);
    gtInitializeStoreNode(store, data);

    return store;
}

//------------------------------------------------------------------------------
// gtNewStoreDynBlkNode : Create a dynamic block store node.
//
// Arguments:
//    addr        - Destination address
//    data        - Value to store (init val or indirection representing a location)
//    dynamicSize - Node that computes number of bytes to store
//    indirFlags  - Indirection flags
//
// Return Value:
//    The created GT_STORE_DYN_BLK node.
//
GenTreeStoreDynBlk* Compiler::gtNewStoreDynBlkNode(GenTree*     addr,
                                                   GenTree*     data,
                                                   GenTree*     dynamicSize,
                                                   GenTreeFlags indirFlags)
{
    assert((indirFlags & GTF_IND_INVARIANT) == 0);
    assert(data->IsInitVal() || data->OperIs(GT_IND));

    GenTreeStoreDynBlk* store = new (this, GT_STORE_DYN_BLK) GenTreeStoreDynBlk(addr, data, dynamicSize);
    store->gtFlags |= GTF_ASG;
    gtInitializeIndirNode(store, indirFlags);
    gtInitializeStoreNode(store, data);

    return store;
}

//------------------------------------------------------------------------------
// gtNewStoreIndNode : Create an indirect store node.
//
// Arguments:
//    type       - Type of the store
//    addr       - Destionation address
//    data       - Value to store
//    indirFlags - Indirection flags
//
// Return Value:
//    The created GT_STOREIND node.
//
GenTreeStoreInd* Compiler::gtNewStoreIndNode(var_types type, GenTree* addr, GenTree* data, GenTreeFlags indirFlags)
{
    assert(((indirFlags & GTF_IND_INVARIANT) == 0) && (type != TYP_STRUCT));

    GenTreeStoreInd* store = new (this, GT_STOREIND) GenTreeStoreInd(type, addr, data);
    store->gtFlags |= GTF_ASG;
    gtInitializeIndirNode(store, indirFlags);
    gtInitializeStoreNode(store, data);

    return store;
}

//------------------------------------------------------------------------
// gtNewStoreValueNode: Return a node that represents a store.
//
// Arguments:
//    type       - Type to store
//    layout     - Struct layout for the store
//    addr       - Destination address
//    data       - Value to store
//    indirFlags - Indirection flags
//
// Return Value:
//    A "STORE_BLK/STORE_IND" node, or "STORE_LCL_VAR" if "addr" points to
//    a compatible local.
//
GenTree* Compiler::gtNewStoreValueNode(
    var_types type, ClassLayout* layout, GenTree* addr, GenTree* data, GenTreeFlags indirFlags)
{
    assert((type != TYP_STRUCT) || (layout != nullptr));

    if (((indirFlags & GTF_IND_VOLATILE) == 0) && addr->IsLclVarAddr())
    {
        unsigned   lclNum = addr->AsLclFld()->GetLclNum();
        LclVarDsc* varDsc = lvaGetDesc(lclNum);
        if ((varDsc->TypeGet() == type) &&
            ((type != TYP_STRUCT) || ClassLayout::AreCompatible(layout, varDsc->GetLayout())))
        {
            return gtNewStoreLclVarNode(lclNum, data);
        }
    }

    GenTree* store;
    if (type == TYP_STRUCT)
    {
        store = gtNewStoreBlkNode(layout, addr, data, indirFlags);
    }
    else
    {
        store = gtNewStoreIndNode(type, addr, data, indirFlags);
    }

    return store;
}

//------------------------------------------------------------------------
// gtNewAtomicNode: Create a new atomic operation node.
//
// Arguments:
//    oper      - The atomic oper
//    type      - Type to store/load
//    addr      - Destination ("location") address
//    value     - Value
//    comparand - Comparand value for a CMPXCHG
//
// Return Value:
//    The created node.
//
GenTree* Compiler::gtNewAtomicNode(genTreeOps oper, var_types type, GenTree* addr, GenTree* value, GenTree* comparand)
{
    assert(GenTree::OperIsAtomicOp(oper) && ((oper == GT_CMPXCHG) == (comparand != nullptr)));
    GenTreeIndir* node;
    if (comparand != nullptr)
    {
        node = new (this, GT_CMPXCHG) GenTreeCmpXchg(type, addr, value, comparand);
        addr->gtFlags |= GTF_DONT_CSE;
    }
    else
    {
        node = new (this, oper) GenTreeIndir(oper, type, addr, value);
    }

    node->AddAllEffectsFlags(GTF_ASG); // All atomics are opaque global stores.
    gtInitializeIndirNode(node, GTF_EMPTY);

    return node;
}

//------------------------------------------------------------------------
// FixupInitBlkValue: Fixup the init value for an initBlk operation
//
// Arguments:
//    type - The type of store that the initBlk is being transformed into
//
// Return Value:
//    Modifies the constant value on this node to be the appropriate "fill"
//    value for the initblk.
//
// Notes:
//    The initBlk MSIL instruction takes a byte value, which must be
//    extended to the size of the assignment when an initBlk is transformed
//    to an assignment of a primitive type.
//    This performs the appropriate extension.
//
void GenTreeIntCon::FixupInitBlkValue(var_types type)
{
    assert(varTypeIsIntegralOrI(type));
    unsigned size = genTypeSize(type);
    if (size > 1)
    {
        size_t cns = gtIconVal;
        cns        = cns & 0xFF;
        cns |= cns << 8;
        if (size >= 4)
        {
            cns |= cns << 16;
#ifdef TARGET_64BIT
            if (size == 8)
            {
                cns |= cns << 32;
            }
#endif // TARGET_64BIT

            // Make the type match for evaluation types.
            gtType = type;

            // if we are initializing a GC type the value being assigned must be zero (null).
            assert(!varTypeIsGC(type) || (cns == 0));
        }

        gtIconVal = cns;
    }
}

//----------------------------------------------------------------------------
// UsesDivideByConstOptimized:
//    returns true if rationalize will use the division by constant
//    optimization for this node.
//
// Arguments:
//    this - a GenTreeOp node
//    comp - the compiler instance
//
// Return Value:
//    Return true iff the node is a GT_DIV,GT_UDIV, GT_MOD or GT_UMOD with
//    an integer constant and we can perform the division operation using
//    a reciprocal multiply or a shift operation.
//
bool GenTreeOp::UsesDivideByConstOptimized(Compiler* comp)
{
    if (!comp->opts.OptimizationEnabled())
    {
        return false;
    }

    if (!OperIs(GT_DIV, GT_MOD, GT_UDIV, GT_UMOD))
    {
        return false;
    }
#if defined(TARGET_ARM64)
    if (OperIs(GT_MOD, GT_UMOD))
    {
        // MOD, UMOD not supported for ARM64
        return false;
    }
#endif // TARGET_ARM64

    bool     isSignedDivide = OperIs(GT_DIV, GT_MOD);
    GenTree* dividend       = gtGetOp1()->gtEffectiveVal();
    GenTree* divisor        = gtGetOp2()->gtEffectiveVal();

#if !defined(TARGET_64BIT)
    if (dividend->OperIs(GT_LONG))
    {
        return false;
    }
#endif

    if (dividend->IsCnsIntOrI())
    {
        // We shouldn't see a divmod with constant operands here but if we do then it's likely
        // because optimizations are disabled or it's a case that's supposed to throw an exception.
        // Don't optimize this.
        return false;
    }

    ssize_t divisorValue;
    if (divisor->IsCnsIntOrI())
    {
        divisorValue = static_cast<ssize_t>(divisor->AsIntCon()->IconValue());
    }
    else
    {
        ValueNum vn = divisor->gtVNPair.GetLiberal();
        if (comp->vnStore && comp->vnStore->IsVNConstant(vn))
        {
            divisorValue = comp->vnStore->CoercedConstantValue<ssize_t>(vn);
        }
        else
        {
            return false;
        }
    }

    const var_types divType = TypeGet();

    if (divisorValue == 0)
    {
        // x / 0 and x % 0 can't be optimized because they are required to throw an exception.
        return false;
    }
    else if (isSignedDivide)
    {
        if (divisorValue == -1)
        {
            // x / -1 can't be optimized because INT_MIN / -1 is required to throw an exception.
            return false;
        }
        else if (isPow2(divisorValue))
        {
            return true;
        }
    }
    else // unsigned divide
    {
        if (divType == TYP_INT)
        {
            // Clear up the upper 32 bits of the value, they may be set to 1 because constants
            // are treated as signed and stored in ssize_t which is 64 bit in size on 64 bit targets.
            divisorValue &= UINT32_MAX;
        }

        size_t unsignedDivisorValue = (size_t)divisorValue;
        if (isPow2(unsignedDivisorValue))
        {
            return true;
        }
    }

    const bool isDiv = OperIs(GT_DIV, GT_UDIV);

    if (isDiv)
    {
        if (isSignedDivide)
        {
            // If the divisor is the minimum representable integer value then the result is either 0 or 1
            if ((divType == TYP_INT && divisorValue == INT_MIN) || (divType == TYP_LONG && divisorValue == INT64_MIN))
            {
                return true;
            }
        }
        else
        {
            // If the divisor is greater or equal than 2^(N - 1) then the result is either 0 or 1
            if (((divType == TYP_INT) && ((UINT32)divisorValue > (UINT32_MAX / 2))) ||
                ((divType == TYP_LONG) && ((UINT64)divisorValue > (UINT64_MAX / 2))))
            {
                return true;
            }
        }
    }

// TODO-ARM-CQ: Currently there's no GT_MULHI for ARM32
#if defined(TARGET_XARCH) || defined(TARGET_ARM64) || defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    if (!comp->opts.MinOpts() && ((divisorValue >= 3) || !isSignedDivide))
    {
        // All checks pass we can perform the division operation using a reciprocal multiply.
        return true;
    }
#endif

    return false;
}

//------------------------------------------------------------------------
// CheckDivideByConstOptimized:
//      Checks if we can use the division by constant optimization
//      on this node
//      and if so sets the flag GTF_DIV_BY_CNS_OPT and
//      set GTF_DONT_CSE on the constant node
//
// Arguments:
//    this       - a GenTreeOp node
//    comp       - the compiler instance
//
void GenTreeOp::CheckDivideByConstOptimized(Compiler* comp)
{
    if (UsesDivideByConstOptimized(comp))
    {
        // Now set DONT_CSE on the GT_CNS_INT divisor, note that
        // with ValueNumbering we can have a non GT_CNS_INT divisor
        GenTree* divisor = gtGetOp2()->gtEffectiveVal();
        if (divisor->OperIs(GT_CNS_INT))
        {
            divisor->gtFlags |= GTF_DONT_CSE;
        }
    }
}

//------------------------------------------------------------------------
// gtNewPutArgReg: Creates a new PutArgReg node.
//
// Arguments:
//    type   - The actual type of the argument
//    arg    - The argument node
//    argReg - The register that the argument will be passed in
//
// Return Value:
//    Returns the newly created PutArgReg node.
//
// Notes:
//    The node is generated as GenTreeMultiRegOp on RyuJIT/armel, GenTreeOp on all the other archs.
//
GenTree* Compiler::gtNewPutArgReg(var_types type, GenTree* arg, regNumber argReg)
{
    assert(arg != nullptr);

    GenTree* node = nullptr;
#if defined(TARGET_ARM)
    // A PUTARG_REG could be a MultiRegOp on arm since we could move a double register to two int registers.
    node = new (this, GT_PUTARG_REG) GenTreeMultiRegOp(GT_PUTARG_REG, type, arg, nullptr);
    if (type == TYP_LONG)
    {
        node->AsMultiRegOp()->gtOtherReg = REG_NEXT(argReg);
    }
#else
    node          = gtNewOperNode(GT_PUTARG_REG, type, arg);
#endif
    node->SetRegNum(argReg);

    return node;
}

//------------------------------------------------------------------------
// gtNewBitCastNode: Creates a new BitCast node.
//
// Arguments:
//    type   - The actual type of the argument
//    arg    - The argument node
//    argReg - The register that the argument will be passed in
//
// Return Value:
//    Returns the newly created BitCast node.
//
// Notes:
//    The node is generated as GenTreeMultiRegOp on RyuJIT/arm, as GenTreeOp on all the other archs.
//
GenTree* Compiler::gtNewBitCastNode(var_types type, GenTree* arg)
{
    assert(arg != nullptr);
    assert(type != TYP_STRUCT);

    GenTree* node = nullptr;
#if defined(TARGET_ARM)
    // A BITCAST could be a MultiRegOp on arm since we could move a double register to two int registers.
    node = new (this, GT_BITCAST) GenTreeMultiRegOp(GT_BITCAST, type, arg, nullptr);
#else
    node          = gtNewOperNode(GT_BITCAST, type, arg);
#endif

    return node;
}

//------------------------------------------------------------------------
// gtNewAllocObjNode: Helper to create an object allocation node.
//
// Arguments:
//    pResolvedToken   - Resolved token for the object being allocated
//    useParent     -    true iff the token represents a child of the object's class
//
// Return Value:
//    Returns GT_ALLOCOBJ node that will be later morphed into an
//    allocation helper call or local variable allocation on the stack.
//
//    Node creation can fail for inlinees when the type described by pResolvedToken
//    can't be represented in jitted code. If this happens, this method will return
//    nullptr.
//
GenTreeAllocObj* Compiler::gtNewAllocObjNode(CORINFO_RESOLVED_TOKEN* pResolvedToken, bool useParent)
{
    const bool      mustRestoreHandle     = true;
    bool* const     pRuntimeLookup        = nullptr;
    bool            usingReadyToRunHelper = false;
    CorInfoHelpFunc helper                = CORINFO_HELP_UNDEF;
    GenTree*        opHandle = impTokenToHandle(pResolvedToken, pRuntimeLookup, mustRestoreHandle, useParent);

#ifdef FEATURE_READYTORUN
    CORINFO_CONST_LOOKUP lookup = {};

    if (opts.IsReadyToRun())
    {
        helper                                        = CORINFO_HELP_READYTORUN_NEW;
        CORINFO_LOOKUP_KIND* const pGenericLookupKind = nullptr;
        usingReadyToRunHelper =
            info.compCompHnd->getReadyToRunHelper(pResolvedToken, pGenericLookupKind, helper, &lookup);
    }
#endif

    if (!usingReadyToRunHelper)
    {
        if (opHandle == nullptr)
        {
            // We must be backing out of an inline.
            assert(compDonotInline());
            return nullptr;
        }
    }

    bool            helperHasSideEffects;
    CorInfoHelpFunc helperTemp = info.compCompHnd->getNewHelper(pResolvedToken->hClass, &helperHasSideEffects);

    if (!usingReadyToRunHelper)
    {
        helper = helperTemp;
    }

    // TODO: ReadyToRun: When generic dictionary lookups are necessary, replace the lookup call
    // and the newfast call with a single call to a dynamic R2R cell that will:
    //      1) Load the context
    //      2) Perform the generic dictionary lookup and caching, and generate the appropriate stub
    //      3) Allocate and return the new object for boxing
    // Reason: performance (today, we'll always use the slow helper for the R2R generics case)

    GenTreeAllocObj* allocObj =
        gtNewAllocObjNode(helper, helperHasSideEffects, pResolvedToken->hClass, TYP_REF, opHandle);

#ifdef FEATURE_READYTORUN
    if (usingReadyToRunHelper)
    {
        assert(lookup.addr != nullptr);
        allocObj->gtEntryPoint = lookup;
    }
#endif

    return allocObj;
}

/*****************************************************************************
 *
 *  Clones the given tree value and returns a copy of the given tree.
 *  If 'complexOK' is false, the cloning is only done provided the tree
 *     is not too complex (whatever that may mean);
 *  If 'complexOK' is true, we try slightly harder to clone the tree.
 *  In either case, NULL is returned if the tree cannot be cloned
 *
 *  Note that there is the function gtCloneExpr() which does a more
 *  complete job if you can't handle this function failing.
 */

GenTree* Compiler::gtClone(GenTree* tree, bool complexOK)
{
    GenTree* copy;

    switch (tree->gtOper)
    {
        case GT_CNS_INT:

#if defined(LATE_DISASM)
            if (tree->IsIconHandle())
            {
                copy = gtNewIconHandleNode(tree->AsIntCon()->gtIconVal, tree->gtFlags, tree->AsIntCon()->gtFieldSeq);
                copy->AsIntCon()->gtCompileTimeHandle = tree->AsIntCon()->gtCompileTimeHandle;
                copy->gtType                          = tree->gtType;
            }
            else
#endif
            {
                copy = new (this, GT_CNS_INT)
                    GenTreeIntCon(tree->gtType, tree->AsIntCon()->gtIconVal, tree->AsIntCon()->gtFieldSeq);
                copy->AsIntCon()->gtCompileTimeHandle = tree->AsIntCon()->gtCompileTimeHandle;
            }
            break;

        case GT_CNS_LNG:
            copy = gtNewLconNode(tree->AsLngCon()->gtLconVal);
            break;

        case GT_CNS_DBL:
        {
            copy = gtNewDconNode(tree->AsDblCon()->DconValue(), tree->TypeGet());
            break;
        }

        case GT_CNS_VEC:
        {
            GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
            vecCon->gtSimdVal     = tree->AsVecCon()->gtSimdVal;
            copy                  = vecCon;
            break;
        }

        case GT_FTN_ADDR:
        {
            copy = new (this, tree->OperGet()) GenTreeFptrVal(tree->TypeGet(), tree->AsFptrVal()->gtFptrMethod);
            copy->AsFptrVal()->gtFptrDelegateTarget = tree->AsFptrVal()->gtFptrDelegateTarget;

#ifdef FEATURE_READYTORUN
            copy->AsFptrVal()->gtEntryPoint = tree->AsFptrVal()->gtEntryPoint;
#endif
            break;
        }

        case GT_LCL_VAR:
            copy = new (this, tree->OperGet())
                GenTreeLclVar(tree->OperGet(), tree->TypeGet(), tree->AsLclVar()->GetLclNum());
            goto FINISH_CLONING_LCL_NODE;

        case GT_LCL_ADDR:
            if (!complexOK && (tree->AsLclFld()->GetLclOffs() == 0))
            {
                return nullptr;
            }
            FALLTHROUGH;

        case GT_LCL_FLD:
            copy = new (this, tree->OperGet())
                GenTreeLclFld(tree->OperGet(), tree->TypeGet(), tree->AsLclFld()->GetLclNum(),
                              tree->AsLclFld()->GetLclOffs(), tree->AsLclFld()->GetLayout());
            goto FINISH_CLONING_LCL_NODE;

        FINISH_CLONING_LCL_NODE:
            // Remember that the local node has been cloned. Below the flag will be set on 'copy' too.
            tree->gtFlags |= GTF_VAR_MOREUSES;
            copy->AsLclVarCommon()->SetSsaNum(tree->AsLclVarCommon()->GetSsaNum());
            assert(!copy->AsLclVarCommon()->HasSsaName() || ((copy->gtFlags & GTF_VAR_DEF) == 0));
            break;

        default:
            if (!complexOK)
            {
                return nullptr;
            }

            if (tree->OperIs(GT_IND, GT_BLK) && tree->AsIndir()->Addr()->OperIs(GT_FIELD_ADDR))
            {
                GenTree*          objp = nullptr;
                GenTreeFieldAddr* addr = tree->AsIndir()->Addr()->AsFieldAddr();

                if (addr->IsInstance())
                {
                    objp = gtClone(addr->GetFldObj(), false);
                    if (objp == nullptr)
                    {
                        return nullptr;
                    }
                }

                copy = gtNewFieldAddrNode(addr->TypeGet(), addr->gtFldHnd, objp, addr->gtFldOffset);
                copy->AsFieldAddr()->gtFldMayOverlap = addr->gtFldMayOverlap;
                copy->AsFieldAddr()->SetIsSpanLength(addr->IsSpanLength());
#ifdef FEATURE_READYTORUN
                copy->AsFieldAddr()->gtFieldLookup = addr->gtFieldLookup;
#endif
                copy = tree->OperIs(GT_BLK) ? gtNewBlkIndir(tree->AsBlk()->GetLayout(), copy)
                                            : gtNewIndir(tree->TypeGet(), copy);
                impAnnotateFieldIndir(copy->AsIndir());
            }
            else if (tree->OperIs(GT_ADD, GT_SUB))
            {
                GenTree* op1 = tree->AsOp()->gtOp1;
                GenTree* op2 = tree->AsOp()->gtOp2;

                if (op1->OperIsLeaf() && op2->OperIsLeaf())
                {
                    op1 = gtClone(op1);
                    if (op1 == nullptr)
                    {
                        return nullptr;
                    }
                    op2 = gtClone(op2);
                    if (op2 == nullptr)
                    {
                        return nullptr;
                    }

                    copy = gtNewOperNode(tree->OperGet(), tree->TypeGet(), op1, op2);
                }
                else
                {
                    return nullptr;
                }
            }
            else
            {
                return nullptr;
            }

            break;
    }

    copy->gtFlags |= tree->gtFlags & ~GTF_NODE_MASK;
#if defined(DEBUG)
    copy->gtDebugFlags |= tree->gtDebugFlags & ~GTF_DEBUG_NODE_MASK;
#endif // defined(DEBUG)

    return copy;
}

//------------------------------------------------------------------------
// gtCloneExpr: Create a copy of `tree`
//
// Arguments:
//    tree - GenTree to create a copy of
//
// Return Value:
//    A copy of the given tree.

GenTree* Compiler::gtCloneExpr(GenTree* tree)
{
    if (tree == nullptr)
    {
        return nullptr;
    }

    /* Figure out what kind of a node we have */

    genTreeOps oper = tree->OperGet();
    unsigned   kind = tree->OperKind();
    GenTree*   copy;

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
            case GT_CNS_INT:

#if defined(LATE_DISASM)
                if (tree->IsIconHandle())
                {
                    copy =
                        gtNewIconHandleNode(tree->AsIntCon()->gtIconVal, tree->gtFlags, tree->AsIntCon()->gtFieldSeq);
                    copy->AsIntCon()->gtCompileTimeHandle = tree->AsIntCon()->gtCompileTimeHandle;
                    copy->gtType                          = tree->gtType;
                }
                else
#endif
                {
                    copy = gtNewIconNode(tree->AsIntCon()->gtIconVal, tree->gtType);
#ifdef DEBUG
                    copy->AsIntCon()->gtTargetHandle = tree->AsIntCon()->gtTargetHandle;
#endif
                    copy->AsIntCon()->gtCompileTimeHandle = tree->AsIntCon()->gtCompileTimeHandle;
                    copy->AsIntCon()->gtFieldSeq          = tree->AsIntCon()->gtFieldSeq;
                }
                goto DONE;

            case GT_CNS_LNG:
                copy = gtNewLconNode(tree->AsLngCon()->gtLconVal);
                goto DONE;

            case GT_CNS_DBL:
            {
                copy = gtNewDconNode(tree->AsDblCon()->DconValue(), tree->TypeGet());
                goto DONE;
            }

            case GT_CNS_STR:
                copy = gtNewSconNode(tree->AsStrCon()->gtSconCPX, tree->AsStrCon()->gtScpHnd);
                goto DONE;

            case GT_CNS_VEC:
            {
                GenTreeVecCon* vecCon = gtNewVconNode(tree->TypeGet());
                vecCon->gtSimdVal     = tree->AsVecCon()->gtSimdVal;
                copy                  = vecCon;
                goto DONE;
            }

            case GT_LCL_VAR:

                // Remember that the local node has been cloned. The flag will be set on 'copy' as well.
                tree->gtFlags |= GTF_VAR_MOREUSES;
                copy =
                    gtNewLclvNode(tree->AsLclVar()->GetLclNum(), tree->gtType DEBUGARG(tree->AsLclVar()->gtLclILoffs));
                copy->AsLclVarCommon()->SetSsaNum(tree->AsLclVarCommon()->GetSsaNum());
                goto DONE;

            case GT_LCL_FLD:
                // Remember that the local node has been cloned. The flag will be set on 'copy' as well.
                tree->gtFlags |= GTF_VAR_MOREUSES;
                copy =
                    new (this, GT_LCL_FLD) GenTreeLclFld(GT_LCL_FLD, tree->TypeGet(), tree->AsLclFld()->GetLclNum(),
                                                         tree->AsLclFld()->GetLclOffs(), tree->AsLclFld()->GetLayout());
                copy->AsLclFld()->SetSsaNum(tree->AsLclFld()->GetSsaNum());
                goto DONE;

            case GT_RET_EXPR:
                // GT_RET_EXPR is unique node, that contains a link to a gtInlineCandidate node,
                // that is part of another statement. We cannot clone both here and cannot
                // create another GT_RET_EXPR that points to the same gtInlineCandidate.
                NO_WAY("Cloning of GT_RET_EXPR node not supported");
                goto DONE;

            case GT_MEMORYBARRIER:
                copy = new (this, GT_MEMORYBARRIER) GenTree(GT_MEMORYBARRIER, TYP_VOID);
                goto DONE;

            case GT_FTN_ADDR:
                copy = new (this, oper) GenTreeFptrVal(tree->gtType, tree->AsFptrVal()->gtFptrMethod);
                copy->AsFptrVal()->gtFptrDelegateTarget = tree->AsFptrVal()->gtFptrDelegateTarget;

#ifdef FEATURE_READYTORUN
                copy->AsFptrVal()->gtEntryPoint = tree->AsFptrVal()->gtEntryPoint;
#endif
                goto DONE;

            case GT_CATCH_ARG:
            case GT_NO_OP:
            case GT_NOP:
            case GT_LABEL:
                copy = new (this, oper) GenTree(oper, tree->gtType);
                goto DONE;

#if !defined(FEATURE_EH_FUNCLETS)
            case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            case GT_JMP:
                copy = new (this, oper) GenTreeVal(oper, tree->gtType, tree->AsVal()->gtVal1);
                goto DONE;

            case GT_LCL_ADDR:
            {
                copy = new (this, oper)
                    GenTreeLclFld(oper, tree->TypeGet(), tree->AsLclFld()->GetLclNum(), tree->AsLclFld()->GetLclOffs());
                goto DONE;
            }

            default:
                NO_WAY("Cloning of node not supported");
                goto DONE;
        }
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        /* If necessary, make sure we allocate a "fat" tree node */
        CLANG_FORMAT_COMMENT_ANCHOR;

        switch (oper)
        {
            case GT_STORE_LCL_VAR:
                // Remember that the local node has been cloned. The flag will be set on 'copy' as well.
                tree->gtFlags |= GTF_VAR_MOREUSES;
                copy = new (this, GT_STORE_LCL_VAR)
                    GenTreeLclVar(tree->TypeGet(), tree->AsLclVar()->GetLclNum(), tree->AsLclVar()->Data());
                break;

            case GT_STORE_LCL_FLD:
                // Remember that the local node has been cloned. The flag will be set on 'copy' as well.
                tree->gtFlags |= GTF_VAR_MOREUSES;
                copy = new (this, GT_STORE_LCL_FLD)
                    GenTreeLclFld(tree->TypeGet(), tree->AsLclFld()->GetLclNum(), tree->AsLclFld()->GetLclOffs(),
                                  tree->AsLclFld()->Data(), tree->AsLclFld()->GetLayout());
                break;

            /* These nodes sometimes get bashed to "fat" ones */

            case GT_MUL:
            case GT_DIV:
            case GT_MOD:

            case GT_UDIV:
            case GT_UMOD:

                //  In the implementation of gtNewLargeOperNode you have
                //  to give an oper that will create a small node,
                //  otherwise it asserts.
                //
                if (GenTree::s_gtNodeSizes[oper] == TREE_NODE_SZ_SMALL)
                {
                    copy = gtNewLargeOperNode(oper, tree->TypeGet(), tree->AsOp()->gtOp1,
                                              tree->OperIsBinary() ? tree->AsOp()->gtOp2 : nullptr);
                }
                else // Always a large tree
                {
                    if (tree->OperIsBinary())
                    {
                        copy = gtNewOperNode(oper, tree->TypeGet(), tree->AsOp()->gtOp1, tree->AsOp()->gtOp2);
                    }
                    else
                    {
                        copy = gtNewOperNode(oper, tree->TypeGet(), tree->AsOp()->gtOp1);
                    }
                }
                break;

            case GT_CAST:
                copy = new (this, LargeOpOpcode())
                    GenTreeCast(tree->TypeGet(), tree->AsCast()->CastOp(), tree->IsUnsigned(),
                                tree->AsCast()->gtCastType DEBUGARG(/*largeNode*/ TRUE));
                break;

            case GT_INDEX_ADDR:
            {
                GenTreeIndexAddr* asIndAddr = tree->AsIndexAddr();

                copy = new (this, GT_INDEX_ADDR)
                    GenTreeIndexAddr(asIndAddr->Arr(), asIndAddr->Index(), asIndAddr->gtElemType,
                                     asIndAddr->gtStructElemClass, asIndAddr->gtElemSize, asIndAddr->gtLenOffset,
                                     asIndAddr->gtElemOffset, asIndAddr->IsBoundsChecked());
                copy->AsIndexAddr()->gtIndRngFailBB = asIndAddr->gtIndRngFailBB;
            }
            break;

            case GT_ALLOCOBJ:
            {
                GenTreeAllocObj* asAllocObj = tree->AsAllocObj();
                copy                        = new (this, GT_ALLOCOBJ)
                    GenTreeAllocObj(tree->TypeGet(), asAllocObj->gtNewHelper, asAllocObj->gtHelperHasSideEffects,
                                    asAllocObj->gtAllocObjClsHnd, asAllocObj->gtOp1);
#ifdef FEATURE_READYTORUN
                copy->AsAllocObj()->gtEntryPoint = asAllocObj->gtEntryPoint;
#endif
            }
            break;

            case GT_RUNTIMELOOKUP:
            {
                GenTreeRuntimeLookup* asRuntimeLookup = tree->AsRuntimeLookup();

                copy = new (this, GT_RUNTIMELOOKUP)
                    GenTreeRuntimeLookup(asRuntimeLookup->gtHnd, asRuntimeLookup->gtHndType, asRuntimeLookup->gtOp1);
            }
            break;

            case GT_STOREIND:
                copy = new (this, GT_STOREIND)
                    GenTreeStoreInd(tree->TypeGet(), tree->AsIndir()->Addr(), tree->AsIndir()->Data());
                copy->AsStoreInd()->SetRMWStatus(tree->AsStoreInd()->GetRMWStatus());
                break;

            case GT_STORE_BLK:
                copy = new (this, GT_STORE_BLK) GenTreeBlk(GT_STORE_BLK, tree->TypeGet(), tree->AsBlk()->Addr(),
                                                           tree->AsBlk()->Data(), tree->AsBlk()->GetLayout());
                break;

            case GT_ARR_ADDR:
                copy = new (this, GT_ARR_ADDR)
                    GenTreeArrAddr(tree->AsArrAddr()->Addr(), tree->AsArrAddr()->GetElemType(),
                                   tree->AsArrAddr()->GetElemClassHandle(), tree->AsArrAddr()->GetFirstElemOffset());
                break;

            case GT_ARR_LENGTH:
                copy =
                    gtNewArrLen(tree->TypeGet(), tree->AsArrLen()->ArrRef(), tree->AsArrLen()->ArrLenOffset(), nullptr);
                break;

            case GT_MDARR_LENGTH:
                copy =
                    gtNewMDArrLen(tree->AsMDArr()->ArrRef(), tree->AsMDArr()->Dim(), tree->AsMDArr()->Rank(), nullptr);
                break;

            case GT_MDARR_LOWER_BOUND:
                copy = gtNewMDArrLowerBound(tree->AsMDArr()->ArrRef(), tree->AsMDArr()->Dim(), tree->AsMDArr()->Rank(),
                                            nullptr);
                break;

            case GT_QMARK:
                copy = new (this, GT_QMARK)
                    GenTreeQmark(tree->TypeGet(), tree->AsOp()->gtGetOp1(), tree->AsOp()->gtGetOp2()->AsColon(),
                                 tree->AsQmark()->ThenNodeLikelihood());
                break;

            case GT_BLK:
                copy = new (this, GT_BLK)
                    GenTreeBlk(GT_BLK, tree->TypeGet(), tree->AsBlk()->Addr(), tree->AsBlk()->GetLayout());
                break;

            case GT_FIELD_ADDR:
                copy = new (this, GT_FIELD_ADDR)
                    GenTreeFieldAddr(tree->TypeGet(), tree->AsFieldAddr()->GetFldObj(), tree->AsFieldAddr()->gtFldHnd,
                                     tree->AsFieldAddr()->gtFldOffset);
                copy->AsFieldAddr()->gtFldMayOverlap = tree->AsFieldAddr()->gtFldMayOverlap;
#ifdef FEATURE_READYTORUN
                copy->AsFieldAddr()->gtFieldLookup = tree->AsFieldAddr()->gtFieldLookup;
#endif
                copy->AsFieldAddr()->SetIsSpanLength(tree->AsFieldAddr()->IsSpanLength());
                break;

            case GT_BOX:
                copy = new (this, GT_BOX)
                    GenTreeBox(tree->TypeGet(), tree->AsOp()->gtOp1, tree->AsBox()->gtDefStmtWhenInlinedBoxValue,
                               tree->AsBox()->gtCopyStmtWhenInlinedBoxValue);
                tree->AsBox()->SetCloned();
                copy->AsBox()->SetCloned();
                break;

            case GT_INTRINSIC:
                copy = new (this, GT_INTRINSIC)
                    GenTreeIntrinsic(tree->TypeGet(), tree->AsOp()->gtOp1, tree->AsOp()->gtOp2,
                                     tree->AsIntrinsic()->gtIntrinsicName, tree->AsIntrinsic()->gtMethodHandle);
#ifdef FEATURE_READYTORUN
                copy->AsIntrinsic()->gtEntryPoint = tree->AsIntrinsic()->gtEntryPoint;
#endif
                break;

            case GT_BOUNDS_CHECK:
                copy = new (this, GT_BOUNDS_CHECK)
                    GenTreeBoundsChk(tree->AsBoundsChk()->GetIndex(), tree->AsBoundsChk()->GetArrayLength(),
                                     tree->AsBoundsChk()->gtThrowKind);
                copy->AsBoundsChk()->gtIndRngFailBB = tree->AsBoundsChk()->gtIndRngFailBB;
                copy->AsBoundsChk()->gtInxType      = tree->AsBoundsChk()->gtInxType;
                break;

            case GT_LEA:
            {
                GenTreeAddrMode* addrModeOp = tree->AsAddrMode();
                copy                        = new (this, GT_LEA)
                    GenTreeAddrMode(addrModeOp->TypeGet(), addrModeOp->Base(), addrModeOp->Index(), addrModeOp->gtScale,
                                    static_cast<unsigned>(addrModeOp->Offset()));
            }
            break;

            case GT_COPY:
            case GT_RELOAD:
            {
                copy = new (this, oper) GenTreeCopyOrReload(oper, tree->TypeGet(), tree->gtGetOp1());
            }
            break;

            default:
                assert(!GenTree::IsExOp(tree->OperKind()) && tree->OperIsSimple());
                // We're in the SimpleOp case, so it's always unary or binary.
                if (GenTree::OperIsUnary(tree->OperGet()))
                {
                    copy = gtNewOperNode(oper, tree->TypeGet(), tree->AsOp()->gtOp1);
                }
                else
                {
                    assert(GenTree::OperIsBinary(tree->OperGet()));
                    copy = gtNewOperNode(oper, tree->TypeGet(), tree->AsOp()->gtOp1, tree->AsOp()->gtOp2);
                }
                break;
        }

        if (tree->AsOp()->gtOp1)
        {
            copy->AsOp()->gtOp1 = gtCloneExpr(tree->AsOp()->gtOp1);
        }

        if (tree->gtGetOp2IfPresent())
        {
            copy->AsOp()->gtOp2 = gtCloneExpr(tree->AsOp()->gtOp2);
        }

        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_CALL:

            // We can't safely clone calls that have GT_RET_EXPRs via gtCloneExpr.
            // You must use gtCloneCandidateCall for these calls (and then do appropriate other fixup)
            if (tree->AsCall()->IsInlineCandidate() || tree->AsCall()->IsGuardedDevirtualizationCandidate())
            {
                NO_WAY("Cloning of calls with associated GT_RET_EXPR nodes is not supported");
            }

            copy = gtCloneExprCallHelper(tree->AsCall());
            break;

#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            copy = new (this, GT_HWINTRINSIC)
                GenTreeHWIntrinsic(tree->TypeGet(), IntrinsicNodeBuilder(getAllocator(CMK_ASTNode), tree->AsMultiOp()),
                                   tree->AsHWIntrinsic()->GetHWIntrinsicId(),
                                   tree->AsHWIntrinsic()->GetSimdBaseJitType(), tree->AsHWIntrinsic()->GetSimdSize());
            copy->AsHWIntrinsic()->SetAuxiliaryJitType(tree->AsHWIntrinsic()->GetAuxiliaryJitType());
            goto CLONE_MULTIOP_OPERANDS;
#endif
#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
        CLONE_MULTIOP_OPERANDS:
            for (GenTree** use : copy->AsMultiOp()->UseEdges())
            {
                *use = gtCloneExpr(*use);
            }
            break;
#endif

        case GT_ARR_ELEM:
        {
            GenTreeArrElem* arrElem = tree->AsArrElem();
            GenTree*        inds[GT_ARR_MAX_RANK];
            for (unsigned dim = 0; dim < arrElem->gtArrRank; dim++)
            {
                inds[dim] = gtCloneExpr(arrElem->gtArrInds[dim]);
            }
            copy = new (this, GT_ARR_ELEM) GenTreeArrElem(arrElem->TypeGet(), gtCloneExpr(arrElem->gtArrObj),
                                                          arrElem->gtArrRank, arrElem->gtArrElemSize, &inds[0]);
        }
        break;

        case GT_PHI:
        {
            copy                      = new (this, GT_PHI) GenTreePhi(tree->TypeGet());
            GenTreePhi::Use** prevUse = &copy->AsPhi()->gtUses;
            for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
            {
                *prevUse = new (this, CMK_ASTNode) GenTreePhi::Use(gtCloneExpr(use.GetNode()), *prevUse);
                prevUse  = &((*prevUse)->NextRef());
            }
        }
        break;

        case GT_FIELD_LIST:
            copy = new (this, GT_FIELD_LIST) GenTreeFieldList();
            for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
            {
                copy->AsFieldList()->AddField(this, gtCloneExpr(use.GetNode()), use.GetOffset(), use.GetType());
            }
            break;

        case GT_CMPXCHG:
            copy = new (this, GT_CMPXCHG)
                GenTreeCmpXchg(tree->TypeGet(), gtCloneExpr(tree->AsCmpXchg()->Addr()),
                               gtCloneExpr(tree->AsCmpXchg()->Data()), gtCloneExpr(tree->AsCmpXchg()->Comparand()));
            break;

        case GT_STORE_DYN_BLK:
            copy = new (this, oper) GenTreeStoreDynBlk(gtCloneExpr(tree->AsStoreDynBlk()->Addr()),
                                                       gtCloneExpr(tree->AsStoreDynBlk()->Data()),
                                                       gtCloneExpr(tree->AsStoreDynBlk()->gtDynamicSize));
            break;

        case GT_SELECT:
            copy =
                new (this, oper) GenTreeConditional(oper, tree->TypeGet(), gtCloneExpr(tree->AsConditional()->gtCond),
                                                    gtCloneExpr(tree->AsConditional()->gtOp1),
                                                    gtCloneExpr(tree->AsConditional()->gtOp2));
            break;
        default:
#ifdef DEBUG
            gtDispTree(tree);
#endif
            NO_WAY("unexpected operator");
    }

DONE:
    assert(copy->gtOper == oper);

    copy->gtVNPair = tree->gtVNPair; // A cloned tree gets the original's Value number pair
    copy->gtFlags  = tree->gtFlags;

#if defined(DEBUG)
    // Non-node debug flags should be propagated from 'tree' to 'copy'
    copy->gtDebugFlags |= (tree->gtDebugFlags & ~GTF_DEBUG_NODE_MASK);
#endif

    /* Make sure to copy back fields that may have been initialized */

    copy->CopyRawCosts(tree);
    copy->gtRsvdRegs = tree->gtRsvdRegs;
    copy->CopyReg(tree);
    return copy;
}

//------------------------------------------------------------------------
// InternalCopyFrom:
//   Copy all information from the specified `CallArgs`, making these argument
//   lists equivalent. Nodes are cloned using the specified function.
//
// Remarks:
//   This function should not be used directly. Instead, use `gtCloneExpr` on
//   the call node.
//
template <typename CopyNodeFunc>
void CallArgs::InternalCopyFrom(Compiler* comp, CallArgs* other, CopyNodeFunc copyNode)
{
    assert((m_head == nullptr) && (m_lateHead == nullptr));

    m_nextStackByteOffset      = other->m_nextStackByteOffset;
    m_hasThisPointer           = other->m_hasThisPointer;
    m_hasRetBuffer             = other->m_hasRetBuffer;
    m_isVarArgs                = other->m_isVarArgs;
    m_abiInformationDetermined = other->m_abiInformationDetermined;
    m_hasRegArgs               = other->m_hasRegArgs;
    m_hasStackArgs             = other->m_hasStackArgs;
    m_argsComplete             = other->m_argsComplete;
    m_needsTemps               = other->m_needsTemps;

    // Unix x86 flags related to stack alignment intentionally not copied as
    // they depend on where the call will be inserted.

    CallArg** tail = &m_head;
    for (CallArg& arg : other->Args())
    {
        CallArg* carg           = new (comp, CMK_CallArgs) CallArg();
        carg->m_earlyNode       = arg.m_earlyNode != nullptr ? copyNode(arg.m_earlyNode) : nullptr;
        carg->m_lateNode        = arg.m_lateNode != nullptr ? copyNode(arg.m_lateNode) : nullptr;
        carg->m_signatureClsHnd = arg.m_signatureClsHnd;
        carg->m_tmpNum          = arg.m_tmpNum;
        carg->m_signatureType   = arg.m_signatureType;
        carg->m_wellKnownArg    = arg.m_wellKnownArg;
        carg->m_needTmp         = arg.m_needTmp;
        carg->m_needPlace       = arg.m_needPlace;
        carg->m_isTmp           = arg.m_isTmp;
        carg->m_processed       = arg.m_processed;
        carg->AbiInfo           = arg.AbiInfo;
        *tail                   = carg;
        tail                    = &carg->m_next;
    }

    // Now copy late pointers. Note that these may not come in order.
    tail = &m_lateHead;
    for (CallArg& arg : other->LateArgs())
    {
        CallArg* it      = m_head;
        CallArg* otherIt = other->m_head;
        while (otherIt != &arg)
        {
            assert(it != nullptr && otherIt != nullptr);
            it      = it->m_next;
            otherIt = otherIt->m_next;
        }

        *tail = it;
        tail  = &it->m_lateNext;
    }
}

//------------------------------------------------------------------------
// gtCloneExprCallHelper: clone a call tree
//
// Notes:
//    Do not invoke this method directly, instead call either gtCloneExpr
//    or gtCloneCandidateCall, as appropriate.
//
// Arguments:
//    tree - the call to clone
//
// Returns:
//    Cloned copy of call and all subtrees.

GenTreeCall* Compiler::gtCloneExprCallHelper(GenTreeCall* tree)
{
    GenTreeCall* copy = new (this, GT_CALL) GenTreeCall(tree->TypeGet());

    copy->gtCallMoreFlags = tree->gtCallMoreFlags;
    INDEBUG(copy->gtCallDebugFlags = tree->gtCallDebugFlags);

    copy->gtArgs.InternalCopyFrom(this, &tree->gtArgs, [=](GenTree* node) { return gtCloneExpr(node); });

    // The call sig comes from the EE and doesn't change throughout the compilation process, meaning
    // we only really need one physical copy of it. Therefore a shallow pointer copy will suffice.
    // (Note that this still holds even if the tree we are cloning was created by an inlinee compiler,
    // because the inlinee still uses the inliner's memory allocator anyway.)
    INDEBUG(copy->callSig = tree->callSig;)

    // The tail call info does not change after it is allocated, so for the same reasons as above
    // a shallow copy suffices.
    copy->tailCallInfo = tree->tailCallInfo;

    copy->gtRetClsHnd        = tree->gtRetClsHnd;
    copy->gtControlExpr      = gtCloneExpr(tree->gtControlExpr);
    copy->gtStubCallStubAddr = tree->gtStubCallStubAddr;

    /* Copy the union */
    if (tree->gtCallType == CT_INDIRECT)
    {
        copy->gtCallCookie = tree->gtCallCookie ? gtCloneExpr(tree->gtCallCookie) : nullptr;
        copy->gtCallAddr   = tree->gtCallAddr ? gtCloneExpr(tree->gtCallAddr) : nullptr;
    }
    else
    {
        copy->gtCallMethHnd         = tree->gtCallMethHnd;
        copy->gtInlineCandidateInfo = tree->gtInlineCandidateInfo;
        copy->gtInlineInfoCount     = tree->gtInlineInfoCount;
    }

    copy->gtCallType   = tree->gtCallType;
    copy->gtReturnType = tree->gtReturnType;

#if FEATURE_MULTIREG_RET
    copy->gtReturnTypeDesc = tree->gtReturnTypeDesc;
#endif

#ifdef FEATURE_READYTORUN
    copy->setEntryPoint(tree->gtEntryPoint);
#endif

#if defined(DEBUG)
    copy->gtInlineObservation = tree->gtInlineObservation;
    copy->gtRawILOffset       = tree->gtRawILOffset;
    copy->gtInlineContext     = tree->gtInlineContext;
#endif

    copy->CopyOtherRegFlags(tree);

    // We keep track of the number of no return calls, so if we've cloned
    // one of these, update the tracking.
    //
    if (tree->IsNoReturn())
    {
        assert(copy->IsNoReturn());
        setMethodHasNoReturnCalls();
    }

    return copy;
}

//------------------------------------------------------------------------
// gtCloneCandidateCall: clone a call that is an inline or guarded
//    devirtualization candidate (~ any call that can have a GT_RET_EXPR)
//
// Notes:
//    If the call really is a candidate, the caller must take additional steps
//    after cloning to re-establish candidate info and the relationship between
//    the candidate and any associated GT_RET_EXPR.
//
// Arguments:
//    call - the call to clone
//
// Returns:
//    Cloned copy of call and all subtrees.

GenTreeCall* Compiler::gtCloneCandidateCall(GenTreeCall* call)
{
    assert(call->IsInlineCandidate() || call->IsGuardedDevirtualizationCandidate());

    GenTreeCall* result = gtCloneExprCallHelper(call);

    // There is some common post-processing in gtCloneExpr that we reproduce
    // here, for the fields that make sense for candidate calls.
    result->gtFlags |= call->gtFlags;

#if defined(DEBUG)
    result->gtDebugFlags |= (call->gtDebugFlags & ~GTF_DEBUG_NODE_MASK);
#endif

    result->CopyReg(call);

    return result;
}

//------------------------------------------------------------------------
// gtUpdateSideEffects: Update the side effects of a tree and its ancestors
//
// Arguments:
//    stmt            - The tree's statement
//    tree            - Tree to update the side effects for
//
// Note: If tree's order hasn't been established, the method updates side effect
//       flags on all statement's nodes.

void Compiler::gtUpdateSideEffects(Statement* stmt, GenTree* tree)
{
    if (fgNodeThreading == NodeThreading::AllTrees)
    {
        gtUpdateTreeAncestorsSideEffects(tree);
    }
    else
    {
        assert(fgNodeThreading != NodeThreading::LIR);
        gtUpdateStmtSideEffects(stmt);
    }
}

//------------------------------------------------------------------------
// gtUpdateTreeAncestorsSideEffects: Update the side effects of a tree and its ancestors
//                                   when statement order has been established.
//
// Arguments:
//    tree            - Tree to update the side effects for
//
void Compiler::gtUpdateTreeAncestorsSideEffects(GenTree* tree)
{
    assert(fgNodeThreading == NodeThreading::AllTrees);
    while (tree != nullptr)
    {
        gtUpdateNodeSideEffects(tree);
        tree = tree->gtGetParent(nullptr);
    }
}

//------------------------------------------------------------------------
// gtUpdateStmtSideEffects: Update the side effects for statement tree nodes.
//
// Arguments:
//    stmt            - The statement to update side effects on
//
void Compiler::gtUpdateStmtSideEffects(Statement* stmt)
{
    struct UpdateSideEffectsWalker : GenTreeVisitor<UpdateSideEffectsWalker>
    {
        enum
        {
            DoPreOrder  = true,
            DoPostOrder = true,
        };

        UpdateSideEffectsWalker(Compiler* comp) : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            tree->gtFlags &= ~(GTF_ASG | GTF_CALL | GTF_EXCEPT);
            return WALK_CONTINUE;
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;

            // Update the node's side effects first.
            if (tree->OperMayThrow(m_compiler))
            {
                tree->gtFlags |= GTF_EXCEPT;
            }

            if (tree->OperRequiresAsgFlag())
            {
                tree->gtFlags |= GTF_ASG;
            }

            if (tree->OperRequiresCallFlag(m_compiler))
            {
                tree->gtFlags |= GTF_CALL;
            }

            // If this node is an indir or array meta-data load, and it doesn't have the GTF_EXCEPT bit set, we
            // set the GTF_IND_NONFAULTING bit. This needs to be done after all children, and this node, have
            // been processed.
            if (tree->OperIsIndirOrArrMetaData() && ((tree->gtFlags & GTF_EXCEPT) == 0))
            {
                tree->gtFlags |= GTF_IND_NONFAULTING;
            }

            // Then update the parent's side effects based on this node.
            if (user != nullptr)
            {
                user->gtFlags |= (tree->gtFlags & GTF_ALL_EFFECT);
            }
            return WALK_CONTINUE;
        }
    };

    UpdateSideEffectsWalker walker(this);
    walker.WalkTree(stmt->GetRootNodePointer(), nullptr);
}

//------------------------------------------------------------------------
// gtUpdateNodeOperSideEffects: Update the side effects based on the node operation.
//
// Arguments:
//    tree            - Tree to update the side effects on
//
// Notes:
//    This method currently only updates GTF_EXCEPT, GTF_ASG, and GTF_CALL flags.
//    The other side effect flags may remain unnecessarily (conservatively) set.
//    The caller of this method is expected to update the flags based on the children's flags.
//
void Compiler::gtUpdateNodeOperSideEffects(GenTree* tree)
{
    if (tree->OperMayThrow(this))
    {
        tree->gtFlags |= GTF_EXCEPT;
    }
    else
    {
        tree->gtFlags &= ~GTF_EXCEPT;
        if (tree->OperIsIndirOrArrMetaData())
        {
            tree->gtFlags |= GTF_IND_NONFAULTING;
        }
    }

    if (tree->OperRequiresAsgFlag())
    {
        tree->gtFlags |= GTF_ASG;
    }
    else
    {
        tree->gtFlags &= ~GTF_ASG;
    }

    if (tree->OperRequiresCallFlag(this))
    {
        tree->gtFlags |= GTF_CALL;
    }
    else
    {
        tree->gtFlags &= ~GTF_CALL;
    }
}

//------------------------------------------------------------------------
// gtUpdateNodeSideEffects: Update the side effects based on the node operation and
//                          children's side efects.
//
// Arguments:
//    tree            - Tree to update the side effects on
//
// Notes:
//    This method currently only updates GTF_EXCEPT, GTF_ASG, and GTF_CALL flags.
//    The other side effect flags may remain unnecessarily (conservatively) set.
//
void Compiler::gtUpdateNodeSideEffects(GenTree* tree)
{
    gtUpdateNodeOperSideEffects(tree);
    tree->VisitOperands([tree](GenTree* operand) -> GenTree::VisitResult {
        tree->gtFlags |= (operand->gtFlags & GTF_ALL_EFFECT);
        return GenTree::VisitResult::Continue;
    });
}

bool GenTree::gtSetFlags() const
{
    //
    // When FEATURE_SET_FLAGS (TARGET_ARM) is active the method returns true
    //    when the gtFlags has the flag GTF_SET_FLAGS set
    // otherwise the architecture will be have instructions that typically set
    //    the flags and this method will return true.
    //
    //    Exceptions: GT_IND (load/store) is not allowed to set the flags
    //                and on XARCH the GT_MUL/GT_DIV and all overflow instructions
    //                do not set the condition flags
    //
    // Precondition we have a GTK_SMPOP
    //
    if (!varTypeIsIntegralOrI(TypeGet()) && (TypeGet() != TYP_VOID))
    {
        return false;
    }

    if (((gtFlags & GTF_SET_FLAGS) != 0) && (gtOper != GT_IND))
    {
        // GTF_SET_FLAGS is not valid on GT_IND and is overlaid with GTF_NONFAULTING_IND
        return true;
    }
    else
    {
        return false;
    }
}

bool GenTree::gtRequestSetFlags()
{
    bool result = false;

#if FEATURE_SET_FLAGS
    // This method is a Nop unless FEATURE_SET_FLAGS is defined

    // In order to set GTF_SET_FLAGS
    //              we must have a GTK_SMPOP
    //          and we have a integer or machine size type (not floating point or TYP_LONG on 32-bit)
    //
    if (!OperIsSimple())
        return false;

    if (!varTypeIsIntegralOrI(TypeGet()))
        return false;

    switch (gtOper)
    {
        case GT_IND:
        case GT_ARR_LENGTH:
        case GT_MDARR_LENGTH:
        case GT_MDARR_LOWER_BOUND:
            // These will turn into simple load from memory instructions
            // and we can't force the setting of the flags on load from memory
            break;

        case GT_MUL:
        case GT_DIV:
            // These instructions don't set the flags (on x86/x64)
            //
            break;

        default:
            // Otherwise we can set the flags for this gtOper
            // and codegen must set the condition flags.
            //
            gtFlags |= GTF_SET_FLAGS;
            result = true;
            break;
    }
#endif // FEATURE_SET_FLAGS

    // Codegen for this tree must set the condition flags if
    // this method returns true.
    //
    return result;
}

GenTreeUseEdgeIterator::GenTreeUseEdgeIterator()
    : m_advance(nullptr), m_node(nullptr), m_edge(nullptr), m_statePtr(nullptr), m_state(-1)
{
}

GenTreeUseEdgeIterator::GenTreeUseEdgeIterator(GenTree* node)
    : m_advance(nullptr), m_node(node), m_edge(nullptr), m_statePtr(nullptr), m_state(0)
{
    assert(m_node != nullptr);

    // NOTE: the switch statement below must be updated when introducing new nodes.

    switch (m_node->OperGet())
    {
        // Leaf nodes
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_ADDR:
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
            m_state = -1;
            return;

        // Standard unary operators
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        case GT_NOT:
        case GT_NEG:
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
        case GT_RUNTIMELOOKUP:
        case GT_ARR_ADDR:
        case GT_INIT_VAL:
        case GT_JTRUE:
        case GT_SWITCH:
        case GT_NULLCHECK:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
        case GT_BSWAP:
        case GT_BSWAP16:
        case GT_KEEPALIVE:
        case GT_INC_SATURATE:
#if FEATURE_ARG_SPLIT
        case GT_PUTARG_SPLIT:
#endif // FEATURE_ARG_SPLIT
        case GT_RETURNTRAP:
            m_edge = &m_node->AsUnOp()->gtOp1;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            return;

        // Unary operators with an optional operand
        case GT_FIELD_ADDR:
        case GT_RETURN:
        case GT_RETFILT:
            if (m_node->AsUnOp()->gtOp1 == nullptr)
            {
                assert(m_node->NullOp1Legal());
                m_state = -1;
            }
            else
            {
                m_edge    = &m_node->AsUnOp()->gtOp1;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
            }
            return;

// Variadic nodes
#ifdef FEATURE_HW_INTRINSICS
        case GT_HWINTRINSIC:
            SetEntryStateForMultiOp();
            return;
#endif // FEATURE_HW_INTRINSICS

        // LEA, which may have no first operand
        case GT_LEA:
            if (m_node->AsAddrMode()->gtOp1 == nullptr)
            {
                m_edge    = &m_node->AsAddrMode()->gtOp2;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
            }
            else
            {
                SetEntryStateForBinOp();
            }
            return;

        // Special nodes
        case GT_FIELD_LIST:
            m_statePtr = m_node->AsFieldList()->Uses().GetHead();
            m_advance  = &GenTreeUseEdgeIterator::AdvanceFieldList;
            AdvanceFieldList();
            return;

        case GT_PHI:
            m_statePtr = m_node->AsPhi()->gtUses;
            m_advance  = &GenTreeUseEdgeIterator::AdvancePhi;
            AdvancePhi();
            return;

        case GT_CMPXCHG:
            m_edge = &m_node->AsCmpXchg()->Addr();
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceCmpXchg;
            return;

        case GT_ARR_ELEM:
            m_edge = &m_node->AsArrElem()->gtArrObj;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceArrElem;
            return;

        case GT_STORE_DYN_BLK:
            m_edge = &m_node->AsStoreDynBlk()->Addr();
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceStoreDynBlk;
            return;

        case GT_CALL:
            m_statePtr = m_node->AsCall()->gtArgs.Args().begin().GetArg();
            m_advance  = &GenTreeUseEdgeIterator::AdvanceCall<CALL_ARGS>;
            AdvanceCall<CALL_ARGS>();
            return;
#ifdef TARGET_ARM64
        case GT_SELECT_NEG:
        case GT_SELECT_INV:
        case GT_SELECT_INC:
#endif
        case GT_SELECT:
            m_edge = &m_node->AsConditional()->gtCond;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceConditional;
            return;

        // Binary nodes
        default:
            assert(m_node->OperIsBinary());
            SetEntryStateForBinOp();
            return;
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceCmpXchg: produces the next operand of a CmpXchg node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceCmpXchg()
{
    switch (m_state)
    {
        case 0:
            m_edge  = &m_node->AsCmpXchg()->Data();
            m_state = 1;
            break;
        case 1:
            m_edge    = &m_node->AsCmpXchg()->Comparand();
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            break;
        default:
            unreached();
    }

    assert(*m_edge != nullptr);
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceArrElem: produces the next operand of a ArrElem node and advances the state.
//
// Because these nodes are variadic, this function uses `m_state` to index into the list of array indices.
//
void GenTreeUseEdgeIterator::AdvanceArrElem()
{
    if (m_state < m_node->AsArrElem()->gtArrRank)
    {
        m_edge = &m_node->AsArrElem()->gtArrInds[m_state];
        assert(*m_edge != nullptr);
        m_state++;
    }
    else
    {
        m_state = -1;
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceStoreDynBlk: produces the next operand of a StoreDynBlk node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceStoreDynBlk()
{
    GenTreeStoreDynBlk* const dynBlock = m_node->AsStoreDynBlk();
    switch (m_state)
    {
        case 0:
            m_edge  = &dynBlock->Data();
            m_state = 1;
            break;
        case 1:
            m_edge    = &dynBlock->gtDynamicSize;
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            break;
        default:
            unreached();
    }

    assert(*m_edge != nullptr);
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceFieldList: produces the next operand of a FieldList node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceFieldList()
{
    assert(m_state == 0);

    if (m_statePtr == nullptr)
    {
        m_state = -1;
    }
    else
    {
        GenTreeFieldList::Use* currentUse = static_cast<GenTreeFieldList::Use*>(m_statePtr);
        m_edge                            = &currentUse->NodeRef();
        m_statePtr                        = currentUse->GetNext();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvancePhi: produces the next operand of a Phi node and advances the state.
//
void GenTreeUseEdgeIterator::AdvancePhi()
{
    assert(m_state == 0);

    if (m_statePtr == nullptr)
    {
        m_state = -1;
    }
    else
    {
        GenTreePhi::Use* currentUse = static_cast<GenTreePhi::Use*>(m_statePtr);
        m_edge                      = &currentUse->NodeRef();
        m_statePtr                  = currentUse->GetNext();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceConditional: produces the next operand of a conditional node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceConditional()
{
    GenTreeConditional* const conditional = m_node->AsConditional();
    switch (m_state)
    {
        case 0:
            m_edge = &conditional->gtOp1;
            if (conditional->gtOp2 == nullptr)
            {
                m_advance = &GenTreeUseEdgeIterator::Terminate;
            }
            else
            {
                m_state = 1;
            }
            break;
        case 1:
            m_edge    = &conditional->gtOp2;
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            break;
        default:
            unreached();
    }

    assert(*m_edge != nullptr);
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceBinOp: produces the next operand of a binary node and advances the state.
//
// This function must be instantiated s.t. `ReverseOperands` is `true` iff the node is marked with the
// `GTF_REVERSE_OPS` flag.
//
template <bool ReverseOperands>
void           GenTreeUseEdgeIterator::AdvanceBinOp()
{
    assert(ReverseOperands == ((m_node->gtFlags & GTF_REVERSE_OPS) != 0));

    m_edge = !ReverseOperands ? &m_node->AsOp()->gtOp2 : &m_node->AsOp()->gtOp1;
    assert(*m_edge != nullptr);
    m_advance = &GenTreeUseEdgeIterator::Terminate;
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::SetEntryStateForBinOp: produces the first operand of a binary node and chooses
//                                                the appropriate advance function.
//
void GenTreeUseEdgeIterator::SetEntryStateForBinOp()
{
    assert(m_node != nullptr);
    assert(m_node->OperIsBinary());

    GenTreeOp* const node = m_node->AsOp();

    if (node->gtOp2 == nullptr)
    {
        assert(node->gtOp1 != nullptr);
        assert(node->NullOp2Legal());
        m_edge    = &node->gtOp1;
        m_advance = &GenTreeUseEdgeIterator::Terminate;
    }
    else if ((node->gtFlags & GTF_REVERSE_OPS) != 0)
    {
        m_edge    = &m_node->AsOp()->gtOp2;
        m_advance = &GenTreeUseEdgeIterator::AdvanceBinOp<true>;
    }
    else
    {
        m_edge    = &m_node->AsOp()->gtOp1;
        m_advance = &GenTreeUseEdgeIterator::AdvanceBinOp<false>;
    }
}

#if defined(FEATURE_SIMD) || defined(FEATURE_HW_INTRINSICS)
//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceMultiOp: produces the next operand of a multi-op node and advances the state.
//
// Takes advantage of the fact that GenTreeMultiOp stores the operands in a contiguous array, simply
// incrementing the "m_edge" pointer, unless the end, stored in "m_statePtr", has been reached.
//
void GenTreeUseEdgeIterator::AdvanceMultiOp()
{
    assert(m_node != nullptr);
    assert(m_node->OperIs(GT_HWINTRINSIC));

    m_edge++;
    if (m_edge == m_statePtr)
    {
        Terminate();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceReversedMultiOp: produces the next operand of a multi-op node
//                                                 marked with GTF_REVERSE_OPS and advances the state.
//
// Takes advantage of the fact that GenTreeMultiOp stores the operands in a contiguous array, simply
// decrementing the "m_edge" pointer, unless the beginning, stored in "m_statePtr", has been reached.
//
void GenTreeUseEdgeIterator::AdvanceReversedMultiOp()
{
    assert(m_node != nullptr);
    assert(m_node->OperIs(GT_HWINTRINSIC));
    assert((m_node->AsMultiOp()->GetOperandCount() == 2) && m_node->IsReverseOp());

    m_edge--;
    if (m_edge == m_statePtr)
    {
        Terminate();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::SetEntryStateForMultiOp: produces the first operand of a multi-op node and sets the
//                                                  required advance function.
//
void GenTreeUseEdgeIterator::SetEntryStateForMultiOp()
{
    size_t operandCount = m_node->AsMultiOp()->GetOperandCount();

    if (operandCount == 0)
    {
        Terminate();
    }
    else
    {
        if (m_node->IsReverseOp())
        {
            assert(operandCount == 2);

            m_edge     = m_node->AsMultiOp()->GetOperandArray() + 1;
            m_statePtr = m_node->AsMultiOp()->GetOperandArray() - 1;
            m_advance  = &GenTreeUseEdgeIterator::AdvanceReversedMultiOp;
        }
        else
        {
            m_edge     = m_node->AsMultiOp()->GetOperandArray();
            m_statePtr = m_node->AsMultiOp()->GetOperandArray(operandCount);
            m_advance  = &GenTreeUseEdgeIterator::AdvanceMultiOp;
        }
    }
}
#endif

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceCall: produces the next operand of a call node and advances the state.
//
// This function is a bit tricky: in order to avoid doing unnecessary work, it is instantiated with the
// state number the iterator will be in when it is called. For example, `AdvanceCall<CALL_INSTANCE>`
// is the instantiation used when the iterator is at the `CALL_INSTANCE` state (i.e. the entry state).
// This sort of templating allows each state to avoid processing earlier states without unnecessary
// duplication of code.
//
// Note that this method expands the argument list (`gtArgs.Args()` and `gtArgs.LateArgs()`) into their
// component operands.
//
template <int state>
void          GenTreeUseEdgeIterator::AdvanceCall()
{
    GenTreeCall* const call = m_node->AsCall();

    switch (state)
    {
        case CALL_ARGS:
            while (m_statePtr != nullptr)
            {
                CallArg* arg = static_cast<CallArg*>(m_statePtr);
                m_edge       = &arg->EarlyNodeRef();
                m_statePtr   = arg->GetNext();

                if (*m_edge != nullptr)
                {
                    return;
                }
            }
            m_statePtr = call->gtArgs.LateArgs().begin().GetArg();
            m_advance  = &GenTreeUseEdgeIterator::AdvanceCall<CALL_LATE_ARGS>;
            FALLTHROUGH;

        case CALL_LATE_ARGS:
            if (m_statePtr != nullptr)
            {
                CallArg* arg = static_cast<CallArg*>(m_statePtr);
                m_edge       = &arg->LateNodeRef();
                assert(*m_edge != nullptr);
                m_statePtr = arg->GetLateNext();
                return;
            }
            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_CONTROL_EXPR>;
            FALLTHROUGH;

        case CALL_CONTROL_EXPR:
            if (call->gtControlExpr != nullptr)
            {
                if (call->gtCallType == CT_INDIRECT)
                {
                    m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_COOKIE>;
                }
                else
                {
                    m_advance = &GenTreeUseEdgeIterator::Terminate;
                }
                m_edge = &call->gtControlExpr;
                return;
            }
            else if (call->gtCallType != CT_INDIRECT)
            {
                m_state = -1;
                return;
            }
            FALLTHROUGH;

        case CALL_COOKIE:
            assert(call->gtCallType == CT_INDIRECT);

            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_ADDRESS>;
            if (call->gtCallCookie != nullptr)
            {
                m_edge = &call->gtCallCookie;
                return;
            }
            FALLTHROUGH;

        case CALL_ADDRESS:
            assert(call->gtCallType == CT_INDIRECT);

            m_advance = &GenTreeUseEdgeIterator::Terminate;
            if (call->gtCallAddr != nullptr)
            {
                m_edge = &call->gtCallAddr;
            }
            return;

        default:
            unreached();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::Terminate: advances the iterator to the terminal state.
//
void GenTreeUseEdgeIterator::Terminate()
{
    m_state = -1;
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::operator++: advances the iterator to the next operand.
//
GenTreeUseEdgeIterator& GenTreeUseEdgeIterator::operator++()
{
    // If we've reached the terminal state, do nothing.
    if (m_state != -1)
    {
        (this->*m_advance)();
    }

    return *this;
}

GenTreeUseEdgeIterator GenTree::UseEdgesBegin()
{
    return GenTreeUseEdgeIterator(this);
}

GenTreeUseEdgeIterator GenTree::UseEdgesEnd()
{
    return GenTreeUseEdgeIterator();
}

IteratorPair<GenTreeUseEdgeIterator> GenTree::UseEdges()
{
    return MakeIteratorPair(UseEdgesBegin(), UseEdgesEnd());
}

GenTreeOperandIterator GenTree::OperandsBegin()
{
    return GenTreeOperandIterator(this);
}

GenTreeOperandIterator GenTree::OperandsEnd()
{
    return GenTreeOperandIterator();
}

IteratorPair<GenTreeOperandIterator> GenTree::Operands()
{
    return MakeIteratorPair(OperandsBegin(), OperandsEnd());
}

bool GenTree::Precedes(GenTree* other)
{
    assert(other != nullptr);

    for (GenTree* node = gtNext; node != nullptr; node = node->gtNext)
    {
        if (node == other)
        {
            return true;
        }
    }

    return false;
}

//------------------------------------------------------------------------------
// SetIndirExceptionFlags : Set GTF_EXCEPT and GTF_IND_NONFAULTING flags as appropriate
//                          on an indirection or an array length node.
//
// Arguments:
//    comp  - compiler instance
//
void GenTree::SetIndirExceptionFlags(Compiler* comp)
{
    assert(OperIsIndirOrArrMetaData() && (OperIsSimple() || OperIs(GT_CMPXCHG, GT_STORE_DYN_BLK)));

    if (IndirMayFault(comp))
    {
        gtFlags |= GTF_EXCEPT;
        return;
    }

    GenTree* addr = GetIndirOrArrMetaDataAddr();

    gtFlags |= GTF_IND_NONFAULTING;
    gtFlags &= ~GTF_EXCEPT;
    gtFlags |= addr->gtFlags & GTF_EXCEPT;
    if (OperIsBinary())
    {
        gtFlags |= gtGetOp2()->gtFlags & GTF_EXCEPT;
    }
    else if (OperIs(GT_CMPXCHG))
    {
        gtFlags |= AsCmpXchg()->Data()->gtFlags & GTF_EXCEPT;
        gtFlags |= AsCmpXchg()->Comparand()->gtFlags & GTF_EXCEPT;
    }
    else if (OperIs(GT_STORE_DYN_BLK))
    {
        gtFlags |= AsStoreDynBlk()->Data()->gtFlags & GTF_EXCEPT;
        gtFlags |= AsStoreDynBlk()->gtDynamicSize->gtFlags & GTF_EXCEPT;
    }
}

#ifdef DEBUG

/* static */ int GenTree::gtDispFlags(GenTreeFlags flags, GenTreeDebugFlags debugFlags)
{
    int charsDisplayed = 10; // the "baseline" number of flag characters displayed

    printf("%c", (flags & GTF_ASG) ? 'A' : (IsContained(flags) ? 'c' : '-'));
    printf("%c", (flags & GTF_CALL) ? 'C' : '-');
    printf("%c", (flags & GTF_EXCEPT) ? 'X' : '-');
    printf("%c", (flags & GTF_GLOB_REF) ? 'G' : '-');
    printf("%c", (debugFlags & GTF_DEBUG_NODE_MORPHED) ? '+' : // First print '+' if GTF_DEBUG_NODE_MORPHED is set
                     (flags & GTF_ORDER_SIDEEFF) ? 'O' : '-'); // otherwise print 'O' or '-'
    printf("%c", (flags & GTF_COLON_COND) ? '?' : '-');
    printf("%c", (flags & GTF_DONT_CSE) ? 'N' :           // N is for No cse
                     (flags & GTF_MAKE_CSE) ? 'H' : '-'); // H is for Hoist this expr
    printf("%c", (flags & GTF_REVERSE_OPS) ? 'R' : '-');
    printf("%c", (flags & GTF_UNSIGNED) ? 'U' : (flags & GTF_BOOLEAN) ? 'B' : '-');
#if FEATURE_SET_FLAGS
    printf("%c", (flags & GTF_SET_FLAGS) ? 'S' : '-');
    ++charsDisplayed;
#endif

    // Both GTF_SPILL and GTF_SPILLED: '#'
    //               Only GTF_SPILLED: 'z'
    //                 Only GTF_SPILL: 'Z'
    printf("%c", ((flags & (GTF_SPILL | GTF_SPILLED)) == (GTF_SPILL | GTF_SPILLED))
                     ? '#'
                     : ((flags & GTF_SPILLED) ? 'z' : ((flags & GTF_SPILL) ? 'Z' : '-')));

    return charsDisplayed;
}

/* static */
const char* GenTree::gtGetHandleKindString(GenTreeFlags flags)
{
    GenTreeFlags handleKind = flags & GTF_ICON_HDL_MASK;
    switch (handleKind)
    {
        case 0:
            return "";
        case GTF_ICON_SCOPE_HDL:
            return "GTF_ICON_SCOPE_HDL";
        case GTF_ICON_CLASS_HDL:
            return "GTF_ICON_CLASS_HDL";
        case GTF_ICON_METHOD_HDL:
            return "GTF_ICON_METHOD_HDL";
        case GTF_ICON_FIELD_HDL:
            return "GTF_ICON_FIELD_HDL";
        case GTF_ICON_STATIC_HDL:
            return "GTF_ICON_STATIC_HDL";
        case GTF_ICON_STR_HDL:
            return "GTF_ICON_STR_HDL";
        case GTF_ICON_OBJ_HDL:
            return "GTF_ICON_OBJ_HDL";
        case GTF_ICON_CONST_PTR:
            return "GTF_ICON_CONST_PTR";
        case GTF_ICON_GLOBAL_PTR:
            return "GTF_ICON_GLOBAL_PTR";
        case GTF_ICON_VARG_HDL:
            return "GTF_ICON_VARG_HDL";
        case GTF_ICON_PINVKI_HDL:
            return "GTF_ICON_PINVKI_HDL";
        case GTF_ICON_TOKEN_HDL:
            return "GTF_ICON_TOKEN_HDL";
        case GTF_ICON_TLS_HDL:
            return "GTF_ICON_TLS_HDL";
        case GTF_ICON_FTN_ADDR:
            return "GTF_ICON_FTN_ADDR";
        case GTF_ICON_CIDMID_HDL:
            return "GTF_ICON_CIDMID_HDL";
        case GTF_ICON_BBC_PTR:
            return "GTF_ICON_BBC_PTR";
        case GTF_ICON_STATIC_BOX_PTR:
            return "GTF_ICON_STATIC_BOX_PTR";
        case GTF_ICON_FIELD_SEQ:
            return "GTF_ICON_FIELD_SEQ";
        case GTF_ICON_STATIC_ADDR_PTR:
            return "GTF_ICON_STATIC_ADDR_PTR";
        case GTF_ICON_SECREL_OFFSET:
            return "GTF_ICON_SECREL_OFFSET";
        case GTF_ICON_TLSGD_OFFSET:
            return "GTF_ICON_TLSGD_OFFSET";
        default:
            return "ILLEGAL!";
    }
}

#ifdef TARGET_X86
inline const char* GetCallConvName(CorInfoCallConvExtension callConv)
{
    switch (callConv)
    {
        case CorInfoCallConvExtension::Managed:
            return "Managed";
        case CorInfoCallConvExtension::C:
            return "C";
        case CorInfoCallConvExtension::Stdcall:
            return "Stdcall";
        case CorInfoCallConvExtension::Thiscall:
            return "Thiscall";
        case CorInfoCallConvExtension::Fastcall:
            return "Fastcall";
        case CorInfoCallConvExtension::CMemberFunction:
            return "CMemberFunction";
        case CorInfoCallConvExtension::StdcallMemberFunction:
            return "StdcallMemberFunction";
        case CorInfoCallConvExtension::FastcallMemberFunction:
            return "FastcallMemberFunction";
        default:
            return "UnknownCallConv";
    }
}
#endif // TARGET_X86

/*****************************************************************************/

void Compiler::gtDispNodeName(GenTree* tree)
{
    /* print the node name */

    const char* name;

    assert(tree);
    if (tree->gtOper < GT_COUNT)
    {
        name = GenTree::OpName(tree->OperGet());
    }
    else
    {
        name = "<ERROR>";
    }
    char  buf[32];
    char* bufp = &buf[0];

    if (tree->IsIconHandle())
    {
        sprintf_s(bufp, sizeof(buf), " %s(h)%c", name, 0);
    }
    else if (tree->gtOper == GT_PUTARG_STK)
    {
        sprintf_s(bufp, sizeof(buf), " %s [+0x%02x]%c", name, tree->AsPutArgStk()->getArgOffset(), 0);
    }
    else if (tree->gtOper == GT_CALL)
    {
        const char* callType = "CALL";
        const char* gtfType  = "";
        const char* ctType   = "";
        char        gtfTypeBuf[100];

        if (tree->AsCall()->gtCallType == CT_USER_FUNC)
        {
            if (tree->AsCall()->IsVirtual())
            {
                callType = "CALLV";
            }
        }
        else if (tree->AsCall()->gtCallType == CT_HELPER)
        {
            ctType = " help";
        }
        else if (tree->AsCall()->gtCallType == CT_INDIRECT)
        {
            ctType = " ind";
        }
        else
        {
            assert(!"Unknown gtCallType");
        }

        if (tree->gtFlags & GTF_CALL_NULLCHECK)
        {
            gtfType = " nullcheck";
        }
        if (tree->AsCall()->IsVirtualVtable())
        {
            gtfType = " vt-ind";
        }
        else if (tree->AsCall()->IsVirtualStub())
        {
            gtfType = " stub";
        }
#ifdef FEATURE_READYTORUN
        else if (tree->AsCall()->IsR2RRelativeIndir())
        {
            gtfType = " r2r_ind";
        }
        else if (tree->gtFlags & GTF_TLS_GET_ADDR)
        {
            gtfType = " _tls_get_addr";
        }
#endif // FEATURE_READYTORUN
        else if (tree->gtFlags & GTF_CALL_UNMANAGED)
        {
            char* gtfTypeBufWalk = gtfTypeBuf;
            gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " unman");
            if (tree->gtFlags & GTF_CALL_POP_ARGS)
            {
                gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " popargs");
            }
#ifdef TARGET_X86
            gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " %s",
                                              GetCallConvName(tree->AsCall()->GetUnmanagedCallConv()));
#endif // TARGET_X86
            gtfType = gtfTypeBuf;
        }

        sprintf_s(bufp, sizeof(buf), " %s%s%s%c", callType, ctType, gtfType, 0);
    }
    else if (tree->gtOper == GT_ARR_ELEM)
    {
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s[", name);
        for (unsigned rank = tree->AsArrElem()->gtArrRank - 1; rank; rank--)
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), ",");
        }
        SimpleSprintf_s(bufp, buf, sizeof(buf), "]");
    }
    else if (tree->gtOper == GT_LEA)
    {
        GenTreeAddrMode* lea = tree->AsAddrMode();
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s(", name);
        if (lea->HasBase())
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "b+");
        }
        if (lea->HasIndex())
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "(i*%d)+", lea->gtScale);
        }
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "%d)", lea->Offset());
    }
    else if (tree->gtOper == GT_BOUNDS_CHECK)
    {
        switch (tree->AsBoundsChk()->gtThrowKind)
        {
            case SCK_RNGCHK_FAIL:
            {
                bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s_Rng", name);
                if (tree->AsBoundsChk()->gtIndRngFailBB != nullptr)
                {
                    bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " -> " FMT_BB,
                                            tree->AsBoundsChk()->gtIndRngFailBB->bbNum);
                }
                break;
            }
            case SCK_ARG_EXCPN:
                sprintf_s(bufp, sizeof(buf), " %s_Arg", name);
                break;
            case SCK_ARG_RNG_EXCPN:
                sprintf_s(bufp, sizeof(buf), " %s_ArgRng", name);
                break;
            default:
                unreached();
        }
    }
    else if (tree->gtOverflowEx())
    {
        sprintf_s(bufp, sizeof(buf), " %s_ovfl%c", name, 0);
    }
    else
    {
        sprintf_s(bufp, sizeof(buf), " %s%c", name, 0);
    }

    if (strlen(buf) < 10)
    {
        printf(" %-10s", buf);
    }
    else
    {
        printf(" %s", buf);
    }
}

//------------------------------------------------------------------------
// gtDispVN: Utility function that prints a tree's ValueNumber: gtVNPair
//
void Compiler::gtDispVN(GenTree* tree)
{
    if (tree->gtVNPair.GetLiberal() != ValueNumStore::NoVN)
    {
        assert(tree->gtVNPair.GetConservative() != ValueNumStore::NoVN);
        printf(" ");
        vnpPrint(tree->gtVNPair, 0);
    }
}

//------------------------------------------------------------------------
// gtDispCommonEndLine
//     Utility function that prints the following node information
//       1: The associated zero field sequence (if any)
//       2. The register assigned to this node (if any)
//       2. The value number assigned (if any)
//       3. A newline character
//
void Compiler::gtDispCommonEndLine(GenTree* tree)
{
    gtDispRegVal(tree);
    gtDispVN(tree);
    printf("\n");
}

//------------------------------------------------------------------------
// gtDispNode: Print a tree to jitstdout.
//
// Arguments:
//    tree - the tree to be printed
//    indentStack - the specification for the current level of indentation & arcs
//    msg         - a contextual method (i.e. from the parent) to print
//
// Return Value:
//    None.
//
// Notes:
//    'indentStack' may be null, in which case no indentation or arcs are printed
//    'msg' may be null

void Compiler::gtDispNode(GenTree* tree, IndentStack* indentStack, _In_ _In_opt_z_ const char* msg, bool isLIR)
{
    bool printFlags = true; // always true..

    int msgLength = 35;

    GenTree* prev;

    if (tree->gtSeqNum)
    {
        printf("N%03u ", tree->gtSeqNum);
        if (tree->gtCostsInitialized)
        {
            printf("(%3u,%3u) ", tree->GetCostEx(), tree->GetCostSz());
        }
        else
        {
            printf("(???"
                   ",???"
                   ") "); // This probably indicates a bug: the node has a sequence number, but not costs.
        }
    }
    else
    {
        prev = tree;

        bool     hasSeqNum = true;
        unsigned dotNum    = 0;
        do
        {
            dotNum++;
            prev = prev->gtPrev;

            if ((prev == nullptr) || (prev == tree))
            {
                hasSeqNum = false;
                break;
            }

            assert(prev);
        } while (prev->gtSeqNum == 0);

        // If we have an indent stack, don't add additional characters,
        // as it will mess up the alignment.
        bool displayDotNum = hasSeqNum && (indentStack == nullptr);
        if (displayDotNum)
        {
            printf("N%03u.%02u ", prev->gtSeqNum, dotNum);
        }
        else
        {
            printf("     ");
        }

        if (tree->gtCostsInitialized)
        {
            printf("(%3u,%3u) ", tree->GetCostEx(), tree->GetCostSz());
        }
        else
        {
            if (displayDotNum)
            {
                // Do better alignment in this case
                printf("       ");
            }
            else
            {
                printf("          ");
            }
        }
    }

    if (optValnumCSE_phase)
    {
        if (IS_CSE_INDEX(tree->gtCSEnum))
        {
            printf(FMT_CSE " (%s)", GET_CSE_INDEX(tree->gtCSEnum), (IS_CSE_USE(tree->gtCSEnum) ? "use" : "def"));
        }
        else
        {
            printf("             ");
        }
    }

    /* Print the node ID */
    printTreeID(JitConfig.JitDumpTreeIDs() ? tree : nullptr);
    printf(" ");

    if (tree->gtOper >= GT_COUNT)
    {
        printf(" **** ILLEGAL NODE ****");
        return;
    }

    if (printFlags)
    {
        /* First print the flags associated with the node */
        switch (tree->gtOper)
        {
            case GT_BLK:
            case GT_IND:
            case GT_STOREIND:
            case GT_STORE_BLK:
            case GT_STORE_DYN_BLK:
                // We prefer printing V or U
                if ((tree->gtFlags & (GTF_IND_VOLATILE | GTF_IND_UNALIGNED)) == 0)
                {
                    if (tree->gtFlags & GTF_IND_TGT_NOT_HEAP)
                    {
                        printf("s");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_TGT_HEAP)
                    {
                        printf("h");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_INITCLASS)
                    {
                        printf("I");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_INVARIANT)
                    {
                        printf("#");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_NONFAULTING)
                    {
                        printf("n"); // print a n for non-faulting
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_NONNULL)
                    {
                        printf("@");
                        --msgLength;
                        break;
                    }
                }
                if (tree->gtFlags & GTF_IND_VOLATILE)
                {
                    printf("V");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_IND_UNALIGNED)
                {
                    printf("U");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_CALL:
                if (tree->AsCall()->IsInlineCandidate())
                {
                    if (tree->AsCall()->IsGuardedDevirtualizationCandidate())
                    {
                        printf("&");
                    }
                    else
                    {
                        printf("I");
                    }
                    --msgLength;
                    break;
                }
                else if (tree->AsCall()->IsGuardedDevirtualizationCandidate())
                {
                    printf("G");
                    --msgLength;
                    break;
                }
                if (tree->AsCall()->gtCallMoreFlags & GTF_CALL_M_RETBUFFARG)
                {
                    printf("S");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_CALL_HOISTABLE)
                {
                    printf("H");
                    --msgLength;
                    break;
                }

                goto DASH;

            case GT_MUL:
#if !defined(TARGET_64BIT)
            case GT_MUL_LONG:
#endif
                if (tree->gtFlags & GTF_MUL_64RSLT)
                {
                    printf("L");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_LCL_FLD:
            case GT_LCL_VAR:
            case GT_LCL_ADDR:
            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
                if (tree->gtFlags & GTF_VAR_USEASG)
                {
                    printf("U");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_MULTIREG)
                {
                    printf((tree->gtFlags & GTF_VAR_DEF) ? "M" : "m");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_DEF)
                {
                    printf("D");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_CONTEXT)
                {
                    printf("!");
                    --msgLength;
                    break;
                }

                goto DASH;

            case GT_EQ:
            case GT_NE:
            case GT_LT:
            case GT_LE:
            case GT_GE:
            case GT_GT:
            case GT_TEST_EQ:
            case GT_TEST_NE:
            case GT_SELECT:
                if (tree->gtFlags & GTF_RELOP_NAN_UN)
                {
                    printf("N");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_RELOP_JMP_USED)
                {
                    printf("J");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_CNS_INT:
                if (tree->IsIconHandle())
                {
                    printf("H");
                    --msgLength;
                    break;
                }
                goto DASH;

            default:
            DASH:
                printf("-");
                --msgLength;
                break;
        }

        /* Then print the general purpose flags */
        GenTreeFlags flags = tree->gtFlags;

        if (tree->OperIsBinary() || tree->OperIsMultiOp())
        {
            genTreeOps oper = tree->OperGet();

            // Check for GTF_ADDRMODE_NO_CSE flag on add/mul/shl Binary Operators
            if ((oper == GT_ADD) || (oper == GT_MUL) || (oper == GT_LSH))
            {
                if ((tree->gtFlags & GTF_ADDRMODE_NO_CSE) != 0)
                {
                    flags |= GTF_DONT_CSE; // Force the GTF_ADDRMODE_NO_CSE flag to print out like GTF_DONT_CSE
                }
            }
        }
        else // !(tree->OperIsBinary() || tree->OperIsMultiOp())
        {
            // the GTF_REVERSE flag only applies to binary operations (which some MultiOp nodes are).
            flags &= ~GTF_REVERSE_OPS;
        }

        msgLength -= GenTree::gtDispFlags(flags, tree->gtDebugFlags);
        /*
            printf("%c", (flags & GTF_ASG           ) ? 'A' : '-');
            printf("%c", (flags & GTF_CALL          ) ? 'C' : '-');
            printf("%c", (flags & GTF_EXCEPT        ) ? 'X' : '-');
            printf("%c", (flags & GTF_GLOB_REF      ) ? 'G' : '-');
            printf("%c", (flags & GTF_ORDER_SIDEEFF ) ? 'O' : '-');
            printf("%c", (flags & GTF_COLON_COND    ) ? '?' : '-');
            printf("%c", (flags & GTF_DONT_CSE      ) ? 'N' :        // N is for No cse
                         (flags & GTF_MAKE_CSE      ) ? 'H' : '-');  // H is for Hoist this expr
            printf("%c", (flags & GTF_REVERSE_OPS   ) ? 'R' : '-');
            printf("%c", (flags & GTF_UNSIGNED      ) ? 'U' :
                         (flags & GTF_BOOLEAN       ) ? 'B' : '-');
            printf("%c", (flags & GTF_SET_FLAGS     ) ? 'S' : '-');
            printf("%c", ((flags & (GTF_SPILL | GTF_SPILLED)) == (GTF_SPILL | GTF_SPILLED)) ? '#' : ((flags &
           GTF_SPILLED) ? 'z' : ((flags & GTF_SPILL) ? 'Z' : '-')));
        */
    }

    // If we're printing a node for LIR, we use the space normally associated with the message
    // to display the node's temp name (if any)
    const bool hasOperands = tree->OperandsBegin() != tree->OperandsEnd();
    if (isLIR)
    {
        assert(msg == nullptr);

        // If the tree does not have any operands, we do not display the indent stack. This gives us
        // two additional characters for alignment.
        if (!hasOperands)
        {
            msgLength += 1;
        }

        if (tree->IsValue())
        {
            const size_t bufLength = msgLength - 1;
            msg                    = reinterpret_cast<char*>(_alloca(bufLength * sizeof(char)));
            sprintf_s(const_cast<char*>(msg), bufLength, "%c%d = %s", tree->IsUnusedValue() ? 'u' : 't', tree->gtTreeID,
                      hasOperands ? "" : " ");
        }
    }

    /* print the msg associated with the node */

    if (msg == nullptr)
    {
        msg = "";
    }
    if (msgLength < 0)
    {
        msgLength = 0;
    }

    printf(isLIR ? " %+*s" : " %-*s", msgLength, msg);

    /* Indent the node accordingly */
    if (!isLIR || hasOperands)
    {
        printIndent(indentStack);
    }

    gtDispNodeName(tree);

    assert(tree == nullptr || tree->gtOper < GT_COUNT);

    if (tree)
    {
        /* print the type of the node */
        if (tree->gtOper != GT_CAST)
        {
            printf(" %-6s", varTypeName(tree->TypeGet()));

            if (varTypeIsStruct(tree->TypeGet()))
            {
                ClassLayout* layout = nullptr;

                if (tree->OperIs(GT_BLK, GT_STORE_BLK))
                {
                    layout = tree->AsBlk()->GetLayout();
                }
                else if (tree->OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
                {
                    LclVarDsc* varDsc = lvaGetDesc(tree->AsLclVar());

                    if (varTypeIsStruct(varDsc->TypeGet()))
                    {
                        layout = varDsc->GetLayout();
                    }
                }
                else if (tree->OperIs(GT_LCL_FLD, GT_STORE_LCL_FLD))
                {
                    layout = tree->AsLclFld()->GetLayout();
                }

                if (layout != nullptr)
                {
                    gtDispClassLayout(layout, tree->TypeGet());
                }
            }

            if (tree->OperIs(GT_INDEX_ADDR, GT_ARR_ADDR))
            {
                var_types elemType =
                    tree->OperIs(GT_INDEX_ADDR) ? tree->AsIndexAddr()->gtElemType : tree->AsArrAddr()->GetElemType();

                CORINFO_CLASS_HANDLE elemClsHnd = tree->OperIs(GT_INDEX_ADDR) ? tree->AsIndexAddr()->gtStructElemClass
                                                                              : tree->AsArrAddr()->GetElemClassHandle();

                if (varTypeIsStruct(elemType) && (elemClsHnd != NO_CLASS_HANDLE))
                {
                    printf("%s[]", eeGetShortClassName(elemClsHnd));
                }
                else
                {
                    printf("%s[]", varTypeName(elemType));
                }
            }

            if (tree->OperIsLocal())
            {
                LclVarDsc* varDsc = lvaGetDesc(tree->AsLclVarCommon());
                if (varDsc->IsAddressExposed())
                {
                    printf("(AX)"); // Variable has address exposed.
                }
                if (varDsc->IsHiddenBufferStructArg())
                {
                    printf("(RB)"); // Variable is hidden return buffer
                }
                if (varDsc->lvUnusedStruct)
                {
                    assert(varDsc->lvPromoted);
                    printf("(U)"); // Unused struct
                }
                else if (varDsc->lvPromoted)
                {
                    if (varTypeIsPromotable(varDsc))
                    {
                        printf("(P)"); // Promoted struct
                    }
                    else
                    {
                        // Promoted implicit by-refs can have this state during
                        // global morph while they are being rewritten
                        printf("(P?!)"); // Promoted struct
                    }
                }
            }

            if (tree->gtOper == GT_RUNTIMELOOKUP)
            {
#ifdef TARGET_64BIT
                printf(" 0x%llx", dspPtr(tree->AsRuntimeLookup()->gtHnd));
#else
                printf(" 0x%x", dspPtr(tree->AsRuntimeLookup()->gtHnd));
#endif

                switch (tree->AsRuntimeLookup()->gtHndType)
                {
                    case CORINFO_HANDLETYPE_CLASS:
                        printf(" class");
                        break;
                    case CORINFO_HANDLETYPE_METHOD:
                        printf(" method");
                        break;
                    case CORINFO_HANDLETYPE_FIELD:
                        printf(" field");
                        break;
                    default:
                        printf(" unknown");
                        break;
                }
            }

            if (tree->OperIs(GT_MDARR_LENGTH, GT_MDARR_LOWER_BOUND))
            {
                printf(" (%u)", tree->AsMDArr()->Dim());
            }
        }

        // for tracking down problems in reguse prediction or liveness tracking

        if (verbose && 0)
        {
            printf(" RR=");
            dspRegMask(tree->gtRsvdRegs);
            printf("\n");
        }
    }
}

#if FEATURE_MULTIREG_RET
//----------------------------------------------------------------------------------
// gtDispMultiRegCount: determine how many registers to print for a multi-reg node
//
// Arguments:
//    tree  -  GenTree node whose registers we want to print
//
// Return Value:
//    The number of registers to print
//
// Notes:
//    This is not the same in all cases as GenTree::GetMultiRegCount().
//    In particular, for COPY or RELOAD it only returns the number of *valid* registers,
//    and for CALL, it will return 0 if the ReturnTypeDesc hasn't yet been initialized.
//    But we want to print all register positions.
//
unsigned Compiler::gtDispMultiRegCount(GenTree* tree)
{
    if (tree->IsCopyOrReload())
    {
        // GetRegCount() will return only the number of valid regs for COPY or RELOAD,
        // but we want to print all positions, so we get the reg count for op1.
        return gtDispMultiRegCount(tree->gtGetOp1());
    }
    else if (!tree->IsMultiRegNode())
    {
        // We can wind up here because IsMultiRegNode() always returns true for COPY or RELOAD,
        // even if its op1 is not multireg.
        // Note that this method won't be called for non-register-producing nodes.
        return 1;
    }
    else if (tree->OperIs(GT_CALL))
    {
        unsigned regCount = tree->AsCall()->GetReturnTypeDesc()->TryGetReturnRegCount();
        // If it hasn't yet been initialized, we'd still like to see the registers printed.
        if (regCount == 0)
        {
            regCount = MAX_RET_REG_COUNT;
        }
        return regCount;
    }
    else
    {
        return tree->GetMultiRegCount(this);
    }
}
#endif // FEATURE_MULTIREG_RET

//----------------------------------------------------------------------------------
// gtDispRegVal: Print the register(s) defined by the given node
//
// Arguments:
//    tree  -  GenTree node whose registers we want to print
//
void Compiler::gtDispRegVal(GenTree* tree)
{
    switch (tree->GetRegTag())
    {
        // Don't display anything for the GT_REGTAG_NONE case;
        // the absence of printed register values will imply this state.

        case GenTree::GT_REGTAG_REG:
            printf(" REG %s", compRegVarName(tree->GetRegNum()));
            break;

        default:
            return;
    }

#if FEATURE_MULTIREG_RET
    if (tree->IsMultiRegNode())
    {
        // 0th reg is GetRegNum(), which is already printed above.
        // Print the remaining regs of a multi-reg node.
        unsigned regCount = gtDispMultiRegCount(tree);

        // For some nodes, e.g. COPY, RELOAD or CALL, we may not have valid regs for all positions.
        for (unsigned i = 1; i < regCount; ++i)
        {
            regNumber reg = tree->GetRegByIndex(i);
            printf(",%s", genIsValidReg(reg) ? compRegVarName(reg) : "NA");
        }
    }
#endif
}

// We usually/commonly don't expect to print anything longer than this string,
#define LONGEST_COMMON_LCL_VAR_DISPLAY "V99 PInvokeFrame"
#define LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH (sizeof(LONGEST_COMMON_LCL_VAR_DISPLAY))
#define BUF_SIZE (LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH * 2)

void Compiler::gtGetLclVarNameInfo(unsigned lclNum, const char** ilKindOut, const char** ilNameOut, unsigned* ilNumOut)
{
    const char* ilKind = nullptr;
    const char* ilName = nullptr;

    unsigned ilNum = compMap2ILvarNum(lclNum);

    if (ilNum == (unsigned)ICorDebugInfo::RETBUF_ILNUM)
    {
        ilName = "RetBuf";
    }
    else if (ilNum == (unsigned)ICorDebugInfo::VARARGS_HND_ILNUM)
    {
        ilName = "VarArgHandle";
    }
    else if (ilNum == (unsigned)ICorDebugInfo::TYPECTXT_ILNUM)
    {
        ilName = "TypeCtx";
    }
    else if (ilNum == (unsigned)ICorDebugInfo::UNKNOWN_ILNUM)
    {
        if (lclNumIsTrueCSE(lclNum))
        {
            ilKind = "cse";
            ilNum  = lclNum - optCSEstart;
        }
        else if (lclNum >= optCSEstart)
        {
            // Currently any new LclVar's introduced after the CSE phase
            // are believed to be created by the "rationalizer" that is what is meant by the "rat" prefix.
            ilKind = "rat";
            ilNum  = lclNum - (optCSEstart + optCSEcount);
        }
        else
        {
            if (lclNum == info.compLvFrameListRoot)
            {
                ilName = "FramesRoot";
            }
            else if (lclNum == lvaInlinedPInvokeFrameVar)
            {
                ilName = "PInvokeFrame";
            }
            else if (lclNum == lvaGSSecurityCookie)
            {
                ilName = "GsCookie";
            }
            else if (lclNum == lvaRetAddrVar)
            {
                ilName = "ReturnAddress";
            }
#if FEATURE_FIXED_OUT_ARGS
            else if (lclNum == lvaOutgoingArgSpaceVar)
            {
                ilName = "OutArgs";
            }
#endif // FEATURE_FIXED_OUT_ARGS
#if !defined(FEATURE_EH_FUNCLETS)
            else if (lclNum == lvaShadowSPslotsVar)
            {
                ilName = "EHSlots";
            }
#endif // !FEATURE_EH_FUNCLETS
#ifdef JIT32_GCENCODER
            else if (lclNum == lvaLocAllocSPvar)
            {
                ilName = "LocAllocSP";
            }
#endif // JIT32_GCENCODER
#if defined(FEATURE_EH_FUNCLETS)
            else if (lclNum == lvaPSPSym)
            {
                ilName = "PSPSym";
            }
#endif // FEATURE_EH_FUNCLETS
            else
            {
                ilKind = "tmp";
                if (compIsForInlining())
                {
                    ilNum = lclNum - impInlineInfo->InlinerCompiler->info.compLocalsCount;
                }
                else
                {
                    ilNum = lclNum - info.compLocalsCount;
                }
            }
        }
    }
    else if (lclNum < (compIsForInlining() ? impInlineInfo->InlinerCompiler->info.compArgsCount : info.compArgsCount))
    {
        if (ilNum == 0 && !info.compIsStatic)
        {
            ilName = "this";
        }
        else
        {
            ilKind = "arg";
        }
    }
    else
    {
        if (!lvaTable[lclNum].lvIsStructField)
        {
            ilKind = "loc";
        }
        if (compIsForInlining())
        {
            ilNum -= impInlineInfo->InlinerCompiler->info.compILargsCount;
        }
        else
        {
            ilNum -= info.compILargsCount;
        }
    }

    *ilKindOut = ilKind;
    *ilNameOut = ilName;
    *ilNumOut  = ilNum;
}

/*****************************************************************************/
int Compiler::gtGetLclVarName(unsigned lclNum, char* buf, unsigned buf_remaining)
{
    char*    bufp_next    = buf;
    unsigned charsPrinted = 0;
    int      sprintf_result;

    sprintf_result = sprintf_s(bufp_next, buf_remaining, "V%02u", lclNum);

    if (sprintf_result < 0)
    {
        return sprintf_result;
    }

    charsPrinted += sprintf_result;
    bufp_next += sprintf_result;
    buf_remaining -= sprintf_result;

    const char* ilKind = nullptr;
    const char* ilName = nullptr;
    unsigned    ilNum  = 0;

    gtGetLclVarNameInfo(lclNum, &ilKind, &ilName, &ilNum);

    if (ilName != nullptr)
    {
        sprintf_result = sprintf_s(bufp_next, buf_remaining, " %s", ilName);
        if (sprintf_result < 0)
        {
            return sprintf_result;
        }
        charsPrinted += sprintf_result;
        bufp_next += sprintf_result;
        buf_remaining -= sprintf_result;
    }
    else if (ilKind != nullptr)
    {
        sprintf_result = sprintf_s(bufp_next, buf_remaining, " %s%d", ilKind, ilNum);
        if (sprintf_result < 0)
        {
            return sprintf_result;
        }
        charsPrinted += sprintf_result;
        bufp_next += sprintf_result;
        buf_remaining -= sprintf_result;
    }

    assert(charsPrinted > 0);
    assert(buf_remaining > 0);

    return (int)charsPrinted;
}

/*****************************************************************************
 * Get the local var name, and create a copy of the string that can be used in debug output.
 */
char* Compiler::gtGetLclVarName(unsigned lclNum)
{
    char buf[BUF_SIZE];
    int  charsPrinted = gtGetLclVarName(lclNum, buf, ArrLen(buf));
    if (charsPrinted < 0)
    {
        return nullptr;
    }

    char* retBuf = new (this, CMK_DebugOnly) char[charsPrinted + 1];
    strcpy_s(retBuf, charsPrinted + 1, buf);
    return retBuf;
}

/*****************************************************************************/
void Compiler::gtDispLclVar(unsigned lclNum, bool padForBiggestDisp)
{
    char buf[BUF_SIZE];
    int  charsPrinted = gtGetLclVarName(lclNum, buf, ArrLen(buf));

    if (charsPrinted < 0)
    {
        return;
    }

    printf("%s", buf);

    if (padForBiggestDisp && (charsPrinted < (int)LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH))
    {
        printf("%*c", LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH - charsPrinted, ' ');
    }
}

//------------------------------------------------------------------------
// gtDispLclVarStructType: Print size and type information about a struct or lclBlk local variable.
//
// Arguments:
//   lclNum - The local var id.
//
void Compiler::gtDispLclVarStructType(unsigned lclNum)
{
    LclVarDsc* varDsc = lvaGetDesc(lclNum);
    var_types  type   = varDsc->TypeGet();
    if (type == TYP_STRUCT)
    {
        ClassLayout* layout = varDsc->GetLayout();
        assert(layout != nullptr);
        gtDispClassLayout(layout, type);
    }
}

#if defined(DEBUG) && defined(TARGET_ARM64)
static const char* InsCflagsToString(insCflags flags)
{
    const static char* s_table[16] = {"0", "v",  "c",  "cv",  "z",  "zv",  "zc",  "zcv",
                                      "n", "nv", "nc", "ncv", "nz", "nzv", "nzc", "nzcv"};
    unsigned index = (unsigned)flags;
    assert((0 <= index) && (index < ArrLen(s_table)));
    return s_table[index];
}
#endif

//------------------------------------------------------------------------
// gtDispSsaName: Display the SSA use/def for a given local.
//
// Arguments:
//   lclNum - The local's number.
//   ssaNum - The SSA number.
//   isDef  - Whether this is a def.
//
void Compiler::gtDispSsaName(unsigned lclNum, unsigned ssaNum, bool isDef)
{
    if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
    {
        return;
    }

    LclVarDsc* const lclDsc  = lvaGetDesc(lclNum);
    bool const       isValid = lclDsc->IsValidSsaNum(ssaNum);

    if (isDef)
    {
        if (!isValid)
        {
            printf("?d:%d", ssaNum);
            return;
        }

        unsigned oldDefSsaNum = lclDsc->GetPerSsaData(ssaNum)->GetUseDefSsaNum();
        if (oldDefSsaNum != SsaConfig::RESERVED_SSA_NUM)
        {
            printf("ud:%d->%d", oldDefSsaNum, ssaNum);
            return;
        }
    }

    printf("%s%s:%d", isValid ? "" : "?", isDef ? "d" : "u", ssaNum);
}

//------------------------------------------------------------------------
// gtDispClassLayout: Print size and type information about a layout.
//
// Arguments:
//   layout - the layout;
//   type   - variable type, used to avoid printing size for SIMD nodes.
//
void Compiler::gtDispClassLayout(ClassLayout* layout, var_types type)
{
    assert(layout != nullptr);
    if (layout->IsBlockLayout())
    {
        printf("<%u>", layout->GetSize());
    }
    else if (varTypeIsSIMD(type))
    {
        printf("<%s>", layout->GetShortClassName());
    }
    else
    {
        printf("<%s, %u>", layout->GetShortClassName(), layout->GetSize());
    }
}

/*****************************************************************************/
void Compiler::gtDispConst(GenTree* tree)
{
    assert(tree->OperIsConst());

    switch (tree->gtOper)
    {
        case GT_CNS_INT:
            if (tree->IsIconHandle(GTF_ICON_STR_HDL))
            {
                printf(" 0x%X [ICON_STR_HDL]", dspPtr(tree->AsIntCon()->gtIconVal));
            }
            else if (tree->IsIconHandle(GTF_ICON_OBJ_HDL))
            {
                eePrintObjectDescription(" ", (CORINFO_OBJECT_HANDLE)tree->AsIntCon()->gtIconVal);
            }
            else
            {
                ssize_t dspIconVal =
                    tree->IsIconHandle() ? dspPtr(tree->AsIntCon()->gtIconVal) : tree->AsIntCon()->gtIconVal;

                if (tree->TypeGet() == TYP_REF)
                {
                    if (tree->AsIntCon()->gtIconVal == 0)
                    {
                        printf(" null");
                    }
                    else
                    {
                        assert(doesMethodHaveFrozenObjects());
                        printf(" 0x%llx", dspIconVal);
                    }
                }
                else if ((tree->AsIntCon()->gtIconVal > -1000) && (tree->AsIntCon()->gtIconVal < 1000))
                {
                    printf(" %ld", dspIconVal);
                }
#ifdef TARGET_64BIT
                else if ((tree->AsIntCon()->gtIconVal & 0xFFFFFFFF00000000LL) != 0)
                {
                    if (dspIconVal >= 0)
                    {
                        printf(" 0x%llx", dspIconVal);
                    }
                    else
                    {
                        printf(" -0x%llx", -dspIconVal);
                    }
                }
#endif
                else
                {
                    if (dspIconVal >= 0)
                    {
                        printf(" 0x%X", dspIconVal);
                    }
                    else
                    {
                        printf(" -0x%X", -dspIconVal);
                    }
                }

                if (tree->IsIconHandle())
                {
                    switch (tree->GetIconHandleFlag())
                    {
                        case GTF_ICON_SCOPE_HDL:
                            printf(" scope");
                            break;
                        case GTF_ICON_CLASS_HDL:
                            printf(" class");
                            break;
                        case GTF_ICON_METHOD_HDL:
                            printf(" method");
                            break;
                        case GTF_ICON_FIELD_HDL:
                            printf(" field");
                            break;
                        case GTF_ICON_STATIC_HDL:
                            printf(" static");
                            break;
                        case GTF_ICON_OBJ_HDL:
                        case GTF_ICON_STR_HDL:
                            unreached(); // These cases are handled above
                            break;
                        case GTF_ICON_CONST_PTR:
                            printf(" const ptr");
                            break;
                        case GTF_ICON_GLOBAL_PTR:
                            printf(" global ptr");
                            break;
                        case GTF_ICON_VARG_HDL:
                            printf(" vararg");
                            break;
                        case GTF_ICON_PINVKI_HDL:
                            printf(" pinvoke");
                            break;
                        case GTF_ICON_TOKEN_HDL:
                            printf(" token");
                            break;
                        case GTF_ICON_TLS_HDL:
                            printf(" tls");
                            break;
                        case GTF_ICON_FTN_ADDR:
                            printf(" ftn");
                            break;
                        case GTF_ICON_CIDMID_HDL:
                            printf(" cid/mid");
                            break;
                        case GTF_ICON_BBC_PTR:
                            printf(" bbc");
                            break;
                        case GTF_ICON_STATIC_BOX_PTR:
                            printf(" static box ptr");
                            break;
                        case GTF_ICON_STATIC_ADDR_PTR:
                            printf(" static base addr cell");
                            break;
                        case GTF_ICON_SECREL_OFFSET:
                            printf(" relative offset in section");
                            break;
                        case GTF_ICON_TLSGD_OFFSET:
                            printf(" tls global dynamic offset");
                            break;
                        default:
                            printf(" UNKNOWN");
                            break;
                    }
                }

#ifdef FEATURE_SIMD
                if ((tree->gtFlags & GTF_ICON_SIMD_COUNT) != 0)
                {
                    printf(" vector element count");
                }
#endif

                if ((tree->IsReuseRegVal()) != 0)
                {
                    printf(" reuse reg val");
                }
            }

            if (tree->AsIntCon()->gtFieldSeq != nullptr)
            {
                FieldSeq* fieldSeq = tree->AsIntCon()->gtFieldSeq;
                gtDispFieldSeq(fieldSeq, tree->AsIntCon()->IconValue() - fieldSeq->GetOffset());
            }
            break;

        case GT_CNS_LNG:
            printf(" 0x%016I64x", tree->AsLngCon()->gtLconVal);
            break;

        case GT_CNS_DBL:
        {
            double dcon = tree->AsDblCon()->DconValue();
            if (FloatingPointUtils::isNegativeZero(dcon))
            {
                printf(" -0.00000");
            }
            else if (FloatingPointUtils::isNaN(dcon))
            {
                uint64_t bits;
                static_assert_no_msg(sizeof(bits) == sizeof(dcon));
                memcpy(&bits, &dcon, sizeof(dcon));
                printf(" %#.17g(0x%llx)\n", dcon, bits);
            }
            else
            {
                printf(" %#.17g", dcon);
            }
            break;
        }

        case GT_CNS_STR:
            printf("<string constant>");
            break;

#if defined(FEATURE_SIMD)
        case GT_CNS_VEC:
        {
            GenTreeVecCon* vecCon = tree->AsVecCon();

            switch (vecCon->TypeGet())
            {
                case TYP_SIMD8:
                {
                    printf("<0x%08x, 0x%08x>", vecCon->gtSimdVal.u32[0], vecCon->gtSimdVal.u32[1]);
                    break;
                }

                case TYP_SIMD12:
                {
                    printf("<0x%08x, 0x%08x, 0x%08x>", vecCon->gtSimdVal.u32[0], vecCon->gtSimdVal.u32[1],
                           vecCon->gtSimdVal.u32[2]);
                    break;
                }

                case TYP_SIMD16:
                {
                    printf("<0x%08x, 0x%08x, 0x%08x, 0x%08x>", vecCon->gtSimdVal.u32[0], vecCon->gtSimdVal.u32[1],
                           vecCon->gtSimdVal.u32[2], vecCon->gtSimdVal.u32[3]);
                    break;
                }

#if defined(TARGET_XARCH)
                case TYP_SIMD32:
                {
                    printf("<0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx>", vecCon->gtSimdVal.u64[0],
                           vecCon->gtSimdVal.u64[1], vecCon->gtSimdVal.u64[2], vecCon->gtSimdVal.u64[3]);
                    break;
                }

                case TYP_SIMD64:
                {
                    printf("<0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx, 0x%016llx>",
                           vecCon->gtSimdVal.u64[0], vecCon->gtSimdVal.u64[1], vecCon->gtSimdVal.u64[2],
                           vecCon->gtSimdVal.u64[3], vecCon->gtSimdVal.u64[4], vecCon->gtSimdVal.u64[5],
                           vecCon->gtSimdVal.u64[6], vecCon->gtSimdVal.u64[7]);
                    break;
                }
#endif // TARGET_XARCH

                default:
                {
                    unreached();
                }
            }

            break;
        }
#endif // FEATURE_SIMD

        default:
            assert(!"unexpected constant node");
    }
}

//------------------------------------------------------------------------
// gtDispFieldSeq: Print out the fields in this field sequence.
//
// Arguments:
//    fieldSeq - The field sequence
//    offset   - Offset of the (implicit) struct fields in the sequence
//
void Compiler::gtDispFieldSeq(FieldSeq* fieldSeq, ssize_t offset)
{
    if (fieldSeq == nullptr)
    {
        return;
    }

    char buffer[128];
    printf(" Fseq[%s", eeGetFieldName(fieldSeq->GetFieldHandle(), false, buffer, sizeof(buffer)));
    if (offset != 0)
    {
        printf(", %zd", offset);
    }
    printf("]");
}

//------------------------------------------------------------------------
// gtDispLeaf: Print a single leaf node to jitstdout.
//
// Arguments:
//    tree - the tree to be printed
//    indentStack - the specification for the current level of indentation & arcs
//
// Return Value:
//    None.
//
// Notes:
//    'indentStack' may be null, in which case no indentation or arcs are printed

void Compiler::gtDispLeaf(GenTree* tree, IndentStack* indentStack)
{
    if (tree->OperIsConst())
    {
        gtDispConst(tree);
        return;
    }

    char buffer[256];

    switch (tree->gtOper)
    {
        case GT_PHI_ARG:
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_ADDR:
            gtDispLocal(tree->AsLclVarCommon(), indentStack);
            break;

        case GT_JMP:
        {
            printf(" %s", eeGetMethodFullName((CORINFO_METHOD_HANDLE)tree->AsVal()->gtVal1, true, true, buffer,
                                              sizeof(buffer)));
        }
        break;

        case GT_LABEL:
            break;

        case GT_FTN_ADDR:
        {
            printf(" %s\n", eeGetMethodFullName((CORINFO_METHOD_HANDLE)tree->AsFptrVal()->gtFptrMethod, true, true,
                                                buffer, sizeof(buffer)));
        }
        break;

#if !defined(FEATURE_EH_FUNCLETS)
        case GT_END_LFIN:
            printf(" endNstLvl=%d", tree->AsVal()->gtVal1);
            break;
#endif // !FEATURE_EH_FUNCLETS

        // Vanilla leaves. No qualifying information available. So do nothing

        case GT_NOP:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_START_PREEMPTGC:
        case GT_PROF_HOOK:
        case GT_CATCH_ARG:
        case GT_MEMORYBARRIER:
        case GT_PINVOKE_PROLOG:
        case GT_JMPTABLE:
            break;

        case GT_RET_EXPR:
        {
            GenTreeCall* inlineCand = tree->AsRetExpr()->gtInlineCandidate;
            printf("(for ");
            printTreeID(inlineCand);
            printf(")");

            if (tree->AsRetExpr()->gtSubstExpr != nullptr)
            {
                printf(" -> ");
                printTreeID(tree->AsRetExpr()->gtSubstExpr);
            }
        }
        break;

        case GT_PHYSREG:
            printf(" %s", getRegName(tree->AsPhysReg()->gtSrcReg));
            break;

        case GT_IL_OFFSET:
            printf(" ");
            tree->AsILOffset()->gtStmtDI.Dump(true);
            break;

        case GT_JCC:
        case GT_SETCC:
            printf(" cond=%s", tree->AsCC()->gtCondition.Name());
            break;
        case GT_JCMP:
        case GT_JTEST:
            printf(" cond=%s", tree->AsOpCC()->gtCondition.Name());
            break;

        default:
            assert(!"don't know how to display tree leaf node");
    }
}

//------------------------------------------------------------------------
// gtDispLocal: Print description of a local node to jitstdout.
//
// Prints the information common to all local nodes. Does not print children.
//
// Arguments:
//    tree        - the local tree
//    indentStack - the specification for the current level of indentation & arcs
//
void Compiler::gtDispLocal(GenTreeLclVarCommon* tree, IndentStack* indentStack)
{
    printf(" ");
    const unsigned   varNum   = tree->GetLclNum();
    const LclVarDsc* varDsc   = lvaGetDesc(varNum);
    const bool       isDef    = (tree->gtFlags & GTF_VAR_DEF) != 0;
    const bool       isLclFld = tree->OperIsLocalField();

    gtDispLclVar(varNum);
    gtDispSsaName(varNum, tree->GetSsaNum(), isDef);

    if (isLclFld)
    {
        printf("[+%u]", tree->AsLclFld()->GetLclOffs());
    }

    if (varDsc->lvRegister)
    {
        printf(" ");
        varDsc->PrintVarReg();
    }
    else if (tree->InReg())
    {
        printf(" %s", compRegVarName(tree->GetRegNum()));
    }

    if (varDsc->lvPromoted)
    {
        if (!varTypeIsPromotable(varDsc) && !varDsc->lvUnusedStruct)
        {
            // Promoted implicit byrefs can get in this state while they are being rewritten
            // in global morph.
        }
        else
        {
            for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
            {
                unsigned   fieldLclNum = varDsc->lvFieldLclStart + index;
                LclVarDsc* fieldVarDsc = lvaGetDesc(fieldLclNum);

                printf("\n");
                printf("                                                            ");
                printIndent(indentStack);
                printf("    %-6s %s -> ", varTypeName(fieldVarDsc->TypeGet()), fieldVarDsc->lvReason);
                gtDispLclVar(fieldLclNum);
                gtDispSsaName(fieldLclNum, tree->GetSsaNum(this, index), isDef);

                if (fieldVarDsc->lvRegister)
                {
                    printf(" ");
                    fieldVarDsc->PrintVarReg();
                }

                if (fieldVarDsc->lvTracked && fgLocalVarLivenessDone && tree->IsLastUse(index))
                {
                    printf(" (last use)");
                }
            }
        }
    }
    else // a normal not-promoted lclvar
    {
        if ((varDsc->lvTracked || varDsc->lvTrackedWithoutIndex) && fgLocalVarLivenessDone &&
            ((tree->gtFlags & GTF_VAR_DEATH) != 0))
        {
            printf(" (last use)");
        }
    }
}

//------------------------------------------------------------------------
// gtDispChild: Print a child node to jitstdout.
//
// Arguments:
//    tree - the tree to be printed
//    indentStack - the specification for the current level of indentation & arcs
//    arcType     - the type of arc to use for this child
//    msg         - a contextual method (i.e. from the parent) to print
//    topOnly     - a boolean indicating whether to print the children, or just the top node
//
// Return Value:
//    None.
//
// Notes:
//    'indentStack' may be null, in which case no indentation or arcs are printed
//    'msg' has a default value of null
//    'topOnly' is an optional argument that defaults to false

void Compiler::gtDispChild(GenTree*             child,
                           IndentStack*         indentStack,
                           IndentInfo           arcType,
                           _In_opt_ const char* msg,     /* = nullptr  */
                           bool                 topOnly) /* = false */
{
    indentStack->Push(arcType);
    gtDispTree(child, indentStack, msg, topOnly);
    indentStack->Pop();
}

/*****************************************************************************/

void Compiler::gtDispTree(GenTree*     tree,
                          IndentStack* indentStack,            /* = nullptr */
                          _In_ _In_opt_z_ const char* msg,     /* = nullptr  */
                          bool                        topOnly, /* = false */
                          bool                        isLIR)   /* = false */
{
    if (tree == nullptr)
    {
        printf(" [%08X] <NULL>\n", tree);
        printf(""); // null string means flush
        return;
    }

    if (indentStack == nullptr)
    {
        indentStack = new (this, CMK_DebugOnly) IndentStack(this);
    }

    if (IsUninitialized(tree))
    {
        /* Value used to initialize nodes */
        printf("Uninitialized tree node!\n");
        return;
    }

    if (tree->gtOper >= GT_COUNT)
    {
        gtDispNode(tree, indentStack, msg, isLIR);
        printf("Bogus operator!\n");
        return;
    }

    // Determine what kind of arc to propagate.
    IndentInfo myArc    = IINone;
    IndentInfo lowerArc = IINone;
    if (indentStack->Depth() > 0)
    {
        myArc = indentStack->Pop();
        switch (myArc)
        {
            case IIArcBottom:
                indentStack->Push(IIArc);
                lowerArc = IINone;
                break;
            case IIArc:
                indentStack->Push(IIArc);
                lowerArc = IIArc;
                break;
            case IIArcTop:
                indentStack->Push(IINone);
                lowerArc = IIArc;
                break;
            case IINone:
                indentStack->Push(IINone);
                lowerArc = IINone;
                break;
            default:
                unreached();
                break;
        }
    }

    /* Is it a 'simple' unary/binary operator? */

    const char* childMsg = nullptr;

    if (tree->OperIsSimple())
    {
        // Now, get the right type of arc for this node
        if (myArc != IINone)
        {
            indentStack->Pop();
            indentStack->Push(myArc);
        }

        gtDispNode(tree, indentStack, msg, isLIR);

        // Propagate lowerArc to the lower children.
        if (indentStack->Depth() > 0)
        {
            (void)indentStack->Pop();
            indentStack->Push(lowerArc);
        }

        if (tree->gtOper == GT_CAST)
        {
            /* Format a message that explains the effect of this GT_CAST */

            var_types fromType  = genActualType(tree->AsCast()->CastOp()->TypeGet());
            var_types toType    = tree->CastToType();
            var_types finalType = tree->TypeGet();

            /* if GTF_UNSIGNED is set then force fromType to an unsigned type */
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                fromType = varTypeToUnsigned(fromType);
            }

            if (finalType != toType)
            {
                printf(" %s <-", varTypeName(finalType));
            }

            printf(" %s <- %s", varTypeName(toType), varTypeName(fromType));
        }

        if (tree->OperIsLocalStore()) // Local stores used to be leaf nodes.
        {
            gtDispLocal(tree->AsLclVarCommon(), indentStack);
        }
        else if (tree->OperIsBlkOp())
        {
            if (tree->OperIsCopyBlkOp())
            {
                printf(" (copy)");
            }
            else if (tree->OperIsInitBlkOp())
            {
                printf(" (init)");
            }
            if (tree->OperIsStoreBlk() && (tree->AsBlk()->gtBlkOpKind != GenTreeBlk::BlkOpKindInvalid))
            {
                switch (tree->AsBlk()->gtBlkOpKind)
                {
                    case GenTreeBlk::BlkOpKindCpObjUnroll:
                        printf(" (CpObjUnroll)");
                        break;
#ifdef TARGET_XARCH
                    case GenTreeBlk::BlkOpKindCpObjRepInstr:
                        printf(" (CpObjRepInstr)");
                        break;

                    case GenTreeBlk::BlkOpKindRepInstr:
                        printf(" (RepInstr)");
                        break;
#endif
                    case GenTreeBlk::BlkOpKindUnroll:
                        printf(" (Unroll)");
                        break;

                    case GenTreeBlk::BlkOpKindUnrollMemmove:
                        printf(" (Memmove)");
                        break;
#ifndef TARGET_X86
                    case GenTreeBlk::BlkOpKindHelper:
                        printf(" (Helper)");
                        break;
#endif

                    case GenTreeBlk::BlkOpKindLoop:
                        printf(" (Loop)");
                        break;

                    default:
                        unreached();
                }
            }
        }
#if FEATURE_PUT_STRUCT_ARG_STK
        else if (tree->OperGet() == GT_PUTARG_STK)
        {
            const GenTreePutArgStk* putArg = tree->AsPutArgStk();
            printf(" (%d stackByteSize), (%d byteOffset)", putArg->GetStackByteSize(), putArg->getArgOffset());
            if (putArg->gtPutArgStkKind != GenTreePutArgStk::Kind::Invalid)
            {
                switch (putArg->gtPutArgStkKind)
                {
                    case GenTreePutArgStk::Kind::RepInstr:
                        printf(" (RepInstr)");
                        break;
                    case GenTreePutArgStk::Kind::PartialRepInstr:
                        printf(" (PartialRepInstr)");
                        break;
                    case GenTreePutArgStk::Kind::Unroll:
                        printf(" (Unroll)");
                        break;
                    case GenTreePutArgStk::Kind::Push:
                        printf(" (Push)");
                        break;
                    default:
                        unreached();
                }
            }
        }
#if FEATURE_ARG_SPLIT
        else if (tree->OperGet() == GT_PUTARG_SPLIT)
        {
            const GenTreePutArgSplit* putArg = tree->AsPutArgSplit();
            printf(" (%d stackByteSize), (%d numRegs)", putArg->GetStackByteSize(), putArg->gtNumRegs);
        }
#endif // FEATURE_ARG_SPLIT
#endif // FEATURE_PUT_STRUCT_ARG_STK

        if (tree->OperIs(GT_FIELD_ADDR))
        {
            auto disp = [&]() {
                char buffer[256];
                printf(" %s", eeGetFieldName(tree->AsFieldAddr()->gtFldHnd, true, buffer, sizeof(buffer)));
            };
            disp();
        }

        if (tree->gtOper == GT_INTRINSIC)
        {
            GenTreeIntrinsic* intrinsic = tree->AsIntrinsic();

            switch (intrinsic->gtIntrinsicName)
            {
                case NI_System_Math_Abs:
                    printf(" abs");
                    break;
                case NI_System_Math_Acos:
                    printf(" acos");
                    break;
                case NI_System_Math_Acosh:
                    printf(" acosh");
                    break;
                case NI_System_Math_Asin:
                    printf(" asin");
                    break;
                case NI_System_Math_Asinh:
                    printf(" asinh");
                    break;
                case NI_System_Math_Atan:
                    printf(" atan");
                    break;
                case NI_System_Math_Atanh:
                    printf(" atanh");
                    break;
                case NI_System_Math_Atan2:
                    printf(" atan2");
                    break;
                case NI_System_Math_Cbrt:
                    printf(" cbrt");
                    break;
                case NI_System_Math_Ceiling:
                    printf(" ceiling");
                    break;
                case NI_System_Math_Cos:
                    printf(" cos");
                    break;
                case NI_System_Math_Cosh:
                    printf(" cosh");
                    break;
                case NI_System_Math_Exp:
                    printf(" exp");
                    break;
                case NI_System_Math_Floor:
                    printf(" floor");
                    break;
                case NI_System_Math_FMod:
                    printf(" fmod");
                    break;
                case NI_System_Math_FusedMultiplyAdd:
                    printf(" fma");
                    break;
                case NI_System_Math_ILogB:
                    printf(" ilogb");
                    break;
                case NI_System_Math_Log:
                    printf(" log");
                    break;
                case NI_System_Math_Log2:
                    printf(" log2");
                    break;
                case NI_System_Math_Log10:
                    printf(" log10");
                    break;
                case NI_System_Math_Max:
                    printf(" max");
                    break;
                case NI_System_Math_MaxMagnitude:
                    printf(" maxMagnitude");
                    break;
                case NI_System_Math_MaxMagnitudeNumber:
                    printf(" maxMagnitudeNumber");
                    break;
                case NI_System_Math_MaxNumber:
                    printf(" maxNumber");
                    break;
                case NI_System_Math_Min:
                    printf(" min");
                    break;
                case NI_System_Math_MinMagnitude:
                    printf(" minMagnitude");
                    break;
                case NI_System_Math_MinMagnitudeNumber:
                    printf(" minMagnitudeNumber");
                    break;
                case NI_System_Math_MinNumber:
                    printf(" minNumber");
                    break;
                case NI_System_Math_Pow:
                    printf(" pow");
                    break;
                case NI_System_Math_Round:
                    printf(" round");
                    break;
                case NI_System_Math_Sin:
                    printf(" sin");
                    break;
                case NI_System_Math_Sinh:
                    printf(" sinh");
                    break;
                case NI_System_Math_Sqrt:
                    printf(" sqrt");
                    break;
                case NI_System_Math_Tan:
                    printf(" tan");
                    break;
                case NI_System_Math_Tanh:
                    printf(" tanh");
                    break;
                case NI_System_Math_Truncate:
                    printf(" truncate");
                    break;
                case NI_System_Object_GetType:
                    printf(" objGetType");
                    break;
                case NI_System_Runtime_CompilerServices_RuntimeHelpers_IsKnownConstant:
                    printf(" isKnownConst");
                    break;
#if defined(FEATURE_SIMD)
                case NI_SIMD_UpperRestore:
                    printf(" simdUpperRestore");
                    break;
                case NI_SIMD_UpperSave:
                    printf(" simdUpperSave");
                    break;
#endif // FEATURE_SIMD
                default:
                    printf("Unknown intrinsic: ");
                    printTreeID(tree);
                    break;
            }
        }
        else if (tree->OperIs(GT_SELECTCC))
        {
            printf(" cond=%s", tree->AsOpCC()->gtCondition.Name());
        }
#ifdef TARGET_ARM64
        else if (tree->OperIs(GT_SELECT_INCCC, GT_SELECT_INVCC, GT_SELECT_NEGCC))
        {
            printf(" cond=%s", tree->AsOpCC()->gtCondition.Name());
        }
        else if (tree->OperIs(GT_CCMP))
        {
            printf(" cond=%s flags=%s", tree->AsCCMP()->gtCondition.Name(),
                   InsCflagsToString(tree->AsCCMP()->gtFlagsVal));
        }
#endif

        gtDispCommonEndLine(tree);

        if (!topOnly)
        {
            if (tree->AsOp()->gtOp1 != nullptr)
            {
                // Label the child of the GT_COLON operator
                // op1 is the else part
                if (tree->gtOper == GT_COLON)
                {
                    childMsg = "else";
                }
                else if (tree->gtOper == GT_QMARK)
                {
                    childMsg = "   if";
                }
                gtDispChild(tree->AsOp()->gtOp1, indentStack,
                            (tree->gtGetOp2IfPresent() == nullptr) ? IIArcBottom : IIArc, childMsg, topOnly);
            }

            if (tree->gtGetOp2IfPresent())
            {
                // Label the childMsgs of the GT_COLON operator
                // op2 is the then part

                if (tree->gtOper == GT_COLON)
                {
                    childMsg = "then";
                }
                gtDispChild(tree->AsOp()->gtOp2, indentStack, IIArcBottom, childMsg, topOnly);
            }
        }

        return;
    }

    // Now, get the right type of arc for this node
    if (myArc != IINone)
    {
        indentStack->Pop();
        indentStack->Push(myArc);
    }
    gtDispNode(tree, indentStack, msg, isLIR);

    // Propagate lowerArc to the lower children.
    if (indentStack->Depth() > 0)
    {
        (void)indentStack->Pop();
        indentStack->Push(lowerArc);
    }

    // See what kind of a special operator we have here, and handle its special children.

    switch (tree->gtOper)
    {
        case GT_FIELD_LIST:
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
                {
                    char offset[32];
                    sprintf_s(offset, sizeof(offset), "ofs %u", use.GetOffset());
                    gtDispChild(use.GetNode(), indentStack, (use.GetNext() == nullptr) ? IIArcBottom : IIArc, offset);
                }
            }
            break;

        case GT_PHI:
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
                {
                    char block[32];
                    sprintf_s(block, sizeof(block), "pred " FMT_BB, use.GetNode()->AsPhiArg()->gtPredBB->bbNum);
                    gtDispChild(use.GetNode(), indentStack, (use.GetNext() == nullptr) ? IIArcBottom : IIArc, block);
                }
            }
            break;

        case GT_CALL:
        {
            GenTreeCall* call      = tree->AsCall();
            GenTree*     lastChild = nullptr;
            call->VisitOperands([&lastChild](GenTree* operand) -> GenTree::VisitResult {
                lastChild = operand;
                return GenTree::VisitResult::Continue;
            });

            if (call->gtCallType != CT_INDIRECT)
            {
                auto disp = [&]() {
                    char buffer[256];
                    printf(" %s", eeGetMethodFullName(call->gtCallMethHnd, true, true, buffer, sizeof(buffer)));
                };
                disp();
            }

            if ((call->gtFlags & GTF_CALL_UNMANAGED) && (call->gtCallMoreFlags & GTF_CALL_M_FRAME_VAR_DEATH))
            {
                printf(" (FramesRoot last use)");
            }

            if ((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0)
            {
                InlineCandidateInfo* inlineInfo;
                if (call->IsGuardedDevirtualizationCandidate())
                {
                    inlineInfo = call->GetGDVCandidateInfo(0);
                }
                else
                {
                    inlineInfo = call->GetSingleInlineCandidateInfo();
                }
                if ((inlineInfo != nullptr) && (inlineInfo->exactContextHnd != nullptr))
                {
                    printf(" (exactContextHnd=0x%p)", dspPtr(inlineInfo->exactContextHnd));
                }
            }

            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                char buf[64];

                gtDispArgList(call, lastChild, indentStack);

                if (call->gtCallType == CT_INDIRECT)
                {
                    gtDispChild(call->gtCallAddr, indentStack, (call->gtCallAddr == lastChild) ? IIArcBottom : IIArc,
                                "calli tgt", topOnly);
                }

                if (call->gtControlExpr != nullptr)
                {
                    gtDispChild(call->gtControlExpr, indentStack,
                                (call->gtControlExpr == lastChild) ? IIArcBottom : IIArc, "control expr", topOnly);
                }

                for (CallArg& arg : call->gtArgs.LateArgs())
                {
                    IndentInfo arcType = (arg.GetLateNext() == nullptr) ? IIArcBottom : IIArc;
                    gtGetLateArgMsg(call, &arg, buf, sizeof(buf));
                    gtDispChild(arg.GetLateNode(), indentStack, arcType, buf, topOnly);
                }
            }
        }
        break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
            if (tree->OperIs(GT_HWINTRINSIC))
            {
                printf(" %s %s", tree->AsHWIntrinsic()->GetSimdBaseType() == TYP_UNKNOWN
                                     ? ""
                                     : varTypeName(tree->AsHWIntrinsic()->GetSimdBaseType()),
                       HWIntrinsicInfo::lookupName(tree->AsHWIntrinsic()->GetHWIntrinsicId()));
            }

            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                size_t index = 0;
                size_t count = tree->AsMultiOp()->GetOperandCount();
                for (GenTree* operand : tree->AsMultiOp()->Operands())
                {
                    gtDispChild(operand, indentStack, ++index < count ? IIArc : IIArcBottom, nullptr, topOnly);
                }
            }
            break;
#endif // defined(FEATURE_HW_INTRINSICS)

        case GT_ARR_ELEM:
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                gtDispChild(tree->AsArrElem()->gtArrObj, indentStack, IIArc, nullptr, topOnly);

                unsigned dim;
                for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
                {
                    IndentInfo arcType = ((dim + 1) == tree->AsArrElem()->gtArrRank) ? IIArcBottom : IIArc;
                    gtDispChild(tree->AsArrElem()->gtArrInds[dim], indentStack, arcType, nullptr, topOnly);
                }
            }
            break;

        case GT_CMPXCHG:
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                gtDispChild(tree->AsCmpXchg()->Addr(), indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->AsCmpXchg()->Data(), indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->AsCmpXchg()->Comparand(), indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        case GT_STORE_DYN_BLK:
            if (tree->OperIsCopyBlkOp())
            {
                printf(" (copy)");
            }
            else if (tree->OperIsInitBlkOp())
            {
                printf(" (init)");
            }
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                gtDispChild(tree->AsStoreDynBlk()->Addr(), indentStack, IIArc, nullptr, topOnly);
                if (tree->AsStoreDynBlk()->Data() != nullptr)
                {
                    gtDispChild(tree->AsStoreDynBlk()->Data(), indentStack, IIArc, nullptr, topOnly);
                }
                gtDispChild(tree->AsStoreDynBlk()->gtDynamicSize, indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        case GT_SELECT:
            gtDispCommonEndLine(tree);

            if (!topOnly)
            {
                gtDispChild(tree->AsConditional()->gtCond, indentStack, IIArc, childMsg, topOnly);
                gtDispChild(tree->AsConditional()->gtOp1, indentStack, IIArc, childMsg, topOnly);
                gtDispChild(tree->AsConditional()->gtOp2, indentStack, IIArcBottom, childMsg, topOnly);
            }
            break;

        default:
            if (tree->OperIsLeaf())
            {
                gtDispLeaf(tree, indentStack);
                gtDispCommonEndLine(tree);
            }
            else
            {
                printf("<DON'T KNOW HOW TO DISPLAY THIS NODE> :");
                printf(""); // null string means flush
            }
            break;
    }
}

//------------------------------------------------------------------------
// gtGetWellKnownArgNameForArgMsg: Get a short descriptor of a well-known arg kind.
//
const char* Compiler::gtGetWellKnownArgNameForArgMsg(WellKnownArg arg)
{
    switch (arg)
    {
        case WellKnownArg::ThisPointer:
            return "this";
        case WellKnownArg::VarArgsCookie:
            return "va cookie";
        case WellKnownArg::InstParam:
            return "gctx";
        case WellKnownArg::RetBuffer:
            return "retbuf";
        case WellKnownArg::PInvokeFrame:
            return "pinv frame";
        case WellKnownArg::SecretStubParam:
            return "stub param";
        case WellKnownArg::WrapperDelegateCell:
            return "wrap cell";
        case WellKnownArg::ShiftLow:
            return "shift low";
        case WellKnownArg::ShiftHigh:
            return "shift high";
        case WellKnownArg::VirtualStubCell:
            return "vsd cell";
        case WellKnownArg::PInvokeCookie:
            return "pinv cookie";
        case WellKnownArg::PInvokeTarget:
            return "pinv tgt";
        case WellKnownArg::R2RIndirectionCell:
            return "r2r cell";
        case WellKnownArg::ValidateIndirectCallTarget:
        case WellKnownArg::DispatchIndirectCallTarget:
            return "cfg tgt";
        default:
            return nullptr;
    }
}

//------------------------------------------------------------------------
// gtPrintArgPrefix: Print a description of an argument into the specified buffer.
//
// Remarks:
//   For well-known arguments this prints a human-readable description.
//   Otherwise it prints e.g. "arg3".
//
void Compiler::gtPrintArgPrefix(GenTreeCall* call, CallArg* arg, char** bufp, unsigned* bufLength)
{
    int         prefLen;
    const char* wellKnownName = gtGetWellKnownArgNameForArgMsg(arg->GetWellKnownArg());
    if (wellKnownName != nullptr)
    {
        prefLen = sprintf_s(*bufp, *bufLength, "%s", wellKnownName);
    }
    else
    {
        unsigned argNum = call->gtArgs.GetIndex(arg);
        prefLen         = sprintf_s(*bufp, *bufLength, "arg%u", argNum);
    }
    assert(prefLen != -1);
    *bufp += prefLen;
    *bufLength -= (unsigned)prefLen;
}

//------------------------------------------------------------------------
// gtGetArgMsg: Construct a message about the given argument
//
// Arguments:
//    call      - The call for which 'arg' is an argument
//    arg       - The argument for which a message should be constructed
//    bufp      - A pointer to the buffer into which the message is written
//    bufLength - The length of the buffer pointed to by bufp
//
// Return Value:
//    No return value, but bufp is written.
//
void Compiler::gtGetArgMsg(GenTreeCall* call, CallArg* arg, char* bufp, unsigned bufLength)
{
    gtPrintArgPrefix(call, arg, &bufp, &bufLength);

    if (arg->GetLateNode() != nullptr)
    {
        sprintf_s(bufp, bufLength, " setup");
    }
    else if (call->gtArgs.IsAbiInformationDetermined())
    {
#ifdef TARGET_ARM
        if (arg->AbiInfo.IsSplit())
        {
            regNumber firstReg = arg->AbiInfo.GetRegNum();
            if (arg->AbiInfo.NumRegs == 1)
            {
                sprintf_s(bufp, bufLength, " %s out+%02x", compRegVarName(firstReg), arg->AbiInfo.ByteOffset);
            }
            else
            {
                regNumber lastReg   = REG_STK;
                char      separator = (arg->AbiInfo.NumRegs == 2) ? ',' : '-';
                if (arg->AbiInfo.IsHfaRegArg())
                {
                    unsigned lastRegNum = genMapFloatRegNumToRegArgNum(firstReg) + arg->AbiInfo.NumRegs - 1;
                    lastReg             = genMapFloatRegArgNumToRegNum(lastRegNum);
                }
                else
                {
                    unsigned lastRegNum = genMapIntRegNumToRegArgNum(firstReg) + arg->AbiInfo.NumRegs - 1;
                    lastReg             = genMapIntRegArgNumToRegNum(lastRegNum);
                }
                sprintf_s(bufp, bufLength, " %s%c%s out+%02x", compRegVarName(firstReg), separator,
                          compRegVarName(lastReg), arg->AbiInfo.ByteOffset);
            }

            return;
        }
#endif // TARGET_ARM
#if FEATURE_FIXED_OUT_ARGS
        sprintf_s(bufp, bufLength, " out+%02x", arg->AbiInfo.ByteOffset);
#else
        sprintf_s(bufp, bufLength, " on STK");
#endif
    }
}

//------------------------------------------------------------------------
// gtGetLateArgMsg: Construct a message about the given argument
//
// Arguments:
//    call      - The call for which 'arg' is an argument
//    arg       - The argument for which a message should be constructed
//    bufp      - A pointer to the buffer into which the message is written
//    bufLength - The length of the buffer pointed to by bufp
//
// Return Value:
//    No return value, but bufp is written.

void Compiler::gtGetLateArgMsg(GenTreeCall* call, CallArg* arg, char* bufp, unsigned bufLength)
{
    assert(arg->GetLateNode() != nullptr);
    regNumber argReg = arg->AbiInfo.GetRegNum();

    gtPrintArgPrefix(call, arg, &bufp, &bufLength);

#if FEATURE_FIXED_OUT_ARGS
    if (argReg == REG_STK)
    {
        sprintf_s(bufp, bufLength, " in out+%02x", arg->AbiInfo.ByteOffset);
    }
    else
#endif
    {
#ifdef TARGET_ARM
        if (arg->AbiInfo.IsSplit())
        {
            regNumber firstReg = arg->AbiInfo.GetRegNum();
            if (arg->AbiInfo.NumRegs == 1)
            {
                sprintf_s(bufp, bufLength, " %s out+%02x", compRegVarName(firstReg), arg->AbiInfo.ByteOffset);
            }
            else
            {
                regNumber lastReg   = REG_STK;
                char      separator = (arg->AbiInfo.NumRegs == 2) ? ',' : '-';
                if (arg->AbiInfo.IsHfaRegArg())
                {
                    unsigned lastRegNum = genMapFloatRegNumToRegArgNum(firstReg) + arg->AbiInfo.NumRegs - 1;
                    lastReg             = genMapFloatRegArgNumToRegNum(lastRegNum);
                }
                else
                {
                    unsigned lastRegNum = genMapIntRegNumToRegArgNum(firstReg) + arg->AbiInfo.NumRegs - 1;
                    lastReg             = genMapIntRegArgNumToRegNum(lastRegNum);
                }
                sprintf_s(bufp, bufLength, " %s%c%s out+%02x", compRegVarName(firstReg), separator,
                          compRegVarName(lastReg), arg->AbiInfo.ByteOffset);
            }

            return;
        }
#endif // TARGET_ARM
#if FEATURE_MULTIREG_ARGS
        if (arg->AbiInfo.NumRegs >= 2)
        {
            char separator = (arg->AbiInfo.NumRegs == 2) ? ',' : '-';
            sprintf_s(bufp, bufLength, " %s%c%s", compRegVarName(argReg), separator,
                      compRegVarName(arg->AbiInfo.GetRegNum(arg->AbiInfo.NumRegs - 1)));
        }
        else
#endif
        {
            sprintf_s(bufp, bufLength, " in %s", compRegVarName(argReg));
        }
    }
}

//------------------------------------------------------------------------
// gtDispArgList: Dump the tree for a call arg list
//
// Arguments:
//    call            - the call to dump arguments for
//    lastCallOperand - the call's last operand (to determine the arc types)
//    indentStack     - the specification for the current level of indentation & arcs
//
// Return Value:
//    None.
//
void Compiler::gtDispArgList(GenTreeCall* call, GenTree* lastCallOperand, IndentStack* indentStack)
{
    for (CallArg& arg : call->gtArgs.EarlyArgs())
    {
        char buf[256];
        gtGetArgMsg(call, &arg, buf, sizeof(buf));
        gtDispChild(arg.GetEarlyNode(), indentStack, (arg.GetEarlyNode() == lastCallOperand) ? IIArcBottom : IIArc, buf,
                    false);
    }
}

// gtDispStmt: Print a statement to jitstdout.
//
// Arguments:
//    stmt - the statement to be printed;
//    msg  - an additional message to print before the statement.
//
void Compiler::gtDispStmt(Statement* stmt, const char* msg /* = nullptr */)
{
    if (opts.compDbgInfo)
    {
        if (msg != nullptr)
        {
            printf("%s ", msg);
        }
        printStmtID(stmt);
        printf(" ( ");
        const DebugInfo& di = stmt->GetDebugInfo();
        // For statements in the root we display just the location without the
        // inline context info.
        if (di.GetInlineContext() == nullptr || di.GetInlineContext()->IsRoot())
        {
            di.GetLocation().Dump();
        }
        else
        {
            stmt->GetDebugInfo().Dump(false);
        }
        printf(" ... ");

        IL_OFFSET lastILOffs = stmt->GetLastILOffset();
        if (lastILOffs == BAD_IL_OFFSET)
        {
            printf("???");
        }
        else
        {
            printf("0x%03X", lastILOffs);
        }

        printf(" )");

        DebugInfo par;
        if (stmt->GetDebugInfo().GetParent(&par))
        {
            printf(" <- ");
            par.Dump(true);
        }

        printf("\n");
    }
    gtDispTree(stmt->GetRootNode());
}

//------------------------------------------------------------------------
// gtDispBlockStmts: dumps all statements inside `block`.
//
// Arguments:
//    block - the block to display statements for.
//
void Compiler::gtDispBlockStmts(BasicBlock* block)
{
    for (Statement* const stmt : block->Statements())
    {
        gtDispStmt(stmt);
        printf("\n");
    }
}

//------------------------------------------------------------------------
// Compiler::gtDispRange: dumps a range of LIR.
//
// Arguments:
//    range - the range of LIR to display.
//
void Compiler::gtDispRange(LIR::ReadOnlyRange const& range)
{
    for (GenTree* node : range)
    {
        gtDispLIRNode(node);
    }
}

//------------------------------------------------------------------------
// Compiler::gtDispTreeRange: dumps the LIR range that contains all of the
//                            nodes in the dataflow tree rooted at a given
//                            node.
//
// Arguments:
//    containingRange - the LIR range that contains the root node.
//    tree - the root of the dataflow tree.
//
void Compiler::gtDispTreeRange(LIR::Range& containingRange, GenTree* tree)
{
    bool     closed;
    unsigned sideEffects;
    gtDispRange(containingRange.GetTreeRangeWithFlags(tree, &closed, &sideEffects));
}

//------------------------------------------------------------------------
// Compiler::gtDispLIRNode: dumps a single LIR node.
//
// Arguments:
//    node - the LIR node to dump.
//    prefixMsg - an optional prefix for each line of output.
//
void Compiler::gtDispLIRNode(GenTree* node, const char* prefixMsg /* = nullptr */)
{
    auto displayOperand = [](GenTree* operand, const char* message, IndentInfo operandArc, IndentStack& indentStack,
                             size_t prefixIndent) {
        assert(operand != nullptr);
        assert(message != nullptr);

        if (prefixIndent != 0)
        {
            printf("%*s", (int)prefixIndent, "");
        }

        // 60 spaces for alignment
        printf("%-60s", "");
#if FEATURE_SET_FLAGS
        // additional flag enlarges the flag field by one character
        printf(" ");
#endif

        indentStack.Push(operandArc);
        indentStack.print();
        indentStack.Pop();
        operandArc = IIArc;

        printf("  t%-5d %-6s %s\n", operand->gtTreeID, varTypeName(operand->TypeGet()), message);
    };

    IndentStack indentStack(this);

    size_t prefixIndent = 0;
    if (prefixMsg != nullptr)
    {
        prefixIndent = strlen(prefixMsg);
    }

    const int bufLength = 256;
    char      buf[bufLength];

    const bool nodeIsCall = node->IsCall();

    // Visit operands
    IndentInfo operandArc = IIArcTop;
    for (GenTree* operand : node->Operands())
    {
        if (!operand->IsValue())
        {
            // Either of these situations may happen with calls.
            continue;
        }

        if (nodeIsCall)
        {
            GenTreeCall* call = node->AsCall();
            if (operand == call->gtCallAddr)
            {
                displayOperand(operand, "calli tgt", operandArc, indentStack, prefixIndent);
            }
            else if (operand == call->gtControlExpr)
            {
                displayOperand(operand, "control expr", operandArc, indentStack, prefixIndent);
            }
            else if (operand == call->gtCallCookie)
            {
                displayOperand(operand, "cookie", operandArc, indentStack, prefixIndent);
            }
            else
            {
                CallArg* curArg = call->gtArgs.FindByNode(operand);
                assert(curArg);

                if (operand == curArg->GetEarlyNode())
                {
                    gtGetArgMsg(call, curArg, buf, sizeof(buf));
                }
                else
                {
                    gtGetLateArgMsg(call, curArg, buf, sizeof(buf));
                }

                displayOperand(operand, buf, operandArc, indentStack, prefixIndent);
            }
        }
        else if (node->OperIs(GT_STORE_DYN_BLK))
        {
            if (operand == node->AsBlk()->Addr())
            {
                displayOperand(operand, "lhs", operandArc, indentStack, prefixIndent);
            }
            else if (operand == node->AsBlk()->Data())
            {
                displayOperand(operand, "rhs", operandArc, indentStack, prefixIndent);
            }
            else
            {
                assert(operand == node->AsStoreDynBlk()->gtDynamicSize);
                displayOperand(operand, "size", operandArc, indentStack, prefixIndent);
            }
        }
        else
        {
            displayOperand(operand, "", operandArc, indentStack, prefixIndent);
        }

        operandArc = IIArc;
    }

    // Visit the operator

    if (prefixMsg != nullptr)
    {
        printf("%s", prefixMsg);
    }

    const bool topOnly = true;
    const bool isLIR   = true;
    gtDispTree(node, &indentStack, nullptr, topOnly, isLIR);
}

/*****************************************************************************/
#endif // DEBUG

/*****************************************************************************
 *
 *  Check if the given node can be folded,
 *  and call the methods to perform the folding
 */

GenTree* Compiler::gtFoldExpr(GenTree* tree)
{
    unsigned kind = tree->OperKind();

    /* We must have a simple operation to fold */

    // If we're in CSE, it's not safe to perform tree
    // folding given that it can will potentially
    // change considered CSE candidates.
    if (optValnumCSE_phase)
    {
        return tree;
    }

    // NOTE: MinOpts() is always true for Tier0 so we have to check explicit flags instead.
    // To be fixed in https://github.com/dotnet/runtime/pull/77465
    const bool tier0opts = !opts.compDbgCode && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT);
    if (!tier0opts)
    {
        return tree;
    }

    if (!(kind & GTK_SMPOP))
    {
        if (tree->OperIsConditional())
        {
            return gtFoldExprConditional(tree);
        }
        return tree;
    }

    GenTree* op1 = tree->AsOp()->gtOp1;

    /* Filter out non-foldable trees that can have constant children */

    assert(kind & (GTK_UNOP | GTK_BINOP));
    switch (tree->gtOper)
    {
        case GT_RETFILT:
        case GT_RETURN:
        case GT_IND:
            return tree;
        default:
            break;
    }

    /* try to fold the current node */

    if ((kind & GTK_UNOP) && op1)
    {
        if (op1->OperIsConst())
        {
            return gtFoldExprConst(tree);
        }
    }
    else if ((kind & GTK_BINOP) && op1 && tree->AsOp()->gtOp2)
    {
        GenTree* op2 = tree->AsOp()->gtOp2;

        // The atomic operations are exempted here because they are never computable statically;
        // one of their arguments is an address.
        if (op1->OperIsConst() && op2->OperIsConst() && !tree->OperIsAtomicOp())
        {
            /* both nodes are constants - fold the expression */
            return gtFoldExprConst(tree);
        }
        else if (op1->OperIsConst() || op2->OperIsConst())
        {
            /* at least one is a constant - see if we have a
             * special operator that can use only one constant
             * to fold - e.g. booleans */

            return gtFoldExprSpecial(tree);
        }
        else if (tree->OperIsCompare())
        {
            /* comparisons of two local variables can sometimes be folded */

            return gtFoldExprCompare(tree);
        }
    }

    /* Return the original node (folded/bashed or not) */

    return tree;
}

//------------------------------------------------------------------------
// gtFoldExprCall: see if a call is foldable
//
// Arguments:
//    call - call to examine
//
// Returns:
//    The original call if no folding happened.
//    An alternative tree if folding happens.
//
// Notes:
//    Checks for calls to Type.op_Equality, Type.op_Inequality, and
//    Enum.HasFlag, and if the call is to one of these,
//    attempts to optimize.

GenTree* Compiler::gtFoldExprCall(GenTreeCall* call)
{
    // This may discard the call and will thus discard arg setup nodes that may
    // have arbitrary side effects, so we only support this being called before
    // args have been morphed.
    assert(!call->gtArgs.AreArgsComplete());

    // Can only fold calls to special intrinsics.
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) == 0)
    {
        return call;
    }

    // Defer folding if not optimizing.
    if (opts.OptimizationDisabled())
    {
        return call;
    }

    // Check for a new-style jit intrinsic.
    const NamedIntrinsic ni = lookupNamedIntrinsic(call->gtCallMethHnd);

    switch (ni)
    {
        case NI_System_Enum_HasFlag:
        {
            GenTree* thisOp = call->gtArgs.GetArgByIndex(0)->GetNode();
            GenTree* flagOp = call->gtArgs.GetArgByIndex(1)->GetNode();
            GenTree* result = gtOptimizeEnumHasFlag(thisOp, flagOp);

            if (result != nullptr)
            {
                return result;
            }
            break;
        }

        case NI_System_Type_op_Equality:
        case NI_System_Type_op_Inequality:
        {
            noway_assert(call->TypeGet() == TYP_INT);
            GenTree* op1 = call->gtArgs.GetArgByIndex(0)->GetNode();
            GenTree* op2 = call->gtArgs.GetArgByIndex(1)->GetNode();

            // If either operand is known to be a RuntimeType, this can be folded
            GenTree* result = gtFoldTypeEqualityCall(ni == NI_System_Type_op_Equality, op1, op2);
            if (result != nullptr)
            {
                return result;
            }
            break;
        }

        default:
            break;
    }

    return call;
}

//------------------------------------------------------------------------
// gtFoldTypeEqualityCall: see if a (potential) type equality call is foldable
//
// Arguments:
//    isEq -- is it == or != operator
//    op1  -- first argument to call
//    op2  -- second argument to call
//
// Returns:
//    nulltpr if no folding happened.
//    An alternative tree if folding happens.
//
// Notes:
//    If either operand is known to be a RuntimeType, then the type
//    equality methods will simply check object identity and so we can
//    fold the call into a simple compare of the call's operands.

GenTree* Compiler::gtFoldTypeEqualityCall(bool isEq, GenTree* op1, GenTree* op2)
{
    if ((gtGetTypeProducerKind(op1) == TPK_Unknown) && (gtGetTypeProducerKind(op2) == TPK_Unknown))
    {
        return nullptr;
    }

    const genTreeOps simpleOp = isEq ? GT_EQ : GT_NE;

    JITDUMP("\nFolding call to Type:op_%s to a simple compare via %s\n", isEq ? "Equality" : "Inequality",
            GenTree::OpName(simpleOp));

    GenTree* compare = gtNewOperNode(simpleOp, TYP_INT, op1, op2);

    return compare;
}

/*****************************************************************************
 *
 *  Some comparisons can be folded:
 *
 *    locA        == locA
 *    classVarA   == classVarA
 *    locA + locB == locB + locA
 *
 */

GenTree* Compiler::gtFoldExprCompare(GenTree* tree)
{
    GenTree* op1 = tree->AsOp()->gtOp1;
    GenTree* op2 = tree->AsOp()->gtOp2;

    assert(tree->OperIsCompare());

    /* Filter out cases that cannot be folded here */

    /* Do not fold floats or doubles (e.g. NaN != Nan) */

    if (varTypeIsFloating(op1->TypeGet()))
    {
        return tree;
    }

    // Currently we can only fold when the two subtrees exactly match
    // and everything is side effect free.
    //
    if (((tree->gtFlags & GTF_SIDE_EFFECT) != 0) || !GenTree::Compare(op1, op2, true))
    {
        // No folding.
        //
        return tree;
    }

    // GTF_ORDER_SIDEEFF here may indicate volatile subtrees.
    // Or it may indicate a non-null assertion prop into an indir subtree.
    //
    // Check the operands.
    //
    if ((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0)
    {
        // If op1 is "volatle" and op2 is not, we can still fold.
        //
        const bool op1MayBeVolatile = (op1->gtFlags & GTF_ORDER_SIDEEFF) != 0;
        const bool op2MayBeVolatile = (op2->gtFlags & GTF_ORDER_SIDEEFF) != 0;

        if (!op1MayBeVolatile || op2MayBeVolatile)
        {
            // No folding.
            //
            return tree;
        }
    }

    GenTree* cons;

    switch (tree->gtOper)
    {
        case GT_EQ:
        case GT_LE:
        case GT_GE:
            cons = gtNewIconNode(true); /* Folds to GT_CNS_INT(true) */
            break;

        case GT_NE:
        case GT_LT:
        case GT_GT:
            cons = gtNewIconNode(false); /* Folds to GT_CNS_INT(false) */
            break;

        default:
            assert(!"Unexpected relOp");
            return tree;
    }

    /* The node has been folded into 'cons' */

    JITDUMP("\nFolding comparison with identical operands:\n");
    DISPTREE(tree);

    if (fgGlobalMorph)
    {
        fgMorphTreeDone(cons);
    }
    else
    {
        cons->gtNext = tree->gtNext;
        cons->gtPrev = tree->gtPrev;
    }

    JITDUMP("Bashed to %s:\n", cons->AsIntConCommon()->IconValue() ? "true" : "false");
    DISPTREE(cons);

    return cons;
}

//------------------------------------------------------------------------
// gtFoldExprConditional: see if a conditional is foldable
//
// Arguments:
//    tree - condition to examine
//
// Returns:
//    The original call if no folding happened.
//    An alternative tree if folding happens.
//
// Notes:
//    Supporting foldings are:
//      SELECT TRUE  X Y  ->  X
//      SELECT FALSE X Y  ->  Y
//      SELECT COND  X X  ->  X
//
GenTree* Compiler::gtFoldExprConditional(GenTree* tree)
{
    GenTree* cond = tree->AsConditional()->gtCond;
    GenTree* op1  = tree->AsConditional()->gtOp1;
    GenTree* op2  = tree->AsConditional()->gtOp2;

    assert(tree->OperIsConditional());

    // Check for a constant conditional
    if (cond->OperIsConst())
    {
        // Constant conditions must be folded away.

        JITDUMP("\nFolding conditional op with constant condition:\n");
        DISPTREE(tree);

        assert(cond->TypeIs(TYP_INT));

        GenTree* replacement = nullptr;
        if (cond->IsIntegralConst(0))
        {
            JITDUMP("Bashed to false path:\n");
            replacement = op2;
        }
        else
        {
            // Condition should never be a constant other than 0 or 1
            assert(cond->IsIntegralConst(1));
            JITDUMP("Bashed to true path:\n");
            replacement = op1;
        }

        if (fgGlobalMorph)
        {
            fgMorphTreeDone(replacement);
        }
        else
        {
            replacement->gtNext = tree->gtNext;
            replacement->gtPrev = tree->gtPrev;
        }
        DISPTREE(replacement);
        JITDUMP("\n");

        // If we bashed to a compare, try to fold that.
        if (replacement->OperIsCompare())
        {
            return gtFoldExprCompare(replacement);
        }

        return replacement;
    }

    assert(cond->OperIsCompare());

    if (((tree->gtFlags & GTF_SIDE_EFFECT) != 0) || !GenTree::Compare(op1, op2, true))
    {
        // No folding.
        return tree;
    }

    // GTF_ORDER_SIDEEFF here may indicate volatile subtrees.
    // Or it may indicate a non-null assertion prop into an indir subtree.
    if ((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0)
    {
        // If op1 is "volatile" and op2 is not, we can still fold.
        const bool op1MayBeVolatile = (op1->gtFlags & GTF_ORDER_SIDEEFF) != 0;
        const bool op2MayBeVolatile = (op2->gtFlags & GTF_ORDER_SIDEEFF) != 0;

        if (!op1MayBeVolatile || op2MayBeVolatile)
        {
            // No folding.
            return tree;
        }
    }

    JITDUMP("Bashed to first of two identical paths:\n");
    GenTree* replacement = op1;

    if (fgGlobalMorph)
    {
        fgMorphTreeDone(replacement);
    }
    else
    {
        replacement->gtNext = tree->gtNext;
        replacement->gtPrev = tree->gtPrev;
    }
    DISPTREE(replacement);
    JITDUMP("\n");

    return replacement;
}

//------------------------------------------------------------------------
// gtFoldTypeCompare: see if a type comparison can be further simplified
//
// Arguments:
//    tree -- tree possibly comparing types
//
// Returns:
//    An alternative tree if folding happens.
//    Original tree otherwise.
//
// Notes:
//    Checks for
//        typeof(...) == obj.GetType()
//        typeof(...) == typeof(...)
//        typeof(...) == null
//        obj1.GetType() == obj2.GetType()
//
//    And potentially optimizes away the need to obtain actual
//    RuntimeType objects to do the comparison.

GenTree* Compiler::gtFoldTypeCompare(GenTree* tree)
{
    // Only handle EQ and NE
    // (maybe relop vs null someday)
    const genTreeOps oper = tree->OperGet();
    if ((oper != GT_EQ) && (oper != GT_NE))
    {
        return tree;
    }

    // Screen for the right kinds of operands
    GenTree* const         op1     = tree->AsOp()->gtOp1;
    GenTree* const         op2     = tree->AsOp()->gtOp2;
    const TypeProducerKind op1Kind = gtGetTypeProducerKind(op1);
    const TypeProducerKind op2Kind = gtGetTypeProducerKind(op2);

    // Fold "typeof(handle) cmp null"
    if (((op2Kind == TPK_Null) && (op1Kind == TPK_Handle)) || ((op1Kind == TPK_Null) && (op2Kind == TPK_Handle)))
    {
        GenTree* call   = op1Kind == TPK_Handle ? op1 : op2;
        GenTree* handle = call->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
        if (gtGetHelperArgClassHandle(handle) != NO_CLASS_HANDLE)
        {
            return oper == GT_EQ ? gtNewFalse() : gtNewTrue();
        }
    }

    // If both types are created via handles, we can simply compare
    // handles instead of the types that they'd create.
    if ((op1Kind == TPK_Handle) && (op2Kind == TPK_Handle))
    {
        JITDUMP("Optimizing compare of types-from-handles to instead compare handles\n");
        assert((tree->AsOp()->gtGetOp1()->AsCall()->gtArgs.CountArgs() == 1) &&
               (tree->AsOp()->gtGetOp2()->AsCall()->gtArgs.CountArgs() == 1));
        GenTree* op1ClassFromHandle  = tree->AsOp()->gtGetOp1()->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
        GenTree* op2ClassFromHandle  = tree->AsOp()->gtGetOp2()->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
        CORINFO_CLASS_HANDLE cls1Hnd = NO_CLASS_HANDLE;
        CORINFO_CLASS_HANDLE cls2Hnd = NO_CLASS_HANDLE;

        // Try and find class handles from op1 and op2
        cls1Hnd = gtGetHelperArgClassHandle(op1ClassFromHandle);
        cls2Hnd = gtGetHelperArgClassHandle(op2ClassFromHandle);

        // If we have both class handles, try and resolve the type equality test completely.
        bool resolveFailed = false;

        if ((cls1Hnd != NO_CLASS_HANDLE) && (cls2Hnd != NO_CLASS_HANDLE))
        {
            JITDUMP("Asking runtime to compare %p (%s) and %p (%s) for equality\n", dspPtr(cls1Hnd),
                    eeGetClassName(cls1Hnd), dspPtr(cls2Hnd), eeGetClassName(cls2Hnd));
            TypeCompareState s = info.compCompHnd->compareTypesForEquality(cls1Hnd, cls2Hnd);

            if (s != TypeCompareState::May)
            {
                // Type comparison result is known.
                const bool typesAreEqual = (s == TypeCompareState::Must);
                const bool operatorIsEQ  = (oper == GT_EQ);
                const int  compareResult = operatorIsEQ ^ typesAreEqual ? 0 : 1;
                JITDUMP("Runtime reports comparison is known at jit time: %u\n", compareResult);
                GenTree* result = gtNewIconNode(compareResult);
                return result;
            }
            else
            {
                resolveFailed = true;
            }
        }

        if (resolveFailed)
        {
            JITDUMP("Runtime reports comparison is NOT known at jit time\n");
        }
        else
        {
            JITDUMP("Could not find handle for %s%s\n", (cls1Hnd == NO_CLASS_HANDLE) ? " cls1" : "",
                    (cls2Hnd == NO_CLASS_HANDLE) ? " cls2" : "");
        }

        GenTree* compare = gtNewOperNode(oper, TYP_INT, op1ClassFromHandle, op2ClassFromHandle);

        // Drop any now-irrelevant flags
        compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

        return compare;
    }

    if ((op1Kind == TPK_GetType) && (op2Kind == TPK_GetType))
    {
        GenTree* arg1;

        if (op1->OperGet() == GT_INTRINSIC)
        {
            arg1 = op1->AsUnOp()->gtOp1;
        }
        else
        {
            arg1 = op1->AsCall()->gtArgs.GetThisArg()->GetNode();
        }

        arg1 = gtNewMethodTableLookup(arg1);

        GenTree* arg2;

        if (op2->OperGet() == GT_INTRINSIC)
        {
            arg2 = op2->AsUnOp()->gtOp1;
        }
        else
        {
            arg2 = op2->AsCall()->gtArgs.GetThisArg()->GetNode();
        }

        arg2 = gtNewMethodTableLookup(arg2);

        GenTree* compare = gtNewOperNode(oper, TYP_INT, arg1, arg2);

        // Drop any now-irrelevant flags
        compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

        return compare;
    }

    // If one operand creates a type from a handle and the other operand is fetching the type from an object,
    // we can sometimes optimize the type compare into a simpler
    // method table comparison.
    //
    // TODO: if other operand is null...
    if (!(((op1Kind == TPK_GetType) && (op2Kind == TPK_Handle)) ||
          ((op1Kind == TPK_Handle) && (op2Kind == TPK_GetType))))
    {
        return tree;
    }

    GenTree* const opHandle = (op1Kind == TPK_Handle) ? op1 : op2;
    GenTree* const opOther  = (op1Kind == TPK_Handle) ? op2 : op1;

    // Tunnel through the handle operand to get at the class handle involved.
    GenTree* const       opHandleArgument = opHandle->AsCall()->gtArgs.GetArgByIndex(0)->GetNode();
    CORINFO_CLASS_HANDLE clsHnd           = gtGetHelperArgClassHandle(opHandleArgument);

    // If we couldn't find the class handle, give up.
    if (clsHnd == NO_CLASS_HANDLE)
    {
        return tree;
    }

    // We're good to go.
    JITDUMP("Optimizing compare of obj.GetType()"
            " and type-from-handle to compare method table pointer\n");

    // opHandleArgument is the method table we're looking for.
    GenTree* const knownMT = opHandleArgument;

    // Fetch object method table from the object itself.
    GenTree* objOp = nullptr;

    // Note we may see intrinsified or regular calls to GetType
    if (opOther->OperGet() == GT_INTRINSIC)
    {
        objOp = opOther->AsUnOp()->gtOp1;
    }
    else
    {
        objOp = opOther->AsCall()->gtArgs.GetThisArg()->GetNode();
    }

    bool                 pIsExact   = false;
    bool                 pIsNonNull = false;
    CORINFO_CLASS_HANDLE objCls     = gtGetClassHandle(objOp, &pIsExact, &pIsNonNull);

    // if both classes are "final" (e.g. System.String[]) we can replace the comparison
    // with `true/false` + null check.
    if ((objCls != NO_CLASS_HANDLE) && (pIsExact || info.compCompHnd->isExactType(objCls)))
    {
        TypeCompareState tcs = info.compCompHnd->compareTypesForEquality(objCls, clsHnd);
        if (tcs != TypeCompareState::May)
        {
            const bool operatorIsEQ  = oper == GT_EQ;
            const bool typesAreEqual = tcs == TypeCompareState::Must;
            GenTree*   compareResult = gtNewIconNode((operatorIsEQ ^ typesAreEqual) ? 0 : 1);

            if (!pIsNonNull)
            {
                // we still have to emit a null-check
                // obj.GetType == typeof() -> (nullcheck) true/false
                GenTree* nullcheck = gtNewNullCheck(objOp, compCurBB);
                return gtNewOperNode(GT_COMMA, tree->TypeGet(), nullcheck, compareResult);
            }
            else if (objOp->gtFlags & GTF_ALL_EFFECT)
            {
                return gtNewOperNode(GT_COMMA, tree->TypeGet(), objOp, compareResult);
            }
            else
            {
                return compareResult;
            }
        }
    }

    // Fetch the method table from the object
    GenTree* const objMT = gtNewMethodTableLookup(objOp);

    // Compare the two method tables
    GenTree* const compare = gtNewOperNode(oper, TYP_INT, objMT, knownMT);

    // Drop any now irrelevant flags
    compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_DONT_CSE);

    // And we're done
    return compare;
}

//------------------------------------------------------------------------
// gtGetHelperArgClassHandle: find the compile time class handle from
//   a helper call argument tree
//
// Arguments:
//    tree - tree that passes the handle to the helper
//
// Returns:
//    The compile time class handle if known.
//
CORINFO_CLASS_HANDLE Compiler::gtGetHelperArgClassHandle(GenTree* tree)
{
    CORINFO_CLASS_HANDLE result = NO_CLASS_HANDLE;

    // The handle could be a literal constant
    if ((tree->OperGet() == GT_CNS_INT) && (tree->TypeGet() == TYP_I_IMPL))
    {
        assert(tree->IsIconHandle(GTF_ICON_CLASS_HDL));
        result = (CORINFO_CLASS_HANDLE)tree->AsIntCon()->gtCompileTimeHandle;
    }
    // Or the result of a runtime lookup
    else if (tree->OperGet() == GT_RUNTIMELOOKUP)
    {
        result = tree->AsRuntimeLookup()->GetClassHandle();
    }
    // Or something reached indirectly
    else if (tree->gtOper == GT_IND)
    {
        // The handle indirs we are looking for will be marked as non-faulting.
        // Certain others (eg from refanytype) may not be.
        if (tree->gtFlags & GTF_IND_NONFAULTING)
        {
            GenTree* handleTreeInternal = tree->AsOp()->gtOp1;

            if ((handleTreeInternal->OperGet() == GT_CNS_INT) && (handleTreeInternal->TypeGet() == TYP_I_IMPL))
            {
                // These handle constants should be class handles.
                assert(handleTreeInternal->IsIconHandle(GTF_ICON_CLASS_HDL));
                result = (CORINFO_CLASS_HANDLE)handleTreeInternal->AsIntCon()->gtCompileTimeHandle;
            }
        }
    }

    return result;
}

//------------------------------------------------------------------------
// gtFoldExprSpecial -- optimize binary ops with one constant operand
//
// Arguments:
//   tree - tree to optimize
//
// Return value:
//   Tree (possibly modified at root or below), or a new tree
//   Any new tree is fully morphed, if necessary.
//
GenTree* Compiler::gtFoldExprSpecial(GenTree* tree)
{
    GenTree*   op1  = tree->AsOp()->gtOp1;
    GenTree*   op2  = tree->AsOp()->gtOp2;
    genTreeOps oper = tree->OperGet();

    GenTree* op;
    GenTree* cons;
    ssize_t  val;

    assert(tree->OperKind() & GTK_BINOP);

    /* Filter out operators that cannot be folded here */
    if (oper == GT_CAST)
    {
        return tree;
    }

    /* We only consider TYP_INT for folding
     * Do not fold pointer arithmetic (e.g. addressing modes!) */

    if (oper != GT_QMARK && !varTypeIsIntOrI(tree->gtType))
    {
        return tree;
    }

    /* Find out which is the constant node */

    if (op1->IsCnsIntOrI())
    {
        op   = op2;
        cons = op1;
    }
    else if (op2->IsCnsIntOrI())
    {
        op   = op1;
        cons = op2;
    }
    else
    {
        return tree;
    }

    /* Get the constant value */

    val = cons->AsIntConCommon()->IconValue();

    // Transforms that would drop op cannot be performed if op has side effects
    bool opHasSideEffects = (op->gtFlags & GTF_SIDE_EFFECT) != 0;

    // Helper function that creates a new IntCon node and morphs it, if required
    auto NewMorphedIntConNode = [&](int value) -> GenTreeIntCon* {
        GenTreeIntCon* icon = gtNewIconNode(value);
        if (fgGlobalMorph)
        {
            fgMorphTreeDone(icon);
        }
        return icon;
    };

    auto NewZeroExtendNode = [&](var_types type, GenTree* op1, var_types castToType) -> GenTree* {
        assert(varTypeIsIntegral(type));
        assert(!varTypeIsSmall(type));
        assert(!varTypeIsUnsigned(type));
        assert(varTypeIsUnsigned(castToType));

        GenTreeCast* cast = gtNewCastNode(TYP_INT, op1, false, castToType);
        if (fgGlobalMorph)
        {
            fgMorphTreeDone(cast);
        }

        if (type == TYP_LONG)
        {
            cast = gtNewCastNode(TYP_LONG, cast, true, TYP_LONG);
            if (fgGlobalMorph)
            {
                fgMorphTreeDone(cast);
            }
        }

        return cast;
    };

    // Here `op` is the non-constant operand, `cons` is the constant operand
    // and `val` is the constant value.

    switch (oper)
    {
        case GT_LE:
            if (tree->IsUnsigned() && (val == 0) && (op1 == cons) && !opHasSideEffects)
            {
                // unsigned (0 <= x) is always true
                op = NewMorphedIntConNode(1);
                goto DONE_FOLD;
            }
            break;

        case GT_GE:
            if (tree->IsUnsigned() && (val == 0) && (op2 == cons) && !opHasSideEffects)
            {
                // unsigned (x >= 0) is always true
                op = NewMorphedIntConNode(1);
                goto DONE_FOLD;
            }
            break;

        case GT_LT:
            if (tree->IsUnsigned() && (val == 0) && (op2 == cons) && !opHasSideEffects)
            {
                // unsigned (x < 0) is always false
                op = NewMorphedIntConNode(0);
                goto DONE_FOLD;
            }
            break;

        case GT_GT:
            if (tree->IsUnsigned() && (val == 0) && (op1 == cons) && !opHasSideEffects)
            {
                // unsigned (0 > x) is always false
                op = NewMorphedIntConNode(0);
                goto DONE_FOLD;
            }
            FALLTHROUGH;
        case GT_EQ:
        case GT_NE:

            // Optimize boxed value classes; these are always false.  This IL is
            // generated when a generic value is tested against null:
            //     <T> ... foo(T x) { ... if ((object)x == null) ...
            if ((val == 0) && op->IsBoxedValue())
            {
                JITDUMP("\nAttempting to optimize BOX(valueType) %s null [%06u]\n", GenTree::OpName(oper),
                        dspTreeID(tree));

                // We don't expect GT_GT with signed compares, and we
                // can't predict the result if we do see it, since the
                // boxed object addr could have its high bit set.
                if ((oper == GT_GT) && !tree->IsUnsigned())
                {
                    JITDUMP(" bailing; unexpected signed compare via GT_GT\n");
                }
                else
                {
                    // The tree under the box must be side effect free
                    // since we will drop it if we optimize.
                    assert(!gtTreeHasSideEffects(op->AsBox()->BoxOp(), GTF_SIDE_EFFECT));

                    // See if we can optimize away the box and related statements.
                    GenTree* boxSourceTree = gtTryRemoveBoxUpstreamEffects(op);
                    bool     didOptimize   = (boxSourceTree != nullptr);

                    // If optimization succeeded, remove the box.
                    if (didOptimize)
                    {
                        // Set up the result of the compare.
                        int compareResult = 0;
                        if (oper == GT_GT)
                        {
                            // GT_GT(null, box) == false
                            // GT_GT(box, null) == true
                            compareResult = (op1 == op);
                        }
                        else if (oper == GT_EQ)
                        {
                            // GT_EQ(box, null) == false
                            // GT_EQ(null, box) == false
                            compareResult = 0;
                        }
                        else
                        {
                            assert(oper == GT_NE);
                            // GT_NE(box, null) == true
                            // GT_NE(null, box) == true
                            compareResult = 1;
                        }

                        JITDUMP("\nSuccess: replacing BOX(valueType) %s null with %d\n", GenTree::OpName(oper),
                                compareResult);

                        return NewMorphedIntConNode(compareResult);
                    }
                }
            }
            else
            {
                return gtFoldBoxNullable(tree);
            }

            break;

        case GT_ADD:
            if (val == 0)
            {
                goto DONE_FOLD;
            }
            break;

        case GT_MUL:
            if (val == 1)
            {
                goto DONE_FOLD;
            }
            else if (val == 0)
            {
                /* Multiply by zero - return the 'zero' node, but not if side effects */
                if (!opHasSideEffects)
                {
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_DIV:
        case GT_UDIV:
            if ((op2 == cons) && (val == 1) && !op1->OperIsConst())
            {
                goto DONE_FOLD;
            }
            break;

        case GT_SUB:
            if ((op2 == cons) && (val == 0) && !op1->OperIsConst())
            {
                goto DONE_FOLD;
            }
            break;

        case GT_AND:
            if (val == 0)
            {
                /* AND with zero - return the 'zero' node, but not if side effects */

                if (!opHasSideEffects)
                {
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            else if (val == 0xFF)
            {
                op = NewZeroExtendNode(tree->TypeGet(), op, TYP_UBYTE);
                goto DONE_FOLD;
            }
            else if (val == 0xFFFF)
            {
                op = NewZeroExtendNode(tree->TypeGet(), op, TYP_USHORT);
                goto DONE_FOLD;
            }
            else if ((val == 0xFFFFFFFF) && varTypeIsLong(tree))
            {
                op = NewZeroExtendNode(tree->TypeGet(), op, TYP_UINT);
                goto DONE_FOLD;
            }
            else
            {
                /* The GTF_BOOLEAN flag is set for nodes that are part
                 * of a boolean expression, thus all their children
                 * are known to evaluate to only 0 or 1 */

                if (tree->gtFlags & GTF_BOOLEAN)
                {

                    /* The constant value must be 1
                     * AND with 1 stays the same */
                    assert(val == 1);
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_OR:
            if (val == 0)
            {
                goto DONE_FOLD;
            }
            else if (tree->gtFlags & GTF_BOOLEAN)
            {
                /* The constant value must be 1 - OR with 1 is 1 */

                assert(val == 1);

                /* OR with one - return the 'one' node, but not if side effects */

                if (!opHasSideEffects)
                {
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        case GT_ROL:
        case GT_ROR:
            if (val == 0)
            {
                if (op2 == cons)
                {
                    goto DONE_FOLD;
                }
                else if (!opHasSideEffects)
                {
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_QMARK:
        {
            assert(op1 == cons && op2 == op && op2->gtOper == GT_COLON);
            assert(op2->AsOp()->gtOp1 && op2->AsOp()->gtOp2);

            assert(val == 0 || val == 1);

            if (val)
            {
                op = op2->AsColon()->ThenNode();
            }
            else
            {
                op = op2->AsColon()->ElseNode();
            }

            // Clear colon flags only if the qmark itself is not conditionally executed
            if ((tree->gtFlags & GTF_COLON_COND) == 0)
            {
                fgWalkTreePre(&op, gtClearColonCond);
            }
        }

            goto DONE_FOLD;

        default:
            break;
    }

    /* The node is not foldable */

    return tree;

DONE_FOLD:

    JITDUMP("\nFolding binary operator with a constant operand:\n");
    DISPTREE(tree);
    JITDUMP("Transformed into:\n");
    DISPTREE(op);

    return op;
}

//------------------------------------------------------------------------
// gtFoldBoxNullable -- optimize a boxed nullable feeding a compare to zero
//
// Arguments:
//   tree - binop tree to potentially optimize, must be
//          GT_GT, GT_EQ, or GT_NE
//
// Return value:
//   Tree (possibly modified below the root).
//
GenTree* Compiler::gtFoldBoxNullable(GenTree* tree)
{
    assert(tree->OperKind() & GTK_BINOP);
    assert(tree->OperIs(GT_GT, GT_EQ, GT_NE));

    genTreeOps const oper = tree->OperGet();

    if ((oper == GT_GT) && !tree->IsUnsigned())
    {
        return tree;
    }

    GenTree* const op1 = tree->AsOp()->gtOp1;
    GenTree* const op2 = tree->AsOp()->gtOp2;
    GenTree*       op;
    GenTree*       cons;

    if (op1->IsCnsIntOrI())
    {
        op   = op2;
        cons = op1;
    }
    else if (op2->IsCnsIntOrI())
    {
        op   = op1;
        cons = op2;
    }
    else
    {
        return tree;
    }

    ssize_t const val = cons->AsIntConCommon()->IconValue();

    if (val != 0)
    {
        return tree;
    }

    if (!op->IsCall())
    {
        return tree;
    }

    GenTreeCall* const call = op->AsCall();

    if (!call->IsHelperCall(this, CORINFO_HELP_BOX_NULLABLE))
    {
        return tree;
    }

    if (call->gtArgs.AreArgsComplete())
    {
        // We cannot handle folding the call away when remorphing.
        return tree;
    }

    JITDUMP("\nReplacing BOX_NULLABLE(&x) %s null [%06u] with x.hasValue\n", GenTree::OpName(oper), dspTreeID(tree));

    static_assert_no_msg(OFFSETOF__CORINFO_NullableOfT__hasValue == 0);
    GenTree* srcAddr      = call->gtArgs.GetArgByIndex(1)->GetNode();
    GenTree* hasValueNode = gtNewIndir(TYP_UBYTE, srcAddr);

    if (op == op1)
    {
        tree->AsOp()->gtOp1 = hasValueNode;
    }
    else
    {
        tree->AsOp()->gtOp2 = hasValueNode;
    }

    cons->gtType = TYP_INT;

    return tree;
}

//------------------------------------------------------------------------
// gtTryRemoveBoxUpstreamEffects: given an unused value type box,
//    try and remove the upstream allocation and unnecessary parts of
//    the copy.
//
// Arguments:
//    op  - the box node to optimize
//    options - controls whether and how trees are modified
//        (see notes)
//
// Return Value:
//    A tree representing the original value to box, if removal
//    is successful/possible (but see note). nullptr if removal fails.
//
// Notes:
//    Value typed box gets special treatment because it has associated
//    side effects that can be removed if the box result is not used.
//
//    By default (options == BR_REMOVE_AND_NARROW) this method will
//    try and remove unnecessary trees and will try and reduce remaining
//    operations to the minimal set, possibly narrowing the width of
//    loads from the box source if it is a struct.
//
//    To perform a trial removal, pass BR_DONT_REMOVE. This can be
//    useful to determine if this optimization should only be
//    performed if some other conditions hold true.
//
//    To remove but not alter the access to the box source, pass
//    BR_REMOVE_BUT_NOT_NARROW.
//
//    To remove and return the tree for the type handle used for
//    the boxed newobj, pass BR_REMOVE_BUT_NOT_NARROW_WANT_TYPE_HANDLE.
//    This can be useful when the only part of the box that is "live"
//    is its type.
//
//    If removal fails, it is possible that a subsequent pass may be
//    able to optimize.  Blocking side effects may now be minimized
//    (null or bounds checks might have been removed) or might be
//    better known (inline return placeholder updated with the actual
//    return expression). So the box is perhaps best left as is to
//    help trigger this re-examination.

GenTree* Compiler::gtTryRemoveBoxUpstreamEffects(GenTree* op, BoxRemovalOptions options)
{
    assert(op->IsBoxedValue());

    // grab related parts for the optimization
    GenTreeBox* box       = op->AsBox();
    Statement*  allocStmt = box->gtDefStmtWhenInlinedBoxValue;
    Statement*  copyStmt  = box->gtCopyStmtWhenInlinedBoxValue;

    JITDUMP("gtTryRemoveBoxUpstreamEffects: %s to %s of BOX (valuetype)"
            " [%06u] (assign/newobj " FMT_STMT " copy " FMT_STMT "\n",
            (options == BR_DONT_REMOVE) ? "checking if it is possible" : "attempting",
            (options == BR_MAKE_LOCAL_COPY) ? "make local unboxed version" : "remove side effects", dspTreeID(op),
            allocStmt->GetID(), copyStmt->GetID());

    // If we don't recognize the form of the store, bail.
    GenTree* boxLclDef = allocStmt->GetRootNode();
    if (!boxLclDef->OperIs(GT_STORE_LCL_VAR))
    {
        JITDUMP(" bailing; unexpected alloc def op %s\n", GenTree::OpName(boxLclDef->OperGet()));
        return nullptr;
    }

    // If this box is no longer single-use, bail.
    if (box->WasCloned())
    {
        JITDUMP(" bailing; unsafe to remove box that has been cloned\n");
        return nullptr;
    }

    // If we're eventually going to return the type handle, remember it now.
    GenTree* boxTypeHandle = nullptr;
    if ((options == BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE) || (options == BR_DONT_REMOVE_WANT_TYPE_HANDLE))
    {
        GenTree*   defSrc     = boxLclDef->AsLclVar()->Data();
        genTreeOps defSrcOper = defSrc->OperGet();

        // Allocation may be via AllocObj or via helper call, depending
        // on when this is invoked and whether the jit is using AllocObj
        // for R2R allocations.
        if (defSrcOper == GT_ALLOCOBJ)
        {
            GenTreeAllocObj* allocObj = defSrc->AsAllocObj();
            boxTypeHandle             = allocObj->AsOp()->gtOp1;
        }
        else if (defSrcOper == GT_CALL)
        {
            GenTreeCall* newobjCall = defSrc->AsCall();

            // In R2R expansions the handle may not be an explicit operand to the helper,
            // so we can't remove the box.
            if (newobjCall->gtArgs.IsEmpty())
            {
                assert(newobjCall->IsHelperCall(this, CORINFO_HELP_READYTORUN_NEW));
                JITDUMP(" bailing; newobj via R2R helper\n");
                return nullptr;
            }

            boxTypeHandle = newobjCall->gtArgs.GetArgByIndex(0)->GetNode();
        }
        else
        {
            unreached();
        }

        assert(boxTypeHandle != nullptr);
    }

    // If we don't recognize the form of the copy, bail.
    GenTree* copy = copyStmt->GetRootNode();
    if (!copy->OperIs(GT_STOREIND, GT_STORE_BLK))
    {
        // GT_RET_EXPR is a tolerable temporary failure.
        // The jit will revisit this optimization after
        // inlining is done.
        if (copy->gtOper == GT_RET_EXPR)
        {
            JITDUMP(" bailing; must wait for replacement of copy %s\n", GenTree::OpName(copy->gtOper));
        }
        else
        {
            // Anything else is a missed case we should figure out how to handle.
            // One known case is GT_COMMAs enclosing the store we are looking for.
            JITDUMP(" bailing; unexpected copy op %s\n", GenTree::OpName(copy->gtOper));
        }
        return nullptr;
    }

    // Handle case where we are optimizing the box into a local copy
    if (options == BR_MAKE_LOCAL_COPY)
    {
        // Drill into the box to get at the box temp local and the box type
        GenTree* boxTemp = box->BoxOp();
        assert(boxTemp->IsLocal());
        const unsigned boxTempLcl = boxTemp->AsLclVar()->GetLclNum();
        assert(lvaTable[boxTempLcl].lvType == TYP_REF);
        CORINFO_CLASS_HANDLE boxClass = lvaTable[boxTempLcl].lvClassHnd;
        assert(boxClass != nullptr);

        // Verify that the copy has the expected shape
        // (store_blk|store_ind (add (boxTempLcl, ptr-size)))
        //
        // The shape here is constrained to the patterns we produce
        // over in impImportAndPushBox for the inlined box case.
        //
        GenTree* copyDstAddr = copy->AsIndir()->Addr();
        if (copyDstAddr->OperGet() != GT_ADD)
        {
            JITDUMP("Unexpected copy dest address tree\n");
            return nullptr;
        }

        GenTree* copyDstAddrOp1 = copyDstAddr->AsOp()->gtOp1;
        if ((copyDstAddrOp1->OperGet() != GT_LCL_VAR) || (copyDstAddrOp1->AsLclVarCommon()->GetLclNum() != boxTempLcl))
        {
            JITDUMP("Unexpected copy dest address 1st addend\n");
            return nullptr;
        }

        GenTree* copyDstAddrOp2 = copyDstAddr->AsOp()->gtOp2;
        if (!copyDstAddrOp2->IsIntegralConst(TARGET_POINTER_SIZE))
        {
            JITDUMP("Unexpected copy dest address 2nd addend\n");
            return nullptr;
        }

        // Screening checks have all passed. Do the transformation.
        //
        // Retype the box temp to be a struct
        JITDUMP("Retyping box temp V%02u to struct %s\n", boxTempLcl, eeGetClassName(boxClass));
        lvaTable[boxTempLcl].lvType   = TYP_UNDEF;
        const bool isUnsafeValueClass = false;
        lvaSetStruct(boxTempLcl, boxClass, isUnsafeValueClass);

        // Remove the newobj and assignment to box temp
        JITDUMP("Bashing NEWOBJ [%06u] to NOP\n", dspTreeID(boxLclDef));
        boxLclDef->gtBashToNOP();

        // Update the copy from the value to be boxed to the box temp
        copy->AsIndir()->Addr() = gtNewLclVarAddrNode(boxTempLcl, TYP_BYREF);

        // Return the address of the now-struct typed box temp
        GenTree* retValue = gtNewLclVarAddrNode(boxTempLcl, TYP_BYREF);

        return retValue;
    }

    // If the copy is a struct copy, make sure we know how to isolate any source side effects.
    GenTree* copySrc = copy->Data();

    // If the copy source is from a pending inline, wait for it to resolve.
    if (copySrc->gtOper == GT_RET_EXPR)
    {
        JITDUMP(" bailing; must wait for replacement of copy source %s\n", GenTree::OpName(copySrc->gtOper));
        return nullptr;
    }

    bool hasSrcSideEffect = false;
    bool isStructCopy     = false;

    if (gtTreeHasSideEffects(copySrc, GTF_SIDE_EFFECT))
    {
        hasSrcSideEffect = true;

        if (varTypeIsStruct(copySrc))
        {
            isStructCopy = true;

            if (!copySrc->OperIs(GT_IND, GT_BLK))
            {
                // We don't know how to handle other cases, yet.
                JITDUMP(" bailing; unexpected copy source struct op with side effect %s\n",
                        GenTree::OpName(copySrc->gtOper));
                return nullptr;
            }
        }
    }

    // If this was a trial removal, we're done.
    if (options == BR_DONT_REMOVE)
    {
        return copySrc;
    }

    if (options == BR_DONT_REMOVE_WANT_TYPE_HANDLE)
    {
        return boxTypeHandle;
    }

    // Otherwise, proceed with the optimization.
    //
    // Change the assignment expression to a NOP.
    JITDUMP("\nBashing NEWOBJ [%06u] to NOP\n", dspTreeID(boxLclDef));
    boxLclDef->gtBashToNOP();

    // Change the copy expression so it preserves key
    // source side effects.
    JITDUMP("\nBashing COPY [%06u]", dspTreeID(copy));

    if (!hasSrcSideEffect)
    {
        // If there were no copy source side effects just bash
        // the copy to a NOP.
        copy->gtBashToNOP();
        JITDUMP(" to NOP; no source side effects.\n");
    }
    else if (!isStructCopy)
    {
        // For scalar types, go ahead and produce the
        // value as the copy is fairly cheap and likely
        // the optimizer can trim things down to just the
        // minimal side effect parts.
        copyStmt->SetRootNode(copySrc);
        JITDUMP(" to scalar read via [%06u]\n", dspTreeID(copySrc));
    }
    else
    {
        // For struct types read the first byte of the source struct; there's
        // no need to read the entire thing, and no place to put it.
        assert(copySrc->OperIs(GT_BLK, GT_IND));
        copyStmt->SetRootNode(copySrc);

        if (options == BR_REMOVE_AND_NARROW || options == BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE)
        {
            JITDUMP(" to read first byte of struct via modified [%06u]\n", dspTreeID(copySrc));
            copySrc->ChangeOper(GT_IND);
            copySrc->ChangeType(TYP_BYTE);
        }
        else
        {
            JITDUMP(" to read entire struct via modified [%06u]\n", dspTreeID(copySrc));
        }
    }

    if (fgNodeThreading == NodeThreading::AllTrees)
    {
        fgSetStmtSeq(allocStmt);
        fgSetStmtSeq(copyStmt);
    }

    // Box effects were successfully optimized.

    if (options == BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE)
    {
        return boxTypeHandle;
    }
    else
    {
        return copySrc;
    }
}

//------------------------------------------------------------------------
// gtOptimizeEnumHasFlag: given the operands for a call to Enum.HasFlag,
//    try and optimize the call to a simple and/compare tree.
//
// Arguments:
//    thisOp  - first argument to the call
//    flagOp  - second argument to the call
//
// Return Value:
//    A new cmp/amd tree if successful. nullptr on failure.
//
// Notes:
//    If successful, may allocate new temps and modify connected
//    statements.

GenTree* Compiler::gtOptimizeEnumHasFlag(GenTree* thisOp, GenTree* flagOp)
{
    JITDUMP("Considering optimizing call to Enum.HasFlag....\n");

    // Operands must be boxes
    if (!thisOp->IsBoxedValue() || !flagOp->IsBoxedValue())
    {
        JITDUMP("bailing, need both inputs to be BOXes\n");
        return nullptr;
    }

    // Operands must have same type
    bool                 isExactThis   = false;
    bool                 isNonNullThis = false;
    CORINFO_CLASS_HANDLE thisHnd       = gtGetClassHandle(thisOp, &isExactThis, &isNonNullThis);

    if (thisHnd == nullptr)
    {
        JITDUMP("bailing, can't find type for 'this' operand\n");
        return nullptr;
    }

    // A boxed thisOp should have exact type and non-null instance
    assert(isExactThis);
    assert(isNonNullThis);

    bool                 isExactFlag   = false;
    bool                 isNonNullFlag = false;
    CORINFO_CLASS_HANDLE flagHnd       = gtGetClassHandle(flagOp, &isExactFlag, &isNonNullFlag);

    if (flagHnd == nullptr)
    {
        JITDUMP("bailing, can't find type for 'flag' operand\n");
        return nullptr;
    }

    // A boxed flagOp should have exact type and non-null instance
    assert(isExactFlag);
    assert(isNonNullFlag);

    if (flagHnd != thisHnd)
    {
        JITDUMP("bailing, operand types differ\n");
        return nullptr;
    }

    // If we have a shared type instance we can't safely check type
    // equality, so bail.
    DWORD classAttribs = info.compCompHnd->getClassAttribs(thisHnd);
    if (classAttribs & CORINFO_FLG_SHAREDINST)
    {
        JITDUMP("bailing, have shared instance type\n");
        return nullptr;
    }

    // Simulate removing the box for thisOP. We need to know that it can
    // be safely removed before we can optimize.
    GenTree* thisVal = gtTryRemoveBoxUpstreamEffects(thisOp, BR_DONT_REMOVE);
    if (thisVal == nullptr)
    {
        // Note we may fail here if the this operand comes from
        // a call. We should be able to retry this post-inlining.
        JITDUMP("bailing, can't undo box of 'this' operand\n");
        return nullptr;
    }

    // Do likewise with flagOp.
    GenTree* flagVal = gtTryRemoveBoxUpstreamEffects(flagOp, BR_DONT_REMOVE);
    if (flagVal == nullptr)
    {
        // Note we may fail here if the flag operand comes from
        // a call. We should be able to retry this post-inlining.
        JITDUMP("bailing, can't undo box of 'flag' operand\n");
        return nullptr;
    }

    // Only proceed when both box sources have the same actual type.
    // (this rules out long/int mismatches)
    if (genActualType(thisVal->TypeGet()) != genActualType(flagVal->TypeGet()))
    {
        JITDUMP("bailing, pre-boxed values have different types\n");
        return nullptr;
    }

    // Yes, both boxes can be cleaned up. Optimize.
    JITDUMP("Optimizing call to Enum.HasFlag\n");

    // Undo the boxing of the Ops and prepare to operate directly
    // on the pre-boxed values.
    thisVal = gtTryRemoveBoxUpstreamEffects(thisOp, BR_REMOVE_BUT_NOT_NARROW);
    flagVal = gtTryRemoveBoxUpstreamEffects(flagOp, BR_REMOVE_BUT_NOT_NARROW);

    // Our trial removals above should guarantee successful removals here.
    assert(thisVal != nullptr);
    assert(flagVal != nullptr);
    assert(genActualType(thisVal->TypeGet()) == genActualType(flagVal->TypeGet()));

    // Type to use for optimized check
    var_types type = genActualType(thisVal->TypeGet());

    // The thisVal and flagVal trees come from earlier statements.
    //
    // Unless they are invariant values, we need to evaluate them both
    // to temps at those points to safely transmit the values here.
    //
    // Also we need to use the flag twice, so we need two trees for it.
    GenTree* thisValOpt     = nullptr;
    GenTree* flagValOpt     = nullptr;
    GenTree* flagValOptCopy = nullptr;

    if (thisVal->IsIntegralConst())
    {
        thisValOpt = gtClone(thisVal);
        assert(thisValOpt != nullptr);
    }
    else
    {
        const unsigned thisTmp       = lvaGrabTemp(true DEBUGARG("Enum:HasFlag this temp"));
        GenTree*       thisStore     = gtNewTempStore(thisTmp, thisVal);
        Statement*     thisStoreStmt = thisOp->AsBox()->gtCopyStmtWhenInlinedBoxValue;
        thisStoreStmt->SetRootNode(thisStore);
        thisValOpt = gtNewLclvNode(thisTmp, type);
    }

    if (flagVal->IsIntegralConst())
    {
        flagValOpt = gtClone(flagVal);
        assert(flagValOpt != nullptr);
        flagValOptCopy = gtClone(flagVal);
        assert(flagValOptCopy != nullptr);
    }
    else
    {
        const unsigned flagTmp       = lvaGrabTemp(true DEBUGARG("Enum:HasFlag flag temp"));
        GenTree*       flagStore     = gtNewTempStore(flagTmp, flagVal);
        Statement*     flagStoreStmt = flagOp->AsBox()->gtCopyStmtWhenInlinedBoxValue;
        flagStoreStmt->SetRootNode(flagStore);
        flagValOpt     = gtNewLclvNode(flagTmp, type);
        flagValOptCopy = gtNewLclvNode(flagTmp, type);
    }

    // Turn the call into (thisValTmp & flagTmp) == flagTmp.
    GenTree* andTree = gtNewOperNode(GT_AND, type, thisValOpt, flagValOpt);
    GenTree* cmpTree = gtNewOperNode(GT_EQ, TYP_INT, andTree, flagValOptCopy);

    JITDUMP("Optimized call to Enum.HasFlag\n");

    return cmpTree;
}

/*****************************************************************************
 *
 *  Fold the given constant tree.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
GenTree* Compiler::gtFoldExprConst(GenTree* tree)
{
    SSIZE_T   i1, i2, itemp;
    INT64     lval1, lval2, ltemp;
    float     f1, f2;
    double    d1, d2;
    var_types switchType;
    FieldSeq* fieldSeq = nullptr; // default unless we override it when folding

    assert(tree->OperIsUnary() || tree->OperIsBinary());

    GenTree* op1 = tree->gtGetOp1();
    GenTree* op2 = tree->gtGetOp2IfPresent();

    // NOTE: MinOpts() is always true for Tier0 so we have to check explicit flags instead.
    // To be fixed in https://github.com/dotnet/runtime/pull/77465
    const bool tier0opts = !opts.compDbgCode && !opts.jitFlags->IsSet(JitFlags::JIT_FLAG_MIN_OPT);
    if (!tier0opts)
    {
        return tree;
    }

    if (tree->OperIs(GT_NOP, GT_ALLOCOBJ, GT_RUNTIMELOOKUP) || tree->OperIsStore() || tree->OperIsHWIntrinsic())
    {
        return tree;
    }

    if (tree->OperIsUnary())
    {
        assert(op1->OperIsConst());

        switch (op1->TypeGet())
        {
            case TYP_INT:

                // Fold constant INT unary operator.

                if (!op1->AsIntCon()->ImmedValCanBeFolded(this, tree->OperGet()))
                {
                    return tree;
                }

                i1 = (INT32)op1->AsIntCon()->IconValue();

                switch (tree->OperGet())
                {
                    case GT_NOT:
                        i1 = ~i1;
                        break;

                    case GT_NEG:
                        i1 = -i1;
                        break;

                    case GT_BSWAP:
                        i1 = ((i1 >> 24) & 0xFF) | ((i1 >> 8) & 0xFF00) | ((i1 << 8) & 0xFF0000) |
                             ((i1 << 24) & 0xFF000000);
                        break;

                    case GT_BSWAP16:
                        i1 = ((i1 >> 8) & 0xFF) | ((i1 << 8) & 0xFF00);
                        break;

                    case GT_CAST:
                        // assert (genActualType(tree->CastToType()) == tree->TypeGet());

                        if (tree->gtOverflow() &&
                            CheckedOps::CastFromIntOverflows((INT32)i1, tree->CastToType(), tree->IsUnsigned()))
                        {
                            goto INTEGRAL_OVF;
                        }

                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                i1 = INT32(INT8(i1));
                                goto CNS_INT;

                            case TYP_SHORT:
                                i1 = INT32(INT16(i1));
                                goto CNS_INT;

                            case TYP_USHORT:
                                i1 = INT32(UINT16(i1));
                                goto CNS_INT;

                            case TYP_UBYTE:
                                i1 = INT32(UINT8(i1));
                                goto CNS_INT;

                            case TYP_UINT:
                            case TYP_INT:
                                goto CNS_INT;

                            case TYP_ULONG:
                                if (tree->IsUnsigned())
                                {
                                    lval1 = UINT64(UINT32(i1));
                                }
                                else
                                {
                                    lval1 = UINT64(INT32(i1));
                                }
                                goto CNS_LONG;

                            case TYP_LONG:
                                if (tree->IsUnsigned())
                                {
                                    lval1 = INT64(UINT32(i1));
                                }
                                else
                                {
                                    lval1 = INT64(INT32(i1));
                                }
                                goto CNS_LONG;

                            case TYP_FLOAT:
                            {
#if defined(TARGET_64BIT)
                                if (tree->IsUnsigned())
                                {
                                    f1 = (float)UINT32(i1);
                                }
                                else
                                {
                                    f1 = (float)INT32(i1);
                                }
#else
                                // 32-bit currently does a 2-step conversion, which is incorrect
                                // but which we are going to take a breaking change around early
                                // in a release cycle.

                                if (tree->IsUnsigned())
                                {
                                    f1 = forceCastToFloat(UINT32(i1));
                                }
                                else
                                {
                                    f1 = forceCastToFloat(INT32(i1));
                                }
#endif

                                d1 = f1;
                                goto CNS_DOUBLE;
                            }

                            case TYP_DOUBLE:
                            {
                                if (tree->IsUnsigned())
                                {
                                    d1 = (double)UINT32(i1);
                                }
                                else
                                {
                                    d1 = (double)INT32(i1);
                                }
                                goto CNS_DOUBLE;
                            }

                            default:
                                assert(!"Bad CastToType() in gtFoldExprConst() for a cast from int");
                                return tree;
                        }

                    default:
                        return tree;
                }

                goto CNS_INT;

            case TYP_LONG:

                // Fold constant LONG unary operator.

                if (!op1->AsIntConCommon()->ImmedValCanBeFolded(this, tree->OperGet()))
                {
                    return tree;
                }

                lval1 = op1->AsIntConCommon()->LngValue();

                switch (tree->OperGet())
                {
                    case GT_NOT:
                        lval1 = ~lval1;
                        break;

                    case GT_NEG:
                        lval1 = -lval1;
                        break;

                    case GT_BSWAP:
                        lval1 = ((lval1 >> 56) & 0xFF) | ((lval1 >> 40) & 0xFF00) | ((lval1 >> 24) & 0xFF0000) |
                                ((lval1 >> 8) & 0xFF000000) | ((lval1 << 8) & 0xFF00000000) |
                                ((lval1 << 24) & 0xFF0000000000) | ((lval1 << 40) & 0xFF000000000000) |
                                ((lval1 << 56) & 0xFF00000000000000);
                        break;

                    case GT_CAST:
                        assert(tree->TypeIs(genActualType(tree->CastToType())));

                        if (tree->gtOverflow() &&
                            CheckedOps::CastFromLongOverflows(lval1, tree->CastToType(), tree->IsUnsigned()))
                        {
                            goto INTEGRAL_OVF;
                        }

                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                i1 = INT32(INT8(lval1));
                                goto CNS_INT;

                            case TYP_SHORT:
                                i1 = INT32(INT16(lval1));
                                goto CNS_INT;

                            case TYP_USHORT:
                                i1 = INT32(UINT16(lval1));
                                goto CNS_INT;

                            case TYP_UBYTE:
                                i1 = INT32(UINT8(lval1));
                                goto CNS_INT;

                            case TYP_INT:
                                i1 = INT32(lval1);
                                goto CNS_INT;

                            case TYP_UINT:
                                i1 = UINT32(lval1);
                                goto CNS_INT;

                            case TYP_ULONG:
                            case TYP_LONG:
                                goto CNS_LONG;

                            case TYP_FLOAT:
                            {
#if defined(TARGET_64BIT)
                                if (tree->IsUnsigned() && (lval1 < 0))
                                {
                                    f1 = FloatingPointUtils::convertUInt64ToFloat((unsigned __int64)lval1);
                                }
                                else
                                {
                                    f1 = (float)lval1;
                                }
#else
                                // 32-bit currently does a 2-step conversion, which is incorrect
                                // but which we are going to take a breaking change around early
                                // in a release cycle.

                                if (tree->IsUnsigned() && (lval1 < 0))
                                {
                                    f1 = forceCastToFloat(
                                        FloatingPointUtils::convertUInt64ToDouble((unsigned __int64)lval1));
                                }
                                else
                                {
                                    f1 = forceCastToFloat((double)lval1);
                                }
#endif

                                d1 = f1;
                                goto CNS_DOUBLE;
                            }

                            case TYP_DOUBLE:
                            {
                                if (tree->IsUnsigned() && (lval1 < 0))
                                {
                                    d1 = FloatingPointUtils::convertUInt64ToDouble((unsigned __int64)lval1);
                                }
                                else
                                {
                                    d1 = (double)lval1;
                                }
                                goto CNS_DOUBLE;
                            }
                            default:
                                assert(!"Bad CastToType() in gtFoldExprConst() for a cast from long");
                                return tree;
                        }

                    default:
                        return tree;
                }

                goto CNS_LONG;

            case TYP_FLOAT:
            case TYP_DOUBLE:
                assert(op1->OperIs(GT_CNS_DBL));

                // Fold constant DOUBLE unary operator.

                d1 = op1->AsDblCon()->DconValue();

                switch (tree->OperGet())
                {
                    case GT_NEG:
                        d1 = -d1;
                        break;

                    case GT_CAST:
                        f1 = forceCastToFloat(d1);

                        if ((op1->TypeIs(TYP_DOUBLE) && CheckedOps::CastFromDoubleOverflows(d1, tree->CastToType())) ||
                            (op1->TypeIs(TYP_FLOAT) && CheckedOps::CastFromFloatOverflows(f1, tree->CastToType())))
                        {
                            // The conversion overflows. The ECMA spec says, in III 3.27, that
                            // "...if overflow occurs converting a floating point type to an integer, ...,
                            // the value returned is unspecified."  However, it would at least be
                            // desirable to have the same value returned for casting an overflowing
                            // constant to an int as would be obtained by passing that constant as
                            // a parameter and then casting that parameter to an int type.

                            // Don't fold overflowing conversions, as the value returned by
                            // JIT's codegen doesn't always match with the C compiler's cast result.
                            // We want the behavior to be the same with or without folding.

                            return tree;
                        }

                        assert(tree->TypeIs(genActualType(tree->CastToType())));

                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                i1 = INT32(INT8(d1));
                                goto CNS_INT;

                            case TYP_SHORT:
                                i1 = INT32(INT16(d1));
                                goto CNS_INT;

                            case TYP_USHORT:
                                i1 = INT32(UINT16(d1));
                                goto CNS_INT;

                            case TYP_UBYTE:
                                i1 = INT32(UINT8(d1));
                                goto CNS_INT;

                            case TYP_INT:
                                i1 = INT32(d1);
                                goto CNS_INT;

                            case TYP_UINT:
                                i1 = forceCastToUInt32(d1);
                                goto CNS_INT;

                            case TYP_LONG:
                                lval1 = INT64(d1);
                                goto CNS_LONG;

                            case TYP_ULONG:
                                lval1 = FloatingPointUtils::convertDoubleToUInt64(d1);
                                goto CNS_LONG;

                            case TYP_FLOAT:
                                d1 = forceCastToFloat(d1);
                                goto CNS_DOUBLE;

                            case TYP_DOUBLE:
                                if (op1->TypeIs(TYP_FLOAT))
                                {
                                    d1 = forceCastToFloat(d1); // Truncate precision.
                                }
                                goto CNS_DOUBLE; // Redundant cast.

                            default:
                                assert(!"Bad CastToType() in gtFoldExprConst() for a cast from double/float");
                                break;
                        }
                        return tree;

                    default:
                        return tree;
                }
                goto CNS_DOUBLE;

            default:
                // Not a foldable typ - e.g. RET const.
                return tree;
        }
    }

    // We have a binary operator.

    assert(tree->OperIsBinary());
    assert(op2 != nullptr);
    assert(op1->OperIsConst());
    assert(op2->OperIsConst());

    if (tree->OperIs(GT_COMMA))
    {
        return op2;
    }

    if (tree->OperIs(GT_BOUNDS_CHECK))
    {
        ssize_t index  = op1->AsIntCon()->IconValue();
        ssize_t length = op2->AsIntCon()->IconValue();

        if ((0 <= index) && (index < length))
        {
            JITDUMP("\nFolding an in-range bounds check:\n");
            DISPTREE(tree);

            tree->gtBashToNOP();

            JITDUMP("Bashed to NOP:\n");
            DISPTREE(tree);
        }

        return tree;
    }

    switchType = op1->TypeGet();

    // Normally we will just switch on op1 types, but for the case where
    // only op2 is a GC type and op1 is not a GC type, we use the op2 type.
    // This makes us handle this as a case of folding for GC type.
    if (varTypeIsGC(op2->gtType) && !varTypeIsGC(op1->gtType))
    {
        switchType = op2->TypeGet();
    }

    switch (switchType)
    {
        // Fold constant REF of BYREF binary operator.
        // These can only be comparisons or null pointers.

        case TYP_REF:

            // String nodes are an RVA at this point.
            if (op1->OperIs(GT_CNS_STR) || op2->OperIs(GT_CNS_STR))
            {
                // Fold "ldstr" ==/!= null.
                if (op2->IsIntegralConst(0))
                {
                    if (tree->OperIs(GT_EQ))
                    {
                        i1 = 0;
                        goto FOLD_COND;
                    }
                    if (tree->OperIs(GT_NE) || (tree->OperIs(GT_GT) && tree->IsUnsigned()))
                    {
                        i1 = 1;
                        goto FOLD_COND;
                    }
                }
                return tree;
            }

            FALLTHROUGH;

        case TYP_BYREF:

            i1 = op1->AsIntConCommon()->IconValue();
            i2 = op2->AsIntConCommon()->IconValue();

            switch (tree->OperGet())
            {
                case GT_EQ:
                    i1 = (i1 == i2);
                    goto FOLD_COND;

                case GT_NE:
                    i1 = (i1 != i2);
                    goto FOLD_COND;

                case GT_ADD:
                    noway_assert(!tree->TypeIs(TYP_REF));
                    // We only fold a GT_ADD that involves a null reference.
                    if ((op1->TypeIs(TYP_REF) && (i1 == 0)) || (op2->TypeIs(TYP_REF) && (i2 == 0)))
                    {
                        JITDUMP("\nFolding operator with constant nodes into a constant:\n");
                        DISPTREE(tree);

                        // Fold into GT_IND of null byref.
                        tree->BashToConst(0, TYP_BYREF);
                        if (vnStore != nullptr)
                        {
                            fgValueNumberTreeConst(tree);
                        }

                        JITDUMP("\nFolded to null byref:\n");
                        DISPTREE(tree);

                        goto DONE;
                    }
                    break;

                default:
                    break;
            }

            return tree;

        // Fold constant INT binary operator.

        case TYP_INT:

            assert(tree->TypeIs(TYP_INT) || varTypeIsGC(tree) || tree->OperIs(GT_MKREFANY));
            // No GC pointer types should be folded here...
            assert(!varTypeIsGC(op1->TypeGet()) && !varTypeIsGC(op2->TypeGet()));

            if (!op1->AsIntConCommon()->ImmedValCanBeFolded(this, tree->OperGet()))
            {
                return tree;
            }

            if (!op2->AsIntConCommon()->ImmedValCanBeFolded(this, tree->OperGet()))
            {
                return tree;
            }

            i1 = op1->AsIntConCommon()->IconValue();
            i2 = op2->AsIntConCommon()->IconValue();

            switch (tree->OperGet())
            {
                case GT_EQ:
                    i1 = (INT32(i1) == INT32(i2));
                    break;
                case GT_NE:
                    i1 = (INT32(i1) != INT32(i2));
                    break;

                case GT_LT:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT32(i1) < UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) < INT32(i2));
                    }
                    break;

                case GT_LE:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT32(i1) <= UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) <= INT32(i2));
                    }
                    break;

                case GT_GE:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT32(i1) >= UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) >= INT32(i2));
                    }
                    break;

                case GT_GT:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT32(i1) > UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) > INT32(i2));
                    }
                    break;

                case GT_ADD:
                    itemp = i1 + i2;
                    if (tree->gtOverflow() && CheckedOps::AddOverflows(INT32(i1), INT32(i2), tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    i1       = itemp;
                    fieldSeq = GetFieldSeqStore()->Append(op1->AsIntCon()->gtFieldSeq, op2->AsIntCon()->gtFieldSeq);
                    break;
                case GT_SUB:
                    itemp = i1 - i2;
                    if (tree->gtOverflow() && CheckedOps::SubOverflows(INT32(i1), INT32(i2), tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    i1 = itemp;
                    break;
                case GT_MUL:
                    itemp = i1 * i2;
                    if (tree->gtOverflow() && CheckedOps::MulOverflows(INT32(i1), INT32(i2), tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    i1 = itemp;
                    break;

                case GT_OR:
                    i1 |= i2;
                    break;
                case GT_XOR:
                    i1 ^= i2;
                    break;
                case GT_AND:
                    i1 &= i2;
                    break;

                case GT_LSH:
                    i1 <<= (i2 & 0x1f);
                    break;
                case GT_RSH:
                    i1 >>= (i2 & 0x1f);
                    break;
                case GT_RSZ:
                    // logical shift -> make it unsigned to not propagate the sign bit.
                    i1 = UINT32(i1) >> (i2 & 0x1f);
                    break;
                case GT_ROL:
                    i1 = (i1 << (i2 & 0x1f)) | (UINT32(i1) >> ((32 - i2) & 0x1f));
                    break;
                case GT_ROR:
                    i1 = (i1 << ((32 - i2) & 0x1f)) | (UINT32(i1) >> (i2 & 0x1f));
                    break;

                // DIV and MOD can throw an exception - if the division is by 0
                // or there is overflow - when dividing MIN by -1.

                case GT_DIV:
                case GT_MOD:
                case GT_UDIV:
                case GT_UMOD:
                    if (INT32(i2) == 0)
                    {
                        // Division by zero.
                        // We have to evaluate this expression and throw an exception.
                        return tree;
                    }
                    else if ((INT32(i2) == -1) && (UINT32(i1) == 0x80000000))
                    {
                        // Overflow Division.
                        // We have to evaluate this expression and throw an exception.
                        return tree;
                    }

                    if (tree->OperIs(GT_DIV))
                    {
                        i1 = INT32(i1) / INT32(i2);
                    }
                    else if (tree->OperIs(GT_MOD))
                    {
                        i1 = INT32(i1) % INT32(i2);
                    }
                    else if (tree->OperIs(GT_UDIV))
                    {
                        i1 = UINT32(i1) / UINT32(i2);
                    }
                    else
                    {
                        assert(tree->OperIs(GT_UMOD));
                        i1 = UINT32(i1) % UINT32(i2);
                    }
                    break;

                default:
                    return tree;
            }

        // We get here after folding to a GT_CNS_INT type.
        // change the node to the new type / value and make sure the node sizes are OK.
        CNS_INT:
        FOLD_COND:

            JITDUMP("\nFolding operator with constant nodes into a constant:\n");
            DISPTREE(tree);

            // Also all conditional folding jumps here since the node hanging from
            // GT_JTRUE has to be a GT_CNS_INT - value 0 or 1.

            // Some operations are performed as 64 bit instead of 32 bit so the upper 32 bits
            // need to be discarded. Since constant values are stored as ssize_t and the node
            // has TYP_INT the result needs to be sign extended rather than zero extended.
            tree->BashToConst(static_cast<int>(i1));
            tree->AsIntCon()->gtFieldSeq = fieldSeq;

            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }

            JITDUMP("Bashed to int constant:\n");
            DISPTREE(tree);

            goto DONE;

        // Fold constant LONG binary operator.

        case TYP_LONG:

            // No GC pointer types should be folded here...
            assert(!varTypeIsGC(op1->TypeGet()) && !varTypeIsGC(op2->TypeGet()));

            // op1 is known to be a TYP_LONG, op2 is normally a TYP_LONG, unless we have a shift operator in which case
            // it is a TYP_INT.
            assert(op2->TypeIs(TYP_LONG, TYP_INT));

            if (!op1->AsIntConCommon()->ImmedValCanBeFolded(this, tree->OperGet()))
            {
                return tree;
            }

            if (!op2->AsIntConCommon()->ImmedValCanBeFolded(this, tree->OperGet()))
            {
                return tree;
            }

            lval1 = op1->AsIntConCommon()->LngValue();

            // For the shift operators we can have a op2 that is a TYP_INT.
            // Thus we cannot just use LngValue(), as it will assert on 32 bit if op2 is not GT_CNS_LNG.
            lval2 = op2->AsIntConCommon()->IntegralValue();

            switch (tree->OperGet())
            {
                case GT_EQ:
                    i1 = (lval1 == lval2);
                    goto FOLD_COND;
                case GT_NE:
                    i1 = (lval1 != lval2);
                    goto FOLD_COND;

                case GT_LT:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT64(lval1) < UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 < lval2);
                    }
                    goto FOLD_COND;

                case GT_LE:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT64(lval1) <= UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 <= lval2);
                    }
                    goto FOLD_COND;

                case GT_GE:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT64(lval1) >= UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 >= lval2);
                    }
                    goto FOLD_COND;

                case GT_GT:
                    if (tree->IsUnsigned())
                    {
                        i1 = (UINT64(lval1) > UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 > lval2);
                    }
                    goto FOLD_COND;

                case GT_ADD:
                    ltemp = lval1 + lval2;
                    if (tree->gtOverflow() && CheckedOps::AddOverflows(lval1, lval2, tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    lval1 = ltemp;
#ifdef TARGET_64BIT
                    fieldSeq = GetFieldSeqStore()->Append(op1->AsIntCon()->gtFieldSeq, op2->AsIntCon()->gtFieldSeq);
#endif
                    break;

                case GT_SUB:
                    ltemp = lval1 - lval2;
                    if (tree->gtOverflow() && CheckedOps::SubOverflows(lval1, lval2, tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    lval1 = ltemp;
                    break;

                case GT_MUL:
                    ltemp = lval1 * lval2;
                    if (tree->gtOverflow() && CheckedOps::MulOverflows(lval1, lval2, tree->IsUnsigned()))
                    {
                        goto INTEGRAL_OVF;
                    }
                    lval1 = ltemp;
                    break;

                case GT_OR:
                    lval1 |= lval2;
                    break;
                case GT_XOR:
                    lval1 ^= lval2;
                    break;
                case GT_AND:
                    lval1 &= lval2;
                    break;

                case GT_LSH:
                    lval1 <<= (lval2 & 0x3f);
                    break;
                case GT_RSH:
                    lval1 >>= (lval2 & 0x3f);
                    break;
                case GT_RSZ:
                    // logical shift -> make it unsigned to not propagate the sign bit.
                    lval1 = UINT64(lval1) >> (lval2 & 0x3f);
                    break;
                case GT_ROL:
                    lval1 = (lval1 << (lval2 & 0x3f)) | (UINT64(lval1) >> ((64 - lval2) & 0x3f));
                    break;
                case GT_ROR:
                    lval1 = (lval1 << ((64 - lval2) & 0x3f)) | (UINT64(lval1) >> (lval2 & 0x3f));
                    break;

                // Both DIV and IDIV on x86 raise an exception for min_int (and min_long) / -1.  So we preserve
                // that behavior here.
                case GT_DIV:
                    if (lval2 == 0)
                    {
                        return tree;
                    }
                    if ((UINT64(lval1) == UINT64(0x8000000000000000)) && (lval2 == INT64(-1)))
                    {
                        return tree;
                    }

                    lval1 /= lval2;
                    break;

                case GT_MOD:
                    if (lval2 == 0)
                    {
                        return tree;
                    }
                    if ((UINT64(lval1) == UINT64(0x8000000000000000)) && (lval2 == INT64(-1)))
                    {
                        return tree;
                    }

                    lval1 %= lval2;
                    break;

                case GT_UDIV:
                    if (lval2 == 0)
                    {
                        return tree;
                    }
                    if ((UINT64(lval1) == UINT64(0x8000000000000000)) && (lval2 == INT64(-1)))
                    {
                        return tree;
                    }

                    lval1 = UINT64(lval1) / UINT64(lval2);
                    break;

                case GT_UMOD:
                    if (lval2 == 0)
                    {
                        return tree;
                    }
                    if ((UINT64(lval1) == UINT64(0x8000000000000000)) && (lval2 == INT64(-1)))
                    {
                        return tree;
                    }

                    lval1 = UINT64(lval1) % UINT64(lval2);
                    break;
                default:
                    return tree;
            }

        CNS_LONG:
#if !defined(TARGET_64BIT)
            if (fieldSeq != nullptr)
            {
                assert(!"Field sequences on CNS_LNG nodes!?");
                return tree;
            }
#endif // !defined(TARGET_64BIT)

            JITDUMP("\nFolding long operator with constant nodes into a constant:\n");
            DISPTREE(tree);

            assert((GenTree::s_gtNodeSizes[GT_CNS_NATIVELONG] == TREE_NODE_SZ_SMALL) ||
                   (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE));

            tree->BashToConst(lval1);
#ifdef TARGET_64BIT
            tree->AsIntCon()->gtFieldSeq = fieldSeq;
#endif
            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }

            JITDUMP("Bashed to long constant:\n");
            DISPTREE(tree);

            goto DONE;

        // Fold constant FLOAT or DOUBLE binary operator

        case TYP_FLOAT:
        case TYP_DOUBLE:

            if (tree->gtOverflowEx())
            {
                return tree;
            }

            assert(op1->OperIs(GT_CNS_DBL));
            d1 = op1->AsDblCon()->DconValue();

            assert(varTypeIsFloating(op2->TypeGet()));
            assert(op2->OperIs(GT_CNS_DBL));
            d2 = op2->AsDblCon()->DconValue();

            // Special case - check if we have NaN operands.
            // For comparisons if not an unordered operation always return 0.
            // For unordered operations (i.e. the GTF_RELOP_NAN_UN flag is set)
            // the result is always true - return 1.

            if (_isnan(d1) || _isnan(d2))
            {
                JITDUMP("Double operator(s) is NaN\n");

                if (tree->OperIsCompare())
                {
                    if (tree->gtFlags & GTF_RELOP_NAN_UN)
                    {
                        // Unordered comparison with NaN always succeeds.
                        i1 = 1;
                        goto FOLD_COND;
                    }
                    else
                    {
                        // Normal comparison with NaN always fails.
                        i1 = 0;
                        goto FOLD_COND;
                    }
                }
            }

            switch (tree->OperGet())
            {
                case GT_EQ:
                    i1 = (d1 == d2);
                    goto FOLD_COND;
                case GT_NE:
                    i1 = (d1 != d2);
                    goto FOLD_COND;

                case GT_LT:
                    i1 = (d1 < d2);
                    goto FOLD_COND;
                case GT_LE:
                    i1 = (d1 <= d2);
                    goto FOLD_COND;
                case GT_GE:
                    i1 = (d1 >= d2);
                    goto FOLD_COND;
                case GT_GT:
                    i1 = (d1 > d2);
                    goto FOLD_COND;

                // Floating point arithmetic should be done in declared
                // precision while doing constant folding. For this reason though TYP_FLOAT
                // constants are stored as double constants, while performing float arithmetic,
                // double constants should be converted to float.  Here is an example case
                // where performing arithmetic in double precision would lead to incorrect
                // results.
                //
                // Example:
                // float a = float.MaxValue;
                // float b = a*a;   This will produce +inf in single precision and 1.1579207543382391e+077 in double
                //                  precision.
                // float c = b/b;   This will produce NaN in single precision and 1 in double precision.
                case GT_ADD:
                    if (op1->TypeIs(TYP_FLOAT))
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = forceCastToFloat(f1 + f2);
                    }
                    else
                    {
                        d1 += d2;
                    }
                    break;

                case GT_SUB:
                    if (op1->TypeIs(TYP_FLOAT))
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = forceCastToFloat(f1 - f2);
                    }
                    else
                    {
                        d1 -= d2;
                    }
                    break;

                case GT_MUL:
                    if (op1->TypeIs(TYP_FLOAT))
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = forceCastToFloat(f1 * f2);
                    }
                    else
                    {
                        d1 *= d2;
                    }
                    break;

                case GT_DIV:
                    // We do not fold division by zero, even for floating point.
                    // This is because the result will be platform-dependent for an expression like 0d / 0d.
                    if (d2 == 0)
                    {
                        return tree;
                    }
                    if (op1->TypeIs(TYP_FLOAT))
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = forceCastToFloat(f1 / f2);
                    }
                    else
                    {
                        d1 /= d2;
                    }
                    break;

                default:
                    return tree;
            }

        CNS_DOUBLE:

            JITDUMP("\nFolding fp operator with constant nodes into a fp constant:\n");
            DISPTREE(tree);

            assert((GenTree::s_gtNodeSizes[GT_CNS_DBL] == TREE_NODE_SZ_SMALL) ||
                   (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE));

            tree->BashToConst(d1, tree->TypeGet());
            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }

            JITDUMP("Bashed to fp constant:\n");
            DISPTREE(tree);

            goto DONE;

        default:
            // Not a foldable type.
            return tree;
    }

DONE:

    // Make sure no side effect flags are set on this constant node.

    tree->gtFlags &= ~GTF_ALL_EFFECT;

    return tree;

INTEGRAL_OVF:

    // This operation is going to cause an overflow exception. Morph into
    // an overflow helper. Put a dummy constant value for code generation.
    //
    // We could remove all subsequent trees in the current basic block,
    // unless this node is a child of GT_COLON
    //
    // NOTE: Since the folded value is not constant we should not change the
    //       "tree" node - otherwise we confuse the logic that checks if the folding
    //       was successful - instead use one of the operands, e.g. op1.

    // Don't fold overflow operations if not global morph phase.
    // The reason for this is that this optimization is replacing a GenTree node
    // with another new GenTree node. Say a GT_CALL(arglist) has one 'arg'
    // involving overflow arithmetic.  During assertion prop, it is possible
    // that the 'arg' could be constant folded and the result could lead to an
    // overflow.  In such a case 'arg' will get replaced with GT_COMMA node
    // but fgMorphArgs() - see the logic around "if(lateArgsComputed)" - doesn't
    // update args table. For this reason this optimization is enabled only
    // for global morphing phase.
    //
    // TODO-CQ: Once fgMorphArgs() is fixed this restriction could be removed.

    if (!fgGlobalMorph)
    {
        assert(tree->gtOverflow());
        return tree;
    }

    var_types type = genActualType(tree->TypeGet());
    op1            = type == TYP_LONG ? gtNewLconNode(0) : gtNewIconNode(0);
    if (vnStore != nullptr)
    {
        op1->gtVNPair.SetBoth(vnStore->VNZeroForType(type));
    }

    JITDUMP("\nFolding binary operator with constant nodes into a comma throw:\n");
    DISPTREE(tree);

    // We will change the cast to a GT_COMMA and attach the exception helper as AsOp()->gtOp1.
    // The constant expression zero becomes op2.

    assert(tree->gtOverflow());
    assert(tree->OperIs(GT_ADD, GT_SUB, GT_CAST, GT_MUL));
    assert(op1 != nullptr);

    op2 = op1;
    op1 = gtNewHelperCallNode(CORINFO_HELP_OVERFLOW, TYP_VOID, gtNewIconNode(compCurBB->bbTryIndex));

    // op1 is a call to the JIT helper that throws an Overflow exception.
    // Attach the ExcSet for VNF_OverflowExc(Void) to this call.

    if (vnStore != nullptr)
    {
        op1->gtVNPair = vnStore->VNPWithExc(ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid()),
                                            vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_OverflowExc,
                                                                                               vnStore->VNPForVoid())));
    }

    tree = gtNewOperNode(GT_COMMA, tree->TypeGet(), op1, op2);

    return tree;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//------------------------------------------------------------------------
// gtFoldIndirConst: Attempt to fold an "IND(addr)" expression to a constant.
//
// Currently handles the case of "addr" being "INDEX_ADDR(CNS_STR, CONST)".
//
// Arguments:
//    indir - The IND node to attempt to fold
//
// Return Value:
//    The new constant node if the folding was successful, "nullptr" otherwise.
//
GenTree* Compiler::gtFoldIndirConst(GenTreeIndir* indir)
{
    assert(opts.OptimizationEnabled() && !optValnumCSE_phase);
    assert(indir->OperIs(GT_IND));

    GenTree* addr = indir->Addr();

    if (indir->TypeIs(TYP_USHORT) && addr->OperIs(GT_INDEX_ADDR) && addr->AsIndexAddr()->Arr()->OperIs(GT_CNS_STR))
    {
        GenTreeStrCon* stringNode = addr->AsIndexAddr()->Arr()->AsStrCon();
        GenTree*       indexNode  = addr->AsIndexAddr()->Index();
        if (!stringNode->IsStringEmptyField() && indexNode->IsCnsIntOrI())
        {
            int cnsIndex = static_cast<int>(indexNode->AsIntConCommon()->IconValue());
            if (cnsIndex >= 0)
            {
                char16_t chr;
                int      length =
                    info.compCompHnd->getStringLiteral(stringNode->gtScpHnd, stringNode->gtSconCPX, &chr, 1, cnsIndex);
                if (length > 0)
                {
                    return gtNewIconNode(chr);
                }
            }
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// gtNewTempStore: Create an assignment of the given value to a temp.
//
// Arguments:
//    tmp         - local number for a compiler temp
//    val         - value to assign to the temp
//    curLevel    - stack level to spill at (importer-only)
//    pAfterStmt  - statement to insert any additional statements after
//    di          - debug info for new statements
//    block       - block to insert any additional statements in
//
// Return Value:
//    Normally a new assignment node.
//    However may return a nop node if val is simply a reference to the temp.
//
// Notes:
//    Self-assignments may be represented via NOPs.
//
//    May update the type of the temp, if it was previously unknown.
//
//    May set compFloatingPointUsed.
//
GenTree* Compiler::gtNewTempStore(
    unsigned tmp, GenTree* val, unsigned curLevel, Statement** pAfterStmt, const DebugInfo& di, BasicBlock* block)
{
    // Self-assignment is a nop.
    if (val->OperGet() == GT_LCL_VAR && val->AsLclVarCommon()->GetLclNum() == tmp)
    {
        return gtNewNothingNode();
    }

    LclVarDsc* varDsc = lvaGetDesc(tmp);

    if (varDsc->TypeGet() == TYP_I_IMPL && val->TypeGet() == TYP_BYREF)
    {
        impBashVarAddrsToI(val);
    }

    var_types valTyp = val->TypeGet();
    if (val->OperGet() == GT_LCL_VAR && lvaTable[val->AsLclVar()->GetLclNum()].lvNormalizeOnLoad())
    {
        valTyp      = lvaGetRealType(val->AsLclVar()->GetLclNum());
        val->gtType = valTyp;
    }
    var_types dstTyp = varDsc->TypeGet();

    /* If the variable's lvType is not yet set then set it here */
    if (dstTyp == TYP_UNDEF)
    {
        varDsc->lvType = dstTyp = genActualType(valTyp);

        if (dstTyp == TYP_STRUCT)
        {
            lvaSetStruct(tmp, val->GetLayout(this), false);
        }
    }

#ifdef DEBUG
    // Make sure the actual types match.
    if (genActualType(valTyp) != genActualType(dstTyp))
    {
        // Plus some other exceptions that are apparently legal:
        // - TYP_REF or BYREF = TYP_I_IMPL
        bool ok = false;
        if (varTypeIsGC(dstTyp) && (valTyp == TYP_I_IMPL))
        {
            ok = true;
        }
        // - TYP_I_IMPL = TYP_BYREF
        else if ((dstTyp == TYP_I_IMPL) && (valTyp == TYP_BYREF))
        {
            ok = true;
        }
        // - TYP_BYREF = TYP_REF when object stack allocation is enabled
        else if (JitConfig.JitObjectStackAllocation() && (dstTyp == TYP_BYREF) && (valTyp == TYP_REF))
        {
            ok = true;
        }
        else if ((dstTyp == TYP_STRUCT) && (valTyp == TYP_INT))
        {
            assert(val->IsInitVal());
            ok = true;
        }

        if (!ok)
        {
            gtDispTree(val);
            assert(!"Incompatible types for gtNewTempStore");
        }
    }
#endif

    // Added this noway_assert for runtime\issue 44895, to protect against silent bad codegen
    //
    if ((dstTyp == TYP_STRUCT) && (valTyp == TYP_REF))
    {
        noway_assert(!"Incompatible types for gtNewTempStore");
    }

    // Floating Point assignments can be created during inlining
    // see "Zero init inlinee locals:" in fgInlinePrependStatements
    // thus we may need to set compFloatingPointUsed to true here.
    //
    if (!varTypeUsesIntReg(dstTyp))
    {
        compFloatingPointUsed = true;
    }

    GenTree* store = gtNewStoreLclVarNode(tmp, val);

    // TODO-ASG: delete this zero-diff quirk. Requires some forward substitution work.
    store->gtType = dstTyp;

    if (varTypeIsStruct(varDsc) && !val->IsInitVal())
    {
        store = impStoreStruct(store, curLevel, pAfterStmt, di, block);
    }

    return store;
}

/*****************************************************************************
 *
 *  Create a helper call to access a COM field (iff 'assg' is non-zero this is
 *  an assignment and 'assg' is the new value).
 */

GenTree* Compiler::gtNewRefCOMfield(GenTree*                objPtr,
                                    CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                    CORINFO_ACCESS_FLAGS    access,
                                    CORINFO_FIELD_INFO*     pFieldInfo,
                                    var_types               lclTyp,
                                    GenTree*                assg)
{
    assert(pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER ||
           pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_ADDR_HELPER ||
           pFieldInfo->fieldAccessor == CORINFO_FIELD_STATIC_ADDR_HELPER);

    // Arguments in reverse order
    GenTree* args[4];
    size_t   nArgs = 0;
    /* If we can't access it directly, we need to call a helper function */
    var_types            helperType = TYP_BYREF;
    CORINFO_CLASS_HANDLE structType = pFieldInfo->structType;

    if (pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER)
    {
        if (access & CORINFO_ACCESS_SET)
        {
            assert(assg != nullptr);
            // helper needs pointer to struct, not struct itself
            if (pFieldInfo->helper == CORINFO_HELP_SETFIELDSTRUCT)
            {
                // TODO-Bug?: verify if flags matter here
                GenTreeFlags indirFlags = GTF_EMPTY;
                assg                    = impGetNodeAddr(assg, CHECK_SPILL_ALL, &indirFlags);
            }
            else if (lclTyp == TYP_DOUBLE && assg->TypeGet() == TYP_FLOAT)
            {
                assg = gtNewCastNode(TYP_DOUBLE, assg, false, TYP_DOUBLE);
            }
            else if (lclTyp == TYP_FLOAT && assg->TypeGet() == TYP_DOUBLE)
            {
                assg = gtNewCastNode(TYP_FLOAT, assg, false, TYP_FLOAT);
            }

            args[nArgs++] = assg;
            helperType    = TYP_VOID;
        }
        else if (access & CORINFO_ACCESS_GET)
        {
            helperType = lclTyp;

            // The calling convention for the helper does not take into
            // account optimization of primitive structs.
            if ((pFieldInfo->helper == CORINFO_HELP_GETFIELDSTRUCT) && !varTypeIsStruct(lclTyp))
            {
                helperType = TYP_STRUCT;
            }
        }
    }

    if (pFieldInfo->helper == CORINFO_HELP_GETFIELDSTRUCT || pFieldInfo->helper == CORINFO_HELP_SETFIELDSTRUCT)
    {
        assert(pFieldInfo->structType != nullptr);
        args[nArgs++] = gtNewIconEmbClsHndNode(pFieldInfo->structType);
    }

    GenTree* fieldHnd = impTokenToHandle(pResolvedToken);
    if (fieldHnd == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    args[nArgs++] = fieldHnd;

    // If it's a static field, we shouldn't have an object node
    // If it's an instance field, we have an object node
    assert((pFieldInfo->fieldAccessor != CORINFO_FIELD_STATIC_ADDR_HELPER) ^ (objPtr == nullptr));

    if (objPtr != nullptr)
    {
        args[nArgs++] = objPtr;
    }

    GenTreeCall* call = gtNewHelperCallNode(pFieldInfo->helper, genActualType(helperType));

    for (size_t i = 0; i < nArgs; i++)
    {
        call->gtArgs.PushFront(this, NewCallArg::Primitive(args[i]));
        call->gtFlags |= args[i]->gtFlags & GTF_ALL_EFFECT;
    }

#if FEATURE_MULTIREG_RET
    if (varTypeIsStruct(call))
    {
        call->InitializeStructReturnType(this, structType, call->GetUnmanagedCallConv());
    }
#endif // FEATURE_MULTIREG_RET

    GenTree* result = call;

    if (pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER)
    {
        if (access & CORINFO_ACCESS_GET)
        {
            if (pFieldInfo->helper == CORINFO_HELP_GETFIELDSTRUCT)
            {
                if (!varTypeIsStruct(lclTyp))
                {
                    // get the result as primitive type
                    GenTreeFlags indirFlags = GTF_EMPTY;
                    result                  = impGetNodeAddr(result, CHECK_SPILL_ALL, &indirFlags);
                    result                  = gtNewIndir(lclTyp, result, indirFlags);
                }
            }
            else if (varTypeIsSmall(lclTyp))
            {
                // The helper does not extend the small return types.
                result = gtNewCastNode(genActualType(lclTyp), result, false, lclTyp);
            }
        }
    }
    else if ((access & CORINFO_ACCESS_ADDRESS) == 0) // OK, now do the indirection
    {
        ClassLayout* layout;
        lclTyp = TypeHandleToVarType(pFieldInfo->fieldType, structType, &layout);

        if ((access & CORINFO_ACCESS_SET) != 0)
        {
            result = (lclTyp == TYP_STRUCT) ? gtNewStoreBlkNode(layout, result, assg)->AsIndir()
                                            : gtNewStoreIndNode(lclTyp, result, assg);
            if (varTypeIsStruct(lclTyp))
            {
                result = impStoreStruct(result, CHECK_SPILL_ALL);
            }
        }
        else
        {
            assert((access & CORINFO_ACCESS_GET) != 0);
            result = (lclTyp == TYP_STRUCT) ? gtNewBlkIndir(layout, result) : gtNewIndir(lclTyp, result);
        }
    }

    return result;
}

/*****************************************************************************
 *
 *  Return true if the given node (excluding children trees) contains side effects.
 *  Note that it does not recurse, and children need to be handled separately.
 *  It may return false even if the node has GTF_SIDE_EFFECT (because of its children).
 *
 *  Similar to OperMayThrow() (but handles GT_CALLs specially), but considers
 *  assignments too.
 */

bool Compiler::gtNodeHasSideEffects(GenTree* tree, GenTreeFlags flags)
{
    if (flags & GTF_ASG)
    {
        if (tree->OperRequiresAsgFlag())
        {
            return true;
        }
    }

    // Are there only GTF_CALL side effects remaining? (and no other side effect kinds)
    if (flags & GTF_CALL)
    {
        if (tree->OperGet() == GT_CALL)
        {
            GenTreeCall* const call             = tree->AsCall();
            const bool         ignoreExceptions = (flags & GTF_EXCEPT) == 0;
            const bool         ignoreCctors     = (flags & GTF_IS_IN_CSE) != 0; // We can CSE helpers that run cctors.
            if (!call->HasSideEffects(this, ignoreExceptions, ignoreCctors))
            {
                // If this call is otherwise side effect free, check its arguments.
                for (CallArg& arg : call->gtArgs.Args())
                {
                    // I'm a little worried that args that assign to temps that are late args will look like
                    // side effects...but better to be conservative for now.
                    if ((arg.GetEarlyNode() != nullptr) && gtTreeHasSideEffects(arg.GetEarlyNode(), flags))
                    {
                        return true;
                    }

                    if ((arg.GetLateNode() != nullptr) && gtTreeHasSideEffects(arg.GetLateNode(), flags))
                    {
                        return true;
                    }
                }

                // Otherwise:
                return false;
            }

            // Otherwise the GT_CALL is considered to have side-effects.
            return true;
        }
    }

    if (flags & GTF_EXCEPT)
    {
        if (tree->OperMayThrow(this))
        {
            return true;
        }
    }

    // Expressions declared as CSE by (e.g.) hoisting code are considered to have relevant side
    // effects (if we care about GTF_MAKE_CSE).
    if ((flags & GTF_MAKE_CSE) && (tree->gtFlags & GTF_MAKE_CSE))
    {
        return true;
    }

    return false;
}

/*****************************************************************************
 * Returns true if the expr tree has any side effects.
 */

bool Compiler::gtTreeHasSideEffects(GenTree* tree, GenTreeFlags flags /* = GTF_SIDE_EFFECT*/)
{
    // These are the side effect flags that we care about for this tree
    GenTreeFlags sideEffectFlags = tree->gtFlags & flags;

    // Does this tree have any Side-effect flags set that we care about?
    if (sideEffectFlags == 0)
    {
        // no it doesn't..
        return false;
    }

    if (sideEffectFlags == GTF_CALL)
    {
        if (tree->OperGet() == GT_CALL)
        {
            // Generally all trees that contain GT_CALL nodes are considered to have side-effects.
            //
            if (tree->AsCall()->gtCallType == CT_HELPER)
            {
                // If this node is a helper call we may not care about the side-effects.
                // Note that gtNodeHasSideEffects checks the side effects of the helper itself
                // as well as the side effects of its arguments.
                return gtNodeHasSideEffects(tree, flags);
            }
        }
        else if (tree->OperGet() == GT_INTRINSIC)
        {
            if (gtNodeHasSideEffects(tree, flags))
            {
                return true;
            }

            if (gtNodeHasSideEffects(tree->AsOp()->gtOp1, flags))
            {
                return true;
            }

            if ((tree->AsOp()->gtOp2 != nullptr) && gtNodeHasSideEffects(tree->AsOp()->gtOp2, flags))
            {
                return true;
            }

            return false;
        }
    }

    return true;
}

//------------------------------------------------------------------------
// gtSplitTree: Split a statement into multiple statements such that a
// specified tree is the first executed non-invariant node in the statement.
//
// Arguments:
//    block        - The block containing the statement.
//    stmt         - The statement containing the tree.
//    splitPoint   - A tree inside the statement.
//    firstNewStmt - [out] The first new statement that was introduced.
//                   [firstNewStmt..stmt) are the statements added by this function.
//    splitNodeUse - The use of the tree to split at.
//
// Notes:
//   This method turns all non-invariant nodes that would be executed before
//   the split point into new separate statements. If those nodes were values
//   this involves introducing new locals for those values, such that they can
//   be used in the original statement.
//
//   This function does not update the flags on the original statement as it is
//   expected that the caller is going to modify the use further. Thus, the
//   caller is responsible for calling gtUpdateStmtSideEffects on the statement,
//   though this is only necessary if the function returned true.
//
//   The function may introduce new block ops that need to be morphed when used
//   after morph. fgMorphStmtBlockOps can be used on the new statements for
//   this purpose. Note that this will invalidate the use, so it should be done
//   after any further modifications have been made.
//
// Returns:
//   True if any changes were made; false if nothing needed to be done to split the tree.
//
bool Compiler::gtSplitTree(
    BasicBlock* block, Statement* stmt, GenTree* splitPoint, Statement** firstNewStmt, GenTree*** splitNodeUse)
{
    class Splitter final : public GenTreeVisitor<Splitter>
    {
        BasicBlock* m_bb;
        Statement*  m_splitStmt;
        GenTree*    m_splitNode;

        struct UseInfo
        {
            GenTree** Use;
            GenTree*  User;
        };
        ArrayStack<UseInfo> m_useStack;

    public:
        enum
        {
            DoPreOrder        = true,
            DoPostOrder       = true,
            UseExecutionOrder = true
        };

        Splitter(Compiler* compiler, BasicBlock* bb, Statement* stmt, GenTree* splitNode)
            : GenTreeVisitor(compiler)
            , m_bb(bb)
            , m_splitStmt(stmt)
            , m_splitNode(splitNode)
            , m_useStack(compiler->getAllocator(CMK_ArrayStack))
        {
        }

        Statement* FirstStatement = nullptr;
        GenTree**  SplitNodeUse   = nullptr;
        bool       MadeChanges    = false;

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            assert(!(*use)->OperIs(GT_QMARK));
            m_useStack.Push(UseInfo{use, user});
            return WALK_CONTINUE;
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            if (*use == m_splitNode)
            {
                bool userIsReturned = false;
                // Split all siblings and ancestor siblings.
                int i;
                for (i = 0; i < m_useStack.Height() - 1; i++)
                {
                    const UseInfo& useInf = m_useStack.BottomRef(i);
                    if (useInf.Use == use)
                    {
                        break;
                    }

                    // If this has the same user as the next node then it is a
                    // sibling of an ancestor -- and thus not on the "path"
                    // that contains the split node.
                    if (m_useStack.BottomRef(i + 1).User == useInf.User)
                    {
                        SplitOutUse(useInf, userIsReturned);
                    }
                    else
                    {
                        // This is an ancestor of the node we're splitting on.
                        userIsReturned = IsReturned(useInf, userIsReturned);
                    }
                }

                assert(m_useStack.Bottom(i).Use == use);
                userIsReturned = IsReturned(m_useStack.BottomRef(i), userIsReturned);

                // The remaining nodes should be operands of the split node.
                for (i++; i < m_useStack.Height(); i++)
                {
                    const UseInfo& useInf = m_useStack.BottomRef(i);
                    assert(useInf.User == *use);
                    SplitOutUse(useInf, userIsReturned);
                }

                SplitNodeUse = use;

                return WALK_ABORT;
            }

            while (m_useStack.Top(0).Use != use)
            {
                m_useStack.Pop();
            }

            return WALK_CONTINUE;
        }

    private:
        bool IsLocation(const UseInfo& useInf)
        {
            if (useInf.User == nullptr)
            {
                return false;
            }

            if (useInf.User->OperIs(GT_STORE_DYN_BLK) && !(*useInf.Use)->OperIs(GT_CNS_INT, GT_INIT_VAL) &&
                (useInf.Use == &useInf.User->AsStoreDynBlk()->Data()))
            {
                return true;
            }

            return false;
        }

        bool IsReturned(const UseInfo& useInf, bool userIsReturned)
        {
            if (useInf.User != nullptr)
            {
                if (useInf.User->OperIs(GT_RETURN))
                {
                    return true;
                }

                if (userIsReturned && useInf.User->OperIs(GT_COMMA) && (useInf.Use == &useInf.User->AsOp()->gtOp2))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsValue(const UseInfo& useInf)
        {
            GenTree* node = *useInf.Use;

            // Some places create void-typed commas that wrap actual values
            // (e.g. VN-based dead store removal), so we need the double check
            // here.
            if (!node->IsValue())
            {
                return false;
            }

            node = node->gtEffectiveVal();
            if (!node->IsValue())
            {
                return false;
            }

            GenTree* user = useInf.User;

            if (user == nullptr)
            {
                return false;
            }

            if (user->OperIs(GT_COMMA) && (&user->AsOp()->gtOp1 == useInf.Use))
            {
                return false;
            }

            if (user->OperIs(GT_CALL))
            {
                for (CallArg& callArg : user->AsCall()->gtArgs.Args())
                {
                    if ((&callArg.EarlyNodeRef() == useInf.Use) && (callArg.GetLateNode() != nullptr))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        void SplitOutUse(const UseInfo& useInf, bool userIsReturned)
        {
            GenTree** use  = useInf.Use;
            GenTree*  user = useInf.User;

            if ((*use)->IsInvariant())
            {
                return;
            }

            if ((*use)->OperIs(GT_LCL_VAR) && !m_compiler->lvaGetDesc((*use)->AsLclVarCommon())->IsAddressExposed())
            {
                // The splitting we do here should always guarantee that we
                // only introduce locals for the tree edges that overlap the
                // split point, so it should be ok to avoid creating statements
                // for locals that aren't address exposed. Note that this
                // relies on it being illegal IR to have a tree edge for a
                // register candidate that overlaps with an interfering node.
                //
                // For example, this optimization would be problematic if IR
                // like the following could occur:
                //
                // CALL
                //   LCL_VAR V00
                //   CALL
                //     STORE_LCL_VAR<V00>(...) (setup)
                //     LCL_VAR V00
                //
                return;
            }

            if (IsLocation(useInf))
            {
                // Only a handful of nodes can be location, and they are all unary or nullary.
                assert((*use)->OperIs(GT_IND, GT_BLK, GT_LCL_VAR, GT_LCL_FLD));
                if ((*use)->OperIsUnary())
                {
                    SplitOutUse(UseInfo{&(*use)->AsUnOp()->gtOp1, user}, false);
                }

                return;
            }

#ifndef TARGET_64BIT
            // GT_MUL with GTF_MUL_64RSLT is required to stay with casts on the
            // operands. Note that one operand may also be a constant, but we
            // would have exited early above for that case.
            if ((user != nullptr) && user->OperIs(GT_MUL) && user->Is64RsltMul())
            {
                assert((*use)->OperIs(GT_CAST));
                SplitOutUse(UseInfo{&(*use)->AsCast()->gtOp1, *use}, false);
                return;
            }
#endif

            if ((*use)->OperIs(GT_FIELD_LIST, GT_INIT_VAL))
            {
                for (GenTree** operandUse : (*use)->UseEdges())
                {
                    SplitOutUse(UseInfo{operandUse, *use}, false);
                }
                return;
            }

            Statement* stmt = nullptr;
            if (!IsValue(useInf))
            {
                GenTree* sideEffects = nullptr;
                m_compiler->gtExtractSideEffList(*use, &sideEffects);
                if (sideEffects != nullptr)
                {
                    stmt = m_compiler->fgNewStmtFromTree(sideEffects, m_splitStmt->GetDebugInfo());
                }
                *use        = m_compiler->gtNewNothingNode();
                MadeChanges = true;
            }
            else
            {
                unsigned lclNum = m_compiler->lvaGrabTemp(true DEBUGARG("Spilling to split statement for tree"));

                if (varTypeIsStruct(*use) &&
                    ((*use)->IsMultiRegNode() ||
                     (IsReturned(useInf, userIsReturned) && m_compiler->compMethodReturnsMultiRegRetType())))
                {
                    m_compiler->lvaGetDesc(lclNum)->lvIsMultiRegRet = true;
                }

                GenTree* store = m_compiler->gtNewTempStore(lclNum, *use);
                stmt           = m_compiler->fgNewStmtFromTree(store, m_splitStmt->GetDebugInfo());
                *use           = m_compiler->gtNewLclvNode(lclNum, genActualType(*use));
                MadeChanges    = true;
            }

            if (stmt != nullptr)
            {
                if (FirstStatement == nullptr)
                {
                    FirstStatement = stmt;
                }

                m_compiler->fgInsertStmtBefore(m_bb, m_splitStmt, stmt);
            }
        }
    };

    Splitter splitter(this, block, stmt, splitPoint);
    splitter.WalkTree(stmt->GetRootNodePointer(), nullptr);
    *firstNewStmt = splitter.FirstStatement;
    *splitNodeUse = splitter.SplitNodeUse;

    return splitter.MadeChanges;
}

//------------------------------------------------------------------------
// gtExtractSideEffList: Extracts side effects from the given expression.
//
// Arguments:
//    expr       - the expression tree to extract side effects from
//    pList      - pointer to a (possibly null) node
//    flags      - side effect flags to be considered
//    ignoreRoot - ignore side effects on the expression root node
//
// Notes:
//    pList is modified such that the original pList is executed after all side
//    effects that were extracted. The original side effect execution order is
//    preserved.
//
void Compiler::gtExtractSideEffList(GenTree*     expr,
                                    GenTree**    pList,
                                    GenTreeFlags flags /* = GTF_SIDE_EFFECT*/,
                                    bool         ignoreRoot /* = false */)
{
    class SideEffectExtractor final : public GenTreeVisitor<SideEffectExtractor>
    {
        const GenTreeFlags m_flags;

        GenTree* m_result = nullptr;

    public:
        enum
        {
            DoPreOrder        = true,
            UseExecutionOrder = true
        };

        GenTree* GetResult()
        {
            return m_result;
        }

        SideEffectExtractor(Compiler* compiler, GenTreeFlags flags) : GenTreeVisitor(compiler), m_flags(flags)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;

            bool treeHasSideEffects = m_compiler->gtTreeHasSideEffects(node, m_flags);

            if (treeHasSideEffects)
            {
                if (m_compiler->gtNodeHasSideEffects(node, m_flags))
                {
                    if (node->OperIsBlk() && !node->OperIsStoreBlk())
                    {
                        JITDUMP("Replace an unused BLK node [%06d] with a NULLCHECK\n", dspTreeID(node));
                        m_compiler->gtChangeOperToNullCheck(node, m_compiler->compCurBB);
                    }

                    Append(node);
                    return Compiler::WALK_SKIP_SUBTREES;
                }

                if (node->OperIs(GT_QMARK))
                {
                    GenTree* prevSideEffects = m_result;
                    // Visit children out of order so we know if we can
                    // completely remove the qmark. We cannot modify the
                    // condition if we cannot completely remove the qmark, so
                    // we cannot visit it first.

                    GenTreeQmark* qmark = node->AsQmark();
                    GenTreeColon* colon = qmark->gtGetOp2()->AsColon();

                    m_result = nullptr;
                    WalkTree(&colon->gtOp1, colon);
                    GenTree* thenSideEffects = m_result;

                    m_result = nullptr;
                    WalkTree(&colon->gtOp2, colon);
                    GenTree* elseSideEffects = m_result;

                    m_result = prevSideEffects;

                    if ((thenSideEffects == nullptr) && (elseSideEffects == nullptr))
                    {
                        WalkTree(&qmark->gtOp1, qmark);
                    }
                    else
                    {
                        colon->gtOp1  = (thenSideEffects != nullptr) ? thenSideEffects : m_compiler->gtNewNothingNode();
                        colon->gtOp2  = (elseSideEffects != nullptr) ? elseSideEffects : m_compiler->gtNewNothingNode();
                        qmark->gtType = TYP_VOID;
                        colon->gtType = TYP_VOID;

                        qmark->gtFlags &= ~GTF_QMARK_CAST_INSTOF;
                        Append(qmark);
                    }

                    return Compiler::WALK_SKIP_SUBTREES;
                }

                // Generally all GT_CALL nodes are considered to have side-effects.
                // So if we get here it must be a helper call that we decided it does
                // not have side effects that we needed to keep.
                assert(!node->OperIs(GT_CALL) || (node->AsCall()->gtCallType == CT_HELPER));
            }

            if ((m_flags & GTF_IS_IN_CSE) != 0)
            {
                // If we're doing CSE then we also need to unmark CSE nodes. This will fail for CSE defs,
                // those need to be extracted as if they're side effects.
                if (!UnmarkCSE(node))
                {
                    Append(node);
                    return Compiler::WALK_SKIP_SUBTREES;
                }

                // The existence of CSE defs and uses is not propagated up the tree like side
                // effects are. We need to continue visiting the tree as if it has side effects.
                treeHasSideEffects = true;
            }

            return treeHasSideEffects ? Compiler::WALK_CONTINUE : Compiler::WALK_SKIP_SUBTREES;
        }

        void Append(GenTree* node)
        {
            if (m_result == nullptr)
            {
                m_result = node;
                return;
            }

            GenTree* comma = m_compiler->gtNewOperNode(GT_COMMA, TYP_VOID, m_result, node);
            comma->gtFlags |= (m_result->gtFlags | node->gtFlags) & GTF_ALL_EFFECT;

#ifdef DEBUG
            if (m_compiler->fgGlobalMorph)
            {
                // Either both should be morphed or neither should be.
                assert((m_result->gtDebugFlags & GTF_DEBUG_NODE_MORPHED) ==
                       (node->gtDebugFlags & GTF_DEBUG_NODE_MORPHED));
                comma->gtDebugFlags |= node->gtDebugFlags & GTF_DEBUG_NODE_MORPHED;
            }
#endif

            // Both should have valuenumbers defined for both or for neither
            // one (unless we are remorphing, in which case a prior transform
            // involving either node may have discarded or otherwise
            // invalidated the value numbers).
            assert((m_result->gtVNPair.BothDefined() == node->gtVNPair.BothDefined()) || !m_compiler->fgGlobalMorph);

            // Set the ValueNumber 'gtVNPair' for the new GT_COMMA node
            //
            if (m_result->gtVNPair.BothDefined() && node->gtVNPair.BothDefined())
            {
                // The result of a GT_COMMA node is op2, the normal value number is op2vnp
                // But we also need to include the union of side effects from op1 and op2.
                // we compute this value into exceptions_vnp.
                ValueNumPair op1vnp;
                ValueNumPair op1Xvnp = ValueNumStore::VNPForEmptyExcSet();
                ValueNumPair op2vnp;
                ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();

                m_compiler->vnStore->VNPUnpackExc(node->gtVNPair, &op1vnp, &op1Xvnp);
                m_compiler->vnStore->VNPUnpackExc(m_result->gtVNPair, &op2vnp, &op2Xvnp);

                ValueNumPair exceptions_vnp = ValueNumStore::VNPForEmptyExcSet();

                exceptions_vnp = m_compiler->vnStore->VNPExcSetUnion(exceptions_vnp, op1Xvnp);
                exceptions_vnp = m_compiler->vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

                comma->gtVNPair = m_compiler->vnStore->VNPWithExc(op2vnp, exceptions_vnp);
            }

            m_result = comma;
        }

    private:
        bool UnmarkCSE(GenTree* node)
        {
            assert(m_compiler->optValnumCSE_phase);

            if (m_compiler->optUnmarkCSE(node))
            {
                // The call to optUnmarkCSE(node) should have cleared any CSE info.
                assert(!IS_CSE_INDEX(node->gtCSEnum));
                return true;
            }
            else
            {
                assert(IS_CSE_DEF(node->gtCSEnum));
#ifdef DEBUG
                if (m_compiler->verbose)
                {
                    printf("Preserving the CSE def #%02d at ", GET_CSE_INDEX(node->gtCSEnum));
                    m_compiler->printTreeID(node);
                }
#endif
                return false;
            }
        }
    };

    SideEffectExtractor extractor(this, flags);

    if (ignoreRoot)
    {
        for (GenTree* op : expr->Operands())
        {
            extractor.WalkTree(&op, nullptr);
        }
    }
    else
    {
        extractor.WalkTree(&expr, nullptr);
    }

    if (*pList != nullptr)
    {
        extractor.Append(*pList);
    }

    *pList = extractor.GetResult();
}

/*****************************************************************************
 *
 *  For debugging only - displays a tree node list and makes sure all the
 *  links are correctly set.
 */

#ifdef DEBUG
void dispNodeList(GenTree* list, bool verbose)
{
    GenTree* last = nullptr;
    GenTree* next;

    if (!list)
    {
        return;
    }

    while (true)
    {
        next = list->gtNext;

        if (verbose)
        {
            printf("%08X -> %08X -> %08X\n", last, list, next);
        }

        assert(!last || last->gtNext == list);

        assert(next == nullptr || next->gtPrev == list);

        if (!next)
        {
            break;
        }

        last = list;
        list = next;
    }
    printf(""); // null string means flush
}
#endif

/*****************************************************************************
 * Callback to mark the nodes of a qmark-colon subtree that are conditionally
 * executed.
 */

/* static */
Compiler::fgWalkResult Compiler::gtMarkColonCond(GenTree** pTree, fgWalkData* data)
{
    assert(data->pCallbackData == nullptr);

    (*pTree)->gtFlags |= GTF_COLON_COND;

    return WALK_CONTINUE;
}

/*****************************************************************************
 * Callback to clear the conditionally executed flags of nodes that no longer
   will be conditionally executed. Note that when we find another colon we must
   stop, as the nodes below this one WILL be conditionally executed. This callback
   is called when folding a qmark condition (ie the condition is constant).
 */

/* static */
Compiler::fgWalkResult Compiler::gtClearColonCond(GenTree** pTree, fgWalkData* data)
{
    GenTree* tree = *pTree;

    assert(data->pCallbackData == nullptr);

    if (tree->OperGet() == GT_COLON)
    {
        // Nodes below this will be conditionally executed.
        return WALK_SKIP_SUBTREES;
    }

    tree->gtFlags &= ~GTF_COLON_COND;
    return WALK_CONTINUE;
}

Compiler::FindLinkData Compiler::gtFindLink(Statement* stmt, GenTree* node)
{
    class FindLinkWalker : public GenTreeVisitor<FindLinkWalker>
    {
        GenTree*  m_node;
        GenTree** m_edge   = nullptr;
        GenTree*  m_parent = nullptr;

    public:
        enum
        {
            DoPreOrder = true,
        };

        FindLinkWalker(Compiler* comp, GenTree* node) : GenTreeVisitor(comp), m_node(node)
        {
        }

        FindLinkData GetResult()
        {
            return FindLinkData{m_node, m_edge, m_parent};
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if (*use == m_node)
            {
                m_edge   = use;
                m_parent = user;
                return WALK_ABORT;
            }

            return WALK_CONTINUE;
        }
    };

    FindLinkWalker walker(this, node);
    walker.WalkTree(stmt->GetRootNodePointer(), nullptr);
    return walker.GetResult();
}

/*****************************************************************************
 *
 *  Callback that checks if a tree node has oper type GT_CATCH_ARG
 */

static Compiler::fgWalkResult gtFindCatchArg(GenTree** pTree, Compiler::fgWalkData* /* data */)
{
    return ((*pTree)->OperGet() == GT_CATCH_ARG) ? Compiler::WALK_ABORT : Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
bool Compiler::gtHasCatchArg(GenTree* tree)
{
    if (((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0) && (fgWalkTreePre(&tree, gtFindCatchArg) == WALK_ABORT))
    {
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// gtGetTypeProducerKind: determine if a tree produces a runtime type, and
//    if so, how.
//
// Arguments:
//    tree - tree to examine
//
// Return Value:
//    TypeProducerKind for the tree.
//
// Notes:
//    Checks to see if this tree returns a RuntimeType value, and if so,
//    how that value is determined.
//
//    Currently handles these cases
//    1) The result of Object::GetType
//    2) The result of typeof(...)
//    3) A null reference
//    4) Tree is otherwise known to have type RuntimeType
//
//    The null reference case is surprisingly common because operator
//    overloading turns the otherwise innocuous
//
//        Type t = ....;
//        if (t == null)
//
//    into a method call.

Compiler::TypeProducerKind Compiler::gtGetTypeProducerKind(GenTree* tree)
{
    if (tree->gtOper == GT_CALL)
    {
        if (tree->AsCall()->gtCallType == CT_HELPER)
        {
            if (gtIsTypeHandleToRuntimeTypeHelper(tree->AsCall()))
            {
                return TPK_Handle;
            }
        }
        else if (tree->AsCall()->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
        {
            if (lookupNamedIntrinsic(tree->AsCall()->gtCallMethHnd) == NI_System_Object_GetType)
            {
                return TPK_GetType;
            }
        }
    }
    else if ((tree->gtOper == GT_INTRINSIC) && (tree->AsIntrinsic()->gtIntrinsicName == NI_System_Object_GetType))
    {
        return TPK_GetType;
    }
    else if ((tree->gtOper == GT_CNS_INT) && (tree->AsIntCon()->gtIconVal == 0))
    {
        return TPK_Null;
    }
    else
    {
        bool                 isExact   = false;
        bool                 isNonNull = false;
        CORINFO_CLASS_HANDLE clsHnd    = gtGetClassHandle(tree, &isExact, &isNonNull);

        if (clsHnd != NO_CLASS_HANDLE && clsHnd == info.compCompHnd->getBuiltinClass(CLASSID_RUNTIME_TYPE))
        {
            return TPK_Other;
        }
    }
    return TPK_Unknown;
}

//------------------------------------------------------------------------
// gtIsTypeHandleToRuntimeTypeHelperCall -- see if tree is constructing
//    a RuntimeType from a handle
//
// Arguments:
//    tree - tree to examine
//
// Return Value:
//    True if so

bool Compiler::gtIsTypeHandleToRuntimeTypeHelper(GenTreeCall* call)
{
    return call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE) ||
           call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL);
}

//------------------------------------------------------------------------
// gtIsTypeHandleToRuntimeTypeHandleHelperCall -- see if tree is constructing
//    a RuntimeTypeHandle from a handle
//
// Arguments:
//    tree - tree to examine
//    pHelper - optional pointer to a variable that receives the type of the helper
//
// Return Value:
//    True if so
//
bool Compiler::gtIsTypeHandleToRuntimeTypeHandleHelper(GenTreeCall* call, CorInfoHelpFunc* pHelper)
{
    CorInfoHelpFunc helper = CORINFO_HELP_UNDEF;

    if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE))
    {
        helper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE;
    }
    else if (call->gtCallMethHnd == eeFindHelper(CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL))
    {
        helper = CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPEHANDLE_MAYBENULL;
    }

    if (pHelper != nullptr)
    {
        *pHelper = helper;
    }

    return helper != CORINFO_HELP_UNDEF;
}

bool Compiler::gtIsActiveCSE_Candidate(GenTree* tree)
{
    return (optValnumCSE_phase && IS_CSE_INDEX(tree->gtCSEnum));
}

//------------------------------------------------------------------------
// gtTreeContainsOper -- check if the tree contains any subtree with the specified oper.
//
// Arguments:
//    tree - tree to examine
//    oper - oper to check for
//
// Return Value:
//    True if any subtree has the specified oper; otherwise false.
//
bool Compiler::gtTreeContainsOper(GenTree* tree, genTreeOps oper)
{
    class Visitor final : public GenTreeVisitor<Visitor>
    {
        genTreeOps m_oper;

    public:
        Visitor(Compiler* comp, genTreeOps oper) : GenTreeVisitor(comp), m_oper(oper)
        {
        }

        enum
        {
            DoPreOrder = true,
        };

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if ((*use)->OperIs(m_oper))
            {
                return WALK_ABORT;
            }

            return WALK_CONTINUE;
        }
    };

    Visitor visitor(this, oper);
    return visitor.WalkTree(&tree, nullptr) == WALK_ABORT;
}

//------------------------------------------------------------------------
// gtCollectExceptions: walk a tree collecting a bit set of exceptions the tree
// may throw.
//
// Arguments:
//    tree - tree to examine
//
// Return Value:
//    Bit set of exceptions the tree may throw.
//
ExceptionSetFlags Compiler::gtCollectExceptions(GenTree* tree)
{
    class ExceptionsWalker final : public GenTreeVisitor<ExceptionsWalker>
    {
        ExceptionSetFlags m_preciseExceptions = ExceptionSetFlags::None;

    public:
        ExceptionsWalker(Compiler* comp) : GenTreeVisitor<ExceptionsWalker>(comp)
        {
        }

        enum
        {
            DoPreOrder = true,
        };

        ExceptionSetFlags GetFlags()
        {
            return m_preciseExceptions;
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* tree = *use;
            if ((tree->gtFlags & GTF_EXCEPT) == 0)
            {
                return WALK_SKIP_SUBTREES;
            }

            m_preciseExceptions |= tree->OperExceptions(m_compiler);
            return WALK_CONTINUE;
        }
    };

    // We only expect the caller to ask for precise exceptions for cases where
    // it may help with disambiguating between exceptions. If the tree contains
    // a call it can always throw arbitrary exceptions.
    assert((tree->gtFlags & GTF_CALL) == 0);

    ExceptionsWalker walker(this);
    walker.WalkTree(&tree, nullptr);
    assert(((tree->gtFlags & GTF_EXCEPT) == 0) || (walker.GetFlags() != ExceptionSetFlags::None));
    return walker.GetFlags();
}

//-----------------------------------------------------------
// gtComplexityExceeds: Check if a tree exceeds a specified complexity in terms
// of number of sub nodes.
//
// Arguments:
//     tree  - The tree to check
//     limit - The limit in terms of number of nodes
//
// Return Value:
//     True if there are mode sub nodes in tree; otherwise false.
//
bool Compiler::gtComplexityExceeds(GenTree* tree, unsigned limit)
{
    struct ComplexityVisitor : GenTreeVisitor<ComplexityVisitor>
    {
        enum
        {
            DoPreOrder = true,
        };

        ComplexityVisitor(Compiler* comp, unsigned limit) : GenTreeVisitor(comp), m_limit(limit)
        {
        }

        fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
        {
            if (++m_numNodes > m_limit)
            {
                return WALK_ABORT;
            }

            return WALK_CONTINUE;
        }

    private:
        unsigned m_limit;
        unsigned m_numNodes = 0;
    };

    ComplexityVisitor visitor(this, limit);
    return visitor.WalkTree(&tree, nullptr) == WALK_ABORT;
}

bool GenTree::IsPhiNode()
{
    return (OperGet() == GT_PHI_ARG) || (OperGet() == GT_PHI) || IsPhiDefn();
}

bool GenTree::IsPhiDefn()
{
    return OperIs(GT_STORE_LCL_VAR) && AsLclVar()->Data()->OperIs(GT_PHI);
}

bool GenTree::IsLclVarAddr() const
{
    return OperIs(GT_LCL_ADDR) && (AsLclFld()->GetLclOffs() == 0);
}

// IsPartialLclFld: Check for a GT_LCL_FLD whose type is a different size than the lclVar.
//
// Arguments:
//    comp      - the Compiler object.
//
// Return Value:
//    Returns "true" iff 'this' is a GT_LCL_FLD or GT_STORE_LCL_FLD on which the type
//    is not the same size as the type of the GT_LCL_VAR
//
bool GenTree::IsPartialLclFld(Compiler* comp)
{
    return OperIs(GT_LCL_FLD, GT_STORE_LCL_FLD) &&
           (comp->lvaGetDesc(AsLclFld())->lvExactSize() != AsLclFld()->GetSize());
}

//------------------------------------------------------------------------
// DefinesLocal: Does "this" define a local?
//
// Arguments:
//    comp        - the compiler instance
//    pLclVarTree - [out] parameter for the local representing the definition
//    pIsEntire   - optional [out] parameter for whether the store represents
//                  a "full" definition (overwrites the entire variable)
//    pOffset     - optional [out] parameter for the offset, relative to the
//                  local, at which the store is performed
//    pSize       - optional [out] parameter for the amount of bytes affected
//                  by the store
//
// Return Value:
//    Whether "this" represents a store to a possibly tracked local variable
//    before rationalization.
//
// Notes:
//    This function is contractually bound to recognize a superset of stores
//    that "LocalAddressVisitor" recognizes and transforms, as it is used to
//    detect which trees can define tracked locals.
//
bool GenTree::DefinesLocal(
    Compiler* comp, GenTreeLclVarCommon** pLclVarTree, bool* pIsEntire, ssize_t* pOffset, unsigned* pSize)
{
    assert((pOffset == nullptr) || (*pOffset == 0));

    if (OperIs(GT_STORE_LCL_VAR))
    {
        *pLclVarTree = AsLclVarCommon();
        if (pIsEntire != nullptr)
        {
            *pIsEntire = true;
        }
        if (pOffset != nullptr)
        {
            *pOffset = 0;
        }
        if (pSize != nullptr)
        {
            *pSize = comp->lvaLclExactSize(AsLclVarCommon()->GetLclNum());
        }

        return true;
    }
    if (OperIs(GT_STORE_LCL_FLD))
    {
        *pLclVarTree = AsLclVarCommon();
        if (pIsEntire != nullptr)
        {
            *pIsEntire = !AsLclFld()->IsPartialLclFld(comp);
        }
        if (pOffset != nullptr)
        {
            *pOffset = AsLclFld()->GetLclOffs();
        }
        if (pSize != nullptr)
        {
            *pSize = AsLclFld()->GetSize();
        }

        return true;
    }
    if (OperIs(GT_CALL))
    {
        GenTreeLclVarCommon* lclAddr = comp->gtCallGetDefinedRetBufLclAddr(AsCall());
        if (lclAddr == nullptr)
        {
            return false;
        }

        *pLclVarTree = lclAddr;

        if ((pIsEntire != nullptr) || (pSize != nullptr))
        {
            unsigned storeSize = comp->typGetObjLayout(AsCall()->gtRetClsHnd)->GetSize();

            if (pIsEntire != nullptr)
            {
                *pIsEntire = storeSize == comp->lvaLclExactSize(lclAddr->GetLclNum());
            }

            if (pSize != nullptr)
            {
                *pSize = storeSize;
            }
        }

        if (pOffset != nullptr)
        {
            *pOffset = lclAddr->GetLclOffs();
        }

        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// IsImplicitByrefParameterValuePreMorph:
//    Determine if this tree represents the value of an implicit byref
//    parameter, and if so return the tree for the parameter. To be used
//    before implicit byrefs have been morphed.
//
// Arguments:
//    compiler - compiler instance
//
// Return Value:
//    Node for the local, or nullptr.
//
GenTreeLclVarCommon* GenTree::IsImplicitByrefParameterValuePreMorph(Compiler* compiler)
{
#if FEATURE_IMPLICIT_BYREFS && !defined(TARGET_LOONGARCH64) // TODO-LOONGARCH64-CQ: enable this.

    GenTreeLclVarCommon* lcl = OperIsLocal() ? AsLclVarCommon() : nullptr;

    if ((lcl != nullptr) && compiler->lvaIsImplicitByRefLocal(lcl->GetLclNum()))
    {
        return lcl;
    }

#endif // FEATURE_IMPLICIT_BYREFS && !defined(TARGET_LOONGARCH64)

    return nullptr;
}

//------------------------------------------------------------------------
// IsImplicitByrefParameterValuePostMorph:
//    Determine if this tree represents the value of an implicit byref
//    parameter, and if so return the tree for the parameter. To be used after
//    implicit byrefs have been morphed.
//
// Arguments:
//    compiler - compiler instance
//    addr     - [out] tree representing the address computation on top of the implicit byref.
//               Will be the same as the return value if the whole implicit byref is used, for example.
//
// Return Value:
//    Node for the local, or nullptr.
//
GenTreeLclVar* GenTree::IsImplicitByrefParameterValuePostMorph(Compiler* compiler, GenTree** addr)
{
#if FEATURE_IMPLICIT_BYREFS && !defined(TARGET_LOONGARCH64) // TODO-LOONGARCH64-CQ: enable this.

    if (!OperIsLoad())
    {
        return nullptr;
    }

    *addr              = AsIndir()->Addr();
    GenTree* innerAddr = *addr;

    while (innerAddr->OperIs(GT_ADD) && innerAddr->gtGetOp2()->IsCnsIntOrI())
    {
        innerAddr = innerAddr->gtGetOp1();
    }

    if (innerAddr->OperIs(GT_LCL_VAR))
    {
        GenTreeLclVar* lcl = innerAddr->AsLclVar();
        if (compiler->lvaIsImplicitByRefLocal(lcl->GetLclNum()))
        {
            return lcl;
        }
    }

#endif // FEATURE_IMPLICIT_BYREFS && !defined(TARGET_LOONGARCH64) && !defined(TARGET_RISCV64)

    return nullptr;
}

//------------------------------------------------------------------------
// IsLclVarUpdateTree: Determine whether this is an assignment tree of the
//                     form Vn = Vn 'oper' 'otherTree' where Vn is a lclVar
//
// Arguments:
//    pOtherTree - An "out" argument in which 'otherTree' will be returned.
//    pOper      - An "out" argument in which 'oper' will be returned.
//
// Return Value:
//    If the tree is of the above form, the lclNum of the variable being
//    updated is returned, and 'pOtherTree' and 'pOper' are set.
//    Otherwise, returns BAD_VAR_NUM.
//
// Notes:
//    'otherTree' can have any shape.
//     We avoid worrying about whether the op is commutative by only considering the
//     first operand of the rhs. It is expected that most trees of this form will
//     already have the lclVar on the lhs.
//     TODO-CQ: Evaluate whether there are missed opportunities due to this, or
//     whether gtSetEvalOrder will already have put the lclVar on the lhs in
//     the cases of interest.

unsigned GenTree::IsLclVarUpdateTree(GenTree** pOtherTree, genTreeOps* pOper)
{
    if (OperIs(GT_STORE_LCL_VAR) && AsLclVar()->Data()->OperIsBinary())
    {
        unsigned lclNum = AsLclVar()->GetLclNum();
        GenTree* value  = AsLclVar()->Data();
        GenTree* op1    = value->AsOp()->gtOp1;
        GenTree* op2    = value->AsOp()->gtOp2;

        // Some operators, such as LEA, are currently declared as binary but may
        // not have two operands. We must check that both operands actually exist.
        if ((op1 != nullptr) && (op2 != nullptr) && (op1->OperGet() == GT_LCL_VAR) &&
            (op1->AsLclVarCommon()->GetLclNum() == lclNum))
        {
            *pOtherTree = op2;
            *pOper      = value->OperGet();
            return lclNum;
        }
    }

    return BAD_VAR_NUM;
}

//------------------------------------------------------------------------
// IsBlockProfileUpdate: Determine whether this tree is updating
//                       a basic block profile counter
//
// Return Value:
//    True if this tree is updating a block profile count
//
bool GenTree::IsBlockProfileUpdate()
{
    return OperIs(GT_STOREIND) && AsIndir()->Addr()->IsIconHandle(GTF_ICON_BBC_PTR);
}

#ifdef DEBUG
//------------------------------------------------------------------------
// canBeContained: check whether this tree node may be a subcomponent of its parent for purposes
//                 of code generation.
//
// Return Value:
//    True if it is possible to contain this node and false otherwise.
//
bool GenTree::canBeContained() const
{
    assert(OperIsLIR());

    if (IsMultiRegLclVar())
    {
        return false;
    }

    if (gtHasReg(nullptr))
    {
        return false;
    }

    // It is not possible for nodes that do not produce values or that are not containable values to be contained.
    if (!IsValue() || ((DebugOperKind() & DBK_NOCONTAIN) != 0))
    {
        return false;
    }
    else if (OperIsHWIntrinsic() && !isContainableHWIntrinsic())
    {
        return isEvexEmbeddedMaskingCompatibleHWIntrinsic();
    }

    return true;
}
#endif // DEBUG

//------------------------------------------------------------------------
// isContained: check whether this tree node is a subcomponent of its parent for codegen purposes
//
// Return Value:
//    Returns true if there is no code generated explicitly for this node.
//    Essentially, it will be rolled into the code generation for the parent.
//
// Assumptions:
//    This method relies upon the value of the GTF_CONTAINED flag.
//    Therefore this method is only valid after Lowering.
//    Also note that register allocation or other subsequent phases may cause
//    nodes to become contained (or not) and therefore this property may change.
//
bool GenTree::isContained() const
{
    assert(OperIsLIR());
    const bool isMarkedContained = ((gtFlags & GTF_CONTAINED) != 0);

#ifdef DEBUG
    if (!canBeContained())
    {
        assert(!isMarkedContained);
    }

    // if it's contained it can't be unused.
    if (isMarkedContained)
    {
        assert(!IsUnusedValue());
    }
#endif // DEBUG
    return isMarkedContained;
}

// return true if node is contained and an indir
bool GenTree::isContainedIndir() const
{
    return OperIsIndir() && isContained();
}

bool GenTree::isIndirAddrMode()
{
    return OperIsIndir() && AsIndir()->Addr()->OperIsAddrMode() && AsIndir()->Addr()->isContained();
}

bool GenTree::isIndir() const
{
    return OperGet() == GT_IND || OperGet() == GT_STOREIND;
}

bool GenTreeIndir::HasBase()
{
    return Base() != nullptr;
}

bool GenTreeIndir::HasIndex()
{
    return Index() != nullptr;
}

GenTree* GenTreeIndir::Base()
{
    GenTree* addr = Addr();

    if (isIndirAddrMode())
    {
        GenTree* result = addr->AsAddrMode()->Base();
        if (result != nullptr)
        {
            result = result->gtEffectiveVal();
        }
        return result;
    }
    else
    {
        return addr; // TODO: why do we return 'addr' here, but we return 'nullptr' in the equivalent Index() case?
    }
}

GenTree* GenTreeIndir::Index()
{
    if (isIndirAddrMode())
    {
        GenTree* result = Addr()->AsAddrMode()->Index();
        if (result != nullptr)
        {
            result = result->gtEffectiveVal();
        }
        return result;
    }
    else
    {
        return nullptr;
    }
}

unsigned GenTreeIndir::Scale()
{
    if (HasIndex())
    {
        return Addr()->AsAddrMode()->gtScale;
    }
    else
    {
        return 1;
    }
}

ssize_t GenTreeIndir::Offset()
{
    if (isIndirAddrMode())
    {
        return Addr()->AsAddrMode()->Offset();
    }
    else if (Addr()->IsCnsIntOrI() && Addr()->isContained())
    {
        return Addr()->AsIntConCommon()->IconValue();
    }
    else
    {
        return 0;
    }
}

unsigned GenTreeIndir::Size() const
{
    assert(isIndir() || OperIsBlk());
    return OperIsBlk() ? AsBlk()->Size() : genTypeSize(TypeGet());
}

//------------------------------------------------------------------------
// GenTreeIntConCommon::ImmedValNeedsReloc: does this immediate value needs recording a relocation with the VM?
//
// Arguments:
//    comp - Compiler instance
//
// Return Value:
//    True if this immediate value requires us to record a relocation for it; false otherwise.

bool GenTreeIntConCommon::ImmedValNeedsReloc(Compiler* comp)
{
    return comp->opts.compReloc && IsIconHandle();
}

//------------------------------------------------------------------------
// ImmedValCanBeFolded: can this immediate value be folded for op?
//
// Arguments:
//    comp - Compiler instance
//    op - Tree operator
//
// Return Value:
//    True if this immediate value can be folded for op; false otherwise.

bool GenTreeIntConCommon::ImmedValCanBeFolded(Compiler* comp, genTreeOps op)
{
    // In general, immediate values that need relocations can't be folded.
    // There are cases where we do want to allow folding of handle comparisons
    // (e.g., typeof(T) == typeof(int)).
    return !ImmedValNeedsReloc(comp) || (op == GT_EQ) || (op == GT_NE);
}

#ifdef TARGET_AMD64
// Returns true if this absolute address fits within the base of an addr mode.
// On Amd64 this effectively means, whether an absolute indirect address can
// be encoded as 32-bit offset relative to IP or zero.
bool GenTreeIntConCommon::FitsInAddrBase(Compiler* comp)
{
#ifdef DEBUG
    // Early out if PC-rel encoding of absolute addr is disabled.
    if (!comp->opts.compEnablePCRelAddr)
    {
        return false;
    }
#endif

    if (comp->opts.compReloc)
    {
        // During Ngen JIT is always asked to generate relocatable code.
        // Hence JIT will try to encode only icon handles as pc-relative offsets.
        return IsIconHandle() && (IMAGE_REL_BASED_REL32 == comp->eeGetRelocTypeHint((void*)IconValue()));
    }
    else
    {
        // During Jitting, we are allowed to generate non-relocatable code.
        // On Amd64 we can encode an absolute indirect addr as an offset relative to zero or RIP.
        // An absolute indir addr that can fit within 32-bits can ben encoded as an offset relative
        // to zero. All other absolute indir addr could be attempted to be encoded as RIP relative
        // based on reloc hint provided by VM.  RIP relative encoding is preferred over relative
        // to zero, because the former is one byte smaller than the latter.  For this reason
        // we check for reloc hint first and then whether addr fits in 32-bits next.
        //
        // VM starts off with an initial state to allow both data and code address to be encoded as
        // pc-relative offsets.  Hence JIT will attempt to encode all absolute addresses as pc-relative
        // offsets.  It is possible while jitting a method, an address could not be encoded as a
        // pc-relative offset.  In that case VM will note the overflow and will trigger re-jitting
        // of the method with reloc hints turned off for all future methods. Second time around
        // jitting will succeed since JIT will not attempt to encode data addresses as pc-relative
        // offsets.  Note that JIT will always attempt to relocate code addresses (.e.g call addr).
        // After an overflow, VM will assume any relocation recorded is for a code address and will
        // emit jump thunk if it cannot be encoded as pc-relative offset.
        return (IMAGE_REL_BASED_REL32 == comp->eeGetRelocTypeHint((void*)IconValue())) || FitsInI32();
    }
}

// Returns true if this icon value is encoded as addr needs recording a relocation with VM
bool GenTreeIntConCommon::AddrNeedsReloc(Compiler* comp)
{
    if (comp->opts.compReloc)
    {
        // During Ngen JIT is always asked to generate relocatable code.
        // Hence JIT will try to encode only icon handles as pc-relative offsets.
        return IsIconHandle() && (IMAGE_REL_BASED_REL32 == comp->eeGetRelocTypeHint((void*)IconValue()));
    }
    else
    {
        return IMAGE_REL_BASED_REL32 == comp->eeGetRelocTypeHint((void*)IconValue());
    }
}

#elif defined(TARGET_X86)
// Returns true if this absolute address fits within the base of an addr mode.
// On x86 all addresses are 4-bytes and can be directly encoded in an addr mode.
bool GenTreeIntConCommon::FitsInAddrBase(Compiler* comp)
{
#ifdef DEBUG
    // Early out if PC-rel encoding of absolute addr is disabled.
    if (!comp->opts.compEnablePCRelAddr)
    {
        return false;
    }
#endif

    return IsCnsIntOrI();
}

// Returns true if this icon value is encoded as addr needs recording a relocation with VM
bool GenTreeIntConCommon::AddrNeedsReloc(Compiler* comp)
{
    // If generating relocatable code, icons should be reported for recording relocatons.
    return comp->opts.compReloc && IsIconHandle();
}
#endif // TARGET_X86

#if defined(FEATURE_HW_INTRINSICS)
unsigned GenTreeVecCon::ElementCount(unsigned simdSize, var_types simdBaseType)
{
    return simdSize / genTypeSize(simdBaseType);
}
#endif // FEATURE_HW_INTRINSICS*/

//------------------------------------------------------------------------
// IsFieldAddr: Is "this" a static or class field address?
//
// Recognizes the following patterns:
//    this: ADD(baseAddr, CONST [FldSeq])
//    this: ADD(CONST [FldSeq], baseAddr)
//    this: CONST [FldSeq]
//
// Arguments:
//    comp      - the Compiler object
//    pBaseAddr - [out] parameter for "the base address"
//    pFldSeq   - [out] parameter for the field sequence
//    pOffset   - [out] parameter for the offset of the component struct fields
//
// Return Value:
//    If "this" matches patterns denoted above, with a valid FldSeq, this method
//    will return "true" and set "pBaseAddr" to some value, which must be used by
//    the caller as the key into the "first field map" to obtain the value for the
//    field, or to "nullptr", in which case the handle for the field is the primary
//    selector. For instance fields, "base address" will be the object reference,
//    for statics - the address to which the field offset with the field sequence
//    is added, see "impImportStaticFieldAccess" and "fgMorphFieldAddr".
//
bool GenTree::IsFieldAddr(Compiler* comp, GenTree** pBaseAddr, FieldSeq** pFldSeq, ssize_t* pOffset)
{
    assert(TypeIs(TYP_I_IMPL, TYP_BYREF, TYP_REF));

    *pBaseAddr = nullptr;
    *pFldSeq   = nullptr;

    GenTree*  baseAddr = nullptr;
    FieldSeq* fldSeq   = nullptr;
    ssize_t   offset   = 0;

    if (OperIs(GT_ADD))
    {
        if (AsOp()->gtOp2->IsCnsIntOrI())
        {
            baseAddr = AsOp()->gtOp1;
            fldSeq   = AsOp()->gtOp2->AsIntCon()->gtFieldSeq;
            offset   = AsOp()->gtOp2->AsIntCon()->IconValue();

            if ((fldSeq != nullptr) && (fldSeq->GetKind() == FieldSeq::FieldKind::SimpleStaticKnownAddress))
            {
                // fldSeq represents a known address (not a small offset) - bail out.
                return false;
            }
        }
        else
        {
            return false;
        }
    }
    else if (IsIconHandle(GTF_ICON_STATIC_HDL))
    {
        fldSeq = AsIntCon()->gtFieldSeq;
        offset = AsIntCon()->IconValue();
        assert((fldSeq == nullptr) || (fldSeq->GetKind() == FieldSeq::FieldKind::SimpleStaticKnownAddress));
    }
    else
    {
        return false;
    }

    if (fldSeq == nullptr)
    {
        return false;
    }

    // Subtract from the offset such that the portion remaining is relative to the field itself.
    offset -= fldSeq->GetOffset();

    // The above screens out obviously invalid cases, but we have more checks to perform. The
    // sequence returned from this method *must* start with either a class (NOT struct) field
    // or a static field. To avoid the expense of calling "getFieldClass" here, we will instead
    // rely on the invariant that TYP_REF base addresses can never appear for struct fields - we
    // will effectively treat such cases ("possible" in unsafe code) as undefined behavior.
    if (fldSeq->IsStaticField())
    {
        // For shared statics, we must encode the logical instantiation argument.
        if (fldSeq->IsSharedStaticField())
        {
            *pBaseAddr = baseAddr;
        }

        *pFldSeq = fldSeq;
        *pOffset = offset;
        return true;
    }

    if (baseAddr->TypeIs(TYP_REF))
    {
        assert(!comp->eeIsValueClass(comp->info.compCompHnd->getFieldClass(fldSeq->GetFieldHandle())));

        *pBaseAddr = baseAddr;
        *pFldSeq   = fldSeq;
        *pOffset   = offset;
        return true;
    }

    // This case is reached, for example, if we have a chain of struct fields that are based on
    // some pointer. We do not model such cases because we do not model maps for ByrefExposed
    // memory, as it does not have the non-aliasing property of GcHeap and reference types.
    return false;
}

bool Compiler::gtIsStaticFieldPtrToBoxedStruct(var_types fieldNodeType, CORINFO_FIELD_HANDLE fldHnd)
{
    if (fieldNodeType != TYP_REF)
    {
        return false;
    }
    noway_assert(fldHnd != nullptr);
    CorInfoType cit      = info.compCompHnd->getFieldType(fldHnd);
    var_types   fieldTyp = JITtype2varType(cit);
    return fieldTyp != TYP_REF;
}

//------------------------------------------------------------------------
// gtStoreDefinesField: Does the given parent store modify the given field?
//
// Arguments:
//    fieldVarDsc       - The field local
//    offset            - Offset of the store, relative to the parent
//    size              - Size of the store in bytes
//    pFieldStoreOffset - [out] parameter for the store's offset relative
//                        to the field local itself
//    pFieldStoreSize   - [out] parameter for the amount of the field's
//                        local's bytes affected by the store
//
// Return Value:
//    If the given store affects the given field local, "true, "false"
//    otherwise.
//
bool Compiler::gtStoreDefinesField(
    LclVarDsc* fieldVarDsc, ssize_t offset, unsigned size, ssize_t* pFieldStoreOffset, unsigned* pFieldStoreSize)
{
    ssize_t  fieldOffset = fieldVarDsc->lvFldOffset;
    unsigned fieldSize   = genTypeSize(fieldVarDsc); // No TYP_STRUCT field locals.

    ssize_t storeEndOffset = offset + static_cast<ssize_t>(size);
    ssize_t fieldEndOffset = fieldOffset + static_cast<ssize_t>(fieldSize);
    if ((fieldOffset < storeEndOffset) && (offset < fieldEndOffset))
    {
        *pFieldStoreOffset = (offset < fieldOffset) ? 0 : (offset - fieldOffset);
        *pFieldStoreSize   = static_cast<unsigned>(min(storeEndOffset, fieldEndOffset) - max(offset, fieldOffset));

        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// gtGetClassHandle: find class handle for a ref type
//
// Arguments:
//    tree -- tree to find handle for
//    pIsExact   [out] -- whether handle is exact type
//    pIsNonNull [out] -- whether tree value is known not to be null
//
// Return Value:
//    nullptr if class handle is unknown,
//        otherwise the class handle.
//    *pIsExact set true if tree type is known to be exactly the handle type,
//        otherwise actual type may be a subtype.
//    *pIsNonNull set true if tree value is known not to be null,
//        otherwise a null value is possible.

CORINFO_CLASS_HANDLE Compiler::gtGetClassHandle(GenTree* tree, bool* pIsExact, bool* pIsNonNull)
{
    // Set default values for our out params.
    *pIsNonNull                   = false;
    *pIsExact                     = false;
    CORINFO_CLASS_HANDLE objClass = nullptr;

    // Bail out if the tree is not a ref type.
    var_types treeType = tree->TypeGet();
    if (treeType != TYP_REF)
    {
        return objClass;
    }

    // Tunnel through commas.
    GenTree*         obj   = tree->gtEffectiveVal();
    const genTreeOps objOp = obj->OperGet();

    switch (objOp)
    {
        case GT_COMMA:
        {
            // gtEffectiveVal above means we shouldn't see commas here.
            assert(!"unexpected GT_COMMA");
            break;
        }

        case GT_LCL_VAR:
        {
            // For locals, pick up type info from the local table.
            const unsigned objLcl = obj->AsLclVar()->GetLclNum();

            objClass  = lvaTable[objLcl].lvClassHnd;
            *pIsExact = lvaTable[objLcl].lvClassIsExact;
            break;
        }

        case GT_CNS_INT:
        {
            if (obj->IsIconHandle(GTF_ICON_OBJ_HDL))
            {
                objClass = info.compCompHnd->getObjectType((CORINFO_OBJECT_HANDLE)obj->AsIntCon()->IconValue());
                if (objClass != NO_CLASS_HANDLE)
                {
                    // if we managed to get a class handle it's definitely not null
                    *pIsNonNull = true;
                    *pIsExact   = true;
                }
            }
            break;
        }

        case GT_RET_EXPR:
        {
            // If we see a RET_EXPR, recurse through to examine the
            // return value expression.
            GenTree* retExpr = obj->AsRetExpr()->gtInlineCandidate;
            objClass         = gtGetClassHandle(retExpr, pIsExact, pIsNonNull);
            break;
        }

        case GT_CALL:
        {
            GenTreeCall* call = obj->AsCall();
            if (call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
            {
                NamedIntrinsic ni = lookupNamedIntrinsic(call->gtCallMethHnd);
                if ((ni == NI_System_Array_Clone) || (ni == NI_System_Object_MemberwiseClone))
                {
                    objClass = gtGetClassHandle(call->gtArgs.GetThisArg()->GetNode(), pIsExact, pIsNonNull);
                    break;
                }

                CORINFO_CLASS_HANDLE specialObjClass = impGetSpecialIntrinsicExactReturnType(call);
                if (specialObjClass != nullptr)
                {
                    objClass    = specialObjClass;
                    *pIsExact   = true;
                    *pIsNonNull = true;
                    break;
                }
            }
            if (call->IsInlineCandidate() && !call->IsGuardedDevirtualizationCandidate())
            {
                // For inline candidates, we've already cached the return
                // type class handle in the inline info (for GDV candidates,
                // this data is valid only for a correct guess, so we cannot
                // use it).
                InlineCandidateInfo* inlInfo = call->GetSingleInlineCandidateInfo();
                assert(inlInfo != nullptr);

                // Grab it as our first cut at a return type.
                assert(inlInfo->methInfo.args.retType == CORINFO_TYPE_CLASS);
                objClass = inlInfo->methInfo.args.retTypeClass;

                // If the method is shared, the above may not capture
                // the most precise return type information (that is,
                // it may represent a shared return type and as such,
                // have instances of __Canon). See if we can use the
                // context to get at something more definite.
                //
                // For now, we do this here on demand rather than when
                // processing the call, but we could/should apply
                // similar sharpening to the argument and local types
                // of the inlinee.
                const unsigned retClassFlags = info.compCompHnd->getClassAttribs(objClass);
                if (retClassFlags & CORINFO_FLG_SHAREDINST)
                {
                    CORINFO_CONTEXT_HANDLE context = inlInfo->exactContextHnd;

                    if (context != nullptr)
                    {
                        CORINFO_CLASS_HANDLE exactClass = eeGetClassFromContext(context);

                        // Grab the signature in this context.
                        CORINFO_SIG_INFO sig;
                        eeGetMethodSig(call->gtCallMethHnd, &sig, exactClass);
                        assert(sig.retType == CORINFO_TYPE_CLASS);
                        objClass = sig.retTypeClass;
                    }
                }
            }
            else if (call->gtCallType == CT_USER_FUNC)
            {
                // For user calls, we can fetch the approximate return
                // type info from the method handle. Unfortunately
                // we've lost the exact context, so this is the best
                // we can do for now.
                CORINFO_METHOD_HANDLE method     = call->gtCallMethHnd;
                CORINFO_CLASS_HANDLE  exactClass = nullptr;
                CORINFO_SIG_INFO      sig;
                eeGetMethodSig(method, &sig, exactClass);
                if (sig.retType == CORINFO_TYPE_VOID)
                {
                    // This is a constructor call.
                    const unsigned methodFlags = info.compCompHnd->getMethodAttribs(method);
                    assert((methodFlags & CORINFO_FLG_CONSTRUCTOR) != 0);
                    objClass    = info.compCompHnd->getMethodClass(method);
                    *pIsExact   = true;
                    *pIsNonNull = true;
                }
                else
                {
                    assert(sig.retType == CORINFO_TYPE_CLASS);
                    objClass = sig.retTypeClass;
                }
            }
            else if (call->gtCallType == CT_HELPER)
            {
                objClass = gtGetHelperCallClassHandle(call, pIsExact, pIsNonNull);
            }

            break;
        }

        case GT_INTRINSIC:
        {
            GenTreeIntrinsic* intrinsic = obj->AsIntrinsic();

            if (intrinsic->gtIntrinsicName == NI_System_Object_GetType)
            {
                CORINFO_CLASS_HANDLE runtimeType = info.compCompHnd->getBuiltinClass(CLASSID_RUNTIME_TYPE);
                assert(runtimeType != NO_CLASS_HANDLE);

                objClass    = runtimeType;
                *pIsExact   = false;
                *pIsNonNull = true;
            }

            break;
        }

        case GT_CNS_STR:
        {
            // For literal strings, we know the class and that the
            // value is not null.
            objClass    = impGetStringClass();
            *pIsExact   = true;
            *pIsNonNull = true;
            break;
        }

        case GT_IND:
        {
            GenTreeIndir* indir = obj->AsIndir();
            GenTree*      base  = indir->Base();

            // indir(lcl_var_addr) --> lcl
            //
            // This comes up during constrained callvirt on ref types.
            //
            if (base->IsLclVarAddr())
            {
                const unsigned objLcl = base->AsLclVarCommon()->GetLclNum();
                objClass              = lvaTable[objLcl].lvClassHnd;
                *pIsExact             = lvaTable[objLcl].lvClassIsExact;
            }
            else if (base->OperIs(GT_INDEX_ADDR, GT_ARR_ELEM))
            {
                // indir(arr_elem(...)) -> array element type

                if (base->OperIs(GT_INDEX_ADDR))
                {
                    objClass = gtGetArrayElementClassHandle(base->AsIndexAddr()->Arr());
                }
                else
                {
                    objClass = gtGetArrayElementClassHandle(base->AsArrElem()->gtArrObj);
                }

                *pIsExact   = false;
                *pIsNonNull = false;
            }
            else if (base->OperGet() == GT_ADD)
            {
                // TODO-VNTypes: use "IsFieldAddr" here instead.

                // This could be a static field access.
                //
                // See if op1 is a static field base helper call
                // and if so, op2 will have the field info.
                GenTree* op1 = base->AsOp()->gtOp1;
                GenTree* op2 = base->AsOp()->gtOp2;

                if (op2->IsCnsIntOrI())
                {
                    FieldSeq* fieldSeq = op2->AsIntCon()->gtFieldSeq;
                    if ((fieldSeq != nullptr) && (fieldSeq->GetOffset() == op2->AsIntCon()->IconValue()))
                    {
                        // No benefit to calling gtGetFieldClassHandle here, as
                        // the exact field being accessed can vary.
                        CORINFO_FIELD_HANDLE fieldHnd   = fieldSeq->GetFieldHandle();
                        CORINFO_CLASS_HANDLE fieldClass = NO_CLASS_HANDLE;
                        var_types            fieldType  = eeGetFieldType(fieldHnd, &fieldClass);

                        if (fieldType == TYP_REF)
                        {
                            objClass = fieldClass;
                        }
                    }
                }
            }
            else if (base->IsIconHandle(GTF_ICON_CONST_PTR, GTF_ICON_STATIC_HDL))
            {
                // Check if we have IND(ICON_HANDLE) that represents a static field
                FieldSeq* fldSeq = base->AsIntCon()->gtFieldSeq;
                if ((fldSeq != nullptr) && (fldSeq->GetOffset() == base->AsIntCon()->IconValue()))
                {
                    CORINFO_FIELD_HANDLE fldHandle = base->AsIntCon()->gtFieldSeq->GetFieldHandle();
                    objClass                       = gtGetFieldClassHandle(fldHandle, pIsExact, pIsNonNull);
                }
            }
            else if (base->OperIs(GT_FIELD_ADDR))
            {
                objClass = gtGetFieldClassHandle(base->AsFieldAddr()->gtFldHnd, pIsExact, pIsNonNull);
            }
            break;
        }

        case GT_BOX:
        {
            // Box should just wrap a local var reference which has
            // the type we're looking for. Also box only represents a
            // non-nullable value type so result cannot be null.
            GenTreeBox* box     = obj->AsBox();
            GenTree*    boxTemp = box->BoxOp();
            assert(boxTemp->IsLocal());
            const unsigned boxTempLcl = boxTemp->AsLclVar()->GetLclNum();
            objClass                  = lvaTable[boxTempLcl].lvClassHnd;
            *pIsExact                 = lvaTable[boxTempLcl].lvClassIsExact;
            *pIsNonNull               = true;
            break;
        }

        default:
        {
            break;
        }
    }

    if ((objClass != NO_CLASS_HANDLE) && !*pIsExact && JitConfig.JitEnableExactDevirtualization())
    {
        CORINFO_CLASS_HANDLE exactClass;
        if (info.compCompHnd->getExactClasses(objClass, 1, &exactClass) == 1)
        {
            *pIsExact = true;
            objClass  = exactClass;
        }
        else
        {
            *pIsExact = info.compCompHnd->isExactType(objClass);
        }
    }

    return objClass;
}

//------------------------------------------------------------------------
// gtGetHelperCallClassHandle: find class handle for return value of a
//   helper call
//
// Arguments:
//    call - helper call to examine
//    pIsExact - [OUT] true if type is known exactly
//    pIsNonNull - [OUT] true if return value is not null
//
// Return Value:
//    nullptr if helper call result is not a ref class, or the class handle
//    is unknown, otherwise the class handle.

CORINFO_CLASS_HANDLE Compiler::gtGetHelperCallClassHandle(GenTreeCall* call, bool* pIsExact, bool* pIsNonNull)
{
    assert(call->gtCallType == CT_HELPER);

    *pIsNonNull                    = false;
    *pIsExact                      = false;
    CORINFO_CLASS_HANDLE  objClass = nullptr;
    const CorInfoHelpFunc helper   = eeGetHelperNum(call->gtCallMethHnd);

    switch (helper)
    {
        case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE:
        case CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE_MAYBENULL:
        {
            // Note for some runtimes these helpers return exact types.
            //
            // But in those cases the types are also sealed, so there's no
            // need to claim exactness here.
            const bool           helperResultNonNull = (helper == CORINFO_HELP_TYPEHANDLE_TO_RUNTIMETYPE);
            CORINFO_CLASS_HANDLE runtimeType         = info.compCompHnd->getBuiltinClass(CLASSID_RUNTIME_TYPE);

            assert(runtimeType != NO_CLASS_HANDLE);

            objClass    = runtimeType;
            *pIsNonNull = helperResultNonNull;
            break;
        }

        case CORINFO_HELP_BOX:
        case CORINFO_HELP_BOX_NULLABLE:
        {
            GenTree* typeArg = call->gtArgs.GetUserArgByIndex(0)->GetNode();
            if (typeArg->IsIconHandle(GTF_ICON_CLASS_HDL))
            {
                const bool isNullableHelper = (helper == CORINFO_HELP_BOX_NULLABLE);

                objClass = gtGetHelperArgClassHandle(typeArg);
                if ((objClass != NO_CLASS_HANDLE) && isNullableHelper)
                {
                    // Nullable<T> is boxed as just T (via CORINFO_HELP_BOX_NULLABLE)
                    objClass = info.compCompHnd->getTypeForBox(objClass);
                }

                if (objClass != NO_CLASS_HANDLE)
                {
                    // CORINFO_HELP_BOX_NULLABLE may return null
                    // CORINFO_HELP_BOX always returns non-null
                    *pIsNonNull = !isNullableHelper;
                    *pIsExact   = true;
                }
            }
        }
        break;

        case CORINFO_HELP_CHKCASTCLASS:
        case CORINFO_HELP_CHKCASTANY:
        case CORINFO_HELP_CHKCASTARRAY:
        case CORINFO_HELP_CHKCASTINTERFACE:
        case CORINFO_HELP_CHKCASTCLASS_SPECIAL:
        case CORINFO_HELP_ISINSTANCEOFINTERFACE:
        case CORINFO_HELP_ISINSTANCEOFARRAY:
        case CORINFO_HELP_ISINSTANCEOFCLASS:
        case CORINFO_HELP_ISINSTANCEOFANY:
        {
            // Fetch the class handle from the helper call arglist
            GenTree*             typeArg = call->gtArgs.GetArgByIndex(0)->GetNode();
            CORINFO_CLASS_HANDLE castHnd = gtGetHelperArgClassHandle(typeArg);

            // We generally assume the type being cast to is the best type
            // for the result, unless it is an interface type.
            //
            // TODO-CQ: when we have default interface methods then
            // this might not be the best assumption. A similar issue arises when
            // typing the temp in impCastClassOrIsInstToTree, when we
            // expand the cast inline.
            if (castHnd != nullptr)
            {
                DWORD attrs = info.compCompHnd->getClassAttribs(castHnd);

                if ((attrs & CORINFO_FLG_INTERFACE) != 0)
                {
                    castHnd = nullptr;
                }
            }

            // If we don't have a good estimate for the type we can use the
            // type from the value being cast instead.
            if (castHnd == nullptr)
            {
                GenTree* valueArg = call->gtArgs.GetArgByIndex(1)->GetNode();
                castHnd           = gtGetClassHandle(valueArg, pIsExact, pIsNonNull);
            }

            // We don't know at jit time if the cast will succeed or fail, but if it
            // fails at runtime then an exception is thrown for cast helpers, or the
            // result is set null for instance helpers.
            //
            // So it safe to claim the result has the cast type.
            // Note we don't know for sure that it is exactly this type.
            if (castHnd != nullptr)
            {
                objClass = castHnd;
            }

            break;
        }

        case CORINFO_HELP_NEWARR_1_DIRECT:
        case CORINFO_HELP_NEWARR_1_MAYBEFROZEN:
        case CORINFO_HELP_NEWARR_1_OBJ:
        case CORINFO_HELP_NEWARR_1_VC:
        case CORINFO_HELP_NEWARR_1_ALIGN8:
        case CORINFO_HELP_READYTORUN_NEWARR_1:
        {
            CORINFO_CLASS_HANDLE arrayHnd = (CORINFO_CLASS_HANDLE)call->compileTimeHelperArgumentHandle;

            if (arrayHnd != NO_CLASS_HANDLE)
            {
                objClass    = arrayHnd;
                *pIsExact   = true;
                *pIsNonNull = true;
            }
            break;
        }

        default:
            break;
    }

    return objClass;
}

//------------------------------------------------------------------------
// gtGetArrayElementClassHandle: find class handle for elements of an array
// of ref types
//
// Arguments:
//    array -- array to find handle for
//
// Return Value:
//    nullptr if element class handle is unknown, otherwise the class handle.

CORINFO_CLASS_HANDLE Compiler::gtGetArrayElementClassHandle(GenTree* array)
{
    bool                 isArrayExact   = false;
    bool                 isArrayNonNull = false;
    CORINFO_CLASS_HANDLE arrayClassHnd  = gtGetClassHandle(array, &isArrayExact, &isArrayNonNull);

    if (arrayClassHnd != nullptr)
    {
        // We know the class of the reference
        DWORD attribs = info.compCompHnd->getClassAttribs(arrayClassHnd);

        if ((attribs & CORINFO_FLG_ARRAY) != 0)
        {
            // We know for sure it is an array
            CORINFO_CLASS_HANDLE elemClassHnd  = nullptr;
            CorInfoType          arrayElemType = info.compCompHnd->getChildType(arrayClassHnd, &elemClassHnd);

            if (arrayElemType == CORINFO_TYPE_CLASS)
            {
                // We know it is an array of ref types
                return elemClassHnd;
            }
        }
    }

    return nullptr;
}

//------------------------------------------------------------------------
// gtGetFieldClassHandle: find class handle for a field
//
// Arguments:
//    fieldHnd - field handle for field in question
//    pIsExact - [OUT] true if type is known exactly
//    pIsNonNull - [OUT] true if field value is not null
//
// Return Value:
//    nullptr if helper call result is not a ref class, or the class handle
//    is unknown, otherwise the class handle.
//
//    May examine runtime state of static field instances.

CORINFO_CLASS_HANDLE Compiler::gtGetFieldClassHandle(CORINFO_FIELD_HANDLE fieldHnd, bool* pIsExact, bool* pIsNonNull)
{
    CORINFO_CLASS_HANDLE fieldClass   = nullptr;
    CorInfoType          fieldCorType = info.compCompHnd->getFieldType(fieldHnd, &fieldClass);

    if (fieldCorType == CORINFO_TYPE_CLASS)
    {
        // Optionally, look at the actual type of the field's value
        bool queryForCurrentClass = true;
        INDEBUG(queryForCurrentClass = (JitConfig.JitQueryCurrentStaticFieldClass() > 0););

        if (queryForCurrentClass)
        {
#if DEBUG
            char fieldNameBuffer[128];
            char classNameBuffer[128];
            JITDUMP("Querying runtime about current class of field %s (declared as %s)\n",
                    eeGetFieldName(fieldHnd, true, fieldNameBuffer, sizeof(fieldNameBuffer)),
                    eeGetClassName(fieldClass, classNameBuffer, sizeof(classNameBuffer)));
#endif // DEBUG

            // Is this a fully initialized init-only static field?
            //
            // Note we're not asking for speculative results here, yet.
            CORINFO_CLASS_HANDLE currentClass = info.compCompHnd->getStaticFieldCurrentClass(fieldHnd);

            if (currentClass != NO_CLASS_HANDLE)
            {
                // Yes! We know the class exactly and can rely on this to always be true.
                fieldClass  = currentClass;
                *pIsExact   = true;
                *pIsNonNull = true;
#ifdef DEBUG
                char buffer[128];
                JITDUMP("Runtime reports field is init-only and initialized and has class %s\n",
                        eeGetClassName(fieldClass, buffer, sizeof(buffer)));
#endif
            }
            else
            {
                JITDUMP("Field's current class not available\n");
            }

            return fieldClass;
        }
    }

    return NO_CLASS_HANDLE;
}

//------------------------------------------------------------------------
// gtIsTypeof: Checks if the tree is a typeof()
//
// Arguments:
//    tree   - the tree that is checked
//    handle - (optional, default nullptr) - if non-null is set to the type
//
// Return Value:
//    Is the tree typeof()
//
bool Compiler::gtIsTypeof(GenTree* tree, CORINFO_CLASS_HANDLE* handle)
{
    if (tree->IsCall())
    {
        GenTreeCall* call = tree->AsCall();
        if (gtIsTypeHandleToRuntimeTypeHelper(call))
        {
            assert(call->gtArgs.CountArgs() == 1);
            CORINFO_CLASS_HANDLE hClass = gtGetHelperArgClassHandle(call->gtArgs.GetArgByIndex(0)->GetEarlyNode());
            if (hClass != NO_CLASS_HANDLE)
            {
                if (handle != nullptr)
                {
                    *handle = hClass;
                }
                return true;
            }
        }
    }
    if (handle != nullptr)
    {
        *handle = NO_CLASS_HANDLE;
    }
    return false;
}

//------------------------------------------------------------------------
// gtCallGetDefinedRetBufLclAddr:
//   Get the tree corresponding to the address of the retbuf that this call defines.
//
// Parameters:
//   call - The call node
//
// Returns:
//   A tree representing the address of a local.
//
GenTreeLclVarCommon* Compiler::gtCallGetDefinedRetBufLclAddr(GenTreeCall* call)
{
    if (!call->IsOptimizingRetBufAsLocal())
    {
        return nullptr;
    }

    CallArg* retBufArg = call->gtArgs.GetRetBufferArg();
    assert(retBufArg != nullptr);

    GenTree* node = retBufArg->GetNode();
    switch (node->OperGet())
    {
        // Get the value from putarg wrapper nodes
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
            node = node->AsOp()->gtGetOp1();
            break;

        default:
            break;
    }

    // This may be called very late to check validity of LIR.
    node = node->gtSkipReloadOrCopy();

    assert(node->OperIs(GT_LCL_ADDR) && lvaGetDesc(node->AsLclVarCommon())->IsHiddenBufferStructArg());

    return node->AsLclVarCommon();
}

//------------------------------------------------------------------------
// ParseArrayAddress: Rehydrate the array and index expression from ARR_ADDR.
//
// Arguments:
//    comp    - The Compiler instance
//    pArr    - [out] parameter for the tree representing the array instance
//              (either an array object pointer, or perhaps a byref to some element)
//    pInxVN  - [out] parameter for the value number representing the index
//
// Return Value:
//    Will set "*pArr" to "nullptr" if this array address is not parseable.
//
void GenTreeArrAddr::ParseArrayAddress(Compiler* comp, GenTree** pArr, ValueNum* pInxVN)
{
    *pArr                 = nullptr;
    ValueNum       inxVN  = ValueNumStore::NoVN;
    target_ssize_t offset = 0;
    ParseArrayAddressWork(this->Addr(), comp, 1, pArr, &inxVN, &offset);

    // If we didn't find an array reference (perhaps it is the constant null?) we will give up.
    if (*pArr == nullptr)
    {
        return;
    }

    // OK, new we have to figure out if any part of the "offset" is a constant contribution to the index.
    target_ssize_t elemOffset = GetFirstElemOffset();
    unsigned       elemSizeUn = (GetElemType() == TYP_STRUCT) ? comp->typGetObjLayout(GetElemClassHandle())->GetSize()
                                                        : genTypeSize(GetElemType());

    assert(FitsIn<target_ssize_t>(elemSizeUn));
    target_ssize_t elemSize         = static_cast<target_ssize_t>(elemSizeUn);
    target_ssize_t constIndexOffset = offset - elemOffset;

    // This should be divisible by the element size...
    assert((constIndexOffset % elemSize) == 0);
    target_ssize_t constIndex = constIndexOffset / elemSize;

    ValueNumStore* vnStore = comp->GetValueNumStore();

    if (inxVN == ValueNumStore::NoVN)
    {
        // Must be a constant index.
        *pInxVN = vnStore->VNForPtrSizeIntCon(constIndex);
    }
    else
    {
        //
        // Perform ((inxVN / elemSizeVN) + vnForConstIndex)
        //

        // The value associated with the index value number (inxVN) is the offset into the array,
        // which has been scaled by element size. We need to recover the array index from that offset
        if (vnStore->IsVNConstant(inxVN))
        {
            target_ssize_t index = vnStore->CoercedConstantValue<target_ssize_t>(inxVN);
            noway_assert(elemSize > 0 && ((index % elemSize) == 0));
            *pInxVN = vnStore->VNForPtrSizeIntCon((index / elemSize) + constIndex);
        }
        else
        {
            bool canFoldDiv = false;

            // If the index VN is a MUL by elemSize, see if we can eliminate it instead of adding
            // the division by elemSize.
            VNFuncApp funcApp;
            if (vnStore->GetVNFunc(inxVN, &funcApp) && funcApp.m_func == (VNFunc)GT_MUL)
            {
                ValueNum vnForElemSize = vnStore->VNForLongCon(elemSize);

                // One of the multiply operand is elemSize, so the resulting
                // index VN should simply be the other operand.
                if (funcApp.m_args[1] == vnForElemSize)
                {
                    *pInxVN    = funcApp.m_args[0];
                    canFoldDiv = true;
                }
                else if (funcApp.m_args[0] == vnForElemSize)
                {
                    *pInxVN    = funcApp.m_args[1];
                    canFoldDiv = true;
                }
            }

            // Perform ((inxVN / elemSizeVN) + vnForConstIndex)
            if (!canFoldDiv)
            {
                ValueNum vnForElemSize  = vnStore->VNForPtrSizeIntCon(elemSize);
                ValueNum vnForScaledInx = vnStore->VNForFunc(TYP_I_IMPL, VNFunc(GT_DIV), inxVN, vnForElemSize);
                *pInxVN                 = vnForScaledInx;
            }

            if (constIndex != 0)
            {
                ValueNum vnForConstIndex = vnStore->VNForPtrSizeIntCon(constIndex);

                *pInxVN = comp->GetValueNumStore()->VNForFunc(TYP_I_IMPL, VNFunc(GT_ADD), *pInxVN, vnForConstIndex);
            }
        }
    }
}

/* static */ void GenTreeArrAddr::ParseArrayAddressWork(
    GenTree* tree, Compiler* comp, target_ssize_t inputMul, GenTree** pArr, ValueNum* pInxVN, target_ssize_t* pOffset)
{
    if (tree->TypeIs(TYP_REF))
    {
        // This must be the array pointer.
        *pArr = tree;
        assert(inputMul == 1); // Can't multiply the array pointer by anything.
    }
    else
    {
        switch (tree->OperGet())
        {
            case GT_CNS_INT:
                assert(!tree->AsIntCon()->ImmedValNeedsReloc(comp));
                // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t
                // type.
                *pOffset += (inputMul * (target_ssize_t)(tree->AsIntCon()->gtIconVal));
                return;

            case GT_ADD:
            case GT_SUB:
                ParseArrayAddressWork(tree->AsOp()->gtOp1, comp, inputMul, pArr, pInxVN, pOffset);
                if (tree->OperIs(GT_SUB))
                {
                    inputMul = -inputMul;
                }
                ParseArrayAddressWork(tree->AsOp()->gtOp2, comp, inputMul, pArr, pInxVN, pOffset);
                return;

            case GT_MUL:
            {
                // If one op is a constant, continue parsing down.
                target_ssize_t subMul   = 0;
                GenTree*       nonConst = nullptr;
                if (tree->AsOp()->gtOp1->IsCnsIntOrI())
                {
                    // If the other arg is an int constant, and is a "not-a-field", choose
                    // that as the multiplier, thus preserving constant index offsets...
                    if (tree->AsOp()->gtOp2->OperGet() == GT_CNS_INT &&
                        tree->AsOp()->gtOp2->AsIntCon()->gtFieldSeq == nullptr)
                    {
                        assert(!tree->AsOp()->gtOp2->AsIntCon()->ImmedValNeedsReloc(comp));
                        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntConCommon::gtIconVal had
                        // target_ssize_t type.
                        subMul   = (target_ssize_t)tree->AsOp()->gtOp2->AsIntConCommon()->IconValue();
                        nonConst = tree->AsOp()->gtOp1;
                    }
                    else
                    {
                        assert(!tree->AsOp()->gtOp1->AsIntCon()->ImmedValNeedsReloc(comp));
                        // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntConCommon::gtIconVal had
                        // target_ssize_t type.
                        subMul   = (target_ssize_t)tree->AsOp()->gtOp1->AsIntConCommon()->IconValue();
                        nonConst = tree->AsOp()->gtOp2;
                    }
                }
                else if (tree->AsOp()->gtOp2->IsCnsIntOrI())
                {
                    assert(!tree->AsOp()->gtOp2->AsIntCon()->ImmedValNeedsReloc(comp));
                    // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntConCommon::gtIconVal had
                    // target_ssize_t type.
                    subMul   = (target_ssize_t)tree->AsOp()->gtOp2->AsIntConCommon()->IconValue();
                    nonConst = tree->AsOp()->gtOp1;
                }
                if (nonConst != nullptr)
                {
                    ParseArrayAddressWork(nonConst, comp, inputMul * subMul, pArr, pInxVN, pOffset);
                    return;
                }
                // Otherwise, exit the switch, treat as a contribution to the index.
            }
            break;

            case GT_LSH:
                // If one op is a constant, continue parsing down.
                if (tree->AsOp()->gtOp2->IsCnsIntOrI())
                {
                    assert(!tree->AsOp()->gtOp2->AsIntCon()->ImmedValNeedsReloc(comp));
                    // TODO-CrossBitness: we wouldn't need the cast below if GenTreeIntCon::gtIconVal had target_ssize_t
                    // type.
                    target_ssize_t shiftVal = (target_ssize_t)tree->AsOp()->gtOp2->AsIntConCommon()->IconValue();
                    target_ssize_t subMul   = target_ssize_t{1} << shiftVal;
                    ParseArrayAddressWork(tree->AsOp()->gtOp1, comp, inputMul * subMul, pArr, pInxVN, pOffset);
                    return;
                }
                // Otherwise, exit the switch, treat as a contribution to the index.
                break;

            case GT_COMMA:
                // We don't care about exceptions for this purpose.
                if (tree->AsOp()->gtOp1->OperIs(GT_BOUNDS_CHECK) || tree->AsOp()->gtOp1->IsNothingNode())
                {
                    ParseArrayAddressWork(tree->AsOp()->gtOp2, comp, inputMul, pArr, pInxVN, pOffset);
                    return;
                }
                break;

            default:
                break;
        }
        // If we didn't return above, must be a contribution to the non-constant part of the index VN.
        ValueNum vn = comp->GetValueNumStore()->VNLiberalNormalValue(tree->gtVNPair);
        if (inputMul != 1)
        {
            ValueNum mulVN = comp->GetValueNumStore()->VNForLongCon(inputMul);
            vn             = comp->GetValueNumStore()->VNForFunc(tree->TypeGet(), VNFunc(GT_MUL), mulVN, vn);
        }
        if (*pInxVN == ValueNumStore::NoVN)
        {
            *pInxVN = vn;
        }
        else
        {
            *pInxVN = comp->GetValueNumStore()->VNForFunc(tree->TypeGet(), VNFunc(GT_ADD), *pInxVN, vn);
        }
    }
}

//------------------------------------------------------------------------
// IsArrayAddr: Is "this" an expression for an array address?
//
// Recognizes the following patterns:
//    this: ARR_ADDR
//    this: ADD(ARR_ADDR, CONST)
//
// Arguments:
//    pArrAddr - [out] parameter for the found ARR_ADDR node
//
// Return Value:
//    Whether "this" matches the pattern denoted above.
//
bool GenTree::IsArrayAddr(GenTreeArrAddr** pArrAddr)
{
    GenTree* addr = this;
    if (addr->OperIs(GT_ADD) && addr->AsOp()->gtGetOp2()->IsCnsIntOrI())
    {
        addr = addr->AsOp()->gtGetOp1();
    }

    if (addr->OperIs(GT_ARR_ADDR))
    {
        *pArrAddr = addr->AsArrAddr();
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// SupportsSettingZeroFlag: Returns true if this is an arithmetic operation
// whose codegen supports setting the "zero flag" as part of its operation.
//
// Return Value:
//    True if so. A false return does not imply that codegen for the node will
//    not trash the zero flag.
//
// Remarks:
//    For example, for EQ (AND x y) 0, both xarch and arm64 can emit
//    instructions that directly set the flags after the 'AND' and thus no
//    comparison is needed.
//
//    The backend expects any node for which the flags will be consumed to be
//    marked with GTF_SET_FLAGS.
//
bool GenTree::SupportsSettingZeroFlag()
{
#if defined(TARGET_XARCH)
    if (OperIs(GT_AND, GT_OR, GT_XOR, GT_ADD, GT_SUB, GT_NEG))
    {
        return true;
    }

#ifdef FEATURE_HW_INTRINSICS
    if (OperIs(GT_HWINTRINSIC) && emitter::DoesWriteZeroFlag(HWIntrinsicInfo::lookupIns(AsHWIntrinsic())))
    {
        return true;
    }
#endif
#elif defined(TARGET_ARM64)
    if (OperIs(GT_AND))
    {
        return true;
    }

    // We do not support setting zero flag for madd/msub.
    if (OperIs(GT_ADD, GT_SUB) && (!gtGetOp2()->OperIs(GT_MUL) || !gtGetOp2()->isContained()))
    {
        return true;
    }
#endif

    return false;
}

//------------------------------------------------------------------------
// Create: Create or retrieve a field sequence for the given field handle.
//
// The field sequence instance contains some cached information relevant to
// its usage; thus for a given handle all callers of this method must pass
// the same set of arguments.
//
// Arguments:
//    fieldHnd  - The field handle
//    offset    - The "offset" value for the field sequence
//    fieldKind - The field's kind
//
// Return Value:
//    The canonical field sequence for the given field.
//
FieldSeq* FieldSeqStore::Create(CORINFO_FIELD_HANDLE fieldHnd, ssize_t offset, FieldSeq::FieldKind fieldKind)
{
    FieldSeq* fieldSeq = m_map.Emplace(fieldHnd, fieldHnd, offset, fieldKind);

    assert(fieldSeq->GetOffset() == offset);
    assert(fieldSeq->GetKind() == fieldKind);

    return fieldSeq;
}

//------------------------------------------------------------------------
// Append: "Merge" two field sequences together.
//
// A field sequence only explicitly represents its "head", i. e. the static
// or class field with which it begins. The struct fields that are part of
// it are "implicit" - represented in IR as offsets with "empty" sequences.
// Thus when two sequences are merged, only one can be explicit:
//
//    field seq + empty     => field seq
//    empty     + field seq => field seq
//    empty     + empty     => empty
//    field seq + field seq => illegal
//
// Arguments:
//    a - The field sequence
//    b - The second sequence
//
// Return Value:
//    The result of "merging" "a" and "b" (see description).
//
FieldSeq* FieldSeqStore::Append(FieldSeq* a, FieldSeq* b)
{
    if (a == nullptr)
    {
        return b;
    }
    if (b == nullptr)
    {
        return a;
    }

    // In UB-like code (such as manual IL) we can see an addition of two static field addresses.
    // Treat that as cancelling out the sequence, since the result will point nowhere.
    //
    // It may be possible for the JIT to encounter other types of UB additions, such as due to
    // complex optimizations, inlining, etc. In release we'll still do the right thing by returning
    // nullptr here, but the more conservative assert can help avoid JIT bugs

    assert(a->GetKind() == FieldSeq::FieldKind::SimpleStaticKnownAddress);
    assert(b->GetKind() == FieldSeq::FieldKind::SimpleStaticKnownAddress);

    return nullptr;
}

FieldSeq::FieldSeq(CORINFO_FIELD_HANDLE fieldHnd, ssize_t offset, FieldKind fieldKind) : m_offset(offset)
{
    assert(fieldHnd != NO_FIELD_HANDLE);

    uintptr_t handleValue = reinterpret_cast<uintptr_t>(fieldHnd);

    assert((handleValue & FIELD_KIND_MASK) == 0);
    m_fieldHandleAndKind = handleValue | static_cast<uintptr_t>(fieldKind);

    assert(JitTls::GetCompiler()->eeIsFieldStatic(fieldHnd) == IsStaticField());
    if (fieldKind == FieldKind::Instance)
    {
        // TODO: enable this assert. At the time of writing, crossgen2 had a bug where the value "getFieldOffset"
        // would return for fields with an offset unknown at compile time was incorrect (not zero).
        // assert(static_cast<ssize_t>(JitTls::GetCompiler()->info.compCompHnd->getFieldOffset(fieldHnd)) == offset);
    }
}

#ifdef FEATURE_SIMD
//-------------------------------------------------------------------
// SetOpLclRelatedToSIMDIntrinsic: Determine if the tree has a local var that needs to be set
// as used by a SIMD intrinsic, and if so, set that local var appropriately.
//
// Arguments:
//     op - The tree, to be an operand of a new SIMD-related node, to check.
//
void Compiler::SetOpLclRelatedToSIMDIntrinsic(GenTree* op)
{
    if ((op != nullptr) && op->OperIsScalarLocal())
    {
        setLclRelatedToSIMDIntrinsic(op);
    }
}

void GenTreeMultiOp::ResetOperandArray(size_t    newOperandCount,
                                       Compiler* compiler,
                                       GenTree** inlineOperands,
                                       size_t    inlineOperandCount)
{
    size_t    oldOperandCount = GetOperandCount();
    GenTree** oldOperands     = GetOperandArray();

    if (newOperandCount > oldOperandCount)
    {
        if (newOperandCount <= inlineOperandCount)
        {
            assert(oldOperandCount <= inlineOperandCount);
            assert(oldOperands == inlineOperands);
        }
        else
        {
            // The most difficult case: we need to recreate the dynamic array.
            assert(compiler != nullptr);

            m_operands = compiler->getAllocator(CMK_ASTNode).allocate<GenTree*>(newOperandCount);
        }
    }
    else
    {
        // We are shrinking the array and may in process switch to an inline representation.
        // We choose to do so for simplicity ("if a node has <= InlineOperandCount operands,
        // then it stores them inline"), but actually it may be more profitable to not do that,
        // it will save us a copy and a potential cache miss (though the latter seems unlikely).

        if ((newOperandCount <= inlineOperandCount) && (oldOperands != inlineOperands))
        {
            m_operands = inlineOperands;
        }
    }

#ifdef DEBUG
    for (size_t i = 0; i < newOperandCount; i++)
    {
        m_operands[i] = nullptr;
    }
#endif // DEBUG

    SetOperandCount(newOperandCount);
}

/* static */ bool GenTreeMultiOp::OperandsAreEqual(GenTreeMultiOp* op1, GenTreeMultiOp* op2)
{
    if (op1->GetOperandCount() != op2->GetOperandCount())
    {
        return false;
    }

    for (size_t i = 1; i <= op1->GetOperandCount(); i++)
    {
        if (!Compare(op1->Op(i), op2->Op(i)))
        {
            return false;
        }
    }

    return true;
}

void GenTreeMultiOp::InitializeOperands(GenTree** operands, size_t operandCount)
{
    for (size_t i = 0; i < operandCount; i++)
    {
        m_operands[i] = operands[i];
        gtFlags |= (operands[i]->gtFlags & GTF_ALL_EFFECT);
    }

    SetOperandCount(operandCount);
}

var_types GenTreeJitIntrinsic::GetAuxiliaryType() const
{
    CorInfoType auxiliaryJitType = GetAuxiliaryJitType();

    if (auxiliaryJitType == CORINFO_TYPE_UNDEF)
    {
        return TYP_UNKNOWN;
    }
    return JitType2PreciseVarType(auxiliaryJitType);
}

var_types GenTreeJitIntrinsic::GetSimdBaseType() const
{
    CorInfoType simdBaseJitType = GetSimdBaseJitType();

    if (simdBaseJitType == CORINFO_TYPE_UNDEF)
    {
        return TYP_UNKNOWN;
    }
    return JitType2PreciseVarType(simdBaseJitType);
}
#endif // FEATURE_SIMD

#ifdef FEATURE_HW_INTRINSICS
bool GenTree::isCommutativeHWIntrinsic() const
{
    assert(gtOper == GT_HWINTRINSIC);

    const GenTreeHWIntrinsic* node = AsHWIntrinsic();
    NamedIntrinsic            id   = node->GetHWIntrinsicId();

    if (HWIntrinsicInfo::IsCommutative(id))
    {
        return true;
    }

    if (HWIntrinsicInfo::IsMaybeCommutative(id))
    {
        switch (id)
        {
#ifdef TARGET_XARCH
            case NI_SSE_Max:
            case NI_SSE_Min:
            {
                return false;
            }

            case NI_SSE2_Max:
            case NI_SSE2_Min:
            {
                return !varTypeIsFloating(node->GetSimdBaseType());
            }

            case NI_AVX_Max:
            case NI_AVX_Min:
            {
                return false;
            }

            case NI_AVX512F_Max:
            case NI_AVX512F_Min:
            {
                return !varTypeIsFloating(node->GetSimdBaseType());
            }
#endif // TARGET_XARCH

            default:
            {
                unreached();
            }
        }
    }

    return false;
}

bool GenTree::isContainableHWIntrinsic() const
{
    assert(gtOper == GT_HWINTRINSIC);

#ifdef TARGET_XARCH
    switch (AsHWIntrinsic()->GetHWIntrinsicId())
    {
        case NI_SSE_LoadAlignedVector128:
        case NI_SSE_LoadScalarVector128:
        case NI_SSE2_LoadAlignedVector128:
        case NI_SSE2_LoadScalarVector128:
        case NI_AVX_LoadAlignedVector256:
        case NI_AVX512F_LoadAlignedVector512:
        {
            // These loads are contained as part of a HWIntrinsic operation
            return true;
        }

        case NI_AVX512F_ConvertToVector256Int32:
        case NI_AVX512F_ConvertToVector256UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32:
        case NI_AVX512F_VL_ConvertToVector128UInt32WithSaturation:
        {
            if (varTypeIsFloating(AsHWIntrinsic()->GetSimdBaseType()))
            {
                return false;
            }
            FALLTHROUGH;
        }

        case NI_Vector128_GetElement:
        case NI_Vector128_ToScalar:
        case NI_Vector256_ToScalar:
        case NI_Vector512_ToScalar:
        case NI_SSE2_ConvertToInt32:
        case NI_SSE2_ConvertToUInt32:
        case NI_SSE2_X64_ConvertToInt64:
        case NI_SSE2_X64_ConvertToUInt64:
        case NI_SSE2_Extract:
        case NI_SSE41_Extract:
        case NI_SSE41_X64_Extract:
        case NI_AVX_ExtractVector128:
        case NI_AVX2_ConvertToInt32:
        case NI_AVX2_ConvertToUInt32:
        case NI_AVX2_ExtractVector128:
        case NI_AVX512F_ExtractVector128:
        case NI_AVX512F_ExtractVector256:
        case NI_AVX512F_ConvertToVector128Byte:
        case NI_AVX512F_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_ConvertToVector128Int16:
        case NI_AVX512F_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_ConvertToVector128SByte:
        case NI_AVX512F_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_ConvertToVector128UInt16:
        case NI_AVX512F_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int16:
        case NI_AVX512F_ConvertToVector256Int16WithSaturation:
        case NI_AVX512F_ConvertToVector256Int32WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt16:
        case NI_AVX512F_ConvertToVector256UInt16WithSaturation:
        case NI_AVX512F_ConvertToVector256UInt32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Byte:
        case NI_AVX512F_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int16:
        case NI_AVX512F_VL_ConvertToVector128Int16WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128Int32:
        case NI_AVX512F_VL_ConvertToVector128Int32WithSaturation:
        case NI_AVX512F_VL_ConvertToVector128SByte:
        case NI_AVX512F_VL_ConvertToVector128SByteWithSaturation:
        case NI_AVX512F_VL_ConvertToVector128UInt16:
        case NI_AVX512F_VL_ConvertToVector128UInt16WithSaturation:
        case NI_AVX512BW_ConvertToVector256Byte:
        case NI_AVX512BW_ConvertToVector256ByteWithSaturation:
        case NI_AVX512BW_ConvertToVector256SByte:
        case NI_AVX512BW_ConvertToVector256SByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128Byte:
        case NI_AVX512BW_VL_ConvertToVector128ByteWithSaturation:
        case NI_AVX512BW_VL_ConvertToVector128SByte:
        case NI_AVX512BW_VL_ConvertToVector128SByteWithSaturation:
        case NI_AVX512DQ_ExtractVector128:
        case NI_AVX512DQ_ExtractVector256:
        {
            // These HWIntrinsic operations are contained as part of a store
            return true;
        }

        case NI_Vector128_CreateScalarUnsafe:
        case NI_Vector256_CreateScalarUnsafe:
        case NI_Vector512_CreateScalarUnsafe:
        {
            // These HWIntrinsic operations are contained as part of scalar ops
            return true;
        }

        case NI_Vector128_get_Zero:
        case NI_Vector256_get_Zero:
        {
            // These HWIntrinsic operations are contained as part of Sse41.Insert
            return true;
        }

        case NI_SSE3_MoveAndDuplicate:
        case NI_AVX_BroadcastScalarToVector128:
        case NI_AVX2_BroadcastScalarToVector128:
        case NI_AVX_BroadcastScalarToVector256:
        case NI_AVX2_BroadcastScalarToVector256:
        case NI_AVX512F_BroadcastScalarToVector512:
        {
            // These intrinsic operations are contained as part of the operand of embedded broadcast compatible
            // instruction
            return true;
        }

        default:
        {
            return false;
        }
    }
#else
    return false;
#endif // TARGET_XARCH
}

bool GenTree::isRMWHWIntrinsic(Compiler* comp)
{
    assert(gtOper == GT_HWINTRINSIC);
    assert(comp != nullptr);

#if defined(TARGET_XARCH)
    GenTreeHWIntrinsic* hwintrinsic = AsHWIntrinsic();
    NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

    if (!comp->canUseVexEncoding())
    {
        return HWIntrinsicInfo::HasRMWSemantics(intrinsicId);
    }

    if (HWIntrinsicInfo::IsRmwIntrinsic(intrinsicId))
    {
        return true;
    }

    switch (intrinsicId)
    {
        case NI_AVX512F_BlendVariableMask:
        {
            GenTree* op2 = hwintrinsic->Op(2);

            if (op2->IsEmbMaskOp())
            {
                GenTree* op1 = hwintrinsic->Op(1);

                if (op1->isContained())
                {
                    assert(op1->IsVectorZero());
                    return false;
                }
                else
                {
                    return true;
                }
            }

            return false;
        }

        case NI_AVX512F_Fixup:
        case NI_AVX512F_FixupScalar:
        case NI_AVX512F_VL_Fixup:
        {
            // We are actually only RMW in the case where the lookup table
            // has any value that could result in `op1` being picked. So
            // in the case `op3` is a constant and none of the nibbles are
            // `0`, then we don't have to be RMW and can actually "drop" `op1`

            GenTree* op4 = hwintrinsic->Op(4);

            if (!op4->IsIntegralConst())
            {
                return true;
            }

            GenTree* op3 = hwintrinsic->Op(3);

            if (!op3->IsCnsVec())
            {
                return true;
            }

            GenTreeVecCon* vecCon = op3->AsVecCon();

            var_types simdBaseType = hwintrinsic->GetSimdBaseType();
            unsigned  simdSize     = hwintrinsic->GetSimdSize();
            uint32_t  count        = simdSize / sizeof(uint32_t);
            uint32_t  incSize      = (simdBaseType == TYP_FLOAT) ? 1 : 2;

            if (intrinsicId == NI_AVX512F_FixupScalar)
            {
                // Upper elements come from op2
                count = 1;
            }

            for (uint32_t i = 0; i < count; i += incSize)
            {
                uint32_t tbl = vecCon->gtSimdVal.u32[i];

                if (((tbl & 0x0000000F) == 0) || ((tbl & 0x000000F0) == 0) || ((tbl & 0x00000F00) == 0) ||
                    ((tbl & 0x0000F000) == 0) || ((tbl & 0x000F0000) == 0) || ((tbl & 0x00F00000) == 0) ||
                    ((tbl & 0x0F000000) == 0) || ((tbl & 0xF0000000) == 0))
                {
                    return true;
                }
            }

            return false;
        }

        case NI_AVX512F_TernaryLogic:
        case NI_AVX512F_VL_TernaryLogic:
        {
            // We may not be RMW depending on the control byte as there
            // are many operations that do not use all three inputs.

            GenTree* op4 = hwintrinsic->Op(4);

            if (!op4->IsIntegralConst())
            {
                return true;
            }

            uint8_t                 control  = static_cast<uint8_t>(op4->AsIntCon()->gtIconVal);
            const TernaryLogicInfo& info     = TernaryLogicInfo::lookup(control);
            TernaryLogicUseFlags    useFlags = info.GetAllUseFlags();

            return useFlags == TernaryLogicUseFlags::ABC;
        }

        default:
        {
            return false;
        }
    }
#elif defined(TARGET_ARM64)
    return HWIntrinsicInfo::HasRMWSemantics(AsHWIntrinsic()->GetHWIntrinsicId());
#else
    return false;
#endif
}

//------------------------------------------------------------------------
// isEvexCompatibleHWIntrinsic: Checks if the intrinsic has a compatible
// EVEX form for its intended lowering instruction.
//
// Return Value:
// true if the intrisic node lowering instruction has an EVEX form
//
bool GenTree::isEvexCompatibleHWIntrinsic() const
{
    // TODO-XARCH-AVX512 remove the ReturnsPerElementMask check once K registers have been properly
    // implemented in the register allocator
    return OperIsHWIntrinsic() && HWIntrinsicInfo::HasEvexSemantics(AsHWIntrinsic()->GetHWIntrinsicId()) &&
           !HWIntrinsicInfo::ReturnsPerElementMask(AsHWIntrinsic()->GetHWIntrinsicId());
}

//------------------------------------------------------------------------
// isEvexEmbeddedMaskingCompatibleHWIntrinsic: Checks if the intrinsic is compatible
// with the EVEX embedded masking form for its intended lowering instruction.
//
// Return Value:
// true if the intrisic node lowering instruction has an EVEX embedded masking
//
bool GenTree::isEvexEmbeddedMaskingCompatibleHWIntrinsic() const
{
#if defined(TARGET_XARCH)
    if (OperIsHWIntrinsic())
    {
        // TODO-AVX512F-CQ: Expand this to the full set of APIs and make it table driven
        // using IsEmbMaskingCompatible. For now, however, limit it to some explicit ids
        // for prototyping purposes.
        return (AsHWIntrinsic()->GetHWIntrinsicId() == NI_AVX512F_Add);
    }
#endif // TARGET_XARCH

    return false;
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types      type,
                                                       NamedIntrinsic hwIntrinsicID,
                                                       CorInfoType    simdBaseJitType,
                                                       unsigned       simdSize)
{
    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, simdBaseJitType, simdSize);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(
    var_types type, GenTree* op1, NamedIntrinsic hwIntrinsicID, CorInfoType simdBaseJitType, unsigned simdSize)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, simdBaseJitType, simdSize, op1);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types      type,
                                                       GenTree*       op1,
                                                       GenTree*       op2,
                                                       NamedIntrinsic hwIntrinsicID,
                                                       CorInfoType    simdBaseJitType,
                                                       unsigned       simdSize)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);
    SetOpLclRelatedToSIMDIntrinsic(op2);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, simdBaseJitType, simdSize, op1, op2);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types      type,
                                                       GenTree*       op1,
                                                       GenTree*       op2,
                                                       GenTree*       op3,
                                                       NamedIntrinsic hwIntrinsicID,
                                                       CorInfoType    simdBaseJitType,
                                                       unsigned       simdSize)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);
    SetOpLclRelatedToSIMDIntrinsic(op2);
    SetOpLclRelatedToSIMDIntrinsic(op3);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, simdBaseJitType, simdSize, op1, op2, op3);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types      type,
                                                       GenTree*       op1,
                                                       GenTree*       op2,
                                                       GenTree*       op3,
                                                       GenTree*       op4,
                                                       NamedIntrinsic hwIntrinsicID,
                                                       CorInfoType    simdBaseJitType,
                                                       unsigned       simdSize)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);
    SetOpLclRelatedToSIMDIntrinsic(op2);
    SetOpLclRelatedToSIMDIntrinsic(op3);
    SetOpLclRelatedToSIMDIntrinsic(op4);

    return new (this, GT_HWINTRINSIC) GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID,
                                                         simdBaseJitType, simdSize, op1, op2, op3, op4);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types      type,
                                                       GenTree**      operands,
                                                       size_t         operandCount,
                                                       NamedIntrinsic hwIntrinsicID,
                                                       CorInfoType    simdBaseJitType,
                                                       unsigned       simdSize)
{
    IntrinsicNodeBuilder nodeBuilder(getAllocator(CMK_ASTNode), operandCount);
    for (size_t i = 0; i < operandCount; i++)
    {
        nodeBuilder.AddOperand(i, operands[i]);
        SetOpLclRelatedToSIMDIntrinsic(operands[i]);
    }

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, std::move(nodeBuilder), hwIntrinsicID, simdBaseJitType, simdSize);
}

GenTreeHWIntrinsic* Compiler::gtNewSimdHWIntrinsicNode(var_types              type,
                                                       IntrinsicNodeBuilder&& nodeBuilder,
                                                       NamedIntrinsic         hwIntrinsicID,
                                                       CorInfoType            simdBaseJitType,
                                                       unsigned               simdSize)
{
    for (size_t i = 0; i < nodeBuilder.GetOperandCount(); i++)
    {
        SetOpLclRelatedToSIMDIntrinsic(nodeBuilder.GetOperand(i));
    }

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, std::move(nodeBuilder), hwIntrinsicID, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdAbsNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeGet() == type);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    if (varTypeIsUnsigned(simdBaseType))
    {
        return op1;
    }

#if defined(TARGET_XARCH)
    if (varTypeIsFloating(simdBaseType))
    {
        // Abs(v) = v & ~new vector<T>(-0.0);
        assert((simdSize != 32) || compIsaSupportedDebugOnly(InstructionSet_AVX));

        GenTree* bitMask;

        bitMask = gtNewDconNode(-0.0, simdBaseType);
        bitMask = gtNewSimdCreateBroadcastNode(type, bitMask, simdBaseJitType, simdSize);

        return gtNewSimdBinOpNode(GT_AND_NOT, type, op1, bitMask, simdBaseJitType, simdSize);
    }

    NamedIntrinsic intrinsic = NI_Illegal;

    if (simdBaseType == TYP_LONG)
    {
        if (simdSize == 64)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            intrinsic = NI_AVX512F_Abs;
        }
        else if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
        {
            intrinsic = NI_AVX512F_VL_Abs;
        }
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
        intrinsic = NI_AVX2_Abs;
    }
    else if (simdSize == 64)
    {
        if (simdBaseType == TYP_INT)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            intrinsic = NI_AVX512F_Abs;
        }
        else
        {
            assert(varTypeIsSmall(simdBaseType));
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
            intrinsic = NI_AVX512BW_Abs;
        }
    }
    else if (compOpportunisticallyDependsOn(InstructionSet_SSSE3))
    {
        intrinsic = NI_SSSE3_Abs;
    }

    if (intrinsic != NI_Illegal)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
    }
    else
    {
        GenTree* tmp;
        GenTree* op1Dup1 = fgMakeMultiUse(&op1);
        GenTree* op1Dup2 = fgMakeMultiUse(&op1Dup1);

        // op1 = op1 < Zero
        tmp = gtNewZeroConNode(type);
        op1 = gtNewSimdCmpOpNode(GT_LT, type, op1, tmp, simdBaseJitType, simdSize);

        // tmp = Zero - op1Dup1
        tmp = gtNewZeroConNode(type);
        tmp = gtNewSimdBinOpNode(GT_SUB, type, tmp, op1Dup1, simdBaseJitType, simdSize);

        // result = ConditionalSelect(op1, tmp, op1Dup2)
        return gtNewSimdCndSelNode(type, op1, tmp, op1Dup2, simdBaseJitType, simdSize);
    }
#elif defined(TARGET_ARM64)
    NamedIntrinsic intrinsic = NI_AdvSimd_Abs;

    if (simdBaseType == TYP_DOUBLE)
    {
        intrinsic = (simdSize == 8) ? NI_AdvSimd_AbsScalar : NI_AdvSimd_Arm64_Abs;
    }
    else if (varTypeIsLong(simdBaseType))
    {
        intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_AbsScalar : NI_AdvSimd_Arm64_Abs;
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif
}

GenTree* Compiler::gtNewSimdBinOpNode(
    genTreeOps op, var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    assert(op1 != nullptr);
    assert(op1->TypeIs(type, simdBaseType, genActualType(simdBaseType)) ||
           (op1->TypeIs(TYP_SIMD12) && type == TYP_SIMD16));

    assert(op2 != nullptr);

    if ((op == GT_LSH) || (op == GT_RSH) || (op == GT_RSZ))
    {
        assert(genActualType(op2) == TYP_INT);
    }
    else
    {
        assert((genActualType(op2) == genActualType(type)) || (genActualType(op2) == genActualType(simdBaseType)) ||
               (op2->TypeIs(TYP_SIMD12) && (type == TYP_SIMD16)));
    }

    NamedIntrinsic intrinsic = NI_Illegal;

    switch (op)
    {
#if defined(TARGET_XARCH)
        case GT_ADD:
        {
            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_Add;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                    intrinsic = NI_AVX2_Add;
                }
            }
            else if (simdSize == 64)
            {
                if (varTypeIsSmall(simdBaseType))
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
                    intrinsic = NI_AVX512BW_Add;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    intrinsic = NI_AVX512F_Add;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_Add;
            }
            else
            {
                intrinsic = NI_SSE2_Add;
            }
            break;
        }

        case GT_AND:
        {
            if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                intrinsic = NI_AVX512F_And;

                if (varTypeIsIntegral(simdBaseType))
                {
                    intrinsic = NI_AVX512F_And;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
                {
                    intrinsic = NI_AVX512DQ_And;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX512F_And;
                    simdBaseJitType = (simdBaseType == TYP_DOUBLE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_INT;
                }
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_And;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    intrinsic = NI_AVX2_And;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX_And;
                    simdBaseJitType = varTypeIsLong(simdBaseType) ? CORINFO_TYPE_DOUBLE : CORINFO_TYPE_FLOAT;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_And;
            }
            else
            {
                intrinsic = NI_SSE2_And;
            }
            break;
        }

        case GT_AND_NOT:
        {
            if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                intrinsic = NI_AVX512F_AndNot;

                if (varTypeIsIntegral(simdBaseType))
                {
                    intrinsic = NI_AVX512F_AndNot;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
                {
                    intrinsic = NI_AVX512DQ_AndNot;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX512F_AndNot;
                    simdBaseJitType = (simdBaseType == TYP_DOUBLE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_INT;
                }
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_AndNot;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    intrinsic = NI_AVX2_AndNot;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX_AndNot;
                    simdBaseJitType = varTypeIsLong(simdBaseType) ? CORINFO_TYPE_DOUBLE : CORINFO_TYPE_FLOAT;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_AndNot;
            }
            else
            {
                intrinsic = NI_SSE2_AndNot;
            }

            // GT_AND_NOT expects `op1 & ~op2`, but xarch does `~op1 & op2`
            // We expect op1 to have already been spilled

            std::swap(op1, op2);
            break;
        }

        case GT_DIV:
        {
            // TODO-XARCH-CQ: We could support division by constant for integral types
            assert(varTypeIsFloating(simdBaseType));

            if (varTypeIsArithmetic(op2))
            {
                op2 = gtNewSimdCreateBroadcastNode(type, op2, simdBaseJitType, simdSize);
            }

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                intrinsic = NI_AVX_Divide;
            }
            else if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                intrinsic = NI_AVX512F_Divide;
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_Divide;
            }
            else
            {
                intrinsic = NI_SSE2_Divide;
            }
            break;
        }

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        {
            // float and double don't have actual instructions for shifting
            // so we'll just use the equivalent integer instruction instead.

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseJitType = CORINFO_TYPE_INT;
                simdBaseType    = TYP_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseJitType = CORINFO_TYPE_LONG;
                simdBaseType    = TYP_LONG;
            }

            assert(!varTypeIsByte(simdBaseType) || op == GT_RSZ);

            // "over shifting" is platform specific behavior. We will match the C# behavior
            // this requires we mask with (sizeof(T) * 8) - 1 which ensures the shift cannot
            // exceed the number of bits available in `T`. This is roughly equivalent to
            // x % (sizeof(T) * 8), but that is "more expensive" and only the same for unsigned
            // inputs, where-as we have a signed-input and so negative values would differ.

            unsigned shiftCountMask = (genTypeSize(simdBaseType) * 8) - 1;

            GenTree* nonConstantByteShiftCountOp = NULL;

            if (op2->IsCnsIntOrI())
            {
                op2->AsIntCon()->gtIconVal &= shiftCountMask;
            }
            else
            {
                op2 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(shiftCountMask));

                if (varTypeIsByte(simdBaseType))
                {
                    // Save the intermediate "shiftCount & shiftCountMask" value as we will need it to
                    // calculate which bits we should mask off after the 32-bit shift we use for 8-bit values.
                    nonConstantByteShiftCountOp = fgMakeMultiUse(&op2);
                }

                op2 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_SSE2_ConvertScalarToVector128Int32, CORINFO_TYPE_INT,
                                               16);
            }

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

                if (op == GT_LSH)
                {
                    intrinsic = NI_AVX2_ShiftLeftLogical;
                }
                else if (op == GT_RSH)
                {
                    if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
                    {
                        assert(varTypeIsSigned(simdBaseType));
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));
                        intrinsic = NI_AVX512F_VL_ShiftRightArithmetic;
                    }
                    else
                    {
                        intrinsic = NI_AVX2_ShiftRightArithmetic;
                    }
                }
                else
                {
                    assert(op == GT_RSZ);
                    intrinsic = NI_AVX2_ShiftRightLogical;
                }
            }
            else if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());

                if (op == GT_LSH)
                {
                    if (varTypeIsShort(simdBaseType))
                    {
                        intrinsic = NI_AVX512BW_ShiftLeftLogical;
                    }
                    else
                    {
                        intrinsic = NI_AVX512F_ShiftLeftLogical;
                    }
                }
                else if (op == GT_RSH)
                {
                    if (varTypeIsShort(simdBaseType))
                    {
                        intrinsic = NI_AVX512BW_ShiftRightArithmetic;
                    }
                    else
                    {
                        intrinsic = NI_AVX512F_ShiftRightArithmetic;
                    }
                }
                else
                {
                    assert(op == GT_RSZ);
                    if (varTypeIsShort(simdBaseType))
                    {
                        intrinsic = NI_AVX512BW_ShiftRightLogical;
                    }
                    else
                    {
                        intrinsic = NI_AVX512F_ShiftRightLogical;
                    }
                }
            }
            else if (op == GT_LSH)
            {
                intrinsic = NI_SSE2_ShiftLeftLogical;
            }
            else if (op == GT_RSH)
            {
                if (varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE))
                {
                    assert(varTypeIsSigned(simdBaseType));
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));
                    intrinsic = NI_AVX512F_VL_ShiftRightArithmetic;
                }
                else
                {
                    intrinsic = NI_SSE2_ShiftRightArithmetic;
                }
            }
            else
            {
                assert(op == GT_RSZ);
                intrinsic = NI_SSE2_ShiftRightLogical;
            }

            if (varTypeIsByte(simdBaseType))
            {
                assert(op == GT_RSZ);

                // We don't have actual instructions for shifting bytes, so we'll emulate them
                // by shifting 32-bit values and masking off the bits that should be zeroed.
                GenTree* maskAmountOp;

                if (op2->IsCnsIntOrI())
                {
                    ssize_t shiftCount = op2->AsIntCon()->gtIconVal;
                    ssize_t mask       = 255 >> shiftCount;

                    maskAmountOp = gtNewIconNode(mask, type);
                }
                else
                {
                    assert(nonConstantByteShiftCountOp != NULL);

                    maskAmountOp = gtNewOperNode(GT_RSZ, TYP_INT, gtNewIconNode(255), nonConstantByteShiftCountOp);
                }

                GenTree* shiftOp = gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, CORINFO_TYPE_INT, simdSize);
                GenTree* maskOp  = gtNewSimdCreateBroadcastNode(type, maskAmountOp, simdBaseJitType, simdSize);

                return gtNewSimdBinOpNode(GT_AND, type, shiftOp, maskOp, simdBaseJitType, simdSize);
            }
            break;
        }

        case GT_MUL:
        {
            GenTree** broadcastOp = nullptr;

            if (varTypeIsArithmetic(op1))
            {
                broadcastOp = &op1;
            }
            else if (varTypeIsArithmetic(op2))
            {
                broadcastOp = &op2;
            }

            if (broadcastOp != nullptr)
            {
                *broadcastOp = gtNewSimdCreateBroadcastNode(type, *broadcastOp, simdBaseJitType, simdSize);
            }

            switch (simdBaseType)
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                {
                    assert((simdSize != 64) || IsBaselineVector512IsaSupportedDebugOnly());

                    CorInfoType    widenedSimdBaseJitType;
                    NamedIntrinsic widenIntrinsic;
                    NamedIntrinsic narrowIntrinsic;
                    var_types      widenedType;
                    unsigned       widenedSimdSize;

                    if (simdSize == 32 && IsBaselineVector512IsaSupportedOpportunistically())
                    {
                        // Input is SIMD32 [U]Byte and AVX512BW is supported:
                        // - Widen inputs as SIMD64 [U]Short
                        // - Multiply widened inputs (SIMD64 [U]Short) as widened product (SIMD64 [U]Short)
                        // - Narrow widened product (SIMD64 [U]Short) as SIMD32 [U]Byte
                        if (simdBaseType == TYP_BYTE)
                        {
                            widenedSimdBaseJitType = CORINFO_TYPE_SHORT;
                            widenIntrinsic         = NI_AVX512BW_ConvertToVector512Int16;
                            narrowIntrinsic        = NI_AVX512BW_ConvertToVector256SByte;
                        }
                        else
                        {
                            widenedSimdBaseJitType = CORINFO_TYPE_USHORT;
                            widenIntrinsic         = NI_AVX512BW_ConvertToVector512UInt16;
                            narrowIntrinsic        = NI_AVX512BW_ConvertToVector256Byte;
                        }

                        widenedType     = TYP_SIMD64;
                        widenedSimdSize = 64;

                        // Vector512<ushort> widenedOp1 = Avx512BW.ConvertToVector512UInt16(op1)
                        GenTree* widenedOp1 = gtNewSimdHWIntrinsicNode(widenedType, op1, widenIntrinsic,
                                                                       simdBaseJitType, widenedSimdSize);

                        // Vector512<ushort> widenedOp2 = Avx512BW.ConvertToVector512UInt16(op2)
                        GenTree* widenedOp2 = gtNewSimdHWIntrinsicNode(widenedType, op2, widenIntrinsic,
                                                                       simdBaseJitType, widenedSimdSize);

                        // Vector512<ushort> widenedProduct = widenedOp1 * widenedOp2;
                        GenTree* widenedProduct = gtNewSimdBinOpNode(GT_MUL, widenedType, widenedOp1, widenedOp2,
                                                                     widenedSimdBaseJitType, widenedSimdSize);

                        // Vector256<byte> product = Avx512BW.ConvertToVector256Byte(widenedProduct)
                        return gtNewSimdHWIntrinsicNode(type, widenedProduct, narrowIntrinsic, widenedSimdBaseJitType,
                                                        widenedSimdSize);
                    }
                    else if (simdSize == 16 && compOpportunisticallyDependsOn(InstructionSet_AVX2))
                    {
                        if (IsBaselineVector512IsaSupportedOpportunistically())
                        {
                            // Input is SIMD16 [U]Byte and AVX512BW_VL is supported:
                            // - Widen inputs as SIMD32 [U]Short
                            // - Multiply widened inputs (SIMD32 [U]Short) as widened product (SIMD32 [U]Short)
                            // - Narrow widened product (SIMD32 [U]Short) as SIMD16 [U]Byte
                            widenIntrinsic = NI_AVX2_ConvertToVector256Int16;

                            if (simdBaseType == TYP_BYTE)
                            {
                                widenedSimdBaseJitType = CORINFO_TYPE_SHORT;
                                narrowIntrinsic        = NI_AVX512BW_VL_ConvertToVector128SByte;
                            }
                            else
                            {
                                widenedSimdBaseJitType = CORINFO_TYPE_USHORT;
                                narrowIntrinsic        = NI_AVX512BW_VL_ConvertToVector128Byte;
                            }

                            widenedType     = TYP_SIMD32;
                            widenedSimdSize = 32;

                            // Vector256<ushort> widenedOp1 = Avx2.ConvertToVector256Int16(op1).AsUInt16()
                            GenTree* widenedOp1 = gtNewSimdHWIntrinsicNode(widenedType, op1, widenIntrinsic,
                                                                           simdBaseJitType, widenedSimdSize);

                            // Vector256<ushort> widenedOp2 = Avx2.ConvertToVector256Int16(op2).AsUInt16()
                            GenTree* widenedOp2 = gtNewSimdHWIntrinsicNode(widenedType, op2, widenIntrinsic,
                                                                           simdBaseJitType, widenedSimdSize);

                            // Vector256<ushort> widenedProduct = widenedOp1 * widenedOp2
                            GenTree* widenedProduct = gtNewSimdBinOpNode(GT_MUL, widenedType, widenedOp1, widenedOp2,
                                                                         widenedSimdBaseJitType, widenedSimdSize);

                            // Vector128<byte> product = Avx512BW.VL.ConvertToVector128Byte(widenedProduct)
                            return gtNewSimdHWIntrinsicNode(type, widenedProduct, narrowIntrinsic,
                                                            widenedSimdBaseJitType, widenedSimdSize);
                        }
                        else
                        {
                            // Input is SIMD16 [U]Byte and AVX512BW_VL is NOT supported (only AVX2 will be used):
                            // - Widen inputs as SIMD32 [U]Short
                            // - Multiply widened inputs (SIMD32 [U]Short) as widened product (SIMD32 [U]Short)
                            // - Mask widened product (SIMD32 [U]Short) to select relevant bits
                            // - Pack masked product so that relevant bits are packed together in upper and lower halves
                            // - Shuffle packed product so that relevant bits are placed together in the lower half
                            // - Select lower (SIMD16 [U]Byte) from shuffled product (SIMD32 [U]Short)
                            widenedSimdBaseJitType =
                                simdBaseType == TYP_BYTE ? CORINFO_TYPE_SHORT : CORINFO_TYPE_USHORT;
                            widenIntrinsic  = NI_AVX2_ConvertToVector256Int16;
                            widenedType     = TYP_SIMD32;
                            widenedSimdSize = 32;

                            // Vector256<ushort> widenedOp1 = Avx2.ConvertToVector256Int16(op1).AsUInt16()
                            GenTree* widenedOp1 =
                                gtNewSimdHWIntrinsicNode(widenedType, op1, widenIntrinsic, simdBaseJitType, simdSize);

                            // Vector256<ushort> widenedOp2 = Avx2.ConvertToVector256Int16(op2).AsUInt16()
                            GenTree* widenedOp2 =
                                gtNewSimdHWIntrinsicNode(widenedType, op2, widenIntrinsic, simdBaseJitType, simdSize);

                            // Vector256<ushort> widenedProduct = widenedOp1 * widenedOp2
                            GenTree* widenedProduct = gtNewSimdBinOpNode(GT_MUL, widenedType, widenedOp1, widenedOp2,
                                                                         widenedSimdBaseJitType, widenedSimdSize);

                            // Vector256<ushort> vecCon1 = Vector256.Create(0x00FF00FF00FF00FF).AsUInt16()
                            GenTreeVecCon* vecCon1 = gtNewVconNode(widenedType);

                            for (unsigned i = 0; i < (widenedSimdSize / 8); i++)
                            {
                                vecCon1->gtSimdVal.u64[i] = 0x00FF00FF00FF00FF;
                            }

                            // Validate we can't use AVX512F_VL_TernaryLogic here
                            assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                            // Vector256<short> maskedProduct = Avx2.And(widenedProduct, vecCon1).AsInt16()
                            GenTree* maskedProduct = gtNewSimdBinOpNode(GT_AND, widenedType, widenedProduct, vecCon1,
                                                                        widenedSimdBaseJitType, widenedSimdSize);
                            GenTree* maskedProductDup = fgMakeMultiUse(&maskedProduct);

                            // Vector256<ulong> packedProduct = Avx2.PackUnsignedSaturate(maskedProduct,
                            //                                                            maskedProduct).AsUInt64()
                            GenTree* packedProduct =
                                gtNewSimdHWIntrinsicNode(widenedType, maskedProduct, maskedProductDup,
                                                         NI_AVX2_PackUnsignedSaturate, CORINFO_TYPE_UBYTE,
                                                         widenedSimdSize);

                            CorInfoType permuteBaseJitType =
                                (simdBaseType == TYP_BYTE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_ULONG;

                            // Vector256<byte> shuffledProduct = Avx2.Permute4x64(w1, 0xD8).AsByte()
                            GenTree* shuffledProduct =
                                gtNewSimdHWIntrinsicNode(widenedType, packedProduct, gtNewIconNode(SHUFFLE_WYZX),
                                                         NI_AVX2_Permute4x64, permuteBaseJitType, widenedSimdSize);

                            // Vector128<byte> product = shuffledProduct.getLower()
                            return gtNewSimdGetLowerNode(type, shuffledProduct, simdBaseJitType, widenedSimdSize);
                        }
                    }

                    // No special handling could be performed, apply fallback logic:
                    // - Widen both inputs lower and upper halves as [U]Short (using helper method)
                    // - Multiply corrsponding widened input halves together as widened product halves
                    // - Narrow widened product halves as [U]Byte (using helper method)
                    widenedSimdBaseJitType = simdBaseType == TYP_BYTE ? CORINFO_TYPE_SHORT : CORINFO_TYPE_USHORT;

                    // op1Dup = op1
                    GenTree* op1Dup = fgMakeMultiUse(&op1);

                    // op2Dup = op2
                    GenTree* op2Dup = fgMakeMultiUse(&op2);

                    // Vector256<ushort> lowerOp1 = Avx2.ConvertToVector256Int16(op1.GetLower()).AsUInt16()
                    GenTree* lowerOp1 = gtNewSimdWidenLowerNode(type, op1, simdBaseJitType, simdSize);

                    // Vector256<ushort> lowerOp2 = Avx2.ConvertToVector256Int16(op2.GetLower()).AsUInt16()
                    GenTree* lowerOp2 = gtNewSimdWidenLowerNode(type, op2, simdBaseJitType, simdSize);

                    // Vector256<ushort> lowerProduct = lowerOp1 * lowerOp2
                    GenTree* lowerProduct =
                        gtNewSimdBinOpNode(GT_MUL, type, lowerOp1, lowerOp2, widenedSimdBaseJitType, simdSize);

                    // Vector256<ushort> upperOp1 = Avx2.ConvertToVector256Int16(op1.GetUpper()).AsUInt16()
                    GenTree* upperOp1 = gtNewSimdWidenUpperNode(type, op1Dup, simdBaseJitType, simdSize);

                    // Vector256<ushort> upperOp2 = Avx2.ConvertToVector256Int16(op2.GetUpper()).AsUInt16()
                    GenTree* upperOp2 = gtNewSimdWidenUpperNode(type, op2Dup, simdBaseJitType, simdSize);

                    // Vector256<ushort> upperProduct = upperOp1 * upperOp2
                    GenTree* upperProduct =
                        gtNewSimdBinOpNode(GT_MUL, type, upperOp1, upperOp2, widenedSimdBaseJitType, simdSize);

                    // Narrow and merge halves using helper method
                    return gtNewSimdNarrowNode(type, lowerProduct, upperProduct, simdBaseJitType, simdSize);
                }

                case TYP_SHORT:
                case TYP_USHORT:
                {
                    if (simdSize == 32)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                        intrinsic = NI_AVX2_MultiplyLow;
                    }
                    else if (simdSize == 64)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
                        intrinsic = NI_AVX512BW_MultiplyLow;
                    }
                    else
                    {
                        intrinsic = NI_SSE2_MultiplyLow;
                    }
                    break;
                }

                case TYP_INT:
                case TYP_UINT:
                {
                    if (simdSize == 32)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                        intrinsic = NI_AVX2_MultiplyLow;
                    }
                    else if (simdSize == 64)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                        intrinsic = NI_AVX512F_MultiplyLow;
                    }
                    else if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                    {
                        intrinsic = NI_SSE41_MultiplyLow;
                    }
                    else
                    {
                        // op1Dup = op1
                        GenTree* op1Dup = fgMakeMultiUse(&op1);

                        // op2Dup = op2
                        GenTree* op2Dup = fgMakeMultiUse(&op2);

                        // op1Dup = Sse2.ShiftRightLogical128BitLane(op1Dup, 4)
                        op1Dup =
                            gtNewSimdHWIntrinsicNode(type, op1Dup, gtNewIconNode(4, TYP_INT),
                                                     NI_SSE2_ShiftRightLogical128BitLane, simdBaseJitType, simdSize);

                        // op2Dup = Sse2.ShiftRightLogical128BitLane(op2Dup, 4)
                        op2Dup =
                            gtNewSimdHWIntrinsicNode(type, op2Dup, gtNewIconNode(4, TYP_INT),
                                                     NI_SSE2_ShiftRightLogical128BitLane, simdBaseJitType, simdSize);

                        // op2Dup = Sse2.Multiply(op1Dup.AsUInt32(), op2Dup.AsUInt32()).AsInt32()
                        op2Dup = gtNewSimdHWIntrinsicNode(type, op1Dup, op2Dup, NI_SSE2_Multiply, CORINFO_TYPE_ULONG,
                                                          simdSize);

                        // op2Dup = Sse2.Shuffle(op2Dup, (0, 0, 2, 0))
                        op2Dup = gtNewSimdHWIntrinsicNode(type, op2Dup, gtNewIconNode(SHUFFLE_XXZX, TYP_INT),
                                                          NI_SSE2_Shuffle, simdBaseJitType, simdSize);

                        // op1 = Sse2.Multiply(op1.AsUInt32(), op2.AsUInt32()).AsInt32()
                        op1 = gtNewSimdHWIntrinsicNode(type, op1, op2, NI_SSE2_Multiply, CORINFO_TYPE_ULONG, simdSize);

                        // op1 = Sse2.Shuffle(op1, (0, 0, 2, 0))
                        op1 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode(SHUFFLE_XXZX, TYP_INT), NI_SSE2_Shuffle,
                                                       simdBaseJitType, simdSize);

                        // op2 = op2Dup;
                        op2 = op2Dup;

                        // result = Sse2.UnpackLow(op1, op2)
                        intrinsic = NI_SSE2_UnpackLow;
                    }
                    break;
                }

                case TYP_LONG:
                case TYP_ULONG:
                {
                    assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512DQ_VL));

                    if (simdSize != 64)
                    {
                        intrinsic = NI_AVX512DQ_VL_MultiplyLow;
                    }
                    else
                    {
                        intrinsic = NI_AVX512DQ_MultiplyLow;
                    }

                    break;
                }

                case TYP_FLOAT:
                {
                    if (simdSize == 32)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                        intrinsic = NI_AVX_Multiply;
                    }
                    else if (simdSize == 64)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                        intrinsic = NI_AVX512F_Multiply;
                    }
                    else
                    {
                        intrinsic = NI_SSE_Multiply;
                    }
                    break;
                }

                case TYP_DOUBLE:
                {
                    if (simdSize == 32)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                        intrinsic = NI_AVX_Multiply;
                    }
                    else if (simdSize == 64)
                    {
                        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                        intrinsic = NI_AVX512F_Multiply;
                    }
                    else
                    {
                        intrinsic = NI_SSE2_Multiply;
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }

        case GT_OR:
        {
            if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                intrinsic = NI_AVX512F_Or;

                if (varTypeIsIntegral(simdBaseType))
                {
                    intrinsic = NI_AVX512F_Or;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
                {
                    intrinsic = NI_AVX512DQ_Or;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX512F_Or;
                    simdBaseJitType = (simdBaseType == TYP_DOUBLE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_INT;
                }
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_Or;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    intrinsic = NI_AVX2_Or;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX_Or;
                    simdBaseJitType = varTypeIsLong(simdBaseType) ? CORINFO_TYPE_DOUBLE : CORINFO_TYPE_FLOAT;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_Or;
            }
            else
            {
                intrinsic = NI_SSE2_Or;
            }
            break;
        }

        case GT_SUB:
        {
            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_Subtract;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                    intrinsic = NI_AVX2_Subtract;
                }
            }
            else if (simdSize == 64)
            {
                if (varTypeIsSmall(simdBaseType))
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
                    intrinsic = NI_AVX512BW_Subtract;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                    intrinsic = NI_AVX512F_Subtract;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_Subtract;
            }
            else
            {
                intrinsic = NI_SSE2_Subtract;
            }
            break;
        }

        case GT_XOR:
        {
            if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
                intrinsic = NI_AVX512F_Xor;

                if (varTypeIsIntegral(simdBaseType))
                {
                    intrinsic = NI_AVX512F_Xor;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX512DQ))
                {
                    intrinsic = NI_AVX512DQ_Xor;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX512F_Xor;
                    simdBaseJitType = (simdBaseType == TYP_DOUBLE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_INT;
                }
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_Xor;
                }
                else if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
                {
                    intrinsic = NI_AVX2_Xor;
                }
                else
                {
                    // Since this is a bitwise operation, we can still support it by lying
                    // about the type and doing the operation using a supported instruction

                    intrinsic       = NI_AVX_Xor;
                    simdBaseJitType = varTypeIsLong(simdBaseType) ? CORINFO_TYPE_DOUBLE : CORINFO_TYPE_FLOAT;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                intrinsic = NI_SSE_Xor;
            }
            else
            {
                intrinsic = NI_SSE2_Xor;
            }
            break;
        }
#elif defined(TARGET_ARM64)
        case GT_ADD:
        {
            if (simdBaseType == TYP_DOUBLE)
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_AddScalar : NI_AdvSimd_Arm64_Add;
            }
            else if ((simdSize == 8) && varTypeIsLong(simdBaseType))
            {
                intrinsic = NI_AdvSimd_AddScalar;
            }
            else
            {
                intrinsic = NI_AdvSimd_Add;
            }
            break;
        }

        case GT_AND:
        {
            intrinsic = NI_AdvSimd_And;
            break;
        }

        case GT_AND_NOT:
        {
            intrinsic = NI_AdvSimd_BitwiseClear;
            break;
        }

        case GT_DIV:
        {
            // TODO-AARCH-CQ: We could support division by constant for integral types
            assert(varTypeIsFloating(simdBaseType));

            if (varTypeIsArithmetic(op2))
            {
                op2 = gtNewSimdCreateBroadcastNode(type, op2, simdBaseJitType, simdSize);
            }

            if ((simdSize == 8) && (simdBaseType == TYP_DOUBLE))
            {
                intrinsic = NI_AdvSimd_DivideScalar;
            }
            else
            {
                intrinsic = NI_AdvSimd_Arm64_Divide;
            }
            break;
        }

        case GT_LSH:
        case GT_RSH:
        case GT_RSZ:
        {
            assert((op != GT_RSH) || !varTypeIsUnsigned(simdBaseType));

            // float and double don't have actual instructions for shifting
            // so we'll just use the equivalent integer instruction instead.

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseJitType = CORINFO_TYPE_INT;
                simdBaseType    = TYP_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseJitType = CORINFO_TYPE_LONG;
                simdBaseType    = TYP_LONG;
            }

            // "over shifting" is platform specific behavior. We will match the C# behavior
            // this requires we mask with (sizeof(T) * 8) - 1 which ensures the shift cannot
            // exceed the number of bits available in `T`. This is roughly equivalent to
            // x % (sizeof(T) * 8), but that is "more expensive" and only the same for unsigned
            // inputs, where-as we have a signed-input and so negative values would differ.

            unsigned shiftCountMask = (genTypeSize(simdBaseType) * 8) - 1;

            if (op2->IsCnsIntOrI())
            {
                op2->AsIntCon()->gtIconVal &= shiftCountMask;

                if ((simdSize == 8) && varTypeIsLong(simdBaseType))
                {
                    if (op == GT_LSH)
                    {
                        intrinsic = NI_AdvSimd_ShiftLeftLogicalScalar;
                    }
                    else if (op == GT_RSH)
                    {
                        intrinsic = NI_AdvSimd_ShiftRightArithmeticScalar;
                    }
                    else
                    {
                        assert(op == GT_RSZ);
                        intrinsic = NI_AdvSimd_ShiftRightLogicalScalar;
                    }
                }
                else if (op == GT_LSH)
                {
                    intrinsic = NI_AdvSimd_ShiftLeftLogical;
                }
                else if (op == GT_RSH)
                {
                    intrinsic = NI_AdvSimd_ShiftRightArithmetic;
                }
                else
                {
                    assert(op == GT_RSZ);
                    intrinsic = NI_AdvSimd_ShiftRightLogical;
                }
            }
            else
            {
                op2 = gtNewOperNode(GT_AND, TYP_INT, op2, gtNewIconNode(shiftCountMask));

                if (op != GT_LSH)
                {
                    op2 = gtNewOperNode(GT_NEG, TYP_INT, op2);
                }

                op2 = gtNewSimdCreateBroadcastNode(type, op2, simdBaseJitType, simdSize);

                if ((simdSize == 8) && varTypeIsLong(simdBaseType))
                {
                    if (op == GT_LSH)
                    {
                        intrinsic = NI_AdvSimd_ShiftLogicalScalar;
                    }
                    else if (op == GT_RSH)
                    {
                        intrinsic = NI_AdvSimd_ShiftArithmeticScalar;
                    }
                    else
                    {
                        intrinsic = NI_AdvSimd_ShiftLogicalScalar;
                    }
                }
                else if (op == GT_LSH)
                {
                    intrinsic = NI_AdvSimd_ShiftLogical;
                }
                else if (op == GT_RSH)
                {
                    intrinsic = NI_AdvSimd_ShiftArithmetic;
                }
                else
                {
                    assert(op == GT_RSZ);
                    intrinsic = NI_AdvSimd_ShiftLogical;
                }
            }
            break;
        }

        case GT_MUL:
        {
            assert(!varTypeIsLong(simdBaseType));
            GenTree** scalarOp = nullptr;

            if (varTypeIsArithmetic(op1))
            {
                // MultiplyByScalar requires the scalar op to be op2
                std::swap(op1, op2);
                scalarOp = &op2;
            }
            else if (varTypeIsArithmetic(op2))
            {
                scalarOp = &op2;
            }

            switch (JitType2PreciseVarType(simdBaseJitType))
            {
                case TYP_BYTE:
                case TYP_UBYTE:
                {
                    if (scalarOp != nullptr)
                    {
                        *scalarOp = gtNewSimdCreateBroadcastNode(type, *scalarOp, simdBaseJitType, simdSize);
                    }
                    intrinsic = NI_AdvSimd_Multiply;
                    break;
                }

                case TYP_SHORT:
                case TYP_USHORT:
                case TYP_INT:
                case TYP_UINT:
                case TYP_FLOAT:
                {
                    if (scalarOp != nullptr)
                    {
                        intrinsic = NI_AdvSimd_MultiplyByScalar;
                        *scalarOp = gtNewSimdCreateScalarUnsafeNode(TYP_SIMD8, *scalarOp, simdBaseJitType, 8);
                    }
                    else
                    {
                        intrinsic = NI_AdvSimd_Multiply;
                    }
                    break;
                }

                case TYP_DOUBLE:
                {
                    if (scalarOp != nullptr)
                    {
                        intrinsic = NI_AdvSimd_Arm64_MultiplyByScalar;
                        *scalarOp = gtNewSimdCreateScalarUnsafeNode(TYP_SIMD8, *scalarOp, simdBaseJitType, 8);
                    }
                    else
                    {
                        intrinsic = NI_AdvSimd_Arm64_Multiply;
                    }

                    if (simdSize == 8)
                    {
                        intrinsic = NI_AdvSimd_MultiplyScalar;
                    }
                    break;
                }

                default:
                {
                    unreached();
                }
            }
            break;
        }

        case GT_OR:
        {
            intrinsic = NI_AdvSimd_Or;
            break;
        }

        case GT_SUB:
        {
            if (simdBaseType == TYP_DOUBLE)
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_SubtractScalar : NI_AdvSimd_Arm64_Subtract;
            }
            else if ((simdSize == 8) && varTypeIsLong(simdBaseType))
            {
                intrinsic = NI_AdvSimd_SubtractScalar;
            }
            else
            {
                intrinsic = NI_AdvSimd_Subtract;
            }
            break;
        }

        case GT_XOR:
        {
            intrinsic = NI_AdvSimd_Xor;
            break;
        }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        default:
        {
            unreached();
        }
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdCeilNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsFloating(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_AVX_Ceiling;
    }
    else if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        GenTree* op2 = gtNewIconNode(static_cast<int32_t>(FloatRoundingMode::ToPositiveInfinity));
        return gtNewSimdHWIntrinsicNode(type, op1, op2, NI_AVX512F_RoundScale, simdBaseJitType, simdSize);
    }
    else
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));
        intrinsic = NI_SSE41_Ceiling;
    }
#elif defined(TARGET_ARM64)
    if (simdBaseType == TYP_DOUBLE)
    {
        intrinsic = (simdSize == 8) ? NI_AdvSimd_CeilingScalar : NI_AdvSimd_Arm64_Ceiling;
    }
    else
    {
        intrinsic = NI_AdvSimd_Ceiling;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdCmpOpNode(
    genTreeOps op, var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic                = NI_Illegal;
    bool           needsConvertMaskToVector = false;

    switch (op)
    {
#if defined(TARGET_XARCH)
        case GT_EQ:
        {
            if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());
                intrinsic                = NI_AVX512F_CompareEqualMask;
                needsConvertMaskToVector = true;
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_CompareEqual;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                    intrinsic = NI_AVX2_CompareEqual;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));
                intrinsic = NI_SSE_CompareEqual;
            }
            else if (varTypeIsLong(simdBaseType))
            {
                assert(simdSize == 16);

                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    intrinsic = NI_SSE41_CompareEqual;
                }
                else
                {
                    // There is no direct SSE2 support for comparing TYP_LONG vectors.
                    // These have to be implemented in terms of TYP_INT vector comparison operations.
                    //
                    // tmp = (op1 == op2) i.e. compare for equality as if op1 and op2 are vector of int
                    // op1 = tmp
                    // op2 = Shuffle(op1, (2, 3, 0, 1))
                    // result = BitwiseAnd(tmp, op2)
                    //
                    // Shuffle is meant to swap the comparison results of low-32-bits and high 32-bits of
                    // respective long elements.

                    GenTree* tmp = gtNewSimdCmpOpNode(op, type, op1, op2, CORINFO_TYPE_INT, simdSize);

                    op1 = fgMakeMultiUse(&tmp);
                    op2 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode(SHUFFLE_ZWXY), NI_SSE2_Shuffle,
                                                   CORINFO_TYPE_INT, simdSize);

                    return gtNewSimdBinOpNode(GT_AND, type, tmp, op2, simdBaseJitType, simdSize);
                }
            }
            else
            {
                assert(simdSize == 16);
                intrinsic = NI_SSE2_CompareEqual;
            }
            break;
        }

        case GT_GE:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                if ((simdSize == 64) || !varTypeIsFloating(simdBaseType))
                {
                    intrinsic                = NI_AVX512F_CompareGreaterThanOrEqualMask;
                    needsConvertMaskToVector = true;
                    break;
                }
            }

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_CompareGreaterThanOrEqual;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));
                intrinsic = NI_SSE_CompareGreaterThanOrEqual;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                assert(simdSize == 16);
                intrinsic = NI_SSE2_CompareGreaterThanOrEqual;
            }

            if (intrinsic == NI_Illegal)
            {
                // If we don't have an intrinsic set for this, try "Max(op1, op2) == op1"
                // NOTE: technically, we can special case byte type to only require SSE2, but it
                // complicates the test matrix for little gains.
                if (((simdSize == 32) && compOpportunisticallyDependsOn(InstructionSet_AVX2)) ||
                    ((simdSize == 16) && compOpportunisticallyDependsOn(InstructionSet_SSE41)))
                {
                    // TODO-AVX512: We can use this trick for longs only with AVX-512
                    if (!varTypeIsLong(simdBaseType))
                    {
                        assert(!varTypeIsFloating(simdBaseType));
                        GenTree* op1Dup = fgMakeMultiUse(&op1);

                        // EQ(Max(op1, op2), op1)
                        GenTree* maxNode = gtNewSimdMaxNode(type, op1, op2, simdBaseJitType, simdSize);
                        return gtNewSimdCmpOpNode(GT_EQ, type, maxNode, op1Dup, simdBaseJitType, simdSize);
                    }
                }

                // There is no direct support for doing a combined comparison and equality for integral types.
                // These have to be implemented by performing both halves and combining their results.
                //
                // op1Dup = op1
                // op2Dup = op2
                //
                // op1 = GreaterThan(op1, op2)
                // op2 = Equals(op1Dup, op2Dup)
                //
                // result = BitwiseOr(op1, op2)

                GenTree* op1Dup = fgMakeMultiUse(&op1);
                GenTree* op2Dup = fgMakeMultiUse(&op2);

                op1 = gtNewSimdCmpOpNode(GT_GT, type, op1, op2, simdBaseJitType, simdSize);
                op2 = gtNewSimdCmpOpNode(GT_EQ, type, op1Dup, op2Dup, simdBaseJitType, simdSize);

                return gtNewSimdBinOpNode(GT_OR, type, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case GT_GT:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                if ((simdSize == 64) || varTypeIsUnsigned(simdBaseType))
                {
                    intrinsic                = NI_AVX512F_CompareGreaterThanMask;
                    needsConvertMaskToVector = true;
                    break;
                }
            }

            if (varTypeIsUnsigned(simdBaseType))
            {
                // Vector of byte, ushort, uint and ulong:
                // Hardware supports > for signed comparison. Therefore, to use it for
                // comparing unsigned numbers, we subtract a constant from both the
                // operands such that the result fits within the corresponding signed
                // type. The resulting signed numbers are compared using signed comparison.
                //
                // Vector of byte: constant to be subtracted is 2^7
                // Vector of ushort: constant to be subtracted is 2^15
                // Vector of uint: constant to be subtracted is 2^31
                // Vector of ulong: constant to be subtracted is 2^63
                //
                // We need to treat op1 and op2 as signed for comparison purpose after
                // the transformation.

                uint64_t    constVal  = 0;
                CorInfoType opJitType = simdBaseJitType;
                var_types   opType    = simdBaseType;

                switch (simdBaseType)
                {
                    case TYP_UBYTE:
                    {
                        constVal        = 0x8080808080808080;
                        simdBaseJitType = CORINFO_TYPE_BYTE;
                        simdBaseType    = TYP_BYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        constVal        = 0x8000800080008000;
                        simdBaseJitType = CORINFO_TYPE_SHORT;
                        simdBaseType    = TYP_SHORT;
                        break;
                    }

                    case TYP_UINT:
                    {
                        constVal        = 0x8000000080000000;
                        simdBaseJitType = CORINFO_TYPE_INT;
                        simdBaseType    = TYP_INT;
                        break;
                    }

                    case TYP_ULONG:
                    {
                        constVal        = 0x8000000000000000;
                        simdBaseJitType = CORINFO_TYPE_LONG;
                        simdBaseType    = TYP_LONG;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = constVal;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);

                // op1 = op1 - constVector
                op1 = gtNewSimdBinOpNode(GT_SUB, type, op1, vecCon1, opJitType, simdSize);

                // op2 = op2 - constVector
                op2 = gtNewSimdBinOpNode(GT_SUB, type, op2, vecCon2, opJitType, simdSize);
            }

            // This should have been mutated by the above path
            assert(!varTypeIsUnsigned(simdBaseType));

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_CompareGreaterThan;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                    intrinsic = NI_AVX2_CompareGreaterThan;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));
                intrinsic = NI_SSE_CompareGreaterThan;
            }
            else if (varTypeIsLong(simdBaseType))
            {
                assert(simdSize == 16);

                if (compOpportunisticallyDependsOn(InstructionSet_SSE42))
                {
                    intrinsic = NI_SSE42_CompareGreaterThan;
                }
                else
                {
                    // There is no direct SSE2 support for comparing TYP_LONG vectors.
                    // These have to be implemented in terms of TYP_INT vector comparison operations.
                    //
                    // Let us consider the case of single long element comparison.
                    // Say op1 = (x1, y1) and op2 = (x2, y2) where x1, y1, x2, and y2 are 32-bit integers that comprise
                    // the
                    // longs op1 and op2.
                    //
                    // GreaterThan(op1, op2) can be expressed in terms of > relationship between 32-bit integers that
                    // comprise op1 and op2 as
                    //                    =  (x1, y1) > (x2, y2)
                    //                    =  (x1 > x2) || [(x1 == x2) && (y1 > y2)]   - eq (1)
                    //
                    // op1Dup1 = op1
                    // op1Dup2 = op1Dup1
                    // op2Dup1 = op2
                    // op2Dup2 = op2Dup1
                    //
                    // t = (op1 > op2)                - 32-bit signed comparison
                    // u = (op1Dup1 == op2Dup1)       - 32-bit equality comparison
                    // v = (op1Dup2 > op2Dup2)        - 32-bit unsigned comparison
                    //
                    // op1 = Shuffle(t, (3, 3, 1, 1)) - This corresponds to (x1 > x2) in eq(1) above
                    // u = Shuffle(u, (3, 3, 1, 1))   - This corresponds to (x1 == x2) in eq(1) above
                    // v = Shuffle(v, (2, 2, 0, 0))   - This corresponds to (y1 > y2) in eq(1) above
                    // op2 = BitwiseAnd(u, v)         - This corresponds to [(x1 == x2) && (y1 > y2)] in eq(1) above
                    //
                    // result = BitwiseOr(op1, op2)

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op1Dup2 = fgMakeMultiUse(&op1Dup1);

                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);
                    GenTree* op2Dup2 = fgMakeMultiUse(&op2Dup1);

                    GenTree* t = gtNewSimdCmpOpNode(op, type, op1, op2, CORINFO_TYPE_INT, simdSize);
                    GenTree* u = gtNewSimdCmpOpNode(GT_EQ, type, op1Dup1, op2Dup1, CORINFO_TYPE_INT, simdSize);
                    GenTree* v = gtNewSimdCmpOpNode(op, type, op1Dup2, op2Dup2, CORINFO_TYPE_UINT, simdSize);

                    op1 = gtNewSimdHWIntrinsicNode(type, t, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                                   CORINFO_TYPE_INT, simdSize);
                    u = gtNewSimdHWIntrinsicNode(type, u, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);
                    v = gtNewSimdHWIntrinsicNode(type, v, gtNewIconNode(SHUFFLE_ZZXX, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);

                    // Validate we can't use AVX512F_VL_TernaryLogic here
                    assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                    op2 = gtNewSimdBinOpNode(GT_AND, type, u, v, simdBaseJitType, simdSize);
                    return gtNewSimdBinOpNode(GT_OR, type, op1, op2, simdBaseJitType, simdSize);
                }
            }
            else
            {
                assert(simdSize == 16);
                intrinsic = NI_SSE2_CompareGreaterThan;
            }
            break;
        }

        case GT_LE:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                if ((simdSize == 64) || !varTypeIsFloating(simdBaseType))
                {
                    intrinsic                = NI_AVX512F_CompareLessThanOrEqualMask;
                    needsConvertMaskToVector = true;
                    break;
                }
            }

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_CompareLessThanOrEqual;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));
                intrinsic = NI_SSE_CompareLessThanOrEqual;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                assert(simdSize == 16);
                intrinsic = NI_SSE2_CompareLessThanOrEqual;
            }

            if (intrinsic == NI_Illegal)
            {
                // If we don't have an intrinsic set for this, try "Min(op1, op2) == op1"
                // NOTE: technically, we can special case byte type to only require SSE2, but it
                // complicates the test matrix for little gains.
                if (((simdSize == 32) && compOpportunisticallyDependsOn(InstructionSet_AVX2)) ||
                    ((simdSize == 16) && compOpportunisticallyDependsOn(InstructionSet_SSE41)))
                {
                    // TODO-AVX512: We can use this trick for longs only with AVX-512
                    if (!varTypeIsLong(simdBaseType))
                    {
                        assert(!varTypeIsFloating(simdBaseType));
                        GenTree* op1Dup = fgMakeMultiUse(&op1);

                        // EQ(Min(op1, op2), op1)
                        GenTree* minNode = gtNewSimdMinNode(type, op1, op2, simdBaseJitType, simdSize);
                        return gtNewSimdCmpOpNode(GT_EQ, type, minNode, op1Dup, simdBaseJitType, simdSize);
                    }
                }

                // There is no direct support for doing a combined comparison and equality for integral types.
                // These have to be implemented by performing both halves and combining their results.
                //
                // op1Dup = op1
                // op2Dup = op2
                //
                // op1 = LessThan(op1, op2)
                // op2 = Equals(op1Dup, op2Dup)
                //
                // result = BitwiseOr(op1, op2)

                GenTree* op1Dup = fgMakeMultiUse(&op1);
                GenTree* op2Dup = fgMakeMultiUse(&op2);

                op1 = gtNewSimdCmpOpNode(GT_LT, type, op1, op2, simdBaseJitType, simdSize);
                op2 = gtNewSimdCmpOpNode(GT_EQ, type, op1Dup, op2Dup, simdBaseJitType, simdSize);

                return gtNewSimdBinOpNode(GT_OR, type, op1, op2, simdBaseJitType, simdSize);
            }
            break;
        }

        case GT_LT:
        {
            if (IsBaselineVector512IsaSupportedOpportunistically())
            {
                if ((simdSize == 64) || varTypeIsUnsigned(simdBaseType))
                {
                    intrinsic                = NI_AVX512F_CompareLessThanMask;
                    needsConvertMaskToVector = true;
                    break;
                }
            }

            if (varTypeIsUnsigned(simdBaseType))
            {
                // Vector of byte, ushort, uint and ulong:
                // Hardware supports < for signed comparison. Therefore, to use it for
                // comparing unsigned numbers, we subtract a constant from both the
                // operands such that the result fits within the corresponding signed
                // type. The resulting signed numbers are compared using signed comparison.
                //
                // Vector of byte: constant to be subtracted is 2^7
                // Vector of ushort: constant to be subtracted is 2^15
                // Vector of uint: constant to be subtracted is 2^31
                // Vector of ulong: constant to be subtracted is 2^63
                //
                // We need to treat op1 and op2 as signed for comparison purpose after
                // the transformation.

                uint64_t    constVal  = 0;
                CorInfoType opJitType = simdBaseJitType;
                var_types   opType    = simdBaseType;

                switch (simdBaseType)
                {
                    case TYP_UBYTE:
                    {
                        constVal        = 0x8080808080808080;
                        simdBaseJitType = CORINFO_TYPE_BYTE;
                        simdBaseType    = TYP_BYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        constVal        = 0x8000800080008000;
                        simdBaseJitType = CORINFO_TYPE_SHORT;
                        simdBaseType    = TYP_SHORT;
                        break;
                    }

                    case TYP_UINT:
                    {
                        constVal        = 0x8000000080000000;
                        simdBaseJitType = CORINFO_TYPE_INT;
                        simdBaseType    = TYP_INT;
                        break;
                    }

                    case TYP_ULONG:
                    {
                        constVal        = 0x8000000000000000;
                        simdBaseJitType = CORINFO_TYPE_LONG;
                        simdBaseType    = TYP_LONG;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = constVal;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);

                // op1 = op1 - constVector
                op1 = gtNewSimdBinOpNode(GT_SUB, type, op1, vecCon1, opJitType, simdSize);

                // op2 = op2 - constVector
                op2 = gtNewSimdBinOpNode(GT_SUB, type, op2, vecCon2, opJitType, simdSize);
            }

            // This should have been mutated by the above path
            assert(!varTypeIsUnsigned(simdBaseType));

            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

                if (varTypeIsFloating(simdBaseType))
                {
                    intrinsic = NI_AVX_CompareLessThan;
                }
                else
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                    intrinsic = NI_AVX2_CompareLessThan;
                }
            }
            else if (simdBaseType == TYP_FLOAT)
            {
                assert((simdSize == 8) || (simdSize == 12) || (simdSize == 16));
                intrinsic = NI_SSE_CompareLessThan;
            }
            else if (varTypeIsLong(simdBaseType))
            {
                assert(simdSize == 16);

                if (compOpportunisticallyDependsOn(InstructionSet_SSE42))
                {
                    intrinsic = NI_SSE42_CompareLessThan;
                }
                else
                {
                    // There is no direct SSE2 support for comparing TYP_LONG vectors.
                    // These have to be implemented in terms of TYP_INT vector comparison operations.
                    //
                    // Let us consider the case of single long element comparison.
                    // Say op1 = (x1, y1) and op2 = (x2, y2) where x1, y1, x2, and y2 are 32-bit integers that comprise
                    // the
                    // longs op1 and op2.
                    //
                    // LessThan(op1, op2) can be expressed in terms of > relationship between 32-bit integers that
                    // comprise op1 and op2 as
                    //                    =  (x1, y1) > (x2, y2)
                    //                    =  (x1 > x2) || [(x1 == x2) && (y1 > y2)]   - eq (1)
                    //
                    // op1Dup1 = op1
                    // op1Dup2 = op1Dup1
                    // op2Dup1 = op2
                    // op2Dup2 = op2Dup1
                    //
                    // t = (op1 > op2)                - 32-bit signed comparison
                    // u = (op1Dup1 == op2Dup1)       - 32-bit equality comparison
                    // v = (op1Dup2 > op2Dup2)        - 32-bit unsigned comparison
                    //
                    // op1 = Shuffle(t, (3, 3, 1, 1)) - This corresponds to (x1 > x2) in eq(1) above
                    // u = Shuffle(u, (3, 3, 1, 1))   - This corresponds to (x1 == x2) in eq(1) above
                    // v = Shuffle(v, (2, 2, 0, 0))   - This corresponds to (y1 > y2) in eq(1) above
                    // op2 = BitwiseAnd(u, v)         - This corresponds to [(x1 == x2) && (y1 > y2)] in eq(1) above
                    //
                    // result = BitwiseOr(op1, op2)

                    GenTree* op1Dup1 = fgMakeMultiUse(&op1);
                    GenTree* op1Dup2 = fgMakeMultiUse(&op1Dup1);

                    GenTree* op2Dup1 = fgMakeMultiUse(&op2);
                    GenTree* op2Dup2 = fgMakeMultiUse(&op2Dup1);

                    GenTree* t = gtNewSimdCmpOpNode(op, type, op1, op2, CORINFO_TYPE_INT, simdSize);
                    GenTree* u = gtNewSimdCmpOpNode(GT_EQ, type, op1Dup1, op2Dup1, CORINFO_TYPE_INT, simdSize);
                    GenTree* v = gtNewSimdCmpOpNode(op, type, op1Dup2, op2Dup2, CORINFO_TYPE_UINT, simdSize);

                    op1 = gtNewSimdHWIntrinsicNode(type, t, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                                   CORINFO_TYPE_INT, simdSize);
                    u = gtNewSimdHWIntrinsicNode(type, u, gtNewIconNode(SHUFFLE_WWYY, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);
                    v = gtNewSimdHWIntrinsicNode(type, v, gtNewIconNode(SHUFFLE_ZZXX, TYP_INT), NI_SSE2_Shuffle,
                                                 CORINFO_TYPE_INT, simdSize);

                    // Validate we can't use AVX512F_VL_TernaryLogic here
                    assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                    op2 = gtNewSimdBinOpNode(GT_AND, type, u, v, simdBaseJitType, simdSize);
                    return gtNewSimdBinOpNode(GT_OR, type, op1, op2, simdBaseJitType, simdSize);
                }
            }
            else
            {
                assert(simdSize == 16);
                intrinsic = NI_SSE2_CompareLessThan;
            }
            break;
        }
#elif defined(TARGET_ARM64)
        case GT_EQ:
        {
            if ((varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE)))
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_CompareEqualScalar : NI_AdvSimd_Arm64_CompareEqual;
            }
            else
            {
                intrinsic = NI_AdvSimd_CompareEqual;
            }
            break;
        }

        case GT_GE:
        {
            if ((varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE)))
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_CompareGreaterThanOrEqualScalar
                                            : NI_AdvSimd_Arm64_CompareGreaterThanOrEqual;
            }
            else
            {
                intrinsic = NI_AdvSimd_CompareGreaterThanOrEqual;
            }
            break;
        }

        case GT_GT:
        {
            if ((varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE)))
            {
                intrinsic =
                    (simdSize == 8) ? NI_AdvSimd_Arm64_CompareGreaterThanScalar : NI_AdvSimd_Arm64_CompareGreaterThan;
            }
            else
            {
                intrinsic = NI_AdvSimd_CompareGreaterThan;
            }
            break;
        }

        case GT_LE:
        {
            if ((varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE)))
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_CompareLessThanOrEqualScalar
                                            : NI_AdvSimd_Arm64_CompareLessThanOrEqual;
            }
            else
            {
                intrinsic = NI_AdvSimd_CompareLessThanOrEqual;
            }
            break;
        }

        case GT_LT:
        {
            if ((varTypeIsLong(simdBaseType) || (simdBaseType == TYP_DOUBLE)))
            {
                intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_CompareLessThanScalar : NI_AdvSimd_Arm64_CompareLessThan;
            }
            else
            {
                intrinsic = NI_AdvSimd_CompareLessThan;
            }
            break;
        }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        default:
        {
            unreached();
        }
    }

    assert(intrinsic != NI_Illegal);

#if defined(TARGET_XARCH)
    if (needsConvertMaskToVector)
    {
        GenTree* retNode = gtNewSimdHWIntrinsicNode(TYP_MASK, op1, op2, intrinsic, simdBaseJitType, simdSize);
        return gtNewSimdHWIntrinsicNode(type, retNode, NI_AVX512F_ConvertMaskToVector, simdBaseJitType, simdSize);
    }
    else
    {
        return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
    }
#else
    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
#endif
}

GenTree* Compiler::gtNewSimdCmpOpAllNode(
    genTreeOps op, var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());
    assert(type == TYP_INT);

    var_types simdType = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    assert(op1 != nullptr);
    assert(op1->TypeIs(simdType));

    assert(op2 != nullptr);
    assert(op2->TypeIs(simdType));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    switch (op)
    {
#if defined(TARGET_XARCH)
        case GT_EQ:
        {
            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                assert(varTypeIsFloating(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));

                intrinsic = NI_Vector256_op_Equality;
            }
            else if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());
                intrinsic = NI_Vector512_op_Equality;
            }
            else
            {
                intrinsic = NI_Vector128_op_Equality;
            }
            break;
        }

        case GT_GE:
        case GT_GT:
        case GT_LE:
        case GT_LT:
        {
            // We want to generate a comparison along the lines of
            // GT_XX(op1, op2).As<T, TInteger>() == Vector128<TInteger>.AllBitsSet

            if (simdSize == 32)
            {
                // TODO-XArch-CQ: It's a non-trivial amount of work to support these
                // for floating-point while only utilizing AVX. It would require, among
                // other things, inverting the comparison and potentially support for a
                // new Avx.TestNotZ intrinsic to ensure the codegen remains efficient.
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
                intrinsic = NI_Vector256_op_Equality;
            }
            else if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());
                intrinsic = NI_Vector512_op_Equality;
            }
            else
            {
                intrinsic = NI_Vector128_op_Equality;
            }

            op1 = gtNewSimdCmpOpNode(op, simdType, op1, op2, simdBaseJitType, simdSize);
            op2 = gtNewAllBitsSetConNode(simdType);

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseType    = TYP_INT;
                simdBaseJitType = CORINFO_TYPE_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseType    = TYP_LONG;
                simdBaseJitType = CORINFO_TYPE_LONG;
            }
            break;
        }
#elif defined(TARGET_ARM64)
        case GT_EQ:
        {
            intrinsic = (simdSize == 8) ? NI_Vector64_op_Equality : NI_Vector128_op_Equality;
            break;
        }

        case GT_GE:
        case GT_GT:
        case GT_LE:
        case GT_LT:
        {
            // We want to generate a comparison along the lines of
            // GT_XX(op1, op2).As<T, TInteger>() == Vector128<TInteger>.AllBitsSet

            if (simdSize == 8)
            {
                intrinsic = NI_Vector64_op_Equality;
            }
            else
            {
                intrinsic = NI_Vector128_op_Equality;
            }

            op1 = gtNewSimdCmpOpNode(op, simdType, op1, op2, simdBaseJitType, simdSize);
            op2 = gtNewAllBitsSetConNode(simdType);

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseType    = TYP_INT;
                simdBaseJitType = CORINFO_TYPE_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseType    = TYP_LONG;
                simdBaseJitType = CORINFO_TYPE_LONG;
            }
            break;
        }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        default:
        {
            unreached();
        }
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdCmpOpAnyNode(
    genTreeOps op, var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());
    assert(type == TYP_INT);

    var_types simdType = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    assert(op1 != nullptr);
    assert(op1->TypeIs(simdType));

    assert(op2 != nullptr);
    assert(op2->TypeIs(simdType));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    switch (op)
    {
#if defined(TARGET_XARCH)
        case GT_EQ:
        case GT_GE:
        case GT_GT:
        case GT_LE:
        case GT_LT:
        {
            // We want to generate a comparison along the lines of
            // GT_XX(op1, op2).As<T, TInteger>() != Vector128<TInteger>.Zero

            if (simdSize == 32)
            {
                // TODO-XArch-CQ: It's a non-trivial amount of work to support these
                // for floating-point while only utilizing AVX. It would require, among
                // other things, inverting the comparison and potentially support for a
                // new Avx.TestNotZ intrinsic to ensure the codegen remains efficient.
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

                intrinsic = NI_Vector256_op_Inequality;
            }
            else if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());
                intrinsic = NI_Vector512_op_Inequality;
            }
            else
            {
                intrinsic = NI_Vector128_op_Inequality;
            }

            op1 = gtNewSimdCmpOpNode(op, simdType, op1, op2, simdBaseJitType, simdSize);
            op2 = gtNewZeroConNode(simdType);

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseType    = TYP_INT;
                simdBaseJitType = CORINFO_TYPE_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseType    = TYP_LONG;
                simdBaseJitType = CORINFO_TYPE_LONG;
            }
            break;
        }

        case GT_NE:
        {
            if (simdSize == 64)
            {
                assert(IsBaselineVector512IsaSupportedDebugOnly());
                intrinsic = NI_Vector512_op_Inequality;
            }
            else if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                assert(varTypeIsFloating(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));

                intrinsic = NI_Vector256_op_Inequality;
            }
            else
            {
                intrinsic = NI_Vector128_op_Inequality;
            }
            break;
        }
#elif defined(TARGET_ARM64)
        case GT_EQ:
        case GT_GE:
        case GT_GT:
        case GT_LE:
        case GT_LT:
        {
            // We want to generate a comparison along the lines of
            // GT_XX(op1, op2).As<T, TInteger>() != Vector128<TInteger>.Zero

            intrinsic = (simdSize == 8) ? NI_Vector64_op_Inequality : NI_Vector128_op_Inequality;

            op1 = gtNewSimdCmpOpNode(op, simdType, op1, op2, simdBaseJitType, simdSize);
            op2 = gtNewZeroConNode(simdType);

            if (simdBaseType == TYP_FLOAT)
            {
                simdBaseType    = TYP_INT;
                simdBaseJitType = CORINFO_TYPE_INT;
            }
            else if (simdBaseType == TYP_DOUBLE)
            {
                simdBaseType    = TYP_LONG;
                simdBaseJitType = CORINFO_TYPE_LONG;
            }
            break;
        }

        case GT_NE:
        {
            intrinsic = (simdSize == 8) ? NI_Vector64_op_Inequality : NI_Vector128_op_Inequality;
            break;
        }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        default:
        {
            unreached();
        }
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdCndSelNode(
    var_types type, GenTree* op1, GenTree* op2, GenTree* op3, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    assert(op3 != nullptr);
    assert(op3->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    assert((simdSize != 32) || compIsaSupportedDebugOnly(InstructionSet_AVX));

    if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
    {
        GenTree* control = gtNewIconNode(0xCA); // (B & A) | (C & ~A)
        return gtNewSimdTernaryLogicNode(type, op1, op2, op3, control, simdBaseJitType, simdSize);
    }

    assert(simdSize != 64);

    if (simdSize == 32)
    {
        intrinsic = NI_Vector256_ConditionalSelect;
    }
    else
    {
        intrinsic = NI_Vector128_ConditionalSelect;
    }
    return gtNewSimdHWIntrinsicNode(type, op1, op2, op3, intrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    return gtNewSimdHWIntrinsicNode(type, op1, op2, op3, NI_AdvSimd_BitwiseSelect, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdCreateBroadcastNode: Creates a new simd CreateBroadcast node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The value of broadcast to every element of the simd value
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created CreateBroadcast node
//
GenTree* Compiler::gtNewSimdCreateBroadcastNode(var_types   type,
                                                GenTree*    op1,
                                                CorInfoType simdBaseJitType,
                                                unsigned    simdSize)
{
    NamedIntrinsic hwIntrinsicID = NI_Vector128_Create;
    var_types      simdBaseType  = JitType2PreciseVarType(simdBaseJitType);

    if (op1->IsIntegralConst() || op1->IsCnsFltOrDbl())
    {
        GenTreeVecCon* vecCon = gtNewVconNode(type);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                uint8_t cnsVal = static_cast<uint8_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < simdSize; i++)
                {
                    vecCon->gtSimdVal.u8[i] = cnsVal;
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                uint16_t cnsVal = static_cast<uint16_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 2); i++)
                {
                    vecCon->gtSimdVal.u16[i] = cnsVal;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                uint32_t cnsVal = static_cast<uint32_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 4); i++)
                {
                    vecCon->gtSimdVal.u32[i] = cnsVal;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                uint64_t cnsVal = static_cast<uint64_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon->gtSimdVal.u64[i] = cnsVal;
                }
                break;
            }

            case TYP_FLOAT:
            {
                float cnsVal = static_cast<float>(op1->AsDblCon()->DconValue());

                for (unsigned i = 0; i < (simdSize / 4); i++)
                {
                    vecCon->gtSimdVal.f32[i] = cnsVal;
                }
                break;
            }

            case TYP_DOUBLE:
            {
                double cnsVal = static_cast<double>(op1->AsDblCon()->DconValue());

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon->gtSimdVal.f64[i] = cnsVal;
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return vecCon;
    }

#if defined(TARGET_XARCH)
#if defined(TARGET_X86)
    if (varTypeIsLong(simdBaseType) && !op1->IsIntegralConst())
    {
        // TODO-XARCH-CQ: It may be beneficial to emit the movq
        // instruction, which takes a 64-bit memory address and
        // works on 32-bit x86 systems.
        unreached();
    }
#endif // TARGET_X86

    if (simdSize == 64)
    {
        hwIntrinsicID = NI_Vector512_Create;
    }
    else if (simdSize == 32)
    {
        hwIntrinsicID = NI_Vector256_Create;
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 8)
    {
        hwIntrinsicID = NI_Vector64_Create;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, hwIntrinsicID, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdCreateScalarNode: Creates a new simd CreateScalar node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The value of element 0 of the simd value
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created CreateScalar node
//
GenTree* Compiler::gtNewSimdCreateScalarNode(var_types   type,
                                             GenTree*    op1,
                                             CorInfoType simdBaseJitType,
                                             unsigned    simdSize)
{
    NamedIntrinsic hwIntrinsicID = NI_Vector128_CreateScalar;
    var_types      simdBaseType  = JitType2PreciseVarType(simdBaseJitType);

    if (op1->IsIntegralConst() || op1->IsCnsFltOrDbl())
    {
        GenTreeVecCon* vecCon = gtNewVconNode(type);
        vecCon->gtSimdVal     = {};

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                uint8_t cnsVal          = static_cast<uint8_t>(op1->AsIntConCommon()->IntegralValue());
                vecCon->gtSimdVal.u8[0] = cnsVal;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                uint16_t cnsVal          = static_cast<uint16_t>(op1->AsIntConCommon()->IntegralValue());
                vecCon->gtSimdVal.u16[0] = cnsVal;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                uint32_t cnsVal          = static_cast<uint32_t>(op1->AsIntConCommon()->IntegralValue());
                vecCon->gtSimdVal.u32[0] = cnsVal;
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                uint64_t cnsVal          = static_cast<uint64_t>(op1->AsIntConCommon()->IntegralValue());
                vecCon->gtSimdVal.u64[0] = cnsVal;
                break;
            }

            case TYP_FLOAT:
            {
                float cnsVal             = static_cast<float>(op1->AsDblCon()->DconValue());
                vecCon->gtSimdVal.f32[0] = cnsVal;
                break;
            }

            case TYP_DOUBLE:
            {
                double cnsVal            = static_cast<double>(op1->AsDblCon()->DconValue());
                vecCon->gtSimdVal.f64[0] = cnsVal;
                break;
            }

            default:
            {
                unreached();
            }
        }

        return vecCon;
    }

#if defined(TARGET_XARCH)
#if defined(TARGET_X86)
    if (varTypeIsLong(simdBaseType) && !op1->IsIntegralConst())
    {
        // TODO-XARCH-CQ: It may be beneficial to emit the movq
        // instruction, which takes a 64-bit memory address and
        // works on 32-bit x86 systems.
        unreached();
    }
#endif // TARGET_X86

    if (simdSize == 32)
    {
        hwIntrinsicID = NI_Vector256_CreateScalar;
    }
    else if (simdSize == 64)
    {
        hwIntrinsicID = NI_Vector512_CreateScalar;
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 8)
    {
        hwIntrinsicID = (genTypeSize(simdBaseType) == 8) ? NI_Vector64_Create : NI_Vector64_CreateScalar;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, hwIntrinsicID, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdCreateScalarUnsafeNode: Creates a new simd CreateScalarUnsafe node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The value of element 0 of the simd value
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created CreateScalarUnsafe node
//
// Remarks:
//    This API is unsafe as it leaves the upper-bits of the vector undefined
//
GenTree* Compiler::gtNewSimdCreateScalarUnsafeNode(var_types   type,
                                                   GenTree*    op1,
                                                   CorInfoType simdBaseJitType,
                                                   unsigned    simdSize)
{
    NamedIntrinsic hwIntrinsicID = NI_Vector128_CreateScalarUnsafe;
    var_types      simdBaseType  = JitType2PreciseVarType(simdBaseJitType);

    if (op1->IsIntegralConst() || op1->IsCnsFltOrDbl())
    {
        GenTreeVecCon* vecCon = gtNewVconNode(type);

        // Since the upper bits are considered non-deterministic and we can therefore
        // set them to anything, we broadcast the value.
        //
        // We do this as it simplifies the logic and allows certain code paths to
        // have better codegen, such as for 0, AllBitsSet, or certain small constants

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                uint8_t cnsVal = static_cast<uint8_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < simdSize; i++)
                {
                    vecCon->gtSimdVal.u8[i] = cnsVal;
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                uint16_t cnsVal = static_cast<uint16_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 2); i++)
                {
                    vecCon->gtSimdVal.u16[i] = cnsVal;
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                uint32_t cnsVal = static_cast<uint32_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 4); i++)
                {
                    vecCon->gtSimdVal.u32[i] = cnsVal;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                uint64_t cnsVal = static_cast<uint64_t>(op1->AsIntConCommon()->IntegralValue());

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon->gtSimdVal.u64[i] = cnsVal;
                }
                break;
            }

            case TYP_FLOAT:
            {
                float cnsVal = static_cast<float>(op1->AsDblCon()->DconValue());

                for (unsigned i = 0; i < (simdSize / 4); i++)
                {
                    vecCon->gtSimdVal.f32[i] = cnsVal;
                }
                break;
            }

            case TYP_DOUBLE:
            {
                double cnsVal = static_cast<double>(op1->AsDblCon()->DconValue());

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon->gtSimdVal.f64[i] = cnsVal;
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        return vecCon;
    }

#if defined(TARGET_XARCH)
#if defined(TARGET_X86)
    if (varTypeIsLong(simdBaseType) && !op1->IsIntegralConst())
    {
        // TODO-XARCH-CQ: It may be beneficial to emit the movq
        // instruction, which takes a 64-bit memory address and
        // works on 32-bit x86 systems.
        unreached();
    }
#endif // TARGET_X86

    if (simdSize == 32)
    {
        hwIntrinsicID = NI_Vector256_CreateScalarUnsafe;
    }
    else if (simdSize == 64)
    {
        hwIntrinsicID = NI_Vector512_CreateScalarUnsafe;
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 8)
    {
        hwIntrinsicID = (genTypeSize(simdBaseType) == 8) ? NI_Vector64_Create : NI_Vector64_CreateScalarUnsafe;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, hwIntrinsicID, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdCreateSequenceNode: Creates a new simd CreateSequence node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The starting value
//    op2                 - The step value
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created CreateSequence node
//
GenTree* Compiler::gtNewSimdCreateSequenceNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    // This effectively doees: (Indices * op2) + Create(op1)
    //
    // When both op2 and op1 are constant we can fully fold this to a constant. Additionally,
    // if only op2 is a constant we can simplify the computation by a lot. However, if only op1
    // is constant than there isn't any real optimization we can do and we need the full computation.

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    GenTree* result    = nullptr;
    bool     isPartial = true;

    if (op2->OperIsConst())
    {
        GenTreeVecCon* vcon       = gtNewVconNode(type);
        uint32_t       simdLength = getSIMDVectorLength(simdSize, simdBaseType);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                uint8_t start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsIntegralConst());
                    start     = static_cast<uint8_t>(op1->AsIntConCommon()->IntegralValue());
                    isPartial = false;
                }

                assert(op2->IsIntegralConst());
                uint8_t step = static_cast<uint8_t>(op2->AsIntConCommon()->IntegralValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.u8[index] = static_cast<uint8_t>((index * step) + start);
                }
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                uint16_t start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsIntegralConst());
                    start     = static_cast<uint16_t>(op1->AsIntConCommon()->IntegralValue());
                    isPartial = false;
                }

                assert(op2->IsIntegralConst());
                uint16_t step = static_cast<uint16_t>(op2->AsIntConCommon()->IntegralValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.u16[index] = static_cast<uint16_t>((index * step) + start);
                }
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                uint32_t start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsIntegralConst());
                    start     = static_cast<uint32_t>(op1->AsIntConCommon()->IntegralValue());
                    isPartial = false;
                }

                assert(op2->IsIntegralConst());
                uint32_t step = static_cast<uint32_t>(op2->AsIntConCommon()->IntegralValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.u32[index] = static_cast<uint32_t>((index * step) + start);
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                uint64_t start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsIntegralConst());
                    start     = static_cast<uint64_t>(op1->AsIntConCommon()->IntegralValue());
                    isPartial = false;
                }

                assert(op2->IsIntegralConst());
                uint64_t step = static_cast<uint64_t>(op2->AsIntConCommon()->IntegralValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.u64[index] = static_cast<uint64_t>((index * step) + start);
                }
                break;
            }

            case TYP_FLOAT:
            {
                float start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsCnsFltOrDbl());
                    start     = static_cast<float>(op1->AsDblCon()->DconValue());
                    isPartial = false;
                }

                assert(op2->IsCnsFltOrDbl());
                float step = static_cast<float>(op2->AsDblCon()->DconValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.f32[index] = static_cast<float>((index * step) + start);
                }
                break;
            }

            case TYP_DOUBLE:
            {
                double start = 0;

                if (op1->OperIsConst())
                {
                    assert(op1->IsCnsFltOrDbl());
                    start     = static_cast<double>(op1->AsDblCon()->DconValue());
                    isPartial = false;
                }

                assert(op2->IsCnsFltOrDbl());
                double step = static_cast<double>(op2->AsDblCon()->DconValue());

                for (uint32_t index = 0; index < simdLength; index++)
                {
                    vcon->gtSimdVal.f64[index] = static_cast<double>((index * step) + start);
                }
                break;
            }

            default:
            {
                unreached();
            }
        }

        result = vcon;
    }
    else
    {
        GenTree* indices = gtNewSimdGetIndicesNode(type, simdBaseJitType, simdSize);
        result           = gtNewSimdBinOpNode(GT_MUL, type, indices, op2, simdBaseJitType, simdSize);
    }

    if (isPartial)
    {
        GenTree* start = gtNewSimdCreateBroadcastNode(type, op1, simdBaseJitType, simdSize);
        result         = gtNewSimdBinOpNode(GT_ADD, type, result, start, simdBaseJitType, simdSize);
    }

    return result;
}

GenTree* Compiler::gtNewSimdDotProdNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    var_types simdType = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    assert(op1 != nullptr);
    assert(op1->TypeIs(simdType));

    assert(op2 != nullptr);
    assert(op2->TypeIs(simdType));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsSIMD(type));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    assert(!varTypeIsByte(simdBaseType) && !varTypeIsLong(simdBaseType));
    assert(simdSize != 64);

    if (simdSize == 32)
    {
        assert(varTypeIsFloating(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));
        intrinsic = NI_Vector256_Dot;
    }
    else
    {
        assert(((simdBaseType != TYP_INT) && (simdBaseType != TYP_UINT)) ||
               compIsaSupportedDebugOnly(InstructionSet_SSE41));
        intrinsic = NI_Vector128_Dot;
    }
#elif defined(TARGET_ARM64)
    assert(!varTypeIsLong(simdBaseType));
    intrinsic = (simdSize == 8) ? NI_Vector64_Dot : NI_Vector128_Dot;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdFloorNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsFloating(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        intrinsic = NI_AVX_Floor;
    }
    else if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        GenTree* op2 = gtNewIconNode(static_cast<int32_t>(FloatRoundingMode::ToNegativeInfinity));
        return gtNewSimdHWIntrinsicNode(type, op1, op2, NI_AVX512F_RoundScale, simdBaseJitType, simdSize);
    }
    else
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));
        intrinsic = NI_SSE41_Floor;
    }
#elif defined(TARGET_ARM64)
    if (simdBaseType == TYP_DOUBLE)
    {
        intrinsic = (simdSize == 8) ? NI_AdvSimd_FloorScalar : NI_AdvSimd_Arm64_Floor;
    }
    else
    {
        intrinsic = NI_AdvSimd_Floor;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdGetElementNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    NamedIntrinsic intrinsicId  = NI_Vector128_GetElement;
    var_types      simdBaseType = JitType2PreciseVarType(simdBaseJitType);

    assert(varTypeIsArithmetic(simdBaseType));

#if defined(TARGET_XARCH)
    bool useToScalar = op2->IsIntegralConst(0);

#if defined(TARGET_X86)
    // We handle decomposition via GetElement for simplicity
    useToScalar &= !varTypeIsLong(simdBaseType);
#endif // TARGET_X86

    if (useToScalar)
    {
        return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);
    }

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
        case TYP_LONG:
        case TYP_ULONG:
        {
            // Using software fallback if simdBaseType is not supported by hardware
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));
            break;
        }

        case TYP_DOUBLE:
        case TYP_FLOAT:
        case TYP_SHORT:
        case TYP_USHORT:
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));
            break;
        }

        default:
        {
            unreached();
        }
    }

    if (simdSize == 64)
    {
        intrinsicId = NI_Vector512_GetElement;
    }
    else if (simdSize == 32)
    {
        intrinsicId = NI_Vector256_GetElement;
    }
#elif defined(TARGET_ARM64)
    if (op2->IsIntegralConst(0))
    {
        return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);
    }

    if (simdSize == 8)
    {
        intrinsicId = NI_Vector64_GetElement;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    int  immUpperBound    = getSIMDVectorLength(simdSize, simdBaseType) - 1;
    bool rangeCheckNeeded = !op2->OperIsConst();

    if (!rangeCheckNeeded)
    {
        ssize_t imm8     = op2->AsIntCon()->IconValue();
        rangeCheckNeeded = (imm8 < 0) || (imm8 > immUpperBound);
    }

    if (rangeCheckNeeded)
    {
        op2 = addRangeCheckForHWIntrinsic(op2, 0, immUpperBound);
    }

    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsicId, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdGetIndicesNode: Creates a new simd get_Indices node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created get_Indices node
//
GenTree* Compiler::gtNewSimdGetIndicesNode(var_types type, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    GenTreeVecCon* indices    = gtNewVconNode(type);
    uint32_t       simdLength = getSIMDVectorLength(simdSize, simdBaseType);

    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.u8[index] = static_cast<uint8_t>(index);
            }
            break;
        }

        case TYP_SHORT:
        case TYP_USHORT:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.u16[index] = static_cast<uint16_t>(index);
            }
            break;
        }

        case TYP_INT:
        case TYP_UINT:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.u32[index] = static_cast<uint32_t>(index);
            }
            break;
        }

        case TYP_LONG:
        case TYP_ULONG:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.u64[index] = static_cast<uint64_t>(index);
            }
            break;
        }

        case TYP_FLOAT:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.f32[index] = static_cast<float>(index);
            }
            break;
        }

        case TYP_DOUBLE:
        {
            for (uint32_t index = 0; index < simdLength; index++)
            {
                indices->gtSimdVal.f64[index] = static_cast<double>(index);
            }
            break;
        }

        default:
        {
            unreached();
        }
    }

    return indices;
}

GenTree* Compiler::gtNewSimdGetLowerNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsicId = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(type == TYP_SIMD16);
        intrinsicId = NI_Vector256_GetLower;
    }
    else
    {
        assert((type == TYP_SIMD32) && (simdSize == 64));
        intrinsicId = NI_Vector512_GetLower;
    }
#elif defined(TARGET_ARM64)
    assert((type == TYP_SIMD8) && (simdSize == 16));
    intrinsicId = NI_Vector128_GetLower;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsicId != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsicId, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdGetUpperNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsicId = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(type == TYP_SIMD16);
        intrinsicId = NI_Vector256_GetUpper;
    }
    else
    {
        assert((type == TYP_SIMD32) && (simdSize == 64));
        intrinsicId = NI_Vector512_GetUpper;
    }
#elif defined(TARGET_ARM64)
    assert((type == TYP_SIMD8) && (simdSize == 16));
    intrinsicId = NI_Vector128_GetUpper;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsicId != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsicId, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdLoadNode: Creates a new simd Load node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The address of the value to be loaded
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created Load node
//
GenTree* Compiler::gtNewSimdLoadNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    return gtNewIndir(type, op1);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdLoadAlignedNode: Creates a new simd LoadAligned node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The address of the value to be loaded
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created LoadAligned node
//
GenTree* Compiler::gtNewSimdLoadAlignedNode(var_types   type,
                                            GenTree*    op1,
                                            CorInfoType simdBaseJitType,
                                            unsigned    simdSize)
{
#if defined(TARGET_XARCH)
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        intrinsic = NI_AVX512F_LoadAlignedVector512;
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_AVX_LoadAlignedVector256;
    }
    else if (simdBaseType != TYP_FLOAT)
    {
        intrinsic = NI_SSE2_LoadAlignedVector128;
    }
    else
    {
        intrinsic = NI_SSE_LoadAlignedVector128;
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    // ARM64 doesn't have aligned loads, but aligned loads are only validated to be
    // aligned when optimizations are disable, so only skip the intrinsic handling
    // if optimizations are enabled

    assert(opts.OptimizationEnabled());
    return gtNewSimdLoadNode(type, op1, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdLoadNonTemporalNode: Creates a new simd LoadNonTemporal node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The address of the value to be loaded
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created LoadNonTemporal node
//
GenTree* Compiler::gtNewSimdLoadNonTemporalNode(var_types   type,
                                                GenTree*    op1,
                                                CorInfoType simdBaseJitType,
                                                unsigned    simdSize)
{
#if defined(TARGET_XARCH)
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic     = NI_Illegal;
    bool           isNonTemporal = false;

    // We don't guarantee a non-temporal load will actually occur, so fallback
    // to regular aligned loads if the required ISA isn't supported.

    if (simdSize == 32)
    {
        if (compOpportunisticallyDependsOn(InstructionSet_AVX2))
        {
            intrinsic     = NI_AVX2_LoadAlignedVector256NonTemporal;
            isNonTemporal = true;
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
            intrinsic = NI_AVX_LoadAlignedVector256;
        }
    }
    else if (simdSize == 64)
    {
        if (compOpportunisticallyDependsOn(InstructionSet_AVX512F))
        {
            intrinsic     = NI_AVX512F_LoadAlignedVector512NonTemporal;
            isNonTemporal = true;
        }
    }
    else if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        intrinsic     = NI_SSE41_LoadAlignedVector128NonTemporal;
        isNonTemporal = true;
    }
    else if (simdBaseType != TYP_FLOAT)
    {
        intrinsic = NI_SSE2_LoadAlignedVector128;
    }
    else
    {
        intrinsic = NI_SSE_LoadAlignedVector128;
    }

    if (isNonTemporal)
    {
        // float and double don't have actual instructions for non-temporal loads
        // so we'll just use the equivalent integer instruction instead.

        if (simdBaseType == TYP_FLOAT)
        {
            simdBaseJitType = CORINFO_TYPE_INT;
        }
        else if (simdBaseType == TYP_DOUBLE)
        {
            simdBaseJitType = CORINFO_TYPE_LONG;
        }
    }

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    // ARM64 doesn't have aligned loads, but aligned loads are only validated to be
    // aligned when optimizations are disable, so only skip the intrinsic handling
    // if optimizations are enabled

    assert(opts.OptimizationEnabled());
    return gtNewSimdLoadNode(type, op1, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdMaxNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

        if (varTypeIsFloating(simdBaseType))
        {
            intrinsic = NI_AVX_Max;
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

            if (!varTypeIsLong(simdBaseType))
            {
                intrinsic = NI_AVX2_Max;
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
            {
                intrinsic = NI_AVX512F_VL_Max;
            }
        }
    }
    else if (simdSize == 64)
    {
        if (varTypeIsSmall(simdBaseType))
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
            intrinsic = NI_AVX512BW_Max;
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            intrinsic = NI_AVX512F_Max;
        }
    }
    else
    {
        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_USHORT:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    intrinsic = NI_SSE41_Max;
                    break;
                }

                uint64_t    constVal  = 0;
                CorInfoType opJitType = simdBaseJitType;
                var_types   opType    = simdBaseType;
                genTreeOps  fixupOp1  = GT_NONE;
                genTreeOps  fixupOp2  = GT_NONE;

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    {
                        constVal        = 0x8080808080808080;
                        fixupOp1        = GT_SUB;
                        fixupOp2        = GT_ADD;
                        simdBaseJitType = CORINFO_TYPE_UBYTE;
                        simdBaseType    = TYP_UBYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        constVal        = 0x8000800080008000;
                        fixupOp1        = GT_ADD;
                        fixupOp2        = GT_SUB;
                        simdBaseJitType = CORINFO_TYPE_SHORT;
                        simdBaseType    = TYP_SHORT;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                assert(fixupOp1 != GT_NONE);
                assert(fixupOp2 != GT_NONE);
                assert(opJitType != simdBaseJitType);
                assert(opType != simdBaseType);

                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = constVal;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);
                GenTree* vecCon3 = gtCloneExpr(vecCon2);

                // op1 = op1 - constVector
                // -or-
                // op1 = op1 + constVector
                op1 = gtNewSimdBinOpNode(fixupOp1, type, op1, vecCon1, opJitType, simdSize);

                // op2 = op2 - constVector
                // -or-
                // op2 = op2 + constVector
                op2 = gtNewSimdBinOpNode(fixupOp1, type, op2, vecCon2, opJitType, simdSize);

                // op1 = Max(op1, op2)
                op1 = gtNewSimdMaxNode(type, op1, op2, simdBaseJitType, simdSize);

                // result = op1 + constVector
                // -or-
                // result = op1 - constVector
                return gtNewSimdBinOpNode(fixupOp2, type, op1, vecCon3, opJitType, simdSize);
            }

            case TYP_INT:
            case TYP_UINT:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    intrinsic = NI_SSE41_Max;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
                {
                    intrinsic = NI_AVX512F_VL_Max;
                }
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_SSE_Max;
                break;
            }

            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_DOUBLE:
            {
                intrinsic = NI_SSE2_Max;
                break;
            }

            default:
            {
                unreached();
            }
        }
    }
#elif defined(TARGET_ARM64)
    if (!varTypeIsLong(simdBaseType))
    {
        if (simdBaseType == TYP_DOUBLE)
        {
            intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_MaxScalar : NI_AdvSimd_Arm64_Max;
        }
        else
        {
            intrinsic = NI_AdvSimd_Max;
        }
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    if (intrinsic != NI_Illegal)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
    }

    GenTree* op1Dup = fgMakeMultiUse(&op1);
    GenTree* op2Dup = fgMakeMultiUse(&op2);

    // op1 = op1 > op2
    op1 = gtNewSimdCmpOpNode(GT_GT, type, op1, op2, simdBaseJitType, simdSize);

    // result = ConditionalSelect(op1, op1Dup, op2Dup)
    return gtNewSimdCndSelNode(type, op1, op1Dup, op2Dup, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdMinNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

        if (varTypeIsFloating(simdBaseType))
        {
            intrinsic = NI_AVX_Min;
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

            if (!varTypeIsLong(simdBaseType))
            {
                intrinsic = NI_AVX2_Min;
            }
            else if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
            {
                intrinsic = NI_AVX512F_VL_Min;
            }
        }
    }
    else if (simdSize == 64)
    {
        if (varTypeIsSmall(simdBaseType))
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW));
            intrinsic = NI_AVX512BW_Min;
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            intrinsic = NI_AVX512F_Min;
        }
    }
    else
    {
        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_USHORT:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    intrinsic = NI_SSE41_Min;
                    break;
                }

                GenTree*    constVal  = nullptr;
                CorInfoType opJitType = simdBaseJitType;
                var_types   opType    = simdBaseType;
                genTreeOps  fixupOp1  = GT_NONE;
                genTreeOps  fixupOp2  = GT_NONE;

                switch (simdBaseType)
                {
                    case TYP_BYTE:
                    {
                        constVal        = gtNewIconNode(0x80808080);
                        fixupOp1        = GT_SUB;
                        fixupOp2        = GT_ADD;
                        simdBaseJitType = CORINFO_TYPE_UBYTE;
                        simdBaseType    = TYP_UBYTE;
                        break;
                    }

                    case TYP_USHORT:
                    {
                        constVal        = gtNewIconNode(0x80008000);
                        fixupOp1        = GT_ADD;
                        fixupOp2        = GT_SUB;
                        simdBaseJitType = CORINFO_TYPE_SHORT;
                        simdBaseType    = TYP_SHORT;
                        break;
                    }

                    default:
                    {
                        unreached();
                    }
                }

                assert(constVal != nullptr);
                assert(fixupOp1 != GT_NONE);
                assert(fixupOp2 != GT_NONE);
                assert(opJitType != simdBaseJitType);
                assert(opType != simdBaseType);

                GenTree* constVector = gtNewSimdCreateBroadcastNode(type, constVal, CORINFO_TYPE_INT, simdSize);

                GenTree* constVectorDup1 = fgMakeMultiUse(&constVector);
                GenTree* constVectorDup2 = fgMakeMultiUse(&constVectorDup1);

                // op1 = op1 - constVector
                // -or-
                // op1 = op1 + constVector
                op1 = gtNewSimdBinOpNode(fixupOp1, type, op1, constVector, opJitType, simdSize);

                // op2 = op2 - constVectorDup1
                // -or-
                // op2 = op2 + constVectorDup1
                op2 = gtNewSimdBinOpNode(fixupOp1, type, op2, constVectorDup1, opJitType, simdSize);

                // op1 = Min(op1, op2)
                op1 = gtNewSimdMinNode(type, op1, op2, simdBaseJitType, simdSize);

                // result = op1 + constVectorDup2
                // -or-
                // result = op1 - constVectorDup2
                return gtNewSimdBinOpNode(fixupOp2, type, op1, constVectorDup2, opJitType, simdSize);
            }

            case TYP_INT:
            case TYP_UINT:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    intrinsic = NI_SSE41_Min;
                }
                break;
            }

            case TYP_LONG:
            case TYP_ULONG:
            {
                if (compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
                {
                    intrinsic = NI_AVX512F_VL_Min;
                }
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_SSE_Min;
                break;
            }

            case TYP_UBYTE:
            case TYP_SHORT:
            case TYP_DOUBLE:
            {
                intrinsic = NI_SSE2_Min;
                break;
            }

            default:
            {
                unreached();
            }
        }
    }
#elif defined(TARGET_ARM64)
    if (!varTypeIsLong(simdBaseType))
    {
        if (simdBaseType == TYP_DOUBLE)
        {
            intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_MinScalar : NI_AdvSimd_Arm64_Min;
        }
        else
        {
            intrinsic = NI_AdvSimd_Min;
        }
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    if (intrinsic != NI_Illegal)
    {
        return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsic, simdBaseJitType, simdSize);
    }

    GenTree* op1Dup = fgMakeMultiUse(&op1);
    GenTree* op2Dup = fgMakeMultiUse(&op2);

    // op1 = op1 < op2
    op1 = gtNewSimdCmpOpNode(GT_LT, type, op1, op2, simdBaseJitType, simdSize);

    // result = ConditionalSelect(op1, op1Dup, op2Dup)
    return gtNewSimdCndSelNode(type, op1, op1Dup, op2Dup, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdNarrowNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType) && !varTypeIsLong(simdBaseType));

    GenTree* tmp1;
    GenTree* tmp2;

#if defined(TARGET_XARCH)
    GenTree* tmp3;
    GenTree* tmp4;

    if (IsBaselineVector512IsaSupportedOpportunistically())
    {
        // This is the same in principle to the other comments below, however due to
        // code formatting, its too long to reasonably display here.

        assert((simdSize == 16) || (simdSize == 32) || (simdSize == 64));
        var_types tmpSimdType = (simdSize == 64) ? TYP_SIMD32 : TYP_SIMD16;

        NamedIntrinsic intrinsicId;
        CorInfoType    opBaseJitType;

        switch (simdBaseType)
        {
            case TYP_BYTE:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512BW_ConvertToVector256SByte;
                }
                else
                {
                    intrinsicId = NI_AVX512BW_VL_ConvertToVector128SByte;
                }

                opBaseJitType = CORINFO_TYPE_SHORT;
                break;
            }

            case TYP_UBYTE:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512BW_ConvertToVector256Byte;
                }
                else
                {
                    intrinsicId = NI_AVX512BW_VL_ConvertToVector128Byte;
                }

                opBaseJitType = CORINFO_TYPE_USHORT;
                break;
            }

            case TYP_SHORT:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512F_ConvertToVector256Int16;
                }
                else
                {
                    intrinsicId = NI_AVX512F_VL_ConvertToVector128Int16;
                }

                opBaseJitType = CORINFO_TYPE_INT;
                break;
            }

            case TYP_USHORT:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512F_ConvertToVector256UInt16;
                }
                else
                {
                    intrinsicId = NI_AVX512F_VL_ConvertToVector128UInt16;
                }

                opBaseJitType = CORINFO_TYPE_UINT;
                break;
            }

            case TYP_INT:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512F_ConvertToVector256Int32;
                }
                else
                {
                    intrinsicId = NI_AVX512F_VL_ConvertToVector128Int32;
                }

                opBaseJitType = CORINFO_TYPE_LONG;
                break;
            }

            case TYP_UINT:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512F_ConvertToVector256UInt32;
                }
                else
                {
                    intrinsicId = NI_AVX512F_VL_ConvertToVector128UInt32;
                }

                opBaseJitType = CORINFO_TYPE_ULONG;
                break;
            }

            case TYP_FLOAT:
            {
                if (simdSize == 64)
                {
                    intrinsicId = NI_AVX512F_ConvertToVector256Single;
                }
                else if (simdSize == 32)
                {
                    intrinsicId = NI_AVX_ConvertToVector128Single;
                }
                else
                {
                    intrinsicId = NI_SSE2_ConvertToVector128Single;
                }

                opBaseJitType = CORINFO_TYPE_DOUBLE;
                break;
            }

            default:
            {
                unreached();
            }
        }

        tmp1 = gtNewSimdHWIntrinsicNode(tmpSimdType, op1, intrinsicId, opBaseJitType, simdSize);
        tmp2 = gtNewSimdHWIntrinsicNode(tmpSimdType, op2, intrinsicId, opBaseJitType, simdSize);

        if (simdSize == 16)
        {
            return gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE_MoveLowToHigh, CORINFO_TYPE_FLOAT, simdSize);
        }

        intrinsicId = (simdSize == 64) ? NI_Vector256_ToVector512Unsafe : NI_Vector128_ToVector256Unsafe;

        tmp1 = gtNewSimdHWIntrinsicNode(type, tmp1, intrinsicId, simdBaseJitType, simdSize / 2);
        return gtNewSimdWithUpperNode(type, tmp1, tmp2, simdBaseJitType, simdSize);
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

                // This is the same in principle to the other comments below, however due to
                // code formatting, its too long to reasonably display here.
                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = 0x00FF00FF00FF00FF;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);

                // Validate we can't use AVX512F_VL_TernaryLogic here
                assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                tmp1 = gtNewSimdBinOpNode(GT_AND, type, op1, vecCon1, simdBaseJitType, simdSize);
                tmp2 = gtNewSimdBinOpNode(GT_AND, type, op2, vecCon2, simdBaseJitType, simdSize);
                tmp3 = gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_AVX2_PackUnsignedSaturate, CORINFO_TYPE_UBYTE,
                                                simdSize);

                CorInfoType permuteBaseJitType = (simdBaseType == TYP_BYTE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_ULONG;
                return gtNewSimdHWIntrinsicNode(type, tmp3, gtNewIconNode(SHUFFLE_WYZX), NI_AVX2_Permute4x64,
                                                permuteBaseJitType, simdSize);
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

                // op1 = Elements 0L, 0U, 1L, 1U, 2L, 2U, 3L, 3U | 4L, 4U, 5L, 5U, 6L, 6U, 7L, 7U
                // op2 = Elements 8L, 8U, 9L, 9U, AL, AU, BL, BU | CL, CU, DL, DU, EL, EU, FL, FU
                //
                // tmp2 = Elements 0L, --, 1L, --, 2L, --, 3L, -- | 4L, --, 5L, --, 6L, --, 7L, --
                // tmp3 = Elements 8L, --, 9L, --, AL, --, BL, -- | CL, --, DL, --, EL, --, FL, --
                // tmp4 = Elements 0L, 1L, 2L, 3L, 8L, 9L, AL, BL | 4L, 5L, 6L, 7L, CL, DL, EL, FL
                // return Elements 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L | 8L, 9L, AL, BL, CL, DL, EL, FL
                //
                // var vcns = Vector256.Create(0x0000FFFF).AsInt16();
                // var tmp1 = Avx2.And(op1.AsInt16(), vcns);
                // var tmp2 = Avx2.And(op2.AsInt16(), vcns);
                // var tmp3 = Avx2.PackUnsignedSaturate(tmp1, tmp2);
                // return Avx2.Permute4x64(tmp3.AsUInt64(), SHUFFLE_WYZX).As<T>();

                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = 0x0000FFFF0000FFFF;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);

                // Validate we can't use AVX512F_VL_TernaryLogic here
                assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                tmp1 = gtNewSimdBinOpNode(GT_AND, type, op1, vecCon1, simdBaseJitType, simdSize);
                tmp2 = gtNewSimdBinOpNode(GT_AND, type, op2, vecCon2, simdBaseJitType, simdSize);
                tmp3 = gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_AVX2_PackUnsignedSaturate, CORINFO_TYPE_USHORT,
                                                simdSize);

                CorInfoType permuteBaseJitType = (simdBaseType == TYP_BYTE) ? CORINFO_TYPE_LONG : CORINFO_TYPE_ULONG;
                return gtNewSimdHWIntrinsicNode(type, tmp3, gtNewIconNode(SHUFFLE_WYZX), NI_AVX2_Permute4x64,
                                                permuteBaseJitType, simdSize);
            }

            case TYP_INT:
            case TYP_UINT:
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

                // op1 = Elements 0, 1 | 2, 3;        0L, 0U, 1L, 1U | 2L, 2U, 3L, 3U
                // op2 = Elements 4, 5 | 6, 7;        4L, 4U, 5L, 5U | 6L, 6U, 7L, 7U
                //
                // tmp1 = Elements 0L, 4L, 0U, 4U | 2L, 6L, 2U, 6U
                // tmp2 = Elements 1L, 5L, 1U, 5U | 3L, 7L, 3U, 7U
                // tmp3 = Elements 0L, 1L, 4L, 5L | 2L, 3L, 6L, 7L
                // return Elements 0L, 1L, 2L, 3L | 4L, 5L, 6L, 7L
                //
                // var tmp1 = Avx2.UnpackLow(op1, op2);
                // var tmp2 = Avx2.UnpackHigh(op1, op2);
                // var tmp3 = Avx2.UnpackLow(tmp1, tmp2);
                // return Avx2.Permute4x64(tmp3.AsUInt64(), SHUFFLE_WYZX).AsUInt32();

                CorInfoType opBaseJitType = (simdBaseType == TYP_INT) ? CORINFO_TYPE_LONG : CORINFO_TYPE_ULONG;

                GenTree* op1Dup = fgMakeMultiUse(&op1);
                GenTree* op2Dup = fgMakeMultiUse(&op2);

                tmp1 = gtNewSimdHWIntrinsicNode(type, op1, op2, NI_AVX2_UnpackLow, simdBaseJitType, simdSize);
                tmp2 = gtNewSimdHWIntrinsicNode(type, op1Dup, op2Dup, NI_AVX2_UnpackHigh, simdBaseJitType, simdSize);
                tmp3 = gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_AVX2_UnpackLow, simdBaseJitType, simdSize);

                return gtNewSimdHWIntrinsicNode(type, tmp3, gtNewIconNode(SHUFFLE_WYZX), NI_AVX2_Permute4x64,
                                                opBaseJitType, simdSize);
            }

            case TYP_FLOAT:
            {
                // op1 = Elements 0, 1 | 2, 3
                // op2 = Elements 4, 5 | 6, 7
                //
                // tmp1 = Elements 0, 1, 2, 3 | -, -, -, -
                // tmp1 = Elements 4, 5, 6, 7
                // return Elements 0, 1, 2, 3 | 4, 5, 6, 7
                //
                // var tmp1 = Avx.ConvertToVector128Single(op1).ToVector256Unsafe();
                // var tmp2 = Avx.ConvertToVector128Single(op2);
                // return tmp1.WithUpper(tmp2);

                CorInfoType opBaseJitType = CORINFO_TYPE_DOUBLE;

                tmp1 =
                    gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_AVX_ConvertToVector128Single, opBaseJitType, simdSize);
                tmp2 =
                    gtNewSimdHWIntrinsicNode(TYP_SIMD16, op2, NI_AVX_ConvertToVector128Single, opBaseJitType, simdSize);

                tmp1 = gtNewSimdHWIntrinsicNode(type, tmp1, NI_Vector128_ToVector256Unsafe, simdBaseJitType, 16);
                return gtNewSimdWithUpperNode(type, tmp1, tmp2, simdBaseJitType, simdSize);
            }

            default:
            {
                unreached();
            }
        }
    }
    else
    {
        assert(simdSize == 16);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                // op1 = Elements 0, 1, 2, 3, 4, 5, 6, 7; 0L, 0U, 1L, 1U, 2L, 2U, 3L, 3U, 4L, 4U, 5L, 5U, 6L, 6U, 7L, 7U
                // op2 = Elements 8, 9, A, B, C, D, E, F; 8L, 8U, 9L, 9U, AL, AU, BL, BU, CL, CU, DL, DU, EL, EU, FL, FU
                //
                // tmp2 = Elements 0L, --, 1L, --, 2L, --, 3L, --, 4L, --, 5L, --, 6L, --, 7L, --
                // tmp3 = Elements 8L, --, 9L, --, AL, --, BL, --, CL, --, DL, --, EL, --, FL, --
                // return Elements 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L, 8L, 9L, AL, BL, CL, DL, EL, FL
                //
                // var vcns = Vector128.Create((ushort)(0x00FF)).AsSByte();
                // var tmp1 = Sse2.And(op1.AsSByte(), vcns);
                // var tmp2 = Sse2.And(op2.AsSByte(), vcns);
                // return Sse2.PackUnsignedSaturate(tmp1, tmp2).As<T>();

                GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                for (unsigned i = 0; i < (simdSize / 8); i++)
                {
                    vecCon1->gtSimdVal.u64[i] = 0x00FF00FF00FF00FF;
                }

                GenTree* vecCon2 = gtCloneExpr(vecCon1);

                // Validate we can't use AVX512F_VL_TernaryLogic here
                assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                tmp1 = gtNewSimdBinOpNode(GT_AND, type, op1, vecCon1, simdBaseJitType, simdSize);
                tmp2 = gtNewSimdBinOpNode(GT_AND, type, op2, vecCon2, simdBaseJitType, simdSize);

                return gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE2_PackUnsignedSaturate, CORINFO_TYPE_UBYTE,
                                                simdSize);
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                // op1 = Elements 0, 1, 2, 3;      0L, 0U, 1L, 1U, 2L, 2U, 3L, 3U
                // op2 = Elements 4, 5, 6, 7;      4L, 4U, 5L, 5U, 6L, 6U, 7L, 7U
                //
                // ...
                if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
                {
                    // ...
                    //
                    // tmp2 = Elements 0L, --, 1L, --, 2L, --, 3L, --
                    // tmp3 = Elements 4L, --, 5L, --, 6L, --, 7L, --
                    // return Elements 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L
                    //
                    // var vcns = Vector128.Create(0x0000FFFF).AsInt16();
                    // var tmp1 = Sse2.And(op1.AsInt16(), vcns);
                    // var tmp2 = Sse2.And(op2.AsInt16(), vcns);
                    // return Sse2.PackUnsignedSaturate(tmp1, tmp2).As<T>();

                    GenTreeVecCon* vecCon1 = gtNewVconNode(type);

                    for (unsigned i = 0; i < (simdSize / 8); i++)
                    {
                        vecCon1->gtSimdVal.u64[i] = 0x0000FFFF0000FFFF;
                    }

                    GenTree* vecCon2 = gtCloneExpr(vecCon1);

                    // Validate we can't use AVX512F_VL_TernaryLogic here
                    assert(!compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                    tmp1 = gtNewSimdBinOpNode(GT_AND, type, op1, vecCon1, simdBaseJitType, simdSize);
                    tmp2 = gtNewSimdBinOpNode(GT_AND, type, op2, vecCon2, simdBaseJitType, simdSize);

                    return gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE41_PackUnsignedSaturate,
                                                    CORINFO_TYPE_USHORT, simdSize);
                }
                else
                {
                    // ...
                    //
                    // tmp1 = Elements 0L, 4L, 0U, 4U, 1L, 5L, 1U, 5U
                    // tmp2 = Elements 2L, 6L, 2U, 6U, 3L, 7L, 3U, 7U
                    // tmp3 = Elements 0L, 2L, 4L, 6L, 0U, 2U, 4U, 6U
                    // tmp4 = Elements 1L, 3L, 5L, 7L, 1U, 3U, 5U, 7U
                    // return Elements 0L, 1L, 2L, 3L, 4L, 5L, 6L, 7L
                    //
                    // var tmp1 = Sse2.UnpackLow(op1.AsUInt16(), op2.AsUInt16());
                    // var tmp2 = Sse2.UnpackHigh(op1.AsUInt16(), op2.AsUInt16());
                    // var tmp3 = Sse2.UnpackLow(tmp1, tmp2);
                    // var tmp4 = Sse2.UnpackHigh(tmp1, tmp2);
                    // return Sse2.UnpackLow(tmp3, tmp4).As<T>();

                    GenTree* op1Dup = fgMakeMultiUse(&op1);
                    GenTree* op2Dup = fgMakeMultiUse(&op2);

                    tmp1 = gtNewSimdHWIntrinsicNode(type, op1, op2, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
                    tmp2 =
                        gtNewSimdHWIntrinsicNode(type, op1Dup, op2Dup, NI_SSE2_UnpackHigh, simdBaseJitType, simdSize);

                    GenTree* tmp1Dup = fgMakeMultiUse(&tmp1);
                    GenTree* tmp2Dup = fgMakeMultiUse(&tmp2);

                    tmp3 = gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
                    tmp4 =
                        gtNewSimdHWIntrinsicNode(type, tmp1Dup, tmp2Dup, NI_SSE2_UnpackHigh, simdBaseJitType, simdSize);

                    return gtNewSimdHWIntrinsicNode(type, tmp3, tmp4, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
                }
            }

            case TYP_INT:
            case TYP_UINT:
            {
                // op1 = Elements 0, 1;      0L, 0U, 1L, 1U
                // op2 = Elements 2, 3;      2L, 2U, 3L, 3U
                //
                // tmp1 = Elements 0L, 2L, 0U, 2U
                // tmp2 = Elements 1L, 3L, 1U, 3U
                // return Elements 0L, 1L, 2L, 3L
                //
                // var tmp1 = Sse2.UnpackLow(op1.AsUInt32(), op2.AsUInt32());
                // var tmp2 = Sse2.UnpackHigh(op1.AsUInt32(), op2.AsUInt32());
                // return Sse2.UnpackLow(tmp1, tmp2).As<T>();

                GenTree* op1Dup = fgMakeMultiUse(&op1);
                GenTree* op2Dup = fgMakeMultiUse(&op2);

                tmp1 = gtNewSimdHWIntrinsicNode(type, op1, op2, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
                tmp2 = gtNewSimdHWIntrinsicNode(type, op1Dup, op2Dup, NI_SSE2_UnpackHigh, simdBaseJitType, simdSize);

                return gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
            }

            case TYP_FLOAT:
            {
                // op1 = Elements 0, 1
                // op2 = Elements 2, 3
                //
                // tmp1 = Elements 0, 1, -, -
                // tmp1 = Elements 2, 3, -, -
                // return Elements 0, 1, 2, 3
                //
                // var tmp1 = Sse2.ConvertToVector128Single(op1);
                // var tmp2 = Sse2.ConvertToVector128Single(op2);
                // return Sse.MoveLowToHigh(tmp1, tmp2);

                CorInfoType opBaseJitType = CORINFO_TYPE_DOUBLE;

                tmp1 = gtNewSimdHWIntrinsicNode(type, op1, NI_SSE2_ConvertToVector128Single, opBaseJitType, simdSize);
                tmp2 = gtNewSimdHWIntrinsicNode(type, op2, NI_SSE2_ConvertToVector128Single, opBaseJitType, simdSize);

                return gtNewSimdHWIntrinsicNode(type, tmp1, tmp2, NI_SSE_MoveLowToHigh, simdBaseJitType, simdSize);
            }

            default:
            {
                unreached();
            }
        }
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 16)
    {
        if (varTypeIsFloating(simdBaseType))
        {
            // var tmp1 = AdvSimd.Arm64.ConvertToSingleLower(op1);
            // return AdvSimd.Arm64.ConvertToSingleUpper(tmp1, op2);

            tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_ConvertToSingleLower, simdBaseJitType, 8);
            return gtNewSimdHWIntrinsicNode(type, tmp1, op2, NI_AdvSimd_Arm64_ConvertToSingleUpper, simdBaseJitType,
                                            simdSize);
        }
        else
        {
            // var tmp1 = AdvSimd.ExtractNarrowingLower(op1);
            // return AdvSimd.ExtractNarrowingUpper(tmp1, op2);

            tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_ExtractNarrowingLower, simdBaseJitType, 8);
            return gtNewSimdHWIntrinsicNode(type, tmp1, op2, NI_AdvSimd_ExtractNarrowingUpper, simdBaseJitType,
                                            simdSize);
        }
    }
    else if (varTypeIsFloating(simdBaseType))
    {
        // var tmp1 = op1.ToVector128Unsafe();
        // var tmp2 = AdvSimd.InsertScalar(tmp1, op2);
        // return AdvSimd.Arm64.ConvertToSingleLower(tmp2);

        CorInfoType tmp2BaseJitType = CORINFO_TYPE_DOUBLE;

        tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector64_ToVector128Unsafe, simdBaseJitType, simdSize);
        tmp2 = gtNewSimdWithUpperNode(TYP_SIMD16, tmp1, op2, tmp2BaseJitType, 16);

        return gtNewSimdHWIntrinsicNode(type, tmp2, NI_AdvSimd_Arm64_ConvertToSingleLower, simdBaseJitType, simdSize);
    }
    else
    {
        // var tmp1 = op1.ToVector128Unsafe();
        // var tmp2 = AdvSimd.InsertScalar(tmp1.AsUInt64(), 1, op2.AsUInt64()).As<T>(); - signed integer use int64,
        // unsigned integer use uint64
        // return AdvSimd.ExtractNarrowingLower(tmp2);

        CorInfoType tmp2BaseJitType = varTypeIsSigned(simdBaseType) ? CORINFO_TYPE_LONG : CORINFO_TYPE_ULONG;

        tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector64_ToVector128Unsafe, simdBaseJitType, simdSize);
        tmp2 = gtNewSimdWithUpperNode(TYP_SIMD16, tmp1, op2, tmp2BaseJitType, 16);

        return gtNewSimdHWIntrinsicNode(type, tmp2, NI_AdvSimd_ExtractNarrowingLower, simdBaseJitType, simdSize);
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdShuffleNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));
    assert(op2->IsVectorConst());

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    if (op2->IsVectorAllBitsSet())
    {
        // AllBitsSet represents indices that are always "out of range" which means zero should be
        // selected for every element. We can special-case this down to just returning a zero node
        return gtNewZeroConNode(type);
    }

    if (op2->IsVectorZero())
    {
        // TODO-XARCH-CQ: Zero represents indices that select the first  element of op1 each time. We can simplify
        // this down to basically a broadcast equivalent.
    }

    GenTree*             retNode = nullptr;
    GenTreeIntConCommon* cnsNode = nullptr;

    size_t elementSize  = genTypeSize(simdBaseType);
    size_t elementCount = simdSize / elementSize;

#if defined(TARGET_XARCH)
    uint8_t  control   = 0;
    bool     crossLane = false;
    bool     needsZero = varTypeIsSmall(simdBaseType) && (simdSize <= 16);
    uint64_t value     = 0;
    simd_t   vecCns    = {};
    simd_t   mskCns    = {};

    for (size_t index = 0; index < elementCount; index++)
    {
        value = op2->GetIntegralVectorConstElement(index, simdBaseType);

        if (value < elementCount)
        {
            if (simdSize == 32)
            {
                // Most of the 256-bit shuffle/permute instructions operate as if
                // the inputs were 2x 128-bit values. If the selected indices cross
                // the respective 128-bit "lane" we may need to specialize the codegen

                if (index < (elementCount / 2))
                {
                    crossLane |= (value >= (elementCount / 2));
                }
                else
                {
                    crossLane |= (value < (elementCount / 2));
                }
            }

            // Setting the control for byte/sbyte and short/ushort is unnecessary
            // and will actually compute an incorrect control word. But it simplifies
            // the overall logic needed here and will remain unused.

            control |= (value << (index * (elementCount / 2)));

            // When Ssse3 is supported, we may need vecCns to accurately select the relevant
            // bytes if some index is outside the valid range. Since x86/x64 is little-endian
            // we can simplify this down to a for loop that scales the value and selects count
            // sequential bytes.

            for (uint32_t i = 0; i < elementSize; i++)
            {
                vecCns.u8[(index * elementSize) + i] = (uint8_t)((value * elementSize) + i);

                // When Ssse3 is not supported, we need to adjust the constant to be AllBitsSet
                // so that we can emit a ConditionalSelect(op2, retNode, zeroNode).

                mskCns.u8[(index * elementSize) + i] = 0xFF;
            }
        }
        else
        {
            needsZero = true;

            // When Ssse3 is supported, we may need vecCns to accurately select the relevant
            // bytes if some index is outside the valid range. We can do this by just zeroing
            // out each byte in the element. This only requires the most significant bit to be
            // set, but we use 0xFF instead since that will be the equivalent of AllBitsSet

            for (uint32_t i = 0; i < elementSize; i++)
            {
                vecCns.u8[(index * elementSize) + i] = 0xFF;

                // When Ssse3 is not supported, we need to adjust the constant to be Zero
                // so that we can emit a ConditionalSelect(op2, retNode, zeroNode).

                mskCns.u8[(index * elementSize) + i] = 0x00;
            }
        }
    }

    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));

        if ((varTypeIsByte(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX512VBMI_VL)) ||
            (varTypeIsShort(simdBaseType) && !compOpportunisticallyDependsOn(InstructionSet_AVX512BW_VL)))
        {
            if (crossLane)
            {
                // TODO-XARCH-CQ: We should emulate cross-lane shuffling for byte/sbyte and short/ushort
                unreached();
            }

            // If we aren't crossing lanes, then we can decompose the byte/sbyte
            // and short/ushort operations into 2x 128-bit operations

            // We want to build what is essentially the following managed code:
            //     var op1Lower = op1.GetLower();
            //     op1Lower = Ssse3.Shuffle(op1Lower, Vector128.Create(...));
            //
            //     var op1Upper = op1.GetUpper();
            //     op1Upper = Ssse3.Shuffle(op1Upper, Vector128.Create(...));
            //
            //     return Vector256.Create(op1Lower, op1Upper);

            simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

            GenTree* op1Dup   = fgMakeMultiUse(&op1);
            GenTree* op1Lower = gtNewSimdGetLowerNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);

            op2                          = gtNewVconNode(TYP_SIMD16);
            op2->AsVecCon()->gtSimd16Val = vecCns.v128[0];

            op1Lower = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1Lower, op2, NI_SSSE3_Shuffle, simdBaseJitType, 16);

            GenTree* op1Upper = gtNewSimdGetUpperNode(TYP_SIMD16, op1Dup, simdBaseJitType, simdSize);

            op2                          = gtNewVconNode(TYP_SIMD16);
            op2->AsVecCon()->gtSimd16Val = vecCns.v128[1];

            op1Upper = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1Upper, op2, NI_SSSE3_Shuffle, simdBaseJitType, 16);

            return gtNewSimdWithUpperNode(type, op1Lower, op1Upper, simdBaseJitType, simdSize);
        }

        if (elementSize == 4)
        {
            for (uint32_t i = 0; i < elementCount; i++)
            {
                vecCns.u32[i] = (uint8_t)(vecCns.u8[i * elementSize] / elementSize);
            }

            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode = gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX2_PermuteVar8x32, simdBaseJitType, simdSize);
        }
        else if (elementSize == 2)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512BW_VL));
            for (uint32_t i = 0; i < elementCount; i++)
            {
                vecCns.u16[i] = (uint8_t)(vecCns.u8[i * elementSize] / elementSize);
            }

            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode =
                gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512BW_VL_PermuteVar16x16, simdBaseJitType, simdSize);
        }
        else if (elementSize == 1)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512VBMI_VL));
            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode =
                gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512VBMI_VL_PermuteVar32x8, simdBaseJitType, simdSize);
        }
        else
        {
            assert(elementSize == 8);

            cnsNode = gtNewIconNode(control);
            retNode = gtNewSimdHWIntrinsicNode(type, op1, cnsNode, NI_AVX2_Permute4x64, simdBaseJitType, simdSize);
        }
    }
    else if (simdSize == 64)
    {
        assert(IsBaselineVector512IsaSupportedDebugOnly());
        if (elementSize == 4)
        {
            for (uint32_t i = 0; i < elementCount; i++)
            {
                vecCns.u32[i] = (uint8_t)(vecCns.u8[i * elementSize] / elementSize);
            }

            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode = gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512F_PermuteVar16x32, simdBaseJitType, simdSize);
        }
        else if (elementSize == 2)
        {
            for (uint32_t i = 0; i < elementCount; i++)
            {
                vecCns.u16[i] = (uint8_t)(vecCns.u8[i * elementSize] / elementSize);
            }

            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode = gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512BW_PermuteVar32x16, simdBaseJitType, simdSize);
        }
        else if (elementSize == 1)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_AVX512VBMI));
            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode = gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512VBMI_PermuteVar64x8, simdBaseJitType, simdSize);
        }
        else
        {
            assert(elementSize == 8);

            for (uint32_t i = 0; i < elementCount; i++)
            {
                vecCns.u64[i] = (uint8_t)(vecCns.u8[i * elementSize] / elementSize);
            }

            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = vecCns;

            // swap the operands to match the encoding requirements
            retNode = gtNewSimdHWIntrinsicNode(type, op2, op1, NI_AVX512F_PermuteVar8x64, simdBaseJitType, simdSize);
        }
        assert(retNode != nullptr);

        // TODO-XArch-AVX512: Switch to VPERMI2*
        if (needsZero)
        {
            op2                        = gtNewVconNode(type);
            op2->AsVecCon()->gtSimdVal = mskCns;
            retNode                    = gtNewSimdBinOpNode(GT_AND, type, op2, retNode, simdBaseJitType, simdSize);
        }

        return retNode;
    }
    else
    {
        if (needsZero && compOpportunisticallyDependsOn(InstructionSet_SSSE3))
        {
            simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

            op2                          = gtNewVconNode(type);
            op2->AsVecCon()->gtSimd16Val = vecCns.v128[0];

            return gtNewSimdHWIntrinsicNode(type, op1, op2, NI_SSSE3_Shuffle, simdBaseJitType, simdSize);
        }

        if (varTypeIsLong(simdBaseType))
        {
            // TYP_LONG and TYP_ULONG don't have their own shuffle/permute instructions and so we'll
            // just utilize the path for TYP_DOUBLE for simplicity. We could alternatively break this
            // down into a TYP_INT or TYP_UINT based shuffle, but that's additional complexity for no
            // real benefit since shuffle gets its own port rather than using the fp specific ports.

            simdBaseJitType = CORINFO_TYPE_DOUBLE;
            simdBaseType    = TYP_DOUBLE;
        }

        cnsNode = gtNewIconNode(control);

        if (varTypeIsIntegral(simdBaseType))
        {
            retNode = gtNewSimdHWIntrinsicNode(type, op1, cnsNode, NI_SSE2_Shuffle, simdBaseJitType, simdSize);
        }
        else if (compOpportunisticallyDependsOn(InstructionSet_AVX))
        {
            retNode = gtNewSimdHWIntrinsicNode(type, op1, cnsNode, NI_AVX_Permute, simdBaseJitType, simdSize);
        }
        else
        {
            // for double we need SSE2, but we can't use the integral path ^ because we still need op1Dup here
            NamedIntrinsic ni     = simdBaseType == TYP_DOUBLE ? NI_SSE2_Shuffle : NI_SSE_Shuffle;
            GenTree*       op1Dup = fgMakeMultiUse(&op1);
            retNode               = gtNewSimdHWIntrinsicNode(type, op1, op1Dup, cnsNode, ni, simdBaseJitType, simdSize);
        }
    }

    assert(retNode != nullptr);

    if (needsZero)
    {
        assert((simdSize == 32) || !compIsaSupportedDebugOnly(InstructionSet_SSSE3));

        op2                        = gtNewVconNode(type);
        op2->AsVecCon()->gtSimdVal = mskCns;
        retNode                    = gtNewSimdBinOpNode(GT_AND, type, op2, retNode, simdBaseJitType, simdSize);
    }

    return retNode;
#elif defined(TARGET_ARM64)
    uint64_t value  = 0;
    simd_t   vecCns = {};

    for (size_t index = 0; index < elementCount; index++)
    {
        value = op2->GetIntegralVectorConstElement(index, simdBaseType);

        if (value < elementCount)
        {
            for (uint32_t i = 0; i < elementSize; i++)
            {
                vecCns.u8[(index * elementSize) + i] = (uint8_t)((value * elementSize) + i);
            }
        }
        else
        {
            for (uint32_t i = 0; i < elementSize; i++)
            {
                vecCns.u8[(index * elementSize) + i] = 0xFF;
            }
        }
    }

    NamedIntrinsic lookupIntrinsic = NI_AdvSimd_VectorTableLookup;

    if (simdSize == 16)
    {
        lookupIntrinsic = NI_AdvSimd_Arm64_VectorTableLookup;

        op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_Vector64_ToVector128, simdBaseJitType, simdSize);
    }

    // VectorTableLookup is only valid on byte/sbyte
    simdBaseJitType = varTypeIsUnsigned(simdBaseType) ? CORINFO_TYPE_UBYTE : CORINFO_TYPE_BYTE;

    op2                        = gtNewVconNode(type);
    op2->AsVecCon()->gtSimdVal = vecCns;

    return gtNewSimdHWIntrinsicNode(type, op1, op2, lookupIntrinsic, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdSqrtNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsFloating(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_AVX_Sqrt;
    }
    else if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        intrinsic = NI_AVX512F_Sqrt;
    }
    else if (simdBaseType == TYP_FLOAT)
    {
        intrinsic = NI_SSE_Sqrt;
    }
    else
    {
        intrinsic = NI_SSE2_Sqrt;
    }
#elif defined(TARGET_ARM64)
    if ((simdSize == 8) && (simdBaseType == TYP_DOUBLE))
    {
        intrinsic = NI_AdvSimd_SqrtScalar;
    }
    else
    {
        intrinsic = NI_AdvSimd_Arm64_Sqrt;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdStoreNode: Creates a new simd Store node
//
//  Arguments:
//    op1                 - The address to which op2 is stored
//    op2                 - The SIMD value to be stored at op1
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created Store node
//
GenTree* Compiler::gtNewSimdStoreNode(GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(op1 != nullptr);
    assert(op2 != nullptr);

    assert(varTypeIsSIMD(op2));
    assert(getSIMDTypeForSize(simdSize) == op2->TypeGet());

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    return gtNewStoreIndNode(op2->TypeGet(), op1, op2);
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdStoreAlignedNode: Creates a new simd StoreAligned node
//
//  Arguments:
//    op1                 - The address to which op2 is stored
//    op2                 - The SIMD value to be stored at op1
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created StoreAligned node
//
GenTree* Compiler::gtNewSimdStoreAlignedNode(GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
#if defined(TARGET_XARCH)
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(op1 != nullptr);
    assert(op2 != nullptr);

    assert(varTypeIsSIMD(op2));
    assert(getSIMDTypeForSize(simdSize) == op2->TypeGet());

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_AVX_StoreAligned;
    }
    else if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        intrinsic = NI_AVX512F_StoreAligned;
    }
    else if (simdBaseType != TYP_FLOAT)
    {
        intrinsic = NI_SSE2_StoreAligned;
    }
    else
    {
        intrinsic = NI_SSE_StoreAligned;
    }

    return gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, intrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    // ARM64 doesn't have aligned stores, but aligned stores are only validated to be
    // aligned when optimizations are disable, so only skip the intrinsic handling
    // if optimizations are enabled

    assert(opts.OptimizationEnabled());
    return gtNewSimdStoreNode(op1, op2, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdStoreNonTemporalNode: Creates a new simd StoreNonTemporal node
//
//  Arguments:
//    op1                 - The address to which op2 is stored
//    op2                 - The SIMD value to be stored at op1
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created StoreNonTemporal node
//
GenTree* Compiler::gtNewSimdStoreNonTemporalNode(GenTree*    op1,
                                                 GenTree*    op2,
                                                 CorInfoType simdBaseJitType,
                                                 unsigned    simdSize)
{
#if defined(TARGET_XARCH)
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(op1 != nullptr);
    assert(op2 != nullptr);

    assert(varTypeIsSIMD(op2));
    assert(getSIMDTypeForSize(simdSize) == op2->TypeGet());

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    if (simdSize == 64)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
        intrinsic = NI_AVX512F_StoreAlignedNonTemporal;
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_AVX_StoreAlignedNonTemporal;
    }
    else if (simdBaseType != TYP_FLOAT)
    {
        intrinsic = NI_SSE2_StoreAlignedNonTemporal;
    }
    else
    {
        intrinsic = NI_SSE_StoreAlignedNonTemporal;
    }

    return gtNewSimdHWIntrinsicNode(TYP_VOID, op1, op2, intrinsic, simdBaseJitType, simdSize);
#elif defined(TARGET_ARM64)
    // ARM64 doesn't have aligned stores, but aligned stores are only validated to be
    // aligned when optimizations are disable, so only skip the intrinsic handling
    // if optimizations are enabled

    assert(opts.OptimizationEnabled());
    return gtNewSimdStoreNode(op1, op2, simdBaseJitType, simdSize);
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdSumNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    var_types simdType = getSIMDTypeForSize(simdSize);
    assert(varTypeIsSIMD(simdType));

    assert(op1 != nullptr);
    assert(op1->TypeIs(simdType));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;
    GenTree*       tmp       = nullptr;

#if defined(TARGET_XARCH)

    if (simdSize == 64)
    {
        assert(IsBaselineVector512IsaSupportedDebugOnly());
        GenTree* op1Dup = fgMakeMultiUse(&op1);
        op1             = gtNewSimdGetUpperNode(TYP_SIMD32, op1, simdBaseJitType, simdSize);
        op1Dup          = gtNewSimdGetLowerNode(TYP_SIMD32, op1Dup, simdBaseJitType, simdSize);
        simdSize        = simdSize / 2;
        op1             = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD32, op1, op1Dup, simdBaseJitType, simdSize);
    }

    if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX2));
        GenTree* op1Dup = fgMakeMultiUse(&op1);
        op1             = gtNewSimdGetUpperNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);
        op1Dup          = gtNewSimdGetLowerNode(TYP_SIMD16, op1Dup, simdBaseJitType, simdSize);
        simdSize        = simdSize / 2;
        op1             = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, op1, op1Dup, simdBaseJitType, simdSize);
    }

    assert(simdSize == 16);

    if (varTypeIsFloating(simdBaseType))
    {
        if (simdBaseType == TYP_FLOAT)
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));
            GenTree* op1Shuffled = fgMakeMultiUse(&op1);
            if (compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                // The permute below gives us  [0, 1, 2, 3] -> [1, 0, 3, 2]
                op1 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode((int)0b10110001, TYP_INT), NI_AVX_Permute,
                                               simdBaseJitType, simdSize);
                // The add below now results in [0 + 1, 1 + 0, 2 + 3, 3 + 2]
                op1         = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, op1, op1Shuffled, simdBaseJitType, simdSize);
                op1Shuffled = fgMakeMultiUse(&op1);
                // The permute below gives us  [0 + 1, 1 + 0, 2 + 3, 3 + 2] -> [2 + 3, 3 + 2, 0 + 1, 1 + 0]
                op1 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode((int)0b01001110, TYP_INT), NI_AVX_Permute,
                                               simdBaseJitType, simdSize);
            }
            else
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_SSE));
                // The shuffle below gives us  [0, 1, 2, 3] -> [1, 0, 3, 2]
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op1Shuffled, gtNewIconNode((int)0b10110001, TYP_INT),
                                               NI_SSE_Shuffle, simdBaseJitType, simdSize);
                op1Shuffled = fgMakeMultiUse(&op1Shuffled);
                // The add below now results in [0 + 1, 1 + 0, 2 + 3, 3 + 2]
                op1         = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, op1, op1Shuffled, simdBaseJitType, simdSize);
                op1Shuffled = fgMakeMultiUse(&op1);
                // The shuffle below gives us  [0 + 1, 1 + 0, 2 + 3, 3 + 2] -> [2 + 3, 3 + 2, 0 + 1, 1 + 0]
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op1Shuffled, gtNewIconNode((int)0b01001110, TYP_INT),
                                               NI_SSE_Shuffle, simdBaseJitType, simdSize);
                op1Shuffled = fgMakeMultiUse(&op1Shuffled);
            }
            // Finally adding the results gets us [(0 + 1) + (2 + 3), (1 + 0) + (3 + 2), (2 + 3) + (0 + 1), (3 + 2) + (1
            // + 0)]
            op1 = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, op1, op1Shuffled, simdBaseJitType, simdSize);
            return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);
        }
        else
        {
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));
            GenTree* op1Shuffled = fgMakeMultiUse(&op1);
            if (compOpportunisticallyDependsOn(InstructionSet_AVX))
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                // The permute below gives us  [0, 1] -> [1, 0]
                op1 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode((int)0b0001, TYP_INT), NI_AVX_Permute,
                                               simdBaseJitType, simdSize);
            }
            else
            {
                // The shuffle below gives us  [0, 1] -> [1, 0]
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, op1Shuffled, gtNewIconNode((int)0b0001, TYP_INT),
                                               NI_SSE2_Shuffle, simdBaseJitType, simdSize);
                op1Shuffled = fgMakeMultiUse(&op1Shuffled);
            }
            // Finally adding the results gets us [0 + 1, 1 + 0]
            op1 = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, op1, op1Shuffled, simdBaseJitType, simdSize);
            return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);
        }
    }

    unsigned vectorLength = getSIMDVectorLength(simdSize, simdBaseType);
    int      shiftCount   = genLog2(vectorLength);
    int      typeSize     = genTypeSize(simdBaseType);
    int      shiftVal     = (typeSize * vectorLength) / 2;

    // The reduced sum is calculated for integer values using a combination of shift + add
    // For e.g. consider a 32 bit integer. This means we have 4 values in a XMM register
    // After the first shift + add -> [(0 + 2), (1 + 3), ...]
    // After the second shift + add -> [(0 + 2 + 1 + 3), ...]
    GenTree* opShifted = nullptr;
    while (shiftVal >= typeSize)
    {
        tmp       = fgMakeMultiUse(&op1);
        opShifted = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, gtNewIconNode(shiftVal, TYP_INT),
                                             NI_SSE2_ShiftRightLogical128BitLane, simdBaseJitType, simdSize);
        op1      = gtNewSimdBinOpNode(GT_ADD, TYP_SIMD16, opShifted, tmp, simdBaseJitType, simdSize);
        shiftVal = shiftVal / 2;
    }

    return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);

#elif defined(TARGET_ARM64)
    switch (simdBaseType)
    {
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        {
            tmp = gtNewSimdHWIntrinsicNode(simdType, op1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, simdSize);
            return gtNewSimdToScalarNode(type, tmp, simdBaseJitType, 8);
        }

        case TYP_INT:
        case TYP_UINT:
        {
            if (simdSize == 8)
            {
                tmp = fgMakeMultiUse(&op1);
                tmp = gtNewSimdHWIntrinsicNode(simdType, op1, tmp, NI_AdvSimd_AddPairwise, simdBaseJitType, simdSize);
            }
            else
            {
                tmp = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, NI_AdvSimd_Arm64_AddAcross, simdBaseJitType, 16);
            }
            return gtNewSimdToScalarNode(type, tmp, simdBaseJitType, 8);
        }

        case TYP_FLOAT:
        {
            if (simdSize == 8)
            {
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddPairwiseScalar, simdBaseJitType,
                                               simdSize);
            }
            else
            {
                unsigned vectorLength = getSIMDVectorLength(simdSize, simdBaseType);
                int      haddCount    = genLog2(vectorLength);

                for (int i = 0; i < haddCount; i++)
                {
                    tmp = fgMakeMultiUse(&op1);
                    op1 = gtNewSimdHWIntrinsicNode(simdType, op1, tmp, NI_AdvSimd_Arm64_AddPairwise, simdBaseJitType,
                                                   simdSize);
                }
            }
            return gtNewSimdToScalarNode(type, op1, simdBaseJitType, simdSize);
        }

        case TYP_DOUBLE:
        case TYP_LONG:
        case TYP_ULONG:
        {
            if (simdSize == 16)
            {
                op1 = gtNewSimdHWIntrinsicNode(TYP_SIMD8, op1, NI_AdvSimd_Arm64_AddPairwiseScalar, simdBaseJitType,
                                               simdSize);
            }
            return gtNewSimdToScalarNode(type, op1, simdBaseJitType, 8);
        }
        default:
        {
            unreached();
        }
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

#if defined(TARGET_XARCH)
//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdTernaryLogicNode: Creates a new simd TernaryLogic node
//
//  Arguments:
//    type                - The return type of SIMD node being created
//    op1                 - The value of the first operand: 'A'
//    op2                 - The value of the second operand: 'B'
//    op3                 - The value of the third operand: 'C'
//    op4                 - The constant control byte used to determine how 'A', 'B', and 'C' are manipulated
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic
//    simdSize            - The size of the SIMD type of the intrinsic
//
// Returns:
//    The created TernaryLogic node that performs the specified operation on 'A', 'B', and 'C'.
//
GenTree* Compiler::gtNewSimdTernaryLogicNode(var_types   type,
                                             GenTree*    op1,
                                             GenTree*    op2,
                                             GenTree*    op3,
                                             GenTree*    op4,
                                             CorInfoType simdBaseJitType,
                                             unsigned    simdSize)
{
    assert(IsBaselineVector512IsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    assert(op2 != nullptr);
    assert(op2->TypeIs(type));

    assert(op3 != nullptr);
    assert(op3->TypeIs(type));

    assert(op4 != nullptr);
    assert(genActualType(op4) == TYP_INT);

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    if (simdSize == 64)
    {
        intrinsic = NI_AVX512F_TernaryLogic;
    }
    else
    {
        assert((simdSize == 16) || (simdSize == 32));
        intrinsic = NI_AVX512F_VL_TernaryLogic;
    }

    return gtNewSimdHWIntrinsicNode(type, op1, op2, op3, op4, intrinsic, simdBaseJitType, simdSize);
}
#endif // TARGET_XARCH

//----------------------------------------------------------------------------------------------
// Compiler::gtNewSimdToScalarNode: Creates a new simd ToScalar node.
//
//  Arguments:
//    type                - The return type of SIMD node being created.
//    op1                 - The SIMD operand.
//    simdBaseJitType     - The base JIT type of SIMD type of the intrinsic.
//    simdSize            - The size of the SIMD type of the intrinsic.
//
// Returns:
//    The created node that has the ToScalar implementation.
//
GenTree* Compiler::gtNewSimdToScalarNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());
    assert(varTypeIsArithmetic(type));

    assert(op1 != nullptr);
    assert(varTypeIsSIMD(op1));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

#ifdef TARGET_XARCH
#if defined(TARGET_X86)
    if (varTypeIsLong(simdBaseType))
    {
        // We need SSE41 to handle long, use software fallback
        assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));

        // Create a GetElement node which handles decomposition
        GenTree* op2 = gtNewIconNode(0);
        return gtNewSimdGetElementNode(type, op1, op2, simdBaseJitType, simdSize);
    }
#endif // TARGET_X86

    if (simdSize == 64)
    {
        assert(IsBaselineVector512IsaSupportedDebugOnly());
        intrinsic = NI_Vector512_ToScalar;
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        intrinsic = NI_Vector256_ToScalar;
    }
    else
    {
        intrinsic = NI_Vector128_ToScalar;
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 8)
    {
        intrinsic = NI_Vector64_ToScalar;
    }
    else
    {
        intrinsic = NI_Vector128_ToScalar;
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    assert(intrinsic != NI_Illegal);
    return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdUnOpNode(
    genTreeOps op, var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;
    GenTree*       op2       = nullptr;
    GenTree*       op3       = nullptr;

    switch (op)
    {
#if defined(TARGET_XARCH)
        case GT_NEG:
        {
            if (simdSize == 32)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                assert(varTypeIsFloating(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));
            }
            else if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));
            }

            if (varTypeIsFloating(simdBaseType))
            {
                // op1 ^ -0.0

                op2 = gtNewDconNode(-0.0, simdBaseType);
                op2 = gtNewSimdCreateBroadcastNode(type, op2, simdBaseJitType, simdSize);

                return gtNewSimdBinOpNode(GT_XOR, type, op1, op2, simdBaseJitType, simdSize);
            }
            else
            {
                // Zero - op1
                op2 = gtNewZeroConNode(type);
                return gtNewSimdBinOpNode(GT_SUB, type, op2, op1, simdBaseJitType, simdSize);
            }
        }

        case GT_NOT:
        {
            if (simdSize == 64)
            {
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F));

                if (genTypeSize(simdBaseType) >= 4)
                {
                    intrinsic = NI_AVX512F_TernaryLogic;
                }
            }
            else
            {
                if (simdSize == 32)
                {
                    assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
                }

                if ((genTypeSize(simdBaseType) >= 4) && compOpportunisticallyDependsOn(InstructionSet_AVX512F_VL))
                {
                    intrinsic = NI_AVX512F_VL_TernaryLogic;
                }
            }

            if (intrinsic != NI_Illegal)
            {
                // AVX512 allows performing `not` without requiring a memory access
                assert(compIsaSupportedDebugOnly(InstructionSet_AVX512F_VL));

                op2 = gtNewZeroConNode(type);
                op3 = gtNewZeroConNode(type);

                GenTree* cns = gtNewIconNode(static_cast<uint8_t>(~0xAA)); // ~C
                return gtNewSimdTernaryLogicNode(type, op3, op2, op1, cns, simdBaseJitType, simdSize);
            }
            else
            {
                op2 = gtNewAllBitsSetConNode(type);
                return gtNewSimdBinOpNode(GT_XOR, type, op1, op2, simdBaseJitType, simdSize);
            }
        }
#elif defined(TARGET_ARM64)
        case GT_NEG:
        {
            if (varTypeIsSigned(simdBaseType))
            {
                if (simdBaseType == TYP_LONG)
                {
                    intrinsic = (simdSize == 8) ? NI_AdvSimd_Arm64_NegateScalar : NI_AdvSimd_Arm64_Negate;
                }
                else if (simdBaseType == TYP_DOUBLE)
                {
                    intrinsic = (simdSize == 8) ? NI_AdvSimd_NegateScalar : NI_AdvSimd_Arm64_Negate;
                }
                else
                {
                    intrinsic = NI_AdvSimd_Negate;
                }

                return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
            }
            else
            {
                // Zero - op1
                op2 = gtNewZeroConNode(type);
                return gtNewSimdBinOpNode(GT_SUB, type, op2, op1, simdBaseJitType, simdSize);
            }
        }

        case GT_NOT:
        {
            return gtNewSimdHWIntrinsicNode(type, op1, NI_AdvSimd_Not, simdBaseJitType, simdSize);
        }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

        default:
        {
            unreached();
        }
    }
}

GenTree* Compiler::gtNewSimdWidenLowerNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType) && !varTypeIsLong(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    GenTree* tmp1;

#if defined(TARGET_XARCH)
    if (simdSize == 64)
    {
        assert(IsBaselineVector512IsaSupportedDebugOnly());

        tmp1 = gtNewSimdGetLowerNode(TYP_SIMD32, op1, simdBaseJitType, simdSize);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            {
                intrinsic = NI_AVX512BW_ConvertToVector512Int16;
                break;
            }

            case TYP_UBYTE:
            {
                intrinsic = NI_AVX512BW_ConvertToVector512UInt16;
                break;
            }

            case TYP_SHORT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Int32;
                break;
            }

            case TYP_USHORT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512UInt32;
                break;
            }

            case TYP_INT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Int64;
                break;
            }

            case TYP_UINT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512UInt64;
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Double;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, tmp1, intrinsic, simdBaseJitType, simdSize);
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        assert(!varTypeIsIntegral(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));

        tmp1 = gtNewSimdGetLowerNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int16;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int32;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int64;
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_AVX_ConvertToVector256Double;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, tmp1, intrinsic, simdBaseJitType, simdSize);
    }
    else if ((simdBaseType == TYP_FLOAT) || compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int16;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int32;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int64;
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_SSE2_ConvertToVector128Double;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
    }
    else
    {
        tmp1 = gtNewZeroConNode(type);

        if (varTypeIsSigned(simdBaseType))
        {
            GenTree* op1Dup = fgMakeMultiUse(&op1);

            tmp1 = gtNewSimdHWIntrinsicNode(type, op1Dup, tmp1, NI_SSE2_CompareLessThan, simdBaseJitType, simdSize);
        }

        return gtNewSimdHWIntrinsicNode(type, op1, tmp1, NI_SSE2_UnpackLow, simdBaseJitType, simdSize);
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 16)
    {
        tmp1 = gtNewSimdGetLowerNode(TYP_SIMD8, op1, simdBaseJitType, simdSize);
    }
    else
    {
        assert(simdSize == 8);
        tmp1 = op1;
    }

    if (varTypeIsFloating(simdBaseType))
    {
        assert(simdBaseType == TYP_FLOAT);
        intrinsic = NI_AdvSimd_Arm64_ConvertToDouble;
    }
    else if (varTypeIsSigned(simdBaseType))
    {
        intrinsic = NI_AdvSimd_SignExtendWideningLower;
    }
    else
    {
        intrinsic = NI_AdvSimd_ZeroExtendWideningLower;
    }

    assert(intrinsic != NI_Illegal);
    tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, tmp1, intrinsic, simdBaseJitType, 8);

    if (simdSize == 8)
    {
        tmp1 = gtNewSimdGetLowerNode(TYP_SIMD8, tmp1, simdBaseJitType, 16);
    }

    return tmp1;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdWidenUpperNode(var_types type, GenTree* op1, CorInfoType simdBaseJitType, unsigned simdSize)
{
    assert(IsBaselineSimdIsaSupportedDebugOnly());

    assert(varTypeIsSIMD(type));
    assert(getSIMDTypeForSize(simdSize) == type);

    assert(op1 != nullptr);
    assert(op1->TypeIs(type));

    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType) && !varTypeIsLong(simdBaseType));

    NamedIntrinsic intrinsic = NI_Illegal;

    GenTree* tmp1;

#if defined(TARGET_XARCH)
    if (simdSize == 64)
    {
        assert(IsBaselineVector512IsaSupportedDebugOnly());

        tmp1 = gtNewSimdGetUpperNode(TYP_SIMD32, op1, simdBaseJitType, simdSize);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            {
                intrinsic = NI_AVX512BW_ConvertToVector512Int16;
                break;
            }

            case TYP_UBYTE:
            {
                intrinsic = NI_AVX512BW_ConvertToVector512UInt16;
                break;
            }

            case TYP_SHORT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Int32;
                break;
            }

            case TYP_USHORT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512UInt32;
                break;
            }

            case TYP_INT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Int64;
                break;
            }

            case TYP_UINT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512UInt64;
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_AVX512F_ConvertToVector512Double;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, tmp1, intrinsic, simdBaseJitType, simdSize);
    }
    else if (simdSize == 32)
    {
        assert(compIsaSupportedDebugOnly(InstructionSet_AVX));
        assert(!varTypeIsIntegral(simdBaseType) || compIsaSupportedDebugOnly(InstructionSet_AVX2));

        tmp1 = gtNewSimdGetUpperNode(TYP_SIMD16, op1, simdBaseJitType, simdSize);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int16;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int32;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                intrinsic = NI_AVX2_ConvertToVector256Int64;
                break;
            }

            case TYP_FLOAT:
            {
                intrinsic = NI_AVX_ConvertToVector256Double;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, tmp1, intrinsic, simdBaseJitType, simdSize);
    }
    else if (varTypeIsFloating(simdBaseType))
    {
        assert(simdBaseType == TYP_FLOAT);

        GenTree* op1Dup = fgMakeMultiUse(&op1);

        tmp1 = gtNewSimdHWIntrinsicNode(type, op1, op1Dup, NI_SSE_MoveHighToLow, simdBaseJitType, simdSize);
        return gtNewSimdHWIntrinsicNode(type, tmp1, NI_SSE2_ConvertToVector128Double, simdBaseJitType, simdSize);
    }
    else if (compOpportunisticallyDependsOn(InstructionSet_SSE41))
    {
        tmp1 = gtNewSimdHWIntrinsicNode(type, op1, gtNewIconNode(8), NI_SSE2_ShiftRightLogical128BitLane,
                                        simdBaseJitType, simdSize);

        switch (simdBaseType)
        {
            case TYP_BYTE:
            case TYP_UBYTE:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int16;
                break;
            }

            case TYP_SHORT:
            case TYP_USHORT:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int32;
                break;
            }

            case TYP_INT:
            case TYP_UINT:
            {
                intrinsic = NI_SSE41_ConvertToVector128Int64;
                break;
            }

            default:
            {
                unreached();
            }
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, tmp1, intrinsic, simdBaseJitType, simdSize);
    }
    else
    {
        tmp1 = gtNewZeroConNode(type);

        if (varTypeIsSigned(simdBaseType))
        {
            GenTree* op1Dup = fgMakeMultiUse(&op1);

            tmp1 = gtNewSimdHWIntrinsicNode(type, op1Dup, tmp1, NI_SSE2_CompareLessThan, simdBaseJitType, simdSize);
        }

        return gtNewSimdHWIntrinsicNode(type, op1, tmp1, NI_SSE2_UnpackHigh, simdBaseJitType, simdSize);
    }
#elif defined(TARGET_ARM64)
    if (simdSize == 16)
    {
        if (varTypeIsFloating(simdBaseType))
        {
            assert(simdBaseType == TYP_FLOAT);
            intrinsic = NI_AdvSimd_Arm64_ConvertToDoubleUpper;
        }
        else if (varTypeIsSigned(simdBaseType))
        {
            intrinsic = NI_AdvSimd_SignExtendWideningUpper;
        }
        else
        {
            intrinsic = NI_AdvSimd_ZeroExtendWideningUpper;
        }

        assert(intrinsic != NI_Illegal);
        return gtNewSimdHWIntrinsicNode(type, op1, intrinsic, simdBaseJitType, simdSize);
    }
    else
    {
        assert(simdSize == 8);
        ssize_t index = 8 / genTypeSize(simdBaseType);

        if (varTypeIsFloating(simdBaseType))
        {
            assert(simdBaseType == TYP_FLOAT);
            intrinsic = NI_AdvSimd_Arm64_ConvertToDouble;
        }
        else if (varTypeIsSigned(simdBaseType))
        {
            intrinsic = NI_AdvSimd_SignExtendWideningLower;
        }
        else
        {
            intrinsic = NI_AdvSimd_ZeroExtendWideningLower;
        }

        assert(intrinsic != NI_Illegal);

        tmp1 = gtNewSimdHWIntrinsicNode(TYP_SIMD16, op1, intrinsic, simdBaseJitType, simdSize);
        return gtNewSimdGetUpperNode(TYP_SIMD8, tmp1, simdBaseJitType, 16);
    }
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64
}

GenTree* Compiler::gtNewSimdWithElementNode(
    var_types type, GenTree* op1, GenTree* op2, GenTree* op3, CorInfoType simdBaseJitType, unsigned simdSize)
{
    NamedIntrinsic hwIntrinsicID = NI_Vector128_WithElement;
    var_types      simdBaseType  = JitType2PreciseVarType(simdBaseJitType);

    assert(varTypeIsArithmetic(simdBaseType));
    assert(op2->IsCnsIntOrI());

    ssize_t imm8  = op2->AsIntCon()->IconValue();
    ssize_t count = simdSize / genTypeSize(simdBaseType);

    assert((0 <= imm8) && (imm8 < count));

#if defined(TARGET_XARCH)
    switch (simdBaseType)
    {
        // Using software fallback if simdBaseType is not supported by hardware
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_INT:
        case TYP_UINT:
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE41));
            break;

        case TYP_LONG:
        case TYP_ULONG:
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE41_X64));
            break;

        case TYP_DOUBLE:
        case TYP_FLOAT:
        case TYP_SHORT:
        case TYP_USHORT:
            assert(compIsaSupportedDebugOnly(InstructionSet_SSE2));
            break;

        default:
            unreached();
    }

    if (simdSize == 64)
    {
        hwIntrinsicID = NI_Vector512_WithElement;
    }
    else if (simdSize == 32)
    {
        hwIntrinsicID = NI_Vector256_WithElement;
    }
#elif defined(TARGET_ARM64)
    switch (simdBaseType)
    {
        case TYP_LONG:
        case TYP_ULONG:
        case TYP_DOUBLE:
            if (simdSize == 8)
            {
                return gtNewSimdHWIntrinsicNode(type, op3, NI_Vector64_Create, simdBaseJitType, simdSize);
            }
            break;

        case TYP_FLOAT:
        case TYP_BYTE:
        case TYP_UBYTE:
        case TYP_SHORT:
        case TYP_USHORT:
        case TYP_INT:
        case TYP_UINT:
            break;

        default:
            unreached();
    }

    hwIntrinsicID = NI_AdvSimd_Insert;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, op2, op3, hwIntrinsicID, simdBaseJitType, simdSize);
}

#ifdef TARGET_ARM64
//------------------------------------------------------------------------
// gtConvertTableOpToFieldList: Convert a operand that represents table of rows into
//    field list, where each field represents a row in the table.
//
// Arguments:
//    op                  -- Operand to convert.
//    fieldCount          -- Number of fields or rows present.
//
// Return Value:
//    The GenTreeFieldList node.
//
GenTreeFieldList* Compiler::gtConvertTableOpToFieldList(GenTree* op, unsigned fieldCount)
{
    LclVarDsc* opVarDsc  = lvaGetDesc(op->AsLclVar());
    unsigned   lclNum    = lvaGetLclNum(opVarDsc);
    unsigned   fieldSize = opVarDsc->lvSize() / fieldCount;
    var_types  fieldType = TYP_SIMD16;

    GenTreeFieldList* fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();
    int               offset    = 0;
    for (unsigned fieldId = 0; fieldId < fieldCount; fieldId++)
    {
        GenTreeLclFld* fldNode = gtNewLclFldNode(lclNum, fieldType, offset);
        fieldList->AddField(this, fldNode, offset, fieldType);

        offset += fieldSize;
    }
    return fieldList;
}

//------------------------------------------------------------------------
// gtConvertParamOpToFieldList: Convert a operand that represents tuple of struct into
//    field list, where each field represents a struct in the tuple.
//
// Arguments:
//    op                  -- Operand to convert.
//    fieldCount          -- Number of fields or rows present.
//    clsHnd              -- Class handle of the tuple.
//
// Return Value:
//    The GenTreeFieldList node.
//
GenTreeFieldList* Compiler::gtConvertParamOpToFieldList(GenTree* op, unsigned fieldCount, CORINFO_CLASS_HANDLE clsHnd)
{
    LclVarDsc*           opVarDsc  = lvaGetDesc(op->AsLclVar());
    unsigned             lclNum    = lvaGetLclNum(opVarDsc);
    unsigned             fieldSize = opVarDsc->lvSize() / fieldCount;
    GenTreeFieldList*    fieldList = new (this, GT_FIELD_LIST) GenTreeFieldList();
    int                  offset    = 0;
    unsigned             sizeBytes = 0;
    CORINFO_CLASS_HANDLE structType;

    for (unsigned fieldId = 0; fieldId < fieldCount; fieldId++)
    {
        CORINFO_FIELD_HANDLE fieldHandle = info.compCompHnd->getFieldInClass(clsHnd, fieldId);
        JitType2PreciseVarType(info.compCompHnd->getFieldType(fieldHandle, &structType));
        getBaseJitTypeAndSizeOfSIMDType(structType, &sizeBytes);
        var_types simdType = getSIMDTypeForSize(sizeBytes);

        GenTreeLclFld* fldNode = gtNewLclFldNode(lclNum, simdType, offset);
        fieldList->AddField(this, fldNode, offset, simdType);

        offset += fieldSize;
    }
    return fieldList;
}
#endif // TARGET_ARM64

GenTree* Compiler::gtNewSimdWithLowerNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsicId = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(type == TYP_SIMD32);
        intrinsicId = NI_Vector256_WithLower;
    }
    else
    {
        assert((type == TYP_SIMD64) && (simdSize == 64));
        intrinsicId = NI_Vector512_WithLower;
    }
#elif defined(TARGET_ARM64)
    assert((type == TYP_SIMD16) && (simdSize == 16));
    intrinsicId = NI_Vector128_WithLower;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsicId, simdBaseJitType, simdSize);
}

GenTree* Compiler::gtNewSimdWithUpperNode(
    var_types type, GenTree* op1, GenTree* op2, CorInfoType simdBaseJitType, unsigned simdSize)
{
    var_types simdBaseType = JitType2PreciseVarType(simdBaseJitType);
    assert(varTypeIsArithmetic(simdBaseType));

    NamedIntrinsic intrinsicId = NI_Illegal;

#if defined(TARGET_XARCH)
    if (simdSize == 32)
    {
        assert(type == TYP_SIMD32);
        intrinsicId = NI_Vector256_WithUpper;
    }
    else
    {
        assert((type == TYP_SIMD64) && (simdSize == 64));
        intrinsicId = NI_Vector512_WithUpper;
    }
#elif defined(TARGET_ARM64)
    assert((type == TYP_SIMD16) && (simdSize == 16));
    intrinsicId = NI_Vector128_WithUpper;
#else
#error Unsupported platform
#endif // !TARGET_XARCH && !TARGET_ARM64

    return gtNewSimdHWIntrinsicNode(type, op1, op2, intrinsicId, simdBaseJitType, simdSize);
}

GenTreeHWIntrinsic* Compiler::gtNewScalarHWIntrinsicNode(var_types type, NamedIntrinsic hwIntrinsicID)
{
    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, CORINFO_TYPE_UNDEF, 0);
}

GenTreeHWIntrinsic* Compiler::gtNewScalarHWIntrinsicNode(var_types type, GenTree* op1, NamedIntrinsic hwIntrinsicID)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, CORINFO_TYPE_UNDEF, 0, op1);
}

GenTreeHWIntrinsic* Compiler::gtNewScalarHWIntrinsicNode(var_types      type,
                                                         GenTree*       op1,
                                                         GenTree*       op2,
                                                         NamedIntrinsic hwIntrinsicID)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);
    SetOpLclRelatedToSIMDIntrinsic(op2);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, CORINFO_TYPE_UNDEF, 0, op1, op2);
}

GenTreeHWIntrinsic* Compiler::gtNewScalarHWIntrinsicNode(
    var_types type, GenTree* op1, GenTree* op2, GenTree* op3, NamedIntrinsic hwIntrinsicID)
{
    SetOpLclRelatedToSIMDIntrinsic(op1);
    SetOpLclRelatedToSIMDIntrinsic(op2);
    SetOpLclRelatedToSIMDIntrinsic(op3);

    return new (this, GT_HWINTRINSIC)
        GenTreeHWIntrinsic(type, getAllocator(CMK_ASTNode), hwIntrinsicID, CORINFO_TYPE_UNDEF, 0, op1, op2, op3);
}

//------------------------------------------------------------------------
// OperIsHWIntrinsic: Is this a hwintrinsic with the specified id
//
// Arguments:
//    intrinsicId -- the id to compare with the current node
//
// Return Value:
//    true if the node is a hwintrinsic intrinsic with the specified id
//    otherwise; false
//
bool GenTree::OperIsHWIntrinsic(NamedIntrinsic intrinsicId) const
{
    if (OperIsHWIntrinsic())
    {
        return AsHWIntrinsic()->GetHWIntrinsicId() == intrinsicId;
    }
    return false;
}

//------------------------------------------------------------------------
// OperIsMemoryLoad: Does this HWI node have memory load semantics?
//
// Arguments:
//    pAddr - optional [out] parameter for the address
//
// Return Value:
//    Whether this intrinsic may throw NullReferenceException if the
//    address is "null".
//
bool GenTreeHWIntrinsic::OperIsMemoryLoad(GenTree** pAddr) const
{
    GenTree* addr = nullptr;

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
    NamedIntrinsic      intrinsicId = GetHWIntrinsicId();
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);

    if (category == HW_Category_MemoryLoad)
    {
        switch (intrinsicId)
        {
#ifdef TARGET_XARCH
            case NI_SSE_LoadLow:
            case NI_SSE_LoadHigh:
            case NI_SSE2_LoadLow:
            case NI_SSE2_LoadHigh:
                addr = Op(2);
                break;
#endif // TARGET_XARCH

#ifdef TARGET_ARM64
            case NI_AdvSimd_LoadAndInsertScalar:
            case NI_AdvSimd_LoadAndInsertScalarVector64x2:
            case NI_AdvSimd_LoadAndInsertScalarVector64x3:
            case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
            case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
                addr = Op(3);
                break;

            case NI_Sve_LoadVector:
                addr = Op(2);
                break;
#endif // TARGET_ARM64

            default:
                addr = Op(1);
                break;
        }
    }
#ifdef TARGET_XARCH
    else if (HWIntrinsicInfo::MaybeMemoryLoad(intrinsicId))
    {
        // Some intrinsics (without HW_Category_MemoryLoad) also have MemoryLoad semantics
        // This is generally because they have both vector and pointer overloads, e.g.,
        // * Vector128<byte> BroadcastScalarToVector128(Vector128<byte> value)
        // * Vector128<byte> BroadcastScalarToVector128(byte* source)
        //
        if ((category == HW_Category_SimpleSIMD) || (category == HW_Category_SIMDScalar))
        {
            assert(GetOperandCount() == 1);

            switch (intrinsicId)
            {
                case NI_SSE41_ConvertToVector128Int16:
                case NI_SSE41_ConvertToVector128Int32:
                case NI_SSE41_ConvertToVector128Int64:
                case NI_AVX2_BroadcastScalarToVector128:
                case NI_AVX2_BroadcastScalarToVector256:
                case NI_AVX512F_BroadcastScalarToVector512:
                case NI_AVX512BW_BroadcastScalarToVector512:
                case NI_AVX2_ConvertToVector256Int16:
                case NI_AVX2_ConvertToVector256Int32:
                case NI_AVX2_ConvertToVector256Int64:
                case NI_AVX2_BroadcastVector128ToVector256:
                case NI_AVX512F_BroadcastVector128ToVector512:
                case NI_AVX512F_BroadcastVector256ToVector512:
                    if (GetAuxiliaryJitType() == CORINFO_TYPE_PTR)
                    {
                        addr = Op(1);
                    }
                    else
                    {
                        assert(GetAuxiliaryJitType() == CORINFO_TYPE_UNDEF);
                    }
                    break;

                default:
                    unreached();
            }
        }
        else if (category == HW_Category_IMM)
        {
            switch (intrinsicId)
            {
                case NI_AVX2_GatherVector128:
                case NI_AVX2_GatherVector256:
                    addr = Op(1);
                    break;

                case NI_AVX2_GatherMaskVector128:
                case NI_AVX2_GatherMaskVector256:
                    addr = Op(2);
                    break;

                default:
                    break;
            }
        }
    }
#endif // TARGET_XARCH
#endif // TARGET_XARCH || TARGET_ARM64

    if (pAddr != nullptr)
    {
        *pAddr = addr;
    }

    if (addr != nullptr)
    {
        assert(varTypeIsI(addr));
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// OperIsMemoryLoad: Does this HWI node have memory store semantics?
//
// Arguments:
//    pAddr - optional [out] parameter for the address
//
// Return Value:
//    Whether this intrinsic may mutate heap state and/or throw a
//    NullReferenceException if the address is "null".
//
bool GenTreeHWIntrinsic::OperIsMemoryStore(GenTree** pAddr) const
{
    GenTree* addr = nullptr;

#if defined(TARGET_XARCH) || defined(TARGET_ARM64)
    NamedIntrinsic      intrinsicId = GetHWIntrinsicId();
    HWIntrinsicCategory category    = HWIntrinsicInfo::lookupCategory(intrinsicId);

    if (category == HW_Category_MemoryStore)
    {
        switch (intrinsicId)
        {
#ifdef TARGET_XARCH
            case NI_SSE2_MaskMove:
                addr = Op(3);
                break;
#endif // TARGET_XARCH

            default:
                addr = Op(1);
                break;
        }
    }
#ifdef TARGET_XARCH
    else if (HWIntrinsicInfo::MaybeMemoryStore(intrinsicId) &&
             (category == HW_Category_IMM || category == HW_Category_Scalar))
    {
        // Some intrinsics (without HW_Category_MemoryStore) also have MemoryStore semantics

        // Bmi2/Bmi2.X64.MultiplyNoFlags may return the lower half result by a out argument
        // unsafe ulong MultiplyNoFlags(ulong left, ulong right, ulong* low)
        //
        // So, the 3-argument form is MemoryStore
        if (GetOperandCount() == 3)
        {
            switch (intrinsicId)
            {
                case NI_BMI2_MultiplyNoFlags:
                case NI_BMI2_X64_MultiplyNoFlags:
                    addr = Op(3);
                    break;

                default:
                    break;
            }
        }
    }
#endif // TARGET_XARCH
#endif // TARGET_XARCH || TARGET_ARM64

    if (pAddr != nullptr)
    {
        *pAddr = addr;
    }

    if (addr != nullptr)
    {
        assert(varTypeIsI(addr));
        return true;
    }

    return false;
}

//------------------------------------------------------------------------
// OperIsMemoryLoadOrStore: Does this HWI node have memory load or store semantics?
//
// Return Value:
//    Whether "this" is "OperIsMemoryLoad" or "OperIsMemoryStore".
//
bool GenTreeHWIntrinsic::OperIsMemoryLoadOrStore() const
{
    return OperIsMemoryLoad() || OperIsMemoryStore();
}

//------------------------------------------------------------------------
// OperIsMemoryStoreOrBarrier: Does this HWI node have memory store or barrier semantics?
//
// Return Value:
//    Whether "this" is a store or memory barrier.
//
bool GenTreeHWIntrinsic::OperIsMemoryStoreOrBarrier() const
{
    if (OperIsMemoryStore())
    {
        return true;
    }
#if defined(TARGET_XARCH)
    NamedIntrinsic intrinsicId = GetHWIntrinsicId();

    if (HWIntrinsicInfo::HasSpecialSideEffect_Barrier(intrinsicId))
    {
        return true;
    }
#endif // TARGET_XARCH
    else
    {
        return false;
    }
}

//------------------------------------------------------------------------
// OperIsEmbBroadcastCompatible: Checks if the intrinsic is a embedded broadcast compatible inintrsic.
//
// Return Value:
//     true if the intrinsic node lowering instruction is embedded broadcast compatible.
//
bool GenTreeHWIntrinsic::OperIsEmbBroadcastCompatible() const
{
#if defined(TARGET_XARCH)
    return HWIntrinsicInfo::IsEmbBroadcastCompatible(GetHWIntrinsicId());
#else
    return false;
#endif // TARGET_XARCH
}

//------------------------------------------------------------------------
// OperIsBroadcastScalar: Is this HWIntrinsic a broadcast node from scalar.
//
// Return Value:
//    Whether "this" is a broadcast node from scalar.
//
bool GenTreeHWIntrinsic::OperIsBroadcastScalar() const
{
#if defined(TARGET_XARCH)
    NamedIntrinsic intrinsicId = GetHWIntrinsicId();
    switch (intrinsicId)
    {
        case NI_AVX2_BroadcastScalarToVector128:
        case NI_AVX2_BroadcastScalarToVector256:
        case NI_AVX_BroadcastScalarToVector128:
        case NI_AVX_BroadcastScalarToVector256:
        case NI_SSE3_MoveAndDuplicate:
        case NI_AVX512F_BroadcastScalarToVector512:
            return true;
        default:
            return false;
    }
#else
    return false;
#endif
}

//------------------------------------------------------------------------
// OperIsCreateScalarUnsafe: Is this HWIntrinsic a CreateScalarUnsafe node.
//
// Return Value:
//    Whether "this" is a CreateScalarUnsafe node.
//
bool GenTreeHWIntrinsic::OperIsCreateScalarUnsafe() const
{
    NamedIntrinsic intrinsicId = GetHWIntrinsicId();

    switch (intrinsicId)
    {
#if defined(TARGET_ARM64)
        case NI_Vector64_CreateScalarUnsafe:
#endif // TARGET_ARM64
        case NI_Vector128_CreateScalarUnsafe:
#if defined(TARGET_XARCH)
        case NI_Vector256_CreateScalarUnsafe:
        case NI_Vector512_CreateScalarUnsafe:
#endif // TARGET_XARCH
        {
            return true;
        }

        default:
        {
            return false;
        }
    }
}

//------------------------------------------------------------------------
// OperIsBitwiseHWIntrinsic: Is this HWIntrinsic a bitwise logic intrinsic node.
//
// Return Value:
//    Whether "this" is a bitwise logic intrinsic node.
//
bool GenTreeHWIntrinsic::OperIsBitwiseHWIntrinsic() const
{
    genTreeOps Oper = HWOperGet();
    return Oper == GT_AND || Oper == GT_OR || Oper == GT_XOR || Oper == GT_AND_NOT;
}

//------------------------------------------------------------------------------
// OperRequiresAsgFlag : Check whether the operation requires GTF_ASG flag regardless
//                       of the children's flags.
//
bool GenTreeHWIntrinsic::OperRequiresAsgFlag() const
{
    // A MemoryStore operation is an assignment and barriers, while they
    // don't technically do an assignment are modeled the same as
    // GT_MEMORYBARRIER which tracks itself as requiring the GTF_ASG flag
    return OperIsMemoryStoreOrBarrier();
}

//------------------------------------------------------------------------------
// OperRequiresCallFlag : Check whether the operation requires GTF_CALL flag regardless
//                        of the children's flags.
//
bool GenTreeHWIntrinsic::OperRequiresCallFlag() const
{
    NamedIntrinsic intrinsicId = GetHWIntrinsicId();

    if (HWIntrinsicInfo::HasSpecialSideEffect(intrinsicId))
    {
        switch (intrinsicId)
        {
#if defined(TARGET_XARCH)
            case NI_X86Base_Pause:
            case NI_SSE_Prefetch0:
            case NI_SSE_Prefetch1:
            case NI_SSE_Prefetch2:
            case NI_SSE_PrefetchNonTemporal:
            {
                return true;
            }
#endif // TARGET_XARCH

#if defined(TARGET_ARM64)
            case NI_ArmBase_Yield:
            {
                return true;
            }
#endif // TARGET_ARM64

            default:
            {
                break;
            }
        }
    }

    return false;
}

//------------------------------------------------------------------------------
// OperRequiresGlobRefFlag : Check whether the operation requires GTF_GLOB_REF
//                           flag regardless of the children's flags.
//
bool GenTreeHWIntrinsic::OperRequiresGlobRefFlag() const
{
    return OperIsMemoryLoad() || OperRequiresAsgFlag() || OperRequiresCallFlag();
}

//------------------------------------------------------------------------
// GetLayout: Get the layout for this TYP_STRUCT HWI node.
//
// Arguments:
//    compiler - The Compiler instance
//
// Return Value:
//    The struct layout for the node.
//
// Notes:
//    Currently, this method synthesizes block layouts, since there is not
//    enough space in the GenTreeHWIntrinsic node to store a proper layout
//    pointer or number.
//
ClassLayout* GenTreeHWIntrinsic::GetLayout(Compiler* compiler) const
{
    assert(TypeIs(TYP_STRUCT));

    switch (GetHWIntrinsicId())
    {
#ifdef TARGET_XARCH
        case NI_X86Base_DivRem:
            return compiler->typGetBlkLayout(genTypeSize(GetSimdBaseType()) * 2);
        case NI_X86Base_X64_DivRem:
            return compiler->typGetBlkLayout(16);
#endif // TARGET_XARCH
#ifdef TARGET_ARM64
        case NI_AdvSimd_Arm64_LoadPairScalarVector64:
        case NI_AdvSimd_Arm64_LoadPairScalarVector64NonTemporal:
        case NI_AdvSimd_Arm64_LoadPairVector64:
        case NI_AdvSimd_Arm64_LoadPairVector64NonTemporal:
        case NI_AdvSimd_LoadVector64x2AndUnzip:
        case NI_AdvSimd_LoadVector64x2:
        case NI_AdvSimd_LoadAndInsertScalarVector64x2:
        case NI_AdvSimd_LoadAndReplicateToVector64x2:
            return compiler->typGetBlkLayout(16);

        case NI_AdvSimd_Arm64_LoadPairVector128:
        case NI_AdvSimd_Arm64_LoadPairVector128NonTemporal:
        case NI_AdvSimd_Arm64_LoadVector128x2AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x2:
        case NI_AdvSimd_LoadVector64x4:
        case NI_AdvSimd_LoadVector64x4AndUnzip:
        case NI_AdvSimd_LoadAndReplicateToVector64x4:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x2:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x2:
        case NI_AdvSimd_LoadAndInsertScalarVector64x4:
            return compiler->typGetBlkLayout(32);

        case NI_AdvSimd_LoadVector64x3AndUnzip:
        case NI_AdvSimd_LoadVector64x3:
        case NI_AdvSimd_LoadAndInsertScalarVector64x3:
        case NI_AdvSimd_LoadAndReplicateToVector64x3:
            return compiler->typGetBlkLayout(24);

        case NI_AdvSimd_Arm64_LoadVector128x3AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x3:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x3:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x3:
            return compiler->typGetBlkLayout(48);

        case NI_AdvSimd_Arm64_LoadVector128x4AndUnzip:
        case NI_AdvSimd_Arm64_LoadVector128x4:
        case NI_AdvSimd_Arm64_LoadAndInsertScalarVector128x4:
        case NI_AdvSimd_Arm64_LoadAndReplicateToVector128x4:
            return compiler->typGetBlkLayout(64);

#endif // TARGET_ARM64

        default:
            unreached();
    }
}

NamedIntrinsic GenTreeHWIntrinsic::GetHWIntrinsicId() const
{
    NamedIntrinsic id             = gtHWIntrinsicId;
    int            numArgs        = HWIntrinsicInfo::lookupNumArgs(id);
    bool           numArgsUnknown = numArgs < 0;

    assert((static_cast<size_t>(numArgs) == GetOperandCount()) || numArgsUnknown);

    return id;
}

void GenTreeHWIntrinsic::SetHWIntrinsicId(NamedIntrinsic intrinsicId)
{
#ifdef DEBUG
    size_t oldOperandCount = GetOperandCount();
    int    newOperandCount = HWIntrinsicInfo::lookupNumArgs(intrinsicId);
    bool   newCountUnknown = newOperandCount < 0;

    // We'll choose to trust the programmer here.
    assert((oldOperandCount == static_cast<size_t>(newOperandCount)) || newCountUnknown);
#endif // DEBUG

    gtHWIntrinsicId = intrinsicId;
}

/* static */ bool GenTreeHWIntrinsic::Equals(GenTreeHWIntrinsic* op1, GenTreeHWIntrinsic* op2)
{
    return (op1->TypeGet() == op2->TypeGet()) && (op1->GetHWIntrinsicId() == op2->GetHWIntrinsicId()) &&
           (op1->GetSimdBaseType() == op2->GetSimdBaseType()) && (op1->GetSimdSize() == op2->GetSimdSize()) &&
           (op1->GetAuxiliaryType() == op2->GetAuxiliaryType()) && (op1->GetRegByIndex(1) == op2->GetRegByIndex(1)) &&
           OperandsAreEqual(op1, op2);
}

void GenTreeHWIntrinsic::Initialize(NamedIntrinsic intrinsicId)
{
    SetHWIntrinsicId(intrinsicId);

    if (OperIsMemoryStore())
    {
        gtFlags |= (GTF_ASG | GTF_GLOB_REF | GTF_EXCEPT);
    }
    else if (OperIsMemoryLoad())
    {
        gtFlags |= (GTF_GLOB_REF | GTF_EXCEPT);
    }
    else if (HWIntrinsicInfo::HasSpecialSideEffect(intrinsicId))
    {
        switch (intrinsicId)
        {
#if defined(TARGET_XARCH)
            case NI_SSE_StoreFence:
            case NI_SSE2_LoadFence:
            case NI_SSE2_MemoryFence:
            case NI_X86Serialize_Serialize:
            {
                // Mark as an assignment and global reference, much as is done for GT_MEMORYBARRIER
                gtFlags |= (GTF_ASG | GTF_GLOB_REF);
                break;
            }

            case NI_X86Base_Pause:
            case NI_SSE_Prefetch0:
            case NI_SSE_Prefetch1:
            case NI_SSE_Prefetch2:
            case NI_SSE_PrefetchNonTemporal:
            {
                // Mark as a call and global reference, much as is done for GT_KEEPALIVE
                gtFlags |= (GTF_CALL | GTF_GLOB_REF);
                break;
            }
#endif // TARGET_XARCH

#if defined(TARGET_ARM64)
            case NI_ArmBase_Yield:
            {
                // Mark as a call and global reference, much as is done for GT_KEEPALIVE
                gtFlags |= (GTF_CALL | GTF_GLOB_REF);
                break;
            }
#endif // TARGET_ARM64

            default:
            {
                assert(!"Unexpected hwintrinsic side-effect");
                break;
            }
        }
    }
}

//------------------------------------------------------------------------------
// HWOperGet : Returns Oper based on the HWIntrinsicId
//
genTreeOps GenTreeHWIntrinsic::HWOperGet() const
{
    switch (GetHWIntrinsicId())
    {
#if defined(TARGET_XARCH)
        case NI_SSE_And:
        case NI_SSE2_And:
        case NI_AVX_And:
        case NI_AVX2_And:
        case NI_AVX512F_And:
        case NI_AVX512DQ_And:
#elif defined(TARGET_ARM64)
        case NI_AdvSimd_And:
#endif
        {
            return GT_AND;
        }

#if defined(TARGET_ARM64)
        case NI_AdvSimd_Not:
        {
            return GT_NOT;
        }
#endif

#if defined(TARGET_XARCH)
        case NI_SSE_Xor:
        case NI_SSE2_Xor:
        case NI_AVX_Xor:
        case NI_AVX2_Xor:
        case NI_AVX512F_Xor:
        case NI_AVX512DQ_Xor:
#elif defined(TARGET_ARM64)
        case NI_AdvSimd_Xor:
#endif
        {
            return GT_XOR;
        }

#if defined(TARGET_XARCH)
        case NI_SSE_Or:
        case NI_SSE2_Or:
        case NI_AVX_Or:
        case NI_AVX2_Or:
        case NI_AVX512F_Or:
        case NI_AVX512DQ_Or:
#elif defined(TARGET_ARM64)
        case NI_AdvSimd_Or:
#endif
        {
            return GT_OR;
        }

#if defined(TARGET_XARCH)
        case NI_SSE_AndNot:
        case NI_SSE2_AndNot:
        case NI_AVX_AndNot:
        case NI_AVX2_AndNot:
        case NI_AVX512F_AndNot:
        case NI_AVX512DQ_AndNot:
        {
            return GT_AND_NOT;
        }
#endif
        // TODO: Handle other cases

        default:
        {
            return GT_NONE;
        }
    }
}

#endif // FEATURE_HW_INTRINSICS

//---------------------------------------------------------------------------------------
// gtNewMustThrowException:
//    create a throw node (calling into JIT helper) that must be thrown.
//    The result would be a comma node: COMMA(jithelperthrow(void), x) where x's type should be specified.
//
// Arguments
//    helper      -  JIT helper ID
//    type        -  return type of the node
//
// Return Value
//    pointer to the throw node
//
GenTree* Compiler::gtNewMustThrowException(unsigned helper, var_types type, CORINFO_CLASS_HANDLE clsHnd)
{
    GenTreeCall* node = gtNewHelperCallNode(helper, TYP_VOID);
    node->gtCallMoreFlags |= GTF_CALL_M_DOES_NOT_RETURN;
    if (type != TYP_VOID)
    {
        unsigned dummyTemp = lvaGrabTemp(true DEBUGARG("dummy temp of must thrown exception"));
        if (type == TYP_STRUCT)
        {
            lvaSetStruct(dummyTemp, clsHnd, false);
            type = lvaTable[dummyTemp].lvType; // struct type is normalized
        }
        else
        {
            lvaTable[dummyTemp].lvType = type;
        }
        GenTree* dummyNode = gtNewLclvNode(dummyTemp, type);
        return gtNewOperNode(GT_COMMA, type, node, dummyNode);
    }
    return node;
}

//---------------------------------------------------------------------------------------
// InitializeStructReturnType:
//    Initialize the Return Type Descriptor for a method that returns a struct type
//
// Arguments
//    comp        -  Compiler Instance
//    retClsHnd   -  VM handle to the struct type returned by the method
//
// Return Value
//    None
//
void ReturnTypeDesc::InitializeStructReturnType(Compiler*                comp,
                                                CORINFO_CLASS_HANDLE     retClsHnd,
                                                CorInfoCallConvExtension callConv)
{
    assert(!m_inited);

    assert(retClsHnd != NO_CLASS_HANDLE);
    unsigned structSize = comp->info.compCompHnd->getClassSize(retClsHnd);

    Compiler::structPassingKind howToReturnStruct;
    var_types returnType = comp->getReturnTypeForStruct(retClsHnd, callConv, &howToReturnStruct, structSize);

    switch (howToReturnStruct)
    {
        case Compiler::SPK_EnclosingType:
        case Compiler::SPK_PrimitiveType:
        {
            assert(returnType != TYP_UNKNOWN);
            assert(returnType != TYP_STRUCT);
            m_regType[0] = returnType;
            break;
        }

        case Compiler::SPK_ByValueAsHfa:
        {
            assert(varTypeIsStruct(returnType));
            var_types hfaType = comp->GetHfaType(retClsHnd);

            // We should have an hfa struct type
            assert(varTypeIsValidHfaType(hfaType));

            // Note that the retail build issues a warning about a potential divsion by zero without this "max",
            unsigned elemSize = max(1, genTypeSize(hfaType));

            // The size of this struct should be evenly divisible by elemSize
            assert((structSize % elemSize) == 0);

            unsigned hfaCount = (structSize / elemSize);
            for (unsigned i = 0; i < hfaCount; ++i)
            {
                m_regType[i] = hfaType;
            }

            comp->compFloatingPointUsed = true;
            break;
        }

        case Compiler::SPK_ByValue:
        {
            assert(varTypeIsStruct(returnType));

#ifdef UNIX_AMD64_ABI

            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            comp->eeGetSystemVAmd64PassStructInRegisterDescriptor(retClsHnd, &structDesc);

            assert(structDesc.passedInRegisters);
            for (int i = 0; i < structDesc.eightByteCount; i++)
            {
                assert(i < MAX_RET_REG_COUNT);
                m_regType[i] = comp->GetEightByteType(structDesc, i);
            }

#elif defined(TARGET_ARM64)

            // a non-HFA struct returned using two registers
            //
            assert((structSize > TARGET_POINTER_SIZE) && (structSize <= (2 * TARGET_POINTER_SIZE)));

            BYTE gcPtrs[2] = {TYPE_GC_NONE, TYPE_GC_NONE};
            comp->info.compCompHnd->getClassGClayout(retClsHnd, &gcPtrs[0]);
            for (unsigned i = 0; i < 2; ++i)
            {
                m_regType[i] = comp->getJitGCType(gcPtrs[i]);
            }

#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
            assert((structSize >= TARGET_POINTER_SIZE) && (structSize <= (2 * TARGET_POINTER_SIZE)));

#ifdef TARGET_LOONGARCH64
            uint32_t floatFieldFlags = comp->info.compCompHnd->getLoongArch64PassStructInRegisterFlags(retClsHnd);
#else
            uint32_t floatFieldFlags = comp->info.compCompHnd->getRISCV64PassStructInRegisterFlags(retClsHnd);
#endif
            BYTE     gcPtrs[2]       = {TYPE_GC_NONE, TYPE_GC_NONE};
            comp->info.compCompHnd->getClassGClayout(retClsHnd, &gcPtrs[0]);

            if (floatFieldFlags & STRUCT_FLOAT_FIELD_ONLY_TWO)
            {
                comp->compFloatingPointUsed = true;
                assert((structSize > 8) == ((floatFieldFlags & STRUCT_HAS_8BYTES_FIELDS_MASK) > 0));
                m_regType[0] = (floatFieldFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
                m_regType[1] = (floatFieldFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
            }
            else if (floatFieldFlags & STRUCT_FLOAT_FIELD_FIRST)
            {
                comp->compFloatingPointUsed = true;
                assert((structSize > 8) == ((floatFieldFlags & STRUCT_HAS_8BYTES_FIELDS_MASK) > 0));
                m_regType[0] = (floatFieldFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
                m_regType[1] =
                    (floatFieldFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? comp->getJitGCType(gcPtrs[1]) : TYP_INT;
            }
            else if (floatFieldFlags & STRUCT_FLOAT_FIELD_SECOND)
            {
                comp->compFloatingPointUsed = true;
                assert((structSize > 8) == ((floatFieldFlags & STRUCT_HAS_8BYTES_FIELDS_MASK) > 0));
                m_regType[0] =
                    (floatFieldFlags & STRUCT_FIRST_FIELD_SIZE_IS8) ? comp->getJitGCType(gcPtrs[0]) : TYP_INT;
                m_regType[1] = (floatFieldFlags & STRUCT_SECOND_FIELD_SIZE_IS8) ? TYP_DOUBLE : TYP_FLOAT;
            }
            else
            {
                for (unsigned i = 0; i < 2; ++i)
                {
                    m_regType[i] = comp->getJitGCType(gcPtrs[i]);
                }
            }

#elif defined(TARGET_X86)

            // an 8-byte struct returned using two registers
            assert(structSize == 8);

            BYTE gcPtrs[2] = {TYPE_GC_NONE, TYPE_GC_NONE};
            comp->info.compCompHnd->getClassGClayout(retClsHnd, &gcPtrs[0]);
            for (unsigned i = 0; i < 2; ++i)
            {
                m_regType[i] = comp->getJitGCType(gcPtrs[i]);
            }

#else //  TARGET_XXX

            // This target needs support here!
            //
            NYI("Unsupported TARGET returning a TYP_STRUCT in InitializeStructReturnType");

#endif             // UNIX_AMD64_ABI
            break; // for case SPK_ByValue
        }

        case Compiler::SPK_ByReference:
            // We are returning using the return buffer argument, there are no return registers.
            break;

        default:
            unreached(); // By the contract of getReturnTypeForStruct we should never get here.

    } // end of switch (howToReturnStruct)

#ifdef DEBUG
    m_inited = true;
#endif
}

//---------------------------------------------------------------------------------------
// InitializeLongReturnType:
//    Initialize the Return Type Descriptor for a method that returns a TYP_LONG
//
void ReturnTypeDesc::InitializeLongReturnType()
{
    assert(!m_inited);
#if defined(TARGET_X86) || defined(TARGET_ARM)
    // Setups up a ReturnTypeDesc for returning a long using two registers
    //
    assert(MAX_RET_REG_COUNT >= 2);
    m_regType[0] = TYP_INT;
    m_regType[1] = TYP_INT;

#else // not (TARGET_X86 or TARGET_ARM)

    m_regType[0] = TYP_LONG;

#endif // TARGET_X86 or TARGET_ARM

#ifdef DEBUG
    m_inited = true;
#endif
}

//---------------------------------------------------------------------------------------
// InitializeReturnType: Initialize the descriptor for a method that returns any type.
//
// Arguments:
//    comp      - The Compiler instance
//    type      - The return type as specififed by the signature
//    retClsHnd - Handle for struct return types
//    callConv  - Method's calling convention
//
void ReturnTypeDesc::InitializeReturnType(Compiler*                comp,
                                          var_types                type,
                                          CORINFO_CLASS_HANDLE     retClsHnd,
                                          CorInfoCallConvExtension callConv)
{
    if (varTypeIsStruct(type))
    {
        InitializeStructReturnType(comp, retClsHnd, callConv);
    }
    else if (type == TYP_LONG)
    {
        InitializeLongReturnType();
    }
    else
    {
        if (type != TYP_VOID)
        {
            assert(varTypeIsEnregisterable(type));
            m_regType[0] = type;
        }

        INDEBUG(m_inited = true);
    }
}

//-------------------------------------------------------------------
// GetABIReturnReg:  Return i'th return register as per target ABI
//
// Arguments:
//     idx   -   Index of the return register.
//               The first return register has an index of 0 and so on.
//
// Return Value:
//     Returns i'th return register as per target ABI.
//
// Notes:
//     x86 and ARM return long in multiple registers.
//     ARM and ARM64 return HFA struct in multiple registers.
//
regNumber ReturnTypeDesc::GetABIReturnReg(unsigned idx) const
{
    unsigned count = GetReturnRegCount();
    assert(idx < count);

    regNumber resultReg = REG_NA;

#ifdef UNIX_AMD64_ABI
    var_types regType0 = GetReturnRegType(0);

    if (idx == 0)
    {
        if (varTypeUsesIntReg(regType0))
        {
            resultReg = REG_INTRET;
        }
        else
        {
            noway_assert(varTypeUsesFloatReg(regType0));
            resultReg = REG_FLOATRET;
        }
    }
    else if (idx == 1)
    {
        var_types regType1 = GetReturnRegType(1);

        if (varTypeUsesIntReg(regType1))
        {
            if (varTypeIsIntegralOrI(regType0))
            {
                resultReg = REG_INTRET_1;
            }
            else
            {
                resultReg = REG_INTRET;
            }
        }
        else
        {
            noway_assert(varTypeUsesFloatReg(regType1));

            if (varTypeUsesFloatReg(regType0))
            {
                resultReg = REG_FLOATRET_1;
            }
            else
            {
                resultReg = REG_FLOATRET;
            }
        }
    }
#elif defined(WINDOWS_AMD64_ABI)

    assert(idx == 0);

    if (varTypeUsesIntReg(GetReturnRegType(0)))
    {
        resultReg = REG_INTRET;
    }
    else
    {
        assert(varTypeUsesFloatReg(GetReturnRegType(0)));
        resultReg = REG_FLOATRET;
    }

#elif defined(TARGET_X86)

    if (idx == 0)
    {
        resultReg = REG_LNGRET_LO;
    }
    else if (idx == 1)
    {
        resultReg = REG_LNGRET_HI;
    }

#elif defined(TARGET_ARM)

    var_types regType = GetReturnRegType(idx);
    if (varTypeIsIntegralOrI(regType))
    {
        // Ints are returned in one return register.
        // Longs are returned in two return registers.
        if (idx == 0)
        {
            resultReg = REG_LNGRET_LO;
        }
        else if (idx == 1)
        {
            resultReg = REG_LNGRET_HI;
        }
    }
    else
    {
        // Floats are returned in one return register (f0).
        // Doubles are returned in one return register (d0).
        // Structs are returned in four registers with HFAs.
        assert(idx < MAX_RET_REG_COUNT); // Up to 4 return registers for HFA's
        if (regType == TYP_DOUBLE)
        {
            resultReg = (regNumber)((unsigned)(REG_FLOATRET) + idx * 2); // d0, d1, d2 or d3
        }
        else
        {
            resultReg = (regNumber)((unsigned)(REG_FLOATRET) + idx); // f0, f1, f2 or f3
        }
    }

#elif defined(TARGET_ARM64)

    var_types regType = GetReturnRegType(idx);
    if (varTypeIsIntegralOrI(regType))
    {
        noway_assert(idx < 2);                              // Up to 2 return registers for 16-byte structs
        resultReg = (idx == 0) ? REG_INTRET : REG_INTRET_1; // X0 or X1
    }
    else
    {
        noway_assert(idx < 4);                                   // Up to 4 return registers for HFA's
        resultReg = (regNumber)((unsigned)(REG_FLOATRET) + idx); // V0, V1, V2 or V3
    }

#elif defined(TARGET_LOONGARCH64) || defined(TARGET_RISCV64)
    var_types regType = GetReturnRegType(idx);
    if (idx == 0)
    {
        resultReg = varTypeIsIntegralOrI(regType) ? REG_INTRET : REG_FLOATRET; // A0 or F0
    }
    else
    {
        noway_assert(idx == 1); // Up to 2 return registers for two-float-field structs

        // If the first return register is from the same register file, return the one next to it.
        if (varTypeUsesIntReg(regType))
        {
            resultReg = varTypeIsIntegralOrI(GetReturnRegType(0)) ? REG_INTRET_1 : REG_INTRET; // A0 or A1
        }
        else
        {
            assert(varTypeUsesFloatReg(regType));
            resultReg = varTypeIsIntegralOrI(GetReturnRegType(0)) ? REG_FLOATRET : REG_FLOATRET_1; // F0 or F1
        }
    }

#endif // TARGET_XXX

    assert(resultReg != REG_NA);
    return resultReg;
}

//--------------------------------------------------------------------------------
// GetABIReturnRegs: get the mask of return registers as per target arch ABI.
//
// Arguments:
//    None
//
// Return Value:
//    reg mask of return registers in which the return type is returned.
//
// Note:
//    This routine can be used when the caller is not particular about the order
//    of return registers and wants to know the set of return registers.
//
// static
regMaskTP ReturnTypeDesc::GetABIReturnRegs() const
{
    regMaskTP resultMask = RBM_NONE;

    unsigned count = GetReturnRegCount();
    for (unsigned i = 0; i < count; ++i)
    {
        resultMask |= genRegMask(GetABIReturnReg(i));
    }

    return resultMask;
}

//------------------------------------------------------------------------
// The following functions manage the gtRsvdRegs set of temporary registers
// created by LSRA during code generation.

//------------------------------------------------------------------------
// AvailableTempRegCount: return the number of available temporary registers in the (optional) given set
// (typically, RBM_ALLINT or RBM_ALLFLOAT).
//
// Arguments:
//    mask - (optional) Check for available temporary registers only in this set.
//
// Return Value:
//    Count of available temporary registers in given set.
//
unsigned GenTree::AvailableTempRegCount(regMaskTP mask /* = (regMaskTP)-1 */) const
{
    return genCountBits(gtRsvdRegs & mask);
}

//------------------------------------------------------------------------
// GetSingleTempReg: There is expected to be exactly one available temporary register
// in the given mask in the gtRsvdRegs set. Get that register. No future calls to get
// a temporary register are expected. Removes the register from the set, but only in
// DEBUG to avoid doing unnecessary work in non-DEBUG builds.
//
// Arguments:
//    mask - (optional) Get an available temporary register only in this set.
//
// Return Value:
//    Available temporary register in given mask.
//
regNumber GenTree::GetSingleTempReg(regMaskTP mask /* = (regMaskTP)-1 */)
{
    regMaskTP availableSet = gtRsvdRegs & mask;
    assert(genCountBits(availableSet) == 1);
    regNumber tempReg = genRegNumFromMask(availableSet);
    INDEBUG(gtRsvdRegs &= ~availableSet;) // Remove the register from the set, so it can't be used again.
    return tempReg;
}

//------------------------------------------------------------------------
// ExtractTempReg: Find the lowest number temporary register from the gtRsvdRegs set
// that is also in the optional given mask (typically, RBM_ALLINT or RBM_ALLFLOAT),
// and return it. Remove this register from the temporary register set, so it won't
// be returned again.
//
// Arguments:
//    mask - (optional) Extract an available temporary register only in this set.
//
// Return Value:
//    Available temporary register in given mask.
//
regNumber GenTree::ExtractTempReg(regMaskTP mask /* = (regMaskTP)-1 */)
{
    regMaskTP availableSet = gtRsvdRegs & mask;
    assert(genCountBits(availableSet) >= 1);
    regNumber tempReg = genFirstRegNumFromMask(availableSet);
    gtRsvdRegs ^= genRegMask(tempReg);
    return tempReg;
}

//------------------------------------------------------------------------
// GetNum: Get the SSA number for a given field.
//
// Arguments:
//    compiler - The Compiler instance
//    index    - The field index
//
// Return Value:
//    The SSA number corresponding to the field at "index".
//
unsigned SsaNumInfo::GetNum(Compiler* compiler, unsigned index) const
{
    assert(IsComposite());
    if (HasCompactFormat())
    {
        return (m_value >> (index * BITS_PER_SIMPLE_NUM)) & SIMPLE_NUM_MASK;
    }

    // We expect this case to be very rare (outside stress).
    return *GetOutlinedNumSlot(compiler, index);
}

//------------------------------------------------------------------------
// GetOutlinedNumSlot: Get a pointer the "outlined" SSA number for a field.
//
// Arguments:
//    compiler - The Compiler instance
//    index    - The field index
//
// Return Value:
//    Pointer to the SSA number corresponding to the field at "index".
//
unsigned* SsaNumInfo::GetOutlinedNumSlot(Compiler* compiler, unsigned index) const
{
    assert(IsComposite() && !HasCompactFormat());

    // The "outlined" format for a composite number encodes a 30-bit-sized index.
    // First, extract it: this will need "bit stitching" from the two parts.
    unsigned outIndexLow  = m_value & OUTLINED_INDEX_LOW_MASK;
    unsigned outIndexHigh = (m_value & OUTLINED_INDEX_HIGH_MASK) >> 1;
    unsigned outIndex     = outIndexLow | outIndexHigh;

    return &compiler->m_outlinedCompositeSsaNums->GetRefNoExpand(outIndex + index);
}

//------------------------------------------------------------------------
// NumCanBeEncodedCompactly: Can the given field ref be encoded compactly?
//
// Arguments:
//    ssaNum - The SSA number
//    index  - The field index
//
// Return Value:
//    Whether the ref of the field at "index" can be encoded through the
//    "compact" encoding scheme.
//
// Notes:
//    Under stress, we randomly reduce the number of refs that can be
//    encoded compactly, to stress the outlined encoding logic.
//
/* static */ bool SsaNumInfo::NumCanBeEncodedCompactly(unsigned index, unsigned ssaNum)
{
#ifdef DEBUG
    if (JitTls::GetCompiler()->compStressCompile(Compiler::STRESS_SSA_INFO, 20))
    {
        return (ssaNum - 2) < index;
    }
#endif // DEBUG

    assert(index < MAX_NumOfFieldsInPromotableStruct);

    return (ssaNum <= MAX_SIMPLE_NUM) &&
           ((index < SIMPLE_NUM_COUNT) || (SIMPLE_NUM_COUNT <= MAX_NumOfFieldsInPromotableStruct));
}

//------------------------------------------------------------------------
// Composite: Form a composite SSA number, one capable of representing refs
//    to more than one SSA local.
//
// Arguments:
//    baseNum      - The SSA number to base the new one on (composite/invalid)
//    compiler     - The Compiler instance
//    parentLclNum - The promoted local representing a "whole" ref
//    index        - The field index
//    ssaNum       - The SSA number
//
// Return Value:
//    A new, always composite, SSA number that represents all of the refs
//    in "baseNum", with the field at "index" set to "ssaNum".
//
// Notes:
//    It is assumed that the new number represents the same "whole" ref as
//    the old one (the same parent local). If the SSA number needs to be
//    reset fully, a new, RESERVED one should be created, and composed from
//    with the appropriate parent reference.
//
/* static */ SsaNumInfo SsaNumInfo::Composite(
    SsaNumInfo baseNum, Compiler* compiler, unsigned parentLclNum, unsigned index, unsigned ssaNum)
{
    assert(baseNum.IsInvalid() || baseNum.IsComposite());
    assert(compiler->lvaGetDesc(parentLclNum)->lvPromoted);

    if (NumCanBeEncodedCompactly(index, ssaNum) && (baseNum.IsInvalid() || baseNum.HasCompactFormat()))
    {
        unsigned ssaNumEncoded = ssaNum << (index * BITS_PER_SIMPLE_NUM);
        if (baseNum.IsInvalid())
        {
            return SsaNumInfo(COMPOSITE_ENCODING_BIT | ssaNumEncoded);
        }

        return SsaNumInfo(ssaNumEncoded | (baseNum.m_value & ~(SIMPLE_NUM_MASK << (index * BITS_PER_SIMPLE_NUM))));
    }

    if (!baseNum.IsInvalid() && !baseNum.HasCompactFormat())
    {
        *baseNum.GetOutlinedNumSlot(compiler, index) = ssaNum;
        return baseNum;
    }

    // This is the only path where we can encounter a null table.
    if (compiler->m_outlinedCompositeSsaNums == nullptr)
    {
        CompAllocator alloc                  = compiler->getAllocator(CMK_SSA);
        compiler->m_outlinedCompositeSsaNums = new (alloc) JitExpandArrayStack<unsigned>(alloc);
    }

    // Allocate a new chunk for the field numbers. Once allocated, it cannot be expanded.
    int                            count      = compiler->lvaGetDesc(parentLclNum)->lvFieldCnt;
    JitExpandArrayStack<unsigned>* table      = compiler->m_outlinedCompositeSsaNums;
    int                            outIdx     = table->Size();
    unsigned*                      pLastSlot  = &table->GetRef(outIdx + count - 1); // This will grow the table.
    unsigned*                      pFirstSlot = pLastSlot - count + 1;

    // Copy over all of the already encoded numbers.
    if (!baseNum.IsInvalid())
    {
        for (int i = 0; i < SIMPLE_NUM_COUNT; i++)
        {
            pFirstSlot[i] = baseNum.GetNum(compiler, i);
        }
    }

    // Copy the one being set last to overwrite any previous values.
    pFirstSlot[index] = ssaNum;

    // Split the index if it does not fit into a small encoding.
    if ((outIdx & ~OUTLINED_INDEX_LOW_MASK) != 0)
    {
        int outIdxLow  = outIdx & OUTLINED_INDEX_LOW_MASK;
        int outIdxHigh = (outIdx << 1) & OUTLINED_INDEX_HIGH_MASK;
        outIdx         = outIdxLow | outIdxHigh;
    }

    return SsaNumInfo(COMPOSITE_ENCODING_BIT | OUTLINED_ENCODING_BIT | outIdx);
}

//------------------------------------------------------------------------
// GetLclOffs: if `this` is a field or a field address it returns offset
// of the field inside the struct, for not a field it returns 0.
//
// Return Value:
//    The offset value.
//
uint16_t GenTreeLclVarCommon::GetLclOffs() const
{
    if (OperIsLocalField())
    {
        return AsLclFld()->GetLclOffs();
    }
    else
    {
        return 0;
    }
}

//------------------------------------------------------------------------
// GetLayout: get the struct layout for a local node of struct type.
//
// Arguments:
//    compiler - the compiler instance
//
// Return Value:
//    If "this" is a local field node, the layout stored in the node,
//    otherwise the layout of local itself.
//
ClassLayout* GenTreeLclVarCommon::GetLayout(Compiler* compiler) const
{
    assert(varTypeIsStruct(TypeGet()));

    if (OperIs(GT_LCL_VAR, GT_STORE_LCL_VAR))
    {
        return compiler->lvaGetDesc(GetLclNum())->GetLayout();
    }

    assert(OperIs(GT_LCL_FLD, GT_STORE_LCL_FLD));
    return AsLclFld()->GetLayout();
}

//------------------------------------------------------------------------
// GenTreeLclVar::IsNeverNegative: Gets true if the lcl var is never negative; otherwise false.
//
// Arguments:
//    comp - the compiler instance
//
// Return Value:
//    true if the lcl var is never negative; otherwise false.
//
bool GenTreeLclVar::IsNeverNegative(Compiler* comp) const
{
    assert(OperIs(GT_LCL_VAR));
    return comp->lvaGetDesc(GetLclNum())->IsNeverNegative();
}

#if defined(TARGET_XARCH) && defined(FEATURE_HW_INTRINSICS)
//------------------------------------------------------------------------
// GetResultOpNumForRmwIntrinsic: check if the result is written into one of the operands.
// In the case that none of the operand is overwritten, check if any of them is lastUse.
//
// Return Value:
//     The operand number overwritten or lastUse. 0 is the default value, where the result is written into
//      a destination that is not one of the source operands and there is no last use op.
//
unsigned GenTreeHWIntrinsic::GetResultOpNumForRmwIntrinsic(GenTree* use, GenTree* op1, GenTree* op2, GenTree* op3)
{
    assert(HWIntrinsicInfo::IsFmaIntrinsic(gtHWIntrinsicId) || HWIntrinsicInfo::IsPermuteVar2x(gtHWIntrinsicId));

    if (use != nullptr && use->OperIs(GT_STORE_LCL_VAR))
    {
        // For store_lcl_var, check if any op is overwritten

        GenTreeLclVarCommon* overwritten       = use->AsLclVarCommon();
        unsigned             overwrittenLclNum = overwritten->GetLclNum();

        if (op1->IsLocal() && (op1->AsLclVarCommon()->GetLclNum() == overwrittenLclNum))
        {
            return 1;
        }
        else if (op2->IsLocal() && (op2->AsLclVarCommon()->GetLclNum() == overwrittenLclNum))
        {
            return 2;
        }
        else if (op3->IsLocal() && (op3->AsLclVarCommon()->GetLclNum() == overwrittenLclNum))
        {
            return 3;
        }
    }

    // If no overwritten op, check if there is any last use op
    // https://github.com/dotnet/runtime/issues/62215

    if (op1->OperIs(GT_LCL_VAR) && op1->IsLastUse(0))
    {
        return 1;
    }
    else if (op2->OperIs(GT_LCL_VAR) && op2->IsLastUse(0))
    {
        return 2;
    }
    else if (op3->OperIs(GT_LCL_VAR) && op3->IsLastUse(0))
    {
        return 3;
    }

    return 0;
}

//------------------------------------------------------------------------
// GetTernaryControlByte: calculate the value of the control byte for ternary node
// with given logic nodes on the input.
//
// Return value: the value of the ternary control byte.
uint8_t GenTreeHWIntrinsic::GetTernaryControlByte(GenTreeHWIntrinsic* second) const
{
    // we assume we have a structure like:
    /*
               /- A
               +- B
        t1 = binary logical op1

               /- C
               +- t1
        t2 = binary logical op2
    */

    // To calculate the control byte value:
    // The way the constants work is we have three keys:
    // * A: 0xF0
    // * B: 0xCC
    // * C: 0xAA
    //
    // To compute the correct control byte, you simply perform the corresponding operation on these keys. So, if you
    // wanted to do (A & B) ^ C, you would compute (0xF0 & 0xCC) ^ 0xAA or 0x6A.
    assert(second->Op(1) == this || second->Op(2) == this);
    const uint8_t A = 0xF0;
    const uint8_t B = 0xCC;
    const uint8_t C = 0xAA;

    genTreeOps firstOper  = HWOperGet();
    genTreeOps secondOper = second->HWOperGet();

    uint8_t AB  = 0;
    uint8_t ABC = 0;

    if (firstOper == GT_AND)
    {
        AB = A & B;
    }
    else if (firstOper == GT_OR)
    {
        AB = A | B;
    }
    else if (firstOper == GT_XOR)
    {
        AB = A ^ B;
    }
    else
    {
        unreached();
    }

    if (secondOper == GT_AND)
    {
        ABC = AB & C;
    }
    else if (secondOper == GT_OR)
    {
        ABC = AB | C;
    }
    else if (secondOper == GT_XOR)
    {
        ABC = AB ^ C;
    }
    else
    {
        unreached();
    }

    return ABC;
}
#endif // TARGET_XARCH && FEATURE_HW_INTRINSICS

unsigned GenTreeLclFld::GetSize() const
{
    return TypeIs(TYP_STRUCT) ? GetLayout()->GetSize() : genTypeSize(TypeGet());
}

#ifdef TARGET_ARM
//------------------------------------------------------------------------
// IsOffsetMisaligned: check if the field needs a special handling on arm.
//
// Return Value:
//    true if it is a float field with a misaligned offset, false otherwise.
//
bool GenTreeLclFld::IsOffsetMisaligned() const
{
    if (varTypeIsFloating(gtType))
    {
        return ((m_lclOffs % emitTypeSize(TYP_FLOAT)) != 0);
    }
    return false;
}
#endif // TARGET_ARM

bool GenTree::IsInvariant() const
{
    return OperIsConst() || OperIs(GT_LCL_ADDR) || OperIs(GT_FTN_ADDR);
}

//------------------------------------------------------------------------
// IsNeverNegative: returns true if the given tree is known to be never
//                  negative, i. e. the upper bit will always be zero.
//                  Only valid for integral types.
//
// Arguments:
//    comp - Compiler object, needed for IntegralRange::ForNode
//
// Return Value:
//    true if the given tree is known to be never negative
//
bool GenTree::IsNeverNegative(Compiler* comp) const
{
    assert(varTypeIsIntegral(this));

    if (IsIntegralConst())
    {
        return AsIntConCommon()->IntegralValue() >= 0;
    }

    if (OperIs(GT_LCL_VAR))
    {
        if (AsLclVar()->IsNeverNegative(comp))
        {
            // This is an early exit, it doesn't cover all cases
            return true;
        }
    }

    // TODO-Casts: extend IntegralRange to handle constants
    return IntegralRange::ForNode(const_cast<GenTree*>(this), comp).IsNonNegative();
}

//------------------------------------------------------------------------
// IsNeverNegativeOne: returns true if the given tree is known to never be
//                     be negative one. Only valid for integral types.
//
// Arguments:
//    comp - Compiler object, needed for IsNeverNegative
//
// Return Value:
//    true if the given tree is known to never be negative one
//
bool GenTree::IsNeverNegativeOne(Compiler* comp) const
{
    assert(varTypeIsIntegral(this));

    if (this->IsNeverNegative(comp))
        return true;

    if (this->IsIntegralConst())
    {
        return !this->IsIntegralConst(-1);
    }

    return false;
}

//------------------------------------------------------------------------
// IsNeverZero: returns true if the given tree is known to never be zero.
//              Only valid for integral types.
//
// Return Value:
//    true if the given tree is known to never be zero
//
bool GenTree::IsNeverZero() const
{
    assert(varTypeIsIntegral(this));

    if (this->IsIntegralConst())
    {
        return !this->IsIntegralConst(0);
    }

    return false;
}

//------------------------------------------------------------------------
// CanDivOrModPossiblyOverflow: returns true if the given tree is known
//                              to possibly overflow on a division.
//                              Only valid for integral types.
//                              Only valid for signed-div/signed-mod.
//
// Arguments:
//    comp - Compiler object, needed for IsNeverNegativeOne
//
// Return Value:
//    true if the given tree is known to possibly overflow on a division
//
bool GenTree::CanDivOrModPossiblyOverflow(Compiler* comp) const
{
    assert(this->OperIs(GT_DIV, GT_MOD));
    assert(varTypeIsIntegral(this));

    if (this->gtFlags & GTF_DIV_MOD_NO_OVERFLOW)
        return false;

    GenTree* op1 = this->gtGetOp1()->gtSkipReloadOrCopy();
    GenTree* op2 = this->gtGetOp2()->gtSkipReloadOrCopy();

    // If the divisor is known to never be '-1', we cannot overflow.
    if (op2->IsNeverNegativeOne(comp))
        return false;

    // If the dividend is a constant with a minimum value with respect to the division's type, then we might overflow
    // as we do not know if the divisor will be '-1' or not at this point.
    if (op1->IsIntegralConst())
    {
        if (this->TypeIs(TYP_INT) && op1->IsIntegralConst(INT32_MIN))
        {
            return true;
        }
        else if (this->TypeIs(TYP_LONG) && (op1->AsIntConCommon()->IntegralValue() == INT64_MIN))
        {
            return true;
        }

        // Dividend is not a minimum value; therefore we cannot overflow.
        return false;
    }

    // Not enough known information; therefore we might overflow.
    return true;
}
