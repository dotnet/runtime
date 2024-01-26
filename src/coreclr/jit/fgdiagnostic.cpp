// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h" // for alloca
#include "jitstd/algorithm.h"

// Flowgraph Check and Dump Support

#ifdef DEBUG
void Compiler::fgPrintEdgeWeights()
{
    // Print out all of the edge weights
    for (BasicBlock* const bDst : Blocks())
    {
        if (bDst->bbPreds != nullptr)
        {
            printf("    Edge weights into " FMT_BB " :", bDst->bbNum);
            for (FlowEdge* const edge : bDst->PredEdges())
            {
                BasicBlock* bSrc = edge->getSourceBlock();
                // This is the control flow edge (bSrc -> bDst)

                printf(FMT_BB " ", bSrc->bbNum);

                if (edge->edgeWeightMin() < BB_MAX_WEIGHT)
                {
                    printf("(%f", edge->edgeWeightMin());
                }
                else
                {
                    printf("(MAX");
                }
                if (edge->edgeWeightMin() != edge->edgeWeightMax())
                {
                    if (edge->edgeWeightMax() < BB_MAX_WEIGHT)
                    {
                        printf("..%f", edge->edgeWeightMax());
                    }
                    else
                    {
                        printf("..MAX");
                    }
                }
                printf(")");
                if (edge->getNextPredEdge() != nullptr)
                {
                    printf(", ");
                }
            }
            printf("\n");
        }
    }
}
#endif // DEBUG

/*****************************************************************************
 *  Check that the flow graph is really updated
 */

#ifdef DEBUG

void Compiler::fgDebugCheckUpdate()
{
    if (!compStressCompile(STRESS_CHK_FLOW_UPDATE, 30))
    {
        return;
    }

    /* We check for these conditions:
     * no unreachable blocks  -> no blocks have countOfInEdges() = 0
     * no empty blocks        -> !block->isEmpty(), unless non-removable or multiple in-edges
     * no un-imported blocks  -> no blocks have BBF_IMPORTED not set (this is
     *                           kind of redundant with the above, but to make sure)
     * no un-compacted blocks -> BBJ_ALWAYS with jump to block with no other jumps to it (countOfInEdges() = 1)
     */

    BasicBlock* prev;
    BasicBlock* block;
    for (prev = nullptr, block = fgFirstBB; block != nullptr; prev = block, block = block->Next())
    {
        /* no unreachable blocks */

        if ((block->countOfInEdges() == 0) && !block->HasFlag(BBF_DONT_REMOVE))
        {
            noway_assert(!"Unreachable block not removed!");
        }

        /* no empty blocks */

        if (block->isEmpty() && !block->HasFlag(BBF_DONT_REMOVE))
        {
            switch (block->GetKind())
            {
                case BBJ_CALLFINALLY:
                case BBJ_EHFINALLYRET:
                case BBJ_EHFAULTRET:
                case BBJ_EHFILTERRET:
                case BBJ_RETURN:
                /* for BBJ_ALWAYS is probably just a GOTO, but will have to be treated */
                case BBJ_ALWAYS:
                case BBJ_EHCATCHRET:
                    /* These jump kinds are allowed to have empty tree lists */
                    break;

                default:
                    /* it may be the case that the block had more than one reference to it
                     * so we couldn't remove it */

                    if (block->countOfInEdges() == 0)
                    {
                        noway_assert(!"Empty block not removed!");
                    }
                    break;
            }
        }

        /* no un-imported blocks */

        if (!block->HasFlag(BBF_IMPORTED))
        {
            /* internal blocks do not count */

            if (!block->HasFlag(BBF_INTERNAL))
            {
                noway_assert(!"Non IMPORTED block not removed!");
            }
        }

        // Check for an unnecessary jumps to the next block.
        // A conditional branch should never jump to the next block as it can be folded into a BBJ_ALWAYS.
        if (block->KindIs(BBJ_COND) && block->TrueTargetIs(block->GetFalseTarget()))
        {
            noway_assert(!"Unnecessary jump to the next block!");
        }

        // For a BBJ_CALLFINALLY block we make sure that we are followed by a BBJ_CALLFINALLYRET block
        // or that it's a BBF_RETLESS_CALL.
        if (block->KindIs(BBJ_CALLFINALLY))
        {
            assert(block->HasFlag(BBF_RETLESS_CALL) || block->isBBCallFinallyPair());
        }

        /* no un-compacted blocks */

        if (fgCanCompactBlocks(block, block->Next()))
        {
            noway_assert(!"Found un-compacted blocks!");
        }
    }
}

#endif // DEBUG

#if DUMP_FLOWGRAPHS

struct escapeMapping_t
{
    char        ch;
    const char* sub;
};

// clang-format off
static escapeMapping_t s_EscapeFileMapping[] =
{
    {' ', "_"},
    {':', "_"},
    {',', "_"},
    {'<', "~lt~"},
    {'>', "~gt~"},
    {';', "~semi~"},
    {'|', "~bar~"},
    {'&', "~amp~"},
    {'"', "~quot~"},
    {'*', "~star~"},
    {0, nullptr}
};

static escapeMapping_t s_EscapeMapping[] =
{
    {'<', "&lt;"},
    {'>', "&gt;"},
    {'&', "&amp;"},
    {'"', "&quot;"},
    {0, nullptr}
};
// clang-format on

const char* Compiler::fgProcessEscapes(const char* nameIn, escapeMapping_t* map)
{
    const char* nameOut = nameIn;
    unsigned    lengthOut;
    unsigned    index;
    bool        match;
    bool        substitutionRequired;
    const char* pChar;

    lengthOut            = 1;
    substitutionRequired = false;
    pChar                = nameIn;
    while (*pChar != '\0')
    {
        match = false;
        index = 0;
        while (map[index].ch != 0)
        {
            if (*pChar == map[index].ch)
            {
                match = true;
                break;
            }
            index++;
        }
        if (match)
        {
            substitutionRequired = true;
            lengthOut += (unsigned)strlen(map[index].sub);
        }
        else
        {
            lengthOut += 1;
        }
        pChar++;
    }

    if (substitutionRequired)
    {
        char* newName = getAllocator(CMK_DebugOnly).allocate<char>(lengthOut);
        char* pDest;
        pDest = newName;
        pChar = nameIn;
        while (*pChar != '\0')
        {
            match = false;
            index = 0;
            while (map[index].ch != 0)
            {
                if (*pChar == map[index].ch)
                {
                    match = true;
                    break;
                }
                index++;
            }
            if (match)
            {
                strcpy(pDest, map[index].sub);
                pDest += strlen(map[index].sub);
            }
            else
            {
                *pDest++ = *pChar;
            }
            pChar++;
        }
        *pDest++ = '\0';
        nameOut  = (const char*)newName;
    }

    return nameOut;
}

static void fprintfDouble(FILE* fgxFile, double value)
{
    assert(value >= 0.0);

    if ((value >= 0.010) || (value == 0.0))
    {
        fprintf(fgxFile, "\"%7.3f\"", value);
    }
    else if (value >= 0.00010)
    {
        fprintf(fgxFile, "\"%7.5f\"", value);
    }
    else
    {
        fprintf(fgxFile, "\"%7E\"", value);
    }
}

//------------------------------------------------------------------------
// fgDumpTree: Dump a tree into the DOT file. Used to provide a very short, one-line,
// visualization of a BBJ_COND block.
//
// Arguments:
//    fgxFile - The file we are writing to.
//    tree    - The operand to dump.
//
// static
void Compiler::fgDumpTree(FILE* fgxFile, GenTree* const tree)
{
    if (tree->OperIsCompare())
    {
        // Want to generate something like:
        //   V01 <= 7
        //   V01 > V02

        const char* opName = GenTree::OpName(tree->OperGet());
        // Make it look nicer if we can
        switch (tree->OperGet())
        {
            case GT_EQ:
                opName = "==";
                break;
            case GT_NE:
                opName = "!=";
                break;
            case GT_LT:
                opName = "<";
                break;
            case GT_LE:
                opName = "<=";
                break;
            case GT_GE:
                opName = ">=";
                break;
            case GT_GT:
                opName = ">";
                break;
            default:
                break;
        }

        GenTree* const lhs = tree->AsOp()->gtOp1;
        GenTree* const rhs = tree->AsOp()->gtOp2;

        fgDumpTree(fgxFile, lhs);
        fprintf(fgxFile, " %s ", opName);
        fgDumpTree(fgxFile, rhs);
    }
    else if (tree->IsCnsIntOrI())
    {
        fprintf(fgxFile, "%d", tree->AsIntCon()->gtIconVal);
    }
    else if (tree->IsCnsFltOrDbl())
    {
        fprintf(fgxFile, "%g", tree->AsDblCon()->DconValue());
    }
    else if (tree->IsLocal())
    {
        fprintf(fgxFile, "V%02u", tree->AsLclVarCommon()->GetLclNum());
    }
    else if (tree->OperIs(GT_ARR_LENGTH))
    {
        GenTreeArrLen* arrLen = tree->AsArrLen();
        GenTree*       arr    = arrLen->ArrRef();
        fgDumpTree(fgxFile, arr);
        fprintf(fgxFile, ".Length");
    }
    else if (tree->OperIs(GT_MDARR_LENGTH))
    {
        GenTreeMDArr* arrOp = tree->AsMDArr();
        GenTree*      arr   = arrOp->ArrRef();
        unsigned      dim   = arrOp->Dim();
        fgDumpTree(fgxFile, arr);
        fprintf(fgxFile, ".GetLength(%u)", dim);
    }
    else if (tree->OperIs(GT_MDARR_LOWER_BOUND))
    {
        GenTreeMDArr* arrOp = tree->AsMDArr();
        GenTree*      arr   = arrOp->ArrRef();
        unsigned      dim   = arrOp->Dim();
        fgDumpTree(fgxFile, arr);
        fprintf(fgxFile, ".GetLowerBound(%u)", dim);
    }
    else
    {
        fprintf(fgxFile, "[%s]", GenTree::OpName(tree->OperGet()));
    }
}

#ifdef DEBUG
namespace
{
const char* ConvertToUtf8(LPCWSTR wideString, CompAllocator& allocator)
{
    int utf8Len = WszWideCharToMultiByte(CP_UTF8, 0, wideString, -1, nullptr, 0, nullptr, nullptr);
    if (utf8Len == 0)
        return nullptr;

    char* alloc = (char*)allocator.allocate<char>(utf8Len);
    if (0 == WszWideCharToMultiByte(CP_UTF8, 0, wideString, -1, alloc, utf8Len, nullptr, nullptr))
        return nullptr;

    return alloc;
}
}
#endif

