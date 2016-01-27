// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                 State machine used in the JIT                             XX
XX To take samples, do                                                       XX
XX   set complus_JitLRSampling=1                                             XX
XX   set complus_ngenlocalworker=1                                           XX
XX   ngen install mscorlib /nologo /silent /NoDependencies                   XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "jitpch.h"
#ifdef _MSC_VER
#pragma hdrstop
#endif

#include "smcommon.cpp"

#ifndef FEATURE_CORECLR  // ???? Is this right?
#undef printf  // We don't want to use logf(). Just print out to the stdout. That simple!
#endif // FEATURE_CORECLR

#ifdef DEBUG
static LONG g_SMTested = 0;
bool g_HeaderPrinted = false;
#endif // DEBUG

//
// The array to map from EE opcodes (i.e. CEE_ ) to state machine opcodes (i.e. SM_ )
//
const SM_OPCODE smOpcodeMap[] =
{
    #define OPCODEMAP(eename,eestring,smname) smname,
    #include "smopcodemap.def"
    #undef  OPCODEMAP
};


// ????????? How to make this method inlinable, since it refers to smOpcodeMap????
/* static */ SM_OPCODE    CodeSeqSM::MapToSMOpcode(OPCODE opcode)
{
    assert(opcode < CEE_COUNT);

    SM_OPCODE smOpcode = smOpcodeMap[opcode];
    assert(smOpcode < SM_COUNT);
    return smOpcode;
}

void CodeSeqSM::Start(Compiler * comp) 
{    
    pComp             = comp;
    States            = gp_SMStates;
    JumpTableCells    = gp_SMJumpTableCells;
    StateWeights      = gp_StateWeights;
    NativeSize        = 0;

#ifdef DEBUG 
    if (!Compiler::s_compInSamplingMode  && // No need to test in the sampling mode.
        InterlockedExchange(&g_SMTested, 1) == 0) 
    {        
        Test();
    }

    if (Compiler::s_compInSamplingMode)
    {
        if (!g_HeaderPrinted)
        {
            PrintSampleHeader();
            g_HeaderPrinted = true;
        }
    }
#endif 

    Reset();  

}

void CodeSeqSM::Reset() 
{
    curState      = SM_STATE_ID_START;

#ifdef DEBUG
    // Reset the state occurence counts
    memset(StateMatchedCounts, 0, sizeof(StateMatchedCounts));    
   
    b0Args        = 
    b1Args        =
    b2Args        =
    b3AndMoreArgs = 
    bNoLocals     = false;
    
    bNoCalls      = true;

    instrCount    = 0;    
#endif // DEBUG
}

void CodeSeqSM::End() 
{
    if (States[curState].term)
    {
        TermStateMatch(curState DEBUGARG(pComp->verbose));
    }

#ifdef DEBUG
    if (pComp->info.compILargsCount == 0)
        b0Args = true;
    else if (pComp->info.compILargsCount == 1)
        b1Args = true;        
    else if (pComp->info.compILargsCount == 2)
        b2Args = true;
    else 
        b3AndMoreArgs = true;
     
    bNoLocals = (pComp->info.compMethodInfo->locals.numArgs == 0); 
#endif // DEBUG
}


void  CodeSeqSM::Run(SM_OPCODE opcode DEBUGARG(int level))
{    
    SM_STATE_ID nextState;
    SM_STATE_ID rollbackState;

    SM_OPCODE   opcodesToRevisit[MAX_CODE_SEQUENCE_LENGTH];

    assert(level<=MAX_CODE_SEQUENCE_LENGTH);

#ifdef DEBUG
    if (opcode == SM_CALL     ||
        opcode == SM_CALLVIRT ||
        opcode == SM_CALLI
       )
    {
        bNoCalls = false;
    }
#endif // DEBUG    

_Next:
    nextState = GetDestState(curState, opcode);    

    if (nextState != 0)
    {
        // This is easy, Just go to the next state.
        curState = nextState;
        return;            
    }

    assert(curState != SM_STATE_ID_START); 
        
    if (States[curState].term)
    {
        TermStateMatch(curState DEBUGARG(pComp->verbose));
        curState = SM_STATE_ID_START;
        goto _Next;
    }
        
    // This is hard. We need to rollback to the longest matched term state and restart from there.

    rollbackState = States[curState].longestTermState;
    TermStateMatch(rollbackState DEBUGARG(pComp->verbose));

    assert(States[curState].length > States[rollbackState].length);
     
    unsigned numOfOpcodesToRevisit = States[curState].length - States[rollbackState].length + 1;
    assert(numOfOpcodesToRevisit > 1   &&
           numOfOpcodesToRevisit <= MAX_CODE_SEQUENCE_LENGTH); // So it can fit in the local array opcodesToRevisit[]

    SM_OPCODE * p = opcodesToRevisit + (numOfOpcodesToRevisit - 1);

    *p = opcode;

    // Fill in the local array:
    for (unsigned i = 0; i<numOfOpcodesToRevisit-1; ++i) 
    {
        * (--p) = States[curState].opc;
        curState = States[curState].prevState;
    }

    assert(curState == rollbackState);    

    // Now revisit these opcodes, starting from SM_STATE_ID_START.
    curState = SM_STATE_ID_START;
    for (p = opcodesToRevisit; p< opcodesToRevisit + numOfOpcodesToRevisit; ++p)
    {
        Run(*p DEBUGARG(level+1));
    }    
}


