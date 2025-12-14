// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef FRAME_TYPE_NAME
#define FRAME_TYPE_NAME(frameType)
#endif

FRAME_TYPE_NAME(InlinedCallFrame)
#if defined(TARGET_X86) && !defined(UNIX_X86_ABI)
FRAME_TYPE_NAME(TailCallFrame)
#endif
#ifdef FEATURE_HIJACK
FRAME_TYPE_NAME(ResumableFrame)
FRAME_TYPE_NAME(RedirectedThreadFrame)
#endif // FEATURE_HIJACK
FRAME_TYPE_NAME(FaultingExceptionFrame)
FRAME_TYPE_NAME(SoftwareExceptionFrame)
#ifdef DEBUGGING_SUPPORTED
FRAME_TYPE_NAME(FuncEvalFrame)
#endif // DEBUGGING_SUPPORTED
#ifdef FEATURE_COMINTEROP
FRAME_TYPE_NAME(ComMethodFrame)
FRAME_TYPE_NAME(CLRToCOMMethodFrame)
FRAME_TYPE_NAME(ComPrestubMethodFrame)
#endif // FEATURE_COMINTEROP
FRAME_TYPE_NAME(PInvokeCalliFrame)
#ifdef FEATURE_HIJACK
FRAME_TYPE_NAME(HijackFrame)
#endif // FEATURE_HIJACK
FRAME_TYPE_NAME(PrestubMethodFrame)
FRAME_TYPE_NAME(CallCountingHelperFrame)
FRAME_TYPE_NAME(StubDispatchFrame)
FRAME_TYPE_NAME(ExternalMethodFrame)
FRAME_TYPE_NAME(DynamicHelperFrame)
FRAME_TYPE_NAME(ProtectValueClassFrame)
FRAME_TYPE_NAME(DebuggerClassInitMarkFrame)
FRAME_TYPE_NAME(DebuggerExitFrame)
FRAME_TYPE_NAME(DebuggerU2MCatchHandlerFrame)
FRAME_TYPE_NAME(ExceptionFilterFrame)
#ifdef FEATURE_INTERPRETER
FRAME_TYPE_NAME(InterpreterFrame)
#endif // FEATURE_INTERPRETER

#undef FRAME_TYPE_NAME
