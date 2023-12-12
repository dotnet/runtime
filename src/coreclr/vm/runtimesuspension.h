// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

struct Tasklet;
struct AsyncDataFrame;
struct RuntimeAsyncReturnValue;

extern "C" Tasklet* QCALLTYPE RuntimeSuspension_CaptureTasklets(QCall::StackCrawlMarkHandle stackMark, uint8_t* returnValue, uint8_t useReturnValueHandle, void* taskAsyncData, Tasklet** lastTasklet, int32_t* pFramesCaptured);
extern "C" void QCALLTYPE RuntimeSuspension_RegisterTasklet(Tasklet * tasklet);
extern "C" void QCALLTYPE RuntimeSuspension_DeleteTasklet(Tasklet* tasklet);

EXTERN_C FCDECL1(void, RuntimeSuspension_UnwindToFunctionWithAsyncFrame, AsyncDataFrame* frame);

EXTERN_C FCDECL2(Object*, RuntimeSuspension_ResumeTaskletReferenceReturn, Tasklet* tasklet, RuntimeAsyncReturnValue *resumptionReturnValue);
EXTERN_C FCDECL2(Object*, RuntimeSuspension_ResumeTaskletIntegerRegisterReturn, Tasklet* tasklet, RuntimeAsyncReturnValue *resumptionReturnValue);

void RegisterTasklet(Tasklet* pTasklet);
void InitializeTasklets();
void UnregisterTasklet(Tasklet* pTasklet);

void IterateTaskletsForGC(promote_func* pCallback, int condemned, ScanContext* sc);
void AgeTasklets(int condemned, int max_gen, ScanContext* sc);
void RejuvenateTasklets(int condemned, int max_gen, ScanContext* sc);