//------------------------------------------------------------------------
// fgOpenFlowGraphFile: Open a file to dump either the xml or dot format flow graph
//
// Arguments:
//    wbDontClose - A boolean out argument that indicates whether the caller should close the file
//    phase       - A phase identifier to indicate which phase is associated with the dump
//    pos         - Are we being called to dump the flow graph pre-phase or post-phase?
//    type        - A (wide) string indicating the type of dump, "dot" or "xml"
//
// Notes:
// The filename to use to write the data comes from the DOTNET_JitDumpFgFile or DOTNET_NgenDumpFgFile
// configuration. If unset, use "default". The "type" argument is used as a filename extension,
// e.g., "default.dot".
//
// There are several "special" filenames recognized:
// "profiled" -- only create graphs for methods with profile info, one file per method.
// "hot" -- only create graphs for the hot region, one file per method.
// "cold" -- only create graphs for the cold region, one file per method.
// "jit" -- only create graphs for JITing, one file per method.
// "all" -- create graphs for all regions, one file per method.
// "stdout" -- output to stdout, not a file.
// "stderr" -- output to stderr, not a file.
//
// Return Value:
//    Opens a file to which a flowgraph can be dumped, whose name is based on the current
//    config vales.
//
FILE* Compiler::fgOpenFlowGraphFile(bool* wbDontClose, Phases phase, PhasePosition pos, const char* type)
{
    FILE*       fgxFile;
    const char* prePhasePattern  = nullptr; // pre-phase:  default (used in Release) is no pre-phase dump
    const char* postPhasePattern = "*";     // post-phase: default (used in Release) is dump all phases
    bool        dumpFunction     = true;    // default (used in Release) is always dump
    const char* filename         = nullptr;
    const char* pathname         = nullptr;
    const char* escapedString;

    if (fgBBcount <= 1)
    {
        return nullptr;
    }

#ifdef DEBUG
    dumpFunction = JitConfig.JitDumpFg().contains(info.compMethodHnd, info.compClassHnd, &info.compMethodInfo->args);

    CompAllocator allocator = getAllocatorDebugOnly();
    filename                = ConvertToUtf8(JitConfig.JitDumpFgFile(), allocator);
    pathname                = ConvertToUtf8(JitConfig.JitDumpFgDir(), allocator);
    prePhasePattern         = ConvertToUtf8(JitConfig.JitDumpFgPrePhase(), allocator);
    postPhasePattern        = ConvertToUtf8(JitConfig.JitDumpFgPhase(), allocator);
#endif // DEBUG

    if (!dumpFunction)
    {
        return nullptr;
    }

    const char* phaseName = PhaseEnums[phase] + strlen("PHASE_");

    if (pos == PhasePosition::PrePhase)
    {
        if (prePhasePattern == nullptr)
        {
            // If pre-phase pattern is not specified, then don't dump for any pre-phase.
            return nullptr;
        }
        else if (*prePhasePattern != '*')
        {
            if (strstr(prePhasePattern, phaseName) == nullptr)
            {
                return nullptr;
            }
        }
    }
    else
    {
        assert(pos == PhasePosition::PostPhase);
        if (postPhasePattern == nullptr)
        {
            // There's no post-phase pattern specified. If there is a pre-phase pattern specified, then that will
            // be the only set of phases dumped. If neither are specified, then post-phase dump after
            // PHASE_DETERMINE_FIRST_COLD_BLOCK.
            if (prePhasePattern != nullptr)
            {
                return nullptr;
            }
            if (phase != PHASE_DETERMINE_FIRST_COLD_BLOCK)
            {
                return nullptr;
            }
        }
        else if (*postPhasePattern != '*')
        {
            if (strstr(postPhasePattern, phaseName) == nullptr)
            {
                return nullptr;
            }
        }
    }

    if (filename == nullptr)
    {
        filename = "default";
    }

    if (strcmp(filename, "profiled") == 0)
    {
        if (fgFirstBB->hasProfileWeight())
        {
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    if (strcmp(filename, "hot") == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_HOT)
        {
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (strcmp(filename, "cold") == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_COLD)
        {
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (strcmp(filename, "jit") == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_JIT)
        {
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (strcmp(filename, "all") == 0)
    {

    ONE_FILE_PER_METHOD:;

#define FILENAME_PATTERN "%s-%s-%s-%s.%s"
#define FILENAME_PATTERN_WITH_NUMBER "%s-%s-%s-%s~%d.%s"

        const size_t MaxFileNameLength = MAX_PATH_FNAME - 20 /* give us some extra buffer */;

        escapedString           = fgProcessEscapes(info.compFullName, s_EscapeFileMapping);
        size_t escapedStringLen = strlen(escapedString);

        static const char* phasePositionStrings[] = {"pre", "post"};
        assert((unsigned)pos < ArrLen(phasePositionStrings));
        const char*  phasePositionString    = phasePositionStrings[(unsigned)pos];
        const size_t phasePositionStringLen = strlen(phasePositionString);
        const char*  tierName               = compGetTieringName(true);
        size_t       charCount = escapedStringLen + 1 + strlen(phasePositionString) + 1 + strlen(phaseName) + 1 +
                           strlen(tierName) + strlen("~999") + 1 + strlen(type) + 1;

        if (charCount > MaxFileNameLength)
        {
            // Crop the escapedString.
            charCount -= escapedStringLen;
            size_t newEscapedStringLen = MaxFileNameLength - charCount;
            char*  newEscapedString    = getAllocator(CMK_DebugOnly).allocate<char>(newEscapedStringLen + 1);
            strncpy_s(newEscapedString, newEscapedStringLen + 1, escapedString, newEscapedStringLen);
            newEscapedString[newEscapedStringLen] = '\0';
            escapedString                         = newEscapedString;
            escapedStringLen                      = newEscapedStringLen;
            charCount += escapedStringLen;
        }

        if (pathname != nullptr)
        {
            charCount += strlen(pathname) + 1;
        }
        filename = (const char*)_alloca(charCount * sizeof(char));

        if (pathname != nullptr)
        {
            sprintf_s((char*)filename, charCount, "%s\\" FILENAME_PATTERN, pathname, escapedString, phasePositionString,
                      phaseName, tierName, type);
        }
        else
        {
            sprintf_s((char*)filename, charCount, FILENAME_PATTERN, escapedString, phasePositionString, phaseName,
                      tierName, type);
        }
        fgxFile = fopen(filename, "wx"); // Open the file for writing only only if it doesn't already exist
        if (fgxFile == nullptr)
        {
            // This filename already exists, so create a different one by appending ~2, ~3, etc...
            for (int i = 2; i < 1000; i++)
            {
                if (pathname != nullptr)
                {
                    sprintf_s((char*)filename, charCount, "%s\\" FILENAME_PATTERN_WITH_NUMBER, pathname, escapedString,
                              phasePositionString, phaseName, tierName, i, type);
                }
                else
                {
                    sprintf_s((char*)filename, charCount, FILENAME_PATTERN_WITH_NUMBER, escapedString,
                              phasePositionString, phaseName, tierName, i, type);
                }
                fgxFile = fopen(filename, "wx"); // Open the file for writing only only if it doesn't already exist
                if (fgxFile != nullptr)
                {
                    break;
                }
            }
            // If we have already created 1000 files with this name then just fail
            if (fgxFile == nullptr)
            {
                return nullptr;
            }
        }
        *wbDontClose = false;
    }
    else if (strcmp(filename, "stdout") == 0)
    {
        fgxFile      = jitstdout();
        *wbDontClose = true;
    }
    else if (strcmp(filename, "stderr") == 0)
    {
        fgxFile      = stderr;
        *wbDontClose = true;
    }
    else
    {
        const char* origFilename = filename;
        size_t      charCount    = strlen(origFilename) + strlen(type) + 2;
        if (pathname != nullptr)
        {
            charCount += strlen(pathname) + 1;
        }
        filename = (char*)_alloca(charCount * sizeof(char));
        if (pathname != nullptr)
        {
            sprintf_s((char*)filename, charCount, "%s\\%s.%s", pathname, origFilename, type);
        }
        else
        {
            sprintf_s((char*)filename, charCount, "%s.%s", origFilename, type);
        }
        fgxFile      = fopen(filename, "a+");
        *wbDontClose = false;
    }

    return fgxFile;
}

//------------------------------------------------------------------------
// fgDumpFlowGraph: Dump the xml or dot format flow graph, if enabled for this phase.
//
// Arguments:
//    phase       - A phase identifier to indicate which phase is associated with the dump,
//                  i.e. which phase has just completed.
//    pos         - Are we being called to dump the flow graph pre-phase or post-phase?
//
// Return Value:
//    True iff a flowgraph has been dumped.
//
// Notes:
//    The xml dumps are the historical mechanism for dumping the flowgraph.
//    The dot format can be viewed by:
//    - https://sketchviz.com/
//    - Graphviz (http://www.graphviz.org/)
//      - The command:
//           "C:\Program Files (x86)\Graphviz2.38\bin\dot.exe" -Tsvg -oFoo.svg -Kdot Foo.dot
//        will produce a Foo.svg file that can be opened with any svg-capable browser.
//    - http://rise4fun.com/Agl/
//      - Cut and paste the graph from your .dot file, replacing the digraph on the page, and then click the play
//        button.
//      - It will show a rotating '/' and then render the graph in the browser.
//    MSAGL has also been open-sourced to https://github.com/Microsoft/automatic-graph-layout.
//
//    Here are the config values that control it:
//      DOTNET_JitDumpFg              A string (ala the DOTNET_JitDump string) indicating what methods to dump
//                                     flowgraphs for.
//      DOTNET_JitDumpFgDir           A path to a directory into which the flowgraphs will be dumped.
//      DOTNET_JitDumpFgFile          The filename to use. The default is "default.[xml|dot]".
//                                     Note that the new graphs will be appended to this file if it already exists.
//      DOTNET_JitDumpFgPhase         Phase(s) after which to dump the flowgraph.
//                                     Set to the short name of a phase to see the flowgraph after that phase.
//                                     Leave unset to dump after COLD-BLK (determine first cold block) or set to *
//                                     for all phases.
//      DOTNET_JitDumpFgPrePhase      Phase(s) before which to dump the flowgraph.
//      DOTNET_JitDumpFgDot           0 for xml format, non-zero for dot format. (Default is dot format.)
//      DOTNET_JitDumpFgEH            (dot only) 0 for no exception-handling information; non-zero to include
//                                     exception-handling regions.
//      DOTNET_JitDumpFgLoops         (dot only) 0 for no loop information; non-zero to include loop regions.
//      DOTNET_JitDumpFgConstrained   (dot only) 0 == don't constrain to mostly linear layout; non-zero == force
//                                     mostly lexical block linear layout.
//      DOTNET_JitDumpFgBlockId       Display blocks with block ID, not just bbNum.
//
// Example:
//
// If you want to dump just before and after a single phase, say loop cloning, use:
//      set DOTNET_JitDumpFgPhase=LP-CLONE
//      set DOTNET_JitDumpFgPrePhase=LP-CLONE
//
bool Compiler::fgDumpFlowGraph(Phases phase, PhasePosition pos)
{
    bool result    = false;
    bool dontClose = false;

#ifdef DEBUG
    const bool createDotFile     = JitConfig.JitDumpFgDot() != 0;
    const bool includeEH         = (JitConfig.JitDumpFgEH() != 0) && !compIsForInlining();
    const bool includeLoops      = (JitConfig.JitDumpFgLoops() != 0) && !compIsForInlining();
    const bool constrained       = JitConfig.JitDumpFgConstrained() != 0;
    const bool useBlockId        = JitConfig.JitDumpFgBlockID() != 0;
    const bool displayBlockFlags = JitConfig.JitDumpFgBlockFlags() != 0;
#else  // !DEBUG
    const bool createDotFile     = true;
    const bool includeEH         = false;
    const bool includeLoops      = false;
    const bool constrained       = true;
    const bool useBlockId        = false;
    const bool displayBlockFlags = false;
#endif // !DEBUG

    FILE* fgxFile = fgOpenFlowGraphFile(&dontClose, phase, pos, createDotFile ? "dot" : "fgx");
    if (fgxFile == nullptr)
    {
        return false;
    }

    JITDUMP("Writing out flow graph %s phase %s\n", (pos == PhasePosition::PrePhase) ? "before" : "after",
            PhaseNames[phase]);

    bool        validWeights  = fgHaveValidEdgeWeights;
    double      weightDivisor = (double)BasicBlock::getCalledCount(this);
    const char* escapedString;
    const char* regionString = "NONE";

    if (info.compMethodInfo->regionKind == CORINFO_REGION_HOT)
    {
        regionString = "HOT";
    }
    else if (info.compMethodInfo->regionKind == CORINFO_REGION_COLD)
    {
        regionString = "COLD";
    }
    else if (info.compMethodInfo->regionKind == CORINFO_REGION_JIT)
    {
        regionString = "JIT";
    }

    if (createDotFile)
    {
        fprintf(fgxFile, "digraph FlowGraph {\n");
        fprintf(fgxFile, "    graph [label = \"%s%s\\n%s\\n%s\"];\n", info.compMethodName,
                compIsForInlining() ? "\\n(inlinee)" : "", (pos == PhasePosition::PrePhase) ? "before" : "after",
                PhaseNames[phase]);
        fprintf(fgxFile, "    node [shape = \"Box\"];\n");
    }
    else
    {
        fprintf(fgxFile, "<method");

        escapedString = fgProcessEscapes(info.compFullName, s_EscapeMapping);
        fprintf(fgxFile, "\n    name=\"%s\"", escapedString);

        escapedString = fgProcessEscapes(info.compClassName, s_EscapeMapping);
        fprintf(fgxFile, "\n    className=\"%s\"", escapedString);

        escapedString = fgProcessEscapes(info.compMethodName, s_EscapeMapping);
        fprintf(fgxFile, "\n    methodName=\"%s\"", escapedString);
        fprintf(fgxFile, "\n    ngenRegion=\"%s\"", regionString);

        fprintf(fgxFile, "\n    bytesOfIL=\"%d\"", info.compILCodeSize);
        fprintf(fgxFile, "\n    localVarCount=\"%d\"", lvaCount);

        if (fgHaveProfileWeights())
        {
            fprintf(fgxFile, "\n    calledCount=\"%f\"", fgCalledCount);
            fprintf(fgxFile, "\n    profileData=\"true\"");
        }
        if (compHndBBtabCount > 0)
        {
            fprintf(fgxFile, "\n    hasEHRegions=\"true\"");
        }
        if (fgHasLoops)
        {
            fprintf(fgxFile, "\n    hasLoops=\"true\"");
        }
        if (validWeights)
        {
            fprintf(fgxFile, "\n    validEdgeWeights=\"true\"");
            if (!fgSlopUsedInEdgeWeights && !fgRangeUsedInEdgeWeights)
            {
                fprintf(fgxFile, "\n    exactEdgeWeights=\"true\"");
            }
        }
        if (fgFirstColdBlock != nullptr)
        {
            fprintf(fgxFile, "\n    firstColdBlock=\"%d\"", fgFirstColdBlock->bbNum);
        }

        fprintf(fgxFile, ">");

        fprintf(fgxFile, "\n    <blocks");
        fprintf(fgxFile, "\n        blockCount=\"%d\"", fgBBcount);
        fprintf(fgxFile, ">");
    }

    // In some cases, we want to change the display based on whether an edge is lexically backwards, forwards,
    // or lexical successor. Also, for the region tree, using the lexical order is useful for determining where
    // to insert in the tree, to determine nesting. We'd like to use the bbNum to do this. However, we don't
    // want to renumber the blocks. So, create a mapping of bbNum to ordinal, and compare block order by
    // comparing the mapped ordinals instead.
    //
    // For inlinees, the max block number of the inliner is used, so we need to allocate the block map based on
    // that size, even though it means allocating a block map possibly much bigger than what's required for just
    // the inlinee blocks.

    unsigned  blkMapSize   = 1 + fgBBNumMax;
    unsigned  blockOrdinal = 1;
    unsigned* blkMap       = new (this, CMK_DebugOnly) unsigned[blkMapSize];
    memset(blkMap, 0, sizeof(unsigned) * blkMapSize);
    for (BasicBlock* const block : Blocks())
    {
        assert(block->bbNum < blkMapSize);
        blkMap[block->bbNum] = blockOrdinal++;
    }

    static const char* kindImage[] = {"EHFINALLYRET", "EHFILTERRET", "EHCATCHRET",  "THROW", "RETURN", "NONE",
                                      "ALWAYS",       "LEAVE",       "CALLFINALLY", "COND",  "SWITCH"};

    BasicBlock* block;
    for (block = fgFirstBB, blockOrdinal = 1; block != nullptr; block = block->Next(), blockOrdinal++)
    {
        if (createDotFile)
        {
            fprintf(fgxFile, "    " FMT_BB " [label = \"", block->bbNum);

            if (useBlockId)
            {
                fprintf(fgxFile, "%s", block->dspToString());
            }
            else
            {
                fprintf(fgxFile, FMT_BB, block->bbNum);
            }

            if (displayBlockFlags)
            {
                // Don't display the `[` `]` unless we're going to display something.
                const bool isTryEntryBlock = bbIsTryBeg(block);

                if (isTryEntryBlock ||
                    block->HasAnyFlag(BBF_FUNCLET_BEG | BBF_RUN_RARELY | BBF_LOOP_HEAD | BBF_LOOP_PREHEADER |
                                      BBF_LOOP_ALIGN))
                {
                    // Display a very few, useful, block flags
                    fprintf(fgxFile, " [");
                    if (isTryEntryBlock)
                    {
                        fprintf(fgxFile, "T");
                    }
                    if (block->HasFlag(BBF_FUNCLET_BEG))
                    {
                        fprintf(fgxFile, "F");
                    }
                    if (block->HasFlag(BBF_RUN_RARELY))
                    {
                        fprintf(fgxFile, "R");
                    }
                    if (block->HasFlag(BBF_LOOP_HEAD))
                    {
                        fprintf(fgxFile, "L");
                    }
                    if (block->HasFlag(BBF_LOOP_PREHEADER))
                    {
                        fprintf(fgxFile, "P");
                    }
                    if (block->HasFlag(BBF_LOOP_ALIGN))
                    {
                        fprintf(fgxFile, "A");
                    }
                    fprintf(fgxFile, "]");
                }
            }

            // Optionally show GC Heap Mem SSA state and Memory Phis
            //
            if ((JitConfig.JitDumpFgMemorySsa() != 0) && (fgSsaPassesCompleted > 0))
            {
                fprintf(fgxFile, "\\n");

                MemoryKind     k      = MemoryKind::GcHeap;
                const unsigned ssaIn  = block->bbMemorySsaNumIn[k];
                const unsigned ssaOut = block->bbMemorySsaNumOut[k];

                if (ssaIn != SsaConfig::RESERVED_SSA_NUM)
                {
                    ValueNum                  vnIn   = GetMemoryPerSsaData(ssaIn)->m_vnPair.GetLiberal();
                    BasicBlock::MemoryPhiArg* memPhi = block->bbMemorySsaPhiFunc[k];
                    if ((memPhi != nullptr) && (memPhi != BasicBlock::EmptyMemoryPhiDef))
                    {
                        fprintf(fgxFile, "MI %d " FMT_VN " = PHI(", ssaIn, vnIn);
                        bool first = true;
                        for (; memPhi != nullptr; memPhi = memPhi->m_nextArg)
                        {
                            ValueNum phiVN = GetMemoryPerSsaData(memPhi->GetSsaNum())->m_vnPair.GetLiberal();
                            fprintf(fgxFile, "%s%d " FMT_VN, first ? "" : ",", memPhi->m_ssaNum, phiVN);
                            first = false;
                        }
                        fprintf(fgxFile, ")");
                    }
                    else
                    {
                        ValueNum vn = GetMemoryPerSsaData(block->bbMemorySsaNumIn[k])->m_vnPair.GetLiberal();
                        fprintf(fgxFile, "MI %d " FMT_VN, block->bbMemorySsaNumIn[k], vn);
                    }
                    fprintf(fgxFile, "\\n");

                    if (block->bbMemoryHavoc != 0)
                    {
                        fprintf(fgxFile, "** HAVOC **\\n");
                    }

                    ValueNum vnOut = GetMemoryPerSsaData(ssaOut)->m_vnPair.GetLiberal();
                    fprintf(fgxFile, "MO %d " FMT_VN, ssaOut, vnOut);
                }
            }

            if (block->KindIs(BBJ_COND))
            {
                fprintf(fgxFile, "\\n");

                // Include a line with the basics of the branch condition, if possible.
                // Find the loop termination test at the bottom of the loop.
                Statement* condStmt = block->lastStmt();
                if (condStmt != nullptr)
                {
                    GenTree* const condTree = condStmt->GetRootNode();
                    noway_assert(condTree->gtOper == GT_JTRUE);
                    GenTree* const compareTree = condTree->AsOp()->gtOp1;
                    fgDumpTree(fgxFile, compareTree);
                }
            }

            // "Raw" Profile weight
            if (block->hasProfileWeight() || (JitConfig.JitSynthesizeCounts() > 0))
            {
                fprintf(fgxFile, "\\n\\n%7.2f", ((double)block->getBBWeight(this)) / BB_UNITY_WEIGHT);
            }

            // end of block label
            fprintf(fgxFile, "\"");

            // other node attributes
            //
            if (block == fgFirstBB)
            {
                fprintf(fgxFile, ", shape = \"house\"");
            }
            else if (block->KindIs(BBJ_RETURN))
            {
                fprintf(fgxFile, ", shape = \"invhouse\"");
            }
            else if (block->KindIs(BBJ_THROW))
            {
                fprintf(fgxFile, ", shape = \"trapezium\"");
            }
            else if (block->HasFlag(BBF_INTERNAL))
            {
                fprintf(fgxFile, ", shape = \"note\"");
            }

            fprintf(fgxFile, "];\n");
        }
        else
        {
            fprintf(fgxFile, "\n        <block");
            fprintf(fgxFile, "\n            id=\"%d\"", block->bbNum);
            fprintf(fgxFile, "\n            ordinal=\"%d\"", blockOrdinal);
            fprintf(fgxFile, "\n            jumpKind=\"%s\"", kindImage[block->GetKind()]);
            if (block->hasTryIndex())
            {
                fprintf(fgxFile, "\n            inTry=\"%s\"", "true");
            }
            if (block->hasHndIndex())
            {
                fprintf(fgxFile, "\n            inHandler=\"%s\"", "true");
            }
            if ((fgFirstBB->hasProfileWeight()) && !block->HasFlag(BBF_COLD))
            {
                fprintf(fgxFile, "\n            hot=\"true\"");
            }
            if (block->HasFlag(BBF_HAS_NEWOBJ))
            {
                fprintf(fgxFile, "\n            callsNew=\"true\"");
            }
            if (block->HasFlag(BBF_LOOP_HEAD))
            {
                fprintf(fgxFile, "\n            loopHead=\"true\"");
            }

            const char* rootTreeOpName = "n/a";
            if (block->IsLIR() || (block->lastStmt() != nullptr))
            {
                if (block->lastNode() != nullptr)
                {
                    rootTreeOpName = GenTree::OpName(block->lastNode()->OperGet());
                }
            }

            fprintf(fgxFile, "\n            weight=");
            fprintfDouble(fgxFile, ((double)block->bbWeight) / weightDivisor);
            // fgGetCodeEstimate() will assert if the costs have not yet been initialized.
            // fprintf(fgxFile, "\n            codeEstimate=\"%d\"", fgGetCodeEstimate(block));
            fprintf(fgxFile, "\n            startOffset=\"%d\"", block->bbCodeOffs);
            fprintf(fgxFile, "\n            rootTreeOp=\"%s\"", rootTreeOpName);
            fprintf(fgxFile, "\n            endOffset=\"%d\"", block->bbCodeOffsEnd);
            fprintf(fgxFile, ">");
            fprintf(fgxFile, "\n        </block>");
        }
    }

    if (!createDotFile)
    {
        fprintf(fgxFile, "\n    </blocks>");

        fprintf(fgxFile, "\n    <edges");
        fprintf(fgxFile, "\n        edgeCount=\"%d\"", fgEdgeCount);
        fprintf(fgxFile, ">");
    }

    if (fgPredsComputed)
    {
        unsigned    edgeNum = 1;
        BasicBlock* bTarget;
        for (bTarget = fgFirstBB; bTarget != nullptr; bTarget = bTarget->Next())
        {
            double targetWeightDivisor;
            if (bTarget->bbWeight == BB_ZERO_WEIGHT)
            {
                targetWeightDivisor = 1.0;
            }
            else
            {
                targetWeightDivisor = (double)bTarget->bbWeight;
            }

            for (FlowEdge* const edge : bTarget->PredEdges())
            {
                BasicBlock* bSource = edge->getSourceBlock();
                double      sourceWeightDivisor;
                if (bSource->bbWeight == BB_ZERO_WEIGHT)
                {
                    sourceWeightDivisor = 1.0;
                }
                else
                {
                    sourceWeightDivisor = (double)bSource->bbWeight;
                }
                if (createDotFile)
                {
                    fprintf(fgxFile, "    " FMT_BB " -> " FMT_BB, bSource->bbNum, bTarget->bbNum);

                    const char* sep = "";

                    if (blkMap[bSource->bbNum] > blkMap[bTarget->bbNum])
                    {
                        // Lexical backedge
                        fprintf(fgxFile, " [color=green");
                        sep = ", ";
                    }
                    else if ((blkMap[bSource->bbNum] + 1) == blkMap[bTarget->bbNum])
                    {
                        // Lexical successor
                        fprintf(fgxFile, " [color=blue, weight=20");
                        sep = ", ";
                    }
                    else
                    {
                        fprintf(fgxFile, " [");
                    }

                    if (validWeights)
                    {
                        weight_t edgeWeight = (edge->edgeWeightMin() + edge->edgeWeightMax()) / 2;
                        fprintf(fgxFile, "%slabel=\"%7.2f\"", sep, (double)edgeWeight / weightDivisor);
                    }

                    fprintf(fgxFile, "];\n");
                }
                else
                {
                    fprintf(fgxFile, "\n        <edge");
                    fprintf(fgxFile, "\n            id=\"%d\"", edgeNum);
                    fprintf(fgxFile, "\n            source=\"%d\"", bSource->bbNum);
                    fprintf(fgxFile, "\n            target=\"%d\"", bTarget->bbNum);
                    if (bSource->KindIs(BBJ_SWITCH))
                    {
                        if (edge->getDupCount() >= 2)
                        {
                            fprintf(fgxFile, "\n            switchCases=\"%d\"", edge->getDupCount());
                        }
                        if (bSource->GetSwitchTargets()->getDefault() == bTarget)
                        {
                            fprintf(fgxFile, "\n            switchDefault=\"true\"");
                        }
                    }
                    if (validWeights)
                    {
                        weight_t edgeWeight = (edge->edgeWeightMin() + edge->edgeWeightMax()) / 2;
                        fprintf(fgxFile, "\n            weight=");
                        fprintfDouble(fgxFile, ((double)edgeWeight) / weightDivisor);

                        if (edge->edgeWeightMin() != edge->edgeWeightMax())
                        {
                            fprintf(fgxFile, "\n            minWeight=");
                            fprintfDouble(fgxFile, ((double)edge->edgeWeightMin()) / weightDivisor);
                            fprintf(fgxFile, "\n            maxWeight=");
                            fprintfDouble(fgxFile, ((double)edge->edgeWeightMax()) / weightDivisor);
                        }

                        if (edgeWeight > 0)
                        {
                            if (edgeWeight < bSource->bbWeight)
                            {
                                fprintf(fgxFile, "\n            out=");
                                fprintfDouble(fgxFile, ((double)edgeWeight) / sourceWeightDivisor);
                            }
                            if (edgeWeight < bTarget->bbWeight)
                            {
                                fprintf(fgxFile, "\n            in=");
                                fprintfDouble(fgxFile, ((double)edgeWeight) / targetWeightDivisor);
                            }
                        }
                    }
                }
                if (!createDotFile)
                {
                    fprintf(fgxFile, ">");
                    fprintf(fgxFile, "\n        </edge>");
                }

                ++edgeNum;
            }
        }
    }

    // For dot, show edges w/o pred lists, and add invisible bbNext links.
    // Also, add EH and/or loop regions as "cluster" subgraphs, if requested.
    //
    if (createDotFile)
    {
        for (BasicBlock* const bSource : Blocks())
        {
            if (constrained)
            {
                // Invisible edge for bbNext chain
                //
                if (!bSource->IsLast())
                {
                    fprintf(fgxFile, "    " FMT_BB " -> " FMT_BB " [style=\"invis\", weight=25];\n", bSource->bbNum,
                            bSource->Next()->bbNum);
                }
            }

            if (fgPredsComputed)
            {
                // Already emitted pred edges above.
                //
                continue;
            }

            // Emit successor edges
            //
            for (BasicBlock* const bTarget : bSource->Succs())
            {
                fprintf(fgxFile, "    " FMT_BB " -> " FMT_BB, bSource->bbNum, bTarget->bbNum);
                if (blkMap[bSource->bbNum] > blkMap[bTarget->bbNum])
                {
                    // Lexical backedge
                    fprintf(fgxFile, " [color=green]\n");
                }
                else if ((blkMap[bSource->bbNum] + 1) == blkMap[bTarget->bbNum])
                {
                    // Lexical successor
                    fprintf(fgxFile, " [color=blue]\n");
                }
                else
                {
                    fprintf(fgxFile, ";\n");
                }
            }
        }

        if (includeEH && (compHndBBtabCount > 0))
        {
            // Generate something like:
            //    subgraph cluster_0 {
            //      label = "xxx";
            //      color = yyy;
            //      bb; bb;
            //      subgraph {
            //        label = "aaa";
            //        color = bbb;
            //        bb; bb...
            //      }
            //      ...
            //    }
            //
            // Thus, the subgraphs need to be nested to show the region nesting.
            //
            // The EH table is in order, top-to-bottom, most nested to least nested where
            // there is a parent/child relationship.
            //
            // Build a region tree, collecting all the regions we want to display,
            // and then walk it to emit the regions.

            // RegionGraph: represent non-overlapping, possibly nested, block ranges in the flow graph.
            class RegionGraph
            {
            public:
                enum class RegionType
                {
                    Root,
                    EH,
                };

            private:
                struct Region
                {
                    Region(RegionType rgnType, const char* rgnName, BasicBlock* bbStart, BasicBlock* bbEnd)
                        : m_rgnNext(nullptr)
                        , m_rgnChild(nullptr)
                        , m_rgnType(rgnType)
                        , m_bbStart(bbStart)
                        , m_bbEnd(bbEnd)
                    {
                        strcpy_s(m_rgnName, sizeof(m_rgnName), rgnName);
                    }

                    Region*     m_rgnNext;
                    Region*     m_rgnChild;
                    RegionType  m_rgnType;
                    char        m_rgnName[30];
                    BasicBlock* m_bbStart;
                    BasicBlock* m_bbEnd;
                };

            public:
                RegionGraph(Compiler* comp, unsigned* blkMap, unsigned blkMapSize)
                    : m_comp(comp), m_rgnRoot(nullptr), m_blkMap(blkMap), m_blkMapSize(blkMapSize)
                {
                    // Create a root region that encompasses the whole function.
                    m_rgnRoot =
                        new (m_comp, CMK_DebugOnly) Region(RegionType::Root, "Root", comp->fgFirstBB, comp->fgLastBB);
                }

                //------------------------------------------------------------------------
                // Insert: Insert a region [start..end] (inclusive) into the graph.
                //
                // Arguments:
                //    name    - the textual label to use for the region
                //    rgnType - the region type
                //    start   - start block of the region
                //    end     - last block of the region
                //
                void Insert(const char* name, RegionType rgnType, BasicBlock* start, BasicBlock* end)
                {
                    JITDUMP("Insert region: %s, type: %s, start: " FMT_BB ", end: " FMT_BB "\n", name,
                            GetRegionType(rgnType), start->bbNum, end->bbNum);

                    assert(start != nullptr);
                    assert(end != nullptr);

                    Region*  newRgn          = new (m_comp, CMK_DebugOnly) Region(rgnType, name, start, end);
                    unsigned newStartOrdinal = m_blkMap[start->bbNum];
                    unsigned newEndOrdinal   = m_blkMap[end->bbNum];

                    Region*  curRgn          = m_rgnRoot;
                    unsigned curStartOrdinal = m_blkMap[curRgn->m_bbStart->bbNum];
                    unsigned curEndOrdinal   = m_blkMap[curRgn->m_bbEnd->bbNum];

                    // A range can be a single block, but there can be no overlap between ranges.
                    assert(newStartOrdinal <= newEndOrdinal);
                    assert(curStartOrdinal <= curEndOrdinal);
                    assert(newStartOrdinal >= curStartOrdinal);
                    assert(newEndOrdinal <= curEndOrdinal);

                    // We know the new region will be part of the current region. Should it be a direct
                    // child, or put within one of the existing children?
                    Region** lastChildPtr = &curRgn->m_rgnChild;
                    Region*  child        = curRgn->m_rgnChild;
                    while (child != nullptr)
                    {
                        unsigned childStartOrdinal = m_blkMap[child->m_bbStart->bbNum];
                        unsigned childEndOrdinal   = m_blkMap[child->m_bbEnd->bbNum];

                        // Consider the following cases, where each "x" is a block in the range:
                        //    xxxxxxx      // current 'child' range; we're comparing against this
                        //    xxxxxxx      // (1) same range; could be considered child or parent
                        //  xxxxxxxxx      // (2) parent range, shares last block
                        //    xxxxxxxxx    // (3) parent range, shares first block
                        //  xxxxxxxxxxx    // (4) fully overlapping parent range
                        // xx              // (5) non-overlapping preceding sibling range
                        //            xx   // (6) non-overlapping following sibling range
                        //      xxx        // (7) child range
                        //    xxx          // (8) child range, shares same start block
                        //    x            // (9) single-block child range, shares same start block
                        //        xxx      // (10) child range, shares same end block
                        //          x      // (11) single-block child range, shares same end block
                        //  xxxxxxx        // illegal: overlapping ranges
                        //  xxx            // illegal: overlapping ranges (shared child start block and new end block)
                        //      xxxxxxx    // illegal: overlapping ranges
                        //          xxx    // illegal: overlapping ranges (shared child end block and new start block)

                        // Assert the child is properly nested within the parent.
                        // Note that if regions have the same start and end, you can't tell which is nested within the
                        // other, though it shouldn't matter.
                        assert(childStartOrdinal <= childEndOrdinal);
                        assert(curStartOrdinal <= childStartOrdinal);
                        assert(childEndOrdinal <= curEndOrdinal);

                        // Should the new region be before this child?
                        // Case (5).
                        if (newEndOrdinal < childStartOrdinal)
                        {
                            // Insert before this child.
                            newRgn->m_rgnNext = child;
                            *lastChildPtr     = newRgn;
                            break;
                        }
                        else if ((newStartOrdinal >= childStartOrdinal) && (newEndOrdinal <= childEndOrdinal))
                        {
                            // Insert as a child of this child.
                            // Need to recurse to walk the child's children list to see where it belongs.
                            // Case (1), (7), (8), (9), (10), (11).

                            curStartOrdinal = m_blkMap[child->m_bbStart->bbNum];
                            curEndOrdinal   = m_blkMap[child->m_bbEnd->bbNum];

                            lastChildPtr = &child->m_rgnChild;
                            child        = child->m_rgnChild;

                            continue;
                        }
                        else if (newStartOrdinal <= childStartOrdinal)
                        {
                            // The new region is a parent of one or more of the existing children.
                            // Case (2), (3), (4).

                            // Find all the children it encompasses.
                            Region** lastEndChildPtr = &child->m_rgnNext;
                            Region*  endChild        = child->m_rgnNext;
                            while (endChild != nullptr)
                            {
                                unsigned endChildStartOrdinal = m_blkMap[endChild->m_bbStart->bbNum];
                                unsigned endChildEndOrdinal   = m_blkMap[endChild->m_bbEnd->bbNum];
                                assert(endChildStartOrdinal <= endChildEndOrdinal);

                                if (newEndOrdinal < endChildStartOrdinal)
                                {
                                    // Found the range
                                    break;
                                }

                                lastEndChildPtr = &endChild->m_rgnNext;
                                endChild        = endChild->m_rgnNext;
                            }

                            // The range is [child..endChild previous]. If endChild is nullptr, then
                            // the range is to the end of the parent. Move these all to be
                            // children of newRgn, and put newRgn in where `child` is.
                            newRgn->m_rgnNext = endChild;
                            *lastChildPtr     = newRgn;

                            newRgn->m_rgnChild = child;
                            *lastEndChildPtr   = nullptr;

                            break;
                        }

                        // Else, look for next child.
                        // Case (6).

                        lastChildPtr = &child->m_rgnNext;
                        child        = child->m_rgnNext;
                    }

                    if (child == nullptr)
                    {
                        // Insert as the last child (could be the only child).
                        *lastChildPtr = newRgn;
                    }
                }

#ifdef DEBUG

                const unsigned dumpIndentIncrement = 2; // How much to indent each nested level.

                //------------------------------------------------------------------------
                // GetRegionType: get a textual name for the region type, to be used in dumps.
                //
                // Arguments:
                //    rgnType - the region type
                //
                static const char* GetRegionType(RegionType rgnType)
                {
                    switch (rgnType)
                    {
                        case RegionType::Root:
                            return "Root";
                        case RegionType::EH:
                            return "EH";
                        default:
                            return "UNKNOWN";
                    }
                }

                //------------------------------------------------------------------------
                // DumpRegionNode: Region graph dump helper to dump a region node at the given indent,
                // and recursive dump its children.
                //
                // Arguments:
                //    rgn    - the region to dump
                //    indent - number of leading characters to indent all output
                //
                void DumpRegionNode(Region* rgn, unsigned indent) const
                {
                    printf("%*s======\n", indent, "");
                    printf("%*sType: %s\n", indent, "", GetRegionType(rgn->m_rgnType));
                    printf("%*sName: %s\n", indent, "", rgn->m_rgnName);
                    printf("%*sRange: " FMT_BB ".." FMT_BB "\n", indent, "", rgn->m_bbStart->bbNum,
                           rgn->m_bbEnd->bbNum);

                    for (Region* child = rgn->m_rgnChild; child != nullptr; child = child->m_rgnNext)
                    {
                        DumpRegionNode(child, indent + dumpIndentIncrement);
                    }
                }

                //------------------------------------------------------------------------
                // Dump: dump the entire region graph
                //
                void Dump()
                {
                    printf("Region graph:\n");
                    DumpRegionNode(m_rgnRoot, 0);
                    printf("\n");
                }

                //------------------------------------------------------------------------
                // VerifyNode: verify the region graph rooted at `rgn`.
                //
                // Arguments:
                //    rgn  - the node (and its children) to check.
                //
                void Verify(Region* rgn)
                {
                    // The region needs to be a non-overlapping parent to all its children.
                    // The children need to be non-overlapping, and in increasing order.

                    unsigned rgnStartOrdinal = m_blkMap[rgn->m_bbStart->bbNum];
                    unsigned rgnEndOrdinal   = m_blkMap[rgn->m_bbEnd->bbNum];
                    assert(rgnStartOrdinal <= rgnEndOrdinal);

                    Region* child     = rgn->m_rgnChild;
                    Region* lastChild = nullptr;
                    if (child != nullptr)
                    {
                        unsigned childStartOrdinal = m_blkMap[child->m_bbStart->bbNum];
                        unsigned childEndOrdinal   = m_blkMap[child->m_bbEnd->bbNum];
                        assert(childStartOrdinal <= childEndOrdinal);
                        assert(rgnStartOrdinal <= childStartOrdinal);

                        while (true)
                        {
                            Verify(child);

                            lastChild                      = child;
                            unsigned lastChildStartOrdinal = childStartOrdinal;
                            unsigned lastChildEndOrdinal   = childEndOrdinal;

                            child = child->m_rgnNext;
                            if (child == nullptr)
                            {
                                break;
                            }

                            childStartOrdinal = m_blkMap[child->m_bbStart->bbNum];
                            childEndOrdinal   = m_blkMap[child->m_bbEnd->bbNum];
                            assert(childStartOrdinal <= childEndOrdinal);

                            // The children can't overlap; they can't share any blocks.
                            assert(lastChildEndOrdinal < childStartOrdinal);
                        }

                        // The parent region must fully include the last child.
                        assert(childEndOrdinal <= rgnEndOrdinal);
                    }
                }

                //------------------------------------------------------------------------
                // Verify: verify the region graph satisfies proper nesting, and other legality rules.
                //
                void Verify()
                {
                    assert(m_comp != nullptr);
                    assert(m_blkMap != nullptr);
                    for (unsigned i = 0; i < m_blkMapSize; i++)
                    {
                        assert(m_blkMap[i] < m_blkMapSize);
                    }

                    // The root region has no siblings.
                    assert(m_rgnRoot != nullptr);
                    assert(m_rgnRoot->m_rgnNext == nullptr);
                    Verify(m_rgnRoot);
                }

#endif // DEBUG

                //------------------------------------------------------------------------
                // Output: output the region graph to the .dot file
                //
                // Arguments:
                //    file - the file to write output to.
                //
                void Output(FILE* file)
                {
                    unsigned clusterNum = 0;

                    // Output the regions; don't output the top (root) region that represents the whole function.
                    for (Region* child = m_rgnRoot->m_rgnChild; child != nullptr; child = child->m_rgnNext)
                    {
                        OutputRegion(file, clusterNum, child, 4);
                    }
                    fprintf(file, "\n");
                }

            private:
                //------------------------------------------------------------------------
                // GetColorForRegion: get a color name to use for a region
                //
                // Arguments:
                //    rgn - the region for which we need a color
                //
                static const char* GetColorForRegion(Region* rgn)
                {
                    RegionType rgnType = rgn->m_rgnType;
                    switch (rgnType)
                    {
                        case RegionType::EH:
                            return "red";
                        default:
                            return "black";
                    }
                }

                //------------------------------------------------------------------------
                // OutputRegion: helper function to output a region and its nested children
                // to the .dot file.
                //
                // Arguments:
                //    file       - the file to write output to.
                //    clusterNum - the number of this dot "cluster". This is updated as we
                //                 create new clusters.
                //    rgn        - the region to output.
                //    indent     - the current indent level, in characters.
                //
                void OutputRegion(FILE* file, unsigned& clusterNum, Region* rgn, unsigned indent)
                {
                    fprintf(file, "%*ssubgraph cluster_%u {\n", indent, "", clusterNum);
                    indent += 4;
                    fprintf(file, "%*slabel = \"%s\";\n", indent, "", rgn->m_rgnName);
                    fprintf(file, "%*scolor = %s;\n", indent, "", GetColorForRegion(rgn));
                    clusterNum++;

                    bool        needIndent = true;
                    BasicBlock* bbCur      = rgn->m_bbStart;
                    BasicBlock* bbEnd      = rgn->m_bbEnd->Next();
                    Region*     child      = rgn->m_rgnChild;
                    BasicBlock* childCurBB = (child == nullptr) ? nullptr : child->m_bbStart;

                    // Count the children and assert we output all of them.
                    unsigned totalChildren = 0;
                    unsigned childCount    = 0;
                    for (Region* tmpChild = child; tmpChild != nullptr; tmpChild = tmpChild->m_rgnNext)
                    {
                        totalChildren++;
                    }

                    while (bbCur != bbEnd)
                    {
                        // Output from bbCur to current child first block.
                        while ((bbCur != childCurBB) && (bbCur != bbEnd))
                        {
                            fprintf(file, "%*s" FMT_BB ";", needIndent ? indent : 0, "", bbCur->bbNum);
                            needIndent = false;
                            bbCur      = bbCur->Next();
                        }

                        if (bbCur == bbEnd)
                        {
                            // We're done at this level.
                            break;
                        }
                        else
                        {
                            assert(bbCur != nullptr); // Or else we should also have `bbCur == bbEnd`
                            assert(child != nullptr);

                            // If there is a child, output that child.
                            if (!needIndent)
                            {
                                // We've printed some basic blocks, so put the subgraph on a new line.
                                fprintf(file, "\n");
                            }
                            OutputRegion(file, clusterNum, child, indent);
                            needIndent = true;

                            childCount++;

                            bbCur      = child->m_bbEnd->Next(); // Next, output blocks after this child.
                            child      = child->m_rgnNext;       // Move to the next child, if any.
                            childCurBB = (child == nullptr) ? nullptr : child->m_bbStart;
                        }
                    }

                    // Put the end brace on its own line and leave the cursor at the beginning of the line for the
                    // parent.
                    indent -= 4;
                    fprintf(file, "\n%*s}\n", indent, "");

                    assert(childCount == totalChildren);
                }

                Compiler* m_comp;
                Region*   m_rgnRoot;
                unsigned* m_blkMap;
                unsigned  m_blkMapSize;
            };

            // Define the region graph object. We'll add regions to this, then output the graph.

            RegionGraph rgnGraph(this, blkMap, blkMapSize);

            // Add the EH regions to the region graph. An EH region consists of a region for the
            // `try`, a region for the handler, and, for filter/filter-handlers, a region for the
            // `filter` as well.

            if (includeEH)
            {
                char      name[30];
                unsigned  XTnum;
                EHblkDsc* ehDsc;
                for (XTnum = 0, ehDsc = compHndBBtab; XTnum < compHndBBtabCount; XTnum++, ehDsc++)
                {
                    sprintf_s(name, sizeof(name), "EH#%u try", XTnum);
                    rgnGraph.Insert(name, RegionGraph::RegionType::EH, ehDsc->ebdTryBeg, ehDsc->ebdTryLast);
                    const char* handlerType = "";
                    switch (ehDsc->ebdHandlerType)
                    {
                        case EH_HANDLER_CATCH:
                            handlerType = "catch";
                            break;
                        case EH_HANDLER_FILTER:
                            handlerType = "filter-hnd";
                            break;
                        case EH_HANDLER_FAULT:
                            handlerType = "fault";
                            break;
                        case EH_HANDLER_FINALLY:
                            handlerType = "finally";
                            break;
                        case EH_HANDLER_FAULT_WAS_FINALLY:
                            handlerType = "fault-was-finally";
                            break;
                    }
                    sprintf_s(name, sizeof(name), "EH#%u %s", XTnum, handlerType);
                    rgnGraph.Insert(name, RegionGraph::RegionType::EH, ehDsc->ebdHndBeg, ehDsc->ebdHndLast);
                    if (ehDsc->HasFilter())
                    {
                        sprintf_s(name, sizeof(name), "EH#%u filter", XTnum);
                        rgnGraph.Insert(name, RegionGraph::RegionType::EH, ehDsc->ebdFilter, ehDsc->ebdHndBeg->Prev());
                    }
                }
            }

            // All the regions have been added. Now, output them.
            DBEXEC(verbose, rgnGraph.Dump());
            INDEBUG(rgnGraph.Verify());
            rgnGraph.Output(fgxFile);
        }

        if (includeLoops && (m_loops != nullptr))
        {
            fgDumpFlowGraphLoops(fgxFile);
        }
    }

    if (createDotFile)
    {
        fprintf(fgxFile, "}\n");
    }
    else
    {
        fprintf(fgxFile, "\n    </edges>");
        fprintf(fgxFile, "\n</method>\n");
    }

    if (dontClose)
    {
        // fgxFile is jitstdout or stderr
        fprintf(fgxFile, "\n");
    }
    else
    {
        fclose(fgxFile);
    }

    return result;
}

//------------------------------------------------------------------------
// fgDumpFlowGraphLoops: Dump the current loops into the flow graph.
//
// Arguments:
//    file - File to dump loop subgraphs into
//
void Compiler::fgDumpFlowGraphLoops(FILE* file)
{
    class Dumper
    {
        FlowGraphNaturalLoops* m_loops;
        BitVecTraits           m_traits;
        BitVec                 m_outputBlocks;
        FILE*                  m_file;
        int                    m_indent    = 4;
        int                    m_loopIndex = 0;

    public:
        Dumper(FlowGraphNaturalLoops* loops, FILE* file)
            : m_loops(loops)
            , m_traits(loops->GetDfsTree()->PostOrderTraits())
            , m_outputBlocks(BitVecOps::MakeEmpty(&m_traits))
            , m_file(file)
        {
        }

        void Output(FlowGraphNaturalLoop* loop)
        {
            Compiler* comp = loop->GetDfsTree()->GetCompiler();
            fprintf(m_file, "%*ssubgraph cluster_%d {\n", m_indent, "", m_loopIndex++);
            m_indent += 4;

            fprintf(m_file, "%*slabel = \"" FMT_LP "\";\n", m_indent, "", loop->GetIndex());
            fprintf(m_file, "%*scolor = blue;\n", m_indent, "");
            fprintf(m_file, "%*s", m_indent, "");

            loop->VisitLoopBlocksReversePostOrder([=](BasicBlock* block) {
                if (BitVecOps::IsMember(&m_traits, m_outputBlocks, block->bbPostorderNum))
                {
                    return BasicBlockVisit::Continue;
                }

                if (block != loop->GetHeader())
                {
                    FlowGraphNaturalLoop* childLoop = m_loops->GetLoopByHeader(block);
                    if (childLoop != nullptr)
                    {
                        fprintf(m_file, "\n");
                        Output(childLoop);
                        fprintf(m_file, "\n%*s", m_indent, "");
                        return BasicBlockVisit::Continue;
                    }
                }

                fprintf(m_file, FMT_BB ";", block->bbNum);
                BitVecOps::AddElemD(&m_traits, m_outputBlocks, block->bbPostorderNum);

                return BasicBlockVisit::Continue;
            });

            m_indent -= 4;
            fprintf(m_file, "\n%*s}", m_indent, "");
        }
    };

    Dumper dumper(m_loops, file);

    for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
    {
        if (loop->GetParent() == nullptr)
        {
            dumper.Output(loop);
            fprintf(file, "\n");
        }
    }
}

#endif // DUMP_FLOWGRAPHS

/*****************************************************************************/
#ifdef DEBUG

void Compiler::fgTableDispBasicBlock(const BasicBlock* block,
                                     const BasicBlock* nextBlock /* = nullptr */,
                                     int               blockTargetFieldWidth /* = 21 */,
                                     int               ibcColWidth /* = 0 */)
{
    const unsigned __int64 flags            = block->GetFlagsRaw();
    unsigned               bbNumMax         = fgBBNumMax;
    int                    maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth                        = max(maxBlockNumWidth, 2);
    int blockNumWidth                       = CountDigits(block->bbNum);
    blockNumWidth                           = max(blockNumWidth, 2);
    int blockNumPadding                     = maxBlockNumWidth - blockNumWidth;

    // Instead of displaying a block number, should we instead display "*" when the specified block is
    // the next block?
    const bool terseNext = (JitConfig.JitDumpTerseNextBlock() != 0);

    printf("%s %2u", block->dspToString(blockNumPadding), block->bbRefs);

    //
    // Display EH 'try' region index
    //

    if (block->hasTryIndex())
    {
        printf(" %2u", block->getTryIndex());
    }
    else
    {
        printf("   ");
    }

    //
    // Display EH handler region index
    //

    if (block->hasHndIndex())
    {
        printf(" %2u", block->getHndIndex());
    }
    else
    {
        printf("   ");
    }

    printf(" ");

    //
    // Display block predecessor list
    //

    unsigned charCnt = block->dspPreds();

    if (charCnt < 19)
    {
        printf("%*s", 19 - charCnt, "");
    }

    printf(" ");

    //
    // Display block weight
    //

    if (block->isMaxBBWeight())
    {
        printf(" MAX  ");
    }
    else
    {
        weight_t weight = block->getBBWeight(this);

        if (weight > 99999) // Is it going to be more than 6 characters?
        {
            if (weight <= 99999 * BB_UNITY_WEIGHT)
            {
                // print weight in this format ddddd.
                printf("%5u.", (unsigned)FloatingPointUtils::round(weight / BB_UNITY_WEIGHT));
            }
            else // print weight in terms of k (i.e. 156k )
            {
                // print weight in this format dddddk
                weight_t weightK = weight / 1000;
                printf("%5uk", (unsigned)FloatingPointUtils::round(weightK / BB_UNITY_WEIGHT));
            }
        }
        else // print weight in this format ddd.dd
        {
            printf("%6s", refCntWtd2str(weight, /* padForDecimalPlaces */ true));
        }
    }

    //
    // Display optional IBC weight column.
    // Note that iColWidth includes one character for a leading space, if there is an IBC column.
    //

    if (ibcColWidth > 0)
    {
        if (block->hasProfileWeight())
        {
            printf("%*u", ibcColWidth, (unsigned)FloatingPointUtils::round(block->bbWeight));
        }
        else
        {
            // No IBC data. Just print spaces to align the column.
            printf("%*s", ibcColWidth, "");
        }
    }

    printf(" ");

    //
    // Display block IL range
    //

    block->dspBlockILRange();

    //
    // Display block branch target
    //

    int printedBlockWidth;

    // Call `dspBlockNum()` to get the block number to print, and update `printedBlockWidth` with the width
    // of the generated string. Note that any computation using `printedBlockWidth` must be done after all
    // calls to this function.
    auto dspBlockNum = [terseNext, nextBlock, &printedBlockWidth](const BasicBlock* b) -> const char* {
        static char buffers[3][64]; // static array of 3 to allow 3 concurrent calls in one printf()
        static int  nextBufferIndex = 0;

        auto& buffer    = buffers[nextBufferIndex];
        nextBufferIndex = (nextBufferIndex + 1) % ArrLen(buffers);

        if (b == nullptr)
        {
            _snprintf_s(buffer, ArrLen(buffer), ArrLen(buffer), "NULL");
            printedBlockWidth += 4;
        }
        else if (terseNext && (b == nextBlock))
        {
            _snprintf_s(buffer, ArrLen(buffer), ArrLen(buffer), "*");
            printedBlockWidth += 1;
        }
        else
        {
            _snprintf_s(buffer, ArrLen(buffer), ArrLen(buffer), FMT_BB, b->bbNum);
            printedBlockWidth += 2 /* BB */ + max(CountDigits(b->bbNum), 2);
        }

        return buffer;
    };

    if (flags & BBF_REMOVED)
    {
        printedBlockWidth = 10;
        printf("[removed] %*s", blockTargetFieldWidth - printedBlockWidth, "");
    }
    else
    {
        switch (block->GetKind())
        {
            case BBJ_COND:
                printedBlockWidth = 3 /* "-> " */ + 1 /* comma */ + 9 /* kind */;
                printf("-> %s,%s", dspBlockNum(block->GetTrueTargetRaw()), dspBlockNum(block->GetFalseTargetRaw()));
                printf("%*s ( cond )", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_CALLFINALLY:
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetTargetRaw()));
                printf("%*s (callf )", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_CALLFINALLYRET:
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetFinallyContinuation()));
                printf("%*s (callfr)", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_ALWAYS:
                const char* label;
                label             = (flags & BBF_KEEP_BBJ_ALWAYS) ? "ALWAYS" : "always";
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetTargetRaw()));
                printf("%*s (%s)", blockTargetFieldWidth - printedBlockWidth, "", label);
                break;

            case BBJ_LEAVE:
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetTargetRaw()));
                printf("%*s (leave )", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_EHFINALLYRET:
            {
                printf("->");
                printedBlockWidth = 2 + 9 /* kind */;

                const BBehfDesc* const ehfDesc = block->GetEhfTargets();
                if (ehfDesc == nullptr)
                {
                    printf(" ????");
                    printedBlockWidth += 5;
                }
                else
                {
                    // Very early in compilation, we won't have fixed up the BBJ_EHFINALLYRET successors yet.

                    const unsigned     jumpCnt = ehfDesc->bbeCount;
                    BasicBlock** const jumpTab = ehfDesc->bbeSuccs;

                    for (unsigned i = 0; i < jumpCnt; i++)
                    {
                        printedBlockWidth += 1 /* space/comma */;
                        printf("%c%s", (i == 0) ? ' ' : ',', dspBlockNum(jumpTab[i]));
                    }
                }

                if (printedBlockWidth < blockTargetFieldWidth)
                {
                    printf("%*s", blockTargetFieldWidth - printedBlockWidth, "");
                }

                printf(" (finret)");
                break;
            }

            case BBJ_EHFAULTRET:
                printedBlockWidth = 9 /* kind */;
                printf("%*s (falret)", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_EHFILTERRET:
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetTargetRaw()));
                printf("%*s (fltret)", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_EHCATCHRET:
                printedBlockWidth = 3 /* "-> " */ + 9 /* kind */;
                printf("-> %s", dspBlockNum(block->GetTargetRaw()));
                printf("%*s ( cret )", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_THROW:
                printedBlockWidth = 9 /* kind */;
                printf("%*s (throw )", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_RETURN:
                printedBlockWidth = 9 /* kind */;
                printf("%*s (return)", blockTargetFieldWidth - printedBlockWidth, "");
                break;

            case BBJ_SWITCH:
            {
                printf("->");
                printedBlockWidth = 2 + 9 /* kind */;

                const BBswtDesc* const jumpSwt = block->GetSwitchTargets();
                const unsigned         jumpCnt = jumpSwt->bbsCount;
                BasicBlock** const     jumpTab = jumpSwt->bbsDstTab;

                for (unsigned i = 0; i < jumpCnt; i++)
                {
                    printedBlockWidth += 1 /* space/comma */;
                    printf("%c%s", (i == 0) ? ' ' : ',', dspBlockNum(jumpTab[i]));

                    const bool isDefault = jumpSwt->bbsHasDefault && (i == jumpCnt - 1);
                    if (isDefault)
                    {
                        printf("[def]");
                        printedBlockWidth += 5;
                    }

                    const bool isDominant = jumpSwt->bbsHasDominantCase && (i == jumpSwt->bbsDominantCase);
                    if (isDominant)
                    {
                        printf("[dom(" FMT_WT ")]", jumpSwt->bbsDominantFraction);
                        printedBlockWidth += 10;
                    }
                }

                if (printedBlockWidth < blockTargetFieldWidth)
                {
                    printf("%*s", blockTargetFieldWidth - printedBlockWidth, "");
                }

                printf(" (switch)");
            }
            break;

            default:
                // Bad Kind
                printedBlockWidth = 9 /* kind */;
                printf("%*s (ERROR )", blockTargetFieldWidth - printedBlockWidth, "");
                break;
        }
    }

    printf(" ");

    //
    // Display block EH region and type, including nesting indicator
    //

    if (block->hasTryIndex())
    {
        printf("T%d ", block->getTryIndex());
    }
    else
    {
        printf("   ");
    }

    if (block->hasHndIndex())
    {
        printf("H%d ", block->getHndIndex());
    }
    else
    {
        printf("   ");
    }

    if (flags & BBF_FUNCLET_BEG)
    {
        printf("F ");
    }
    else
    {
        printf("  ");
    }

    int cnt = 0;

    switch (block->bbCatchTyp)
    {
        case BBCT_NONE:
            break;
        case BBCT_FAULT:
            printf("fault ");
            cnt += 6;
            break;
        case BBCT_FINALLY:
            printf("finally ");
            cnt += 8;
            break;
        case BBCT_FILTER:
            printf("filter ");
            cnt += 7;
            break;
        case BBCT_FILTER_HANDLER:
            printf("filtHnd ");
            cnt += 8;
            break;
        default:
            printf("catch ");
            cnt += 6;
            break;
    }

    if (block->bbCatchTyp != BBCT_NONE)
    {
        cnt += 2;
        printf("{ ");
        /* brace matching editor workaround to compensate for the preceding line: } */
    }

    if (bbIsTryBeg(block))
    {
        // Output a brace for every try region that this block opens

        for (EHblkDsc* const HBtab : EHClauses(this))
        {
            if (HBtab->ebdTryBeg == block)
            {
                cnt += 6;
                printf("try { ");
                /* brace matching editor workaround to compensate for the preceding line: } */
            }
        }
    }

    for (EHblkDsc* const HBtab : EHClauses(this))
    {
        if (HBtab->ebdTryLast == block)
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
        if (HBtab->ebdHndLast == block)
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
        if (HBtab->HasFilter() && block->NextIs(HBtab->ebdHndBeg))
        {
            cnt += 2;
            /* brace matching editor workaround to compensate for the following line: { */
            printf("} ");
        }
    }

    while (cnt < 12)
    {
        cnt++;
        printf(" ");
    }

    //
    // Display block flags
    //

    block->dspFlags();

    // Display OSR info
    //
    if (opts.IsOSR())
    {
        if (block == fgEntryBB)
        {
            printf(" original-entry");
        }
        if (block == fgOSREntryBB)
        {
            printf(" osr-entry");
        }
    }

    // Indicate if it's the merged return block.
    if (block == genReturnBB)
    {
        printf(" merged-return");
    }

    printf("\n");
}

/****************************************************************************
    Dump blocks from firstBlock to lastBlock.
*/

void Compiler::fgDispBasicBlocks(BasicBlock* firstBlock, BasicBlock* lastBlock, bool dumpTrees)
{
    // Build vector of blocks in order.
    //
    if (fgBBOrder == nullptr)
    {
        CompAllocator allocator = getAllocator(CMK_DebugOnly);
        fgBBOrder               = new (allocator) jitstd::vector<BasicBlock*>(allocator);
    }

    fgBBOrder->reserve(fgBBcount);
    fgBBOrder->clear();

    int ibcColWidth = 0;

    for (BasicBlock* block = firstBlock; block != nullptr; block = block->Next())
    {
        if (block->hasProfileWeight())
        {
            int thisIbcWidth = CountDigits(block->bbWeight);
            ibcColWidth      = max(ibcColWidth, thisIbcWidth);
        }

        fgBBOrder->push_back(block);

        if (block == lastBlock)
        {
            break;
        }
    }

    if (ibcColWidth > 0)
    {
        ibcColWidth = max(ibcColWidth, 3) + 1; // + 1 for the leading space
    }

    bool inDefaultOrder = true;

    struct fgBBNumCmp
    {
        bool operator()(const BasicBlock* bb1, const BasicBlock* bb2)
        {
            return bb1->bbNum < bb2->bbNum;
        }
    };

    struct fgBBIDCmp
    {
        bool operator()(const BasicBlock* bb1, const BasicBlock* bb2)
        {
            return bb1->bbID < bb2->bbID;
        }
    };

    // Optionally sort
    //
    if (JitConfig.JitDumpFgBlockOrder() == 1)
    {
        jitstd::sort(fgBBOrder->begin(), fgBBOrder->end(), fgBBNumCmp());
        inDefaultOrder = false;
    }
    else if (JitConfig.JitDumpFgBlockOrder() == 2)
    {
        jitstd::sort(fgBBOrder->begin(), fgBBOrder->end(), fgBBIDCmp());
        inDefaultOrder = false;
    }

    unsigned bbNumMax         = fgBBNumMax;
    int      maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth          = max(maxBlockNumWidth, 2);
    int padWidth              = maxBlockNumWidth - 2; // Account for functions with a large number of blocks.

    // Calculate the field width allocated for the block target. The field width is allocated to allow for two blocks
    // for BBJ_COND. It does not include any extra space for variable-sized BBJ_EHFINALLYRET and BBJ_SWITCH.
    int blockTargetFieldWidth = 3 /* "-> " */ + 2 /* BB */ + maxBlockNumWidth + 1 /* comma */ + 2 /* BB */ +
                                maxBlockNumWidth + 1 /* space */ + 8 /* kind: "(xxxxxx)" */;

    // clang-format off

    printf("\n");
    printf("------%*s-------------------------------------%*s--------------------------%*s--------------------------\n",
        padWidth, "------------", //
        ibcColWidth, "------------", //
        blockTargetFieldWidth, "-----------------------"); //
    printf("BBnum %*sBBid ref try hnd %s     weight  %*s%s [IL range]   [jump]%*s [EH region]        [flags]\n",
        padWidth, "",
        (fgPredsComputed        ? "preds      "
                                : "           "),
        ((ibcColWidth > 0) ? ibcColWidth - 3 : 0), "",  // Subtract 3 for the width of "IBC", printed next.
        ((ibcColWidth > 0)      ? "IBC"
                                : ""),
        blockTargetFieldWidth - 8 /* "   [jump]" */, ""
        );
    printf("------%*s-------------------------------------%*s--------------------------%*s--------------------------\n",
        padWidth, "------------", //
        ibcColWidth, "------------", //
        blockTargetFieldWidth, "-----------------------"); //

    // clang-format on

    for (auto block_iter = fgBBOrder->begin(), block_iter_end = fgBBOrder->end(); block_iter != block_iter_end;
         ++block_iter)
    {
        BasicBlock* block           = *block_iter;
        BasicBlock* nextBlock       = nullptr;
        auto        block_iter_next = block_iter + 1;
        if (block_iter_next != block_iter_end)
        {
            nextBlock = *block_iter_next;
        }

        // First, do some checking on the bbPrev links
        if (!block->IsFirst())
        {
            if (!block->Prev()->NextIs(block))
            {
                printf("bad prev link\n");
            }
        }
        else if (block != fgFirstBB)
        {
            printf("bad prev link!\n");
        }

        if (inDefaultOrder && block->IsFirstColdBlock(this))
        {
            printf("~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~"
                   "~~~~~~~~~~~~~~~~\n",
                   padWidth, "~~~~~~~~~~~~",                          //
                   ibcColWidth, "~~~~~~~~~~~~",                       //
                   blockTargetFieldWidth, "~~~~~~~~~~~~~~~~~~~~~~~"); //
        }

#if defined(FEATURE_EH_FUNCLETS)
        if (inDefaultOrder && (block == fgFirstFuncletBB))
        {
            printf("++++++%*s+++++++++++++++++++++++++++++++++++++%*s++++++++++++++++++++++++++%*s++++++++++"
                   "++++++++++++++++ funclets follow\n",
                   padWidth, "++++++++++++",                          //
                   ibcColWidth, "++++++++++++",                       //
                   blockTargetFieldWidth, "+++++++++++++++++++++++"); //
        }
#endif // FEATURE_EH_FUNCLETS

        fgTableDispBasicBlock(block, nextBlock, blockTargetFieldWidth, ibcColWidth);

        if (block == lastBlock)
        {
            break;
        }
    }

    printf("------%*s-------------------------------------%*s--------------------------%*s------------------"
           "--------\n",
           padWidth, "------------",                          //
           ibcColWidth, "------------",                       //
           blockTargetFieldWidth, "-----------------------"); //

    if (dumpTrees)
    {
        for (BasicBlock* block : *fgBBOrder)
        {
            fgDumpBlock(block);
        }
        printf("\n-----------------------------------------------------------------------------------------------------"
               "--------------\n");
    }
}

/*****************************************************************************/

void Compiler::fgDispBasicBlocks(bool dumpTrees)
{
    fgDispBasicBlocks(fgFirstBB, nullptr, dumpTrees);
}

//------------------------------------------------------------------------
// fgDumpStmtTree: dump the statement and the basic block number.
//
// Arguments:
//    block - the basic block that contains the statement to dump.
//    stmt  - the statement to dump.
//
void Compiler::fgDumpStmtTree(const BasicBlock* block, Statement* stmt)
{
    printf("\n***** %s\n", block->dspToString());
    gtDispStmt(stmt);
}

//------------------------------------------------------------------------
// Compiler::fgDumpBlock: dumps the contents of the given block to stdout.
//
// Arguments:
//    block - The block to dump.
//
void Compiler::fgDumpBlock(BasicBlock* block)
{
    printf("\n------------ ");
    block->dspBlockHeader(this);

    if (fgSsaValid)
    {
        fgDumpBlockMemorySsaIn(block);
    }

    if (!block->IsLIR())
    {
        for (Statement* const stmt : block->Statements())
        {
            fgDumpStmtTree(block, stmt);
        }
    }
    else
    {
        gtDispRange(LIR::AsRange(block));
    }

    if (fgSsaValid)
    {
        printf("\n");
        fgDumpBlockMemorySsaOut(block);
    }
}

//------------------------------------------------------------------------
// fgDumpTrees: dumps the trees for every block in a range of blocks.
//
// Arguments:
//    firstBlock - The first block to dump.
//    lastBlock  - The last block to dump.
//
void Compiler::fgDumpTrees(BasicBlock* firstBlock, BasicBlock* lastBlock)
{
    // Note that typically we have already called fgDispBasicBlocks()
    // so we don't need to print the preds and succs again here.
    for (BasicBlock* block = firstBlock; block != nullptr; block = block->Next())
    {
        fgDumpBlock(block);

        if (block == lastBlock)
        {
            break;
        }
    }
    printf("\n---------------------------------------------------------------------------------------------------------"
           "----------\n");
}

//------------------------------------------------------------------------
// fgDumpBlockMemorySsaIn: Dump memory state SSAs incoming to a block.
//
// Arguments:
//    block - The block
//
void Compiler::fgDumpBlockMemorySsaIn(BasicBlock* block)
{
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if (byrefStatesMatchGcHeapStates)
        {
            printf("SSA MEM: %s, %s", memoryKindNames[ByrefExposed], memoryKindNames[GcHeap]);
        }
        else
        {
            printf("SSA MEM: %s", memoryKindNames[memoryKind]);
        }

        if (block->bbMemorySsaPhiFunc[memoryKind] == nullptr)
        {
            printf(" = m:%u\n", block->bbMemorySsaNumIn[memoryKind]);
        }
        else if (block->bbMemorySsaPhiFunc[memoryKind] == BasicBlock::EmptyMemoryPhiDef)
        {
            printf(" = phi([not filled])\n");
        }
        else
        {
            printf(" = phi(");
            BasicBlock::MemoryPhiArg* phiArgs = block->bbMemorySsaPhiFunc[memoryKind];
            const char*               sep     = "";
            for (BasicBlock::MemoryPhiArg* arg = block->bbMemorySsaPhiFunc[memoryKind]; arg != nullptr;
                 arg                           = arg->m_nextArg)
            {
                printf("%sm:%u", sep, arg->GetSsaNum());
                sep = ", ";
            }
            printf(")\n");
        }

        if (byrefStatesMatchGcHeapStates)
        {
            break;
        }
    }
}

//------------------------------------------------------------------------
// fgDumpBlockMemorySsaOut: Dump memory state SSAs outgoing from a block.
//
// Arguments:
//    block - The block
//
void Compiler::fgDumpBlockMemorySsaOut(BasicBlock* block)
{
    for (MemoryKind memoryKind : allMemoryKinds())
    {
        if (byrefStatesMatchGcHeapStates)
        {
            printf("SSA MEM: %s, %s", memoryKindNames[ByrefExposed], memoryKindNames[GcHeap]);
        }
        else
        {
            printf("SSA MEM: %s", memoryKindNames[memoryKind]);
        }

        printf(" = m:%u\n", block->bbMemorySsaNumOut[memoryKind]);

        if (byrefStatesMatchGcHeapStates)
        {
            break;
        }
    }
}

/*****************************************************************************
 * Try to create as many candidates for GTF_MUL_64RSLT as possible.
 * We convert 'intOp1*intOp2' into 'int(long(nop(intOp1))*long(intOp2))'.
 */

/* static */
Compiler::fgWalkResult Compiler::fgStress64RsltMulCB(GenTree** pTree, fgWalkData* data)
{
    GenTree*  tree  = *pTree;
    Compiler* pComp = data->compiler;

    if (tree->gtOper != GT_MUL || tree->gtType != TYP_INT || (tree->gtOverflow()))
    {
        return WALK_CONTINUE;
    }

    JITDUMP("STRESS_64RSLT_MUL before:\n")
    DISPTREE(tree)

    tree->AsOp()->gtOp1 = pComp->gtNewCastNode(TYP_LONG, tree->gtGetOp1(), false, TYP_LONG);
    tree->AsOp()->gtOp2 = pComp->gtNewCastNode(TYP_LONG, tree->gtGetOp2(), false, TYP_LONG);
    tree->gtType        = TYP_LONG;
    *pTree              = pComp->gtNewCastNode(TYP_INT, tree, false, TYP_INT);

    // To ensure optNarrowTree() doesn't fold back to the original tree.
    tree->gtGetOp1()->gtDebugFlags |= GTF_DEBUG_CAST_DONT_FOLD;
    tree->gtGetOp2()->gtDebugFlags |= GTF_DEBUG_CAST_DONT_FOLD;

    JITDUMP("STRESS_64RSLT_MUL after:\n")
    DISPTREE(*pTree)

    return WALK_SKIP_SUBTREES;
}

void Compiler::fgStress64RsltMul()
{
    if (!compStressCompile(STRESS_64RSLT_MUL, 20))
    {
        return;
    }

    fgWalkAllTreesPre(fgStress64RsltMulCB, (void*)this);
}

// BBPredsChecker checks jumps from the block's predecessors to the block.
class BBPredsChecker
{
public:
    BBPredsChecker(Compiler* compiler) : comp(compiler)
    {
    }

    unsigned CheckBBPreds(BasicBlock* block, unsigned curTraversalStamp);

private:
    bool CheckEhTryDsc(BasicBlock* block, BasicBlock* blockPred, EHblkDsc* ehTryDsc);
    bool CheckEhHndDsc(BasicBlock* block, BasicBlock* blockPred, EHblkDsc* ehHndlDsc);
    bool CheckJump(BasicBlock* blockPred, BasicBlock* block);
    bool CheckEHFinallyRet(BasicBlock* blockPred, BasicBlock* block);

private:
    Compiler* comp;
};

//------------------------------------------------------------------------
// CheckBBPreds: Check basic block predecessors list.
//
// Notes:
//   This DEBUG routine checks that all predecessors have the correct traversal stamp
//   and have correct jumps to the block.
//   It calculates the number of incoming edges from the internal block,
//   i.e. it does not count the global incoming edge for the first block.
//
// Arguments:
//   block - the block to process;
//   curTraversalStamp - current traversal stamp to distinguish different iterations.
//
// Return value:
//   the number of incoming edges for the block.
unsigned BBPredsChecker::CheckBBPreds(BasicBlock* block, unsigned curTraversalStamp)
{
    if (!comp->fgPredsComputed)
    {
        assert(block->bbPreds == nullptr);
        return 0;
    }

    unsigned blockRefs = 0;
    for (FlowEdge* const pred : block->PredEdges())
    {
        blockRefs += pred->getDupCount();

        BasicBlock* blockPred = pred->getSourceBlock();

        // Make sure this pred is part of the BB list.
        assert(blockPred->bbTraversalStamp == curTraversalStamp);

        EHblkDsc* ehTryDsc = comp->ehGetBlockTryDsc(block);
        if (ehTryDsc != nullptr)
        {
            assert(CheckEhTryDsc(block, blockPred, ehTryDsc));
        }

        EHblkDsc* ehHndDsc = comp->ehGetBlockHndDsc(block);
        if (ehHndDsc != nullptr)
        {
            assert(CheckEhHndDsc(block, blockPred, ehHndDsc));
        }

        assert(CheckJump(blockPred, block));
    }

    // Make sure preds are in increasing BBnum order
    //
    assert(block->checkPredListOrder());

    return blockRefs;
}

bool BBPredsChecker::CheckEhTryDsc(BasicBlock* block, BasicBlock* blockPred, EHblkDsc* ehTryDsc)
{
    // You can jump to the start of a try
    if (ehTryDsc->ebdTryBeg == block)
    {
        return true;
    }

    // You can jump within the same try region
    if (comp->bbInTryRegions(block->getTryIndex(), blockPred))
    {
        return true;
    }

    // The catch block can jump back into the middle of the try
    if (comp->bbInCatchHandlerRegions(block, blockPred))
    {
        return true;
    }

    // The end of a finally region is a BBJ_EHFINALLYRET block (during importing, BBJ_LEAVE) which
    // is marked as "returning" to the BBJ_CALLFINALLYRET block following the BBJ_CALLFINALLY
    // block that does a local call to the finally. This BBJ_CALLFINALLYRET is within
    // the try region protected by the finally (for x86), but that's ok.
    BasicBlock* prevBlock = block->Prev();
    if (prevBlock->KindIs(BBJ_CALLFINALLY) && block->KindIs(BBJ_CALLFINALLYRET) && blockPred->KindIs(BBJ_EHFINALLYRET))
    {
        return true;
    }

    // If this is an OSR method and we haven't run post-importation cleanup, we may see a branch
    // from fgFirstBB to the middle of a try. Those get fixed during cleanup. Tolerate.
    //
    if (comp->opts.IsOSR() && !comp->compPostImportationCleanupDone && (blockPred == comp->fgFirstBB))
    {
        return true;
    }

    printf("Jump into the middle of try region: " FMT_BB " branches to " FMT_BB "\n", blockPred->bbNum, block->bbNum);
    assert(!"Jump into middle of try region");
    return false;
}

bool BBPredsChecker::CheckEhHndDsc(BasicBlock* block, BasicBlock* blockPred, EHblkDsc* ehHndlDsc)
{
    // You can do a BBJ_EHFINALLYRET or BBJ_EHFILTERRET into a handler region
    if (blockPred->KindIs(BBJ_EHFINALLYRET, BBJ_EHFILTERRET))
    {
        return true;
    }

    // Our try block can call our finally block
    if ((block->bbCatchTyp == BBCT_FINALLY) && blockPred->KindIs(BBJ_CALLFINALLY) &&
        comp->ehCallFinallyInCorrectRegion(blockPred, block->getHndIndex()))
    {
        return true;
    }

    // You can jump within the same handler region
    if (comp->bbInHandlerRegions(block->getHndIndex(), blockPred))
    {
        return true;
    }

    // A filter can jump to the start of the filter handler
    if (ehHndlDsc->HasFilter())
    {
        return true;
    }

    printf("Jump into the middle of handler region: " FMT_BB " branches to " FMT_BB "\n", blockPred->bbNum,
           block->bbNum);
    assert(!"Jump into the middle of handler region");
    return false;
}

bool BBPredsChecker::CheckJump(BasicBlock* blockPred, BasicBlock* block)
{
    switch (blockPred->GetKind())
    {
        case BBJ_COND:
            assert(blockPred->FalseTargetIs(block) || blockPred->TrueTargetIs(block));
            return true;

        case BBJ_ALWAYS:
        case BBJ_CALLFINALLY:
        case BBJ_CALLFINALLYRET:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
            assert(blockPred->TargetIs(block));
            return true;

        case BBJ_EHFINALLYRET:
            assert(CheckEHFinallyRet(blockPred, block));
            return true;

        case BBJ_EHFAULTRET:
        case BBJ_THROW:
        case BBJ_RETURN:
            assert(!"EHFAULTRET, THROW, and RETURN block cannot be in the predecessor list!");
            break;

        case BBJ_SWITCH:
            for (BasicBlock* const bTarget : blockPred->SwitchTargets())
            {
                if (block == bTarget)
                {
                    return true;
                }
            }
            assert(!"SWITCH in the predecessor list with no jump label to BLOCK!");
            break;

        case BBJ_LEAVE:
            // We may see BBJ_LEAVE preds if we haven't done cleanup yet.
            if (!comp->compPostImportationCleanupDone)
            {
                return true;
            }
            assert(!"Unexpected BBJ_LEAVE predecessor");
            break;

        default:
            assert(!"Unexpected bbKind");
            break;
    }
    return false;
}

bool BBPredsChecker::CheckEHFinallyRet(BasicBlock* blockPred, BasicBlock* block)
{
    // If the current block is a successor to a BBJ_EHFINALLYRET (return from finally),
    // then the lexically previous block should be a call to the same finally.
    // Also, `block` should be in the explicit successors list of `blockPred`.
    // Verify all of that.

    bool found = false;
    for (BasicBlock* const succ : blockPred->EHFinallyRetSuccs())
    {
        if (block == succ)
        {
            assert(!found); // we should only find it once
            found = true;
        }
    }
    assert(found && "BBJ_EHFINALLYRET successor not found");

    unsigned    hndIndex = blockPred->getHndIndex();
    EHblkDsc*   ehDsc    = comp->ehGetDsc(hndIndex);
    BasicBlock* finBeg   = ehDsc->ebdHndBeg;

    BasicBlock* firstBlock;
    BasicBlock* lastBlock;
    comp->ehGetCallFinallyBlockRange(hndIndex, &firstBlock, &lastBlock);

    found = false;
    for (BasicBlock* const bcall : comp->Blocks(firstBlock, lastBlock))
    {
        if (bcall->KindIs(BBJ_CALLFINALLY) && bcall->TargetIs(finBeg) && bcall->NextIs(block))
        {
            found = true;
            break;
        }
    }

#if defined(FEATURE_EH_FUNCLETS)

    if (!found && comp->fgFuncletsCreated)
    {
        // There is no easy way to search just the funclets that were pulled out of
        // the corresponding try body, so instead we search all the funclets, and if
        // we find a potential 'hit' we check if the funclet we're looking at is
        // from the correct try region.

        for (BasicBlock* const bcall : comp->Blocks(comp->fgFirstFuncletBB))
        {
            if (bcall->KindIs(BBJ_CALLFINALLY) && bcall->TargetIs(finBeg) && bcall->NextIs(block) &&
                comp->ehCallFinallyInCorrectRegion(bcall, hndIndex))
            {
                found = true;
                break;
            }
        }
    }

#endif // FEATURE_EH_FUNCLETS

    assert(found && "BBJ_EHFINALLYRET predecessor of block that doesn't follow a BBJ_CALLFINALLY!");
    return found;
}

//------------------------------------------------------------------------------
// fgDebugCheckBBNumIncreasing: Check that the block list bbNum are in increasing order in the bbNext
// traversal. Given a block B1 and its bbNext successor B2, this means `B1->bbNum < B2->bbNum`, but not
// that `B1->bbNum + 1 == B2->bbNum` (which is true after renumbering). This can be used as a precondition
// to a phase that expects this ordering to compare block numbers (say, to look for backwards branches)
// and doesn't want to call fgRenumberBlocks(), to avoid that potential expense.
//
void Compiler::fgDebugCheckBBNumIncreasing()
{
    for (BasicBlock* const block : Blocks())
    {
        assert(block->IsLast() || (block->bbNum < block->Next()->bbNum));
    }
}

// This variable is used to generate "traversal labels": one-time constants with which
// we label basic blocks that are members of the basic block list, in order to have a
// fast, high-probability test for membership in that list.  Type is "volatile" because
// it's incremented with an atomic operation, which wants a volatile type; "long" so that
// wrap-around to 0 (which I think has the highest probability of accidental collision) is
// postponed a *long* time.
static volatile int bbTraverseLabel = 1;

/*****************************************************************************
 *
 * A DEBUG routine to check the consistency of the flowgraph,
 * i.e. bbNum, bbRefs, bbPreds have to be up to date.
 *
 *****************************************************************************/

void Compiler::fgDebugCheckBBlist(bool checkBBNum /* = false */, bool checkBBRefs /* = true  */)
{
    if (verbose)
    {
        JITDUMP("*************** In fgDebugCheckBBlist\n");
    }

    // Don't bother checking a failed inlinee; we may have bailed
    // out in the middle of importation.
    //
    if (compIsForInlining() && compInlineResult->IsFailure())
    {
        JITDUMP("... failed inline attempt, no checking needed\n");
        return;
    }

    fgDebugCheckBlockLinks();
    fgFirstBBisScratch();

    if (fgBBcount > 10000 && expensiveDebugCheckLevel < 1)
    {
        // The basic block checks are too expensive if there are too many blocks,
        // so give up unless we've been told to try hard.
        return;
    }

#if defined(FEATURE_EH_FUNCLETS)
    bool reachedFirstFunclet = false;
    if (fgFuncletsCreated)
    {
        //
        // Make sure that fgFirstFuncletBB is accurate.
        // It should be the first basic block in a handler region.
        //
        if (fgFirstFuncletBB != nullptr)
        {
            assert(fgFirstFuncletBB->hasHndIndex() == true);
            assert(fgFirstFuncletBB->HasFlag(BBF_FUNCLET_BEG));
        }
    }
#endif // FEATURE_EH_FUNCLETS

    /* Check bbNum, bbRefs and bbPreds */
    // First, pick a traversal stamp, and label all the blocks with it.
    unsigned curTraversalStamp = unsigned(InterlockedIncrement((LONG*)&bbTraverseLabel));
    for (BasicBlock* const block : Blocks())
    {
        block->bbTraversalStamp = curTraversalStamp;
    }

    bool allNodesLinked = (fgNodeThreading == NodeThreading::AllTrees) || (fgNodeThreading == NodeThreading::LIR);

    unsigned numBlocks = 0;
    unsigned maxBBNum  = 0;

    for (BasicBlock* const block : Blocks())
    {
        numBlocks++;

        if (checkBBNum)
        {
            // Check that bbNum is sequential
            assert(block->IsLast() || (block->bbNum + 1 == block->Next()->bbNum));
        }

        maxBBNum = max(maxBBNum, block->bbNum);

        // BBJ_COND's normal (false) jump target is expected to be the next block
        // TODO-NoFallThrough: Allow bbFalseTarget to diverge from bbNext
        if (block->KindIs(BBJ_COND))
        {
            assert(block->NextIs(block->GetFalseTarget()));
        }

        // Check that all the successors have the current traversal stamp. Use the 'Compiler*' version of the
        // iterator, but not for BBJ_SWITCH: we don't want to end up calling GetDescriptorForSwitch(), which will
        // dynamically create the unique switch list.
        if (block->KindIs(BBJ_SWITCH))
        {
            for (BasicBlock* const succBlock : block->Succs())
            {
                assert(succBlock->bbTraversalStamp == curTraversalStamp);
            }

            // Also check the unique successor set, if it exists. Make sure to NOT allocate it if it doesn't exist!
            BlockToSwitchDescMap* switchMap = GetSwitchDescMap(/* createIfNull */ false);
            if (switchMap != nullptr)
            {
                SwitchUniqueSuccSet sd;
                if (switchMap->Lookup(block, &sd))
                {
                    for (unsigned i = 0; i < sd.numDistinctSuccs; i++)
                    {
                        const BasicBlock* const nonDuplicateSucc = sd.nonDuplicates[i];
                        assert(nonDuplicateSucc != nullptr);
                        assert(nonDuplicateSucc->bbTraversalStamp == curTraversalStamp);
                    }
                }
            }
        }
        else
        {
            for (BasicBlock* const succBlock : block->Succs(this))
            {
                assert(succBlock->bbTraversalStamp == curTraversalStamp);
            }
        }

        // If the block is a BBJ_COND, a BBJ_SWITCH or a
        // lowered GT_SWITCH_TABLE node then make sure it
        // ends with a conditional jump or a GT_SWITCH
        //
        // This may not be true for unimported blocks, if
        // we haven't run post-importation cleanup yet.
        //
        if (compPostImportationCleanupDone || block->HasFlag(BBF_IMPORTED))
        {
            if (block->KindIs(BBJ_COND))
            {
                assert((!allNodesLinked || (block->lastNode()->gtNext == nullptr)) &&
                       block->lastNode()->OperIsConditionalJump());
            }
            else if (block->KindIs(BBJ_SWITCH))
            {
                assert((!allNodesLinked || (block->lastNode()->gtNext == nullptr)) &&
                       (block->lastNode()->gtOper == GT_SWITCH || block->lastNode()->gtOper == GT_SWITCH_TABLE));
            }
        }

        if (block->bbCatchTyp == BBCT_FILTER)
        {
            // A filter has no predecessors
            assert(block->bbPreds == nullptr);
        }

#if defined(FEATURE_EH_FUNCLETS)
        if (fgFuncletsCreated)
        {
            //
            // There should be no handler blocks until
            // we get to the fgFirstFuncletBB block,
            // then every block should be a handler block
            //
            if (!reachedFirstFunclet)
            {
                if (block == fgFirstFuncletBB)
                {
                    assert(block->hasHndIndex() == true);
                    reachedFirstFunclet = true;
                }
                else
                {
                    assert(block->hasHndIndex() == false);
                }
            }
            else // reachedFirstFunclet
            {
                assert(block->hasHndIndex() == true);
            }
        }
#endif // FEATURE_EH_FUNCLETS

        if (checkBBRefs)
        {
            assert(fgPredsComputed);
        }

        BBPredsChecker checker(this);
        unsigned       blockRefs = checker.CheckBBPreds(block, curTraversalStamp);

        // First basic block has an additional global incoming edge.
        if (block == fgFirstBB)
        {
            blockRefs += 1;
        }

        // Under OSR, if we also are keeping the original method entry around
        // via artifical ref counts, account for those.
        //
        if (opts.IsOSR() && (block == fgEntryBB))
        {
            blockRefs += fgEntryBBExtraRefs;
        }

        /* Check the bbRefs */
        if (checkBBRefs)
        {
            if (block->bbRefs != blockRefs)
            {
                // Check to see if this block is the beginning of a filter or a handler and adjust the ref count
                // appropriately.
                for (EHblkDsc* const HBtab : EHClauses(this))
                {
                    if (HBtab->ebdHndBeg == block)
                    {
                        blockRefs++;
                    }
                    if (HBtab->HasFilter() && (HBtab->ebdFilter == block))
                    {
                        blockRefs++;
                    }
                }
            }

            assert(block->bbRefs == blockRefs);
        }

        /* Check that BBF_HAS_HANDLER is valid bbTryIndex */
        if (block->hasTryIndex())
        {
            assert(block->getTryIndex() < compHndBBtabCount);
        }

        // Blocks with these jump kinds must have non-null jump targets
        if (block->HasTarget())
        {
            assert(block->HasInitializedTarget());
        }

        // A branch or fall-through to a BBJ_CALLFINALLY block must come from the `try` region associated
        // with the finally block the BBJ_CALLFINALLY is targeting. There is one special case: if the
        // BBJ_CALLFINALLY is the first block of a `try`, then its predecessor can be outside the `try`:
        // either a branch or fall-through to the first block.
        //
        // Note that this IR condition is a choice. It naturally occurs when importing EH constructs.
        // This condition prevents flow optimizations from skipping blocks in a `try` and branching
        // directly to the BBJ_CALLFINALLY. Relaxing this constraint would require careful thinking about
        // the implications, such as data flow optimizations.
        //
        // Don't depend on predecessors list for the check.
        for (BasicBlock* const succBlock : block->Succs())
        {
            if (succBlock->KindIs(BBJ_CALLFINALLY))
            {
                BasicBlock* finallyBlock = succBlock->GetTarget();
                assert(finallyBlock->hasHndIndex());
                unsigned finallyIndex = finallyBlock->getHndIndex();

                // Now make sure the block branching to the BBJ_CALLFINALLY is in the correct region. The branch
                // to the BBJ_CALLFINALLY can come from the try region of the finally block, or from a more nested
                // try region, e.g.:
                //    try {
                //        try {
                //            LEAVE L_OUTER; // this becomes a branch to a BBJ_CALLFINALLY in an outer try region
                //                           // (in the FEATURE_EH_CALLFINALLY_THUNKS case)
                //        } catch {
                //        }
                //    } finally {
                //    }
                //    L_OUTER:
                //
                EHblkDsc* ehDsc = ehGetDsc(finallyIndex);
                if (ehDsc->ebdTryBeg == succBlock)
                {
                    // The BBJ_CALLFINALLY is the first block of it's `try` region. Don't check the predecessor.
                    // Note that this case won't occur in the FEATURE_EH_CALLFINALLY_THUNKS case, since the
                    // BBJ_CALLFINALLY in that case won't exist in the `try` region of the `finallyIndex`.
                }
                else
                {
                    assert(bbInTryRegions(finallyIndex, block));
                }
            }
        }

        /* Check if BBF_RUN_RARELY is set that we have bbWeight of zero */
        if (block->isRunRarely())
        {
            assert(block->bbWeight == BB_ZERO_WEIGHT);
        }
        else
        {
            assert(block->bbWeight > BB_ZERO_WEIGHT);
        }
    }

    assert(fgBBcount == numBlocks);
    assert(fgBBNumMax >= maxBBNum);

    // Make sure the one return BB is not changed.
    if (genReturnBB != nullptr)
    {
        assert(genReturnBB->GetFirstLIRNode() != nullptr || genReturnBB->bbStmtList != nullptr);
        assert(genReturnBB->KindIs(BBJ_RETURN));
    }

    // Ensure that all throw helper blocks are currently in the block list.
    for (Compiler::AddCodeDsc* add = fgAddCodeList; add != nullptr; add = add->acdNext)
    {
        if (add->acdUsed)
        {
            assert(add->acdDstBlk->bbTraversalStamp == curTraversalStamp);
        }
    }

    // If this is an inlinee, we're done checking.
    if (compIsForInlining())
    {
        return;
    }

    // The general encoder/decoder (currently) only reports "this" as a generics context as a stack location,
    // so we mark info.compThisArg as lvAddrTaken to ensure that it is not enregistered. Otherwise, it should
    // not be address-taken.  This variable determines if the address-taken-ness of "thisArg" is "OK".
    bool copiedForGenericsCtxt;
#ifndef JIT32_GCENCODER
    copiedForGenericsCtxt = ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0);
#else  // JIT32_GCENCODER
    copiedForGenericsCtxt        = false;
#endif // JIT32_GCENCODER

    // This if only in support of the noway_asserts it contains.
    if (info.compIsStatic)
    {
        // For static method, should have never grabbed the temp.
        assert(lvaArg0Var == BAD_VAR_NUM);
    }
    else
    {
        // For instance method:
        assert(info.compThisArg != BAD_VAR_NUM);
        bool compThisArgAddrExposedOK = !lvaTable[info.compThisArg].IsAddressExposed();

#ifndef JIT32_GCENCODER
        compThisArgAddrExposedOK = compThisArgAddrExposedOK || copiedForGenericsCtxt;
#endif // !JIT32_GCENCODER

        // Should never expose the address of arg 0 or write to arg 0.
        // In addition, lvArg0Var should remain 0 if arg0 is not
        // written to or address-exposed.
        assert(compThisArgAddrExposedOK && !lvaTable[info.compThisArg].lvHasILStoreOp &&
               (lvaArg0Var == info.compThisArg ||
                (lvaArg0Var != info.compThisArg && (lvaTable[lvaArg0Var].IsAddressExposed() ||
                                                    lvaTable[lvaArg0Var].lvHasILStoreOp || copiedForGenericsCtxt))));
    }
}

//------------------------------------------------------------------------
// fgDebugCheckTypes: Validate node types used in the given tree
//
// Arguments:
//    tree - the tree to (recursively) check types for
//
void Compiler::fgDebugCheckTypes(GenTree* tree)
{
    struct NodeTypeValidator : GenTreeVisitor<NodeTypeValidator>
    {
        enum
        {
            DoPostOrder = true,
        };

        NodeTypeValidator(Compiler* comp) : GenTreeVisitor(comp)
        {
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user) const
        {
            GenTree* node = *use;

            // Validate types of nodes in the IR:
            //
            // * TYP_ULONG and TYP_UINT are not legal.
            // * Small types are only legal for the following nodes:
            //    * All kinds of indirections including GT_NULLCHECK
            //    * All kinds of locals
            //    * GT_COMMA wrapped around any of the above.
            //
            if (node->TypeIs(TYP_ULONG, TYP_UINT))
            {
                m_compiler->gtDispTree(node);
                assert(!"TYP_ULONG and TYP_UINT are not legal in IR");
            }

            if (node->OperIs(GT_NOP))
            {
                assert(node->TypeIs(TYP_VOID) && "GT_NOP should be TYP_VOID.");
            }

            if (varTypeIsSmall(node))
            {
                if (node->OperIs(GT_COMMA))
                {
                    // TODO: it's only allowed if its underlying effective node is also a small type.
                    return WALK_CONTINUE;
                }

                if (node->OperIsIndir() || node->OperIs(GT_NULLCHECK) || node->IsPhiNode() || node->IsAnyLocal())
                {
                    return WALK_CONTINUE;
                }

                m_compiler->gtDispTree(node);
                assert(!"Unexpected small type in IR");
            }

            // TODO: validate types in GT_CAST nodes.
            // Validate mismatched types in binopt's arguments, etc.
            //
            return WALK_CONTINUE;
        }
    };

    NodeTypeValidator walker(this);
    walker.WalkTree(&tree, nullptr);
}

//------------------------------------------------------------------------
// fgDebugCheckFlags: Validate various invariants related to the propagation
//                    and setting of tree, block, and method flags
//
// Arguments:
//    tree - the tree to (recursively) check the flags for
//    block - basic block containing the tree
//
void Compiler::fgDebugCheckFlags(GenTree* tree, BasicBlock* block)
{
    GenTreeFlags actualFlags   = tree->gtFlags & GTF_ALL_EFFECT;
    GenTreeFlags expectedFlags = GTF_EMPTY;

    if (tree->OperMayThrow(this))
    {
        expectedFlags |= GTF_EXCEPT;
    }

    if (tree->OperRequiresAsgFlag())
    {
        expectedFlags |= GTF_ASG;
    }

    if (tree->OperRequiresCallFlag(this))
    {
        expectedFlags |= GTF_CALL;
    }

    if ((tree->gtFlags & GTF_REVERSE_OPS) != 0)
    {
        assert(tree->OperSupportsReverseOpEvalOrder(this));
    }

    GenTree* op1 = tree->OperIsSimple() ? tree->gtGetOp1() : nullptr;

    switch (tree->OperGet())
    {
        case GT_STORE_LCL_VAR:
        case GT_STORE_LCL_FLD:
            assert((tree->gtFlags & GTF_VAR_DEF) != 0);
            assert(((tree->gtFlags & GTF_VAR_USEASG) != 0) == tree->IsPartialLclFld(this));
            break;

        case GT_CATCH_ARG:
            expectedFlags |= GTF_ORDER_SIDEEFF;
            break;

        case GT_MEMORYBARRIER:
            expectedFlags |= (GTF_GLOB_REF | GTF_ASG);
            break;

        case GT_QMARK:
            assert(!op1->CanCSE());
            assert(op1->OperIsCompare() || op1->IsIntegralConst(0) || op1->IsIntegralConst(1));
            break;

        case GT_IND:
            // Do we have a constant integer address as op1 that is also a handle?
            if (op1->IsIconHandle())
            {
                if ((tree->gtFlags & GTF_IND_INVARIANT) != 0)
                {
                    actualFlags |= GTF_IND_INVARIANT;
                }
                if ((tree->gtFlags & GTF_IND_NONFAULTING) != 0)
                {
                    actualFlags |= GTF_IND_NONFAULTING;
                }

                GenTreeFlags handleKind = op1->GetIconHandleFlag();

                // Some of these aren't handles to invariant data...
                if ((handleKind == GTF_ICON_STATIC_HDL) || // Pointer to a mutable class Static variable
                    (handleKind == GTF_ICON_BBC_PTR) ||    // Pointer to a mutable basic block count value
                    (handleKind == GTF_ICON_FTN_ADDR) ||   // Pointer to a potentially mutable VM slot
                    (handleKind == GTF_ICON_GLOBAL_PTR))   // Pointer to mutable data from the VM state
                {
                    // For statics, we expect the GTF_GLOB_REF to be set. However, we currently
                    // fail to set it in a number of situations, and so this check is disabled.
                    // TODO: enable checking of GTF_GLOB_REF.
                    // expectedFlags |= GTF_GLOB_REF;
                }
                else // All the other handle indirections are considered invariant
                {
                    expectedFlags |= GTF_IND_INVARIANT;
                }

                // Currently we expect all indirections with constant addresses to be nonfaulting.
                expectedFlags |= GTF_IND_NONFAULTING;
            }

            assert(((tree->gtFlags & GTF_IND_TGT_NOT_HEAP) == 0) || ((tree->gtFlags & GTF_IND_TGT_HEAP) == 0));
            break;

        case GT_CALL:
        {
            GenTreeCall* const call = tree->AsCall();

            // Before global morph, if there are recursive tail calls, we should have
            // set the associated block and method flags.
            //
            if (!fgGlobalMorphDone && call->CanTailCall() && gtIsRecursiveCall(call))
            {
                assert(doesMethodHaveRecursiveTailcall());
                assert(block->HasFlag(BBF_RECURSIVE_TAILCALL));
            }
        }
        break;

        case GT_CMPXCHG:
            expectedFlags |= (GTF_GLOB_REF | GTF_ASG);
            break;

#if defined(FEATURE_HW_INTRINSICS)
        case GT_HWINTRINSIC:
        {
            GenTreeHWIntrinsic* hwintrinsic = tree->AsHWIntrinsic();
            NamedIntrinsic      intrinsicId = hwintrinsic->GetHWIntrinsicId();

            if (hwintrinsic->OperIsMemoryLoad())
            {
                assert(tree->OperMayThrow(this));
                expectedFlags |= GTF_GLOB_REF;
            }
            else if (hwintrinsic->OperIsMemoryStore())
            {
                assert(tree->OperRequiresAsgFlag());
                assert(tree->OperMayThrow(this));
                expectedFlags |= GTF_GLOB_REF;
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
                        assert(tree->OperRequiresAsgFlag());
                        expectedFlags |= GTF_GLOB_REF;
                        break;
                    }

                    case NI_X86Base_Pause:
                    case NI_SSE_Prefetch0:
                    case NI_SSE_Prefetch1:
                    case NI_SSE_Prefetch2:
                    case NI_SSE_PrefetchNonTemporal:
                    {
                        assert(tree->OperRequiresCallFlag(this));
                        expectedFlags |= GTF_GLOB_REF;
                        break;
                    }
#endif // TARGET_XARCH

#if defined(TARGET_ARM64)
                    case NI_ArmBase_Yield:
                    {
                        assert(tree->OperRequiresCallFlag(this));
                        expectedFlags |= GTF_GLOB_REF;
                        break;
                    }
#endif // TARGET_ARM64

                    default:
                    {
                        assert(!"Unhandled HWIntrinsic with special side effect");
                        break;
                    }
                }
            }

            break;
        }
#endif // FEATURE_HW_INTRINSICS

        default:
            break;
    }

    tree->VisitOperands([&](GenTree* operand) -> GenTree::VisitResult {
        fgDebugCheckFlags(operand, block);
        expectedFlags |= (operand->gtFlags & GTF_ALL_EFFECT);

        return GenTree::VisitResult::Continue;
    });

    fgDebugCheckFlagsHelper(tree, actualFlags, expectedFlags);
}

