// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#ifndef __STACKFRAME_H
#define __STACKFRAME_H

#include "regdisp.h"


struct StackFrame
{
    const static UINT_PTR maxVal = (UINT_PTR)(INT_PTR)-1;
    StackFrame() : SP(NULL)
    {
    }

    StackFrame(UINT_PTR sp)
    {
        SP = sp;
    }

    void Clear()
    {
        SP = NULL;
    }

    void SetMaxVal()
    {
        SP = maxVal;
    }

    bool IsNull()
    {
        return (SP == NULL);
    }

    bool IsMaxVal()
    {
        return (SP == maxVal);
    }

    bool operator==(StackFrame sf)
    {
        return (SP == sf.SP);
    }

    bool operator!=(StackFrame sf)
    {
        return (SP != sf.SP);
    }

    bool operator<(StackFrame sf)
    {
        return (SP < sf.SP);
    }

    bool operator<=(StackFrame sf)
    {
        return (SP <= sf.SP);
    }

    bool operator>(StackFrame sf)
    {
        return (SP > sf.SP);
    }

    bool operator>=(StackFrame sf)
    {
        return (SP >= sf.SP);
    }

    static inline StackFrame FromEstablisherFrame(UINT_PTR EstablisherFrame)
    {
        return StackFrame(EstablisherFrame);
    }

    static inline StackFrame FromRegDisplay(REGDISPLAY* pRD)
    {
        return StackFrame(GetRegdisplaySP(pRD));
    }

    UINT_PTR SP;
};


//---------------------------------------------------------------------------------------
//
// On WIN64, all the stack range tracking done by the Exception Handling (EH) subsystem is based on the 
// establisher frame given by the OS.  On IA64, the establisher frame is the caller SP and the current BSP.
// On X64, it is the initial SP before any dynamic stack allocation, i.e. it is the SP when a function exits
// the prolog.  The EH subsystem uses the same format.
//
// The stackwalker needs to get information from the EH subsystem in order to skip funclets.  Unfortunately, 
// stackwalking is based on the current SP, i.e. the SP when the control flow leaves a function via a 
// function call.  Thus, for stack frames with dynamic stack allocations on X64, the SP values used by the 
// stackwalker and the EH subsystem don't match.  
//
// To work around this problem, we need to somehow bridge the different SP values.  We do so by using the
// caller SP instead of the current SP for comparisons during a stackwalk on X64.  Creating a new type 
// explicitly spells out the important distinction that this is NOT in the same format as the 
// OS establisher frame.
//
// Notes:
//    In the long term, we should look at merging the two SP formats and have one consistent abstraction.
//

struct CallerStackFrame : StackFrame
{
    CallerStackFrame() : StackFrame()
    {
    }

    CallerStackFrame(UINT_PTR sp) : StackFrame(sp)
    {
    }

#ifdef WIN64EXCEPTIONS
    static inline CallerStackFrame FromRegDisplay(REGDISPLAY* pRD)
    {
        _ASSERTE(pRD->IsCallerSPValid || pRD->IsCallerContextValid);
        return CallerStackFrame(GetSP(pRD->pCallerContext));
    }
#endif // WIN64EXCEPTIONS
};

#endif  // __STACKFRAME_H
