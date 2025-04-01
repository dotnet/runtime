// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#ifndef EXCEPTION_HANDLING_QCALLS_H
#define EXCEPTION_HANDLING_QCALLS_H

#ifdef FEATURE_EH_FUNCLETS

struct RhEHClause;
struct ExInfo;

#ifndef DACCESS_COMPILE

extern "C" void * QCALLTYPE CallCatchFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo);
extern "C" void QCALLTYPE CallFinallyFunclet(BYTE* pHandlerIP, REGDISPLAY* pvRegDisplay, ExInfo* exInfo);
extern "C" CLR_BOOL QCALLTYPE CallFilterFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pFilterP, REGDISPLAY* pvRegDisplay);
extern "C" void QCALLTYPE ResumeAtInterceptionLocation(REGDISPLAY* pvRegDisplay);
extern "C" void QCALLTYPE AppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo);
extern "C" CLR_BOOL QCALLTYPE EHEnumInitFromStackFrameIterator(StackFrameIterator *pFrameIter, IJitManager::MethodRegionInfo *pMethodRegionInfo, EH_CLAUSE_ENUMERATOR * pEHEnum);
extern "C" CLR_BOOL QCALLTYPE EHEnumNext(EH_CLAUSE_ENUMERATOR* pEHEnum, RhEHClause* pEHClause);
extern "C" CLR_BOOL QCALLTYPE SfiInit(StackFrameIterator* pThis, CONTEXT* pStackwalkCtx, CLR_BOOL instructionFault, CLR_BOOL* pIsExceptionIntercepted);
extern "C" CLR_BOOL QCALLTYPE SfiNext(StackFrameIterator* pThis, unsigned int* uExCollideClauseIdx, CLR_BOOL* fUnwoundReversePInvoke, CLR_BOOL* pIsExceptionIntercepted);
#endif // DACCESS_COMPILE

#endif // FEATURE_EH_FUNCLETS

#endif // EXCEPTION_HANDLING_QCALLS_H