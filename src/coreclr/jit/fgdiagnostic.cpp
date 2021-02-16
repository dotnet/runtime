// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "jitpch.h"

#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "allocacheck.h" // for alloca

// Flowgraph Check and Dump Support

#ifdef DEBUG
void Compiler::fgPrintEdgeWeights()
{
    BasicBlock* bSrc;
    BasicBlock* bDst;
    flowList*   edge;

    // Print out all of the edge weights
    for (bDst = fgFirstBB; bDst != nullptr; bDst = bDst->bbNext)
    {
        if (bDst->bbPreds != nullptr)
        {
            printf("    Edge weights into " FMT_BB " :", bDst->bbNum);
            for (edge = bDst->bbPreds; edge != nullptr; edge = edge->flNext)
            {
                bSrc = edge->getBlock();
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
                if (edge->flNext != nullptr)
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
     *                           kind of redundand with the above, but to make sure)
     * no un-compacted blocks -> BBJ_NONE followed by block with no jumps to it (countOfInEdges() = 1)
     */

    BasicBlock* prev;
    BasicBlock* block;
    for (prev = nullptr, block = fgFirstBB; block != nullptr; prev = block, block = block->bbNext)
    {
        /* no unreachable blocks */

        if ((block->countOfInEdges() == 0) && !(block->bbFlags & BBF_DONT_REMOVE)
#if defined(FEATURE_EH_FUNCLETS) && defined(TARGET_ARM)
            // With funclets, we never get rid of the BBJ_ALWAYS part of a BBJ_CALLFINALLY/BBJ_ALWAYS pair,
            // even if we can prove that the finally block never returns.
            && !block->isBBCallAlwaysPairTail()
#endif // FEATURE_EH_FUNCLETS
                )
        {
            noway_assert(!"Unreachable block not removed!");
        }

        /* no empty blocks */

        if (block->isEmpty() && !(block->bbFlags & BBF_DONT_REMOVE))
        {
            switch (block->bbJumpKind)
            {
                case BBJ_CALLFINALLY:
                case BBJ_EHFINALLYRET:
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

        if (!(block->bbFlags & BBF_IMPORTED))
        {
            /* internal blocks do not count */

            if (!(block->bbFlags & BBF_INTERNAL))
            {
                noway_assert(!"Non IMPORTED block not removed!");
            }
        }

        bool prevIsCallAlwaysPair = block->isBBCallAlwaysPairTail();

        // Check for an unnecessary jumps to the next block
        bool doAssertOnJumpToNextBlock = false; // unless we have a BBJ_COND or BBJ_ALWAYS we can not assert

        if (block->bbJumpKind == BBJ_COND)
        {
            // A conditional branch should never jump to the next block
            // as it can be folded into a BBJ_NONE;
            doAssertOnJumpToNextBlock = true;
        }
        else if (block->bbJumpKind == BBJ_ALWAYS)
        {
            // Generally we will want to assert if a BBJ_ALWAYS branches to the next block
            doAssertOnJumpToNextBlock = true;

            // If the BBF_KEEP_BBJ_ALWAYS flag is set we allow it to jump to the next block
            if (block->bbFlags & BBF_KEEP_BBJ_ALWAYS)
            {
                doAssertOnJumpToNextBlock = false;
            }

            // A call/always pair is also allowed to jump to the next block
            if (prevIsCallAlwaysPair)
            {
                doAssertOnJumpToNextBlock = false;
            }

            // We are allowed to have a branch from a hot 'block' to a cold 'bbNext'
            //
            if ((block->bbNext != nullptr) && fgInDifferentRegions(block, block->bbNext))
            {
                doAssertOnJumpToNextBlock = false;
            }
        }

        if (doAssertOnJumpToNextBlock)
        {
            if (block->bbJumpDest == block->bbNext)
            {
                noway_assert(!"Unnecessary jump to the next block!");
            }
        }

        /* Make sure BBF_KEEP_BBJ_ALWAYS is set correctly */

        if ((block->bbJumpKind == BBJ_ALWAYS) && prevIsCallAlwaysPair)
        {
            noway_assert(block->bbFlags & BBF_KEEP_BBJ_ALWAYS);
        }

        /* For a BBJ_CALLFINALLY block we make sure that we are followed by */
        /* an BBJ_ALWAYS block with BBF_INTERNAL set */
        /* or that it's a BBF_RETLESS_CALL */
        if (block->bbJumpKind == BBJ_CALLFINALLY)
        {
            assert((block->bbFlags & BBF_RETLESS_CALL) || block->isBBCallAlwaysPair());
        }

        /* no un-compacted blocks */

        if (fgCanCompactBlocks(block, block->bbNext))
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
    {':', "="},
    {'<', "["},
    {'>', "]"},
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
    bool        subsitutionRequired;
    const char* pChar;

    lengthOut           = 1;
    subsitutionRequired = false;
    pChar               = nameIn;
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
            subsitutionRequired = true;
            lengthOut += (unsigned)strlen(map[index].sub);
        }
        else
        {
            lengthOut += 1;
        }
        pChar++;
    }

    if (subsitutionRequired)
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
// fgOpenFlowGraphFile: Open a file to dump either the xml or dot format flow graph
//
// Arguments:
//    wbDontClose - A boolean out argument that indicates whether the caller should close the file
//    phase       - A phase identifier to indicate which phase is associated with the dump
//    type        - A (wide) string indicating the type of dump, "dot" or "xml"
//
// Return Value:
//    Opens a file to which a flowgraph can be dumped, whose name is based on the current
//    config vales.

FILE* Compiler::fgOpenFlowGraphFile(bool* wbDontClose, Phases phase, LPCWSTR type)
{
    FILE*       fgxFile;
    LPCWSTR     pattern  = nullptr;
    LPCWSTR     filename = nullptr;
    LPCWSTR     pathname = nullptr;
    const char* escapedString;
    bool        createDuplicateFgxFiles = true;

#ifdef DEBUG
    if (opts.jitFlags->IsSet(JitFlags::JIT_FLAG_PREJIT))
    {
        pattern  = JitConfig.NgenDumpFg();
        filename = JitConfig.NgenDumpFgFile();
        pathname = JitConfig.NgenDumpFgDir();
    }
    else
    {
        pattern  = JitConfig.JitDumpFg();
        filename = JitConfig.JitDumpFgFile();
        pathname = JitConfig.JitDumpFgDir();
    }
#endif // DEBUG

    if (fgBBcount <= 1)
    {
        return nullptr;
    }

    if (pattern == nullptr)
    {
        return nullptr;
    }

    if (wcslen(pattern) == 0)
    {
        return nullptr;
    }

    LPCWSTR phasePattern = JitConfig.JitDumpFgPhase();
    LPCWSTR phaseName    = PhaseShortNames[phase];
    if (phasePattern == nullptr)
    {
        if (phase != PHASE_DETERMINE_FIRST_COLD_BLOCK)
        {
            return nullptr;
        }
    }
    else if (*phasePattern != W('*'))
    {
        if (wcsstr(phasePattern, phaseName) == nullptr)
        {
            return nullptr;
        }
    }

    if (*pattern != W('*'))
    {
        bool hasColon = (wcschr(pattern, W(':')) != nullptr);

        if (hasColon)
        {
            const char* className = info.compClassName;
            if (*pattern == W('*'))
            {
                pattern++;
            }
            else
            {
                while ((*pattern != W(':')) && (*pattern != W('*')))
                {
                    if (*pattern != *className)
                    {
                        return nullptr;
                    }

                    pattern++;
                    className++;
                }
                if (*pattern == W('*'))
                {
                    pattern++;
                }
                else
                {
                    if (*className != 0)
                    {
                        return nullptr;
                    }
                }
            }
            if (*pattern != W(':'))
            {
                return nullptr;
            }

            pattern++;
        }

        const char* methodName = info.compMethodName;
        if (*pattern == W('*'))
        {
            pattern++;
        }
        else
        {
            while ((*pattern != 0) && (*pattern != W('*')))
            {
                if (*pattern != *methodName)
                {
                    return nullptr;
                }

                pattern++;
                methodName++;
            }
            if (*pattern == W('*'))
            {
                pattern++;
            }
            else
            {
                if (*methodName != 0)
                {
                    return nullptr;
                }
            }
        }
        if (*pattern != 0)
        {
            return nullptr;
        }
    }

    if (filename == nullptr)
    {
        filename = W("default");
    }

    if (wcscmp(filename, W("profiled")) == 0)
    {
        if (fgFirstBB->hasProfileWeight())
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    if (wcscmp(filename, W("hot")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_HOT)

        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (wcscmp(filename, W("cold")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_COLD)
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (wcscmp(filename, W("jit")) == 0)
    {
        if (info.compMethodInfo->regionKind == CORINFO_REGION_JIT)
        {
            createDuplicateFgxFiles = true;
            goto ONE_FILE_PER_METHOD;
        }
        else
        {
            return nullptr;
        }
    }
    else if (wcscmp(filename, W("all")) == 0)
    {
        createDuplicateFgxFiles = true;

    ONE_FILE_PER_METHOD:;

        escapedString = fgProcessEscapes(info.compFullName, s_EscapeFileMapping);

        const char* tierName = compGetTieringName(true);
        size_t      wCharCount =
            strlen(escapedString) + wcslen(phaseName) + 1 + strlen("~999") + wcslen(type) + strlen(tierName) + 1;
        if (pathname != nullptr)
        {
            wCharCount += wcslen(pathname) + 1;
        }
        filename = (LPCWSTR)alloca(wCharCount * sizeof(WCHAR));

        if (pathname != nullptr)
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%S-%s-%S.%s"), pathname, escapedString, phaseName, tierName,
                       type);
        }
        else
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%S.%s"), escapedString, type);
        }
        fgxFile = _wfopen(filename, W("r")); // Check if this file already exists
        if (fgxFile != nullptr)
        {
            // For Generic methods we will have both hot and cold versions
            if (createDuplicateFgxFiles == false)
            {
                fclose(fgxFile);
                return nullptr;
            }
            // Yes, this filename already exists, so create a different one by appending ~2, ~3, etc...
            for (int i = 2; i < 1000; i++)
            {
                fclose(fgxFile);
                if (pathname != nullptr)
                {
                    swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%S~%d.%s"), pathname, escapedString, i, type);
                }
                else
                {
                    swprintf_s((LPWSTR)filename, wCharCount, W("%S~%d.%s"), escapedString, i, type);
                }
                fgxFile = _wfopen(filename, W("r")); // Check if this file exists
                if (fgxFile == nullptr)
                {
                    break;
                }
            }
            // If we have already created 1000 files with this name then just fail
            if (fgxFile != nullptr)
            {
                fclose(fgxFile);
                return nullptr;
            }
        }
        fgxFile      = _wfopen(filename, W("a+"));
        *wbDontClose = false;
    }
    else if (wcscmp(filename, W("stdout")) == 0)
    {
        fgxFile      = jitstdout;
        *wbDontClose = true;
    }
    else if (wcscmp(filename, W("stderr")) == 0)
    {
        fgxFile      = stderr;
        *wbDontClose = true;
    }
    else
    {
        LPCWSTR origFilename = filename;
        size_t  wCharCount   = wcslen(origFilename) + wcslen(type) + 2;
        if (pathname != nullptr)
        {
            wCharCount += wcslen(pathname) + 1;
        }
        filename = (LPCWSTR)alloca(wCharCount * sizeof(WCHAR));
        if (pathname != nullptr)
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s\\%s.%s"), pathname, origFilename, type);
        }
        else
        {
            swprintf_s((LPWSTR)filename, wCharCount, W("%s.%s"), origFilename, type);
        }
        fgxFile      = _wfopen(filename, W("a+"));
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
//
// Return Value:
//    True iff a flowgraph has been dumped.
//
// Notes:
//    The xml dumps are the historical mechanism for dumping the flowgraph.
//    The dot format can be viewed by:
//    - Graphviz (http://www.graphviz.org/)
//      - The command "C:\Program Files (x86)\Graphviz2.38\bin\dot.exe" -Tsvg -oFoo.svg -Kdot Foo.dot
//        will produce a Foo.svg file that can be opened with any svg-capable browser (e.g. IE).
//    - http://rise4fun.com/Agl/
//      - Cut and paste the graph from your .dot file, replacing the digraph on the page, and then click the play
//        button.
//      - It will show a rotating '/' and then render the graph in the browser.
//    MSAGL has also been open-sourced to https://github.com/Microsoft/automatic-graph-layout.git.
//
//    Here are the config values that control it:
//      COMPlus_JitDumpFg       A string (ala the COMPlus_JitDump string) indicating what methods to dump flowgraphs
//                              for.
//      COMPlus_JitDumpFgDir    A path to a directory into which the flowgraphs will be dumped.
//      COMPlus_JitDumpFgFile   The filename to use. The default is "default.[xml|dot]".
//                              Note that the new graphs will be appended to this file if it already exists.
//      COMPlus_JitDumpFgPhase  Phase(s) after which to dump the flowgraph.
//                              Set to the short name of a phase to see the flowgraph after that phase.
//                              Leave unset to dump after COLD-BLK (determine first cold block) or set to * for all
//                              phases.
//      COMPlus_JitDumpFgDot    Set to non-zero to emit Dot instead of Xml Flowgraph dump. (Default is xml format.)

bool Compiler::fgDumpFlowGraph(Phases phase)
{
    bool result        = false;
    bool dontClose     = false;
    bool createDotFile = false;
    if (JitConfig.JitDumpFgDot())
    {
        createDotFile = true;
    }

    FILE* fgxFile = fgOpenFlowGraphFile(&dontClose, phase, createDotFile ? W("dot") : W("fgx"));

    if (fgxFile == nullptr)
    {
        return false;
    }
    bool        validWeights  = fgHaveValidEdgeWeights;
    double      weightDivisor = (double)fgCalledCount;
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
        fprintf(fgxFile, "    graph [label = \"%s\\nafter\\n%s\"];\n", info.compMethodName, PhaseNames[phase]);
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

        if (fgHaveProfileData())
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

    static const char* kindImage[] = {"EHFINALLYRET", "EHFILTERRET", "EHCATCHRET",  "THROW", "RETURN", "NONE",
                                      "ALWAYS",       "LEAVE",       "CALLFINALLY", "COND",  "SWITCH"};

    BasicBlock* block;
    unsigned    blockOrdinal;
    for (block = fgFirstBB, blockOrdinal = 1; block != nullptr; block = block->bbNext, blockOrdinal++)
    {
        if (createDotFile)
        {
            fprintf(fgxFile, "    BB%02u [label = \"BB%02u\\n\\n", block->bbNum, block->bbNum);

            // "Raw" Profile weight
            if (block->hasProfileWeight())
            {
                fprintf(fgxFile, "%7.2f", ((double)block->getBBWeight(this)) / BB_UNITY_WEIGHT);
            }

            // end of block label
            fprintf(fgxFile, "\"");

            // other node attributes
            //
            if (block == fgFirstBB)
            {
                fprintf(fgxFile, ", shape = \"house\"");
            }
            else if (block->bbJumpKind == BBJ_RETURN)
            {
                fprintf(fgxFile, ", shape = \"invhouse\"");
            }
            else if (block->bbJumpKind == BBJ_THROW)
            {
                fprintf(fgxFile, ", shape = \"trapezium\"");
            }
            else if (block->bbFlags & BBF_INTERNAL)
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
            fprintf(fgxFile, "\n            jumpKind=\"%s\"", kindImage[block->bbJumpKind]);
            if (block->hasTryIndex())
            {
                fprintf(fgxFile, "\n            inTry=\"%s\"", "true");
            }
            if (block->hasHndIndex())
            {
                fprintf(fgxFile, "\n            inHandler=\"%s\"", "true");
            }
            if ((fgFirstBB->hasProfileWeight()) && ((block->bbFlags & BBF_COLD) == 0))
            {
                fprintf(fgxFile, "\n            hot=\"true\"");
            }
            if (block->bbFlags & (BBF_HAS_NEWOBJ | BBF_HAS_NEWARRAY))
            {
                fprintf(fgxFile, "\n            callsNew=\"true\"");
            }
            if (block->bbFlags & BBF_LOOP_HEAD)
            {
                fprintf(fgxFile, "\n            loopHead=\"true\"");
            }
            fprintf(fgxFile, "\n            weight=");
            fprintfDouble(fgxFile, ((double)block->bbWeight) / weightDivisor);
            fprintf(fgxFile, "\n            codeEstimate=\"%d\"", fgGetCodeEstimate(block));
            fprintf(fgxFile, "\n            startOffset=\"%d\"", block->bbCodeOffs);
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

    if (fgComputePredsDone)
    {
        unsigned    edgeNum = 1;
        BasicBlock* bTarget;
        for (bTarget = fgFirstBB; bTarget != nullptr; bTarget = bTarget->bbNext)
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

            flowList* edge;
            for (edge = bTarget->bbPreds; edge != nullptr; edge = edge->flNext, edgeNum++)
            {
                BasicBlock* bSource = edge->getBlock();
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

                    if (bSource->bbNum > bTarget->bbNum)
                    {
                        // Lexical backedge
                        fprintf(fgxFile, " [color=green");
                        sep = ", ";
                    }
                    else if ((bSource->bbNum + 1) == bTarget->bbNum)
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
                        BasicBlock::weight_t edgeWeight = (edge->edgeWeightMin() + edge->edgeWeightMax()) / 2;
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
                    if (bSource->bbJumpKind == BBJ_SWITCH)
                    {
                        if (edge->flDupCount >= 2)
                        {
                            fprintf(fgxFile, "\n            switchCases=\"%d\"", edge->flDupCount);
                        }
                        if (bSource->bbJumpSwt->getDefault() == bTarget)
                        {
                            fprintf(fgxFile, "\n            switchDefault=\"true\"");
                        }
                    }
                    if (validWeights)
                    {
                        BasicBlock::weight_t edgeWeight = (edge->edgeWeightMin() + edge->edgeWeightMax()) / 2;
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
            }
        }
    }

    // For dot, show edges w/o pred lists, and add invisible bbNext links.
    //
    if (createDotFile)
    {
        for (BasicBlock* bSource = fgFirstBB; bSource != nullptr; bSource = bSource->bbNext)
        {
            // Invisible edge for bbNext chain
            //
            if (bSource->bbNext != nullptr)
            {
                fprintf(fgxFile, "    " FMT_BB " -> " FMT_BB " [style=\"invis\", weight=25];\n", bSource->bbNum,
                        bSource->bbNext->bbNum);
            }

            if (fgComputePredsDone)
            {
                // Already emitted pred edges above.
                //
                continue;
            }

            // Emit successor edges
            //
            const unsigned numSuccs = bSource->NumSucc();

            for (unsigned i = 0; i < numSuccs; i++)
            {
                BasicBlock* const bTarget = bSource->GetSucc(i);
                fprintf(fgxFile, "    " FMT_BB " -> " FMT_BB, bSource->bbNum, bTarget->bbNum);
                if (bSource->bbNum > bTarget->bbNum)
                {
                    // Lexical backedge
                    fprintf(fgxFile, " [color=green]\n");
                }
                else if ((bSource->bbNum + 1) == bTarget->bbNum)
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

#endif // DUMP_FLOWGRAPHS

/*****************************************************************************/
#ifdef DEBUG

void Compiler::fgDispReach()
{
    printf("------------------------------------------------\n");
    printf("BBnum  Reachable by \n");
    printf("------------------------------------------------\n");

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        printf(FMT_BB " : ", block->bbNum);
        BlockSetOps::Iter iter(this, block->bbReach);
        unsigned          bbNum = 0;
        while (iter.NextElem(&bbNum))
        {
            printf(FMT_BB " ", bbNum);
        }
        printf("\n");
    }
}

void Compiler::fgDispDoms()
{
    // Don't bother printing this when we have a large number of BasicBlocks in the method
    if (fgBBcount > 256)
    {
        return;
    }

    printf("------------------------------------------------\n");
    printf("BBnum  Dominated by\n");
    printf("------------------------------------------------\n");

    for (unsigned i = 1; i <= fgBBNumMax; ++i)
    {
        BasicBlock* current = fgBBInvPostOrder[i];
        printf(FMT_BB ":  ", current->bbNum);
        while (current != current->bbIDom)
        {
            printf(FMT_BB " ", current->bbNum);
            current = current->bbIDom;
        }
        printf("\n");
    }
}

/*****************************************************************************/

void Compiler::fgTableDispBasicBlock(BasicBlock* block, int ibcColWidth /* = 0 */)
{
    const unsigned __int64 flags    = block->bbFlags;
    unsigned               bbNumMax = compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : fgBBNumMax;
    int                    maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth                        = max(maxBlockNumWidth, 2);
    int blockNumWidth                       = CountDigits(block->bbNum);
    blockNumWidth                           = max(blockNumWidth, 2);
    int blockNumPadding                     = maxBlockNumWidth - blockNumWidth;

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

    unsigned charCnt;
    if (fgCheapPredsValid)
    {
        charCnt = block->dspCheapPreds();
    }
    else
    {
        charCnt = block->dspPreds();
    }

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
        BasicBlock::weight_t weight = block->getBBWeight(this);

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
                BasicBlock::weight_t weightK = weight / 1000;
                printf("%5uk", (unsigned)FloatingPointUtils::round(weightK / BB_UNITY_WEIGHT));
            }
        }
        else // print weight in this format ddd.dd
        {
            printf("%6s", refCntWtd2str(weight));
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
    // Display natural loop number
    //
    if (block->bbNatLoopNum == BasicBlock::NOT_IN_LOOP)
    {
        printf("   ");
    }
    else
    {
        printf("%2d ", block->bbNatLoopNum);
    }

    //
    // Display block IL range
    //

    block->dspBlockILRange();

    //
    // Display block branch target
    //

    if (flags & BBF_REMOVED)
    {
        printf("[removed]       ");
    }
    else
    {
        switch (block->bbJumpKind)
        {
            case BBJ_COND:
                printf("-> " FMT_BB "%*s ( cond )", block->bbJumpDest->bbNum,
                       maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                break;

            case BBJ_CALLFINALLY:
                printf("-> " FMT_BB "%*s (callf )", block->bbJumpDest->bbNum,
                       maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                break;

            case BBJ_ALWAYS:
                if (flags & BBF_KEEP_BBJ_ALWAYS)
                {
                    printf("-> " FMT_BB "%*s (ALWAYS)", block->bbJumpDest->bbNum,
                           maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                }
                else
                {
                    printf("-> " FMT_BB "%*s (always)", block->bbJumpDest->bbNum,
                           maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                }
                break;

            case BBJ_LEAVE:
                printf("-> " FMT_BB "%*s (leave )", block->bbJumpDest->bbNum,
                       maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                break;

            case BBJ_EHFINALLYRET:
                printf("%*s        (finret)", maxBlockNumWidth - 2, "");
                break;

            case BBJ_EHFILTERRET:
                printf("%*s        (fltret)", maxBlockNumWidth - 2, "");
                break;

            case BBJ_EHCATCHRET:
                printf("-> " FMT_BB "%*s ( cret )", block->bbJumpDest->bbNum,
                       maxBlockNumWidth - max(CountDigits(block->bbJumpDest->bbNum), 2), "");
                break;

            case BBJ_THROW:
                printf("%*s        (throw )", maxBlockNumWidth - 2, "");
                break;

            case BBJ_RETURN:
                printf("%*s        (return)", maxBlockNumWidth - 2, "");
                break;

            default:
                printf("%*s                ", maxBlockNumWidth - 2, "");
                break;

            case BBJ_SWITCH:
                printf("->");

                unsigned jumpCnt;
                jumpCnt = block->bbJumpSwt->bbsCount;
                BasicBlock** jumpTab;
                jumpTab = block->bbJumpSwt->bbsDstTab;
                int switchWidth;
                switchWidth = 0;
                do
                {
                    printf("%c" FMT_BB, (jumpTab == block->bbJumpSwt->bbsDstTab) ? ' ' : ',', (*jumpTab)->bbNum);
                    switchWidth += 1 /* space/comma */ + 2 /* BB */ + max(CountDigits((*jumpTab)->bbNum), 2);
                } while (++jumpTab, --jumpCnt);

                if (switchWidth < 7)
                {
                    printf("%*s", 8 - switchWidth, "");
                }

                printf(" (switch)");
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

    if (flags & BBF_TRY_BEG)
    {
        // Output a brace for every try region that this block opens

        EHblkDsc* HBtab;
        EHblkDsc* HBtabEnd;

        for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd; HBtab++)
        {
            if (HBtab->ebdTryBeg == block)
            {
                cnt += 6;
                printf("try { ");
                /* brace matching editor workaround to compensate for the preceding line: } */
            }
        }
    }

    EHblkDsc* HBtab;
    EHblkDsc* HBtabEnd;

    for (HBtab = compHndBBtab, HBtabEnd = compHndBBtab + compHndBBtabCount; HBtab < HBtabEnd; HBtab++)
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
        if (HBtab->HasFilter() && block->bbNext == HBtab->ebdHndBeg)
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

    printf("\n");
}

/****************************************************************************
    Dump blocks from firstBlock to lastBlock.
*/

void Compiler::fgDispBasicBlocks(BasicBlock* firstBlock, BasicBlock* lastBlock, bool dumpTrees)
{
    BasicBlock* block;

    // If any block has IBC data, we add an "IBC weight" column just before the 'IL range' column. This column is as
    // wide as necessary to accommodate all the various IBC weights. It's at least 4 characters wide, to accommodate
    // the "IBC" title and leading space.
    int ibcColWidth = 0;
    for (block = firstBlock; block != nullptr; block = block->bbNext)
    {
        if (block->hasProfileWeight())
        {
            int thisIbcWidth = CountDigits(block->bbWeight);
            ibcColWidth      = max(ibcColWidth, thisIbcWidth);
        }

        if (block == lastBlock)
        {
            break;
        }
    }
    if (ibcColWidth > 0)
    {
        ibcColWidth = max(ibcColWidth, 3) + 1; // + 1 for the leading space
    }

    unsigned bbNumMax         = compIsForInlining() ? impInlineInfo->InlinerCompiler->fgBBNumMax : fgBBNumMax;
    int      maxBlockNumWidth = CountDigits(bbNumMax);
    maxBlockNumWidth          = max(maxBlockNumWidth, 2);
    int padWidth              = maxBlockNumWidth - 2; // Account for functions with a large number of blocks.

    // clang-format off

    printf("\n");
    printf("------%*s-------------------------------------%*s--------------------------%*s----------------------------------------\n",
        padWidth, "------------",
        ibcColWidth, "------------",
        maxBlockNumWidth, "----");
    printf("BBnum %*sBBid ref try hnd %s     weight  %*s%s  lp [IL range]     [jump]%*s    [EH region]         [flags]\n",
        padWidth, "",
        fgCheapPredsValid       ? "cheap preds" :
        (fgComputePredsDone     ? "preds      "
                                : "           "),
        ((ibcColWidth > 0) ? ibcColWidth - 3 : 0), "",  // Subtract 3 for the width of "IBC", printed next.
        ((ibcColWidth > 0)      ? "IBC"
                                : ""),
        maxBlockNumWidth, ""
        );
    printf("------%*s-------------------------------------%*s--------------------------%*s----------------------------------------\n",
        padWidth, "------------",
        ibcColWidth, "------------",
        maxBlockNumWidth, "----");

    // clang-format on

    for (block = firstBlock; block; block = block->bbNext)
    {
        // First, do some checking on the bbPrev links
        if (block->bbPrev)
        {
            if (block->bbPrev->bbNext != block)
            {
                printf("bad prev link\n");
            }
        }
        else if (block != fgFirstBB)
        {
            printf("bad prev link!\n");
        }

        if (block == fgFirstColdBlock)
        {
            printf(
                "~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~~~%*s~~~~~~~~~~~~~~~~~~~~~~~~"
                "~~~~~~~~~~~~~~~~\n",
                padWidth, "~~~~~~~~~~~~", ibcColWidth, "~~~~~~~~~~~~", maxBlockNumWidth, "~~~~");
        }

#if defined(FEATURE_EH_FUNCLETS)
        if (block == fgFirstFuncletBB)
        {
            printf(
                "++++++%*s+++++++++++++++++++++++++++++++++++++%*s++++++++++++++++++++++++++%*s++++++++++++++++++++++++"
                "++++++++++++++++ funclets follow\n",
                padWidth, "++++++++++++", ibcColWidth, "++++++++++++", maxBlockNumWidth, "++++");
        }
#endif // FEATURE_EH_FUNCLETS

        fgTableDispBasicBlock(block, ibcColWidth);

        if (block == lastBlock)
        {
            break;
        }
    }

    printf(
        "------%*s-------------------------------------%*s--------------------------%*s--------------------------------"
        "--------\n",
        padWidth, "------------", ibcColWidth, "------------", maxBlockNumWidth, "----");

    if (dumpTrees)
    {
        fgDumpTrees(firstBlock, lastBlock);
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
//    stmt  - the statement to dump;
//    bbNum - the basic block number to dump.
//
void Compiler::fgDumpStmtTree(Statement* stmt, unsigned bbNum)
{
    printf("\n***** " FMT_BB "\n", bbNum);
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

    if (!block->IsLIR())
    {
        for (Statement* stmt : block->Statements())
        {
            fgDumpStmtTree(stmt, block->bbNum);
        }
    }
    else
    {
        gtDispRange(LIR::AsRange(block));
    }
}

/*****************************************************************************/
//  Walk the BasicBlock list calling fgDumpTree once per Stmt
//
void Compiler::fgDumpTrees(BasicBlock* firstBlock, BasicBlock* lastBlock)
{
    // Walk the basic blocks.

    // Note that typically we have already called fgDispBasicBlocks()
    // so we don't need to print the preds and succs again here.
    for (BasicBlock* block = firstBlock; block; block = block->bbNext)
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

#ifdef DEBUG
    if (pComp->verbose)
    {
        printf("STRESS_64RSLT_MUL before:\n");
        pComp->gtDispTree(tree);
    }
#endif // DEBUG

    // To ensure optNarrowTree() doesn't fold back to the original tree.
    tree->AsOp()->gtOp1 = pComp->gtNewCastNode(TYP_LONG, tree->AsOp()->gtOp1, false, TYP_LONG);
    tree->AsOp()->gtOp1 = pComp->gtNewOperNode(GT_NOP, TYP_LONG, tree->AsOp()->gtOp1);
    tree->AsOp()->gtOp1 = pComp->gtNewCastNode(TYP_LONG, tree->AsOp()->gtOp1, false, TYP_LONG);
    tree->AsOp()->gtOp2 = pComp->gtNewCastNode(TYP_LONG, tree->AsOp()->gtOp2, false, TYP_LONG);
    tree->gtType        = TYP_LONG;
    *pTree              = pComp->gtNewCastNode(TYP_INT, tree, false, TYP_INT);

#ifdef DEBUG
    if (pComp->verbose)
    {
        printf("STRESS_64RSLT_MUL after:\n");
        pComp->gtDispTree(*pTree);
    }
#endif // DEBUG

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
    bool CheckEHFinalyRet(BasicBlock* blockPred, BasicBlock* block);

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
    if (comp->fgCheapPredsValid)
    {
        return 0;
    }

    if (!comp->fgComputePredsDone)
    {
        assert(block->bbPreds == nullptr);
        return 0;
    }

    unsigned blockRefs = 0;
    for (flowList* pred = block->bbPreds; pred != nullptr; pred = pred->flNext)
    {
        blockRefs += pred->flDupCount;

        BasicBlock* blockPred = pred->getBlock();

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

    // Make sure preds are in increasting BBnum order
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
    // is marked as "returning" to the BBJ_ALWAYS block following the BBJ_CALLFINALLY
    // block that does a local call to the finally. This BBJ_ALWAYS is within
    // the try region protected by the finally (for x86, ARM), but that's ok.
    BasicBlock* prevBlock = block->bbPrev;
    if (prevBlock->bbJumpKind == BBJ_CALLFINALLY && block->bbJumpKind == BBJ_ALWAYS &&
        blockPred->bbJumpKind == BBJ_EHFINALLYRET)
    {
        return true;
    }

    // For OSR, we allow the firstBB to branch to the middle of a try.
    if (comp->opts.IsOSR() && (blockPred == comp->fgFirstBB))
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
    if ((blockPred->bbJumpKind == BBJ_EHFINALLYRET) || (blockPred->bbJumpKind == BBJ_EHFILTERRET))
    {
        return true;
    }

    // Our try block can call our finally block
    if ((block->bbCatchTyp == BBCT_FINALLY) && (blockPred->bbJumpKind == BBJ_CALLFINALLY) &&
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
    switch (blockPred->bbJumpKind)
    {
        case BBJ_COND:
            assert(blockPred->bbNext == block || blockPred->bbJumpDest == block);
            return true;

        case BBJ_NONE:
            assert(blockPred->bbNext == block);
            return true;

        case BBJ_CALLFINALLY:
        case BBJ_ALWAYS:
        case BBJ_EHCATCHRET:
        case BBJ_EHFILTERRET:
            assert(blockPred->bbJumpDest == block);
            return true;

        case BBJ_EHFINALLYRET:
            assert(CheckEHFinalyRet(blockPred, block));
            return true;

        case BBJ_THROW:
        case BBJ_RETURN:
            assert(!"THROW and RETURN block cannot be in the predecessor list!");
            break;

        case BBJ_SWITCH:
        {
            unsigned jumpCnt = blockPred->bbJumpSwt->bbsCount;

            for (unsigned i = 0; i < jumpCnt; ++i)
            {
                BasicBlock* jumpTab = blockPred->bbJumpSwt->bbsDstTab[i];
                assert(jumpTab != nullptr);
                if (block == jumpTab)
                {
                    return true;
                }
            }

            assert(!"SWITCH in the predecessor list with no jump label to BLOCK!");
        }
        break;

        default:
            assert(!"Unexpected bbJumpKind");
            break;
    }
    return false;
}

bool BBPredsChecker::CheckEHFinalyRet(BasicBlock* blockPred, BasicBlock* block)
{

    // If the current block is a successor to a BBJ_EHFINALLYRET (return from finally),
    // then the lexically previous block should be a call to the same finally.
    // Verify all of that.

    unsigned    hndIndex = blockPred->getHndIndex();
    EHblkDsc*   ehDsc    = comp->ehGetDsc(hndIndex);
    BasicBlock* finBeg   = ehDsc->ebdHndBeg;

    // Because there is no bbPrev, we have to search for the lexically previous
    // block.  We can shorten the search by only looking in places where it is legal
    // to have a call to the finally.

    BasicBlock* begBlk;
    BasicBlock* endBlk;
    comp->ehGetCallFinallyBlockRange(hndIndex, &begBlk, &endBlk);

    for (BasicBlock* bcall = begBlk; bcall != endBlk; bcall = bcall->bbNext)
    {
        if (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
        {
            continue;
        }

        if (block == bcall->bbNext)
        {
            return true;
        }
    }

#if defined(FEATURE_EH_FUNCLETS)

    if (comp->fgFuncletsCreated)
    {
        // There is no easy way to search just the funclets that were pulled out of
        // the corresponding try body, so instead we search all the funclets, and if
        // we find a potential 'hit' we check if the funclet we're looking at is
        // from the correct try region.

        for (BasicBlock* bcall = comp->fgFirstFuncletBB; bcall != nullptr; bcall = bcall->bbNext)
        {
            if (bcall->bbJumpKind != BBJ_CALLFINALLY || bcall->bbJumpDest != finBeg)
            {
                continue;
            }

            if (block != bcall->bbNext)
            {
                continue;
            }

            if (comp->ehCallFinallyInCorrectRegion(bcall, hndIndex))
            {
                return true;
            }
        }
    }

#endif // FEATURE_EH_FUNCLETS

    assert(!"BBJ_EHFINALLYRET predecessor of block that doesn't follow a BBJ_CALLFINALLY!");
    return false;
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
#ifdef DEBUG
    if (verbose)
    {
        printf("*************** In fgDebugCheckBBlist\n");
    }
#endif // DEBUG

    fgDebugCheckBlockLinks();
    fgFirstBBisScratch();

    if (fgBBcount > 10000 && expensiveDebugCheckLevel < 1)
    {
        // The basic block checks are too expensive if there are too many blocks,
        // so give up unless we've been told to try hard.
        return;
    }

    DWORD startTickCount = GetTickCount();

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
            assert(fgFirstFuncletBB->bbFlags & BBF_FUNCLET_BEG);
        }
    }
#endif // FEATURE_EH_FUNCLETS

    /* Check bbNum, bbRefs and bbPreds */
    // First, pick a traversal stamp, and label all the blocks with it.
    unsigned curTraversalStamp = unsigned(InterlockedIncrement((LONG*)&bbTraverseLabel));
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        block->bbTraversalStamp = curTraversalStamp;
    }

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (checkBBNum)
        {
            // Check that bbNum is sequential
            assert(block->bbNext == nullptr || (block->bbNum + 1 == block->bbNext->bbNum));
        }

        // If the block is a BBJ_COND, a BBJ_SWITCH or a
        // lowered GT_SWITCH_TABLE node then make sure it
        // ends with a conditional jump or a GT_SWITCH

        if (block->bbJumpKind == BBJ_COND)
        {
            assert(block->lastNode()->gtNext == nullptr && block->lastNode()->OperIsConditionalJump());
        }
        else if (block->bbJumpKind == BBJ_SWITCH)
        {
            assert(block->lastNode()->gtNext == nullptr &&
                   (block->lastNode()->gtOper == GT_SWITCH || block->lastNode()->gtOper == GT_SWITCH_TABLE));
        }

        if (block->bbCatchTyp == BBCT_FILTER)
        {
            if (!fgCheapPredsValid) // Don't check cheap preds
            {
                // A filter has no predecessors
                assert(block->bbPreds == nullptr);
            }
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
            assert(fgComputePredsDone);
        }

        BBPredsChecker checker(this);
        unsigned       blockRefs = checker.CheckBBPreds(block, curTraversalStamp);

        // First basic block has an additional global incoming edge.
        if (block == fgFirstBB)
        {
            blockRefs += 1;
        }

        // Under OSR, if we also are keeping the original method entry around,
        // mark that as implicitly referenced as well.
        if (opts.IsOSR() && (block == fgEntryBB))
        {
            blockRefs += 1;
        }

        /* Check the bbRefs */
        if (checkBBRefs)
        {
            if (block->bbRefs != blockRefs)
            {
                // Check to see if this block is the beginning of a filter or a handler and adjust the ref count
                // appropriately.
                for (EHblkDsc *HBtab = compHndBBtab, *HBtabEnd = &compHndBBtab[compHndBBtabCount]; HBtab != HBtabEnd;
                     HBtab++)
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

    // Make sure the one return BB is not changed.
    if (genReturnBB != nullptr)
    {
        assert(genReturnBB->GetFirstLIRNode() != nullptr || genReturnBB->bbStmtList != nullptr);
    }

    // The general encoder/decoder (currently) only reports "this" as a generics context as a stack location,
    // so we mark info.compThisArg as lvAddrTaken to ensure that it is not enregistered. Otherwise, it should
    // not be address-taken.  This variable determines if the address-taken-ness of "thisArg" is "OK".
    bool copiedForGenericsCtxt;
#ifndef JIT32_GCENCODER
    copiedForGenericsCtxt = ((info.compMethodInfo->options & CORINFO_GENERICS_CTXT_FROM_THIS) != 0);
#else  // JIT32_GCENCODER
    copiedForGenericsCtxt = FALSE;
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
        bool compThisArgAddrExposedOK = !lvaTable[info.compThisArg].lvAddrExposed;

#ifndef JIT32_GCENCODER
        compThisArgAddrExposedOK = compThisArgAddrExposedOK || copiedForGenericsCtxt;
#endif // !JIT32_GCENCODER

        // Should never expose the address of arg 0 or write to arg 0.
        // In addition, lvArg0Var should remain 0 if arg0 is not
        // written to or address-exposed.
        assert(compThisArgAddrExposedOK && !lvaTable[info.compThisArg].lvHasILStoreOp &&
               (lvaArg0Var == info.compThisArg ||
                (lvaArg0Var != info.compThisArg && (lvaTable[lvaArg0Var].lvAddrExposed ||
                                                    lvaTable[lvaArg0Var].lvHasILStoreOp || copiedForGenericsCtxt))));
    }
}

/*****************************************************************************
 *
 * A DEBUG routine to check the that the exception flags are correctly set.
 *
 ****************************************************************************/

void Compiler::fgDebugCheckFlags(GenTree* tree)
{
    const genTreeOps oper      = tree->OperGet();
    const unsigned   kind      = tree->OperKind();
    unsigned         treeFlags = tree->gtFlags & GTF_ALL_EFFECT;
    unsigned         chkFlags  = 0;

    if (tree->OperMayThrow(this))
    {
        chkFlags |= GTF_EXCEPT;
    }

    if (tree->OperRequiresCallFlag(this))
    {
        chkFlags |= GTF_CALL;
    }

    /* Is this a leaf node? */

    if (kind & GTK_LEAF)
    {
        switch (oper)
        {
            case GT_CLS_VAR:
                chkFlags |= GTF_GLOB_REF;
                break;

            case GT_CATCH_ARG:
                chkFlags |= GTF_ORDER_SIDEEFF;
                break;

            case GT_MEMORYBARRIER:
                chkFlags |= GTF_GLOB_REF | GTF_ASG;
                break;

            case GT_LCL_VAR:
                assert((tree->gtFlags & GTF_VAR_FOLDED_IND) == 0);
                break;

            default:
                break;
        }
    }

    /* Is it a 'simple' unary/binary operator? */

    else if (kind & GTK_SMPOP)
    {
        GenTree* op1 = tree->AsOp()->gtOp1;
        GenTree* op2 = tree->gtGetOp2IfPresent();

        // During GS work, we make shadow copies for params.
        // In gsParamsToShadows(), we create a shadow var of TYP_INT for every small type param.
        // Then in gsReplaceShadowParams(), we change the gtLclNum to the shadow var.
        // We also change the types of the local var tree and the assignment tree to TYP_INT if necessary.
        // However, since we don't morph the tree at this late stage. Manually propagating
        // TYP_INT up to the GT_ASG tree is only correct if we don't need to propagate the TYP_INT back up.
        // The following checks will ensure this.

        // Is the left child of "tree" a GT_ASG?
        //
        // If parent is a TYP_VOID, we don't no need to propagate TYP_INT up. We are fine.
        // (or) If GT_ASG is the left child of a GT_COMMA, the type of the GT_COMMA node will
        // be determined by its right child. So we don't need to propagate TYP_INT up either. We are fine.
        if (op1 && op1->gtOper == GT_ASG)
        {
            assert(tree->gtType == TYP_VOID || tree->gtOper == GT_COMMA);
        }

        // Is the right child of "tree" a GT_ASG?
        //
        // If parent is a TYP_VOID, we don't no need to propagate TYP_INT up. We are fine.
        if (op2 && op2->gtOper == GT_ASG)
        {
            // We can have ASGs on the RHS of COMMAs in setup arguments to a call.
            assert(tree->gtType == TYP_VOID || tree->gtOper == GT_COMMA);
        }

        switch (oper)
        {
            case GT_QMARK:
                if (op1->OperIsCompare())
                {
                    noway_assert(op1->gtFlags & GTF_DONT_CSE);
                }
                else
                {
                    noway_assert((op1->gtOper == GT_CNS_INT) &&
                                 ((op1->AsIntCon()->gtIconVal == 0) || (op1->AsIntCon()->gtIconVal == 1)));
                }
                break;

            case GT_LIST:
                if ((op2 != nullptr) && op2->OperIsAnyList())
                {
                    ArrayStack<GenTree*> stack(getAllocator(CMK_DebugOnly));
                    while ((tree->gtGetOp2() != nullptr) && tree->gtGetOp2()->OperIsAnyList())
                    {
                        stack.Push(tree);
                        tree = tree->gtGetOp2();
                    }

                    fgDebugCheckFlags(tree);

                    while (!stack.Empty())
                    {
                        tree = stack.Pop();
                        assert((tree->gtFlags & GTF_REVERSE_OPS) == 0);
                        fgDebugCheckFlags(tree->AsOp()->gtOp1);
                        chkFlags |= (tree->AsOp()->gtOp1->gtFlags & GTF_ALL_EFFECT);
                        chkFlags |= (tree->gtGetOp2()->gtFlags & GTF_ALL_EFFECT);
                        fgDebugCheckFlagsHelper(tree, (tree->gtFlags & GTF_ALL_EFFECT), chkFlags);
                    }

                    return;
                }
                break;
            case GT_ADDR:
                assert(!op1->CanCSE());
                break;

            case GT_IND:
                // Do we have a constant integer address as op1?
                //
                if (op1->OperGet() == GT_CNS_INT)
                {
                    // Is this constant a handle of some kind?
                    //
                    unsigned handleKind = (op1->gtFlags & GTF_ICON_HDL_MASK);
                    if (handleKind != 0)
                    {
                        // Is the GTF_IND_INVARIANT flag set or unset?
                        //
                        bool invariantFlag = (tree->gtFlags & GTF_IND_INVARIANT) != 0;
                        if (invariantFlag)
                        {
                            // Record the state of the GTF_IND_INVARIANT flags into 'chkFlags'
                            chkFlags |= GTF_IND_INVARIANT;
                        }

                        // Is the GTF_IND_NONFAULTING flag set or unset?
                        //
                        bool nonFaultingFlag = (tree->gtFlags & GTF_IND_NONFAULTING) != 0;
                        if (nonFaultingFlag)
                        {
                            // Record the state of the GTF_IND_NONFAULTING flags into 'chkFlags'
                            chkFlags |= GTF_IND_NONFAULTING;
                        }
                        assert(nonFaultingFlag); // Currently this should always be set for all handle kinds

                        // Some of these aren't handles to invariant data...
                        //
                        if ((handleKind == GTF_ICON_STATIC_HDL) || // Pointer to a mutable class Static variable
                            (handleKind == GTF_ICON_BBC_PTR) ||    // Pointer to a mutable basic block count value
                            (handleKind == GTF_ICON_GLOBAL_PTR))   // Pointer to mutable data from the VM state

                        {
                            // We expect the Invariant flag to be unset for this handleKind
                            // If it is set then we will assert with "unexpected GTF_IND_INVARIANT flag set ...
                            //
                            if (handleKind == GTF_ICON_STATIC_HDL)
                            {
                                // We expect the GTF_GLOB_REF flag to be set for this handleKind
                                // If it is not set then we will assert with "Missing flags on tree"
                                //
                                treeFlags |= GTF_GLOB_REF;
                            }
                        }
                        else // All the other handle indirections are considered invariant
                        {
                            // We expect the Invariant flag to be set for this handleKind
                            // If it is not set then we will assert with "Missing flags on tree"
                            //
                            treeFlags |= GTF_IND_INVARIANT;
                        }

                        // We currently expect all handle kinds to be nonFaulting
                        //
                        treeFlags |= GTF_IND_NONFAULTING;

                        // Matrix for GTF_IND_INVARIANT (treeFlags and chkFlags)
                        //
                        //                    chkFlags INVARIANT value
                        //                       0                 1
                        //                 +--------------+----------------+
                        //  treeFlags   0  |    OK        |  Missing Flag  |
                        //  INVARIANT      +--------------+----------------+
                        //  value:      1  |  Extra Flag  |       OK       |
                        //                 +--------------+----------------+
                    }
                }
                break;

            default:
                break;
        }

        /* Recursively check the subtrees */

        if (op1)
        {
            fgDebugCheckFlags(op1);
        }
        if (op2)
        {
            fgDebugCheckFlags(op2);
        }

        if (op1)
        {
            chkFlags |= (op1->gtFlags & GTF_ALL_EFFECT);
        }
        if (op2)
        {
            chkFlags |= (op2->gtFlags & GTF_ALL_EFFECT);
        }

        // We reuse the value of GTF_REVERSE_OPS for a GT_IND-specific flag,
        // so exempt that (unary) operator.
        if (tree->OperGet() != GT_IND && tree->gtFlags & GTF_REVERSE_OPS)
        {
            /* Must have two operands if GTF_REVERSE is set */
            noway_assert(op1 && op2);

            /* Make sure that the order of side effects has not been swapped. */

            /* However CSE may introduce an assignment after the reverse flag
               was set and thus GTF_ASG cannot be considered here. */

            /* For a GT_ASG(GT_IND(x), y) we are interested in the side effects of x */
            GenTree* op1p;
            if ((oper == GT_ASG) && (op1->gtOper == GT_IND))
            {
                op1p = op1->AsOp()->gtOp1;
            }
            else
            {
                op1p = op1;
            }

            /* This isn't true any more with the sticky GTF_REVERSE */
            /*
            // if op1p has side effects, then op2 cannot have side effects
            if (op1p->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG))
            {
                if (op2->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG))
                    gtDispTree(tree);
                noway_assert(!(op2->gtFlags & (GTF_SIDE_EFFECT & ~GTF_ASG)));
            }
            */
        }

        if (tree->OperRequiresAsgFlag())
        {
            chkFlags |= GTF_ASG;
        }

        if (oper == GT_ADDR && (op1->OperIsLocal() || op1->gtOper == GT_CLS_VAR ||
                                (op1->gtOper == GT_IND && op1->AsOp()->gtOp1->gtOper == GT_CLS_VAR_ADDR)))
        {
            /* &aliasedVar doesn't need GTF_GLOB_REF, though alisasedVar does.
               Similarly for clsVar */
            treeFlags |= GTF_GLOB_REF;
        }
    }

    /* See what kind of a special operator we have here */

    else
    {
        switch (tree->OperGet())
        {
            case GT_CALL:

                GenTreeCall* call;

                call = tree->AsCall();

                if (call->gtCallThisArg != nullptr)
                {
                    fgDebugCheckFlags(call->gtCallThisArg->GetNode());
                    chkFlags |= (call->gtCallThisArg->GetNode()->gtFlags & GTF_SIDE_EFFECT);

                    if ((call->gtCallThisArg->GetNode()->gtFlags & GTF_ASG) != 0)
                    {
                        treeFlags |= GTF_ASG;
                    }
                }

                for (GenTreeCall::Use& use : call->Args())
                {
                    fgDebugCheckFlags(use.GetNode());

                    chkFlags |= (use.GetNode()->gtFlags & GTF_SIDE_EFFECT);

                    if ((use.GetNode()->gtFlags & GTF_ASG) != 0)
                    {
                        treeFlags |= GTF_ASG;
                    }
                }

                for (GenTreeCall::Use& use : call->LateArgs())
                {
                    fgDebugCheckFlags(use.GetNode());

                    chkFlags |= (use.GetNode()->gtFlags & GTF_SIDE_EFFECT);

                    if ((use.GetNode()->gtFlags & GTF_ASG) != 0)
                    {
                        treeFlags |= GTF_ASG;
                    }
                }

                if ((call->gtCallType == CT_INDIRECT) && (call->gtCallCookie != nullptr))
                {
                    fgDebugCheckFlags(call->gtCallCookie);
                    chkFlags |= (call->gtCallCookie->gtFlags & GTF_SIDE_EFFECT);
                }

                if (call->gtCallType == CT_INDIRECT)
                {
                    fgDebugCheckFlags(call->gtCallAddr);
                    chkFlags |= (call->gtCallAddr->gtFlags & GTF_SIDE_EFFECT);
                }

                if ((call->gtControlExpr != nullptr) && call->IsExpandedEarly() && call->IsVirtualVtable())
                {
                    fgDebugCheckFlags(call->gtControlExpr);
                    chkFlags |= (call->gtControlExpr->gtFlags & GTF_SIDE_EFFECT);
                }

                if (call->IsUnmanaged() && (call->gtCallMoreFlags & GTF_CALL_M_UNMGD_THISCALL))
                {
                    if (call->gtCallArgs->GetNode()->OperGet() == GT_NOP)
                    {
                        noway_assert(call->gtCallLateArgs->GetNode()->TypeGet() == TYP_I_IMPL ||
                                     call->gtCallLateArgs->GetNode()->TypeGet() == TYP_BYREF);
                    }
                    else
                    {
                        noway_assert(call->gtCallArgs->GetNode()->TypeGet() == TYP_I_IMPL ||
                                     call->gtCallArgs->GetNode()->TypeGet() == TYP_BYREF);
                    }
                }
                break;

            case GT_ARR_ELEM:

                GenTree* arrObj;
                unsigned dim;

                arrObj = tree->AsArrElem()->gtArrObj;
                fgDebugCheckFlags(arrObj);
                chkFlags |= (arrObj->gtFlags & GTF_ALL_EFFECT);

                for (dim = 0; dim < tree->AsArrElem()->gtArrRank; dim++)
                {
                    fgDebugCheckFlags(tree->AsArrElem()->gtArrInds[dim]);
                    chkFlags |= tree->AsArrElem()->gtArrInds[dim]->gtFlags & GTF_ALL_EFFECT;
                }
                break;

            case GT_ARR_OFFSET:

                fgDebugCheckFlags(tree->AsArrOffs()->gtOffset);
                chkFlags |= (tree->AsArrOffs()->gtOffset->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(tree->AsArrOffs()->gtIndex);
                chkFlags |= (tree->AsArrOffs()->gtIndex->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(tree->AsArrOffs()->gtArrObj);
                chkFlags |= (tree->AsArrOffs()->gtArrObj->gtFlags & GTF_ALL_EFFECT);
                break;

            case GT_ARR_BOUNDS_CHECK:
#ifdef FEATURE_SIMD
            case GT_SIMD_CHK:
#endif // FEATURE_SIMD
#ifdef FEATURE_HW_INTRINSICS
            case GT_HW_INTRINSIC_CHK:
#endif // FEATURE_HW_INTRINSICS

                GenTreeBoundsChk* bndsChk;
                bndsChk = tree->AsBoundsChk();
                fgDebugCheckFlags(bndsChk->gtIndex);
                chkFlags |= (bndsChk->gtIndex->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(bndsChk->gtArrLen);
                chkFlags |= (bndsChk->gtArrLen->gtFlags & GTF_ALL_EFFECT);
                break;

            case GT_PHI:
                for (GenTreePhi::Use& use : tree->AsPhi()->Uses())
                {
                    fgDebugCheckFlags(use.GetNode());
                    chkFlags |= (use.GetNode()->gtFlags & GTF_ALL_EFFECT);
                }
                break;

            case GT_FIELD_LIST:
                for (GenTreeFieldList::Use& use : tree->AsFieldList()->Uses())
                {
                    fgDebugCheckFlags(use.GetNode());
                    chkFlags |= (use.GetNode()->gtFlags & GTF_ALL_EFFECT);
                }
                break;

            case GT_CMPXCHG:

                chkFlags |= (GTF_GLOB_REF | GTF_ASG);
                GenTreeCmpXchg* cmpXchg;
                cmpXchg = tree->AsCmpXchg();
                fgDebugCheckFlags(cmpXchg->gtOpLocation);
                chkFlags |= (cmpXchg->gtOpLocation->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(cmpXchg->gtOpValue);
                chkFlags |= (cmpXchg->gtOpValue->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(cmpXchg->gtOpComparand);
                chkFlags |= (cmpXchg->gtOpComparand->gtFlags & GTF_ALL_EFFECT);
                break;

            case GT_STORE_DYN_BLK:
            case GT_DYN_BLK:

                GenTreeDynBlk* dynBlk;
                dynBlk = tree->AsDynBlk();
                fgDebugCheckFlags(dynBlk->gtDynamicSize);
                chkFlags |= (dynBlk->gtDynamicSize->gtFlags & GTF_ALL_EFFECT);
                fgDebugCheckFlags(dynBlk->Addr());
                chkFlags |= (dynBlk->Addr()->gtFlags & GTF_ALL_EFFECT);
                if (tree->OperGet() == GT_STORE_DYN_BLK)
                {
                    fgDebugCheckFlags(dynBlk->Data());
                    chkFlags |= (dynBlk->Data()->gtFlags & GTF_ALL_EFFECT);
                }
                break;

            default:

#ifdef DEBUG
                gtDispTree(tree);
#endif

                assert(!"Unknown operator for fgDebugCheckFlags");
                break;
        }
    }

    fgDebugCheckFlagsHelper(tree, treeFlags, chkFlags);
}

//------------------------------------------------------------------------------
// fgDebugCheckDispFlags:
//    Wrapper function that displays two GTF_IND_ flags
//      and then calls ftDispFlags to display the rest.
//
// Arguments:
//    tree       - Tree whose flags are being checked
//    dispFlags  - the first argument for gtDispFlags
//                 ands hold GTF_IND_INVARIANT and GTF_IND_NONFLUALTING
//    debugFlags - the second argument to gtDispFlags
//
void Compiler::fgDebugCheckDispFlags(GenTree* tree, unsigned dispFlags, unsigned debugFlags)
{
    if (tree->OperGet() == GT_IND)
    {
        printf("%c", (dispFlags & GTF_IND_INVARIANT) ? '#' : '-');
        printf("%c", (dispFlags & GTF_IND_NONFAULTING) ? 'n' : '-');
    }
    GenTree::gtDispFlags(dispFlags, debugFlags);
}

//------------------------------------------------------------------------------
// fgDebugCheckFlagsHelper : Check if all bits that are set in chkFlags are also set in treeFlags.
//
//
// Arguments:
//    tree  - Tree whose flags are being checked
//    treeFlags - Actual flags on the tree
//    chkFlags - Expected flags
//
// Note:
//    Checking that all bits that are set in treeFlags are also set in chkFlags is currently disabled.

void Compiler::fgDebugCheckFlagsHelper(GenTree* tree, unsigned treeFlags, unsigned chkFlags)
{
    if (chkFlags & ~treeFlags)
    {
        // Print the tree so we can see it in the log.
        printf("Missing flags on tree [%06d]: ", dspTreeID(tree));
        Compiler::fgDebugCheckDispFlags(tree, chkFlags & ~treeFlags, GTF_DEBUG_NONE);
        printf("\n");
        gtDispTree(tree);

        noway_assert(!"Missing flags on tree");

        // Print the tree again so we can see it right after we hook up the debugger.
        printf("Missing flags on tree [%06d]: ", dspTreeID(tree));
        Compiler::fgDebugCheckDispFlags(tree, chkFlags & ~treeFlags, GTF_DEBUG_NONE);
        printf("\n");
        gtDispTree(tree);
    }
    else if (treeFlags & ~chkFlags)
    {
        // We can't/don't consider these flags (GTF_GLOB_REF or GTF_ORDER_SIDEEFF) as being "extra" flags
        //
        unsigned flagsToCheck = ~GTF_GLOB_REF & ~GTF_ORDER_SIDEEFF;

        if ((treeFlags & ~chkFlags & flagsToCheck) != 0)
        {
            // Print the tree so we can see it in the log.
            printf("Extra flags on tree [%06d]: ", dspTreeID(tree));
            Compiler::fgDebugCheckDispFlags(tree, treeFlags & ~chkFlags, GTF_DEBUG_NONE);
            printf("\n");
            gtDispTree(tree);

            noway_assert(!"Extra flags on tree");

            // Print the tree again so we can see it right after we hook up the debugger.
            printf("Extra flags on tree [%06d]: ", dspTreeID(tree));
            Compiler::fgDebugCheckDispFlags(tree, treeFlags & ~chkFlags, GTF_DEBUG_NONE);
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

    assert(fgStmtListThreaded);

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
    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
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
    for (Statement* stmt : block->Statements())
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

        fgDebugCheckFlags(stmt->GetRootNode());

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
        if (fgStmtListThreaded)
        {
            fgDebugCheckNodeLinks(block, stmt);
        }
    }
}

// ensure that bbNext and bbPrev are consistent
void Compiler::fgDebugCheckBlockLinks()
{
    assert(fgFirstBB->bbPrev == nullptr);

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
    {
        if (block->bbNext)
        {
            assert(block->bbNext->bbPrev == block);
        }
        else
        {
            assert(block == fgLastBB);
        }

        if (block->bbPrev)
        {
            assert(block->bbPrev->bbNext == block);
        }
        else
        {
            assert(block == fgFirstBB);
        }

        // If this is a switch, check that the tables are consistent.
        // Note that we don't call GetSwitchDescMap(), because it has the side-effect
        // of allocating it if it is not present.
        if (block->bbJumpKind == BBJ_SWITCH && m_switchDescMap != nullptr)
        {
            SwitchUniqueSuccSet uniqueSuccSet;
            if (m_switchDescMap->Lookup(block, &uniqueSuccSet))
            {
                // Create a set with all the successors. Don't use BlockSet, so we don't need to worry
                // about the BlockSet epoch.
                BitVecTraits bitVecTraits(fgBBNumMax + 1, this);
                BitVec       succBlocks(BitVecOps::MakeEmpty(&bitVecTraits));
                BasicBlock** jumpTable = block->bbJumpSwt->bbsDstTab;
                unsigned     jumpCount = block->bbJumpSwt->bbsCount;
                for (unsigned i = 0; i < jumpCount; i++)
                {
                    BitVecOps::AddElemD(&bitVecTraits, succBlocks, jumpTable[i]->bbNum);
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

// UniquenessCheckWalker keeps data that is neccesary to check
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

    for (BasicBlock* block = fgFirstBB; block != nullptr; block = block->bbNext)
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
            for (Statement* stmt : block->Statements())
            {
                GenTree* root = stmt->GetRootNode();
                fgWalkTreePre(&root, UniquenessCheckWalker::MarkTreeId, &walker);
            }
        }
    }
}

/*****************************************************************************/
#endif // DEBUG
