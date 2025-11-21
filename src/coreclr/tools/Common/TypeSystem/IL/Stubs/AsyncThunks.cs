// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Internal.TypeSystem;
using Internal.ReadyToRunConstants;

namespace Internal.IL.Stubs
{
    public static class AsyncThunkILEmitter
    {
        private static MethodDesc GetHelperMethod(TypeSystemContext context, ReadyToRunHelper helper)
        {
            ILCompiler.JitHelper.GetEntryPoint(context, helper, out _, out MethodDesc methodDesc);
            return methodDesc;
        }

        // Emits a thunk that wraps an async method to return a Task or ValueTask.
        // The thunk calls the async method, and if it completes synchronously,
        // it returns a completed Task/ValueTask. If the async method suspends,
        // it calls FinalizeTaskReturningThunk/FinalizeValueTaskReturningThunk method to get the Task/ValueTask.

        // The emitted code matches method EmitTaskReturningThunk in CoreCLR VM.
        public static MethodIL EmitTaskReturningThunk(MethodDesc taskReturningMethod, MethodDesc asyncMethod)
        {
            TypeSystemContext context = taskReturningMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            MethodSignature sig = taskReturningMethod.Signature;
            TypeDesc returnType = sig.ReturnType;

            bool isValueTask = returnType.IsValueType;

            TypeDesc logicalReturnType = null;
            ILLocalVariable logicalResultLocal = 0;
            if (returnType.HasInstantiation)
            {
                // The return type is either Task<T> or ValueTask<T>, exactly one generic argument
                logicalReturnType = returnType.Instantiation[0];
                logicalResultLocal = emitter.NewLocal(logicalReturnType);
            }

            ILLocalVariable returnTaskLocal = emitter.NewLocal(returnType);

            TypeDesc executionAndSyncBlockStoreType = context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "ExecutionAndSyncBlockStore"u8);
            ILLocalVariable executionAndSyncBlockStoreLocal = emitter.NewLocal(executionAndSyncBlockStoreType);

            ILCodeLabel returnTaskLabel = emitter.NewCodeLabel();
            ILCodeLabel suspendedLabel = emitter.NewCodeLabel();
            ILCodeLabel finishedLabel = emitter.NewCodeLabel();

            codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
            codestream.Emit(ILOpcode.call, emitter.NewToken(GetHelperMethod(context, ReadyToRunHelper.ExecutionAndSyncBlockStore_Push)));

            ILExceptionRegionBuilder tryFinallyRegion = emitter.NewFinallyRegion();
            {
                codestream.BeginTry(tryFinallyRegion);
                codestream.Emit(ILOpcode.nop);

                TypeDesc exceptionType = context.GetWellKnownType(WellKnownType.Exception);
                ILExceptionRegionBuilder tryCatchRegion = emitter.NewCatchRegion(exceptionType);
                {
                    codestream.BeginTry(tryCatchRegion);

                    int localArg = 0;
                    if (!sig.IsStatic)
                    {
                        codestream.EmitLdArg(localArg++);
                    }

                    for (int iArg = 0; iArg < sig.Length; iArg++)
                    {
                        codestream.EmitLdArg(localArg++);
                    }

                    if (asyncMethod.OwningType.HasInstantiation)
                    {
                        var instantiatedType = (InstantiatedType)TypeSystemHelpers.InstantiateAsOpen(asyncMethod.OwningType);
                        asyncMethod = context.GetMethodForInstantiatedType(asyncMethod, instantiatedType);
                    }

                    if (asyncMethod.HasInstantiation)
                    {
                        var inst = new TypeDesc[asyncMethod.Instantiation.Length];
                        for (int i = 0; i < inst.Length; i++)
                        {
                            inst[i] = context.GetSignatureVariable(i, true);
                        }
                        asyncMethod = asyncMethod.MakeInstantiatedMethod(new Instantiation(inst));
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(asyncMethod));

                    if (logicalReturnType != null)
                    {
                        codestream.EmitStLoc(logicalResultLocal);
                    }

                    MethodDesc asyncCallContinuationMd = GetHelperMethod(context, ReadyToRunHelper.AsyncHelpers_AsyncCallContinuation);

                    codestream.Emit(ILOpcode.call, emitter.NewToken(asyncCallContinuationMd));

                    codestream.Emit(ILOpcode.brfalse, finishedLabel);
                    codestream.Emit(ILOpcode.leave, suspendedLabel);
                    codestream.EmitLabel(finishedLabel);

                    if (logicalReturnType != null)
                    {
                        codestream.EmitLdLoc(logicalResultLocal);

                        MethodDesc fromResultMethod;
                        if (isValueTask)
                        {
                            fromResultMethod = GetHelperMethod(context, ReadyToRunHelper.ValueTask_FromResult)
                                .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                        }
                        else
                        {
                            fromResultMethod = GetHelperMethod(context, ReadyToRunHelper.Task_FromResult)
                                .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                        }

                        codestream.Emit(ILOpcode.call, emitter.NewToken(fromResultMethod));
                    }
                    else
                    {
                        MethodDesc getCompletedTaskMethod;
                        if (isValueTask)
                        {
                            getCompletedTaskMethod = GetHelperMethod(context, ReadyToRunHelper.ValueTask_get_CompletedTask);
                        }
                        else
                        {
                            getCompletedTaskMethod = GetHelperMethod(context, ReadyToRunHelper.Task_get_CompletedTask);
                        }
                        codestream.Emit(ILOpcode.call, emitter.NewToken(getCompletedTaskMethod));
                    }

                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);

                    codestream.EndTry(tryCatchRegion);
                }
                // Catch
                {
                    codestream.BeginHandler(tryCatchRegion);

                    MethodDesc fromExceptionMd;
                    if (logicalReturnType != null)
                    {
                        fromExceptionMd = GetHelperMethod(context, isValueTask ? ReadyToRunHelper.AsyncHelpers_ValueTaskFromException : ReadyToRunHelper.AsyncHelpers_TaskFromException)
                            .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                    }
                    else
                    {
                        fromExceptionMd = GetHelperMethod(context, isValueTask ? ReadyToRunHelper.AsyncHelpers_ValueTaskFromException : ReadyToRunHelper.AsyncHelpers_TaskFromException);
                    }

                    codestream.Emit(ILOpcode.call, emitter.NewToken(fromExceptionMd));
                    codestream.EmitStLoc(returnTaskLocal);
                    codestream.Emit(ILOpcode.leave, returnTaskLabel);
                    codestream.EndHandler(tryCatchRegion);
                }

