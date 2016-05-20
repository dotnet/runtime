// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// ==++==
// 
 
// 
// ==--==
/****************************************************************************
* STRIKE.C                                                                  *
*   Routines for the NTSD extension - STRIKE                                *
*                                                                           *
* History:                                                                  *
*   09/07/99  Microsoft   Created                                           *
*                                                                           *
*                                                                           *
\***************************************************************************/
#include <windows.h>
#include <winternl.h>
#include <winver.h>
#include <wchar.h>

#define NOEXTAPI
#define KDEXT_64BIT
#include <wdbgexts.h>
#undef DECLARE_API
#undef StackTrace

#include <stdio.h>
#include <stdlib.h>
#include <stdint.h>
#include <string.h>
#include <stddef.h>

#include "strike.h"
// We need to define the target address type.  This will be used in the 
// functions that read directly from the debuggee address space, vs. using 
// the DAC tgo read the DAC-ized data structures.
#include "daccess.h"
//#include "dbgeng.h"


#ifndef STRESS_LOG
#define STRESS_LOG
#endif // STRESS_LOG
#define STRESS_LOG_READONLY
#include "stresslog.h"
#include <dbghelp.h>

#include "corhdr.h"
#include "dacprivate.h"

#define  CORHANDLE_MASK 0x1
#define DEFINE_EXT_GLOBALS

#include "util.h"