//------------------------------------------------------------------------------
// fgDebugCheckDispFlags: Wrapper function that displays GTF_IND_ flags
// and then calls gtDispFlags to display the rest.
//
// Arguments:
//    tree       - Tree whose flags are being checked
//    dispFlags  - the first argument for gtDispFlags (flags to display),
//                 including GTF_IND_INVARIANT, GTF_IND_NONFAULTING, GTF_IND_NONNULL
//    debugFlags - the second argument to gtDispFlags
//
void Compiler::fgDebugCheckDispFlags(GenTree* tree, GenTreeFlags dispFlags, GenTreeDebugFlags debugFlags)
{
    if (tree->OperGet() == GT_IND)
    {
        printf("%c", (dispFlags & GTF_IND_INVARIANT) ? '#' : '-');
        printf("%c", (dispFlags & GTF_IND_NONFAULTING) ? 'n' : '-');
        printf("%c", (dispFlags & GTF_IND_NONNULL) ? '@' : '-');
    }
    GenTree::gtDispFlags(dispFlags, debugFlags);
}

//------------------------------------------------------------------------------
// fgDebugCheckFlagsHelper : Check if all bits that are set in chkFlags are also set in treeFlags.
//
// Arguments:
//    tree          - Tree whose flags are being checked
//    actualFlags   - Actual flags on the tree
//    expectedFlags - Expected flags
//
void Compiler::fgDebugCheckFlagsHelper(GenTree* tree, GenTreeFlags actualFlags, GenTreeFlags expectedFlags)
{
    if (expectedFlags & ~actualFlags)
    {
        // Print the tree so we can see it in the log.
        printf("Missing flags on tree [%06d]: ", dspTreeID(tree));
        Compiler::fgDebugCheckDispFlags(tree, expectedFlags & ~actualFlags, GTF_DEBUG_NONE);
        printf("\n");
        gtDispTree(tree);

        noway_assert(!"Missing flags on tree");

        // Print the tree again so we can see it right after we hook up the debugger.
        printf("Missing flags on tree [%06d]: ", dspTreeID(tree));
        Compiler::fgDebugCheckDispFlags(tree, expectedFlags & ~actualFlags, GTF_DEBUG_NONE);
        printf("\n");
        gtDispTree(tree);
    }
    else if (actualFlags & ~expectedFlags)
    {
        // We can't/don't consider these flags (GTF_GLOB_REF or GTF_ORDER_SIDEEFF) as being "extra" flags
        //
        GenTreeFlags flagsToCheck = ~GTF_GLOB_REF & ~GTF_ORDER_SIDEEFF;

        if (tree->isIndir() && tree->AsIndir()->Addr()->IsIconHandle(GTF_ICON_FTN_ADDR))
        {
            // IND(ICON_FTN_ADDR) may or may not have GTF_IND_INVARIANT flag.
            flagsToCheck &= ~GTF_IND_INVARIANT;
        }

        if ((actualFlags & ~expectedFlags & flagsToCheck) != 0)
        {
            // Print the tree so we can see it in the log.
            printf("Extra flags on tree [%06d]: ", dspTreeID(tree));
            Compiler::fgDebugCheckDispFlags(tree, actualFlags & ~expectedFlags, GTF_DEBUG_NONE);
            printf("\n");
            gtDispTree(tree);

            noway_assert(!"Extra flags on tree");

            // Print the tree again so we can see it right after we hook up the debugger.
            printf("Extra flags on tree [%06d]: ", dspTreeID(tree));
            Compiler::fgDebugCheckDispFlags(tree, actualFlags & ~expectedFlags, GTF_DEBUG_NONE);
            printf("\n");
            gtDispTree(tree);
        }
    }
}

