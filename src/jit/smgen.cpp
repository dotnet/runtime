// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XX                                                                           XX
XX                 Code sequence state machine generator                     XX
XX                                                                           XX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
*/

#include "smgen.h"
#include "smcommon.cpp"

static bool debug = false;

#define debugprint(x) if (debug) printf x;

//
// State machine generator
//

SMGen::SMGen()
{      
    memset(this, 0, sizeof(*this));
    
    States[SM_STATE_ID_START].id = SM_STATE_ID_START;  
    SMGenDestStateDesc * pDestStateListHead = new SMGenDestStateDesc();                    
    pDestStateListHead->next                = NULL;
    States[SM_STATE_ID_START].destStateList = pDestStateListHead;             

    lastStateID = 1;       

    //
    // Process all interesting code sequences       
    //
    
    debugprint(("\n======== The code sequences =================\n"));

    SM_OPCODE * CodeSeqs = (SM_OPCODE *)s_CodeSeqs;
    
    while (*CodeSeqs != CODE_SEQUENCE_END) 
    {
        ProcessSeq(CodeSeqs);  
        CodeSeqs += MAX_CODE_SEQUENCE_LENGTH;
    } 
   
    debugprint(("\n======== The state machine =================\n"));

    for (SM_STATE_ID i=1; i<=lastStateID; ++i)
    {    
        debugprint(("State %-4d : length=%-2d prev=%-4d lngest=%-4d  (%c) Desc=[%s]\n", 
               i, States[i].length, States[i].prevState, States[i].longestTermState, 
               States[i].term?'*':' ', StateDesc(i)));      

        for (unsigned j=0; j<SM_COUNT; ++j)
        {  
            if (States[i].getDestState((SM_OPCODE)j) != 0)
            {
                debugprint(("    [%s] ==> %d\n", 
                       smOpcodeNames[(SM_OPCODE)j],
                       States[i].getDestState((SM_OPCODE)j)));
            }
        }
    }  

    debugprint(("\n# MethodName| NativeSize| ILBytes| ILInstrCount| 0Args| 1Args| 2Args| 3AndMoreArgs| NoLocals| NoCalls"));

    unsigned termStateCount = 0;
    
    for (SM_STATE_ID i=1; i<=lastStateID; ++i)
    {    
        if (States[i].term)
        {
            ++termStateCount;
            debugprint(("| %s[%d]", StateDesc(i), i));
        }
    }

    debugprint(("\n\n%d termination states.\n", termStateCount));         
}

SMGen::~SMGen()
{           
}

void  SMGen::ProcessSeq(SM_OPCODE * CodeSeq)
{
    SM_STATE_ID longestTermState = 0;
    
    SM_STATE_ID curState = SM_STATE_ID_START;
    BYTE        curLen = 0;         
        
    SM_OPCODE * pOpcode = CodeSeq;
    SM_OPCODE   opcode;

    debugprint(("\nCodeSeq : {"));

    do 
    {
        opcode = * pOpcode;
              
        debugprint(("%s, " , smOpcodeNames[opcode]));

        assert(curLen < MAX_CODE_SEQUENCE_LENGTH);
        assert(curLen < 255);

        ++curLen;

        SM_STATE_ID nextState = States[curState].getDestState(opcode);        
        if (nextState == 0)
        {
            // Need to create a new state
            assert(lastStateID < MAX_NUM_STATES);
            ++lastStateID;
            
            States[curState].setDestState(opcode, lastStateID);

            States[lastStateID].id               = lastStateID; 
            States[lastStateID].longestTermState = longestTermState;
            States[lastStateID].prevState        = curState;   
            States[lastStateID].opc              = opcode;
            States[lastStateID].term             = false;

            SMGenDestStateDesc * pDestStateListHead = new SMGenDestStateDesc();                    
            pDestStateListHead->next                = NULL;
            States[lastStateID].destStateList       = pDestStateListHead;             
                                                    
            curState = lastStateID;
            States[curState].length = curLen; 
        } 
        else
        {
            curState = nextState;
            if (States[curState].term)
            {
                longestTermState = curState;
            }
        }        
    }        
    while (* (++pOpcode) != CODE_SEQUENCE_END);

    assert(curState != SM_STATE_ID_START);
    assert(!States[curState].term && "Duplicated rule.");
    
    States[curState].term = true;

    debugprint(("    }\n"));  
}

