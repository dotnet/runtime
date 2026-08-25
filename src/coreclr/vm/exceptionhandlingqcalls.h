// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef EXCEPTION_HANDLING_QCALLS_H
#define EXCEPTION_HANDLING_QCALLS_H

struct RhEHClause;
struct ExInfo;

#ifndef DACCESS_COMPILE

extern "C" QCallExceptionStatus QCALLTYPE CallFilterFunclet(QCall::ObjectHandleOnStack exceptionObj, BYTE* pFilterP, REGDISPLAY* pvRegDisplay, CLR_BOOL* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE AppendExceptionStackFrame(QCall::ObjectHandleOnStack exceptionObj, SIZE_T ip, SIZE_T sp, int flags, ExInfo *pExInfo);
extern "C" CLR_BOOL QCALLTYPE EHEnumInitFromStackFrameIterator(StackFrameIterator *pFrameIter, IJitManager::MethodRegionInfo *pMethodRegionInfo, EH_CLAUSE_ENUMERATOR * pEHEnum);
extern "C" QCallExceptionStatus QCALLTYPE EHEnumNext(EH_CLAUSE_ENUMERATOR* pEHEnum, RhEHClause* pEHClause, CLR_BOOL* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE SfiInit(StackFrameIterator* pThis, CONTEXT* pStackwalkCtx, CLR_BOOL instructionFault, CLR_BOOL* pIsExceptionIntercepted, CLR_BOOL* pReturnValue);
extern "C" QCallExceptionStatus QCALLTYPE SfiNext(StackFrameIterator* pThis, unsigned int* uExCollideClauseIdx, CLR_BOOL* fUnwoundReversePInvoke, CLR_BOOL* pIsExceptionIntercepted, CLR_BOOL* pReturnValue);
#endif // DACCESS_COMPILE

#endif // EXCEPTION_HANDLING_QCALLS_H