// DEBUG routine to check correctness of the internal gtNext, gtPrev threading of a statement.
// This threading is only valid when fgStmtListThreaded is true.
// This calls an alternate method for FGOrderLinear.
void Compiler::fgDebugCheckNodeLinks(BasicBlock* block, Statement* stmt)
{
    // LIR blocks are checked using BasicBlock::CheckLIR().
    if (block->IsLIR())
    {
        LIR::AsRange(block).CheckLIR(this);
        // TODO: return?
    }

    assert(fgNodeThreading != NodeThreading::None);

    noway_assert(stmt->GetTreeList());

    // The first node's gtPrev must be nullptr (the gtPrev list is not circular).
    // The last node's gtNext must be nullptr (the gtNext list is not circular). This is tested if the loop below
    // terminates.
    assert(stmt->GetTreeList()->gtPrev == nullptr);

    for (GenTree* tree = stmt->GetTreeList(); tree != nullptr; tree = tree->gtNext)
    {
        if (tree->gtPrev)
        {
            noway_assert(tree->gtPrev->gtNext == tree);
        }
        else
        {
            noway_assert(tree == stmt->GetTreeList());
        }

        if (tree->gtNext)
        {
            noway_assert(tree->gtNext->gtPrev == tree);
        }
        else
        {
            noway_assert(tree == stmt->GetRootNode());
        }

        /* Cross-check gtPrev,gtNext with GetOp() for simple trees */

        GenTree* expectedPrevTree = nullptr;

        if (tree->OperIsLeaf())
        {
            if (tree->gtOper == GT_CATCH_ARG)
            {
                // The GT_CATCH_ARG should always have GTF_ORDER_SIDEEFF set
                noway_assert(tree->gtFlags & GTF_ORDER_SIDEEFF);
                // The GT_CATCH_ARG has to be the first thing evaluated
                noway_assert(stmt == block->FirstNonPhiDef());
                noway_assert(stmt->GetTreeList()->gtOper == GT_CATCH_ARG);
                // The root of the tree should have GTF_ORDER_SIDEEFF set
                noway_assert(stmt->GetRootNode()->gtFlags & GTF_ORDER_SIDEEFF);
            }
        }

        if (tree->OperIsUnary() && tree->AsOp()->gtOp1)
        {
            expectedPrevTree = tree->AsOp()->gtOp1;
        }
        else if (tree->OperIsBinary() && tree->AsOp()->gtOp1)
        {
            switch (tree->gtOper)
            {
                case GT_QMARK:
                    // "then" operand of the GT_COLON (generated second).
                    expectedPrevTree = tree->AsOp()->gtOp2->AsColon()->ThenNode();
                    break;

                case GT_COLON:
                    expectedPrevTree = tree->AsColon()->ElseNode(); // "else" branch result (generated first).
                    break;

                default:
                    if (tree->AsOp()->gtOp2)
                    {
                        if (tree->gtFlags & GTF_REVERSE_OPS)
                        {
                            expectedPrevTree = tree->AsOp()->gtOp1;
                        }
                        else
                        {
                            expectedPrevTree = tree->AsOp()->gtOp2;
                        }
                    }
                    else
                    {
                        expectedPrevTree = tree->AsOp()->gtOp1;
                    }
                    break;
            }
        }

        noway_assert(expectedPrevTree == nullptr ||     // No expectations about the prev node
                     tree->gtPrev == expectedPrevTree); // The "normal" case
    }
}

