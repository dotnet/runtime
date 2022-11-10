// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef EXCEPTION_HANDLING_QCALLS_H
#define EXCEPTION_HANDLING_QCALLS_H

#ifdef FEATURE_EH_FUNCLETS

struct RhEHClause;
struct ExInfo;

#ifndef DACCESS_COMPILE

extern "C" void * QCALLTYPE RhpCallCatchFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo);
extern "C" void QCALLTYPE RhpCallFinallyFunclet(BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo);
extern "C" BOOL QCALLTYPE RhpCallFilterFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pFilterP, REGDISPLAY* pvRegDisplay);
extern "C" void QCALLTYPE RhpAppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo);
extern "C" BOOL QCALLTYPE RhpEHEnumInitFromStackFrameIterator(StackFrameIterator *pFrameIter, BYTE** pMethodStartAddress, EH_CLAUSE_ENUMERATOR * pEHEnum);
extern "C" BOOL QCALLTYPE RhpEHEnumNext(EH_CLAUSE_ENUMERATOR* pEHEnum, RhEHClause* pEHClause);
extern "C" bool QCALLTYPE RhpSfiInit(StackFrameIterator* pThis, CONTEXT* pStackwalkCtx, bool instructionFault);
extern "C" bool QCALLTYPE RhpSfiNext(StackFrameIterator* pThis, unsigned int* uExCollideClauseIdx, bool* fUnwoundReversePInvoke);
#endif // DACCESS_COMPILE

#endif // FEATURE_EH_FUNCLETS

#endif // EXCEPTION_HANDLING_QCALLS_H