void  SMGen::Emit()
{    
    // Zero out the entire buffer.
    memset(&emitBuffer, 0, sizeof(emitBuffer));      

    BYTE * pBuffer = (BYTE *)&emitBuffer;       
    pAllStates = (SMState *) pBuffer; 
    
    pBuffer += sizeof(*pAllStates) * (lastStateID+1);    
    pAllJumpTables = (JumpTableCell *) pBuffer;    

    pJumpTableMax  = 0;
        
    //
    // Loop through each state and fill in the buffer
    //
    for (SM_STATE_ID i=1; i<=lastStateID; ++i)
    {    
        SMState *  pState = pAllStates+i;           

        pState->term             = States[i].term;
        pState->length           = States[i].length;
        pState->longestTermState = States[i].longestTermState;
        pState->prevState        = States[i].prevState;
        pState->opc              = States[i].opc;        
        pState->jumpTableByteOffset  = JumpTableSet(i);

    }

    debugprint(("pJumpTableMax at starts at cell# %d\n", pJumpTableMax-pAllJumpTables));

    totalCellNeeded = pJumpTableMax - pAllJumpTables + SM_COUNT;

    debugprint(("MAX_NUM_STATES = %d\n", MAX_NUM_STATES));
    debugprint(("Actual total number of states = %d\n", lastStateID+1));
    assert(lastStateID+1 <= MAX_NUM_STATES);
    
    debugprint(("Total number of cells  = %d\n", totalCellNeeded));
    assert(totalCellNeeded <= MAX_CELL_COUNT);        

    debugprint(("sizeof(SMState) = %d\n", sizeof(SMState)));   
    debugprint(("sizeof(JumpTableCell) = %d\n", sizeof(JumpTableCell)));   
    debugprint(("sizeof(emitBuffer) = %d\n", sizeof(emitBuffer)));   

    EmitDone();
}

void  SMGen::EmitDone()
{       
    printf("// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n"); 
    printf("//\n"); 
    printf("//   Automatically generated code. DO NOT MODIFY! \n"); 
    printf("//   To generate this file. Do \"smgen.exe > SMData.cpp\" \n");         
    printf("//\n"); 
    printf("// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n\n"); 

    printf("#include \"jitpch.h\"\n");
    
    printf("//\n");
    printf("// States in the state machine\n");
    printf("//\n");
            
    printf("const SMState g_SMStates[] = \n{\n");

    printf(" // {term, len, lng, prev, SMOpcode and SMOpcodeName           , offsets  }           //  state ID and name\n"); 

    // Print out states
    for (SM_STATE_ID id=0; id<=lastStateID; ++id)
    {    
        SMState *  pState = pAllStates+id;
        printf("    {");
        printf("%4d, ",  pState->term);
        printf("%3d, ",  pState->length);
        printf("%3d, ",  pState->longestTermState);
        printf("%4d, ",  pState->prevState);
        printf("(SM_OPCODE)%3d /* %-15s */, ",  pState->opc, smOpcodeNames[pState->opc]);
        printf("%7d" ,  pState->jumpTableByteOffset);        
        
        printf("  },          //  ");
        printf("state %d [%s]", id,  StateDesc(id));   

        printf("\n");
    }

    printf("};\n\n");

    printf("static_assert_no_msg(NUM_SM_STATES == sizeof(g_SMStates)/sizeof(g_SMStates[0]));\n\n");

    printf("const SMState * gp_SMStates = g_SMStates;\n\n");

    printf("//\n");
    printf("// JumpTableCells in the state machine\n");
    printf("//\n");
  
    printf("const JumpTableCell g_SMJumpTableCells[] = \n{\n");
    printf(" // {src, dest  }\n");

    for (unsigned i=0; i<totalCellNeeded; ++i)
    {
        JumpTableCell * cell = pAllJumpTables+i;

        printf("    {");
        printf("%3d, ", cell->srcState);
        printf("%4d",  cell->destState);
        printf("  },   // cell# %d", i);

        if (cell->srcState != 0)
        {        
            JumpTableCell * pThisJumpTable = 
                (JumpTableCell * )(((PBYTE)pAllJumpTables) + pAllStates[cell->srcState].jumpTableByteOffset);

            assert(cell >= pThisJumpTable);

            SM_OPCODE opcode = (SM_OPCODE) (cell - pThisJumpTable);
               
            printf(" : state %d [%s]", cell->srcState, StateDesc(cell->srcState));
       
            printf(" --(%d %s)--> ", opcode, smOpcodeNames[opcode]);

            printf("state %d [%s]", cell->destState, StateDesc(cell->destState));
        }
        printf("\n");
     }
    
    printf("};\n\n");

    printf("const JumpTableCell * gp_SMJumpTableCells = g_SMJumpTableCells;\n\n");
}