//------------------------------------------------------------------------------
// fgDebugCheckLinkedLocals: Check the linked list of locals.
//
void Compiler::fgDebugCheckLinkedLocals()
{
    if (fgNodeThreading != NodeThreading::AllLocals)
    {
        return;
    }

    class DebugLocalSequencer : public GenTreeVisitor<DebugLocalSequencer>
    {
        ArrayStack<GenTree*> m_locals;

        bool ShouldLink(GenTree* node)
        {
            return node->OperIsAnyLocal();
        }

    public:
        enum
        {
            DoPostOrder       = true,
            UseExecutionOrder = true,
        };

        DebugLocalSequencer(Compiler* comp) : GenTreeVisitor(comp), m_locals(comp->getAllocator(CMK_DebugOnly))
        {
        }

        void Sequence(Statement* stmt)
        {
            m_locals.Reset();
            WalkTree(stmt->GetRootNodePointer(), nullptr);
        }

        ArrayStack<GenTree*>* GetSequence()
        {
            return &m_locals;
        }

        fgWalkResult PostOrderVisit(GenTree** use, GenTree* user)
        {
            GenTree* node = *use;
            if (ShouldLink(node))
            {
                if ((user != nullptr) && user->IsCall() &&
                    (node == m_compiler->gtCallGetDefinedRetBufLclAddr(user->AsCall())))
                {
                }
                else
                {
                    m_locals.Push(node);
                }
            }

            if (node->IsCall())
            {
                GenTree* defined = m_compiler->gtCallGetDefinedRetBufLclAddr(node->AsCall());
                if (defined != nullptr)
                {
                    assert(ShouldLink(defined));
                    m_locals.Push(defined);
                }
            }

            return WALK_CONTINUE;
        }
    };

    DebugLocalSequencer seq(this);
    for (BasicBlock* block : Blocks())
    {
        for (Statement* stmt : block->Statements())
        {
            GenTree* first = stmt->GetTreeList();
            CheckDoublyLinkedList<GenTree, &GenTree::gtPrev, &GenTree::gtNext>(first);

            seq.Sequence(stmt);

            ArrayStack<GenTree*>* expected = seq.GetSequence();

            bool success = true;

            if (expected->Height() > 0)
            {
                success &= (stmt->GetTreeList() == expected->Bottom(0)) && (stmt->GetTreeListEnd() == expected->Top(0));
            }
            else
            {
                success &= (stmt->GetTreeList() == nullptr) && (stmt->GetTreeListEnd() == nullptr);
            }

            int nodeIndex = 0;
            for (GenTree* cur = first; cur != nullptr; cur = cur->gtNext)
            {
                success &= cur->OperIsAnyLocal();
                success &= (nodeIndex < expected->Height()) && (cur == expected->Bottom(nodeIndex));
                nodeIndex++;
            }

            success &= nodeIndex == expected->Height();

            if (!success && verbose)
            {
                printf("Locals are improperly linked in the following statement:\n");
                DISPSTMT(stmt);

                printf("\nExpected:\n");
                const char* pref = "  ";
                for (int i = 0; i < expected->Height(); i++)
                {
                    printf("%s[%06u]", pref, dspTreeID(expected->Bottom(i)));
                    pref = " -> ";
                }

                printf("\n\nActual:\n");
                pref = "  ";
                for (GenTree* cur = first; cur != nullptr; cur = cur->gtNext)
                {
                    printf("%s[%06u]", pref, dspTreeID(cur));
                    pref = " -> ";
                }

                printf("\n");
            }

            assert(success && "Locals are improperly linked!");
        }
    }
}

