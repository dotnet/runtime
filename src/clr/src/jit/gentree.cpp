// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                               GenTree                                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#include "simd.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

/*****************************************************************************/

const unsigned short GenTree::gtOperKindTable[] = {
#define GTNODE(en, st, cm, ok) ok + GTK_COMMUTE *cm,
#include "gtlist.h"
};

#ifdef LEGACY_BACKEND
/*****************************************************************************/
// static
genTreeOps GenTree::OpAsgToOper(genTreeOps op)
{
    // Precondition.
    assert(OperIsAssignment(op) && op != GT_ASG);
    switch (op)
    {
        case GT_ASG_ADD:
            return GT_ADD;
        case GT_ASG_SUB:
            return GT_SUB;
        case GT_ASG_MUL:
            return GT_MUL;
        case GT_ASG_DIV:
            return GT_DIV;
        case GT_ASG_MOD:
            return GT_MOD;

        case GT_ASG_UDIV:
            return GT_UDIV;
        case GT_ASG_UMOD:
            return GT_UMOD;

        case GT_ASG_OR:
            return GT_OR;
        case GT_ASG_XOR:
            return GT_XOR;
        case GT_ASG_AND:
            return GT_AND;
        case GT_ASG_LSH:
            return GT_LSH;
        case GT_ASG_RSH:
            return GT_RSH;
        case GT_ASG_RSZ:
            return GT_RSZ;

        case GT_CHS:
            return GT_NEG;

        default:
            unreached(); // Precondition implies we don't get here.
    }
}
#endif // LEGACY_BACKEND

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
    ICEmbedded,
    ICTerminal,
    ICError,
    IndentCharCount
};

// clang-format off
// Sets of strings for different dumping options            vert             bot             top             mid             dash       embedded    terminal    error
static const char*  emptyIndents[IndentCharCount]   = {     " ",             " ",            " ",            " ",            " ",           "{",      "",        "?"  };
static const char*  asciiIndents[IndentCharCount]   = {     "|",            "\\",            "/",            "+",            "-",           "{",      "*",       "?"  };
static const char*  unicodeIndents[IndentCharCount] = { "\xe2\x94\x82", "\xe2\x94\x94", "\xe2\x94\x8c", "\xe2\x94\x9c", "\xe2\x94\x80",     "{", "\xe2\x96\x8c", "?"  };
// clang-format on

typedef ArrayStack<Compiler::IndentInfo> IndentInfoStack;
struct IndentStack
{
    IndentInfoStack stack;
    const char**    indents;

    // Constructor for IndentStack.  Uses 'compiler' to determine the mode of printing.
    IndentStack(Compiler* compiler) : stack(compiler)
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
            switch (stack.Index(index))
            {
                case Compiler::IndentInfo::IINone:
                    printf("   ");
                    break;
                case Compiler::IndentInfo::IIEmbedded:
                    printf("%s  ", indents[ICEmbedded]);
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

#if defined(DEBUG) || NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS

static const char* opNames[] = {
#define GTNODE(en, st, cm, ok) #en,
#include "gtlist.h"
};

const char* GenTree::OpName(genTreeOps op)
{
    assert((unsigned)op < sizeof(opNames) / sizeof(opNames[0]));

    return opNames[op];
}

#endif

#if MEASURE_NODE_SIZE && SMALL_TREE_NODES

static const char* opStructNames[] = {
#define GTNODE(en, st, cm, ok) #st,
#include "gtlist.h"
};

const char* GenTree::OpStructName(genTreeOps op)
{
    assert((unsigned)op < sizeof(opStructNames) / sizeof(opStructNames[0]));

    return opStructNames[op];
}

#endif

/*****************************************************************************
 *
 *  When 'SMALL_TREE_NODES' is enabled, we allocate tree nodes in 2 different
 *  sizes: 'TREE_NODE_SZ_SMALL' for most nodes and 'TREE_NODE_SZ_LARGE' for the
 *  few nodes (such as calls) that have more fields and take up a lot more space.
 */

#if SMALL_TREE_NODES

/* GT_COUNT'th oper is overloaded as 'undefined oper', so allocate storage for GT_COUNT'th oper also */
/* static */
unsigned char GenTree::s_gtNodeSizes[GT_COUNT + 1];

#if NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS

unsigned char GenTree::s_gtTrueSizes[GT_COUNT + 1]{
#define GTNODE(en, st, cm, ok) sizeof(st),
#include "gtlist.h"
};

#endif // NODEBASH_STATS || MEASURE_NODE_SIZE || COUNT_AST_OPERS

#if COUNT_AST_OPERS
LONG GenTree::s_gtNodeCounts[GT_COUNT + 1] = {0};
#endif // COUNT_AST_OPERS

/* static */
void GenTree::InitNodeSize()
{
    /* 'GT_LCL_VAR' often gets changed to 'GT_REG_VAR' */

    assert(GenTree::s_gtNodeSizes[GT_LCL_VAR] >= GenTree::s_gtNodeSizes[GT_REG_VAR]);

    /* Set all sizes to 'small' first */

    for (unsigned op = 0; op <= GT_COUNT; op++)
    {
        GenTree::s_gtNodeSizes[op] = TREE_NODE_SZ_SMALL;
    }

    // Now set all of the appropriate entries to 'large'
    CLANG_FORMAT_COMMENT_ANCHOR;

// clang-format off
#if defined(FEATURE_HFA) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
    // On ARM32, ARM64 and System V for struct returning
    // there is code that does GT_ASG-tree.CopyObj call.
    // CopyObj is a large node and the GT_ASG is small, which triggers an exception.
    GenTree::s_gtNodeSizes[GT_ASG]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_RETURN]           = TREE_NODE_SZ_LARGE;
#endif // defined(FEATURE_HFA) || defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)

    GenTree::s_gtNodeSizes[GT_CALL]             = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_CAST]             = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_FTN_ADDR]         = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_BOX]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_INDEX]            = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_INDEX_ADDR]        = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_ARR_BOUNDS_CHECK] = TREE_NODE_SZ_LARGE;
#ifdef FEATURE_SIMD
    GenTree::s_gtNodeSizes[GT_SIMD_CHK] = TREE_NODE_SZ_LARGE;
#endif // FEATURE_SIMD
    GenTree::s_gtNodeSizes[GT_ARR_ELEM]         = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_ARR_INDEX]        = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_ARR_OFFSET]       = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_RET_EXPR]         = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_OBJ]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_FIELD]            = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_STMT]             = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_CMPXCHG]          = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_QMARK]            = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_LEA]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_STORE_OBJ]        = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_DYN_BLK]          = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_STORE_DYN_BLK]    = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_INTRINSIC]        = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_ALLOCOBJ]         = TREE_NODE_SZ_LARGE;
#if USE_HELPERS_FOR_INT_DIV
    GenTree::s_gtNodeSizes[GT_DIV]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_UDIV]             = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_MOD]              = TREE_NODE_SZ_LARGE;
    GenTree::s_gtNodeSizes[GT_UMOD]             = TREE_NODE_SZ_LARGE;
#endif
#ifdef FEATURE_PUT_STRUCT_ARG_STK
    // TODO-Throughput: This should not need to be a large node. The object info should be
    // obtained from the child node.
    GenTree::s_gtNodeSizes[GT_PUTARG_STK]       = TREE_NODE_SZ_LARGE;
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
    GenTree::s_gtNodeSizes[GT_PUTARG_SPLIT]     = TREE_NODE_SZ_LARGE;
#endif
#endif // FEATURE_PUT_STRUCT_ARG_STK

    assert(GenTree::s_gtNodeSizes[GT_RETURN] == GenTree::s_gtNodeSizes[GT_ASG]);

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
#ifndef LEGACY_BACKEND
    static_assert_no_msg(sizeof(GenTreeJumpTable)    <= TREE_NODE_SZ_SMALL);
#endif // !LEGACY_BACKEND
    static_assert_no_msg(sizeof(GenTreeIntCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLngCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeDblCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeStrCon)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLclVarCommon) <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLclVar)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLclFld)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeRegVar)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCC)           <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCast)         <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeBox)          <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeField)        <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArgList)      <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeFieldList)    <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeColon)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeCall)         <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeCmpXchg)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeFptrVal)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeQmark)        <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIntrinsic)    <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIndex)        <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArrLen)       <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeBoundsChk)    <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArrElem)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArrIndex)     <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeArrOffs)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeIndir)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeStoreInd)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeAddrMode)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeObj)          <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeBlk)          <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeRetExpr)      <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeStmt)         <= TREE_NODE_SZ_LARGE); // *** large node
    static_assert_no_msg(sizeof(GenTreeClsVar)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeArgPlace)     <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeLabel)        <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreePhiArg)       <= TREE_NODE_SZ_SMALL);
    static_assert_no_msg(sizeof(GenTreeAllocObj)     <= TREE_NODE_SZ_LARGE); // *** large node
#ifndef FEATURE_PUT_STRUCT_ARG_STK
    static_assert_no_msg(sizeof(GenTreePutArgStk)    <= TREE_NODE_SZ_SMALL);
#else  // FEATURE_PUT_STRUCT_ARG_STK
    // TODO-Throughput: This should not need to be a large node. The object info should be
    // obtained from the child node.
    static_assert_no_msg(sizeof(GenTreePutArgStk)    <= TREE_NODE_SZ_LARGE);
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
    static_assert_no_msg(sizeof(GenTreePutArgSplit)  <= TREE_NODE_SZ_LARGE);
#endif
#endif // FEATURE_PUT_STRUCT_ARG_STK

#ifdef FEATURE_SIMD
    static_assert_no_msg(sizeof(GenTreeSIMD)         <= TREE_NODE_SZ_SMALL);
#endif // FEATURE_SIMD
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

#if SMALL_TREE_NODES
//------------------------------------------------------------------------
// ReplaceWith: replace this with the src node. The source must be an isolated node
//              and cannot be used after the replacement.
//
// Arguments:
//    src  - source tree, that replaces this.
//    comp - the compiler instance to transfer annotations for arrays.
//
void GenTree::ReplaceWith(GenTree* src, Compiler* comp)
{
    // The source may be big only if the target is also a big node
    assert((gtDebugFlags & GTF_DEBUG_NODE_LARGE) || GenTree::s_gtNodeSizes[src->gtOper] == TREE_NODE_SZ_SMALL);

    // The check is effective only if nodes have been already threaded.
    assert((src->gtPrev == nullptr) && (src->gtNext == nullptr));

    RecordOperBashing(OperGet(), src->OperGet()); // nop unless NODEBASH_STATS is enabled

    GenTreePtr prev = gtPrev;
    GenTreePtr next = gtNext;
    // The VTable pointer is copied intentionally here
    memcpy((void*)this, (void*)src, src->GetNodeSize());
    this->gtPrev = prev;
    this->gtNext = next;

#ifdef DEBUG
    gtSeqNum = 0;
#endif
    // Transfer any annotations.
    if (src->OperGet() == GT_IND && src->gtFlags & GTF_IND_ARR_INDEX)
    {
        ArrayInfo arrInfo;
        bool      b = comp->GetArrayInfoMap()->Lookup(src, &arrInfo);
        assert(b);
        comp->GetArrayInfoMap()->Set(this, arrInfo);
    }
    DEBUG_DESTROY_NODE(src);
}

#endif

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

#else // SMALL_TREE_NODES

#ifdef DEBUG
bool GenTree::IsNodeProperlySized() const
{
    return true;
}
#endif

#endif // SMALL_TREE_NODES

/*****************************************************************************/

#if MEASURE_NODE_SIZE

void GenTree::DumpNodeSizes(FILE* fp)
{
// Dump the sizes of the various GenTree flavors

#if SMALL_TREE_NODES
    fprintf(fp, "Small tree node size = %3u bytes\n", TREE_NODE_SZ_SMALL);
#endif
    fprintf(fp, "Large tree node size = %3u bytes\n", TREE_NODE_SZ_LARGE);
    fprintf(fp, "\n");

#if SMALL_TREE_NODES

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

#endif
}

#endif // MEASURE_NODE_SIZE

/*****************************************************************************
 *
 *  Walk all basic blocks and call the given function pointer for all tree
 *  nodes contained therein.
 */

void Compiler::fgWalkAllTreesPre(fgWalkPreFn* visitor, void* pCallBackData)
{
    BasicBlock* block;

    for (block = fgFirstBB; block; block = block->bbNext)
    {
        GenTreePtr tree;

        for (tree = block->bbTreeList; tree; tree = tree->gtNext)
        {
            assert(tree->gtOper == GT_STMT);

            fgWalkTreePre(&tree->gtStmt.gtStmtExpr, visitor, pCallBackData);
        }
    }
}

//-----------------------------------------------------------
// CopyReg: Copy the _gtRegNum/_gtRegPair/gtRegTag fields.
//
// Arguments:
//     from   -  GenTree node from which to copy
//
// Return Value:
//     None
void GenTree::CopyReg(GenTreePtr from)
{
    // To do the copy, use _gtRegPair, which must be bigger than _gtRegNum. Note that the values
    // might be undefined (so gtRegTag == GT_REGTAG_NONE).
    _gtRegPair = from->_gtRegPair;
    C_ASSERT(sizeof(_gtRegPair) >= sizeof(_gtRegNum));
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
// gtHasReg: Whether node beeen assigned a register by LSRA
//
// Arguments:
//    None
//
// Return Value:
//    Returns true if the node was assigned a register.
//
//    In case of multi-reg call nodes, it is considered
//    having a reg if regs are allocated for all its
//    return values.
//
//    In case of GT_COPY or GT_RELOAD of a multi-reg call,
//    GT_COPY/GT_RELOAD is considered having a reg if it
//    has a reg assigned to any of its positions.
//
// Assumption:
//    In order for this to work properly, gtClearReg must be called
//    prior to setting the register value.
//
bool GenTree::gtHasReg() const
{
    bool hasReg;

#if CPU_LONG_USES_REGPAIR
    if (isRegPairType(TypeGet()))
    {
        assert(_gtRegNum != REG_NA);
        INDEBUG(assert(gtRegTag == GT_REGTAG_REGPAIR));
        return (gtRegPair != REG_PAIR_NONE);
    }
    assert(_gtRegNum != REG_PAIR_NONE);
    INDEBUG(assert(gtRegTag == GT_REGTAG_REG));
#endif
    if (IsMultiRegCall())
    {
        // Have to cast away const-ness because GetReturnTypeDesc() is a non-const method
        GenTree*     tree     = const_cast<GenTree*>(this);
        GenTreeCall* call     = tree->AsCall();
        unsigned     regCount = call->GetReturnTypeDesc()->GetReturnRegCount();
        hasReg                = false;

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
        GenTree*             tree         = const_cast<GenTree*>(this);
        GenTreeCopyOrReload* copyOrReload = tree->AsCopyOrReload();
        GenTreeCall*         call         = copyOrReload->gtGetOp1()->AsCall();
        unsigned             regCount     = call->GetReturnTypeDesc()->GetReturnRegCount();
        hasReg                            = false;

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
    else
    {
        hasReg = (gtRegNum != REG_NA);
    }

    return hasReg;
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

#if CPU_LONG_USES_REGPAIR
    if (isRegPairType(TypeGet()))
    {
        resultMask = genRegPairMask(gtRegPair);
    }
    else
#endif
    {
        if (IsMultiRegCall())
        {
            // temporarily cast away const-ness as AsCall() method is not declared const
            resultMask    = genRegMask(gtRegNum);
            GenTree* temp = const_cast<GenTree*>(this);
            resultMask |= temp->AsCall()->GetOtherRegMask();
        }
        else if (IsCopyOrReloadOfMultiRegCall())
        {
            // A multi-reg copy or reload, will have valid regs for only those
            // positions that need to be copied or reloaded.  Hence we need
            // to consider only those registers for computing reg mask.

            GenTree*             tree         = const_cast<GenTree*>(this);
            GenTreeCopyOrReload* copyOrReload = tree->AsCopyOrReload();
            GenTreeCall*         call         = copyOrReload->gtGetOp1()->AsCall();
            unsigned             regCount     = call->GetReturnTypeDesc()->GetReturnRegCount();

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
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
        else if (OperIsPutArgSplit())
        {
            GenTree*            tree     = const_cast<GenTree*>(this);
            GenTreePutArgSplit* splitArg = tree->AsPutArgSplit();
            unsigned            regCount = splitArg->gtNumRegs;

            resultMask = RBM_NONE;
            for (unsigned i = 0; i < regCount; ++i)
            {
                regNumber reg = splitArg->GetRegNumByIdx(i);
                assert(reg != REG_NA);
                resultMask |= genRegMask(reg);
            }
        }
#endif
        else
        {
            resultMask = genRegMask(gtRegNum);
        }
    }

    return resultMask;
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
//    Copiler - the compiler context.
//
// Returns:
//    True if the call is pure; false otherwise.
//
bool GenTreeCall::IsPure(Compiler* compiler) const
{
    return (gtCallType == CT_HELPER) &&
           compiler->s_helperCallProperties.IsPure(compiler->eeGetHelperNum(gtCallMethHnd));
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

    // If we also care about exceptions then check if the helper can throw
    if (!ignoreExceptions && !helperProperties.NoThrow(helper))
    {
        return true;
    }

    // If this is not a Pure helper call or an allocator (that will not need to run a finalizer)
    // then this call has side effects.
    return !helperProperties.IsPure(helper) &&
           (!helperProperties.IsAllocator(helper) || helperProperties.MayFinalize(helper));
}

#ifndef LEGACY_BACKEND

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
    else if (gtCallType == CT_INDIRECT)
    {
        if (IsVirtualStub())
        {
            // R11 = Virtual stub param
            return 1;
        }
        else if (gtCallCookie != nullptr)
        {
            // R10 = PInvoke target param
            // R11 = PInvoke cookie param
            return 2;
        }
    }
    return 0;
}

#endif // !LEGACY_BACKEND

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
//     These two Jit Helpers that we handle here by returning true
//     aren't actually defined to return a struct, so they don't expect
//     their RetBuf to be passed in x8, instead they  expect it in x0.
//
bool GenTreeCall::TreatAsHasRetBufArg(Compiler* compiler) const
{
    if (HasRetBufArg())
    {
        return true;
    }
    else
    {
        // If we see a Jit helper call that returns a TYP_STRUCT we will
        // transform it as if it has a Return Buffer Argument
        //
        if (IsHelperCall() && (gtReturnType == TYP_STRUCT))
        {
            // There are two possible helper calls that use this path:
            //  CORINFO_HELP_GETFIELDSTRUCT and CORINFO_HELP_UNBOX_NULLABLE
            //
            CorInfoHelpFunc helpFunc = compiler->eeGetHelperNum(gtCallMethHnd);

            if (helpFunc == CORINFO_HELP_GETFIELDSTRUCT)
            {
                return true;
            }
            else if (helpFunc == CORINFO_HELP_UNBOX_NULLABLE)
            {
                return true;
            }
            else
            {
                assert(!"Unexpected JIT helper in TreatAsHasRetBufArg");
            }
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

//------------------------------------------------------------------------
// GenTreeCall::ReplaceCallOperand:
//    Replaces a given operand to a call node and updates the call
//    argument table if necessary.
//
// Arguments:
//    useEdge - the use edge that points to the operand to be replaced.
//    replacement - the replacement node.
//
void GenTreeCall::ReplaceCallOperand(GenTree** useEdge, GenTree* replacement)
{
    assert(useEdge != nullptr);
    assert(replacement != nullptr);
    assert(TryGetUse(*useEdge, &useEdge));

    GenTree* originalOperand = *useEdge;
    *useEdge                 = replacement;

    const bool isArgument =
        (replacement != gtControlExpr) &&
        ((gtCallType != CT_INDIRECT) || ((replacement != gtCallCookie) && (replacement != gtCallAddr)));

    if (isArgument)
    {
        if ((originalOperand->gtFlags & GTF_LATE_ARG) != 0)
        {
            replacement->gtFlags |= GTF_LATE_ARG;
        }
        else
        {
            assert((replacement->gtFlags & GTF_LATE_ARG) == 0);

            fgArgTabEntryPtr fp = Compiler::gtArgEntryByNode(this, originalOperand);
            assert(fp->node == originalOperand);
            fp->node = replacement;
        }
    }
}

//-------------------------------------------------------------------------
// AreArgsComplete: Determine if this GT_CALL node's arguments have been processed.
//
// Return Value:
//     Returns true if fgMorphArgs has processed the arguments.
//
bool GenTreeCall::AreArgsComplete() const
{
    if (fgArgInfo == nullptr)
    {
        return false;
    }
    if (fgArgInfo->AreArgsComplete())
    {
        assert((gtCallLateArgs != nullptr) || !fgArgInfo->HasRegArgs());
        return true;
    }
    assert(gtCallArgs == nullptr);
    return false;
}

#if !defined(FEATURE_PUT_STRUCT_ARG_STK) && !defined(LEGACY_BACKEND)
unsigned GenTreePutArgStk::getArgSize()
{
    return genTypeSize(genActualType(gtOp1->gtType));
}
#endif // !defined(FEATURE_PUT_STRUCT_ARG_STK) && !defined(LEGACY_BACKEND)

/*****************************************************************************
 *
 *  Returns non-zero if the two trees are identical.
 */

bool GenTree::Compare(GenTreePtr op1, GenTreePtr op2, bool swapOK)
{
    genTreeOps oper;
    unsigned   kind;

//  printf("tree1:\n"); gtDispTree(op1);
//  printf("tree2:\n"); gtDispTree(op2);

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

    assert(op1->gtOper != GT_STMT);
    assert(op2->gtOper != GT_STMT);

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

    if (kind & GTK_CONST)
    {
        switch (oper)
        {
            case GT_CNS_INT:
                if (op1->gtIntCon.gtIconVal == op2->gtIntCon.gtIconVal)
                {
                    return true;
                }
                break;
#if 0
            // TODO-CQ: Enable this in the future
        case GT_CNS_LNG:
            if  (op1->gtLngCon.gtLconVal == op2->gtLngCon.gtLconVal)
                return true;
            break;

        case GT_CNS_DBL:
            if  (op1->gtDblCon.gtDconVal == op2->gtDblCon.gtDconVal)
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
            case GT_LCL_VAR:
                if (op1->gtLclVarCommon.gtLclNum != op2->gtLclVarCommon.gtLclNum)
                {
                    break;
                }

                return true;

            case GT_LCL_FLD:
                if (op1->gtLclFld.gtLclNum != op2->gtLclFld.gtLclNum ||
                    op1->gtLclFld.gtLclOffs != op2->gtLclFld.gtLclOffs)
                {
                    break;
                }

                return true;

            case GT_CLS_VAR:
                if (op1->gtClsVar.gtClsVarHnd != op2->gtClsVar.gtClsVarHnd)
                {
                    break;
                }

                return true;

            case GT_LABEL:
                return true;

            case GT_ARGPLACE:
                if ((op1->gtType == TYP_STRUCT) &&
                    (op1->gtArgPlace.gtArgPlaceClsHnd != op2->gtArgPlace.gtArgPlaceClsHnd))
                {
                    break;
                }
                return true;

            default:
                break;
        }

        return false;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_UNOP)
    {
        if (IsExOp(kind))
        {
            // ExOp operators extend unary operator with extra, non-GenTreePtr members.  In many cases,
            // these should be included in the comparison.
            switch (oper)
            {
                case GT_ARR_LENGTH:
                    if (op1->gtArrLen.ArrLenOffset() != op2->gtArrLen.ArrLenOffset())
                    {
                        return false;
                    }
                    break;
                case GT_CAST:
                    if (op1->gtCast.gtCastType != op2->gtCast.gtCastType)
                    {
                        return false;
                    }
                    break;
                case GT_OBJ:
                    if (op1->AsObj()->gtClass != op2->AsObj()->gtClass)
                    {
                        return false;
                    }
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_BOX:
                    break;

                default:
                    assert(!"unexpected unary ExOp operator");
            }
        }
        return Compare(op1->gtOp.gtOp1, op2->gtOp.gtOp1);
    }

    if (kind & GTK_BINOP)
    {
        if (IsExOp(kind))
        {
            // ExOp operators extend unary operator with extra, non-GenTreePtr members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_INTRINSIC:
                    if (op1->gtIntrinsic.gtIntrinsicId != op2->gtIntrinsic.gtIntrinsicId)
                    {
                        return false;
                    }
                    break;
                case GT_LEA:
                    if (op1->gtAddrMode.gtScale != op2->gtAddrMode.gtScale)
                    {
                        return false;
                    }
                    if (op1->gtAddrMode.Offset() != op2->gtAddrMode.Offset())
                    {
                        return false;
                    }
                    break;
                case GT_INDEX:
                    if (op1->gtIndex.gtIndElemSize != op2->gtIndex.gtIndElemSize)
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
#ifdef FEATURE_SIMD
                case GT_SIMD:
                    if ((op1->AsSIMD()->gtSIMDIntrinsicID != op2->AsSIMD()->gtSIMDIntrinsicID) ||
                        (op1->AsSIMD()->gtSIMDBaseType != op2->AsSIMD()->gtSIMDBaseType) ||
                        (op1->AsSIMD()->gtSIMDSize != op2->AsSIMD()->gtSIMDSize))
                    {
                        return false;
                    }
                    break;
#endif // FEATURE_SIMD

                // For the ones below no extra argument matters for comparison.
                case GT_QMARK:
                    break;

                default:
                    assert(!"unexpected binary ExOp operator");
            }
        }

        if (op1->gtOp.gtOp2)
        {
            if (!Compare(op1->gtOp.gtOp1, op2->gtOp.gtOp1, swapOK))
            {
                if (swapOK && OperIsCommutative(oper) &&
                    ((op1->gtOp.gtOp1->gtFlags | op1->gtOp.gtOp2->gtFlags | op2->gtOp.gtOp1->gtFlags |
                      op2->gtOp.gtOp2->gtFlags) &
                     GTF_ALL_EFFECT) == 0)
                {
                    if (Compare(op1->gtOp.gtOp1, op2->gtOp.gtOp2, swapOK))
                    {
                        op1 = op1->gtOp.gtOp2;
                        op2 = op2->gtOp.gtOp1;
                        goto AGAIN;
                    }
                }

                return false;
            }

            op1 = op1->gtOp.gtOp2;
            op2 = op2->gtOp.gtOp2;

            goto AGAIN;
        }
        else
        {

            op1 = op1->gtOp.gtOp1;
            op2 = op2->gtOp.gtOp1;

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
        case GT_FIELD:
            if (op1->gtField.gtFldHnd != op2->gtField.gtFldHnd)
            {
                break;
            }

            op1 = op1->gtField.gtFldObj;
            op2 = op2->gtField.gtFldObj;

            if (op1 || op2)
            {
                if (op1 && op2)
                {
                    goto AGAIN;
                }
            }

            return true;

        case GT_CALL:

            if (op1->gtCall.gtCallType != op2->gtCall.gtCallType)
            {
                return false;
            }

            if (op1->gtCall.gtCallType != CT_INDIRECT)
            {
                if (op1->gtCall.gtCallMethHnd != op2->gtCall.gtCallMethHnd)
                {
                    return false;
                }

#ifdef FEATURE_READYTORUN_COMPILER
                if (op1->gtCall.gtEntryPoint.addr != op2->gtCall.gtEntryPoint.addr)
                {
                    return false;
                }
#endif
            }
            else
            {
                if (!Compare(op1->gtCall.gtCallAddr, op2->gtCall.gtCallAddr))
                {
                    return false;
                }
            }

            if (Compare(op1->gtCall.gtCallLateArgs, op2->gtCall.gtCallLateArgs) &&
                Compare(op1->gtCall.gtCallArgs, op2->gtCall.gtCallArgs) &&
                Compare(op1->gtCall.gtControlExpr, op2->gtCall.gtControlExpr) &&
                Compare(op1->gtCall.gtCallObjp, op2->gtCall.gtCallObjp))
            {
                return true;
            }
            break;

        case GT_ARR_ELEM:

            if (op1->gtArrElem.gtArrRank != op2->gtArrElem.gtArrRank)
            {
                return false;
            }

            // NOTE: gtArrElemSize may need to be handled

            unsigned dim;
            for (dim = 0; dim < op1->gtArrElem.gtArrRank; dim++)
            {
                if (!Compare(op1->gtArrElem.gtArrInds[dim], op2->gtArrElem.gtArrInds[dim]))
                {
                    return false;
                }
            }

            op1 = op1->gtArrElem.gtArrObj;
            op2 = op2->gtArrElem.gtArrObj;
            goto AGAIN;

        case GT_ARR_OFFSET:
            if (op1->gtArrOffs.gtCurrDim != op2->gtArrOffs.gtCurrDim ||
                op1->gtArrOffs.gtArrRank != op2->gtArrOffs.gtArrRank)
            {
                return false;
            }
            return (Compare(op1->gtArrOffs.gtOffset, op2->gtArrOffs.gtOffset) &&
                    Compare(op1->gtArrOffs.gtIndex, op2->gtArrOffs.gtIndex) &&
                    Compare(op1->gtArrOffs.gtArrObj, op2->gtArrOffs.gtArrObj));

        case GT_CMPXCHG:
            return Compare(op1->gtCmpXchg.gtOpLocation, op2->gtCmpXchg.gtOpLocation) &&
                   Compare(op1->gtCmpXchg.gtOpValue, op2->gtCmpXchg.gtOpValue) &&
                   Compare(op1->gtCmpXchg.gtOpComparand, op2->gtCmpXchg.gtOpComparand);

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            return Compare(op1->gtBoundsChk.gtIndex, op2->gtBoundsChk.gtIndex) &&
                   Compare(op1->gtBoundsChk.gtArrLen, op2->gtBoundsChk.gtArrLen) &&
                   (op1->gtBoundsChk.gtThrowKind == op2->gtBoundsChk.gtThrowKind);

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            return Compare(op1->gtDynBlk.Addr(), op2->gtDynBlk.Addr()) &&
                   Compare(op1->gtDynBlk.Data(), op2->gtDynBlk.Data()) &&
                   Compare(op1->gtDynBlk.gtDynamicSize, op2->gtDynBlk.gtDynamicSize);

        default:
            assert(!"unexpected operator");
    }

    return false;
}

/*****************************************************************************
 *
 *  Returns non-zero if the given tree contains a use of a local #lclNum.
 */

bool Compiler::gtHasRef(GenTreePtr tree, ssize_t lclNum, bool defOnly)
{
    genTreeOps oper;
    unsigned   kind;

AGAIN:

    assert(tree);

    oper = tree->OperGet();
    kind = tree->OperKind();

    assert(oper != GT_STMT);

    /* Is this a constant node? */

    if (kind & GTK_CONST)
    {
        return false;
    }

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        if (oper == GT_LCL_VAR)
        {
            if (tree->gtLclVarCommon.gtLclNum == (unsigned)lclNum)
            {
                if (!defOnly)
                {
                    return true;
                }
            }
        }
        else if (oper == GT_RET_EXPR)
        {
            return gtHasRef(tree->gtRetExpr.gtInlineCandidate, lclNum, defOnly);
        }

        return false;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        if (tree->gtGetOp2IfPresent())
        {
            if (gtHasRef(tree->gtOp.gtOp1, lclNum, defOnly))
            {
                return true;
            }

            tree = tree->gtOp.gtOp2;
            goto AGAIN;
        }
        else
        {
            tree = tree->gtOp.gtOp1;

            if (!tree)
            {
                return false;
            }

            if (GenTree::OperIsAssignment(oper))
            {
                // 'tree' is the gtOp1 of an assignment node. So we can handle
                // the case where defOnly is either true or false.

                if (tree->gtOper == GT_LCL_VAR && tree->gtLclVarCommon.gtLclNum == (unsigned)lclNum)
                {
                    return true;
                }
                else if (tree->gtOper == GT_FIELD && lclNum == (ssize_t)tree->gtField.gtFldHnd)
                {
                    return true;
                }
            }

            goto AGAIN;
        }
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_FIELD:
            if (lclNum == (ssize_t)tree->gtField.gtFldHnd)
            {
                if (!defOnly)
                {
                    return true;
                }
            }

            tree = tree->gtField.gtFldObj;
            if (tree)
            {
                goto AGAIN;
            }
            break;

        case GT_CALL:

            if (tree->gtCall.gtCallObjp)
            {
                if (gtHasRef(tree->gtCall.gtCallObjp, lclNum, defOnly))
                {
                    return true;
                }
            }

            if (tree->gtCall.gtCallArgs)
            {
                if (gtHasRef(tree->gtCall.gtCallArgs, lclNum, defOnly))
                {
                    return true;
                }
            }

            if (tree->gtCall.gtCallLateArgs)
            {
                if (gtHasRef(tree->gtCall.gtCallLateArgs, lclNum, defOnly))
                {
                    return true;
                }
            }

            if (tree->gtCall.gtControlExpr)
            {
                if (gtHasRef(tree->gtCall.gtControlExpr, lclNum, defOnly))
                {
                    return true;
                }
            }

            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
                // pinvoke-calli cookie is a constant, or constant indirection
                assert(tree->gtCall.gtCallCookie == nullptr || tree->gtCall.gtCallCookie->gtOper == GT_CNS_INT ||
                       tree->gtCall.gtCallCookie->gtOper == GT_IND);

                tree = tree->gtCall.gtCallAddr;
            }
            else
            {
                tree = nullptr;
            }

            if (tree)
            {
                goto AGAIN;
            }

            break;

        case GT_ARR_ELEM:
            if (gtHasRef(tree->gtArrElem.gtArrObj, lclNum, defOnly))
            {
                return true;
            }

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                if (gtHasRef(tree->gtArrElem.gtArrInds[dim], lclNum, defOnly))
                {
                    return true;
                }
            }

            break;

        case GT_ARR_OFFSET:
            if (gtHasRef(tree->gtArrOffs.gtOffset, lclNum, defOnly) ||
                gtHasRef(tree->gtArrOffs.gtIndex, lclNum, defOnly) ||
                gtHasRef(tree->gtArrOffs.gtArrObj, lclNum, defOnly))
            {
                return true;
            }
            break;

        case GT_CMPXCHG:
            if (gtHasRef(tree->gtCmpXchg.gtOpLocation, lclNum, defOnly))
            {
                return true;
            }
            if (gtHasRef(tree->gtCmpXchg.gtOpValue, lclNum, defOnly))
            {
                return true;
            }
            if (gtHasRef(tree->gtCmpXchg.gtOpComparand, lclNum, defOnly))
            {
                return true;
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            if (gtHasRef(tree->gtBoundsChk.gtIndex, lclNum, defOnly))
            {
                return true;
            }
            if (gtHasRef(tree->gtBoundsChk.gtArrLen, lclNum, defOnly))
            {
                return true;
            }
            break;

        case GT_STORE_DYN_BLK:
            if (gtHasRef(tree->gtDynBlk.Data(), lclNum, defOnly))
            {
                return true;
            }
            __fallthrough;
        case GT_DYN_BLK:
            if (gtHasRef(tree->gtDynBlk.Addr(), lclNum, defOnly))
            {
                return true;
            }
            if (gtHasRef(tree->gtDynBlk.gtDynamicSize, lclNum, defOnly))
            {
                return true;
            }
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
#endif
            assert(!"unexpected operator");
    }

    return false;
}

struct AddrTakenDsc
{
    Compiler* comp;
    bool      hasAddrTakenLcl;
};

/* static */
Compiler::fgWalkResult Compiler::gtHasLocalsWithAddrOpCB(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree = *pTree;
    Compiler*  comp = data->compiler;

    if (tree->gtOper == GT_LCL_VAR)
    {
        unsigned   lclNum = tree->gtLclVarCommon.gtLclNum;
        LclVarDsc* varDsc = &comp->lvaTable[lclNum];

        if (varDsc->lvHasLdAddrOp || varDsc->lvAddrExposed)
        {
            ((AddrTakenDsc*)data->pCallbackData)->hasAddrTakenLcl = true;
            return WALK_ABORT;
        }
    }

    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Return true if this tree contains locals with lvHasLdAddrOp or lvAddrExposed
 *  flag(s) set.
 */

bool Compiler::gtHasLocalsWithAddrOp(GenTreePtr tree)
{
    AddrTakenDsc desc;

    desc.comp            = this;
    desc.hasAddrTakenLcl = false;

    fgWalkTreePre(&tree, gtHasLocalsWithAddrOpCB, &desc);

    return desc.hasAddrTakenLcl;
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

    GenTreePtr temp;

AGAIN:
    assert(tree);
    assert(tree->gtOper != GT_STMT);

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Include the operator value in the hash */

    hash = genTreeHashAdd(hash, oper);

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        size_t add;

        switch (oper)
        {
            UINT64 bits;
            case GT_LCL_VAR:
                add = tree->gtLclVar.gtLclNum;
                break;
            case GT_LCL_FLD:
                hash = genTreeHashAdd(hash, tree->gtLclFld.gtLclNum);
                add  = tree->gtLclFld.gtLclOffs;
                break;

            case GT_CNS_INT:
                add = tree->gtIntCon.gtIconVal;
                break;
            case GT_CNS_LNG:
                bits = (UINT64)tree->gtLngCon.gtLconVal;
#ifdef _TARGET_64BIT_
                add = bits;
#else // 32-bit target
                add = genTreeHashAdd(uhi32(bits), ulo32(bits));
#endif
                break;
            case GT_CNS_DBL:
                bits = *(UINT64*)(&tree->gtDblCon.gtDconVal);
#ifdef _TARGET_64BIT_
                add = bits;
#else // 32-bit target
                add = genTreeHashAdd(uhi32(bits), ulo32(bits));
#endif
                break;
            case GT_CNS_STR:
                add = tree->gtStrCon.gtSconCPX;
                break;

            case GT_JMP:
                add = tree->gtVal.gtVal1;
                break;

            default:
                add = 0;
                break;
        }

        // clang-format off
        // narrow 'add' into a 32-bit 'val'
        unsigned val;
#ifdef _TARGET_64BIT_
        val = genTreeHashAdd(uhi32(add), ulo32(add));
#else // 32-bit target
        val = add;
#endif
        // clang-format on

        hash = genTreeHashAdd(hash, val);
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    GenTreePtr op1;

    if (kind & GTK_UNOP)
    {
        op1 = tree->gtOp.gtOp1;
        /* Special case: no sub-operand at all */

        if (GenTree::IsExOp(kind))
        {
            // ExOp operators extend operators with extra, non-GenTreePtr members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_ARR_LENGTH:
                    hash += tree->gtArrLen.ArrLenOffset();
                    break;
                case GT_CAST:
                    hash ^= tree->gtCast.gtCastType;
                    break;
                case GT_INDEX:
                    hash += tree->gtIndex.gtIndElemSize;
                    break;
                case GT_INDEX_ADDR:
                    hash += tree->AsIndexAddr()->gtElemSize;
                    break;
                case GT_ALLOCOBJ:
                    hash = genTreeHashAdd(hash, static_cast<unsigned>(
                                                    reinterpret_cast<uintptr_t>(tree->gtAllocObj.gtAllocObjClsHnd)));
                    hash = genTreeHashAdd(hash, tree->gtAllocObj.gtNewHelper);
                    break;
                case GT_RUNTIMELOOKUP:
                    hash =
                        genTreeHashAdd(hash,
                                       static_cast<unsigned>(reinterpret_cast<uintptr_t>(tree->gtRuntimeLookup.gtHnd)));
                    break;

                case GT_OBJ:
                    hash =
                        genTreeHashAdd(hash, static_cast<unsigned>(reinterpret_cast<uintptr_t>(tree->gtObj.gtClass)));
                    break;
                // For the ones below no extra argument matters for comparison.
                case GT_BOX:
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
            // ExOp operators extend operators with extra, non-GenTreePtr members.  In many cases,
            // these should be included in the hash code.
            switch (oper)
            {
                case GT_INTRINSIC:
                    hash += tree->gtIntrinsic.gtIntrinsicId;
                    break;
                case GT_LEA:
                    hash += static_cast<unsigned>(tree->gtAddrMode.Offset() << 3) + tree->gtAddrMode.gtScale;
                    break;

                case GT_BLK:
                case GT_STORE_BLK:
                    hash += tree->gtBlk.gtBlkSize;
                    break;

                case GT_OBJ:
                case GT_STORE_OBJ:
                    hash ^= reinterpret_cast<unsigned>(tree->AsObj()->gtClass);
                    break;

                case GT_DYN_BLK:
                case GT_STORE_DYN_BLK:
                    hash += gtHashValue(tree->AsDynBlk()->gtDynamicSize);
                    break;

                // For the ones below no extra argument matters for comparison.
                case GT_ARR_INDEX:
                case GT_QMARK:
                case GT_INDEX:
                case GT_INDEX_ADDR:
                    break;

#ifdef FEATURE_SIMD
                case GT_SIMD:
                    hash += tree->gtSIMD.gtSIMDIntrinsicID;
                    hash += tree->gtSIMD.gtSIMDBaseType;
                    break;
#endif // FEATURE_SIMD

                default:
                    assert(!"unexpected binary ExOp operator");
            }
        }

        op1            = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtOp.gtOp2;

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
        case GT_FIELD:
            if (tree->gtField.gtFldObj)
            {
                temp = tree->gtField.gtFldObj;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }
            break;

        case GT_STMT:
            temp = tree->gtStmt.gtStmtExpr;
            assert(temp);
            hash = genTreeHashAdd(hash, gtHashValue(temp));
            break;

        case GT_ARR_ELEM:

            hash = genTreeHashAdd(hash, gtHashValue(tree->gtArrElem.gtArrObj));

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                hash = genTreeHashAdd(hash, gtHashValue(tree->gtArrElem.gtArrInds[dim]));
            }

            break;

        case GT_ARR_OFFSET:
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtArrOffs.gtOffset));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtArrOffs.gtIndex));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtArrOffs.gtArrObj));
            break;

        case GT_CALL:

            if (tree->gtCall.gtCallObjp && tree->gtCall.gtCallObjp->gtOper != GT_NOP)
            {
                temp = tree->gtCall.gtCallObjp;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }

            if (tree->gtCall.gtCallArgs)
            {
                temp = tree->gtCall.gtCallArgs;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }

            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
                temp = tree->gtCall.gtCallAddr;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }
            else
            {
                hash = genTreeHashAdd(hash, tree->gtCall.gtCallMethHnd);
            }

            if (tree->gtCall.gtCallLateArgs)
            {
                temp = tree->gtCall.gtCallLateArgs;
                assert(temp);
                hash = genTreeHashAdd(hash, gtHashValue(temp));
            }
            break;

        case GT_CMPXCHG:
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtCmpXchg.gtOpLocation));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtCmpXchg.gtOpValue));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtCmpXchg.gtOpComparand));
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtBoundsChk.gtIndex));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtBoundsChk.gtArrLen));
            hash = genTreeHashAdd(hash, tree->gtBoundsChk.gtThrowKind);
            break;

        case GT_STORE_DYN_BLK:
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtDynBlk.Data()));
            __fallthrough;
        case GT_DYN_BLK:
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtDynBlk.Addr()));
            hash = genTreeHashAdd(hash, gtHashValue(tree->gtDynBlk.gtDynamicSize));
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
 *  Given an arbitrary expression tree, attempts to find the set of all local variables
 *  referenced by the tree, and return them as "*result".
 *  If "findPtr" is null, this is a tracked variable set;
 *  if it is non-null, this is an "all var set."
 *  The "*result" value is valid only if the call returns "true."  It may return "false"
 *  for several reasons:
 *     If "findPtr" is NULL, and the expression contains an untracked variable.
 *     If "findPtr" is non-NULL, and the expression contains a variable that can't be represented
 *        in an "all var set."
 *     If the expression accesses address-exposed variables.
 *
 *  If there
 *  are any indirections or global refs in the expression, the "*refsPtr" argument
 *  will be assigned the appropriate bit set based on the 'varRefKinds' type.
 *  It won't be assigned anything when there are no indirections or global
 *  references, though, so this value should be initialized before the call.
 *  If we encounter an expression that is equal to *findPtr we set *findPtr
 *  to NULL.
 */
bool Compiler::lvaLclVarRefs(GenTreePtr tree, GenTreePtr* findPtr, varRefKinds* refsPtr, void* result)
{
    genTreeOps   oper;
    unsigned     kind;
    varRefKinds  refs = VR_NONE;
    ALLVARSET_TP allVars(AllVarSetOps::UninitVal());
    VARSET_TP    trkdVars(VarSetOps::UninitVal());
    if (findPtr)
    {
        AllVarSetOps::AssignNoCopy(this, allVars, AllVarSetOps::MakeEmpty(this));
    }
    else
    {
        VarSetOps::AssignNoCopy(this, trkdVars, VarSetOps::MakeEmpty(this));
    }

AGAIN:

    assert(tree);
    assert(tree->gtOper != GT_STMT);

    /* Remember whether we've come across the expression we're looking for */

    if (findPtr && *findPtr == tree)
    {
        *findPtr = nullptr;
    }

    /* Figure out what kind of a node we have */

    oper = tree->OperGet();
    kind = tree->OperKind();

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        if (oper == GT_LCL_VAR)
        {
            unsigned lclNum = tree->gtLclVarCommon.gtLclNum;

            /* Should we use the variable table? */

            if (findPtr)
            {
                if (lclNum >= lclMAX_ALLSET_TRACKED)
                {
                    return false;
                }

                AllVarSetOps::AddElemD(this, allVars, lclNum);
            }
            else
            {
                assert(lclNum < lvaCount);
                LclVarDsc* varDsc = lvaTable + lclNum;

                if (varDsc->lvTracked == false)
                {
                    return false;
                }

                // Don't deal with expressions with address-exposed variables.
                if (varDsc->lvAddrExposed)
                {
                    return false;
                }

                VarSetOps::AddElemD(this, trkdVars, varDsc->lvVarIndex);
            }
        }
        else if (oper == GT_LCL_FLD)
        {
            /* We can't track every field of every var. Moreover, indirections
               may access different parts of the var as different (but
               overlapping) fields. So just treat them as indirect accesses */

            if (varTypeIsGC(tree->TypeGet()))
            {
                refs = VR_IND_REF;
            }
            else
            {
                refs = VR_IND_SCL;
            }
        }
        else if (oper == GT_CLS_VAR)
        {
            refs = VR_GLB_VAR;
        }

        if (refs != VR_NONE)
        {
            /* Write it back to callers parameter using an 'or' */
            *refsPtr = varRefKinds((*refsPtr) | refs);
        }
        lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
        return true;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        if (oper == GT_IND)
        {
            assert(tree->gtOp.gtOp2 == nullptr);

            /* Set the proper indirection bit */

            if ((tree->gtFlags & GTF_IND_INVARIANT) == 0)
            {
                if (varTypeIsGC(tree->TypeGet()))
                {
                    refs = VR_IND_REF;
                }
                else
                {
                    refs = VR_IND_SCL;
                }

                // If the flag GTF_IND_TGTANYWHERE is set this indirection
                // could also point at a global variable

                if (tree->gtFlags & GTF_IND_TGTANYWHERE)
                {
                    refs = varRefKinds(((int)refs) | ((int)VR_GLB_VAR));
                }
            }

            /* Write it back to callers parameter using an 'or' */
            *refsPtr = varRefKinds((*refsPtr) | refs);

            // For IL volatile memory accesses we mark the GT_IND node
            // with a GTF_DONT_CSE flag.
            //
            // This flag is also set for the left hand side of an assignment.
            //
            // If this flag is set then we return false
            //
            if (tree->gtFlags & GTF_DONT_CSE)
            {
                return false;
            }
        }

        if (tree->gtGetOp2IfPresent())
        {
            /* It's a binary operator */
            if (!lvaLclVarRefsAccum(tree->gtOp.gtOp1, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            tree = tree->gtOp.gtOp2;
            assert(tree);
            goto AGAIN;
        }
        else
        {
            /* It's a unary (or nilary) operator */

            tree = tree->gtOp.gtOp1;
            if (tree)
            {
                goto AGAIN;
            }

            lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
            return true;
        }
    }

    switch (oper)
    {
        case GT_ARR_ELEM:
            if (!lvaLclVarRefsAccum(tree->gtArrElem.gtArrObj, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                VARSET_TP tmpVs(VarSetOps::UninitVal());
                if (!lvaLclVarRefsAccum(tree->gtArrElem.gtArrInds[dim], findPtr, refsPtr, &allVars, &trkdVars))
                {
                    return false;
                }
            }
            lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
            return true;

        case GT_ARR_OFFSET:
            if (!lvaLclVarRefsAccum(tree->gtArrOffs.gtOffset, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            if (!lvaLclVarRefsAccum(tree->gtArrOffs.gtIndex, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            if (!lvaLclVarRefsAccum(tree->gtArrOffs.gtArrObj, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
            return true;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            if (!lvaLclVarRefsAccum(tree->gtBoundsChk.gtIndex, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            if (!lvaLclVarRefsAccum(tree->gtBoundsChk.gtArrLen, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
            return true;
        }

        case GT_STORE_DYN_BLK:
            if (!lvaLclVarRefsAccum(tree->gtDynBlk.Data(), findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            __fallthrough;
        case GT_DYN_BLK:
            if (!lvaLclVarRefsAccum(tree->gtDynBlk.Addr(), findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            if (!lvaLclVarRefsAccum(tree->gtDynBlk.gtDynamicSize, findPtr, refsPtr, &allVars, &trkdVars))
            {
                return false;
            }
            // Otherwise...
            lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
            break;

        case GT_CALL:
            /* Allow calls to the Shared Static helper */
            if (IsSharedStaticHelper(tree))
            {
                *refsPtr = varRefKinds((*refsPtr) | VR_INVARIANT);
                lvaLclVarRefsAccumIntoRes(findPtr, result, allVars, trkdVars);
                return true;
            }
            break;
        default:
            break;

    } // end switch (oper)

    return false;
}

bool Compiler::lvaLclVarRefsAccum(
    GenTreePtr tree, GenTreePtr* findPtr, varRefKinds* refsPtr, ALLVARSET_TP* allVars, VARSET_TP* trkdVars)
{
    if (findPtr)
    {
        ALLVARSET_TP tmpVs(AllVarSetOps::UninitVal());
        if (!lvaLclVarRefs(tree, findPtr, refsPtr, &tmpVs))
        {
            return false;
        }
        // Otherwise...
        AllVarSetOps::UnionD(this, *allVars, tmpVs);
    }
    else
    {
        VARSET_TP tmpVs(VarSetOps::UninitVal());
        if (!lvaLclVarRefs(tree, findPtr, refsPtr, &tmpVs))
        {
            return false;
        }
        // Otherwise...
        VarSetOps::UnionD(this, *trkdVars, tmpVs);
    }
    return true;
}

void Compiler::lvaLclVarRefsAccumIntoRes(GenTreePtr*         findPtr,
                                         void*               result,
                                         ALLVARSET_VALARG_TP allVars,
                                         VARSET_VALARG_TP    trkdVars)
{
    if (findPtr)
    {
        ALLVARSET_TP* avsPtr = (ALLVARSET_TP*)result;
        AllVarSetOps::AssignNoCopy(this, (*avsPtr), allVars);
    }
    else
    {
        VARSET_TP* vsPtr = (VARSET_TP*)result;
        VarSetOps::AssignNoCopy(this, (*vsPtr), trkdVars);
    }
}

/*****************************************************************************
 *
 *  Return a relational operator that is the reverse of the given one.
 */

/* static */
genTreeOps GenTree::ReverseRelop(genTreeOps relop)
{
    static const genTreeOps reverseOps[] = {
        GT_NE, // GT_EQ
        GT_EQ, // GT_NE
        GT_GE, // GT_LT
        GT_GT, // GT_LE
        GT_LT, // GT_GE
        GT_LE, // GT_GT
#ifndef LEGACY_BACKEND
        GT_TEST_NE, // GT_TEST_EQ
        GT_TEST_EQ, // GT_TEST_NE
#endif
    };

    assert(reverseOps[GT_EQ - GT_EQ] == GT_NE);
    assert(reverseOps[GT_NE - GT_EQ] == GT_EQ);

    assert(reverseOps[GT_LT - GT_EQ] == GT_GE);
    assert(reverseOps[GT_LE - GT_EQ] == GT_GT);
    assert(reverseOps[GT_GE - GT_EQ] == GT_LT);
    assert(reverseOps[GT_GT - GT_EQ] == GT_LE);

#ifndef LEGACY_BACKEND
    assert(reverseOps[GT_TEST_EQ - GT_EQ] == GT_TEST_NE);
    assert(reverseOps[GT_TEST_NE - GT_EQ] == GT_TEST_EQ);
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
    static const genTreeOps swapOps[] = {
        GT_EQ, // GT_EQ
        GT_NE, // GT_NE
        GT_GT, // GT_LT
        GT_GE, // GT_LE
        GT_LE, // GT_GE
        GT_LT, // GT_GT
#ifndef LEGACY_BACKEND
        GT_TEST_EQ, // GT_TEST_EQ
        GT_TEST_NE, // GT_TEST_NE
#endif
    };

    assert(swapOps[GT_EQ - GT_EQ] == GT_EQ);
    assert(swapOps[GT_NE - GT_EQ] == GT_NE);

    assert(swapOps[GT_LT - GT_EQ] == GT_GT);
    assert(swapOps[GT_LE - GT_EQ] == GT_GE);
    assert(swapOps[GT_GE - GT_EQ] == GT_LE);
    assert(swapOps[GT_GT - GT_EQ] == GT_LT);

#ifndef LEGACY_BACKEND
    assert(swapOps[GT_TEST_EQ - GT_EQ] == GT_TEST_EQ);
    assert(swapOps[GT_TEST_NE - GT_EQ] == GT_TEST_NE);
#endif

    assert(OperIsCompare(relop));
    assert(relop >= GT_EQ && (unsigned)(relop - GT_EQ) < sizeof(swapOps));

    return swapOps[relop - GT_EQ];
}

/*****************************************************************************
 *
 *  Reverse the meaning of the given test condition.
 */

GenTreePtr Compiler::gtReverseCond(GenTree* tree)
{
    if (tree->OperIsCompare())
    {
        tree->SetOper(GenTree::ReverseRelop(tree->OperGet()));

        // Flip the GTF_RELOP_NAN_UN bit
        //     a ord b   === (a != NaN && b != NaN)
        //     a unord b === (a == NaN || b == NaN)
        // => !(a ord b) === (a unord b)
        if (varTypeIsFloating(tree->gtOp.gtOp1->TypeGet()))
        {
            tree->gtFlags ^= GTF_RELOP_NAN_UN;
        }
    }
    else if (tree->OperIs(GT_JCC, GT_SETCC))
    {
        GenTreeCC* cc   = tree->AsCC();
        cc->gtCondition = GenTree::ReverseRelop(cc->gtCondition);
    }
    else if (tree->OperIs(GT_JCMP))
    {
        // Flip the GTF_JCMP_EQ
        //
        // This causes switching
        //     cbz <=> cbnz
        //     tbz <=> tbnz
        tree->gtFlags ^= GTF_JCMP_EQ;
    }
    else
    {
        tree = gtNewOperNode(GT_NOT, TYP_INT, tree);
    }

    return tree;
}

/*****************************************************************************/

#ifdef DEBUG

bool GenTree::gtIsValid64RsltMul()
{
    if ((gtOper != GT_MUL) || !(gtFlags & GTF_MUL_64RSLT))
    {
        return false;
    }

    GenTreePtr op1 = gtOp.gtOp1;
    GenTreePtr op2 = gtOp.gtOp2;

    if (TypeGet() != TYP_LONG || op1->TypeGet() != TYP_LONG || op2->TypeGet() != TYP_LONG)
    {
        return false;
    }

    if (gtOverflow())
    {
        return false;
    }

    // op1 has to be conv.i8(i4Expr)
    if ((op1->gtOper != GT_CAST) || (genActualType(op1->CastFromType()) != TYP_INT))
    {
        return false;
    }

    // op2 has to be conv.i8(i4Expr)
    if ((op2->gtOper != GT_CAST) || (genActualType(op2->CastFromType()) != TYP_INT))
    {
        return false;
    }

    // The signedness of both casts must be the same
    if (((op1->gtFlags & GTF_UNSIGNED) != 0) != ((op2->gtFlags & GTF_UNSIGNED) != 0))
    {
        return false;
    }

    // Do unsigned mul iff both the casts are unsigned
    if (((op1->gtFlags & GTF_UNSIGNED) != 0) != ((gtFlags & GTF_UNSIGNED) != 0))
    {
        return false;
    }

    return true;
}

#endif // DEBUG

//------------------------------------------------------------------------------
// gtSetListOrder : Figure out the evaluation order for a list of values.
//
//
// Arguments:
//    list  - List to figure out the evaluation order for
//    isListCallArgs - True iff the list is a list of call arguments
//    callArgsInRegs -  True iff the list is a list of call arguments and they are passed in registers
//
// Return Value:
//    True if the operation can be a root of a bitwise rotation tree; false otherwise.

unsigned Compiler::gtSetListOrder(GenTree* list, bool isListCallArgs, bool callArgsInRegs)
{
    assert((list != nullptr) && list->OperIsAnyList());
    assert(!callArgsInRegs || isListCallArgs);

    ArrayStack<GenTree*> listNodes(this);

    do
    {
        listNodes.Push(list);
        list = list->gtOp.gtOp2;
    } while ((list != nullptr) && (list->OperIsAnyList()));

    unsigned nxtlvl = (list == nullptr) ? 0 : gtSetEvalOrder(list);
    while (listNodes.Height() > 0)
    {
#if FEATURE_STACK_FP_X87
        /* Save the current FP stack level since an argument list
        * will implicitly pop the FP stack when pushing the argument */
        unsigned FPlvlSave = codeGen->genGetFPstkLevel();
#endif // FEATURE_STACK_FP_X87

        list = listNodes.Pop();
        assert(list && list->OperIsAnyList());
        GenTreePtr next = list->gtOp.gtOp2;

        unsigned level = 0;
        unsigned ftreg = 0;

        // TODO: Do we have to compute costs differently for argument lists and
        // all other lists?
        // https://github.com/dotnet/coreclr/issues/7095
        unsigned costSz = (isListCallArgs || (next == nullptr)) ? 0 : 1;
        unsigned costEx = (isListCallArgs || (next == nullptr)) ? 0 : 1;

        if (next != nullptr)
        {
            ftreg |= next->gtRsvdRegs;
            if (isListCallArgs)
            {
                if (level < nxtlvl)
                {
                    level = nxtlvl;
                }
            }
            costEx += next->gtCostEx;
            costSz += next->gtCostSz;
        }

        GenTreePtr op1 = list->gtOp.gtOp1;
        unsigned   lvl = gtSetEvalOrder(op1);

#if FEATURE_STACK_FP_X87
        // restore the FP level
        codeGen->genResetFPstkLevel(FPlvlSave);
#endif // FEATURE_STACK_FP_X87

        list->gtRsvdRegs = (regMaskSmall)(ftreg | op1->gtRsvdRegs);

        // Swap the level counts
        if (list->gtFlags & GTF_REVERSE_OPS)
        {
            unsigned tmpl;

            tmpl   = lvl;
            lvl    = nxtlvl;
            nxtlvl = tmpl;
        }

        // TODO: Do we have to compute levels differently for argument lists and
        // all other lists?
        // https://github.com/dotnet/coreclr/issues/7095
        if (isListCallArgs)
        {
            if (level < lvl)
            {
                level = lvl;
            }
        }
        else
        {
            if (lvl < 1)
            {
                level = nxtlvl;
            }
            else if (lvl == nxtlvl)
            {
                level = lvl + 1;
            }
            else
            {
                level = lvl;
            }
        }

        if (op1->gtCostEx != 0)
        {
            costEx += op1->gtCostEx;
            costEx += (callArgsInRegs || !isListCallArgs) ? 0 : IND_COST_EX;
        }

        if (op1->gtCostSz != 0)
        {
            costSz += op1->gtCostSz;
#ifdef _TARGET_XARCH_
            if (callArgsInRegs) // push is smaller than mov to reg
#endif
            {
                costSz += 1;
            }
        }

        list->SetCosts(costEx, costSz);

        nxtlvl = level;
    }

    return nxtlvl;
}

/*****************************************************************************
 *
 *  This routine is a helper routine for gtSetEvalOrder() and is used to
 *  mark the interior address computation nodes with the GTF_ADDRMODE_NO_CSE flag
 *  which prevents them from being considered for CSE's.
 *
 *  Furthermore this routine is a factoring of the logic used to walk down
 *  the child nodes of a GT_IND tree, similar to optParseArrayRef().
 *
 *  Previously we had this logic repeated three times inside of gtSetEvalOrder().
 *  Here we combine those three repeats into this routine and use the
 *  bool constOnly to modify the behavior of this routine for the first call.
 *
 *  The object here is to mark all of the interior GT_ADD's and GT_NOP's
 *  with the GTF_ADDRMODE_NO_CSE flag and to set op1 and op2 to the terminal nodes
 *  which are later matched against 'adr' and 'idx'.
 *
 *  *pbHasRangeCheckBelow is set to false if we traverse a range check GT_NOP
 *  node in our walk. It remains unchanged otherwise.
 *
 *  TODO-Cleanup: It is essentially impossible to determine
 *  what it is supposed to do, or to write a reasonable specification comment
 *  for it that describes what it is supposed to do. There are obviously some
 *  very specific tree patterns that it expects to see, but those are not documented.
 *  The fact that it writes back to its op1WB and op2WB arguments, and traverses
 *  down both op1 and op2 trees, but op2 is only related to op1 in the (!constOnly)
 *  case (which really seems like a bug) is very confusing.
 */

void Compiler::gtWalkOp(GenTree** op1WB, GenTree** op2WB, GenTree* adr, bool constOnly)
{
    GenTreePtr op1 = *op1WB;
    GenTreePtr op2 = *op2WB;

    op1 = op1->gtEffectiveVal();

    // Now we look for op1's with non-overflow GT_ADDs [of constants]
    while ((op1->gtOper == GT_ADD) && (!op1->gtOverflow()) && (!constOnly || (op1->gtOp.gtOp2->IsCnsIntOrI())))
    {
        // mark it with GTF_ADDRMODE_NO_CSE
        op1->gtFlags |= GTF_ADDRMODE_NO_CSE;

        if (!constOnly)
        { // TODO-Cleanup: It seems bizarre that this is !constOnly
            op2 = op1->gtOp.gtOp2;
        }
        op1 = op1->gtOp.gtOp1;

        // If op1 is a GT_NOP then swap op1 and op2.
        // (Why? Also, presumably op2 is not a GT_NOP in this case?)
        if (op1->gtOper == GT_NOP)
        {
            GenTreePtr tmp;

            tmp = op1;
            op1 = op2;
            op2 = tmp;
        }

        if (!constOnly && ((op2 == adr) || (!op2->IsCnsIntOrI())))
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
GenTreePtr Compiler::gtWalkOpEffectiveVal(GenTreePtr op)
{
    for (;;)
    {
        op = op->gtEffectiveVal();

        if ((op->gtOper != GT_ADD) || op->gtOverflow() || !op->gtOp.gtOp2->IsCnsIntOrI())
        {
            break;
        }

        op = op->gtOp.gtOp1;
    }

    return op;
}
#endif // DEBUG

/*****************************************************************************
 *
 *  Given a tree, set the gtCostEx and gtCostSz fields which
 *  are used to measure the relative costs of the codegen of the tree
 *
 */

void Compiler::gtPrepareCost(GenTree* tree)
{
#if FEATURE_STACK_FP_X87
    codeGen->genResetFPstkLevel();
#endif // FEATURE_STACK_FP_X87
    gtSetEvalOrder(tree);
}

bool Compiler::gtIsLikelyRegVar(GenTree* tree)
{
    if (tree->gtOper != GT_LCL_VAR)
    {
        return false;
    }

    assert(tree->gtLclVar.gtLclNum < lvaTableCnt);
    LclVarDsc* varDsc = lvaTable + tree->gtLclVar.gtLclNum;

    if (varDsc->lvDoNotEnregister)
    {
        return false;
    }

    if (varDsc->lvRefCntWtd < (BB_UNITY_WEIGHT * 3))
    {
        return false;
    }

#ifdef _TARGET_X86_
    if (varTypeIsFloating(tree->TypeGet()))
        return false;
    if (varTypeIsLong(tree->TypeGet()))
        return false;
#endif

    return true;
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
    // Relative of order of global / side effects can't be swapped.

    bool canSwap = true;

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
                if (!secondNode->OperIsConst())
                {
                    canSwap = false;
                }
            }
        }
    }
    return canSwap;
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
 *      1. gtCostEx to the execution complexity estimate
 *      2. gtCostSz to the code size estimate
 *      3. gtRsvdRegs to the set of fixed registers trashed by the tree
 *      4. gtFPlvl to the "floating point depth" value for node, i.e. the max. number
 *         of operands the tree will push on the x87 (coprocessor) stack. Also sets
 *         genFPstkLevel, tmpDoubleSpillMax, and possibly gtFPstLvlRedo.
 *      5. Sometimes sets GTF_ADDRMODE_NO_CSE on nodes in the tree.
 *      6. DEBUG-only: clears GTF_DEBUG_NODE_MORPHED.
 */

#ifdef _PREFAST_
#pragma warning(push)
#pragma warning(disable : 21000) // Suppress PREFast warning about overly large function
#endif
unsigned Compiler::gtSetEvalOrder(GenTree* tree)
{
    assert(tree);
    assert(tree->gtOper != GT_STMT);

#ifdef DEBUG
    /* Clear the GTF_DEBUG_NODE_MORPHED flag as well */
    tree->gtDebugFlags &= ~GTF_DEBUG_NODE_MORPHED;
#endif

    /* Is this a FP value? */

    bool     isflt = varTypeIsFloating(tree->TypeGet());
    unsigned FPlvlSave;

    /* Figure out what kind of a node we have */

    const genTreeOps oper = tree->OperGet();
    const unsigned   kind = tree->OperKind();

    /* Assume no fixed registers will be trashed */

    regMaskTP ftreg = RBM_NONE; // Set of registers that will be used by the subtree
    unsigned  level;
    int       costEx;
    int       costSz;

    bool bRngChk;

#ifdef DEBUG
    costEx = -1;
    costSz = -1;
#endif

    /* Is this a constant or a leaf node? */

    if (kind & (GTK_LEAF | GTK_CONST))
    {
        switch (oper)
        {
            GenTreeIntConCommon* con;

#ifdef _TARGET_ARM_
            case GT_CNS_LNG:
                costSz = 9;
                costEx = 4;
                goto COMMON_CNS;

            case GT_CNS_STR:
                // Uses movw/movt
                costSz = 7;
                costEx = 3;
                goto COMMON_CNS;

            case GT_CNS_INT:

                // If the constant is a handle then it will need to have a relocation
                //  applied to it.
                // Any constant that requires a reloc must use the movw/movt sequence
                //
                con = tree->AsIntConCommon();

                if (con->ImmedValNeedsReloc(this) || !codeGen->validImmForInstr(INS_mov, tree->gtIntCon.gtIconVal))
                {
                    // Uses movw/movt
                    costSz = 7;
                    costEx = 3;
                }
                else if (((unsigned)tree->gtIntCon.gtIconVal) <= 0x00ff)
                {
                    // mov  Rd, <const8>
                    costSz = 1;
                    costEx = 1;
                }
                else
                {
                    // Uses movw/mvn
                    costSz = 3;
                    costEx = 1;
                }
                goto COMMON_CNS;

#elif defined _TARGET_XARCH_

            case GT_CNS_LNG:
                costSz = 10;
                costEx = 3;
                goto COMMON_CNS;

            case GT_CNS_STR:
                costSz = 4;
                costEx = 1;
                goto COMMON_CNS;

            case GT_CNS_INT:
            {
                // If the constant is a handle then it will need to have a relocation
                //  applied to it.
                //
                con = tree->AsIntConCommon();

                bool iconNeedsReloc = con->ImmedValNeedsReloc(this);

                if (!iconNeedsReloc && con->FitsInI8())
                {
                    costSz = 1;
                    costEx = 1;
                }
#if defined(_TARGET_AMD64_)
                else if (iconNeedsReloc || !con->FitsInI32())
                {
                    costSz = 10;
                    costEx = 3;
                }
#endif // _TARGET_AMD64_
                else
                {
                    costSz = 4;
                    costEx = 1;
                }
                goto COMMON_CNS;
            }

#elif defined(_TARGET_ARM64_)
            case GT_CNS_LNG:
            case GT_CNS_STR:
            case GT_CNS_INT:
                // TODO-ARM64-NYI: Need cost estimates.
                costSz = 1;
                costEx = 1;
                goto COMMON_CNS;

#else
            case GT_CNS_LNG:
            case GT_CNS_STR:
            case GT_CNS_INT:
#error "Unknown _TARGET_"
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
                level = 0;
                /* We use fldz and fld1 to load 0.0 and 1.0, but all other  */
                /* floating point constants are loaded using an indirection */
                if ((*((__int64*)&(tree->gtDblCon.gtDconVal)) == 0) ||
                    (*((__int64*)&(tree->gtDblCon.gtDconVal)) == I64(0x3ff0000000000000)))
                {
                    costEx = 1;
                    costSz = 1;
                }
                else
                {
                    costEx = IND_COST_EX;
                    costSz = 4;
                }
                break;

            case GT_LCL_VAR:
                level = 1;
                if (gtIsLikelyRegVar(tree))
                {
                    costEx = 1;
                    costSz = 1;
                    /* Sign-extend and zero-extend are more expensive to load */
                    if (lvaTable[tree->gtLclVar.gtLclNum].lvNormalizeOnLoad())
                    {
                        costEx += 1;
                        costSz += 1;
                    }
                }
                else
                {
                    costEx = IND_COST_EX;
                    costSz = 2;
                    /* Sign-extend and zero-extend are more expensive to load */
                    if (varTypeIsSmall(tree->TypeGet()))
                    {
                        costEx += 1;
                        costSz += 1;
                    }
                }
#if defined(_TARGET_AMD64_)
                // increase costSz for floating point locals
                if (isflt)
                {
                    costSz += 1;
                    if (!gtIsLikelyRegVar(tree))
                    {
                        costSz += 1;
                    }
                }
#endif
#if CPU_LONG_USES_REGPAIR
                if (varTypeIsLong(tree->TypeGet()))
                {
                    costEx *= 2; // Longs are twice as expensive
                    costSz *= 2;
                }
#endif
                break;

            case GT_CLS_VAR:
#ifdef _TARGET_ARM_
                // We generate movw/movt/ldr
                level  = 1;
                costEx = 3 + IND_COST_EX; // 6
                costSz = 4 + 4 + 2;       // 10
                break;
#endif
            case GT_LCL_FLD:
                level  = 1;
                costEx = IND_COST_EX;
                costSz = 4;
                if (varTypeIsSmall(tree->TypeGet()))
                {
                    costEx += 1;
                    costSz += 1;
                }
                break;

            case GT_PHI_ARG:
            case GT_ARGPLACE:
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
#if FEATURE_STACK_FP_X87
        if (isflt && (oper != GT_PHI_ARG))
        {
            codeGen->genIncrementFPstkLevel();
        }
#endif // FEATURE_STACK_FP_X87
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        int      lvlb; // preference for op2
        unsigned lvl2; // scratch variable

        GenTreePtr op1 = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtGetOp2IfPresent();

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
            ftreg |= op1->gtRsvdRegs;

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
#if defined(_TARGET_ARM_)
                    costEx = 1;
                    costSz = 1;
                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        costEx = 3;
                        costSz = 4;
                    }
#elif defined(_TARGET_ARM64_)
                    costEx = 1;
                    costSz = 2;
                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        costEx = 2;
                        costSz = 4;
                    }
#elif defined(_TARGET_XARCH_)
                    costEx = 1;
                    costSz = 2;

                    if (isflt || varTypeIsFloating(op1->TypeGet()))
                    {
                        /* cast involving floats always go through memory */
                        costEx = IND_COST_EX * 2;
                        costSz = 6;

#if FEATURE_STACK_FP_X87
                        if (isflt != varTypeIsFloating(op1->TypeGet()))
                        {
                            isflt ? codeGen->genIncrementFPstkLevel()  // Cast from int to float
                                  : codeGen->genDecrementFPstkLevel(); // Cast from float to int
                        }
#endif // FEATURE_STACK_FP_X87
                    }
#else
#error "Unknown _TARGET_"
#endif

#if CPU_LONG_USES_REGPAIR
                    if (varTypeIsLong(tree->TypeGet()))
                    {
                        if (varTypeIsUnsigned(tree->TypeGet()))
                        {
                            /* Cast to unsigned long */
                            costEx += 1;
                            costSz += 2;
                        }
                        else
                        {
                            /* Cast to signed long is slightly more costly */
                            costEx += 2;
                            costSz += 3;
                        }
                    }
#endif // CPU_LONG_USES_REGPAIR

                    /* Overflow casts are a lot more expensive */
                    if (tree->gtOverflow())
                    {
                        costEx += 6;
                        costSz += 6;
                    }

                    break;

                case GT_LIST:
                case GT_FIELD_LIST:
                case GT_NOP:
                    costEx = 0;
                    costSz = 0;
                    break;

                case GT_INTRINSIC:
                    // GT_INTRINSIC intrinsics Sin, Cos, Sqrt, Abs ... have higher costs.
                    // TODO: tune these costs target specific as some of these are
                    // target intrinsics and would cost less to generate code.
                    switch (tree->gtIntrinsic.gtIntrinsicId)
                    {
                        default:
                            assert(!"missing case for gtIntrinsicId");
                            costEx = 12;
                            costSz = 12;
                            break;

                        case CORINFO_INTRINSIC_Sin:
                        case CORINFO_INTRINSIC_Cos:
                        case CORINFO_INTRINSIC_Sqrt:
                        case CORINFO_INTRINSIC_Cosh:
                        case CORINFO_INTRINSIC_Sinh:
                        case CORINFO_INTRINSIC_Tan:
                        case CORINFO_INTRINSIC_Tanh:
                        case CORINFO_INTRINSIC_Asin:
                        case CORINFO_INTRINSIC_Acos:
                        case CORINFO_INTRINSIC_Atan:
                        case CORINFO_INTRINSIC_Atan2:
                        case CORINFO_INTRINSIC_Log10:
                        case CORINFO_INTRINSIC_Pow:
                        case CORINFO_INTRINSIC_Exp:
                        case CORINFO_INTRINSIC_Ceiling:
                        case CORINFO_INTRINSIC_Floor:
                        case CORINFO_INTRINSIC_Object_GetType:
                            // Giving intrinsics a large fixed execution cost is because we'd like to CSE
                            // them, even if they are implemented by calls. This is different from modeling
                            // user calls since we never CSE user calls.
                            costEx = 36;
                            costSz = 4;
                            break;

                        case CORINFO_INTRINSIC_Abs:
                            costEx = 5;
                            costSz = 15;
                            break;

                        case CORINFO_INTRINSIC_Round:
                            costEx = 3;
                            costSz = 4;
#if FEATURE_STACK_FP_X87
                            if (tree->TypeGet() == TYP_INT)
                            {
                                // This is a special case to handle the following
                                // optimization: conv.i4(round.d(d)) -> round.i(d)
                                codeGen->genDecrementFPstkLevel();
                            }
#endif // FEATURE_STACK_FP_X87
                            break;
                    }
                    level++;
                    break;

                case GT_NOT:
                case GT_NEG:
                    // We need to ensure that -x is evaluated before x or else
                    // we get burned while adjusting genFPstkLevel in x*-x where
                    // the rhs x is the last use of the enregsitered x.
                    //
                    // Even in the integer case we want to prefer to
                    // evaluate the side without the GT_NEG node, all other things
                    // being equal.  Also a GT_NOT requires a scratch register

                    level++;
                    break;

                case GT_ADDR:

#if FEATURE_STACK_FP_X87
                    /* If the operand was floating point, pop the value from the stack */

                    if (varTypeIsFloating(op1->TypeGet()))
                    {
                        codeGen->genDecrementFPstkLevel();
                    }
#endif // FEATURE_STACK_FP_X87
                    costEx = 0;
                    costSz = 1;

                    // If we have a GT_ADDR of an GT_IND we can just copy the costs from indOp1
                    if (op1->OperGet() == GT_IND)
                    {
                        GenTreePtr indOp1 = op1->gtOp.gtOp1;
                        costEx            = indOp1->gtCostEx;
                        costSz            = indOp1->gtCostSz;
                    }
                    break;

                case GT_ARR_LENGTH:
                    level++;

                    /* Array Len should be the same as an indirections, which have a costEx of IND_COST_EX */
                    costEx = IND_COST_EX - 1;
                    costSz = 2;
                    break;

                case GT_MKREFANY:
                case GT_OBJ:
                    // We estimate the cost of a GT_OBJ or GT_MKREFANY to be two loads (GT_INDs)
                    costEx = 2 * IND_COST_EX;
                    costSz = 2 * 2;
                    break;

                case GT_BOX:
                    // We estimate the cost of a GT_BOX to be two stores (GT_INDs)
                    costEx = 2 * IND_COST_EX;
                    costSz = 2 * 2;
                    break;

                case GT_BLK:
                case GT_IND:

                    /* An indirection should always have a non-zero level.
                     * Only constant leaf nodes have level 0.
                     */

                    if (level == 0)
                    {
                        level = 1;
                    }

                    /* Indirections have a costEx of IND_COST_EX */
                    costEx = IND_COST_EX;
                    costSz = 2;

                    /* If we have to sign-extend or zero-extend, bump the cost */
                    if (varTypeIsSmall(tree->TypeGet()))
                    {
                        costEx += 1;
                        costSz += 1;
                    }

                    if (isflt)
                    {
#if FEATURE_STACK_FP_X87
                        /* Indirect loads of FP values push a new value on the FP stack */
                        codeGen->genIncrementFPstkLevel();
#endif // FEATURE_STACK_FP_X87
                        if (tree->TypeGet() == TYP_DOUBLE)
                        {
                            costEx += 1;
                        }
#ifdef _TARGET_ARM_
                        costSz += 2;
#endif // _TARGET_ARM_
                    }

                    // Can we form an addressing mode with this indirection?
                    // TODO-CQ: Consider changing this to op1->gtEffectiveVal() to take into account
                    // addressing modes hidden under a comma node.

                    if (op1->gtOper == GT_ADD)
                    {
                        bool rev;
#if SCALED_ADDR_MODES
                        unsigned mul;
#endif
                        unsigned   cns;
                        GenTreePtr base;
                        GenTreePtr idx;

                        // See if we can form a complex addressing mode.

                        GenTreePtr addr = op1->gtEffectiveVal();

                        bool doAddrMode = true;
                        // See if we can form a complex addressing mode.
                        // Always use an addrMode for an array index indirection.
                        // TODO-1stClassStructs: Always do this, but first make sure it's
                        // done in Lowering as well.
                        if ((tree->gtFlags & GTF_IND_ARR_INDEX) == 0)
                        {
                            if (tree->TypeGet() == TYP_STRUCT)
                            {
                                doAddrMode = false;
                            }
                            else if (varTypeIsStruct(tree))
                            {
                                // This is a heuristic attempting to match prior behavior when indirections
                                // under a struct assignment would not be considered for addressing modes.
                                if (compCurStmt != nullptr)
                                {
                                    GenTree* expr = compCurStmt->gtStmt.gtStmtExpr;
                                    if ((expr->OperGet() == GT_ASG) &&
                                        ((expr->gtGetOp1() == tree) || (expr->gtGetOp2() == tree)))
                                    {
                                        doAddrMode = false;
                                    }
                                }
                            }
                        }
                        if ((doAddrMode) &&
                            codeGen->genCreateAddrMode(addr,     // address
                                                       0,        // mode
                                                       false,    // fold
                                                       RBM_NONE, // reg mask
                                                       &rev,     // reverse ops
                                                       &base,    // base addr
                                                       &idx,     // index val
#if SCALED_ADDR_MODES
                                                       &mul, // scaling
#endif
                                                       &cns,  // displacement
                                                       true)) // don't generate code
                        {
                            // We can form a complex addressing mode, so mark each of the interior
                            // nodes with GTF_ADDRMODE_NO_CSE and calculate a more accurate cost.

                            addr->gtFlags |= GTF_ADDRMODE_NO_CSE;
#ifdef _TARGET_XARCH_
                            // addrmodeCount is the count of items that we used to form
                            // an addressing mode.  The maximum value is 4 when we have
                            // all of these:   { base, idx, cns, mul }
                            //
                            unsigned addrmodeCount = 0;
                            if (base)
                            {
                                costEx += base->gtCostEx;
                                costSz += base->gtCostSz;
                                addrmodeCount++;
                            }

                            if (idx)
                            {
                                costEx += idx->gtCostEx;
                                costSz += idx->gtCostSz;
                                addrmodeCount++;
                            }

                            if (cns)
                            {
                                if (((signed char)cns) == ((int)cns))
                                {
                                    costSz += 1;
                                }
                                else
                                {
                                    costSz += 4;
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

                                GenTreePtr tmp = addr;
                                while (addrmodeCount > 0)
                                {
                                    // decrement the gtCosts for the interior GT_ADD or GT_LSH node by the remaining
                                    // addrmodeCount
                                    tmp->SetCosts(tmp->gtCostEx - addrmodeCount, tmp->gtCostSz - addrmodeCount);

                                    addrmodeCount--;
                                    if (addrmodeCount > 0)
                                    {
                                        GenTreePtr tmpOp1 = tmp->gtOp.gtOp1;
                                        GenTreePtr tmpOp2 = tmp->gtGetOp2();
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
#elif defined _TARGET_ARM_
                            if (base)
                            {
                                costEx += base->gtCostEx;
                                costSz += base->gtCostSz;
                                if ((base->gtOper == GT_LCL_VAR) && ((idx == NULL) || (cns == 0)))
                                {
                                    costSz -= 1;
                                }
                            }

                            if (idx)
                            {
                                costEx += idx->gtCostEx;
                                costSz += idx->gtCostSz;
                                if (mul > 0)
                                {
                                    costSz += 2;
                                }
                            }

                            if (cns)
                            {
                                if (cns >= 128) // small offsets fits into a 16-bit instruction
                                {
                                    if (cns < 4096) // medium offsets require a 32-bit instruction
                                    {
                                        if (!isflt)
                                            costSz += 2;
                                    }
                                    else
                                    {
                                        costEx += 2; // Very large offsets require movw/movt instructions
                                        costSz += 8;
                                    }
                                }
                            }
#elif defined _TARGET_ARM64_
                            if (base)
                            {
                                costEx += base->gtCostEx;
                                costSz += base->gtCostSz;
                            }

                            if (idx)
                            {
                                costEx += idx->gtCostEx;
                                costSz += idx->gtCostSz;
                            }

                            if (cns != 0)
                            {
                                if (cns >= (4096 * genTypeSize(tree->TypeGet())))
                                {
                                    costEx += 1;
                                    costSz += 4;
                                }
                            }
#else
#error "Unknown _TARGET_"
#endif

                            assert(addr->gtOper == GT_ADD);
                            assert(!addr->gtOverflow());
                            assert(op2 == nullptr);
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

#if SCALED_ADDR_MODES
                            assert((base != nullptr) || (idx != nullptr && mul >= 2));
#else
                            assert(base != NULL);
#endif

                            INDEBUG(GenTreePtr op1Save = addr);

                            /* Walk addr looking for non-overflow GT_ADDs */
                            gtWalkOp(&addr, &op2, base, false);

                            // addr and op2 are now children of the root GT_ADD of the addressing mode
                            assert(addr != op1Save);
                            assert(op2 != nullptr);

                            /* Walk addr looking for non-overflow GT_ADDs of constants */
                            gtWalkOp(&addr, &op2, nullptr, true);

                            // TODO-Cleanup: It seems very strange that we might walk down op2 now, even though the
                            // prior
                            //           call to gtWalkOp() may have altered op2.

                            /* Walk op2 looking for non-overflow GT_ADDs of constants */
                            gtWalkOp(&op2, &addr, nullptr, true);

                            // OK we are done walking the tree
                            // Now assert that addr and op2 correspond with base and idx
                            // in one of the several acceptable ways.

                            // Note that sometimes addr/op2 is equal to idx/base
                            // and other times addr/op2 is a GT_COMMA node with
                            // an effective value that is idx/base

                            if (mul > 1)
                            {
                                if ((addr != base) && (addr->gtOper == GT_LSH))
                                {
                                    addr->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                    if (addr->gtOp.gtOp1->gtOper == GT_MUL)
                                    {
                                        addr->gtOp.gtOp1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                    }
                                    assert((base == nullptr) || (op2 == base) ||
                                           (op2->gtEffectiveVal() == base->gtEffectiveVal()) ||
                                           (gtWalkOpEffectiveVal(op2) == gtWalkOpEffectiveVal(base)));
                                }
                                else
                                {
                                    assert(op2);
                                    assert(op2->gtOper == GT_LSH || op2->gtOper == GT_MUL);
                                    op2->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                    // We may have eliminated multiple shifts and multiplies in the addressing mode,
                                    // so navigate down through them to get to "idx".
                                    GenTreePtr op2op1 = op2->gtOp.gtOp1;
                                    while ((op2op1->gtOper == GT_LSH || op2op1->gtOper == GT_MUL) && op2op1 != idx)
                                    {
                                        op2op1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                        op2op1 = op2op1->gtOp.gtOp1;
                                    }
                                    assert(addr->gtEffectiveVal() == base);
                                    assert(op2op1 == idx);
                                }
                            }
                            else
                            {
                                assert(mul == 0);

                                if ((addr == idx) || (addr->gtEffectiveVal() == idx))
                                {
                                    if (idx != nullptr)
                                    {
                                        if ((addr->gtOper == GT_MUL) || (addr->gtOper == GT_LSH))
                                        {
                                            if ((addr->gtOp.gtOp1->gtOper == GT_NOP) ||
                                                (addr->gtOp.gtOp1->gtOper == GT_MUL &&
                                                 addr->gtOp.gtOp1->gtOp.gtOp1->gtOper == GT_NOP))
                                            {
                                                addr->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                                if (addr->gtOp.gtOp1->gtOper == GT_MUL)
                                                {
                                                    addr->gtOp.gtOp1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                                }
                                            }
                                        }
                                    }
                                    assert((op2 == base) || (op2->gtEffectiveVal() == base));
                                }
                                else if ((addr == base) || (addr->gtEffectiveVal() == base))
                                {
                                    if (idx != nullptr)
                                    {
                                        assert(op2);
                                        if ((op2->gtOper == GT_MUL) || (op2->gtOper == GT_LSH))
                                        {
                                            if ((op2->gtOp.gtOp1->gtOper == GT_NOP) ||
                                                (op2->gtOp.gtOp1->gtOper == GT_MUL &&
                                                 op2->gtOp.gtOp1->gtOp.gtOp1->gtOper == GT_NOP))
                                            {
                                                // assert(bRngChk);
                                                op2->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                                if (op2->gtOp.gtOp1->gtOper == GT_MUL)
                                                {
                                                    op2->gtOp.gtOp1->gtFlags |= GTF_ADDRMODE_NO_CSE;
                                                }
                                            }
                                        }
                                        assert((op2 == idx) || (op2->gtEffectiveVal() == idx));
                                    }
                                }
                                else
                                {
                                    // addr isn't base or idx. Is this possible? Or should there be an assert?
                                }
                            }
                            goto DONE;

                        } // end  if  (genCreateAddrMode(...))

                    } // end if  (op1->gtOper == GT_ADD)
                    else if (gtIsLikelyRegVar(op1))
                    {
                        /* Indirection of an enregister LCL_VAR, don't increase costEx/costSz */
                        goto DONE;
                    }
#ifdef _TARGET_XARCH_
                    else if (op1->IsCnsIntOrI())
                    {
                        // Indirection of a CNS_INT, subtract 1 from costEx
                        // makes costEx 3 for x86 and 4 for amd64
                        //
                        costEx += (op1->gtCostEx - 1);
                        costSz += op1->gtCostSz;
                        goto DONE;
                    }
#endif
                    break;

                default:
                    break;
            }
            costEx += op1->gtCostEx;
            costSz += op1->gtCostSz;
            goto DONE;
        }

        /* Binary operator - check for certain special cases */

        lvlb = 0;

        /* Default Binary ops have a cost of 1,1 */
        costEx = 1;
        costSz = 1;

#ifdef _TARGET_ARM_
        if (isflt)
        {
            costSz += 2;
        }
#endif
#ifndef _TARGET_64BIT_
        if (varTypeIsLong(op1->TypeGet()))
        {
            /* Operations on longs are more expensive */
            costEx += 3;
            costSz += 3;
        }
#endif
        switch (oper)
        {
            case GT_MOD:
            case GT_UMOD:

                /* Modulo by a power of 2 is easy */

                if (op2->IsCnsIntOrI())
                {
                    size_t ival = op2->gtIntConCommon.IconValue();

                    if (ival > 0 && ival == genFindLowestBit(ival))
                    {
                        break;
                    }
                }

                __fallthrough;

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
                    lvlb -= 3;

#ifdef _TARGET_XARCH_
                    // the idiv and div instruction requires EAX/EDX
                    ftreg |= RBM_EAX | RBM_EDX;
#endif
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

#ifdef _TARGET_X86_
                    if ((tree->gtType == TYP_LONG) || tree->gtOverflow())
                    {
                        /* We use imulEAX for TYP_LONG and overflow multiplications */
                        // Encourage the first operand to be evaluated (into EAX/EDX) first */
                        lvlb -= 4;

                        // the imulEAX instruction ob x86 requires EDX:EAX
                        ftreg |= (RBM_EAX | RBM_EDX);

                        /* The 64-bit imul instruction costs more */
                        costEx += 4;
                    }
#endif //  _TARGET_X86_
                }
                break;

            case GT_ADD:
            case GT_SUB:
#ifdef LEGACY_BACKEND
            case GT_ASG_ADD:
            case GT_ASG_SUB:
#endif
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

            case GT_COMMA:

                /* Comma tosses the result of the left operand */
                gtSetEvalOrderAndRestoreFPstkLevel(op1);
                level = gtSetEvalOrder(op2);

                ftreg |= op1->gtRsvdRegs | op2->gtRsvdRegs;

                /* GT_COMMA cost is the sum of op1 and op2 costs */
                costEx = (op1->gtCostEx + op2->gtCostEx);
                costSz = (op1->gtCostSz + op2->gtCostSz);

                goto DONE;

            case GT_COLON:

                level = gtSetEvalOrderAndRestoreFPstkLevel(op1);
                lvl2  = gtSetEvalOrder(op2);

                if (level < lvl2)
                {
                    level = lvl2;
                }
                else if (level == lvl2)
                {
                    level += 1;
                }

                ftreg |= op1->gtRsvdRegs | op2->gtRsvdRegs;
                costEx = op1->gtCostEx + op2->gtCostEx;
                costSz = op1->gtCostSz + op2->gtCostSz;

                goto DONE;

            case GT_LIST:
            case GT_FIELD_LIST:
            {
                const bool isListCallArgs = false;
                const bool callArgsInRegs = false;
                return gtSetListOrder(tree, isListCallArgs, callArgsInRegs);
            }

            default:
                break;
        }

        /* Assignments need a bit of special handling */

        if (GenTree::OperIsAssignment(oper))
        {
            /* Process the target */

            level = gtSetEvalOrder(op1);

#if FEATURE_STACK_FP_X87

            /* If assigning an FP value, the target won't get pushed */

            if (isflt && !tree->IsPhiDefn())
            {
                op1->gtFPlvl--;
                codeGen->genDecrementFPstkLevel();
            }

#endif // FEATURE_STACK_FP_X87

            if (gtIsLikelyRegVar(op1))
            {
                assert(lvlb == 0);
                lvl2 = gtSetEvalOrder(op2);
                if (oper != GT_ASG)
                {
                    ftreg |= op2->gtRsvdRegs;
                }

                /* Assignment to an enregistered LCL_VAR */
                costEx = op2->gtCostEx;
                costSz = max(3, op2->gtCostSz); // 3 is an estimate for a reg-reg assignment
                goto DONE_OP1_AFTER_COST;
            }
            else if (oper != GT_ASG)
            {
                // Assign-Op instructions read and write op1
                //
                costEx += op1->gtCostEx;
#ifdef _TARGET_ARM_
                costSz += op1->gtCostSz;
#endif
            }

            goto DONE_OP1;
        }

        /* Process the sub-operands */

        level = gtSetEvalOrder(op1);
        if (lvlb < 0)
        {
            level -= lvlb; // lvlb is negative, so this increases level
            lvlb = 0;
        }

    DONE_OP1:
        assert(lvlb >= 0);
        lvl2 = gtSetEvalOrder(op2) + lvlb;
        ftreg |= op1->gtRsvdRegs;
        // For assignment, we execute op2 before op1, except that for block
        // ops the destination address is evaluated first.
        if ((oper != GT_ASG) || tree->OperIsBlkOp())
        {
            ftreg |= op2->gtRsvdRegs;
        }

        costEx += (op1->gtCostEx + op2->gtCostEx);
        costSz += (op1->gtCostSz + op2->gtCostSz);

    DONE_OP1_AFTER_COST:
#if FEATURE_STACK_FP_X87
        /*
            Binary FP operators pop 2 operands and produce 1 result;
            FP comparisons pop 2 operands and produces 0 results.
            assignments consume 1 value and don't produce anything.
         */

        if (isflt && !tree->IsPhiDefn())
        {
            assert(oper != GT_COMMA);
            codeGen->genDecrementFPstkLevel();
        }
#endif // FEATURE_STACK_FP_X87

        bool bReverseInAssignment = false;
        if (GenTree::OperIsAssignment(oper))
        {
            GenTreePtr op1Val = op1;

            if (tree->gtOper == GT_ASG)
            {
                // Skip over the GT_IND/GT_ADDR tree (if one exists)
                //
                if ((op1->gtOper == GT_IND) && (op1->gtOp.gtOp1->gtOper == GT_ADDR))
                {
                    op1Val = op1->gtOp.gtOp1->gtOp.gtOp1;
                }
            }

            switch (op1Val->gtOper)
            {
                case GT_IND:

                    // Struct assignments are different from scalar assignments in that semantically
                    // the address of op1 is evaluated prior to op2.
                    if (!varTypeIsStruct(op1))
                    {
                        // If we have any side effects on the GT_IND child node
                        // we have to evaluate op1 first.
                        if (op1Val->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT)
                        {
                            break;
                        }
                    }

                    // In case op2 assigns to a local var that is used in op1Val, we have to evaluate op1Val first.
                    if (op2->gtFlags & GTF_ASG)
                    {
                        break;
                    }

                    // If op2 is simple then evaluate op1 first

                    if (op2->OperKind() & GTK_LEAF)
                    {
                        break;
                    }

                // fall through and set GTF_REVERSE_OPS

                case GT_LCL_VAR:
                case GT_LCL_FLD:
                case GT_BLK:
                case GT_OBJ:
                case GT_DYN_BLK:

                    // We evaluate op2 before op1
                    bReverseInAssignment = true;
                    tree->gtFlags |= GTF_REVERSE_OPS;
                    break;

                default:
                    break;
            }
        }
        else if (kind & GTK_RELOP)
        {
            /* Float compares remove both operands from the FP stack */
            /* Also FP comparison uses EAX for flags */

            if (varTypeIsFloating(op1->TypeGet()))
            {
#if FEATURE_STACK_FP_X87
                codeGen->genDecrementFPstkLevel(2);
#endif // FEATURE_STACK_FP_X87
#ifdef _TARGET_XARCH_
                ftreg |= RBM_EAX;
#endif
                level++;
                lvl2++;
            }
#if CPU_LONG_USES_REGPAIR
            if (varTypeIsLong(op1->TypeGet()))
            {
                costEx *= 2; // Longs are twice as expensive
                costSz *= 2;
            }
#endif
            if ((tree->gtFlags & GTF_RELOP_JMP_USED) == 0)
            {
                /* Using a setcc instruction is more expensive */
                costEx += 3;
            }
        }

        /* Check for other interesting cases */

        switch (oper)
        {
            case GT_LSH:
            case GT_RSH:
            case GT_RSZ:
            case GT_ROL:
            case GT_ROR:
#ifdef LEGACY_BACKEND
            case GT_ASG_LSH:
            case GT_ASG_RSH:
            case GT_ASG_RSZ:
#endif
                /* Variable sized shifts are more expensive and use REG_SHIFT */

                if (!op2->IsCnsIntOrI())
                {
                    costEx += 3;
                    if (REG_SHIFT != REG_NA)
                    {
                        ftreg |= RBM_SHIFT;
                    }

#ifndef _TARGET_64BIT_
                    // Variable sized LONG shifts require the use of a helper call
                    //
                    if (tree->gtType == TYP_LONG)
                    {
                        level += 5;
                        lvl2 += 5;
                        costEx += 3 * IND_COST_EX;
                        costSz += 4;
                        ftreg |= RBM_CALLEE_TRASH;
                    }
#endif // !_TARGET_64BIT_
                }
                break;

            case GT_INTRINSIC:

                switch (tree->gtIntrinsic.gtIntrinsicId)
                {
                    case CORINFO_INTRINSIC_Atan2:
                    case CORINFO_INTRINSIC_Pow:
                        // These math intrinsics are actually implemented by user calls.
                        // Increase the Sethi 'complexity' by two to reflect the argument
                        // register requirement.
                        level += 2;
                        break;
                    default:
                        assert(!"Unknown binary GT_INTRINSIC operator");
                        break;
                }

                break;

            default:
                break;
        }

        /* We need to evalutate constants later as many places in codegen
           can't handle op1 being a constant. This is normally naturally
           enforced as constants have the least level of 0. However,
           sometimes we end up with a tree like "cns1 < nop(cns2)". In
           such cases, both sides have a level of 0. So encourage constants
           to be evaluated last in such cases */

        if ((level == 0) && (level == lvl2) && (op1->OperKind() & GTK_CONST) &&
            (tree->OperIsCommutative() || tree->OperIsCompare()))
        {
            lvl2++;
        }

        /* We try to swap operands if the second one is more expensive */
        bool       tryToSwap;
        GenTreePtr opA, opB;

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            opA = op2;
            opB = op1;
        }
        else
        {
            opA = op1;
            opB = op2;
        }

        if (fgOrder == FGOrderLinear)
        {
            // Don't swap anything if we're in linear order; we're really just interested in the costs.
            tryToSwap = false;
        }
        else if (bReverseInAssignment)
        {
            // Assignments are special, we want the reverseops flags
            // so if possible it was set above.
            tryToSwap = false;
        }
        else if ((oper == GT_INTRINSIC) &&
                 Compiler::IsIntrinsicImplementedByUserCall(tree->AsIntrinsic()->gtIntrinsicId))
        {
            // We do not swap operand execution order for intrinsics that are implemented by user calls
            // because of trickiness around ensuring the execution order does not change during rationalization.
            tryToSwap = false;
        }
        else
        {
            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                tryToSwap = (level > lvl2);
            }
            else
            {
                tryToSwap = (level < lvl2);
            }

            // Try to force extra swapping when in the stress mode:
            if (compStressCompile(STRESS_REVERSE_FLAG, 60) && ((tree->gtFlags & GTF_REVERSE_OPS) == 0) &&
                ((op2->OperKind() & GTK_CONST) == 0))
            {
                tryToSwap = true;
            }
        }

        if (tryToSwap)
        {
            bool canSwap = gtCanSwapOrder(opA, opB);

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

                        __fallthrough;

                    case GT_ADD:
                    case GT_MUL:

                    case GT_OR:
                    case GT_XOR:
                    case GT_AND:

                        /* Swap the operands */

                        tree->gtOp.gtOp1 = op2;
                        tree->gtOp.gtOp2 = op1;

#if FEATURE_STACK_FP_X87
                        /* We may have to recompute FP levels */
                        if (op1->gtFPlvl || op2->gtFPlvl)
                            gtFPstLvlRedo = true;
#endif // FEATURE_STACK_FP_X87
                        break;

                    case GT_QMARK:
                    case GT_COLON:
                    case GT_MKREFANY:
                        break;

                    case GT_LIST:
                    case GT_FIELD_LIST:
                        break;

                    case GT_SUB:
#ifdef LEGACY_BACKEND
                        // For LSRA we require that LclVars be "evaluated" just prior to their use,
                        // so that if they must be reloaded, it is done at the right place.
                        // This means that we allow reverse evaluation for all BINOPs.
                        // (Note that this doesn't affect the order of the operands in the instruction).
                        if (!isflt)
                            break;
#endif // LEGACY_BACKEND

                        __fallthrough;

                    default:

                        /* Mark the operand's evaluation order to be swapped */
                        if (tree->gtFlags & GTF_REVERSE_OPS)
                        {
                            tree->gtFlags &= ~GTF_REVERSE_OPS;
                        }
                        else
                        {
                            tree->gtFlags |= GTF_REVERSE_OPS;
                        }

#if FEATURE_STACK_FP_X87
                        /* We may have to recompute FP levels */
                        if (op1->gtFPlvl || op2->gtFPlvl)
                            gtFPstLvlRedo = true;
#endif // FEATURE_STACK_FP_X87

                        break;
                }
            }
        }

        /* Swap the level counts */
        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            unsigned tmpl;

            tmpl  = level;
            level = lvl2;
            lvl2  = tmpl;
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

            /* Evaluate the 'this' argument, if present */

            if (tree->gtCall.gtCallObjp)
            {
                GenTreePtr thisVal = tree->gtCall.gtCallObjp;

                lvl2 = gtSetEvalOrder(thisVal);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += thisVal->gtCostEx;
                costSz += thisVal->gtCostSz + 1;
                ftreg |= thisVal->gtRsvdRegs;
            }

            /* Evaluate the arguments, right to left */

            if (tree->gtCall.gtCallArgs)
            {
#if FEATURE_STACK_FP_X87
                FPlvlSave = codeGen->genGetFPstkLevel();
#endif // FEATURE_STACK_FP_X87
                const bool isListCallArgs = true;
                const bool callArgsInRegs = false;
                lvl2                      = gtSetListOrder(tree->gtCall.gtCallArgs, isListCallArgs, callArgsInRegs);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += tree->gtCall.gtCallArgs->gtCostEx;
                costSz += tree->gtCall.gtCallArgs->gtCostSz;
                ftreg |= tree->gtCall.gtCallArgs->gtRsvdRegs;
#if FEATURE_STACK_FP_X87
                codeGen->genResetFPstkLevel(FPlvlSave);
#endif // FEATURE_STACK_FP_X87
            }

            /* Evaluate the temp register arguments list
             * This is a "hidden" list and its only purpose is to
             * extend the life of temps until we make the call */

            if (tree->gtCall.gtCallLateArgs)
            {
#if FEATURE_STACK_FP_X87
                FPlvlSave = codeGen->genGetFPstkLevel();
#endif // FEATURE_STACK_FP_X87
                const bool isListCallArgs = true;
                const bool callArgsInRegs = true;
                lvl2                      = gtSetListOrder(tree->gtCall.gtCallLateArgs, isListCallArgs, callArgsInRegs);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += tree->gtCall.gtCallLateArgs->gtCostEx;
                costSz += tree->gtCall.gtCallLateArgs->gtCostSz;
                ftreg |= tree->gtCall.gtCallLateArgs->gtRsvdRegs;
#if FEATURE_STACK_FP_X87
                codeGen->genResetFPstkLevel(FPlvlSave);
#endif // FEATURE_STACK_FP_X87
            }

            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
                // pinvoke-calli cookie is a constant, or constant indirection
                assert(tree->gtCall.gtCallCookie == nullptr || tree->gtCall.gtCallCookie->gtOper == GT_CNS_INT ||
                       tree->gtCall.gtCallCookie->gtOper == GT_IND);

                GenTreePtr indirect = tree->gtCall.gtCallAddr;

                lvl2 = gtSetEvalOrder(indirect);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += indirect->gtCostEx + IND_COST_EX;
                costSz += indirect->gtCostSz;
                ftreg |= indirect->gtRsvdRegs;
            }
            else
            {
#ifdef _TARGET_ARM_
                if ((tree->gtFlags & GTF_CALL_VIRT_KIND_MASK) == GTF_CALL_VIRT_STUB)
                {
                    // We generate movw/movt/ldr
                    costEx += (1 + IND_COST_EX);
                    costSz += 8;
                    if (tree->gtCall.gtCallMoreFlags & GTF_CALL_M_VIRTSTUB_REL_INDIRECT)
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
#ifdef _TARGET_XARCH_
                costSz += 3;
#endif
            }

            level += 1;

            unsigned callKind;
            callKind = (tree->gtFlags & GTF_CALL_VIRT_KIND_MASK);

            /* Virtual calls are a bit more expensive */
            if (callKind != GTF_CALL_NONVIRT)
            {
                costEx += 2 * IND_COST_EX;
                costSz += 2;
            }

            /* Virtual stub calls also must reserve the VIRTUAL_STUB_PARAM reg */
            if (callKind == GTF_CALL_VIRT_STUB)
            {
                ftreg |= virtualStubParamInfo->GetRegMask();
            }

#ifdef FEATURE_READYTORUN_COMPILER
#if defined(_TARGET_ARMARCH_)
            if (tree->gtCall.IsR2RRelativeIndir())
            {
                ftreg |= RBM_R2R_INDIRECT_PARAM;
            }
#endif
#endif

#ifdef LEGACY_BACKEND
            // Normally function calls don't preserve caller save registers
            //   and thus are much more expensive.
            // However a few function calls do preserve these registers
            //   such as the GC WriteBarrier helper calls.

            if (!(tree->gtFlags & GTF_CALL_REG_SAVE))
#endif // LEGACY_BACKEND
            {
                level += 5;
                costEx += 3 * IND_COST_EX;
                ftreg |= RBM_CALLEE_TRASH;
            }

#if FEATURE_STACK_FP_X87
            if (isflt)
                codeGen->genIncrementFPstkLevel();
#endif // FEATURE_STACK_FP_X87

            break;

        case GT_ARR_ELEM:

            level  = gtSetEvalOrder(tree->gtArrElem.gtArrObj);
            costEx = tree->gtArrElem.gtArrObj->gtCostEx;
            costSz = tree->gtArrElem.gtArrObj->gtCostSz;

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                lvl2 = gtSetEvalOrder(tree->gtArrElem.gtArrInds[dim]);
                if (level < lvl2)
                {
                    level = lvl2;
                }
                costEx += tree->gtArrElem.gtArrInds[dim]->gtCostEx;
                costSz += tree->gtArrElem.gtArrInds[dim]->gtCostSz;
            }

#if FEATURE_STACK_FP_X87
            if (isflt)
                codeGen->genIncrementFPstkLevel();
#endif // FEATURE_STACK_FP_X87
            level += tree->gtArrElem.gtArrRank;
            costEx += 2 + (tree->gtArrElem.gtArrRank * (IND_COST_EX + 1));
            costSz += 2 + (tree->gtArrElem.gtArrRank * 2);
            break;

        case GT_ARR_OFFSET:
            level  = gtSetEvalOrder(tree->gtArrOffs.gtOffset);
            costEx = tree->gtArrOffs.gtOffset->gtCostEx;
            costSz = tree->gtArrOffs.gtOffset->gtCostSz;
            lvl2   = gtSetEvalOrder(tree->gtArrOffs.gtIndex);
            level  = max(level, lvl2);
            costEx += tree->gtArrOffs.gtIndex->gtCostEx;
            costSz += tree->gtArrOffs.gtIndex->gtCostSz;
            lvl2  = gtSetEvalOrder(tree->gtArrOffs.gtArrObj);
            level = max(level, lvl2);
            costEx += tree->gtArrOffs.gtArrObj->gtCostEx;
            costSz += tree->gtArrOffs.gtArrObj->gtCostSz;
            break;

        case GT_CMPXCHG:

            level  = gtSetEvalOrder(tree->gtCmpXchg.gtOpLocation);
            costSz = tree->gtCmpXchg.gtOpLocation->gtCostSz;

            lvl2 = gtSetEvalOrder(tree->gtCmpXchg.gtOpValue);
            if (level < lvl2)
            {
                level = lvl2;
            }
            costSz += tree->gtCmpXchg.gtOpValue->gtCostSz;

            lvl2 = gtSetEvalOrder(tree->gtCmpXchg.gtOpComparand);
            if (level < lvl2)
            {
                level = lvl2;
            }
            costSz += tree->gtCmpXchg.gtOpComparand->gtCostSz;

            costEx = MAX_COST; // Seriously, what could be more expensive than lock cmpxchg?
            costSz += 5;       // size of lock cmpxchg [reg+C], reg
#ifdef _TARGET_XARCH_
            ftreg |= RBM_EAX; // cmpxchg must be evaluated into eax.
#endif
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif                  // FEATURE_SIMD
            costEx = 4; // cmp reg,reg and jae throw (not taken)
            costSz = 7; // jump to cold section

            level = gtSetEvalOrder(tree->gtBoundsChk.gtIndex);
            costEx += tree->gtBoundsChk.gtIndex->gtCostEx;
            costSz += tree->gtBoundsChk.gtIndex->gtCostSz;

            lvl2 = gtSetEvalOrder(tree->gtBoundsChk.gtArrLen);
            if (level < lvl2)
            {
                level = lvl2;
            }
            costEx += tree->gtBoundsChk.gtArrLen->gtCostEx;
            costSz += tree->gtBoundsChk.gtArrLen->gtCostSz;

            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
        {
            costEx = 0;
            costSz = 0;
            level  = 0;
            if (oper == GT_STORE_DYN_BLK)
            {
                lvl2  = gtSetEvalOrder(tree->gtDynBlk.Data());
                level = max(level, lvl2);
                costEx += tree->gtDynBlk.Data()->gtCostEx;
                costSz += tree->gtDynBlk.Data()->gtCostSz;
            }
            lvl2               = gtSetEvalOrder(tree->gtDynBlk.Addr());
            level              = max(level, lvl2);
            costEx             = tree->gtDynBlk.Addr()->gtCostEx;
            costSz             = tree->gtDynBlk.Addr()->gtCostSz;
            unsigned sizeLevel = gtSetEvalOrder(tree->gtDynBlk.gtDynamicSize);

            // Determine whether the size node should be evaluated first.
            // We would like to do this if the sizeLevel is larger than the current level,
            // but we have to ensure that we obey ordering constraints.
            if (tree->AsDynBlk()->gtEvalSizeFirst != (level < sizeLevel))
            {
                bool canChange = true;

                GenTree* sizeNode = tree->AsDynBlk()->gtDynamicSize;
                GenTree* dst      = tree->AsDynBlk()->Addr();
                GenTree* src      = tree->AsDynBlk()->Data();

                if (tree->AsDynBlk()->gtEvalSizeFirst)
                {
                    canChange = gtCanSwapOrder(sizeNode, dst);
                    if (canChange && (src != nullptr))
                    {
                        canChange = gtCanSwapOrder(sizeNode, src);
                    }
                }
                else
                {
                    canChange = gtCanSwapOrder(dst, sizeNode);
                    if (canChange && (src != nullptr))
                    {
                        gtCanSwapOrder(src, sizeNode);
                    }
                }
                if (canChange)
                {
                    tree->AsDynBlk()->gtEvalSizeFirst = (level < sizeLevel);
                }
            }
            level = max(level, sizeLevel);
            costEx += tree->gtDynBlk.gtDynamicSize->gtCostEx;
            costSz += tree->gtDynBlk.gtDynamicSize->gtCostSz;
        }
        break;

        case GT_INDEX_ADDR:
            costEx = 6; // cmp reg,reg; jae throw; mov reg, [addrmode]  (not taken)
            costSz = 9; // jump to cold section

            level = gtSetEvalOrder(tree->AsIndexAddr()->Index());
            costEx += tree->AsIndexAddr()->Index()->gtCostEx;
            costSz += tree->AsIndexAddr()->Index()->gtCostSz;

            lvl2 = gtSetEvalOrder(tree->AsIndexAddr()->Arr());
            if (level < lvl2)
            {
                level = lvl2;
            }
            costEx += tree->AsIndexAddr()->Arr()->gtCostEx;
            costSz += tree->AsIndexAddr()->Arr()->gtCostSz;
            break;

        default:
#ifdef DEBUG
            if (verbose)
            {
                printf("unexpected operator in this tree:\n");
                gtDispTree(tree);
            }
#endif
            NO_WAY("unexpected operator");
    }

DONE:

#if FEATURE_STACK_FP_X87
    // printf("[FPlvl=%2u] ", genGetFPstkLevel()); gtDispTree(tree, 0, true);
    noway_assert((unsigned char)codeGen->genFPstkLevel == codeGen->genFPstkLevel);
    tree->gtFPlvl = (unsigned char)codeGen->genFPstkLevel;

    if (codeGen->genFPstkLevel > tmpDoubleSpillMax)
        tmpDoubleSpillMax = codeGen->genFPstkLevel;
#endif // FEATURE_STACK_FP_X87

    tree->gtRsvdRegs = (regMaskSmall)ftreg;

    // Some path through this function must have set the costs.
    assert(costEx != -1);
    assert(costSz != -1);

    tree->SetCosts(costEx, costSz);

    return level;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

#if FEATURE_STACK_FP_X87

/*****************************************************************************/
void Compiler::gtComputeFPlvls(GenTreePtr tree)
{
    genTreeOps oper;
    unsigned   kind;
    bool       isflt;
    unsigned   savFPstkLevel;

    noway_assert(tree);
    noway_assert(tree->gtOper != GT_STMT);

    /* Figure out what kind of a node we have */

    oper  = tree->OperGet();
    kind  = tree->OperKind();
    isflt = varTypeIsFloating(tree->TypeGet()) ? 1 : 0;

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        codeGen->genFPstkLevel += isflt;
        goto DONE;
    }

    /* Is it a 'simple' unary/binary operator? */

    if (kind & GTK_SMPOP)
    {
        GenTreePtr op1 = tree->gtOp.gtOp1;
        GenTreePtr op2 = tree->gtGetOp2IfPresent();

        /* Check for some special cases */

        switch (oper)
        {
            case GT_IND:

                gtComputeFPlvls(op1);

                /* Indirect loads of FP values push a new value on the FP stack */

                codeGen->genFPstkLevel += isflt;
                goto DONE;

            case GT_CAST:

                gtComputeFPlvls(op1);

                /* Casts between non-FP and FP push on / pop from the FP stack */

                if (varTypeIsFloating(op1->TypeGet()))
                {
                    if (isflt == false)
                        codeGen->genFPstkLevel--;
                }
                else
                {
                    if (isflt != false)
                        codeGen->genFPstkLevel++;
                }

                goto DONE;

            case GT_LIST:  /* GT_LIST presumably part of an argument list */
            case GT_COMMA: /* Comma tosses the result of the left operand */

                savFPstkLevel = codeGen->genFPstkLevel;
                gtComputeFPlvls(op1);
                codeGen->genFPstkLevel = savFPstkLevel;

                if (op2)
                    gtComputeFPlvls(op2);

                goto DONE;

            default:
                break;
        }

        if (!op1)
        {
            if (!op2)
                goto DONE;

            gtComputeFPlvls(op2);
            goto DONE;
        }

        if (!op2)
        {
            gtComputeFPlvls(op1);
            if (oper == GT_ADDR)
            {
                /* If the operand was floating point pop the value from the stack */
                if (varTypeIsFloating(op1->TypeGet()))
                {
                    noway_assert(codeGen->genFPstkLevel);
                    codeGen->genFPstkLevel--;
                }
            }

            // This is a special case to handle the following
            // optimization: conv.i4(round.d(d)) -> round.i(d)

            if (oper == GT_INTRINSIC && tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Round &&
                tree->TypeGet() == TYP_INT)
            {
                codeGen->genFPstkLevel--;
            }

            goto DONE;
        }

        /* FP assignments need a bit special handling */

        if (isflt && (kind & GTK_ASGOP))
        {
            /* The target of the assignment won't get pushed */

            if (tree->gtFlags & GTF_REVERSE_OPS)
            {
                gtComputeFPlvls(op2);
                gtComputeFPlvls(op1);
                op1->gtFPlvl--;
                codeGen->genFPstkLevel--;
            }
            else
            {
                gtComputeFPlvls(op1);
                op1->gtFPlvl--;
                codeGen->genFPstkLevel--;
                gtComputeFPlvls(op2);
            }

            codeGen->genFPstkLevel--;
            goto DONE;
        }

        /* Here we have a binary operator; visit operands in proper order */

        if (tree->gtFlags & GTF_REVERSE_OPS)
        {
            gtComputeFPlvls(op2);
            gtComputeFPlvls(op1);
        }
        else
        {
            gtComputeFPlvls(op1);
            gtComputeFPlvls(op2);
        }

        /*
            Binary FP operators pop 2 operands and produce 1 result;
            assignments consume 1 value and don't produce any.
         */

        if (isflt)
            codeGen->genFPstkLevel--;

        /* Float compares remove both operands from the FP stack */

        if (kind & GTK_RELOP)
        {
            if (varTypeIsFloating(op1->TypeGet()))
                codeGen->genFPstkLevel -= 2;
        }

        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_FIELD:
            gtComputeFPlvls(tree->gtField.gtFldObj);
            codeGen->genFPstkLevel += isflt;
            break;

        case GT_CALL:

            if (tree->gtCall.gtCallObjp)
                gtComputeFPlvls(tree->gtCall.gtCallObjp);

            if (tree->gtCall.gtCallArgs)
            {
                savFPstkLevel = codeGen->genFPstkLevel;
                gtComputeFPlvls(tree->gtCall.gtCallArgs);
                codeGen->genFPstkLevel = savFPstkLevel;
            }

            if (tree->gtCall.gtCallLateArgs)
            {
                savFPstkLevel = codeGen->genFPstkLevel;
                gtComputeFPlvls(tree->gtCall.gtCallLateArgs);
                codeGen->genFPstkLevel = savFPstkLevel;
            }

            codeGen->genFPstkLevel += isflt;
            break;

        case GT_ARR_ELEM:

            gtComputeFPlvls(tree->gtArrElem.gtArrObj);

            unsigned dim;
            for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
                gtComputeFPlvls(tree->gtArrElem.gtArrInds[dim]);

            /* Loads of FP values push a new value on the FP stack */
            codeGen->genFPstkLevel += isflt;
            break;

        case GT_CMPXCHG:
            // Evaluate the trees left to right
            gtComputeFPlvls(tree->gtCmpXchg.gtOpLocation);
            gtComputeFPlvls(tree->gtCmpXchg.gtOpValue);
            gtComputeFPlvls(tree->gtCmpXchg.gtOpComparand);
            noway_assert(!isflt);
            break;

        case GT_ARR_BOUNDS_CHECK:
            gtComputeFPlvls(tree->gtBoundsChk.gtIndex);
            gtComputeFPlvls(tree->gtBoundsChk.gtArrLen);
            noway_assert(!isflt);
            break;

        default:
#ifdef DEBUG
            noway_assert(!"Unhandled special operator in gtComputeFPlvls()");
#endif
            break;
    }

DONE:

    noway_assert((unsigned char)codeGen->genFPstkLevel == codeGen->genFPstkLevel);

    tree->gtFPlvl = (unsigned char)codeGen->genFPstkLevel;
}

#endif // FEATURE_STACK_FP_X87

/*****************************************************************************
 *
 *  If the given tree is an integer constant that can be used
 *  in a scaled index address mode as a multiplier (e.g. "[4*index]"), then return
 *  the scale factor: 2, 4, or 8. Otherwise, return 0. Note that we never return 1,
 *  to match the behavior of GetScaleIndexShf().
 */

unsigned GenTree::GetScaleIndexMul()
{
    if (IsCnsIntOrI() && jitIsScaleIndexMul(gtIntConCommon.IconValue()) && gtIntConCommon.IconValue() != 1)
    {
        return (unsigned)gtIntConCommon.IconValue();
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
    if (IsCnsIntOrI() && jitIsScaleIndexShift(gtIntConCommon.IconValue()))
    {
        return (unsigned)(1 << gtIntConCommon.IconValue());
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
    // with (!opts.OptEnabled(CLFLG_CONSTANTFOLD) we can have
    //   CNS_INT * CNS_INT
    //
    if (gtOp.gtOp1->IsCnsIntOrI())
    {
        return 0;
    }

    switch (gtOper)
    {
        case GT_MUL:
            return gtOp.gtOp2->GetScaleIndexMul();

        case GT_LSH:
            return gtOp.gtOp2->GetScaleIndexShf();

        default:
            assert(!"GenTree::GetScaledIndex() called with illegal gtOper");
            break;
    }

    return 0;
}

/*****************************************************************************
 *
 *  Returns true if "addr" is a GT_ADD node, at least one of whose arguments is an integer (<= 32 bit)
 *  constant.  If it returns true, it sets "*offset" to (one of the) constant value(s), and
 *  "*addr" to the other argument.
 */

bool GenTree::IsAddWithI32Const(GenTreePtr* addr, int* offset)
{
    if (OperGet() == GT_ADD)
    {
        if (gtOp.gtOp1->IsIntCnsFitsInI32())
        {
            *offset = (int)gtOp.gtOp1->gtIntCon.gtIconVal;
            *addr   = gtOp.gtOp2;
            return true;
        }
        else if (gtOp.gtOp2->IsIntCnsFitsInI32())
        {
            *offset = (int)gtOp.gtOp2->gtIntCon.gtIconVal;
            *addr   = gtOp.gtOp1;
            return true;
        }
    }
    // Otherwise...
    return false;
}

//------------------------------------------------------------------------
// gtGetChildPointer: If 'parent' is the parent of this node, return the pointer
//    to the child node so that it can be modified; otherwise, return nullptr.
//
// Arguments:
//    parent - The possible parent of this node
//
// Return Value:
//    If "child" is a child of "parent", returns a pointer to the child node in the parent
//    (i.e. a pointer to a GenTree pointer).
//    Otherwise, returns nullptr.
//
// Assumptions:
//    'parent' must be non-null
//
// Notes:
//    When FEATURE_MULTIREG_ARGS is defined we can get here with GT_OBJ tree.
//    This happens when we have a struct that is passed in multiple registers.
//
//    Also note that when FEATURE_UNIX_AMD64_STRUCT_PASSING is defined the GT_LDOBJ
//    later gets converted to a GT_FIELD_LIST with two GT_LCL_FLDs in Lower/LowerXArch.
//

GenTreePtr* GenTree::gtGetChildPointer(GenTreePtr parent) const

{
    switch (parent->OperGet())
    {
        default:
            if (!parent->OperIsSimple())
            {
                return nullptr;
            }
            if (this == parent->gtOp.gtOp1)
            {
                return &(parent->gtOp.gtOp1);
            }
            if (this == parent->gtOp.gtOp2)
            {
                return &(parent->gtOp.gtOp2);
            }
            break;

        case GT_CMPXCHG:
            if (this == parent->gtCmpXchg.gtOpLocation)
            {
                return &(parent->gtCmpXchg.gtOpLocation);
            }
            if (this == parent->gtCmpXchg.gtOpValue)
            {
                return &(parent->gtCmpXchg.gtOpValue);
            }
            if (this == parent->gtCmpXchg.gtOpComparand)
            {
                return &(parent->gtCmpXchg.gtOpComparand);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            if (this == parent->gtBoundsChk.gtIndex)
            {
                return &(parent->gtBoundsChk.gtIndex);
            }
            if (this == parent->gtBoundsChk.gtArrLen)
            {
                return &(parent->gtBoundsChk.gtArrLen);
            }
            if (this == parent->gtBoundsChk.gtIndRngFailBB)
            {
                return &(parent->gtBoundsChk.gtIndRngFailBB);
            }
            break;

        case GT_ARR_ELEM:
            if (this == parent->gtArrElem.gtArrObj)
            {
                return &(parent->gtArrElem.gtArrObj);
            }
            for (int i = 0; i < GT_ARR_MAX_RANK; i++)
            {
                if (this == parent->gtArrElem.gtArrInds[i])
                {
                    return &(parent->gtArrElem.gtArrInds[i]);
                }
            }
            break;

        case GT_ARR_OFFSET:
            if (this == parent->gtArrOffs.gtOffset)
            {
                return &(parent->gtArrOffs.gtOffset);
            }
            if (this == parent->gtArrOffs.gtIndex)
            {
                return &(parent->gtArrOffs.gtIndex);
            }
            if (this == parent->gtArrOffs.gtArrObj)
            {
                return &(parent->gtArrOffs.gtArrObj);
            }
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            if (this == parent->gtDynBlk.gtOp1)
            {
                return &(parent->gtDynBlk.gtOp1);
            }
            if (this == parent->gtDynBlk.gtOp2)
            {
                return &(parent->gtDynBlk.gtOp2);
            }
            if (this == parent->gtDynBlk.gtDynamicSize)
            {
                return &(parent->gtDynBlk.gtDynamicSize);
            }
            break;

        case GT_FIELD:
            if (this == parent->AsField()->gtFldObj)
            {
                return &(parent->AsField()->gtFldObj);
            }
            break;

        case GT_RET_EXPR:
            if (this == parent->gtRetExpr.gtInlineCandidate)
            {
                return &(parent->gtRetExpr.gtInlineCandidate);
            }
            break;

        case GT_CALL:
        {
            GenTreeCall* call = parent->AsCall();

            if (this == call->gtCallObjp)
            {
                return &(call->gtCallObjp);
            }
            if (this == call->gtCallArgs)
            {
                return reinterpret_cast<GenTreePtr*>(&(call->gtCallArgs));
            }
            if (this == call->gtCallLateArgs)
            {
                return reinterpret_cast<GenTreePtr*>(&(call->gtCallLateArgs));
            }
            if (this == call->gtControlExpr)
            {
                return &(call->gtControlExpr);
            }
            if (call->gtCallType == CT_INDIRECT)
            {
                if (this == call->gtCallCookie)
                {
                    return &(call->gtCallCookie);
                }
                if (this == call->gtCallAddr)
                {
                    return &(call->gtCallAddr);
                }
            }
        }
        break;

        case GT_STMT:
            noway_assert(!"Illegal node for gtGetChildPointer()");
            unreached();
    }

    return nullptr;
}

bool GenTree::TryGetUse(GenTree* def, GenTree*** use)
{
    assert(def != nullptr);
    assert(use != nullptr);

    switch (OperGet())
    {
        // Leaf nodes
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_VAR_ADDR:
        case GT_LCL_FLD_ADDR:
        case GT_CATCH_ARG:
        case GT_LABEL:
        case GT_FTN_ADDR:
        case GT_RET_EXPR:
        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
        case GT_CNS_STR:
        case GT_MEMORYBARRIER:
        case GT_JMP:
        case GT_JCC:
        case GT_SETCC:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
        case GT_PHI_ARG:
#ifndef LEGACY_BACKEND
        case GT_JMPTABLE:
#endif // LEGACY_BACKEND
        case GT_REG_VAR:
        case GT_CLS_VAR:
        case GT_CLS_VAR_ADDR:
        case GT_ARGPLACE:
        case GT_PHYSREG:
        case GT_EMITNOP:
        case GT_PINVOKE_PROLOG:
        case GT_PINVOKE_EPILOG:
        case GT_IL_OFFSET:
            return false;

        // Standard unary operators
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
        case GT_NOT:
        case GT_NEG:
        case GT_COPY:
        case GT_RELOAD:
        case GT_ARR_LENGTH:
        case GT_CAST:
        case GT_BITCAST:
        case GT_CKFINITE:
        case GT_LCLHEAP:
        case GT_ADDR:
        case GT_IND:
        case GT_OBJ:
        case GT_BLK:
        case GT_BOX:
        case GT_ALLOCOBJ:
        case GT_RUNTIMELOOKUP:
        case GT_INIT_VAL:
        case GT_JTRUE:
        case GT_SWITCH:
        case GT_NULLCHECK:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
        case GT_RETURNTRAP:
        case GT_NOP:
        case GT_RETURN:
        case GT_RETFILT:
            if (def == this->AsUnOp()->gtOp1)
            {
                *use = &this->AsUnOp()->gtOp1;
                return true;
            }
            return false;

        // Variadic nodes
        case GT_PHI:
            assert(this->AsUnOp()->gtOp1 != nullptr);
            return this->AsUnOp()->gtOp1->TryGetUseList(def, use);

        case GT_FIELD_LIST:
            return TryGetUseList(def, use);
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
        case GT_PUTARG_SPLIT:
            if (this->AsUnOp()->gtOp1->gtOper == GT_FIELD_LIST)
            {
                return TryGetUseList(def, use);
            }
            if (def == this->AsUnOp()->gtOp1)
            {
                *use = &this->AsUnOp()->gtOp1;
                return true;
            }
            return false;
#endif // !LEGACY_BACKEND && _TARGET_ARM_

#ifdef FEATURE_SIMD
        case GT_SIMD:
            if (this->AsSIMD()->gtSIMDIntrinsicID == SIMDIntrinsicInitN)
            {
                assert(this->AsSIMD()->gtOp1 != nullptr);
                return this->AsSIMD()->gtOp1->TryGetUseList(def, use);
            }

            return TryGetUseBinOp(def, use);
#endif // FEATURE_SIMD

        // Special nodes
        case GT_CMPXCHG:
        {
            GenTreeCmpXchg* const cmpXchg = this->AsCmpXchg();
            if (def == cmpXchg->gtOpLocation)
            {
                *use = &cmpXchg->gtOpLocation;
                return true;
            }
            if (def == cmpXchg->gtOpValue)
            {
                *use = &cmpXchg->gtOpValue;
                return true;
            }
            if (def == cmpXchg->gtOpComparand)
            {
                *use = &cmpXchg->gtOpComparand;
                return true;
            }
            return false;
        }

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        {
            GenTreeBoundsChk* const boundsChk = this->AsBoundsChk();
            if (def == boundsChk->gtIndex)
            {
                *use = &boundsChk->gtIndex;
                return true;
            }
            if (def == boundsChk->gtArrLen)
            {
                *use = &boundsChk->gtArrLen;
                return true;
            }
            return false;
        }

        case GT_FIELD:
            if (def == this->AsField()->gtFldObj)
            {
                *use = &this->AsField()->gtFldObj;
                return true;
            }
            return false;

        case GT_STMT:
            if (def == this->AsStmt()->gtStmtExpr)
            {
                *use = &this->AsStmt()->gtStmtExpr;
                return true;
            }
            return false;

        case GT_ARR_ELEM:
        {
            GenTreeArrElem* const arrElem = this->AsArrElem();
            if (def == arrElem->gtArrObj)
            {
                *use = &arrElem->gtArrObj;
                return true;
            }
            for (unsigned i = 0; i < arrElem->gtArrRank; i++)
            {
                if (def == arrElem->gtArrInds[i])
                {
                    *use = &arrElem->gtArrInds[i];
                    return true;
                }
            }
            return false;
        }

        case GT_ARR_OFFSET:
        {
            GenTreeArrOffs* const arrOffs = this->AsArrOffs();
            if (def == arrOffs->gtOffset)
            {
                *use = &arrOffs->gtOffset;
                return true;
            }
            if (def == arrOffs->gtIndex)
            {
                *use = &arrOffs->gtIndex;
                return true;
            }
            if (def == arrOffs->gtArrObj)
            {
                *use = &arrOffs->gtArrObj;
                return true;
            }
            return false;
        }

        case GT_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = this->AsDynBlk();
            if (def == dynBlock->gtOp1)
            {
                *use = &dynBlock->gtOp1;
                return true;
            }
            if (def == dynBlock->gtDynamicSize)
            {
                *use = &dynBlock->gtDynamicSize;
                return true;
            }
            return false;
        }

        case GT_STORE_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = this->AsDynBlk();
            if (def == dynBlock->gtOp1)
            {
                *use = &dynBlock->gtOp1;
                return true;
            }
            if (def == dynBlock->gtOp2)
            {
                *use = &dynBlock->gtOp2;
                return true;
            }
            if (def == dynBlock->gtDynamicSize)
            {
                *use = &dynBlock->gtDynamicSize;
                return true;
            }
            return false;
        }

        case GT_CALL:
        {
            GenTreeCall* const call = this->AsCall();
            if (def == call->gtCallObjp)
            {
                *use = &call->gtCallObjp;
                return true;
            }
            if (def == call->gtControlExpr)
            {
                *use = &call->gtControlExpr;
                return true;
            }
            if (call->gtCallType == CT_INDIRECT)
            {
                if (def == call->gtCallCookie)
                {
                    *use = &call->gtCallCookie;
                    return true;
                }
                if (def == call->gtCallAddr)
                {
                    *use = &call->gtCallAddr;
                    return true;
                }
            }
            if ((call->gtCallArgs != nullptr) && call->gtCallArgs->TryGetUseList(def, use))
            {
                return true;
            }

            return (call->gtCallLateArgs != nullptr) && call->gtCallLateArgs->TryGetUseList(def, use);
        }

        // Binary nodes
        default:
            assert(this->OperIsBinary());
            return TryGetUseBinOp(def, use);
    }
}

bool GenTree::TryGetUseList(GenTree* def, GenTree*** use)
{
    assert(def != nullptr);
    assert(use != nullptr);

    for (GenTreeArgList* node = this->AsArgList(); node != nullptr; node = node->Rest())
    {
        if (def == node->gtOp1)
        {
            *use = &node->gtOp1;
            return true;
        }
    }
    return false;
}

bool GenTree::TryGetUseBinOp(GenTree* def, GenTree*** use)
{
    assert(def != nullptr);
    assert(use != nullptr);
    assert(this->OperIsBinary());

    GenTreeOp* const binOp = this->AsOp();
    if (def == binOp->gtOp1)
    {
        *use = &binOp->gtOp1;
        return true;
    }
    if (def == binOp->gtOp2)
    {
        *use = &binOp->gtOp2;
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// GenTree::ReplaceOperand:
//    Replace a given operand to this node with a new operand. If the
//    current node is a call node, this will also udpate the call
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

    if (OperGet() == GT_CALL)
    {
        AsCall()->ReplaceCallOperand(useEdge, replacement);
    }
    else
    {
        *useEdge = replacement;
    }
}

//------------------------------------------------------------------------
// gtGetParent: Get the parent of this node, and optionally capture the
//    pointer to the child so that it can be modified.
//
// Arguments:

//    parentChildPointer - A pointer to a GenTreePtr* (yes, that's three
//                         levels, i.e. GenTree ***), which if non-null,
//                         will be set to point to the field in the parent
//                         that points to this node.
//
//    Return value       - The parent of this node.
//
//    Notes:
//
//    This requires that the execution order must be defined (i.e. gtSetEvalOrder() has been called).
//    To enable the child to be replaced, it accepts an argument, parentChildPointer that, if non-null,
//    will be set to point to the child pointer in the parent that points to this node.

GenTreePtr GenTree::gtGetParent(GenTreePtr** parentChildPtrPtr) const
{
    // Find the parent node; it must be after this node in the execution order.
    GenTreePtr* parentChildPtr = nullptr;
    GenTreePtr  parent;
    for (parent = gtNext; parent != nullptr; parent = parent->gtNext)
    {
        parentChildPtr = gtGetChildPointer(parent);
        if (parentChildPtr != nullptr)
        {
            break;
        }
    }
    if (parentChildPtrPtr != nullptr)
    {
        *parentChildPtrPtr = parentChildPtr;
    }
    return parent;
}

//------------------------------------------------------------------------------
// OperMayThrow : Check whether the operation requires GTF_ASG flag regardless
//                of the children's flags.
//

bool GenTree::OperRequiresAsgFlag()
{
    return (OperIsAssignment() || (gtOper == GT_XADD) || (gtOper == GT_XCHG) || (gtOper == GT_LOCKADD) ||
            (gtOper == GT_CMPXCHG) || (gtOper == GT_MEMORYBARRIER));
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

bool GenTree::OperMayThrow(Compiler* comp)
{
    GenTreePtr op;

    switch (gtOper)
    {
        case GT_MOD:
        case GT_DIV:
        case GT_UMOD:
        case GT_UDIV:

            /* Division with a non-zero, non-minus-one constant does not throw an exception */

            op = gtOp.gtOp2;

            if (varTypeIsFloating(op->TypeGet()))
            {
                return false; // Floating point division does not throw.
            }

            // For integers only division by 0 or by -1 can throw
            if (op->IsIntegralConst() && !op->IsIntegralConst(0) && !op->IsIntegralConst(-1))
            {
                return false;
            }
            return true;

        case GT_INTRINSIC:
            // If this is an intrinsic that represents the object.GetType(), it can throw an NullReferenceException.
            // Report it as may throw.
            // Note: Some of the rest of the existing intrinsics could potentially throw an exception (for example
            //       the array and string element access ones). They are handled differently than the GetType intrinsic
            //       and are not marked with GTF_EXCEPT. If these are revisited at some point to be marked as
            //       GTF_EXCEPT,
            //       the code below might need to be specialized to handle them properly.
            if ((this->gtFlags & GTF_EXCEPT) != 0)
            {
                return true;
            }

            break;

        case GT_CALL:

            CorInfoHelpFunc helper;
            helper = comp->eeGetHelperNum(this->AsCall()->gtCallMethHnd);
            return ((helper == CORINFO_HELP_UNDEF) || !comp->s_helperCallProperties.NoThrow(helper));

        case GT_IND:
        case GT_BLK:
        case GT_OBJ:
        case GT_DYN_BLK:
        case GT_STORE_BLK:
        case GT_NULLCHECK:
            return (((this->gtFlags & GTF_IND_NONFAULTING) == 0) && comp->fgAddrCouldBeNull(this->AsIndir()->Addr()));

        case GT_ARR_LENGTH:
            return (((this->gtFlags & GTF_IND_NONFAULTING) == 0) && comp->fgAddrCouldBeNull(gtOp.gtOp1));

        case GT_ARR_BOUNDS_CHECK:
        case GT_ARR_ELEM:
        case GT_ARR_INDEX:
        case GT_ARR_OFFSET:
        case GT_LCLHEAP:
        case GT_CKFINITE:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
        case GT_INDEX_ADDR:
            return true;
        default:
            break;
    }

    /* Overflow arithmetic operations also throw exceptions */

    if (gtOverflowEx())
    {
        return true;
    }

    return false;
}

#if DEBUGGABLE_GENTREE
// static
GenTree::VtablePtr GenTree::s_vtablesForOpers[] = {nullptr};
GenTree::VtablePtr GenTree::s_vtableForOp       = nullptr;

GenTree::VtablePtr GenTree::GetVtableForOper(genTreeOps oper)
{
    noway_assert(oper < GT_COUNT);

    if (s_vtablesForOpers[oper] != nullptr)
    {
        return s_vtablesForOpers[oper];
    }
    // Otherwise...
    VtablePtr res = nullptr;
    switch (oper)
    {
#define GTSTRUCT_0(nm, tag) /*handle explicitly*/
#define GTSTRUCT_1(nm, tag)                                                                                            \
    case tag:                                                                                                          \
    {                                                                                                                  \
        GenTree##nm gt;                                                                                                \
        res = *reinterpret_cast<VtablePtr*>(&gt);                                                                      \
    }                                                                                                                  \
    break;
#define GTSTRUCT_2(nm, tag, tag2)             /*handle explicitly*/
#define GTSTRUCT_3(nm, tag, tag2, tag3)       /*handle explicitly*/
#define GTSTRUCT_4(nm, tag, tag2, tag3, tag4) /*handle explicitly*/
#define GTSTRUCT_N(nm, ...)                   /*handle explicitly*/
#include "gtstructs.h"

#if !FEATURE_EH_FUNCLETS
        // If FEATURE_EH_FUNCLETS is set, then GT_JMP becomes the only member of Val, and will be handled above.
        case GT_END_LFIN:
        case GT_JMP:
        {
            GenTreeVal gt(GT_JMP, TYP_INT, 0);
            res = *reinterpret_cast<VtablePtr*>(&gt);
            break;
        }
#endif
        case GT_OBJ:
        {
            GenTreeIntCon dummyOp(TYP_I_IMPL, 0);
            GenTreeObj    obj(TYP_STRUCT, &dummyOp, NO_CLASS_HANDLE, 0);
            res = *reinterpret_cast<VtablePtr*>(&obj);
        }
        break;

        default:
        {
            // Should be unary or binary op.
            if (s_vtableForOp == nullptr)
            {
                unsigned opKind = OperKind(oper);
                assert(!IsExOp(opKind));
                assert(OperIsSimple(oper) || OperIsLeaf(oper));
                // Need to provide non-null operands.
                Compiler*     comp = (Compiler*)_alloca(sizeof(Compiler));
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

GenTreePtr Compiler::gtNewOperNode(genTreeOps oper, var_types type, GenTreePtr op1, GenTreePtr op2)
{
    assert(op1 != nullptr);
    assert(op2 != nullptr);

    // We should not be allocating nodes that extend GenTreeOp with this;
    // should call the appropriate constructor for the extended type.
    assert(!GenTree::IsExOp(GenTree::OperKind(oper)));

    GenTreePtr node = new (this, oper) GenTreeOp(oper, type, op1, op2);

    return node;
}

GenTreePtr Compiler::gtNewQmarkNode(var_types type, GenTreePtr cond, GenTreePtr colon)
{
    compQmarkUsed   = true;
    GenTree* result = new (this, GT_QMARK) GenTreeQmark(type, cond, colon, this);
#ifdef DEBUG
    if (compQmarkRationalized)
    {
        fgCheckQmarkAllowedForm(result);
    }
#endif
    return result;
}

GenTreeQmark::GenTreeQmark(var_types type, GenTreePtr cond, GenTreePtr colonOp, Compiler* comp)
    : GenTreeOp(GT_QMARK, type, cond, colonOp)
#ifdef LEGACY_BACKEND
    , gtThenLiveSet(VarSetOps::UninitVal())
    , gtElseLiveSet(VarSetOps::UninitVal())
#endif
{
    // These must follow a specific form.
    assert(cond != nullptr && cond->TypeGet() == TYP_INT);
    assert(colonOp != nullptr && colonOp->OperGet() == GT_COLON);

#ifdef LEGACY_BACKEND
    comp->impInlineRoot()->compQMarks->Push(this);
#endif
}

GenTreeIntCon* Compiler::gtNewIconNode(ssize_t value, var_types type)
{
    return new (this, GT_CNS_INT) GenTreeIntCon(type, value);
}

// return a new node representing the value in a physical register
GenTree* Compiler::gtNewPhysRegNode(regNumber reg, var_types type)
{
    assert(genIsValidIntReg(reg) || (reg == REG_SPBASE));
    GenTree* result = new (this, GT_PHYSREG) GenTreePhysReg(reg, type);
    return result;
}

#ifndef LEGACY_BACKEND
GenTreePtr Compiler::gtNewJmpTableNode()
{
    GenTreePtr node                   = new (this, GT_JMPTABLE) GenTreeJumpTable(TYP_INT);
    node->gtJumpTable.gtJumpTableAddr = 0;
    return node;
}
#endif // !LEGACY_BACKEND

/*****************************************************************************
 *
 *  Converts an annotated token into an icon flags (so that we will later be
 *  able to tell the type of the handle that will be embedded in the icon
 *  node)
 */

unsigned Compiler::gtTokenToIconFlags(unsigned token)
{
    unsigned flags = 0;

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

/*****************************************************************************
 *
 *  Allocates a integer constant entry that represents a HANDLE to something.
 *  It may not be allowed to embed HANDLEs directly into the JITed code (for eg,
 *  as arguments to JIT helpers). Get a corresponding value that can be embedded.
 *  If the handle needs to be accessed via an indirection, pValue points to it.
 */

GenTreePtr Compiler::gtNewIconEmbHndNode(void* value, void* pValue, unsigned flags, void* compileTimeHandle)
{
    GenTreePtr node;

    assert((!value) != (!pValue));

    if (value)
    {
        node = gtNewIconHandleNode((size_t)value, flags, /*fieldSeq*/ FieldSeqStore::NotAField());
        node->gtIntCon.gtCompileTimeHandle = (size_t)compileTimeHandle;
    }
    else
    {
        node = gtNewIconHandleNode((size_t)pValue, flags, /*fieldSeq*/ FieldSeqStore::NotAField());
        node->gtIntCon.gtCompileTimeHandle = (size_t)compileTimeHandle;
        node                               = gtNewOperNode(GT_IND, TYP_I_IMPL, node);

        // This indirection won't cause an exception.  It should also
        // be invariant, but marking it as such leads to bad diffs.
        node->gtFlags |= GTF_IND_NONFAULTING;
    }

    return node;
}

/*****************************************************************************/
GenTreePtr Compiler::gtNewStringLiteralNode(InfoAccessType iat, void* pValue)
{
    GenTreePtr tree = nullptr;

    switch (iat)
    {
        case IAT_VALUE: // The info value is directly available
            tree         = gtNewIconEmbHndNode(pValue, nullptr, GTF_ICON_STR_HDL);
            tree->gtType = TYP_REF;
            tree         = gtNewOperNode(GT_NOP, TYP_REF, tree); // prevents constant folding
            break;

        case IAT_PVALUE: // The value needs to be accessed via an       indirection
            tree = gtNewIconHandleNode((size_t)pValue, GTF_ICON_STR_HDL);
            // An indirection of a string handle can't cause an exception so don't set GTF_EXCEPT
            tree = gtNewOperNode(GT_IND, TYP_REF, tree);
            tree->gtFlags |= GTF_GLOB_REF;
            break;

        case IAT_PPVALUE: // The value needs to be accessed via a double indirection
            tree = gtNewIconHandleNode((size_t)pValue, GTF_ICON_PSTR_HDL);
            tree = gtNewOperNode(GT_IND, TYP_I_IMPL, tree);
            tree->gtFlags |= GTF_IND_INVARIANT;
            // An indirection of a string handle can't cause an exception so don't set GTF_EXCEPT
            tree = gtNewOperNode(GT_IND, TYP_REF, tree);
            tree->gtFlags |= GTF_GLOB_REF;
            break;

        default:
            assert(!"Unexpected InfoAccessType");
    }

    return tree;
}

/*****************************************************************************/

GenTreePtr Compiler::gtNewLconNode(__int64 value)
{
#ifdef _TARGET_64BIT_
    GenTreePtr node = new (this, GT_CNS_INT) GenTreeIntCon(TYP_LONG, value);
#else
    GenTreePtr node = new (this, GT_CNS_LNG) GenTreeLngCon(value);
#endif

    return node;
}

GenTreePtr Compiler::gtNewDconNode(double value)
{
    GenTreePtr node = new (this, GT_CNS_DBL) GenTreeDblCon(value);

    return node;
}

GenTreePtr Compiler::gtNewSconNode(int CPX, CORINFO_MODULE_HANDLE scpHandle)
{

#if SMALL_TREE_NODES

    /* 'GT_CNS_STR' nodes later get transformed into 'GT_CALL' */

    assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_CNS_STR]);

    GenTreePtr node = new (this, GT_CALL) GenTreeStrCon(CPX, scpHandle DEBUGARG(/*largeNode*/ true));
#else
    GenTreePtr node = new (this, GT_CNS_STR) GenTreeStrCon(CPX, scpHandle DEBUGARG(/*largeNode*/ true));
#endif

    return node;
}

GenTreePtr Compiler::gtNewZeroConNode(var_types type)
{
    GenTreePtr zero;
    switch (type)
    {
        case TYP_INT:
            zero = gtNewIconNode(0);
            break;

        case TYP_BYREF:
            __fallthrough;

        case TYP_REF:
            zero         = gtNewIconNode(0);
            zero->gtType = type;
            break;

        case TYP_LONG:
            zero = gtNewLconNode(0);
            break;

        case TYP_FLOAT:
            zero         = gtNewDconNode(0.0);
            zero->gtType = type;
            break;

        case TYP_DOUBLE:
            zero = gtNewDconNode(0.0);
            break;

        default:
            assert(!"Bad type");
            zero = nullptr;
            break;
    }
    return zero;
}

GenTreePtr Compiler::gtNewOneConNode(var_types type)
{
    switch (type)
    {
        case TYP_INT:
        case TYP_UINT:
            return gtNewIconNode(1);

        case TYP_LONG:
        case TYP_ULONG:
            return gtNewLconNode(1);

        case TYP_FLOAT:
        {
            GenTreePtr one = gtNewDconNode(1.0);
            one->gtType    = type;
            return one;
        }

        case TYP_DOUBLE:
            return gtNewDconNode(1.0);

        default:
            assert(!"Bad type");
            return nullptr;
    }
}

#ifdef FEATURE_SIMD
//---------------------------------------------------------------------
// gtNewSIMDVectorZero: create a GT_SIMD node for Vector<T>.Zero
//
// Arguments:
//    simdType  -  simd vector type
//    baseType  -  element type of vector
//    size      -  size of vector in bytes
GenTreePtr Compiler::gtNewSIMDVectorZero(var_types simdType, var_types baseType, unsigned size)
{
    baseType         = genActualType(baseType);
    GenTree* initVal = gtNewZeroConNode(baseType);
    initVal->gtType  = baseType;
    return gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, baseType, size);
}

//---------------------------------------------------------------------
// gtNewSIMDVectorOne: create a GT_SIMD node for Vector<T>.One
//
// Arguments:
//    simdType  -  simd vector type
//    baseType  -  element type of vector
//    size      -  size of vector in bytes
GenTreePtr Compiler::gtNewSIMDVectorOne(var_types simdType, var_types baseType, unsigned size)
{
    GenTree* initVal;
    if (varTypeIsSmallInt(baseType))
    {
        unsigned baseSize = genTypeSize(baseType);
        int      val;
        if (baseSize == 1)
        {
            val = 0x01010101;
        }
        else
        {
            val = 0x00010001;
        }
        initVal = gtNewIconNode(val);
    }
    else
    {
        initVal = gtNewOneConNode(baseType);
    }

    baseType        = genActualType(baseType);
    initVal->gtType = baseType;
    return gtNewSIMDNode(simdType, initVal, nullptr, SIMDIntrinsicInit, baseType, size);
}
#endif // FEATURE_SIMD

GenTreeCall* Compiler::gtNewIndCallNode(GenTreePtr addr, var_types type, GenTreeArgList* args, IL_OFFSETX ilOffset)
{
    return gtNewCallNode(CT_INDIRECT, (CORINFO_METHOD_HANDLE)addr, type, args, ilOffset);
}

GenTreeCall* Compiler::gtNewCallNode(
    gtCallTypes callType, CORINFO_METHOD_HANDLE callHnd, var_types type, GenTreeArgList* args, IL_OFFSETX ilOffset)
{
    GenTreeCall* node = new (this, GT_CALL) GenTreeCall(genActualType(type));

    node->gtFlags |= (GTF_CALL | GTF_GLOB_REF);
    if (args)
    {
        node->gtFlags |= (args->gtFlags & GTF_ALL_EFFECT);
    }
    node->gtCallType      = callType;
    node->gtCallMethHnd   = callHnd;
    node->gtCallArgs      = args;
    node->gtCallObjp      = nullptr;
    node->fgArgInfo       = nullptr;
    node->callSig         = nullptr;
    node->gtRetClsHnd     = nullptr;
    node->gtControlExpr   = nullptr;
    node->gtCallMoreFlags = 0;

    if (callType == CT_INDIRECT)
    {
        node->gtCallCookie = nullptr;
    }
    else
    {
        node->gtInlineCandidateInfo = nullptr;
    }
    node->gtCallLateArgs = nullptr;
    node->gtReturnType   = type;

#ifdef LEGACY_BACKEND
    node->gtCallRegUsedMask = RBM_NONE;
#endif // LEGACY_BACKEND

#ifdef FEATURE_READYTORUN_COMPILER
    node->gtEntryPoint.addr = nullptr;
#endif

#if defined(DEBUG) || defined(INLINE_DATA)
    // These get updated after call node is built.
    node->gtInlineObservation = InlineObservation::CALLEE_UNUSED_INITIAL;
    node->gtRawILOffset       = BAD_IL_OFFSET;
#endif

    // Spec: Managed Retval sequence points needs to be generated while generating debug info for debuggable code.
    //
    // Implementation note: if not generating MRV info genCallSite2ILOffsetMap will be NULL and
    // codegen will pass BAD_IL_OFFSET as IL offset of a call node to emitter, which will cause emitter
    // not to emit IP mapping entry.
    if (opts.compDbgCode && opts.compDbgInfo)
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
        if (genCallSite2ILOffsetMap == nullptr)
        {
            genCallSite2ILOffsetMap = new (getAllocator()) CallSiteILOffsetTable(getAllocator());
        }

        // Make sure that there are no duplicate entries for a given call node
        IL_OFFSETX value;
        assert(!genCallSite2ILOffsetMap->Lookup(node, &value));
        genCallSite2ILOffsetMap->Set(node, ilOffset);
    }

    // Initialize gtOtherRegs
    node->ClearOtherRegs();

    // Initialize spill flags of gtOtherRegs
    node->ClearOtherRegFlags();

#if (defined(_TARGET_X86_) || defined(_TARGET_ARM_)) && !defined(LEGACY_BACKEND)
    // Initialize the multi-reg long return info if necessary
    if (varTypeIsLong(node))
    {
        // The return type will remain as the incoming long type
        node->gtReturnType = node->gtType;

        // Initialize Return type descriptor of call node
        ReturnTypeDesc* retTypeDesc = node->GetReturnTypeDesc();
        retTypeDesc->InitializeLongReturnType(this);

        // must be a long returned in two registers
        assert(retTypeDesc->GetReturnRegCount() == 2);
    }
#endif // (defined(_TARGET_X86_) || defined(_TARGET_ARM_)) && !defined(_LEGACY_BACKEND_)

    return node;
}

GenTreePtr Compiler::gtNewLclvNode(unsigned lnum, var_types type, IL_OFFSETX ILoffs)
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
    GenTreePtr node = new (this, GT_LCL_VAR) GenTreeLclVar(type, lnum, ILoffs);

    /* Cannot have this assert because the inliner uses this function
     * to add temporaries */

    // assert(lnum < lvaCount);

    return node;
}

GenTreePtr Compiler::gtNewLclLNode(unsigned lnum, var_types type, IL_OFFSETX ILoffs)
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
#if SMALL_TREE_NODES
    /* This local variable node may later get transformed into a large node */

    // assert(GenTree::s_gtNodeSizes[GT_CALL] > GenTree::s_gtNodeSizes[GT_LCL_VAR]);

    GenTreePtr node = new (this, GT_CALL) GenTreeLclVar(type, lnum, ILoffs DEBUGARG(/*largeNode*/ true));
#else
    GenTreePtr node = new (this, GT_LCL_VAR) GenTreeLclVar(type, lnum, ILoffs DEBUGARG(/*largeNode*/ true));
#endif

    return node;
}

GenTreeLclFld* Compiler::gtNewLclFldNode(unsigned lnum, var_types type, unsigned offset)
{
    GenTreeLclFld* node = new (this, GT_LCL_FLD) GenTreeLclFld(type, lnum, offset);

    /* Cannot have this assert because the inliner uses this function
     * to add temporaries */

    // assert(lnum < lvaCount);

    node->gtFieldSeq = FieldSeqStore::NotAField();
    return node;
}

GenTreePtr Compiler::gtNewInlineCandidateReturnExpr(GenTreePtr inlineCandidate, var_types type)

{
    assert(GenTree::s_gtNodeSizes[GT_RET_EXPR] == TREE_NODE_SZ_LARGE);

    GenTreePtr node = new (this, GT_RET_EXPR) GenTreeRetExpr(type);

    node->gtRetExpr.gtInlineCandidate = inlineCandidate;

    if (varTypeIsStruct(inlineCandidate) && !inlineCandidate->OperIsBlkOp())
    {
        node->gtRetExpr.gtRetClsHnd = gtGetStructHandle(inlineCandidate);
    }

    // GT_RET_EXPR node eventually might be bashed back to GT_CALL (when inlining is aborted for example).
    // Therefore it should carry the GTF_CALL flag so that all the rules about spilling can apply to it as well.
    // For example, impImportLeave or CEE_POP need to spill GT_RET_EXPR before empty the evaluation stack.
    node->gtFlags |= GTF_CALL;

    return node;
}

GenTreeArgList* Compiler::gtNewListNode(GenTreePtr op1, GenTreeArgList* op2)
{
    assert((op1 != nullptr) && (op1->OperGet() != GT_LIST));

    return new (this, GT_LIST) GenTreeArgList(op1, op2);
}

/*****************************************************************************
 *
 *  Create a list out of one value.
 */

GenTreeArgList* Compiler::gtNewArgList(GenTreePtr arg)
{
    return new (this, GT_LIST) GenTreeArgList(arg);
}

/*****************************************************************************
 *
 *  Create a list out of the two values.
 */

GenTreeArgList* Compiler::gtNewArgList(GenTreePtr arg1, GenTreePtr arg2)
{
    return new (this, GT_LIST) GenTreeArgList(arg1, gtNewArgList(arg2));
}

/*****************************************************************************
 *
 *  Create a list out of the three values.
 */

GenTreeArgList* Compiler::gtNewArgList(GenTreePtr arg1, GenTreePtr arg2, GenTreePtr arg3)
{
    return new (this, GT_LIST) GenTreeArgList(arg1, gtNewArgList(arg2, arg3));
}

/*****************************************************************************
 *
 *  Given a GT_CALL node, access the fgArgInfo and find the entry
 *  that has the matching argNum and return the fgArgTableEntryPtr
 */

fgArgTabEntryPtr Compiler::gtArgEntryByArgNum(GenTreeCall* call, unsigned argNum)
{
    fgArgInfoPtr argInfo = call->fgArgInfo;
    noway_assert(argInfo != nullptr);

    unsigned          argCount       = argInfo->ArgCount();
    fgArgTabEntryPtr* argTable       = argInfo->ArgTable();
    fgArgTabEntryPtr  curArgTabEntry = nullptr;

    for (unsigned i = 0; i < argCount; i++)
    {
        curArgTabEntry = argTable[i];
        if (curArgTabEntry->argNum == argNum)
        {
            return curArgTabEntry;
        }
    }
    noway_assert(!"gtArgEntryByArgNum: argNum not found");
    return nullptr;
}

/*****************************************************************************
 *
 *  Given a GT_CALL node, access the fgArgInfo and find the entry
 *  that has the matching node and return the fgArgTableEntryPtr
 */

fgArgTabEntryPtr Compiler::gtArgEntryByNode(GenTreeCall* call, GenTreePtr node)
{
    fgArgInfoPtr argInfo = call->fgArgInfo;
    noway_assert(argInfo != nullptr);

    unsigned          argCount       = argInfo->ArgCount();
    fgArgTabEntryPtr* argTable       = argInfo->ArgTable();
    fgArgTabEntryPtr  curArgTabEntry = nullptr;

    for (unsigned i = 0; i < argCount; i++)
    {
        curArgTabEntry = argTable[i];

        if (curArgTabEntry->node == node)
        {
            return curArgTabEntry;
        }
        else if (curArgTabEntry->parent != nullptr)
        {
            assert(curArgTabEntry->parent->OperIsList());
            if (curArgTabEntry->parent->Current() == node)
            {
                return curArgTabEntry;
            }
        }
        else // (curArgTabEntry->parent == NULL)
        {
            if (call->gtCallObjp == node)
            {
                return curArgTabEntry;
            }
        }
    }
    noway_assert(!"gtArgEntryByNode: node not found");
    return nullptr;
}

/*****************************************************************************
 *
 *  Find and return the entry with the given "lateArgInx".  Requires that one is found
 *  (asserts this).
 */
fgArgTabEntryPtr Compiler::gtArgEntryByLateArgIndex(GenTreeCall* call, unsigned lateArgInx)
{
    fgArgInfoPtr argInfo = call->fgArgInfo;
    noway_assert(argInfo != nullptr);

    unsigned          argCount       = argInfo->ArgCount();
    fgArgTabEntryPtr* argTable       = argInfo->ArgTable();
    fgArgTabEntryPtr  curArgTabEntry = nullptr;

    for (unsigned i = 0; i < argCount; i++)
    {
        curArgTabEntry = argTable[i];
        if (curArgTabEntry->lateArgInx == lateArgInx)
        {
            return curArgTabEntry;
        }
    }
    noway_assert(!"gtArgEntryByNode: node not found");
    return nullptr;
}

/*****************************************************************************
 *
 *  Given an fgArgTabEntryPtr, return true if it is the 'this' pointer argument.
 */
bool Compiler::gtArgIsThisPtr(fgArgTabEntryPtr argEntry)
{
    return (argEntry->parent == nullptr);
}

/*****************************************************************************
 *
 *  Create a node that will assign 'src' to 'dst'.
 */

GenTreePtr Compiler::gtNewAssignNode(GenTreePtr dst, GenTreePtr src)
{
    /* Mark the target as being assigned */

    if ((dst->gtOper == GT_LCL_VAR) || (dst->OperGet() == GT_LCL_FLD))
    {
        dst->gtFlags |= GTF_VAR_DEF;
        if (dst->IsPartialLclFld(this))
        {
            // We treat these partial writes as combined uses and defs.
            dst->gtFlags |= GTF_VAR_USEASG;
        }
    }
    dst->gtFlags |= GTF_DONT_CSE;

    /* Create the assignment node */

    GenTreePtr asg = gtNewOperNode(GT_ASG, dst->TypeGet(), dst, src);

    /* Mark the expression as containing an assignment */

    asg->gtFlags |= GTF_ASG;

    return asg;
}

//------------------------------------------------------------------------
// gtNewObjNode: Creates a new Obj node.
//
// Arguments:
//    structHnd - The class handle of the struct type.
//    addr      - The address of the struct.
//
// Return Value:
//    Returns a node representing the struct value at the given address.
//
// Assumptions:
//    Any entry and exit conditions, such as required preconditions of
//    data structures, memory to be freed by caller, etc.
//
// Notes:
//    It will currently return a GT_OBJ node for any struct type, but may
//    return a GT_IND or a non-indirection for a scalar type.
//    The node will not yet have its GC info initialized. This is because
//    we may not need this info if this is an r-value.

GenTree* Compiler::gtNewObjNode(CORINFO_CLASS_HANDLE structHnd, GenTree* addr)
{
    var_types nodeType = impNormStructType(structHnd);
    assert(varTypeIsStruct(nodeType));
    unsigned size = info.compCompHnd->getClassSize(structHnd);

    // It would be convenient to set the GC info at this time, but we don't actually require
    // it unless this is going to be a destination.
    if (!varTypeIsStruct(nodeType))
    {
        if ((addr->gtOper == GT_ADDR) && (addr->gtGetOp1()->TypeGet() == nodeType))
        {
            return addr->gtGetOp1();
        }
        else
        {
            return gtNewOperNode(GT_IND, nodeType, addr);
        }
    }
    GenTreeBlk* newBlkOrObjNode = new (this, GT_OBJ) GenTreeObj(nodeType, addr, structHnd, size);

    // An Obj is not a global reference, if it is known to be a local struct.
    if ((addr->gtFlags & GTF_GLOB_REF) == 0)
    {
        GenTreeLclVarCommon* lclNode = addr->IsLocalAddrExpr();
        if (lclNode != nullptr)
        {
            newBlkOrObjNode->gtFlags |= GTF_IND_NONFAULTING;
            if (!lvaIsImplicitByRefLocal(lclNode->gtLclNum))
            {
                newBlkOrObjNode->gtFlags &= ~GTF_GLOB_REF;
            }
        }
    }
    return newBlkOrObjNode;
}

//------------------------------------------------------------------------
// gtSetObjGcInfo: Set the GC info on an object node
//
// Arguments:
//    objNode - The object node of interest

void Compiler::gtSetObjGcInfo(GenTreeObj* objNode)
{
    CORINFO_CLASS_HANDLE structHnd  = objNode->gtClass;
    var_types            nodeType   = objNode->TypeGet();
    unsigned             size       = objNode->gtBlkSize;
    unsigned             slots      = 0;
    unsigned             gcPtrCount = 0;
    BYTE*                gcPtrs     = nullptr;

    assert(varTypeIsStruct(nodeType));
    assert(size == info.compCompHnd->getClassSize(structHnd));
    assert(nodeType == impNormStructType(structHnd));

    if (nodeType == TYP_STRUCT)
    {
        if (size >= TARGET_POINTER_SIZE)
        {
            // Get the GC fields info
            var_types simdBaseType; // Dummy argument
            slots    = (unsigned)(roundUp(size, TARGET_POINTER_SIZE) / TARGET_POINTER_SIZE);
            gcPtrs   = new (this, CMK_ASTNode) BYTE[slots];
            nodeType = impNormStructType(structHnd, gcPtrs, &gcPtrCount, &simdBaseType);
        }
    }
    objNode->SetGCInfo(gcPtrs, gcPtrCount, slots);
    assert(objNode->gtType == nodeType);
}

//------------------------------------------------------------------------
// gtNewStructVal: Return a node that represents a struct value
//
// Arguments:
//    structHnd - The class for the struct
//    addr      - The address of the struct
//
// Return Value:
//    A block, object or local node that represents the struct value pointed to by 'addr'.

GenTree* Compiler::gtNewStructVal(CORINFO_CLASS_HANDLE structHnd, GenTreePtr addr)
{
    if (addr->gtOper == GT_ADDR)
    {
        GenTree* val = addr->gtGetOp1();
        if (val->OperGet() == GT_LCL_VAR)
        {
            unsigned   lclNum = addr->gtGetOp1()->AsLclVarCommon()->gtLclNum;
            LclVarDsc* varDsc = &(lvaTable[lclNum]);
            if (varTypeIsStruct(varDsc) && (varDsc->lvVerTypeInfo.GetClassHandle() == structHnd) &&
                !lvaIsImplicitByRefLocal(lclNum))
            {
                return addr->gtGetOp1();
            }
        }
    }
    return gtNewObjNode(structHnd, addr);
}

//------------------------------------------------------------------------
// gtNewBlockVal: Return a node that represents a possibly untyped block value
//
// Arguments:
//    addr      - The address of the block
//    size      - The size of the block
//
// Return Value:
//    A block, object or local node that represents the block value pointed to by 'addr'.

GenTree* Compiler::gtNewBlockVal(GenTreePtr addr, unsigned size)
{
    // By default we treat this as an opaque struct type with known size.
    var_types blkType = TYP_STRUCT;
    if ((addr->gtOper == GT_ADDR) && (addr->gtGetOp1()->OperGet() == GT_LCL_VAR))
    {
        GenTree* val = addr->gtGetOp1();
#if FEATURE_SIMD
        if (varTypeIsSIMD(val))
        {
            if (genTypeSize(val->TypeGet()) == size)
            {
                blkType = val->TypeGet();
                return addr->gtGetOp1();
            }
        }
        else
#endif // FEATURE_SIMD
#ifndef LEGACY_BACKEND
            if (val->TypeGet() == TYP_STRUCT)
        {
            GenTreeLclVarCommon* lcl    = addr->gtGetOp1()->AsLclVarCommon();
            LclVarDsc*           varDsc = &(lvaTable[lcl->gtLclNum]);
            if ((varDsc->TypeGet() == TYP_STRUCT) && (varDsc->lvExactSize == size))
            {
                return addr->gtGetOp1();
            }
        }
#endif // !LEGACY_BACKEND
    }
    return new (this, GT_BLK) GenTreeBlk(GT_BLK, blkType, addr, size);
}

// Creates a new assignment node for a CpObj.
// Parameters (exactly the same as MSIL CpObj):
//
//  dstAddr    - The target to copy the struct to
//  srcAddr    - The source to copy the struct from
//  structHnd  - A class token that represents the type of object being copied. May be null
//               if FEATURE_SIMD is enabled and the source has a SIMD type.
//  isVolatile - Is this marked as volatile memory?

GenTree* Compiler::gtNewCpObjNode(GenTreePtr           dstAddr,
                                  GenTreePtr           srcAddr,
                                  CORINFO_CLASS_HANDLE structHnd,
                                  bool                 isVolatile)
{
    GenTreePtr lhs = gtNewStructVal(structHnd, dstAddr);
    GenTree*   src = nullptr;
    unsigned   size;

    if (lhs->OperIsBlk())
    {
        size = lhs->AsBlk()->gtBlkSize;
        if (lhs->OperGet() == GT_OBJ)
        {
            gtSetObjGcInfo(lhs->AsObj());
        }
    }
    else
    {
        size = genTypeSize(lhs->gtType);
    }

    if (srcAddr->OperGet() == GT_ADDR)
    {
        src = srcAddr->gtOp.gtOp1;
    }
    else
    {
        src = gtNewOperNode(GT_IND, lhs->TypeGet(), srcAddr);
    }

    GenTree* result = gtNewBlkOpNode(lhs, src, size, isVolatile, true);
    return result;
}

//------------------------------------------------------------------------
// FixupInitBlkValue: Fixup the init value for an initBlk operation
//
// Arguments:
//    asgType - The type of assignment that the initBlk is being transformed into
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

void GenTreeIntCon::FixupInitBlkValue(var_types asgType)
{
    assert(varTypeIsIntegralOrI(asgType));
    unsigned size = genTypeSize(asgType);
    if (size > 1)
    {
        size_t cns = gtIconVal;
        cns        = cns & 0xFF;
        cns |= cns << 8;
        if (size >= 4)
        {
            cns |= cns << 16;
#ifdef _TARGET_64BIT_
            if (size == 8)
            {
                cns |= cns << 32;
            }
#endif // _TARGET_64BIT_

            // Make the type match for evaluation types.
            gtType = asgType;

            // if we are initializing a GC type the value being assigned must be zero (null).
            assert(!varTypeIsGC(asgType) || (cns == 0));
        }

        gtIconVal = cns;
    }
}

//
//------------------------------------------------------------------------
// gtBlockOpInit: Initializes a BlkOp GenTree
//
// Arguments:
//    result     - an assignment node that is to be initialized.
//    dst        - the target (destination) we want to either initialize or copy to.
//    src        - the init value for InitBlk or the source struct for CpBlk/CpObj.
//    isVolatile - specifies whether this node is a volatile memory operation.
//
// Assumptions:
//    'result' is an assignment that is newly constructed.
//    If 'dst' is TYP_STRUCT, then it must be a block node or lclVar.
//
// Notes:
//    This procedure centralizes all the logic to both enforce proper structure and
//    to properly construct any InitBlk/CpBlk node.

void Compiler::gtBlockOpInit(GenTreePtr result, GenTreePtr dst, GenTreePtr srcOrFillVal, bool isVolatile)
{
    if (!result->OperIsBlkOp())
    {
        assert(dst->TypeGet() != TYP_STRUCT);
        return;
    }
#ifdef DEBUG
    // If the copy involves GC pointers, the caller must have already set
    // the node additional members (gtGcPtrs, gtGcPtrCount, gtSlots) on the dst.
    if ((dst->gtOper == GT_OBJ) && dst->AsBlk()->HasGCPtr())
    {
        GenTreeObj* objNode = dst->AsObj();
        assert(objNode->gtGcPtrs != nullptr);
        assert(!IsUninitialized(objNode->gtGcPtrs));
        assert(!IsUninitialized(objNode->gtGcPtrCount));
        assert(!IsUninitialized(objNode->gtSlots) && objNode->gtSlots > 0);

        for (unsigned i = 0; i < objNode->gtGcPtrCount; ++i)
        {
            CorInfoGCType t = (CorInfoGCType)objNode->gtGcPtrs[i];
            switch (t)
            {
                case TYPE_GC_NONE:
                case TYPE_GC_REF:
                case TYPE_GC_BYREF:
                case TYPE_GC_OTHER:
                    break;
                default:
                    unreached();
            }
        }
    }
#endif // DEBUG

    /* In the case of CpBlk, we want to avoid generating
    * nodes where the source and destination are the same
    * because of two reasons, first, is useless, second
    * it introduces issues in liveness and also copying
    * memory from an overlapping memory location is
    * undefined both as per the ECMA standard and also
    * the memcpy semantics specify that.
    *
    * NOTE: In this case we'll only detect the case for addr of a local
    * and a local itself, any other complex expressions won't be
    * caught.
    *
    * TODO-Cleanup: though having this logic is goodness (i.e. avoids self-assignment
    * of struct vars very early), it was added because fgInterBlockLocalVarLiveness()
    * isn't handling self-assignment of struct variables correctly.  This issue may not
    * surface if struct promotion is ON (which is the case on x86/arm).  But still the
    * fundamental issue exists that needs to be addressed.
    */
    if (result->OperIsCopyBlkOp())
    {
        GenTreePtr currSrc = srcOrFillVal;
        GenTreePtr currDst = dst;

        if (currSrc->OperIsBlk() && (currSrc->AsBlk()->Addr()->OperGet() == GT_ADDR))
        {
            currSrc = currSrc->AsBlk()->Addr()->gtGetOp1();
        }
        if (currDst->OperIsBlk() && (currDst->AsBlk()->Addr()->OperGet() == GT_ADDR))
        {
            currDst = currDst->AsBlk()->Addr()->gtGetOp1();
        }

        if (currSrc->OperGet() == GT_LCL_VAR && currDst->OperGet() == GT_LCL_VAR &&
            currSrc->gtLclVarCommon.gtLclNum == currDst->gtLclVarCommon.gtLclNum)
        {
            // Make this a NOP
            // TODO-Cleanup: probably doesn't matter, but could do this earlier and avoid creating a GT_ASG
            result->gtBashToNOP();
            return;
        }
    }

    // Propagate all effect flags from children
    result->gtFlags |= dst->gtFlags & GTF_ALL_EFFECT;
    result->gtFlags |= result->gtOp.gtOp2->gtFlags & GTF_ALL_EFFECT;

    // REVERSE_OPS is necessary because the use must occur before the def
    result->gtFlags |= GTF_REVERSE_OPS;

    result->gtFlags |= (dst->gtFlags & GTF_EXCEPT) | (srcOrFillVal->gtFlags & GTF_EXCEPT);

    if (isVolatile)
    {
        result->gtFlags |= GTF_BLK_VOLATILE;
    }

#ifdef FEATURE_SIMD
    if (result->OperIsCopyBlkOp() && varTypeIsSIMD(srcOrFillVal))
    {
        // If the source is a GT_SIMD node of SIMD type, then the dst lclvar struct
        // should be labeled as simd intrinsic related struct.
        // This is done so that the morpher can transform any field accesses into
        // intrinsics, thus avoiding conflicting access methods (fields vs. whole-register).

        GenTree* src = srcOrFillVal;
        if (src->OperIsIndir() && (src->AsIndir()->Addr()->OperGet() == GT_ADDR))
        {
            src = src->AsIndir()->Addr()->gtGetOp1();
        }
        if (src->OperGet() == GT_SIMD)
        {
            if (dst->OperIsBlk() && (dst->AsIndir()->Addr()->OperGet() == GT_ADDR))
            {
                dst = dst->AsIndir()->Addr()->gtGetOp1();
            }

            if (dst->OperIsLocal() && varTypeIsStruct(dst))
            {
                setLclRelatedToSIMDIntrinsic(dst);
            }
        }
    }
#endif // FEATURE_SIMD
}

//------------------------------------------------------------------------
// gtNewBlkOpNode: Creates a GenTree for a block (struct) assignment.
//
// Arguments:
//    dst           - Destination or target to copy to / initialize the buffer.
//    srcOrFillVall - the size of the buffer to copy/initialize or zero, in the case of CpObj.
//    size          - The size of the buffer or a class token (in the case of CpObj).
//    isVolatile    - Whether this is a volatile memory operation or not.
//    isCopyBlock   - True if this is a block copy (rather than a block init).
//
// Return Value:
//    Returns the newly constructed and initialized block operation.
//
// Notes:
//    If size is zero, the dst must be a GT_OBJ with the class handle.
//    'dst' must be a block node or lclVar.
//
GenTree* Compiler::gtNewBlkOpNode(
    GenTreePtr dst, GenTreePtr srcOrFillVal, unsigned size, bool isVolatile, bool isCopyBlock)
{
    assert(dst->OperIsBlk() || dst->OperIsLocal());
    if (isCopyBlock)
    {
        srcOrFillVal->gtFlags |= GTF_DONT_CSE;
        if (srcOrFillVal->OperIsIndir() && (srcOrFillVal->gtGetOp1()->gtOper == GT_ADDR))
        {
            srcOrFillVal = srcOrFillVal->gtGetOp1()->gtGetOp1();
        }
    }
    else
    {
        // InitBlk
        assert(varTypeIsIntegral(srcOrFillVal));
        if (varTypeIsStruct(dst))
        {
            if (!srcOrFillVal->IsIntegralConst(0))
            {
                srcOrFillVal = gtNewOperNode(GT_INIT_VAL, TYP_INT, srcOrFillVal);
            }
        }
    }

    GenTree* result = gtNewAssignNode(dst, srcOrFillVal);
    gtBlockOpInit(result, dst, srcOrFillVal, isVolatile);
    return result;
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
GenTreePtr Compiler::gtNewPutArgReg(var_types type, GenTreePtr arg, regNumber argReg)
{
    assert(arg != nullptr);

    GenTreePtr node = nullptr;
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
    // A PUTARG_REG could be a MultiRegOp on arm since we could move a double register to two int registers.
    node = new (this, GT_PUTARG_REG) GenTreeMultiRegOp(GT_PUTARG_REG, type, arg, nullptr);
#else
    node            = gtNewOperNode(GT_PUTARG_REG, type, arg);
#endif
    node->gtRegNum = argReg;

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
//    The node is generated as GenTreeMultiRegOp on RyuJIT/armel, as GenTreeOp on all the other archs.
//
GenTreePtr Compiler::gtNewBitCastNode(var_types type, GenTreePtr arg)
{
    assert(arg != nullptr);

    GenTreePtr node = nullptr;
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
    // A BITCAST could be a MultiRegOp on arm since we could move a double register to two int registers.
    node = new (this, GT_BITCAST) GenTreeMultiRegOp(GT_BITCAST, type, arg, nullptr);
#else
    node            = gtNewOperNode(GT_BITCAST, type, arg);
#endif

    return node;
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

GenTreePtr Compiler::gtClone(GenTree* tree, bool complexOK)
{
    GenTreePtr copy;

    switch (tree->gtOper)
    {
        case GT_CNS_INT:

#if defined(LATE_DISASM)
            if (tree->IsIconHandle())
            {
                copy = gtNewIconHandleNode(tree->gtIntCon.gtIconVal, tree->gtFlags, tree->gtIntCon.gtFieldSeq);
                copy->gtIntCon.gtCompileTimeHandle = tree->gtIntCon.gtCompileTimeHandle;
                copy->gtType                       = tree->gtType;
            }
            else
#endif
            {
                copy = new (this, GT_CNS_INT)
                    GenTreeIntCon(tree->gtType, tree->gtIntCon.gtIconVal, tree->gtIntCon.gtFieldSeq);
                copy->gtIntCon.gtCompileTimeHandle = tree->gtIntCon.gtCompileTimeHandle;
            }
            break;

        case GT_CNS_LNG:
            copy = gtNewLconNode(tree->gtLngCon.gtLconVal);
            break;

        case GT_LCL_VAR:
            // Remember that the LclVar node has been cloned. The flag will be set
            // on 'copy' as well.
            tree->gtFlags |= GTF_VAR_CLONED;
            copy = gtNewLclvNode(tree->gtLclVarCommon.gtLclNum, tree->gtType, tree->gtLclVar.gtLclILoffs);
            break;

        case GT_LCL_FLD:
        case GT_LCL_FLD_ADDR:
            // Remember that the LclVar node has been cloned. The flag will be set
            // on 'copy' as well.
            tree->gtFlags |= GTF_VAR_CLONED;
            copy = new (this, tree->gtOper)
                GenTreeLclFld(tree->gtOper, tree->TypeGet(), tree->gtLclFld.gtLclNum, tree->gtLclFld.gtLclOffs);
            copy->gtLclFld.gtFieldSeq = tree->gtLclFld.gtFieldSeq;
            break;

        case GT_CLS_VAR:
            copy = new (this, GT_CLS_VAR)
                GenTreeClsVar(tree->gtType, tree->gtClsVar.gtClsVarHnd, tree->gtClsVar.gtFieldSeq);
            break;

        case GT_REG_VAR:
            assert(!"clone regvar");

        default:
            if (!complexOK)
            {
                return nullptr;
            }

            if (tree->gtOper == GT_FIELD)
            {
                GenTreePtr objp;

                // copied from line 9850

                objp = nullptr;
                if (tree->gtField.gtFldObj)
                {
                    objp = gtClone(tree->gtField.gtFldObj, false);
                    if (!objp)
                    {
                        return objp;
                    }
                }

                copy = gtNewFieldRef(tree->TypeGet(), tree->gtField.gtFldHnd, objp, tree->gtField.gtFldOffset);
                copy->gtField.gtFldMayOverlap = tree->gtField.gtFldMayOverlap;
            }
            else if (tree->gtOper == GT_ADD)
            {
                GenTreePtr op1 = tree->gtOp.gtOp1;
                GenTreePtr op2 = tree->gtOp.gtOp2;

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

                    copy = gtNewOperNode(GT_ADD, tree->TypeGet(), op1, op2);
                }
                else
                {
                    return nullptr;
                }
            }
            else if (tree->gtOper == GT_ADDR)
            {
                GenTreePtr op1 = gtClone(tree->gtOp.gtOp1);
                if (op1 == nullptr)
                {
                    return nullptr;
                }
                copy = gtNewOperNode(GT_ADDR, tree->TypeGet(), op1);
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
// gtCloneExpr: Create a copy of `tree`, adding flags `addFlags`, mapping
//              local `varNum` to int constant `varVal` if it appears at
//              the root, and mapping uses of local `deepVarNum` to constant
//              `deepVarVal` if they occur beyond the root.
//
// Arguments:
//    tree - GenTree to create a copy of
//    addFlags - GTF_* flags to add to the copied tree nodes
//    varNum - lclNum to replace at the root, or ~0 for no root replacement
//    varVal - If replacing at root, replace local `varNum` with IntCns `varVal`
//    deepVarNum - lclNum to replace uses of beyond the root, or ~0 for no replacement
//    deepVarVal - If replacing beyond root, replace `deepVarNum` with IntCns `deepVarVal`
//
// Return Value:
//    A copy of the given tree with the replacements and added flags specified.
//
// Notes:
//    Top-level callers should generally call the overload that doesn't have
//    the explicit `deepVarNum` and `deepVarVal` parameters; those are used in
//    recursive invocations to avoid replacing defs.

GenTreePtr Compiler::gtCloneExpr(
    GenTree* tree, unsigned addFlags, unsigned varNum, int varVal, unsigned deepVarNum, int deepVarVal)
{
    if (tree == nullptr)
    {
        return nullptr;
    }

    /* Figure out what kind of a node we have */

    genTreeOps oper = tree->OperGet();
    unsigned   kind = tree->OperKind();
    GenTree*   copy;

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        switch (oper)
        {
            case GT_CNS_INT:

#if defined(LATE_DISASM)
                if (tree->IsIconHandle())
                {
                    copy = gtNewIconHandleNode(tree->gtIntCon.gtIconVal, tree->gtFlags, tree->gtIntCon.gtFieldSeq);
                    copy->gtIntCon.gtCompileTimeHandle = tree->gtIntCon.gtCompileTimeHandle;
                    copy->gtType                       = tree->gtType;
                }
                else
#endif
                {
                    copy                               = gtNewIconNode(tree->gtIntCon.gtIconVal, tree->gtType);
                    copy->gtIntCon.gtCompileTimeHandle = tree->gtIntCon.gtCompileTimeHandle;
                    copy->gtIntCon.gtFieldSeq          = tree->gtIntCon.gtFieldSeq;
                }
                goto DONE;

            case GT_CNS_LNG:
                copy = gtNewLconNode(tree->gtLngCon.gtLconVal);
                goto DONE;

            case GT_CNS_DBL:
                copy         = gtNewDconNode(tree->gtDblCon.gtDconVal);
                copy->gtType = tree->gtType; // keep the same type
                goto DONE;

            case GT_CNS_STR:
                copy = gtNewSconNode(tree->gtStrCon.gtSconCPX, tree->gtStrCon.gtScpHnd);
                goto DONE;

            case GT_LCL_VAR:

                if (tree->gtLclVarCommon.gtLclNum == varNum)
                {
                    copy = gtNewIconNode(varVal, tree->gtType);
                    if (tree->gtFlags & GTF_VAR_ARR_INDEX)
                    {
                        copy->LabelIndex(this);
                    }
                }
                else
                {
                    // Remember that the LclVar node has been cloned. The flag will
                    // be set on 'copy' as well.
                    tree->gtFlags |= GTF_VAR_CLONED;
                    copy = gtNewLclvNode(tree->gtLclVar.gtLclNum, tree->gtType, tree->gtLclVar.gtLclILoffs);
                    copy->AsLclVarCommon()->SetSsaNum(tree->AsLclVarCommon()->GetSsaNum());
                }
                copy->gtFlags = tree->gtFlags;
                goto DONE;

            case GT_LCL_FLD:
                if (tree->gtLclFld.gtLclNum == varNum)
                {
                    IMPL_LIMITATION("replacing GT_LCL_FLD with a constant");
                }
                else
                {
                    // Remember that the LclVar node has been cloned. The flag will
                    // be set on 'copy' as well.
                    tree->gtFlags |= GTF_VAR_CLONED;
                    copy = new (this, GT_LCL_FLD)
                        GenTreeLclFld(tree->TypeGet(), tree->gtLclFld.gtLclNum, tree->gtLclFld.gtLclOffs);
                    copy->gtLclFld.gtFieldSeq = tree->gtLclFld.gtFieldSeq;
                    copy->gtFlags             = tree->gtFlags;
                }
                goto DONE;

            case GT_CLS_VAR:
                copy = new (this, GT_CLS_VAR)
                    GenTreeClsVar(tree->TypeGet(), tree->gtClsVar.gtClsVarHnd, tree->gtClsVar.gtFieldSeq);
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

            case GT_ARGPLACE:
                copy = gtNewArgPlaceHolderNode(tree->gtType, tree->gtArgPlace.gtArgPlaceClsHnd);
                goto DONE;

            case GT_REG_VAR:
                NO_WAY("Cloning of GT_REG_VAR node not supported");
                goto DONE;

            case GT_FTN_ADDR:
                copy = new (this, oper) GenTreeFptrVal(tree->gtType, tree->gtFptrVal.gtFptrMethod);

#ifdef FEATURE_READYTORUN_COMPILER
                copy->gtFptrVal.gtEntryPoint = tree->gtFptrVal.gtEntryPoint;
#endif
                goto DONE;

            case GT_CATCH_ARG:
            case GT_NO_OP:
                copy = new (this, oper) GenTree(oper, tree->gtType);
                goto DONE;

#if !FEATURE_EH_FUNCLETS
            case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
            case GT_JMP:
                copy = new (this, oper) GenTreeVal(oper, tree->gtType, tree->gtVal.gtVal1);
                goto DONE;

            case GT_LABEL:
                copy = new (this, oper) GenTreeLabel(tree->gtLabel.gtLabBB);
                goto DONE;

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

#if SMALL_TREE_NODES
        switch (oper)
        {
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
                    copy = gtNewLargeOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1,
                                              tree->OperIsBinary() ? tree->gtOp.gtOp2 : nullptr);
                }
                else // Always a large tree
                {
                    if (tree->OperIsBinary())
                    {
                        copy = gtNewOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1, tree->gtOp.gtOp2);
                    }
                    else
                    {
                        copy = gtNewOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1);
                    }
                }
                break;

            case GT_CAST:
                copy = new (this, LargeOpOpcode()) GenTreeCast(tree->TypeGet(), tree->gtCast.CastOp(),
                                                               tree->gtCast.gtCastType DEBUGARG(/*largeNode*/ TRUE));
                break;

            // The nodes below this are not bashed, so they can be allocated at their individual sizes.

            case GT_LIST:
                assert((tree->gtOp.gtOp2 == nullptr) || tree->gtOp.gtOp2->OperIsList());
                copy             = new (this, GT_LIST) GenTreeArgList(tree->gtOp.gtOp1);
                copy->gtOp.gtOp2 = tree->gtOp.gtOp2;
                break;

            case GT_FIELD_LIST:
                copy = new (this, GT_FIELD_LIST) GenTreeFieldList(tree->gtOp.gtOp1, tree->AsFieldList()->gtFieldOffset,
                                                                  tree->AsFieldList()->gtFieldType, nullptr);
                copy->gtOp.gtOp2 = tree->gtOp.gtOp2;
                copy->gtFlags    = (copy->gtFlags & ~GTF_FIELD_LIST_HEAD) | (tree->gtFlags & GTF_FIELD_LIST_HEAD);
                break;

            case GT_INDEX:
            {
                GenTreeIndex* asInd = tree->AsIndex();
                copy                = new (this, GT_INDEX)
                    GenTreeIndex(asInd->TypeGet(), asInd->Arr(), asInd->Index(), asInd->gtIndElemSize);
                copy->AsIndex()->gtStructElemClass = asInd->gtStructElemClass;
            }
            break;

            case GT_INDEX_ADDR:
            {
                GenTreeIndexAddr* asIndAddr = tree->AsIndexAddr();

                copy = new (this, GT_INDEX_ADDR)
                    GenTreeIndexAddr(asIndAddr->Arr(), asIndAddr->Index(), asIndAddr->gtElemType,
                                     asIndAddr->gtStructElemClass, asIndAddr->gtElemSize, asIndAddr->gtLenOffset,
                                     asIndAddr->gtElemOffset);
                copy->AsIndexAddr()->gtIndRngFailBB = asIndAddr->gtIndRngFailBB;
                copy->AsIndexAddr()->gtStkDepth     = asIndAddr->gtStkDepth;
            }
            break;

            case GT_ALLOCOBJ:
            {
                GenTreeAllocObj* asAllocObj = tree->AsAllocObj();
                copy = new (this, GT_ALLOCOBJ) GenTreeAllocObj(tree->TypeGet(), asAllocObj->gtNewHelper,
                                                               asAllocObj->gtAllocObjClsHnd, asAllocObj->gtOp1);
            }
            break;

            case GT_RUNTIMELOOKUP:
            {
                GenTreeRuntimeLookup* asRuntimeLookup = tree->AsRuntimeLookup();

                copy = new (this, GT_RUNTIMELOOKUP)
                    GenTreeRuntimeLookup(asRuntimeLookup->gtHnd, asRuntimeLookup->gtHndType, asRuntimeLookup->gtOp1);
            }
            break;

            case GT_ARR_LENGTH:
                copy = gtNewArrLen(tree->TypeGet(), tree->gtOp.gtOp1, tree->gtArrLen.ArrLenOffset());
                break;

            case GT_ARR_INDEX:
                copy = new (this, GT_ARR_INDEX)
                    GenTreeArrIndex(tree->TypeGet(),
                                    gtCloneExpr(tree->gtArrIndex.ArrObj(), addFlags, deepVarNum, deepVarVal),
                                    gtCloneExpr(tree->gtArrIndex.IndexExpr(), addFlags, deepVarNum, deepVarVal),
                                    tree->gtArrIndex.gtCurrDim, tree->gtArrIndex.gtArrRank,
                                    tree->gtArrIndex.gtArrElemType);
                break;

            case GT_QMARK:
                copy = new (this, GT_QMARK) GenTreeQmark(tree->TypeGet(), tree->gtOp.gtOp1, tree->gtOp.gtOp2, this);
#ifdef LEGACY_BACKEND
                VarSetOps::AssignAllowUninitRhs(this, copy->gtQmark.gtThenLiveSet, tree->gtQmark.gtThenLiveSet);
                VarSetOps::AssignAllowUninitRhs(this, copy->gtQmark.gtElseLiveSet, tree->gtQmark.gtElseLiveSet);
#endif
                break;

            case GT_OBJ:
                copy = new (this, GT_OBJ)
                    GenTreeObj(tree->TypeGet(), tree->gtOp.gtOp1, tree->AsObj()->gtClass, tree->gtBlk.gtBlkSize);
                copy->AsObj()->CopyGCInfo(tree->AsObj());
                copy->gtBlk.gtBlkOpGcUnsafe = tree->gtBlk.gtBlkOpGcUnsafe;
                break;

            case GT_BLK:
                copy = new (this, GT_BLK) GenTreeBlk(GT_BLK, tree->TypeGet(), tree->gtOp.gtOp1, tree->gtBlk.gtBlkSize);
                copy->gtBlk.gtBlkOpGcUnsafe = tree->gtBlk.gtBlkOpGcUnsafe;
                break;

            case GT_DYN_BLK:
                copy = new (this, GT_DYN_BLK) GenTreeDynBlk(tree->gtOp.gtOp1, tree->gtDynBlk.gtDynamicSize);
                copy->gtBlk.gtBlkOpGcUnsafe = tree->gtBlk.gtBlkOpGcUnsafe;
                break;

            case GT_BOX:
                copy = new (this, GT_BOX)
                    GenTreeBox(tree->TypeGet(), tree->gtOp.gtOp1, tree->gtBox.gtAsgStmtWhenInlinedBoxValue,
                               tree->gtBox.gtCopyStmtWhenInlinedBoxValue);
                break;

            case GT_INTRINSIC:
                copy = new (this, GT_INTRINSIC)
                    GenTreeIntrinsic(tree->TypeGet(), tree->gtOp.gtOp1, tree->gtOp.gtOp2,
                                     tree->gtIntrinsic.gtIntrinsicId, tree->gtIntrinsic.gtMethodHandle);
#ifdef FEATURE_READYTORUN_COMPILER
                copy->gtIntrinsic.gtEntryPoint = tree->gtIntrinsic.gtEntryPoint;
#endif
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

#ifdef FEATURE_SIMD
            case GT_SIMD:
            {
                GenTreeSIMD* simdOp = tree->AsSIMD();
                copy                = gtNewSIMDNode(simdOp->TypeGet(), simdOp->gtGetOp1(), simdOp->gtGetOp2IfPresent(),
                                     simdOp->gtSIMDIntrinsicID, simdOp->gtSIMDBaseType, simdOp->gtSIMDSize);
            }
            break;
#endif

            default:
                assert(!GenTree::IsExOp(tree->OperKind()) && tree->OperIsSimple());
                // We're in the SimpleOp case, so it's always unary or binary.
                if (GenTree::OperIsUnary(tree->OperGet()))
                {
                    copy = gtNewOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1, /*doSimplifications*/ false);
                }
                else
                {
                    assert(GenTree::OperIsBinary(tree->OperGet()));
                    copy = gtNewOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1, tree->gtOp.gtOp2);
                }
                break;
        }
#else
        // We're in the SimpleOp case, so it's always unary or binary.
        copy = gtNewOperNode(oper, tree->TypeGet(), tree->gtOp.gtOp1, tree->gtOp.gtOp2);
#endif

        // Some flags are conceptually part of the gtOper, and should be copied immediately.
        if (tree->gtOverflowEx())
        {
            copy->gtFlags |= GTF_OVERFLOW;
        }
        if (copy->OperGet() == GT_CAST)
        {
            copy->gtFlags |= (tree->gtFlags & GTF_UNSIGNED);
        }

        if (tree->gtOp.gtOp1)
        {
            if (tree->gtOper == GT_ASG)
            {
                // Don't replace varNum if it appears as the LHS of an assign.
                copy->gtOp.gtOp1 = gtCloneExpr(tree->gtOp.gtOp1, addFlags, -1, 0, deepVarNum, deepVarVal);
            }
            else
            {
                copy->gtOp.gtOp1 = gtCloneExpr(tree->gtOp.gtOp1, addFlags, deepVarNum, deepVarVal);
            }
        }

        if (tree->gtGetOp2IfPresent())
        {
            copy->gtOp.gtOp2 = gtCloneExpr(tree->gtOp.gtOp2, addFlags, deepVarNum, deepVarVal);
        }

        /* Flags */
        addFlags |= tree->gtFlags;

        // Copy any node annotations, if necessary.
        switch (tree->gtOper)
        {
            case GT_ASG:
            {
                IndirectAssignmentAnnotation* pIndirAnnot = nullptr;
                if (m_indirAssignMap != nullptr && GetIndirAssignMap()->Lookup(tree, &pIndirAnnot))
                {
                    IndirectAssignmentAnnotation* pNewIndirAnnot = new (this, CMK_Unknown)
                        IndirectAssignmentAnnotation(pIndirAnnot->m_lclNum, pIndirAnnot->m_fieldSeq,
                                                     pIndirAnnot->m_isEntire);
                    GetIndirAssignMap()->Set(copy, pNewIndirAnnot);
                }
            }
            break;

            case GT_STOREIND:
            case GT_IND:
            case GT_OBJ:
            case GT_STORE_OBJ:
            {
                ArrayInfo arrInfo;
                if (!tree->AsIndir()->gtOp1->OperIs(GT_INDEX_ADDR) && TryGetArrayInfo(tree->AsIndir(), &arrInfo))
                {
                    GetArrayInfoMap()->Set(copy, arrInfo);
                }
            }
            break;

            default:
                break;
        }

#ifdef DEBUG
        /* GTF_NODE_MASK should not be propagated from 'tree' to 'copy' */
        addFlags &= ~GTF_NODE_MASK;
#endif

        // Effects flags propagate upwards.
        if (copy->gtOp.gtOp1 != nullptr)
        {
            copy->gtFlags |= (copy->gtOp.gtOp1->gtFlags & GTF_ALL_EFFECT);
        }
        if (copy->gtGetOp2IfPresent() != nullptr)
        {
            copy->gtFlags |= (copy->gtGetOp2()->gtFlags & GTF_ALL_EFFECT);
        }

#ifdef LEGACY_BACKEND
        // The early morph for TailCall creates a GT_NOP with GTF_REG_VAL flag set
        // Thus we have to copy the gtRegNum/gtRegPair value if we clone it here.
        //
        if (tree->InReg())
        {
            copy->CopyReg(tree);
        }
#endif // LEGACY_BACKEND

        goto DONE;
    }

    /* See what kind of a special operator we have here */

    switch (oper)
    {
        case GT_STMT:
            copy = gtCloneExpr(tree->gtStmt.gtStmtExpr, addFlags, deepVarNum, deepVarVal);
            copy = gtNewStmt(copy, tree->gtStmt.gtStmtILoffsx);
            goto DONE;

        case GT_CALL:

            copy = new (this, GT_CALL) GenTreeCall(tree->TypeGet());

            copy->gtCall.gtCallObjp = tree->gtCall.gtCallObjp
                                          ? gtCloneExpr(tree->gtCall.gtCallObjp, addFlags, deepVarNum, deepVarVal)
                                          : nullptr;
            copy->gtCall.gtCallArgs =
                tree->gtCall.gtCallArgs
                    ? gtCloneExpr(tree->gtCall.gtCallArgs, addFlags, deepVarNum, deepVarVal)->AsArgList()
                    : nullptr;
            copy->gtCall.gtCallMoreFlags = tree->gtCall.gtCallMoreFlags;
            copy->gtCall.gtCallLateArgs =
                tree->gtCall.gtCallLateArgs
                    ? gtCloneExpr(tree->gtCall.gtCallLateArgs, addFlags, deepVarNum, deepVarVal)->AsArgList()
                    : nullptr;

#if !FEATURE_FIXED_OUT_ARGS
            copy->gtCall.regArgList      = tree->gtCall.regArgList;
            copy->gtCall.regArgListCount = tree->gtCall.regArgListCount;
#endif

            // The call sig comes from the EE and doesn't change throughout the compilation process, meaning
            // we only really need one physical copy of it. Therefore a shallow pointer copy will suffice.
            // (Note that this still holds even if the tree we are cloning was created by an inlinee compiler,
            // because the inlinee still uses the inliner's memory allocator anyway.)
            copy->gtCall.callSig = tree->gtCall.callSig;

            copy->gtCall.gtCallType    = tree->gtCall.gtCallType;
            copy->gtCall.gtReturnType  = tree->gtCall.gtReturnType;
            copy->gtCall.gtControlExpr = tree->gtCall.gtControlExpr;

            /* Copy the union */
            if (tree->gtCall.gtCallType == CT_INDIRECT)
            {
                copy->gtCall.gtCallCookie =
                    tree->gtCall.gtCallCookie ? gtCloneExpr(tree->gtCall.gtCallCookie, addFlags, deepVarNum, deepVarVal)
                                              : nullptr;
                copy->gtCall.gtCallAddr = tree->gtCall.gtCallAddr
                                              ? gtCloneExpr(tree->gtCall.gtCallAddr, addFlags, deepVarNum, deepVarVal)
                                              : nullptr;
            }
            else if (tree->gtFlags & GTF_CALL_VIRT_STUB)
            {
                copy->gtCall.gtCallMethHnd      = tree->gtCall.gtCallMethHnd;
                copy->gtCall.gtStubCallStubAddr = tree->gtCall.gtStubCallStubAddr;
            }
            else
            {
                copy->gtCall.gtCallMethHnd         = tree->gtCall.gtCallMethHnd;
                copy->gtCall.gtInlineCandidateInfo = tree->gtCall.gtInlineCandidateInfo;
            }

            if (tree->gtCall.fgArgInfo)
            {
                // Create and initialize the fgArgInfo for our copy of the call tree
                copy->gtCall.fgArgInfo = new (this, CMK_Unknown) fgArgInfo(copy->AsCall(), tree->AsCall());
            }
            else
            {
                copy->gtCall.fgArgInfo = nullptr;
            }
            copy->gtCall.gtRetClsHnd = tree->gtCall.gtRetClsHnd;

#if FEATURE_MULTIREG_RET
            copy->gtCall.gtReturnTypeDesc = tree->gtCall.gtReturnTypeDesc;
#endif

#ifdef LEGACY_BACKEND
            copy->gtCall.gtCallRegUsedMask = tree->gtCall.gtCallRegUsedMask;
#endif // LEGACY_BACKEND

#ifdef FEATURE_READYTORUN_COMPILER
            copy->gtCall.setEntryPoint(tree->gtCall.gtEntryPoint);
#endif

#ifdef DEBUG
            copy->gtCall.gtInlineObservation = tree->gtCall.gtInlineObservation;
#endif

            copy->AsCall()->CopyOtherRegFlags(tree->AsCall());
            break;

        case GT_FIELD:

            copy = gtNewFieldRef(tree->TypeGet(), tree->gtField.gtFldHnd, nullptr, tree->gtField.gtFldOffset);

            copy->gtField.gtFldObj = tree->gtField.gtFldObj
                                         ? gtCloneExpr(tree->gtField.gtFldObj, addFlags, deepVarNum, deepVarVal)
                                         : nullptr;
            copy->gtField.gtFldMayOverlap = tree->gtField.gtFldMayOverlap;
#ifdef FEATURE_READYTORUN_COMPILER
            copy->gtField.gtFieldLookup = tree->gtField.gtFieldLookup;
#endif

            break;

        case GT_ARR_ELEM:
        {
            GenTreePtr inds[GT_ARR_MAX_RANK];
            for (unsigned dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
            {
                inds[dim] = gtCloneExpr(tree->gtArrElem.gtArrInds[dim], addFlags, deepVarNum, deepVarVal);
            }
            copy = new (this, GT_ARR_ELEM)
                GenTreeArrElem(tree->TypeGet(), gtCloneExpr(tree->gtArrElem.gtArrObj, addFlags, deepVarNum, deepVarVal),
                               tree->gtArrElem.gtArrRank, tree->gtArrElem.gtArrElemSize, tree->gtArrElem.gtArrElemType,
                               &inds[0]);
        }
        break;

        case GT_ARR_OFFSET:
        {
            copy = new (this, GT_ARR_OFFSET)
                GenTreeArrOffs(tree->TypeGet(), gtCloneExpr(tree->gtArrOffs.gtOffset, addFlags, deepVarNum, deepVarVal),
                               gtCloneExpr(tree->gtArrOffs.gtIndex, addFlags, deepVarNum, deepVarVal),
                               gtCloneExpr(tree->gtArrOffs.gtArrObj, addFlags, deepVarNum, deepVarVal),
                               tree->gtArrOffs.gtCurrDim, tree->gtArrOffs.gtArrRank, tree->gtArrOffs.gtArrElemType);
        }
        break;

        case GT_CMPXCHG:
            copy = new (this, GT_CMPXCHG)
                GenTreeCmpXchg(tree->TypeGet(),
                               gtCloneExpr(tree->gtCmpXchg.gtOpLocation, addFlags, deepVarNum, deepVarVal),
                               gtCloneExpr(tree->gtCmpXchg.gtOpValue, addFlags, deepVarNum, deepVarVal),
                               gtCloneExpr(tree->gtCmpXchg.gtOpComparand, addFlags, deepVarNum, deepVarVal));
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            copy = new (this, oper)
                GenTreeBoundsChk(oper, tree->TypeGet(),
                                 gtCloneExpr(tree->gtBoundsChk.gtIndex, addFlags, deepVarNum, deepVarVal),
                                 gtCloneExpr(tree->gtBoundsChk.gtArrLen, addFlags, deepVarNum, deepVarVal),
                                 tree->gtBoundsChk.gtThrowKind);
            copy->gtBoundsChk.gtIndRngFailBB = tree->gtBoundsChk.gtIndRngFailBB;
            copy->gtBoundsChk.gtStkDepth     = tree->gtBoundsChk.gtStkDepth;
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            copy = new (this, oper)
                GenTreeDynBlk(gtCloneExpr(tree->gtDynBlk.Addr(), addFlags, deepVarNum, deepVarVal),
                              gtCloneExpr(tree->gtDynBlk.gtDynamicSize, addFlags, deepVarNum, deepVarVal));
            break;

        default:
#ifdef DEBUG
            gtDispTree(tree);
#endif
            NO_WAY("unexpected operator");
    }

DONE:

    // If it has a zero-offset field seq, copy annotation.
    if (tree->TypeGet() == TYP_BYREF)
    {
        FieldSeqNode* fldSeq = nullptr;
        if (GetZeroOffsetFieldMap()->Lookup(tree, &fldSeq))
        {
            GetZeroOffsetFieldMap()->Set(copy, fldSeq);
        }
    }

    copy->gtVNPair = tree->gtVNPair; // A cloned tree gets the orginal's Value number pair

    /* We assume the FP stack level will be identical */

    copy->gtCopyFPlvl(tree);

    /* Compute the flags for the copied node. Note that we can do this only
       if we didnt gtFoldExpr(copy) */

    if (copy->gtOper == oper)
    {
        addFlags |= tree->gtFlags;

#ifdef DEBUG
        /* GTF_NODE_MASK should not be propagated from 'tree' to 'copy' */
        addFlags &= ~GTF_NODE_MASK;
#endif
        // Some other flags depend on the context of the expression, and should not be preserved.
        // For example, GTF_RELOP_QMARK:
        if (copy->OperKind() & GTK_RELOP)
        {
            addFlags &= ~GTF_RELOP_QMARK;
        }
        // On the other hand, if we're creating such a context, restore this flag.
        if (copy->OperGet() == GT_QMARK)
        {
            copy->gtOp.gtOp1->gtFlags |= GTF_RELOP_QMARK;
        }

        // Clear the copy's GTF_ASG and GTF_EXCEPT bits; these will instead be taken from the source.
        copy->gtFlags &= ~(GTF_ASG | GTF_EXCEPT);

        copy->gtFlags |= addFlags;
    }

    /* GTF_COLON_COND should be propagated from 'tree' to 'copy' */
    copy->gtFlags |= (tree->gtFlags & GTF_COLON_COND);

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
// gtReplaceTree: Replace a tree with a new tree.
//
// Arguments:
//    stmt            - The top-level root stmt of the tree being replaced.
//                      Must not be null.
//    tree            - The tree being replaced. Must not be null.
//    replacementTree - The replacement tree. Must not be null.
//
// Return Value:
//    The tree node that replaces the old tree.
//
// Assumptions:
//    The sequencing of the stmt has been done.
//
// Notes:
//    The caller must ensure that the original statement has been sequenced,
//    and the side effect flags are updated on the statement nodes,
//    but this method will sequence 'replacementTree', and insert it into the
//    proper place in the statement sequence.

GenTreePtr Compiler::gtReplaceTree(GenTreePtr stmt, GenTreePtr tree, GenTreePtr replacementTree)
{
    assert(fgStmtListThreaded);
    assert(tree != nullptr);
    assert(stmt != nullptr);
    assert(replacementTree != nullptr);

    GenTreePtr* treePtr    = nullptr;
    GenTreePtr  treeParent = tree->gtGetParent(&treePtr);

    assert(treeParent != nullptr || tree == stmt->gtStmt.gtStmtExpr);

    if (treePtr == nullptr)
    {
        // Replace the stmt expr and rebuild the linear order for "stmt".
        assert(treeParent == nullptr);
        assert(fgOrder != FGOrderLinear);
        stmt->gtStmt.gtStmtExpr = tree;
        fgSetStmtSeq(stmt);
    }
    else
    {
        assert(treeParent != nullptr);

        // Check to see if the node to be replaced is a call argument and if so,
        // set `treeParent` to the call node.
        GenTree* cursor = treeParent;
        while ((cursor != nullptr) && (cursor->OperGet() == GT_LIST))
        {
            cursor = cursor->gtNext;
        }

        if ((cursor != nullptr) && (cursor->OperGet() == GT_CALL))
        {
            treeParent = cursor;
        }

#ifdef DEBUG
        GenTree** useEdge;
        assert(treeParent->TryGetUse(tree, &useEdge));
        assert(useEdge == treePtr);
#endif // DEBUG

        GenTreePtr treeFirstNode = fgGetFirstNode(tree);
        GenTreePtr treeLastNode  = tree;
        GenTreePtr treePrevNode  = treeFirstNode->gtPrev;
        GenTreePtr treeNextNode  = treeLastNode->gtNext;

        treeParent->ReplaceOperand(treePtr, replacementTree);

        // Build the linear order for "replacementTree".
        fgSetTreeSeq(replacementTree, treePrevNode);

        // Restore linear-order Prev and Next for "replacementTree".
        if (treePrevNode != nullptr)
        {
            treeFirstNode         = fgGetFirstNode(replacementTree);
            treeFirstNode->gtPrev = treePrevNode;
            treePrevNode->gtNext  = treeFirstNode;
        }
        else
        {
            // Update the linear oder start of "stmt" if treeFirstNode
            // appears to have replaced the original first node.
            assert(treeFirstNode == stmt->gtStmt.gtStmtList);
            stmt->gtStmt.gtStmtList = fgGetFirstNode(replacementTree);
        }

        if (treeNextNode != nullptr)
        {
            treeLastNode         = replacementTree;
            treeLastNode->gtNext = treeNextNode;
            treeNextNode->gtPrev = treeLastNode;
        }
    }

    return replacementTree;
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

void Compiler::gtUpdateSideEffects(GenTree* stmt, GenTree* tree)
{
    if (fgStmtListThreaded)
    {
        gtUpdateTreeAncestorsSideEffects(tree);
    }
    else
    {
        gtUpdateStmtSideEffects(stmt);
    }
}

//------------------------------------------------------------------------
// gtUpdateTreeAncestorsSideEffects: Update the side effects of a tree and its ancestors
//                                   when statement order has been established.
//
// Arguments:
//    tree            - Tree to update the side effects for

void Compiler::gtUpdateTreeAncestorsSideEffects(GenTree* tree)
{
    assert(fgStmtListThreaded);
    while (tree != nullptr)
    {
        gtResetNodeSideEffects(tree);
        unsigned nChildren = tree->NumChildren();
        for (unsigned childNum = 0; childNum < nChildren; childNum++)
        {
            tree->gtFlags |= (tree->GetChild(childNum)->gtFlags & GTF_ALL_EFFECT);
        }
        tree = tree->gtGetParent(nullptr);
    }
}

//------------------------------------------------------------------------
// gtUpdateStmtSideEffects: Update the side effects for statement tree nodes.
//
// Arguments:
//    stmt            - The statement to update side effects on

void Compiler::gtUpdateStmtSideEffects(GenTree* stmt)
{
    fgWalkTree(&stmt->gtStmt.gtStmtExpr, fgUpdateSideEffectsPre, fgUpdateSideEffectsPost);
}

//------------------------------------------------------------------------
// gtResetNodeSideEffects: Update the side effects based on the node operation.
//
// Arguments:
//    tree            - Tree to update the side effects on
//
// Notes:
//    This method currently only updates GTF_EXCEPT and GTF_ASG flags. The other side effect
//    flags may remain unnecessarily (conservatively) set.
//    The caller of this method is expected to update the flags based on the children's flags.

void Compiler::gtResetNodeSideEffects(GenTree* tree)
{
    if (tree->OperMayThrow(this))
    {
        tree->gtFlags |= GTF_EXCEPT;
    }
    else
    {
        tree->gtFlags &= ~GTF_EXCEPT;
        if (tree->OperIsIndirOrArrLength())
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
}

//------------------------------------------------------------------------
// fgUpdateSideEffectsPre: Update the side effects based on the tree operation.
//
// Arguments:
//    pTree            - Pointer to the tree to update the side effects
//    fgWalkPre        - Walk data
//
// Notes:
//    This method currently only updates GTF_EXCEPT and GTF_ASG flags. The other side effect
//    flags may remain unnecessarily (conservatively) set.

Compiler::fgWalkResult Compiler::fgUpdateSideEffectsPre(GenTree** pTree, fgWalkData* fgWalkPre)
{
    fgWalkPre->compiler->gtResetNodeSideEffects(*pTree);

    return WALK_CONTINUE;
}

//------------------------------------------------------------------------
// fgUpdateSideEffectsPost: Update the side effects of the parent based on the tree's flags.
//
// Arguments:
//    pTree            - Pointer to the tree
//    fgWalkPost       - Walk data
//
// Notes:
//    The routine is used for updating the stale side effect flags for ancestor
//    nodes starting from treeParent up to the top-level stmt expr.

Compiler::fgWalkResult Compiler::fgUpdateSideEffectsPost(GenTree** pTree, fgWalkData* fgWalkPost)
{
    GenTree* tree   = *pTree;
    GenTree* parent = fgWalkPost->parent;
    if (parent != nullptr)
    {
        parent->gtFlags |= (tree->gtFlags & GTF_ALL_EFFECT);
    }
    return WALK_CONTINUE;
}

/*****************************************************************************
 *
 *  Compares two trees and returns true when both trees are the same.
 *  Instead of fully comparing the two trees this method can just return false.
 *  Thus callers should not assume that the trees are different when false is returned.
 *  Only when true is returned can the caller perform code optimizations.
 *  The current implementation only compares a limited set of LEAF/CONST node
 *  and returns false for all othere trees.
 */
bool Compiler::gtCompareTree(GenTree* op1, GenTree* op2)
{
    /* Make sure that both trees are of the same GT node kind */
    if (op1->OperGet() != op2->OperGet())
    {
        return false;
    }

    /* Make sure that both trees are returning the same type */
    if (op1->gtType != op2->gtType)
    {
        return false;
    }

    /* Figure out what kind of a node we have */

    genTreeOps oper = op1->OperGet();
    unsigned   kind = op1->OperKind();

    /* Is this a constant or leaf node? */

    if (kind & (GTK_CONST | GTK_LEAF))
    {
        switch (oper)
        {
            case GT_CNS_INT:
                if ((op1->gtIntCon.gtIconVal == op2->gtIntCon.gtIconVal) && GenTree::SameIconHandleFlag(op1, op2))
                {
                    return true;
                }
                break;

            case GT_CNS_LNG:
                if (op1->gtLngCon.gtLconVal == op2->gtLngCon.gtLconVal)
                {
                    return true;
                }
                break;

            case GT_CNS_STR:
                if (op1->gtStrCon.gtSconCPX == op2->gtStrCon.gtSconCPX)
                {
                    return true;
                }
                break;

            case GT_LCL_VAR:
                if (op1->gtLclVarCommon.gtLclNum == op2->gtLclVarCommon.gtLclNum)
                {
                    return true;
                }
                break;

            case GT_CLS_VAR:
                if (op1->gtClsVar.gtClsVarHnd == op2->gtClsVar.gtClsVarHnd)
                {
                    return true;
                }
                break;

            default:
                // we return false for these unhandled 'oper' kinds
                break;
        }
    }
    return false;
}

GenTreePtr Compiler::gtGetThisArg(GenTreeCall* call)
{
    if (call->gtCallObjp != nullptr)
    {
        if (call->gtCallObjp->gtOper != GT_NOP && call->gtCallObjp->gtOper != GT_ASG)
        {
            if (!(call->gtCallObjp->gtFlags & GTF_LATE_ARG))
            {
                return call->gtCallObjp;
            }
        }

        if (call->gtCallLateArgs)
        {
            regNumber        thisReg         = REG_ARG_0;
            unsigned         argNum          = 0;
            fgArgTabEntryPtr thisArgTabEntry = gtArgEntryByArgNum(call, argNum);
            GenTreePtr       result          = thisArgTabEntry->node;

#if !FEATURE_FIXED_OUT_ARGS
            GenTreePtr lateArgs = call->gtCallLateArgs;
            regList    list     = call->regArgList;
            int        index    = 0;
            while (lateArgs != NULL)
            {
                assert(lateArgs->gtOper == GT_LIST);
                assert(index < call->regArgListCount);
                regNumber curArgReg = list[index];
                if (curArgReg == thisReg)
                {
                    if (optAssertionPropagatedCurrentStmt)
                        result = lateArgs->gtOp.gtOp1;

                    assert(result == lateArgs->gtOp.gtOp1);
                }

                lateArgs = lateArgs->gtOp.gtOp2;
                index++;
            }
#endif
            return result;
        }
    }
    return nullptr;
}

bool GenTree::gtSetFlags() const
{
    //
    // When FEATURE_SET_FLAGS (_TARGET_ARM_) is active the method returns true
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
    if (!varTypeIsIntegralOrI(TypeGet()))
    {
        return false;
    }

#if defined(LEGACY_BACKEND) && !FEATURE_SET_FLAGS && defined(_TARGET_XARCH_)
    // Return true if/when the codegen for this node will set the flags
    //
    //
    if ((gtOper == GT_IND) || (gtOper == GT_MUL) || (gtOper == GT_DIV))
    {
        return false;
    }
    else if (gtOverflowEx())
    {
        return false;
    }
    else
    {
        return true;
    }
#else // !(defined(LEGACY_BACKEND) && !FEATURE_SET_FLAGS && defined(_TARGET_XARCH_))

#if FEATURE_SET_FLAGS
    assert(OperIsSimple());
#endif
    if (((gtFlags & GTF_SET_FLAGS) != 0) && (gtOper != GT_IND))
    {
        // GTF_SET_FLAGS is not valid on GT_IND and is overlaid with GTF_NONFAULTING_IND
        return true;
    }
    else
    {
        return false;
    }
#endif // !(defined(LEGACY_BACKEND) && !FEATURE_SET_FLAGS && defined(_TARGET_XARCH_))
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

/*****************************************************************************/
void GenTree::CopyTo(class Compiler* comp, const GenTree& gt)
{
    SetOperRaw(gt.OperGet());

    gtType          = gt.gtType;
    gtAssertionInfo = gt.gtAssertionInfo;

    gtRegNum = gt.gtRegNum; // one union member.
    CopyCosts(&gt);

    gtFlags  = gt.gtFlags;
    gtVNPair = gt.gtVNPair;

    gtRsvdRegs = gt.gtRsvdRegs;

#ifdef LEGACY_BACKEND
    gtUsedRegs = gt.gtUsedRegs;
#endif // LEGACY_BACKEND

#if FEATURE_STACK_FP_X87
    gtFPlvl = gt.gtFPlvl;
#endif // FEATURE_STACK_FP_X87

    gtNext = gt.gtNext;
    gtPrev = gt.gtPrev;
#ifdef DEBUG
    gtTreeID = gt.gtTreeID;
    gtSeqNum = gt.gtSeqNum;
#endif
    // Largest node subtype:
    void* remDst = reinterpret_cast<char*>(this) + sizeof(GenTree);
    void* remSrc = reinterpret_cast<char*>(const_cast<GenTree*>(&gt)) + sizeof(GenTree);
    memcpy(remDst, remSrc, TREE_NODE_SZ_LARGE - sizeof(GenTree));
}

void GenTree::CopyToSmall(const GenTree& gt)
{
    // Small node size is defined by GenTreeOp.
    void* remDst = reinterpret_cast<char*>(this) + sizeof(GenTree);
    void* remSrc = reinterpret_cast<char*>(const_cast<GenTree*>(&gt)) + sizeof(GenTree);
    memcpy(remDst, remSrc, TREE_NODE_SZ_SMALL - sizeof(GenTree));
}

unsigned GenTree::NumChildren()
{
    if (OperIsConst() || OperIsLeaf())
    {
        return 0;
    }
    else if (OperIsUnary())
    {
        if (OperGet() == GT_NOP || OperGet() == GT_RETURN || OperGet() == GT_RETFILT)
        {
            if (gtOp.gtOp1 == nullptr)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            return 1;
        }
    }
    else if (OperIsBinary())
    {
        // All binary operators except LEA have at least one arg; the second arg may sometimes be null, however.
        if (OperGet() == GT_LEA)
        {
            unsigned childCount = 0;
            if (gtOp.gtOp1 != nullptr)
            {
                childCount++;
            }
            if (gtOp.gtOp2 != nullptr)
            {
                childCount++;
            }
            return childCount;
        }
        // Special case for assignment of dynamic block.
        // This is here to duplicate the former case where the size may be evaluated prior to the
        // source and destination addresses. In order to do this, we treat the size as a child of the
        // assignment.
        // TODO-1stClassStructs-Cleanup: Remove all this special casing, and ensure that the diffs are reasonable.
        if ((OperGet() == GT_ASG) && (gtOp.gtOp1->OperGet() == GT_DYN_BLK) && (gtOp.gtOp1->AsDynBlk()->gtEvalSizeFirst))
        {
            return 3;
        }
        assert(gtOp.gtOp1 != nullptr);
        if (gtOp.gtOp2 == nullptr)
        {
            return 1;
        }
        else
        {
            return 2;
        }
    }
    else
    {
        // Special
        switch (OperGet())
        {
            case GT_CMPXCHG:
                return 3;

            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
                return 2;

            case GT_FIELD:
            case GT_STMT:
                return 1;

            case GT_ARR_ELEM:
                return 1 + AsArrElem()->gtArrRank;

            // This really has two children, but if the size is evaluated first, we treat it as a child of the
            // parent assignment.
            case GT_DYN_BLK:
                if (AsDynBlk()->gtEvalSizeFirst)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }

            case GT_ARR_OFFSET:
            case GT_STORE_DYN_BLK:
                return 3;

            case GT_CALL:
            {
                GenTreeCall* call = AsCall();
                unsigned     res  = 0; // arg list(s) (including late args).
                if (call->gtCallObjp != nullptr)
                {
                    res++; // Add objp?
                }
                if (call->gtCallArgs != nullptr)
                {
                    res++; // Add args?
                }
                if (call->gtCallLateArgs != nullptr)
                {
                    res++; // Add late args?
                }
                if (call->gtControlExpr != nullptr)
                {
                    res++;
                }

                if (call->gtCallType == CT_INDIRECT)
                {
                    if (call->gtCallCookie != nullptr)
                    {
                        res++;
                    }
                    if (call->gtCallAddr != nullptr)
                    {
                        res++;
                    }
                }
                return res;
            }
            case GT_NONE:
                return 0;
            default:
                unreached();
        }
    }
}

GenTreePtr GenTree::GetChild(unsigned childNum)
{
    assert(childNum < NumChildren()); // Precondition.
    assert(NumChildren() <= MAX_CHILDREN);
    assert(!(OperIsConst() || OperIsLeaf()));
    if (OperIsUnary())
    {
        return AsUnOp()->gtOp1;
    }
    // Special case for assignment of dynamic block.
    // This code is here to duplicate the former case where the size may be evaluated prior to the
    // source and destination addresses. In order to do this, we treat the size as a child of the
    // assignment.
    // TODO-1stClassStructs: Revisit the need to duplicate former behavior, so that we can remove
    // these special cases.
    if ((OperGet() == GT_ASG) && (gtOp.gtOp1->OperGet() == GT_DYN_BLK) && (childNum == 2))
    {
        return gtOp.gtOp1->AsDynBlk()->gtDynamicSize;
    }
    else if (OperIsBinary())
    {
        if (OperIsAddrMode())
        {
            // If this is the first (0th) child, only return op1 if it is non-null
            // Otherwise, we return gtOp2.
            if (childNum == 0 && AsOp()->gtOp1 != nullptr)
            {
                return AsOp()->gtOp1;
            }
            return AsOp()->gtOp2;
        }
        // TODO-Cleanup: Consider handling ReverseOps here, and then we wouldn't have to handle it in
        // fgGetFirstNode().  However, it seems that it causes loop hoisting behavior to change.
        if (childNum == 0)
        {
            return AsOp()->gtOp1;
        }
        else
        {
            return AsOp()->gtOp2;
        }
    }
    else
    {
        // Special
        switch (OperGet())
        {
            case GT_CMPXCHG:
                switch (childNum)
                {
                    case 0:
                        return AsCmpXchg()->gtOpLocation;
                    case 1:
                        return AsCmpXchg()->gtOpValue;
                    case 2:
                        return AsCmpXchg()->gtOpComparand;
                    default:
                        unreached();
                }
            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
                switch (childNum)
                {
                    case 0:
                        return AsBoundsChk()->gtIndex;
                    case 1:
                        return AsBoundsChk()->gtArrLen;
                    default:
                        unreached();
                }

            case GT_STORE_DYN_BLK:
                switch (childNum)
                {
                    case 0:
                        return AsDynBlk()->Addr();
                    case 1:
                        return AsDynBlk()->Data();
                    case 2:
                        return AsDynBlk()->gtDynamicSize;
                    default:
                        unreached();
                }
            case GT_DYN_BLK:
                switch (childNum)
                {
                    case 0:
                        return AsDynBlk()->Addr();
                    case 1:
                        assert(!AsDynBlk()->gtEvalSizeFirst);
                        return AsDynBlk()->gtDynamicSize;
                    default:
                        unreached();
                }

            case GT_FIELD:
                return AsField()->gtFldObj;

            case GT_STMT:
                return AsStmt()->gtStmtExpr;

            case GT_ARR_ELEM:
                if (childNum == 0)
                {
                    return AsArrElem()->gtArrObj;
                }
                else
                {
                    return AsArrElem()->gtArrInds[childNum - 1];
                }

            case GT_ARR_OFFSET:
                switch (childNum)
                {
                    case 0:
                        return AsArrOffs()->gtOffset;
                    case 1:
                        return AsArrOffs()->gtIndex;
                    case 2:
                        return AsArrOffs()->gtArrObj;
                    default:
                        unreached();
                }

            case GT_CALL:
            {
                // The if chain below assumes that all possible children are non-null.
                // If some are null, "virtually skip them."
                // If there isn't "virtually skip it."
                GenTreeCall* call = AsCall();

                if (call->gtCallObjp == nullptr)
                {
                    childNum++;
                }
                if (childNum >= 1 && call->gtCallArgs == nullptr)
                {
                    childNum++;
                }
                if (childNum >= 2 && call->gtCallLateArgs == nullptr)
                {
                    childNum++;
                }
                if (childNum >= 3 && call->gtControlExpr == nullptr)
                {
                    childNum++;
                }
                if (call->gtCallType == CT_INDIRECT)
                {
                    if (childNum >= 4 && call->gtCallCookie == nullptr)
                    {
                        childNum++;
                    }
                }

                if (childNum == 0)
                {
                    return call->gtCallObjp;
                }
                else if (childNum == 1)
                {
                    return call->gtCallArgs;
                }
                else if (childNum == 2)
                {
                    return call->gtCallLateArgs;
                }
                else if (childNum == 3)
                {
                    return call->gtControlExpr;
                }
                else
                {
                    assert(call->gtCallType == CT_INDIRECT);
                    if (childNum == 4)
                    {
                        return call->gtCallCookie;
                    }
                    else
                    {
                        assert(childNum == 5);
                        return call->gtCallAddr;
                    }
                }
            }
            case GT_NONE:
                unreached();
            default:
                unreached();
        }
    }
}

GenTreeUseEdgeIterator::GenTreeUseEdgeIterator()
    : m_advance(nullptr), m_node(nullptr), m_edge(nullptr), m_argList(nullptr), m_state(-1)
{
}

GenTreeUseEdgeIterator::GenTreeUseEdgeIterator(GenTree* node)
    : m_advance(nullptr), m_node(node), m_edge(nullptr), m_argList(nullptr), m_state(0)
{
    assert(m_node != nullptr);

    // NOTE: the switch statement below must be updated when introducing new nodes.

    switch (m_node->OperGet())
    {
        // Leaf nodes
        case GT_LCL_VAR:
        case GT_LCL_FLD:
        case GT_LCL_VAR_ADDR:
        case GT_LCL_FLD_ADDR:
        case GT_CATCH_ARG:
        case GT_LABEL:
        case GT_FTN_ADDR:
        case GT_RET_EXPR:
        case GT_CNS_INT:
        case GT_CNS_LNG:
        case GT_CNS_DBL:
        case GT_CNS_STR:
        case GT_MEMORYBARRIER:
        case GT_JMP:
        case GT_JCC:
        case GT_SETCC:
        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
#endif // !FEATURE_EH_FUNCLETS
        case GT_PHI_ARG:
#ifndef LEGACY_BACKEND
        case GT_JMPTABLE:
#endif // LEGACY_BACKEND
        case GT_REG_VAR:
        case GT_CLS_VAR:
        case GT_CLS_VAR_ADDR:
        case GT_ARGPLACE:
        case GT_PHYSREG:
        case GT_EMITNOP:
        case GT_PINVOKE_PROLOG:
        case GT_PINVOKE_EPILOG:
        case GT_IL_OFFSET:
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
        case GT_CAST:
        case GT_BITCAST:
        case GT_CKFINITE:
        case GT_LCLHEAP:
        case GT_ADDR:
        case GT_IND:
        case GT_OBJ:
        case GT_BLK:
        case GT_BOX:
        case GT_ALLOCOBJ:
        case GT_RUNTIMELOOKUP:
        case GT_INIT_VAL:
        case GT_JTRUE:
        case GT_SWITCH:
        case GT_NULLCHECK:
        case GT_PUTARG_REG:
        case GT_PUTARG_STK:
#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
        case GT_PUTARG_SPLIT:
#endif // !LEGACY_BACKEND && _TARGET_ARM_
        case GT_RETURNTRAP:
            m_edge = &m_node->AsUnOp()->gtOp1;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            return;

        // Unary operators with an optional operand
        case GT_NOP:
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
        case GT_PHI:
            SetEntryStateForList(m_node->AsUnOp()->gtOp1);
            return;

        case GT_FIELD_LIST:
            SetEntryStateForList(m_node);
            return;

#ifdef FEATURE_SIMD
        case GT_SIMD:
            if (m_node->AsSIMD()->gtSIMDIntrinsicID == SIMDIntrinsicInitN)
            {
                SetEntryStateForList(m_node->AsSIMD()->gtOp1);
            }
            else
            {
                SetEntryStateForBinOp();
            }
            return;
#endif // FEATURE_SIMD

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
        case GT_CMPXCHG:
            m_edge = &m_node->AsCmpXchg()->gtOpLocation;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceCmpXchg;
            return;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            m_edge = &m_node->AsBoundsChk()->gtIndex;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceBoundsChk;
            return;

        case GT_FIELD:
            if (m_node->AsField()->gtFldObj == nullptr)
            {
                m_state = -1;
            }
            else
            {
                m_edge    = &m_node->AsField()->gtFldObj;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
            }
            return;

        case GT_STMT:
            if (m_node->AsStmt()->gtStmtExpr == nullptr)
            {
                m_state = -1;
            }
            else
            {
                m_edge    = &m_node->AsStmt()->gtStmtExpr;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
            }
            return;

        case GT_ARR_ELEM:
            m_edge = &m_node->AsArrElem()->gtArrObj;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceArrElem;
            return;

        case GT_ARR_OFFSET:
            m_edge = &m_node->AsArrOffs()->gtOffset;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceArrOffset;
            return;

        case GT_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = m_node->AsDynBlk();
            m_edge                        = dynBlock->gtEvalSizeFirst ? &dynBlock->gtDynamicSize : &dynBlock->gtOp1;
            assert(*m_edge != nullptr);
            m_advance = &GenTreeUseEdgeIterator::AdvanceDynBlk;
        }
            return;

        case GT_STORE_DYN_BLK:
        {
            GenTreeDynBlk* const dynBlock = m_node->AsDynBlk();
            if (dynBlock->gtEvalSizeFirst)
            {
                m_edge = &dynBlock->gtDynamicSize;
            }
            else
            {
                m_edge = dynBlock->IsReverseOp() ? &dynBlock->gtOp2 : &dynBlock->gtOp1;
            }
            assert(*m_edge != nullptr);

            m_advance = &GenTreeUseEdgeIterator::AdvanceStoreDynBlk;
        }
            return;

        case GT_CALL:
            AdvanceCall<CALL_INSTANCE>();
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
            m_edge  = &m_node->AsCmpXchg()->gtOpValue;
            m_state = 1;
            break;
        case 1:
            m_edge    = &m_node->AsCmpXchg()->gtOpComparand;
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            break;
        default:
            unreached();
    }

    assert(*m_edge != nullptr);
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceBoundsChk: produces the next operand of a BoundsChk node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceBoundsChk()
{
    m_edge = &m_node->AsBoundsChk()->gtArrLen;
    assert(*m_edge != nullptr);
    m_advance = &GenTreeUseEdgeIterator::Terminate;
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
// GenTreeUseEdgeIterator::AdvanceArrOffset: produces the next operand of a ArrOffset node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceArrOffset()
{
    switch (m_state)
    {
        case 0:
            m_edge  = &m_node->AsArrOffs()->gtIndex;
            m_state = 1;
            break;
        case 1:
            m_edge    = &m_node->AsArrOffs()->gtArrObj;
            m_advance = &GenTreeUseEdgeIterator::Terminate;
            break;
        default:
            unreached();
    }

    assert(*m_edge != nullptr);
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceDynBlk: produces the next operand of a DynBlk node and advances the state.
//
void GenTreeUseEdgeIterator::AdvanceDynBlk()
{
    GenTreeDynBlk* const dynBlock = m_node->AsDynBlk();

    m_edge = dynBlock->gtEvalSizeFirst ? &dynBlock->gtOp1 : &dynBlock->gtDynamicSize;
    assert(*m_edge != nullptr);
    m_advance = &GenTreeUseEdgeIterator::Terminate;
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceStoreDynBlk: produces the next operand of a StoreDynBlk node and advances the state.
//
// These nodes are moderately complicated but rare enough that templating this function is probably not
// worth the extra complexity.
//
void GenTreeUseEdgeIterator::AdvanceStoreDynBlk()
{
    GenTreeDynBlk* const dynBlock = m_node->AsDynBlk();
    if (dynBlock->gtEvalSizeFirst)
    {
        switch (m_state)
        {
            case 0:
                m_edge  = dynBlock->IsReverseOp() ? &dynBlock->gtOp2 : &dynBlock->gtOp1;
                m_state = 1;
                break;
            case 1:
                m_edge    = dynBlock->IsReverseOp() ? &dynBlock->gtOp1 : &dynBlock->gtOp2;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
                break;
            default:
                unreached();
        }
    }
    else
    {
        switch (m_state)
        {
            case 0:
                m_edge  = dynBlock->IsReverseOp() ? &dynBlock->gtOp1 : &dynBlock->gtOp2;
                m_state = 1;
                break;
            case 1:
                m_edge    = &dynBlock->gtDynamicSize;
                m_advance = &GenTreeUseEdgeIterator::Terminate;
                break;
            default:
                unreached();
        }
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

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceList: produces the next operand of a variadic node and advances the state.
//
// This function does not use `m_state` for anything meaningful; it simply walks the `m_argList` until
// there are no further entries.
//
void GenTreeUseEdgeIterator::AdvanceList()
{
    assert(m_state == 0);

    if (m_argList == nullptr)
    {
        m_state = -1;
    }
    else
    {
        GenTreeArgList* listNode = m_argList->AsArgList();
        m_edge                   = &listNode->gtOp1;
        m_argList                = listNode->Rest();
    }
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::SetEntryStateForList: produces the first operand of a list node.
//
void GenTreeUseEdgeIterator::SetEntryStateForList(GenTree* list)
{
    m_argList = list;
    m_advance = &GenTreeUseEdgeIterator::AdvanceList;
    AdvanceList();
}

//------------------------------------------------------------------------
// GenTreeUseEdgeIterator::AdvanceCall: produces the next operand of a call node and advances the state.
//
// This function is a bit tricky: in order to avoid doing unnecessary work, it is instantiated with the
// state number the iterator will be in when it is called. For example, `AdvanceCall<CALL_INSTANCE>`
// is the instantiation used when the iterator is at the `CALL_INSTANCE` state (i.e. the entry state).
// This sort of templating allows each state to avoid processing earlier states without unnecessary
// duplication of code.
//
// Note that this method expands the argument lists (`gtCallArgs` and `gtCallLateArgs`) into their
// component operands.
//
template <int state>
void          GenTreeUseEdgeIterator::AdvanceCall()
{
    GenTreeCall* const call = m_node->AsCall();

    switch (state)
    {
        case CALL_INSTANCE:
            m_argList = call->gtCallArgs;
            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_ARGS>;
            if (call->gtCallObjp != nullptr)
            {
                m_edge = &call->gtCallObjp;
                return;
            }
            __fallthrough;

        case CALL_ARGS:
            if (m_argList != nullptr)
            {
                GenTreeArgList* argNode = m_argList->AsArgList();
                m_edge                  = &argNode->gtOp1;
                m_argList               = argNode->Rest();
                return;
            }
            m_argList = call->gtCallLateArgs;
            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_LATE_ARGS>;
            __fallthrough;

        case CALL_LATE_ARGS:
            if (m_argList != nullptr)
            {
                GenTreeArgList* argNode = m_argList->AsArgList();
                m_edge                  = &argNode->gtOp1;
                m_argList               = argNode->Rest();
                return;
            }
            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_CONTROL_EXPR>;
            __fallthrough;

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
            __fallthrough;

        case CALL_COOKIE:
            assert(call->gtCallType == CT_INDIRECT);

            m_advance = &GenTreeUseEdgeIterator::AdvanceCall<CALL_ADDRESS>;
            if (call->gtCallCookie != nullptr)
            {
                m_edge = &call->gtCallCookie;
                return;
            }
            __fallthrough;

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

#ifdef DEBUG

/* static */ int GenTree::gtDispFlags(unsigned flags, unsigned debugFlags)
{
    int charsDisplayed = 11; // 11 is the "baseline" number of flag characters displayed

#ifdef LEGACY_BACKEND
    printf("%c", (flags & GTF_ASG) ? 'A' : '-');
#else  // !LEGACY_BACKEND
    printf("%c", (flags & GTF_ASG) ? 'A' : (IsContained(flags) ? 'c' : '-'));
#endif // LEGACY_BACKEND
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
    printf("%c", (flags & GTF_LATE_ARG) ? 'L' : '-');
    printf("%c", (flags & GTF_SPILLED) ? 'z' : (flags & GTF_SPILL) ? 'Z' : '-');

    return charsDisplayed;
}

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

    if ((tree->gtOper == GT_CNS_INT) && tree->IsIconHandle())
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

        if (tree->gtCall.gtCallType == CT_USER_FUNC)
        {
            if ((tree->gtFlags & GTF_CALL_VIRT_KIND_MASK) != GTF_CALL_NONVIRT)
            {
                callType = "CALLV";
            }
        }
        else if (tree->gtCall.gtCallType == CT_HELPER)
        {
            ctType = " help";
        }
        else if (tree->gtCall.gtCallType == CT_INDIRECT)
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
        if (tree->gtFlags & GTF_CALL_VIRT_VTABLE)
        {
            gtfType = " ind";
        }
        else if (tree->gtFlags & GTF_CALL_VIRT_STUB)
        {
            gtfType = " stub";
        }
#ifdef FEATURE_READYTORUN_COMPILER
        else if (tree->gtCall.IsR2RRelativeIndir())
        {
            gtfType = " r2r_ind";
        }
#endif // FEATURE_READYTORUN_COMPILER
        else if (tree->gtFlags & GTF_CALL_UNMANAGED)
        {
            char* gtfTypeBufWalk = gtfTypeBuf;
            gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " unman");
            if (tree->gtFlags & GTF_CALL_POP_ARGS)
            {
                gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " popargs");
            }
            if (tree->gtCall.gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL)
            {
                gtfTypeBufWalk += SimpleSprintf_s(gtfTypeBufWalk, gtfTypeBuf, sizeof(gtfTypeBuf), " thiscall");
            }
            gtfType = gtfTypeBuf;
        }

        sprintf_s(bufp, sizeof(buf), " %s%s%s%c", callType, ctType, gtfType, 0);
    }
    else if (tree->gtOper == GT_ARR_ELEM)
    {
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s[", name);
        for (unsigned rank = tree->gtArrElem.gtArrRank - 1; rank; rank--)
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), ",");
        }
        SimpleSprintf_s(bufp, buf, sizeof(buf), "]");
    }
    else if (tree->gtOper == GT_ARR_OFFSET || tree->gtOper == GT_ARR_INDEX)
    {
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s[", name);
        unsigned char currDim;
        unsigned char rank;
        if (tree->gtOper == GT_ARR_OFFSET)
        {
            currDim = tree->gtArrOffs.gtCurrDim;
            rank    = tree->gtArrOffs.gtArrRank;
        }
        else
        {
            currDim = tree->gtArrIndex.gtCurrDim;
            rank    = tree->gtArrIndex.gtArrRank;
        }

        for (unsigned char dim = 0; dim < rank; dim++)
        {
            // Use a defacto standard i,j,k for the dimensions.
            // Note that we only support up to rank 3 arrays with these nodes, so we won't run out of characters.
            char dimChar = '*';
            if (dim == currDim)
            {
                dimChar = 'i' + dim;
            }
            else if (dim > currDim)
            {
                dimChar = ' ';
            }

            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "%c", dimChar);
            if (dim != rank - 1)
            {
                bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), ",");
            }
        }
        SimpleSprintf_s(bufp, buf, sizeof(buf), "]");
    }
    else if (tree->gtOper == GT_LEA)
    {
        GenTreeAddrMode* lea = tree->AsAddrMode();
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), " %s(", name);
        if (lea->Base() != nullptr)
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "b+");
        }
        if (lea->Index() != nullptr)
        {
            bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "(i*%d)+", lea->gtScale);
        }
        bufp += SimpleSprintf_s(bufp, buf, sizeof(buf), "%d)", lea->Offset());
    }
    else if (tree->gtOper == GT_ARR_BOUNDS_CHECK)
    {
        switch (tree->gtBoundsChk.gtThrowKind)
        {
            case SCK_RNGCHK_FAIL:
                sprintf_s(bufp, sizeof(buf), " %s_Rng", name);
                break;
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
    else if (tree->OperIsBlk() && !tree->OperIsDynBlk())
    {
        sprintf_s(bufp, sizeof(buf), " %s(%d)", name, tree->AsBlk()->gtBlkSize);
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

void Compiler::gtDispNode(GenTreePtr tree, IndentStack* indentStack, __in __in_z __in_opt const char* msg, bool isLIR)
{
    bool printPointer = true; // always true..
    bool printFlags   = true; // always true..
    bool printCost    = true; // always true..

    int msgLength = 25;

    GenTree* prev;

    if (tree->gtSeqNum)
    {
        printf("N%03u ", tree->gtSeqNum);
        if (tree->gtCostsInitialized)
        {
            printf("(%3u,%3u) ", tree->gtCostEx, tree->gtCostSz);
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
        if (tree->gtOper == GT_STMT)
        {
            prev = tree->gtStmt.gtStmtExpr;
        }
        else
        {
            prev = tree;
        }

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
        bool displayDotNum = tree->gtOper != GT_STMT && hasSeqNum && (indentStack == nullptr);
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
            printf("(%3u,%3u) ", tree->gtCostEx, tree->gtCostSz);
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
            printf("CSE #%02d (%s)", GET_CSE_INDEX(tree->gtCSEnum), (IS_CSE_USE(tree->gtCSEnum) ? "use" : "def"));
        }
        else
        {
            printf("             ");
        }
    }

    /* Print the node ID */
    printTreeID(tree);
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
            case GT_LEA:
            case GT_BLK:
            case GT_OBJ:
            case GT_DYN_BLK:
            case GT_STORE_BLK:
            case GT_STORE_OBJ:
            case GT_STORE_DYN_BLK:

            case GT_IND:
                // We prefer printing V or U
                if ((tree->gtFlags & (GTF_IND_VOLATILE | GTF_IND_UNALIGNED)) == 0)
                {
                    if (tree->gtFlags & GTF_IND_TGTANYWHERE)
                    {
                        printf("*");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_INVARIANT)
                    {
                        printf("#");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_ARR_INDEX)
                    {
                        printf("a");
                        --msgLength;
                        break;
                    }
                    if (tree->gtFlags & GTF_IND_NONFAULTING)
                    {
                        printf("x");
                        --msgLength;
                        break;
                    }
                }
                __fallthrough;

            case GT_INDEX:
            case GT_INDEX_ADDR:

                if ((tree->gtFlags & (GTF_IND_VOLATILE | GTF_IND_UNALIGNED)) == 0) // We prefer printing V or U over R
                {
                    if (tree->gtFlags & GTF_INX_REFARR_LAYOUT)
                    {
                        printf("R");
                        --msgLength;
                        break;
                    } // R means RefArray
                }
                __fallthrough;

            case GT_FIELD:
            case GT_CLS_VAR:
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

            case GT_ASG:
                if (tree->OperIsInitBlkOp())
                {
                    printf("I");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_CALL:
                if (tree->gtFlags & GTF_CALL_INLINE_CANDIDATE)
                {
                    printf("I");
                    --msgLength;
                    break;
                }
                if (tree->gtCall.gtCallMoreFlags & GTF_CALL_M_RETBUFFARG)
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
#if !defined(_TARGET_64BIT_) && !defined(LEGACY_BACKEND)
            case GT_MUL_LONG:
#endif
                if (tree->gtFlags & GTF_MUL_64RSLT)
                {
                    printf("L");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_ADDR:
                if (tree->gtFlags & GTF_ADDR_ONSTACK)
                {
                    printf("L");
                    --msgLength;
                    break;
                } // L means LclVar
                goto DASH;

            case GT_LCL_FLD:
            case GT_LCL_VAR:
            case GT_LCL_VAR_ADDR:
            case GT_LCL_FLD_ADDR:
            case GT_STORE_LCL_FLD:
            case GT_STORE_LCL_VAR:
            case GT_REG_VAR:
                if (tree->gtFlags & GTF_VAR_USEASG)
                {
                    printf("U");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_DEF)
                {
                    printf("D");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_CAST)
                {
                    printf("C");
                    --msgLength;
                    break;
                }
                if (tree->gtFlags & GTF_VAR_ARR_INDEX)
                {
                    printf("i");
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
#ifndef LEGACY_BACKEND
            case GT_TEST_EQ:
            case GT_TEST_NE:
#endif
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
                if (tree->gtFlags & GTF_RELOP_QMARK)
                {
                    printf("Q");
                    --msgLength;
                    break;
                }
                goto DASH;

            case GT_JCMP:
                printf((tree->gtFlags & GTF_JCMP_TST) ? "T" : "C");
                printf((tree->gtFlags & GTF_JCMP_EQ) ? "EQ" : "NE");
                goto DASH;

            case GT_FIELD_LIST:
                if (tree->gtFlags & GTF_FIELD_LIST_HEAD)
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
        unsigned flags = tree->gtFlags;

        if (tree->OperIsBinary())
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
        else // !tree->OperIsBinary()
        {
            // the GTF_REVERSE flag only applies to binary operations
            flags &= ~GTF_REVERSE_OPS; // we use this value for GTF_VAR_ARR_INDEX above
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
    printf("%c", (flags & GTF_SPILLED       ) ? 'z' : '-');
    printf("%c", (flags & GTF_SPILL         ) ? 'Z' : '-');
*/

#if FEATURE_STACK_FP_X87
        BYTE fpLvl = (BYTE)tree->gtFPlvl;
        if (IsUninitialized(fpLvl) || fpLvl == 0x00)
        {
            printf("-");
        }
        else
        {
            printf("%1u", tree->gtFPlvl);
        }
#endif // FEATURE_STACK_FP_X87
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
            msg                    = reinterpret_cast<char*>(alloca(bufLength * sizeof(char)));
            sprintf_s(const_cast<char*>(msg), bufLength, "t%d = %s", tree->gtTreeID, hasOperands ? "" : " ");
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
            if (tree->gtOper == GT_LCL_VAR || tree->gtOper == GT_STORE_LCL_VAR)
            {
                LclVarDsc* varDsc = &lvaTable[tree->gtLclVarCommon.gtLclNum];
                if (varDsc->lvAddrExposed)
                {
                    printf("(AX)"); // Variable has address exposed.
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
                        assert(fgGlobalMorph);
                        printf("(P?!)"); // Promoted struct
                    }
                }
            }

            if (tree->gtOper == GT_STMT)
            {
                if (opts.compDbgInfo)
                {
                    IL_OFFSET endIL = tree->gtStmt.gtStmtLastILoffs;

                    printf("(IL ");
                    if (tree->gtStmt.gtStmtILoffsx == BAD_IL_OFFSET)
                    {
                        printf("  ???");
                    }
                    else
                    {
                        printf("0x%03X", jitGetILoffs(tree->gtStmt.gtStmtILoffsx));
                    }
                    printf("...");
                    if (endIL == BAD_IL_OFFSET)
                    {
                        printf("  ???");
                    }
                    else
                    {
                        printf("0x%03X", endIL);
                    }
                    printf(")");
                }
            }

            if (tree->IsArgPlaceHolderNode() && (tree->gtArgPlace.gtArgPlaceClsHnd != nullptr))
            {
                printf(" => [clsHnd=%08X]", dspPtr(tree->gtArgPlace.gtArgPlaceClsHnd));
            }

            if (tree->gtOper == GT_RUNTIMELOOKUP)
            {
#ifdef _TARGET_64BIT_
                printf(" 0x%llx", dspPtr(tree->gtRuntimeLookup.gtHnd));
#else
                printf(" 0x%x", dspPtr(tree->gtRuntimeLookup.gtHnd));
#endif

                switch (tree->gtRuntimeLookup.gtHndType)
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
        }

        // for tracking down problems in reguse prediction or liveness tracking

        if (verbose && 0)
        {
            printf(" RR=");
            dspRegMask(tree->gtRsvdRegs);
#ifdef LEGACY_BACKEND
            printf(",UR=");
            dspRegMask(tree->gtUsedRegs);
#endif // LEGACY_BACKEND
            printf("\n");
        }
    }
}

void Compiler::gtDispRegVal(GenTree* tree)
{
    switch (tree->GetRegTag())
    {
        // Don't display NOREG; the absence of this tag will imply this state
        // case GenTree::GT_REGTAG_NONE:       printf(" NOREG");   break;

        case GenTree::GT_REGTAG_REG:
            printf(" REG %s", compRegVarName(tree->gtRegNum));
            break;

#if CPU_LONG_USES_REGPAIR
        case GenTree::GT_REGTAG_REGPAIR:
            printf(" PAIR %s", compRegPairName(tree->gtRegPair));
            break;
#endif

        default:
            break;
    }

    if (tree->IsMultiRegCall())
    {
        // 0th reg is gtRegNum, which is already printed above.
        // Print the remaining regs of a multi-reg call node.
        GenTreeCall* call     = tree->AsCall();
        unsigned     regCount = call->GetReturnTypeDesc()->TryGetReturnRegCount();
        for (unsigned i = 1; i < regCount; ++i)
        {
            printf(",%s", compRegVarName(call->GetRegNumByIdx(i)));
        }
    }
    else if (tree->IsCopyOrReloadOfMultiRegCall())
    {
        GenTreeCopyOrReload* copyOrReload = tree->AsCopyOrReload();
        GenTreeCall*         call         = tree->gtGetOp1()->AsCall();
        unsigned             regCount     = call->GetReturnTypeDesc()->TryGetReturnRegCount();
        for (unsigned i = 1; i < regCount; ++i)
        {
            printf(",%s", compRegVarName(copyOrReload->GetRegNumByIdx(i)));
        }
    }

#if FEATURE_MULTIREG_RET
    if (tree->IsCopyOrReload())
    {
        for (int i = 1; i < MAX_RET_REG_COUNT; i++)
        {
            regNumber reg = (regNumber)tree->AsCopyOrReload()->GetRegNumByIdx(i);
            if (reg == REG_NA)
            {
                break;
            }
            printf(",%s", compRegVarName(reg));
        }
    }
#endif

#if !defined(LEGACY_BACKEND) && defined(_TARGET_ARM_)
    if (tree->OperIsMultiRegOp() && (tree->AsMultiRegOp()->gtOtherReg != REG_NA))
    {
        printf(",%s", compRegVarName(tree->AsMultiRegOp()->gtOtherReg));
    }
#endif

#ifdef LEGACY_BACKEND
    if (tree->InReg())
    {
        printf(" RV");
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
#if FEATURE_ANYCSE
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
#endif // FEATURE_ANYCSE
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
#if FEATURE_FIXED_OUT_ARGS
            else if (lclNum == lvaPInvokeFrameRegSaveVar)
            {
                ilName = "PInvokeFrameRegSave";
            }
            else if (lclNum == lvaOutgoingArgSpaceVar)
            {
                ilName = "OutArgs";
            }
#endif // FEATURE_FIXED_OUT_ARGS
#ifdef _TARGET_ARM_
            else if (lclNum == lvaPromotedStructAssemblyScratchVar)
            {
                ilName = "PromotedStructScratch";
            }
#endif // _TARGET_ARM_
#if !FEATURE_EH_FUNCLETS
            else if (lclNum == lvaShadowSPslotsVar)
            {
                ilName = "EHSlots";
            }
#endif // !FEATURE_EH_FUNCLETS
            else if (lclNum == lvaLocAllocSPvar)
            {
                ilName = "LocAllocSP";
            }
#if FEATURE_EH_FUNCLETS
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

    Compiler::gtGetLclVarNameInfo(lclNum, &ilKind, &ilName, &ilNum);

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
    int  charsPrinted = gtGetLclVarName(lclNum, buf, sizeof(buf) / sizeof(buf[0]));
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
    int  charsPrinted = gtGetLclVarName(lclNum, buf, sizeof(buf) / sizeof(buf[0]));

    if (charsPrinted < 0)
    {
        return;
    }

    printf("%s", buf);

    if (padForBiggestDisp && (charsPrinted < LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH))
    {
        printf("%*c", LONGEST_COMMON_LCL_VAR_DISPLAY_LENGTH - charsPrinted, ' ');
    }
}

/*****************************************************************************/
void Compiler::gtDispConst(GenTree* tree)
{
    assert(tree->OperKind() & GTK_CONST);

    switch (tree->gtOper)
    {
        case GT_CNS_INT:
            if (tree->IsIconHandle(GTF_ICON_STR_HDL))
            {
                const wchar_t* str = eeGetCPString(tree->gtIntCon.gtIconVal);
                if (str != nullptr)
                {
                    printf(" 0x%X \"%S\"", dspPtr(tree->gtIntCon.gtIconVal), str);
                }
                else
                {
                    // Note that eGetCPString isn't currently implemented on Linux/ARM
                    // and instead always returns nullptr
                    printf(" 0x%X [ICON_STR_HDL]", dspPtr(tree->gtIntCon.gtIconVal));
                }
            }
            else
            {
                ssize_t dspIconVal = tree->IsIconHandle() ? dspPtr(tree->gtIntCon.gtIconVal) : tree->gtIntCon.gtIconVal;

                if (tree->TypeGet() == TYP_REF)
                {
                    assert(tree->gtIntCon.gtIconVal == 0);
                    printf(" null");
                }
                else if ((tree->gtIntCon.gtIconVal > -1000) && (tree->gtIntCon.gtIconVal < 1000))
                {
                    printf(" %ld", dspIconVal);
#ifdef _TARGET_64BIT_
                }
                else if ((tree->gtIntCon.gtIconVal & 0xFFFFFFFF00000000LL) != 0)
                {
                    printf(" 0x%llx", dspIconVal);
#endif
                }
                else
                {
                    printf(" 0x%X", dspIconVal);
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
                        case GTF_ICON_STR_HDL:
                            unreached(); // This case is handled above
                            break;
                        case GTF_ICON_PSTR_HDL:
                            printf(" pstr");
                            break;
                        case GTF_ICON_PTR_HDL:
                            printf(" ptr");
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
                            printf(" cid");
                            break;
                        case GTF_ICON_BBC_PTR:
                            printf(" bbc");
                            break;
                        default:
                            printf(" UNKNOWN");
                            break;
                    }
                }

                if ((tree->gtFlags & GTF_ICON_FIELD_OFF) != 0)
                {
                    printf(" field offset");
                }

#ifdef FEATURE_SIMD
                if ((tree->gtFlags & GTF_ICON_SIMD_COUNT) != 0)
                {
                    printf(" Vector<T>.Count");
                }
#endif

                if ((tree->IsReuseRegVal()) != 0)
                {
                    printf(" reuse reg val");
                }
            }

            gtDispFieldSeq(tree->gtIntCon.gtFieldSeq);

            break;

        case GT_CNS_LNG:
            printf(" 0x%016I64x", tree->gtLngCon.gtLconVal);
            break;

        case GT_CNS_DBL:
            if (*((__int64*)&tree->gtDblCon.gtDconVal) == (__int64)I64(0x8000000000000000))
            {
                printf(" -0.00000");
            }
            else
            {
                printf(" %#.17g", tree->gtDblCon.gtDconVal);
            }
            break;
        case GT_CNS_STR:
            printf("<string constant>");
            break;
        default:
            assert(!"unexpected constant node");
    }

    gtDispRegVal(tree);
}

void Compiler::gtDispFieldSeq(FieldSeqNode* pfsn)
{
    if (pfsn == FieldSeqStore::NotAField() || (pfsn == nullptr))
    {
        return;
    }

    // Otherwise...
    printf(" Fseq[");
    while (pfsn != nullptr)
    {
        assert(pfsn != FieldSeqStore::NotAField()); // Can't exist in a field sequence list except alone
        CORINFO_FIELD_HANDLE fldHnd = pfsn->m_fieldHnd;
        // First check the "pseudo" field handles...
        if (fldHnd == FieldSeqStore::FirstElemPseudoField)
        {
            printf("#FirstElem");
        }
        else if (fldHnd == FieldSeqStore::ConstantIndexPseudoField)
        {
            printf("#ConstantIndex");
        }
        else
        {
            printf("%s", eeGetFieldName(fldHnd));
        }
        pfsn = pfsn->m_next;
        if (pfsn != nullptr)
        {
            printf(", ");
        }
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
    if (tree->OperKind() & GTK_CONST)
    {
        gtDispConst(tree);
        return;
    }

    bool isLclFld = false;

    switch (tree->gtOper)
    {
        unsigned   varNum;
        LclVarDsc* varDsc;

        case GT_LCL_FLD:
        case GT_LCL_FLD_ADDR:
        case GT_STORE_LCL_FLD:
            isLclFld = true;
            __fallthrough;

        case GT_PHI_ARG:
        case GT_LCL_VAR:
        case GT_LCL_VAR_ADDR:
        case GT_STORE_LCL_VAR:
            printf(" ");
            varNum = tree->gtLclVarCommon.gtLclNum;
            varDsc = &lvaTable[varNum];
            gtDispLclVar(varNum);
            if (tree->gtLclVarCommon.HasSsaName())
            {
                if (tree->gtFlags & GTF_VAR_USEASG)
                {
                    assert(tree->gtFlags & GTF_VAR_DEF);
                    printf("ud:%d->%d", tree->gtLclVarCommon.gtSsaNum, GetSsaNumForLocalVarDef(tree));
                }
                else
                {
                    printf("%s:%d", (tree->gtFlags & GTF_VAR_DEF) ? "d" : "u", tree->gtLclVarCommon.gtSsaNum);
                }
            }

            if (isLclFld)
            {
                printf("[+%u]", tree->gtLclFld.gtLclOffs);
                gtDispFieldSeq(tree->gtLclFld.gtFieldSeq);
            }

            if (varDsc->lvRegister)
            {
                printf(" ");
                varDsc->PrintVarReg();
            }
#ifndef LEGACY_BACKEND
            else if (tree->InReg())
            {
#if CPU_LONG_USES_REGPAIR
                if (isRegPairType(tree->TypeGet()))
                    printf(" %s", compRegPairName(tree->gtRegPair));
                else
#endif
                    printf(" %s", compRegVarName(tree->gtRegNum));
            }
#endif // !LEGACY_BACKEND

            if (varDsc->lvPromoted)
            {
                if (!varTypeIsPromotable(varDsc) && !varDsc->lvUnusedStruct)
                {
                    // Promoted implicit byrefs can get in this state while they are being rewritten
                    // in global morph.
                    assert(fgGlobalMorph);
                }
                else
                {
                    CORINFO_CLASS_HANDLE typeHnd = varDsc->lvVerTypeInfo.GetClassHandle();
                    CORINFO_FIELD_HANDLE fldHnd;

                    for (unsigned i = varDsc->lvFieldLclStart; i < varDsc->lvFieldLclStart + varDsc->lvFieldCnt; ++i)
                    {
                        LclVarDsc*  fieldVarDsc = &lvaTable[i];
                        const char* fieldName;
#if !defined(_TARGET_64BIT_)
                        if (varTypeIsLong(varDsc))
                        {
                            fieldName = (i == 0) ? "lo" : "hi";
                        }
                        else
#endif // !defined(_TARGET_64BIT_)
                        {
                            fldHnd    = info.compCompHnd->getFieldInClass(typeHnd, fieldVarDsc->lvFldOrdinal);
                            fieldName = eeGetFieldName(fldHnd);
                        }

                        printf("\n");
                        printf("                                                  ");
                        printIndent(indentStack);
                        printf("    %-6s V%02u.%s (offs=0x%02x) -> ", varTypeName(fieldVarDsc->TypeGet()),
                               tree->gtLclVarCommon.gtLclNum, fieldName, fieldVarDsc->lvFldOffset);
                        gtDispLclVar(i);

                        if (fieldVarDsc->lvRegister)
                        {
                            printf(" ");
                            fieldVarDsc->PrintVarReg();
                        }

                        if (fieldVarDsc->lvTracked && fgLocalVarLivenessDone && // Includes local variable liveness
                            ((tree->gtFlags & GTF_VAR_DEATH) != 0))
                        {
                            printf(" (last use)");
                        }
                    }
                }
            }
            else // a normal not-promoted lclvar
            {
                if (varDsc->lvTracked && fgLocalVarLivenessDone && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
                {
                    printf(" (last use)");
                }
            }
            break;

        case GT_REG_VAR:
            printf(" ");
            gtDispLclVar(tree->gtRegVar.gtLclNum);
            if (isFloatRegType(tree->gtType))
            {
                assert(tree->gtRegVar.gtRegNum == tree->gtRegNum);
                printf(" FPV%u", tree->gtRegNum);
            }
            else
            {
                printf(" %s", compRegVarName(tree->gtRegVar.gtRegNum));
            }

            varNum = tree->gtRegVar.gtLclNum;
            varDsc = &lvaTable[varNum];

            if (varDsc->lvTracked && fgLocalVarLivenessDone && ((tree->gtFlags & GTF_VAR_DEATH) != 0))
            {
                printf(" (last use)");
            }

            break;

        case GT_JMP:
        {
            const char* methodName;
            const char* className;

            methodName = eeGetMethodName((CORINFO_METHOD_HANDLE)tree->gtVal.gtVal1, &className);
            printf(" %s.%s\n", className, methodName);
        }
        break;

        case GT_CLS_VAR:
            printf(" Hnd=%#x", dspPtr(tree->gtClsVar.gtClsVarHnd));
            gtDispFieldSeq(tree->gtClsVar.gtFieldSeq);
            break;

        case GT_CLS_VAR_ADDR:
            printf(" Hnd=%#x", dspPtr(tree->gtClsVar.gtClsVarHnd));
            break;

        case GT_LABEL:
            if (tree->gtLabel.gtLabBB)
            {
                printf(" dst=BB%02u", tree->gtLabel.gtLabBB->bbNum);
            }
            else
            {
                printf(" dst=<null>");
            }

            break;

        case GT_FTN_ADDR:
        {
            const char* methodName;
            const char* className;

            methodName = eeGetMethodName((CORINFO_METHOD_HANDLE)tree->gtFptrVal.gtFptrMethod, &className);
            printf(" %s.%s\n", className, methodName);
        }
        break;

#if !FEATURE_EH_FUNCLETS
        case GT_END_LFIN:
            printf(" endNstLvl=%d", tree->gtVal.gtVal1);
            break;
#endif // !FEATURE_EH_FUNCLETS

        // Vanilla leaves. No qualifying information available. So do nothing

        case GT_NO_OP:
        case GT_START_NONGC:
        case GT_PROF_HOOK:
        case GT_CATCH_ARG:
        case GT_MEMORYBARRIER:
        case GT_ARGPLACE:
        case GT_PINVOKE_PROLOG:
#ifndef LEGACY_BACKEND
        case GT_JMPTABLE:
#endif // !LEGACY_BACKEND
            break;

        case GT_RET_EXPR:
            printf("(inl return from call ");
            printTreeID(tree->gtRetExpr.gtInlineCandidate);
            printf(")");
            break;

        case GT_PHYSREG:
            printf(" %s", getRegName(tree->gtPhysReg.gtSrcReg, varTypeIsFloating(tree)));
            break;

        case GT_IL_OFFSET:
            printf(" IL offset: ");
            if (tree->gtStmt.gtStmtILoffsx == BAD_IL_OFFSET)
            {
                printf("???");
            }
            else
            {
                printf("0x%x", jitGetILoffs(tree->gtStmt.gtStmtILoffsx));
            }
            break;

        case GT_JCC:
        case GT_SETCC:
            printf(" cond=%s", GenTree::OpName(tree->AsCC()->gtCondition));
            break;
        case GT_JCMP:
            printf(" cond=%s%s", (tree->gtFlags & GTF_JCMP_TST) ? "TEST_" : "",
                   (tree->gtFlags & GTF_JCMP_EQ) ? "EQ" : "NE");

        default:
            assert(!"don't know how to display tree leaf node");
    }

    gtDispRegVal(tree);
}

//------------------------------------------------------------------------
// gtDispLeaf: Print a child node to jitstdout.
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

void Compiler::gtDispChild(GenTreePtr           child,
                           IndentStack*         indentStack,
                           IndentInfo           arcType,
                           __in_opt const char* msg,     /* = nullptr  */
                           bool                 topOnly) /* = false */
{
    IndentInfo info;
    indentStack->Push(arcType);
    gtDispTree(child, indentStack, msg, topOnly);
    indentStack->Pop();
}

#ifdef FEATURE_SIMD
// Intrinsic Id to name map
extern const char* const simdIntrinsicNames[] = {
#define SIMD_INTRINSIC(mname, inst, id, name, r, ac, arg1, arg2, arg3, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10) name,
#include "simdintrinsiclist.h"
};
#endif // FEATURE_SIMD

/*****************************************************************************/

void Compiler::gtDispTree(GenTreePtr   tree,
                          IndentStack* indentStack,                 /* = nullptr */
                          __in __in_z __in_opt const char* msg,     /* = nullptr  */
                          bool                             topOnly, /* = false */
                          bool                             isLIR)   /* = false */
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
        /* Value used to initalize nodes */
        printf("Uninitialized tree node!");
        return;
    }

    if (tree->gtOper >= GT_COUNT)
    {
        gtDispNode(tree, indentStack, msg, isLIR);
        printf("Bogus operator!");
        return;
    }

    /* Is tree a leaf node? */

    if (tree->OperIsLeaf() || tree->OperIsLocalStore()) // local stores used to be leaves
    {
        gtDispNode(tree, indentStack, msg, isLIR);
        gtDispLeaf(tree, indentStack);
        gtDispVN(tree);
        printf("\n");
        if (tree->OperIsLocalStore() && !topOnly)
        {
            gtDispChild(tree->gtOp.gtOp1, indentStack, IINone);
        }
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
            case IIEmbedded:
                indentStack->Push(IIEmbedded);
                lowerArc = IIEmbedded;
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

    // Special case formatting for PHI nodes -- arg lists like calls.

    if (tree->OperGet() == GT_PHI)
    {
        gtDispNode(tree, indentStack, msg, isLIR);
        gtDispVN(tree);
        printf("\n");

        if (!topOnly)
        {
            if (tree->gtOp.gtOp1 != nullptr)
            {
                IndentInfo arcType = IIArcTop;
                for (GenTreeArgList* args = tree->gtOp.gtOp1->AsArgList(); args != nullptr; args = args->Rest())
                {
                    if (args->Rest() == nullptr)
                    {
                        arcType = IIArcBottom;
                    }
                    gtDispChild(args->Current(), indentStack, arcType);
                    arcType = IIArc;
                }
            }
        }
        return;
    }

    /* Is it a 'simple' unary/binary operator? */

    const char* childMsg = nullptr;

    if (tree->OperIsSimple())
    {
        if (!topOnly)
        {
            if (tree->gtGetOp2IfPresent())
            {
                // Label the childMsgs of the GT_COLON operator
                // op2 is the then part

                if (tree->gtOper == GT_COLON)
                {
                    childMsg = "then";
                }
                gtDispChild(tree->gtOp.gtOp2, indentStack, IIArcTop, childMsg, topOnly);
            }
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

        if (tree->gtOper == GT_CAST)
        {
            /* Format a message that explains the effect of this GT_CAST */

            var_types fromType  = genActualType(tree->gtCast.CastOp()->TypeGet());
            var_types toType    = tree->CastToType();
            var_types finalType = tree->TypeGet();

            /* if GTF_UNSIGNED is set then force fromType to an unsigned type */
            if (tree->gtFlags & GTF_UNSIGNED)
            {
                fromType = genUnsignedType(fromType);
            }

            if (finalType != toType)
            {
                printf(" %s <-", varTypeName(finalType));
            }

            printf(" %s <- %s", varTypeName(toType), varTypeName(fromType));
        }

        if (tree->gtOper == GT_OBJ && (tree->gtFlags & GTF_VAR_DEATH))
        {
            printf(" (last use)");
        }
        if (tree->OperIsBlkOp())
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
                    case GenTreeBlk::BlkOpKindRepInstr:
                        printf(" (RepInstr)");
                        break;
                    case GenTreeBlk::BlkOpKindUnroll:
                        printf(" (Unroll)");
                        break;
                    case GenTreeBlk::BlkOpKindHelper:
                        printf(" (Helper)");
                        break;
                    default:
                        unreached();
                }
            }
        }
        else if (tree->OperIsFieldList())
        {
            printf(" %s at offset %d", varTypeName(tree->AsFieldList()->gtFieldType),
                   tree->AsFieldList()->gtFieldOffset);
        }
#if FEATURE_PUT_STRUCT_ARG_STK
        else if (tree->OperGet() == GT_PUTARG_STK)
        {
            printf(" (%d slots)", tree->AsPutArgStk()->gtNumSlots);
            if (tree->AsPutArgStk()->gtPutArgStkKind != GenTreePutArgStk::Kind::Invalid)
            {
                switch (tree->AsPutArgStk()->gtPutArgStkKind)
                {
                    case GenTreePutArgStk::Kind::RepInstr:
                        printf(" (RepInstr)");
                        break;
                    case GenTreePutArgStk::Kind::Unroll:
                        printf(" (Unroll)");
                        break;
                    case GenTreePutArgStk::Kind::Push:
                        printf(" (Push)");
                        break;
                    case GenTreePutArgStk::Kind::PushAllSlots:
                        printf(" (PushAllSlots)");
                        break;
                    default:
                        unreached();
                }
            }
        }
#endif // FEATURE_PUT_STRUCT_ARG_STK

        IndirectAssignmentAnnotation* pIndirAnnote;
        if (tree->gtOper == GT_ASG && GetIndirAssignMap()->Lookup(tree, &pIndirAnnote))
        {
            printf("  indir assign of V%02d:", pIndirAnnote->m_lclNum);
            if (pIndirAnnote->m_isEntire)
            {
                printf("d:%d", pIndirAnnote->m_defSsaNum);
            }
            else
            {
                printf("ud:%d->%d", pIndirAnnote->m_useSsaNum, pIndirAnnote->m_defSsaNum);
            }
        }

        if (tree->gtOper == GT_INTRINSIC)
        {
            switch (tree->gtIntrinsic.gtIntrinsicId)
            {
                case CORINFO_INTRINSIC_Sin:
                    printf(" sin");
                    break;
                case CORINFO_INTRINSIC_Cos:
                    printf(" cos");
                    break;
                case CORINFO_INTRINSIC_Sqrt:
                    printf(" sqrt");
                    break;
                case CORINFO_INTRINSIC_Abs:
                    printf(" abs");
                    break;
                case CORINFO_INTRINSIC_Round:
                    printf(" round");
                    break;
                case CORINFO_INTRINSIC_Cosh:
                    printf(" cosh");
                    break;
                case CORINFO_INTRINSIC_Sinh:
                    printf(" sinh");
                    break;
                case CORINFO_INTRINSIC_Tan:
                    printf(" tan");
                    break;
                case CORINFO_INTRINSIC_Tanh:
                    printf(" tanh");
                    break;
                case CORINFO_INTRINSIC_Asin:
                    printf(" asin");
                    break;
                case CORINFO_INTRINSIC_Acos:
                    printf(" acos");
                    break;
                case CORINFO_INTRINSIC_Atan:
                    printf(" atan");
                    break;
                case CORINFO_INTRINSIC_Atan2:
                    printf(" atan2");
                    break;
                case CORINFO_INTRINSIC_Log10:
                    printf(" log10");
                    break;
                case CORINFO_INTRINSIC_Pow:
                    printf(" pow");
                    break;
                case CORINFO_INTRINSIC_Exp:
                    printf(" exp");
                    break;
                case CORINFO_INTRINSIC_Ceiling:
                    printf(" ceiling");
                    break;
                case CORINFO_INTRINSIC_Floor:
                    printf(" floor");
                    break;
                case CORINFO_INTRINSIC_Object_GetType:
                    printf(" objGetType");
                    break;

                default:
                    unreached();
            }
        }

#ifdef FEATURE_SIMD
        if (tree->gtOper == GT_SIMD)
        {
            printf(" %s %s", varTypeName(tree->gtSIMD.gtSIMDBaseType),
                   simdIntrinsicNames[tree->gtSIMD.gtSIMDIntrinsicID]);
        }
#endif // FEATURE_SIMD

        gtDispRegVal(tree);
        gtDispVN(tree);
        printf("\n");

        if (!topOnly && tree->gtOp.gtOp1)
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
            gtDispChild(tree->gtOp.gtOp1, indentStack, IIArcBottom, childMsg, topOnly);
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
        case GT_FIELD:
            printf(" %s", eeGetFieldName(tree->gtField.gtFldHnd), 0);

            if (tree->gtField.gtFldObj && !topOnly)
            {
                gtDispVN(tree);
                printf("\n");
                gtDispChild(tree->gtField.gtFldObj, indentStack, IIArcBottom);
            }
            else
            {
                gtDispRegVal(tree);
                gtDispVN(tree);
                printf("\n");
            }
            break;

        case GT_CALL:
        {
            GenTreeCall* call = tree->AsCall();
            assert(call->gtFlags & GTF_CALL);
            unsigned numChildren = call->NumChildren();
            GenTree* lastChild   = nullptr;
            if (numChildren != 0)
            {
                lastChild = call->GetChild(numChildren - 1);
            }

            if (call->gtCallType != CT_INDIRECT)
            {
                const char* methodName;
                const char* className;

                methodName = eeGetMethodName(call->gtCallMethHnd, &className);

                printf(" %s.%s", className, methodName);
            }

            if ((call->gtFlags & GTF_CALL_UNMANAGED) && (call->gtCallMoreFlags & GTF_CALL_M_FRAME_VAR_DEATH))
            {
                printf(" (FramesRoot last use)");
            }

            if (((call->gtFlags & GTF_CALL_INLINE_CANDIDATE) != 0) && (call->gtInlineCandidateInfo != nullptr) &&
                (call->gtInlineCandidateInfo->exactContextHnd != nullptr))
            {
                printf(" (exactContextHnd=0x%p)", dspPtr(call->gtInlineCandidateInfo->exactContextHnd));
            }

            gtDispVN(call);
            if (call->IsMultiRegCall())
            {
                gtDispRegVal(call);
            }
            printf("\n");

            if (!topOnly)
            {
                char  buf[64];
                char* bufp;

                bufp = &buf[0];

                if ((call->gtCallObjp != nullptr) && (call->gtCallObjp->gtOper != GT_NOP) &&
                    (!call->gtCallObjp->IsArgPlaceHolderNode()))
                {
                    if (call->gtCallObjp->gtOper == GT_ASG)
                    {
                        sprintf_s(bufp, sizeof(buf), "this SETUP%c", 0);
                    }
                    else
                    {
                        sprintf_s(bufp, sizeof(buf), "this in %s%c", compRegVarName(REG_ARG_0), 0);
                    }
                    gtDispChild(call->gtCallObjp, indentStack, (call->gtCallObjp == lastChild) ? IIArcBottom : IIArc,
                                bufp, topOnly);
                }

                if (call->gtCallArgs)
                {
                    gtDispArgList(call, indentStack);
                }

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

#if !FEATURE_FIXED_OUT_ARGS
                regList list = call->regArgList;
#endif
                /* process the late argument list */
                int lateArgIndex = 0;
                for (GenTreeArgList* lateArgs = call->gtCallLateArgs; lateArgs;
                     (lateArgIndex++, lateArgs = lateArgs->Rest()))
                {
                    GenTreePtr argx;

                    argx = lateArgs->Current();

                    IndentInfo arcType = (lateArgs->Rest() == nullptr) ? IIArcBottom : IIArc;
                    gtGetLateArgMsg(call, argx, lateArgIndex, -1, bufp, sizeof(buf));
                    gtDispChild(argx, indentStack, arcType, bufp, topOnly);
                }
            }
        }
        break;

        case GT_STMT:
            printf("\n");

            if (!topOnly)
            {
                gtDispChild(tree->gtStmt.gtStmtExpr, indentStack, IIArcBottom);
            }
            break;

        case GT_ARR_ELEM:
            gtDispVN(tree);
            printf("\n");

            if (!topOnly)
            {
                gtDispChild(tree->gtArrElem.gtArrObj, indentStack, IIArc, nullptr, topOnly);

                unsigned dim;
                for (dim = 0; dim < tree->gtArrElem.gtArrRank; dim++)
                {
                    IndentInfo arcType = ((dim + 1) == tree->gtArrElem.gtArrRank) ? IIArcBottom : IIArc;
                    gtDispChild(tree->gtArrElem.gtArrInds[dim], indentStack, arcType, nullptr, topOnly);
                }
            }
            break;

        case GT_ARR_OFFSET:
            gtDispVN(tree);
            printf("\n");
            if (!topOnly)
            {
                gtDispChild(tree->gtArrOffs.gtOffset, indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtArrOffs.gtIndex, indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtArrOffs.gtArrObj, indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        case GT_CMPXCHG:
            gtDispVN(tree);
            printf("\n");
            if (!topOnly)
            {
                gtDispChild(tree->gtCmpXchg.gtOpLocation, indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtCmpXchg.gtOpValue, indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtCmpXchg.gtOpComparand, indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
        case GT_SIMD_CHK:
#endif // FEATURE_SIMD
            gtDispVN(tree);
            printf("\n");
            if (!topOnly)
            {
                gtDispChild(tree->gtBoundsChk.gtIndex, indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtBoundsChk.gtArrLen, indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        case GT_STORE_DYN_BLK:
        case GT_DYN_BLK:
            if (tree->OperIsCopyBlkOp())
            {
                printf(" (copy)");
            }
            else if (tree->OperIsInitBlkOp())
            {
                printf(" (init)");
            }
            gtDispVN(tree);
            printf("\n");
            if (!topOnly)
            {
                if (tree->gtDynBlk.Data() != nullptr)
                {
                    gtDispChild(tree->gtDynBlk.Data(), indentStack, IIArc, nullptr, topOnly);
                }
                gtDispChild(tree->gtDynBlk.Addr(), indentStack, IIArc, nullptr, topOnly);
                gtDispChild(tree->gtDynBlk.gtDynamicSize, indentStack, IIArcBottom, nullptr, topOnly);
            }
            break;

        default:
            printf("<DON'T KNOW HOW TO DISPLAY THIS NODE> :");
            printf(""); // null string means flush
            break;
    }
}

//------------------------------------------------------------------------
// gtGetArgMsg: Construct a message about the given argument
//
// Arguments:
//    call      - The call for which 'arg' is an argument
//    arg       - The argument for which a message should be constructed
//    argNum    - The ordinal number of the arg in the argument list
//    listCount - When printing in LIR form this is the count for a GT_FIELD_LIST
//                or -1 if we are not printing in LIR form
//    bufp      - A pointer to the buffer into which the message is written
//    bufLength - The length of the buffer pointed to by bufp
//
// Return Value:
//    No return value, but bufp is written.
//
// Assumptions:
//    'call' must be a call node
//    'arg' must be an argument to 'call' (else gtArgEntryByNode will assert)

void Compiler::gtGetArgMsg(
    GenTreeCall* call, GenTreePtr arg, unsigned argNum, int listCount, char* bufp, unsigned bufLength)
{
    if (call->gtCallLateArgs != nullptr)
    {
        fgArgTabEntryPtr curArgTabEntry = gtArgEntryByArgNum(call, argNum);
        assert(curArgTabEntry);

        if (arg->gtFlags & GTF_LATE_ARG)
        {
            sprintf_s(bufp, bufLength, "arg%d SETUP%c", argNum, 0);
        }
        else
        {
#ifdef _TARGET_ARM_
            if (curArgTabEntry->isSplit)
            {
                regNumber firstReg = curArgTabEntry->regNum;
                if (listCount == -1)
                {
                    if (curArgTabEntry->numRegs == 1)
                    {
                        sprintf_s(bufp, bufLength, "arg%d %s out+%02x%c", argNum, compRegVarName(firstReg),
                                  (curArgTabEntry->slotNum) * TARGET_POINTER_SIZE, 0);
                    }
                    else
                    {
                        regNumber lastReg   = REG_STK;
                        char      separator = (curArgTabEntry->numRegs == 2) ? ',' : '-';
                        if (curArgTabEntry->isHfaRegArg)
                        {
                            unsigned lastRegNum = genMapFloatRegNumToRegArgNum(firstReg) + curArgTabEntry->numRegs - 1;
                            lastReg             = genMapFloatRegArgNumToRegNum(lastRegNum);
                        }
                        else
                        {
                            unsigned lastRegNum = genMapIntRegNumToRegArgNum(firstReg) + curArgTabEntry->numRegs - 1;
                            lastReg             = genMapIntRegArgNumToRegNum(lastRegNum);
                        }
                        sprintf_s(bufp, bufLength, "arg%d %s%c%s out+%02x%c", argNum, compRegVarName(firstReg),
                                  separator, compRegVarName(lastReg), (curArgTabEntry->slotNum) * TARGET_POINTER_SIZE,
                                  0);
                    }
                }
                else
                {
                    unsigned curArgNum = BAD_VAR_NUM;
                    bool     isFloat   = curArgTabEntry->isHfaRegArg;
                    if (isFloat)
                    {
                        curArgNum = genMapFloatRegNumToRegArgNum(firstReg) + listCount;
                    }
                    else
                    {
                        curArgNum = genMapIntRegNumToRegArgNum(firstReg) + listCount;
                    }

                    if (!isFloat && curArgNum < MAX_REG_ARG)
                    {
                        regNumber curReg = genMapIntRegArgNumToRegNum(curArgNum);
                        sprintf_s(bufp, bufLength, "arg%d m%d %s%c", argNum, listCount, compRegVarName(curReg), 0);
                    }
                    else if (isFloat && curArgNum < MAX_FLOAT_REG_ARG)
                    {
                        regNumber curReg = genMapFloatRegArgNumToRegNum(curArgNum);
                        sprintf_s(bufp, bufLength, "arg%d m%d %s%c", argNum, listCount, compRegVarName(curReg), 0);
                    }
                    else
                    {
                        unsigned stackSlot = listCount - curArgTabEntry->numRegs;
                        sprintf_s(bufp, bufLength, "arg%d m%d out+%s%c", argNum, listCount, stackSlot, 0);
                    }
                }
                return;
            }
#endif // _TARGET_ARM_
#if FEATURE_FIXED_OUT_ARGS
            if (listCount == -1)
            {
                sprintf_s(bufp, bufLength, "arg%d out+%02x%c", argNum, curArgTabEntry->slotNum * TARGET_POINTER_SIZE,
                          0);
            }
            else // listCount is 0,1,2 or 3
            {
                assert(listCount <= MAX_ARG_REG_COUNT);
                sprintf_s(bufp, bufLength, "arg%d out+%02x%c", argNum,
                          (curArgTabEntry->slotNum + listCount) * TARGET_POINTER_SIZE, 0);
            }
#else
            sprintf_s(bufp, bufLength, "arg%d on STK%c", argNum, 0);
#endif
        }
    }
    else
    {
        sprintf_s(bufp, bufLength, "arg%d%c", argNum, 0);
    }
}

//------------------------------------------------------------------------
// gtGetLateArgMsg: Construct a message about the given argument
//
// Arguments:
//    call         - The call for which 'arg' is an argument
//    argx         - The argument for which a message should be constructed
//    lateArgIndex - The ordinal number of the arg in the lastArg  list
//    listCount    - When printing in LIR form this is the count for a multireg GT_FIELD_LIST
//                   or -1 if we are not printing in LIR form
//    bufp         - A pointer to the buffer into which the message is written
//    bufLength    - The length of the buffer pointed to by bufp
//
// Return Value:
//    No return value, but bufp is written.
//
// Assumptions:
//    'call' must be a call node
//    'arg' must be an argument to 'call' (else gtArgEntryByNode will assert)

void Compiler::gtGetLateArgMsg(
    GenTreeCall* call, GenTreePtr argx, int lateArgIndex, int listCount, char* bufp, unsigned bufLength)
{
    assert(!argx->IsArgPlaceHolderNode()); // No place holders nodes are in gtCallLateArgs;

    fgArgTabEntryPtr curArgTabEntry = gtArgEntryByLateArgIndex(call, lateArgIndex);
    assert(curArgTabEntry);
    regNumber argReg = curArgTabEntry->regNum;

#if !FEATURE_FIXED_OUT_ARGS
    assert(lateArgIndex < call->regArgListCount);
    assert(argReg == call->regArgList[lateArgIndex]);
#else
    if (argReg == REG_STK)
    {
        sprintf_s(bufp, bufLength, "arg%d in out+%02x%c", curArgTabEntry->argNum,
                  curArgTabEntry->slotNum * TARGET_POINTER_SIZE, 0);
    }
    else
#endif
    {
        if (gtArgIsThisPtr(curArgTabEntry))
        {
            sprintf_s(bufp, bufLength, "this in %s%c", compRegVarName(argReg), 0);
        }
#ifdef _TARGET_ARM_
        else if (curArgTabEntry->isSplit)
        {
            regNumber firstReg = curArgTabEntry->regNum;
            unsigned  argNum   = curArgTabEntry->argNum;
            if (listCount == -1)
            {
                if (curArgTabEntry->numRegs == 1)
                {
                    sprintf_s(bufp, bufLength, "arg%d %s out+%02x%c", argNum, compRegVarName(firstReg),
                              (curArgTabEntry->slotNum) * TARGET_POINTER_SIZE, 0);
                }
                else
                {
                    regNumber lastReg   = REG_STK;
                    char      separator = (curArgTabEntry->numRegs == 2) ? ',' : '-';
                    if (curArgTabEntry->isHfaRegArg)
                    {
                        unsigned lastRegNum = genMapFloatRegNumToRegArgNum(firstReg) + curArgTabEntry->numRegs - 1;
                        lastReg             = genMapFloatRegArgNumToRegNum(lastRegNum);
                    }
                    else
                    {
                        unsigned lastRegNum = genMapIntRegNumToRegArgNum(firstReg) + curArgTabEntry->numRegs - 1;
                        lastReg             = genMapIntRegArgNumToRegNum(lastRegNum);
                    }
                    sprintf_s(bufp, bufLength, "arg%d %s%c%s out+%02x%c", argNum, compRegVarName(firstReg), separator,
                              compRegVarName(lastReg), (curArgTabEntry->slotNum) * TARGET_POINTER_SIZE, 0);
                }
            }
            else
            {
                unsigned curArgNum = BAD_VAR_NUM;
                bool     isFloat   = curArgTabEntry->isHfaRegArg;
                if (isFloat)
                {
                    curArgNum = genMapFloatRegNumToRegArgNum(firstReg) + listCount;
                }
                else
                {
                    curArgNum = genMapIntRegNumToRegArgNum(firstReg) + listCount;
                }

                if (!isFloat && curArgNum < MAX_REG_ARG)
                {
                    regNumber curReg = genMapIntRegArgNumToRegNum(curArgNum);
                    sprintf_s(bufp, bufLength, "arg%d m%d %s%c", argNum, listCount, compRegVarName(curReg), 0);
                }
                else if (isFloat && curArgNum < MAX_FLOAT_REG_ARG)
                {
                    regNumber curReg = genMapFloatRegArgNumToRegNum(curArgNum);
                    sprintf_s(bufp, bufLength, "arg%d m%d %s%c", argNum, listCount, compRegVarName(curReg), 0);
                }
                else
                {
                    unsigned stackSlot = listCount - curArgTabEntry->numRegs;
                    sprintf_s(bufp, bufLength, "arg%d m%d out+%s%c", argNum, listCount, stackSlot, 0);
                }
            }
            return;
        }
#endif // _TARGET_ARM_
        else
        {
#if FEATURE_MULTIREG_ARGS
            if (curArgTabEntry->numRegs >= 2)
            {
                regNumber otherRegNum;
#if defined(FEATURE_UNIX_AMD64_STRUCT_PASSING)
                assert(curArgTabEntry->numRegs == 2);
                otherRegNum = curArgTabEntry->otherRegNum;
#else
                otherRegNum = (regNumber)(((unsigned)curArgTabEntry->regNum) + curArgTabEntry->numRegs - 1);
#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

                if (listCount == -1)
                {
                    char seperator = (curArgTabEntry->numRegs == 2) ? ',' : '-';

                    sprintf_s(bufp, bufLength, "arg%d %s%c%s%c", curArgTabEntry->argNum, compRegVarName(argReg),
                              seperator, compRegVarName(otherRegNum), 0);
                }
                else // listCount is 0,1,2 or 3
                {
                    assert(listCount <= MAX_ARG_REG_COUNT);
                    regNumber curReg = (listCount == 1) ? otherRegNum : (regNumber)((unsigned)(argReg) + listCount);
                    sprintf_s(bufp, bufLength, "arg%d m%d %s%c", curArgTabEntry->argNum, listCount,
                              compRegVarName(curReg), 0);
                }
            }
            else
#endif
            {
                sprintf_s(bufp, bufLength, "arg%d in %s%c", curArgTabEntry->argNum, compRegVarName(argReg), 0);
            }
        }
    }
}

//------------------------------------------------------------------------
// gtDispArgList: Dump the tree for a call arg list
//
// Arguments:
//    call         - The call to dump arguments for
//    indentStack  - the specification for the current level of indentation & arcs
//
// Return Value:
//    None.
//
void Compiler::gtDispArgList(GenTreeCall* call, IndentStack* indentStack)
{
    GenTree*  args      = call->gtCallArgs;
    unsigned  argnum    = 0;
    const int BufLength = 256;
    char      buf[BufLength];
    char*     bufp        = &buf[0];
    unsigned  numChildren = call->NumChildren();
    assert(numChildren != 0);
    bool argListIsLastChild = (args == call->GetChild(numChildren - 1));

    IndentInfo arcType = IIArc;
    if (call->gtCallObjp != nullptr)
    {
        argnum++;
    }

    while (args != nullptr)
    {
        assert(args->gtOper == GT_LIST);
        GenTree* arg = args->gtOp.gtOp1;
        if (!arg->IsNothingNode() && !arg->IsArgPlaceHolderNode())
        {
            gtGetArgMsg(call, arg, argnum, -1, bufp, BufLength);
            if (argListIsLastChild && (args->gtOp.gtOp2 == nullptr))
            {
                arcType = IIArcBottom;
            }
            gtDispChild(arg, indentStack, arcType, bufp, false);
        }
        args = args->gtOp.gtOp2;
        argnum++;
    }
}

//------------------------------------------------------------------------
// gtDispArgList: Dump the tree for a call arg list
//
// Arguments:
//    tree         - The call for which 'arg' is an argument
//    indentStack  - the specification for the current level of indentation & arcs
//
// Return Value:
//    None.
//
// Assumptions:
//    'tree' must be a GT_LIST node

void Compiler::gtDispTreeList(GenTreePtr tree, IndentStack* indentStack /* = nullptr */)
{
    for (/*--*/; tree != nullptr; tree = tree->gtNext)
    {
        gtDispTree(tree, indentStack);
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
    bool unused;
    gtDispRange(containingRange.GetTreeRange(tree, &unused));
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

        // 49 spaces for alignment
        printf("%-49s", "");
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
        if (operand->IsArgPlaceHolderNode() || !operand->IsValue())
        {
            // Either of these situations may happen with calls.
            continue;
        }

        if (nodeIsCall)
        {
            GenTreeCall* call = node->AsCall();
            if (operand == call->gtCallObjp)
            {
                sprintf_s(buf, sizeof(buf), "this in %s", compRegVarName(REG_ARG_0));
                displayOperand(operand, buf, operandArc, indentStack, prefixIndent);
            }
            else if (operand == call->gtCallAddr)
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
                fgArgTabEntryPtr curArgTabEntry = gtArgEntryByNode(call, operand);
                assert(curArgTabEntry);

                if (operand->OperGet() == GT_LIST)
                {
                    int listIndex = 0;
                    for (GenTreeArgList* element = operand->AsArgList(); element != nullptr; element = element->Rest())
                    {
                        operand = element->Current();
                        if (curArgTabEntry->lateArgInx == (unsigned)-1)
                        {
                            gtGetArgMsg(call, operand, curArgTabEntry->argNum, listIndex, buf, sizeof(buf));
                        }
                        else
                        {
                            gtGetLateArgMsg(call, operand, curArgTabEntry->lateArgInx, listIndex, buf, sizeof(buf));
                        }

                        displayOperand(operand, buf, operandArc, indentStack, prefixIndent);
                        operandArc = IIArc;
                    }
                }
                else
                {
                    if (curArgTabEntry->lateArgInx == (unsigned)-1)
                    {
                        gtGetArgMsg(call, operand, curArgTabEntry->argNum, -1, buf, sizeof(buf));
                    }
                    else
                    {
                        gtGetLateArgMsg(call, operand, curArgTabEntry->lateArgInx, -1, buf, sizeof(buf));
                    }

                    displayOperand(operand, buf, operandArc, indentStack, prefixIndent);
                }
            }
        }
        else if (node->OperIsDynBlkOp())
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
                assert(operand == node->AsDynBlk()->gtDynamicSize);
                displayOperand(operand, "size", operandArc, indentStack, prefixIndent);
            }
        }
        else if (node->OperGet() == GT_DYN_BLK)
        {
            if (operand == node->AsBlk()->Addr())
            {
                displayOperand(operand, "lhs", operandArc, indentStack, prefixIndent);
            }
            else
            {
                assert(operand == node->AsDynBlk()->gtDynamicSize);
                displayOperand(operand, "size", operandArc, indentStack, prefixIndent);
            }
        }
        else if (node->OperIsAssignment())
        {
            if (operand == node->gtGetOp1())
            {
                displayOperand(operand, "lhs", operandArc, indentStack, prefixIndent);
            }
            else
            {
                displayOperand(operand, "rhs", operandArc, indentStack, prefixIndent);
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

GenTreePtr Compiler::gtFoldExpr(GenTreePtr tree)
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

    if (!(kind & GTK_SMPOP))
    {
        return tree;
    }

    GenTreePtr op1 = tree->gtOp.gtOp1;

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
        if (op1->OperKind() & GTK_CONST)
        {
            return gtFoldExprConst(tree);
        }
    }
    else if ((kind & GTK_BINOP) && op1 && tree->gtOp.gtOp2 &&
             // Don't take out conditionals for debugging
             !((opts.compDbgCode || opts.MinOpts()) && tree->OperIsCompare()))
    {
        GenTreePtr op2 = tree->gtOp.gtOp2;

        // The atomic operations are exempted here because they are never computable statically;
        // one of their arguments is an address.
        if (((op1->OperKind() & op2->OperKind()) & GTK_CONST) && !tree->OperIsAtomicOp())
        {
            /* both nodes are constants - fold the expression */
            return gtFoldExprConst(tree);
        }
        else if ((op1->OperKind() | op2->OperKind()) & GTK_CONST)
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
        else if (op2->OperGet() == GT_COLON)
        {
            assert(tree->OperGet() == GT_QMARK);

            GenTreePtr colon_op1 = op2->gtOp.gtOp1;
            GenTreePtr colon_op2 = op2->gtOp.gtOp2;

            if (gtCompareTree(colon_op1, colon_op2))
            {
                // Both sides of the GT_COLON are the same tree

                GenTreePtr sideEffList = nullptr;
                gtExtractSideEffList(op1, &sideEffList);

                fgUpdateRefCntForExtract(op1, sideEffList);   // Decrement refcounts for op1, Keeping any side-effects
                fgUpdateRefCntForExtract(colon_op1, nullptr); // Decrement refcounts for colon_op1

                // Clear colon flags only if the qmark itself is not conditionaly executed
                if ((tree->gtFlags & GTF_COLON_COND) == 0)
                {
                    fgWalkTreePre(&colon_op2, gtClearColonCond);
                }

                if (sideEffList == nullptr)
                {
                    // No side-effects, just return colon_op2
                    return colon_op2;
                }
                else
                {
#ifdef DEBUG
                    if (verbose)
                    {
                        printf("\nIdentical GT_COLON trees with side effects! Extracting side effects...\n");
                        gtDispTree(sideEffList);
                        printf("\n");
                    }
#endif
                    // Change the GT_COLON into a GT_COMMA node with the side-effects
                    op2->ChangeOper(GT_COMMA);
                    op2->gtFlags |= (sideEffList->gtFlags & GTF_ALL_EFFECT);
                    op2->gtOp.gtOp1 = sideEffList;
                    return op2;
                }
            }
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
    // Can only fold calls to special intrinsics.
    if ((call->gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC) == 0)
    {
        return call;
    }

    // Defer folding if not optimizing.
    if (opts.compDbgCode || opts.MinOpts())
    {
        return call;
    }

    // Fetch id of the intrinsic.
    const CorInfoIntrinsics methodID = info.compCompHnd->getIntrinsicID(call->gtCallMethHnd);

    switch (methodID)
    {
        case CORINFO_INTRINSIC_TypeEQ:
        case CORINFO_INTRINSIC_TypeNEQ:
        {
            noway_assert(call->TypeGet() == TYP_INT);
            GenTree* op1 = call->gtCallArgs->gtOp.gtOp1;
            GenTree* op2 = call->gtCallArgs->gtOp.gtOp2->gtOp.gtOp1;

            // If either operand is known to be a RuntimeType, this can be folded
            GenTree* result = gtFoldTypeEqualityCall(methodID, op1, op2);
            if (result != nullptr)
            {
                return result;
            }
            break;
        }

        default:
            break;
    }

    // Check for a new-style jit intrinsic.
    const NamedIntrinsic ni = lookupNamedIntrinsic(call->gtCallMethHnd);

    if (ni == NI_System_Enum_HasFlag)
    {
        GenTree* thisOp = call->gtCallObjp;
        GenTree* flagOp = call->gtCallArgs->gtOp.gtOp1;
        GenTree* result = gtOptimizeEnumHasFlag(thisOp, flagOp);

        if (result != nullptr)
        {
            return result;
        }
    }

    return call;
}

//------------------------------------------------------------------------
// gtFoldTypeEqualityCall: see if a (potential) type equality call is foldable
//
// Arguments:
//    methodID -- type equality intrinsic ID
//    op1 -- first argument to call
//    op2 -- second argument to call
//
// Returns:
//    nulltpr if no folding happened.
//    An alternative tree if folding happens.
//
// Notes:
//    If either operand is known to be a a RuntimeType, then the type
//    equality methods will simply check object identity and so we can
//    fold the call into a simple compare of the call's operands.

GenTree* Compiler::gtFoldTypeEqualityCall(CorInfoIntrinsics methodID, GenTree* op1, GenTree* op2)
{
    // The method must be be a type equality intrinsic
    assert(methodID == CORINFO_INTRINSIC_TypeEQ || methodID == CORINFO_INTRINSIC_TypeNEQ);

    if ((gtGetTypeProducerKind(op1) == TPK_Unknown) && (gtGetTypeProducerKind(op2) == TPK_Unknown))
    {
        return nullptr;
    }

    const genTreeOps simpleOp = (methodID == CORINFO_INTRINSIC_TypeEQ) ? GT_EQ : GT_NE;

    JITDUMP("\nFolding call to Type:op_%s to a simple compare via %s\n",
            methodID == CORINFO_INTRINSIC_TypeEQ ? "Equality" : "Inequality", GenTree::OpName(simpleOp));

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

GenTreePtr Compiler::gtFoldExprCompare(GenTreePtr tree)
{
    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtOp.gtOp2;

    assert(tree->OperIsCompare());

    /* Filter out cases that cannot be folded here */

    /* Do not fold floats or doubles (e.g. NaN != Nan) */

    if (varTypeIsFloating(op1->TypeGet()))
    {
        return tree;
    }

    /* Currently we can only fold when the two subtrees exactly match */

    if ((tree->gtFlags & GTF_SIDE_EFFECT) || GenTree::Compare(op1, op2, true) == false)
    {
        return tree; /* return unfolded tree */
    }

    GenTreePtr cons;

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

    /* The node has beeen folded into 'cons' */

    if (fgGlobalMorph)
    {
        fgMorphTreeDone(cons);
    }
    else
    {
        cons->gtNext = tree->gtNext;
        cons->gtPrev = tree->gtPrev;
    }
    if (lvaLocalVarRefCounted)
    {
        lvaRecursiveDecRefCounts(tree);
    }
    return cons;
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
    GenTree* const         op1     = tree->gtOp.gtOp1;
    const TypeProducerKind op1Kind = gtGetTypeProducerKind(op1);
    if (op1Kind == TPK_Unknown)
    {
        return tree;
    }

    GenTree* const         op2     = tree->gtOp.gtOp2;
    const TypeProducerKind op2Kind = gtGetTypeProducerKind(op2);
    if (op2Kind == TPK_Unknown)
    {
        return tree;
    }

    // We must have a handle on one side or the other here to optimize,
    // otherwise we can't be sure that optimizing is sound.
    const bool op1IsFromHandle = (op1Kind == TPK_Handle);
    const bool op2IsFromHandle = (op2Kind == TPK_Handle);

    if (!(op1IsFromHandle || op2IsFromHandle))
    {
        return tree;
    }

    // If both types are created via handles, we can simply compare
    // handles (or the indirection cells for handles) instead of the
    // types that they'd create.
    if (op1IsFromHandle && op2IsFromHandle)
    {
        JITDUMP("Optimizing compare of types-from-handles to instead compare handles\n");
        GenTree*             op1ClassFromHandle = tree->gtOp.gtOp1->gtCall.gtCallArgs->gtOp.gtOp1;
        GenTree*             op2ClassFromHandle = tree->gtOp.gtOp2->gtCall.gtCallArgs->gtOp.gtOp1;
        GenTree*             op1TunneledHandle  = nullptr;
        GenTree*             op2TunneledHandle  = nullptr;
        CORINFO_CLASS_HANDLE cls1Hnd            = nullptr;
        CORINFO_CLASS_HANDLE cls2Hnd            = nullptr;
        unsigned             runtimeLookupCount = 0;

        // Try and find class handle for op1
        if ((op1ClassFromHandle->gtOper == GT_CNS_INT) && (op1ClassFromHandle->gtType == TYP_I_IMPL))
        {
            assert(op1ClassFromHandle->IsIconHandle(GTF_ICON_CLASS_HDL));
            cls1Hnd = (CORINFO_CLASS_HANDLE)op1ClassFromHandle->gtIntCon.gtCompileTimeHandle;
        }
        else if (op1ClassFromHandle->OperGet() == GT_RUNTIMELOOKUP)
        {
            cls1Hnd = op1ClassFromHandle->AsRuntimeLookup()->GetClassHandle();
            runtimeLookupCount++;
        }
        // Tunnel through indirs we may see when prejitting
        else if (op1ClassFromHandle->gtOper == GT_IND)
        {
            // The handle indirs we can optimize will be marked as non-faulting.
            // Certain others (eg from refanytype) may not be.
            if (op1ClassFromHandle->gtFlags & GTF_IND_NONFAULTING)
            {
                GenTree* op1HandleLiteral = op1ClassFromHandle->gtOp.gtOp1;

                // If, after tunneling, we have a constant handle,
                // remember the class and the value tree for later.
                if ((op1HandleLiteral->gtOper == GT_CNS_INT) && (op1HandleLiteral->gtType == TYP_I_IMPL))
                {
                    JITDUMP("tunneling through indir on op1\n");
                    op1TunneledHandle = op1HandleLiteral;

                    // These handle constants should be class handles.
                    assert(op1TunneledHandle->IsIconHandle(GTF_ICON_CLASS_HDL));
                    cls1Hnd = (CORINFO_CLASS_HANDLE)op1TunneledHandle->gtIntCon.gtCompileTimeHandle;
                }
            }
        }

        // Try and find class handle for op2
        if ((op2ClassFromHandle->gtOper == GT_CNS_INT) && (op2ClassFromHandle->gtType == TYP_I_IMPL))
        {
            assert(op2ClassFromHandle->IsIconHandle(GTF_ICON_CLASS_HDL));
            cls2Hnd = (CORINFO_CLASS_HANDLE)op2ClassFromHandle->gtIntCon.gtCompileTimeHandle;
        }
        else if (op2ClassFromHandle->OperGet() == GT_RUNTIMELOOKUP)
        {
            cls2Hnd = op2ClassFromHandle->AsRuntimeLookup()->GetClassHandle();
            runtimeLookupCount++;
        }
        // Tunnel through indirs we may see when prejitting
        else if (op2ClassFromHandle->gtOper == GT_IND)
        {
            // The handle indirs we can optimize will be marked as non-faulting.
            // Certain others (eg from refanytype) may not be.
            if (op2ClassFromHandle->gtFlags & GTF_IND_NONFAULTING)
            {
                GenTree* op2HandleLiteral = op2ClassFromHandle->gtOp.gtOp1;

                // If, after tunneling, we have a constant handle,
                // remember the class and the value tree for later.
                if ((op2HandleLiteral->gtOper == GT_CNS_INT) && (op2HandleLiteral->gtType == TYP_I_IMPL))
                {
                    JITDUMP("tunneling through indir on op2\n");
                    op2TunneledHandle = op2HandleLiteral;

                    // These handle constants should be class handles.
                    assert(op2TunneledHandle->IsIconHandle(GTF_ICON_CLASS_HDL));
                    cls2Hnd = (CORINFO_CLASS_HANDLE)op2TunneledHandle->gtIntCon.gtCompileTimeHandle;
                }
            }
        }

        // If we have class handles, try and resolve the type equality test completely.
        if ((cls1Hnd != nullptr) && (cls2Hnd != nullptr))
        {
            JITDUMP("Asking runtime to compare %p (%s) and %p (%s) for equality\n", cls1Hnd,
                    info.compCompHnd->getClassName(cls1Hnd), cls2Hnd, info.compCompHnd->getClassName(cls2Hnd));
            TypeCompareState s = info.compCompHnd->compareTypesForEquality(cls1Hnd, cls2Hnd);

            if (s != TypeCompareState::May)
            {
                // Type comparison result is known.
                const bool typesAreEqual = (s == TypeCompareState::Must);
                const bool operatorIsEQ  = (oper == GT_EQ);
                const int  compareResult = operatorIsEQ ^ typesAreEqual ? 0 : 1;
                JITDUMP("Runtime reports comparison is known at jit time: %u\n", compareResult);
                GenTree* result = gtNewIconNode(compareResult);

                // Any runtime lookups that fed into this compare are
                // now dead code, so they no longer require the runtime context.
                assert(lvaGenericsContextUseCount >= runtimeLookupCount);
                lvaGenericsContextUseCount -= runtimeLookupCount;
                return result;
            }
        }

        JITDUMP("Could not find handle for %s%s\n", (cls1Hnd == nullptr) ? " cls1" : "",
                (cls2Hnd == nullptr) ? " cls2" : "");

        // We can't answer the equality comparison definitively at jit
        // time, but can still simplfy the comparison.
        //
        // If we successfully tunneled through both operands, compare
        // the tunnneled values, otherwise compare the orignal values.
        GenTree* compare = nullptr;
        if ((op1TunneledHandle != nullptr) && (op2TunneledHandle != nullptr))
        {
            compare = gtNewOperNode(oper, TYP_INT, op1TunneledHandle, op2TunneledHandle);
        }
        else
        {
            compare = gtNewOperNode(oper, TYP_INT, op1ClassFromHandle, op2ClassFromHandle);
        }

        // Drop any now-irrelvant flags
        compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);

        return compare;
    }

    // Just one operand creates a type from a handle.
    //
    // If the other operand is fetching the type from an object,
    // we can sometimes optimize the type compare into a simpler
    // method table comparison.
    //
    // TODO: if other operand is null...
    if (op1Kind != TPK_GetType && op2Kind != TPK_GetType)
    {
        return tree;
    }

    GenTree* const opHandle = op1IsFromHandle ? op1 : op2;
    GenTree* const opOther  = op1IsFromHandle ? op2 : op1;

    // Tunnel through the handle operand to get at the class handle involved.
    GenTree* const       opHandleArgument = opHandle->gtCall.gtCallArgs->gtOp.gtOp1;
    GenTree*             opHandleLiteral  = opHandleArgument;
    CORINFO_CLASS_HANDLE clsHnd           = nullptr;

    // Unwrap any GT_NOP node used to prevent constant folding
    if ((opHandleLiteral->gtOper == GT_NOP) && (opHandleLiteral->gtType == TYP_I_IMPL))
    {
        opHandleLiteral = opHandleLiteral->gtOp.gtOp1;
    }

    // For runtime lookups we can get at the handle directly
    if (opHandleLiteral->gtOper == GT_RUNTIMELOOKUP)
    {
        clsHnd = opHandleLiteral->AsRuntimeLookup()->GetClassHandle();
    }
    else
    {
        // Tunnel through prejit indirs if necessary
        if (opHandleLiteral->gtOper == GT_IND)
        {
            // Handle indirs should be marked as nonfaulting.
            assert((opHandleLiteral->gtFlags & GTF_IND_NONFAULTING) != 0);
            opHandleLiteral = opHandleLiteral->gtOp.gtOp1;
        }

        if ((opHandleLiteral->gtOper == GT_CNS_INT) && (opHandleLiteral->gtType == TYP_I_IMPL))
        {
            assert(opHandleLiteral->IsIconHandle(GTF_ICON_CLASS_HDL));
            clsHnd = CORINFO_CLASS_HANDLE(opHandleLiteral->gtIntCon.gtCompileTimeHandle);
        }
    }

    // If we couldn't find the class handle, give up.
    if (clsHnd == nullptr)
    {
        return tree;
    }

    // Ask the VM if this type can be equality tested by a simple method
    // table comparison.
    if (!info.compCompHnd->canInlineTypeCheckWithObjectVTable(clsHnd))
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
        objOp = opOther->gtUnOp.gtOp1;
    }
    else
    {
        assert(opOther->OperGet() == GT_CALL);
        objOp = opOther->gtCall.gtCallObjp;
    }

    GenTree* const objMT = gtNewOperNode(GT_IND, TYP_I_IMPL, objOp);

    // Update various flags
    objMT->gtFlags |= GTF_EXCEPT;
    compCurBB->bbFlags |= BBF_HAS_VTABREF;
    optMethodFlags |= OMF_HAS_VTABLEREF;

    // Compare the two method tables
    GenTree* const compare = gtNewOperNode(oper, TYP_INT, objMT, knownMT);

    // Drop any any now irrelevant flags
    compare->gtFlags |= tree->gtFlags & (GTF_RELOP_JMP_USED | GTF_RELOP_QMARK | GTF_DONT_CSE);

    // And we're done
    return compare;
}

/*****************************************************************************
 *
 *  Some binary operators can be folded even if they have only one
 *  operand constant - e.g. boolean operators, add with 0
 *  multiply with 1, etc
 */

GenTreePtr Compiler::gtFoldExprSpecial(GenTreePtr tree)
{
    GenTreePtr op1  = tree->gtOp.gtOp1;
    GenTreePtr op2  = tree->gtOp.gtOp2;
    genTreeOps oper = tree->OperGet();

    GenTreePtr op, cons;
    ssize_t    val;

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

    val = cons->gtIntConCommon.IconValue();

    /* Here op is the non-constant operand, val is the constant,
       first is true if the constant is op1 */

    switch (oper)
    {
        case GT_EQ:
        case GT_NE:
        case GT_GT:

            // Optimize boxed value classes; these are always false.  This IL is
            // generated when a generic value is tested against null:
            //     <T> ... foo(T x) { ... if ((object)x == null) ...
            if (val == 0 && op->IsBoxedValue())
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
                    assert(!gtTreeHasSideEffects(op->gtBox.gtOp.gtOp1, GTF_SIDE_EFFECT));

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

                        op = gtNewIconNode(compareResult);

                        if (fgGlobalMorph)
                        {
                            fgMorphTreeDone(op);
                        }
                        else
                        {
                            op->gtNext = tree->gtNext;
                            op->gtPrev = tree->gtPrev;
                        }

                        return op;
                    }
                }
            }

            break;

        case GT_ADD:
#ifdef LEGACY_BACKEND
        case GT_ASG_ADD:
#endif
            if (val == 0)
            {
                goto DONE_FOLD;
            }
            break;

        case GT_MUL:
#ifdef LEGACY_BACKEND
        case GT_ASG_MUL:
#endif
            if (val == 1)
            {
                goto DONE_FOLD;
            }
            else if (val == 0)
            {
                /* Multiply by zero - return the 'zero' node, but not if side effects */
                if (!(op->gtFlags & GTF_SIDE_EFFECT))
                {
                    if (lvaLocalVarRefCounted)
                    {
                        lvaRecursiveDecRefCounts(op);
                    }
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_DIV:
        case GT_UDIV:
#ifdef LEGACY_BACKEND
        case GT_ASG_DIV:
#endif
            if ((op2 == cons) && (val == 1) && !(op1->OperKind() & GTK_CONST))
            {
                goto DONE_FOLD;
            }
            break;

        case GT_SUB:
#ifdef LEGACY_BACKEND
        case GT_ASG_SUB:
#endif
            if ((op2 == cons) && (val == 0) && !(op1->OperKind() & GTK_CONST))
            {
                goto DONE_FOLD;
            }
            break;

        case GT_AND:
            if (val == 0)
            {
                /* AND with zero - return the 'zero' node, but not if side effects */

                if (!(op->gtFlags & GTF_SIDE_EFFECT))
                {
                    if (lvaLocalVarRefCounted)
                    {
                        lvaRecursiveDecRefCounts(op);
                    }
                    op = cons;
                    goto DONE_FOLD;
                }
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

                if (!(op->gtFlags & GTF_SIDE_EFFECT))
                {
                    if (lvaLocalVarRefCounted)
                    {
                        lvaRecursiveDecRefCounts(op);
                    }
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
#ifdef LEGACY_BACKEND
        case GT_ASG_LSH:
        case GT_ASG_RSH:
        case GT_ASG_RSZ:
#endif
            if (val == 0)
            {
                if (op2 == cons)
                {
                    goto DONE_FOLD;
                }
                else if (!(op->gtFlags & GTF_SIDE_EFFECT))
                {
                    if (lvaLocalVarRefCounted)
                    {
                        lvaRecursiveDecRefCounts(op);
                    }
                    op = cons;
                    goto DONE_FOLD;
                }
            }
            break;

        case GT_QMARK:
        {
            assert(op1 == cons && op2 == op && op2->gtOper == GT_COLON);
            assert(op2->gtOp.gtOp1 && op2->gtOp.gtOp2);

            assert(val == 0 || val == 1);

            GenTree* opToDelete;
            if (val)
            {
                op         = op2->AsColon()->ThenNode();
                opToDelete = op2->AsColon()->ElseNode();
            }
            else
            {
                op         = op2->AsColon()->ElseNode();
                opToDelete = op2->AsColon()->ThenNode();
            }
            if (lvaLocalVarRefCounted)
            {
                lvaRecursiveDecRefCounts(opToDelete);
            }

            // Clear colon flags only if the qmark itself is not conditionaly executed
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

    /* The node has beeen folded into 'op' */

    // If there was an assigment update, we just morphed it into
    // a use, update the flags appropriately
    if (op->gtOper == GT_LCL_VAR)
    {
        assert(tree->OperIsAssignment() || (op->gtFlags & (GTF_VAR_USEASG | GTF_VAR_DEF)) == 0);

        op->gtFlags &= ~(GTF_VAR_USEASG | GTF_VAR_DEF);
    }

    op->gtNext = tree->gtNext;
    op->gtPrev = tree->gtPrev;

    return op;
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
//    try and remove unnecessary trees and will try and reduce remaning
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
//    If removal fails, is is possible that a subsequent pass may be
//    able to optimize.  Blocking side effects may now be minimized
//    (null or bounds checks might have been removed) or might be
//    better known (inline return placeholder updated with the actual
//    return expression). So the box is perhaps best left as is to
//    help trigger this re-examination.

GenTree* Compiler::gtTryRemoveBoxUpstreamEffects(GenTree* op, BoxRemovalOptions options)
{
    JITDUMP("gtTryRemoveBoxUpstreamEffects called for [%06u]\n", dspTreeID(op));
    assert(op->IsBoxedValue());

    // grab related parts for the optimization
    GenTree* asgStmt = op->gtBox.gtAsgStmtWhenInlinedBoxValue;
    assert(asgStmt->gtOper == GT_STMT);
    GenTree* copyStmt = op->gtBox.gtCopyStmtWhenInlinedBoxValue;
    assert(copyStmt->gtOper == GT_STMT);

#ifdef DEBUG
    if (verbose)
    {
        printf("\n%s to remove side effects of BOX (valuetype)\n",
               options == BR_DONT_REMOVE ? "Checking if it is possible" : "Attempting");
        gtDispTree(op);
        printf("\nWith assign\n");
        gtDispTree(asgStmt);
        printf("\nAnd copy\n");
        gtDispTree(copyStmt);
    }
#endif

    // If we don't recognize the form of the assign, bail.
    GenTree* asg = asgStmt->gtStmt.gtStmtExpr;
    if (asg->gtOper != GT_ASG)
    {
        JITDUMP(" bailing; unexpected assignment op %s\n", GenTree::OpName(asg->gtOper));
        return nullptr;
    }

    // If we're eventually going to return the type handle, remember it now.
    GenTree* boxTypeHandle = nullptr;
    if (options == BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE)
    {
        GenTree*   asgSrc     = asg->gtOp.gtOp2;
        genTreeOps asgSrcOper = asgSrc->OperGet();

        // Allocation may be via AllocObj or via helper call, depending
        // on when this is invoked and whether the jit is using AllocObj
        // for R2R allocations.
        if (asgSrcOper == GT_ALLOCOBJ)
        {
            GenTreeAllocObj* allocObj = asgSrc->AsAllocObj();
            boxTypeHandle             = allocObj->gtOp.gtOp1;
        }
        else if (asgSrcOper == GT_CALL)
        {
            GenTreeCall* newobjCall = asgSrc->AsCall();
            GenTree*     newobjArgs = newobjCall->gtCallArgs;

            // In R2R expansions the handle may not be an explicit operand to the helper,
            // so we can't remove the box.
            if (newobjArgs == nullptr)
            {
                assert(newobjCall->IsHelperCall(this, CORINFO_HELP_READYTORUN_NEW));
                JITDUMP("bailing; newobj via R2R helper\n");
                return nullptr;
            }

            boxTypeHandle = newobjArgs->AsArgList()->Current();
        }
        else
        {
            unreached();
        }

        assert(boxTypeHandle != nullptr);
    }

    // If we don't recognize the form of the copy, bail.
    GenTree* copy = copyStmt->gtStmt.gtStmtExpr;
    if (copy->gtOper != GT_ASG)
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
            // Anything else is a missed case we should
            // figure out how to handle.  One known case
            // is GT_COMMAs enclosing the GT_ASG we are
            // looking for.
            JITDUMP(" bailing; unexpected copy op %s\n", GenTree::OpName(copy->gtOper));
        }
        return nullptr;
    }

    // If the copy is a struct copy, make sure we know how to isolate
    // any source side effects.
    GenTree* copySrc = copy->gtOp.gtOp2;

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

        if (copySrc->gtType == TYP_STRUCT)
        {
            isStructCopy = true;

            if ((copySrc->gtOper != GT_OBJ) && (copySrc->gtOper != GT_IND) && (copySrc->gtOper != GT_FIELD))
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

    // Otherwise, proceed with the optimization.
    //
    // Change the assignment expression to a NOP.
    JITDUMP("\nBashing NEWOBJ [%06u] to NOP\n", dspTreeID(asg));
    asg->gtBashToNOP();

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
        copyStmt->gtStmt.gtStmtExpr = copySrc;
        JITDUMP(" to scalar read via [%06u]\n", dspTreeID(copySrc));
    }
    else
    {
        // For struct types read the first byte of the
        // source struct; there's no need to read the
        // entire thing, and no place to put it.
        assert(copySrc->gtOper == GT_OBJ || copySrc->gtOper == GT_IND || copySrc->gtOper == GT_FIELD);
        copyStmt->gtStmt.gtStmtExpr = copySrc;

        if (options == BR_REMOVE_AND_NARROW || options == BR_REMOVE_AND_NARROW_WANT_TYPE_HANDLE)
        {
            JITDUMP(" to read first byte of struct via modified [%06u]\n", dspTreeID(copySrc));
            copySrc->ChangeOper(GT_IND);
            copySrc->gtType = TYP_BYTE;
        }
        else
        {
            JITDUMP(" to read entire struct via modified [%06u]\n", dspTreeID(copySrc));
        }
    }

    if (fgStmtListThreaded)
    {
        fgSetStmtSeq(asgStmt);
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

    GenTree* flagVal = gtTryRemoveBoxUpstreamEffects(flagOp, BR_REMOVE_BUT_NOT_NARROW);
    if (flagVal == nullptr)
    {
        // Note we may fail here if the flag operand comes from
        // a call. We should be able to retry this post-inlining.
        JITDUMP("bailing, can't undo box of 'flag' operand\n");
        return nullptr;
    }

    // Yes, both boxes can be cleaned up. Optimize.
    JITDUMP("Optimizing call to Enum.HasFlag\n");

    // Undo the boxing of thisOp and prepare to operate directly
    // on the original enum values.
    thisVal = gtTryRemoveBoxUpstreamEffects(thisOp, BR_REMOVE_BUT_NOT_NARROW);

    // Our trial removal above should guarantee successful removal here.
    assert(thisVal != nullptr);

    // We should have a consistent view of the type
    var_types type = thisVal->TypeGet();
    assert(type == flagVal->TypeGet());

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
        const unsigned thisTmp         = lvaGrabTemp(true DEBUGARG("Enum:HasFlag this temp"));
        GenTree*       thisAsg         = gtNewTempAssign(thisTmp, thisVal);
        GenTree*       thisAsgStmt     = thisOp->AsBox()->gtCopyStmtWhenInlinedBoxValue;
        thisAsgStmt->gtStmt.gtStmtExpr = thisAsg;
        thisValOpt                     = gtNewLclvNode(thisTmp, type);
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
        const unsigned flagTmp         = lvaGrabTemp(true DEBUGARG("Enum:HasFlag flag temp"));
        GenTree*       flagAsg         = gtNewTempAssign(flagTmp, flagVal);
        GenTree*       flagAsgStmt     = flagOp->AsBox()->gtCopyStmtWhenInlinedBoxValue;
        flagAsgStmt->gtStmt.gtStmtExpr = flagAsg;
        flagValOpt                     = gtNewLclvNode(flagTmp, type);
        flagValOptCopy                 = gtNewLclvNode(flagTmp, type);
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
GenTreePtr Compiler::gtFoldExprConst(GenTreePtr tree)
{
    unsigned kind = tree->OperKind();

    SSIZE_T       i1, i2, itemp;
    INT64         lval1, lval2, ltemp;
    float         f1, f2;
    double        d1, d2;
    var_types     switchType;
    FieldSeqNode* fieldSeq = FieldSeqStore::NotAField(); // default unless we override it when folding

    assert(kind & (GTK_UNOP | GTK_BINOP));

    GenTreePtr op1 = tree->gtOp.gtOp1;
    GenTreePtr op2 = tree->gtGetOp2IfPresent();

    if (!opts.OptEnabled(CLFLG_CONSTANTFOLD))
    {
        return tree;
    }

    if (tree->OperGet() == GT_NOP)
    {
        return tree;
    }

#ifdef FEATURE_SIMD
    if (tree->OperGet() == GT_SIMD)
    {
        return tree;
    }
#endif // FEATURE_SIMD

    if (tree->gtOper == GT_ALLOCOBJ)
    {
        return tree;
    }

    if (tree->gtOper == GT_RUNTIMELOOKUP)
    {
        return tree;
    }

    if (kind & GTK_UNOP)
    {
        assert(op1->OperKind() & GTK_CONST);

        switch (op1->gtType)
        {
            case TYP_INT:

                /* Fold constant INT unary operator */
                assert(op1->gtIntCon.ImmedValCanBeFolded(this, tree->OperGet()));
                i1 = (int)op1->gtIntCon.gtIconVal;

                // If we fold a unary oper, then the folded constant
                // is considered a ConstantIndexField if op1 was one
                //

                if ((op1->gtIntCon.gtFieldSeq != nullptr) && op1->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq())
                {
                    fieldSeq = op1->gtIntCon.gtFieldSeq;
                }

                switch (tree->gtOper)
                {
                    case GT_NOT:
                        i1 = ~i1;
                        break;

                    case GT_NEG:
#ifdef LEGACY_BACKEND
                    case GT_CHS:
#endif
                        i1 = -i1;
                        break;

                    case GT_CAST:
                        // assert (genActualType(tree->CastToType()) == tree->gtType);
                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                itemp = INT32(INT8(i1));
                                goto CHK_OVF;

                            case TYP_SHORT:
                                itemp = INT32(INT16(i1));
                            CHK_OVF:
                                if (tree->gtOverflow() && ((itemp != i1) || ((tree->gtFlags & GTF_UNSIGNED) && i1 < 0)))
                                {
                                    goto INT_OVF;
                                }
                                i1 = itemp;
                                goto CNS_INT;

                            case TYP_CHAR:
                                itemp = INT32(UINT16(i1));
                                if (tree->gtOverflow())
                                {
                                    if (itemp != i1)
                                    {
                                        goto INT_OVF;
                                    }
                                }
                                i1 = itemp;
                                goto CNS_INT;

                            case TYP_BOOL:
                            case TYP_UBYTE:
                                itemp = INT32(UINT8(i1));
                                if (tree->gtOverflow())
                                {
                                    if (itemp != i1)
                                    {
                                        goto INT_OVF;
                                    }
                                }
                                i1 = itemp;
                                goto CNS_INT;

                            case TYP_UINT:
                                if (!(tree->gtFlags & GTF_UNSIGNED) && tree->gtOverflow() && i1 < 0)
                                {
                                    goto INT_OVF;
                                }
                                goto CNS_INT;

                            case TYP_INT:
                                if ((tree->gtFlags & GTF_UNSIGNED) && tree->gtOverflow() && i1 < 0)
                                {
                                    goto INT_OVF;
                                }
                                goto CNS_INT;

                            case TYP_ULONG:
                                if (!(tree->gtFlags & GTF_UNSIGNED) && tree->gtOverflow() && i1 < 0)
                                {
                                    goto LNG_OVF;
                                }
                                lval1 = UINT64(UINT32(i1));
                                goto CNS_LONG;

                            case TYP_LONG:
                                if (tree->gtFlags & GTF_UNSIGNED)
                                {
                                    lval1 = INT64(UINT32(i1));
                                }
                                else
                                {
                                    lval1 = INT64(INT32(i1));
                                }
                                goto CNS_LONG;

                            case TYP_FLOAT:
                                if (tree->gtFlags & GTF_UNSIGNED)
                                {
                                    f1 = forceCastToFloat(UINT32(i1));
                                }
                                else
                                {
                                    f1 = forceCastToFloat(INT32(i1));
                                }
                                d1 = f1;
                                goto CNS_DOUBLE;

                            case TYP_DOUBLE:
                                if (tree->gtFlags & GTF_UNSIGNED)
                                {
                                    d1 = (double)UINT32(i1);
                                }
                                else
                                {
                                    d1 = (double)INT32(i1);
                                }
                                goto CNS_DOUBLE;

                            default:
                                assert(!"BAD_TYP");
                                break;
                        }
                        return tree;

                    default:
                        return tree;
                }

                goto CNS_INT;

            case TYP_LONG:

                /* Fold constant LONG unary operator */

                assert(op1->gtIntConCommon.ImmedValCanBeFolded(this, tree->OperGet()));
                lval1 = op1->gtIntConCommon.LngValue();

                switch (tree->gtOper)
                {
                    case GT_NOT:
                        lval1 = ~lval1;
                        break;

                    case GT_NEG:
#ifdef LEGACY_BACKEND
                    case GT_CHS:
#endif
                        lval1 = -lval1;
                        break;

                    case GT_CAST:
                        assert(genActualType(tree->CastToType()) == tree->gtType);
                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                i1 = INT32(INT8(lval1));
                                goto CHECK_INT_OVERFLOW;

                            case TYP_SHORT:
                                i1 = INT32(INT16(lval1));
                                goto CHECK_INT_OVERFLOW;

                            case TYP_CHAR:
                                i1 = INT32(UINT16(lval1));
                                goto CHECK_UINT_OVERFLOW;

                            case TYP_UBYTE:
                                i1 = INT32(UINT8(lval1));
                                goto CHECK_UINT_OVERFLOW;

                            case TYP_INT:
                                i1 = INT32(lval1);

                            CHECK_INT_OVERFLOW:
                                if (tree->gtOverflow())
                                {
                                    if (i1 != lval1)
                                    {
                                        goto INT_OVF;
                                    }
                                    if ((tree->gtFlags & GTF_UNSIGNED) && i1 < 0)
                                    {
                                        goto INT_OVF;
                                    }
                                }
                                goto CNS_INT;

                            case TYP_UINT:
                                i1 = UINT32(lval1);

                            CHECK_UINT_OVERFLOW:
                                if (tree->gtOverflow() && UINT32(i1) != lval1)
                                {
                                    goto INT_OVF;
                                }
                                goto CNS_INT;

                            case TYP_ULONG:
                                if (!(tree->gtFlags & GTF_UNSIGNED) && tree->gtOverflow() && lval1 < 0)
                                {
                                    goto LNG_OVF;
                                }
                                goto CNS_LONG;

                            case TYP_LONG:
                                if ((tree->gtFlags & GTF_UNSIGNED) && tree->gtOverflow() && lval1 < 0)
                                {
                                    goto LNG_OVF;
                                }
                                goto CNS_LONG;

                            case TYP_FLOAT:
                            case TYP_DOUBLE:
                                if ((tree->gtFlags & GTF_UNSIGNED) && lval1 < 0)
                                {
                                    d1 = FloatingPointUtils::convertUInt64ToDouble((unsigned __int64)lval1);
                                }
                                else
                                {
                                    d1 = (double)lval1;
                                }

                                if (tree->CastToType() == TYP_FLOAT)
                                {
                                    f1 = forceCastToFloat(d1); // truncate precision
                                    d1 = f1;
                                }
                                goto CNS_DOUBLE;
                            default:
                                assert(!"BAD_TYP");
                                break;
                        }
                        return tree;

                    default:
                        return tree;
                }

                goto CNS_LONG;

            case TYP_FLOAT:
            case TYP_DOUBLE:
                assert(op1->gtOper == GT_CNS_DBL);

                /* Fold constant DOUBLE unary operator */

                d1 = op1->gtDblCon.gtDconVal;

                switch (tree->gtOper)
                {
                    case GT_NEG:
#ifdef LEGACY_BACKEND
                    case GT_CHS:
#endif
                        d1 = -d1;
                        break;

                    case GT_CAST:

                        if (tree->gtOverflowEx())
                        {
                            return tree;
                        }

                        assert(genActualType(tree->CastToType()) == tree->gtType);

                        if ((op1->gtType == TYP_FLOAT && !_finite(forceCastToFloat(d1))) ||
                            (op1->gtType == TYP_DOUBLE && !_finite(d1)))
                        {
                            // The floating point constant is not finite.  The ECMA spec says, in
                            // III 3.27, that "...if overflow occurs converting a floating point type
                            // to an integer, ..., the value returned is unspecified."  However, it would
                            // at least be desirable to have the same value returned for casting an overflowing
                            // constant to an int as would obtained by passing that constant as a parameter
                            // then casting that parameter to an int type.  We will assume that the C compiler's
                            // cast logic will yield the desired result (and trust testing to tell otherwise).
                            // Cross-compilation is an issue here; if that becomes an important scenario, we should
                            // capture the target-specific values of overflow casts to the various integral types as
                            // constants in a target-specific function.
                            CLANG_FORMAT_COMMENT_ANCHOR;

                            // Don't fold conversions of +inf/-inf to integral value on all platforms
                            // as the value returned by JIT helper doesn't match with the C compiler's cast result.
                            // We want the behavior to be same with or without folding.
                            return tree;
                        }

                        if (d1 <= -1.0 && varTypeIsUnsigned(tree->CastToType()))
                        {
                            // Don't fold conversions of these cases becasue the result is unspecified per ECMA spec
                            // and the native math doing the fold doesn't match the run-time computation on all
                            // platforms.
                            // We want the behavior to be same with or without folding.
                            return tree;
                        }

                        switch (tree->CastToType())
                        {
                            case TYP_BYTE:
                                i1 = INT32(INT8(d1));
                                goto CNS_INT;

                            case TYP_SHORT:
                                i1 = INT32(INT16(d1));
                                goto CNS_INT;

                            case TYP_CHAR:
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
                                if (op1->gtType == TYP_FLOAT)
                                {
                                    d1 = forceCastToFloat(d1); // truncate precision
                                }
                                goto CNS_DOUBLE; // redundant cast

                            default:
                                assert(!"BAD_TYP");
                                break;
                        }
                        return tree;

                    default:
                        return tree;
                }
                goto CNS_DOUBLE;

            default:
                /* not a foldable typ - e.g. RET const */
                return tree;
        }
    }

    /* We have a binary operator */

    assert(kind & GTK_BINOP);
    assert(op2);
    assert(op1->OperKind() & GTK_CONST);
    assert(op2->OperKind() & GTK_CONST);

    if (tree->gtOper == GT_COMMA)
    {
        return op2;
    }

    if (tree->OperIsAnyList())
    {
        return tree;
    }

    switchType = op1->gtType;

    // Normally we will just switch on op1 types, but for the case where
    //  only op2 is a GC type and op1 is not a GC type, we use the op2 type.
    //  This makes us handle this as a case of folding for GC type.
    //
    if (varTypeIsGC(op2->gtType) && !varTypeIsGC(op1->gtType))
    {
        switchType = op2->gtType;
    }

    switch (switchType)
    {

        /*-------------------------------------------------------------------------
         * Fold constant REF of BYREF binary operator
         * These can only be comparisons or null pointers
         */

        case TYP_REF:

            /* String nodes are an RVA at this point */

            if (op1->gtOper == GT_CNS_STR || op2->gtOper == GT_CNS_STR)
            {
                return tree;
            }

            __fallthrough;

        case TYP_BYREF:

            i1 = op1->gtIntConCommon.IconValue();
            i2 = op2->gtIntConCommon.IconValue();

            switch (tree->gtOper)
            {
                case GT_EQ:
                    i1 = (i1 == i2);
                    goto FOLD_COND;

                case GT_NE:
                    i1 = (i1 != i2);
                    goto FOLD_COND;

                case GT_ADD:
                    noway_assert(tree->gtType != TYP_REF);
                    // We only fold a GT_ADD that involves a null reference.
                    if (((op1->TypeGet() == TYP_REF) && (i1 == 0)) || ((op2->TypeGet() == TYP_REF) && (i2 == 0)))
                    {
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("\nFolding operator with constant nodes into a constant:\n");
                            gtDispTree(tree);
                        }
#endif
                        // Fold into GT_IND of null byref
                        tree->ChangeOperConst(GT_CNS_INT);
                        tree->gtType              = TYP_BYREF;
                        tree->gtIntCon.gtIconVal  = 0;
                        tree->gtIntCon.gtFieldSeq = FieldSeqStore::NotAField();
                        if (vnStore != nullptr)
                        {
                            fgValueNumberTreeConst(tree);
                        }
#ifdef DEBUG
                        if (verbose)
                        {
                            printf("\nFolded to null byref:\n");
                            gtDispTree(tree);
                        }
#endif
                        goto DONE;
                    }

                default:
                    break;
            }

            return tree;

        /*-------------------------------------------------------------------------
         * Fold constant INT binary operator
         */

        case TYP_INT:

            if (tree->OperIsCompare() && (tree->gtType == TYP_BYTE))
            {
                tree->gtType = TYP_INT;
            }

            assert(tree->gtType == TYP_INT || varTypeIsGC(tree->TypeGet()) || tree->gtOper == GT_MKREFANY);

            // No GC pointer types should be folded here...
            //
            assert(!varTypeIsGC(op1->gtType) && !varTypeIsGC(op2->gtType));

            assert(op1->gtIntConCommon.ImmedValCanBeFolded(this, tree->OperGet()));
            assert(op2->gtIntConCommon.ImmedValCanBeFolded(this, tree->OperGet()));

            i1 = op1->gtIntConCommon.IconValue();
            i2 = op2->gtIntConCommon.IconValue();

            switch (tree->gtOper)
            {
                case GT_EQ:
                    i1 = (INT32(i1) == INT32(i2));
                    break;
                case GT_NE:
                    i1 = (INT32(i1) != INT32(i2));
                    break;

                case GT_LT:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT32(i1) < UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) < INT32(i2));
                    }
                    break;

                case GT_LE:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT32(i1) <= UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) <= INT32(i2));
                    }
                    break;

                case GT_GE:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT32(i1) >= UINT32(i2));
                    }
                    else
                    {
                        i1 = (INT32(i1) >= INT32(i2));
                    }
                    break;

                case GT_GT:
                    if (tree->gtFlags & GTF_UNSIGNED)
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
                    if (tree->gtOverflow())
                    {
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            if (INT64(UINT32(itemp)) != INT64(UINT32(i1)) + INT64(UINT32(i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                        else
                        {
                            if (INT64(INT32(itemp)) != INT64(INT32(i1)) + INT64(INT32(i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                    }
                    i1       = itemp;
                    fieldSeq = GetFieldSeqStore()->Append(op1->gtIntCon.gtFieldSeq, op2->gtIntCon.gtFieldSeq);
                    break;
                case GT_SUB:
                    itemp = i1 - i2;
                    if (tree->gtOverflow())
                    {
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            if (INT64(UINT32(itemp)) != ((INT64)((UINT32)i1) - (INT64)((UINT32)i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                        else
                        {
                            if (INT64(INT32(itemp)) != INT64(INT32(i1)) - INT64(INT32(i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                    }
                    i1 = itemp;
                    break;
                case GT_MUL:
                    itemp = i1 * i2;
                    if (tree->gtOverflow())
                    {
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            if (INT64(UINT32(itemp)) != ((INT64)((UINT32)i1) * (INT64)((UINT32)i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                        else
                        {
                            if (INT64(INT32(itemp)) != INT64(INT32(i1)) * INT64(INT32(i2)))
                            {
                                goto INT_OVF;
                            }
                        }
                    }
                    // For the very particular case of the "constant array index" pseudo-field, we
                    // assume that multiplication is by the field width, and preserves that field.
                    // This could obviously be made more robust by a more complicated set of annotations...
                    if ((op1->gtIntCon.gtFieldSeq != nullptr) && op1->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq())
                    {
                        assert(op2->gtIntCon.gtFieldSeq == FieldSeqStore::NotAField());
                        fieldSeq = op1->gtIntCon.gtFieldSeq;
                    }
                    else if ((op2->gtIntCon.gtFieldSeq != nullptr) &&
                             op2->gtIntCon.gtFieldSeq->IsConstantIndexFieldSeq())
                    {
                        assert(op1->gtIntCon.gtFieldSeq == FieldSeqStore::NotAField());
                        fieldSeq = op2->gtIntCon.gtFieldSeq;
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
                    /* logical shift -> make it unsigned to not propagate the sign bit */
                    i1 = UINT32(i1) >> (i2 & 0x1f);
                    break;
                case GT_ROL:
                    i1 = (i1 << (i2 & 0x1f)) | (UINT32(i1) >> ((32 - i2) & 0x1f));
                    break;
                case GT_ROR:
                    i1 = (i1 << ((32 - i2) & 0x1f)) | (UINT32(i1) >> (i2 & 0x1f));
                    break;

                /* DIV and MOD can generate an INT 0 - if division by 0
                 * or overflow - when dividing MIN by -1 */

                case GT_DIV:
                case GT_MOD:
                case GT_UDIV:
                case GT_UMOD:
                    if (INT32(i2) == 0)
                    {
                        // Division by zero:
                        // We have to evaluate this expression and throw an exception
                        return tree;
                    }
                    else if ((INT32(i2) == -1) && (UINT32(i1) == 0x80000000))
                    {
                        // Overflow Division:
                        // We have to evaluate this expression and throw an exception
                        return tree;
                    }

                    if (tree->gtOper == GT_DIV)
                    {
                        i1 = INT32(i1) / INT32(i2);
                    }
                    else if (tree->gtOper == GT_MOD)
                    {
                        i1 = INT32(i1) % INT32(i2);
                    }
                    else if (tree->gtOper == GT_UDIV)
                    {
                        i1 = UINT32(i1) / UINT32(i2);
                    }
                    else
                    {
                        assert(tree->gtOper == GT_UMOD);
                        i1 = UINT32(i1) % UINT32(i2);
                    }
                    break;

                default:
                    return tree;
            }

        /* We get here after folding to a GT_CNS_INT type
         * change the node to the new type / value and make sure the node sizes are OK */
        CNS_INT:
        FOLD_COND:

#ifdef DEBUG
            if (verbose)
            {
                printf("\nFolding operator with constant nodes into a constant:\n");
                gtDispTree(tree);
            }
#endif

#ifdef _TARGET_64BIT_
            // Some operations are performed as 64 bit instead of 32 bit so the upper 32 bits
            // need to be discarded. Since constant values are stored as ssize_t and the node
            // has TYP_INT the result needs to be sign extended rather than zero extended.
            i1 = INT32(i1);
#endif // _TARGET_64BIT_

            /* Also all conditional folding jumps here since the node hanging from
             * GT_JTRUE has to be a GT_CNS_INT - value 0 or 1 */

            tree->ChangeOperConst(GT_CNS_INT);
            tree->gtType              = TYP_INT;
            tree->gtIntCon.gtIconVal  = i1;
            tree->gtIntCon.gtFieldSeq = fieldSeq;
            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }
#ifdef DEBUG
            if (verbose)
            {
                printf("Bashed to int constant:\n");
                gtDispTree(tree);
            }
#endif
            goto DONE;

        /* This operation is going to cause an overflow exception. Morph into
           an overflow helper. Put a dummy constant value for code generation.

           We could remove all subsequent trees in the current basic block,
           unless this node is a child of GT_COLON

           NOTE: Since the folded value is not constant we should not change the
                 "tree" node - otherwise we confuse the logic that checks if the folding
                 was successful - instead use one of the operands, e.g. op1
         */

        LNG_OVF:
            // Don't fold overflow operations if not global morph phase.
            // The reason for this is that this optimization is replacing a gentree node
            // with another new gentree node. Say a GT_CALL(arglist) has one 'arg'
            // involving overflow arithmetic.  During assertion prop, it is possible
            // that the 'arg' could be constant folded and the result could lead to an
            // overflow.  In such a case 'arg' will get replaced with GT_COMMA node
            // but fgMorphArgs() - see the logic around "if(lateArgsComputed)" - doesn't
            // update args table. For this reason this optimization is enabled only
            // for global morphing phase.
            //
            // X86/Arm32 legacy codegen note: This is not an issue on x86 for the reason that
            // it doesn't use arg table for calls.  In addition x86/arm32 legacy codegen doesn't
            // expect long constants to show up as an operand of overflow cast operation.
            //
            // TODO-CQ: Once fgMorphArgs() is fixed this restriction could be removed.
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifndef LEGACY_BACKEND
            if (!fgGlobalMorph)
            {
                assert(tree->gtOverflow());
                return tree;
            }
#endif // !LEGACY_BACKEND

            op1 = gtNewLconNode(0);
            if (vnStore != nullptr)
            {
                op1->gtVNPair.SetBoth(vnStore->VNZeroForType(TYP_LONG));
            }
            goto OVF;

        INT_OVF:
#ifndef LEGACY_BACKEND
            // Don't fold overflow operations if not global morph phase.
            // The reason for this is that this optimization is replacing a gentree node
            // with another new gentree node. Say a GT_CALL(arglist) has one 'arg'
            // involving overflow arithmetic.  During assertion prop, it is possible
            // that the 'arg' could be constant folded and the result could lead to an
            // overflow.  In such a case 'arg' will get replaced with GT_COMMA node
            // but fgMorphArgs() - see the logic around "if(lateArgsComputed)" - doesn't
            // update args table. For this reason this optimization is enabled only
            // for global morphing phase.
            //
            // X86/Arm32 legacy codegen note: This is not an issue on x86 for the reason that
            // it doesn't use arg table for calls.  In addition x86/arm32 legacy codegen doesn't
            // expect long constants to show up as an operand of overflow cast operation.
            //
            // TODO-CQ: Once fgMorphArgs() is fixed this restriction could be removed.

            if (!fgGlobalMorph)
            {
                assert(tree->gtOverflow());
                return tree;
            }
#endif // !LEGACY_BACKEND

            op1 = gtNewIconNode(0);
            if (vnStore != nullptr)
            {
                op1->gtVNPair.SetBoth(vnStore->VNZeroForType(TYP_INT));
            }
            goto OVF;

        OVF:
#ifdef DEBUG
            if (verbose)
            {
                printf("\nFolding binary operator with constant nodes into a comma throw:\n");
                gtDispTree(tree);
            }
#endif
            /* We will change the cast to a GT_COMMA and attach the exception helper as gtOp.gtOp1.
             * The constant expression zero becomes op2. */

            assert(tree->gtOverflow());
            assert(tree->gtOper == GT_ADD || tree->gtOper == GT_SUB || tree->gtOper == GT_CAST ||
                   tree->gtOper == GT_MUL);
            assert(op1);

            op2 = op1;
            op1 = gtNewHelperCallNode(CORINFO_HELP_OVERFLOW, TYP_VOID,
                                      gtNewArgList(gtNewIconNode(compCurBB->bbTryIndex)));

            if (vnStore != nullptr)
            {
                op1->gtVNPair =
                    vnStore->VNPWithExc(ValueNumPair(ValueNumStore::VNForVoid(), ValueNumStore::VNForVoid()),
                                        vnStore->VNPExcSetSingleton(vnStore->VNPairForFunc(TYP_REF, VNF_OverflowExc)));
            }

            tree = gtNewOperNode(GT_COMMA, tree->gtType, op1, op2);

            return tree;

        /*-------------------------------------------------------------------------
         * Fold constant LONG binary operator
         */

        case TYP_LONG:

            // No GC pointer types should be folded here...
            //
            assert(!varTypeIsGC(op1->gtType) && !varTypeIsGC(op2->gtType));

            // op1 is known to be a TYP_LONG, op2 is normally a TYP_LONG, unless we have a shift operator in which case
            // it is a TYP_INT
            //
            assert((op2->gtType == TYP_LONG) || (op2->gtType == TYP_INT));

            assert(op1->gtIntConCommon.ImmedValCanBeFolded(this, tree->OperGet()));
            assert(op2->gtIntConCommon.ImmedValCanBeFolded(this, tree->OperGet()));

            lval1 = op1->gtIntConCommon.LngValue();

            // For the shift operators we can have a op2 that is a TYP_INT and thus will be GT_CNS_INT
            if (op2->OperGet() == GT_CNS_INT)
            {
                lval2 = op2->gtIntConCommon.IconValue();
            }
            else
            {
                lval2 = op2->gtIntConCommon.LngValue();
            }

            switch (tree->gtOper)
            {
                case GT_EQ:
                    i1 = (lval1 == lval2);
                    goto FOLD_COND;
                case GT_NE:
                    i1 = (lval1 != lval2);
                    goto FOLD_COND;

                case GT_LT:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT64(lval1) < UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 < lval2);
                    }
                    goto FOLD_COND;

                case GT_LE:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT64(lval1) <= UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 <= lval2);
                    }
                    goto FOLD_COND;

                case GT_GE:
                    if (tree->gtFlags & GTF_UNSIGNED)
                    {
                        i1 = (UINT64(lval1) >= UINT64(lval2));
                    }
                    else
                    {
                        i1 = (lval1 >= lval2);
                    }
                    goto FOLD_COND;

                case GT_GT:
                    if (tree->gtFlags & GTF_UNSIGNED)
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

                LNG_ADD_CHKOVF:
                    /* For the SIGNED case - If there is one positive and one negative operand, there can be no overflow
                     * If both are positive, the result has to be positive, and similary for negatives.
                     *
                     * For the UNSIGNED case - If a UINT32 operand is bigger than the result then OVF */

                    if (tree->gtOverflow())
                    {
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            if ((UINT64(lval1) > UINT64(ltemp)) || (UINT64(lval2) > UINT64(ltemp)))
                            {
                                goto LNG_OVF;
                            }
                        }
                        else if (((lval1 < 0) == (lval2 < 0)) && ((lval1 < 0) != (ltemp < 0)))
                        {
                            goto LNG_OVF;
                        }
                    }
                    lval1 = ltemp;
                    break;

                case GT_SUB:
                    ltemp = lval1 - lval2;
                    if (tree->gtOverflow())
                    {
                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            if (UINT64(lval2) > UINT64(lval1))
                            {
                                goto LNG_OVF;
                            }
                        }
                        else
                        {
                            /* If both operands are +ve or both are -ve, there can be no
                               overflow. Else use the logic for : lval1 + (-lval2) */

                            if ((lval1 < 0) != (lval2 < 0))
                            {
                                if (lval2 == INT64_MIN)
                                {
                                    goto LNG_OVF;
                                }
                                lval2 = -lval2;
                                goto LNG_ADD_CHKOVF;
                            }
                        }
                    }
                    lval1 = ltemp;
                    break;

                case GT_MUL:
                    ltemp = lval1 * lval2;

                    if (tree->gtOverflow() && lval2 != 0)
                    {

                        if (tree->gtFlags & GTF_UNSIGNED)
                        {
                            UINT64 ultemp = ltemp;
                            UINT64 ulval1 = lval1;
                            UINT64 ulval2 = lval2;
                            if ((ultemp / ulval2) != ulval1)
                            {
                                goto LNG_OVF;
                            }
                        }
                        else
                        {
                            // This does a multiply and then reverses it.  This test works great except for MIN_INT *
                            //-1.  In that case we mess up the sign on ltmp.  Make sure to double check the sign.
                            // if either is 0, then no overflow
                            if (lval1 != 0) // lval2 checked above.
                            {
                                if (((lval1 < 0) == (lval2 < 0)) && (ltemp < 0))
                                {
                                    goto LNG_OVF;
                                }
                                if (((lval1 < 0) != (lval2 < 0)) && (ltemp > 0))
                                {
                                    goto LNG_OVF;
                                }

                                // TODO-Amd64-Unix: Remove the code that disables optimizations for this method when the
                                // clang
                                // optimizer is fixed and/or the method implementation is refactored in a simpler code.
                                // There is a bug in the clang-3.5 optimizer. The issue is that in release build the
                                // optimizer is mistyping (or just wrongly decides to use 32 bit operation for a corner
                                // case of MIN_LONG) the args of the (ltemp / lval2) to int (it does a 32 bit div
                                // operation instead of 64 bit.). For the case of lval1 and lval2 equal to MIN_LONG
                                // (0x8000000000000000) this results in raising a SIGFPE.
                                // Optimizations disabled for now. See compiler.h.
                                if ((ltemp / lval2) != lval1)
                                {
                                    goto LNG_OVF;
                                }
                            }
                        }
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
                    /* logical shift -> make it unsigned to not propagate the sign bit */
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
                    if (!lval2)
                    {
                        return tree;
                    }

                    if (UINT64(lval1) == UI64(0x8000000000000000) && lval2 == INT64(-1))
                    {
                        return tree;
                    }
                    lval1 /= lval2;
                    break;

                case GT_MOD:
                    if (!lval2)
                    {
                        return tree;
                    }
                    if (UINT64(lval1) == UI64(0x8000000000000000) && lval2 == INT64(-1))
                    {
                        return tree;
                    }
                    lval1 %= lval2;
                    break;

                case GT_UDIV:
                    if (!lval2)
                    {
                        return tree;
                    }
                    if (UINT64(lval1) == UI64(0x8000000000000000) && lval2 == INT64(-1))
                    {
                        return tree;
                    }
                    lval1 = UINT64(lval1) / UINT64(lval2);
                    break;

                case GT_UMOD:
                    if (!lval2)
                    {
                        return tree;
                    }
                    if (UINT64(lval1) == UI64(0x8000000000000000) && lval2 == INT64(-1))
                    {
                        return tree;
                    }
                    lval1 = UINT64(lval1) % UINT64(lval2);
                    break;
                default:
                    return tree;
            }

        CNS_LONG:

            if (fieldSeq != FieldSeqStore::NotAField())
            {
                return tree;
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("\nFolding long operator with constant nodes into a constant:\n");
                gtDispTree(tree);
            }
#endif
            assert((GenTree::s_gtNodeSizes[GT_CNS_NATIVELONG] == TREE_NODE_SZ_SMALL) ||
                   (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE));

            tree->ChangeOperConst(GT_CNS_NATIVELONG);
            tree->gtIntConCommon.SetLngValue(lval1);
            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }

#ifdef DEBUG
            if (verbose)
            {
                printf("Bashed to long constant:\n");
                gtDispTree(tree);
            }
#endif
            goto DONE;

        /*-------------------------------------------------------------------------
         * Fold constant FLOAT or DOUBLE binary operator
         */

        case TYP_FLOAT:
        case TYP_DOUBLE:

            if (tree->gtOverflowEx())
            {
                return tree;
            }

            assert(op1->gtOper == GT_CNS_DBL);
            d1 = op1->gtDblCon.gtDconVal;

            assert(varTypeIsFloating(op2->gtType));
            assert(op2->gtOper == GT_CNS_DBL);
            d2 = op2->gtDblCon.gtDconVal;

            /* Special case - check if we have NaN operands.
             * For comparisons if not an unordered operation always return 0.
             * For unordered operations (i.e. the GTF_RELOP_NAN_UN flag is set)
             * the result is always true - return 1. */

            if (_isnan(d1) || _isnan(d2))
            {
#ifdef DEBUG
                if (verbose)
                {
                    printf("Double operator(s) is NaN\n");
                }
#endif
                if (tree->OperKind() & GTK_RELOP)
                {
                    if (tree->gtFlags & GTF_RELOP_NAN_UN)
                    {
                        /* Unordered comparison with NaN always succeeds */
                        i1 = 1;
                        goto FOLD_COND;
                    }
                    else
                    {
                        /* Normal comparison with NaN always fails */
                        i1 = 0;
                        goto FOLD_COND;
                    }
                }
            }

            switch (tree->gtOper)
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

#if FEATURE_STACK_FP_X87
                case GT_ADD:
                    d1 += d2;
                    break;
                case GT_SUB:
                    d1 -= d2;
                    break;
                case GT_MUL:
                    d1 *= d2;
                    break;
                case GT_DIV:
                    if (!d2)
                        return tree;
                    d1 /= d2;
                    break;
#else  //! FEATURE_STACK_FP_X87
                // non-x86 arch: floating point arithmetic should be done in declared
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
                // flaot c = b/b;   This will produce NaN in single precision and 1 in double precision.
                case GT_ADD:
                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = f1 + f2;
                    }
                    else
                    {
                        d1 += d2;
                    }
                    break;

                case GT_SUB:
                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = f1 - f2;
                    }
                    else
                    {
                        d1 -= d2;
                    }
                    break;

                case GT_MUL:
                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = f1 * f2;
                    }
                    else
                    {
                        d1 *= d2;
                    }
                    break;

                case GT_DIV:
                    if (!d2)
                    {
                        return tree;
                    }
                    if (op1->TypeGet() == TYP_FLOAT)
                    {
                        f1 = forceCastToFloat(d1);
                        f2 = forceCastToFloat(d2);
                        d1 = f1 / f2;
                    }
                    else
                    {
                        d1 /= d2;
                    }
                    break;
#endif //! FEATURE_STACK_FP_X87

                default:
                    return tree;
            }

        CNS_DOUBLE:

#ifdef DEBUG
            if (verbose)
            {
                printf("\nFolding fp operator with constant nodes into a fp constant:\n");
                gtDispTree(tree);
            }
#endif

            assert((GenTree::s_gtNodeSizes[GT_CNS_DBL] == TREE_NODE_SZ_SMALL) ||
                   (tree->gtDebugFlags & GTF_DEBUG_NODE_LARGE));

            tree->ChangeOperConst(GT_CNS_DBL);
            tree->gtDblCon.gtDconVal = d1;
            if (vnStore != nullptr)
            {
                fgValueNumberTreeConst(tree);
            }
#ifdef DEBUG
            if (verbose)
            {
                printf("Bashed to fp constant:\n");
                gtDispTree(tree);
            }
#endif
            goto DONE;

        default:
            /* not a foldable typ */
            return tree;
    }

//-------------------------------------------------------------------------

DONE:

    /* Make sure no side effect flags are set on this constant node */

    tree->gtFlags &= ~GTF_ALL_EFFECT;

    return tree;
}
#ifdef _PREFAST_
#pragma warning(pop)
#endif

//------------------------------------------------------------------------
// gtNewTempAssign: Create an assignment of the given value to a temp.
//
// Arguments:
//    tmp - local number for a compiler temp
//    val - value to assign to the temp
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

GenTreePtr Compiler::gtNewTempAssign(unsigned tmp, GenTreePtr val)
{
    // Self-assignment is a nop.
    if (val->OperGet() == GT_LCL_VAR && val->gtLclVarCommon.gtLclNum == tmp)
    {
        return gtNewNothingNode();
    }

    LclVarDsc* varDsc = lvaTable + tmp;

    if (varDsc->TypeGet() == TYP_I_IMPL && val->TypeGet() == TYP_BYREF)
    {
        impBashVarAddrsToI(val);
    }

    var_types valTyp = val->TypeGet();
    if (val->OperGet() == GT_LCL_VAR && lvaTable[val->gtLclVar.gtLclNum].lvNormalizeOnLoad())
    {
        valTyp      = lvaGetRealType(val->gtLclVar.gtLclNum);
        val->gtType = valTyp;
    }
    var_types dstTyp = varDsc->TypeGet();

    /* If the variable's lvType is not yet set then set it here */
    if (dstTyp == TYP_UNDEF)
    {
        varDsc->lvType = dstTyp = genActualType(valTyp);
        if (varTypeIsGC(dstTyp))
        {
            varDsc->lvStructGcCount = 1;
        }
#if FEATURE_SIMD
        else if (varTypeIsSIMD(dstTyp))
        {
            varDsc->lvSIMDType = 1;
        }
#endif
    }

#ifdef DEBUG
    /* Make sure the actual types match               */
    if (genActualType(valTyp) != genActualType(dstTyp))
    {
        // Plus some other exceptions that are apparently legal:
        // 1) TYP_REF or BYREF = TYP_I_IMPL
        bool ok = false;
        if (varTypeIsGC(dstTyp) && (valTyp == TYP_I_IMPL))
        {
            ok = true;
        }
        // 2) TYP_DOUBLE = TYP_FLOAT or TYP_FLOAT = TYP_DOUBLE
        else if (varTypeIsFloating(dstTyp) && varTypeIsFloating(valTyp))
        {
            ok = true;
        }

        if (!ok)
        {
            gtDispTree(val);
            assert(!"Incompatible types for gtNewTempAssign");
        }
    }
#endif

    // Floating Point assignments can be created during inlining
    // see "Zero init inlinee locals:" in fgInlinePrependStatements
    // thus we may need to set compFloatingPointUsed to true here.
    //
    if (varTypeIsFloating(dstTyp) && (compFloatingPointUsed == false))
    {
        compFloatingPointUsed = true;
    }

    /* Create the assignment node */

    GenTreePtr asg;
    GenTreePtr dest = gtNewLclvNode(tmp, dstTyp);
    dest->gtFlags |= GTF_VAR_DEF;

    // With first-class structs, we should be propagating the class handle on all non-primitive
    // struct types. We don't have a convenient way to do that for all SIMD temps, since some
    // internal trees use SIMD types that are not used by the input IL. In this case, we allow
    // a null type handle and derive the necessary information about the type from its varType.
    CORINFO_CLASS_HANDLE structHnd = gtGetStructHandleIfPresent(val);
    if (varTypeIsStruct(valTyp) && ((structHnd != NO_CLASS_HANDLE) || (varTypeIsSIMD(valTyp))))
    {
        // The GT_OBJ may be be a child of a GT_COMMA.
        GenTreePtr valx = val->gtEffectiveVal(/*commaOnly*/ true);

        if (valx->gtOper == GT_OBJ)
        {
            assert(structHnd != nullptr);
            lvaSetStruct(tmp, structHnd, false);
        }
        dest->gtFlags |= GTF_DONT_CSE;
        valx->gtFlags |= GTF_DONT_CSE;
        asg = impAssignStruct(dest, val, structHnd, (unsigned)CHECK_SPILL_NONE);
    }
    else
    {
        asg = gtNewAssignNode(dest, val);
    }

#ifndef LEGACY_BACKEND
    if (compRationalIRForm)
    {
        Rationalizer::RewriteAssignmentIntoStoreLcl(asg->AsOp());
    }
#endif // !LEGACY_BACKEND

    return asg;
}

/*****************************************************************************
 *
 *  Create a helper call to access a COM field (iff 'assg' is non-zero this is
 *  an assignment and 'assg' is the new value).
 */

GenTreePtr Compiler::gtNewRefCOMfield(GenTreePtr              objPtr,
                                      CORINFO_RESOLVED_TOKEN* pResolvedToken,
                                      CORINFO_ACCESS_FLAGS    access,
                                      CORINFO_FIELD_INFO*     pFieldInfo,
                                      var_types               lclTyp,
                                      CORINFO_CLASS_HANDLE    structType,
                                      GenTreePtr              assg)
{
    assert(pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER ||
           pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_ADDR_HELPER ||
           pFieldInfo->fieldAccessor == CORINFO_FIELD_STATIC_ADDR_HELPER);

    /* If we can't access it directly, we need to call a helper function */
    GenTreeArgList* args       = nullptr;
    var_types       helperType = TYP_BYREF;

    if (pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER)
    {
        if (access & CORINFO_ACCESS_SET)
        {
            assert(assg != nullptr);
            // helper needs pointer to struct, not struct itself
            if (pFieldInfo->helper == CORINFO_HELP_SETFIELDSTRUCT)
            {
                assert(structType != nullptr);
                assg = impGetStructAddr(assg, structType, (unsigned)CHECK_SPILL_ALL, true);
            }
            else if (lclTyp == TYP_DOUBLE && assg->TypeGet() == TYP_FLOAT)
            {
                assg = gtNewCastNode(TYP_DOUBLE, assg, TYP_DOUBLE);
            }
            else if (lclTyp == TYP_FLOAT && assg->TypeGet() == TYP_DOUBLE)
            {
                assg = gtNewCastNode(TYP_FLOAT, assg, TYP_FLOAT);
            }

            args       = gtNewArgList(assg);
            helperType = TYP_VOID;
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
        args = gtNewListNode(gtNewIconEmbClsHndNode(pFieldInfo->structType), args);
    }

    GenTreePtr fieldHnd = impTokenToHandle(pResolvedToken);
    if (fieldHnd == nullptr)
    { // compDonotInline()
        return nullptr;
    }

    args = gtNewListNode(fieldHnd, args);

    // If it's a static field, we shouldn't have an object node
    // If it's an instance field, we have an object node
    assert((pFieldInfo->fieldAccessor != CORINFO_FIELD_STATIC_ADDR_HELPER) ^ (objPtr == nullptr));

    if (objPtr != nullptr)
    {
        args = gtNewListNode(objPtr, args);
    }

    GenTreePtr tree = gtNewHelperCallNode(pFieldInfo->helper, genActualType(helperType), args);

    if (pFieldInfo->fieldAccessor == CORINFO_FIELD_INSTANCE_HELPER)
    {
        if (access & CORINFO_ACCESS_GET)
        {
            if (pFieldInfo->helper == CORINFO_HELP_GETFIELDSTRUCT)
            {
                if (!varTypeIsStruct(lclTyp))
                {
                    // get the result as primitive type
                    tree = impGetStructAddr(tree, structType, (unsigned)CHECK_SPILL_ALL, true);
                    tree = gtNewOperNode(GT_IND, lclTyp, tree);
                }
            }
            else if (varTypeIsIntegral(lclTyp) && genTypeSize(lclTyp) < genTypeSize(TYP_INT))
            {
                // The helper does not extend the small return types.
                tree = gtNewCastNode(genActualType(lclTyp), tree, lclTyp);
            }
        }
    }
    else
    {
        // OK, now do the indirection
        if (access & CORINFO_ACCESS_GET)
        {
            if (varTypeIsStruct(lclTyp))
            {
                tree = gtNewObjNode(structType, tree);
            }
            else
            {
                tree = gtNewOperNode(GT_IND, lclTyp, tree);
            }
            tree->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF);
        }
        else if (access & CORINFO_ACCESS_SET)
        {
            if (varTypeIsStruct(lclTyp))
            {
                tree = impAssignStructPtr(tree, assg, structType, (unsigned)CHECK_SPILL_ALL);
            }
            else
            {
                tree = gtNewOperNode(GT_IND, lclTyp, tree);
                tree->gtFlags |= (GTF_EXCEPT | GTF_GLOB_REF | GTF_IND_TGTANYWHERE);
                tree = gtNewAssignNode(tree, assg);
            }
        }
    }

    return (tree);
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

bool Compiler::gtNodeHasSideEffects(GenTreePtr tree, unsigned flags)
{
    if (flags & GTF_ASG)
    {
        if (tree->OperIsAssignment())
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
                for (GenTreeArgList* args = call->gtCallArgs; args != nullptr; args = args->Rest())
                {
                    if (gtTreeHasSideEffects(args->Current(), flags))
                    {
                        return true;
                    }
                }
                // I'm a little worried that args that assign to temps that are late args will look like
                // side effects...but better to be conservative for now.
                for (GenTreeArgList* args = call->gtCallLateArgs; args != nullptr; args = args->Rest())
                {
                    if (gtTreeHasSideEffects(args->Current(), flags))
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

bool Compiler::gtTreeHasSideEffects(GenTreePtr tree, unsigned flags /* = GTF_SIDE_EFFECT*/)
{
    // These are the side effect flags that we care about for this tree
    unsigned sideEffectFlags = tree->gtFlags & flags;

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
            if (tree->gtCall.gtCallType == CT_HELPER)
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

            if (gtNodeHasSideEffects(tree->gtOp.gtOp1, flags))
            {
                return true;
            }

            if ((tree->gtOp.gtOp2 != nullptr) && gtNodeHasSideEffects(tree->gtOp.gtOp2, flags))
            {
                return true;
            }

            return false;
        }
    }

    return true;
}

GenTreePtr Compiler::gtBuildCommaList(GenTreePtr list, GenTreePtr expr)
{
    // 'list' starts off as null,
    //        and when it is null we haven't started the list yet.
    //
    if (list != nullptr)
    {
        // Create a GT_COMMA that appends 'expr' in front of the remaining set of expressions in (*list)
        GenTreePtr result = gtNewOperNode(GT_COMMA, TYP_VOID, expr, list);

        // Set the flags in the comma node
        result->gtFlags |= (list->gtFlags & GTF_ALL_EFFECT);
        result->gtFlags |= (expr->gtFlags & GTF_ALL_EFFECT);

        // 'list' and 'expr' should have valuenumbers defined for both or for neither one (unless we are remorphing,
        // in which case a prior transform involving either node may have discarded or otherwise invalidated the value
        // numbers).
        assert((list->gtVNPair.BothDefined() == expr->gtVNPair.BothDefined()) || !fgGlobalMorph);

        // Set the ValueNumber 'gtVNPair' for the new GT_COMMA node
        //
        if (list->gtVNPair.BothDefined() && expr->gtVNPair.BothDefined())
        {
            // The result of a GT_COMMA node is op2, the normal value number is op2vnp
            // But we also need to include the union of side effects from op1 and op2.
            // we compute this value into exceptions_vnp.
            ValueNumPair op1vnp;
            ValueNumPair op1Xvnp = ValueNumStore::VNPForEmptyExcSet();
            ValueNumPair op2vnp;
            ValueNumPair op2Xvnp = ValueNumStore::VNPForEmptyExcSet();

            vnStore->VNPUnpackExc(expr->gtVNPair, &op1vnp, &op1Xvnp);
            vnStore->VNPUnpackExc(list->gtVNPair, &op2vnp, &op2Xvnp);

            ValueNumPair exceptions_vnp = ValueNumStore::VNPForEmptyExcSet();

            exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op1Xvnp);
            exceptions_vnp = vnStore->VNPExcSetUnion(exceptions_vnp, op2Xvnp);

            result->gtVNPair = vnStore->VNPWithExc(op2vnp, exceptions_vnp);
        }

        return result;
    }
    else
    {
        // The 'expr' will start the list of expressions
        return expr;
    }
}

/*****************************************************************************
 *
 *  Extracts side effects from the given expression
 *  and appends them to a given list (actually a GT_COMMA list)
 *  If ignore root is specified, the method doesn't treat the top
 *  level tree node as having side-effect.
 */

void Compiler::gtExtractSideEffList(GenTreePtr  expr,
                                    GenTreePtr* pList,
                                    unsigned    flags /* = GTF_SIDE_EFFECT*/,
                                    bool        ignoreRoot /* = false */)
{
    assert(expr);
    assert(expr->gtOper != GT_STMT);

    /* If no side effect in the expression return */

    if (!gtTreeHasSideEffects(expr, flags))
    {
        return;
    }

    genTreeOps oper = expr->OperGet();
    unsigned   kind = expr->OperKind();

    // Look for any side effects that we care about
    //
    if (!ignoreRoot && gtNodeHasSideEffects(expr, flags))
    {
        // Add the side effect to the list and return
        //
        *pList = gtBuildCommaList(*pList, expr);
        return;
    }

    if (kind & GTK_LEAF)
    {
        return;
    }

    if (oper == GT_LOCKADD || oper == GT_XADD || oper == GT_XCHG || oper == GT_CMPXCHG)
    {
        // XADD both adds to the memory location and also fetches the old value.  If we only need the side
        // effect of this instruction, change it into a GT_LOCKADD node (the add only)
        if (oper == GT_XADD)
        {
            expr->SetOperRaw(GT_LOCKADD);
            assert(genActualType(expr->gtType) == genActualType(expr->gtOp.gtOp2->gtType));
            expr->gtType = TYP_VOID;
        }

        // These operations are kind of important to keep
        *pList = gtBuildCommaList(*pList, expr);
        return;
    }

    if (kind & GTK_SMPOP)
    {
        GenTreePtr op1 = expr->gtOp.gtOp1;
        GenTreePtr op2 = expr->gtGetOp2IfPresent();

        if (flags & GTF_EXCEPT)
        {
            // Special case - GT_ADDR of GT_IND nodes of TYP_STRUCT
            // have to be kept together

            if (oper == GT_ADDR && op1->OperIsIndir() && op1->gtType == TYP_STRUCT)
            {
                *pList = gtBuildCommaList(*pList, expr);

#ifdef DEBUG
                if (verbose)
                {
                    printf("Keep the GT_ADDR and GT_IND together:\n");
                }
#endif
                return;
            }
        }

        /* Continue searching for side effects in the subtrees of the expression
         * NOTE: Be careful to preserve the right ordering - side effects are prepended
         * to the list */

        /* Continue searching for side effects in the subtrees of the expression
         * NOTE: Be careful to preserve the right ordering
         * as side effects are prepended to the list */

        if (expr->gtFlags & GTF_REVERSE_OPS)
        {
            assert(oper != GT_COMMA);
            if (op1)
            {
                gtExtractSideEffList(op1, pList, flags);
            }
            if (op2)
            {
                gtExtractSideEffList(op2, pList, flags);
            }
        }
        else
        {
            if (op2)
            {
                gtExtractSideEffList(op2, pList, flags);
            }
            if (op1)
            {
                gtExtractSideEffList(op1, pList, flags);
            }
        }
    }

    if (expr->OperGet() == GT_CALL)
    {
        // Generally all GT_CALL nodes are considered to have side-effects.
        // So if we get here it must be a Helper call that we decided does
        // not have side effects that we needed to keep
        //
        assert(expr->gtCall.gtCallType == CT_HELPER);

        // We can remove this Helper call, but there still could be
        // side-effects in the arguments that we may need to keep
        //
        GenTreePtr args;
        for (args = expr->gtCall.gtCallArgs; args; args = args->gtOp.gtOp2)
        {
            assert(args->OperIsList());
            gtExtractSideEffList(args->Current(), pList, flags);
        }
        for (args = expr->gtCall.gtCallLateArgs; args; args = args->gtOp.gtOp2)
        {
            assert(args->OperIsList());
            gtExtractSideEffList(args->Current(), pList, flags);
        }
    }

    if (expr->OperGet() == GT_ARR_BOUNDS_CHECK
#ifdef FEATURE_SIMD
        || expr->OperGet() == GT_SIMD_CHK
#endif // FEATURE_SIMD
        )
    {
        gtExtractSideEffList(expr->AsBoundsChk()->gtIndex, pList, flags);
        gtExtractSideEffList(expr->AsBoundsChk()->gtArrLen, pList, flags);
    }

    if (expr->OperGet() == GT_DYN_BLK || expr->OperGet() == GT_STORE_DYN_BLK)
    {
        if (expr->AsDynBlk()->Data() != nullptr)
        {
            gtExtractSideEffList(expr->AsDynBlk()->Data(), pList, flags);
        }
        gtExtractSideEffList(expr->AsDynBlk()->Addr(), pList, flags);
        gtExtractSideEffList(expr->AsDynBlk()->gtDynamicSize, pList, flags);
    }
}

/*****************************************************************************
 *
 *  For debugging only - displays a tree node list and makes sure all the
 *  links are correctly set.
 */

#ifdef DEBUG

void dispNodeList(GenTreePtr list, bool verbose)
{
    GenTreePtr last = nullptr;
    GenTreePtr next;

    if (!list)
    {
        return;
    }

    for (;;)
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

/*****************************************************************************
 * Callback to assert that the nodes of a qmark-colon subtree are marked
 */

/* static */
Compiler::fgWalkResult Compiler::gtAssertColonCond(GenTreePtr* pTree, fgWalkData* data)
{
    assert(data->pCallbackData == nullptr);

    assert((*pTree)->gtFlags & GTF_COLON_COND);

    return WALK_CONTINUE;
}
#endif // DEBUG

/*****************************************************************************
 * Callback to mark the nodes of a qmark-colon subtree that are conditionally
 * executed.
 */

/* static */
Compiler::fgWalkResult Compiler::gtMarkColonCond(GenTreePtr* pTree, fgWalkData* data)
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
Compiler::fgWalkResult Compiler::gtClearColonCond(GenTreePtr* pTree, fgWalkData* data)
{
    GenTreePtr tree = *pTree;

    assert(data->pCallbackData == nullptr);

    if (tree->OperGet() == GT_COLON)
    {
        // Nodes below this will be conditionally executed.
        return WALK_SKIP_SUBTREES;
    }

    tree->gtFlags &= ~GTF_COLON_COND;
    return WALK_CONTINUE;
}

struct FindLinkData
{
    GenTreePtr  nodeToFind;
    GenTreePtr* result;
};

/*****************************************************************************
 *
 *  Callback used by the tree walker to implement fgFindLink()
 */
static Compiler::fgWalkResult gtFindLinkCB(GenTreePtr* pTree, Compiler::fgWalkData* cbData)
{
    FindLinkData* data = (FindLinkData*)cbData->pCallbackData;
    if (*pTree == data->nodeToFind)
    {
        data->result = pTree;
        return Compiler::WALK_ABORT;
    }

    return Compiler::WALK_CONTINUE;
}

GenTreePtr* Compiler::gtFindLink(GenTreePtr stmt, GenTreePtr node)
{
    assert(stmt->gtOper == GT_STMT);

    FindLinkData data = {node, nullptr};

    fgWalkResult result = fgWalkTreePre(&stmt->gtStmt.gtStmtExpr, gtFindLinkCB, &data);

    if (result == WALK_ABORT)
    {
        assert(data.nodeToFind == *data.result);
        return data.result;
    }
    else
    {
        return nullptr;
    }
}

/*****************************************************************************
 *
 *  Callback that checks if a tree node has oper type GT_CATCH_ARG
 */

static Compiler::fgWalkResult gtFindCatchArg(GenTreePtr* pTree, Compiler::fgWalkData* /* data */)
{
    return ((*pTree)->OperGet() == GT_CATCH_ARG) ? Compiler::WALK_ABORT : Compiler::WALK_CONTINUE;
}

/*****************************************************************************/
bool Compiler::gtHasCatchArg(GenTreePtr tree)
{
    if (((tree->gtFlags & GTF_ORDER_SIDEEFF) != 0) && (fgWalkTreePre(&tree, gtFindCatchArg) == WALK_ABORT))
    {
        return true;
    }
    return false;
}

//------------------------------------------------------------------------
// gtHasCallOnStack:
//
// Arguments:
//    parentStack: a context (stack of parent nodes)
//
// Return Value:
//     returns true if any of the parent nodes are a GT_CALL
//
// Assumptions:
//    We have a stack of parent nodes. This generally requires that
//    we are performing a recursive tree walk using struct fgWalkData
//
//------------------------------------------------------------------------
/* static */ bool Compiler::gtHasCallOnStack(GenTreeStack* parentStack)
{
    for (int i = 0; i < parentStack->Height(); i++)
    {
        GenTree* node = parentStack->Index(i);
        if (node->OperGet() == GT_CALL)
        {
            return true;
        }
    }
    return false;
}

//------------------------------------------------------------------------
// gtCheckQuirkAddrExposedLclVar:
//
// Arguments:
//    tree: an address taken GenTree node that is a GT_LCL_VAR
//    parentStack: a context (stack of parent nodes)
//    The 'parentStack' is used to ensure that we are in an argument context.
//
// Return Value:
//    None
//
// Notes:
//    When allocation size of this LclVar is 32-bits we will quirk the size to 64-bits
//    because some PInvoke signatures incorrectly specify a ByRef to an INT32
//    when they actually write a SIZE_T or INT64. There are cases where overwriting
//    these extra 4 bytes corrupts some data (such as a saved register) that leads to A/V
//    Wheras previously the JIT64 codegen did not lead to an A/V
//
// Assumptions:
//    'tree' is known to be address taken and that we have a stack
//    of parent nodes. Both of these generally requires that
//    we are performing a recursive tree walk using struct fgWalkData
//------------------------------------------------------------------------
void Compiler::gtCheckQuirkAddrExposedLclVar(GenTreePtr tree, GenTreeStack* parentStack)
{
#ifdef _TARGET_64BIT_
    // We only need to Quirk for _TARGET_64BIT_

    // Do we have a parent node that is a Call?
    if (!Compiler::gtHasCallOnStack(parentStack))
    {
        // No, so we don't apply the Quirk
        return;
    }
    noway_assert(tree->gtOper == GT_LCL_VAR);
    unsigned   lclNum  = tree->gtLclVarCommon.gtLclNum;
    LclVarDsc* varDsc  = &lvaTable[lclNum];
    var_types  vartype = varDsc->TypeGet();

    if (varDsc->lvIsParam)
    {
        // We can't Quirk the size of an incoming parameter
        return;
    }

    // We may need to Quirk the storage size for this LCL_VAR
    if (genActualType(vartype) == TYP_INT)
    {
        varDsc->lvQuirkToLong = true;
#ifdef DEBUG
        if (verbose)
        {
            printf("\nAdding a Quirk for the storage size of LvlVar V%02d:", lclNum);
            printf(" (%s ==> %s)\n", varTypeName(vartype), varTypeName(TYP_LONG));
        }
#endif // DEBUG
    }
#endif
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
        if (tree->gtCall.gtCallType == CT_HELPER)
        {
            if (gtIsTypeHandleToRuntimeTypeHelper(tree->AsCall()))
            {
                return TPK_Handle;
            }
        }
        else if (tree->gtCall.gtCallMoreFlags & GTF_CALL_M_SPECIAL_INTRINSIC)
        {
            if (info.compCompHnd->getIntrinsicID(tree->gtCall.gtCallMethHnd) == CORINFO_INTRINSIC_Object_GetType)
            {
                return TPK_GetType;
            }
        }
    }
    else if ((tree->gtOper == GT_INTRINSIC) && (tree->gtIntrinsic.gtIntrinsicId == CORINFO_INTRINSIC_Object_GetType))
    {
        return TPK_GetType;
    }
    else if ((tree->gtOper == GT_CNS_INT) && (tree->gtIntCon.gtIconVal == 0))
    {
        return TPK_Null;
    }
    else
    {
        bool                 isExact   = false;
        bool                 isNonNull = false;
        CORINFO_CLASS_HANDLE clsHnd    = gtGetClassHandle(tree, &isExact, &isNonNull);

        if (clsHnd == info.compCompHnd->getBuiltinClass(CLASSID_RUNTIME_TYPE))
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

bool Compiler::gtIsActiveCSE_Candidate(GenTreePtr tree)
{
    return (optValnumCSE_phase && IS_CSE_INDEX(tree->gtCSEnum));
}

/*****************************************************************************/

struct ComplexityStruct
{
    unsigned m_numNodes;
    unsigned m_nodeLimit;
    ComplexityStruct(unsigned nodeLimit) : m_numNodes(0), m_nodeLimit(nodeLimit)
    {
    }
};

static Compiler::fgWalkResult ComplexityExceedsWalker(GenTreePtr* pTree, Compiler::fgWalkData* data)
{
    ComplexityStruct* pComplexity = (ComplexityStruct*)data->pCallbackData;
    if (++pComplexity->m_numNodes > pComplexity->m_nodeLimit)
    {
        return Compiler::WALK_ABORT;
    }
    else
    {
        return Compiler::WALK_CONTINUE;
    }
}

bool Compiler::gtComplexityExceeds(GenTreePtr* tree, unsigned limit)
{
    ComplexityStruct complexity(limit);
    if (fgWalkTreePre(tree, &ComplexityExceedsWalker, &complexity) == WALK_ABORT)
    {
        return true;
    }
    else
    {
        return false;
    }
}

// -------------------------------------------------------------------------
// IsRegOptional: Returns true if this gentree node is marked by lowering to
// indicate that codegen can still generate code even if it wasn't allocated
// a register.
bool GenTree::IsRegOptional() const
{
#ifdef LEGACY_BACKEND
    return false;
#else
    return gtLsraInfo.regOptional;
#endif
}

bool GenTree::IsPhiNode()
{
    return (OperGet() == GT_PHI_ARG) || (OperGet() == GT_PHI) || IsPhiDefn();
}

bool GenTree::IsPhiDefn()
{
    bool res = ((OperGet() == GT_ASG) && (gtOp.gtOp2 != nullptr) && (gtOp.gtOp2->OperGet() == GT_PHI)) ||
               ((OperGet() == GT_STORE_LCL_VAR) && (gtOp.gtOp1 != nullptr) && (gtOp.gtOp1->OperGet() == GT_PHI));
    assert(!res || OperGet() == GT_STORE_LCL_VAR || gtOp.gtOp1->OperGet() == GT_LCL_VAR);
    return res;
}

bool GenTree::IsPhiDefnStmt()
{
    if (OperGet() != GT_STMT)
    {
        return false;
    }
    GenTreePtr asg = gtStmt.gtStmtExpr;
    return asg->IsPhiDefn();
}

// IsPartialLclFld: Check for a GT_LCL_FLD whose type is a different size than the lclVar.
//
// Arguments:
//    comp      - the Compiler object.
//
// Return Value:
//    Returns "true" iff 'this' is a GT_LCL_FLD or GT_STORE_LCL_FLD on which the type
//    is not the same size as the type of the GT_LCL_VAR

bool GenTree::IsPartialLclFld(Compiler* comp)
{
    return ((gtOper == GT_LCL_FLD) &&
            (comp->lvaTable[this->gtLclVarCommon.gtLclNum].lvExactSize != genTypeSize(gtType)));
}

bool GenTree::DefinesLocal(Compiler* comp, GenTreeLclVarCommon** pLclVarTree, bool* pIsEntire)
{
    GenTreeBlk* blkNode = nullptr;
    if (OperIsAssignment())
    {
        if (gtOp.gtOp1->IsLocal())
        {
            GenTreeLclVarCommon* lclVarTree = gtOp.gtOp1->AsLclVarCommon();
            *pLclVarTree                    = lclVarTree;
            if (pIsEntire != nullptr)
            {
                if (lclVarTree->IsPartialLclFld(comp))
                {
                    *pIsEntire = false;
                }
                else
                {
                    *pIsEntire = true;
                }
            }
            return true;
        }
        else if (gtOp.gtOp1->OperGet() == GT_IND)
        {
            GenTreePtr indArg = gtOp.gtOp1->gtOp.gtOp1;
            return indArg->DefinesLocalAddr(comp, genTypeSize(gtOp.gtOp1->TypeGet()), pLclVarTree, pIsEntire);
        }
        else if (gtOp.gtOp1->OperIsBlk())
        {
            blkNode = gtOp.gtOp1->AsBlk();
        }
    }
    else if (OperIsBlk())
    {
        blkNode = this->AsBlk();
    }
    if (blkNode != nullptr)
    {
        GenTreePtr destAddr = blkNode->Addr();
        unsigned   width    = blkNode->gtBlkSize;
        // Do we care about whether this assigns the entire variable?
        if (pIsEntire != nullptr && width == 0)
        {
            assert(blkNode->gtOper == GT_DYN_BLK);
            GenTreePtr blockWidth = blkNode->AsDynBlk()->gtDynamicSize;
            if (blockWidth->IsCnsIntOrI())
            {
                if (blockWidth->IsIconHandle())
                {
                    // If it's a handle, it must be a class handle.  We only create such block operations
                    // for initialization of struct types, so the type of the argument(s) will match this
                    // type, by construction, and be "entire".
                    assert(blockWidth->IsIconHandle(GTF_ICON_CLASS_HDL));
                    width = comp->info.compCompHnd->getClassSize(
                        CORINFO_CLASS_HANDLE(blockWidth->gtIntConCommon.IconValue()));
                }
                else
                {
                    ssize_t swidth = blockWidth->AsIntConCommon()->IconValue();
                    assert(swidth >= 0);
                    // cpblk of size zero exists in the wild (in yacc-generated code in SQL) and is valid IL.
                    if (swidth == 0)
                    {
                        return false;
                    }
                    width = unsigned(swidth);
                }
            }
        }
        return destAddr->DefinesLocalAddr(comp, width, pLclVarTree, pIsEntire);
    }
    // Otherwise...
    return false;
}

// Returns true if this GenTree defines a result which is based on the address of a local.
bool GenTree::DefinesLocalAddr(Compiler* comp, unsigned width, GenTreeLclVarCommon** pLclVarTree, bool* pIsEntire)
{
    if (OperGet() == GT_ADDR || OperGet() == GT_LCL_VAR_ADDR)
    {
        GenTreePtr addrArg = this;
        if (OperGet() == GT_ADDR)
        {
            addrArg = gtOp.gtOp1;
        }

        if (addrArg->IsLocal() || addrArg->OperIsLocalAddr())
        {
            GenTreeLclVarCommon* addrArgLcl = addrArg->AsLclVarCommon();
            *pLclVarTree                    = addrArgLcl;
            if (pIsEntire != nullptr)
            {
                unsigned lclOffset = 0;
                if (addrArg->OperIsLocalField())
                {
                    lclOffset = addrArg->gtLclFld.gtLclOffs;
                }

                if (lclOffset != 0)
                {
                    // We aren't updating the bytes at [0..lclOffset-1] so *pIsEntire should be set to false
                    *pIsEntire = false;
                }
                else
                {
                    unsigned lclNum   = addrArgLcl->GetLclNum();
                    unsigned varWidth = comp->lvaLclExactSize(lclNum);
                    if (comp->lvaTable[lclNum].lvNormalizeOnStore())
                    {
                        // It's normalize on store, so use the full storage width -- writing to low bytes won't
                        // necessarily yield a normalized value.
                        varWidth = genTypeStSz(var_types(comp->lvaTable[lclNum].lvType)) * sizeof(int);
                    }
                    *pIsEntire = (varWidth == width);
                }
            }
            return true;
        }
        else if (addrArg->OperGet() == GT_IND)
        {
            // A GT_ADDR of a GT_IND can both be optimized away, recurse using the child of the GT_IND
            return addrArg->gtOp.gtOp1->DefinesLocalAddr(comp, width, pLclVarTree, pIsEntire);
        }
    }
    else if (OperGet() == GT_ADD)
    {
        if (gtOp.gtOp1->IsCnsIntOrI())
        {
            // If we just adding a zero then we allow an IsEntire match against width
            //  otherwise we change width to zero to disallow an IsEntire Match
            return gtOp.gtOp2->DefinesLocalAddr(comp, gtOp.gtOp1->IsIntegralConst(0) ? width : 0, pLclVarTree,
                                                pIsEntire);
        }
        else if (gtOp.gtOp2->IsCnsIntOrI())
        {
            // If we just adding a zero then we allow an IsEntire match against width
            //  otherwise we change width to zero to disallow an IsEntire Match
            return gtOp.gtOp1->DefinesLocalAddr(comp, gtOp.gtOp2->IsIntegralConst(0) ? width : 0, pLclVarTree,
                                                pIsEntire);
        }
    }
    // Post rationalization we could have GT_IND(GT_LEA(..)) trees.
    else if (OperGet() == GT_LEA)
    {
        // This method gets invoked during liveness computation and therefore it is critical
        // that we don't miss 'use' of any local.  The below logic is making the assumption
        // that in case of LEA(base, index, offset) - only base can be a GT_LCL_VAR_ADDR
        // and index is not.
        CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
        GenTreePtr index = gtOp.gtOp2;
        if (index != nullptr)
        {
            assert(!index->DefinesLocalAddr(comp, width, pLclVarTree, pIsEntire));
        }
#endif // DEBUG

        // base
        GenTreePtr base = gtOp.gtOp1;
        if (base != nullptr)
        {
            // Lea could have an Indir as its base.
            if (base->OperGet() == GT_IND)
            {
                base = base->gtOp.gtOp1->gtEffectiveVal(/*commas only*/ true);
            }
            return base->DefinesLocalAddr(comp, width, pLclVarTree, pIsEntire);
        }
    }
    // Otherwise...
    return false;
}

//------------------------------------------------------------------------
// IsLocalExpr: Determine if this is a LclVarCommon node and return some
//              additional info about it in the two out parameters.
//
// Arguments:
//    comp        - The Compiler instance
//    pLclVarTree - An "out" argument that returns the local tree as a
//                  LclVarCommon, if it is indeed local.
//    pFldSeq     - An "out" argument that returns the value numbering field
//                  sequence for the node, if any.
//
// Return Value:
//    Returns true, and sets the out arguments accordingly, if this is
//    a LclVarCommon node.

bool GenTree::IsLocalExpr(Compiler* comp, GenTreeLclVarCommon** pLclVarTree, FieldSeqNode** pFldSeq)
{
    if (IsLocal()) // Note that this covers "GT_LCL_FLD."
    {
        *pLclVarTree = AsLclVarCommon();
        if (OperGet() == GT_LCL_FLD)
        {
            // Otherwise, prepend this field to whatever we've already accumulated outside in.
            *pFldSeq = comp->GetFieldSeqStore()->Append(AsLclFld()->gtFieldSeq, *pFldSeq);
        }
        return true;
    }
    else
    {
        return false;
    }
}

// If this tree evaluates some sum of a local address and some constants,
// return the node for the local being addressed

GenTreeLclVarCommon* GenTree::IsLocalAddrExpr()
{
    if (OperGet() == GT_ADDR)
    {
        return gtOp.gtOp1->IsLocal() ? gtOp.gtOp1->AsLclVarCommon() : nullptr;
    }
    else if (OperIsLocalAddr())
    {
        return this->AsLclVarCommon();
    }
    else if (OperGet() == GT_ADD)
    {
        if (gtOp.gtOp1->OperGet() == GT_CNS_INT)
        {
            return gtOp.gtOp2->IsLocalAddrExpr();
        }
        else if (gtOp.gtOp2->OperGet() == GT_CNS_INT)
        {
            return gtOp.gtOp1->IsLocalAddrExpr();
        }
    }
    // Otherwise...
    return nullptr;
}

bool GenTree::IsLocalAddrExpr(Compiler* comp, GenTreeLclVarCommon** pLclVarTree, FieldSeqNode** pFldSeq)
{
    if (OperGet() == GT_ADDR)
    {
        assert(!comp->compRationalIRForm);
        GenTreePtr addrArg = gtOp.gtOp1;
        if (addrArg->IsLocal()) // Note that this covers "GT_LCL_FLD."
        {
            *pLclVarTree = addrArg->AsLclVarCommon();
            if (addrArg->OperGet() == GT_LCL_FLD)
            {
                // Otherwise, prepend this field to whatever we've already accumulated outside in.
                *pFldSeq = comp->GetFieldSeqStore()->Append(addrArg->AsLclFld()->gtFieldSeq, *pFldSeq);
            }
            return true;
        }
        else
        {
            return false;
        }
    }
    else if (OperIsLocalAddr())
    {
        *pLclVarTree = this->AsLclVarCommon();
        if (this->OperGet() == GT_LCL_FLD_ADDR)
        {
            *pFldSeq = comp->GetFieldSeqStore()->Append(this->AsLclFld()->gtFieldSeq, *pFldSeq);
        }
        return true;
    }
    else if (OperGet() == GT_ADD)
    {
        if (gtOp.gtOp1->OperGet() == GT_CNS_INT)
        {
            if (gtOp.gtOp1->AsIntCon()->gtFieldSeq == nullptr)
            {
                return false;
            }
            // Otherwise, prepend this field to whatever we've already accumulated outside in.
            *pFldSeq = comp->GetFieldSeqStore()->Append(gtOp.gtOp1->AsIntCon()->gtFieldSeq, *pFldSeq);
            return gtOp.gtOp2->IsLocalAddrExpr(comp, pLclVarTree, pFldSeq);
        }
        else if (gtOp.gtOp2->OperGet() == GT_CNS_INT)
        {
            if (gtOp.gtOp2->AsIntCon()->gtFieldSeq == nullptr)
            {
                return false;
            }
            // Otherwise, prepend this field to whatever we've already accumulated outside in.
            *pFldSeq = comp->GetFieldSeqStore()->Append(gtOp.gtOp2->AsIntCon()->gtFieldSeq, *pFldSeq);
            return gtOp.gtOp1->IsLocalAddrExpr(comp, pLclVarTree, pFldSeq);
        }
    }
    // Otherwise...
    return false;
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
    unsigned lclNum = BAD_VAR_NUM;
    if (OperIsAssignment())
    {
        GenTree* lhs = gtOp.gtOp1;
        if (lhs->OperGet() == GT_LCL_VAR)
        {
            unsigned lhsLclNum = lhs->AsLclVarCommon()->gtLclNum;
            if (gtOper == GT_ASG)
            {
                GenTree* rhs = gtOp.gtOp2;
                if (rhs->OperIsBinary() && (rhs->gtOp.gtOp1->gtOper == GT_LCL_VAR) &&
                    (rhs->gtOp.gtOp1->AsLclVarCommon()->gtLclNum == lhsLclNum))
                {
                    lclNum      = lhsLclNum;
                    *pOtherTree = rhs->gtOp.gtOp2;
                    *pOper      = rhs->gtOper;
                }
            }
#ifdef LEGACY_BACKEND
            else
            {
                lclNum      = lhsLclNum;
                *pOper      = GenTree::OpAsgToOper(gtOper);
                *pOtherTree = gtOp.gtOp2;
            }
#endif
        }
    }
    return lclNum;
}

//------------------------------------------------------------------------
// canBeContained: check whether this tree node may be a subcomponent of its parent for purposes
//                 of code generation.
//
// Return value: returns true if it is possible to contain this node and false otherwise.
bool GenTree::canBeContained() const
{
    assert(IsLIR());

    if (gtHasReg())
    {
        return false;
    }

    // It is not possible for nodes that do not produce values or that are not containable values
    // to be contained.
    if ((OperKind() & (GTK_NOVALUE | GTK_NOCONTAIN)) != 0)
    {
        return false;
    }

    return true;
}

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
#ifdef LEGACY_BACKEND
    return false;
#else // !LEGACY_BACKEND
    assert(IsLIR());
    const bool isMarkedContained = ((gtFlags & GTF_CONTAINED) != 0);

#ifdef DEBUG
    if (!canBeContained())
    {
        assert(!isMarkedContained);
    }

    // these actually produce a register (the flags reg, we just don't model it)
    // and are a separate instruction from the branch that consumes the result.
    // They can only produce a result if the child is a SIMD equality comparison.
    else if (OperKind() & GTK_RELOP)
    {
        // We have to cast away const-ness since AsOp() method is non-const.
        GenTree* childNode = const_cast<GenTree*>(this)->AsOp()->gtOp1;
        assert((isMarkedContained == false) || childNode->IsSIMDEqualityOrInequality());
    }

    // these either produce a result in register or set flags reg.
    else if (IsSIMDEqualityOrInequality())
    {
        assert(!isMarkedContained);
    }

    // if it's contained it better have a user
    if (isMarkedContained)
    {
        assert(gtNext != nullptr);
    }
#endif // DEBUG
    return isMarkedContained;
#endif // !LEGACY_BACKEND
}

// return true if node is contained and an indir
bool GenTree::isContainedIndir() const
{
    return isIndir() && isContained();
}

bool GenTree::isIndirAddrMode()
{
    return isIndir() && AsIndir()->Addr()->OperIsAddrMode() && AsIndir()->Addr()->isContained();
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

GenTreePtr GenTreeIndir::Base()
{
    GenTreePtr addr = Addr();

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
    else if (Addr()->gtOper == GT_CLS_VAR_ADDR)
    {
        return static_cast<ssize_t>(reinterpret_cast<intptr_t>(Addr()->gtClsVar.gtClsVarHnd));
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
    return comp->opts.compReloc && (gtOper == GT_CNS_INT) && IsIconHandle();
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

#ifdef _TARGET_AMD64_
// Returns true if this absolute address fits within the base of an addr mode.
// On Amd64 this effectively means, whether an absolute indirect address can
// be encoded as 32-bit offset relative to IP or zero.
bool GenTreeIntConCommon::FitsInAddrBase(Compiler* comp)
{
#ifndef LEGACY_BACKEND
#ifdef DEBUG
    // Early out if PC-rel encoding of absolute addr is disabled.
    if (!comp->opts.compEnablePCRelAddr)
    {
        return false;
    }
#endif
#endif //! LEGACY_BACKEND

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

#elif defined(_TARGET_X86_)
// Returns true if this absolute address fits within the base of an addr mode.
// On x86 all addresses are 4-bytes and can be directly encoded in an addr mode.
bool GenTreeIntConCommon::FitsInAddrBase(Compiler* comp)
{
#ifndef LEGACY_BACKEND
#ifdef DEBUG
    // Early out if PC-rel encoding of absolute addr is disabled.
    if (!comp->opts.compEnablePCRelAddr)
    {
        return false;
    }
#endif
#endif //! LEGACY_BACKEND

    return IsCnsIntOrI();
}

// Returns true if this icon value is encoded as addr needs recording a relocation with VM
bool GenTreeIntConCommon::AddrNeedsReloc(Compiler* comp)
{
    // If generating relocatable code, icons should be reported for recording relocatons.
    return comp->opts.compReloc && IsIconHandle();
}
#endif //_TARGET_X86_

bool GenTree::IsFieldAddr(Compiler* comp, GenTreePtr* pObj, GenTreePtr* pStatic, FieldSeqNode** pFldSeq)
{
    FieldSeqNode* newFldSeq    = nullptr;
    GenTreePtr    baseAddr     = nullptr;
    bool          mustBeStatic = false;

    FieldSeqNode* statStructFldSeq = nullptr;
    if (TypeGet() == TYP_REF)
    {
        // Recognize struct static field patterns...
        if (OperGet() == GT_IND)
        {
            GenTreePtr     addr = gtOp.gtOp1;
            GenTreeIntCon* icon = nullptr;
            if (addr->OperGet() == GT_CNS_INT)
            {
                icon = addr->AsIntCon();
            }
            else if (addr->OperGet() == GT_ADD)
            {
                // op1 should never be a field sequence (or any other kind of handle)
                assert((addr->gtOp.gtOp1->gtOper != GT_CNS_INT) || !addr->gtOp.gtOp1->IsIconHandle());
                if (addr->gtOp.gtOp2->OperGet() == GT_CNS_INT)
                {
                    icon = addr->gtOp.gtOp2->AsIntCon();
                }
            }
            if (icon != nullptr && !icon->IsIconHandle(GTF_ICON_STR_HDL) // String handles are a source of TYP_REFs.
                && icon->gtFieldSeq != nullptr &&
                icon->gtFieldSeq->m_next == nullptr // A static field should be a singleton
                // TODO-Review: A pseudoField here indicates an issue - this requires investigation
                // See test case src\ddsuites\src\clr\x86\CoreMangLib\Dev\Globalization\CalendarRegressions.exe
                && !(FieldSeqStore::IsPseudoField(icon->gtFieldSeq->m_fieldHnd)) &&
                icon->gtFieldSeq != FieldSeqStore::NotAField()) // Ignore non-fields.
            {
                statStructFldSeq = icon->gtFieldSeq;
            }
            else
            {
                addr = addr->gtEffectiveVal();

                // Perhaps it's a direct indirection of a helper call or a cse with a zero offset annotation.
                if ((addr->OperGet() == GT_CALL) || (addr->OperGet() == GT_LCL_VAR))
                {
                    FieldSeqNode* zeroFieldSeq = nullptr;
                    if (comp->GetZeroOffsetFieldMap()->Lookup(addr, &zeroFieldSeq))
                    {
                        if (zeroFieldSeq->m_next == nullptr)
                        {
                            statStructFldSeq = zeroFieldSeq;
                        }
                    }
                }
            }
        }
        else if (OperGet() == GT_CLS_VAR)
        {
            GenTreeClsVar* clsVar = AsClsVar();
            if (clsVar->gtFieldSeq != nullptr && clsVar->gtFieldSeq->m_next == nullptr)
            {
                statStructFldSeq = clsVar->gtFieldSeq;
            }
        }
        else if (OperIsLocal())
        {
            // If we have a GT_LCL_VAR, it can be result of a CSE substitution
            // If it is then the CSE assignment will have a ValueNum that
            // describes the RHS of the CSE assignment.
            //
            // The CSE could be a pointer to a boxed struct
            //
            GenTreeLclVarCommon* lclVar = AsLclVarCommon();
            ValueNum             vn     = gtVNPair.GetLiberal();
            if (vn != ValueNumStore::NoVN)
            {
                // Is the ValueNum a MapSelect involving a SharedStatic helper?
                VNFuncApp funcApp1;
                if (comp->vnStore->GetVNFunc(vn, &funcApp1) && (funcApp1.m_func == VNF_MapSelect) &&
                    (comp->vnStore->IsSharedStatic(funcApp1.m_args[1])))
                {
                    ValueNum mapVN = funcApp1.m_args[0];
                    // Is this new 'mapVN' ValueNum, a MapSelect involving a handle?
                    VNFuncApp funcApp2;
                    if (comp->vnStore->GetVNFunc(mapVN, &funcApp2) && (funcApp2.m_func == VNF_MapSelect) &&
                        (comp->vnStore->IsVNHandle(funcApp2.m_args[1])))
                    {
                        ValueNum fldHndVN = funcApp2.m_args[1];
                        // Is this new 'fldHndVN' VNhandle a FieldHandle?
                        unsigned flags = comp->vnStore->GetHandleFlags(fldHndVN);
                        if (flags == GTF_ICON_FIELD_HDL)
                        {
                            CORINFO_FIELD_HANDLE fieldHnd =
                                CORINFO_FIELD_HANDLE(comp->vnStore->ConstantValue<ssize_t>(fldHndVN));

                            // Record this field sequence in 'statStructFldSeq' as it is likely to be a Boxed Struct
                            // field access.
                            statStructFldSeq = comp->GetFieldSeqStore()->CreateSingleton(fieldHnd);
                        }
                    }
                }
            }
        }

        if (statStructFldSeq != nullptr)
        {
            assert(statStructFldSeq->m_next == nullptr);
            // Is this a pointer to a boxed struct?
            if (comp->gtIsStaticFieldPtrToBoxedStruct(TYP_REF, statStructFldSeq->m_fieldHnd))
            {
                *pFldSeq = comp->GetFieldSeqStore()->Append(statStructFldSeq, *pFldSeq);
                *pObj    = nullptr;
                *pStatic = this;
                return true;
            }
        }

        // Otherwise...
        *pObj    = this;
        *pStatic = nullptr;
        return true;
    }
    else if (OperGet() == GT_ADD)
    {
        // If one operator is a field sequence/handle, the other operator must not also be a field sequence/handle.
        if ((gtOp.gtOp1->OperGet() == GT_CNS_INT) && gtOp.gtOp1->IsIconHandle())
        {
            assert((gtOp.gtOp2->gtOper != GT_CNS_INT) || !gtOp.gtOp2->IsIconHandle());
            newFldSeq = gtOp.gtOp1->AsIntCon()->gtFieldSeq;
            baseAddr  = gtOp.gtOp2;
        }
        else if (gtOp.gtOp2->OperGet() == GT_CNS_INT)
        {
            assert((gtOp.gtOp1->gtOper != GT_CNS_INT) || !gtOp.gtOp1->IsIconHandle());
            newFldSeq = gtOp.gtOp2->AsIntCon()->gtFieldSeq;
            baseAddr  = gtOp.gtOp1;
        }
    }
    else
    {
        // Check if "this" has a zero-offset annotation.
        if (!comp->GetZeroOffsetFieldMap()->Lookup(this, &newFldSeq))
        {
            // If not, this is not a field address.
            return false;
        }
        else
        {
            baseAddr     = this;
            mustBeStatic = true;
        }
    }

    // If not we don't have a field seq, it's not a field address.
    if (newFldSeq == nullptr || newFldSeq == FieldSeqStore::NotAField())
    {
        return false;
    }

    // Prepend this field to whatever we've already accumulated (outside-in).
    *pFldSeq = comp->GetFieldSeqStore()->Append(newFldSeq, *pFldSeq);

    // Is it a static or instance field?
    if (!FieldSeqStore::IsPseudoField(newFldSeq->m_fieldHnd) &&
        comp->info.compCompHnd->isFieldStatic(newFldSeq->m_fieldHnd))
    {
        // It is a static field.  We're done.
        *pObj    = nullptr;
        *pStatic = baseAddr;
        return true;
    }
    else if ((baseAddr != nullptr) && !mustBeStatic)
    {
        // It's an instance field...but it must be for a struct field, since we've not yet encountered
        // a "TYP_REF" address.  Analyze the reset of the address.
        return baseAddr->gtEffectiveVal()->IsFieldAddr(comp, pObj, pStatic, pFldSeq);
    }

    // Otherwise...
    return false;
}

bool Compiler::gtIsStaticFieldPtrToBoxedStruct(var_types fieldNodeType, CORINFO_FIELD_HANDLE fldHnd)
{
    if (fieldNodeType != TYP_REF)
    {
        return false;
    }
    CORINFO_CLASS_HANDLE fldCls = nullptr;
    noway_assert(fldHnd != nullptr);
    CorInfoType cit      = info.compCompHnd->getFieldType(fldHnd, &fldCls);
    var_types   fieldTyp = JITtype2varType(cit);
    return fieldTyp != TYP_REF;
}

CORINFO_CLASS_HANDLE Compiler::gtGetStructHandleIfPresent(GenTree* tree)
{
    CORINFO_CLASS_HANDLE structHnd = NO_CLASS_HANDLE;
    tree                           = tree->gtEffectiveVal();
    if (varTypeIsStruct(tree->gtType))
    {
        switch (tree->gtOper)
        {
            default:
                break;
            case GT_MKREFANY:
                structHnd = impGetRefAnyClass();
                break;
            case GT_OBJ:
                structHnd = tree->gtObj.gtClass;
                break;
            case GT_CALL:
                structHnd = tree->gtCall.gtRetClsHnd;
                break;
            case GT_RET_EXPR:
                structHnd = tree->gtRetExpr.gtRetClsHnd;
                break;
            case GT_ARGPLACE:
                structHnd = tree->gtArgPlace.gtArgPlaceClsHnd;
                break;
            case GT_INDEX:
                structHnd = tree->gtIndex.gtStructElemClass;
                break;
            case GT_INDEX_ADDR:
                structHnd = tree->AsIndexAddr()->gtStructElemClass;
                break;
            case GT_FIELD:
                info.compCompHnd->getFieldType(tree->gtField.gtFldHnd, &structHnd);
                break;
            case GT_ASG:
                structHnd = gtGetStructHandleIfPresent(tree->gtGetOp1());
                break;
            case GT_LCL_FLD:
#ifdef FEATURE_SIMD
                if (varTypeIsSIMD(tree))
                {
                    structHnd = gtGetStructHandleForSIMD(tree->gtType, TYP_FLOAT);
                }
#endif
                break;
            case GT_LCL_VAR:
                structHnd = lvaTable[tree->AsLclVarCommon()->gtLclNum].lvVerTypeInfo.GetClassHandle();
                break;
            case GT_RETURN:
                structHnd = gtGetStructHandleIfPresent(tree->gtOp.gtOp1);
                break;
            case GT_IND:
#ifdef FEATURE_SIMD
                if (varTypeIsSIMD(tree))
                {
                    structHnd = gtGetStructHandleForSIMD(tree->gtType, TYP_FLOAT);
                }
                else
#endif
                {
                    ArrayInfo arrInfo;
                    if (TryGetArrayInfo(tree->AsIndir(), &arrInfo))
                    {
                        structHnd = EncodeElemType(arrInfo.m_elemType, arrInfo.m_elemStructType);
                    }
                }
                break;
#ifdef FEATURE_SIMD
            case GT_SIMD:
                structHnd = gtGetStructHandleForSIMD(tree->gtType, tree->AsSIMD()->gtSIMDBaseType);
#endif // FEATURE_SIMD
                break;
        }
    }
    return structHnd;
}

CORINFO_CLASS_HANDLE Compiler::gtGetStructHandle(GenTree* tree)
{
    CORINFO_CLASS_HANDLE structHnd = gtGetStructHandleIfPresent(tree);
    assert(structHnd != NO_CLASS_HANDLE);
    return structHnd;
}

//------------------------------------------------------------------------
// gtGetClassHandle: find class handle for a ref type
//
// Arguments:
//    tree -- tree to find handle for
//    isExact   [out] -- whether handle is exact type
//    isNonNull [out] -- whether tree value is known not to be null
//
// Return Value:
//    nullptr if class handle is unknown,
//        otherwise the class handle.
//    isExact set true if tree type is known to be exactly the handle type,
//        otherwise actual type may be a subtype.
//    isNonNull set true if tree value is known not to be null,
//        otherwise a null value is possible.

CORINFO_CLASS_HANDLE Compiler::gtGetClassHandle(GenTreePtr tree, bool* isExact, bool* isNonNull)
{
    // Set default values for our out params.
    *isNonNull                    = false;
    *isExact                      = false;
    CORINFO_CLASS_HANDLE objClass = nullptr;

    // Bail out if we're just importing and not generating code, since
    // the jit uses TYP_REF for CORINFO_TYPE_VAR locals and args, but
    // these may not be ref types.
    if (compIsForImportOnly())
    {
        return objClass;
    }

    // Bail out if the tree is not a ref type.
    var_types treeType = tree->TypeGet();
    if (treeType != TYP_REF)
    {
        return objClass;
    }

    // Tunnel through commas.
    GenTreePtr       obj   = tree->gtEffectiveVal(false);
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

            objClass = lvaTable[objLcl].lvClassHnd;
            *isExact = lvaTable[objLcl].lvClassIsExact;
            break;
        }

        case GT_FIELD:
        {
            // For fields, get the type from the field handle.
            CORINFO_FIELD_HANDLE fieldHnd = obj->gtField.gtFldHnd;

            if (fieldHnd != nullptr)
            {
                CORINFO_CLASS_HANDLE fieldClass   = nullptr;
                CorInfoType          fieldCorType = info.compCompHnd->getFieldType(fieldHnd, &fieldClass);
                if (fieldCorType == CORINFO_TYPE_CLASS)
                {
                    objClass = fieldClass;
                }
            }

            break;
        }

        case GT_RET_EXPR:
        {
            // If we see a RET_EXPR, recurse through to examine the
            // return value expression.
            GenTreePtr retExpr = tree->gtRetExpr.gtInlineCandidate;
            objClass           = gtGetClassHandle(retExpr, isExact, isNonNull);
            break;
        }

        case GT_CALL:
        {
            GenTreeCall* call = tree->AsCall();
            if (call->IsInlineCandidate())
            {
                // For inline candidates, we've already cached the return
                // type class handle in the inline info.
                InlineCandidateInfo* inlInfo = call->gtInlineCandidateInfo;
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
                        CORINFO_CLASS_HANDLE exactClass = nullptr;

                        if (((size_t)context & CORINFO_CONTEXTFLAGS_MASK) == CORINFO_CONTEXTFLAGS_CLASS)
                        {
                            exactClass = (CORINFO_CLASS_HANDLE)((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK);
                        }
                        else
                        {
                            CORINFO_METHOD_HANDLE exactMethod =
                                (CORINFO_METHOD_HANDLE)((size_t)context & ~CORINFO_CONTEXTFLAGS_MASK);
                            exactClass = info.compCompHnd->getMethodClass(exactMethod);
                        }

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
                    objClass   = info.compCompHnd->getMethodClass(method);
                    *isExact   = true;
                    *isNonNull = true;
                }
                else
                {
                    assert(sig.retType == CORINFO_TYPE_CLASS);
                    objClass = sig.retTypeClass;
                }
            }

            break;
        }

        case GT_CNS_STR:
        {
            // For literal strings, we know the class and that the
            // value is not null.
            objClass   = impGetStringClass();
            *isExact   = true;
            *isNonNull = true;
            break;
        }

        case GT_IND:
        {
            // indir(addr(lcl)) --> lcl
            //
            // This comes up during constrained callvirt on ref types.
            GenTreeIndir* indir = obj->AsIndir();
            if (indir->HasBase() && !indir->HasIndex())
            {
                GenTreePtr           base = indir->Base();
                GenTreeLclVarCommon* lcl  = base->IsLocalAddrExpr();

                if ((lcl != nullptr) && (base->OperGet() != GT_ADD))
                {
                    const unsigned objLcl = lcl->GetLclNum();
                    objClass              = lvaTable[objLcl].lvClassHnd;
                    *isExact              = lvaTable[objLcl].lvClassIsExact;
                }
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
            *isExact                  = lvaTable[boxTempLcl].lvClassIsExact;
            *isNonNull                = true;
            break;
        }

        default:
        {
            break;
        }
    }

    return objClass;
}

void GenTree::ParseArrayAddress(
    Compiler* comp, ArrayInfo* arrayInfo, GenTreePtr* pArr, ValueNum* pInxVN, FieldSeqNode** pFldSeq)
{
    *pArr                = nullptr;
    ValueNum      inxVN  = ValueNumStore::NoVN;
    ssize_t       offset = 0;
    FieldSeqNode* fldSeq = nullptr;

    ParseArrayAddressWork(comp, 1, pArr, &inxVN, &offset, &fldSeq);

    // If we didn't find an array reference (perhaps it is the constant null?) we will give up.
    if (*pArr == nullptr)
    {
        return;
    }

    // OK, new we have to figure out if any part of the "offset" is a constant contribution to the index.
    // First, sum the offsets of any fields in fldSeq.
    unsigned      fieldOffsets = 0;
    FieldSeqNode* fldSeqIter   = fldSeq;
    // Also, find the first non-pseudo field...
    assert(*pFldSeq == nullptr);
    while (fldSeqIter != nullptr)
    {
        if (fldSeqIter == FieldSeqStore::NotAField())
        {
            // TODO-Review: A NotAField here indicates a failure to properly maintain the field sequence
            // See test case self_host_tests_x86\jit\regression\CLR-x86-JIT\v1-m12-beta2\ b70992\ b70992.exe
            // Safest thing to do here is to drop back to MinOpts
            CLANG_FORMAT_COMMENT_ANCHOR;

#ifdef DEBUG
            if (comp->opts.optRepeat)
            {
                // We don't guarantee preserving these annotations through the entire optimizer, so
                // just conservatively return null if under optRepeat.
                *pArr = nullptr;
                return;
            }
#endif // DEBUG
            noway_assert(!"fldSeqIter is NotAField() in ParseArrayAddress");
        }

        if (!FieldSeqStore::IsPseudoField(fldSeqIter->m_fieldHnd))
        {
            if (*pFldSeq == nullptr)
            {
                *pFldSeq = fldSeqIter;
            }
            CORINFO_CLASS_HANDLE fldCls = nullptr;
            noway_assert(fldSeqIter->m_fieldHnd != nullptr);
            CorInfoType cit = comp->info.compCompHnd->getFieldType(fldSeqIter->m_fieldHnd, &fldCls);
            fieldOffsets += comp->compGetTypeSize(cit, fldCls);
        }
        fldSeqIter = fldSeqIter->m_next;
    }

    // Is there some portion of the "offset" beyond the first-elem offset and the struct field suffix we just computed?
    if (!FitsIn<ssize_t>(fieldOffsets + arrayInfo->m_elemOffset) || !FitsIn<ssize_t>(arrayInfo->m_elemSize))
    {
        // This seems unlikely, but no harm in being safe...
        *pInxVN = comp->GetValueNumStore()->VNForExpr(nullptr, TYP_INT);
        return;
    }
    // Otherwise...
    ssize_t offsetAccountedFor = static_cast<ssize_t>(fieldOffsets + arrayInfo->m_elemOffset);
    ssize_t elemSize           = static_cast<ssize_t>(arrayInfo->m_elemSize);

    ssize_t constIndOffset = offset - offsetAccountedFor;
    // This should be divisible by the element size...
    assert((constIndOffset % elemSize) == 0);
    ssize_t constInd = constIndOffset / elemSize;

    ValueNumStore* vnStore = comp->GetValueNumStore();

    if (inxVN == ValueNumStore::NoVN)
    {
        // Must be a constant index.
        *pInxVN = vnStore->VNForPtrSizeIntCon(constInd);
    }
    else
    {
        //
        // Perform ((inxVN / elemSizeVN) + vnForConstInd)
        //

        // The value associated with the index value number (inxVN) is the offset into the array,
        // which has been scaled by element size. We need to recover the array index from that offset
        if (vnStore->IsVNConstant(inxVN))
        {
            ssize_t index = vnStore->CoercedConstantValue<ssize_t>(inxVN);
            noway_assert(elemSize > 0 && ((index % elemSize) == 0));
            *pInxVN = vnStore->VNForPtrSizeIntCon((index / elemSize) + constInd);
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

            // Perform ((inxVN / elemSizeVN) + vnForConstInd)
            if (!canFoldDiv)
            {
                ValueNum vnForElemSize = vnStore->VNForPtrSizeIntCon(elemSize);
                ValueNum vnForScaledInx =
                    vnStore->VNForFunc(TYP_I_IMPL, GetVNFuncForOper(GT_DIV, false), inxVN, vnForElemSize);
                *pInxVN = vnForScaledInx;
            }

            if (constInd != 0)
            {
                ValueNum vnForConstInd = comp->GetValueNumStore()->VNForPtrSizeIntCon(constInd);
                *pInxVN                = comp->GetValueNumStore()->VNForFunc(TYP_I_IMPL,
                                                              GetVNFuncForOper(GT_ADD, (gtFlags & GTF_UNSIGNED) != 0),
                                                              *pInxVN, vnForConstInd);
            }
        }
    }
}

void GenTree::ParseArrayAddressWork(
    Compiler* comp, ssize_t inputMul, GenTreePtr* pArr, ValueNum* pInxVN, ssize_t* pOffset, FieldSeqNode** pFldSeq)
{
    if (TypeGet() == TYP_REF)
    {
        // This must be the array pointer.
        *pArr = this;
        assert(inputMul == 1); // Can't multiply the array pointer by anything.
    }
    else
    {
        switch (OperGet())
        {
            case GT_CNS_INT:
                *pFldSeq = comp->GetFieldSeqStore()->Append(*pFldSeq, gtIntCon.gtFieldSeq);
                *pOffset += (inputMul * gtIntCon.gtIconVal);
                return;

            case GT_ADD:
            case GT_SUB:
                gtOp.gtOp1->ParseArrayAddressWork(comp, inputMul, pArr, pInxVN, pOffset, pFldSeq);
                if (OperGet() == GT_SUB)
                {
                    inputMul = -inputMul;
                }
                gtOp.gtOp2->ParseArrayAddressWork(comp, inputMul, pArr, pInxVN, pOffset, pFldSeq);
                return;

            case GT_MUL:
            {
                // If one op is a constant, continue parsing down.
                ssize_t    subMul   = 0;
                GenTreePtr nonConst = nullptr;
                if (gtOp.gtOp1->IsCnsIntOrI())
                {
                    // If the other arg is an int constant, and is a "not-a-field", choose
                    // that as the multiplier, thus preserving constant index offsets...
                    if (gtOp.gtOp2->OperGet() == GT_CNS_INT &&
                        gtOp.gtOp2->gtIntCon.gtFieldSeq == FieldSeqStore::NotAField())
                    {
                        subMul   = gtOp.gtOp2->gtIntConCommon.IconValue();
                        nonConst = gtOp.gtOp1;
                    }
                    else
                    {
                        subMul   = gtOp.gtOp1->gtIntConCommon.IconValue();
                        nonConst = gtOp.gtOp2;
                    }
                }
                else if (gtOp.gtOp2->IsCnsIntOrI())
                {
                    subMul   = gtOp.gtOp2->gtIntConCommon.IconValue();
                    nonConst = gtOp.gtOp1;
                }
                if (nonConst != nullptr)
                {
                    nonConst->ParseArrayAddressWork(comp, inputMul * subMul, pArr, pInxVN, pOffset, pFldSeq);
                    return;
                }
                // Otherwise, exit the switch, treat as a contribution to the index.
            }
            break;

            case GT_LSH:
                // If one op is a constant, continue parsing down.
                if (gtOp.gtOp2->IsCnsIntOrI())
                {
                    ssize_t subMul = 1 << gtOp.gtOp2->gtIntConCommon.IconValue();
                    gtOp.gtOp1->ParseArrayAddressWork(comp, inputMul * subMul, pArr, pInxVN, pOffset, pFldSeq);
                    return;
                }
                // Otherwise, exit the switch, treat as a contribution to the index.
                break;

            case GT_COMMA:
                // We don't care about exceptions for this purpose.
                if ((gtOp.gtOp1->OperGet() == GT_ARR_BOUNDS_CHECK) || gtOp.gtOp1->IsNothingNode())
                {
                    gtOp.gtOp2->ParseArrayAddressWork(comp, inputMul, pArr, pInxVN, pOffset, pFldSeq);
                    return;
                }
                break;

            default:
                break;
        }
        // If we didn't return above, must be a constribution to the non-constant part of the index VN.
        ValueNum vn = comp->GetValueNumStore()->VNNormVal(gtVNPair.GetLiberal()); // We don't care about exceptions for
                                                                                  // this purpose.
        if (inputMul != 1)
        {
            ValueNum mulVN = comp->GetValueNumStore()->VNForLongCon(inputMul);
            vn             = comp->GetValueNumStore()->VNForFunc(TypeGet(), GetVNFuncForOper(GT_MUL, false), mulVN, vn);
        }
        if (*pInxVN == ValueNumStore::NoVN)
        {
            *pInxVN = vn;
        }
        else
        {
            *pInxVN = comp->GetValueNumStore()->VNForFunc(TypeGet(), GetVNFuncForOper(GT_ADD, false), *pInxVN, vn);
        }
    }
}

bool GenTree::ParseArrayElemForm(Compiler* comp, ArrayInfo* arrayInfo, FieldSeqNode** pFldSeq)
{
    if (OperIsIndir())
    {
        if (gtFlags & GTF_IND_ARR_INDEX)
        {
            bool b = comp->GetArrayInfoMap()->Lookup(this, arrayInfo);
            assert(b);
            return true;
        }

        // Otherwise...
        GenTreePtr addr = AsIndir()->Addr();
        return addr->ParseArrayElemAddrForm(comp, arrayInfo, pFldSeq);
    }
    else
    {
        return false;
    }
}

bool GenTree::ParseArrayElemAddrForm(Compiler* comp, ArrayInfo* arrayInfo, FieldSeqNode** pFldSeq)
{
    switch (OperGet())
    {
        case GT_ADD:
        {
            GenTreePtr arrAddr = nullptr;
            GenTreePtr offset  = nullptr;
            if (gtOp.gtOp1->TypeGet() == TYP_BYREF)
            {
                arrAddr = gtOp.gtOp1;
                offset  = gtOp.gtOp2;
            }
            else if (gtOp.gtOp2->TypeGet() == TYP_BYREF)
            {
                arrAddr = gtOp.gtOp2;
                offset  = gtOp.gtOp1;
            }
            else
            {
                return false;
            }
            if (!offset->ParseOffsetForm(comp, pFldSeq))
            {
                return false;
            }
            return arrAddr->ParseArrayElemAddrForm(comp, arrayInfo, pFldSeq);
        }

        case GT_ADDR:
        {
            GenTreePtr addrArg = gtOp.gtOp1;
            if (addrArg->OperGet() != GT_IND)
            {
                return false;
            }
            else
            {
                // The "Addr" node might be annotated with a zero-offset field sequence.
                FieldSeqNode* zeroOffsetFldSeq = nullptr;
                if (comp->GetZeroOffsetFieldMap()->Lookup(this, &zeroOffsetFldSeq))
                {
                    *pFldSeq = comp->GetFieldSeqStore()->Append(*pFldSeq, zeroOffsetFldSeq);
                }
                return addrArg->ParseArrayElemForm(comp, arrayInfo, pFldSeq);
            }
        }

        default:
            return false;
    }
}

bool GenTree::ParseOffsetForm(Compiler* comp, FieldSeqNode** pFldSeq)
{
    switch (OperGet())
    {
        case GT_CNS_INT:
        {
            GenTreeIntCon* icon = AsIntCon();
            *pFldSeq            = comp->GetFieldSeqStore()->Append(*pFldSeq, icon->gtFieldSeq);
            return true;
        }

        case GT_ADD:
            if (!gtOp.gtOp1->ParseOffsetForm(comp, pFldSeq))
            {
                return false;
            }
            return gtOp.gtOp2->ParseOffsetForm(comp, pFldSeq);

        default:
            return false;
    }
}

void GenTree::LabelIndex(Compiler* comp, bool isConst)
{
    switch (OperGet())
    {
        case GT_CNS_INT:
            // If we got here, this is a contribution to the constant part of the index.
            if (isConst)
            {
                gtIntCon.gtFieldSeq =
                    comp->GetFieldSeqStore()->CreateSingleton(FieldSeqStore::ConstantIndexPseudoField);
            }
            return;

        case GT_LCL_VAR:
            gtFlags |= GTF_VAR_ARR_INDEX;
            return;

        case GT_ADD:
        case GT_SUB:
            gtOp.gtOp1->LabelIndex(comp, isConst);
            gtOp.gtOp2->LabelIndex(comp, isConst);
            break;

        case GT_CAST:
            gtOp.gtOp1->LabelIndex(comp, isConst);
            break;

        case GT_ARR_LENGTH:
            gtFlags |= GTF_ARRLEN_ARR_IDX;
            return;

        default:
            // For all other operators, peel off one constant; and then label the other if it's also a constant.
            if (OperIsArithmetic() || OperIsCompare())
            {
                if (gtOp.gtOp2->OperGet() == GT_CNS_INT)
                {
                    gtOp.gtOp1->LabelIndex(comp, isConst);
                    break;
                }
                else if (gtOp.gtOp1->OperGet() == GT_CNS_INT)
                {
                    gtOp.gtOp2->LabelIndex(comp, isConst);
                    break;
                }
                // Otherwise continue downward on both, labeling vars.
                gtOp.gtOp1->LabelIndex(comp, false);
                gtOp.gtOp2->LabelIndex(comp, false);
            }
            break;
    }
}

// Note that the value of the below field doesn't matter; it exists only to provide a distinguished address.
//
// static
FieldSeqNode FieldSeqStore::s_notAField(nullptr, nullptr);

// FieldSeqStore methods.
FieldSeqStore::FieldSeqStore(IAllocator* alloc) : m_alloc(alloc), m_canonMap(new (alloc) FieldSeqNodeCanonMap(alloc))
{
}

FieldSeqNode* FieldSeqStore::CreateSingleton(CORINFO_FIELD_HANDLE fieldHnd)
{
    FieldSeqNode  fsn(fieldHnd, nullptr);
    FieldSeqNode* res = nullptr;
    if (m_canonMap->Lookup(fsn, &res))
    {
        return res;
    }
    else
    {
        res  = reinterpret_cast<FieldSeqNode*>(m_alloc->Alloc(sizeof(FieldSeqNode)));
        *res = fsn;
        m_canonMap->Set(fsn, res);
        return res;
    }
}

FieldSeqNode* FieldSeqStore::Append(FieldSeqNode* a, FieldSeqNode* b)
{
    if (a == nullptr)
    {
        return b;
    }
    else if (a == NotAField())
    {
        return NotAField();
    }
    else if (b == nullptr)
    {
        return a;
    }
    else if (b == NotAField())
    {
        return NotAField();
        // Extremely special case for ConstantIndex pseudo-fields -- appending consecutive such
        // together collapse to one.
    }
    else if (a->m_next == nullptr && a->m_fieldHnd == ConstantIndexPseudoField &&
             b->m_fieldHnd == ConstantIndexPseudoField)
    {
        return b;
    }
    else
    {
        FieldSeqNode* tmp = Append(a->m_next, b);
        FieldSeqNode  fsn(a->m_fieldHnd, tmp);
        FieldSeqNode* res = nullptr;
        if (m_canonMap->Lookup(fsn, &res))
        {
            return res;
        }
        else
        {
            res  = reinterpret_cast<FieldSeqNode*>(m_alloc->Alloc(sizeof(FieldSeqNode)));
            *res = fsn;
            m_canonMap->Set(fsn, res);
            return res;
        }
    }
}

// Static vars.
int FieldSeqStore::FirstElemPseudoFieldStruct;
int FieldSeqStore::ConstantIndexPseudoFieldStruct;

CORINFO_FIELD_HANDLE FieldSeqStore::FirstElemPseudoField =
    (CORINFO_FIELD_HANDLE)&FieldSeqStore::FirstElemPseudoFieldStruct;
CORINFO_FIELD_HANDLE FieldSeqStore::ConstantIndexPseudoField =
    (CORINFO_FIELD_HANDLE)&FieldSeqStore::ConstantIndexPseudoFieldStruct;

bool FieldSeqNode::IsFirstElemFieldSeq()
{
    // this must be non-null per ISO C++
    return m_fieldHnd == FieldSeqStore::FirstElemPseudoField;
}

bool FieldSeqNode::IsConstantIndexFieldSeq()
{
    // this must be non-null per ISO C++
    return m_fieldHnd == FieldSeqStore::ConstantIndexPseudoField;
}

bool FieldSeqNode::IsPseudoField()
{
    if (this == nullptr)
    {
        return false;
    }
    return m_fieldHnd == FieldSeqStore::FirstElemPseudoField || m_fieldHnd == FieldSeqStore::ConstantIndexPseudoField;
}

#ifdef FEATURE_SIMD
GenTreeSIMD* Compiler::gtNewSIMDNode(
    var_types type, GenTreePtr op1, SIMDIntrinsicID simdIntrinsicID, var_types baseType, unsigned size)
{
    assert(op1 != nullptr);
    SetOpLclRelatedToSIMDIntrinsic(op1);

    return new (this, GT_SIMD) GenTreeSIMD(type, op1, simdIntrinsicID, baseType, size);
}

GenTreeSIMD* Compiler::gtNewSIMDNode(
    var_types type, GenTreePtr op1, GenTreePtr op2, SIMDIntrinsicID simdIntrinsicID, var_types baseType, unsigned size)
{
    assert(op1 != nullptr);
    SetOpLclRelatedToSIMDIntrinsic(op1);
    if (op2 != nullptr)
    {
        SetOpLclRelatedToSIMDIntrinsic(op2);
    }

    return new (this, GT_SIMD) GenTreeSIMD(type, op1, op2, simdIntrinsicID, baseType, size);
}

//-------------------------------------------------------------------
// SetOpLclRelatedToSIMDIntrinsic: Determine if the tree has a local var that needs to be set
// as used by a SIMD intrinsic, and if so, set that local var appropriately.
//
// Arguments:
//     op - The tree, to be an operand of a new GT_SIMD node, to check.
//
void Compiler::SetOpLclRelatedToSIMDIntrinsic(GenTreePtr op)
{
    if (op->OperIsLocal())
    {
        setLclRelatedToSIMDIntrinsic(op);
    }
    else if ((op->OperGet() == GT_OBJ) && (op->gtOp.gtOp1->OperGet() == GT_ADDR) &&
             op->gtOp.gtOp1->gtOp.gtOp1->OperIsLocal())
    {
        setLclRelatedToSIMDIntrinsic(op->gtOp.gtOp1->gtOp.gtOp1);
    }
}

bool GenTree::isCommutativeSIMDIntrinsic()
{
    assert(gtOper == GT_SIMD);
    switch (AsSIMD()->gtSIMDIntrinsicID)
    {
        case SIMDIntrinsicAdd:
        case SIMDIntrinsicBitwiseAnd:
        case SIMDIntrinsicBitwiseOr:
        case SIMDIntrinsicBitwiseXor:
        case SIMDIntrinsicEqual:
        case SIMDIntrinsicMax:
        case SIMDIntrinsicMin:
        case SIMDIntrinsicMul:
        case SIMDIntrinsicOpEquality:
        case SIMDIntrinsicOpInEquality:
            return true;
        default:
            return false;
    }
}
#endif // FEATURE_SIMD

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
void ReturnTypeDesc::InitializeStructReturnType(Compiler* comp, CORINFO_CLASS_HANDLE retClsHnd)
{
    assert(!m_inited);

#if FEATURE_MULTIREG_RET

    assert(retClsHnd != NO_CLASS_HANDLE);
    unsigned structSize = comp->info.compCompHnd->getClassSize(retClsHnd);

    Compiler::structPassingKind howToReturnStruct;
    var_types                   returnType = comp->getReturnTypeForStruct(retClsHnd, &howToReturnStruct, structSize);

    switch (howToReturnStruct)
    {
        case Compiler::SPK_PrimitiveType:
        {
            assert(returnType != TYP_UNKNOWN);
            assert(returnType != TYP_STRUCT);
            m_regType[0] = returnType;
            break;
        }

        case Compiler::SPK_ByValueAsHfa:
        {
            assert(returnType == TYP_STRUCT);
            var_types hfaType = comp->GetHfaType(retClsHnd);

            // We should have an hfa struct type
            assert(varTypeIsFloating(hfaType));

            // Note that the retail build issues a warning about a potential divsion by zero without this Max function
            unsigned elemSize = Max((unsigned)1, EA_SIZE_IN_BYTES(emitActualTypeSize(hfaType)));

            // The size of this struct should be evenly divisible by elemSize
            assert((structSize % elemSize) == 0);

            unsigned hfaCount = (structSize / elemSize);
            for (unsigned i = 0; i < hfaCount; ++i)
            {
                m_regType[i] = hfaType;
            }

            if (comp->compFloatingPointUsed == false)
            {
                comp->compFloatingPointUsed = true;
            }
            break;
        }

        case Compiler::SPK_ByValue:
        {
            assert(returnType == TYP_STRUCT);

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING

            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR structDesc;
            comp->eeGetSystemVAmd64PassStructInRegisterDescriptor(retClsHnd, &structDesc);

            assert(structDesc.passedInRegisters);
            for (int i = 0; i < structDesc.eightByteCount; i++)
            {
                assert(i < MAX_RET_REG_COUNT);
                m_regType[i] = comp->GetEightByteType(structDesc, i);
            }

#elif defined(_TARGET_ARM64_)

            // a non-HFA struct returned using two registers
            //
            assert((structSize > TARGET_POINTER_SIZE) && (structSize <= (2 * TARGET_POINTER_SIZE)));

            BYTE gcPtrs[2] = {TYPE_GC_NONE, TYPE_GC_NONE};
            comp->info.compCompHnd->getClassGClayout(retClsHnd, &gcPtrs[0]);
            for (unsigned i = 0; i < 2; ++i)
            {
                m_regType[i] = comp->getJitGCType(gcPtrs[i]);
            }

#else //  _TARGET_XXX_

            // This target needs support here!
            //
            NYI("Unsupported TARGET returning a TYP_STRUCT in InitializeStructReturnType");

#endif // FEATURE_UNIX_AMD64_STRUCT_PASSING

            break; // for case SPK_ByValue
        }

        case Compiler::SPK_ByReference:

            // We are returning using the return buffer argument
            // There are no return registers
            break;

        default:

            unreached(); // By the contract of getReturnTypeForStruct we should never get here.

    } // end of switch (howToReturnStruct)

#endif //  FEATURE_MULTIREG_RET

#ifdef DEBUG
    m_inited = true;
#endif
}

//---------------------------------------------------------------------------------------
// InitializeLongReturnType:
//    Initialize the Return Type Descriptor for a method that returns a TYP_LONG
//
// Arguments
//    comp        -  Compiler Instance
//
// Return Value
//    None
//
void ReturnTypeDesc::InitializeLongReturnType(Compiler* comp)
{
#if defined(_TARGET_X86_) || defined(_TARGET_ARM_)

    // Setups up a ReturnTypeDesc for returning a long using two registers
    //
    assert(MAX_RET_REG_COUNT >= 2);
    m_regType[0] = TYP_INT;
    m_regType[1] = TYP_INT;

#else // not (_TARGET_X86_ or _TARGET_ARM_)

    m_regType[0] = TYP_LONG;

#endif // _TARGET_X86_ or _TARGET_ARM_

#ifdef DEBUG
    m_inited = true;
#endif
}

//-------------------------------------------------------------------
// GetABIReturnReg:  Return ith return register as per target ABI
//
// Arguments:
//     idx   -   Index of the return register.
//               The first return register has an index of 0 and so on.
//
// Return Value:
//     Returns ith return register as per target ABI.
//
// Notes:
//     x86 and ARM return long in multiple registers.
//     ARM and ARM64 return HFA struct in multiple registers.
//
regNumber ReturnTypeDesc::GetABIReturnReg(unsigned idx)
{
    unsigned count = GetReturnRegCount();
    assert(idx < count);

    regNumber resultReg = REG_NA;

#ifdef FEATURE_UNIX_AMD64_STRUCT_PASSING
    var_types regType0 = GetReturnRegType(0);

    if (idx == 0)
    {
        if (varTypeIsIntegralOrI(regType0))
        {
            resultReg = REG_INTRET;
        }
        else
        {
            noway_assert(varTypeIsFloating(regType0));
            resultReg = REG_FLOATRET;
        }
    }
    else if (idx == 1)
    {
        var_types regType1 = GetReturnRegType(1);

        if (varTypeIsIntegralOrI(regType1))
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
            noway_assert(varTypeIsFloating(regType1));

            if (varTypeIsFloating(regType0))
            {
                resultReg = REG_FLOATRET_1;
            }
            else
            {
                resultReg = REG_FLOATRET;
            }
        }
    }

#elif defined(_TARGET_X86_)

    if (idx == 0)
    {
        resultReg = REG_LNGRET_LO;
    }
    else if (idx == 1)
    {
        resultReg = REG_LNGRET_HI;
    }

#elif defined(_TARGET_ARM_)

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

#elif defined(_TARGET_ARM64_)

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
regMaskTP ReturnTypeDesc::GetABIReturnRegs()
{
    regMaskTP resultMask = RBM_NONE;

    unsigned count = GetReturnRegCount();
    for (unsigned i = 0; i < count; ++i)
    {
        resultMask |= genRegMask(GetABIReturnReg(i));
    }

    return resultMask;
}

#ifndef LEGACY_BACKEND

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
    regMaskTP tempRegMask = genFindLowestBit(availableSet);
    gtRsvdRegs &= ~tempRegMask;
    return genRegNumFromMask(tempRegMask);
}

#endif // !LEGACY_BACKEND