unsigned short  SMGen::JumpTableSet(SM_STATE_ID id)
{    
    JumpTableCell * pThisJumpTable = pAllJumpTables;

    while (true)
    {
        if (bJumpTableFit(pThisJumpTable, id))
        {
            JumpTableFill(pThisJumpTable, id);

            if (pThisJumpTable > pJumpTableMax)
                pJumpTableMax = pThisJumpTable;

            unsigned offset = ((BYTE*)pThisJumpTable) - ((BYTE*)pAllJumpTables);

            assert(offset == (unsigned short)offset);
             
            return (unsigned short)offset;
        }
        
        ++pThisJumpTable;
    }
}

bool   SMGen::bJumpTableFit(JumpTableCell * pTable, SM_STATE_ID id)
{
    SMGenDestStateDesc * p = States[id].destStateList->next; // Skip the list head.
         
    while (p)
    {
        SM_OPCODE opcode = p->opcode;      
        JumpTableCell * pCell = pTable + opcode;
        if (pCell->srcState != 0)
        {
            // This cell has been occupied.
            return false;
        }
        
        p = p->next;        
    }

    debugprint(("JumpTable for state %d [%s] starts at cell# %d:\n", 
               id, 
               StateDesc(id),
               pTable-pAllJumpTables));
    return true;    
}

void   SMGen::JumpTableFill(JumpTableCell * pTable, SM_STATE_ID id)
{
    SMGenDestStateDesc * p = States[id].destStateList->next;  // Skip the list head.
         
    while (p)
    {
        SM_OPCODE opcode = p->opcode;      
        JumpTableCell * pCell = pTable + opcode;        
        assert(pCell->srcState == 0);
        
        pCell->srcState  = id;
        pCell->destState = p->destState;

        debugprint(("    cell# %d : [%s (%d)] --> %d\n", 
                   pCell-pAllJumpTables,
                   smOpcodeNames[opcode],
                   opcode,
                   pCell->destState));

        p = p->next;  
    }
}

char * SMGen::StateDesc(SM_STATE_ID stateID)
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


SM_STATE_ID  SMGenState::getDestState(SM_OPCODE opcode)
{            
    assert(opcode < SM_COUNT);
    
    SMGenDestStateDesc * p = destStateList->next;  // Skip the list head.  
    int lastSeenOpcode = -1;
    
    while (p)
    {
       assert(lastSeenOpcode < p->opcode); // opcode should be in accending order.
       lastSeenOpcode = p->opcode; 
       if (p->opcode == opcode)
       {
           assert(p->destState != 0);
           return p->destState;
       }
       p = p->next;
    }
   
    return 0;    
}

void  SMGenState::setDestState(SM_OPCODE opcode, SM_STATE_ID destState)
{
    assert(id != 0);  // Should not have come here for the invalid state.       

    assert(getDestState(opcode) == 0); // Don't set it twice.

    SMGenDestStateDesc * newSMGenDestStateDesc = new SMGenDestStateDesc();

    newSMGenDestStateDesc->opcode    = opcode;
    newSMGenDestStateDesc->destState = destState;


    // Insert the new entry in accending order

    SMGenDestStateDesc * p = destStateList;  
    SMGenDestStateDesc * q = p->next;

    while (q)
    {
        if (q->opcode > opcode)
            break;
        
        p = q;
        q = q->next;
    }  

    newSMGenDestStateDesc->next = p->next;    
    p->next                     = newSMGenDestStateDesc;    
}

void Usage()
{   
    printf("JIT code sequence state machine generator\n");
    printf("==============================================\n");
    printf("smgen -?   : Print usage.\n");
    printf("smgen      : Generate static array for the state machine.\n");
    printf("smgen debug: Print debug info.\n");
}


//-----------------------------------------------------------------------------
// main
//-----------------------------------------------------------------------------
extern "C" int _cdecl wmain(int argc, __in_ecount(argc) WCHAR **argv)
{   
    if (argc > 1)
    {
        if (wcscmp(argv[1], W("-?")) == 0  || wcscmp(argv[1], W("/?")) == 0)
        {
            Usage();
            return 1;
        }
        
        if (wcscmp(argv[1], W("debug")) == 0)
        {
            debug = true;
        }        
    }

    // Generate the state machine
    SMGen smGen;
    smGen.Emit();

    
    
    
    return 0;
}