/*****************************************************************************
 *
 * A DEBUG routine to check the correctness of the links between statements
 * and ordinary nodes within a statement.
 *
 ****************************************************************************/

void Compiler::fgDebugCheckLinks(bool morphTrees)
{
    // This used to be only on for stress, and there was a comment stating that
    // it was "quite an expensive operation" but I did not find that to be true.
    // Set DO_SANITY_DEBUG_CHECKS to false to revert to that behavior.
    const bool DO_SANITY_DEBUG_CHECKS = true;

    if (!DO_SANITY_DEBUG_CHECKS && !compStressCompile(STRESS_CHK_FLOW_UPDATE, 30))
    {
        return;
    }

    fgDebugCheckBlockLinks();

    // For each block check the links between the trees.
    for (BasicBlock* const block : Blocks())
    {
        if (block->IsLIR())
        {
            LIR::AsRange(block).CheckLIR(this);
        }
        else
        {
            fgDebugCheckStmtsList(block, morphTrees);
        }
    }

    fgDebugCheckNodesUniqueness();
    fgDebugCheckSsa();
}

//------------------------------------------------------------------------------
// fgDebugCheckStmtsList : Perfoms the set of checks:
//    - all statements in the block are linked correctly
//    - check statements flags
//    - check nodes gtNext and gtPrev values, if the node list is threaded
//
// Arguments:
//    block  - the block to check statements in
//    morphTrees - try to morph trees in the checker
//
// Note:
//    Checking that all bits that are set in treeFlags are also set in chkFlags is currently disabled.