SM_STATE_ID  CodeSeqSM::GetDestState(SM_STATE_ID srcState, SM_OPCODE opcode)
{
    assert(opcode < SM_COUNT);
         
    JumpTableCell * pThisJumpTable = (JumpTableCell * )(((PBYTE)JumpTableCells) + States[srcState].jumpTableByteOffset);

    JumpTableCell * cell = pThisJumpTable+opcode;

    if (cell->srcState != srcState)
    {
        assert(cell->srcState == 0 || cell->srcState != srcState); // Either way means there is not outgoing edge from srcState.
        return 0;
    }
    else
    {
        return cell->destState; 
    }
}

#ifdef DEBUG

const char * CodeSeqSM::StateDesc(SM_STATE_ID stateID)
{    
    static char      s_StateDesc[500];
    static SM_OPCODE s_StateDescOpcodes[MAX_CODE_SEQUENCE_LENGTH];

    if (stateID == 0)
        return "invalid";
    
    if (stateID == SM_STATE_ID_START)
        return "start";    

    unsigned i = 0;
    
    SM_STATE_ID b = stateID; 
 
    while (States[b].prevState != 0)
    {
        s_StateDescOpcodes[i] = States[b].opc;
        b = States[b].prevState;
        ++i;
    }

    assert(i == States[stateID].length && i>0);

    * s_StateDesc = 0;
    
    while (--i>0)
    {
        strcat(s_StateDesc, smOpcodeNames[s_StateDescOpcodes[i]]);
        strcat(s_StateDesc, " -> ");
    }

    strcat(s_StateDesc, smOpcodeNames[s_StateDescOpcodes[0]]); 

    return s_StateDesc;
}


void CodeSeqSM::PrintSampleHeader() 
{        
    // Output the NUM_SM_STATES here for the linear regression tool to generate the weight array with this size.    
    printf("# MethodName{NUM_SM_STATES=%d}| NativeSize| ILBytes| ILInstrCount| 0Args| 1Args| 2Args| 3AndMoreArgs| NoLocals| NoCalls", NUM_SM_STATES);

    for (BYTE i=1; i<NUM_SM_STATES; ++i)
    {    
        if (States[i].term)
        {    
            printf("| %s[%d]", StateDesc(i), i);
        }
    }
    printf("\n");  
}

void CodeSeqSM::PrintSampleResult() 
{        
    printf("%s| %d| %d| %d",       
           pComp->info.compFullName,   
           BBCodeSize,                 // NativeSize
           pComp->info.compILCodeSize, // ILBytes
           instrCount);                // ILInstrCount
    
    printf("| %d| %d| %d| %d| %d| %d", b0Args, b1Args, b2Args, b3AndMoreArgs, bNoLocals, bNoCalls);
        
    for (unsigned i=1; i<NUM_SM_STATES; ++i)
    {    
        if (States[i].term)
        {    
            printf("| %d [%3d]", StateMatchedCounts[i], i);
        }
    }
    printf("\n");    
}

int  s_TermStateReachedCounts[NUM_SM_STATES];  // How many times have we reached these termination states?

//
// Test the state machine to make sure it does recognize the interesting code sequences.
//
void CodeSeqSM::Test() 
{                         
    memset(s_TermStateReachedCounts, 0, sizeof(s_TermStateReachedCounts));

    //
    // Process all interesting code sequences       
    //
    
    SM_OPCODE * CodeSeqs = (SM_OPCODE *)s_CodeSeqs;
    
    while (*CodeSeqs != CODE_SEQUENCE_END) 
    {        
        TestSeq(CodeSeqs);  
        CodeSeqs += MAX_CODE_SEQUENCE_LENGTH;
    } 

    // Now make sure we have ended at each termination state.
    for (unsigned i=0; i<NUM_SM_STATES; ++i)
    {    
        if (States[i].term)
        {    
            assert(s_TermStateReachedCounts[i] == 1);           
        }
        else
        {
            assert(s_TermStateReachedCounts[i] == 0);
        }
    }
}

void CodeSeqSM::TestSeq(SM_OPCODE * CodeSeq) 
{      
    // Reset all the counters.
    Reset();
         
    while (*CodeSeq != CODE_SEQUENCE_END) 
    {                                   
        Run(*CodeSeq++ DEBUGARG(0));     
    }          

    // Make sure we end at a termination state. 
    assert(States[curState].term);

    s_TermStateReachedCounts[curState]++;
}

#endif // DEBUG