                codestream.EmitLabel(suspendedLabel);

                MethodDesc finalizeTaskReturningThunkMd;
                if (logicalReturnType != null)
                {
                    finalizeTaskReturningThunkMd = GetHelperMethod(context, isValueTask ? ReadyToRunHelper.AsyncHelpers_FinalizeValueTaskReturningThunk : ReadyToRunHelper.AsyncHelpers_FinalizeTaskReturningThunk)
                        .MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }
                else
                {
                    finalizeTaskReturningThunkMd = GetHelperMethod(context, isValueTask ? ReadyToRunHelper.AsyncHelpers_FinalizeValueTaskReturningThunk : ReadyToRunHelper.AsyncHelpers_FinalizeTaskReturningThunk);
                }

                codestream.Emit(ILOpcode.call, emitter.NewToken(finalizeTaskReturningThunkMd));
                codestream.EmitStLoc(returnTaskLocal);
                codestream.Emit(ILOpcode.leave, returnTaskLabel);

                codestream.EndTry(tryFinallyRegion);
            }

            {
                codestream.BeginHandler(tryFinallyRegion);

                codestream.EmitLdLoca(executionAndSyncBlockStoreLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(GetHelperMethod(context, ReadyToRunHelper.ExecutionAndSyncBlockStore_Pop)));
                codestream.Emit(ILOpcode.endfinally);
                codestream.EndHandler(tryFinallyRegion);
            }

            codestream.EmitLabel(returnTaskLabel);
            codestream.EmitLdLoc(returnTaskLocal);
            codestream.Emit(ILOpcode.ret);

