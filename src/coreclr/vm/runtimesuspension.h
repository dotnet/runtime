// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct Tasklet;
struct AsyncDataFrame;
struct RuntimeAsyncReturnValue;

extern "C" Tasklet* QCALLTYPE RuntimeSuspension_CaptureTasklets(QCall::StackCrawlMarkHandle stackMark, uint8_t* returnValue, uint8_t useReturnValueHandle, void* taskAsyncData, Tasklet** lastTasklet);
extern "C" void QCALLTYPE RuntimeSuspension_DeleteTasklet(Tasklet* tasklet);

EXTERN_C FCDECL1(void, RuntimeSuspension_UnwindToFunctionWithAsyncFrame, AsyncDataFrame* frame);

EXTERN_C FCDECL2(Object*, RuntimeSuspension_ResumeTaskletReferenceReturn, Tasklet* tasklet, RuntimeAsyncReturnValue *resumptionReturnValue);
EXTERN_C FCDECL2(Object*, RuntimeSuspension_ResumeTaskletIntegerRegisterReturn, Tasklet* tasklet, RuntimeAsyncReturnValue *resumptionReturnValue);