void Compiler::fgDebugCheckStmtsList(BasicBlock* block, bool morphTrees)
{
    for (Statement* const stmt : block->Statements())
    {
        // Verify that bbStmtList is threaded correctly.
        // Note that for the statements list, the GetPrevStmt() list is circular.
        // The GetNextStmt() list is not: GetNextStmt() of the last statement in a block is nullptr.

        noway_assert(stmt->GetPrevStmt() != nullptr);

        if (stmt == block->bbStmtList)
        {
            noway_assert(stmt->GetPrevStmt()->GetNextStmt() == nullptr);
        }
        else
        {
            noway_assert(stmt->GetPrevStmt()->GetNextStmt() == stmt);
        }

        if (stmt->GetNextStmt() != nullptr)
        {
            noway_assert(stmt->GetNextStmt()->GetPrevStmt() == stmt);
        }
        else
        {
            noway_assert(block->lastStmt() == stmt);
        }

        /* For each statement check that the exception flags are properly set */

        noway_assert(stmt->GetRootNode());

        if (verbose && 0)
        {
            gtDispTree(stmt->GetRootNode());
        }

        fgDebugCheckFlags(stmt->GetRootNode(), block);
        fgDebugCheckTypes(stmt->GetRootNode());

        // Not only will this stress fgMorphBlockStmt(), but we also get all the checks
        // done by fgMorphTree()

        if (morphTrees)
        {
            // If 'stmt' is removed from the block, start a new check for the current block,
            // break the current check.
            if (fgMorphBlockStmt(block, stmt DEBUGARG("test morphing")))
            {
                fgDebugCheckStmtsList(block, morphTrees);
                break;
            }
        }

        // For each statement check that the nodes are threaded correctly - m_treeList.
        if (fgNodeThreading != NodeThreading::None)
        {
            fgDebugCheckNodeLinks(block, stmt);
        }
    }
}

// ensure that bbNext and bbPrev are consistent
void Compiler::fgDebugCheckBlockLinks()
{
    assert(fgFirstBB->IsFirst());

    for (BasicBlock* const block : Blocks())
    {
        if (block->IsLast())
        {
            assert(block == fgLastBB);
        }
        else
        {
            assert(block->Next()->PrevIs(block));
        }

        if (block->IsFirst())
        {
            assert(block == fgFirstBB);
        }
        else
        {
            assert(block->Prev()->NextIs(block));
        }

        // If this is a switch, check that the tables are consistent.
        // Note that we don't call GetSwitchDescMap(), because it has the side-effect
        // of allocating it if it is not present.
        if (block->KindIs(BBJ_SWITCH) && m_switchDescMap != nullptr)
        {
            SwitchUniqueSuccSet uniqueSuccSet;
            if (m_switchDescMap->Lookup(block, &uniqueSuccSet))
            {
                // Create a set with all the successors. Don't use BlockSet, so we don't need to worry
                // about the BlockSet epoch.
                BitVecTraits bitVecTraits(fgBBNumMax + 1, this);
                BitVec       succBlocks(BitVecOps::MakeEmpty(&bitVecTraits));
                for (BasicBlock* const bTarget : block->SwitchTargets())
                {
                    BitVecOps::AddElemD(&bitVecTraits, succBlocks, bTarget->bbNum);
                }
                // Now we should have a set of unique successors that matches what's in the switchMap.
                // First, check the number of entries, then make sure all the blocks in uniqueSuccSet
                // are in the BlockSet.
                unsigned count = BitVecOps::Count(&bitVecTraits, succBlocks);
                assert(uniqueSuccSet.numDistinctSuccs == count);
                for (unsigned i = 0; i < uniqueSuccSet.numDistinctSuccs; i++)
                {
                    assert(BitVecOps::IsMember(&bitVecTraits, succBlocks, uniqueSuccSet.nonDuplicates[i]->bbNum));
                }
            }
        }
    }
}

// UniquenessCheckWalker keeps data that is necessary to check
// that each tree has it is own unique id and they do not repeat.
class UniquenessCheckWalker
{
public:
    UniquenessCheckWalker(Compiler* comp)
        : comp(comp), nodesVecTraits(comp->compGenTreeID, comp), uniqueNodes(BitVecOps::MakeEmpty(&nodesVecTraits))
    {
    }

    //------------------------------------------------------------------------
    // fgMarkTreeId: Visit all subtrees in the tree and check gtTreeIDs.
    //
    // Arguments:
    //    pTree     - Pointer to the tree to walk
    //    fgWalkPre - the UniquenessCheckWalker instance
    //
    static Compiler::fgWalkResult MarkTreeId(GenTree** pTree, Compiler::fgWalkData* fgWalkPre)
    {
        UniquenessCheckWalker* walker   = static_cast<UniquenessCheckWalker*>(fgWalkPre->pCallbackData);
        unsigned               gtTreeID = (*pTree)->gtTreeID;
        walker->CheckTreeId(gtTreeID);
        return Compiler::WALK_CONTINUE;
    }

    //------------------------------------------------------------------------
    // CheckTreeId: Check that this tree was not visited before and memorize it as visited.
    //
    // Arguments:
    //    gtTreeID - identificator of GenTree.
    //
    // Note:
    //    This method causes an assert failure when we find a duplicated node in our tree
    //
    void CheckTreeId(unsigned gtTreeID)
    {
        if (BitVecOps::IsMember(&nodesVecTraits, uniqueNodes, gtTreeID))
        {
            if (comp->verbose)
            {
                printf("Duplicate gtTreeID was found: %d\n", gtTreeID);
            }
            assert(!"Duplicate gtTreeID was found");
        }
        else
        {
            BitVecOps::AddElemD(&nodesVecTraits, uniqueNodes, gtTreeID);
        }
    }

private:
    Compiler*    comp;
    BitVecTraits nodesVecTraits;
    BitVec       uniqueNodes;
};

//------------------------------------------------------------------------------
// fgDebugCheckNodesUniqueness: Check that each tree in the method has its own unique gtTreeId.
//
void Compiler::fgDebugCheckNodesUniqueness()
{
    UniquenessCheckWalker walker(this);

    for (BasicBlock* const block : Blocks())
    {
        if (block->IsLIR())
        {
            for (GenTree* i : LIR::AsRange(block))
            {
                walker.CheckTreeId(i->gtTreeID);
            }
        }
        else
        {
            for (Statement* const stmt : block->Statements())
            {
                GenTree* root = stmt->GetRootNode();
                fgWalkTreePre(&root, UniquenessCheckWalker::MarkTreeId, &walker);
            }
        }
    }
}

//------------------------------------------------------------------------------
// SsaCheckVisitor: build and maintain state about SSA uses in the IR
//
// Expects to be invoked on each root expression in each basic block that
// SSA renames (note SSA will not rename defs and uses in unreachable blocks)
// and all blocks created after SSA was built (identified by bbID).
//
// Maintains a hash table keyed by (lclNum, ssaNum) that tracks information
// about that SSA lifetime. This information is updated by each SSA use and
// def seen in the trees via ProcessUses and ProcessDefs.
//
// We can spot certain errors during collection, if local occurrences either
// unexpectedy lack or have SSA numbers.
//
// Once collection is done, DoChecks() verifies that the collected information
// is soundly approximated by the data stored in the LclSsaVarDsc entries.
//
// In particular the properties claimed for an SSA lifetime via its
// LclSsaVarDsc must be accurate or an over-estimate. We tolerate over-estimates
// as there is no good mechanism in the jit for keeping track when bits of IR
// are deleted, so over time the number and kind of uses indicated in the
// LclSsaVarDsc may show more uses and more different kinds of uses then actually
// remain in the IR.
//
// One important caveat is that for promoted locals there may be implicit uses
// (via the parent var) that do not get numbered by SSA. Neither the LclSsaVarDsc
// nor the IR will track these implicit uses. So the checking done below will
// only catch anomalies in the defs or in the explicit uses.
//
class SsaCheckVisitor : public GenTreeVisitor<SsaCheckVisitor>
{
private:
    // Hash key for tracking per-SSA lifetime info
    //
    struct SsaKey
    {
    private:
        unsigned m_lclNum;
        unsigned m_ssaNum;

    public:
        SsaKey() : m_lclNum(BAD_VAR_NUM), m_ssaNum(SsaConfig::RESERVED_SSA_NUM)
        {
        }

        SsaKey(unsigned lclNum, unsigned ssaNum) : m_lclNum(lclNum), m_ssaNum(ssaNum)
        {
        }

        static bool Equals(const SsaKey& x, const SsaKey& y)
        {
            return (x.m_lclNum == y.m_lclNum) && (x.m_ssaNum == y.m_ssaNum);
        }

        static unsigned GetHashCode(const SsaKey& x)
        {
            return (x.m_lclNum << 16) ^ x.m_ssaNum;
        }

        unsigned GetLclNum() const
        {
            return m_lclNum;
        }
        unsigned GetSsaNum() const
        {
            return m_ssaNum;
        }
    };

    // Per-SSA lifetime info
    //
    struct SsaInfo
    {
    private:
        BasicBlock* m_defBlock;
        BasicBlock* m_useBlock;
        unsigned    m_useCount;
        bool        m_hasPhiUse;
        bool        m_hasGlobalUse;
        bool        m_hasMultipleDef;

    public:
        SsaInfo()
            : m_defBlock(nullptr)
            , m_useBlock(nullptr)
            , m_useCount(0)
            , m_hasPhiUse(false)
            , m_hasGlobalUse(false)
            , m_hasMultipleDef(false)
        {
        }