            return emitter.Link(taskReturningMethod);
        }

        // The emitted code matches method EmitAsyncMethodThunk in CoreCLR VM.
        public static MethodIL EmitAsyncMethodThunk(MethodDesc asyncMethod, MethodDesc taskReturningMethod)
        {
            TypeSystemContext context = asyncMethod.Context;

            var emitter = new ILEmitter();
            var codestream = emitter.NewCodeStream();

            if (taskReturningMethod.OwningType.HasInstantiation)
            {
                var instantiatedType = (InstantiatedType)TypeSystemHelpers.InstantiateAsOpen(taskReturningMethod.OwningType);
                taskReturningMethod = context.GetMethodForInstantiatedType(taskReturningMethod, instantiatedType);
            }

            if (taskReturningMethod.HasInstantiation)
            {
                var inst = new TypeDesc[taskReturningMethod.Instantiation.Length];
                for (int i = 0; i < inst.Length; i++)
                {
                    inst[i] = context.GetSignatureVariable(i, true);
                }
                taskReturningMethod = taskReturningMethod.MakeInstantiatedMethod(new Instantiation(inst));
            }

            MethodSignature sig = asyncMethod.Signature;

            int localArg = 0;
            if (!sig.IsStatic)
            {
                codestream.EmitLdArg(localArg++);
            }

            for (int iArg = 0; iArg < sig.Length; iArg++)
            {
                codestream.EmitLdArg(localArg++);
            }

            codestream.Emit(ILOpcode.call, emitter.NewToken(taskReturningMethod));

            TypeDesc taskReturningMethodReturnType = taskReturningMethod.Signature.ReturnType;

            bool isValueTask = taskReturningMethodReturnType.IsValueType;

            if (isValueTask)
            {
                TypeDesc valueTaskType = taskReturningMethodReturnType;
                MethodDesc isCompletedMethod;
                MethodDesc completionResultMethod;
                MethodDesc asTaskOrNotifierMethod;

                if (!taskReturningMethodReturnType.HasInstantiation)
                {
                    // ValueTask (non-generic)
                    isCompletedMethod = valueTaskType.GetKnownMethod("get_IsCompleted"u8, null);
                    completionResultMethod = valueTaskType.GetKnownMethod("ThrowIfCompletedUnsuccessfully"u8, null);
                    asTaskOrNotifierMethod = valueTaskType.GetKnownMethod("AsTaskOrNotifier"u8, null);
                }
                else
                {
                    // ValueTask<T> (generic)
                    isCompletedMethod = GetHelperMethod(context, ReadyToRunHelper.ValueTask_get_IsCompleted);
                    completionResultMethod = GetHelperMethod(context, ReadyToRunHelper.ValueTask_get_Result);
                    asTaskOrNotifierMethod = GetHelperMethod(context, ReadyToRunHelper.ValueTask_AsTaskOrNotifier);
                }

                ILLocalVariable valueTaskLocal = emitter.NewLocal(valueTaskType);
                ILCodeLabel valueTaskCompletedLabel = emitter.NewCodeLabel();

                // Store value task returned by call to actual user func
                codestream.EmitStLoc(valueTaskLocal);
                codestream.EmitLdLoca(valueTaskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(isCompletedMethod));
                codestream.Emit(ILOpcode.brtrue, valueTaskCompletedLabel);

                codestream.EmitLdLoca(valueTaskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(asTaskOrNotifierMethod));
                codestream.Emit(ILOpcode.call, emitter.NewToken(
                    GetHelperMethod(context, ReadyToRunHelper.AsyncHelpers_TransparentAwait)));

                codestream.EmitLabel(valueTaskCompletedLabel);
                codestream.EmitLdLoca(valueTaskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(completionResultMethod));
                codestream.Emit(ILOpcode.ret);
            }
            else
            {
                // Task path
                TypeDesc taskType = taskReturningMethodReturnType;
                MethodDesc completedTaskResultMethod;

                if (!taskReturningMethodReturnType.HasInstantiation)
                {
                    // Task (non-generic)
                    completedTaskResultMethod = GetHelperMethod(context, ReadyToRunHelper.AsyncHelpers_CompletedTask);
                }
                else
                {
                    // Task<T> (generic)
                    TypeDesc logicalReturnType = taskReturningMethodReturnType.Instantiation[0];

                    MethodDesc completedTaskResultMethodOpen = GetHelperMethod(context, ReadyToRunHelper.AsyncHelpers_CompletedTaskResult);
                    completedTaskResultMethod = completedTaskResultMethodOpen.MakeInstantiatedMethod(new Instantiation(logicalReturnType));
                }

                ILLocalVariable taskLocal = emitter.NewLocal(taskType);
                ILCodeLabel getResultLabel = emitter.NewCodeLabel();

                // Store task returned by actual user func or by ValueTask.AsTask
                codestream.EmitStLoc(taskLocal);

                codestream.EmitLdLoc(taskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(
                    GetHelperMethod(context, ReadyToRunHelper.Task_get_IsCompleted)));
                codestream.Emit(ILOpcode.brtrue, getResultLabel);

                codestream.EmitLdLoc(taskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(
                    GetHelperMethod(context, ReadyToRunHelper.AsyncHelpers_TransparentAwait)));

                codestream.EmitLabel(getResultLabel);
                codestream.EmitLdLoc(taskLocal);
                codestream.Emit(ILOpcode.call, emitter.NewToken(completedTaskResultMethod));
                codestream.Emit(ILOpcode.ret);
            }

            return emitter.Link(asyncMethod);
        }
    }
}