#ifndef _ASSERTE
#ifdef _DEBUG
#define _ASSERTE(expr) 		\
        do { if (!(expr) ) { ExtOut(#expr); DebugBreak(); } } while (0)
#else // _DEBUG
#define _ASSERTE(expr)
#endif // _DEBUG else
#endif // !_ASSERTE

#ifdef _MSC_VER
#pragma warning(disable:4244)   // conversion from 'unsigned int' to 'unsigned short', possible loss of data
#pragma warning(disable:4189)   // local variable is initialized but not referenced
#endif // _MSC_VER

struct PlugRecord
{
    PlugRecord *next;
    
    size_t PlugStart;
    size_t PlugEnd;
    size_t Delta;

    PlugRecord() { ZeroMemory(this,sizeof(PlugRecord)); }
};

struct PromoteRecord
{
    PromoteRecord *next;
    
    size_t Root;
    size_t Value;
    size_t methodTable;

    PromoteRecord() { ZeroMemory(this,sizeof(PromoteRecord)); }
};

struct RelocRecord
{
    RelocRecord *next;
    
    size_t Root;
    size_t PrevValue;
    size_t NewValue;
    size_t methodTable;

    RelocRecord() { ZeroMemory(this,sizeof(RelocRecord)); }
};

struct GCRecord
{
    ULONG64 GCCount;
    
    // BOOL IsComplete() { return bFinished && bHaveStart; }
    
    PlugRecord *PlugList;
    RelocRecord *RelocList;
    PromoteRecord *PromoteList;

    void AddPlug(PlugRecord& p) { 
        PlugRecord *pTmp = PlugList;
        PlugList = new PlugRecord(p);
        PlugList->next = pTmp;
    }

    void AddReloc(RelocRecord& r) {
        RelocRecord *pTmp = RelocList;
        RelocList = new RelocRecord(r);
        RelocList->next = pTmp;
    }

    void AddPromote(PromoteRecord& r) {
        PromoteRecord *pTmp = PromoteList;
        PromoteList = new PromoteRecord(r);
        PromoteList->next = pTmp;
    }

    UINT PlugCount() {
        UINT ret = 0;
        PlugRecord *Iter = PlugList;
        while (Iter) {
            Iter = Iter->next;
            ret++;
        }
        return ret;
    }

    UINT RelocCount() {
        UINT ret = 0;
        RelocRecord *Iter = RelocList;
        while (Iter) {
            Iter = Iter->next;
            ret++;
        }
        return ret;
    }

    UINT PromoteCount() {
        UINT ret = 0;
        PromoteRecord *Iter = PromoteList;
        while (Iter) {
            Iter = Iter->next;
            ret++;
        }
        return ret;
    }
    
    void Clear() {

        PlugRecord *pTrav = PlugList;
        while (pTrav) {
            PlugRecord *pTmp = pTrav->next;
            delete pTrav;
            pTrav = pTmp;
        }

        RelocRecord *pTravR = RelocList;
        while (pTravR) {
            RelocRecord *pTmp = pTravR->next;
            delete pTravR;
            pTravR = pTmp;
        }

        PromoteRecord *pTravP = PromoteList;
        while (pTravP) {
            PromoteRecord *pTmp = pTravP->next;
            delete pTravP;
            pTravP = pTmp;
        }

        ZeroMemory(this,sizeof(GCRecord));
    }        
        
};

#define MAX_GCRECORDS 500
UINT g_recordCount = 0;
GCRecord g_records[MAX_GCRECORDS];

void GcHistClear()
{
    for (UINT i=0; i < g_recordCount; i++)
    {
        g_records[i].Clear();
    }
    g_recordCount = 0;    
}

void GcHistAddLog(LPCSTR msg, StressMsg* stressMsg)
{
    if (g_recordCount >= MAX_GCRECORDS)
    {
        return;
    }
    
    if (strcmp(msg, ThreadStressLog::gcPlugMoveMsg()) == 0)
    {
        PlugRecord pr;
        // this is a plug message
        _ASSERTE(stressMsg->numberOfArgs == 3);
        pr.PlugStart = (size_t) stressMsg->args[0];
        pr.PlugEnd = (size_t) stressMsg->args[1];
        pr.Delta = (size_t) stressMsg->args[2];

        g_records[g_recordCount].AddPlug(pr);
    }
    else if (strcmp(msg, ThreadStressLog::gcRootMsg()) == 0)
    {
        // this is a root message
        _ASSERTE(stressMsg->numberOfArgs == 4);
        RelocRecord rr;
        rr.Root = (size_t) stressMsg->args[0];
        rr.PrevValue = (size_t) stressMsg->args[1];
        rr.NewValue = (size_t) stressMsg->args[2];
        rr.methodTable = (size_t) stressMsg->args[3];
        g_records[g_recordCount].AddReloc(rr);        
    }
    else if (strcmp(msg, ThreadStressLog::gcRootPromoteMsg()) == 0)
    {
        // this is a promote message
        _ASSERTE(stressMsg->numberOfArgs == 3);
        PromoteRecord pr;
        pr.Root = (size_t) stressMsg->args[0];
        pr.Value = (size_t) stressMsg->args[1];
        pr.methodTable = (size_t) stressMsg->args[2];
        g_records[g_recordCount].AddPromote(pr);
    }
    else if (strcmp(msg, ThreadStressLog::gcStartMsg()) == 0)
    {
        // Gc start!
        _ASSERTE(stressMsg->numberOfArgs == 3);
        ULONG64 gc_count = (ULONG64) stressMsg->args[0];
        g_records[g_recordCount].GCCount = gc_count;
        g_recordCount++;
    }
    else if (strcmp(msg, ThreadStressLog::gcEndMsg()) == 0)
    {
        // Gc end!
        // ULONG64 gc_count = (ULONG64) stressMsg->data;
        // ExtOut ("ENDGC %d\n", gc_count);
    }
}

DECLARE_API(HistStats)
{
    INIT_API();
    
    ExtOut ("%8s %8s %8s\n",
        "GCCount", "Promotes", "Relocs");
    ExtOut ("-----------------------------------\n");
    
    // Just traverse the data structure, printing basic stats
    for (UINT i=0; i < g_recordCount; i++)
    {
        UINT PromoteCount = g_records[i].PromoteCount();
        UINT RelocCount = g_records[i].RelocCount();
        UINT GCCount = (UINT) g_records[i].GCCount;

        ExtOut ("%8d %8d %8d\n",
            GCCount,
            PromoteCount,
            RelocCount);
    }

    BOOL bErrorFound = FALSE;
    
    // Check for duplicate Reloc or Promote messages within one gc.
    // Method is very inefficient, improve it later.
    for (UINT i=0; i < g_recordCount; i++)
    {       
        {   // Promotes
            PromoteRecord *Iter = g_records[i].PromoteList;
            UINT GCCount = (UINT) g_records[i].GCCount;
            while (Iter) 
            {
                PromoteRecord *innerIter = Iter->next;
                while (innerIter)
                {
                    if (Iter->Root == innerIter->Root)
                    {
                        ExtOut ("Root %p promoted multiple times in gc %d\n",
                            SOS_PTR(Iter->Root),
                            GCCount);
                        bErrorFound = TRUE;
                    }
                    innerIter = innerIter->next;
                }
                
                Iter = Iter->next;            
            }
        }

        {   // Relocates
            RelocRecord *Iter = g_records[i].RelocList;
            UINT GCCount = (UINT) g_records[i].GCCount;
            while (Iter) 
            {
                RelocRecord *innerIter = Iter->next;
                while (innerIter)
                {
                    if (Iter->Root == innerIter->Root)
                    {
                        ExtOut ("Root %p relocated multiple times in gc %d\n",
                            SOS_PTR(Iter->Root),
                            GCCount);
                        bErrorFound = TRUE;
                    }
                    innerIter = innerIter->next;
                }
                
                Iter = Iter->next;            
            }
        }        
    }

    if (!bErrorFound)
    {
        ExtOut ("No duplicate promote or relocate messages found in the log.\n");
    }
    
    return Status;
}

DECLARE_API(HistRoot)
{
    INIT_API();
    size_t nArg;

    StringHolder rootstr;
    CMDValue arg[] = 
    {
        // vptr, type
        {&rootstr.data, COSTRING},
    };

    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
        return Status;

    if (nArg != 1)
    {
        ExtOut ("!Root <valid object pointer>\n");
        return Status;
    }

    size_t Root = (size_t) GetExpression(rootstr.data);
    
    ExtOut ("%8s %" POINTERSIZE "s %" POINTERSIZE "s %9s %20s\n",
        "GCCount", "Value", "MT", "Promoted?", "Notes");
    ExtOut ("---------------------------------------------------------\n");

    bool bBoringPeople = false;
    
    // Just traverse the data structure, printing basic stats
    for (UINT i=0; i < g_recordCount; i++)
    {
        UINT GCCount = (UINT) g_records[i].GCCount;

        // Find promotion records...there should only be one.
        PromoteRecord *pPtr = g_records[i].PromoteList;
        PromoteRecord *pPromoteRec = NULL;
        bool bPromotedMoreThanOnce = false;
        while(pPtr)
        {
            if (pPtr->Root == Root)
            {
                if (pPromoteRec)
                {
                    bPromotedMoreThanOnce = true;
                }
                else
                {
                    pPromoteRec = pPtr;
                }
            }
            pPtr = pPtr->next;
        }

        RelocRecord *pReloc = g_records[i].RelocList;
        RelocRecord *pRelocRec = NULL;
        bool bRelocatedMoreThanOnce = false;
        while(pReloc)
        {
            if (pReloc->Root == Root)
            {
                if (pRelocRec)
                {
                    bRelocatedMoreThanOnce = true;
                }
                else
                {
                    pRelocRec = pReloc;
                }
            }
            pReloc = pReloc->next;
        }

        // Validate the records found for this root.
        if (pRelocRec != NULL)
        {
            bBoringPeople = false;
            
            ExtOut ("%8d %p %p %9s ", GCCount,
                SOS_PTR(pRelocRec->NewValue),
                SOS_PTR(pRelocRec->methodTable),
                pPromoteRec ? "yes" : "no");
            if (pPromoteRec != NULL)
            {
                // There should be similarities between the promote and reloc record
                if (pPromoteRec->Value != pRelocRec->PrevValue ||
                    pPromoteRec->methodTable != pRelocRec->methodTable)
                {
                    ExtOut ("promote/reloc records in error ");
                }

                if (bPromotedMoreThanOnce || bRelocatedMoreThanOnce)
                {
                    ExtOut ("Duplicate promote/relocs");
                }
            }
            ExtOut ("\n");
        }
        else if (pPromoteRec)
        {
            ExtOut ("Error: There is a promote record for root %p, but no relocation record\n",
                (ULONG64) pPromoteRec->Root);
        }
        else
        {
            if (!bBoringPeople)
            {
                ExtOut ("...\n");
                bBoringPeople = true;
            }
        }        
    }    
    return Status;
}

DECLARE_API(HistObjFind)
{
    INIT_API();
    size_t nArg;

    StringHolder objstr;
    CMDValue arg[] = 
    {
        // vptr, type
        {&objstr.data, COSTRING},
    };

    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
        return Status;

    if (nArg != 1)
    {
        ExtOut ("!ObjSearch <valid object pointer>\n");
        return Status;
    }

    size_t object = (size_t) GetExpression(objstr.data);
    
    ExtOut ("%8s %" POINTERSIZE "s %40s\n",
        "GCCount", "Object", "Message");
    ExtOut ("---------------------------------------------------------\n");

    size_t curAddress = object;
    bool bBoringPeople = false;
    
    // Just traverse the data structure, printing basic stats
    for (UINT i=0; i < g_recordCount; i++)
    {
        if (curAddress == 0)
        {
            break;
        }
        
        UINT GCCount = (UINT) g_records[i].GCCount;        

        PromoteRecord *pPtr = g_records[i].PromoteList;
        while(pPtr)
        {
            if (pPtr->Value  == curAddress)
            {
                bBoringPeople = false;
                ExtOut ("%8d %p ", GCCount, SOS_PTR(curAddress));
                ExtOut ("Promotion for root %p (MT = %p)\n",
                    SOS_PTR(pPtr->Root),
                    SOS_PTR(pPtr->methodTable));
            }
            pPtr = pPtr->next;
        }

        RelocRecord *pReloc = g_records[i].RelocList;
        while(pReloc)
        {
            if (pReloc->NewValue == curAddress ||
                pReloc->PrevValue == curAddress)
            {
                bBoringPeople = false;
                ExtOut ("%8d %p ", GCCount, SOS_PTR(curAddress));
                ExtOut ("Relocation %s for root %p\n",
                    (pReloc->NewValue == curAddress) ? "NEWVALUE" : "PREVVALUE",
                    SOS_PTR(pReloc->Root));
            }
            pReloc = pReloc->next;
        }
        
        if (!bBoringPeople)
        {
            ExtOut ("...\n");
            bBoringPeople = true;
        }

    }    
    return Status;
}

DECLARE_API(HistObj)
{
    INIT_API();
    size_t nArg;

    StringHolder objstr;
    CMDValue arg[] = 
    {
        // vptr, type
        {&objstr.data, COSTRING},
    };

    if (!GetCMDOption(args, NULL, 0, arg, _countof(arg), &nArg))
        return Status;

    if (nArg != 1)
    {
        ExtOut ("!object <valid object pointer>\n");
        return Status;
    }

    size_t object = (size_t) GetExpression(objstr.data);
    
    ExtOut ("%8s %" POINTERSIZE "s %40s\n",
        "GCCount", "Object", "Roots");
    ExtOut ("---------------------------------------------------------\n");

    size_t curAddress = object;
    
    // Just traverse the data structure, printing basic stats
    for (UINT i=0; i < g_recordCount; i++)
    {
        if (curAddress == 0)
        {
            break;
        }
        
        UINT GCCount = (UINT) g_records[i].GCCount;

        ExtOut ("%8d %p ", GCCount, SOS_PTR(curAddress));

        RelocRecord *pReloc = g_records[i].RelocList;
        size_t candidateCurAddress = curAddress;
        bool bFirstReloc = true;
        while(pReloc)
        {
            if (pReloc->NewValue == curAddress)
            {
                ExtOut ("%p, ", SOS_PTR(pReloc->Root));
                if (bFirstReloc)
                {
                    candidateCurAddress = pReloc->PrevValue;
                    bFirstReloc = false;
                }
                else if (candidateCurAddress != pReloc->PrevValue)
                {
                    ExtOut ("differing reloc values for this object!\n");
                }
            }
            pReloc = pReloc->next;
        }

        ExtOut ("\n");
        curAddress = candidateCurAddress;                
    }    
    return Status;
}

DECLARE_API(HistInit)
{
    INIT_API();

    GcHistClear();

    CLRDATA_ADDRESS stressLogAddr = 0;
    if (g_sos->GetStressLogAddress(&stressLogAddr) != S_OK)
    {
        ExtOut("Unable to find stress log via DAC\n");
        return E_FAIL;
    }    
    
    ExtOut ("Attempting to read Stress log\n");
        
    Status = StressLog::Dump(stressLogAddr, NULL, g_ExtData);
    if (Status == S_OK)
        ExtOut("SUCCESS: GCHist structures initialized\n");
    else if (Status == S_FALSE)
        ExtOut("No Stress log in the image, GCHist commands unavailable\n");
    else
        ExtOut("FAILURE: Stress log unreadable\n");

    return Status;
}

DECLARE_API(HistClear)
{
    INIT_API();
    GcHistClear();
    ExtOut("Completed successfully.\n");
    return Status;
}