        void AddUse(BasicBlock* block, const SsaKey& key)
        {
            // We may see uses before defs. If so, record the first use block we see.
            // And if we see multiple uses before/without seeing a def, use that to decide
            // if the uses are global.
            //
            if (m_defBlock == nullptr)
            {
                if (m_useBlock == nullptr)
                {
                    // Use before we've seen a def
                    //
                    m_useBlock = block;
                }
                else if (m_useBlock != block)
                {
                    // Another use, before def, see if global
                    //
                    m_hasGlobalUse = true;
                }
            }
            else if (m_defBlock != block)
            {
                m_hasGlobalUse = true;
            }

            m_useCount++;
        }

        void AddPhiUse(BasicBlock* block, const SsaKey& key)
        {
            m_hasPhiUse = true;
            AddUse(block, key);
        }

        void AddDef(BasicBlock* block, const SsaKey& key)
        {
            if (m_defBlock == nullptr)
            {
                // If we already saw a use, it might have been a global use.
                //
                if ((m_useBlock != nullptr) && (m_useBlock != block))
                {
                    m_hasGlobalUse = true;
                }

                m_defBlock = block;
            }
            else
            {
                m_hasMultipleDef = true;
            }
        }

        BasicBlock* GetDefBlock() const
        {
            return m_defBlock;
        }

        unsigned GetNumUses() const
        {
            // The ssa table use count saturates at USHRT_MAX.
            //
            if (m_useCount > USHRT_MAX)
            {
                return USHRT_MAX;
            }
            return m_useCount;
        }

        bool HasPhiUse() const
        {
            return m_hasPhiUse;
        }

        bool HasGlobalUse() const
        {
            return m_hasGlobalUse;
        }

        bool HasMultipleDef() const
        {
            return m_hasMultipleDef;
        }
    };

    typedef JitHashTable<SsaKey, SsaKey, SsaInfo*> SsaInfoMap;

    Compiler* const m_compiler;
    BasicBlock*     m_block;
    SsaInfoMap      m_infoMap;
    bool            m_hasErrors;

public:
    enum
    {
        DoPreOrder = true
    };

    SsaCheckVisitor(Compiler* compiler)
        : GenTreeVisitor<SsaCheckVisitor>(compiler)
        , m_compiler(compiler)
        , m_block(nullptr)
        , m_infoMap(compiler->getAllocator(CMK_DebugOnly))
        , m_hasErrors(false)
    {
    }

    void ProcessDef(GenTree* tree, unsigned lclNum, unsigned ssaNum)
    {
        // If the var is not in ssa, the local should not have an ssa num.
        //
        if (!m_compiler->lvaInSsa(lclNum))
        {
            if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
            {
                SetHasErrors();
                JITDUMP("[error] Unexpected SSA number on def [%06u] (V%02u)\n", m_compiler->dspTreeID(tree), lclNum);
            }
            return;
        }

        // All defs of ssa vars should have valid ssa nums.
        //
        if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
        {
            SetHasErrors();
            JITDUMP("[error] Missing SSA number on def [%06u] (V%02u)\n", m_compiler->dspTreeID(tree), lclNum);
            return;
        }

        SsaKey   key(lclNum, ssaNum);
        SsaInfo* ssaInfo = nullptr;
        if (!m_infoMap.Lookup(key, &ssaInfo))
        {
            ssaInfo = new (m_compiler->getAllocator(CMK_DebugOnly)) SsaInfo;
            m_infoMap.Set(key, ssaInfo);
        }

        ssaInfo->AddDef(m_block, key);
    }

    void ProcessDefs(GenTree* tree)
    {
        GenTreeLclVarCommon* lclNode;
        bool                 isFullDef    = false;
        ssize_t              offset       = 0;
        unsigned             storeSize    = 0;
        bool                 definesLocal = tree->DefinesLocal(m_compiler, &lclNode, &isFullDef, &offset, &storeSize);

        if (!definesLocal)
        {
            return;
        }

        const bool       isUse  = (lclNode->gtFlags & GTF_VAR_USEASG) != 0;
        unsigned const   lclNum = lclNode->GetLclNum();
        LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

        assert(!(isFullDef && isUse));

        if (lclNode->HasCompositeSsaName())
        {
            for (unsigned index = 0; index < varDsc->lvFieldCnt; index++)
            {
                unsigned const   fieldLclNum = varDsc->lvFieldLclStart + index;
                LclVarDsc* const fieldVarDsc = m_compiler->lvaGetDesc(fieldLclNum);
                unsigned const   fieldSsaNum = lclNode->GetSsaNum(m_compiler, index);

                ssize_t  fieldStoreOffset;
                unsigned fieldStoreSize;
                if (m_compiler->gtStoreDefinesField(fieldVarDsc, offset, storeSize, &fieldStoreOffset, &fieldStoreSize))
                {
                    ProcessDef(lclNode, fieldLclNum, fieldSsaNum);

                    if (!ValueNumStore::LoadStoreIsEntire(genTypeSize(fieldVarDsc), fieldStoreOffset, fieldStoreSize))
                    {
                        assert(isUse);
                        unsigned const fieldUseSsaNum = fieldVarDsc->GetPerSsaData(fieldSsaNum)->GetUseDefSsaNum();
                        ProcessUse(lclNode, fieldLclNum, fieldUseSsaNum);
                    }
                }
            }
        }
        else
        {
            unsigned const ssaNum = lclNode->GetSsaNum();
            ProcessDef(lclNode, lclNum, ssaNum);

            if (isUse)
            {
                unsigned useSsaNum = SsaConfig::RESERVED_SSA_NUM;
                if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
                {
                    useSsaNum = varDsc->GetPerSsaData(ssaNum)->GetUseDefSsaNum();
                }
                ProcessUse(lclNode, lclNum, useSsaNum);
            }
        }
    }

    void ProcessUse(GenTreeLclVarCommon* tree, unsigned lclNum, unsigned ssaNum)
    {
        // If the var is not in ssa, the tree should not have an ssa num.
        //
        if (!m_compiler->lvaInSsa(lclNum))
        {
            if (ssaNum != SsaConfig::RESERVED_SSA_NUM)
            {
                SetHasErrors();
                JITDUMP("[error] Unexpected SSA number on [%06u] (V%02u)\n", m_compiler->dspTreeID(tree), lclNum);
            }
            return;
        }

        // All uses of ssa vars should have valid ssa nums, unless there are no defs.
        //
        if (ssaNum == SsaConfig::RESERVED_SSA_NUM)
        {
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            if (varDsc->lvPerSsaData.GetCount() > 0)
            {
                SetHasErrors();
                JITDUMP("[error] Missing SSA number on use [%06u] (V%02u)\n", m_compiler->dspTreeID(tree), lclNum);
            }
            return;
        }

        SsaKey   key(lclNum, ssaNum);
        SsaInfo* ssaInfo = nullptr;

        if (!m_infoMap.Lookup(key, &ssaInfo))
        {
            ssaInfo = new (m_compiler->getAllocator(CMK_DebugOnly)) SsaInfo;
            m_infoMap.Set(key, ssaInfo);
        }

        if (tree->OperIs(GT_PHI_ARG))
        {
            ssaInfo->AddPhiUse(m_block, key);
        }
        else
        {
            ssaInfo->AddUse(m_block, key);
        }
    }

    void ProcessUses(GenTreeLclVarCommon* tree)
    {
        unsigned const lclNum = tree->GetLclNum();
        unsigned const ssaNum = tree->GetSsaNum();

        // We currently should not see composite SSA numbers for uses.
        //
        if (tree->HasCompositeSsaName())
        {
            SetHasErrors();
            JITDUMP("[error] Composite SSA number on use [%06u] (V%02u)\n", m_compiler->dspTreeID(tree), lclNum);
            return;
        }

        ProcessUse(tree, lclNum, ssaNum);
    }

    Compiler::fgWalkResult PreOrderVisit(GenTree** use, GenTree* user)
    {
        GenTree* const tree = *use;

        if (tree->OperIsSsaDef())
        {
            ProcessDefs(tree);
        }
        else if (tree->OperIs(GT_LCL_VAR, GT_LCL_FLD, GT_PHI_ARG))
        {
            ProcessUses(tree->AsLclVarCommon());
        }

        return fgWalkResult::WALK_CONTINUE;
    }

    void SetHasErrors()
    {
        if (!m_hasErrors)
        {
            JITDUMP("fgDebugCheckSsa: errors found\n");
            m_hasErrors = true;
        }
    }

    bool HasErrors() const
    {
        return m_hasErrors;
    }

    void SetBlock(BasicBlock* block)
    {
        m_block = block;
        CheckPhis(block);
    }

    void CheckPhis(BasicBlock* block)
    {
        Statement* nonPhiStmt = nullptr;
        for (Statement* const stmt : block->Statements())
        {
            // All PhiDefs should appear before any other statements
            //
            if (!stmt->IsPhiDefnStmt())
            {
                if (nonPhiStmt == nullptr)
                {
                    nonPhiStmt = stmt;
                }
                continue;
            }

            if (nonPhiStmt != nullptr)
            {
                SetHasErrors();
                JITDUMP("[error] " FMT_BB " PhiDef " FMT_STMT " appears after non-PhiDef " FMT_STMT "\n", block->bbNum,
                        stmt->GetID(), nonPhiStmt->GetID());
            }

            GenTreeLclVar* const phiDefNode = stmt->GetRootNode()->AsLclVar();
            GenTreePhi* const    phi        = phiDefNode->Data()->AsPhi();
            assert(phiDefNode->IsPhiDefn());

            // Verify each GT_PHI_ARG is the right local.
            //
            // If block does not begin a handler, verify GT_PHI_ARG blocks are unique
            // (that is, each pred supplies at most one ssa def).
            //
            BitVecTraits bitVecTraits(m_compiler->fgBBNumMax + 1, m_compiler);
            BitVec       phiPreds(BitVecOps::MakeEmpty(&bitVecTraits));

            for (GenTreePhi::Use& use : phi->Uses())
            {
                GenTreePhiArg* const phiArgNode = use.GetNode()->AsPhiArg();
                if (phiArgNode->GetLclNum() != phiDefNode->GetLclNum())
                {
                    SetHasErrors();
                    JITDUMP("[error] Wrong local V%02u in PhiArg [%06u] -- expected V%02u\n", phiArgNode->GetLclNum(),
                            m_compiler->dspTreeID(phiArgNode), phiDefNode->GetLclNum());
                }

                // Handlers can have multiple PhiArgs from the same block and implicit preds.
                // So we can't easily check their PhiArgs.
                //
                if (m_compiler->bbIsHandlerBeg(block))
                {
                    continue;
                }

                BasicBlock* const phiArgBlock = phiArgNode->gtPredBB;

                if (phiArgBlock != nullptr)
                {
                    if (BitVecOps::IsMember(&bitVecTraits, phiPreds, phiArgBlock->bbNum))
                    {
                        SetHasErrors();
                        JITDUMP("[error] " FMT_BB " [%06u]: multiple PhiArgs for predBlock " FMT_BB "\n", block->bbNum,
                                m_compiler->dspTreeID(phi), phiArgBlock->bbNum);
                    }

                    BitVecOps::AddElemD(&bitVecTraits, phiPreds, phiArgBlock->bbNum);

                    // If phiArgBlock is not a pred of block we either have messed up when building
                    // SSA or made modifications after building SSA and possibly should have pruned
                    // out or updated this PhiArg.
                    //
                    FlowEdge* const edge = m_compiler->fgGetPredForBlock(block, phiArgBlock);

                    if (edge == nullptr)
                    {
                        JITDUMP("[info] " FMT_BB " [%06u]: stale PhiArg [%06u] pred block " FMT_BB "\n", block->bbNum,
                                m_compiler->dspTreeID(phi), m_compiler->dspTreeID(phiArgNode), phiArgBlock->bbNum);
                    }
                }
            }
        }
    }

    void DoChecks()
    {
        for (unsigned lclNum = 0; lclNum < m_compiler->lvaCount; lclNum++)
        {
            // Check each local in SSA
            //
            LclVarDsc* const varDsc = m_compiler->lvaGetDesc(lclNum);

            if (!varDsc->lvInSsa)
            {
                continue;
            }

            // Check each SSA lifetime of that local
            //
            const SsaDefArray<LclSsaVarDsc>& ssaDefs = varDsc->lvPerSsaData;

            for (unsigned i = 0; i < ssaDefs.GetCount(); i++)
            {
                LclSsaVarDsc* const ssaVarDsc = ssaDefs.GetSsaDefByIndex(i);
                const unsigned      ssaNum    = ssaDefs.GetSsaNum(ssaVarDsc);

                // Find the SSA info we gathered for this lifetime via the IR walk
                //
                SsaKey   key(lclNum, ssaNum);
                SsaInfo* ssaInfo = nullptr;

                if (!m_infoMap.Lookup(key, &ssaInfo))
                {
                    // IR has no information about this lifetime.
                    // Possibly there are no more references.
                    //
                    continue;
                }

                // Now cross-check the gathered ssaInfo vs the LclSsaVarDsc.
                // LclSsaVarDsc should have the correct def block
                //
                BasicBlock* const ssaInfoDefBlock   = ssaInfo->GetDefBlock();
                BasicBlock* const ssaVarDscDefBlock = ssaVarDsc->GetBlock();

                if (ssaInfoDefBlock != ssaVarDscDefBlock)
                {
                    // We are inconsistent in tracking where the initial values of params
                    // and uninit locals come from. Tolerate.
                    //
                    const bool initialValOfParamOrLocal =
                        (lclNum < m_compiler->lvaCount) && (ssaNum == SsaConfig::FIRST_SSA_NUM);
                    const bool noDefBlockOrFirstBB =
                        (ssaInfoDefBlock == nullptr) && (ssaVarDscDefBlock == m_compiler->fgFirstBB);
                    if (!(initialValOfParamOrLocal && noDefBlockOrFirstBB))
                    {
                        JITDUMP("[error] Wrong def block for V%02u.%u : IR " FMT_BB " SSA " FMT_BB "\n", lclNum, ssaNum,
                                ssaInfoDefBlock == nullptr ? 0 : ssaInfoDefBlock->bbNum,
                                ssaVarDscDefBlock == nullptr ? 0 : ssaVarDscDefBlock->bbNum);

                        SetHasErrors();
                    }
                }

                unsigned const ssaInfoUses   = ssaInfo->GetNumUses();
                unsigned const ssaVarDscUses = ssaVarDsc->GetNumUses();

                // LclSsaVarDsc use count must be accurate or an over-estimate
                //
                if (ssaInfoUses > ssaVarDscUses)
                {
                    // If this assert fires, it's possible some optimization did not call optRecordSsaUse.
                    //
                    JITDUMP("[error] NumUses underestimated for V%02u.%u: IR %u SSA %u\n", lclNum, ssaNum, ssaInfoUses,
                            ssaVarDscUses);
                    SetHasErrors();
                }
                else if (ssaInfoUses < ssaVarDscUses)
                {
                    JITDUMP("[info] NumUses overestimated for V%02u.%u: IR %u SSA %u\n", lclNum, ssaNum, ssaInfoUses,
                            ssaVarDscUses);
                }

                // LclSsaVarDsc HasPhiUse use must be accurate or an over-estimate
                //
                if (ssaInfo->HasPhiUse() && !ssaVarDsc->HasPhiUse())
                {
                    JITDUMP("[error] HasPhiUse underestimated for V%02u.%u\n", lclNum, ssaNum);
                    SetHasErrors();
                }
                else if (!ssaInfo->HasPhiUse() && ssaVarDsc->HasPhiUse())
                {
                    JITDUMP("[info] HasPhiUse overestimated for V%02u.%u\n", lclNum, ssaNum);
                }

                // LclSsaVarDsc HasGlobalUse use must be accurate or an over-estimate
                //
                if (ssaInfo->HasGlobalUse() && !ssaVarDsc->HasGlobalUse())
                {
                    JITDUMP("[error] HasGlobalUse underestimated for V%02u.%u\n", lclNum, ssaNum);
                    SetHasErrors();
                }
                else if (!ssaInfo->HasGlobalUse() && ssaVarDsc->HasGlobalUse())
                {
                    JITDUMP("[info] HasGlobalUse overestimated for V%02u.%u\n", lclNum, ssaNum);
                }

                // There should be at most one def.
                //
                if (ssaInfo->HasMultipleDef())
                {
                    JITDUMP("[error] HasMultipleDef for V%02u.%u\n", lclNum, ssaNum);
                    SetHasErrors();
                }
            }
        }
    }
};

//------------------------------------------------------------------------------
// fgDebugCheckSsa: Check that certain SSA invariants hold.
//
// Currently verifies:
// * There is at most one SSA def for a given SSA num, and it is in the expected block.
// * Operands that should have SSA numbers have them
// * Operands that should not have SSA numbers do not have them
// * GetNumUses is accurate or an over-estimate
// * HasGlobalUse is properly set or an over-estimate
// * HasPhiUse is properly set or an over-estimate
//
// Todo:
// * Try and sanity check PHIs
// * Verify VNs on uses match the VN on the def
//
void Compiler::fgDebugCheckSsa()
{
    if (!fgSsaValid)
    {
        return;
    }

    assert(fgSsaPassesCompleted > 0);

    // Visit the blocks that SSA initially renamed
    //
    SsaCheckVisitor scv(this);
    for (unsigned i = 0; i < m_dfsTree->GetPostOrderCount(); i++)
    {
        BasicBlock* block = m_dfsTree->GetPostOrder(i);
        scv.SetBlock(block);

        for (Statement* const stmt : block->Statements())
        {
            scv.WalkTree(stmt->GetRootNodePointer(), nullptr);
        }
    }

    // Cross-check the information gathered from IR against the info
    // in the LclSsaVarDscs.
    //
    scv.DoChecks();

    if (scv.HasErrors())
    {
        assert(!"SSA check failures");
    }
    else
    {
        JITDUMP("SSA checks completed successfully\n");
    }
}

//------------------------------------------------------------------------------
// fgDebugCheckLoops: Checks that all loops are canonicalized as expected.
//
void Compiler::fgDebugCheckLoops()
{
    if (m_loops == nullptr)
    {
        return;
    }
    if (optLoopsRequirePreHeaders)
    {
        for (FlowGraphNaturalLoop* loop : m_loops->InReversePostOrder())
        {
            assert(loop->EntryEdges().size() == 1);
            assert(loop->EntryEdge(0)->getSourceBlock()->KindIs(BBJ_ALWAYS));
        }
    }
}

//------------------------------------------------------------------------------
// fgDebugCheckDfsTree: Checks that the DFS tree matches the current flow graph.
//
void Compiler::fgDebugCheckDfsTree()
{
    unsigned count =
        fgRunDfs([](BasicBlock* block, unsigned preorderNum) { assert(block->bbPreorderNum == preorderNum); },
                 [=](BasicBlock* block, unsigned postorderNum) {
                     assert(block->bbPostorderNum == postorderNum);
                     assert(m_dfsTree->GetPostOrder(postorderNum) == block);
                 },
                 [](BasicBlock* block, BasicBlock* succ) {});

    assert(m_dfsTree->GetPostOrderCount() == count);
}

/*****************************************************************************/
#endif // DEBUG
